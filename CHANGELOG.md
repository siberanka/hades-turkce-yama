# Değişiklikler

## 1.0.2 — 28 Ekim 2026

- SJSON ile Lua ayrı kodlanır. Sütun komutları (`Column 215/380/530`) ve seçim köşeli parantezleri gereksiz kaçış karakterleri olmadan korunur; görünmeyen yardım seçeneklerinin paketleme nedeni giderilir.
- Üç tırnaklı metinler doğru okunur. Lütuf başlıkları, 210 Chaos birleşimi, tanrı hoşnutsuzluk bildirimleri, ilişki açıklamaları ve kayıt/bulut uyarılarıyla 479 kayıt eklendi; kapsam 26.108 kayda çıktı. Oyun künyesi ve telif bildirimleri korunur.
- Sisyphus ve Patroclus yardım başlıkları kısaltıldı. Arayüzde biçim sınırlarında sözcüklerin bitişmesine karşı boşluk yerleşimi düzeltildi. İki Chaos durum adı, düşmana yeni bir etki eklemeyecek şekilde düzeltildi; birleşik başlıkları eşlendi.
- Özgün piksel sanatlı simge ve üç sahneli slayt eklendi. Dar pencerede görsel panel gizlenir; küçük ekranlarda işlemler kaydırılarak erişilebilir kalır.
- `music.mp3` varsa yerel derlemeye eklenir; yoksa ses kaynağı gömülmez. GitHub kaynağı ve yayın EXE’si müziksizdir. Yerel müzik susturulabilir ve geçici dosyası kapanışta temizlenir.
- Steam/Epic dosya kopyaları ve eski/değişmiş kaynaklar için kurulum, doğrulanmış yedek, geri yükleme ve kaldırma kontrolleri genişletildi. Kurulu oyunlar değiştirilmeden sınandı; gerçek oyun içi görünüm kontrolü ayrı tutulur.

## 1.0.1 — 25 Ekim 2026

- Farklı oyun sürümlerinde kimlik ve özgün metin özetiyle eşleşen çeviriler uygulanır; yeni, değişmiş veya belirsiz kayıtlar korunur. SJSON, CSV ve Lua desteklenir.
- Her kurulum kendi oyun sürümünü ayrı yedekler; eski yedekler üzerine yazılmaz. Kaldırma ve yarıda kesilen kurulumdan geri dönüş bu yedeği kullanır.
- Uyumluluk sonucunda eşleşen ve atlanan kayıtlar gösterilir. Uyumlu olmayan font eşlemesi metin kurulumunu engellemeden korunur.
- Kısmi eşleşme, tekrar eden kimlikler, biçim farkları ve farklı sürüm yedekleri için testler eklendi. Gerçek Steam kurulumu ve oyun içi son kontrol kullanıcı tarafından yapılmalıdır.

## 1.0.0 — 24 Ekim 2026

- 25.629 metin kaydının tamamı Türkçeye çevrildi: arayüz, yardım metinleri, Kodeks, bütün karakter diyalogları, saha ve konak sesleri, anlatıcı ve şarkı altyazıları.
- Bütün metin özgün İngilizce metinle satır satır karşılaştırılarak son okumadan geçirildi; sahne, konuşmacı, hitap ve terim tutarlılığı düzeltildi.
- Çeviri kayıtları `ceviri/` klasöründe yayımlandı; yama paketi herkesin kendi oyun kurulumundan yeniden derlenebilir.
- Tek komutluk derleme betiği: `tools/derle.ps1`.
- Yerel çalışma alanı `calisma/` klasörüne taşındı; depo belgeleri yalnızca Türkçe olarak düzenlendi.
- Kurulum aracı GitHub Releases üzerinden yayımlandı; resmî görsel ve müzik içermez, yanında SHA-256 özet dosyası bulunur.

## Kaynak araçlar — 12 Ekim 2026

- Font dosyası dağıtmadan, oyunun mevcut fontlarına yönlendirmeyle Türkçe karakter uyumluluğu.
- Salt okunur font ön koşulları, doğrulanmış eşleme yedeği ve bire bir geri yükleme.
- Değişmiş fontların, yinelenen eşlemelerin ve izin verilmemiş font yollarının reddedilmesi.

## 0.1.0 — Yerel deneme sürümü, 11 Ekim 2026

- İngilizce metin envanteri, biçim koruyan yama üretimi ve doğrulama araçları.
- Steam/Epic keşfi, klasör seçimi ve uyumluluk kontrolü.
- Doğrulanmış yedekleme, kurulum, geri yükleme ve oyun klasörüne eklenen kaldırma aracı.
- Başarılı kaldırma sonrasında kaldırma aracının silinmesi.
- Kaynak oyun varlıklarını içermeyen paket biçimi.
- Resmî dil karşılaştırması, olay bağlamı dizini ve metin değişince geçersizleşen inceleme kaydı.
- Süreç kesintisinden sonra geri yükleme ve değiştirilmiş kaldırma aracının korunması.
- Sade koyu arayüz, isteğe bağlı yerel slaytlar ve kullanıcı seçimiyle müzik.
