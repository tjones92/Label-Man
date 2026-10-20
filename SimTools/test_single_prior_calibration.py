import csv
import tempfile
import unittest
from pathlib import Path

from calibrate_single_prior import fit, isotonic, load, predict


class SinglePriorCalibrationTests(unittest.TestCase):
    def test_weighted_pooling_preserves_evidence_weight(self):
        self.assertEqual(isotonic([100., 0.], [1., 9.]), [10., 10.])
        self.assertEqual(isotonic([3., 1., 2.], [1., 1., 1.]), [2., 2., 2.])

    def test_monotone_supported_surface_is_preserved(self):
        qualities = [.35, .425, .475, .525, .575, .6125, .6375, .6625, .6875, .725, .775, .85]
        data = []
        for row, q in enumerate(qualities):
            for career in range(4):
                contribution = 1000. + row * 100. + career * 400.
                exposure, cost = .8, 300.
                net = contribution * exposure - cost
                data.append((q, career, exposure, cost, net, 0., 1960))
        model = fit(data)
        for sample in data:
            self.assertAlmostEqual(predict(model, *sample[:4]), sample[4], places=5)
        # An interpolation between two supported observations preserves the expected money.
        self.assertAlmostEqual(predict(model, .45, 1, .8, 300.), 940., places=5)

    def test_mature_cohort_missing_final_is_reported_and_promo_is_excluded(self):
        logs = (Path(__file__).resolve().parents[1] / "SimLogs").resolve()
        with tempfile.TemporaryDirectory(dir=logs, prefix="single-prior-test-") as folder:
            self.assertTrue(Path(folder).resolve().is_relative_to(logs))
            prefix = Path(folder) / "fixture"

            def write(suffix, fieldnames, records):
                with Path(f"{prefix}-{suffix}.csv").open("w", newline="") as stream:
                    writer = csv.DictWriter(stream, fieldnames=fieldnames)
                    writer.writeheader()
                    writer.writerows(records)

            fields = "week,recordId,chosenFormat,careerState,qualityEstimate,reachFactor,genreSinglesMarketFactor,singleProductionCost,year".split(",")
            base = dict(zip(fields, [1, "final", "Single", "NewSigning", .6, .8, 1., 300., 1960]))
            write("calibration-decisions", fields, [base, base | {"recordId": "missing"},
                  base | {"recordId": "late", "week": 500}, base | {"recordId": "promo", "chosenFormat": "Album"}])
            write("a3-economic-decisions", ["recordId", "priorSingleNet"],
                  [{"recordId": rid, "priorSingleNet": 1000.} for rid in ("final", "missing", "late", "promo")])
            outcome = {"recordId": "final", "format": "Single", "memoryEligible": "true", "realizedNet": 500.}
            write("release-outcomes", list(outcome), [outcome, outcome | {"recordId": "promo"}, outcome | {"recordId": "late"}])
            data, coverage = load(prefix, 418)
            self.assertEqual(len(data), 1)
            self.assertEqual(coverage["launches"], 2)
            self.assertEqual(coverage["missingByCareer"], {"New/Unsigned": 1})
            # Duplicate finals would overweight a release and must be rejected.
            write("release-outcomes", list(outcome), [outcome, outcome])
            with self.assertRaisesRegex(ValueError, "Duplicate final"):
                load(prefix, 418)


if __name__ == "__main__":
    unittest.main()
