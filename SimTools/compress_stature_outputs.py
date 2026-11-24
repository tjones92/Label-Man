"""Losslessly archive a completed run's manifest-listed CSVs, verifying before removing raw copies."""
import argparse
import gzip
import hashlib
import json
import shutil
from pathlib import Path

from stature_normalization import file_digest


def csv_digest(path):
    if path.exists():
        return file_digest(path)
    digest = hashlib.sha256()
    with gzip.open(str(path) + '.gz', 'rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            digest.update(block)
    return digest.hexdigest()


def compress(directory, name):
    directory = directory.resolve()
    manifest = directory / 'writing-calibration' / f'{name}-outputs.json'
    entries = json.loads(manifest.read_text(encoding='utf-8-sig'))
    for entry in entries:
        path = directory / entry['name']
        if path.parent.resolve() != directory or not path.name.startswith(name + '-') or path.suffix != '.csv':
            raise ValueError('Invalid archive target')
        if path.is_symlink():
            raise ValueError('Refusing symlink archive target')
        if not path.exists():
            if csv_digest(path).upper() != entry['sha256'].upper():
                raise ValueError('Existing archive differs from manifest')
            continue
        if file_digest(path).upper() != entry['sha256'].upper():
            raise ValueError('Raw CSV differs from manifest')
        archive = Path(str(path) + '.gz')
        if archive.exists():
            raise FileExistsError(archive)
        with path.open('rb') as source, archive.open('xb') as target:
            with gzip.GzipFile(filename='', mode='wb', fileobj=target, compresslevel=1, mtime=0) as packed:
                shutil.copyfileobj(source, packed, 1024 * 1024)
        digest = hashlib.sha256()
        size = 0
        with gzip.open(archive, 'rb') as restored:
            for block in iter(lambda: restored.read(1024 * 1024), b''):
                digest.update(block); size += len(block)
        if digest.hexdigest().upper() != entry['sha256'].upper() or size != entry['bytes']:
            raise ValueError('Archive failed lossless verification; raw CSV retained')
        # Do not remove a source changed while it was being packed.
        if file_digest(path).upper() != entry['sha256'].upper():
            raise ValueError('Raw CSV changed during archive; raw CSV retained')
        path.unlink()
    print(f'Archived and verified {len(entries)} CSVs: {name}', flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--directory', type=Path, default=Path('SimLogs'))
    parser.add_argument('--name', required=True)
    args = parser.parse_args()
    compress(args.directory, args.name)
