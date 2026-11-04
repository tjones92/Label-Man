import csv
import tempfile
import unittest
from pathlib import Path

import analyze_writing_market as audit


class WritingMarketAnalysisTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parent.parent / 'SimLogs')
        self.original = audit.LOGS
        audit.LOGS = Path(self.directory.name)
        self.write('weeks', ['week'], [[104]])
        self.write('records', ['week', 'recordId', 'artistId', 'currentPosition'], [
            [0, 'opening', 'old', 1], [1, 'opening', 'old', 1],
            [2, 'a1', 'a', 20], [3, 'a1', 'a', 19], [54, 'a2', 'a', 30],
            [55, 'a3', 'a', 10], [60, 'b1', 'b', 1], [104, 'c1', 'c', 100]])

    def tearDown(self):
        audit.LOGS = self.original
        self.directory.cleanup()

    def write(self, suffix, header, data):
        with (audit.LOGS / f'fixture-{suffix}.csv').open('w', newline='', encoding='utf-8') as f:
            writer = csv.writer(f)
            writer.writerow(header)
            writer.writerows(data)

    def test_entries_and_complete_followup(self):
        result = audit.summarize('fixture')
        self.assertEqual(result['top40Entries'], 4)
        self.assertEqual(result['mature52WeekHitActs'], 1)
        self.assertEqual(result['oneToThreeFurtherHits52'], 1)
        self.assertEqual(result['fourPlusFurtherHits52'], 0)

    def test_cumulative_ledgers_not_added_across_weeks(self):
        header = ['week','writerId','commissionFees','licenseFees','retainerFees','pending','licensed','recorded','contract']
        self.write('writing-suppliers', header, [[1,'w',100,20,0,1,0,0,'Freelance'], [104,'w',100,30,0,0,0,1,'Freelance']])
        self.write('writing-orders', ['week','id','status','fee'], [[1,'o','Pending',100],[104,'o','Delivered',100]])
        result = audit.summarize('fixture')
        self.assertEqual(result['orders'], 1)
        self.assertEqual(result['commissionFees'], 100)
        self.assertEqual(result['licenseFees'], 30)
        self.assertEqual(result['meanPendingSlotShare'], .5)

    def test_bad_ledger_is_rejected(self):
        self.write('writing-orders', ['week','id','status','fee'], [[104,'o','Delivered',100]])
        with self.assertRaisesRegex(ValueError, 'does not reconcile'):
            audit.summarize('fixture')


if __name__ == '__main__':
    unittest.main()
