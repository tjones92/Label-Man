using System;
using System.Collections.Generic;
using System.Linq;

// ============================================================================================
// LEADS (SimTools/SigningRealismBreakdown.md §3.6, step 7): a reason to go and hear somebody before you've heard them.
//
// Each lead is said by a source that could know it, from real state:
//   The Street passes on what the rooms are saying, once the Label Man has been in one (a set heard); before that the
//   only lead is a room billing its regulars, which is printed: a residency and how long the room has kept them, a full room with them
//   on the bill this week, the people who come in for them, an original the crowd asks for by name. That's noise, not
//   music. A Street of 3 passes it on plainly. Below that it hears only the noise and talks it up, so a loud, ordinary
//   band sounds like the best thing in town and a good quiet one isn't mentioned (the fog's direction). From 4 it can
//   tell a crowd from a band and says so, and at 5 it hears about a good band nobody's talking about yet.
//   A booker you've met recommends by draw, for an act that has played their room.
//   The Fixer passes on that an AI label's daily market looked at them, only when one did and they're still free.
//
// A pure read: building leads moves nothing. The rival looks are recorded from the AI market's own appointments
// (OnRivalTalentMarketAppointment) into the player's scene knowledge, so they're saved and go stale after four weeks.
// ============================================================================================
public sealed class SetLead {
	public ExecutiveVoice Voice;
	/// <summary>Who says it: "The Street", a booker's name, "The Fixer".</summary>
	public string Source;
	public string Text;
}

/// <summary>An AI label's daily market looked at an act that stayed free (Fixer leads).</summary>
public sealed class RivalLook {
	public string ArtistId { get; set; }
	public string LabelId { get; set; }
	public int Day { get; set; }
	/// <summary>A booker who rang to tip him off about it (PlayerDesk.Callers), or null. Once told, the lead shows whatever
	/// his Fixer.</summary>
	public string ToldBy { get; set; }
}

public partial class PlayerDesk {
	/// <summary>People who came in for them, as a share of the room, for the Street to call it a draw: about the top tenth
	/// of free acts with a night on record (seed 1001's opening week: median 0.10, p90 0.14, max 0.20).</summary>
	public const float LeadDrawShare = 0.14f;
	/// <summary>A bill's attendance over capacity the Street calls packed. Bills never fill (attendance is capped below the
	/// room's capacity by the expected audience): median 0.57, max 0.68 in the opening week. This is the top of that.</summary>
	public const float LeadBusyRoom = 0.65f;
	/// <summary>Nights in one room before the Street says the room keeps having them back.</summary>
	public const int LeadRegularShows = 13;
	/// <summary>The band's own quality (CalculateBaseQuality) a good Street calls "the real thing" (about the top tenth of
	/// free acts: p90 0.69), and below which it calls the noise the room's (about the bottom fifth).</summary>
	public const float LeadGoodBand = 0.69f, LeadOrdinaryBand = 0.45f;
	public const int RivalLookDays = 28;

