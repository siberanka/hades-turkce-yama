"""Validate translation drafts without claiming editorial or in-game approval."""
from __future__ import annotations

import argparse
from collections import Counter
import json
from pathlib import Path
import re
import unicodedata

from catalog import ROOT, WORK, digest, write_json

MARKERS = re.compile(r'\{[^{}]*\}|@[A-Za-z][A-Za-z0-9_\\/]*|%(?:\d+\$)?[sdif]|\\(?:Column\s+\d+|\[|\])')
STAGES = {'draft', 'edited', 'context-reviewed', 'in-game-verified'}
TOOLTIP_STYLE_TOKENS = re.compile(r'\{#[^{}]+\}|\{\$TooltipData\.[^{}]+\}')
KEYWORDS = re.compile(r'\{\$Keywords\.([^{}]+)\}')


def tooltip_style_scopes(text):
    """Conservative lexical check, not a simulation of the game's style stack."""
    style = None
    values = []
    for match in TOOLTIP_STYLE_TOKENS.finditer(text):
        token = match.group()
        if token.startswith('{#'):
            style = token
        else:
            values.append((token, style))
    return Counter(values)


def keyword_review_hashes(text, definitions):
    """Track rendered term dependencies, including nested keyword labels."""
    result = {}

    def visit(name):
        if name in result:
            return
        target = definitions.get(name)
        result[name] = digest(target.encode('utf-8')) if isinstance(target, str) else None
        if isinstance(target, str):
            for dependency in KEYWORDS.findall(target):
                visit(dependency)

    for name in KEYWORDS.findall(text):
        visit(name)
    return result


def load_rows(folder: Path):
    result = []
    for path in sorted(folder.glob('*.jsonl')):
        for number, line in enumerate(path.read_text(encoding='utf-8').splitlines(), 1):
            if line.strip():
                row = json.loads(line)
                row['_location'] = f'{path.name}:{number}'
                result.append(row)
    return result


def load_reviews():
    path = WORK / 'review/editorial-reviews.json'
    return json.loads(path.read_text(encoding='utf-8')) if path.exists() else {}


def validate(catalog, rows, reviews=None):
    entries = {e['key']: e for e in catalog}
    targets = {r.get('key'): r.get('target') for r in rows}
    definitions = {e['id'].split('::')[0]: targets.get(e['key']) for e in catalog
                   if e.get('file', '').endswith('HelpText.en.sjson') and '::DisplayName::' in e.get('id', '')}
    errors, warnings = [], []
    seen = set()
    for row in rows:
        key = row.get('key', '')
        where = row.get('_location', key)
        if key in seen:
            errors.append(f'{where}: duplicate translation')
        seen.add(key)
        if key not in entries:
            errors.append(f'{where}: unknown source ID')
            continue
        entry = entries[key]
        target = row.get('target')
        if not isinstance(target, str) or not target.strip():
            errors.append(f'{where}: empty translation')
            continue
        if row.get('source_sha256') != entry['source_sha256']:
            errors.append(f'{where}: source changed; translation needs review')
        if row.get('stage') not in STAGES:
            errors.append(f'{where}: invalid editorial stage')
        if row.get('stage') in {'context-reviewed', 'in-game-verified'}:
            review = (reviews or {}).get(key, {})
            if review.get('source_sha256') != entry['source_sha256'] or review.get('target_sha256') != digest(target.encode('utf-8')):
                errors.append(f'{where}: missing or stale contextual review')
            if not review.get('references') or not all(review.get(check) for check in ('meaning', 'voice', 'scene', 'formatting')):
                errors.append(f'{where}: contextual review lacks evidence or required checks')
            dependencies = keyword_review_hashes(target, definitions)
            if dependencies and (any(value is None for value in dependencies.values()) or
                                 review.get('keyword_sha256') != dependencies):
                errors.append(f'{where}: missing or stale rendered keyword review')
            if row.get('stage') == 'in-game-verified' and not review.get('in_game_evidence'):
                errors.append(f'{where}: in-game verification requires recorded evidence')
        if entry['scope'] != 'game':
            errors.append(f'{where}: protected or out-of-scope source')
        if Counter(MARKERS.findall(entry['source'])) != Counter(MARKERS.findall(target)):
            errors.append(f'{where}: missing, added or changed format marker')
        elif tooltip_style_scopes(entry['source']) != tooltip_style_scopes(target):
            warnings.append(f'{where}: tooltip value formatting changed; review style scope')
        if target.count('{') != target.count('}'):
            errors.append(f'{where}: unbalanced braces')
        if any(ord(ch) < 32 and ch not in '\n\t\r' for ch in target):
            errors.append(f'{where}: control character')
        if any(x in target for x in ['\ufffd', '\u200b', '\ufeff']):
            errors.append(f'{where}: invalid or hidden character')
        if re.search(r'(?<=\w)\?(?=\w)', target):
            errors.append(f'{where}: question mark inside a word; possible encoding damage')
        if target != unicodedata.normalize('NFC', target):
            errors.append(f'{where}: non-NFC Unicode')
        if target == entry['source']:
            warnings.append(f'{where}: unchanged source; verify proper name or deliberate retention')
        if len(target) > max(60, len(entry['source']) * 1.7):
            warnings.append(f'{where}: screen length review needed')
    scope = [e for e in catalog if e['scope'] == 'game']
    return {'total': len(scope), 'translated': len(seen & {e['key'] for e in scope}),
            'stages': dict(Counter(row.get('stage') for row in rows)),
            'errors': errors, 'warnings': warnings}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--drafts', type=Path, default=WORK / 'translations')
    args = parser.parse_args()
    catalog = json.loads((WORK / 'cache/catalog.json').read_text(encoding='utf-8'))
    report = validate(catalog, load_rows(args.drafts), load_reviews())
    write_json(WORK / 'reports/validation.json', report)
    print(json.dumps(report, ensure_ascii=True, indent=2))
    raise SystemExit(bool(report['errors']))


if __name__ == '__main__':
    main()
