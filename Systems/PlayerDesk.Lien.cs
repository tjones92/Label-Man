using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// THE LIEN (SimTools/LabelManInnerLifeDirective.md §3, Phase 1).
//
// On New Year's Eve 1959 the Label Man got blackout drunk at a party and signed a lien on everything he owns and
// everything the label will own: a $20,000 face sum, due 1 January 1961. Each December the holder's lawyer sits down
// with him, and the lien rolls to the next 1 January if he meets ONE of three bars: Pay (interest plus a slice of
// principal), Show (evidence the label is worth more than last year), or Concede (give something up). The holder stresses
// one bar; another bar can be pushed back on, and answered with an honest counter (instinct + fact) or a labelled bluff.
// Miss a deadline and the lawyer seizes part of what is owed (cash above zero, then pressed stock) and extends three
// months; a second miss in a row, or nothing left to take, ends the run. 1 January 1970 is the reckoning: the run ends
// either paid clean or taken. The paper forbids paying it off early: a 1960 hit can't end the run (§3.2).
//
// Who holds the paper is one of five authored variants (§3.5): Dee-Dee Sawyer (the hometown's biggest room), Walter
// Kubiak (a jukebox route), Clyde Winters (a song-plugger), Gene Floria (a Major's front) and Jack Germain (a jock fired
// in the payola sweep). Each holder has his own rate (§3.5, run against Phase 0 cash). Kubiak's compounds; Floria stops
// taking money from the December 1966 sitting. The founding "What do you remember?" answers narrow the pool (§3.6), and a
// keyed Philox draw on the world seed picks within it, never the shared stream.
//
// Player-only. Nothing here exists without a player label, so a headless AI run never reaches it. Payments come from
// cash above zero only and never push the books red; the bank's own credit-floor game over is untouched.
// ============================================================================================
public partial class PlayerDesk {
	// Ordinals are saved. They keep the slots of the first draft's holders (Dace, Kubiak, Fennell, Ainsley, Lark).
	public enum LienHolder { Sawyer, Kubiak, Winters, Floria, Germain }
	public enum LienBar { Pay, Show, Concede }
	public enum PartyWhere { SupperClub, DjHouseParty, HotelBallroom, BarBackRoom, Nothing }
	public enum PartyWho { CamelCoat, DiscJockey, RouteMan, MajorMan, Nothing }
	public enum PartyBrag { TheLabel, TheAct, Money, Hits, Nothing }
	public enum PartyEnd { CardTable, Car, PhoneCall, Nothing }
	public enum LienLedgerKind { Signed, InterestAccrued, Paid, Shown, ConcededStock, WrittenOn, Seized, Extended, PaidClean, Taken }

	public const float LienFaceSum = 20000f;
	/// <summary>The fallback rate (older saves carry their own). Each holder sets his own: <see cref="LienHolderProfile.Rate"/>.</summary>
	public const float LienRate = 0.03f;
	public const int LienSittingHours = 2;
	public const int LienWindowDays = 14;
	public const int LienShortExtensionMonths = 3;
	public const int LienFailuresToEnd = 2;
	/// <summary>"Write it onto the paper": the face sum grows by this multiple of what he would have taken.</summary>
	public const float LienWrittenOnMultiple = 1.5f;
	public const float LienBluffStandingBar = 0.5f;
	public const int LienBluffInstinctBar = 4;
	public const int LienFinalYear = 1970;
	private const uint LienPickDecision = 0x4C49454EU; // "LIEN"
	private const string LienVenueFallback = "the best room in town";

	/// <summary>Principal slice asked under Pay, by the year the bar is drawn from (§3.2). Provisional: Phase 0 put the median
	/// starting-Small survivor at $8-14k cash through 1966 and $28-42k by 1969, p25 $2-7k mid-decade.</summary>
	private static readonly Dictionary<int, float> LienSlice = new() {
		[1960] = 0f, [1961] = 250f, [1962] = 500f, [1963] = 750f, [1964] = 1000f,
		[1965] = 1250f, [1966] = 1500f, [1967] = 2000f, [1968] = 2500f,
	};

	/// <summary>Copies the label must have sold since the last sitting to Show (1960 asks only for a record out). Provisional.</summary>
	private static readonly Dictionary<int, int> LienShowUnits = new() {
		[1960] = 0, [1961] = 5000, [1962] = 10000, [1963] = 20000, [1964] = 30000,
		[1965] = 40000, [1966] = 60000, [1967] = 80000, [1968] = 100000,
	};

	// ── the five holders (§3.5) ──────────────────────────────────────────────────────────────────────

	/// <param name="Rate">Simple annual interest on the outstanding face sum (§3.5; provisional, run against Phase 0 cash).</param>
	/// <param name="Compounds">Interest left unpaid when the lien rolls over is written onto the face sum (Kubiak).</param>
	/// <param name="RefusesPayFrom">From this bar year he won't take money at a sitting (Floria, 1966); 0 = never refuses.</param>
	/// <remarks>"{venue}" in any line is the hometown's biggest room (<see cref="LienState.Venue"/>); never at a sentence start.</remarks>
	public sealed record LienHolderProfile(
		LienHolder Id, string Name, string Title, string Lawyer, LienBar Stresses, float Rate, bool Compounds, int RefusesPayFrom,
		string Opening, string WantsLine, string PayAccepted, string ShowAccepted, string ConcedeAccepted,
		string Pushback, ExecutiveVoice HonestVoice, string HonestLine, ExecutiveVoice BluffVoice, string BluffLine,
		string BluffCaught, string Seizure, string FixerRead, string RefusesPayLine = null);

