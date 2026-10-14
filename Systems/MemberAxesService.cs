using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Phase 1b of SimTools/BandMemberSimulationDirective.md: split skill axes, ceilings and sight-reading.
/// <para>
/// <b>Anchored, keyed, unread.</b> <see cref="Musician.technicalSkill"/> stays the stored, authoritative value.
/// The new axes are generated AROUND it (a singer's vocal axes centre on it, an instrumentalist's instrumental
/// axis does), every draw is keyed on the world seed and the person id (§2.8: drawing five new axes from the
/// population stream would reshuffle every draw after it), and nothing existing reads them. Byte identity is
/// proved by probe hash. The first reader to switch is PolarActProfileDeriver, and only behind its own flag
/// (Phase 5b), because moving it moves material fit.
/// </para>
/// <para>
/// <b>Ceilings are separate from ability and only weakly tied to it</b> (§4.13): most people are already at
/// theirs, a long tail is not, and youth is where the tail lives. Growth (Phase 5) closes the gap from the
/// formation value toward the ceiling with work; scouting reads evidence correlated with the ceiling, never
/// the ceiling itself.
/// </para>
/// </summary>
public static class MemberAxesService {
	public const int CurrentAxesVersion = 1;

	/// <summary>Generates axes for every member who doesn't have them yet. Idempotent; keyed; touches no stream.</summary>
	public static void EnsureAxes(SimulatedArtist artist, int year) {
		if (!BandLife.MemberAxesEnabled || artist?.members == null) return;
		foreach (Musician m in artist.members) EnsureAxes(m, artist, year);
	}

	public static void EnsureAxes(Musician m, SimulatedArtist artist, int year) {
		if (!BandLife.MemberAxesEnabled || m == null || m.axesVersion >= CurrentAxesVersion) return;
		Generate(m, artist?.primaryGenre ?? Genre.TraditionalPop, year);
	}

	/// <summary>The generation itself, without the flag gate, so a probe can exercise the real rule.</summary>
	internal static void Generate(Musician m, Genre genre, int year) {
		string k = $"axes|{m.personId}";
		float t = m.technicalSkill;
		bool singer = IsSinger(m);
		if (singer) {
			m.vocalControl = Clamp01(t + 0.08f * BandLife.Normal(k + "|vc"));
			m.vocalPower = Clamp01(t + 0.10f * BandLife.Normal(k + "|vp") + 0.10f * (m.stagePresence - 0.42f));
			m.diction = Clamp01(0.55f * t + 0.45f * (0.5f * m.creativity + 0.5f * m.musicalVersatility) + 0.08f * BandLife.Normal(k + "|di"));
			// Most singers play a little; few play like the band's instrumentalists.
			m.instrumentalSkill = Clamp01(t - 0.15f + 0.15f * BandLife.Normal(k + "|in"));
		} else {
			m.instrumentalSkill = Clamp01(t + 0.05f * BandLife.Normal(k + "|in"));
			m.vocalPower = Clamp01(0.30f + 0.35f * (t - 0.45f) + 0.12f * BandLife.Normal(k + "|vp"));
			m.vocalControl = Clamp01(0.30f + 0.35f * (t - 0.45f) + 0.12f * BandLife.Normal(k + "|vc"));
			m.diction = Clamp01(0.35f + 0.20f * m.creativity + 0.10f * BandLife.Normal(k + "|di"));
		}

		int age = Mathf.Max(14, m.GetAge(year));
		float youth = Mathf.Clamp((30f - age) / 12f, 0f, 1f);
		// A shared "late bloomer" draw ties the two ceilings together a little (some people simply have more
		// in them), and each axis then draws its own share of the tail.
		float bloom = BandLife.Unit(k + "|bloom");
		m.ceilingInstrumental = Clamp01(m.instrumentalSkill + Headroom(m.instrumentalSkill, youth, bloom, BandLife.Unit(k + "|ci"), BandLife.Unit(k + "|gi")));
		float vocalNow = Mathf.Max(m.vocalPower, m.vocalControl);
		m.ceilingVocal = Clamp01(vocalNow + Headroom(vocalNow, youth, bloom, BandLife.Unit(k + "|cv"), BandLife.Unit(k + "|gv")));
		m.developmentRate = Mathf.Clamp(Mathf.Exp(0.30f * BandLife.Normal(k + "|rate")), 0.45f, 2.0f);

		m.formationInstrumental = m.instrumentalSkill;
		m.formationVocalPower = m.vocalPower;
		m.formationVocalControl = m.vocalControl;
		m.formationDiction = m.diction;
		m.formationTechnical = m.technicalSkill;

		m.sightReading = Clamp01(0.20f + 0.50f * m.instrumentalSkill + RoleReadingBonus(m.primaryRole) +
			GenreReadingBonus(genre) + 0.10f * BandLife.Normal(k + "|sr"));
		m.axesVersion = CurrentAxesVersion;
	}

	/// <summary>
	/// Headroom above today's skill. A gate puts most people at their ceiling already (more so the older they
	/// are); past it, headroom is a power-law share of what is left below 1, so the tail is long and thin.
	/// </summary>
	private static float Headroom(float current, float youth, float bloom, float tail, float gate) {
		float atCeilingShare = 0.62f - 0.30f * youth;
		if (gate < atCeilingShare) return 0.02f * tail;
		float shape = Mathf.Pow(Mathf.Max(tail, bloom * 0.8f), 2.6f);
		return (1f - current) * shape * (0.35f + 0.65f * youth);
	}

	public static bool IsSinger(Musician m) => m.isLeadVocalist ||
		m.primaryRole is MusicianRole.LeadVocals or MusicianRole.BackingVocals;

	private static float RoleReadingBonus(MusicianRole role) => role switch {
		MusicianRole.Piano or MusicianRole.Organ or MusicianRole.Saxophone or MusicianRole.Trumpet or MusicianRole.Violin => 0.10f,
		MusicianRole.LeadVocals or MusicianRole.BackingVocals => -0.15f,
		MusicianRole.Harmonica => -0.10f,
		_ => 0f
	};

	private static float GenreReadingBonus(Genre genre) => genre switch {
		Genre.Jazz or Genre.Classical or Genre.EasyListening or Genre.TraditionalPop or Genre.BossaNova => 0.12f,
		Genre.Country or Genre.Blues or Genre.Folk or Genre.RockAndRoll or Genre.GarageRock or Genre.SurfRock
			or Genre.ProtoPunk => -0.05f,
		_ => 0f
	};

	private static float Clamp01(float v) => Mathf.Clamp(v, 0f, 1f);
}
