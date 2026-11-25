using System;
using System.Linq;
using Godot;
using static PlayerDesk;

/// <summary>
/// Inner-life Phase 1b (§3.5 H1): Dee-Dee Sawyer's girlfriend. With Sawyer holding the paper, Connie Boursaw is on the roster from
/// the first day on his terms (five years, his option, the top royalty, a side a year, no advance); she is drawn from a private
/// keyed stream so the world's streams don't move; her paper never lapses (Dee-Dee exercises the option) until she reaches Star;
/// a year her career hasn't moved the way he asked makes Pay cost 1.5x; the first time she charts he sends the car back; and no
/// other holder has her, even when the lien is refounded over a desk that did.
///   --path . SimTools/ConnieChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints CONNIE_CHECKS_PASS / _FAIL.
/// </summary>
public partial class ConnieChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static bool Near(float a, float b, float tol = .02f) => Math.Abs(a - b) <= tol;

	private static readonly PartyMemory SawyerNight = new() { Where = PartyWhere.SupperClub, Brag = PartyBrag.TheAct, End = PartyEnd.CardTable };

	public override void _Ready() {
		try {
			var desk = PlayerDesk.Instance;
			var clock = TimeManager.Instance;
			clock.RestoreClock(new GameDate(1960, 1, 1), 9);
			Check(desk.FoundLabel("Connie Checks", "detroit", FoundingArchetype.ExMusician, null, SawyerNight, out var message), "Found: " + message);
			Seeded(desk);
			Streams(desk, clock);
			Option(desk, clock);
			Star(desk, clock);
			Car(desk, clock);
			Pay(desk, clock);
			Other(desk, clock);
			Save(desk, clock);
			GD.Print($"CONNIE_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("CONNIE_CHECKS_FAIL: " + e.Message);
			GetTree().Quit(1);
		}
	}

	private void Refound(PlayerDesk desk, TimeManager clock, int year = 1960) {
		clock.RestoreClock(new GameDate(1960, 1, 1), 9);
		desk.FoundLienForChecks(SawyerNight, LienHolder.Sawyer);
		desk.Label.cashReserves = 5000f;
		clock.RestoreClock(new GameDate(year, 3, 1), 9);
	}

	private void Seeded(PlayerDesk desk) {
		var c = desk.Connie;
		Check(c != null && c.stageName == ConnieStageName && c.type == ArtistType.SoloFemale, "Connie Boursaw is a solo female act: " + c?.stageName);
		Check(desk.Label.roster.Count == 1 && desk.Label.roster[0] == c && c.labelId == desk.Label.labelId && c.isPlayerOwned, "She is the only act on the roster, and it's the player's");
		Check(Near(c.royaltyRate, ConnieRoyalty, .0001f) && c.contractLength == ConnieTermYears && c.contractSinglesObligation == ConnieSidesPerTerm
			&& c.unrecoupedAdvance == 0f && c.contractExpiresYear == 1960 + ConnieTermYears, $"Dee-Dee's paper: {c.royaltyRate:P0}, {c.contractLength} years, {c.contractSinglesObligation} sides, no advance, to {c.contractExpiresYear}");
		Check(desk.IsHeldByHolder(c) && desk.HolderPaperLine(c) != null, "His option holds her paper: " + desk.HolderPaperLine(c));
		Check(c.members.Count >= 1 && c.members.Any(m => m.FullName == ConnieStageName && !m.isMale && m.isLeadVocalist), "Her lead vocalist is Connie Boursaw");
		Check(c.vocalPower < .5f && c.CalculateBaseQuality() < .5f, $"She is underwhelming: vocal {c.vocalPower:F2}, quality {c.CalculateBaseQuality():F2}");
		Check(c.artistId.StartsWith("artist_9", StringComparison.Ordinal), "Her id comes from its own range: " + c.artistId);
		Check(desk.ConnieAsk() is (false, "A record by Connie, out."), "In 1960 he asks for a record out");
	}

	private void Streams(PlayerDesk desk, TimeManager clock) {
		clock.RestoreClock(new GameDate(1960, 1, 1), 9);
		ulong global = GameRandom.StateForChecks;
		desk.FoundLienForChecks(SawyerNight, LienHolder.Sawyer);
		Check(GameRandom.StateForChecks == global, "Seeding her drew nothing from the global stream");
		var first = desk.Connie;
		float ego = first.members[0].ego; int born = first.members[0].birthYear;
		desk.FoundLienForChecks(SawyerNight, LienHolder.Sawyer);
		var second = desk.Connie;
		Check(second != first && desk.Label.roster.Count == 1 && desk.Label.roster[0] == second, "Refounding replaces her; it doesn't pile them up");
		Check(Near(second.members[0].ego, ego, .0001f) && second.members[0].birthYear == born, "The keyed stream draws the same Connie every time");
	}

	private void Option(PlayerDesk desk, TimeManager clock) {
		Refound(desk, clock, 1965);
		var c = desk.Connie;
		c.contractExpiresYear = 1965; c.contractExpiresWeek = 0;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		Check(RosterManager.IsContractMatured(c, 1965, week), "By 1965 her term has run out");
		desk.ProcessHolderOptionForChecks(new GameDate(1965, 3, 1));
		Check(!RosterManager.IsContractMatured(c, 1965, week) && c.contractExpiresYear == 1965 + ConnieTermYears, $"Dee-Dee exercised his option: she's signed to {c.contractExpiresYear}");
		Check(desk.IsHeldByHolder(c), "Still his paper");
		c.contractReleases = c.contractSinglesObligation;
		Check(RosterManager.IsContractMatured(c, 1965, week), "Five sides delivered also matures it");
		desk.ProcessHolderOptionForChecks(new GameDate(1965, 3, 2));
		Check(!RosterManager.IsContractMatured(c, 1965, week) && c.contractReleases == 0, "...and the option runs it again, the sides starting over");
		desk.ProcessRenewalPoachingForCheck();
		Check(desk.PendingRivalOffer(c.artistId) == null, "No rival ever finds her paper run out");
	}

	private void Star(PlayerDesk desk, TimeManager clock) {
		Refound(desk, clock, 1967);
		var c = desk.Connie;
		c.careerState = CareerState.Star;
		desk.ProcessHolderOptionForChecks(new GameDate(1967, 3, 1));
		Check(desk.Lien.ConnieFree && !desk.IsHeldByHolder(c) && desk.HolderPaperLine(c) == null, "At Star she has outgrown him: her paper is an ordinary act's");
		c.careerState = CareerState.Established;
		c.contractExpiresYear = 1967; c.contractExpiresWeek = 0;
		desk.ProcessHolderOptionForChecks(new GameDate(1967, 3, 2));
		Check(c.contractExpiresYear == 1967 && desk.Lien.ConnieFree, "He doesn't exercise anything once she's free, and slipping back from Star doesn't bind her again");
	}

	private void Car(PlayerDesk desk, TimeManager clock) {
		Refound(desk, clock);
		var c = desk.Connie;
		Check(!desk.CarRecovered && !desk.HasCar, "Dee-Dee has the car until she charts");
		desk.ProcessHolderOptionForChecks(new GameDate(1961, 3, 1));
		Check(!desk.CarRecovered, "An uncharted Connie gets nothing");
		c.regionalBreakouts = 1;
		desk.ProcessHolderOptionForChecks(new GameDate(1961, 3, 2));
		Check(desk.CarRecovered && desk.CarOwned == CarKind.Original && desk.CarWhereabouts() == null, "The first time she charts he sends the car back");
		Refound(desk, clock);
		desk.SetCarForChecks(CarKind.New);
		desk.Connie.charted = 1;
		desk.ProcessHolderOptionForChecks(new GameDate(1961, 3, 1));
		Check(desk.CarRecovered && desk.CarOwned == CarKind.New, "A man who bought a new car keeps it");
	}

	private void Pay(PlayerDesk desk, TimeManager clock) {
		Refound(desk, clock);
		var c = desk.Connie;
		desk.Lien.BarYear = 1962;
		float stalled = desk.LienPayBar();
		Check(desk.ConnieStalled && !desk.ConnieAsk().Met, "In 1962 an unmoved Connie is stalled");
		c.charted = 1;
		float moved = desk.LienPayBar();
		Check(!desk.ConnieStalled && desk.ConnieAsk().Met, "Once she's on a chart she isn't");
		Check(moved > 0f && Near(stalled, moved * ConnieStalledPayMultiple), $"Pay costs {ConnieStalledPayMultiple}x when she hasn't moved: ${stalled:N2} against ${moved:N2}");
		desk.Lien.BarYear = 1966;
		c.charted = 0; c.top40Hits = 0; c.careerState = CareerState.NewSigning;
		Check(!desk.ConnieAsk().Met && desk.ConnieAsk().What.Contains("Top 40"), "1965-66: he wants the Top 40, or Rising");
		c.careerState = CareerState.Rising;
		Check(desk.ConnieAsk().Met, "A Rising Connie meets it");
		desk.Lien.BarYear = 1968;
		Check(!desk.ConnieAsk().Met && desk.ConnieAsk().What.Contains("Star"), "1967 on: only Star will do");
		desk.Lien.Deadline = new GameDate(1970, 1, 1);
		Check(!desk.ConnieStalled, "At the reckoning only money counts, and it doesn't cost extra");
	}

	private void Other(PlayerDesk desk, TimeManager clock) {
		Refound(desk, clock);
		Check(desk.Connie != null, "Sawyer has her");
		foreach (var h in Enum.GetValues<LienHolder>().Where(h => h != LienHolder.Sawyer)) {
			clock.RestoreClock(new GameDate(1960, 1, 1), 9);
			desk.FoundLienForChecks(SawyerNight, h);
			Check(desk.Connie == null && desk.Label.roster.All(a => a.stageName != ConnieStageName) && !desk.ConnieStalled, $"{h}: no Connie, and nothing stalls Pay");
			Check(desk.ConnieAsk() is (true, ""), $"{h}: no ask about her");
		}
	}

	private void Save(PlayerDesk desk, TimeManager clock) {
		Refound(desk, clock);
		desk.Lien.ConnieFree = true;
		string id = desk.Lien.ConnieId;
		var data = desk.CaptureLienForChecks();
		desk.RestoreLienForChecks(data);
		Check(desk.Lien.ConnieId == id && desk.Lien.ConnieFree && desk.Connie?.stageName == ConnieStageName, "Who she is and whether she's free survive a save");
		data.ConnieId = null; data.ConnieFree = false;
		desk.RestoreLienForChecks(data);
		Check(desk.Connie == null && !desk.ConnieStalled, "An older save without her simply has none");
	}
}
