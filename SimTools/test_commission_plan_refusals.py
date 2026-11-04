import unittest
from unittest.mock import patch
from analyze_commission_plan_refusals import summarize


class RefusalFollowupTests(unittest.TestCase):
    def test_maturity_next_week_and_outcome_window(self):
        data = {
            'records': [{'week': '52'}],
            'commission-plan-diagnostic': [
                dict(week='10', labelId='l', artistId='a', primary='no-matching-plan', closed='0', preparing='0', material='0', lead='0', horizon='0'),
                dict(week='27', labelId='l', artistId='a', primary='insufficient-lead', closed='1', preparing='0', material='0', lead='1', horizon='0')],
            'professional-arrivals': [dict(week=str(w), labelId='l', artistId='a', provenance='arrival', outcome='commission-refused:no-plan-minimum-delivery') for w in (10, 27)],
            'commission-eligible-plans': [
                dict(week='10', labelId='l', artistId='a', slotId='same'),
                dict(week='12', labelId='l', artistId='a', slotId='next')],
            'release-slate': [dict(week='13', slotId='next', state='Proposed', preparedWeek=''),
                              dict(week='35', slotId='next', state='Issued', preparedWeek='20', recordId='r'),
                              dict(week='37', slotId='next', state='Cancelled', preparedWeek='20')],
            'writing-launches': [dict(recordId='r', source='ArtistWritten')],
        }
        with patch('analyze_commission_plan_refusals.rows', side_effect=lambda run, suffix: iter(data[suffix])):
            result = summarize('x', 52)
        self.assertEqual(result['allPrimary']['insufficient-lead'], 1)
        self.assertEqual(result['overlapFlags']['closed'], 1)
        self.assertNotIn('insufficient-lead', result['matureByPrimary'])
        self.assertEqual(result['matureByPrimary']['no-matching-plan'], {
            'events': 1, 'observedEligible': 1, 'waitWeeksTotal': 2, 'issued': 1, 'issuedSource_ArtistWritten': 1, 'cancelled': 0, 'prepared': 1})

    def test_incomplete_endpoint_rejected(self):
        with patch('analyze_commission_plan_refusals.rows', return_value=iter([{'week': '51'}])):
            with self.assertRaises(ValueError):
                summarize('x', 52)


if __name__ == '__main__':
    unittest.main()