	public static readonly IReadOnlyDictionary<LienHolder, LienHolderProfile> LienHolders = new Dictionary<LienHolder, LienHolderProfile> {
		[LienHolder.Sawyer] = new(LienHolder.Sawyer, "Dee-Dee Sawyer", "who owns {venue}, the most popular room in town", "Mr. Halloran", LienBar.Concede,
			Rate: 0.04f, Compounds: false, RefusesPayFrom: 0,
			Opening: "Halloran takes the good chair without being asked. \"Mr. Sawyer sends his regards. He still talks about the hand he won off you. He talks about it a great deal.\"",
			WantsLine: "\"He'd rather have a favour than your money. It's about Miss Boursaw. It's always about Miss Boursaw.\"",
			PayAccepted: "\"Money's fine,\" Halloran says, in a voice that means it isn't. He stamps the date in.",
			ShowAccepted: "\"He'll be pleased you're doing well. He'll be more pleased when Miss Boursaw is.\"",
			ConcedeAccepted: "Halloran nods slowly. \"That's the spirit Mr. Sawyer was hoping for.\" The new date goes on the paper.",
			Pushback: "\"Mr. Sawyer doesn't read sales sheets. He wants to know what you're doing for Connie.\"",
			HonestVoice: ExecutiveVoice.Ear, HonestLine: "\"I've put a record on the chart. Tell him that's the label Connie wants.\"",
			BluffVoice: ExecutiveVoice.Street, BluffLine: "\"Half the town's asking about my acts. Dee-Dee wants in on that, he waits a year.\"",
			BluffCaught: "\"Mr. Sawyer owns the room this town drinks in,\" Halloran says. \"Nobody's asking about your acts.\"",
			Seizure: "Halloran comes on the 1st with two men from the door at {venue}.",
			FixerRead: "He thinks he won that hand fair. A man that sure of himself never checks the deck."),
		[LienHolder.Kubiak] = new(LienHolder.Kubiak, "Walter Kubiak", "who runs a jukebox route that doesn't reach very far", "Mr. Pruitt", LienBar.Pay,
			Rate: 0.08f, Compounds: true, RefusesPayFrom: 0,
			Opening: "Pruitt sets his hat on your desk and opens a ledger that isn't yours. \"Mr. Kubiak sends his regards. He'd like to talk about clause fourteen.\"",
			WantsLine: "\"He's a simple man. Money on the table, and you've got another year. What you don't pay goes on the paper.\"",
			PayAccepted: "Pruitt counts it twice, writes a receipt in a hand like a bank's, and stamps the new date on the paper. \"Mr. Kubiak appreciates a man who pays.\"",
			ShowAccepted: "Pruitt reads the figures, then reads them again. \"Fine. He'll take it as a down payment on next year's money.\"",
			ConcedeAccepted: "\"It's not cash,\" Pruitt says, \"but he can count it.\" He writes the new date in.",
			Pushback: "\"Copies sold don't pay Mr. Kubiak's mechanics. He wants money.\"",
			HonestVoice: ExecutiveVoice.Suit, HonestLine: "\"The houses owe me ${0:N0}. He'll see it before Easter.\"",
			BluffVoice: ExecutiveVoice.Fixer, BluffLine: "\"Tell Walter I've got a check coming from a distributor. Big one.\"",
			BluffCaught: "Pruitt smiles without any warmth. \"Mr. Kubiak knows every distributor in this region. None of them is writing you a check.\"",
			Seizure: "Kubiak's men come on the 1st with a truck and a list. One of them asks if you'd like a jukebox.",
			FixerRead: "He's sure jukeboxes are about to explode. He's sure of a lot of things. Give him cash and it's a short meeting."),
		[LienHolder.Winters] = new(LienHolder.Winters, "Clyde Winters", "who plugs songs for a big publisher and wants a catalogue of his own", "Mr. Weisz", LienBar.Show,
			Rate: 0f, Compounds: false, RefusesPayFrom: 0,
			Opening: "Weisz has brought a trade paper with your label's name circled in pencil, or not circled, which is worse. \"Mr. Winters is a believer. Give him something to believe in.\"",
			WantsLine: "\"Hits are copyrights. Show him the label's making them. He isn't charging interest. He wants that noted.\"",
			PayAccepted: "\"He'll take it,\" Weisz says, and for a moment looks as if he'd like to ask where it's going.",
			ShowAccepted: "Weisz folds the paper with something like pleasure. \"He'll be glad. Another year.\"",
			ConcedeAccepted: "\"Not what he hoped for, but it's paper he can use.\" The new date goes on.",
			Pushback: "\"That's money. Mr. Winters wants to see the label moving.\"",
			HonestVoice: ExecutiveVoice.Ear, HonestLine: "\"I picked a Top 40 record. That's what he paid for, isn't it? Whatever it was he paid.\"",
			BluffVoice: ExecutiveVoice.Ear, BluffLine: "\"I've got a song in the can that's a hit. Tell him to wait for it.\"",
			BluffCaught: "\"Mr. Winters has heard that sentence from better men than you,\" Weisz says. \"He's said it himself.\"",
			Seizure: "Weisz sends a process server on the 1st, and a man to count the stock. Neither will say who's paying them.",
			FixerRead: "Twenty thousand dollars, no interest, and he sweats every December. Ask yourself what he's afraid of."),
		[LienHolder.Floria] = new(LienHolder.Floria, "Gene Floria", "a quiet man in a camel coat from a Major's regional office", "Mr. Lowe", LienBar.Show,
			Rate: 0.03f, Compounds: false, RefusesPayFrom: 1966,
			Opening: "Lowe's suit cost more than your pressing. \"My client is a patient investor. He wants to see the business grow.\"",
			WantsLine: "\"Growth. Growth is all he asks.\"",
			PayAccepted: "\"Very good.\" Lowe pockets the check as if it weighed nothing. The new date goes on.",
			ShowAccepted: "Lowe reads the sales figures with real interest, the way a man reads a menu. \"He'll be pleased. Keep it up.\"",
			ConcedeAccepted: "\"A sensible arrangement,\" Lowe says, and writes it down in a small, neat hand.",
			Pushback: "\"Money's very nice. My client would rather see the label grow.\"",
			HonestVoice: ExecutiveVoice.Suit, HonestLine: "\"Here's the interest in cash. The growth is coming.\"",
			BluffVoice: ExecutiveVoice.Fixer, BluffLine: "\"I'm about to sign an act everybody wants. Ask around.\"",
			BluffCaught: "\"We did ask around,\" Lowe says pleasantly.",
			Seizure: "Lowe's people come on the 1st, very politely, with an inventory form.",
			FixerRead: "You can't read him. That's the read.",
			RefusesPayLine: "\"My client isn't interested in being paid. He's interested in the label.\""),
		[LienHolder.Germain] = new(LienHolder.Germain, "Jack Germain", "who was the hottest jock in the region until the payola sweep", "Mr. Tolliver", LienBar.Concede,
			Rate: 0.06f, Compounds: false, RefusesPayFrom: 0,
			Opening: "Tolliver talks like a man reading copy over a record intro. \"Jack sends his best. Jack wants you to know he's doing fine. Jack asks, very casually, whether you've come across a small black book.\"",
			WantsLine: "\"Jack's a promotion consultant now. He'd like to be consulted. His boys, your records. And his name on a song or two.\"",
			PayAccepted: "Tolliver writes \"consulting fee\" on the receipt and underlines it twice. The new date goes on.",
			ShowAccepted: "\"Jack'll be proud. He'll tell everyone it was his boys.\"",
			ConcedeAccepted: "Tolliver beams. \"Now you're in the business.\" The new date goes on the paper.",
			Pushback: "\"Sales are sales. Jack wants to be part of it. That's the whole point of a consultant.\"",
			HonestVoice: ExecutiveVoice.Ear, HonestLine: "\"My records get played without Jack's boys. Tell him I picked a Top 40 record on my own.\"",
			BluffVoice: ExecutiveVoice.Fixer, BluffLine: "\"I've got jocks of my own now. Jack's boys can stand in line.\"",
			BluffCaught: "Tolliver laughs, a real one. \"Kid, Jack knows every jock you know. He bought most of them dinner.\"",
			Seizure: "Tolliver comes on the 1st with two men who used to carry Jack's records into stations.",
			FixerRead: "He's scared of a grand jury and he's lost something. A scared man who's lost something will pay to get it back."),
	};

	// ── the opening questions (§3.1a, §3.6) ──────────────────────────────────────────────────────────

	public sealed class PartyMemory {
		public PartyWhere Where = PartyWhere.Nothing;
		public PartyWho Who = PartyWho.Nothing;
		public PartyBrag Brag = PartyBrag.Nothing;
		public PartyEnd End = PartyEnd.Nothing;
		public PartyMemory Copy() => (PartyMemory)MemberwiseClone();
		public string Key => $"{Where}|{Who}|{Brag}|{End}";
	}

	public static readonly (PartyWhere Value, string Text)[] PartyWhereAnswers = {
		(PartyWhere.SupperClub, "White tablecloths, and a waiter who knew your name"),
		(PartyWhere.DjHouseParty, "Someone's living room, records stacked to the ceiling"),
		(PartyWhere.HotelBallroom, "Under a chandelier, with a dance band on a riser"),
		(PartyWhere.BarBackRoom, "Behind the bar, through a door marked Private"),
		(PartyWhere.Nothing, "There were walls. That's all I've got"),
	};
	public static readonly (PartyWho Value, string Text)[] PartyWhoAnswers = {
		(PartyWho.CamelCoat, "A man in a camel coat"),
		(PartyWho.DiscJockey, "Somebody who talked like he was introducing a record"),
		(PartyWho.RouteMan, "A heavy fella with a ring of keys on his belt"),
		(PartyWho.MajorMan, "A fella who said \"we\" as if he meant a whole building"),
		(PartyWho.Nothing, "A crowd. Nobody in particular"),
	};
	public static readonly (PartyBrag Value, string Text)[] PartyBragAnswers = {
		(PartyBrag.TheLabel, "Putting your name on a record. Then on every record in town"),
		(PartyBrag.TheAct, "A kid you'd heard once, who was going to change everything"),
		(PartyBrag.Money, "How much you could lay your hands on by Monday"),
		(PartyBrag.Hits, "That you could hear a hit before the second chorus"),
		(PartyBrag.Nothing, "For once, you kept your mouth shut"),
	};
	public static readonly (PartyEnd Value, string Text)[] PartyEndAnswers = {
		(PartyEnd.CardTable, "Someone shuffling, and a pile of chips that wasn't yours"),
		(PartyEnd.Car, "Headlights, and somebody else's hand on the wheel"),
		(PartyEnd.PhoneCall, "A phone ringing in a hallway, somebody holding it out to you"),
		(PartyEnd.Nothing, "A hum. And then this morning"),
	};

