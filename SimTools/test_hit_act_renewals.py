import unittest

import pandas as pd

from analyze_hit_act_renewals import read_lines, validate


class RenewalAccountingTests(unittest.TestCase):
    def test_shadow_cannot_act_and_active_advance_must_be_paid(self):
        line = ('HIT_ACT_RENEWAL week=53 artist=a label=l tier=MidTier wants=True original=False '
                'eligible=True proposed=True applied=False cash=1000 advance=100 fullReserve=1200 '
                'goingReserve=600 taxService=800 nearTaxDue=200 cashFlow=10 contractHits=1 sinceHit=2 audience=10000')
        shadow, empty = read_lines([line])
        validate(shadow, empty, active=False)
        active = shadow.assign(applied=True)
        receipt = pd.DataFrame([dict(week=53, artist='a', label='l', advance=100., cashAfter=900.)])
        validate(active, receipt, active=True)
        with self.assertRaisesRegex(ValueError, 'paid advance'):
            validate(active, empty, active=True)
        with self.assertRaisesRegex(ValueError, 'failed to pay'):
            validate(active, receipt.assign(cashAfter=999.), active=True)
        with self.assertRaisesRegex(ValueError, 'outside'):
            validate(active, receipt, active=False)
        with self.assertRaisesRegex(ValueError, 'components'):
            validate(active.assign(goingReserve=500.), receipt, active=True)
        with self.assertRaisesRegex(ValueError, 'ineligible'):
            validate(active.assign(contractHits=0), receipt, active=True)


if __name__ == '__main__':
    unittest.main()
