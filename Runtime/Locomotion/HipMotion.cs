using System;
using UnityEngine;

namespace TARS.Locomotion
{
    public class HipMotion : BodyMotion
    {
        private const float SqrEpsilon = 1e-8f;
        private const float WalkLandingAnticipation = 0.35f;
        private const float RunLandingAnticipation = 0.75f;
        private const float RunBlendWidth = 0.1f;
        private const float LoadingEnd = 0.3f;
        private const float RunCompression = 0.5f;
        private const float CapRounding = 0.25f;
        private const float MinRounding = 1e-3f;
        private const float RunBlendTime = 0.5f;

        [SerializeField] private Transform _hips;
        [SerializeField, Range(0f, 1f)] private float _crouch = 0.05f;
        [SerializeField, Range(0f, 1f)] private float _walkBounce = 0.3f;
        [SerializeField, Range(0f, 2f)] private float _runBounce = 1f;
        [SerializeField, Range(0f, 1f)] private float _swayWeight = 0.3f;
        [SerializeField, Range(0f, 15f)] private float _hipDrop = 4f;
        [SerializeField, Range(0f, 1f)] private float _twistWeight = 0.07f;
        [SerializeField, Range(0f, 1f)] private float _leanWeight = 0.5f;
        [SerializeField, Min(0f)] private float _smoothing = 0.25f;
        [SerializeField, HideInInspector] private Vector3 _restLocalPosition;
        [SerializeField, HideInInspector] private Quaternion _restLocalRotation = Quaternion.identity;

        private float _reach;
        private float _reachVelocity;
        private float _run;
        private float _runVelocity;
        private float _support;
        private float _sway;
        private float _swayVelocity;
        private float _twist;
        private float _twistVelocity;
        private float _drop;
        private float _dropVelocity;
        private float _pitch;
        private float _pitchVelocity;
        private float _bank;
        private float _bankVelocity;

        public Transform Hips => _hips;

        private void Reset()
        {
            if (_hips != null)
                CaptureRestPose();
        }

        [ContextMenu("Capture Rest Pose")]
        public void CaptureRestPose()
        {
            _restLocalPosition = _hips.localPosition;
            _restLocalRotation = _hips.localRotation;
        }

        public override Vector3 RestWorldPoint(Transform owner, Vector3 worldPoint)
        {
            return RestPoint(_hips, _restLocalPosition, _restLocalRotation, owner, worldPoint);
        }

        public override Vector3 RestWorldDirection(Transform owner, Vector3 worldDirection)
        {
            return RestDirection(_hips, _restLocalRotation, owner, worldDirection);
        }

