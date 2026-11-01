using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>
/// Media Atmosphere Phase 1 gate: the behavior fixtures of MediaAtmosphereDirective §16.1 that Phase 1 owns (catalog dates,
/// time-slot audiences, carriage, preemption, syndication, repeats, read-only views) plus the content tags and the trade
/// signal arithmetic. Runs on the real autoloads.
///   --path . SimTools/MediaAtmosphereChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints MEDIA_ATMOSPHERE_CHECKS_PASS / _FAIL.
/// </summary>
public partial class MediaAtmosphereChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static GameDate D(int y, int m, int d) => new(y, m, d);

	public override void _Ready() {
		try {
			Catalog();
			Audiences();
			Carriage();
			Syndication();
			ContentTags();
			Trade();
			Bookings();
			TelevisionEcosystem();
			Performance();
			Standards();
			EventNights();
			PressAndPrivacy();
			PlayerLoop();
			PlayerPress();
			PhaseTwoSave();
			FanArithmetic();
			FansInTheWorld();
			AiPlanner();
			PhaseThreeSave();
			RecognitionShadow();
			DemandAllocation();
			CausalPlayerCosts();
			DemandShadow();
			FollowingArithmetic();
			FollowingInTheWorld();
			MemberFameArithmetic();
			MemberFameInTheWorld();
			FameWritersArithmetic();
			FameWritersInTheWorld();
			EventHypeArithmetic();
			CriticsArithmetic();
			CriticsInTheWorld();
			GD.Print($"MEDIA_ATMOSPHERE_CHECKS_PASS checks={checks}");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("MEDIA_ATMOSPHERE_CHECKS_FAIL: " + e);
			GetTree().Quit(1);
		}
	}

	// Fixture 2: institutions only inside their dates.
	private void Catalog() {
		var shindig = MediaOutletCatalog.Get("shindig");
		Check(shindig.Runs(D(1966, 1, 8)) && !shindig.Runs(D(1966, 1, 13)) && !shindig.OnAir(D(1966, 2, 1)), "Shindig! ends with its January 8, 1966 episode");
		Check(!MediaOutletCatalog.Get("hullabaloo").OnAir(D(1966, 9, 5)) && MediaOutletCatalog.Get("hullabaloo").Runs(D(1966, 4, 11)),
			"Hullabaloo: on Monday April 11 1966, gone by September 1966");
		var bandstand = MediaOutletCatalog.Get("american_bandstand");
		Check(bandstand.Runs(D(1963, 8, 30)) && !bandstand.Runs(D(1963, 9, 2)) && bandstand.Runs(D(1963, 9, 7)), "Bandstand: last weekday Aug 30 1963, Saturdays from Sep 7");
		Check(bandstand.VersionOn(D(1964, 2, 1)).ProductionCityId == "philadelphia" && bandstand.VersionOn(D(1964, 2, 15)).ProductionCityId == "los_angeles",
			"Bandstand moves production to Los Angeles in February 1964");
		Check(!MediaOutletCatalog.Get("smothers_brothers").OnAir(D(1966, 12, 25)) && MediaOutletCatalog.Get("smothers_brothers").Runs(D(1967, 2, 5)),
			"Smothers Brothers premieres February 5, 1967");
		var press = MediaOutletCatalog.Press.ToDictionary(p => p.Id);
		Check(!press["tiger_beat"].Publishing(D(1965, 8, 31)) && press["tiger_beat"].Publishing(D(1965, 9, 1)), "No Tiger Beat before September 1965");
		Check(!press["rolling_stone"].Publishing(D(1967, 11, 8)) && press["rolling_stone"].Publishing(D(1967, 11, 9)), "No Rolling Stone before November 9, 1967");
		Check(press["village_voice"].Publishing(D(1960, 1, 1)) && press["sixteen"].Publishing(D(1960, 1, 1)), "The Voice and 16 exist in 1960");
		Check(press["music_vendor"].Publishing(D(1964, 9, 26)) && !press["record_world"].Publishing(D(1964, 9, 26)), "Music Vendor is still Music Vendor in September 1964");
		Check(MediaOutletCatalog.DisplayName(MediaOutletCatalog.Get("upbeat"), D(1965, 6, 5)) == "The Big 5 Show", "Upbeat is The Big 5 Show before 1966");
		int locals = MediaOutletCatalog.Programs.Count(p => p.Network == TvNetwork.Local);
		Check(MediaOutletCatalog.CityIds.All(c => MediaOutletCatalog.Programs.Any(p => p.HomeCityId == c)), $"Every player city has a local music show ({locals} local programs)");
	}

	private static TvProgram Probe(float[] attraction) => new() { Id = "probe", Name = "Probe", Network = TvNetwork.Local, Kind = TvProgramKind.LocalTeenDance,
		Attraction = attraction, GuestSlots = 3, GenreFit = MediaOutletCatalog.TeenFit };

	private static BroadcastExposure.Market City(GameDate date, int minute, float clearance = 1f) =>
		new("cleveland", "greatlakes", 2_000_000, TimeZoneBand.Eastern, clearance, date, minute);

	// Fixture 3: the hour changes who watches; season changes viewing, not sales.
	private void Audiences() {
		var program = Probe(new[] { 0.5f, 0.5f, 0.5f, 0.5f });
		GameDate tuesday = D(1962, 10, 9);
		var afternoon = BroadcastExposure.Estimate(program, 30, 0, 1, 0f, new[] { City(tuesday, 16 * 60 + 30) });
		var lateNight = BroadcastExposure.Estimate(program, 30, 0, 1, 0f, new[] { City(tuesday, 23 * 60) });
		double TeenShare(ExposureEstimate e) => e.AverageByGroup[1] / e.AverageByGroup.Sum();
		Check(TeenShare(afternoon) > 2 * TeenShare(lateNight), $"After-school slot is teen-heavy: teen share {TeenShare(afternoon):P0} vs {TeenShare(lateNight):P0} late night");
		Check(afternoon.AverageViewers < BroadcastExposure.Estimate(program, 30, 0, 1, 0f, new[] { City(tuesday, 20 * 60) }).AverageViewers,
			"A smaller after-school audience than primetime, but a younger one");
		var october = BroadcastExposure.Estimate(program, 60, 0, 1, 0f, new[] { City(D(1962, 10, 7), 20 * 60) });
		var july = BroadcastExposure.Estimate(program, 60, 0, 1, 0f, new[] { City(D(1962, 7, 8), 20 * 60) });
		Check(july.AverageViewers < october.AverageViewers, $"Summer evening viewing is lower ({july.AverageViewers:N0} vs {october.AverageViewers:N0})");
		var opening = BroadcastExposure.Estimate(program, 60, 0, 3, 0f, new[] { City(tuesday, 20 * 60) });
		var closing = BroadcastExposure.Estimate(program, 60, 2, 3, 0f, new[] { City(tuesday, 20 * 60) });
		Check(BroadcastExposure.SegmentRetention(2, 3, 22 * 60) < BroadcastExposure.SegmentRetention(0, 3, 22 * 60), "A closing number keeps fewer viewers than an opener");
		Check(closing.PeakViewers != opening.PeakViewers, "A closing segment has its own audience, not the episode peak");
		var drawn = BroadcastExposure.ProgramShare(program, ViewerGroup.Teen, tuesday, 20 * 60, 0.5f);
		var plain = BroadcastExposure.ProgramShare(program, ViewerGroup.Teen, tuesday, 20 * 60, 0f);
		Check(drawn <= plain * 1.10001f, "A guest's draw changes program share by at most 10%");
		Check(opening.ViewerMinutes > opening.UniqueReach && opening.PeakViewers <= 2_000_000, "Viewer-minutes, peak and unique reach stay distinct and bounded");
	}

	// Fixture 4: delayed West Coast carriage, preempted affiliates.
	private void Carriage() {
		Check(BroadcastExposure.LocalStart(CarriageRule.NetworkPrimetime, 20 * 60, TimeZoneBand.Pacific) == 20 * 60 &&
			BroadcastExposure.LocalStart(CarriageRule.NetworkPrimetime, 20 * 60, TimeZoneBand.Central) == 19 * 60,
			"Sunday 8 p.m. ET: 8 p.m. on a Pacific tape, 7 p.m. in Chicago");
		Check(BroadcastExposure.LocalStart(CarriageRule.NetworkSimultaneous, 13 * 60 + 30, TimeZoneBand.Pacific) >= 15 * 60,
			"A live Eastern afternoon teen show is taped for after school out West, not dropped into the lunch hour");
		var program = Probe(new[] { 0.5f, 1f, 0.5f, 0.5f });
		var pre = BroadcastExposure.Estimate(program, 30, 0, 1, 0f, new[] { City(D(1962, 10, 9), 16 * 60, 0f) });
		Check(pre.UniqueReach == 0 && pre.MarketsPreempted == 1 && pre.MarketsCleared == 0, "A preempted market gets no exposure");
		var regions = ChartManager.Instance.GetAllRegions();
		var palace = MediaOutletCatalog.Get("hollywood_palace");
		var sullivan = MediaOutletCatalog.Get("ed_sullivan");
		Check(regions.All(r => BroadcastExposure.Clearance(TvNetwork.ABC, r) <= BroadcastExposure.Clearance(TvNetwork.CBS, r)) &&
			regions.Any(r => BroadcastExposure.Clearance(TvNetwork.ABC, r) < 0.6f), "ABC clears fewer stations than CBS (bandstandReach prior)");
		var ed = BroadcastExposure.Estimate(sullivan, 60, 1, 2, 0.1f, BroadcastExposure.Markets(sullivan, sullivan.VersionOn(D(1965, 10, 3)), D(1965, 10, 3), regions));
		double population = regions.Sum(r => r.population) * 1e6;
		Check(ed.UniqueReach > 15e6 && ed.UniqueReach < population * 0.5, $"A routine Sullivan closing spot: {ed.UniqueReach / 1e6:F1}M reached of {population / 1e6:F0}M, not 73M");
	}

	// Fixture 5: one tape, dated regional airings; reruns and repeats never exceed population.
	private void Syndication() {
		var upbeat = MediaOutletCatalog.Get("upbeat");
		var airings = BroadcastExposure.SyndicatedAirings(upbeat, D(1967, 7, 1));
		Check(airings.Count >= 6 && airings.Select(a => a.Date).Distinct().Count() > 1 && airings.All(a => a.Date > D(1967, 7, 1)),
			$"One Upbeat tape airs in {airings.Count} markets on {airings.Select(a => a.Date).Distinct().Count()} later dates");
		Check(BroadcastExposure.SyndicatedAirings(upbeat, D(1965, 7, 3)).Count == 0, "No syndication before the tape travels");
		double pop = 10e6, reach = 0;
		for (int i = 0; i < 50; i++) reach = BroadcastExposure.CombineReach(reach, 3e6, pop);
		Check(reach <= pop && reach > 9.9e6, "Fifty airings to the same people approach, never exceed, the population");
		var program = Probe(new[] { 0.5f, 1f, 0.5f, 0.5f });
		var fresh = BroadcastExposure.Estimate(program, 30, 0, 1, 0f, new[] { City(D(1962, 10, 9), 16 * 60 + 30) });
		var repeat = BroadcastExposure.Estimate(program, 30, 0, 1, 0f, new[] { City(D(1962, 10, 9), 16 * 60 + 30) }, novelty: 0.55f);
		Check(repeat.UniqueReach < fresh.UniqueReach, "A repeat reaches fewer new viewers than the original airing");
	}

	private void ContentTags() {
		List<string> Tags(string title, SongLyricMode[] modes = null, SongContentContext context = SongContentContext.Secular) =>
			SongContentTags.Classify(title, modes, context).Select(f => f.Tag + ":" + f.Severity).ToList();
		Check(Tags("Whiskey River").SequenceEqual(new[] { "alcohol:Mild" }), "\"Whiskey River\" reads as an alcohol reference");
		Check(Tags("Honky Tonk Angel").Count == 1, "A phrase is one finding, not one per word");
		Check(Tags("Freedom Train", new[] { SongLyricMode.TopicalTestimony }).SequenceEqual(new[] { "political-protest:Marked" }) &&
			Tags("Freedom Train").SequenceEqual(new[] { "political-protest:Mild" }), "Topical testimony turns a protest word into a statement");
		Check(Tags("Devil Woman", context: SongContentContext.Sacred).Count == 0 && Tags("Devil Woman").Count == 1, "A sacred song's devil is its subject, not a provocation");
		Check(Tags("Sinner's Prayer").Count == 0 && Tags("Hello Mary Lou").Count == 0 && Tags("Ginny Come Lately").Count == 0, "Whole words only: no tag inside Sinner, Ginny");
		Check(Tags("Teen Angel").Count == 0, "No title evidence means no tag (unknown is not clean, and not dirty)");
		var song = new SongComposition { songId = "probe", title = "Moonshine Blues" };
		SongContentTags.Author(song);
		Check(song.contentTagIds.SequenceEqual(new[] { "alcohol" }) && song.contentTagsBasis == SongContentTags.Basis && song.contentTagEvidence[0].Contains("Moonshine"),
			"Authoring records tag, basis and evidence");
		song.title = "Peace Train";
		Check(SongContentTags.Read(song).Count == 0, "A retitled song is read fresh, never from stale tags");
		var catalog = Enum.GetValues<Genre>().SelectMany(CompositionCatalogService.GetCatalogForGenre).Distinct().ToList();
		int authored = catalog.Count(s => s.contentTagsBasis == SongContentTags.Basis);
		int tagged = catalog.Count(s => s.contentTagIds.Length > 0);
		foreach (var s in catalog.Where(s => s.contentTagIds.Length > 0).Take(6)) GD.Print($"  tagged: \"{s.title}\" -> {string.Join(", ", s.contentTagEvidence)}");
		Check(authored > 0 && tagged < authored, $"Catalog songs are authored on registration: {authored} read, {tagged} with a tag");
	}

	private void Trade() {
		Check(PlayerDesk.PressVoice(0.25f, 0f) == 0.25f && PlayerDesk.PressVoice(0f, 0.22f) == 0.22f, "A pick alone or an ad alone keeps its old value");
		float both = PlayerDesk.PressVoice(0.25f, 0.22f);
		Check(both > 0.25f && both < 0.25f + 0.22f, $"A pick plus an ad saturates: {both:F3} < {0.25f + 0.22f:F2}");
		Check(PlayerDesk.BreadthFor(0) == 0 && PlayerDesk.BreadthFor(1) == 1 && PlayerDesk.BreadthFor(7) > 1.8f, "Breakout evidence grows with breadth");
		Check(PlayerDesk.AllowanceFor(0) == 1 && PlayerDesk.AllowanceFor(2) == 1 && PlayerDesk.AllowanceFor(3) == 2 && PlayerDesk.AllowanceFor(7) == 4,
			"Extra stranger calls only for three or more markets breaking at once; seven is a flood of four");
	}

	// Bookings on the real chart: dates honored, rivals only, one place per act per day, reads draw nothing.
	private void Bookings() {
		var clock = TimeManager.Instance;
		for (int i = 0; i < 21; i++) clock.EndDay();
		var appearances = MediaAtmosphereService.Appearances.Where(a => a.RepeatOfId == null).ToList();
		Check(appearances.Count > 20, $"{appearances.Count} appearances booked over three weeks");
		Check(appearances.All(a => MediaOutletCatalog.Get(a.ProgramId).Runs(a.Date)), "Every booking falls on a date its program actually ran");
		Check(appearances.All(a => ChartManager.Instance.GetAllRecords().FirstOrDefault(r => r.baseRecord.recordId == a.RecordId)?.baseRecord.isPlayerOwned != true),
			"Observe mode books rival acts only");
		Check(appearances.GroupBy(a => (a.ArtistId, a.Date)).All(g => g.Count() == 1), "No act is booked in two places on one day");
		Check(MediaAtmosphereService.Tallies.Count > 0 && MediaAtmosphereService.Appearances.SelectMany(a => a.Airings).Any(x => x.Tallied),
			$"Aired appearances enter the act tallies ({MediaAtmosphereService.Tallies.Count} acts)");

		var desk = PlayerDesk.Instance;
		if (desk.Label == null) {
			// The fictional local shows are keyed per world, so Cleveland may be between shows; found the label in a town
			// with a local dance show on the air through the player-loop fixture, Cleveland when it has one.
			bool airing(TvProgram p) => p.Network == TvNetwork.Local && p.Kind == TvProgramKind.LocalTeenDance && p.HomeCityId != null
				&& p.Runs(clock.CurrentDate) && p.Runs(clock.CurrentDate.AddDays(90));
			string town = MediaOutletCatalog.Programs.Where(airing).Select(p => p.HomeCityId)
				.OrderBy(c => c == "cleveland" ? 0 : 1).ThenBy(c => c, StringComparer.Ordinal).FirstOrDefault() ?? "cleveland";
			desk.FoundLabel("Media Checks", town, FoundingArchetype.ExMusician, out _);
		}
		int before = MediaAtmosphereService.Appearances.Count;
		GameRandom.Seed(4242); desk.TelevisionColumn(clock.CurrentDate); RolodexDirectory.Cards(desk, RolodexRole.Press); uint r1 = GameRandom.Randi();
		GameRandom.Seed(4242); uint r2 = GameRandom.Randi();
		Check(r1 == r2 && MediaAtmosphereService.Appearances.Count == before, "Reading the paper and the Rolodex draws no RNG and books nothing");
		var column = desk.TelevisionColumn(clock.CurrentDate);
		Check(column.SequenceEqual(desk.TelevisionColumn(clock.CurrentDate)), "The television column reads the same twice");
		foreach (string line in column) GD.Print("  paper: " + line);
		var cards = RolodexDirectory.Cards(desk, RolodexRole.Press);
		// Relational: which local show is on the air in the label's town depends on the keyed fictional catalog.
		bool localOnAir = MediaOutletCatalog.Programs.Any(p => p.Network == TvNetwork.Local && (p.HomeCityId == desk.Label.homeCityId || p.HomeCityId == desk.CurrentCityId)
			&& p.VersionOn(clock.CurrentDate) != null);
		Check(cards.Any(c => c.Title == "Producer") == localOnAir && cards.Any(c => c.Title == "Talent booker"),
			$"PRESS & TV lists a local producer exactly when a local show is on the air (local on air: {localOnAir}; {cards.Count} cards: {string.Join("; ", cards.Select(c => c.Title + " @ " + c.Firm))})");
		foreach (var c in cards.Take(4)) GD.Print($"  card: {c.Name}, {c.Title}, {c.Firm} — {c.Facts.FirstOrDefault().Text}");

		var opts = new JsonSerializerOptions { IncludeFields = true };
		var world = new WorldSaveData();
		MediaAtmosphereService.CaptureWorld(world);
		string json = JsonSerializer.Serialize(world.Media, opts);
		var back = JsonSerializer.Deserialize<MediaSaveData>(json, opts);
		Check(back.Appearances.Count == MediaAtmosphereService.Appearances.Count && back.BookedThroughDay == world.Media.BookedThroughDay &&
			back.Appearances.Zip(MediaAtmosphereService.Appearances).All(p => p.First.Id == p.Second.Id && p.First.Airings.Count == p.Second.Airings.Count &&
				p.First.Airings.Zip(p.Second.Airings).All(x => x.First.Tallied == x.Second.Tallied && Math.Abs(x.First.Exposure.UniqueReach - x.Second.Exposure.UniqueReach) < 1e-6)),
			$"Media state survives a save round trip ({json.Length / 1024} KB)");
		Check(back.Appearances.Zip(MediaAtmosphereService.Appearances).All(p => p.First.Lane == p.Second.Lane && p.First.Song == p.Second.Song && p.First.Title == p.Second.Title),
			"Each appearance's bill lane and the song it sang survive the round trip");
	}

	// The television ecosystem (SimTools/MediaTvHeadlinerBookingDesign.md): dated programs, bills with standing and comedy
	// lanes, and what a standing act sings. Runs on the three weeks Bookings() just booked.
	private void TelevisionEcosystem() {
		var tonight = MediaOutletCatalog.Get("tonight_paar");
		Check(tonight.Runs(D(1962, 3, 30)) && !tonight.OnAir(D(1962, 4, 2)) && MediaOutletCatalog.Get("tonight_interim").Runs(D(1962, 4, 2))
			&& !MediaOutletCatalog.Get("tonight_carson").OnAir(D(1962, 9, 28)) && MediaOutletCatalog.Get("tonight_carson").Runs(D(1962, 10, 1)),
			"Tonight: Paar to March 30 1962, guest hosts, Carson from October 1 1962");
		Check(!MediaOutletCatalog.Get("hootenanny").OnAir(D(1963, 4, 5)) && MediaOutletCatalog.Get("hootenanny").Runs(D(1963, 4, 6)), "No Hootenanny before April 6, 1963");
		var dean = MediaOutletCatalog.Get("jimmy_dean");
		Check(dean.Runs(D(1965, 9, 2)) && dean.Runs(D(1965, 9, 10)) && !dean.OnAir(D(1966, 4, 8)), "The Jimmy Dean Show moves to Fridays in September 1965 and ends April 1, 1966");
		var como = MediaOutletCatalog.Get("perry_como");
		int weekly = Enumerable.Range(0, 84).Count(i => como.Runs(D(1961, 1, 1).AddDays(i)));
		int monthly = Enumerable.Range(0, 84).Count(i => como.Runs(D(1964, 1, 1).AddDays(i)));
		Check(weekly >= 11 && monthly is >= 2 and <= 3, $"Como is weekly to 1963, then about monthly ({weekly} vs {monthly} episodes in twelve weeks)");
		Check(MediaOutletCatalog.Programs.All(p => p.Bills.All(b => b.Total > 0)) && MediaOutletCatalog.Programs.Count(p => p.BooksNationally) >= 30,
			$"Every bill has room for a guest; {MediaOutletCatalog.Programs.Count(p => p.BooksNationally)} programs book nationally");
		var sullivan = MediaOutletCatalog.Get("ed_sullivan");
		Check(sullivan.BillOn(D(1961, 3, 5)).Standing == 2 && sullivan.BillOn(D(1961, 3, 5)).Chart == 1 && sullivan.BillOn(D(1965, 3, 7)).Chart == 2,
			"Sullivan's bill: one current record and two names in 1961, two current records from 1964");

		var booked = MediaAtmosphereService.Appearances.Where(a => a.RepeatOfId == null).ToList();
		var standing = booked.Where(a => a.Lane == BillLane.Standing).ToList();
		var comedy = booked.Where(a => a.Lane == BillLane.Comedy).ToList();
		foreach (var a in standing.Concat(comedy).Take(10))
			GD.Print($"  bill: {a.Date.ToShortString()} {MediaOutletCatalog.Get(a.ProgramId).Name}: {a.ArtistName} ({a.Lane}) sings \"{a.Title}\" [{a.Song}]");
		Check(standing.Count > 0 && standing.All(a => ArtistRecognitionService.EffectiveRecognition(ArtistManager.Instance.GetArtist(a.ArtistId))
				>= MediaOutletCatalog.Get(a.ProgramId).StandingFloor - 0.05f),
			$"Standing spots go to acts the public knows ({standing.Count} in three weeks)");
		Check(standing.Any(a => a.ChartPosition <= 0 || a.ChartPosition > 40), "A name with no current top-40 record gets a standing spot");
		Check(standing.Any(a => a.Song != PerformedSong.Record) && standing.Where(a => a.Song != PerformedSong.Record).All(a => a.StandardsQueryId == null),
			"Standing acts sing standards, covers and old hits, which standards doesn't review as the record");
		foreach (var a in booked.Where(a => a.ProgramId == "ed_sullivan")) {
			var act = ArtistManager.Instance.GetArtist(a.ArtistId);
			GD.Print($"  sullivan: {a.Date.ToShortString()} {a.ArtistName} {a.Lane} prep={a.Preparation} outcome={a.Performance?.Outcome} fault={a.Performance?.Fault} " +
				$"musical={MediaPerformance.MusicalSkill(a.Method, act):F2} presence={MediaPerformance.Presence(act):F2} members={act?.members.Count(m => m.isActive)}");
		}
		var comics = ArtistManager.Instance.GetAllArtists().Where(x => x.primaryGenre == Genre.Comedy).ToList();
		var comicRecords = ChartManager.Instance.GetAllRecords().Where(r => r.baseRecord?.primaryGenre == Genre.Comedy).ToList();
		GD.Print($"  comedy: {comics.Count} comedy acts, {comics.Count(x => x.isActive && !string.IsNullOrEmpty(x.labelId))} signed, " +
			$"recognition max {comics.Select(ArtistRecognitionService.EffectiveRecognition).DefaultIfEmpty(0).Max():F3}, " +
			$"{comics.Count(x => ArtistRecognitionService.EffectiveRecognition(x) >= 0.2f)} at 0.20+; {comicRecords.Count} live comedy records " +
			$"by {comicRecords.Select(r => r.baseRecord.artistId).Distinct().Count()} acts");
		Check(comedy.All(a => a.Song == PerformedSong.Routine && (a.Genre == Genre.Comedy || ArtistManager.Instance.GetArtist(a.ArtistId)?.primaryGenre == Genre.Comedy)),
			$"Comedy spots go to comedians who make records ({comedy.Count} in three weeks)");
		Check(booked.Where(a => a.Lane == BillLane.Chart).All(a => a.Song == PerformedSong.Record), "A chart booking sings its record");
		Check(booked.GroupBy(a => a.EpisodeId).All(g => g.Count(a => a.Lane == BillLane.Standing) <= MediaOutletCatalog.Get(g.First().ProgramId).BillOn(g.First().Date).Standing),
			"No episode books more standing guests than its bill has");
	}

	// ---- Phase 2 --------------------------------------------------------------------------------------------------

	private static SimulatedArtist ProbeAct(string id, float vocal, float hands, float presence, float reliability) {
		var a = new SimulatedArtist { artistId = id, stageName = id, primaryGenre = Genre.TeenPop };
		a.members.Add(new Musician { personId = id + "-lead", firstName = "Lead", lastName = id, isLeadVocalist = true, vocalControl = vocal, vocalPower = vocal,
			diction = vocal, instrumentalSkill = hands, technicalSkill = hands, stagePresence = presence, reliability = reliability, ego = 0.5f });
		for (int i = 0; i < 2; i++)
			a.members.Add(new Musician { personId = id + "-" + i, firstName = "Player", lastName = id + i, instrumentalSkill = hands, technicalSkill = hands,
				stagePresence = presence, reliability = reliability, ego = 0.5f });
		return a;
	}

	// Fixture 6: a strong live act usually delivers; a weak unprepared singer has a worse tail; a failed mime is a picture problem.
	private void Performance() {
		var strong = ProbeAct("strong", 0.85f, 0.85f, 0.8f, 0.85f);
		var weak = ProbeAct("weak", 0.25f, 0.4f, 0.3f, 0.35f);
		const int n = 800;
		int Bad(SimulatedArtist a, MediaPreparation p, PerformanceMethod m, BroadcastMethod b) =>
			Enumerable.Range(0, n).Count(i => MediaPerformance.Resolve("probe-" + i, m, b, a, p).Outcome <= PerformanceOutcome.Weak);
		int strongBad = Bad(strong, MediaPreparation.Standard, PerformanceMethod.LiveBandAndVocal, BroadcastMethod.Live);
		int weakBad = Bad(weak, MediaPreparation.Minimal, PerformanceMethod.LiveBandAndVocal, BroadcastMethod.Live);
		int weakPrepared = Bad(weak, MediaPreparation.Extra, PerformanceMethod.LiveBandAndVocal, BroadcastMethod.Live);
		Check(strongBad < n * 0.05 && weakBad > 3 * strongBad, $"Prepared professionals usually deliver ({strongBad}/{n} bad); an unprepared weak singer has a tail ({weakBad}/{n})");
		Check(weakPrepared < weakBad, $"Rehearsal trims the avoidable errors ({weakPrepared} vs {weakBad}), not all of them");
		int weakTaped = Bad(weak, MediaPreparation.Minimal, PerformanceMethod.LiveBandAndVocal, BroadcastMethod.Taped);
		Check(weakTaped < weakBad, $"Tape forgives a bad take the producer lets them reshoot ({weakTaped} vs {weakBad} live)");
		// A network variety production's own camera and dress rehearsal: a weak unrehearsed act still slips, rarely collapses.
		float variety = MediaOutletCatalog.Get("ed_sullivan").ProductionReadiness;
		int Collapses(float production) => Enumerable.Range(0, n).Count(i => MediaPerformance.Resolve("probe-" + i, PerformanceMethod.LiveVocalOverTrack,
			BroadcastMethod.Live, weak, MediaPreparation.Minimal, production).Outcome == PerformanceOutcome.Breakdown);
		int bare = Collapses(0f), rehearsed = Collapses(variety);
		Check(variety > 0f && rehearsed <= n * 0.01 && rehearsed < bare / 5,
			$"A rehearsed network production rarely sees a collapse ({rehearsed}/{n} for a weak act, {bare}/{n} with no production rehearsal)");
		int Slips(MediaPreparation p) => Enumerable.Range(0, n).Count(i => MediaPerformance.Resolve("probe-" + i, PerformanceMethod.LiveVocalOverTrack,
			BroadcastMethod.Live, weak, p, variety).Outcome <= PerformanceOutcome.Weak);
		int slipsBare = Slips(MediaPreparation.Minimal), slipsReady = Slips(MediaPreparation.Extra);
		Check(slipsReady < slipsBare, $"On a network show the label's own rehearsal still matters ({slipsReady} vs {slipsBare} weak or worse of {n})");
		var mimes = Enumerable.Range(0, n).Select(i => MediaPerformance.Resolve("mime-" + i, PerformanceMethod.Mime, BroadcastMethod.Live, weak, MediaPreparation.Minimal)).ToList();
		Check(mimes.All(r => r.SoundFromMaster) && mimes.Any(r => r.Outcome <= PerformanceOutcome.Weak),
			$"A mime's sound is the master; its failures are on camera ({mimes.Count(r => r.Outcome <= PerformanceOutcome.Weak)} of {n})");
		var greatHandsWeakVoice = ProbeAct("hands", 0.2f, 0.95f, 0.6f, 0.8f);
		var goodVoice = ProbeAct("voice", 0.8f, 0.4f, 0.6f, 0.8f);
		Check(MediaPerformance.MusicalSkill(PerformanceMethod.LiveVocalOverTrack, greatHandsWeakVoice) < MediaPerformance.MusicalSkill(PerformanceMethod.LiveVocalOverTrack, goodVoice),
			"A strong guitarist doesn't rescue a weak live singer");
		float before = weak.members.Sum(m => m.vocalControl + m.stagePresence + m.reliability + m.personalRecognition);
		var again = MediaPerformance.Resolve("probe-7", PerformanceMethod.LiveBandAndVocal, BroadcastMethod.Live, weak, MediaPreparation.Minimal);
		Check(before == weak.members.Sum(m => m.vocalControl + m.stagePresence + m.reliability + m.personalRecognition)
			&& again.Outcome == MediaPerformance.Resolve("probe-7", PerformanceMethod.LiveBandAndVocal, BroadcastMethod.Live, weak, MediaPreparation.Minimal).Outcome,
			"Resolving a segment writes nothing to the musicians and always resolves the same way");
	}

	// Fixture 7: an edit, another song, an honest refusal and a broken promise are four different consequences.
	private void Standards() {
		var sullivan = MediaOutletCatalog.Get("ed_sullivan");
		var local = MediaOutletCatalog.Programs.First(p => p.Network == TvNetwork.Local && p.Method == PerformanceMethod.Mime);
		Check(MediaStandards.Strictness(sullivan, SongContentTags.DrugReference, ContentSeverity.Marked, D(1964, 2, 9), null) >= 1f &&
			MediaStandards.Strictness(local, SongContentTags.Alcohol, ContentSeverity.Mild, D(1964, 2, 9), "greatlakes") == 0f,
			"Family variety always queries a marked drug reference; a local dance show lets a mild drinking word through");
		var q = new StandardsQuery { Id = "probe-q", ProgramId = "ed_sullivan", ProducerSympathetic = false };
		var mimeOptions = MediaStandards.Options(q, PerformanceMethod.Mime, true).ToList();
		Check(!mimeOptions.Contains(StandardsResponse.ComplyEdit) && !mimeOptions.Contains(StandardsResponse.PromiseThenDefy) && mimeOptions.Contains(StandardsResponse.AlternateSong),
			"A fixed mime track can't sing a different lyric, or defy one");

		var state = MediaAtmosphereService.State;
		void Run(string id, StandardsResponse response, BroadcastMethod broadcast) {
			var query = new StandardsQuery { Id = "probe-std-" + id, ProgramId = "ed_sullivan", ArtistId = "probe-act-" + id, ArtistName = "Probe " + id, Title = "Mary Jane",
				Tag = SongContentTags.DrugReference, Severity = ContentSeverity.Marked, AirDate = D(1960, 3, 6), Authority = MediaStandards.Authority(sullivan) };
			var version = new BroadcastScheduleVersion { Broadcast = broadcast };
			MediaAtmosphereService.Settle(query, response, sullivan, version, null, null);
			state.StandardsQueries.Add(query);
			var a = new MediaAppearance { Id = "probe-app-" + id, ProgramId = "ed_sullivan", ArtistId = query.ArtistId, ArtistName = query.ArtistName, Title = query.Title,
				Date = query.AirDate, StandardsQueryId = query.Id, Broadcast = broadcast, Method = PerformanceMethod.LiveVocalOverTrack,
				Cut = query.Resolution == StandardsResolution.Cut, Performance = new MediaPerformanceResult { Outcome = PerformanceOutcome.FlatAdequate } };
			if (query.Resolution != StandardsResolution.Withdrawn) MediaAtmosphereService.AfterAir(a, new MediaAiring { Date = a.Date, Exposure = new ExposureEstimate() });
		}
		int day = BroadcastExposure.DayIndex(D(1960, 3, 6));
		Run("edit", StandardsResponse.ComplyEdit, BroadcastMethod.Live);
		Run("song", StandardsResponse.AlternateSong, BroadcastMethod.Live);
		Run("withdraw", StandardsResponse.Withdraw, BroadcastMethod.Live);
		Run("defy", StandardsResponse.PromiseThenDefy, BroadcastMethod.Live);
		var edit = MediaAtmosphereService.ProgramMemoryFor("ed_sullivan", "probe-act-edit");
		var song = MediaAtmosphereService.ProgramMemoryFor("ed_sullivan", "probe-act-song");
		var defy = MediaAtmosphereService.ProgramMemoryFor("ed_sullivan", "probe-act-defy");
		Check(edit.Delivered == 1 && edit.ExcludedUntilDay < day && song.Delivered == 1 && song.ExcludedUntilDay < day, "An agreed edit and another song both keep the act welcome");
		Check(MediaAtmosphereService.ProgramMemoryFor("ed_sullivan", "probe-act-withdraw") == null && state.StandardsQueries.First(x => x.Id == "probe-std-withdraw").Resolution == StandardsResolution.Withdrawn,
			"An honest refusal loses the date and leaves no breach");
		float[] att = MediaAtmosphereService.AttitudeOf("probe-act-defy");
		Check(defy.Breaches == 1 && defy.ExcludedUntilDay > day + 1000 && att[0] > 0 && att[1] < 0 &&
			MediaAtmosphereService.Controversies.Any(c => c.ArtistId == "probe-act-defy" && c.Verified),
			"A promise broken on a live microphone: the program shuts its door, the artistic audience warms, the family audience cools, and it is a public incident");
		var taped = new StandardsQuery { Id = "probe-taped" };
		MediaAtmosphereService.Settle(taped, StandardsResponse.PromiseThenDefy, sullivan, new BroadcastScheduleVersion { Broadcast = BroadcastMethod.Taped }, null, null);
		Check(taped.Resolution is StandardsResolution.Cut or StandardsResolution.Defied, $"On tape a broken promise is usually caught and cut ({taped.Resolution})");
		var queries = MediaAtmosphereService.StandardsQueries.Where(x => !x.Id.StartsWith("probe")).ToList();
		Check(MediaAtmosphereService.Appearances.Where(a => a.StandardsQueryId != null).All(a => queries.Any(x => x.Id == a.StandardsQueryId))
			&& MediaAtmosphereService.Appearances.Where(a => a.Cut).All(a => a.Airings.All(x => x.Exposure.UniqueReach == 0)),
			$"Real bookings carry their queries ({queries.Count}: {string.Join(", ", queries.GroupBy(x => x.Resolution).Select(g => g.Key + " " + g.Count()))}); a cut segment reaches nobody");
		foreach (var x in queries.Take(3)) GD.Print($"  standards: {x.ArtistName} \"{x.Title}\" on {x.ProgramId}: {x.Evidence} -> {x.Outcome}");
	}

	// The Beatles-night question: an event is possible with the right conjunction, and nothing routine gets near it.
	private void EventNights() {
		var sullivan = MediaOutletCatalog.Get("ed_sullivan");
		GameDate night = D(1964, 2, 9);
		var regions = ChartManager.Instance.GetAllRegions();
		var markets = BroadcastExposure.Markets(sullivan, sullivan.VersionOn(night), night, regions);
		double population = regions.Sum(r => r.population) * 1e6;
		ExposureEstimate At(float hype) {
			var (lift, tune) = MediaAtmosphereService.EventDraw(hype, sullivan);
			return BroadcastExposure.Estimate(sullivan, 60, 1, 2, 0.1f, markets, 1f, lift, tune);
		}
		var routine = At(0f); var hot = At(0.45f); var big = At(0.7f); var peak = At(1f);
		GD.Print($"  Sullivan, Feb 9 1964, closing spot: routine {routine.UniqueReach / 1e6:F1}M, hype .45 {hot.UniqueReach / 1e6:F1}M, hype .7 {big.UniqueReach / 1e6:F1}M, " +
			$"hype 1.0 {peak.UniqueReach / 1e6:F1}M of {population / 1e6:F0}M (peak simultaneous {peak.PeakViewers / 1e6:F1}M)");
		Check(MediaAtmosphereService.EventDraw(0.24f, sullivan) == (0f, 0f), "Below the hype floor an appearance is routine: no event draw at all");
		Check(peak.UniqueReach > 2.2 * routine.UniqueReach && peak.UniqueReach > population * 0.28 && peak.UniqueReach < population * 0.6,
			$"A maximal event night reaches {peak.UniqueReach / population:P0} of the country ({peak.UniqueReach / 1e6:F0}M, {peak.UniqueReach / routine.UniqueReach:F1}x a routine closing spot), and never the whole population");
		Check(routine.UniqueReach < hot.UniqueReach && hot.UniqueReach < big.UniqueReach && big.UniqueReach < peak.UniqueReach, "Event audiences grow smoothly with hype");
		var local = MediaOutletCatalog.Programs.First(p => p.Network == TvNetwork.Local);
		Check(MediaAtmosphereService.EventDraw(1f, local) == (0f, 0f), "A local dance show has no event night");
		Check(BroadcastExposure.ProgramShare(sullivan, ViewerGroup.Adult, night, 20 * 60, 5f) <= BroadcastExposure.ProgramShare(sullivan, ViewerGroup.Adult, night, 20 * 60, 0f) * 1.10001f,
			"A routine guest's draw is still capped at 10%");
		Check(BroadcastExposure.TunedIn(0.62f, 1f) < 0.92f && BroadcastExposure.TunedIn(0.62f, 0.35f) > 0.62f && BroadcastExposure.TunedIn(0.95f, 0.35f) >= 0.95f,
			"Tune-in draws from people not watching, bounded below everyone");
		var chart = ChartManager.Instance.GetCurrentChart();
		var precursors = chart.Where(r => r?.baseRecord != null)
			.Select(r => MediaAtmosphereService.Precursor(r, chart, regions.Count, CompetitorManager.Instance?.RegionalBreakoutDealThreshold ?? 0.2f)).ToList();
		GD.Print($"  event precursors on today's chart: max {precursors.DefaultIfEmpty(0).Max():F2}, records over the hold bar {precursors.Count(p => p >= 0.3f)}; holds placed {MediaAtmosphereService.Holds.Count}");
	}

	// Fixture 8 and the press machinery: no private state leaks, budgets hold, roots are counted once.
	private void PressAndPrivacy() {
		var chart = ChartManager.Instance.GetCurrentChart().Where(r => r?.baseRecord?.artistId != null).ToList();
		int marked = 0;
		foreach (var r in chart) {
			var artist = ArtistManager.Instance.GetArtist(r.baseRecord.artistId);
			if (artist == null) continue;
			foreach (var m in artist.members) { m.substanceLoad = 1f; marked++; }
		}
		Check(!Enum.GetNames<EvidenceSource>().Any(n => n.Contains("Private") || n.Contains("Musician") || n.Contains("Substance")), "No evidence source can carry private state");
		var clock = TimeManager.Instance;
		for (int i = 0; i < 28; i++) clock.EndDay();
		var stories = MediaAtmosphereService.Stories.Where(s => s.Status == StoryStatus.Scheduled && !s.Id.StartsWith("probe")).ToList();
		Check(stories.Count > 30, $"The press ran {stories.Count} items in four weeks ({string.Join(", ", stories.GroupBy(s => MediaOutletCatalog.GetPress(s.OutletId).Family).Select(g => g.Key + " " + g.Count()))})");
		foreach (var s in stories.Where(s => s.IssueDay <= BroadcastExposure.DayIndex(clock.CurrentDate)).OrderByDescending(s => s.Interest).Take(6))
			GD.Print($"  press: {MediaOutletCatalog.GetPress(s.OutletId).Name}, {MediaAtmosphereService.FromDayIndex(s.IssueDay).ToShortString()}: {MediaPress.Sentence(MediaPress.Headline(s))} {MediaPress.Body(s)}");
		Check(stories.All(s => s.Evidence.All(e => !e.Fact.Contains("substance") && !e.Fact.Contains("addict") && !e.Fact.Contains("drug use")))
			&& stories.Where(s => s.Angle == StoryAngle.Controversy).All(s => MediaAtmosphereService.StandardsQueries.Any(q => s.RootId.EndsWith(q.Id) && q.Resolution == StandardsResolution.Defied)),
			$"With {marked} charting musicians at maximum private substance load, no item reports it; every controversy traces to a witnessed broadcast");
		Check(stories.GroupBy(s => (s.OutletId, s.RootId)).All(g => g.Count() == 1), "One item per story root per outlet: repeated coverage of one event is one story");
		bool budgets = stories.Where(s => s.Angle is not StoryAngle.Correction and not StoryAngle.OrdersFollowUp)
			.GroupBy(s => (s.OutletId, MediaOutletCatalog.GetPress(s.OutletId).Cadence == PressCadence.Daily ? s.IssueDay / 7 : s.IssueDay))
			.All(g => g.Count() <= MediaOutletCatalog.GetPress(g.Key.OutletId).Slots);
		Check(budgets, "No issue carries more music items than its page budget");
		Check(stories.All(s => MediaOutletCatalog.GetPress(s.OutletId).Publishing(MediaAtmosphereService.FromDayIndex(s.IssueDay))),
			"Every item runs in an outlet that existed that day (no Tiger Beat, Crawdaddy! or Rolling Stone in 1960)");
		int mainstream = stories.Count(s => MediaOutletCatalog.GetPress(s.OutletId).Family == PressFamily.Mainstream);
		foreach (var s in stories.Where(s => MediaOutletCatalog.GetPress(s.OutletId).Family == PressFamily.Mainstream))
			GD.Print($"  mainstream: {s.OutletId} {s.Angle} {s.ArtistName} program={s.ProgramId} root={s.RootId}");
		Check(mainstream <= 4, $"Mainstream magazines rarely cover popular music ({mainstream} items in four weeks)");
		var desk = PlayerDesk.Instance;
		GameRandom.Seed(777); desk.PressColumn(clock.CurrentDate); RolodexDirectory.Cards(desk, RolodexRole.Press); uint a = GameRandom.Randi();
		GameRandom.Seed(777); uint b = GameRandom.Randi();
		Check(a == b && desk.PressColumn(clock.CurrentDate).SequenceEqual(desk.PressColumn(clock.CurrentDate)), "Reading the press column and the press cards draws nothing and reads the same twice");
		foreach (string line in desk.PressColumn(clock.CurrentDate)) GD.Print("  paper: " + line);
	}

	// Fixture 1 and the label's television loop: a plausible local act gets a local date; a network show can still say no.
	private void PlayerLoop() {
		var desk = PlayerDesk.Instance;
		var clock = TimeManager.Instance;
		string home = desk.Label.homeCityId;
		string region = DistanceModel.GetCityById(home).parentRegionId;
		var locals = MediaAtmosphereService.PitchablePrograms(home, home, clock.CurrentDate).Where(p => p.Network == TvNetwork.Local && p.HomeCityId == home).ToList();
		Check(locals.Count > 0, $"The label's town has a local show to pitch ({string.Join(", ", locals.Select(p => p.Name))})");
		var records = ChartManager.Instance.GetAllRecords()
			.Where(r => r?.baseRecord != null && r.baseRecord.format == ReleaseFormat.Single && !r.baseRecord.isPlayerOwned && r.regionalData.TryGetValue(region, out var rd) && rd != null && rd.unitsSoldThisWeek > 0)
			.OrderByDescending(r => r.regionalData[region].unitsSoldThisWeek).ThenBy(r => r.baseRecord.recordId).Take(12).ToList();
		var uncharted = ChartManager.Instance.GetAllRecords().Where(r => r?.baseRecord != null && r.baseRecord.format == ReleaseFormat.Single && r.currentPosition <= 0 && r.unitsThisWeek > 0)
			.Take(8).ToList();
		foreach (var r in records) MediaAtmosphereService.PitchTelevision(desk, r, locals[0].Id, out _);
		foreach (var r in uncharted) MediaAtmosphereService.PitchTelevision(desk, r, "ed_sullivan", out _);
		for (int i = 0; i < 6; i++) clock.EndDay();
		var local = MediaAtmosphereService.Pitches.Where(p => p.ProgramId == locals[0].Id).ToList();
		var national = MediaAtmosphereService.Pitches.Where(p => p.ProgramId == "ed_sullivan").ToList();
		Check(local.Count(p => p.Status == PitchStatus.Offered) >= 2 && local.All(p => p.Status != PitchStatus.Sent),
			$"Records the town is buying get local dates: {local.Count(p => p.Status == PitchStatus.Offered)} offers, {local.Count(p => p.Status == PitchStatus.Declined)} declined");
		Check(national.Count > 0 && national.All(p => p.Status == PitchStatus.Declined), $"The Ed Sullivan Show declines {national.Count} uncharted records" +
			string.Concat(national.Where(p => p.Status != PitchStatus.Declined).Select(p => $"; not declined: {p.ArtistName} {p.Status} {p.Reason}")));
		foreach (var p in local.Take(3)) GD.Print($"  pitch: {p.ArtistName} -> {p.Status} {p.Reason} {(p.Status == PitchStatus.Offered ? MediaAtmosphereService.FromDayIndex(p.AirDay).ToShortString() + " reach " + MediaText.Audience(p.ReachHigh) : "")}");
		var offer = local.First(p => p.Status == PitchStatus.Offered);
		Check(offer.AirDay > MediaAtmosphereService.State.BookedThroughDay && offer.ReachHigh > 0 && offer.CostNote.Contains("estimate"),
			"An offer names an unbooked date, an audience range, and costs marked as estimates");
		var query = offer.QueryId == null ? null : MediaAtmosphereService.StandardsQueries.First(x => x.Id == offer.QueryId);
		if (query != null) foreach (var r in MediaAtmosphereService.StandardsOptions(query)) if (desk.AnswerStandardsQuery(query.Id, r, out _)) break;
		float cash = desk.Label.cashReserves;
		int hour = clock.CurrentHour;
		Check(desk.AnswerTelevisionOffer(offer.Id, true, MediaPreparation.Extra, out string msg) && offer.Status == PitchStatus.Confirmed, "Confirming the date: " + msg);
		Check(desk.Label.cashReserves == cash && clock.CurrentHour == hour, "Observe mode charges neither cash nor diary time");
		int air = offer.AirDay;
		for (int guard = 0; guard < 40 && BroadcastExposure.DayIndex(clock.CurrentDate) <= air + 1; guard++) clock.EndDay();
		var mine = MediaAtmosphereService.Appearances.FirstOrDefault(a => a.PlayerBooking && a.RecordId == offer.RecordId);
		Check(mine != null && mine.Reported && (mine.Cut || mine.Performance != null) && mine.Preparation == MediaPreparation.Extra && offer.Status == PitchStatus.Aired,
			$"The date aired and the office heard how it went: {mine?.Performance?.Account} ({mine?.Airings.FirstOrDefault()?.Exposure.UniqueReach:N0} reached)");
		var contact = MediaAtmosphereService.ContactFor(MediaAtmosphereService.ProducerKey(locals[0]));
		Check(contact != null && contact.Lines.Count >= 3, $"The producer remembers the label: \"{contact?.Lines.FirstOrDefault()}\"");
	}

	// Fixtures 9 and 10: a sheet is a submission, not a guarantee; figures get checked; photo terms have scope and expiry.
	private void PlayerPress() {
		var desk = PlayerDesk.Instance;
		var clock = TimeManager.Instance;
		int tradeBefore = desk.TradeSubmissions.Count;
		string home = desk.Label.homeCityId;
		var records = ChartManager.Instance.GetAllRecords().Where(r => r?.baseRecord != null && r.baseRecord.format == ReleaseFormat.Single && r.unitsThisWeek > 0)
			.OrderBy(r => r.baseRecord.recordId).Take(10).ToList();
		var daily = MediaOutletCatalog.GetPress("daily_" + home);
		foreach (var r in records) MediaAtmosphereService.PitchPress(desk, r, daily.Id, StoryAngle.Hometown, out _);
		for (int i = 0; i < 8; i++) clock.EndDay();
		var sheets = MediaAtmosphereService.Pitches.Where(p => p.Kind == PitchKind.Press && p.OutletId == daily.Id).ToList();
		Check(sheets.All(p => p.Status is PitchStatus.Placed or PitchStatus.Passed) && sheets.Any(p => p.Status == PitchStatus.Passed),
			$"A press sheet is a submission: {sheets.Count(p => p.Status == PitchStatus.Placed)} placed, {sheets.Count(p => p.Status == PitchStatus.Passed)} passed");
		Check(desk.TradeSubmissions.Count == tradeBefore, "Named press desks don't create or multiply trade-review submissions");

		var state = MediaAtmosphereService.State;
		int today = BroadcastExposure.DayIndex(clock.CurrentDate);
		var artist = ArtistManager.Instance.GetArtist(records[0].baseRecord.artistId);
		MediaStory Story(string id, PressFamily family, StoryAngle angle, int issue) {
			var outlet = MediaOutletCatalog.Press.First(o => o.Family == family && o.Publishing(clock.CurrentDate));
			var s = new MediaStory { Id = id, RootId = id, OutletId = outlet.Id, Angle = angle, Status = StoryStatus.Scheduled, IssueDay = issue, LeadDay = today,
				PlayerSubject = true, ArtistId = artist.artistId, ArtistName = artist.stageName, RecordId = records[0].baseRecord.recordId, Title = records[0].baseRecord.title };
			state.Stories.Add(s);
			return s;
		}
		var orders = Story("probe-orders", PressFamily.Trade, StoryAngle.Breakout, today + 5);
		state.Inquiries.Add(new MediaInquiry { Id = "probe-inq-orders", Kind = InquiryKind.OrdersQuestion, OutletId = orders.OutletId, StoryId = orders.Id, ArtistId = artist.artistId,
			RecordId = orders.RecordId, Title = orders.Title, Day = today, DeadlineDay = today + 4 });
		Check(desk.AnswerMediaInquiry("probe-inq-orders", "inflate", null, out _) && orders.Inaccurate && orders.ClaimedUnits > 0
			&& state.Inquiries.First(i => i.Id == "probe-inq-orders").CheckDay > today, "An inflated figure is a claim on the record, scheduled to meet the dealers' reports");

		int exclusives = 0, refusals = 0;
		for (int i = 0; i < 30; i++) {
			var s = Story("probe-teen-" + i, PressFamily.TeenFan, StoryAngle.TeenFeature, today + 40);
			state.Inquiries.Add(new MediaInquiry { Id = "probe-photo-" + i, Kind = InquiryKind.PhotoRequest, OutletId = s.OutletId, StoryId = s.Id, ArtistId = artist.artistId,
				ArtistName = artist.stageName, Day = today, DeadlineDay = today + 10 });
			if (desk.AnswerMediaInquiry("probe-photo-" + i, "exclusive", i % 2 == 0 ? "plant" : "private", out _)) exclusives++; else refusals++;
		}
		var made = MediaAtmosphereService.Exclusives.Where(e => e.ArtistId == artist.artistId).ToList();
		Check(exclusives > 0 && refusals > 0 && made.All(e => e.UntilDay == e.FromDay + 28 && !string.IsNullOrEmpty(e.Scope)),
			$"Photo terms have a scope and a four-week expiry; the act can refuse ({exclusives} agreed, {refusals} refused)");
		Check(made.Where(e => e.Romance == "plant").All(e => e.PlantedClaim && e.ContradictionDay > today), "A planted romance is stored as a claim that can later be contradicted");

		var wrong = Story("probe-wrong", PressFamily.LocalDaily, StoryAngle.Hometown, today);
		wrong.Inaccurate = true; wrong.InaccurateClaim = "the label's home town printed wrong";
		Check(desk.RequestPressCorrection(wrong.Id, out _) && wrong.Corrected && state.Stories.Any(s => s.Angle == StoryAngle.Correction && s.RootId == wrong.RootId)
			&& state.Stories.Contains(wrong), "A correction runs under the same root and the original stays in history");
	}

	// ---- Phase 3 --------------------------------------------------------------------------------------------------

	private static FanWeek Week(int day, int warm, int hot, int selling, FanEvidence families, bool national, bool extraordinary = false) =>
		new() { Day = day, WarmRegions = warm, HotRegions = hot, TopSellerRegions = selling, Families = families, NationalExposure = national, Extraordinary = extraordinary };

	// Fixture 11 and §10.2 persistence: a local favorite can stay regional; one week is not a sensation; one event retold is one family.
	private void FanArithmetic() {
		const int regions = 10;
		float a = 0f;
		for (int i = 0; i < 200; i++) a = MediaFans.Deposit(a, 1f);
		Check(a <= 1f && MediaFans.Deposit(0.9f, 0.12f) - 0.9f < MediaFans.Deposit(0.1f, 0.12f) - 0.1f && MediaFans.Deposit(0f, 5f) == MediaFans.MaxWeeklyGain,
			"Attachment saturates below 1 with diminishing returns, and a week can add at most the weekly cap");
		Check(Math.Abs(Math.Pow(MediaFans.WeeklyRetention, 8) - 0.5) < 1e-3, "Teen attachment halves in eight weeks without fresh response");

		// Regional audience 3f: standing rises at once, falls on a three-week half-life; the top-seller latch has hysteresis.
		Check(MediaFans.NextStanding(0.01f, 0.02f) == 0.02f && Math.Abs(MediaFans.NextStanding(0.02f, 0f) - 0.02f * MediaFans.StandingRetention) < 1e-6f
			&& Math.Abs(Math.Pow(MediaFans.StandingRetention, 3) - 0.5) < 1e-3, "Sales standing takes a better week at once and halves in three weeks of no sales");
		Check(MediaFans.TopSeller(false, 9) && !MediaFans.TopSeller(false, 10) && MediaFans.TopSeller(true, 17) && !MediaFans.TopSeller(true, 18)
			&& !MediaFans.TopSeller(true, 0), "A region is entered at rank 9 and left only outside rank 17");
		float st = 0.025f; int weeksAbove = 0;
		for (int w = 0; w < 12 && st >= 0.0074f; w++, weeksAbove++) st = MediaFans.NextStanding(st, 0.005f);
		Check(weeksAbove >= 4, $"A #1 that falls to #25-level sales stays above #17-level standing for {weeksAbove} weeks, not zero");

		var local = new MediaFanState { ArtistId = "probe-local" };
		for (int w = 0; w < 16; w++) MediaFans.Advance(local, Week(7 * w, 2, 1, 2, FanEvidence.Sales | FanEvidence.Coverage, national: false), regions);
		Check(local.State == FanEpisodeState.RegionalFavorite && local.Peak == FanEpisodeState.RegionalFavorite,
			"A beloved regional favorite with no national exposure stays a regional favorite for months");

		var hit = new MediaFanState { ArtistId = "probe-hit" };
		var strong = Week(0, 6, 4, 5, FanEvidence.Sales | FanEvidence.Coverage, national: true);
		MediaFans.Advance(hit, strong, regions);
		Check(hit.State != FanEpisodeState.NationalSensation, $"One qualifying week is not a national sensation ({hit.State})");
		strong.Day = 7; MediaFans.Advance(hit, strong, regions);
		Check(hit.State == FanEpisodeState.NationalSensation, "Two qualifying weeks in the window are");

		var retold = new MediaFanState { ArtistId = "probe-retold" };
		for (int w = 0; w < 8; w++) MediaFans.Advance(retold, Week(7 * w, 10, 10, 0, FanEvidence.Coverage, national: true, extraordinary: true), regions);
		Check(retold.State == FanEpisodeState.None, "Coverage alone, however wide, is not audience behavior: no episode at all");
		var tvOnly = new MediaFanState { ArtistId = "probe-tv" };
		for (int w = 0; w < 8; w++) MediaFans.Advance(tvOnly, Week(7 * w, 10, 10, 0, FanEvidence.TvResponse, national: true, extraordinary: true), regions);
		Check(tvOnly.State < FanEpisodeState.NationalSensation, "Mail after one program, with no record selling, never makes a sensation");

		var phen = new MediaFanState { ArtistId = "probe-phen" };
		var huge = Week(0, 10, 8, 9, FanEvidence.Sales | FanEvidence.TvResponse | FanEvidence.Coverage, national: true, extraordinary: true);
		for (int w = 0; w < 3; w++) { huge.Day = 7 * w; MediaFans.Advance(phen, huge, regions); }
		Check(phen.State == FanEpisodeState.NationalSensation, "Three extraordinary weeks make a sensation, not yet a phenomenon");
		huge.Day = 21; MediaFans.Advance(phen, huge, regions);
		Check(phen.State == FanEpisodeState.CulturalPhenomenon, "Four in a row, with two independent families including audience behavior, make a cultural phenomenon");

		var quiet = Week(0, 0, 0, 0, FanEvidence.None, national: false);
		int day = 28;
		for (int w = 0; w < 4 && phen.State != FanEpisodeState.Cooldown; w++) { quiet.Day = day; day += 7; MediaFans.Advance(phen, quiet, regions); }
		Check(phen.State == FanEpisodeState.Cooldown && phen.CooldownUntilDay >= day - 7 + MediaFans.CooldownDays - 7, $"When the response fades the episode ends in a cooldown (until day {phen.CooldownUntilDay})");
		int until = phen.CooldownUntilDay;
		for (; day < until; day += 7) { huge.Day = day; MediaFans.Advance(phen, huge, regions); }
		Check(phen.State == FanEpisodeState.Cooldown, "No new national episode inside the eight-week cooldown, whatever the evidence");
		huge.Day = day; MediaFans.Advance(phen, huge, regions);
		Check(MediaAtmosphereService.IsNational(phen.State) && phen.Peak == FanEpisodeState.CulturalPhenomenon, $"After the cooldown, fresh persistent evidence can start a new one ({phen.State})");

		Check(MediaFans.HypeCap(FanEpisodeState.None) <= 0.5f && MediaFans.HypeCap(FanEpisodeState.Cooldown) <= 0.5f && MediaFans.HypeCap(FanEpisodeState.NationalSensation) < 1f
			&& MediaFans.HypeCap(FanEpisodeState.CulturalPhenomenon) == 1f, "Event tune-in waits for a sensation, the ceiling for a phenomenon");

		var band = ProbeAct("faces", 0.6f, 0.6f, 0.5f, 0.8f);
		var shares = MediaFans.CameraShares(band, "probe-show", 1964);
		Check(Math.Abs(shares.Sum(s => s.Weight) - 1f) < 1e-4 && shares.SequenceEqual(MediaFans.CameraShares(band, "probe-show", 1964)), "Camera shares sum to one and are keyed: a re-read never moves the lens");
		int leadTop = Enumerable.Range(0, 400).Count(i => MediaFans.CameraShares(band, "probe-" + i, 1964).OrderByDescending(s => s.Weight).First().Member.isLeadVocalist);
		Check(leadTop > 200 && leadTop < 400, $"The singer usually gets the camera ({leadTop}/400), but not always: a drummer can catch it");
		var fs = new MediaFanState();
		fs.Spotlight["a"] = 0.30f; fs.Spotlight["b"] = 0.10f; fs.Faces["a"] = "Ann"; fs.Faces["b"] = "Bo";
		var even = new MediaFanState();
		even.Spotlight["a"] = 0.30f; even.Spotlight["b"] = 0.28f; even.Faces["a"] = "Ann"; even.Faces["b"] = "Bo";
		Check(MediaFans.Face(fs) == "Ann" && MediaFans.Face(even) == null, "The letters name a member only when one stands clearly ahead");
	}

	// The world's own fans after a season of play: every act through the same arithmetic, nothing private, nothing economic.
	private void FansInTheWorld() {
		var clock = TimeManager.Instance;
		var acts = ArtistManager.Instance.GetAllArtists().Take(400).ToList();
		float recognition = acts.Sum(x => x.publicRecognition + x.culturalStanding + x.members.Sum(m => m.personalRecognition));
		for (int i = 0; i < 35; i++) clock.EndDay();
		var fans = MediaAtmosphereService.Fans.Values.ToList();
		GD.Print($"  fans: {fans.Count} acts [{string.Join(", ", fans.GroupBy(f => f.State).OrderBy(g => g.Key).Select(g => g.Key + " " + g.Count()))}], " +
			$"{fans.Count(f => f.Spotlight.Count > 0)} with a spotlight, letters {fans.Sum(f => f.Letters):N0}");
		foreach (var f in MediaAtmosphereService.LoudestFans(4))
			GD.Print($"  fans: {f.ArtistName}: {MediaFans.Describe(f.State)}, warm in {string.Join(", ", MediaAtmosphereService.WarmRegionNames(f, 3))}, face {MediaFans.Face(f) ?? "-"}, history {f.History.FirstOrDefault()}");
		Check(fans.Count > 0 && fans.Any(f => f.State != FanEpisodeState.None), "Rival acts acquire followings from their own careers");
		Check(fans.All(f => f.Attachment.Values.All(v => v >= 0f && v <= 1f) && f.Spotlight.Values.All(v => v >= 0f && v <= 1f)), "Attachment and spotlight stay in [0,1]");
		Check(fans.All(f => f.Spotlight.Keys.All(k => ArtistManager.Instance.GetArtist(f.ArtistId)?.members.Any(m => m.personId == k) != false)),
			"Spotlight is held by real members of the act");
		Check(MediaAtmosphereService.Stories.Where(s => s.Angle == StoryAngle.FanScene).All(s => s.Evidence.All(e => e.Source is EvidenceSource.RegionalSales or EvidenceSource.BroadcastAiring or EvidenceSource.OtherPublication)),
			$"Fan stories cite sales reports, program mail and other papers, nothing private ({MediaAtmosphereService.Stories.Count(s => s.Angle == StoryAngle.FanScene)} so far)");
		float after = acts.Sum(x => x.publicRecognition + x.culturalStanding + x.members.Sum(m => m.personalRecognition));
		GD.Print($"  canonical recognition across 400 acts moved {after - recognition:F4} in five weeks (the economy's own milestones; media writes none)");
		var desk = PlayerDesk.Instance;
		GameRandom.Seed(9191); desk.FanLines(); MediaAtmosphereService.LoudestFans(5); RolodexDirectory.Cards(desk, RolodexRole.Press); uint r1 = GameRandom.Randi();
		GameRandom.Seed(9191); uint r2 = GameRandom.Randi();
		Check(r1 == r2 && desk.FanLines().SequenceEqual(desk.FanLines()), "Reading the fan lines and the teen desks draws nothing and reads the same twice");
		foreach (string line in desk.FanLines()) GD.Print("  office: " + line);
	}

	// Fixture 14 and §13: allocations sum to the existing envelope and reserve once; rivals use the same slots; policies differ.
	private void AiPlanner() {
		foreach (MediaPolicy p in Enum.GetValues<MediaPolicy>())
			for (int i = 0; i < 300; i++) {
				var a = AiMediaPlanner.Allocate("probe", "probe", "probe", p, 0.37f + i * 13.731f, 0);
				if (!a.Balanced) throw new InvalidOperationException($"allocation for {p} at {a.EnvelopeCents} cents does not sum");
			}
		Check(true, "Every policy's allocation sums to its envelope to the cent (1,800 envelopes)");
		var labels = CompetitorManager.Instance.GetAllLabels().Where(l => !l.isPlayerOwned).ToList();
		var policies = labels.GroupBy(AiMediaPlanner.LabelPolicy).ToDictionary(g => g.Key, g => g.Count());
		Check(policies.Count >= 4, $"Rival labels have recognizably different publicity policies ({string.Join(", ", policies.Select(p => p.Key + " " + p.Value))})");
		var bandstand = MediaOutletCatalog.Get("american_bandstand");
		Check(!AiMediaPlanner.Considers(bandstand, MediaPolicy.SpecialistCommunity, null, Genre.Country, 100, out string why) && why == "policy",
			"A specialist country house doesn't chase a national teen slot for a country record");
		var upbeat = MediaOutletCatalog.Get("upbeat");
		long near = AiMediaPlanner.AppearanceCents(upbeat, D(1966, 3, 5), upbeat.HomeCityId, 3, MediaPreparation.Standard);
		long far = AiMediaPlanner.AppearanceCents(upbeat, D(1966, 3, 5), "los_angeles", 3, MediaPreparation.Standard);
		long network = AiMediaPlanner.AppearanceCents(MediaOutletCatalog.Get("ed_sullivan"), D(1966, 3, 6), "los_angeles", 3, MediaPreparation.Standard);
		Check(near == AiMediaPlanner.PreparationCents(MediaPreparation.Standard) && far > 20 * near && network == near,
			$"A local show at home costs the rehearsal, across the country ${far / 100.0:N0}; a network production pays its guests' way");

		double cash = labels.Sum(l => (double)l.cashReserves), expenses = labels.Sum(l => (double)l.monthlyExpenses);
		GameRandom.Seed(5150);
		foreach (var l in labels.Take(50)) {
			var artist = ArtistManager.Instance.GetAllArtists().FirstOrDefault(x => x.labelId == l.labelId);
			AiMediaPlanner.EnvelopeEstimate(l, artist);
			AiMediaPlanner.PhotoAnswer(AiMediaPlanner.LabelPolicy(l), artist, "probe-" + l.labelId);
		}
		uint r1 = GameRandom.Randi(); GameRandom.Seed(5150); uint r2 = GameRandom.Randi();
		Check(r1 == r2 && cash == labels.Sum(l => (double)l.cashReserves) && expenses == labels.Sum(l => (double)l.monthlyExpenses),
			"Estimating envelopes and answering for rival labels draws nothing and touches no label's money");

		var allocations = MediaAtmosphereService.AiAllocations.Values.ToList();
		Check(allocations.Count > 0 && allocations.All(a => a.Balanced && a.TvReservedCents <= a.TvCents && a.PressReservedCents <= a.PressCents
				&& a.Receipts.Values.Sum() == a.TvReservedCents + a.PressReservedCents),
			$"{allocations.Count} live campaign allocations: balanced, reservations within their lines, every reserved cent has one receipt");
		var ledger = MediaAtmosphereService.AiLedger;
		GD.Print($"  ai ledger: {string.Join(", ", ledger.OrderBy(p => p.Key).Select(p => p.Key + " " + p.Value))}");
		Check(ledger.Keys.Any(k => k.StartsWith("accept")) && ledger.Keys.Any(k => k.StartsWith("pitch-")), "Rival labels pitch, and take the dates they want and can afford");
		Check(MediaAtmosphereService.AiPitches.All(p => p.Kind == PitchKind.Television ? MediaOutletCatalog.Get(p.ProgramId) != null : MediaOutletCatalog.GetPress(p.OutletId) != null)
			&& MediaAtmosphereService.AiPitches.GroupBy(p => (p.LabelId, p.Day)).All(g => g.Count() <= 2 * AiMediaPlanner.FocusRecordsPerLabel),
			$"The planner is bounded: at most one disc and one sheet per focus record per week ({MediaAtmosphereService.AiPitches.Count} pitches)");

		// Same slots, same dates: every episode's bill, player and rival together, fits its program's guest slots.
		var byEpisode = MediaAtmosphereService.Appearances.Where(a => a.RepeatOfId == null).GroupBy(a => a.EpisodeId).ToList();
		Check(byEpisode.All(g => g.Count() <= MediaOutletCatalog.Get(g.First().ProgramId).SlotsOn(g.First().Date)),
			$"Player and rival bookings share each episode's finite slots ({byEpisode.Count} episodes)");
		var mine = MediaAtmosphereService.Appearances.FirstOrDefault(a => a.PlayerBooking);
		if (mine != null) GD.Print($"  shared bill: {string.Join(" / ", MediaAtmosphereService.Appearances.Where(a => a.EpisodeId == mine.EpisodeId).Select(a => a.ArtistName + (a.PlayerBooking ? " (yours)" : "")))}");
		var rivalTeen = MediaAtmosphereService.Stories.Where(s => s.Angle == StoryAngle.TeenFeature && !s.PlayerSubject && s.PhotoNote != null).ToList();
		GD.Print($"  rival photo answers: {string.Join(", ", rivalTeen.GroupBy(s => s.PhotoNote.Split(';')[0]).Select(g => g.Key + " " + g.Count()))}");
	}

	private void PhaseThreeSave() {
		var opts = new JsonSerializerOptions { IncludeFields = true };
		var world = new WorldSaveData();
		MediaAtmosphereService.CaptureWorld(world);
		string json = JsonSerializer.Serialize(world.Media, opts);
		var back = JsonSerializer.Deserialize<MediaSaveData>(json, opts);
		var now = world.Media;
		Check(back.Fans.Count == now.Fans.Count && back.AiAllocations.Count == now.AiAllocations.Count && back.AiPitches.Count == now.AiPitches.Count
			&& back.Fans.All(p => now.Fans[p.Key].State == p.Value.State && now.Fans[p.Key].Levels.SequenceEqual(p.Value.Levels) && now.Fans[p.Key].CooldownUntilDay == p.Value.CooldownUntilDay
				&& p.Value.Attachment.All(x => Math.Abs(now.Fans[p.Key].Attachment[x.Key] - x.Value) < 1e-6) && p.Value.Spotlight.Count == now.Fans[p.Key].Spotlight.Count)
			&& back.AiAllocations.All(p => now.AiAllocations[p.Key].TvReservedCents == p.Value.TvReservedCents && p.Value.Receipts.Count == now.AiAllocations[p.Key].Receipts.Count && p.Value.Balanced),
			$"Phase 3 state survives a save round trip ({json.Length / 1024} KB: {now.Fans.Count} fan records, {now.AiAllocations.Count} allocations, {now.AiPitches.Count} AI pitches)");
	}

	// Media → recognition, shadow (SimTools/MediaRecognitionAdapterDesign.md): the deposit's shape, then the live pass.
	private void RecognitionShadow() {
		const double P = 158e6;
		var ordinary = new MediaPerformanceResult { Outcome = PerformanceOutcome.FlatAdequate };
		float First(double fresh, MediaPerformanceResult perf = null) => MediaRecognition.Deposit(0, fresh, P, perf ?? ordinary, 0f).Gain;
		float sullivan = First(0.14 * P), talk = First(6e6);
		Check(Math.Abs(sullivan - ArtistRecognitionService.Top40Gain) < 0.003f && Math.Abs(talk - ArtistRecognitionService.ChartEntryGain) < 0.002f,
			$"A first Sullivan spot is about a Top 40 milestone ({sullivan:F4} vs {ArtistRecognitionService.Top40Gain}); a talk spot about a chart entry ({talk:F4})");
		float regular = MediaRecognition.Deposit(0.6 * P, 0.25 * 0.14 * P, P, ordinary, 0f).Gain;
		double faded = 0.6 * P * Math.Pow(ArtistRecognitionService.PublicRecognitionDecay, 52 * 5);
		float back = MediaRecognition.Deposit(faded, 0.14 * P, P, ordinary, 0f).Gain;
		Check(regular < 0.15f * sullivan && back > 5f * regular,
			$"A weekly regular stops earning ({regular:F4}); five years away and the audience has forgotten ({back:F4})");
		float bad = First(0.14 * P, new MediaPerformanceResult { Outcome = PerformanceOutcome.Breakdown });
		float great = First(0.14 * P, new MediaPerformanceResult { Outcome = PerformanceOutcome.Electrifying });
		Check(bad > 0f && Math.Abs(bad - 0.5f * sullivan) < 1e-5f && great > sullivan,
			$"A disaster still makes a face known, at half ({bad:F4}); an electrifying night more ({great:F4})");
		var clip = MediaRecognition.Deposit(0, 0.14 * P, P, ordinary, MediaRecognition.AnnualCap - 0.005f);
		Check(Math.Abs(clip.Gain - 0.005f) < 1e-6f && Math.Abs(clip.Gain + clip.Clipped - sullivan) < 1e-6f && MediaRecognition.Deposit(0, 0, P, ordinary, 0f).Gain == 0f,
			$"The year's cap clips the deposit ({clip.Gain:F4} paid, {clip.Clipped:F4} clipped); no audience, no deposit");

		MediaRecognition.SetForChecks(true);
		try {
			var clock = TimeManager.Instance;
			GameRandom.Seed(4242);
			for (int i = 0; i < 14; i++) clock.EndDay();
			var tallies = MediaAtmosphereService.Tallies.Values.ToList();
			Check(tallies.Any(t => t.ShadowRecognition > 0f && t.RecognitionReach > 0) && tallies.All(t => t.ShadowRecognition is >= 0f and <= 1f
					&& t.RecognitionDepositedYear <= MediaRecognition.AnnualCap + 1e-6f),
				$"Two weeks of airings build shadow familiarity within [0,1] and the cap ({tallies.Count(t => t.ShadowRecognition > 0f)} acts, top " +
				$"{tallies.Max(t => t.ShadowRecognition):F4})");
			var opts = new JsonSerializerOptions { IncludeFields = true };
			var world = new WorldSaveData();
			MediaAtmosphereService.CaptureWorld(world);
			var round = JsonSerializer.Deserialize<MediaSaveData>(JsonSerializer.Serialize(world.Media, opts), opts);
			Check(round.Tallies.All(p => world.Media.Tallies[p.Key].RecognitionReach == p.Value.RecognitionReach && world.Media.Tallies[p.Key].ShadowRecognition == p.Value.ShadowRecognition
					&& world.Media.Tallies[p.Key].RecognitionDepositedYear == p.Value.RecognitionDepositedYear && world.Media.Tallies[p.Key].RecognitionYear == p.Value.RecognitionYear),
				"The reach stock, shadow and year's deposit survive a save round trip");
		} finally { MediaRecognition.SetForChecks(false); }

		// Causal: the same deposits land in the act's real stock. Chart milestones only add, so after two more weeks each
		// act's stock is at least its decayed opening stock plus what television earned it.
		var recognitionSwitches = ArtistRecognition.CaptureSwitches();
		ArtistRecognition.ConfigureForProbe(enable: true, observe: false);
		MediaAtmosphere.SetForProbe(MediaAtmosphereMode.Causal);
		MediaRecognition.SetForChecks(true, writeCausal: true);
		try {
			foreach (MediaActTally t in MediaAtmosphereService.Tallies.Values) t.ShadowRecognition = 0f;
			var opening = ArtistManager.Instance.GetAllArtists().Where(x => x != null).ToDictionary(x => x.artistId, x => x.publicRecognition);
			var clock = TimeManager.Instance;
			for (int i = 0; i < 14; i++) clock.EndDay();
			float floorDecay = (float)Math.Pow(ArtistRecognitionService.PublicRecognitionDecay, 3);
			var earned = MediaAtmosphereService.Tallies.Values.Where(t => t.ShadowRecognition > 0f).ToList();
			var missing = earned.Where(t => {
				SimulatedArtist x = ArtistManager.Instance.GetArtist(t.ArtistId);
				return x == null || x.publicRecognition + 1e-4f < opening.GetValueOrDefault(t.ArtistId) * floorDecay + t.ShadowRecognition
					|| t.ShadowRecognition > x.publicRecognition + 1e-6f;
			}).ToList();
			Check(earned.Count > 0 && missing.Count == 0,
				$"Causal: TV deposits reach the act's real recognition ({earned.Count} acts, {earned.Sum(t => t.ShadowRecognition):F4} earned; " +
				$"{missing.Count} below opening stock + TV share)");
		} finally {
			MediaRecognition.SetForChecks(false);
			MediaAtmosphere.SetForProbe(MediaAtmosphereMode.Observe);
			ArtistRecognition.RestoreSwitches(recognitionSwitches);
		}
	}

	// Fixtures 12 and 13 (§12.3): the singles adapter's limits, in integers, after rounding, on edge cases and a sweep.
	private void DemandAllocation() {
		MediaDemand.Title T(string key, int b, int phys, float q = 0f, bool frenzy = false) =>
			new() { Key = key, Baseline = b, PhysicalLimit = phys, Request = q, Frenzy = frenzy };
		List<MediaDemand.Title> Field(int n, int b, int phys) => Enumerable.Range(0, n).Select(i => T("n" + i, b, phys)).ToList();
		int Sum(List<MediaDemand.Title> ts) => ts.Sum(t => t.Adjusted);

		var routine = Field(9, 1000, 2000); routine.Insert(0, T("hit", 1000, 2000, 0.08f));
		var r = MediaDemand.Allocate(routine);
		Check(routine[0].Adjusted == 1050 && Sum(routine) == 10000 && routine.Skip(1).All(t => t.Adjusted is >= 950 and <= 1000) && r.Moved == 50,
			$"A routine request is clipped to 5% of the title ({routine[0].Adjusted}), drawn from neutral titles, and the region keeps its total");
		var frenzy = Field(9, 1000, 2000); frenzy.Insert(0, T("hit", 1000, 2000, 0.08f, frenzy: true));
		MediaDemand.Allocate(frenzy);
		Check(frenzy[0].Adjusted == 1080 && Sum(frenzy) == 10000, $"A national sensation's 10% ceiling replaces the 5% one ({frenzy[0].Adjusted})");
		var crowded = Field(7, 1000, 2000); crowded.InsertRange(0, new[] { T("a", 1000, 2000, 0.05f), T("b", 1000, 2000, 0.05f), T("c", 1000, 2000, 0.05f) });
		r = MediaDemand.Allocate(crowded);
		Check(r.Moved == 100 && crowded.Take(3).Sum(t => t.Granted) == 100 && Sum(crowded) == 10000 && r.WantedUp == 150,
			$"At most 1% of a region's intent moves: three titles ask {r.WantedUp}, {r.Moved} move");
		var zero = Field(9, 1000, 2000); zero.Insert(0, T("unserved", 0, 0, 0.05f));
		r = MediaDemand.Allocate(zero);
		Check(zero[0].Adjusted == 0 && r.NoBaseline == 1 && r.Moved == 0, "A title with no serviceable intent stays at zero, however hot");
		var full = Field(9, 1000, 2000); full.Insert(0, T("stockbound", 1000, 1010, 0.05f));
		MediaDemand.Allocate(full);
		Check(full[0].Adjusted == 1010, $"A stock-bound title gets only what it can serve ({full[0].Adjusted})");
		var allUp = new List<MediaDemand.Title> { T("a", 1000, 2000, 0.05f), T("b", 1000, 2000, 0.03f) };
		r = MediaDemand.Allocate(allUp);
		Check(r.Moved == 0 && allUp.All(t => t.Adjusted == t.Baseline), "All titles asking up and no donor: nothing moves (the unmatched request is cancelled)");
		var allDown = Field(9, 1000, 1000); allDown.Insert(0, T("flop", 1000, 1000, -0.05f));
		r = MediaDemand.Allocate(allDown);
		Check(r.Moved == 0 && Sum(allDown) == 10000, "A title asking down with no recipient headroom keeps its intent");
		var down = Field(9, 1000, 2000); down.Insert(0, T("flop", 1000, 1000, -0.05f));
		r = MediaDemand.Allocate(down);
		Check(down[0].Adjusted == 950 && Sum(down) == 10000 && down.Skip(1).All(t => t.Adjusted >= 1000), "A bad night's request sends units to neutral titles with headroom");
		var tiny = new List<MediaDemand.Title> { T("a", 3, 10, 0.05f), T("b", 2, 10), T("c", 1, 10) };
		r = MediaDemand.Allocate(tiny);
		Check(r.TransferCap == 0 && tiny.All(t => t.Adjusted == t.Baseline), "With six units in a region nothing can move");

		var rng = new Random(1001);
		int cases = 0, units = 0;
		for (int c = 0; c < 600; c++) {
			int n = 1 + rng.Next(30);
			var ts = Enumerable.Range(0, n).Select(i => {
				int b = rng.Next(5) == 0 ? 0 : rng.Next(1, 4000);
				int phys = b + (rng.Next(3) == 0 ? 0 : rng.Next(0, 400));
				float q = rng.Next(3) == 0 ? 0f : (float)(rng.NextDouble() * 0.3 - 0.12);
				return T("t" + i.ToString("D2"), b, phys, q, rng.Next(10) == 0);
			}).ToList();
			int total = ts.Sum(t => t.Baseline);
			r = MediaDemand.Allocate(ts);
			int moved = ts.Sum(t => Math.Abs(t.Adjusted - t.Baseline));
			bool ok = Sum(ts) == total && moved % 2 == 0 && moved / 2 <= Math.Floor(0.01 * total) && moved / 2 == r.Moved;
			foreach (var t in ts) {
				int d = t.Adjusted - t.Baseline;
				float cap = t.Frenzy && t.Request != 0f ? MediaDemand.FrenzyCap : MediaDemand.RoutineCap;
				ok &= t.Adjusted >= 0 && (t.Baseline > 0 || t.Adjusted == 0) && Math.Abs(d) <= Math.Floor(cap * t.Baseline)
					&& (d <= 0 || t.Adjusted <= t.PhysicalLimit)
					&& !(t.Request > 0f && d < 0) && !(t.Request < 0f && d > 0);
			}
			if (!ok) throw new InvalidOperationException($"MediaDemand sweep case {c} broke an invariant: " +
				string.Join(" ", ts.Select(t => $"{t.Key}:{t.Baseline}/{t.PhysicalLimit}/{t.Request:F3}/{t.Frenzy}->{t.Adjusted}")));
			cases++; units += r.Moved;
		}
		Check(cases == 600, $"600 random regions keep every limit after rounding: totals, 1% region share, per-title caps, physical limits, " +
			$"zero baselines, one direction per title ({units:N0} units moved)");
	}

	// Phase 4 player costs (§5.4, fixtures 14-15): charged once, by receipt, through save/reload; fees are performer income.
	private void CausalPlayerCosts() {
		var desk = PlayerDesk.Instance;
		var clock = TimeManager.Instance;
		var opts = new JsonSerializerOptions { IncludeFields = true };
		MediaAtmosphere.SetForProbe(MediaAtmosphereMode.Causal);
		try {
			string home = desk.Label.homeCityId;
			TvProgram local = MediaAtmosphereService.PitchablePrograms(home, home, clock.CurrentDate).First(p => p.Network == TvNetwork.Local && p.HomeCityId == home);
			var record = ChartManager.Instance.GetAllRecords().First(r => r?.baseRecord != null && r.baseRecord.format == ReleaseFormat.Single
				&& !MediaAtmosphereService.Pitches.Any(p => p.RecordId == r.baseRecord.recordId && p.ProgramId == local.Id));
			if (clock.CurrentHour > 15) clock.EndDay();
			float cash = desk.Label.cashReserves;
			int hour = clock.CurrentHour;
			Check(MediaAtmosphereService.PitchTelevision(desk, record, local.Id, out string msg), "Causal pitch: " + msg);
			MediaPitch pitch = MediaAtmosphereService.Pitches.Last();
			Check(Math.Abs(desk.Label.cashReserves - (cash - 0.15f)) < 1e-3f && clock.CurrentHour == hour + 1 && !pitch.CostNote.Contains("estimate")
					&& MediaAtmosphereService.State.CausalFromDay >= 0 && MediaAtmosphereService.State.Receipts.Contains("pitch|" + pitch.Id),
				$"Causal mode charges the pitch once: postage and an hour at the desk, with a receipt (\"{pitch.CostNote}\")");

			SimulatedArtist act = ArtistManager.Instance.GetAllArtists().First(a => a != null && a.members?.Count(m => m != null && m.isActive) >= 2);
			int performers = act.members.Count(m => m != null && m.isActive);
			TvProgram sullivan = MediaOutletCatalog.Get("ed_sullivan");
			var date = new MediaAppearance { Id = "check-causal-app", ProgramId = sullivan.Id, ArtistId = act.artistId, ArtistName = act.stageName,
				Date = clock.CurrentDate, Preparation = MediaPreparation.Extra };
			int year = clock.CurrentDate.year;
			float feesBefore = act.members.Where(m => m != null && m.isActive).Sum(m => MediaAtmosphereService.PerformerFeesPaid(m.personId, year));
			cash = desk.Label.cashReserves;
			MediaAtmosphereService.BillProduction(desk, date, sullivan);
			MediaAtmosphereService.BillProduction(desk, date, sullivan);
			float fees = act.members.Where(m => m != null && m.isActive).Sum(m => MediaAtmosphereService.PerformerFeesPaid(m.personId, year)) - feesBefore;
			Check(Math.Abs(desk.Label.cashReserves - (cash - MediaAtmosphereService.PreparationCost(MediaPreparation.Extra))) < 1e-3f
					&& Math.Abs(fees - performers * MediaAtmosphereService.NetworkScaleFee) < 1e-3f,
				$"A produced network date bills the label its rehearsal once (${cash - desk.Label.cashReserves:N2}; the network pays fares) and pays " +
				$"{performers} performers scale once (${fees:N0}), none of it to the label");

			var world = new WorldSaveData();
			MediaAtmosphereService.CaptureWorld(world);
			var round = JsonSerializer.Deserialize<MediaSaveData>(JsonSerializer.Serialize(world.Media, opts), opts);
			MediaAtmosphereService.RehydrateWorld(new WorldSaveData { Media = round });
			cash = desk.Label.cashReserves;
			MediaAtmosphereService.BillProduction(desk, date, sullivan);
			float feesAfter = act.members.Where(m => m != null && m.isActive).Sum(m => MediaAtmosphereService.PerformerFeesPaid(m.personId, year)) - feesBefore;
			Check(desk.Label.cashReserves == cash && Math.Abs(feesAfter - fees) < 1e-3f && round.CausalFromDay == world.Media.CausalFromDay,
				"After save and reload the receipts hold: the produced date is not billed or paid again");

			var localDate = new MediaAppearance { Id = "check-causal-local", ProgramId = local.Id, ArtistId = act.artistId, ArtistName = act.stageName,
				Date = clock.CurrentDate, Preparation = MediaPreparation.Minimal };
			cash = desk.Label.cashReserves;
			MediaAtmosphereService.BillProduction(desk, localDate, local);
			float localFees = act.members.Where(m => m != null && m.isActive).Sum(m => MediaAtmosphereService.PerformerFeesPaid(m.personId, year)) - feesBefore;
			Check(desk.Label.cashReserves == cash && Math.Abs(localFees - fees) < 1e-3f,
				"A hometown dance show with no rehearsal costs the label nothing and pays no scale");
			Check(MediaAtmosphereService.Pitches.Where(p => p.Status is PitchStatus.Confirmed or PitchStatus.Aired && p.Id != pitch.Id).All(p => !p.CausalTerms),
				"Dates confirmed in observe mode stay free after the world turns causal (activation boundary)");
		} finally {
			MediaAtmosphere.SetForProbe(MediaAtmosphereMode.Observe);
		}
	}

	// §12.3 live, shadow: airings of the record itself leave a signed regional request that decays; nothing else asks.
	private void DemandShadow() {
		MediaDemand.SetForChecks(shadowOn: true, causalOn: false);
		try {
			var clock = TimeManager.Instance;
			for (int i = 0; i < 14; i++) clock.EndDay();
			var lift = MediaAtmosphereService.State.DemandLift;
			var recordSongs = new HashSet<string>(MediaAtmosphereService.Appearances.Where(a => a.Song == PerformedSong.Record && !a.Cut).Select(a => a.RecordId));
			float maxRequest = lift.Values.SelectMany(x => x.Values).DefaultIfEmpty(0f).Max(Math.Abs);
			Check(lift.Count > 0 && lift.Keys.All(recordSongs.Contains) && maxRequest < 0.5f,
				$"Two weeks of airings leave regional requests on {lift.Count} records, all of them sung on the air (largest {maxRequest:F4})");
			var before = lift.ToDictionary(p => p.Key, p => p.Value.ToDictionary(x => x.Key, x => x.Value));
			MediaDemand.SetForChecks(false, false);
			for (int i = 0; i < 7; i++) clock.EndDay();
			Check(MediaAtmosphereService.State.DemandLift.Count == before.Count, "With the adapter off the requests are neither written nor decayed");
		} finally { MediaDemand.SetForChecks(false, false); }
	}

	// Celebrity fanbase Phase 0 (SimTools/CelebrityFanbaseDesign.md §2, §7): the stock's arithmetic, then a live observe pass.
	private void FollowingArithmetic() {
		MediaFollowing.ResetBars();
		float f = 0f;
		for (int i = 0; i < 500; i++) f = MediaFollowing.Join(f, 50_000f, 1_000_000);
		Check(f <= 1_000_000f && f > 990_000f && MediaFollowing.Join(900_000f, 10_000f, 1_000_000) - 900_000f < 1_001f
			&& MediaFollowing.Join(0f, 10_000f, 1_000_000) == 10_000f, "A following saturates at the people who fit the act, with joiners scaled by the room left");
		Check(Math.Abs(MediaFollowing.HeatFactor(0f) * 1_000_000 * MediaFollowing.CBuy - 60_000) < 1 && Math.Abs(MediaFollowing.HeatFactor(1f) * 1_000_000 * MediaFollowing.CBuy - 300_000) < 1,
			"A million units sold cold leave 60k followers; the same million in a frenzy leave 300k");
		float drift = 1f;
		for (int w = 0; w < 52; w++) drift *= 1f - MediaFollowing.Weekly(MediaFollowing.YoungDriftYear);
		Check(Math.Abs(drift - (1f - MediaFollowing.YoungDriftYear)) < 1e-3f, $"Young drift takes its yearly rate, applied weekly ({1 - drift:P1})");

		// Ageing: nobody leaves the young pool in the first three years, everyone has left by year nine, evenly between.
		double SurviveTo(double years) {
			double c = 1;
			for (int w = 0; w < (int)Math.Round(years * 52); w++) c *= 1 - MediaFollowing.AgeOutThisWeek(w / 52.0);
			return c;
		}
		double three = SurviveTo(3), six = SurviveTo(6), gone = SurviveTo(9.1);
		Check(three > 0.98 && Math.Abs(six - 0.5) < 0.03 && gone < 1e-6, $"A young cohort ages out between years 3 and 9 (left at 3/6/9: {three:F3}/{six:F3}/{gone:F6})");

		Check(MediaFollowing.Disappointment(new List<float>(), 0.2f) == 0f && MediaFollowing.Disappointment(new List<float> { 0.70f, 0.72f }, 0.63f) == 0f && MediaFollowing.Disappointment(new List<float> { 0.80f }, 0.30f) == 0f
			&& Math.Abs(MediaFollowing.Disappointment(new List<float> { 0.70f, 0.70f }, 0.55f) - 0.10f) < 1e-5f
			&& MediaFollowing.Disappointment(new List<float> { 0.80f, 0.80f }, 0.30f) == MediaFollowing.DisappointmentMax,
			"A record clearly below the act's recent average loses 5-20% of the following; under two records to compare, or a small dip, loses nothing");
		Check(MediaFollowing.Novelty(0) == 1f && MediaFollowing.Novelty(30) < 0.41f && MediaFollowing.Attention(0f) == 1f
			&& Math.Abs(MediaFollowing.Attention(MediaFollowing.PhenomenonShare) - 0.5f) < 1e-6f,
			"A first national date is fully novel and wears toward 0.4; a phenomenon's worth of others' attention halves registration");
		var (sy, sa) = MediaFollowing.AdvancePerFollower(ReleaseFormat.Single);
		var (ly, la) = MediaFollowing.AdvancePerFollower(ReleaseFormat.Album);
		Check(sy > ly && la > sa && MediaFollowing.YoungBuyerShare(Genre.TeenPop, ReleaseFormat.Single) > MediaFollowing.YoungBuyerShare(Genre.TeenPop, ReleaseFormat.Album)
			&& MediaFollowing.YoungBuyerShare(Genre.TeenPop, ReleaseFormat.Single) > MediaFollowing.YoungBuyerShare(Genre.Jazz, ReleaseFormat.Single),
			"Young followers buy singles, adult followers LPs; teen genres and singles sell young");

		// Salience states (§7.2), on synthetic smoothed shares.
		MediaFollowingState Act(float sales, float tv, float press, float young, float adult, float youngSales, float youngTv) =>
			new() { ArtistId = "a", Family = new[] { sales, tv, press }, Audience = new[] { young, adult }, YoungFamily = new[] { youngSales, youngTv } };
		var idol = Act(0.035f, 0.04f, 0.01f, 0.05f, 0.005f, 0.05f, 0.05f);
		Check(MediaFollowing.Advance(idol, 0.01f, 1) && idol.State == SalienceState.NationalSensation && MediaFollowing.YoungBreadth(idol) == 2,
			$"A teen idol with sales and television is a national sensation (young salience {MediaFollowing.YoungSalience(idol):F3}, young breadth {MediaFollowing.YoungBreadth(idol)})");
		for (int w = 0; w < 30; w++) MediaFollowing.Advance(idol, 0.002f, 2 + w);
		Check(idol.State == SalienceState.NationalSensation, "A pure teen craze tops out at sensation however long it lasts: no adult crossover, no phenomenon");
		var daydreams = Act(0.012f, 0.012f, 0.002f, 0.04f, 0.004f, 0.04f, 0.04f);
		Check(daydreams.Salience() < MediaFollowing.SensationShare && MediaFollowing.Advance(daydreams, 0.02f, 1) && daydreams.State == SalienceState.NationalSensation,
			$"A teen idol below θs in total attention (adults weigh most of it) is a sensation on young attention (total {daydreams.Salience():F3})");
		var crooner = Act(0.035f, 0.04f, 0.01f, 0.005f, 0.05f, 0.005f, 0.005f);
		for (int w = 0; w < 30; w++) MediaFollowing.Advance(crooner, 0.01f, w);
		Check(crooner.State == SalienceState.None && MediaFollowing.Breadth(crooner) == 2,
			"An adult act with the same sales and television but no young lead is not a sensation: the sensation is the teen state");
		var bigAdult = Act(0.06f, 0.06f, 0.04f, 0.035f, 0.07f, 0.035f, 0.035f);
		for (int w = 0; w < 30; w++) MediaFollowing.Advance(bigAdult, 0.01f, w);
		Check(bigAdult.State == SalienceState.None,
			"A big adult act that clears the young bars on volume, but holds more of the adults' attention than the young's, is not a sensation");
		var beatles = Act(0.14f, 0.12f, 0.10f, 0.15f, 0.06f, 0.15f, 0.15f);
		bool early = false;
		for (int w = 0; w < MediaFollowing.PhenomenonHoldWeeks - 1; w++) { MediaFollowing.Advance(beatles, 0.03f, w); early |= beatles.State == SalienceState.CulturalPhenomenon; }
		MediaFollowing.Advance(beatles, 0.03f, 99);
		Check(!early && beatles.State == SalienceState.CulturalPhenomenon, $"Dominance in every family and both audiences for eight weeks makes a phenomenon (salience {beatles.Salience():F3})");
		var crowded = Act(0.14f, 0.12f, 0.10f, 0.15f, 0.06f, 0.15f, 0.15f);
		for (int w = 0; w < 20; w++) MediaFollowing.Advance(crowded, 0.06f, w);
		Check(crowded.State == SalienceState.NationalSensation, "The same act in a busy field, with a rival at half its salience, stays a sensation");
		MediaFollowing.Advance(beatles, 0.20f, 100);
		Check(beatles.State == SalienceState.CulturalPhenomenon, "One week out-shone does not end a phenomenon");
		for (int w = 0; w < MediaFollowing.PhenomenonHoldWeeks; w++) MediaFollowing.Advance(beatles, 0.20f, 101 + w);
		Check(beatles.State == SalienceState.NationalSensation, "Eight weeks failing the test settles a phenomenon back to a sensation");
		beatles.Family = new[] { 0.005f, 0.002f, 0f };
		beatles.Audience = new[] { 0.01f, 0.003f };
		MediaFollowing.Advance(beatles, 0f, 200);
		Check(beatles.State == SalienceState.None && beatles.Peak == SalienceState.CulturalPhenomenon, "Below 0.6 of the sensation bar in young attention the act leaves; its peak is remembered");
	}

	private void FollowingInTheWorld() {
		MediaFollowing.SetForChecks(true);
		try {
			var clock = TimeManager.Instance;
			for (int i = 0; i < 28; i++) clock.EndDay();
			var following = MediaAtmosphereService.State.Following;
			var sizes = following.Values.Select(s => s.YoungTotal() + s.AdultTotal()).OrderByDescending(x => x).ToList();
			bool sane = following.Values.All(s => s.Regions.Values.All(r => r.Adult >= 0f && r.Young.Values.All(v => v >= 0f && float.IsFinite(v)))
				&& s.Family.All(v => v >= 0f && v <= 1f) && s.Salience() <= 1f);
			Check(following.Count > 0 && sane && sizes[0] < 5e6f,
				$"Four observe weeks build followings for {following.Count} acts, every stock finite and non-negative (largest {sizes[0]:F0}, tenth {(sizes.Count > 9 ? sizes[9] : 0):F0})");
			float sum = following.Values.Sum(s => s.Salience());
			Check(sum > 0f && sum <= 1.0001f, $"Salience is a share of national attention: summed over acts it stays within 1 ({sum:F3})");
			var opts = new JsonSerializerOptions { IncludeFields = true };
			var world = new WorldSaveData();
			MediaAtmosphereService.CaptureWorld(world);
			var back = JsonSerializer.Deserialize<MediaSaveData>(JsonSerializer.Serialize(world.Media, opts), opts);
			Check(back.Following.Count == following.Count && back.Following.All(p => Math.Abs(p.Value.YoungTotal() - following[p.Key].YoungTotal()) < 1e-2f
				&& Math.Abs(p.Value.AdultTotal() - following[p.Key].AdultTotal()) < 1e-2f && p.Value.State == following[p.Key].State
				&& p.Value.Family.SequenceEqual(following[p.Key].Family) && p.Value.ReleasesSeen.SetEquals(following[p.Key].ReleasesSeen)
				&& p.Value.Regions.Values.All(r => r.Young.Keys.All(y => y >= 1950 && y <= 1970))),
				$"Followings survive a save round trip with their cohorts ({following.Count} acts)");
			var before = following.ToDictionary(p => p.Key, p => p.Value.YoungTotal() + p.Value.AdultTotal());
			MediaFollowing.SetForChecks(false);
			for (int i = 0; i < 7; i++) clock.EndDay();
			Check(following.Count == before.Count && following.All(p => p.Value.YoungTotal() + p.Value.AdultTotal() == before[p.Key]),
				"With the switch off, followings are neither written nor decayed");
		} finally { MediaFollowing.SetForChecks(false); MediaFollowing.ResetBars(); }
	}

	// Celebrity fanbase Phase 1 (SimTools/CelebrityFanbaseDesign.md §5): spotlight writes personal recognition.
	private void MemberFameArithmetic() {
		const double P = 180e6;
		var ordinary = new MediaPerformanceResult { Outcome = PerformanceOutcome.FlatAdequate };
		var electric = new MediaPerformanceResult { Outcome = PerformanceOutcome.Electrifying };
		double sullivan = MediaMemberFame.RegisteredViewers(0.14 * P, 0.35, 0.55, Genre.TeenPop, ordinary, 0, 1f);
		Check(sullivan / P is > 0.008 and < 0.035, $"A first ordinary Sullivan spot registers {sullivan / P:F4} of the country, split among the faces the camera found");
		Check(MediaMemberFame.RegisteredViewers(0.14 * P, 0.35, 0.55, Genre.TeenPop, electric, 0, 1f) > sullivan
			&& MediaMemberFame.RegisteredViewers(0.14 * P, 0.35, 0.55, Genre.TeenPop, ordinary, 20, 1f) < 0.45 * sullivan
			&& MediaMemberFame.RegisteredViewers(0.14 * P, 0.35, 0.55, Genre.TeenPop, ordinary, 0, 0.5f) < 0.51 * sullivan
			&& MediaMemberFame.RegisteredViewers(0, 0.35, 0.55, Genre.TeenPop, ordinary, 0, 1f) == 0,
			"An electrifying night registers more; a twentieth date or a week another act holds the country registers less; no fresh viewers, nothing");
		double feature = MediaMemberFame.RegisteredReaders(1e6, 0.8f, cover: false), cover = MediaMemberFame.RegisteredReaders(1e6, 0.8f, cover: true);
		Check(Math.Abs(cover - MediaMemberFame.CoverWeight * feature) < 1e-6 && feature / P < 0.002,
			$"A cover registers three times a feature's readers; a million-reader pin-up is a small national deposit ({feature / P:F5})");
		Check(MediaMemberFame.Gain(0f, 0.02f) == 0.02f && Math.Abs(MediaMemberFame.Gain(0.5f, 0.02f) - 0.51f) < 1e-6f && MediaMemberFame.Gain(1f, 0.5f) == 1f,
			"A member's deposit has the act's diminishing returns");

		var group = new SimulatedArtist { artistId = "promise-act", stageName = "The Fixtures" };
		for (int i = 0; i < 4; i++)
			group.members.Add(new Musician { personId = "pm" + i, firstName = "P" + i, lastName = "M", isActive = true, stagePresence = 0.4f, isLeadVocalist = i == 0, birthYear = 1940 });
		var without = MediaFans.CameraShares(group, "tv|fixture", 1964);
		var with = MediaFans.CameraShares(group, "tv|fixture", 1964, m => m.personId == "pm3");
		Check(with.First(x => x.Member.personId == "pm3").Weight > without.First(x => x.Member.personId == "pm3").Weight + 0.1f
			&& Math.Abs(with.Sum(x => x.Weight) - 1f) < 1e-5f,
			$"A promised drummer pulls the camera ({without.First(x => x.Member.personId == "pm3").Weight:P0} → {with.First(x => x.Member.personId == "pm3").Weight:P0}); shares still sum to one");

		// Fame follows the camera's favorite more than the frame does: a typical lead (stage presence 0.6, sidemen 0.4)
		// takes 0.81 / 1.83 = 44% of chart-earned member fame, so television must not hand them less (Phase 1 notes).
		group.members[0].stagePresence = 0.6f;
		var frames = Enumerable.Range(0, 400).Select(k => MediaFans.CameraShares(group, "tv|lead" + k, 1964)).ToList();
		float frame = frames.Average(s => s.First(x => x.Member.personId == "pm0").Weight);
		float fame = frames.Average(s => MediaMemberFame.FameShares(s).First(x => x.Member.personId == "pm0").Weight);
		Check(fame > 0.44f && fame > frame && frames.All(s => Math.Abs(MediaMemberFame.FameShares(s).Sum(x => x.Weight) - 1f) < 1e-5f),
			$"The lead holds {frame:P0} of the frame but takes {fame:P0} of the fame, above the chart's 44%; fame shares sum to one");
	}

	private void MemberFameInTheWorld() {
		var opts = new JsonSerializerOptions { IncludeFields = true };
		var clock = TimeManager.Instance;
		bool Group(SimulatedArtist a) => a?.members != null && a.members.Count(m => m != null && m.isActive) > 1;
		MediaMemberFame.SetForChecks(true);
		try {
			for (int i = 0; i < 28; i++) clock.EndDay();
			var fame = MediaAtmosphereService.MemberFame;
			Check(fame.Count > 0 && fame.Values.All(e => e.Earned is >= 0f and <= 1f && float.IsFinite(e.Earned)),
				$"Four shadow weeks put {fame.Count} group members' faces in front of viewers and readers (top earned {(fame.Count > 0 ? fame.Values.Max(e => e.Earned) : 0):F4})");
			Check(fame.Keys.All(id => ArtistManager.Instance.GetArtist(fame[id].ArtistId)?.members.Any(m => m.personId == id) == true),
				"Every member credited is a real member of the act named");
			Check(!MediaAtmosphereService.PromiseCamera(ArtistManager.Instance.GetAllArtists().First(Group), ArtistManager.Instance.GetAllArtists().First(Group).members[0], true, clock.CurrentDate)
				&& MediaAtmosphereService.CameraPromises.Count == 0, "Shadow mode makes no camera promise: the band room keeps minting as before");
			var world = new WorldSaveData();
			MediaAtmosphereService.CaptureWorld(world);
			var back = JsonSerializer.Deserialize<MediaSaveData>(JsonSerializer.Serialize(world.Media, opts), opts);
			Check(back.MemberFame.Count == fame.Count && back.MemberFame.All(p => fame[p.Key].Earned == p.Value.Earned && fame[p.Key].ArtistId == p.Value.ArtistId),
				"The media-earned ledger survives a save round trip");
		} finally { MediaMemberFame.SetForChecks(false); }

		// Causal: the same deposits land in the members' real stock. Chart milestones only add, so each credited member ends at
		// least at the decayed opening stock plus what the camera earned; and the band room now promises the camera.
		var recognitionSwitches = ArtistRecognition.CaptureSwitches();
		ArtistRecognition.ConfigureForProbe(enable: true, observe: false);
		MediaAtmosphere.SetForProbe(MediaAtmosphereMode.Causal);
		MediaMemberFame.SetForChecks(true, writeCausal: true);
		try {
			foreach (MemberFameEntry e in MediaAtmosphereService.MemberFame.Values) e.Earned = 0f;
			var people = ArtistManager.Instance.GetAllArtists().Where(Group).SelectMany(a => a.members.Where(m => m.isActive && m.personId != null))
				.GroupBy(m => m.personId).ToDictionary(g => g.Key, g => g.First());
			var opening = people.ToDictionary(p => p.Key, p => p.Value.personalRecognition);
			for (int i = 0; i < 14; i++) clock.EndDay();
			float floorDecay = (float)Math.Pow(ArtistRecognitionService.PublicRecognitionDecay, 3);
			var earned = MediaAtmosphereService.MemberFame.Where(p => p.Value.Earned > 0f && people.ContainsKey(p.Key)).ToList();
			var missing = earned.Where(p => people[p.Key].personalRecognition + 1e-4f < opening[p.Key] * floorDecay + p.Value.Earned
				|| p.Value.Earned > people[p.Key].personalRecognition + 1e-6f).ToList();
			Check(earned.Count > 0 && missing.Count == 0,
				$"Causal: camera deposits reach members' personal recognition ({earned.Count} members, {earned.Sum(p => p.Value.Earned):F4} earned; {missing.Count} below opening + earned)");

			SimulatedArtist act = ArtistManager.Instance.GetAllArtists().First(a => Group(a) && a.isActive);
			Musician drummer = act.members.Where(m => m.isActive).OrderBy(m => m.stagePresence).First();
			float stock = drummer.personalRecognition;
			Check(MediaAtmosphereService.PromiseCamera(act, drummer, billing: true, clock.CurrentDate) && drummer.personalRecognition == stock
				&& MediaAtmosphereService.CameraPromises.TryGetValue(drummer.personId, out CameraPromise p) && p.Billing && p.StageName == act.stageName,
				"Causal: billing a member promises the camera and mints no fame in the office");
			string name = act.stageName;
			act.stageName = name + " (renamed)";
			for (int i = 0; i < 7; i++) clock.EndDay();
			bool lapsed = !MediaAtmosphereService.CameraPromises.ContainsKey(drummer.personId);
			act.stageName = name;
			Check(lapsed, "A billing promise lapses once the act is no longer billed under that name");
		} finally {
			MediaMemberFame.SetForChecks(false);
			MediaAtmosphere.SetForProbe(MediaAtmosphereMode.Observe);
			ArtistRecognition.RestoreSwitches(recognitionSwitches);
		}
	}

	// Celebrity fanbase Phase 2 (SimTools/CelebrityFanbaseDesign.md §3-§4): registration replaces the cap; press and
	// controversy write the act's name, once per root event.
	private void FameWritersArithmetic() {
		const double P = 158e6;
		var ordinary = new MediaPerformanceResult { Outcome = PerformanceOutcome.FlatAdequate };
		var electric = new MediaPerformanceResult { Outcome = PerformanceOutcome.Electrifying };
		double Reg(double seen, double fresh, MediaPerformanceResult perf, int dates = 0, Genre genre = Genre.TraditionalPop, double young = 0.3, double adult = 0.6) =>
			MediaRecognition.Registered(seen, fresh, P, young, adult, genre, perf, dates, 1f).Registered / P;
		double sullivan = Reg(0, 0.14 * P, ordinary);
		Check(sullivan is > 0.01 and < 0.04, $"A routine first Sullivan spot registers {sullivan:F4} of the country, close to the design's 0.035: upkeep, not a breakout");
		double night = Reg(0, 0.38 * P, electric, genre: Genre.TeenPop, young: 0.55, adult: 0.4);
		Check(night > MediaRecognition.AnnualCap && night >= MediaRecognition.BreakoutGain,
			$"One electrifying night before 38% of the country registers {night:F3}, past the old yearly cap ({MediaRecognition.AnnualCap}): a breakout");
		// The tally passes a return to the same program as its new quarter (ReturnVisitNewShare); the reach stock then removes
		// the part of even that quarter who saw the act elsewhere.
		double again = Reg(0.38 * P, 0.25 * 0.38 * P, electric, dates: 1, genre: Genre.TeenPop, young: 0.55, adult: 0.4);
		Check(again < 0.2 * night, $"The same show the next week registers {again:F3}: people who already know the act can't learn it again");
		Check(Reg(0, 0.14 * P, ordinary, dates: 20) < 0.45 * sullivan && Reg(0, 0, ordinary) == 0,
			"A twentieth national date registers less than half a first; no audience, nobody");

		PressOutlet Press(string id) => MediaOutletCatalog.GetPress(id);
		double life = MediaRecognition.RegisteredReaders(Press("life"), Genre.TeenPop), look = MediaRecognition.RegisteredReaders(Press("look"), Genre.TeenPop);
		Check(Math.Abs(life - 20e6 * MediaRecognition.MainstreamRegistration) < 1 && life / P is > 0.02 and < 0.03,
			$"A Life story registers a fifth of its 20M readers, {life / P:F3} of the country, about a Top 40 hit");
		Check(MediaRecognition.RegisteredReaders(Press("billboard"), Genre.TeenPop) == 0 && MediaRecognition.RegisteredReaders(Press("sixteen"), Genre.TeenPop) == 0
			&& MediaRecognition.RegisteredReaders(Press("village_voice"), Genre.Folk) == 0 && MediaRecognition.RegisteredReaders(Press("ebony"), Genre.Soul) > 0,
			"Trades, teen magazines and critics register no act's name; the Black press does, among its readers");
		var (lookExtra, credit) = MediaRecognition.RootBudget(0, look);
		var (lifeExtra, credit2) = MediaRecognition.RootBudget(credit, life);
		var (lookAgain, _) = MediaRecognition.RootBudget(credit2, look);
		Check(lookExtra == look && Math.Abs(lifeExtra - (life - look)) < 1 && lookAgain == 0 && credit2 == life,
			"One story in Look and Life counts once, at the wider outlet: Life adds only its readers beyond Look's, and a retelling adds nothing");
	}

	// Celebrity fanbase Phase 2c (SimTools/CelebrityFanbaseDesign.md §3.4): fan episodes raise an event night's hype, and
	// registration reads one constant fit, so only an event night can break an act out.
	private void EventHypeArithmetic() {
		bool was = MediaFans.EventHype;
		MediaFans.EventHype = true;
		try { EventHypeArithmeticOn(); } finally { MediaFans.EventHype = was; }
	}

	private void EventHypeArithmeticOn() {
		var s = new MediaFanState { ArtistId = "x" };
		s.State = FanEpisodeState.NationalSensation; MediaFans.NoteRecentHigh(s, 100);
		s.State = FanEpisodeState.Cooldown; MediaFans.NoteRecentHigh(s, 114);
		Check(MediaFans.RecentHighOf(s, 128) == FanEpisodeState.NationalSensation && MediaFans.RecentHighOf(s, 100 + MediaFans.EventMemoryDays + 7) == FanEpisodeState.None,
			"A sensation that cooled two weeks ago still lifts a date booked in the frenzy; eight weeks on it doesn't");

		var sullivan = MediaOutletCatalog.Get("ed_sullivan");
		GameDate night = D(1964, 2, 9);
		var regions = ChartManager.Instance.GetAllRegions();
		var markets = BroadcastExposure.Markets(sullivan, sullivan.VersionOn(night), night, regions);
		double P = regions.Sum(r => r.population) * 1e6;
		double Fresh(float hype) {
			var (lift, tune) = MediaAtmosphereService.EventDraw(Math.Min(hype, 1f), sullivan);
			return BroadcastExposure.Estimate(sullivan, 60, 1, 2, 0f, markets, 1f, lift, tune).UniqueReach / P;
		}
		// Hype = max(precursor, episode) × novelty × (0.3 + 0.7 × coverage), first primetime date, coverage 0.6.
		float ordinaryHype = 0.2f * 0.85f * 1f, sensationHype = MediaFans.EpisodeHype(FanEpisodeState.NationalSensation) * 0.72f,
			phenomenonHype = MediaFans.EpisodeHype(FanEpisodeState.CulturalPhenomenon);
		double routine = Fresh(0f), ordinary = Fresh(ordinaryHype), sensation = Fresh(sensationHype), phenomenon = Fresh(phenomenonHype);
		Check(ordinary == routine && sensation > 1.15 * routine && phenomenon > 1.8 * routine,
			$"An ordinary No. 1 draws Sullivan's usual {routine:F3}; a sensation's first event night {sensation:F3}; a phenomenon's {phenomenon:F3}");
		Check(Fresh(sensationHype * 0.4f) == routine, "A second event night on the same program is damped back to routine");

		// The exposure model sets the ratios; the base is the largest fresh Sullivan audience the 1963-66 worlds drew
		// (0.166, CelebrityFanbasePhase2Notes.md), above this bare fixture night's.
		double world = 0.166 / routine;
		routine *= world; sensation *= world; phenomenon *= world;
		var flat = new MediaPerformanceResult { Outcome = PerformanceOutcome.FlatAdequate };
		var electric = new MediaPerformanceResult { Outcome = PerformanceOutcome.Electrifying };
		double Reg(double fresh, MediaPerformanceResult perf, Genre genre) =>
			MediaRecognition.Registered(0, fresh * P, P, 0.3, 0.6, genre, perf, 0, 1f).Registered / P;
		float bar = MediaRecognition.BreakoutGain;
		Check(Reg(routine, electric, Genre.TeenPop) < bar && Reg(routine, flat, Genre.TeenPop) is > 0.015 and < 0.035,
			$"No routine date breaks out, however it goes: a first Sullivan spot registers {Reg(routine, flat, Genre.TeenPop):F3}, an electrifying one {Reg(routine, electric, Genre.TeenPop):F3}");
		Check(Reg(routine, flat, Genre.TeenPop) == Reg(routine, flat, Genre.Comedy),
			"Familiarity ignores taste: a teen act on a family show registers as well as the show's own kind of act");
		Check(Reg(sensation, flat, Genre.TeenPop) < bar && Reg(sensation, electric, Genre.TeenPop) >= bar && Reg(phenomenon, flat, Genre.TeenPop) >= bar,
			$"A sensation breaks out only on an electrifying event night ({Reg(sensation, electric, Genre.TeenPop):F3}); a phenomenon on any decent one ({Reg(phenomenon, flat, Genre.TeenPop):F3})");

		// §3.4.5: a rising act's announced first network date, novelty intact, advance coverage 0.8, capped as a sensation.
		float risingHype = Math.Min(MediaFans.RisingHype * 1f * (0.3f + 0.7f * 0.8f), MediaFans.HypeCap(MediaFans.RisingCapState));
		double rising = Fresh(risingHype) * world;
		var engaging = new MediaPerformanceResult { Outcome = PerformanceOutcome.Engaging };
		Check(Reg(rising, flat, Genre.TeenPop) < bar && Reg(rising, engaging, Genre.TeenPop) >= bar,
			$"A rising act's first network date (hype {risingHype:F2}, {rising:F3} of the country) breaks out on an engaging night ({Reg(rising, engaging, Genre.TeenPop):F3}), not a routine one ({Reg(rising, flat, Genre.TeenPop):F3})");
	}

	private void FameWritersInTheWorld() {
		var opts = new JsonSerializerOptions { IncludeFields = true };
		var clock = TimeManager.Instance;
		MediaRecognition.SetForChecks(true, withRegistration: true);
		MediaFollowing.SetForChecks(true);
		try {
			for (int i = 0; i < 14; i++) clock.EndDay();
			var roots = MediaAtmosphereService.State.FameRoots;
			var tallies = MediaAtmosphereService.Tallies.Values.ToList();
			Check(roots.Count > 0 && roots.Values.All(r => r.People >= 0 && double.IsFinite(r.People)) && roots.Keys.Any(k => k.StartsWith("tv|"))
				&& tallies.All(t => t.ShadowRecognition is >= 0f and <= 1f),
				$"Two shadow weeks credit {roots.Count} root events ({roots.Keys.Count(k => !k.StartsWith("tv|"))} from the papers); every act's media familiarity stays in [0,1]");
			var world = new WorldSaveData();
			MediaAtmosphereService.CaptureWorld(world);
			var back = JsonSerializer.Deserialize<MediaSaveData>(JsonSerializer.Serialize(world.Media, opts), opts);
			Check(back.FameRoots.Count == roots.Count && back.FameRoots.All(p => roots[p.Key].People == p.Value.People && roots[p.Key].Day == p.Value.Day),
				"Root credits survive a save round trip");
		} finally { MediaRecognition.SetForChecks(false); }

		// Causal: deposits reach the act's real stock, and a controversy makes the act more known while its adults leave.
		var recognitionSwitches = ArtistRecognition.CaptureSwitches();
		ArtistRecognition.ConfigureForProbe(enable: true, observe: false);
		MediaAtmosphere.SetForProbe(MediaAtmosphereMode.Causal);
		MediaRecognition.SetForChecks(true, writeCausal: true, withRegistration: true);
		try {
			foreach (MediaActTally t in MediaAtmosphereService.Tallies.Values) t.ShadowRecognition = 0f;
			var opening = ArtistManager.Instance.GetAllArtists().Where(x => x != null).ToDictionary(x => x.artistId, x => x.publicRecognition);
			for (int i = 0; i < 14; i++) clock.EndDay();
			float floorDecay = (float)Math.Pow(ArtistRecognitionService.PublicRecognitionDecay, 3);
			var earned = MediaAtmosphereService.Tallies.Values.Where(t => t.ShadowRecognition > 0f).ToList();
			var missing = earned.Where(t => {
				SimulatedArtist x = ArtistManager.Instance.GetArtist(t.ArtistId);
				return x == null || x.publicRecognition + 1e-4f < opening.GetValueOrDefault(t.ArtistId) * floorDecay + t.ShadowRecognition
					|| t.ShadowRecognition > x.publicRecognition + 1e-6f;
			}).ToList();
			Check(earned.Count > 0 && missing.Count == 0,
				$"Causal: registration and the papers reach the act's real recognition ({earned.Count} acts, {earned.Sum(t => t.ShadowRecognition):F4} earned; {missing.Count} short)");

			MediaFollowingState fs = MediaAtmosphereService.State.Following.Values
				.Where(s => s.AdultTotal() > 0f && s.YoungTotal() > 0f && ArtistManager.Instance.GetArtist(s.ArtistId) != null)
				.OrderByDescending(s => s.AdultTotal()).ThenBy(s => s.ArtistId).First();
			SimulatedArtist act = ArtistManager.Instance.GetArtist(fs.ArtistId);
			float recog = act.publicRecognition, adult = fs.AdultTotal(), young = fs.YoungTotal();
			int today = BroadcastExposure.DayIndex(clock.CurrentDate);
			var c = new MediaControversy { Id = "controversy|fixture", RootId = "controversy|fixture", ArtistId = act.artistId, ArtistName = act.stageName,
				StartDay = today, ExpiresDay = today + 84, Severity = 0.6f, National = true, Verified = true };
			MediaAtmosphereService.ControversyFame(c);
			float expected = recog + 0.6f * MediaRecognition.NotorietyGain * (1f - recog);
			Check(Math.Abs(act.publicRecognition - expected) < 1e-4f && Math.Abs(fs.AdultTotal() - 0.7f * adult) < 1e-2f * adult
				&& Math.Abs(fs.YoungTotal() - 0.91f * young) < 1e-2f * young && fs.HardenedUntilDay == today + MediaFollowing.HardenedDays,
				$"A national defiance makes the act more known ({recog:F3} → {act.publicRecognition:F3}); 30% of its adults and 9% of its young fans leave; the core hardens for a year");
			float after = act.publicRecognition;
			MediaAtmosphereService.ControversyFame(c);
			Check(act.publicRecognition == after, "The same controversy retold adds no recognition: its root is already credited");
		} finally {
			MediaRecognition.SetForChecks(false);
			MediaFollowing.SetForChecks(false);
			MediaFollowing.ResetBars();
			MediaAtmosphere.SetForProbe(MediaAtmosphereMode.Observe);
			ArtistRecognition.RestoreSwitches(recognitionSwitches);
		}
	}

	// Celebrity fanbase Phase 2b (CelebrityFanbaseDesign.md §4.3): reviews decide how much of a record's craft is heard.
	private void CriticsArithmetic() {
		PressOutlet Press(string id) => MediaOutletCatalog.GetPress(id);
		float bar = ArtistCriticalAcclaimService.CraftBar, good = 0.80f, weak = 0.45f;
		var none = new List<CriticReview>();
		(float floorCoverage, float floorVerdict) = MediaCritics.Heard(good, none);
		Check(floorCoverage == MediaCritics.TradeFloor && floorVerdict == good
			&& Math.Abs(MediaCritics.Delta(good, 0f, none) - ArtistCriticalAcclaimService.GetAcclaimDelta(good, 0f)) < 1e-6f,
			"An unreviewed record is heard only through the trades' picks (coverage 0.5, verdict = craft) and keeps today's climb: the neutral floor");
		Check(MediaCritics.Delta(weak, 0f, none) == ArtistCriticalAcclaimService.GetAcclaimDelta(weak, 0f)
			&& MediaCritics.Delta(weak, 0f, new List<CriticReview> { new() { Authority = 0.6f, Taste = 0f } }) == ArtistCriticalAcclaimService.GetAcclaimDelta(weak, 0f),
			$"Erosion below the craft bar ({bar:F2}) is not scaled: nobody has to review a record for it to be forgotten");

		float jazz = MediaCritics.Authority(Press("down_beat"), Genre.Jazz, 1962), teen = MediaCritics.Authority(Press("down_beat"), Genre.TeenPop, 1962);
		var downBeat = new List<CriticReview> { new() { Authority = jazz, Taste = 0f } };
		Check(Math.Abs(jazz - 0.6f) < 1e-6f && teen < 0.06f && Math.Abs(MediaCritics.Heard(good, downBeat).Coverage - 0.8f) < 1e-6f,
			$"Down Beat speaks for jazz (authority {jazz:F2}, coverage with the trades 0.80), hardly for a teen record ({teen:F3})");
		Check(MediaCritics.BaseAuthority(Press("village_voice"), 1960) == 0.15f && Math.Abs(MediaCritics.BaseAuthority(Press("village_voice"), 1967) - 0.35f) < 1e-6f
			&& MediaCritics.BaseAuthority(Press("rolling_stone"), 1967) == 0.3f && Math.Abs(MediaCritics.BaseAuthority(Press("rolling_stone"), 1969) - 0.6f) < 1e-6f
			&& MediaCritics.BaseAuthority(Press("jet"), 1963) == 0.3f && MediaCritics.BaseAuthority(Press("life"), 1963) == 0f && MediaCritics.BaseAuthority(Press("billboard"), 1963) == 0f,
			"Authority rises through the decade (the Voice 0.15 → 0.35 by 1967, Rolling Stone 0.3 → 0.6 by 1969); Life and the trades' news desks review nothing");

		var tastes = Enumerable.Range(0, 4000).Select(i => MediaCritics.Taste("down_beat", "fixture-" + i)).ToList();
		Check(Math.Abs(tastes.Average()) < 0.01f && tastes.All(t => Math.Abs(t) <= MediaCritics.TasteSpread / 2f),
			$"Critics disagree without drift: taste is centred (mean {tastes.Average():+0.0000;-0.0000}) and moves a verdict at most ±{MediaCritics.TasteSpread / 2f:F3}");

		var harsh = new List<CriticReview> { new() { Authority = 0.6f, Taste = -0.15f } };
		var warm = new List<CriticReview> { new() { Authority = 0.6f, Taste = 0.15f } };
		float edge = bar + 0.03f;
		Check(MediaCritics.Heard(edge, harsh).Verdict < bar && MediaCritics.Delta(edge, 0f, harsh) < 0f
			&& MediaCritics.Heard(edge, warm).Verdict > edge && MediaCritics.Delta(edge, 0f, warm) > MediaCritics.Delta(edge, 0f, none),
			"A harsh review can sink a record just over the bar; a warm one lifts it past what the trades alone gave");

		var acclaimed = new List<CriticReview> { new() { Authority = 0.6f, Taste = 0f }, new() { Authority = 0.5f, Taste = 0f } };
		float reviewed = MediaCritics.Delta(good, 0f, acclaimed), unreviewed = MediaCritics.Delta(good, 0f, none);
		Check(Math.Abs(reviewed - 1.8f * unreviewed) < 1e-5f && reviewed <= ArtistCriticalAcclaimService.MaxTotalGainPerRelease
			&& MediaCritics.Delta(1f, 0f, acclaimed) == ArtistCriticalAcclaimService.MaxTotalGainPerRelease,
			$"The underrated LP Down Beat and Sing Out! reviewed earns {reviewed:F3}, 1.8× its unreviewed {unreviewed:F3}; a perfect one stops at the per-release ceiling");
	}

	private void CriticsInTheWorld() {
		var opts = new JsonSerializerOptions { IncludeFields = true };
		var clock = TimeManager.Instance;
		var media = MediaAtmosphereService.State;
		MediaCritics.SetForChecks(true);
		try {
			for (int i = 0; i < 14; i++) clock.EndDay();
			var reviews = media.Reviews.SelectMany(p => p.Value.Reviews.Select(r => (Record: p.Key, Review: r))).ToList();
			var column = reviews.Where(x => x.Review.Column).ToList();
			var byId = ChartManager.Instance.GetAllRecords().Where(r => r?.baseRecord?.recordId != null).ToDictionary(r => r.baseRecord.recordId);
			Check(column.Count > 0 && column.All(x => byId.TryGetValue(x.Record, out var r) && r.baseRecord.format == ReleaseFormat.Album)
				&& reviews.All(x => x.Review.Authority is >= 0f and <= 1f)
				&& media.Reviews.Values.All(e => e.Reviews.Select(r => r.OutletId).Distinct().Count() == e.Reviews.Count)
				&& column.All(x => MediaOutletCatalog.GetPress(x.Review.OutletId).Fit(byId[x.Record].baseRecord.primaryGenre) >= MediaCritics.ColumnFit),
				$"Two shadow weeks: the columns review {column.Select(x => x.Record).Distinct().Count()} new LPs ({column.Count} reviews, {reviews.Count - column.Count} of chart entries), each in its readers' music, one per outlet");
			Check(media.Stories.All(s => s.Angle != StoryAngle.Review || !column.Any(x => x.Record == s.RecordId && x.Review.OutletId == s.OutletId)),
				"The review column is its own ledger: no LP review enters the news items other media readers see");
			var world = new WorldSaveData();
			MediaAtmosphereService.CaptureWorld(world);
			var back = JsonSerializer.Deserialize<MediaSaveData>(JsonSerializer.Serialize(world.Media, opts), opts);
			Check(back.Reviews.Count == media.Reviews.Count && back.ReviewColumn.Count == media.ReviewColumn.Count
				&& back.Reviews.All(p => p.Value.Reviews.Count == media.Reviews[p.Key].Reviews.Count && p.Value.LastDay == media.Reviews[p.Key].LastDay
					&& p.Value.Reviews.Zip(media.Reviews[p.Key].Reviews).All(z => z.First.OutletId == z.Second.OutletId && z.First.Authority == z.Second.Authority && z.First.Taste == z.Second.Taste)),
				$"Review ledgers and column counts survive a save round trip ({media.Reviews.Count} records)");

			// Consumption: a record's published reviews are read once, then its ledger is gone.
			int today = BroadcastExposure.DayIndex(clock.CurrentDate);
			var (recordId, entry) = media.Reviews.Where(p => p.Value.Reviews.Any(r => r.IssueDay <= today) && byId.ContainsKey(p.Key)
				&& ArtistManager.Instance.GetArtist(byId[p.Key].baseRecord.artistId) != null).OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => (p.Key, p.Value)).First();
			Record rec = byId[recordId].baseRecord;
			SimulatedArtist act = ArtistManager.Instance.GetArtist(rec.artistId);
			float craft = 0.80f, before = act.criticalAcclaim;
			var published = entry.Reviews.Where(r => r.IssueDay <= today).ToList();
			Check(MediaAtmosphereService.CriticAcclaimDelta(act, rec, craft, 0f) == null && !media.Reviews.ContainsKey(recordId) && act.criticalAcclaim == before
				&& Math.Abs(media.ShadowAcclaim[act.artistId] - Math.Clamp(before * ArtistCriticalAcclaimService.RetentionPerRelease + MediaCritics.Delta(craft, 0f, published), 0f, 1f)) < 1e-6f,
				$"Shadow: a completed run reads its {published.Count} published review(s) once into the act's shadow acclaim; the real acclaim is untouched");

			MediaAtmosphere.SetForProbe(MediaAtmosphereMode.Causal);
			MediaCritics.SetForChecks(true, writeCausal: true);
			for (int i = 0; i < 7; i++) clock.EndDay();
			today = BroadcastExposure.DayIndex(clock.CurrentDate);
			var next = media.Reviews.Where(p => p.Value.Reviews.Any(r => r.IssueDay <= today) && byId.ContainsKey(p.Key)
				&& ArtistManager.Instance.GetArtist(byId[p.Key].baseRecord.artistId) != null).OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => (p.Key, p.Value)).FirstOrDefault();
			if (next.Key != null) {
				Record r2 = byId[next.Key].baseRecord;
				SimulatedArtist a2 = ArtistManager.Instance.GetArtist(r2.artistId);
				var heard = next.Value.Reviews.Where(r => r.IssueDay <= today).ToList();
				float? delta = MediaAtmosphereService.CriticAcclaimDelta(a2, r2, craft, 0f);
				Check(delta.HasValue && Math.Abs(delta.Value - MediaCritics.Delta(craft, 0f, heard)) < 1e-6f
					&& MediaAtmosphereService.CriticAcclaimDelta(a2, r2, craft, 0f) == MediaCritics.Delta(craft, 0f, new List<CriticReview>()),
					$"Causal: the delta carries the record's {heard.Count} review(s) ({delta:F3}); asked again, the record is unreviewed (the same review never counts twice)");
			}
		} finally {
			MediaCritics.SetForChecks(false);
			MediaAtmosphere.SetForProbe(MediaAtmosphereMode.Observe);
		}
	}

	private void PhaseTwoSave() {
		var opts = new JsonSerializerOptions { IncludeFields = true };
		var world = new WorldSaveData();
		MediaAtmosphereService.CaptureWorld(world);
		string json = JsonSerializer.Serialize(world.Media, opts);
		var back = JsonSerializer.Deserialize<MediaSaveData>(json, opts);
		var now = world.Media;
		Check(back.Stories.Count == now.Stories.Count && back.StandardsQueries.Count == now.StandardsQueries.Count && back.Pitches.Count == now.Pitches.Count
			&& back.Contacts.Count == now.Contacts.Count && back.Inquiries.Count == now.Inquiries.Count && back.Exclusives.Count == now.Exclusives.Count
			&& back.ProgramMemory.Count == now.ProgramMemory.Count && back.Credibility.Count == now.Credibility.Count && back.BreakoutsReported.Count == now.BreakoutsReported.Count
			&& back.PlayerSequence == now.PlayerSequence
			&& back.Appearances.Zip(now.Appearances).All(p => p.First.Performance?.Outcome == p.Second.Performance?.Outcome && p.First.Cut == p.Second.Cut && p.First.Reported == p.Second.Reported)
			&& back.Stories.Zip(now.Stories).All(p => p.First.Id == p.Second.Id && p.First.Evidence.Count == p.Second.Evidence.Count && p.First.IssueDay == p.Second.IssueDay),
			$"Phase 2 media state survives a save round trip ({json.Length / 1024} KB: {now.Stories.Count} items, {now.Contacts.Count} contacts, {now.Pitches.Count} pitches)");
	}
}