	/// <summary>Which holders an answer fits (§3.6). Each holder fits exactly five answers; "nothing" fits nobody.</summary>
	private static IEnumerable<LienHolder> Fits(PartyMemory m) {
		switch (m.Where) {
			case PartyWhere.SupperClub: yield return LienHolder.Sawyer; break;
			case PartyWhere.DjHouseParty: yield return LienHolder.Winters; yield return LienHolder.Germain; break;
			case PartyWhere.HotelBallroom: yield return LienHolder.Floria; break;
			case PartyWhere.BarBackRoom: yield return LienHolder.Sawyer; yield return LienHolder.Kubiak; break;
		}
		switch (m.Who) {
			case PartyWho.CamelCoat: yield return LienHolder.Floria; break;
			case PartyWho.DiscJockey: yield return LienHolder.Winters; yield return LienHolder.Germain; break;
			case PartyWho.RouteMan: yield return LienHolder.Kubiak; break;
			case PartyWho.MajorMan: yield return LienHolder.Winters; yield return LienHolder.Floria; break;
		}
		switch (m.Brag) {
			case PartyBrag.TheLabel: yield return LienHolder.Kubiak; yield return LienHolder.Floria; break;
			case PartyBrag.TheAct: yield return LienHolder.Sawyer; yield return LienHolder.Germain; break;
			case PartyBrag.Money: yield return LienHolder.Sawyer; yield return LienHolder.Kubiak; break;
			case PartyBrag.Hits: yield return LienHolder.Winters; yield return LienHolder.Germain; break;
		}
		switch (m.End) {
			case PartyEnd.CardTable: yield return LienHolder.Sawyer; break;
			case PartyEnd.Car: yield return LienHolder.Kubiak; yield return LienHolder.Germain; break;
			case PartyEnd.PhoneCall: yield return LienHolder.Winters; yield return LienHolder.Floria; break;
		}
	}

	/// <summary>Per-holder fit scores for a set of answers.</summary>
	public static Dictionary<LienHolder, int> PartyScores(PartyMemory m) {
		var scores = Enum.GetValues<LienHolder>().ToDictionary(h => h, _ => 0);
		foreach (var h in Fits(m)) scores[h]++;
		return scores;
	}

	/// <summary>The holder for these answers on this world: the pool is every holder within 1 of the top score, weighted 2:1
	/// toward the top scorers, picked by a keyed draw (stable for a seed and a set of answers; never the shared stream).</summary>
	public static LienHolder PickHolder(PartyMemory m, ulong seed) {
		var scores = PartyScores(m);
		int top = scores.Values.Max();
		var pool = Enum.GetValues<LienHolder>().Where(h => scores[h] >= top - 1).Select(h => (h, w: scores[h] == top ? 2 : 1)).ToList();
		int total = pool.Sum(p => p.w);
		double u = PhiloxCounterRandom.Unit(seed, "lien-holder", m.Key, 0, LienPickDecision);
		double acc = 0;
		foreach (var (h, w) in pool) { acc += (double)w / total; if (u < acc) return h; }
		return pool[^1].h;
	}

	/// <summary>The answers the chosen holder does not fit: what the Label Man remembers wrongly (clues later; §3.4).</summary>
	public static List<string> WrongMemories(PartyMemory m, LienHolder h) {
		var wrong = new List<string>();
		bool Fit(PartyMemory single) => Fits(single).Contains(h);
		if (m.Where != PartyWhere.Nothing && !Fit(new PartyMemory { Where = m.Where })) wrong.Add(nameof(PartyMemory.Where));
		if (m.Who != PartyWho.Nothing && !Fit(new PartyMemory { Who = m.Who })) wrong.Add(nameof(PartyMemory.Who));
		if (m.Brag != PartyBrag.Nothing && !Fit(new PartyMemory { Brag = m.Brag })) wrong.Add(nameof(PartyMemory.Brag));
		if (m.End != PartyEnd.Nothing && !Fit(new PartyMemory { End = m.End })) wrong.Add(nameof(PartyMemory.End));
		return wrong;
	}

	// ── state ──────────────────────────────────────────────────────────────────────────────────────

	public sealed class LienLedgerEntry {
		public GameDate Date;
		public LienLedgerKind Kind;
		public float Amount;
		public string Detail;
	}

	public sealed class LienState {
		public LienHolder Holder;
		public PartyMemory Memory = new();
		public List<string> WrongMemories = new();
		public float FaceSum = LienFaceSum;
		public float Rate = LienRate;
		/// <summary>The hometown's biggest performance room at founding ("the River Lantern"): the room Sawyer owns. Kept for
		/// every holder so a line can name it; fixed at founding.</summary>
		public string Venue, VenueId;
		public GameDate Signed;
		public GameDate LastAccrual;
		public float InterestAccrued, InterestPaid, PrincipalPaid;
		/// <summary>The 1st the lien next falls due. A December sitting that meets a bar moves it to the next 1 January.</summary>
		public GameDate Deadline;
		/// <summary>Where a met bar moved the lien; it takes over from <see cref="Deadline"/> on that 1st.</summary>
		public GameDate? PendingDeadline;
		/// <summary>The year the bars are drawn from (Pay slice, Show copies).</summary>
		public int BarYear;
		/// <summary>True once a sitting has met a bar for the current <see cref="Deadline"/>.</summary>
		public bool Extended;
		public int ConsecutiveFailures;
		public float Standing = 0.5f;
		public bool OpeningSeen;
		/// <summary>Last sitting's concession was "write it onto the paper": he won't do that twice running.</summary>
		public bool LastWasWrittenOn;
		public long UnitsAtLastSitting;
		public int Top40AtLastSitting;
		public string Ending;
		public List<LienLedgerEntry> Ledger = new();

		public float Outstanding => R2(FaceSum - PrincipalPaid);
		public float InterestDue => R2(InterestAccrued - InterestPaid);
		public bool IsReckoning => Deadline.year >= LienFinalYear;
		/// <summary>The authored profile, with "{venue}" still in it; <see cref="PlayerDesk.LienProfile"/> fills it in.</summary>
		public LienHolderProfile Profile => LienHolders[Holder];

		internal void Add(GameDate date, LienLedgerKind kind, float amount, string detail = null) =>
			Ledger.Add(new LienLedgerEntry { Date = date, Kind = kind, Amount = R2(amount), Detail = detail });

		/// <summary>Simple interest on the outstanding face sum, accrued actual/365 up to <paramref name="date"/>.</summary>
		internal void AccrueTo(GameDate date) {
			int days = DaysBetween(LastAccrual, date);
			if (days <= 0) return;
			float interest = R2(Outstanding * Rate * days / 365f);
			if (interest > 0f) { InterestAccrued = R2(InterestAccrued + interest); Add(date, LienLedgerKind.InterestAccrued, interest); }
			LastAccrual = date;
		}

		/// <summary>Interest that will be owed on the deadline, without accruing it.</summary>
		public float InterestDueBy(GameDate date) {
			int days = Math.Max(0, DaysBetween(LastAccrual, date));
			return R2(InterestDue + Outstanding * Rate * days / 365f);
		}
	}

	private LienState lien;
	public LienState Lien => lien;

	/// <summary>The holder's profile with the hometown's room named in it. Null without a lien.</summary>
	public LienHolderProfile LienProfile {
		get {
			if (lien == null) return null;
			var p = lien.Profile;
			string venue = string.IsNullOrEmpty(lien.Venue) ? LienVenueFallback : lien.Venue;
			string V(string s) => s?.Replace("{venue}", venue);
			return p with {
				Title = V(p.Title), Opening = V(p.Opening), WantsLine = V(p.WantsLine), PayAccepted = V(p.PayAccepted),
				ShowAccepted = V(p.ShowAccepted), ConcedeAccepted = V(p.ConcedeAccepted), Pushback = V(p.Pushback),
				HonestLine = V(p.HonestLine), BluffLine = V(p.BluffLine), BluffCaught = V(p.BluffCaught), Seizure = V(p.Seizure),
				FixerRead = V(p.FixerRead), RefusesPayLine = V(p.RefusesPayLine),
			};
		}
	}

	/// <summary>True when this holder won't take money at a sitting this year (Floria from the December 1966 sitting).
	/// The reckoning always takes money.</summary>
	public bool LienRefusesPay => lien != null && !lien.IsReckoning && lien.Profile.RefusesPayFrom > 0 && lien.BarYear >= lien.Profile.RefusesPayFrom;

