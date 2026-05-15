using UnityEngine;

namespace AlcoholAwareness
{
    /// <summary>
    /// Main setup script for the Alcohol Awareness MR application.
    /// Creates the UI hierarchy at runtime and wires everything together.
    /// 
    /// Architecture:
    ///   - Hand Canvas (240x300) → attached to palm via HandMenuController
    ///     └─ ScenarioMenuUI → 4 scenario buttons
    ///   - Info Canvas (520x360) → spawns in front of camera (separate from hand)
    ///     └─ ScenarioInfoUI → scenario details + Başla/Geri buttons
    /// 
    /// Setup in Unity Editor:
    /// 1. Create empty GameObject "AlcoholAwarenessManager"
    /// 2. Attach this script
    /// 3. Assign 4 ScenarioData ScriptableObjects
    /// </summary>
    public class AlcoholAwarenessSetup : MonoBehaviour
    {
        [Header("Senaryo Verileri")]
        [Tooltip("4 adet senaryo verisi (ScriptableObject)")]
        [SerializeField] ScenarioData[] m_Scenarios = new ScenarioData[4];

        [Header("El Menüsü Canvas")]
        [Tooltip("El menüsü canvas boyutu (px). 240x300 önerilir.")]
        [SerializeField] Vector2 m_HandCanvasSize = new Vector2(240f, 300f);

        // Runtime references
        Canvas m_HandCanvas;
        HandMenuController m_HandMenuController;
        ScenarioMenuUI m_MenuUI;
        ScenarioInfoUI m_InfoUI;

        void Start()
        {
            // Template cleanup
            if (GetComponent<TemplateCleanup>() == null)
                gameObject.AddComponent<TemplateCleanup>();

            ValidateScenarios();
            CreateUI();
            Debug.Log("[AlcoholAwareness] Kurulum tamamlandı. Sol avuç içinizi kendinize çevirin.");
        }

        void ValidateScenarios()
        {
            if (m_Scenarios == null || m_Scenarios.Length == 0)
            {
                Debug.LogError("[AlcoholAwareness] HATA: Senaryo verileri atanmamış!");
                return;
            }

            for (int i = 0; i < m_Scenarios.Length; i++)
            {
                if (m_Scenarios[i] == null)
                    Debug.LogWarning($"[AlcoholAwareness] Senaryo [{i}] boş.");
            }
        }

        void CreateUI()
        {
            // ── 0. Scenario Manager ──
            gameObject.AddComponent<ScenarioManager>();

            // ── 1. Hand Menu Controller ──
            m_HandMenuController = gameObject.AddComponent<HandMenuController>();

            // ── 2. Hand Canvas (small, on palm) ──
            m_HandCanvas = UIFactory.CreateWorldSpaceCanvas(
                "HandMenu_Canvas", transform,
                m_HandCanvasSize, 0.001f);

            // Park offscreen initially; HandMenuController moves it
            m_HandCanvas.transform.localPosition = new Vector3(0f, -10f, 0f);

            m_HandMenuController.SetCanvasTransform(m_HandCanvas.transform);

            // ── 3. Menu UI (child of hand canvas) ──
            var menuObj = new GameObject("ScenarioMenuUI");
            menuObj.transform.SetParent(transform, false);
            m_MenuUI = menuObj.AddComponent<ScenarioMenuUI>();

            // ── 4. Info UI (creates its OWN canvas, in front of camera) ──
            var infoObj = new GameObject("ScenarioInfoUI");
            infoObj.transform.SetParent(transform, false);
            m_InfoUI = infoObj.AddComponent<ScenarioInfoUI>();

            // ── 5. Wire everything ──
            m_InfoUI.Initialize(m_MenuUI, m_HandMenuController);
            m_MenuUI.Initialize(m_HandCanvas.transform, m_Scenarios, m_InfoUI, m_HandMenuController);

            m_HandMenuController.SetUIReferences(m_MenuUI, m_InfoUI);

            // Start with menu hidden (will show when palm faces user)
            m_MenuUI.SetVisible(false);
        }
    }
}
