using UnityEngine;
using TMPro;

public class StateViewer : MonoBehaviour
{
    #region Serialize Field
    [SerializeField]
    private GameObject _ragdoll = null;
    #endregion

    #region Private Field
    private TextMeshProUGUI _output = null;
    private ActiveRagdoll _ac = null;
    private RagdollJointController _jc = null;
    #endregion

    private void Start()
    {
        _output = GetComponentInChildren<TextMeshProUGUI>();
        _ac = _ragdoll.GetComponent<ActiveRagdoll>();
        _jc = _ragdoll.GetComponent<RagdollJointController>();
    }

    private void Update()
    {
        _output.text = "Ragdoll State :\n" + _ac.RagdollState + "\n\n" +
            "Lean Dir :\n" + _ac.LeaningDir + "\n\n" +
            "Active Leg :\n" + _jc.ActiveLeg + "\n\n";
    }
}