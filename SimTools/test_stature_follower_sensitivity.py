import unittest

from stature_follower_sensitivity import discount


class FollowerSensitivityChecks(unittest.TestCase):
    def test_discount_preserves_age_proportions_and_limits(self):
        young, adult = discount(60, 40, 25)
        self.assertAlmostEqual(young, 45)
        self.assertAlmostEqual(adult, 30)
        self.assertEqual(discount(0, 0, 0), (0, 0))
        self.assertEqual(discount(2, 3, 5), (0, 0))
        with self.assertRaises(ValueError):
            discount(2, 3, 6)


if __name__ == '__main__':
    unittest.main()
