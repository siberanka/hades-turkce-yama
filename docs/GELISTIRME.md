# Kaynaktan derleme ve geliştirme

Araçlar Windows'ta Python 3.14 ve .NET Framework 4.x C# derleyicisiyle doğrulanmıştır. Ek Python paketi gerekmez. Bütün işlemler yereldir; CI/CD iş akışı kullanılmaz.

## Gereksinimler

- Windows 10 veya 11
- Python 3 (`py` başlatıcısı ya da `python`; 3.14 ile doğrulanmıştır)
- .NET Framework 4.x (Windows ile birlikte gelir; derleyici `%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe`)
- Yasal olarak sahip olduğunuz bir Hades kurulumu (Steam veya Epic)

## Tek komutla derleme

```powershell
./tools/derle.ps1 -OyunKlasoru 'C:\Program Files (x86)\Steam\steamapps\common\Hades'
```

Betik sırasıyla oyun metinlerinin envanterini çıkarır, Türkçe karakter font eşlemesini hazırlar, `ceviri/` kayıtlarını doğrular, yama paketini üretip yeniden doğrular ve kurulum aracını `dist/Hades-Turkce-Yama-0.1.0-onizleme.exe` olarak derler. Yanına `.sha256` özet dosyası yazılır. Oyun klasörü yalnızca okunur; derleme sırasında oyuna hiçbir şey kurulmaz.

Aynı oyun sürümü ve aynı `ceviri/` kayıtlarıyla üretilen yama paketi bayt düzeyinde aynıdır.

## Adım adım

```powershell
py -3 tools/catalog.py 'OYUN_KLASORU'
py -3 tools/font_patch.py 'OYUN_KLASORU'
py -3 tools/validate.py
py -3 tools/package.py --version 0.1.0-onizleme --preview
py -3 tools/verify_payload.py
./tools/build.ps1
```

| Araç | Görevi |
| --- | --- |
| `catalog.py` | Oyunun İngilizce SJSON, altyazı CSV ve Lua diyalog dosyalarını değiştirmeden okur; her metnin konumunu ve özetini çıkarır. Oyun betikleri çalıştırılmaz. |
| `font_patch.py` | Oyunun üç mevcut fontunda Türkçe karakterleri denetler ve `Fonts.remap` için küçük bir yönlendirme hazırlar. Font dosyası dağıtılmaz. |
| `validate.py` | Kayıtların kaynak özetini, biçim işaretleyicilerini, değişkenleri, sütun düzenini ve karakter bütünlüğünü denetler. |
| `package.py` | Yalnızca değişiklik aralıklarını, Türkçe karşılıkları ve dosya özetlerini içeren yama paketini üretir; tam oyun dosyası pakete konmaz. |
| `verify_payload.py` | Paketi bellekte uygulayıp dosyaları yeniden ayrıştırır; kimlikleri, çevrilmemiş metinleri ve çıktı özetlerini karşılaştırır. |
| `build.ps1` | Paketi gömerek kurulum aracını derler. |

`--preview` olmadan yapılan derleme tam kapsam ve oyun içi doğrulama aşamasını gerektirir; bu bir kalite denetimidir, dağıtım izni denetimi değildir.

## Çalışma klasörü

Envanter, oyun dosyalarının yerel kopyaları, raporlar ve paket depo kökündeki `calisma/` klasörüne yazılır. Bu klasör Git'e girmez. Farklı bir konum kullanmak için `HADES_TR_CALISMA` ortam değişkenini ayarlayabilirsiniz.

## Çeviriyi düzenleme

Çeviri kayıtları `ceviri/` klasöründedir; biçim [ceviri/README.md](../ceviri/README.md) dosyasında açıklanır. Bir metni düzelttikten sonra `validate.py` ile denetleyin ve kurulum aracını yeniden derleyin. Biçim işaretleyicileri (`{#...}`, `{$...}`, `\Column`, `@...`) kaynaktaki sayı ve türüyle aynı kalmalıdır.

## Testler

```powershell
./tools/test.ps1
```

Python birim testleri ile kurulum aracının yedekleme, geri yükleme, kesinti sonrası kurtarma ve oyun bulma testlerini çalıştırır.

## Yardımcı araçlar

- `tools/context.py index 'OYUN_KLASORU' --languages de fr` — resmî dillerdeki karşılıkları adlandırılmış Lua olaylarına bağlayan bağlam dizinini çıkarır. Koşullar kanıt olarak saklanır; oyun çalıştırılmaz.
- `tools/font_audit.py 'OYUN_KLASORU'` — sıkıştırılmamış XNB v6 fontlarının karakter tablolarını okur. Bazı dekoratif fontlarda Türkçe karakter eksikleri bildirilmesi beklenir; rapor oyun içi görüntü kontrolünün yerine geçmez.

## Depo koruması

Depo kancalarını etkinleştirmek için:

```powershell
git config core.hooksPath .githooks
```

`py -3 tools/repo_guard.py --staged`, sahnelenmiş içerikte izin verilen dosya yollarını, ikili dosyaları, oyun kaynak metni sızıntısını ve yaygın erişim anahtarı biçimlerini denetler. Bu kontroller bir güvenlik garantisi değildir; değişiklikler ayrıca incelenmelidir.

`tools/build.ps1 -LocalArtwork`, yalnızca yerelde tutulan ekran görüntülerini gömer ve `LOCAL-ONLY` işareti oluşturur. Bu çıktı yayımlanmaz. Normal derleme özgün geometrik tasarım kullanır; resmî görsel veya müzik içermez.
