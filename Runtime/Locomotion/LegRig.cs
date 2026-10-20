using System;
using UnityEngine;

namespace TARS.Locomotion
{
    public class LegRig : MonoBehaviour
    {
        private const float MaxSoftness = 0.1f;
        private const int MinBones = 3;
        private const float NeutralAnkle = 90f;
        private const float FullTurn = 360f;
        private const float GroundProbeLift = 0.05f;
        private const float GroundProbeDepth = 1f;
        private const float PlantigradeDorsiflexion = 25f;
        private const float PlantigradePlantarflexion = 50f;
        private const float DigitigradeDorsiflexion = 15f;
        private const float DigitigradePlantarflexion = 30f;

        [SerializeField] private Transform[] _bones = Array.Empty<Transform>();
        [SerializeField] private Transform _pole;
        [SerializeField] private LegSide _side;
        [SerializeField, Min(0)] private int _segment;
        [SerializeField] private bool _overridePhase;
        [SerializeField, Range(0f, 1f)] private float _phaseOffset;
        [SerializeField] private FootType _footType = FootType.Plantigrade;
        [SerializeField, Min(0f)] private float _footHeight;
        [SerializeField, Min(0f)] private float _toeLength;
        [SerializeField, Range(0f, 90f)] private float _dorsiflexion = PlantigradeDorsiflexion;
        [SerializeField, Range(0f, 90f)] private float _plantarflexion = PlantigradePlantarflexion;
        [SerializeField] private float[] _bendWeights = Array.Empty<float>();
        [SerializeField, Range(0f, MaxSoftness)] private float _softness = 0.01f;
        [SerializeField, HideInInspector] private Vector3 _restFootPosition;
        [SerializeField, HideInInspector] private Quaternion _restFootRotation = Quaternion.identity;
        [SerializeField, HideInInspector] private Vector3[] _segmentAxes = Array.Empty<Vector3>();
        [SerializeField, HideInInspector] private Vector3[] _segmentNormals = Array.Empty<Vector3>();
        [SerializeField, HideInInspector] private FootType _ankleFootType;

        [NonSerialized] private float[] _lengths;
        [NonSerialized] private float _chainLength = -1f;
        [NonSerialized] private float _minReach;
        [NonSerialized] private float _curlLimit;
        [NonSerialized] private float _curl;
        [NonSerialized] private Quaternion[] _poseRotations;
        [NonSerialized] private Vector3[] _posePositions;

        public ReadOnlySpan<Transform> Bones => _bones;
        public Transform Upper => _bones[0];
        public Transform Lower => _bones[_bones.Length - 2];
        public Transform Foot => _bones[_bones.Length - 1];
        public Transform Pole => _pole;
        public LegSide Side => _side;
        public int Segment => _segment;
        public bool OverridePhase => _overridePhase;
        public float PhaseOffset => _phaseOffset;
        public FootType FootType => _footType;
        public bool RotateFoot => _footType != FootType.Point;
        public float FootHeight => _footHeight;
        public float ToeLength => _toeLength;
        public float Softness => _softness;
        public bool Physical { get; set; }
        public Quaternion PoseLower => _poseRotations[_bones.Length - 2];
        public Quaternion PoseFoot => _poseRotations[_bones.Length - 1];
        public ReadOnlySpan<Quaternion> PoseRotations => _poseRotations;
        public float Dorsiflexion => _dorsiflexion;
        public float Plantarflexion => _plantarflexion;
        public ReadOnlySpan<float> BendWeights => _bendWeights;
        public ReadOnlySpan<Vector3> SegmentAxes => _segmentAxes;
        public ReadOnlySpan<Vector3> SegmentNormals => _segmentNormals;
        public Vector3 RestFootPosition => transform.TransformPoint(_restFootPosition);
        public Quaternion RestFootRotation => _restFootRotation;
        public float ChainLength
        {
            get
            {
                if (_chainLength < 0f)
                    MeasureBones();
                return _chainLength;
            }
        }
        public bool IsValid => HasChain && _segmentAxes.Length == _bones.Length - 1 && _segmentNormals.Length == _bones.Length - 1;
        private bool HasChain
        {
            get
            {
                if (_pole == null || _bones == null || _bones.Length < MinBones) return false;
                for (int i = 0; i < _bones.Length; i++)
                {
                    if (_bones[i] == null) return false;
                }
                return true;
            }
        }

        private void OnValidate()
        {
            _chainLength = -1f;
            if (_footType == _ankleFootType) return;
            _ankleFootType = _footType;
            _dorsiflexion = _footType == FootType.Digitigrade ? DigitigradeDorsiflexion : PlantigradeDorsiflexion;
            _plantarflexion = _footType == FootType.Digitigrade ? DigitigradePlantarflexion : PlantigradePlantarflexion;
        }

        private void Reset()
        {
            if (HasChain)
                CaptureRestPose();
        }

