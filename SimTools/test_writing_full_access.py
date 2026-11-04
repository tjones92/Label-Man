import unittest
from unittest.mock import patch

from analyze_writing_full_access import REQUIRED, SWITCH, comparison_result, complete_window_mix, mix, stock_destinations, validate_pair


class FullAccessTests(unittest.TestCase):
    def manifests(self):
        flags = sorted(REQUIRED | {'--seed=1001'})
        return (dict(assemblySha256='same', flags=flags + ['--run=control']),
                dict(assemblySha256='same', flags=flags + ['--run=treatment', SWITCH]))

    def test_accepts_only_sampling_difference(self):
        self.assertEqual(validate_pair(*self.manifests()), 1001)

    def test_rejects_different_seed_policy_or_assembly(self):
        for kind in ('seed', 'policy', 'assembly', 'duplicate', 'control-treatment'):
            with self.subTest(kind=kind):
                c, t = self.manifests()
                if kind == 'seed':
                    t['flags'].remove('--seed=1001')
                    t['flags'].append('--seed=2002')
                elif kind == 'policy':
                    t['flags'].append('--writing-market-plan-reminders')
                elif kind == 'assembly':
                    t['assemblySha256'] = 'different'
                elif kind == 'duplicate':
                    t['flags'].append(SWITCH)
                else:
                    c['flags'].append(SWITCH)
                with self.assertRaises(ValueError):
                    validate_pair(c, t)

    def test_ratio_excludes_traditional_and_uses_summed_counts(self):
        result = mix({'ExternalProfessional': 3, 'CoverStandard': 4,
                      'CoverRecentHit': 2, 'TraditionalPublicDomain': 1})
        self.assertEqual(result['coversPerProfessional'], 2)
        self.assertEqual(result['professionalPerCover'], .5)
        self.assertEqual(result['professionalShare'], .3)
        self.assertIsNone(mix({'CoverStandard': 1})['coversPerProfessional'])

    def test_stock_excludes_opening_recordings_and_deduplicates_album_tracks(self):
        def demo(song, week, status='Recorded'):
            return dict(songId=song, artistId='a', week=str(week), status=status)
        def use(song, source='ExternalProfessional'):
            return dict(songId=song, artistId='a', week='5', source=source, terminalState='Released')
        data = {'writing-demos': [demo('opening', 0), demo('opening', 104),
                                  demo('both', 4), demo('both', 104), demo('album', 5),
                                  demo('unused', 104), demo('stock', 104, 'Available')],
                'writing-launches': [use('both'), use('album', 'CoverRecentHit')],
                'writing-album-tracks': [use('both'), use('album'), use('album')],
                'writing-suppliers': []}
        with patch('analyze_writing_full_access.iter_rows', side_effect=lambda r, s: iter(data[s])):
            result = stock_destinations('fixture')
        self.assertEqual(result['newlyRecordedDemoDestinations'],
                         {'both': 1, 'albumOnly': 1, 'noObservedReleasedUse': 1})
        self.assertEqual(result['endpointDemoStatus'], {'Recorded': 4, 'Available': 1})

    def test_launch_window_keeps_boundary_and_rejects_missing_provenance(self):
        data = {'writing-launches': [{'recordId': 'p', 'source': 'ExternalProfessional'},
                                    {'recordId': 'c', 'source': 'CoverStandard'}],
                'single-launch-quality': [{'singleId': 'c', 'week': '0'},
                                          {'singleId': 'p', 'week': '91'},
                                          {'singleId': 'c', 'week': '92'}]}
        with patch('analyze_writing_full_access.iter_rows', side_effect=lambda r, s: iter(data[s])):
            self.assertEqual(complete_window_mix('fixture')['sources'], {'ExternalProfessional': 1})
            data['single-launch-quality'].append({'singleId': 'missing', 'week': '1'})
            with self.assertRaises(ValueError):
                complete_window_mix('fixture')

    def test_pooling_uses_counts_and_rejects_duplicate_seeds_or_mixed_builds(self):
        def pair(seed, pro, covers):
            arm = dict(workflow=dict(assemblySha256='same'),
                       matureIntentions=mix({'ExternalProfessional': pro, 'CoverStandard': covers}),
                       completeChartWindowLaunches=mix({'ExternalProfessional': pro, 'CoverStandard': covers}))
            return dict(seed=seed, control=arm, treatment=arm)
        pairs = [pair(1, 1, 10), pair(2, 9, 9)]
        self.assertEqual(comparison_result(pairs)['pooled']['matureIntentions']['control']['coversPerProfessional'], 1.9)
        with self.assertRaises(ValueError):
            comparison_result([pairs[0], pairs[0]])
        pairs[1]['treatment']['workflow']['assemblySha256'] = 'different'
        with self.assertRaises(ValueError):
            comparison_result(pairs)


if __name__ == '__main__':
    unittest.main()
