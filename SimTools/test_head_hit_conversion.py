import unittest

import pandas as pd

from analyze_head_hit_conversion import audit_year, conversion_cohorts, decompose, quality_standardization, require_complete_chart, sampled_artist_heat, single_launches


class HeadHitConversionTests(unittest.TestCase):
    def test_album_promo_maps_to_the_single_and_standalone_album_is_excluded(self):
        strategies = pd.DataFrame([
            dict(chosenFormat="Single", strategy="OrphanSingle", recordId="s1", promoSingleId=None),
            dict(chosenFormat="Album", strategy="AlbumWithPromo", recordId="lp1", promoSingleId="s2"),
            dict(chosenFormat="Album", strategy="AlbumStandalone", recordId="lp2", promoSingleId=None),
        ])
        self.assertEqual(list(single_launches(strategies).singleId), ["s1", "s2"])
        strategies.loc[1, "promoSingleId"] = "s1"
        with self.assertRaises(ValueError):
            single_launches(strategies)
        strategies.loc[1, "promoSingleId"] = None
        with self.assertRaises(ValueError):
            single_launches(strategies)

    def test_fixed_followup_excludes_late_success_and_censored_launches(self):
        launches = pd.DataFrame(dict(singleId=["late", "eight", "outside", "miss", "censored"],
                                     week=[1, 48, 49, 2, 48], year=[1960] * 5))
        chart = pd.DataFrame(dict(recordId=["late", "late", "eight", "outside"],
                                  week=[8, 9, 55, 50], currentPosition=[80, 1, 10, 1]))
        observed = conversion_cohorts(launches, chart, through=55).set_index("singleId")
        self.assertNotIn("outside", observed.index)
        self.assertTrue(observed.loc["late", "hot100"])
        self.assertFalse(observed.loc["late", "top10"])
        self.assertTrue(observed.loc["eight", "top10"])
        self.assertFalse(observed.loc["miss", "hot100"])
        shortened = conversion_cohorts(launches, chart, through=54)
        self.assertNotIn("eight", set(shortened.singleId))
        self.assertNotIn("censored", set(shortened.singleId))

    def test_audit_year_at_calendar_boundary_and_missing_interior_week(self):
        self.assertEqual(list(audit_year(pd.Series([0, 1, 52, 53, 104, 105]))),
                         [1959, 1960, 1960, 1961, 1961, 1962])
        chart = pd.DataFrame([dict(week=w, recordId=f"r{r}", currentPosition=r)
                              for w in (1, 2, 3) for r in range(1, 101)])
        require_complete_chart(chart, 3)
        with self.assertRaises(ValueError):
            require_complete_chart(chart[chart.week.ne(2)], 3)
        with self.assertRaises(ValueError):
            require_complete_chart(chart.iloc[:-1], 3)
        with self.assertRaises(ValueError):
            require_complete_chart(pd.concat([chart, pd.DataFrame([dict(week=1, recordId="extra", currentPosition=100)])]), 3)

    def test_constant_volume_attributes_mercury_hit_gain_to_conversion(self):
        old = dict(launches=48, entryRate=33 / 48, conversionGivenEntry=11 / 33, top10=11)
        new = dict(launches=48, entryRate=38 / 48, conversionGivenEntry=17 / 38, top10=17)
        change = decompose(old, new)
        self.assertEqual(change["volume"], 0)
        self.assertAlmostEqual(change["chartEntry"], 5 / 3)
        self.assertAlmostEqual(change["conditionalTop10"], 13 / 3)
        self.assertEqual(change["hitChange"], 6)

    def test_heat_sampling_keeps_zero_heat_new_artists(self):
        chart = pd.DataFrame(dict(week=[1, 1, 2], artistId=["established", "new", "established"]))
        heat = pd.DataFrame(dict(week=[1], artistId=["established"], originalHeat=[0.45], singlesOnlyHeat=[0.0]))
        sampled = sampled_artist_heat(chart, heat)
        self.assertEqual(len(sampled), 2)
        self.assertEqual(sampled.loc[sampled.artistId.eq("new"), "originalHeat"].iloc[0], 0)
        self.assertAlmostEqual(sampled.originalHeat.mean(), 0.225)

    def test_quality_standardization_separates_mix_from_conversion(self):
        def cohort(low_count, low_hits, high_count, high_hits):
            rows = []
            for quality, n, hits in [(0.6, low_count, low_hits), (0.8, high_count, high_hits)]:
                rows.extend(dict(singleId=f"{quality}-{i}", quality=quality, hot100=True, top10=i < hits) for i in range(n))
            return pd.DataFrame(rows)
        result = quality_standardization(cohort(80, 8, 20, 10), cohort(20, 4, 80, 48))
        self.assertEqual(result["controlEntrantsCovered"], 100)
        self.assertAlmostEqual(result["controlConditionalRate"], 0.18)
        self.assertAlmostEqual(result["candidateConditionalRate"], 0.52)
        self.assertAlmostEqual(result["candidateRateWithControlQualityMix"], 0.28)


if __name__ == "__main__":
    unittest.main()
