using System;

/// <summary>
/// A member's second role: the harmony a guitarist sings on stage, the guitar a folk singer plays, the organ a pianist
/// doubles on. <see cref="Musician.secondaryRole"/> was declared and read (by studio code) but never written, so it
/// read as LeadVocals for everyone; <see cref="MusicianRole.None"/> now means "nothing besides the main part".
/// <para>
/// <b>Keyed and player-side.</b> Every draw is keyed on the world seed and the person id (BandLife.Unit) and touches no
/// RNG stream, and only studio code reads the result, so the AI economy is byte-identical. The rule reads the voice
/// axes when they exist and otherwise the axes' own expected value, so it never generates axes early.
/// </para>
/// <para>
/// <b>Provisional authored rates.</b> Harmony by genre follows the period's practice in broad strokes: surf, beat and
/// folk groups were built on members' harmonies (the Beach Boys, the Beatles, the Kingston Trio's players), country
/// bands carried a harmony singer (Don Rich in the Buckaroos), R&amp;B and soul bands mostly left voices to the singers,
/// and jazz combos rarely sang. Drummers and horn players sing less because they can't do both at once. None of
/// these shares is calibrated against a census.
/// </para>
/// </summary>
public static class MemberRoleService {
	public const int CurrentVersion = 1;
	/// <summary>A bandmate who sings harmony on stage holds a part in the studio from this voice up. Below the .45
	/// unprompted bar (<see cref="PlayerDesk.StudioHarmonyBar"/>): singing the part every night is the practice.</summary>
	public const float HarmonyVoiceFloor = .35f;

	/// <summary>The member's second role, assigning it first if it never was.</summary>
	public static MusicianRole Secondary(Musician m) {
		if (m == null) return MusicianRole.None;
		if (m.secondaryRoleVersion < CurrentVersion) Ensure(m, null);
		return m.secondaryRole;
	}

	public static bool Plays(Musician m, MusicianRole role) =>
		m != null && (m.primaryRole == role || Secondary(m) == role);

	/// <summary>A member who sings harmony behind the lead: a singer by part, or a player whose second role is harmony
	/// and whose voice holds a part.</summary>
	public static bool SingsHarmony(Musician m) =>
		m != null && !m.isLeadVocalist && (m.primaryRole is MusicianRole.LeadVocals or MusicianRole.BackingVocals ||
			Secondary(m) == MusicianRole.BackingVocals && SessionEmploymentService.BackingVoice(m) >= HarmonyVoiceFloor);

	/// <summary>Assigns the second role once. Idempotent; keyed; reads the act's genre when known.</summary>
	public static void Ensure(Musician m, Genre? genre, int year = 1960) {
		if (m == null || m.secondaryRoleVersion >= CurrentVersion) return;
		m.secondaryRole = Derive(m, genre, year);
		m.secondaryRoleVersion = CurrentVersion;
	}

	public static void Ensure(SimulatedArtist act, int year) {
		if (act?.members == null) return;
		foreach (Musician m in act.members) Ensure(m, act.primaryGenre, year);
	}

	/// <summary>A main part changed (joining an act, a slot swap). A person who now plays what they used to double on
	/// keeps the old part as their second one.</summary>
	public static void OnPrimaryChanged(Musician m, MusicianRole old, Genre? genre, int year) {
		if (m == null || old == m.primaryRole) return;
		Ensure(m, genre, year);
		bool vocalNow = m.primaryRole is MusicianRole.LeadVocals or MusicianRole.BackingVocals;
		if (m.secondaryRole == m.primaryRole) m.secondaryRole = old == MusicianRole.LeadVocals && !vocalNow ? MusicianRole.BackingVocals : old;
		if (vocalNow && m.secondaryRole is MusicianRole.LeadVocals or MusicianRole.BackingVocals) m.secondaryRole = MusicianRole.None;
	}

	/// <summary>The rule itself, without the version gate, so a check can exercise it.</summary>
	internal static MusicianRole Derive(Musician m, Genre? genre, int year) {
		string key = $"second-role|{m.personId}";
		GenreFamily? family = null;
		if (genre.HasValue) family = GenreCatalog.Get(GenreCatalog.MapLegacy(genre.Value, year)).Family;
		bool singerPart = m.isLeadVocalist || m.primaryRole is MusicianRole.LeadVocals or MusicianRole.BackingVocals;

		if (!singerPart && m.primaryRole != MusicianRole.MultiInstrumentalist) {
			float voice = m.axesVersion > 0 ? SessionEmploymentService.BackingVoice(m) : .30f + .35f * (m.technicalSkill - .45f);
			float p = HarmonyShare(genre, family) * RoleSingShare(m.primaryRole) * Math.Clamp(.5f + 2f * (voice - .35f), .15f, 1.5f);
			if (BandLife.Unit(key + "|harmony") < p) return MusicianRole.BackingVocals;
		}

		float hands = m.axesVersion > 0 ? m.instrumentalSkill : singerPart ? m.technicalSkill - .15f : m.technicalSkill;
		float skill = Math.Clamp(.5f + hands, .5f, 1.3f);
		float u = BandLife.Unit(key + "|instrument");
		foreach (var (role, share) in Doubles(m, genre, family)) {
			float s = share * skill;
			if (u < s) return role;
			u -= s;
		}
		return MusicianRole.None;
	}

