using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TARS
{
    public class PhysicsMovementController : MovementController
    {
        public event Action<bool> OnBalance;
        
        [SerializeField] protected Rigidbody _body;
        [SerializeField] protected Transform _rayOrigin;
        [SerializeField] protected LayerMask _ignoreLayer;
        [SerializeField] protected List<Transform> _rootFeet;
        [SerializeField] protected PhysicsMovementPreset _movementPreset;
        [SerializeField] protected InputActionProperty _moveInputAction;
        [SerializeField] protected InputActionProperty _jumpInputAction;

        protected bool _jumpInput;
        protected float _floatHeight;
        protected Quaternion _lookRotation;
        protected bool _grounded = true;
        protected bool _balanceState = true;
        protected bool _previousBalanceState;
        protected float _elapsedTimeBeforeGetup;

        private void Awake()
        {
            _moveInputAction.action.performed += ReadMoveInput;
            _moveInputAction.action.canceled += ReadMoveInput;
            
            _jumpInputAction.action.performed += ReadJumpInput;
            _jumpInputAction.action.canceled += ReadJumpInput;
        }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            
            _moveInput = _moveDir = _body.transform.forward;
            _lookRotation = Quaternion.LookRotation(_moveDir);
            _targetSpeed = _movementPreset.WalkSpeed;
            _floatHeight = _movementPreset.FloatHeight;
        }

        private void FixedUpdate()
        {
            (bool isHit, RaycastHit rayHit) = CastRay(_ignoreLayer);
            Vector3 centerPoint = CalculateCenterPoint(_rootFeet);
            
            if (_balanceState)
                _balanceState = isHit && CheckOnBalance(centerPoint);

            if (_balanceState)
            {
                Balance(rayHit, centerPoint);

                if (_hasInput)
                    Move(_moveInput);
                Jump();
            }
            else
                GetUp(rayHit);
        }

        private void OnDestroy()
        {
            _moveInputAction.action.performed -= ReadMoveInput;
            _moveInputAction.action.canceled -= ReadMoveInput;
            
            _jumpInputAction.action.performed -= ReadJumpInput;
            _jumpInputAction.action.canceled -= ReadJumpInput;
        }

        private void ReadMoveInput(InputAction.CallbackContext context)
        {
            Vector2 rawInput = context.ReadValue<Vector2>();
            _hasInput = rawInput != Vector2.zero;
            _moveInput = Vector3.right * rawInput.x + Vector3.forward * rawInput.y;
        }
        
        private void ReadJumpInput(InputAction.CallbackContext context)
        {
            _jumpInput = context.ReadValueAsButton();
        }

        public override void Move(Vector3 moveInput)
        {
            _moveDir = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0) * _moveInput;
            
            Vector3 targetVel = _moveDir * _targetSpeed;
            _currentVel = Vector3.MoveTowards(_currentVel, targetVel, _movementPreset.Acceleration * Time.fixedDeltaTime);

            Vector3 targetAccel = (_currentVel - _body.velocity) / Time.fixedDeltaTime;
            targetAccel = Vector3.ClampMagnitude(targetAccel, _movementPreset.MaxAcceleration);

            Vector3 force = Vector3.Scale(targetAccel * _body.mass, new Vector3(1, 0, 1));
            //_targetBody.AddForceAtPosition(force, _targetBody.position);
            _body.AddForce(force);
            
            InvokeOnMove(new MoveData
            {
                MoveDir = _moveDir,
                TargetSpeed = _targetSpeed,
                CurrentSpeed = _currentVel.magnitude,
                IsRunning = false
            });
        }

        public override void RepositionBody(Vector3 centerPoint)
        {
            centerPoint.y = _body.position.y;
            Vector3 displacement = centerPoint - _body.position;
            Vector3 springForce = displacement * _movementPreset.RepositionStrength -
                                  _body.velocity * _movementPreset.RepositionDamper;
            _body.AddForce(springForce);
        }
        
        protected void Rotation(Vector3 moveDir, ref Quaternion lookRot)
        {
            if (_hasInput)
                lookRot = Quaternion.LookRotation(moveDir);
            
            Quaternion targetRotation = ShortestRotation(lookRot, _body.rotation);
            targetRotation.ToAngleAxis(out float angle, out Vector3 axis);
            _body.AddTorque(axis.normalized *
                (angle * Mathf.Deg2Rad * _movementPreset.RotationStrength) - _body.angularVelocity * _movementPreset.RotationDamper);
        }
        
        protected void Floating(RaycastHit rayHit)
        {
            _grounded = rayHit.distance <= _floatHeight;
            if (!_grounded) return;
            
            Vector3 hitVel = rayHit.rigidbody != null ? rayHit.rigidbody.velocity : Vector3.zero;
            float rayDirVel = Vector3.Dot(Vector3.down, _body.velocity);
            float hitRayDirVel = Vector3.Dot(Vector3.down, hitVel);
            float relVel = rayDirVel - hitRayDirVel;
            float offset = rayHit.distance - _floatHeight;
            float springForce = offset * _movementPreset.SpringStrength - relVel * _movementPreset.SpringDamper;
            _body.AddForce(Vector3.down * springForce);
        }
        
        protected void Jump()
        {
            if (!_grounded)
            {
                if (_body.velocity.y < 0)
                {
                    float gravity = Physics.gravity.y * (_movementPreset.FallMultiplier - 1);
                    _body.velocity += Vector3.up * (gravity * Time.fixedDeltaTime);
                }
                return;
            }

            if (_jumpInput)
            {
                _body.velocity = new Vector3(_body.velocity.x, 0, _body.velocity.z);
                _body.AddForce(Vector3.up * _movementPreset.JumpForce, ForceMode.Impulse);
            }
        }
        
        protected (bool, RaycastHit) CastRay(LayerMask layer)
        {
            Ray ray = new(_rayOrigin.position, Vector3.down);
            bool isHit = Physics.Raycast(ray, out RaycastHit rayHit, _movementPreset.RayLength, ~layer);
            return (isHit, rayHit);
        }
        
        protected bool CheckOnBalance(Vector3 centerPoint)
        {
            float upDirDot = Vector3.Dot(_body.transform.up, Vector3.up);
            Vector3 centerPointLocal = _body.transform.InverseTransformPoint(centerPoint);
            centerPointLocal.y = 0f;

            return upDirDot > _movementPreset.UpDirThreshold &&
                   centerPointLocal.magnitude < _movementPreset.CenterPointThreshold;
        }
        
        protected virtual void Balance(RaycastHit rayHit, Vector3 feetCenterPoint)
        {
            if (_previousBalanceState != _balanceState && _balanceState)
                OnBalance?.Invoke(true);
            _previousBalanceState = _balanceState;
            
            if (rayHit.distance > _movementPreset.FloatHeight + _movementPreset.ExtendedHeight) return;
            _elapsedTimeBeforeGetup = 0f;
            
            RepositionBody(feetCenterPoint);
            Rotation(_moveDir, ref _lookRotation);
            Floating(rayHit);
        }

        protected virtual void GetUp(RaycastHit rayHit)
        {
            if (_previousBalanceState != _balanceState && !_balanceState)
                OnBalance?.Invoke(false);
            _previousBalanceState = _balanceState;

            if (rayHit.transform == null || 
                rayHit.distance > _movementPreset.FloatHeight + _movementPreset.ExtendedHeight) return;
            
            if (_elapsedTimeBeforeGetup < _movementPreset.GetupDelayTime)
            {
                _elapsedTimeBeforeGetup += Time.deltaTime;
                return;
            }

            Rotation(_moveDir, ref _lookRotation);
            Floating(rayHit);
            
            if (_previousBalanceState != _balanceState && !_balanceState)
                OnBalance?.Invoke(true);
            _balanceState = true;
        }
        
        private Quaternion ShortestRotation(Quaternion a, Quaternion b)
        {
            if (Quaternion.Dot(a, b) < 0)
                return a * Quaternion.Inverse(Multiply(b, -1f));
            return a * Quaternion.Inverse(b);
        }

        private Quaternion Multiply(Quaternion q, float scalar)
        {
            return new Quaternion(q.x * scalar, q.y * scalar, q.z * scalar, q.w * scalar);
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(_rayOrigin.position, Vector3.down * _movementPreset.RayLength);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(_rayOrigin.position, Vector3.down * _movementPreset.FloatHeight);
        }
#endif
    }
}