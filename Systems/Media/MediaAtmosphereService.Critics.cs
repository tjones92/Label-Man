using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Celebrity fanbase Phase 2b (SimTools/CelebrityFanbaseDesign.md §4.3, <see cref="MediaCritics"/>): the critics' review
/// column and the ledger acclaim reads. Each week, after the press pass, this week's reviews of chart entries are logged,
/// and each reviewing outlet picks LPs released this week for its column. At a completed run the record's published
/// reviews are consumed once, by <see cref="CriticAcclaimDelta"/>. The column lives in its own ledger, not in the news
/// items, so no other media reader sees it.
/// </summary>
public static partial class MediaAtmosphereService {
	/// <summary>Year ledger for MEDIA_CRITICS (print-only, cleared each year end).</summary>
	private static readonly Dictionary<string, double> criticsLedger = new(StringComparer.Ordinal);

	private static void CriticsLedger(string key, double amount) => criticsLedger[key] = criticsLedger.GetValueOrDefault(key) + amount;

	/// <summary>Jazz, folk and critic-fit records: the music the reviewing press was about. Everything else is "pop" here.</summary>
	private static bool CriticsMusic(Genre g) =>
		MediaOutletCatalog.CriticFit(g) >= MediaCritics.ColumnFit || MediaOutletCatalog.JazzPressFit(g) >= MediaCritics.ColumnFit
		|| MediaOutletCatalog.FolkPressFit(g) >= MediaCritics.ColumnFit;

	private static bool AddReview(string recordId, PressOutlet outlet, int issueDay, Genre genre, int year, bool column) {
		if (recordId == null || outlet == null) return false;
		if (!state.Reviews.TryGetValue(recordId, out CriticRecordReviews entry)) state.Reviews[recordId] = entry = new CriticRecordReviews();
		if (entry.Reviews.Any(r => r.OutletId == outlet.Id)) return false;
		float authority = MediaCritics.Authority(outlet, genre, year);
		entry.Reviews.Add(new CriticReview { OutletId = outlet.Id, IssueDay = issueDay, Authority = authority, Taste = MediaCritics.Taste(outlet.Id, recordId), Column = column });
		entry.LastDay = Math.Max(entry.LastDay, issueDay);
		CriticsLedger(column ? "reviewsColumn" : "reviewsChart", 1);
		CriticsLedger("reviews|" + outlet.Id, 1);
		CriticsLedger("authority", authority);
		return true;
	}

