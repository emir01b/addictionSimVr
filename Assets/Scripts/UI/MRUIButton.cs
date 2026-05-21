using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using AlcoholSimVR.Utilities;

namespace AlcoholSimVR.UI
{
    /// <summary>
    /// World Space MR düğmesi — OVR ray + pinch/tetik desteği, haptik geri bildirim.
    /// </summary>
    public class MRUIButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private bool _interactable = true;
        [SerializeField] private UnityEvent _onClick;
        [SerializeField] private Color _normalColor = new Color(0.05f, 0.55f, 0.85f, 0.82f);
        [SerializeField] private Color _hoverColor = new Color(0f, 0.85f, 1f, 0.95f);
        [SerializeField] private Color _pressedColor = new Color(1f, 0.94f, 0.45f, 0.95f);
        [SerializeField] private float _pointerHitPadding = 26f;
        [SerializeField] private bool _directTouchEnabled = true;
        [SerializeField] private float _touchHitPadding = 22f;
        [SerializeField] private float _touchPressDepth = 0.025f;
        [SerializeField] private float _touchHoverDepth = 0.055f;
        [SerializeField] private float _touchCooldownSeconds = 0.35f;

        /// <summary>Tıklama olayı.</summary>
        public event Action OnClicked;

        private bool _hovered;
        private RectTransform _rectTransform;
        private Image _image;
        private int _lastClickFrame = -1;
        private bool _wasTouching;
        private float _nextTouchTime;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _image = GetComponent<Image>();
            ApplyVisual(false, false);
        }

        /// <summary>Etkileşimi aç/kapat (kilitli menü öğeleri için).</summary>
        public void SetInteractable(bool interactable)
        {
            _interactable = interactable;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable)
            {
                return;
            }

            FireClick();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            ApplyVisual(true, false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            ApplyVisual(false, false);
        }

        private void Update()
        {
            if (!_interactable || !IsVisibleInCanvasGroup())
            {
                _wasTouching = false;
                ApplyVisual(false, false);
                return;
            }

            bool touchHover = IsRightIndexOverButton(_touchHoverDepth, _touchHitPadding);
            bool touchPress = IsRightIndexOverButton(_touchPressDepth, _touchHitPadding);
            bool hovered = _hovered || IsRightPointerOverButton() || touchHover;
            bool pressed = hovered && MRInputHelper.GetRightSelectDown();
            if (touchPress && !_wasTouching && Time.unscaledTime >= _nextTouchTime)
            {
                pressed = true;
                _nextTouchTime = Time.unscaledTime + _touchCooldownSeconds;
            }

            _wasTouching = touchPress;
            ApplyVisual(hovered, pressed);

            if (pressed)
            {
                FireClick();
            }
        }

        private void FireClick()
        {
            if (_lastClickFrame == Time.frameCount)
            {
                return;
            }

            _lastClickFrame = Time.frameCount;
            MRInputHelper.TriggerRightHaptic();
            _onClick?.Invoke();
            OnClicked?.Invoke();
        }

        private bool IsRightPointerOverButton()
        {
            if (_rectTransform == null || !MRInputHelper.TryGetRightPointerRay(out Ray ray))
            {
                return false;
            }

            var plane = new Plane(_rectTransform.forward, _rectTransform.position);
            if (!plane.Raycast(ray, out float enter) || enter < 0f || enter > 3f)
            {
                return false;
            }

            Vector3 worldPoint = ray.GetPoint(enter);
            Vector3 localPoint = _rectTransform.InverseTransformPoint(worldPoint);
            Rect paddedRect = _rectTransform.rect;
            paddedRect.xMin -= _pointerHitPadding;
            paddedRect.xMax += _pointerHitPadding;
            paddedRect.yMin -= _pointerHitPadding;
            paddedRect.yMax += _pointerHitPadding;
            return paddedRect.Contains(new Vector2(localPoint.x, localPoint.y));
        }

        private bool IsRightIndexOverButton(float depthMeters, float padding)
        {
            if (!_directTouchEnabled
                || _rectTransform == null
                || !MRInputHelper.TryGetRightIndexTip(out Vector3 fingertip))
            {
                return false;
            }

            var plane = new Plane(_rectTransform.forward, _rectTransform.position);
            float distance = Mathf.Abs(plane.GetDistanceToPoint(fingertip));
            if (distance > depthMeters)
            {
                return false;
            }

            Vector3 localPoint = _rectTransform.InverseTransformPoint(fingertip);
            Rect paddedRect = _rectTransform.rect;
            paddedRect.xMin -= padding;
            paddedRect.xMax += padding;
            paddedRect.yMin -= padding;
            paddedRect.yMax += padding;
            return paddedRect.Contains(new Vector2(localPoint.x, localPoint.y));
        }

        private bool IsVisibleInCanvasGroup()
        {
            foreach (var group in GetComponentsInParent<CanvasGroup>(false))
            {
                if (!group.interactable || !group.blocksRaycasts || group.alpha < 0.15f)
                {
                    return false;
                }
            }

            return isActiveAndEnabled;
        }

        private void ApplyVisual(bool hovered, bool pressed)
        {
            if (_image == null)
            {
                return;
            }

            _image.color = pressed ? _pressedColor : hovered ? _hoverColor : _normalColor;
        }
    }
}
