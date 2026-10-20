using System;
using UnityEngine;

namespace TARS.Locomotion
{
    [CreateAssetMenu(menuName = "TARS/Locomotion/Gait Settings", fileName = "GaitSettings")]
    public class GaitSettings : ScriptableObject
    {
        [Serializable]
        public struct GaitPoint
        {
            [Range(MinFroude, MaxFroude)] public float Speed;
            [Min(MinStride)] public float Stride;
            [Range(MinDutyFactor, MaxDutyFactor)] public float Duty;
        }

        [Serializable]
        public struct LegPattern
        {
            [Range(0f, 1f)] public float SideLag;
            [Range(0f, 1f)] public float SegmentLag;
        }

        private const float MinFroude = 0.001f;
        private const float MaxFroude = 3f;
        private const float MinStride = 0.05f;
        private const float MinDutyFactor = 0.1f;
        private const float MaxDutyFactor = 0.95f;
        private const float MaxStanceLength = 1f;
        private const float MaxStrideExponent = 1f;

        public GaitPoint Walk = new GaitPoint { Speed = 0.01f, Stride = 0.6f, Duty = 0.7f };
        public GaitPoint Run = new GaitPoint { Speed = 0.5f, Stride = 1.55f, Duty = 0.55f };
        public LegPattern Pattern = new LegPattern { SideLag = 0.5f, SegmentLag = 0.5f };
        [Range(0f, 0.5f)] public float SettleDistance = 0.07f;
        [Min(0f)] public float SwingSmoothing = 0.3f;

        [NonSerialized] private bool _fitted;
        [NonSerialized] private float _strideExponent;

        private void OnValidate()
        {
            _fitted = false;
            Run.Speed = Mathf.Max(Run.Speed, Walk.Speed);
            Run.Stride = Mathf.Clamp(Run.Stride, Walk.Stride, Walk.Stride * Mathf.Sqrt(Run.Speed / Walk.Speed));
        }

        public GaitTiming Evaluate(float speed, float legLength, float stanceReach, float supportDuty)
        {
            if (!_fitted)
                FitStride();
            float gravity = Physics.gravity.magnitude;
            float gravityLength = gravity * legLength;
            float walkSpeed = Mathf.Sqrt(Walk.Speed * gravityLength);
            float runSpeed = Mathf.Sqrt(Run.Speed * gravityLength);
            float timingSpeed = Mathf.Clamp(speed, walkSpeed, runSpeed);
            float stanceSpeed = Mathf.Max(speed, walkSpeed);
            float maxStance = MaxStanceLength * stanceReach;
            GaitTiming timing;
            timing.PendulumTime = Mathf.Sqrt(legLength / gravity);
            timing.CycleDuration = Walk.Stride * legLength * Mathf.Pow(timingSpeed / walkSpeed, _strideExponent) / timingSpeed;
            timing.SpeedBlend = Mathf.InverseLerp(walkSpeed, runSpeed, speed);
            timing.RollWeight = Mathf.Clamp01(speed / walkSpeed);
            float support = supportDuty * (1f - timing.SpeedBlend * timing.SpeedBlend);
            if (stanceSpeed * timing.CycleDuration * support > maxStance)
                timing.CycleDuration = maxStance / (stanceSpeed * support);
            float stanceLimit = maxStance / (stanceSpeed * timing.CycleDuration);
            timing.DutyFactor = Mathf.Max(Mathf.Min(Mathf.Lerp(Walk.Duty, Run.Duty, timing.SpeedBlend), stanceLimit), support, MinDutyFactor);
            return timing;
        }

        public float PhaseOffset(LegRig leg)
        {
            if (leg.OverridePhase) return leg.PhaseOffset;
            float side = leg.Side == LegSide.Right ? 1f : 0f;
            return Mathf.Repeat(Pattern.SideLag * side + Pattern.SegmentLag * leg.Segment, 1f);
        }

        private void FitStride()
        {
            _fitted = true;
            float speedRatio = Mathf.Sqrt(Run.Speed / Walk.Speed);
            _strideExponent = speedRatio > 1f ? Mathf.Clamp(Mathf.Log(Run.Stride / Walk.Stride) / Mathf.Log(speedRatio), 0f, MaxStrideExponent) : 0f;
        }
    }
}
