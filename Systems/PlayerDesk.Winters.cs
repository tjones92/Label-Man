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
		lien.AddressBookBack = true;
		live.Say("Weisz takes a battered address book out of his case and sets it on the desk. \"Mr. Winters asked me to return this. He says he took good care of it.\"", LienProfile.Lawyer);
		if (InstinctProfile.TheStreet >= 3) live.Say("He took care of it all right. Call them yourself now. They'll know the difference.", "", ExecutiveVoice.Street);
		Note("Clyde Winters sent your address book back. The jocks he worked in your name will hear from the real you now.");
	}

	/// <summary>The first sitting whose Show returns the book: a year of real sales, not just a record out.</summary>
	public const int WintersBookBackFromYear = 1961;

	// ── checks ─────────────────────────────────────────────────────────────────────────────────────

	internal RolodexEntry AddRolodexCardForChecks(RadioStation station) => EnsureStationEntry(station, discover: true);
	internal LienSittingLive ReturnWintersBookForChecks(LienBar bar) { var live = new LienSittingLive(); ReturnWintersBook(live, bar); return live; }
	internal void ClearRolodexForChecks() => rolodex.Clear();
	internal List<RadioStation> HomeReportersForChecks() => HomeReportersByInfluence();
}
