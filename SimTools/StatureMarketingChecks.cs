using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Exercises the marketing table, actual programme funding, paid follow-up campaigns, and saved reader settings.</summary>
public static class StatureMarketingChecks {
	private static int liveReads, liveChanges;
	private static readonly HashSet<string> liveContexts = new(StringComparer.Ordinal);
	private static void ObserveLive(ArtistStature.MarketingRead read) {
		liveReads++; if (read.Budget != read.LegacyBudget) liveChanges++;
		liveContexts.Add(read.Context);
		if (!float.IsFinite(read.Budget) || read.Budget < 0 || read.StandingCareer.HasValue &&
			((new DateTime(read.DecisionDate.year, read.DecisionDate.month, read.DecisionDate.day) - read.SnapshotDate).Days is < 0 or > 7))
			throw new InvalidOperationException("Invalid live stature marketing envelope or snapshot timing.");
	}
	public static void BeginLiveObservation() {
		if (!ArtistStature.MarketingReader) return;
		liveReads = liveChanges = 0; liveContexts.Clear();
		ArtistStature.OnMarketingRead -= ObserveLive;
		ArtistStature.OnMarketingRead += ObserveLive;
	}
	public static void CheckLiveState() {
		if (!ArtistStature.MarketingReader) return;
		var world = new WorldSaveData(); MediaAtmosphereService.CaptureWorld(world);
		if (world.Media?.StatureReader?.MarketingReader != true || world.Media.ExperimentalStature == null || liveReads == 0)
			throw new InvalidOperationException("Live marketing reader lacks saved settings, weekly state or actual decision reads.");
		GD.Print($"STATURE_MARKETING_LIVE passed reads={liveReads} changed={liveChanges} contexts={string.Join(',', liveContexts.OrderBy(c => c))}");
	}

	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Stature marketing: " + message); checks++; }
		var manager = CompetitorManager.Instance;
		var original = new WorldSaveData(); MediaAtmosphereService.CaptureWorld(original);
		var config = StatureObservation.ShadowConfig;
		bool reader = ArtistStature.ProgrammeAlbumReader, display = ArtistStature.CareerDisplay, marketing = ArtistStature.MarketingReader;
		bool legacyCampaignShare = ArtistStature.LegacyCampaignShare;
		float originalBlend = ArtistStature.MarketingBlend;
		bool persist = MediaAtmosphereService.PersistExperimentalStature, following = MediaFollowing.Active;
		var media = MediaAtmosphere.Mode;
		var programme = manager.ProgrammeState;
		bool allocation = LabelReleaseProgramme.AllocationEnabled, capacity = LabelReleaseProgramme.CapacityEnabled;
		bool campaigns = RecordCampaignSpend.Enabled;
		float riskFraction = SingleCapitalRisk.CapitalFraction; var riskPolicy = SingleCapitalRisk.Policy;
		var reads = new List<ArtistStature.MarketingRead>();
		void Observe(ArtistStature.MarketingRead read) => reads.Add(read);
		try {
			MediaAtmosphere.SetForProbe(MediaAtmosphereMode.Observe); MediaFollowing.SetForChecks(true);
			ArtistStature.Configure(Array.Empty<string>()); MediaAtmosphereService.ResetForProbe();
			bool rejected = false;
			try { ArtistStature.Configure(new[] { ArtistStature.MarketingSwitch }); } catch (ArgumentException) { rejected = true; }
			Check(rejected, "marketing requires an explicit calibration");
			rejected = false;
			try { ArtistStature.Configure(new[] { ArtistStature.LegacyCampaignShareSwitch }); } catch (ArgumentException) { rejected = true; }
			Check(rejected, "the campaign-share experiment requires the marketing reader");
			ArtistStature.Configure(new[] { ArtistStature.MarketingSwitch,
				ArtistStature.CalibrationSwitch + ProjectSettings.GlobalizePath("res://SimTools/StatureShadowExperimental.json") });
			Check(ArtistStature.MarketingReader && !ArtistStature.ProgrammeAlbumReader && !ArtistStature.CareerDisplay &&
				MediaAtmosphereService.PersistExperimentalStature, "marketing is an independent persisted opt-in");
			ArtistStature.OnMarketingRead += Observe;
			var date = TimeManager.Instance.CurrentDate; var day = new DateTime(date.year, date.month, date.day);
			var label = new AILabel { labelId = "marketing-check", tier = LabelTier.Independent, marketingPower = .7f, cashReserves = 1e7f, releasesPerMonth = 1 };
			var artist = new SimulatedArtist { artistId = "marketing-act", labelId = label.labelId, careerState = CareerState.NewSigning,
				primaryGenre = Genre.TraditionalPop, vocalPower = .7f, musicianship = .7f, songwritingAbility = .7f };
			float Legacy() => label.GetMarketingBudget(artist);
			float Read() => label.GetReleaseMarketingBudget(artist, "fixture", date);
			Check(Read() == Legacy() && reads[^1].Status == "Missing", "missing snapshot uses the exact existing budget");
			artist.artistId = null;
			Check(Read() == Legacy() && reads[^1].Status == "Missing", "missing artist identity falls back without a registry lookup");
			artist.artistId = "marketing-act";
			var shadow = MediaAtmosphereService.ExperimentalStatureShadow();
			var snapshot = new StatureWeeklyShadow.Snapshot { Date = day, Band = StatureShadowPolicy.Band.Star, Status = StatureShadowPolicy.ResultStatus.Observed };
			shadow.Snapshots[artist.artistId] = snapshot;
			float raw = Legacy(), expected = label.MarketingBudgetForCareer(CareerState.Star);
			ArtistStature.MarketingBlend = .5f;
			Check(Read() == raw + .5f * (expected - raw), "half blend applies an upward change");
			artist.careerState = CareerState.Superstar;
			Check(Read() == Legacy() + .5f * (expected - Legacy()) && Read() < Legacy(), "half blend preserves a downward change");
			ArtistStature.MarketingBlend = 0;
			Check(Read() == Legacy(), "zero blend preserves legacy exactly");
			ArtistStature.MarketingBlend = 1;
			artist.careerState = CareerState.NewSigning;
			string before = JsonSerializer.Serialize(artist, SaveGameService.TestJsonOptions); ulong rng = GameRandom.State;
			Check(Read() == expected && Read() > raw && Legacy() == raw && artist.careerState == CareerState.NewSigning,
				"same-day completed snapshot changes the release envelope while raw/acquisition budgets retain the old rung");
			Check(reads[^1].Context == "fixture" && reads[^1].SnapshotDate == day && reads[^1].LegacyBudget == raw && reads[^1].Budget == expected,
				"trace identifies the context, dated snapshot, old and requested envelope before caps");
			Check(JsonSerializer.Serialize(artist, SaveGameService.TestJsonOptions) == before && GameRandom.State == rng,
				"budget reads change no artist state or RNG");
			foreach (var band in new[] { StatureShadowPolicy.Band.Rising, StatureShadowPolicy.Band.Established, StatureShadowPolicy.Band.Star, StatureShadowPolicy.Band.Superstar }) {
				snapshot.Band = band; artist.careerState = Enum.Parse<CareerState>(band.ToString());
				Check(Read() == Legacy(), "rung-equivalent marketing table: " + band);
			}
			artist.careerState = CareerState.Star; snapshot.Band = StatureShadowPolicy.Band.BelowRising; snapshot.Status = StatureShadowPolicy.ResultStatus.BelowRising;
			Check(Read() == label.MarketingBudgetForCareer(CareerState.Declining) && Read() < Legacy(), "quiet standing can reduce investment without an uplift floor");
			artist.careerState = CareerState.Declining; snapshot.Band = StatureShadowPolicy.Band.Star; snapshot.Status = StatureShadowPolicy.ResultStatus.Observed;
			Check(Read() == expected && Read() > Legacy(), "an active declining act can earn a comeback budget");
			artist.careerState = CareerState.NewSigning;
			foreach (string fallback in new[] { "future", "stale", "missing", "player-artist", "player-label", "terminal", "dropped", "other-owner" }) {
				snapshot.Date = fallback == "future" ? day.AddDays(1) : fallback == "stale" ? day.AddDays(-8) : day;
				snapshot.Status = fallback == "missing" ? StatureShadowPolicy.ResultStatus.MissingEvidence : StatureShadowPolicy.ResultStatus.Observed;
				artist.isPlayerOwned = fallback == "player-artist"; label.isPlayerOwned = fallback == "player-label";
				artist.lifecycleStatus = fallback == "terminal" ? ArtistLifecycleStatus.Retired : ArtistLifecycleStatus.Active;
				artist.careerState = fallback == "dropped" ? CareerState.Dropped : CareerState.NewSigning;
				artist.labelId = fallback == "other-owner" ? "seller" : label.labelId;
				Check(Read() == Legacy(), "legacy fallback: " + fallback);
			}
			artist.labelId = label.labelId; artist.careerState = CareerState.NewSigning;
			artist.isPlayerOwned = label.isPlayerOwned = false; artist.lifecycleStatus = ArtistLifecycleStatus.Active;
			snapshot.Date = day; snapshot.Status = StatureShadowPolicy.ResultStatus.Observed;
			ArtistStature.MarketingReader = false;
			Check(Read() == Legacy() && reads[^1].Budget == Legacy() && reads[^1].ProposedBudget == expected,
				"shadow logging retains the exact raw budget while reporting the counterfactual");
			Check(JsonSerializer.Serialize(artist, SaveGameService.TestJsonOptions) == before && GameRandom.State == rng,
				"shadow budget logging changes no artist state or RNG");
			ArtistStature.MarketingReader = true;

			// Test the ordinary funding gate with the new requested budget, then the same gate with its normal capacity cap.
			LabelReleaseProgramme.SetForChecks(true, false); manager.SetProgrammeForChecks(new ReleaseProgrammeState());
			SingleCapitalRisk.SetForChecks(0);
			var item = new ReleaseSlateItem { Id = "marketing-reserve", LabelId = label.labelId, ArtistId = artist.artistId,
				Format = ReleaseFormat.Single, State = ReleaseSlateState.Scheduled, DueWeek = 1 };
			Check(manager.ReserveSlate(label, artist, item, date, out _) && item.ReservedMarketing == expected,
				"actual slate reservation uses the stature marketing quote");
			label.cashReserves = 0;
			Check(!manager.ReserveSlate(label, artist, item, date, out _), "a larger standing cannot bypass the funding gate");
			label.cashReserves = 1e7f;
			LabelReleaseProgramme.SetForChecks(true, true);
			Check(manager.ReserveSlate(label, artist, item, date, out _) && item.ReservedMarketing <= label.GetStandardMarketingBudget(),
				"ordinary campaign capacity still caps the requested envelope");
			Check(AiMediaPlanner.EnvelopeEstimate(label, artist) == expected, "media allocation estimates use the same own-roster table");

			float paid = raw * .5f;
			var coupled = new Record { recordId = "marketing-coupled", format = ReleaseFormat.Album };
			manager.BookProgrammeCampaign(coupled, artist, label, paid);
			Check(manager.ProgrammePushFactor(new RecordRuntimeData { baseRecord = coupled }) == paid / expected,
				"ordinary booking divides paid spend by the standing envelope");
			ArtistStature.LegacyCampaignShare = true;
			var legacyShare = new Record { recordId = "marketing-legacy-share", format = ReleaseFormat.Album };
			manager.BookProgrammeCampaign(legacyShare, artist, label, paid);
			Check(manager.ProgrammePushFactor(new RecordRuntimeData { baseRecord = legacyShare }) == .5f &&
				manager.ProgrammeState.Campaigns.Last().Spend == paid && reads[^1].Budget == raw && reads[^1].ProposedBudget == expected,
				"the campaign-share experiment holds spend fixed and logs the legacy denominator");
			Check(Read() == expected, "the campaign-share experiment keeps the standing envelope for other reads");

			// A real breaking record must pay for its next tranche using the new envelope.
			LabelReleaseProgramme.SetForChecks(false, false); RecordCampaignSpend.SetForChecks(true);
			var liveLabel = manager.GetAllLabels().First(l => l.IsActive && !l.isPlayerOwned && manager.GetLabelActiveRecordIdsForChecks(l.labelId)
				.Select(id => ChartManager.Instance.GetRecordRuntimeData(id)).Any(r => r?.baseRecord.format == ReleaseFormat.Single &&
					ArtistManager.Instance.GetArtist(r.baseRecord.artistId) is { isPlayerOwned: false, lifecycleStatus: ArtistLifecycleStatus.Active } a && a.labelId == l.labelId));
			var hot = manager.GetLabelActiveRecordIdsForChecks(liveLabel.labelId).Select(id => ChartManager.Instance.GetRecordRuntimeData(id))
				.First(r => r?.baseRecord.format == ReleaseFormat.Single && ArtistManager.Instance.GetArtist(r.baseRecord.artistId)?.labelId == liveLabel.labelId);
			var liveArtist = ArtistManager.Instance.GetArtist(hot.baseRecord.artistId);
			var savedCareer = liveArtist.careerState; float cash = liveLabel.cashReserves, expenses = liveLabel.monthlyExpenses;
			var records = ChartManager.Instance.GetAllRecords().Select(r => (r, r.campaignPushFactor, r.campaignTranches, r.sunkMarketingCost,
				r.weeksSinceRelease, r.currentPosition, r.lastWeekPosition, r.peakPosition)).ToList();
			try {
				foreach (var record in records) record.r.campaignPushFactor = null;
				liveArtist.careerState = CareerState.NewSigning; shadow.Snapshots[liveArtist.artistId] = snapshot;
				var projectArtist = CompetitorManager.CreateProjectDecisionArtistForProbe(liveArtist, liveArtist.primaryGenre);
				Check(liveLabel.GetReleaseMarketingBudget(projectArtist, "project-clone") == liveLabel.MarketingBudgetForCareer(CareerState.Star),
					"forecast project copies use canonical ownership and the same snapshot as commitment");
				hot.weeksSinceRelease = 3; hot.currentPosition = 25; hot.lastWeekPosition = 40; hot.peakPosition = 25;
				hot.campaignPushFactor = 1; hot.campaignTranches = 0; hot.sunkMarketingCost = 100;
				liveLabel.cashReserves = 1e6f; float start = liveLabel.cashReserves;
				float spend = liveLabel.MarketingBudgetForCareer(CareerState.Star) * RecordCampaignSpend.TrancheShare;
				manager.ProcessRecordCampaignsForChecks();
				Check(Math.Abs(liveLabel.cashReserves - (start - spend)) < .1f && hot.campaignTranches == 1 &&
					Math.Abs(hot.sunkMarketingCost.Value - (100 + spend)) < .01f, "actual follow-up pays and attributes the stature-based tranche");
				hot.campaignTranches = 0;
				liveLabel.cashReserves = liveLabel.GetMonthlyOverhead() * RecordCampaignSpend.ReserveMonths + (float)LabelCorporateModel.TaxAndServiceReserve(liveLabel);
				start = liveLabel.cashReserves; manager.ProcessRecordCampaignsForChecks();
				Check(liveLabel.cashReserves == start && hot.campaignTranches == 0, "follow-up spending cannot consume the cash reserve");
			} finally {
				liveArtist.careerState = savedCareer; liveLabel.cashReserves = cash; liveLabel.monthlyExpenses = expenses;
				foreach (var record in records) {
					record.r.campaignPushFactor = record.campaignPushFactor; record.r.campaignTranches = record.campaignTranches;
					record.r.sunkMarketingCost = record.sunkMarketingCost; record.r.weeksSinceRelease = record.weeksSinceRelease;
					record.r.currentPosition = record.currentPosition; record.r.lastWeekPosition = record.lastWeekPosition; record.r.peakPosition = record.peakPosition;
				}
			}
			ArtistStature.MarketingBlend = .5f;
			var saved = new WorldSaveData(); MediaAtmosphereService.CaptureWorld(saved);
			string json = JsonSerializer.Serialize(saved.Media, SaveGameService.TestJsonOptions);
			ArtistStature.Configure(Array.Empty<string>());
			MediaAtmosphereService.RehydrateWorld(new WorldSaveData { Media = JsonSerializer.Deserialize<MediaSaveData>(json, SaveGameService.TestJsonOptions) });
			Check(ArtistStature.MarketingReader && ArtistStature.LegacyCampaignShare && ArtistStature.MarketingBlend == .5f &&
				!ArtistStature.ProgrammeAlbumReader && !ArtistStature.CareerDisplay && Read() == raw + .5f * (expected - raw),
				"marketing-only saves restore the reader, calibration and requested amount");
			MediaAtmosphereService.RehydrateWorld(new WorldSaveData());
			Check(!ArtistStature.MarketingReader && !ArtistStature.LegacyCampaignShare && StatureObservation.ShadowConfig == null,
				"loading an older slot clears the previous slot's marketing opt-ins");
			GD.Print($"STATURE_MARKETING_CHECK passed checks={checks}");
		} finally {
			ArtistStature.OnMarketingRead -= Observe;
			manager.SetProgrammeForChecks(programme); LabelReleaseProgramme.SetForChecks(allocation, capacity);
			RecordCampaignSpend.SetForChecks(campaigns); SingleCapitalRisk.SetForChecks(riskFraction, riskPolicy);
			MediaAtmosphereService.RehydrateWorld(original); StatureObservation.ShadowConfig = config;
			ArtistStature.ProgrammeAlbumReader = reader; ArtistStature.CareerDisplay = display; ArtistStature.MarketingReader = marketing;
			ArtistStature.MarketingBlend = originalBlend;
			ArtistStature.LegacyCampaignShare = legacyCampaignShare;
			MediaAtmosphereService.PersistExperimentalStature = persist; MediaAtmosphere.SetForProbe(media); MediaFollowing.SetForChecks(following);
		}
	}
}
