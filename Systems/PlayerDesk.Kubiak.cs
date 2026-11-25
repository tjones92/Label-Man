using Godot;

// ============================================================================================
// WALTER KUBIAK'S JUKEBOX RIGHTS (SimTools/LabelManInnerLifeDirective.md §3.5 H2).
//
// Clause 14 gave Walter the jukebox rights to everything you press in your home town, and his route doesn't reach far. So:
//   - The rights: while he holds the paper, the home town's operators can only put your records on his machines. An
//     operator account there reaches half its usual machines for you (its route crowd and its order), every year.
//   - The first move (§6.1) taken: his 200 copies go on those machines the week the run is delivered, and the bars hear the
//     record. That's a small appeal lift for that single at the home town's counters, fading over twelve weeks.
//   - The first move turned down: the home town's operators won't take your records at all that year (built in §6.1).
// The channel is the player's own: operator accounts and counter appeal are only ever read for the player's records, so
// the AI economy is untouched. Values are provisional.
// ============================================================================================
public partial class PlayerDesk {
	/// <summary>Share of a home-town operator's machines your records can reach while Kubiak holds the rights.</summary>
	public const float KubiakRouteReach = 0.5f;
	/// <summary>Appeal lift for the claimed single at the home town's counters the week it goes on his machines, and how long it fades.</summary>
	public const float KubiakRouteLift = 0.04f;
	public const int KubiakRouteWeeks = 12;

	private bool KubiakHoldsRights => lien?.Holder == LienHolder.Kubiak && !IsGameOver && Label != null;

	/// <summary>This operator works the home town, where your records only go on Kubiak's machines.</summary>
	public bool KubiakRouteBinds(PlayerStop stop) => KubiakHoldsRights && stop?.Kind == StopKind.Op && stop.CityId == Label.homeCityId;

	/// <summary>The share of an operator's machines your records can reach.</summary>
	private float RouteReach(PlayerStop stop) => KubiakRouteBinds(stop) ? KubiakRouteReach : 1f;

	/// <summary>The claimed single, on his machines: a lift at the home town's counters that fades as the records rotate out.</summary>
	private float KubiakRouteAppeal(Record record, string cityId) {
		if (!KubiakHoldsRights || record == null || cityId != Label.homeCityId || lien.Move != HolderMoveStage.Accepted
			|| record.recordId != lien.MoveRecordId || lien.KubiakRouteDate is not { } since) return 0f;
		int days = DaysBetween(since, LienToday);
		if (days < 0) return 0f;
		return KubiakRouteLift * Mathf.Max(0f, 1f - days / 7f / KubiakRouteWeeks);
	}

	/// <summary>The line under the home town's jukebox operators while he holds the rights.</summary>
	public string KubiakRouteLine(string cityId) {
		if (!KubiakHoldsRights || cityId != Label.homeCityId) return null;
		if (lien.KubiakRefusedYear == LienToday.year)
			return $"Walter Kubiak holds the jukebox rights in this town, and you turned him down. No operator here will take your records this year.";
		return "Walter Kubiak holds the jukebox rights in this town. The operators can only put your records on his machines, and his route doesn't reach far: about half what they'd take.";
	}

	// ── checks ─────────────────────────────────────────────────────────────────────────────────────

	internal float RouteReachForChecks(PlayerStop stop) => RouteReach(stop);
	internal float KubiakRouteAppealForChecks(Record record, string cityId) => KubiakRouteAppeal(record, cityId);
	internal float CounterCrowdCeilingForChecks(PlayerStop stop, float appeal) => CounterCrowdCeiling(stop, appeal);
	internal PlayerStop StopForChecks(StopKind kind, bool home) => System.Linq.Enumerable.FirstOrDefault(System.Linq.Enumerable.OrderBy(EnsureStops().Values, s => s.StopId, System.StringComparer.Ordinal),
		s => s.Kind == kind && (s.CityId == Label.homeCityId) == home);
}
