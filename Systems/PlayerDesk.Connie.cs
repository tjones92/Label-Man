using System;
using System.Linq;
using Godot;

// ============================================================================================
// CONNIE BOURSAW (SimTools/LabelManInnerLifeDirective.md §3.5 H1, Phase 1b).
//
// When Dee-Dee Sawyer holds the paper, his girlfriend's contract came with it. She is on the roster from the first day, an
// underwhelming singer on terms the Label Man never negotiated: a five-year term whose option is Dee-Dee's, the highest royalty
// the paper allows, a side a year, no advance, and no release clause. The player's roster has no drop verb, so "can't drop her"
// means her paper never lapses: the day it matures Dee-Dee exercises his option and it runs another term, so no rival ever finds
// it run out and the player is never asked to renew it. She leaves his hold only by reaching Star (she's outgrown him, §3.5):
// from then on her paper is an ordinary act's.
//
// What he asks each December is her career (ConnieAsk), cumulative so a year she hasn't moved is a year the ask isn't met. An
// unmet ask makes Pay cost 1.5x. The first time she charts he hands back the keys to the car (§3.9).
//
// Player-only and AI-inert: she is generated from a private stream keyed on the world seed (ArtistManager.GenerateKeyedArtist),
// so a Sawyer run and any other draw the same world, and a world with no player never reaches this file.
// ============================================================================================
public partial class PlayerDesk {
	public const string ConnieStageName = "Connie Boursaw";
	public const float ConnieRoyalty = 0.15f;       // the highest the paper allows (§3.5: "a high royalty")
	public const int ConnieTermYears = 5;
	public const int ConnieSidesPerTerm = 5;        // a guaranteed session every year
	/// <summary>Pay costs this much when her career hasn't moved the way he asked.</summary>
	public const float ConnieStalledPayMultiple = 1.5f;

	/// <summary>The act on the roster, or null: no lien, another holder, or she's gone.</summary>
	public SimulatedArtist Connie => lien?.ConnieId == null ? null : ArtistManager.Instance?.GetArtist(lien.ConnieId);

	/// <summary>True while Dee-Dee holds the option on this act's paper: no release clause, and it never lapses.</summary>
	public bool IsHeldByHolder(SimulatedArtist artist) =>
		lien != null && lien.ConnieId != null && !lien.ConnieFree && artist != null && artist.artistId == lien.ConnieId;

	private void SeedConnie(string previousId) {
		if (Label?.roster != null && previousId != null) Label.roster.RemoveAll(a => a.artistId == previousId);
		if (lien == null || lien.Holder != LienHolder.Sawyer || Label == null || ArtistManager.Instance == null) return;
		int year = LienToday.year;
		ScenePlace place = ScenePlaceRegistry.Get(Label.homeCityId);
		SimulatedArtist connie = ArtistManager.Instance.GenerateKeyedArtist(ArtistType.SoloFemale, Genre.TraditionalPop, year, Label.homeRegion,
			"connie|" + Label.homeCityId, place);
		// Underwhelming and fixed (artist identity is frozen): nothing here changes again, the player's choices around her do.
		var voice = connie.members.FirstOrDefault(m => m.isActive) ?? connie.members.FirstOrDefault();
		if (voice != null) {
			voice.firstName = "Connie"; voice.lastName = "Boursaw"; voice.isMale = false; voice.isLeadVocalist = true;
			voice.technicalSkill = 0.38f; voice.creativity = 0.30f; voice.stagePresence = 0.42f; voice.studioEfficiency = 0.40f;
		}
		connie.stageName = ConnieStageName;
		connie.RecalculateStats();
		// His paper, not the label's: no advance, the top royalty, five years with the option his, a side a year, and the label keeps
		// the publishing. Label.SignArtist applies it exactly as it would a negotiated sheet.
		var sheet = new ContractTermSheet(0f, ConnieRoyalty, ConnieTermYears, ConnieSidesPerTerm, true, false, 0f, ManagerArchetype.None, null,
			"Dee-Dee Sawyer's paper: five years, his option, the top royalty, a side a year, and no release clause.");
		Label.SignArtist(connie, year, sheet);
		ArtistManager.Instance.SignArtist(connie, Label.labelId, year);
		lien.ConnieId = connie.artistId;
		Note($"Dee-Dee Sawyer's girlfriend, {ConnieStageName}, is on your roster. It came with the paper: five years, his option, no release clause.");
	}

	// ── what he asks (§3.5 H1) ───────────────────────────────────────────────────────────────────────

