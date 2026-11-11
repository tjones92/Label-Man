import unittest

import stature_shadow_policy as policy


def calibration():
    # Invented numbers exercise the algorithm; they are not fitted policy.
    return {'version': 'fixture-only', 'effectiveDate': '1960-01-01', 'bands': {
        name: {'entry': entry, 'exit': entry - .05,
               'reachWeight': dict.fromkeys(policy.ROUTES, weight)}
        for name, entry, weight in zip(policy.BANDS, (.2, .4, .6, .8), (0, .2, .4, .6))}}


def evidence(response=0, draw=0, reach=1):
    return {route: {'response': response, 'draw': draw, 'reach': reach} for route in policy.ROUTES}


class ShadowPolicyChecks(unittest.TestCase):
    def run_policy(self, values, previous=None):
        return policy.evaluate(values, calibration(), '1960-01-08', previous)

    def test_jump_cooling_hysteresis_and_quiet_week(self):
        self.assertEqual(self.run_policy(evidence(.85))['band'], 'Superstar')
        self.assertEqual(self.run_policy(evidence(.57), 'Star')['band'], 'Star')
        self.assertEqual(self.run_policy(evidence(.57))['band'], 'Established')
        self.assertEqual(self.run_policy(evidence(.3), 'Superstar')['band'], 'Rising')
        self.assertEqual(self.run_policy(evidence())['status'], 'below_rising')

    def test_routes_use_max_and_reach_is_continuous(self):
        values = evidence()
        values['regional'] = {'response': .5, 'draw': .5, 'reach': .5}
        result = self.run_policy(values)
        self.assertAlmostEqual(result['bands']['Established']['routes']['regional']['lower'], .45)
        self.assertEqual(result['band'], 'Established')
        values['record'] = {'response': .85, 'draw': 0, 'reach': 1}
        self.assertEqual(self.run_policy(values)['band'], 'Superstar')

    def test_missing_is_unknown_unless_known_support_decides(self):
        values = evidence()
        values['record']['draw'] = None
        self.assertEqual(self.run_policy(values)['status'], 'missing_evidence')
        values['conversion']['response'] = .9
        self.assertEqual(self.run_policy(values)['band'], 'Superstar')
        values = evidence(.1, .1, None)
        self.assertEqual(self.run_policy(values)['status'], 'below_rising')

    def test_bounded_missing_evidence_can_prove_low_standing(self):
        values = evidence(0, [0, .05], [0, .05])
        self.assertEqual(self.run_policy(values)['status'], 'below_rising')
        values['record']['draw'] = [0, .9]
        self.assertEqual(self.run_policy(values)['status'], 'missing_evidence')
        values['record']['draw'] = [.9, .1]
        with self.assertRaises(ValueError):
            self.run_policy(values)

    def test_invalid_calibration_and_dates(self):
        config = calibration()
        config['bands']['Star']['reachWeight']['record'] = 0
        with self.assertRaises(ValueError):
            policy.validate(config)
        with self.assertRaises(ValueError):
            policy.evaluate(evidence(), calibration(), '1959-12-31')
        with self.assertRaises(ValueError):
            self.run_policy(evidence(float('nan')))

    def national_calibration(self):
        config = calibration()
        for band in ('Star', 'Superstar'):
            config['bands'][band]['eligibleRoutes'] = [False, True, True]
        return config

    def test_national_eligibility_applies_to_entry_and_retention(self):
        values = evidence()
        values['regional']['draw'] = 1
        for previous in (None, 'Star', 'Superstar'):
            result = policy.evaluate(values, self.national_calibration(), '1960-01-08', previous)
            self.assertEqual(result['band'], 'Established')
            self.assertEqual(result['bands']['Star']['routes']['regional'], dict(lower=0., upper=0.))
        values['record']['response'] = 1
        self.assertEqual(policy.evaluate(values, self.national_calibration(), '1960-01-08')['band'], 'Superstar')

    def test_ineligible_missing_evidence_does_not_create_high_band_uncertainty(self):
        values = evidence()
        values['regional'] = dict(response=None, draw=None, reach=None)
        values['record']['response'] = .5
        self.assertEqual(policy.evaluate(values, self.national_calibration(), '1960-01-08')['band'], 'Established')
        values['record']['draw'] = None
        self.assertEqual(policy.evaluate(values, self.national_calibration(), '1960-01-08')['status'], 'missing_evidence')

    def test_omitted_null_and_explicit_all_eligible_preserve_legacy(self):
        for values in (evidence(.85), evidence(.57), evidence(0, None, None)):
            expected = self.run_policy(values, 'Star')
            for flags in (None, [True] * 3):
                config = calibration()
                for band in config['bands'].values():
                    band['eligibleRoutes'] = flags
                self.assertEqual(policy.evaluate(values, config, '1960-01-08', 'Star'), expected)

    def test_invalid_eligibility_is_rejected(self):
        for flags in ([], [True], [True] * 4, [False] * 3, [1, True, True], 'all'):
            config = self.national_calibration()
            config['bands']['Star']['eligibleRoutes'] = flags
            with self.subTest(flags=flags), self.assertRaises(ValueError):
                policy.validate(config)
        for flags in (None, [True] * 3):
            config = self.national_calibration()
            config['bands']['Superstar']['eligibleRoutes'] = flags
            with self.assertRaises(ValueError):
                policy.validate(config)


if __name__ == '__main__':
    unittest.main()
