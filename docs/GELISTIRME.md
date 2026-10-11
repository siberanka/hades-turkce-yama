# Local development / Yerel geliştirme

## English

The tools were validated with Python 3.14 and the Windows .NET Framework C# compiler. No additional Python packages are required. Builds and tests run locally; no CI/CD workflow is configured.

```powershell
py -3.14 tools/catalog.py 'GAME_DIRECTORY'
py -3.14 tools/context.py index 'GAME_DIRECTORY' --languages de fr
py -3.14 tools/font_patch.py 'GAME_DIRECTORY'
py -3.14 tools/validate.py
py -3.14 tools/package.py --version 0.1.0-onizleme --preview
py -3.14 tools/verify_payload.py
./tools/test.ps1
./tools/build.ps1
```

The inventory reads English SJSON, subtitle CSV and Lua dialogue tables without executing game scripts. CSV text columns are resolved from their headers. Quoted commas, multiline cells and byte spans are preserved.

Font preparation checks Turkish glyphs in three existing game fonts and produces a small alias patch for the English `Fonts.remap`. Existing mappings are preserved. No font binaries are embedded or distributed; their hashes are read-only installation prerequisites. Missing or changed fonts stop installation before any writes. Font size, alignment and clipping still require in-game verification.

The context index joins official localized comments to named Lua events and retains speaker labels, neighboring dialogue, source locations and hashes. Repeated IDs, alternate events and closing cues remain separate. It preserves conditions as evidence; it does not simulate the game or resolve inherited conditions. Missing or stale event names require manual investigation.

Context-reviewed records require local review evidence for meaning, voice, scene and formatting, bound to both source and target hashes. Changing either text invalidates that review. In-game verification additionally requires recorded game evidence. These checks establish traceability, not literary quality or independent human approval.

Translation drafts use JSONL records with `key`, `source_sha256`, `target` and `stage`. Source text and scene context remain in the ignored workspace. The payload carries edit ranges, replacement text and hashes, never complete original game files. Verification reconstructs the outputs in memory and reparses them, checking IDs and untouched entries.

A non-preview payload requires complete coverage and in-game verification. This is a quality check, not legal clearance. The EXE builder embeds an existing payload; package validation and distribution review remain separate requirements.

`tools/font_audit.py GAME_DIRECTORY` reads uncompressed XNB v6 character tables. Unsupported formats fail explicitly. Some decorative and language-selection fonts have missing Turkish characters; a full-font audit is currently expected to report these. Glyph presence does not establish that a scene renders correctly.

Enable repository hooks with `git config core.hooksPath .githooks`. Run `py -3.14 tools/repo_guard.py --staged` to check allowed paths, binaries, source-text leakage and common secret formats. Review changes manually as well.

`tools/build.ps1 -LocalArtwork` optionally embeds private local screenshot inputs and writes a LOCAL-ONLY marker. That output must not be uploaded as a cleared release. Normal builds use original geometric artwork and include no screenshots or soundtrack.

## Türkçe

Araçlar Python 3.14 ve Windows .NET Framework C# derleyicisiyle doğrulanmıştır. Ek Python paketi gerekmez. İşlemler yereldir; CI/CD iş akışı bulunmaz.

```powershell
py -3.14 tools/catalog.py 'OYUN_KLASORU'
py -3.14 tools/context.py index 'OYUN_KLASORU' --languages de fr
py -3.14 tools/font_patch.py 'OYUN_KLASORU'
py -3.14 tools/validate.py
py -3.14 tools/package.py --version 0.1.0-onizleme --preview
py -3.14 tools/verify_payload.py
./tools/test.ps1
./tools/build.ps1
```

Envanter, kaynağı değiştirmeden İngilizce SJSON dosyalarını, altyazı CSV'lerini ve konuşma içeren Lua tablolarını okur. CSV başlığından doğru metin sütununu bulur; farklı sütun düzenleri, tırnaklar ve çok satırlı hücreler korunur. Oyun betikleri çalıştırılmaz.

