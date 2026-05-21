#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using AlcoholSimVR.Core;
using AlcoholSimVR.Simulation;
using AlcoholSimVR.UI;
using AlcoholSimVR.Utilities;

namespace AlcoholSimVR.Editor
{
    /// <summary>
    /// MainScene hiyerarşisini, passthrough ayarlarını ve temel prefabları oluşturur.
    /// Menü: AlcoholSimVR / Kurulum Sihirbazı
    /// </summary>
    public static class AlcoholSimSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/MainScene.unity";
        private const string BoardMatPath = "Assets/Materials/Board_MR.mat";
        private const string UiMatPath = "Assets/Materials/UI_Dark.mat";
        private const string BeamPrefabPath = "Assets/Prefabs/Board/BeamBoard.prefab";
        private const string VolumeProfilePath = "Assets/Settings/AlcoholPostProcessProfile.asset";

        [MenuItem("AlcoholSimVR/1 - Proje Ayarlarını Uygula (Passthrough + Eller)", false, 1)]
        public static void ApplyProjectSettings()
        {
            var config = OVRProjectConfig.CachedProjectConfig;
            if (config == null)
            {
                Debug.LogError("[AlcoholSimVR] OVRProjectConfig bulunamadı.");
                return;
            }

            config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.HandsOnly;
            config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Required;
            EditorUtility.SetDirty(config);
            EnsureMainSceneInBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[AlcoholSimVR] Proje ayarları: HandsOnly + Passthrough Required.");
        }

        [MenuItem("AlcoholSimVR/2 - Mevcut Sahneyi Onar (Passthrough + Eller)", false, 2)]
        public static void RepairActiveScene()
        {
            ApplyProjectSettings();

            var systems = GameObject.Find("--- AlcoholSimVR ---");
            if (systems == null)
            {
                systems = new GameObject("--- AlcoholSimVR ---");
            }

            var rig = Object.FindAnyObjectByType<OVRCameraRig>();
            var manager = Object.FindAnyObjectByType<OVRManager>();
            var passthroughLayer = EnsurePassthroughLayerInScene(rig);

            var configurator = systems.GetComponent<MRRuntimeConfigurator>();
            if (configurator == null)
            {
                configurator = systems.AddComponent<MRRuntimeConfigurator>();
            }

            if (systems.GetComponent<PassthroughBootstrap>() == null)
            {
                systems.AddComponent<PassthroughBootstrap>();
            }

            var palm = systems.GetComponent<HandPalmDetector>() ?? systems.AddComponent<HandPalmDetector>();
            var wrist = Object.FindAnyObjectByType<WristMenuPanel>();
            var setup = systems.GetComponent<HandTrackingSetup>();
            if (setup == null)
            {
                setup = systems.AddComponent<HandTrackingSetup>();
            }

            SetSerialized(setup, "_palmDetector", palm);
            SetSerialized(setup, "_wristMenu", wrist);
            SetSerialized(setup, "_cameraRig", rig);

            if (manager != null)
            {
                manager.isInsightPassthroughEnabled = true;
                manager.launchSimultaneousHandsControllersOnStartup = false;
                manager.SimultaneousHandsAndControllersEnabled = false;
                EditorUtility.SetDirty(manager);
            }

            SetSerialized(configurator, "_cameraRig", rig);
            SetSerialized(configurator, "_ovrManager", manager);
            SetSerialized(configurator, "_passthroughLayer", passthroughLayer);
            if (rig != null)
            {
                SetSerialized(configurator, "_xrCameras", rig.GetComponentsInChildren<Camera>(true));
            }

            FixEventSystemInScene();
            FixCamerasInScene();
            HideControllerModelsInScene();
            RemoveOrphanLeftHandSkeleton();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[AlcoholSimVR] Sahne onarımı tamamlandı. Sahneyi kaydedip yeniden build alın.");
        }