	/// <summary>
	/// Once a week, after the press pass. Chart-entry reviews scheduled this week join their record's ledger; then each
	/// publishing critic and specialist paper fills its next issue's column from the LPs released this week that are its
	/// readers' music. Editors pick before listening, so the pick reads fit, the act's public standing and a keyed spread,
	/// never the record's craft.
	/// </summary>
	private static void CriticsWeek(Evidence evidence, GameDate weekEnd) {
		if (!MediaCritics.Active) return;
		int today = BroadcastExposure.DayIndex(weekEnd) + 1;
		int year = weekEnd.year;
		foreach (MediaStory s in state.Stories)
			if (s.Angle == StoryAngle.Review && s.Status == StoryStatus.Scheduled && s.LeadDay == today && s.RecordId != null)
				AddReview(s.RecordId, MediaOutletCatalog.GetPress(s.OutletId), s.IssueDay, s.Genre, year, column: false);

		var released = new List<(RecordRuntimeData Record, float Standing)>();
		foreach (RecordRuntimeData r in evidence.All) {
			Record rec = r?.baseRecord;
			if (rec?.recordId == null || rec.artistId == null || rec.format != ReleaseFormat.Album || rec.album == null) continue;
			if (rec.album.albumFormat is AlbumFormat.Compilation or AlbumFormat.Soundtrack) continue;
			int day = BroadcastExposure.DayIndex(rec.releaseDate);
			if (day > today - 1 || day < today - 7) continue;
			if (!CriticsMusic(rec.primaryGenre) && MediaOutletCatalog.SoulPressFit(rec.primaryGenre) < MediaCritics.ColumnFit
				&& MediaOutletCatalog.CountryPressFit(rec.primaryGenre) < MediaCritics.ColumnFit) { CriticsLedger("lpsOther", 1); continue; }
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(rec.artistId);
			released.Add((r, artist == null ? 0f : Math.Max(artist.publicRecognition, artist.culturalStanding)));
		}
		CriticsLedger("lpsEligible", released.Count);
		var reviewed = new HashSet<string>(StringComparer.Ordinal);
		foreach (PressOutlet outlet in MediaOutletCatalog.Press) {
			if (outlet.Family is not (PressFamily.Specialist or PressFamily.RockCriticism) || !outlet.Publishing(weekEnd)) continue;
			int slots = MediaCritics.ColumnSlots(outlet);
			if (slots <= 0) continue;
			int issue = MediaPress.NextIssueDay(outlet, today + outlet.LeadDays);
			string key = outlet.Id + "|" + (outlet.Cadence == PressCadence.Daily ? issue / 7 * 7 : issue);
			int room = slots - state.ReviewColumn.GetValueOrDefault(key);
			if (room <= 0) continue;
			var picks = released.Where(x => outlet.Fit(x.Record.baseRecord.primaryGenre) >= MediaCritics.ColumnFit)
				.Select(x => (x.Record, Score: outlet.Fit(x.Record.baseRecord.primaryGenre) * (0.6f + 0.4f * x.Standing)
					* (0.6f + 0.8f * MediaAtmosphere.Unit("review-pick|" + outlet.Id + "|" + x.Record.baseRecord.recordId))))
				.OrderByDescending(x => x.Score).ThenBy(x => x.Record.baseRecord.recordId, StringComparer.Ordinal).Take(room).ToList();
			foreach (var (r, _) in picks)
				if (AddReview(r.baseRecord.recordId, outlet, issue, r.baseRecord.primaryGenre, year, column: true)) {
					state.ReviewColumn[key] = state.ReviewColumn.GetValueOrDefault(key) + 1;
					reviewed.Add(r.baseRecord.recordId);
				}
		}
		CriticsLedger("lpsReviewed", reviewed.Count);
		foreach (string key in state.ReviewColumn.Keys.Where(k => int.Parse(k[(k.LastIndexOf('|') + 1)..]) < today - 90).ToList()) state.ReviewColumn.Remove(key);
		if (weekEnd.month == 12 && weekEnd.day >= 25)
			foreach (string id in state.Reviews.Where(p => p.Value.LastDay < today - MediaCritics.LedgerDays).Select(p => p.Key).ToList()) state.Reviews.Remove(id);
	}

	/// <summary>
	/// A record's run is complete: its published reviews are read once and its ledger dropped. Returns the acclaim delta to
	/// write in causal mode; in shadow mode the act's media-owned shadow acclaim takes it and this returns null, as it does
	/// with the switch off, so the caller applies today's delta. Telemetry either way.
	/// </summary>
	internal static float? CriticAcclaimDelta(SimulatedArtist artist, Record record, float craft, float commercial) {
		if (!MediaCritics.Active || artist == null || record?.recordId == null) return null;
		int today = BroadcastExposure.DayIndex(TimeManager.Instance?.CurrentDate ?? new GameDate(1960, 1, 1));
		List<CriticReview> reviews = null;
		if (state.Reviews.Remove(record.recordId, out CriticRecordReviews entry)) reviews = entry.Reviews.Where(r => r.IssueDay <= today).ToList();
		(float coverage, float verdict) = MediaCritics.Heard(craft, reviews);
		float delta = MediaCritics.Delta(craft, commercial, reviews);
		float old = ArtistCriticalAcclaimService.GetAcclaimDelta(craft, commercial);
		string group = (CriticsMusic(record.primaryGenre) ? "Art" : "Pop") + (record.format == ReleaseFormat.Album ? "Lp" : "Single");
		CriticsLedger("consumed" + group, 1);
		CriticsLedger("coverage" + group, coverage);
		if (reviews is { Count: > 0 }) {
			CriticsLedger("consumedReviewed" + group, 1);
			CriticsLedger("verdictShift", verdict - craft);
			CriticsLedger("verdictShiftAbs", Math.Abs(verdict - craft));
		}
		CriticsLedger((old > 0f ? "climbOld" : "erodeOld") + group, old);
		CriticsLedger((delta > 0f ? "climbNew" : "erodeNew") + group, delta);
		if (MediaCritics.Causal) return InCausalWindow(today) ? delta : null;
		float prior = state.ShadowAcclaim.TryGetValue(artist.artistId, out float s) ? s : artist.criticalAcclaim;
		state.ShadowAcclaim[artist.artistId] = Math.Clamp(prior * ArtistCriticalAcclaimService.RetentionPerRelease + delta, 0f, 1f);
		return null;
	}

