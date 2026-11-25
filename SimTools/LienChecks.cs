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
			FloriaBuyout(desk, OS.GetCmdlineUserArgs().Contains("--floria-forced"));
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
		Check(PartyEndAnswers.Any(a => a.Value == PartyEnd.Nothing) && PartyWhoAnswers.Any(a => a.Value == PartyWho.DiscJockey)
			&& PartyBragAnswers.Any(a => a.Value == PartyBrag.Hits), "The answer set has the disc jockey and the hits you can pick");

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

	private void FloriaBuyout(PlayerDesk desk, bool forced) {
		var clock = TimeManager.Instance;
		var chart = ChartManager.Instance;
		void Sitting(int year) {
			desk.CloseLienSitting();
			clock.RestoreClock(GameDate.StartDate, 9);
			desk.FoundLienForChecks(Own[LienHolder.Floria], LienHolder.Floria);
			desk.Lien.BarYear = year;
			desk.Lien.Deadline = new GameDate(year + 1, 1, 1);
			clock.RestoreClock(new GameDate(year, 12, 20), 9);
		}

		Sitting(1968);
		string majorId = desk.Lien.FloriaMajorId;
		Check(chart.GetLabelById(majorId)?.tier == LabelTier.Major && desk.Lien.FloriaOpeningShare > 0f,
			"Floria is tied to an actual regional Major at founding");
		desk.RestoreLienForChecks(desk.CaptureLienForChecks());
		Check(desk.Lien.FloriaMajorId == majorId && desk.Lien.FloriaOpeningShare > 0f, "The Major and its opening share survive a save");
		Check(desk.OpenLienSitting(out var message) && !desk.ActiveSitting.IsFloriaBuyout, "Weak Show puts the 1968 purchase off for a year: " + message);
		var writeOn = desk.LienSittingOptions(desk.ActiveSitting).Single(o => o.verb == LienVerb.WriteItOn);
		Check(desk.ChooseLienOption(desk.ActiveSitting, writeOn, out _), "The 1968 sitting can still be extended on ordinary terms");
		desk.EnforceLienForChecks(new GameDate(1969, 1, 1));
		desk.CloseLienSitting();
		clock.RestoreClock(new GameDate(1969, 12, 20), 9);
		Check(desk.OpenLienSitting(out _) && desk.ActiveSitting.IsFloriaBuyout, "Weak Show cannot postpone the offer past 1969");

		Sitting(1966);
		desk.Lien.FloriaMajorId = "major-no-longer-in-business";
		desk.Label.cashReserves = 100000f;
		Check(desk.OpenLienSitting(out _) && desk.Lien.FloriaDismissed && !desk.LienRefusesPay,
			"When the Major is gone, Floria's successor accepts an ordinary payment");
		desk.RestoreLienForChecks(desk.CaptureLienForChecks());
		Check(desk.Lien.FloriaDismissed && !desk.LienRefusesPay, "The successor terms survive a save");

		var act = ArtistManager.Instance.GetUnsignedArtists().OrderBy(a => a.artistId, StringComparer.Ordinal).First();
		desk.Label.SignArtist(act, 1968, desk.Label.GenerateTermSheet(act, 1968));
		ArtistManager.Instance.SignArtist(act, desk.Label.labelId, 1968);
		var record = new Record {
			recordId = "floria-valuation-probe", title = "Floria Probe", artistId = act.artistId, artistName = act.stageName,
			labelId = desk.Label.labelId, isPlayerOwned = true, primaryGenre = act.primaryGenre, secondaryGenre = act.primaryGenre,
			releaseDate = new GameDate(1968, 7, 1),
		};
		chart.ReleaseRecord(record);
		var runtime = chart.GetRecordRuntimeData(record.recordId);
		runtime.totalUnitsSold = 5_000_000;
		runtime.lifetimeLabelNet = FloriaValuationBar / 2;
		var regions = chart.GetAllRegions().Select(r => r.regionId).Take(4).ToArray();
		foreach (string region in regions)
			runtime.regionalData[region] = new RegionalRecordData { unitsSoldTotal = 10_000 };

		Sitting(1968);
		Check(desk.OpenLienSitting(out _) && desk.Lien.FloriaBuyoutWaived && !desk.ActiveSitting.IsFloriaBuyout && desk.LienShow().Met,
			"A national catalogue at the valuation bar makes the Major waive its option");
		desk.RestoreLienForChecks(desk.CaptureLienForChecks());
		Check(desk.Lien.FloriaBuyoutWaived && desk.LienRefusesPay, "The waiver survives a save with the lien still in force");

		Sitting(1968);
		foreach (string region in regions.Skip(1)) runtime.regionalData[region].unitsSoldTotal = 0;
		desk.Label.top40Hits = 1;
		foreach (var buyer in chart.GetAllLabels().Where(label => label.tier == LabelTier.Major)) buyer.cashReserves = FloriaValuationBar * 2;
		Check(desk.OpenLienSitting(out _) && desk.ActiveSitting.IsFloriaBuyout && !desk.Lien.FloriaBuyoutWaived,
			"Even an expensive regional catalogue faces the 1968 purchase after a strong Show");
		var live = desk.ActiveSitting;
		var offers = desk.LienSittingOptions(live);
		var major = live.FloriaForced;
		var rival = live.FloriaRival;
		Check(rival != null && rival.Price > major.Price && offers.All(o => o.verb is LienVerb.FloriaSign or LienVerb.FloriaRivalBid or LienVerb.WalkOut),
			"A funded rival can outbid the Major and the sitting offers the two purchases");

		var bid = forced ? major : rival;
		float buyerCash = bid.Buyer.cashReserves;
		if (forced) {
			var walk = offers.Single(o => o.verb == LienVerb.WalkOut);
			Check(desk.ChooseLienOption(live, walk, out _) && !desk.IsGameOver, "Walking out leaves the purchase for the deadline");
			desk.EnforceLienForChecks(new GameDate(1969, 1, 1));
		} else {
			Check(desk.ChooseLienOption(live, offers.Single(o => o.verb == LienVerb.FloriaRivalBid), out _), "The rival sale closes at the sitting");
		}
		Check(desk.IsGameOver && desk.Lien.Ending == (forced ? "Taken" : "SoldToMajor") &&
			desk.Label.ownerLabelId == bid.Buyer.labelId && desk.Label.IsActive && desk.Lien.Outstanding == 0f &&
			Near(bid.Buyer.cashReserves, buyerCash - bid.Price, 1f), "The purchase settles the lien and transfers ownership without closing the imprint");
	}
}
