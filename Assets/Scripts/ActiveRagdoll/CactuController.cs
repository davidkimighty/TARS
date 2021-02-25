using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CactuController : ActiveRagdoll
{
    #region Serialize Field
    [Space, Header("Target")]
    [SerializeField]
    private GameObject _target = null;

    [Space, Header("Locomotion")]
    [SerializeField]
    private float _movementSpeed = 5f;
    [SerializeField]
    private float _stoppingDst = 1f;

    [Space, Header("Vision")]
    [SerializeField]
    private float _AngleCutoff = 30f;
    #endregion

    #region Private Field
    private RagdollJointController _jointController = null;
    #endregion

    #region Properties
    
    #endregion

    private void Awake()
    {
        RagdollSetup();
    }

    private void Start()
    {
        _jointController = GetComponent<RagdollJointController>();
        _jointController.Initialize(_joints);

        _target = GameObject.FindGameObjectWithTag("Target");
    }

    private void Update()
    {
        RagdollCalculateStateValues();
        RagdollCheckState();
    }

    private void FixedUpdate()
    {
        switch (ragdollState)
        {
            case BalanceState.InAir:
                LoosenJoints();
                break;

            case BalanceState.FellDown:
                LoosenJoints();
                break;

            default:
                CactuBehaviour();
                break;
        }
    }

    #region Behaviours
    private void CactuBehaviour()
    {
        if (_target != null && IsTargetInViewAngle(_target))
        {
            SetHeadJointPower(JointDrives.Strong);

            Vector3 vec2Target = _target.transform.position - _joints.Head.transform.position;
            Vector3 vec2TargetFlat = Vector3.Scale(vec2Target, new Vector3(1, 0, 1));
            _jointController.RotateRagdoll(Quaternion.LookRotation(vec2TargetFlat));
            
            float dst = Vector3.Distance(_target.transform.position, _joints.Head.transform.position);
            
            if (dst > _stoppingDst)
            {
                _jointController.WalkLegMovement();

            }
            else
            {
                SetHeadJointPower(JointDrives.Weak);
                BalanceBehaviour();
            }
        }
        else
        {
            BalanceBehaviour();
        }
    }

    private void BalanceBehaviour()
    {
        switch (ragdollState)
        {
            case BalanceState.OutOfBalance:
                _jointController.BalanceLegMovement(leaningDirection);
                break;

            case BalanceState.InBalance:

                break;
        }
    }
    #endregion

    #region Detect Functions
    private bool IsTargetInViewAngle(GameObject target)
    {
        Vector3 dir = target.transform.position - _joints.Head.transform.position;
        float cosAngle = Vector3.Dot(dir.normalized, -_joints.Head.transform.forward);
        float angle = Mathf.Acos(cosAngle) * Mathf.Rad2Deg;
        return angle < _AngleCutoff;
    }
    #endregion
}