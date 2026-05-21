using System;
using UnityEngine;

namespace AlcoholSimVR.Utilities
{
    /// <summary>
    /// Sol el avuç içinin kameraya dönük olup olmadığını algılar (OVRHand / OVRSkeleton).
    /// </summary>
    public class HandPalmDetector : MonoBehaviour
    {
        /// <summary>Avuç kameraya döndüğünde tetiklenir.</summary>
        public event Action OnPalmFacingCamera;

        /// <summary>Avuç kameradan döndüğünde tetiklenir.</summary>
        public event Action OnPalmNotFacingCamera;

        [Header("El Referansı")]
        [SerializeField] private OVRHand _leftHand;
        [SerializeField] private OVRSkeleton _leftHandSkeleton;
        [SerializeField] private Transform _leftHandFallbackAnchor;
        [SerializeField] private Transform _cameraTransform;

        [Header("Eşikler")]
        [SerializeField] private float _palmFacingDotThreshold = 0.25f;
        [SerializeField] private float _palmNotFacingDotThreshold = 0.05f;
        [SerializeField] private float _checkIntervalSeconds = 0.02f;
        [SerializeField] private bool _requireHighConfidence;

        private bool _isPalmFacing;
        private float _nextCheckTime;

        /// <summary>Runtime el bileşenlerini bağlar.</summary>
        public void BindHands(OVRHand leftHand, OVRSkeleton skeleton, Transform fallbackAnchor)
        {
            _leftHand = leftHand;
            _leftHandSkeleton = skeleton;
            _leftHandFallbackAnchor = fallbackAnchor;
        }

        public void BindCamera(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }

        private void Start()
        {
            ResolveCameraTransform();
        }

        private void Update()
        {
            if (Time.time < _nextCheckTime)
            {
                return;
            }

            _nextCheckTime = Time.time + _checkIntervalSeconds;

            ResolveCameraTransform();

            if (_cameraTransform == null)
            {
                return;
            }

            if (!TryGetPalmVectors(out Vector3 palmPosition, out Vector3 palmNormal))
            {
                if (_isPalmFacing)
                {
                    _isPalmFacing = false;
                    OnPalmNotFacingCamera?.Invoke();
                }

                return;
            }

            Vector3 toCamera = (_cameraTransform.position - palmPosition).normalized;
            float dot = Vector3.Dot(palmNormal, toCamera);

            if (!_isPalmFacing && dot >= _palmFacingDotThreshold)
            {
                _isPalmFacing = true;
                OnPalmFacingCamera?.Invoke();
            }
            else if (_isPalmFacing && dot <= _palmNotFacingDotThreshold)
            {
                _isPalmFacing = false;
                OnPalmNotFacingCamera?.Invoke();
            }
        }

        /// <summary>
        /// Avuç pozisyonu ve dışa bakan normal vektörünü döndürür.
        /// </summary>
        public bool TryGetPalmVectors(out Vector3 palmPosition, out Vector3 palmNormal)
        {
            palmPosition = Vector3.zero;
            palmNormal = Vector3.forward;

            if (_leftHand != null)
            {
                if (!_leftHand.IsTracked)
                {
                    return false;
                }

                if (_requireHighConfidence && !_leftHand.IsDataHighConfidence)
                {
                    return false;
                }
            }

            if (_leftHandSkeleton != null
                && _leftHandSkeleton.IsDataValid
                && (!_requireHighConfidence || _leftHandSkeleton.IsDataHighConfidence)
                && _leftHandSkeleton.Bones != null
                && _leftHandSkeleton.Bones.Count > (int)OVRSkeleton.BoneId.Hand_Middle1)
            {
                Transform wrist = _leftHandSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_WristRoot].Transform;
                Transform middleMetacarpal = _leftHandSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_Middle1].Transform;
                Transform indexMetacarpal = _leftHandSkeleton.Bones[(int)OVRSkeleton.BoneId.Hand_Index1].Transform;

                if (wrist != null && middleMetacarpal != null && indexMetacarpal != null)
                {
                    palmPosition = wrist.position;
                    Vector3 across = (indexMetacarpal.position - middleMetacarpal.position).normalized;
                    Vector3 forward = (middleMetacarpal.position - wrist.position).normalized;
                    palmNormal = Vector3.Cross(across, forward).normalized;
                    palmNormal = -palmNormal;
                    return true;
                }
            }

            if (_leftHandFallbackAnchor != null)
            {
                palmPosition = _leftHandFallbackAnchor.position;
                palmNormal = _leftHandFallbackAnchor.forward;
                if (_cameraTransform != null
                    && Vector3.Dot(palmNormal, (_cameraTransform.position - palmPosition).normalized) < 0f)
                {
                    palmNormal = -palmNormal;
                }

                return true;
            }

            return false;
        }

        private void ResolveCameraTransform()
        {
            if (_cameraTransform != null)
            {
                return;
            }

            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null && rig.centerEyeAnchor != null)
            {
                _cameraTransform = rig.centerEyeAnchor;
                return;
            }

            if (Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }
        }
    }
}
