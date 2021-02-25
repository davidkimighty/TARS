using UnityEngine;

public class ActiveRagdoll : MonoBehaviour
{
    #region Serialize Field
    [Space, Header("Active Ragdoll")]
    [SerializeField]
    private GameObject _root = null;
    [SerializeField]
    private Rigidbody[] _rigidbodies = null;
    [SerializeField]
    private JointDrives _jointDrives = null;

    [Space, Header("Balance")]
    [SerializeField]
    private Transform _CoM = null;
    [SerializeField]
    private Transform _feetCenterPoint = null;
    [SerializeField]
    private float _outOfBalanceThreshold = 0.3f;
    [SerializeField]
    private float _fallDownThreshold = 0.3f;
    [SerializeField]
    private float _inAirThreshold = 0.7f;
    [SerializeField]
    private float _leanThreshold = 0.5f;
    #endregion

    #region Private Field
    private static Vector3 _flatUnitVec = new Vector3(1, 0, 1);
    private Vector3 _leanDirFlat = Vector3.zero;
    private float _outOfBalanceValue = 0f;
    #endregion

    #region Protected Field
    protected Joints _joints;
    protected JointDrives JointDrives { get => _jointDrives; }
    protected BalanceState ragdollState = BalanceState.InBalance;
    protected LeaningDirection leaningDirection = LeaningDirection.NoDir;
    #endregion

    #region Properties
    public BalanceState RagdollState { get => ragdollState; }
    public LeaningDirection LeaningDir { get => leaningDirection; }
    #endregion

    #region Initialize
    /// <summary>
    /// Setting up Active Ragdoll.
    /// </summary>
    protected void RagdollSetup()
    {
        PopulateChild();
        IgnoreCollisions();
        InitializeJointDrives();
        InitVariables();
    }

    private void InitVariables()
    {
        _CoM = transform.GetChild(0);
        CalculateCoM();
    }

    private void PopulateChild()
    {
        if (_root != null)
        {
            _rigidbodies = _root.GetComponentsInChildren<Rigidbody>();
            _joints.Head = _rigidbodies[0].GetComponent<ConfigurableJoint>();
            _joints.Thigh_L = _rigidbodies[1].GetComponent<ConfigurableJoint>();
            _joints.Thigh_R = _rigidbodies[2].GetComponent<ConfigurableJoint>();
            _joints.Calf_L = _rigidbodies[3].GetComponent<ConfigurableJoint>();
            _joints.Calf_R = _rigidbodies[4].GetComponent<ConfigurableJoint>();
            _joints.Foot_L = _rigidbodies[5].GetComponent<ConfigurableJoint>();
            _joints.Foot_R = _rigidbodies[6].GetComponent<ConfigurableJoint>();
        }
    }

    private void IgnoreCollisions()
    {
        Collider head = _rigidbodies[0].GetComponent<Collider>();
        Collider Thigh_L = _rigidbodies[0].GetComponent<Collider>();
        Collider Thigh_R = _rigidbodies[0].GetComponent<Collider>();
        Collider Calf_L = _rigidbodies[0].GetComponent<Collider>();
        Collider Calf_R = _rigidbodies[0].GetComponent<Collider>();
        Collider Foot_L = _rigidbodies[0].GetComponent<Collider>();
        Collider Foot_R = _rigidbodies[0].GetComponent<Collider>();

        Physics.IgnoreCollision(head, Thigh_L, true);
        Physics.IgnoreCollision(head, Thigh_R, true);
        Physics.IgnoreCollision(Thigh_L, Calf_L, true);
        Physics.IgnoreCollision(Calf_L, Foot_L, true);
        Physics.IgnoreCollision(Thigh_R, Calf_R, true);
        Physics.IgnoreCollision(Calf_R, Foot_R, true);
    }

