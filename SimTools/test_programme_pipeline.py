import csv
import tempfile
import unittest
from unittest.mock import patch
from pathlib import Path
from analyze_programme_pipeline import trace, opening_cohort


class PipelineTests(unittest.TestCase):
    def test_fixed_cohort_excludes_new_arrivals(self):
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / 'SimLogs') as folder:
            prefix = str(Path(folder) / 'run')
            with open(prefix + '-programme-choices.csv', 'w', newline='') as f:
                writer = csv.DictWriter(f, fieldnames=['week','format','eligible','careerState','artistId'])
                writer.writeheader()
                writer.writerows([
                    dict(week=1, format='Single', eligible=1, careerState='Established', artistId='old'),
                    dict(week=1, format='Single', eligible=1, careerState='NewSigning', artistId='new'),
                    dict(week=2, format='Single', eligible=1, careerState='Star', artistId='later')])
            self.assertEqual(opening_cohort(prefix)[0], {'old'})

    def test_cancelled_and_unfinished_work_are_retained(self):
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / 'SimLogs') as folder:
            prefix = str(Path(folder) / 'run')
            fields = ['week','artistId','format','slotId','proposedWeek','intendedWeek','source','preparedWeek','state','reason','actualWeek']
            base = dict(week=2, artistId='act', format='Single', proposedWeek=1, intendedWeek=2,
                        source='hit-follow-up', preparedWeek='', actualWeek=0)
            with open(prefix + '-release-slate.csv', 'w', newline='') as f:
                writer = csv.DictWriter(f, fieldnames=fields); writer.writeheader()
                writer.writerows([{**base, 'slotId':'cancel', 'state':'Cancelled','reason':'window-expired'},
                                 {**base, 'slotId':'pending','state':'Postponed','reason':'preparation-capacity-unavailable'}])
            summary, acts = trace(prefix, {'act'}, {'act':dict(week='1',intendedWeek='2',gap='13')})
            self.assertEqual(summary['dueStates'], {'Cancelled':1,'Postponed':1})
            self.assertIsNone(summary['issuedMeanPreparationLateness'])
            self.assertEqual(len(acts[0]['intentions']), 2)

    def test_early_issue_fulfils_follow_up_without_negative_wait(self):
        with tempfile.TemporaryDirectory(dir=Path(__file__).resolve().parents[1] / 'SimLogs') as folder:
            prefix = str(Path(folder) / 'run')
            fields = ['week','artistId','format','slotId','proposedWeek','intendedWeek','source','preparedWeek','state','reason','actualWeek']
            issued = dict(week=3, artistId='act', format='Single', slotId='early', proposedWeek=1,
                          intendedWeek=3, source='roster-programme', preparedWeek=2, state='Issued',reason='issued',actualWeek=3)
            with open(prefix + '-release-slate.csv', 'w', newline='') as f:
                writer = csv.DictWriter(f, fieldnames=fields); writer.writeheader()
                writer.writerows([issued, {**issued,'week':70,'artistId':'other','slotId':'endpoint'}])
            summary, _ = trace(prefix, {'act'}, {'act':dict(week='1',intendedWeek='2',gap='1')})
            self.assertEqual(summary['openingDue52'], dict(fullyObservedActs=1, issued52=1, noIssue52=0, meanWaitCapped52=0))

    def test_lp_campaign_single_counts_as_actual_follow_up(self):
        slate = [dict(week='70', artistId='act', format='Single', slotId='cancelled', proposedWeek='1',
                      intendedWeek='3', source='hit-follow-up', preparedWeek='', state='Cancelled', reason='window-expired', actualWeek='0')]
        launch = [dict(week='3', artistId='act', singleId='promo', strategy='AlbumWithPromo')]
        with patch('analyze_programme_pipeline.rows', side_effect=lambda path: iter(launch if str(path).endswith('single-launch-quality.csv') else slate if str(path).endswith('release-slate.csv') else [])), \
             patch('analyze_programme_pipeline.Path.exists', return_value=True):
            summary, acts = trace('run', {'act'}, {'act':dict(week='1',intendedWeek='2',gap='1')})
        self.assertEqual(summary['openingDue52']['issued52'], 1)
        self.assertEqual(summary['openingDue52']['meanWaitCapped52'], 0)
        self.assertEqual(acts[0]['launches'][0]['strategy'], 'AlbumWithPromo')


if __name__ == '__main__':
    unittest.main()
