using System;
using Godot;

/// <summary>--label-footprint-parity-check: off, every read is the identity; on, the purchase-rate scale and a
/// non-Major's covered launch factor move the configured share of the way to 1, a Major's launch factor and strong and
/// uncovered regions are untouched, and a held region arrives inside the configured national depth.</summary>
public static class LabelFootprintParityChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Label footprint parity: " + why); }
		bool Near(float a, float b) => Mathf.Abs(a - b) < 1e-5f;
		float demandWas = LabelFootprintParity.DemandParity, coveredWas = LabelFootprintParity.CoveredLaunchParity;
		int depthWas = LabelFootprintParity.ArrivalChartDepth, rankWas = LabelFootprintParity.FitExposureNationalRank;
		try {
			var indie = new AILabel { labelId = "indie", tier = LabelTier.Independent, distributionStrength = 0.4f, nationalReach = 0.3f,
				distributionRegions = new[] { "eastcoast" }, strongRegions = new[] { "greatlakes" } };
			var major = new AILabel { labelId = "major", tier = LabelTier.Major, distributionStrength = 0.9f, nationalReach = 0.9f,
				distributionRegions = new[] { "eastcoast" }, strongRegions = Array.Empty<string>() };

			LabelFootprintParity.SetForChecks(0f);
			float indieScale = ChartSimulator.GetLiveLabelDemandScale(indie, null), majorScale = ChartSimulator.GetLiveLabelDemandScale(major, null);
			Check(indieScale < 1f && majorScale > 1f, "the fixtures must straddle 1");
			Check(LabelFootprintParity.Demand(0.7f) == 0.7f && LabelFootprintParity.Demand(1.2f) == 1.2f, "off must return the scale unchanged");
			float coveredOff = ChartSimulator.GetRegionalLaunchFactor(indie, "eastcoast");
			float strongOff = ChartSimulator.GetRegionalLaunchFactor(indie, "greatlakes");
			float uncoveredOff = ChartSimulator.GetRegionalLaunchFactor(indie, "westcoast");
			float majorOff = ChartSimulator.GetRegionalLaunchFactor(major, "eastcoast");
			Check(coveredOff < 1f && strongOff > 1f && uncoveredOff < coveredOff, "launch factor fixtures");

			LabelFootprintParity.SetForChecks(1f, 100, 1f, 100);
			Check(Near(ChartSimulator.GetLiveLabelDemandScale(indie, null), 1f) && Near(ChartSimulator.GetLiveLabelDemandScale(major, null), 1f),
				"full demand parity must give every label a scale of 1");
			Check(Near(ChartSimulator.GetRegionalLaunchFactor(indie, "eastcoast"), 1f), "full covered parity must launch a non-Major at 1 where it ships");
			Check(ChartSimulator.GetRegionalLaunchFactor(indie, "greatlakes") == strongOff, "a strong region must be untouched");
			Check(ChartSimulator.GetRegionalLaunchFactor(indie, "westcoast") == uncoveredOff, "an uncovered region must be untouched");
			Check(ChartSimulator.GetRegionalLaunchFactor(major, "eastcoast") == majorOff, "a Major's launch factor must be untouched");

			LabelFootprintParity.SetForChecks(0.5f, 40, 0.5f);
			Check(Near(ChartSimulator.GetLiveLabelDemandScale(indie, null), indieScale + (1f - indieScale) * 0.5f), "half demand parity must move half way");
			Check(Near(ChartSimulator.GetRegionalLaunchFactor(indie, "eastcoast"), coveredOff + (1f - coveredOff) * 0.5f), "half covered parity must move half way");

			var record = new RecordRuntimeData { baseRecord = new Record { recordId = "r", format = ReleaseFormat.Single }, currentPosition = 70 };
			var held = new RegionalRecordData("westcoast") { awaitingArrival = true };
			RegionalAudience.TryArrive(record, held, indie);
			Check(held.awaitingArrival, "position 70 must not arrive at depth 40");
			LabelFootprintParity.SetForChecks(0f, 100);
			RegionalAudience.TryArrive(record, held, indie);
			Check(!held.awaitingArrival, "position 70 must arrive at depth 100");
			GD.Print("LABEL_FOOTPRINT_PARITY_CHECK passed");
		} finally {
			LabelFootprintParity.SetForChecks(demandWas, depthWas, coveredWas, rankWas);
		}
	}
}
