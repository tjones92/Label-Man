using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using LabelMan.Naming;

/// <summary>
/// Coherent January 1960 careers (SimTools/OpeningCareersDesign.md; CelebrityEconomyAudit-2026-10-15 §1-2).
/// Picks ~75 launch acts by genre fit and quality, gives each a compact pre-1960 history, and derives
/// everything else from it: ages, formation year, the chart counters (summed from the history, so an act
/// can no longer have more chart entries than releases), and public recognition plus members' fame
/// (replayed through <see cref="ArtistRecognitionService"/> with its own weekly decay). No new launch
/// multiplier: the replayed recognition feeds launches through the existing bounded profile.
/// <para>
/// Every draw comes from a per-act generator keyed on the world seed and the act id; selection is a
/// deterministic sort. Nothing touches the shared or population streams.
/// </para>
/// </summary>
public static class OpeningCareerService {
	private sealed class Spec {
		public OpeningCareerArchetype Archetype;
		public int Count, Superstars;
		public CareerState Rung;
		public Genre[] Genres;
		public LabelTier[] Tiers;
		public int LeadAgeMin, LeadAgeMax, StartMin, StartMax;
		public int SinglesMin, SinglesMax, AlbumsMin, AlbumsMax;
		/// <summary>Chance a side charts nationally at the height of the career, and the strength of its placings.</summary>
		public float ChartChance, Strength;
		public int NumberOnesMin, NumberOnesMax, SuperstarNumberOnesMin, SuperstarNumberOnesMax;
	}

	private static readonly LabelTier[] Big = { LabelTier.Major, LabelTier.MidTier };

	// Superstar and Star totals match the canopy this replaces (6 and 24).
	private static readonly Spec[] Specs = {
		new() { Archetype = OpeningCareerArchetype.AdultStandardBearer, Count = 8, Superstars = 3, Rung = CareerState.Star,
			Genres = new[] { Genre.TraditionalPop, Genre.EasyListening, Genre.Jazz, Genre.Country, Genre.Classical }, Tiers = Big,
			LeadAgeMin = 35, LeadAgeMax = 50, StartMin = 1936, StartMax = 1950, SinglesMin = 3, SinglesMax = 6, AlbumsMin = 1, AlbumsMax = 2,
			ChartChance = 0.70f, Strength = 0.85f, NumberOnesMin = 2, NumberOnesMax = 3, SuperstarNumberOnesMin = 5, SuperstarNumberOnesMax = 9 },
		new() { Archetype = OpeningCareerArchetype.CurrentHitmaker, Count = 10, Superstars = 3, Rung = CareerState.Star,
			Genres = new[] { Genre.RockAndRoll, Genre.TeenPop, Genre.DooWop }, Tiers = Big,
			LeadAgeMin = 17, LeadAgeMax = 26, StartMin = 1955, StartMax = 1958, SinglesMin = 3, SinglesMax = 5, AlbumsMin = 0, AlbumsMax = 1,
			ChartChance = 0.80f, Strength = 0.85f, NumberOnesMin = 1, NumberOnesMax = 3, SuperstarNumberOnesMin = 4, SuperstarNumberOnesMax = 6 },
		new() { Archetype = OpeningCareerArchetype.EstablishedHitmaker, Count = 12, Rung = CareerState.Star,
			Genres = new[] { Genre.RnB, Genre.Country, Genre.TraditionalPop, Genre.RockAndRoll, Genre.DooWop, Genre.EasyListening, Genre.Blues, Genre.Comedy, Genre.LatinPop, Genre.Folk },
			Tiers = Big, LeadAgeMin = 25, LeadAgeMax = 40, StartMin = 1948, StartMax = 1956, SinglesMin = 2, SinglesMax = 5, AlbumsMin = 0, AlbumsMax = 1,
			ChartChance = 0.55f, Strength = 0.65f, NumberOnesMin = 1, NumberOnesMax = 2 },
		new() { Archetype = OpeningCareerArchetype.FormerStar, Count = 10, Rung = CareerState.Declining,
			Genres = new[] { Genre.TraditionalPop, Genre.EasyListening, Genre.Country, Genre.RnB, Genre.Jazz, Genre.Blues },
			Tiers = new[] { LabelTier.Major, LabelTier.MidTier, LabelTier.Independent },
			LeadAgeMin = 38, LeadAgeMax = 55, StartMin = 1935, StartMax = 1946, SinglesMin = 2, SinglesMax = 5, AlbumsMin = 0, AlbumsMax = 1,
			ChartChance = 0.60f, Strength = 0.75f, NumberOnesMin = 1, NumberOnesMax = 4 },
		new() { Archetype = OpeningCareerArchetype.CriticsFavourite, Count = 15, Rung = CareerState.Established,
			Genres = new[] { Genre.Jazz, Genre.Folk, Genre.Blues, Genre.Classical, Genre.Gospel },
			Tiers = new[] { LabelTier.Major, LabelTier.MidTier, LabelTier.Independent, LabelTier.Boutique, LabelTier.Small },
			LeadAgeMin = 25, LeadAgeMax = 50, StartMin = 1945, StartMax = 1957, SinglesMin = 0, SinglesMax = 2, AlbumsMin = 1, AlbumsMax = 2,
			ChartChance = 0.25f, Strength = 0.40f },
		new() { Archetype = OpeningCareerArchetype.RegionalName, Count = 20, Rung = CareerState.Established,
			Genres = new[] { Genre.Country, Genre.Blues, Genre.RnB, Genre.Gospel, Genre.LatinPop },
			Tiers = new[] { LabelTier.Independent, LabelTier.Small, LabelTier.Boutique, LabelTier.MidTier },
			LeadAgeMin = 25, LeadAgeMax = 45, StartMin = 1945, StartMax = 1956, SinglesMin = 2, SinglesMax = 4, AlbumsMin = 0, AlbumsMax = 1,
			ChartChance = 0.12f, Strength = 0.35f },
	};

