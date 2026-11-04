import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import pandas as pd

from analyze_midtier_breakout import followup_cohorts, replay_identity, validate_manifests


class MidTierBreakoutTests(unittest.TestCase):
    def test_followups_keep_silent_acts_and_exclude_opening_and_censored_hits(self):
        records = pd.DataFrame([
            (1, 1960, 'old', 'opening', 'field', 1),
            (4, 1960, 'early', 'early-hit', 'field', 1),
            (5, 1960, 'a', 'hit', 'field', 5),
            (6, 1960, 'b', 'silent', 'field', 6),
            (10, 1960, 'a', 'followup', 'big5', 40),
            (22, 1960, 'a', 'followup2', 'big5', 30),
            (58, 1961, 'a', 'followup', 'big5', 9),
            (60, 1961, 'late', 'late-hit', 'field', 4),
        ], columns=['week', 'year', 'artistId', 'recordId', 'group', 'currentPosition'])
        launches = pd.DataFrame([(8, 'a', 'followup'), (20, 'a', 'followup2')], columns=['week', 'artistId', 'singleId'])
        rows = followup_cohorts(records, launches, {'opening'}, 100)
        self.assertEqual({r['artistId'] for r in rows}, {'a', 'b'})
        a = next(r for r in rows if r['artistId'] == 'a')
        self.assertEqual((a['group'], a['releases52'], a['chartedFollowups52']), ('field', 2, 2))
        self.assertEqual((a['firstReleaseGap'], a['firstDebutGap'], a['subsequentDebutGaps']), (3, 5, [12]))
        self.assertEqual(a['top10Followups52'], 0)  # Top 10 after the fixed 52-week window is not conversion.
        b = next(r for r in rows if r['artistId'] == 'b')
        self.assertEqual((b['releases52'], b['noRelease52'], b['noDebut52']), (0, 1, 1))
        self.assertIsNone(b['firstDebutGap'])

    def test_off_replay_checks_rows_but_ignores_later_control_weeks(self):
        with tempfile.TemporaryDirectory() as folder, patch('analyze_midtier_breakout.ROOT', folder):
            for number in range(50):
                Path(folder, f'base-default-1-265-stream{number}.csv').write_text('week,value\n1,a\n52,b\n53,c\n')
                Path(folder, f'off-default-1-53-stream{number}.csv').write_text('week,value\n1,a\n52,b\n53,d\n')
            # path() has its own imported ROOT; patch it at its defining module as well.
            with patch('analyze_offchart_screen.ROOT', folder):
                self.assertEqual(replay_identity('off', 'base', 1, 265)['streams'], 50)
                Path(folder, 'off-default-1-53-stream0.csv').write_text('week,value\n1,a\n52,wrong\n')
                with self.assertRaisesRegex(ValueError, 'Off replay differs'):
                    replay_identity('off', 'base', 1, 265)

    def test_manifest_refuses_mixed_configuration_and_incomplete_cohort(self):
        with tempfile.TemporaryDirectory() as folder, patch('analyze_midtier_breakout.ROOT', folder):
            plan = dict(baseline='base', arms={'repair': 'repair', 'breakout': 'breakout'}, seeds=[1, 2, 3], weeks=265, assemblySha256='new')
            baseline = dict(extraFlags=['--base'], profile='game', weeks=265, seeds=[1, 2, 3], modes=['default'],
                            assemblySha256='old', results=[dict(seed=s, passed=True) for s in [1, 2, 3]])
            for arm in ['base', 'repair', 'breakout']:
                m = dict(baseline)
                m['extraFlags'] = ['--base'] + ([] if arm == 'base' else ['--slate-work-budget=0.67', '--slate-work-budget-midtier=1', '--slate-going-concern-reserve'])
                if arm == 'breakout':
                    m['extraFlags'].append('--slate-breakout-planning')
                if arm != 'base':
                    m['assemblySha256'] = 'new'
                Path(folder, arm).mkdir()
                Path(folder, arm, 'runs.json').write_text(json.dumps(m))
            validate_manifests(plan)
            m['results'] = m['results'][:2]
            Path(folder, 'breakout', 'runs.json').write_text(json.dumps(m))
            with self.assertRaisesRegex(ValueError, 'Incomplete run cohort'):
                validate_manifests(plan)
            m['results'] = baseline['results']; m['extraFlags'].append('--unexpected')
            Path(folder, 'breakout', 'runs.json').write_text(json.dumps(m))
            with self.assertRaisesRegex(ValueError, 'Mismatched configuration'):
                validate_manifests(plan)


if __name__ == '__main__':
    unittest.main()
