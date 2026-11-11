import unittest
from partition_opening_following_routes import evidence


class SourceRoutePartitionChecks(unittest.TestCase):
    def fixture(self):
        s = {k + 'Score': '0' for k in ('singleYoung', 'singleAdult', 'albumYoung', 'albumAdult', 'tvYoung', 'tvAdult', 'followingYoung', 'followingAdult')}
        s.update(followingYoungScore='1', regionalResponse='0')
        o = dict(followingYoungReference='50', followingAdultReference='50', followingYoung='120', followingAdult='0',
            openingYoung='100', openingAdult='0', actualRecordReach='1', actualRegionalDraw='1', actualConversionReach='1',
            zeroRecordReach='.2', zeroRegionalDraw='.2', zeroConversionReach='.2')
        return s, o

    def test_national_and_regional_removal_are_separate(self):
        s, o = self.fixture()
        n = evidence(s, o, national=True)
        r = evidence(s, o, regional=True)
        self.assertEqual(n['record']['draw'], .4)
        self.assertEqual(n['record']['reach'], 1)
        self.assertEqual(r['record']['draw'], 1)
        self.assertEqual(r['record']['reach'], .2)

    def test_missing_following_remains_unknown(self):
        s, o = self.fixture(); s['followingAdultScore'] = ''
        self.assertIsNone(evidence(s, o, national=True)['record']['draw'])


if __name__ == '__main__':
    unittest.main()
