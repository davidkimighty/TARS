using UnityEngine;

namespace TARS
{
    [CreateAssetMenu(menuName = "TAPS/ProceduralAnimation/Body", fileName = "Body")]
    public class BodyPreset : ScriptableObject
    {
        public float StepDuration = 0.7f;
        public float MinStepDurationFactor = 0.7f;
        public float LegDelay = 0.3f;
        public float BodyDelay = 0.3f;
    }
}
