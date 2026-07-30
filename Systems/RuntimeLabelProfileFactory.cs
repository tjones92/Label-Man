using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Enabled-lifecycle operating profile for a label founded during the simulation.
/// This intentionally sits beside, rather than inside, <see cref="LabelGenerator"/>:
/// that generator remains the legacy identity shell and must retain its shared-RNG
/// schedule for disabled compatibility.  All decisions here use a local stable PRNG.
/// </summary>
public static class RuntimeLabelProfileFactory {
	public const string ProfileVersion = "runtime-founded-v1";

	// Section 27: fraction of Independent founders generated as dependent "Stax" hitmakers --
	// high creative capability, low owned reach, financially fragile, and locked out of
	// self-built distribution -- so they chart through a major's network, stay dependent, and
	// are absorbed late-decade with real chart volume. Deliberately a minority; the rest of the
	// dependent population stays the weak one-or-two-hit labels.
	public const float DependentHitmakerShare = 0.12f;

	public sealed class Result {
		public AILabel Label { get; }
		public int BirthWeek { get; }
		public GameDate BirthDate { get; }
		public ulong Seed { get; }
		internal Result(AILabel label, int birthWeek, GameDate birthDate, ulong seed) {
			Label = label; BirthWeek = birthWeek; BirthDate = birthDate; Seed = seed;
		}
	}

	private readonly record struct Envelope(float BudgetMin, float BudgetMax, float MarketingMin, float MarketingMax,
		float ReachMin, float ReachMax, float NationalMin, float NationalMax, float ScoutingMin, float ScoutingMax,
		float ProductionMin, float ProductionMax, float CadenceMin, float CadenceMax);

	private static readonly LabelArchetype[] SmallArchetypes = {
		LabelArchetype.RegionalHustler, LabelArchetype.RockRebel, LabelArchetype.BluesRoots,
		LabelArchetype.CountrySpecialist, LabelArchetype.GospelPowerhouse
	};
	private static readonly LabelArchetype[] IndependentArchetypes = {
		LabelArchetype.SoulFactory, LabelArchetype.RockRebel, LabelArchetype.BluesRoots,
		LabelArchetype.CountrySpecialist, LabelArchetype.TeenHitMachine, LabelArchetype.GospelPowerhouse,
		LabelArchetype.RegionalHustler
	};

	public static Result Initialize(AILabel label, MarketRegion[] regions, int birthWeek, GameDate birthDate, ulong seed) {
		if (label == null) throw new ArgumentNullException(nameof(label));
		if (label.tier != LabelTier.Small && label.tier != LabelTier.Independent)
			throw new InvalidOperationException("Runtime founded labels must begin at Small or Independent tier.");
		var random = new StableRandom(Mix(seed, label.labelId, birthWeek, ProfileVersion));
		label.archetype = SelectArchetype(label.tier, birthDate.year, ref random);
		AssignGenres(label, ref random);
		ApplyOperatingProfile(label, ref random);
		ReconcileFoundingAndGeography(label, regions, birthDate.year);
		return new Result(label, birthWeek, birthDate, seed);
	}

	public static bool IsValidRuntimePair(LabelTier tier, LabelArchetype archetype) =>
		(tier == LabelTier.Small && SmallArchetypes.Contains(archetype)) ||
		(tier == LabelTier.Independent && IndependentArchetypes.Contains(archetype));

	public static bool HasCompleteOperatingProfile(AILabel label) => label != null &&
		label.budgetLevel > 0f && label.scoutingAbility > 0f && label.productionQuality > 0f &&
		label.marketingPower > 0f && label.ownedReach > 0f && label.nationalReach > 0f &&
		label.riskTolerance > 0f && label.artistLoyalty > 0f && label.payolaWillingness > 0f &&
		label.releasesPerMonth > 0f;

	private static LabelArchetype SelectArchetype(LabelTier tier, int year, ref StableRandom random) {
		LabelArchetype[] choices = tier == LabelTier.Small ? SmallArchetypes : IndependentArchetypes;
		var weights = new List<float>(choices.Length);
		foreach (LabelArchetype archetype in choices) weights.Add(archetype switch {
			LabelArchetype.SoulFactory => year >= 1962 ? 20f : 10f,
			LabelArchetype.RockRebel => year >= 1964 ? 25f : 15f,
			LabelArchetype.BluesRoots => year < 1965 ? 15f : 8f,
			LabelArchetype.RegionalHustler => tier == LabelTier.Small ? 25f : 10f,
			LabelArchetype.CountrySpecialist => 15f,
			LabelArchetype.TeenHitMachine => 15f,
			LabelArchetype.GospelPowerhouse => 8f,
			_ => 1f
		});
		float roll = random.Next01() * weights.Sum();
		for (int index = 0; index < choices.Length; index++) {
			roll -= weights[index];
			if (roll <= 0f) return choices[index];
		}
		return choices[^1];
	}

