using UnityEngine;

namespace SAR
{
    [CreateAssetMenu(menuName = "SAR/ProceduralAnimation/ProceduralAnimation", fileName = "ProceduralAnimationPreset")]
    public class ProceduralAnimationPreset : ScriptableObject
    {
        public float StepDuration = 0.7f;
        public float MinStepDurationFactor = 0.7f;
        public float LegDelay = 0.3f;
        public float BodyDelay = 0.3f;
    }
}
