using System;
using System.Collections.Generic;
using Godot;

/// <summary>What kind of thing happened that other people in the industry noticed.</summary>
public enum CulturalEventType {
	/// <summary>An album that hung together and was heard. The album-as-art signal.</summary>
	LandmarkAlbum,
	/// <summary>A record that went high enough that everyone in the trade knew about it.</summary>
	BreakthroughHit,
	/// <summary>A record that opened a genre up. Reserved; nothing publishes this yet.</summary>
	GenreBreakthrough
}

/// <summary>One thing that happened, as the rest of the industry would have known it.</summary>
public readonly struct CulturalEvent {
	public readonly long Sequence;
	public readonly string ArtistId;
	public readonly string LabelId;
	public readonly Genre Genre;
	public readonly int Year;
	public readonly CulturalEventType Type;
	/// <summary>Intrinsic quality (Layer 1). Never changes.</summary>
	public readonly float Merit;
	/// <summary>How widely it was known (Layer 2). Journalism will move this.</summary>
	public readonly float Recognition;
	/// <summary>Merit x recognition x earliness: how hard it landed on the people who heard it.</summary>
	public readonly float Strength;

	public CulturalEvent(long sequence, string artistId, string labelId, Genre genre, int year,
		CulturalEventType type, float merit, float recognition, float strength) {
		Sequence = sequence; ArtistId = artistId; LabelId = labelId; Genre = genre; Year = year;
		Type = type; Merit = merit; Recognition = recognition; Strength = strength;
	}

	public bool IsEmpty => ArtistId == null;
}

/// <summary>
/// LAYER 3: the industry's shared memory. A capacity-bounded ledger of records that other
/// people noticed, plus the lazy machinery that lets acts and labels find out about them.
/// <para>
/// Dylan knew what the Beatles had cut in 1964 and it changed what he did next; the Beatles
/// knew what he had cut and it changed what they did next. That mutual awareness is the
/// thing this file exists to make possible, and it is deliberately built as a ledger of
/// EVENTS rather than a table of relationships -- an act does not track other acts, it
/// hears records, and remembers the ones that hit it.
/// </para>
/// <para>
/// PERFORMANCE CONTRACT, and it is not negotiable on this project: propagation is lazy and
/// indexed. An artist reads the events published since they last looked, at their own next
/// release, and no faster. There is no sweep over the artist registry here, per event or
/// per week, and there must never be one.
/// </para>
/// </summary>
public static class CulturalMemoryService {
	/// <summary>
	/// Sized against measurement, not intuition. The 16-slot ring this replaces was lapped
	/// roughly every six weeks at the 1968-69 landmark rate (~2.6/week) while the median act
	/// releases twice in a DECADE, so an act witnessed ~16 of the decade's 408 landmarks and
	/// the rest were overwritten unheard. At 256 the ring holds about two years of events at
	/// that rate, which is the span over which an influence memory is worth anything anyway.
	/// </summary>
	public const int LedgerCapacity = 256;
	/// <summary>Capacity-bounded per artist: an unbounded influence list on 22.5k artists is a leak.</summary>
	public const int MaxInfluencesPerArtist = 8;
	public const int InfluenceMemoryYears = 3;
	/// <summary>Below this an event is not worth the slot it would occupy in a memory.</summary>
	public const float InfluenceFloor = .02f;
	/// <summary>A single has to have gone genuinely high before the whole trade knows about it.</summary>
	public const int BreakthroughHitPeak = 10;
	/// <summary>
	/// How hard each kind of event lands on somebody who heard it. A hit makes other acts
	/// want to do that; a landmark album changes what they think a record can be, which is a
	/// different and much rarer kind of influence.
	/// <para>
	/// Not cosmetic. Top-ten hits run ~78 a year and landmark albums a few dozen a DECADE, so
	/// at equal weight the hit channel drowns the landmark channel by two orders of magnitude
	/// and every peer motive in the run reads as chasing a hit. Measured at equal weight,
	/// PeerInfluence took 68% of all conversions -- a second monopoly in place of the
	/// commercial one, which is not the fix.
	/// </para>
	/// </summary>
	public const float HitInfluenceWeight = .55f;
	public const float LandmarkInfluenceWeight = 1f;

	private static readonly CulturalEvent[] Ledger = new CulturalEvent[LedgerCapacity];
	private static long sequence;

	/// <summary>
	/// How many OTHER acts have taken something from this act's records. The measure of
	/// being influential, accumulated from the only channel that carries influence, and the
	/// thing that separates an act with a story from an act with a genre tag.
	/// Keyed only by artists who have actually published an event, so it cannot grow to
	/// registry size.
	/// </summary>
	private static readonly Dictionary<string, int> InfluenceCounts = new();

