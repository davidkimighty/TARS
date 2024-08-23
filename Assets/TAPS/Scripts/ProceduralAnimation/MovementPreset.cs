using UnityEngine;

namespace TAPS
{
    [CreateAssetMenu(menuName = "TAPS/ProceduralAnimation/Movement", fileName = "Movement")]
    public class MovementPreset : ScriptableObject
    {
        public float WalkSpeed = 0.9f;
        public float RunSpeed = 6f;
        public float Acceleration = 1f;
        public float Deceleration = 1f;
        public float Gravity = -9.81f;
        public bool EnableGravity = true;

        public float BodyHeight = 1.97f;
        public float BodyRepositionSpeed = 2f;
        
        public float RayLength = 2.5f;
    }
}
