using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AddictionSim
{
    /// <summary>
    /// Unity Editor içinden Hand Menu Canvas prefab'ını otomatik olarak oluşturan
    /// yardımcı sınıf. Bu script sadece Editor'da çalışır.
    ///
    /// Kullanım: Unity menüsünden:
    /// GameObject > AddictionSim > Create Hand Menu Panel
    /// </summary>
    public static class HandMenuSetup
    {
#if UNITY_EDITOR
        [MenuItem("GameObject/AddictionSim/Create Hand Menu Panel", false, 10)]
        public static void CreateHandMenuPanel()
        {
            // === ANA CANVAS ===
            var canvasGO = new GameObject("HandMenuCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.dynamicPixelsPerUnit = 10f;

            canvasGO.AddComponent<GraphicRaycaster>();

            var canvasGroup = canvasGO.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;

            // Canvas boyutu (World Space - küçük panel)
            var canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(300, 400);
            canvasRect.localScale = new Vector3(0.0005f, 0.0005f, 0.0005f);

            // === PANEL ARKA PLAN ===
            var panelGO = new GameObject("Panel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            var panelRect = panelGO.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.05f, 0.05f, 0.12f, 0.85f);

            // === BAŞLIK ===
            var titleGO = CreateTextElement(panelGO.transform, "TitleText",
                "Bağımlılıkla Mücadele MR",
                24, FontStyles.Bold, TextAlignmentOptions.Center,
                new Color(1f, 1f, 1f, 0.95f));
            var titleRect = titleGO.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.85f);
            titleRect.anchorMax = new Vector2(1, 0.97f);
            titleRect.offsetMin = new Vector2(15, 0);
            titleRect.offsetMax = new Vector2(-15, 0);

            // === AYIRICI ÇİZGİ ===
            var separatorGO = new GameObject("Separator");
            separatorGO.transform.SetParent(panelGO.transform, false);
            var sepRect = separatorGO.AddComponent<RectTransform>();
            sepRect.anchorMin = new Vector2(0.1f, 0.83f);
            sepRect.anchorMax = new Vector2(0.9f, 0.835f);
            sepRect.offsetMin = Vector2.zero;
            sepRect.offsetMax = Vector2.zero;
            var sepImage = separatorGO.AddComponent<Image>();
            sepImage.color = new Color(0.3f, 0.6f, 0.9f, 0.4f);

            // === DURUM METNİ ===
            var statusGO = CreateTextElement(panelGO.transform, "StatusText",
                "Hazır",
                18, FontStyles.Normal, TextAlignmentOptions.Center,
                new Color(0.4f, 0.9f, 0.5f, 1f));
            var statusRect = statusGO.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0.72f);
            statusRect.anchorMax = new Vector2(1, 0.82f);
            statusRect.offsetMin = new Vector2(15, 0);
            statusRect.offsetMax = new Vector2(-15, 0);

            // === BAGIMLILK TİPİ METNİ ===
            var addictionTextGO = CreateTextElement(panelGO.transform, "AddictionTypeText",
                "",
                14, FontStyles.Italic, TextAlignmentOptions.Center,
                new Color(1f, 0.8f, 0.3f, 0.9f));
            var addictionRect = addictionTextGO.GetComponent<RectTransform>();
            addictionRect.anchorMin = new Vector2(0, 0.64f);
            addictionRect.anchorMax = new Vector2(1, 0.72f);
            addictionRect.offsetMin = new Vector2(15, 0);
            addictionRect.offsetMax = new Vector2(-15, 0);

            // === BUTONLAR CONTAINER ===
            var buttonsContainer = new GameObject("ButtonsContainer");
            buttonsContainer.transform.SetParent(panelGO.transform, false);
            var btnContainerRect = buttonsContainer.AddComponent<RectTransform>();
            btnContainerRect.anchorMin = new Vector2(0.05f, 0.28f);
            btnContainerRect.anchorMax = new Vector2(0.95f, 0.62f);
            btnContainerRect.offsetMin = Vector2.zero;
            btnContainerRect.offsetMax = Vector2.zero;

            var hlg = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(5, 5, 5, 5);

            // Sigara butonu
            var cigaretteBtn = CreateScenarioButton(buttonsContainer.transform,
                "CigaretteButton", "Sigara",
                new Color(0.9f, 0.4f, 0.3f, 0.15f),
                "icon_cigarette");

            // Alkol butonu
            var alcoholBtn = CreateScenarioButton(buttonsContainer.transform,
                "AlcoholButton", "Alkol",
                new Color(0.3f, 0.5f, 0.9f, 0.15f),
                "icon_alcohol");

            // Uyuşturucu butonu
            var drugBtn = CreateScenarioButton(buttonsContainer.transform,
                "DrugButton", "Uyuşturucu",
                new Color(0.6f, 0.3f, 0.8f, 0.15f),
                "icon_drugs");

            // === DURDURMA BUTONU ===
            var stopBtnGO = new GameObject("StopButton");
            stopBtnGO.transform.SetParent(panelGO.transform, false);
            var stopRect = stopBtnGO.AddComponent<RectTransform>();
            stopRect.anchorMin = new Vector2(0.15f, 0.06f);
            stopRect.anchorMax = new Vector2(0.85f, 0.22f);
            stopRect.offsetMin = Vector2.zero;
            stopRect.offsetMax = Vector2.zero;

            var stopBtnImage = stopBtnGO.AddComponent<Image>();
            stopBtnImage.color = new Color(0.8f, 0.15f, 0.15f, 0.35f);

            var stopButton = stopBtnGO.AddComponent<Button>();
            var stopColors = stopButton.colors;
            stopColors.normalColor = new Color(0.8f, 0.15f, 0.15f, 0.35f);
            stopColors.highlightedColor = new Color(0.9f, 0.2f, 0.2f, 0.6f);
            stopColors.pressedColor = new Color(1f, 0.3f, 0.3f, 0.8f);
            stopButton.colors = stopColors;

            // Stop buton içeriği (icon + text) - horizontal layout
            var stopContentLayout = stopBtnGO.AddComponent<HorizontalLayoutGroup>();
            stopContentLayout.spacing = 8;
            stopContentLayout.childAlignment = TextAnchor.MiddleCenter;
            stopContentLayout.childControlWidth = false;
            stopContentLayout.childControlHeight = false;
            stopContentLayout.childForceExpandWidth = false;
            stopContentLayout.childForceExpandHeight = false;
            stopContentLayout.padding = new RectOffset(10, 10, 5, 5);

            // Stop icon placeholder
            var stopIconGO = new GameObject("StopIcon");
            stopIconGO.transform.SetParent(stopBtnGO.transform, false);
            var stopIconRect = stopIconGO.AddComponent<RectTransform>();
            stopIconRect.sizeDelta = new Vector2(30, 30);
            var stopIconImage = stopIconGO.AddComponent<Image>();
            stopIconImage.color = new Color(1f, 0.3f, 0.3f, 0.9f);

            // Stop buton metni
            var stopTextGO = CreateTextElement(stopBtnGO.transform, "StopText",
                "Durdur",
                18, FontStyles.Bold, TextAlignmentOptions.Center,
                new Color(1f, 0.85f, 0.85f, 1f));
            var stopTextRect = stopTextGO.GetComponent<RectTransform>();
            stopTextRect.sizeDelta = new Vector2(120, 40);

            // === SCRIPT'LERİ EKLE ===
            // HandMenuController
            var controllerGO = new GameObject("HandMenuController");
            var controller = controllerGO.AddComponent<HandMenuController>();

            // SimulationManager
            var simManagerGO = new GameObject("SimulationManager");
            simManagerGO.AddComponent<SimulationManager>();

            // HandMenuUI
            var handMenuUIComp = canvasGO.AddComponent<HandMenuUI>();

            // Seçimi canvas yap
            Selection.activeGameObject = canvasGO;

            Debug.Log("[AddictionSim] Hand Menu Panel oluşturuldu.\n" +
                      "Şimdi: GameObject > AddictionSim > Create Info Panel komutu ile bilgilendirme panelini de oluşturun.\n" +
                      "Inspector'dan referansları bağlamayı unutmayın!");
        }

        [MenuItem("GameObject/AddictionSim/Create Info Panel", false, 11)]
        public static void CreateInfoPanel()
        {
            // === BİLGİLENDİRME PANELİ CANVAS ===
            var infoCanvasGO = new GameObject("InfoPanelCanvas");
            var infoCanvas = infoCanvasGO.AddComponent<Canvas>();
            infoCanvas.renderMode = RenderMode.WorldSpace;

            var infoScaler = infoCanvasGO.AddComponent<CanvasScaler>();
            infoScaler.dynamicPixelsPerUnit = 10f;

            infoCanvasGO.AddComponent<GraphicRaycaster>();

            var infoCanvasGroup = infoCanvasGO.AddComponent<CanvasGroup>();
            infoCanvasGroup.alpha = 0f;

            // Canvas boyutu - daha geniş panel
            var infoCanvasRect = infoCanvasGO.GetComponent<RectTransform>();
            infoCanvasRect.sizeDelta = new Vector2(500, 600);
            infoCanvasRect.localScale = new Vector3(0.001f, 0.001f, 0.001f);

            // === ANA PANEL ARKA PLAN ===
            var infoPanelGO = new GameObject("Panel");
            infoPanelGO.transform.SetParent(infoCanvasGO.transform, false);
            var infoPanelRect = infoPanelGO.AddComponent<RectTransform>();
            infoPanelRect.anchorMin = Vector2.zero;
            infoPanelRect.anchorMax = Vector2.one;
            infoPanelRect.offsetMin = Vector2.zero;
            infoPanelRect.offsetMax = Vector2.zero;

            var infoPanelImage = infoPanelGO.AddComponent<Image>();
            infoPanelImage.color = new Color(0.04f, 0.04f, 0.1f, 0.92f);

            // === ÜST BÖLÜM: İKON + BAŞLIK ===
            var headerGO = new GameObject("Header");
            headerGO.transform.SetParent(infoPanelGO.transform, false);
            var headerRect = headerGO.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.82f);
            headerRect.anchorMax = new Vector2(1, 0.97f);
            headerRect.offsetMin = new Vector2(20, 0);
            headerRect.offsetMax = new Vector2(-20, -8);

            var headerLayout = headerGO.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 15;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = false;
            headerLayout.childControlHeight = false;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;

            // İkon
            var infoIconGO = new GameObject("Icon");
            infoIconGO.transform.SetParent(headerGO.transform, false);
            var infoIconRect = infoIconGO.AddComponent<RectTransform>();
            infoIconRect.sizeDelta = new Vector2(60, 60);
            var infoIconImage = infoIconGO.AddComponent<Image>();
            infoIconImage.color = Color.white;

            // Başlık
            var infoTitleGO = CreateTextElement(headerGO.transform, "TitleText",
                "Bağımlılık Bilgisi",
                28, FontStyles.Bold, TextAlignmentOptions.Left,
                new Color(1f, 1f, 1f, 0.95f));
            var infoTitleRect = infoTitleGO.GetComponent<RectTransform>();
            infoTitleRect.sizeDelta = new Vector2(350, 60);

            // === AYIRICI ÇİZGİ ===
            var infoSepGO = new GameObject("Separator");
            infoSepGO.transform.SetParent(infoPanelGO.transform, false);
            var infoSepRect = infoSepGO.AddComponent<RectTransform>();
            infoSepRect.anchorMin = new Vector2(0.05f, 0.80f);
            infoSepRect.anchorMax = new Vector2(0.95f, 0.805f);
            infoSepRect.offsetMin = Vector2.zero;
            infoSepRect.offsetMax = Vector2.zero;
            var infoSepImage = infoSepGO.AddComponent<Image>();
            infoSepImage.color = new Color(0.3f, 0.6f, 0.9f, 0.3f);

            // === AÇIKLAMA METNİ ===
            var descGO = CreateTextElement(infoPanelGO.transform, "DescriptionText",
                "Bağımlılık açıklaması burada görünecek.",
                16, FontStyles.Normal, TextAlignmentOptions.TopLeft,
                new Color(0.85f, 0.85f, 0.9f, 0.9f));
            var descRect = descGO.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0, 0.35f);
            descRect.anchorMax = new Vector2(1, 0.79f);
            descRect.offsetMin = new Vector2(25, 0);
            descRect.offsetMax = new Vector2(-25, 0);

            // === UYARI METNİ ===
            var warningGO = CreateTextElement(infoPanelGO.transform, "WarningText",
                "⚠ Uyarı metni burada görünecek.",
                14, FontStyles.Bold, TextAlignmentOptions.Center,
                new Color(1f, 0.75f, 0.2f, 0.95f));
            var warningRect = warningGO.GetComponent<RectTransform>();
            warningRect.anchorMin = new Vector2(0, 0.22f);
            warningRect.anchorMax = new Vector2(1, 0.33f);
            warningRect.offsetMin = new Vector2(20, 0);
            warningRect.offsetMax = new Vector2(-20, 0);

            // === UYARI ARKA PLAN ===
            var warningBgGO = new GameObject("WarningBg");
            warningBgGO.transform.SetParent(infoPanelGO.transform, false);
            warningBgGO.transform.SetSiblingIndex(warningGO.transform.GetSiblingIndex());
            var warningBgRect = warningBgGO.AddComponent<RectTransform>();
            warningBgRect.anchorMin = new Vector2(0.03f, 0.22f);
            warningBgRect.anchorMax = new Vector2(0.97f, 0.34f);
            warningBgRect.offsetMin = Vector2.zero;
            warningBgRect.offsetMax = Vector2.zero;
            var warningBgImage = warningBgGO.AddComponent<Image>();
            warningBgImage.color = new Color(1f, 0.7f, 0f, 0.08f);

            // === BUTONLAR ALT BÖLÜM ===
            var btnContainerGO = new GameObject("ButtonsContainer");
            btnContainerGO.transform.SetParent(infoPanelGO.transform, false);
            var btnContRect = btnContainerGO.AddComponent<RectTransform>();
            btnContRect.anchorMin = new Vector2(0.05f, 0.04f);
            btnContRect.anchorMax = new Vector2(0.95f, 0.18f);
            btnContRect.offsetMin = Vector2.zero;
            btnContRect.offsetMax = Vector2.zero;

            var btnHlg = btnContainerGO.AddComponent<HorizontalLayoutGroup>();
            btnHlg.spacing = 20;
            btnHlg.childAlignment = TextAnchor.MiddleCenter;
            btnHlg.childControlWidth = true;
            btnHlg.childControlHeight = true;
            btnHlg.childForceExpandWidth = true;
            btnHlg.childForceExpandHeight = true;
            btnHlg.padding = new RectOffset(10, 10, 5, 5);

            // Başlat butonu
            var startBtnGO = new GameObject("StartButton");
            startBtnGO.transform.SetParent(btnContainerGO.transform, false);
            var startBtnImage = startBtnGO.AddComponent<Image>();
            startBtnImage.color = new Color(0.15f, 0.7f, 0.35f, 0.5f);
            var startBtn = startBtnGO.AddComponent<Button>();
            var startColors = startBtn.colors;
            startColors.normalColor = new Color(0.15f, 0.7f, 0.35f, 0.5f);
            startColors.highlightedColor = new Color(0.2f, 0.85f, 0.4f, 0.7f);
            startColors.pressedColor = new Color(0.25f, 1f, 0.5f, 0.9f);
            startBtn.colors = startColors;

            var startTextGO = CreateTextElement(startBtnGO.transform, "Text",
                "▶ Simülasyonu Başlat",
                18, FontStyles.Bold, TextAlignmentOptions.Center,
                new Color(1f, 1f, 1f, 0.95f));

            // Kapat butonu
            var closeBtnGO = new GameObject("CloseButton");
            closeBtnGO.transform.SetParent(btnContainerGO.transform, false);
            var closeBtnImage = closeBtnGO.AddComponent<Image>();
            closeBtnImage.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            var closeBtn = closeBtnGO.AddComponent<Button>();
            var closeColors = closeBtn.colors;
            closeColors.normalColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            closeColors.highlightedColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
            closeColors.pressedColor = new Color(0.7f, 0.7f, 0.7f, 0.7f);
            closeBtn.colors = closeColors;

            var closeTextGO = CreateTextElement(closeBtnGO.transform, "Text",
                "✕ Kapat",
                18, FontStyles.Normal, TextAlignmentOptions.Center,
                new Color(1f, 1f, 1f, 0.8f));

            // === KAPATMA BUTONU (SAĞ ÜST KÖŞE) ===
            var cornerCloseGO = new GameObject("CornerCloseButton");
            cornerCloseGO.transform.SetParent(infoPanelGO.transform, false);
            var cornerCloseRect = cornerCloseGO.AddComponent<RectTransform>();
            cornerCloseRect.anchorMin = new Vector2(0.9f, 0.93f);
            cornerCloseRect.anchorMax = new Vector2(0.98f, 0.99f);
            cornerCloseRect.offsetMin = Vector2.zero;
            cornerCloseRect.offsetMax = Vector2.zero;
            var cornerCloseImage = cornerCloseGO.AddComponent<Image>();
            cornerCloseImage.color = new Color(1f, 1f, 1f, 0.05f);
            var cornerCloseBtn = cornerCloseGO.AddComponent<Button>();

            var cornerCloseTextGO = CreateTextElement(cornerCloseGO.transform, "X",
                "✕", 20, FontStyles.Bold, TextAlignmentOptions.Center,
                new Color(1f, 1f, 1f, 0.6f));

            // === InfoPanelController SCRIPT ===
            var infoPanelController = infoCanvasGO.AddComponent<InfoPanelController>();

            Selection.activeGameObject = infoCanvasGO;

            Debug.Log("[AddictionSim] Bilgilendirme Paneli oluşturuldu.\n" +
                      "Inspector'dan şu referansları bağlayın:\n" +
                      "1. Info Panel Canvas → InfoPanelCanvas kendisi\n" +
                      "2. Title Text → TitleText\n" +
                      "3. Description Text → DescriptionText\n" +
                      "4. Warning Text → WarningText\n" +
                      "5. Icon Image → Icon\n" +
                      "6. Start Button → StartButton\n" +
                      "7. Close Button → CloseButton (ve CornerCloseButton)\n" +
                      "8. İkon sprite'larını atayın");
        }

        private static GameObject CreateScenarioButton(Transform parent, string name, string label,
            Color bgColor, string iconName)
        {
            var btnGO = new GameObject(name);
            btnGO.transform.SetParent(parent, false);

            var btnImage = btnGO.AddComponent<Image>();
            btnImage.color = bgColor;

            var button = btnGO.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = new Color(bgColor.r + 0.1f, bgColor.g + 0.1f, bgColor.b + 0.1f, 0.4f);
            colors.pressedColor = new Color(bgColor.r + 0.2f, bgColor.g + 0.2f, bgColor.b + 0.2f, 0.6f);
            button.colors = colors;

            // Vertical layout for icon + text
            var vlg = btnGO.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(5, 5, 10, 5);

            // Icon (placeholder - sprite Inspector'dan atanacak)
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(btnGO.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(50, 50);
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.color = Color.white;
            // Sprite Inspector'dan atanacak

            // Label
            var labelGO = CreateTextElement(btnGO.transform, "Label", label,
                12, FontStyles.Normal, TextAlignmentOptions.Center,
                new Color(1f, 1f, 1f, 0.85f));
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(70, 25);

            return btnGO;
        }

        private static GameObject CreateTextElement(Transform parent, string name, string text,
            float fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.enableAutoSizing = false;
            tmp.overflowMode = TextOverflowModes.Overflow;

            return go;
        }
#endif
    }
}