	private const int MaxSuperstarsPerLabel = 2, MaxStarsPerLabel = 4, MaxOpeningActsPerLabel = 6;
	public const int FirstLpYear = 1949;

	// --studio-parent-labels: the studio labels' 1960 stars (FilmBroadcastLabelResearch.md). MGM had Connie Francis and
	// Conway Twitty, ABC-Paramount Paul Anka and Lloyd Price/Ray Charles, Warner the Everly Brothers, Dot Pat Boone and
	// Billy Vaughn, UA Marv Johnson. Each row picks the label's best act in those genres. The acts come out of the same
	// budget as everyone else's (archetype counts and Superstars), so the world has no more stars, only a different owner.
	private static readonly (string Parent, OpeningCareerArchetype Archetype, CareerState Rung, Genre[] Genres)[] StudioStars = {
		(StudioParentLabels.Mgm, OpeningCareerArchetype.CurrentHitmaker, CareerState.Superstar, new[] { Genre.TeenPop, Genre.TraditionalPop }),
		(StudioParentLabels.Mgm, OpeningCareerArchetype.CurrentHitmaker, CareerState.Star, new[] { Genre.RockAndRoll, Genre.Country }),
		(StudioParentLabels.Abpt, OpeningCareerArchetype.CurrentHitmaker, CareerState.Star, new[] { Genre.TeenPop, Genre.RockAndRoll }),
		(StudioParentLabels.Abpt, OpeningCareerArchetype.EstablishedHitmaker, CareerState.Star, new[] { Genre.RnB, Genre.Blues }),
		(StudioParentLabels.Warner, OpeningCareerArchetype.CurrentHitmaker, CareerState.Superstar, new[] { Genre.RockAndRoll, Genre.TeenPop }),
		(StudioParentLabels.Paramount, OpeningCareerArchetype.CurrentHitmaker, CareerState.Star, new[] { Genre.TeenPop, Genre.TraditionalPop, Genre.RockAndRoll }),
		(StudioParentLabels.Paramount, OpeningCareerArchetype.AdultStandardBearer, CareerState.Star, new[] { Genre.EasyListening, Genre.TraditionalPop }),
		(StudioParentLabels.UnitedArtists, OpeningCareerArchetype.EstablishedHitmaker, CareerState.Established, new[] { Genre.RnB, Genre.DooWop, Genre.TeenPop }),
	};

