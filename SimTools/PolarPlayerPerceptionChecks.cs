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
		CheckHearings(desk, artist, choice, idBefore);
		CheckRefusal(desk);
		GD.Print("POLAR_PLAYER_PERCEPTION_PASS reopen=ok evidence=ok staff=ok market=ok save=ok intervals=ok rng=ok purePreview=ok comparison=ok hearings=ok refusal=ok override=ok disabled=ok");
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
	/// <param name="margin">How far past the refusal bar the stretch must sit; 1 takes the first real refusal.</param>
	public static (SimulatedArtist Artist, PlayerDesk.MaterialChoice Choice) RefusalFixture(PlayerDesk desk, float margin = 1) {
		var artist = new SimulatedArtist { artistId = "resistance-fixture", stageName = "The Test Players", labelId = desk.Label.labelId,
			type = ArtistType.Band, careerState = CareerState.Superstar, groupCohesion = 0,
			evolution = new ArtistEvolutionProfile { artisticAmbition = 1, rootsAttachment = 1, experimentalAppetite = 1 },
			members = new() { new Musician { isActive = true, creativity = 1, temperament = 1, reliability = 0 } } };
		SongComposition refused = null;
		// Find an actual resolver refusal; do not mock the gate or change recording thresholds.
		foreach (Genre genre in Enum.GetValues<Genre>()) {
			artist.primaryGenre = genre;
			foreach (var fixture in PolarSongTable.Current.Archetypes.SelectMany(row => Enumerable.Range(0,16).Select(sample => (row,sample)))) {
				var row = fixture.row;
				var candidate = new SongComposition { songId = "resistance-song-" + row.Name + "-" + fixture.sample, title = "Someone Else's Tune", primaryGenre = Genre.TraditionalPop,
					plasticity = 1, plasticityFrozen = true, demoTaxonomy = new SongTaxonomy { primaryGenre = Genre.TraditionalPop, archetype = Enum.Parse<SongArchetype>(row.Name) } };
				// A real authored composition already carries its persistent offset before preview.
				PolarSongMetadataService.EnsureComposition(candidate);
				var act = PolarActProfileDeriver.Derive(artist, desk.Label, desk.PreviewSessionContext(PlayerDesk.StudioTier.Budget, artist), PolarSongTable.Current);
				var proposal = PolarCoverResolver.Propose(candidate, null, act, genre, 1960, 1, desk.PreviewMasterId(), null, PolarSongTable.Current);
				if (PolarMaterialFit.WouldRefuse(proposal.fit, 1, 1, false, PolarSongTable.Current) &&
					proposal.fit.Stretch >= proposal.fit.Capability * PolarSongTable.Current.N("refusalStretchCapability") * margin) { refused = candidate; break; }
			}
			if (refused != null) break;
		}
		Check(refused != null, "real resolver produces a strong pushback fixture");
		var world = new WorldSaveData(); CompositionCatalogService.CaptureWorld(world);
		world.Composition.Songs[refused.songId] = refused; CompositionCatalogService.RehydrateWorld(world);
		var choice = new PlayerDesk.MaterialChoice { SongId = refused.songId, Title = refused.title, Kind = PlayerDesk.MaterialKind.LiveCover, ReferenceMasterId = "demo:" + refused.songId, Detail = "fixture cover" };
		var player = desk.CaptureState();
		// The fixture may be built more than once in a run; keep one roster entry for the fixture act.
		player.Label.RosterArtistIds.Remove(artist.artistId); player.RosterArtists.RemoveAll(a => a.artistId == artist.artistId);
		player.Label.RosterArtistIds.Add(artist.artistId); player.RosterArtists.Add(artist);
		player.Repertoire[artist.artistId] = new() {
			RepertoireSaveData.From(new PlayerDesk.RepertoireItem { Title = "Our Own Tune", IsOriginal = true, SourceTag = "their own" }),
			RepertoireSaveData.From(new PlayerDesk.RepertoireItem { Title = choice.Title, SongId = choice.SongId, ReferenceMasterId = choice.ReferenceMasterId, SourceTag = choice.Detail }) };
		Check(desk.RestoreState(player, out var message), message);
		return (artist, choice);
	}
	/// <summary>Ear hearings read the song itself; only studio hearings project an act, fit or pushback.</summary>
	private static void CheckHearings(PlayerDesk desk, SimulatedArtist artist, PlayerDesk.MaterialChoice choice, string id) {
		var label = desk.Label;
		float oldProduction = label.productionQuality, oldScouting = label.scoutingAbility;
		label.productionQuality = 1; label.scoutingAbility = 0;
		var venue = new PolarHearing { source = PolarHearingSource.Venue, place = "the honky-tonks", when = GameDate.StartDate, heardHook = .62f, heardHookConfidence = .5f };
		var live = PolarPlayerPerception.Compare(choice, artist, PolarEvidenceGate.FirstListen, "hearing-fixture", id, hearing: venue);
		Check(live.source == PolarHearingSource.Venue && live.showsAct && !live.showsFit && !live.mayResist && live.resistance == "", "venue read projects no fit or pushback");
		Check(live.subjectLabel.StartsWith("Heard live · the honky-tonks"), "venue read names where it was heard: " + live.subjectLabel);
		Check(live.hook.lo < .62f && live.hook.hi > .62f && live.hookText == "Likely a strong tune", "venue hook agrees with the set list read: " + live.hookText);
		Check(!string.IsNullOrEmpty(live.tightnessText), "watching the act yields a tightness read");
		var studio = PolarPlayerPerception.Compare(choice, artist, PolarEvidenceGate.FirstListen, "hearing-fixture", id);
		float Width(PolarBand band) => band.hi - band.lo;
		Check(Width(live.act.axes[0]) > Width(studio.act.axes[0]), "a stage read of the act is the scout's ear, not the producer's");
		label.productionQuality = oldProduction; label.scoutingAbility = oldScouting;

		var record = PolarPlayerPerception.Compare(choice, artist, PolarEvidenceGate.Demo, "hearing-fixture", id,
			hearing: new PolarHearing { source = PolarHearingSource.Record, fitWithAct = true });
		Check(record.heard == record.reference && !record.showsAct && record.showsFit && record.referenceFit?.Length == 3 && record.resistance == "", "record read is the song plus a small fit");
		Check(record.subjectLabel.StartsWith("Heard on record · ") || record.subjectLabel.StartsWith("Known from the sheet music"), "record read names its source: " + record.subjectLabel);
		Check(Json(record) == Json(PolarPlayerPerception.Compare(choice, artist, PolarEvidenceGate.Demo, "hearing-fixture", id,
			hearing: new PolarHearing { source = PolarHearingSource.Record, fitWithAct = true })), "record read stable on reopen");
		Check(!studio.mayResist || studio.resistance != "", "studio pushback text appears only with a confident read");
		Check(studio.mayResist || studio.resistance == "", "no default pushback line");
	}
	private static void CheckRefusal(PlayerDesk desk) {
		// Warnings are for refusals the staff can be sure of; a knife-edge refusal may stay unflagged.
		var (clearArtist, clearChoice) = RefusalFixture(desk, 1.3f);
		PolarComparisonRead Studio(PolarEvidenceGate gate, string eventId) => PolarPlayerPerception.Compare(clearChoice, clearArtist, gate, eventId,
			desk.PreviewMasterId(), desk.PreviewSessionContext(PlayerDesk.StudioTier.Budget, clearArtist));
		var sure = Studio(PolarEvidenceGate.Rehearsal, "refusal-read");
		Check(sure.mayResist && sure.resistance.StartsWith("Expect pushback"), "a working read of a clear refusal warns: " + sure.resistance);
		Check(!Studio(PolarEvidenceGate.Demo, "refusal-read-demo").mayResist, "a rough demo read is never sure enough to warn");
		Check(!PolarPlayerPerception.Compare(clearChoice, clearArtist, PolarEvidenceGate.Playback, "refusal-read-venue", desk.PreviewMasterId(),
			hearing: new PolarHearing { source = PolarHearingSource.Venue, place = "a club", heardHook = .5f, heardHookConfidence = .5f }).mayResist, "pushback is never read away from the studio");
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