	/// <summary>Per-label read cursor. Held here rather than on AILabel so the feature stays revertible.</summary>
	private sealed class LabelMemory {
		public long Cursor;
		public Genre WantedGenre;
		public float WantedStrength;
		public int WantedYear = -1;
	}
	private static readonly Dictionary<string, LabelMemory> LabelMemories = new();

	/// <summary>Diagnostic tap. Nothing in the simulation subscribes; the audit runner does.</summary>
	public static event Action<CulturalEvent> OnEventPublished;

	public static long EventCount => sequence;
	public static int InfluenceCountFor(string artistId) =>
		artistId != null && InfluenceCounts.TryGetValue(artistId, out int count) ? count : 0;

	// ---- PUBLICATION ----------------------------------------------------------------------------

	/// <summary>
	/// Records that something happened. Callers decide what qualifies; this only files it.
	/// </summary>
	public static void Publish(string artistId, string labelId, Genre genre, int year,
		CulturalEventType type, float merit, float recognition, float strength) {
		if (string.IsNullOrEmpty(artistId) || strength <= 0f) return;
		var published = new CulturalEvent(sequence, artistId, labelId, genre, year, type, merit,
			recognition, Mathf.Clamp(strength, 0f, 1f));
		Ledger[(int)(sequence % LedgerCapacity)] = published;
		sequence++;
		OnEventPublished?.Invoke(published);
	}

	/// <summary>
	/// The singles channel. A record that went top ten was known to everyone in the trade
	/// whatever its merit -- which is the point: a breakthrough hit propagates as a
	/// commercial fact, and only carries artistic weight to the degree it had merit.
	/// </summary>
	public static void OnChartRunComplete(SimulatedArtist artist, RecordRuntimeData record, AILabel label, int year) {
		if (!ArtistEvolution.CulturalMemoryEnabled || artist == null || record?.baseRecord == null) return;
		if (record.baseRecord.format == ReleaseFormat.Album) return;   // albums come through the landmark path
		if (record.peakPosition <= 0 || record.peakPosition > BreakthroughHitPeak) return;
		float merit = ArtisticMeritService.Evaluate(record.baseRecord, label?.productionQuality ?? .5f);
		(float recognition, _) = CulturalRecognitionService.Consume(record.baseRecord.recordId,
			record.peakPosition, artist.reputation);
		// A hit's reach is its recognition; its power to change what somebody else records is
		// its merit. A novelty number one is heard by everyone and moves nobody.
		//
		// Deliberately NOT scaled by the album-legitimacy earliness premium. Earliness is a
		// property of the album-as-art MOVEMENT -- being early to that -- and hearing
		// somebody's hit single has nothing to do with it. Gating this channel on the
		// movement's start year emptied the ledger for every year before 1964 and left the
		// peer motive at flat zero for a third of the decade, which is a statement nobody
		// intended: acts have always heard each other's records.
		float strength = Mathf.Clamp(recognition * (.35f + .65f * merit) * HitInfluenceWeight, 0f, 1f);
		if (strength <= InfluenceFloor) return;
		Publish(artist.artistId, artist.labelId, GenreCatalog.MapLegacy(record.baseRecord.primaryGenre, year),
			year, CulturalEventType.BreakthroughHit, merit, recognition, strength);
	}

	// ---- ABSORPTION -----------------------------------------------------------------------------

	/// <summary>
	/// Lazy, indexed propagation for one act, called at their own next release. Reads only
	/// what has been published since they last looked.
	/// </summary>
	public static void AbsorbForArtist(SimulatedArtist artist, int year) {
		if (!ArtistEvolution.CulturalMemoryEnabled) return;
		ArtistEvolutionProfile profile = artist?.evolution;
		if (profile == null || profile.lastLandmarkSequenceSeen >= sequence) return;
		long from = Math.Max(profile.lastLandmarkSequenceSeen, sequence - LedgerCapacity);
		Genre identity = GenreCatalog.MapLegacy(artist.primaryGenre, year);
		for (long index = from; index < sequence; index++) {
			CulturalEvent culturalEvent = Ledger[(int)(index % LedgerCapacity)];
			if (culturalEvent.IsEmpty || culturalEvent.ArtistId == artist.artistId) continue;
			if (year - culturalEvent.Year > InfluenceMemoryYears) continue;
			// You have to have been paying attention, and it has to be music you could
			// plausibly have made. This is the only place peer influence enters.
			float adjacency = GenreMarketMomentumService.GetAdjacency(identity, culturalEvent.Genre);
			if (adjacency < ArtistEvolution.AdjacencyFloor) continue;
			// peerSensitivity is applied HERE and nowhere else. It was previously applied
			// again when the pressure was read, squaring a sub-unit term and costing ~4x at
			// a typical .5 -- which is most of why peer pressure never once won a motive.
			float strength = Mathf.Clamp(culturalEvent.Strength * adjacency * profile.peerSensitivity, 0f, 1f);
			if (strength <= InfluenceFloor) continue;
			Remember(profile, new ArtistInfluenceMemory {
				sourceArtistId = culturalEvent.ArtistId,
				sourceGenre = culturalEvent.Genre,
				type = ToInfluenceType(culturalEvent.Type),
				year = culturalEvent.Year,
				strength = strength
			}, year);
			InfluenceCounts[culturalEvent.ArtistId] = InfluenceCounts.GetValueOrDefault(culturalEvent.ArtistId) + 1;
			// Being taken from is the durable standing signal: the SOURCE act earns cultural
			// standing whenever another act converts on its record. Recognition-gated internally,
			// so it is inert unless recognition is observing.
			ArtistRecognitionService.AddCulturalStanding(
				ArtistManager.Instance?.GetArtist(culturalEvent.ArtistId), ArtistRecognitionService.InfluenceStandingGain * strength);
		}
		profile.lastLandmarkSequenceSeen = sequence;
	}

