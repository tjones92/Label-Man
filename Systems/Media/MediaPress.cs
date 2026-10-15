using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Who at an outlet does what (§9.1). Authority follows the role: a photographer can't commission a feature.</summary>
public enum PressRole { Editor, Reporter, Reviewer, Columnist, Photographer }

/// <summary>The story an item tells. Structured first; the words are composed from it afterwards (§9.4).</summary>
public enum StoryAngle {
	ChartNews, Breakout, TvAdvance, TvNotice, Hometown, CommunityNews, TeenFeature, CulturalPhenomenon, Review,
	StandardsDispute, Controversy, LabelPitch, OrdersFollowUp, Correction, FanScene,
}

public enum StoryTone { Neutral, Admiring, Curious, Skeptical, Alarmed }

public enum StoryStatus { Lead, Scheduled, Killed }

/// <summary>
/// Where a fact came from (§9.3 evidence record). Every source here is public or was knowingly supplied: a chart, a
/// shop's sales, a broadcast, a witnessed public incident, the label's own statement, another publication, the record
/// itself. There is deliberately no source for a musician's private state: a reporter cannot learn what nobody has
/// seen or said (§7.1, fixture 8).
/// </summary>
public enum EvidenceSource { NationalChart, RegionalSales, BroadcastAiring, PublicIncident, LabelStatement, OtherPublication, RecordItself }

public sealed class MediaEvidence {
	public EvidenceSource Source;
	public string Fact;
	public bool Firsthand;
	public float Confidence = 1f;
	public int Day;
}

/// <summary>
/// One press item: a lead, an item scheduled into an issue, or a killed lead (§9.3-§9.4). Items about the same event share
/// a root: ten papers repeating one quote are ten exposures to one story, never ten independent events.
/// </summary>
public sealed class MediaStory {
	public string Id, RootId, OutletId;
	public StoryAngle Angle;
	public StoryTone Tone;
	public StoryStatus Status;
	public string ArtistId, ArtistName, RecordId, Title, LabelId, CityId, ProgramId, AppearanceId;
	public Genre Genre;
	public int LeadDay, IssueDay;
	public float Interest;
	public int ChartPosition, Regions;
	public long ClaimedUnits;
	/// <summary>A claim in the item is wrong: the label's inflated figure, or a desk's honest mistake.</summary>
	public bool Inaccurate;
	public string InaccurateClaim;
	public bool Corrected;
	public bool PlayerSubject;
	/// <summary>The label declined to comment: the item runs shorter, not suppressed (§9.3).</summary>
	public bool Declined;
	/// <summary>Phase 3: the fan episode an item reports, the member the fans write about, and how the photographs were got.</summary>
	public FanEpisodeState FanState;
	public string Face, PhotoNote;
	public List<MediaEvidence> Evidence = new();
	public bool PublishedBy(int day) => Status == StoryStatus.Scheduled && IssueDay <= day;
}

/// <summary>Program memory of an act (§2 network/program reliability): kept appointments, honored agreements, breaches.</summary>
public sealed class MediaProgramMemory {
	public string ProgramId, ArtistId;
	public int Delivered, Withdrawn, Breaches;
	/// <summary>Day index before which the program won't book the act again (a breached agreement).</summary>
	public int ExcludedUntilDay = -1;
	public string LastNote;
}

/// <summary>The player's concrete history with one media contact (§14: "Used your regional figures last month").</summary>
public sealed class MediaContactMemory {
	public string Key, Name, Role, OutletName;
	/// <summary>Slow-moving working trust, [-1,1]. It cannot buy a favorable review (§2).</summary>
	public float Trust;
	public List<string> Lines = new();
	public void Remember(GameDate date, string line) {
		Lines.Insert(0, date.ToShortString() + "  " + line);
		if (Lines.Count > 8) Lines.RemoveAt(Lines.Count - 1);
	}
}

/// <summary>One attributed public controversy episode (§2): severity, reach, verification, expiry. Never a permanent stat.</summary>
public sealed class MediaControversy {
	public string Id, ArtistId, ArtistName, RootId, Summary;
	public int StartDay, ExpiresDay;
	public float Severity;
	public bool National, Verified;
}

/// <summary>
/// An announced headline date on a network show (§4.3 "event appearances", §4.5 advertised guests): placed weeks ahead,
/// so the press has time to write about it before air. Filled when the episode is booked.
/// </summary>
public sealed class MediaHold {
	public string ProgramId, ArtistId, ArtistName, RecordId, LabelId;
	public int Day, PlacedDay;
	public float Precursor;
}

