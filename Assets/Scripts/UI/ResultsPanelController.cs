using System;
using System.Collections;
using UnityEngine;
using TMPro;
using AlcoholSimVR.Core;

namespace AlcoholSimVR.UI
{
    /// <summary>
    /// Oturum sonuç ekranı — süre, denge skoru ve renkli geri bildirim.
    /// </summary>
    public class ResultsPanelController : MonoBehaviour
    {
        /// <summary>Kullanıcı paneli kapattığında.</summary>
        public event Action OnDismissed;

        [Header("UI")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panelRoot;
        [SerializeField] private TextMeshProUGUI _durationText;
        [SerializeField] private TextMeshProUGUI _stepOffText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _feedbackText;
        [SerializeField] private MRUIButton _closeButton;

        [Header("Skor Renkleri")]
        [SerializeField] private Color _greenFeedback = new Color(0.2f, 0.9f, 0.3f);
        [SerializeField] private Color _yellowFeedback = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color _redFeedback = new Color(1f, 0.35f, 0.35f);
        [SerializeField] private float _greenThreshold = 70f;
        [SerializeField] private float _yellowThreshold = 40f;

        [Header("Animasyon")]
        [SerializeField] private float _appearDuration = 0.35f;
        [SerializeField] private float _spawnDistance = 1.0f;

        private Coroutine _animRoutine;

        private void Awake()
        {
            if (_closeButton != null)
                _closeButton.OnClicked += HandleClose;
            Hide(immediate: true);
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.OnClicked -= HandleClose;
        }

        /// <summary>Sonuçları gösterir.</summary>
        public void Show(SessionTracker.SessionResult result)
        {
            PopulateTexts(result);
            PositionInFrontOfCamera();
            gameObject.SetActive(true);

            if (_animRoutine != null)
                StopCoroutine(_animRoutine);
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
                gameObject.SetActive(false);
                return;
            }

            StartCoroutine(HideRoutine());
        }

        private void PopulateTexts(SessionTracker.SessionResult result)
        {
            if (_durationText != null)
            {
                TimeSpan span = TimeSpan.FromSeconds(result.TotalDurationSeconds);
                _durationText.text = $"Süre: {span:mm\\:ss}";
            }

            // Tahta üzerinde kalma süresi
            if (_stepOffText != null)
            {
                TimeSpan onBeam = TimeSpan.FromSeconds(result.TimeOnBeamSeconds);
                _stepOffText.text = $"Tahta üzerinde: {onBeam:mm\\:ss}";
            }

            if (_scoreText != null)
            {
                _scoreText.text = $"Denge skoru: %{result.BalanceScorePercent:F0}";
            }

            if (_feedbackText != null)
            {
                float score = result.BalanceScorePercent;
                if (score >= _greenThreshold)
                {
                    _feedbackText.text = "Çok iyi denge!";
                    _feedbackText.color = _greenFeedback;
                }
                else if (score >= _yellowThreshold)
                {
                    _feedbackText.text = "Orta düzey denge";
                    _feedbackText.color = _yellowFeedback;
                }
                else
                {
                    _feedbackText.text = "Denge geliştirilmeli";
                    _feedbackText.color = _redFeedback;
                }
            }
        }

        private void PositionInFrontOfCamera()
        {
            Transform cam = ResolveCameraTransform();
            if (cam == null) return;

            transform.position = cam.position + cam.forward * _spawnDistance;
            transform.rotation = Quaternion.LookRotation(transform.position - cam.position, Vector3.up);
        }

        private Transform ResolveCameraTransform()
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null && rig.centerEyeAnchor != null)
                return rig.centerEyeAnchor;
            return Camera.main != null ? Camera.main.transform : null;
        }

        private void HandleClose()
        {
            Hide();
            OnDismissed?.Invoke();
        }

        private IEnumerator AppearRoutine()
        {
            _canvasGroup.alpha = 0f;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _appearDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _animRoutine = null;
        }

        private IEnumerator HideRoutine()
        {
            float elapsed = 0f;
            float duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
