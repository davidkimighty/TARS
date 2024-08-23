using System;
using System.Collections;
using UnityEngine;

namespace TAPS.ActiveRagdoll
{
    public enum RagdollState { Idle, Walk, Run, FellDown }
    
    public class Ragdoll : MonoBehaviour
    {
        [Header("Ragdoll Settings")]
        [SerializeField] private RagdollArmature armature = null;
        [SerializeField] private RagdollMovement movement = null;
        [SerializeField] private RagdollSensor sensors = null;
        [SerializeField] private RagdollPresets presets = null;

        [SerializeField] private GameObject comObj = null;
        [SerializeField] private GameObject normalFace = null;
        [SerializeField] private GameObject xFace = null;

        private Vector3 comInitialValue = Vector3.zero;
        private Vector3 com = Vector3.zero;
        private float balanceValue = 0f;
        private bool outOfBalance = false;
        private bool fellDown = false;
        private bool inViewAngle = false;
        
        public GameObject CoMObj => comObj;
        public bool FellDown => fellDown;
        public bool InViewAngle => inViewAngle;

        private void Awake()
        {
            armature.GatherBones();
            armature.FindKeyBones();
            armature.IgnoreColliders();

            movement.SetJointPowerAll(armature.AllBones, JointStrength.Strong);
            movement.InitMovements(armature.Legs);
        }

        public void UpdateCoM()
        {
            comObj.transform.position = armature.CalculateCoM();
            com = comObj.transform.InverseTransformPoint(armature.RootBone.body.position);

            if (comInitialValue == Vector3.zero)
                comInitialValue = com;

            CoMObj.transform.rotation = armature.RootBone.body.rotation;
        }

        public void CheckBalanceState()
        {
            balanceValue = Vector3.Distance(com, comInitialValue);
            outOfBalance = balanceValue > presets.OutOfBalanceThreshold;
            fellDown = !fellDown && balanceValue > presets.FallDownThreshold;
        }

        public void LookForTarget(Transform target)
        {
            if (target == null) return;
            inViewAngle = sensors.IsTargetInViewAngle(armature.Head[0].obj, target, presets.ViewAngle);
        }

        public void Rotate(Transform target)
        {
            if (target == null) return;
            movement.RotateTowardsTarget(target, armature.RootBone, presets.RotationSpeed);
        }

        public void Walk()
        {
            movement.LegMovements(armature.Legs);
        }

        public void FallDown()
        {
            movement.SetJointPowerAll(armature.AllBones, JointStrength.None);
            movement.SetJointPower(armature.RootBone, JointStrength.None);
            normalFace.SetActive(false);
            xFace.SetActive(true);
        }

        public IEnumerator GetUp(float getupAfterSeconds, Action done)
        {
            yield return new WaitForSeconds(getupAfterSeconds);

            movement.SetJointPowerAll(armature.AllBones, JointStrength.Strong);
            done?.Invoke();
        }
    }

    
}