	/// <summary>Where he wants her career to be at this sitting, and whether it is. Cumulative: a career that got there stays there.</summary>
	public (bool Met, string What) ConnieAsk() {
		var c = Connie;
		if (c == null) return (true, "");
		int year = lien.BarYear;
		bool star = c.careerState is CareerState.Star or CareerState.Superstar || lien.ConnieFree;
		if (year <= 1960) {
			bool out0 = ReleasedRecords.Any(r => r.baseRecord?.artistId == c.artistId);
			return (out0, out0 ? "A record by Connie is out." : "A record by Connie, out.");
		}
		if (year <= 1962) {
			bool ok = c.regionalBreakouts > 0 || c.charted > 0 || star;
			return (ok, ok ? "Connie is on a regional chart." : "Connie on a regional chart.");
		}
		if (year <= 1964) {
			bool ok = c.charted > 0 || star;
			return (ok, ok ? "Connie has been on the Hot 100." : "Connie in the Hot 100.");
		}
		if (year <= 1966) {
			bool ok = c.top40Hits > 0 || c.careerState >= CareerState.Rising && c.careerState <= CareerState.Superstar || star;
			return (ok, ok ? "Connie is a Top 40 act, or rising." : "Connie in the Top 40, or Rising.");
		}
		return (star, star ? "Connie is a Star." : "Connie at Star: a number one.");
	}

	/// <summary>Whether Pay costs the 1.5 multiple this year: the holder is Sawyer, she's on the paper, and she hasn't moved.</summary>
	public bool ConnieStalled => lien != null && lien.Holder == LienHolder.Sawyer && !lien.IsReckoning && Connie != null && !ConnieAsk().Met;

	/// <summary>Sawyer's rule for player bookings at his room. Each performance year needs its own Connie release.</summary>
	private string SawyerBookingRefusal(SceneRoomProfile room, string artistId, int year) {
		if (lien?.Holder != LienHolder.Sawyer || lien.Ending != null || lien.VenueId != room.Id
			|| lien.ConnieId == null || lien.ConnieId == artistId) return null;
		if (ReleasedRecords.Any(r => r.baseRecord.artistId == lien.ConnieId && r.baseRecord.releaseDate.year == year)) return null;
		if (SawyerLaunchWaives(year)) return null;   // §6.1: he waived it for the year you launched at his room
		return $"{room.ContactName}: \"Dee-Dee says I can't book your other acts at {room.Name} for {year} until Connie has a record out that year.\"";
	}

	// ── each day ─────────────────────────────────────────────────────────────────────────────────────

	/// <summary>Dawn: Dee-Dee exercises his option on a matured paper, hands back the keys the first time she charts, and lets go
	/// when she reaches Star. Runs before the rivals look for run-out paper, so nobody ever finds hers run out.</summary>
	private void ProcessHolderOption(GameDate date) {
		var c = Connie;
		if (lien == null || Label == null || c == null || c.labelId != Label.labelId) return;
		if (!lien.ConnieFree && c.careerState is CareerState.Star or CareerState.Superstar) {
			lien.ConnieFree = true;
			Note($"{ConnieStageName} has outgrown Dee-Dee Sawyer. Her paper is an ordinary act's now.");
		}
		if (!lien.CarRecovered && (c.charted > 0 || c.regionalBreakouts > 0)) {
			lien.CarRecovered = true;
			if (lien.Car != CarKind.New) lien.Car = CarKind.Original;
			lien.TripCopies = 0;
			Note($"{ConnieStageName} is on the chart and Dee-Dee is in a good mood. He sends your car back round with the keys in it.");
		}
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		if (!lien.ConnieFree && RosterManager.IsContractMatured(c, date.year, week)) {
			c.contractExpiresYear = date.year + c.contractLength;
			c.contractExpiresWeek = week + c.contractLength * 52;
			c.contractReleases = 0;
			maturedNotified.Remove(c.artistId);
			c.careerEvents.Add($"{date.year}: Dee-Dee Sawyer exercised his option: {c.contractLength} more years, same terms");
			Note($"Dee-Dee Sawyer exercised his option on {ConnieStageName}: {c.contractLength} more years, same terms.");
		}
	}

	/// <summary>The roster card's line about her paper, or null for any other act.</summary>
	public string HolderPaperLine(SimulatedArtist artist) =>
		IsHeldByHolder(artist) ? "Dee-Dee Sawyer's paper: his option, no release clause" : null;

	/// <summary>Checks only: run a dawn of the holder's option.</summary>
	internal void ProcessHolderOptionForChecks(GameDate date) => ProcessHolderOption(date);
}
