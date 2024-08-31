using UnityEngine;

namespace TAPS
{
    [CreateAssetMenu(menuName = "TAPS/ProceduralAnimation/PhysicsMovement", fileName = "PhysicsMovement")]
    public class PhysicsMovementPreset : ScriptableObject
    {
        public float WalkSpeed = 1f;
        public float RunSpeed = 2f;
        public float Acceleration = 3f;
        public float MaxAcceleration = 7f;
        
        public float SpringStrength = 3000f;
        public float SpringDamper = 300f;
        public float RayLength = 1.5f;
        public float FloatHeight = 1f;
        public float CrouchHeight = 0.3f;
        public float ExtendedHeight = 0.3f;
        public Vector3 RayOffset = Vector3.zero;
        
        public float RotationStrength = 300f;
        public float RotationDamper = 70f;
        public float RepositionStrength = 150f;
        public float RepositionDamper = 50f;
        
        public float UpDirThreshold = 0.4f;
        public float CenterPointThreshold = 0.6f;
        public float GetupDelayTime = 3f;
    }
}