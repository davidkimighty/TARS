using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace SAR
{
    [Serializable]
    public class LowerBody
    {
        public Transform Body;
        public Leg[] Legs;
    }
    
    public class ProceduralAnimationController : MonoBehaviour
    {
        [SerializeField] private MovementController _movementController;
        [Tooltip("Index 0 is root.")]
        [SerializeField] private LowerBody[] _lowerBodies;
        [SerializeField] private ProceduralAnimationPreset _preset;

        private MoveData _moveData;
        private IEnumerator _LegMovementCoroutine;

        private void Awake()
        {
            if (_movementController != null)
                _movementController.OnMove += ReceiveMoveData;

            if (_lowerBodies != null || _lowerBodies.Length > 0)
            {
                SetLegPriorities();
                _LegMovementCoroutine = LowerBodyMovementLoop();
                StartCoroutine(_LegMovementCoroutine);
            }
        }

        private void ReceiveMoveData(MoveData data)
        {
            bool dirChanged = _moveData.MoveDir == Vector3.zero && _moveData.MoveDir != data.MoveDir;
            if (dirChanged)
            {
                _moveData = data;
                SetLegPriorities();
                return;
            }
            _moveData = data;
        }
        
        private IEnumerator LowerBodyMovementLoop()
        {
            while (true)
            {
                float speedRatio = Mathf.Clamp01(_moveData.CurrentSpeed / _moveData.TargetSpeed);
                float stepDurationFactor = Mathf.Lerp(1f, _preset.MinStepDurationFactor, speedRatio);
                float stepDuration = _preset.StepDuration * stepDurationFactor;
                
                for (int i = 0; i < _lowerBodies.Length; i++)
                {
                    LowerBody lowerBody = _lowerBodies[i];
                    
                    for (int j = 0; j < lowerBody.Legs.Length; j++)
                    {
                        Leg leg = lowerBody.Legs[j];
                        if (!leg.CanSwing()) continue;

                        Vector3 targetPos = leg.CalculateTargetPoint(_moveData.MoveDir, _moveData.IsRunning);
                        float legDelay = lowerBody.Legs.Length > 2 ? _preset.LegDelay : stepDuration;
                        leg.ExecuteSwing(targetPos, stepDuration, legDelay);
                    }
                        
                    if (_lowerBodies.Length > 1)
                        yield return new WaitForSeconds(_preset.BodyDelay);
                }
                yield return null;
            }
        }

        private void SetLegPriorities()
        {
            LowerBody root = _lowerBodies[0];
            SortLegsByDistance(root, _moveData.MoveDir);
            LinkNextLeg(root);
            
            Leg firstLeg = root.Legs.First();
            firstLeg.CanTakeStep = true;
            Side startSide = firstLeg.Side;
            
            for (int i = 0; i < _lowerBodies.Length; i++)
            {
                if (i == 0) continue;
                LowerBody lowerBody = _lowerBodies[i];
                SortLegsBySide(lowerBody, startSide);
                LinkNextLeg(lowerBody);

                foreach (Leg leg in lowerBody.Legs)
                    leg.CanTakeStep = false;
                lowerBody.Legs.First().CanTakeStep = true;
                startSide = startSide == Side.Right ? Side.Left : Side.Right;
            }
        }
        
        private void LinkNextLeg(LowerBody lowerBody)
        {
            for (int i = 0; i < lowerBody.Legs.Length; i++)
                lowerBody.Legs[i].NextLeg = lowerBody.Legs[(i + 1) % lowerBody.Legs.Length];
        }
        
        private void SortLegsByDistance(LowerBody lowerBody, Vector3 moveDir)
        {
            Vector3 centerPoint = lowerBody.Legs.Aggregate(moveDir, (current, leg) => current + leg.Foot.position);
            centerPoint /= lowerBody.Legs.Length;
            
            lowerBody.Legs = lowerBody.Legs.OrderByDescending(leg =>
                Vector3.Distance(leg.Foot.transform.position, centerPoint)).ToArray();
        }

        private void SortLegsBySide(LowerBody lowerBody, Side prioritySide)
        {
            lowerBody.Legs = lowerBody.Legs.OrderByDescending(leg => leg.Side == prioritySide).ToArray();
        }
    }
}
