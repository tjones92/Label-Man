using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// CLYDE WINTERS AND YOUR ADDRESS BOOK (SimTools/LabelManInnerLifeDirective.md §3.5 H3, Phase 1b).
//
// Clyde took your address book on New Year's Eve and has spent the time since calling your contacts as you: borrowing money,
// pitching his songs, making promises. So the hometown Rolodex starts cold, and some cards start burned:
//   - The jocks he worked are the home region's reporters worth calling: the most influential half, at most six.
//   - Cold: every one of them thinks you owe him one (a promise Clyde made in your name).
//   - Burned: the top third of them (at least one) also stopped taking your word (Clyde's songs, pitched as yours).
//   - Turning down his first move (§6.1) burns one more cold card: he tells a jock you stiffed his publisher.
//   - The book comes back after a year you Show well (a Show met at a sitting from 1961), and every card thaws.
// The marks live on the lien, not on the cards: the cards' own flags stay for what the player earned himself, and the thaw
// never clears a burn that wasn't Clyde's. Player-only: the Rolodex has no AI reader, and nothing here draws a random number.
// ============================================================================================
public partial class PlayerDesk {
	public const int WintersMaxStations = 6;

	/// <summary>The home region's reporter stations, lead jock most influential first, ties by id.</summary>
	private List<RadioStation> HomeReportersByInfluence() {
		var chart = ChartManager.Instance;
		var city = Label == null ? null : DistanceModel.GetCityById(Label.homeCityId);
		if (chart == null || city == null) return new();
		return (chart.ReporterStationsInRegion(city.parentRegionId) ?? Array.Empty<RadioStation>())
			.Where(s => s != null && !string.IsNullOrEmpty(s.leadDjId) && chart.GetDeejay(s.leadDjId) != null)
			.OrderByDescending(s => chart.GetDeejay(s.leadDjId).influence).ThenBy(s => s.stationId, StringComparer.Ordinal).ToList();
	}

	/// <summary>At founding: the jocks Clyde has been calling as you, and the ones he burned.</summary>
	private void SeedWintersBook() {
		if (lien == null || lien.Holder != LienHolder.Winters) return;
		var all = HomeReportersByInfluence();
		var worked = all.Take(Math.Min(WintersMaxStations, (all.Count + 1) / 2)).ToList();
		int burned = worked.Count == 0 ? 0 : Math.Max(1, (worked.Count + 2) / 3);
		lien.WintersBurned = worked.Take(burned).Select(s => s.stationId).ToList();
		lien.WintersCold = worked.Skip(burned).Select(s => s.stationId).ToList();
	}

	private bool WintersBookOut => lien?.Holder == LienHolder.Winters && !lien.AddressBookBack && !IsGameOver;

	/// <summary>Clyde has been calling this jock as you: he thinks you owe him one.</summary>
	public bool WintersWorked(RolodexEntry entry) => entry != null && WintersBookOut
		&& (lien.WintersCold.Contains(entry.stationId) || lien.WintersBurned.Contains(entry.stationId));

	/// <summary>Clyde pitched his songs to this jock as yours: he doesn't take your word any more.</summary>
	public bool WintersBurnedCard(RolodexEntry entry) => entry != null && WintersBookOut && lien.WintersBurned.Contains(entry.stationId);

	/// <summary>The card's own line on it: who's been calling, as far as the Label Man knows.</summary>
	public string WintersCardLine(RolodexEntry entry) {
		if (!WintersWorked(entry)) return null;
		string who = LienHolderIdentified ? "Clyde Winters has been calling him as you, out of your address book." : "Somebody has been calling him in your name.";
		return who + (WintersBurnedCard(entry) ? " He pitched him songs as yours, and they stank." : " He made him promises you never made.");
	}

