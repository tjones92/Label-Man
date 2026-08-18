using System;
using System.Collections.Generic;
using Godot;

// Publishing & Cover-Song layer (Phase 0, data-only). A Record is a performance/master; a
// SongComposition is the underlying song. Covers, standards, staff songs, and artist originals are
// all just different ways of attaching a Record to a SongComposition. See
// SimTools/PublishingCoverSongDirective.md. In Phase 0 these types are authored and attached but the
// economy does not yet read them (settlement still keys off SimulatedArtist.labelOwnsPublishing).

/// <summary>How a Record's underlying song came to be attached to it.</summary>
public enum SongMaterialSource {
	Unknown,
	// New songs
	ArtistWritten,
	ArtistCowrittenWithProfessional,
	LabelStaffWriter,
	ExternalProfessional,
	// Existing songs
	CoverRecentHit,
	CoverCatalogSong,
	CoverStandard,
	TraditionalPublicDomain,
	AdaptedTraditional
}

public enum WriterEntityType {
	Unknown,
	Musician,
	ProfessionalSongwriter,
	Traditional,
	PublicDomain,
	HouseCredit
}

/// <summary>Who controls the publishing (the goldmine axis). Consumed for real in Phase 3.</summary>
public enum PublishingControlType {
	Unknown,
	PublicDomain,
	ExternalPublisher,
	LabelAffiliate,
	ArtistControlled,
	LabelBuyout,
	SharedControl
}

public enum SongOriginKind {
	Unknown,
	PreGameStandard,
	PreGameCatalog,
	Traditional,
	ArtistOriginal,
	ProfessionalOffice,
	LabelStaff,
	RecentHit
}

/// <summary>Professional publishing "scenes" -- rights-metadata only in this phase.</summary>
public enum PublishingScene {
	LegacyTinPanAlley,
	NewYorkPopFactory, // Brill-style abstraction
	Nashville,
	DetroitInHouse,
	MemphisSoul,
	LosAngelesPop,
	IndependentFolk,
	ChurchGospel,
	LabelAffiliate
}

[Serializable]
public sealed class SongwriterCredit {
	public WriterEntityType writerType;
	public string writerId;
	public string writerName;
	public float share;
	public bool isArtistMember;
}

[Serializable]
public sealed class SongRightsProfile {
	public PublishingControlType controlType = PublishingControlType.Unknown;
	public string publisherId;
	public string publisherName;
	public string controllerLabelId;
	public string controllerArtistId;
	// Common abstraction: the composition pool splits into writer and publisher halves.
	public float writerShare = 0.50f;
	public float publisherShare = 0.50f;
	public bool compulsoryMechanicalAvailable = true;
	public bool exclusiveHold;
	public int exclusiveHoldUntilWeek;
}

/// <summary>A remembered recording of a song, appended when a Record completes its chart run.</summary>
[Serializable]
public sealed class SongRecordingMemory {
	public string recordId;
	public string artistId;
	public string artistName;
	public int year;
	public int peakPosition;
	public int weeksOnChart;
	public int units;
	public float definitiveVersionScore;
}

/// <summary>
/// The underlying song: melody, lyric, hook, familiarity, standardness, rights, and the history of
/// every record cut from it. NOT a Godot Resource -- it lives in CompositionCatalogService keyed by
/// songId; Records reference it by the durable string songId.
/// </summary>
[Serializable]
public sealed class SongComposition {
	public string songId;
	public string title;
	public Genre primaryGenre;
	public Genre secondaryGenre;
	// Seasonal / holiday and other secondary tags, mirrored onto covering records' genreTagIds so
	// the existing seasonal-tag boost applies to a cut of a Christmas standard.
	public string[] genreTagIds = Array.Empty<string>();
	public int originYear;
	public SongOriginKind originKind;
	public float compositionQuality;
	public float melodicStrength;
	public float lyricQuality;
	public float commercialHook;
	public float rhythmicAppeal;
	public float adaptability;
	public float originality;
	// How likely this song survives as a standard.
	public float standardDurability;
	// Public familiarity independent of any one record.
	public float nationalFamiliarity;
	public float adultFamiliarity;
	public float teenFamiliarity;
	public float regionalFamiliarityBias;
	public bool isTraditional;
	public bool isPublicDomain;
	public bool isStandard;
	public bool isCoverable = true;
	public SongRightsProfile rights = new();
	public List<SongwriterCredit> credits = new();
	public List<SongRecordingMemory> recordings = new();

	public float GetCraftScore() {
		return Mathf.Clamp(
			compositionQuality * 0.30f +
			melodicStrength * 0.25f +
			lyricQuality * 0.18f +
			commercialHook * 0.17f +
			adaptability * 0.10f,
			0f,
			1f
		);
	}

	public float GetFamiliarityForYear(int year) {
		int age = Mathf.Max(0, year - originYear);
		// Standards decay slowly; fad songs decay quickly.
		float retention = isStandard
			? Mathf.Pow(0.992f, age)
			: Mathf.Pow(0.955f, age);
		return Mathf.Clamp(nationalFamiliarity * retention + standardDurability * 0.12f, 0f, 1f);
	}
}

/// <summary>
/// A professional (office/staff) songwriter who is not an artist. Rights-metadata only in this phase:
/// the credited counterparty on a professional song's rights, with no P&L or agency yet.
/// </summary>
[Serializable]
public sealed class ProfessionalSongwriter {
	public string writerId;
	public string name;
	public string publisherId;
	public Genre primaryGenre;
	public Genre secondaryGenre;
	public float melodyCraft;
	public float lyricCraft;
	public float hookCraft;
	public float commercialInstinct;
	public float versatility;
	public float reliability;
	public float trendSensitivity;
	public int activeStartYear;
	public int activeEndYear;
}

/// <summary>A publisher house. Rights-metadata only in this phase.</summary>
[Serializable]
public sealed class MusicPublisher {
	public string publisherId;
	public string publisherName;
	public string affiliateLabelId;
	public PublishingScene scene;
	public Genre[] focusGenres = Array.Empty<Genre>();
	public float catalogQuality;
	public float songPluggerSkill;
	public float commercialAggression;
	public float artistFriendly;
	public float buyoutWillingness;
	public List<string> staffWriterIds = new();
	public List<string> catalogSongIds = new();
}
