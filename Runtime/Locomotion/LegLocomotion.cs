using System;
using System.Collections;
using UnityEngine;

namespace TARS.Locomotion
{
    [ExecuteAlways]
    public class LegLocomotion : MonoBehaviour
    {
        private const float GizmoRadius = 0.04f;
        private const float RotationEpsilon = 1e-6f;
        private const float FullLiftStepRatio = 0.5f;
        private const float MaxLateLiftOff = 0.5f;
        private const float PhaseTolerance = 1e-3f;
        private const float StepWaitPoint = 0.4f;
        private const float KickShapePeak = 0.544f;
        private const float KickMinStep = 1e-3f;
        private const int ProbeHitCapacity = 8;
        private const float MinGravitySqr = 1e-6f;
        private const float MaxSupportDuty = 0.95f;
        private const float SupportMargin = 0.02f;
        private const int MinWalkSupport = 3;
        private const float LandingShare = 0.4f;
        private const float MaxLandingLead = 0.25f;
        private const float FlightPushOff = 0.8f;
        private const float SettleAngle = 10f;
        private const float LiftOffSpeed = 0.25f;
        private const float RollFraction = 0.2f;
        private const float OverreachRatio = 1.05f;
        private const float TuckReach = 0.55f;

        [SerializeField] private GaitSettings _gait;
        [SerializeField] private StepStyle _style;
        [SerializeField] private LegRig[] _legs;
        [SerializeField] private BodyMotion[] _bodyMotions = Array.Empty<BodyMotion>();
        [SerializeField, HideInInspector] private HipMotion _hipMotion;
        [SerializeField] private LayerMask _groundMask = ~0;
        [SerializeField, Min(0f)] private float _probeHeight = 1f;
        [SerializeField, Min(0f)] private float _probeDepth = 1f;
        [SerializeField, Min(0f)] private float _velocityLag = 0.16f;

        private static readonly WaitForFixedUpdate AfterPhysicsStep = new WaitForFixedUpdate();

        private RaycastHit[] _probeHits = new RaycastHit[ProbeHitCapacity];
        private LegState[] _states;
        private float _phase;
        private float _cycleDuration;
        private float _dutyFactor;
        private float _speedBlend;
        private int _plantedCount;
        private float _rollWeight;
        private Vector3 _travelDirection = Vector3.forward;
        private float _legLength;
        private float _stanceReach;
        private float _supportDuty;
        private float _pendulumTime;
        private float _footfallInterval;
        private float _planarSpeed;
        private Vector3 _velocity;
        private Vector3 _acceleration;
        private Vector3 _angularVelocity;
        private Vector3 _previousPosition;
        private Quaternion _previousRotation;
        private Vector3 _up;
        private Vector3 _bodyPosition;
        private Quaternion _bodyRotation;
        private Vector3 _stepPosition;
        private Quaternion _stepRotation;
        private int _stepCount;
        private bool _frameDriven;
        private BlendWeight _blend = BlendWeight.On;
        private bool _replant;
        private PhysicsBody _physicsBody;
        private BlendWeight _tuck;

        public float Phase => _phase;
        public float Weight => _blend.Value;
        public float CycleDuration => _cycleDuration;
        public float DutyFactor => _dutyFactor;
        public Vector3 Velocity => _velocity;
        public Vector3 Acceleration => _acceleration;
        public Vector3 AngularVelocity => _angularVelocity;
        public ReadOnlySpan<LegState> States => _states;
        public ReadOnlySpan<LegRig> Legs => _legs;
        public ReadOnlySpan<BodyMotion> BodyMotions => _bodyMotions;
        public GaitSettings Gait => _gait;
        public StepStyle Style => _style;
        public float FootfallInterval => _footfallInterval;
        public Quaternion BodyRotation => _bodyRotation;
        public float LegLength => _legLength;
        public PhysicsBody PhysicsBody => _physicsBody;

        private void OnEnable()
        {
            Initialize();
            if (Application.isPlaying)
                StartCoroutine(SamplePhysicsSteps());
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                SnapToGround();
                return;
            }
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;
            CacheFrame();
            SampleMotion(deltaTime);
            _blend.Advance(deltaTime);
            _tuck.Advance(deltaTime);
            if (_blend.IsOff)
            {
                if (!_tuck.IsOff)
                    PoseTucked();
                return;
            }
            if (_replant)
            {
                _replant = false;
                Replant();
            }
            AdvanceClock(deltaTime);
            _plantedCount = 0;
            for (int i = 0; i < _states.Length; i++)
            {
                if (!_states[i].IsSwinging)
                    _plantedCount++;
            }
            for (int i = 0; i < _legs.Length; i++)
                UpdateLeg(_legs[i], ref _states[i], deltaTime);
            UpdateSupport();
            PoseBody(deltaTime);
        }

