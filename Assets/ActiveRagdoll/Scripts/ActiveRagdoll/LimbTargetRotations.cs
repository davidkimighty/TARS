using System;
using UnityEngine;

namespace CollieLab.ActiveRagdoll
{
    [CreateAssetMenu(fileName = "LimbTargetRotations", menuName = "Coco/Creature/LimbTargetRotations")]
    public class LimbTargetRotations : ScriptableObject
    {
        #region Serialized Field
        [SerializeField] public LimbTargetRotation[] targetRotations = null;
        #endregion
    }

    [Serializable]
    public class LimbTargetRotation
    {
        #region Serialized Field
        [SerializeField] private Vector3 upperRotation = Vector3.zero;
        public Quaternion UpperRot
        {
            get => Quaternion.Euler(upperRotation);
        }

        [SerializeField] private Vector3 lowerRotation = Vector3.zero;
        public Quaternion LowerRot
        {
            get => Quaternion.Euler(lowerRotation);
        }

        [SerializeField] private Vector3 endRotation = Vector3.zero;
        public Quaternion EndRot
        {
            get => Quaternion.Euler(endRotation);
        }

        [SerializeField] private float duration = 0f;
        public float Duration
        {
            get => duration;
        }
        #endregion
    }
}
