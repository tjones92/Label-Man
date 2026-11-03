import unittest
from unittest.mock import patch
from analyze_commission_reminders import gate, summarize


def snapshot(**overrides):
    row = dict(week='5', marketWeek='5', labelId='l', artistId='a', slotId='p', owned='0', pending='0', recent='0', writerId='w', deliveryWeeks='3', reserve='1', selectedPlanId='p', deliveryActive='1', clientMatches='1', activeCandidates='1')
    row.update(overrides)
    return row


class ReminderTests(unittest.TestCase):
    def test_order_gates_and_delivery_plan(self):
        self.assertEqual(gate(snapshot(pending='1', recent='1')), 'pending-order')
        self.assertEqual(gate(snapshot(recent='1')), 'recent-order-only')
        self.assertEqual(gate(snapshot(writerId='')), 'no-available-writer')
        self.assertEqual(gate(snapshot(reserve='0')), 'cash-reserve')
        self.assertEqual(gate(snapshot(selectedPlanId='')), 'actual-delivery-plan')
        self.assertEqual(gate(snapshot()), 'passes-observed-gates')

    def test_repeated_arrivals_map_to_one_pair_and_cutoff(self):
        arrivals = [dict(week=str(w), labelId='l', artistId='a', provenance='arrival', outcome='commission-refused:no-plan-minimum-delivery') for w in (2, 3, 27)]
        data = {
            'records': [dict(week='52')],
            'professional-arrivals': arrivals,
            'commission-plan-diagnostic': [dict(week=x['week'], labelId='l', artistId='a', primary='no-matching-plan') for x in arrivals],
            'commission-eligible-plans': [dict(week='5', labelId='l', artistId='a', slotId='p')],
            'commission-reminder-gates': [snapshot()],
            'release-slate': [dict(week='20', slotId='p', state='Issued')],
            'material-funnel': [dict(week='4', slotId='p')],
            'programme-material-search': [dict(week='6', slotId='p', state='Pending')],
        }
        with patch('analyze_commission_reminders.rows', side_effect=lambda run, suffix: iter(data[suffix])):
            result = summarize('x', 52)
        self.assertEqual(result['events'], {'matureEvents': 2, 'eventsWithEligible': 2,
                                            'distinctMatureLabelArtists': 1, 'distinctEligibleLabelArtists': 1})
        pair = result['distinctArtistPlanPairs']
        self.assertEqual((pair['pairs'], pair['eventsMapped'], pair['passes-observed-gates']), (1, 2, 1))
        self.assertEqual(pair['passesGatesAndCandidateCount'], 1)
        self.assertEqual((pair['reviewedBySnapshot'], pair['searchStartedBySnapshot'], pair['issuedByWindow']), (1, 0, 1))

    def test_missing_gate_snapshot_is_rejected(self):
        data = {'records': [dict(week='52')],
                'professional-arrivals': [dict(week='2', labelId='l', artistId='a', provenance='arrival', outcome='commission-refused:no-plan-minimum-delivery')],
                'commission-plan-diagnostic': [dict(week='2', labelId='l', artistId='a', primary='no-matching-plan')],
                'commission-eligible-plans': [dict(week='5', labelId='l', artistId='a', slotId='p')],
                'commission-reminder-gates': []}
        with patch('analyze_commission_reminders.rows', side_effect=lambda run, suffix: iter(data[suffix])):
            with self.assertRaises(ValueError):
                summarize('x', 52)


if __name__ == '__main__':
    unittest.main()