        private void OnValidate()
        {
            MigrateBodyMotions();
            if (Application.isPlaying)
            {
                if (_states != null && _legs != null && _legs.Length != _states.Length)
                    Initialize();
                return;
            }
            _states = null;
            transform.hasChanged = true;
        }

        private void OnDrawGizmos()
        {
            if (_states == null) return;
            for (int i = 0; i < _states.Length; i++)
            {
                LegState state = _states[i];
                Gizmos.color = Color.white;
                Gizmos.DrawLine(state.ProbeOrigin, state.TargetPosition);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(state.TargetPosition, GizmoRadius);
                Gizmos.color = state.IsSwinging ? Color.red : Color.green;
                Gizmos.DrawSphere(state.FootPosition, GizmoRadius);
                if (state.IsSwinging)
                    Gizmos.DrawLine(state.LiftOffPosition, state.TargetPosition);
            }
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, _velocity);
        }

        public void SetActive(bool active, float blendTime)
        {
            if (active && _blend.IsOff)
                _replant = true;
            _blend.Set(active, blendTime);
        }

        public void SetTucked(bool tucked, float blendTime)
        {
            _tuck.Set(tucked, blendTime);
        }

        private void Initialize()
        {
            _states = null;
            MigrateBodyMotions();
            TryGetComponent(out _physicsBody);
            if (!Application.isPlaying && !HasEditModeSetup()) return;
            CacheFrame();
            _states = new LegState[_legs.Length];
            _phase = 0f;
            _velocity = Vector3.zero;
            _acceleration = Vector3.zero;
            _angularVelocity = Vector3.zero;
            _previousPosition = _bodyPosition;
            _previousRotation = _bodyRotation;
            _stepCount = 0;
            _frameDriven = false;
            _planarSpeed = 0f;
            _legLength = 0f;
            _stanceReach = 0f;
            _travelDirection = Vector3.forward;
            for (int i = 0; i < _legs.Length; i++)
            {
                LegRig leg = _legs[i];
                ref LegState state = ref _states[i];
                state.RestHipLocal = RestBodyPoint(leg.transform, leg.transform.position);
                state.RestUpperLocal = RestBodyPoint(leg.Upper, leg.Upper.position);
                state.ChainLength = leg.ChainLength;
                state.RestFootLocal = RestBodyPoint(leg.transform, leg.RestFootPosition);
                state.RestForwardLocal = RestBodyDirection(leg.transform, leg.transform.forward);
                state.RestAnkle = Quaternion.Inverse(leg.Lower.rotation) * leg.Foot.rotation;
                state.HipHeight = state.RestHipLocal.y - state.RestFootLocal.y + leg.FootHeight;
                state.PhaseOffset = _gait.PhaseOffset(leg);
                _legLength += state.HipHeight / _legs.Length;
                _stanceReach += Mathf.Max(state.HipHeight, state.ChainLength) / _legs.Length;
            }
            _footfallInterval = 1f / CountFootfalls();
            _supportDuty = SupportDuty();
            UpdateGaitTiming();
            for (int i = 0; i < _legs.Length; i++)
            {
                ref LegState state = ref _states[i];
                state.DutyFactor = _dutyFactor;
                state.PreviousLegPhase = LegPhase(state.PhaseOffset);
                state.WasInSwingWindow = state.PreviousLegPhase >= state.DutyFactor;
                PlantAtRest(_legs[i], ref state);
            }
            UpdateSupport();
            PoseBody(0f);
        }

        private float SupportDuty()
        {
            int support = MinWalkSupport;
            if (support <= 0 || _legs.Length <= support) return 0f;
            float legsPerFootfall = _legs.Length * _footfallInterval;
            return Mathf.Min(Mathf.Ceil(support / legsPerFootfall) * _footfallInterval + SupportMargin, MaxSupportDuty);
        }

        private int CountFootfalls()
        {
            int footfalls = 0;
            for (int i = 0; i < _states.Length; i++)
            {
                bool distinct = true;
                for (int j = 0; j < i && distinct; j++)
                    distinct = Mathf.Abs(Mathf.Repeat(_states[i].PhaseOffset - _states[j].PhaseOffset + 0.5f, 1f) - 0.5f) > PhaseTolerance;
                if (distinct)
                    footfalls++;
            }
            return footfalls;
        }

        private void MigrateBodyMotions()
        {
            if (_hipMotion == null) return;
            if (_bodyMotions == null || _bodyMotions.Length == 0)
                _bodyMotions = new BodyMotion[] { _hipMotion };
            _hipMotion = null;
        }

        private bool HasEditModeSetup()
        {
            if (_gait == null || _style == null || _legs == null) return false;
            for (int i = 0; i < _legs.Length; i++)
            {
                if (_legs[i] == null || !_legs[i].IsValid) return false;
            }
            return true;
        }

        private void SnapToGround()
        {
            if (!transform.hasChanged) return;
            transform.hasChanged = false;
            if (_states == null)
                Initialize();
            if (_states == null) return;
            CacheFrame();
            for (int i = 0; i < _legs.Length; i++)
                PlantAtRest(_legs[i], ref _states[i]);
            UpdateSupport();
            PoseBody(0f);
        }

        private void PlantAtRest(LegRig leg, ref LegState state)
        {
            UpdateTarget(leg, ref state, 0f, 0f, 0f);
            Plant(ref state, state.TargetPosition, state.TargetRotation, state.TargetOnGround);
        }

        private void Replant()
        {
            for (int i = 0; i < _legs.Length; i++)
            {
                LegRig leg = _legs[i];
                ref LegState state = ref _states[i];
                Vector3 foot = leg.Foot.position;
                Vector3 normal = _up;
                float probeHeight = _probeHeight * state.HipHeight;
                bool onGround = ProbeGround(foot + _up * probeHeight, -_up, probeHeight + _probeDepth * state.HipHeight, out RaycastHit hit);
                if (onGround)
                {
                    normal = hit.normal;
                    foot = hit.point + normal * leg.FootHeight;
                }
                Plant(ref state, foot, Quaternion.LookRotation(Vector3.ProjectOnPlane(_bodyRotation * state.RestForwardLocal, normal), normal), onGround);
                state.DutyFactor = _dutyFactor;
                state.PreviousLegPhase = LegPhase(state.PhaseOffset);
                state.WasInSwingWindow = state.PreviousLegPhase >= state.DutyFactor;
            }
        }

        private static void Plant(ref LegState state, Vector3 position, Quaternion rotation, bool onGround)
        {
            state.IsSwinging = false;
            state.OnGround = onGround;
            state.FootPosition = position;
            state.FootRotation = rotation;
            state.FootPitch = 0f;
            state.HeelPitch = 0f;
            state.ReachExhausted = false;
            state.StanceReach = state.ChainLength;
            state.AnklePosition = position;
            state.SupportAnkle = position;
        }

        private void UpdateSupport()
        {
            for (int i = 0; i < _legs.Length; i++)
            {
                ref LegState state = ref _states[i];
                if (state.IsSwinging) continue;
                float allowance = state.HeelLiftAllowance(ToBodyPoint(state.FootPosition), _travelDirection, Alignment(state.FootRotation), _legs[i], _style.Foot.ToeOff);
                state.SupportAnkle = allowance > 0f ? _legs[i].ContactAnklePosition(state.FootPosition, state.FootRotation, Mathf.Min(state.FootPitch, -allowance)) : state.AnklePosition;
            }
        }

        public Vector3 SwingContact(int index, float anticipationStart, out float weight)
        {
            ref readonly LegState state = ref _states[index];
            Vector3 localVelocity = ToBodyDirection(_velocity);
            bool landing = state.SwingProgress >= anticipationStart;
            Vector3 bodyShift = landing ? localVelocity * ((1f - state.SwingProgress) * state.SwingDuration) : -localVelocity * (state.SwingProgress * state.SwingDuration);
            Vector3 contact = landing ? state.LandingAnklePosition : LiftOffSupportAnkle(_legs[index], state, bodyShift);
            float t = landing ? (state.SwingProgress - anticipationStart) / (1f - anticipationStart) : 1f - state.SwingProgress / anticipationStart;
            weight = t * t * (3f - 2f * t);
            return ToBodyPoint(contact) - bodyShift;
        }

        private Vector3 LiftOffSupportAnkle(LegRig leg, in LegState state, Vector3 bodyShift)
        {
            float allowance = state.HeelLiftAllowance(ToBodyPoint(state.LiftOffPosition) - bodyShift, _travelDirection, Alignment(state.LiftOffRotation), leg, _style.Foot.ToeOff);
            return leg.ContactAnklePosition(state.LiftOffPosition, state.LiftOffRotation, Mathf.Min(state.LiftOffPitch, -allowance));
        }

        private void PoseBody(float deltaTime)
        {
            for (int i = 0; i < _bodyMotions.Length; i++)
            {
                if (_bodyMotions[i] != null && _bodyMotions[i].isActiveAndEnabled)
                    _bodyMotions[i].Pose(this, deltaTime);
            }
            float weight = Application.isPlaying ? _blend.Value : 1f;
            for (int i = 0; i < _legs.Length; i++)
            {
                if (_states[i].IsSwinging)
                    KeepInReach(_legs[i], ref _states[i]);
                else
                    LiftHeel(_legs[i], ref _states[i]);
                if (_physicsBody != null && !_states[i].IsSwinging)
                    CatchStep(_legs[i], ref _states[i]);
                PoseLeg(_legs[i], _states[i], weight);
            }
        }

        public Vector3 ToBodyPoint(Vector3 worldPoint)
        {
            return Quaternion.Inverse(_bodyRotation) * (worldPoint - _bodyPosition);
        }

        public Vector3 ToBodyDirection(Vector3 worldDirection)
        {
            return Quaternion.Inverse(_bodyRotation) * worldDirection;
        }

        private void CacheFrame()
        {
            _bodyPosition = _physicsBody != null ? _physicsBody.FramePosition : transform.position;
            Quaternion rotation = transform.rotation;
            Vector3 gravity = Physics.gravity;
            _up = gravity.sqrMagnitude > MinGravitySqr ? -gravity.normalized : rotation * Vector3.up;
            _bodyRotation = _physicsBody != null ? Heading(rotation) : rotation;
        }

        private Quaternion Heading(Quaternion rotation)
        {
            Vector3 forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, _up);
            return forward.sqrMagnitude > RotationEpsilon ? Quaternion.LookRotation(forward, _up) : _bodyRotation;
        }

        private Vector3 RestBodyPoint(Transform owner, Vector3 worldPoint)
        {
            for (int i = 0; i < _bodyMotions.Length; i++)
            {
                if (_bodyMotions[i] != null)
                    worldPoint = _bodyMotions[i].RestWorldPoint(owner, worldPoint);
            }
            return ToBodyPoint(worldPoint);
        }

        private Vector3 RestBodyDirection(Transform owner, Vector3 worldDirection)
        {
            for (int i = 0; i < _bodyMotions.Length; i++)
            {
                if (_bodyMotions[i] != null)
                    worldDirection = _bodyMotions[i].RestWorldDirection(owner, worldDirection);
            }
            return ToBodyDirection(worldDirection);
        }

        private IEnumerator SamplePhysicsSteps()
        {
            while (true)
            {
                yield return AfterPhysicsStep;
                _stepPosition = _physicsBody != null ? _physicsBody.FramePosition : transform.position;
                _stepRotation = _physicsBody != null ? Heading(transform.rotation) : transform.rotation;
                _stepCount++;
            }
        }

        private void SampleMotion(float deltaTime)
        {
            Vector3 position = _bodyPosition;
            Quaternion rotation = _bodyRotation;
            bool stepped = _stepCount > 0;
            bool movedInFrame = stepped ? !position.Equals(_stepPosition) || !rotation.Equals(_stepRotation) : !position.Equals(_previousPosition) || !rotation.Equals(_previousRotation);
            float sampleTime;
            if (movedInFrame)
            {
                sampleTime = deltaTime;
                _frameDriven = true;
            }
            else if (stepped)
            {
                sampleTime = _stepCount * Time.fixedDeltaTime;
                _frameDriven = false;
            }
            else if (_frameDriven)
            {
                sampleTime = deltaTime;
            }
            else
            {
                return;
            }
            _stepCount = 0;
            Vector3 rawVelocity = (position - _previousPosition) / sampleTime;
            Quaternion delta = rotation * Quaternion.Inverse(_previousRotation);
            if (delta.w < 0f)
                delta = new Quaternion(-delta.x, -delta.y, -delta.z, -delta.w);
            Vector3 imaginary = new Vector3(delta.x, delta.y, delta.z);
            float sinHalfAngle = imaginary.magnitude;
            float angleScale = sinHalfAngle > RotationEpsilon ? 2f * Mathf.Atan2(sinHalfAngle, delta.w) / sinHalfAngle : 2f;
            Vector3 rawAngularVelocity = imaginary * (angleScale / sampleTime);
            float blend = 1f - Mathf.Exp(-sampleTime / (_velocityLag * _pendulumTime));
            Vector3 previousVelocity = _velocity;
            _velocity = Vector3.Lerp(_velocity, rawVelocity, blend);
            _acceleration = Vector3.Lerp(_acceleration, _physicsBody != null ? _physicsBody.PlannedAcceleration : (_velocity - previousVelocity) / sampleTime, blend);
            _angularVelocity = Vector3.Lerp(_angularVelocity, rawAngularVelocity, blend);
            _previousPosition = position;
            _previousRotation = rotation;
        }

        private void AdvanceClock(float deltaTime)
        {
            _planarSpeed = Vector3.ProjectOnPlane(_velocity, _up).magnitude;
            UpdateGaitTiming();
            UpdateTravelDirection();
            if (HoldForStep()) return;
            _phase = Mathf.Repeat(_phase + deltaTime / _cycleDuration, 1f);
        }

        private void UpdateTravelDirection()
        {
            Vector3 velocity = ToBodyDirection(_velocity);
            velocity.y = 0f;
            float speed = velocity.magnitude;
            _travelDirection = speed > RotationEpsilon ? Vector3.Slerp(Vector3.forward, velocity / speed, _rollWeight) : Vector3.forward;
        }

        private float Alignment(Quaternion footRotation)
        {
            Vector3 forward = ToBodyDirection(footRotation * Vector3.forward);
            forward.y = 0f;
            float length = forward.magnitude;
            return length > RotationEpsilon ? Vector3.Dot(forward, _travelDirection) / length : 0f;
        }

        private bool HoldForStep()
        {
            int waiting = -1;
            for (int i = 0; i < _states.Length; i++)
            {
                if (_states[i].IsSwinging) return false;
                if (waiting < 0 && _states[i].PreviousLegPhase >= WaitPhase(_states[i].DutyFactor))
                    waiting = i;
            }
            if (waiting < 0) return false;
            _phase = Mathf.Repeat(WaitPhase(_states[waiting].DutyFactor) - _states[waiting].PhaseOffset, 1f);
            return true;
        }

        private static float WaitPhase(float dutyFactor)
        {
            return dutyFactor + StepWaitPoint * (1f - dutyFactor);
        }

        public static float LandingLead(float dutyFactor)
        {
            return Mathf.Min(dutyFactor * LandingShare, MaxLandingLead);
        }

        private void UpdateGaitTiming()
        {
            GaitTiming timing = _gait.Evaluate(_planarSpeed, _legLength, _stanceReach, _supportDuty);
            _pendulumTime = timing.PendulumTime;
            _cycleDuration = timing.CycleDuration;
            _speedBlend = timing.SpeedBlend;
            _dutyFactor = timing.DutyFactor;
            _rollWeight = timing.RollWeight;
        }

        private void UpdateLeg(LegRig leg, ref LegState state, float deltaTime)
        {
            float legPhase = LegPhase(state.PhaseOffset);
            if (!state.IsSwinging || legPhase < state.PreviousLegPhase)
                state.DutyFactor = _dutyFactor;
            state.PreviousLegPhase = legPhase;
            float touchdownTime = (1f - legPhase) * _cycleDuration;
            float landingTime = LandingLead(state.DutyFactor) * _cycleDuration;
            bool inSwingWindow = legPhase >= state.DutyFactor;
            bool canLiftOff = !state.WasInSwingWindow || (legPhase - state.DutyFactor) / (1f - state.DutyFactor) <= MaxLateLiftOff;
            bool pushOff = state.ReachExhausted && (_plantedCount > 1 || state.DutyFactor < _footfallInterval);
            if (!state.IsSwinging && (inSwingWindow && canLiftOff || pushOff))
                TryLiftOff(leg, ref state, legPhase, touchdownTime, landingTime);
            else if (!inSwingWindow && state.WasInSwingWindow && state.IsSwinging)
                Land(ref state);
            state.WasInSwingWindow = inSwingWindow;
            state.SwingProgress = state.IsSwinging ? (legPhase - state.SwingStartPhase) / (1f - state.SwingStartPhase) : 0f;
            state.SwingDuration = (1f - state.SwingStartPhase) * _cycleDuration;
            if (state.IsSwinging)
            {
                Swing(leg, ref state, touchdownTime, landingTime, state.SwingProgress, deltaTime);
            }
            else
            {
                state.FootPitch = inSwingWindow ? 0f : StancePitch(leg, legPhase / state.DutyFactor) * _rollWeight * Alignment(state.FootRotation);
                state.AnklePosition = leg.ContactAnklePosition(state.FootPosition, state.FootRotation, state.FootPitch);
            }
        }

        private void TryLiftOff(LegRig leg, ref LegState state, float legPhase, float touchdownTime, float landingTime)
        {
            UpdateTarget(leg, ref state, touchdownTime, landingTime, 0f);
            float settleDistance = _gait.SettleDistance * state.HipHeight;
            bool displaced = (state.TargetPosition - state.FootPosition).sqrMagnitude > settleDistance * settleDistance;
            bool twisted = leg.RotateFoot && Quaternion.Angle(state.FootRotation, state.TargetRotation) > SettleAngle;
            if (!displaced && !twisted) return;
            state.IsSwinging = true;
            state.ReachExhausted = false;
            _plantedCount--;
            state.SwingStartPhase = legPhase;
            state.LiftOffPosition = state.FootPosition;
            state.LiftOffAnklePosition = state.AnklePosition;
            state.LiftOffRotation = state.FootRotation;
            state.LiftOffPitch = state.FootPitch;
            state.LiftOffAnkle = Quaternion.Inverse(leg.PoseLower) * leg.PoseFoot * Quaternion.Inverse(leg.RestFootRotation);
        }

        private void Swing(LegRig leg, ref LegState state, float touchdownTime, float landingTime, float t, float deltaTime)
        {
            UpdateTarget(leg, ref state, touchdownTime, landingTime, deltaTime);
            float travel = SwingTravel(t, LiftOffSpeed);
            Vector3 step = Vector3.ProjectOnPlane(state.TargetPosition - state.LiftOffPosition, _up);
            float stepDistance = step.magnitude;
            float stepScale = Mathf.Clamp01(stepDistance / (FullLiftStepRatio * state.HipHeight));
            float stepHeight = _style.Height(_speedBlend) * state.HipHeight;
            Vector3 lift = _up * (stepHeight * _style.Arc.Evaluate(travel) * stepScale);
            if (stepDistance > KickMinStep)
                lift += (_up - step / stepDistance) * (_style.Kick(_speedBlend) * state.HipHeight * stepScale * KickShape(t));
            float roll = _rollWeight * Alignment(state.TargetRotation);
            Vector3 landingAnklePosition = leg.ContactAnklePosition(state.TargetPosition, state.TargetRotation, LandingPitch(leg) * roll);
            state.LandingAnklePosition = landingAnklePosition;
            state.FootPosition = Vector3.LerpUnclamped(state.LiftOffPosition, state.TargetPosition, travel) + lift;
            state.AnklePosition = Vector3.LerpUnclamped(state.LiftOffAnklePosition, landingAnklePosition, travel) + lift;
            state.FootRotation = Quaternion.Slerp(state.LiftOffRotation, state.TargetRotation, travel);
            float pitchRecovery = (1f - travel) * (1f - travel);
            state.FootPitch = ArcPitch(travel) * roll + (state.LiftOffPitch - LiftOffArcPitch() * roll) * pitchRecovery;
            if (leg.FootType == FootType.Digitigrade)
                state.FootPitch = Mathf.Min(state.FootPitch, 0f);
        }

        private void Land(ref LegState state)
        {
            state.IsSwinging = false;
            state.HeelPitch = 0f;
            state.ReachExhausted = false;
            _plantedCount++;
            state.FootPosition = state.TargetPosition;
            state.FootRotation = state.TargetRotation;
            state.OnGround = state.TargetOnGround;
        }

        private void PoseLeg(LegRig leg, in LegState state, float weight)
        {
            float tuck = _tuck.Value;
            leg.Solve(tuck > 0f ? Vector3.Lerp(state.AnklePosition, TuckPoint(state), tuck) : state.AnklePosition);
            if (leg.RotateFoot)
            {
                Quaternion foot = LegRig.PitchedRotation(state.FootRotation, state.FootPitch);
                if (state.IsSwinging)
                    foot = Quaternion.Slerp(leg.PoseLower * state.LiftOffAnkle, foot, Smootherstep(state.SwingProgress));
                foot = leg.LimitAnkle(foot) * leg.RestFootRotation;
                leg.SetPoseFoot(tuck > 0f ? Quaternion.Slerp(foot, leg.PoseLower * state.RestAnkle, tuck) : foot);
            }
            if (weight < 1f)
                leg.BlendPose(weight);
            if (!leg.Physical)
                leg.WritePose();
        }

        private void PoseTucked()
        {
            for (int i = 0; i < _legs.Length; i++)
            {
                LegRig leg = _legs[i];
                leg.Solve(TuckPoint(_states[i]));
                if (leg.RotateFoot)
                    leg.SetPoseFoot(leg.PoseLower * _states[i].RestAnkle);
                if (!leg.Physical)
                    leg.WritePose();
            }
        }

        private Vector3 TuckPoint(in LegState state)
        {
            return transform.TransformPoint(Vector3.Lerp(state.RestUpperLocal, state.RestFootLocal, TuckReach));
        }

        private void LiftHeel(LegRig leg, ref LegState state)
        {
            float reach = state.HeelPitch < 0f ? Mathf.Min(state.ChainLength, state.StanceReach) : state.ChainLength;
            float exhaustShare = state.DutyFactor < _footfallInterval ? FlightPushOff : 1f;
            float pitch = Mathf.Min(HeelOffPitch(leg, state, reach, exhaustShare, out state.ReachExhausted), state.HeelPitch);
            state.HeelPitch = pitch;
            if (pitch < 0f && pitch < state.FootPitch)
            {
                state.FootPitch = pitch;
                state.AnklePosition = leg.ContactAnklePosition(state.FootPosition, state.FootRotation, pitch);
            }
            state.StanceReach = state.HeelPitch < 0f ? Mathf.Min(state.StanceReach, Vector3.Distance(leg.Upper.position, state.AnklePosition)) : state.ChainLength;
        }

        private void KeepInReach(LegRig leg, ref LegState state)
        {
            Vector3 toAnkle = state.AnklePosition - leg.Upper.position;
            float vertical = Vector3.Dot(toAnkle, _up);
            float distanceSqr = toAnkle.sqrMagnitude;
            float reachSqr = state.ChainLength * state.ChainLength;
            float horizontalSqr = distanceSqr - vertical * vertical;
            if (vertical >= 0f || distanceSqr <= reachSqr || horizontalSqr >= reachSqr) return;
            Vector3 raise = _up * (-Mathf.Sqrt(reachSqr - horizontalSqr) - vertical);
            state.AnklePosition += raise;
            state.FootPosition += raise;
        }

        private static void CatchStep(LegRig leg, ref LegState state)
        {
            float reach = state.ChainLength * OverreachRatio;
            if ((state.AnklePosition - leg.Upper.position).sqrMagnitude > reach * reach)
                state.ReachExhausted = true;
        }

        private float HeelOffPitch(LegRig leg, in LegState state, float reach, float exhaustShare, out bool exhausted)
        {
            exhausted = false;
            if (!leg.RotateFoot) return 0f;
            float footHeight = leg.FootHeight;
            float toeLength = leg.ToeLength;
            Vector3 toe = state.FootPosition + state.FootRotation * new Vector3(0f, -footHeight, toeLength);
            float allowance = state.HeelLiftAllowance(ToBodyPoint(state.FootPosition), _travelDirection, Alignment(state.FootRotation), leg, _style.Foot.ToeOff);
            if (allowance <= 0f) return 0f;
            Vector3 toHip = Quaternion.Inverse(state.FootRotation) * (leg.Upper.position - toe);
            float target = (footHeight * footHeight + toeLength * toeLength + toHip.sqrMagnitude - reach * reach) * 0.5f;
            float cosine = toHip.y * footHeight - toHip.z * toeLength;
            if (cosine >= target) return 0f;
            float sine = toHip.y * toeLength + toHip.z * footHeight;
            float best = Mathf.Atan2(sine, cosine);
            float angle = (best - Mathf.Acos(Mathf.Min(target / Mathf.Sqrt(cosine * cosine + sine * sine), 1f))) * Mathf.Rad2Deg;
            exhausted = allowance >= _style.Foot.ToeOff && angle > allowance * exhaustShare;
            return -Mathf.Clamp(angle, 0f, allowance);
        }

        private void UpdateTarget(LegRig leg, ref LegState state, float touchdownTime, float landingTime, float deltaTime)
        {
            PredictBody(touchdownTime, out Vector3 position, out Vector3 velocity);
            Quaternion predictedTurn = Quaternion.AngleAxis(_angularVelocity.magnitude * (touchdownTime + landingTime) * Mathf.Rad2Deg, _angularVelocity);
            Vector3 home = position + velocity * landingTime + predictedTurn * (_bodyRotation * state.RestFootLocal);
            if (deltaTime > 0f)
                home = Vector3.SmoothDamp(state.Placement, home, ref state.PlacementVelocity, _gait.SwingSmoothing * _pendulumTime, Mathf.Infinity, deltaTime);
            else
                state.PlacementVelocity = Vector3.zero;
            state.Placement = home;
            Vector3 normal = _up;
            float probeHeight = _probeHeight * state.HipHeight;
            state.ProbeOrigin = home + _up * probeHeight;
            state.TargetOnGround = ProbeGround(state.ProbeOrigin, -_up, probeHeight + _probeDepth * state.HipHeight, out RaycastHit hit);
            if (state.TargetOnGround)
            {
                normal = hit.normal;
                state.TargetPosition = hit.point + normal * leg.FootHeight;
            }
            else
            {
                state.TargetPosition = home;
            }
            state.TargetRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(predictedTurn * (_bodyRotation * state.RestForwardLocal), normal), normal);
        }

        private void PredictBody(float time, out Vector3 position, out Vector3 velocity)
        {
            position = _bodyPosition + _velocity * time;
            velocity = _velocity;
            Vector3 planarVelocity = Vector3.ProjectOnPlane(_velocity, _up);
            float speed = planarVelocity.magnitude;
            if (speed <= RotationEpsilon) return;
            Vector3 heading = planarVelocity / speed;
            Vector3 acceleration = Vector3.ProjectOnPlane(_acceleration, _up);
            float tangential = Vector3.Dot(acceleration, heading);
            Accelerate(heading * Mathf.Min(tangential, 0f), speed, time, ref position, ref velocity);
            Accelerate(acceleration - heading * tangential, speed, time, ref position, ref velocity);
        }

        private static void Accelerate(Vector3 acceleration, float speed, float time, ref Vector3 position, ref Vector3 velocity)
        {
            float magnitude = acceleration.magnitude;
            if (magnitude <= 0f) return;
            float duration = Mathf.Min(time, speed / magnitude);
            velocity += acceleration * duration;
            position += acceleration * (duration * (time - 0.5f * duration));
        }

        private bool ProbeGround(Vector3 origin, Vector3 direction, float distance, out RaycastHit groundHit)
        {
            int count = Physics.RaycastNonAlloc(origin, direction, _probeHits, distance, _groundMask, QueryTriggerInteraction.Ignore);
            while (count == _probeHits.Length)
            {
                _probeHits = new RaycastHit[_probeHits.Length * 2];
                count = Physics.RaycastNonAlloc(origin, direction, _probeHits, distance, _groundMask, QueryTriggerInteraction.Ignore);
            }
            groundHit = default;
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _probeHits[i];
                if (hit.distance >= nearest || hit.collider.transform.IsChildOf(transform)) continue;
                nearest = hit.distance;
                groundHit = hit;
            }
            return nearest < float.MaxValue;
        }

        private float ArcPitch(float travel)
        {
            return -_style.Foot.HeelStrike * _style.ArcTilt(travel);
        }

        private float LiftOffArcPitch()
        {
            return -_style.Foot.HeelStrike * _style.ArcStartTilt;
        }

        private float LandingPitch(LegRig leg)
        {
            float pitch = -_style.Foot.HeelStrike * _style.ArcEndTilt;
            return leg.FootType == FootType.Digitigrade ? Mathf.Min(pitch, 0f) : pitch;
        }

        private float StancePitch(LegRig leg, float stanceProgress)
        {
            float roll = RollFraction;
            if (stanceProgress < roll)
                return Mathf.Lerp(LandingPitch(leg), 0f, Smootherstep(stanceProgress / roll));
            if (stanceProgress > 1f - roll)
            {
                float toeOff = (stanceProgress - (1f - roll)) / roll;
                return -_style.Foot.ToeOff * toeOff * toeOff;
            }
            return 0f;
        }

        private float LegPhase(float phaseOffset)
        {
            return Mathf.Repeat(_phase + phaseOffset, 1f);
        }

        private static float KickShape(float t)
        {
            float rise = Mathf.Sin(Mathf.PI * t);
            return rise * rise * (1f - t) / KickShapePeak;
        }

        private static float SwingTravel(float t, float liftOffSpeed)
        {
            return t * (liftOffSpeed + t * (3f - 2f * liftOffSpeed + t * (liftOffSpeed - 2f)));
        }

        private static float Smootherstep(float t)
        {
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }
    }
}
