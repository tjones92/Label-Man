using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Opt-in game reader of completed-week audience standing. Career and contract state stay independent.</summary>
public static class ArtistStature {
	public const string CalibrationSwitch = "--stature-calibration=";
	public const string DisplaySwitch = "--stature-career-display";
	public const string MarketingSwitch = "--stature-marketing-reader";
	public const string MarketingBlendSwitch = "--stature-marketing-blend=";
	public const string LegacyCampaignShareSwitch = "--stature-marketing-legacy-campaign-share";
	internal const string CampaignShareContext = "programme-campaign-share";
	public static float MarketingBlend { get; internal set; } = 1f;
	private static float launchMarketingBlend = 1f;
	public static bool LegacyCampaignShare { get; internal set; }
	private static bool launchLegacyCampaignShare;
	public static bool ProgrammeAlbumReader { get; internal set; }
	public static bool CareerDisplay { get; internal set; }
	public static bool MarketingReader { get; internal set; }
	private static StatureWeeklyShadow.Config launchCalibration;
	private static bool launchReader, launchDisplay, launchMarketing, launchPersistence, restoredReader;

	// Called by ordinary game startup as well as headless scenes. CSV writers are optional.
	public static void Configure(IEnumerable<string> arguments) {
		var args = (arguments ?? Array.Empty<string>()).ToArray();
		var calibrations = args.Where(a => a.StartsWith(CalibrationSwitch, StringComparison.Ordinal) ||
			a.StartsWith(StatureObservation.ShadowSwitch, StringComparison.Ordinal)).ToArray();
		bool reader = args.Contains(StatureObservation.ProgrammeReaderSwitch);
		bool display = args.Contains(DisplaySwitch);
		bool marketing = args.Contains(MarketingSwitch);
		bool legacyCampaignShare = args.Contains(LegacyCampaignShareSwitch);
		if (legacyCampaignShare && !marketing)
			throw new ArgumentException("Legacy campaign share requires the stature marketing reader.");
		var blends = args.Where(a => a.StartsWith(MarketingBlendSwitch, StringComparison.Ordinal)).ToArray();
		float blend = 1f;
		if (blends.Length > 1 || blends.Length == 1 && (!marketing ||
			!float.TryParse(blends[0][MarketingBlendSwitch.Length..], System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out blend) || !float.IsFinite(blend) || blend < 0 || blend > 1))
			throw new ArgumentException("Marketing blend requires the marketing reader and a finite weight from zero to one.");
		launchMarketingBlend = MarketingBlend = blend;
		launchLegacyCampaignShare = LegacyCampaignShare = legacyCampaignShare;
		bool persist = reader || display || marketing || args.Contains("--persist-stature-shadow");
		if (calibrations.Length > 1) throw new ArgumentException("Specify one stature calibration.");
		if (calibrations.Length == 0) {
			if (persist) throw new ArgumentException("The stature reader and persistence require an explicit calibration.");
			launchCalibration = StatureObservation.ShadowConfig = null;
			launchReader = ProgrammeAlbumReader = false;
			launchDisplay = CareerDisplay = false;
			launchMarketing = MarketingReader = false;
			launchPersistence = MediaAtmosphereService.PersistExperimentalStature = false;
			return;
		}
		if (!MediaAtmosphere.Observing || !MediaFollowing.Active)
			throw new ArgumentException("Stature requires active media atmosphere and following.");
		string path = calibrations[0][(calibrations[0].StartsWith(CalibrationSwitch, StringComparison.Ordinal)
			? CalibrationSwitch.Length : StatureObservation.ShadowSwitch.Length)..];
		StatureObservation.ConfigureShadow(path, persist);
		ProgrammeAlbumReader = reader;
		CareerDisplay = display;
		MarketingReader = marketing;
		launchCalibration = StatureObservation.ShadowConfig;
		launchReader = reader;
		launchDisplay = display;
		launchMarketing = marketing;
		launchPersistence = persist;
	}

	internal static void RestoreReader(StatureReaderSaveData saved) {
		if (saved != null) {
			var calibration = saved.Calibration ?? throw new InvalidOperationException("Saved stature reader is missing its calibration.");
			StatureShadowPolicy.Validate(calibration.Policy);
			StatureObservation.ShadowConfig = calibration;
			ProgrammeAlbumReader = saved.ProgrammeAlbumReader;
			CareerDisplay = saved.CareerDisplay;
			MarketingReader = saved.MarketingReader;
			if (!float.IsFinite(saved.MarketingBlend) || saved.MarketingBlend < 0 || saved.MarketingBlend > 1)
				throw new InvalidOperationException("Invalid saved stature marketing blend.");
			MarketingBlend = saved.MarketingBlend;
			LegacyCampaignShare = saved.LegacyCampaignShare;
			MediaAtmosphereService.PersistExperimentalStature = true;
		} else if (restoredReader) {
			// Loading another slot must not inherit the previous slot's opt-in.
			StatureObservation.ShadowConfig = launchCalibration;
			ProgrammeAlbumReader = launchReader;
			CareerDisplay = launchDisplay;
			MarketingReader = launchMarketing;
			MarketingBlend = launchMarketingBlend;
			LegacyCampaignShare = launchLegacyCampaignShare;
			MediaAtmosphereService.PersistExperimentalStature = launchPersistence;
		}
		restoredReader = saved != null;
	}

