import unittest
from collections import defaultdict

import analyze_combined_label_turnover as audit


def statement(period, week, **changes):
    r = {k: '0' for k in (*audit.INFLOWS, *audit.OUTFLOWS)}
    r.update(labelId='label', period=str(period), week=str(week), openingCash='100',
             closingCash='110', recordReceipts='10', cashRounding='0', recordUnits='10', recordGross='15')
    r.update(changes)
    return r


def world(death=None):
    rows = [dict(week='1', status='Stable', cashReserves='100', outstandingWholesaleReceivables='0',
                 outstandingPlantPayables='0', exitReason='None')]
    if death:
        rows.append(dict(rows[0], week=str(death), status='Defunct', cashReserves='-1', exitReason='Bankruptcy'))
    rows.append(dict(rows[-1], week='104'))
    key = ('Label', 'Type', 'false', 'Small')
    result = {k: defaultdict(list) for k in ('histories','monthly','sales','releases','shortages','risks','issueRefusals','launches')}
    result['opening'] = {key: 'label'}
    result['histories']['label'] = rows
    result['monthly']['label'] = [statement(i, w) for i, w in ((1,5),(2,9),(3,14),(4,18),(5,23))]
    return result, key


class TurnoverTests(unittest.TestCase):
    def test_cash_bridge_includes_signed_funding_and_rounding(self):
        r = statement(1,5,recordReceipts='100',serviceIncome='5',recording='30',campaigns='10',
                      financingNet='-2',ownerTransfersNet='-3',cashRounding='.001',closingCash='160.001')
        terms = audit.cash_terms(r)
        self.assertAlmostEqual(60.001,sum(terms.values()))
        self.assertEqual(-30,terms['recording'])
        self.assertEqual(-3,terms['ownerTransfersNet'])

    def test_cash_bridge_rejects_unexplained_money(self):
        with self.assertRaises(ValueError):
            audit.cash_terms(statement(1,5,closingCash='120'))

    def test_calendar_intersection_and_exit_anchor_exclude_future(self):
        c,key = world()
        t,_ = world(20)
        t['monthly']['label'] = [r for r in t['monthly']['label'] if r['period'] != '2']
        row = audit.firm_pair(c,t,key)
        self.assertEqual('additionalExit',row['cohort'])
        self.assertEqual(20,row['anchorWeek'])
        self.assertEqual([1,3,4],row['control']['monthlyPeriods'])
        self.assertEqual([1,3,4],row['combined']['monthlyPeriods'])
        self.assertEqual(30,row['control']['cashTerms']['recordReceipts'])

    def test_both_exit_uses_earlier_exit(self):
        c,key = world(10)
        t,_ = world(30)
        row = audit.firm_pair(c,t,key)
        self.assertEqual('bothExit',row['cohort'])
        self.assertEqual(10,row['anchorWeek'])
        self.assertEqual([1,2],row['combined']['monthlyPeriods'])

    def test_reviews_are_deduplicated_and_not_lost_release_counts(self):
        w,_ = world()
        shortage = dict(week='12',slotId='plan',cash='100',production='50',marketing='10',pressing='0')
        risk = dict(week='12',slotId='plan',prepared='false',commit='false',meanCommit='true')
        w['shortages']['label'] = [shortage,shortage]
        w['risks']['label'] = [risk,risk,dict(risk,week='21')]
        result = audit.window(w,'label',20,{2,3,4})
        self.assertEqual(1,result['cashShortfallSlotWeeks'])
        self.assertEqual(1,result['riskRefusals'])
        self.assertEqual(1,result['riskRefusalsMeanWouldCommit'])
        self.assertEqual(1,result['shortagesWithImmediateProjectCash'])

    def test_bankrupt_status_is_not_yet_a_defunct_snapshot(self):
        self.assertIsNone(audit.first_exit([dict(week='4',status='Bankrupt')]))
        self.assertEqual(5,audit.first_exit([dict(week='4',status='Bankrupt'),dict(week='5',status='Defunct')]))


if __name__ == '__main__':
    unittest.main()