Font hazırlığı, oyundaki üç mevcut fontun Türkçe karakterlerini kontrol eder ve İngilizce `Fonts.remap` için küçük bir eşleme üretir. Mevcut eşlemeler korunur. Font dosyaları gömülmez veya dağıtılmaz; özetleri kurulumun salt okunur ön koşullarıdır. Eksik veya değişmiş fontta hiçbir dosyaya yazılmadan durulur. Boyut, hizalama ve taşma oyun içinde ayrıca doğrulanmalıdır.

Bağlam dizini resmî dillerdeki açıklamaları adlandırılmış Lua olaylarına bağlar; konuşmacı etiketlerini, çevre replikleri, kaynak konumlarını ve özetlerini korur. Tekrarlanan kimlikler, alternatif olaylar ve kapanış replikleri ayrı tutulur. Koşullar kanıt olarak saklanır; oyun çalıştırılmaz, üst tablolardan gelen koşullar çözümlenmiş sayılmaz. Bulunamayan veya eskimiş olay adları ayrıca incelenir.

Bağlam incelemesi aşaması, anlam, karakter sesi, sahne ve biçim kontrollerinin yerel kaydını gerektirir. İnceleme kaynak ve hedef metnin özetlerine bağlıdır; metin değişirse geçersiz olur. Oyun içi doğrulama için ayrıca oyun kanıtı gerekir. Bu denetimler izlenebilirlik sağlar; edebî kalite veya bağımsız insan onayı oluşturmaz.

Çeviri kayıtları `key`, `source_sha256`, `target` ve `stage` alanlarından oluşan JSONL biçimindedir. Kaynak metin ve bağlam çalışma alanında tutulur. Kayıtların tek başına yayımlanması için hak durumu ayrıca değerlendirilmelidir.

Paketleyici yalnızca değişiklik aralıkları, Türkçe karşılıklar ve dosya özetleri üretir. Tam oyun dosyası pakete konmaz. Üretimden sonra tüm dosyalar bellekte yeniden oluşturulur ve yeniden ayrıştırılır; kayıt kimlikleri, çevrilmemiş metinler ve çıktı özetleri karşılaştırılır.

`--preview` olmadan derlenecek metin paketi tam kapsam ve oyun içi doğrulama aşamasını gerektirir. Bu denetim, dağıtım hakkı veya hukukî onay denetimi değildir. EXE derlemesi mevcut paketi gömer; yayın kararı ve paket testleri derlemeden önce verilmelidir.

Yazı tipi denetimi için:

```powershell
py -3.14 tools/font_audit.py 'OYUN_KLASORU'
```

Denetim, sıkıştırılmamış XNB v6 dosyalarındaki karakter tablolarını okur. Desteklemediği biçimlerde tahmin yürütmez. Bazı dekoratif/alternatif fontlarda Türkçe karakter eksikleri bulunduğundan bütün font denetiminin sıfır koduyla çıkması henüz beklenmemelidir. Bu rapor oyun içi görüntü kontrolünün yerine geçmez.

Depo korumasını etkinleştirmek için `git config core.hooksPath .githooks` kullanın. `py -3.14 tools/repo_guard.py --staged`, sahnelenmiş içerikte izin verilen dosya yollarını, ikili dosyaları, kaynak metin sızıntısını ve yaygın erişim anahtarlarını kontrol eder. Bu kontroller bir güvenlik garantisi değildir; yayın öncesinde değişiklikler ayrıca incelenmelidir.

`tools/build.ps1 -LocalArtwork` özel yerel ekran görüntülerini gömer ve LOCAL-ONLY işareti oluşturur. Bu çıktı yayına uygunmuş gibi yüklenmemelidir. Normal derleme özgün geometrik tasarım kullanır; resmî görsel veya müzik içermez.
