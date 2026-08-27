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
	public const int ArtistBuyInHours = ActionCosts.QuickMeeting;   // 2 -- sit the act down and hand off the cartons

	// Inbound demand (directive §4): thresholds and pacing for "they called me." All read off real
	// regional chart state (RecordRuntimeData.regionalData) and the player's own stop roster -- never a
	// parallel buzz meter (§4.1).
	private const int CallExpiryWeeks = 3;              // unanswered, they fill it from someone else or forget
	private const int SoldOutOnHandThreshold = 3;        // a known account this thin, with real demand, calls
	private const float StrangerAwarenessThreshold = 0.22f;  // regional awareness that makes a never-visited shop plausible
	private const float StrangerRadioThreshold = 0.15f;      // regional radioPlay that does the same
	private const float StrangerCallChancePerWeek = 0.35f;   // even with real signal, not every week rings
	private const int StrangerCallsPerRecordPerWeek = 1;     // keep the office list legible, not a flood
	// A shop or op calling and getting no answer is not that out of the ordinary -- one miss barely dents
	// it. What actually costs you is a pattern: the penalty compounds with each consecutive unanswered
	// call at the same stop, capped so a bad month is bruising, not fatal to the account.
	private const float MissedCallBasePenalty = 0.015f;
	private const float MissedCallMaxPenalty = 0.12f;

	// One-stop (directive §6): the metro counterparty, "locked as a customer until inbound demand
	// exists." Not an IndependentDistributor -- a lighter, player-only counterparty (§0/§12).
	private const float OneStopServedRelationshipFloor = 0.35f;  // "an op or dealer they already serve" -- friendly or better
	private const int OneStopCartonDefaultQty = 100;             // "the first 50-200 that move at once"
	private const int OneStopCartonMaxQty = 200;
	private const float OneStopUnitPrice = 0.58f;                // wholesale, below the 0.89 retail -- "still fast-ish cash"
	private const int OneStopWarehouseVisitHours = ActionCosts.QuickMeeting; // 2 -- a real sit-down, not a counter pitch
	private const int OneStopPaymentTermWeeks = 5;               // "net 30-45" (days) once trusted; COD on the first carton

	// UI-facing mirrors of the private constants above (PlayerDeskPanel needs the numbers, not the logic).
	public static int OneStopVisitHours => OneStopWarehouseVisitHours;
	public static int OneStopCartonMax => OneStopCartonMaxQty;
	public static int OneStopCartonDefault => OneStopCartonDefaultQty;

	// People, first tier: contractors, never payroll (directive §7). No fixed weekly cost either one can
	// reach before the channel it serves has paid (invariant 5) -- the runner eats only a cut of what he
	// actually brings in, and the promo man is a one-off project fee, not a retainer.
	private const int RunnerUnlockReorderCount = 3;    // "persistent reorders in one city" -- successful Service calls, same town
	private const int RunnerUnlockCallCities = 2;      // "inbound calls in two cities the same week"
	public const int RunnerHandoffHours = ActionCosts.QuickMeeting; // 2 -- sit him down with a carton at the office
	// 8-15% of his collections (directive §7); mid-band, taken directly off the net he brings in, the
	// instant it lands -- "paid when the shop pays," never a separate payroll line.
	private const float RunnerCommissionRate = 0.12f;
	private const float RunnerAcceptBase = 0.20f;   // worse than the player's own 0.35 (PitchAtStop)
	private const float RunnerAcceptSlope = 0.5f;   // worse than the player's own 0.6 slope
	private const float RunnerFamiliarityGain = 0.06f; // "rises on his accounts" -- his own curve, not stop.Relationship
	private const float RunnerRelationshipGain = 0.02f; // still your label's stock -- a small tick for the player too

	public static int RunnerUnlockReorders => RunnerUnlockReorderCount;
	public static int RunnerUnlockCities => RunnerUnlockCallCities;
	public static float RunnerCommission => RunnerCommissionRate;

	/// <summary>$25-75, or a point on the record (directive §7) -- the point option isn't built this pass;
	/// cash only. Escalating tiers buy more stations, not a bigger bribe per station.</summary>
	public enum ProjectPromoTier { Small, Medium, Large }
	public static float ProjectPromoCost(ProjectPromoTier tier) => tier switch {
		ProjectPromoTier.Small => 25f, ProjectPromoTier.Medium => 50f, ProjectPromoTier.Large => 75f, _ => 25f };
	private static int ProjectPromoStationCount(ProjectPromoTier tier) => tier switch {
		ProjectPromoTier.Small => 1, ProjectPromoTier.Medium => 2, ProjectPromoTier.Large => 3, _ => 1 };
	private static float ProjectPromoTierEffectiveness(ProjectPromoTier tier) => tier switch {
		ProjectPromoTier.Small => 0.30f, ProjectPromoTier.Medium => 0.45f, ProjectPromoTier.Large => 0.60f, _ => 0.30f };
	public const int ProjectPromoHours = ActionCosts.QuickMeeting; // briefing the promo man
	public const int ProjectPromoWeeks = 2;                        // "1-2 weeks" -- the long end
	// A local promo man, not connected muscle -- moderate cover, low severity-if-busted, unlike a mob-tied
	// IndiePromoter. Only the duration differs from that class's own defaults otherwise.
	private const float ProjectPromoDiscretion = 0.5f;
	private const float ProjectPromoMobConnection = 0.1f;

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
		public float RunnerCommission; // what the runner kept this week, already netted out of Earned/Banked
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
	// Promo mechanic directive §3.2: who has actually been sent a copy of what. One row per
	// (recordId, stationId); see FindServicing/IsServiced/ServiceStation and Objection.NotServiced.
	private readonly List<RecordServicing> servicing = new();
	// Promo mechanic directive §6.1: one row per record ever sent to the trade review desk.
	private readonly List<TradeSubmission> tradeSubmissions = new();
	// Promo mechanic directive §6.2: live paid trade ads, one row per record currently running.
	private readonly List<TradeAd> tradeAds = new();
	// Promo mechanic directive §7.1: which reporting dealers the player has actually WORKED OUT report
	// their counter numbers. Who reports is fixed, generated data (PlayerStop.ReportsToTrades); knowing
	// it is not. "That is the information the early game is actually about" -- so it is learned, either
	// by dealing with the man across his own counter (TouchStop) or by asking at the station whose
	// survey he phones (AskWhatsOnSurvey), and never simply printed on a stop the player has never met.
	private readonly HashSet<string> knownReportingStopIds = new(StringComparer.Ordinal);
	// Named accounts (shops and jukebox operators), keyed by StopId, generated once per session by
	// PlayerStopFactory (see EnsureStops). This is what actually sells, day by day, decaying until you
	// drive back to restock -- the per-city ConsignmentLot this used to be is now per-stop, on PlayerStop.OnHand.
	private Dictionary<string, PlayerStop> stops;
	// Open "they called me" demand (directive §4). Generated once per chart week (CheckWeeklyInboundCalls),
	// answered by working the stop normally (TryFulfillCall), or left to expire.
	private readonly List<InboundCall> inboundCalls = new();
	private int lastCallGenWeek = -1;
	// Trunk units sold this chart-week per record, accumulated daily and swept into the weekly chart total
	// so a record that only sells out of the trunk still charts on those units.
	private readonly Dictionary<string, int> weeklyTrunkUnits = new(StringComparer.Ordinal);
	// This chart-week's trunk business, accumulated daily in BookTrunkSale and folded into the week's settlement
	// write-up at week-end (then reset) so the settlement reflects trunk sales, not only the wholesale channel.
	// trunkHeld is the cut that went out on consignment (towns you weren't standing in) and hasn't reached the
	// bank yet; the rest was spot cash. Persisted so a mid-week save doesn't drop the partial week.
	private long weeklyTrunkUnitsSold;
	private float weeklyTrunkGross, weeklyTrunkRoyalty, weeklyTrunkHeld;
	// Towns the player has physically worked -- opened a market to sell out of the trunk.
	private readonly HashSet<string> workedCities = new(StringComparer.Ordinal);
	// People (directive §7). The runner is null until hired; unlock ratchets on and never closes once earned.
	private PlayerRunner runner;
	private bool runnerUnlocked;
	private int lastRunnerTickWeek = -1;
	// "Persistent reorders in one city" -- successful ServiceStop calls, tallied per city toward the unlock.
	private readonly Dictionary<string, int> serviceReorderCountByCity = new(StringComparer.Ordinal);
	// This chart-week's runner commission, accumulated in BookSale and folded into the settlement write-up
	// at week-end (then reset) -- without it, Earned would overstate the label's actual post-commission cut.
	private float weeklyRunnerCommission;
	// Plant credit (directive §11): null when nothing is owed. One outstanding at a time.
	private PlantCredit plantCredit;
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
	// The stampers cut for a title's FIRST run stay at the plant -- a repress of the same title doesn't
	// pay the lacquer setup again and can run far under the minimum, historically low hundreds at a time
	// as a building hit needs them. PressMinimumOrder still gates the first run of any title.
	public const int PressReorderMinimum = 100;
	// Promo mechanic directive §3.1: the promotion budget is a real slice of the run struck as free
	// goods that can never be sold. Capped on a FIRST run so a label can't exploit it as a free-goods
	// channel; a repress carries no cap -- "servicing a second market on a record that's already
	// moving" is meant to be an obvious, cheap move.
	public const float PressPromoCapFraction = 0.35f;
	// Directive §3.1's suggested first-run default -- "120 of 500". Well under the 35% cap, because the
	// default has to be a competent campaign the player can afford to give away, not the maximum dodge.
	public const float PressPromoSuggestedFraction = 0.24f;
	// Directive §7.1: "one or two per city." How many of his own town's reporter stations one identified
	// dealer phones his counter numbers in to -- see EnsureStops.
	private const int ReportingStationsPerDealer = 2;
	// Press-to-fill (directive §11): size a run off open InboundCall demand instead of a guessed
	// quantity -- a cushion above the raw backlog so the run doesn't land already sold out.
	private const float PressToFillCushion = 1.2f;
	// Plant credit (directive §11, "a mid-game gun"): some plants fronted a promising client a real run
	// on credit (Plastic Products for Stax/Sun) -- gated on the same kind of real, geographic evidence as
	// the house-line proof gate (§5), not a tutorial freebie. One outstanding at a time; the plant WILL
	// collect on schedule, win or lose -- that certainty is the point (invariant 2's cash-timing trap).
	public const int PlantCreditQuantity = 1000;
	public const int PlantCreditTermWeeks = 10;
	public const int PlantCreditDemandThreshold = 250;
	public const int PlantCreditHours = ActionCosts.LongCall; // a real ask, not a form to fill out

	/// <summary>isRepress drops the lacquer setup -- the stampers cut for that title's first run are
	/// already sitting at the plant, so a repress only pays for vinyl, sleeves/labels, and shipping.</summary>
	public static float PressingCost(int quantity, bool isRepress = false) =>
		(isRepress ? 0f : PressLacquerSetup) + PressShipping + Mathf.Max(0, quantity) * (PressVinylPerUnit + PressSleeveLabelPerUnit);

	/// <summary>Pressed 45s of one single sitting on hand at the office, to be carried out to towns.
	/// Promo mechanic directive §3.1: <see cref="PromoRemaining"/> is a second, disjoint pool struck off
	/// the same run -- free goods that can never be sold. Neither pool ever converts into the other.</summary>
	public sealed class PressStock {
		public int Remaining;
		public int PromoRemaining;
		public int TotalPressed;
		public float TotalSpent;
	}

	/// <summary>A pressing run in the pipeline: paid for, mailed off, and working its way back from the
	/// plant. Delivered to <see cref="inventory"/> on its arrival day.</summary>
	public sealed class PressOrder {
		public string RecordId;
		public int Quantity;
		// How much of Quantity lands in PromoRemaining instead of Remaining on delivery (directive §3.1).
		public int PromoQuantity;
		public float Cost;
		public GameDate Ordered;
		public GameDate Arrives;
	}

	/// <summary>Directive §11's plant credit: the plant fronts a run with nothing charged up front, and
	/// WILL collect the pressing cost on schedule regardless of how the record's doing by then -- the
	/// certainty is the point (invariant 2's cash-timing trap, a mid-game gun). One at a time.</summary>
	public sealed class PlantCredit {
		public string RecordId;
		public float Amount;
		public int DueWeek;
	}

	/// <summary>Records left with one stop. They sell on their own, day by day, at a rate that decays the
	/// longer it's been since you restocked -- the stop's appetite tapers until you come back.
	/// ConsignmentTerms marks stock placed on <see cref="ConsignAtStop"/> terms: unlike a COD pitch, it
	/// never pays cash-in-hand even when you're standing in town -- it always waits in the stop's
	/// OpenBalance, the worse-cash trade for the lower bar to get it on the shelf.</summary>
	public sealed class ConsignmentLot {
		public int Remaining;
		public int Placed;
		public int DaysSinceRestock;
		public bool ConsignmentTerms;
		/// <summary>True when the stock currently sitting in this lot came out of the runner's carton, not
		/// the office's own inventory (directive §7) -- ProcessTrunkDay reads this to route the day's
		/// sell-through through BookRunnerSale (commission taken off the top) instead of BookTrunkSale.</summary>
		public bool RunnerSourced;
		/// <summary>Promo mechanic directive §9: a live window card / counter card at this stop for this
		/// record. 0 = none running. ProcessTrunkDay's appeal term reads it while live; never a source of
		/// units on its own, just a bounded lift on how well the record already sells here.</summary>
		public int WindowCardExpiresWeek;
	}

	/// <summary>A named account in a town -- a shop, a jukebox operator, or a metro one-stop -- with its
	/// own relationship, stock, and balance. Identity (name/city/kind) is regenerated deterministically
	/// every session by <see cref="PlayerStopFactory"/> off the world seed, so it never has to round-trip
	/// through the save file; only the mutable state below does (see StopState in PlayerSaveData).
	/// OneStop is not a walk-in counter like Shop/Op -- see PlayerStop.OneStopUnlocked and
	/// VisitOneStopWarehouse/SellCartonToOneStop below (directive §6). Venue (directive §3.1's "hop/club/
	/// church table") is not a standing account either -- no OnHand, no OpenBalance, just a single verb,
	/// WorkTheHopTable below.</summary>
	// Station: directive §4. Unlike the others, not invented by PlayerStopFactory -- it's a
	// read-only projection of a real reporter station onto the stop layer (see EnsureStops).
	public enum StopKind { Shop, Op, OneStop, Venue, Station }

	public sealed class PlayerStop {
		public string StopId;
		public string DisplayName;
		public string CityId;
		public StopKind Kind;
		/// <summary>0-1, slow to earn and slower to repair -- ELIGIBILITY (how much they'll take, how
		/// readily), not a damage stat.</summary>
		public float Relationship;
		public int LastVisitWeek;
		/// <summary>recordId -> lot. Replaces the old per-city ConsignmentLot -- the same sell-through
		/// math now runs per stop instead of per town, so a hot shop and a dead one two doors down no
		/// longer move in lockstep.</summary>
		public readonly Dictionary<string, ConsignmentLot> OnHand = new(StringComparer.Ordinal);
		/// <summary>This stop's slice of what it's holding for you, wired thin and daily
		/// (<see cref="WireOwedTrickle"/>) or collected in a lump when you show up in its city.</summary>
		public float OpenBalance;
		/// <summary>Consecutive unanswered calls at this stop. Drives the escalating relationship penalty
		/// in <see cref="ExpireInboundCalls"/> -- one miss is nothing, a run of them is neglect. Resets on
		/// any visit (<see cref="TouchStop"/>), not just an answered call.</summary>
		public int MissedCallStreak;
		/// <summary>Last calendar day a Pitch or Consign was attempted here -- one approach per stop per
		/// day, shared across both verbs. Stops a player from spamming the same shop on the same trip
		/// hoping for a better roll; go somewhere else, or come back tomorrow.</summary>
		public GameDate LastApproachDate;
		/// <summary>Titles this stop has said no to on COD terms. A pass sticks -- it is not cleared by
		/// simply asking again -- until real evidence reopens the door: <see cref="GenerateInboundCalls"/>
		/// clears an entry the moment that title generates a call at this stop, which only happens once
		/// regional airplay/velocity make a stranger call plausible (never a re-roll on demand). Consign
		/// is unaffected -- it is the low-risk fallback a stop that passed on COD will still take.</summary>
		public readonly HashSet<string> PassedRecordIds = new(StringComparer.Ordinal);
		/// <summary>OneStop-kind stops only. A metro one-stop is "locked as a customer until inbound
		/// demand exists" (directive §3.1) -- it takes no Pitch/Consign/Service, only a warehouse visit
		/// once it's called (see VisitOneStopWarehouse), after which SellCartonToOneStop is live.</summary>
		public bool OneStopUnlocked;
		/// <summary>OneStop-kind stops only. First carton is COD ("if you're nobody") -- flips true on the
		/// first completed sale, after which SellCartonToOneStop extends net terms (directive §6).</summary>
		public bool OneStopTrusted;
		/// <summary>Station-kind stops only: the real reporter station this stop projects (directive §4).
		/// A Station stop holds no OnHand lot and no OpenBalance -- it never sells anything.</summary>
		public string StationId;

		/// <summary>Shop/OneStop only (directive §7.1): whether this account's counter feeds the national
		/// trade charts. Set once at generation (PlayerStopFactory) -- the one-stop, always, plus the one
		/// or two biggest dealers per city -- never rolled at runtime.</summary>
		public bool ReportsToTrades;
		/// <summary>Shop only (directive §7.1): the real reporter station(s) this dealer phones his own
		/// counter numbers to for the local Top 40 survey. Populated at EnsureStops from the same
		/// ReportsToTrades dealers -- a big dealer plausibly does both jobs -- never a separate roll.</summary>
		public List<string> ReportsToStationIds = new();
		/// <summary>Shop only (directive §7.3): tripped once he's caught noticing the same man buying his
		/// own record off the counter. Permanent -- blocks HypeTheCount and AskForTheReport at this stop
		/// from ever running again; it does not end ordinary business, only the trust a report or a
		/// second hype needs.</summary>
		public bool HypeBurned;
	}

	/// <summary>
	/// Directive §7: "no weekly nut." A commission trunk runner covers a route of the player's own named
	/// stops off a carton the player hands him -- the same sell/consign/service outcomes as the player's
	/// own verbs (see CheckWeeklyRunner), just a worse starting conversion per account (Familiarity, not
	/// stop.Relationship) that rises the more he services it. Paid only out of what he actually collects
	/// (RunnerCommissionRate, taken in BookSale) -- fire him by simply not handing him more stock.
	/// </summary>
	public sealed class PlayerRunner {
		public readonly HashSet<string> RouteStopIds = new(StringComparer.Ordinal);
		public string CartonRecordId;
		public int CartonRemaining;
		/// <summary>stopId -> his own conversion curve at that account, 0-1. Separate from stop.Relationship
		/// -- a stop already warm to the player still starts cold on HIM.</summary>
		public readonly Dictionary<string, float> Familiarity = new(StringComparer.Ordinal);
	}

	/// <summary>Directive §4.2: "the world phones the office" instead of the player having to guess where
	/// demand outran the shelf. Why the phone rang, not a synthetic score -- every reason here reads off
	/// regional chart state or a stop's own on-hand, never a parallel buzz meter (§4.1).</summary>
	public enum InboundCallReason {
		SoldOut,       // an account you already stocked ran thin while real demand is still there
		Requests,      // a shop you've never visited, but the counter's fielding requests for it
		StationAdded,  // same "stranger" call, triggered off airplay rather than raw sales velocity
		AdjacentCity,  // a stop in a town next to one you've already worked wants in too
		OneStopTest    // the metro one-stop's first look -- directive §6, surfaced through a stop it already serves
	}

	/// <summary>One piece of "they called me" demand (directive §4.2): a stop asking for stock beyond
	/// what the player has physically carried out. The player answers it with the same Pitch/Consign/
	/// Service verbs as any other stop -- there is no separate "fulfill" action -- or lets it lapse,
	/// which costs relationship at a known account and just evaporates at a stranger one (§4.3).</summary>
	public sealed class InboundCall {
		public string StopId;
		public string RecordId;
		public int Week;
		public int RequestedQty;
		public InboundCallReason Reason;
		public int ExpiresWeek;
		/// <summary>What terms they're expecting if you show up -- true reads as "leave it on consignment",
		/// false as "COD's fine." A hint for the office readout, not an enforced contract.</summary>
		public bool ConsignmentTerms;
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
	// How loudly a real, cultivated reporter spin speaks in a stop's local pull, ON TOP of the region's
	// accumulated awareness. This is the player-only lever that fixes the "a Detroit spin and a private
	// pressing sold the same" problem at full strength: reaching airplay through regionalData.awareness
	// alone dilutes it to ~13% (the AI-shared REPORTER_PANEL_WEIGHT). Reading the reporter panel directly
	// here bypasses that dilution for the player's trunk math without touching any AI-economy number.
	// Bounded lift only (never a penalty), so an organically-broken record is never dragged down by a
	// cooling airplay curve. Tunable knob -- raise for a punchier Rolodex payoff.
	private const float TrunkReporterAirplayLift = 0.5f;
	// While you're away, a town's shops wire you a thin slice of what they owe each day, so money isn't
	// fully stranded -- but the bulk waits for you to drive back and collect it in person.
	private const float TrunkWireFractionPerDay = 0.04f;

	public PressStock StockFor(string recordId) =>
		recordId != null && inventory.TryGetValue(recordId, out PressStock stock) ? stock : null;
	public IEnumerable<string> WorkedCities => workedCities;

	// ── Promo servicing (directive §3.2) ────────────────────────────────────────────────────────
	// A copy sent months ago is in a stack somewhere, not on the turntable -- servicing decays out
	// after this many chart weeks, same order of magnitude as a pitch's record-memory settlement.
	public const int ServicingDecayWeeks = 16;

	private RecordServicing FindServicingRow(string recordId, string stationId) =>
		string.IsNullOrEmpty(recordId) || string.IsNullOrEmpty(stationId) ? null
			: servicing.FirstOrDefault(s => s.RecordId == recordId && s.StationId == stationId);

	/// <summary>Whether this station has a live (not decayed) serviced copy of this record right now --
	/// the fact Objection.NotServiced and Resolve() gate on. Directive invariant 2: nothing gets played
	/// that nobody has been sent.</summary>
	public bool IsServiced(string recordId, string stationId) {
		RecordServicing row = FindServicingRow(recordId, stationId);
		if (row == null) return false;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		return week - row.Week <= ServicingDecayWeeks;
	}

	/// <summary>The conviction of the live serviced copy, or 0 if none (decayed or never sent).</summary>
	public float ServicingConviction(string recordId, string stationId) {
		RecordServicing row = FindServicingRow(recordId, stationId);
		if (row == null) return 0f;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		return week - row.Week <= ServicingDecayWeeks ? row.Conviction : 0f;
	}

	/// <summary>Records a copy as sent. A later, stronger servicing (a hand-delivery after a cold
	/// mailing) overwrites the row rather than stacking -- one copy per station is what matters, and
	/// the best one you've sent is the one on the turntable.</summary>
	private void ServiceStation(string recordId, string stationId, float conviction, ServicingSource source) {
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		RecordServicing row = FindServicingRow(recordId, stationId);
		if (row == null) { row = new RecordServicing { RecordId = recordId, StationId = stationId }; servicing.Add(row); }
		if (row.Week > 0 && conviction < row.Conviction && week - row.Week <= ServicingDecayWeeks) return; // don't downgrade a live, better copy
		row.Week = week;
		row.Conviction = conviction;
		row.Source = source;
	}

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
	/// <summary>The stampers for a title exist once it's been pressed at all -- a repress doesn't need a
	/// fresh lacquer cut, and can run far under the first-run minimum.</summary>
	public bool HasBeenPressed(string recordId) =>
		recordId != null && inventory.TryGetValue(recordId, out PressStock stock) && stock.TotalPressed > 0;

	/// <summary>The floor the plant will run for this title right now: the full first-run minimum until
	/// it's been pressed once, the much smaller repress floor after.</summary>
	public int MinimumPressRun(string recordId) => HasBeenPressed(recordId) ? PressReorderMinimum : PressMinimumOrder;

	/// <summary>Promo mechanic directive §3.1: the plant will strike part of the run as promo stock --
	/// free goods, drawn only by servicing verbs, that can never be sold. Capped at
	/// <see cref="PressPromoCapFraction"/> of the run on a FIRST pressing; a repress carries no cap, since
	/// pressing an all-promo repress to service a second market is meant to be an obvious, cheap move.</summary>
	public int MaxPromoCount(string recordId, int quantity) =>
		HasBeenPressed(recordId) ? quantity : Mathf.FloorToInt(quantity * PressPromoCapFraction);

	/// <summary>What the plant ticket should open on. Directive §3.1 names 120 of 500 for a first run --
	/// the promo pool is the foundation every other verb on this branch draws from, so a first pressing
	/// that defaults to zero silently locks the player out of station stops, the mailing, the review desk
	/// and window cards. A REPRESS opens at zero instead: by then the player knows what promo stock is
	/// for, and striking a repress all-promo is a deliberate second-market move, not a default.</summary>
	public int SuggestedPromoCount(string recordId, int quantity) =>
		HasBeenPressed(recordId) ? 0
			: Mathf.Min(MaxPromoCount(recordId, quantity), Mathf.RoundToInt(quantity * PressPromoSuggestedFraction));

	public bool OrderPressing(string recordId, int quantity, int promoCount, out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (!RequireHome(out message)) return false;
		if (string.IsNullOrEmpty(recordId)) { message = "No single selected."; return false; }
		if (IsMasterOut(recordId)) { message = $"\"{TitleForRecord(recordId)}\" isn't yours to press right now -- the master's out."; return false; }
		bool repress = HasBeenPressed(recordId);
		int minimum = MinimumPressRun(recordId);
		if (quantity < minimum) {
			message = repress ? $"Even a repress won't run under {minimum}." : $"The plant won't run under {minimum} on a first pressing.";
			return false;
		}
		promoCount = Mathf.Clamp(promoCount, 0, quantity);
		int promoCap = MaxPromoCount(recordId, quantity);
		// A repress carries no cap (MaxPromoCount returns the full quantity), so only a first run can
		// ever trip this -- promoCount is already clamped to <= quantity above.
		if (promoCount > promoCap) {
			message = $"A first run can't strike more than {promoCap:N0} of {quantity:N0} as promo ({PressPromoCapFraction:P0}) -- that's not promotion, that's a free-goods dodge.";
			return false;
		}

		float cost = PressingCost(quantity, repress);
		if (Label.cashReserves < cost) {
			message = $"You're ${cost - Label.cashReserves:N0} short of a ${cost:N0} run.";
			return false;
		}

		Label.cashReserves -= cost;
		Label.monthlyExpenses += cost;
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int lead = RollPressLeadDays(today);
		GameDate arrives = today.AddDays(lead);
		pressOrders.Add(new PressOrder {
			RecordId = recordId, Quantity = quantity, PromoQuantity = promoCount, Cost = cost, Ordered = today, Arrives = arrives
		});

		string title = TitleForRecord(recordId);
		string promoNote = promoCount > 0 ? $" ({promoCount:N0} struck as promo)" : "";
		Note($"{(repress ? "Repressed" : "Ordered")} {quantity:N0} of \"{title}\"{promoNote} for ${cost:N0} -- the plant quotes {lead} days, in by {arrives.ToHeadlineString()}.");
		message = $"Run ordered -- about {lead} days at the plant.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Directive §11's "press-to-fill": the total the office is on the hook for right now, read
	/// off open InboundCalls for this title -- what shops and ops are actually asking for, not a guess.</summary>
	public int OpenCallDemand(string recordId) =>
		string.IsNullOrEmpty(recordId) ? 0 : inboundCalls.Where(c => c.RecordId == recordId).Sum(c => c.RequestedQty);

	/// <summary>The suggested press-to-fill quantity: open demand plus a cushion so the run doesn't land
	/// already sold out, floored at whatever minimum this title can run right now.</summary>
	public int PressToFillQuantity(string recordId) =>
		Mathf.Max(MinimumPressRun(recordId), Mathf.CeilToInt(OpenCallDemand(recordId) * PressToFillCushion));

	/// <summary>One click: press a run sized to what's actually being asked for, instead of eyeballing a
	/// quantity. Same plant, same cost, same turnaround as OrderPressing -- only the quantity is chosen
	/// for the player, off real backlog rather than a guess.</summary>
	public bool PressToFill(string recordId, out string message) {
		if (OpenCallDemand(recordId) <= 0) { message = "No open calls on that one to fill."; return false; }
		return OrderPressing(recordId, PressToFillQuantity(recordId), 0, out message);
	}

	/// <summary>Whether the plant would front a credit run on this title right now: real, geographic
	/// evidence it's moving (the same open-call backlog press-to-fill reads), and nothing already owed.</summary>
	public bool PlantCreditEligible(string recordId) =>
		plantCredit == null && OpenCallDemand(recordId) >= PlantCreditDemandThreshold;

	/// <summary>What's currently owed on a plant credit run, or null if none is outstanding.</summary>
	public (string RecordId, float Amount, int WeeksAway)? PlantCreditOwed {
		get {
			if (plantCredit == null) return null;
			int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
			return (plantCredit.RecordId, plantCredit.Amount, Mathf.Max(0, plantCredit.DueWeek - week));
		}
	}

	/// <summary>
	/// Directive §11: "some places... helped startups... by pressing ~1k on credit." Nothing is charged
	/// now -- the plant fronts <see cref="PlantCreditQuantity"/> and WILL collect the full pressing cost
	/// on <see cref="PlantCreditTermWeeks"/>, win or lose on the record by then (<see cref="SettlePlantCreditIfDue"/>).
	/// Only on real backlog evidence (<see cref="PlantCreditEligible"/>), and only one at a time.
	/// </summary>
	public bool RequestPlantCredit(string recordId, out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (!RequireHome(out message)) return false;
		if (plantCredit != null) { message = "You still owe the plant for the last credit run."; return false; }
		if (OpenCallDemand(recordId) < PlantCreditDemandThreshold) { message = "The plant isn't hearing enough on that one to front you a run."; return false; }
		if (!Require(PlantCreditHours, out message)) return false;

		Spend(PlantCreditHours);
		bool repress = HasBeenPressed(recordId);
		float cost = PressingCost(PlantCreditQuantity, repress);
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int lead = RollPressLeadDays(today);
		pressOrders.Add(new PressOrder { RecordId = recordId, Quantity = PlantCreditQuantity, Cost = cost, Ordered = today, Arrives = today.AddDays(lead) });
		int dueWeek = (ChartManager.Instance?.GetCurrentChartWeek() ?? 0) + PlantCreditTermWeeks;
		plantCredit = new PlantCredit { RecordId = recordId, Amount = cost, DueWeek = dueWeek };

		string title = TitleForRecord(recordId);
		Note($"The plant fronted {PlantCreditQuantity:N0} of \"{title}\" on credit -- ${cost:N0} due in {PlantCreditTermWeeks} weeks, no questions asked till then.");
		message = $"Plant's fronting {PlantCreditQuantity:N0}, no cash down -- ${cost:N0} comes due in {PlantCreditTermWeeks} weeks.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Collects on a due plant credit -- certain, not a dice roll, whatever shape the record's in
	/// by then (invariant 2's cash-timing trap). Runs every settled week.</summary>
	private void SettlePlantCreditIfDue() {
		if (plantCredit == null) return;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		if (week < plantCredit.DueWeek) return;
		float amount = plantCredit.Amount;
		Label.cashReserves -= amount;
		Label.monthlyExpenses += amount;
		Note($"The plant collected on its credit run -- ${amount:N0} due, paid whether \"{TitleForRecord(plantCredit.RecordId)}\" is still moving or not.");
		plantCredit = null;
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
			int promo = Mathf.Clamp(order.PromoQuantity, 0, order.Quantity);
			stock.Remaining += order.Quantity - promo;
			stock.PromoRemaining += promo;
			stock.TotalPressed += order.Quantity;
			stock.TotalSpent += order.Cost;
			string promoNote = promo > 0 ? $" ({promo:N0} promo)" : "";
			Note($"The pressing plant delivered {order.Quantity:N0} of \"{TitleForRecord(order.RecordId)}\"{promoNote}.");
		}
	}

	/// <summary>Pressing runs still at the plant, soonest first.</summary>
	public IEnumerable<(string Title, int Quantity, GameDate Arrives)> PendingPressings() =>
		pressOrders.OrderBy(o => o.Arrives).Select(o => (TitleForRecord(o.RecordId), o.Quantity, o.Arrives));

	// ========================================================================
	// NAMED STOPS -- shops and jukebox operators. The grain WorkThisTown used to sell in one whole-city
	// lot; a city is now a small, legible roster of named accounts, each with its own relationship,
	// stock and balance. Identity is generated once per session (EnsureStops); only the mutable state
	// below is ever saved (see StopState in PlayerSaveData).
	// ========================================================================

	/// <summary>Generates (or returns the cached) named-stop roster for every city, deterministic on the
	/// world seed so identity never has to round-trip through the save file. A few hundred stops
	/// nationally -- cheap enough to build once per session, not worth generating lazily per city.</summary>
	private Dictionary<string, PlayerStop> EnsureStops() {
		if (stops != null) return stops;
		var regionsById = (ChartManager.Instance?.GetAllRegions() ?? Enumerable.Empty<MarketRegion>())
			.Where(r => r != null).ToDictionary(r => r.regionId, r => r, StringComparer.Ordinal);
		ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
		stops = PlayerStopFactory.Generate(DistanceModel.GetCities(), regionsById, seed)
			.ToDictionary(s => s.StopId, StringComparer.Ordinal);

		// Directive §4: station stops are PROJECTED from the real reporter stations the sim already
		// runs, never invented -- no random draw, nothing that could diverge from ChartManager's own
		// panel. Identity (callsign/city) always regenerates fresh from there; only mutable state
		// (relationship, visit history) is ever saved, same as every other stop kind.
		foreach (RadioStation station in ChartManager.Instance?.AllReporterStations ?? Enumerable.Empty<RadioStation>()) {
			MarketCity city = DistanceModel.GetCityByName(station.cityName);
			if (city == null || string.IsNullOrEmpty(station.stationId)) continue;
			string stopId = "station_" + station.stationId;
			stops[stopId] = new PlayerStop {
				StopId = stopId, DisplayName = station.callsign, CityId = city.cityId,
				Kind = StopKind.Station, StationId = station.stationId,
			};
		}

		// Directive §7.1: the same identified big dealer(s) who report to the trades also phone their
		// counter numbers in to a real reporter station -- not a separate roll, just the read-only
		// station roster ChartManager already runs, same as the Station stops above.
		//
		// "Keep it small and legible -- one or two per city; the whole point is that they are
		// identifiable." A dealer reports to his OWN TOWN's stations: that is what a counter report was,
		// a man phoning the local Top 40 survey with what moved this week. Handing him the whole region's
		// panel (six to eight stations off a 77-station national panel) made one AskForTheReport a
		// region-wide advocacy grant and one hype detection a region-wide burn -- far past the bounded,
		// local nudge §7.2 asks for. A city with no reporter of its own falls back to the single nearest
		// in the region by road, which is the one whose survey a dealer there would plausibly phone.
		foreach (PlayerStop shop in stops.Values.Where(s => s.Kind == StopKind.Shop && s.ReportsToTrades)) {
			string regionId = DistanceModel.GetCityById(shop.CityId)?.parentRegionId;
			if (string.IsNullOrEmpty(regionId)) continue;
			List<RadioStation> inRegion = (ChartManager.Instance?.ReporterStationsInRegion(regionId) ?? Array.Empty<RadioStation>())
				.Where(s => s != null && !string.IsNullOrEmpty(s.stationId)).ToList();
			List<string> inTown = inRegion
				.Where(s => string.Equals(DistanceModel.GetCityByName(s.cityName)?.cityId, shop.CityId, StringComparison.Ordinal))
				.Take(ReportingStationsPerDealer)
				.Select(s => s.stationId).ToList();
			shop.ReportsToStationIds = inTown.Count > 0 ? inTown : inRegion
				.OrderBy(s => DistanceModel.GetRoadMilesBetween(shop.CityId, DistanceModel.GetCityByName(s.cityName)?.cityId))
				.Take(1)
				.Select(s => s.stationId)
				.ToList();
		}
		return stops;
	}

	private PlayerStop GetStop(string stopId) =>
		!string.IsNullOrEmpty(stopId) && EnsureStops().TryGetValue(stopId, out PlayerStop stop) ? stop : null;

	private string CityName(string cityId) => DistanceModel.GetCityById(cityId)?.name ?? cityId;

	/// <summary>The region a named stop's city sits in -- the read directive §10 needs to gate retail
	/// access off <see cref="MarketRegion.GetSegregationFactor"/> without inventing a parallel region
	/// lookup (EnsureStops already resolves one at generation time, but only as a local scratch dict).</summary>
	private MarketRegion RegionForStop(PlayerStop stop) {
		string regionId = DistanceModel.GetCityById(stop?.CityId)?.parentRegionId;
		return string.IsNullOrEmpty(regionId) ? null : ChartManager.Instance?.GetRegionById(regionId);
	}

	/// <summary>The genre of a record still in the player's hands, mirroring <see cref="TitleForRecord"/>'s
	/// masters-then-released lookup order.</summary>
	private Genre GenreForRecord(string recordId) {
		Master master = masters.FirstOrDefault(m => m.Record?.recordId == recordId);
		if (master?.Record != null) return master.Record.primaryGenre;
		RecordRuntimeData released = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId);
		return released?.baseRecord?.primaryGenre ?? Genre.RockAndRoll;
	}

	/// <summary>Directive §10: "gate which stops and stations see the player off GetEraIntegration(year) --
	/// a visible access change, not a hidden buff." 1.0 outside the race-record-era genres; for
	/// RnB/Soul/Gospel/DooWop it's exactly <see cref="MarketRegion.GetSegregationFactor"/> -- the same
	/// white-market-reach term a record's own market size already reads (Data/MarketRegion.cs) -- so a
	/// Deep South county's retail opens to the player on the identical civil-rights-era curve it opens to
	/// everyone else's black-audience records on, never a separately hand-tuned number.</summary>
	private float RetailAccessFactor(PlayerStop stop, string recordId) {
		Genre genre = GenreForRecord(recordId);
		if (!MarketRegion.IsBlackAudienceGenre(genre)) return 1f;
		return RegionForStop(stop)?.GetSegregationFactor(genre) ?? 1f;
	}

	/// <summary>The named accounts in a town, kind then name so the day-sheet reads the same every visit.</summary>
	public IEnumerable<PlayerStop> StopsInCity(string cityId) =>
		string.IsNullOrEmpty(cityId) ? Enumerable.Empty<PlayerStop>()
			: EnsureStops().Values.Where(s => s.CityId == cityId)
				.OrderBy(s => s.Kind).ThenBy(s => s.DisplayName, StringComparer.Ordinal);

	/// <summary>How many of a single a stop will comfortably hold: a cold call is a handful, a cultivated
	/// account takes a real box, and an op's route always moves more than a shop's counter -- "one op
	/// order should match a week of shop-by-shop nickels" (directive §3.3). Only the default the picker
	/// starts on; the player sets the actual number. An op's route shrinks with the decade (directive
	/// §10, PlayerStopFactory.JukeboxEraWeight) -- the same route that moved 20-40 in 1962 is a fraction
	/// of that by 1968, no matter how good the relationship.</summary>
	public int SuggestedPlacement(PlayerStop stop) {
		if (stop == null) return 5;
		if (stop.Kind == StopKind.Op) {
			int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
			float route = Mathf.Lerp(20f, 40f, stop.Relationship) * PlayerStopFactory.JukeboxEraWeight(year);
			return Mathf.Max(1, Mathf.RoundToInt(route));
		}
		// A hop, club or church table never scales past what fits on it -- "tiny volume" (directive §3.3),
		// well under even a cold shop's handful.
		if (stop.Kind == StopKind.Venue) return Mathf.Max(1, Mathf.RoundToInt(Mathf.Lerp(3f, 12f, stop.Relationship)));
		return Mathf.RoundToInt(Mathf.Lerp(4f, 30f, stop.Relationship));
	}

	/// <summary>Rough hours a visit to this kind of account runs, for display before the roll -- see
	/// StopVisitHours for the real (varying) cost charged when the action executes. A shop counter
	/// pitch is not the same errand as driving between towns (DistributionHours, 4h) -- it's a quick
	/// in-town stop, longer for an op working a whole route than a clerk at one counter.</summary>
	public static int EstimatedStopHours(StopKind kind) => kind == StopKind.Op ? 2 : 1;

	/// <summary>Real time cost of working one account, rolled fresh each visit: mostly the estimate,
	/// sometimes a quick in-and-out, sometimes the owner wants to talk or the route's backed up. A flat
	/// 4h for a five-minute counter pitch (the old DistributionHours reuse) wasn't a realistic price for
	/// "dealing with one record shop in your hometown."</summary>
	private static int StopVisitHours(StopKind kind) {
		int baseHours = EstimatedStopHours(kind);
		float roll = GD.Randf();
		if (roll < 0.15f) return Mathf.Max(1, baseHours - 1);
		if (roll > 0.80f) return baseHours + 1;
		return baseHours;
	}

	private bool ValidateStopAction(string stopId, string recordId, out PlayerStop stop, out PressStock stockOnHand, out string message) {
		stop = null; stockOnHand = null; message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		stop = GetStop(stopId);
		if (stop == null) { message = "No such account."; return false; }
		if (stop.Kind == StopKind.OneStop) { message = $"{stop.DisplayName} is a one-stop counter -- sell them a carton instead."; return false; }
		if (stop.Kind == StopKind.Venue) { message = $"{stop.DisplayName} doesn't keep a shelf -- work the table instead."; return false; }
		if (stop.Kind == StopKind.Station) { message = $"{stop.DisplayName} doesn't buy records -- service it instead."; return false; }
		if (stop.CityId != CurrentCityId) { message = "You have to be in town to work that account."; return false; }
		if (string.IsNullOrEmpty(recordId)) { message = "Pick a single to leave here."; return false; }
		if (IsMasterOut(recordId)) { message = $"\"{TitleForRecord(recordId)}\" isn't yours to sell right now -- the master's out."; return false; }
		stockOnHand = StockFor(recordId);
		if (stockOnHand == null || stockOnHand.Remaining <= 0) { message = "None of that pressed on hand -- order a run and let it come in first."; return false; }
		return true;
	}

	private ConsignmentLot LotFor(PlayerStop stop, string recordId) {
		if (!stop.OnHand.TryGetValue(recordId, out ConsignmentLot lot)) { lot = new ConsignmentLot(); stop.OnHand[recordId] = lot; }
		return lot;
	}

	private void TouchStop(PlayerStop stop, float relationshipGain) {
		stop.Relationship = Mathf.Clamp(stop.Relationship + relationshipGain, 0f, 1f);
		stop.LastVisitWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? stop.LastVisitWeek;
		stop.MissedCallStreak = 0; // you just showed up -- whatever streak of no-shows was building breaks here
		// Directive §7.1: "a shop conversation reveals who reports." You worked this counter in person --
		// the ledger by the register and the fact he takes a call from the station on Tuesdays are not
		// things a man hides from the label rep standing in front of him.
		LearnWhoReports(stop);
	}

	/// <summary>Marks a reporting stop as KNOWN to report. Silent and idempotent -- the discovery is only
	/// ever a display change, never a gate on the verbs themselves (those already gate on stock,
	/// sell-through and relationship, all of which take more visits than this does).</summary>
	private void LearnWhoReports(PlayerStop stop) {
		if (stop == null || !stop.ReportsToTrades && stop.ReportsToStationIds.Count == 0) return;
		knownReportingStopIds.Add(stop.StopId);
	}

	/// <summary>Whether the player has worked out that this stop keeps a report -- see
	/// <see cref="LearnWhoReports"/>. The UI hides the "reports" flag until this is true.</summary>
	public bool KnowsWhoReports(string stopId) =>
		!string.IsNullOrEmpty(stopId) && knownReportingStopIds.Contains(stopId);

	/// <summary>One approach (Pitch or Consign) per stop per day -- a player standing at the counter does
	/// not get to ask twice hoping for a better roll. Does not gate Service; topping off a standing
	/// account twice in a day is a no-op, not an exploit.</summary>
	private bool CheckDailyApproach(PlayerStop stop, out string message) {
		message = null;
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		if (stop.LastApproachDate == today) {
			message = $"Already talked to {stop.DisplayName} today -- one approach a day per account.";
			return false;
		}
		return true;
	}

	/// <summary>
	/// COD: ask the stop to take copies outright. A cold account can say no, or take only a handful --
	/// refusal is common and correct, the honest cost of a first visit (directive §3.3). Placed stock
	/// sells on the normal daily trickle; present-in-town units pay cash on the spot the same as before,
	/// the terms just live on the account now, not the whole city.
	/// </summary>
	public bool PitchAtStop(string stopId, string recordId, out string message) {
		if (!ValidateStopAction(stopId, recordId, out PlayerStop stop, out PressStock stockOnHand, out message)) return false;
		if (!CheckDailyApproach(stop, out message)) return false;
		if (stop.PassedRecordIds.Contains(recordId)) {
			message = $"{stop.DisplayName} already passed on \"{TitleForRecord(recordId)}\" -- nothing's changed there since. Try consignment, or wait for it to catch on somewhere.";
			return false;
		}
		int hours = StopVisitHours(stop.Kind);
		if (!Require(hours, out message)) return false;
		Spend(hours);
		stop.LastApproachDate = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		// You're here, so first settle up whatever this town's stops have been holding since your last pass.
		CollectAtCity(stop.CityId);

		bool untried = !stop.OnHand.ContainsKey(recordId);
		float access = RetailAccessFactor(stop, recordId);
		float acceptChance = Mathf.Clamp((0.35f + stop.Relationship * 0.6f) * access, 0.05f, 0.97f);
		if (untried && GD.Randf() > acceptChance) {
			TouchStop(stop, 0.02f); // a passed call still counts as an introduction
			stop.PassedRecordIds.Add(recordId); // sticks until GenerateInboundCalls clears it on real evidence
			// A market that isn't open to the sound at all yet (directive §10) reads differently than an
			// ordinary cold-shop no -- both clear the same way (a real call in reopens the pass), but the
			// player shouldn't mistake one for the other.
			string passReason = access < 0.6f ? "that trade hasn't found its way to this side of town yet" : "come back once it's played somewhere";
			Note($"{stop.DisplayName} in {CityName(stop.CityId)} passed on \"{TitleForRecord(recordId)}\" -- \"{passReason}.\"");
			message = $"{stop.DisplayName} passed on \"{TitleForRecord(recordId)}\" this time ({hours}h).";
			Changed?.Invoke();
			return true;
		}

		// The stop decides its own take (§3.3's cold-shop-vs-standing-account math), not a number the
		// player dials in -- SuggestedPlacement already IS "how much they'll take." Scaled by the same
		// access term (directive §10): a shop that's willing at all still starts thin in a market the
		// sound hasn't fully reached.
		int cap = Mathf.Max(1, Mathf.RoundToInt(SuggestedPlacement(stop) * access));
		int place = Mathf.Min(cap, stockOnHand.Remaining);
		stockOnHand.Remaining -= place;
		ConsignmentLot lot = LotFor(stop, recordId);
		lot.Remaining += place;
		lot.Placed = lot.Remaining;
		lot.DaysSinceRestock = 0;
		lot.ConsignmentTerms = false;
		lot.RunnerSourced = false; // the player's own hand, the player's own stock
		TouchStop(stop, 0.05f);
		workedCities.Add(stop.CityId);

		string callNote = TryFulfillCall(stop, recordId);
		Note($"{stop.DisplayName} took {place:N0} of \"{TitleForRecord(recordId)}\" COD.");
		message = $"{stop.DisplayName} took {place:N0} of \"{TitleForRecord(recordId)}\" ({hours}h).{callNote}";
		Changed?.Invoke();
		return true;
	}

	/// <summary>
	/// Leave stock on consignment: worse cash -- every dollar waits in the stop's OpenBalance, even in a
	/// town you're standing in, never handed over on the spot -- but the low-risk ask a cold account will
	/// always take a few of. Available even on a title this stop has passed on COD -- that refusal is
	/// specifically a "not paying up front for that" no, not a "not carrying it at all" no.
	/// </summary>
	public bool ConsignAtStop(string stopId, string recordId, out string message) {
		if (!ValidateStopAction(stopId, recordId, out PlayerStop stop, out PressStock stockOnHand, out message)) return false;
		if (!CheckDailyApproach(stop, out message)) return false;
		int hours = StopVisitHours(stop.Kind);
		if (!Require(hours, out message)) return false;
		Spend(hours);
		stop.LastApproachDate = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		CollectAtCity(stop.CityId);

		// Same as PitchAtStop -- the account's own capacity sets the amount, not a player-chosen quantity,
		// scaled by the same directive-§10 access term (a market the sound hasn't reached takes less even
		// on the free-to-them consignment ask).
		float access = RetailAccessFactor(stop, recordId);
		int cap = Mathf.Max(1, Mathf.RoundToInt((stop.Relationship > 0f ? SuggestedPlacement(stop) : 5f) * access));
		int place = Mathf.Min(cap, stockOnHand.Remaining);
		stockOnHand.Remaining -= place;
		ConsignmentLot lot = LotFor(stop, recordId);
		lot.Remaining += place;
		lot.Placed = lot.Remaining;
		lot.DaysSinceRestock = 0;
		lot.ConsignmentTerms = true;
		lot.RunnerSourced = false; // the player's own hand, the player's own stock
		TouchStop(stop, 0.04f);
		workedCities.Add(stop.CityId);

		string callNote = TryFulfillCall(stop, recordId);
		Note($"Left {place:N0} of \"{TitleForRecord(recordId)}\" with {stop.DisplayName} on consignment.");
		message = $"{place:N0} of \"{TitleForRecord(recordId)}\" out on consignment with {stop.DisplayName} ({hours}h).{callNote}";
		Changed?.Invoke();
		return true;
	}

	/// <summary>
	/// Service a standing account: restock what's sold through, collect what it owes, and take the
	/// biggest relationship tick in the game. Only makes sense once a stop already has history with the
	/// single -- pitch or consign it there first.
	/// </summary>
	public bool ServiceStop(string stopId, string recordId, out string message) {
		if (!ValidateStopAction(stopId, recordId, out PlayerStop stop, out PressStock stockOnHand, out message)) return false;
		if (!stop.OnHand.ContainsKey(recordId)) { message = $"{stop.DisplayName} has no history with that single yet -- pitch or consign it first."; return false; }
		int hours = StopVisitHours(stop.Kind);
		if (!Require(hours, out message)) return false;
		Spend(hours);

		float owed = stop.OpenBalance;
		CollectAtCity(stop.CityId); // this stop's balance, plus any other stop in town holding money for you

		ConsignmentLot lot = LotFor(stop, recordId);
		int target = Mathf.Max(1, Mathf.RoundToInt(SuggestedPlacement(stop) * RetailAccessFactor(stop, recordId)));
		int topUp = Mathf.Clamp(target - lot.Remaining, 0, stockOnHand.Remaining);
		stockOnHand.Remaining -= topUp;
		lot.Remaining += topUp;
		lot.Placed = Mathf.Max(lot.Placed, lot.Remaining);
		lot.DaysSinceRestock = 0;
		lot.RunnerSourced = false; // the player's own hand, the player's own stock
		TouchStop(stop, 0.08f);
		string callNote = TryFulfillCall(stop, recordId);
		NoteReorder(stop.CityId); // directive §7: "persistent reorders in one city" toward the runner unlock

		message = (topUp > 0
			? $"Serviced {stop.DisplayName} -- topped up {topUp:N0}{(owed > 0.5f ? $", collected ${owed:N0}" : "")} ({hours}h)."
			: owed > 0.5f ? $"Serviced {stop.DisplayName} -- collected ${owed:N0} ({hours}h)." : $"Serviced {stop.DisplayName} -- already stocked, nothing owed ({hours}h).") + callNote;
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>
	/// Directive §3.3: "Work the hop table -- Venue. Retail cash (~list), tiny volume, story." A walk-up
	/// sale at a hop, club or church hall: cash in hand at list price the instant it happens, no
	/// consignment ledger and no standing account to come back and service -- the table just doesn't hold
	/// enough to scale past a handful. Reuses BookTrunkSale's own accounting (list price, royalty,
	/// recoupment, chart units) rather than inventing a second pricing path for the same kind of sale.
	/// Deliberately NOT scaled by RetailAccessFactor (directive §10): a church basement or a soul revue was
	/// never waiting on white retail's integration curve to open its own door -- it's the always-open
	/// community channel that gate exists in contrast to, not another market it gates.
	/// </summary>
	public bool WorkTheHopTable(string stopId, string recordId, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		PlayerStop stop = GetStop(stopId);
		if (stop == null || stop.Kind != StopKind.Venue) { message = "No such table."; return false; }
		if (stop.CityId != CurrentCityId) { message = "You have to be in town for that."; return false; }
		if (!CheckDailyApproach(stop, out message)) return false;
		if (string.IsNullOrEmpty(recordId)) { message = "Pick a single to sell."; return false; }
		if (IsMasterOut(recordId)) { message = $"\"{TitleForRecord(recordId)}\" isn't yours to sell right now -- the master's out."; return false; }
		PressStock stockOnHand = StockFor(recordId);
		if (stockOnHand == null || stockOnHand.Remaining <= 0) { message = "None of that pressed on hand -- order a run and let it come in first."; return false; }
		RecordRuntimeData rec = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId);
		if (rec == null) { message = "Can't sell that one here."; return false; }
		int hours = StopVisitHours(stop.Kind);
		if (!Require(hours, out message)) return false;
		Spend(hours);
		stop.LastApproachDate = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;

		int qty = Mathf.Min(SuggestedPlacement(stop), stockOnHand.Remaining);
		stockOnHand.Remaining -= qty;
		BookTrunkSale(rec, qty, stop, cashNow: true);
		TouchStop(stop, 0.06f); // no shelf to keep here, but the table remembers a familiar face
		workedCities.Add(stop.CityId);

		Note($"Sold {qty:N0} of \"{TitleForRecord(recordId)}\" off the table at {stop.DisplayName}.");
		message = $"Sold {qty:N0} of \"{TitleForRecord(recordId)}\" at the table, cash in hand ({hours}h).";
		Changed?.Invoke();
		return true;
	}

	/// <summary>
	/// Directive §6: a one-stop is "locked as a customer until inbound demand exists" -- this is that
	/// unlock. Only makes sense once GenerateInboundCalls has actually rung this counter
	/// (InboundCallReason.OneStopTest); the visit itself is a formality once called ("capacity is almost
	/// never the no" -- §5.2's logic, reused here), not a second roll on top of the call's own.
	/// </summary>
	public bool VisitOneStopWarehouse(string stopId, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		PlayerStop stop = GetStop(stopId);
		if (stop == null || stop.Kind != StopKind.OneStop) { message = "No such one-stop."; return false; }
		if (stop.CityId != CurrentCityId) { message = "You have to be in town for that."; return false; }
		if (stop.OneStopUnlocked) { message = $"Already doing business with {stop.DisplayName}."; return false; }
		InboundCall call = inboundCalls.FirstOrDefault(c => c.StopId == stop.StopId && c.Reason == InboundCallReason.OneStopTest);
		if (call == null) { message = $"{stop.DisplayName} doesn't know you yet -- wait for them to call."; return false; }
		if (!Require(OneStopWarehouseVisitHours, out message)) return false;
		Spend(OneStopWarehouseVisitHours);

		stop.OneStopUnlocked = true;
		string recordId = call.RecordId;
		TryFulfillCall(stop, recordId);
		Note($"{stop.DisplayName} will take a carton of \"{TitleForRecord(recordId)}\" -- your first metro wholesale account.");
		message = $"{stop.DisplayName} is open for business ({OneStopWarehouseVisitHours}h).";
		Changed?.Invoke();
		return true;
	}

	/// <summary>
	/// Directive §6: "you sell a carton; they scatter it to shops/ops you never meet." A flat wholesale
	/// sale of physical inventory, not a standing line into the regional store engine (that machinery is
	/// AI-side and out of scope -- §0). COD on the first carton ("if you're nobody"); once trusted, net
	/// terms via the same WholesaleReceivable ledger the house line already uses, so settlement pays it
	/// out automatically on schedule with no new plumbing.
	/// </summary>
	public bool SellCartonToOneStop(string stopId, string recordId, int quantity, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		PlayerStop stop = GetStop(stopId);
		if (stop == null || stop.Kind != StopKind.OneStop) { message = "No such one-stop."; return false; }
		if (!stop.OneStopUnlocked) { message = $"{stop.DisplayName} isn't taking calls yet."; return false; }
		if (stop.CityId != CurrentCityId) { message = "You have to be in town for that."; return false; }
		if (IsMasterOut(recordId)) { message = $"\"{TitleForRecord(recordId)}\" isn't yours to sell right now -- the master's out."; return false; }
		PressStock stockOnHand = StockFor(recordId);
		if (stockOnHand == null || stockOnHand.Remaining <= 0) { message = "None of that pressed on hand -- order a run and let it come in first."; return false; }
		RecordRuntimeData rec = ReleasedRecords.FirstOrDefault(r => r.baseRecord.recordId == recordId);
		if (rec == null) { message = "Can't place that one."; return false; }
		int qty = Mathf.Clamp(quantity, 1, Mathf.Min(OneStopCartonMaxQty, stockOnHand.Remaining));
		if (!Require(OneStopWarehouseVisitHours, out message)) return false;
		Spend(OneStopWarehouseVisitHours);

		// Same royalty/recoupment bookkeeping as a trunk sale (BookTrunkSale) -- only the unit price and
		// the cash-timing rule differ (wholesale, and COD-vs-receivable rather than present-vs-away).
		stockOnHand.Remaining -= qty;
		float gross = qty * OneStopUnitPrice;
		SimulatedArtist artist = ArtistManager.Instance?.GetArtist(rec.baseRecord.artistId);
		float accrued = gross * (artist?.royaltyRate ?? 0.05f);
		float recouped = artist != null ? Mathf.Min(Mathf.Max(0f, artist.unrecoupedAdvance), accrued) : 0f;
		float royalty = accrued - recouped;
		if (artist != null) {
			artist.unrecoupedAdvance = Mathf.Max(0f, artist.unrecoupedAdvance - recouped);
			artist.totalRoyaltyEarnings += royalty;
		}
		float net = gross - royalty;
		rec.lifetimeLabelNet += net;
		// Scattered to shops/ops the player never meets, but it charts the same -- reuse the trunk's own
		// chart-injection accumulator (TakeWeeklyTrunkUnits) rather than the AI-side regional store engine
		// (out of scope, §0). Gross/royalty/units book as earned this week either way; only the cash timing
		// differs below, mirrored on weeklyTrunkHeld's existing "earned but not yet banked" role.
		weeklyTrunkUnits.TryGetValue(recordId, out int runningUnits);
		weeklyTrunkUnits[recordId] = runningUnits + qty;
		weeklyTrunkUnitsSold += qty;
		weeklyTrunkGross += gross;
		weeklyTrunkRoyalty += royalty;

		bool cod = !stop.OneStopTrusted;
		if (cod) {
			Label.cashReserves += net;
			Label.monthlyRevenue += net;
		} else {
			int dueWeek = (ChartManager.Instance?.GetCurrentChartWeek() ?? 0) + OneStopPaymentTermWeeks;
			Label.wholesaleReceivables.Add(new WholesaleReceivable(dueWeek, $"onestop_{stop.StopId}", net));
			Label.outstandingWholesaleReceivables += net;
			weeklyTrunkHeld += net; // not yet banked -- settlement collects it automatically at dueWeek
		}
		stop.OneStopTrusted = true;
		oneStopKnownRecordIds.Add(recordId); // §9's master-deal gate: "a one-stop knows the title"

		string termsNote = cod ? "COD" : $"net terms, due in {OneStopPaymentTermWeeks} weeks";
		Note($"Sold {qty:N0} of \"{TitleForRecord(recordId)}\" to {stop.DisplayName} ({termsNote}).");
		message = $"{stop.DisplayName} took {qty:N0} on {termsNote} ({OneStopWarehouseVisitHours}h).";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Pressed singles sitting in the office, ready to carry out to a town. (recordId, title, on hand).</summary>
	public IEnumerable<(string RecordId, string Title, int OnHand)> PressedSinglesOnHand() {
		foreach (var kv in inventory)
			if (kv.Value.Remaining > 0) yield return (kv.Key, TitleForRecord(kv.Key), kv.Value.Remaining);
	}

	/// <summary>This act's own pressed singles sitting in the office -- what they could buy in for
	/// cash (see <see cref="ArtistBuyIn"/>). Only records enough on hand to actually make a buy-in
	/// (§3.3's 50-100 floor) are worth showing.</summary>
	public IEnumerable<(string RecordId, string Title, int OnHand)> BuyInEligibleFor(SimulatedArtist artist) {
		if (artist == null) yield break;
		foreach (var kv in inventory) {
			if (kv.Value.Remaining < ArtistBuyInMin) continue;
			RecordRuntimeData rec = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == kv.Key);
			if (rec == null || rec.baseRecord.artistId != artist.artistId) continue;
			yield return (kv.Key, TitleForRecord(kv.Key), kv.Value.Remaining);
		}
	}

	/// <summary>Every act on the roster with a buy-in-eligible single on hand -- the DISTRIBUTION-window
	/// version of <see cref="BuyInEligibleFor"/>, spanning the whole roster instead of one act's dossier.</summary>
	public IEnumerable<(SimulatedArtist Artist, string RecordId, string Title, int OnHand)> BuyInEligible() {
		foreach (SimulatedArtist artist in Roster)
			foreach ((string recordId, string title, int onHand) in BuyInEligibleFor(artist))
				yield return (artist, recordId, title, onHand);
	}

	/// <summary>
	/// An act buys a run of its own single outright at a wholesale-grade discount, cash to the label
	/// right now (directive §3.3: "a one-stop with legs"). No stop, no consignment, no chart credit --
	/// there's nowhere to attribute the sale to, since the act moves these on its own -- and no royalty
	/// on units it bought itself; the discount is the trade the act gets instead.
	/// </summary>
	public bool ArtistBuyIn(SimulatedArtist artist, string recordId, int quantity, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (!RequireHome(out message)) return false;
		if (artist == null || !Roster.Contains(artist)) { message = "Not one of your acts."; return false; }
		RecordRuntimeData rec = string.IsNullOrEmpty(recordId) ? null
			: ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId);
		if (rec == null || rec.baseRecord.artistId != artist.artistId) { message = $"That single isn't {artist.stageName}'s."; return false; }
		if (IsMasterOut(recordId)) { message = $"\"{TitleForRecord(recordId)}\" isn't yours to sell right now -- the master's out."; return false; }
		PressStock stock = StockFor(recordId);
		if (stock == null || stock.Remaining < ArtistBuyInMin) {
			message = $"Not enough of \"{TitleForRecord(recordId)}\" on hand -- {artist.stageName} won't bother for under {ArtistBuyInMin}.";
			return false;
		}
		if (!Require(ArtistBuyInHours, out message)) return false;

		int take = Mathf.Clamp(quantity, ArtistBuyInMin, Mathf.Min(ArtistBuyInMax, stock.Remaining));
		Spend(ArtistBuyInHours);
		stock.Remaining -= take;
		float net = take * ArtistBuyInPrice;
		Label.cashReserves += net;
		Label.monthlyRevenue += net;

		Note($"{artist.stageName} bought {take:N0} of \"{TitleForRecord(recordId)}\" outright, cash -- ${net:N0}.");
		message = $"{artist.stageName} takes {take:N0} of \"{TitleForRecord(recordId)}\" for ${net:N0} cash.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Stock on hand at each named stop, for the DISTRIBUTION readout.</summary>
	public IEnumerable<(string CityName, string StopName, string Title, int Remaining)> StopStock() {
		foreach (PlayerStop stop in EnsureStops().Values)
			foreach (var (recordId, lot) in stop.OnHand)
				if (lot.Remaining > 0) yield return (CityName(stop.CityId), stop.DisplayName, TitleForRecord(recordId), lot.Remaining);
	}

	// ========================================================================
	// INBOUND DEMAND -- "they called me" (directive §4). Today, trunk is 100% player-push; this is the
	// seam that lets a title with real local demand pull the player back to a shelf instead of dying at
	// "a little bit on the radio, then nothing." Every signal below reads RecordRuntimeData.regionalData
	// (the same state the AI's regional-breakout math already uses) or the player's own stop roster --
	// never a parallel buzz meter (§4.1).
	// ========================================================================

	/// <summary>$5-10/mo, buys the office an "on" switch for InboundCalls generated while the player is
	/// on the road (§4.3) -- without it, only calls generated while at the home office get logged; the
	/// world still moves, the office just never hears about it.</summary>
	public bool HasAnsweringService => Label?.hasAnsweringService ?? false;

	public bool PurchaseAnsweringService(out string message) {
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (Label.hasAnsweringService) { message = "Already got one."; return false; }
		Label.hasAnsweringService = true;
		Note($"Hired an answering service -- ${AILabel.AnsweringServiceMonthlyCost:N0}/mo -- to catch calls while you're out.");
		message = $"Answering service hired (${AILabel.AnsweringServiceMonthlyCost:N0}/mo).";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Pending calls for the OFFICE readout, soonest-to-expire first.</summary>
	public IEnumerable<(InboundCall Call, string StopName, string CityName, string Title, int ExpiresInWeeks)> PendingCalls() {
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		foreach (InboundCall call in inboundCalls.OrderBy(c => c.ExpiresWeek)) {
			PlayerStop stop = GetStop(call.StopId);
			if (stop == null) continue;
			yield return (call, stop.DisplayName, CityName(stop.CityId), TitleForRecord(call.RecordId), Mathf.Max(0, call.ExpiresWeek - week));
		}
	}

	/// <summary>Whether the given stop has an open call for that single -- lets the DISTRIBUTION stop
	/// rows flag "they called" so the player doesn't have to cross-reference the office list by hand.</summary>
	public bool HasOpenCall(string stopId, string recordId) =>
		inboundCalls.Any(c => c.StopId == stopId && c.RecordId == recordId);

	/// <summary>Runs at most once per chart week (from OnDayStarted): expires anything overdue, then
	/// rolls for a fresh batch. A week boundary, not a daily one -- InboundCalls are lower-frequency,
	/// office-readout events, not another daily-tick system layered on top of the trunk.</summary>
	private void CheckWeeklyInboundCalls() {
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		if (week == lastCallGenWeek) return;
		lastCallGenWeek = week;
		ExpireInboundCalls(week);
		GenerateInboundCalls(week);
		MaybeUnlockRunnerFromCalls(week);
	}

	/// <summary>Directive §7's other unlock path: "inbound calls in two cities the same week." Reads what
	/// GenerateInboundCalls just added rather than tracking a parallel counter.</summary>
	private void MaybeUnlockRunnerFromCalls(int week) {
		if (runnerUnlocked) return;
		int distinctCities = inboundCalls.Where(c => c.Week == week)
			.Select(c => GetStop(c.StopId)?.CityId).Where(id => id != null).Distinct(StringComparer.Ordinal).Count();
		if (distinctCities >= RunnerUnlockCallCities) UnlockRunner("Calls came in from two towns the same week");
	}

	/// <summary>Directive §4.2: "they fill it from someone else, or forget." An account you already know
	/// pays a relationship cost for going unanswered; a stranger shop just evaporates -- there was no
	/// relationship there to spend. A single miss is ordinary -- shops and ops call around, they don't
	/// expect you to jump every time -- so the first one barely registers. Missing the same account
	/// again and again is neglect, and the penalty escalates with the streak (MissedCallBasePenalty ×
	/// MissedCallStreak, capped at MissedCallMaxPenalty).</summary>
	private void ExpireInboundCalls(int week) {
		foreach (InboundCall call in inboundCalls.Where(c => c.ExpiresWeek <= week).ToList()) {
			inboundCalls.Remove(call);
			PlayerStop stop = GetStop(call.StopId);
			if (stop == null) continue;
			if (stop.LastVisitWeek > 0) {
				stop.MissedCallStreak++;
				float penalty = Mathf.Min(MissedCallBasePenalty * stop.MissedCallStreak, MissedCallMaxPenalty);
				stop.Relationship = Mathf.Max(0f, stop.Relationship - penalty);
				Note($"{stop.DisplayName} in {CityName(stop.CityId)} gave up waiting on \"{TitleForRecord(call.RecordId)}\" -- filled it elsewhere.");
			}
		}
	}

	/// <summary>Regions the office can plausibly hear from this week: home, next door to home, and next
	/// door to anywhere the player has already worked a stop. Mirrors the reach a driving label actually
	/// has (CanReach) -- a call from clear across the map with no road between here and there would read
	/// as noise, not a hook back into the map.</summary>
	private HashSet<string> CallEligibleRegions() {
		var regions = new HashSet<string>(StringComparer.Ordinal);
		if (Label == null) return regions;
		void AddWithNeighbors(string regionId) {
			if (string.IsNullOrEmpty(regionId) || !regions.Add(regionId)) return;
			foreach (string adjacent in DistanceModel.GetAdjacentRegions(regionId)) regions.Add(adjacent);
		}
		AddWithNeighbors(Label.homeRegion);
		foreach (PlayerStop stop in EnsureStops().Values)
			if (stop.LastVisitWeek > 0) AddWithNeighbors(DistanceModel.GetCityById(stop.CityId)?.parentRegionId);
		return regions;
	}

	/// <summary>
	/// Rolls this week's batch of "they called me." Two sources, matching the first two rungs of
	/// directive §4.2's priority order (the later rungs -- one-stop test carton, house courting -- hang
	/// off §5/§6, not yet built):
	///   1. SoldOut -- an account you already stocked ran thin while the region still shows real demand.
	///   2. Requests/StationAdded -- a shop you've never visited, only where regional awareness or
	///      airplay is real enough to make "a stranger called" plausible (the request loop).
	/// Without an answering service, a call generated while the player is away from the office is never
	/// logged at all -- §4.3's missed-call pressure, not solved with omniscient voicemail.
	/// </summary>
	private void GenerateInboundCalls(int week) {
		if (Label == null) return;
		if (!AtHome && !HasAnsweringService) return;

		HashSet<string> eligibleRegions = CallEligibleRegions();
		Dictionary<string, PlayerStop> allStops = EnsureStops();

		foreach (RecordRuntimeData rec in ReleasedRecords) {
			if (rec?.baseRecord == null) continue;
			string recordId = rec.baseRecord.recordId;
			int strangerCallsThisRecord = 0;

			foreach (var pair in rec.regionalData) {
				string regionId = pair.Key;
				RegionalRecordData rd = pair.Value;
				if (rd == null || !eligibleRegions.Contains(regionId)) continue;

				List<PlayerStop> stopsHere = allStops.Values.Where(s =>
					string.Equals(DistanceModel.GetCityById(s.CityId)?.parentRegionId, regionId, StringComparison.Ordinal)).ToList();
				if (stopsHere.Count == 0) continue;

				bool demandReal = rd.awareness > 0.15f || rd.radioPlay > 0.10f || rd.unitsBackordered > 0;

				// 1. SoldOut -- known accounts, thin on hand, demand still there.
				foreach (PlayerStop stop in stopsHere) {
					if (stop.LastVisitWeek == 0) continue;
					if (HasOpenCall(stop.StopId, recordId)) continue;
					if (!stop.OnHand.TryGetValue(recordId, out ConsignmentLot lot) || lot.Placed <= 0) continue;
					if (lot.Remaining > SoldOutOnHandThreshold || !demandReal) continue;
					AddCall(stop, recordId, SuggestedPlacement(stop), InboundCallReason.SoldOut, week, lot.ConsignmentTerms);
				}

				// 2. The stranger shop -- never carried THIS title, real regional signal, the request loop.
				// "Never carried" rather than "never visited" so a stop that passed on this one COD (§3.3
				// addendum: a pass sticks) is exactly as reachable here as a true never-visited shop once
				// the record earns real evidence -- this call is the "potential success" that reopens it.
				if (strangerCallsThisRecord < StrangerCallsPerRecordPerWeek) {
					bool strangerSignal = rd.awareness >= StrangerAwarenessThreshold || rd.radioPlay >= StrangerRadioThreshold;
					if (strangerSignal) {
						// Venue excluded alongside OneStop: it keeps no OnHand history to be "untried" against,
						// so it would otherwise qualify on every pass and ring a call its own verb (WorkTheHopTable)
						// has no ledger to fulfill against -- the table is a walk-up, not a standing account.
						List<PlayerStop> strangers = stopsHere.Where(s =>
							s.Kind != StopKind.OneStop && s.Kind != StopKind.Venue && s.Kind != StopKind.Station
							&& IsUntriedAt(s, recordId) && !HasOpenCall(s.StopId, recordId)).ToList();
						// Promo mechanic directive §6.1/§6.3: a live trade pick or breakout listing is the
						// loudest inbound generator in the game -- it multiplies this roll, never units directly.
						if (strangers.Count > 0 && GD.Randf() < Mathf.Min(1f, StrangerCallChancePerWeek * TradeInboundCallMultiplier(recordId))) {
							PlayerStop pick = strangers[GD.RandRange(0, strangers.Count - 1)];
							InboundCallReason reason = rd.radioPlay >= StrangerRadioThreshold ? InboundCallReason.StationAdded : InboundCallReason.Requests;
							AddCall(pick, recordId, SuggestedPlacement(pick), reason, week, consignment: true);
							strangerCallsThisRecord++;
						}
					}
				}

				// 3. OneStopTest (directive §6): "the natural unlock is an op or dealer they already serve
				// asking for the record." A one-stop grew up serving jukebox ops, so the trigger is a
				// known account in the one-stop's OWN CITY already carrying the title with a real
				// relationship, on top of the same regional demand signal the stranger call needs.
				if (demandReal) {
					foreach (PlayerStop oneStop in stopsHere.Where(s => s.Kind == StopKind.OneStop && !s.OneStopUnlocked)) {
						if (HasOpenCall(oneStop.StopId, recordId)) continue;
						bool servedByKnownAccount = allStops.Values.Any(s => s.CityId == oneStop.CityId && s.Kind != StopKind.OneStop
							&& s.Relationship >= OneStopServedRelationshipFloor
							&& s.OnHand.TryGetValue(recordId, out ConsignmentLot ownLot) && ownLot.Placed > 0);
						if (!servedByKnownAccount || GD.Randf() >= Mathf.Min(1f, StrangerCallChancePerWeek * TradeInboundCallMultiplier(recordId))) continue;
						AddCall(oneStop, recordId, OneStopCartonDefaultQty, InboundCallReason.OneStopTest, week, consignment: false);
					}
				}
			}
		}
	}

	/// <summary>Never carried this title -- covers both a true never-visited stop and one that passed on
	/// it COD (a pass sticks on <see cref="PlayerStop.PassedRecordIds"/> until a call like this clears it,
	/// per the §3.3 addendum), since neither has ever actually had it on the shelf.</summary>
	private static bool IsUntriedAt(PlayerStop stop, string recordId) =>
		!stop.OnHand.TryGetValue(recordId, out ConsignmentLot lot) || lot.Placed <= 0;

	private void AddCall(PlayerStop stop, string recordId, int qty, InboundCallReason reason, int week, bool consignment) {
		stop.PassedRecordIds.Remove(recordId); // a call in is the "potential success" that reopens a pass
		inboundCalls.Add(new InboundCall {
			StopId = stop.StopId, RecordId = recordId, Week = week, RequestedQty = Mathf.Max(1, qty),
			Reason = reason, ExpiresWeek = week + CallExpiryWeeks, ConsignmentTerms = consignment
		});
		string reasonText = reason switch {
			InboundCallReason.SoldOut => "sold out and wants more",
			InboundCallReason.StationAdded => "it's on the air there and the counter's fielding requests",
			InboundCallReason.Requests => "getting asked for it at the counter",
			InboundCallReason.OneStopTest => "heard about it from an account they serve and want a look",
			_ => "wants in on it"
		};
		Note($"{stop.DisplayName} in {CityName(stop.CityId)} called -- {reasonText} on \"{TitleForRecord(recordId)}\".");
	}

	/// <summary>Answering a call is the highest-trust relationship tick in the game (directive §4.2) --
	/// called from PitchAtStop/ConsignAtStop/ServiceStop whenever the stop being worked has an open call
	/// for that single, on top of that verb's own relationship gain.</summary>
	private string TryFulfillCall(PlayerStop stop, string recordId) {
		InboundCall call = inboundCalls.FirstOrDefault(c => c.StopId == stop.StopId && c.RecordId == recordId);
		if (call == null) return null;
		inboundCalls.Remove(call);
		TouchStop(stop, 0.06f);
		return " You got there before they gave up waiting.";
	}

	// ========================================================================
	// PEOPLE -- contractors first, payroll later (directive §7). A commission trunk runner covers a
	// route of the player's own named stops off his own carton, at a worse starting conversion that
	// improves the more he services an account, paid only out of what he actually collects (no weekly
	// nut). A project promo man is a short, city-scoped, record-specific radio push that sells no units
	// -- it reuses the existing PayolaLedger/IndiePromoter machinery (already player-facing, and already
	// carrying the scandal/detection risk model §7 asks for) rather than inventing a second one.
	// ========================================================================

	public bool RunnerUnlocked => runnerUnlocked;
	public bool HasRunner => runner != null;
	/// <summary>The hired runner's live state, or null -- exposed directly for the same reason PlayerStop
	/// is (see StopsInCity): SaveLoadRoundTripRunner hand-mutates it to prove CaptureState/RestoreState
	/// round-trip his carton/route/familiarity, the same way it already does for stop state.</summary>
	public PlayerRunner Runner => runner;
	public IReadOnlyCollection<string> RunnerRouteStopIds => runner?.RouteStopIds ?? (IReadOnlyCollection<string>)Array.Empty<string>();
	public string RunnerCartonRecordId => runner?.CartonRecordId;
	public int RunnerCartonRemaining => runner?.CartonRemaining ?? 0;
	public bool IsOnRunnerRoute(string stopId) => runner != null && runner.RouteStopIds.Contains(stopId);

	/// <summary>Test-only seam (mirrors TimeManager.DebugAdvanceWeek): satisfies the §7 unlock directly
	/// rather than making a probe grind out real reorders or a two-city call week. Optionally marks a city
	/// worked too, since AssignRunnerStop requires it and the probe never runs the trunk-selling verbs
	/// that would set it organically.</summary>
	public void DebugUnlockRunner(string alsoMarkCityWorked = null) {
		runnerUnlocked = true;
		if (!string.IsNullOrEmpty(alsoMarkCityWorked)) workedCities.Add(alsoMarkCityWorked);
	}

	/// <summary>Test-only seam: hand-constructs an outstanding plant credit, bypassing the real
	/// open-call-backlog eligibility gate (RequestPlantCredit) -- lets a probe prove PlantCredit
	/// round-trips through CaptureState/RestoreState without grinding out a real backlog first.</summary>
	public void DebugSetPlantCredit(PlantCredit credit) => plantCredit = credit;

	/// <summary>Test-only seams for directive §9: mark a title's one-stop exposure directly rather than
	/// releasing a real record and running it through SellCartonToOneStop, and stage an unsigned offer
	/// on the desk without grinding out real regional-breakout evidence first -- same spirit as
	/// DebugSetPlantCredit above.</summary>
	public void DebugMarkOneStopKnown(string recordId) => oneStopKnownRecordIds.Add(recordId);
	public void DebugSetMasterSold(string recordId) => soldMasterRecordIds.Add(recordId);
	public void DebugSetMasterLeased(string recordId, int expiryWeek) => leasedMasterExpiryWeek[recordId] = expiryWeek;
	public void DebugSetPendingDistributionOffer(DistributionDeal offer) => pendingDistributionOffer = offer;

	/// <summary>Test-only seams for the promo mechanic directive's persisted state (§3.1 promo stock,
	/// §3.2 servicing, §6.1 submissions, §6.2 ads, §7.1 who-reports knowledge). Same spirit as
	/// DebugSetPlantCredit: staging these directly beats pressing a real run, driving a real route and
	/// waiting out a real review-desk resolution just to prove they survive save/load. §14's
	/// verification 2 is specifically that RebuildRadioForLoad doesn't wipe them.</summary>
	public void DebugSetPressStock(string recordId, int remaining, int promoRemaining, int totalPressed, float totalSpent) =>
		inventory[recordId] = new PressStock {
			Remaining = remaining, PromoRemaining = promoRemaining, TotalPressed = totalPressed, TotalSpent = totalSpent
		};
	public void DebugServiceStation(string recordId, string stationId, float conviction, ServicingSource source) =>
		ServiceStation(recordId, stationId, conviction, source);
	public void DebugAddTradeSubmission(TradeSubmission submission) => tradeSubmissions.Add(submission);
	public void DebugAddTradeAd(TradeAd ad) => tradeAds.Add(ad);
	public void DebugLearnWhoReports(string stopId) {
		PlayerStop stop = GetStop(stopId);
		if (stop != null) knownReportingStopIds.Add(stop.StopId);
	}

	/// <summary>A reorder at a standing account, toward "persistent reorders in one city" (directive §7).
	/// Called from ServiceStop -- the restock verb IS a reorder.</summary>
	private void NoteReorder(string cityId) {
		if (runnerUnlocked || string.IsNullOrEmpty(cityId)) return;
		serviceReorderCountByCity.TryGetValue(cityId, out int count);
		count++;
		serviceReorderCountByCity[cityId] = count;
		if (count >= RunnerUnlockReorderCount) UnlockRunner($"{CityName(cityId)} keeps calling you back to restock");
	}

	private void UnlockRunner(string reason) {
		if (runnerUnlocked) return;
		runnerUnlocked = true;
		Note($"You could use a hand covering your accounts -- {reason}. A commission runner is worth a look.");
		Changed?.Invoke();
	}

	public bool HireRunner(out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (!runnerUnlocked) { message = "Nobody's looking to run your route yet -- keep servicing accounts, or let demand ring in from two towns the same week."; return false; }
		if (runner != null) { message = "You've already got a runner."; return false; }
		runner = new PlayerRunner();
		Note("Took on a commission runner -- no salary, just a cut of what he brings back.");
		message = "Runner hired -- give him a route and a carton to work it.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Adds or drops one stop from the runner's route. Restricted to towns the player has
	/// personally opened (workedCities) -- he covers ground you've already broken into, never a way to
	/// open a fresh market without setting foot in it.</summary>
	public bool AssignRunnerStop(string stopId, bool onRoute, out string message) {
		message = null;
		if (runner == null) { message = "No runner to send."; return false; }
		PlayerStop stop = GetStop(stopId);
		if (stop == null || stop.Kind == StopKind.OneStop || stop.Kind == StopKind.Venue || stop.Kind == StopKind.Station) { message = "Not an account he can work."; return false; }
		if (onRoute) {
			if (!workedCities.Contains(stop.CityId)) { message = $"You haven't opened {CityName(stop.CityId)} yourself yet -- work it first."; return false; }
			runner.RouteStopIds.Add(stopId);
			message = $"{stop.DisplayName} added to his route.";
		} else {
			runner.RouteStopIds.Remove(stopId);
			message = $"{stop.DisplayName} taken off his route.";
		}
		Changed?.Invoke();
		return true;
	}

	/// <summary>Hands the runner a carton out of office inventory. He carries one single's worth at a
	/// time -- let him sell through before switching titles.</summary>
	public bool HandCartonToRunner(string recordId, int quantity, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (runner == null) { message = "No runner to hand stock to."; return false; }
		if (runner.CartonRemaining > 0 && runner.CartonRecordId != recordId) {
			message = $"He's still carrying \"{TitleForRecord(runner.CartonRecordId)}\" -- let him sell through first.";
			return false;
		}
		if (!RequireHome(out message)) return false;
		PressStock stock = StockFor(recordId);
		if (stock == null || stock.Remaining <= 0) { message = "Nothing pressed on hand to give him."; return false; }
		if (!Require(RunnerHandoffHours, out message)) return false;

		int take = Mathf.Clamp(quantity, 1, stock.Remaining);
		Spend(RunnerHandoffHours);
		stock.Remaining -= take;
		runner.CartonRecordId = recordId;
		runner.CartonRemaining += take;
		Note($"Handed the runner {take:N0} of \"{TitleForRecord(recordId)}\" to work his route.");
		message = $"He's carrying {runner.CartonRemaining:N0} of \"{TitleForRecord(recordId)}\" now.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Runs at most once per chart week (from OnDayStarted): the runner works every stop on his
	/// route off his own carton -- the same sell/consign/service shape the player's own verbs use
	/// (SuggestedPlacement, TryFulfillCall), just his own worse-starting acceptance curve (Familiarity,
	/// never stop.Relationship) that improves the more he services an account. Costs no player hours --
	/// that is the whole point of him. If his carton runs dry, nothing happens until he's handed more --
	/// "fire him by not handing him stock" (directive §7), no separate dismissal action needed.</summary>
	private void CheckWeeklyRunner() {
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		if (week == lastRunnerTickWeek) return;
		lastRunnerTickWeek = week;
		if (runner == null || runner.CartonRemaining <= 0 || string.IsNullOrEmpty(runner.CartonRecordId)) return;

		string recordId = runner.CartonRecordId;
		Dictionary<string, PlayerStop> allStops = EnsureStops();
		foreach (string stopId in runner.RouteStopIds.ToList()) {
			if (runner.CartonRemaining <= 0) break;
			if (!allStops.TryGetValue(stopId, out PlayerStop stop)) continue;

			runner.Familiarity.TryGetValue(stopId, out float familiarity);
			bool untried = IsUntriedAt(stop, recordId);
			if (untried) {
				float acceptChance = Mathf.Clamp(RunnerAcceptBase + familiarity * RunnerAcceptSlope, 0.08f, 0.9f);
				if (GD.Randf() > acceptChance) {
					stop.PassedRecordIds.Add(recordId);
					Note($"Your runner struck out with {stop.DisplayName} in {CityName(stop.CityId)} on \"{TitleForRecord(recordId)}\".");
					continue;
				}
			}

			int target = Mathf.Max(1, SuggestedPlacement(stop));
			ConsignmentLot lot = LotFor(stop, recordId);
			// A cold account only takes what a worse-conversion cold call earns; a familiar one gets the
			// same real target the player's own Service verb would top it up to.
			int cap = untried ? Mathf.Max(1, Mathf.RoundToInt(target * Mathf.Lerp(0.3f, 1f, familiarity))) : target;
			int place = Mathf.Clamp(cap - lot.Remaining, 0, runner.CartonRemaining);
			if (place <= 0) { TryFulfillCall(stop, recordId); continue; }

			runner.CartonRemaining -= place;
			lot.Remaining += place;
			lot.Placed = Mathf.Max(lot.Placed, lot.Remaining);
			if (untried) lot.DaysSinceRestock = 0;
			lot.RunnerSourced = true;
			TouchStop(stop, RunnerRelationshipGain); // still your label's stock -- a small tick for you too
			runner.Familiarity[stopId] = Mathf.Clamp(familiarity + RunnerFamiliarityGain, 0f, 1f);
			string callNote = TryFulfillCall(stop, recordId);
			Note($"Your runner left {place:N0} of \"{TitleForRecord(recordId)}\" with {stop.DisplayName} in {CityName(stop.CityId)}.{callNote}");
		}
		Changed?.Invoke();
	}

	/// <summary>
	/// Directive §7: "$25-75 ... to work one city / a few stations for 1-2 weeks. Creates spins and
	/// rumors ... sells no units." Reuses the PayolaLedger's IndiePromoter path (already player-facing,
	/// already dormant -- nothing else ever registers a promoter) rather than a second risk model: an
	/// ephemeral promoter scoped to this one project, a handful of this region's reporter stations, a
	/// short duration, and the ledger's own scandal/detection math supplies the "risky" half for free.
	/// Restricted to towns the player has already opened -- proof is geographic (directive §1.3) even for
	/// a phone-arranged spend.
	/// </summary>
	public bool HireProjectPromo(string recordId, string cityId, ProjectPromoTier tier, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (string.IsNullOrEmpty(recordId)) { message = "Pick a record to work."; return false; }
		if (!workedCities.Contains(cityId)) { message = "You haven't opened that town yourself yet -- work it first."; return false; }
		MarketCity city = DistanceModel.GetCityById(cityId);
		MarketRegion region = city != null ? ChartManager.Instance?.GetRegionById(city.parentRegionId) : null;
		if (region == null) { message = "No market data for that town."; return false; }
		RecordRuntimeData rec = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId);
		if (rec == null) { message = "That single isn't out yet."; return false; }

		float cost = ProjectPromoCost(tier);
		if (Label.cashReserves < cost) { message = $"You're ${cost - Label.cashReserves:N0} short of the ${cost:N0} promo fee."; return false; }

		var reporters = (ChartManager.Instance?.ReporterStationsInRegion(region.regionId) ?? Array.Empty<RadioStation>())
			.Where(s => s != null).ToList();
		if (reporters.Count == 0) { message = $"No reporter stations in {region.regionName} to work."; return false; }
		if (!Require(ProjectPromoHours, out message)) return false;

		int stationCount = Mathf.Min(ProjectPromoStationCount(tier), reporters.Count);
		string[] picks = reporters.OrderBy(_ => GD.Randf()).Take(stationCount).Select(s => s.stationId).ToArray();

		Spend(ProjectPromoHours);
		Label.cashReserves -= cost;
		Label.monthlyExpenses += cost;

		// "Temperature from media.payolaSusceptibility" (directive §7) -- a more open market gets more
		// lift for the same fee; the era/station scandal-detection math (PayolaLedger.AdjudicateDetection)
		// already supplies the risk side untouched.
		float susceptibility = Mathf.Clamp(region.media?.payolaSusceptibility ?? 0.3f, 0f, 1f);
		float effectiveness = Mathf.Clamp(ProjectPromoTierEffectiveness(tier) * (0.6f + susceptibility * 0.8f), 0.1f, 0.9f);

		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		GameDate date = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		ChartManager.Instance?.PlaceProjectPromo(recordId, Label.labelId, picks, effectiveness,
			ProjectPromoDiscretion, ProjectPromoMobConnection, week, date.year, date.month, ProjectPromoWeeks);

		Note($"Put a promo man on \"{TitleForRecord(recordId)}\" in {region.regionName} for ${cost:N0} -- {stationCount} station(s), about {ProjectPromoWeeks} weeks.");
		message = $"Promo man working {region.regionName} for about {ProjectPromoWeeks} weeks (${cost:N0}).";
		Changed?.Invoke();
		return true;
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
		CollectAtCity(dest.cityId); // show up and the shops settle what they've been holding
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

	/// <summary>Whether the player has proven a real regional breakout in this market yet (directive
	/// §5.1) -- the same bar the AI's own independent-distribution path uses. A pitch below this comes
	/// back cold and can be turned down outright; at or above it, the house is courting you.</summary>
	public bool IsProvenInRegion(string regionId) =>
		Label != null && (CompetitorManager.Instance?.HasProvenBreakoutIn(Label, regionId) ?? false);

	/// <summary>
	/// Places the label's line with a wholesale house in one market. This is what makes a
	/// record physically available outside the player's home town. Gated on regional proof
	/// (directive §5.1): without it, this is a real pitch that can be turned down cold, or land on
	/// worse terms -- not an automatic yes.
	/// </summary>
	public bool PlaceLine(string regionId, out string message) {
		if (!Require(DistributionHours, out message)) return false;

		bool proven = false, anyHouse = false;
		// Promo mechanic directive §6.1: a live trade pick nudges an unproven house past "I don't hear it."
		IndependentDistributor house = CompetitorManager.Instance?.PlacePlayerLine(Label, regionId, TradeHouseAcceptBonus(), out proven, out anyHouse);
		string regionName = ChartManager.Instance?.GetRegionById(regionId)?.regionName ?? regionId;
		if (!anyHouse) { message = $"No house in {regionName} has room for another line right now."; return false; }

		Spend(DistributionHours); // the trip itself costs the hours once there's someone there to pitch

		if (house == null) {
			message = $"No deal in {regionName} this trip -- \"I don't hear it yet.\" Come back once you've broken out there.";
			Note($"{regionName}: the warehouse passed -- no regional proof to show them.");
			Changed?.Invoke();
			return true;
		}

		Note($"{house.distributorName} {(proven ? "comes courting" : "takes a flyer on")} you in {regionName} " +
			$"({house.paymentTermWeeks}-week terms, {house.reportingHonesty:P0} reporting).");
		message = proven
			? $"{house.distributorName} is carrying you in {regionName} -- they'd already heard about you."
			: $"{house.distributorName} takes the line in {regionName}, cold -- back of the pile until you prove it there.";
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
		CheckWeeklyInboundCalls();
		CheckWeeklyRunner();
		ExpireStaleAppointments();
		ResolveTradeSubmissions();
		Changed?.Invoke();
	}

	/// <summary>A night away from your own bed is a motel bill.</summary>
	private void ChargeHotelIfAway() {
		if (AtHome) return;
		Label.cashReserves -= HotelNightly;
		Label.monthlyExpenses += HotelNightly;
		Note($"Motel in {CurrentCity?.name ?? "town"} -- ${HotelNightly:N0}.");
	}

	/// <summary>A stop's real local pull, not just the record's national number. rec.awareness is driven
	/// by radioHeat -- a national "should be getting airplay" proxy off quality/label push/momentum/chart
	/// position, with no read of what a specific station is actually spinning -- so on its own it cannot
	/// tell a record that's genuinely on WJLB in Detroit from a private pressing nobody's played anywhere.
	/// The real signal already exists in RecordRuntimeData.regionalData[regionId].awareness, which the
	/// reporter panel's per-station playlists (Rolodex rapport/advocacy) and the tail both feed -- read it
	/// here at the same 40/60 national/regional blend the AI-shared demand pass already trusts (see
	/// ChartSimulator's effectiveAwareness), rather than inventing a separate buzz meter (directive §4.1).
	/// On top of that, add a direct, undiluted read of the reporter panel's actual airplay for this region
	/// (ChartManager.ReporterAirplay). regionalData.awareness only ever hears a cultivated spin at ~13%
	/// (the AI-shared REPORTER_PANEL_WEIGHT dilutes it); reading the panel here lets the player's own trunk
	/// pull respond to real, cultivated station placement at full strength -- a pure read that leaves every
	/// AI-economy number (radioHeat/radioPlay/tail/REPORTER_PANEL_WEIGHT) untouched. Bounded lift, never a
	/// penalty, so an organically-broken record with a cooling airplay curve is never dragged down.
	/// Falls back to the national number alone if this record has no regional row for the stop's region.</summary>
	private float RegionalBuzz(RecordRuntimeData rec, string cityId) {
		float national = Mathf.Clamp(rec.awareness, 0f, 1f);
		string regionId = DistanceModel.GetCityById(cityId)?.parentRegionId;
		if (string.IsNullOrEmpty(regionId) || rec.regionalData == null
			|| !rec.regionalData.TryGetValue(regionId, out RegionalRecordData rd)) return national;
		float reporterLift = ChartManager.Instance?.ReporterAirplay(rec.baseRecord.recordId, regionId) * TrunkReporterAirplayLift ?? 0f;
		float regional = Mathf.Min(1f, rd.awareness + reporterLift);
		return Mathf.Clamp(national * 0.4f + regional * 0.6f, 0f, 1f);
	}

	/// <summary>
	/// A day's trunk sell-through. Every town you've stocked sells a slice of what its shops are holding,
	/// smaller the longer it's been since you restocked -- so a market runs down and needs another run. The
	/// town you're standing in pays cash on the spot; a town you've left holds your cut for you to collect on
	/// your next visit (a thin daily wire aside). The vinyl was paid for at the plant, so there's no skim.
	/// </summary>
	private void ProcessTrunkDay() {
		foreach (PlayerStop stop in EnsureStops().Values.ToList()) {
			if (stop.OnHand.Count == 0) continue;
			bool present = stop.CityId == CurrentCityId; // standing in the town -> COD stock pays cash; away -> it's owed
			foreach (var (recordId, lot) in stop.OnHand.Select(kv => (kv.Key, kv.Value)).ToList()) {
				if (lot.Remaining <= 0) { lot.DaysSinceRestock++; continue; }
				RecordRuntimeData rec = ReleasedRecords.FirstOrDefault(r => r.baseRecord.recordId == recordId);
				// A lot whose record can't be resolved still AGES. It used to `continue` before the
				// increment, which froze the lot forever: no sales and no decay clock, so stock stranded
				// at a stop could never sell through and never be written off either. Player records are
				// no longer culled, so this should now only be reachable for a lot whose record a
				// pre-fix save lost outright and the load-time repair could not rebuild.
				if (rec == null) { lot.DaysSinceRestock++; continue; }
				// A day's move is a slice of the fresh lot, decaying since the last restock, scaled by how much
				// the record actually pulls -- its hook, its sound, and how many have heard of it (an unknown act
				// with a $10 campaign has near-zero awareness and trickles out), then rolled with day-to-day,
				// stop-to-stop luck so two accounts never sell in lockstep.
				float buzz = RegionalBuzz(rec, stop.CityId);
				// Promo mechanic directive §9: a live window/counter card is a bounded lift on this same
				// appeal term -- never a source of units on its own, just a reason this stop in particular
				// outsells an unserviced one this week.
				float windowCard = lot.WindowCardExpiresWeek > 0
					&& (ChartManager.Instance?.GetCurrentChartWeek() ?? 0) <= lot.WindowCardExpiresWeek
					? WindowCardSellThroughBoost : 0f;
				float appeal = Mathf.Clamp(rec.baseRecord.hookStrength * 0.45f + rec.baseRecord.productionQuality * 0.25f + buzz * 0.30f + windowCard, 0.04f, 1f);
				float decay = Mathf.Pow(TrunkDecayPerDay, lot.DaysSinceRestock);
				float luck = (float)GD.RandRange(0.55, 1.45);
				int units = Mathf.Min(lot.Remaining, Mathf.RoundToInt(lot.Placed * TrunkDailyBaseFraction * appeal * decay * luck));
				lot.DaysSinceRestock++;
				if (units <= 0) continue;
				lot.Remaining -= units;
				// Runner-sourced stock is HIS collection, not the player standing in town -- cash timing
				// turns on terms alone (he's out there collecting regardless of where the player is), and
				// BookRunnerSale takes his commission off the top before the label ever sees it (directive §7).
				if (lot.RunnerSourced) BookRunnerSale(rec, units, stop, !lot.ConsignmentTerms);
				// Consignment terms never pay cash-in-hand, even standing in the stop's own town -- COD terms do.
				else BookTrunkSale(rec, units, stop, present && !lot.ConsignmentTerms);
			}
		}
		WireOwedTrickle();
	}

	/// <summary>
	/// Books a day's trunk sell-through at one stop. The records leave the shelves and count toward the
	/// chart (accumulated into the weekly total) whichever stop they sold at -- department-store and
	/// record-shop sales are chart sales. The MONEY, though, only reaches the bank when the terms and your
	/// whereabouts say so: cashNow is COD stock in the town you're standing in; otherwise the stop holds
	/// your cut until you drive back (with a thin daily wire in the meantime). The artist's royalty is
	/// credited on the sale either way.
	/// </summary>
	private void BookTrunkSale(RecordRuntimeData rec, int units, PlayerStop stop, bool cashNow) =>
		BookSale(rec, units, stop, cashNow, labelShare: 1f);

	/// <summary>Same sale, minus the runner's cut (directive §7: "8-15% of his collections ... paid when
	/// the shop pays") -- his commission comes off the net the instant it lands, whichever channel it
	/// lands through (cash-now or the stop's OpenBalance), so it's never a separate payroll step.</summary>
	private void BookRunnerSale(RecordRuntimeData rec, int units, PlayerStop stop, bool cashNow) =>
		BookSale(rec, units, stop, cashNow, labelShare: 1f - RunnerCommissionRate);

	private void BookSale(RecordRuntimeData rec, int units, PlayerStop stop, bool cashNow, float labelShare) {
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
		// The act's royalty is real either way; the RUNNER's cut (if any) comes out of what's left, before
		// the label ever sees it -- labelShare is 1f for a straight trunk sale, 1-commission for a runner one.
		float fullNet = gross - royalty;
		float net = fullNet * labelShare;
		float commission = fullNet - net;
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
		weeklyRunnerCommission += commission;
		if (cashNow) {
			Label.cashReserves += net;
			Label.monthlyRevenue += net;
		} else {
			stop.OpenBalance += net;
			weeklyTrunkHeld += net;   // out on consignment this week; not yet at the bank
		}
		// (The royalty and recoupment were already booked above, against `accrued`. A second credit here
		// double-paid the act on every trunk sale and recouped the advance twice -- removed.)
	}

	/// <summary>A thin daily wire from every stop holding money for you (bar the ones in the town you're
	/// standing in). Keeps a market you never return to from stranding your cut entirely.</summary>
	private void WireOwedTrickle() {
		foreach (PlayerStop stop in EnsureStops().Values) {
			if (stop.CityId == CurrentCityId) continue;
			if (stop.OpenBalance <= 0f) continue;
			float wire = Mathf.Min(stop.OpenBalance, Mathf.Max(1f, stop.OpenBalance * TrunkWireFractionPerDay));
			stop.OpenBalance -= wire;
			Label.cashReserves += wire;
			Label.monthlyRevenue += wire;
			if (stop.OpenBalance <= 0.5f) stop.OpenBalance = 0f;
		}
	}

	/// <summary>Pockets whatever a town's stops have been holding for you. Called when you show up --
	/// driving in, or working an account there. Nothing to do for a town that owes you nothing.</summary>
	private void CollectAtCity(string cityId) {
		if (string.IsNullOrEmpty(cityId)) return;
		float total = 0f;
		foreach (PlayerStop stop in EnsureStops().Values.Where(s => s.CityId == cityId)) {
			if (stop.OpenBalance <= 0f) continue;
			total += stop.OpenBalance;
			stop.OpenBalance = 0f;
		}
		if (total <= 0f) return;
		Label.cashReserves += total;
		Label.monthlyRevenue += total;
		Note($"Collected ${total:N0} the shops in {CityName(cityId)} were holding.");
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

	/// <summary>What each named stop is currently holding for you, for the DISTRIBUTION readout.</summary>
	public IEnumerable<(string CityName, string StopName, float Amount)> OpenBalancesByStop() {
		foreach (PlayerStop stop in EnsureStops().Values)
			if (stop.OpenBalance > 0.5f) yield return (CityName(stop.CityId), stop.DisplayName, stop.OpenBalance);
	}

	public const float SinglePrice = 0.89f; // historical 45 retail
	// Directive §3.3: "the act takes 50-100 at a discount, cash now -- a one-stop with legs." A
	// wholesale-grade cut of the $0.89 trunk/retail price -- well above the ~$0.37-0.40/disc pressing
	// cost, so the label still profits, but a real discount against paying full retail.
	public const float ArtistBuyInPrice = 0.50f;
	public const int ArtistBuyInMin = 50;
	public const int ArtistBuyInMax = 100;

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
		// The plant collects on a due credit run before the settlement snapshot -- certain, not a dice
		// roll, so it shows in this week's Cash the same as any other bill (directive §11).
		SettlePlantCreditIfDue();
		// The wholesale channel (what cleared through stores this week), then the trunk folded on top so the
		// write-up covers the whole week's business, not only wholesale. Trunk carries no manufacturing (pressed
		// and paid up front) and no distributor skim; its full net counts as earned, and the consignment slice
		// (weeklyTrunkHeld) is earned-but-not-yet-banked, so it sits alongside wholesale credit deferral.
		long wholesaleUnits = ReleasedRecords.Sum(record => (long)record.regionalData.Values.Sum(data => Mathf.Max(0, data.unitsSoldThisWeek)));
		// The runner's cut is already OUT of every dollar credited to cash or held at a stop (BookSale) --
		// subtract it here too, or Earned/Banked would overstate what the label actually kept this week.
		float trunkNet = weeklyTrunkGross - weeklyTrunkRoyalty - weeklyRunnerCommission;
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
			RunnerCommission = weeklyRunnerCommission,
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
		weeklyRunnerCommission = 0f;
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

	/// <summary>Directive §8's "factor the paper": a house's better reliability (it actually pays what it
	/// owes) is worth more to whoever's buying the risk off you, so the discount reads off that same
	/// field rather than a flat number -- 70-85¢ on the dollar, same range the directive names.</summary>
	private static float FactorRate(IndependentDistributor house) =>
		Mathf.Clamp(Mathf.Lerp(0.70f, 0.85f, house?.reliability ?? 0.5f), 0.70f, 0.85f);

	/// <summary>The rate a given row of <see cref="OutstandingInvoices"/> would factor at, for the UI to
	/// show before the player commits.</summary>
	public float FactorRatePreview(int outstandingIndex) {
		var ordered = OrderedReceivables();
		if (outstandingIndex < 0 || outstandingIndex >= ordered.Count) return 0f;
		return FactorRate(HouseFor(ordered[outstandingIndex].Receivable.DistributorId));
	}

	private List<(WholesaleReceivable Receivable, int Index)> OrderedReceivables() =>
		(Label?.wholesaleReceivables ?? new List<WholesaleReceivable>())
			.Select((r, i) => (Receivable: r, Index: i)).OrderBy(t => t.Receivable.DueWeek).ToList();

	private IndependentDistributor HouseFor(string distributorId) =>
		(CompetitorManager.Instance?.GetIndependentDistributors() ?? Array.Empty<IndependentDistributor>())
			.FirstOrDefault(h => h.distributorId == distributorId);

	/// <summary>
	/// Directive §8: "sell the receivable at 70-85¢ now" -- one of the four buttons the first-hit squeeze
	/// needs (starve the hit and die famous need no new mechanic; master-lease/P&amp;D is §9). Indexes into
	/// the same order <see cref="OutstandingInvoices"/> shows, so the UI can factor the row it's looking
	/// at. The discount books as a real write-off, same ledger a house's own short-pay uses -- the
	/// certainty and the immediacy are what you're buying.
	/// </summary>
	public bool FactorReceivable(int outstandingIndex, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		var ordered = OrderedReceivables();
		if (outstandingIndex < 0 || outstandingIndex >= ordered.Count) { message = "No such invoice to factor."; return false; }
		(WholesaleReceivable receivable, int originalIndex) = ordered[outstandingIndex];

		IndependentDistributor house = HouseFor(receivable.DistributorId);
		float rate = FactorRate(house);
		float cashNow = receivable.Amount * rate;
		string houseName = house?.distributorName ?? "the house";

		Label.wholesaleReceivables.RemoveAt(originalIndex);
		Label.outstandingWholesaleReceivables = Mathf.Max(0f, Label.outstandingWholesaleReceivables - receivable.Amount);
		Label.cashReserves += cashNow;
		Label.monthlyRevenue += cashNow;
		Label.lifetimeWholesaleWriteOffs += receivable.Amount - cashNow;

		Note($"Factored ${receivable.Amount:N0} owed by {houseName} for ${cashNow:N0} cash now ({rate:P0} on the dollar) -- someone else collects it, not you.");
		message = $"Sold that invoice for ${cashNow:N0} now, at {rate:P0} on the dollar.";
		Changed?.Invoke();
		return true;
	}

	// ========================================================================
	// LATE EXITS -- directive §9. Two front doors, both reusing machinery that already exists rather
	// than inventing a parallel deal brain: master lease/sale is a one-off transaction on a single
	// proven title (§9's "honorable success for an $800 company"); the P&D pitch below reuses the AI's
	// own DistributionDeal type and CompetitorManager plumbing (GenerateDealTerms, SelectDistributor,
	// the regional-evidence bar), with CompetitorManager's automatic courting/poaching loops now gated
	// off the player's label entirely (see the isPlayerOwned guards there) so nothing gets signed
	// without the player choosing to. "Own distribution arm" is out of scope for this pass -- it has no
	// existing AI-side type to build on and is a separate, later piece of work.
	// ========================================================================

	// Master lease/sale: which titles are out, and for how long. Sold is forever; leased comes back at
	// the recorded chart week. A record can only be in one of these at a time (MasterDealEligible
	// already refuses a title that's out).
	private readonly HashSet<string> soldMasterRecordIds = new(StringComparer.Ordinal);
	private readonly Dictionary<string, int> leasedMasterExpiryWeek = new(StringComparer.Ordinal);
	// "Once a one-stop knows the title" (§9) -- SellCartonToOneStop doesn't otherwise leave a mark on
	// the specific record sold (only on office PressStock and the stop's trust), so this is the one new
	// bit of bookkeeping the gate needs. Sticky once true -- a one-stop that has moved a title once
	// doesn't un-know it.
	private readonly HashSet<string> oneStopKnownRecordIds = new(StringComparer.Ordinal);

	public const int MasterDealHours = SignHours; // a real sit-down, same weight as any other contract talk
	private const float MasterSaleValueMultiple = 3.0f;   // permanent -- the bigger number buys the whole future
	private const float MasterLeaseValueMultiple = 1.25f; // temporary -- a slice, not the whole future
	public const int MasterLeaseTermWeeks = 26;
	// An $800 company's real floor -- directive §9 frames this as "an honorable success," never a token
	// payout once a title has actually earned the gate below.
	private const float MasterDealValueFloor = 150f;

	/// <summary>True while a title is out on lease or sold outright -- the player's own sale verbs
	/// (PitchAtStop/ConsignAtStop/ServiceStop via ValidateStopAction, SellCartonToOneStop, ArtistBuyIn,
	/// OrderPressing) all refuse it. A lease expires on its own; a sale never does.</summary>
	public bool IsMasterOut(string recordId) {
		if (string.IsNullOrEmpty(recordId)) return false;
		if (soldMasterRecordIds.Contains(recordId)) return true;
		return leasedMasterExpiryWeek.TryGetValue(recordId, out int expiry)
			&& (ChartManager.Instance?.GetCurrentChartWeek() ?? 0) < expiry;
	}

	/// <summary>
	/// Directive §9: "on the table once a station and a one-stop both know the title." Station
	/// knowledge reuses <see cref="StrangerRadioThreshold"/> -- the same regional radioPlay bar that
	/// already makes a stranger's call plausible (§4), so this is not a second buzz meter. One-stop
	/// knowledge is <see cref="oneStopKnownRecordIds"/>, set the moment a carton of the title actually
	/// moves through a metro counter (§6). Below both, there is nobody shopping for this title yet.
	/// </summary>
	public bool MasterDealEligible(string recordId) {
		if (string.IsNullOrEmpty(recordId) || IsMasterOut(recordId)) return false;
		if (!IsOneStopKnown(recordId)) return false;
		RecordRuntimeData rec = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId);
		return rec != null && rec.regionalData.Values.Any(rd => rd != null && rd.radioPlay >= StrangerRadioThreshold);
	}

	/// <summary>Whether this title has ever moved through a metro one-stop counter -- half of the §9
	/// master-deal gate, and worth showing on its own in the UI ("a one-stop already knows this one").</summary>
	public bool IsOneStopKnown(string recordId) => !string.IsNullOrEmpty(recordId) && oneStopKnownRecordIds.Contains(recordId);

	// The record's own lifetimeLabelNet is the one number on the books that already prices "how much
	// this title has proven it can pull" -- a buyer pays a multiple of that for the right to keep
	// collecting elsewhere, floored so a genuine hit is never sold for a token sum.
	private float MasterDealValue(string recordId, float multiple) {
		RecordRuntimeData rec = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId);
		return Mathf.Max(MasterDealValueFloor, (rec?.lifetimeLabelNet ?? 0f) * multiple);
	}

	public float MasterSaleValue(string recordId) => MasterDealValue(recordId, MasterSaleValueMultiple);
	public float MasterLeaseValue(string recordId) => MasterDealValue(recordId, MasterLeaseValueMultiple);

	/// <summary>Sell a title's master outright -- cash now, forever, no more collecting on it through any
	/// of the player's own channels. The bigger of the two payouts, because it's the whole future.</summary>
	public bool SellMaster(string recordId, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (!MasterDealEligible(recordId)) { message = "Nobody's shopping for that one yet -- it needs real airplay and a one-stop that already knows it."; return false; }
		if (!Require(MasterDealHours, out message)) return false;
		Spend(MasterDealHours);

		float cash = MasterSaleValue(recordId);
		soldMasterRecordIds.Add(recordId);
		Label.cashReserves += cash;
		Label.monthlyRevenue += cash;

		string title = TitleForRecord(recordId);
		Note($"Sold the master to \"{title}\" outright for ${cash:N0} -- it's someone else's record to work now.");
		message = $"Sold \"{title}\" for ${cash:N0} cash, for good.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Lease a title out for <see cref="MasterLeaseTermWeeks"/> -- smaller cash now, the title
	/// comes back to the player's own channels once the term runs out.</summary>
	public bool LeaseMaster(string recordId, out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (!MasterDealEligible(recordId)) { message = "Nobody's shopping for that one yet -- it needs real airplay and a one-stop that already knows it."; return false; }
		if (!Require(MasterDealHours, out message)) return false;
		Spend(MasterDealHours);

		float cash = MasterLeaseValue(recordId);
		leasedMasterExpiryWeek[recordId] = (ChartManager.Instance?.GetCurrentChartWeek() ?? 0) + MasterLeaseTermWeeks;
		Label.cashReserves += cash;
		Label.monthlyRevenue += cash;

		string title = TitleForRecord(recordId);
		Note($"Leased out \"{title}\" for ${cash:N0} -- {MasterLeaseTermWeeks} weeks before it's yours to work again.");
		message = $"Leased \"{title}\" for ${cash:N0} cash, {MasterLeaseTermWeeks} weeks.";
		Changed?.Invoke();
		return true;
	}

	// P&D distribution deal: a pitch generates a real offer that sits on the desk until the player
	// decides. Only one live at a time -- walk away or sign before pursuing another.
	private DistributionDeal pendingDistributionOffer;
	public DistributionDeal PendingDistributionOffer => pendingDistributionOffer;
	public string PendingDistributionOfferDistributorName =>
		pendingDistributionOffer == null ? null : CompetitorManager.Instance?.GetLabel(pendingDistributionOffer.distributorId)?.labelName;

	/// <summary>
	/// Directive §9: the player's front door into a P&amp;D distribution deal -- CompetitorManager does
	/// all the real work (the same evidence bar, distributor pool, and term generator the AI's own
	/// courting loop used before it was gated off the player). A real pitch costs the sit-down whether
	/// or not anyone bites; failing the pre-checks (already under contract, outgrown the tier, no proof
	/// yet) costs nothing, since those are things the player can already tell from their own desk.
	/// </summary>
	public bool PursueDistributionDeal(out string message) {
		message = null;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (pendingDistributionOffer != null) { message = "You've already got an offer on the table -- decide on that one first."; return false; }
		if (!Require(SignHours, out message)) return false;

		bool consulted = false;
		DistributionDeal offer = CompetitorManager.Instance != null
			? CompetitorManager.Instance.PursueDistributionDeal(Label, out consulted, out message)
			: null;
		if (!consulted) { message ??= "No distribution office to pitch."; return false; } // pre-checks failed -- no hours spent

		Spend(SignHours);
		if (offer == null) {
			Note($"Made the rounds for a distribution deal -- {message}");
			Changed?.Invoke();
			return true;
		}

		pendingDistributionOffer = offer;
		string distributorName = CompetitorManager.Instance?.GetLabel(offer.distributorId)?.labelName ?? "A distributor";
		Note($"{distributorName} made an offer -- {message}");
		Changed?.Invoke();
		return true;
	}

	/// <summary>Signs the offer sitting on the desk. Advance, deal event, and everything downstream
	/// (distribution skim, term resolution/renewal/absorption) run through the exact same
	/// CompetitorManager machinery an AI client's deal does from here on -- no separate settlement path.</summary>
	public bool AcceptDistributionOffer(out string message) {
		message = null;
		if (pendingDistributionOffer == null) { message = "No offer on the table."; return false; }
		DistributionDeal offer = pendingDistributionOffer;
		CompetitorManager.Instance?.SignDistributionDeal(Label, offer);
		string distributorName = CompetitorManager.Instance?.GetLabel(offer.distributorId)?.labelName ?? "the distributor";
		pendingDistributionOffer = null;
		Note($"Signed with {distributorName} -- {offer.termWeeks}-week P&D deal.");
		message = $"Signed with {distributorName}.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Walks away from the offer on the desk. Costs nothing beyond the sit-down already spent
	/// pursuing it -- staying independent is always on the table.</summary>
	public bool DeclineDistributionOffer(out string message) {
		message = null;
		if (pendingDistributionOffer == null) { message = "No offer to walk away from."; return false; }
		Note("Passed on the distribution deal -- staying independent for now.");
		pendingDistributionOffer = null;
		message = "Passed.";
		Changed?.Invoke();
		return true;
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
				RecordId = kv.Key, Remaining = kv.Value.Remaining, PromoRemaining = kv.Value.PromoRemaining,
				TotalPressed = kv.Value.TotalPressed, TotalSpent = kv.Value.TotalSpent
			}).ToList(),
			PressOrders = pressOrders.Select(o => new PressOrderSaveData {
				RecordId = o.RecordId, Quantity = o.Quantity, PromoQuantity = o.PromoQuantity, Cost = o.Cost,
				OrderedYear = o.Ordered.year, OrderedMonth = o.Ordered.month, OrderedDay = o.Ordered.day,
				ArrivesYear = o.Arrives.year, ArrivesMonth = o.Arrives.month, ArrivesDay = o.Arrives.day
			}).ToList(),
			// Promo mechanic directive §3.2: who's been sent a copy of what. Player-only; no AI path
			// reads or writes this.
			Servicing = servicing.Select(RecordServicingSaveData.From).ToList(),
			// Promo mechanic directive §6.1: who's been sent to the trade review desk and what came back.
			TradeSubmissions = tradeSubmissions.Select(TradeSubmissionSaveData.From).ToList(),
			// Promo mechanic directive §6.2: live paid trade ads.
			TradeAds = tradeAds.Select(TradeAdSaveData.From).ToList(),
			// Promo mechanic directive §7.1: which reporting dealers the player has worked out report.
			KnownReportingStopIds = knownReportingStopIds.ToList(),
			// Stop identity (name/city/kind) regenerates deterministically from the world seed every
			// session (EnsureStops) -- only the mutable state each account has earned gets saved, and
			// only for stops that actually have any (untouched stops need no row at all).
			StopState = EnsureStops().Values
				.Where(stop => stop.Relationship > 0f || stop.OpenBalance > 0f || stop.LastVisitWeek > 0
					|| stop.OnHand.Count > 0 || stop.PassedRecordIds.Count > 0 || stop.OneStopUnlocked || stop.HypeBurned)
				.Select(stop => new PlayerStopSaveData {
					StopId = stop.StopId, Relationship = stop.Relationship, LastVisitWeek = stop.LastVisitWeek,
					OpenBalance = stop.OpenBalance, MissedCallStreak = stop.MissedCallStreak,
					LastApproachYear = stop.LastApproachDate.year, LastApproachMonth = stop.LastApproachDate.month,
					LastApproachDay = stop.LastApproachDate.day,
					PassedRecordIds = stop.PassedRecordIds.ToList(),
					OneStopUnlocked = stop.OneStopUnlocked, OneStopTrusted = stop.OneStopTrusted,
					HypeBurned = stop.HypeBurned,
					OnHand = stop.OnHand.Select(kv => new ConsignmentLotSaveData {
						RecordId = kv.Key, Remaining = kv.Value.Remaining,
						Placed = kv.Value.Placed, DaysSinceRestock = kv.Value.DaysSinceRestock,
						ConsignmentTerms = kv.Value.ConsignmentTerms, RunnerSourced = kv.Value.RunnerSourced,
						WindowCardExpiresWeek = kv.Value.WindowCardExpiresWeek,
					}).ToList()
				}).ToList(),
			// Open "they called me" demand (directive §4), plus the week cursor that keeps a reload from
			// re-rolling the same week's batch (CheckWeeklyInboundCalls).
			InboundCalls = inboundCalls.Select(call => new InboundCallSaveData {
				StopId = call.StopId, RecordId = call.RecordId, Week = call.Week,
				RequestedQty = call.RequestedQty, Reason = (int)call.Reason,
				ExpiresWeek = call.ExpiresWeek, ConsignmentTerms = call.ConsignmentTerms
			}).ToList(),
			LastCallGenWeek = lastCallGenWeek,
			WeeklyTrunkUnits = new Dictionary<string, int>(weeklyTrunkUnits),
			WeeklyTrunkUnitsSold = weeklyTrunkUnitsSold,
			WeeklyTrunkGross = weeklyTrunkGross,
			WeeklyTrunkRoyalty = weeklyTrunkRoyalty,
			WeeklyTrunkHeld = weeklyTrunkHeld,
			WorkedCities = workedCities.ToList(),
			CurrentCityId = currentCityId,
			Counter = counter,
			MonthsInTheRed = monthsInTheRed,

			// People (directive §7): the runner's own state, plus the unlock ledger so a reload can't
			// re-earn (or lose) an unlock already granted.
			RunnerUnlocked = runnerUnlocked,
			ServiceReorderCountByCity = new Dictionary<string, int>(serviceReorderCountByCity),
			LastRunnerTickWeek = lastRunnerTickWeek,
			WeeklyRunnerCommission = weeklyRunnerCommission,
			Runner = runner == null ? null : new PlayerRunnerSaveData {
				RouteStopIds = runner.RouteStopIds.ToList(),
				CartonRecordId = runner.CartonRecordId,
				CartonRemaining = runner.CartonRemaining,
				Familiarity = new Dictionary<string, float>(runner.Familiarity)
			},

			// Plant credit (directive §11): null when nothing is owed.
			PlantCredit = plantCredit == null ? null : new PlantCreditSaveData {
				RecordId = plantCredit.RecordId, Amount = plantCredit.Amount, DueWeek = plantCredit.DueWeek
			},

			// Late exits (directive §9): master lease/sale, and whatever P&D offer is sitting unsigned
			// on the desk. The signed deal itself travels on Label.ActiveDeal, captured above.
			SoldMasterRecordIds = soldMasterRecordIds.ToList(),
			LeasedMasterExpiryWeek = new Dictionary<string, int>(leasedMasterExpiryWeek),
			OneStopKnownRecordIds = oneStopKnownRecordIds.ToList(),
			PendingDistributionOffer = DistributionDealSaveData.From(pendingDistributionOffer),

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
				Remaining = saved.Remaining, PromoRemaining = saved.PromoRemaining,
				TotalPressed = saved.TotalPressed, TotalSpent = saved.TotalSpent
			};
		pressOrders.Clear();
		foreach (PressOrderSaveData saved in data.PressOrders ?? new List<PressOrderSaveData>())
			pressOrders.Add(new PressOrder {
				RecordId = saved.RecordId, Quantity = saved.Quantity, PromoQuantity = saved.PromoQuantity, Cost = saved.Cost,
				Ordered = new GameDate(saved.OrderedYear, saved.OrderedMonth, saved.OrderedDay),
				Arrives = new GameDate(saved.ArrivesYear, saved.ArrivesMonth, saved.ArrivesDay)
			});
		servicing.Clear();
		servicing.AddRange((data.Servicing ?? new List<RecordServicingSaveData>()).Select(s => s.ToServicing()));
		tradeSubmissions.Clear();
		tradeSubmissions.AddRange((data.TradeSubmissions ?? new List<TradeSubmissionSaveData>()).Select(s => s.ToSubmission()));
		tradeAds.Clear();
		tradeAds.AddRange((data.TradeAds ?? new List<TradeAdSaveData>()).Select(a => a.ToAd()));
		// Stop identity regenerates fresh (deterministic on the world seed); only overlay the mutable
		// state a save carries. `stops = null` forces EnsureStops to rebuild rather than reuse whatever
		// roster (if any) belonged to a previously-loaded label in this same process.
		stops = null;
		Dictionary<string, PlayerStop> liveStops = EnsureStops();
		bool hasStopState = data.StopState != null && data.StopState.Count > 0;
		if (hasStopState) {
			foreach (PlayerStopSaveData saved in data.StopState) {
				// A stop the current roster no longer generates (a changed seed) has nowhere to land --
				// drop it rather than resurrect a ghost account with no city, no name, nothing.
				if (!liveStops.TryGetValue(saved.StopId, out PlayerStop stop)) continue;
				stop.Relationship = saved.Relationship;
				stop.LastVisitWeek = saved.LastVisitWeek;
				stop.OpenBalance = saved.OpenBalance;
				stop.MissedCallStreak = saved.MissedCallStreak;
				stop.LastApproachDate = saved.LastApproachYear > 0
					? new GameDate(saved.LastApproachYear, saved.LastApproachMonth, saved.LastApproachDay)
					: default;
				stop.PassedRecordIds.Clear();
				foreach (string recordId in saved.PassedRecordIds ?? new List<string>()) stop.PassedRecordIds.Add(recordId);
				stop.OneStopUnlocked = saved.OneStopUnlocked;
				stop.OneStopTrusted = saved.OneStopTrusted;
				stop.HypeBurned = saved.HypeBurned;
				stop.OnHand.Clear();
				foreach (ConsignmentLotSaveData lot in saved.OnHand ?? new List<ConsignmentLotSaveData>())
					stop.OnHand[lot.RecordId] = new ConsignmentLot {
						Remaining = lot.Remaining, Placed = lot.Placed,
						DaysSinceRestock = lot.DaysSinceRestock, ConsignmentTerms = lot.ConsignmentTerms,
						RunnerSourced = lot.RunnerSourced, WindowCardExpiresWeek = lot.WindowCardExpiresWeek,
					};
			}
		} else if (data.Consignment != null && data.Consignment.Count > 0) {
			// A pre-stop-layer save: fold each city's old whole-city ConsignmentLot into that city's
			// first generated Shop stop (deterministic, so this is reproducible) rather than dropping
			// the stock on the floor -- same spirit as the dead-stock-cull repair below.
			var owedByCity = data.ConsignmentOwed ?? new Dictionary<string, float>();
			foreach (var cityGroup in data.Consignment.GroupBy(c => c.CityId, StringComparer.Ordinal)) {
				PlayerStop landing = liveStops.Values.FirstOrDefault(s => s.CityId == cityGroup.Key && s.Kind == StopKind.Shop)
					?? liveStops.Values.FirstOrDefault(s => s.CityId == cityGroup.Key);
				if (landing == null) continue; // the current roster generates nothing for this city
				foreach (ConsignmentLotSaveData lot in cityGroup)
					landing.OnHand[lot.RecordId] = new ConsignmentLot {
						Remaining = lot.Remaining, Placed = lot.Placed, DaysSinceRestock = lot.DaysSinceRestock
					};
				if (owedByCity.TryGetValue(cityGroup.Key, out float owed) && owed > 0f) landing.OpenBalance += owed;
				landing.Relationship = Mathf.Max(landing.Relationship, 0.3f); // it already had stock out -- not a cold call
			}
		}
		// Promo mechanic directive §7.1: which reporting dealers the player has worked out report. A save
		// written before this existed carries no list, so back-fill it from visit history rather than
		// making a veteran player re-discover accounts he's been servicing for a year -- a stop he has
		// stood in has, by definition, already had the shop conversation TouchStop grants this on.
		knownReportingStopIds.Clear();
		if (data.KnownReportingStopIds != null && data.KnownReportingStopIds.Count > 0) {
			foreach (string stopId in data.KnownReportingStopIds)
				if (liveStops.ContainsKey(stopId)) knownReportingStopIds.Add(stopId);
		} else {
			foreach (PlayerStop stop in liveStops.Values)
				if (stop.LastVisitWeek > 0) LearnWhoReports(stop);
		}

		inboundCalls.Clear();
		foreach (InboundCallSaveData saved in data.InboundCalls ?? new List<InboundCallSaveData>()) {
			if (!liveStops.ContainsKey(saved.StopId)) continue; // same ghost-account guard as StopState above
			inboundCalls.Add(new InboundCall {
				StopId = saved.StopId, RecordId = saved.RecordId, Week = saved.Week,
				RequestedQty = saved.RequestedQty, Reason = (InboundCallReason)saved.Reason,
				ExpiresWeek = saved.ExpiresWeek, ConsignmentTerms = saved.ConsignmentTerms
			});
		}
		lastCallGenWeek = data.LastCallGenWeek;
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

		// People (directive §7).
		runnerUnlocked = data.RunnerUnlocked;
		lastRunnerTickWeek = data.LastRunnerTickWeek;
		weeklyRunnerCommission = data.WeeklyRunnerCommission;
		serviceReorderCountByCity.Clear();
		foreach (var kv in data.ServiceReorderCountByCity ?? new Dictionary<string, int>()) serviceReorderCountByCity[kv.Key] = kv.Value;
		if (data.Runner == null) runner = null;
		else {
			runner = new PlayerRunner { CartonRecordId = data.Runner.CartonRecordId, CartonRemaining = data.Runner.CartonRemaining };
			foreach (string stopId in data.Runner.RouteStopIds ?? new List<string>())
				if (liveStops.ContainsKey(stopId)) runner.RouteStopIds.Add(stopId); // same ghost-account guard as StopState above
			foreach (var kv in data.Runner.Familiarity ?? new Dictionary<string, float>())
				if (liveStops.ContainsKey(kv.Key)) runner.Familiarity[kv.Key] = kv.Value;
		}
		plantCredit = data.PlantCredit == null ? null : new PlantCredit {
			RecordId = data.PlantCredit.RecordId, Amount = data.PlantCredit.Amount, DueWeek = data.PlantCredit.DueWeek
		};
		counter = Mathf.Max(data.Counter, songs.Count);

		// Late exits (directive §9).
		soldMasterRecordIds.Clear();
		foreach (string recordId in data.SoldMasterRecordIds ?? new List<string>()) soldMasterRecordIds.Add(recordId);
		leasedMasterExpiryWeek.Clear();
		foreach (var kv in data.LeasedMasterExpiryWeek ?? new Dictionary<string, int>()) leasedMasterExpiryWeek[kv.Key] = kv.Value;
		oneStopKnownRecordIds.Clear();
		foreach (string recordId in data.OneStopKnownRecordIds ?? new List<string>()) oneStopKnownRecordIds.Add(recordId);
		pendingDistributionOffer = data.PendingDistributionOffer?.ToDeal();

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
		foreach (PlayerStop stop in EnsureStops().Values)
			foreach (string recordId in stop.OnHand.Keys) referenced.Add(recordId);
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

	/// <summary>Copies a record's stops have actually sold -- the only honest lifetime-units figure
	/// left once the runtime record is gone.</summary>
	private int UnitsMovedFromLots(string recordId) {
		int moved = 0;
		foreach (PlayerStop stop in EnsureStops().Values)
			if (stop.OnHand.TryGetValue(recordId, out ConsignmentLot lot))
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
