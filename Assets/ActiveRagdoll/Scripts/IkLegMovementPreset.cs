using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollieMollie.ActiveRagdoll
{
    [CreateAssetMenu(fileName = "IkLegMovement", menuName = "CollieMollie/ActiveRagdoll/IkLegMovement")]
    public class IkLegMovementPreset : ScriptableObject
    {
        public float StepLength = 0.3f;
        public float StepThreshold = 0.6f;
        public float StepHeight = 0.3f;
        public float StepDuration = 1f;
        public AnimationCurve StepSpeedCurve = null;
    }
}
