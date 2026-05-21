using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AlcoholSimVR.Simulation
{
    /// <summary>
    /// Alkol bozulma simülasyonu: URP post-processing + kamera sallanması (child offset).
    /// OVRCameraRig asla hareket ettirilmez.
    /// </summary>
    public class AlcoholEffectController : MonoBehaviour
    {
        [Header("Post-Processing Volume")]
        [SerializeField] private Volume _postProcessVolume;

        [Header("Kamera Sallanması (Child Offset)")]
        [SerializeField] private Transform _cameraSwayOffset;
        [SerializeField] private float _yawSwayDegrees = 1.5f;
        [SerializeField] private float _yawSwayFrequency = 0.7f;
        [SerializeField] private float _rollSwayDegrees = 1.0f;
        [SerializeField] private float _rollSwayFrequency = 0.4f;
        [SerializeField] private float _rollPhaseOffset = 1.2f;

        [Header("Alkol Seviyesi")]
        [SerializeField, Range(0f, 1f)] private float _alcoholLevel = 0.3f;
        [SerializeField] private float _startLevel = 0.3f;
        [SerializeField] private float _targetLevel = 0.7f;
        [SerializeField] private float _rampDurationSeconds = 30f;

        [Header("Chromatic Aberration")]
        [SerializeField] private float _chromaticMax = 0.8f;
        [SerializeField] private float _chromaticRampSeconds = 10f;

        [Header("Lens Distortion")]
        [SerializeField] private float _lensDistortionMin = -0.1f;
        [SerializeField] private float _lensDistortionMax = 0.1f;
        [SerializeField] private float _lensDistortionPeriodMin = 3f;
        [SerializeField] private float _lensDistortionPeriodMax = 5f;

        [Header("Vignette")]
        [SerializeField] private float _vignetteMin = 0.3f;
        [SerializeField] private float _vignetteMax = 0.5f;
        [SerializeField] private float _vignettePulseFrequency = 0.35f;

        [Header("Depth of Field")]
        [SerializeField] private float _dofApertureMin = 4f;
        [SerializeField] private float _dofApertureMax = 6f;
        [SerializeField] private float _dofFocusDistance = 2f;

        [Header("Blur Pulse")]
        [SerializeField] private float _blurPulseIntervalMin = 8f;
        [SerializeField] private float _blurPulseIntervalMax = 12f;
        [SerializeField] private float _blurPulseDurationMin = 1f;
        [SerializeField] private float _blurPulseDurationMax = 2f;
        [SerializeField] private float _blurPulseApertureBoost = 3f;

        private ChromaticAberration _chromatic;
        private LensDistortion _lensDistortion;
        private Vignette _vignette;
        private DepthOfField _depthOfField;

        private bool _simulationRunning;
        private float _simulationTime;
        private float _lensPeriod;
        private float _lensPhase;
        private Coroutine _rampCoroutine;
        private Coroutine _blurPulseCoroutine;
        private Quaternion _swayBaseLocalRotation = Quaternion.identity;

        /// <summary>0–1 arası mevcut alkol etki yoğunluğu.</summary>
        public float AlcoholLevel => _alcoholLevel;

        private void Awake()
        {
            CacheVolumeOverrides();
            if (_postProcessVolume != null)
            {
                _postProcessVolume.weight = 0f;
            }

            if (_cameraSwayOffset != null)
            {
                _swayBaseLocalRotation = _cameraSwayOffset.localRotation;
            }
        }

        private void Update()
        {
            if (!_simulationRunning)
            {
                return;
            }

            _simulationTime += Time.deltaTime;
            ApplyPostProcessing();
            ApplyCameraSway();
        }

        /// <summary>Simülasyon efektlerini başlatır ve seviyeyi rampeler.</summary>
        public void StartSimulation()
        {
            _simulationRunning = true;
            _simulationTime = 0f;
            _alcoholLevel = _startLevel;
            _lensPeriod = Random.Range(_lensDistortionPeriodMin, _lensDistortionPeriodMax);
            _lensPhase = Random.Range(0f, Mathf.PI * 2f);

            EnableVolumeOverrides(true);
            if (_postProcessVolume != null)
            {
                _postProcessVolume.weight = 1f;
            }

            if (_rampCoroutine != null)
            {
                StopCoroutine(_rampCoroutine);
            }

            _rampCoroutine = StartCoroutine(RampAlcoholLevelRoutine());

            if (_blurPulseCoroutine != null)
            {
                StopCoroutine(_blurPulseCoroutine);
            }

            _blurPulseCoroutine = StartCoroutine(BlurPulseLoopRoutine());
        }

        /// <summary>Tüm efektleri durdurur ve varsayılanlara döner.</summary>
        public void StopSimulation()
        {
            _simulationRunning = false;

            if (_rampCoroutine != null)
            {
                StopCoroutine(_rampCoroutine);
                _rampCoroutine = null;
            }

            if (_blurPulseCoroutine != null)
            {
                StopCoroutine(_blurPulseCoroutine);
                _blurPulseCoroutine = null;
            }

            ResetPostProcessing();
            ResetCameraSway();
            EnableVolumeOverrides(false);
            if (_postProcessVolume != null)
            {
                _postProcessVolume.weight = 0f;
            }
        }

        private void CacheVolumeOverrides()
        {
            if (_postProcessVolume == null || _postProcessVolume.profile == null)
            {
                return;
            }

            VolumeProfile profile = _postProcessVolume.profile;
            profile.TryGet(out _chromatic);
            profile.TryGet(out _lensDistortion);
            profile.TryGet(out _vignette);
            profile.TryGet(out _depthOfField);
        }

        private void EnableVolumeOverrides(bool active)
        {
            SetOverrideActive(_chromatic, active);
            SetOverrideActive(_lensDistortion, active);
            SetOverrideActive(_vignette, active);
            SetOverrideActive(_depthOfField, active);
        }

        private static void SetOverrideActive<T>(T component, bool active) where T : VolumeComponent
        {
            if (component == null)
            {
                return;
            }

            component.active = active;
        }

        private void ApplyPostProcessing()
        {
            float intensity = Mathf.Clamp01(_alcoholLevel);

            float chromaticT = Mathf.Clamp01(_simulationTime / Mathf.Max(0.01f, _chromaticRampSeconds));
            float chromatic = Mathf.Lerp(0f, _chromaticMax, chromaticT) * intensity;

            if (_chromatic != null)
            {
                _chromatic.intensity.Override(chromatic);
            }

            if (_lensDistortion != null)
            {
                float lensWave = Mathf.Sin((_simulationTime / _lensPeriod) * Mathf.PI * 2f + _lensPhase);
                float lens = Mathf.Lerp(_lensDistortionMin, _lensDistortionMax, (lensWave + 1f) * 0.5f) * intensity;
                _lensDistortion.intensity.Override(lens);
            }

            if (_vignette != null)
            {
                float vignetteWave = (Mathf.Sin(_simulationTime * _vignettePulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
                float vignette = Mathf.Lerp(_vignetteMin, _vignetteMax, vignetteWave) * intensity;
                _vignette.intensity.Override(vignette);
            }

            if (_depthOfField != null)
            {
                _depthOfField.mode.Override(DepthOfFieldMode.Bokeh);
                _depthOfField.focusDistance.Override(_dofFocusDistance);
                float aperture = Mathf.Lerp(_dofApertureMin, _dofApertureMax, intensity);
                _depthOfField.aperture.Override(aperture);
            }
        }

        private void ApplyCameraSway()
        {
            if (_cameraSwayOffset == null)
            {
                return;
            }

            float intensity = Mathf.Clamp01(_alcoholLevel);
            float yaw = Mathf.Sin(_simulationTime * _yawSwayFrequency * Mathf.PI * 2f) * _yawSwayDegrees * intensity;
            float roll = Mathf.Sin(_simulationTime * _rollSwayFrequency * Mathf.PI * 2f + _rollPhaseOffset)
                * _rollSwayDegrees * intensity;

            Quaternion sway = Quaternion.Euler(0f, yaw, roll);
            _cameraSwayOffset.localRotation = _swayBaseLocalRotation * sway;
        }

        private void ResetPostProcessing()
        {
            if (_chromatic != null)
            {
                _chromatic.intensity.Override(0f);
            }

            if (_lensDistortion != null)
            {
                _lensDistortion.intensity.Override(0f);
            }

            if (_vignette != null)
            {
                _vignette.intensity.Override(0f);
            }

            if (_depthOfField != null)
            {
                _depthOfField.aperture.Override(_dofApertureMin);
            }
        }

        private void ResetCameraSway()
        {
            if (_cameraSwayOffset != null)
            {
                _cameraSwayOffset.localRotation = _swayBaseLocalRotation;
            }
        }

        private IEnumerator RampAlcoholLevelRoutine()
        {
            float elapsed = 0f;
            while (elapsed < _rampDurationSeconds && _simulationRunning)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _rampDurationSeconds);
                _alcoholLevel = Mathf.Lerp(_startLevel, _targetLevel, t);
                yield return null;
            }

            if (_simulationRunning)
            {
                _alcoholLevel = _targetLevel;
            }
        }

        private IEnumerator BlurPulseLoopRoutine()
        {
            while (_simulationRunning)
            {
                float wait = Random.Range(_blurPulseIntervalMin, _blurPulseIntervalMax);
                yield return new WaitForSeconds(wait);

                if (!_simulationRunning || _depthOfField == null)
                {
                    continue;
                }

                float duration = Random.Range(_blurPulseDurationMin, _blurPulseDurationMax);
                yield return StartCoroutine(BlurPulseRoutine(duration));
            }
        }

        private IEnumerator BlurPulseRoutine(float duration)
        {
            float baseAperture = Mathf.Lerp(_dofApertureMin, _dofApertureMax, _alcoholLevel);
            float peakAperture = baseAperture + _blurPulseApertureBoost;
            float half = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half && _simulationRunning)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                _depthOfField.aperture.Override(Mathf.Lerp(baseAperture, peakAperture, t));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half && _simulationRunning)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                _depthOfField.aperture.Override(Mathf.Lerp(peakAperture, baseAperture, t));
                yield return null;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _alcoholLevel = Mathf.Clamp01(_alcoholLevel);
        }
#endif
    }
}
