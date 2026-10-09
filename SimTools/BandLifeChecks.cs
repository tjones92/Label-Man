using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>
/// Headless checks for the band-member simulation, Phases 0-3 (SimTools/BandMemberSimulationDirective.md).
/// Run through the save/load runner, which boots the full world:
///   Godot_console.exe --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1002 \
///     --enable-genre-market-v2 --enable-artist-population-lifecycle --band-life-check
/// Every check prints BANDLIFE_CHECK_PASS or throws BANDLIFE_CHECK_FAIL.
/// </summary>
public static class BandLifeChecks {
	private static void Check(bool ok, string label) {
		if (!ok) throw new InvalidOperationException("BANDLIFE_CHECK_FAIL: " + label);
		GD.Print("BANDLIFE_CHECK_PASS: " + label);
	}

	public static void Run() {
		BandLife.Switches saved = BandLife.CaptureSwitches();
		try {
			BandLife.ConfigureForProbe(cowritingOn: true, axesOn: true, observeOn: false, scope: LineupChurnScope.Roster);
			KeyedDrawsTouchNoStream();
			CowritingShapes();
			FragmentsReadFacts();
			PlayerBandRoomFlow();
			LaterPhaseInvariants();
		} finally {
			BandLife.RestoreSwitches(saved);
		}
		GD.Print("BANDLIFE_CHECKS_COMPLETE");
	}

	/// <summary>Phases 4c-7 (§14): the invariants each phase is built on, checked on real objects.</summary>
	private static void LaterPhaseInvariants() {
		// 4c: a lone writer's song is untouched; complementary specialists beat two of the same kind.
		var lone = TeamWritingService.Delta(new[] { new TeamWritingService.Crafts(.3f, .2f, .25f) }, 0, 0f);
		Check(lone.Melody == 0f && lone.Lyric == 0f && lone.Hook == 0f, "team craft: a lone writer's song is unchanged");
		var complementary = TeamWritingService.Delta(new[] { new TeamWritingService.Crafts(.6f, .1f, .3f), new TeamWritingService.Crafts(.1f, .6f, .3f) }, 0, 0f);
		var same = TeamWritingService.Delta(new[] { new TeamWritingService.Crafts(.6f, .1f, .3f), new TeamWritingService.Crafts(.6f, .1f, .3f) }, 0, 0f);
		Check(complementary.Lyric + complementary.Melody > same.Lyric + same.Melody, "team craft: complementary specialists are the strongest pairing");
		Check(same.Melody <= 0f, "team craft: two of the same kind are no better than one");
		// 4e: the norm is monotone and bounded; nothing reads it with the readers off.
		var poor = new Musician { wealth = 500f }; var rich = new Musician { wealth = 20000f };
		Check(MemberWealthService.Norm(poor) < MemberWealthService.Norm(rich) && MemberWealthService.Norm(rich) < 1f, "wealth: norm is monotone and below 1");
		// 5: plasticity falls with age; a person seen for the first time at 45 is not aged again by switching growth on.
		Check(MemberGrowthService.Plasticity(19) > MemberGrowthService.Plasticity(28) && MemberGrowthService.Plasticity(28) > MemberGrowthService.Plasticity(45),
			"growth: plasticity falls with age");
		// 6: the scouting read on live unsigned acts: three lines, a potential word, and a revisit that reads improvement.
		var acts = ArtistManager.Instance.GetAllArtists().Where(a => string.IsNullOrEmpty(a.labelId) && a.members.Any(m => m.isActive))
			.OrderBy(a => a.artistId, StringComparer.Ordinal).Take(4).ToList();
		Check(acts.Count > 0, "scouting rough: the world has unsigned acts to read");
		foreach (SimulatedArtist act in acts) {
			var tells = ScoutingRough.Read(act, null, Array.Empty<PlayerDesk.RepertoireItem>(), 10, 0.40f, 0.46f, 120);
			List<string> lines = ScoutingRough.Lines(tells, "holds the room");
			Check(lines.Count == 3 && lines[2].StartsWith("Potential: ", StringComparison.Ordinal), $"scouting rough: {act.stageName} reads [{string.Join(" | ", lines)}]");
			Check(lines[2].Contains("much tighter"), "scouting rough: a revisit three months on reads the improvement");
		}
	}

