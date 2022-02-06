using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CollieLab.ActiveRagdoll
{
    public class CreatureManager : MonoBehaviour
    {
        #region Satatic Field
        
        #endregion

        #region Delegates
        public Action OnFixedUpdate = null;
        public Action OnUpdate = null;
        #endregion

        private void Awake()
        {
            //Application.targetFrameRate = 60;
        }

        private void FixedUpdate()
        {
            OnFixedUpdate?.Invoke();
        }

        private void Update()
        {
            OnUpdate?.Invoke();
        }
    }
}
