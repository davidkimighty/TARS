using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TAPS
{
    public class AxisMovementController : MovementController
    {
        [SerializeField] private bool _receivePlayerInput = true;
        [SerializeField] private InputActionProperty _moveInput;
        [SerializeField] private Transform _rootBody;
        [SerializeField] private List<Transform> _rootFeet;
        [SerializeField] private LayerMask _ignoreLayer;
        [SerializeField] private MovementPreset _preset;
        
        private void Awake()
        {
            ReceivePlayerInput(_receivePlayerInput);
            _targetSpeed = _preset.WalkSpeed;
        }

        private void Update()
        {
            Move(_moveDir);

            Vector3 centerPoint = _hasInput ? _rootBody.position : CalculateCenterPoint(_rootFeet);
            RepositionBody(centerPoint);
        }

        public override void Move(Vector3 moveDir)
        {
            Vector3 targetVel = Vector3.zero;
            if (_hasInput)
            {
                targetVel = moveDir * _targetSpeed;
                _currentVel = Vector3.MoveTowards(_currentVel, targetVel, _preset.Acceleration * Time.deltaTime);
            }
            else
                _currentVel = Vector3.MoveTowards(_currentVel, targetVel, _preset.Deceleration * Time.deltaTime);
            _rootBody.position += _currentVel * Time.deltaTime;
            
            InvokeOnMove(new MoveData
            {
                MoveDir = moveDir,
                TargetSpeed = _targetSpeed,
                CurrentSpeed = _currentVel.magnitude,
                IsRunning = false
            });
        }

        public override void RepositionBody(Vector3 centerPoint)
        {
            float feetDst = Vector3.Distance(_rootFeet.First().position, _rootFeet.Last().position);
            float feetHeight = _rootFeet.Aggregate(0f, (current, leg) => current + GetGroundHeight(leg.transform.position)) / _rootFeet.Count;
            float bodyHeight = Mathf.Sqrt(Mathf.Pow(_preset.BodyHeight, 2f) - Mathf.Pow(feetDst / 2f, 2f)) + feetHeight;
            
            if (!float.IsNaN(bodyHeight))
                centerPoint.y = bodyHeight;
            _rootBody.position = Vector3.Lerp(_rootBody.position, centerPoint, _preset.BodyRepositionSpeed * Time.fixedDeltaTime);
            
            float GetGroundHeight(Vector3 position)
            {
                return Physics.Raycast(position, Vector3.down,
                    out RaycastHit hit, _preset.RayLength, ~_ignoreLayer) ? hit.point.y : 0f;
            }
        }

        public void ReceivePlayerInput(bool state)
        {
            if (state)
            {
                _moveInput.action.performed += ReadMoveInput;
                _moveInput.action.canceled += ReadMoveInput;
                return;
            }
            _moveInput.action.performed -= ReadMoveInput;
            _moveInput.action.canceled -= ReadMoveInput;
        }
        
        public void SetMoveDirection(Vector3 dir)
        {
            _hasInput = dir != Vector3.zero;
            _moveDir = _rootBody.TransformDirection(dir).normalized;
        }

        public void SetMoveSpeed(float speed) => _targetSpeed = speed;
        
        private void ReadMoveInput(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();
            _hasInput = input != Vector2.zero;
            _moveDir = (_rootBody.right * input.x + _rootBody.forward * input.y).normalized;
        }
    }
}