	/// <summary>§8: no new code draws from the global stream. Axes and keyed draws leave GD exactly where it was.</summary>
	private static void KeyedDrawsTouchNoStream() {
		var m = new Musician("probe_mus_1", "Probe", "Person", true, 1940) {
			technicalSkill = 0.55f, creativity = 0.6f, musicalVersatility = 0.5f, stagePresence = 0.5f, isLeadVocalist = true,
			primaryRole = MusicianRole.LeadVocals
		};
		GD.Seed(4242);
		float before = GD.Randf();
		GD.Seed(4242);
		MemberAxesService.Generate(m, Genre.RockAndRoll, 1961);
		float a = BandLife.Unit("probe|x"), n = BandLife.Normal("probe|y");
		float after = GD.Randf();
		Check(before == after, "axes and keyed draws consume nothing from the global stream");
		var copy = new Musician("probe_mus_1", "Probe", "Person", true, 1940) {
			technicalSkill = 0.55f, creativity = 0.6f, musicalVersatility = 0.5f, stagePresence = 0.5f, isLeadVocalist = true,
			primaryRole = MusicianRole.LeadVocals
		};
		MemberAxesService.Generate(copy, Genre.RockAndRoll, 1961);
		Check(copy.vocalPower == m.vocalPower && copy.ceilingVocal == m.ceilingVocal && copy.sightReading == m.sightReading &&
			a == BandLife.Unit("probe|x") && n == BandLife.Normal("probe|y"), "keyed generation is deterministic per person");
		Check(m.formationVocalControl == m.vocalControl && m.axesVersion == MemberAxesService.CurrentAxesVersion &&
			m.ceilingVocal >= Math.Max(m.vocalPower, m.vocalControl) - 1e-6f, "formation skill starts at today's skill; ceiling never below it");
	}

	/// <summary>Phase 1 gate shape: a two-writer band co-writes some of the time, a one-writer band rarely, shares
	/// always sum to one, and the same song key always gets the same team.</summary>
	private static void CowritingShapes() {
		SimulatedArtist band = ProbeBand("probe_band_cw", writers: 2);
		int multi = 0, n = 400;
		bool sumsToOne = true;
		for (int i = 0; i < n; i++) {
			// The rate before any pact: once two writers formalise one, every song is theirs together by design.
			band.writingPartnerships = null;
			var team = CowritingService.PickTeam(band, "song" + i, 1962);
			sumsToOne &= Math.Abs(team.Sum(t => t.Share) - 1f) < 1e-4f;
			if (team.Count > 1) multi++;
		}
		Check(sumsToOne, "credit shares always sum to one");
		float share = multi / (float)n;
		Check(share > 0.20f && share < 0.75f, $"a two-writer band co-writes a realistic share of its songs ({share:P0})");
		var t1 = CowritingService.PickTeam(band, "song7", 1962).Select(t => t.Member.personId).ToList();
		var t2 = CowritingService.PickTeam(band, "song7", 1962).Select(t => t.Member.personId).ToList();
		Check(t1.SequenceEqual(t2), "the same song key always gets the same team");
		band.writingPartnerships = null;
		for (int i = 0; i < 40; i++) CowritingService.PickTeam(band, "run" + i, 1962);
		Check(band.writingPartnerships != null && band.writingPartnerships.Any(p => p.coCredits >= CowritingService.PactCoCreditThreshold),
			"co-credits accumulate on the writing partnership");
		SimulatedArtist solo = ProbeBand("probe_band_one", writers: 1);
		int soloMulti = Enumerable.Range(0, 300).Count(i => CowritingService.PickTeam(solo, "s" + i, 1962).Count > 1);
		Check(soloMulti < 60, $"a one-writer band is mostly one writer ({soloMulti}/300 shared)");
	}

	/// <summary>The Rolodex rule: a death line matches its channel; a credit grievance quotes the ledger.</summary>
	private static void FragmentsReadFacts() {
		SimulatedArtist band = ProbeBand("probe_band_fx", writers: 2);
		Musician m = band.members[2];
		var ctx = new BandRoomContext {
			visit = new BandVisit { visitId = "probe_visit_death", kind = BandVisitKind.Death, channel = DeathChannel.Travel },
			artist = band, member = m, instincts = new ExecutiveInstinctProfile { TheEar = 3, TheStreet = 3, TheSuit = 3, TheFixer = 3 }
		};
		string death = BandRoomFragments.Pick(RolodexSceneBeat.SituationRead, ctx);
		Check(death != null && (death.Contains("plane") || death.Contains("car")) && death.Contains(m.firstName), $"a road death reads as a road death: \"{death}\"");
		ctx.visit = new BandVisit { visitId = "probe_visit_credit", kind = BandVisitKind.Grievance, cause = StrainCause.CreditAndMoney };
		ctx.cause = StrainCause.CreditAndMoney; ctx.voice = MemberVoice.Blunt; ctx.other = band.members[0];
		ctx.actOriginals = 6; ctx.memberSongs = 1; ctx.otherSongs = 5; ctx.memberCreditShare = 0.17f; ctx.otherCreditShare = 0.83f;
		string credit = BandRoomFragments.Pick(RolodexSceneBeat.SituationRead, ctx);
		Check(credit != null && credit.Contains("6") && credit.Contains("1"), $"a credit grievance quotes the ledger: \"{credit}\"");
	}