/// <summary>
/// The press half of Phase 2 (directive §8-§9), observe-only. Once a week, after the television step, desks turn public
/// evidence into leads, editors spend a finite page budget on the ones that interest their readers, and the items run in
/// dated issues. Nothing here reads a musician's private state or writes an economic value; every draw is keyed.
/// </summary>
public static class MediaPress {
	public const int LeadShelfDays = 21;
	private const int MaxLeadsPerWeek = 400;

	/// <summary>The desk that handles an angle at an outlet.</summary>
	public static PressRole RoleFor(PressOutlet outlet, StoryAngle angle) => outlet.Family switch {
		PressFamily.Trade => angle == StoryAngle.Review || angle == StoryAngle.LabelPitch ? PressRole.Reviewer : PressRole.Reporter,
		PressFamily.TeenFan => PressRole.Editor,
		PressFamily.Mainstream => PressRole.Reporter,
		PressFamily.RockCriticism => PressRole.Reviewer,
		_ => PressRole.Columnist,
	};

	public static string ContactKey(PressOutlet outlet, PressRole role) => "press:" + outlet.Id + ":" + role;
	public static string RoleTitle(PressOutlet outlet, PressRole role) => role switch {
		PressRole.Reviewer => outlet.Family == PressFamily.Trade ? "Singles review editor" : "Critic",
		PressRole.Reporter => outlet.Family == PressFamily.Trade ? "News reporter" : "Staff writer",
		PressRole.Editor => outlet.Family == PressFamily.TeenFan ? "Editor" : "Assignment editor",
		PressRole.Photographer => "Photographer",
		_ => outlet.Family == PressFamily.LocalDaily ? "Entertainment columnist" : "Music columnist",
	};
	public static string ContactName(PressOutlet outlet, PressRole role) {
		string key = ContactKey(outlet, role);
		return RolodexDirectory.PersonName(key, Portraits.Hash(key + "|female") % 3 == 0);
	}

	// ---- issues ----------------------------------------------------------------------------------------------

	/// <summary>The first issue date on or after a day.</summary>
	public static int NextIssueDay(PressOutlet outlet, int day) {
		GameDate date = MediaAtmosphereService.FromDayIndex(day);
		switch (outlet.Cadence) {
			case PressCadence.Daily: return day;
			case PressCadence.Monthly:
				GameDate first = date.day == 1 ? date : new GameDate(date.month == 12 ? date.year + 1 : date.year, date.month == 12 ? 1 : date.month + 1, 1);
				return BroadcastExposure.DayIndex(first);
			default:
				return day + (((int)outlet.IssueDay - (int)date.DayOfWeek + 7) % 7);
		}
	}

	private static int FollowingIssueDay(PressOutlet outlet, int issueDay) => outlet.Cadence switch {
		PressCadence.Daily => issueDay + 1,
		PressCadence.Monthly => NextIssueDay(outlet, issueDay + 1),
		_ => issueDay + 7,
	};

	/// <summary>Budget bucket for an issue. A daily's music budget is weekly, so its bucket is the week.</summary>
	private static int Bucket(PressOutlet outlet, int issueDay) => outlet.Cadence == PressCadence.Daily ? issueDay / 7 : issueDay;

	// ---- the weekly pass ----------------------------------------------------------------------------------------

	internal sealed class Lead {
		public MediaStory Story;
		public List<string> Outlets = new();
	}

