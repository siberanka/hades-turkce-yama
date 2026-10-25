# Kurulum ve kaldırma

Kurulum aracı Windows'ta çalışır ve .NET Framework 4.x gerektirir. macOS, Linux/Proton ve konsollar için doğrulanmış bir kurulum aracı değildir. Kurulum aracını [Releases](https://github.com/siberanka/hades-turkce-yama/releases/latest) sayfasından indirip SHA-256 özetini doğrulayabilir veya [kaynaktan kendiniz derleyebilirsiniz](GELISTIRME.md).

## Kurulum

1. Hades'i kapatın ve kurulum aracını açın.
2. Bulunan Hades klasörünü kontrol edin. Birden fazla kurulum varsa listeden seçin. Oyun bulunamadıysa **Gözat** ile `Content` ve `x64`/`x64Vk`/`x86` klasörlerinin bulunduğu ana klasörü seçin.
3. **Uyumluluğu kontrol et** ile dosyaları denetleyin. Bu işlem oyunu değiştirmez.
4. **Çeviriyi kur** seçeneğini kullanın. Dosya sürümü farklıysa araç metnin kimliğini ve özgün İngilizcesinin SHA-256 özetini birlikte denetler. Yalnızca güvenle eşleşen kayıtlar çevrilir; yeni, değişmiş veya belirsiz kayıtlar özgün hâlinde kalır. Eşleşen ve atlanan kayıt sayıları gösterilir.
5. Oyunda dil olarak **İngilizce** seçili olsun; Türkçe metinler İngilizce dil dosyalarının yerine kurulur.

Kurulum orijinal dosyaları oyun klasöründeki `.hades-tr-backup` dizininde saklar. Her yedek SHA-256 ile doğrulanır. Bu dizini taşımayın veya silmeyin. Oyun kayıtları, seslendirmeler ve oyun programı değiştirilmez.

Font eşleme dosyası da uyumluysa metinlerle birlikte yedeklenir ve geri yüklenir. Oyunun mevcut üç fontu denetlenir; font dosyaları değiştirilmez ve kurulum aracına eklenmez. Fontlar veya eşleme farklıysa eşleme korunur, uyumlu metinlerin kurulumu devam eder ve görünümün oyun içinde kontrol edilmesi gerektiği bildirilir.

## Kaldırma

Oyun klasöründeki **Hades-Turkce-Yamayi-Kaldir.exe** dosyasını açın veya kurulum aracında **Çeviriyi kaldır** seçeneğini kullanın. Dosyalar doğrulanmış yedekten geri yüklenir. Kaldırma aracı başarılı işlem sonunda kendini siler; orijinal yedekler korunur. Windows çalışan EXE'yi kilitlediği için bu silme işlemi geçici klasörde çalışan kısa ömürlü bir yardımcı süreçle tamamlanır; yardımcı dosya geçici klasörde kalabilir.

## Güncellemeler ve sorunlar

Çeviri kurulduktan sonra oyun güncellendiyse, başka bir mod aynı dosyaları değiştirdiyse veya yedek bozulduysa araç dosyaların üzerine yazmaz. Hata iletisindeki dosyayı inceleyin. Gerekirse Steam/Epic'in dosya doğrulama özelliğiyle oyunun orijinal dosyalarını geri getirin; ardından güncel oyun sürümüne uygun paketi kullanın.

Yeni yama sürümünü kurmadan önce mevcut çeviriyi kaldırın. Geri yükleme tamamlandıktan sonra aynı kaynak sürümüne ait yedekler yeniden kullanılabilir. Kaynak sürümü değişmiş yedekler sessizce yenilenmez.

1.0.1 ve sonrasında her kurulum, o anda kurulu oyun dosyaları için `.hades-tr-backup/snapshots/` altında ayrı yedek oluşturur. Eski yedekler üzerine yazılmaz. Kaldırma, başka bir oyun sürümünün dosyalarını kullanmak yerine o kurulumun kendi yedeğini geri getirir. Eski sürümlerin yedek kayıtlarıyla kaldırma desteği korunur.

Dosya bulunamazsa veya metin yapısı güvenle çözümlenemezse dosya atlanır. Aynı kimlikteki tekrarlar belirsizse tahmin yapılmaz. Güvenle uygulanabilecek hiçbir değişiklik yoksa kurulum başlamaz. Yalnızca dosya özetini görmezden gelip eski konumlara yazma yöntemi kullanılmaz.

## Güvenlik notları

Yönetici yetkisi otomatik istenmez; klasöre yazma yetkisi yoksa işlem hata verir. Yönlendirilmiş klasör, junction ve symlink yolları kabul edilmez. Araç internete bağlanmaz, oyun başlatmaz, indirme yapmaz ve telemetri göndermez. Kurulum aracı dijital olarak imzalanmamıştır; yayımlanan her sürümün yanında `.sha256` özet dosyası bulunur.

Kurulum aracında müzik varsayılan olarak kapalıdır; kullanıcı kendi MP3/WAV dosyasını seçebilir. Hades müziği ve resmî görseller kurulum aracına eklenmez.
