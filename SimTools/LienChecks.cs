using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using static PlayerDesk;

/// <summary>
/// Inner-life §3.5-3.6: the five holders. The opening answers are balanced (each holder fits exactly five, "nothing" fits
/// nobody, an all-blank night is a uniform pick with no wrong memories); the keyed pick is stable and a holder's own answers
/// pick him on every seed; each holder carries his own rate; Sawyer owns the hometown's biggest room and every line names
/// it; Kubiak's unpaid interest compounds at the rollover and nobody else's does; Floria refuses money at sittings from the
/// 1966 bar year (and a no-show is then a missed deadline) but takes it at the reckoning; the venue survives a save.
///   --path . SimTools/LienChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints LIEN_CHECKS_PASS / _FAIL.
/// </summary>
public partial class LienChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static bool Near(float a, float b, float tol = .011f) => Math.Abs(a - b) <= tol;

	/// <summary>Answers that fit this holder and leave every other holder at least two behind.</summary>
	private static readonly Dictionary<LienHolder, PartyMemory> Own = new() {
		[LienHolder.Sawyer] = new() { Where = PartyWhere.SupperClub, Brag = PartyBrag.TheAct, End = PartyEnd.CardTable },
		[LienHolder.Kubiak] = new() { Where = PartyWhere.BarBackRoom, Who = PartyWho.RouteMan, Brag = PartyBrag.TheLabel, End = PartyEnd.Car },
		[LienHolder.Winters] = new() { Where = PartyWhere.DjHouseParty, Who = PartyWho.MajorMan, Brag = PartyBrag.Hits, End = PartyEnd.PhoneCall },
		[LienHolder.Floria] = new() { Where = PartyWhere.HotelBallroom, Who = PartyWho.CamelCoat, Brag = PartyBrag.TheLabel, End = PartyEnd.PhoneCall },
		[LienHolder.Germain] = new() { Where = PartyWhere.DjHouseParty, Who = PartyWho.DiscJockey, Brag = PartyBrag.TheAct, End = PartyEnd.Car },
	};

	public override void _Ready() {
		try {
			Mapping();
			var desk = PlayerDesk.Instance;
			Check(desk.FoundLabel("Lien Checks", "detroit", FoundingArchetype.ExMusician, null, Own[LienHolder.Sawyer], out var message), "Found: " + message);
			Founding(desk);
			foreach (var h in Enum.GetValues<LienHolder>()) Holder(desk, h);
			Compounding(desk);
			Floria(desk);
			SaveRoundTrip(desk);
			GD.Print($"LIEN_CHECKS_PASS ({checks} checks)");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("LIEN_CHECKS_FAIL: " + e.Message);
			GetTree().Quit(1);
		}
	}

	private void Mapping() {
		var singles = new List<PartyMemory>();
		singles.AddRange(PartyWhereAnswers.Where(a => a.Value != PartyWhere.Nothing).Select(a => new PartyMemory { Where = a.Value }));
		singles.AddRange(PartyWhoAnswers.Where(a => a.Value != PartyWho.Nothing).Select(a => new PartyMemory { Who = a.Value }));
		singles.AddRange(PartyBragAnswers.Where(a => a.Value != PartyBrag.Nothing).Select(a => new PartyMemory { Brag = a.Value }));
		singles.AddRange(PartyEndAnswers.Where(a => a.Value != PartyEnd.Nothing).Select(a => new PartyMemory { End = a.Value }));
		var fits = Enum.GetValues<LienHolder>().ToDictionary(h => h, h => singles.Count(m => PartyScores(m)[h] == 1));
		Check(fits.Values.All(n => n == 5), "Each holder fits exactly five answers: " + string.Join(", ", fits.Select(kv => $"{kv.Key} {kv.Value}")));
		Check(singles.All(m => PartyScores(m).Values.Sum() >= 1), "Every real answer fits at least one holder");
		Check(PartyEndAnswers.Any(a => a.Value == PartyEnd.Nothing) && PartyWhoAnswers.Any(a => a.Text == "A disc jockey")
			&& PartyBragAnswers.Any(a => a.Text == "The hits you can pick"), "The answer set has the disc jockey and the hits you can pick");

		var blank = new PartyMemory();
		Check(PartyScores(blank).Values.All(s => s == 0) && Enum.GetValues<LienHolder>().All(h => WrongMemories(blank, h).Count == 0),
			"An all-blank night scores nobody and leaves no wrong memories");
		var drawn = Enumerable.Range(1, 400).Select(s => PickHolder(blank, (ulong)s)).ToList();
		Check(Enum.GetValues<LienHolder>().All(h => drawn.Count(x => x == h) >= 40), "An all-blank night can draw every holder: "
			+ string.Join(", ", Enum.GetValues<LienHolder>().Select(h => $"{h} {drawn.Count(x => x == h)}")));

		foreach (var (h, m) in Own) {
			Check(Enumerable.Range(1, 200).All(s => PickHolder(m, (ulong)s) == h), $"{h}'s own answers pick him on every seed");
			Check(PickHolder(m, 77UL) == PickHolder(m.Copy(), 77UL), $"{h}: the pick is stable for a seed and answers");
		}
		var wrong = WrongMemories(new PartyMemory { Where = PartyWhere.SupperClub, Who = PartyWho.CamelCoat, End = PartyEnd.CardTable }, LienHolder.Sawyer);
		Check(wrong.SequenceEqual(new[] { nameof(PartyMemory.Who) }), "Sawyer wasn't in a camel coat: that answer is the wrong memory");
	}

	private void Founding(PlayerDesk desk) {
		var lien = desk.Lien;
		Check(lien != null && lien.Holder == LienHolder.Sawyer && Near(lien.Rate, .04f), $"Sawyer holds the paper at 4%: {lien?.Holder} {lien?.Rate}");
		string scene = LocalScenePersistenceService.SceneIdFor(desk.Label.homeCityId);
		var biggest = SceneRoomCatalog.ForPlace(scene).Where(r => r.IsPerformance).OrderByDescending(r => r.Capacity).ThenBy(r => r.Id, StringComparer.Ordinal).First();
		Check(lien.VenueId == biggest.Id && lien.Venue == "the " + biggest.Name, $"The venue is the hometown's biggest room: {lien.Venue} ({biggest.Capacity})");
		Check(desk.LienProfile.Title.Contains(biggest.Name) && desk.LienProfile.Seizure.Contains(biggest.Name), "Sawyer's title and seizure name that room: " + desk.LienProfile.Title);
		string lost = desk.LostBelongings();
		Check(lost.Contains("car") && lost.Contains("watch") && lost.Contains("pedal steel"), "The Ex-Musician owing Sawyer lost the car, the watch and the pedal steel: " + lost);
		var morning = desk.LienMorningLines();
		Check(morning.Any(l => l.text.Contains("Halloran")) && morning.All(l => !l.text.Contains("{venue}")), "The morning names the lawyer and no placeholder");
	}

	private void Holder(PlayerDesk desk, LienHolder h) {
		desk.FoundLienForChecks(Own[h], h);
		var p = desk.LienProfile;
		Check(desk.Lien.Holder == h && Near(desk.Lien.Rate, LienHolders[h].Rate, .0001f), $"{h} ({p.Name}) carries his own rate {p.Rate:P0}");
		var text = new[] { p.Title, p.Opening, p.WantsLine, p.PayAccepted, p.ShowAccepted, p.ConcedeAccepted, p.Pushback, p.HonestLine,
			p.BluffLine, p.BluffCaught, p.Seizure, p.FixerRead, p.RefusesPayLine }.Where(s => s != null).ToList();
		Check(text.All(s => !s.Contains("{venue}")), $"{h}: every line has the room filled in");
		Check(!desk.LienRefusesPay, $"{h}: takes money in 1960");
	}

	private void Compounding(PlayerDesk desk) {
		foreach (var h in new[] { LienHolder.Kubiak, LienHolder.Winters, LienHolder.Floria }) {
			desk.FoundLienForChecks(Own[h], h);
			var lien = desk.Lien;
			var signed = lien.Signed;
			var rollover = new GameDate(signed.year + 1, 1, 1);
			int days = (int)(new DateTime(rollover.year, rollover.month, rollover.day) - new DateTime(signed.year, signed.month, signed.day)).TotalDays;
			float interest = Mathf.Round(20000f * lien.Rate * days / 365f * 100f) / 100f;
			lien.Extended = true;
			lien.PendingDeadline = new GameDate(rollover.year + 1, 1, 1);
			desk.EnforceLienForChecks(rollover);
			Check(lien.Deadline.Equals(new GameDate(rollover.year + 1, 1, 1)) && !lien.Extended, $"{h}: an extended lien rolls over on the 1st");
			if (h == LienHolder.Kubiak)
				Check(Near(lien.FaceSum, 20000f + interest) && Near(lien.InterestDue, 0f) && lien.Ledger.Any(e => e.Kind == LienLedgerKind.WrittenOn),
					$"Kubiak: ${interest:N2} unpaid interest went onto the paper (face ${lien.FaceSum:N2})");
			else
				Check(Near(lien.FaceSum, 20000f) && lien.Ledger.All(e => e.Kind != LienLedgerKind.WrittenOn), $"{h}: unpaid interest stays interest (face ${lien.FaceSum:N2})");
		}
	}

	private void Floria(PlayerDesk desk) {
		desk.FoundLienForChecks(Own[LienHolder.Floria], LienHolder.Floria);
		var lien = desk.Lien;
		desk.Label.cashReserves = 100000f;
		var live = new LienSittingLive();
		lien.BarYear = 1965;
		var pay = desk.LienSittingOptions(live).First(o => o.verb == LienVerb.Pay);
		Check(pay.enabled && !desk.LienRefusesPay, "Floria takes money at the 1965 sitting");
		lien.BarYear = 1966;
		pay = desk.LienSittingOptions(live).First(o => o.verb == LienVerb.Pay);
		Check(!pay.enabled && desk.LienRefusesPay && pay.disabledReason == desk.LienProfile.RefusesPayLine, "From the 1966 sitting he won't: " + pay.disabledReason);
		Check(desk.LienSittingOptions(live).Any(o => o.verb == LienVerb.Show), "Show is still on the table");

		lien.Deadline = new GameDate(1967, 1, 1);
		lien.Extended = false; lien.ConsecutiveFailures = 0;
		desk.EnforceLienForChecks(new GameDate(1967, 1, 1));
		Check(lien.ConsecutiveFailures == 1 && lien.Deadline.Equals(new GameDate(1967, 4, 1)), "A no-show with money in the till is still a missed deadline with Floria");

		lien.Deadline = new GameDate(1970, 1, 1);
		Check(lien.IsReckoning && !desk.LienRefusesPay, "At the reckoning he has to take the money");
	}

	private void SaveRoundTrip(PlayerDesk desk) {
		desk.FoundLienForChecks(Own[LienHolder.Sawyer], LienHolder.Sawyer);
		var before = desk.Lien;
		var data = desk.CaptureLienForChecks();
		desk.RestoreLienForChecks(data);
		var after = desk.Lien;
		Check(after.Holder == before.Holder && after.Venue == before.Venue && after.VenueId == before.VenueId && Near(after.Rate, before.Rate, .0001f),
			"Holder, rate and venue survive a save: " + after.Venue);
		data.Venue = null; data.VenueId = null;
		desk.RestoreLienForChecks(data);
		Check(desk.LienProfile.Title.Contains("the best room in town"), "An older save without a venue falls back: " + desk.LienProfile.Title);
	}
}
