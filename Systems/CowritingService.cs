using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Co-writing, layer 1 -- credit teams (SimTools/BandMemberSimulationDirective.md §4.14).
/// <para>
/// Before this, every artist original was credited 100% to <see cref="SimulatedArtist.GetMainWriter"/>, the
/// first flagged writer in LIST ORDER, so credit inside a band was always 100/0/0/0 and the Lennon /
/// McCartney / Harrison / Starr split could not occur (§2.3). Fed to a credit grievance, that binary shape
/// would win every comparison on shape rather than magnitude. This layer gives each original a writing team
/// from the act's voiced writers. It writes CREDITS ONLY: song traits are untouched, so the economy is
/// byte-identical, and the grievance it feeds has a real gap to measure.
/// </para>
/// <para>
/// Every draw is keyed on the world seed and the record id; nothing touches the global or catalogue stream.
/// </para>
/// </summary>
public static class CowritingService {
	/// <summary>When an act has a second flagged writer, how often a song is theirs together.</summary>
	public const float PrimaryCoWriteChance = 0.38f;
	/// <summary>How often a minor writer gets a hand in a flagged writer's song.</summary>
	public const float MinorContributionChance = 0.08f;
	/// <summary>A minor writer's share when they contribute to someone else's song.</summary>
	public const float MinorContributionShare = 0.25f;
	/// <summary>Co-credited songs before two flagged writers agree to share everything.</summary>
	public const int PactCoCreditThreshold = 3;
	/// <summary>Not every pair that writes together formalises it. Keyed per pair.</summary>
	public const float PactFormationChance = 0.55f;

	// Telemetry: originals credited per year, and how many carried more than one member.
	private static readonly Dictionary<int, int> originalsByYear = new();
	private static readonly Dictionary<int, int> multiWriterByYear = new();
	private static readonly Dictionary<int, int> pactsFormedByYear = new();
	public static IReadOnlyDictionary<int, int> OriginalsByYear => originalsByYear;
	public static IReadOnlyDictionary<int, int> MultiWriterByYear => multiWriterByYear;
	public static IReadOnlyDictionary<int, int> PactsFormedByYear => pactsFormedByYear;

	public static void ResetTelemetry() { originalsByYear.Clear(); multiWriterByYear.Clear(); pactsFormedByYear.Clear(); }

	/// <summary>
	/// Player-only credit decisions (the Band Room): a writing session's team, a member promised a song, a label
	/// cut-in. The desk supplies one for its own acts; it is null in every headless AI run, so the AI path never
	/// sees it.
	/// </summary>
	public sealed class TeamOverride {
		public readonly List<string> PersonIds = new();
		public bool PromiseConsumed;
		public float LabelCutInShare;
		public string LabelName;
		public string LabelId;
		public readonly HashSet<string> CreditedPersonIds = new();
		public Action OnApplied;
	}

	public static Func<SimulatedArtist, string, TeamOverride> PlayerTeamOverride;

	/// <summary>The writer id a label cut-in is credited under, so the mechanical royalty can find it.</summary>
	public static string LabelWriterId(string labelId) => "label:" + labelId;

