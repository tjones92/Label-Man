import unittest

import pandas as pd

from analyze_followup_chart_funnel import event_starts, funnel_rows


class FollowupChartFunnelTests(unittest.TestCase):
    def test_release_failures_are_not_mislabeled_as_entry_delays(self):
        records = pd.DataFrame([
            (5, 'a', 'hit', 8, 7), (6, 'a', 'hit', 9, 8),
            (13, 'a', 'prehit-release', 80, 12),
            (24, 'a', 'second', 90, 2), (26, 'a', 'second', 9, 4),
            (6, 'b', 'silent-hit', 7, 5),
            (60, 'c', 'censored-hit', 4, 6),
        ], columns=['week', 'artistId', 'recordId', 'currentPosition', 'weeksSinceRelease'])
        records['quality'] = .75
        records['group'] = 'field'
        launches = pd.DataFrame([
            (1, 'a', 'prehit-release', 10), (7, 'a', 'miss', 6),
            (22, 'a', 'second', 15), (54, 'a', 'late-miss', 32),
        ], columns=['week', 'artistId', 'singleId', 'weeksSinceLastSingle'])
        for column, value in dict(tier='Small', labelId='l', strategy='OrphanSingle', genre='Soul',
                                  careerState='Rising', runtimeQuality=.63, hook=.6, production=.66).items():
            launches[column] = value
        cohorts, releases = funnel_rows(records, launches, set(), 100)
        a = cohorts.set_index('artistId').loc['a']
        self.assertEqual(set(cohorts.artistId), {'a', 'b'})
        self.assertEqual((a.firstReleaseGap, a.firstDebutGap), (2, 8))
        self.assertEqual((a.releases52, a.debuts52), (3, 2))
        q = releases.set_index('singleId')
        self.assertTrue(pd.isna(q.loc['miss', 'entryDelay']))
        self.assertEqual(q.loc['second', 'entryDelay'], 2)
        self.assertEqual(q.loc['second', 'entered13'], 1)
        self.assertEqual(q.loc['late-miss', 'entry13Eligible'], 0)
        self.assertEqual(q.loc['miss', 'predecessors'], 1)
        self.assertTrue(q.loc['miss', 'originalHitOnChart'])
        self.assertEqual(cohorts.set_index('artistId').loc['b', 'noRelease52'], 1)

    def test_three_week_events_are_proxies_and_do_not_merge_different_artists(self):
        debuts = pd.DataFrame([
            ('a', 9, 'a1'), ('a', 10, 'a2'), ('a', 12, 'a3'), ('a', 16, 'a4'), ('a', 17, 'a5'),
            ('b', 10, 'b1'),
        ], columns=['artistId', 'week', 'recordId'])
        self.assertEqual(set(event_starts(debuts).recordId), {'a1', 'a4', 'b1'})


if __name__ == '__main__':
    unittest.main()
