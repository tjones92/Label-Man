using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Game-facing standing, lifecycle separation, actual dossier/roster controls, and save compatibility.</summary>
public static class StatureCareerDisplayChecks {
	public static void CheckLiveState() {
		if (!ArtistStature.CareerDisplay) return;
		var world = new WorldSaveData(); MediaAtmosphereService.CaptureWorld(world);
		var shadow = world.Media?.ExperimentalStature;
		if (world.Media?.StatureReader?.CareerDisplay != true || shadow == null || shadow.Normalizer.LastProcessedDate == default)
			throw new InvalidOperationException("Live career display is missing its saved setting or weekly audience state.");
		var date = TimeManager.Instance.CurrentDate;
		int available = 0, different = 0;
		foreach (var label in CompetitorManager.Instance.GetAllLabels().Where(l => l != null && !l.isPlayerOwned))
			foreach (var artist in label.roster ?? new List<SimulatedArtist>()) {
				if (artist == null || ArtistStature.AudienceStanding(artist, date).Status != ArtistStature.StandingStatus.Available) continue;
				var displayed = ArtistStature.DisplayCareer(artist, date);
				if (ArtistManager.Instance.GetPublicProfile(artist.artistId)?.careerState != displayed)
					throw new InvalidOperationException("Live public file disagrees with the audience-standing display.");
				available++;
				if (displayed != artist.careerState) different++;
			}
		if (shadow.Normalizer.LastProcessedDate >= StatureObservation.ShadowConfig.Policy.EffectiveDate && available == 0)
			throw new InvalidOperationException("Effective career-display calibration produced no available roster standing.");
		GD.Print($"STATURE_CAREER_DISPLAY_LIVE passed date={date.ToShortString()} available={available} differentFromLegacy={different}");
	}

	private static IEnumerable<Node> Nodes(Node root) {
		if (root.IsQueuedForDeletion()) yield break;
		yield return root;
		foreach (Node child in root.GetChildren()) foreach (var node in Nodes(child)) yield return node;
	}
	private static bool HasText(Node root, string text) => Nodes(root).OfType<Label>().Any(l => l.Text == text);

