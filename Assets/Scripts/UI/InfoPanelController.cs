using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AlcoholSimVR.Utilities;
using AlcoholSimVR.Simulation;

namespace AlcoholSimVR.UI
{
    /// <summary>
    /// Bilgi paneli — simülasyon öncesi Türkçe talimatlar ve Başlat düğmesi.
    /// </summary>
    public class InfoPanelController : MonoBehaviour
    {
        /// <summary>Başlat düğmesine basıldığında.</summary>
        public event Action OnStartPressed;
        public event Action<AlcoholEffectLevel> OnEffectLevelSelected;

        [Header("UI")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private MRUIButton _startButton;
        [SerializeField] private TextMeshProUGUI _effectLevelText;
        [SerializeField] private MRUIButton _lowEffectButton;
        [SerializeField] private MRUIButton _mediumEffectButton;
        [SerializeField] private MRUIButton _highEffectButton;
        [SerializeField] private WorldSpaceBillboard _billboard;

        [Header("İçerik (Türkçe)")]
        [SerializeField] private string _title = "Düz Tahta Yürüme Testi Hakkında";
        [TextArea(6, 12)]
        [SerializeField] private string _body =
            "Bu simülasyon, alkol etkisinin denge ve koordinasyon üzerindeki etkilerini " +
            "deneyimlemenizi sağlar. Yerde belirecek dar tahta üzerinde ileri doğru yürümeye " +
            "çalışın. Simülasyon süresince görsel bozulmalar alkol etkisini temsil edecektir. " +
            "Düşme riskiniz yoktur — gerçek ortamınızı görmeye devam edersiniz. " +
            "Hazır olduğunuzda 'Başlat' butonuna basın.";

        [Header("Spawn")]
        [SerializeField] private float _spawnDistance = 1.15f;
        [SerializeField] private float _verticalOffset = -0.04f;
        [SerializeField] private Vector2 _panelSizeMeters = new Vector2(0.42f, 0.34f);
        [SerializeField] private float _worldScale = 0.001f;

        [Header("Animasyon")]
        [SerializeField] private float _appearDuration = 0.35f;
        [SerializeField] private float _scaleFrom = 0.8f;
        [SerializeField] private float _scaleTo = 1.0f;

        private Coroutine _animRoutine;
        private AlcoholEffectLevel _selectedEffectLevel = AlcoholEffectLevel.Medium;

        private void Awake()
        {
            ResolveReferences();
            EnsureLevelControls();
            ApplyPanelStyle();

            if (_titleText != null)
            {
                _titleText.text = _title;
            }

            if (_bodyText != null)
            {
                _bodyText.text = _body;
            }

            if (_startButton != null)
            {
                _startButton.OnClicked += HandleStartClicked;
            }

            RegisterLevelButtonHandlers();
            SetSelectedEffectLevel(_selectedEffectLevel);

            Hide(immediate: true);
        }

        private void OnDestroy()
        {
            if (_startButton != null)
            {
                _startButton.OnClicked -= HandleStartClicked;
            }

            UnregisterLevelButtonHandlers();
        }

        /// <summary>Paneli kameranın önünde gösterir.</summary>
        public void Show()
        {
            PositionInFrontOfCamera();
            gameObject.SetActive(true);

            if (_animRoutine != null)
            {
                StopCoroutine(_animRoutine);
            }

            _animRoutine = StartCoroutine(AppearRoutine());
        }

        /// <summary>Paneli gizler.</summary>
        public void Hide(bool immediate = false)
        {
            if (_animRoutine != null)
            {
                StopCoroutine(_animRoutine);
                _animRoutine = null;
            }

            if (_canvasGroup == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (immediate)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
                return;
            }

            StartCoroutine(HideRoutine());
        }

        public void SetSelectedEffectLevel(AlcoholEffectLevel level)
        {
            _selectedEffectLevel = level;
            UpdateLevelButtonVisuals();
        }

        private void PositionInFrontOfCamera()
        {
            Transform cam = ResolveCameraTransform();
            if (cam == null)
            {
                return;
            }

            transform.SetParent(null, true);
            if (_billboard != null)
            {
                _billboard.enabled = false;
            }

            Vector3 forward = cam.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = cam.forward;
            }

            Vector3 toPanel = forward.normalized;
            transform.position = cam.position + toPanel * _spawnDistance + Vector3.up * _verticalOffset;
            transform.rotation = Quaternion.LookRotation(toPanel, Vector3.up);
            transform.localScale = Vector3.one * _worldScale;

            if (_panelRoot != null)
            {
                _panelRoot.sizeDelta = _panelSizeMeters * 1000f;
            }
        }

        private Transform ResolveCameraTransform()
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null && rig.centerEyeAnchor != null)
            {
                return rig.centerEyeAnchor;
            }

