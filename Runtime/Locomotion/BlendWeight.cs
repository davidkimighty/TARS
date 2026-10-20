using UnityEngine;

namespace TARS.Locomotion
{
    public struct BlendWeight
    {
        private float _target;
        private float _rate;
        private float _value;

        public static BlendWeight On => new BlendWeight { _target = 1f, _value = 1f };

        public readonly float Value => _value * _value * (3f - 2f * _value);
        public readonly bool IsOff => _value <= 0f && _target <= 0f;
        public readonly bool IsOn => _value >= 1f && _target >= 1f;
        public readonly bool IsActive => _target > 0f;

        public void Set(bool active, float blendTime)
        {
            _target = active ? 1f : 0f;
            _rate = blendTime > 0f ? 1f / blendTime : 0f;
            if (blendTime <= 0f)
                _value = _target;
        }

        public void Advance(float deltaTime)
        {
            _value = Mathf.MoveTowards(_value, _target, _rate * deltaTime);
        }
    }
}
