import json
import tempfile
import unittest
from pathlib import Path

import stature_baseline as baseline


class BaselineVerification(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.path = Path(self.temp.name)
        self.assembly = self.write('game.dll', b'build')
        self.request = self.write('request.json', json.dumps({
            'assemblySha256': baseline.digest(self.assembly),
            'flags': ['--run=trial', '--seed=2002', '--weeks=522', '--senior-cooling']
        }).encode())
        self.log = self.write('run.log', b'CHART_AUDIT_COMPLETE run=trial weeks=522\n')
        self.errors = self.write('errors.log', b'')
        self.flags = self.write('flags.txt', b'# profile\n--senior-cooling\n')
        self.write('trial-records.csv', b'header\nvalue\n')

    def write(self, name, contents):
        path = self.path / name
        path.write_bytes(contents)
        return path

    def verify(self):
        return baseline.verify(self.request, self.assembly, self.log, self.errors, self.path, self.flags)

    def test_verified_and_fingerprinted(self):
        result = self.verify()
        self.assertEqual(result['seed'], '2002')
        self.assertEqual(result['outputs']['records.csv']['bytes'], 13)
        self.assertIsNone(result['sourceRevision'])

    def test_rejects_incomplete_and_errors(self):
        self.log.write_text('CHART_AUDIT_COMPLETE run=trial weeks=52\n')
        with self.assertRaisesRegex(ValueError, 'completion'):
            self.verify()
        self.log.write_text('CHART_AUDIT_COMPLETE run=trial weeks=522\n')
        self.errors.write_text('failure')
        with self.assertRaisesRegex(ValueError, 'Error log'):
            self.verify()

    def test_rejects_wrong_build_and_missing_flag(self):
        self.assembly.write_bytes(b'other')
        with self.assertRaisesRegex(ValueError, 'Assembly hash'):
            self.verify()
        self.assembly.write_bytes(b'build')
        self.flags.write_text('--senior-cooling\n--follower-demand\n')
        with self.assertRaisesRegex(ValueError, 'Missing combined flags'):
            self.verify()


if __name__ == '__main__':
    unittest.main()
