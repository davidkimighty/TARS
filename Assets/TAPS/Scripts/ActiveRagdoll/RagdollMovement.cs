using System;
using System.Collections;
using UnityEngine;

namespace TAPS.ActiveRagdoll
{
    public enum LegState { Swing, Stance }
    
    [Serializable]
    public class Movement
    {
        public bool isExecuting = false;
        public Coroutine movement = null;
    }

    [Serializable]
    public class LegMovement
    {
        public LegState leftLegState = LegState.Swing;
        public LegState rightLegState = LegState.Stance;
        public Movement leftLegMovement = null;
        public Movement rightLegMovement = null;
    }
    
    public class RagdollMovement : MonoBehaviour
    {
        [SerializeField] private JointDrives jointDrives = null;
        [SerializeField] private LimbTargetRotations swingRotations = null;
        [SerializeField] private LimbTargetRotations stanceRotations = null;

        private LegMovement[] legMovements = null;

        public void InitMovements(LegInfo[] legInfo)
        {
            legMovements = new LegMovement[legInfo.Length];
            for (int i = 0; i < legMovements.Length; i++)
            {
                LegMovement movement = legMovements[i] = new LegMovement();
                if (i % 2 == 0)
                {
                    ToggleLegState(ref movement.leftLegState);
                    ToggleLegState(ref movement.rightLegState);
                }
                movement.leftLegMovement = new Movement();
                movement.rightLegMovement = new Movement();
            }
        }

        public void SetJointPowerAll(BoneInfo[] allBones, JointStrength strength)
        {
            for (int i = 0; i < allBones.Length; i++)
                SetJointPower(allBones[i], strength);
        }

        public void SetJointPower(BoneInfo bone, JointStrength strength)
        {
            JointDrive jointDrive = jointDrives.GetJointDrive(strength);
            ConfigurableJoint joint = bone.joint;
            joint.angularXDrive = jointDrive;
            joint.angularYZDrive = jointDrive;
        }

        public void RotateTowardsTarget(Transform target, BoneInfo bone, float speed)
        {
            Vector3 vecToTarget = target.position - bone.obj.position;
            Vector3 vecToTargetFlat = Vector3.Scale(vecToTarget, new Vector3(1, 0, 1));
            Quaternion lookRotation = Quaternion.Inverse(Quaternion.LookRotation(vecToTargetFlat));
            Quaternion lerpValue = Quaternion.Lerp(bone.joint.targetRotation, lookRotation, speed * Time.fixedDeltaTime);
            bone.joint.targetRotation = lerpValue;
        }

        public void LegMovements(LegInfo[] legs)
        {
            if (legs.Length != legMovements.Length) return;

            for (int i = 0; i < legs.Length; i++)
            {
                LegMovement leg = legMovements[i];
                LimbTargetRotations leftRot = leg.leftLegState == LegState.Swing ? swingRotations : stanceRotations;
                LimbTargetRotations rightRot = leg.rightLegState == LegState.Swing ? swingRotations : stanceRotations;

                leg.leftLegMovement.movement = StartCoroutine(RotateLimbJoints(legs[i].leftLeg, leftRot, leg.leftLegMovement, () =>
                {
                    ToggleLegState(ref leg.leftLegState);
                }));

                leg.rightLegMovement.movement = StartCoroutine(RotateLimbJoints(legs[i].rightLeg, rightRot, leg.rightLegMovement, () =>
                {
                    ToggleLegState(ref leg.rightLegState);
                }));
            }
        }

        private IEnumerator RotateLimbJoints(LimbInfo lim, LimbTargetRotations limbRot, Movement movement, Action executed)
        {
            if (movement.isExecuting) yield break;
            movement.isExecuting = true;

            for (int i = 0; i < limbRot.targetRotations.Length; i++)
            {
                LimbTargetRotation targetRot = limbRot.targetRotations[i];
                float elapsedTime = 0f;

                while (elapsedTime < targetRot.Duration)
                {
                    elapsedTime += Time.fixedDeltaTime;
                    float perc = elapsedTime / targetRot.Duration;

                    lim.upper.joint.targetRotation = Quaternion.Lerp(lim.upper.joint.targetRotation, targetRot.UpperRot, perc);
                    lim.lower.joint.targetRotation = Quaternion.Lerp(lim.lower.joint.targetRotation, targetRot.LowerRot, perc);
                    lim.end.joint.targetRotation = Quaternion.Lerp(lim.end.joint.targetRotation, targetRot.EndRot, perc);
                    yield return new WaitForFixedUpdate();
                }
                lim.upper.joint.targetRotation = targetRot.UpperRot;
                lim.lower.joint.targetRotation = targetRot.LowerRot;
                lim.end.joint.targetRotation = targetRot.EndRot;
            }
            movement.isExecuting = false;
            executed?.Invoke();
        }
        
        private void ToggleLegState(ref LegState state)
        {
            state = state == LegState.Swing ? LegState.Stance : LegState.Swing;
        }
    }
}
