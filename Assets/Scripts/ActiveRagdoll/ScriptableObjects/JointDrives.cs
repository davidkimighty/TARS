using UnityEngine;

[CreateAssetMenu]
public class JointDrives : ScriptableObject
{
    #region Private Field
    private JointDrive _strong;
    private JointDrive _medium;
    private JointDrive _weak;
    private JointDrive _none;
    #endregion

    #region Properties
    public JointDrive Strong => _strong;
    public JointDrive Medium => _medium;
    public JointDrive Weak => _weak;
    public JointDrive None => _none;
    public bool IsInitialized { get; set; } = false;
    #endregion

    private void OnEnable()
    {
        IsInitialized = false;
    }

    #region Initialize
    public void InitializeJoints()
    {
        if (IsInitialized) return;
        IsInitialized = true;

        _strong = new JointDrive
        {
            positionSpring = 1000,
            positionDamper = 0,
            maximumForce = Mathf.Infinity
        };

        _medium = new JointDrive
        {
            positionSpring = 600,
            positionDamper = 0,
            maximumForce = Mathf.Infinity
        };

        _weak = new JointDrive
        {
            positionSpring = 100,
            positionDamper = 0,
            maximumForce = Mathf.Infinity
        };

        _none = new JointDrive
        {
            positionSpring = 0,
            positionDamper = 0,
            maximumForce = Mathf.Infinity
        };
    }
    #endregion
}