	/// <summary>A card just went into the book: if Clyde got there first, the log says so.</summary>
	private void NoteWintersOnNewCard(RolodexEntry entry) {
		if (!WintersWorked(entry)) return;
		entry.log.Add($"{Today()} — " + (WintersBurnedCard(entry)
			? "He's heard from you already. \"You\" pitched him three songs last month and borrowed twenty dollars. He wants the twenty."
			: "He's heard from you already. \"You\" promised him the first copy of everything. He's still waiting."));
	}

	/// <summary>His first move turned down: he tells the biggest jock still only cold that you stiffed his publisher.</summary>
	private string BurnOneWintersCard() {
		if (!WintersBookOut || lien.WintersCold.Count == 0) return null;
		string id = lien.WintersCold[0];
		lien.WintersCold.RemoveAt(0);
		lien.WintersBurned.Add(id);
		var station = ChartManager.Instance?.GetRadioStation(id);
		rolodex.FirstOrDefault(e => e.stationId == id)?.log.Add($"{Today()} — Clyde got to him first: you stiffed his publisher. He doesn't take your word any more.");
		return station == null ? "" : $" By the end of the week {station.callsign} has heard you stiffed his publisher.";
	}

	/// <summary>A year he was shown well: Clyde sends the book back, and the cards he worked thaw.</summary>
	private void ReturnWintersBook(LienSittingLive live, LienBar bar) {
		if (!WintersBookOut || bar != LienBar.Show || lien.BarYear < WintersBookBackFromYear) return;
		live.Say("Weisz takes a battered address book out of his case and sets it on the desk. \"Mr. Winters asked me to return this. He says he took good care of it.\"", LienProfile.Lawyer);
		if (InstinctProfile.TheStreet >= 3) live.Say("He took care of it all right. Call them yourself now. They'll know the difference.", "", ExecutiveVoice.Street);
		foreach (string line in BookComesBack("Weisz returned it at the sitting")) live.Say(line, "", line.StartsWith("A cheque stub") ? ExecutiveVoice.Suit : ExecutiveVoice.None);
		Note("Clyde Winters sent your address book back. The jocks he worked in your name will hear from the real you now.");
	}

	// ── his secretary (§3.5 H3: "Fixer or Street with his secretary") ───────────────────────────────

	/// <summary>The score a route through Clyde's secretary takes, the hours, and what the Fixer's envelope costs (provisional).</summary>
	public const int WintersSecretaryScore = 3;
	public const int WintersSecretaryStreetHours = 3, WintersSecretaryFixerHours = 1;
	public const float WintersSecretaryFixerCost = 40f;

	public enum WintersSecretaryRoute { Street, Fixer }

	/// <summary>Clyde is named and still has the book: his secretary at the publisher's office in town can get it back.</summary>
	public bool WintersSecretaryOpen => WintersBookOut && LienHolderIdentified;

	/// <summary>What the paper says about the book while it's out, or after it's back.</summary>
	public string WintersBookLead() {
		if (lien?.Holder != LienHolder.Winters || !LienHolderIdentified) return null;
		if (lien.AddressBookBack) return lien.ChequeStub switch {
			ChequeStubStage.Found => "Your address book is back. Folded in the back is a cheque stub you can't make sense of. The Suit could read it.",
			ChequeStubStage.Read => "Your address book is back, and the cheque stub in it says Clyde's $20,000 never left a bank. Weisz knows you know.",
			_ => "Your address book is back.",
		};
		return "Clyde keeps your address book in his desk at the publisher's office in town. His secretary sits outside it. " +
			$"The Street could talk her round over lunch; the Fixer could make it worth her while. Or show Clyde a good year from {WintersBookBackFromYear} and he'll send it back.";
	}

	/// <summary>Why a route through the secretary can't be taken now, or null.</summary>
	public string WintersSecretaryBlock(WintersSecretaryRoute route) {
		if (!WintersSecretaryOpen) return "There's nothing to get back.";
		if (!AtHome) return "Clyde's office is in town. You have to be home.";
		int score = route == WintersSecretaryRoute.Street ? InstinctProfile.TheStreet : InstinctProfile.TheFixer;
		if (score < WintersSecretaryScore) return $"{route} {WintersSecretaryScore} needed.";
		if (route == WintersSecretaryRoute.Fixer && Label.cashReserves < WintersSecretaryFixerCost) return $"The envelope takes ${WintersSecretaryFixerCost:N0}.";
		return null;
	}

