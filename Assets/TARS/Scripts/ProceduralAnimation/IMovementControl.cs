using System;
using UnityEngine;

namespace TARS
{
    public struct MoveData
    {
        public Vector3 MoveDir;
        public Vector3 BodyForward;
        public float TargetSpeed;
        public float CurrentSpeed;
        public bool IsRunning;
    }
    
    public interface IMovementControl
    {
        event Action<MoveData> OnMove;
        
        void Move(Vector3 moveInput);
        void RepositionBody(Vector3 centerPoint);
        void InvokeOnMove(MoveData moveData);
    }
}
