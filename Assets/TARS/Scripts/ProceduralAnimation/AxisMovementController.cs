using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TARS
{
    public class AxisMovementController : MonoBehaviour, IMovement
    {
        public event Action<MoveData> OnMove;
        
        [SerializeField] private InputActionProperty _moveInputAction;
        [SerializeField] private Transform _rootBody;
        [SerializeField] private List<Transform> _rootFeet;
        [SerializeField] private LayerMask _ignoreLayer;
        [SerializeField] private MovementPreset _preset;
        
        private Vector3 _moveInput;
        private bool _hasInput;
        private Vector3 _moveDir;
        private Vector3 _currentVel;
        private float _targetSpeed;
        
        private void Awake()
        {
            _targetSpeed = _preset.WalkSpeed;
        }

        private void Update()
        {
            Move(_moveInput);

            Vector3 centerPoint = _hasInput ? _rootBody.position : MovementUtils.CalculateCenterPoint(_rootFeet);
            RepositionBody(centerPoint);
        }

        public void Move(Vector3 moveInput)
        {
            Vector3 targetVel = Vector3.zero;
            if (_hasInput)
            {
                targetVel = moveInput * _targetSpeed;
                _currentVel = Vector3.MoveTowards(_currentVel, targetVel, _preset.Acceleration * Time.deltaTime);
                
                InvokeOnMove(new MoveData
                {
                    MoveDir = moveInput,
                    BodyForward = _rootBody.forward,
                    TargetSpeed = _targetSpeed,
                    CurrentSpeed = _currentVel.magnitude,
                    IsRunning = false
                });
            }
            else
                _currentVel = Vector3.MoveTowards(_currentVel, targetVel, _preset.Deceleration * Time.deltaTime);
            
            _rootBody.position += _currentVel * Time.deltaTime;
        }

        public void RepositionBody(Vector3 centerPoint)
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

        public void InvokeOnMove(MoveData moveData) => OnMove?.Invoke(moveData);
        public void Rotate(Vector3 moveDir, ref Quaternion lookRot) { } // empty

        public void Jump() { } // empty

        public void SetMoveDirection(Vector3 dir)
        {
            _hasInput = dir != Vector3.zero;
            _moveInput = _rootBody.TransformDirection(dir).normalized;
        }

        public void SetMoveSpeed(float speed) => _targetSpeed = speed;
    }
}
