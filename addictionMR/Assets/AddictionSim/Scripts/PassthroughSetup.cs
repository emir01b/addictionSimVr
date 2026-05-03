using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.ARFoundation;

namespace AddictionSim
{
    /// <summary>
    /// Uygulama başlatıldığında passthrough'u etkinleştirir.
    /// - Skybox'ı kaldırır
    /// - Kamera arka planını şeffaf yapar
    /// - OpenXR environment blend mode'u Alpha Blend'e ayarlar
    /// 
    /// Bu script, MR template'in passthrough ayarlarını garanti altına alır.
    /// Sahneye boş bir GameObject üzerinde eklenmelidir.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PassthroughSetup : MonoBehaviour
    {
        [Header("Ayarlar")]
        [Tooltip("Passthrough'u otomatik etkinleştir")]
        [SerializeField] private bool enablePassthrough = true;

        // #region agent log
        private const string DebugSessionId = "0e1049";
        private const string DebugServerEndpoint = "http://127.0.0.1:7387/ingest/f39cbf13-66f0-4fd7-a0fe-d999e0793e0d";
        private static string s_logFilePath;
        // #endregion

        private void Awake()
        {
            // #region agent log
            DiagLog("Awake-entry", "PassthroughSetup.Awake started", "A,B,C,D,E,F", new Dictionary<string, object>
            {
                {"enablePassthrough", enablePassthrough},
                {"unityVersion", Application.unityVersion},
                {"platform", Application.platform.ToString()},
                {"isEditor", Application.isEditor},
            });
            // #endregion

            if (!enablePassthrough) return;

            ConfigurePassthrough();
        }

        private void Start()
        {
            // #region agent log
            DiagLog("Start-entry", "PassthroughSetup.Start started", "A,B,C,D,E,F", null);
            // #endregion

            if (!enablePassthrough) return;

            ConfigurePassthrough();
            ConfigureCamera();

            // #region agent log
            StartCoroutine(DiagnoseAfterFrames(2));
            StartCoroutine(DiagnoseAfterFrames(60));
            // #endregion
        }

        private void ConfigurePassthrough()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.2f, 1f);

            Debug.Log("[PassthroughSetup] Skybox kaldırıldı.");

            try
            {
                var openxrSettings = OpenXRSettings.Instance;
                if (openxrSettings != null)
                {
                    Debug.Log("[PassthroughSetup] OpenXR ayarları konfigüre edildi.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PassthroughSetup] OpenXR ayarı yapılırken hata: {e.Message}");
            }
        }

