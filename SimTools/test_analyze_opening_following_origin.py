import csv
import json
import tempfile
import unittest
from pathlib import Path

from analyze_opening_following_origin import analyze_world, verify_neutrality, NEW_SUFFIXES


class OpeningSourceChecks(unittest.TestCase):
    def world(self, directory, changes=None):
        tables = {
            'opening-following-census': [dict(date='1/1/1960', artistId='a', artist='Act', openingArchetype='FormerStar',
                careerState='Established', region='r', seedYoung='100', seedAdult='10')],
            'opening-following-shadow': [dict(date='7/1/1960', artistId='a', followingYoung='120', followingAdult='20',
                openingYoung='60', openingAdult='5', actualBand='Superstar', actualStatus='Observed',
                halfOpeningBand='Star', halfOpeningStatus='Observed', zeroOpeningBand='Established', zeroOpeningStatus='Observed')],
            'opening-following-marketing': [dict(date='7/8/1960', artistId='a', context='release-launch',
                snapshotDate='1960-07-01', status='Available', legacyBudget='120', actualProposedBudget='250',
                halfOpeningBudget='200', halfOpeningBand='Star', zeroOpeningBudget='120', zeroOpeningBand='Established')]
        }
        for suffix, updates in (changes or {}).items():
            tables[suffix][0].update(updates)
        for suffix, records in tables.items():
            with (directory / f'world-{suffix}.csv').open('w', newline='') as f:
                w = csv.DictWriter(f, fieldnames=records[0]); w.writeheader(); w.writerows(records)
        return analyze_world(directory, 'world')

    def test_raw_source_totals_and_partial_complete_uplift_removal(self):
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / 'SimLogs') as d:
            r = self.world(Path(d))
            self.assertEqual(r['seedTotal'], 110)
            self.assertEqual(r['weeklySourceTotals']['1960-07-01']['openingTotal'], 65)
            h, z = r['budgetReads']
            self.assertEqual((h['upliftReduced'], h['upliftRemoved']), (1, 0))
            self.assertEqual((z['upliftReduced'], z['upliftRemoved']), (1, 1))

    def test_excess_source_and_missing_snapshot_rejected(self):
        for changes in ({'opening-following-shadow': {'openingYoung': '121'}},
                        {'opening-following-marketing': {'snapshotDate': '1960-06-24'}}):
            with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / 'SimLogs') as d, self.assertRaises(ValueError):
                self.world(Path(d), changes)

    def test_missing_standing_falls_back(self):
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / 'SimLogs') as d:
            r = self.world(Path(d), {'opening-following-marketing': {'status': 'Missing', 'halfOpeningBudget': '120', 'zeroOpeningBudget': '120'}})
            self.assertEqual(r['budgetReads'][0]['allReads'], 1)
            self.assertNotIn('availableActualReads', r['budgetReads'][0])

    def test_neutrality_requires_all_original_digests_and_only_new_diagnostics(self):
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / 'SimLogs') as d:
            root = Path(d); (root / 'writing-calibration').mkdir()
            for arm in ('control', 'treatment'):
                for prefix in ('old', 'new'):
                    name = f'{prefix}-1001-{arm}'
                    m = [dict(name=f'{name}-records.csv', sha256='ABC')]
                    if prefix == 'new':
                        m += [dict(name=f'{name}-{s}', sha256='DEF') for s in NEW_SUFFIXES]
                    (root / 'writing-calibration' / f'{name}-outputs.json').write_text(json.dumps(m))
            self.assertEqual(verify_neutrality(root, 'new', 'old', 1001)['control']['byteIdenticalOriginalCsvs'], 1)
            p = root / 'writing-calibration/new-1001-control-outputs.json'
            m = json.loads(p.read_text()); m[0]['sha256'] = 'BAD'; p.write_text(json.dumps(m))
            with self.assertRaisesRegex(ValueError, 'changed'):
                verify_neutrality(root, 'new', 'old', 1001)


if __name__ == '__main__':
    unittest.main()

