import csv
import tempfile
import unittest
from pathlib import Path

import stature_normalization as model


class NormalizationChecks(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.addCleanup(self.tmp.cleanup)
        self.path = Path(self.tmp.name) / 'evidence.csv'
        self.reference = {'version': 'fixture', 'effectiveDate': '1960-01-08', 'values': {key: 0.5 for key in model.COMPONENTS},
            'denominatorFloors': {key: 0.001 for key in model.COMPONENTS}}

    def write_trace(self, second_a=0, omit_region=False):
        fields = ['date', 'artistId', 'regionId', 'labelTier', 'genre', 'careerState', 'followingStatus',
                  *model.SALES, *('market' + s[0].upper() + s[1:] for s in model.SALES),
                  'tvRegisteredYoungEstimate', 'tvRegisteredAdultEstimate', 'followingYoung', 'followingAdult']
        with self.path.open('w', newline='') as out:
            writer = csv.DictWriter(out, fieldnames=fields)
            writer.writeheader()
            for date in ('1/1/1960', '1/8/1960'):
                for artist, units in [('a', 20 if date == '1/1/1960' else second_a), ('b', 40), ('c', 40)]:
                    for region in ('east', 'west'):
                        if omit_region and artist == 'a' and region == 'west':
                            continue
                        row = dict.fromkeys(fields, 0)
                        row.update(date=date, artistId=artist, regionId=region, labelTier='Small', genre='Folk',
                                   careerState='Rising', followingStatus='no_following_record' if artist == 'c' else 'observed',
                                   singleYoungUnits=units / 2, marketSingleYoungUnits=50,
                                   marketAlbumYoungUnits=25, followingYoung=0 if artist == 'c' else 50)
                        writer.writerow(row)

    def rows(self):
        return list(model.candidates(self.path, self.reference, 1, 1, 1))

    def test_lagged_self_exclusion_smoothing_and_missing(self):
        self.write_trace()
        rows = self.rows()
        first = next(r for r in rows if r['artistId'] == 'a' and r['date'] == '1960-01-01' and r['component'] == 'single_young_share')
        second = next(r for r in rows if r['artistId'] == 'a' and r['date'] == '1960-01-08' and r['component'] == 'single_young_share')
        missing = next(r for r in rows if r['artistId'] == 'c' and r['date'] == '1960-01-08' and r['component'] == 'following_young')
        self.assertIsNone(first['fixedScore'])
        self.assertEqual(first['peerStatus'], 'cold_start_or_week_gap')
        self.assertAlmostEqual(second['smoothed'], 0.1)
        self.assertAlmostEqual(second['peerReference'], 0.4)
        self.assertAlmostEqual(second['peerScore'], 0.25)
        self.assertEqual(second['peerReferenceDate'], '1960-01-01')
        self.assertIsNone(missing['smoothed'])
        self.assertEqual(missing['evidenceStatus'], 'missing_following_record')

    def test_current_values_cannot_change_previous_reference(self):
        self.write_trace(second_a=0)
        first = next(r for r in self.rows() if r['date'] == '1960-01-08' and r['artistId'] == 'a' and r['component'] == 'single_young_share')
        self.write_trace(second_a=99)
        second = next(r for r in self.rows() if r['date'] == '1960-01-08' and r['artistId'] == 'a' and r['component'] == 'single_young_share')
        self.assertEqual(first['peerReference'], second['peerReference'])

    def test_ties_zero_floor_and_sparse_peers(self):
        ranked = model.peer_references({'b': {'x': 1}, 'a': {'x': 1}, 'c': {'x': 0}}, 'x', 1)
        self.assertEqual(ranked, [(1, 'a'), (1, 'b')])
        self.assertEqual(model.peer_reference(ranked, 3, 'a', True, 1, 1, 0.1), (1.0, 2, 'ok'))
        highest = model.peer_references({'a': {'x': 9}, 'b': {'x': 2}, 'c': {'x': 1}}, 'x', 1)
        self.assertEqual(model.peer_reference(highest, 3, 'a', True, 1, 1, 0.1), (2.0, 2, 'ok'))
        self.assertEqual(model.peer_reference([(0, 'b')], 1, 'a', False, 1, 1, 0.1), (0.1, 1, 'ok'))
        self.assertIsNone(model.peer_reference(ranked, 3, 'a', True, 1, 3, 0.1)[0])
        self.assertEqual(model.normalize(0, 1), 0)
        self.assertEqual(model.normalize(3, 1), 1)

    def test_week_gap_resets_reference_and_missing_market_is_not_zero(self):
        self.write_trace()
        self.path.write_text(self.path.read_text().replace('1/8/1960', '1/15/1960'))
        rows = self.rows()
        second = next(r for r in rows if r['date'] == '1960-01-15' and r['artistId'] == 'a' and r['component'] == 'single_young_share')
        missing = next(r for r in rows if r['date'] == '1960-01-15' and r['artistId'] == 'a' and r['component'] == 'album_adult_share')
        self.assertIsNone(second['peerReference'])
        self.assertEqual(second['historyStatus'], 'new_or_gap')
        self.assertIsNone(missing['raw'])
        self.assertEqual(missing['evidenceStatus'], 'missing_market')

    def test_incomplete_regions_rejected(self):
        self.write_trace(omit_region=True)
        with self.assertRaisesRegex(ValueError, 'Incomplete regional coverage'):
            self.rows()


if __name__ == '__main__':
    unittest.main()
