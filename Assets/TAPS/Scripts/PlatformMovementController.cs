using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TAPS
{
    public class PlatformMovementController : MonoBehaviour
    {
        [SerializeField] private InputActionProperty _moveAction;
        [SerializeField] private InputActionProperty _runAction;
        [SerializeField] private MovementController _movementController;
        [SerializeField] private MovementPreset _preset;
        [SerializeField] private TMP_Text _inputText;
        
        private void OnEnable()
        {
            _moveAction.action.performed += ReadMove;
            _moveAction.action.canceled += ReadMove;
            
            _runAction.action.performed += ReadRun;
            _runAction.action.canceled += ReadRun;

            _movementController.OnMove += ReadMove;
        }

        private void OnDisable()
        {
            _moveAction.action.performed -= ReadMove;
            _moveAction.action.canceled -= ReadMove;
            
            _runAction.action.performed -= ReadRun;
            _runAction.action.canceled -= ReadRun;
            
            _movementController.OnMove -= ReadMove;
        }

        private void ReadMove(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();
            Vector3 dir = Vector3.forward * input.x;
            _movementController.SetMoveDirection(dir);
        }

        private void ReadRun(InputAction.CallbackContext context)
        {
            bool input = context.ReadValueAsButton();
            _movementController.SetMoveSpeed(input ? _preset.RunSpeed : _preset.WalkSpeed);
        }

        private void ReadMove(MoveData data)
        {
            _inputText.text = $"move direction: {data.MoveDir}\n" +
                              $"target velocity: {data.TargetSpeed}\n" +
                              $"current velocity: {data.CurrentSpeed}";
        }
    }
}
