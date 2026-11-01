using System;
using System.Collections.Generic;

/// <summary>
/// Celebrity fanbase Phase 2b (SimTools/CelebrityFanbaseDesign.md §4.3): reviews become the channel a record's craft earns
/// critical acclaim through. Off unless named.
/// <list type="bullet">
/// <item>A critic's verdict reads the record's merit craft (the craft acclaim and the landmark rule read) plus that critic's
/// taste centred on zero, so critics add disagreement, not drift.</item>
/// <item>LPs in critic-fit and specialist genres become review leads at release, charted or not, in each outlet's own
/// review column (<see cref="ColumnSlots"/>). The column is kept apart from the news items, so nothing else in media reads it.</item>
/// <item>At a completed run the positive acclaim delta is scaled by coverage, 1 − ∏(1 − authority) over the trades' picks
/// (every release, <see cref="TradeFloor"/>) and the outlets that reviewed it, relative to that floor: an unreviewed record
/// keeps today's climb. The craft term reads the authority-weighted verdict. Erosion is not scaled.</item>
/// </list>
/// <c>--observe-critic-acclaim</c> keeps a media-owned shadow acclaim per act and leaves <see cref="SimulatedArtist.criticalAcclaim"/>
/// alone; <c>--critic-acclaim</c> writes the scaled delta to it and needs <see cref="MediaAtmosphere.CausalSwitch"/>. Standing
/// keeps its three writers: critics reach it only through acclaim → landmarks and influence (directive §8.4, §12.4).
/// Draws no RNG.
/// </summary>
public static class MediaCritics {
	public const string ShadowSwitch = "--observe-critic-acclaim";
	public const string CausalSwitch = "--critic-acclaim";

	/// <summary>Billboard and Cash Box picked nearly every release, briefly: the coverage every record has (author, 2026-11-01).</summary>
	public const float TradeFloor = 0.5f;
	/// <summary>How far one critic's taste moves the verdict, as the old tone formula's 0.35 weight, centred.</summary>
	public const float TasteSpread = 0.35f;
	/// <summary>The review column reads an outlet's own audience: an LP is a lead where the paper's fit reaches this.</summary>
	public const float ColumnFit = 0.7f;
	/// <summary>Review ledgers are kept this long after a record's last review, then dropped unread.</summary>
	public const int LedgerDays = 7 * 260;

	private static bool configured;
	private static bool shadow;
	private static bool causal;
	/// <summary>Reviews are logged and the would-be acclaim is media telemetry.</summary>
	public static bool Shadow => shadow && !causal && MediaAtmosphere.Observing;
	/// <summary>The scaled delta is written to the act's acclaim.</summary>
	public static bool Causal => causal && MediaAtmosphere.Causal;
	/// <summary>Either mode: the review column runs, verdicts read merit craft, and MEDIA_CRITICS is printed.</summary>
	public static bool Active => Shadow || Causal;

