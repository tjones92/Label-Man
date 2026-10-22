using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>--midtier-opening-realism-check: --workload-scaled-staffing, --defer-british-labels and --midtier-national-ar change only what they name.</summary>
public static class MidTierOpeningRealismChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("MidTier opening realism: " + why); checks++; }
		bool staffingWas = WorkloadStaffing.Enabled, britishWas = BritishLabelDeferral.Enabled;
		bool nationalWas = MidTierNationalAr.Enabled, nationalRuntimeWas = MidTierNationalAr.Runtime;
		string[] london = { "Parlophone", "Pye", "EMI", "Decca UK" };
		try {
			// Staffing: the MidTier tier term follows the roster; other tiers and full rosters are untouched.
			LabelCorporateModel.SetForChecks(true, staff: true, premises: true);
			AILabel Firm(LabelTier tier, int acts) => new() { labelId = $"realism-{tier}-{acts}", labelName = "Realism Check",
				tier = tier, homeCityId = "chicago", cashReserves = 50000f, releasesPerMonth = 2f,
				roster = Enumerable.Range(0, acts).Select(i => new SimulatedArtist { artistId = $"act-{i}" }).ToList(),
				company = new LabelCompanyState { ownership = tier == LabelTier.Major ? LabelOwnershipForm.PublicCorporation : LabelOwnershipForm.PrivateCorporation } };
			double Payroll(LabelTier tier, int acts, bool on) {
				WorkloadStaffing.SetEnabledForChecks(on);
				var firm = Firm(tier, acts);
				LabelCorporateModel.InitializeOpeningOrganization(firm);
				return firm.company.payrollQuote;
			}
			double thinOff = Payroll(LabelTier.MidTier, 8, false), thinOn = Payroll(LabelTier.MidTier, 8, true);
			Check(thinOn < thinOff * .8, $"an 8-act MidTier must pay materially less with the switch ({thinOn:F0} vs {thinOff:F0})");
			Check(Payroll(LabelTier.MidTier, 15, true) == Payroll(LabelTier.MidTier, 15, false), "a reference-size MidTier is unchanged");
			Check(Payroll(LabelTier.MidTier, 30, true) == Payroll(LabelTier.MidTier, 30, false), "the tier term is never raised");
			Check(Payroll(LabelTier.MidTier, 0, true) > 0 && Payroll(LabelTier.MidTier, 0, true) < Payroll(LabelTier.MidTier, 0, false),
				"an empty MidTier keeps a skeleton office");
			Check(Payroll(LabelTier.Major, 8, true) == Payroll(LabelTier.Major, 8, false), "Majors are untouched");
			Check(Payroll(LabelTier.Independent, 8, true) == Payroll(LabelTier.Independent, 8, false), "Independents are untouched");
			Check(Payroll(LabelTier.MidTier, 12, true) > Payroll(LabelTier.MidTier, 8, true), "payroll still rises with the roster");

			// British deferral: the four London templates leave; nothing replaces them; the US set is unchanged.
			List<AILabel> Generate(bool on) { BritishLabelDeferral.SetEnabledForChecks(on); GameRandom.Seed(17017); return AILabelFactory.GenerateAllLabels(600); }
			bool IsLondon(AILabel l) => l.isHistorical && london.Any(n => l.labelName == n || l.labelName.StartsWith(n + " ", StringComparison.Ordinal));
			var off = Generate(false);
			var on = Generate(true);
			Check(off.Count == 600 && off.Count(IsLondon) == 4, $"off must keep the four London templates ({off.Count(IsLondon)})");
			Check(on.Count == 596 && on.Count(IsLondon) == 0, $"on must drop exactly the London templates ({on.Count}, {on.Count(IsLondon)})");
			Check(on.All(l => l.headquartersCity != "London"), "on must open no London label");
			string Traits(AILabel l) => $"{l.labelId}|{(l.isHistorical ? l.labelName : "procedural")}|{l.tier}|{l.headquartersCity}|{l.budgetLevel}|{l.nationalReach}|{l.releasesPerMonth}|{l.maxRosterSize}|{l.cashReserves}";
			var twins = off.Where(l => !IsLondon(l)).ToList();
			for (int i = 0; i < twins.Count; i++) Check(Traits(on[i]) == Traits(twins[i]), $"every other label must keep its id and traits ({Traits(twins[i])} vs {Traits(on[i])})");
			Check(on.Count(l => l.isHistorical) == off.Count(l => l.isHistorical) - 4, "no historical template may be added");

			// National A&R: a capable MidTier reaches beyond 650 road miles only where the switch says, and nobody else gains.
			AILabel Scout(LabelTier tier, float scouting = .75f, float reach = .8f) => new() { labelId = "realism-scout", tier = tier,
				scoutingAbility = scouting, nationalReach = reach, geography = new GeographicIdentity { basePlaceId = "new_york" } };
			var seattle = new SimulatedArtist { artistId = "realism-seattle", geography = new GeographicIdentity { basePlaceId = "seattle", originPlaceId = "seattle" } };
			var boston = new SimulatedArtist { artistId = "realism-boston", geography = new GeographicIdentity { basePlaceId = "boston", originPlaceId = "boston" } };
			string Route(AILabel label, SimulatedArtist act, bool launch) {
				string route = null;
				if (launch) MidTierNationalAr.RunLaunchAllocation(() => route = SceneRecruitmentService.Explain(label, act, 0).Route);
				else route = SceneRecruitmentService.Explain(label, act, 0).Route;
				return route;
			}
			MidTierNationalAr.SetEnabledForChecks(false);
			Check(Route(Scout(LabelTier.MidTier), seattle, true) == "OutsideReach", "off, a MidTier keeps its 650-mile radius at launch");
			MidTierNationalAr.SetEnabledForChecks(true);
			Check(Route(Scout(LabelTier.MidTier), seattle, true) == "NationalAr", "on, a capable MidTier reaches nationally at launch");
			Check(Route(Scout(LabelTier.MidTier), seattle, false) == "OutsideReach", "launch-only leaves weekly recruitment unchanged");
			Check(Route(Scout(LabelTier.MidTier), boston, true) == "RoadCircuit", "road-circuit acts keep their nearer route");
			Check(Route(Scout(LabelTier.MidTier, scouting: .4f), seattle, true) == "OutsideReach", "weak scouting confers no national A&R");
			Check(Route(Scout(LabelTier.MidTier, reach: .4f), seattle, true) == "OutsideReach", "weak national reach confers no national A&R");
			Check(Route(Scout(LabelTier.Independent), seattle, true) == "OutsideReach", "Independents are untouched");
			Check(Route(Scout(LabelTier.Major), seattle, false) == "NationalAr", "Majors keep national A&R");
			MidTierNationalAr.SetEnabledForChecks(false, runtime: true);
			Check(Route(Scout(LabelTier.MidTier), seattle, false) == "NationalAr", "the runtime switch keeps the route open after launch");
		} finally {
			WorkloadStaffing.SetEnabledForChecks(staffingWas);
			BritishLabelDeferral.SetEnabledForChecks(britishWas);
			MidTierNationalAr.SetEnabledForChecks(nationalWas, nationalRuntimeWas);
		}
		GD.Print($"MIDTIER_OPENING_REALISM_CHECKS passed={checks}");
	}
}