	// --historical-independent-stars: named Independents' January 1960 stars. Imperial had Ricky Nelson (a run of Top 10s
	// since 1957) and Fats Domino (on Imperial since 1949); both made the 1960-61 Top 10 there. Liberty's 1960-61 Top 10s
	// were all acts without a prior national hit (Bobby Vee, Johnny Burnette, Gene McDaniels, Timi Yuro) and Monument's were
	// Roy Orbison's first, so neither label gets a row. Like the studio rows, these come out of the same archetype budget.
	private static readonly (string Label, OpeningCareerArchetype Archetype, CareerState Rung, Genre[] Genres)[] IndependentStars = {
		("Imperial", OpeningCareerArchetype.CurrentHitmaker, CareerState.Star, new[] { Genre.TeenPop, Genre.RockAndRoll }),
		("Imperial", OpeningCareerArchetype.EstablishedHitmaker, CareerState.Star, new[] { Genre.RnB, Genre.RockAndRoll }),
	};

	private static bool IsNamed(AILabel l, string name) =>
		l != null && l.isHistorical && l.labelName != null && (l.labelName == name || l.labelName.StartsWith(name + " ", StringComparison.Ordinal));

	public static void Seed(List<AILabel> labels, int year) {
		if (labels == null) return;
		var chosen = new HashSet<string>(StringComparer.Ordinal);
		var perLabel = new Dictionary<string, (int Superstars, int Stars, int Total)>(StringComparer.Ordinal);
		var tally = new Dictionary<OpeningCareerArchetype, int>();
		var studioCount = new Dictionary<OpeningCareerArchetype, (int Acts, int Superstars)>();
		var independentCount = new Dictionary<OpeningCareerArchetype, (int Acts, int Superstars)>();
		void SeedRow(string tag, string owner, AILabel label, OpeningCareerArchetype archetype, CareerState rung, Genre[] genres,
				Dictionary<OpeningCareerArchetype, (int Acts, int Superstars)> count) {
			SimulatedArtist artist = label?.roster?
				.Where(a => a != null && !chosen.Contains(a.artistId) && a.members?.Count > 0 &&
					genres.Contains(GenreCatalog.MapLegacy(a.primaryGenre, year)))
				.OrderByDescending(a => a.CalculateBaseQuality()).ThenBy(a => a.artistId, StringComparer.Ordinal).FirstOrDefault();
			GD.Print($"{tag} label={label?.labelId} {owner} archetype={archetype} rung={rung} " +
				$"artist={artist?.artistId} genre={artist?.primaryGenre} quality={artist?.CalculateBaseQuality() ?? 0f:F3}");
			if (artist == null) return;
			Stamp(artist, label, Specs.First(s => s.Archetype == archetype), rung, year);
			chosen.Add(artist.artistId);
			perLabel.TryGetValue(label.labelId, out var used);
			perLabel[label.labelId] = Count(used, rung, +1);
			count.TryGetValue(archetype, out var n);
			count[archetype] = (n.Acts + 1, n.Superstars + (rung == CareerState.Superstar ? 1 : 0));
		}
		if (StudioParentLabels.Enabled)
			foreach (var row in StudioStars)
				SeedRow("STUDIO_OPENING_CAREER", $"parent=\"{row.Parent}\"",
					labels.FirstOrDefault(l => StudioParentLabels.IsStudioLabel(l) && l.studioParent == row.Parent),
					row.Archetype, row.Rung, row.Genres, studioCount);
		if (OpeningCareers.IndependentStars)
			foreach (var row in IndependentStars)
				SeedRow("INDEPENDENT_OPENING_CAREER", $"name=\"{row.Label}\"", labels.FirstOrDefault(l => IsNamed(l, row.Label)),
					row.Archetype, row.Rung, row.Genres, independentCount);
		foreach (Spec spec in Specs) {
			studioCount.TryGetValue(spec.Archetype, out var studio);
			independentCount.TryGetValue(spec.Archetype, out var independent);
			var candidates = new List<(SimulatedArtist Artist, AILabel Label)>();
			foreach (AILabel label in labels) {
				if (label?.roster == null || !spec.Tiers.Contains(label.tier)) continue;
				if (StudioParentLabels.IsStudioLabel(label)) continue;   // the studio rows are the studio labels' whole allocation
				foreach (SimulatedArtist artist in label.roster)
					if (artist != null && !chosen.Contains(artist.artistId) && artist.members?.Count > 0 &&
						spec.Genres.Contains(GenreCatalog.MapLegacy(artist.primaryGenre, year)))
						candidates.Add((artist, label));
			}
			candidates.Sort((a, b) => {
				int byQuality = b.Artist.CalculateBaseQuality().CompareTo(a.Artist.CalculateBaseQuality());
				return byQuality != 0 ? byQuality : string.CompareOrdinal(a.Artist.artistId, b.Artist.artistId);
			});
			var picks = new List<(SimulatedArtist Artist, AILabel Label, CareerState Rung)>();
			foreach ((SimulatedArtist artist, AILabel label) in candidates) {
				if (picks.Count >= spec.Count) break;
				perLabel.TryGetValue(label.labelId, out var used);
				if (used.Total >= MaxOpeningActsPerLabel) continue;
				CareerState rung = picks.Count < spec.Superstars ? CareerState.Superstar : spec.Rung;
				if (rung == CareerState.Superstar && used.Superstars >= MaxSuperstarsPerLabel) rung = spec.Rung;
				if (rung == CareerState.Star && used.Stars >= MaxStarsPerLabel) continue;
				picks.Add((artist, label, rung));
				perLabel[label.labelId] = Count(used, rung, +1);
			}
			if (studio.Acts > 0) GiveUpMidTierPicks(picks, perLabel, spec, studio.Acts, studio.Superstars);
			// The Independents' rows replace the weakest picks wherever they sit: the Big-5 already over-hold 1960-61 Top 10s.
			if (independent.Acts > 0) GiveUpMidTierPicks(picks, perLabel, spec, independent.Acts, independent.Superstars, midTierFirst: false);
			foreach ((SimulatedArtist artist, AILabel label, CareerState rung) in picks) {
				Stamp(artist, label, spec, rung, year);
				chosen.Add(artist.artistId);
			}
			tally[spec.Archetype] = picks.Count + studio.Acts + independent.Acts;
		}
		int superstars = labels.Sum(l => l?.roster?.Count(a => a?.openingArchetype != OpeningCareerArchetype.None && a.careerState == CareerState.Superstar) ?? 0);
		GD.Print($"OpeningCareerService: seeded {chosen.Count} opening careers ({superstars} Superstars) - " +
			string.Join(", ", tally.Select(t => $"{t.Key} {t.Value}")));
	}

