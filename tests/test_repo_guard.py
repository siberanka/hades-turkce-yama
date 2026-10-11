"""Publication must catch private files deleted from the latest commit."""
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

GUARD = Path(__file__).resolve().parents[1] / 'tools/repo_guard.py'


class HistoryGuardTests(unittest.TestCase):
    def test_deleted_private_file_is_still_rejected_from_history(self):
        with tempfile.TemporaryDirectory(prefix='hades-repo-guard-') as folder:
            root = Path(folder)

            def git(*args):
                return subprocess.run(['git', *args], cwd=root, check=True, capture_output=True)

            git('init', '--quiet')
            git('config', 'user.name', 'siberanka')
            git('config', 'user.email', '164649238+siberanka@users.noreply.github.com')
            git('config', 'core.hooksPath', '.no-test-hooks')
            (root / 'README.md').write_text('Synthetic test repository\n', encoding='utf-8')
            (root / 'ai').mkdir()
            private = root / 'ai/private.txt'
            private.write_text('Synthetic private cache\n', encoding='utf-8')
            git('add', '.')
            git('commit', '--quiet', '-m', 'test: initial fixture')
            git('rm', 'ai/private.txt')
            git('commit', '--quiet', '-m', 'test: remove private fixture')
            current = subprocess.run([sys.executable, str(GUARD)], cwd=root, capture_output=True)
            self.assertEqual(current.returncode, 0, current.stderr)
            history = subprocess.run([sys.executable, str(GUARD), '--history'], cwd=root, capture_output=True)
            self.assertNotEqual(history.returncode, 0)
            self.assertIn(b'ai/private.txt: disallowed repository path', history.stderr)


if __name__ == '__main__':
    unittest.main()
