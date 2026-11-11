import copy
import json
import unittest
from pathlib import Path

from analyze_regional_standing_sensitivity import analyze, evidence, evaluate_mode, reach_requirements, MODES
from partition_opening_following_routes import policy
from replay_opening_geography import VARIANTS


class RegionalStandingChecks(unittest.TestCase):
    def setUp(self):
        self.config = json.loads((Path(__file__).parent / 'StatureShadowExperimental.json').read_text())
        self.snapshot = dict(snapshotDate='1960-07-01', artistId='x', shadowBand='Rising', status='Observed',
            regionalResponse='0', followingYoungScore='0', followingAdultScore='0',
            **{k + 'Score': '0' for k in ('singleYoung', 'singleAdult', 'albumYoung', 'albumAdult', 'tvYoung', 'tvAdult')})
        self.source = dict(regionalDraw=1., recordReach=.04, conversionReach=.04)

    def evaluate(self, mode, previous=None, source=None, snapshot=None):
        return evaluate_mode(evidence(snapshot or self.snapshot, source or self.source, mode),
            policy(self.config), '1960-07-01', previous, mode)

    def test_regional_concentration_cannot_alone_make_national_star(self):
        self.assertEqual(self.evaluate('full')['band'], 'Star')
        self.assertEqual(self.evaluate(MODES[2])['band'], 'Established')
        self.assertEqual(self.evaluate('without_regional')['status'], 'below_rising')

    def test_national_record_evidence_can_still_make_star(self):
        snapshot = {**self.snapshot, 'singleYoungScore': '1'}
        self.assertEqual(self.evaluate(MODES[2], snapshot=snapshot)['band'], 'Star')

    def test_regional_evidence_cannot_retain_high_band_from_prior_history(self):
        for prior in ('Star', 'Superstar'):
            with self.subTest(prior=prior):
                self.assertEqual(self.evaluate(MODES[2], previous=prior)['band'], 'Established')

    def test_national_evidence_can_make_superstar(self):
        snapshot = {**self.snapshot, 'singleYoungScore': '1'}
        source = {**self.source, 'recordReach': .9}
        self.assertEqual(self.evaluate(MODES[2], snapshot=snapshot, source=source)['band'], 'Superstar')

    def test_missing_national_evidence_remains_unknown(self):
        snapshot = {**self.snapshot, 'singleYoungScore': ''}
        self.assertEqual(self.evaluate(MODES[2], snapshot=snapshot)['status'], 'missing_evidence')

    def test_regional_response_is_disabled_as_well_as_draw(self):
        snapshot = {**self.snapshot, 'regionalResponse': '1'}
        source = {**self.source, 'regionalDraw': 0.}
        self.assertEqual(self.evaluate('full', source=source, snapshot=snapshot)['band'], 'Star')
        self.assertEqual(self.evaluate('without_regional', source=source, snapshot=snapshot)['status'], 'below_rising')

    def test_unit_magnitude_support_floor_explains_entry_and_retention(self):
        star = reach_requirements(self.config)[2]
        self.assertAlmostEqual(star['entryMinimumReachAtUnitMagnitude'], .03180827784345186)
        self.assertEqual(star['exitMinimumReachAtUnitMagnitude'], 0)
        self.assertEqual(self.evaluate('full', source={**self.source, 'recordReach': 0.}, previous='Star')['band'], 'Star')
        self.assertEqual(self.evaluate('full', source={**self.source, 'recordReach': 0.})['band'], 'Established')

    def fixture(self):
        weekly = [dict(date='1960-07-01', artistId='x', variant=v, band='Rising' if v == 'neutral' else 'Star',
                       status='Observed', **{**self.source, 'regionalDraw': .1 if v == 'neutral' else 1.}) for v in VARIANTS]
        marketing = [dict(artistId='x', context='release-launch', status='Available', date='7/1/1960',
            snapshotDate='1960-07-01', legacyCareer='Rising', legacyBudget='120', actualProposedBudget='120')]
        return dict(weekly=weekly), [self.snapshot], marketing

    def test_complete_comparison_uses_independent_trajectories_and_like_policy_baselines(self):
        replay, snapshots, marketing = self.fixture()
        before = copy.deepcopy(replay)
        report = analyze(replay, snapshots, marketing, self.config)
        self.assertEqual(replay, before)
        self.assertEqual(len(report['weekly']), 12)
        row = next(r for r in report['launchBudgetReads'] if r['variant'] == 'home_only')
        self.assertEqual(row['fullGeographyHigherThanNeutral'], 1)
        # Regional evidence still makes Established: this candidate reduces, but does not erase, the increase.
        self.assertEqual(row[MODES[2] + 'GeographyHigherThanNeutral'], 1)
        self.assertEqual(row['without_regionalGeographyHigherThanNeutral'], 0)
        self.assertEqual(row['nationalHighBandsLowerThanFull'], 1)

    def test_duplicate_unmatched_initial_missing_and_bad_neutral_fail(self):
        for corruption in ('duplicate', 'unmatched', 'initial_missing', 'neutral'):
            replay, snapshots, marketing = self.fixture()
            if corruption == 'duplicate':
                snapshots *= 2
            elif corruption == 'unmatched':
                replay['weekly'].append({**replay['weekly'][0], 'date': '1960-07-08'})
            elif corruption == 'initial_missing':
                snapshots[0] = {**snapshots[0], 'snapshotDate': '1960-07-08'}
            else:
                marketing[0]['actualProposedBudget'] = '200'
            with self.subTest(corruption=corruption), self.assertRaises((ValueError, KeyError)):
                analyze(replay, snapshots, marketing, self.config)

    def test_national_following_can_still_supply_high_standing_without_current_sales(self):
        snapshot = {**self.snapshot, 'followingYoungScore': '1'}
        self.assertEqual(self.evaluate(MODES[2], snapshot=snapshot)['band'], 'Star')


if __name__ == '__main__':
    unittest.main()
