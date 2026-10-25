import json
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'tools'))
from catalog import digest, sjson_strings
import package


class PackageCompatibilityTests(unittest.TestCase):
    def test_locator_binds_id_and_source_hash_without_shipping_original_text(self):
        text = '{Id="UniqueId" DisplayName="Original fixture sentence"}'
        ident, start, end, source, kind = next(sjson_strings(text))
        path = 'Content/Game/Text/en/Test.en.sjson'
        entry = dict(key=path + '::' + ident, file=path, id=ident, kind=kind,
                     source=source, source_sha256=digest(source.encode()), start=start, end=end, scope='game')
        row = dict(key=entry['key'], source_sha256=entry['source_sha256'], target='Türkçe örnek', stage='draft')
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            original = root / 'cache/originals' / path
            original.parent.mkdir(parents=True)
            original.write_bytes(text.encode())
            with patch.object(package, 'WORK', root):
                result = package.build([entry], [dict(path=path, sha256=digest(text.encode()))], [row], 'test')
        edit = result['files'][0]['edits'][0]
        self.assertEqual(result['schema'], 2)
        self.assertEqual((edit['entry'], edit['source_sha256']), (ident, entry['source_sha256']))
        self.assertNotIn(source, json.dumps(result))


if __name__ == '__main__':
    unittest.main()
