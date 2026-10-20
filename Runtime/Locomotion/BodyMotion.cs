using System;
using UnityEngine;

namespace TARS.Locomotion
{
    public abstract class BodyMotion : MonoBehaviour
    {
        private BlendWeight _blend = BlendWeight.On;
        private bool _wasOff;
        private Vector3 _targetOffset;
        private Quaternion _targetTurn = Quaternion.identity;

        public float Weight => _blend.Value;
        public Vector3 TargetOffset => _targetOffset;
        public Quaternion TargetTurn => _targetTurn;

        public void SetActive(bool active, float blendTime)
        {
            _blend.Set(active, blendTime);
        }

        public void Pose(LegLocomotion locomotion, float deltaTime)
        {
            _blend.Advance(deltaTime);
            if (_blend.IsOff)
            {
                _wasOff = true;
                return;
            }
            ApplyPose(locomotion, _wasOff ? 0f : deltaTime, _blend.Value);
            _wasOff = false;
        }

        public virtual Vector3 RestWorldPoint(Transform owner, Vector3 worldPoint)
        {
            return worldPoint;
        }

        public virtual Vector3 RestWorldDirection(Transform owner, Vector3 worldDirection)
        {
            return worldDirection;
        }

        protected abstract void ApplyPose(LegLocomotion locomotion, float deltaTime, float weight);

        protected void Output(LegLocomotion locomotion, Transform moved, Vector3 restLocalPosition, Quaternion restLocalRotation, Vector3 offset, Quaternion turn, float weight)
        {
            if (locomotion.PhysicsBody != null)
            {
                _targetOffset = offset;
                _targetTurn = turn;
                return;
            }
            Quaternion bodyRotation = locomotion.BodyRotation;
            PoseMoved(moved, restLocalPosition, restLocalRotation, bodyRotation, offset, turn, weight);
            CorrectReach(moved, locomotion.States, locomotion.Legs, bodyRotation * Vector3.up, weight);
        }

        protected static Vector3 RestPoint(Transform moved, Vector3 restLocalPosition, Quaternion restLocalRotation, Transform owner, Vector3 worldPoint)
        {
            if (!owner.IsChildOf(moved)) return worldPoint;
            Vector3 restLocal = restLocalPosition + restLocalRotation * Vector3.Scale(moved.InverseTransformPoint(worldPoint), moved.localScale);
            return moved.parent != null ? moved.parent.TransformPoint(restLocal) : restLocal;
        }

        protected static Vector3 RestDirection(Transform moved, Quaternion restLocalRotation, Transform owner, Vector3 worldDirection)
        {
            if (!owner.IsChildOf(moved)) return worldDirection;
            Vector3 restLocal = restLocalRotation * moved.InverseTransformDirection(worldDirection);
            return moved.parent != null ? moved.parent.rotation * restLocal : restLocal;
        }

        protected static Vector3 RestOrigin(LegLocomotion locomotion, Transform moved, Vector3 restLocalPosition)
        {
            return locomotion.ToBodyPoint(moved.parent != null ? moved.parent.TransformPoint(restLocalPosition) : restLocalPosition);
        }

        protected static void PoseMoved(Transform moved, Vector3 restLocalPosition, Quaternion restLocalRotation, Quaternion bodyRotation, Vector3 offset, Quaternion turn, float weight)
        {
            Transform parent = moved.parent;
            Quaternion parentInBody = Quaternion.Inverse(bodyRotation) * (parent != null ? parent.rotation : Quaternion.identity);
            Vector3 position = restLocalPosition + ToParentVector(parent, bodyRotation * offset);
            Quaternion rotation = Quaternion.Inverse(parentInBody) * turn * parentInBody * restLocalRotation;
            if (weight < 1f)
            {
                position = Vector3.Lerp(moved.localPosition, position, weight);
                rotation = Quaternion.Slerp(moved.localRotation, rotation, weight);
            }
            moved.localPosition = position;
            moved.localRotation = rotation;
        }

        protected static void CorrectReach(Transform moved, ReadOnlySpan<LegState> states, ReadOnlySpan<LegRig> legs, Vector3 up, float weight)
        {
            float correction = 0f;
            for (int i = 0; i < legs.Length; i++)
            {
                if (states[i].IsSwinging || !legs[i].transform.IsChildOf(moved)) continue;
                float reach = states[i].ChainLength;
                Vector3 toAnkle = states[i].SupportAnkle - legs[i].Upper.position;
                float vertical = Vector3.Dot(toAnkle, up);
                float horizontalSqr = (toAnkle - up * vertical).sqrMagnitude;
                if (horizontalSqr >= reach * reach) continue;
                correction = Mathf.Min(correction, vertical + Mathf.Sqrt(reach * reach - horizontalSqr));
            }
            if (correction < 0f)
                moved.localPosition += ToParentVector(moved.parent, up * (correction * weight));
        }

        private static Vector3 ToParentVector(Transform parent, Vector3 worldVector)
        {
            return parent != null ? parent.InverseTransformVector(worldVector) : worldVector;
        }
    }
}
