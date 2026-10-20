using System;
using UnityEngine;

namespace TARS.Locomotion
{
    [RequireComponent(typeof(Rigidbody), typeof(LegLocomotion))]
    public class PhysicsBody : MonoBehaviour
    {
        private const int SolverIterations = 12;
        private const int SolverVelocityIterations = 4;
        private const float MinGravitySqr = 1e-6f;
        private const float ReachTolerance = 1.05f;
        private const float MinAngle = 1e-3f;
        private const float FullHold = 0.5f;
        private const float MaxLead = 0.1f;
        private const float MinInertia = 0.4f;

        [SerializeField, Min(0f)] private float _heightFrequency = 3f;
        [SerializeField, Range(0f, 2f)] private float _heightDamping = 0.8f;
        [SerializeField, Min(0f)] private float _uprightFrequency = 4f;
        [SerializeField, Range(0f, 2f)] private float _uprightDamping = 0.8f;
        [SerializeField, Min(0f)] private float _driveFrequency = 2f;
        [SerializeField, Range(0f, 2f)] private float _driveDamping = 1f;
        [SerializeField, Min(0f)] private float _grip = 0.6f;
        [SerializeField, Min(0f)] private float _supportSmoothing = 1f;

        private Rigidbody _body;
        private LegLocomotion _locomotion;
        private LegRagdoll[] _ragdolls;
        private BlendWeight _strength = BlendWeight.On;
        private Vector3 _moveVelocity;
        private Quaternion _heading = Quaternion.identity;
        private Vector3 _reference;
        private Vector3 _plannedAcceleration;
        private float _support = 1f;
        private float _groundHeight;
        private bool _hasGround;

        public Rigidbody Body => _body;
        public bool IsActive => _strength.IsActive;
        public float Strength => _strength.Value;
        public float Support => Mathf.Clamp01(_support);
        public float Tilt => Vector3.Angle(_body.rotation * Vector3.up, Up());
        public Vector3 PlannedAcceleration => _plannedAcceleration;
        public Quaternion Heading => _heading;
        public Vector3 FramePosition
        {
            get
            {
                Vector3 position = transform.position;
                if (!_hasGround) return position;
                Vector3 up = Up();
                return position + up * (_groundHeight - Vector3.Dot(position, up));
            }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.solverIterations = SolverIterations;
            _body.solverVelocityIterations = SolverVelocityIterations;
            _locomotion = GetComponent<LegLocomotion>();
            ReadOnlySpan<LegRig> legs = _locomotion.Legs;
            _ragdolls = new LegRagdoll[legs.Length];
            for (int i = 0; i < legs.Length; i++)
                legs[i].TryGetComponent(out _ragdolls[i]);
            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                for (int j = i + 1; j < colliders.Length; j++)
                    Physics.IgnoreCollision(colliders[i], colliders[j]);
            }
            _heading = UprightHeading(transform.rotation, Up());
            _reference = transform.position;
        }

        private void Start()
        {
            float legLength = _locomotion.LegLength;
            float inertia = MinInertia * _body.mass * legLength * legLength;
            _body.inertiaTensor = Vector3.Max(_body.inertiaTensor, new Vector3(inertia, inertia, inertia));
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            Vector3 gravity = Physics.gravity;
            Vector3 up = Up();
            _strength.Advance(deltaTime);
            UpdateSupport(up, deltaTime);
            float strength = _strength.Value;
            CollectTargets(up, out Vector3 offset, out Quaternion turn);
            Hold(gravity, up, offset, strength);
            Turn(turn, strength);
            Drive(gravity, up, offset, strength, deltaTime);
        }

        public void Move(Vector3 velocity, Quaternion heading)
        {
            _moveVelocity = velocity;
            _heading = heading;
        }

        public void SetActive(bool active, float blendTime)
        {
            if (active && !_strength.IsActive)
            {
                _hasGround = false;
                _heading = UprightHeading(_body.rotation, Up());
                _reference = _body.position;
            }
            _strength.Set(active, blendTime);
        }

