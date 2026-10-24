# Hades Türkçe Yama

Supergiant Games'in **Hades** oyunu için hazırlanmış, bağlama dayalı Türkçe çeviri ve yedekli kurulum aracı. Proje sahibi ve çevirmen: **siberanka**.

## Durum

| Alan | Durum |
| --- | --- |
| Çeviri kapsamı | 25.629 / 25.629 metin kaydı (%100) |
| Diyaloglar, arayüz, Kodeks, altyazılar ve şarkılar | Çevrildi |
| Bütün metnin özgün metinle satır satır son okuması | Tamamlandı |
| Oyun içi görünüm, font ve taşma kontrolü | Geri bildirimle sürüyor |

Çeviri tamamlanmıştır. Oyun içinde fark ettiğiniz yazım, anlam, font veya taşma sorunlarını depo üzerinden konu açarak bildirebilirsiniz.

## İndirme

Güncel kurulum aracını [Releases](https://github.com/siberanka/hades-turkce-yama/releases/latest) sayfasından indirebilirsiniz. İndirdiğiniz dosyanın SHA-256 özetini sürüm sayfasındaki `.sha256` dosyasıyla karşılaştırın. Kurulum adımları için [kurulum belgesine](docs/KURULUM.md) bakın.

## Özellikler

- Karakter sesine, sahneye ve olay sırasına göre hazırlanmış doğal Türkçe; tutarlı terim ve hitap kararları.
- Steam kütüphaneleri, Epic kurulumları ve yaygın oyun klasörleri için otomatik bulma; bulunamazsa klasör seçimi.
- Kurulumdan önce kaynak dosya sürümü denetimi, SHA-256 ile doğrulanmış yedekler ve bire bir geri yükleme.
- Oyun klasörüne eklenen kaldırma aracı; oyun kayıtlarına, seslere ve oyun programına dokunulmaz.
- Font dosyası dağıtmadan, oyunun kendi fontlarına yönlendirmeyle Türkçe karakter desteği.
- İnternet bağlantısı, telemetri veya indirme yoktur.

## Belgeler

- [Kurulum ve kaldırma](docs/KURULUM.md)
- [Kaynaktan derleme ve geliştirme](docs/GELISTIRME.md)
- [Çeviri ilkeleri](docs/CEVIRI.md)
- [Çeviri veri biçimi](ceviri/README.md)
- [Katkıda bulunma](CONTRIBUTING.md)
- [Güvenlik](SECURITY.md)
- [Haklar ve bildirimler](NOTICE.md)
- [Değişiklikler](CHANGELOG.md)

## Kaynaktan derleme (kısa yol)

Windows'ta Python 3 ve .NET Framework 4.x yeterlidir; ek paket gerekmez. Oyunun yasal olarak sahip olduğunuz kurulumu yalnızca okunur:

```powershell
git clone https://github.com/siberanka/hades-turkce-yama.git
cd hades-turkce-yama
./tools/derle.ps1 -OyunKlasoru 'C:\Program Files (x86)\Steam\steamapps\common\Hades'
```

Kurulum aracı `dist/` klasörüne derlenir. Ayrıntılar için [geliştirme belgesine](docs/GELISTIRME.md) bakın.

## Depo yapısı

| Klasör | İçerik |
| --- | --- |
| `ceviri/` | Türkçe çeviri kayıtları (kimlik, kaynak özeti ve Türkçe metin) |
| `tools/` | Envanter, doğrulama, paketleme, font eşlemesi ve derleme araçları |
| `installer/` | Windows kurulum ve kaldırma aracı (C#) |
| `tests/` | Python ve kurulum testleri |
| `docs/` | Kurulum, geliştirme ve çeviri belgeleri |

Depoda özgün oyun dosyaları, oyunun İngilizce metinleri, resmî görseller, müzik veya font dosyaları bulunmaz. Yama paketi derleme sırasında kullanıcının kendi oyun kurulumundan üretilir.

## Haklar

Bu çalışma Supergiant Games tarafından hazırlanmış, onaylanmış veya desteklenen resmî bir ürün değildir. Hades ve ilgili oyun içeriğinin hakları Supergiant Games, LLC ve ilgili hak sahiplerine aittir. Özgün araç kodları MIT lisanslıdır; çeviri metinlerinin durumu için [NOTICE.md](NOTICE.md) dosyasına bakın.

[Hades resmî mağaza sayfası](https://store.steampowered.com/app/1145360/Hades/) · [Supergiant Games](https://www.supergiantgames.com/)
