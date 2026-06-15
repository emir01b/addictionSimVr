using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AlcoholSimVR.Simulation
{
    public enum AlcoholEffectLevel
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Alkol bozulma simülasyonu: URP post-processing + kamera sallanması (child offset).
    /// OVRCameraRig asla hareket ettirilmez.
    /// </summary>
    [DefaultExecutionOrder(10000)]
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

        [Header("Etki Kademesi")]
        [SerializeField] private AlcoholEffectLevel _effectLevel = AlcoholEffectLevel.Medium;

        [Header("Alkol Seviyesi")]
        [SerializeField, Range(0f, 1f)] private float _alcoholLevel = 0.3f;
        [SerializeField] private float _startLevel = 0.3f;
        [SerializeField] private float _targetLevel = 0.7f;
        [SerializeField] private float _rampDurationSeconds = 30f;

        [Header("Chromatic Aberration")]
        [SerializeField] private float _chromaticMax = 0.8f;
        [SerializeField] private float _chromaticRampSeconds = 10f;

        [Header("Motion Blur / Head Drag")]
        [SerializeField] private float _headMotionDeadZoneDegreesPerSecond = 8f;
        [SerializeField] private float _motionActivitySmoothTime = 0.08f;
        [SerializeField] private bool _enableHeadMotionDrag = true;
        [SerializeField] private bool _enableHeadMotionFrameHold = true;
        [SerializeField] private bool _enableMotionBlur = true;

        [Header("Passthrough Style Distortion")]
        [SerializeField] private bool _enablePassthroughStyleEffect = true;
        [SerializeField] private float _passthroughStyleSmoothTime = 0.045f;

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
        [SerializeField] private bool _enableRandomBlurPulses = false;

        private ChromaticAberration _chromatic;
        private LensDistortion _lensDistortion;
        private Vignette _vignette;
        private DepthOfField _depthOfField;
        private MotionBlur _motionBlur;

        private bool _simulationRunning;
        private float _simulationTime;
        private float _lensPeriod;
        private float _lensPhase;
        private Coroutine _rampCoroutine;
        private Coroutine _blurPulseCoroutine;
        private Quaternion _swayBaseLocalRotation = Quaternion.identity;
        private Transform _headTrackingReference;
        private Quaternion _lastHeadLocalRotation = Quaternion.identity;
        private bool _hasLastHeadRotation;
        private float _motionActivity;
        private float _motionActivityVelocity;
        private Vector3 _motionDragEuler;
        private Vector3 _motionDragVelocity;
        private Vector3 _heldMotionDragEuler;
        private Vector3 _heldMotionDragVelocity;
        private float _headMotionHoldTimer;
        private OVRPassthroughLayer _passthroughLayer;
        private float _passthroughStyleActivity;
        private float _passthroughStyleVelocity;
        private float _passthroughStyleHoldTimer;
        private float _heldPassthroughBrightness;
        private float _heldPassthroughContrast;
        private float _heldPassthroughSaturation;

        /// <summary>0–1 arası mevcut alkol etki yoğunluğu.</summary>
        public float AlcoholLevel => _alcoholLevel;
        public AlcoholEffectLevel EffectLevel => _effectLevel;

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

        private void LateUpdate()
        {
            if (!_simulationRunning)
            {
                return;
            }

            _simulationTime += Time.deltaTime;
            float motionActivity = UpdateHeadMotionActivity();
            ApplyPostProcessing(motionActivity);
            ApplyCameraSway(motionActivity);
            ApplyPassthroughStyle(motionActivity);
        }

        public void SetEffectLevel(AlcoholEffectLevel level)
        {
            _effectLevel = level;
        }

        /// <summary>Simülasyon efektlerini başlatır ve seviyeyi rampeler.</summary>
        public void StartSimulation()
        {
            CacheVolumeOverrides();
            ResolveHeadTrackingReference();
            EnsureCameraSwayOffsetOwnsHead();
            ResolvePassthroughLayer();
            _simulationRunning = true;
            _simulationTime = 0f;
            _alcoholLevel = GetStartLevelForCurrentLevel();
            _motionActivity = 0f;
            _motionActivityVelocity = 0f;
            _motionDragEuler = Vector3.zero;
            _motionDragVelocity = Vector3.zero;
            _heldMotionDragEuler = Vector3.zero;
            _heldMotionDragVelocity = Vector3.zero;
            _headMotionHoldTimer = 0f;
            _passthroughStyleActivity = 0f;
            _passthroughStyleVelocity = 0f;
            _passthroughStyleHoldTimer = 0f;
            _heldPassthroughBrightness = 0f;
            _heldPassthroughContrast = 0f;
            _heldPassthroughSaturation = 0f;
            _hasLastHeadRotation = false;
            _lensPeriod = Random.Range(_lensDistortionPeriodMin, _lensDistortionPeriodMax);
            _lensPhase = Random.Range(0f, Mathf.PI * 2f);

            ForcePassthroughCameraTransparency();
            EnableVolumeOverrides(false);
            if (_postProcessVolume != null)
            {
                _postProcessVolume.weight = 0f;
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

            if (_enableRandomBlurPulses)
            {
                _blurPulseCoroutine = StartCoroutine(BlurPulseLoopRoutine());
            }
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
            ResetHeadMotionState();
            ResetPassthroughStyle();
            EnableVolumeOverrides(false);
            ForcePassthroughCameraTransparency();
            if (_postProcessVolume != null)
            {
                _postProcessVolume.weight = 0f;
            }
        }

        private void CacheVolumeOverrides()
        {
            EnsurePostProcessVolume();
            if (_postProcessVolume == null)
            {
                return;
            }

            VolumeProfile profile = _postProcessVolume.profile;
            RemoveMissingVolumeComponents(profile);
            _chromatic = EnsureVolumeComponent<ChromaticAberration>(profile);
            _lensDistortion = EnsureVolumeComponent<LensDistortion>(profile);
            _vignette = EnsureVolumeComponent<Vignette>(profile);
            _depthOfField = EnsureVolumeComponent<DepthOfField>(profile);
            _motionBlur = EnsureVolumeComponent<MotionBlur>(profile);
        }

        private void EnsurePostProcessVolume()
        {
            if (_postProcessVolume != null)
            {
                return;
            }

            foreach (Volume volume in FindObjectsByType<Volume>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (volume != null && volume.name.IndexOf("Alcohol", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _postProcessVolume = volume;
                    return;
                }
            }

            var volumeGo = new GameObject("AlcoholPostProcessVolume");
            volumeGo.transform.SetParent(transform, false);
            _postProcessVolume = volumeGo.AddComponent<Volume>();
            _postProcessVolume.isGlobal = true;
            _postProcessVolume.priority = 10f;
            _postProcessVolume.weight = 0f;
        }

        private static T EnsureVolumeComponent<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile == null)
            {
                return null;
            }

            if (profile.TryGet(out T component))
            {
                return component;
            }

            return profile.Add<T>(true);
        }

        private static void RemoveMissingVolumeComponents(VolumeProfile profile)
        {
            if (profile == null || profile.components == null)
            {
                return;
            }

            for (int i = profile.components.Count - 1; i >= 0; i--)
            {
                if (profile.components[i] == null)
                {
                    profile.components.RemoveAt(i);
                }
            }
        }

        private void EnableVolumeOverrides(bool active)
        {
            SetOverrideActive(_chromatic, active);
            SetOverrideActive(_lensDistortion, active);
            SetOverrideActive(_vignette, active);
            SetOverrideActive(_depthOfField, active);
            SetOverrideActive(_motionBlur, active);
        }

        private static void SetOverrideActive<T>(T component, bool active) where T : VolumeComponent
        {
            if (component == null)
            {
                return;
            }

            component.active = active;
        }

        private void ApplyPostProcessing(float motionActivity)
        {
            float intensity = Mathf.Clamp01(_alcoholLevel);
            float visualMultiplier = GetVisualMultiplierForCurrentLevel();

            float chromaticT = Mathf.Clamp01(_simulationTime / Mathf.Max(0.01f, _chromaticRampSeconds));
            float chromatic = Mathf.Lerp(0f, _chromaticMax, chromaticT) * intensity * visualMultiplier;

            if (_chromatic != null)
            {
                _chromatic.intensity.Override(chromatic);
            }

            if (_lensDistortion != null)
            {
                float lensWave = Mathf.Sin((_simulationTime / _lensPeriod) * Mathf.PI * 2f + _lensPhase);
                float lens = Mathf.Lerp(_lensDistortionMin, _lensDistortionMax, (lensWave + 1f) * 0.5f)
                    * intensity
                    * visualMultiplier;
                _lensDistortion.intensity.Override(lens);
            }

            if (_vignette != null)
            {
                float vignetteWave = (Mathf.Sin(_simulationTime * _vignettePulseFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
                float vignette = Mathf.Lerp(_vignetteMin, _vignetteMax, vignetteWave) * intensity * visualMultiplier;
                _vignette.intensity.Override(vignette);
            }

            if (_depthOfField != null)
            {
                _depthOfField.mode.Override(DepthOfFieldMode.Bokeh);
                _depthOfField.focusDistance.Override(_dofFocusDistance);
                float aperture = Mathf.Lerp(_dofApertureMin, _dofApertureMax, intensity);
                _depthOfField.aperture.Override(aperture);
            }

            if (_motionBlur != null)
            {
                _motionBlur.mode.Override(MotionBlurMode.CameraOnly);
                _motionBlur.quality.Override(GetMotionBlurQualityForCurrentLevel());
                _motionBlur.clamp.Override(GetMotionBlurClampForCurrentLevel());
                float blur = _enableMotionBlur
                    ? GetMotionBlurIntensityForCurrentLevel() * intensity * motionActivity
                    : 0f;
                _motionBlur.intensity.Override(blur);
            }
        }

        private void ApplyCameraSway(float motionActivity)
        {
            if (_cameraSwayOffset == null)
            {
                return;
            }

            float intensity = Mathf.Clamp01(_alcoholLevel);
            float swayMultiplier = GetSwayMultiplierForCurrentLevel() * motionActivity;
            Vector3 visualDragEuler = GetFrameHeldMotionDragEuler(motionActivity, intensity);
            float yaw = Mathf.Sin(_simulationTime * _yawSwayFrequency * Mathf.PI * 2f)
                * _yawSwayDegrees
                * intensity
                * swayMultiplier;
            float roll = Mathf.Sin(_simulationTime * _rollSwayFrequency * Mathf.PI * 2f + _rollPhaseOffset)
                * _rollSwayDegrees
                * intensity
                * swayMultiplier;

            float stutter = GetMotionStutterDegreesForCurrentLevel() * motionActivity * intensity;
            float stutterYaw = Mathf.Sin(_simulationTime * GetMotionStutterFrequencyForCurrentLevel() * Mathf.PI * 2f) * stutter;
            float stutterPitch = Mathf.Sin((_simulationTime * GetMotionStutterFrequencyForCurrentLevel() * 1.37f + 0.31f) * Mathf.PI * 2f)
                * stutter
                * 0.45f;

            Quaternion sway = Quaternion.Euler(
                visualDragEuler.x + stutterPitch,
                visualDragEuler.y + yaw + stutterYaw,
                visualDragEuler.z + roll);
            _cameraSwayOffset.localRotation = _swayBaseLocalRotation * sway;
        }

        private Vector3 GetFrameHeldMotionDragEuler(float motionActivity, float intensity)
        {
            if (!_enableHeadMotionFrameHold || motionActivity < 0.04f || intensity <= 0.01f)
            {
                _headMotionHoldTimer = 0f;
                _heldMotionDragEuler = Vector3.SmoothDamp(
                    _heldMotionDragEuler,
                    _motionDragEuler,
                    ref _heldMotionDragVelocity,
                    GetMotionDragSmoothTimeForCurrentLevel());
                return _heldMotionDragEuler;
            }

            _headMotionHoldTimer -= Time.deltaTime;
            if (_headMotionHoldTimer <= 0f)
            {
                float jitter = GetMotionFrameJitterDegreesForCurrentLevel() * motionActivity * intensity;
                _heldMotionDragEuler = _motionDragEuler + new Vector3(
                    Random.Range(-jitter * 0.45f, jitter * 0.45f),
                    Random.Range(-jitter, jitter),
                    Random.Range(-jitter * 0.35f, jitter * 0.35f));
                _heldMotionDragVelocity = Vector3.zero;
                _headMotionHoldTimer = 1f / Mathf.Max(1f, GetMotionFrameHoldFpsForCurrentLevel());
            }

            return _heldMotionDragEuler;
        }

        private float UpdateHeadMotionActivity()
        {
            ResolveHeadTrackingReference();
            if (_headTrackingReference == null)
            {
                _motionDragEuler = Vector3.SmoothDamp(
                    _motionDragEuler,
                    Vector3.zero,
                    ref _motionDragVelocity,
                    GetMotionDragSmoothTimeForCurrentLevel());
                return 0f;
            }

            Quaternion currentRotation = _headTrackingReference.localRotation;
            if (!_hasLastHeadRotation)
            {
                _lastHeadLocalRotation = currentRotation;
                _hasLastHeadRotation = true;
                return 0f;
            }

            float deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            Quaternion delta = currentRotation * Quaternion.Inverse(_lastHeadLocalRotation);
            Vector3 deltaEuler = NormalizeEuler(delta.eulerAngles);
            Vector3 angularVelocity = deltaEuler / deltaTime;
            float angularSpeed = angularVelocity.magnitude;
            float targetActivity = Mathf.InverseLerp(
                _headMotionDeadZoneDegreesPerSecond,
                GetHeadMotionMaxSpeedForCurrentLevel(),
                angularSpeed);

            _motionActivity = Mathf.SmoothDamp(
                _motionActivity,
                targetActivity,
                ref _motionActivityVelocity,
                _motionActivitySmoothTime);

            UpdateHeadMotionDrag(angularVelocity, _motionActivity);
            _lastHeadLocalRotation = currentRotation;
            return _motionActivity;
        }

        private void UpdateHeadMotionDrag(Vector3 angularVelocity, float motionActivity)
        {
            if (!_enableHeadMotionDrag)
            {
                _motionDragEuler = Vector3.SmoothDamp(
                    _motionDragEuler,
                    Vector3.zero,
                    ref _motionDragVelocity,
                    GetMotionDragSmoothTimeForCurrentLevel());
                return;
            }

            float lagSeconds = GetMotionLagSecondsForCurrentLevel();
            float maxLag = GetMotionLagDegreesForCurrentLevel();
            Vector3 target = new Vector3(
                Mathf.Clamp(-angularVelocity.x * lagSeconds, -maxLag * 0.65f, maxLag * 0.65f),
                Mathf.Clamp(-angularVelocity.y * lagSeconds, -maxLag, maxLag),
                Mathf.Clamp(-angularVelocity.z * lagSeconds, -maxLag * 0.5f, maxLag * 0.5f));

            target *= motionActivity;
            _motionDragEuler = Vector3.SmoothDamp(
                _motionDragEuler,
                target,
                ref _motionDragVelocity,
                GetMotionDragSmoothTimeForCurrentLevel());
        }

        private void ResolveHeadTrackingReference()
        {
            if (_headTrackingReference != null)
            {
                return;
            }

            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null && rig.centerEyeAnchor != null)
            {
                _headTrackingReference = rig.centerEyeAnchor;
                return;
            }

            if (Camera.main != null)
            {
                _headTrackingReference = Camera.main.transform;
            }
        }

        private void EnsureCameraSwayOffsetOwnsHead()
        {
            Transform head = ResolveHeadTransform();
            if (head == null)
            {
                return;
            }

            if (_cameraSwayOffset != null && head.IsChildOf(_cameraSwayOffset))
            {
                _swayBaseLocalRotation = _cameraSwayOffset.localRotation;
                _headTrackingReference = head;
                return;
            }

            Transform originalParent = head.parent;
            if (originalParent == null)
            {
                return;
            }

            if (_cameraSwayOffset == null || _cameraSwayOffset == head || _cameraSwayOffset.IsChildOf(head))
            {
                _cameraSwayOffset = new GameObject("CameraSwayOffset").transform;
            }

            int siblingIndex = head.GetSiblingIndex();
            _cameraSwayOffset.name = "CameraSwayOffset";
            _cameraSwayOffset.SetParent(originalParent, false);
            _cameraSwayOffset.SetSiblingIndex(siblingIndex);
            _cameraSwayOffset.localPosition = Vector3.zero;
            _cameraSwayOffset.localRotation = Quaternion.identity;
            _cameraSwayOffset.localScale = Vector3.one;

            head.SetParent(_cameraSwayOffset, false);
            _swayBaseLocalRotation = _cameraSwayOffset.localRotation;
            _headTrackingReference = head;
        }

        private static Transform ResolveHeadTransform()
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null && rig.centerEyeAnchor != null)
            {
                return rig.centerEyeAnchor;
            }

            return Camera.main != null ? Camera.main.transform : null;
        }

        private void ResolvePassthroughLayer()
        {
            if (_passthroughLayer == null)
            {
                _passthroughLayer = FindAnyObjectByType<OVRPassthroughLayer>();
            }
        }

        private void ApplyPassthroughStyle(float motionActivity)
        {
            if (!_enablePassthroughStyleEffect)
            {
                return;
            }

            ResolvePassthroughLayer();
            if (_passthroughLayer == null)
            {
                return;
            }

            float intensity = Mathf.Clamp01(_alcoholLevel);
            float targetActivity = Mathf.Clamp01(
                (GetPassthroughBaseActivityForCurrentLevel() + motionActivity)
                * intensity
                * GetPassthroughStyleMultiplierForCurrentLevel());

            _passthroughStyleActivity = Mathf.SmoothDamp(
                _passthroughStyleActivity,
                targetActivity,
                ref _passthroughStyleVelocity,
                _passthroughStyleSmoothTime);

            if (_passthroughStyleActivity <= 0.01f)
            {
                _passthroughLayer.edgeRenderingEnabled = false;
                return;
            }

            _passthroughStyleHoldTimer -= Time.deltaTime;
            if (_passthroughStyleHoldTimer <= 0f)
            {
                float pulse = (Mathf.Sin(_simulationTime * GetPassthroughPulseFrequencyForCurrentLevel() * Mathf.PI * 2f) + 1f) * 0.5f;
                float pulseOffset = (pulse - 0.5f) * GetPassthroughPulseAmountForCurrentLevel() * _passthroughStyleActivity;
                _heldPassthroughBrightness = Mathf.Clamp(
                    GetPassthroughBrightnessForCurrentLevel() * _passthroughStyleActivity + pulseOffset,
                    -1f,
                    1f);
                _heldPassthroughContrast = Mathf.Clamp(
                    GetPassthroughContrastForCurrentLevel() * _passthroughStyleActivity,
                    -1f,
                    1f);
                _heldPassthroughSaturation = Mathf.Clamp(
                    GetPassthroughSaturationForCurrentLevel() * _passthroughStyleActivity,
                    -1f,
                    1f);
                _passthroughStyleHoldTimer = 1f / Mathf.Max(1f, GetMotionFrameHoldFpsForCurrentLevel());
            }

            _passthroughLayer.textureOpacity = 1f;
            _passthroughLayer.SetBrightnessContrastSaturation(
                _heldPassthroughBrightness,
                _heldPassthroughContrast,
                _heldPassthroughSaturation);
            _passthroughLayer.edgeRenderingEnabled = true;
            _passthroughLayer.edgeColor = new Color(
                0.35f,
                0.95f,
                1f,
                GetPassthroughEdgeAlphaForCurrentLevel() * _passthroughStyleActivity);
        }

        private void ResetPassthroughStyle()
        {
            ResolvePassthroughLayer();
            _passthroughStyleActivity = 0f;
            _passthroughStyleVelocity = 0f;
            _passthroughStyleHoldTimer = 0f;
            _heldPassthroughBrightness = 0f;
            _heldPassthroughContrast = 0f;
            _heldPassthroughSaturation = 0f;

            if (_passthroughLayer == null)
            {
                return;
            }

            _passthroughLayer.textureOpacity = 1f;
            _passthroughLayer.edgeRenderingEnabled = false;
            _passthroughLayer.edgeColor = Color.white;
            _passthroughLayer.DisableColorMap();
        }

        private void ForcePassthroughCameraTransparency()
        {
            var rig = FindAnyObjectByType<OVRCameraRig>();
            if (rig != null)
            {
                foreach (Camera cam in rig.GetComponentsInChildren<Camera>(true))
                {
                    ForcePassthroughCameraTransparency(cam);
                }
                RenderSettings.skybox = null;
                return;
            }

            ForcePassthroughCameraTransparency(Camera.main);
            RenderSettings.skybox = null;
        }

        private static void ForcePassthroughCameraTransparency(Camera cam)
        {
            if (cam == null)
            {
                return;
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);

            var urp = cam.GetComponent<UniversalAdditionalCameraData>();
            if (urp != null)
            {
                urp.renderPostProcessing = false;
            }
        }

        private void ResetHeadMotionState()
        {
            _motionActivity = 0f;
            _motionActivityVelocity = 0f;
            _motionDragEuler = Vector3.zero;
            _motionDragVelocity = Vector3.zero;
            _heldMotionDragEuler = Vector3.zero;
            _heldMotionDragVelocity = Vector3.zero;
            _headMotionHoldTimer = 0f;
            _hasLastHeadRotation = false;
        }

        private float GetStartLevelForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.25f,
                AlcoholEffectLevel.High => 0.85f,
                _ => Mathf.Max(_startLevel, 0.5f)
            };
        }

        private float GetTargetLevelForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.38f,
                AlcoholEffectLevel.High => 1f,
                _ => _targetLevel
            };
        }

        private float GetRampDurationForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => Mathf.Max(0.01f, _rampDurationSeconds * 1.35f),
                AlcoholEffectLevel.High => Mathf.Max(3f, _rampDurationSeconds * 0.2f),
                _ => Mathf.Max(0.01f, _rampDurationSeconds)
            };
        }

        private float GetVisualMultiplierForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.45f,
                AlcoholEffectLevel.High => 1.25f,
                _ => 0.8f
            };
        }

        private float GetSwayMultiplierForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.35f,
                AlcoholEffectLevel.High => 1.65f,
                _ => 0.9f
            };
        }

        private float GetHeadMotionMaxSpeedForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 85f,
                AlcoholEffectLevel.High => 25f,
                _ => 50f
            };
        }

        private float GetMotionLagSecondsForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.025f,
                AlcoholEffectLevel.High => 0.12f,
                _ => 0.065f
            };
        }

        private float GetMotionLagDegreesForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 3.0f,
                AlcoholEffectLevel.High => 16.0f,
                _ => 8.0f
            };
        }

        private float GetMotionDragSmoothTimeForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.055f,
                AlcoholEffectLevel.High => 0.16f,
                _ => 0.1f
            };
        }

        private float GetMotionStutterDegreesForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.18f,
                AlcoholEffectLevel.High => 2.2f,
                _ => 0.8f
            };
        }

        private float GetMotionStutterFrequencyForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 9f,
                AlcoholEffectLevel.High => 18f,
                _ => 13f
            };
        }

        private float GetMotionFrameHoldFpsForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 24f,
                AlcoholEffectLevel.High => 7f,
                _ => 12f
            };
        }

        private float GetMotionFrameJitterDegreesForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.18f,
                AlcoholEffectLevel.High => 1.35f,
                _ => 0.55f
            };
        }

        private float GetPassthroughStyleMultiplierForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.45f,
                AlcoholEffectLevel.High => 1.45f,
                _ => 0.85f
            };
        }

        private float GetPassthroughBaseActivityForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.02f,
                AlcoholEffectLevel.High => 0.16f,
                _ => 0.07f
            };
        }

        private float GetPassthroughBrightnessForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => -0.035f,
                AlcoholEffectLevel.High => -0.24f,
                _ => -0.1f
            };
        }

        private float GetPassthroughContrastForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.18f,
                AlcoholEffectLevel.High => 0.85f,
                _ => 0.42f
            };
        }

        private float GetPassthroughSaturationForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => -0.18f,
                AlcoholEffectLevel.High => -0.9f,
                _ => -0.48f
            };
        }

        private float GetPassthroughPulseFrequencyForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 1.8f,
                AlcoholEffectLevel.High => 6.5f,
                _ => 3.5f
            };
        }

        private float GetPassthroughPulseAmountForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.025f,
                AlcoholEffectLevel.High => 0.18f,
                _ => 0.08f
            };
        }

        private float GetPassthroughEdgeAlphaForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.2f,
                AlcoholEffectLevel.High => 0.85f,
                _ => 0.45f
            };
        }

        private float GetMotionBlurIntensityForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.24f,
                AlcoholEffectLevel.High => 0.95f,
                _ => 0.55f
            };
        }

        private float GetMotionBlurClampForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => 0.035f,
                AlcoholEffectLevel.High => 0.14f,
                _ => 0.08f
            };
        }

        private MotionBlurQuality GetMotionBlurQualityForCurrentLevel()
        {
            return _effectLevel switch
            {
                AlcoholEffectLevel.Low => MotionBlurQuality.Low,
                AlcoholEffectLevel.High => MotionBlurQuality.High,
                _ => MotionBlurQuality.Medium
            };
        }

        private static Vector3 NormalizeEuler(Vector3 euler)
        {
            return new Vector3(
                Mathf.DeltaAngle(0f, euler.x),
                Mathf.DeltaAngle(0f, euler.y),
                Mathf.DeltaAngle(0f, euler.z));
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

            if (_motionBlur != null)
            {
                _motionBlur.intensity.Override(0f);
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
            float startLevel = GetStartLevelForCurrentLevel();
            float targetLevel = GetTargetLevelForCurrentLevel();
            float rampDuration = GetRampDurationForCurrentLevel();
            while (elapsed < rampDuration && _simulationRunning)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rampDuration);
                _alcoholLevel = Mathf.Lerp(startLevel, targetLevel, t);
                yield return null;
            }

            if (_simulationRunning)
            {
                _alcoholLevel = targetLevel;
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
