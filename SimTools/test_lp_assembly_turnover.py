import unittest
from unittest.mock import patch

import analyze_lp_assembly_turnover as analysis


class LpAssemblyTurnoverTests(unittest.TestCase):
    def pair(self):
        flags = analysis.access.REQUIRED | {'--seed=1001','--lp-assembly','--album-master-costs',
                    '--album-release-practice','--release-all-in-economics','--album-prior-recalibration'}
        return (dict(assemblySha256='same',flags=sorted(flags)),
                dict(assemblySha256='same',flags=sorted(flags-{'--lp-assembly'})))

    def test_only_assembly_omitted(self):
        self.assertEqual(1001,analysis.validate_pair(*self.pair()))

    def test_cost_correction_cannot_be_removed(self):
        c,t = self.pair()
        t['flags'].remove('--album-master-costs')
        with self.assertRaises(ValueError): analysis.validate_pair(c,t)

    def test_other_policy_changes_cannot_enter(self):
        c,t = self.pair()
        t['flags'].append('--senior-cooling')
        with self.assertRaises(ValueError): analysis.validate_pair(c,t)

    def test_mixed_binaries_rejected(self):
        c,t = self.pair()
        t['assemblySha256'] = 'different'
        with self.assertRaises(ValueError): analysis.validate_pair(c,t)

    def test_duplicate_parameter_keys_rejected(self):
        c,t = self.pair()
        t['flags'].append('--seed=2002')
        with self.assertRaises(ValueError): analysis.validate_pair(c,t)

    def test_prepared_costs_and_released_assembly_uses_have_distinct_scopes(self):
        row = dict(format='Album',recordId='released',preparedWeek='2',paidProduction='100')
        assembly = dict(recordId='released',kind='HitLed',newSessions='7',releasedMasters='3',shelvedMasters='1')
        rows = {'release-slate':[row,row,dict(row,recordId='cancelled',paidProduction='50')],
                'album-projects':[dict(albumRecordId='released',terminalState='Released',scheduledWeek='3'),
                                  dict(albumRecordId='cancelled',terminalState='Cancelled',scheduledWeek='4')],
                'lp-assembly':[assembly,assembly,dict(assembly,recordId='cancelled')],
                'album-practice':[dict(albumId='released',format='Standard'),dict(albumId='cancelled',format='Compilation')]}
        with patch.object(analysis,'iter_rows',side_effect=lambda run,suffix: iter(rows.get(suffix,[]))):
            r = analysis.lp_outputs('fixture')
        self.assertEqual(2,r['preparedSlateAlbumMasters'])
        self.assertEqual(150,r['paidSlateAlbumProduction'])
        self.assertEqual(1,r['preparedButNoReleasedAlbumUse'])
        self.assertEqual({'HitLed':1},r['releasedAssemblyKinds'])
        self.assertEqual(7,r['releasedAssemblyNewSessions'])
        self.assertEqual(4,r['releasedAssemblyReusedMasters'])


if __name__ == '__main__':
    unittest.main()
