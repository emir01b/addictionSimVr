using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AlcoholAwareness
{
    /// <summary>
    /// Compact hand-attached scenario selection menu.
    /// Displays 4 scenario buttons in a 2x2 grid.
    /// Panel size: 240x300 px (24cm x 30cm at 0.001 scale).
    ///
    /// Layout budget (vertical, spacing=4, padding=10):
    ///   Content area = 300 - 20 = 280px
    ///   Title:     24px
    ///   Subtitle:  22px
    ///   Separator:  1px
    ///   Grid:     200px (2 rows of 95px + 10px spacing)
    ///   Spacings:  4*3 = 12px
    ///   Total:    259px < 280px ✓
    /// </summary>
    public class ScenarioMenuUI : MonoBehaviour
    {
        // ── Dimensions ─────────────────────────────────────────────
        const float PanelW = 240f;
        const float PanelH = 340f;
        const float BtnSize = 95f;
        const float BtnSpacing = 10f;
        const float IconSize = 40f;

        ScenarioData[] m_Scenarios;
        ScenarioInfoUI m_InfoUI;
        HandMenuController m_HandMenu;

        RectTransform m_PanelRoot;
        CanvasGroup m_CanvasGroup;

        public RectTransform PanelRoot => m_PanelRoot;

        /// <summary>
        /// Initializes and builds the menu UI hierarchy.
        /// </summary>
        public void Initialize(Transform parent, ScenarioData[] scenarios,
                               ScenarioInfoUI infoUI, HandMenuController handMenu)
        {
            m_Scenarios = scenarios;
            m_InfoUI = infoUI;
            m_HandMenu = handMenu;
            BuildUI(parent);
        }

        void BuildUI(Transform parent)
        {
            // ── Root panel ──
            m_PanelRoot = UIFactory.CreatePanel("MenuPanel", parent,
                new Vector2(PanelW, PanelH), UIFactory.PanelBackground);

            m_CanvasGroup = m_PanelRoot.gameObject.AddComponent<CanvasGroup>();
            UIFactory.AddOutline(m_PanelRoot.gameObject, UIFactory.AccentCyan, new Vector2(1f, 1f));

            // Vertical layout — everything inside
            UIFactory.AddVerticalLayout(m_PanelRoot.gameObject,
                new RectOffset(10, 10, 10, 10), 4f, TextAnchor.UpperCenter);

            // ── Title ──
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(m_PanelRoot, false);
            titleObj.AddComponent<RectTransform>();
            UIFactory.AddLayoutElement(titleObj, preferredHeight: 24f);

            UIFactory.CreateText("TitleText", titleObj.transform,
                "Alkol Farkındalık",
                18f, UIFactory.AccentCyan, TextAlignmentOptions.Center, FontStyles.Bold);

            // ── Subtitle ──
            var subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(m_PanelRoot, false);
            subObj.AddComponent<RectTransform>();
            UIFactory.AddLayoutElement(subObj, preferredHeight: 22f);

            UIFactory.CreateText("SubText", subObj.transform,
                "Bir senaryo seçin",
                13f, UIFactory.TextSub, TextAlignmentOptions.Center);

            // ── Separator ──
            BuildSeparator(m_PanelRoot, PanelW - 40f);

            // ── Button grid ──
            BuildButtonGrid(m_PanelRoot);
        }

        void BuildSeparator(Transform parent, float width)
        {
            var obj = new GameObject("Sep");
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            UIFactory.AddLayoutElement(obj, preferredHeight: 1f, preferredWidth: width);
            var img = obj.AddComponent<Image>();
            img.color = UIFactory.AccentPurple;
            img.raycastTarget = false;
        }

        void BuildButtonGrid(Transform parent)
        {
            var gridObj = new GameObject("Grid");
            gridObj.transform.SetParent(parent, false);
            gridObj.AddComponent<RectTransform>();

            float gridH = BtnSize * 2f + BtnSpacing;
            UIFactory.AddLayoutElement(gridObj, preferredHeight: gridH, preferredWidth: PanelW - 20f);

            UIFactory.AddGridLayout(gridObj,
                new Vector2(BtnSize, BtnSize),
                new Vector2(BtnSpacing, BtnSpacing),
                new RectOffset(4, 4, 0, 0), 2);

            for (int i = 0; i < m_Scenarios.Length && i < 4; i++)
            {
                BuildButton(gridObj.transform, m_Scenarios[i]);
            }
        }

        void BuildButton(Transform parent, ScenarioData scenario)
        {
            var btn = UIFactory.CreateButton(
                $"Btn_{scenario.scenarioType}", parent,
                new Vector2(BtnSize, BtnSize), UIFactory.ButtonNormal,
                () => OnScenarioSelected(scenario));

            UIFactory.AddOutline(btn.gameObject,
                new Color(0f, 0.95f, 1f, 0.2f), new Vector2(0.5f, 0.5f));

            // Vertical layout inside button
            UIFactory.AddVerticalLayout(btn.gameObject,
                new RectOffset(6, 6, 12, 6), 4f, TextAnchor.MiddleCenter);

            // Icon
            var iconWrap = new GameObject("IconWrap");
            iconWrap.transform.SetParent(btn.transform, false);
            iconWrap.AddComponent<RectTransform>();
            UIFactory.AddLayoutElement(iconWrap, preferredHeight: IconSize, preferredWidth: IconSize);

            var icon = UIFactory.CreateIcon("Icon", iconWrap.transform,
                scenario.scenarioIcon, new Vector2(IconSize, IconSize));
            icon.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
            icon.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);

            // Label — 2 satıra kadar sığdır
            var labelWrap = new GameObject("LabelWrap");
            labelWrap.transform.SetParent(btn.transform, false);
            labelWrap.AddComponent<RectTransform>();
            UIFactory.AddLayoutElement(labelWrap, preferredHeight: 32f, preferredWidth: BtnSize - 12f);

            var tmp = UIFactory.CreateText("Label", labelWrap.transform,
                scenario.scenarioName,
                11f, UIFactory.TextMain, TextAlignmentOptions.Center, FontStyles.Bold);
            tmp.enableWordWrapping = true;
        }

        void OnScenarioSelected(ScenarioData scenario)
        {
            Debug.Log($"[AlcoholAwareness] Senaryo seçildi: {scenario.scenarioName}");
            SetVisible(false);

            // Tell hand controller that info panel is taking over
            if (m_HandMenu != null)
                m_HandMenu.SetInfoPanelActive(true);

            m_InfoUI.ShowScenario(scenario);
        }

        // ── Visibility ─────────────────────────────────────────────

        public void SetVisible(bool visible)
        {
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = visible ? 1f : 0f;
                m_CanvasGroup.interactable = visible;
                m_CanvasGroup.blocksRaycasts = visible;
            }
        }

        public bool IsVisible => m_CanvasGroup != null && m_CanvasGroup.alpha > 0.5f;
    }
}
