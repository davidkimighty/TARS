using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TARS.Locomotion.Samples
{
    [RequireComponent(typeof(PhysicsBody))]
    public class SamplePhysicsMovement : MonoBehaviour
    {
        private enum BodyState
        {
            Standing,
            Fallen,
            GettingUp
        }

        private const float MinSupport = 0.2f;
        private const float RestSpeed = 0.5f;

        [SerializeField] private InputAction _moveAction;
        [SerializeField] private InputAction _turnAction;
        [SerializeField] private InputAction _runAction;
        [SerializeField] private InputAction _knockAction;
        [SerializeField, Min(0f)] private float _moveSpeed = 1f;
        [SerializeField, Min(0f)] private float _runSpeed = 2f;
        [SerializeField, Min(0f)] private float _acceleration = 3f;
        [SerializeField, Min(0f)] private float _turnSpeed = 90f;
        [SerializeField, Min(0f)] private float _knockTime = 2f;
        [SerializeField, Range(0f, 180f)] private float _maxTilt = 60f;
        [SerializeField, Min(0f)] private float _supportGrace = 0.3f;
        [SerializeField, Min(0f)] private float _knockSpeed = 2f;
        [SerializeField, Min(0f)] private float _fallTime = 0.2f;
        [SerializeField, Min(0f)] private float _downTime = 1.5f;
        [SerializeField, Min(0f)] private float _getUpTime = 1f;

        private PhysicsBody _body;
        private LegLocomotion _locomotion;
        private BodyState _state;
        private float _stateTime;
        private float _unsupportedTime;
        private float _downDuration;
        private Vector3 _velocity;
        private Quaternion _heading;

        private void Awake()
        {
            _body = GetComponent<PhysicsBody>();
            _locomotion = GetComponent<LegLocomotion>();
        }

        private void OnEnable()
        {
            _moveAction.Enable();
            _turnAction.Enable();
            _runAction.Enable();
            _knockAction.Enable();
        }

        private void Start()
        {
            _heading = _body.Heading;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (_knockAction.WasPressedThisFrame())
                Fall(_knockTime);
            Vector2 move = _moveAction.ReadValue<Vector2>();
            bool standing = _state == BodyState.Standing;
            _heading = standing ? Quaternion.AngleAxis(_turnAction.ReadValue<float>() * _turnSpeed * deltaTime, Vector3.up) * _heading : _body.Heading;
            float speed = _runAction.IsPressed() ? _runSpeed : _moveSpeed;
            Vector3 targetVelocity = standing ? _heading * new Vector3(move.x, 0f, move.y) * speed : Vector3.zero;
            _velocity = Vector3.MoveTowards(_velocity, targetVelocity, _acceleration * deltaTime);
            _body.Move(_velocity, _heading);
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            _stateTime += deltaTime;
            switch (_state)
            {
                case BodyState.Standing:
                    _unsupportedTime = _body.Support < MinSupport ? _unsupportedTime + deltaTime : 0f;
                    if (_unsupportedTime > _supportGrace || _body.Tilt > _maxTilt)
                        Fall(_downTime);
                    break;
                case BodyState.Fallen:
                    if (_stateTime >= _downDuration && _body.Body.linearVelocity.sqrMagnitude < RestSpeed * RestSpeed)
                        GetUp();
                    break;
                case BodyState.GettingUp:
                    if (_body.Strength >= 1f)
                        SetState(BodyState.Standing);
                    break;
            }
        }

        private void OnDisable()
        {
            _moveAction.Disable();
            _turnAction.Disable();
            _runAction.Disable();
            _knockAction.Disable();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_state == BodyState.Standing && collision.impulse.magnitude > _knockSpeed * _body.Body.mass)
                Fall(_downTime);
        }

        private void Reset()
        {
            _moveAction = CreateMoveAction();
            _turnAction = CreateTurnAction();
            _runAction = CreateRunAction();
            _knockAction = CreateKnockAction();
        }

        private void Fall(float duration)
        {
            SetState(BodyState.Fallen);
            _downDuration = duration;
            SetPowered(false, _fallTime);
        }

        private void GetUp()
        {
            SetState(BodyState.GettingUp);
            SetPowered(true, _getUpTime);
        }

        private void SetPowered(bool powered, float blendTime)
        {
            _body.SetActive(powered, blendTime);
            _locomotion.SetActive(powered, blendTime);
            _locomotion.SetTucked(!powered, blendTime);
            ReadOnlySpan<BodyMotion> motions = _locomotion.BodyMotions;
            for (int i = 0; i < motions.Length; i++)
            {
                if (motions[i] != null)
                    motions[i].SetActive(powered, blendTime);
            }
        }

        private void SetState(BodyState state)
        {
            _state = state;
            _stateTime = 0f;
            _unsupportedTime = 0f;
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

        private static InputAction CreateKnockAction()
        {
            return new InputAction("Knock", InputActionType.Button, "<Keyboard>/k");
        }
    }
}