    private void InitializeJointDrives()
    {
        _jointDrives.InitializeJoints();
        StrengthenJoints();

        _joints.Thigh_L.xDrive = _jointDrives.Strong;
        _joints.Thigh_L.yDrive = _jointDrives.Strong;
        _joints.Thigh_L.zDrive = _jointDrives.Strong;
        _joints.Thigh_R.xDrive = _jointDrives.Strong;
        _joints.Thigh_R.yDrive = _jointDrives.Strong;
        _joints.Thigh_R.zDrive = _jointDrives.Strong;

        _joints.Calf_L.xDrive = _jointDrives.Strong;
        _joints.Calf_L.yDrive = _jointDrives.Strong;
        _joints.Calf_L.zDrive = _jointDrives.Strong;
        _joints.Calf_R.xDrive = _jointDrives.Strong;
        _joints.Calf_R.yDrive = _jointDrives.Strong;
        _joints.Calf_R.zDrive = _jointDrives.Strong;

        _joints.Foot_L.xDrive = _jointDrives.Medium;
        _joints.Foot_L.yDrive = _jointDrives.Medium;
        _joints.Foot_L.zDrive = _jointDrives.Medium;
        _joints.Foot_R.xDrive = _jointDrives.Medium;
        _joints.Foot_R.yDrive = _jointDrives.Medium;
        _joints.Foot_R.zDrive = _jointDrives.Medium;
    }
    #endregion

    #region Ragdoll Core Values
    /// <summary>
    /// Calculate core values that effect the state of ragdoll.
    /// This function should be called in the child script every frame.
    /// </summary>
    protected void RagdollCalculateStateValues()
    {
        CalculateCoM();
        CalculateCenterPoint();
        CalculateLeanValue();
    }

    private void CalculateCoM()
    {
        Vector3 com = Vector3.zero;
        float sum = 0f;

        foreach (Rigidbody rb in _rigidbodies)
        {
            com += rb.worldCenterOfMass * rb.mass;
            sum += rb.mass;
        }
        com /= sum;
        _CoM.position = com;

        Vector3 tartgetRot = Vector3.Scale(-_joints.Head.transform.forward, _flatUnitVec);
        _CoM.rotation = Quaternion.LookRotation(tartgetRot);
    }

    private void CalculateCenterPoint()
    {
        Vector3[] _feetPos = new Vector3[2];
        _feetPos[0] = _joints.Foot_L.transform.position;
        _feetPos[1] = _joints.Foot_R.transform.position;

        Vector3 sum = Vector3.zero;
        if (_feetPos == null || _feetPos.Length == 0)
            _feetCenterPoint.position = sum;

        foreach (Vector3 vec in _feetPos)
            sum += vec;

        _feetCenterPoint.position = sum / _feetPos.Length;
    }

    private void CalculateLeanValue()
    {
        Vector3 cpFlat = Vector3.Scale(_feetCenterPoint.position, _flatUnitVec);
        Vector3 comFlat = Vector3.Scale(_CoM.position, _flatUnitVec);
        _leanDirFlat = (comFlat - cpFlat).normalized;
        _outOfBalanceValue = Vector3.Distance(comFlat, cpFlat);
    }
    #endregion

    #region Ragdoll State
    /// <summary>
    /// Determine the state based on the core values.
    /// This function should be called in the child script every frame.
    /// </summary>
    protected void RagdollCheckState()
    {
        CheckBalanceState();
        CheckLeaningState();
    }

    private void CheckBalanceState()
    {
        float dst = Mathf.Abs(_CoM.position.y - _feetCenterPoint.position.y);

        if (_CoM.position.y > _inAirThreshold)
            ragdollState = BalanceState.InAir;

        else if (dst < _fallDownThreshold)
            ragdollState = BalanceState.FellDown;

        else if (_outOfBalanceValue > _outOfBalanceThreshold)
            ragdollState = BalanceState.OutOfBalance;

        else
            ragdollState = BalanceState.InBalance;
    }

