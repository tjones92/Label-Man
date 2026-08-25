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
	public const int ScoutHours = ActionCosts.LongCall;             // 3 -- an evening catching a bill
	public const int FollowUpHours = ActionCosts.QuickMeeting;      // 2 -- a second look and a talk
	public const int SignHours = ActionCosts.LongMeeting;           // 6 -- contract negotiation
	public const int WriteHours = ActionCosts.Songwriting;          // 4 -- a writing session
	public const int DistributionHours = ActionCosts.RegionalTravel;// 4 -- travel and pitch a house
	public const int ScheduleHours = ActionCosts.Planning;          // 2 -- booking the release
	public const int TeachHours = ActionCosts.QuickMeeting;         // 2 -- sitting the act down to start a cover
	public const int CommissionHours = ActionCosts.QuickMeeting;    // 2 -- putting the brief to a writer
	public const int CommissionDeliveryDays = 7;                    // a writer turns a commission around in about a week
	public const float CommissionFee = 150f;                        // the writer's / publisher's fee, paid on commission
	// A cover is now worked up over several days, not learned on the spot: teaching costs a short setup at the
	// desk (TeachHours) and then the act rehearses on its own for this many days -- fewer the more capable the
	// act, scaled by musicianship/cohesion/studio craft in EstimateCoverLearnDays.
	public const int MinCoverLearnDays = 3;
	public const int MaxCoverLearnDays = 14;

	// Working a town out of the trunk is, first, the drive to get there -- one-way hours off REAL road
	// miles at a period highway average. A far adjacent town can outrun a single day; that's the seam the
	// on-the-road model fills. Gas is charged round-trip.
	private const float DriveMph = 64f;           // 1960 highway average, tuned against known drive times
	private const float GasPerMile = 0.02f;       // ~2c/mile: cheap gas, cheap car

	public const float FoundingCapital = 800f;
	private const int PlayerRosterCapacity = 6;

	/// <summary>Where the player went to hear acts. Each room draws a different crowd.</summary>
	public enum ScoutingVenue { ClubsAndRoadhouses, TheatresAndSupperClubs, HonkyTonks, IndustryMeets }

	/// <summary>
	/// One song in an act's live set, as the player heard it. This is what a period act
	/// actually walked in with -- a mix of a couple of their own numbers and the covers and
	/// standards everybody played -- and it is the material the label would cut, so it is the
	/// single biggest tell on the pad. Read values are the player's ear, not the truth.
	/// </summary>
	public sealed class RepertoireItem {
		public string Title;
		/// <summary>"their own" / "cover" / "standard" -- how the song came to the act.</summary>
		public string SourceTag;
		public bool IsOriginal;
		/// <summary>Set for a cover/standard so the recording step (Phase 2) can find the song. Also set for a
		/// commissioned professional song, which is a real composition delivered by a writer to order.</summary>
		public string SongId;
		/// <summary>A professional song written to order and delivered into the set. Not the act's own writing
		/// and not a cover of a known song -- it records as professional material, but only after the player
		/// has seen it here. This is what makes commissioning "see it, then cut it" instead of blind.</summary>
		public bool IsCommission;
		public Genre Genre;
		public float ReadHook;
		public float ReadQuality;
		/// <summary>Once the act has cut this number, it's spent -- dropped from the studio's material list and
		/// shown in the set as recorded, linked to its record. <see cref="RecordedId"/> is the player record.</summary>
		public bool Recorded;
		public string RecordedId;
	}

	/// <summary>A cover the act is working up but doesn't have yet. Started by <see cref="TeachCover"/>; the act
	/// rehearses it on its own over several days (faster the more capable they are) and it lands in their
	/// repertoire on <see cref="ReadyDate"/> -- see <see cref="ProcessCoverRehearsals"/>. One per act at a time.</summary>
	public sealed class CoverRehearsal {
		public string ArtistId;
		public string SongId;
		public string Title;
		public string SourceTag;
		public Genre Genre;
		public float ReadHook;
		public float ReadQuality;
		public GameDate Started;
		public GameDate ReadyDate;
		// A commissioned professional song being written to order, not a cover being rehearsed. Same
		// pending-delivery machinery; it just lands as a commissioned repertoire item instead of a cover.
		public bool IsCommission;
	}

	/// <summary>One act the player looked at on a scouting trip, as the player sees them.</summary>
	public sealed class Prospect {
		public SimulatedArtist Artist;
		/// <summary>The player's read on the act, not its true quality. Better scouting narrows the gap.</summary>
		public float ReadQuality;
		public float AskingAdvance;
		public string Note;
		public ScoutingVenue Venue;
		/// <summary>The act's full live set. Only <see cref="HeardCount"/> of it is visible until follow-up.</summary>
		public readonly List<RepertoireItem> LiveSet = new();
		/// <summary>How many songs the player actually caught on the night, before a second look.</summary>
		public int HeardCount;
		/// <summary>A second look has been taken: the full set is legible and the read has tightened.</summary>
		public bool FollowedUp;
		/// <summary>The label's opening offer, generated once when the player approaches. See <see cref="ApproachToSign"/>.</summary>
		public ContractTermSheet Baseline;
		public bool HasBaseline;
		/// <summary>How hard this act is to sign -- see SimTools/ContractNegotiationDirective.md Part 2.
		/// Pushover stays the single-click ContractForm; Firm/Hardball opens <see cref="Talk"/>.</summary>
		public NegotiationPosture Posture = NegotiationPosture.Pushover;
		/// <summary>The live negotiation scene, non-null only while a Firm/Hardball negotiation is open.</summary>
		public ContractTalk Talk;
		/// <summary>Set only when patience ran out at the table -- the act won't take a fresh approach until then.</summary>
		public GameDate? CooldownUntil;
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
		public string RecordedId;   // the player record it was cut to, once recorded
	}

	/// <summary>What a session is cutting -- where the material comes from.</summary>
	public enum MaterialKind {
		Original,       // one of the act's own numbers (a live-set original or a written song)
		LiveCover,      // a specific cover/standard the act plays, by songId
		Commission,     // a professional/staff-writer song bought in for the act
		FreshStandard,  // reach into the catalog for a standard the act doesn't already play
		FreshHit        // reach into the catalog for a recent hit to cover
	}

	/// <summary>
	/// One thing the player can put on tape for an act. Assembled by <see cref="MaterialOptionsFor"/>
	/// from the act's own repertoire (what they walked in with) plus the material a label can bring to
	/// the act: a commissioned professional song, or a cover pulled fresh from the catalog. This is the
	/// choice that used to be "pick a song you wrote" and is now "pick what this act should record".
	/// </summary>
	public sealed class MaterialChoice {
		public MaterialKind Kind;
		public string Title;         // display + the record's title for Original / LiveCover
		public string SongId;        // LiveCover: the specific composition
		public Song WrittenSong;     // Original: set when it came from the songbook (marked Recorded on cut)
		public string Detail;        // e.g. "their own", "cover", "standard", "professional"
		// For a browsable catalog cover: what the song is and how strong its hook reads, so the player is
		// picking a known quantity rather than a bare title. Zero/unset for material with no catalog song.
		public Genre Genre;
		public float Hook;
		public bool HasSong;         // true when Genre/Hook came from a real catalog composition

		public string Describe() => $"{Title}  ({Detail})";
	}

	/// <summary>The room you book. Better rooms cost more per hour and cut cleaner tape.</summary>
	public enum StudioTier { Budget, Mid, Top }

	/// <summary>One pass at a song. More studio time buys more passes; you keep the best one.</summary>
	public sealed class SessionTake {
		public int Number;
		public float Hook;         // the performance captured on this pass, before the song is weighed in
		public float Production;
		public float Overall => (Hook + Production) * 0.5f;
	}

	/// <summary>One song being cut this session, its takes, and which take the player keeps.</summary>
	public sealed class SessionCut {
		public MaterialChoice Choice;
		public readonly List<SessionTake> Takes = new();
		public int KeptTake;       // index into Takes
	}

	/// <summary>
	/// A booked session sitting on the console: the room, the money already spent, and the takes,
	/// waiting for the player to keep one per song and print the masters. See <see cref="StartSession"/>.
	/// </summary>
	public sealed class PendingSession {
		public string ArtistId;
		public StudioTier Tier;
		public int Hours;
		public float Cost;
		public GameDate Date;
		public readonly List<SessionCut> Cuts = new();
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
		public Master Master;      // the A-side -- the plug side that charts
		public Master BSide;       // the flip; pressed and shipped with it
		// A single is first ASSEMBLED (both sides paired, so it can be pressed and quoted a turnaround) and
		// only later DATED. Until Dated is set it sits "in prep": pressable, but it never ships.
		public bool Dated;
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
		public float TrunkHeld;   // trunk cut out on consignment this week (earned, not yet banked)
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
	// A signed act keeps the live set it walked in with. Phase 2's recording step reads this
	// so an act cuts the material it already plays rather than a fresh batch written on the spot.
	private readonly Dictionary<string, List<RepertoireItem>> repertoire = new();
	// Covers being worked up but not yet in a set (one in progress per act). Completed in ProcessCoverRehearsals.
	private readonly List<CoverRehearsal> rehearsals = new();
	// B-side masters that shipped on the flip of a single. They never become a worked record of their own
	// (only the A-side goes to market and charts), so they never enter ReleasedRecords -- which left their
	// repertoire line reading "cut, not out yet" forever. Tracked here so the repertoire can say the truth:
	// they came out, on the flip. Keyed by the B-side master's own record id.
	private readonly HashSet<string> shippedBSideRecordIds = new(StringComparer.Ordinal);
	private PendingSession pendingSession;
	// Pressed vinyl on hand at the office, per single (by record id). The warehouse stock you draw from
	// to stock towns. The player can only sell what has been pressed AND carried out to a town.
	private readonly Dictionary<string, PressStock> inventory = new();
	// Pressing runs ordered but not yet delivered -- a plant takes weeks (see OrderPressing).
	private readonly List<PressOrder> pressOrders = new();
	// Consignment: records left with a town's shops, per town. This is what actually sells, day by day,
	// decaying until you drive back to restock. cityId -> recordId -> lot.
	private readonly Dictionary<string, Dictionary<string, ConsignmentLot>> consignment = new(StringComparer.Ordinal);
	// Trunk units sold this chart-week per record, accumulated daily and swept into the weekly chart total
	// so a record that only sells out of the trunk still charts on those units.
	private readonly Dictionary<string, int> weeklyTrunkUnits = new(StringComparer.Ordinal);
	// This chart-week's trunk business, accumulated daily in BookTrunkSale and folded into the week's settlement
	// write-up at week-end (then reset) so the settlement reflects trunk sales, not only the wholesale channel.
	// trunkHeld is the cut that went out on consignment (towns you weren't standing in) and hasn't reached the
	// bank yet; the rest was spot cash. Persisted so a mid-week save doesn't drop the partial week.
	private long weeklyTrunkUnitsSold;
	private float weeklyTrunkGross, weeklyTrunkRoyalty, weeklyTrunkHeld;
	// Your cut of trunk sales in towns you're not standing in: the shops hold it for you, you collect the
	// lump when you drive back, and a small trickle wires through between visits. cityId -> dollars owed.
	private readonly Dictionary<string, float> consignmentOwed = new(StringComparer.Ordinal);
	// Towns the player has physically worked -- opened a market to sell out of the trunk.
	private readonly HashSet<string> workedCities = new(StringComparer.Ordinal);
	// Where the player physically is right now. Home office by default; driving changes it, and while
	// away the office/studio work is out of reach. See the ON THE ROAD region.
	private string currentCityId;
	// Prospects A&R generated to fill a scouting slate (fresh local talent). Any the player doesn't sign
	// are pulled back out of the population when the next slate is worked up, so scouting doesn't bloat it.
	private readonly HashSet<string> generatedProspectIds = new(StringComparer.Ordinal);
	private float lastSnapshotCash;
	private int counter;

	// LOSING. A scrappy label can run a tab -- you're allowed into the red, but only so far and only so
	// long. The bank carries you down to a credit line scaled to your overhead; blow past that, or sit
	// under water too many months running, and the creditors close you. See OnMonthChanged / GameOver.
	private const float OverdraftMonths = 3f;   // credit line = this many months of overhead in the red
	private const int MaxMonthsInTheRed = 3;    // consecutive months below zero the bank will tolerate
	private int monthsInTheRed;

	/// <summary>The label has folded -- the run is over. The desk shows a game-over card and stops taking actions.</summary>
	public bool IsGameOver { get; private set; }
	/// <summary>Why the label folded, shown on the game-over card.</summary>
	public string GameOverReason { get; private set; }
	/// <summary>How far into the red the bank will let you go before it closes you (a negative dollar figure).</summary>
	public float CreditFloor => -OverdraftMonths * Mathf.Max(1f, Label?.GetMonthlyOverhead() ?? 0f);
	/// <summary>Consecutive months the label has ended under water. At <see cref="MaxMonthsInTheRed"/> the run ends.</summary>
	public int MonthsInTheRed => monthsInTheRed;
	/// <summary>Months of red left before the bank closes you (0 while solvent).</summary>
	public int MonthsOfGraceLeft => Label != null && Label.cashReserves < 0f ? Mathf.Max(0, MaxMonthsInTheRed - monthsInTheRed) : MaxMonthsInTheRed;

	// ── Rolodex ────────────────────────────────────────────────────────────────────────────────
	// Cards the player has discovered. Unknown DJs have no entry; the list only holds contacted ones.
	private readonly List<RolodexEntry> rolodex = new();
	public IReadOnlyList<RolodexEntry> Rolodex => rolodex;
	// Legacy sub-hour accumulator. The clock carries its own minute hand now (TimeManager.SpendMinutes),
	// so nothing adds to this any more; it is kept only so an older save round-trips without a schema break.
	private int phoneMinutesAccum;
	// Per-action rapport gain is always bounded -- no single call or check manufactures a hit.
	// The read is uncapped (rt.Rapport can climb past this), but the SOFT cap shrinks marginal gains
	// as it climbs, and DecayLabelRapport (StationNetwork) pulls an uncultivated relationship back down.
	private const float RapportSoftCap = 1.0f;

	/// <summary>Who the player was before opening the label. Set at founding; gates Rolodex reads and actions.</summary>
	public FoundingArchetype Archetype { get; private set; } = FoundingArchetype.TradeInsider;
	/// <summary>The four Executive Instinct scores derived from <see cref="Archetype"/>. Never null after founding.</summary>
	public ExecutiveInstinctProfile InstinctProfile { get; private set; } = FoundingArchetypeData.Get(FoundingArchetype.TradeInsider).Instincts;

	// A 45 pressing plant's period bill: cheap vinyl by the unit over a minimum run, a one-off lacquer
	// setup per side, and a little for sleeves, labels and getting the boxes to your office.
	public const int PressMinimumOrder = 500;
	public const float PressVinylPerUnit = 0.22f;
	public const float PressSleeveLabelPerUnit = 0.03f;
	public const float PressLacquerSetup = 38f;
	public const float PressShipping = 20f;

	public static float PressingCost(int quantity) =>
		PressLacquerSetup + PressShipping + Mathf.Max(0, quantity) * (PressVinylPerUnit + PressSleeveLabelPerUnit);

	/// <summary>Pressed 45s of one single sitting on hand at the office, to be carried out to towns.</summary>
	public sealed class PressStock {
		public int Remaining;
		public int TotalPressed;
		public float TotalSpent;
	}

	/// <summary>A pressing run in the pipeline: paid for, mailed off, and working its way back from the
	/// plant. Delivered to <see cref="inventory"/> on its arrival day.</summary>
	public sealed class PressOrder {
		public string RecordId;
		public int Quantity;
		public float Cost;
		public GameDate Ordered;
		public GameDate Arrives;
	}

	/// <summary>Records left with one town's shops. They sell on their own, day by day, at a rate that
	/// decays the longer it's been since you restocked -- the town's appetite tapers until you come back.</summary>
	public sealed class ConsignmentLot {
		public int Remaining;
		public int Placed;
		public int DaysSinceRestock;
	}

	// Pressing pipeline (real 1960 plant turnaround), each a day range rolled per order.
	private const int PressMailDaysMin = 1, PressMailDaysMax = 4;      // mailing the master tape out
	private const int PressPlatingDaysMin = 3, PressPlatingDaysMax = 5; // cutting lacquers / plating
	private const int PressQueueDaysMin = 7, PressQueueDaysMax = 14;    // waiting in the plant's queue
	private const int PressShipDaysMin = 3, PressShipDaysMax = 7;       // boxes shipped back to you

	// Nightly motel bill when you sleep somewhere other than your own bed.
	public const float HotelNightly = 9f;

	// A town's shops sell a small daily slice of what they're holding; the slice shrinks each day since a
	// restock (the novelty wears off) until you drive back with fresh stock.
	private const float TrunkDailyBaseFraction = 0.07f; // of the lot, on a fresh restock (before appeal/buzz/luck scale it down)
	private const float TrunkDecayPerDay = 0.90f;       // multiplied in each day since the restock
	// While you're away, a town's shops wire you a thin slice of what they owe each day, so money isn't
	// fully stranded -- but the bulk waits for you to drive back and collect it in person.
	private const float TrunkWireFractionPerDay = 0.04f;

	public PressStock StockFor(string recordId) =>
		recordId != null && inventory.TryGetValue(recordId, out PressStock stock) ? stock : null;
	public IEnumerable<string> WorkedCities => workedCities;

	/// <summary>The session on the console waiting for takes to be kept, or null.</summary>
	public PendingSession Session => pendingSession;

	/// <summary>The live set a signed act carries, or an empty list.</summary>
	public IReadOnlyList<RepertoireItem> RepertoireFor(string artistId) =>
		artistId != null && repertoire.TryGetValue(artistId, out List<RepertoireItem> set)
			? set : (IReadOnlyList<RepertoireItem>)Array.Empty<RepertoireItem>();

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
			// Likewise last for the month: CompetitorManager has already charged the player's overhead
			// by the time this runs, so the solvency check reads the post-overhead balance.
			TimeManager.Instance.OnMonthChanged += OnMonthChanged;
		}
	}

	public override void _ExitTree() {
		if (TimeManager.Instance != null) {
			TimeManager.Instance.OnDayStarted -= OnDayStarted;
			TimeManager.Instance.OnHourChanged -= OnHourChanged;
			TimeManager.Instance.OnWeekEnded -= OnWeekEnded;
			TimeManager.Instance.OnMonthChanged -= OnMonthChanged;
		}
		if (Instance == this) Instance = null;
	}

	private void OnHourChanged(int hour) => Changed?.Invoke();

	/// <summary>
	/// The monthly reckoning. CompetitorManager has already taken this month's overhead out of the bank by
	/// the time this runs (PlayerDesk is the last autoload). If that has put the label past its credit line,
	/// the run ends now; if it has merely put it under water, the bank tolerates a few months of that before
	/// pulling the plug. Climb back to black and the count resets -- a bad month is survivable, a bad quarter
	/// is not.
	/// </summary>
	private void OnMonthChanged(GameDate date) {
		if (Label == null || IsGameOver) return;
		if (Label.cashReserves < CreditFloor) {
			GameOver($"You ran past your ${-CreditFloor:N0} credit line. The creditors have closed {Label.labelName} down.");
			return;
		}
		if (Label.cashReserves < 0f) {
			monthsInTheRed++;
			if (monthsInTheRed >= MaxMonthsInTheRed) {
				GameOver($"{MaxMonthsInTheRed} months in the red with the bills unpaid. {Label.labelName} folds.");
				return;
			}
			Note($"Another month in the red (${Label.cashReserves:N0}). {MonthsOfGraceLeft} month(s) before the bank closes you.");
		} else if (monthsInTheRed > 0) {
			monthsInTheRed = 0;
			Note("Back in the black -- the bank's off your back for now.");
		}
		CheckForManagerInterest();
		CheckForMaturedContracts(date.year);
		Changed?.Invoke();
	}

	private void GameOver(string reason) {
		IsGameOver = true;
		GameOverReason = reason;
		if (Label != null) Label.status = LabelStatus.Bankrupt;
		Note($"THE DOORS CLOSE: {reason}");
		Changed?.Invoke();
	}

	// ========================================================================
	// FOUNDING
	// ========================================================================

	// Shim for the save-load probe (and any caller that doesn't need the archetype picker).
	public bool FoundLabel(string labelName, string cityId, out string message) =>
		FoundLabel(labelName, cityId, FoundingArchetype.TradeInsider, out message);

	public bool FoundLabel(string labelName, string cityId, FoundingArchetype archetype, out string message) {
		if (Label != null) { message = "You already run a label."; return false; }
		// The player picks the town they work out of; the market it sits in is inferred from it.
		MarketCity city = DistanceModel.GetCityById(cityId);
		if (city == null) { message = "Pick a home town first."; return false; }
		MarketRegion region = ChartManager.Instance?.GetRegionById(city.parentRegionId);
		if (region == null) { message = "That town has no market resolved."; return false; }

		FoundingArchetypeData.ArchetypeProfile profile = FoundingArchetypeData.Get(archetype);
		Archetype = archetype;
		InstinctProfile = profile.Instincts;

		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		var label = new AILabel {
			labelId = "player_label",
			labelName = string.IsNullOrWhiteSpace(labelName) ? "Player Records" : labelName.Trim(),
			founderName = "You",
			headquartersCity = city.name,
			archetype = LabelArchetype.RegionalHustler,
			tier = LabelTier.Small,
			foundedYear = year,
			isHistorical = false,
			isPlayerOwned = true,
			status = LabelStatus.Stable,
			homeRegion = region.regionId,
			// You have no standing distribution at all: your own markets are worked town by town out of
			// the trunk (daily, in PlayerDesk), and the weekly engine only sells where a wholesale house
			// carries you -- so distributionRegions starts empty. Everything past this is earned.
			strongRegions = new[] { region.regionId },
			distributionRegions = Array.Empty<string>(),
			cashReserves = profile.Capital,
			maxRosterSize = PlayerRosterCapacity,
			nationalReach = 0.02f,
			budgetLevel = 0.15f,
			scoutingAbility = profile.ScoutingAbility,
			productionQuality = profile.ProductionQuality,
			marketingPower = profile.MarketingPower,
			riskTolerance = profile.RiskTolerance,
			artistLoyalty = profile.ArtistLoyalty,
			payolaWillingness = profile.PayolaWillingness,
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
		currentCityId = city.cityId; // you start at your own office

		Note($"{label.labelName} opens for business in {city.name}, {region.regionName} with ${profile.Capital:N0}.");
		Note($"You are {profile.Name}. {profile.Tagline}");
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

	/// <summary>The room a venue draws. Empty means "anyone worth a pop label's evening".</summary>
	private static IReadOnlyCollection<GenreFamily> FamiliesFor(ScoutingVenue venue) => venue switch {
		// The clubs, dance halls and roadhouses -- where the loud, young, danceable acts play.
		ScoutingVenue.ClubsAndRoadhouses => new[] {
			GenreFamily.Rock, GenreFamily.RhythmAndSoul, GenreFamily.Blues, GenreFamily.Latin, GenreFamily.Caribbean },
		// The theatres, supper clubs and ballrooms -- traditional pop, vocal, and jazz.
		ScoutingVenue.TheatresAndSupperClubs => new[] { GenreFamily.Pop, GenreFamily.Jazz },
		// The honky-tonks and coffee houses -- country and folk.
		ScoutingVenue.HonkyTonks => new[] { GenreFamily.Country, GenreFamily.Folk },
		// Publishers, agents and the trade -- no family filter, but the connected acts are better.
		_ => Array.Empty<GenreFamily>()
	};

	public static string VenueName(ScoutingVenue venue) => venue switch {
		ScoutingVenue.ClubsAndRoadhouses => "the clubs & roadhouses",
		ScoutingVenue.TheatresAndSupperClubs => "the theatres & supper clubs",
		ScoutingVenue.HonkyTonks => "the honky-tonks",
		_ => "an industry meet"
	};

	/// <summary>When each kind of room is worth showing up to, as [open, close) hours on the 24h clock.
	/// Clubs are a night thing; the trade keeps business hours; honky-tonks run lunchtime sets on.</summary>
	public static (int Open, int Close) VenueHours(ScoutingVenue venue) => venue switch {
		ScoutingVenue.ClubsAndRoadhouses => (17, 21),     // the acts don't go on until night
		ScoutingVenue.TheatresAndSupperClubs => (14, 21), // matinee through the supper show
		ScoutingVenue.HonkyTonks => (12, 21),             // lunchtime sets right through closing
		_ => (9, 17)                                       // publishers and agents keep an office day
	};

	private static string Clock12(int hour) {
		int h = ((hour + 11) % 12) + 1;
		return $"{h} {(hour < 12 || hour >= 24 ? "AM" : "PM")}";
	}

	private bool VenueOpenNow(ScoutingVenue venue, out string message) {
		int hour = TimeManager.Instance?.CurrentHour ?? 12;
		(int open, int close) = VenueHours(venue);
		if (hour >= open && hour < close) { message = string.Empty; return true; }
		message = hour < open
			? $"{Capitalize(VenueName(venue))} don't get going until {Clock12(open)}. Come back later."
			: $"{Capitalize(VenueName(venue))} have wound down for the night.";
		return false;
	}

	private static string Capitalize(string text) =>
		string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);

	private static GenreFamily FamilyOf(SimulatedArtist artist) =>
		GenreCatalog.Get(GenreCatalog.MapLegacy(artist.primaryGenre,
			TimeManager.Instance?.CurrentDate.year ?? 1960)).Family;

	/// <summary>
	/// The genres that are, in period terms, "manufactured" industry product -- youth pop assembled and
	/// worked by the trade (Brill-Building teen pop, girl groups, bubblegum, sunshine pop, doo-wop, and
	/// a lot of rock'n'roll). These are who the industry meet points you at, and they command more.
	/// </summary>
	private static readonly HashSet<Genre> ManufacturedGenres = new() {
		Genre.TeenPop, Genre.Bubblegum, Genre.GirlGroup, Genre.DooWop, Genre.SunshinePop, Genre.RockAndRoll
	};

	/// <summary>
	/// What an unknown act asks to sign, by the room you found them in. The room IS the price band in
	/// 1960: a rock'n'roll quartet in a roadhouse takes dinner money and a promise, a professional act
	/// working a supper club has a going rate, and the trade's manufactured product arrives with people
	/// behind it and a number in mind. These are the BASE asks -- the act's own talent and standing
	/// scale them in <see cref="VenueAdvanceAsk"/>, and a manager scales them again on top.
	/// Player-only: the AI economy still prices signings off <see cref="AILabel.CalculateAdvanceOffer"/>.
	/// </summary>
	private static float VenueAdvanceBase(ScoutingVenue venue) => venue switch {
		ScoutingVenue.HonkyTonks            => 20f,    // a fifth and a steak dinner
		ScoutingVenue.ClubsAndRoadhouses    => 25f,    // "you're buying, right?"
		ScoutingVenue.TheatresAndSupperClubs => 260f,  // working pros with a going rate
		ScoutingVenue.IndustryMeets         => 600f,   // polished product, professionally represented
		_                                   => 25f
	};

	/// <summary>
	/// The advance this act asks the player for. Venue sets the band; the act's talent and any standing
	/// it has scale inside it (same shape as the AI's offer curve, so the two read consistently); the
	/// manager multiplies on top, which is why a Shark on a bar band is still a tell. Rounded to a
	/// number a period contract would actually carry.
	/// </summary>
	private static float VenueAdvanceAsk(SimulatedArtist artist, ScoutingVenue venue) {
		float talent = 0.5f + (artist.CalculateBaseQuality() * 1.5f);          // 0.5x .. 2.0x
		float standing = 1f + (artist.reputation * 2f) + (artist.momentum * 1.5f);
		float ask = VenueAdvanceBase(venue) * talent * standing
			* ManagerProfile.Of(artist.manager).AdvanceDemandMult;
		return RoundToContractFigure(ask);
	}

	/// <summary>
	/// The royalty the act expects, by the room. Same logic as the advance band: what a 1960 act asked
	/// for was set by who was standing next to them, and for a small act with no representation that was
	/// one to three points -- Stevie Wonder's first Motown deal was ~2%, the Jackson 5's later ~2.7%.
	/// The supper-club professional has a going rate; the trade's product is represented and knows it.
	///
	/// This is the rate with a REASONABLE CHANCE OF ACCEPTANCE, not a floor and not a promise. The player
	/// can offer under it (see <see cref="PlayerRoyaltyFloor"/>); the further under, the likelier the
	/// pushback -- see SimTools/ContractNegotiationDirective.md Part 2 for the acceptance curve.
	/// Player-only: AI signings keep pricing off <see cref="AILabel.CalculateRoyaltyRate"/>.
	/// </summary>
	private static float VenueRoyaltyBaseline(SimulatedArtist artist, ScoutingVenue venue) {
		float band = venue switch {
			ScoutingVenue.HonkyTonks             => 0.015f,  // a point and a half and a handshake
			ScoutingVenue.ClubsAndRoadhouses     => 0.020f,  // the standard small-act deal
			ScoutingVenue.TheatresAndSupperClubs => 0.030f,  // working pros with a going rate
			ScoutingVenue.IndustryMeets          => 0.045f,  // represented, and they know the number
			_                                    => 0.020f
		};
		// An act with a career behind it has leverage regardless of the room it is playing tonight.
		band += artist.careerState switch {
			CareerState.Superstar => 0.05f, CareerState.Star => 0.03f,
			CareerState.Established => 0.015f, CareerState.Rising => 0.005f, _ => 0f
		};
		band *= ManagerProfile.Of(artist.manager).RoyaltyDemandMult;
		// Quarter-point steps: contracts were not written to four decimal places.
		return Mathf.Clamp(Mathf.Round(band * 400f) / 400f, PlayerRoyaltyFloor, 0.15f);
	}

	/// <summary>How low the player is ALLOWED to write it. Half a point is the bottom of what the era
	/// actually papered (the Beatles' 1962 EMI deal worked out under one point). Whether the act signs
	/// it is a different question from whether the form accepts it.</summary>
	public const float PlayerRoyaltyFloor = 0.005f;

	/// <summary>
	/// The deliverables the player's ask opens on: 2-3 singles a year is the period norm for a new
	/// act, tapering to none once a career is established -- the same career-state gate
	/// <see cref="AILabel.CalculateContractSinglesObligation"/> uses, so a Star isn't shown a quota a
	/// real Star wouldn't carry. Player-only re-price, same spirit as <see cref="VenueAdvanceAsk"/>
	/// and <see cref="VenueRoyaltyBaseline"/> -- the AI's own obligation formula is untouched.
	/// </summary>
	private static int PlayerDeliverablesAsk(SimulatedArtist artist, int termYears, int year) {
		if (year > AILabel.SinglesObligationFinalYear) return 0;
		if (artist.careerState is CareerState.Established or CareerState.Star or CareerState.Superstar) return 0;
		float perYear = artist.careerState == CareerState.Rising ? 3f : 2f;
		return Mathf.Clamp(Mathf.RoundToInt(perYear * Mathf.Max(1, termYears)), 1, 24);
	}

	/// <summary>Advances were written in round money: $5 steps under a hundred, $25 steps over it.</summary>
	private static float RoundToContractFigure(float amount) =>
		amount < 100f ? Mathf.Max(5f, Mathf.Round(amount / 5f) * 5f) : Mathf.Round(amount / 25f) * 25f;

	/// <summary>
	/// An evening working one kind of room in the player's own market. Returns the acts they got a look
	/// at: unsigned, local, playing the genres that room draws, read through the label's own ear rather
	/// than perfectly. An industry meet is different in kind -- it points you at the trade's manufactured
	/// youth-pop product, polished and pricier, rather than whoever is playing the local scene.
	/// </summary>
	public bool ScoutVenue(ScoutingVenue venue, out string message) {
		if (!Require(ScoutHours, out message)) return false;
		if (!VenueOpenNow(venue, out message)) return false;
		// You scout the scene wherever you physically are -- no signing an act 700 miles from home who'd
		// never relocate. On the road that's the town you're in; otherwise your own market.
		MarketRegion region = CurrentRegion();
		if (region == null) { message = "No market resolved where you are."; return false; }

		bool trade = venue == ScoutingVenue.IndustryMeets;
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		Dictionary<Genre, float> affinity = BuildRegionAffinity(region);
		IReadOnlyCollection<GenreFamily> families = FamiliesFor(venue);

		Spend(ScoutHours);
		PurgeGeneratedProspects(); // let go of the last slate's unsigned discoveries
		slate.Clear();
		SlateDate = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int target = (int)GD.RandRange(2, trade ? 3 : 4);

		// Real local unsigned acts the room would draw -- but only genres the market has an ear for, so an
		// implausible act (Tex-Mex in the Great Lakes) never crowds the bill.
		List<SimulatedArtist> realRoom = (ArtistManager.Instance?.GetUnsignedArtists() ?? new List<SimulatedArtist>())
			.Where(a => IsLocal(a, region) && IsWorthAPopLabelsEvening(a)
				&& AdmitsGenre(venue, trade, families, a.primaryGenre, year)
				&& AffinityOf(affinity, a.primaryGenre) >= MinScoutAffinity)
			.ToList();
		float Weight(SimulatedArtist a) => WeightOf(a) * AffinityMultiplier(affinity, a.primaryGenre);
		var slateActs = trade
			? realRoom.OrderByDescending(a => a.CalculateBaseQuality() * AffinityMultiplier(affinity, a.primaryGenre)
				* (float)GD.RandRange(0.85, 1.15)).Take(target).ToList()
			: SampleTheRoom(realRoom, Mathf.Min(target, realRoom.Count), Weight);

		// The scene never runs dry: once the AI has signed the standing pool (it signs nearly all of it),
		// the rest of the bill is fresh local acts coming up -- real, signable artists homed here, in
		// genres the market supports. This is what keeps A&R alive and keeps the bill genre-appropriate.
		List<Genre> plausible = PlausibleGenres(region, affinity, venue, trade, year);
		while (slateActs.Count < target && plausible.Count > 0) {
			SimulatedArtist fresh = GenerateLocalProspect(region, PickByAffinity(plausible, affinity), year);
			if (fresh == null) break;
			slateActs.Add(fresh);
		}

		if (slateActs.Count == 0) {
			Note($"A wasted evening around {VenueName(venue)} in {region.regionName} -- nobody worth hearing.");
			message = "Nobody worth hearing tonight.";
			Changed?.Invoke();
			return true;
		}

		foreach (SimulatedArtist artist in slateActs) {
			float truth = artist.CalculateBaseQuality();
			float noise = (1f - Mathf.Clamp(Label.scoutingAbility, 0f, 1f)) * 0.25f;
			var prospect = new Prospect {
				Artist = artist,
				Venue = venue,
				ReadQuality = Mathf.Clamp(truth + (float)GD.RandRange(-noise, noise), 0f, 1f),
				AskingAdvance = VenueAdvanceAsk(artist, venue),
				Note = DescribeProspect(artist)
			};
			BuildLiveSet(prospect, artist, year, noise);
			slate.Add(prospect);
		}
		Note($"Worked {VenueName(venue)} in {region.regionName}: {slate.Count} act(s) on the pad.");
		message = $"Caught {slate.Count} act(s).";
		Changed?.Invoke();
		return true;
	}

	// A genre the market has essentially no ear for is not on the bill at all -- this is the hard floor
	// under the softer AffinityMultiplier weighting, and it's what keeps Tex-Mex out of a Detroit club.
	private const float MinScoutAffinity = 0.08f;

	private static float AffinityOf(Dictionary<Genre, float> affinity, Genre genre) =>
		affinity != null && affinity.TryGetValue(genre, out float value) ? value : 0f;

	private static GenreFamily FamilyOfGenre(Genre genre, int year) =>
		GenreCatalog.Get(GenreCatalog.MapLegacy(genre, year)).Family;

	/// <summary>Whether a genre belongs in this room: the trade deals in manufactured youth-pop; every
	/// other room is filtered by the families it draws.</summary>
	private static bool AdmitsGenre(ScoutingVenue venue, bool trade, IReadOnlyCollection<GenreFamily> families, Genre genre, int year) =>
		trade ? ManufacturedGenres.Contains(genre) : families.Count == 0 || families.Contains(FamilyOfGenre(genre, year));

	/// <summary>The genres a room could plausibly turn up here: in the room's remit and with a real local
	/// audience. Used to generate fresh talent that fits both the venue and the market.</summary>
	private List<Genre> PlausibleGenres(MarketRegion region, Dictionary<Genre, float> affinity, ScoutingVenue venue, bool trade, int year) {
		IReadOnlyCollection<GenreFamily> families = FamiliesFor(venue);
		var genres = new List<Genre>();
		foreach (GenrePreference preference in region?.genrePreferences ?? Array.Empty<GenrePreference>())
			if (preference.affinity >= MinScoutAffinity && AdmitsGenre(venue, trade, families, preference.genre, year))
				genres.Add(preference.genre);
		return genres;
	}

	private static Genre PickByAffinity(List<Genre> genres, Dictionary<Genre, float> affinity) {
		float total = 0f;
		foreach (Genre g in genres) total += AffinityOf(affinity, g);
		float target = total > 0f ? (float)GD.RandRange(0f, total) : 0f;
		foreach (Genre g in genres) { target -= AffinityOf(affinity, g); if (target <= 0f) return g; }
		return genres[genres.Count - 1];
	}

	private static readonly ArtistType[] ScoutableTypes =
		{ ArtistType.Band, ArtistType.SoloMale, ArtistType.SoloFemale, ArtistType.VocalGroup, ArtistType.Duo };

	/// <summary>
	/// Mints a fresh, signable local act in a given genre, homed in the current market -- an up-and-coming
	/// bar band the player just "discovered". Registered as a Seeking prospect so the sign path accepts it;
	/// tracked so it's pulled back out of the population if the player never signs it.
	/// </summary>
	private SimulatedArtist GenerateLocalProspect(MarketRegion region, Genre genre, int year) {
		ArtistManager manager = ArtistManager.Instance;
		if (manager == null) return null;
		ArtistType type = ScoutableTypes[(int)GD.RandRange(0, ScoutableTypes.Length - 1)];
		SimulatedArtist artist = manager.GenerateArtist(type, genre, year, region.regionName);
		if (artist == null) return null;
		artist.prospectMarketStatus = ProspectMarketStatus.Seeking; // otherwise the signing gate refuses it
		generatedProspectIds.Add(artist.artistId);
		return artist;
	}

	/// <summary>Returns every generated prospect the player didn't sign to the ether. Signed ones were
	/// dropped from the tracking set at signing, and the remove is refused for anything now on a label.</summary>
	private void PurgeGeneratedProspects() {
		foreach (string id in generatedProspectIds) ArtistManager.Instance?.RemoveUnsignedArtist(id);
		generatedProspectIds.Clear();
	}

	/// <summary>The market the player is physically in right now (home office by default).</summary>
	private MarketRegion CurrentRegion() {
		string cityId = currentCityId ?? Label?.homeCityId;
		string regionId = DistanceModel.GetCityById(cityId)?.parentRegionId ?? Label?.homeRegion;
		return ChartManager.Instance?.GetRegionById(regionId);
	}

	/// <summary>
	/// The act's live set: a couple of their own numbers plus the covers and standards everybody
	/// played. The originals are stubs -- title and a read only -- until the act is signed and the
	/// song is actually cut; the covers point at real catalog songs so the recording step can pull
	/// the composition. Only <see cref="Prospect.HeardCount"/> of this is visible before a follow-up.
	/// </summary>
	private void BuildLiveSet(Prospect prospect, SimulatedArtist artist, int year, float readNoise) {
		float Read(float truth) => Mathf.Clamp(truth + (float)GD.RandRange(-readNoise, readNoise), 0f, 1f);

		// How many of their own the act carries scales with their writing.
		int originals = artist.songwritingAbility > 0.6f ? 2 : artist.songwritingAbility > 0.3f ? 1 : 0;
		for (int i = 0; i < originals; i++) {
			float hook = Mathf.Clamp(artist.songwritingAbility * 0.7f + (float)GD.RandRange(-0.15, 0.25), 0f, 1f);
			prospect.LiveSet.Add(new RepertoireItem {
				Title = NameGenerator.Instance?.GenerateSongTitle(artist.primaryGenre, year, artist.artistId) ?? $"Untitled",
				SourceTag = "their own", IsOriginal = true, Genre = artist.primaryGenre,
				ReadHook = Read(hook), ReadQuality = Read(hook)
			});
		}

		// Fill the rest of the set (aim for 3-5 songs total) with the covers and standards on offer.
		// Genre-exact first, then the whole family, so a small-genre act still walks in with a set --
		// the same cross-family pools the recording service draws from.
		var pool = new List<SongComposition>();
		pool.AddRange(CompositionCatalogService.GetStandardsForGenre(artist.primaryGenre));
		pool.AddRange(CompositionCatalogService.GetCoverableHitsForGenre(artist.primaryGenre));
		if (pool.Count < 4) {
			GenreFamily family = FamilyOf(artist);
			pool.AddRange(CompositionCatalogService.GetStandardsForFamily(family));
			pool.AddRange(CompositionCatalogService.GetCoverableHitsForFamily(family));
		}
		int want = (int)GD.RandRange(3, 5) - prospect.LiveSet.Count;
		var seen = new HashSet<string>();
		for (int i = 0; i < want && pool.Count > 0; i++) {
			SongComposition song = pool[(int)GD.RandRange(0, pool.Count - 1)];
			if (song == null || !seen.Add(song.songId)) { i--; if (seen.Count >= pool.Count) break; continue; }
			prospect.LiveSet.Add(new RepertoireItem {
				Title = song.title, SourceTag = song.isStandard ? "standard" : "cover",
				IsOriginal = false, SongId = song.songId, Genre = song.primaryGenre,
				ReadHook = Read(song.commercialHook), ReadQuality = Read(song.GetCraftScore())
			});
		}

		// You caught one or two on the night; the rest is what they say they play.
		prospect.HeardCount = Mathf.Min(prospect.LiveSet.Count, (int)GD.RandRange(1, 2));
	}

	/// <summary>
	/// A second look and a talk. Cheaper than the first night: the act is already in front of you.
	/// Reveals the whole live set, tightens the read on both the act and its material, and firms up
	/// the asking advance -- the information you buy before deciding whether to make an offer.
	/// </summary>
	public bool FollowUp(Prospect prospect, out string message) {
		if (prospect?.Artist == null) { message = "No act selected."; return false; }
		if (prospect.FollowedUp) { message = "You've already had a second look."; return true; }
		if (!Require(FollowUpHours, out message)) return false;

		Spend(FollowUpHours);
		prospect.FollowedUp = true;
		prospect.HeardCount = prospect.LiveSet.Count;
		// The read tightens toward the truth now that you've spent real time on them.
		float truth = prospect.Artist.CalculateBaseQuality();
		prospect.ReadQuality = Mathf.Clamp(Mathf.Lerp(prospect.ReadQuality, truth, 0.6f), 0f, 1f);
		Note($"Followed up with {prospect.Artist.stageName} -- heard the full set ({prospect.LiveSet.Count} songs).");
		message = $"You know {prospect.Artist.stageName} a lot better now.";
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
	private static List<SimulatedArtist> SampleTheRoom(List<SimulatedArtist> pool, int count, Func<SimulatedArtist, float> weightOf) {
		var remaining = new List<SimulatedArtist>(pool);
		var weights = remaining.Select(weightOf).ToList();
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

	/// <summary>The region's taste, genre -> affinity, from its <see cref="GenrePreference"/> table.</summary>
	private static Dictionary<Genre, float> BuildRegionAffinity(MarketRegion region) {
		var map = new Dictionary<Genre, float>();
		foreach (GenrePreference preference in region?.genrePreferences ?? Array.Empty<GenrePreference>())
			map[preference.genre] = preference.affinity;
		return map;
	}

	/// <summary>
	/// How much the region's taste scales an act's chance of being on the bill. A genre the region
	/// loves rides near full weight; one it has no ear for is knocked down hard but kept just possible,
	/// so a lone Tex-Mex act in Chicago is a rarity rather than an impossibility or an infestation.
	/// </summary>
	private static float AffinityMultiplier(Dictionary<Genre, float> affinity, Genre genre) {
		float a = affinity != null && affinity.TryGetValue(genre, out float value) ? value : 0f;
		return 0.10f + Mathf.Clamp(a, 0f, 1f);
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

	/// <summary>
	/// Opens negotiations with an act you've taken a second look at. Generates the label's opening
	/// offer once -- the manager's demands, if the act has one, land here -- and stashes it on the
	/// prospect for the contract menu to pre-fill. Costs no hours: the talking is the contract, and
	/// that is where <see cref="OfferContract"/> charges. Returns false (with a reason) if there is
	/// no point opening the menu at all.
	/// </summary>
	public bool ApproachToSign(Prospect prospect, out string message) {
		if (prospect?.Artist == null) { message = "No act selected."; return false; }
		if (!prospect.FollowedUp) { message = "Follow up with them before you make an offer."; return false; }
		if (!Label.HasRosterSpace) { message = "Roster is full."; return false; }
		if (!string.IsNullOrEmpty(prospect.Artist.labelId)) { message = "Somebody signed them first."; return false; }
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		if (prospect.CooldownUntil.HasValue && today < prospect.CooldownUntil.Value) {
			message = $"{prospect.Artist.stageName}'s side isn't ready to talk again yet -- try after {prospect.CooldownUntil.Value.ToShortString()}.";
			return false;
		}

		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		if (ArtistManager.Instance?.IsEligibleForPopulationSigning(prospect.Artist, week) == false) {
			message = "They're not taking offers right now.";
			return false;
		}

		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		ContractTermSheet t = Label.GenerateTermSheet(prospect.Artist, year);
		// The term sheet's own advance is the AI's tier-priced offer; for the player the ROOM sets the
		// band, so the ask the player already saw on the pad is the number that opens the table. Keeping
		// them the same figure is what makes the ask an anchor you can negotiate against.
		prospect.AskingAdvance = VenueAdvanceAsk(prospect.Artist, prospect.Venue);
		float royalty = VenueRoyaltyBaseline(prospect.Artist, prospect.Venue);
		int singles = PlayerDeliverablesAsk(prospect.Artist, t.TermYears, year);
		prospect.Baseline = new ContractTermSheet(prospect.AskingAdvance, royalty, t.TermYears,
			singles, t.LabelOwnsPublishing, t.ArtistCreativeControl,
			t.NegotiationDifficulty, t.Manager, t.ManagerName,
			AILabel.BuildDemandSummary(t.Manager, prospect.AskingAdvance, royalty,
				t.LabelOwnsPublishing, t.ArtistCreativeControl));
		prospect.HasBaseline = true;

		// NegotiationDifficulty used to be stored and never read (see ContractTermSheet's own doc
		// comment). This is where it finally gets consumed: most acts stay Pushover -- today's
		// single-click form -- and a managed or high-drama act opens the negotiation scene instead.
		prospect.Posture = PostureOf(prospect.Artist);
		prospect.Talk = prospect.Posture == NegotiationPosture.Pushover ? null : OpenNegotiation(prospect);

		message = prospect.Posture != NegotiationPosture.Pushover
			? $"{prospect.Artist.stageName} wants to talk terms. {prospect.Baseline.DemandSummary}"
			: string.IsNullOrEmpty(prospect.Baseline.DemandSummary)
				? $"Table an offer for {prospect.Artist.stageName}."
				: prospect.Baseline.DemandSummary;
		Changed?.Invoke();
		return true;
	}

	/// <summary>
	/// Puts a concrete contract on the table. The player set the terms; this is where the six-hour
	/// negotiation is spent and the act is actually signed. Non-money fields (singles obligation,
	/// negotiation difficulty, the manager) are carried from the opening offer so a hand-set advance
	/// does not erase the rest of the deal.
	/// </summary>
	public bool OfferContract(Prospect prospect, float advance, float royaltyRate, int termYears, int singlesObligation,
		bool labelOwnsPublishing, bool artistCreativeControl, out string message) {
		if (prospect?.Artist == null) { message = "No act selected."; return false; }
		if (!prospect.HasBaseline) { message = "Approach them first."; return false; }
		// Firm/Hardball acts don't take an accept-or-walk offer -- they go through TableOffer's
		// negotiation loop instead. See SimTools/ContractNegotiationDirective.md Part 2.
		if (prospect.Posture != NegotiationPosture.Pushover) {
			message = "They want to talk terms, not just sign -- work it through the negotiation.";
			return false;
		}
		if (!Require(SignHours, out message)) return false;
		if (!Label.HasRosterSpace) { message = "Roster is full."; return false; }
		if (!string.IsNullOrEmpty(prospect.Artist.labelId)) { message = "Somebody signed them first."; return false; }

		advance = Mathf.Max(0f, advance);
		if (!Label.CanAffordToSign(advance)) {
			message = $"You can't cover a ${advance:N0} advance and hold next month's overhead.";
			return false;
		}

		ContractTermSheet b = prospect.Baseline;
		var sheet = new ContractTermSheet(
			advance, Mathf.Clamp(royaltyRate, PlayerRoyaltyFloor, 0.15f), Mathf.Clamp(termYears, 1, 7),
			Mathf.Clamp(singlesObligation, 0, 30), labelOwnsPublishing, artistCreativeControl,
			b.NegotiationDifficulty, b.Manager, b.ManagerName, b.DemandSummary);

		Spend(SignHours);
		FinalizeSigning(prospect, sheet, out message);
		Changed?.Invoke();
		return true;
	}

	// ========================================================================
	// SONGWRITING
	// ========================================================================

	/// <summary>A writing session with one act. Their own writing ability sets the ceiling.</summary>
	public bool WriteSongs(SimulatedArtist artist, out string message) {
		if (artist == null) { message = "No act selected."; return false; }
		if (!RequireHome(out message)) return false;
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
	/// The material an act could cut: their own numbers and the covers/standards they already play
	/// (their repertoire), plus anything still unrecorded the player wrote for them, plus the two things
	/// a label brings to the act -- a commissioned professional song and a fresh cover from the catalog.
	/// A period act mostly records what it already has; commissioning and fresh covers are there for the
	/// act that needs a song.
	/// </summary>
	public IReadOnlyList<MaterialChoice> MaterialOptionsFor(SimulatedArtist artist) {
		var options = new List<MaterialChoice>();
		if (artist == null) return options;

		// A number the act has already cut is spent -- it drops out of the material list so it can't be re-recorded.
		foreach (RepertoireItem item in RepertoireFor(artist.artistId)) {
			if (item.Recorded) continue;
			options.Add(
				item.IsOriginal
					? new MaterialChoice { Kind = MaterialKind.Original, Title = item.Title, Detail = "their own" }
					: item.IsCommission
						? new MaterialChoice { Kind = MaterialKind.Commission, Title = item.Title, SongId = item.SongId, Detail = "commissioned" }
						: new MaterialChoice { Kind = MaterialKind.LiveCover, Title = item.Title, SongId = item.SongId, Detail = item.SourceTag });
		}
		foreach (Song song in songs.Where(s => !s.Recorded && s.ArtistId == artist.artistId))
			options.Add(new MaterialChoice { Kind = MaterialKind.Original, Title = song.Title, WrittenSong = song, Detail = "their own" });

		// Neither commissioning nor covering is a blind "cut something" any more: the player commissions a
		// specific professional song (CommissionSong) or teaches a specific catalog cover (TeachCover), and
		// both arrive in the act's repertoire above, by name and with a read, before a note is cut here.
		return options;
	}

	/// <summary>
	/// The catalog covers a specific act could take on, by real title: the standards and recent hits in
	/// their genre and family, best hooks first, minus what they already play. This is the browse list
	/// the player picks from before teaching a cover -- so the song's name is on the table up front, not
	/// revealed only after the master is cut.
	/// </summary>
	public IReadOnlyList<MaterialChoice> CoverCatalogFor(SimulatedArtist artist, int max = 24) {
		var result = new List<MaterialChoice>();
		if (artist == null) return result;
		var already = new HashSet<string>(RepertoireFor(artist.artistId)
			.Where(item => !item.IsOriginal && item.SongId != null).Select(item => item.SongId));
		// Also hide covers already in rehearsal for this act, so the same song can't be queued twice.
		foreach (CoverRehearsal r in RehearsalsFor(artist.artistId)) if (r.SongId != null) already.Add(r.SongId);

		GenreFamily family = FamilyOf(artist);
		var pool = new List<SongComposition>();
		pool.AddRange(CompositionCatalogService.GetCoverableHitsForGenre(artist.primaryGenre));
		pool.AddRange(CompositionCatalogService.GetStandardsForGenre(artist.primaryGenre));
		pool.AddRange(CompositionCatalogService.GetCoverableHitsForFamily(family));
		pool.AddRange(CompositionCatalogService.GetStandardsForFamily(family));

		// Order by hook AND fit, not hook alone. A song squarely in the act's own genre outranks a
		// family-adjacent one with a marginally catchier hook, so a soul act is offered soul sides first
		// and only reaches into the wider Rhythm-and-Soul songbook when nothing closer is worth cutting.
		foreach (SongComposition song in pool.Where(s => s != null && !already.Contains(s.songId))
			.GroupBy(s => s.songId).Select(group => group.First())
			.OrderByDescending(s => s.commercialHook * CoverFit(artist, family, s)).Take(max))
			result.Add(new MaterialChoice {
				Kind = MaterialKind.LiveCover, Title = song.title, SongId = song.songId,
				Detail = song.isStandard ? "standard" : "cover",
				Genre = song.primaryGenre, Hook = song.commercialHook, HasSong = true
			});
		return result;
	}

	/// <summary>How well a catalog song fits the act cutting it: a full weight for a song in the act's own
	/// genre, less for one merely in the same family, least for anything further out. This is what turns
	/// "highest hook wins" into "highest hook that actually suits them".</summary>
	private static float CoverFit(SimulatedArtist artist, GenreFamily family, SongComposition song) {
		if (song.primaryGenre == artist.primaryGenre) return 1f;
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		GenreFamily songFamily = GenreCatalog.Get(GenreCatalog.MapLegacy(song.primaryGenre, year)).Family;
		return songFamily == family ? 0.72f : 0.45f;
	}

	/// <summary>How many days this act would take to work a new cover into their set: fewer the more capable
	/// they are (musicianship / group cohesion / studio craft), between <see cref="MinCoverLearnDays"/> and
	/// <see cref="MaxCoverLearnDays"/>.</summary>
	public int EstimateCoverLearnDays(SimulatedArtist artist) {
		if (artist == null) return MaxCoverLearnDays;
		float ability = Mathf.Clamp(
			artist.musicianship * 0.45f + artist.groupCohesion * 0.30f + artist.studioPerformance * 0.25f, 0f, 1f);
		return Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(MaxCoverLearnDays, MinCoverLearnDays, ability)),
			MinCoverLearnDays, MaxCoverLearnDays);
	}

	/// <summary>Whether this act already has a cover in rehearsal (only one at a time). Commissions are a
	/// separate track, so a commission in flight does not close the cover catalog and vice versa.</summary>
	public bool IsRehearsing(string artistId) => rehearsals.Any(r => r.ArtistId == artistId && !r.IsCommission);

	/// <summary>Whether this act already has a commission out with a writer (only one at a time).</summary>
	public bool IsCommissioning(string artistId) => rehearsals.Any(r => r.ArtistId == artistId && r.IsCommission);

	/// <summary>Covers and commissions this act is currently working up / awaiting (not yet in the set).</summary>
	public IEnumerable<CoverRehearsal> RehearsalsFor(string artistId) =>
		rehearsals.Where(r => r.ArtistId == artistId);

	/// <summary>
	/// Puts a brief to a professional writer for this act. A short setup at the desk plus a writer's fee kicks
	/// it off; the song is written to order and delivered into the act's set after a week -- see
	/// <see cref="ProcessCoverRehearsals"/> -- with its real title and a read on it, so the player sees exactly
	/// what they are cutting before the studio. One commission out at a time per act.
	/// </summary>
	public bool CommissionSong(SimulatedArtist artist, out string message) {
		message = "";
		if (artist == null || artist.labelId != Label?.labelId) { message = "That act isn't on your roster."; return false; }
		if (!RequireHome(out message)) return false;
		if (IsCommissioning(artist.artistId)) { message = $"A writer is already working on something for {artist.stageName}."; return false; }

		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		SongComposition song = PickCommissionSong(artist.primaryGenre, year);
		if (song == null) { message = $"No writer would take a {GenreNameFormatter.Format(artist.primaryGenre)} commission right now."; return false; }
		if (Label.cashReserves < CommissionFee) { message = $"You're ${CommissionFee - Label.cashReserves:N0} short of the ${CommissionFee:N0} writer's fee."; return false; }
		if (!Require(CommissionHours, out message)) return false;

		Spend(CommissionHours);
		Label.cashReserves -= CommissionFee;
		Label.monthlyExpenses += CommissionFee;
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		rehearsals.Add(new CoverRehearsal {
			ArtistId = artist.artistId, SongId = song.songId, Title = song.title,
			SourceTag = "commissioned", Genre = song.primaryGenre,
			ReadHook = song.commercialHook, ReadQuality = song.GetCraftScore(),
			Started = today, ReadyDate = today.AddDays(CommissionDeliveryDays), IsCommission = true
		});
		Note($"Commissioned \"{song.title}\" for {artist.stageName} (${CommissionFee:N0}, ~{CommissionDeliveryDays} days).");
		message = $"A writer will have \"{song.title}\" for {artist.stageName} in about {CommissionDeliveryDays} days.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>The professional song a commission would deliver for a genre: a weighted pick over the better
	/// craft in the pool. Player-turn-local (GD.Rand), never called from the AI weekly tick, so it does not
	/// touch the economy's deterministic RNG schedule -- same discipline as the rest of the desk.</summary>
	private static SongComposition PickCommissionSong(Genre genre, int year) {
		var pool = CompositionCatalogService.GetProfessionalForGenre(genre);
		if (pool == null || pool.Count == 0) return null;
		var top = pool.OrderByDescending(s => s.GetCraftScore() + s.GetFamiliarityForYear(year) * 0.2f)
			.Take(Mathf.Min(8, pool.Count)).ToList();
		return top[(int)(GD.Randf() * top.Count) % top.Count];
	}

	/// <summary>
	/// Starts an act working a specific catalog cover into their set. A short setup at the desk kicks it off;
	/// the act then rehearses it on their own for several days (faster the more capable they are) and it lands
	/// in their repertoire on its own -- see <see cref="ProcessCoverRehearsals"/> -- even while you're on the road.
	/// One cover in rehearsal per act at a time.
	/// </summary>
	public bool TeachCover(SimulatedArtist artist, string songId, out string message) {
		if (artist == null || artist.labelId != Label?.labelId) { message = "That act isn't on your roster."; return false; }
		if (!RequireHome(out message)) return false;
		SongComposition song = CompositionCatalogService.GetSong(songId);
		if (song == null) { message = "No such song in the catalog."; return false; }
		if (!repertoire.TryGetValue(artist.artistId, out List<RepertoireItem> set)) {
			set = new List<RepertoireItem>();
			repertoire[artist.artistId] = set;
		}
		if (set.Any(item => item.SongId == songId)) { message = $"{artist.stageName} already plays \"{song.title}\"."; return false; }
		if (rehearsals.Any(r => r.ArtistId == artist.artistId && r.SongId == songId)) {
			message = $"{artist.stageName} is already working \"{song.title}\" up."; return false;
		}
		if (IsRehearsing(artist.artistId)) {
			message = $"{artist.stageName} is already rehearsing a cover -- let them finish it first."; return false;
		}
		if (!Require(TeachHours, out message)) return false;

		Spend(TeachHours);
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int days = EstimateCoverLearnDays(artist);
		rehearsals.Add(new CoverRehearsal {
			ArtistId = artist.artistId, SongId = song.songId, Title = song.title,
			SourceTag = song.isStandard ? "standard" : "cover", Genre = song.primaryGenre,
			ReadHook = song.commercialHook, ReadQuality = song.GetCraftScore(),
			Started = today, ReadyDate = today.AddDays(days)
		});
		Note($"{artist.stageName} started working \"{song.title}\" up (~{days} days).");
		message = $"{artist.stageName} will have \"{song.title}\" in about {days} day{(days == 1 ? "" : "s")}.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Lands any cover whose rehearsal has come due into the act's set. Runs daily, so a cover finishes
	/// on its own even while the player is away.</summary>
	private void ProcessCoverRehearsals(GameDate date) {
		foreach (CoverRehearsal r in rehearsals.Where(entry => entry.ReadyDate <= date).ToList()) {
			rehearsals.Remove(r);
			// If the act already picked it up somehow, don't double-add.
			if (!repertoire.TryGetValue(r.ArtistId, out List<RepertoireItem> set)) {
				set = new List<RepertoireItem>();
				repertoire[r.ArtistId] = set;
			}
			if (set.Any(item => item.SongId == r.SongId)) continue;
			set.Add(new RepertoireItem {
				Title = r.Title, SourceTag = r.SourceTag, IsOriginal = false, SongId = r.SongId,
				IsCommission = r.IsCommission,
				Genre = r.Genre, ReadHook = r.ReadHook, ReadQuality = r.ReadQuality
			});
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(r.ArtistId);
			Note(r.IsCommission
				? $"The writer delivered \"{r.Title}\" for {artist?.stageName ?? "the act"} — ready to record."
				: $"{artist?.stageName ?? "The act"} has \"{r.Title}\" in the set now.");
		}
	}

	// Session length in hours: a short date to a full day. More hours over fewer songs = more takes.
	public const int MinSessionHours = 3;
	public const int MaxSessionHours = 8;

	/// <summary>The per-hour ranges the period rooms charged. The realized rate lands within by how
	/// good the local studios are -- a strong studio town's rooms cost (and cut) at the top of the band.</summary>
	public static (float Low, float High) StudioRateRange(StudioTier tier) => tier switch {
		StudioTier.Budget => (10f, 20f),
		StudioTier.Mid => (20f, 40f),
		StudioTier.Top => (50f, 65f),
		_ => (20f, 40f)
	};

	public static string StudioTierName(StudioTier tier) => tier switch {
		StudioTier.Budget => "Budget room", StudioTier.Top => "Top room", _ => "Mid room"
	};

	/// <summary>0 (weak local studios) .. 1 (a signature-sound town), from the region's music industry.</summary>
	private float StudioQualityT() {
		MarketRegion region = ChartManager.Instance?.GetRegionById(Label?.homeRegion);
		float mod = region != null ? ChartSimulator.GetStudioQualityModifier(region) : 0.7f;
		return Mathf.Clamp((mod - 0.5f) / 0.65f, 0f, 1f);
	}

	public float StudioHourlyRate(StudioTier tier) {
		(float low, float high) = StudioRateRange(tier);
		return Mathf.Round(Mathf.Lerp(low, high, StudioQualityT()));
	}

	public float SessionCost(StudioTier tier, int hours) =>
		StudioHourlyRate(tier) * Mathf.Clamp(hours, MinSessionHours, MaxSessionHours);

	/// <summary>
	/// Books studio time and cuts takes -- nothing is a master yet. You pick the songs (an A and a B
	/// side, or a whole session of sides), the room, and the hours; the session lays down several takes
	/// of each song, more when you give it more time over fewer songs. The money is spent now; you then
	/// keep the best take of each and print them with <see cref="PrintSession"/>.
	/// </summary>
	public bool StartSession(SimulatedArtist artist, IReadOnlyList<MaterialChoice> choices, StudioTier tier, int hours, out string message) {
		if (pendingSession != null) { message = "There's already a session on the console -- print or scrap it first."; return false; }
		if (!RequireHome(out message)) return false;
		if (artist == null || artist.labelId != Label?.labelId) { message = "That act isn't on your roster."; return false; }
		List<MaterialChoice> cutting = (choices ?? Array.Empty<MaterialChoice>()).Where(c => c != null).ToList();
		if (cutting.Count == 0) { message = "Pick at least one song to cut."; return false; }
		hours = Mathf.Clamp(hours, MinSessionHours, MaxSessionHours);
		if (!Require(hours, out message)) return false;

		float cost = SessionCost(tier, hours);
		// A scrappy label spends what it has -- cash on hand is the only gate. Next month's overhead
		// is next month's problem.
		if (Label.cashReserves < cost) {
			message = $"You're ${cost - Label.cashReserves:N0} short of a ${cost:N0} session.";
			return false;
		}

		Spend(hours);
		Label.cashReserves -= cost;
		Label.monthlyExpenses += cost;
		artist.unrecoupedAdvance += cost;

		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		float t = StudioQualityT();
		// Takes are what the hours buy: time spread across songs. Fewer songs, more takes each.
		int takes = Mathf.Clamp(Mathf.RoundToInt((float)hours / cutting.Count), 1, 4);

		var session = new PendingSession { ArtistId = artist.artistId, Tier = tier, Hours = hours, Cost = cost, Date = today };
		foreach (MaterialChoice choice in cutting) {
			var cut = new SessionCut { Choice = choice };
			for (int i = 1; i <= takes; i++) cut.Takes.Add(RollTake(i, artist, choice, tier, t));
			cut.KeptTake = BestTakeIndex(cut.Takes);
			session.Cuts.Add(cut);
		}
		pendingSession = session;

		Note($"Booked {StudioTierName(tier)} for {hours}h with {artist.stageName}: {cutting.Count} song(s), {takes} take(s) each, ${cost:N0}.");
		message = $"{takes} take(s) of {cutting.Count} song(s) in the can -- keep the ones you want.";
		Changed?.Invoke();
		return true;
	}

	private static SessionTake RollTake(int number, SimulatedArtist artist, MaterialChoice choice, StudioTier tier, float studioT) {
		// The room sets a production ceiling; the act's ear sets the hook; each take rolls around them.
		float tierProd = tier switch { StudioTier.Budget => 0.30f, StudioTier.Top => 0.62f, _ => 0.46f };
		float basePerformance = choice.WrittenSong != null ? choice.WrittenSong.Hook
			: Mathf.Clamp(artist.studioPerformance * 0.5f + artist.livePerformance * 0.2f + 0.15f, 0f, 1f);
		return new SessionTake {
			Number = number,
			Hook = Mathf.Clamp(basePerformance * 0.75f + studioT * 0.10f + (float)GD.RandRange(-0.10, 0.14), 0f, 1f),
			Production = Mathf.Clamp(tierProd + artist.studioPerformance * 0.22f + studioT * 0.12f + (float)GD.RandRange(-0.08, 0.10), 0f, 1f)
		};
	}

	private static int BestTakeIndex(List<SessionTake> takes) {
		int best = 0;
		for (int i = 1; i < takes.Count; i++) if (takes[i].Overall > takes[best].Overall) best = i;
		return best;
	}

	/// <summary>Chooses which take of a song to keep before printing.</summary>
	public void KeepTake(int cutIndex, int takeIndex) {
		if (pendingSession == null) return;
		if (cutIndex < 0 || cutIndex >= pendingSession.Cuts.Count) return;
		SessionCut cut = pendingSession.Cuts[cutIndex];
		cut.KeptTake = Mathf.Clamp(takeIndex, 0, cut.Takes.Count - 1);
		Changed?.Invoke();
	}

	/// <summary>
	/// Prints the kept take of each song in the session to a finished master. Material is resolved and
	/// stamped here (the same path the AI uses), so each master carries a real song identity. The tape
	/// was paid for at booking; this just commits the takes. Clears the console.
	/// </summary>
	public bool PrintSession(out string message) {
		if (pendingSession == null) { message = "No session to print."; return false; }
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(pendingSession.ArtistId);
		if (artist == null) { pendingSession = null; message = "The act is gone; session scrapped."; Changed?.Invoke(); return false; }

		int year = pendingSession.Date.year;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		float perMasterCost = pendingSession.Cost / Mathf.Max(1, pendingSession.Cuts.Count);
		int printed = 0;
		foreach (SessionCut cut in pendingSession.Cuts) {
			SessionTake take = cut.Takes[Mathf.Clamp(cut.KeptTake, 0, cut.Takes.Count - 1)];
			if (PrintMaster(artist, cut.Choice, take, perMasterCost, pendingSession.Date, year, week)) printed++;
		}
		Note($"Printed {printed} master(s) from the {StudioTierName(pendingSession.Tier)} date with {artist.stageName}.");
		pendingSession = null;
		message = $"{printed} master(s) on the shelf.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Scraps the session on the console without printing. The money is already spent.</summary>
	public void ScrapSession() {
		if (pendingSession == null) return;
		Note("Walked out of the session with nothing.");
		pendingSession = null;
		Changed?.Invoke();
	}

	/// <summary>Builds one finished master from a kept take and its material.</summary>
	private bool PrintMaster(SimulatedArtist artist, MaterialChoice choice, SessionTake take, float cost, GameDate date, int year, int week) {
		var record = new Record {
			recordId = $"player_{++counter}",
			labelId = Label.labelId,
			title = choice.Title,
			artistId = artist.artistId,
			artistName = artist.stageName,
			format = ReleaseFormat.Single,
			isPlayerOwned = true,
			primaryGenre = artist.primaryGenre,
			secondaryGenre = artist.secondaryGenre,
			controversy = (float)GD.RandRange(0f, 0.2f),
			hookStrength = take.Hook,
			productionQuality = take.Production,
			originality = choice.WrittenSong != null ? choice.WrittenSong.Originality
				: Mathf.Clamp((artist.members.Count > 0 ? artist.members[0].creativity : 0.4f) * 0.6f + 0.2f, 0f, 1f),
			danceability = choice.WrittenSong != null ? choice.WrittenSong.Danceability : (float)GD.RandRange(0.3, 0.95)
		};

		// Stamp the song identity and blend the take toward the material (same path as the AI).
		SelectedSongMaterial material = ResolveMaterial(choice, artist, record, year, week);
		if (material?.Song != null) {
			record.title = material.Song.title;
			SongMaterialApplicationService.Apply(record, material, Label, artist);
		}
		if (choice.WrittenSong != null) { choice.WrittenSong.Recorded = true; choice.WrittenSong.RecordedId = record.recordId; }
		MarkRepertoireRecorded(artist.artistId, choice, record.recordId);

		masters.Add(new Master {
			Record = record, ArtistId = artist.artistId, SongTitle = record.title,
			ProductionCost = cost, Cut = date
		});
		return true;
	}

	/// <summary>Marks the act's repertoire number this master came from as recorded, so it drops out of the
	/// studio's material list and shows in the set as cut (linked to its record). A live cover matches by songId;
	/// a live-set original by title. A commissioned/fresh song has no repertoire entry and is a no-op.</summary>
	private void MarkRepertoireRecorded(string artistId, MaterialChoice choice, string recordId) {
		if (!repertoire.TryGetValue(artistId, out List<RepertoireItem> set)) return;
		RepertoireItem match = choice.Kind is MaterialKind.LiveCover or MaterialKind.Commission && choice.SongId != null
			? set.FirstOrDefault(item => !item.Recorded && !item.IsOriginal && item.SongId == choice.SongId)
			: choice.WrittenSong == null && choice.Kind == MaterialKind.Original
				? set.FirstOrDefault(item => !item.Recorded && item.IsOriginal && item.Title == choice.Title)
				: null;
		if (match == null) return;
		match.Recorded = true;
		match.RecordedId = recordId;
	}

	/// <summary>Turns a player's material choice into a concrete <see cref="SelectedSongMaterial"/>.</summary>
	private SelectedSongMaterial ResolveMaterial(MaterialChoice choice, SimulatedArtist artist, Record record, int year, int week) {
		switch (choice.Kind) {
			case MaterialKind.LiveCover:
				SongComposition song = CompositionCatalogService.GetSong(choice.SongId);
				return song != null
					? SongMaterialSelectionService.BuildCoverForSong(artist, record, song, record.primaryGenre, year)
					: null;
			case MaterialKind.Commission:
				// A delivered commission is a specific song the player has already seen. Record that exact
				// composition as professional material, rather than re-sampling a fresh one at the studio.
				SongComposition commissioned = choice.SongId != null ? CompositionCatalogService.GetSong(choice.SongId) : null;
				return commissioned != null
					? SongMaterialSelectionService.BuildProfessionalForSong(Label.tier, artist, record, commissioned, record.primaryGenre, year)
					: SongMaterialSelectionService.ChooseMaterial(Label, artist, record, record.primaryGenre, year, week,
						SongMaterialSource.ExternalProfessional);
			case MaterialKind.FreshStandard:
				return SongMaterialSelectionService.ChooseMaterial(Label, artist, record, record.primaryGenre, year, week,
					SongMaterialSource.CoverStandard);
			case MaterialKind.FreshHit:
				return SongMaterialSelectionService.ChooseMaterial(Label, artist, record, record.primaryGenre, year, week,
					SongMaterialSource.CoverRecentHit);
			default: // Original -- one of the act's own numbers (live-set original or a written song).
				return SongMaterialSelectionService.ChooseMaterial(Label, artist, record, record.primaryGenre, year, week,
					SongMaterialSource.ArtistWritten);
		}
	}

	// ========================================================================
	// DISTRIBUTION
	// ========================================================================

	/// <summary>
	/// Orders a pressing run of one single from a plant. The player pays the plant up front -- vinyl by
	/// the unit over a 500 minimum, a lacquer setup, and a little for sleeves and freight -- and takes
	/// delivery of the boxes. Nothing of that single sells until there is stock here to move. Because the
	/// vinyl is paid for now, no manufacturing cost is taken again when the records sell.
	/// </summary>
	/// <summary>
	/// Orders a pressing run of one single from a plant, from the office. The player pays up front, but
	/// the boxes DON'T teleport in: a 1960 plant takes weeks -- mail the master tape out, cut and plate
	/// lacquers, wait in the plant's queue (longer in the holiday build-up), and ship the boxes back. The
	/// run lands in the office inventory on its arrival day (see <see cref="DeliverArrivedPressings"/>).
	/// </summary>
	public bool OrderPressing(string recordId, int quantity, out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (!RequireHome(out message)) return false;
		if (string.IsNullOrEmpty(recordId)) { message = "No single selected."; return false; }
		if (quantity < PressMinimumOrder) { message = $"The plant won't run under {PressMinimumOrder}."; return false; }

		float cost = PressingCost(quantity);
		if (Label.cashReserves < cost) {
			message = $"You're ${cost - Label.cashReserves:N0} short of a ${cost:N0} run.";
			return false;
		}

		Label.cashReserves -= cost;
		Label.monthlyExpenses += cost;
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int lead = RollPressLeadDays(today);
		GameDate arrives = today.AddDays(lead);
		pressOrders.Add(new PressOrder { RecordId = recordId, Quantity = quantity, Cost = cost, Ordered = today, Arrives = arrives });

		string title = TitleForRecord(recordId);
		Note($"Ordered {quantity:N0} of \"{title}\" for ${cost:N0} -- the plant quotes {lead} days, in by {arrives.ToHeadlineString()}.");
		message = $"Run ordered -- about {lead} days at the plant.";
		Changed?.Invoke();
		return true;
	}

	private int RollPressLeadDays(GameDate today) {
		int mail = (int)GD.RandRange(PressMailDaysMin, PressMailDaysMax);
		int plating = (int)GD.RandRange(PressPlatingDaysMin, PressPlatingDaysMax);
		int queue = (int)GD.RandRange(PressQueueDaysMin, PressQueueDaysMax);
		int ship = (int)GD.RandRange(PressShipDaysMin, PressShipDaysMax);
		// Plants back up when the whole market is pressing hard -- the holiday build-up. The seasonal
		// single-demand multiplier stands in for how deep the plant's queue is that month.
		float backlog = MarketSeasonality.Enabled
			? Mathf.Clamp(MarketSeasonality.GetSingleSalesMultiplier(today.year, today.month, liveTick: false), 0.8f, 1.8f)
			: 1f;
		return mail + plating + Mathf.RoundToInt(queue * backlog) + ship;
	}

	/// <summary>Delivers any pressing runs that have arrived into the office inventory. Called each day.</summary>
	private void DeliverArrivedPressings(GameDate date) {
		foreach (PressOrder order in pressOrders.Where(o => o.Arrives <= date).ToList()) {
			pressOrders.Remove(order);
			if (!inventory.TryGetValue(order.RecordId, out PressStock stock)) { stock = new PressStock(); inventory[order.RecordId] = stock; }
			stock.Remaining += order.Quantity;
			stock.TotalPressed += order.Quantity;
			stock.TotalSpent += order.Cost;
			Note($"The pressing plant delivered {order.Quantity:N0} of \"{TitleForRecord(order.RecordId)}\".");
		}
	}

	/// <summary>Pressing runs still at the plant, soonest first.</summary>
	public IEnumerable<(string Title, int Quantity, GameDate Arrives)> PendingPressings() =>
		pressOrders.OrderBy(o => o.Arrives).Select(o => (TitleForRecord(o.RecordId), o.Quantity, o.Arrives));

	/// <summary>
	/// A day working the town you're standing in: carrying the boxes round its shops and juke operators
	/// and leaving your pressed singles on consignment. Draws from the office inventory you brought stock
	/// from. The records then sell here on their own, day by day, decaying until you drive back to restock.
	/// You have to be physically in the town to do it -- that's the point of the road.
	/// </summary>
	public bool WorkThisTown(string recordId, int quantity, out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		MarketCity city = CurrentCity;
		if (city == null) { message = "Nowhere resolved."; return false; }
		if (string.IsNullOrEmpty(recordId)) { message = "Pick a single to leave here."; return false; }
		PressStock stock = StockFor(recordId);
		if (stock == null || stock.Remaining <= 0) { message = "None of that pressed on hand -- order a run and let it come in first."; return false; }
		int place = Mathf.Clamp(quantity, 1, stock.Remaining);
		if (!Require(DistributionHours, out message)) return false;

		Spend(DistributionHours);
		// You're here, so first settle up whatever the shops have been holding for you since your last pass.
		CollectFromTown(city.cityId);

		stock.Remaining -= place;
		if (!consignment.TryGetValue(city.cityId, out var lots)) { lots = new(StringComparer.Ordinal); consignment[city.cityId] = lots; }
		if (!lots.TryGetValue(recordId, out var lot)) { lot = new ConsignmentLot(); lots[recordId] = lot; }
		lot.Remaining += place;
		lot.Placed = lot.Remaining;   // the "fresh" size the daily slice is taken off
		lot.DaysSinceRestock = 0;     // resets the town's appetite
		workedCities.Add(city.cityId);

		Note($"Worked {city.name} -- left {place:N0} of \"{TitleForRecord(recordId)}\" on consignment.");
		message = $"{place:N0} of \"{TitleForRecord(recordId)}\" out in {city.name}.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>How many of a single a town's shops will comfortably hold: scaled by how many record stores
	/// it has, floored and capped. This is only the default the picker starts on -- the player sets the
	/// actual number to leave.</summary>
	public int SuggestedPlacement(MarketCity city) =>
		Mathf.Clamp((city?.distribution?.recordStoreCount ?? 20) * 6, 150, 1500);

	/// <summary>Pressed singles sitting in the office, ready to carry out to a town. (recordId, title, on hand).</summary>
	public IEnumerable<(string RecordId, string Title, int OnHand)> PressedSinglesOnHand() {
		foreach (var kv in inventory)
			if (kv.Value.Remaining > 0) yield return (kv.Key, TitleForRecord(kv.Key), kv.Value.Remaining);
	}

	/// <summary>Consignment on hand in a town, for the DISTRIBUTION readout.</summary>
	public IEnumerable<(string CityName, string Title, int Remaining)> TownStock() {
		foreach (var (cityId, lots) in consignment) {
			string cityName = DistanceModel.GetCityById(cityId)?.name ?? cityId;
			foreach (var (recordId, lot) in lots)
				if (lot.Remaining > 0) yield return (cityName, TitleForRecord(recordId), lot.Remaining);
		}
	}

	// ========================================================================
	// ON THE ROAD -- where you physically are, and getting there
	// ========================================================================

	public string CurrentCityId => currentCityId ?? Label?.homeCityId;
	public MarketCity CurrentCity => DistanceModel.GetCityById(CurrentCityId);
	public bool AtHome => Label != null && (string.IsNullOrEmpty(currentCityId) || currentCityId == Label.homeCityId);

	/// <summary>You can drive between towns in the same region or ones next to it; farther is a wholesale job.</summary>
	private static bool CanReach(MarketCity from, MarketCity to) =>
		from != null && to != null && (to.parentRegionId == from.parentRegionId
			|| DistanceModel.GetAdjacentRegions(from.parentRegionId).Contains(to.parentRegionId));

	/// <summary>One-way drive cost between two towns: hours off real road miles, gas one way.</summary>
	public (int Hours, float Gas) DriveQuote(string fromCityId, string toCityId) {
		float miles = DistanceModel.GetRoadMilesBetween(fromCityId, toCityId);
		return (Mathf.Max(1, Mathf.RoundToInt(miles / DriveMph)), Mathf.Round(miles * GasPerMile));
	}

	/// <summary>Towns you could drive to from where you are now (your region + the ones next to it).</summary>
	public IEnumerable<MarketCity> ReachableCities() {
		MarketCity here = CurrentCity;
		if (here == null) yield break;
		foreach (MarketCity city in DistanceModel.GetCities())
			if (city.cityId != here.cityId && CanReach(here, city))
				yield return city;
	}

	/// <summary>Drives to a town. Costs the one-way time and gas; you're now physically there, so the
	/// office and studio are out of reach until you drive back. Must fit in the daylight you have left.</summary>
	public bool DriveTo(string cityId, out string message) => Travel(cityId, out message);

	/// <summary>Drives back to the home office from wherever you are.</summary>
	public bool DriveHome(out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (AtHome) { message = "You're already at the office."; return false; }
		return Travel(Label.homeCityId, out message);
	}

	private bool Travel(string toCityId, out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		MarketCity here = CurrentCity, dest = DistanceModel.GetCityById(toCityId);
		if (dest == null) { message = "No such town."; return false; }
		if (dest.cityId == CurrentCityId) { message = $"You're already in {dest.name}."; return false; }
		bool goingHome = dest.cityId == Label.homeCityId;
		if (!goingHome && !CanReach(here, dest)) {
			message = $"{dest.name} is too far to drive to from {here?.name} -- you'd need a wholesale house out there.";
			return false;
		}
		(int hours, float gas) = DriveQuote(CurrentCityId, dest.cityId);
		if (!TimeManager.Instance?.CanAffordHours(hours, allowOvertime: true) ?? true) {
			message = $"Not enough daylight to make {dest.name} today ({hours}h) -- rest and start fresh.";
			return false;
		}
		if (Label.cashReserves < gas) { message = $"You're ${gas - Label.cashReserves:N0} short of the ${gas:N0} gas."; return false; }

		Spend(hours);
		if (gas > 0f) { Label.cashReserves -= gas; Label.monthlyExpenses += gas; }
		currentCityId = dest.cityId;
		CollectFromTown(dest.cityId); // show up and the shops settle what they've been holding
		Note($"Drove to {dest.name} ({hours}h, ${gas:N0} gas).");
		message = goingHome ? "Back at the office." : $"You're in {dest.name}.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Office/studio work needs the office. On the road you can only scout, work the town, and drive.</summary>
	private bool RequireHome(out string message) {
		if (AtHome) { message = string.Empty; return true; }
		message = "You're on the road -- drive back to the office for that.";
		return false;
	}

	private string TitleForRecord(string recordId) {
		Master master = masters.FirstOrDefault(m => m.Record?.recordId == recordId);
		if (master != null) return master.SongTitle;
		RecordRuntimeData released = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId);
		return released?.baseRecord?.title ?? recordId;
	}

	/// <summary>
	/// Singles the player can order a pressing run for. A run is a physical 45 -- one disc, both sides
	/// together -- so this lists whole singles, never a lone side: scheduled releases (keyed to their
	/// A-side, both sides named) and records already in the market. Pressing a raw shelf master would
	/// mean pressing an A-side and a B-side as if they were two records, which is what they are not.
	/// </summary>
	public IEnumerable<(string RecordId, string Title, bool InMarket)> PressableSingles() {
		foreach (PlannedRelease release in planned)
			yield return (release.Master.Record.recordId,
				release.BSide != null ? $"{release.Master.SongTitle} b/w {release.BSide.SongTitle}" : release.Master.SongTitle,
				false);
		foreach (RecordRuntimeData record in ReleasedRecords)
			yield return (record.baseRecord.recordId, record.baseRecord.title, true);
	}

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

	/// <summary>
	/// Pairs two shelf masters into a single -- an A-side (the plug side that chases the chart) and a
	/// different B-side (the flip that ships on the same disc). This is the FIRST half of putting a record
	/// out: once assembled, the single can be sent to the pressing plant and quoted a real turnaround, and
	/// only then, knowing when the vinyl lands, does the player set a release date (<see cref="SetReleaseDate"/>).
	/// Costs no hours -- it is just deciding the coupling; the pressing and the scheduling are where the
	/// work and money go.
	/// </summary>
	public bool AssembleSingle(Master aSide, Master bSide, out string message) {
		if (aSide == null || aSide.Scheduled || aSide.Released) { message = "Pick an A-side."; return false; }
		if (bSide == null || bSide.Scheduled || bSide.Released) { message = "Pick a B-side."; return false; }
		if (ReferenceEquals(aSide, bSide)) { message = "The two sides have to be different cuts."; return false; }
		if (Label == null) { message = "You don't have a label yet."; return false; }

		aSide.Scheduled = true;
		bSide.Scheduled = true;
		planned.Add(new PlannedRelease { Master = aSide, BSide = bSide, Dated = false, MarketingBudget = 0f });

		Note($"Cut a single: \"{aSide.SongTitle}\" b/w \"{bSide.SongTitle}\" -- ready for the plant.");
		message = $"\"{aSide.SongTitle}\" b/w \"{bSide.SongTitle}\" is ready to press.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>
	/// Sets the release date on an assembled single -- the SECOND half of putting a record out, done once
	/// the pressing plant has quoted a turnaround so the date can be set for after the vinyl arrives. The
	/// A-side chases the chart; the B-side rides along.
	/// </summary>
	public bool SetReleaseDate(PlannedRelease single, int daysOut, float marketingBudget, out string message) {
		if (single == null || !planned.Contains(single)) { message = "Pick a single to date."; return false; }
		if (single.Dated) { message = "That single already has a release date."; return false; }
		if (!RequireHome(out message)) return false;
		if (!Require(ScheduleHours, out message)) return false;
		marketingBudget = Mathf.Max(0f, marketingBudget);
		if (marketingBudget > Label.cashReserves) {
			message = $"You can't cover a ${marketingBudget:N0} campaign on ${Label.cashReserves:N0} cash.";
			return false;
		}

		Spend(ScheduleHours);
		GameDate date = (TimeManager.Instance?.CurrentDate ?? GameDate.StartDate).AddDays(Mathf.Max(1, daysOut));
		single.Dated = true;
		single.Date = date;
		single.MarketingBudget = marketingBudget;

		Note($"\"{single.Master.SongTitle}\" b/w \"{single.BSide?.SongTitle}\" scheduled for {date.ToHeadlineString()} " +
			$"with a ${marketingBudget:N0} campaign.");
		message = $"Shipping {date.ToHeadlineString()}.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Singles that have been assembled but not yet given a release date -- ready to press and date.</summary>
	public IEnumerable<PlannedRelease> UndatedSingles() => planned.Where(single => !single.Dated);

	private void OnDayStarted(GameDate date) {
		// You are not still holding a man on the line at nine the next morning.
		ActiveCall = null;
		if (Label == null) return;
		ChargeHotelIfAway();
		DeliverArrivedPressings(date);
		ProcessCoverRehearsals(date);
		foreach (PlannedRelease release in planned.Where(entry => entry.Dated && entry.Date <= date).ToList()) {
			planned.Remove(release);
			FireRelease(release, date);
		}
		ProcessTrunkDay();
		Changed?.Invoke();
	}

	/// <summary>A night away from your own bed is a motel bill.</summary>
	private void ChargeHotelIfAway() {
		if (AtHome) return;
		Label.cashReserves -= HotelNightly;
		Label.monthlyExpenses += HotelNightly;
		Note($"Motel in {CurrentCity?.name ?? "town"} -- ${HotelNightly:N0}.");
	}

	/// <summary>
	/// A day's trunk sell-through. Every town you've stocked sells a slice of what its shops are holding,
	/// smaller the longer it's been since you restocked -- so a market runs down and needs another run. The
	/// town you're standing in pays cash on the spot; a town you've left holds your cut for you to collect on
	/// your next visit (a thin daily wire aside). The vinyl was paid for at the plant, so there's no skim.
	/// </summary>
	private void ProcessTrunkDay() {
		foreach (var (cityId, lots) in consignment.Select(kv => (kv.Key, kv.Value)).ToList()) {
			bool present = cityId == CurrentCityId; // standing in the town -> cash in hand; away -> the shops owe you
			foreach (var (recordId, lot) in lots.Select(kv => (kv.Key, kv.Value)).ToList()) {
				if (lot.Remaining <= 0) { lot.DaysSinceRestock++; continue; }
				RecordRuntimeData rec = ReleasedRecords.FirstOrDefault(r => r.baseRecord.recordId == recordId);
				// A lot whose record can't be resolved still AGES. It used to `continue` before the
				// increment, which froze the lot forever: no sales and no decay clock, so stock stranded
				// in a town could never sell through and never be written off either. Player records are
				// no longer culled, so this should now only be reachable for a lot whose record a
				// pre-fix save lost outright and the load-time repair could not rebuild.
				if (rec == null) { lot.DaysSinceRestock++; continue; }
				// A day's move is a slice of the fresh lot, decaying since the last restock, scaled by how much
				// the record actually pulls -- its hook, its sound, and how many have heard of it (an unknown act
				// with a $10 campaign has near-zero awareness and trickles out), then rolled with day-to-day,
				// town-to-town luck so two towns never sell in lockstep.
				float buzz = Mathf.Clamp(rec.awareness, 0f, 1f);
				float appeal = Mathf.Clamp(rec.baseRecord.hookStrength * 0.45f + rec.baseRecord.productionQuality * 0.25f + buzz * 0.30f, 0.04f, 1f);
				float decay = Mathf.Pow(TrunkDecayPerDay, lot.DaysSinceRestock);
				float luck = (float)GD.RandRange(0.55, 1.45);
				int units = Mathf.Min(lot.Remaining, Mathf.RoundToInt(lot.Placed * TrunkDailyBaseFraction * appeal * decay * luck));
				lot.DaysSinceRestock++;
				if (units <= 0) continue;
				lot.Remaining -= units;
				BookTrunkSale(rec, units, cityId, present);
			}
		}
		WireOwedTrickle();
	}

	/// <summary>
	/// Books a day's trunk sell-through in one town. The records leave the shelves and count toward the
	/// chart (accumulated into the weekly total) whichever town they sold in -- department-store and
	/// record-shop sales are chart sales. The MONEY, though, only reaches the bank when you're there to
	/// take it: standing in the town it's cash in hand, otherwise the shops hold your cut until you drive
	/// back (with a thin daily wire in the meantime). The artist's royalty is credited on the sale either way.
	/// </summary>
	private void BookTrunkSale(RecordRuntimeData rec, int units, string cityId, bool present) {
		float gross = units * SinglePrice;
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(rec.baseRecord.artistId);
		float accrued = gross * (artist?.royaltyRate ?? 0.05f);
		// The advance comes back out of the royalty account before the act sees anything -- the trunk
		// path never recouped at all, so every dollar of advance was paid twice out of the player's
		// pocket. Mirrors the weekly settlement (CompetitorManager.CalculateLabelRevenue).
		float recouped = artist != null ? Mathf.Min(Mathf.Max(0f, artist.unrecoupedAdvance), accrued) : 0f;
		float royalty = accrued - recouped;
		if (artist != null) {
			artist.unrecoupedAdvance = Mathf.Max(0f, artist.unrecoupedAdvance - recouped);
			artist.totalRoyaltyEarnings += royalty;
		}
		float net = gross - royalty;
		// Units are NOT added to totalUnitsSold here -- the weekly settlement adds them exactly once through
		// the chart injection (FinalizeWeeklySales += TakeWeeklyTrunkUnits). Counting them here as well double-
		// counted every trunk sale. The MONEY is booked here (the settlement only monetizes wholesale units).
		rec.lifetimeLabelNet += net;
		// These units chart: swept into the weekly chart total at settlement (see TakeWeeklyTrunkUnits).
		weeklyTrunkUnits.TryGetValue(rec.baseRecord.recordId, out int running);
		weeklyTrunkUnits[rec.baseRecord.recordId] = running + units;
		// Accumulate this week's trunk business for the settlement write-up (folded in + reset at week-end).
		weeklyTrunkUnitsSold += units;
		weeklyTrunkGross += gross;
		weeklyTrunkRoyalty += royalty;
		if (present) {
			Label.cashReserves += net;
			Label.monthlyRevenue += net;
		} else {
			consignmentOwed.TryGetValue(cityId, out float owed);
			consignmentOwed[cityId] = owed + net;
			weeklyTrunkHeld += net;   // out on consignment this week; not yet at the bank
		}
		// (The royalty and recoupment were already booked above, against `accrued`. A second credit here
		// double-paid the act on every trunk sale and recouped the advance twice -- removed.)
	}

	/// <summary>A thin daily wire from every town holding money for you (bar the one you're standing in,
	/// which pays cash). Keeps a market you never return to from stranding your cut entirely.</summary>
	private void WireOwedTrickle() {
		foreach (string cityId in consignmentOwed.Keys.ToList()) {
			if (cityId == CurrentCityId) continue;
			float owed = consignmentOwed[cityId];
			if (owed <= 0f) { consignmentOwed.Remove(cityId); continue; }
			float wire = Mathf.Min(owed, Mathf.Max(1f, owed * TrunkWireFractionPerDay));
			consignmentOwed[cityId] = owed - wire;
			Label.cashReserves += wire;
			Label.monthlyRevenue += wire;
			if (consignmentOwed[cityId] <= 0.5f) consignmentOwed.Remove(cityId);
		}
	}

	/// <summary>Pockets whatever a town's shops have been holding for you. Called when you show up -- driving
	/// in, or working the town. Nothing to do for a town that owes you nothing.</summary>
	private void CollectFromTown(string cityId) {
		if (string.IsNullOrEmpty(cityId)) return;
		if (!consignmentOwed.TryGetValue(cityId, out float owed) || owed <= 0f) return;
		consignmentOwed.Remove(cityId);
		Label.cashReserves += owed;
		Label.monthlyRevenue += owed;
		Note($"Collected ${owed:N0} the shops in {DistanceModel.GetCityById(cityId)?.name ?? cityId} were holding.");
	}

	/// <summary>Pulls this record's accumulated trunk units for the week and resets the tally. Read once at
	/// weekly settlement so the units fold into the chart total exactly once.</summary>
	public int TakeWeeklyTrunkUnits(string recordId) {
		if (recordId == null || !weeklyTrunkUnits.TryGetValue(recordId, out int units)) return 0;
		weeklyTrunkUnits.Remove(recordId);
		return units;
	}

	/// <summary>This week's trunk units for a record that have sold but not yet folded into totalUnitsSold
	/// (that fold happens once, at weekly settlement). The MONEY for them is already in lifetimeLabelNet,
	/// so the readout adds these back in -- otherwise a record worked hard out of the trunk mid-week reads
	/// as impossibly high dollars-per-unit until the week settles.</summary>
	public int PendingTrunkUnits(string recordId) =>
		recordId != null && weeklyTrunkUnits.TryGetValue(recordId, out int units) ? units : 0;

	/// <summary>What each town's shops are currently holding for you, for the DISTRIBUTION readout.</summary>
	public IEnumerable<(string CityName, float Amount)> ConsignmentOwedByTown() {
		foreach (var kv in consignmentOwed)
			if (kv.Value > 0.5f) yield return (DistanceModel.GetCityById(kv.Key)?.name ?? kv.Key, kv.Value);
	}

	private const float SinglePrice = 0.89f; // historical 45 retail

	private void FireRelease(PlannedRelease release, GameDate date) {
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(release.Master.ArtistId);
		if (artist == null) { Note($"\"{release.Master.SongTitle}\" never shipped -- the act is gone."); return; }

		float budget = Mathf.Min(release.MarketingBudget, Mathf.Max(0f, Label.cashReserves));
		Label.cashReserves -= budget;
		Label.monthlyExpenses += budget;

		// The A-side goes to market and chases the chart; the B-side ships on the flip but is not
		// worked as its own record. Both come off the shelf.
		bool released = CompetitorManager.Instance?.ReleasePlayerRecord(
			Label, artist, release.Master.Record, budget, release.Master.ProductionCost, date) ?? false;
		if (!released) { Note($"\"{release.Master.SongTitle}\" failed to ship."); return; }

		release.Master.Released = true;
		masters.Remove(release.Master);
		if (release.BSide != null) {
			release.BSide.Released = true;
			masters.Remove(release.BSide);
			// It shipped on the flip -- record that so its repertoire line reads "out (B-side)" and not
			// "cut, not out yet". The B-side is never a market record of its own, so this is its only trace.
			if (release.BSide.Record?.recordId != null) shippedBSideRecordIds.Add(release.BSide.Record.recordId);
		}
		string flip = release.BSide != null ? $" b/w \"{release.BSide.SongTitle}\"" : "";
		Note($"RELEASED: \"{release.Master.SongTitle}\"{flip} by {artist.stageName} ({date.ToHeadlineString()}).");
	}

	// ========================================================================
	// SHARED
	// ========================================================================

	/// <summary>
	/// Kills time at the desk without doing work -- the day still burns. This is how you wait for the
	/// clubs to open (they don't get going until the evening) instead of being forced into an industry
	/// meet just to move the clock. Hours are clamped to what's left in the day, overtime included.
	/// </summary>
	public bool PassTime(int hours, out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		TimeManager time = TimeManager.Instance;
		if (time == null) { message = "No clock."; return false; }
		int available = time.HoursRemainingWithOvertime;
		int spend = Mathf.Clamp(hours, 0, available);
		if (spend <= 0) { message = "The day's already gone -- turn in for the night."; return false; }
		time.SpendHours(spend, allowOvertime: true);
		message = $"You let {spend} hour{(spend == 1 ? "" : "s")} go by.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Hours between now and a target hour of the day (0 if it's already past).</summary>
	public int HoursUntil(int targetHour) =>
		Mathf.Max(0, targetHour - (TimeManager.Instance?.CurrentHour ?? targetHour));

	/// <summary>Spend sub-hour time. The clock now carries a minute hand, so a 25-minute call moves the
	/// visible time by 25 minutes instead of vanishing into an accumulator the player could not see.
	/// Returns false (without moving the clock) if the action does not fit in what is left of the day.</summary>
	private bool SpendMinutes(int minutes) => TimeManager.Instance?.SpendMinutes(minutes) ?? false;

	private bool Require(int hours, out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (IsGameOver) { message = "The label has folded -- load a save to keep playing."; return false; }
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
		// Rolodex settlement lands before the books are drawn up so any payola penalty shows in this
		// week's Cash figure: expire spent advocacy, apply busts, settle the pitches you staked your
		// word on against what the records actually sold.
		ProcessRolodexWeek();
		// The wholesale channel (what cleared through stores this week), then the trunk folded on top so the
		// write-up covers the whole week's business, not only wholesale. Trunk carries no manufacturing (pressed
		// and paid up front) and no distributor skim; its full net counts as earned, and the consignment slice
		// (weeklyTrunkHeld) is earned-but-not-yet-banked, so it sits alongside wholesale credit deferral.
		long wholesaleUnits = ReleasedRecords.Sum(record => (long)record.regionalData.Values.Sum(data => Mathf.Max(0, data.unitsSoldThisWeek)));
		float trunkNet = weeklyTrunkGross - weeklyTrunkRoyalty;
		float cash = Label.cashReserves;
		books.Insert(0, new WeekBooks {
			Week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0,
			Date = date,
			Units = wholesaleUnits + weeklyTrunkUnitsSold,
			Gross = Label.weeklyGrossRevenue + weeklyTrunkGross,
			ManufacturingCost = Label.weeklyCogs,
			DistributionSkim = Label.weeklyDistributionSkim,
			ArtistRoyalty = Label.weeklyArtistRoyalty + weeklyTrunkRoyalty,
			Earned = Label.weeklyNetRevenue + trunkNet,
			Deferred = Label.weeklyWholesaleDeferred,
			Collected = Label.weeklyWholesaleCollected,
			TrunkHeld = weeklyTrunkHeld,
			// What the records earned this week, less what went out on credit and what the towns are still
			// holding on consignment, plus what old invoices finally paid. This is the figure that moved the
			// bank balance: wholesale net-of-deferral plus the trunk's spot-cash slice (net minus held).
			Banked = (Label.weeklyNetRevenue - Label.weeklyWholesaleDeferred + Label.weeklyWholesaleCollected)
				+ (trunkNet - weeklyTrunkHeld),
			Outstanding = Label.outstandingWholesaleReceivables,
			Cash = cash
		});
		if (books.Count > 120) books.RemoveAt(books.Count - 1);
		weeklyTrunkUnitsSold = 0;
		weeklyTrunkGross = 0f;
		weeklyTrunkRoyalty = 0f;
		weeklyTrunkHeld = 0f;
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

	// ========================================================================
	// SAVE / LOAD  (the player layer -- the AI world is restored separately by WorldStateService, world-first)
	// ========================================================================

	/// <summary>
	/// Snapshots the player's whole side of the desk into a plain, serializable object: the label, its
	/// cash and profile, the roster (by id), the songbook and repertoire, the books and the log, PLUS the
	/// full working state -- shelf masters, the release pipeline, pressed inventory, runs still at the
	/// plant, trunk consignment and what towns owe, where you are, and the loss clock -- and the player's
	/// released catalogue with its chart/regional state. The surrounding AI world is restored separately by
	/// <see cref="WorldStateService"/> (which runs first on load), so the roster and catalogue re-link
	/// against the saved world. See <see cref="SaveGameService"/>.
	/// </summary>
	public PlayerSaveData CaptureState() {
		if (Label == null) return null;
		var data = new PlayerSaveData {
			Label = LabelSaveData.From(Label),
			RosterArtists = (Label.roster ?? new List<SimulatedArtist>()).ToList(),
			Songs = songs.Select(SongSaveData.From).ToList(),
			Repertoire = repertoire.ToDictionary(kv => kv.Key,
				kv => kv.Value.Select(RepertoireSaveData.From).ToList()),
			Rehearsals = rehearsals.Select(CoverRehearsalSaveData.From).ToList(),
			ShippedBSideRecordIds = shippedBSideRecordIds.ToList(),
			Log = new List<string>(log),
			Books = books.Select(WeekBookSaveData.From).ToList(),

			// The desk's working state: the shelf, the release pipeline, the pressing plant, the trunk.
			Masters = masters.Select(MasterSaveData.From).ToList(),
			Planned = planned.Select(PlannedReleaseSaveData.From).ToList(),
			Inventory = inventory.Select(kv => new PressStockSaveData {
				RecordId = kv.Key, Remaining = kv.Value.Remaining,
				TotalPressed = kv.Value.TotalPressed, TotalSpent = kv.Value.TotalSpent
			}).ToList(),
			PressOrders = pressOrders.Select(o => new PressOrderSaveData {
				RecordId = o.RecordId, Quantity = o.Quantity, Cost = o.Cost,
				OrderedYear = o.Ordered.year, OrderedMonth = o.Ordered.month, OrderedDay = o.Ordered.day,
				ArrivesYear = o.Arrives.year, ArrivesMonth = o.Arrives.month, ArrivesDay = o.Arrives.day
			}).ToList(),
			Consignment = consignment.SelectMany(city => city.Value.Select(lot => new ConsignmentLotSaveData {
				CityId = city.Key, RecordId = lot.Key, Remaining = lot.Value.Remaining,
				Placed = lot.Value.Placed, DaysSinceRestock = lot.Value.DaysSinceRestock
			})).ToList(),
			ConsignmentOwed = new Dictionary<string, float>(consignmentOwed),
			WeeklyTrunkUnits = new Dictionary<string, int>(weeklyTrunkUnits),
			WeeklyTrunkUnitsSold = weeklyTrunkUnitsSold,
			WeeklyTrunkGross = weeklyTrunkGross,
			WeeklyTrunkRoyalty = weeklyTrunkRoyalty,
			WeeklyTrunkHeld = weeklyTrunkHeld,
			WorkedCities = workedCities.ToList(),
			CurrentCityId = currentCityId,
			Counter = counter,
			MonthsInTheRed = monthsInTheRed,

			// The player's released catalogue, with its chart/regional state (increment 2).
			ReleasedRecords = ReleasedRecords.Select(RuntimeRecordSaveData.From).ToList(),

			// Player character (Phase 1 Rolodex branch).
			ArchetypeOrdinal   = (int)Archetype,
			ExecutiveInstincts = InstinctProfile,

			// Rolodex contacts + accumulated sub-hour phone time (Phase 2).
			PhoneMinutesAccum = phoneMinutesAccum,
			Rolodex = rolodex.Select(RolodexEntrySaveData.From).ToList(),
			Advocacy = (ChartManager.Instance?.Advocacy.Active ?? (IReadOnlyList<StationAdvocacy>)Array.Empty<StationAdvocacy>())
				.Select(StationAdvocacySaveData.From).ToList(),
			StationState = CaptureStationState(),
		};
		return data;
	}

	/// <summary>
	/// Rebuilds the player layer from a snapshot. Runs after the AI world is rehydrated
	/// (<see cref="WorldStateService"/>), so roster acts re-link against the restored world. If a player
	/// label is already live (a same-session load), its fields are updated in place so the registrations
	/// with ChartManager/CompetitorManager are not duplicated; otherwise a fresh label is built and
	/// registered. A roster act that the restored world does not contain (e.g. a runtime-signed prospect)
	/// is re-injected from the save's own copy, so the roster always comes back whole.
	/// </summary>
	public bool RestoreState(PlayerSaveData data, out string message) {
		if (data?.Label == null) { message = "Nothing to load."; return false; }

		// A load lifts any game-over: you're picking the label back up from a solvent point in its history.
		IsGameOver = false;
		GameOverReason = null;
		monthsInTheRed = 0;

		// Restore player character. ExecutiveInstincts being null means a pre-feature save; in that case
		// default to TradeInsider rather than trusting the ArchetypeOrdinal default of 0.
		if (data.ExecutiveInstincts == null) {
			Archetype = FoundingArchetype.TradeInsider;
			InstinctProfile = FoundingArchetypeData.Get(FoundingArchetype.TradeInsider).Instincts;
		} else {
			Archetype = data.ArchetypeOrdinal >= 0 && data.ArchetypeOrdinal < System.Enum.GetValues(typeof(FoundingArchetype)).Length
				? (FoundingArchetype)data.ArchetypeOrdinal
				: FoundingArchetype.TradeInsider;
			InstinctProfile = data.ExecutiveInstincts;
		}

		// Restore rolodex and phone accumulator (Phase 2). Null/missing = pre-feature save → empty rolodex.
		phoneMinutesAccum = data.PhoneMinutesAccum;
		rolodex.Clear();
		rolodex.AddRange((data.Rolodex ?? new List<RolodexEntrySaveData>()).Select(s => s.ToEntry()));
		// Advocacy is rebuilt onto the freshly-constructed service (the radio panel is rebuilt on load,
		// so the service is empty at this point). Missing on a pre-feature save = nothing outstanding.
		ChartManager.Instance?.Advocacy.Restore(
			(data.Advocacy ?? new List<StationAdvocacySaveData>()).Select(a => a.ToAdvocacy()));
		RestoreStationState(data.StationState);
		ActiveCall = null;   // you are not on the phone in a loaded game
		// Same reasoning as ActiveCall: a live negotiation (new signing or renewal) is scene state,
		// not save state -- Prospect.Talk dies with the slate.Clear() below, but PendingRenewal is a
		// bare field with no owning collection to clear it for us, so it needs the explicit reset or
		// a same-session load could resume a stale renewal against the freshly-loaded world.
		PendingRenewal = null;
		maturedNotified.Clear();

		AILabel label = Label != null && Label.labelId == data.Label.labelId ? Label : new AILabel();
		bool fresh = label != Label;
		data.Label.ApplyTo(label);
		label.isPlayerOwned = true;
		DistanceModel.AssignHomeCity(label);

		// Rebuild the roster. Prefer the act already live in this world (a same-session load); otherwise put
		// our saved copy of the act back into the population (a relaunch, or a runtime-signed act this fresh
		// world never generated). Either way the act is re-homed on the player label.
		label.roster ??= new List<SimulatedArtist>();
		label.roster.Clear();
		var savedArtists = (data.RosterArtists ?? new List<SimulatedArtist>())
			.Where(artist => artist != null && !string.IsNullOrEmpty(artist.artistId))
			.GroupBy(artist => artist.artistId).ToDictionary(group => group.Key, group => group.First());
		int missing = 0;
		foreach (string artistId in data.Label.RosterArtistIds) {
			SimulatedArtist artist = ArtistManager.Instance?.GetArtist(artistId);
			if (artist == null && savedArtists.TryGetValue(artistId, out SimulatedArtist saved)) {
				artist = saved;
				ArtistManager.Instance?.RestoreArtist(artist);
			}
			if (artist == null) { missing++; continue; }
			artist.labelId = label.labelId;
			artist.isPlayerOwned = true;
			label.roster.Add(artist);
		}

		if (fresh) Label = label;
		// Always (re)register: the full-world rehydrate rebuilds the AI label list without the player's own
		// label (the player layer owns it), so it must be put back into ChartManager/CompetitorManager on every
		// load, not only when a fresh label object was built. RegisterLabel is idempotent.
		ChartManager.Instance?.RegisterLabel(label);
		CompetitorManager.Instance?.RegisterLabel(label);

		songs.Clear();
		songs.AddRange((data.Songs ?? new List<SongSaveData>()).Select(s => s.ToSong()));
		repertoire.Clear();
		foreach (var kv in data.Repertoire ?? new Dictionary<string, List<RepertoireSaveData>>())
			repertoire[kv.Key] = (kv.Value ?? new List<RepertoireSaveData>()).Select(r => r.ToItem()).ToList();
		rehearsals.Clear();
		rehearsals.AddRange((data.Rehearsals ?? new List<CoverRehearsalSaveData>()).Select(r => r.ToRehearsal()));
		shippedBSideRecordIds.Clear();
		if (data.ShippedBSideRecordIds != null) shippedBSideRecordIds.UnionWith(data.ShippedBSideRecordIds);
		log.Clear();
		log.AddRange(data.Log ?? new List<string>());
		books.Clear();
		books.AddRange((data.Books ?? new List<WeekBookSaveData>()).Select(b => b.ToWeekBooks()));

		// A load replaces the desk's working state wholesale -- any live session/slate is dropped.
		pendingSession = null;
		slate.Clear();

		// Masters first, keyed by record id, so planned releases can re-link their A/B sides to the very
		// same Master objects (a scheduled master lives in both lists).
		masters.Clear();
		var mastersByRecordId = new Dictionary<string, Master>(StringComparer.Ordinal);
		foreach (MasterSaveData saved in data.Masters ?? new List<MasterSaveData>()) {
			Master master = saved.ToMaster();
			masters.Add(master);
			if (master.Record?.recordId != null) mastersByRecordId[master.Record.recordId] = master;
		}
		planned.Clear();
		foreach (PlannedReleaseSaveData saved in data.Planned ?? new List<PlannedReleaseSaveData>()) {
			Master aSide = saved.ASideRecordId != null && mastersByRecordId.TryGetValue(saved.ASideRecordId, out Master a) ? a : null;
			if (aSide == null) continue; // a plug side with no master is meaningless -- drop it
			Master bSide = saved.BSideRecordId != null && mastersByRecordId.TryGetValue(saved.BSideRecordId, out Master b) ? b : null;
			planned.Add(new PlannedRelease {
				Master = aSide, BSide = bSide, Dated = saved.Dated,
				Date = new GameDate(saved.Year, saved.Month, saved.Day), MarketingBudget = saved.MarketingBudget
			});
		}

		inventory.Clear();
		foreach (PressStockSaveData saved in data.Inventory ?? new List<PressStockSaveData>())
			inventory[saved.RecordId] = new PressStock {
				Remaining = saved.Remaining, TotalPressed = saved.TotalPressed, TotalSpent = saved.TotalSpent
			};
		pressOrders.Clear();
		foreach (PressOrderSaveData saved in data.PressOrders ?? new List<PressOrderSaveData>())
			pressOrders.Add(new PressOrder {
				RecordId = saved.RecordId, Quantity = saved.Quantity, Cost = saved.Cost,
				Ordered = new GameDate(saved.OrderedYear, saved.OrderedMonth, saved.OrderedDay),
				Arrives = new GameDate(saved.ArrivesYear, saved.ArrivesMonth, saved.ArrivesDay)
			});
		consignment.Clear();
		foreach (ConsignmentLotSaveData saved in data.Consignment ?? new List<ConsignmentLotSaveData>()) {
			if (!consignment.TryGetValue(saved.CityId, out var lots)) { lots = new(StringComparer.Ordinal); consignment[saved.CityId] = lots; }
			lots[saved.RecordId] = new ConsignmentLot { Remaining = saved.Remaining, Placed = saved.Placed, DaysSinceRestock = saved.DaysSinceRestock };
		}
		consignmentOwed.Clear();
		foreach (var kv in data.ConsignmentOwed ?? new Dictionary<string, float>()) consignmentOwed[kv.Key] = kv.Value;
		weeklyTrunkUnits.Clear();
		foreach (var kv in data.WeeklyTrunkUnits ?? new Dictionary<string, int>()) weeklyTrunkUnits[kv.Key] = kv.Value;
		weeklyTrunkUnitsSold = data.WeeklyTrunkUnitsSold;
		weeklyTrunkGross = data.WeeklyTrunkGross;
		weeklyTrunkRoyalty = data.WeeklyTrunkRoyalty;
		weeklyTrunkHeld = data.WeeklyTrunkHeld;
		workedCities.Clear();
		foreach (string city in data.WorkedCities ?? new List<string>()) workedCities.Add(city);
		currentCityId = data.CurrentCityId;
		monthsInTheRed = data.MonthsInTheRed;
		counter = Mathf.Max(data.Counter, songs.Count);

		// Increment 2: put the player's released catalogue back into the world with its chart run intact,
		// after putting back anything an old save lost to the dead-stock cull (see RepairCulledRecords).
		List<RecordRuntimeData> catalogue = (data.ReleasedRecords ?? new List<RuntimeRecordSaveData>())
			.Select(record => record.ToRuntime()).ToList();
		int recovered = RepairCulledRecords(catalogue);
		ChartManager.Instance?.RestorePlayerRecords(catalogue);

		message = missing == 0
			? $"Loaded {label.labelName}."
			: $"Loaded {label.labelName} -- {missing} roster act(s) could not be re-linked.";
		if (recovered > 0) message += $" Recovered {recovered} record(s) an older save had dropped.";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	// ========================================================================
	// LOAD-TIME REPAIR -- records an older save lost to the dead-stock cull
	// ========================================================================

	/// <summary>
	/// Rebuilds player records a pre-fix save lost to the cull. Until <c>ChartManager.CullDeadRecords</c>
	/// learned to exempt player-owned records, a single that went five weeks under the sales floor without
	/// charting was deleted from the catalogue -- and because the discography is a live projection of that
	/// catalogue, the record vanished from the desk, saved as nothing, and left the press inventory, the
	/// town consignment lots and the rolodex airplay watches pointing at an id that resolved to nothing
	/// (which is why a town would offer "player_2" for sale). The retired-track archive kept a snapshot of
	/// every retired single, so the master can be reconstructed from it.
	///
	/// What comes back is the record's IDENTITY -- title, song, genre, the take's hook and production --
	/// not the chart run that was thrown away with it. Units are recovered from what the town lots
	/// actually moved; buzz starts cold, because a fabricated awareness figure is worth less than an
	/// honest zero. Returns how many were rebuilt. A healthy save finds nothing to do here.
	/// </summary>
	private int RepairCulledRecords(List<RecordRuntimeData> catalogue) {
		ChartManager charts = ChartManager.Instance;
		if (charts == null || Label == null) return 0;

		var known = new HashSet<string>(StringComparer.Ordinal);
		foreach (RecordRuntimeData record in catalogue)
			if (record?.baseRecord?.recordId != null) known.Add(record.baseRecord.recordId);
		foreach (Master master in masters)
			if (master.Record?.recordId != null) known.Add(master.Record.recordId);

		// Every record id the desk still holds a reference to. One that no longer resolves is a record
		// the player is holding stock, a plant order or a cut repertoire number against.
		var referenced = new HashSet<string>(StringComparer.Ordinal);
		foreach (string recordId in inventory.Keys) referenced.Add(recordId);
		foreach (var lots in consignment.Values)
			foreach (string recordId in lots.Keys) referenced.Add(recordId);
		foreach (PressOrder order in pressOrders)
			if (order.RecordId != null) referenced.Add(order.RecordId);
		foreach (string recordId in weeklyTrunkUnits.Keys) referenced.Add(recordId);
		foreach (var kv in repertoire)
			foreach (RepertoireItem item in kv.Value)
				if (item.Recorded && item.RecordedId != null) referenced.Add(item.RecordedId);
		foreach (Song song in songs)
			if (song.Recorded && song.RecordedId != null) referenced.Add(song.RecordedId);

		int recovered = 0;
		foreach (string recordId in referenced) {
			if (known.Contains(recordId)) continue;
			if (!charts.TryGetRetiredTrackSnapshot(recordId, out AlbumTrack track) || track == null) continue;
			RecordRuntimeData rebuilt = RebuildRecordFromSnapshot(recordId, track);
			if (rebuilt == null) continue;
			catalogue.Add(rebuilt);
			known.Add(recordId);
			recovered++;
			GD.Print($"[PlayerDesk] Repaired culled record {recordId} ({track.title}) from the retired-track archive.");
		}
		return recovered;
	}

	/// <summary>Reconstructs one culled record from its retired-track snapshot. The snapshot carries the
	/// master's identity and the song's biography, but not who cut it or what it sold: the act comes from
	/// whichever repertoire number or written song points at this record id (falling back to a one-act
	/// roster), and the units from what the town lots actually moved. Returns null if no act can be
	/// established -- a record with nobody's name on it is worse than a missing one.</summary>
	private RecordRuntimeData RebuildRecordFromSnapshot(string recordId, AlbumTrack track) {
		SimulatedArtist artist = ArtistForRecordId(recordId);
		if (artist == null) return null;

		var record = new Record {
			recordId = recordId,
			labelId = Label.labelId,
			title = track.title,
			artistId = artist.artistId,
			artistName = artist.stageName,
			format = ReleaseFormat.Single,
			isPlayerOwned = true,
			primaryGenre = track.genre,
			secondaryGenre = artist.secondaryGenre,
			hookStrength = track.hookStrength,
			productionQuality = track.productionQuality,
			danceability = track.danceability,
			releaseDate = track.releaseDate,
			songId = track.songId,
			songSource = track.songSource,
			isCover = track.isCover,
			originalRecordId = track.originalRecordId,
			originalArtistId = track.originalArtistId,
			publisherId = track.publisherId,
			songwriterNames = track.songwriterNames ?? Array.Empty<string>(),
			compositionQuality = track.compositionQuality,
			compositionHook = track.compositionHook,
			lyricQuality = track.lyricQuality,
			songFamiliarityAtRelease = track.songFamiliarityAtRelease,
			standardDurability = track.standardDurability,
			arrangementOriginality = track.arrangementOriginality
		};

		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		return new RecordRuntimeData(record) {
			peakPosition = track.peakPosition,
			weeksSinceRelease = today.WeeksDifference(track.releaseDate),
			totalUnitsSold = UnitsMovedFromLots(recordId),
			// The run is over and its reads already ran on the way out, when the cull retired it. Say so,
			// or the restored record would take its career and cultural reads a second time.
			artistChartRunCompleted = true,
			culturalRunCompleted = true
		};
	}

	/// <summary>The act a culled record belonged to, read back off whatever still points at its id.</summary>
	private SimulatedArtist ArtistForRecordId(string recordId) {
		foreach (var kv in repertoire)
			if (kv.Value.Any(item => item.Recorded && item.RecordedId == recordId))
				return Roster.FirstOrDefault(a => a.artistId == kv.Key);
		string writerArtistId = songs.FirstOrDefault(song => song.Recorded && song.RecordedId == recordId)?.ArtistId;
		if (writerArtistId != null) {
			SimulatedArtist writer = Roster.FirstOrDefault(a => a.artistId == writerArtistId);
			if (writer != null) return writer;
		}
		// A one-act label leaves no ambiguity about whose record it was.
		List<SimulatedArtist> roster = Roster.ToList();
		return roster.Count == 1 ? roster[0] : null;
	}

	/// <summary>Copies a record's town lots have actually sold -- the only honest lifetime-units figure
	/// left once the runtime record is gone.</summary>
	private int UnitsMovedFromLots(string recordId) {
		int moved = 0;
		foreach (var lots in consignment.Values)
			if (lots.TryGetValue(recordId, out ConsignmentLot lot))
				moved += Mathf.Max(0, lot.Placed - lot.Remaining);
		return moved;
	}

	public IEnumerable<SimulatedArtist> Roster => Label?.roster ?? new List<SimulatedArtist>();
	public IEnumerable<Song> UnrecordedSongs => songs.Where(song => !song.Recorded);
	/// <summary>All the act's written songs, recorded or not (the repertoire view shows recorded ones as cut).</summary>
	public IEnumerable<Song> SongsFor(string artistId) => songs.Where(song => song.ArtistId == artistId);
	/// <summary>True once the record has actually shipped (it's in the world), vs merely cut and sitting on the shelf.
	/// Counts a B-side that shipped on a single's flip, which is out even though it never became a market record.</summary>
	public bool IsRecordReleased(string recordId) =>
		recordId != null && (ReleasedRecords.Any(r => r.baseRecord?.recordId == recordId)
			|| shippedBSideRecordIds.Contains(recordId));

	/// <summary>True if this cut shipped only as the B-side of a single -- out, but on the flip, never worked
	/// as its own record. Lets the repertoire distinguish an A-side chasing the chart from its flip.</summary>
	public bool IsRecordReleasedAsBSide(string recordId) =>
		recordId != null && shippedBSideRecordIds.Contains(recordId)
			&& !ReleasedRecords.Any(r => r.baseRecord?.recordId == recordId);
	public IEnumerable<RecordRuntimeData> ReleasedRecords =>
		Label == null ? Enumerable.Empty<RecordRuntimeData>()
			: (ChartManager.Instance?.GetAllRecords() ?? new List<RecordRuntimeData>())
				.Where(record => record.baseRecord?.labelId == Label.labelId);
}
