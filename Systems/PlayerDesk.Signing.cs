using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// The signing moment (playtest 2026-11-04: "Signed Kenny Sanders." and nothing else -- the first signing deserves a
// beat). Everything here is presentation over a signing that has already happened in FinalizeSigning: a catalogue
// number in the label's own series, one line from the act in its own temperament, and, for the label's first act, a
// front-page story. No random stream is touched; every line is chosen from the act's traits and the deal's terms.
//
// The push-back is the haggle beat for easy acts: an offer that clears their floor but lands well under the ask gets
// one "make it $X" before the pen goes down. It is computed here and asked by the UI before OfferContract runs, so the
// engine's accept-or-walk rule (and every runner that calls it) is unchanged: MEET HIM submits the bump, HOLD submits
// the original offer.
public partial class PlayerDesk {
	/// <summary>One signing, ready to be shown once. Not saved: a moment missed on load is just a signing.</summary>
	public sealed class SigningMoment {
		public string ArtistId, ActName, Signer, CatalogNumber, Reaction, Handshake;
		public bool First;
		public float Advance;
	}

	// Catalogue numbers in the order the label signed its acts: artistId -> 1, 2, 3... Saved.
	private readonly Dictionary<string, int> catalogNumbers = new();
	private SigningMoment pendingSigningMoment;

	/// <summary>How far under the ask an easy act's offer can land before they push back once.</summary>
	public const float PushbackUnderAsk = 0.35f;
	/// <summary>Under the ask by at least this much, the act pauses before it signs; less and it's a plain yes.</summary>
	private const float ReluctantUnderAsk = 0.05f;

	/// <summary>The signing the office hasn't shown yet, handed over once.</summary>
	public SigningMoment TakeSigningMoment() {
		SigningMoment moment = pendingSigningMoment;
		pendingSigningMoment = null;
		return moment;
	}

	/// <summary>"LR-001": the label's initials and the act's place in its signing order. Null for an act the label
	/// signed before catalogue numbers existed (an older save).</summary>
	public string CatalogNumberFor(string artistId) =>
		artistId != null && catalogNumbers.TryGetValue(artistId, out int n) ? CatalogNumber(n) : null;

	private string CatalogNumber(int n) => $"{CatalogPrefix(Label?.labelName)}-{n:000}";

	/// <summary>Initials of the label's own words: Lantern Records -> LR, Blue Bayou Recording Co. -> BBR.</summary>
	public static string CatalogPrefix(string labelName) {
		if (string.IsNullOrWhiteSpace(labelName)) return "LBL";
		string[] skip = { "the", "co.", "co", "&", "and", "of", "inc.", "inc" };
		string initials = string.Concat(labelName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Where(word => !skip.Contains(word.ToLowerInvariant()) && char.IsLetter(word[0]))
			.Select(word => char.ToUpperInvariant(word[0])));
		return initials.Length == 0 ? "LBL" : initials.Length > 3 ? initials[..3] : initials;
	}

	/// <summary>Restores the signing order. A save from before catalogue numbers numbers the acts already on the
	/// roster in the order they signed, so the next signing isn't announced as the label's first.</summary>
	private void LoadCatalogNumbers(Dictionary<string, int> saved) {
		catalogNumbers.Clear();
		pendingSigningMoment = null;
		foreach (var (artistId, n) in saved ?? new Dictionary<string, int>())
			if (!string.IsNullOrEmpty(artistId) && n > 0) catalogNumbers[artistId] = n;
		if (catalogNumbers.Count > 0 || Label?.roster == null) return;
		foreach (SimulatedArtist act in Label.roster.Where(a => a != null).OrderBy(a => a.signedYear).ThenBy(a => a.artistId, StringComparer.Ordinal))
			catalogNumbers[act.artistId] = catalogNumbers.Count + 1;
	}

	/// <summary>Called from FinalizeSigning once the act is on the roster.</summary>
	private void RecordSigningMoment(SimulatedArtist artist, ContractTermSheet sheet, float underAsk) {
		if (artist == null || Label == null) return;
		bool first = catalogNumbers.Count == 0;
		if (!catalogNumbers.ContainsKey(artist.artistId)) catalogNumbers[artist.artistId] = catalogNumbers.Count + 1;
		string number = CatalogNumber(catalogNumbers[artist.artistId]);
		Musician face = SpokesMember(artist);
		pendingSigningMoment = new SigningMoment {
			ArtistId = artist.artistId, ActName = artist.stageName, CatalogNumber = number,
			// A person signs with their own name: the leader for a band, the singer's legal name under a stage name.
			Signer = string.IsNullOrWhiteSpace(face?.FullName) ? artist.stageName : face.FullName, First = first, Advance = sheet.Advance,
			Reaction = underAsk >= ReluctantUnderAsk ? ReluctantLine(artist, face, underAsk) : null,
			Handshake = HandshakeLine(artist, face, underAsk),
		};
		if (first) {
			string genre = GenreNameFormatter.Format(artist.primaryGenre).ToLowerInvariant();
			Note($"FIRST SIGNING: {Label.labelName} puts {artist.stageName} under contract -- {genre} out of " +
				$"{CurrentCity?.name ?? "town"}, catalogue {number}. The first name on the label.");
		}
	}