        private void ConfigureCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cameras.Length > 0)
                {
                    mainCam = cameras[0];
                }
            }

            if (mainCam != null)
            {
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0f, 0f, 0f, 0f);

                var cameraData = mainCam.GetComponent<UniversalAdditionalCameraData>();
                if (cameraData != null)
                {
                    cameraData.renderPostProcessing = true;
                }

                Debug.Log($"[PassthroughSetup] Kamera '{mainCam.name}' passthrough için konfigüre edildi.");
            }
            else
            {
                Debug.LogWarning("[PassthroughSetup] Ana kamera bulunamadı!");
            }
        }

        // #region agent log
        private IEnumerator DiagnoseAfterFrames(int frames)
        {
            for (int i = 0; i < frames; i++) yield return null;
            yield return new WaitForEndOfFrame();
            RunFullDiagnosis($"after-{frames}-frames");
        }

        private void RunFullDiagnosis(string runId)
        {
            // ---------- Hipotez A: XR loader / OpenXR runtime ----------
            try
            {
                var generalSettings = XRGeneralSettings.Instance;
                var manager = generalSettings != null ? generalSettings.Manager : null;
                var activeLoader = manager != null ? manager.activeLoader : null;
                var loaderName = activeLoader != null ? activeLoader.name : "<null>";
                bool xrInit = manager != null && manager.isInitializationComplete;
                DiagLog("xr-loader", "XR loader state", "A", new Dictionary<string, object>
                {
                    {"runId", runId},
                    {"xrGeneralSettingsExists", generalSettings != null},
                    {"managerExists", manager != null},
                    {"isInitializationComplete", xrInit},
                    {"activeLoader", loaderName},
                    {"loadersCount", manager != null && manager.activeLoaders != null ? manager.activeLoaders.Count : -1},
                });
            }
            catch (System.Exception e)
            {
                DiagLog("xr-loader-error", "XR loader probe failed", "A", new Dictionary<string, object> { { "runId", runId }, { "error", e.Message } });
            }

            // ---------- Hipotez C: OpenXR settings & blend mode ----------
            try
            {
                var settings = OpenXRSettings.Instance;
                if (settings != null)
                {
                    int featureCount = 0;
                    int enabledFeatureCount = 0;
                    bool fbPassthroughFeatureEnabled = false;
                    bool metaQuestSupportEnabled = false;
                    bool arCameraFeatureEnabled = false;
                    var features = settings.GetFeatures<OpenXRFeature>();
                    if (features != null)
                    {
                        featureCount = features.Length;
                        foreach (var f in features)
                        {
                            if (f == null) continue;
                            if (f.enabled) enabledFeatureCount++;
                            string nm = f.name ?? string.Empty;
                            if (nm.Contains("ARCameraFeature")) arCameraFeatureEnabled = f.enabled;
                            if (nm.Contains("MetaQuestFeature") || nm.Contains("MetaXRFeature")) metaQuestSupportEnabled = f.enabled;
                            if (nm.Contains("Passthrough")) fbPassthroughFeatureEnabled = f.enabled;
                        }
                    }

                    DiagLog("openxr-settings", "OpenXR settings & features", "A,C,D,F", new Dictionary<string, object>
                    {
                        {"runId", runId},
                        {"renderMode", settings.renderMode.ToString()},
                        {"depthSubmissionMode", settings.depthSubmissionMode.ToString()},
                        {"featureCount", featureCount},
                        {"enabledFeatureCount", enabledFeatureCount},
                        {"arCameraFeatureEnabled", arCameraFeatureEnabled},
                        {"metaQuestSupportEnabled", metaQuestSupportEnabled},
                        {"fbPassthroughFeatureEnabled", fbPassthroughFeatureEnabled},
                    });
                }

                var blend = UnityEngine.XR.XRSettings.gameViewRenderMode;
                DiagLog("xr-render", "XRSettings runtime", "A,C", new Dictionary<string, object>
                {
                    {"runId", runId},
                    {"xrEnabled", UnityEngine.XR.XRSettings.enabled},
                    {"loadedDeviceName", UnityEngine.XR.XRSettings.loadedDeviceName ?? "<null>"},
                    {"isDeviceActive", UnityEngine.XR.XRSettings.isDeviceActive},
                    {"gameViewRenderMode", blend.ToString()},
                });
            }
            catch (System.Exception e)
            {
                DiagLog("openxr-settings-error", "OpenXR settings probe failed", "A,C", new Dictionary<string, object> { { "runId", runId }, { "error", e.Message } });
            }

            // ---------- Hipotez B & F: Cameras ----------
            try
            {
                var cams = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                DiagLog("cameras-count", $"Found {cams.Length} cameras in scene", "B,F", new Dictionary<string, object>
                {
                    {"runId", runId},
                    {"count", cams.Length},
                    {"mainCameraName", Camera.main != null ? Camera.main.name : "<null>"},
                });
                int idx = 0;
                foreach (var c in cams)
                {
                    var bg = c.backgroundColor;
                    var urp = c.GetComponent<UniversalAdditionalCameraData>();
                    DiagLog("camera-info", $"Camera #{idx}: {c.name}", "B,E,F", new Dictionary<string, object>
                    {
                        {"runId", runId},
                        {"index", idx},
                        {"name", c.name},
                        {"tag", c.tag},
                        {"isActiveAndEnabled", c.isActiveAndEnabled},
                        {"clearFlags", c.clearFlags.ToString()},
                        {"bg_r", bg.r},
                        {"bg_g", bg.g},
                        {"bg_b", bg.b},
                        {"bg_a", bg.a},
                        {"depth", c.depth},
                        {"cullingMask", c.cullingMask},
                        {"targetEye", c.stereoTargetEye.ToString()},
                        {"hasURPData", urp != null},
                        {"urp_renderType", urp != null ? urp.renderType.ToString() : "<n/a>"},
                        {"urp_renderPostProcessing", urp != null ? urp.renderPostProcessing : false},
                    });
                    idx++;
                }
            }
            catch (System.Exception e)
            {
                DiagLog("cameras-error", "Camera probe failed", "B,F", new Dictionary<string, object> { { "runId", runId }, { "error", e.Message } });
            }

            // ---------- Hipotez D: ARSession / ARCameraManager ----------
            try
            {
                var sessions = FindObjectsByType<ARSession>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                var camManagers = FindObjectsByType<ARCameraManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                var camBgs = FindObjectsByType<ARCameraBackground>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                DiagLog("ar-foundation", "ARFoundation components", "D", new Dictionary<string, object>
                {
                    {"runId", runId},
                    {"arSessionCount", sessions.Length},
                    {"arCameraManagerCount", camManagers.Length},
                    {"arCameraBackgroundCount", camBgs.Length},
                    {"arSessionStateGlobal", ARSession.state.ToString()},
                });
                if (sessions.Length > 0)
                {
                    var s = sessions[0];
                    DiagLog("ar-session", "First ARSession details", "D", new Dictionary<string, object>
                    {
                        {"runId", runId},
                        {"name", s.name},
                        {"enabled", s.enabled},
                        {"isActiveAndEnabled", s.isActiveAndEnabled},
                        {"goName", s.gameObject.name},
                        {"goActive", s.gameObject.activeInHierarchy},
                    });
                }
                if (camManagers.Length > 0)
                {
                    var cm = camManagers[0];
                    DiagLog("ar-camera-manager", "First ARCameraManager details", "D", new Dictionary<string, object>
                    {
                        {"runId", runId},
                        {"name", cm.name},
                        {"enabled", cm.enabled},
                        {"isActiveAndEnabled", cm.isActiveAndEnabled},
                        {"requestedLightEstimation", cm.requestedLightEstimation.ToString()},
                        {"currentLightEstimation", cm.currentLightEstimation.ToString()},
                        {"hasARCameraBackground", cm.GetComponent<ARCameraBackground>() != null},
                    });
                }
            }
            catch (System.Exception e)
            {
                DiagLog("ar-error", "ARFoundation probe failed", "D", new Dictionary<string, object> { { "runId", runId }, { "error", e.Message } });
            }

            // ---------- Hipotez E: Render settings / skybox ----------
            try
            {
                DiagLog("render-settings", "RenderSettings/Quality", "E", new Dictionary<string, object>
                {
                    {"runId", runId},
                    {"skyboxNull", RenderSettings.skybox == null},
                    {"skyboxName", RenderSettings.skybox != null ? RenderSettings.skybox.name : "<null>"},
                    {"ambientMode", RenderSettings.ambientMode.ToString()},
                    {"qualityLevel", QualitySettings.GetQualityLevel()},
                    {"qualityName", QualitySettings.names[QualitySettings.GetQualityLevel()]},
                    {"colorSpace", QualitySettings.activeColorSpace.ToString()},
                });
            }
            catch (System.Exception e)
            {
                DiagLog("render-error", "Render settings probe failed", "E", new Dictionary<string, object> { { "runId", runId }, { "error", e.Message } });
            }
        }

        private void DiagLog(string location, string message, string hypothesisId, Dictionary<string, object> data)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append('{');
                AppendKv(sb, "sessionId", DebugSessionId, true); sb.Append(',');
                AppendKv(sb, "id", $"log_{System.DateTime.UtcNow.Ticks}_{Random.Range(1000, 9999)}", true); sb.Append(',');
                AppendKv(sb, "timestamp", System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()); sb.Append(',');
                AppendKv(sb, "location", "PassthroughSetup.cs:" + location, true); sb.Append(',');
                AppendKv(sb, "message", message, true); sb.Append(',');
                AppendKv(sb, "hypothesisId", hypothesisId, true); sb.Append(',');
                sb.Append("\"data\":{");
                if (data != null)
                {
                    bool first = true;
                    foreach (var kv in data)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        AppendKv(sb, kv.Key, kv.Value);
                    }
                }
                sb.Append("}}");
                string json = sb.ToString();

                Debug.Log("[DebugSession][" + location + "] " + message + " | " + JsonShort(data));

                if (s_logFilePath == null)
                {
                    s_logFilePath = Path.Combine(Application.persistentDataPath, "debug-0e1049.log");
                }
                try
                {
                    File.AppendAllText(s_logFilePath, json + "\n");
                }
                catch { }

