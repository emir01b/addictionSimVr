# AlcoholSimVR — Meta Quest 3S MR Kurulum

Passthrough (Mixed Reality) modunda alkol bozulma simülasyonu. Tüm mantık `Assets/Scripts/` altındadır.

## Hızlı başlangıç

1. Unity 6 (6000.0.x) ile projeyi açın.
2. Menü: **AlcoholSimVR → 1 - Proje Ayarlarını Uygula (Passthrough + Eller)** — manifest için zorunlu.
3. `Assets/Scenes/MainScene.unity` açın → **AlcoholSimVR → 2 - Mevcut Sahneyi Onar (Passthrough + Eller)**.
4. **File → Build Settings → Android** — ARM64, IL2CPP, Vulkan; sahneyi build’e ekleyin.
5. Quest’te kontrolör takmayın; el takibi açık olmalı. Sol avuç size dönükken bilek menüsü açılır.

> İlk kurulum için **Kurulum Sihirbazı** da kullanılabilir; ardından mutlaka **Proje Ayarlarını Uygula** ve **Sahneyi Onar** çalıştırın.

## El takibi (kontrolörsüz)

- Proje manifest: `Hand Tracking Support = Hands Only`
- UI: sağ el **işaret parmağı pinch** ile tıklama (`OVRInputModule` + `OVRHand`)
- Menü: sol avuç kameraya dönükken (`dot > 0.6`) bilek paneli açılır
- Kontrolör modelleri sahnede gizlenir; takmanız gerekmez

## Passthrough kontrol listesi

| Ayar | Değer |
|------|--------|
| Kamera Clear Flags | Solid Color, alpha = 0 |
| OVRPassthroughLayer | Underlay |
| OVRManager | Insight Passthrough enabled |
| Skybox | Yok |

`PassthroughBootstrap` sahne yüklendiğinde bunları uygular.

## Durum makinesi (`AppManager`)

| Durum | Geçiş |
|--------|--------|
| Idle | Başlangıç |
| MenuOpen | Sol avuç kameraya dönük (dot > 0.6) |
| InfoPanel | "Düz Tahta Yürüme" |
| SimulationActive | "Başlat" |
| ResultsScreen | Oturum sonu / süre dolumu |
| → Idle | Geri tuşu uzun basış (~1 sn) |

## Kamera sallanması

**OVRCameraRig taşınmaz.** `CameraSwayOffset` child objesi üzerinde ek rotasyon uygulanır (`AlcoholEffectController`).

## Test modülleri

- **Düz Tahta Yürüme** — aktif
- Bilişsel / Denge — kilitli (placeholder)

## Inspector ayarları

Tüm eşikler `[SerializeField]` ile ayarlanabilir: avuç dot eşiği, alkol ramp süresi, post-process yoğunlukları, tahta boyutu, simülasyon süresi.

## Gerçek tahta ile kullanım

Fiziksel dar tahtayı zemine yerleştirin. Uygulama MR overlay tahta + görsel bozulma sağlar; kullanıcı gerçek ortamda yürür.

## Sorun giderme

- El takibi yoksa `HandPalmDetector` için `LeftHandFallbackAnchor` atayın.
- UI tıklanmıyorsa sahnede `EventSystem` + `OVRInputModule` + `OVRRaycaster` olduğunu doğrulayın.
- Post-process görünmüyorsa URP Renderer’da post-processing açık olsun ve `AlcoholPostProcessVolume` global volume atanmış olsun.
