using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>--midtier-opening-realism-check: --workload-scaled-staffing and --defer-british-labels change only what they name.</summary>
public static class MidTierOpeningRealismChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("MidTier opening realism: " + why); checks++; }
		bool staffingWas = WorkloadStaffing.Enabled, britishWas = BritishLabelDeferral.Enabled;
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
		} finally {
			WorkloadStaffing.SetEnabledForChecks(staffingWas);
			BritishLabelDeferral.SetEnabledForChecks(britishWas);
		}
		GD.Print($"MIDTIER_OPENING_REALISM_CHECKS passed={checks}");
	}
}
