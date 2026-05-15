using UnityEngine;
using UnityEngine.XR.Templates.MR;

namespace AlcoholAwareness
{
    /// <summary>
    /// Disables/removes the MR Template's default UI systems at runtime.
    /// 
    /// The MR Template includes several default UI elements that are not needed
    /// for the Alcohol Awareness application:
    /// 
    /// 1. CoachingUI — Welcome/onboarding panel (passthrough toggle, find surfaces, etc.)
    /// 2. GoalManager — Onboarding goal system that controls CoachingUI flow
    /// 3. HandMenuSetupVariant_MRTemplate — Default hand menu with settings toggles
    /// 4. TutorialPlayer — Video tutorial player
    /// 5. GazeTooltips — "Tap anywhere on surface" tooltip
    /// 6. SpawnedObjectsManager — Object spawning system
    /// 
    /// This script must run BEFORE other scripts (Execution Order: -100).
    /// It finds and disables these components/objects while preserving:
    /// - MR Interaction Setup (XR Origin, hand tracking, poke interaction)
    /// - ARFeatureController (passthrough — we force it ON)
    /// - Permissions Manager
    /// 
    /// Add this script to the AlcoholAwarenessManager GameObject.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class TemplateCleanup : MonoBehaviour
    {
        [Header("Ayarlar")]
        [Tooltip("Passthrough'u otomatik olarak açık başlat")]
        [SerializeField] bool m_ForcePassthroughOn = true;

        [Tooltip("Template UI objelerini tamamen yok et (false = sadece devre dışı bırak)")]
        [SerializeField] bool m_DestroyInsteadOfDisable = false;

        void Awake()
        {
            Debug.Log("[AlcoholAwareness] Template temizliği başlatılıyor...");

            DisableGoalManager();
            DisableCoachingUI();
            DisableHandMenu();
            DisableTutorialPlayer();
            DisableGazeTooltips();
            DisableSpawnedObjectsManager();

            if (m_ForcePassthroughOn)
            {
                ForcePassthroughOn();
            }

            Debug.Log("[AlcoholAwareness] Template temizliği tamamlandı. Passthrough açık.");
        }

        /// <summary>
        /// Disables the GoalManager component which controls the onboarding flow.
        /// Located inside the "MR Interaction Setup" prefab.
        /// </summary>
        void DisableGoalManager()
        {
            var goalManager = FindAnyObjectByType<GoalManager>(FindObjectsInactive.Include);
            if (goalManager != null)
            {
                goalManager.enabled = false;
                Debug.Log("[TemplateCleanup] GoalManager devre dışı bırakıldı.");
            }
        }

        /// <summary>
        /// Disables/destroys the CoachingUI panel — the welcome/onboarding UI.
        /// This is a child of the "UI" root object in the scene.
        /// </summary>
        void DisableCoachingUI()
        {
            // CoachingUI is typically a LazyFollow-attached panel
            // Search by known component types and name patterns
            var allObjects = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allObjects)
            {
                if (t.name.Contains("Coaching") || t.name.Contains("coaching"))
                {
                    DisableOrDestroy(t.gameObject, "CoachingUI");
                }
            }
        }

        /// <summary>
        /// Disables/destroys the default HandMenu from the MR Template.
        /// Our custom HandMenuController replaces this functionality.
        /// </summary>
        void DisableHandMenu()
        {
            var allObjects = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allObjects)
            {
                // The template hand menu has variants of these names
                if (t.name.Contains("HandMenu") || t.name.Contains("Hand Menu"))
                {
                    DisableOrDestroy(t.gameObject, "HandMenu");
                }
            }
        }

        /// <summary>
        /// Disables/destroys the TutorialPlayer video player.
        /// </summary>
        void DisableTutorialPlayer()
        {
            var allObjects = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allObjects)
            {
                if (t.name.Contains("Tutorial Player") || t.name.Contains("TutorialPlayer"))
                {
                    DisableOrDestroy(t.gameObject, "TutorialPlayer");
                }
            }
        }

        /// <summary>
        /// Disables the GazeTooltips component which shows "Tap anywhere on surface" tooltip.
        /// </summary>
        void DisableGazeTooltips()
        {
            var gazeTooltips = FindAnyObjectByType<GazeTooltips>(FindObjectsInactive.Include);
            if (gazeTooltips != null)
            {
                gazeTooltips.enabled = false;
                // Also disable the tooltip object itself
                var tooltip = gazeTooltips.transform.parent;
                if (tooltip != null && tooltip.name.Contains("Tooltip"))
                {
                    DisableOrDestroy(tooltip.gameObject, "Tooltip");
                }
                else
                {
                    gazeTooltips.gameObject.SetActive(false);
                }
                Debug.Log("[TemplateCleanup] GazeTooltips devre dışı bırakıldı.");
            }

            // Also find and disable any worldspace tooltips
            var allObjects = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var t in allObjects)
            {
                if (t.name.Contains("Tooltip Worldspace") || t.name.Contains("Tap Tooltip"))
                {
                    DisableOrDestroy(t.gameObject, "Tooltip");
                }
            }
        }

        /// <summary>
        /// Disables the SpawnedObjectsManager which handles object spawning on surfaces.
        /// </summary>
        void DisableSpawnedObjectsManager()
        {
            var spawner = FindAnyObjectByType<SpawnedObjectsManager>(FindObjectsInactive.Include);
            if (spawner != null)
            {
                spawner.enabled = false;
                Debug.Log("[TemplateCleanup] SpawnedObjectsManager devre dışı bırakıldı.");
            }
        }

        /// <summary>
        /// Forces passthrough on immediately without waiting for the onboarding flow.
        /// </summary>
        void ForcePassthroughOn()
        {
            var featureController = FindAnyObjectByType<ARFeatureController>(FindObjectsInactive.Include);
            if (featureController != null)
            {
                featureController.TogglePassthrough(true);
                Debug.Log("[TemplateCleanup] Passthrough zorla açıldı.");
            }
            else
            {
                Debug.LogWarning("[TemplateCleanup] ARFeatureController bulunamadı! Passthrough elle açılmalı.");
            }
        }

        /// <summary>
        /// Disables or destroys a GameObject based on the m_DestroyInsteadOfDisable setting.
        /// </summary>
        void DisableOrDestroy(GameObject obj, string label)
        {
            if (obj == null) return;

            if (m_DestroyInsteadOfDisable)
            {
                Destroy(obj);
                Debug.Log($"[TemplateCleanup] {label} yok edildi: {obj.name}");
            }
            else
            {
                obj.SetActive(false);
                Debug.Log($"[TemplateCleanup] {label} devre dışı bırakıldı: {obj.name}");
            }
        }
    }
}
