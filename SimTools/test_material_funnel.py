import unittest
from unittest.mock import patch
import analyze_material_funnel as audit


class MaterialFunnelTests(unittest.TestCase):
    def test_deduplicates_mature_plans_and_first_review(self):
        datasets = {
            'records': [{'week': '104'}],
            'release-slate': [
                {'slotId': 'a', 'format': 'Single', 'proposedWeek': '1'},
                {'slotId': 'a', 'format': 'Single', 'proposedWeek': '1'},
                {'slotId': 'b', 'format': 'Single', 'proposedWeek': '79'},
                {'slotId': 'c', 'format': 'Album', 'proposedWeek': '1'}],
            'material-funnel': []}
        def row(slot, week, selected):
            d = dict.fromkeys(('pool', 'draws', 'unique', 'accessible', 'owned', 'future', 'held', 'placement', 'license', 'fitRefused', 'positiveProfessional', 'positiveCover'), '0')
            d.update(slotId=slot, week=str(week), selected=selected, craft='.3', started='0')
            return d
        datasets['material-funnel'] = [row('a', 2, 'none'), row('a', 3, 'cover'), row('b', 80, 'cover')]
        with patch.object(audit, 'rows', side_effect=lambda run, suffix: iter(datasets[suffix])):
            result = audit.summarize('fixture', 104)
        self.assertEqual(result['maturePlans'], 1)
        self.assertEqual(result['observedFirstReviews'], 1)
        self.assertEqual(result['totals']['selected_none'], 1)
        self.assertNotIn('selected_cover', result['totals'])
        self.assertEqual(result['craftBands']['low']['reviews'], 1)

    def test_rejects_incomplete_window(self):
        with patch.object(audit, 'rows', return_value=iter([{'week': '103'}])):
            with self.assertRaises(ValueError):
                audit.summarize('fixture', 104)


if __name__ == '__main__':
    unittest.main()
