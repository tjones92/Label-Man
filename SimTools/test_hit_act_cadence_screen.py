import unittest

import pandas as pd

from analyze_hit_act_cadence import annual_metrics, departure_followup


class CadenceScreenTests(unittest.TestCase):
    def test_departures_count_acts_once_and_exclude_censored_hits(self):
        records = pd.DataFrame([(1, 'a', 'old', 1), (10, 'a', 'new', 3), (60, 'b', 'late', 2)],
                               columns=['week', 'artistId', 'recordId', 'currentPosition'])
        events = pd.DataFrame([(62, 'a', 'drop'), (62, 'a', 'performance-departure'), (70, 'b', 'drop')],
                              columns=['week', 'artistId', 'eventType'])
        row = departure_followup(records, events, {'old'}, 100)
        self.assertEqual(row['hitActFollowupCohort'], 1)
        self.assertEqual(row['hitActsLeavingWithin52Weeks'], 1)
        self.assertEqual(row['hitAct52WeekDepartureShare'], 1)

    def test_repeats_count_records_not_weeks_and_assign_movers_once(self):
        records = pd.DataFrame([
            (1, 1960, 'a', 'r1', 'major', 1, 'big5'),
            (2, 1960, 'a', 'r1', 'major', 1, 'big5'),
            (2, 1960, 'a', 'r2', 'indie', 8, 'field'),
            (3, 1960, 'b', 'r3', 'indie', 7, 'field'),
        ], columns=['week', 'year', 'artistId', 'recordId', 'labelId', 'currentPosition', 'group'])
        q = pd.DataFrame(columns=['year', 'artistId', 'weeksSinceLastSingle'])
        row = annual_metrics(records, q, set())[1960]
        self.assertEqual((row['acts'], row['repeat2'], row['repeat3']), (2, 1, 0))
        self.assertEqual((row['big5_repeat2'], row['field_repeat2']), (1, 0))
        self.assertEqual(row['top10WeeksPerRecord'], 4 / 3)

    def test_lagged_cohort_excludes_future_hits_and_opening_debuts(self):
        records = pd.DataFrame([
            (1, 1960, 'a', 'opening', 'm', 3, 'big5'),
            (53, 1961, 'a', 'opening', 'm', 30, 'big5'),
            (54, 1961, 'a', 'new', 'm', 90, 'big5'),
            (55, 1961, 'a', 'new', 'm', 8, 'big5'),
            (55, 1961, 'b', 'future', 'i', 1, 'field'),
        ], columns=['week', 'year', 'artistId', 'recordId', 'labelId', 'currentPosition', 'group'])
        q = pd.DataFrame([(1961, 'a', 13), (1961, 'b', 2)],
                         columns=['year', 'artistId', 'weeksSinceLastSingle'])
        row = annual_metrics(records, q, {'opening'})[1961]
        self.assertEqual(row['priorHitActs'], 1)
        self.assertEqual(row['singlesPerPriorHitAct'], 1)
        self.assertEqual(row['medianLaunchGap'], 13)
        self.assertEqual(row['top100DebutsPerPriorHitAct'], 1)
        self.assertEqual(row['top10DebutsPerPriorHitAct'], 1)


if __name__ == '__main__':
    unittest.main()