            return Camera.main != null ? Camera.main.transform : null;
        }

        private void HandleStartClicked()
        {
            OnStartPressed?.Invoke();
        }

        private void HandleLowEffectClicked()
        {
            SelectEffectLevel(AlcoholEffectLevel.Low);
        }

        private void HandleMediumEffectClicked()
        {
            SelectEffectLevel(AlcoholEffectLevel.Medium);
        }

        private void HandleHighEffectClicked()
        {
            SelectEffectLevel(AlcoholEffectLevel.High);
        }

        private void SelectEffectLevel(AlcoholEffectLevel level)
        {
            SetSelectedEffectLevel(level);
            OnEffectLevelSelected?.Invoke(level);
        }

        private void RegisterLevelButtonHandlers()
        {
            if (_lowEffectButton != null)
            {
                _lowEffectButton.OnClicked += HandleLowEffectClicked;
            }

            if (_mediumEffectButton != null)
            {
                _mediumEffectButton.OnClicked += HandleMediumEffectClicked;
            }

            if (_highEffectButton != null)
            {
                _highEffectButton.OnClicked += HandleHighEffectClicked;
            }
        }

        private void UnregisterLevelButtonHandlers()
        {
            if (_lowEffectButton != null)
            {
                _lowEffectButton.OnClicked -= HandleLowEffectClicked;
            }

            if (_mediumEffectButton != null)
            {
                _mediumEffectButton.OnClicked -= HandleMediumEffectClicked;
            }

            if (_highEffectButton != null)
            {
                _highEffectButton.OnClicked -= HandleHighEffectClicked;
            }
        }

        private void ResolveReferences()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_panelRoot == null)
            {
                _panelRoot = GetComponent<RectTransform>();
            }

            if (_billboard == null)
            {
                _billboard = GetComponent<WorldSpaceBillboard>();
            }

            if (_billboard != null)
            {
                _billboard.enabled = false;
            }
        }

        private void EnsureLevelControls()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
            {
                return;
            }

            if (_effectLevelText == null)
            {
                Transform existing = transform.Find("EffectLevelTitle");
                _effectLevelText = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
                if (_effectLevelText == null)
                {
                    var titleGo = new GameObject("EffectLevelTitle");
                    titleGo.transform.SetParent(transform, false);
                    _effectLevelText = titleGo.AddComponent<TextMeshProUGUI>();
                }
            }

            _lowEffectButton = _lowEffectButton != null
                ? _lowEffectButton
                : EnsureLevelButton("LowEffect_Button", "DUSUK");
            _mediumEffectButton = _mediumEffectButton != null
                ? _mediumEffectButton
                : EnsureLevelButton("MediumEffect_Button", "ORTA");
            _highEffectButton = _highEffectButton != null
                ? _highEffectButton
                : EnsureLevelButton("HighEffect_Button", "YUKSEK");
        }

        private MRUIButton EnsureLevelButton(string objectName, string label)
        {
            Transform existing = transform.Find(objectName);
            GameObject buttonGo = existing != null ? existing.gameObject : new GameObject(objectName);
            buttonGo.transform.SetParent(transform, false);

            var rect = EnsureComponent<RectTransform>(buttonGo);
            rect.sizeDelta = new Vector2(104f, 34f);

            var image = EnsureComponent<Image>(buttonGo);
            image.raycastTarget = true;

            var button = EnsureComponent<MRUIButton>(buttonGo);
            TextMeshProUGUI text = buttonGo.GetComponentInChildren<TextMeshProUGUI>(true);
            if (text == null)
            {
                var textGo = new GameObject("Text");
                textGo.transform.SetParent(buttonGo.transform, false);
                text = textGo.AddComponent<TextMeshProUGUI>();
            }

            text.text = label;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;

            return button;
        }

        private void ApplyPanelStyle()
        {
            transform.localScale = Vector3.one * _worldScale;

            var canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 60;
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.dynamicPixelsPerUnit = 24f;
            }

            if (_panelRoot != null)
            {
                _panelRoot.sizeDelta = _panelSizeMeters * 1000f;
            }

            foreach (var image in GetComponentsInChildren<Image>(true))
            {
                bool isButton = image.GetComponent<MRUIButton>() != null;
                image.raycastTarget = isButton;
                if (!isButton)
                {
                    image.color = new Color(0.018f, 0.025f, 0.03f, 0.86f);
                    var outline = EnsureComponent<Outline>(image.gameObject);
                    outline.effectColor = new Color(0.2f, 0.85f, 1f, 0.34f);
                    outline.effectDistance = new Vector2(2f, -2f);
                }
            }

            if (_titleText != null)
            {
                _titleText.fontSize = 20f;
                _titleText.fontStyle = FontStyles.Bold;
                _titleText.alignment = TextAlignmentOptions.Center;
                _titleText.color = new Color(0.85f, 0.98f, 1f, 1f);
                _titleText.raycastTarget = false;
                _titleText.rectTransform.sizeDelta = new Vector2(370f, 36f);
                _titleText.rectTransform.anchoredPosition = new Vector2(0f, 122f);
            }

            if (_bodyText != null)
            {
                _bodyText.fontSize = 15f;
                _bodyText.enableAutoSizing = true;
                _bodyText.fontSizeMin = 11f;
                _bodyText.fontSizeMax = 15f;
                _bodyText.alignment = TextAlignmentOptions.TopLeft;
                _bodyText.color = new Color(0.92f, 0.97f, 1f, 0.95f);
                _bodyText.raycastTarget = false;
                _bodyText.rectTransform.sizeDelta = new Vector2(360f, 112f);
                _bodyText.rectTransform.anchoredPosition = new Vector2(0f, 46f);
            }

            if (_effectLevelText != null)
            {
                _effectLevelText.text = "ETKI SEVIYESI";
                _effectLevelText.fontSize = 13f;
                _effectLevelText.fontStyle = FontStyles.Bold;
                _effectLevelText.alignment = TextAlignmentOptions.Center;
                _effectLevelText.color = new Color(0.85f, 0.98f, 1f, 0.92f);
                _effectLevelText.raycastTarget = false;
                _effectLevelText.rectTransform.sizeDelta = new Vector2(360f, 24f);
                _effectLevelText.rectTransform.anchoredPosition = new Vector2(0f, -24f);
            }

            StyleLevelButton(_lowEffectButton, "DUSUK", new Vector2(-116f, -58f));
            StyleLevelButton(_mediumEffectButton, "ORTA", new Vector2(0f, -58f));
            StyleLevelButton(_highEffectButton, "YUKSEK", new Vector2(116f, -58f));

            UpdateLevelButtonVisuals();

            if (_startButton != null)
            {
                var buttonRect = _startButton.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.sizeDelta = new Vector2(170f, 42f);
                    buttonRect.anchoredPosition = new Vector2(0f, -126f);
                }

                var label = _startButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = "BASLAT";
                    label.fontSize = 20f;
                    label.fontStyle = FontStyles.Bold;
                    label.alignment = TextAlignmentOptions.Center;
                    label.color = Color.white;
                    label.raycastTarget = false;
                    label.rectTransform.sizeDelta = new Vector2(170f, 42f);
                    label.rectTransform.anchoredPosition = Vector2.zero;
                }
            }
        }

        private void StyleLevelButton(MRUIButton button, string labelText, Vector2 anchoredPosition)
        {
            if (button == null)
            {
                return;
            }

            var rect = button.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = new Vector2(104f, 34f);
                rect.anchoredPosition = anchoredPosition;
            }

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                var outline = EnsureComponent<Outline>(image.gameObject);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = labelText;
                label.fontSize = 14f;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.enableAutoSizing = true;
                label.fontSizeMin = 10f;
                label.fontSizeMax = 14f;
                label.raycastTarget = false;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = Vector2.zero;
                label.rectTransform.offsetMax = Vector2.zero;
            }
        }

        private void UpdateLevelButtonVisuals()
        {
            ApplyLevelButtonVisual(_lowEffectButton, _selectedEffectLevel == AlcoholEffectLevel.Low);
            ApplyLevelButtonVisual(_mediumEffectButton, _selectedEffectLevel == AlcoholEffectLevel.Medium);
            ApplyLevelButtonVisual(_highEffectButton, _selectedEffectLevel == AlcoholEffectLevel.High);
        }

        private static void ApplyLevelButtonVisual(MRUIButton button, bool selected)
        {
            if (button == null)
            {
                return;
            }

            Color normal = selected
                ? new Color(1f, 0.88f, 0.28f, 0.95f)
                : new Color(0.04f, 0.45f, 0.72f, 0.82f);
            Color hover = selected
                ? new Color(1f, 0.96f, 0.45f, 1f)
                : new Color(0f, 0.85f, 1f, 0.95f);
            Color pressed = new Color(1f, 0.94f, 0.45f, 0.95f);
            button.SetVisualColors(normal, hover, pressed);

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                var outline = image.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = selected
                        ? new Color(1f, 0.96f, 0.45f, 0.9f)
                        : new Color(0.85f, 0.98f, 1f, 0.35f);
                }
            }

            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null)
            {
                label.color = selected ? new Color(0.05f, 0.04f, 0.01f, 1f) : Color.white;
            }
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            return target.GetComponent<T>() ?? target.AddComponent<T>();
        }

        private IEnumerator AppearRoutine()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            if (_panelRoot != null)
            {
                _panelRoot.localScale = Vector3.one * (_worldScale * _scaleFrom);
            }

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _appearDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _canvasGroup.alpha = t;
                if (_panelRoot != null)
                {
                    float scale = Mathf.Lerp(_scaleFrom, _scaleTo, t);
                    _panelRoot.localScale = Vector3.one * (_worldScale * scale);
                }

                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            if (_panelRoot != null)
            {
                _panelRoot.localScale = Vector3.one * (_worldScale * _scaleTo);
            }

            _animRoutine = null;
        }

        private IEnumerator HideRoutine()
        {
            float elapsed = 0f;
            float duration = 0.2f;
            float startAlpha = _canvasGroup.alpha;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
    }
}
