# Eyes Off!

Karanlık bir odada iki oyunculu FPS prototipi. Nesneler sadece yaklaşınca beyaz outline ile görünür.
Biri ateş edince 3 saniyelik **Panic Time** başlar ve ateş eden donar. Ardından ışıklar 5 saniye yanar.

**Unity 6 (6000.6.6f1) · URP · Netcode for GameObjects · Animation Rigging · Multiplayer Play Mode**

## Çalıştırma
- **Editör:** `Assets/_Project/Scenes/Game.unity` sahnesini aç. Multiplayer Play Mode'da Player 2'yi aktif et ve Play'e bas.
  Bir pencerede Host yap, diğerinde `127.0.0.1` ile Join yap.
- **İki bilgisayar:** Menüde host'un IP adresi yazar, diğer bilgisayar bu IP ile Join yapar (port 7777).
- **Kontroller:** WASD, mouse, sol tık.

## Akış
```
Bekleme ──2 oyuncu──▶ Karanlık ──ateş──▶ Panic (3 sn) ──▶ Aydınlık (5 sn) ──▶ Karanlık ... ──süre biter──▶ Sonuç
```
Süreler `Assets/_Project/Settings/GameSettings.asset` (ScriptableObject) dosyasından değiştirilir.

## Kararlar

### Outline: Ekran uzayında kenar tespiti
URP'nin Full Screen Pass'i ve özel bir shader kullanılıyor. Shader, kameranın derinlik ve normal texture'larında
ani değişim olan pikselleri beyaza boyuyor. Çizgiler mesafeye göre sönüyor.
- Duvar arkasındaki nesneler kendiliğinden gizli, çünkü derinlik texture'ında sadece en öndeki yüzey var.
- Nesne başına kurulum yok. Yeni eklenen her şey otomatik outline alıyor.

| Elenen yöntem | Neden |
|---|---|
| Inverted hull | Her objeye ek materyal gerekiyor, keskin köşelerde boşluk oluşuyor |
| Stencil / Render Objects | Layer ataması ve duvar arkası için ek derinlik testi gerekiyor |
| Hazır outline asset'i | Nesne başına component gerekiyor, duvar arkası görünüyor |

### Ağ
- **Hareket owner-authoritative.** Case hile korumasını ve lag kompanzasyonunu istemediği için en basit çözüm seçildi.
- **Fazı sadece sunucu değiştiriyor.** Süreler bitiş zamanı olarak (`ServerTime`) paylaşılıyor, her client kalan süreyi kendisi hesaplıyor.
- **Animasyon ağdan gönderilmiyor.** Her client karşı oyuncunun animasyonunu, senkron gelen pozisyon değişiminden hesaplıyor.

### Karakter
- **Kendi kafamız gizli ama gölgesi var.** Kafa `Shadows Only` modunda. Layer ile gizleme elendi, çünkü URP'de
  kameranın çizmediği obje gölge de bırakmıyor.
- **Kamera boyun ekseninde dönüyor.** Aşağı bakınca öne ve aşağı kayıyor, gövdenin içine girmiyor ve ayaklar görünüyor.
- **Animation Rigging:** Multi-Aim ile kafa ve gövde bakışa dönüyor, Two Bone IK ile eller silahta. Bakış açısı
  ağdan paylaşılıyor, rakip nereye bakıldığını görüyor.
- **Mermi nişangaha gidiyor.** Kameranın ortasından atılan ışının çarptığı noktaya yönleniyor. Mermi sunucuda
  raycast ile ilerliyor (tunneling olmasın diye Rigidbody kullanılmadı).

### UI
Menü, HUD (faz, sayaç, nişangah), sonuç ekranı (atış sayıları) ve World Space isim etiketi.
İsim etiketi karanlıkta gizleniyor, aksi halde rakibin yerini ele verirdi.

## Yapılmayanlar
- **İnternet üzerinden bağlantı (Unity Relay):** Case LAN istiyor. Unity Gaming Services kurulumu kapsamı büyütüyordu.
- **Vuruş ve hasar:** Case şart koşmuyor. Sonuç ekranı bu yüzden atış sayısını gösteriyor.
- **Ayrı FPS kol modeli:** Case kendi gövdemizin görünmesini istediği için aynı model kullanıldı. Kol pozu,
  kamera ve silah konumu ayarlanarak dengelendi.
- **Ses ve efektler:** Zaman case maddelerine ayrıldı.
