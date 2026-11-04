"""Regression: ownership transfers must not rewrite past sales or import pre-window units."""
import tempfile
import unittest
from pathlib import Path

from analyze_offchart_screen import label_tiers, sales_units


class SalesAttributionChecks(unittest.TestCase):
    def test_weekly_seller_survives_transfer_and_lease_reversion(self):
        with tempfile.TemporaryDirectory() as directory:
            p = lambda stream: str(Path(directory) / (stream + ".csv"))
            Path(p("label-sales-weekly")).write_text(
                "week,year,labelId,format,units,chartedUnits,top10Units\n"
                "1,1960,boutique,Single,100,0,0\n"
                "2,1960,buyer,Single,200,200,0\n"
                "3,1960,boutique,Single,50,0,0\n"
                "3,1960,buyer,Album,900,900,0\n")
            Path(p("release-outcomes")).write_text("labelId,format,pressingBookSold\n")
            Path(p("live-records-snapshot")).write_text(
                "labelId,format,totalUnitsSold\nboutique,Single,10350\n")
            weekly = sales_units(p)
            self.assertEqual(weekly.groupby("labelId").units.sum().to_dict(), {"boutique": 150, "buyer": 200})
            self.assertEqual(weekly.chartedUnits.sum(), 200)
            self.assertEqual(sales_units(p, True).units.sum(), 10350)

    def test_old_lean_audit_requires_explicit_legacy_choice(self):
        with tempfile.TemporaryDirectory() as directory:
            with self.assertRaisesRegex(ValueError, "Weekly owner attribution unavailable"):
                sales_units(lambda stream: str(Path(directory) / (stream + ".csv")))

    def test_full_settlement_fallback(self):
        with tempfile.TemporaryDirectory() as directory:
            p = lambda stream: str(Path(directory) / (stream + ".csv"))
            Path(p("completed-week-settlement")).write_text(
                "labelId,format,totalUnits\nboutique,Single,100\nbuyer,Single,200\nbuyer,Album,900\n")
            self.assertEqual(sales_units(p).units.sum(), 300)

    def test_runtime_births_keep_their_birth_tier(self):
        with tempfile.TemporaryDirectory() as directory:
            p = lambda stream: str(Path(directory) / (stream + ".csv"))
            Path(p("label-directory")).write_text("labelId,initialTier\noriginal,Boutique\n")
            Path(p("runtime-label-profiles")).write_text(
                "labelId,birthTier\nnew,Small\noriginal,Independent\n")
            self.assertEqual(label_tiers(p), {"original": "Boutique", "new": "Small"})


if __name__ == "__main__":
    unittest.main()
