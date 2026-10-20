using UnityEngine;

namespace TARS.ActiveRagdoll
{
    [CreateAssetMenu(fileName = "Ragdoll", menuName = "TAPS/ActiveRagdoll/Ragdoll")]
    public class RagdollPresets : ScriptableObject
    {
        [Header("Balance")]
        [SerializeField] private float outOfBalanceThreshold = 0.03f;
        [SerializeField] private float fallDownThreshold = 0.05f;
        
        [Header("Movement")]
        [SerializeField] private float rotationSpeed = 3f;

        [Header("Detect")]
        [SerializeField] private float viewAngle = 60f;
        
        public float OutOfBalanceThreshold => outOfBalanceThreshold;
        public float FallDownThreshold => fallDownThreshold;
        public float RotationSpeed => rotationSpeed;
        public float ViewAngle => viewAngle;
    }
}