	internal static CareerState? SelectProgrammeAlbumCareer(bool enabled, StatureWeeklyShadow.Snapshot snapshot, DateTime date,
		CareerState current, bool playerOwned, ArtistLifecycleStatus lifecycle) {
		if (!enabled) return null;
		var read = StatureDecisionSnapshot.Resolve(snapshot, date, current, playerOwned, lifecycle);
		return read.Status == StatureDecisionSnapshot.Status.Available ? read.CareerState : null;
	}

	internal static CareerState? ProgrammeAlbumCareer(AILabel label, SimulatedArtist artist, GameDate date) {
		if (!ProgrammeAlbumReader || label == null || artist == null || StatureObservation.ShadowConfig == null) return null;
		var snapshot = MediaAtmosphereService.StatureSnapshotFor(artist.artistId);
		return SelectProgrammeAlbumCareer(true, snapshot, new DateTime(date.year, date.month, date.day), artist.careerState,
			label.isPlayerOwned || artist.isPlayerOwned, artist.lifecycleStatus);
	}

	public enum StandingStatus { Available, Missing, Future, Stale, MissingEvidence, PlayerOwned, Inactive, Terminal }
	public readonly record struct StandingRead(StandingStatus Status, StatureShadowPolicy.Band? Band, DateTime Date);
	public readonly record struct MarketingRead(GameDate DecisionDate, string LabelId, string ArtistId, string Context,
		DateTime SnapshotDate, string Status, CareerState LegacyCareer, CareerState? StandingCareer, float LegacyBudget, float Budget,
		float ProposedBudget);
	/// <summary>Requested envelope before the caller's existing quality, cash, reserve and capacity limits. No payment is made here.</summary>
	public static event Action<MarketingRead> OnMarketingRead;

	internal static float ReleaseMarketingBudget(AILabel label, SimulatedArtist artist, GameDate date, string context) {
		float legacy = label.GetMarketingBudget(artist);
		if (!MarketingReader && OnMarketingRead == null) return legacy;
		// Project-decision copies intentionally omit ownership. Resolve the real act by id for eligibility only.
		var owner = string.IsNullOrEmpty(artist.artistId) ? artist : ArtistManager.Instance?.GetArtist(artist.artistId) ?? artist;
		var snapshot = MediaAtmosphereService.StatureSnapshotFor(artist.artistId);
		var read = ResolveStanding(snapshot, new DateTime(date.year, date.month, date.day), owner.careerState,
			label.isPlayerOwned || artist.isPlayerOwned || owner.isPlayerOwned, owner.lifecycleStatus);
		string status = read.Status.ToString();
		CareerState? standing = null;
		if (read.Status == StandingStatus.Available) {
			if (owner.labelId != label.labelId || string.IsNullOrEmpty(owner.labelId)) status = "NotOwned";
			else if (owner.careerState is CareerState.Unsigned or CareerState.Dropped) status = "LifecycleUnresolved";
			else standing = CareerFromStanding(read.Band, owner.careerState);
		}
		float proposed = standing.HasValue ? label.MarketingBudgetForCareer(standing.Value) : legacy;
		// The share experiment keeps the price-to-push reference on the career table.
		bool useStanding = MarketingReader && !(LegacyCampaignShare && context == CampaignShareContext);
		float budget = useStanding ? BlendMarketingBudget(legacy, proposed, MarketingBlend) : legacy;
		OnMarketingRead?.Invoke(new(date, label.labelId, artist.artistId, context, read.Date, status,
			artist.careerState, standing, legacy, budget, proposed));
		return budget;
	}

	internal static float BlendMarketingBudget(float legacy, float standing, float weight) =>
		weight == 1f ? standing : weight == 0f || legacy == standing ? legacy : legacy + weight * (standing - legacy);