	/// <summary>The hometown's most popular room: the biggest performance room in the home scene, ties broken by id.</summary>
	private SceneRoomProfile HomeVenue() {
		string scene = Label == null ? null : LocalScenePersistenceService.SceneIdFor(Label.homeCityId);
		return scene == null ? null : SceneRoomCatalog.ForPlace(scene).Where(r => r.IsPerformance)
			.OrderByDescending(r => r.Capacity).ThenBy(r => r.Id, StringComparer.Ordinal).FirstOrDefault();
	}

	public bool LienMorningPending => lien != null && !lien.OpeningSeen && !IsGameOver;
	/// <summary>The title the game-over page shows: the doors closing, or the decade's reckoning.</summary>
	public string GameOverTitle { get; private set; }

	private static GameDate LienToday => TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;

	private void FoundLien(PartyMemory memory, LienHolder? forced = null) {
		memory ??= new PartyMemory();
		var today = LienToday;
		var holder = forced ?? PickHolder(memory, BandLife.WorldSeed);
		var room = HomeVenue();
		lien = new LienState {
			Holder = holder, Memory = memory.Copy(), WrongMemories = WrongMemories(memory, holder), Rate = LienHolders[holder].Rate,
			Venue = room == null ? LienVenueFallback : "the " + room.Name, VenueId = room?.Id,
			Signed = today, LastAccrual = today, Deadline = new GameDate(today.year + 1, 1, 1), BarYear = today.year,
		};
		lien.Add(today, LienLedgerKind.Signed, LienFaceSum, holder.ToString());
	}

	public void DismissLienMorning() {
		if (lien == null) return;
		lien.OpeningSeen = true;
		Changed?.Invoke();
	}

	// ── what was lost (§3.1, §3.5) ───────────────────────────────────────────────────────────────────

	/// <summary>The archetype's signature belonging (§3.5): one per archetype, wherever this holder's variant puts it.</summary>
	public string SignatureBelonging() => Archetype switch {
		FoundingArchetype.PawnShopOwner => "the whole back room of stock from the shop",
		FoundingArchetype.ExMusician => "your 1926 pedal steel",
		FoundingArchetype.PromoMan => "your address book with every jock's home number in it",
		_ => "ten years of chart files you kept from the trade",
	};

	/// <summary>What this holder's night took besides the car and the signature belonging (§3.5). The Winters address book
	/// is the Promo Man's signature one, so it isn't named twice.</summary>
	private string HolderBelongings() => lien?.Holder switch {
		LienHolder.Sawyer => "your watch",
		LienHolder.Winters when Archetype == FoundingArchetype.PromoMan => "your tape recorder",
		LienHolder.Winters when Archetype == FoundingArchetype.ExMusician => "your address book",
		LienHolder.Winters => "your address book, your tape recorder",
		LienHolder.Floria => "your good overcoat, your briefcase",
		LienHolder.Germain => "your chequebook",
		_ => null,
	};

	public string LostBelongings() {
		string extra = HolderBelongings();
		return "the car, " + (extra == null ? "" : extra + ", ") + "and " + SignatureBelonging();
	}

	/// <summary>The morning after, line by line. The voices argue about the night (theories, not facts: the night is a
	/// mystery). Shown once, on the first page after founding.</summary>
	public List<CallLine> LienMorningLines() {
		var lines = new List<CallLine>();
		if (lien == null || Label == null) return lines;
		void Say(string text, ExecutiveVoice voice = ExecutiveVoice.None, string speaker = "") =>
			lines.Add(new CallLine { beat = RolodexSceneBeat.Opening, text = text, voice = voice, speaker = speaker });
		Say("1 January 1960, about noon. Your head is splitting. You're wearing yesterday's shirt and one shoe.");
		Say("The lawyer's letter is still on the desk. Some time after midnight you signed a lien on everything you own " +
			$"and everything {Label.labelName} will ever own. It secures ${lien.FaceSum:N0}, and it falls due on {lien.Deadline.ToHeadlineString()}.");
		Say($"The founding money is still in the cigar box: ${Label.cashReserves:N0}. Everything else is gone: {LostBelongings()}. You don't know where.");
		Say($"Under the lamp, the scrawl at the foot of the letter turns out to read {LienProfile.Lawyer}, \"for the holder\". It doesn't say who the holder is. " +
			"It says you may apply for an extension at a sitting in the last two weeks of December.");
		foreach (var voice in LienMorningVoices()) lines.Add(voice);
		Say("So the label you meant to start anyway starts today, with a fuse on it.");
		return lines;
	}

	/// <summary>The four voices' first theories about the night, each built from what the player said they remembered (§3.1a).
	/// They are theories, not facts: an answer the holder doesn't fit is argued as hard as one he does. A voice with nothing to
	/// go on falls back on its habit. The one the player trusts most (their highest instinct) speaks first.</summary>
	public List<CallLine> LienMorningVoices() {
		var list = new List<CallLine>();
		if (lien == null) return list;
		var m = lien.Memory;
		string fixer = m.End switch {
			PartyEnd.CardTable => "A card game. It's always a card game. You said so yourself: a pile of chips that wasn't yours.",
			PartyEnd.Car => "Somebody else's hand on the wheel. Nobody hands a man a pen in a moving car. You were driven there, and you were driven home.",
			PartyEnd.PhoneCall => "A phone ringing in a hallway, and somebody holding it out to you. Somebody was waiting on that call, and made it worth the wait.",
			_ when m.Brag == PartyBrag.Money => "You can't say how it ended, but you bragged about money you didn't have. Somebody wrote down what you said.",
			_ => "A card game. It's always a card game.",
		};
		string suit = m.Where switch {
			PartyWhere.SupperClub => "A supper club has a maitre d' and a table of men in good suits, and one of them had paper in an inside pocket. You'd never sign a thing like that. Not sober.",
			PartyWhere.DjHouseParty => "A living room full of records and no lawyer in the house, so somebody brought the paper with him. That's not a party, that's an appointment.",
			PartyWhere.HotelBallroom => "A ballroom has a manager, a night clerk, a bellhop. Somebody there can say who signed what. You'd never sign a thing like that with a lawyer in the room, which means there wasn't one.",
			PartyWhere.BarBackRoom => "A back room, through a door marked Private. No witnesses, and a good reason for that. You'd never sign a thing like that sober.",
			_ => "You can't say where, so I can't say who witnessed it. You'd never sign a thing like that. Not sober. Not with a lawyer in the room.",
		};
		string ear = m.Brag switch {
			PartyBrag.TheAct => "You were going on about a kid you'd heard once, who was going to change everything. Somebody liked the sound of that. Somebody wanted a piece of it.",
			PartyBrag.Hits => "You said you could hear a hit before the second chorus. Somebody wanted to hear you prove it.",
			PartyBrag.TheLabel => "Your name on every record in town, you said. That's a lot of noise to make in front of a man who'd been listening.",
			PartyBrag.Money => "Money you didn't have isn't a song, it's a drum roll. Somebody was counting along.",
			_ when m.Where == PartyWhere.DjHouseParty => "Records stacked to the ceiling, and something on the hi-fi you can't place. I'd start with what was playing.",
			_ => "There was a band. A good one. You were up there with them, you remember the horns.",
		};
		string street = m.Who switch {
			PartyWho.CamelCoat => "A man in a camel coat. Money wears a camel coat, and it doesn't take it off.",
			PartyWho.DiscJockey => "Somebody who talked like he was introducing a record. A jock will talk you into anything. It's the job.",
			PartyWho.RouteMan => "A heavy fella with a ring of keys. Keys mean doors, and doors mean he owns something that locks.",
			PartyWho.MajorMan => "A fella who said \"we\" as if he meant a whole building. A building has lawyers. A building keeps paper.",
			_ => "There was a woman. There's always a woman.",
		};
		var inst = InstinctProfile;
		foreach (var (voice, text) in new[] { (ExecutiveVoice.Fixer, fixer), (ExecutiveVoice.Suit, suit), (ExecutiveVoice.Ear, ear), (ExecutiveVoice.Street, street) }
			.OrderByDescending(v => Score(inst, v.Item1)))
			list.Add(new CallLine { beat = RolodexSceneBeat.Opening, text = text, voice = voice, speaker = "" });
		return list;
	}

