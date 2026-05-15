using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AlcoholAwareness
{
    /// <summary>
    /// Scenario information panel that appears IN FRONT of the user (world space).
    /// Has its OWN canvas — NOT attached to the hand menu canvas.
    /// Spawns at 0.7m in front of the camera when a scenario is selected.
    /// 
    /// Now supports XR Grab interaction to be movable by the user.
    /// </summary>
    public class ScenarioInfoUI : MonoBehaviour
    {
        // ── Dimensions ─────────────────────────────────────────────
        const float PanelW = 540f;
        const float PanelH = 420f;
        const float InfoCanvasScale = 0.001f;
        const float SpawnDistance = 0.7f; // Was 1.2f, brought closer for better interaction

        Canvas m_InfoCanvas;
        RectTransform m_PanelRoot;
        CanvasGroup m_CanvasGroup;

        // References
        ScenarioMenuUI m_MenuUI;
        HandMenuController m_HandMenu;

        // Dynamic content
        Image m_ScenarioIcon;
        TextMeshProUGUI m_TitleText;
        TextMeshProUGUI m_PurposeText;
        TextMeshProUGUI m_ExpectationText;
        TextMeshProUGUI m_EffectText;
        ScenarioData m_CurrentScenario;

        /// <summary>
        /// Initializes and builds the info UI with its own canvas.
        /// </summary>
        public void Initialize(ScenarioMenuUI menuUI, HandMenuController handMenu)
        {
            m_MenuUI = menuUI;
            m_HandMenu = handMenu;
            BuildUI();
            SetVisible(false);
        }

        void BuildUI()
        {
            // ── Create dedicated canvas ──
            m_InfoCanvas = UIFactory.CreateWorldSpaceCanvas(
                "InfoPanel_Canvas", transform,
                new Vector2(PanelW, PanelH), InfoCanvasScale);

            // Start hidden
            m_InfoCanvas.transform.localScale = Vector3.zero;

            // ── Root panel fills canvas ──
            m_PanelRoot = UIFactory.CreatePanel("InfoPanel", m_InfoCanvas.transform,
                new Vector2(PanelW, PanelH), UIFactory.PanelBackground);

            m_CanvasGroup = m_PanelRoot.gameObject.AddComponent<CanvasGroup>();
            UIFactory.AddOutline(m_PanelRoot.gameObject, UIFactory.AccentCyan, new Vector2(1f, 1f));

            // ── Make the panel movable/grabbable in VR ──
            UIFactory.MakeMovable(m_PanelRoot.gameObject, new Vector2(PanelW, PanelH));

            UIFactory.AddVerticalLayout(m_PanelRoot.gameObject,
                new RectOffset(20, 20, 16, 16), 8f, TextAnchor.UpperCenter);

            // ── Header: icon + title (horizontal) ──
            BuildHeader(m_PanelRoot);

            // ── Separator ──
            BuildSeparator(m_PanelRoot);

            // ── Info sections ──
            BuildInfoSections(m_PanelRoot);

            // ── Button row ──
            BuildButtons(m_PanelRoot);
        }

        // ── Build Helpers ──────────────────────────────────────────

        void BuildHeader(Transform parent)
        {
            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(parent, false);
            headerObj.AddComponent<RectTransform>();
            UIFactory.AddLayoutElement(headerObj, preferredHeight: 50f);
            UIFactory.AddHorizontalLayout(headerObj,
                new RectOffset(0, 0, 4, 4), 12f, TextAnchor.MiddleLeft);

            // Icon
            var iconBtn = new GameObject("IconWrap");
            iconBtn.transform.SetParent(headerObj.transform, false);
            iconBtn.AddComponent<RectTransform>();
            UIFactory.AddLayoutElement(iconBtn, preferredHeight: 42f, preferredWidth: 42f);

            m_ScenarioIcon = UIFactory.CreateIcon("Icon", iconBtn.transform, null,
                new Vector2(42f, 42f));

            // Title
            var titleWrap = new GameObject("TitleWrap");
            titleWrap.transform.SetParent(headerObj.transform, false);
            titleWrap.AddComponent<RectTransform>();
            UIFactory.AddLayoutElement(titleWrap, preferredHeight: 42f, preferredWidth: PanelW - 100f);

            m_TitleText = UIFactory.CreateText("Title", titleWrap.transform,
                "", 24f, UIFactory.AccentCyan, TextAlignmentOptions.Left, FontStyles.Bold);
        }

        void BuildSeparator(Transform parent)
        {
            var obj = new GameObject("Sep");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            UIFactory.AddLayoutElement(obj, preferredHeight: 1f, preferredWidth: PanelW - 60f);
            var img = obj.AddComponent<Image>();
            img.color = UIFactory.AccentPurple;
            img.raycastTarget = false;
        }

        void BuildInfoSections(Transform parent)
        {
            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(parent, false);
            contentObj.AddComponent<RectTransform>();
            UIFactory.AddLayoutElement(contentObj, preferredHeight: 210f);
            UIFactory.AddVerticalLayout(contentObj,
                new RectOffset(0, 0, 0, 0), 6f, TextAnchor.UpperLeft);

            m_PurposeText     = BuildSection(contentObj.transform, "🎯  Amaç");
            m_ExpectationText = BuildSection(contentObj.transform, "📋  Ne Yapacaksınız");
            m_EffectText      = BuildSection(contentObj.transform, "🍺  Alkol Etkisi");
        }

        TextMeshProUGUI BuildSection(Transform parent, string label)
        {
            var section = new GameObject($"Sec_{label}");
            section.transform.SetParent(parent, false);
            section.AddComponent<RectTransform>();
            UIFactory.AddLayoutElement(section, preferredHeight: 70f);
            UIFactory.AddVerticalLayout(section,
                new RectOffset(0, 0, 0, 0), 4f, TextAnchor.UpperLeft);

            // Label
            var labelWrap = new GameObject("LabelWrap");
            labelWrap.transform.SetParent(section.transform, false);
            labelWrap.AddComponent<RectTransform>();
            UIFactory.AddLayoutElement(labelWrap, preferredHeight: 18f);

            UIFactory.CreateText("Lbl", labelWrap.transform,
                label, 14f, UIFactory.AccentCyan, TextAlignmentOptions.Left, FontStyles.Bold);

            // Content text
            var contentWrap = new GameObject("ContentWrap");
            contentWrap.transform.SetParent(section.transform, false);
            contentWrap.AddComponent<RectTransform>();
            UIFactory.AddLayoutElement(contentWrap, preferredHeight: 48f);

            var tmp = UIFactory.CreateText("Txt", contentWrap.transform,
                "", 13f, UIFactory.TextSub, TextAlignmentOptions.Center);
            tmp.enableWordWrapping = true;

            return tmp;
        }

        void BuildButtons(Transform parent)
        {
            var row = new GameObject("Buttons");
            row.transform.SetParent(parent, false);
            row.AddComponent<RectTransform>();
            UIFactory.AddLayoutElement(row, preferredHeight: 50f);
            UIFactory.AddHorizontalLayout(row,
                new RectOffset(40, 40, 0, 0), 20f, TextAnchor.MiddleCenter);

            // Back
            var backBtn = UIFactory.CreateButton("BackBtn", row.transform,
                new Vector2(160f, 44f), UIFactory.ButtonNormal, OnBackPressed);
            UIFactory.AddOutline(backBtn.gameObject, UIFactory.AccentCyan, new Vector2(0.5f, 0.5f));
            UIFactory.CreateText("BackLbl", backBtn.transform,
                "← Geri", 15f, UIFactory.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);

            // Start
            var startBtn = UIFactory.CreateButton("StartBtn", row.transform,
                new Vector2(160f, 44f), UIFactory.ButtonStart, OnStartPressed);
            UIFactory.CreateText("StartLbl", startBtn.transform,
                "Başla →", 15f, UIFactory.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        // ── Public API ─────────────────────────────────────────────

        public void ShowScenario(ScenarioData scenario)
        {
            if (scenario == null) return;

            m_ScenarioIcon.sprite = scenario.scenarioIcon;
            m_TitleText.text = scenario.scenarioName;
            m_PurposeText.text = scenario.purpose;
            m_ExpectationText.text = scenario.expectation;
            m_EffectText.text = scenario.alcoholEffect;
            m_CurrentScenario = scenario;

            PositionInFrontOfCamera();
            SetVisible(true);
        }

        void PositionInFrontOfCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var camT = cam.transform;
            Vector3 forward = camT.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 pos = camT.position + forward * SpawnDistance;
            pos.y = camT.position.y;

            m_InfoCanvas.transform.position = pos;
            
            // Fix: Face same direction as camera so user sees the front of the UI
            m_InfoCanvas.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            
            m_InfoCanvas.transform.localScale = Vector3.one * InfoCanvasScale;
        }

        public void SetVisible(bool visible)
        {
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = visible ? 1f : 0f;
                m_CanvasGroup.interactable = visible;
                m_CanvasGroup.blocksRaycasts = visible;
            }

            if (m_InfoCanvas != null)
            {
                m_InfoCanvas.transform.localScale = visible
                    ? Vector3.one * InfoCanvasScale
                    : Vector3.zero;
            }
        }

        public bool IsVisible => m_CanvasGroup != null && m_CanvasGroup.alpha > 0.5f;

        void OnBackPressed()
        {
            SetVisible(false);
            if (m_HandMenu != null)
                m_HandMenu.SetInfoPanelActive(false);
        }

        void OnStartPressed()
        {
            Debug.Log($"[AlcoholAwareness] Starting Scenario: {m_CurrentScenario?.scenarioName}");
            
            if (ScenarioManager.Instance == null)
            {
                Debug.LogError("[AlcoholAwareness] HATA: ScenarioManager.Instance bulunamadı!");
                return;
            }

            if (m_CurrentScenario != null)
            {
                SetVisible(false);
                ScenarioManager.Instance.StartScenario(m_CurrentScenario);
            }
        }
    }
}
