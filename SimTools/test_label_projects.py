import copy
import unittest
import pandas as pd
from analyze_label_projects import CONNECTIONS, validate_manifests, validate_reviews


class ProjectAuditTests(unittest.TestCase):
    def manifests(self):
        m = {'weeks': 106, 'profile': 'game', 'extraFlags': ['--label-release-slate'], 'assemblySha256': 'same',
             'results': [{'seed': 1001, 'passed': True, 'exitCode': 0}]}
        arms = {a: copy.deepcopy(m) for a in ('control', 'connected', 'judged')}
        arms['connected']['extraFlags'] += sorted(CONNECTIONS)
        arms['judged']['extraFlags'] += ['--label-project-judgement']
        return arms

    def test_completed_same_binary_declared_changes(self):
        validate_manifests(self.manifests(), 1001, 106)
        for mutate in (lambda m: m['judged'].update(assemblySha256='other'),
                       lambda m: m['connected']['extraFlags'].append('--unrelated'),
                       lambda m: m['judged']['results'][0].update(passed=False)):
            m = self.manifests(); mutate(m)
            with self.assertRaises(ValueError): validate_manifests(m, 1001, 106)

    def test_material_authorization(self):
        rows = pd.DataFrame([dict(action='hold-for-stronger-material', perceived=-1, routineRead=.6, candidateId=None),
                             dict(action='record-reviewed-material', perceived=.7, routineRead=.6, candidateId='song')])
        validate_reviews(rows)
        rows.loc[1, 'perceived'] = .5
        with self.assertRaises(ValueError): validate_reviews(rows)
        rows.loc[1, 'perceived'] = .7; rows.loc[1, 'candidateId'] = None
        with self.assertRaises(ValueError): validate_reviews(rows)
        rows.loc[1, 'candidateId'] = 'song'; rows.loc[1, 'perceived'] = float('nan')
        with self.assertRaises(ValueError): validate_reviews(rows)

    def test_shortfall_inside_uncertainty(self):
        rows = pd.DataFrame([dict(action='record-reviewed-material', perceived=.55, routineRead=.6, candidateId='song', uncertainty=.07)])
        validate_reviews(rows)
        rows.loc[0, 'uncertainty'] = .03
        with self.assertRaises(ValueError): validate_reviews(rows)

    def test_own_session_at_deadline(self):
        rows = pd.DataFrame([dict(action='record-own-session', perceived=.5, routineRead=.6, candidateId='routine-session|a')])
        validate_reviews(rows)
        rows.loc[0, 'candidateId'] = 'rj:a:2'
        with self.assertRaises(ValueError): validate_reviews(rows)


if __name__ == '__main__': unittest.main()
