using UnityEngine;

namespace TARS.Locomotion
{
    public class LimbContact : MonoBehaviour
    {
        private LegRagdoll _owner;

        private void OnCollisionEnter(Collision collision)
        {
            if (_owner != null)
                _owner.Hit(collision);
        }

        public void Bind(LegRagdoll owner)
        {
            _owner = owner;
        }
    }
}
