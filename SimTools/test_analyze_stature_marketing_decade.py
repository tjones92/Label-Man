import unittest

from analyze_stature_marketing_decade import chart_metrics


class DecadeMetricsChecks(unittest.TestCase):
    def test_repeat_window_excludes_same_week_and_uses_distinct_records(self):
        def row(week, record, artist='a', position=20):
            return dict(week=str(week), year='1964', artistId=artist,
                        recordId=record, currentPosition=str(position), labelTier='Independent')
        annual, _ = chart_metrics([row(0, 'one'), row(0, 'two'), row(1, 'one'),
                                   row(104, 'three'), row(209, 'four')])
        a = annual['1964']
        self.assertEqual(a['top40Entries'], 4)
        self.assertEqual(a['repeatTop40Entries104Weeks'], 1)
        self.assertEqual(a['openingWeekTop40Entries'], 2)

    def test_cohort_is_first_observed_chart_year_and_slots_count_records(self):
        rows = [dict(week='0', year='1960', artistId='old', recordId='a', currentPosition='9', labelTier='Major'),
                dict(week='209', year='1964', artistId='old', recordId='b', currentPosition='1', labelTier='Major'),
                dict(week='209', year='1964', artistId='new', recordId='c', currentPosition='10', labelTier='Independent'),
                dict(week='210', year='1964', artistId='old', recordId='d', currentPosition='40', labelTier='Major')]
        annual, first = chart_metrics(rows)
        self.assertEqual(first['old'], (1960, 0))
        self.assertEqual(annual['1964']['cohort1960Top10Share'], .5)
        self.assertEqual(annual['1964']['cohort1963to64Hot100Slots'], 1)
        self.assertEqual(annual['1964']['cohort1960Hot100WeeksPerChartingAct'], 2)


if __name__ == '__main__':
    unittest.main()
