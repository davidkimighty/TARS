using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollieLab.ActiveRagdoll
{
    public class RagdollGizmos : MonoBehaviour
    {
        #region Serialized Field
        [SerializeField] private Ragdoll ragdoll = null;
        #endregion

        private void OnDrawGizmos()
        {
            if (ragdoll == null) { return; }

            DrawCoM();
        }

        #region Gizmos
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
        #endregion
    }
}
