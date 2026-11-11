import unittest
from pathlib import Path
from analyze_opening_geography import inputs, populations, opening


class GeographyChecks(unittest.TestCase):
    def test_concentration_can_raise_draw_without_lowering_reach(self):
        national = inputs(20, {'a': 1, 'b': 3}, ('a', 'b'), 100)
        local = inputs(20, {'a': 1, 'b': 3}, ('a',), 100)
        self.assertGreater(local['regionalDraw'], national['regionalDraw'])
        self.assertEqual(local['conversionReach'], national['conversionReach'])

    def test_saturation_reduces_weighted_reach(self):
        self.assertEqual(inputs(200, {'a': 1, 'b': 3}, ('a',), 100)['conversionReach'], .25)
        self.assertEqual(inputs(200, {'a': 1, 'b': 3}, ('a', 'b'), 100)['conversionReach'], .5)

    def test_population_scene(self):
        self.assertEqual(len(populations(Path(__file__).resolve().parents[1] / 'chart_manager.tscn')), 7)

    def test_invalid_footprint(self):
        with self.assertRaises(ValueError):
            inputs(10, {'a': 1}, ('unknown',), 100)

    def test_reject_national_spread_claim_without_full_census(self):
        with self.assertRaises(ValueError):
            opening([dict(artistId='x', artist='X', openingArchetype='RegionalName',
                          region='a', seedYoung='10', seedAdult='0')], {'a': 1, 'b': 3}, 100)


if __name__ == '__main__':
    unittest.main()
