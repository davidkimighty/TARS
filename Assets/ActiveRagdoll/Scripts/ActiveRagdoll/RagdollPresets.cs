using UnityEngine;

namespace CollieLab.ActiveRagdoll
{
    [CreateAssetMenu(fileName = "RagdollPresets", menuName = "Coco/Creature/RagdollPresets")]
    public class RagdollPresets : ScriptableObject
    {
        [Header("Balance")]
        [SerializeField] private float outOfBalanceThreshold = 0.03f;
        public float OutOfBalanceThreshold
        {
            get => outOfBalanceThreshold;
        }

        [SerializeField] private float fallDownThreshold = 0.05f;
        public float FallDownThreshold
        {
            get => fallDownThreshold;
        }

        [Header("Movement")]
        [SerializeField] private float rotationSpeed = 3f;
        public float RotationSpeed
        {
            get => rotationSpeed;
        }

        [Header("Detect")]
        [SerializeField] private float viewAngle = 60f;
        public float ViewAngle
        {
            get => viewAngle;
        }
    }
}