    private void CheckLeaningState()
    {
        float forward = Vector3.Dot(_CoM.forward, _leanDirFlat);
        float backward = Vector3.Dot(-_CoM.forward, _leanDirFlat);
        float right = Vector3.Dot(_CoM.right, _leanDirFlat);
        float left = Vector3.Dot(-_CoM.right, _leanDirFlat);

        if (forward > _leanThreshold)
            leaningDirection = LeaningDirection.Forward;

        else if (backward > _leanThreshold)
            leaningDirection = LeaningDirection.Backward;

        else if (right > _leanThreshold)
            leaningDirection = LeaningDirection.Right;

        else if (left > _leanThreshold)
            leaningDirection = LeaningDirection.Left;

        else
            leaningDirection = LeaningDirection.NoDir;
    }
    #endregion

    #region Joint Power
    protected void LoosenJoints()
    {
        _joints.Head.angularXDrive = _jointDrives.None;
        _joints.Head.angularYZDrive = _jointDrives.None;

        _joints.Thigh_L.angularXDrive = _jointDrives.None;
        _joints.Thigh_L.angularYZDrive = _jointDrives.None;
        _joints.Thigh_R.angularXDrive = _jointDrives.None;
        _joints.Thigh_R.angularYZDrive = _jointDrives.None;

        _joints.Calf_L.angularXDrive = _jointDrives.None;
        _joints.Calf_L.angularYZDrive = _jointDrives.None;
        _joints.Calf_R.angularXDrive = _jointDrives.None;
        _joints.Calf_R.angularYZDrive = _jointDrives.None;

        _joints.Foot_L.angularXDrive = _jointDrives.None;
        _joints.Foot_L.angularYZDrive = _jointDrives.None;
        _joints.Foot_R.angularXDrive = _jointDrives.None;
        _joints.Foot_R.angularYZDrive = _jointDrives.None;
    }

    protected void StrengthenJoints()
    {
        _joints.Head.angularXDrive = _jointDrives.Weak;
        _joints.Head.angularYZDrive = _jointDrives.Weak;

        _joints.Thigh_L.angularXDrive = _jointDrives.Strong;
        _joints.Thigh_L.angularYZDrive = _jointDrives.Strong;
        _joints.Thigh_R.angularXDrive = _jointDrives.Strong;
        _joints.Thigh_R.angularYZDrive = _jointDrives.Strong;

        _joints.Calf_L.angularXDrive = _jointDrives.Strong;
        _joints.Calf_L.angularYZDrive = _jointDrives.Strong;
        _joints.Calf_R.angularXDrive = _jointDrives.Strong;
        _joints.Calf_R.angularYZDrive = _jointDrives.Strong;

        _joints.Foot_L.angularXDrive = _jointDrives.Medium;
        _joints.Foot_L.angularYZDrive = _jointDrives.Medium;
        _joints.Foot_R.angularXDrive = _jointDrives.Medium;
        _joints.Foot_R.angularYZDrive = _jointDrives.Medium;
    }

    protected void SetHeadJointPower(JointDrive jointDrive)
    {
        _joints.Head.angularXDrive = jointDrive;
        _joints.Head.angularYZDrive = jointDrive;
    }
    #endregion

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_CoM.position, 0.03f);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(_feetCenterPoint.position, 0.03f);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(_CoM.position, _leanDirFlat);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(_CoM.position, _CoM.forward);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(_CoM.position, _CoM.right);
    }
}

public enum BalanceState { InBalance, OutOfBalance, FellDown, InAir }
public enum LeaningDirection { NoDir, Forward, Backward, Left, Right }

public struct Joints
{
    public ConfigurableJoint Head { get; set; }
    public ConfigurableJoint Thigh_L { get; set; }
    public ConfigurableJoint Thigh_R { get; set; }
    public ConfigurableJoint Calf_L { get; set; }
    public ConfigurableJoint Calf_R { get; set; }
    public ConfigurableJoint Foot_L { get; set; }
    public ConfigurableJoint Foot_R { get; set; }
}