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
    p.add_argument('--history', action='store_true', help='Audit all reachable commit trees before publication')
    args = p.parse_args()
    if args.staged and args.history:
        p.error('--staged and --history cannot be combined')
    objects = []
    if args.history:
        seen = set()
        for commit in git('rev-list', '--all').decode().splitlines():
            for raw in git('ls-tree', '-rz', '--full-tree', commit).split(b'\0'):
                if not raw:
                    continue
                metadata, name = raw.split(b'\t', 1)
                mode, kind, object_id = metadata.split()
                pair = (name, object_id)
                if pair in seen:
                    continue
                seen.add(pair)
                objects.append((name, object_id.decode(), kind))
    else:
        names = git('diff', '--cached', '--name-only', '--diff-filter=ACMR', '-z') if args.staged else git('ls-files', '-z')
        objects = [(raw, ':' + raw.decode('utf-8'), b'blob') for raw in names.split(b'\0') if raw]
    errors = []
    for raw, reference, kind in objects:
        name = raw.decode('utf-8')
        path = PurePosixPath(name)
        allowed = name in ALLOWED_TOP or (path.parts[0] in ALLOWED_ROOTS and
                  (path.suffix in ALLOWED_SUFFIXES or name in {'.githooks/pre-commit', '.githooks/pre-push'}))
        if kind != b'blob' or not allowed or any(part.lower() in {'ai', '.old', '.github', '.gitlab', 'content', 'dist', 'build', '__pycache__'} for part in path.parts):
            errors.append(name + ': disallowed repository path')
            continue
        data = git('show', reference)
        if len(data) > 512_000 or b'\0' in data:
            errors.append(name + ': binary or oversized file')
        for label, pattern in PATTERNS.items():
            if pattern.search(data):
                errors.append(name + ': ' + label)
    if errors:
        raise SystemExit('\n'.join(errors))
    print(f'Repository guard: passed ({len(objects)} file versions checked)')


if __name__ == '__main__':
    main()
