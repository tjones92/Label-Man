using System;

/// <summary>Schema versions, not behavioral tuning. Directive N phase 1.</summary>
public static class PolarSongConstants {
	public const int SchemaVersion = 1;
}

// Spec B consolidated taxonomy. Unknown preserves missing legacy facts; it is not a neutral profile.
public enum SongArchetype {
	Unknown, StomperRocker, MidTempoRocker, GrooveRiffVamp, ShuffleTwelveBar, SlowBlues,
	JamExtendedWorkout, LightAndShadeEpic, CountryTwoBeat,
	BrightPopNumber, MidTempoPopSong, JauntyMusicHallRomp, SingAlongChant, DanceNumber,
	ChamberBaroquePiece, DramaticBalladBigBuild, SlowBallad, ReverieMoodPiece,
	LushStandard, SaloonBallad, CharmSong, Swinger,
	HornDrivenSoulNumber, DeepSoulPleader, FunkWorkout, SpiritualShout,
	VerseDrivenSong, ProtestMessageSong, RagaModalDrone, SuiteMultiPart, Collage,
	Novelty, SpokenWord, Medley, LivePartyRecord,
	CountryShuffle, CountryWaltz, WesternSwing, NashvilleBallad, BossaSong, ModernJazzInstrumental,
	SurfInstrumental, QuietHymn, GospelQuartet, GospelChoir, LatinBolero, LatinDance, TexMexSong,
	ClassicalOrchestral, ClassicalChamber, ClassicalSolo
}
public enum SongLyricMode {
	Unknown, RomanticAddress, AdviceExhortation, InvectiveAccusation, DemandBoast,
	CharacterSketch, Narrative, SceneVignette, Reminiscence, Confessional, TopicalTestimony,
	SurrealCatalogue, Nonsense, CallAndResponse, Dialogue, McPatterFrame, Instrumental
}
public enum SongVocalApproach { Unknown, Crooned, Conversational, Belted, Testifying, Shouted, HarmonyBlend, Spoken, Deadpan }
public enum SongMeter { Unknown, FourFour, ThreeFour, SixEight, Shuffle, Shifting }
public enum SongForm { Unknown, Aaba, VerseChorus, TwelveBar, Strophic, ThroughComposed }
public enum SongProduction { Unknown, LiveInRoom, Overdubbed, Layered, Constructed }
public enum SongFunction { Unknown, Opener, AlbumTrack, Interlude, Reprise, Closer, Coda }
public enum SongPerformerKind { Unknown, Band, Session, Mixed }
public enum SongVocalPresence { Unknown, Present, Instrumental }

/// <summary>Recording taxonomy, or the provisional taxonomy of an unrecorded demo.
/// Pace/mood retain authored strings until a vocabulary is supplied. No inferred archetype tuning.</summary>
[Serializable]
public sealed class SongTaxonomy {
	public int schemaVersion = PolarSongConstants.SchemaVersion;
	public string mappingId = "legacy-preserved-v1";
	public SongArchetype archetype;
	public SongLyricMode[] lyricModes = Array.Empty<SongLyricMode>();
	public SongVocalApproach[] vocalApproaches = Array.Empty<SongVocalApproach>();
	public string pace;
	public string mood;
	public Genre primaryGenre;
	public Genre secondaryGenre;
	public SongMeter? meterOverride;
	public SongForm? formOverride;
	public SongProduction production;
	public SongFunction function;
	public SongPerformerKind backingPerformers;
	public SongPerformerKind vocalPerformers;
	public SongVocalPresence vocalPresence;
	public float? durationSeconds;
	public PolarSongTags tags = new();

	public SongTaxonomy Copy() => new() {
		schemaVersion = schemaVersion, mappingId = mappingId, archetype = archetype,
		lyricModes = (SongLyricMode[])(lyricModes ?? Array.Empty<SongLyricMode>()).Clone(),
		vocalApproaches = (SongVocalApproach[])(vocalApproaches ?? Array.Empty<SongVocalApproach>()).Clone(),
		pace = pace, mood = mood, primaryGenre = primaryGenre, secondaryGenre = secondaryGenre,
		meterOverride = meterOverride, formOverride = formOverride, production = production,
		function = function, backingPerformers = backingPerformers, vocalPerformers = vocalPerformers,
		vocalPresence = vocalPresence, durationSeconds = durationSeconds, tags = (tags ?? new()).Copy()
	};
}

/// <summary>Spec C compatibility classification. Legacy IDs remain untouched for existing consumers.</summary>
[Serializable]
public sealed class PolarSongTags {
	public string[] instrumentation = Array.Empty<string>();
	public string[] technique = Array.Empty<string>();
	public string[] production = Array.Empty<string>();
	public string[] scene = Array.Empty<string>();
	public string[] release = Array.Empty<string>();
	public string[] content = Array.Empty<string>();
	public string[] unclassified = Array.Empty<string>();
	public PolarSongTags Copy() => new() {
		instrumentation = Clone(instrumentation), technique = Clone(technique), production = Clone(production),
		scene = Clone(scene), release = Clone(release), content = Clone(content), unclassified = Clone(unclassified)
	};
	private static string[] Clone(string[] values) => (string[])(values ?? Array.Empty<string>()).Clone();
}

/// <summary>Durable master metadata, independent of a market release or Godot Resource.
/// Rights stay on SongComposition and existing intentional settlement snapshots.</summary>
[Serializable]
public sealed class SongMasterMetadata {
	public int schemaVersion = PolarSongConstants.SchemaVersion;
	public string masterId;
	public string songId;
	public string parentRecordingId;
	public string artistId;
	public string title;
	public int recordingYear;
	public int recordingMonth;
	public int recordingDay;
	public bool isHistoricalReference;
	public bool isLegacyInferred;
	public SongTaxonomy taxonomy;
	public SongProfile cachedProfile;
	public string profileFingerprint;
	public PolarFitSnapshot realizedFit;
}

[Serializable]
public sealed class PolarFitSnapshot {
	public float capability, identity, moment, stretch;
	public bool hasMarketEvidence;
	public string mappingVersion;
	public static PolarFitSnapshot From(MaterialFit fit, string version) => new() {
		capability = fit.Capability, identity = fit.Identity, moment = fit.Moment, stretch = fit.Stretch,
		hasMarketEvidence = fit.HasMarketEvidence, mappingVersion = version
	};
}
