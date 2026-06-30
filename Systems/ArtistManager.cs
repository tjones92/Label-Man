using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class ArtistManager : Node {
	public static ArtistManager Instance { get; private set; }
	
	[ExportGroup("Configuration")]
	[Export] private int initialPoolSize = 3000;
	[Export] private float maleArtistRatio = 0.72f;
	
	[ExportGroup("Debug")]
	[Export] private bool debugMode = false;
	
	private Dictionary<string, SimulatedArtist> artistRegistry = new Dictionary<string, SimulatedArtist>();
	private Dictionary<string, Musician> musicianRegistry = new Dictionary<string, Musician>();
	
	private int artistIdCounter = 0;
	private int musicianIdCounter = 0;
	
	private List<SimulatedArtist> unsignedArtists = new List<SimulatedArtist>();
	
	public override void _EnterTree() {
		if (Instance != null && Instance != this) {
			QueueFree();
			return;
		}
		Instance = this;
	}

	public override void _Ready() {
		if (ChartManager.Instance != null) {
			ChartManager.Instance.OnRecordLeftChart += OnRecordLeftChart;
		}
	}

	public override void _ExitTree() {
		if (ChartManager.Instance != null) {
			ChartManager.Instance.OnRecordLeftChart -= OnRecordLeftChart;
		}
	}

	private void OnRecordLeftChart(RecordRuntimeData record) {
		if (record?.baseRecord == null) return;

		var artist = GetArtist(record.baseRecord.artistId);
		if (artist == null) return;

		artist.UpdateAfterChartRun(record.peakPosition, record.weeksOnChart, record.totalUnitsSold);
	}
	
	public void GenerateInitialPool(int year) {
		GD.Print($"ArtistManager: Generating initial pool of {initialPoolSize} artists...");
		
		int soloMales = Mathf.RoundToInt(initialPoolSize * 0.25f);
		int soloFemales = Mathf.RoundToInt(initialPoolSize * 0.12f);
		int bands = Mathf.RoundToInt(initialPoolSize * 0.40f);
		int vocalGroups = Mathf.RoundToInt(initialPoolSize * 0.18f);
		int duos = initialPoolSize - soloMales - soloFemales - bands - vocalGroups;
		
		for (int i = 0; i < soloMales; i++) unsignedArtists.Add(GenerateArtist(ArtistType.SoloMale, GetRandomGenre(), year, null));
		for (int i = 0; i < soloFemales; i++) unsignedArtists.Add(GenerateArtist(ArtistType.SoloFemale, GetRandomGenre(), year, null));
		for (int i = 0; i < bands; i++) unsignedArtists.Add(GenerateArtist(ArtistType.Band, GetRandomGenre(), year, null));
		for (int i = 0; i < vocalGroups; i++) unsignedArtists.Add(GenerateArtist(ArtistType.VocalGroup, GetVocalGroupGenre(), year, null));
		for (int i = 0; i < duos; i++) unsignedArtists.Add(GenerateArtist(ArtistType.Duo, GetRandomGenre(), year, null));
		
		if (debugMode) PrintPoolStats();
		GD.Print($"ArtistManager: Generated {artistRegistry.Count} artists with {musicianRegistry.Count} musicians");
	}
	
	public SimulatedArtist GenerateArtist(ArtistType type, Genre genre, int year, string region) {
		artistIdCounter++;
		string id = $"artist_{artistIdCounter:D5}";
		
		var artist = new SimulatedArtist {
			artistId = id,
			type = type,
			primaryGenre = genre,
			secondaryGenre = GetRelatedGenre(genre),
			homeRegion = region ?? GetRandomRegion(),
			formedYear = year - (int)GD.RandRange(0, 5),
			careerState = CareerState.Unsigned
		};
		
		GenerateMembers(artist, type, genre, year);
		artist.stageName = type is ArtistType.SoloMale or ArtistType.SoloFemale
			? artist.members[0].FullName
			: GenerateStageName(type, genre, year);
		artist.RecalculateStats();
		
		artist.momentum = 0f;
		artist.reputation = (float)GD.RandRange(0f, 0.1f);
		
		artistRegistry[id] = artist;
		return artist;
	}
	
	private string GenerateStageName(ArtistType type, Genre genre, int year) {
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
		musician.isPrimaryWriter = GD.Randf() > 0.4f;
		musician.isBandLeader = true;
		musician.stagePresence = Mathf.Clamp(musician.stagePresence + 0.15f, 0f, 1f);
		musician.ego = Mathf.Clamp(musician.ego + 0.1f, 0f, 1f);
		musician.ambition = Mathf.Clamp(musician.ambition + 0.1f, 0f, 1f);
		artist.AddMember(musician, year, true);
	}
	
	private void GenerateDuo(SimulatedArtist artist, Genre genre, int year) {
		bool sameSex = GD.Randf() > 0.3f;
		bool member1Male = GD.Randf() > 0.35f;
		bool member2Male = sameSex ? member1Male : !member1Male;
		
		var member1 = GenerateMusician(member1Male, year);
		member1.primaryRole = MusicianRole.LeadVocals;
		member1.isLeadVocalist = true;
		member1.isPrimaryWriter = GD.Randf() > 0.5f;
		
		var member2 = GenerateMusician(member2Male, year);
		member2.primaryRole = genre switch {
			Genre.Folk or Genre.Country => MusicianRole.RhythmGuitar,
			Genre.Jazz => MusicianRole.Piano,
			_ => MusicianRole.BackingVocals
		};
		member2.isPrimaryWriter = GD.Randf() > 0.5f;
		
		if (GD.Randf() > 0.5f) member1.isBandLeader = true;
		else member2.isBandLeader = true;
		
		artist.AddMember(member1, year, true);
		artist.AddMember(member2, year, true);
	}
	
	private void GenerateBand(SimulatedArtist artist, Genre genre, int year) {
		var lineup = GetBandLineup(genre);
		bool firstMember = true;
		
		foreach (var role in lineup) {
			bool isMale = role.isMale ?? (GD.Randf() < maleArtistRatio);
			var musician = GenerateMusician(isMale, year);
			musician.primaryRole = role.role;
			musician.isLeadVocalist = role.isLead;
			musician.isBandLeader = firstMember && role.isLead;
			
			if (firstMember || (role.role == MusicianRole.LeadGuitar && GD.Randf() > 0.5f)) {
				musician.isPrimaryWriter = GD.Randf() > 0.3f;
			}
			
			artist.AddMember(musician, year, true);
			firstMember = false;
		}
	}
	
	private void GenerateVocalGroup(SimulatedArtist artist, Genre genre, int year) {
		bool isGirlGroup = genre == Genre.GirlGroup || (genre == Genre.Motown && GD.Randf() > 0.6f);
		int memberCount = (int)GD.RandRange(3, 6);
		bool hasLeadDesignated = false;
		
		for (int i = 0; i < memberCount; i++) {
			bool isMale = !isGirlGroup && (GD.Randf() < 0.85f);
			var musician = GenerateMusician(isMale, year);
			
			if (!hasLeadDesignated && GD.Randf() > 0.3f) {
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
		
		if (GD.Randf() > 0.7f) {
			var smartest = artist.members.OrderByDescending(m => m.creativity).First();
			smartest.isPrimaryWriter = true;
		}
	}
	
	private Musician GenerateMusician(bool isMale, int currentYear) {
		musicianIdCounter++;
		string id = $"mus_{musicianIdCounter:D6}";
		string firstName, lastName;
		
		if (NameGenerator.Instance != null) {
			(firstName, lastName) = NameGenerator.Instance.GeneratePersonName(isMale);
		} else {
			firstName = isMale ? $"John{musicianIdCounter}" : $"Jane{musicianIdCounter}";
			lastName = $"Doe{musicianIdCounter}";
		}
		
		int birthYear = currentYear - (GD.Randf() < 0.85f ? (int)GD.RandRange(18, 29) : (int)GD.RandRange(29, 42));
		
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
		float u1 = GD.Randf();
		float u2 = GD.Randf();
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
		float roll = GD.Randf();
		if (roll < 0.18f) return Genre.RockAndRoll;
		if (roll < 0.32f) return Genre.RnB;
		if (roll < 0.42f) return Genre.TraditionalPop;
		if (roll < 0.50f) return Genre.DooWop;
		if (roll < 0.58f) return Genre.Soul;
		if (roll < 0.64f) return Genre.Country;
		if (roll < 0.69f) return Genre.Jazz;
		if (roll < 0.74f) return Genre.Gospel;
		if (roll < 0.79f) return Genre.TeenPop;
		if (roll < 0.84f) return Genre.Folk;
		if (roll < 0.88f) return Genre.GirlGroup;
		if (roll < 0.92f) return Genre.Motown;
		if (roll < 0.95f) return Genre.SurfRock;
		return Genre.BluesRock;
	}
	
	private Genre GetVocalGroupGenre() {
		float roll = GD.Randf();
		if (roll < 0.35f) return Genre.DooWop;
		if (roll < 0.55f) return Genre.GirlGroup;
		if (roll < 0.75f) return Genre.Motown;
		if (roll < 0.85f) return Genre.Soul;
		if (roll < 0.92f) return Genre.RnB;
		return Genre.Gospel;
	}
	
	private Genre GetRelatedGenre(Genre primary) {
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
	
	private Genre RandomPick(params Genre[] options) => options[(int)GD.RandRange(0, options.Length - 1)];
	
	private string GetRandomRegion() {
		string[] regions = { "Northeast", "Southeast", "Midwest", "Southwest", "WestCoast", "UK" };
		return regions[(int)GD.RandRange(0, regions.Length - 1)];
	}
	
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
	public List<SimulatedArtist> GetUnsignedArtists() => unsignedArtists.Where(a => a.careerState == CareerState.Unsigned && a.isActive).ToList();
	
	public List<SimulatedArtist> GetUnsignedByGenre(Genre genre) {
		return unsignedArtists.Where(a => a.careerState == CareerState.Unsigned && a.isActive && (a.primaryGenre == genre || a.secondaryGenre == genre)).ToList();
	}
	
	public List<SimulatedArtist> GetTopUnsignedTalent(int count, Genre? preferredGenre = null) {
		var pool = preferredGenre.HasValue ? GetUnsignedByGenre(preferredGenre.Value) : GetUnsignedArtists();
		return pool.OrderByDescending(a => a.CalculateBaseQuality()).Take(count).ToList();
	}
	
	public void SignArtist(SimulatedArtist artist, string labelId, int year) {
		artist.labelId = labelId;
		artist.signedYear = year;
		artist.careerState = CareerState.NewSigning;
		unsignedArtists.Remove(artist);
		artist.careerEvents.Add($"{year}: Signed to {labelId}");
	}
	
	public void DropArtist(SimulatedArtist artist, int year) {
		artist.labelId = null;
		artist.careerState = CareerState.Dropped;
		unsignedArtists.Add(artist);
		artist.careerEvents.Add($"{year}: Dropped from label");
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
