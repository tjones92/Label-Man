import csv
import json
import tempfile
import unittest
from pathlib import Path

from compare_stature_references import compare


class ReferenceChecks(unittest.TestCase):
    def test_distinguishes_missing_and_mismatched_bands(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            fixed, peer, output = (root / x for x in ('fixed.csv', 'peer.csv', 'out.json'))
            fields = ('date', 'artistId', 'status', 'band', 'careerState')
            for path, bands in ((fixed, ('Rising', '')), (peer, ('Star', ''))):
                with path.open('w', newline='') as stream:
                    writer = csv.DictWriter(stream, fieldnames=fields); writer.writeheader()
                    writer.writerow(dict(date='1961-01-06', artistId='a', status='observed', band=bands[0], careerState='Rising'))
                    writer.writerow(dict(date='1961-01-06', artistId='b', status='missing_evidence', band=bands[1], careerState='NewSigning'))
            compare(fixed, peer, output)
            counts = json.loads(output.read_text())['years'][0]['counts']
            self.assertEqual(counts['observations'], 2)
            self.assertEqual(counts['comparable'], 1)
            self.assertEqual(counts['differentBand'], 1)
            self.assertEqual(counts['fixedDiffersFromLegacy'], 0)
            self.assertEqual(counts['peerDiffersFromLegacy'], 1)


if __name__ == '__main__':
    unittest.main()
