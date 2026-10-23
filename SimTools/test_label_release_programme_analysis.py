import unittest
import json
import tempfile
from pathlib import Path

from analyze_label_release_programme import annual_issues, validate_resources, factorial_effects, compare_control_reference, factorial_bundle, validate_slate_transition, validate_weekly_reviews


class ProgrammeAnalysisChecks(unittest.TestCase):
    def test_deferred_work_waits_a_week_and_releases_holds(self):
        failed = dict(week='5', slotId='s', state='Postponed', reason='idle-preparation-material-economics-or-funding',
                      weeklyReviewPolicy='defer-failed-work', reservedProduction='0', reservedMarketing='0', reservedPressing='0')
        ready = dict(failed, state='Ready', reason='master-recorded-and-production-paid')
        with self.assertRaisesRegex(ValueError, 'retried'):
            validate_weekly_reviews([failed, ready])
        self.assertEqual(validate_weekly_reviews([failed, dict(ready, week='6')])['deferredItemWeeks'], 1)
        validate_weekly_reviews([failed, dict(ready, slotId='other')])
        validate_weekly_reviews([failed, dict(ready, state='Cancelled')])
        with self.assertRaisesRegex(ValueError, 'unspent holds'):
            validate_weekly_reviews([dict(failed, reservedPressing='100')])
        # A rush review preserves the previous state but is a date change, not a failed attempt.
        validate_weekly_reviews([dict(failed, reason='observed-breakout-rush-review'), ready])
        legacy = {k: v for k, v in failed.items() if k != 'weeklyReviewPolicy'}
        self.assertEqual(validate_weekly_reviews([legacy, legacy])['guardedReviewRows'], 0)
        with self.assertRaisesRegex(ValueError, 'Unknown'):
            validate_weekly_reviews([dict(failed, weeklyReviewPolicy='unknown')])

    def test_actual_recording_lead_including_advance_single_and_opening_exception(self):
        row = dict(state='Issued', format='Single', actualWeek='10', actualDate='date', readyWeek='7', recordId='s',
                   preparedWeek='10', proposedWeek='5', intendedWeek='8', reason='issued')
        with self.assertRaisesRegex(ValueError, 'issue lead'):
            validate_slate_transition(row, {'s': dict(observedAuditWeek='10')})
        row['actualWeek'] = '11'
        validate_slate_transition(row, {'s': dict(observedAuditWeek='11')})
        row.update(actualWeek='10', proposedWeek='10', intendedWeek='10')
        validate_slate_transition(row, {'s': dict(observedAuditWeek='10')})
        row.update(state='AwaitingDelivery', format='Album', actualWeek='0', actualDate='', week='10',
                   proposedWeek='5', intendedWeek='8', reservedProduction='0', reservedMarketing='0', reservedPressing='0')
        with self.assertRaisesRegex(ValueError, 'issue lead'):
            validate_slate_transition(row, {})
        row['week'] = '11'
        validate_slate_transition(row, {})
        row.pop('preparedWeek')
        validate_slate_transition(row, {})  # Earlier logs remain readable.

    def test_future_promotion_holds_cannot_overbook_even_when_current_spend_fits(self):
        row = dict(week='1', labelId='major', promotionBooked='30', promotionPool='100',
                   promotionCommittedPeak='110', cashReserved='10', attempts='1', successes='1')
        with self.assertRaisesRegex(ValueError, 'Future promotion overbooking'):
            validate_resources([row], capacity=True)
        row['promotionCommittedPeak'] = '100'
        self.assertEqual(validate_resources([row], capacity=True)['labelWeeksWithFuturePromotionAudit'], 1)
        row['promotionCommittedPeak'] = '20'
        with self.assertRaisesRegex(ValueError, 'omits current campaigns'):
            validate_resources([row], capacity=True)

    def test_delayed_lp_requires_actual_delivery_and_no_duplicate_holds(self):
        row = dict(state='AwaitingDelivery', format='Album', actualWeek='0', actualDate='', readyWeek='3', recordId='lp',
                   reservedProduction='0', reservedMarketing='0', reservedPressing='0', reason='advance-single-issued-awaiting-lp')
        validate_slate_transition(row, {})
        row['reservedPressing'] = '100'
        with self.assertRaises(ValueError):
            validate_slate_transition(row, {})
        row.update(reservedPressing='0', actualWeek='4', actualDate='1/29/1960')
        with self.assertRaises(ValueError):
            validate_slate_transition(row, {})
        row.update(state='Issued', reason='issued-delayed-lp')
        with self.assertRaises(ValueError):
            validate_slate_transition(row, {})
        with self.assertRaises(ValueError):
            validate_slate_transition(row, {'lp': dict(observedAuditWeek='6')})
        row['actualWeek'] = '6'
        validate_slate_transition(row, {'lp': dict(observedAuditWeek='6')})

    def test_adopted_bundle_can_reconstruct_explicit_factorial_control(self):
        treatments = ['--label-release-slate', '--label-release-capacity', '--studio-parent-labels']
        base = ['--hit-act-cadence', '--hit-act-retention', '--midtier-national-ar']
        snapshot = dict(profile='game', bundle=base + treatments)
        self.assertEqual(factorial_bundle(snapshot, treatments), base)
        self.assertEqual(factorial_bundle(dict(profile='game', bundle=base), []), base)
        with self.assertRaises(ValueError):
            factorial_bundle(snapshot, ['--midtier-national-ar'])

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

    def test_reference_replay_detects_changed_missing_and_added_streams(self):
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parent.parent / 'SimLogs') as directory:
            root = Path(directory)
            for name in ('screen', 'native', 'reference'):
                (root / name).mkdir()
            plan = dict(weeks=265, seeds=[1001], bundle=['--hit-act-cadence'],
                        groups=[dict(studio='off', arm='control', tag='native')])
            manifest = dict(weeks=265, profile='game', modes=['default'], extraFlags=plan['bundle'],
                            results=[dict(seed=1001, passed=True, name='old')])
            (root / 'screen/plan.json').write_text(json.dumps(plan))
            (root / 'reference/runs.json').write_text(json.dumps(manifest))
            manifest['results'][0]['name'] = 'new'
            (root / 'native/runs.json').write_text(json.dumps(manifest))
            for name in ('old', 'new'):
                (root / f'{name}-records.csv').write_text('same\n')
            def compare():
                return compare_control_reference(root / 'screen/plan.json', root / 'reference/runs.json')['seeds'][0]
            self.assertTrue(compare()['byteIdentical'])
            (root / 'old-performance-profile.csv').write_text('old timing\n')
            (root / 'new-performance-profile.csv').write_text('new timing\n')
            self.assertFalse(compare()['byteIdentical'])
            self.assertTrue(compare()['simulationStreamsByteIdentical'])
            (root / 'new-records.csv').write_text('changed\n')
            (root / 'old-missing.csv').write_text('missing\n')
            (root / 'new-added.csv').write_text('added\n')
            result = compare()
            self.assertFalse(result['byteIdentical'])
            self.assertEqual(result['changedStreams'], ['performance-profile.csv', 'records.csv'])
            self.assertEqual(result['missingStreams'], ['missing.csv'])
            self.assertEqual(result['addedStreams'], ['added.csv'])
            manifest['extraFlags'] = []
            (root / 'reference/runs.json').write_text(json.dumps(manifest))
            with self.assertRaises(ValueError):
                compare()


if __name__ == '__main__':
    unittest.main()
