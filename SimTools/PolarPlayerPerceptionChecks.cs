using System;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Development fixture: evidence persistence, interval bounds and the real booking gate.</summary>
public static class PolarPlayerPerceptionChecks {
	private static string Json<T>(T value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
	private static void Check(bool result, string message) { if (!result) throw new InvalidOperationException("POLAR_PLAYER_PERCEPTION_FAIL: " + message); }
	public static void Run() {
		PolarSongBehavior.UsePolarFitSelection = true;
		var desk = PlayerDesk.Instance;
		Check(desk.FoundLabel("Perception fixture", DistanceModel.GetCities().First().cityId, FoundingArchetype.ExMusician, out var message), message);
		var label = desk.Label;
		var axes = Enumerable.Repeat(.5f, SongProfile.AxisCount).ToArray();
		PolarObservation Read(PolarEvidenceGate gate, string kind = "performer") => PolarPlayerPerception.Observe(axes, .5f, .5f, .5f, .5f, label, "fixture", kind, "same-listen", gate);
		var broad = Read(PolarEvidenceGate.FirstListen);
		Check(Json(broad) == Json(Read(PolarEvidenceGate.FirstListen)), "reopen stable");
		var follow = Read(PolarEvidenceGate.FollowUp);
		Check(follow.axes.Zip(broad.axes).All(p => p.First.lo >= p.Second.lo && p.First.hi <= p.Second.hi), "follow-up narrows same evidence");
		var playback = Read(PolarEvidenceGate.Playback);
		Check(playback.axes.Zip(follow.axes).All(p => p.First.lo >= p.Second.lo && p.First.hi <= p.Second.hi), "playback narrows same evidence");
		Check(Json(playback) == Json(Read(PolarEvidenceGate.FirstListen)), "weaker view retains earned evidence");
		float oldProduction = label.productionQuality, oldScouting = label.scoutingAbility;
		label.productionQuality = 1; label.scoutingAbility = 1;
		var skilled = Read(PolarEvidenceGate.Playback);
		Check(skilled.axes[0].hi - skilled.axes[0].lo < playback.axes[0].hi - playback.axes[0].lo, "better staff refines an existing read");
		label.productionQuality = oldProduction; label.scoutingAbility = oldScouting;
		var market = Read(PolarEvidenceGate.FirstListen, "market");
		var marketPlayback = Read(PolarEvidenceGate.Playback, "market");
		Check(market.axes.Skip(6).SequenceEqual(marketPlayback.axes.Skip(6)), "playback cannot sharpen market forecast");
		var saved = Json(desk.CaptureState());
		PolarPlayerPerception.Reset();
		PolarPlayerPerception.Restore(JsonSerializer.Deserialize<PlayerSaveData>(saved, SaveGameService.TestJsonOptions).PolarObservations);
		Check(Json(Read(PolarEvidenceGate.Playback)) == Json(skilled), "save/load preserves earned read");
		Check(JsonSerializer.Deserialize<PlayerSaveData>("{}", SaveGameService.TestJsonOptions).PolarObservations == null, "older saves omit evidence safely");
		CheckIntervals();

		var artist = ArtistManager.Instance.GetUnsignedArtists().First(a => desk.CoverCatalogFor(a).Count > 0);
		var choice = desk.CoverCatalogFor(artist).First();
		var song = CompositionCatalogService.GetSong(choice.SongId);
		string songBefore = Json(song), mastersBefore = Json(PolarSongMetadataService.Capture()), idBefore = desk.PreviewMasterId();
		GD.Seed(7823); float nextRandom = GD.Randf(); GD.Seed(7823);
		var view = PolarPlayerPerception.Compare(choice, artist, PolarEvidenceGate.Demo, "preview-fixture", idBefore);
		Check(GD.Randf() == nextRandom, "preview leaves GD stream untouched");
		Check(Json(view) == Json(PolarPlayerPerception.Compare(choice, artist, PolarEvidenceGate.Demo, "preview-fixture", idBefore)), "comparison stable");
		Check(Json(song) == songBefore && Json(PolarSongMetadataService.Capture()) == mastersBefore && desk.PreviewMasterId() == idBefore, "preview leaves song, masters and ID allocation untouched");
		var other = ArtistManager.Instance.GetUnsignedArtists().First(a => a.primaryGenre != artist.primaryGenre);
		Check(Json(view.act.axes) != Json(PolarPlayerPerception.Compare(choice, other, PolarEvidenceGate.Demo, "preview-fixture", idBefore).act.axes), "same song compares differently across acts");
		CheckRefusal(desk);
		GD.Print("POLAR_PLAYER_PERCEPTION_PASS reopen=ok evidence=ok staff=ok market=ok save=ok intervals=ok rng=ok purePreview=ok comparison=ok refusal=ok override=ok disabled=ok");
	}
	private static void CheckIntervals() {
		// Samples exercise the renderer's band arithmetic independently of observer bias.
		var random = new Random(441);
		for (int trial = 0; trial < 120; trial++) {
			PolarBand[] Bands(int count) => Enumerable.Range(0, count).Select(_ => {
				float low = (float)random.NextDouble(); return new PolarBand(low, low + (float)random.NextDouble() * (1 - low));
			}).ToArray();
			var material = Bands(10); var actor = Bands(10); var market = Bands(4); var rigidity = Bands(1)[0];
			var predicted = PolarPlayerPerception.FitBands(material, actor, rigidity, market);
			float Sample(PolarBand band) => band.lo + (float)random.NextDouble() * (band.hi - band.lo);
			for (int draw = 0; draw < 30; draw++) {
				var song = new SongProfile { axes = material.Select(Sample).ToArray() };
				var act = new ActProfile { axes = actor.Select(Sample).ToArray(), identityRigidity = Sample(rigidity) };
				var taste = new MarketTasteSnapshot { identity = market.Select(Sample).ToArray(), observations = 1, asOfWeek = 1 };
				var fit = PolarMaterialFit.Evaluate(song, song, act, taste, 2, PolarSongTable.Current);
				var values = new[] { fit.Capability, fit.Identity, fit.Moment };
				Check(values.Select((value, i) => value >= predicted[i].lo - .00001f && value <= predicted[i].hi + .00001f).All(v => v), "fit interval contains sampled realization");
			}
		}
	}
	public static (SimulatedArtist Artist, PlayerDesk.MaterialChoice Choice) RefusalFixture(PlayerDesk desk) {
		var artist = new SimulatedArtist { artistId = "resistance-fixture", stageName = "The Test Players", labelId = desk.Label.labelId,
			type = ArtistType.Band, careerState = CareerState.Superstar, groupCohesion = 0,
			evolution = new ArtistEvolutionProfile { artisticAmbition = 1, rootsAttachment = 1, experimentalAppetite = 1 },
			members = new() { new Musician { isActive = true, creativity = 1, temperament = 1, reliability = 0 } } };
		SongComposition refused = null;
		// Find an actual resolver refusal; do not mock the gate or change recording thresholds.
		foreach (Genre genre in Enum.GetValues<Genre>()) {
			artist.primaryGenre = genre;
			foreach (var row in PolarSongTable.Current.Archetypes) {
				var candidate = new SongComposition { songId = "resistance-song", title = "Someone Else's Tune", primaryGenre = Genre.TraditionalPop,
					plasticity = 1, plasticityFrozen = true, demoTaxonomy = new SongTaxonomy { primaryGenre = Genre.TraditionalPop, archetype = Enum.Parse<SongArchetype>(row.Name) } };
				var act = PolarActProfileDeriver.Derive(artist, desk.Label, desk.PreviewSessionContext(PlayerDesk.StudioTier.Budget, artist), PolarSongTable.Current);
				var proposal = PolarCoverResolver.Propose(candidate, null, act, genre, 1960, 1, desk.PreviewMasterId(), null, PolarSongTable.Current);
				if (PolarMaterialFit.WouldRefuse(proposal.fit, 1, 1, false, PolarSongTable.Current)) { refused = candidate; break; }
			}
			if (refused != null) break;
		}
		Check(refused != null, "real resolver produces a strong pushback fixture");
		var world = new WorldSaveData(); CompositionCatalogService.CaptureWorld(world);
		world.Composition.Songs[refused.songId] = refused; CompositionCatalogService.RehydrateWorld(world);
		var choice = new PlayerDesk.MaterialChoice { SongId = refused.songId, Title = refused.title, Kind = PlayerDesk.MaterialKind.LiveCover, ReferenceMasterId = "demo:" + refused.songId, Detail = "fixture cover" };
		var player = desk.CaptureState();
		player.Label.RosterArtistIds.Add(artist.artistId); player.RosterArtists.Add(artist);
		player.Repertoire[artist.artistId] = new() {
			RepertoireSaveData.From(new PlayerDesk.RepertoireItem { Title = "Our Own Tune", IsOriginal = true, SourceTag = "their own" }),
			RepertoireSaveData.From(new PlayerDesk.RepertoireItem { Title = choice.Title, SongId = choice.SongId, ReferenceMasterId = choice.ReferenceMasterId, SourceTag = choice.Detail }) };
		Check(desk.RestoreState(player, out var message), message);
		return (artist, choice);
	}
	private static void CheckRefusal(PlayerDesk desk) {
		var (artist, choice) = RefusalFixture(desk);
		var choices = new[] { choice };
		float cash = desk.Label.cashReserves; int hour = TimeManager.Instance.CurrentHour; string id = desk.PreviewMasterId();
		Check(desk.MaterialRefusals(artist, choices, PlayerDesk.StudioTier.Budget).Count == 1, "strong pushback communicates a response");
		Check(!desk.StartSession(artist, choices, PlayerDesk.StudioTier.Budget, 2, out var message), "booking requires an answer");
		Check(desk.Label.cashReserves == cash && TimeManager.Instance.CurrentHour == hour && desk.Session == null && desk.PreviewMasterId() == id, "refusal spends no cash/time and allocates no master ID");
		PolarSongBehavior.UsePolarFitSelection = false;
		Check(desk.MaterialRefusals(artist, choices).Count == 0, "disabled mode skips the refusal gate");
		PolarSongBehavior.UsePolarFitSelection = true;
		Check(desk.StartSession(artist, choices, PlayerDesk.StudioTier.Budget, 2, out message, overrideRefusal: true), "explicit insist books: " + message);
		Check(desk.Session != null && desk.Label.cashReserves == cash - desk.Session.Cost, "override charges normal session exactly once");
	}
}