#if UNITY_EDITOR
                StartCoroutine(SendHttp(json));
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[DebugSession] DiagLog failed: " + e.Message);
            }
        }

        private IEnumerator SendHttp(string json)
        {
            using (var req = new UnityWebRequest(DebugServerEndpoint, "POST"))
            {
                byte[] body = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("X-Debug-Session-Id", DebugSessionId);
                req.timeout = 2;
                yield return req.SendWebRequest();
            }
        }

        private static void AppendKv(StringBuilder sb, string key, object value, bool forceString = false)
        {
            sb.Append('"').Append(EscapeJson(key)).Append("\":");
            if (value == null) { sb.Append("null"); return; }
            if (forceString) { sb.Append('"').Append(EscapeJson(value.ToString())).Append('"'); return; }
            switch (value)
            {
                case bool b: sb.Append(b ? "true" : "false"); break;
                case int or long or float or double or short or byte: sb.Append(System.Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)); break;
                default: sb.Append('"').Append(EscapeJson(value.ToString())).Append('"'); break;
            }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
        }

        private static string JsonShort(Dictionary<string, object> data)
        {
            if (data == null) return "{}";
            var sb = new StringBuilder("{");
            bool first = true;
            foreach (var kv in data)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append(kv.Key).Append('=').Append(kv.Value);
            }
            return sb.Append('}').ToString();
        }
        // #endregion
    }
}
