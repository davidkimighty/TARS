using System;
using System.Collections;
using UnityEngine;

namespace TARS
{
    public enum Side { Left, Right }
    
    public class Leg : MonoBehaviour
    {
        [SerializeField] private Side _side;
        [SerializeField] private LegPreset _legPreset;
        [SerializeField] private Transform _rayOrigin;
        [SerializeField] private Transform _ikTarget;
        [SerializeField] private Transform _foot;
        [SerializeField] private Vector3 _footForwardAdjust = Vector3.one;

        private bool _isMoving;
        private Vector3 _targetPos;
        private IEnumerator _swingCoroutine;
        
#if UNITY_EDITOR
        private Vector3 _gizmosMoveDir;
        private float _gizmosStrideLength;
        private Vector3 _gizmosTargetPoint;
#endif
        
        public Side Side => _side;
        public Transform Foot => _foot;
        public Leg NextLeg { get; set; }
        public bool CanTakeStep { get; set; }

        private void OnEnable()
        {
            if (Physics.Raycast(_rayOrigin.position, Vector3.down, out RaycastHit hit, _legPreset.RayLength, _legPreset.GroundMask))
            {
                _targetPos = hit.point;
                _targetPos.y += _legPreset.FootHeightOffset;
                _ikTarget.position = _targetPos;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_rayOrigin == null) return;
            
            Gizmos.color = Color.yellow;
            Vector3 castPoint = _rayOrigin.position + _gizmosMoveDir * _gizmosStrideLength;
            
            Gizmos.DrawSphere(castPoint, 0.1f);
            Gizmos.DrawRay(castPoint, Vector3.down * _legPreset.RayLength);
            
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_gizmosTargetPoint, 0.1f);
        }

        public bool CanSwing(Vector3 moveDir, bool isRunning, out Vector3 targetPoint)
        {
            Vector3 flat = new Vector3(1, 0, 1);
            float dist = Vector3.Distance(Vector3.Scale(_ikTarget.position, flat), Vector3.Scale(_rayOrigin.position, flat));
            bool overThreshold = dist > _legPreset.StepThreshold;
            bool canSwing = !_isMoving && CanTakeStep && overThreshold;
            
            float strideLength = isRunning ? _legPreset.RunStepLength : _legPreset.WalkStepLength;
            Vector3 castPoint = _rayOrigin.position + moveDir * strideLength;

            canSwing &= Physics.Raycast(castPoint, Vector3.down, out RaycastHit hit,
                _legPreset.RayLength, _legPreset.GroundMask);
            targetPoint = hit.point;
            
#if UNITY_EDITOR
            _gizmosMoveDir = moveDir;
            _gizmosStrideLength = strideLength;
            if (canSwing)
                _gizmosTargetPoint = targetPoint;
#endif
            return canSwing;
        }
        
        public void ExecuteSwing(Vector3 bodyForward, Vector3 targetPos, float stepDuration, float legDelay)
        {
            Debug.Log($"{Side} Leg Swing");
            _isMoving = true;
            _swingCoroutine = PerformSwing(bodyForward, targetPos, stepDuration, legDelay);
            StartCoroutine(_swingCoroutine);
        }

        private IEnumerator PerformSwing(Vector3 bodyForward, Vector3 targetPos, float stepDuration, float legDelay)
        {
            float elapsedTime = 0f;
            Vector3 startPos = transform.position;
            targetPos.y += _legPreset.FootHeightOffset;
            Quaternion footStartRot = _ikTarget.rotation;
            
            while (elapsedTime < stepDuration)
            {
                float fraction = elapsedTime / stepDuration;
                float stepSpeedFrac = _legPreset.StepSpeedCurve.Evaluate(fraction);
                float stepHeightFrac = _legPreset.StepHeightCurve.Evaluate(fraction);
                float sinFrac = Mathf.Sin(fraction / 2f * Mathf.PI * 2f);

                Vector3 offset = transform.position - startPos;
                Vector3 newPos = Vector3.Lerp(_ikTarget.position, targetPos + offset, stepSpeedFrac);
                newPos.y += _legPreset.StepHeight * sinFrac * stepHeightFrac;
                _ikTarget.position = newPos;
                
                Quaternion moveLookRot = Quaternion.LookRotation(bodyForward, Vector3.up);
                moveLookRot *= Quaternion.Euler(_footForwardAdjust);
                _ikTarget.rotation =  Quaternion.Lerp(footStartRot, moveLookRot, stepSpeedFrac);

                float swingFootAngle = _legPreset.SwingFootAngle * sinFrac;
                _ikTarget.rotation = footStartRot *  Quaternion.Euler(swingFootAngle, 0f, 0f);

                if (elapsedTime > legDelay)
                    NextLeg.CanTakeStep = true;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            _targetPos = targetPos + (transform.position - startPos);
            _ikTarget.position = _targetPos;
            _ikTarget.rotation = Quaternion.LookRotation(bodyForward, Vector3.up) * Quaternion.Euler(_footForwardAdjust);
            
            CanTakeStep = _isMoving = false;
            NextLeg.CanTakeStep = true;
        }
    }
}
