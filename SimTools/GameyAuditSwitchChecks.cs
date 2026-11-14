using System;
using System.Linq;
using Godot;

/// <summary>--gamey-audit-switches-check: the three audit switches are the identity when off and do what
/// SimTools/GameyBehaviorAudit.md says when on.</summary>
public static class GameyAuditSwitchChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Gamey audit switches: " + why); }
		bool Near(float a, float b) => Mathf.Abs(a - b) < 1e-5f;
		float floorWas = LpEarnedDiscovery.FloorForChecks, shareWas = ReceivableBackedOverdraft.Share;
		bool staggerWas = StaggeredOpeningContracts.Enabled;
		try {
			// LP earned discovery.
			LpEarnedDiscovery.SetForChecks(1f);
			Check(!LpEarnedDiscovery.Enabled && LpEarnedDiscovery.Factor(0f, 0f) == 1f, "off must leave the discovery term alone");
			LpEarnedDiscovery.SetForChecks(0.25f);
			Check(Near(LpEarnedDiscovery.Factor(0f, 0f), 0.25f), "an unknown act keeps the floor");
			Check(Near(LpEarnedDiscovery.Factor(1f, 0f), 1f) && Near(LpEarnedDiscovery.Factor(0f, LpEarnedDiscovery.KnownActBonus), 1f),
				"a hot act or a known act keeps the whole term");
			Check(Near(LpEarnedDiscovery.Factor(0f, LpEarnedDiscovery.KnownActBonus / 2f), 0.625f), "half known is half way");
			Check(LpEarnedDiscovery.Factor(0.2f, 0.03f) < LpEarnedDiscovery.Factor(0.4f, 0.03f), "more heat must mean more discovery");

			// Staggered opening contracts.
			StaggeredOpeningContracts.SetForChecks(false);
			Check(StaggeredOpeningContracts.ExpiryWeek("artist_00001", 1961, 1960) == 52, "off must keep the year boundary");
			StaggeredOpeningContracts.SetForChecks(true);
			int[] weeks = Enumerable.Range(1, 2000).Select(i => StaggeredOpeningContracts.ExpiryWeek($"artist_{i:D5}", 1961, 1960)).ToArray();
			Check(weeks.Min() >= 1 && weeks.Max() <= 52, "a 1961 expiry must fall in weeks 1-52");
			Check(weeks.Distinct().Count() == 52, "every week must be used");
			Check(weeks.GroupBy(w => (w - 1) / 13).All(g => g.Count() > 400 && g.Count() < 600), "expiries must spread evenly over the quarters");
			Check(StaggeredOpeningContracts.ExpiryWeek("artist_00007", 1963, 1960) is >= 105 and <= 156, "a later expiry keeps its year");
			Check(StaggeredOpeningContracts.ExpiryWeek("artist_00007", 1963, 1960) == StaggeredOpeningContracts.ExpiryWeek("artist_00007", 1963, 1960),
				"the week must be stable");

			// Receivable-backed overdraft.
			ReceivableBackedOverdraft.SetForChecks(0f);
			Check(!ReceivableBackedOverdraft.Carries(-100f, 1000f), "off must never carry");
			ReceivableBackedOverdraft.SetForChecks(0.5f);
			Check(ReceivableBackedOverdraft.Carries(-400f, 1000f), "an overdraft inside half the receivables is carried");
			Check(!ReceivableBackedOverdraft.Carries(-600f, 1000f), "an overdraft past half the receivables is not");
			Check(!ReceivableBackedOverdraft.Carries(50f, 1000f) && !ReceivableBackedOverdraft.Carries(-50f, 0f), "no overdraft or no receivables: nothing to carry");
			GD.Print("GAMEY_AUDIT_SWITCHES_CHECK passed");
		} finally {
			LpEarnedDiscovery.SetForChecks(floorWas);
			StaggeredOpeningContracts.SetForChecks(staggerWas);
			ReceivableBackedOverdraft.SetForChecks(shareWas);
		}
	}
}
