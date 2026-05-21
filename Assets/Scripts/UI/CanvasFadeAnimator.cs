using System.Collections;
using UnityEngine;

namespace AlcoholSimVR.UI
{
    /// <summary>
    /// CanvasGroup alpha animasyonu (coroutine tabanlı).
    /// </summary>
    public class CanvasFadeAnimator : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeDuration = 0.25f;

        private Coroutine _fadeRoutine;

        /// <summary>Hedef alpha değerine animasyonlu geçiş.</summary>
        public void FadeTo(float targetAlpha, bool immediate = false)
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }

            if (_canvasGroup == null)
            {
                return;
            }

            if (_fadeRoutine != null)
            {
                StopCoroutine(_fadeRoutine);
            }

            if (immediate)
            {
                _canvasGroup.alpha = targetAlpha;
                _canvasGroup.interactable = targetAlpha > 0.9f;
                _canvasGroup.blocksRaycasts = targetAlpha > 0.9f;
                return;
            }

            _fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
        }

        private IEnumerator FadeRoutine(float targetAlpha)
        {
            float start = _canvasGroup.alpha;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, _fadeDuration);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, t);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            _canvasGroup.interactable = targetAlpha > 0.9f;
            _canvasGroup.blocksRaycasts = targetAlpha > 0.9f;
            _fadeRoutine = null;
        }
    }
}
