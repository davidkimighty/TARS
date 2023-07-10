using System;
using System.Collections;
using UnityEngine;

namespace SimpleActiveRagdoll
{
    public class IkLowerBody : MonoBehaviour
    {
        [SerializeField] private Transform _body = null;
        [SerializeField] private IkLeg _leftLeg = null;
        [SerializeField] private IkLeg _rightLeg = null;
        [SerializeField] private DominentLeg _dominentLeg = DominentLeg.Left;

        private IkLeg _primaryLeg = null;
        private IkLeg _secondaryLeg = null;
        private bool _legMoving = false;
        private float _leftRightDelay = 0.5f;

        private IEnumerator _legMovement = null;

        #region Public Functions
        public void Init()
        {
            _leftLeg.Init();
            _rightLeg.Init();

            if (_dominentLeg == DominentLeg.Left)
            {
                _primaryLeg = _leftLeg;
                _secondaryLeg = _rightLeg;
            }
            else
            {
                _primaryLeg = _rightLeg;
                _secondaryLeg = _leftLeg;
            }
        }

        public void MoveLegs(IkLegMovementPreset preset)
        {
            if (_legMoving) return;
            _legMoving = true;

            _legMovement = LegMovement(preset, () =>
            {
                _legMoving = false;
            });
            StartCoroutine(_legMovement);
        }

        #endregion

        private IEnumerator LegMovement(IkLegMovementPreset preset, Action done)
        {
            _primaryLeg.MoveLeg(_body, preset);

            float delay = preset.StepDuration * _leftRightDelay;
            yield return new WaitForSeconds(delay);

            _secondaryLeg.MoveLeg(_body, preset);

            done?.Invoke();
        }
    }

    public enum DominentLeg { Left, Right }
    public enum LegMovementState { Contact, Low, Passing, High }
}
