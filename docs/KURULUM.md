# Kurulum ve kaldırma

Kurulum aracı Windows'ta çalışır ve .NET Framework 4.x gerektirir. macOS, Linux/Proton ve konsollar için doğrulanmış bir kurulum aracı değildir. Kurulum aracını [kaynaktan derleyebilir](GELISTIRME.md) veya güvenilir bir yayın kaydından indirip SHA-256 özetini doğrulayabilirsiniz.

## Kurulum

1. Hades'i kapatın ve kurulum aracını açın.
2. Bulunan Hades klasörünü kontrol edin. Birden fazla kurulum varsa listeden seçin. Oyun bulunamadıysa **Gözat** ile `Content` ve `x64`/`x64Vk`/`x86` klasörlerinin bulunduğu ana klasörü seçin.
3. **Uyumluluğu kontrol et** ile dosyaları denetleyin. Bu işlem oyunu değiştirmez.
4. **Çeviriyi kur** seçeneğini kullanın. Araç yalnızca tanıdığı dosya sürümlerini değiştirir.
5. Oyunda dil olarak **İngilizce** seçili olsun; Türkçe metinler İngilizce dil dosyalarının yerine kurulur.

Kurulum orijinal dosyaları oyun klasöründeki `.hades-tr-backup` dizininde saklar. Her yedek SHA-256 ile doğrulanır. Bu dizini taşımayın veya silmeyin. Oyun kayıtları, seslendirmeler ve oyun programı değiştirilmez.

Font eşleme dosyası da metinlerle birlikte yedeklenir ve geri yüklenir. Oyunun mevcut üç fontu uyumluluk için denetlenir; font dosyaları değiştirilmez ve kurulum aracına eklenmez. Başka font eşlemeleri varsa kurulumdan önce ayrıca incelenmelidir.

## Kaldırma

Oyun klasöründeki **Hades-Turkce-Yamayi-Kaldir.exe** dosyasını açın veya kurulum aracında **Çeviriyi kaldır** seçeneğini kullanın. Dosyalar doğrulanmış yedekten geri yüklenir. Kaldırma aracı başarılı işlem sonunda kendini siler; orijinal yedekler korunur. Windows çalışan EXE'yi kilitlediği için bu silme işlemi geçici klasörde çalışan kısa ömürlü bir yardımcı süreçle tamamlanır; yardımcı dosya geçici klasörde kalabilir.

## Güncellemeler ve sorunlar

Çeviri kurulduktan sonra oyun güncellendiyse, başka bir mod aynı dosyaları değiştirdiyse veya yedek bozulduysa araç dosyaların üzerine yazmaz. Hata iletisindeki dosyayı inceleyin. Gerekirse Steam/Epic'in dosya doğrulama özelliğiyle oyunun orijinal dosyalarını geri getirin; ardından güncel oyun sürümüne uygun paketi kullanın.

Yeni yama sürümünü kurmadan önce mevcut çeviriyi kaldırın. Geri yükleme tamamlandıktan sonra aynı kaynak sürümüne ait yedekler yeniden kullanılabilir. Kaynak sürümü değişmiş yedekler sessizce yenilenmez.

## Güvenlik notları

Yönetici yetkisi otomatik istenmez; klasöre yazma yetkisi yoksa işlem hata verir. Yönlendirilmiş klasör, junction ve symlink yolları kabul edilmez. Araç internete bağlanmaz, oyun başlatmaz, indirme yapmaz ve telemetri göndermez. Kurulum aracı dijital olarak imzalanmamıştır; yayımlanan her sürümün yanında `.sha256` özet dosyası bulunur.

Kurulum aracında müzik varsayılan olarak kapalıdır; kullanıcı kendi MP3/WAV dosyasını seçebilir. Hades müziği ve resmî görseller kurulum aracına eklenmez.
