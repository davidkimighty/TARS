using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TARS
{
    public struct MoveData
    {
        public Vector3 MoveDir;
        public float TargetSpeed;
        public float CurrentSpeed;
        public bool IsRunning;
    }
    
    public abstract class MovementController : MonoBehaviour
    {
        public event Action<MoveData> OnMove;

        protected Vector3 _moveInput;
        protected bool _hasInput;
        protected Vector3 _moveDir;
        protected Vector3 _currentVel;
        protected float _targetSpeed;

        public abstract void Move(Vector3 moveInput);

        public abstract void RepositionBody(Vector3 centerPoint);

        public virtual void InvokeOnMove(MoveData moveData) => OnMove?.Invoke(moveData);
        
        protected Vector3 CalculateCenterPoint(List<Transform> points)
        {
            return points.Aggregate(Vector3.zero, (current, point) => current + point.position) / points.Count;
        }
    }
}
