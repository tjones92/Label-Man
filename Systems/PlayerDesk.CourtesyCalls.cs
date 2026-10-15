using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Early-game directive V2 §6.1: DJ courtesy conversations, in place of the no-record HANG UP. Three calls that make
/// sense before there is anything to pitch: introduce the label, ask what is moving, arrange a listen. None of them is a
/// disguised record pitch, and none pays for repetition: the introduction gains once, a "what's moving" read refreshes
/// at most monthly, and a listen is the existing soft appointment -- kept by putting the acetate in his hands, lost
/// (with the existing small rapport cost) by never turning up. No randomness: the reads are stable on the date.
/// </summary>
public partial class PlayerDesk {
	public const int CourtesyCallMinutes = 15;
	/// <summary>Weeks before a fresh "what's moving" read has anything new in it.</summary>
	public const int MovingReadRefreshWeeks = 4;
	private const float IntroductionRapportGain = 0.025f;
	private const float KeptListenRapportGain = 0.04f;

	/// <summary>The unreleased master a listen would be arranged for: the one with an acetate in hand first, then the
	/// newest. Null when there is nothing unreleased to hear.</summary>
	private Master ListenCandidate() =>
		masters.Where(m => !m.Released && m.Record != null)
			.OrderByDescending(m => AcetatesFor(m.Record.recordId) > 0).ThenByDescending(m => m.Cut).FirstOrDefault();

	private void AddCourtesyOptions(RolodexCall call, List<CallOption> options) {
		RolodexEntry entry = call.entry;
		var intro = new CallOption {
			label = "Introduce the label",
			subLabel = $"{CourtesyCallMinutes} min · who you are, what you record, whether he's the man to know",
			approach = RolodexApproach.IntroduceLabel, minutes = CourtesyCallMinutes,
		};
		if (entry.labelIntroduced) { intro.enabled = false; intro.disabledReason = "He knows who you are. Call when you have something to say."; }
		options.Add(intro);

		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		var moving = new CallOption {
			label = "Ask what's moving for him",
			subLabel = $"{CourtesyCallMinutes} min · his own read of his station, as he tells it",
			approach = RolodexApproach.AskWhatsMoving, minutes = CourtesyCallMinutes,
		};
		if (entry.movingReadWeek > 0 && week - entry.movingReadWeek < MovingReadRefreshWeeks) {
			moving.enabled = false; moving.disabledReason = "He told you a few weeks ago; nothing much will have changed. Check the card.";
		}
		options.Add(moving);

		Master listen = ListenCandidate();
		var arrange = new CallOption {
			label = "Ask if you can bring something by",
			subLabel = listen == null ? "nothing unreleased to bring yet"
				: $"{CourtesyCallMinutes} min · a time to put \"{listen.SongTitle}\" in his hands; no promise he'll play it",
			approach = RolodexApproach.ArrangeListen, minutes = CourtesyCallMinutes,
		};
		if (listen == null) { arrange.enabled = false; arrange.disabledReason = "Cut a master first; there is nothing to bring."; }
		else if (!string.IsNullOrEmpty(entry.appointmentRecordId)) { arrange.enabled = false; arrange.disabledReason = "He's already expecting you. Keep that one first."; }
		options.Add(arrange);
	}

	private static string CourtesyOpeningLine(RolodexApproach approach) => approach switch {
		RolodexApproach.IntroduceLabel => "\"I run a new label in town. I wanted you to know the name before I ever ask you for anything.\"",
		RolodexApproach.AskWhatsMoving => "\"What's actually moving for you right now? Not the trades -- your phones.\"",
		RolodexApproach.ArrangeListen => "\"I've got something cut and I'd rather you heard it in person. When could I bring it by?\"",
		_ => "",
	};