	/// <summary>
	/// Editors choose among this week's leads (§9.4). Each outlet takes the leads that interest its readers, at most one
	/// item per story root, into the first issue its print lead time allows that still has room. Weak or displaced leads
	/// are killed. Deterministic: ordered by interest, then id; the spread is keyed per story and outlet.
	/// </summary>
	internal static void Decide(List<Lead> leads, MediaSaveData state, int today) {
		var used = new Dictionary<(string, int), int>();
		foreach (MediaStory s in state.Stories)
			if (s.Status == StoryStatus.Scheduled && s.IssueDay >= today) {
				PressOutlet o = MediaOutletCatalog.GetPress(s.OutletId);
				if (o != null) used[(o.Id, Bucket(o, s.IssueDay))] = used.GetValueOrDefault((o.Id, Bucket(o, s.IssueDay))) + 1;
			}
		var covered = new HashSet<(string Outlet, string Root)>(state.Stories.Where(s => s.Status == StoryStatus.Scheduled).Select(s => (s.OutletId, s.RootId)));
		var recentByOutletArtist = state.Stories.Where(s => s.Status == StoryStatus.Scheduled && today - s.IssueDay < 42 && s.ArtistId != null)
			.GroupBy(s => (s.OutletId, s.ArtistId)).ToDictionary(g => g.Key, g => g.Count());

		var offers = new List<(MediaStory Template, PressOutlet Outlet, float Score)>();
		// The desks read the label's own sheets and the strongest leads first; a crowded week drops the weakest, never a sent sheet.
		foreach (Lead lead in leads.OrderByDescending(l => l.Story.PlayerSubject).ThenByDescending(l => l.Story.Interest)
			.ThenBy(l => l.Story.Id, StringComparer.Ordinal).Take(MaxLeadsPerWeek)) {
			foreach (string outletId in lead.Outlets.Distinct()) {
				PressOutlet outlet = MediaOutletCatalog.GetPress(outletId);
				if (outlet == null || !outlet.Publishing(MediaAtmosphereService.FromDayIndex(today))) continue;
				if (covered.Contains((outlet.Id, lead.Story.RootId))) continue;
				float interest = lead.Story.Interest * outlet.Fit(lead.Story.Genre);
				// An editor who ran the same act recently wants a new angle (§10.3: editors seek a new angle).
				int recent = lead.Story.ArtistId == null ? 0 : recentByOutletArtist.GetValueOrDefault((outlet.Id, lead.Story.ArtistId));
				interest /= 1f + 0.6f * recent;
				float score = interest * (0.6f + 0.8f * MediaAtmosphere.Unit("editor|" + outlet.Id + "|" + lead.Story.Id));
				if (score >= Bar(outlet, lead.Story.Angle)) offers.Add((lead.Story, outlet, score));
			}
		}
		foreach (var (template, outlet, score) in offers.OrderByDescending(o => o.Score).ThenBy(o => o.Template.Id, StringComparer.Ordinal).ThenBy(o => o.Outlet.Id, StringComparer.Ordinal)) {
			if (!covered.Add((outlet.Id, template.RootId))) continue;
			int issue = NextIssueDay(outlet, today + outlet.LeadDays);
			bool placed = false;
			for (int tries = 0; tries < 2 && !placed; tries++) {
				var bucket = (outlet.Id, Bucket(outlet, issue));
				if (used.GetValueOrDefault(bucket) < outlet.Slots) { used[bucket] = used.GetValueOrDefault(bucket) + 1; placed = true; break; }
				issue = FollowingIssueDay(outlet, issue);
			}
			if (!placed) continue;
			var item = Clone(template, outlet, issue, score);
			state.Stories.Add(item);
		}
	}

	/// <summary>How interesting a lead must be for an outlet to spend space on it. Mainstream editors need a reason to
	/// cover popular music at all (§8.3); a city daily will run a hometown item gladly.</summary>
	private static float Bar(PressOutlet outlet, StoryAngle angle) => outlet.Family switch {
		PressFamily.Mainstream => 0.85f,
		PressFamily.TeenFan => 0.45f,
		PressFamily.RockCriticism => 0.40f,
		PressFamily.Trade => angle == StoryAngle.LabelPitch ? 0.30f : 0.25f,
		PressFamily.Specialist => 0.30f,
		_ => 0.25f,
	};

	private static MediaStory Clone(MediaStory t, PressOutlet outlet, int issueDay, float score) {
		var s = new MediaStory {
			Id = t.Id + "@" + outlet.Id, RootId = t.RootId, OutletId = outlet.Id, Angle = t.Angle, Tone = t.Tone, Status = StoryStatus.Scheduled,
			ArtistId = t.ArtistId, ArtistName = t.ArtistName, RecordId = t.RecordId, Title = t.Title, LabelId = t.LabelId, CityId = t.CityId,
			ProgramId = t.ProgramId, AppearanceId = t.AppearanceId, Genre = t.Genre, LeadDay = t.LeadDay, IssueDay = issueDay, Interest = score,
			ChartPosition = t.ChartPosition, Regions = t.Regions, ClaimedUnits = t.ClaimedUnits, Inaccurate = t.Inaccurate, InaccurateClaim = t.InaccurateClaim,
			PlayerSubject = t.PlayerSubject, Declined = t.Declined, Evidence = t.Evidence.ToList(), FanState = t.FanState, Face = t.Face, PhotoNote = t.PhotoNote,
		};
		// Critics can be wrong, partisan or surprised (§8.4): the tone is theirs, keyed per outlet, not the record's verdict.
		if (t.Angle == StoryAngle.Review) s.Tone = CriticTone(t, outlet);
		// A desk's honest mistake: rare, small, and correctable (§9.4).
		if (!s.Inaccurate && s.PlayerSubject && MediaAtmosphere.Unit("press-error|" + s.Id) < 0.06f) {
			s.Inaccurate = true;
			s.InaccurateClaim = "the label's home town printed wrong";
		}
		return s;
	}

