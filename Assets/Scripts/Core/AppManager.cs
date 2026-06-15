using System;
using UnityEngine;

namespace AlcoholSimVR.Core
{
    /// <summary>
    /// Uygulama durumları. Geçişler yalnızca <see cref="AppManager"/> üzerinden yapılır.
    /// </summary>
    public enum AppState
    {
        Idle,
        MenuOpen,
        InfoPanel,
        SimulationActive,
        ResultsScreen
    }

    /// <summary>
    /// Merkezi durum makinesi. Passthrough MR deneyiminin tüm alt sistemlerini koordine eder.
    /// OVRCameraRig transformuna asla dokunulmaz.
    /// </summary>
    public class AppManager : MonoBehaviour
    {
        public static AppManager Instance { get; private set; }

        /// <summary>Mevcut uygulama durumu.</summary>
        public AppState CurrentState { get; private set; } = AppState.Idle;

        /// <summary>Durum değiştiğinde: (önceki, yeni).</summary>
        public event Action<AppState, AppState> OnStateChanged;

        [Header("Referanslar")]
        [SerializeField] private Utilities.HandPalmDetector _palmDetector;
        [SerializeField] private UI.WristMenuPanel _wristMenu;
        [SerializeField] private UI.InfoPanelController _infoPanel;
        [SerializeField] private UI.ResultsPanelController _resultsPanel;
        [SerializeField] private Simulation.BoardManager _boardManager;
        [SerializeField] private Simulation.AlcoholEffectController _alcoholEffects;
        [SerializeField] private SessionTracker _sessionTracker;

        [Header("Geri Tuşu")]
        [SerializeField] private float _backButtonLongPressSeconds = 1.0f;

        [Header("Simülasyon")]
        [SerializeField] private float _simulationMaxDurationSeconds = 120f;
        [SerializeField] private bool _autoEndSimulationOnMaxDuration = true;

        private float _backButtonHoldTime;
        private bool _backWasHeld;
        private Coroutine _simulationTimeoutCoroutine;
        private Simulation.AlcoholEffectLevel _selectedEffectLevel = Simulation.AlcoholEffectLevel.Medium;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (_palmDetector == null)
            {
                _palmDetector = FindAnyObjectByType<Utilities.HandPalmDetector>();
            }

            if (_wristMenu == null)
            {
                _wristMenu = FindAnyObjectByType<UI.WristMenuPanel>();
            }

            if (_infoPanel == null)
            {
                _infoPanel = FindAnyObjectByType<UI.InfoPanelController>();
            }

            if (_resultsPanel == null)
            {
                _resultsPanel = FindAnyObjectByType<UI.ResultsPanelController>();
            }

            if (_boardManager == null)
            {
                _boardManager = FindAnyObjectByType<Simulation.BoardManager>();
            }

            if (_alcoholEffects == null)
            {
                _alcoholEffects = FindAnyObjectByType<Simulation.AlcoholEffectController>();
            }

            if (_sessionTracker == null)
            {
                _sessionTracker = FindAnyObjectByType<SessionTracker>();
            }
        }

        private void OnEnable()
        {
            if (_palmDetector != null)
            {
                _palmDetector.OnPalmFacingCamera += HandlePalmFacingCamera;
                _palmDetector.OnPalmNotFacingCamera += HandlePalmNotFacingCamera;
            }

            if (_wristMenu != null)
            {
                _wristMenu.OnStraightBeamWalkSelected += HandleStraightBeamWalkSelected;
            }

            if (_infoPanel != null)
            {
                _infoPanel.OnEffectLevelSelected += HandleEffectLevelSelected;
                _infoPanel.OnStartPressed += HandleInfoStartPressed;
            }

            if (_sessionTracker != null)
            {
                _sessionTracker.OnSessionEnded += HandleSessionEnded;
            }

            if (_resultsPanel != null)
            {
                _resultsPanel.OnDismissed += HandleResultsDismissed;
            }
        }

        private void OnDisable()
        {
            if (_palmDetector != null)
            {
                _palmDetector.OnPalmFacingCamera -= HandlePalmFacingCamera;
                _palmDetector.OnPalmNotFacingCamera -= HandlePalmNotFacingCamera;
            }

            if (_wristMenu != null)
            {
                _wristMenu.OnStraightBeamWalkSelected -= HandleStraightBeamWalkSelected;
            }

            if (_infoPanel != null)
            {
                _infoPanel.OnEffectLevelSelected -= HandleEffectLevelSelected;
                _infoPanel.OnStartPressed -= HandleInfoStartPressed;
            }

            if (_sessionTracker != null)
            {
                _sessionTracker.OnSessionEnded -= HandleSessionEnded;
            }

            if (_resultsPanel != null)
            {
                _resultsPanel.OnDismissed -= HandleResultsDismissed;
            }
        }

        private void Start()
        {
            TransitionTo(AppState.Idle, force: true);
        }

        private void Update()
        {
            UpdateBackButtonLongPress();
        }

        /// <summary>
        /// Simülasyonu manuel olarak sonlandırır (HUD veya test için).
        /// </summary>
        public void RequestEndSimulation()
        {
            if (CurrentState != AppState.SimulationActive)
            {
                return;
            }

            _sessionTracker?.EndSession();
        }

        /// <summary>
        /// Herhangi bir durumdan Idle'a döner.
        /// </summary>
        public void ReturnToIdle()
        {
            TransitionTo(AppState.Idle);
        }

