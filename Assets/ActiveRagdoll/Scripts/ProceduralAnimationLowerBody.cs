using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace CollieMollie.ActiveRagdoll
{
    public class ProceduralAnimationLowerBody : MonoBehaviour
    {
        #region Variable Field
        [SerializeField] private Transform _body = null;
        [SerializeField] private LayerMask _groundMask;
        [SerializeField] private float _stepDistance = 0.6f;
        [SerializeField] private float _stepLength = 1f;
        [SerializeField] private float _stepHeight = 0.3f;
        [SerializeField] private Vector3 _footOffset = default;
        [SerializeField] private float _legMovementSpeed = 3f;
        [SerializeField] private AnimationCurve _legMovementCurve = null;

        [SerializeField] private TwoBoneIkInfo _ikInfoLeft = null;
        [SerializeField] private TwoBoneIkInfo _ikInfoRight = null;

        private IkMoveInfo _ikMoveInfoLeft = new IkMoveInfo();
        private IkMoveInfo _ikMoveInfoRight = new IkMoveInfo();
        private CancellationTokenSource _ctsLeft = new CancellationTokenSource();
        private CancellationTokenSource _ctsRight = new CancellationTokenSource();

        #endregion

        private void Start()
        {
            _ikMoveInfoLeft.Init(_ikInfoLeft.IkTarget.position);
            _ikMoveInfoRight.Init(_ikInfoRight.IkTarget.position);
        }

        private void Update()
        {
            UpdateFoot();
        }

        #region Private Functions
        private void UpdateFoot()
        {
            Ray leftFootRay = new Ray(_body.position + (-_body.right * _ikInfoLeft.Spacing), Vector3.down);
            SetNewFootPosition(leftFootRay, _ikInfoLeft, _ikMoveInfoLeft, _ikMoveInfoRight.IsMoving());
            LegMovement(_ikMoveInfoLeft);

            Ray rightFootRay = new Ray(_body.position + (_body.right * _ikInfoRight.Spacing), Vector3.down);
            SetNewFootPosition(rightFootRay, _ikInfoRight, _ikMoveInfoRight, _ikMoveInfoLeft.IsMoving());
            LegMovement(_ikMoveInfoRight);

            void SetNewFootPosition(Ray ray, TwoBoneIkInfo ikInfo, IkMoveInfo ikPoints, bool isOtherFootMoving)
            {
                ikInfo.IkTarget.position = ikPoints.CurrentPosition;
                if (Physics.Raycast(ray, out RaycastHit rayHit, ikInfo.RayLength, _groundMask))
                {
                    ikPoints.RayHitPoint = rayHit.point;
                    if (Vector3.Distance(ikPoints.NewPosition, rayHit.point) > _stepDistance && !isOtherFootMoving)
                    {
                        ikPoints.LerpValue = 0f;
                        int direction = _body.InverseTransformPoint(rayHit.point).z > _body.InverseTransformPoint(ikPoints.NewPosition).z ? 1 : -1;
                        ikPoints.NewPosition = rayHit.point + (_body.forward * _stepLength * direction) + _footOffset;
                    }
                }

                
            }

            void LegMovement(IkMoveInfo ikPoints)
            {
                if (ikPoints.LerpValue < 1)
                {
                    Vector3 footPosition = Vector3.Lerp(ikPoints.OldPosition, ikPoints.NewPosition, ikPoints.LerpValue);
                    footPosition.y += _legMovementCurve.Evaluate(Mathf.Sin(ikPoints.LerpValue * Mathf.PI) * _stepHeight);

                    ikPoints.CurrentPosition = footPosition;
                    ikPoints.LerpValue += Time.deltaTime * _legMovementSpeed;
                }
                else
                {
                    ikPoints.OldPosition = ikPoints.NewPosition;
                }
            }
        }

        #endregion

        [Serializable]
        public class TwoBoneIkInfo
        {
            public Transform IkTarget = null;
            public Transform IkHint = null;
            public float Spacing = 0.2f;
            public float RayLength = 10f;
        }

        [Serializable]
        public class IkMoveInfo
        {
            public Vector3 RayHitPoint = Vector3.zero;
            public Vector3 OldPosition = Vector3.zero;
            public Vector3 CurrentPosition = Vector3.zero;
            public Vector3 NewPosition = Vector3.zero;
            public float LerpValue = 0;

            public void Init(Vector3 position) => OldPosition = CurrentPosition = NewPosition = position;
            public bool IsMoving() => LerpValue < 1;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(_ikMoveInfoLeft.RayHitPoint, 0.03f);
            Gizmos.DrawSphere(_ikMoveInfoRight.RayHitPoint, 0.03f);

            Gizmos.DrawLine(_ikMoveInfoLeft.CurrentPosition, _ikMoveInfoLeft.NewPosition);
            Gizmos.DrawLine(_ikMoveInfoRight.CurrentPosition, _ikMoveInfoRight.NewPosition);

        }
#endif
    }
}