	/// <summary>What the town is saying about an act, from the sources that could know: the Street's noise and its read
	/// of it, a booker you've met, the Fixer. <paramref name="setDay"/> is the set you'd go to. Pure.</summary>
	public IReadOnlyList<SetLead> LeadsFor(string artistId, string placeId, int setDay) {
		var leads = new List<SetLead>();
		var act = ArtistManager.Instance?.GetArtist(artistId);
		string scene = LocalScenePersistenceService.SceneIdFor(placeId);
		if (Label == null || act == null || scene == null || !LocalScenes.Rooms) return leads;
		int today = SceneDay, now = NowMinute;
		string them = act.stageName;
		var rooms = SceneRoomCatalog.ForPlace(scene).Where(r => r.IsPerformance).OrderBy(r => r.Id, StringComparer.Ordinal).ToArray();

		// ── The noise ──
		var facts = new List<string>();
		int heat = 0;
		var resident = SceneRoomCatalog.Get(LocalSceneRoomService.EngagementRoom(artistId, today));
		if (resident != null && resident.PlaceId == scene) {
			heat++;
			bool regular = LocalSceneRoomService.ShowsAt(resident.Id, artistId) >= LeadRegularShows;
			facts.Add($"They've got {resident.Name} steady{(regular ? ", and the room keeps having them back" : "")}.");
		}
		SceneBill full = null;
		foreach (var room in rooms) {
			var last = LocalSceneRoomService.LastCompletedBill(room.Id, artistId, today, now);
			if (last != null && today - last.Day <= 7 && last.Capacity > 0 && last.Attendance >= LeadBusyRoom * last.Capacity && (full == null || last.Day > full.Day)) full = last;
		}
		if (full != null) {
			heat++;
			string when = full.Day == today ? "tonight" : full.Day == today - 1 ? "last night" : LocalSceneRoomService.Date(full.Day).DayOfWeek.ToString();
			facts.Add($"{SceneRoomCatalog.Get(full.RoomId).Name} was packed {when} with them on the bill.");
		}
		var draw = rooms.Select(r => (room: r, d: LocalSceneRoomService.DrawAt(r.Id, artistId)))
			.Where(x => x.d is { Nights: >= 1 } && SceneLiveEconomics.Capacity(x.room) > 0)
			.Select(x => (x.room, arrivals: x.d.Value.AverageArrivals, share: x.d.Value.AverageArrivals / (float)SceneLiveEconomics.Capacity(x.room)))
			.OrderByDescending(x => x.share).ThenBy(x => x.room.Id, StringComparer.Ordinal).FirstOrDefault();
		bool draws = draw.room != null && draw.share >= LeadDrawShare;
		if (draws) {
			heat++;
			facts.Add($"People come in for them at {draw.room.Name}, about {draw.arrivals} a night.");
		}
		if (heat > 0 && OriginalAskedFor(artistId, rooms, today, now) is { } song)
			facts.Add($"Kids are asking for \"{song}\", one of theirs.");

		// ── The Street ──
		// It hears what the rooms say only once he's been in one. On the first morning he's just stumbled out of the office
		// with a hangover, so all he has is what's printed: the room's own billing of its regulars (playtest 2026-11-26).
		int street = InstinctScore(InstinctProfile, ExecutiveVoice.Street);
		float band = act.CalculateBaseQuality();
		string streetText = null;
		bool beenOut = scoutingKnowledge.Dossiers.Any(d => HasScoutingEvidence(d.ArtistId));
		if (!beenOut) {
			if (resident != null && resident.PlaceId == scene)
				leads.Add(new SetLead { Voice = ExecutiveVoice.None, Source = resident.Name, Text = $"The room bills {them} as its regulars." });
		} else if (heat > 0) {
			string said = string.Join(" ", facts);
			streetText = street <= 2 ? (heat >= 2 ? $"Everybody's talking about {them}. " : $"Word in the rooms is, go and see {them}. ") + said
				: street == 3 ? said
				: band < LeadOrdinaryBand ? said + " It's the room, not the band. They're loud and not much else."
				: band >= LeadGoodBand ? said + " And the band's the real thing."
				: said;
		} else if (street >= 5 && band >= LeadGoodBand && rooms.Any(r => LocalSceneRoomService.ShowsAt(r.Id, artistId) > 0))
			streetText = $"Nobody's talking about {them} yet. The people who play with them say they can.";
		if (streetText != null) leads.Add(new SetLead { Voice = ExecutiveVoice.Street, Source = "The Street", Text = streetText });

		// ── A booker you've met ──
		foreach (var room in rooms.Where(r => KnowsSceneBooker(r.Id) && LocalSceneRoomService.ShowsAt(r.Id, artistId) > 0)) {
			bool theirs = resident?.Id == room.Id;
			var d = LocalSceneRoomService.DrawAt(room.Id, artistId);
			int cap = SceneLiveEconomics.Capacity(room);
			if (!theirs && !(d is { Nights: >= 1 } && cap > 0 && d.Value.AverageArrivals >= LeadDrawShare * cap)) continue;
			var date = LocalSceneRoomService.Date(setDay);
			string when = setDay == today ? "tonight" : setDay - today < 7 ? date.DayOfWeek.ToString() : date.ToHeadlineString();
			leads.Add(new SetLead { Voice = ExecutiveVoice.None, Source = room.ContactName,
				Text = $"You should catch {them} {when}. {(theirs ? "They're our regulars." : "They bring people in.")} Trust me." });
			break;
		}

		// ── The Fixer, or a booker who rang to say so ──
		if (string.IsNullOrEmpty(act.labelId) &&
			sceneInformation.RivalLooks?.Where(l => l.ArtistId == artistId && today - l.Day <= RivalLookDays).OrderByDescending(l => l.Day).FirstOrDefault() is { } look) {
			string rival = ChartManager.Instance?.GetLabelName(look.LabelId) ?? "a label";
			if (InstinctScore(InstinctProfile, ExecutiveVoice.Fixer) >= 3)
				leads.Add(new SetLead { Voice = ExecutiveVoice.Fixer, Source = "The Fixer", Text = $"Somebody from {rival} was asking about them." });
			else if (look.ToldBy != null)
				leads.Add(new SetLead { Voice = ExecutiveVoice.None, Source = look.ToldBy, Text = $"Somebody from {rival} was in asking about them. (He rang to tell you.)" });
		}
		return leads;
	}

	/// <summary>An original in their last set in town this week, when the room was busy for it.</summary>
	private static string OriginalAskedFor(string artistId, IEnumerable<SceneRoomProfile> rooms, int today, int now) {
		foreach (var room in rooms) {
			var last = LocalSceneRoomService.LastCompletedBill(room.Id, artistId, today, now);
			if (last == null || today - last.Day > 7 || last.Capacity <= 0 || last.Attendance < LeadBusyRoom * last.Capacity) continue;
			var song = last.Appearances.FirstOrDefault(a => a.ArtistId == artistId)?.Set?.FirstOrDefault(s => s.Original && !string.IsNullOrEmpty(s.Title));
			if (song != null) return song.Title;
		}
		return null;
	}

	/// <summary>OnRivalTalentMarketAppointment: an AI label's market looked at a free act in a scene town and it stayed free.</summary>
	private void RecordRivalLook(RosterManager.DailyTalentMarketAppointment appointment) {
		var act = appointment?.SelectedArtist;
		if (Label == null || act == null || appointment.Label == null || appointment.Label.isPlayerOwned || !string.IsNullOrEmpty(act.labelId) ||
			LocalScenePersistenceService.SceneIdFor(act.geography?.basePlaceId) == null) return;
		int day = LocalSceneRoomService.Day(appointment.Date);
		var looks = sceneInformation.RivalLooks ??= new();
		looks.RemoveAll(l => day - l.Day > RivalLookDays || l.ArtistId == act.artistId);
		looks.Add(new RivalLook { ArtistId = act.artistId, LabelId = appointment.Label.labelId, Day = day });
		QueueRivalTip(act, appointment.Label.labelId, day);   // a booker he's met may ring about it (PlayerDesk.Callers)
	}
}

public partial class PlayerDesk {
	// ---- fixture access, never rendered ----
	internal void MeetBookerForCheck(string roomId) { if (SceneRoomCatalog.Get(roomId) is { } room) RememberBookerIntroduction(room); }
	internal void AddRivalLookForCheck(string artistId, string labelId, int day) =>
		(sceneInformation.RivalLooks ??= new()).Add(new RivalLook { ArtistId = artistId, LabelId = labelId, Day = day });
	internal int RivalLookCountForCheck => sceneInformation.RivalLooks?.Count ?? 0;
	/// <summary>A heard set on file for this act, as a LISTEN would leave.</summary>
	internal void ScoutingEvidenceForChecks(string artistId) {
		if (HasScoutingEvidence(artistId)) return;
		var act = ArtistManager.Instance?.GetArtist(artistId);
		scoutingKnowledge.Dossiers.Add(new ScoutingDossier { ArtistId = artistId, ArtistName = act?.stageName ?? artistId,
			Observations = { new ScoutingObservation { Id = "check_heard_" + artistId, ArtistId = artistId, Kind = ScoutingEvidenceKind.Performance } } });
	}
	/// <summary>A heard set on file (for an act that isn't anyone), so the Street has been in a room.</summary>
	internal void BeenOutForCheck() => scoutingKnowledge.Dossiers.Add(new ScoutingDossier { ArtistId = "check_been_out", ArtistName = "Check",
		Observations = { new ScoutingObservation { Id = "check_been_out", ArtistId = "check_been_out", Kind = ScoutingEvidenceKind.Performance } } });
}
