using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SAR
{
    public struct MoveData
    {
        public Vector3 MoveDir;
        public float TargetSpeed;
        public float CurrentSpeed;
        public bool IsRunning;
    }
    
    public class MovementController : MonoBehaviour
    {
        public event Action<MoveData> OnMove;

        [SerializeField] private bool _receivePlayerInput = true;
        [SerializeField] private InputActionProperty _moveInput;
        [SerializeField] private LowerBody _rootBody;
        [SerializeField] private LayerMask _ignoreLayer;
        [SerializeField] private MovementPreset _preset;

        private Vector3 _moveDir;
        private bool _hasInput;
        private Vector3 _currentVel;
        private float _targetSpeed;

        private void Awake()
        {
            ReceivePlayerInput(_receivePlayerInput);
            _targetSpeed = _preset.WalkSpeed;
        }

        private void Update()
        {
            Move();
            RepositionBody();
        }

        public void ReceivePlayerInput(bool state)
        {
            if (state)
            {
                _moveInput.action.performed += ReadMoveInput;
                _moveInput.action.canceled += ReadMoveInput;
            }
            else
            {
                _moveInput.action.performed -= ReadMoveInput;
                _moveInput.action.canceled -= ReadMoveInput;
            }
        }

        public void SetMoveDirection(Vector3 dir)
        {
            _hasInput = dir != Vector3.zero;
            _moveDir = _rootBody.Body.TransformDirection(dir).normalized;
        }

        public void SetMoveSpeed(float speed) => _targetSpeed = speed;

        private void ReadMoveInput(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();
            _hasInput = input != Vector2.zero;
            _moveDir = (_rootBody.Body.right * input.x + _rootBody.Body.forward * input.y).normalized;
        }

        private void Move()
        {
            Vector3 targetVel = Vector3.zero;
            if (_hasInput)
            {
                targetVel = _moveDir * _targetSpeed;
                _currentVel = Vector3.MoveTowards(_currentVel, targetVel, _preset.Acceleration * Time.deltaTime);
            }
            else
                _currentVel = Vector3.MoveTowards(_currentVel, targetVel, _preset.Deceleration * Time.deltaTime);
            _rootBody.Body.position += _currentVel * Time.deltaTime;
            
            OnMove?.Invoke(new MoveData
            {
                MoveDir = _moveDir,
                TargetSpeed = _targetSpeed,
                CurrentSpeed = _currentVel.magnitude,
                IsRunning = false
            });
        }
        
        private void RepositionBody()
        {
            Vector3 newPos = _rootBody.Body.position;
            if (!_hasInput)
                newPos = _rootBody.Legs.Aggregate(Vector3.zero, (current, leg) => current + leg.Foot.position) / _rootBody.Legs.Length;
                    
            float feetDst = Vector3.Distance(_rootBody.Legs.First().Foot.position, _rootBody.Legs.Last().Foot.position);
            float feetHeight = _rootBody.Legs.Aggregate(0f, (current, leg) => current + GetGroundHeight(leg.transform.position)) / _rootBody.Legs.Length;
            float bodyHeight = Mathf.Sqrt(Mathf.Pow(_preset.BodyHeight, 2f) - Mathf.Pow(feetDst / 2f, 2f)) + feetHeight;
            if (!float.IsNaN(bodyHeight))
                newPos.y = bodyHeight;
            _rootBody.Body.position = Vector3.Lerp(_rootBody.Body.position, newPos, _preset.BodyRepositionSpeed * Time.fixedDeltaTime);
            
            float GetGroundHeight(Vector3 position)
            {
                return Physics.Raycast(position, Vector3.down,
                    out RaycastHit hit, _preset.RayLength, ~_ignoreLayer) ? hit.point.y : 0f;
            }
        }
    }
}