	private static StoryTone CriticTone(MediaStory t, PressOutlet outlet) {
		float craft = t.Evidence.Where(e => e.Source == EvidenceSource.RecordItself).Select(e => e.Confidence).DefaultIfEmpty(0.5f).First();
		float taste = MediaAtmosphere.Unit("critic|" + outlet.Id + "|" + t.RecordId);
		float verdict = 0.65f * craft + 0.35f * taste;
		return verdict > 0.62f ? StoryTone.Admiring : verdict < 0.38f ? StoryTone.Skeptical : StoryTone.Curious;
	}

	// ---- reading -------------------------------------------------------------------------------------------------

	/// <summary>Distinct press families that ran an item about an act between two days. One root counts once per family.</summary>
	public static int FamiliesCovering(MediaSaveData state, string artistId, int fromDay, int toDay) =>
		state.Stories.Where(s => s.ArtistId == artistId && s.Status == StoryStatus.Scheduled && s.IssueDay >= fromDay && s.IssueDay <= toDay)
			.Select(s => MediaOutletCatalog.GetPress(s.OutletId)?.Family).Where(f => f != null).Distinct().Count();

	/// <summary>The headline, composed from the structured item after the fact (§9.4). Text never decides anything.</summary>
	public static string Headline(MediaStory s) {
		string who = s.ArtistName ?? "a new label";
		string title = s.Title == null ? "" : $"\"{s.Title}\"";
		string program = s.ProgramId == null ? null : MediaOutletCatalog.DisplayName(MediaOutletCatalog.Get(s.ProgramId), MediaAtmosphereService.FromDayIndex(s.IssueDay));
		string city = s.CityId == null ? null : DistanceModel.GetCityById(s.CityId)?.name;
		return s.Angle switch {
			StoryAngle.ChartNews => s.ChartPosition == 1 ? $"{title} by {who} goes to No. 1" : $"{who}'s {title} moves to No. {s.ChartPosition}",
			StoryAngle.Breakout => s.Regions > 1 ? $"{title} breaking in {s.Regions} markets" : $"{title} breaking out regionally",
			StoryAngle.TvAdvance => $"{who} booked for {program}",
			StoryAngle.TvNotice => $"{who} on {program}",
			StoryAngle.Hometown => $"{city ?? "Local"} act {who} has a record moving",
			StoryAngle.CommunityNews => $"{who}: {title}",
			StoryAngle.TeenFeature => $"Meet {who}!",
			StoryAngle.CulturalPhenomenon => $"What is it about {who}?",
			StoryAngle.FanScene => s.FanState switch {
				FanEpisodeState.LocalExcitement => $"{city ?? "Local"} teenagers turn out for {who}",
				FanEpisodeState.RegionalFavorite => s.Regions > 1 ? $"{who} the talk of {s.Regions} markets" : $"{who} the talk of the town",
				FanEpisodeState.NationalSensation => $"{who}: the fans can't get enough",
				FanEpisodeState.Cooldown => $"Whatever happened to {who}?",
				_ => $"The fans and {who}",
			},
			StoryAngle.Review => s.Tone switch { StoryTone.Admiring => $"{title}: {who} get it right", StoryTone.Skeptical => $"{title}: not much there", _ => $"On {who}'s {title}" },
			StoryAngle.StandardsDispute => $"{who} and the censors",
			StoryAngle.Controversy => $"{who} defy the network",
			StoryAngle.LabelPitch => $"{who}: {title}",
			StoryAngle.OrdersFollowUp => $"Orders on {title} smaller than claimed",
			StoryAngle.Correction => $"Correction: {title}",
			_ => title,
		};
	}

	/// <summary>A one-sentence body: what the item reports and on what evidence. "Declined to comment" when that's what happened.</summary>
	public static string Body(MediaStory s) {
		string basis = s.Evidence.Count == 0 ? "" : Sentence(s.Evidence[0].Fact);
		if (s.ClaimedUnits > 0 && s.Angle != StoryAngle.OrdersFollowUp) basis += $" The label puts sales at {s.ClaimedUnits:N0}.";
		if (s.Declined) basis += " The label declined to comment.";
		if (s.Face != null && s.Angle is StoryAngle.TeenFeature or StoryAngle.FanScene) basis += $" The letters are mostly about {s.Face}.";
		if (s.PhotoNote != null) basis += " " + Sentence(s.PhotoNote);
		return basis.Trim();
	}

	/// <summary>End a line with one stop: a headline that's a question keeps its question mark.</summary>
	public static string Sentence(string text) {
		if (string.IsNullOrEmpty(text)) return text;
		text = text.Trim();
		return text.EndsWith(".") || text.EndsWith("?") || text.EndsWith("!") ? text : text + ".";
	}
}