	private static void AssignGenres(AILabel label, ref StableRandom random) {
		label.preferredGenres = label.archetype switch {
			LabelArchetype.SoulFactory => new[] { Genre.Soul, Genre.RnB },
			LabelArchetype.RockRebel => new[] { Genre.RockAndRoll, Genre.GarageRock },
			LabelArchetype.BluesRoots => new[] { Genre.RnB, Genre.RockAndRoll },
			LabelArchetype.CountrySpecialist => new[] { Genre.Country },
			LabelArchetype.TeenHitMachine => new[] { Genre.TeenPop, Genre.RockAndRoll },
			LabelArchetype.GospelPowerhouse => new[] { Genre.Gospel },
			LabelArchetype.RegionalHustler => random.Next01() < .5f ? new[] { Genre.RnB, Genre.Soul } : new[] { Genre.Country, Genre.Gospel },
			_ => Array.Empty<Genre>()
		};
		label.secondaryGenres = label.archetype switch {
			LabelArchetype.SoulFactory => new[] { Genre.Gospel },
			LabelArchetype.RockRebel => new[] { Genre.SurfRock },
			LabelArchetype.CountrySpecialist => new[] { Genre.Folk, Genre.Gospel },
			LabelArchetype.BluesRoots => new[] { Genre.Gospel },
			LabelArchetype.GospelPowerhouse => new[] { Genre.Soul },
			_ => Array.Empty<Genre>()
		};
	}

	private static void ApplyOperatingProfile(AILabel label, ref StableRandom random) {
		Envelope envelope = label.tier == LabelTier.Small
			? new(.10f, .40f, .18f, .56f, .12f, .42f, .07f, .30f, .34f, .84f, .28f, .80f, .20f, .80f)
			: new(.28f, .62f, .30f, .72f, .28f, .62f, .18f, .50f, .44f, .91f, .40f, .91f, .50f, 1.50f);
		label.budgetLevel = Sample(envelope.BudgetMin, envelope.BudgetMax, ref random);
		label.marketingPower = Sample(envelope.MarketingMin, envelope.MarketingMax, ref random);
		label.ownedReach = Sample(envelope.ReachMin, envelope.ReachMax, ref random);
		label.nationalReach = Sample(envelope.NationalMin, envelope.NationalMax, ref random);
		label.scoutingAbility = Sample(envelope.ScoutingMin, envelope.ScoutingMax, ref random);
		label.productionQuality = Sample(envelope.ProductionMin, envelope.ProductionMax, ref random);
		label.releasesPerMonth = Sample(envelope.CadenceMin, envelope.CadenceMax, ref random);
		label.riskTolerance = Sample(.18f, .72f, ref random);
		label.artistLoyalty = Sample(.28f, .78f, ref random);
		label.payolaWillingness = Sample(.08f, .58f, ref random);

		// Archetype modifiers are directional only and are always clamped to the
		// canonical Small/Independent envelopes above.
		switch (label.archetype) {
			case LabelArchetype.SoulFactory: label.productionQuality += .08f; label.marketingPower += .06f; label.artistLoyalty += .08f; label.releasesPerMonth += .18f; label.riskTolerance -= .08f; break;
			case LabelArchetype.RockRebel: label.riskTolerance += .14f; label.scoutingAbility += .08f; label.productionQuality -= .08f; label.artistLoyalty -= .08f; break;
			case LabelArchetype.TeenHitMachine: label.marketingPower += .10f; label.productionQuality += .07f; label.scoutingAbility += .07f; label.releasesPerMonth += .20f; label.artistLoyalty -= .10f; break;
			case LabelArchetype.BluesRoots: label.artistLoyalty += .08f; label.productionQuality += .05f; label.marketingPower -= .08f; label.riskTolerance -= .08f; break;
			case LabelArchetype.CountrySpecialist: label.artistLoyalty += .10f; label.ownedReach += .08f; label.riskTolerance -= .09f; label.nationalReach -= .06f; break;
			case LabelArchetype.GospelPowerhouse: label.artistLoyalty += .12f; label.productionQuality += .07f; label.marketingPower -= .06f; label.riskTolerance -= .10f; break;
			case LabelArchetype.RegionalHustler: label.budgetLevel -= .08f; label.ownedReach -= .07f; label.nationalReach -= .06f; label.scoutingAbility += .08f; label.riskTolerance += .08f; break;
		}
		label.budgetLevel = Clamp(label.budgetLevel, envelope.BudgetMin, envelope.BudgetMax);
		label.marketingPower = Clamp(label.marketingPower, envelope.MarketingMin, envelope.MarketingMax);
		label.ownedReach = Clamp(label.ownedReach, envelope.ReachMin, envelope.ReachMax);
		label.nationalReach = Clamp(label.nationalReach, envelope.NationalMin, envelope.NationalMax);
		label.scoutingAbility = Clamp(label.scoutingAbility, envelope.ScoutingMin, envelope.ScoutingMax);
		label.productionQuality = Clamp(label.productionQuality, envelope.ProductionMin, envelope.ProductionMax);
		label.releasesPerMonth = Clamp(label.releasesPerMonth, envelope.CadenceMin, envelope.CadenceMax);
		label.riskTolerance = Clamp(label.riskTolerance, .05f, .95f);
		label.artistLoyalty = Clamp(label.artistLoyalty, .05f, .95f);
		label.payolaWillingness = Clamp(label.payolaWillingness, .01f, .95f);

		// Section 27: a minority of Independent founders are dependent "Stax" hitmakers. Give them
		// strong creative capability (so they genuinely chart) but low owned/national reach (so they
		// must lean on a distributor for national access) and a fragile balance sheet at founding
		// (so they sign out of necessity). GrowSelfBuiltDistributionReach then leaves them alone, so
		// they stay high-dependency and are absorbed late-decade. All values stay within the
		// canonical envelope; the roll uses the label's own stable PRNG so other labels are
		// unaffected.
		if (label.tier == LabelTier.Independent && random.Next01() < DependentHitmakerShare) {
			label.distributionDependentHitmaker = true;
			label.productionQuality = Clamp(Sample(.74f, .91f, ref random), envelope.ProductionMin, envelope.ProductionMax);
			label.scoutingAbility = Clamp(Sample(.70f, .91f, ref random), envelope.ScoutingMin, envelope.ScoutingMax);
			label.marketingPower = Clamp(Sample(.50f, .72f, ref random), envelope.MarketingMin, envelope.MarketingMax);
			label.ownedReach = Clamp(Sample(.28f, .36f, ref random), envelope.ReachMin, envelope.ReachMax);
			label.nationalReach = Clamp(Sample(.18f, .28f, ref random), envelope.NationalMin, envelope.NationalMax);
			label.cashReserves = Mathf.Min(label.cashReserves, label.GetMonthlyOverhead() * 2f);
		}
	}

