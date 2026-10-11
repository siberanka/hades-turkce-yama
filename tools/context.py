"""Cache official localization notes and Lua event evidence without executing code.

All generated data stays in the ignored local workspace. Event conditions are
preserved as source evidence, never presented as evaluated game state.
"""
import argparse
from bisect import bisect_right
from collections import defaultdict, deque
import json
from pathlib import Path
import re

from catalog import TOKEN, WORK, decode_sjson, digest, write_json


def objects(text):
    """Yield lexical brace objects with direct fields/comments and named ancestry."""
    stack = []
    previous = deque(maxlen=5)
    for token in TOKEN.finditer(text):
        value = token[0]
        if value.startswith(('/*', '//', '--')):
            if stack:
                stack[-1]['comments'].append(value)
            continue
        prior = list(previous)
        if value == '{':
            name = None
            if len(prior) >= 2 and prior[-1] in ('=', ':'):
                candidate = prior[-2]
                if candidate == ']' and len(prior) >= 4 and prior[-4] == '[':
                    candidate = prior[-3]
                if candidate.startswith(('"', "'")):
                    name = decode_sjson(candidate)
                elif re.fullmatch(r'[A-Za-z_][\w.]*', candidate):
                    name = candidate
            parents = [obj['name'] for obj in stack if obj['name']]
            stack.append({'name': name, 'parents': parents, 'start': token.start(),
                          'fields': {}, 'comments': []})
        elif value == '}':
            if not stack:
                raise ValueError('Unexpected closing object')
            obj = stack.pop()
            obj['end'] = token.end()
            yield obj
        elif stack and value.startswith(('"', "'")) and len(prior) >= 2 and prior[-1] in ('=', ':'):
            field = prior[-2].strip('"\'')
            stack[-1]['fields'][field] = decode_sjson(value)
        previous.append(value)
    if stack:
        raise ValueError('Unclosed object')


def localized_records(text):
    line_starts = [0] + [m.end() for m in re.finditer('\n', text)]
    for obj in objects(text):
        fields = obj['fields']
        if 'Id' not in fields or 'DisplayName' not in fields:
            continue
        notes = []
        events = []
        for comment in obj['comments']:
            if not comment.startswith('/*'):
                continue
            note = '\n'.join(line.strip() for line in comment[2:-2].strip().splitlines())
            found = re.findall(r'^Event:\s*(.+?)\s*$', note, re.M)
            if found:
                events.extend(found)
                notes.append(note)
        yield {'id': fields['Id'], 'speaker': fields.get('Speaker'),
               'events': events, 'context': notes, 'text': fields['DisplayName'],
               'line': bisect_right(line_starts, obj['start'])}


def cache_source(game, path, manifest):
    data = path.read_bytes()
    relative = path.relative_to(game).as_posix()
    cached = WORK / 'cache/context-sources' / relative
    if cached.exists() and cached.read_bytes() != data:
        raise ValueError('Source changed; archive and review the previous cache: ' + relative)
    cached.parent.mkdir(parents=True, exist_ok=True)
    if not cached.exists():
        cached.write_bytes(data)
    manifest.append({'path': relative, 'sha256': digest(data), 'bytes': len(data)})
    return relative, data.decode('utf-8-sig')


def build(game, languages):
    game = game.resolve(strict=True)
    if not languages or any(not re.fullmatch(r'[a-z]{2}(?:-[A-Z]{2})?', lang) or lang == 'en' for lang in languages):
        raise ValueError('Choose official comparison languages such as de fr it')
    localized = defaultdict(list)
    events = defaultdict(list)
    scripts = defaultdict(list)
    manifest = []
    for language in dict.fromkeys(languages):
        paths = sorted((game / 'Content/Game/Text' / language).glob('*.sjson'))
        if not paths:
            raise ValueError('No local files for language: ' + language)
        for path in paths:
            relative, text = cache_source(game, path, manifest)
            for record in localized_records(text):
                record.update(language=language, file=relative)
                localized[record['id']].append(record)
    event_names = {event for records in localized.values() for r in records for event in r['events']}
    for path in sorted((game / 'Content/Scripts').glob('*.lua')):
        relative, text = cache_source(game, path, manifest)
        lines = text.splitlines()
        line_starts = [0] + [m.end() for m in re.finditer('\n', text)]
        for obj in objects(text):
            line = bisect_right(line_starts, obj['start'])
            if obj['name'] in event_names:
                events[obj['name']].append({'file': relative, 'line': line,
                    'parents': obj['parents'], 'lua': text[obj['start']:obj['end']]})
            for cue_field in ('Cue', 'EndCue'):
                cue = obj['fields'].get(cue_field)
                if not cue:
                    continue
                ancestry = obj['parents'] + ([obj['name']] if obj['name'] else [])
                scripts[cue.removeprefix('/VO/')].append({'file': relative, 'line': line,
                    'cue_field': cue_field, 'parents': ancestry,
                    'events': [n for n in ancestry if n in event_names],
                    'text': obj['fields'].get('Text') if cue_field == 'Cue' else None,
                    'excerpt_first_line': max(1, line - 12),
                    'excerpt': '\n'.join(lines[max(0, line - 13):line + 12])})
    output = {'schema': 1, 'languages': languages, 'sources': manifest,
              'localized': dict(localized), 'events': dict(events), 'scripts': dict(scripts)}
    write_json(WORK / 'cache/context-index.json', output)
    report = {'files': len(manifest), 'localized_ids': len(localized),
              'script_cues': len(scripts), 'events_with_lua': len(events),
              'events_without_lua': sorted(event_names - events.keys())}
    write_json(WORK / 'reports/context-index.json', report)
    print(json.dumps({k: len(v) if isinstance(v, list) else v for k, v in report.items()}))


def dossier(cues):
    data = json.loads((WORK / 'cache/context-index.json').read_text(encoding='utf-8'))
    result = {}
    for cue in cues:
        localized = data['localized'].get(cue, [])
        scripts = data['scripts'].get(cue, [])
        events = set(e for r in localized + scripts for e in r['events'])
        result[cue] = {'localized': localized, 'scripts': scripts,
                       'events': {e: data['events'].get(e, []) for e in sorted(events)}}
    write_json(WORK / 'review/current-context.json', result)
    print('Saved local dossier for', len(result), 'identifiers')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest='command', required=True)
    index = commands.add_parser('index')
    index.add_argument('game', type=Path)
    index.add_argument('--languages', nargs='+', default=['de', 'fr'])
    review = commands.add_parser('review')
    review.add_argument('cues', nargs='+')
    args = parser.parse_args()
    if args.command == 'index':
        build(args.game, args.languages)
    else:
        dossier(args.cues)
