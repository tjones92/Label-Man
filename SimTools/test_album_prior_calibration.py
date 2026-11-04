import unittest

from calibrate_album_prior import fit, predict


class AlbumPriorCalibrationTests(unittest.TestCase):
    def test_affinity_and_hit_terms_are_identified_separately_from_cost(self):
        data = []
        expected = [1.1, 2.3, 3.9, 4.9]
        for band in range(4):
            for base, hits, cost in ((100., 0., 30.), (50., 70., 200.), (200., 10., 50.)):
                data.append({"band": band, "base": base, "hits": hits, "cost": cost,
                             "net": expected[band] * base + .8 * hits - cost})
        model = fit(data)
        for observed in data:
            self.assertAlmostEqual(predict(model, observed), observed["net"], places=6)
        self.assertAlmostEqual(model["hitCoefficient"], .8, places=7)
        # A changed cost does not rescale either component of the contribution.
        self.assertAlmostEqual(predict(model, data[0] | {"cost": 80.}), data[0]["net"] - 50., places=6)

    def test_unidentified_model_is_rejected_and_career_order_is_constrained(self):
        with self.assertRaisesRegex(ValueError, "All career bands"):
            fit([{"band": 0, "base": 100., "hits": 10., "cost": 20., "net": 50.}])
        data = []
        for band, affinity in enumerate((1., 4., 2., 5.)):
            for base, hits in ((100., 0.), (50., 100.)):
                data.append({"band": band, "base": base, "hits": hits, "cost": 0., "net": affinity * base + hits})
        model = fit(data)
        self.assertEqual(model["careerAffinity"], sorted(model["careerAffinity"]))
        self.assertGreaterEqual(model["hitCoefficient"], 0.)


if __name__ == "__main__":
    unittest.main()
