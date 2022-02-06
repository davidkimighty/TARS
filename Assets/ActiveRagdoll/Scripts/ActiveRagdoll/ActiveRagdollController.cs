using UnityEngine;

namespace CollieLab.ActiveRagdoll
{
    public class ActiveRagdollController : MonoBehaviour
    {
        #region Serialized Field
        [SerializeField] private Ragdoll ragdoll = null;
        [SerializeField] private Transform target = null;
        #endregion

        #region Private Field
        private CreatureManager creatureManager = null;

        private CocoState currentCocoState = CocoState.Idle;
        public CocoState CurrentCocoState
        {
            get => currentCocoState;
        }
        #endregion

        private void Awake()
        {
            creatureManager = FindObjectOfType<CreatureManager>();
        }

        private void OnEnable()
        {
            creatureManager.OnUpdate += ragdoll.UpdateCoM;
            creatureManager.OnUpdate += ragdoll.CheckBalanceState;

            creatureManager.OnUpdate += CocoStateChanger;
        }

        #region Coco Behaviors
        /// <summary>
        /// Change Coco's behavior by current state.
        /// </summary>
        private void CocoStateChanger()
        {
            if (ragdoll.FellDown && currentCocoState != CocoState.FellDown)
            {
                currentCocoState = CocoState.FellDown;
                creatureManager.OnFixedUpdate -= MoveTowardsTarget;

                ragdoll.FallDown();
                StartCoroutine(ragdoll.GetUp(3f, () =>
                {
                    currentCocoState = CocoState.Idle;
                    
                }));
            }

            switch (currentCocoState)
            {
                case CocoState.Idle:
                    if (!ragdoll.InViewAngle)
                        ragdoll.LookForTarget(target);

                    if (ragdoll.InViewAngle)
                    {
                        currentCocoState = CocoState.TargetDetected;
                        creatureManager.OnFixedUpdate += MoveTowardsTarget;
                    }
                    break;

                case CocoState.TargetDetected:
                    if (ragdoll.InViewAngle)
                        ragdoll.LookForTarget(target);

                    if (!ragdoll.InViewAngle)
                    {
                        currentCocoState = CocoState.Idle;
                        creatureManager.OnFixedUpdate -= MoveTowardsTarget;
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Keep walking towards target.
        /// </summary>
        private void MoveTowardsTarget()
        {
            ragdoll.Rotate(target);
            ragdoll.Walk();
        }
        #endregion

        private void OnDisable()
        {
            creatureManager.OnUpdate -= ragdoll.UpdateCoM;
            creatureManager.OnUpdate -= ragdoll.CheckBalanceState;

            creatureManager.OnUpdate -= CocoStateChanger;
        }
    }

    public enum CocoState { Idle, TargetDetected, FellDown }
}
