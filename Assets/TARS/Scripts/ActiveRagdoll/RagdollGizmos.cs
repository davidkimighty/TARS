using UnityEngine;

namespace TARS.ActiveRagdoll
{
    public class RagdollGizmos : MonoBehaviour
    {
        [SerializeField] private Ragdoll ragdoll = null;

        private void OnDrawGizmos()
        {
            if (ragdoll == null) { return; }

            DrawCoM();
        }

        private void DrawCoM()
        {
            Transform comObjTransform = ragdoll.CoMObj.transform;

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(comObjTransform.position, 0.03f);

            Gizmos.color = Color.green;
            Gizmos.DrawRay(comObjTransform.position, comObjTransform.forward);

            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(comObjTransform.position, comObjTransform.right);
        }
    }
}
