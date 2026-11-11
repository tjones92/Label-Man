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
// either paid clean or taken.
//
// Who holds the paper is one of five authored variants (§3.5), narrowed by the founding "What do you remember?" answers
// (§3.6) and picked by a keyed Philox draw on the world seed, never the shared stream.
//
// Player-only. Nothing here exists without a player label, so a headless AI run never reaches it. Payments come from
// cash above zero only and never push the books red; the bank's own credit-floor game over is untouched.
// ============================================================================================
public partial class PlayerDesk {
	public enum LienHolder { Dace, Kubiak, Fennell, Ainsley, Lark }
	public enum LienBar { Pay, Show, Concede }
	public enum PartyWhere { SupperClub, DjHouseParty, HotelBallroom, BarBackRoom, Nothing }
	public enum PartyWho { CamelCoat, Bandleader, RouteMan, MajorMan, Nothing }
	public enum PartyBrag { TheLabel, TheAct, Money, Songs, Nothing }
	public enum PartyEnd { CardTable, Car, PhoneCall, Nothing }
	public enum LienLedgerKind { Signed, InterestAccrued, Paid, Shown, ConcededStock, WrittenOn, Seized, Extended, PaidClean, Taken }

	public const float LienFaceSum = 20000f;
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

	// ── the five holders (§3.5) ─────────────────────────────────────────────────────────────────────

	public sealed record LienHolderProfile(
		LienHolder Id, string Name, string Title, string Lawyer, LienBar Stresses,
		string Opening, string WantsLine, string PayAccepted, string ShowAccepted, string ConcedeAccepted,
		string Pushback, ExecutiveVoice HonestVoice, string HonestLine, ExecutiveVoice BluffVoice, string BluffLine,
		string BluffCaught, string Seizure, string FixerRead);

