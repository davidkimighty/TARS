using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CollieLab.ActiveRagdoll
{
    public class RagdollArmature : MonoBehaviour
    {
        #region Serialized Field
        [SerializeField] private BoneInfo[] head = null;
        public BoneInfo[] Head
        {
            get => head;
            set => head = value;
        }

        [SerializeField] private BoneInfo[] bodies = null;
        public BoneInfo[] Bodies
        {
            get => bodies;
            set => bodies = value;
        }

        [SerializeField] private LegInfo[] legs = null;
        public LegInfo[] Legs
        {
            get => legs;
            set => legs = value;
        }

        [SerializeField] private BoneInfo[] tail = null;
        public BoneInfo[] Tail
        {
            get => tail;
            set => tail = value;
        }

        [SerializeField] private IgnoreCollider[] ignoreColliderGroup = null;
        #endregion

        #region Private Field
        private BoneInfo[] allBones = null;
        public BoneInfo[] AllBones
        {
            get => allBones;
            set => allBones = value;
        }

        private BoneInfo rootBone = null;
        public BoneInfo RootBone
        {
            get => rootBone;
            set => rootBone = value;
        }
        #endregion

        #region Initialize Bone Settings
        /// <summary>
        /// Gathering all the bones for calculation.
        /// </summary>
        public void GatherBones()
        {
            List<BoneInfo> allBones = new List<BoneInfo>();

            allBones.AddRange(head.ToList());
            allBones.AddRange(bodies.ToList());
            for (int i = 0; i < legs.Length; i++)
            {
                AddLimbs(allBones, legs[i].leftLeg);
                AddLimbs(allBones, legs[i].rightLeg);
            }
            allBones.AddRange(tail.ToList());

            this.allBones = allBones.ToArray();
        }

        private void AddLimbs(List<BoneInfo> bones, LimbInfo limb)
        {
            if (limb.upper.obj == null) return;

            bones.Add(limb.upper);
            bones.Add(limb.lower);
            bones.Add(limb.end);
        }

        /// <summary>
        /// Find key bones that needs direct reference.
        /// Body index 0 is set as the root bone.
        /// </summary>
        public void FindKeyBones()
        {
            rootBone = bodies[0];
        }

        /// <summary>
        /// Ignore collisions of bones in the list.
        /// </summary>
        public void IgnoreColliders()
        {
            foreach (var colliders in ignoreColliderGroup)
            {
                for (int i = 0; i < colliders.ignoreColliders.Length - 1; i++)
                {
                    for (int j = i + 1; j < colliders.ignoreColliders.Length; j++)
                    {
                        Physics.IgnoreCollision(colliders.ignoreColliders[i], colliders.ignoreColliders[j]);
                    }
                }
            }
        }
        #endregion

        #region Bone Calculations
        /// <summary>
        /// Calculate center of mass of all the bones.
        /// </summary>
        public Vector3 CalculateCoM()
        {
            Vector3 com = Vector3.zero;
            float sum = 0f;

            for (int i = 0; i < allBones.Length; i++)
            {
                BoneInfo bone = allBones[i];
                com += bone.obj.position * bone.body.mass;
                sum += bone.body.mass;
            }
            com /= sum;
            return com;
        }
        #endregion
    }

    [Serializable]
    public class BoneInfo
    {
        public Transform obj = null;
        public ConfigurableJoint joint = null;
        public Rigidbody body = null;
        public Collider collider = null;
    }

    [Serializable]
    public class LimbInfo
    {
        public BoneInfo upper = null;
        public BoneInfo lower = null;
        public BoneInfo end = null;
    }

    [Serializable]
    public class LegInfo
    {
        public LimbInfo leftLeg = null;
        public LimbInfo rightLeg = null;
    }

    [Serializable]
    public class IgnoreCollider
    {
        public Collider[] ignoreColliders = null;
    }
}
