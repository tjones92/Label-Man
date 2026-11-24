import csv
import json
import tempfile
import unittest
from pathlib import Path

from build_stature_chart_cohorts import build


class ChartCohortChecks(unittest.TestCase):
    def test_run_start_is_censored_and_later_chart_waits_for_next_week(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            records, population, output = (root / x for x in ('records.csv', 'population.csv', 'out.json'))
            with records.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=('week', 'year', 'recordId', 'artistId', 'currentPosition'))
                writer.writeheader()
                for week, artist, position in [(0, 'a', 20), (1, 'a', 10), (1, 'b', 0), (2, 'b', 99)]:
                    writer.writerow(dict(week=week, year=1960, recordId=artist, artistId=artist, currentPosition=position))
            with population.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=('week', 'eventType', 'artistId'))
                writer.writeheader(); writer.writerow(dict(week=1, eventType='formation', artistId='b'))
            build(records, population, output)
            result = {r['artistId']: r for r in json.loads(output.read_text())['cohorts']}
            self.assertTrue(result['a']['leftCensoredAtRunStart'])
            self.assertEqual(result['a']['availableFromDecisionWeek'], 1)
            self.assertEqual(result['b']['availableFromDecisionWeek'], 3)
            self.assertEqual(result['b']['preRunChartHistory'], 'not_applicable_observed_runtime_formation')


if __name__ == '__main__':
    unittest.main()
