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
			Performance();
			Standards();
			EventNights();
			PressAndPrivacy();
			PlayerLoop();
			PlayerPress();
			PhaseTwoSave();
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
		if (desk.Label == null) desk.FoundLabel("Media Checks", "cleveland", FoundingArchetype.ExMusician, out _);
		int before = MediaAtmosphereService.Appearances.Count;
		GD.Seed(4242); desk.TelevisionColumn(clock.CurrentDate); RolodexDirectory.Cards(desk, RolodexRole.Press); uint r1 = GD.Randi();
		GD.Seed(4242); uint r2 = GD.Randi();
		Check(r1 == r2 && MediaAtmosphereService.Appearances.Count == before, "Reading the paper and the Rolodex draws no RNG and books nothing");
		var column = desk.TelevisionColumn(clock.CurrentDate);
		Check(column.SequenceEqual(desk.TelevisionColumn(clock.CurrentDate)), "The television column reads the same twice");
		foreach (string line in column) GD.Print("  paper: " + line);
		var cards = RolodexDirectory.Cards(desk, RolodexRole.Press);
		Check(cards.Any(c => c.Firm.Contains("Upbeat") || c.Firm.Contains("Big 5")) || cards.Any(c => c.Title == "Producer"), $"PRESS & TV lists the local producer ({cards.Count} cards)");
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
		Check(mainstream <= 4, $"Mainstream magazines rarely cover popular music ({mainstream} items in four weeks)");
		var desk = PlayerDesk.Instance;
		GD.Seed(777); desk.PressColumn(clock.CurrentDate); RolodexDirectory.Cards(desk, RolodexRole.Press); uint a = GD.Randi();
		GD.Seed(777); uint b = GD.Randi();
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
		Check(national.Count > 0 && national.All(p => p.Status == PitchStatus.Declined), $"The Ed Sullivan Show declines {national.Count} uncharted records");
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
