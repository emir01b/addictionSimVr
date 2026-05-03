using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace AddictionSim
{
    /// <summary>
    /// Hand Menu panelindeki UI elemanlarını yönetir.
    /// SimulationManager ile iletişim kurarak buton tıklamalarını ve
    /// durum metnini güncelleyen controller sınıfı.
    /// </summary>
    public class HandMenuUI : MonoBehaviour
    {
        [Header("Metin Referansları")]
        [Tooltip("Paneldeki durum metnini gösteren TMP bileşeni")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Tooltip("Alt bilgi / bağımlılık tipi metnini gösteren TMP bileşeni")]
        [SerializeField] private TextMeshProUGUI addictionTypeText;

        [Header("Buton Referansları")]
        [SerializeField] private Button cigaretteButton;
        [SerializeField] private Button alcoholButton;
        [SerializeField] private Button drugButton;
        [SerializeField] private Button stopButton;

        [Header("Buton Görsel Feedback")]
        [SerializeField] private Image cigaretteButtonBg;
        [SerializeField] private Image alcoholButtonBg;
        [SerializeField] private Image drugButtonBg;

        [Header("Renkler")]
        [SerializeField] private Color normalButtonColor = new Color(1f, 1f, 1f, 0.08f);
        [SerializeField] private Color activeButtonColor = new Color(0.2f, 0.8f, 0.4f, 0.3f);
        [SerializeField] private Color statusReadyColor = new Color(0.4f, 0.9f, 0.5f, 1f);
        [SerializeField] private Color statusActiveColor = new Color(1f, 0.6f, 0.2f, 1f);

        private SimulationManager simulationManager;

        private void Start()
        {
            simulationManager = SimulationManager.Instance;

            if (simulationManager == null)
            {
                Debug.LogError("[HandMenuUI] SimulationManager bulunamadı!");
                return;
            }

            // Buton listener'ları
            if (cigaretteButton != null)
                cigaretteButton.onClick.AddListener(OnCigaretteClicked);
            if (alcoholButton != null)
                alcoholButton.onClick.AddListener(OnAlcoholClicked);
            if (drugButton != null)
                drugButton.onClick.AddListener(OnDrugClicked);
            if (stopButton != null)
                stopButton.onClick.AddListener(OnStopClicked);

            // Event listener'ları
            simulationManager.OnStateChanged.AddListener(OnStateChanged);
            simulationManager.OnAddictionTypeChanged.AddListener(OnAddictionTypeChanged);

            // İlk UI güncellemesi
            UpdateUI();
        }

        private void OnDestroy()
        {
            if (simulationManager != null)
            {
                simulationManager.OnStateChanged.RemoveListener(OnStateChanged);
                simulationManager.OnAddictionTypeChanged.RemoveListener(OnAddictionTypeChanged);
            }
        }

        // === Buton Callback'leri ===

        private void OnCigaretteClicked()
        {
            // Bilgilendirme panelini aç (simülasyon oradan başlatılacak)
            var infoPanel = InfoPanelController.Instance;
            if (infoPanel != null)
            {
                infoPanel.ShowInfoPanel(SimulationManager.AddictionType.Cigarette);
            }
            else
            {
                // InfoPanel yoksa direkt başlat (fallback)
                simulationManager?.StartCigaretteSimulation();
            }
        }

        private void OnAlcoholClicked()
        {
            var infoPanel = InfoPanelController.Instance;
            if (infoPanel != null)
            {
                infoPanel.ShowInfoPanel(SimulationManager.AddictionType.Alcohol);
            }
            else
            {
                simulationManager?.StartAlcoholSimulation();
            }
        }

        private void OnDrugClicked()
        {
            var infoPanel = InfoPanelController.Instance;
            if (infoPanel != null)
            {
                infoPanel.ShowInfoPanel(SimulationManager.AddictionType.Drug);
            }
            else
            {
                simulationManager?.StartDrugSimulation();
            }
        }

        private void OnStopClicked()
        {
            // Bilgilendirme paneli açıksa onu da kapat
            var infoPanel = InfoPanelController.Instance;
            if (infoPanel != null)
            {
                infoPanel.HideInfoPanel();
            }

            simulationManager?.StopSimulation();
        }

        // === Event Handler'lar ===

        private void OnStateChanged(SimulationManager.SimulationState newState)
        {
            UpdateUI();
        }

        private void OnAddictionTypeChanged(SimulationManager.AddictionType newType)
        {
            UpdateUI();
        }

        // === UI Güncelleme ===

        private void UpdateUI()
        {
            if (simulationManager == null) return;

            // Durum metni güncelle
            if (statusText != null)
            {
                statusText.text = simulationManager.StateText;
                statusText.color = simulationManager.CurrentState == SimulationManager.SimulationState.Active
                    ? statusActiveColor
                    : statusReadyColor;
            }

            // Bağımlılık tipi metni güncelle
            if (addictionTypeText != null)
            {
                addictionTypeText.text = simulationManager.AddictionText;
            }

            // Buton görsellerini güncelle
            UpdateButtonVisuals();
        }

        private void UpdateButtonVisuals()
        {
            if (simulationManager == null) return;

            var activeType = simulationManager.CurrentAddiction;

            // Aktif olan butona vurgu rengi
            if (cigaretteButtonBg != null)
                cigaretteButtonBg.color = activeType == SimulationManager.AddictionType.Cigarette
                    ? activeButtonColor : normalButtonColor;

            if (alcoholButtonBg != null)
                alcoholButtonBg.color = activeType == SimulationManager.AddictionType.Alcohol
                    ? activeButtonColor : normalButtonColor;

            if (drugButtonBg != null)
                drugButtonBg.color = activeType == SimulationManager.AddictionType.Drug
                    ? activeButtonColor : normalButtonColor;

            // Simülasyon aktifken stop butonunu göster/aktifleştir
            if (stopButton != null)
            {
                stopButton.interactable = simulationManager.CurrentState == SimulationManager.SimulationState.Active;
            }
        }
    }
}
