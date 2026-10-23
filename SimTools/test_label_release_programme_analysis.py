import unittest

from analyze_label_release_programme import annual_issues, validate_resources, factorial_effects


class ProgrammeAnalysisChecks(unittest.TestCase):
    def test_census_excludes_opening_and_keeps_formats_and_tiers_separate(self):
        def issue(identity, kind, fmt, tier):
            return dict(recordId=identity, observationKind=kind, format=fmt, launchTier=tier, observedYear='1960')
        row = annual_issues([issue('old', 'Opening', 'Single', ''), issue('s', 'Launch', 'Single', 'Major'),
                             issue('lp', 'Launch', 'Album', 'Small')], [dict(week='1', year='1960')])[1960]
        self.assertEqual((row['singleIssues'], row['albumIssues'], row['lpToSingleRatio']), (1, 1, 1))
        self.assertEqual(row['issuesByTier']['Major']['Single'], 1)
        self.assertFalse(row['completeYear'])
        self.assertNotIn('singleIssuesToEstimate', row)

    def test_duplicate_master_issue_identity_rejected(self):
        row = dict(recordId='s', observationKind='Launch', format='Single', launchTier='Major', observedYear='1960')
        with self.assertRaises(ValueError):
            annual_issues([row, row], [])

    def test_missing_calendar_coverage_is_not_a_valid_empty_annual_report(self):
        with self.assertRaises(ValueError):
            annual_issues([], [])

    def test_promotion_cannot_exceed_the_pool(self):
        row = dict(week='1', labelId='major', promotionBooked='101', promotionPool='100',
                   cashReserved='10', attempts='2', successes='2')
        with self.assertRaises(ValueError):
            validate_resources([row], capacity=True)
        row['promotionBooked'] = '99'
        self.assertEqual(validate_resources([row], capacity=True)['multipleSuccessLabelWeeks'], 1)

    def test_factorial_separates_capacity_allocation_and_interaction(self):
        cells = {(studio, arm, 1001): {'annual': {1960: {'completeYear': True, 'singleIssues': value, 'albumIssues': 0}}}
                 for studio in ('on', 'off') for arm, value in [('control', 100), ('allocation', 110), ('capacity', 130), ('both', 145)]}
        report = factorial_effects(cells)
        self.assertFalse(report['multiSeedEvidence'])
        row = report['perSeed'][0]['effects']['singleIssues']
        self.assertEqual(row, dict(allocationMinusControl=10, capacityMinusControl=30, bothMinusControl=45, interaction=5))


if __name__ == '__main__':
    unittest.main()
