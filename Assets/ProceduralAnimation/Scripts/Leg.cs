using System.Collections;
using UnityEngine;

namespace SAR
{
    public enum Side { Left, Right }
    
    public class Leg : MonoBehaviour
    {
        [HideInInspector] public Leg NextLeg;
        [HideInInspector] public bool CanTakeStep;

        [SerializeField] private Side _side;
        [SerializeField] private Transform _rayOrigin;
        [SerializeField] private Transform _ikTarget;
        [SerializeField] private Transform _foot;
        [SerializeField] private LegPreset _legPreset;

        private bool _isMoving;
        private Quaternion _footStartRot;
        private Vector3 _targetPos;
        private IEnumerator _swingCoroutine;
        
        public Side Side => _side;
        public Transform Foot => _foot;

        private void Awake()
        {
            if (Physics.Raycast(_rayOrigin.position, Vector3.down, out RaycastHit hit, _legPreset.RayLength, _legPreset.GroundMask))
            {
                _targetPos = hit.point;
                _targetPos.y += _legPreset.FootHeightOffset;
                _ikTarget.position = _targetPos;
            }
            _footStartRot = _foot.rotation;
        }
        
        public bool CanSwing()
        {
            Vector3 flat = new Vector3(1, 0, 1);
            float dist = Vector3.Distance(Vector3.Scale(_ikTarget.position, flat), Vector3.Scale(_rayOrigin.position, flat));
            bool overThreshold = dist > _legPreset.StepThreshold;
            return !_isMoving && CanTakeStep && overThreshold;
        }
        
        public Vector3 CalculateTargetPoint(Vector3 moveDir, bool isRunning)
        {
            float strideLength = isRunning ? _legPreset.RunStepLength : _legPreset.WalkStepLength;
            Vector3 castPoint = _rayOrigin.position + moveDir * strideLength;
            
            if (Physics.Raycast(castPoint, Vector3.down, out RaycastHit hit,_legPreset.RayLength, _legPreset.GroundMask))
                return hit.point;

            castPoint.y = _targetPos.y;
            return castPoint;
        }
        
        public void ExecuteSwing(Vector3 targetPos, float stepDuration, float legDelay)
        {
            _isMoving = true;
            _swingCoroutine = PerformSwing(targetPos, stepDuration, legDelay);
            StartCoroutine(_swingCoroutine);
        }

        private IEnumerator PerformSwing(Vector3 targetPos, float stepDuration, float legDelay)
        {
            float elapsedTime = 0f;
            Vector3 startPos = transform.position;
            targetPos.y += _legPreset.FootHeightOffset;
            
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

                if (_legPreset.EnableSwingFootRotation)
                {
                    float swingFootAngle = _legPreset.SwingFootAngle * sinFrac;
                    _ikTarget.rotation = _footStartRot *  Quaternion.Euler(swingFootAngle, 0f, 0f);
                }

                if (elapsedTime > legDelay)
                    NextLeg.CanTakeStep = true;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            _targetPos = targetPos + (transform.position - startPos);
            _ikTarget.position = _targetPos;
            _ikTarget.rotation = _footStartRot;

            CanTakeStep = _isMoving = false;
            NextLeg.CanTakeStep = true;
        }
    }
}
