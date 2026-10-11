"""Read glyph maps from uncompressed Supergiant XNB v6 fonts, without modifying them."""
import argparse
import json
from pathlib import Path
import struct

from catalog import WORK, digest, write_json


class Reader:
    def __init__(self, data):
        self.data, self.pos = data, 0

    def take(self, count):
        if count < 0 or self.pos + count > len(self.data):
            raise ValueError('Truncated XNB')
        result = self.data[self.pos:self.pos + count]
        self.pos += count
        return result

    def int32(self):
        return struct.unpack('<I', self.take(4))[0]

    def varint(self):
        value = 0
        for shift in range(0, 35, 7):
            byte = self.take(1)[0]
            value |= (byte & 127) << shift
            if byte < 128:
                return value
        raise ValueError('Invalid XNB length')

    def char(self):
        first = self.data[self.pos]
        count = 1 if first < 128 else 2 if 0xC2 <= first <= 0xDF else 3 if 0xE0 <= first <= 0xEF else 4 if 0xF0 <= first <= 0xF4 else 0
        if not count:
            raise ValueError('Invalid UTF-8 glyph')
        return self.take(count).decode('utf-8')


def glyphs(data):
    r = Reader(data)
    if r.take(6) != b'XNBw\x06\x00' or r.int32() != len(data):
        raise ValueError('Unsupported or invalid XNB header')
    surface, width, height, mipmaps = (r.int32() for _ in range(4))
    if surface != 12 or not 0 < width <= 16384 or not 0 < height <= 16384 or mipmaps != 1:
        raise ValueError('Unsupported texture layout')
    pixels = r.int32()
    if pixels != width * height:
        raise ValueError('Invalid Alpha8 texture size')
    r.take(pixels)
    rectangles = r.varint()
    if not 0 < rectangles <= 65536:
        raise ValueError('Invalid glyph count')
    r.take(rectangles * 16)
    if r.varint() != rectangles:
        raise ValueError('Crop count differs from glyph count')
    r.take(rectangles * 16)
    if r.varint() != rectangles:
        raise ValueError('Character count differs from glyph count')
    chars = [r.char() for _ in range(rectangles)]
    if len(set(chars)) != rectangles or chars != sorted(chars):
        raise ValueError('Invalid character map')
    return set(chars)


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('game', type=Path)
    args = p.parse_args()
    fonts = sorted((args.game / 'Content/Win/Fonts/en').glob('*.xnb'))
    required = set('ÇçĞğİıÖöŞşÜüÂâÎîÛû’“”…')
    results = []
    for path in fonts:
        data = path.read_bytes()
        try:
            available = glyphs(data)
            results.append({'font': path.name, 'sha256': digest(data), 'glyph_count': len(available), 'missing': ''.join(sorted(required - available))})
        except (ValueError, IndexError) as error:
            results.append({'font': path.name, 'sha256': digest(data), 'error': str(error)})
    write_json(WORK / 'reports/font-audit.json', results)
    print(json.dumps(results, ensure_ascii=True, indent=2))
    raise SystemExit(not results or any(r.get('error') or r.get('missing') for r in results))


if __name__ == '__main__':
    main()
