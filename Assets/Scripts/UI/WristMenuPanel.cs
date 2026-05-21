using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AlcoholSimVR.Utilities;

namespace AlcoholSimVR.UI
{
    /// <summary>
    /// Sol bilek menüsü — avuç kameraya dönükken görünür, modül listesi sunar.
    /// </summary>
    public class WristMenuPanel : MonoBehaviour
    {
        /// <summary>"Düz Tahta Yürüme" seçildiğinde.</summary>
        public event Action OnStraightBeamWalkSelected;

        [Header("UI")]
        [SerializeField] private CanvasFadeAnimator _fadeAnimator;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private MRUIButton _straightBeamButton;
        [SerializeField] private GameObject[] _lockedOverlays;

        [Header("Billboard")]
        [SerializeField] private WorldSpaceBillboard _billboard;

        [Header("Boyut")]
        [SerializeField] private Vector2 _canvasSizeMeters = new Vector2(0.26f, 0.17f);

        private bool _visible;

        private void Awake()
        {
            ResolveReferences();
            if (_straightBeamButton != null)
            {
                _straightBeamButton.OnClicked += HandleStraightBeamClicked;
            }

            ApplyCanvasSize();
            ApplyVisualStyle();
            SetVisible(false, immediate: true);
        }

        private void OnDestroy()
        {
            if (_straightBeamButton != null)
            {
                _straightBeamButton.OnClicked -= HandleStraightBeamClicked;
            }
        }

        /// <summary>Menü görünürlüğünü ayarlar.</summary>
        public void SetVisible(bool visible, bool immediate = false)
        {
            _visible = visible;

            if (_fadeAnimator != null)
            {
                _fadeAnimator.FadeTo(visible ? 1f : 0f, immediate);
            }
            else if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1f : 0f;
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
            }

            gameObject.SetActive(true);
        }

        private void HandleStraightBeamClicked()
        {
            OnStraightBeamWalkSelected?.Invoke();
        }

        private void ApplyCanvasSize()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            canvas.sortingOrder = 50;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.dynamicPixelsPerUnit = 20f;
            }

            var rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = _canvasSizeMeters * 1000f;
            }
        }

        private void ResolveReferences()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_fadeAnimator == null)
            {
                _fadeAnimator = GetComponent<CanvasFadeAnimator>();
            }

            if (_billboard == null)
            {
                _billboard = GetComponent<WorldSpaceBillboard>();
            }

            var canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera == null)
            {
                var rig = FindAnyObjectByType<OVRCameraRig>();
                if (rig != null && rig.centerEyeAnchor != null)
                {
                    canvas.worldCamera = rig.centerEyeAnchor.GetComponent<Camera>();
                }
            }
        }

        private void ApplyVisualStyle()
        {
            var rect = GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            foreach (var image in GetComponentsInChildren<Image>(true))
            {
                bool isButton = image.GetComponent<MRUIButton>() != null;
                image.raycastTarget = isButton;
                if (!isButton)
                {
                    image.color = new Color(0.02f, 0.03f, 0.04f, 0.82f);
                    var outline = EnsureComponent<Outline>(image.gameObject);
                    outline.effectColor = new Color(0.2f, 0.85f, 1f, 0.42f);
                    outline.effectDistance = new Vector2(2f, -2f);
                }
                else
                {
                    var outline = EnsureComponent<Outline>(image.gameObject);
                    outline.effectColor = new Color(0.85f, 0.98f, 1f, 0.5f);
                    outline.effectDistance = new Vector2(2f, -2f);
                }
            }

            EnsureTitle(rect);

            if (_straightBeamButton != null)
            {
                var buttonRect = _straightBeamButton.GetComponent<RectTransform>();
                if (buttonRect != null)
                {
                    buttonRect.sizeDelta = new Vector2(220f, 58f);
                    buttonRect.anchoredPosition = new Vector2(0f, -28f);
                }

                var label = _straightBeamButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null)
                {
                    label.text = "DUZ TAHTA";
                    label.fontSize = 24f;
                    label.fontStyle = FontStyles.Bold;
                    label.enableAutoSizing = true;
                    label.fontSizeMin = 16f;
                    label.fontSizeMax = 24f;
                    label.color = Color.white;
                    var shadow = EnsureComponent<Shadow>(label.gameObject);
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
                    shadow.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }
        }

        private static T EnsureComponent<T>(GameObject target) where T : Component
        {
            return target.GetComponent<T>() ?? target.AddComponent<T>();
        }

        private void EnsureTitle(RectTransform root)
        {
            Transform existing = transform.Find("Title");
            TextMeshProUGUI title = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
            if (title == null)
            {
                var titleGo = new GameObject("Title");
                titleGo.transform.SetParent(transform, false);
                title = titleGo.AddComponent<TextMeshProUGUI>();
            }

            title.text = "DENGE MODU";
            title.fontSize = 22f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = new Color(0.85f, 0.98f, 1f, 1f);
            title.raycastTarget = false;

            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(root.sizeDelta.x - 40f, 38f);
            titleRect.anchoredPosition = new Vector2(0f, 45f);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_lockedOverlays != null)
            {
                foreach (var overlay in _lockedOverlays)
                {
                    if (overlay != null)
                    {
                        overlay.SetActive(true);
                    }
                }
            }
        }
#endif
    }
}
