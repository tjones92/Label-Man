import unittest
import json
from pathlib import Path
from unittest.mock import patch
from analyze_reminder_pilot import manifests, reminder_outcomes


def fixture():
    base = dict(labelId='l', artistId='a', contractSequence='1', createdWeek='1')
    return {
        'plan-reminder-events': [dict(base, week='1', action='created', reason='no-matching-plan', slotId='', orderId=''),
                                 dict(base, week='3', action='attempt', reason='ordered', slotId='p', orderId='o')],
        'writing-orders': [dict(week='6', id='o', artistId='a', labelId='l', startedWeek='3', deliveryWeek='5', status='Delivered', fee='250', releasePlanId='p', songId='s')],
        'release-slate': [dict(week='8', slotId='p', state='Issued', actualWeek='8', recordId='r')],
        'material-funnel': [dict(week='5', slotId='p')],
        'writing-launches': [dict(recordId='r', songId='s')],
        'records': [dict(recordId='r', week='10', currentPosition='23')],
    }


class ReminderPilotAnalysisTests(unittest.TestCase):
    def run_fixture(self, data):
        with patch('analyze_reminder_pilot.rows', side_effect=lambda run, suffix: iter(data[suffix])):
            return reminder_outcomes('x', 52)

    def test_linked_order_and_complete_delivery_window(self):
        result = self.run_fixture(fixture())
        self.assertEqual(result['lifecycle']['linkedPlanUsedSong26'], 1)
        self.assertEqual(result['lifecycle']['commissionFees'], 250)
        self.assertEqual(result['lifecycle']['linkedSongTop40Within13'], 1)
        self.assertEqual(result['attemptReasons'], {'ordered': 1})

    def test_rejects_late_attempt_and_prior_material_review(self):
        data = fixture()
        data['plan-reminder-events'][1]['week'] = '10'
        with self.assertRaises(ValueError):
            self.run_fixture(data)
        data = fixture()
        data['material-funnel'][0]['week'] = '2'
        with self.assertRaises(ValueError):
            self.run_fixture(data)
        data = fixture()
        data['material-funnel'][0]['week'] = '4'
        with self.assertRaises(ValueError):
            self.run_fixture(data)

    def test_rejects_duplicate_attempt(self):
        data = fixture()
        data['plan-reminder-events'].append(dict(data['plan-reminder-events'][1]))
        with self.assertRaises(ValueError):
            self.run_fixture(data)

    def test_matched_manifest_requires_only_the_pilot_flag_to_differ(self):
        common = ['--seed=1001', '--weeks=104', '--writing-market-planned-assignments']
        documents = {
            'control-manifest.json': dict(assemblySha256='same', flags=common + ['--run=control']),
            'pilot-manifest.json': dict(assemblySha256='same', flags=common + ['--run=pilot', '--writing-market-plan-reminders']),
        }
        with patch.object(Path, 'read_text', autospec=True, side_effect=lambda path, **kw: json.dumps(documents[path.name])):
            self.assertEqual(len(manifests(['control', 'pilot'], 104)), 2)
            documents['pilot-manifest.json']['flags'].append('--other-policy')
            with self.assertRaises(ValueError):
                manifests(['control', 'pilot'], 104)


if __name__ == '__main__':
    unittest.main()
