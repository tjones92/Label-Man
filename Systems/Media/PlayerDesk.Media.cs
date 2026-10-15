using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The office's PRESS & TELEVISION verbs (media directive Phase 2, §9.2 and §14). Thin wrappers over
/// <see cref="MediaAtmosphereService"/>: the desk supplies the label and the log, the media service owns every outcome.
/// While media runs in observe mode no hours, copies or cash change hands; the offer cards say what they would cost.
/// </summary>
public partial class PlayerDesk {
	/// <summary>A media line in the office log (and so in the morning paper).</summary>
	internal void MediaNote(string entry) => Note(entry);

	private RecordRuntimeData ReleasedSingle(string recordId) =>
		ReleasedRecords.FirstOrDefault(r => r?.baseRecord?.recordId == recordId);

	public IEnumerable<RecordRuntimeData> PitchableSingles() =>
		ReleasedRecords.Where(r => r?.baseRecord != null && r.baseRecord.format == ReleaseFormat.Single && !IsMasterOut(r.baseRecord.recordId));

	public bool PitchTelevision(string recordId, string programId, out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		bool ok = MediaAtmosphereService.PitchTelevision(this, ReleasedSingle(recordId), programId, out message);
		if (ok) { Note(message); Changed?.Invoke(); }
		return ok;
	}

	public bool PitchPress(string recordId, string outletId, StoryAngle angle, out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		bool ok = MediaAtmosphereService.PitchPress(this, ReleasedSingle(recordId), outletId, angle, out message);
		if (ok) { Note(message); Changed?.Invoke(); }
		return ok;
	}

	public bool AnswerTelevisionOffer(string pitchId, bool accept, MediaPreparation prep, out string message) {
		bool ok = MediaAtmosphereService.AnswerOffer(this, pitchId, accept, prep, out message);
		if (ok) { Note(message); Changed?.Invoke(); }
		return ok;
	}

	public bool AnswerStandardsQuery(string queryId, StandardsResponse response, out string message) {
		bool ok = MediaAtmosphereService.AnswerStandards(this, queryId, response, out message);
		if (ok) { Note("Standards: " + message); Changed?.Invoke(); }
		return ok;
	}

	public bool AnswerMediaInquiry(string inquiryId, string answer, string romance, out string message) {
		bool ok = MediaAtmosphereService.AnswerInquiry(this, inquiryId, answer, romance, out message);
		if (ok) { Note(message); Changed?.Invoke(); }
		return ok;
	}

	public bool RequestPressCorrection(string storyId, out string message) {
		bool ok = MediaAtmosphereService.RequestCorrection(this, storyId, out message);
		if (ok) { Note(message); Changed?.Invoke(); }
		return ok;
	}

	private const int PressColumnLines = 3;

	/// <summary>
	/// THE FANS in the office (§10, §14): where the label's own acts have a following and whose face the letters are about,
	/// then the loudest rival episodes. Proportional to the evidence: a regional favorite is never called a phenomenon.
	/// Read-only; media spotlight is shown as the fans' attention, distinct from the act's earned recognition.
	/// </summary>
	public IReadOnlyList<string> FanLines() {
		var lines = new List<string>();
		if (!MediaAtmosphere.Observing || Label == null) return lines;
		var ours = new HashSet<string>((ChartManager.Instance?.GetAllRecords() ?? new List<RecordRuntimeData>())
			.Where(r => r.baseRecord?.isPlayerOwned == true && r.baseRecord.artistId != null).Select(r => r.baseRecord.artistId), StringComparer.Ordinal);
		var mine = MediaAtmosphereService.Fans.Values.Where(f => ours.Contains(f.ArtistId) || ArtistManager.Instance?.GetArtist(f.ArtistId)?.labelId == Label.labelId)
			.Where(f => f.Attachment.Count > 0 || f.State != FanEpisodeState.None)
			.OrderByDescending(f => f.Attachment.Values.Sum()).ThenBy(f => f.ArtistId, StringComparer.Ordinal).Take(3);
		foreach (MediaFanState f in mine) {
			var warm = MediaAtmosphereService.WarmRegionNames(f, 3);
			string where = warm.Count > 0 ? "the kids are asking for them in " + string.Join(", ", warm) : "a few listeners, no following yet";
			string face = MediaFans.Face(f);
			string letters = f.Letters > 0 ? $"; about {MediaAtmosphereService.RoundLetters(f.Letters):N0} letters to the TV shows so far" : "";
			lines.Add($"{f.ArtistName}: {MediaFans.Describe(f.State)}; {where}{letters}." + (face != null ? $" The letters are mostly about {face}." : ""));
		}
		foreach (MediaFanState f in MediaAtmosphereService.LoudestFans(6).Where(f => MediaAtmosphereService.IsNational(f.State) || f.State == FanEpisodeState.Cooldown).Take(2)) {
			if (lines.Any(l => l.StartsWith(f.ArtistName + ":"))) continue;
			string face = MediaFans.Face(f);
			lines.Add($"Elsewhere: {f.ArtistName} — {MediaFans.Describe(f.State)}" + (f.State == FanEpisodeState.Cooldown ? "." : face != null ? $", and the magazines can't print enough of {face}." : "."));
		}
		return lines;
	}

	/// <summary>
	/// The paper's IN THE PAPERS rubric: today's items about the label's own acts first, then this town's paper, then the
	/// biggest national items. Read-only, composed from items already scheduled (§9.4: text never decides anything).
	/// </summary>
	public IReadOnlyList<string> PressColumn(GameDate today) {
		var lines = new List<string>();
		if (!MediaAtmosphere.Observing) return lines;
		int day = BroadcastExposure.DayIndex(today);
		string town = CurrentCityId;
		var items = MediaAtmosphereService.PublishedOn(day)
			.Select(s => (Story: s, Outlet: MediaOutletCatalog.GetPress(s.OutletId)))
			.Where(x => x.Outlet != null)
			.OrderByDescending(x => x.Story.PlayerSubject)
			.ThenByDescending(x => x.Outlet.HomeCityId == town)
			.ThenByDescending(x => x.Outlet.ReadersMillions * x.Story.Interest)
			.ThenBy(x => x.Story.Id, StringComparer.Ordinal);
		var roots = new HashSet<string>(StringComparer.Ordinal);
		foreach (var (story, outlet) in items) {
			if (lines.Count >= PressColumnLines) break;
			if (!roots.Add(story.RootId)) continue;
			if (!story.PlayerSubject && outlet.HomeCityId != town && outlet.Family is PressFamily.LocalDaily) continue;
			string body = MediaPress.Body(story);
			lines.Add($"{outlet.Name}: {MediaPress.Sentence(MediaPress.Headline(story))}" + (string.IsNullOrEmpty(body) ? "" : " " + MediaPress.Sentence(Capitalize(body))));
		}
		return lines;
	}

}