        [ContextMenu("Capture Rest Pose")]
        public void CaptureRestPose()
        {
            _restFootPosition = transform.InverseTransformPoint(Foot.position);
            _restFootRotation = Quaternion.Inverse(transform.rotation) * Foot.rotation;
            int segments = _bones.Length - 1;
            _segmentAxes = new Vector3[segments];
            _segmentNormals = new Vector3[segments];
            Span<float> restBends = stackalloc float[segments - 1];
            LegIK.Capture(_bones, _pole.position, _segmentAxes, _segmentNormals, restBends);
            if (segments > 2 && _bendWeights.Length != restBends.Length)
                _bendWeights = BendWeightsFrom(restBends);
            if (_footHeight <= 0f)
                _footHeight = MeasureFootHeight();
            if (_toeLength <= 0f && RotateFoot && Foot.childCount > 0)
                _toeLength = Mathf.Max(Vector3.Dot(Foot.GetChild(0).position - Foot.position, transform.forward), 0f);
            MeasureBones();
        }

        public void Solve(Vector3 target)
        {
            if (_chainLength < 0f)
                MeasureBones();
            int foot = _bones.Length - 1;
            _curl = LegIK.Solve(_bones[0].position, _lengths, _segmentAxes, _segmentNormals, _bendWeights, _chainLength, _minReach, _curlLimit, _curl, target, _pole.position, _softness, _poseRotations, _posePositions);
            _poseRotations[foot] = _poseRotations[foot - 1] * _bones[foot].localRotation;
        }

        public void SetPoseFoot(Quaternion rotation)
        {
            _poseRotations[_bones.Length - 1] = rotation;
        }

        public void BlendPose(float weight)
        {
            Quaternion parent = _bones[0].parent != null ? _bones[0].parent.rotation : Quaternion.identity;
            Quaternion targetParent = parent;
            for (int i = 0; i < _bones.Length; i++)
            {
                Quaternion target = _poseRotations[i];
                Quaternion local = Quaternion.Slerp(_bones[i].localRotation, Quaternion.Inverse(targetParent) * target, weight);
                targetParent = target;
                parent *= local;
                _poseRotations[i] = parent;
            }
        }

        public void WritePose()
        {
            for (int i = 0; i < _bones.Length; i++)
                _bones[i].rotation = _poseRotations[i];
        }

        public Vector3 ContactAnklePosition(Vector3 flatPosition, Quaternion rotation, float pitch)
        {
            if (!RotateFoot || pitch >= 0f) return flatPosition;
            Vector3 pivot = new Vector3(0f, -_footHeight, _toeLength);
            return flatPosition + rotation * pivot - PitchedRotation(rotation, pitch) * pivot;
        }

        public Quaternion LimitAnkle(Quaternion footRotation)
        {
            int foot = _bones.Length - 1;
            Vector3 toKnee = Quaternion.Inverse(footRotation) * (_posePositions[foot - 1] - _posePositions[foot]);
            float angle = Mathf.Atan2(toKnee.y, toKnee.z) * Mathf.Rad2Deg;
            if (angle < -NeutralAnkle)
                angle += FullTurn;
            float limited = Mathf.Clamp(angle, NeutralAnkle - _dorsiflexion, NeutralAnkle + _plantarflexion);
            return limited == angle ? footRotation : footRotation * Quaternion.AngleAxis(limited - angle, Vector3.right);
        }

        public static Quaternion PitchedRotation(Quaternion rotation, float pitch)
        {
            return rotation * Quaternion.AngleAxis(-pitch, Vector3.right);
        }

        private static float[] BendWeightsFrom(ReadOnlySpan<float> restBends)
        {
            float largest = 0f;
            for (int i = 0; i < restBends.Length; i++)
                largest = Mathf.Max(largest, Mathf.Abs(restBends[i]));
            float[] weights = new float[restBends.Length];
            for (int i = 0; i < weights.Length; i++)
                weights[i] = largest > 0f ? restBends[i] / largest : 1f;
            return weights;
        }

        private float MeasureFootHeight()
        {
            Vector3 up = transform.root.up;
            RaycastHit[] hits = Physics.RaycastAll(Foot.position + up * GroundProbeLift, -up, GroundProbeLift + GroundProbeDepth, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float height = 0f;
            float nearest = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].distance >= nearest || hits[i].collider.transform.IsChildOf(transform.root)) continue;
                nearest = hits[i].distance;
                height = Mathf.Max(nearest - GroundProbeLift, 0f);
            }
            return height;
        }

        private void MeasureBones()
        {
            int segments = _bones.Length - 1;
            if (_poseRotations == null || _poseRotations.Length != _bones.Length)
            {
                _poseRotations = new Quaternion[_bones.Length];
                _posePositions = new Vector3[_bones.Length];
                for (int i = 0; i < _bones.Length; i++)
                {
                    _poseRotations[i] = _bones[i].rotation;
                    _posePositions[i] = _bones[i].position;
                }
            }
            if (_lengths == null || _lengths.Length != segments)
                _lengths = new float[segments];
            _chainLength = 0f;
            for (int i = 0; i < segments; i++)
            {
                _lengths[i] = Vector3.Distance(_bones[i].position, _bones[i + 1].position);
                _chainLength += _lengths[i];
            }
            _curl = 0f;
            if (segments == 2)
            {
                _minReach = Mathf.Abs(_lengths[0] - _lengths[1]);
                _curlLimit = 0f;
                return;
            }
            LegIK.MeasureCurl(_lengths, _bendWeights, _chainLength, out _curlLimit, out _minReach);
        }
    }
}