	public static void Configure(IEnumerable<string> arguments) {
		if (configured) return;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == ShadowSwitch) shadow = true;
			if (argument == CausalSwitch) causal = true;
		}
		if (causal && shadow)
			throw new ArgumentException($"{CausalSwitch} and {ShadowSwitch} cannot be used together; the causal run prints the same telemetry.");
		if (causal && !MediaAtmosphere.Causal)
			throw new ArgumentException($"{CausalSwitch} writes critical acclaim; it requires {MediaAtmosphere.CausalSwitch}.");
		configured = true;
	}

	internal static void SetForChecks(bool on, bool writeCausal = false) { shadow = on && !writeCausal; causal = on && writeCausal; configured = true; }

	/// <summary>
	/// An outlet's authority with its own audience before fit (design §4.3, initial values). The Voice and Rolling Stone
	/// grow through the decade; the underground papers and the other community papers are minor voices.
	/// </summary>
	public static float BaseAuthority(PressOutlet outlet, int year) {
		if (outlet == null) return 0f;
		static float Ramp(float from, float to, int year, int y0, int y1) => from + (to - from) * Math.Clamp((year - y0) / (float)(y1 - y0), 0f, 1f);
		return outlet.Id switch {
			"down_beat" => 0.6f,
			"sing_out" => 0.5f,
			"broadside" => 0.2f,
			"jet" or "ebony" or "chicago_defender" => 0.3f,
			"village_voice" => Ramp(0.15f, 0.35f, year, 1960, 1967),
			"crawdaddy" => 0.3f,
			"rolling_stone" => Ramp(0.3f, 0.6f, year, 1967, 1969),
			_ => outlet.Family is PressFamily.Specialist or PressFamily.RockCriticism ? 0.1f : 0f,
		};
	}

	/// <summary>Authority for this record: the outlet's standing times how far the record is its readers' kind of music.</summary>
	public static float Authority(PressOutlet outlet, Genre genre, int year) =>
		Math.Clamp(BaseAuthority(outlet, year) * (outlet?.Fit?.Invoke(genre) ?? 1f), 0f, 1f);

	/// <summary>LP reviews an outlet prints per issue (a daily's per week). Down Beat and Crawdaddy! were mostly reviews.</summary>
	public static int ColumnSlots(PressOutlet outlet) => outlet?.Id switch {
		"down_beat" or "sing_out" or "crawdaddy" or "rolling_stone" => 6,
		"ebony" or "country_song_roundup" or "music_city_news" => 3,
		"jet" or "chicago_defender" or "village_voice" or "broadside" => 2,
		null => 0,
		_ => outlet.Family is PressFamily.Specialist or PressFamily.RockCriticism ? 1 : 0,
	};

	/// <summary>One critic's departure from the record's craft, keyed as the printed tone is, so the two agree.</summary>
	public static float Taste(string outletId, string recordId) =>
		TasteSpread * (MediaAtmosphere.Unit("critic|" + outletId + "|" + recordId) - 0.5f);

	/// <summary>
	/// What the reviewers heard: coverage 1 − (1 − floor) ∏(1 − a), and the authority-weighted verdict, with the trades'
	/// picks reading craft itself.
	/// </summary>
	public static (float Coverage, float Verdict) Heard(float craft, IReadOnlyList<CriticReview> reviews) {
		double unheard = 1.0 - TradeFloor, weight = TradeFloor, taste = 0.0;
		if (reviews != null)
			foreach (CriticReview r in reviews) {
				unheard *= 1.0 - r.Authority;
				weight += r.Authority;
				taste += r.Authority * r.Taste;
			}
		return ((float)(1.0 - unheard), Math.Clamp(craft + (float)(taste / weight), 0f, 1f));
	}

	/// <summary>
	/// The per-release acclaim delta: the verdict in place of craft, the climb scaled by coverage relative to the trades'
	/// floor (the neutral floor, author 2026-11-01), erosion as before. An unreviewed record keeps today's climb; reviews
	/// add to it, up to 2× and never past the service's per-release ceiling. The as-specified scaling (coverage itself)
	/// halved every climb and cost the underrated landmarks (CelebrityFanbasePhase2bNotes.md).
	/// </summary>
	public static float Delta(float craft, float commercial, IReadOnlyList<CriticReview> reviews) {
		(float coverage, float verdict) = Heard(craft, reviews);
		float delta = ArtistCriticalAcclaimService.GetAcclaimDelta(verdict, commercial);
		return delta > 0f ? Math.Min(delta * coverage / TradeFloor, ArtistCriticalAcclaimService.MaxTotalGainPerRelease) : delta;
	}
}

/// <summary>One printed review of a record: the outlet, its issue day, its authority for this record and its critic's taste term.</summary>
public sealed class CriticReview {
	public string OutletId;
	public int IssueDay;
	public float Authority;
	public float Taste;
	/// <summary>From the LP column at release, not a review of a chart entry.</summary>
	public bool Column;
}

/// <summary>A record's reviews, held until its run completes (then consumed) or <see cref="MediaCritics.LedgerDays"/> after the last.</summary>
public sealed class CriticRecordReviews {
	public List<CriticReview> Reviews = new();
	public int LastDay;
}
