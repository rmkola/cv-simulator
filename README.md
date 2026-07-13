# OCPP 1.6 Şarj İstasyonu Simülatörü (WinForms)

OCPP 1.6 FINAL standardını uygulayan, **Charge Point** rolünde bir masaüstü (Windows Forms)
simülatör. Bir Central System (CSMS) backend'ine **OCPP-J (WebSocket üzerinden JSON,
alt-protokol `ocpp1.6`)** ile bağlanır ve OCPP 1.6'nın **6 feature profilinin tamamını**
destekler: Core, Firmware Management, Local Auth List Management, Reservation,
Smart Charging, Remote Trigger.

## Proje Yapısı

| Proje | Açıklama |
|-------|----------|
| `Ocpp.Core` | Protokol, mesaj/tip modelleri, `ChargePoint` durum makinesi (platformdan bağımsız). |
| `OcppSimulator.App` | WinForms arayüzü (9 sekme) — yalnızca Windows. |
| `OcppSimulator.Mac` | Avalonia arayüzü (9 sekme) — macOS / Linux / Windows. |
| `Ocpp.TestServer` | Geliştirme için minimal yerel CSMS (konsol). |
| `Ocpp.Core.Tests` | xUnit birim + uçtan uca entegrasyon testleri (29 test). |

## Gereksinimler

- .NET 8 SDK (veya üzeri) — `Ocpp.Core` ve Avalonia projesi `net8.0`, WinForms `net8.0-windows` hedefler.
- WinForms arayüzü için Windows; `OcppSimulator.Mac` (Avalonia) için macOS / Linux / Windows.

## Derleme ve Çalıştırma

```bash
dotnet build OcppSimulator.sln
dotnet test  OcppSimulator.sln          # 26 test

# 1) Yerel test CSMS'ini başlat (varsayılan: http://localhost:9220/)
dotnet run --project Ocpp.TestServer

# 2) Simülatörü başlat
dotnet run --project OcppSimulator.App
```

Simülatörde **Bağlantı** sekmesinde `Central System URL` = `ws://localhost:9220/`,
`Charge Point ID` = örn. `CP001` girin ve **Bağlan**'a basın. Bağlanınca otomatik
BootNotification gider ve Heartbeat akmaya başlar.

## Paketleme / Dağıtım

Tek dosyalık, **kurulum gerektirmeyen** (self-contained, .NET runtime dahil) bir Windows exe üretmek için:

```bash
dotnet publish OcppSimulator.App/OcppSimulator.App.csproj -c Release -r win-x64 \
  --self-contained true -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true \
  -o publish/OcppSimulator
```

Çıktı: `publish/OcppSimulator/OcppSanalSarjSimulatoru.exe` (~69 MB, çift tıkla çalışır).
Hazır paket ayrıca `publish/OcppSanalSarjSimulatoru-v1.0.0-win-x64.zip` olarak sıkıştırılmıştır.

Uygulama simgesi (`OcppSimulator.App/appicon.ico`) yeşil bir şarj istasyonu olup üzerinde
**SANAL** bandı bulunur; böylece gerçek bir istasyon değil, simülatör olduğu görsel olarak bellidir.

## macOS (ve Linux) Sürümü — Avalonia

WinForms yalnızca Windows'ta çalışır. Çapraz platform sürüm için **`OcppSimulator.Mac`**
projesi (Avalonia UI) aynı `Ocpp.Core`'u kullanır ve macOS / Linux / Windows'ta çalışır.
Tüm protokol/durum mantığı ortaktır; yalnızca arayüz farklıdır.

Çalıştırma (herhangi bir platform):
```bash
dotnet run --project OcppSimulator.Mac
```

**macOS için `.app` + `.dmg` paketi** (bir Mac üzerinde, .NET 8 SDK kurulu olmalı):
```bash
chmod +x package-macos.sh
./package-macos.sh            # Mac'in kendi mimarisi (Apple Silicon / Intel) otomatik
# ./package-macos.sh osx-x64  # Intel'e zorla
```
Çıktılar:
- `publish/OcppSimulator.app` — Finder'da çift tıkla / `/Applications`'a sürükle
- `publish/OcppSimulator-1.0.0-osx-arm64.dmg` — sürükle-bırak kurulumlu, sıkıştırılmış DMG

Script; self-contained publish yapar, `.app` yapısını kurar, PNG'den `.icns` ikon üretir,
çalıştırma iznini verir, Gatekeeper quarantine bayrağını temizler ve `.dmg` oluşturur.

> İmzasız uygulama olduğu için ilk açılışta uyarı çıkarsa: **sağ tık → Aç**, ya da
> `xattr -dr com.apple.quarantine /Applications/OcppSimulator.app`.

Windows'tan macOS ikili dosyaları da üretilebilir (test edilemez ama derlenir):
```bash
dotnet publish OcppSimulator.Mac -c Release -r osx-arm64 --self-contained -o publish/mac-arm64
dotnet publish OcppSimulator.Mac -c Release -r osx-x64   --self-contained -o publish/mac-x64
```

## Arayüz Sekmeleri

1. **Bağlantı** — CSMS URL, Charge Point ID, Basic Auth, BootNotification kimliği, bağlan/kes.
2. **Konnektörler** — canlı konnektör durumları; kablo tak/çıkar, RFID Authorize,
   transaction başlat/durdur, MeterValues, güç ve arıza simülasyonu.
3. **Mesajlar** — her CP→CS mesajını düzenlenebilir JSON payload ile elle gönder.
4. **Konfigürasyon** — 43 standart konfigürasyon anahtarı (düzenlenebilir).
5. **Local Auth List** — SendLocalList ile gelen liste ve versiyon.
6. **Rezervasyonlar** — ReserveNow/CancelReservation ile yönetilen rezervasyonlar.
7. **Smart Charging** — SetChargingProfile ile kurulan charging profile'lar.
8. **Firmware / Diagnostics** — firmware & diagnostics durumu; durum bildirimi gönderme.
9. **Log** — ham OCPP-J trafiği (yön renkli, dışa aktarılabilir).

## Test CSMS Komutları

`Ocpp.TestServer` konsolunda `help` yazın. Örnekler (`<cp>` = Charge Point ID):

```
list
remotestart <cp> 1 RFID-001
remotestop  <cp> <transactionId>
reset       <cp> Soft
trigger     <cp> StatusNotification
getconf     <cp>
setconf     <cp> HeartbeatInterval 120
unlock      <cp> 1
changeavail <cp> 1 Inoperative
reservenow  <cp> 1 RFID-001 42
setprofile  <cp> 1
updatefw    <cp>
getdiag     <cp>
```

## Desteklenen Mesajlar (30)

**CP → CS:** Authorize, BootNotification, DataTransfer, DiagnosticsStatusNotification,
FirmwareStatusNotification, Heartbeat, MeterValues, StartTransaction, StatusNotification,
StopTransaction.

**CS → CP (yanıtlanır):** CancelReservation, ChangeAvailability, ChangeConfiguration,
ClearCache, ClearChargingProfile, DataTransfer, GetCompositeSchedule, GetConfiguration,
GetDiagnostics, GetLocalListVersion, RemoteStartTransaction, RemoteStopTransaction,
ReserveNow, Reset, SendLocalList, SetChargingProfile, TriggerMessage, UnlockConnector,
UpdateFirmware.

Ayarlar `%AppData%\OcppSimulator\settings.json` içinde saklanır.
