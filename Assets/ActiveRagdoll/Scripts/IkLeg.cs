using System;
using System.Collections;
using UnityEngine;

namespace SimpleActiveRagdoll
{
    [DefaultExecutionOrder(-100)]
    public class IkLeg : MonoBehaviour
    {
        [SerializeField] private Transform _ikTarget = null;
        [SerializeField] private Transform _ikHint = null;

        [SerializeField] private Transform _rayOrigin = null;
        [SerializeField] private float _rayLength = 1f;
        [SerializeField] private LayerMask _groundMask;

        private Vector3 _nextStep = Vector3.zero;
        private Vector3 _currentStep = Vector3.zero;
        private Vector3 _rayHitPoint = Vector3.zero;
        private bool _isMoving = false;
        public bool IsMoving
        {
            get => _isMoving;
        }
        public LegState _currentLegState = LegState.Stance;

        private IEnumerator _ikLegMovement = null;
        private IEnumerator _ikTargetUpdate = null;

        #region Public Functions
        public void Init()
        {
            _currentStep = _ikTarget.position;
        }

        public void MoveLeg(Transform body, IkLegMovementPreset preset)
        {
            if (_isMoving) return;

            if (TakeNextStep(body, preset))
            {
                _currentLegState = LegState.Swing;
                _ikLegMovement = LegSwingMovement(_nextStep, preset, () =>
                {
                    _currentLegState = LegState.Stance;
                    _isMoving = false;
                });
                StartCoroutine(_ikLegMovement);
            }

            if (_ikTargetUpdate == null)
            {
                _ikTargetUpdate = UpdateIkTarget();
                StartCoroutine(_ikTargetUpdate);
            }
        }

        public void StopMovement()
        {
            if (_ikLegMovement != null)
                StopCoroutine(_ikLegMovement);

            if (_ikTargetUpdate != null)
            {
                StopCoroutine(_ikTargetUpdate);
                _ikTargetUpdate = null;
            }
        }

        #endregion

        private IEnumerator UpdateIkTarget()
        {
            while (true)
            {
                _ikTarget.position = _currentStep;
                yield return null;
            }
        }

        private bool TakeNextStep(Transform body, IkLegMovementPreset preset)
        {
            Ray ray = new Ray(_rayOrigin.position, _rayOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit rayHit, _rayLength, _groundMask))
            {
                _rayHitPoint = rayHit.point;
                //if ((_nextStep - rayHit.point).magnitude > preset.StepThreshold)
                {
                    int direction = body.InverseTransformPoint(rayHit.point).z > body.InverseTransformPoint(_currentStep).z ? 1 : -1;
                    _nextStep = rayHit.point + (body.forward * preset.StepLength * direction);
                    return true;
                }
            }
            return false;
        }

        private IEnumerator LegSwingMovement(Vector3 targetPoint, IkLegMovementPreset preset, Action done = null)
        {
            float elapsedTime = 0f;
            Vector3 startingPoint = _currentStep;

            while (elapsedTime < preset.StepDuration)
            {
                if (_currentStep == targetPoint) break;

                float t = preset.StepSpeedCurve.Evaluate(elapsedTime / preset.StepDuration);
                Vector3 newPosition = Vector3.LerpUnclamped(startingPoint, targetPoint, t);
                newPosition.y += Mathf.Sin(t * Mathf.PI) * preset.StepHeight;
                _currentStep = newPosition;

                elapsedTime += Time.fixedDeltaTime;
                yield return null;
            }
            _currentStep = targetPoint;
            done?.Invoke();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (_rayOrigin != null)
            {
                Gizmos.DrawWireSphere(_rayOrigin.position, 0.03f);
                Gizmos.DrawRay(_rayOrigin.position, _rayOrigin.forward * _rayLength);
            }
            if (_rayHitPoint != Vector3.zero)
                Gizmos.DrawSphere(_rayHitPoint, 0.03f);
        }
    }

    public enum LegState { None, Swing, Stance }
}
