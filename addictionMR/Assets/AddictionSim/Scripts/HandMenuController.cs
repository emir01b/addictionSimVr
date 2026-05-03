using UnityEngine;
using UnityEngine.XR.Hands;
using System.Collections.Generic;

namespace AddictionSim
{
    /// <summary>
    /// Sol elin avuç içi kullanıcıya baktığında (palm-up) bir UI panelini
    /// gösterip/gizleyen controller. XR Hands subsystem kullanılır.
    ///
    /// Çalışma prensibi:
    /// 1. XRHandSubsystem üzerinden sol elin bilek (wrist) kemik pozisyonunu alır
    /// 2. Avuç normal vektörü ile kullanıcının baş yönü arasındaki açıyı hesaplar
    /// 3. Açı eşik değerinin altındaysa paneli gösterir
    /// 4. Panel, elin konumuna ve yönüne göre pozisyonlanır
    /// </summary>
    public class HandMenuController : MonoBehaviour
    {
        [Header("Panel Referansı")]
        [Tooltip("Gösterilecek/gizlenecek hand menu paneli (Canvas)")]
        [SerializeField] private GameObject handMenuPanel;

        [Header("Algılama Ayarları")]
        [Tooltip("Avuç-kamera açısı bu değerin altında olmalı (derece)")]
        [SerializeField] private float palmAngleThreshold = 60f;

        [Tooltip("Panel gösterme/gizleme smooth geçiş süresi")]
        [SerializeField] private float smoothTime = 0.15f;

        [Tooltip("Panelin ele olan mesafesi (metre)")]
        [SerializeField] private float panelOffset = 0.12f;

        [Tooltip("Panelin el üstünde yükseklik ofseti (metre)")]
        [SerializeField] private float panelHeightOffset = 0.08f;

        [Header("Stabilizasyon")]
        [Tooltip("Pozisyon stabilizasyonu için lerp hızı")]
        [SerializeField] private float positionLerpSpeed = 12f;

        [Tooltip("Rotasyon stabilizasyonu için lerp hızı")]
        [SerializeField] private float rotationLerpSpeed = 8f;

        [Tooltip("El algılama kaybolduğunda panelin gizlenmesi için bekleme süresi (saniye)")]
        [SerializeField] private float hideDelay = 0.5f;

        // XR Hands
        private XRHandSubsystem handSubsystem;
        private static List<XRHandSubsystem> s_HandSubsystems = new List<XRHandSubsystem>();

        // Panel durumu
        private bool isPanelVisible;
        private float currentAlpha;
        private float targetAlpha;
        private float alphaVelocity;
        private float hideTimer;

        // Pozisyon takibi
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool hasValidPose;

        // Canvas Group (alpha kontrolü için)
        private CanvasGroup canvasGroup;

        private Camera mainCamera;

        private void Start()
        {
            mainCamera = Camera.main;

            if (handMenuPanel != null)
            {
                canvasGroup = handMenuPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = handMenuPanel.AddComponent<CanvasGroup>();
                }

                // Başlangıçta gizle
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                isPanelVisible = false;
            }
        }

        private void OnEnable()
        {
            TryGetHandSubsystem();
        }

        private void Update()
        {
            // Subsystem yoksa tekrar dene
            if (handSubsystem == null || !handSubsystem.running)
            {
                TryGetHandSubsystem();
                if (handSubsystem == null) return;
            }

            // Sol eli kontrol et
            var leftHand = handSubsystem.leftHand;
            if (leftHand.isTracked)
            {
                EvaluatePalmOrientation(leftHand);
            }
            else
            {
                // El takibi kaybedildi
                HandleHandLost();
            }

            // Panel alpha animasyonu
            UpdatePanelVisibility();

            // Panel pozisyon/rotasyon güncelle
            if (hasValidPose && handMenuPanel != null)
            {
                handMenuPanel.transform.position = Vector3.Lerp(
                    handMenuPanel.transform.position,
                    targetPosition,
                    Time.deltaTime * positionLerpSpeed
                );

                handMenuPanel.transform.rotation = Quaternion.Slerp(
                    handMenuPanel.transform.rotation,
                    targetRotation,
                    Time.deltaTime * rotationLerpSpeed
                );
            }
        }

        /// <summary>
        /// Sol elin avuç yönünü değerlendirir ve paneli gösterip göstermeyeceğine karar verir.
        /// </summary>
        private void EvaluatePalmOrientation(XRHand hand)
        {
            // Bilek (wrist) kemik pozisyonunu al
            var wristJoint = hand.GetJoint(XRHandJointID.Wrist);
            var middleProximal = hand.GetJoint(XRHandJointID.MiddleProximal);

            if (!wristJoint.TryGetPose(out Pose wristPose) ||
                !middleProximal.TryGetPose(out Pose middlePose))
            {
                HandleHandLost();
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            // Avuç normalini hesapla (bilek pozisyonunun yukarı yönü)
            // Sol el için: up vektörü avuç içine bakar
            Vector3 palmNormal = -wristPose.up;

            // Kameraya doğru yön
            Vector3 directionToCamera = (mainCamera.transform.position - wristPose.position).normalized;

            // Açıyı hesapla
            float angle = Vector3.Angle(palmNormal, directionToCamera);

            // Palm kullanıcıya bakıyorsa paneli göster
            bool shouldShow = angle < palmAngleThreshold;

            if (shouldShow)
            {
                hideTimer = hideDelay;
                ShowPanel();

                // Panel pozisyonunu hesapla - avuç üstünde, hafif yukarıda
                Vector3 palmCenter = Vector3.Lerp(wristPose.position, middlePose.position, 0.5f);
                targetPosition = palmCenter + (palmNormal * panelOffset) + (Vector3.up * panelHeightOffset);

                // Panel kullanıcıya baksın
                Vector3 lookDirection = mainCamera.transform.position - targetPosition;
                lookDirection.y *= 0.3f; // Dikey açıyı azalt, daha rahat okunabilsin
                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    targetRotation = Quaternion.LookRotation(-lookDirection.normalized, Vector3.up);
                }

                hasValidPose = true;
            }
            else
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0f)
                {
                    HidePanel();
                }
            }
        }

        private void HandleHandLost()
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0f)
            {
                HidePanel();
            }
        }

        private void ShowPanel()
        {
            if (!isPanelVisible)
            {
                isPanelVisible = true;
                targetAlpha = 1f;
            }
        }

        private void HidePanel()
        {
            if (isPanelVisible)
            {
                isPanelVisible = false;
                targetAlpha = 0f;
                hasValidPose = false;
            }
        }

        private void UpdatePanelVisibility()
        {
            if (canvasGroup == null) return;

            currentAlpha = Mathf.SmoothDamp(currentAlpha, targetAlpha, ref alphaVelocity, smoothTime);
            canvasGroup.alpha = currentAlpha;

            // Etkileşim kontrolü
            bool isInteractable = currentAlpha > 0.5f;
            canvasGroup.interactable = isInteractable;
            canvasGroup.blocksRaycasts = isInteractable;
        }

        private void TryGetHandSubsystem()
        {
            s_HandSubsystems.Clear();
            SubsystemManager.GetSubsystems(s_HandSubsystems);

            foreach (var subsystem in s_HandSubsystems)
            {
                if (subsystem.running)
                {
                    handSubsystem = subsystem;
                    return;
                }
            }

            if (s_HandSubsystems.Count > 0)
            {
                handSubsystem = s_HandSubsystems[0];
            }
        }
    }
}