	/// <summary>The craft a reviewer hears in a record: merit craft when Phase 2b is on, the old hook-and-production reading otherwise.</summary>
	internal static float ReviewCraft(RecordRuntimeData r) {
		Record rec = r?.baseRecord;
		if (rec == null) return 0.5f;
		if (!MediaCritics.Active) return (rec.hookStrength + rec.productionQuality) * 0.5f;
		return ArtistCriticalAcclaimService.RecordCraft(rec, ChartManager.Instance?.GetLabelById(rec.labelId));
	}

	private static void PrintCritics(int year) {
		if (!MediaCritics.Active) { criticsLedger.Clear(); return; }
		double V(string k) => criticsLedger.GetValueOrDefault(k);
		string Group(string g) {
			double n = V("consumed" + g), reviewed = V("consumedReviewed" + g);
			return $"{g}[n/reviewed/coverage/climbOld/climbNew/erodeOld/erodeNew]={n:0}/{reviewed:0}/{(n > 0 ? V("coverage" + g) / n : 0):F2}" +
				$"/{V("climbOld" + g):F2}/{V("climbNew" + g):F2}/{V("erodeOld" + g):F2}/{V("erodeNew" + g):F2}";
		}
		double reviewedAll = new[] { "ArtLp", "ArtSingle", "PopLp", "PopSingle" }.Sum(g => V("consumedReviewed" + g));
		var acts = ArtistManager.Instance?.GetAllArtists().Where(a => a != null && a.isActive && !a.isPlayerOwned).ToList() ?? new List<SimulatedArtist>();
		string Dist(Func<SimulatedArtist, float> read, Func<SimulatedArtist, bool> where) {
			var v = acts.Where(where).Select(read).OrderBy(x => x).ToList();
			return v.Count == 0 ? "0" : $"{v.Average():F3}/{v[Math.Min(v.Count - 1, (int)(0.9 * v.Count))]:F3}/{v.Count(x => x >= 0.55f)}";
		}
		Func<SimulatedArtist, float> shadow = a => state.ShadowAcclaim.TryGetValue(a.artistId, out float s) ? s : a.criticalAcclaim;
		var outlets = criticsLedger.Where(p => p.Key.StartsWith("reviews|", StringComparison.Ordinal)).OrderByDescending(p => p.Value)
			.ThenBy(p => p.Key, StringComparer.Ordinal).Take(8).Select(p => $"{p.Key[8..]}:{p.Value:0}");
		Godot.GD.Print($"MEDIA_CRITICS year={year} mode={(MediaCritics.Causal ? "causal" : "shadow")} reviews[column/chart]={V("reviewsColumn"):0}/{V("reviewsChart"):0}" +
			$" meanAuthority={(V("reviewsColumn") + V("reviewsChart") > 0 ? V("authority") / (V("reviewsColumn") + V("reviewsChart")) : 0):F3}" +
			$" lps[eligible/reviewed/other]={V("lpsEligible"):0}/{V("lpsReviewed"):0}/{V("lpsOther"):0}" +
			$" verdictShift[mean/abs]={(reviewedAll > 0 ? V("verdictShift") / reviewedAll : 0):+0.000;-0.000}/{(reviewedAll > 0 ? V("verdictShiftAbs") / reviewedAll : 0):F3}" +
			$" {Group("ArtLp")} {Group("ArtSingle")} {Group("PopLp")} {Group("PopSingle")}" +
			$" acclaim[mean/p90/darlings] art={Dist(a => a.criticalAcclaim, a => CriticsMusic(a.primaryGenre))} pop={Dist(a => a.criticalAcclaim, a => !CriticsMusic(a.primaryGenre))}" +
			(MediaCritics.Shadow ? $" shadow art={Dist(shadow, a => CriticsMusic(a.primaryGenre))} pop={Dist(shadow, a => !CriticsMusic(a.primaryGenre))}" : "") +
			$" ledger={state.Reviews.Count} top=[{string.Join(" ", outlets)}]");
		criticsLedger.Clear();
	}
}
