import unittest
from trace_stature_marketing_mechanisms import sensitivity


class FollowingMagnitudeScreen(unittest.TestCase):
    def fixture(self):
        row = {k + 'Score': '0' for k in ('singleYoung', 'singleAdult', 'albumYoung', 'albumAdult',
            'tvYoung', 'tvAdult', 'followingYoung', 'followingAdult')}
        row.update(regionalResponse='0', regionalDraw='0', recordReach='1', conversionReach='1', followingYoungScore='1')
        for band, boundary in zip(('Rising', 'Established', 'Star', 'Superstar'), (.1, .2, .5, .8)):
            row[band + 'Boundary'] = str(boundary)
        return row, {'Bands': [{'ReachWeights': [0, 0, 0]} for _ in range(4)]}

    def test_following_led_support_and_frozen_response(self):
        row, policy = self.fixture()
        self.assertEqual(sensitivity(row, 1, policy), 'Superstar')
        self.assertEqual(sensitivity(row, .5, policy), 'Star')
        self.assertEqual(sensitivity(row, 0, policy), 'BelowRising')
        row['singleAdultScore'] = '1'
        self.assertEqual(sensitivity(row, 0, policy), 'Superstar')

    def test_regional_following_is_also_discounted(self):
        row, policy = self.fixture()
        row.update(followingYoungScore='0', regionalDraw='1')
        self.assertEqual(sensitivity(row, 1, policy), 'Superstar')
        self.assertEqual(sensitivity(row, 0, policy), 'BelowRising')

    def test_screen_respects_national_high_tier_eligibility(self):
        row, policy = self.fixture()
        row.update(followingYoungScore='0', regionalDraw='1')
        for band in policy['Bands'][2:]:
            band['EligibleRoutes'] = [False, True, True]
        self.assertEqual(sensitivity(row, 1, policy), 'Established')
        row['singleAdultScore'] = '1'
        self.assertEqual(sensitivity(row, 1, policy), 'Superstar')


if __name__ == '__main__':
    unittest.main()