	/// <summary>Who speaks for the act at the table: its leader, else its first active member.</summary>
	private static Musician SpokesMember(SimulatedArtist artist) =>
		artist?.members?.FirstOrDefault(m => m != null && m.isActive && m.isBandLeader)
		?? artist?.members?.FirstOrDefault(m => m != null && m.isActive)
		?? artist?.members?.FirstOrDefault(m => m != null);

	private static string FirstName(SimulatedArtist artist, Musician m) {
		string full = m?.FullName;
		if (string.IsNullOrWhiteSpace(full)) return artist?.stageName ?? "They";
		return full.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
	}

	private static (string He, string His) Pronouns(Musician m) =>
		m == null ? ("they", "their") : m.isMale ? ("he", "his") : ("she", "her");

	/// <summary>A pause before the pen goes down, for an offer that came in under the ask but over the push-back.</summary>
	private static string ReluctantLine(SimulatedArtist artist, Musician m, float underAsk) {
		string name = FirstName(artist, m);
		var (he, _) = Pronouns(m);
		return underAsk >= PushbackUnderAsk
			? $"{name} reads the number twice. Then {he} signs."
			: $"{name} looks at the number for a long moment. \"All right.\"";
	}

	/// <summary>One line in the act's own voice as the deal closes, picked from how it signed and who is holding the
	/// pen. Deterministic on the traits, so the same act always shakes hands the same way.</summary>
	public static string HandshakeLine(SimulatedArtist artist, Musician m, float underAsk) {
		string name = FirstName(artist, m);
		var (he, his) = Pronouns(m);
		bool band = (artist?.members?.Count(x => x != null && x.isActive) ?? 0) > 1;
		if (m == null) return $"{artist?.stageName ?? "They"} shake your hand. \"Let's make a record.\"";
		if (underAsk >= GrudgeNoticeFloor) return $"{name} signs without reading it twice. {Cap(he)}'ll read it twice at renewal.";
		if (m.temperament < 0.35f) return $"{name} grips your hand a beat too hard. \"Don't make me regret this.\"";
		if (m.ego > 0.6f) return $"{name} signs with a flourish and asks where the photographer is.";
		if (band && m.loyalty > 0.65f) return $"{name} passes the pen down the line until every one of them has signed. \"All of us or none of us.\"";
		if (m.ambition > 0.65f) return $"{name} folds {his} copy into {his} jacket. \"So when do we cut?\"";
		if (m.reliability < 0.45f) return $"{name} turns up forty minutes late to sign, and apologises like {he} means it.";
		if (m.creativity > 0.65f) return $"{name} is humming something new before the ink is dry.";
		return $"{name} shakes your hand, then shakes it again. \"Thank you. Really.\"";
	}

	private static string Cap(string s) => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];

	/// <summary>The one push-back an easy act makes before signing well under its ask: halfway back to the ask on
	/// each money axis that fell short, the advance rounded to $5. Null when the offer would be refused anyway (it
	/// goes through the existing counter), when it is close enough to sign, or for an act that negotiates in full.</summary>
	public ContractTermSheet? PushbackFor(Prospect prospect, ContractTermSheet offer) {
		if (prospect?.Artist == null || !prospect.HasBaseline || prospect.Posture != NegotiationPosture.Pushover) return null;
		ContractTermSheet ask = prospect.Baseline;
		if (!ClearsPushoverFloor(ask, offer, out _, ReadContractOutlook(prospect.Artist, renewal: false).Credit)) return null;
		if (UnderAskFraction(ask, offer) < PushbackUnderAsk) return null;
		float advance = offer.Advance < ask.Advance
			? Mathf.Min(ask.Advance, Mathf.Ceil((offer.Advance + ask.Advance) / 2f / 5f) * 5f) : offer.Advance;
		float royalty = offer.RoyaltyRate < ask.RoyaltyRate
			? Mathf.Min(ask.RoyaltyRate, Mathf.Ceil((offer.RoyaltyRate + ask.RoyaltyRate) / 2f * 400f) / 400f) : offer.RoyaltyRate;
		return new ContractTermSheet(advance, royalty, offer.TermYears, offer.SinglesObligation, offer.LabelOwnsPublishing,
			offer.ArtistCreativeControl, ask.NegotiationDifficulty, ask.Manager, ask.ManagerName, ask.DemandSummary);
	}

	/// <summary>What the act says when it pushes back: the bump, in its own words.</summary>
	public string PushbackLine(Prospect prospect, ContractTermSheet offer, ContractTermSheet bump) {
		SimulatedArtist artist = prospect.Artist;
		Musician m = SpokesMember(artist);
		string name = FirstName(artist, m);
		var (he, _) = Pronouns(m);
		var asks = new List<string>();
		if (bump.Advance > offer.Advance) asks.Add($"${bump.Advance:N0} up front");
		if (bump.RoyaltyRate > offer.RoyaltyRate + 0.00001f) asks.Add($"{bump.RoyaltyRate:P1} on the records");
		string want = string.Join(" and ", asks);
		return m != null && m.temperament < 0.35f
			? $"{name} pushes the paper back across the table. \"{want}. That's not a big ask.\""
			: $"{name} sets the pen down. \"Make it {want} and I'll shake on it.\"";
	}
}
