using UnityEngine;

namespace TAPS.ActiveRagdoll
{
    public enum JointStrength { None, Weak, Moderate, Strong }
    
    [CreateAssetMenu(fileName = "JointDrives", menuName = "TAPS/ActiveRagdoll/JointDrives")]
    public class JointDrives : ScriptableObject
    {
        public JointDrive strong = new JointDrive
        {
            positionSpring = 1000,
            positionDamper = 0,
            maximumForce = Mathf.Infinity
        };

        public JointDrive moderate = new JointDrive
        {
            positionSpring = 600,
            positionDamper = 0,
            maximumForce = Mathf.Infinity
        };

        public JointDrive weak = new JointDrive
        {
            positionSpring = 100,
            positionDamper = 0,
            maximumForce = Mathf.Infinity
        };

        public JointDrive none = new JointDrive
        {
            positionSpring = 0,
            positionDamper = 0,
            maximumForce = Mathf.Infinity
        };
        
        public JointDrive GetJointDrive(JointStrength strength)
        {
            switch (strength)
            {
                case JointStrength.None:
                    return none;

                case JointStrength.Weak:
                    return weak;

                case JointStrength.Moderate:
                    return moderate;

                case JointStrength.Strong:
                    return strong;

                default:
                    return moderate;
            }
        }
    }
}
