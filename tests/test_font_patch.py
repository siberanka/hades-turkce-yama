import base64
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
from font_patch import ALIASES, compatibility_patch, read_remap, verify_compatibility


class FontPatchTests(unittest.TestCase):
    def test_patch_preserves_existing_aliases_comments_bom_and_newlines(self):
        original = b'\xef\xbb\xbf/* { ignored } */\r\n{\r\n "Credits_Title": "TitleBase",\r\n}\r\n'
        patch = compatibility_patch(original)
        edit = patch['edits'][0]
        result = original[:edit['offset']] + base64.b64decode(edit['replacement']) + original[edit['offset'] + edit['length']:]
        verify_compatibility(original, result)
        self.assertTrue(result.startswith(b'\xef\xbb\xbf/* { ignored } */\r\n'))
        self.assertTrue(result.endswith(b' "Credits_Title": "TitleBase",\r\n}\r\n'))
        self.assertEqual(read_remap(result)[0], {'Credits_Title': 'TitleBase', **ALIASES})

    def test_existing_overrides_duplicates_and_malformed_maps_are_rejected(self):
        for data in [b'{"CaesarDressing":"Custom"}', b'{"A":"B","A":"C"}',
                     b'{"A":12}', b'{"A":"B" "C":"D"}', b'{"A":{"B":"C"}}']:
            with self.subTest(data=data), self.assertRaises(ValueError):
                compatibility_patch(data)

    def test_unrelated_alias_modification_is_detected(self):
        original = b'{"Custom":"Keep"}'
        patch = compatibility_patch(original)
        edit = patch['edits'][0]
        result = original[:edit['offset']] + base64.b64decode(edit['replacement']) + original[edit['offset'] + 1:]
        with self.assertRaises(ValueError):
            verify_compatibility(original, result.replace(b'"Keep"', b'"Changed"'))