	private static void ReconcileFoundingAndGeography(AILabel label, MarketRegion[] regions, int birthYear) {
		label.foundedYear = birthYear; label.monthsActive = 0; label.totalReleases = 0; label.top40Hits = 0; label.numberOneHits = 0;
		label.momentumScore = 0f; label.consecutiveLossMonths = 0; label.sustainedCapabilityQuarters = 0; label.sustainedLowCapabilityQuarters = 0;
		// MarketRegion.majorCities is not the canonical city catalog and did not contain
		// the generated headquarters names in live runs. Falling back to regions[0] therefore
		// assigned every runtime-founded label to the East Coast -- 674/674 in the measured
		// decade, including labels headquartered in San Francisco and Dallas. Resolve through
		// the same canonical city substrate that assigns homeCityId.
		MarketCity homeCity = DistanceModel.GetCityByName(label.headquartersCity);
		string mappedRegionId = homeCity?.parentRegionId;
		MarketRegion home = regions?.FirstOrDefault(region => region.regionId == mappedRegionId) ??
			regions?.FirstOrDefault(region => region.majorCities?.Contains(label.headquartersCity) ?? false);
		label.homeRegion = home?.regionId ?? mappedRegionId ?? "eastcoast";
		if (string.IsNullOrWhiteSpace(label.headquartersCity)) label.headquartersCity = DistanceModel.GetHubCityForRegion(label.homeRegion)?.name ?? "New York";
		label.strongRegions = new[] { label.homeRegion };
		// A functioning home-market wholesale path is part of being a label, including at
		// runtime. Preserve any wider network drawn by LabelGenerator and make the home market
		// explicit so replenishment does not treat the founder's own strong region as uncovered.
		label.distributionRegions = new[] { label.homeRegion }
			.Concat(label.distributionRegions ?? Array.Empty<string>())
			.Where(regionId => !string.IsNullOrEmpty(regionId))
			.Distinct(StringComparer.Ordinal)
			.ToArray();
		DistanceModel.AssignHomeCity(label);
	}

	private static float Sample(float minimum, float maximum, ref StableRandom random) => minimum + ((maximum - minimum) * random.Next01());
	private static float Clamp(float value, float minimum, float maximum) => Mathf.Clamp(value, minimum, maximum);
	private static ulong Mix(ulong seed, string labelId, int birthWeek, string domain) {
		ulong value = seed ^ 0x9E3779B97F4A7C15UL; value = MixStep(value ^ (uint)birthWeek);
		foreach (char c in (labelId ?? string.Empty) + "|" + domain) value = MixStep(value ^ c);
		return value;
	}
	private static ulong MixStep(ulong value) { value += 0x9E3779B97F4A7C15UL; value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL; value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL; return value ^ (value >> 31); }
	private struct StableRandom { private ulong state; public StableRandom(ulong state) => this.state = state; public float Next01() { state = MixStep(state); return (state >> 40) * (1f / 16777216f); } }
}
