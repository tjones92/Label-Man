using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class ArtistManager : Node {
	private static readonly (float Upper, Genre Genre)[] InitialGeneralGenreBands = {
		(.18f, Genre.RockAndRoll), (.32f, Genre.RnB), (.42f, Genre.TraditionalPop), (.50f, Genre.DooWop),
		(.58f, Genre.Soul), (.64f, Genre.Country), (.69f, Genre.Jazz), (.74f, Genre.Gospel),
		(.79f, Genre.TeenPop), (.84f, Genre.Folk), (.88f, Genre.GirlGroup), (.92f, Genre.Motown),
		(.95f, Genre.SurfRock), (1f, Genre.BluesRock)
	};
	private static readonly (float Upper, Genre Genre)[] InitialVocalGenreBands = {
		(.35f, Genre.DooWop), (.55f, Genre.GirlGroup), (.75f, Genre.Motown),
		(.85f, Genre.Soul), (.92f, Genre.RnB), (1f, Genre.Gospel)
	};
	public static ArtistManager Instance { get; private set; }
	
	[ExportGroup("Configuration")]
	internal const int LegacyInitialPoolSize = 3000;
	internal const int EnabledLifecycleInitialPoolSize = 7000;
	[Export] private int initialPoolSize = LegacyInitialPoolSize;
	[Export] private int enabledLifecycleInitialPoolSize = EnabledLifecycleInitialPoolSize;
	[Export] private float maleArtistRatio = 0.72f;
	
	[ExportGroup("Debug")]
	[Export] private bool debugMode = false;
	
	private Dictionary<string, SimulatedArtist> artistRegistry = new Dictionary<string, SimulatedArtist>();
	private Dictionary<string, Musician> musicianRegistry = new Dictionary<string, Musician>();
	
	private int artistIdCounter = 0;
	private int musicianIdCounter = 0;
	
	private List<SimulatedArtist> unsignedArtists = new List<SimulatedArtist>();
	private const int AnnualRuntimeFormationCount = 300;
	private const int NominalRuntimeFormationWeeks = 52;
	internal const int ExperiencedComebackStartYear = 1961;
	// Directive 6 authorized one-variable candidate after the C=26 Gate C
	// capacity failure. All probation, formation, scouting, and exit constants
	// remain frozen while this cooldown candidate is evaluated.
	public const int PerformanceDropCooldownWeeks = 13;
	public const int RepeatPerformanceDropCooldownWeeks = 52;
	private const int InactivityHorizonWeeks = 78;
	private const int TerminalInactivityWeeks = 52;
	private const int MinimumSoloRetirementAge = 35;
	private readonly Dictionary<Genre, int> recentRuntimeFormationCounts = new();
	private RandomNumberGenerator populationRng;
	private float formationAccumulator;
	private int formationYear = -1;
	private int formedThisWeek;
	private int formedYtd;
	private bool generatingRuntimePopulation;
	private bool generatingInitialReserve;
	private bool UsesPopulationRng => generatingRuntimePopulation || generatingInitialReserve;

	public int FormedThisWeek => formedThisWeek;
	public int FormedYtd => formedYtd;
	public IReadOnlyCollection<SimulatedArtist> GetAllArtists() => artistRegistry.Values;
	public event System.Action<string, SimulatedArtist> OnPopulationEvent;
	private void EmitPopulationEvent(string eventType, SimulatedArtist artist) {
		if (ArtistPopulationLifecycle.Enabled && artist != null) OnPopulationEvent?.Invoke(eventType, artist);
	}
	
	public override void _EnterTree() {
		if (Instance != null && Instance != this) {
			QueueFree();
			return;
		}
		Instance = this;
	}

	public override void _Ready() {
		if (ChartManager.Instance != null) {
			ChartManager.Instance.OnRecordChartUpdated += OnRecordChartUpdated;
			ChartManager.Instance.OnRecordLeftChart += OnRecordLeftChart;
		}
	}

	public override void _ExitTree() {
		if (ChartManager.Instance != null) {
			ChartManager.Instance.OnRecordChartUpdated -= OnRecordChartUpdated;
			ChartManager.Instance.OnRecordLeftChart -= OnRecordLeftChart;
		}
	}

private void OnRecordChartUpdated(RecordRuntimeData record) {
	if (record?.baseRecord == null) return;
	var artist = GetArtist(record.baseRecord.artistId);
	if (artist == null) return;

	if (record.peakPosition > 0 && !record.artistChartEntryCredited) {
		artist.RegisterChartEntry();
		record.artistChartEntryCredited = true;
	}
	if (record.peakPosition > 0 && record.peakPosition <= 40 && !record.artistTop40Credited) {
		artist.RegisterTop40Hit(CreditsCurrentContract(record, artist));
		record.artistTop40Credited = true;
	}
	if (record.peakPosition > 0 && record.peakPosition <= 10 && !record.artistTop10Credited) {
		artist.RegisterTop10Hit();
		record.artistTop10Credited = true;
	}
	if (record.peakPosition == 1 && !record.artistNumberOneCredited) {
		artist.RegisterNumberOne();
		record.artistNumberOneCredited = true;
	}
}



	private void OnRecordLeftChart(RecordRuntimeData record) {
		if (record?.baseRecord == null) return;

		var artist = GetArtist(record.baseRecord.artistId);
		if (artist == null) return;

		if (record.artistChartRunCompleted) return;
		artist.CompleteChartRun(record.peakPosition, record.weeksOnChart, record.totalUnitsSold,
			CreditsCurrentContract(record, artist));
		record.artistChartRunCompleted = true;
		if (GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)
			RosterManager.Instance?.RecordChartRunComplete(artist, record);
	}

	internal static bool CreditsCurrentContract(RecordRuntimeData record, SimulatedArtist artist) =>
		!ArtistPopulationLifecycle.Enabled || (record != null && artist != null &&
			record.artistContractSequenceAtRelease == artist.contractSequence);
	
	public void GenerateInitialPool(int year) {
		GD.Print($"ArtistManager: Generating launch roster pool of {initialPoolSize} artists...");
		GenerateInitialArtists(initialPoolSize, year);
		
		if (debugMode) PrintPoolStats();
		GD.Print($"ArtistManager: Generated {artistRegistry.Count} artists with {musicianRegistry.Count} musicians");
	}

	private void GenerateInitialArtists(int count, int year) {
		int soloMales = Mathf.RoundToInt(count * 0.25f);
		int soloFemales = Mathf.RoundToInt(count * 0.12f);
		int bands = Mathf.RoundToInt(count * 0.40f);
		int vocalGroups = Mathf.RoundToInt(count * 0.18f);
		int duos = count - soloMales - soloFemales - bands - vocalGroups;
		
		for (int i = 0; i < soloMales; i++) unsignedArtists.Add(GenerateArtist(ArtistType.SoloMale, GetRandomGenre(), year, null));
		for (int i = 0; i < soloFemales; i++) unsignedArtists.Add(GenerateArtist(ArtistType.SoloFemale, GetRandomGenre(), year, null));
		for (int i = 0; i < bands; i++) unsignedArtists.Add(GenerateArtist(ArtistType.Band, GetRandomGenre(), year, null));
		for (int i = 0; i < vocalGroups; i++) unsignedArtists.Add(GenerateArtist(ArtistType.VocalGroup, GetVocalGroupGenre(), year, null));
		for (int i = 0; i < duos; i++) unsignedArtists.Add(GenerateArtist(ArtistType.Duo, GetRandomGenre(), year, null));
	}

	public void MaterializeEnabledInitialUnsignedReserve(int year) {
		if (!ArtistPopulationLifecycle.Enabled) return;
		int reserveCount = Mathf.Max(0, enabledLifecycleInitialPoolSize - artistRegistry.Count);
		if (reserveCount == 0) return;
		EnsurePopulationRng();
		generatingInitialReserve = true;
		try { GenerateInitialArtists(reserveCount, year); }
		finally { generatingInitialReserve = false; }
		GD.Print($"ArtistManager: Added {reserveCount} isolated-RNG unsigned reserve artists; " +
			$"market total={artistRegistry.Count}, unsigned={unsignedArtists.Count}");
	}

	internal static int GetDefaultInitialPoolSizeForPath(bool enabledLifecyclePath) =>
		enabledLifecyclePath ? EnabledLifecycleInitialPoolSize : LegacyInitialPoolSize;
	
	public SimulatedArtist GenerateArtist(ArtistType type, Genre genre, int year, string region) {
		artistIdCounter++;
		string id = $"artist_{artistIdCounter:D5}";
		Genre primaryGenre;
		Genre secondaryGenre;
		if (GenreMarketV2.Enabled) {
			(primaryGenre, secondaryGenre) = CanonicalizeEnabledInitialGenres(genre, year, GetRelatedGenre);
		} else {
			primaryGenre = genre;
			secondaryGenre = GetRelatedGenre(genre);
		}
		
		var artist = new SimulatedArtist {
			artistId = id,
			type = type,
			primaryGenre = primaryGenre,
			secondaryGenre = secondaryGenre,
			homeRegion = region ?? GetRandomRegion(),
			formedYear = generatingRuntimePopulation ? year : year - RandInt(0, 5),
			careerState = CareerState.Unsigned,
			cohort = generatingRuntimePopulation ? ArtistCohort.RuntimeFormation : ArtistCohort.InitialLegacy,
			formationPrimaryGenre = primaryGenre,
			formationSecondaryGenre = secondaryGenre,
			lifecycleStatus = ArtistLifecycleStatus.Active
		};
		
		GenerateMembers(artist, type, primaryGenre, year);
		artist.stageName = type is ArtistType.SoloMale or ArtistType.SoloFemale
			? artist.members[0].FullName
			: GenerateStageName(type, primaryGenre, year);
		artist.RecalculateStats();
		
		artist.momentum = 0f;
		artist.reputation = RandRange(0f, 0.1f);
		
		artistRegistry[id] = artist;
		return artist;
	}
	
	private string GenerateStageName(ArtistType type, Genre genre, int year) {
		if (UsesPopulationRng)
			return type switch {
				ArtistType.Duo => $"The {genre} Pair {artistIdCounter}",
				ArtistType.VocalGroup => $"The {genre} Voices {artistIdCounter}",
				_ => $"The {genre} Group {artistIdCounter}"
			};
		if (NameGenerator.Instance != null) {
			return NameGenerator.Instance.GenerateArtistName(genre, year, type, null, LabelArchetype.RegionalHustler);
		}
		artistIdCounter++;
		return type switch {
			ArtistType.SoloMale or ArtistType.SoloFemale => $"Artist {artistIdCounter}",
			ArtistType.Duo => $"The Duo {artistIdCounter}",
			ArtistType.VocalGroup => $"The Vocals {artistIdCounter}",
			_ => $"The Band {artistIdCounter}"
		};
	}
	
	private void GenerateMembers(SimulatedArtist artist, ArtistType type, Genre genre, int year) {
		switch (type) {
			case ArtistType.SoloMale: GenerateSoloArtist(artist, true, year); break;
			case ArtistType.SoloFemale: GenerateSoloArtist(artist, false, year); break;
			case ArtistType.Duo: GenerateDuo(artist, genre, year); break;
			case ArtistType.Band: GenerateBand(artist, genre, year); break;
			case ArtistType.VocalGroup: GenerateVocalGroup(artist, genre, year); break;
		}
	}
	
	private void GenerateSoloArtist(SimulatedArtist artist, bool isMale, int year) {
		var musician = GenerateMusician(isMale, year);
		musician.primaryRole = MusicianRole.LeadVocals;
		musician.isLeadVocalist = true;
		musician.isPrimaryWriter = Randf() > 0.4f;
		musician.isBandLeader = true;
		musician.stagePresence = Mathf.Clamp(musician.stagePresence + 0.15f, 0f, 1f);
		musician.ego = Mathf.Clamp(musician.ego + 0.1f, 0f, 1f);
		musician.ambition = Mathf.Clamp(musician.ambition + 0.1f, 0f, 1f);
		artist.AddMember(musician, year, true);
	}
	
	private void GenerateDuo(SimulatedArtist artist, Genre genre, int year) {
		bool sameSex = Randf() > 0.3f;
		bool member1Male = Randf() > 0.35f;
		bool member2Male = sameSex ? member1Male : !member1Male;
		
		var member1 = GenerateMusician(member1Male, year);
		member1.primaryRole = MusicianRole.LeadVocals;
		member1.isLeadVocalist = true;
		member1.isPrimaryWriter = Randf() > 0.5f;
		
		var member2 = GenerateMusician(member2Male, year);
		member2.primaryRole = genre switch {
			Genre.Folk or Genre.Country => MusicianRole.RhythmGuitar,
			Genre.Jazz => MusicianRole.Piano,
			_ => MusicianRole.BackingVocals
		};
		member2.isPrimaryWriter = Randf() > 0.5f;
		
		if (Randf() > 0.5f) member1.isBandLeader = true;
		else member2.isBandLeader = true;
		
		artist.AddMember(member1, year, true);
		artist.AddMember(member2, year, true);
	}
	
	private void GenerateBand(SimulatedArtist artist, Genre genre, int year) {
		var lineup = GetBandLineup(genre);
		bool firstMember = true;
		
		foreach (var role in lineup) {
			bool isMale = role.isMale ?? (Randf() < maleArtistRatio);
			var musician = GenerateMusician(isMale, year);
			musician.primaryRole = role.role;
			musician.isLeadVocalist = role.isLead;
			musician.isBandLeader = firstMember && role.isLead;
			
			if (firstMember || (role.role == MusicianRole.LeadGuitar && Randf() > 0.5f)) {
				musician.isPrimaryWriter = Randf() > 0.3f;
			}
			
			artist.AddMember(musician, year, true);
			firstMember = false;
		}
	}
	
	private void GenerateVocalGroup(SimulatedArtist artist, Genre genre, int year) {
		bool isGirlGroup = genre == Genre.GirlGroup || (genre == Genre.Motown && Randf() > 0.6f);
		int memberCount = RandInt(3, 6);
		bool hasLeadDesignated = false;
		
		for (int i = 0; i < memberCount; i++) {
			bool isMale = !isGirlGroup && (Randf() < 0.85f);
			var musician = GenerateMusician(isMale, year);
			
			if (!hasLeadDesignated && Randf() > 0.3f) {
				musician.primaryRole = MusicianRole.LeadVocals;
				musician.isLeadVocalist = true;
				musician.isBandLeader = true;
				musician.stagePresence = Mathf.Clamp(musician.stagePresence + 0.2f, 0f, 1f);
				hasLeadDesignated = true;
			} else {
				musician.primaryRole = MusicianRole.BackingVocals;
			}
			artist.AddMember(musician, year, true);
		}
		
		if (!hasLeadDesignated && artist.members.Count > 0) {
			var bestSinger = artist.members.OrderByDescending(m => m.technicalSkill + m.stagePresence).First();
			bestSinger.primaryRole = MusicianRole.LeadVocals;
			bestSinger.isLeadVocalist = true;
			bestSinger.isBandLeader = true;
		}
		
		if (Randf() > 0.7f) {
			var smartest = artist.members.OrderByDescending(m => m.creativity).First();
			smartest.isPrimaryWriter = true;
		}
	}
	
	private Musician GenerateMusician(bool isMale, int currentYear) {
		musicianIdCounter++;
		string id = $"mus_{musicianIdCounter:D6}";
		string firstName, lastName;
		
		if (NameGenerator.Instance != null && !UsesPopulationRng) {
			(firstName, lastName) = NameGenerator.Instance.GeneratePersonName(isMale);
		} else {
			firstName = isMale ? $"John{musicianIdCounter}" : $"Jane{musicianIdCounter}";
			lastName = $"Doe{musicianIdCounter}";
		}
		
		int birthYear = currentYear - (Randf() < 0.85f ? RandInt(18, 29) : RandInt(29, 42));
		
		var musician = new Musician(id, firstName, lastName, isMale, birthYear);
		
		musician.technicalSkill = GenerateStat(0.45f, 0.22f);
		musician.creativity = GenerateStat(0.40f, 0.25f);
		musician.musicalVersatility = GenerateStat(0.45f, 0.20f);
		musician.stagePresence = GenerateStat(0.42f, 0.24f);
		musician.studioEfficiency = GenerateStat(0.50f, 0.20f);
		
		musician.ego = GenerateStat(0.40f, 0.22f);
		musician.ambition = GenerateStat(0.50f, 0.22f);
		musician.reliability = GenerateStat(0.65f, 0.20f);
		musician.loyalty = GenerateStat(0.60f, 0.22f);
		musician.temperament = GenerateStat(0.55f, 0.22f);
		
		musicianRegistry[id] = musician;
		return musician;
	}
	
	private float GenerateStat(float mean, float stdDev) {
		float u1 = Mathf.Max(.000001f, Randf());
		float u2 = Randf();
		float normal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.Pi * u2);
		return Mathf.Clamp(mean + normal * stdDev, 0f, 1f);
	}
	
	private List<(MusicianRole role, bool isLead, bool? isMale)> GetBandLineup(Genre genre) {
		return genre switch {
			Genre.RockAndRoll or Genre.BritishInvasion or Genre.GarageRock => new List<(MusicianRole, bool, bool?)> {
				(MusicianRole.LeadVocals, true, null), (MusicianRole.LeadGuitar, false, true), (MusicianRole.Bass, false, true), (MusicianRole.Drums, false, true)
			},
			Genre.SurfRock => new List<(MusicianRole, bool, bool?)> {
				(MusicianRole.LeadVocals, true, true), (MusicianRole.LeadGuitar, false, true), (MusicianRole.RhythmGuitar, false, true), (MusicianRole.Bass, false, true), (MusicianRole.Drums, false, true)
			},
			Genre.Soul or Genre.RnB or Genre.Motown => new List<(MusicianRole, bool, bool?)> {
				(MusicianRole.LeadVocals, true, null), (MusicianRole.Piano, false, null), (MusicianRole.Bass, false, true), (MusicianRole.Drums, false, true), (MusicianRole.Saxophone, false, true)
			},
			Genre.Jazz => new List<(MusicianRole, bool, bool?)> {
				(MusicianRole.LeadVocals, true, null), (MusicianRole.Piano, false, null), (MusicianRole.Bass, false, true), (MusicianRole.Drums, false, true), (MusicianRole.Saxophone, false, true), (MusicianRole.Trumpet, false, true)
			},
			Genre.Folk or Genre.FolkRock => new List<(MusicianRole, bool, bool?)> {
				(MusicianRole.LeadVocals, true, null), (MusicianRole.RhythmGuitar, false, null), (MusicianRole.Bass, false, true)
			},
			Genre.Country or Genre.CountryRock => new List<(MusicianRole, bool, bool?)> {
				(MusicianRole.LeadVocals, true, null), (MusicianRole.LeadGuitar, false, true), (MusicianRole.Bass, false, true), (MusicianRole.Drums, false, true), (MusicianRole.Violin, false, null)
			},
			Genre.Psychedelic or Genre.AcidRock or Genre.ProgressiveRock => new List<(MusicianRole, bool, bool?)> {
				(MusicianRole.LeadVocals, true, true), (MusicianRole.LeadGuitar, false, true), (MusicianRole.Organ, false, true), (MusicianRole.Bass, false, true), (MusicianRole.Drums, false, true)
			},
			Genre.BluesRock or Genre.HardRock => new List<(MusicianRole, bool, bool?)> {
				(MusicianRole.LeadVocals, true, true), (MusicianRole.LeadGuitar, false, true), (MusicianRole.Bass, false, true), (MusicianRole.Drums, false, true)
			},
			_ => new List<(MusicianRole, bool, bool?)> {
				(MusicianRole.LeadVocals, true, null), (MusicianRole.LeadGuitar, false, true), (MusicianRole.Bass, false, true), (MusicianRole.Drums, false, true)
			}
		};
	}
	
	private Genre GetRandomGenre() {
		return GetLegacyInitialGenreForProbe(Randf(), vocalGroup: false);
	}

	internal static Genre GetLegacyInitialGenreForProbe(float roll, bool vocalGroup) {
		foreach ((float upper, Genre genre) in vocalGroup ? InitialVocalGenreBands : InitialGeneralGenreBands)
			if (roll < upper) return genre;
		return (vocalGroup ? InitialVocalGenreBands : InitialGeneralGenreBands)[^1].Genre;
	}

	/// <summary>
	/// Exact expected primary-identity prior of the frozen 1960 pool. The general
	/// picker supplies 82% of artists and the vocal picker 18%; Girl Group's
	/// three-way secondary draw maps one-third to Soul and two-thirds to Teen Pop.
	/// This is a fixed prospective cohort, not a realized release-count input.
	/// </summary>
	internal static IReadOnlyDictionary<Genre, float> GetEnabledInitialPrimaryGenrePrior() {
		var result = new Dictionary<Genre, float>();
		AddInitialBandWeights(result, InitialGeneralGenreBands, .82f);
		AddInitialBandWeights(result, InitialVocalGenreBands, .18f);
		return result;
	}

	private static void AddInitialBandWeights(Dictionary<Genre, float> result,
		IReadOnlyList<(float Upper, Genre Genre)> bands, float cohortWeight) {
		float lower = 0f;
		foreach ((float upper, Genre legacy) in bands) {
			float weight = cohortWeight * (upper - lower);
			lower = upper;
			if (legacy == Genre.GirlGroup) {
				result[Genre.Soul] = result.GetValueOrDefault(Genre.Soul) + weight / 3f;
				result[Genre.TeenPop] = result.GetValueOrDefault(Genre.TeenPop) + weight * 2f / 3f;
				continue;
			}
			Genre canonical = GenreCatalog.MapLegacy(legacy, 1960);
			result[canonical] = result.GetValueOrDefault(canonical) + weight;
		}
	}

	private Genre GetVocalGroupGenre() {
		return GetLegacyInitialGenreForProbe(Randf(), vocalGroup: true);
	}

	/// <summary>Draws the legacy secondary once, then applies deterministic enabled-path migration.</summary>
	internal static (Genre Primary, Genre Secondary) CanonicalizeEnabledInitialGenres(
		Genre legacyPrimary, int year, Func<Genre, Genre> drawSecondary) {
		if (drawSecondary == null) throw new ArgumentNullException(nameof(drawSecondary));
		Genre primary = legacyPrimary;
		Genre secondary = drawSecondary(legacyPrimary);
		GenreMigration.Canonicalize(ref primary, ref secondary, year);
		return (primary, secondary);
	}
	
	private Genre GetRelatedGenre(Genre primary) {
		if (GenreMarketV2.Enabled && primary == Genre.Soul) return RandomPick(Genre.RnB, Genre.Gospel, Genre.TeenPop);
		return primary switch {
			Genre.RockAndRoll => RandomPick(Genre.RnB, Genre.TeenPop, Genre.BluesRock),
			Genre.RnB => RandomPick(Genre.Soul, Genre.DooWop, Genre.Gospel),
			Genre.Soul => RandomPick(Genre.RnB, Genre.Motown, Genre.Gospel),
			Genre.DooWop => RandomPick(Genre.RnB, Genre.TeenPop, Genre.Soul),
			Genre.TeenPop => RandomPick(Genre.RockAndRoll, Genre.DooWop, Genre.TraditionalPop),
			Genre.Country => RandomPick(Genre.Folk, Genre.RockAndRoll, Genre.Gospel),
			Genre.Folk => RandomPick(Genre.Country, Genre.FolkRock, Genre.TraditionalPop),
			Genre.Jazz => RandomPick(Genre.TraditionalPop, Genre.Soul, Genre.RnB),
			Genre.Gospel => RandomPick(Genre.Soul, Genre.RnB, Genre.Country),
			Genre.GirlGroup => RandomPick(Genre.Motown, Genre.TeenPop, Genre.Soul),
			Genre.Motown => RandomPick(Genre.Soul, Genre.RnB, Genre.GirlGroup),
			_ => Genre.TraditionalPop
		};
	}
	
	private Genre RandomPick(params Genre[] options) => options[RandInt(0, options.Length - 1)];
	
	private string GetRandomRegion() {
		string[] regions = { "East Coast", "Great Lakes", "Great Plains", "Deep South", "Southwest", "Rockies", "West Coast" };
		return regions[RandInt(0, regions.Length - 1)];
	}

	private float Randf() => UsesPopulationRng ? populationRng.Randf() : GD.Randf();
	private float RandRange(float min, float max) => UsesPopulationRng ? populationRng.RandfRange(min, max) : (float)GD.RandRange(min, max);
	private int RandInt(int min, int max) => UsesPopulationRng ? populationRng.RandiRange(min, max) : (int)GD.RandRange(min, max);

	/// <summary>Called only by ChartManager's explicit post-chart live sequence.</summary>
	public void AdvancePopulationLifecycle(GameDate date) {
		if (!ArtistPopulationLifecycle.IsLive) return;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		formedThisWeek = 0;
		if (formationYear != date.year) {
			formationYear = date.year;
			formationAccumulator = 0f;
			formedYtd = 0;
			recentRuntimeFormationCounts.Clear();
		}
		ReconcileLifecycleAndOwnership(date.year, week, advanceUnownedWeeks: true);
		ApplyLifecycleExits(date.year, week);
		MaterializeRuntimeFormation(date);
		ReconcileLifecycleAndOwnership(date.year, week, advanceUnownedWeeks: false);
	}

	private void MaterializeRuntimeFormation(GameDate date) {
		EnsurePopulationRng();
		int count = CalculateCalendarFormationCount(ref formationAccumulator, formedYtd);
		for (int i = 0; i < count; i++) {
			generatingRuntimePopulation = true;
			try {
				// This scope includes every runtime-formation decision.  In
				// particular, genre and artist-type selection must not consume the
				// global simulation stream before attribute generation begins.
				Genre primary = ChooseRuntimeFormationGenre(date.year);
				Genre secondary = ChooseRuntimeSecondaryGenre(primary, date.year);
				ArtistType type = ChooseRuntimeArtistType();
				SimulatedArtist artist = GenerateArtist(type, primary, date.year, null);
				artist.secondaryGenre = secondary;
				artist.formationSecondaryGenre = secondary;
				artist.formedYear = date.year;
				artist.cohort = ArtistCohort.RuntimeFormation;
				unsignedArtists.Add(artist);
				recentRuntimeFormationCounts[primary] = recentRuntimeFormationCounts.GetValueOrDefault(primary) + 1;
				formedThisWeek++;
				formedYtd++;
				EmitPopulationEvent("formation", artist);
			} finally {
				generatingRuntimePopulation = false;
			}
		}
	}

	internal static int CalculateCalendarFormationCount(ref float accumulator, int formedYtd) {
		int remaining = Mathf.Max(0, AnnualRuntimeFormationCount - formedYtd);
		if (remaining == 0) { accumulator = 0f; return 0; }
		accumulator += AnnualRuntimeFormationCount / (float)NominalRuntimeFormationWeeks;
		int count = Mathf.Min(remaining, Mathf.FloorToInt(accumulator + .00001f));
		accumulator -= count;
		return count;
	}

	private void EnsurePopulationRng() {
		if (populationRng != null) return;
		populationRng = new RandomNumberGenerator();
		ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
		populationRng.Seed = seed ^ 0x617274697374706fUL; // "artistpo", stable namespace
	}

	private ArtistType ChooseRuntimeArtistType() {
		float roll = Randf();
		return roll < .25f ? ArtistType.SoloMale : roll < .37f ? ArtistType.SoloFemale :
			roll < .77f ? ArtistType.Band : roll < .95f ? ArtistType.VocalGroup : ArtistType.Duo;
	}

	private Genre ChooseRuntimeFormationGenre(int year) {
		IReadOnlyList<Genre> genres = GenreSupplyService.GetAvailableGenres(year);
		if (genres.Count == 0) return Genre.TraditionalPop;
		float total = genres.Sum(genre => GenreSupplyService.GetSupplyWeight(genre, null, null, null, year,
			recentRuntimeFormationCounts, recentRuntimeFormationCounts));
		float target = Randf() * total;
		foreach (Genre genre in genres) {
			target -= GenreSupplyService.GetSupplyWeight(genre, null, null, null, year, recentRuntimeFormationCounts, recentRuntimeFormationCounts);
			if (target <= 0f) return genre;
		}
		return genres[^1];
	}

	private Genre ChooseRuntimeSecondaryGenre(Genre primary, int year) {
		IReadOnlyList<Genre> available = GenreSupplyService.GetAvailableGenres(year);
		Genre[] related = available.Where(candidate => candidate != primary &&
			GenreMarketMomentumService.GetAdjacency(primary, candidate) > 0f).ToArray();
		if (related.Length == 0) related = available.Where(candidate => candidate != primary).ToArray();
		return related.Length == 0 ? primary : related[RandInt(0, related.Length - 1)];
	}

	private void ReconcileLifecycleAndOwnership(int year, int week, bool advanceUnownedWeeks) {
		foreach (SimulatedArtist artist in artistRegistry.Values) {
			if (artist.lifecycleStatus != ArtistLifecycleStatus.Active) {
				artist.isActive = false;
				artist.labelId = null;
				unsignedArtists.RemoveAll(candidate => candidate == artist);
				foreach (AILabel label in ChartManager.Instance?.GetAllLabels() ?? new List<AILabel>()) label.roster?.RemoveAll(candidate => candidate == artist);
				continue;
			}
			artist.isActive = true;
			if (string.IsNullOrEmpty(artist.labelId)) {
				if (advanceUnownedWeeks) artist.weeksContinuouslyUnowned++;
			} else {
				artist.weeksContinuouslyUnowned = 0;
			}
		}
		ReconcileEnabledUnsignedPool();
	}

	private void ApplyLifecycleExits(int year, int week) {
		foreach (SimulatedArtist artist in artistRegistry.Values.Where(candidate => candidate.lifecycleStatus == ArtistLifecycleStatus.Active &&
			string.IsNullOrEmpty(candidate.labelId) && HasPriorContractForInactivityExit(candidate) &&
			candidate.weeksContinuouslyUnowned >= InactivityHorizonWeeks).ToArray()) {
			if (HasLiveRecordOrPendingProject(artist)) continue;
			artist.lifecycleStatus = ArtistLifecycleStatus.Inactive;
			artist.inactiveSinceWeek = week;
			artist.isActive = false;
			artist.careerEvents.Add($"{year}: Became inactive after {artist.weeksContinuouslyUnowned} unowned weeks");
			unsignedArtists.RemoveAll(candidate => candidate == artist);
			EmitPopulationEvent("inactivity", artist);
		}
		foreach (SimulatedArtist artist in artistRegistry.Values.Where(candidate => candidate.lifecycleStatus == ArtistLifecycleStatus.Inactive &&
			candidate.inactiveSinceWeek >= 0 && week - candidate.inactiveSinceWeek >= TerminalInactivityWeeks).ToArray()) {
			bool group = artist.type is ArtistType.Band or ArtistType.Duo or ArtistType.Trio or ArtistType.VocalGroup;
			Musician lead = artist.GetLeadSinger() ?? artist.members.FirstOrDefault(member => member.isActive);
			if (!group && (lead == null || lead.GetAge(year) < MinimumSoloRetirementAge)) continue;
			artist.lifecycleStatus = group ? ArtistLifecycleStatus.Disbanded : ArtistLifecycleStatus.Retired;
			artist.careerState = group ? CareerState.Disbanded : CareerState.Retired;
			artist.disbandReason = "Lifecycle inactivity";
			foreach (Musician member in artist.members.Where(member => member.isActive)) { member.isActive = false; member.reasonLeft = artist.lifecycleStatus.ToString(); }
			artist.careerEvents.Add($"{year}: {artist.lifecycleStatus} after prolonged inactivity");
			EmitPopulationEvent(artist.lifecycleStatus == ArtistLifecycleStatus.Retired ? "retirement" : "disbandment", artist);
		}
	}
	internal static bool HasPriorContractForInactivityExit(SimulatedArtist artist) => artist?.contractSequence > 0;

	private static bool HasLiveRecordOrPendingProject(SimulatedArtist artist) =>
		IsExitDeferred(
			ChartManager.Instance?.GetAllRecords().Any(record => record?.baseRecord?.artistId == artist.artistId && !record.artistChartRunCompleted) == true,
			CompetitorManager.Instance?.HasPendingProjectForArtist(artist.artistId) == true);
	internal static bool IsExitDeferredForProbe(bool hasLiveChartCallback, bool hasPendingAlbumProject) => IsExitDeferred(hasLiveChartCallback, hasPendingAlbumProject);
	private static bool IsExitDeferred(bool hasLiveChartCallback, bool hasPendingAlbumProject) => hasLiveChartCallback || hasPendingAlbumProject;
	
	public SimulatedArtist GetArtist(string artistId) => artistRegistry.TryGetValue(artistId, out var artist) ? artist : null;

	public ArtistPublicProfile GetPublicProfile(string artistId) {
		var artist = GetArtist(artistId);
		if (artist == null) return null;
		var records = ChartManager.Instance?.GetAllRecords()
			.Where(r => r?.baseRecord?.artistId == artistId).ToList() ?? new List<RecordRuntimeData>();
		var profile = new ArtistPublicProfile {
			artistId = artist.artistId, name = artist.stageName, artistType = artist.type,
			isBand = artist.type is ArtistType.Band or ArtistType.Duo or ArtistType.Trio or ArtistType.VocalGroup,
			homeRegion = artist.homeRegion, primaryGenre = artist.primaryGenre, secondaryGenre = artist.secondaryGenre,
			formedYear = artist.formedYear, careerState = artist.careerState, labelId = artist.labelId,
			labelName = ChartManager.Instance?.GetLabelName(artist.labelId) ?? "Independent",
			totalCharted = artist.charted, top40Hits = artist.top40Hits, top10Hits = artist.top10Hits,
			numberOneHits = artist.numberOnes, totalRecordsReleased = artist.totalReleases,
			highestPosition = records.Where(r => r.peakPosition > 0).Select(r => r.peakPosition).DefaultIfEmpty(0).Min(),
			totalWeeksOnChart = records.Sum(r => r.weeksOnChart)
		};
		profile.personnel = artist.members.Select(m => new ArtistPersonnelProfile {
			name = m.FullName, role = m.primaryRole, joinedYear = m.joinedYear,
			isFoundingMember = m.isFoundingMember, isActive = m.isActive, reasonLeft = m.reasonLeft
		}).ToList();
		if (artist.numberOnes > 0) profile.reputationTags.Add(ReputationTag.HitMachine);
		if (artist.careerState >= CareerState.Established) profile.reputationTags.Add(ReputationTag.Established);
		if (artist.momentum > 0.5f) profile.reputationTags.Add(ReputationTag.RisingStar);
		return profile;
	}
	public Musician GetMusician(string musicianId) => musicianRegistry.TryGetValue(musicianId, out var musician) ? musician : null;
	public List<SimulatedArtist> GetUnsignedArtists() => unsignedArtists.Where(IsEligibleUnsignedCandidate).ToList();
	public bool IsEligibleForPopulationSigning(SimulatedArtist artist, int currentWeek) =>
		IsEligibleUnsignedCandidate(artist) && artist.lifecycleStatus == ArtistLifecycleStatus.Active &&
		!IsPopulationCooldownBlocked(artist, currentWeek);
	public int GetWeeksSincePerformanceDrop(SimulatedArtist artist, int currentWeek) => artist?.lastPerformanceDropWeek >= 0 ? currentWeek - artist.lastPerformanceDropWeek : -1;
	internal static bool IsPopulationCooldownBlockedForProbe(SimulatedArtist artist, int currentWeek) => IsPopulationCooldownBlocked(artist, currentWeek);
	internal static bool IsEligibleForPopulationSigningForProbe(SimulatedArtist artist, int currentWeek) =>
		IsEligibleUnsignedCandidate(artist) && artist.lifecycleStatus == ArtistLifecycleStatus.Active && !IsPopulationCooldownBlocked(artist, currentWeek);
	internal static ArtistLifecycleStatus ClassifyTerminalLifecycleForProbe(SimulatedArtist artist, int year) =>
		artist.type is ArtistType.Band or ArtistType.Duo or ArtistType.Trio or ArtistType.VocalGroup ? ArtistLifecycleStatus.Disbanded :
		((artist.GetLeadSinger() ?? artist.members.FirstOrDefault(member => member.isActive))?.GetAge(year) ?? 0) >= MinimumSoloRetirementAge
			? ArtistLifecycleStatus.Retired : ArtistLifecycleStatus.Inactive;
	internal static int GetPerformanceDropCooldownWeeks(SimulatedArtist artist) =>
		artist?.usesRepeatPerformanceRecovery == true ? RepeatPerformanceDropCooldownWeeks : PerformanceDropCooldownWeeks;
	private static bool IsPopulationCooldownBlocked(SimulatedArtist artist, int currentWeek) => artist?.careerState == CareerState.Dropped &&
		artist.lastDropReason == ArtistDropReason.Performance && artist.lastPerformanceDropWeek >= 0 &&
		currentWeek - artist.lastPerformanceDropWeek < GetPerformanceDropCooldownWeeks(artist);

	internal static SimulatedArtist GenerateRuntimeArtistForProbe(int year, ulong seed) {
		var manager = new ArtistManager {
			populationRng = new RandomNumberGenerator { Seed = seed ^ 0x617274697374706fUL },
			generatingRuntimePopulation = true
		};
		try {
			return manager.GenerateRuntimeArtistForProbeCore(year);
		} finally { manager.generatingRuntimePopulation = false; }
	}
	internal static IReadOnlyList<SimulatedArtist> GenerateRuntimeArtistsForProbe(int year, ulong seed, int count) {
		var manager = new ArtistManager {
			populationRng = new RandomNumberGenerator { Seed = seed ^ 0x617274697374706fUL },
			generatingRuntimePopulation = true
		};
		try {
			var artists = new List<SimulatedArtist>();
			for (int i = 0; i < count; i++) artists.Add(manager.GenerateRuntimeArtistForProbeCore(year));
			return artists;
		} finally { manager.generatingRuntimePopulation = false; }
	}
	internal static Genre ChooseRuntimeSecondaryGenreForProbe(Genre primary, int year, ulong seed) {
		var manager = new ArtistManager { populationRng = new RandomNumberGenerator { Seed = seed }, generatingRuntimePopulation = true };
		try { return manager.ChooseRuntimeSecondaryGenre(primary, year); }
		finally { manager.generatingRuntimePopulation = false; }
	}
	private SimulatedArtist GenerateRuntimeArtistForProbeCore(int year) {
		Genre primary = ChooseRuntimeFormationGenre(year);
		Genre secondary = ChooseRuntimeSecondaryGenre(primary, year);
		SimulatedArtist artist = GenerateArtist(ChooseRuntimeArtistType(), primary, year, null);
		artist.secondaryGenre = secondary;
		artist.formationSecondaryGenre = secondary;
		recentRuntimeFormationCounts[primary] = recentRuntimeFormationCounts.GetValueOrDefault(primary) + 1;
		return artist;
	}
	
	public List<SimulatedArtist> GetUnsignedByGenre(Genre genre) {
		return unsignedArtists.Where(a => IsEligibleUnsignedCandidate(a) &&
			(a.primaryGenre == genre || a.secondaryGenre == genre)).ToList();
	}
	
	public List<SimulatedArtist> GetTopUnsignedTalent(int count, Genre? preferredGenre = null) {
		var pool = preferredGenre.HasValue ? GetUnsignedByGenre(preferredGenre.Value) : GetUnsignedArtists();
		return pool.OrderByDescending(a => a.CalculateBaseQuality()).Take(count).ToList();
	}

	public int GetEnabledFreeAgentPoolSize() => unsignedArtists.Count(IsEligibleUnsignedCandidate);
	public int GetEnabledPoolOwnershipConflicts() => unsignedArtists.Count(artist => artist != null && !string.IsNullOrEmpty(artist.labelId));
	public int GetEnabledDuplicatePoolEntries() {
		var seen = new HashSet<SimulatedArtist>();
		return unsignedArtists.Count(artist => artist != null && !seen.Add(artist));
	}

	/// <summary>Live-path integrity sweep: a pool entry is unique and unowned.</summary>
	public void ReconcileEnabledUnsignedPool() {
		var seen = new HashSet<SimulatedArtist>();
		unsignedArtists.RemoveAll(artist => !IsEligibleUnsignedCandidate(artist) || !seen.Add(artist));
	}
	
	public void SignArtist(SimulatedArtist artist, string labelId, int year) {
		bool reSigning = artist?.careerState == CareerState.Dropped;
		ReconcileSignedArtist(artist, unsignedArtists, labelId, year, ChartManager.Instance?.GetCurrentChartWeek() ?? 0);
		EmitPopulationEvent(reSigning ? "re-signing" : "signing", artist);
	}

	internal static void ReconcileSignedArtistForProbe(SimulatedArtist artist, List<SimulatedArtist> unsignedPool,
		string labelId, int year) => ReconcileSignedArtist(artist, unsignedPool, labelId, year, 0);

	internal static bool IsEligibleUnsignedCandidateForProbe(SimulatedArtist artist) => IsEligibleUnsignedCandidate(artist);

	private static bool IsEligibleUnsignedCandidate(SimulatedArtist artist) => artist != null &&
		(artist.careerState == CareerState.Unsigned || artist.careerState == CareerState.Dropped) &&
		artist.isActive && string.IsNullOrEmpty(artist.labelId);

	private static void ReconcileSignedArtist(SimulatedArtist artist, List<SimulatedArtist> unsignedPool,
		string labelId, int year, int currentWeek) {
		if (artist == null || unsignedPool == null) return;
		// AILabel applies commercial terms before this atomic ownership seam and
		// may already have changed Unsigned to NewSigning. Pool membership is the
		// authoritative indication that this is a new free-agent contract cycle.
		bool droppedFreeAgent = artist.careerState == CareerState.Dropped;
		bool freeAgentSigning = unsignedPool.Contains(artist) || droppedFreeAgent;
		int priorContractCount = artist.contractSequence;
		artist.labelId = labelId;
		artist.signedYear = year;
		artist.weeksContinuouslyUnowned = 0;
		if (ArtistPopulationLifecycle.Enabled && freeAgentSigning) {
			artist.contractSequence++;
			artist.contractStartWeek = currentWeek;
			artist.contractTop40Hits = 0;
			artist.contractConsecutiveFlops = 0;
			artist.contractCompletedChartRuns = 0;
		}
		artist.contractUsesExperiencedComebackPolicy = ArtistPopulationLifecycle.Enabled && droppedFreeAgent && priorContractCount > 0;
		artist.careerState = artist.contractUsesExperiencedComebackPolicy
			? GetExperiencedComebackCareerState(artist.careerStateBeforeDrop)
			: CareerState.NewSigning;
		artist.contractEntryCareerState = artist.careerState;
		unsignedPool.RemoveAll(candidate => candidate == artist);
		artist.careerEvents.Add($"{year}: Signed to {labelId}");
	}

	internal static CareerState GetExperiencedComebackCareerState(CareerState stateBeforeDrop) => stateBeforeDrop switch {
		CareerState.NewSigning or CareerState.Rising or CareerState.Established or CareerState.Star or CareerState.Superstar or CareerState.Declining => stateBeforeDrop,
		_ => CareerState.Declining
	};
	
	/// <summary>Legacy drop behavior retained for the disabled replay boundary.</summary>
	public void DropArtist(SimulatedArtist artist, int year) {
		if (artist == null) return;
		artist.labelId = null;
		artist.careerState = CareerState.Dropped;
		unsignedArtists.Add(artist);
		artist.careerEvents.Add($"{year}: Dropped from label");
	}

	/// <summary>
	/// Performs the free-agent half of a roster departure.  When an owning label
	/// is supplied, its roster membership is removed in the same operation.
	/// Repeated reconciliation normalizes the pool without creating a second
	/// event or a duplicate free-agent entry.
	/// </summary>
	public bool DropArtist(SimulatedArtist artist, int year, AILabel owner, ArtistDropReason reason) {
		bool changed = ReconcileDroppedArtist(artist, owner, unsignedArtists, year, reason);
		if (changed) EmitPopulationEvent(artist.lastDropReason == ArtistDropReason.PerformanceExhaustion
			? "performance-exhaustion" : reason == ArtistDropReason.Performance ? "performance-departure" : "drop", artist);
		return changed;
	}

	internal static bool ReconcileDroppedArtistForProbe(SimulatedArtist artist, AILabel owner,
		List<SimulatedArtist> unsignedPool, int year, string reason = "dropped") =>
		ReconcileDroppedArtist(artist, owner, unsignedPool, year, ArtistDropReason.Voluntary);
	internal static bool ReconcileDroppedArtistForProbe(SimulatedArtist artist, AILabel owner,
		List<SimulatedArtist> unsignedPool, int year, ArtistDropReason reason) =>
		ReconcileDroppedArtist(artist, owner, unsignedPool, year, reason);

	private static bool ReconcileDroppedArtist(SimulatedArtist artist, AILabel owner,
		List<SimulatedArtist> unsignedPool, int year, ArtistDropReason reason) {
		if (artist == null || unsignedPool == null) return false;
		int rosterEntries = owner?.roster?.Count(candidate => candidate == artist) ?? 0;
		bool ownershipTransition = artist.careerState != CareerState.Dropped || !string.IsNullOrEmpty(artist.labelId) || rosterEntries > 0;
		owner?.roster?.RemoveAll(candidate => candidate == artist);
		if (artist.careerState != CareerState.Dropped) artist.careerStateBeforeDrop = artist.careerState;
		artist.labelId = null;
		artist.careerState = CareerState.Dropped;
		artist.isActive = true;
		artist.lifecycleStatus = ArtistLifecycleStatus.Active;
		artist.lastDropReason = reason;
		if (ArtistPopulationLifecycle.Enabled && reason == ArtistDropReason.Performance) {
			artist.lastPerformanceDropWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
			if (ownershipTransition) {
				artist.performanceDropCount++;
				artist.usesRepeatPerformanceRecovery = artist.performanceDropCount > 1;
			}
		}
		unsignedPool.RemoveAll(candidate => candidate == artist);
		if (ArtistPopulationLifecycle.Enabled && reason == ArtistDropReason.Performance && artist.performanceDropCount >= 2) {
			artist.careerState = CareerState.Retired;
			artist.lifecycleStatus = ArtistLifecycleStatus.Inactive;
			artist.isActive = false;
			artist.lastDropReason = ArtistDropReason.PerformanceExhaustion;
			if (ownershipTransition) artist.careerEvents.Add($"{year}: PerformanceExhaustion");
			return ownershipTransition;
		}
		unsignedPool.Add(artist);
		if (ownershipTransition) {
			string labelName = owner?.labelName ?? "label";
			artist.careerEvents.Add($"{year}: Released from {labelName} ({reason})");
		}
		return ownershipTransition;
	}
	
	private void PrintPoolStats() {
		GD.Print("=== ARTIST POOL STATS ===");
		var byType = unsignedArtists.GroupBy(a => a.type);
		foreach (var group in byType) GD.Print($"{group.Key}: {group.Count()}");
		
		var byGenre = unsignedArtists.GroupBy(a => a.primaryGenre).OrderByDescending(g => g.Count());
		GD.Print("Top Genres:");
		foreach (var group in byGenre.Take(8)) GD.Print($"  {group.Key}: {group.Count()}");
		
		var avgQuality = unsignedArtists.Average(a => a.CalculateBaseQuality());
		var topTier = unsignedArtists.Count(a => a.CalculateBaseQuality() > 0.7f);
		var lowTier = unsignedArtists.Count(a => a.CalculateBaseQuality() < 0.4f);
		
		GD.Print($"Average Quality: {avgQuality:F2}");
		GD.Print($"High Talent (>0.7): {topTier} ({100f * topTier / unsignedArtists.Count:F1}%)");
		GD.Print($"Low Talent (<0.4): {lowTier} ({100f * lowTier / unsignedArtists.Count:F1}%)");
	}
	
	public void DebugPrintPoolStats() => PrintPoolStats();

	public void DebugPrintSampleArtists() {
		var samples = unsignedArtists.Take(10);
		foreach (var artist in samples) {
			GD.Print($"{artist.stageName} ({artist.type}, {artist.primaryGenre})");
			GD.Print($"  Quality: {artist.CalculateBaseQuality():F2} | Vocal: {artist.vocalPower:F2} | Writing: {artist.songwritingAbility:F2}");
			GD.Print($"  Members: {string.Join(", ", artist.members.Select(m => $"{m.FullName} ({m.primaryRole})"))}");
		}
	}
}
