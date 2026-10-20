using System;
using UnityEngine;

namespace TARS.Locomotion
{
    public class BodyFit : BodyMotion
    {
        private const float AnticipationStart = 0.5f;
        private const float TiltRidge = 1e-3f;
        private const float MinWeight = 1e-4f;

        [SerializeField] private Transform _body;
        [SerializeField, Range(0f, 1f)] private float _tiltWeight = 1f;
        [SerializeField, Range(0f, 1f)] private float _crouch = 0.05f;
        [SerializeField, Range(0f, 1f)] private float _leanWeight = 0.5f;
        [SerializeField, Min(0f)] private float _smoothing = 0.25f;
        [SerializeField, HideInInspector] private Vector3 _restLocalPosition;
        [SerializeField, HideInInspector] private Quaternion _restLocalRotation = Quaternion.identity;

        private float _height;
        private float _heightVelocity;
        private float _pitch;
        private float _pitchVelocity;
        private float _roll;
        private float _rollVelocity;

        public Transform Body => _body;

        private void Reset()
        {
            if (_body != null)
                CaptureRestPose();
        }

        [ContextMenu("Capture Rest Pose")]
        public void CaptureRestPose()
        {
            _restLocalPosition = _body.localPosition;
            _restLocalRotation = _body.localRotation;
        }

        public override Vector3 RestWorldPoint(Transform owner, Vector3 worldPoint)
        {
            return RestPoint(_body, _restLocalPosition, _restLocalRotation, owner, worldPoint);
        }

        public override Vector3 RestWorldDirection(Transform owner, Vector3 worldDirection)
        {
            return RestDirection(_body, _restLocalRotation, owner, worldDirection);
        }

        protected override void ApplyPose(LegLocomotion locomotion, float deltaTime, float weight)
        {
            ReadOnlySpan<LegState> states = locomotion.States;
            ReadOnlySpan<LegRig> legs = locomotion.Legs;
            bool snap = deltaTime <= 0f;
            Vector3 acceleration = locomotion.ToBodyDirection(locomotion.Acceleration);
            Vector3 origin = RestOrigin(locomotion, _body, _restLocalPosition);
            Span<Vector3> limits = stackalloc Vector3[states.Length];
            int limitCount = 0;
            float sw = 0f, sx = 0f, sz = 0f, sxx = 0f, szz = 0f, sxz = 0f, sy = 0f, sxy = 0f, szy = 0f;
            float hipHeight = 0f;
            int count = 0;
            for (int i = 0; i < states.Length; i++)
            {
                if (!legs[i].transform.IsChildOf(_body)) continue;
                ref readonly LegState state = ref states[i];
                float w = 1f;
                Vector3 ankle = state.IsSwinging ? locomotion.SwingContact(i, AnticipationStart, out w) : locomotion.ToBodyPoint(state.SupportAnkle);
                Vector3 foot = ankle - origin;
                float ground = ankle.y - state.RestFootLocal.y;
                sw += w;
                sx += w * foot.x;
                sz += w * foot.z;
                sxx += w * foot.x * foot.x;
                szz += w * foot.z * foot.z;
                sxz += w * foot.x * foot.z;
                sy += w * ground;
                sxy += w * foot.x * ground;
                szy += w * foot.z * ground;
                hipHeight += state.HipHeight;
                count++;
                if (state.IsSwinging) continue;
                Vector3 hip = state.RestUpperLocal - origin;
                limits[limitCount++] = new Vector3(hip.x, state.ReachHeight(ankle, state.RestUpperLocal) - _crouch * state.HipHeight, hip.z);
            }
            if (count == 0) return;
            hipHeight /= count;
            float a = 0f, b = 0f, c = 0f;
            if (sw > MinWeight)
            {
                float ridge = TiltRidge * (sxx + szz + sw * hipHeight * hipHeight);
                FitPlane(sw, sx, sz, sxx + ridge, szz + ridge, sxz, sy, sxy, szy, out b, out c);
                b *= _tiltWeight;
                c *= _tiltWeight;
                a = (sy - b * sx - c * sz) / sw;
            }
            for (int i = 0; i < limitCount; i++)
                a = Mathf.Min(a, limits[i].y - b * limits[i].x - c * limits[i].z);
            float gravity = Physics.gravity.magnitude;
            float pitch = -Mathf.Atan(c) * Mathf.Rad2Deg + Mathf.Atan2(acceleration.z, gravity) * Mathf.Rad2Deg * _leanWeight;
            float roll = Mathf.Atan(b) * Mathf.Rad2Deg - Mathf.Atan2(acceleration.x, gravity) * Mathf.Rad2Deg * _leanWeight;
            if (snap)
            {
                _height = a;
                _pitch = pitch;
                _roll = roll;
                _heightVelocity = 0f;
                _pitchVelocity = 0f;
                _rollVelocity = 0f;
            }
            else
            {
                float smoothTime = _smoothing * Mathf.Sqrt(hipHeight / gravity);
                _height = Mathf.SmoothDamp(_height, a, ref _heightVelocity, smoothTime, Mathf.Infinity, deltaTime);
                _pitch = Mathf.SmoothDamp(_pitch, pitch, ref _pitchVelocity, smoothTime, Mathf.Infinity, deltaTime);
                _roll = Mathf.SmoothDamp(_roll, roll, ref _rollVelocity, smoothTime, Mathf.Infinity, deltaTime);
            }
            Output(locomotion, _body, _restLocalPosition, _restLocalRotation, new Vector3(0f, _height, 0f), Quaternion.Euler(_pitch, 0f, _roll), weight);
        }

        private static void FitPlane(float sw, float sx, float sz, float sxx, float szz, float sxz, float sy, float sxy, float szy, out float slopeX, out float slopeZ)
        {
            float vxx = sxx - sx * sx / sw;
            float vzz = szz - sz * sz / sw;
            float vxz = sxz - sx * sz / sw;
            float vxy = sxy - sx * sy / sw;
            float vzy = szy - sz * sy / sw;
            float determinant = vxx * vzz - vxz * vxz;
            if (Mathf.Abs(determinant) < MinWeight * MinWeight)
            {
                slopeX = 0f;
                slopeZ = 0f;
                return;
            }
            slopeX = (vxy * vzz - vzy * vxz) / determinant;
            slopeZ = (vzy * vxx - vxy * vxz) / determinant;
        }
    }
}