	/// <summary>
	/// The Band Room end to end on a real player act: strain reaches an ultimatum at the year boundary, the member
	/// comes in with a fact, a verb writes the sim, a departure leaves an alumni record and a person in the pool,
	/// a replacement joins, and the whole thing survives a player save round trip and a world save round trip.
	/// </summary>
	private static void PlayerBandRoomFlow() {
		PlayerDesk desk = PlayerDesk.Instance;
		var cities = DistanceModel.GetCities();
		Check(desk != null && cities.Count > 0 && desk.FoundLabel("Band Room Probe", cities[0].cityId, FoundingArchetype.ExMusician, out _),
			"found a player label");
		int year = TimeManager.Instance.CurrentDate.year;
		SimulatedArtist act = ArtistManager.Instance.GetUnsignedArtists()
			.Where(a => a.type == ArtistType.Band && a.members.Count(m => m.isActive) >= 4)
			.OrderBy(a => a.artistId, StringComparer.Ordinal).FirstOrDefault();
		Check(act != null, "an unsigned four-piece band exists to sign");
		desk.Label.SignArtist(act, year, desk.Label.GenerateTermSheet(act, year));
		ArtistManager.Instance.SignArtist(act, desk.Label.labelId, year);
		Check(act.isPlayerOwned && desk.Roster.Contains(act) && act.contractLeavingMemberOption, "signed to the player with a leaving-member clause");
		desk.Label.cashReserves = 5000f;

		// A grievance that has been brewing, at near-breaking strain.
		var members = act.members.Where(m => m.isActive).ToList();
		Musician x = members[1], y = members[2];
		MemberRelation edge = BandLifeService.Edge(act, x, y);
		edge.strain = 0.99f;
		edge.causeWeight[(int)StrainCause.Burnout] = 0.99f;
		Musician complainer = x.temperament <= y.temperament ? x : y;
		complainer.departureStage = DepartureStage.Brewing;
		BandLifeService.OnYearBoundary(year + 1);
		BandVisit visit = desk.PendingVisits().FirstOrDefault(v => v.artistId == act.artistId && v.kind == BandVisitKind.Ultimatum);
		Check(visit != null && visit.personId == complainer.personId && complainer.departureStage == DepartureStage.Ultimatum,
			"a brewing grievance hardens into an ultimatum and the member comes in");

		Check(desk.OpenVisit(visit.visitId, out _) && desk.ActiveVisit.transcript.Count >= 2, "the visit opens with an opening and a situation read");
		var options = desk.VisitOptions(desk.ActiveVisit);
		Check(options.Any(o => o.verb == BandVerb.TimeOff) && options.Any(o => o.verb == BandVerb.LetThemGo) && options.Any(o => o.verb == BandVerb.Mediate),
			"burnout ultimatum offers time off, mediation and letting them go");

		// Time off writes the sim: morale up, the road's grudge relieved, momentum spent.
		float strainBefore = edge.strain, moraleBefore = act.morale;
		desk.ChooseBandVerb(desk.ActiveVisit, options.First(o => o.verb == BandVerb.TimeOff), out _);
		Check(edge.strain < strainBefore && act.morale > moraleBefore && visit.resolved, "time off lowers the strain and lifts morale");
		desk.CloseVisit();

		// Let a member go: alumni record, pool, departure visit, then a replacement.
		int before = act.members.Count;
		Musician leaver = members[3];
		BandLifeService.PlayerDepart(act, leaver, DepartureKind.Acrimony, StrainCause.Direction, year + 1);
		Check(act.members.Count == before - 1 && act.alumni?.Any(r => r.personId == leaver.personId) == true &&
			!act.members.Contains(leaver), "a departure removes the person and leaves an alumni record");
		bool pooled = PersonPool.Contains(leaver.personId);
		bool career = PersonPool.HasCareerToContinue(leaver, year + 1, CompositionCatalogService.HasAnyWriterCredit(leaver.personId));
		Check(pooled == career || (act.lifecycleStatus != ArtistLifecycleStatus.Active), "only a person with a career to continue enters the pool");
		BandVisit departure = desk.PendingVisits().FirstOrDefault(v => v.artistId == act.artistId && v.kind == BandVisitKind.Departure);
		Check(departure != null, "the departure arrives as a Band Room visit");
		desk.OpenVisit(departure.visitId, out _);
		var hire = desk.VisitOptions(desk.ActiveVisit).FirstOrDefault(o => o.verb == BandVerb.HireNewcomer);
		Check(hire != null, "a departure offers a replacement");
		desk.ChooseBandVerb(desk.ActiveVisit, hire, out _);
		desk.CloseVisit();
		Musician newcomer = act.members.LastOrDefault();
		Check(act.members.Count == before && newcomer != null && newcomer.joinedYear == year && newcomer.axesVersion > 0 &&
			ArtistManager.Instance.GetMusician(newcomer.personId) == newcomer, "a newcomer joins with keyed traits and axes, registered " +
			$"(members {act.members.Count}/{before}, joined {newcomer?.joinedYear}/{year}, axes {newcomer?.axesVersion}, " +
			$"registered {ArtistManager.Instance.GetMusician(newcomer?.personId ?? "") == newcomer})");

		// Bookings, a writing session and a cut-in all land in the desk's state.
		Check(desk.BookRoad(act, 8, 4, out _) && desk.RoadBookingFor(act.artistId)?.WeeksRemaining == 12, "a residency and a tour are booked");
		Check(desk.CutInOnSongs(act, out _) && desk.CutInSongsLeft(act.artistId) == PlayerDesk.LabelCutInSongs, "the label cuts itself in");

		// Player save round trip: the Band Room comes back whole.
		edge.strain = 0.99f; complainer.departureStage = DepartureStage.Brewing;
		BandLifeService.OnYearBoundary(year + 2);
		int pending = desk.PendingVisits().Count;
		float trust = desk.TrustOf(complainer.personId);
		PlayerSaveData captured = desk.CaptureState();
		string json = JsonSerializer.Serialize(captured, SaveGameService.TestJsonOptions);
		PlayerSaveData restored = JsonSerializer.Deserialize<PlayerSaveData>(json, SaveGameService.TestJsonOptions);
		Check(desk.RestoreState(restored, out string restoreMessage), "player state restores: " + restoreMessage);
		Check(desk.PendingVisits().Count == pending && Math.Abs(desk.TrustOf(complainer.personId) - trust) < 1e-6f &&
			desk.CutInSongsLeft(act.artistId) == PlayerDesk.LabelCutInSongs, "visits, trust and cut-ins survive a player save");

		// World save round trip: relations, alumni, partners, axes and the pool are all inside the snapshot.
		// The probe world was generated before the probe switched member axes on, so its first load migrates everyone
		// onto axes (the old-save path). Normalise once, then require a load to change nothing.
		WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(JsonSerializer.Serialize(WorldStateService.Capture(),
			SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions), TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);
		WorldSaveData w1 = WorldStateService.Capture();
		string a1 = JsonSerializer.Serialize(w1, SaveGameService.TestJsonOptions);
		WorldStateService.Apply(JsonSerializer.Deserialize<WorldSaveData>(a1, SaveGameService.TestJsonOptions),
			TimeManager.Instance.CurrentDate, SimulationSeedBootstrap.RequestedSeed);
		string a2 = JsonSerializer.Serialize(WorldStateService.Capture(), SaveGameService.TestJsonOptions);
		if (a1 != a2) {
			int at = 0, n = Math.Min(a1.Length, a2.Length);
			while (at < n && a1[at] == a2[at]) at++;
			int from = Math.Max(0, at - 300);
			GD.Print($"BANDLIFE_SAVE_DIFF at {at} of {a1.Length}/{a2.Length}\n  before: {a1.Substring(from, Math.Min(500, a1.Length - from))}\n  after:  {a2.Substring(from, Math.Min(500, a2.Length - from))}");
		}
		Check(a1 == a2, $"the world save round-trips byte-identically with band-life state ({a1.Length / 1024} KB)");
		Check(a1.Contains("\"BandLife\"") && (!pooled || a1.Contains(leaver.personId)),
			"the snapshot carries the band-life section and the pooled person (player acts ride in the player save)");
	}

	private static SimulatedArtist ProbeBand(string id, int writers) {
		var band = new SimulatedArtist { artistId = id, stageName = "The Probes", type = ArtistType.Band, primaryGenre = Genre.RockAndRoll };
		string[] names = { "Al", "Bo", "Cy", "Di" };
		for (int i = 0; i < 4; i++) {
			var m = new Musician($"{id}_m{i}", names[i], "Probe", true, 1940) {
				technicalSkill = 0.5f, creativity = i < writers ? 0.7f : 0.3f, ambition = 0.6f, ego = 0.5f, loyalty = 0.5f,
				temperament = 0.5f, reliability = 0.6f, stagePresence = 0.5f, musicalVersatility = 0.5f, isActive = true,
				isPrimaryWriter = i < writers, isLeadVocalist = i == 0, primaryRole = i == 0 ? MusicianRole.LeadVocals : MusicianRole.LeadGuitar,
				joinedYear = 1960
			};
			band.members.Add(m);
		}
		return band;
	}
}
