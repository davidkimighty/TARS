using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollieLab.ActiveRagdoll
{
    public static class RagdollHelper
    {
        /// <summary>
        /// 
        /// </summary>
        public static void ToggleLegState(this ref LegState state)
        {
            state = state == LegState.Swing ? LegState.Stance : LegState.Swing;
        }

    }
}
