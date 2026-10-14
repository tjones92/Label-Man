using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Phase 6 of SimTools/BandMemberSimulationDirective.md (§4.13): scouting the rough.
/// <para>
/// The player never sees a ceiling. They see <b>tells</b>: how young the act is, how far its live set reaches past
/// what it can play now (the set's Polar demand against the act's capability, through <see cref="PolarMaterialFit"/>),
/// how much of the set is their own, a partly visible read on ambition, and -- on a revisit three months or more
/// after the first note -- how much tighter they have got (the <b>improvement delta</b>, a change in Execution).
/// </para>
/// <para>
/// The <b>potential read</b> is built from those tells and nothing else, so it is wrong the way they are wrong: an
/// act reaching past itself with no headroom reads as promising, a finished pro with untapped vocal room reads as
/// done. It is never noise added to the ceiling, which would only be a noisy reveal of the answer.
/// </para>
/// </summary>
public static class ScoutingRough {
	public const int ImprovementDays = 90;

	public sealed class Tells {
		public int MedianAge;
		public float Overreach;        // mean capability deficit across the heard set, 0 = well within them
		public float OriginalsShare;
		public float AmbitionRead;     // label-keyed blur over the members' real ambition
		public float? Improvement;     // execution now minus then, on a revisit after ImprovementDays
		public float IdentityRigidity; // how settled the act's sense of itself reads
	}

	public static Tells Read(SimulatedArtist a, AILabel label, IEnumerable<PlayerDesk.RepertoireItem> heard, int week,
		float? executionThen, float executionNow, int daysSinceFirstNote) {
		var t = new Tells();
		var members = a.members.Where(m => m != null && m.isActive).ToList();
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		var ages = members.Select(m => m.GetAge(year)).OrderBy(x => x).ToList();
		t.MedianAge = ages.Count == 0 ? 25 : ages[ages.Count / 2];
		PolarSongTable table = PolarSongTable.Current;
		ActProfile act = PolarActProfileDeriver.Derive(a, label, null, table);
		t.IdentityRigidity = act.identityRigidity;
		var songs = heard?.ToList() ?? new List<PlayerDesk.RepertoireItem>();
		float deficit = 0f; int n = 0;
		foreach (PlayerDesk.RepertoireItem item in songs) {
			SongComposition song = string.IsNullOrEmpty(item.SongId) ? null : CompositionCatalogService.GetSong(item.SongId);
			if (song == null) continue;
			SongProfile reference = SongProfileDeriver.Derive(song, song.demoTaxonomy, table);
			deficit += PolarMaterialFit.Compute(reference, act, null, week, table).Deficit;
			n++;
		}
		t.Overreach = n == 0 ? 0f : deficit / n;
		t.OriginalsShare = songs.Count == 0 ? 0f : songs.Count(s => s.IsOriginal) / (float)songs.Count;
		float ambition = members.Count == 0 ? 0.5f : members.Average(m => m.ambition);
		t.AmbitionRead = Mathf.Clamp(ambition + 0.15f * Blur(label, a, "ambition"), 0f, 1f);
		if (executionThen.HasValue && daysSinceFirstNote >= ImprovementDays) t.Improvement = executionNow - executionThen.Value;
		return t;
	}

	/// <summary>The potential read, 0..1, from the tells only.</summary>
	public static float Potential(Tells t) {
		float youth = Mathf.Clamp((30f - t.MedianAge) / 10f, 0f, 1f);
		float reach = Mathf.Clamp(t.Overreach / 0.15f, 0f, 1f);
		float score = 0.35f * youth + 0.25f * reach + 0.15f * t.OriginalsShare + 0.15f * t.AmbitionRead + 0.10f;
		if (t.Improvement.HasValue) score += 0.25f * Mathf.Clamp(t.Improvement.Value / 0.05f, -1f, 1f);
		return Mathf.Clamp(score, 0f, 1f);
	}

	public static string PotentialWord(float potential) => potential >= 0.55f ? "wide" : potential >= 0.38f ? "some room" : "narrow";

	/// <summary>The three lines of the read, in the ear's vocabulary: Execution, Identity, Potential.</summary>
	public static List<string> Lines(Tells t, string executionPhrase) {
		float potential = Potential(t);
		string identity = t.IdentityRigidity >= 0.62f ? "knows exactly who they are" : t.IdentityRigidity >= 0.42f ? "a clear idea of themselves" :
			"still working out who they are";
		var why = new List<string>();
		if (t.MedianAge <= 21) why.Add("very young"); else if (t.MedianAge <= 24) why.Add("young");
		else if (t.MedianAge >= 30) why.Add("seasoned");
		if (t.Overreach >= 0.08f) why.Add("reaching past what they can play"); else if (t.Overreach <= 0.02f) why.Add("playing well inside themselves");
		if (t.OriginalsShare >= 0.5f) why.Add("writing their own");
		if (t.AmbitionRead >= 0.65f) why.Add("hungry"); else if (t.AmbitionRead <= 0.35f) why.Add("comfortable");
		if (t.Improvement.HasValue)
			why.Add(t.Improvement.Value >= 0.015f ? "much tighter than when you first saw them" :
				t.Improvement.Value <= -0.015f ? "looser than when you first saw them" : "about where they were last time");
		return new List<string> {
			$"Execution: {executionPhrase}",
			$"Identity: {identity}",
			$"Potential: {PotentialWord(potential)}{(why.Count > 0 ? " — " + string.Join(", ", why) : "")}"
		};
	}

	private static float Blur(AILabel label, SimulatedArtist a, string field) {
		ulong hash = 14695981039346656037UL;
		foreach (char c in $"{label?.labelId}|{a?.artistId}|{field}|RoughV1") { hash ^= c; hash *= 1099511628211UL; }
		return ((hash >> 40) * (1f / 16777216f)) * 2f - 1f;
	}
}
