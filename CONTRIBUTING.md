# Katkıda bulunma

Çeviri hatası, doğal olmayan bir ifade, ekrana sığmayan bir metin veya kurulum sorunu bildirmek için konu açabilirsiniz.

## Çeviri düzeltmeleri

1. Konu açarken metnin oyundaki yerini (bölge, karakter, menü), mevcut Türkçesini ve önerinizi yazın; mümkünse ekran görüntüsü ekleyin.
2. Değişiklik önerisi gönderecekseniz ilgili satırı `ceviri/` klasöründe düzenleyin ve yalnızca `target` alanını değiştirin.
3. `py -3 tools/validate.py` ile denetleyin; biçim işaretleyicileri korunmuş olmalıdır.
4. Önerinin karakterin sesine, sahneye ve [çeviri ilkelerine](docs/CEVIRI.md) uygun olduğunu kısaca açıklayın.

## Kod değişiklikleri

1. `git config core.hooksPath .githooks` ile depo korumasını etkinleştirin.
2. Değişiklikten sonra `./tools/test.ps1` çalıştırın.
3. Oyun dosyalarını, İngilizce kaynak metinleri, derlenmiş dosyaları, resmî görselleri veya kişisel bilgileri depoya eklemeyin.

Commit iletileri kısa, Türkçe ve tür önekiyle yazılır: `fix:`, `feat:`, `docs:`, `chore:`.

Gönderdiğiniz kod katkıları [MIT lisansı](LICENSE) altında, çeviri katkıları ise [NOTICE.md](NOTICE.md) dosyasındaki koşullarla yayımlanır.