	private static CareerState CareerFromStanding(StatureShadowPolicy.Band? band, CareerState current) => band switch {
		StatureShadowPolicy.Band.Rising => CareerState.Rising,
		StatureShadowPolicy.Band.Established => CareerState.Established,
		StatureShadowPolicy.Band.Star => CareerState.Star,
		StatureShadowPolicy.Band.Superstar => CareerState.Superstar,
		StatureShadowPolicy.Band.BelowRising => current == CareerState.NewSigning ? CareerState.NewSigning : CareerState.Declining,
		_ => current
	};

	/// <summary>Audience standing is independent of signing, dropping and chart rank. Reading never advances its clock.</summary>
	public static StandingRead AudienceStanding(SimulatedArtist artist, GameDate date) {
		if (artist == null) return new(StandingStatus.Missing, null, default);
		return ResolveStanding(MediaAtmosphereService.StatureSnapshotFor(artist.artistId), new DateTime(date.year, date.month, date.day),
			artist.careerState, IsPlayerOwned(artist), artist.lifecycleStatus);
	}

	internal static StandingRead ResolveStanding(StatureWeeklyShadow.Snapshot snapshot, DateTime date, CareerState career,
		bool playerOwned, ArtistLifecycleStatus lifecycle) {
		StandingRead Unavailable(StandingStatus status) => new(status, null, snapshot?.Date ?? default);
		if (playerOwned) return Unavailable(StandingStatus.PlayerOwned);
		if (lifecycle is ArtistLifecycleStatus.Retired or ArtistLifecycleStatus.Disbanded || career is CareerState.Retired or CareerState.Disbanded)
			return Unavailable(StandingStatus.Terminal);
		if (lifecycle != ArtistLifecycleStatus.Active) return Unavailable(StandingStatus.Inactive);
		if (snapshot == null || snapshot.Date == default || date == default) return Unavailable(StandingStatus.Missing);
		int age = (date.Date - snapshot.Date.Date).Days;
		// Unlike a forecast, a public file may show the week that has just settled today.
		if (age < 0) return Unavailable(StandingStatus.Future);
		if (age > 7) return Unavailable(StandingStatus.Stale);
		if (snapshot.Status == StatureShadowPolicy.ResultStatus.MissingEvidence || !Enum.IsDefined(snapshot.Band))
			return Unavailable(StandingStatus.MissingEvidence);
		return new(StandingStatus.Available, snapshot.Band, snapshot.Date);
	}

	private static bool IsPlayerOwned(SimulatedArtist artist) => artist.isPlayerOwned ||
		(!string.IsNullOrEmpty(artist.labelId) && CompetitorManager.Instance?.GetAllLabels()
			.Any(l => l != null && l.labelId == artist.labelId && l.isPlayerOwned) == true);

	/// <summary>Presentation only. Economic and lifecycle readers continue to read the saved career state.</summary>
	public static CareerState DisplayCareer(SimulatedArtist artist, GameDate date) {
		if (artist == null) return CareerState.Unsigned;
		if (!CareerDisplay || IsPlayerOwned(artist)) return artist.careerState;
		if (artist.lifecycleStatus == ArtistLifecycleStatus.Retired) return CareerState.Retired;
		if (artist.lifecycleStatus == ArtistLifecycleStatus.Disbanded) return CareerState.Disbanded;
		// Departure remains visible even when the act retains an audience off roster.
		if (artist.careerState is CareerState.Unsigned or CareerState.Dropped or CareerState.Retired or CareerState.Disbanded)
			return artist.careerState;
		var read = AudienceStanding(artist, date);
		if (read.Status != StandingStatus.Available) return artist.careerState;
		return CareerFromStanding(read.Band, artist.careerState);
	}

	public static string ContractContext(SimulatedArtist artist) {
		if (!CareerDisplay || artist == null || IsPlayerOwned(artist) || artist.lifecycleStatus != ArtistLifecycleStatus.Active ||
			artist.careerState is CareerState.Unsigned or CareerState.Dropped or CareerState.Retired or CareerState.Disbanded) return null;
		if (string.IsNullOrEmpty(artist.labelId)) return "Unsigned";
		if (artist.IsExperiencedComebackEvaluationPending()) return "Comeback contract";
		return artist.careerState == CareerState.NewSigning ? "New signing" : "Signed";
	}
}

/// <summary>Embedded calibration makes an opted-in game's saves independent of the original calibration file.</summary>
public sealed class StatureReaderSaveData {
	public StatureWeeklyShadow.Config Calibration { get; set; }
	// Older saves contained only Calibration and implicitly enabled the programme reader.
	public bool ProgrammeAlbumReader { get; set; } = true;
	public bool CareerDisplay { get; set; }
	public bool MarketingReader { get; set; }
	public float MarketingBlend { get; set; } = 1f;
	public bool LegacyCampaignShare { get; set; }
}
