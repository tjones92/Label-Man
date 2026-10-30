import unittest
import pandas as pd
from score_playlist_pipeline import first_events, score


class ObserverChecks(unittest.TestCase):
    def test_same_station_confirmation(self):
        x = pd.DataFrame([('a', 'r', 1), ('b', 'r', 2), ('b', 'r', 3)],
                         columns=['stationId', 'recordId', 'week'])
        first, sustained = first_events(x)
        self.assertEqual(first['r'], 1)
        self.assertEqual(sustained['r'], 2)

    def test_boundary_and_matched_horizon(self):
        records = pd.DataFrame([dict(recordId='r', week=w, weeksSinceRelease=w-3,
                                     labelId='indie', currentPosition=5 if w == 8 else 0)
                                for w in range(4, 10)])
        playlists = pd.DataFrame([dict(recordId='r', week=w, stationId=s, city=c, format='FullService')
                                  for s, c in [('a', 'Los Angeles'), ('b', 'Los Angeles'), ('c', 'Minneapolis')]
                                  for w in range(4, 10)])
        panel = {'Los Angeles': ['a', 'b'], 'Minneapolis': ['c']}
        _, _, listings = score(records, playlists, panel, start=5, end=6, horizon=3)
        self.assertEqual(listings.firstListings.sum(), 0, 'Carryovers must not become new listings')
        _, events, _ = score(records, playlists, panel, start=4, end=4, horizon=3)
        self.assertTrue(events.peak.isna().all(), 'Late chart success is outside the matched horizon')
        with self.assertRaises(ValueError):
            score(records, playlists, {'Los Angeles': ['a', 'missing'], 'Minneapolis': ['c']})


if __name__ == '__main__':
    unittest.main()
