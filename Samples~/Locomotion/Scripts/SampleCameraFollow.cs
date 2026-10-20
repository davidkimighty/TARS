using UnityEngine;
using UnityEngine.InputSystem;

namespace TARS.Locomotion.Samples
{
    public class SampleCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _targetOffset = new Vector3(0f, 0f, 0f);
        [SerializeField, Min(0f)] private float _rotateSpeed = 0.2f;
        [SerializeField] private float _minPitch = -10f;
        [SerializeField] private float _maxPitch = 70f;
        [SerializeField, Min(0f)] private float _followSharpness = 8f;

        private Vector3 _focus;
        private float _distance;
        private float _yaw;
        private float _pitch;

        private void Start()
        {
            _focus = _target.position + _targetOffset;
            Vector3 toCamera = transform.position - _focus;
            _distance = toCamera.magnitude;
            Vector3 euler = Quaternion.LookRotation(-toCamera).eulerAngles;
            _yaw = euler.y;
            _pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, euler.x), _minPitch, _maxPitch);
        }

        private void LateUpdate()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _yaw += delta.x * _rotateSpeed;
                _pitch = Mathf.Clamp(_pitch - delta.y * _rotateSpeed, _minPitch, _maxPitch);
            }
            _focus = Vector3.Lerp(_focus, _target.position + _targetOffset, 1f - Mathf.Exp(-_followSharpness * Time.deltaTime));
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            transform.SetPositionAndRotation(_focus - rotation * Vector3.forward * _distance, rotation);
        }
    }
}
