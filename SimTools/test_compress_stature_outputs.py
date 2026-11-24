import gzip
import json
import tempfile
import unittest
from pathlib import Path

from compress_stature_outputs import compress, csv_digest
from stature_normalization import file_digest
from summarize_stature_marketing_trial import rows


class ArchiveChecks(unittest.TestCase):
    def fixture(self, root):
        metadata = root / 'writing-calibration'; metadata.mkdir()
        source = root / 'trial-example.csv'
        source.write_bytes(b'artistId,value\r\nexample,42\r\n' * 10)
        entry = dict(name=source.name, sha256=file_digest(source), bytes=source.stat().st_size)
        (metadata / 'trial-outputs.json').write_text(json.dumps([entry]))
        return source, entry

    def test_lossless_and_idempotent(self):
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / 'SimLogs') as temp:
            root = Path(temp); source, entry = self.fixture(root)
            original = source.read_bytes()
            compress(root, 'trial')
            self.assertFalse(source.exists())
            self.assertEqual(gzip.decompress(Path(str(source) + '.gz').read_bytes()), original)
            self.assertEqual(csv_digest(source), entry['sha256'])
            self.assertEqual(next(rows(source))['artistId'], 'example')
            compress(root, 'trial')

    def test_changed_raw_and_existing_archive_are_retained(self):
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / 'SimLogs') as temp:
            root = Path(temp); source, _ = self.fixture(root)
            original = source.read_bytes()
            source.write_bytes(b'changed')
            with self.assertRaisesRegex(ValueError, 'manifest'):
                compress(root, 'trial')
            self.assertTrue(source.exists())
            source.write_bytes(original)
            Path(str(source) + '.gz').write_bytes(b'incomplete')
            with self.assertRaises(FileExistsError):
                compress(root, 'trial')
            self.assertEqual(source.read_bytes(), original)


if __name__ == '__main__':
    unittest.main()
