import csv
import tempfile
import unittest
from pathlib import Path

from analyze_demand_attention import summarize, quality_band


class DemandAttentionTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.prefix = Path(self.directory.name) / 'audit'
        self.clearing = dict(week=1, regionId='home', localClearedSingleUnits=50,
                             localClearedAlbumUnits=20, serviceableSingleIntent=100, serviceableAlbumIntent=20,
                             singleFormatBudget=50, albumFormatBudget=30, clearedSingleUnits=60,
                             clearedAlbumUnits=20, localCleared=70, spilloverCleared=10, clearedTotalUnits=80,
                             marketDisplacedDemand=40, inventoryViolationCount=0, allocationViolationCount=0,
                             reconciliationDelta=0, settlementDelta=0)
        self.outcome = dict(week=20, recordId='record', format='Single', genre='Pop', memoryEligible='true', sunkMarketingCost=50,
                            lifetimeLabelNet=200, sunkProductionCost=100, realizedNet=50,
                            bookedContributionBeforeMarketing=100, bookedContributionAfterMarketing=50,
                            unitsPressed=500, pressingBookSold=100)

    def write(self, suffix, values, fields=None):
        with Path(f'{self.prefix}-{suffix}.csv').open('w', newline='', encoding='utf-8') as stream:
            writer = csv.DictWriter(stream, fieldnames=fields or list(values[0]))
            writer.writeheader()
            writer.writerows(values)

    def audit(self, clearing=None, outcomes=None):
        if clearing is None:
            clearing = [self.clearing]
            window_path = Path(f'{self.prefix}-demand-observation-window.csv')
            if window_path.exists():
                with window_path.open(newline='') as stream:
                    window = next(csv.DictReader(stream))
                clearing = [self.clearing | dict(week=week) for week in
                            range(int(window['startAuditWeek']) + 1, int(window['endAuditWeek']) + 1)]
        self.write('market-clearing-weekly', clearing, list(self.clearing))
        self.write('release-outcomes', outcomes if outcomes is not None else [self.outcome], list(self.outcome))
        return summarize(self.prefix)

    def test_local_rationing_and_final_displacement_are_distinct(self):
        report = self.audit()
        singles = report['clearing']['byFormat']['Single']
        self.assertEqual(singles['localDisplaced'], 50)
        self.assertEqual(singles['residualDisplaced'], 40)
        self.assertEqual(singles['spilloverCleared'], 10)
        self.assertEqual(singles['localBudgetBindingShare'], 1)
        self.assertEqual(report['clearing']['byFormat']['Album']['localBudgetBindingShare'], 0)

    def test_marketing_loss_denominator_excludes_unknown_but_includes_zero(self):
        report = self.audit(outcomes=[self.outcome | dict(sunkMarketingCost=150, realizedNet=-50,
                                                        bookedContributionAfterMarketing=-50),
                                     self.outcome | dict(recordId='unknown', sunkMarketingCost='',
                                                         bookedContributionAfterMarketing='', realizedNet=100),
                                     self.outcome | dict(recordId='free', sunkMarketingCost=0,
                                                         bookedContributionAfterMarketing=100, realizedNet=100)])
        group = report['releases']['groups']['format:Single']
        self.assertEqual(group['lossBeforeMarketingShare'], 0)
        self.assertEqual(group['lossAfterMarketingShare'], .5)
        self.assertEqual(group['unknownMarketing'], 1)
        self.assertEqual(group['unsoldBookedCopies'], 1200)

    def test_no_final_outcomes_does_not_claim_zero_loss(self):
        report = self.audit(outcomes=[])['releases']
        self.assertEqual(report['finalized'], 0)
        self.assertEqual(report['groups'], {})
        self.assertIsNone(report['attributedCampaignCoverage'])

    def test_launch_coverage_counts_unfinished_without_imputing_profit(self):
        self.write('release-campaign-costs', [dict(recordId='record'), dict(recordId='unfinished')])
        self.assertEqual(self.audit()['releases']['attributedCampaignCoverage'],
                         dict(observedLaunchCampaigns=2, finalized=1, notFinalizedInWindow=1))

    def test_unattributed_marketing_is_not_imputed(self):
        report = self.audit(outcomes=[self.outcome | dict(sunkMarketingCost='',
                                                        bookedContributionAfterMarketing='', realizedNet=100)])
        self.assertIsNone(report['releases']['groups']['format:Single']['lossAfterMarketingShare'])

    def test_rejects_broken_reconciliation_and_duplicate_observations(self):
        for field in ('localCleared', 'clearedTotalUnits', 'spilloverCleared', 'marketDisplacedDemand',
                      'inventoryViolationCount', 'allocationViolationCount', 'reconciliationDelta', 'settlementDelta'):
            with self.subTest(field=field), self.assertRaises(ValueError):
                self.audit(clearing=[self.clearing | {field: self.clearing[field] + 1}])
        with self.assertRaises(ValueError):
            self.audit(clearing=[self.clearing, self.clearing])
        with self.assertRaises(ValueError):
            self.audit(outcomes=[self.outcome, self.outcome])

    def test_rejects_nonfinite_costs_and_oversold_book(self):
        for change in (dict(sunkMarketingCost='nan'), dict(bookedContributionBeforeMarketing='inf'),
                       dict(bookedContributionAfterMarketing=100), dict(pressingBookSold=501),
                       dict(unitsPressed=500.5), dict(sunkMarketingCost=-1)):
            with self.subTest(change=change), self.assertRaises(ValueError):
                self.audit(outcomes=[self.outcome | change])

    def test_requires_new_columns_instead_of_guessing_old_boundary(self):
        self.audit()
        old = {key: value for key, value in self.outcome.items() if key != 'sunkMarketingCost'}
        self.write('release-outcomes', [old])
        with self.assertRaisesRegex(ValueError, 'missing Stage A columns'):
            summarize(self.prefix)

    def census(self, changes=None, window_changes=None):
        base = dict(recordId='record', format='Single', genre='Pop', observationKind='Launch',
                    observedAuditWeek=2, observedYear=1960, launchTier='Small', runtimeQualityAtLaunch=.6,
                    status='Retired', retiredAuditWeek=20)
        entries = [base,
                   base | dict(recordId='unfinished', observedAuditWeek=14, launchTier='Major',
                               runtimeQualityAtLaunch=.9, status='Active', retiredAuditWeek=''),
                   base | dict(recordId='opening', observationKind='Opening', observedAuditWeek=0,
                               launchTier='', runtimeQualityAtLaunch='', status='Active', retiredAuditWeek='')]
        if changes:
            for index, change in changes.items():
                entries[index].update(change)
        window = dict(startAuditWeek=0, endAuditWeek=26, completed='true', openingRecords=1,
                      launchedRecords=2, activeRecords=2, retiredRecords=1, missingRecords=0)
        window.update(window_changes or {})
        self.write('demand-release-census', entries)
        self.write('demand-observation-window', [window])

    def test_census_excludes_opening_and_counts_active_launches(self):
        self.census()
        cohort = self.audit()['releases']['launchCohorts']
        self.assertEqual(cohort['coverage']['selectedLaunches'], 2)
        self.assertEqual(cohort['coverage']['openingRecords'], 1)
        total = cohort['groups']['format:Single']
        self.assertEqual(total['launched'], 2)
        self.assertEqual(total['activeAtEnd'], 1)
        self.assertEqual(total['finalizedShare'], .5)
        self.assertIsNone(total['cohortLifetimeLossBeforeMarketingShare'])
        self.assertEqual(cohort['groups']['formatTier:Single|Small']['finalized'], 1)
        self.assertIsNone(cohort['groups']['formatTier:Single|Major']['finalizedLossBeforeMarketingShare'])
        self.assertIn('formatQuality:Single|>0.5 to 0.6', cohort['groups'])

    def test_cutoff_selects_early_launches_with_full_followup(self):
        self.census()
        self.audit()
        cohort = summarize(self.prefix, launch_through_week=13)['releases']['launchCohorts']
        self.assertEqual(cohort['coverage']['launchedAfterCutoff'], 1)
        self.assertEqual(cohort['coverage']['selectedLaunches'], 1)
        self.assertEqual(cohort['endAuditWeek'], 26)
        total = cohort['groups']['format:Single']
        self.assertTrue(total['completeCohort'])
        self.assertEqual(total['cohortLifetimeLossBeforeMarketingShare'], 0)
        self.assertEqual(total['cohortLifetimeLossAfterMarketingShare'], 0)

    def test_rejects_census_missing_outcome_and_false_completion(self):
        for changes, window in (({0: dict(status='Active', retiredAuditWeek='')}, None),
                                ({1: dict(status='Retired', retiredAuditWeek=25)}, None),
                                ({1: dict(recordId='record')}, None),
                                ({0: dict(retiredAuditWeek=19)}, None),
                                ({0: dict(runtimeQualityAtLaunch='nan')}, None),
                                ({2: dict(launchTier='Small')}, None),
                                (None, dict(completed='false')),
                                (None, dict(launchedRecords=3))):
            with self.subTest(changes=changes, window=window), self.assertRaises(ValueError):
                self.census(changes, window)
                self.audit()

    def test_cutoff_requires_census_and_valid_window(self):
        self.audit()
        with self.assertRaisesRegex(ValueError, 'requires the actual-release census'):
            summarize(self.prefix, 13)
        self.census()
        for cutoff in (0, 27):
            with self.subTest(cutoff=cutoff), self.assertRaises(ValueError):
                summarize(self.prefix, cutoff)

    def test_quality_boundaries_match_research_runtime_bins(self):
        self.assertEqual(quality_band(.4), '<=0.4')
        self.assertEqual(quality_band(.5), '>0.4 to 0.5')
        self.assertEqual(quality_band(.6), '>0.5 to 0.6')
        self.assertEqual(quality_band(.7), '>0.6 to 0.7')
        self.assertEqual(quality_band(.7001), '>0.7')

    def test_rejects_partial_clearing_window_and_missing_regional_rows(self):
        self.census()
        with self.assertRaisesRegex(ValueError, 'does not match'):
            self.audit(clearing=[self.clearing])
        with self.assertRaisesRegex(ValueError, 'Incomplete weekly clearing'):
            self.audit(clearing=[self.clearing, self.clearing | dict(week=3)])
        with self.assertRaisesRegex(ValueError, 'inconsistent regional panel'):
            self.audit(clearing=[self.clearing, self.clearing | dict(regionId='other'),
                                 self.clearing | dict(week=2)])

    def test_resumed_observation_uses_absolute_weeks_and_new_opening_catalog(self):
        self.census({0: dict(observedAuditWeek=54, retiredAuditWeek=72),
                     1: dict(observedAuditWeek=66), 2: dict(observedAuditWeek=52)},
                    dict(startAuditWeek=52, endAuditWeek=78))
        self.audit(outcomes=[self.outcome | dict(week=72)])
        cohort = summarize(self.prefix, 65)['releases']['launchCohorts']
        self.assertEqual(cohort['coverage']['selectedLaunches'], 1)
        self.assertEqual(cohort['coverage']['openingRecords'], 1)
        self.assertIn('formatLaunchCohort:Single|53-65', cohort['groups'])

    def test_complete_cohort_with_unknown_marketing_has_no_all_in_loss_rate(self):
        self.census()
        self.audit(outcomes=[self.outcome | dict(sunkMarketingCost='',
                                                bookedContributionAfterMarketing='', realizedNet=100)])
        group = summarize(self.prefix, 13)['releases']['launchCohorts']['groups']['format:Single']
        self.assertTrue(group['completeCohort'])
        self.assertEqual(group['cohortLifetimeLossBeforeMarketingShare'], 0)
        self.assertIsNone(group['cohortLifetimeLossAfterMarketingShare'])


if __name__ == '__main__':
    unittest.main()
