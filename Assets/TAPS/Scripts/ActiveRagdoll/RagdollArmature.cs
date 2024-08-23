using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TAPS.ActiveRagdoll
{
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
    
    public class RagdollArmature : MonoBehaviour
    {
        [SerializeField] private BoneInfo[] head = null;
        [SerializeField] private BoneInfo[] bodies = null;
        [SerializeField] private LegInfo[] legs = null;
        [SerializeField] private BoneInfo[] tail = null;
        [SerializeField] private IgnoreCollider[] ignoreColliderGroup = null;

        private BoneInfo[] allBones = null;
        private BoneInfo rootBone = null;
        
        public BoneInfo[] Head => head;
        public BoneInfo[] Bodies => bodies;
        public LegInfo[] Legs => legs;
        public BoneInfo[] Tail => tail;
        public BoneInfo[] AllBones => allBones;
        public BoneInfo RootBone => rootBone;

        public void GatherBones()
        {
            List<BoneInfo> bones = new List<BoneInfo>();
            bones.AddRange(head.ToList());
            bones.AddRange(bodies.ToList());
            
            for (int i = 0; i < legs.Length; i++)
            {
                AddLimbs(bones, legs[i].leftLeg);
                AddLimbs(bones, legs[i].rightLeg);
            }
            
            bones.AddRange(tail.ToList());
            allBones = bones.ToArray();
        }

        private void AddLimbs(List<BoneInfo> bones, LimbInfo limb)
        {
            if (limb.upper.obj == null) return;

            bones.Add(limb.upper);
            bones.Add(limb.lower);
            bones.Add(limb.end);
        }
        
        public void FindKeyBones()
        {
            rootBone = bodies[0];
        }
        
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
    }
}
