using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>--seed-head-templates-check: off leaves the roster alone, and on, seeds exactly the §17l templates.</summary>
public static class HeadScaleTemplatesChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Head-scale templates: " + why); }
		bool was = HeadScaleTemplates.Enabled;
		string[] added = { "MGM", "ABC-Paramount", "Warner Bros." };
		string[] head = { "MGM", "ABC-Paramount", "Warner Bros.", "Cameo-Parkway" };
		List<AILabel> Generate(bool on) { HeadScaleTemplates.SetEnabledForChecks(on); GameRandom.Seed(17017); return AILabelFactory.GenerateAllLabels(600); }
		AILabel Named(List<AILabel> labels, string name) {
			var found = labels.Where(l => l.isHistorical && AILabelFactory.IsHeadScaleProbeTemplate(l.labelName) &&
				(l.labelName == name || l.labelName.StartsWith(name + " ", StringComparison.Ordinal))).ToList();
			return found.Count == 1 ? found[0] : null;
		}
		string Traits(AILabel l) => $"{l.labelId}|{(l.isHistorical ? l.labelName : "procedural")}|{l.tier}|{l.budgetLevel}|{l.marketingPower}|{l.distributionStrength}|" +
			$"{l.nationalReach}|{l.releasesPerMonth}|{l.maxRosterSize}|{l.cashReserves}|{string.Join(",", l.distributionRegions)}";
		try {
			var off = Generate(false);
			Check(off.Count == 600, "off must keep 600 labels");
			foreach (string name in added) Check(Named(off, name) == null, $"off must not seed {name}");
			Check(Named(off, "Liberty")?.tier == LabelTier.Independent && Named(off, "Liberty").archetype == LabelArchetype.RockRebel,
				"off must keep Liberty an Independent RockRebel");
			Check(Named(off, "Vee-Jay")?.tier == LabelTier.Independent, "off must keep Vee-Jay Independent");
			Check(Named(off, "Cameo-Parkway")?.releasesPerMonth <= 2.71f, "off must keep Cameo-Parkway's MidTier volume");
			var again = Generate(false);
			int firstDiff = Enumerable.Range(0, 600).FirstOrDefault(i => Traits(again[i]) != Traits(off[i]), -1);
			Check(firstDiff < 0, $"off must be deterministic for a seed: {(firstDiff < 0 ? "" : Traits(off[firstDiff]) + " vs " + Traits(again[firstDiff]))}");

			var on = Generate(true);
			Check(on.Count == 600, "on must keep 600 labels");
			for (int i = 0; i < 5; i++) Check(Traits(on[i]) == Traits(off[i]), $"the Big-5 ({off[i].labelName}) must be unchanged");
			for (int i = 0; i < added.Length; i++) {
				AILabel label = Named(on, added[i]);
				Check(label != null, $"on must seed {added[i]} once");
				Check(label.labelId == $"label_{43 + i:D4}", $"{added[i]} must follow the existing 42 templates, got {label.labelId}");
			}
			foreach (string name in head) {
				AILabel l = Named(on, name);
				Check(l != null && l.tier == LabelTier.MidTier, $"{name} must be MidTier");
				Check(l.budgetLevel >= 0.78f && l.marketingPower >= 0.70f && l.distributionStrength >= 0.75f && l.nationalReach >= 0.75f,
					$"{name} must draw the Major launch-channel envelope ({Traits(l)})");
				Check(l.releasesPerMonth >= 1.6f && l.releasesPerMonth <= 2.61f, $"{name} must draw head volume, got {l.releasesPerMonth}");
				Check(l.maxRosterSize >= 25 && l.maxRosterSize <= 40, $"{name} must draw a head roster, got {l.maxRosterSize}");
				Check(l.cashReserves >= 50000f, $"{name} must open with Major cash, got {l.cashReserves}");
				Check(l.distributionRegions.Length >= 6, $"{name} must take the Major region count, got {l.distributionRegions.Length}");
			}
			Check(Named(on, "Warner Bros.").cashReserves == 50000f, "Warner Bros. must open at the bottom of the Major band");
			AILabel liberty = Named(on, "Liberty");
			Check(liberty.archetype == LabelArchetype.TeenHitMachine && liberty.preferredGenres.Contains(Genre.EasyListening),
				"Liberty must become a TeenHitMachine with EasyListening");
			Check(liberty.tier == LabelTier.MidTier && liberty.budgetLevel <= 0.82f && liberty.distributionRegions.Length <= 5,
				$"Liberty must be a standard MidTier, not head scale ({Traits(liberty)})");

			// Stand-in year-one Major promotion hold (§17s): probe templates only, first 12 months only, on only.
			AILabel mgm = Named(on, "MGM");
			AILabel procedural = on.First(l => !l.isHistorical && l.tier == LabelTier.MidTier);
			int mgmMonths = mgm.monthsActive, procMonths = procedural.monthsActive;
			try {
				mgm.monthsActive = HeadScaleTemplates.MajorPromotionHoldMonths - 1;
				Check(HeadScaleTemplates.HoldsMajorPromotion(mgm), "a template must be held in its first year");
				Check(HeadScaleTemplates.HoldsMajorPromotion(liberty) == (liberty.monthsActive < HeadScaleTemplates.MajorPromotionHoldMonths),
					"a standard-envelope template must be held too");
				mgm.monthsActive = HeadScaleTemplates.MajorPromotionHoldMonths;
				Check(!HeadScaleTemplates.HoldsMajorPromotion(mgm), "the hold must end after twelve months");
				procedural.monthsActive = 0;
				Check(!HeadScaleTemplates.HoldsMajorPromotion(procedural), "a procedural label must never be held");
				mgm.monthsActive = 0;
				HeadScaleTemplates.SetEnabledForChecks(false);
				Check(!HeadScaleTemplates.HoldsMajorPromotion(mgm), "the hold must be off with the switch off");
				HeadScaleTemplates.SetEnabledForChecks(true);
			} finally {
				mgm.monthsActive = mgmMonths; procedural.monthsActive = procMonths;
			}
			AILabel veeJay = Named(on, "Vee-Jay");
			Check(veeJay != null && veeJay.tier == LabelTier.MidTier && veeJay.budgetLevel <= 0.82f && veeJay.releasesPerMonth <= 2.5f &&
				veeJay.distributionRegions.Length <= 5, $"Vee-Jay must be a standard MidTier ({(veeJay == null ? "missing" : Traits(veeJay))})");
			Check(on.Count(l => l.tier == LabelTier.Major && l.isHistorical) == off.Count(l => l.tier == LabelTier.Major && l.isHistorical),
				"the switch must not add a Major");
			GD.Print("HEAD_SCALE_TEMPLATES_CHECK passed");
		} finally {
			HeadScaleTemplates.SetEnabledForChecks(was);
		}
	}
}
