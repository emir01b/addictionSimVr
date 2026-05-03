# 🧠 Bağımlılıkla Mücadele MR

**Meta Quest 3S** üzerinde çalışan, karma gerçeklik (Mixed Reality) tabanlı bir bağımlılık farkındalık simülasyonu.

Kullanıcı gerçek dünyayı passthrough kamera ile görürken, sigara, alkol ve uyuşturucu bağımlılıklarının insan algısı üzerindeki etkilerini (bulanıklaşma, renk kayması, baş dönmesi, gecikme) simüle eder.

![Unity](https://img.shields.io/badge/Unity-6000.3.10f1%20LTS-000000?style=for-the-badge&logo=unity)
![Meta Quest](https://img.shields.io/badge/Meta%20Quest%203S-0467DF?style=for-the-badge&logo=meta)
![Platform](https://img.shields.io/badge/Platform-Android%20(Quest)-3DDC84?style=for-the-badge&logo=android)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

---

## 📋 İçindekiler

- [Özellikler](#-özellikler)
- [Mimari](#-mimari)
- [Gereksinimler](#-gereksinimler)
- [Kurulum](#-kurulum)
- [Proje Yapısı](#-proje-yapısı)
- [Kullanım](#-kullanım)
- [Teknik Detaylar](#-teknik-detaylar)
- [Geliştirme Yol Haritası](#-geliştirme-yol-haritası)
- [Katkıda Bulunma](#-katkıda-bulunma)
- [Lisans](#-lisans)

---

## ✨ Özellikler

### 🖐️ El Takibi ile Etkileşim
- **Palm-Up Menü:** Sol elin avuç içi kullanıcıya baktığında otomatik açılan kontrol paneli
- **Poke Etkileşimi:** Sağ el işaret parmağıyla butonlara dokunarak senaryo seçimi
- XR Interaction SDK tabanlı el takibi (controller gerekmez)

### 🌍 Karma Gerçeklik (MR)
- **Passthrough Kamera:** Gerçek dünya Meta Quest 3S kameraları üzerinden görülür
- Sanal sahne yok — tüm efektler gerçek dünya üzerine uygulanır
- OpenXR + Meta XR SDK entegrasyonu

### 📊 Bağımlılık Senaryoları
| Senaryo | Simüle Edilen Etkiler |
|---------|----------------------|
| 🚬 **Sigara** | Bulanıklaşma, renk solması, odaklanma güçlüğü |
| 🍺 **Alkol** | Gecikmiş tepkiler, denge kaybı, renk kayması |
| 💊 **Uyuşturucu** | Halüsinasyonlar, zaman bozulması, şiddetli baş dönmesi |

### 📖 Bilgilendirme Paneli
- Her bağımlılık tipi için detaylı Türkçe açıklama
- Sağlık uyarıları
- Simülasyonu başlatmadan önce bilgi edinme imkânı

---

## 🏗️ Mimari

```
┌─────────────────────────────────────────────────────┐
│                   META QUEST 3S                      │
│                                                      │
│  ┌──────────────┐    ┌─────────────────────────┐    │
│  │  XR Hand     │    │   MR Interaction Setup   │    │
│  │  Subsystem   │    │   (Passthrough + XR      │    │
│  │              │    │    Origin + Hands)        │    │
│  └──────┬───────┘    └─────────────────────────┘    │
│         │                                            │
│  ┌──────▼───────┐    ┌─────────────────────────┐    │
│  │ HandMenu     │───▶│     HandMenuUI          │    │
│  │ Controller   │    │  (Buton yönetimi)        │    │
│  │ (Palm algı)  │    └──────────┬──────────────┘    │
│  └──────────────┘               │                    │
│                          ┌──────▼──────────────┐    │
│                          │  InfoPanelController │    │
│                          │  (Bilgilendirme)     │    │
│                          └──────────┬──────────┘    │
│                                     │                │
│                          ┌──────────▼──────────┐    │
│                          │ SimulationManager    │    │
│                          │ (Senaryo durumu)     │    │
│                          └──────────┬──────────┘    │
│                                     │                │
│                          ┌──────────▼──────────┐    │
│                          │   Efekt Sistemi      │    │
│                          │   (İleride)          │    │
│                          └─────────────────────┘    │
└─────────────────────────────────────────────────────┘
```

---

## 📦 Gereksinimler

### Donanım
- **Meta Quest 3S** (veya Quest 3 / Quest Pro)
- USB-C kablosu (geliştirme için)

### Yazılım
| Araç | Sürüm |
|------|-------|
| Unity | **6000.3.10f1 LTS** |
| Meta XR SDK | 201.0.0 |
| XR Hands | 1.7.3 |
| XR Interaction Toolkit | 3.4.1 |
| OpenXR Plugin | Güncel |
| Meta Quest Developer Hub | Güncel |

---

## 🚀 Kurulum

### 1. Repoyu Klonla
```bash
git clone https://github.com/KULLANICI_ADI/addiction-sim-mr.git
cd addiction-sim-mr
```

### 2. Unity'de Aç
1. **Unity Hub** → **Open** → klonlanan klasörü seç
2. Unity **6000.3.10f1 LTS** sürümüyle aç
3. İlk açılışta paketlerin yüklenmesini bekle (~5-10 dakika)

### 3. Platform Ayarı
1. `File > Build Settings`
2. **Android** platformunu seç → **Switch Platform**
3. **Texture Compression** → `ASTC`

### 4. Quest Bağlantısı
1. Quest'te **Geliştirici Modu**'nu etkinleştir
2. USB-C ile bilgisayara bağla
3. Quest'te **USB Debugging**'i kabul et

### 5. Build & Run
1. `File > Build and Run`
2. Quest'inizi cihaz olarak seçin
3. APK otomatik olarak yüklenir ve çalışır

---

## 📁 Proje Yapısı

```
Assets/
├── AddictionSim/                    # Ana proje dosyaları
│   ├── Scripts/
│   │   ├── SimulationManager.cs     # Senaryo durum yönetimi (Singleton)
│   │   ├── HandMenuController.cs    # Sol el palm-up algılama
│   │   ├── HandMenuUI.cs            # El menüsü buton/metin yönetimi
│   │   ├── HandMenuSetup.cs         # Editor: otomatik UI oluşturma
│   │   ├── InfoPanelController.cs   # Bilgilendirme paneli (karşıda açılır)
│   │   ├── PassthroughSetup.cs      # Runtime passthrough konfigürasyonu
│   │   └── XRCanvasSetup.cs         # Canvas'ları XR uyumlu yapma
│   ├── Icons/
│   │   ├── icon_cigarette.png       # Sigara simgesi
│   │   ├── icon_alcohol.png         # Alkol simgesi
│   │   ├── icon_drugs.png           # Uyuşturucu simgesi
│   │   └── icon_stop.png            # Durdurma simgesi
│   ├── Materials/                   # Materyaller
│   └── Prefabs/                     # Prefab'lar
│
├── MRTemplateAssets/                # Meta MR Template dosyaları
├── Oculus/                          # Oculus SDK konfigürasyonu
├── Scenes/
│   └── SampleScene.unity            # Ana sahne
├── Settings/                        # URP ve render ayarları
└── XR/                              # OpenXR konfigürasyonu
```

---

## 🎮 Kullanım

### Temel Akış
1. **Uygulamayı başlat** → Gerçek dünyayı passthrough ile gör
2. **Sol elinin avuç içini kendine çevir** → Kontrol paneli belirir
3. **Sağ elinle bir bağımlılık butonuna dokun** (Sigara / Alkol / Uyuşturucu)
4. **Bilgilendirme paneli açılır** → Bağımlılık hakkında bilgi oku
5. **"Simülasyonu Başlat"** butonuna dokun → Efektler başlar
6. **"Durdur"** butonu ile simülasyonu sonlandır

### Kontroller
| Hareket | İşlem |
|---------|-------|
| Sol el avuç açma | Menü panelini göster/gizle |
| Sağ el işaret parmağı | Butonlara dokunma (poke) |
| Menü butonları | Senaryo seçimi |
| Durdur butonu | Aktif simülasyonu sonlandır |

---

## 🔧 Teknik Detaylar

### El Takibi (Hand Tracking)
- **XR Hand Subsystem** üzerinden sol elin bilek (wrist) kemik pozisyonu okunur
- Avuç normal vektörü ile kamera yönü arasındaki açı hesaplanır
- Açı `palmAngleThreshold` (varsayılan: 60°) altında ise panel gösterilir
- Panel pozisyonu `SmoothDamp` ve `Lerp` ile stabilize edilir

### Passthrough (Karma Gerçeklik)
- **Meta Insight Passthrough** teknolojisi kullanılır
- Kamera arka planı şeffaf (`CameraClearFlags.SolidColor`, alpha=0)
- Skybox materyali kaldırılmıştır
- `OculusProjectConfig` → `insightPassthroughEnabled: true`

### UI Etkileşimi
- World Space Canvas'lar kullanılır
- `TrackedDeviceGraphicRaycaster` ile XR hand/controller desteği
- `XRUIInputModule` üzerinden input yönlendirmesi

### Bilgilendirme Paneli
- Kameranın 1.2m önünde konumlanır
- Her zaman kullanıcıya bakar (billboard)
- Fade in/out animasyonları ile açılır/kapanır
- Her bağımlılık tipi için Türkçe detaylı açıklama ve sağlık uyarısı

---

## 🗺️ Geliştirme Yol Haritası

- [x] Proje altyapısı ve MR ortamı
- [x] El takibi ile palm-up menü sistemi
- [x] Bağımlılık senaryo butonları ve UI
- [x] Bilgilendirme paneli (açıklama + başlat)
- [x] Passthrough konfigürasyonu
- [x] XR Canvas etkileşim düzeltmeleri
- [ ] **Sigara senaryosu efektleri** (bulanıklaşma, renk solması)
- [ ] **Alkol senaryosu efektleri** (gecikme, denge kaybı)
- [ ] **Uyuşturucu senaryosu efektleri** (halüsinasyon, zaman bozulması)
- [ ] Post-processing efekt zinciri
- [ ] Ses efektleri (kalp atışı, nefes darlığı)
- [ ] İstatistik ve sonuç ekranı
- [ ] Çoklu dil desteği

---

## 🤝 Katkıda Bulunma

1. Bu repoyu **fork** edin
2. Yeni bir **feature branch** oluşturun (`git checkout -b feature/yeni-ozellik`)
3. Değişikliklerinizi **commit** edin (`git commit -m 'feat: yeni özellik eklendi'`)
4. Branch'inizi **push** edin (`git push origin feature/yeni-ozellik`)
5. Bir **Pull Request** açın

### Commit Mesajı Kuralları
```
feat: yeni özellik
fix: hata düzeltmesi
docs: dokümantasyon
style: kod formatlama
refactor: kod yeniden yapılandırma
test: test ekleme
chore: araç/konfigürasyon değişikliği
```

---

## 📄 Lisans

Bu proje [MIT Lisansı](LICENSE) altında lisanslanmıştır.

---

## 👥 Ekip

| Rol | Kişi |
|-----|------|
| Geliştirici | [Emirhan] |

---

<p align="center">
  <b>Bağımlılıkla Mücadele MR</b> — Farkındalık için teknoloji 🧠
</p>