	/// <summary>Everything the night took, one thing each: the car, what this holder's night took, the archetype's signature belonging.</summary>
	public List<string> LienGoneList() {
		var gone = new List<string> { "the car" };
		string extra = HolderBelongings();
		if (extra != null) gone.AddRange(extra.Split(", "));
		gone.Add(SignatureBelonging());
		return gone;
	}

	// ── the bars (§3.2) ──────────────────────────────────────────────────────────────────────────────

	private long PlayerUnitsSold() => ReleasedRecords.Sum(r => (long)r.totalUnitsSold);

	/// <summary>What Pay asks at the coming deadline: interest due by then plus the year's slice (the whole balance at the reckoning).</summary>
	public float LienPayBar() {
		if (lien == null) return 0f;
		float interest = lien.InterestDueBy(lien.Deadline);
		if (lien.IsReckoning) return R2(interest + lien.Outstanding);
		float slice = Mathf.Min(LienSlice.GetValueOrDefault(Math.Clamp(lien.BarYear, 1960, 1968)), lien.Outstanding);
		return R2(interest + slice);
	}

	public int LienShowUnitsBar() => lien == null ? 0 : LienShowUnits.GetValueOrDefault(Math.Clamp(lien.BarYear, 1960, 1968));
	public long LienUnitsSinceSitting() => lien == null ? 0 : Math.Max(0, PlayerUnitsSold() - lien.UnitsAtLastSitting);
	public int LienTop40SinceSitting() => lien == null || Label == null ? 0 : Math.Max(0, Label.top40Hits - lien.Top40AtLastSitting);

	/// <summary>Whether the Show bar is met, and the sentence that says what it takes.</summary>
	public (bool Met, string What) LienShow() {
		if (lien == null) return (false, "");
		if (lien.IsReckoning) return (false, "At the reckoning only money counts.");
		if (lien.BarYear <= 1960) {
			bool any = ReleasedRecords.Any();
			return (any, any ? "A record is out with the label's name on it." : "He wants to see a record pressed and out.");
		}
		int bar = LienShowUnitsBar();
		long units = LienUnitsSinceSitting();
		int hits = LienTop40SinceSitting();
		bool met = units >= bar || (lien.BarYear >= 1962 && hits > 0);
		string what = $"{units:N0} of {bar:N0} copies sold since the last sitting" + (lien.BarYear >= 1962 ? ", or a Top 40 record in that time" : "") + ".";
		return (met, what);
	}

	/// <summary>The pressed stock on hand, valued at the dealer price.</summary>
	private float StockValueOnHand() => R2(inventory.Values.Sum(s => Math.Max(0, s.Remaining)) * DealerPrice);

	/// <summary>Why the sitting can't happen now, or null.</summary>
	public string LienSittingBlock() {
		if (lien == null) return "There's no lien.";
		if (IsGameOver) return "It's over.";
		if (lien.Extended) return $"The lien runs to {(lien.PendingDeadline ?? lien.Deadline).ToHeadlineString()}. {LienProfile.Lawyer} sits down with you again in the last two weeks before it.";
		var today = LienToday;
		var opens = AddDays(lien.Deadline, -LienWindowDays);
		if (today < opens) return $"{LienProfile.Lawyer} sits down with you from {opens.ToHeadlineString()}.";
		if (!AtHome) return $"{LienProfile.Lawyer} comes to the office; you have to be home.";
		var clock = TimeManager.Instance;
		if (clock == null) return "No clock.";
		if (clock.CurrentHour < BackerOfficeFromHour || clock.CurrentHour + LienSittingHours > BackerOfficeUntilHour)
			return $"{LienProfile.Lawyer} keeps office hours: {BackerOfficeFromHour} a.m. to {BackerOfficeUntilHour - 12} p.m.";
		return null;
	}

	/// <summary>True while the sitting window is open and nothing has extended the lien yet.</summary>
	public bool LienSittingDue => lien != null && !lien.Extended && !IsGameOver && LienToday >= AddDays(lien.Deadline, -LienWindowDays);

	// ── the sitting (§3.2) ───────────────────────────────────────────────────────────────────────────

	public enum LienVerb { Pay, Show, ConcedeStock, WriteItOn, PayInFull, HandOver, CounterInterest, CounterHonest, CounterBluff, WalkOut }

	public sealed class LienOption {
		public LienVerb verb;
		public string label, subLabel, disabledReason;
		public ExecutiveVoice voice;
		public bool isBluff, enabled = true;
		public float cash;
	}

	public sealed class LienSittingLive {
		public CallStage stage = CallStage.Open;
		public readonly List<CallLine> transcript = new();
		/// <summary>Show was offered and pushed back on; this round's counters answer that.</summary>
		public bool showPushed;
		public bool bluffCaught;
		public void Say(string text, string speaker = "", ExecutiveVoice voice = ExecutiveVoice.None, bool isPlayer = false) {
			if (!string.IsNullOrEmpty(text)) transcript.Add(new CallLine { beat = RolodexSceneBeat.Opening, text = text, speaker = speaker, voice = voice, isPlayer = isPlayer });
		}
	}

	private LienSittingLive activeSitting;
	public LienSittingLive ActiveSitting => activeSitting;

