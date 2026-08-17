using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The player's side of the desk: a thin, deliberately barebones vertical slice of the
/// label loop -- scout, sign, write, cut, release -- driven entirely by the player
/// spending hours out of the working day.
///
/// The player's label is a real <see cref="AILabel"/> registered with ChartManager and
/// CompetitorManager, so it charts, earns, and pays overhead on exactly the same rails as
/// every AI label. What it does NOT do is decide for itself: the AI's weekly release roll,
/// daily talent market, monthly roster review, and independent-distribution pursuit all
/// skip player-owned labels, and every one of those decisions is instead an action here.
/// </summary>
public partial class PlayerDesk : Node {
	public static PlayerDesk Instance { get; private set; }

	// Hour costs. These lean on ActionCosts so the desk stays on the same clock as the
	// rest of the game rather than inventing a second economy of time.
	public const int ScoutHours = ActionCosts.StandardMeeting;      // 4 -- an afternoon working the clubs
	public const int SignHours = ActionCosts.LongMeeting;           // 6 -- contract negotiation
	public const int WriteHours = ActionCosts.Songwriting;          // 4 -- a writing session
	public const int SessionHours = ActionCosts.StudioSession;      // 8 -- a full day tracking
	public const int DistributionHours = ActionCosts.RegionalTravel;// 4 -- travel and pitch a house
	public const int ScheduleHours = ActionCosts.Planning;          // 2 -- booking the release

	private const float FoundingCapital = 9000f;
	private const int PlayerRosterCapacity = 6;
	private const int SlateSize = 4;

	/// <summary>One act the player looked at on a scouting trip, as the player sees them.</summary>
	public sealed class Prospect {
		public SimulatedArtist Artist;
		/// <summary>The player's read on the act, not its true quality. Better scouting narrows the gap.</summary>
		public float ReadQuality;
		public float AskingAdvance;
		public string Note;
	}

	/// <summary>An unrecorded song sitting in the writers' book.</summary>
	public sealed class Song {
		public string SongId;
		public string Title;
		public string ArtistId;
		public Genre Genre;
		public float Hook, Originality, Danceability;
		public GameDate Written;
		public bool Recorded;
	}

	/// <summary>A finished master, paid for and sitting on the shelf until it is scheduled.</summary>
	public sealed class Master {
		public Record Record;
		public string ArtistId;
		public string SongTitle;
		public float ProductionCost;
		public GameDate Cut;
		public bool Scheduled;
		public bool Released;
	}

	public sealed class PlannedRelease {
		public Master Master;
		public GameDate Date;
		public float MarketingBudget;
	}

	/// <summary>
	/// One week's books, snapshotted after the settlement is booked. Earned and banked
	/// are deliberately separate figures: money earned in a market served by a wholesale
	/// house is billed now and paid on that house's terms months later, so a good week on
	/// the chart and a good week at the bank are not the same week.
	/// </summary>
	public sealed class WeekBooks {
		public int Week;
		public GameDate Date;
		public long Units;
		public float Gross, ManufacturingCost, DistributionSkim, ArtistRoyalty, Earned;
		public float Deferred, Collected, Banked;
		public float Outstanding, Cash;
	}

	public AILabel Label { get; private set; }
	public bool HasLabel => Label != null;

	public IReadOnlyList<Prospect> Slate => slate;
	public GameDate SlateDate { get; private set; }
	public IReadOnlyList<Song> Songs => songs;
	public IReadOnlyList<Master> Masters => masters;
	public IReadOnlyList<PlannedRelease> Planned => planned;
	public IReadOnlyList<string> Log => log;
	/// <summary>Most recent week first.</summary>
	public IReadOnlyList<WeekBooks> Books => books;

	private readonly List<Prospect> slate = new();
	private readonly List<Song> songs = new();
	private readonly List<Master> masters = new();
	private readonly List<PlannedRelease> planned = new();
	private readonly List<string> log = new();
	private readonly List<WeekBooks> books = new();
	private float lastSnapshotCash;
	private int counter;

	/// <summary>Raised whenever anything the desk UI displays has changed.</summary>
	public event Action Changed;

	public override void _EnterTree() {
		if (Instance != null && Instance != this) { QueueFree(); return; }
		Instance = this;
	}

