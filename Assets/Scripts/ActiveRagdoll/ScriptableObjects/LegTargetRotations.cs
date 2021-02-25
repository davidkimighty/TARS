using UnityEngine;

[CreateAssetMenu]
public class LegTargetRotations : ScriptableObject
{
    #region Serialize Field
    [Space, Header("Swing")]
    [SerializeField]
    private Vector3 _upperSwingStart = Vector3.zero;
    [SerializeField]
    private Vector3 _lowerSwingStart = Vector3.zero;
    [SerializeField]
    private Vector3 _footSwingStart = Vector3.zero;

    [SerializeField]
    private Vector3 _upperSwingEnd = Vector3.zero;
    [SerializeField]
    private Vector3 _lowerSwingEnd = Vector3.zero;
    [SerializeField]
    private Vector3 _footSwingEnd = Vector3.zero;

    [Space, Header("Stance")]
    [SerializeField]
    private Vector3 _upperStanceStart = Vector3.zero;
    [SerializeField]
    private Vector3 _lowerStanceStart = Vector3.zero;
    [SerializeField]
    private Vector3 _footStanceStart = Vector3.zero;

    [SerializeField]
    private Vector3 _upperStanceEnd = Vector3.zero;
    [SerializeField]
    private Vector3 _lowerStanceEnd = Vector3.zero;
    [SerializeField]
    private Vector3 _footStanceEnd = Vector3.zero;

    [Space, Header("Duration")]
    [SerializeField]
    private float _startPhaseDuration = 1f;
    [SerializeField]
    private float _endPhaseDuration = 0.5f;
    #endregion

    #region Properties
    public Quaternion SwingUpStart => Quaternion.Euler(_upperSwingStart);
    public Quaternion SwingLowStart => Quaternion.Euler(_lowerSwingStart);
    public Quaternion SwingFootStart => Quaternion.Euler(_footSwingStart);
    public Quaternion StanceUpStart => Quaternion.Euler(_upperStanceStart);
    public Quaternion StanceLowStart => Quaternion.Euler(_lowerStanceStart);
    public Quaternion StanceFootStart => Quaternion.Euler(_footStanceStart);

    public Quaternion SwingUpEnd => Quaternion.Euler(_upperSwingEnd);
    public Quaternion SwingLowEnd => Quaternion.Euler(_lowerSwingEnd);
    public Quaternion SwingFootEnd => Quaternion.Euler(_footSwingEnd);
    public Quaternion StanceUpEnd => Quaternion.Euler(_upperStanceEnd);
    public Quaternion StanceLowEnd => Quaternion.Euler(_lowerStanceEnd);
    public Quaternion StanceFootEnd => Quaternion.Euler(_footStanceEnd);

    public float StartPhaseDuration => _startPhaseDuration;
    public float EndPhaseDuration => _endPhaseDuration;
    #endregion
}
