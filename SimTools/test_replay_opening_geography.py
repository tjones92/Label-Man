import json
import unittest
from pathlib import Path
from unittest.mock import patch

from replay_opening_geography import allocation, profiles, relocate, derive, analyze


class GeographyReplayChecks(unittest.TestCase):
    def setUp(self):
        self.pop = {'a': 1., 'b': 3., 'c': 2.}
        self.names = {'Home': 'a'}
        self.release = dict(origin='Home', markets='2', peakPosition='50', year='1958', format='Single', seedYoung='100', seedAdult='40')

    def test_home_only_uses_named_origin(self):
        self.assertEqual(allocation(self.release, self.pop, self.names, 'home_only'), {'a': 1., 'b': 0., 'c': 0.})

    def test_count_average_enumerates_home_inclusive_footprints(self):
        result = allocation(self.release, self.pop, self.names, 'home_count_average')
        self.assertAlmostEqual(result['a'], (.25 + 1 / 3) / 2)
        self.assertAlmostEqual(result['b'], .75 / 2)
        self.assertAlmostEqual(sum(result.values()), 1)

    def test_single_market_is_home_only(self):
        self.release['markets'] = '1'
        self.assertEqual(allocation(self.release, self.pop, self.names, 'home_count_average'), {'a': 1., 'b': 0., 'c': 0.})

    def test_full_market_count_is_national(self):
        self.release['markets'] = '3'
        self.assertEqual(allocation(self.release, self.pop, self.names, 'home_count_average'), allocation(self.release, self.pop, self.names, 'neutral'))

    def test_top40_crossover_interpretation(self):
        self.release['peakPosition'] = '40'
        self.assertEqual(allocation(self.release, self.pop, self.names, 'crossover_national'), allocation(self.release, self.pop, self.names, 'neutral'))
        self.release['peakPosition'] = '41'
        self.assertEqual(allocation(self.release, self.pop, self.names, 'crossover_national'), allocation(self.release, self.pop, self.names, 'home_count_average'))

    def test_missing_origin_and_bad_count_fail(self):
        for field, value in (('origin', ''), ('markets', '0'), ('markets', '4')):
            with self.subTest(field=field, value=value), self.assertRaises(ValueError):
                allocation({**self.release, field: value}, self.pop, self.names, 'home_count_average')

    def regions(self):
        return [dict(region=r, populationMillions=str(p), followingYoung='50', followingAdult='20',
                     openingYoung='30', openingAdult='10', openingYoungByYear=json.dumps({'1958': 30}), salesResponse='0')
                for r, p in self.pop.items()]

    def test_conserves_age_totals_and_untagged_stock(self):
        regions = self.regions()
        result = relocate(regions, profiles([self.release], self.pop, self.names, 'home_only'))
        self.assertEqual(result, {'a': (110., 40.), 'b': (20., 10.), 'c': (20., 10.)})
        self.assertEqual(sum(v[0] for v in result.values()), 150)
        self.assertEqual(sum(v[1] for v in result.values()), 60)

    def test_cohorts_have_independent_release_weights(self):
        other = {**self.release, 'origin': 'b', 'year': '1959'}
        names = {**self.names, 'b': 'b'}
        result = profiles([self.release, other], self.pop, names, 'home_only')
        self.assertEqual(result['1958']['a'], 1)
        self.assertEqual(result['1959']['b'], 1)
        self.assertEqual(result['adult']['a'], .5)

    def test_missing_following_remains_missing(self):
        regions = self.regions(); regions[0]['followingYoung'] = ''
        result = relocate(regions, profiles([self.release], self.pop, self.names, 'home_only'))
        self.assertIsNone(result['a'][0])

    def snapshot(self):
        s = {k + 'Score': '0' for k in ('singleYoung', 'singleAdult', 'albumYoung', 'albumAdult', 'tvYoung', 'tvAdult')}
        return {**s, 'followingYoungScore': '.2', 'followingAdultScore': '.1', 'regionalResponse': '0'}

    def test_relocation_changes_regional_inputs_preserving_national_scores(self):
        regions = self.regions()
        stock = relocate(regions, profiles([self.release], self.pop, self.names, 'home_only'))
        result = derive(self.snapshot(), regions, stock, 100)
        self.assertEqual(result['record']['draw'], .2)
        self.assertEqual(result['conversion']['draw'], .2)
        self.assertEqual(result['regional']['draw'], 1)
        self.assertAlmostEqual(result['conversion']['reach'], (100 + 30 + 30) / 600)

    def test_unknown_sales_response_makes_record_reach_unknown(self):
        regions = self.regions(); regions[0]['salesResponse'] = ''
        result = derive(self.snapshot(), regions, relocate(regions, None, neutral=True), 100)
        self.assertIsNone(result['record']['reach'])
        self.assertIsNotNone(result['conversion']['reach'])

    def test_source_above_actual_fails(self):
        regions = self.regions(); regions[0]['openingYoung'] = '500'
        with self.assertRaises(ValueError):
            relocate(regions, profiles([self.release], self.pop, self.names, 'home_only'))

    def test_complete_replay_checks_neutral_bands_and_budgets(self):
        census = [dict(artistId='x', artist='X', openingArchetype='RegionalName', seedYoung='30', seedAdult='10') for _ in self.pop]
        release = {**self.release, 'artistId': 'x', 'seedYoung': '90', 'seedAdult': '30'}
        regional = [{**r, 'artistId': 'x', 'date': '7/1/1960'} for r in self.regions()]
        snapshot = {**self.snapshot(), 'artistId': 'x', 'snapshotDate': '1960-07-01', 'shadowBand': 'Established',
                    'status': 'Observed', 'regionalDraw': '.7', 'recordReach': '.35', 'conversionReach': '.35'}
        marketing = dict(artistId='x', context='release-launch', status='Available', date='7/1/1960',
                         snapshotDate='1960-07-01', legacyCareer='Established', legacyBudget='150', actualProposedBudget='150')
        data = {'opening-following-census': census, 'opening-geography-releases': [release],
                'opening-geography-regions': regional, 'opening-following-marketing': [marketing]}
        config = json.loads((Path(__file__).resolve().parent / 'StatureShadowExperimental.json').read_text())
        config['FollowingPerMillionReference'] = 100
        with patch('replay_opening_geography.rows', side_effect=lambda directory, name, suffix: iter(data[suffix])), \
             patch('replay_opening_geography.selected_shadow', return_value=iter([snapshot])), \
             patch('replay_opening_geography.aliases', return_value=self.names):
            report = analyze(Path('.'), 'fixture', config, Path('.'))
        self.assertTrue(report['neutralMatchesEveryCSharpObservation'])
        self.assertEqual(len(report['bandObservations']), 4)
        self.assertEqual(report['launchBudgetReads'][2]['variant'], 'home_only')


if __name__ == '__main__':
    unittest.main()
