using System.Collections;
using UnityEngine;

public class RagdollJointController : MonoBehaviour
{
    #region Serialize Field
    [Space, Header("Settings")]
    [SerializeField]
    private AnimationCurve _startPhaseAC = null;
    [SerializeField]
    private AnimationCurve _endPhaseAC = null;
    [SerializeField]
    private PhysicMaterial _slide = null;
    [SerializeField]
    private PhysicMaterial _rough = null;
    [SerializeField]
    private float _rotSpeed = 3f;

    [Space, Header("Leg Target Rotations")]
    [SerializeField]
    private LegTargetRotations _balanceForwardRots = null;
    [SerializeField]
    private LegTargetRotations _WalkingForwardRots = null;
    #endregion

    #region Private Field
    private Joints _joints;
    private Coroutine _activeLegCoroutine = null;
    private Swing _swing;
    private Stance _stance;
    private ActiveLeg _lastActiveLeg = ActiveLeg.Right;
    #endregion

    #region Properties
    public ActiveLeg ActiveLeg { get; set; } = ActiveLeg.Left;
    #endregion

    #region Initialize
    public void Initialize(Joints joints)
    {
        _joints = joints;
        SetSwingStance();
    }
    #endregion

    #region Head
    /// <summary>
    /// 
    /// </summary>
    public void RotateRagdoll(Quaternion currentRot)
    {
        var targetRot = Quaternion.identity * Quaternion.Inverse(currentRot);
        _joints.Head.targetRotation = Quaternion.Lerp(_joints.Head.targetRotation, targetRot, _rotSpeed * Time.fixedDeltaTime);
    }
    #endregion

    #region Legs
    /// <summary>
    /// 
    /// </summary>
    public void BalanceLegMovement(LeaningDirection leaningDir)
    {
        switch (leaningDir)
        {
            case LeaningDirection.Forward:
                if (!_lastActiveLeg.Equals(ActiveLeg))
                {
                    _lastActiveLeg = ActiveLeg;
                    _activeLegCoroutine = StartCoroutine(
                        RotateLegJoints(_swing, _stance, _balanceForwardRots));
                }
                break;

            case LeaningDirection.Backward:

                break;

            case LeaningDirection.Left:

                break;

            case LeaningDirection.Right:

                break;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public void WalkLegMovement()
    {
        if (!_lastActiveLeg.Equals(ActiveLeg))
        {
            _lastActiveLeg = ActiveLeg;
            _activeLegCoroutine = StartCoroutine(
                RotateLegJoints(_swing, _stance, _WalkingForwardRots));
        }
    }

    private void SetSwingStance()
    {
        if (ActiveLeg.Equals(ActiveLeg.Left))
        {
            _swing.Upper = _joints.Thigh_L;
            _swing.Lower = _joints.Calf_L;
            _swing.Foot = _joints.Foot_L;

            _stance.Upper = _joints.Thigh_R;
            _stance.Lower = _joints.Calf_R;
            _stance.Foot = _joints.Foot_R;
        }
        else
        {
            _swing.Upper = _joints.Thigh_R;
            _swing.Lower = _joints.Calf_R;
            _swing.Foot = _joints.Foot_R;

            _stance.Upper = _joints.Thigh_L;
            _stance.Lower = _joints.Calf_L;
            _stance.Foot = _joints.Foot_L;
        }
    }

    private IEnumerator RotateLegJoints(Swing swing, Stance stance, LegTargetRotations tr)
    {
        float startElapsedTime = 0f;

        swing.Foot.GetComponent<Collider>().material = _slide;
        stance.Foot.GetComponent<Collider>().material = _rough;

        while (startElapsedTime < tr.StartPhaseDuration)
        {
            startElapsedTime += Time.fixedDeltaTime;
            float startPhasePerc = startElapsedTime / tr.StartPhaseDuration;

            swing.Upper.targetRotation = Quaternion.Lerp(swing.Upper.targetRotation,
                tr.SwingUpStart, _startPhaseAC.Evaluate(startPhasePerc));

            swing.Lower.targetRotation = Quaternion.Lerp(swing.Lower.targetRotation,
                tr.SwingLowStart, _startPhaseAC.Evaluate(startPhasePerc));

            swing.Foot.targetRotation = Quaternion.Lerp(swing.Foot.targetRotation,
                tr.SwingFootStart, _startPhaseAC.Evaluate(startPhasePerc));

            stance.Upper.targetRotation = Quaternion.Lerp(stance.Upper.targetRotation,
                tr.StanceUpStart, _startPhaseAC.Evaluate(startPhasePerc));

            stance.Lower.targetRotation = Quaternion.Lerp(stance.Lower.targetRotation,
                tr.StanceLowStart, _startPhaseAC.Evaluate(startPhasePerc));

            stance.Foot.targetRotation = Quaternion.Lerp(stance.Foot.targetRotation,
                tr.StanceFootStart, _startPhaseAC.Evaluate(startPhasePerc));
            yield return null;
        }
        
        float endElapsedTime = 0f;
        swing.Foot.GetComponent<Collider>().material = _rough;
        stance.Foot.GetComponent<Collider>().material = _slide;

        while (endElapsedTime < tr.EndPhaseDuration)
        {
            endElapsedTime += Time.fixedDeltaTime;
            float endPhasePerc = endElapsedTime / tr.EndPhaseDuration;

            swing.Upper.targetRotation = Quaternion.Lerp(swing.Upper.targetRotation,
                tr.SwingUpEnd, _endPhaseAC.Evaluate(endPhasePerc));

            swing.Lower.targetRotation = Quaternion.Lerp(swing.Lower.targetRotation,
                tr.SwingLowEnd, _endPhaseAC.Evaluate(endPhasePerc));

            swing.Foot.targetRotation = Quaternion.Lerp(swing.Foot.targetRotation,
                tr.SwingFootEnd, _endPhaseAC.Evaluate(endPhasePerc));

            stance.Upper.targetRotation = Quaternion.Lerp(stance.Upper.targetRotation,
                tr.StanceUpEnd, _endPhaseAC.Evaluate(endPhasePerc));

            stance.Lower.targetRotation = Quaternion.Lerp(stance.Lower.targetRotation,
                tr.StanceLowEnd, _endPhaseAC.Evaluate(endPhasePerc));

            stance.Foot.targetRotation = Quaternion.Lerp(stance.Foot.targetRotation,
                tr.StanceFootEnd, _endPhaseAC.Evaluate(endPhasePerc));
            yield return null;
        }
        
        ActiveLeg = ActiveLeg.Equals(ActiveLeg.Left) ?
            ActiveLeg.Right : ActiveLeg.Left;

        SetSwingStance();
    }
    #endregion
}

public enum ActiveLeg { Left, Right }

public struct Swing
{
    public ConfigurableJoint Upper { get; set; }
    public ConfigurableJoint Lower { get; set; }
    public ConfigurableJoint Foot { get; set; }
}
public struct Stance
{
    public ConfigurableJoint Upper { get; set; }
    public ConfigurableJoint Lower { get; set; }
    public ConfigurableJoint Foot { get; set; }
}