        private void UpdateBackButtonLongPress()
        {
            bool backHeld = IsBackButtonHeld();

            if (backHeld)
            {
                _backButtonHoldTime += Time.deltaTime;
                if (_backButtonHoldTime >= _backButtonLongPressSeconds && !_backWasHeld)
                {
                    _backWasHeld = true;
                    ReturnToIdle();
                }
            }
            else
            {
                _backButtonHoldTime = 0f;
                _backWasHeld = false;
            }
        }

        private static bool IsBackButtonHeld()
        {
            try
            {
                return OVRInput.Get(OVRInput.Button.Back, OVRInput.Controller.LTouch)
                    || OVRInput.Get(OVRInput.Button.Back, OVRInput.Controller.RTouch)
                    || OVRInput.Get(OVRInput.Button.Back, OVRInput.Controller.Touch);
            }
            catch
            {
                return false;
            }
        }

        private void HandlePalmFacingCamera()
        {
            if (CurrentState == AppState.Idle)
            {
                TransitionTo(AppState.MenuOpen);
            }
        }

        private void HandlePalmNotFacingCamera()
        {
            if (CurrentState == AppState.MenuOpen)
            {
                TransitionTo(AppState.Idle);
            }
        }

        private void HandleStraightBeamWalkSelected()
        {
            if (CurrentState == AppState.MenuOpen)
            {
                TransitionTo(AppState.InfoPanel);
            }
        }

        private void HandleInfoStartPressed()
        {
            if (CurrentState == AppState.InfoPanel)
            {
                TransitionTo(AppState.SimulationActive);
            }
        }

        private void HandleEffectLevelSelected(Simulation.AlcoholEffectLevel level)
        {
            _selectedEffectLevel = level;
            _alcoholEffects?.SetEffectLevel(level);
            _boardManager?.SetEffectLevel(level);
        }

        private void HandleSessionEnded(SessionTracker.SessionResult result)
        {
            if (CurrentState == AppState.SimulationActive)
            {
                TransitionTo(AppState.ResultsScreen);
            }
        }

        private void HandleResultsDismissed()
        {
            if (CurrentState == AppState.ResultsScreen)
            {
                TransitionTo(AppState.Idle);
            }
        }

        private void TransitionTo(AppState newState, bool force = false)
        {
            if (!force && CurrentState == newState)
            {
                return;
            }

            AppState previous = CurrentState;
            ExitState(previous, newState);
            CurrentState = newState;
            EnterState(newState);
            OnStateChanged?.Invoke(previous, newState);
        }

        private void ExitState(AppState state, AppState nextState)
        {
            switch (state)
            {
                case AppState.MenuOpen:
                    _wristMenu?.SetVisible(false, immediate: false);
                    break;

                case AppState.InfoPanel:
                    _infoPanel?.Hide();
                    break;

                case AppState.SimulationActive:
                    StopSimulationTimeout();
                    _alcoholEffects?.StopSimulation();
                    _boardManager?.DespawnBoard();
                    if (nextState != AppState.ResultsScreen)
                    {
                        _sessionTracker?.EndSession();
                    }
                    break;

                case AppState.ResultsScreen:
                    _resultsPanel?.Hide();
                    break;
            }
        }

        private void EnterState(AppState state)
        {
            switch (state)
            {
                case AppState.Idle:
                    _wristMenu?.SetVisible(false, immediate: true);
                    _infoPanel?.Hide(immediate: true);
                    _resultsPanel?.Hide(immediate: true);
                    _boardManager?.DespawnBoard();
                    _alcoholEffects?.StopSimulation();
                    _sessionTracker?.ResetTracker();
                    break;

                case AppState.MenuOpen:
                    _wristMenu?.SetVisible(true, immediate: false);
                    break;

                case AppState.InfoPanel:
                    _wristMenu?.SetVisible(false, immediate: false);
                    _infoPanel?.SetSelectedEffectLevel(_selectedEffectLevel);
                    _infoPanel?.Show();
                    break;

                case AppState.SimulationActive:
                    _infoPanel?.Hide(immediate: true);
                    _alcoholEffects?.SetEffectLevel(_selectedEffectLevel);
                    _boardManager?.SetEffectLevel(_selectedEffectLevel);
                    _boardManager?.SpawnBoard();
                    _alcoholEffects?.StartSimulation();
                    _sessionTracker?.BeginSession();
                    StartSimulationTimeout();
                    break;

                case AppState.ResultsScreen:
                    _alcoholEffects?.StopSimulation();
                    _boardManager?.DespawnBoard();
                    if (_sessionTracker != null && _sessionTracker.HasResult)
                    {
                        _resultsPanel?.Show(_sessionTracker.LastResult);
                    }
                    break;
            }
        }

        private void StartSimulationTimeout()
        {
            StopSimulationTimeout();
            if (_autoEndSimulationOnMaxDuration && _simulationMaxDurationSeconds > 0f)
            {
                _simulationTimeoutCoroutine = StartCoroutine(SimulationTimeoutRoutine());
            }
        }

        private void StopSimulationTimeout()
        {
            if (_simulationTimeoutCoroutine != null)
            {
                StopCoroutine(_simulationTimeoutCoroutine);
                _simulationTimeoutCoroutine = null;
            }
        }

        private System.Collections.IEnumerator SimulationTimeoutRoutine()
        {
            yield return new WaitForSeconds(_simulationMaxDurationSeconds);
            if (CurrentState == AppState.SimulationActive)
            {
                _sessionTracker?.EndSession();
            }
        }
    }
}
