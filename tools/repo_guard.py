"""Reject private workspaces, game assets, binaries and common secret formats in Git."""
import argparse
import re
import subprocess
from pathlib import PurePosixPath

ALLOWED_ROOTS = {'installer', 'tools', 'tests', 'docs', '.githooks'}
ALLOWED_TOP = {'.gitignore', '.gitattributes', '.editorconfig', 'README.md', 'LICENSE', 'NOTICE.md', 'CHANGELOG.md', 'CONTRIBUTING.md', 'SECURITY.md'}
ALLOWED_SUFFIXES = {'.cs', '.py', '.ps1', '.md', '.manifest'}
PATTERNS = {
    'access token': re.compile(rb'(?:gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{25,}|glpat-[A-Za-z0-9_-]{15,})'),
    'private key': re.compile(rb'-----BEGIN (?:RSA |EC |OPENSSH )?PRIVATE KEY-----'),
    'private user path': re.compile(rb'[A-Za-z]:[\\/]+Users[\\/]+[^\\/\r\n"\s]+', re.I),
    'embedded original corpus': re.compile(rb'"source"\s*:\s*"[^"\r\n]{40,}'),
}


def git(*args):
    return subprocess.check_output(['git', *args])


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--staged', action='store_true')
    args = p.parse_args()
    names = git('diff', '--cached', '--name-only', '--diff-filter=ACMR', '-z') if args.staged else git('ls-files', '-z')
    errors = []
    for raw in names.split(b'\0'):
        if not raw:
            continue
        name = raw.decode('utf-8')
        path = PurePosixPath(name)
        allowed = name in ALLOWED_TOP or (path.parts[0] in ALLOWED_ROOTS and
                  (path.suffix in ALLOWED_SUFFIXES or name in {'.githooks/pre-commit', '.githooks/pre-push'}))
        if not allowed or any(part.lower() in {'ai', '.old', '.github', '.gitlab', 'content', 'dist', 'build', '__pycache__'} for part in path.parts):
            errors.append(name + ': disallowed repository path')
            continue
        data = git('show', ':' + name)
        if len(data) > 512_000 or b'\0' in data:
            errors.append(name + ': binary or oversized file')
        for label, pattern in PATTERNS.items():
            if pattern.search(data):
                errors.append(name + ': ' + label)
    if errors:
        raise SystemExit('\n'.join(errors))
    print('Repository guard: passed')


if __name__ == '__main__':
    main()