        [MenuItem("AlcoholSimVR/3 - Kurulum Sihirbazı (MainScene)", false, 3)]
        public static void RunSetupWizard()
        {
            ApplyProjectSettings();
            EnsureFolders();
            Material boardMat = CreateBoardMaterial();
            CreateUiMaterial();
            VolumeProfile volumeProfile = CreatePostProcessProfile();
            GameObject beamPrefab = CreateBeamPrefab(boardMat);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Temel ışık/skybox kaldır
            var light = Object.FindAnyObjectByType<Light>();
            if (light != null)
            {
                light.gameObject.SetActive(false);
            }

            RenderSettings.skybox = null;

            GameObject rigRoot = CreateCameraRig(volumeProfile, out Transform swayOffset, out Camera centerCam);
            GameObject systems = new GameObject("--- AlcoholSimVR ---");

            var palmDetector = systems.AddComponent<HandPalmDetector>();
            var sessionTracker = systems.AddComponent<SessionTracker>();
            var boardManager = systems.AddComponent<BoardManager>();
            var alcoholFx = systems.AddComponent<AlcoholEffectController>();
            var appManager = systems.AddComponent<AppManager>();
            systems.AddComponent<PassthroughBootstrap>();
            systems.AddComponent<MRRuntimeConfigurator>();
            var handSetup = systems.AddComponent<HandTrackingSetup>();

            // Wrist menu
            Transform leftAnchor = FindChildRecursive(rigRoot.transform, "LeftHandAnchor")
                ?? FindChildRecursive(rigRoot.transform, "LeftControllerAnchor");
            GameObject wristMenu = CreateWristMenu(leftAnchor != null ? leftAnchor : rigRoot.transform);
            var wristPanel = wristMenu.GetComponent<WristMenuPanel>();

            GameObject infoPanel = CreateInfoPanel();
            GameObject resultsPanel = CreateResultsPanel();
            GameObject hud = CreateSimulationHud();
            WireSessionHud(sessionTracker, hud);

            // Post process volume
            var volumeGo = new GameObject("AlcoholPostProcessVolume");
            volumeGo.transform.SetParent(systems.transform, false);
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.profile = volumeProfile;
            volume.weight = 0f;
            volume.priority = 10f;

            SetSerialized(alcoholFx, "_postProcessVolume", volume);
            SetSerialized(alcoholFx, "_cameraSwayOffset", swayOffset);
            SetSerialized(boardManager, "_beamBoardPrefab", beamPrefab);
            SetSerialized(boardManager, "_sessionTracker", sessionTracker);

            SetSerialized(appManager, "_palmDetector", palmDetector);
            SetSerialized(appManager, "_wristMenu", wristPanel);
            SetSerialized(appManager, "_infoPanel", infoPanel.GetComponent<InfoPanelController>());
            SetSerialized(appManager, "_resultsPanel", resultsPanel.GetComponent<ResultsPanelController>());
            SetSerialized(appManager, "_boardManager", boardManager);
            SetSerialized(appManager, "_alcoholEffects", alcoholFx);
            SetSerialized(appManager, "_sessionTracker", sessionTracker);

            SetSerialized(handSetup, "_palmDetector", palmDetector);
            SetSerialized(handSetup, "_wristMenu", wristPanel);
            SetSerialized(handSetup, "_cameraRig", rigRoot.GetComponent<OVRCameraRig>());

            // Passthrough layer
            var ptGo = new GameObject("OVRPassthroughLayer");
            ptGo.transform.SetParent(rigRoot.transform, false);
            var ptLayer = ptGo.AddComponent<OVRPassthroughLayer>();

            var configurator = systems.GetComponent<MRRuntimeConfigurator>();
            SetSerialized(configurator, "_ovrManager", rigRoot.GetComponent<OVRManager>());
            SetSerialized(configurator, "_passthroughLayer", ptLayer);
            SetSerialized(configurator, "_xrCameras", new[] { centerCam });

            // EventSystem + OVRInputModule (el pinch)
            var eventSystem = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<OVRInputModule>();
            }
            else
            {
                var standalone = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (standalone != null)
                {
                    Object.DestroyImmediate(standalone);
                }

                if (eventSystem.GetComponent<OVRInputModule>() == null)
                {
                    eventSystem.gameObject.AddComponent<OVRInputModule>();
                }
            }