	public static void Run(Node host) {
		int checks = 0;
		void Check(bool value, string message) {
			if (!value) throw new InvalidOperationException("Stature career display: " + message);
			checks++;
		}
		var original = new WorldSaveData();
		MediaAtmosphereService.CaptureWorld(original);
		var oldConfig = StatureObservation.ShadowConfig;
		bool oldReader = ArtistStature.ProgrammeAlbumReader, oldDisplay = ArtistStature.CareerDisplay;
		bool oldPersist = MediaAtmosphereService.PersistExperimentalStature, oldFollowing = MediaFollowing.Active;
		var oldMode = MediaAtmosphere.Mode;
		var opts = SaveGameService.TestJsonOptions;
		ArtistDetailPanel dossier = null;
		LabelDetailPanel roster = null;
		try {
			MediaAtmosphere.SetForProbe(MediaAtmosphereMode.Observe);
			MediaFollowing.SetForChecks(true);
			ArtistStature.Configure(Array.Empty<string>());
			MediaAtmosphereService.ResetForProbe();
			var date = TimeManager.Instance.CurrentDate;
			var day = new DateTime(date.year, date.month, date.day);
			var artist = new SimulatedArtist { artistId = "display-check", labelId = "ai-check", careerState = CareerState.NewSigning };
			bool rejected = false;
			try { ArtistStature.Configure(new[] { ArtistStature.DisplaySwitch }); }
			catch (ArgumentException) { rejected = true; }
			Check(rejected, "display requires a calibration");
			ArtistStature.Configure(new[] { ArtistStature.DisplaySwitch,
				ArtistStature.CalibrationSwitch + ProjectSettings.GlobalizePath("res://SimTools/StatureShadowExperimental.json") });
			Check(ArtistStature.CareerDisplay && !ArtistStature.ProgrammeAlbumReader && MediaAtmosphereService.PersistExperimentalStature,
				"display can run and persist independently of the financial reader");
			var empty = new WorldSaveData(); MediaAtmosphereService.CaptureWorld(empty);
			Check(ArtistStature.AudienceStanding(artist, date).Status == ArtistStature.StandingStatus.Missing &&
				ArtistStature.DisplayCareer(artist, date) == CareerState.NewSigning && empty.Media.ExperimentalStature == null,
				"missing evidence falls back without allocating a saved shadow");
			var snapshot = new StatureWeeklyShadow.Snapshot { Date = day, Band = StatureShadowPolicy.Band.Star,
				Status = StatureShadowPolicy.ResultStatus.Observed };
			var shadow = MediaAtmosphereService.ExperimentalStatureShadow();
			shadow.Snapshots[artist.artistId] = snapshot;
			Check(ArtistStature.DisplayCareer(artist, date) == CareerState.Star && ArtistStature.ContractContext(artist) == "New signing" &&
				artist.careerState == CareerState.NewSigning, "same-day standing displays while new-contract state stays intact");
			Check(ArtistStature.ProgrammeAlbumCareer(new AILabel(), artist, date.AddDays(1)) == null,
				"display opt-in does not enable the programme reader");
			ArtistStature.CareerDisplay = false;
			Check(ArtistStature.DisplayCareer(artist, date) == CareerState.NewSigning && ArtistStature.ContractContext(artist) == null,
				"disabled display preserves the old presentation");
			ArtistStature.CareerDisplay = true;
			foreach (var band in new[] { StatureShadowPolicy.Band.Rising, StatureShadowPolicy.Band.Established,
				StatureShadowPolicy.Band.Star, StatureShadowPolicy.Band.Superstar }) {
				snapshot.Band = band;
				Check(ArtistStature.DisplayCareer(artist, date).ToString() == band.ToString(), "existing rank name: " + band);
			}
			snapshot.Band = StatureShadowPolicy.Band.BelowRising; snapshot.Status = StatureShadowPolicy.ResultStatus.BelowRising;
			Check(ArtistStature.DisplayCareer(artist, date) == CareerState.NewSigning, "below Rising preserves a new signing's context");
			artist.careerState = CareerState.Star;
			Check(ArtistStature.DisplayCareer(artist, date) == CareerState.Declining, "genuine low standing can display cooling");
			snapshot.Status = StatureShadowPolicy.ResultStatus.MissingEvidence; snapshot.Band = StatureShadowPolicy.Band.Superstar;
			Check(ArtistStature.DisplayCareer(artist, date) == CareerState.Star, "missing evidence does not display retained hysteresis as a new rank");
			snapshot.Status = StatureShadowPolicy.ResultStatus.Observed;
			snapshot.Date = day.AddDays(1);
			Check(ArtistStature.AudienceStanding(artist, date).Status == ArtistStature.StandingStatus.Future, "future snapshot excluded");
			snapshot.Date = day.AddDays(-8);
			Check(ArtistStature.DisplayCareer(artist, date) == CareerState.Star, "stale snapshot falls back");
			snapshot.Date = day;
			artist.careerState = CareerState.Declining;
			Check(ArtistStature.DisplayCareer(artist, date) == CareerState.Superstar, "a comeback's audience is independent of legacy decline");
			artist.careerState = CareerState.Dropped; artist.labelId = null;
			Check(ArtistStature.DisplayCareer(artist, date) == CareerState.Dropped &&
				ArtistStature.AudienceStanding(artist, date).Band == StatureShadowPolicy.Band.Superstar,
				"dropping remains visible without erasing off-roster audience standing");
			ArtistManager.ReconcileSignedArtistForProbe(artist, new List<SimulatedArtist> { artist }, "new-label", date.year);
			Check(ArtistStature.DisplayCareer(artist, date) == CareerState.Superstar && artist.careerState == CareerState.NewSigning,
				"normal signing transition carries audience forward while starting a new contract");
			artist.isPlayerOwned = true;
			Check(ArtistStature.DisplayCareer(artist, date) == CareerState.NewSigning, "player artist retains existing progression");
			artist.isPlayerOwned = false;
			artist.lifecycleStatus = ArtistLifecycleStatus.Retired;
			Check(ArtistStature.DisplayCareer(artist, date) == CareerState.Retired, "retirement is authoritative");
			artist.lifecycleStatus = ArtistLifecycleStatus.Disbanded;
			Check(ArtistStature.DisplayCareer(artist, date) == CareerState.Disbanded, "disbandment is authoritative");

			// Exercise the controls against a real generated roster artist, not a mocked public profile.
			var label = CompetitorManager.Instance.GetAllLabels().First(l => l != null && !l.isPlayerOwned &&
				l.roster?.Count is > 0 and < 8 && l.roster.Any(a => a != null && !a.isPlayerOwned && a.lifecycleStatus == ArtistLifecycleStatus.Active));
			var live = label.roster.First(a => a != null && !a.isPlayerOwned && a.lifecycleStatus == ArtistLifecycleStatus.Active);
			var priorCareer = live.careerState; bool priorComeback = live.contractUsesExperiencedComebackPolicy;
			try {
				live.careerState = CareerState.NewSigning; live.contractUsesExperiencedComebackPolicy = false;
				snapshot.Band = StatureShadowPolicy.Band.Star;
				shadow.Snapshots[live.artistId] = snapshot;
				string beforeArtist = JsonSerializer.Serialize(live, opts), beforeShadow = JsonSerializer.Serialize(shadow, opts);
				ulong rng = GameRandom.State;
				dossier = new ArtistDetailPanel(); host.AddChild(dossier); dossier.ShowArtist(live.artistId);
				Check(HasText(dossier, "Star") && HasText(dossier, "New signing"), "actual dossier shows rank and contract context");
				roster = new LabelDetailPanel(); host.AddChild(roster); roster.ShowLabel(label.labelId);
				Nodes(roster).OfType<Button>().Single(b => b.Text == "ROSTER").EmitSignal(Button.SignalName.Pressed);
				var card = Nodes(roster).OfType<SleeveCard>().Single(c => HasText(c, live.stageName));
				Check(HasText(card, (GenreNameFormatter.Format(live.primaryGenre) + "  •  Star").ToUpperInvariant()) &&
					HasText(card, "New signing"), "actual label roster uses the same standing and contract context");
				Check(JsonSerializer.Serialize(live, opts) == beforeArtist && JsonSerializer.Serialize(shadow, opts) == beforeShadow && GameRandom.State == rng,
					"opening both panels changes no artist, audience snapshot or RNG state");
				try {
					dossier.ShowArtist(live.artistId, isOwnedByPlayer: true);
					Check(HasText(dossier, "New Signing") && !HasText(dossier, "Star"),
						"explicit player-owned dossier request applies ownership before building its public profile");
				} finally { live.isPlayerOwned = false; }
			} finally { live.careerState = priorCareer; live.contractUsesExperiencedComebackPolicy = priorComeback; }

			var save = new WorldSaveData(); MediaAtmosphereService.CaptureWorld(save);
			string json = JsonSerializer.Serialize(save.Media, opts);
			ArtistStature.Configure(Array.Empty<string>());
			MediaAtmosphereService.RehydrateWorld(new WorldSaveData { Media = JsonSerializer.Deserialize<MediaSaveData>(json, opts) });
			Check(ArtistStature.CareerDisplay && !ArtistStature.ProgrammeAlbumReader && MediaAtmosphereService.StatureSnapshotFor(artist.artistId) != null,
				"display-only save restores its setting and audience without enabling the financial reader");
			string legacy = JsonSerializer.Serialize(new { Calibration = StatureObservation.ShadowConfig }, opts);
			var oldSavedReader = JsonSerializer.Deserialize<StatureReaderSaveData>(legacy, opts);
			Check(oldSavedReader.ProgrammeAlbumReader && !oldSavedReader.CareerDisplay, "previous calibration-only saves retain their programme-reader meaning");
			MediaAtmosphereService.RehydrateWorld(new WorldSaveData());
			Check(!ArtistStature.CareerDisplay && !ArtistStature.ProgrammeAlbumReader && StatureObservation.ShadowConfig == null,
				"an older slot does not inherit display or reader settings");
			GD.Print($"STATURE_CAREER_DISPLAY_CHECK passed checks={checks}");
		} finally {
			dossier?.QueueFree(); roster?.QueueFree();
			MediaAtmosphereService.RehydrateWorld(original);
			StatureObservation.ShadowConfig = oldConfig;
			ArtistStature.ProgrammeAlbumReader = oldReader; ArtistStature.CareerDisplay = oldDisplay;
			MediaAtmosphereService.PersistExperimentalStature = oldPersist;
			MediaAtmosphere.SetForProbe(oldMode); MediaFollowing.SetForChecks(oldFollowing);
		}
	}
}
