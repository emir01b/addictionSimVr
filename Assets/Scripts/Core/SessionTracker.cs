using System;
using UnityEngine;

namespace AlcoholSimVR.Core
{
    /// <summary>
    /// Plumb line (dikme) değerlendirmesi: kafanın dikey izdüşümünün tahta üzerinde
    /// kalma oranını hesaplar. HUD yok — skor simülasyon sonunda gösterilir.
    /// </summary>
    public class SessionTracker : MonoBehaviour
    {
        [Serializable]
        public struct SessionResult
        {
            public float TotalDurationSeconds;
            public float TimeOnBeamSeconds;
            public float BalanceScorePercent;
        }

        public event Action<SessionResult> OnSessionEnded;

        public bool HasResult { get; private set; }
        public SessionResult LastResult { get; private set; }

        private Simulation.BoardManager _boardManager;
        private Transform _cameraTransform;

        private bool _sessionActive;
        private bool _evaluationStarted;
        private float _totalEvalTime;
        private float _timeOnBeam;

        private void Update()
        {
            if (!_sessionActive) return;

            if (_cameraTransform == null || _boardManager == null) return;

            bool onBoard = _boardManager.IsPlumbLineOnBoard(_cameraTransform.position, out bool pastEnds);

            // Dikme tahtaya ilk kez değene kadar bekle
            if (!_evaluationStarted)
            {
                if (onBoard)
                    _evaluationStarted = true;
                return;
            }

            // Değerlendirme aktif
            _totalEvalTime += Time.deltaTime;
            if (onBoard)
                _timeOnBeam += Time.deltaTime;

            // Kullanıcı tahtanın ön veya arkasından çıktı → simülasyon biter
            if (pastEnds)
                EndSession();
        }

        public void BeginSession()
        {
            ResetTracker();
            _sessionActive = true;
            _boardManager = FindAnyObjectByType<Simulation.BoardManager>();
            ResolveCameraTransform();
        }

        public void EndSession()
        {
            if (!_sessionActive) return;
            _sessionActive = false;

            float score = _totalEvalTime > 0.01f
                ? (_timeOnBeam / _totalEvalTime) * 100f
                : 0f;

            LastResult = new SessionResult
            {
                TotalDurationSeconds = _totalEvalTime,
                TimeOnBeamSeconds = _timeOnBeam,
                BalanceScorePercent = score
            };

            HasResult = true;
            OnSessionEnded?.Invoke(LastResult);
        }

        public void ResetTracker()
        {
            _sessionActive = false;
            _evaluationStarted = false;
            _totalEvalTime = 0f;
            _timeOnBeam = 0f;
            HasResult = false;
        }

        // Eski API — BeamWalkTrigger uyumluluğu için boş bırakıldı
        public void NotifyEnteredBeam() { }
        public void NotifyExitedBeam() { }

        private void ResolveCameraTransform()
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null && rig.centerEyeAnchor != null)
            {
                _cameraTransform = rig.centerEyeAnchor;
                return;
            }
            if (Camera.main != null)
                _cameraTransform = Camera.main.transform;
        }
    }
}
