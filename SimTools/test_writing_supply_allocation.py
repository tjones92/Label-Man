import unittest
from unittest.mock import patch

import analyze_writing_supply_allocation as analysis


class SupplyAllocationTests(unittest.TestCase):
    def test_supply_requires_one_isolated_switch(self):
        control = dict(assemblySha256='same', flags=sorted(analysis.access.REQUIRED | {'--seed=1001'}))
        treatment = dict(assemblySha256='same', flags=control['flags'] + ['--rock-hit-writers'])
        self.assertEqual(1001, analysis.validate_supply_pair(control, treatment))
        treatment['flags'].append('--material-accessible-sampling')
        with self.assertRaises(ValueError):
            analysis.validate_supply_pair(control, treatment)

    def fixture(self, recorded=78, issued=104, proposed=77, prepared=False):
        demo = dict(week=str(recorded), songId='album', artistId='act', labelId='label',
                    status='Recorded', compositionHook='.9', feePaid='100')
        plan = dict(week=str(proposed), slotId='plan', labelId='label', artistId='act',
                    format='Single', state='Proposed', intendedWeek=str(recorded + 2),
                    recordId='single' if prepared else '', preparedWeek='70' if prepared else '',
                    paidProduction='100' if prepared else '0', actualWeek='0')
        return {'writing-demos': [demo],
                'writing-launches': [dict(week=str(issued), recordId='single', songId='cover',
                                         labelId='label', artistId='act', source='CoverStandard', compositionHook='.6')],
                'writing-album-tracks': [dict(songId='album', artistId='act', terminalState='Released',
                                             source='ExternalProfessional', scheduledWeek=str(recorded + 1), projectId='lp')],
                'release-slate': [plan, dict(plan, week=str(issued), state='Issued', recordId='single', actualWeek=str(issued))]}

    def analyze(self, rows):
        with patch.object(analysis, 'iter_rows', side_effect=lambda run, suffix: iter(rows.get(suffix, []))), \
                patch.object(analysis.access, 'stock_destinations', return_value={}):
            return analysis.allocation_rows('fixture')

    def test_complete_window_and_actual_plan_join(self):
        result = self.analyze(self.fixture())
        self.assertEqual(1, result['counts']['withSubsequentlyIssuedPlan'])
        self.assertEqual(1, result['counts']['unpreparedPlanAndHookAtLeastPoint1Higher'])
        self.assertEqual('single', result['examples'][0]['singleRecordId'])
        late = self.analyze(self.fixture(issued=105))
        self.assertNotIn('withSubsequentlyIssuedPlan', late['counts'])
        immature = self.analyze(self.fixture(recorded=79))
        self.assertNotIn('albumOnlyRecordedBy78', immature['counts'])

    def test_future_plan_and_other_client_do_not_qualify(self):
        result = self.analyze(self.fixture(proposed=79))
        self.assertNotIn('withOpenUpcomingSinglePlan', result['counts'])
        rows = self.fixture()
        rows['writing-launches'][0]['artistId'] = 'other'
        self.assertNotIn('withSubsequentlyIssuedPlan', self.analyze(rows)['counts'])

    def test_opening_stock_and_both_destinations_excluded(self):
        rows = self.fixture()
        rows['writing-demos'].insert(0, dict(rows['writing-demos'][0], week='0'))
        self.assertNotIn('albumOnly', self.analyze(rows)['counts'])
        rows = self.fixture()
        rows['writing-launches'][0].update(songId='album', source='ExternalProfessional')
        self.assertNotIn('albumOnly', self.analyze(rows)['counts'])

    def test_paid_prepared_single_not_counted_as_unprepared(self):
        result = self.analyze(self.fixture(prepared=True))
        self.assertEqual(1, result['counts']['withSubsequentlyIssuedPlan'])
        self.assertNotIn('nextIssuedPlanUnpreparedAtSnapshot', result['counts'])

    def test_duplicate_album_uses_count_once(self):
        rows = self.fixture()
        rows['writing-album-tracks'] *= 2
        result = self.analyze(rows)
        self.assertEqual(1, result['counts']['albumOnly'])
        self.assertEqual(1, result['counts']['distinctNextIssuedSingles'])


if __name__ == '__main__':
    unittest.main()
