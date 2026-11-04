"""Measurement regressions: carryovers, timing, lookbacks and comparison windows."""
import unittest

import pandas as pd

from analyze_follower_calibration import entries, summarize


class EntryMeasurementTests(unittest.TestCase):
    def test_opening_hit_seeds_repeat_without_counting_as_a_new_entry(self):
        c = pd.DataFrame([
            (0, 1963, 'old', 'act', 3),
            (1, 1963, 'old', 'act', 5),
            (2, 1963, 'new', 'act', 60),
            (3, 1963, 'new', 'act', 35),
        ], columns=['week', 'year', 'recordId', 'artistId', 'currentPosition'])
        h, _ = entries(c)
        self.assertEqual(list(h.recordId), ['new'])
        self.assertEqual(list(h.week), [3])
        self.assertTrue(h.repeat.iloc[0])

    def test_old_entry_with_recent_occupancy_qualifies_but_expired_hit_does_not(self):
        c = pd.DataFrame([
            (1, 1960, 'old', 'active', 10),
            (105, 1962, 'old', 'active', 35),
            (106, 1962, 'new', 'active', 20),
            (1, 1960, 'expired', 'quiet', 10),
            (106, 1962, 'return', 'quiet', 20),
        ], columns=['week', 'year', 'recordId', 'artistId', 'currentPosition'])
        h, _ = entries(c)
        flags = h.set_index('recordId').repeat
        self.assertTrue(flags['new'])
        self.assertFalse(flags['return'])

    def test_still_active_endpoint_title_counts_once(self):
        c = pd.DataFrame([
            (1, 1963, 'a', 'act', 50),
            (2, 1963, 'a', 'act', 40),
            (3, 1963, 'a', 'act', 20),
            (4, 1963, 'a', 'act', 45),
            (5, 1963, 'a', 'act', 30),
        ], columns=['week', 'year', 'recordId', 'artistId', 'currentPosition'])
        h, _ = entries(c)
        self.assertEqual(len(h), 1)
        self.assertEqual(h.week.iloc[0], 2)

    def test_short_boundary_year_does_not_shift_three_year_window(self):
        c = pd.DataFrame([(w, 1963 + (w - 1) // 52, f'r{w}', f'a{w}', 1)
                          for w in range(1, 210)],
                         columns=['week', 'year', 'recordId', 'artistId', 'currentPosition'])
        _, _, s = summarize(c, 'test')
        self.assertEqual((s['startYear'], s['endYear']), (1964, 1966))
        self.assertEqual(s['entries'], 156)


if __name__ == '__main__':
    unittest.main()
