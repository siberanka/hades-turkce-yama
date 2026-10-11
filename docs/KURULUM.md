# Installation and removal / Kurulum ve kaldırma

## English

The current installer is an incomplete local preview, not a finished translation. It requires Windows and .NET Framework 4.x. macOS, Linux/Proton and consoles have not been validated.

1. Close Hades and open the installer.
2. Confirm the detected game folder. Steam libraries, Epic manifests and common install locations are checked. Select another installation from the list or browse manually if needed.
3. Use the compatibility check. It does not modify the game.
4. Install the translation. Only known source-file hashes are accepted.
5. Select **English** in the game. Untranslated text remains English.

Original files are stored in `.hades-tr-backup` inside the game folder and verified using SHA-256. Keep that folder intact. Saved games, game executables and voice acting are not modified.

The font alias file is backed up and restored with the text files. Three existing game fonts are checked for compatibility; their binaries are neither modified nor included in the installer. Other font overrides must be reviewed before installation.

To remove the patch, run **Hades-Turkce-Yamayi-Kaldir.exe** from the game folder or choose the removal button in the installer. Verified originals are restored. The uninstaller removes itself after successful removal; original backups are retained. A short-lived helper in Windows TEMP removes the running uninstaller after it exits. The helper file may remain in TEMP.

If a game update or another mod has changed a file, or a backup is damaged, the tool refuses to overwrite it. Inspect the reported file. Steam/Epic file verification can restore official files when needed. Remove the previous patch before applying a new version. Backups from a different source version are never silently replaced.

The tool does not request elevation automatically. Missing write permissions produce an error. Junction/symlink paths are rejected. There is no network access, telemetry, downloader or game launcher. The current EXE is unsigned; distributable artifacts should have accompanying SHA-256 checksums.

The local visual preview has a pausable screenshot slideshow. Music is off by default and uses a local MP3/WAV selected by the user. No Hades soundtrack is embedded. Official screenshot assets are kept out of the repository and require separate distribution review.

## Türkçe

Mevcut paket geliştirme amaçlı, eksik çeviri içeren yerel bir ön sürümdür. Windows'ta .NET Framework 4.x gerektirir. macOS, Linux/Proton ve konsollar için doğrulanmış bir kurulum aracı değildir.

1. Hades'i kapatın ve kurulum aracını açın.
2. Bulunan Hades klasörünü kontrol edin. Birden fazla kurulum varsa listeden seçin. Oyun bulunamadıysa **Gözat** ile `Content` ve `x64`/`x64Vk`/`x86` klasörlerinin bulunduğu ana klasörü seçin.
3. **Uyumluluğu kontrol et** ile dosyaları kontrol edin. Bu işlem oyunu değiştirmez.
4. **Çeviriyi kur** seçeneğini kullanın. Araç yalnızca tanıdığı dosya sürümlerini değiştirir.
5. Oyunda **İngilizce** dilini seçin. Çevrilmeyen kısımlar İngilizce görünür.

Kurulum orijinal dosyaları oyun klasöründeki `.hades-tr-backup` dizininde saklar. Her yedek SHA-256 ile doğrulanır. Bu dizini taşımayın veya silmeyin. Oyun kayıtları, seslendirmeler ve yürütülebilir oyun dosyaları değiştirilmez.

Font eşleme dosyası da metinlerle birlikte yedeklenir ve geri yüklenir. Oyunun mevcut üç fontu uyumluluk için kontrol edilir; font dosyaları değiştirilmez ve kurulum aracına eklenmez. Başka font eşlemeleri varsa kurulumdan önce ayrıca incelenmelidir.

Kaldırmak için oyun klasöründeki **Hades-Turkce-Yamayi-Kaldir.exe** dosyasını açın veya kurulum aracında **Çeviriyi kaldır** seçeneğini kullanın. Dosyalar doğrulanmış yedekten geri yüklenir. Kaldırma aracı başarılı işlem sonunda silinir; orijinal yedekler korunur. Windows'un çalışan EXE'yi kilitlemesi nedeniyle kendi kendini kaldırma işlemi, geçici klasörde çalışan kısa ömürlü bir yardımcı süreçle tamamlanır; yardımcı dosya geçici klasörde kalabilir.

Çeviri kurulduktan sonra oyun güncellendiyse, başka bir mod aynı dosyaları değiştirdiyse veya yedek bozulduysa araç dosyaların üzerine yazmaz. Hata iletisindeki dosyayı inceleyin. Gerekirse Steam/Epic'in dosya doğrulama özelliğiyle oyunun orijinallerini geri getirin; ardından güncel oyun sürümüne uygun paket kullanın.

Yeni yama sürümünden önce mevcut çeviriyi kaldırın. Geri yükleme tamamlandıktan sonra aynı kaynak sürümüne ait yedekler yeniden kullanılabilir. Kaynak sürümü değişmiş yedekler sessizce yenilenmez.

Yönetici yetkisi otomatik istenmez. Klasöre yazma yetkisi yoksa işlem hata verir. Yönlendirilmiş klasörler/junction/symlink yolları kabul edilmez. Araç internete bağlanmaz, oyun başlatmaz, indirme yapmaz ve telemetri göndermez. Mevcut EXE dijital olarak imzalanmamıştır; yayımlanacak bir sürümün özeti yanında `.sha256` dosyası bulunmalıdır.

Yerel görsel önizlemede duraklatılabilir ekran görüntüsü slaytları bulunur. Müzik varsayılan olarak kapalıdır; kullanıcı kendi MP3/WAV dosyasını seçebilir. Resmî görüntüler kaynak depoya dahil edilmez.
