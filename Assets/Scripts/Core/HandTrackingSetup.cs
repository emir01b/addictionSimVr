using UnityEngine;
using AlcoholSimVR.Utilities;
using AlcoholSimVR.UI;

namespace AlcoholSimVR.Core
{
    /// <summary>
    /// Sol/sağ el takibi (OVRHand + OVRSkeleton), bilek menüsü hizalaması ve UI pinch girişi.
    /// Kontrolör kullanılmaz.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class HandTrackingSetup : MonoBehaviour
    {
        public static HandTrackingSetup Instance { get; private set; }

        public OVRHand LeftHand { get; private set; }
        public OVRHand RightHand { get; private set; }
        public Transform LeftPalmAnchor { get; private set; }

        [Header("Referanslar")]
        [SerializeField] private OVRCameraRig _cameraRig;
        [SerializeField] private HandPalmDetector _palmDetector;
        [SerializeField] private WristMenuPanel _wristMenu;

        [Header("Bilek Menü Ofseti")]
        [SerializeField] private Vector3 _wristMenuLocalPosition = new Vector3(0.07f, 0.04f, 0.08f);
        [SerializeField] private float _wristMenuScale = 0.0012f;
        [SerializeField] private float _wristMenuFacingSmooth = 18f;

        private Transform _leftHandAnchor;
        private Transform _rightHandAnchor;

        private void Awake()
        {
            Instance = this;

            if (_cameraRig == null)
            {
                _cameraRig = FindAnyObjectByType<OVRCameraRig>();
            }

            if (_palmDetector == null)
            {
                _palmDetector = FindAnyObjectByType<HandPalmDetector>();
            }

            if (_wristMenu == null)
            {
                _wristMenu = FindAnyObjectByType<WristMenuPanel>();
            }

            if (_cameraRig == null)
            {
                Debug.LogError("[HandTrackingSetup] OVRCameraRig bulunamadı.");
                return;
            }

            _leftHandAnchor = FindDeepChild(_cameraRig.transform, "LeftHandAnchor")
                ?? FindDeepChild(_cameraRig.transform, "LeftHandAnchorDetached");
            _rightHandAnchor = FindDeepChild(_cameraRig.transform, "RightHandAnchor")
                ?? FindDeepChild(_cameraRig.transform, "RightHandAnchorDetached");

            LeftHand = EnsureHand(_leftHandAnchor, OVRHand.Hand.HandLeft, "LeftHandTracking");
            RightHand = EnsureHand(_rightHandAnchor, OVRHand.Hand.HandRight, "RightHandTracking");

            MRInputHelper.RegisterHands(LeftHand, RightHand);

            if (_palmDetector != null)
            {
                _palmDetector.BindHands(LeftHand, GetLeftSkeleton(), _leftHandAnchor);
                _palmDetector.BindCamera(_cameraRig.centerEyeAnchor);
            }

            SetupWristMenu();
        }

        private void LateUpdate()
        {
            UpdatePalmAnchor();
            AlignWristMenuToPalm();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private OVRHand EnsureHand(Transform anchor, OVRHand.Hand handType, string objectName)
        {
            if (anchor == null)
            {
                return null;
            }

            OVRHand existing = anchor.GetComponentInChildren<OVRHand>(true);
            if (existing != null)
            {
                ConfigureHand(existing, handType);
                EnsureSkeleton(existing.gameObject, handType);
                return existing;
            }

            var handGo = new GameObject(objectName);
            handGo.transform.SetParent(anchor, false);
            handGo.transform.localPosition = Vector3.zero;
            handGo.transform.localRotation = Quaternion.identity;

            var hand = handGo.AddComponent<OVRHand>();
            ConfigureHand(hand, handType);
            EnsureSkeleton(handGo, handType);
            return hand;
        }

        private static void ConfigureHand(OVRHand hand, OVRHand.Hand handType)
        {
            if (hand == null)
            {
                return;
            }

            OVRHandUtility.SetHandType(hand, handType);
            hand.m_showState = OVRInput.InputDeviceShowState.ControllerNotInHand;
        }

        private static void EnsureSkeleton(GameObject handRoot, OVRHand.Hand handType)
        {
            var skeleton = handRoot.GetComponent<OVRSkeleton>() ?? handRoot.AddComponent<OVRSkeleton>();
            OVRSkeleton.SkeletonType skType = handType == OVRHand.Hand.HandLeft
                ? OVRSkeleton.SkeletonType.HandLeft
                : OVRSkeleton.SkeletonType.HandRight;

            OVRHandUtility.SetSkeletonType(skeleton, skType);
        }

        private OVRSkeleton GetLeftSkeleton()
        {
            return LeftHand != null ? LeftHand.GetComponent<OVRSkeleton>() : null;
        }

        private void SetupWristMenu()
        {
            if (_wristMenu == null || _leftHandAnchor == null)
            {
                return;
            }

            var billboard = _wristMenu.GetComponent<WorldSpaceBillboard>();
            if (billboard != null)
            {
                billboard.enabled = false;
            }

            var palmGo = new GameObject("LeftPalmAnchor");
            palmGo.transform.SetParent(_leftHandAnchor, false);
            LeftPalmAnchor = palmGo.transform;

            _wristMenu.transform.SetParent(LeftPalmAnchor, false);
            ApplyWristMenuLocalPose();
        }

        private void UpdatePalmAnchor()
        {
            if (LeftPalmAnchor == null || LeftHand == null || !LeftHand.IsTracked)
            {
                return;
            }

            var skeleton = GetLeftSkeleton();
            if (skeleton != null
                && skeleton.IsDataValid
                && skeleton.Bones != null
                && skeleton.Bones.Count > (int)OVRSkeleton.BoneId.Hand_Middle1)
            {
                Transform wrist = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_WristRoot].Transform;
                Transform middle = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_Middle1].Transform;
                Transform index = skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_Index1].Transform;
                if (wrist != null && middle != null && index != null)
                {
                    Vector3 across = (index.position - middle.position).normalized;
                    Vector3 forward = (middle.position - wrist.position).normalized;
                    Vector3 palmNormal = -Vector3.Cross(across, forward).normalized;
                    LeftPalmAnchor.position = wrist.position + palmNormal * 0.04f;
                    LeftPalmAnchor.rotation = Quaternion.LookRotation(-palmNormal, forward);
                    return;
                }
            }

