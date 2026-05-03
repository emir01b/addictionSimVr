using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace AddictionSim
{
    /// <summary>
    /// Sahnedeki tüm World Space Canvas'lara XR etkileşim desteği ekler.
    /// 
    /// Sorun: Standart GraphicRaycaster, XR hand tracking ile çalışmaz.
    /// Çözüm: TrackedDeviceGraphicRaycaster kullanılmalı.
    /// 
    /// Bu script tüm Canvas'ları tarar ve:
    /// 1. GraphicRaycaster varsa kaldırır
    /// 2. TrackedDeviceGraphicRaycaster ekler
    /// 
    /// Ayrıca XR UI Input Module'ün sahnede var olduğundan emin olur.
    /// </summary>
    [DefaultExecutionOrder(-50)] // PassthroughSetup'tan sonra, diğerlerinden önce
    public class XRCanvasSetup : MonoBehaviour
    {
        [Header("Ayarlar")]
        [Tooltip("Otomatik olarak tüm Canvas'ları XR uyumlu yap")]
        [SerializeField] private bool autoSetup = true;

        [Tooltip("Sadece World Space canvas'ları dönüştür")]
        [SerializeField] private bool onlyWorldSpace = true;

        private void Start()
        {
            if (!autoSetup) return;

            SetupAllCanvases();
            EnsureXRUIInputModule();
        }

        /// <summary>
        /// Sahnedeki tüm Canvas'ları XR interaction uyumlu hale getirir.
        /// </summary>
        private void SetupAllCanvases()
        {
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int convertedCount = 0;

            foreach (var canvas in allCanvases)
            {
                if (onlyWorldSpace && canvas.renderMode != RenderMode.WorldSpace)
                    continue;

                // Zaten TrackedDeviceGraphicRaycaster varsa atla
                if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() != null)
                    continue;

                // Standart GraphicRaycaster'ı kaldır
                var graphicRaycaster = canvas.GetComponent<GraphicRaycaster>();
                if (graphicRaycaster != null)
                {
                    DestroyImmediate(graphicRaycaster);
                }

                // TrackedDeviceGraphicRaycaster ekle (XR hand/controller interaction desteği)
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                convertedCount++;

                Debug.Log($"[XRCanvasSetup] '{canvas.name}' canvas'ına TrackedDeviceGraphicRaycaster eklendi.");
            }

            if (convertedCount > 0)
            {
                Debug.Log($"[XRCanvasSetup] Toplam {convertedCount} canvas XR uyumlu hale getirildi.");
            }
        }

        /// <summary>
        /// Sahnede XRUIInputModule olduğundan emin olur.
        /// Bu modül olmadan XR UI etkileşimleri çalışmaz.
        /// </summary>
        private void EnsureXRUIInputModule()
        {
            // XRUIInputModule zaten var mı kontrol et
            var existingModule = FindAnyObjectByType<XRUIInputModule>();
            if (existingModule != null)
            {
                Debug.Log("[XRCanvasSetup] XRUIInputModule zaten mevcut.");
                return;
            }

            // MR Interaction Setup prefab'ı varsa, XRUIInputModule zaten içinde olmalı
            // Yoksa EventSystem üzerinde oluştur
            var eventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem != null)
            {
                // Standart input module'ü kaldır
                var standaloneInput = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (standaloneInput != null)
                {
                    DestroyImmediate(standaloneInput);
                }

                // XR UI Input Module ekle
                if (eventSystem.GetComponent<XRUIInputModule>() == null)
                {
                    eventSystem.gameObject.AddComponent<XRUIInputModule>();
                    Debug.Log("[XRCanvasSetup] EventSystem'e XRUIInputModule eklendi.");
                }
            }
            else
            {
                Debug.LogWarning("[XRCanvasSetup] EventSystem bulunamadı! MR Interaction Setup prefab'ının sahnede olduğundan emin olun.");
            }
        }
    }
}
