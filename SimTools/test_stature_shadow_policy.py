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


if __name__ == '__main__':
    unittest.main()
