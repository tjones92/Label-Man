import unittest
from unittest.mock import patch
import analyze_material_constraints as audit


class ConstraintTests(unittest.TestCase):
    def test_mature_blocked_plan_links_blockers_and_bounded_outcomes(self):
        data = {
            'records': [{'week': '52'}],
            'release-slate': [
                dict(slotId='a', format='Single', proposedWeek='1', week='1', state='Proposed', preparedWeek='0'),
                dict(slotId='a', format='Single', proposedWeek='1', week='27', state='Issued', recordId='record', preparedWeek='0'),
                dict(slotId='late', format='Single', proposedWeek='27', week='27', state='Proposed')],
            'programme-material-search': [
                dict(slotId='b', week='1', state='Pending', professional='0', startedWeek='1', songId='cover'),
                dict(slotId='a', week='4', state='Pending', professional='1', startedWeek='4', songId='song'),
                dict(slotId='a', week='28', state='Pending', professional='1', startedWeek='28', songId='other')],
            'material-funnel': [
                dict(slotId='a', week='2', selected='professional', startBlock='label-capacity', selectedSongId='song', blockingSlots='b', career='NewSigning', genre='Soul', craft='.3'),
                dict(slotId='a', week='3', selected='cover', startBlock='', selectedSongId='', blockingSlots='', career='NewSigning', genre='Soul', craft='.3')],
            'writing-launches': [],
            'professional-arrivals': [dict(provenance='arrival', outcome='commission-refused:cash-reserve')],
        }
        with patch.object(audit, 'rows', side_effect=lambda run, suffix: iter(data[suffix])):
            result = audit.summarize('fixture', 52)
        self.assertEqual(result['maturePlans'], 1)
        self.assertEqual(result['capacityBlockedProfessional']['sameSongWaitWeeksTotal'], 2)
        self.assertEqual(result['capacityBlockedProfessional']['issued'], 1)
        self.assertEqual(result['capacityBlockedProfessional']['prepared'], 0)
        self.assertEqual(result['blockerSearches']['cover'], 1)
        self.assertEqual(result['arrivalEvents']['arrival']['commission-refused:cash-reserve'], 1)

    def test_rejects_incomplete_endpoint(self):
        with patch.object(audit, 'rows', return_value=iter([{'week': '51'}])):
            with self.assertRaises(ValueError):
                audit.summarize('fixture', 52)


if __name__ == '__main__':
    unittest.main()