	/// <summary>
	/// Writes the credits for a fresh artist original. With co-writing off this is the legacy path exactly:
	/// the first flagged writer in list order at 100%, or a house credit in the act's name.
	/// </summary>
	public static void CreditOriginal(SongComposition song, SimulatedArtist artist, string songKey, int year) {
		if (song == null || artist == null) return;
		if (!BandLife.CowritingEnabled) {
			Musician writer = artist.GetMainWriter();
			if (writer != null) song.credits.Add(MemberCredit(writer, 1f));
			else song.credits.Add(HouseCredit(artist));
			CompositionCatalogService.RecordOriginalCredits(song, artist);
			return;
		}
		TeamOverride over = artist.isPlayerOwned ? PlayerTeamOverride?.Invoke(artist, songKey) : null;
		List<(Musician Member, float Share)> team = over != null && over.PersonIds.Count > 0
			? OverrideTeam(artist, over, year) : PickTeam(artist, songKey, year);
		// Phase 4c layer 4: an AI label's cut-in, the AI twin of the player's demand.
		float aiCutIn = over == null && team.Count > 0 ? TeamWritingService.AiCutInShare(artist, songKey) : 0f;
		float cutIn = over != null && over.LabelCutInShare > 0f ? over.LabelCutInShare : aiCutIn;
		float memberScale = cutIn > 0f && team.Count > 0 ? 1f - cutIn : 1f;
		if (team.Count == 0) song.credits.Add(HouseCredit(artist));
		else foreach ((Musician member, float share) in team) song.credits.Add(MemberCredit(member, share * memberScale));
		if (memberScale < 1f) {
			string labelId = over != null ? over.LabelId : artist.labelId;
			string labelName = over != null ? over.LabelName :
				ChartManager.Instance?.GetAllLabels()?.FirstOrDefault(l => l.labelId == artist.labelId)?.labelName ?? "the label";
			song.credits.Add(new SongwriterCredit { writerType = WriterEntityType.HouseCredit, writerId = LabelWriterId(labelId),
				writerName = labelName, share = cutIn });
			if (aiCutIn > 0f) TeamWritingService.ApplyCutInStrain(artist, team.Select(t => t.Member), year);
		}
		CompositionCatalogService.RecordOriginalCredits(song, artist);
		if (over != null) {
			foreach ((Musician member, _) in team) over.CreditedPersonIds.Add(member.personId);
			over.OnApplied?.Invoke();
		}
		originalsByYear[year] = originalsByYear.GetValueOrDefault(year) + 1;
		if (team.Count > 1) multiWriterByYear[year] = multiWriterByYear.GetValueOrDefault(year) + 1;
	}

	/// <summary>
	/// The team for one song. The lead is a keyed weighted draw over the act's writers (flagged writers at
	/// creativity x ambition, creative non-writers at a fraction -- which is how the quiet member gets the odd
	/// whole song). A pact credits the lead's partner equally on anything either writes. Otherwise a second
	/// flagged writer co-writes some of the time, and a minor writer occasionally gets a hand in.
	/// Returns (member, share) with shares summing to 1; empty when the act has no writer at all.
	/// </summary>
	/// <param name="record">False for a side-effect-free peek (team craft scores the team before the song exists).</param>
	public static List<(Musician Member, float Share)> PickTeam(SimulatedArtist artist, string songKey, int year, bool record = true) {
		var team = new List<(Musician, float)>();
		List<(Musician Member, float Weight, bool Primary)> writers = artist.GetWriters();
		// An act with no flagged writer keeps the house credit, exactly as before: a band of creative
		// non-writers did not suddenly start writing because the credit model learned arithmetic.
		if (!writers.Any(w => w.Primary)) return team;

		string key = $"cowrite|{artist.artistId}|{songKey}";
		var lead = WeightedPick(writers, $"{key}|lead");
		var leadPrimary = writers.First(w => w.Member == lead).Primary;

		WritingPartnership pact = artist.writingPartnerships?.FirstOrDefault(p => p.pact && Involves(p, lead.personId) &&
			writers.Any(w => w.Member.personId == Other(p, lead.personId)));
		if (pact != null) {
			Musician partner = writers.First(w => w.Member.personId == Other(pact, lead.personId)).Member;
			team.Add((lead, 0.5f));
			team.Add((partner, 0.5f));
			if (record) RecordCoCredit(artist, lead, partner, year);
			return team;
		}

		var otherPrimaries = writers.Where(w => w.Primary && w.Member != lead).ToList();
		if (leadPrimary && otherPrimaries.Count > 0 && BandLife.Chance($"{key}|cowrite", PrimaryCoWriteChance)) {
			Musician partner = WeightedPick(otherPrimaries, $"{key}|partner");
			team.Add((lead, 0.5f));
			team.Add((partner, 0.5f));
			if (record) RecordCoCredit(artist, lead, partner, year);
			return team;
		}

		var minors = writers.Where(w => !w.Primary && w.Member != lead).ToList();
		if (leadPrimary && minors.Count > 0 && BandLife.Chance($"{key}|minor", MinorContributionChance)) {
			Musician helper = WeightedPick(minors, $"{key}|helper");
			team.Add((lead, 1f - MinorContributionShare));
			team.Add((helper, MinorContributionShare));
			return team;
		}

		team.Add((lead, 1f));
		return team;
	}

