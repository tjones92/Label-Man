using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Phase 4e of SimTools/BandMemberSimulationDirective.md (§4.15): people get rich, unevenly.
/// <para>
/// <b>The stock.</b> <see cref="Musician.wealth"/> moves at the year boundary from figures the economy already
/// computes, and moves no economy figure. A member's year is their slice of the act's performer royalty (the year's
/// increase in <see cref="SimulatedArtist.totalRoyaltyEarnings"/>, split evenly across the lineup, the period
/// convention; a sideman is paid a wage and the leader keeps the rest), plus their own writer income (charted units
/// on songs they are credited on x their credit share on each x the writer's half of the 2-cent mechanical, derived
/// for the person's account only and never charged to a label), less spending. Spending is a share of the stock that
/// rises with ego and fame: a sink, not a cap, so the stock rises and falls and most people never hold much.
/// </para>
/// <para>
/// <b>The readers</b> (only with <c>--enable-member-wealth</c>; <c>--observe-member-wealth</c> keeps the stock alone,
/// unread): independence (the solo urge, the studio-only exit, and the success term of the solvent),
/// the money grievance (CreditAndMoney reads the wealth gap), lasting fame (a small non-decaying floor on member
/// fame), and the substance hazard (wealth replaces the fame proxy). Each reads <see cref="Norm"/>.
/// </para>
/// </summary>
public static class MemberWealthService {
	/// <summary>The writer's half of the 2-cent statutory mechanical, per unit.</summary>
	public const float WriterMechanicalPerUnit = 0.01f;
	/// <summary>A sideman's yearly wage at a full road year, paid from the act's royalty before the leader's share.</summary>
	public const float SidemanWage = 2500f;
	/// <summary>Yearly spending as a share of the stock: a floor, plus ego and fame (the lifestyle).</summary>
	public const float SpendBase = 0.18f, SpendEgo = 0.22f, SpendFame = 0.30f;
	/// <summary>The stock that reads as "comfortably rich" (Norm ~ .63). Sized on bms5-obs-1001 (§14): with spending,
	/// charting-group members held ~$600 median, $3.9k at the 90th percentile and $17k at the 99th, so the studio-only
	/// bar (Norm .45, ~$4.8k) reaches roughly the top 8% of them. At $40k almost nobody crossed any reader's bar.</summary>
	public const float WealthScale = 8000f;
	/// <summary>An act or a person met for the first time mid-career (an old save, a 1965 world) starts with this much
	/// of its lifetime income still in hand, so a resumed world doesn't hand everyone their whole career as one year.</summary>
	public const float SeedRetention = 0.35f;

	// ---- readers (sized offline on the bms5-obs member-year ledger; §13a) -------------------------------
	/// <summary>Added to the solo urge at Norm = 1: a rich member can afford to walk.</summary>
	public const float IndependenceUrge = 0.12f;
	/// <summary>How much of the act's success stops absorbing a rich member's strain (the solvent's success term).</summary>
	public const float IndependenceSolventDiscount = 0.35f;
	/// <summary>Norm at which an exhausted member can afford to stop touring (StudioOnly) rather than quit.</summary>
	public const float StudioOnlyAffordNorm = 0.45f;
	/// <summary>The money grievance: weight of the pair's wealth gap in the CreditAndMoney raw term.</summary>
	public const float WealthGapWeight = 0.25f;
	/// <summary>The lasting-fame floor at Norm = 1. Kept modest on purpose: wealth comes from the same records as fame.</summary>
	public const float LastingFameFloor = 0.02f;

	public static float Norm(Musician m) => m == null || m.wealth <= 0f ? 0f : 1f - Mathf.Exp(-m.wealth / WealthScale);

	public static bool Readers => BandLife.MemberWealthReaders;