        private void UpdateSupport(Vector3 up, float deltaTime)
        {
            ReadOnlySpan<LegState> states = _locomotion.States;
            ReadOnlySpan<LegRig> legs = _locomotion.Legs;
            if (states.Length == 0) return;
            bool checkReach = _strength.IsOn;
            Vector3 position = _body.position;
            float planted = 0f;
            float height = 0f;
            for (int i = 0; i < states.Length; i++)
            {
                ref readonly LegState state = ref states[i];
                if (state.IsSwinging || !state.OnGround) continue;
                float reach = state.ChainLength * ReachTolerance;
                if (checkReach && (state.SupportAnkle - legs[i].Upper.position).sqrMagnitude > reach * reach) continue;
                float weight = _ragdolls[i] != null ? _ragdolls[i].Weight : 1f;
                planted += weight;
                height += weight * (Vector3.Dot(state.FootPosition - position, up) - state.RestFootLocal.y);
            }
            if (planted > 0f)
            {
                _groundHeight = Vector3.Dot(position, up) + height / planted;
                _hasGround = true;
            }
            float expected = Mathf.Max(states.Length * _locomotion.DutyFactor, 1f);
            float smoothTime = _supportSmoothing * _locomotion.FootfallInterval * _locomotion.CycleDuration;
            float support = planted / expected;
            _support = smoothTime > 0f ? Mathf.Lerp(_support, support, 1f - Mathf.Exp(-deltaTime / smoothTime)) : support;
        }

        private void CollectTargets(Vector3 up, out Vector3 offset, out Quaternion turn)
        {
            offset = Vector3.zero;
            turn = Quaternion.identity;
            ReadOnlySpan<BodyMotion> motions = _locomotion.BodyMotions;
            for (int i = 0; i < motions.Length; i++)
            {
                BodyMotion motion = motions[i];
                if (motion == null || !motion.isActiveAndEnabled) continue;
                float weight = motion.Weight;
                offset += motion.TargetOffset * weight;
                turn *= Quaternion.Slerp(Quaternion.identity, motion.TargetTurn, weight);
            }
            offset = UprightHeading(_body.rotation, up) * offset;
        }

        private void Hold(Vector3 gravity, Vector3 up, Vector3 offset, float strength)
        {
            float support = Mathf.Clamp01(_support / FullHold) * strength;
            if (support <= 0f) return;
            float height = (_hasGround ? _groundHeight : Vector3.Dot(_body.position, up)) + Vector3.Dot(offset, up);
            float omega = 2f * Mathf.PI * _heightFrequency;
            float lift = omega * omega * (height - Vector3.Dot(_body.position, up)) - 2f * _heightDamping * omega * Vector3.Dot(_body.linearVelocity, up);
            _body.AddForce((up * lift - gravity) * support, ForceMode.Acceleration);
        }

        private void Turn(Quaternion turn, float strength)
        {
            if (strength <= 0f) return;
            Quaternion error = _heading * turn * Quaternion.Inverse(_body.rotation);
            if (error.w < 0f)
                error = new Quaternion(-error.x, -error.y, -error.z, -error.w);
            error.ToAngleAxis(out float angle, out Vector3 axis);
            float omega = 2f * Mathf.PI * _uprightFrequency;
            Vector3 torque = -_body.angularVelocity * (2f * _uprightDamping * omega);
            if (angle > MinAngle)
                torque += axis * (angle * Mathf.Deg2Rad * omega * omega);
            _body.AddTorque(torque * strength, ForceMode.Acceleration);
        }

        private void Drive(Vector3 gravity, Vector3 up, Vector3 offset, float strength, float deltaTime)
        {
            Vector3 position = _body.position;
            Vector3 target = Vector3.ProjectOnPlane(_moveVelocity, up);
            float maxLead = MaxLead * _locomotion.LegLength;
            _reference = position + Vector3.ClampMagnitude(Vector3.ProjectOnPlane(_reference + target * deltaTime - position, up), maxLead);
            float maxAcceleration = _grip * gravity.magnitude * Mathf.Clamp01(_support) * strength;
            float omega = 2f * Mathf.PI * _driveFrequency;
            Vector3 error = Vector3.ProjectOnPlane(_reference + offset - position, up);
            Vector3 velocity = Vector3.ProjectOnPlane(_body.linearVelocity, up);
            _plannedAcceleration = Vector3.ClampMagnitude(error * (omega * omega) + (target - velocity) * (2f * _driveDamping * omega), maxAcceleration);
            _body.AddForce(_plannedAcceleration, ForceMode.Acceleration);
        }

        private static Vector3 Up()
        {
            Vector3 gravity = Physics.gravity;
            return gravity.sqrMagnitude > MinGravitySqr ? -gravity.normalized : Vector3.up;
        }

        private static Quaternion UprightHeading(Quaternion rotation, Vector3 up)
        {
            Vector3 forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, up);
            if (forward.sqrMagnitude < MinGravitySqr)
                forward = Vector3.ProjectOnPlane(rotation * Vector3.down, up);
            return forward.sqrMagnitude > MinGravitySqr ? Quaternion.LookRotation(forward, up) : Quaternion.identity;
        }
    }
}
