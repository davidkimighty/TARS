using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace TARS
{
    [Serializable]
    public class LowerBody
    {
        public Transform Body;
        public Leg[] Legs;
    }
    
    public class ProceduralAnimationController : MonoBehaviour
    {
        [Tooltip("Index 0 is root.")]
        [SerializeField] private LowerBody[] _lowerBodies;
        [SerializeField] private BodyPreset _preset;

        private IMovement _movement;
        private MoveData _moveData;
        private IEnumerator _LegMovementCoroutine;

        private void Awake()
        {
            _movement = GetComponent<IMovement>();
            if (_movement != null)
                _movement.OnMove += ReceiveMoveData;

            _moveData = new MoveData
            {
                MoveDir = transform.forward,
                BodyForward = transform.right
            };
        }

        private void Start()
        {
            if (_lowerBodies != null && _lowerBodies.Length > 0)
            {
                SetLegPriorities(_moveData.MoveDir);
                _LegMovementCoroutine = LowerBodyMovementLoop();
                StartCoroutine(_LegMovementCoroutine);
            }
        }

        private void ReceiveMoveData(MoveData data)
        {
            float bodyForwardDot = Vector3.Dot(_moveData.MoveDir, data.MoveDir);
            _moveData.UpdateData(data);
            
            if (bodyForwardDot < 0)
                SetLegPriorities(_moveData.MoveDir);
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

                        float legDelay = lowerBody.Legs.Length > 2 ? _preset.LegDelay : stepDuration;
                        leg.ExecuteSwing(_moveData, stepDuration, legDelay);
                    }
                        
                    if (_lowerBodies.Length > 1)
                        yield return new WaitForSeconds(_preset.BodyDelay);
                }
                yield return null;
            }
        }

        private void SetLegPriorities(Vector3 moveDir)
        {
            // Debug.Log("Leg priorities set");
            LowerBody root = _lowerBodies[0];
            SortLegsByDistance(root, moveDir);
            LinkNextLeg(root);
            
            Leg firstLeg = root.Legs.First();
            firstLeg.CanTakeStep = true;
            Side startSide = firstLeg.Side;
            
            for (int i = 1; i < _lowerBodies.Length; i++)
            {
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
            Vector3 comparePoint = lowerBody.Body.position + moveDir;
            
            lowerBody.Legs = lowerBody.Legs.OrderByDescending(leg =>
                Vector3.Distance(leg.Foot.transform.position, comparePoint)).ToArray();
        }

        private void SortLegsBySide(LowerBody lowerBody, Side prioritySide)
        {
            lowerBody.Legs = lowerBody.Legs.OrderByDescending(leg => leg.Side == prioritySide).ToArray();
        }
    }
}