        protected override void ApplyPose(LegLocomotion locomotion, float deltaTime, float weight)
        {
            ReadOnlySpan<LegState> states = locomotion.States;
            ReadOnlySpan<LegRig> legs = locomotion.Legs;
            float cycleDuration = locomotion.CycleDuration;
            float footfallInterval = locomotion.FootfallInterval;
            bool snap = deltaTime <= 0f;
            Vector3 localVelocity = locomotion.ToBodyDirection(locomotion.Velocity);
            Vector3 acceleration = locomotion.ToBodyDirection(locomotion.Acceleration);
            float run = 0f;
            for (int i = 0; i < states.Length; i++)
                run += RunWeight(states[i].DutyFactor, footfallInterval) / states.Length;
            if (snap)
            {
                _run = run;
                _runVelocity = 0f;
            }
            else
            {
                _run = Mathf.SmoothDamp(_run, run, ref _runVelocity, footfallInterval * cycleDuration * RunBlendTime, Mathf.Infinity, deltaTime);
            }
            float supportLateral = 0f;
            float restLateral = 0f;
            float twistNumerator = 0f;
            float twistDenominator = 0f;
            float drop = 0f;
            float chainLength = 0f;
            float hipHeight = 0f;
            float dutyFactor = 0f;
            float crouch = 0f;
            int planted = 0;
            for (int i = 0; i < states.Length; i++)
            {
                ref readonly LegState state = ref states[i];
                Vector3 hip = state.RestHipLocal;
                Vector3 foot = locomotion.ToBodyPoint(state.FootPosition);
                restLateral += state.RestFootLocal.x;
                twistNumerator += hip.x * (foot.z - hip.z);
                twistDenominator += hip.x * hip.x;
                chainLength += state.ChainLength / states.Length;
                hipHeight += state.HipHeight / states.Length;
                dutyFactor += state.DutyFactor / states.Length;
                crouch += _crouch * state.HipHeight / states.Length;
                if (state.IsSwinging)
                {
                    drop += SideSign(legs[i].Side) * Mathf.Sin(state.SwingProgress * Mathf.PI);
                }
                else
                {
                    supportLateral += foot.x;
                    planted++;
                }
            }
            float sway = planted > 0 ? _swayWeight * (supportLateral / planted - restLateral / states.Length) : 0f;
            float twist = twistDenominator > SqrEpsilon ? -Mathf.Atan(twistNumerator / twistDenominator) * Mathf.Rad2Deg * _twistWeight : 0f;
            float gravity = Physics.gravity.magnitude;
            float smoothTime = _smoothing * Mathf.Sqrt(hipHeight / gravity);
            float pitch = Mathf.Atan2(acceleration.z, gravity) * Mathf.Rad2Deg * _leanWeight;
            float bank = -Mathf.Atan2(acceleration.x, gravity) * Mathf.Rad2Deg * _leanWeight;
            if (snap)
            {
                Snap(sway, twist, -_hipDrop * drop, pitch, bank);
            }
            else
            {
                _sway = Mathf.SmoothDamp(_sway, sway, ref _swayVelocity, smoothTime, Mathf.Infinity, deltaTime);
                _twist = Mathf.SmoothDamp(_twist, twist, ref _twistVelocity, smoothTime, Mathf.Infinity, deltaTime);
                _drop = Mathf.SmoothDamp(_drop, -_hipDrop * drop, ref _dropVelocity, smoothTime, Mathf.Infinity, deltaTime);
                _pitch = Mathf.SmoothDamp(_pitch, pitch, ref _pitchVelocity, smoothTime, Mathf.Infinity, deltaTime);
                _bank = Mathf.SmoothDamp(_bank, bank, ref _bankVelocity, smoothTime, Mathf.Infinity, deltaTime);
            }
            Quaternion turn = Quaternion.Euler(_pitch, _twist, _drop + _bank);
            Vector3 restHips = RestOrigin(locomotion, _hips, _restLocalPosition);
            Vector3 swayOffset = new Vector3(_sway, 0f, 0f);
            Span<float> arcs = stackalloc float[states.Length];
            Span<float> arcWeights = stackalloc float[states.Length];
            float reachHeight = float.MaxValue;
            float anticipationStart = Mathf.Lerp(WalkLandingAnticipation, RunLandingAnticipation, _run);
            for (int i = 0; i < states.Length; i++)
            {
                ref readonly LegState state = ref states[i];
                Vector3 upper = restHips + swayOffset + turn * (state.RestUpperLocal - restHips);
                if (state.IsSwinging)
                {
                    arcs[i] = state.ReachHeight(locomotion.SwingContact(i, anticipationStart, out arcWeights[i]), upper);
                    continue;
                }
                arcWeights[i] = 0f;
                arcs[i] = state.ReachHeight(locomotion.ToBodyPoint(state.SupportAnkle), upper);
                reachHeight = Mathf.Min(reachHeight, arcs[i]);
            }
            float landingLead = new Vector2(localVelocity.x, localVelocity.z).magnitude * cycleDuration * LegLocomotion.LandingLead(dutyFactor);
            float pendulumDip = chainLength - Mathf.Sqrt(Mathf.Max(chainLength * chainLength - landingLead * landingLead, 0f));
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].IsSwinging) continue;
                float stanceProgress = states[i].StanceProgress;
                float spring = stanceProgress < 1f ? Mathf.Sin(Mathf.PI * stanceProgress) : 0f;
                arcs[i] -= pendulumDip * Mathf.Lerp(_walkBounce * LoadingShape(stanceProgress), RunCompression * _runBounce * spring * spring, _run);
            }
            float minSoftness = MinRounding * chainLength;
            float capSoftness = Mathf.Max(pendulumDip * CapRounding * _run, minSoftness);
            if (planted > 0)
                _support = SoftCap(SoftMin(arcs, states, false, minSoftness), capSoftness);
            float freeHeight = SoftCap(Envelope(arcs, arcWeights, states, _support, minSoftness), capSoftness) - crouch;
            reachHeight = Mathf.Min(reachHeight, freeHeight + chainLength);
            if (snap || reachHeight <= _reach)
            {
                _reach = reachHeight;
                _reachVelocity = 0f;
            }
            else
            {
                _reach = Mathf.SmoothDamp(_reach, reachHeight, ref _reachVelocity, smoothTime, Mathf.Infinity, deltaTime);
            }
            Output(locomotion, _hips, _restLocalPosition, _restLocalRotation, new Vector3(_sway, Mathf.Min(freeHeight, _reach), 0f), turn, weight);
        }

        private static float LoadingShape(float stanceProgress)
        {
            if (stanceProgress >= LoadingEnd) return 0f;
            float rise = Mathf.Sin(Mathf.PI * stanceProgress / LoadingEnd);
            return rise * rise;
        }

        private static float RunWeight(float dutyFactor, float footfallInterval)
        {
            return Mathf.InverseLerp(footfallInterval * (1f + RunBlendWidth), footfallInterval * (1f - RunBlendWidth), dutyFactor);
        }

        private static float Envelope(Span<float> arcs, ReadOnlySpan<float> weights, ReadOnlySpan<LegState> states, float support, float softness)
        {
            for (int i = 0; i < arcs.Length; i++)
            {
                if (states[i].IsSwinging)
                    arcs[i] = Mathf.Lerp(support, Mathf.Min(arcs[i], support), weights[i]);
            }
            return SoftMin(arcs, states, true, softness);
        }

        private static float SoftCap(float value, float softness)
        {
            return Mathf.Min(value, 0f) - softness * Mathf.Log(1f + Mathf.Exp(-Mathf.Abs(value) / softness));
        }

        private static float SoftMin(ReadOnlySpan<float> values, ReadOnlySpan<LegState> states, bool includeSwinging, float softness)
        {
            float lowest = float.MaxValue;
            for (int i = 0; i < values.Length; i++)
            {
                if (includeSwinging || !states[i].IsSwinging)
                    lowest = Mathf.Min(lowest, values[i]);
            }
            float sum = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                if (includeSwinging || !states[i].IsSwinging)
                    sum += Mathf.Exp((lowest - values[i]) / softness);
            }
            return lowest - softness * Mathf.Log(sum);
        }

        private void Snap(float sway, float twist, float drop, float pitch, float bank)
        {
            _sway = sway;
            _twist = twist;
            _drop = drop;
            _pitch = pitch;
            _bank = bank;
            _swayVelocity = 0f;
            _twistVelocity = 0f;
            _dropVelocity = 0f;
            _pitchVelocity = 0f;
            _bankVelocity = 0f;
        }

        private static float SideSign(LegSide side)
        {
            return side switch { LegSide.Left => -1f, LegSide.Right => 1f, _ => 0f };
        }
    }
}
