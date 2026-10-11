"""Patch font aliases to existing game fonts; never distribute font binaries."""
import argparse
import base64
from pathlib import Path

from catalog import TOKEN, WORK, decode_sjson, digest, write_json
from font_audit import glyphs

FONT_PATH = 'Content/Win/Fonts/en/Fonts.remap'
ALIASES = {
    'CaesarDressing': 'AlegreyaSCBold',
    'CaesarDressingTitling': 'AlegreyaSCBoldTracking',
    'MonospaceTypewriterBold': 'UbuntuMonoBold',
}
REQUIRED = set('ÇçĞğİıÖöŞşÜüÂâÎîÛû’“”…')


def read_remap(data):
    text = data.decode('utf-8-sig')
    tokens = []
    cursor = 0
    for token in TOKEN.finditer(text):
        if text[cursor:token.start()].strip():
            raise ValueError('Unexpected font-map syntax')
        cursor = token.end()
        if not token[0].startswith(('/*', '//', '--')):
            tokens.append(token)
    if text[cursor:].strip() or len(tokens) < 2 or tokens[0][0] != '{' or tokens[-1][0] != '}':
        raise ValueError('Invalid font-map object')
    result = {}
    i = 1
    while i < len(tokens) - 1:
        if i + 2 >= len(tokens) - 1 or not tokens[i][0].startswith('"') or tokens[i + 1][0] != ':' or not tokens[i + 2][0].startswith('"'):
            raise ValueError('Font map requires string keys and values')
        name, value = decode_sjson(tokens[i][0]), decode_sjson(tokens[i + 2][0])
        if name in result:
            raise ValueError('Duplicate font alias: ' + name)
        result[name] = value
        i += 3
        if i < len(tokens) - 1:
            if tokens[i][0] != ',':
                raise ValueError('Missing font-map separator')
            i += 1
    prefix = 3 if data.startswith(b'\xef\xbb\xbf') else 0
    offset = prefix + len(text[:tokens[0].start()].encode('utf-8'))
    return result, offset


def compatibility_patch(original):
    before, offset = read_remap(original)
    if set(before) & ALIASES.keys():
        raise ValueError('Font map already defines a compatibility alias; review existing modifications')
    newline = '\r\n' if b'\r\n' in original else '\n'
    replacement = ('{' + newline + ''.join(f'  "{key}": "{value}",' + newline for key, value in ALIASES.items())).encode('utf-8')
    result = original[:offset] + replacement + original[offset + 1:]
    verify_compatibility(original, result)
    return {'path': FONT_PATH, 'before': digest(original), 'after': digest(result),
            'edits': [{'offset': offset, 'length': 1,
                       'replacement': base64.b64encode(replacement).decode('ascii')}]}


def verify_compatibility(original, result):
    before, _ = read_remap(original)
    after, _ = read_remap(result)
    if set(before) & ALIASES.keys() or after != {**before, **ALIASES}:
        raise ValueError('Unexpected font-map change')


def prepare(game):
    original = (game / FONT_PATH).read_bytes()
    compatibility_patch(original)
    fonts = []
    for name in ALIASES.values():
        path = 'Content/Win/Fonts/en/' + name + '.xnb'
        data = (game / path).read_bytes()
        missing = REQUIRED - glyphs(data)
        if missing:
            raise ValueError('Replacement font lacks required characters: ' + name)
        fonts.append({'path': path, 'sha256': digest(data)})
    cached = WORK / 'cache/originals' / FONT_PATH
    cached.parent.mkdir(parents=True, exist_ok=True)
    if cached.exists() and cached.read_bytes() != original:
        raise ValueError('Font-map source changed; review the old cache')
    if not cached.exists():
        cached.write_bytes(original)
    record = {'schema': 1, 'path': FONT_PATH, 'sha256': digest(original),
              'aliases': ALIASES, 'fonts': fonts, 'in_game_verified': False,
              'redistributed_font_binaries': False}
    write_json(WORK / 'cache/font-compatibility.json', record)
    print('Prepared three font aliases; glyphs verified, in-game layout pending')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('game', type=Path)
    prepare(parser.parse_args().game.resolve(strict=True))
