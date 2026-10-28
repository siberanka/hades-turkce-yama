# Çeviri kayıtları

Bu klasör Hades'in 26.108 metin kaydının Türkçe çevirisini içerir. Dosyalar konu ve sahnelere göre bölünmüştür (örneğin `001-interface.jsonl`, `567-ares-story-and-affinity.jsonl`). Dosya adları yalnızca düzen içindir; derleme bütün `.jsonl` dosyalarını birlikte okur.

## Biçim

Her satır bir JSON nesnesidir:

```json
{"key": "Content/Subtitles/en/ZagreusField.csv::ZagreusField_0483::Line::1", "source_sha256": "832b97…", "target": "İşe yarayacaktır."}
```

| Alan | Anlamı |
| --- | --- |
| `key` | Oyun dosyası ve o dosyadaki metin kimliği |
| `source_sha256` | Özgün İngilizce metnin SHA-256 özeti; oyun güncellenip metin değişirse doğrulama bu kaydı işaretler |
| `target` | Türkçe metin |

Oyunun İngilizce metinleri bu depoda bulunmaz; derleme sırasında kullanıcının kendi kurulumundan okunur.

## Düzenleme kuralları

- Dosyalar UTF-8 (BOM'suz) ve satır sonu LF'dir. Türkçe kesme işareti olarak `’` kullanılır.
- Biçim işaretleyicileri (`{#DialogueItalicFormat}`, `{$Keywords.…}`, `{!Icons.…}`, `\Column 380`, `@GUI…`) kaynaktaki sayı ve türüyle korunmalıdır; yeri Türkçe söz dizimine göre değişebilir.
- SJSON, JSON ile aynı kaçış kurallarını kullanmaz. Sütun/seçim komutlarının ters eğik çizgilerini elle çiftlemeyin; kodlayıcıya doğal hedefi verin. Biçim sıfırlamasından sonra devam eden sözcük aralığını kapanan biçimin içinde tutun: `Koşulları {#PreviousFormat}Seç`.
- Hitap ve terim kararları için [çeviri ilkelerine](../docs/CEVIRI.md) bakın.
- Değişiklikten sonra `py -3 tools/validate.py` çalıştırın.

Bu metinlerin kullanım koşulları için [NOTICE.md](../NOTICE.md) dosyasına bakın.
