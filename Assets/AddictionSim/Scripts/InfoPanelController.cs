using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AddictionSim
{
    /// <summary>
    /// Kullanıcının karşısında beliren bilgilendirme paneli.
    /// Bağımlılık butonuna tıklandığında açılır, ilgili bağımlılık hakkında
    /// detaylı bilgi verir ve altında simülasyonu başlatma butonu bulunur.
    /// 
    /// Panel, kullanıcının kamerasının önünde (1.2m mesafede) konumlanır
    /// ve her zaman kullanıcıya bakar.
    /// </summary>
    public class InfoPanelController : MonoBehaviour
    {
        [Header("Panel Referansları")]
        [Tooltip("Bilgilendirme panelinin ana Canvas objesi")]
        [SerializeField] private GameObject infoPanelCanvas;

        [Header("İçerik Referansları")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI warningText;
        [SerializeField] private Image iconImage;

        [Header("Butonlar")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button closeButton;

        [Header("İkon Sprite'ları")]
        [SerializeField] private Sprite cigaretteSprite;
        [SerializeField] private Sprite alcoholSprite;
        [SerializeField] private Sprite drugSprite;

        [Header("Pozisyonlama")]
        [Tooltip("Panelin kameradan uzaklığı (metre)")]
        [SerializeField] private float distanceFromCamera = 1.2f;

        [Tooltip("Panelin kamera yüksekliğinden farkı (metre)")]
        [SerializeField] private float heightOffset = -0.1f;

        [Tooltip("Pozisyon/rotasyon smooth takip hızı")]
        [SerializeField] private float followSpeed = 5f;

        [Header("Animasyon")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.2f;

        // Singleton
        public static InfoPanelController Instance { get; private set; }

        // Durum
        private SimulationManager.AddictionType pendingAddiction;
        private CanvasGroup canvasGroup;
        private bool isVisible;
        private float fadeProgress;
        private bool isFadingIn;
        private bool isFadingOut;

        // Pozisyon hedefi
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool hasTargetPose;

        private Camera mainCamera;

        // === Bağımlılık Bilgileri ===

        private static readonly string CigaretteTitle = "Sigara Bağımlılığı";
        private static readonly string CigaretteDescription =
            "Sigara, içerdiği nikotin maddesi nedeniyle güçlü bir bağımlılık oluşturur. " +
            "Nikotin, beyindeki ödül merkezini uyararak kısa süreli bir rahatlama hissi verir, " +
            "ancak bu etki hızla geçer ve daha fazlasına ihtiyaç duyulur.\n\n" +
            "Bu simülasyonda sigara kullanımının görsel algı üzerindeki etkilerini " +
            "deneyimleyeceksiniz: bulanıklaşma, renk solması ve odaklanma güçlüğü.";
        private static readonly string CigaretteWarning =
            "⚠ Sigara kullanımı akciğer kanseri, kalp hastalıkları ve felç riskini artırır.";

        private static readonly string AlcoholTitle = "Alkol Bağımlılığı";
        private static readonly string AlcoholDescription =
            "Alkol, merkezi sinir sistemini baskılayan bir maddedir. Düşük dozlarda " +
            "rahatlama ve sosyal rahatlık sağlarken, yüksek dozlarda karar verme, " +
            "denge ve koordinasyon bozukluklarına yol açar.\n\n" +
            "Bu simülasyonda alkol etkisi altındaki görsel bozulmaları deneyimleyeceksiniz: " +
            "bulanık görüş, gecikmiş tepkiler, renk kaymaları ve denge kaybı.";
        private static readonly string AlcoholWarning =
            "⚠ Aşırı alkol tüketimi karaciğer sirozu, beyin hasarı ve ölüme yol açabilir.";

        private static readonly string DrugTitle = "Uyuşturucu Bağımlılığı";
        private static readonly string DrugDescription =
            "Uyuşturucu maddeler, beynin kimyasal dengesini ciddi şekilde bozar. " +
            "İlk kullanımda bile güçlü bağımlılık oluşturabilir. Kullanıcının gerçeklik " +
            "algısını, zaman kavramını ve duygusal durumunu derinden etkiler.\n\n" +
            "Bu simülasyonda uyuşturucu etkisi altındaki algı bozulmalarını deneyimleyeceksiniz: " +
            "halüsinasyonlar, şiddetli renk değişimleri, zaman bozulması ve yoğun baş dönmesi.";
        private static readonly string DrugWarning =
            "⚠ Uyuşturucu kullanımı organ yetmezliği, psikoz ve ani ölüme neden olabilir.";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            mainCamera = Camera.main;

            if (infoPanelCanvas != null)
            {
                canvasGroup = infoPanelCanvas.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = infoPanelCanvas.AddComponent<CanvasGroup>();

                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                infoPanelCanvas.SetActive(false);
            }

            // Buton listener'ları
            if (startButton != null)
                startButton.onClick.AddListener(OnStartClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void Update()
        {
            // Fade animasyonları
            if (isFadingIn)
            {
                fadeProgress += Time.deltaTime / fadeInDuration;
                if (fadeProgress >= 1f)
                {
                    fadeProgress = 1f;
                    isFadingIn = false;
                }
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, fadeProgress);
                    canvasGroup.interactable = canvasGroup.alpha > 0.5f;
                    canvasGroup.blocksRaycasts = canvasGroup.alpha > 0.5f;
                }
            }
            else if (isFadingOut)
            {
                fadeProgress -= Time.deltaTime / fadeOutDuration;
                if (fadeProgress <= 0f)
                {
                    fadeProgress = 0f;
                    isFadingOut = false;
                    isVisible = false;
                    if (infoPanelCanvas != null)
                        infoPanelCanvas.SetActive(false);
                }
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, fadeProgress);
                    canvasGroup.interactable = false;
                    canvasGroup.blocksRaycasts = false;
                }
            }

            // Panel pozisyonlama (kullanıcının karşısında, sabit)
            if (isVisible && hasTargetPose && infoPanelCanvas != null)
            {
                infoPanelCanvas.transform.position = Vector3.Lerp(
                    infoPanelCanvas.transform.position,
                    targetPosition,
                    Time.deltaTime * followSpeed
                );
                infoPanelCanvas.transform.rotation = Quaternion.Slerp(
                    infoPanelCanvas.transform.rotation,
                    targetRotation,
                    Time.deltaTime * followSpeed
                );
            }
        }

        /// <summary>
        /// Belirtilen bağımlılık tipi için bilgilendirme panelini açar.
        /// Panel, kullanıcının karşısında konumlanır.
        /// </summary>
        public void ShowInfoPanel(SimulationManager.AddictionType type)
        {
            if (type == SimulationManager.AddictionType.None) return;

            pendingAddiction = type;

            // İçeriği ayarla
            SetContent(type);

            // Paneli kullanıcının karşısına konumla
            PositionInFrontOfUser();

            // Göster
            if (infoPanelCanvas != null)
            {
                infoPanelCanvas.SetActive(true);
                isVisible = true;
                fadeProgress = 0f;
                isFadingIn = true;
                isFadingOut = false;
            }

            Debug.Log($"[InfoPanel] {type} bilgilendirme paneli açıldı.");
        }

        /// <summary>
        /// Paneli kapatır.
        /// </summary>
        public void HideInfoPanel()
        {
            if (!isVisible) return;

            isFadingOut = true;
            isFadingIn = false;
            hasTargetPose = false;

            Debug.Log("[InfoPanel] Bilgilendirme paneli kapatılıyor.");
        }

        private void SetContent(SimulationManager.AddictionType type)
        {
            switch (type)
            {
                case SimulationManager.AddictionType.Cigarette:
                    if (titleText != null) titleText.text = CigaretteTitle;
                    if (descriptionText != null) descriptionText.text = CigaretteDescription;
                    if (warningText != null) warningText.text = CigaretteWarning;
                    if (iconImage != null && cigaretteSprite != null) iconImage.sprite = cigaretteSprite;
                    break;

                case SimulationManager.AddictionType.Alcohol:
                    if (titleText != null) titleText.text = AlcoholTitle;
                    if (descriptionText != null) descriptionText.text = AlcoholDescription;
                    if (warningText != null) warningText.text = AlcoholWarning;
                    if (iconImage != null && alcoholSprite != null) iconImage.sprite = alcoholSprite;
                    break;

                case SimulationManager.AddictionType.Drug:
                    if (titleText != null) titleText.text = DrugTitle;
                    if (descriptionText != null) descriptionText.text = DrugDescription;
                    if (warningText != null) warningText.text = DrugWarning;
                    if (iconImage != null && drugSprite != null) iconImage.sprite = drugSprite;
                    break;
            }
        }

        private void PositionInFrontOfUser()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }

            // Kameranın baktığı yönde, belirtilen mesafede konumla
            Vector3 cameraForward = mainCamera.transform.forward;
            cameraForward.y = 0; // Sadece yatay düzlemde
            cameraForward.Normalize();

            targetPosition = mainCamera.transform.position
                + cameraForward * distanceFromCamera
                + Vector3.up * heightOffset;

            // Panel kullanıcıya baksın
            Vector3 lookDir = mainCamera.transform.position - targetPosition;
            lookDir.y = 0; // Dikey bileşeni kaldır, panel dik dursun
            if (lookDir.sqrMagnitude > 0.001f)
            {
                targetRotation = Quaternion.LookRotation(-lookDir.normalized, Vector3.up);
            }

            hasTargetPose = true;

            // İlk pozisyonu hemen ayarla (smooth takip sonra devam edecek)
            if (infoPanelCanvas != null)
            {
                infoPanelCanvas.transform.position = targetPosition;
                infoPanelCanvas.transform.rotation = targetRotation;
            }
        }

        // === Buton Callback'leri ===

        private void OnStartClicked()
        {
            var sim = SimulationManager.Instance;
            if (sim != null)
            {
                sim.StartSimulation(pendingAddiction);
            }

            HideInfoPanel();
            Debug.Log($"[InfoPanel] {pendingAddiction} simülasyonu başlatıldı.");
        }

        private void OnCloseClicked()
        {
            HideInfoPanel();
        }
    }
}