	public static readonly IReadOnlyDictionary<LienHolder, LienHolderProfile> LienHolders = new Dictionary<LienHolder, LienHolderProfile> {
		[LienHolder.Kubiak] = new(LienHolder.Kubiak, "Walt Kubiak", "who runs the jukebox route across the region", "Mr. Pruitt", LienBar.Pay,
			Opening: "Pruitt sets his hat on your desk and opens a ledger that isn't yours. \"Mr. Kubiak sends his regards. He'd like to talk about clause fourteen.\"",
			WantsLine: "\"He's a simple man. Money on the table, and you've got another year.\"",
			PayAccepted: "Pruitt counts it twice, writes a receipt in a hand like a bank's, and stamps the new date on the paper. \"Mr. Kubiak appreciates a man who pays.\"",
			ShowAccepted: "Pruitt reads the figures, then reads them again. \"Fine. He'll take it as a down payment on next year's money.\"",
			ConcedeAccepted: "\"It's not cash,\" Pruitt says, \"but he can count it.\" He writes the new date in.",
			Pushback: "\"Copies sold don't pay Mr. Kubiak's mechanics. He wants money.\"",
			HonestVoice: ExecutiveVoice.Suit, HonestLine: "\"The houses owe me ${0:N0}. He'll see it before Easter.\"",
			BluffVoice: ExecutiveVoice.Fixer, BluffLine: "\"Tell Walt I've got a check coming from a distributor. Big one.\"",
			BluffCaught: "Pruitt smiles without any warmth. \"Mr. Kubiak knows every distributor in this region. None of them is writing you a check.\"",
			Seizure: "Kubiak's men come on the 1st with a truck and a list.",
			FixerRead: "He's here for cash. Give him cash and it's a short meeting."),
		[LienHolder.Dace] = new(LienHolder.Dace, "Harlan Dace", "who owns the Starlite Room", "Mr. Halloran", LienBar.Concede,
			Opening: "Halloran takes the good chair without being asked. \"Mr. Dace remembers your evening at the Starlite fondly. He hopes you do.\"",
			WantsLine: "\"He'd rather have a favour than your money. Ask me what favour.\"",
			PayAccepted: "\"Money's fine,\" Halloran says, in a voice that means it isn't. He stamps the date in.",
			ShowAccepted: "\"He'll be pleased you're doing well. He'll be more pleased when you do well for him.\"",
			ConcedeAccepted: "Halloran nods slowly. \"That's the spirit Mr. Dace was hoping for.\" The new date goes on the paper.",
			Pushback: "\"Mr. Dace doesn't read sales sheets. He wants something for the Starlite.\"",
			HonestVoice: ExecutiveVoice.Ear, HonestLine: "\"I've put a record on the chart. Tell him his singer wants a label that can do that.\"",
			BluffVoice: ExecutiveVoice.Street, BluffLine: "\"Everyone in town's asking about my acts. Dace wants in on that, he waits a year.\"",
			BluffCaught: "\"Mr. Dace owns the room everyone in town drinks in,\" Halloran says. \"Nobody's asking about your acts.\"",
			Seizure: "Halloran comes on the 1st with two men from the Starlite's door.",
			FixerRead: "He wants a piece of what you make, not the money. Hand him something and he'll smile."),
		[LienHolder.Fennell] = new(LienHolder.Fennell, "Mort Fennell", "who plugs songs for a big publisher and wants a catalogue of his own", "Mr. Weisz", LienBar.Show,
			Opening: "Weisz has brought a trade paper with your label's name circled in pencil, or not circled, which is worse. \"Mr. Fennell is a believer. Give him something to believe in.\"",
			WantsLine: "\"Hits are copyrights. Show him the label's making them.\"",
			PayAccepted: "\"He'll take it,\" Weisz says, \"but he's not in this for the interest.\"",
			ShowAccepted: "Weisz folds the paper with something like pleasure. \"He'll be glad. Another year.\"",
			ConcedeAccepted: "\"Not what he hoped for, but it's paper he can use.\" The new date goes on.",
			Pushback: "\"That's money. Mr. Fennell wants to see the label moving.\"",
			HonestVoice: ExecutiveVoice.Suit, HonestLine: "\"Here's the interest in cash, and the records will follow it.\"",
			BluffVoice: ExecutiveVoice.Ear, BluffLine: "\"I've got a song in the can that's a hit. Tell him to wait for it.\"",
			BluffCaught: "\"Mr. Fennell has heard that sentence from better men than you,\" Weisz says.",
			Seizure: "Weisz sends a process server on the 1st, and a man to count the stock.",
			FixerRead: "He wants to see the label growing. Numbers will do it."),
		[LienHolder.Ainsley] = new(LienHolder.Ainsley, "Mr. Ainsley", "a quiet man in a camel coat whom nobody in town can place", "Mr. Lowe", LienBar.Show,
			Opening: "Lowe's suit cost more than your pressing. \"My client is a patient investor. He wants to see the business grow.\"",
			WantsLine: "\"Growth, Mr. — growth is all he asks.\"",
			PayAccepted: "\"Very good.\" Lowe pockets the check as if it weighed nothing. The new date goes on.",
			ShowAccepted: "Lowe reads the sales figures with real interest. \"He'll be pleased. Keep it up.\"",
			ConcedeAccepted: "\"A sensible arrangement,\" Lowe says, and writes it down in a small, neat hand.",
			Pushback: "\"Money's very nice. My client would rather see the label grow.\"",
			HonestVoice: ExecutiveVoice.Suit, HonestLine: "\"Here's the interest in cash. The growth is coming.\"",
			BluffVoice: ExecutiveVoice.Fixer, BluffLine: "\"I'm about to sign an act everybody wants. Ask around.\"",
			BluffCaught: "\"We did ask around,\" Lowe says pleasantly.",
			Seizure: "Lowe's people come on the 1st, very politely, with an inventory form.",
			FixerRead: "You can't read him. That's the read."),
		[LienHolder.Lark] = new(LienHolder.Lark, "Teddy Lark", "who led Teddy Lark and His Orchestra when they still had hits", "Mr. Becker", LienBar.Concede,
			Opening: "Becker is young and apologetic. \"Mr. Lark asked me to say he's sorry about all this. He'd still like you to keep your word.\"",
			WantsLine: "\"He wants a session. One session for the band.\"",
			PayAccepted: "\"He'll be embarrassed by the money,\" Becker says, \"but the pension fund won't be.\"",
			ShowAccepted: "\"He'll be glad you're doing well. He'd be gladder if you did it with the band.\"",
			ConcedeAccepted: "Becker looks relieved. \"That'll mean a lot to him.\" He writes the date in.",
			Pushback: "\"Mr. Lark doesn't care about sales figures. He wants you to do right by the band.\"",
			HonestVoice: ExecutiveVoice.Ear, HonestLine: "\"I can make hits now. That's what the band needs from me, and that takes another year.\"",
			BluffVoice: ExecutiveVoice.Street, BluffLine: "\"Tell Teddy the big bands are coming back. I'm getting ready for it.\"",
			BluffCaught: "Becker looks at the floor. \"Mr. Lark reads the trades too.\"",
			Seizure: "Becker comes on the 1st, miserable, with a list his own lawyer made him bring.",
			FixerRead: "He isn't a crook. He's scared. That's leverage, if you want it."),
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
		(PartyWhere.SupperClub, "A supper club, all white tablecloths"), (PartyWhere.DjHouseParty, "A DJ's house party"),
		(PartyWhere.HotelBallroom, "A hotel ballroom"), (PartyWhere.BarBackRoom, "The back room of a bar"), (PartyWhere.Nothing, "No idea"),
	};
	public static readonly (PartyWho Value, string Text)[] PartyWhoAnswers = {
		(PartyWho.CamelCoat, "A man in a camel coat"), (PartyWho.Bandleader, "A bandleader"),
		(PartyWho.RouteMan, "A jukebox route man"), (PartyWho.MajorMan, "Somebody from a Major"), (PartyWho.Nothing, "Everybody, nobody"),
	};
	public static readonly (PartyBrag Value, string Text)[] PartyBragAnswers = {
		(PartyBrag.TheLabel, "The label you're about to start"), (PartyBrag.TheAct, "The act you're going to sign"),
		(PartyBrag.Money, "Money you don't have"), (PartyBrag.Songs, "The songs you're going to find"), (PartyBrag.Nothing, "You don't brag"),
	};
	public static readonly (PartyEnd Value, string Text)[] PartyEndAnswers = {
		(PartyEnd.CardTable, "At a card table"), (PartyEnd.Car, "In a car"), (PartyEnd.PhoneCall, "With a phone call"), (PartyEnd.Nothing, "It didn't. It just stopped."),
	};

