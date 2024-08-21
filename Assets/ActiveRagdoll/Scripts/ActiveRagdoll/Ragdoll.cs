using System;
using System.Collections;
using UnityEngine;

namespace CollieLab.ActiveRagdoll
{
    public class Ragdoll : MonoBehaviour
    {
        #region Serialized Field
        [Header("Ragdoll Settings")]
        [SerializeField] private RagdollArmature armature = null;
        [SerializeField] private RagdollMovement movement = null;
        [SerializeField] private RagdollSensor sensors = null;
        [SerializeField] private RagdollPresets presets = null;

        [SerializeField] private GameObject comObj = null;
        public GameObject CoMObj
        {
            get => comObj;
            set => comObj = value;
        }
        [SerializeField] private GameObject normalFace = null;
        [SerializeField] private GameObject xFace = null;
        #endregion

        #region Private Field
        private Vector3 comInitialValue = Vector3.zero;

        private Vector3 com = Vector3.zero;
        public Vector3 CoM
        {
            get => com;
        }

        private float balanceValue = 0f;
        public float BalanceValue
        {
            get => balanceValue;
        }

        private bool outOfBalance = false;
        public bool OutOfBalance
        {
            get => outOfBalance;
        }

        private bool fellDown = false;
        public bool FellDown
        {
            get => fellDown;
        }

        private bool inViewAngle = false;
        public bool InViewAngle
        {
            get => inViewAngle;
        }
        #endregion

        private void Awake()
        {
            armature.GatherBones();
            armature.FindKeyBones();
            armature.IgnoreColliders();

            movement.SetJointPowerAll(armature.AllBones, JointStrength.Strong);
            movement.InitMovements(armature.Legs);
        }

        #region Ragdoll Calculations
        /// <summary>
        /// Update position & rotation of CoM gameobject.
        /// </summary>
        public void UpdateCoM()
        {
            comObj.transform.position = armature.CalculateCoM();
            com = comObj.transform.InverseTransformPoint(armature.RootBone.body.position);

            if (comInitialValue == Vector3.zero)
                comInitialValue = com;

            CoMObj.transform.rotation = armature.RootBone.body.rotation;
        }

        /// <summary>
        /// Update ragdoll balance state.
        /// </summary>
        public void CheckBalanceState()
        {
            balanceValue = Vector3.Distance(com, comInitialValue);
            outOfBalance = balanceValue > presets.OutOfBalanceThreshold;
            fellDown = !fellDown && balanceValue > presets.FallDownThreshold;
        }
        #endregion

        #region Ragdoll Behaviors
        /// <summary>
        /// Ragdoll look for given target.
        /// </summary>
        public void LookForTarget(Transform target)
        {
            if (target == null) return;
            inViewAngle = sensors.IsTargetInViewAngle(armature.Head[0].obj, target, presets.ViewAngle);
        }

        /// <summary>
        /// Ragdoll rotate towards target.
        /// </summary>
        public void Rotate(Transform target)
        {
            if (target == null) return;
            movement.RotateTowardsTarget(target, armature.RootBone, presets.RotationSpeed);
        }

        /// <summary>
        /// Ragoll walk movement.
        /// </summary>
        public void Walk()
        {
            movement.LegMovements(armature.Legs);
        }

        /// <summary>
        /// 
        /// </summary>
        public void FallDown()
        {
            movement.SetJointPowerAll(armature.AllBones, JointStrength.None);
            movement.SetJointPower(armature.RootBone, JointStrength.None);
            normalFace.SetActive(false);
            xFace.SetActive(true);
        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerator GetUp(float getupAfterSeconds, Action done)
        {
            yield return new WaitForSeconds(getupAfterSeconds);

            movement.SetJointPowerAll(armature.AllBones, JointStrength.Strong);
            done?.Invoke();
        }
        #endregion
    }

    public enum RagdollState { Idle, Walk, Run, FellDown }
}
