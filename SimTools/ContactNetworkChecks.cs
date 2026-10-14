using System;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>--contact-network-check (run after a year boundary, e.g. --weeks=60): edge invariants, session and
/// house-band ledgers, the audition sentence, the player's "ask around" tips, and save round-trips of both.</summary>
public static class ContactNetworkChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("CONTACT_NETWORK_CHECK_FAILED: " + reason); checks++; }
		string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);

		// Flags.
		ContactNetworkService.Configure(new[] { "--disable-contact-network", "--disable-session-employment", "--disable-employment-growth" });
		Check(!ContactNetworkService.Enabled && !SessionEmploymentService.Enabled && !SessionEmploymentService.EmploymentGrowth, "disable flags");
		bool rejected = false;
		try { ContactNetworkService.Configure(new[] { "--enable-contact-network", "--disable-contact-network" }); } catch (ArgumentException) { rejected = true; }
		Check(rejected, "contradictory contact flags rejected");
		ContactNetworkService.Configure(Array.Empty<string>());
		Check(ContactNetworkService.Enabled && SessionEmploymentService.Enabled && SessionEmploymentService.EmploymentGrowth, "defaults on");

		// Edge invariants on the live world.
		var edges = ContactNetworkService.Edges.ToList();
		Check(edges.Count > 0, "edges exist after a year");
		Check(edges.All(e => e.A != e.B && string.CompareOrdinal(e.A, e.B) < 0), "edges ordered, no self edges");
		Check(edges.All(e => ContactNetworkService.ContactsOf(e.A).Contains(e.B) && ContactNetworkService.ContactsOf(e.B).Contains(e.A)), "adjacency symmetric");
		Check(edges.Any(e => e.Kinds.HasFlag(ContactKind.FormerBandmate)) && edges.Any(e => e.Kinds.HasFlag(ContactKind.Session)) &&
			edges.Any(e => e.Kinds.HasFlag(ContactKind.HouseBand)), "all three edge kinds occur");
		Check(edges.All(e => e.Kinds != ContactKind.None && e.Jobs > 0 && e.FirstYear <= e.LastYear), "edge fields well formed");

		// Linking semantics on a synthetic pair.
		ContactNetworkService.Link("zz_probe_a", "zz_probe_b", ContactKind.Session, 1961, 2, via: "label_0001");
		ContactNetworkService.Link("zz_probe_b", "zz_probe_a", ContactKind.FormerBandmate, 1962, 3, fallout: true);
		ContactNetworkService.Link("zz_probe_a", "zz_probe_b", ContactKind.Session, 1963);
		var probe = ContactNetworkService.Edge("zz_probe_b", "zz_probe_a");
		Check(probe != null && probe.Jobs == 6 && probe.Fallout && probe.Kinds == (ContactKind.Session | ContactKind.FormerBandmate) &&
			probe.FirstYear == 1961 && probe.LastYear == 1963, "repeated work deepens one undirected edge; fallout is sticky");

		// Sessions and house bands.
		Check(SessionEmploymentService.RecordsCrewed > 0, "AI records hire session crews");
		Check(SessionEmploymentService.Accounts.All(a => a.Sessions > 0 &&
			Math.Abs(a.Pay - a.Sessions * SessionEmploymentService.ScalePerSession1960 * SceneLiveEconomics.PriceLevel(a.Year)) < 0.01f * a.Pay + 0.01f),
			"session pay is scale x sessions");
		Check(LocalSceneRoomService.HouseStanding.Any(h => h.Shows > 0), "house bands back acts in the rooms");

		// The audition sentence names a real member.
		var acts = ArtistManager.Instance.GetAllArtists().Where(a => a.lifecycleStatus == ArtistLifecycleStatus.Active && a.members != null).ToList();
		var described = acts.SelectMany(a => a.members.Where(m => m.isActive).SelectMany(m => ContactNetworkService.ContactsOf(m.personId)
				.Select(o => (act: a, member: m, other: ArtistManager.Instance.GetMusician(o)))))
			.FirstOrDefault(x => x.other != null && !x.act.members.Contains(x.other) && ContactNetworkService.Edge(x.member.personId, x.other.personId) is { Fallout: false });
		Check(described.other != null, "someone outside an act knows a member");
		string sentence = ContactNetworkService.Describe(described.other, described.act);
		Check(sentence != null && sentence.Contains(described.member.firstName), "audition note names the member they know");

		// The player asks around: tips come from real edges, never repeat, and survive a player save.
		var desk = PlayerDesk.Instance;
		var date = TimeManager.Instance.CurrentDate;
		Check(desk.FoundLabel("Contact Probe Records", described.act.geography?.basePlaceId ?? "new_york", out _), "player label founded");
		string originalLabel = described.act.labelId;
		described.act.labelId = desk.Label.labelId;
		try {
			TimeManager.Instance.RestoreClock(date, 9);
			Check(desk.AskAround(out string said), "asking around returns tips: " + said);
			var notes = desk.ContactTipNotes();
			Check(notes.Count > 0 && notes.Count <= PlayerDesk.TipsPerAsk, "tips recorded, at most three per ask");
			string firstNotes = Json(notes);
			desk.AskAround(out _);
			Check(desk.ContactTipNotes().Distinct().Count() == desk.ContactTipNotes().Count, "no duplicate tips");
			var saved = desk.CaptureState();
			var copy = JsonSerializer.Deserialize<PlayerSaveData>(Json(saved), SaveGameService.TestJsonOptions);
			Check(desk.RestoreState(copy, out string restoreMessage), "player state restores: " + restoreMessage);
			Check(Json(desk.ContactTipNotes()).Length > 0 && desk.ContactTipNotes().Any(n => firstNotes.Contains(n.Split(" · ")[1].Split(" (That")[0])),
				"tips survive the player save");
		} finally { described.act.labelId = originalLabel; }

		// Phase 3 flags.
		ContactNetworkService.Configure(new[] { "--disable-repertoire-influence", "--disable-work-migration" });
		Check(!RepertoireInfluenceService.Enabled && !WorkMigrationService.Enabled && ContactNetworkService.Enabled, "phase 3 disable flags");
		foreach (string slice in new[] { "repertoire-influence", "work-migration" }) {
			bool clash = false;
			try { ContactNetworkService.Configure(new[] { "--enable-" + slice, "--disable-" + slice }); } catch (ArgumentException) { clash = true; }
			Check(clash, slice + " contradictory flags rejected");
		}
		ContactNetworkService.Configure(Array.Empty<string>());
		Check(RepertoireInfluenceService.Enabled && WorkMigrationService.Enabled && LocalScenes.AiInvestment, "phase 3 defaults on");

		// Carried songs: a bounded, most-recent-last list, one entry per song.
		var learner = new Musician { personId = "zz_probe_learner" };
		for (int i = 0; i < RepertoireInfluenceService.CarryLimit + 2; i++) RepertoireInfluenceService.Learn(learner, "song" + i, null, "act_x", 1960);
		Check(learner.carriedSongs.Count == RepertoireInfluenceService.CarryLimit && learner.carriedSongs[0].songId == "song2", "oldest songs fall off");
		RepertoireInfluenceService.Learn(learner, "song3", "rec_9", "act_y", 1961);
		Check(learner.carriedSongs[^1] is { songId: "song3", recordId: "rec_9", viaArtistId: "act_y", year: 1961 } &&
			learner.carriedSongs.Count(c => c.songId == "song3") == 1, "relearning moves a song to the recent end with its new version");
		var live = acts.SelectMany(a => a.members).Where(m => m.carriedSongs != null).ToList();
		Check(live.Count > 0 && live.All(m => m.carriedSongs.Count <= RepertoireInfluenceService.CarryLimit &&
			m.carriedSongs.Select(c => c.songId).Distinct().Count() == m.carriedSongs.Count), "members carry the songs they played, bounded and distinct");

		// A song is carried in only from another act, and only if it beats the sample with the known-song bonus.
		var host = acts.First(a => a.members.Any(m => m.isActive));
		var holder = host.members.First(m => m.isActive);
		var standard = CompositionCatalogService.GetStandardsForGenre(host.primaryGenre).FirstOrDefault()
			?? CompositionCatalogService.GetStandardsForFamily(GenreFamily.Pop).First();
		var kept = holder.carriedSongs;
		try {
			holder.carriedSongs = new() { new CarriedSong { songId = standard.songId, viaArtistId = host.artistId, year = 1960 } };
			Check(RepertoireInfluenceService.BestCarried(host, _ => true, _ => 1f, 0f, out _, out _) == null, "a song learned in this act is not carried in");
			holder.carriedSongs[0].viaArtistId = "zz_other_act";
			bool recordedHere = standard.recordings?.Any(r => r.artistId == host.artistId) == true;
			Check(recordedHere || RepertoireInfluenceService.BestCarried(host, _ => true, _ => 1f, 0f, out var carrier, out var person) == standard &&
				person == holder && carrier.viaArtistId == "zz_other_act", "a song from another act is offered");
			Check(RepertoireInfluenceService.BestCarried(host, _ => true, _ => 1f, 1f + RepertoireInfluenceService.KnownSongBonus, out _, out _) == null,
				"a carried song must beat the sample outright");
			Check(RepertoireInfluenceService.BestCarried(host, _ => false, _ => 1f, 0f, out _, out _) == null, "a carried song must be one the source allows");
		} finally { holder.carriedSongs = kept; }

		// Work moves: real, paid, dated, domestic, between distinct towns.
		foreach (var move in WorkMigrationService.Log) {
			var mover = ArtistManager.Instance.GetMusician(move.PersonId);
			Check(move.From != move.To && move.Cost > 0 && move.DestinationWorkers >= WorkMigrationService.MinimumWorkers &&
				move.DestinationWorkers > move.HomeWorkers, "a work move goes to a busier town and costs money");
			Check(mover?.geography?.moves?.Any(x => x.toPlaceId == move.To && x.year == move.Year && x.evidence == PlaceEvidence.Simulated) == true,
				"a work move is recorded on the person");
			Check(ScenePlaceRegistry.Get(move.To)?.CountryCode == "US", "work moves are domestic");
		}
		Check(WorkMigrationService.Moved == WorkMigrationService.Log.Count, "every move is logged");
		Check(Enumerable.Range(0, 100).Select(i => WorkMigrationService.ConsentChance(new Musician { ambition = i / 99f, birthYear = 1935 }, 1961))
			.All(c => c >= .05f && c <= .80f), "consent chance bounded");

		// AI programs: independents only, one per room and per label at a time.
		var programs = SceneEcosystemService.Programs;
		var aiPrograms = programs.Where(p => ChartManager.Instance.GetLabelById(p.SponsorId) is { isPlayerOwned: false }).ToList();
		Check(aiPrograms.Count == SceneEcosystemService.AiPrograms, "AI programs counted");
		Check(aiPrograms.All(p => ChartManager.Instance.GetLabelById(p.SponsorId).tier != LabelTier.Major), "majors don't fund local programs");
		bool Overlap(SceneProgram x, SceneProgram y) => x.StartDay <= y.ThroughDay && y.StartDay <= x.ThroughDay;
		Check(!programs.Any(x => programs.Any(y => !ReferenceEquals(x, y) && x.RoomId == y.RoomId && Overlap(x, y))), "one program per room at a time");
		Check(!aiPrograms.Any(x => aiPrograms.Any(y => !ReferenceEquals(x, y) && x.SponsorId == y.SponsorId && Overlap(x, y))), "one program per label at a time");
		var eco = new WorldSaveData();
		SceneEcosystemService.CaptureWorld(eco);
		if (eco.SceneEcosystem != null) {
			Check(eco.SceneEcosystem.SchemaVersion == 2 && eco.SceneEcosystem.LastInvestmentWindow >= 0, "investment window saved");
			var v1 = JsonSerializer.Deserialize<SceneEcosystemSaveData>(Json(eco.SceneEcosystem), SaveGameService.TestJsonOptions);
			v1.SchemaVersion = 1;
			SceneEcosystemService.ValidateRestore(v1);
			checks++;
		}
		GD.Print($"CONTACT_NETWORK_PHASE3 learned={RepertoireInfluenceService.SongsLearned} chosen={RepertoireInfluenceService.CarriedChosen} " +
			$"arrangements={RepertoireInfluenceService.ArrangementsCarried} workMoves={WorkMigrationService.Moved} aiPrograms={SceneEcosystemService.AiPrograms} aiMoves={SceneEcosystemService.AiMoves}");

		// World round-trip of the network and both ledgers.
		var world = new WorldSaveData();
		ContactNetworkService.CaptureWorld(world);
		string before = Json(world.ContactNetwork);
		var restored = JsonSerializer.Deserialize<WorldSaveData>(Json(world), SaveGameService.TestJsonOptions);
		ContactNetworkService.RehydrateWorld(restored);
		var again = new WorldSaveData();
		ContactNetworkService.CaptureWorld(again);
		Check(Json(again.ContactNetwork) == before, "contact network, session work and regulars round-trip byte-identically");
		GD.Print($"CONTACT_NETWORK_CHECK_PASS checks={checks} edges={ContactNetworkService.Count}");
	}
}