	public bool OpenLienSitting(out string message) {
		message = LienSittingBlock();
		if (message != null) return false;
		if (!Require(LienSittingHours, out message)) return false;
		Spend(LienSittingHours);
		var p = LienProfile;
		var live = new LienSittingLive();
		live.Say(p.Opening);
		live.Say(lien.IsReckoning ? $"\"It's the end of the decade, and the end of the paper. Mr. {p.Name.Split(' ')[^1]} wants ${LienPayBar():N0}, all of it.\"" : p.WantsLine, p.Lawyer);
		// Passive reads: each voice speaks only past its threshold, and only about a fact that is true.
		var inst = InstinctProfile;
		float pay = LienPayBar();
		if (LienRefusesPay) { live.Say(p.RefusesPayLine, p.Lawyer); if (inst.TheSuit >= 2) live.Say("He won't take a dime. That's not generosity. He wants the whole thing, later.", "", ExecutiveVoice.Suit); }
		else if (inst.TheSuit >= 3) live.Say($"He'll want ${pay:N0} to roll it. You have ${Mathf.Max(0f, Label.cashReserves):N0}.", "", ExecutiveVoice.Suit);
		else if (inst.TheSuit >= 2) live.Say($"Money. Something like ${Mathf.Round(pay / 100f) * 100f:N0}.", "", ExecutiveVoice.Suit);
		var show = LienShow();
		if (!lien.IsReckoning && inst.TheStreet >= 3) live.Say(show.What + (show.Met ? " That's enough to show him." : " Not enough to show him yet."), "", ExecutiveVoice.Street);
		if (!lien.IsReckoning && inst.TheFixer >= 3) live.Say(p.FixerRead, "", ExecutiveVoice.Fixer);
		activeSitting = live;
		message = $"Sat down with {p.Lawyer}, for {p.Name}.";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	public void CloseLienSitting() { activeSitting = null; Changed?.Invoke(); }

	// ── what the sitting screen shows (read-only views of the ledger and the bars) ────────────────────

	public enum LienYearOutcome { Met, NoShow, Seized }

	/// <summary>The pressed stock on hand at the dealer price: what "give him stock instead" can cover.</summary>
	public float LienStockValue() => StockValueOnHand();

	/// <summary>How each past year's deadline went, oldest first, read from the ledger. A seizure's short extension and the
	/// sitting that follows it are one year, not two.</summary>
	public List<LienYearOutcome> LienYearOutcomes() {
		var list = new List<LienYearOutcome>();
		if (lien == null) return list;
		bool afterSeizure = false;
		foreach (var e in lien.Ledger) {
			if (e.Kind != LienLedgerKind.Extended) continue;
			bool seizure = e.Detail?.StartsWith("seizure", StringComparison.Ordinal) == true;
			bool noShow = e.Detail?.StartsWith("no-show", StringComparison.Ordinal) == true;
			if (afterSeizure && !seizure) { afterSeizure = false; continue; }
			list.Add(seizure ? LienYearOutcome.Seized : noShow ? LienYearOutcome.NoShow : LienYearOutcome.Met);
			afterSeizure = seizure;
		}
		return list;
	}

	/// <summary>What next December's sitting will ask, once this one has rolled the lien: the principal slice, the copies Show
	/// wants, the year it falls due, and whether that is the reckoning.</summary>
	public (bool Reckoning, float Slice, int Units, int DueYear) LienNextBars() {
		if (lien == null) return (false, 0f, 0, 0);
		var due = lien.PendingDeadline ?? lien.Deadline;
		int bar = Math.Clamp(due.year - 1, 1960, 1968);
		return (due.year >= LienFinalYear, LienSlice.GetValueOrDefault(bar), LienShowUnits.GetValueOrDefault(bar), due.year);
	}

	public List<LienOption> LienSittingOptions(LienSittingLive live) {
		var list = new List<LienOption>();
		if (lien == null || live == null) return list;
		float cash = Mathf.Max(0f, Label.cashReserves);
		float pay = LienPayBar();
		if (lien.IsReckoning) {
			list.Add(new LienOption { verb = LienVerb.PayInFull, label = $"Pay him every cent (${pay:N0})", cash = pay,
				subLabel = "The paper is torn up in front of you. The label is yours, free and clear.",
				enabled = cash >= pay, disabledReason = $"You have ${cash:N0}." });
			list.Add(new LienOption { verb = LienVerb.HandOver, label = "Hand him the keys", subLabel = "He takes the catalogue and the lease. It's over." });
			return list;
		}
		bool refuses = LienRefusesPay;
		list.Add(new LienOption { verb = LienVerb.Pay, label = $"Pay him (${pay:N0})", cash = pay, subLabel = "Interest and this year's slice of principal.",
			enabled = cash >= pay && !refuses, disabledReason = refuses ? LienProfile.RefusesPayLine : $"You have ${cash:N0}." });
		var show = LienShow();
		list.Add(new LienOption { verb = LienVerb.Show, label = "Show him the books", subLabel = show.What,
			enabled = show.Met && !live.showPushed && !live.bluffCaught, disabledReason = live.bluffCaught ? "He's stopped listening to that." : live.showPushed ? "He's already pushed back on that." : show.What });
		float stock = StockValueOnHand();
		list.Add(new LienOption { verb = LienVerb.ConcedeStock, label = "Give him pressed stock instead", subLabel = $"Copies on hand at ${DealerPrice:F2}: ${stock:N0} of the ${pay:N0}.",
			enabled = stock >= pay && pay > 0f, disabledReason = $"Your stock on hand is worth ${stock:N0}." });
		list.Add(new LienOption { verb = LienVerb.WriteItOn, label = "Ask him to write it onto the paper", subLabel = $"The face sum grows by ${R2(pay * LienWrittenOnMultiple):N0}. You'll owe it in 1970.",
			enabled = !lien.LastWasWrittenOn && pay > 0f, disabledReason = "He wrote it on last year. He won't do it twice running." });
		list.Add(new LienOption { verb = LienVerb.WalkOut, label = "Show him out", subLabel = $"Nothing settled. On {lien.Deadline.ToHeadlineString()} he takes the payment if it's there, or seizes what he can." });
		return list;
	}

	/// <summary>The answers to a pushback on Show.</summary>
	public List<LienOption> LienCounterOptions(LienSittingLive live) {
		var list = new List<LienOption>();
		if (lien == null || live == null) return list;
		var p = LienProfile;
		float interest = lien.InterestDueBy(lien.Deadline);
		float cash = Mathf.Max(0f, Label.cashReserves);
		list.Add(new LienOption { verb = LienVerb.CounterInterest, label = $"Pay the interest on top (${interest:N0})", cash = interest,
			subLabel = "The figures and the interest, in cash.", enabled = cash >= interest, disabledReason = $"You have ${cash:N0}." });
		var inst = InstinctProfile;
		int honestScore = Score(inst, p.HonestVoice);
		bool honestFact = HonestFactHolds(p);
		if (honestScore >= 3 && honestFact)
			list.Add(new LienOption { verb = LienVerb.CounterHonest, voice = p.HonestVoice, label = HonestLineFor(p), subLabel = "True, and he can check it." });
		int bluffScore = Score(inst, p.BluffVoice);
		if (bluffScore >= 3)
			list.Add(new LienOption { verb = LienVerb.CounterBluff, voice = p.BluffVoice, isBluff = true, label = p.BluffLine, subLabel = "Not true. He may call it." });
		list.Add(new LienOption { verb = LienVerb.WalkOut, label = "Drop it", subLabel = "Back to what you can offer." });
		return list;
	}

	private static int Score(ExecutiveInstinctProfile i, ExecutiveVoice v) => v switch {
		ExecutiveVoice.Ear => i.TheEar, ExecutiveVoice.Street => i.TheStreet, ExecutiveVoice.Suit => i.TheSuit, ExecutiveVoice.Fixer => i.TheFixer, _ => 0,
	};

	/// <summary>The fact the honest counter claims: receivables cover the interest (Suit), or the label has charted (Ear).</summary>
	private bool HonestFactHolds(LienHolderProfile p) => p.HonestVoice switch {
		ExecutiveVoice.Suit when p.Id == LienHolder.Kubiak => Label.outstandingWholesaleReceivables >= lien.InterestDueBy(lien.Deadline),
		ExecutiveVoice.Suit => Label.cashReserves >= lien.InterestDueBy(lien.Deadline),
		ExecutiveVoice.Ear => Label.top40Hits > 0,
		_ => false,
	};

	private string HonestLineFor(LienHolderProfile p) => p.Id == LienHolder.Kubiak
		? string.Format(System.Globalization.CultureInfo.InvariantCulture, p.HonestLine, Label.outstandingWholesaleReceivables) : p.HonestLine;

	public bool ChooseLienOption(LienSittingLive live, LienOption opt, out string message) {
		message = null;
		if (lien == null || live == null || opt == null || !opt.enabled) { message = opt?.disabledReason ?? "You can't do that."; return false; }
		var p = LienProfile;
		var today = LienToday;
		live.Say(opt.label, "You", opt.voice, isPlayer: true);
		switch (opt.verb) {
			case LienVerb.Pay:
				TakeLienPayment(today, LienPayBar(), LienLedgerKind.Paid);
				Extend(live, LienBar.Pay, p.PayAccepted);
				break;
			case LienVerb.Show:
				// Every holder takes a record out as the first year's Show (§3.2): the 1960 bar is the same for all five.
				if (p.Stresses == LienBar.Show || lien.BarYear <= 1960) {
					if (!LienRefusesPay) PayInterestIfAny(today, onlyIfCash: true);
					Extend(live, LienBar.Show, p.ShowAccepted);
					break;
				}
				live.showPushed = true;
				live.stage = CallStage.Pushback;
				live.Say(p.Pushback, p.Lawyer);
				break;
			case LienVerb.ConcedeStock: {
				float due = LienPayBar();
				int copies = HandOverStock(due, today);
				lien.AccrueTo(today);
				float interest = Mathf.Min(lien.InterestDue, due);
				lien.InterestPaid = R2(lien.InterestPaid + interest);
				float principal = R2(Mathf.Min(lien.Outstanding, due - interest));
				lien.PrincipalPaid = R2(lien.PrincipalPaid + principal);
				lien.Add(today, LienLedgerKind.ConcededStock, due, $"{copies} copies");
				Extend(live, LienBar.Concede, p.ConcedeAccepted);
				break;
			}
			case LienVerb.WriteItOn: {
				float add = R2(LienPayBar() * LienWrittenOnMultiple);
				lien.AccrueTo(today);
				float capitalised = lien.InterestDue;
				lien.InterestPaid = R2(lien.InterestPaid + capitalised);
				lien.FaceSum = R2(lien.FaceSum + add);
				lien.Add(today, LienLedgerKind.WrittenOn, add, $"interest {capitalised:N2} folded in");
				Extend(live, LienBar.Concede, p.ConcedeAccepted, writtenOn: true);
				break;
			}
			case LienVerb.CounterInterest:
				PayInterestIfAny(today, onlyIfCash: false);
				Extend(live, LienBar.Show, p.ShowAccepted);
				break;
			case LienVerb.CounterHonest:
				PayInterestIfAny(today, onlyIfCash: true);
				Extend(live, LienBar.Show, p.ShowAccepted);
				break;
			case LienVerb.CounterBluff: {
				int score = Score(InstinctProfile, p.BluffVoice);
				if (score >= LienBluffInstinctBar && lien.Standing >= LienBluffStandingBar) {
					lien.Standing = Mathf.Max(0f, lien.Standing - 0.05f);
					PayInterestIfAny(today, onlyIfCash: true);
					Extend(live, LienBar.Show, p.ShowAccepted);
				} else {
					lien.Standing = Mathf.Max(0f, lien.Standing - 0.2f);
					live.bluffCaught = true;
					live.stage = CallStage.Open;
					live.Say(p.BluffCaught, p.Lawyer);
				}
				break;
			}
			case LienVerb.WalkOut:
				if (live.stage == CallStage.Pushback) { live.stage = CallStage.Open; break; }
				live.Say($"{p.Lawyer} puts his hat back on. \"Then I'll see you on the 1st.\"");
				live.stage = CallStage.Ended;
				Note($"Showed {p.Lawyer} out with nothing settled. The lien falls due on {lien.Deadline.ToHeadlineString()}.");
				break;
			case LienVerb.PayInFull: {
				TakeLienPayment(today, LienPayBar(), LienLedgerKind.PaidClean);
				live.Say($"{p.Lawyer} tears the paper in half, then in half again. \"Mr. {p.Name.Split(' ')[^1]} says it was a pleasure doing business.\"");
				live.stage = CallStage.Ended;
				EndDecade(paid: true);
				break;
			}
			case LienVerb.HandOver:
				live.Say($"{p.Lawyer} takes the keys. He doesn't look at you while he does it.");
				live.stage = CallStage.Ended;
				lien.Add(today, LienLedgerKind.Taken, lien.Outstanding);
				EndDecade(paid: false);
				break;
		}
		message = live.transcript.LastOrDefault()?.text;
		Changed?.Invoke();
		return true;
	}

	private void Extend(LienSittingLive live, LienBar bar, string line, bool writtenOn = false) {
		var p = LienProfile;
		var today = LienToday;
		live.Say(line, p.Lawyer);
		if (bar == p.Stresses) lien.Standing = Mathf.Min(1f, lien.Standing + 0.1f);
		lien.LastWasWrittenOn = writtenOn;
		lien.ConsecutiveFailures = 0;
		lien.Extended = true;
		lien.UnitsAtLastSitting = PlayerUnitsSold();
		lien.Top40AtLastSitting = Label.top40Hits;
		var next = new GameDate(lien.Deadline.year + 1, 1, 1);
		lien.Add(today, LienLedgerKind.Extended, 0f, $"{bar} → {next.ToShortString()}");
		lien.PendingDeadline = next;
		live.stage = CallStage.Resolved;
		Note($"{p.Lawyer} extended the lien: it now runs to {next.ToHeadlineString()}.");
	}

	/// <summary>Cash above zero only, interest first. Booked as an expense for the interest, financing for the principal.</summary>
	private float TakeLienPayment(GameDate date, float amount, LienLedgerKind kind) {
		lien.AccrueTo(date);
		float pay = R2(Mathf.Min(Mathf.Max(0f, Label.cashReserves), amount));
		if (pay <= 0f) return 0f;
		float toInterest = R2(Mathf.Min(pay, lien.InterestDue)), toPrincipal = R2(Mathf.Min(lien.Outstanding, pay - toInterest));
		lien.InterestPaid = R2(lien.InterestPaid + toInterest);
		lien.PrincipalPaid = R2(lien.PrincipalPaid + toPrincipal);
		Label.cashReserves -= toInterest + toPrincipal;
		Label.monthlyExpenses += toInterest;
		lien.Add(date, kind, toInterest + toPrincipal);
		return toInterest + toPrincipal;
	}

	private void PayInterestIfAny(GameDate date, bool onlyIfCash) {
		lien.AccrueTo(date);
		float due = lien.InterestDue;
		if (due <= 0f) return;
		if (onlyIfCash && Label.cashReserves < due) return;
		TakeLienPayment(date, due, LienLedgerKind.Paid);
	}

	/// <summary>Pressed copies handed over at the dealer price, largest stock first, until <paramref name="value"/> is covered.</summary>
	private int HandOverStock(float value, GameDate date) {
		int need = Mathf.CeilToInt(value / DealerPrice - 0.0001f), took = 0;
		foreach (var (id, stock) in inventory.Where(kv => kv.Value.Remaining > 0).OrderByDescending(kv => kv.Value.Remaining).ThenBy(kv => kv.Key, StringComparer.Ordinal).ToList()) {
			if (need <= 0) break;
			int copies = Math.Min(need, stock.Remaining);
			stock.Remaining -= copies;
			need -= copies; took += copies;
		}
		return took;
	}

	// ── the deadline (OnMonthChanged) ────────────────────────────────────────────────────────────────

	/// <summary>Runs on the 1st, after the overhead and the backer. A sitting that met a bar has already moved the deadline;
	/// otherwise the lawyer takes the payment if it is there, or seizes what he can and extends three months.</summary>
	private void EnforceLien(GameDate date) {
		if (lien == null || IsGameOver) return;
		if (lien.Extended && lien.PendingDeadline.HasValue && date >= lien.Deadline) {
			CompoundUnpaidInterest(date);
			lien.Deadline = lien.PendingDeadline.Value;
			lien.BarYear = lien.Deadline.year - 1;
			lien.PendingDeadline = null;
			lien.Extended = false;
			return;
		}
		if (date < lien.Deadline) {
			if (date.month == 12 && date.day == 1 && !lien.Extended)
				Note($"{LienProfile.Lawyer} writes: the lien falls due on {lien.Deadline.ToHeadlineString()}. He'll sit down with you from {AddDays(lien.Deadline, -LienWindowDays).ToHeadlineString()}.");
			return;
		}
		var p = LienProfile;
		float due = LienPayBar();
		if (lien.IsReckoning) {
			if (Label.cashReserves >= due) { TakeLienPayment(date, due, LienLedgerKind.PaidClean); Note($"{p.Lawyer}'s man came on the 1st and took the last ${due:N0}. The paper is torn up."); EndDecade(paid: true); }
			else { lien.Add(date, LienLedgerKind.Taken, lien.Outstanding); Note(p.Seizure); EndDecade(paid: false); }
			return;
		}
		// A holder who won't take money (Floria from 1966) doesn't send a man for the till: a no-show is a missed deadline.
		if (Label.cashReserves >= due && !LienRefusesPay) {
			TakeLienPayment(date, due, LienLedgerKind.Paid);
			lien.Standing = Mathf.Max(0f, lien.Standing - 0.1f);
			lien.ConsecutiveFailures = 0;
			lien.UnitsAtLastSitting = PlayerUnitsSold();
			lien.Top40AtLastSitting = Label.top40Hits;
			lien.Deadline = new GameDate(lien.Deadline.year + 1, 1, 1);
			lien.BarYear = lien.Deadline.year - 1;
			lien.LastWasWrittenOn = false;
			lien.Add(date, LienLedgerKind.Extended, 0f, $"no-show, paid → {lien.Deadline.ToShortString()}");
			Note($"You never sat down with {p.Lawyer}, so his man came on the 1st and took ${due:N0} from the till. The lien runs to {lien.Deadline.ToHeadlineString()}.");
			return;
		}
		// Partial seizure: cash above zero, then pressed stock at the dealer price, up to what Pay asked.
		lien.AccrueTo(date);
		float cashTaken = TakeLienPayment(date, due, LienLedgerKind.Seized);
		float rest = R2(due - cashTaken);
		int copies = rest > 0f ? HandOverStock(rest, date) : 0;
		float stockValue = R2(Mathf.Min(rest, copies * DealerPrice));
		if (stockValue > 0f) {
			float toInterest = R2(Mathf.Min(stockValue, lien.InterestDue));
			lien.InterestPaid = R2(lien.InterestPaid + toInterest);
			lien.PrincipalPaid = R2(lien.PrincipalPaid + Mathf.Min(lien.Outstanding, stockValue - toInterest));
			lien.Add(date, LienLedgerKind.Seized, stockValue, $"{copies} copies");
		}
		lien.ConsecutiveFailures++;
		lien.Standing = Mathf.Max(0f, lien.Standing - 0.2f);
		Note($"{p.Seizure} They take ${cashTaken:N0} in cash" + (copies > 0 ? $" and {copies:N0} copies of pressed stock" : "") + ".");
		if (cashTaken + stockValue <= 0.005f) { EndRunTaken($"There was nothing left for {p.Name} to take, so he took the label."); return; }
		if (lien.ConsecutiveFailures >= LienFailuresToEnd) { EndRunTaken($"Two deadlines missed in a row. {p.Name} enforces the whole lien and takes {Label.labelName}."); return; }
		CompoundUnpaidInterest(date);
		int month = lien.Deadline.month + LienShortExtensionMonths;
		lien.Deadline = new GameDate(lien.Deadline.year + (month - 1) / 12, (month - 1) % 12 + 1, 1);
		lien.Add(date, LienLedgerKind.Extended, 0f, $"seizure → {lien.Deadline.ToShortString()}");
		Note($"{p.Lawyer} gives you until {lien.Deadline.ToHeadlineString()}. Miss that and it's over.");
	}

	/// <summary>Kubiak's clause: interest still unpaid when the lien rolls over goes onto the face sum, so next year's interest
	/// runs on it. Other holders carry it as interest due, owed at the reckoning.</summary>
	private void CompoundUnpaidInterest(GameDate date) {
		if (!lien.Profile.Compounds) return;
		lien.AccrueTo(date);
		float unpaid = lien.InterestDue;
		if (unpaid <= 0.005f) return;
		lien.InterestPaid = R2(lien.InterestPaid + unpaid);
		lien.FaceSum = R2(lien.FaceSum + unpaid);
		lien.Add(date, LienLedgerKind.WrittenOn, unpaid, "unpaid interest compounded");
		Note($"{LienProfile.Lawyer} writes: ${unpaid:N0} of unpaid interest has gone onto the paper. The face sum is now ${lien.FaceSum:N0}.");
	}

	private void EndRunTaken(string reason) {
		lien.Ending = "Taken";
		GameOverTitle = "TAKEN";
		GameOver(reason);
	}

	/// <summary>1 January 1970: the game ends either way (author decision).</summary>
	private void EndDecade(bool paid) {
		var p = LienProfile;
		lien.Ending = paid ? "PaidClean" : "Taken";
		GameOverTitle = paid ? "PAID CLEAN" : "TAKEN";
		string reason = paid
			? $"The decade's over and so is the paper. {Label.labelName} is yours, free and clear, and {p.Name} never sees you again."
			: $"The decade's over. {p.Name} takes the catalogue and the lease, and {Label.labelName} belongs to someone else.";
		GameOver(reason, bankrupt: !paid);
	}

	// ── save ────────────────────────────────────────────────────────────────────────────────────────

	private LienSaveData CaptureLien() {
		if (lien == null) return null;
		return new LienSaveData {
			Holder = (int)lien.Holder,
			Where = (int)lien.Memory.Where, Who = (int)lien.Memory.Who, Brag = (int)lien.Memory.Brag, End = (int)lien.Memory.End,
			WrongMemories = new List<string>(lien.WrongMemories),
			FaceSum = lien.FaceSum, Rate = lien.Rate, Venue = lien.Venue, VenueId = lien.VenueId, Signed = DateSave.From(lien.Signed), LastAccrual = DateSave.From(lien.LastAccrual),
			InterestAccrued = lien.InterestAccrued, InterestPaid = lien.InterestPaid, PrincipalPaid = lien.PrincipalPaid,
			Deadline = DateSave.From(lien.Deadline), PendingDeadline = lien.PendingDeadline.HasValue ? DateSave.From(lien.PendingDeadline.Value) : null,
			BarYear = lien.BarYear, Extended = lien.Extended, ConsecutiveFailures = lien.ConsecutiveFailures, Standing = lien.Standing,
			OpeningSeen = lien.OpeningSeen, LastWasWrittenOn = lien.LastWasWrittenOn, UnitsAtLastSitting = lien.UnitsAtLastSitting,
			Top40AtLastSitting = lien.Top40AtLastSitting, Ending = lien.Ending,
			Ledger = lien.Ledger.Select(e => new LienLedgerSaveData { Date = DateSave.From(e.Date), Kind = (int)e.Kind, Amount = e.Amount, Detail = e.Detail }).ToList(),
		};
	}

	private void RestoreLien(LienSaveData d) {
		lien = null; activeSitting = null; GameOverTitle = null;
		if (d == null || d.Deadline?.Valid != true) return;
		lien = new LienState {
			Holder = (LienHolder)d.Holder,
			Memory = new PartyMemory { Where = (PartyWhere)d.Where, Who = (PartyWho)d.Who, Brag = (PartyBrag)d.Brag, End = (PartyEnd)d.End },
			WrongMemories = d.WrongMemories ?? new(),
			FaceSum = d.FaceSum, Rate = d.Rate, Venue = d.Venue, VenueId = d.VenueId, Signed = d.Signed?.Valid == true ? d.Signed.ToDate() : GameDate.StartDate,
			LastAccrual = d.LastAccrual?.Valid == true ? d.LastAccrual.ToDate() : d.Deadline.ToDate(),
			InterestAccrued = d.InterestAccrued, InterestPaid = d.InterestPaid, PrincipalPaid = d.PrincipalPaid,
			Deadline = d.Deadline.ToDate(), PendingDeadline = d.PendingDeadline?.Valid == true ? d.PendingDeadline.ToDate() : null,
			BarYear = d.BarYear, Extended = d.Extended, ConsecutiveFailures = d.ConsecutiveFailures, Standing = d.Standing,
			OpeningSeen = d.OpeningSeen, LastWasWrittenOn = d.LastWasWrittenOn, UnitsAtLastSitting = d.UnitsAtLastSitting,
			Top40AtLastSitting = d.Top40AtLastSitting, Ending = d.Ending,
			Ledger = (d.Ledger ?? new()).Where(e => e?.Date?.Valid == true).Select(e => new LienLedgerEntry {
				Date = e.Date.ToDate(), Kind = (LienLedgerKind)e.Kind, Amount = e.Amount, Detail = e.Detail }).ToList(),
		};
	}

	// ── checks ──────────────────────────────────────────────────────────────────────────────────────

	/// <summary>Checks only: run the 1st-of-month enforcement for a date without waiting for the clock.</summary>
	internal void EnforceLienForChecks(GameDate date) => EnforceLien(date);
	/// <summary>Checks only: found a lien with given answers on an already-founded label.</summary>
	internal void FoundLienForChecks(PartyMemory memory, LienHolder? holder = null) => FoundLien(memory, holder);
	/// <summary>Checks only: the save form of the lien, and a restore from it.</summary>
	internal LienSaveData CaptureLienForChecks() => CaptureLien();
	internal void RestoreLienForChecks(LienSaveData d) => RestoreLien(d);
}

/// <summary>Inner-life Phase 1: the New Year's Eve lien. Absent in older saves (no lien).</summary>
public sealed class LienSaveData {
	public int Holder { get; set; }
	public int Where { get; set; }
	public int Who { get; set; }
	public int Brag { get; set; }
	public int End { get; set; }
	public List<string> WrongMemories { get; set; } = new();
	public float FaceSum { get; set; }
	public float Rate { get; set; }
	/// <summary>The hometown room named in the holder's lines. Null in older saves (the lines fall back to "the best room in town").</summary>
	public string Venue { get; set; }
	public string VenueId { get; set; }
	public DateSave Signed { get; set; }
	public DateSave LastAccrual { get; set; }
	public float InterestAccrued { get; set; }
	public float InterestPaid { get; set; }
	public float PrincipalPaid { get; set; }
	public DateSave Deadline { get; set; }
	public DateSave PendingDeadline { get; set; }
	public int BarYear { get; set; }
	public bool Extended { get; set; }
	public int ConsecutiveFailures { get; set; }
	public float Standing { get; set; }
	public bool OpeningSeen { get; set; }
	public bool LastWasWrittenOn { get; set; }
	public long UnitsAtLastSitting { get; set; }
	public int Top40AtLastSitting { get; set; }
	public string Ending { get; set; }
	public List<LienLedgerSaveData> Ledger { get; set; } = new();
}

public sealed class LienLedgerSaveData {
	public DateSave Date { get; set; }
	public int Kind { get; set; }
	public float Amount { get; set; }
	public string Detail { get; set; }
}
