using System;
using Godot;

// Checks the failure modes in A2, price/cost accounting, and the real decision seam's disabled/prewarm identity.
public static class SinglePriorRecalibrationChecks {
	public static void Run() {
		bool saved = SinglePriorRecalibration.Enabled;
		int checks = 0;
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Single prior recalibration: " + why); checks++; }
		try {
			CompetitorManager manager = CompetitorManager.Instance;
			SinglePriorRecalibration.SetForChecks(false);
			float oldEstablished = manager.SinglePriorNetForProbe(.60f, 2, 1f, 1f, 1000f, live: true);
			float oldStar = manager.SinglePriorNetForProbe(.60f, 3, 1f, 1f, 1000f, live: true);
			Check(oldEstablished > oldStar * 2f, "disabled seam retains the frozen Q3 career inversion");
			SinglePriorRecalibration.SetForChecks(true);
			Check(manager.SinglePriorNetForProbe(.60f, 2, 1f, 1f, 1000f, live: false) == oldEstablished,
				"prewarm/non-live decisions retain the frozen prior even with the switch on");
			float newEstablished = manager.SinglePriorNetForProbe(.60f, 2, 1f, 1f, 1000f, live: true);
			float newStar = manager.SinglePriorNetForProbe(.60f, 3, 1f, 1f, 1000f, live: true);
			Check(newStar >= newEstablished && newEstablished < oldEstablished * .5f,
				"live seam removes the Q3 Star penalty and stale Established overprojection");
			bool monotone = true;
			for (int step = 0; step < 1000; step++) {
				float q = step / 1000f;
				for (int band = 0; band < 4; band++) {
					float value = SinglePriorRecalibration.Contribution(q, band);
					monotone &= value <= SinglePriorRecalibration.Contribution(q + .001f, band);
					if (band < 3) monotone &= value <= SinglePriorRecalibration.Contribution(q, band + 1);
				}
			}
			Check(monotone, "quality and career curves are nondecreasing across the whole domain");
			float left = SinglePriorRecalibration.Contribution(.623967f, 0);
			float right = SinglePriorRecalibration.Contribution(.623969f, 0);
			Check(Math.Abs(left - right) < 1f && SinglePriorRecalibration.Contribution(.68f, 0) > right,
				"the old Q3/Q4 boundary is continuous, with resolution above the chart-entry region");
			float contribution = SinglePriorRecalibration.Contribution(.70f, 0);
			float net = SinglePriorRecalibration.EstimateNet(.70f, 0, .8f, 1.2f, 1000f, SinglePriorRecalibration.ReferenceMargin);
			Check(Math.Abs(net - (contribution * .8f * 1.2f - 1000f)) < .01f,
				"fitted booked-price contribution is not discounted a second time and production is subtracted once");
			float halfMargin = SinglePriorRecalibration.EstimateNet(.70f, 0, .8f, 1.2f, 1000f, SinglePriorRecalibration.ReferenceMargin / 2f);
			Check(Math.Abs(halfMargin - ((net + 1000f) / 2f - 1000f)) < .01f,
				"repricing scales contribution without scaling the sunk production cost");
			Check(SinglePriorRecalibration.EstimateNet(.7f, 3, 0f, 1f, 1000f, SinglePriorRecalibration.ReferenceMargin) == -1000f,
				"zero reach preserves the production loss");
			SinglePriorRecalibration.SetForChecks(false);
			Check(manager.SinglePriorNetForProbe(.60f, 2, 1f, 1f, 1000f, live: true) == oldEstablished,
				"switch off restores the exact legacy result");
			GD.Print($"SINGLE_PRIOR_RECALIBRATION_CHECK passed checks={checks}");
		} finally { SinglePriorRecalibration.SetForChecks(saved); }
	}
}