            // Player trigger on camera for beam detection
            var playerTrigger = new GameObject("PlayerTrigger");
            playerTrigger.transform.SetParent(centerCam.transform, false);
            if (!TagExists("Player"))
            {
                Debug.LogWarning("[AlcoholSimVR] 'Player' tag oluşturun: Edit → Project Settings → Tags");
            }
            else
            {
                playerTrigger.tag = "Player";
            }

            var sphere = playerTrigger.AddComponent<SphereCollider>();
            sphere.isTrigger = false;
            sphere.radius = 0.15f;
            var rb = playerTrigger.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[AlcoholSimVR] Kurulum tamamlandı: {ScenePath}");
        }

        private static void FixEventSystemInScene()
        {
            var es = Object.FindAnyObjectByType<EventSystem>();
            if (es == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<OVRInputModule>();
                return;
            }

            var standalone = es.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                Object.DestroyImmediate(standalone);
            }

            if (es.GetComponent<OVRInputModule>() == null)
            {
                es.gameObject.AddComponent<OVRInputModule>();
            }
        }

        private static void FixCamerasInScene()
        {
            var rig = Object.FindAnyObjectByType<OVRCameraRig>();
            if (rig == null)
            {
                return;
            }

            Camera mainRigCamera = rig.centerEyeAnchor != null
                ? rig.centerEyeAnchor.GetComponentInChildren<Camera>(true)
                : null;
            bool mainAssigned = false;

            foreach (var cam in rig.GetComponentsInChildren<Camera>(true))
            {
                cam.enabled = true;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);

                var urp = cam.GetComponent<UniversalAdditionalCameraData>();
                if (urp != null)
                {
                    urp.renderPostProcessing = false;
                }

                if (!mainAssigned && (cam == mainRigCamera || mainRigCamera == null))
                {
                    cam.tag = "MainCamera";
                    mainAssigned = true;
                }
                else if (cam.CompareTag("MainCamera"))
                {
                    cam.tag = "Untagged";
                }

                EditorUtility.SetDirty(cam);
            }

            foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (cam == null || cam.transform.IsChildOf(rig.transform))
                {
                    continue;
                }

                cam.enabled = false;
                if (cam.CompareTag("MainCamera"))
                {
                    cam.tag = "Untagged";
                }

                var listener = cam.GetComponent<AudioListener>();
                if (listener != null)
                {
                    listener.enabled = false;
                    EditorUtility.SetDirty(listener);
                }

                EditorUtility.SetDirty(cam);
            }

            RenderSettings.skybox = null;
        }

        private static OVRPassthroughLayer EnsurePassthroughLayerInScene(OVRCameraRig rig)
        {
            var layers = Object.FindObjectsByType<OVRPassthroughLayer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            var layer = layers.Length > 0 ? layers[0] : null;

            if (layer == null)
            {
                var go = new GameObject("OVRPassthroughLayer");
                if (rig != null)
                {
                    go.transform.SetParent(rig.transform, false);
                }

                layer = go.AddComponent<OVRPassthroughLayer>();
            }

            layer.gameObject.SetActive(true);
            layer.enabled = true;
            layer.overlayType = OVROverlay.OverlayType.Underlay;
            layer.hidden = false;
            layer.textureOpacity = 1f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        private static void HideControllerModelsInScene()
        {
            foreach (var helper in Object.FindObjectsByType<OVRControllerHelper>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                helper.gameObject.SetActive(false);
            }
        }

        private static void RemoveOrphanLeftHandSkeleton()
        {
            var orphan = GameObject.Find("LeftHandSkeleton");
            if (orphan != null && orphan.GetComponentInParent<OVRHand>() == null)
            {
                Object.DestroyImmediate(orphan);
            }
        }

        private static void EnsureFolders()
        {
            foreach (string folder in new[]
            {
                "Assets/Scripts", "Assets/Scenes", "Assets/Materials", "Assets/Prefabs/UI",
                "Assets/Prefabs/Board", "Assets/Settings"
            })
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
        }

        private static void EnsureMainSceneInBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        private static GameObject CreateCameraRig(VolumeProfile profile, out Transform swayOffset, out Camera centerCam)
        {
            // Meta Building Block veya mevcut OVRCameraRig
            var existing = Object.FindAnyObjectByType<OVRCameraRig>();
            if (existing != null)
            {
                swayOffset = CreateSwayOffset(existing.centerEyeAnchor, out centerCam);
                return existing.gameObject;
            }

            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Packages/com.meta.xr.sdk.core/Prefabs/OVRCameraRig.prefab");
            GameObject rig;
            if (rigPrefab != null)
            {
                rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            }
            else
            {
                rig = new GameObject("OVRCameraRig");
                rig.AddComponent<OVRCameraRig>();
                rig.AddComponent<OVRManager>();
            }

            var ovrRig = rig.GetComponent<OVRCameraRig>();
            swayOffset = CreateSwayOffset(ovrRig.centerEyeAnchor, out centerCam);
            return rig;
        }

        private static Transform CreateSwayOffset(Transform centerEyeAnchor, out Camera centerCam)
        {
            centerCam = centerEyeAnchor.GetComponent<Camera>();
            if (centerCam == null)
            {
                centerCam = centerEyeAnchor.gameObject.AddComponent<Camera>();
            }

            var swayGo = new GameObject("CameraSwayOffset");
            swayGo.transform.SetParent(centerEyeAnchor.parent, false);
            int index = centerEyeAnchor.GetSiblingIndex();
            swayGo.transform.SetSiblingIndex(index);

            centerEyeAnchor.SetParent(swayGo.transform, false);
            Transform result = swayGo.transform;
            return result;
        }

        private static GameObject CreateWristMenu(Transform parent)
        {
            var root = new GameObject("WristMenuPanel");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0.05f, 0f, 0.05f);
            root.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(260f, 170f);
            rect.localScale = Vector3.one * 0.0012f;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 20f;
            var group = root.AddComponent<CanvasGroup>();
            var fade = root.AddComponent<CanvasFadeAnimator>();
            var panel = root.AddComponent<WristMenuPanel>();
            var billboard = root.AddComponent<WorldSpaceBillboard>();
            root.AddComponent<OVRRaycaster>();

            CreateUiPanelBackground(root.transform, "Panel");
            var btn = CreateButton(root.transform, "DUZ TAHTA", new Vector2(0, -28));
            var title = CreateTmp(root.transform, "Title", 22, new Vector2(0, 45));
            title.text = "DENGE MODU";
            title.fontStyle = FontStyles.Bold;
            btn.GetComponent<RectTransform>().sizeDelta = new Vector2(220, 58);
            btn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -28);
            SetSerialized(fade, "_canvasGroup", group);
            SetSerialized(panel, "_fadeAnimator", fade);
            SetSerialized(panel, "_canvasGroup", group);
            SetSerialized(panel, "_straightBeamButton", btn.GetComponent<MRUIButton>());
            SetSerialized(panel, "_billboard", billboard);

            return root;
        }

        private static GameObject CreateInfoPanel()
        {
            var root = CreateWorldPanel("InfoPanel", new Vector2(420, 280), typeof(InfoPanelController));
            var controller = root.GetComponent<InfoPanelController>();
            var billboard = root.GetComponent<WorldSpaceBillboard>();
            if (billboard != null)
            {
                billboard.enabled = false;
            }

            var title = CreateTmp(root.transform, "Title", 20, new Vector2(0, 94));
            var body = CreateTmp(root.transform, "Body", 15, new Vector2(0, 10));
            body.rectTransform.sizeDelta = new Vector2(360, 132);
            var btn = CreateButton(root.transform, "Başlat", new Vector2(0, -150));
            var btnRect = btn.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(170, 42);
            btnRect.anchoredPosition = new Vector2(0, -104);
            SetSerialized(controller, "_titleText", title);
            SetSerialized(controller, "_bodyText", body);
            SetSerialized(controller, "_startButton", btn.GetComponent<MRUIButton>());
            SetSerialized(controller, "_canvasGroup", root.GetComponent<CanvasGroup>());
            SetSerialized(controller, "_panelRoot", root.GetComponent<RectTransform>());
            SetSerialized(controller, "_billboard", billboard);
            return root;
        }

        private static GameObject CreateResultsPanel()
        {
            var root = CreateWorldPanel("ResultsPanel", new Vector2(500, 420), typeof(ResultsPanelController));
            var c = root.GetComponent<ResultsPanelController>();
            var duration = CreateTmp(root.transform, "Duration", 24, new Vector2(0, 120));
            var step = CreateTmp(root.transform, "StepOff", 24, new Vector2(0, 70));
            var score = CreateTmp(root.transform, "Score", 26, new Vector2(0, 20));
            var feedback = CreateTmp(root.transform, "Feedback", 24, new Vector2(0, -30));
            var btn = CreateButton(root.transform, "Kapat", new Vector2(0, -140));
            SetSerialized(c, "_durationText", duration);
            SetSerialized(c, "_stepOffText", step);
            SetSerialized(c, "_scoreText", score);
            SetSerialized(c, "_feedbackText", feedback);
            SetSerialized(c, "_closeButton", btn.GetComponent<MRUIButton>());
            SetSerialized(c, "_canvasGroup", root.GetComponent<CanvasGroup>());
            return root;
        }

        private static GameObject CreateSimulationHud()
        {
            var root = CreateWorldPanel("SimulationHUD", new Vector2(300, 120), typeof(SimulationHudController));
            var timer = CreateTmp(root.transform, "Timer", 22, new Vector2(-40, 0));
            var step = CreateTmp(root.transform, "Step", 22, new Vector2(80, 0));
            var stopBtn = CreateButton(root.transform, "Durdur", new Vector2(0, -40));
            root.GetComponent<CanvasGroup>().alpha = 0f;
            return root;
        }

        private static GameObject CreateWorldPanel(string name, Vector2 size, System.Type extraComponent)
        {
            var root = new GameObject(name);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.localScale = Vector3.one * 0.001f;
            root.AddComponent<CanvasScaler>();
            root.AddComponent<CanvasGroup>();
            root.AddComponent<WorldSpaceBillboard>();
            root.AddComponent<OVRRaycaster>();
            if (extraComponent != null)
            {
                root.AddComponent(extraComponent);
            }

            CreateUiPanelBackground(root.transform, "Background");
            return root;
        }

        private static void CreateUiPanelBackground(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.7f);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject CreateButton(Transform parent, string label, Vector2 anchoredPos)
        {
            var go = new GameObject(label + "_Button");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280, 56);
            rect.anchoredPosition = anchoredPos;
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0.83f, 1f, 0.35f);
            go.AddComponent<MRUIButton>();
            var text = CreateTmp(go.transform, "Text", 24, Vector2.zero);
            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            return go;
        }

        private static TextMeshProUGUI CreateTmp(Transform parent, string name, float fontSize, Vector2 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(460, 60);
            rect.anchoredPosition = pos;
            return tmp;
        }

        private static Material CreateBoardMaterial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(BoardMatPath);
            if (mat != null)
            {
                return mat;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            mat = new Material(shader)
            {
                color = new Color(0.55f, 0.35f, 0.15f, 0.65f)
            };
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0.4f, 1f, 0.5f) * 0.6f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            AssetDatabase.CreateAsset(mat, BoardMatPath);
            return mat;
        }

        private static void CreateUiMaterial()
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(UiMatPath) != null)
            {
                return;
            }

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { color = new Color(0f, 0f, 0f, 0.7f) };
            AssetDatabase.CreateAsset(mat, UiMatPath);
        }

        private static VolumeProfile CreatePostProcessProfile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
            if (profile != null)
            {
                return profile;
            }

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.Add<ChromaticAberration>(true);
            profile.Add<LensDistortion>(true);
            profile.Add<Vignette>(true);
            profile.Add<DepthOfField>(true);
            AssetDatabase.CreateAsset(profile, VolumeProfilePath);
            return profile;
        }

        private static GameObject CreateBeamPrefab(Material mat)
        {
            // Mevcut prefab'ı sil — güncel boyutlarla yeniden oluştur
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BeamPrefabPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(BeamPrefabPath);
            }

            // --- Tahta boyutları (BoardManager varsayılanlarıyla eşleşmeli) ---
            const float plankWidth  = 0.20f;  // 20 cm genişlik
            const float plankHeight = 0.03f;  // 3 cm yükseklik
            const float plankLength = 3.0f;   // 3 m uzunluk

            var root = new GameObject("BeamBoard");

            // Görsel tahta
            var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plank.name = "Plank";
            plank.transform.SetParent(root.transform, false);
            plank.transform.localScale = new Vector3(plankWidth, plankHeight, plankLength);
            Object.DestroyImmediate(plank.GetComponent<BoxCollider>());
            var renderer = plank.GetComponent<MeshRenderer>();
            if (renderer != null && mat != null)
            {
                renderer.sharedMaterial = mat;
            }

            // Yürüme tetikleyicisi — tahtanın biraz içinde
            var walk = new GameObject("WalkTrigger");
            walk.transform.SetParent(root.transform, false);
            walk.transform.localScale = new Vector3(plankWidth * 0.8f, 0.10f, plankLength * 0.93f);
            var walkCol = walk.AddComponent<BoxCollider>();
            walkCol.isTrigger = true;
            walk.AddComponent<BeamWalkTrigger>();

            // Yan sınır tetikleyicileri — tahtanın kenarlarında
            float sideOffsetX = (plankWidth * 0.5f) + 0.03f;
            foreach (string side in new[] { "LeftSide", "RightSide" })
            {
                var sideGo = new GameObject(side);
                sideGo.transform.SetParent(root.transform, false);
                sideGo.transform.localPosition = new Vector3(
                    side.StartsWith("L") ? -sideOffsetX : sideOffsetX, 0f, 0f);
                sideGo.transform.localScale = new Vector3(0.04f, 0.12f, plankLength * 0.93f);
                var col = sideGo.AddComponent<BoxCollider>();
                col.isTrigger = true;
                sideGo.AddComponent<BeamWalkTrigger>();
            }

            // Başlangıç ve bitiş işaretçileri
            float markerZ = (plankLength * 0.5f) - 0.1f;
            float markerY = plankHeight + 0.005f;
            CreateMarker(root.transform, "StartMarker", new Vector3(0, markerY, -markerZ), Color.green);
            CreateMarker(root.transform, "EndMarker",   new Vector3(0, markerY,  markerZ), Color.cyan);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, BeamPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateMarker(Transform parent, string name, Vector3 localPos, Color color)
        {
            var m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            m.name = name;
            m.transform.SetParent(parent, false);
            m.transform.localPosition = localPos;
            m.transform.localScale = new Vector3(0.18f, 0.005f, 0.18f);
            Object.DestroyImmediate(m.GetComponent<Collider>());
            var r = m.GetComponent<MeshRenderer>();
            if (r != null)
            {
                var markerMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                markerMat.SetColor("_BaseColor", color);
                markerMat.EnableKeyword("_EMISSION");
                markerMat.SetColor("_EmissionColor", color * 1.5f);
                r.sharedMaterial = markerMat;
            }
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            foreach (Transform child in parent)
            {
                var found = FindChildRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void WireSessionHud(SessionTracker tracker, GameObject hudRoot)
        {
            if (tracker == null || hudRoot == null)
            {
                return;
            }

            var timer = hudRoot.transform.Find("Timer")?.GetComponent<TextMeshProUGUI>();
            var step = hudRoot.transform.Find("Step")?.GetComponent<TextMeshProUGUI>();
            var group = hudRoot.GetComponent<CanvasGroup>();
            SetSerialized(tracker, "_hudCanvasGroup", group);
            SetSerialized(tracker, "_timerText", timer);
            SetSerialized(tracker, "_stepOffText", step);
        }

        private static bool TagExists(string tag)
        {
            try
            {
                GameObject.FindGameObjectWithTag(tag);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static void SetSerialized(Object target, string fieldName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetSerialized(Object target, string fieldName, Object[] values)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null && prop.isArray)
            {
                prop.arraySize = values != null ? values.Length : 0;
                for (int i = 0; i < prop.arraySize; i++)
                {
                    prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetSerializedEnum(Object target, string fieldName, int enumIndex)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop != null)
            {
                prop.enumValueIndex = enumIndex;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
