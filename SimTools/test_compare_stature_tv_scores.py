import csv
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from compare_stature_tv_scores import compare


class TvScoreChecks(unittest.TestCase):
    def test_lagged_peers_and_smoothing_use_prior_week(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source, output = root / 'evidence.csv', root / 'result.json'
            fields = ['date', 'artistId', 'tvRegisteredYoungEstimate', 'tvJoinYoung',
                      'tvRegisteredAdultEstimate', 'tvJoinAdult']
            with source.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=fields)
                writer.writeheader()
                for date in ('1/1/1960', '1/8/1960'):
                    for artist in ('a', 'b', 'c'):
                        join = 1 if artist == 'a' and date == '1/1/1960' else 100
                        writer.writerow(dict(date=date, artistId=artist, tvRegisteredYoungEstimate=100,
                                             tvJoinYoung=join, tvRegisteredAdultEstimate=100, tvJoinAdult=join))
            with patch('compare_stature_tv_scores.verify'):
                compare(source, output, half_life=1, top_n=1, minimum=1, floor=.01, evaluation_start='1960-01-08')
            result = json.loads(output.read_text())['ages']['Young']
            self.assertEqual(result['artistWeeksCompared'], 3)
            self.assertAlmostEqual(result['absoluteDifference']['p100'], .495)
            self.assertEqual(result['artistWeeksDifferenceOverPoint1'], 1)


if __name__ == '__main__':
    unittest.main()
