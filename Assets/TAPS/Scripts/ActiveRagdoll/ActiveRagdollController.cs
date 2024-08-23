using UnityEngine;

namespace TAPS.ActiveRagdoll
{
    public enum CocoState { Idle, TargetDetected, FellDown }
    
    public class ActiveRagdollController : MonoBehaviour
    {
        [SerializeField] private Ragdoll ragdoll = null;
        [SerializeField] private Transform target = null;

        private CreatureManager creatureManager = null;
        private CocoState currentCocoState = CocoState.Idle;
        
        public CocoState CurrentCocoState => currentCocoState;

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
        
        private void MoveTowardsTarget()
        {
            ragdoll.Rotate(target);
            ragdoll.Walk();
        }

        private void OnDisable()
        {
            creatureManager.OnUpdate -= ragdoll.UpdateCoM;
            creatureManager.OnUpdate -= ragdoll.CheckBalanceState;

            creatureManager.OnUpdate -= CocoStateChanger;
        }
    }
}