            LeftPalmAnchor.position = LeftHand.PointerPose != null
                ? LeftHand.PointerPose.position
                : _leftHandAnchor.position;
            LeftPalmAnchor.rotation = LeftHand.PointerPose != null && LeftHand.IsPointerPoseValid
                ? LeftHand.PointerPose.rotation
                : _leftHandAnchor.rotation;
        }

        private void AlignWristMenuToPalm()
        {
            if (_wristMenu == null || LeftPalmAnchor == null)
            {
                return;
            }

            if (_wristMenu.transform.parent != LeftPalmAnchor)
            {
                _wristMenu.transform.SetParent(LeftPalmAnchor, false);
            }

            ApplyWristMenuLocalPose();
        }

        private void ApplyWristMenuLocalPose()
        {
            _wristMenu.transform.localPosition = _wristMenuLocalPosition;
            _wristMenu.transform.localScale = Vector3.one * _wristMenuScale;

            Transform cam = _cameraRig != null ? _cameraRig.centerEyeAnchor : null;
            if (cam == null)
            {
                return;
            }

            Vector3 toPanel = _wristMenu.transform.position - cam.position;
            if (toPanel.sqrMagnitude < 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(toPanel.normalized, cam.up);
            float t = 1f - Mathf.Exp(-_wristMenuFacingSmooth * Time.deltaTime);
            _wristMenu.transform.rotation = Quaternion.Slerp(_wristMenu.transform.rotation, targetRotation, t);
        }

        private static Transform FindDeepChild(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            foreach (Transform child in parent)
            {
                Transform found = FindDeepChild(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
