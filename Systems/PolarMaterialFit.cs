using System;
using System.Linq;

public static class PolarMaterialFit {
	public static SongProfile Pull(SongProfile reference, ActProfile act) {
		var proposal = reference.Copy();
		float reach = act.interpretiveReach * reference.plasticity;
		for (int i = SongProfile.DemandCount; i < SongProfile.AxisCount; i++)
			proposal.axes[i] += Math.Clamp(act.axes[i] - reference.axes[i], -reach, reach);
		return proposal;
	}
	public static MaterialFit Compute(SongProfile reference, ActProfile act, MarketTasteSnapshot taste, int week, PolarSongTable table) =>
		Evaluate(reference, Pull(reference, act), act, taste, week, table);
	public static MaterialFit Evaluate(SongProfile reference, SongProfile realized, ActProfile act, MarketTasteSnapshot taste, int week, PolarSongTable table) {
		float penalty = 0, deficit = 0;
		int dominant = 0; SongAxis worst = SongAxis.VocalPower;
		for (int i = 0; i < SongProfile.DemandCount; i++) {
			if (realized.axes[i] > realized.axes[dominant]) dominant = i;
			float d = Math.Max(0, realized.axes[i] - act.axes[i]);
			if (d > deficit) { deficit = d; worst = (SongAxis)i; }
			penalty += realized.axes[i] * d * d * table.N("capabilityPenalty");
		}
		// C6: have on dominant axis minus that demand ONCE. WorstAxis is the largest unmet demand.
		float headroom = Math.Max(0, act.axes[dominant] - realized.axes[dominant]);
		float capability = SongProfileDeriver.Clamp(MathF.Exp(-penalty) + headroom * table.N("headroomBonus"));
		float identity = SongProfileDeriver.Clamp(1 - IdentityDistance(realized.axes, act.axes, table) * (table.N("identityBasePenalty") + table.N("rigidityPenalty") * act.identityRigidity));
		float stretch = 0, weight = table.IdentityWeights.Sum();
		for (int i = 0; i < SongProfile.IdentityCount; i++) stretch += Math.Abs(realized.axes[SongProfile.DemandCount + i] - reference.axes[SongProfile.DemandCount + i]) * table.IdentityWeights[i];
		stretch /= weight;
		bool hasMarket = taste != null && taste.observations > 0 && taste.asOfWeek < week;
		float moment = table.N("neutralMoment");
		if (hasMarket) {
			float distance = 0;
			foreach (var axis in new[] { SongAxis.Toughness, SongAxis.Sophistication, SongAxis.Maturity }) {
				float delta = realized[axis] - taste.identity[(int)axis - SongProfile.DemandCount]; distance += delta * delta;
			}
			moment = SongProfileDeriver.Clamp(1 - MathF.Sqrt(distance / 3f) * table.N("momentPenalty"));
		}
		return new MaterialFit(capability, identity, moment, deficit, worst, stretch, IdentityDistance(reference.axes, act.axes, table), hasMarket);
	}
	public static float IdentityDistance(float[] a, float[] b, PolarSongTable table) {
		float distance = 0;
		for (int i = 0; i < SongProfile.IdentityCount; i++) {
			float delta = a[SongProfile.DemandCount + i] - b[SongProfile.DemandCount + i]; distance += delta * delta * table.IdentityWeights[i];
		}
		return MathF.Sqrt(distance / table.IdentityWeights.Sum());
	}
	public static float ReinterpretationOutcome(MaterialFit fit, ActProfile act, float familiarity, PolarSongTable table) =>
		fit.Stretch < table.N("reinterpretationThreshold") ? 0 : fit.Stretch * familiarity *
		(fit.Capability * act.interpretiveReach - table.N("reinterpretationLandedCenter")) * table.N("reinterpretationMultiplier");
	// Also used by the enabled player booking gate; override does not invent morale/economy penalties.
	public static bool WouldRefuse(MaterialFit fit, float ambition, float standing, bool emptySongbook, PolarSongTable table) {
		float threshold = table.N("refusalBase") + table.N("refusalAmbition") * ambition * standing;
		if (emptySongbook) threshold *= table.N("emptySongbookSoftening");
		return fit.Identity < threshold && fit.Stretch >= fit.Capability * table.N("refusalStretchCapability");
	}
}