	private void ResolveCourtesy(RolodexCall call, out string message) {
		RolodexCallContext c = call.ctx; RolodexEntry entry = call.entry;
		switch (call.pendingApproach) {
			case RolodexApproach.IntroduceLabel: {
				entry.labelIntroduced = true;
				float after = ApplyRapport(entry, IntroductionRapportGain);
				entry.MaybePromoteState(after);
				Genre[] ours = Label?.preferredGenres ?? Array.Empty<Genre>();
				float fit = ours.Length == 0 || c.dj == null ? 1f : ours.Max(g => c.dj.GenreAffinity(g));
				string line = fit >= 1.15f ? $"\"{GenreNameFormatter.Format(ours.OrderByDescending(g => c.dj.GenreAffinity(g)).First())}? That's my kind of thing. Keep me in mind.\""
					: fit >= 0.85f ? "\"Good luck to you. Send me what you've got when it's real.\""
					: "\"Nice to know you. I'll be honest: it isn't what I play much of.\"";
				call.Say(RolodexSceneBeat.Success, line, speaker: entry.displayName);
				message = fit >= 0.85f ? "He knows the name now." : "He knows the name; it may not be his kind of record.";
				entry.log.Insert(0, $"{Today()} — Introduced the label. {message}");
				break;
			}
			case RolodexApproach.AskWhatsMoving: {
				int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
				entry.movingReadWeek = Math.Max(1, week);
				string read = MovingRead(c);
				call.Say(RolodexSceneBeat.Success, read, speaker: entry.displayName);
				message = "His read, as of today. It will go stale.";
				entry.log.Insert(0, $"{Today()} — Asked what's moving: {read}");
				break;
			}
			default: {   // ArrangeListen
				Master listen = ListenCandidate();
				if (listen == null) { call.Say(RolodexSceneBeat.Failure, "\"Bring me what, exactly?\"", speaker: entry.displayName); message = "Nothing to bring."; break; }
				entry.appointmentRecordId = listen.Record.recordId;
				entry.appointmentExpiresWeek = c.week + AppointmentExpiryWeeks;
				string where = c.station != null ? $" at {c.station.callsign}" : "";
				call.Say(RolodexSceneBeat.Success, $"\"Bring it by{where} before the end of the month. If I'm on air you wait.\"", speaker: entry.displayName);
				call.Say(RolodexSceneBeat.RelationshipAftermath,
					AcetatesFor(listen.Record.recordId) > 0 ? "Hand him the acetate at the station to keep it. Standing him up costs you."
						: "You have no acetate of it yet: cut one, or bring the pressing. Standing him up costs you.");
				message = $"He expects \"{listen.SongTitle}\" within {AppointmentExpiryWeeks} weeks.";
				entry.log.Insert(0, $"{Today()} — Arranged to bring \"{listen.SongTitle}\" in person. He's expecting you.");
				break;
			}
		}
		Note($"{entry.displayName} ({c.station?.callsign}): {message}");
	}

	/// <summary>His situated, imperfect account of his own station: the sound he has most of in rotation and one record
	/// he is leaning on, read from the actual playlist. Never a ranking or a number; empty playlists get an honest shrug.</summary>
	private static string MovingRead(RolodexCallContext c) {
		var chart = ChartManager.Instance;
		var rt = c.station?.rt;
		if (chart == null || rt == null || rt.playlist.Count == 0) return "\"Honestly? Nothing's grabbing me. The sheet's all holdovers.\"";
		var rows = rt.playlist.Select(p => (Rec: chart.GetRecordRuntimeData(p.Key), Tier: p.Value)).Where(x => x.Rec?.baseRecord != null).ToList();
		if (rows.Count == 0) return "\"Same old sheet. Nothing new in a while.\"";
		var lead = rows.GroupBy(x => GenreCatalog.MapLegacy(x.Rec.baseRecord.primaryGenre, x.Rec.baseRecord.releaseDate.year))
			.OrderByDescending(g => g.Sum(x => (int)x.Tier)).ThenBy(g => g.Key).First();
		var leaning = rows.Where(x => x.Tier == SpinTier.High).OrderByDescending(x => rt.weeksInPlaylist.GetValueOrDefault(x.Rec.baseRecord.recordId))
			.ThenBy(x => x.Rec.baseRecord.recordId, StringComparer.Ordinal).FirstOrDefault();
		string sound = GenreNameFormatter.Format(lead.Key);
		return leaning.Rec != null
			? $"\"Lot of {sound} on the sheet. And \"{leaning.Rec.baseRecord.title}\" -- the phones won't leave that one alone.\""
			: $"\"Lot of {sound} on the sheet. Nothing's running away with it.\"";
	}

	/// <summary>§6.1: putting the acetate in his hands at the station keeps a listen he agreed to. The delivery itself
	/// is the existing in-person servicing; this only adds the kept-appointment reliability and clears the promise.</summary>
	private void KeepListenAppointment(string recordId, RadioStation station) {
		foreach (RolodexEntry entry in rolodex.Where(e => e.stationId == station.stationId && e.appointmentRecordId == recordId)) {
			entry.appointmentRecordId = ""; entry.appointmentExpiresWeek = 0;
			float after = ApplyRapport(entry, KeptListenRapportGain);
			entry.MaybePromoteState(after);
			entry.log.Insert(0, $"{Today()} — Brought \"{TitleForRecord(recordId)}\" in as promised.");
		}
	}
}
