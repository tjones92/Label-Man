import csv
import tempfile
import unittest
from pathlib import Path

from fix_stature_peer_reference import fix


class FixedPeerChecks(unittest.TestCase):
    def test_uses_baseline_reference_and_adjusted_response(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            baseline, adjusted, output, manifest = (root / name for name in ('a.csv', 'b.csv', 'out.csv', 'manifest.json'))
            fields = ('date', 'artistId', 'component', 'smoothed', 'peerReference', 'peerReferenceDate', 'peerCount', 'peerStatus', 'peerScore')
            for path, value, reference in ((baseline, 8, 10), (adjusted, 4, 5)):
                with path.open('w', newline='') as stream:
                    writer = csv.DictWriter(stream, fieldnames=fields)
                    writer.writeheader()
                    writer.writerow(dict(date='1960-01-08', artistId='a', component='single_young_share', smoothed=value,
                                         peerReference=reference, peerReferenceDate='1960-01-01', peerCount=3, peerStatus='ok', peerScore='.8'))
            fix(baseline, adjusted, output, manifest)
            with output.open(newline='') as stream:
                row = next(csv.DictReader(stream))
            self.assertEqual(float(row['peerReference']), 10)
            self.assertEqual(float(row['peerScore']), .4)


if __name__ == '__main__':
    unittest.main()
