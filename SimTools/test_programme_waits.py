import unittest
from analyze_programme_waits import episodes, summarize


def row(week, intended, selected="0", eligible="1", fmt="Single"):
    return dict(labelId="label", artistId="act", format=fmt, week=str(week), intendedWeek=str(intended),
                eligible=eligible, selected=selected, gap="20", hot="1")


class ProgrammeWaitTests(unittest.TestCase):
    def test_wait_then_select(self):
        result = episodes([row(1, 3), row(2, 5, "1")])
        self.assertEqual((result[0]["opportunities"], result[0]["reviewWeeks"], result[0]["intendedWeeks"]), (2, 1, 2))
        self.assertEqual(summarize(result)["Single_hot"]["selected"], 1)

    def test_exclusion_and_end_are_censored(self):
        result = episodes([row(1, 3), row(2, 4, eligible="0"), row(3, 5)])
        self.assertEqual([e["end"] for e in result], ["ineligible", "audit-end"])
        self.assertEqual(summarize(result)["Single_all"]["selectedMeanReviewWeeks"], None)

    def test_formats_have_independent_episodes(self):
        result = episodes([row(1, 3), row(1, 3, "1", fmt="Album"), row(2, 4, "1")])
        self.assertEqual([e["opportunities"] for e in result], [1, 2])

    def test_revisited_horizon_does_not_create_negative_wait(self):
        result = episodes([row(1, 8), row(2, 4, "1")])
        self.assertEqual(result[0]["intendedWeeks"], 0)


if __name__ == "__main__":
    unittest.main()
