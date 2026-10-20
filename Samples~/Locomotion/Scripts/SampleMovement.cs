using UnityEngine;
using UnityEngine.InputSystem;

namespace TARS.Locomotion.Samples
{
    [RequireComponent(typeof(CharacterController))]
    public class SampleMovement : MonoBehaviour
    {
        [SerializeField] private InputAction _moveAction;
        [SerializeField] private InputAction _turnAction;
        [SerializeField] private InputAction _runAction;
        [SerializeField, Min(0f)] private float _moveSpeed = 1f;
        [SerializeField, Min(0f)] private float _runSpeed = 2f;
        [SerializeField, Min(0f)] private float _acceleration = 3f;
        [SerializeField, Min(0f)] private float _turnSpeed = 90f;
        [SerializeField] private float _groundedVerticalSpeed = -1f;

        private CharacterController _controller;
        private Vector3 _planarVelocity;
        private float _verticalSpeed;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            _moveAction.Enable();
            _turnAction.Enable();
            _runAction.Enable();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            Vector2 move = _moveAction.ReadValue<Vector2>();
            transform.Rotate(0f, _turnAction.ReadValue<float>() * _turnSpeed * deltaTime, 0f);
            float speed = _runAction.IsPressed() ? _runSpeed : _moveSpeed;
            Vector3 targetVelocity = transform.TransformDirection(new Vector3(move.x, 0f, move.y)) * speed;
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, targetVelocity, _acceleration * deltaTime);
            if (_controller.isGrounded && _verticalSpeed < _groundedVerticalSpeed)
                _verticalSpeed = _groundedVerticalSpeed;
            _verticalSpeed += Physics.gravity.y * deltaTime;
            _controller.Move((_planarVelocity + Vector3.up * _verticalSpeed) * deltaTime);
        }

        private void OnDisable()
        {
            _moveAction.Disable();
            _turnAction.Disable();
            _runAction.Disable();
        }

        private void Reset()
        {
            _moveAction = CreateMoveAction();
            _turnAction = CreateTurnAction();
            _runAction = CreateRunAction();
        }

        private static InputAction CreateMoveAction()
        {
            InputAction action = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            action.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            return action;
        }

        private static InputAction CreateTurnAction()
        {
            InputAction action = new InputAction("Turn", InputActionType.Value, expectedControlType: "Axis");
            action.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/q").With("Positive", "<Keyboard>/e");
            return action;
        }

        private static InputAction CreateRunAction()
        {
            return new InputAction("Run", InputActionType.Button, "<Keyboard>/leftShift");
        }
    }
}
