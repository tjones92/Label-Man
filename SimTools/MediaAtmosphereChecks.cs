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
}