	private static float HarmonyShare(Genre? genre, GenreFamily? family) => genre switch {
		Genre.SurfRock => .65f,
		Genre.BritishInvasion => .60f,
		Genre.Folk or Genre.FolkRock or Genre.Skiffle => .55f,
		Genre.Country or Genre.CountryRock => .45f,
		Genre.RockAndRoll or Genre.TeenPop => .45f,
		Genre.GarageRock => .40f,
		Genre.DooWop or Genre.GirlGroup or Genre.Motown => .40f,
		Genre.Soul or Genre.RnB or Genre.Funk => .25f,
		Genre.Blues => .15f,
		Genre.Jazz or Genre.EasyListening => .08f,
		_ => family switch {
			GenreFamily.Rock or GenreFamily.Pop => .40f,
			GenreFamily.Folk or GenreFamily.Country or GenreFamily.Gospel => .45f,
			GenreFamily.RhythmAndSoul => .25f,
			GenreFamily.Blues => .15f,
			GenreFamily.Jazz or GenreFamily.Classical or GenreFamily.NonMusic => .08f,
			_ => .30f
		}
	};

	private static float RoleSingShare(MusicianRole role) => role switch {
		MusicianRole.Drums or MusicianRole.Percussion => .6f,
		MusicianRole.Harmonica => .5f,
		MusicianRole.Saxophone or MusicianRole.Trumpet => .3f,
		_ => 1f
	};

	/// <summary>What a part commonly doubles on, and how often (before scaling by how well the person plays).</summary>
	private static (MusicianRole role, float share)[] Doubles(Musician m, Genre? genre, GenreFamily? family) {
		bool folkish = family is GenreFamily.Folk || genre is Genre.Folk or Genre.FolkRock or Genre.Skiffle;
		bool country = family is GenreFamily.Country;
		bool blues = family is GenreFamily.Blues;
		bool soulful = family is GenreFamily.RhythmAndSoul or GenreFamily.Gospel;
		bool jazz = family is GenreFamily.Jazz;
		if (m.isLeadVocalist || m.primaryRole == MusicianRole.LeadVocals) {
			if (folkish) return new[] { (MusicianRole.RhythmGuitar, .70f), (MusicianRole.Harmonica, .08f) };
			if (country) return new[] { (MusicianRole.RhythmGuitar, .55f), (MusicianRole.Piano, .05f) };
			if (blues) return new[] { (MusicianRole.RhythmGuitar, .40f), (MusicianRole.Harmonica, .20f), (MusicianRole.Piano, .08f) };
			if (soulful) return new[] { (MusicianRole.Piano, .18f), (MusicianRole.RhythmGuitar, .05f) };
			if (jazz) return new[] { (MusicianRole.Piano, .20f) };
			if (family is GenreFamily.Rock) return new[] { (MusicianRole.RhythmGuitar, .35f), (MusicianRole.Piano, .08f) };
			return new[] { (MusicianRole.Piano, .10f), (MusicianRole.RhythmGuitar, .08f) };
		}
		return m.primaryRole switch {
			MusicianRole.BackingVocals => new[] { (MusicianRole.Piano, .06f), (MusicianRole.RhythmGuitar, folkish ? .45f : .05f) },
			MusicianRole.Piano => new[] { (MusicianRole.Organ, soulful ? .45f : .35f) },
			MusicianRole.Organ => new[] { (MusicianRole.Piano, .60f) },
			MusicianRole.LeadGuitar => new[] { (MusicianRole.RhythmGuitar, .30f), (MusicianRole.Bass, .05f) },
			MusicianRole.RhythmGuitar => new[] { (MusicianRole.LeadGuitar, .20f), (MusicianRole.Harmonica, folkish || blues ? .15f : .02f) },
			MusicianRole.Bass => new[] { (MusicianRole.RhythmGuitar, .10f) },
			MusicianRole.Drums => new[] { (MusicianRole.Percussion, .30f) },
			MusicianRole.Saxophone => new[] { (MusicianRole.Trumpet, .05f), (MusicianRole.Percussion, .10f) },
			MusicianRole.Trumpet => new[] { (MusicianRole.Percussion, .10f) },
			MusicianRole.Violin => new[] { (MusicianRole.RhythmGuitar, country ? .40f : .15f) },
			MusicianRole.Harmonica => new[] { (MusicianRole.RhythmGuitar, .35f) },
			MusicianRole.Percussion => new[] { (MusicianRole.Drums, .25f) },
			_ => Array.Empty<(MusicianRole, float)>()
		};
	}
}
