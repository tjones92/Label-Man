import csv
import tempfile
import unittest
from pathlib import Path

from analyze_demand_attention import physical_inventory_summary


class PhysicalInventoryTests(unittest.TestCase):
    def setUp(self):
        directory = tempfile.TemporaryDirectory()
        self.addCleanup(directory.cleanup)
        self.prefix = Path(directory.name) / 'audit'
        self.row = dict(week=1, recordId='r', format='Single', status='Active', openingUnits=0,
                        productionReceived=500, unitsPressed=500, bookSold=100, warehouse=100,
                        shelves=200, heldForArrival=100, shipments=400, saleableReturns=0, heldReturns=0,
                        promotionalWithdrawals=0, damagedCopies=0, terminalWriteOffs=0,
                        distributedWriteOffs=0, productionOrdered=500, productionDueWeek=2,
                        unbilledInitial=0, unbilledRepress=0, unfilledShipmentRequests=200, reconciliationDelta=0)
        self.write('demand-release-census', [dict(recordId='r', status='Active', retiredAuditWeek='')])
        self.write('demand-observation-window', [dict(endAuditWeek=1, completed='true')])
        self.write('release-outcomes', [], ['recordId', 'unitsPressed', 'pressingBookSold'])

    def write(self, suffix, data, fields=None):
        with Path(f'{self.prefix}-{suffix}.csv').open('w', newline='') as stream:
            writer = csv.DictWriter(stream, fieldnames=fields or list(data[0]))
            writer.writeheader()
            writer.writerows(data)

    def summary(self, data=None):
        self.write('physical-inventory', data or [self.row])
        return physical_inventory_summary(self.prefix)

    def test_orders_are_not_receipts_and_held_units_are_conserved(self):
        group = self.summary()['byFormat']['Single']
        self.assertEqual(group['productionReceived'], 500)
        self.assertEqual(group['productionOrdered'], 500)
        self.assertEqual(group['heldForArrival'], 100)

    def test_phantom_shelf_copies_are_rejected(self):
        with self.assertRaisesRegex(ValueError, 'unit conservation'):
            self.summary([self.row | dict(shelves=201)])

    def test_reported_zero_cannot_hide_unbacked_shipments(self):
        with self.assertRaisesRegex(ValueError, 'shipment conservation'):
            self.summary([self.row | dict(shipments=401)])

    def test_return_damage_and_promo_are_distinct(self):
        report = self.summary([self.row | dict(shelves=100, warehouse=170, saleableReturns=80,
                                              damagedCopies=20, promotionalWithdrawals=10)])
        self.assertEqual(report['byFormat']['Single']['bookSold'], 100)

    def test_retired_inventory_matches_outcome(self):
        self.write('demand-release-census', [dict(recordId='r', status='Retired', retiredAuditWeek=1)])
        self.write('release-outcomes', [dict(recordId='r', unitsPressed=500, pressingBookSold=100)])
        self.summary([self.row | dict(status='Retired', warehouse=0, shelves=0, heldForArrival=0,
                                      terminalWriteOffs=400, distributedWriteOffs=300,
                                      productionOrdered=0, productionDueWeek=-1)])
        self.write('release-outcomes', [dict(recordId='r', unitsPressed=501, pressingBookSold=100)])
        with self.assertRaisesRegex(ValueError, 'outcome mismatch'):
            physical_inventory_summary(self.prefix)

    def test_duplicate_and_incomplete_history_are_rejected(self):
        with self.assertRaisesRegex(ValueError, 'identity/status'):
            self.summary([self.row, self.row])
        self.write('demand-observation-window', [dict(endAuditWeek=2, completed='true')])
        with self.assertRaisesRegex(ValueError, 'Incomplete physical'):
            self.summary()

    def test_nonfinite_fractional_and_overdue_are_rejected(self):
        for changes in (dict(shelves='nan'), dict(shelves=1.5), dict(productionDueWeek=1), dict(unbilledInitial=1)):
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                self.summary([self.row | changes])

    def test_old_runs_explicitly_have_no_physical_evidence(self):
        self.assertIsNone(physical_inventory_summary(self.prefix))


if __name__ == '__main__':
    unittest.main()
