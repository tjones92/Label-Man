using System;
using System.Linq;
using Godot;
using static PlayerDesk;

/// <summary>
/// Inner-life §3.5 H3: Clyde Winters has your address book. At founding he has worked the home region's most influential
/// reporter jocks as you: every one is cold (you owe him one), the top third burned (he doesn't take your word). A card for one
/// of them says so when it goes into the book, and the call reads it; a jock Clyde never called is untouched. Turning down
/// Clyde's first move burns one more. The book comes back on a Show from 1961, and only Clyde's marks thaw. Other holders
/// mark nothing, and the marks survive a save.
///   --path . SimTools/WintersChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints WINTERS_CHECKS_PASS / _FAIL.
/// </summary>
public partial class WintersChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }

	private static readonly PartyMemory Clyde = new() { Where = PartyWhere.DjHouseParty, Who = PartyWho.MajorMan, Brag = PartyBrag.Hits, End = PartyEnd.PhoneCall };

	public override void _Ready() {
		try {
			var desk = PlayerDesk.Instance;
			Check(desk.FoundLabel("Winters Checks", "detroit", FoundingArchetype.PromoMan, null, Clyde, out var message), "Found: " + message);
			Seeded(desk);
			OtherHolder(desk);
			Refused(desk);
			BookBack(desk);
			SaveRoundTrip(desk);
			GD.Print($"WINTERS_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("WINTERS_CHECKS_FAIL: " + e.Message);
			GetTree().Quit(1);
		}
	}

	private static void Fresh(PlayerDesk desk, LienHolder h) {
		desk.ClearPressingForChecks();
		desk.ClearRolodexForChecks();
		desk.FoundLienForChecks(Clyde, h);
	}

	private void Seeded(PlayerDesk desk) {
		Fresh(desk, LienHolder.Winters);
		var lien = desk.Lien;
		var all = desk.HomeReportersForChecks();
		int worked = Math.Min(WintersMaxStations, (all.Count + 1) / 2);
		int burned = Math.Max(1, (worked + 2) / 3);
		Check(all.Count >= 2, $"The home region has reporters to work ({all.Count})");
		Check(lien.WintersBurned.Count == burned && lien.WintersCold.Count == worked - burned,
			$"He worked {worked} of {all.Count}: {burned} burned, {worked - burned} cold");
		Check(lien.WintersBurned.SequenceEqual(all.Take(burned).Select(s => s.stationId)), "The burned ones are the biggest jocks");

		var hot = all.First(s => lien.WintersBurned.Contains(s.stationId));
		var cold = all.First(s => lien.WintersCold.Contains(s.stationId));
		var clean = all.Last();
		Check(!lien.WintersBurned.Contains(clean.stationId) && !lien.WintersCold.Contains(clean.stationId), "The smallest jock never heard from him");

		var hotCard = desk.AddRolodexCardForChecks(hot);
		var coldCard = desk.AddRolodexCardForChecks(cold);
		var cleanCard = desk.AddRolodexCardForChecks(clean);
		Check(hotCard.log.Any(l => l.Contains("heard from you already")) && coldCard.log.Any(l => l.Contains("heard from you already")),
			"A card he worked says so when it goes into the book");
		Check(!cleanCard.log.Any(l => l.Contains("heard from you already")), "A card he didn't work says nothing");
		Check(!hotCard.professionallyBurned && !hotCard.youOweThem, "The card's own flags stay clean: the marks live on the lien");

		var ctx = desk.BuildCallContext(hotCard, null);
		Check(ctx.youOweThem && ctx.professionallyBurned, "A burned card's call: you owe him, and he doesn't take your word");
		ctx = desk.BuildCallContext(coldCard, null);
		Check(ctx.youOweThem && !ctx.professionallyBurned, "A cold card's call: you owe him one");
		ctx = desk.BuildCallContext(cleanCard, null);
		Check(!ctx.youOweThem && !ctx.professionallyBurned, "A clean card's call is clean");

		Check(desk.WintersCardLine(hotCard)?.StartsWith("Somebody") == true, "Before Clyde is named, the card says somebody");
		lien.HolderIdentified = true;
		Check(desk.WintersCardLine(hotCard)?.Contains("Clyde Winters") == true, "After, it names him");
		Check(desk.WintersCardLine(cleanCard) == null, "A clean card has no line");
	}

	private void OtherHolder(PlayerDesk desk) {
		Fresh(desk, LienHolder.Kubiak);
		Check(desk.Lien.WintersBurned.Count == 0 && desk.Lien.WintersCold.Count == 0, "Kubiak has no address book to work");
		var card = desk.AddRolodexCardForChecks(desk.HomeReportersForChecks().First());
		Check(!desk.WintersWorked(card) && !desk.BuildCallContext(card, null).professionallyBurned, "Under Kubiak the biggest jock is clean");
	}

	private void Refused(PlayerDesk desk) {
		Fresh(desk, LienHolder.Winters);
		var lien = desk.Lien;
		var today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int burned = lien.WintersBurned.Count, cold = lien.WintersCold.Count;
		string next = lien.WintersCold.FirstOrDefault();
		Check(next != null, "There's a cold card left to burn");
		desk.PressFirstSingleForChecks("winters-refuse", 1000, 1300f, today, today.AddDays(20));
		desk.ProcessHolderMoveDawnForChecks(lien.MoveDue);
		Check(desk.ResolveHolderMove(false, out var m), "Clyde turned down: " + m);
		Check(lien.WintersBurned.Count == burned + 1 && lien.WintersCold.Count == cold - 1 && lien.WintersBurned.Contains(next),
			"Turning him down burns the biggest cold card");
		Check(m.Contains("stiffed his publisher"), "The answer says so");
	}

	private void BookBack(PlayerDesk desk) {
		Fresh(desk, LienHolder.Winters);
		var lien = desk.Lien;
		var card = desk.AddRolodexCardForChecks(ChartManager.Instance.GetRadioStation(lien.WintersBurned[0]));
		card.professionallyBurned = true;   // a burn he earned himself, on top of Clyde's
		var coldCard = desk.AddRolodexCardForChecks(ChartManager.Instance.GetRadioStation(lien.WintersCold.FirstOrDefault() ?? lien.WintersBurned[0]));
		lien.BarYear = 1960;
		desk.ReturnWintersBookForChecks(LienBar.Show);
		Check(!lien.AddressBookBack, "A record out in 1960 isn't a year shown well: he keeps the book");
		lien.BarYear = 1961;
		desk.ReturnWintersBookForChecks(LienBar.Pay);
		Check(!lien.AddressBookBack, "Money isn't what he asked for: he keeps the book");
		var live = desk.ReturnWintersBookForChecks(LienBar.Show);
		Check(lien.AddressBookBack && live.transcript.Any(l => l.text.Contains("address book")), "A 1961 Show brings the book back, on the record");
		Check(!desk.WintersWorked(coldCard) && !desk.BuildCallContext(coldCard, null).youOweThem, "Clyde's cold card thaws");
		Check(desk.BuildCallContext(card, null).professionallyBurned, "A burn the Label Man earned himself stays");
	}

	private void SaveRoundTrip(PlayerDesk desk) {
		Fresh(desk, LienHolder.Winters);
		var cold = desk.Lien.WintersCold.ToList();
		var burned = desk.Lien.WintersBurned.ToList();
		desk.RestoreLienForChecks(desk.CaptureLienForChecks());
		Check(desk.Lien.WintersCold.SequenceEqual(cold) && desk.Lien.WintersBurned.SequenceEqual(burned) && !desk.Lien.AddressBookBack,
			"The cards he worked survive a save");
		desk.Lien.AddressBookBack = true;
		desk.RestoreLienForChecks(desk.CaptureLienForChecks());
		Check(desk.Lien.AddressBookBack, "So does the book coming back");
		var old = desk.CaptureLienForChecks();
		old.WintersCold = null; old.WintersBurned = null;
		desk.RestoreLienForChecks(old);
		Check(desk.Lien.WintersCold.Count == 0 && desk.Lien.WintersBurned.Count == 0, "An older save has no marks");
	}
}
