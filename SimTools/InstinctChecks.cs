using System;
using System.Linq;
using Godot;
using static PlayerDesk;

/// <summary>
/// Inner-life §4.3, the December point: the run's instincts are its own copy, never the archetype's table; a December sitting
/// that rolls the lien earns one point; a point raises one instinct by one, up to 5, onto the change ledger; a 5 takes no
/// point; with every instinct at 5 no point is earned; the sitting after a seizure's three months earns none; and the points
/// and the ledger survive a save.
///   --path . SimTools/InstinctChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints INSTINCT_CHECKS_PASS / _FAIL.
/// </summary>
public partial class InstinctChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }

	private static readonly PartyMemory Nothing = new() { Where = PartyWhere.Nothing, Who = PartyWho.Nothing, Brag = PartyBrag.Nothing, End = PartyEnd.Nothing };

	public override void _Ready() {
		try {
			var desk = PlayerDesk.Instance;
			Check(desk.FoundLabel("Instinct Checks", "detroit", FoundingArchetype.PromoMan, null, Nothing, out var message), "Found: " + message);
			OwnCopy(desk);
			Earned(desk);
			Spent(desk);
			NoPointWhenFull(desk);
			NoPointAfterSeizure(desk);
			SaveRoundTrip(desk);
			GD.Print($"INSTINCT_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("INSTINCT_CHECKS_FAIL: " + e.Message);
			GetTree().Quit(1);
		}
	}

	private void OwnCopy(PlayerDesk desk) {
		var table = FoundingArchetypeData.Get(FoundingArchetype.PromoMan).Instincts;
		Check(!ReferenceEquals(desk.InstinctProfile, table), "The run's instincts are its own copy");
		int suit = table.TheSuit;
		desk.InstinctProfile.TheSuit = suit + 1;
		Check(table.TheSuit == suit, "Raising the run's Suit leaves the archetype's table alone");
		desk.InstinctProfile.TheSuit = suit;
		Check(desk.InstinctPoints == 0 && desk.InstinctLedger.Count == 0, "A new label starts with no points and no changes");
	}

	/// <summary>A December sitting in <paramref name="year"/>, rolled by Pay. Returns whether it rolled.</summary>
	private bool PaySitting(PlayerDesk desk, int year, GameDate? deadline = null) {
		var clock = TimeManager.Instance;
		desk.CloseLienSitting();
		desk.FoundLienForChecks(Nothing, LienHolder.Winters);
		var lien = desk.Lien;
		lien.Deadline = deadline ?? new GameDate(year + 1, 1, 1);
		lien.BarYear = year;
		clock.RestoreClock(lien.Deadline.AddDays(-10), 9);
		desk.Label.cashReserves = 50_000f;
		Check(desk.OpenLienSitting(out var m), $"{year}: the sitting opens: {m}");
		var pay = desk.LienSittingOptions(desk.ActiveSitting).First(o => o.verb == LienVerb.Pay);
		bool ok = desk.ChooseLienOption(desk.ActiveSitting, pay, out _) && lien.Extended;
		desk.CloseLienSitting();
		return ok;
	}

	private void Earned(PlayerDesk desk) {
		Check(PaySitting(desk, 1960) && desk.InstinctPoints == 1, "Rolling the 1960 lien earns one point");
		Check(PaySitting(desk, 1961) && desk.InstinctPoints == 2, "Rolling 1961 earns another; unspent points keep");
	}

	private void Spent(PlayerDesk desk) {
		var inst = desk.InstinctProfile;
		int suit = inst.TheSuit;
		Check(desk.SpendInstinctPoint(ExecutiveVoice.Suit, out var m) && inst.TheSuit == suit + 1 && desk.InstinctPoints == 1, "A point raises the Suit by one: " + m);
		var change = desk.InstinctLedger.Last();
		Check(change.Voice == ExecutiveVoice.Suit && change.From == suit && change.To == suit + 1 && change.Why == "December point", "It's on the change ledger");
		int ear = inst.TheEar;
		inst.TheEar = InstinctPointCap;
		Check(!desk.SpendInstinctPoint(ExecutiveVoice.Ear, out var capped) && capped.Contains("gold record") && desk.InstinctPoints == 1, "A 5 takes no point: " + capped);
		inst.TheEar = ear;
		Check(desk.SpendInstinctPoint(ExecutiveVoice.Street, out _) && desk.InstinctPoints == 0, "The last point goes on the Street");
		Check(!desk.SpendInstinctPoint(ExecutiveVoice.Fixer, out var none) && none.Contains("No point"), "With none left, nothing to spend");
		inst.TheSuit = suit; inst.TheStreet--;
	}

	private void NoPointWhenFull(PlayerDesk desk) {
		var inst = desk.InstinctProfile;
		var before = (inst.TheEar, inst.TheStreet, inst.TheSuit, inst.TheFixer);
		inst.TheEar = inst.TheStreet = inst.TheSuit = inst.TheFixer = InstinctPointCap;
		Check(PaySitting(desk, 1962) && desk.InstinctPoints == 0, "With every instinct at 5, a rolled year earns nothing to spend");
		(inst.TheEar, inst.TheStreet, inst.TheSuit, inst.TheFixer) = before;
	}

	private void NoPointAfterSeizure(PlayerDesk desk) {
		Check(PaySitting(desk, 1962, new GameDate(1963, 4, 1)) && desk.InstinctPoints == 0, "The sitting after a seizure's three months isn't a year survived: no point");
	}

	private void SaveRoundTrip(PlayerDesk desk) {
		desk.SetInstinctPointsForChecks(2);
		var (points, ledger) = desk.CaptureInstinctsForChecks();
		desk.RestoreInstinctsForChecks(0, null);
		Check(desk.InstinctPoints == 0 && desk.InstinctLedger.Count == 0, "An older save has no points and no changes");
		desk.RestoreInstinctsForChecks(points, ledger);
		Check(desk.InstinctPoints == 2 && desk.InstinctLedger.Count == ledger.Count && desk.InstinctLedger[0].Voice == ExecutiveVoice.Suit, "Points and the ledger survive a save");
	}
}
