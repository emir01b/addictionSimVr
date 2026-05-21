using UnityEngine;

namespace AlcoholSimVR.Simulation
{
    /// <summary>
    /// Tahta yürüme hacmi tetikleyicisi — oyuncu giriş/çıkışını SessionTracker'a bildirir.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BeamWalkTrigger : MonoBehaviour
    {
        [SerializeField] private bool _isSideBoundary;

        private Core.SessionTracker _tracker;
        private int _overlapCount;

        public void Setup(Core.SessionTracker tracker, bool isSideBoundary = false)
        {
            _tracker = tracker;
            _isSideBoundary = isSideBoundary;

            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            if (_isSideBoundary)
            {
                _tracker?.NotifyExitedBeam();
                return;
            }

            _overlapCount++;
            if (_overlapCount == 1)
            {
                _tracker?.NotifyEnteredBeam();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other))
            {
                return;
            }

            if (_isSideBoundary)
            {
                return;
            }

            _overlapCount = Mathf.Max(0, _overlapCount - 1);
            if (_overlapCount == 0)
            {
                _tracker?.NotifyExitedBeam();
            }
        }

        private static bool IsPlayer(Collider other)
        {
            if (other == null)
            {
                return false;
            }

            if (other.CompareTag("Player"))
            {
                return true;
            }

            return other.GetComponentInParent<OVRCameraRig>() != null
                || other.GetComponent<CharacterController>() != null
                || other.name.IndexOf("Camera", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
