using UnityEngine;
using TMPro;
using AlcoholSimVR.Core;

namespace AlcoholSimVR.UI
{
    /// <summary>
    /// Simülasyon HUD'u — devre dışı. Skor artık plumb line ile hesaplanıyor.
    /// </summary>
    public class SimulationHudController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _stepOffText;
        [SerializeField] private MRUIButton _stopButton;

        private void Awake()
        {
            SetVisible(false);
        }

        private void OnEnable()
        {
            if (AppManager.Instance != null)
                AppManager.Instance.OnStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (AppManager.Instance != null)
                AppManager.Instance.OnStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(AppState previous, AppState current)
        {
            // HUD devre dışı — simülasyon sırasında panel gösterme
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup == null) return;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
    }
}
