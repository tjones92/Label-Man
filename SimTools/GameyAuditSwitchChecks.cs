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
		bool staggerWas = StaggeredOpeningContracts.Enabled, launchWas = LpEarnedLaunch.Enabled, anniversaryWas = AnniversaryOpeningContracts.Enabled, parityWas = LpLabelDemandParity.Enabled;
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

			// LP earned launch and LP label demand parity.
			LpEarnedLaunch.SetForChecks(false);
			Check(LpEarnedLaunch.Base(0.04f, 0f) == 0.04f, "off must keep the launch base");
			LpEarnedLaunch.SetForChecks(true);
			Check(LpEarnedLaunch.Base(0.04f, 0f) == 0f && Near(LpEarnedLaunch.Base(0.04f, LpEarnedDiscovery.KnownActBonus), 0.04f), "an unknown act earns no base, a known act the whole base");
			LpLabelDemandParity.SetForChecks(false);
			Check(LpLabelDemandParity.Apply(0.5f) == 0.5f, "off must keep the tier scale");
			LpLabelDemandParity.SetForChecks(true);
			Check(Near(LpLabelDemandParity.Apply(0.5f), LabelFootprintParity.Demand(0.5f)), "on must follow the singles parity share");

			// Anniversary opening contracts.
			int[] sw = Enumerable.Range(1, 2000).Select(i => AnniversaryOpeningContracts.SigningWeek($"artist_{i:D5}")).ToArray();
			Check(sw.Min() == 0 && sw.Max() == 51 && sw.GroupBy(w => w / 13).All(g => g.Count() > 400 && g.Count() < 600), "signing weeks must spread over the year");
			foreach (string id in new[] { "artist_00001", "artist_00450", "artist_01999" }) {
				int w = AnniversaryOpeningContracts.SigningWeek(id);
				Check(AnniversaryOpeningContracts.ExpiryWeek(id, 1958, 3, 1960) == w - 104 + 156, "a live deal matures on its anniversary after its term");
				int lapsed = AnniversaryOpeningContracts.ExpiryWeek(id, 1955, 3, 1960);
				Check(lapsed >= 1 && lapsed <= 52 && (lapsed - w) % 52 == 0, "a lapsed deal comes due on its 1960 anniversary");
				int fresh = AnniversaryOpeningContracts.ExpiryWeek(id, 1960, 3, 1960);
				Check(fresh >= 105 && fresh <= 156 && (fresh - w) % 52 == 0, "a 1960 signing is dated in the year before the opening");
			}

			// Renewal live evidence.
			Check(RenewalLiveEvidence.IsEvidence(55, 0, 0) && RenewalLiveEvidence.IsEvidence(0, 40, 0) && RenewalLiveEvidence.IsEvidence(0, 0, 1),
				"charting now, charted, or broken out each count");
			Check(!RenewalLiveEvidence.IsEvidence(0, 0, 0) && !RenewalLiveEvidence.IsEvidence(0, 150, 0), "nothing, or an off-chart peak, is no evidence");
			var clock = new SimulatedArtist { contractExpiresWeek = 100, contractSinglesObligation = 4, contractReleases = 4 };
			Check(RosterManager.IsContractMatured(clock, 1961, 60) && !RenewalLiveEvidence.TermMatured(clock, 1961, 60) && RenewalLiveEvidence.TermMatured(clock, 1961, 100),
				"with the switch a delivered obligation waits for the date");

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
			Check(ReceivableBackedOverdraft.MonthlyCharge > 0f && ReceivableBackedOverdraft.MonthlyCharge <= 0.05f, "the charge must be a monthly rate");
			ReceivableBackedOverdraft.SetForChecks(0.5f);
			Check(ReceivableBackedOverdraft.Carries(-400f, 1000f), "an overdraft inside half the receivables is carried");
			Check(!ReceivableBackedOverdraft.Carries(-600f, 1000f), "an overdraft past half the receivables is not");
			Check(!ReceivableBackedOverdraft.Carries(50f, 1000f) && !ReceivableBackedOverdraft.Carries(-50f, 0f), "no overdraft or no receivables: nothing to carry");
			GD.Print("GAMEY_AUDIT_SWITCHES_CHECK passed");
		} finally {
			LpEarnedDiscovery.SetForChecks(floorWas);
			StaggeredOpeningContracts.SetForChecks(staggerWas);
			LpEarnedLaunch.SetForChecks(launchWas);
			AnniversaryOpeningContracts.SetForChecks(anniversaryWas);
			LpLabelDemandParity.SetForChecks(parityWas);
			ReceivableBackedOverdraft.SetForChecks(shareWas);
		}
	}
}