	/// <summary>Get the book out of Clyde's desk through his secretary.</summary>
	public bool TakeBookFromSecretary(WintersSecretaryRoute route, out string message) {
		message = WintersSecretaryBlock(route);
		if (message != null) return false;
		int hours = route == WintersSecretaryRoute.Street ? WintersSecretaryStreetHours : WintersSecretaryFixerHours;
		if (!Require(hours, out message)) return false;
		Spend(hours);
		if (route == WintersSecretaryRoute.Fixer) {
			Label.cashReserves -= WintersSecretaryFixerCost;
			Label.monthlyExpenses += WintersSecretaryFixerCost;
		}
		var lines = BookComesBack(route == WintersSecretaryRoute.Street ? "His secretary, over lunch" : "His secretary, for an envelope");
		message = (route == WintersSecretaryRoute.Street
			? "Lunch at the counter across from the publisher's office. Clyde's secretary has been covering for him all year and is tired of it. She brings the book down in her handbag."
			: $"Flowers on her desk, and ${WintersSecretaryFixerCost:N0} in an envelope under them. By four o'clock your address book is in a manila folder at your office door.")
			+ " " + string.Join(" ", lines);
		Note(message);
		Changed?.Invoke();
		return true;
	}

	// ── what's in the book ─────────────────────────────────────────────────────────────────────────

	public enum ChequeStubStage { None, Found, Read }

	/// <summary>The book is back, however it came: the cards thaw, its numbers become leads, and the cheque stub is in the back.
	/// Returns what the Label Man notices, line by line.</summary>
	private List<string> BookComesBack(string how) {
		var lines = new List<string>();
		lien.AddressBookBack = true;
		lien.Add(LienToday, LienLedgerKind.AddressBookBack, 0f, how);
		int leads = AddBookLeads();
		if (leads > 0) lines.Add($"{Plural.Of(leads, "jock")} in the home region you'd never reached {(leads == 1 ? "is" : "are")} in there, home numbers and all.");
		lien.ChequeStub = ChequeStubStage.Found;
		if (InstinctProfile.TheSuit >= WintersChequeStubSuit) {
			ReadChequeStubNow();
			lines.Add(ChequeStubReadLine);
		} else lines.Add("Folded in the back is a cheque stub. It means nothing to you yet.");
		return lines;
	}

	/// <summary>The book's numbers: every jock Clyde worked who isn't in the Rolodex yet. A Promo Man's book (his signature belonging,
	/// §3.8) has every reporter in the home region, and his first calls start warmer.</summary>
	private int AddBookLeads() {
		var chart = ChartManager.Instance;
		if (chart == null) return 0;
		bool promo = Archetype == FoundingArchetype.PromoMan;
		var known = new HashSet<string>(rolodex.Select(e => e.stationId), StringComparer.Ordinal);
		var worked = new HashSet<string>(lien.WintersCold.Concat(lien.WintersBurned), StringComparer.Ordinal);
		int added = 0;
		foreach (var station in HomeReportersByInfluence()) {
			if (known.Contains(station.stationId) || !promo && !worked.Contains(station.stationId)) continue;
			var dj = chart.GetDeejay(station.leadDjId);
			string name = SynthesizeDJName(dj, station);
			var entry = new RolodexEntry {
				djId = dj.djId, stationId = station.stationId, state = DiscoveryState.HeardOf,
				displayName = name, portraitKey = dj.archetype.ToString(), shiftKnown = true,
			};
			entry.log.Add($"{Today()} — In your address book: {name}, {station.callsign}, with his home number.");
			rolodex.Add(entry);
			if (promo) ApplyRapport(entry, WintersPromoLeadRapport);
			added++;
		}
		return added;
	}

