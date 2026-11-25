using System;
using System.Linq;
using Godot;
using static PlayerDesk;

/// <summary>
/// Inner-life Phase 1b (§3.5 H5): Jack Germain's two Concedes. His name goes on the next two originals an act writes, as a third-party
/// credit carved out of the writing team's (a quarter), and he is paid that quarter through every chain that carries the writer's money
/// while the label's outlay stays the same; each credited writer's trust in the label falls; a song that has no writer to take it from
/// carries no name and leaves him owed. His boys work the newest single over the home region's reporters for a consulting fee, and are
/// less discreet each year he is used. Nobody else has either option, the state survives a save, and a record with no outsider credit
/// moves nothing.
///   --path . SimTools/GermainChecks.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// Prints GERMAIN_CHECKS_PASS / _FAIL.
/// </summary>
public partial class GermainChecks : Node {
	private int checks;
	private void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; GD.Print("ok: " + why); }
	private static bool Near(float a, float b, float tol = .01f) => Math.Abs(a - b) <= tol;

	private static readonly PartyMemory GermainNight = new() { Where = PartyWhere.DjHouseParty, Who = PartyWho.DiscJockey, Brag = PartyBrag.TheAct, End = PartyEnd.Car };

	public override void _Ready() {
		var switches = BandLife.CaptureSwitches();
		try {
			BandLife.ConfigureForProbe(cowritingOn: true);
			var desk = PlayerDesk.Instance;
			var clock = TimeManager.Instance;
			clock.RestoreClock(new GameDate(1960, 1, 1), 9);
			Check(desk.FoundLabel("Germain Checks", "detroit", FoundingArchetype.ExMusician, null, GermainNight, out var message), "Found: " + message);
			desk.FoundLienForChecks(GermainNight, LienHolder.Germain);
			desk.Label.cashReserves = 5000f;
			desk.Lien.BarYear = 1961;

			Barred(desk);
			var act = SignWriters(desk, clock);
			CutInName(desk, act);
			Money(desk, act);
			Boys(desk, clock, act);
			Others(desk, clock);
			Save(desk);
			BlackLedger(desk, clock);
			GD.Print($"GERMAIN_CHECKS_PASS ({checks} checks)");
			BandLife.RestoreSwitches(switches);
			GetTree().Quit(0);
		} catch (Exception e) {
			BandLife.RestoreSwitches(switches);
			GD.PrintErr("GERMAIN_CHECKS_FAIL: " + e.Message);
			GetTree().Quit(1);
		}
	}

	private static LienOption Opt(PlayerDesk desk, LienVerb verb) => desk.LienSittingOptions(new LienSittingLive()).FirstOrDefault(o => o.verb == verb);

	private void Barred(PlayerDesk desk) {
		var name = Opt(desk, LienVerb.ConcedeCutIn);
		var boys = Opt(desk, LienVerb.ConcedeNetwork);
		Check(name != null && boys != null, "Jack offers both: his name, and his boys");
		Check(!name.enabled && name.disabledReason.Contains("writes its own"), "With no act that writes, there's nothing to put his name on: " + name.disabledReason);
		Check(!boys.enabled && boys.disabledReason.Contains("single out"), "With nothing out, his boys have nothing to push: " + boys.disabledReason);
		Check(desk.GermainPushRecord() == null && desk.GermainWritingActs == 0, "Neither a single to push nor an act that writes");
	}

	private SimulatedArtist SignWriters(PlayerDesk desk, TimeManager clock) {
		int year = clock.CurrentDate.year;
		var act = ArtistManager.Instance.GetUnsignedArtists().Where(a => a.type == ArtistType.Band && a.members.Count(m => m.isActive) >= 3)
			.OrderBy(a => a.artistId, StringComparer.Ordinal).First();
		desk.Label.SignArtist(act, year, desk.Label.GenerateTermSheet(act, year));
		ArtistManager.Instance.SignArtist(act, desk.Label.labelId, year);
		foreach (var m in act.members.Where(m => m.isActive).Take(2)) m.isPrimaryWriter = true;
		Check(act.isPlayerOwned && desk.GermainWritingActs == 1, "A band with two writers is on the roster");
		return act;
	}

	private static SongComposition Original(SimulatedArtist act, string key) {
		var song = new SongComposition { songId = "germain-" + key };
		CowritingService.CreditOriginal(song, act, key, 1961);
		return song;
	}

	private void CutInName(PlayerDesk desk, SimulatedArtist act) {
		var lien = desk.Lien;
		var opt = Opt(desk, LienVerb.ConcedeCutIn);
		Check(opt.enabled, "Now he can have a name on a song");
		var live = new LienSittingLive();
		Check(desk.ChooseLienOption(live, opt, out _), "Gave him his name");
		Check(lien.CutInSongsLeft == GermainCutInSongs && lien.Extended && lien.Ledger.Any(e => e.Kind == LienLedgerKind.GaveName), $"He's owed {GermainCutInSongs} songs and the lien is extended");
		var again = Opt(desk, LienVerb.ConcedeCutIn);
		Check(!again.enabled && again.disabledReason.Contains("still waiting"), "He can't be given a second name while he's owed: " + again.disabledReason);

		float before = act.members.Where(m => m.isActive).Min(m => desk.TrustOf(m.personId));
		var first = Original(act, "one");
		var outsider = first.credits.Where(c => c.writerId == GermainWriterId).ToList();
		Check(outsider.Count == 1 && Near(outsider[0].share, GermainCutInShare, .0001f) && outsider[0].writerName == GermainName,
			$"His name is on the credit at {GermainCutInShare:P0}");
		var members = first.credits.Where(c => c.isArtistMember).ToList();
		Check(members.Count > 0 && Near(members.Sum(c => c.share), 1f - GermainCutInShare, .0001f) && Near(first.credits.Sum(c => c.share), 1f, .0001f),
			$"The writers' share is carved down to {1f - GermainCutInShare:P0} and the credit still sums to one");
		Check(lien.CutInSongsLeft == GermainCutInSongs - 1 && lien.CutInSongs == 1, "One song down, one owed");
		var credited = act.members.Where(m => members.Any(c => c.writerId == m.personId)).ToList();
		Check(credited.Count > 0 && credited.All(m => Near(desk.TrustOf(m.personId), PlayerDesk.DefaultTrust - GermainTrustHit, .0001f)),
			$"Every writer who lost a share trusts the label {GermainTrustHit:P0} less: {string.Join(", ", credited.Select(m => desk.TrustOf(m.personId).ToString("F2")))}");

		var second = Original(act, "two");
		Check(second.credits.Any(c => c.writerId == GermainWriterId) && lien.CutInSongsLeft == 0 && lien.CutInSongs == 2, "The second song has his name and settles the debt");
		var third = Original(act, "three");
		Check(third.credits.All(c => c.writerId != GermainWriterId) && lien.CutInSongs == 2, "The third song is the writers' own");

		var rec = new Record();
		CompositionCatalogService.ApplyToRecord(rec, first, SongMaterialSource.ArtistWritten, false, null, null, 0f, 0f, 0f);
		Check(Near(MechanicalRoyaltyService.OutsiderCutInShare(rec), GermainCutInShare, .0001f) && Near(MechanicalRoyaltyService.LabelCutInShare(rec, desk.Label), 0f, .0001f),
			"On the record he is an outsider's share, not the label's");
		Check(MechanicalRoyaltyService.OutsiderCutInShare(new Record()) == 0f, "A record nobody cut in is zero");
	}

	private void Money(PlayerDesk desk, SimulatedArtist act) {
		var lien = desk.Lien;
		lien.CutInPaid = 0f;
		float Charge(float labelCut, float outsiderCut, out float writer) {
			float w0 = act.writerRoyaltyEarnings;
			float expense = MechanicalRoyaltyService.ChargeSide(PublishingControlType.LabelAffiliate, desk.Label.labelId, null, act, desk.Label, 1000,
				id => CompetitorManager.Instance?.GetLabel(id), id => ArtistManager.Instance?.GetArtist(id), labelCut, outsiderCut);
			writer = act.writerRoyaltyEarnings - w0;
			return expense;
		}
		float plain = Charge(0f, 0f, out float plainWriter);
		Check(plain > 0f && plainWriter > 0f && lien.CutInPaid == 0f, $"A plain 1,000 copies costs ${plain:N2}, ${plainWriter:N2} of it the writer's, and pays Jack nothing");
		float named = Charge(0f, GermainCutInShare, out float namedWriter);
		Check(Near(named, plain, .0001f), $"With his name on it the label still pays ${named:N2}: the cost doesn't change, only who is paid");
		Check(Near(namedWriter, plainWriter * (1f - GermainCutInShare), .0001f), $"The writer's half drops a quarter, ${plainWriter:N2} to ${namedWriter:N2}");
		Check(Near(lien.CutInPaid, plainWriter * GermainCutInShare, .0001f), $"Jack is paid the quarter: ${lien.CutInPaid:N2}");
		float kept = Charge(GermainCutInShare, 0f, out _);
		Check(kept < plain && lien.CutInPaid > 0f, "The label's own cut-in is still the label keeping it (it costs less), and pays him nothing");
		float paid = lien.CutInPaid;
		Charge(GermainCutInShare, 0f, out _);
		Check(Near(lien.CutInPaid, paid, .0001f), "...and nothing more reaches Jack from it");
		var other = LienHolder.Kubiak;
		desk.FoundLienForChecks(GermainNight, other);
		Charge(0f, GermainCutInShare, out _);
		Check(desk.Lien.CutInPaid == 0f, "A holder who isn't Jack keeps no books on his payments");
		desk.FoundLienForChecks(GermainNight, LienHolder.Germain);
		desk.Label.cashReserves = 5000f;
		desk.Lien.BarYear = 1961;
		desk.Lien.CutInPaid = paid;
	}

	private void Boys(PlayerDesk desk, TimeManager clock, SimulatedArtist act) {
		var lien = desk.Lien;
		var rec = new Record { recordId = "germain_probe", title = "Germain Probe", artistId = act.artistId, artistName = act.stageName,
			format = ReleaseFormat.Single, primaryGenre = act.primaryGenre, hookStrength = .5f, productionQuality = .5f, originality = .5f, danceability = .5f };
		Check(CompetitorManager.Instance.ReleasePlayerRecord(desk.Label, act, rec, 0f, 0f, clock.CurrentDate), "A single is out");
		Check(desk.GermainPushRecord()?.baseRecord.recordId == "germain_probe", "It's the one his boys would push");
		var stations = desk.GermainStations();
		Check(stations.Count > 0 && stations.Count <= GermainNetworkMaxStations, $"His network is the home region's reporters: {stations.Count}");
		Check(Near(desk.GermainDiscretionNow, GermainNetworkDiscretion, .0001f), "Fresh, his boys are as discreet as they get");

		desk.Label.cashReserves = GermainNetworkFee - 1f;
		var poor = Opt(desk, LienVerb.ConcedeNetwork);
		Check(!poor.enabled && poor.disabledReason.Contains("fee"), "Without his fee he doesn't send them: " + poor.disabledReason);
		desk.Label.cashReserves = 5000f;
		lien.Extended = false;
		var live = new LienSittingLive();
		float cash = desk.Label.cashReserves;
		var opt = Opt(desk, LienVerb.ConcedeNetwork);
		Check(opt.enabled && opt.label.Contains("Germain Probe"), "He names the record: " + opt.label);
		Check(desk.ChooseLienOption(live, opt, out _), "Sent his boys");
		Check(lien.NetworkUses == 1 && lien.Extended && Near(cash - desk.Label.cashReserves, GermainNetworkFee, .0001f)
			&& lien.Ledger.Any(e => e.Kind == LienLedgerKind.SentBoys && e.Detail == "Germain Probe"), "It cost his fee, it counts as a Concede, and the ledger remembers");
		Check(Near(desk.GermainDiscretionNow, GermainNetworkDiscretion - GermainDiscretionLostPerUse, .0001f), "The next push is less discreet");
		lien.NetworkUses = 20;
		Check(Near(desk.GermainDiscretionNow, GermainDiscretionFloor, .0001f), "But never below the floor");
		lien.NetworkUses = 1;
	}

	private void Others(PlayerDesk desk, TimeManager clock) {
		foreach (var h in Enum.GetValues<LienHolder>().Where(h => h != LienHolder.Germain)) {
			desk.FoundLienForChecks(GermainNight, h);
			var all = desk.LienSittingOptions(new LienSittingLive());
			Check(all.All(o => o.verb != LienVerb.ConcedeCutIn && o.verb != LienVerb.ConcedeNetwork), $"{h} has neither of Jack's Concedes");
		}
		desk.FoundLienForChecks(GermainNight, LienHolder.Germain);
	}

	private void Save(PlayerDesk desk) {
		var lien = desk.Lien;
		lien.CutInSongsLeft = 1; lien.CutInSongs = 3; lien.NetworkUses = 2; lien.CutInPaid = 12.34f;
		var data = desk.CaptureLienForChecks();
		desk.RestoreLienForChecks(data);
		Check(desk.Lien.CutInSongsLeft == 1 && desk.Lien.CutInSongs == 3 && desk.Lien.NetworkUses == 2 && Near(desk.Lien.CutInPaid, 12.34f, .0001f),
			"What he's owed, what he's been paid and how often he's been used survive a save");
		data.CutInSongsLeft = 0; data.CutInSongs = 0; data.NetworkUses = 0; data.CutInPaid = 0f;
		desk.RestoreLienForChecks(data);
		Check(desk.Lien.CutInSongsLeft == 0 && desk.Lien.NetworkUses == 0, "An older save without them owes him nothing");
	}

	private void BlackLedger(PlayerDesk desk, TimeManager clock) {
		clock.RestoreClock(GameDate.StartDate, 9);
		desk.FoundLienForChecks(GermainNight, LienHolder.Germain);
		Check(desk.GermainLedgerLead() == null, "Jack's station is hidden until the holder is identified");
		clock.RestoreClock(new GameDate(1960, 12, 20), 9);
		Check(desk.OpenLienSitting(out _), "Tolliver identifies Jack at the sitting");
		desk.CloseLienSitting();
		desk.RestoreLienForChecks(desk.CaptureLienForChecks());
		Check(desk.GermainLedgerLead() != null, "Jack stays identified after a sitting without an extension and after saving");
		var stops = DistanceModel.GetCities().SelectMany(city => desk.StopsInCity(city.cityId)).ToArray();
		var station = stops.Single(stop => desk.GermainLedgerStopVerb(stop.StopId) != null);
		Check(!desk.FollowGermainLedgerLead(station.StopId, out _) && desk.Lien.GermainLedger == GermainLedgerStage.Missing,
			"The records cannot leave the library before the seeded clearance");
		clock.RestoreClock(new GameDate(1964, 6, 1), 9);
		Check(desk.FollowGermainLedgerLead(station.StopId, out string clue), "The station names the record shop: " + clue);
		desk.RestoreLienForChecks(desk.CaptureLienForChecks());
		var shop = stops.Single(stop => desk.GermainLedgerStopVerb(stop.StopId) != null);
		Check(shop.Kind == StopKind.Shop && desk.FollowGermainLedgerLead(shop.StopId, out _) &&
			desk.Lien.GermainLedger == GermainLedgerStage.Found && !desk.GermainLedgerFrozen, "The saved clue leads to an unread ledger");
		Check(!desk.ReadGermainLedger(out var blocked) && blocked.Contains("Fixer"), "Possession alone cannot freeze Jack");
		desk.InstinctProfile.TheFixer = GermainLedgerFixer;
		desk.Lien.CutInSongsLeft = 2;
		float pay = desk.LienPayBar();
		var deadline = desk.Lien.Deadline;
		var stale = new LienOption { verb = LienVerb.ConcedeCutIn, label = "Offer his name" };
		Check(desk.ReadGermainLedger(out _) && desk.GermainLedgerFrozen && desk.Lien.CutInSongsLeft == 0,
			"Reading the ledger makes Jack drop his pending cut-ins");
		desk.RestoreLienForChecks(desk.CaptureLienForChecks());
		Check(desk.GermainLedgerFrozen && desk.Lien.Deadline == deadline && Near(desk.LienPayBar(), pay)
			&& Opt(desk, LienVerb.Pay) != null && Opt(desk, LienVerb.Show) != null,
			"The freeze survives a save without changing the deadline or payment");
		Check(Opt(desk, LienVerb.ConcedeCutIn) == null && Opt(desk, LienVerb.ConcedeNetwork) == null &&
			Opt(desk, LienVerb.ConcedeStock) == null && Opt(desk, LienVerb.WriteItOn) == null &&
			!desk.ChooseLienOption(new LienSittingLive(), stale, out _), "Tolliver offers no concessions and rejects a stale one");
	}
}
