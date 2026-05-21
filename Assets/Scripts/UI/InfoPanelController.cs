using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AlcoholSimVR.Utilities;

namespace AlcoholSimVR.UI
{
    /// <summary>
    /// Bilgi paneli — simülasyon öncesi Türkçe talimatlar ve Başlat düğmesi.
    /// </summary>
    public class InfoPanelController : MonoBehaviour
    {
        /// <summary>Başlat düğmesine basıldığında.</summary>
        public event Action OnStartPressed;

        [Header("UI")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private MRUIButton _startButton;
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
        [SerializeField] private Vector2 _panelSizeMeters = new Vector2(0.42f, 0.28f);
        [SerializeField] private float _worldScale = 0.001f;

        [Header("Animasyon")]
        [SerializeField] private float _appearDuration = 0.35f;
        [SerializeField] private float _scaleFrom = 0.8f;
        [SerializeField] private float _scaleTo = 1.0f;

        private Coroutine _animRoutine;

        private void Awake()
        {
            ResolveReferences();
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

            Hide(immediate: true);
        }

        private void OnDestroy()
        {
            if (_startButton != null)
            {
                _startButton.OnClicked -= HandleStartClicked;
            }
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
                _titleText.rectTransform.anchoredPosition = new Vector2(0f, 94f);
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
                _bodyText.rectTransform.sizeDelta = new Vector2(360f, 132f);
                _bodyText.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            }

            if (_startButton != null)
            {
                var buttonRect = _startButton.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.sizeDelta = new Vector2(170f, 42f);
                    buttonRect.anchoredPosition = new Vector2(0f, -104f);
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
