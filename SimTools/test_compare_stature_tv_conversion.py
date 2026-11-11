import csv
import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from compare_stature_tv_conversion import compare


class TvConversionChecks(unittest.TestCase):
    def test_aggregates_regions_before_counting_artist_weeks(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source, output = root / 'evidence.csv', root / 'result.json'
            fields = ['date', 'artistId', 'tvRegisteredYoungEstimate', 'tvRegisteredAdultEstimate', 'tvJoinYoung', 'tvJoinAdult']
            with source.open('w', newline='') as stream:
                writer = csv.DictWriter(stream, fieldnames=fields)
                writer.writeheader()
                for artist, young, joined in [('a', 100, 1), ('a', 100, 0), ('b', 100, 0)]:
                    writer.writerow(dict(date='1/1/1960', artistId=artist, tvRegisteredYoungEstimate=young,
                                         tvRegisteredAdultEstimate=0, tvJoinYoung=joined, tvJoinAdult=0))
            with patch('compare_stature_tv_conversion.verify'):
                compare(source, output)
            result = json.loads(output.read_text())
            young = result['years']['1960']['young']
            self.assertEqual(result['artistWeeks'], 2)
            self.assertEqual(young['artistWeeksWithRegistration'], 2)
            self.assertEqual(young['artistWeeksWithRegistrationAndNoBookedJoin'], 1)
            self.assertEqual(young['unweightedJoinPerRegistration']['p100'], .005)
            self.assertIsNone(result['years']['1960']['adult']['unweightedJoinPerRegistration']['p50'])


if __name__ == '__main__':
    unittest.main()
