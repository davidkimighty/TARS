using System;
using UnityEngine;

namespace TARS.ActiveRagdoll
{
    [Serializable]
    public class LimbTargetRotation
    {
        [SerializeField] private Vector3 upperRotation = Vector3.zero;
        [SerializeField] private Vector3 lowerRotation = Vector3.zero;
        [SerializeField] private Vector3 endRotation = Vector3.zero;
        [SerializeField] private float duration = 0f;
        
        public Quaternion UpperRot => Quaternion.Euler(upperRotation);
        public Quaternion LowerRot => Quaternion.Euler(lowerRotation);
        public Quaternion EndRot => Quaternion.Euler(endRotation);
        public float Duration => duration;
    }
    
    [CreateAssetMenu(fileName = "LimbTargetRotations", menuName = "TAPS/ActiveRagdoll/LimbTargetRotations")]
    public class LimbTargetRotations : ScriptableObject
    {
        [SerializeField] public LimbTargetRotation[] targetRotations = null;
    }
}
