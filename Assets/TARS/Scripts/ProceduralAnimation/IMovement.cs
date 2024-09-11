using System;
using UnityEngine;

namespace TARS
{
    [Serializable]
    public class MoveData
    {
        public Vector3 MoveDir;
        public Vector3 BodyForward;
        public float TargetSpeed;
        public float CurrentSpeed;
        public bool IsRunning;

        public void UpdateData(MoveData data)
        {
            MoveDir = data.MoveDir;
            BodyForward = data.BodyForward;
            TargetSpeed = data.TargetSpeed;
            CurrentSpeed = data.CurrentSpeed;
            IsRunning = data.IsRunning;
        }
    }
    
    public interface IMovement
    {
        event Action<MoveData> OnMove;
        
        void Move(Vector3 moveInput);
        void RepositionBody(Vector3 centerPoint);
        void InvokeOnMove(MoveData moveData);
        void Rotate(Vector3 moveDir, ref Quaternion lookRot);
        void Jump();
    }
}
