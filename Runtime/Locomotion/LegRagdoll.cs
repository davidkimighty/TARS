using System;
using System.Collections;
using UnityEngine;

namespace TARS.Locomotion
{
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(LegRig))]
    public class LegRagdoll : MonoBehaviour
    {
        public enum RagdollMode
        {
            Driven,
            Kinematic
        }

        private const int SolverIterations = 12;
        private const int SolverVelocityIterations = 4;
        private const float MaxBend = 150f;
        private const float HipBend = 90f;
        private const float HipSwing = 45f;
        private const float HipTwist = 30f;
        private const float AnkleTwist = 25f;
        private const float MaxLimit = 177f;
        private const float NeutralAnkle = 90f;
        private const float FullTurn = 360f;
        private const float HitDeadZone = 0.1f;
        private const float JointSign = -1f;
        private const float MinFootShare = 0.1f;
        private const float MinAngle = 1e-3f;

        [SerializeField] private RagdollMode _mode;
        [SerializeField, Range(0f, 1f)] private float _massShare = 0.1f;
        [SerializeField, Min(0f)] private float _frequency = 8f;
        [SerializeField, Range(0f, 2f)] private float _damping = 1f;
        [SerializeField, Min(0f)] private float _hitSpeed = 1f;
        [SerializeField, Min(0f)] private float _recoverTime = 0.5f;

        private static double _syncedStep = -1d;

        private LegRig _leg;
        private PhysicsBody _body;
        private Rigidbody _root;
        private Rigidbody[] _bodies;
        private ConfigurableJoint[] _joints;
        private Quaternion[] _startLocal;
        private Quaternion[] _jointFrames;
        private Quaternion[] _locals;
        private Quaternion[] _targets;
        private Vector3[] _targetSpins;
        private Vector3[] _restPositions;
        private Quaternion[] _boneLocals;
        private Vector3[] _posePositions;
        private Quaternion[] _poseRotations;
        private Vector3[] _poseVelocities;
        private Vector3[] _poseSpins;
        private Vector3[] _velocities;
        private Vector3[] _spins;
        private readonly WaitForFixedUpdate _afterStep = new WaitForFixedUpdate();
        private bool _hasTargets;
        private bool _limp;
        private float _hitWeight = 1f;
        private float _driveWeight = -1f;

        public float Weight => _body != null ? Mathf.Min(_hitWeight, _body.Strength) : _hitWeight;

        private void Awake()
        {
            _leg = GetComponent<LegRig>();
            _body = GetComponentInParent<PhysicsBody>();
            ReadOnlySpan<Transform> bones = _leg.Bones;
            int count = bones.Length;
            _bodies = new Rigidbody[count];
            _joints = new ConfigurableJoint[count];
            _startLocal = new Quaternion[count];
            _jointFrames = new Quaternion[count];
            _locals = new Quaternion[count];
            _targets = new Quaternion[count];
            _targetSpins = new Vector3[count];
            _restPositions = new Vector3[count];
            _boneLocals = new Quaternion[count];
            _posePositions = new Vector3[count];
            _poseRotations = new Quaternion[count];
            _poseVelocities = new Vector3[count];
            _poseSpins = new Vector3[count];
            _velocities = new Vector3[count];
            _spins = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                _restPositions[i] = bones[i].localPosition;
                if (!bones[i].TryGetComponent(out _bodies[i]) || !bones[i].TryGetComponent(out _joints[i]))
                {
                    enabled = false;
                    return;
                }
                if (bones[i].TryGetComponent(out LimbContact contact))
                    contact.Bind(this);
            }
            if (_body == null || !_body.TryGetComponent(out _root))
            {
                enabled = false;
                return;
            }
            for (int i = 0; i < count; i++)
                Configure(i);
            if (_mode == RagdollMode.Kinematic)
                SetLimp(false);
            else
                _leg.Physical = true;
        }

        private void OnEnable()
        {
            if (_mode == RagdollMode.Kinematic)
                StartCoroutine(RestoreAfterStep());
        }

        private void LateUpdate()
        {
            if (_mode == RagdollMode.Kinematic)
            {
                if (!_limp)
                    CapturePose(Time.deltaTime);
                return;
            }
            ReadOnlySpan<Quaternion> pose = _leg.PoseRotations;
            if (pose.Length != _joints.Length) return;
            float deltaTime = Time.deltaTime;
            Quaternion parent = _root.transform.rotation;
            for (int i = 0; i < _joints.Length; i++)
            {
                Quaternion local = Quaternion.Inverse(parent) * pose[i];
                parent = pose[i];
                Quaternion frame = _jointFrames[i];
                Quaternion inverseFrame = Quaternion.Inverse(frame);
                _targets[i] = inverseFrame * Quaternion.Inverse(local) * _startLocal[i] * frame;
                Vector3 spin = _hasTargets && deltaTime > 0f ? AngularVelocity(_locals[i], local, deltaTime) : Vector3.zero;
                _targetSpins[i] = -(inverseFrame * (Quaternion.Inverse(_startLocal[i]) * spin));
                _locals[i] = local;
            }
            _hasTargets = true;
        }

        private void FixedUpdate()
        {
            if (_mode == RagdollMode.Kinematic)
            {
                Follow(Time.fixedDeltaTime);
                return;
            }
            if (!_hasTargets) return;
            _hitWeight = _recoverTime > 0f ? Mathf.MoveTowards(_hitWeight, 1f, Time.fixedDeltaTime / _recoverTime) : 1f;
            float weight = Weight;
            if (weight != _driveWeight)
            {
                _driveWeight = weight;
                float omega = 2f * Mathf.PI * _frequency;
                JointDrive drive = new JointDrive { positionSpring = weight * omega * omega, positionDamper = 2f * _damping * omega * Mathf.Sqrt(weight), maximumForce = float.MaxValue, useAcceleration = true };
                for (int i = 0; i < _joints.Length; i++)
                    _joints[i].slerpDrive = drive;
            }
            for (int i = 0; i < _joints.Length; i++)
            {
                ConfigurableJoint joint = _joints[i];
                joint.targetRotation = _targets[i];
                joint.targetAngularVelocity = _targetSpins[i] * weight;
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }

        private void OnValidate()
        {
            _driveWeight = -1f;
        }

        public void Hit(Collision collision)
        {
            Rigidbody other = collision.rigidbody;
            if (other == null || _hitSpeed <= 0f || _root == null || _mode == RagdollMode.Kinematic) return;
            float speed = collision.relativeVelocity.magnitude * other.mass / (other.mass + _root.mass);
            if (speed < _hitSpeed * HitDeadZone) return;
            _hitWeight = Mathf.Min(_hitWeight, 1f - Mathf.Clamp01(speed / _hitSpeed));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSyncedStep()
        {
            _syncedStep = -1d;
        }

        [ContextMenu("Build Ragdoll")]
        public void Build()
        {
            LegRig leg = GetComponent<LegRig>();
            PhysicsBody owner = GetComponentInParent<PhysicsBody>();
            if (owner == null || !owner.TryGetComponent(out Rigidbody root) || !leg.IsValid) return;
            ReadOnlySpan<Transform> bones = leg.Bones;
            float chainLength = leg.ChainLength;
            float footLength = Mathf.Max(leg.ToeLength, chainLength * MinFootShare);
            float massPerLength = _massShare * root.mass / (chainLength + footLength);
            Rigidbody parent = root;
            for (int i = 0; i < bones.Length; i++)
            {
                Transform bone = bones[i];
                Transform next = i < bones.Length - 1 ? bones[i + 1] : null;
                Rigidbody body = GetOrAdd<Rigidbody>(bone.gameObject);
                body.mass = massPerLength * (next != null ? Vector3.Distance(bone.position, next.position) : footLength);
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                body.solverIterations = SolverIterations;
                body.solverVelocityIterations = SolverVelocityIterations;
                GetOrAdd<ConfigurableJoint>(bone.gameObject).connectedBody = parent;
                GetOrAdd<LimbContact>(bone.gameObject);
                foreach (Transform child in bone)
                {
                    if (child != next && child.TryGetComponent(out MeshFilter mesh) && !child.TryGetComponent(out Collider _))
                        AddShape(child.gameObject, mesh.sharedMesh);
                }
                parent = body;
            }
        }

        private void Follow(float deltaTime)
        {
            bool limp = !_body.IsActive;
            if (limp != _limp)
                SetLimp(limp);
            if (limp || !_hasTargets || deltaTime <= 0f) return;
            if (_syncedStep != Time.fixedTimeAsDouble)
            {
                _syncedStep = Time.fixedTimeAsDouble;
                Physics.SyncTransforms();
            }
            Vector3 origin = _root.position;
            Quaternion rotation = _root.rotation;
            Vector3 nextOrigin = origin + _root.linearVelocity * deltaTime;
            Quaternion nextRotation = Rotation(_root.angularVelocity * deltaTime) * rotation;
            for (int i = 0; i < _bodies.Length; i++)
            {
                Vector3 position = origin + rotation * _posePositions[i];
                Quaternion boneRotation = rotation * _poseRotations[i];
                Vector3 nextPosition = nextOrigin + nextRotation * (_posePositions[i] + _poseVelocities[i] * deltaTime);
                Quaternion nextBoneRotation = nextRotation * (Rotation(_poseSpins[i] * deltaTime) * _poseRotations[i]);
                Rigidbody body = _bodies[i];
                body.position = position;
                body.rotation = boneRotation;
                body.MovePosition(nextPosition);
                body.MoveRotation(nextBoneRotation);
                _velocities[i] = (nextPosition - position) / deltaTime;
                _spins[i] = AngularVelocity(boneRotation, nextBoneRotation, deltaTime);
            }
        }

        private void CapturePose(float deltaTime)
        {
            ReadOnlySpan<Transform> bones = _leg.Bones;
            Transform root = _root.transform;
            Quaternion inverse = Quaternion.Inverse(root.rotation);
            Vector3 origin = root.position;
            bool sampled = _hasTargets && deltaTime > 0f;
            for (int i = 0; i < bones.Length; i++)
            {
                Transform bone = bones[i];
                Vector3 position = inverse * (bone.position - origin);
                Quaternion rotation = inverse * bone.rotation;
                _poseVelocities[i] = sampled ? (position - _posePositions[i]) / deltaTime : Vector3.zero;
                _poseSpins[i] = sampled ? AngularVelocity(_poseRotations[i], rotation, deltaTime) : Vector3.zero;
                _posePositions[i] = position;
                _poseRotations[i] = rotation;
                _boneLocals[i] = bone.localRotation;
            }
            _hasTargets = true;
        }

        private void SetLimp(bool limp)
        {
            _limp = limp;
            _leg.Physical = limp;
            ReadOnlySpan<Transform> bones = _leg.Bones;
            for (int i = 0; i < _bodies.Length; i++)
            {
                Rigidbody body = _bodies[i];
                body.isKinematic = !limp;
                body.interpolation = limp ? RigidbodyInterpolation.Interpolate : RigidbodyInterpolation.None;
                if (limp)
                {
                    body.linearVelocity = _velocities[i];
                    body.angularVelocity = _spins[i];
                }
                else
                    bones[i].localPosition = _restPositions[i];
            }
            ConfigurableJoint hip = _joints[0];
            ConfigurableJointMotion linear = limp ? ConfigurableJointMotion.Locked : ConfigurableJointMotion.Free;
            ConfigurableJointMotion angular = limp ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
            hip.xMotion = linear;
            hip.yMotion = linear;
            hip.zMotion = linear;
            hip.angularXMotion = angular;
            hip.angularYMotion = angular;
            hip.angularZMotion = angular;
            if (limp) return;
            _hasTargets = false;
            CapturePose(0f);
        }

        private IEnumerator RestoreAfterStep()
        {
            while (true)
            {
                yield return _afterStep;
                if (_limp || !_hasTargets) continue;
                ReadOnlySpan<Transform> bones = _leg.Bones;
                for (int i = 0; i < bones.Length; i++)
                    bones[i].SetLocalPositionAndRotation(_restPositions[i], _boneLocals[i]);
            }
        }

        private void Configure(int index)
        {
            ReadOnlySpan<Transform> bones = _leg.Bones;
            int last = bones.Length - 1;
            bool foot = index == last;
            Vector3 axis = foot ? Quaternion.Inverse(_leg.RestFootRotation) * Vector3.right : _leg.SegmentNormals[index];
            Vector3 secondary = foot ? Quaternion.Inverse(_leg.RestFootRotation) * Vector3.up : _leg.SegmentAxes[index];
            Rigidbody parent = index == 0 ? _root : _bodies[index - 1];
            ConfigurableJoint joint = _joints[index];
            joint.autoConfigureConnectedAnchor = true;
            joint.anchor = Vector3.zero;
            joint.axis = axis;
            joint.secondaryAxis = secondary;
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.rotationDriveMode = RotationDriveMode.Slerp;
            joint.enableCollision = false;
            if (index == 0)
            {
                joint.angularYMotion = ConfigurableJointMotion.Limited;
                joint.angularZMotion = ConfigurableJointMotion.Limited;
                joint.lowAngularXLimit = new SoftJointLimit { limit = -HipBend };
                joint.highAngularXLimit = new SoftJointLimit { limit = HipBend };
                joint.angularYLimit = new SoftJointLimit { limit = HipSwing };
                joint.angularZLimit = new SoftJointLimit { limit = HipTwist };
            }
            else if (!foot)
            {
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Locked;
                Vector2 range = KneeRange(index);
                SetBendLimit(joint, range.x, range.y);
            }
            else if (_leg.RotateFoot)
            {
                joint.angularYMotion = ConfigurableJointMotion.Limited;
                joint.angularZMotion = ConfigurableJointMotion.Limited;
                joint.angularYLimit = new SoftJointLimit { limit = AnkleTwist };
                joint.angularZLimit = new SoftJointLimit { limit = AnkleTwist };
                Vector2 range = AnkleRange();
                SetBendLimit(joint, range.x, range.y);
            }
            else
            {
                joint.angularXMotion = ConfigurableJointMotion.Locked;
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Locked;
            }
            joint.connectedBody = parent;
            _startLocal[index] = Quaternion.Inverse(parent.transform.rotation) * bones[index].rotation;
            _jointFrames[index] = JointFrame(axis, secondary);
        }

        private Vector2 KneeRange(int index)
        {
            ReadOnlySpan<Transform> bones = _leg.Bones;
            ReadOnlySpan<float> weights = _leg.BendWeights;
            Vector3 normal = bones[index].rotation * _leg.SegmentNormals[index];
            float bend = Vector3.SignedAngle(bones[index].position - bones[index - 1].position, bones[index + 1].position - bones[index].position, normal);
            bool positive = index - 1 >= weights.Length || weights[index - 1] >= 0f;
            return new Vector2((positive ? 0f : -MaxBend) - bend, (positive ? MaxBend : 0f) - bend);
        }

        private Vector2 AnkleRange()
        {
            Quaternion ground = _leg.Foot.rotation * Quaternion.Inverse(_leg.RestFootRotation);
            Vector3 toKnee = Quaternion.Inverse(ground) * (_leg.Lower.position - _leg.Foot.position);
            float angle = Mathf.Atan2(toKnee.y, toKnee.z) * Mathf.Rad2Deg;
            if (angle < -NeutralAnkle)
                angle += FullTurn;
            return new Vector2(NeutralAnkle - _leg.Dorsiflexion - angle, NeutralAnkle + _leg.Plantarflexion - angle);
        }

        private static void SetBendLimit(ConfigurableJoint joint, float low, float high)
        {
            low = Mathf.Clamp(Mathf.Min(low, 0f) * JointSign, -MaxLimit, MaxLimit);
            high = Mathf.Clamp(Mathf.Max(high, 0f) * JointSign, -MaxLimit, MaxLimit);
            joint.lowAngularXLimit = new SoftJointLimit { limit = Mathf.Min(low, high) };
            joint.highAngularXLimit = new SoftJointLimit { limit = Mathf.Max(low, high) };
        }

        private static Quaternion JointFrame(Vector3 axis, Vector3 secondary)
        {
            Vector3 forward = Vector3.Cross(axis, secondary).normalized;
            return Quaternion.LookRotation(forward, Vector3.Cross(forward, axis).normalized);
        }

        private static Quaternion Rotation(Vector3 angle)
        {
            float magnitude = angle.magnitude;
            return magnitude > MinAngle * Mathf.Deg2Rad ? Quaternion.AngleAxis(magnitude * Mathf.Rad2Deg, angle / magnitude) : Quaternion.identity;
        }

        private static Vector3 AngularVelocity(Quaternion from, Quaternion to, float deltaTime)
        {
            Quaternion delta = to * Quaternion.Inverse(from);
            if (delta.w < 0f)
                delta = new Quaternion(-delta.x, -delta.y, -delta.z, -delta.w);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            return angle > MinAngle ? axis * (angle * Mathf.Deg2Rad / deltaTime) : Vector3.zero;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.TryGetComponent(out T component) ? component : target.AddComponent<T>();
        }

        private static void AddShape(GameObject target, Mesh mesh)
        {
            string shape = mesh != null ? mesh.name : string.Empty;
            if (shape == "Sphere")
                target.AddComponent<SphereCollider>();
            else if (shape == "Capsule")
                target.AddComponent<CapsuleCollider>();
            else
                target.AddComponent<BoxCollider>();
        }
    }
}
