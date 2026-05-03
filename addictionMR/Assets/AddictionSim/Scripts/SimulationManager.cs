using System;
using UnityEngine;
using UnityEngine.Events;

namespace AddictionSim
{
    /// <summary>
    /// Bağımlılık simülasyonunun merkezi yönetici sınıfı.
    /// Senaryo durumunu, aktif bağımlılık tipini ve efektleri kontrol eder.
    /// </summary>
    public class SimulationManager : MonoBehaviour
    {
        public enum AddictionType
        {
            None,
            Cigarette,  // Sigara
            Alcohol,    // Alkol
            Drug        // Uyuşturucu
        }

        public enum SimulationState
        {
            Ready,      // Hazır
            Active,     // Senaryo Aktif
            Paused      // Duraklatıldı
        }

        [Header("Durum")]
        [SerializeField] private SimulationState currentState = SimulationState.Ready;
        [SerializeField] private AddictionType currentAddiction = AddictionType.None;

        [Header("Events")]
        public UnityEvent<SimulationState> OnStateChanged;
        public UnityEvent<AddictionType> OnAddictionTypeChanged;
        public UnityEvent OnSimulationStarted;
        public UnityEvent OnSimulationStopped;

        // Singleton pattern
        public static SimulationManager Instance { get; private set; }

        public SimulationState CurrentState => currentState;
        public AddictionType CurrentAddiction => currentAddiction;

        /// <summary>
        /// Mevcut durumu Türkçe metin olarak döndürür.
        /// </summary>
        public string StateText
        {
            get
            {
                return currentState switch
                {
                    SimulationState.Ready => "Hazır",
                    SimulationState.Active => "Senaryo Aktif",
                    SimulationState.Paused => "Duraklatıldı",
                    _ => "Bilinmiyor"
                };
            }
        }

        /// <summary>
        /// Aktif bağımlılık tipini Türkçe metin olarak döndürür.
        /// </summary>
        public string AddictionText
        {
            get
            {
                return currentAddiction switch
                {
                    AddictionType.Cigarette => "Sigara Senaryosu",
                    AddictionType.Alcohol => "Alkol Senaryosu",
                    AddictionType.Drug => "Uyuşturucu Senaryosu",
                    AddictionType.None => "",
                    _ => ""
                };
            }
        }

        private void Awake()
        {
            // Singleton kurulumu
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Sigara senaryosunu başlatır.
        /// </summary>
        public void StartCigaretteSimulation()
        {
            StartSimulation(AddictionType.Cigarette);
        }

        /// <summary>
        /// Alkol senaryosunu başlatır.
        /// </summary>
        public void StartAlcoholSimulation()
        {
            StartSimulation(AddictionType.Alcohol);
        }

        /// <summary>
        /// Uyuşturucu senaryosunu başlatır.
        /// </summary>
        public void StartDrugSimulation()
        {
            StartSimulation(AddictionType.Drug);
        }

        /// <summary>
        /// Belirtilen bağımlılık tipinde simülasyonu başlatır.
        /// </summary>
        public void StartSimulation(AddictionType type)
        {
            if (currentState == SimulationState.Active && currentAddiction == type)
            {
                Debug.Log($"[AddictionSim] {type} senaryosu zaten aktif.");
                return;
            }

            currentAddiction = type;
            SetState(SimulationState.Active);

            OnAddictionTypeChanged?.Invoke(currentAddiction);
            OnSimulationStarted?.Invoke();

            Debug.Log($"[AddictionSim] {AddictionText} başlatıldı.");
        }

        /// <summary>
        /// Aktif simülasyonu durdurur.
        /// </summary>
        public void StopSimulation()
        {
            if (currentState == SimulationState.Ready)
            {
                Debug.Log("[AddictionSim] Zaten durdurulmuş durumda.");
                return;
            }

            currentAddiction = AddictionType.None;
            SetState(SimulationState.Ready);

            OnAddictionTypeChanged?.Invoke(currentAddiction);
            OnSimulationStopped?.Invoke();

            Debug.Log("[AddictionSim] Simülasyon durduruldu.");
        }

        private void SetState(SimulationState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            OnStateChanged?.Invoke(currentState);
        }
    }
}
