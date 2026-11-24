import csv
import json
import tempfile
import unittest
from pathlib import Path

from sample_stature_trajectories import sample


class TrajectorySamplingChecks(unittest.TestCase):
    def test_gaps_and_missing_evidence_do_not_create_transitions(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source, output, manifest = (root / name for name in ('input.csv', 'out.csv', 'manifest.json'))
            fields = ['date', 'artistId', 'status', 'band', *[b + 'LeadingRoute' for b in
                      ('Rising', 'Established', 'Star', 'Superstar')]]
            with source.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=fields)
                writer.writeheader()
                for artist, dates, bands, statuses in [
                    ('a', [1, 8, 15, 22, 29, 12], ['', '', '', '', '', 'Rising'], ['ok'] * 6),
                    ('b', [1, 8, 15, 22, 29, 5, 12], ['Star', 'Star', '', 'Rising', 'Rising', 'Star', 'Star'], ['ok', 'ok', 'missing_evidence', 'ok', 'ok', 'ok', 'ok']),
                    ('c', [1, 8, 15, 22, 29], ['Star', 'Star', 'Rising', 'Rising', 'Star'], ['ok'] * 5),
                ]:
                    for index, (day, band, status) in enumerate(zip(dates, bands, statuses)):
                        month = 2 if index >= 5 else 1
                        writer.writerow(dict(date=f'1960-{month:02d}-{day:02d}', artistId=artist,
                                             status=status, band=band, RisingLeadingRoute='record'))
            sample(source, output, manifest)
            result = json.loads(manifest.read_text())
            self.assertEqual(result['selectedArtistIds']['rise'], 'c')
            self.assertEqual(result['selectedArtistIds']['decline'], 'c')
            self.assertEqual(result['selectedArtistIds']['comeback'], 'c')
            self.assertEqual(len(result['sourceSha256']), 64)

    def test_duplicate_dates_fail(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / 'input.csv'
            source.write_text('date,artistId,status,band\n1960-01-01,a,ok,\n1960-01-01,a,ok,\n')
            with self.assertRaisesRegex(ValueError, 'Duplicate artist observation'):
                sample(source, root / 'out.csv', root / 'manifest.json')


if __name__ == '__main__':
    unittest.main()
