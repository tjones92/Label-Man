import copy
import unittest

import pandas as pd

from analyze_label_balance import annual_acts, demand_panel, event_count, slot_outcomes, validate_pilot


class LabelBalanceTests(unittest.TestCase):
    def test_pilot_rejects_unmatched_flags_assemblies_and_incomplete_seeds(self):
        plan = dict(status='native-complete', seeds=[1001], arms={'control': 'c', 'floor13': 'f'},
                    singleFloor=13, baseFlags=['--audience'], weeks=106, assemblySha256='same')
        manifests = {arm: dict(extraFlags=['--audience'] + ([] if arm == 'control' else ['--slate-single-floor=13']),
            profile='game', weeks=106, assemblySha256='same', results=[dict(seed=1001, passed=True, exitCode=0)])
            for arm in plan['arms']}
        validate_pilot(plan, manifests)
        for key, value in [('assemblySha256', 'different'), ('extraFlags', ['--slate-single-floor=13']),
                           ('results', []), ('results', manifests['floor13']['results'] * 2)]:
            invalid = copy.deepcopy(manifests)
            invalid['floor13'][key] = value
            with self.assertRaises(ValueError):
                validate_pilot(plan, invalid)

    def test_mover_is_assigned_once_but_all_hits_count(self):
        chart = pd.DataFrame([
            (1960, 1, 9, 'a', 'first', 'studio', 's'),
            (1960, 2, 8, 'a', 'first', 'big5', 'b'),
            (1960, 3, 7, 'a', 'second', 'big5', 'b'),
            (1961, 53, 5, 'a', 'second', 'big5', 'b'),
            (1960, 1, 0, 'silent', 'miss', 'studio', 's'),
        ], columns=['year', 'week', 'currentPosition', 'artistId', 'recordId', 'group', 'labelId'])
        result = annual_acts(chart).set_index('year')
        self.assertEqual(len(result), 2)
        self.assertEqual(result.loc[1960, 'group'], 'studio')
        self.assertEqual(result.loc[1960, 'top10Records'], 2)
        self.assertEqual(result.loc[1961, 'group'], 'big5')

    def test_debut_proxy_is_anchored_and_owner_changes_remain_separate(self):
        sides = pd.DataFrame(dict(debut=pd.to_datetime(['1960-01-04', '1960-01-18', '1960-02-01']),
                                  recordId=['a', 'b', 'c'], labelId='owner'))
        self.assertEqual(event_count(sides, -1), 3)
        self.assertEqual(event_count(sides, 3), 2)  # not a transitive six-week cluster
        same_day = sides.assign(debut=pd.Timestamp('1960-01-04'))
        self.assertEqual(event_count(same_day, -1), 3)
        self.assertEqual(event_count(same_day, 0), 1)
        same_day.loc[2, 'labelId'] = 'other'
        self.assertEqual(event_count(same_day, 0), 2)

    def test_slots_count_once_and_a_refusal_is_not_a_lost_release(self):
        rows = pd.DataFrame([
            (1, 'a', 1, 'Proposed', 'programme-review'),
            (2, 'a', 1, 'Postponed', 'insufficient-unreserved-cash'),
            (3, 'a', 1, 'Issued', 'issued'),
            (3, 'b', 1, 'Proposed', 'programme-review'),
            (3, 'b', 1, 'Issued', 'issued'),  # last file row wins within one week
            (20, 'late', 20, 'Proposed', 'programme-review'),
            (80, 'a', 1, 'Cancelled', 'window-expired'),  # beyond the observation window
        ], columns=['week', 'slotId', 'proposedWeek', 'state', 'reason']).assign(format='Single')
        result = slot_outcomes(rows, 30).set_index('slotId')
        self.assertEqual(set(result.index), {'a', 'b'})
        self.assertTrue(result.loc['a', 'issued'])
        self.assertTrue(result.loc['a', 'cashBlocked'])
        self.assertFalse(result.loc['a', 'windowExpired'])
        self.assertTrue(result.loc['b', 'issued'])

    def demand_fixture(self):
        releases = pd.DataFrame(dict(singleId=['entry', 'miss', 'late'], week=[10, 10, 30],
                                     entry13Eligible=[1, 1, 0], entered13=[1, 0, 0]))
        stages = pd.DataFrame([dict(recordId=r, week=w, region=region, rawDemand=10,
            serviceableDemand=8, clearedUnits=4, clearedFanUnits=1, commercialFitAtCapture=.5)
            for r in ['entry', 'miss'] for w in range(10, 25) for region in ['east', 'west']])
        attributed = pd.DataFrame(dict(singleId=['entry', 'miss'], initialStock=100,
            initialAwareness=.5, effectiveRecognition=.1, launchCampaignImpact=.4))
        return releases, stages, attributed

    def test_equal_demand_windows_retain_misses_and_do_not_end_at_entry(self):
        result = demand_panel(*self.demand_fixture()).set_index('singleId')
        self.assertEqual(set(result.index), {'entry', 'miss'})
        self.assertTrue(result.observedWeeks.eq(13).all())
        self.assertTrue(result.raw.eq(260).all())
        self.assertTrue(result.cleared.eq(104).all())

    def test_incomplete_missing_and_duplicate_demand_are_rejected(self):
        releases, stages, attributed = self.demand_fixture()
        for invalid in [stages[~(stages.recordId.eq('miss') & stages.week.eq(11))],
                        stages[stages.recordId.ne('miss')]]:
            with self.assertRaisesRegex(ValueError, 'all 13'):
                demand_panel(releases, invalid, attributed)
        with self.assertRaisesRegex(ValueError, 'Duplicate regional'):
            demand_panel(releases, pd.concat([stages, stages.iloc[:1]]), attributed)
        with self.assertRaisesRegex(ValueError, 'Missing launch'):
            demand_panel(releases, stages, attributed.iloc[:1])

    def test_retired_miss_stays_in_denominator_with_verified_inactive_weeks(self):
        releases, stages, attributed = self.demand_fixture()
        census = pd.DataFrame(dict(recordId=['entry', 'miss'], status=['Active', 'Retired'],
                                   retiredAuditWeek=[None, 14]))
        stages = stages[stages.recordId.ne('miss') | stages.week.lt(14)]
        result = demand_panel(releases, stages, attributed, census).set_index('singleId')
        self.assertEqual(result.loc['miss', 'observedWeeks'], 4)
        self.assertEqual(result.loc['miss', 'inactiveWeeks'], 9)
        self.assertEqual(result.loc['miss', 'raw'], 80)
        with self.assertRaisesRegex(ValueError, 'all 13'):
            demand_panel(releases, stages[stages.week.ne(11)], attributed, census)


if __name__ == '__main__':
    unittest.main()
