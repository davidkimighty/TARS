using System;
using UnityEngine;

namespace TARS.Locomotion
{
    [CreateAssetMenu(menuName = "TARS/Locomotion/Step Style", fileName = "StepStyle")]
    public class StepStyle : ScriptableObject
    {
        [Serializable]
        public struct StepPoint
        {
            [Range(0f, 1f)] public float Height;
            [Range(0f, 1f)] public float Kick;
        }

        [Serializable]
        public struct FootRoll
        {
            [Range(0f, 60f)] public float HeelStrike;
            [Range(0f, 80f)] public float ToeOff;
        }

        private const float SlopeSampleStep = 0.01f;
        private const int SlopeSampleCount = 64;

        public StepPoint Walk = new StepPoint { Height = 0.1f, Kick = 0f };
        public StepPoint Run = new StepPoint { Height = 0.3f, Kick = 0.2f };
        public FootRoll Foot = new FootRoll { HeelStrike = 20f, ToeOff = 40f };
        public AnimationCurve Arc = new AnimationCurve(new Keyframe(0f, 0f, 0f, Mathf.PI), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f, -Mathf.PI, 0f));

        [NonSerialized] private bool _measured;
        [NonSerialized] private float _arcPeakSlope;
        [NonSerialized] private float _arcStartTilt;
        [NonSerialized] private float _arcEndTilt;

        public float ArcStartTilt
        {
            get
            {
                if (!_measured)
                    MeasureArc();
                return _arcStartTilt;
            }
        }
        public float ArcEndTilt
        {
            get
            {
                if (!_measured)
                    MeasureArc();
                return _arcEndTilt;
            }
        }

        private void OnValidate()
        {
            _measured = false;
        }

        public float Height(float speedBlend)
        {
            return Mathf.Lerp(Walk.Height, Run.Height, speedBlend);
        }

        public float Kick(float speedBlend)
        {
            return Mathf.Lerp(Walk.Kick, Run.Kick, speedBlend);
        }

        public float ArcTilt(float travel)
        {
            if (!_measured)
                MeasureArc();
            return _arcPeakSlope > 0f ? Mathf.Clamp(ArcSlope(travel) / _arcPeakSlope, -1f, 1f) : 0f;
        }

        private float ArcSlope(float travel)
        {
            float before = Mathf.Clamp01(travel - SlopeSampleStep);
            float after = Mathf.Clamp01(travel + SlopeSampleStep);
            return (Arc.Evaluate(after) - Arc.Evaluate(before)) / (after - before);
        }

        private void MeasureArc()
        {
            _measured = true;
            float peak = 0f;
            for (int i = 0; i <= SlopeSampleCount; i++)
                peak = Mathf.Max(peak, Mathf.Abs(ArcSlope((float)i / SlopeSampleCount)));
            _arcPeakSlope = peak;
            _arcStartTilt = ArcTilt(0f);
            _arcEndTilt = ArcTilt(1f);
        }
    }
}