	private static (int Superstars, int Stars, int Total) Count((int Superstars, int Stars, int Total) used, CareerState rung, int sign) =>
		(used.Superstars + sign * (rung == CareerState.Superstar ? 1 : 0),
		 used.Stars + sign * (rung is CareerState.Star or CareerState.Superstar ? 1 : 0), used.Total + sign);

	/// <summary>
	/// --studio-parent-labels: the studio rows replace other MidTier labels' picks, not the Big-5's. In 1960 the field held a
	/// third of the repeat Top 10 acts and the Big-5 a bit more (FilmBroadcastLabelResearch.md (d)); taking the studio
	/// stars out of the quality ranking halved the Big-5's. Drops the lowest-quality MidTier picks and demotes the
	/// lowest-quality MidTier Superstars; if MidTier runs short, the lowest-quality picks anywhere make up the rest.
	/// With <paramref name="midTierFirst"/> off, the lowest-quality picks of any tier give way.
	/// </summary>
	private static void GiveUpMidTierPicks(List<(SimulatedArtist Artist, AILabel Label, CareerState Rung)> picks,
			Dictionary<string, (int Superstars, int Stars, int Total)> perLabel, Spec spec, int acts, int superstars, bool midTierFirst = true) {
		int Order((SimulatedArtist Artist, AILabel Label, CareerState Rung) p) => midTierFirst && p.Label.tier == LabelTier.MidTier ? 0 : 1;
		var byWeakest = picks.Select((p, i) => (Pick: p, Index: i))
			.OrderBy(x => Order(x.Pick)).ThenBy(x => x.Pick.Artist.CalculateBaseQuality()).ThenByDescending(x => x.Index).ToList();
		int demote = superstars;
		foreach (var x in byWeakest.Where(x => x.Pick.Rung == CareerState.Superstar)) {   // superstar budget first
			if (demote <= 0) break;
			perLabel[x.Pick.Label.labelId] = Count(Count(perLabel[x.Pick.Label.labelId], CareerState.Superstar, -1), spec.Rung, +1);
			picks[x.Index] = (x.Pick.Artist, x.Pick.Label, spec.Rung);
			demote--;
		}
		var drop = new HashSet<int>();
		foreach (var x in byWeakest.Where(x => picks[x.Index].Rung != CareerState.Superstar)) {
			if (drop.Count >= acts) break;
			perLabel[x.Pick.Label.labelId] = Count(perLabel[x.Pick.Label.labelId], picks[x.Index].Rung, -1);
			drop.Add(x.Index);
		}
		GD.Print($"{(midTierFirst ? "STUDIO" : "INDEPENDENT")}_OPENING_GIVE_UP archetype={spec.Archetype} acts={acts} dropped={drop.Count} superstars={superstars} demoted={superstars - demote} " +
			$"fromMidTier={drop.Count(i => picks[i].Label.tier == LabelTier.MidTier)} labels={string.Join(",", drop.Select(i => picks[i].Label.labelId))}");
		for (int i = picks.Count - 1; i >= 0; i--) if (drop.Contains(i)) picks.RemoveAt(i);
	}

