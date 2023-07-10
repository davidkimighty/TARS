using System.Collections;
using UnityEngine;

namespace SimpleActiveRagdoll
{
    [DisallowMultipleComponent]
    public class IkRig : MonoBehaviour
    {
        [SerializeField] private IkLowerBody[] _lowerBodies = null;
        [SerializeField] private IkLegMovementPreset _legPreset = null;

        private float _rearDelay = 0.75f;

        private IEnumerator _lowerBodyMovement = null;

        private void Awake()
        {
            for (int i = 0; i < _lowerBodies.Length; i++)
                _lowerBodies[i].Init();
        }

        private void Start()
        {
            _lowerBodyMovement = LowerBodyMovement();
            StartCoroutine(_lowerBodyMovement);
        }

        private IEnumerator LowerBodyMovement()
        {
            float elapsedTime = 0f;
            int index = 0;

            while (true)
            {
                float delay = _legPreset.StepDuration * _rearDelay;
                if (elapsedTime > delay)
                {
                    _lowerBodies[index++ % _lowerBodies.Length].MoveLegs(_legPreset);
                    elapsedTime = 0;
                }
                elapsedTime += Time.fixedDeltaTime;

                yield return new WaitForFixedUpdate();
            }
        }
    }
}