	/// <summary>A team the player decided: equal shares among the named members still in the act (a pact partner
	/// joins a promised writer, as the pact says). Co-credits accrue exactly as a picked team's do.</summary>
	private static List<(Musician Member, float Share)> OverrideTeam(SimulatedArtist artist, TeamOverride over, int year) {
		var people = over.PersonIds.Select(id => artist.members.FirstOrDefault(m => m.isActive && m.personId == id))
			.Where(m => m != null).Distinct().ToList();
		if (people.Count == 1) {
			WritingPartnership pact = artist.writingPartnerships?.FirstOrDefault(p => p.pact && Involves(p, people[0].personId));
			Musician partner = pact == null ? null : artist.members.FirstOrDefault(m => m.isActive && m.personId == Other(pact, people[0].personId));
			if (partner != null) people.Add(partner);
		}
		var team = people.Select(m => (m, 1f / people.Count)).ToList();
		for (int i = 0; i < people.Count; i++) for (int j = i + 1; j < people.Count; j++) {
			Musician a = people[i], b = people[j];
			if (a.isPrimaryWriter && b.isPrimaryWriter) RecordCoCredit(artist, a, b, year);
			else GetOrCreatePartnership(artist, a.personId, b.personId).coCredits++;
		}
		return team;
	}

	/// <summary>Counts a co-credit between two flagged writers and, once they have written enough together,
	/// may turn it into a pact. Keyed per pair, so whether a given pair formalises is fixed for the world.</summary>
	private static void RecordCoCredit(SimulatedArtist artist, Musician a, Musician b, int year) {
		if (!a.isPrimaryWriter || !b.isPrimaryWriter) return;
		WritingPartnership p = GetOrCreatePartnership(artist, a.personId, b.personId);
		p.coCredits++;
		if (!p.pact && p.coCredits >= PactCoCreditThreshold &&
			BandLife.Chance($"pact|{artist.artistId}|{p.personA}|{p.personB}", PactFormationChance)) {
			p.pact = true;
			p.pactYear = year;
			pactsFormedByYear[year] = pactsFormedByYear.GetValueOrDefault(year) + 1;
		}
	}

	public static WritingPartnership GetOrCreatePartnership(SimulatedArtist artist, string idA, string idB) {
		(string a, string b) = string.CompareOrdinal(idA, idB) <= 0 ? (idA, idB) : (idB, idA);
		artist.writingPartnerships ??= new List<WritingPartnership>();
		WritingPartnership p = artist.writingPartnerships.FirstOrDefault(x => x.personA == a && x.personB == b);
		if (p == null) {
			p = new WritingPartnership { personA = a, personB = b };
			artist.writingPartnerships.Add(p);
		}
		return p;
	}

	public static WritingPartnership FindPartnership(SimulatedArtist artist, string idA, string idB) {
		(string a, string b) = string.CompareOrdinal(idA, idB) <= 0 ? (idA, idB) : (idB, idA);
		return artist?.writingPartnerships?.FirstOrDefault(x => x.personA == a && x.personB == b);
	}

	/// <summary>Grant a pact by decision rather than by habit -- the player conceding it in the Band Room.</summary>
	public static void ConcedePact(SimulatedArtist artist, Musician a, Musician b, int year) {
		WritingPartnership p = GetOrCreatePartnership(artist, a.personId, b.personId);
		if (p.pact) return;
		p.pact = true;
		p.pactYear = year;
		p.pactConcededByLabel = true;
		pactsFormedByYear[year] = pactsFormedByYear.GetValueOrDefault(year) + 1;
	}

	private static bool Involves(WritingPartnership p, string id) => p.personA == id || p.personB == id;
	private static string Other(WritingPartnership p, string id) => p.personA == id ? p.personB : p.personA;

	private static Musician WeightedPick(List<(Musician Member, float Weight, bool Primary)> pool, string key) {
		float total = 0f;
		foreach (var w in pool) total += w.Weight;
		float draw = BandLife.Unit(key) * total;
		foreach (var w in pool) { draw -= w.Weight; if (draw < 0f) return w.Member; }
		return pool[^1].Member;
	}

	private static SongwriterCredit MemberCredit(Musician m, float share) => new() {
		writerType = WriterEntityType.Musician, writerId = m.personId, writerName = m.FullName,
		share = share, isArtistMember = true
	};

	private static SongwriterCredit HouseCredit(SimulatedArtist artist) => new() {
		writerType = WriterEntityType.HouseCredit, writerName = artist.stageName, share = 1f
	};
}
