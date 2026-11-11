import copy
import io
import json
import unittest
from pathlib import Path

import test_analyze_regional_standing_sensitivity as fixtures
from analyze_regional_standing_sensitivity import analyze
from verify_national_stature_policy import candidate_is_scoped, verify_world


class NationalPolicyVerificationChecks(unittest.TestCase):
    def setUp(self):
        fixture = fixtures.RegionalStandingChecks()
        fixture.setUp()
        self.legacy = fixture.config
        self.candidate = json.loads((Path(__file__).parent / 'StatureShadowNationalHighTiers.json').read_text())
        replay, self.snapshots, marketing = fixture.fixture()
        self.world = analyze(replay, self.snapshots, marketing, self.legacy)
        self.world['_geography'] = {(r['date'], r['artistId'], r['variant']): r for r in replay['weekly']}

    def test_candidate_only_changes_versions_and_eligibility(self):
        candidate_is_scoped(self.legacy, self.candidate)
        for field in ('FollowingPerMillionReference', 'LocalSalesShareReference'):
            changed = copy.deepcopy(self.candidate)
            changed[field] *= 2
            with self.assertRaises(ValueError):
                candidate_is_scoped(self.legacy, changed)
        changed = copy.deepcopy(self.candidate)
        changed['Policy']['Bands'][2]['EligibleRoutes'] = [True] * 3
        with self.assertRaises(ValueError):
            candidate_is_scoped(self.legacy, changed)

    def test_shared_kernel_matches_reference_and_emits_csharp_cases(self):
        stream = io.StringIO()
        counts = verify_world(self.world, self.snapshots, self.legacy, self.candidate, stream, 'fixture')
        self.assertEqual(counts['verifiedCases'], 12)
        cases = [json.loads(line) for line in stream.getvalue().splitlines()]
        candidate_case = next(r for r in cases if r['calibration'] == 'candidate' and '/home_only/' in r['key'])
        self.assertEqual(candidate_case['expected']['band'], 'Established')
        self.assertEqual(candidate_case['expected']['supports'][2][3:5], [0, 0])

    def test_corrupted_reference_support_or_history_fails(self):
        for field in ('support', 'history'):
            changed = copy.deepcopy(self.world)
            if field == 'support':
                changed['weekly'][0]['supports']['Star']['lower'] += .1
            else:
                changed['weekly'][0]['previousBand'] = 'Star'
            with self.subTest(field=field), self.assertRaises(ValueError):
                verify_world(changed, self.snapshots, self.legacy, self.candidate, io.StringIO(), 'fixture')

    def test_duplicate_or_missing_source_observations_fail(self):
        for snapshots in (self.snapshots * 2, []):
            with self.assertRaises(ValueError):
                verify_world(self.world, snapshots, self.legacy, self.candidate, io.StringIO(), 'fixture')


if __name__ == '__main__':
    unittest.main()