	/// <summary>Rapport a Promo Man's own book starts a lead at: he's called these men before (provisional).</summary>
	public const float WintersPromoLeadRapport = 0.05f;

	// ── the cheque stub (§3.5 H3: "The cheque stub for the 'advance' is inside; Suit 3+ reads it") ──────

	public const int WintersChequeStubSuit = 3;
	/// <summary>Standing lost each time the stub stands in for Show: Weisz likes you less every time (provisional). Net of the
	/// +0.1 any Show of Winters' stressed bar earns, it's -0.1.</summary>
	public const float WintersStubStanding = 0.2f;

	private const string ChequeStubReadLine = "A cheque stub for $20,000, dated 31 December 1959, made out to you. The counterfoil was never stamped by a bank. " +
		"Clyde never had the money.";

	private void ReadChequeStubNow() {
		lien.ChequeStub = ChequeStubStage.Read;
		lien.Add(LienToday, LienLedgerKind.ReadChequeStub, 0f, "the $20,000 never left a bank");
	}

	/// <summary>At the desk: the Suit reads the stub the book came back with.</summary>
	public bool ReadChequeStub(out string message) {
		if (lien?.Holder != LienHolder.Winters || lien.ChequeStub != ChequeStubStage.Found || IsGameOver) { message = "There's no unread stub on the desk."; return false; }
		if (!AtHome) { message = "The stub is at the office."; return false; }
		if (InstinctProfile.TheSuit < WintersChequeStubSuit) { message = $"It's a cheque stub. The Suit needs {WintersChequeStubSuit} to see what's wrong with it."; return false; }
		if (!Require(1, out message)) return false;
		Spend(1);
		ReadChequeStubNow();
		message = ChequeStubReadLine;
		Note(message);
		Changed?.Invoke();
		return true;
	}

	/// <summary>The read stub at a Winters sitting: one honest answer when the copies fall short. It rolls the lien as Show, and
	/// costs standing each time (§3.4: every link found gives an honest counter at every later sitting).</summary>
	private LienOption ChequeStubOption() {
		if (lien?.Holder != LienHolder.Winters || lien.ChequeStub != ChequeStubStage.Read || lien.IsReckoning || LienShow().Met) return null;
		return new LienOption { verb = LienVerb.ShowStub, voice = ExecutiveVoice.Suit, label = "Lay the cheque stub on the desk",
			subLabel = "True, and Weisz knows what it means. He'll roll the paper rather than argue. He'll like you less for it." };
	}

	private void UseChequeStub(LienSittingLive live) {
		lien.Standing = Mathf.Max(0f, lien.Standing - WintersStubStanding);
		lien.Add(LienToday, LienLedgerKind.ReadChequeStub, 0f, "laid on the desk at the sitting");
		live.Say("Weisz looks at the stub for a long time. He doesn't pick it up. \"Mr. Winters is satisfied with the year,\" he says, and writes in the new date.", LienProfile.Lawyer);
	}

	/// <summary>The first sitting whose Show returns the book: a year of real sales, not just a record out.</summary>
	public const int WintersBookBackFromYear = 1961;

	// ── checks ─────────────────────────────────────────────────────────────────────────────────────

	internal RolodexEntry AddRolodexCardForChecks(RadioStation station) => EnsureStationEntry(station, discover: true);
	internal LienSittingLive ReturnWintersBookForChecks(LienBar bar) { var live = new LienSittingLive(); ReturnWintersBook(live, bar); return live; }
	internal int RolodexCountForChecks() => rolodex.Count;
	internal RolodexEntry RolodexCardForChecks(string stationId) => rolodex.FirstOrDefault(e => e.stationId == stationId);
	internal void ClearRolodexForChecks() => rolodex.Clear();
	internal List<RadioStation> HomeReportersForChecks() => HomeReportersByInfluence();
}