	// ---- one act ---------------------------------------------------------------------------------

	private static void Stamp(SimulatedArtist artist, AILabel label, Spec spec, CareerState rung, int year) {
		var rng = new DeterministicRandom((ulong)(BandLife.Unit("opening-career|" + artist.artistId) * 9007199254740992.0));
		int start = rng.Range(spec.StartMin, spec.StartMax);
		Reage(artist, rng.Range(spec.LeadAgeMin, spec.LeadAgeMax), start, year);

		List<PublicReleaseEntry> history = DrawHistory(artist, label, spec, rung, start, year, rng);
		artist.publicHistory = history;
		artist.openingArchetype = spec.Archetype;
		artist.careerState = rung;
		DeriveCounters(artist, history, rng);
		DeriveMomentumAndReputation(artist, history, year);
		ReplayRecognition(artist, history, year);
		DeriveEsteem(artist, spec, rng);

		int firstNumberOne = history.Where(e => e.peakPosition == 1).Select(e => e.year).DefaultIfEmpty(0).Min();
		artist.careerEvents.Add($"{start}: Made their first records{(firstNumberOne > 0 ? $"; first #1 in {firstNumberOne}" : "")}.");
		artist.careerEvents.Add($"{year}: {Describe(spec.Archetype, rung)} on {label.labelName} ({artist.publicHistory.Count} releases since {start}).");
		// Who holds the opening stars: the screens compare this allocation (by owner tier and studio parent) with history.
		GD.Print($"OPENING_CAREER artist={artist.artistId} label={label.labelId} tier={label.tier} historical={label.isHistorical} " +
			$"studio={StudioParentLabels.IsStudioLabel(label)} archetype={spec.Archetype} rung={rung} genre={artist.primaryGenre} " +
			$"numberOnes={artist.numberOnes} top10={artist.top10Hits}");
	}

	/// <summary>Moves the whole lineup in time so the lead is <paramref name="leadAge"/>, keeping the band's age
	/// structure, then re-derives the person values that were keyed on the old age.</summary>
	private static void Reage(SimulatedArtist artist, int leadAge, int start, int year) {
		Musician lead = artist.GetLeadSinger() ?? artist.members.FirstOrDefault(m => m.isActive);
		int shift = lead == null ? 0 : leadAge - lead.GetAge(year);
		foreach (Musician m in artist.members) {
			if (m == null) continue;
			m.birthYear = Math.Min(m.birthYear - shift, start - 16);   // nobody made a record before sixteen
			if (m.isFoundingMember || m.joinedYear >= artist.formedYear) m.joinedYear = start;
			// Ceilings (axes) and maturity (identity) were drawn against the generated age. Both are keyed and draw
			// nothing from a stream, so regenerating them against the real age is the same person, older.
			if (BandLife.MemberAxesEnabled && m.axesVersion > 0) { m.axesVersion = 0; MemberAxesService.EnsureAxes(m, artist, year); }
			if (m.identityVersion > 0) { m.identityVersion = 0; MemberIdentityService.EnsureIdentity(m, artist, year); }
		}
		artist.formedYear = start;
		artist.signedYear = Math.Max(artist.signedYear, start);
	}

	// ---- the history -----------------------------------------------------------------------------

	private static List<PublicReleaseEntry> DrawHistory(SimulatedArtist artist, AILabel label, Spec spec, CareerState rung,
			int start, int year, IRandom rng) {
		int last = year - 1;
		int peakYear = spec.Archetype switch {
			OpeningCareerArchetype.CurrentHitmaker => rng.Range(Math.Max(start, 1957), last),
			OpeningCareerArchetype.FormerStar => rng.Range(Math.Min(start + 3, 1950), 1952),
			OpeningCareerArchetype.AdultStandardBearer => rng.Range(Math.Min(start + 4, 1955), 1957),
			_ => rng.Range(Math.Min(start + 2, last), last),
		};
		float width = spec.Archetype == OpeningCareerArchetype.CurrentHitmaker ? 2f : spec.Archetype == OpeningCareerArchetype.AdultStandardBearer ? 8f : 5f;
		bool regional = spec.Archetype == OpeningCareerArchetype.RegionalName;
		var history = new List<PublicReleaseEntry>();
		for (int y = start; y <= last; y++) {
			float curve = Mathf.Exp(-Mathf.Pow((y - peakYear) / width, 2f));
			if (spec.Archetype == OpeningCareerArchetype.FormerStar && y > peakYear) curve *= 0.5f;   // the fall is steeper than the climb
			int singles = rng.Range(spec.SinglesMin, spec.SinglesMax);
			int albums = y >= FirstLpYear ? rng.Range(spec.AlbumsMin, spec.AlbumsMax) : 0;
			for (int i = 0; i < singles + albums; i++) {
				bool lp = i >= singles;
				bool charts = rng.NextDouble() < spec.ChartChance * (0.25 + 0.75 * curve);
				int peak = 0;
				if (charts) {
					double q = spec.Strength * curve * Math.Sqrt(rng.NextDouble());
					peak = Mathf.Clamp((int)Math.Round(1 + 99 * Math.Pow(1 - q, 2.2)), 1, 100);
				} else if (regional) peak = 0;
				history.Add(Entry(artist, label, y, lp, peak, regional, rng));
			}
		}
		if (history.Count == 0) history.Add(Entry(artist, label, last, true, 0, regional, rng));

		bool superstar = rung == CareerState.Superstar;
		int numberOnes = superstar ? rng.Range(spec.SuperstarNumberOnesMin, spec.SuperstarNumberOnesMax) : rng.Range(spec.NumberOnesMin, spec.NumberOnesMax);
		SetNumberOnes(history, numberOnes, spec.Archetype == OpeningCareerArchetype.FormerStar ? 1954 : last, rng);
		ShapeTail(history, rung, spec.Archetype == OpeningCareerArchetype.CriticsFavourite, rng);
		foreach (PublicReleaseEntry e in history) Finish(e, regional, rng);
		return history;
	}

	private static PublicReleaseEntry Entry(SimulatedArtist artist, AILabel label, int y, bool lp, int peak, bool regional, IRandom rng) {
		string key = $"{artist.artistId}|{y}|{rng.Next(1 << 30)}";
		string title = NameGenerator.Instance?.KeyedSongTitle(artist.primaryGenre, y, key);
		return new PublicReleaseEntry {
			year = y, format = lp ? ReleaseFormat.Album : ReleaseFormat.Single, peakPosition = peak,
			title = string.IsNullOrWhiteSpace(title) ? (lp ? $"{artist.stageName} Sings" : $"Side {y}") : title,
			labelName = label.labelName, regionId = regional ? artist.homeRegion : null,
		};
	}

	/// <summary>Exactly <paramref name="target"/> #1s, taken from the act's strongest sides up to <paramref name="latestYear"/>.</summary>
	private static void SetNumberOnes(List<PublicReleaseEntry> history, int target, int latestYear, IRandom rng) {
		var ones = history.Where(e => e.peakPosition == 1).ToList();
		foreach (PublicReleaseEntry e in ones.Skip(target)) e.peakPosition = rng.Range(2, 6);
		int missing = target - Math.Min(target, ones.Count);
		foreach (PublicReleaseEntry e in history.Where(e => e.peakPosition > 1 && e.year <= latestYear)
				.OrderBy(e => e.peakPosition).Take(missing).ToList())
			e.peakPosition = 1;
		missing = target - history.Count(e => e.peakPosition == 1);
		foreach (PublicReleaseEntry e in history.Where(e => e.peakPosition != 1 && e.year <= latestYear)
				.OrderByDescending(e => e.year).Take(missing).ToList())
			e.peakPosition = 1;   // a career this size had its #1s even if the draw was cold
	}

	/// <summary>
	/// The most recent sides decide where the act stands on the live ladder (SimulatedArtist.UpdateCareerState): a
	/// Top 40 placing is a hit, a side that missed or peaked below 60 is a flop. The tail is shaped so the rung is the
	/// one the ladder would hold the act at, not one its first live record promotes or demotes it out of.
	/// </summary>
	private static void ShapeTail(List<PublicReleaseEntry> history, CareerState rung, bool lastLpCharted, IRandom rng) {
		int n = history.Count;
		void Hit(PublicReleaseEntry e) { if (e.peakPosition is <= 0 or > 40) e.peakPosition = rng.Range(8, 35); }
		void Flop(PublicReleaseEntry e) { if (e.peakPosition is > 0 and <= 60) e.peakPosition = rng.Chance(0.5) ? 0 : rng.Range(61, 100); }
		switch (rung) {
			case CareerState.Superstar:   // Star -> Superstar needs four straight hits; three flops would demote
				for (int i = Math.Max(0, n - 4); i < n; i++) Hit(history[i]);
				break;
			case CareerState.Star:        // two flops demote a Star
				for (int i = Math.Max(0, n - 2); i < n; i++) Hit(history[i]);
				break;
			case CareerState.Declining:   // one hit restores Established, three flops end the deal: the act sits between
				Flop(history[n - 1]);
				if (n >= 2) Flop(history[n - 2]);
				break;
			default:                      // Established: a critics' favourite's latest LP made the Top 40; a regional name
				// crossed over once in its last few sides. Without that the two-flop cap below would have to invent a run of
				// national #41-60 placings, and each one is a recognition credit a regional act never earned nationally.
				Hit(history[lastLpCharted ? n - 1 : Math.Max(0, n - 1 - rng.Range(0, 2))]);
				break;
		}
		// No more than two flops since the last hit, for every rung, so nothing starts one side from a drop.
		int flops = 0;
		for (int i = n - 1; i >= 0 && history[i].peakPosition is <= 0 or > 40; i--) {
			if (history[i].peakPosition is > 0 and <= 60) continue;
			if (++flops > 2) history[i].peakPosition = rng.Range(41, 60);
		}
	}

	private static void Finish(PublicReleaseEntry e, bool regional, IRandom rng) {
		int p = e.peakPosition;
		if (p <= 0) { e.weeksCharted = 0; e.weeksInTopTen = 0; e.markets = regional ? rng.Range(1, 2) : 1; return; }
		int weeks = (int)Math.Round(4 + (100 - p) / 100.0 * 14 + rng.NextDouble() * 4);
		if (e.format == ReleaseFormat.Album) weeks = (int)Math.Round(weeks * 2.2);
		e.weeksCharted = weeks;
		e.weeksInTopTen = p <= 10 ? Math.Min(weeks, (int)Math.Round((11 - p) * 0.9 + rng.NextDouble() * 3)) : 0;
		e.markets = regional ? rng.Range(1, 3) : p <= 40 ? rng.Range(5, 7) : rng.Range(2, 4);
	}

	// ---- derivation ------------------------------------------------------------------------------

	/// <summary>The counters, summed in date order by the same rules the live chart handlers apply.</summary>
	private static void DeriveCounters(SimulatedArtist a, List<PublicReleaseEntry> history, IRandom rng) {
		a.totalReleases = history.Count;
		a.charted = a.top40Hits = a.top10Hits = a.numberOnes = a.weeksAtNumberOne = 0;
		a.consecutiveHits = a.consecutiveFlops = 0;
		foreach (PublicReleaseEntry e in history) {
			int p = e.peakPosition;
			if (p > 0) a.charted++;
			if (p is > 0 and <= 40) { a.top40Hits++; a.consecutiveHits++; a.consecutiveFlops = 0; }
			if (p is > 0 and <= 10) a.top10Hits++;
			if (p == 1) { a.numberOnes++; a.weeksAtNumberOne += Math.Max(1, Math.Min(e.weeksInTopTen, rng.Range(1, 5))); }
			if (p <= 0 || p > 60) { a.consecutiveFlops++; a.consecutiveHits = 0; }
		}
	}

	/// <summary>Momentum is what the last two years did; reputation is the whole record. A former star keeps the name and loses the heat.</summary>
	private static void DeriveMomentumAndReputation(SimulatedArtist a, List<PublicReleaseEntry> history, int year) {
		static float Weight(int p) => p == 1 ? 1f : p is > 0 and <= 10 ? 0.6f : p is > 0 and <= 40 ? 0.3f : 0f;
		float recent = history.Where(e => e.year >= year - 2).Sum(e => Weight(e.peakPosition));
		a.momentum = Mathf.Clamp(0.10f + 0.22f * recent, 0.05f, 0.90f);
		float record = 1f - Mathf.Exp(-(a.numberOnes * 0.5f + a.top10Hits * 0.15f + a.top40Hits * 0.05f));
		a.reputation = Mathf.Clamp(0.25f + 0.70f * record, 0.20f, 0.97f);
	}

	/// <summary>
	/// Public recognition and the members' names, earned side by side through the live milestone and run-complete
	/// credits, with the live weekly decay between them, up to the first week of <paramref name="year"/>.
	/// </summary>
	private static void ReplayRecognition(SimulatedArtist a, List<PublicReleaseEntry> history, int year) {
		if (!ArtistRecognition.Observing) return;
		int previousWeek = -1;
		foreach (IGrouping<int, PublicReleaseEntry> releaseYear in history.GroupBy(e => e.year)) {
			var sides = releaseYear.ToList();
			for (int i = 0; i < sides.Count; i++) {
				int week = (releaseYear.Key - 1900) * 52 + i * 52 / sides.Count;
				if (previousWeek >= 0) ArtistRecognitionService.DecayActForWeeks(a, week - previousWeek);
				previousWeek = week;
				PublicReleaseEntry e = sides[i];
				int p = e.peakPosition;
				if (p <= 0) continue;
				ArtistRecognitionService.OnChartEntry(a);
				if (p <= 40) ArtistRecognitionService.OnTop40(a);
				if (p <= 10) ArtistRecognitionService.OnTop10(a);
				if (p == 1) ArtistRecognitionService.OnNumberOne(a);
				ArtistRecognitionService.CreditChartRun(a, p, e.weeksCharted, e.weeksInTopTen, e.markets);
			}
		}
		if (previousWeek >= 0) ArtistRecognitionService.DecayActForWeeks(a, Math.Max(0, (year - 1900) * 52 - previousWeek));
	}

	/// <summary>Esteem the charts don't measure: the critics' favourites and the standard-bearers carry standing.</summary>
	private static void DeriveEsteem(SimulatedArtist a, Spec spec, IRandom rng) {
		(float standingMin, float standingMax, float acclaimMin, float acclaimMax) = spec.Archetype switch {
			OpeningCareerArchetype.AdultStandardBearer => (0.15f, 0.35f, 0.45f, 0.70f),
			OpeningCareerArchetype.CriticsFavourite => (0.10f, 0.30f, 0.55f, 0.85f),
			OpeningCareerArchetype.FormerStar => (0.05f, 0.15f, 0.30f, 0.55f),
			OpeningCareerArchetype.EstablishedHitmaker => (0.02f, 0.08f, 0.25f, 0.50f),
			OpeningCareerArchetype.RegionalName => (0.01f, 0.05f, 0.25f, 0.50f),
			_ => (0f, 0.04f, 0.15f, 0.40f),
		};
		ArtistRecognitionService.AddCulturalStanding(a, standingMin + (float)rng.NextDouble() * (standingMax - standingMin));
		a.criticalAcclaim = Mathf.Max(a.criticalAcclaim, acclaimMin + (float)rng.NextDouble() * (acclaimMax - acclaimMin));
	}

	public static string Describe(OpeningCareerArchetype archetype, CareerState rung) => archetype switch {
		OpeningCareerArchetype.AdultStandardBearer => rung == CareerState.Superstar ? "One of the great names of popular music" : "A long-established star",
		OpeningCareerArchetype.CurrentHitmaker => rung == CareerState.Superstar ? "The hottest act in the country" : "A current hitmaker",
		OpeningCareerArchetype.EstablishedHitmaker => "A proven hitmaker",
		OpeningCareerArchetype.FormerStar => "A former star past their peak",
		OpeningCareerArchetype.CriticsFavourite => "A musicians' musician the critics admire",
		OpeningCareerArchetype.RegionalName => "A regional favourite",
		_ => "An act",
	};
}
