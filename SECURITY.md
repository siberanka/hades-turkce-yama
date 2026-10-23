# Güvenlik

Kurulum aracı yalnızca paket içinde tanımlı Hades metin dosyalarına ve kendi yedek/kaldırma dosyalarına erişir. Bilinmeyen kaynak dosya sürümleri, bozuk yedekler, çakışan düzenlemeler ve seçilen oyun klasörünün dışına çıkan yollar reddedilir. Oyun kayıtları değiştirilmez. Araç internete bağlanmaz, telemetri göndermez ve oyun başlatmaz.

## Sorun bildirme

Bir hata bildirirken erişim anahtarlarını, kullanıcı profilinizin tamamını, orijinal oyun dosyalarını veya kaynağından emin olmadığınız EXE'leri paylaşmayın. Sürüm numarası, hata iletisi ve etkilenen göreli dosya yolu genellikle ilk inceleme için yeterlidir. Güvenlik açığı niteliğindeki bulguları herkese açık konu yerine depo sahibine doğrudan iletin.

## Doğrulama

Statik inceleme veya temiz test sonucu, bir yürütülebilir dosyanın zararlı yazılım içermediğini tek başına kanıtlamaz. İndirdiğiniz kurulum aracının SHA-256 özetini güvenilir yayın kaydıyla karşılaştırın ya da aracı [kaynaktan kendiniz derleyin](docs/GELISTIRME.md).
