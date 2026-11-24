using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Passive source accounting: original opening stock surviving observed losses.
/// New joins are untagged. Adult capacity loss is allocated proportionally between
/// existing adults and transfers. This is an accounting convention, not a model
/// of identifiable people or indirect descendants of early audience influence.</summary>
public sealed class OpeningFollowingOrigin {
	public Dictionary<int, double> Young = new();
	public double Adult;
	public double YoungTotal => Young.OrderBy(p => p.Key).Sum(p => p.Value);
	public static void Seed(FollowingRegion region) => region.OpeningOrigin = new() {
		Young = region.Young.ToDictionary(p => p.Key, p => (double)p.Value), Adult = region.Adult
	};
	public void Scale(double youngKeep, double adultKeep) {
		foreach (int cohort in Young.Keys.ToArray()) Young[cohort] *= youngKeep;
		Adult *= adultKeep;
	}
	public double AgeCohort(int cohort, float before, float aged, float after) {
		double origin = Young.GetValueOrDefault(cohort);
		if (origin == 0) return 0;
		double ratio = before > 0 ? Math.Clamp(origin / before, 0, 1) : 0;
		if (after < 1) Young.Remove(cohort); else Young[cohort] = Math.Min(after, after * ratio);
		return aged * ratio;
	}
	public void SettleAdult(double survivingAdult, double transferred, double actualAfterCap, double proposedBeforeCap) {
		double keep = proposedBeforeCap > 0 ? Math.Clamp(actualAfterCap / proposedBeforeCap, 0, 1) : 0;
		Adult = Math.Clamp((survivingAdult + transferred) * keep, 0, Math.Max(0, actualAfterCap));
	}
	public void Validate(FollowingRegion region) {
		if (!double.IsFinite(Adult) || Adult < 0 || Adult > region.Adult + Math.Max(.01, region.Adult * 2e-6) ||
			Young.Any(p => !double.IsFinite(p.Value) || p.Value < 0 || p.Value > region.Young.GetValueOrDefault(p.Key) + Math.Max(.01, p.Value * 2e-6)))
			throw new InvalidOperationException("Opening following lineage exceeds observed stock.");
	}
}
