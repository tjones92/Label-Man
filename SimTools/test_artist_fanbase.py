import unittest

import pandas as pd

from analyze_artist_fanbase import validate_stages, validate_stock


class AudienceAccountingTests(unittest.TestCase):
    def test_bridge_rejects_double_counting_and_a_refreshed_title_pool(self):
        stages = pd.DataFrame(dict(week=[1, 2], recordId='r', region='east', rawDemand=[100., 90.],
            fanAnonymousRaw=[80., 75.], fanRaw=[20., 15.], clearedFanUnits=[10., 5.], clearedUnits=[50., 30.],
            fanLaunchHeads=100., fanPurchasedToDate=[10., 15.]))
        self.assertEqual(validate_stages(stages)['tracedFanUnits'], 15)
        for changed in [stages.assign(fanRaw=40.), stages.assign(clearedFanUnits=100.),
                        stages.assign(fanPurchasedToDate=[10., 5.]), stages.assign(fanLaunchHeads=[100., 150.]),
                        stages.assign(fanPurchasedToDate=101.)]:
            with self.assertRaises(ValueError):
                validate_stages(changed)

    def test_stock_keeps_departures_in_its_accounting(self):
        fans = pd.DataFrame(dict(week=[1, 2], artistId='a', attachedHeads=[10., 8.], openingHeads=2.,
            recruitedHeads=[9., 9.], departedHeads=[1., 3.], newHeadsThisWeek=[9., 0.],
            returningPurchasesThisWeek=[0., 2.]))
        self.assertEqual(validate_stock(fans)['actualReturningUnits'], 2)
        with self.assertRaisesRegex(ValueError, 'does not reconcile'):
            validate_stock(fans.assign(departedHeads=0.))
        with self.assertRaisesRegex(ValueError, 'duplicate'):
            validate_stock(pd.concat([fans, fans]))


if __name__ == '__main__':
    unittest.main()
