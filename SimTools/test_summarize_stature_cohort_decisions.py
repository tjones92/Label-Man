import csv
import json
import tempfile
import unittest
from pathlib import Path

from summarize_stature_cohort_decisions import summarize


class CohortDecisionChecks(unittest.TestCase):
    def test_future_chart_entry_is_not_used_in_prior_decision(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            decisions, cohorts, output = (root / x for x in ('decisions.csv', 'cohorts.json', 'out.json'))
            cohorts.write_text(json.dumps({'cohorts': [{'artistId': 'a', 'availableFromDecisionWeek': 3,
                'leftCensoredAtRunStart': False, 'firstObservedChartYear': 1960}]}))
            fields = ('date', 'week', 'artistId', 'tier', 'eligible', 'readStatus', 'oldMultiplier', 'proposedMultiplier', 'appliedMultiplier')
            with decisions.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=fields); writer.writeheader()
                for week, date in ((2, '1/8/1960'), (3, '1/15/1960')):
                    writer.writerow(dict(date=date, week=week, artistId='a', tier='Small', eligible='1', readStatus='Available',
                                         oldMultiplier='1', proposedMultiplier='2', appliedMultiplier='1'))
            summarize(decisions, cohorts, output)
            groups = json.loads(output.read_text())['groups']
            self.assertEqual({g['cohort'] for g in groups}, {'not_yet_observed_on_chart', '1960_observed_after_start'})
            self.assertEqual(sum(g['counts']['higher'] for g in groups), 2)


if __name__ == '__main__':
    unittest.main()
