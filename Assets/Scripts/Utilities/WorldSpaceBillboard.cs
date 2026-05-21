using UnityEngine;

namespace AlcoholSimVR.Utilities
{
    /// <summary>
    /// World Space UI'nin her karede kameraya bakmasını sağlar (Y ekseni kilitli opsiyonel).
    /// </summary>
    public class WorldSpaceBillboard : MonoBehaviour
    {
        [SerializeField] private bool _lockYAxis = true;
        [SerializeField] private Transform _cameraOverride;

        private Transform _cameraTransform;

        private void LateUpdate()
        {
            if (_cameraTransform == null)
            {
                _cameraTransform = ResolveCameraTransform();
            }

            if (_cameraTransform == null)
            {
                return;
            }

            Vector3 direction = transform.position - _cameraTransform.position;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            if (_lockYAxis)
            {
                direction.y = 0f;
                if (direction.sqrMagnitude < 0.0001f)
                {
                    direction = _cameraTransform.forward;
                    direction.y = 0f;
                }
            }

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private Transform ResolveCameraTransform()
        {
            if (_cameraOverride != null)
            {
                return _cameraOverride;
            }

            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null && rig.centerEyeAnchor != null)
            {
                return rig.centerEyeAnchor;
            }

            return Camera.main != null ? Camera.main.transform : null;
        }
    }
}
