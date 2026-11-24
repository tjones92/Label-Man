import csv
import tempfile
import unittest
from pathlib import Path

from summarize_stature_reader_trial import summarize_arm


class ReaderTrialChecks(unittest.TestCase):
    def test_applied_values_and_fallback(self):
        with tempfile.TemporaryDirectory() as temp:
            source = Path(temp) / 'decisions.csv'
            fields = ('date', 'artistId', 'eligible', 'readStatus', 'snapshotDate', 'oldMultiplier', 'proposedMultiplier', 'appliedMultiplier')
            with source.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=fields); writer.writeheader()
                writer.writerow(dict(date='1/8/1960', artistId='a', eligible='1', readStatus='Available',
                                     snapshotDate='1960-01-01', oldMultiplier='1', proposedMultiplier='2', appliedMultiplier='2'))
                writer.writerow(dict(date='1/8/1960', artistId='b', eligible='1', readStatus='Missing',
                                     snapshotDate='', oldMultiplier='1', proposedMultiplier='', appliedMultiplier='1'))
            result = summarize_arm(source, True)
            self.assertEqual(result['eligible'], 2)
            self.assertEqual(result['changed'], 1)
            with self.assertRaisesRegex(ValueError, 'reader rule'):
                summarize_arm(source, False)


if __name__ == '__main__':
    unittest.main()
