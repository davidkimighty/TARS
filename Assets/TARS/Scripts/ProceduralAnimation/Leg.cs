using System.Collections;
using UnityEngine;

namespace TARS
{
    public enum Side { Left, Right }
    
    public class Leg : MonoBehaviour
    {
        [SerializeField] private Side _side;
        [SerializeField] private bool _inverse;
        [SerializeField] private LegPreset _legPreset;
        [SerializeField] private Transform _rayOrigin;
        [SerializeField] private Transform _ikTarget;
        [SerializeField] private Transform _foot;
        [SerializeField] private Vector3 _footForwardAdjust = Vector3.one;

        private bool _isMoving;
        private Vector3 _targetPos;
        private IEnumerator _swingCoroutine;
        
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

        public bool CanSwing()
        {
            Vector3 flat = new Vector3(1, 0, 1);
            float dist = Vector3.Distance(Vector3.Scale(_ikTarget.position, flat), Vector3.Scale(_rayOrigin.position, flat));
            bool overThreshold = dist > _legPreset.StepThreshold;
            return !_isMoving && CanTakeStep && overThreshold;
        }
        
        public void ExecuteSwing(MoveData moveData, float stepDuration, float legDelay)
        {
            // Debug.Log($"{Side} Leg Swing");
            _isMoving = true;
            _swingCoroutine = PerformSwing(moveData, stepDuration, legDelay);
            StartCoroutine(_swingCoroutine);
        }

        private IEnumerator PerformSwing(MoveData moveData, float stepDuration, float legDelay)
        {
            float elapsedTime = 0f;
            Vector3 startPos = transform.position;
            Quaternion footStartRot = _ikTarget.rotation;
            _targetPos = GetTargetPosition(moveData, startPos);
            
            while (elapsedTime < stepDuration)
            {
                float fraction = elapsedTime / stepDuration;
                float stepSpeedFrac = _legPreset.StepSpeedCurve.Evaluate(fraction);
                float stepHeightFrac = _legPreset.StepHeightCurve.Evaluate(fraction);
                float sinFrac = Mathf.Sin(fraction / 2f * Mathf.PI * 2f);

                Vector3 offset = transform.position - startPos;
                Vector3 newPos = Vector3.Lerp(_ikTarget.position, _targetPos + offset, stepSpeedFrac);
                newPos.y += _legPreset.StepHeight * sinFrac * stepHeightFrac;
                _ikTarget.position = newPos;

                Quaternion moveLookRot = GetTargetRotation(moveData);
                _ikTarget.rotation =  Quaternion.Lerp(footStartRot, moveLookRot, stepSpeedFrac);
                float footAngle = _legPreset.SwingFootAngle * sinFrac * (_inverse ? -1f : +1f);
                _ikTarget.rotation = footStartRot *  Quaternion.Euler(footAngle, 0f, 0f);

                if (elapsedTime > legDelay)
                    NextLeg.CanTakeStep = true;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            _ikTarget.position = _targetPos + (transform.position - startPos);
            _ikTarget.rotation = GetTargetRotation(moveData);
            
            CanTakeStep = _isMoving = false;
            NextLeg.CanTakeStep = true;
        }

        private Vector3 GetTargetPosition(MoveData moveData, Vector3 startPos)
        {
            float strideLength = moveData.IsRunning ? _legPreset.RunStepLength : _legPreset.WalkStepLength;
            Vector3 castPoint = _rayOrigin.position + moveData.MoveDir * strideLength;

            Vector3 targetPos = Physics.Raycast(castPoint, Vector3.down, out RaycastHit hit,
                _legPreset.RayLength, _legPreset.GroundMask) ? hit.point : _targetPos;
            
            targetPos.y += _legPreset.FootHeightOffset;
            return targetPos + (transform.position - startPos);
        }

        private Quaternion GetTargetRotation(MoveData moveData)
        {
            return Quaternion.LookRotation(moveData.BodyForward, Vector3.up) * Quaternion.Euler(_footForwardAdjust);
        }
    }
}