	/// <summary>
	/// The same read, for a label. Labels notice what is working and lean on their acts to
	/// go and get some of it, which is a motive of its own rather than a restatement of how
	/// badly the act is selling. Cursored per label; there are hundreds of labels, not
	/// tens of thousands, so this is cheap.
	/// </summary>
	public static (Genre? Genre, float Strength) AbsorbForLabel(AILabel label, int year) {
		if (!ArtistEvolution.CulturalMemoryEnabled || label == null || string.IsNullOrEmpty(label.labelId))
			return (null, 0f);
		if (!LabelMemories.TryGetValue(label.labelId, out LabelMemory memory))
			LabelMemories[label.labelId] = memory = new LabelMemory();
		if (memory.Cursor < sequence) {
			long from = Math.Max(memory.Cursor, sequence - LedgerCapacity);
			for (long index = from; index < sequence; index++) {
				CulturalEvent culturalEvent = Ledger[(int)(index % LedgerCapacity)];
				if (culturalEvent.IsEmpty || culturalEvent.LabelId == label.labelId) continue;
				// A label chases what it already believes in, or what is close enough to it
				// that the A&R man can imagine signing it. Preference is the filter.
				float fit = PreferenceFit(label, culturalEvent.Genre);
				if (fit <= 0f) continue;
				float strength = culturalEvent.Strength * fit;
				if (strength <= memory.WantedStrength) continue;
				memory.WantedGenre = culturalEvent.Genre;
				memory.WantedStrength = strength;
				memory.WantedYear = culturalEvent.Year;
			}
			memory.Cursor = sequence;
		}
		if (memory.WantedYear < 0) return (null, 0f);
		// What a label wants goes stale like anything else.
		float age = Mathf.Clamp(1f - (year - memory.WantedYear) / (float)InfluenceMemoryYears, 0f, 1f);
		if (age <= 0f) { memory.WantedStrength = 0f; memory.WantedYear = -1; return (null, 0f); }
		return (memory.WantedGenre, Mathf.Clamp(memory.WantedStrength * age, 0f, 1f));
	}

	/// <summary>How much a label cares about a genre: its own, one it dabbles in, or a musical neighbour.</summary>
	private static float PreferenceFit(AILabel label, Genre genre) {
		if (label.preferredGenres != null)
			foreach (Genre preferred in label.preferredGenres) if (preferred == genre) return 1f;
		if (label.secondaryGenres != null)
			foreach (Genre secondary in label.secondaryGenres) if (secondary == genre) return .70f;
		float best = 0f;
		if (label.preferredGenres != null)
			foreach (Genre preferred in label.preferredGenres)
				best = Mathf.Max(best, GenreMarketMomentumService.GetAdjacency(preferred, genre));
		return best < ArtistEvolution.AdjacencyFloor ? 0f : .55f * best;
	}

	private static ArtistInfluenceType ToInfluenceType(CulturalEventType type) => type switch {
		CulturalEventType.LandmarkAlbum => ArtistInfluenceType.CohesiveAlbum,
		CulturalEventType.GenreBreakthrough => ArtistInfluenceType.GenreBreakthrough,
		_ => ArtistInfluenceType.HitSingle
	};

	/// <summary>Keeps the strongest few and forgets anything stale, on insert.</summary>
	private static void Remember(ArtistEvolutionProfile profile, ArtistInfluenceMemory memory, int year) {
		profile.influences.RemoveAll(existing => year - existing.year > InfluenceMemoryYears);
		profile.influences.Add(memory);
		if (profile.influences.Count <= MaxInfluencesPerArtist) return;
		int weakest = 0;
		for (int index = 1; index < profile.influences.Count; index++)
			if (profile.influences[index].strength < profile.influences[weakest].strength) weakest = index;
		profile.influences.RemoveAt(weakest);
	}

	internal static void ResetForProbe() {
		sequence = 0;
		Array.Clear(Ledger, 0, Ledger.Length);
		InfluenceCounts.Clear();
		LabelMemories.Clear();
	}
}
