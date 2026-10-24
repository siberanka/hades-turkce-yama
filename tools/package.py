"""Build a patch-only payload. Original game text is never embedded."""
import argparse
import base64
import json
from pathlib import Path
from collections import defaultdict

from catalog import TRANSLATIONS, WORK, digest, encode, write_json
from validate import load_rows, load_reviews, validate
from font_patch import FONT_PATH, ALIASES, compatibility_patch


def build(catalog, manifest, rows, version, preview=False, reviews=None, font_compatibility=None):
    report = validate(catalog, rows, reviews)
    if report['errors']:
        raise ValueError('\n'.join(report['errors']))
    if not preview and report['translated'] != report['total']:
        raise ValueError('A full release requires every game text to be translated')
    by_key = {e['key']: e for e in catalog}
    by_file = {e['path']: e for e in manifest}
    grouped = defaultdict(list)
    for row in rows:
        entry = by_key[row['key']]
        grouped[entry['file']].append((entry, row))
    files = []
    for path, pairs in sorted(grouped.items()):
        data = (WORK / 'cache/originals' / path).read_bytes()
        if digest(data) != by_file[path]['sha256']:
            raise ValueError('Original cache changed: ' + path)
        result = bytearray()
        cursor = 0
        edits = []
        for entry, row in sorted(pairs, key=lambda pair: pair[0]['start']):
            start, end = entry['start'], entry['end']
            if not 0 <= cursor <= start < end <= len(data):
                raise ValueError('Overlapping or invalid spans')
            replacement = encode(row['target'], entry['kind'])
            result.extend(data[cursor:start])
            result.extend(replacement)
            cursor = end
            edits.append({'offset': start, 'length': end - start,
                          'replacement': base64.b64encode(replacement).decode('ascii')})
        result.extend(data[cursor:])
        files.append({'path': path, 'before': digest(data), 'after': digest(result), 'edits': edits})
    if font_compatibility is not None:
        if font_compatibility.get('schema') != 1 or font_compatibility.get('path') != FONT_PATH or font_compatibility.get('aliases') != ALIASES:
            raise ValueError('Invalid font compatibility record')
        original = (WORK / 'cache/originals' / FONT_PATH).read_bytes()
        if digest(original) != font_compatibility['sha256']:
            raise ValueError('Font-map cache changed')
        files.append(compatibility_patch(original))
    return {'schema': 1, 'version': version, 'preview': preview,
            'translated': report['translated'], 'total': report['total'], 'files': files,
            'requirements': font_compatibility['fonts'] if font_compatibility else []}


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--drafts', type=Path, default=TRANSLATIONS)
    p.add_argument('--version', required=True)
    p.add_argument('--preview', action='store_true')
    p.add_argument('--output', type=Path, default=WORK / 'build/payload.json')
    args = p.parse_args()
    font_path = WORK / 'cache/font-compatibility.json'
    if not font_path.exists():
        raise ValueError('Prepare font compatibility with tools/font_patch.py GAME_PATH first')
    fonts = json.loads(font_path.read_text(encoding='utf-8'))
    catalog = json.loads((WORK / 'cache/catalog.json').read_text(encoding='utf-8'))
    manifest = json.loads((WORK / 'cache/source-manifest.json').read_text(encoding='utf-8'))
    payload = build(catalog, manifest, load_rows(args.drafts), args.version, args.preview, load_reviews(), fonts)
    if not payload['files']:
        raise ValueError('Refusing to build an empty translation installer')
    write_json(args.output, payload)
    print(f"Payload: {len(payload['files'])} files, {payload['translated']}/{payload['total']} entries")


if __name__ == '__main__':
    main()
