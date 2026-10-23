"""Lossless, offline inventory of a locally owned Hades installation.

Only data is read. No game scripts or third-party installers are executed.
The source corpus and context belong in an ignored workspace, never in Git.
"""
from __future__ import annotations

import argparse
from collections import Counter
import csv
import hashlib
import io
import json
import os
from pathlib import Path
import re
import shutil

ROOT = Path(__file__).resolve().parents[1]
WORK = Path(os.environ.get('HADES_TR_CALISMA') or ROOT / 'calisma')
TRANSLATIONS = ROOT / 'ceviri'
TOKEN = re.compile(r'--\[(=*)\[.*?\]\1\]|--[^\r\n]*|/\*.*?\*/|//[^\r\n]*|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\'|[A-Za-z_][\w.]*|[{}\[\]=,:]', re.S)
FIELDS = {'DisplayName', 'Description', 'AdditionalDescription'}


def decode_sjson(value: str) -> str:
    # SJSON permits literal backslashes, e.g. keyboard/platform hints.
    def escape(match):
        seq = match[0]
        return json.loads('"' + seq + '"') if seq[1] in '"\\/bfnrtu' else seq
    return re.sub(r'\\(?:u[0-9a-fA-F]{4}|.)', escape, value[1:-1], flags=re.S)


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def write_json(path: Path, obj):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(obj, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def sjson_strings(text: str, lua=False):
    """Return string spans from each identified object; skip comments completely."""
    tokens = [m for m in TOKEN.finditer(text) if not m[0].startswith(('/*', '//', '--'))]
    stack = []
    occurrences = Counter()
    for i, tok in enumerate(tokens):
        if tok[0] == '{':
            stack.append({})
        elif tok[0] == '}':
            if not stack:
                raise ValueError('Unbalanced SJSON object')
            obj = stack.pop()
            id_field = 'Cue' if lua else 'Id'
            if id_field not in obj:
                continue
            ident = decode_sjson(obj[id_field][0])
            for field in sorted(({'Text'} if lua else FIELDS) & obj.keys()):
                value = obj[field]
                key = f'{ident}::{field}'
                occurrences[key] += 1
                yield (f'{key}::{occurrences[key]}', value.start(), value.end(),
                       decode_sjson(value[0]), 'lua' if lua else 'sjson')
        elif stack and i + 2 < len(tokens) and tokens[i + 1][0] in ('=', ':') and tokens[i + 2][0].startswith(('"', "'")):
            stack[-1][tok[0].strip('"')] = tokens[i + 2]
    if stack:
        raise ValueError('Unclosed SJSON object')


def csv_spans(text: str):
    """Retain exact byte layout even for quoted commas and multiline cells."""
    start = 0
    cells = []
    quoted = False
    i = 0
    while i < len(text):
        ch = text[i]
        if ch == '"':
            if quoted and i + 1 < len(text) and text[i + 1] == '"':
                i += 2
                continue
            quoted = not quoted
        if not quoted and ch in ',\r\n':
            cells.append((start, i))
            if ch != ',':
                yield cells
                cells = []
                if ch == '\r' and i + 1 < len(text) and text[i + 1] == '\n':
                    i += 1
            start = i + 1
        i += 1
    if quoted:
        raise ValueError('Unclosed CSV cell')
    if start < len(text) or cells:
        cells.append((start, len(text)))
        yield cells


def csv_strings(text: str):
    occurrences = Counter()
    line_column = None
    for row in csv_spans(text):
        if len(row) < 7:
            continue
        values = next(csv.reader(io.StringIO(','.join(text[a:b] for a, b in row))))
        if line_column is None:
            if 'Line' not in values:
                raise ValueError('CSV subtitle header has no Line column')
            line_column = values.index('Line')
            continue
        if len(values) <= line_column:
            continue
        ident = values[3]
        if not re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*', ident) or not values[line_column].strip():
            continue
        occurrences[ident] += 1
        yield (f'{ident}::Line::{occurrences[ident]}', *row[line_column], values[line_column], 'csv')


def encode(text: str, kind: str) -> bytes:
    if kind in ('sjson', 'lua'):
        return json.dumps(text, ensure_ascii=False).encode('utf-8')
    buf = io.StringIO(newline='')
    csv.writer(buf, lineterminator='\r\n').writerow([text])
    return buf.getvalue()[:-2].encode('utf-8')


def inventory(game: Path):
    game = game.resolve(strict=True)
    sources = sorted((game / 'Content/Game/Text/en').glob('*.sjson'))
    sources += sorted((game / 'Content/Subtitles/en').glob('*.csv'))
    sources += sorted((game / 'Content/Scripts').glob('*.lua'))
    if not sources:
        raise ValueError('Hades English text files were not found')
    entries = []
    files = []
    for src in sources:
        rel = src.relative_to(game).as_posix()
        data = src.read_bytes()
        text = data.decode('utf-8-sig')
        bom = 3 if data.startswith(b'\xef\xbb\xbf') else 0
        target = WORK / 'cache/originals' / rel
        if target.exists() and target.read_bytes() != data:
            raise ValueError(f'Cached source differs; archive the old corpus first: {rel}')
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(data)
        spans = csv_strings(text) if src.suffix == '.csv' else sjson_strings(text, lua=src.suffix == '.lua')
        offsets = [bom]
        for ch in text:
            offsets.append(offsets[-1] + len(ch.encode('utf-8')))
        for key, start, end, value, kind in spans:
            # Patch history, credits/copyright and formatting-only cells are inventoried
            # separately and never counted as translated story text.
            scope = 'historical' if src.name.startswith('PatchNotes') else 'game'
            if 'Copyright' in key or src.name == 'MacroText.sjson' or not re.search(r'[A-Za-z]{2}', re.sub(r'\{[^{}]*\}', '', value)):
                scope = 'protected'
            entries.append({'key': f'{rel}::{key}', 'file': rel, 'id': key,
                            'kind': kind, 'source': value, 'source_sha256': digest(value.encode('utf-8')),
                            'start': offsets[start], 'end': offsets[end], 'scope': scope})
        files.append({'path': rel, 'sha256': digest(data), 'bytes': len(data)})
    # Read-only local copies provide conversation trigger and speaker context.
    for src in sorted((game / 'Content/Scripts').glob('*.lua')):
        dst = WORK / 'cache/context' / src.name
        dst.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(src, dst)
    write_json(WORK / 'cache/catalog.json', entries)
    write_json(WORK / 'cache/source-manifest.json', files)
    report = {'files': len(files), 'entries': len(entries),
              'scope': dict(Counter(e['scope'] for e in entries)),
              'words_game': sum(len(e['source'].split()) for e in entries if e['scope'] == 'game'),
              'by_file': dict(Counter(e['file'] for e in entries))}
    write_json(WORK / 'reports/inventory.json', report)
    print(json.dumps({k: v for k, v in report.items() if k != 'by_file'}, indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('game', type=Path)
    args = parser.parse_args()
    inventory(args.game)


if __name__ == '__main__':
    main()