	/// <summary>
	/// One act's year (called from the band-life pass for every act in scope, before the lineup can change).
	/// Pays the year's income to the members present and charges each member's spending.
	/// </summary>
	public static void OnActYear(SimulatedArtist a, List<Musician> present, LineupConstitution constitution, float roadLoad, int year) {
		if (!BandLife.MemberWealthActive || a == null || present == null) return;
		if (!a.wealthBaselined) BaselineAct(a, present);
		foreach (Musician m in present) if (!m.wealthBaselined) BaselinePerson(m);

		float royalty = Mathf.Max(0f, a.totalRoyaltyEarnings - a.royaltyAtYearStart);
		a.royaltyAtYearStart = a.totalRoyaltyEarnings;
		var pay = new Dictionary<Musician, float>();
		if (present.Count > 0 && royalty > 0f) {
			if (constitution == LineupConstitution.LeaderAndSidemen && present.Any(m => m.isBandLeader)) {
				Musician leader = present.First(m => m.isBandLeader);
				float left = royalty;
				foreach (Musician m in present.Where(m => m != leader)) {
					float wage = Mathf.Min(left, SidemanWage * Mathf.Max(0.2f, roadLoad));
					pay[m] = wage;
					left -= wage;
				}
				pay[leader] = left;
			} else {
				float each = royalty / present.Count;
				foreach (Musician m in present) pay[m] = each;
			}
		}
		float fame = Mathf.Clamp(a.publicRecognition * 2.5f, 0f, 1f);
		float liveHours = MemberGrowthService.LiveHours(a, roadLoad);
		int players = present.Count(x => x.lifeState == MemberLifeState.Active);
		foreach (Musician m in present) {
			float writer = WriterIncomeSinceSnapshot(m);
			float live = SceneLiveEconomics.MemberLiveIncome(a, m, liveHours, players, year);
			float income = pay.GetValueOrDefault(m) + writer + SceneLiveEconomics.LiveSavingsShare * live;
			float spend = Mathf.Clamp(SpendBase + SpendEgo * m.ego + SpendFame * Mathf.Max(fame, Mathf.Clamp(m.personalRecognition * 4f, 0f, 1f)), 0f, 0.85f);
			m.wealth = Mathf.Max(0f, m.wealth * (1f - spend) + income);
			m.lastYearIncome = income;
			m.lastYearWriterIncome = writer;
			m.lastYearLiveIncome = live;
		}
	}

	/// <summary>Writer income since the person's last snapshot, and a new snapshot: share-weighted charted units.</summary>
	private static float WriterIncomeSinceSnapshot(Musician m) {
		double units = CompositionCatalogService.GetPersonShareUnits(m.personId);
		double delta = Math.Max(0d, units - m.writerUnitsAtYearStart);
		m.writerUnitsAtYearStart = units;
		return (float)(delta * WriterMechanicalPerUnit);
	}

	private static void BaselineAct(SimulatedArtist a, List<Musician> present) {
		a.wealthBaselined = true;
		float lifetime = Mathf.Max(0f, a.totalRoyaltyEarnings);
		a.royaltyAtYearStart = a.totalRoyaltyEarnings;
		if (lifetime <= 0f || present.Count == 0) return;
		float each = lifetime * SeedRetention / present.Count;
		foreach (Musician m in present) {
			if (!m.wealthBaselined) BaselinePerson(m);
			m.wealth += each;
		}
	}

	private static void BaselinePerson(Musician m) {
		m.wealthBaselined = true;
		double units = CompositionCatalogService.GetPersonShareUnits(m.personId);
		m.writerUnitsAtYearStart = units;
		m.wealth += (float)(units * WriterMechanicalPerUnit * SeedRetention);
	}

	// ---- readers ---------------------------------------------------------------------------------------

	/// <summary>Reader 1: the solo urge a member's own money adds.</summary>
	public static float SoloIndependence(Musician m) => Readers ? IndependenceUrge * Norm(m) : 0f;

	/// <summary>Reader 1: the share of an act's success that still absorbs strain on a pair, given its richer member.</summary>
	public static float SuccessSolventFactor(Musician x, Musician y) =>
		Readers ? 1f - IndependenceSolventDiscount * Mathf.Max(Norm(x), Norm(y)) : 1f;

	/// <summary>Reader 1: an exhausted member who can afford it stops touring instead of quitting.</summary>
	public static bool CanAffordStudioOnly(Musician m) => Readers && Norm(m) >= StudioOnlyAffordNorm;

	/// <summary>Reader 2: "he's bought a house; I'm still in a flat." The gap is relative, felt by the poorer member
	/// in proportion to how much they want it.</summary>
	public static float WealthGapGrievance(Musician x, Musician y) {
		if (!Readers) return 0f;
		(Musician rich, Musician poor) = x.wealth >= y.wealth ? (x, y) : (y, x);
		float gap = (rich.wealth - poor.wealth) / (rich.wealth + poor.wealth + 0.25f * WealthScale);
		return WealthGapWeight * Mathf.Max(0f, gap) * Norm(rich) * poor.ambition * poor.ego;
	}

	/// <summary>Reader 3: the floor member fame doesn't decay below while the money lasts.</summary>
	public static float LastingFame(Musician m) => Readers ? LastingFameFloor * Norm(m) : 0f;

	/// <summary>Reader 4: the substance hazard's personal exposure. Wealth replaces the fame proxy.</summary>
	public static float SubstanceExposure(Musician m, float fameProxy) => Readers ? Norm(m) : fameProxy;
}
