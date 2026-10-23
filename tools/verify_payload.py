"""Verify every edit against the corpus and reparse the resulting text in memory."""
import argparse
import base64
import json
from pathlib import Path

from catalog import TRANSLATIONS, WORK, csv_strings, sjson_strings, digest, write_json
from validate import load_rows
from font_patch import FONT_PATH, verify_compatibility


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--payload', type=Path, default=WORK / 'build/payload.json')
    p.add_argument('--drafts', type=Path, default=TRANSLATIONS)
    args = p.parse_args()
    payload = json.loads(args.payload.read_text(encoding='utf-8'))
    translations = {r['key']: r['target'] for r in load_rows(args.drafts)}
    count = 0
    for f in payload['files']:
        original = (WORK / 'cache/originals' / f['path']).read_bytes()
        if digest(original) != f['before']:
            raise ValueError('Source hash mismatch')
        cursor = 0
        result = bytearray()
        for edit in f['edits']:
            if not cursor <= edit['offset'] or edit['length'] <= 0:
                raise ValueError('Invalid edit order')
            result.extend(original[cursor:edit['offset']])
            result.extend(base64.b64decode(edit['replacement'], validate=True))
            cursor = edit['offset'] + edit['length']
        result.extend(original[cursor:])
        if digest(result) != f['after']:
            raise ValueError('Output hash mismatch')
        if f['path'] == FONT_PATH:
            verify_compatibility(original, bytes(result))
            continue
        def parse(data):
            text = data.decode('utf-8-sig')
            rows = csv_strings(text) if f['path'].endswith('.csv') else sjson_strings(text, lua=f['path'].endswith('.lua'))
            return {r[0]: r[3] for r in rows}
        before, after = parse(original), parse(result)
        if before.keys() != after.keys():
            raise ValueError('Text IDs were changed')
        for key in before:
            full = f['path'] + '::' + key
            expected = translations.get(full, before[key])
            if after[key] != expected:
                raise ValueError('Unexpected text modification: ' + full)
            count += full in translations
    if count != payload['translated']:
        raise ValueError('Translation count mismatch')
    report = {'files': len(payload['files']), 'verified_translations': count, 'hashes': 'passed', 'reparse': 'passed', 'ids': 'unchanged', 'untouched_entries': 'unchanged'}
    write_json(WORK / 'reports/payload-verification.json', report)
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
