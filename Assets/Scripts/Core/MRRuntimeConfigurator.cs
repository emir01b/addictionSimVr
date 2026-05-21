using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;

namespace AlcoholSimVR.Core
{
    /// <summary>
    /// Uygulama başlangıcında passthrough MR, el takibi ve kamera ayarlarını zorlar.
    /// OVRCameraRig root transformuna dokunmaz.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class MRRuntimeConfigurator : MonoBehaviour
    {
        [Header("Passthrough")]
        [SerializeField] private OVRCameraRig _cameraRig;
        [SerializeField] private OVRManager _ovrManager;
        [SerializeField] private OVRPassthroughLayer _passthroughLayer;

        [Header("Kamera")]
        [SerializeField] private Camera[] _xrCameras;

        [Header("Kontrolör")]
        [SerializeField] private bool _hideControllerModels = true;

        private float _passthroughRetryUntil;

        private void Awake()
        {
            ResolveReferences();
            ConfigurePassthrough();
            ConfigureCameras();
            ConfigureEventSystem();
            HideControllers();
            _passthroughRetryUntil = Time.unscaledTime + 5f;
        }

        private void Start()
        {
            ResolveReferences();
            ConfigurePassthrough();
            ConfigureCameras();
        }

        private void Update()
        {
            if (Time.unscaledTime <= _passthroughRetryUntil)
            {
                ConfigurePassthrough();
            }
        }

        private void ResolveReferences()
        {
            if (_cameraRig == null)
            {
                _cameraRig = FindAnyObjectByType<OVRCameraRig>();
            }

            if (_ovrManager == null)
            {
                if (_cameraRig != null)
                {
                    _ovrManager = _cameraRig.GetComponent<OVRManager>();
                }

                if (_ovrManager == null)
                {
                    _ovrManager = FindAnyObjectByType<OVRManager>();
                }
            }

            if (_passthroughLayer == null)
            {
                _passthroughLayer = FindAnyObjectByType<OVRPassthroughLayer>();
            }

            if (_passthroughLayer == null)
            {
                Transform parent = _ovrManager != null
                    ? _ovrManager.transform
                    : _cameraRig != null
                        ? _cameraRig.transform
                        : transform;
                var passthroughGo = new GameObject("OVRPassthroughLayer");
                passthroughGo.transform.SetParent(parent, false);
                _passthroughLayer = passthroughGo.AddComponent<OVRPassthroughLayer>();
            }

            if (_xrCameras == null || _xrCameras.Length == 0)
            {
                if (_cameraRig != null)
                {
                    _xrCameras = _cameraRig.GetComponentsInChildren<Camera>(true);
                }
                else if (Camera.main != null)
                {
                    _xrCameras = new[] { Camera.main };
                }
            }
        }

        private void ConfigurePassthrough()
        {
            if (_ovrManager != null)
            {
                _ovrManager.isInsightPassthroughEnabled = true;
                _ovrManager.launchSimultaneousHandsControllersOnStartup = false;
                _ovrManager.SimultaneousHandsAndControllersEnabled = false;
                _ovrManager.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
            }

            if (OVRManager.instance != null && OVRManager.instance != _ovrManager)
            {
                OVRManager.instance.isInsightPassthroughEnabled = true;
                OVRManager.instance.launchSimultaneousHandsControllersOnStartup = false;
                OVRManager.instance.SimultaneousHandsAndControllersEnabled = false;
                OVRManager.instance.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;
            }

            if (_passthroughLayer != null)
            {
                _passthroughLayer.gameObject.SetActive(true);
                _passthroughLayer.enabled = true;
                _passthroughLayer.overlayType = OVROverlay.OverlayType.Underlay;
                _passthroughLayer.hidden = false;
                _passthroughLayer.textureOpacity = 1f;
            }
        }

        private void ConfigureCameras()
        {
            if (_xrCameras == null)
            {
                return;
            }

            foreach (Camera cam in _xrCameras)
            {
                if (cam == null)
                {
                    continue;
                }

                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);

                var urp = cam.GetComponent<UniversalAdditionalCameraData>();
                if (urp != null)
                {
                    urp.renderPostProcessing = false;
                }
            }

            EnsureMainCameraIsRigCamera();
            DisableNonRigCameras();
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        }

        private void EnsureMainCameraIsRigCamera()
        {
            if (_xrCameras == null || _xrCameras.Length == 0)
            {
                return;
            }

            Camera preferredCamera = _cameraRig != null && _cameraRig.centerEyeAnchor != null
                ? _cameraRig.centerEyeAnchor.GetComponentInChildren<Camera>(true)
                : null;

            foreach (Camera cam in _xrCameras)
            {
                if (cam == null)
                {
                    continue;
                }

                if (preferredCamera != null && cam != preferredCamera && cam.CompareTag("MainCamera"))
                {
                    cam.tag = "Untagged";
                    continue;
                }

                if (preferredCamera != null && cam != preferredCamera)
                {
                    continue;
                }

                try
                {
                    cam.tag = "MainCamera";
                }
                catch
                {
                    // MainCamera is built in, but keep runtime resilient if tags are edited.
                }

                return;
            }
        }

        private void DisableNonRigCameras()
        {
            if (_cameraRig == null)
            {
                return;
            }

            foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (cam == null || cam.transform.IsChildOf(_cameraRig.transform))
                {
                    continue;
                }

                cam.enabled = false;
                var listener = cam.GetComponent<AudioListener>();
                if (listener != null)
                {
                    listener.enabled = false;
                }

                if (cam.CompareTag("MainCamera"))
                {
                    cam.tag = "Untagged";
                }
            }
        }

        private void ConfigureEventSystem()
        {
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
                go.AddComponent<OVRInputModule>();
                return;
            }

            var standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                Destroy(standalone);
            }

            if (eventSystem.GetComponent<OVRInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<OVRInputModule>();
            }
        }

        private void HideControllers()
        {
            if (!_hideControllerModels)
            {
                return;
            }

            foreach (var helper in FindObjectsByType<OVRControllerHelper>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (helper != null)
                {
                    helper.gameObject.SetActive(false);
                }
            }
        }
    }
}
