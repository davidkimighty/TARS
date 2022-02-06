using UnityEngine;

namespace CollieLab.ActiveRagdoll
{
    [CreateAssetMenu(fileName = "JointDrives", menuName = "Coco/Creature/JointDrives")]
    public class JointDrives : ScriptableObject
    {
        #region Public Field
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
        #endregion

        /// <summary>
        /// Return joint power accordingly.
        /// </summary>
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

    public enum JointStrength { None, Weak, Moderate, Strong }
}