	public override void _Ready() {
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnDayStarted += OnDayStarted;
			TimeManager.Instance.OnHourChanged += OnHourChanged;
			// PlayerDesk is the last autoload, so this handler runs after ChartManager's --
			// which is where the week is settled and the revenue booked. The books are read
			// after that, never during it.
			TimeManager.Instance.OnWeekEnded += OnWeekEnded;
		}
	}

	public override void _ExitTree() {
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnDayStarted -= OnDayStarted;
			TimeManager.Instance.OnHourChanged -= OnHourChanged;
			TimeManager.Instance.OnWeekEnded -= OnWeekEnded;
		}
		if (Instance == this) Instance = null;
	}

	private void OnHourChanged(int hour) => Changed?.Invoke();

	// ========================================================================
	// FOUNDING
	// ========================================================================

	public bool FoundLabel(string labelName, string regionId, out string message) {
		if (Label != null) { message = "You already run a label."; return false; }
		MarketRegion region = ChartManager.Instance?.GetRegionById(regionId);
		if (region == null) { message = "Pick a home market first."; return false; }

		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		var label = new AILabel {
			labelId = "player_label",
			labelName = string.IsNullOrWhiteSpace(labelName) ? "Player Records" : labelName.Trim(),
			founderName = "You",
			headquartersCity = region.majorCities?.FirstOrDefault() ?? region.regionName,
			archetype = LabelArchetype.RegionalHustler,
			tier = LabelTier.Small,
			foundedYear = year,
			isHistorical = false,
			isPlayerOwned = true,
			status = LabelStatus.Stable,
			homeRegion = region.regionId,
			// One market, served out of the back of a car. Everything past this is earned.
			strongRegions = new[] { region.regionId },
			distributionRegions = new[] { region.regionId },
			cashReserves = FoundingCapital,
			maxRosterSize = PlayerRosterCapacity,
			nationalReach = 0.02f,
			budgetLevel = 0.15f,
			scoutingAbility = 0.5f,
			productionQuality = 0.4f,
			marketingPower = 0.35f,
			riskTolerance = 0.5f,
			artistLoyalty = 0.6f,
			payolaWillingness = 0.05f,
			releasesPerMonth = 0.5f,
			populationOrigin = LabelPopulationOrigin.RuntimeFounded,
			runtimeBirthWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0,
			runtimeBirthYear = year
		};
		label.distributionStrength = 0.05f;
		label.preferredGenres = TopRegionalGenres(region, 2);
		label.secondaryGenres = Array.Empty<Genre>();
		label.roster = new List<SimulatedArtist>();
		label.SetOperatingRosterTarget(1, LabelOperatingTargetReason.RuntimeBootstrap, label.runtimeBirthWeek);
		DistanceModel.AssignHomeCity(label);

		ChartManager.Instance?.RegisterLabel(label);
		CompetitorManager.Instance?.RegisterLabel(label);
		Label = label;

		Note($"{label.labelName} opens for business in {region.regionName} with ${FoundingCapital:N0}.");
		message = $"{label.labelName} is open.";
		Changed?.Invoke();
		return true;
	}

	private static Genre[] TopRegionalGenres(MarketRegion region, int count) =>
		(region.genrePreferences ?? Array.Empty<GenrePreference>())
			.OrderByDescending(preference => preference.affinity)
			.Take(count)
			.Select(preference => preference.genre)
			.DefaultIfEmpty(Genre.RockAndRoll)
			.ToArray();

	// ========================================================================
	// SCOUTING
	// ========================================================================

	/// <summary>
	/// An afternoon working the player's own market. Returns the acts they got a look at:
	/// unsigned, local, and read through the label's own ear rather than perfectly.
	/// </summary>
	public bool ScoutLocally(out string message) {
		if (!Require(ScoutHours, out message)) return false;
		MarketRegion region = ChartManager.Instance?.GetRegionById(Label.homeRegion);
		if (region == null) { message = "No home market resolved."; return false; }

		List<SimulatedArtist> local = (ArtistManager.Instance?.GetUnsignedArtists() ?? new List<SimulatedArtist>())
			.Where(artist => IsLocal(artist, region) && IsWorthAPopLabelsEvening(artist))
			.ToList();
		if (local.Count == 0) {
			Spend(ScoutHours);
			slate.Clear();
			Note($"A wasted afternoon -- nothing unsigned worth hearing in {region.regionName}.");
			message = "Nobody worth hearing tonight.";
			Changed?.Invoke();
			return true;
		}

		Spend(ScoutHours);
		List<SimulatedArtist> heard = SampleTheRoom(local, SlateSize);

		slate.Clear();
		foreach (SimulatedArtist artist in heard) {
			float truth = artist.CalculateBaseQuality();
			float noise = (1f - Mathf.Clamp(Label.scoutingAbility, 0f, 1f)) * 0.25f;
			slate.Add(new Prospect {
				Artist = artist,
				ReadQuality = Mathf.Clamp(truth + (float)GD.RandRange(-noise, noise), 0f, 1f),
				AskingAdvance = Label.CalculateAdvanceOffer(artist),
				Note = DescribeProspect(artist)
			});
		}
		SlateDate = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		Note($"Scouted {region.regionName}: heard {slate.Count} unsigned act(s).");
		message = $"Heard {slate.Count} acts.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>
	/// A few acts out of everyone playing the market this week, weighted toward the better ones.
	/// <para>
	/// This was <c>OrderByDescending(quality * rand(0.55, 1.45)).Take(4)</c>, which is a top-of-tail
	/// draw over a pool that does not change between trips — so the player met the same four acts
	/// every week, and which four was decided by whoever happened to sit at the region's quality
	/// maximum. Measured in Great Lakes: the pool is 804 acts led by Country 92, RockAndRoll 73 and
	/// RnB 69, but the highest top quality in the region belongs to Classical at n=34, and small
	/// genres win that slot on sampling noise (Classical mean .58 and LatinPop mean .59 against
	/// Country's .51). That is the whole of the reported "every act is easy listening or classical":
	/// not a supply problem, a selection problem.
	/// </para>
	/// <para>
	/// Weighted sampling without replacement fixes both halves. Quality still dominates — the
	/// exponent makes a 0.8 act roughly four times as likely to be heard as a 0.5 one — but the
	/// draw is over the whole room, so the slate changes week to week and reads like a scene
	/// rather than a leaderboard.
	/// </para>
	/// </summary>
	private static List<SimulatedArtist> SampleTheRoom(List<SimulatedArtist> pool, int count) {
		var remaining = new List<SimulatedArtist>(pool);
		var weights = remaining.Select(WeightOf).ToList();
		var heard = new List<SimulatedArtist>(count);
		while (heard.Count < count && remaining.Count > 0) {
			float total = weights.Sum();
			// Every act in the room is a floor away from zero, so a degenerate pool still draws.
			float target = total > 0f ? (float)GD.RandRange(0f, total) : 0f;
			int index = remaining.Count - 1;
			for (int i = 0; i < remaining.Count; i++) {
				target -= weights[i];
				if (target > 0f) continue;
				index = i;
				break;
			}
			heard.Add(remaining[index]);
			remaining.RemoveAt(index);
			weights.RemoveAt(index);
		}
		return heard;
	}

	/// <summary>Talent tells, but the room is not sorted by it.</summary>
	private static float WeightOf(SimulatedArtist artist) {
		float quality = Mathf.Clamp(artist.CalculateBaseQuality(), 0f, 1f);
		return 0.02f + quality * quality * quality;
	}

	/// <summary>
	/// A pop label in 1960 does not sign a string quartet, a comedian or a children's record.
	/// <para>
	/// Same odd-entity families <see cref="AlbumLegitimacyService.IsEligibleFamily"/> already
	/// excludes from the album-as-art movement, and for the same reason: they sell records and
	/// are occasionally culturally large, but they are not the business this desk is in. Without
	/// this the classical acts are simply never signed by anybody, so they accumulate in the
	/// unsigned pool and turn up on the player's slate week after week.
	/// </para>
	/// </summary>
	private static bool IsWorthAPopLabelsEvening(SimulatedArtist artist) =>
		AlbumLegitimacyService.IsEligibleFamily(
			GenreCatalog.Get(GenreCatalog.MapLegacy(artist.primaryGenre,
				TimeManager.Instance?.CurrentDate.year ?? 1960)).Family);

	private static bool IsLocal(SimulatedArtist artist, MarketRegion region) =>
		artist != null && Normalize(artist.homeRegion) == Normalize(region.regionName);

	private static string Normalize(string value) =>
		new((value ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

	private static string DescribeProspect(SimulatedArtist artist) {
		string writing = artist.songwritingAbility > 0.6f ? "writes their own"
			: artist.songwritingAbility > 0.35f ? "some original material" : "covers band";
		string stage = artist.livePerformance > 0.6f ? "kills live" : "stiff on stage";
		return $"{artist.members.Count(member => member.isActive)}-piece, {writing}, {stage}";
	}

	// ========================================================================
	// SIGNING
	// ========================================================================

	public bool SignProspect(Prospect prospect, out string message) {
		if (prospect?.Artist == null) { message = "No act selected."; return false; }
		if (!Require(SignHours, out message)) return false;
		if (!Label.HasRosterSpace) { message = "Roster is full."; return false; }
		if (!string.IsNullOrEmpty(prospect.Artist.labelId)) { message = "Somebody signed them first."; return false; }

		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		if (ArtistManager.Instance?.IsEligibleForPopulationSigning(prospect.Artist, week) == false) {
			message = "They're not taking offers right now.";
			return false;
		}
		float advance = Label.CalculateAdvanceOffer(prospect.Artist);
		if (!Label.CanAffordToSign(advance)) {
			message = $"You can't cover a ${advance:N0} advance.";
			return false;
		}

		Spend(SignHours);
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		float paid = Label.SignArtist(prospect.Artist, year);
		CompetitorManager.Instance?.RecordExpense(Label, paid);
		ArtistManager.Instance?.SignArtist(prospect.Artist, Label.labelId, year);
		// Capacity grows with the roster so the label is never sitting under its own target.
		Label.SetOperatingRosterTarget(Label.CurrentRosterSize, LabelOperatingTargetReason.OrganicGrowth, week);
		slate.Remove(prospect);

		Note($"Signed {prospect.Artist.stageName} -- ${paid:N0} advance, {prospect.Artist.royaltyRate:P0} royalty, " +
			$"{prospect.Artist.contractLength}yr.");
		message = $"Signed {prospect.Artist.stageName}.";
		Changed?.Invoke();
		return true;
	}

	// ========================================================================
	// SONGWRITING
	// ========================================================================

	/// <summary>A writing session with one act. Their own writing ability sets the ceiling.</summary>
	public bool WriteSongs(SimulatedArtist artist, out string message) {
		if (artist == null) { message = "No act selected."; return false; }
		if (!Require(WriteHours, out message)) return false;

		Spend(WriteHours);
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		float creativity = artist.members.Count > 0 ? artist.members.Max(member => member.creativity) : 0.4f;

		int written = GD.Randf() < 0.35f ? 2 : 1;
		var titles = new List<string>();
		for (int index = 0; index < written; index++) {
			var song = new Song {
				SongId = $"song_{++counter}",
				ArtistId = artist.artistId,
				Genre = artist.primaryGenre,
				Written = today,
				Hook = Mathf.Clamp(artist.songwritingAbility * 0.7f + (float)GD.RandRange(-0.18, 0.28), 0f, 1f),
				Originality = Mathf.Clamp(creativity * 0.7f + (float)GD.RandRange(0f, 0.3f), 0f, 1f),
				Danceability = (float)GD.RandRange(0.3, 0.95)
			};
			song.Title = NameGenerator.Instance?.GenerateSongTitle(song.Genre, year, artist.artistId)
				?? $"Untitled {counter}";
			songs.Add(song);
			titles.Add(song.Title);
		}

		Note($"{artist.stageName} wrote {string.Join(" / ", titles)}.");
		message = $"Wrote {written} song(s).";
		Changed?.Invoke();
		return true;
	}

	// ========================================================================
	// THE SESSION
	// ========================================================================

	/// <summary>
	/// Books a day of studio time and cuts one song as a master. The player pays the
	/// label's production cost here, so the release itself is free -- the money has
	/// already gone into the tape.
	/// </summary>
	public bool BookSession(Song song, float budgetMultiplier, out string message) {
		if (song == null || song.Recorded) { message = "No song selected."; return false; }
		if (!Require(SessionHours, out message)) return false;
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(song.ArtistId);
		if (artist == null || artist.labelId != Label.labelId) { message = "That act isn't on your roster."; return false; }

		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		budgetMultiplier = Mathf.Clamp(budgetMultiplier, 0.5f, 2f);

		var record = new Record {
			recordId = $"player_{++counter}",
			labelId = Label.labelId,
			title = song.Title,
			artistId = artist.artistId,
			artistName = artist.stageName,
			format = ReleaseFormat.Single,
			isPlayerOwned = true,
			primaryGenre = song.Genre,
			secondaryGenre = artist.secondaryGenre,
			originality = song.Originality,
			danceability = song.Danceability,
			controversy = (float)GD.RandRange(0f, 0.2f)
		};

		float cost = (CompetitorManager.Instance?.GetPlayerProductionCost(Label, record, today)
			?? Label.GetProductionCost()) * budgetMultiplier;
		if (Label.cashReserves - cost < Label.GetMonthlyOverhead()) {
			message = $"A ${cost:N0} session would leave you short of next month's overhead.";
			return false;
		}

		Spend(SessionHours);
		MarketRegion region = ChartManager.Instance?.GetRegionById(Label.homeRegion);
		float studioMod = region != null ? ChartSimulator.GetStudioQualityModifier(region) : 1f;
		// What the room and the money buy is the recording, not the song: the hook is
		// mostly what was written, the production is mostly what was spent.
		record.hookStrength = Mathf.Clamp(song.Hook * 0.8f + studioMod * 0.1f + (float)GD.RandRange(-0.06, 0.10), 0f, 1f);
		record.productionQuality = Mathf.Clamp(
			Label.productionQuality * 0.3f + artist.studioPerformance * 0.25f +
			studioMod * 0.15f + (budgetMultiplier - 0.5f) / 1.5f * 0.3f + (float)GD.RandRange(-0.05, 0.05), 0f, 1f);

		Label.cashReserves -= cost;
		Label.monthlyExpenses += cost;
		artist.unrecoupedAdvance += cost;
		song.Recorded = true;
		masters.Add(new Master {
			Record = record, ArtistId = artist.artistId, SongTitle = song.Title,
			ProductionCost = cost, Cut = today
		});

		Note($"Cut \"{song.Title}\" with {artist.stageName} for ${cost:N0} " +
			$"(hook {record.hookStrength:F2}, production {record.productionQuality:F2}).");
		message = $"\"{song.Title}\" is in the can.";
		Changed?.Invoke();
		return true;
	}

	// ========================================================================
	// DISTRIBUTION
	// ========================================================================

	public IReadOnlyList<MarketRegion> GetPlaceableMarkets() {
		if (Label == null || CompetitorManager.Instance == null || ChartManager.Instance == null)
			return Array.Empty<MarketRegion>();
		return ChartManager.Instance.GetAllRegions()
			.Where(region => !Label.HasDistributionInRegion(region.regionId) &&
				CompetitorManager.Instance.GetIndependentDistributorsInRegion(region.regionId)
					.Any(house => house.HasCapacity && !house.CarriesLabel(Label.labelId)))
			.ToList();
	}

	/// <summary>
	/// Places the label's line with a wholesale house in one market. This is what makes a
	/// record physically available outside the player's home town.
	/// </summary>
	public bool PlaceLine(string regionId, out string message) {
		if (!Require(DistributionHours, out message)) return false;
		IndependentDistributor house = CompetitorManager.Instance?.PlacePlayerLine(Label, regionId);
		if (house == null) { message = "No house in that market would take the line."; return false; }

		Spend(DistributionHours);
		string regionName = ChartManager.Instance?.GetRegionById(regionId)?.regionName ?? regionId;
		Note($"{house.distributorName} takes the line in {regionName} " +
			$"({house.paymentTermWeeks}-week terms, {house.reportingHonesty:P0} reporting).");
		message = $"{house.distributorName} is carrying you in {regionName}.";
		Changed?.Invoke();
		return true;
	}

	// ========================================================================
	// RELEASE
	// ========================================================================

	public bool ScheduleRelease(Master master, int daysOut, float marketingBudget, out string message) {
		if (master == null || master.Scheduled || master.Released) { message = "No master selected."; return false; }
		if (!Require(ScheduleHours, out message)) return false;
		marketingBudget = Mathf.Max(0f, marketingBudget);
		if (marketingBudget > Label.cashReserves - Label.GetMonthlyOverhead()) {
			message = "You can't cover that campaign and next month's overhead.";
			return false;
		}

		Spend(ScheduleHours);
		GameDate date = (TimeManager.Instance?.CurrentDate ?? GameDate.StartDate).AddDays(Mathf.Max(1, daysOut));
		master.Scheduled = true;
		planned.Add(new PlannedRelease { Master = master, Date = date, MarketingBudget = marketingBudget });

		Note($"\"{master.SongTitle}\" scheduled for {date.ToHeadlineString()} " +
			$"with a ${marketingBudget:N0} campaign.");
		message = $"Shipping {date.ToHeadlineString()}.";
		Changed?.Invoke();
		return true;
	}

	private void OnDayStarted(GameDate date) {
		if (Label == null || planned.Count == 0) return;
		foreach (PlannedRelease release in planned.Where(entry => entry.Date <= date).ToList()) {
			planned.Remove(release);
			FireRelease(release, date);
		}
		Changed?.Invoke();
	}

	private void FireRelease(PlannedRelease release, GameDate date) {
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(release.Master.ArtistId);
		if (artist == null) { Note($"\"{release.Master.SongTitle}\" never shipped -- the act is gone."); return; }

		float budget = Mathf.Min(release.MarketingBudget, Mathf.Max(0f, Label.cashReserves));
		Label.cashReserves -= budget;
		Label.monthlyExpenses += budget;

		bool released = CompetitorManager.Instance?.ReleasePlayerRecord(
			Label, artist, release.Master.Record, budget, release.Master.ProductionCost, date) ?? false;
		if (!released) { Note($"\"{release.Master.SongTitle}\" failed to ship."); return; }

		release.Master.Released = true;
		masters.Remove(release.Master);
		Note($"RELEASED: \"{release.Master.SongTitle}\" by {artist.stageName} ({date.ToHeadlineString()}).");
	}

	// ========================================================================
	// SHARED
	// ========================================================================

	private bool Require(int hours, out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (TimeManager.Instance == null) { message = "No clock."; return false; }
		if (!TimeManager.Instance.CanAffordHours(hours, allowOvertime: true)) {
			message = $"Not enough hours left today (needs {hours}h).";
			return false;
		}
		message = string.Empty;
		return true;
	}

	private void Spend(int hours) => TimeManager.Instance?.SpendHours(hours, allowOvertime: true);

	private void Note(string entry) {
		GameDate date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		log.Insert(0, $"{date.ToShortString()}  {entry}");
		if (log.Count > 60) log.RemoveAt(log.Count - 1);
		GD.Print($"[Desk] {entry}");
	}

	// ========================================================================
	// THE BOOKS
	// ========================================================================

	private void OnWeekEnded(GameDate date) {
		if (Label == null) return;
		long units = ReleasedRecords.Sum(record => (long)record.unitsThisWeek);
		float cash = Label.cashReserves;
		books.Insert(0, new WeekBooks {
			Week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0,
			Date = date,
			Units = units,
			Gross = Label.weeklyGrossRevenue,
			ManufacturingCost = Label.weeklyCogs,
			DistributionSkim = Label.weeklyDistributionSkim,
			ArtistRoyalty = Label.weeklyArtistRoyalty,
			Earned = Label.weeklyNetRevenue,
			Deferred = Label.weeklyWholesaleDeferred,
			Collected = Label.weeklyWholesaleCollected,
			// What the records earned this week, less what went out on credit, plus what
			// old invoices finally paid. This is the figure that moved the bank balance.
			Banked = Label.weeklyNetRevenue - Label.weeklyWholesaleDeferred + Label.weeklyWholesaleCollected,
			Outstanding = Label.outstandingWholesaleReceivables,
			Cash = cash
		});
		if (books.Count > 120) books.RemoveAt(books.Count - 1);
		lastSnapshotCash = cash;
		Changed?.Invoke();
	}

	/// <summary>The receivables book: what each house owes and when its terms run out.</summary>
	public IEnumerable<(string HouseName, string RegionName, float Amount, int WeeksAway)> OutstandingInvoices() {
		if (Label == null) yield break;
		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		IReadOnlyList<IndependentDistributor> houses =
			CompetitorManager.Instance?.GetIndependentDistributors() ?? Array.Empty<IndependentDistributor>();
		foreach (WholesaleReceivable receivable in Label.wholesaleReceivables.OrderBy(entry => entry.DueWeek)) {
			IndependentDistributor house = houses.FirstOrDefault(candidate => candidate.distributorId == receivable.DistributorId);
			yield return (
				house?.distributorName ?? "a house",
				ChartManager.Instance?.GetRegionById(house?.regionId)?.regionName ?? "?",
				receivable.Amount,
				Mathf.Max(0, receivable.DueWeek - currentWeek));
		}
	}

	public IEnumerable<SimulatedArtist> Roster => Label?.roster ?? new List<SimulatedArtist>();
	public IEnumerable<Song> UnrecordedSongs => songs.Where(song => !song.Recorded);
	public IEnumerable<RecordRuntimeData> ReleasedRecords =>
		Label == null ? Enumerable.Empty<RecordRuntimeData>()
			: (ChartManager.Instance?.GetAllRecords() ?? new List<RecordRuntimeData>())
				.Where(record => record.baseRecord?.labelId == Label.labelId);
}
