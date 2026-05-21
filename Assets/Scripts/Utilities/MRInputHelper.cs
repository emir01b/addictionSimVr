using UnityEngine;

namespace AlcoholSimVR.Utilities
{
    /// <summary>
    /// El pinch ve (yedek) kontrolör girişlerini birleştirir.
    /// </summary>
    public static class MRInputHelper
    {
        private static OVRHand _leftHand;
        private static OVRHand _rightHand;
        private static OVRSkeleton _rightSkeleton;
        private static bool _rightWasPinching;

        /// <summary>El takibi bileşenlerini kaydeder.</summary>
        public static void RegisterHands(OVRHand leftHand, OVRHand rightHand)
        {
            _leftHand = leftHand;
            _rightHand = rightHand;
            _rightSkeleton = rightHand != null ? rightHand.GetComponent<OVRSkeleton>() : null;
        }

        /// <summary>Sağ el işaret parmağı pinch bu karede başladı mı?</summary>
        public static bool GetRightSelectDown()
        {
            if (TryGetHandPinchDown(_rightHand, ref _rightWasPinching))
            {
                return true;
            }

            return GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)
                || GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        }

        /// <summary>Sağ el pinch veya tetik basılı mı?</summary>
        public static bool GetRightSelectHeld()
        {
            if (_rightHand != null && _rightHand.IsTracked && _rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
            {
                return true;
            }

            return Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch)
                || Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        }

        /// <summary>Sağ el işaret ray'ini dünya koordinatında verir.</summary>
        public static bool TryGetRightPointerRay(out Ray ray)
        {
            if (_rightHand != null && _rightHand.IsTracked && _rightHand.PointerPose != null)
            {
                ray = new Ray(_rightHand.PointerPose.position, _rightHand.PointerPose.forward);
                return true;
            }

            try
            {
                if (OVRInput.GetControllerPositionValid(OVRInput.Controller.RHand))
                {
                    Vector3 position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RHand);
                    Quaternion rotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RHand);
                    ray = new Ray(position, rotation * Vector3.forward);
                    return true;
                }

                if (OVRInput.GetControllerPositionValid(OVRInput.Controller.RTouch))
                {
                    Vector3 position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
                    Quaternion rotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
                    ray = new Ray(position, rotation * Vector3.forward);
                    return true;
                }
            }
            catch
            {
                // OVR hazÄ±r deÄŸil
            }

            ray = default;
            return false;
        }

        /// <summary>Sağ işaret parmağı ucunun dünya pozisyonunu verir.</summary>
        public static bool TryGetRightIndexTip(out Vector3 position)
        {
            if (_rightHand == null || !_rightHand.IsTracked)
            {
                position = default;
                return false;
            }

            if (_rightSkeleton == null || _rightSkeleton.gameObject != _rightHand.gameObject)
            {
                _rightSkeleton = _rightHand.GetComponent<OVRSkeleton>();
            }

            if (_rightSkeleton != null
                && _rightSkeleton.IsDataValid
                && TryGetBonePosition(_rightSkeleton, OVRSkeleton.BoneId.Hand_IndexTip, out position))
            {
                return true;
            }

            if (_rightSkeleton != null
                && _rightSkeleton.IsDataValid
                && TryGetBonePosition(_rightSkeleton, OVRSkeleton.BoneId.Hand_Index3, out position))
            {
                return true;
            }

            if (_rightHand.PointerPose != null && _rightHand.IsPointerPoseValid)
            {
                position = _rightHand.PointerPose.position + _rightHand.PointerPose.forward * 0.06f;
                return true;
            }

            position = default;
            return false;
        }

        /// <summary>Kısa titreşim (el takibinde sessiz kalır).</summary>
        public static void TriggerHaptic(OVRInput.Controller controller, float frequency = 0.5f, float amplitude = 0.6f)
        {
            try
            {
                OVRInput.SetControllerVibration(frequency, amplitude, controller);
            }
            catch
            {
                // OVR hazır değil
            }
        }

        public static void TriggerRightHaptic()
        {
            TriggerHaptic(OVRInput.Controller.RTouch);
        }

        private static bool TryGetHandPinchDown(OVRHand hand, ref bool wasPinching)
        {
            if (hand == null || !hand.IsTracked)
            {
                wasPinching = false;
                return false;
            }

            bool pinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
            bool down = pinching && !wasPinching;
            wasPinching = pinching;
            return down;
        }

        private static bool TryGetBonePosition(OVRSkeleton skeleton, OVRSkeleton.BoneId boneId, out Vector3 position)
        {
            int index = (int)boneId;
            if (skeleton.Bones != null && index >= 0 && skeleton.Bones.Count > index)
            {
                var bone = skeleton.Bones[index];
                if (bone?.Transform != null)
                {
                    position = bone.Transform.position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        private static bool Get(OVRInput.Button button, OVRInput.Controller controller)
        {
            try
            {
                return OVRInput.Get(button, controller);
            }
            catch
            {
                return false;
            }
        }

        private static bool GetDown(OVRInput.Button button, OVRInput.Controller controller)
        {
            try
            {
                return OVRInput.GetDown(button, controller);
            }
            catch
            {
                return false;
            }
        }
    }
}
