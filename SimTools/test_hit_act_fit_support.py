import unittest

import pandas as pd

from analyze_hit_act_fit_support import career_progress
from analyze_hit_act_fit_discrepancies import rank_bands


class CareerContinuityTests(unittest.TestCase):
    def test_disbanding_does_not_count_as_a_contract_continuity_failure(self):
        cohort = pd.DataFrame([('a', 'field', 5)], columns=['artistId', 'group', 'hitWeek'])
        rows = pd.DataFrame([dict(artistId='a', week=w, careerState='Rising' if w < 10 else 'Established' if w < 31 else 'Disbanded',
            lifecycleStatus='Active' if w < 31 else 'Disbanded', labelId='label' if w < 31 else '',
            publicRecognition=.1, top40Hits=1 if w < 10 else 3, top10Hits=1 if w < 10 else 2, cohort='RuntimeFormation') for w in range(5, 58)])
        result = career_progress(cohort, rows).iloc[0]
        self.assertEqual(result.everNewEstablished, 1)
        self.assertEqual(result.promotedThenUnowned, 0)
        self.assertEqual(result.promotedThenTerminal, 1)
        self.assertEqual(result.activeUnownedWeeks, 0)

    def test_peak_bands_keep_misses_and_exclude_ineligible_late_launches(self):
        releases = pd.DataFrame(dict(arm='off', seed=1001, group='field',
            peakIn52=[0, 10, 40, 60, 100, 1], entry13Eligible=[1, 1, 1, 1, 1, 0], quality=.7))
        rows = rank_bands(releases).set_index('peakBand')
        self.assertEqual(rows.launches.sum(), 5)
        self.assertAlmostEqual(rows.share.sum(), 1)
        self.assertEqual(rows.loc['miss', 'launches'], 1)
        self.assertEqual(rows.loc['1-10', 'launches'], 1)
        self.assertEqual(rows.loc['61-100', 'launches'], 1)
        self.assertTrue(rows.bindingShare.isna().all())
        with self.assertRaisesRegex(ValueError, 'Missing/invalid'):
            rank_bands(releases.assign(peakIn52=float('nan')))

    def test_decline_and_silent_acts_stay_in_the_endpoint_cohort(self):
        cohorts = pd.DataFrame([('a', 'field', 5), ('b', 'field', 6), ('c', 'field', 7)], columns=['artistId', 'group', 'hitWeek'])
        careers = pd.DataFrame([
            *[('a', w, 'Rising' if w < 10 else 'Established', .10 + .15 * (w - 5) / 52, 1 if w < 10 else 3, 1 if w < 10 else 2, 'label') for w in range(5, 58)],
            *[('b', w, 'Established' if w < 58 else 'Dropped', .30 - .04 * (w - 6) / 52, 3, 2, 'label' if w < 58 else '') for w in range(6, 59)],
            *[('c', w, 'Rising' if w < 10 else 'Established' if w < 41 else 'Dropped', .1, 1 if w < 10 else 3, 1 if w < 10 else 2, 'label' if w < 41 else '') for w in range(7, 60)],
        ], columns=['artistId', 'week', 'careerState', 'publicRecognition', 'top40Hits', 'top10Hits', 'labelId'])
        careers['cohort'] = 'RuntimeFormation'
        rows = career_progress(cohorts, careers).set_index('artistId')
        self.assertEqual(set(rows.index), {'a', 'b', 'c'})
        self.assertEqual(rows.loc['a', 'newEstablished'], 1)
        self.assertAlmostEqual(rows.loc['a', 'recognitionChange'], .15)
        self.assertEqual(rows.loc['b', 'declinedAtEnd'], 1)
        self.assertEqual(rows.loc['b', 'creditedTop40s'], 0)
        self.assertEqual(rows.loc['a', 'firstCareerTop10'], 1)
        self.assertEqual(rows.loc['b', 'firstCareerTop10'], 0)
        self.assertEqual(rows.loc['c', 'newEstablished'], 0)
        self.assertEqual(rows.loc['c', 'everNewEstablished'], 1)
        self.assertEqual(rows.loc['c', 'promotedThenUnowned'], 1)
        with self.assertRaisesRegex(ValueError, 'Incomplete career trajectory'):
            career_progress(cohorts, careers[~((careers.artistId == 'b') & (careers.week == 58))])
        with self.assertRaisesRegex(ValueError, 'Incomplete career trajectory'):
            career_progress(cohorts, careers[~((careers.artistId == 'a') & (careers.week == 30))])


if __name__ == '__main__':
    unittest.main()