	/// <summary>Which holders an answer fits (§3.6). Each holder fits exactly five answers; "nothing" fits nobody.</summary>
	private static IEnumerable<LienHolder> Fits(PartyMemory m) {
		switch (m.Where) {
			case PartyWhere.SupperClub: yield return LienHolder.Dace; yield return LienHolder.Lark; break;
			case PartyWhere.DjHouseParty: yield return LienHolder.Fennell; break;
			case PartyWhere.HotelBallroom: yield return LienHolder.Ainsley; yield return LienHolder.Lark; break;
			case PartyWhere.BarBackRoom: yield return LienHolder.Dace; yield return LienHolder.Kubiak; break;
		}
		switch (m.Who) {
			case PartyWho.CamelCoat: yield return LienHolder.Ainsley; break;
			case PartyWho.Bandleader: yield return LienHolder.Fennell; yield return LienHolder.Lark; break;
			case PartyWho.RouteMan: yield return LienHolder.Dace; yield return LienHolder.Kubiak; break;
			case PartyWho.MajorMan: yield return LienHolder.Fennell; yield return LienHolder.Ainsley; break;
		}
		switch (m.Brag) {
			case PartyBrag.TheLabel: yield return LienHolder.Kubiak; yield return LienHolder.Ainsley; break;
			case PartyBrag.TheAct: yield return LienHolder.Lark; break;
			case PartyBrag.Money: yield return LienHolder.Dace; yield return LienHolder.Kubiak; break;
			case PartyBrag.Songs: yield return LienHolder.Fennell; break;
		}
		switch (m.End) {
			case PartyEnd.CardTable: yield return LienHolder.Dace; break;
			case PartyEnd.Car: yield return LienHolder.Kubiak; yield return LienHolder.Lark; break;
			case PartyEnd.PhoneCall: yield return LienHolder.Fennell; yield return LienHolder.Ainsley; break;
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
	public bool LienMorningPending => lien != null && !lien.OpeningSeen && !IsGameOver;
	/// <summary>The title the game-over page shows: the doors closing, or the decade's reckoning.</summary>
	public string GameOverTitle { get; private set; }

	private static GameDate LienToday => TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;

	private void FoundLien(PartyMemory memory) {
		memory ??= new PartyMemory();
		var today = LienToday;
		var holder = PickHolder(memory, BandLife.WorldSeed);
		lien = new LienState {
			Holder = holder, Memory = memory.Copy(), WrongMemories = WrongMemories(memory, holder),
			Signed = today, LastAccrual = today, Deadline = new GameDate(today.year + 1, 1, 1), BarYear = today.year,
		};
		lien.Add(today, LienLedgerKind.Signed, LienFaceSum, holder.ToString());
	}

	public void DismissLienMorning() {
		if (lien == null) return;
		lien.OpeningSeen = true;
		Changed?.Invoke();
	}

	// ── what the archetype lost (§3.1) ───────────────────────────────────────────────────────────────

	public string LostBelongings() => Archetype switch {
		FoundingArchetype.PawnShopOwner => "the car, the savings book, and the whole back room of stock from the shop",
		FoundingArchetype.ExMusician => "the car, the savings book, and your guitar",
		FoundingArchetype.PromoMan => "the car, the savings book, and your address book with every number you ever got",
		_ => "the car, the savings book, and the pension you had put by from the trade",
	};

	/// <summary>The morning after, line by line. The voices argue about the night (theories, not facts: the night is a
	/// mystery). Shown once, on the first page after founding.</summary>
	public List<CallLine> LienMorningLines() {
		var lines = new List<CallLine>();
		if (lien == null || Label == null) return lines;
		void Say(string text, ExecutiveVoice voice = ExecutiveVoice.None, string speaker = "") =>
			lines.Add(new CallLine { beat = RolodexSceneBeat.Opening, text = text, voice = voice, speaker = speaker });
		Say("1 January 1960, about noon. Your head is splitting. You're wearing yesterday's shirt and one shoe.");
		Say("There's a lawyer's letter on the mat. Some time after midnight you signed a lien on everything you own " +
			$"and everything {Label.labelName} will ever own. It secures ${lien.FaceSum:N0}, and it falls due on {lien.Deadline.ToHeadlineString()}.");
		Say($"The founding money is still in the cigar box: ${Label.cashReserves:N0}. Everything else is gone: {LostBelongings()}. You don't know where.");
		Say($"The letter is signed by {lien.Profile.Lawyer}, \"for the holder\". It doesn't say who the holder is. " +
			"It says you may apply for an extension at a sitting in the last two weeks of December.");
		Say("A card game. It's always a card game.", ExecutiveVoice.Fixer);
		Say("You'd never sign a thing like that. Not sober. Not with a lawyer in the room.", ExecutiveVoice.Suit);
		Say("There was a band. A good one. You were up there with them, you remember the horns.", ExecutiveVoice.Ear);
		Say("There was a woman. There's always a woman.", ExecutiveVoice.Street);
		Say("So the label you meant to start anyway starts today, with a fuse on it.");
		return lines;
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
		if (lien.Extended) return $"The lien runs to {(lien.PendingDeadline ?? lien.Deadline).ToHeadlineString()}. {lien.Profile.Lawyer} sits down with you again in the last two weeks before it.";
		var today = LienToday;
		var opens = AddDays(lien.Deadline, -LienWindowDays);
		if (today < opens) return $"{lien.Profile.Lawyer} sits down with you from {opens.ToHeadlineString()}.";
		if (!AtHome) return $"{lien.Profile.Lawyer} comes to the office; you have to be home.";
		var clock = TimeManager.Instance;
		if (clock == null) return "No clock.";
		if (clock.CurrentHour < BackerOfficeFromHour || clock.CurrentHour + LienSittingHours > BackerOfficeUntilHour)
			return $"{lien.Profile.Lawyer} keeps office hours: {BackerOfficeFromHour} a.m. to {BackerOfficeUntilHour - 12} p.m.";
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
		var p = lien.Profile;
		var live = new LienSittingLive();
		live.Say(p.Opening);
		live.Say(lien.IsReckoning ? $"\"It's the end of the decade, and the end of the paper. Mr. {p.Name.Split(' ')[^1]} wants ${LienPayBar():N0}, all of it.\"" : p.WantsLine, p.Lawyer);
		// Passive reads: each voice speaks only past its threshold, and only about a fact that is true.
		var inst = InstinctProfile;
		float pay = LienPayBar();
		if (inst.TheSuit >= 3) live.Say($"He'll want ${pay:N0} to roll it. You have ${Mathf.Max(0f, Label.cashReserves):N0}.", "", ExecutiveVoice.Suit);
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
		list.Add(new LienOption { verb = LienVerb.Pay, label = $"Pay him (${pay:N0})", cash = pay, subLabel = "Interest and this year's slice of principal.",
			enabled = cash >= pay, disabledReason = $"You have ${cash:N0}." });
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
		var p = lien.Profile;
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
		var p = lien.Profile;
		var today = LienToday;
		live.Say(opt.label, "You", opt.voice, isPlayer: true);
		switch (opt.verb) {
			case LienVerb.Pay:
				TakeLienPayment(today, LienPayBar(), LienLedgerKind.Paid);
				Extend(live, LienBar.Pay, p.PayAccepted);
				break;
			case LienVerb.Show:
				if (p.Stresses == LienBar.Show) { PayInterestIfAny(today, onlyIfCash: true); Extend(live, LienBar.Show, p.ShowAccepted); break; }
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
		var p = lien.Profile;
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
			lien.Deadline = lien.PendingDeadline.Value;
			lien.BarYear = lien.Deadline.year - 1;
			lien.PendingDeadline = null;
			lien.Extended = false;
			return;
		}
		if (date < lien.Deadline) {
			if (date.month == 12 && date.day == 1 && !lien.Extended)
				Note($"{lien.Profile.Lawyer} writes: the lien falls due on {lien.Deadline.ToHeadlineString()}. He'll sit down with you from {AddDays(lien.Deadline, -LienWindowDays).ToHeadlineString()}.");
			return;
		}
		var p = lien.Profile;
		float due = LienPayBar();
		if (lien.IsReckoning) {
			if (Label.cashReserves >= due) { TakeLienPayment(date, due, LienLedgerKind.PaidClean); Note($"{p.Lawyer}'s man came on the 1st and took the last ${due:N0}. The paper is torn up."); EndDecade(paid: true); }
			else { lien.Add(date, LienLedgerKind.Taken, lien.Outstanding); Note(p.Seizure); EndDecade(paid: false); }
			return;
		}
		if (Label.cashReserves >= due) {
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
		int month = lien.Deadline.month + LienShortExtensionMonths;
		lien.Deadline = new GameDate(lien.Deadline.year + (month - 1) / 12, (month - 1) % 12 + 1, 1);
		lien.Add(date, LienLedgerKind.Extended, 0f, $"seizure → {lien.Deadline.ToShortString()}");
		Note($"{p.Lawyer} gives you until {lien.Deadline.ToHeadlineString()}. Miss that and it's over.");
	}

	private void EndRunTaken(string reason) {
		lien.Ending = "Taken";
		GameOverTitle = "TAKEN";
		GameOver(reason);
	}

	/// <summary>1 January 1970: the game ends either way (author decision).</summary>
	private void EndDecade(bool paid) {
		var p = lien.Profile;
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
			FaceSum = lien.FaceSum, Rate = lien.Rate, Signed = DateSave.From(lien.Signed), LastAccrual = DateSave.From(lien.LastAccrual),
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
			FaceSum = d.FaceSum, Rate = d.Rate, Signed = d.Signed?.Valid == true ? d.Signed.ToDate() : GameDate.StartDate,
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
	internal void FoundLienForChecks(PartyMemory memory) => FoundLien(memory);
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
