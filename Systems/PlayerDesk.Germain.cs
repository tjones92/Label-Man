using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// JACK GERMAIN'S CONCEDES (SimTools/LabelManInnerLifeDirective.md §3.5 H5, Phase 1b).
//
// The payola jock takes two things instead of money, and both are his: his name on a song or two, and his boys on your record.
//
// HIS NAME. For the next two originals an act writes, Jack Germain is credited with a quarter of the writer's share. He is a third
// party: the credit is an outsider's (MechanicalRoyaltyService.OutsiderWriterPrefix), carved out of the writing team's credit like the
// label's own cut-in, but the money is his, not the label's. Every mechanical, plug-side performance and AI-label cut of a song that
// carries his name pays him that quarter of the writer's half, out of the writer's pocket. The label's outlay is unchanged. The real
// writer is never happy about it, whatever the Label Man's ear (author decision): each credited writer's trust drops and the band
// feels the outsider, every time.
//
// HIS BOYS. Jack's network is a real IndiePromoter: every reporter station in the home region, strong, cheap and indiscreet, with a
// high mob connection. It works. It works through the same PayolaLedger path as a project promo man, so the ledger's own detection
// and scandal math supplies the risk. Each year he is used the next push is less discreet, which is the cost of an ally who eases
// off as you lean on him.
//
// Player-only and AI-inert: no AI record has an outsider credit, the ledger holds nothing until the player places something, and
// every hook is null in a headless run.
// ============================================================================================
public partial class PlayerDesk {
	public const string GermainWriterId = MechanicalRoyaltyService.OutsiderWriterPrefix + "germain";
	public const string GermainName = "Jack Germain";
	public const string GermainPromoterId = "germain-network";
	// Numbers measured by SimTools/PayolaMeasureProbe (SimLogs/payola-measure-1001.txt; directive §3.5 H5 "Measured"):
	//  - the quarter matches the label's own cut-in, and is below the third Alan Freed took on "Maybellene". At the statutory 2c it is
	//    a quarter-cent a copy ($25 per 10,000), so the cost of his name is the writers' trust, not the money;
	//  - his boys are half the home region's reporters (every region has 11: six), eight weeks, at his network's effectiveness;
	//  - the fee prices that push at half a local promo man's rate per boost-week (the measured Project Promo L rate is ~$57), so he is
	//    the cheapest airplay in the game without being free;
	//  - discretion starts careful (he has just been fired) and falls fast, so a first push in the 1960 crackdown is caught about one
	//    time in seven and the fourth about one in four; used every year, he is caught at least once by about 1964 half the time.
	/// <summary>The share of the writer credit on each song his name goes on (the same as the label's own cut-in).</summary>
	public const float GermainCutInShare = 0.25f;
	/// <summary>"A song or two": the originals his name goes on, per Concede.</summary>
	public const int GermainCutInSongs = 2;
	/// <summary>Trust lost with each credited writer when his name goes on their song (the label's own cut-in costs 0.08).</summary>
	public const float GermainTrustHit = 0.12f;
	public const float GermainStrain = 0.08f;
	public const int GermainNetworkWeeks = 8;
	/// <summary>His old jocks: half the home region's reporters, rounded up, and never more than six.</summary>
	public const int GermainNetworkMaxStations = 6;
	public const float GermainNetworkEffectiveness = 0.8f;
	public const float GermainNetworkMob = 0.7f;
	/// <summary>How discreet his boys are on the first push, how much each year of use costs, and the least they ever are.</summary>
	public const float GermainNetworkDiscretion = 0.55f, GermainDiscretionLostPerUse = 0.12f, GermainDiscretionFloor = 0.05f;
	/// <summary>His "consulting fee", booked as such. Cheap, which is how he gets in.</summary>
	public const float GermainNetworkFee = 300f;
	/// <summary>The payola trail each song with his name on it leaves on the label (a known payola jock credited as a writer is
	/// exactly what the 1960 hearings went looking for), and what each consulting fee on the books adds.</summary>
	public const float GermainNameExposure = 0.06f, GermainFeeExposure = 0.05f;
	/// <summary>The newest single he will push, in weeks since release.</summary>
	public const int GermainNetworkRecordWeeks = 52;

	// ── his name ──────────────────────────────────────────────────────────────────────────────────────

	private static bool WritesItsOwn(SimulatedArtist a) => a?.GetWriters().Any(w => w.Primary) == true;

	/// <summary>Acts on the roster who write: a name can only go on a song somebody in the act wrote.</summary>
	public int GermainWritingActs => Label?.roster?.Count(WritesItsOwn) ?? 0;

	/// <summary>Voice and what the next song will carry, for the Concede card.</summary>
	public float GermainDiscretionNow => Mathf.Max(GermainDiscretionFloor, GermainNetworkDiscretion - GermainDiscretionLostPerUse * (lien?.NetworkUses ?? 0));

	/// <summary>Put his name on the credit the desk is about to write, when he is owed one and the act has someone who writes.</summary>
	private void StageGermainCredit(SimulatedArtist a, CowritingService.TeamOverride o) {
		if (lien == null || lien.Holder != LienHolder.Germain || lien.CutInSongsLeft <= 0 || a == null || o == null || !WritesItsOwn(a)) return;
		o.OutsiderShare = GermainCutInShare;
		o.OutsiderId = GermainWriterId;
		o.OutsiderName = GermainName;
	}

	/// <summary>The credit went on. A song whose credit fell back to the act's house name had no writer for it to come out of, so
	/// nothing went on and he is still owed.</summary>
	private void SettleGermainCredit(SimulatedArtist a, CowritingService.TeamOverride o) {
		if (lien == null || o == null || o.OutsiderShare <= 0f || o.CreditedPersonIds.Count == 0) return;
		lien.CutInSongsLeft = Math.Max(0, lien.CutInSongsLeft - 1);
		lien.CutInSongs++;
		ChartManager.Instance?.AddPayolaExposure(Label?.labelId, GermainNameExposure);
		var writers = a.members.Where(m => m.isActive && o.CreditedPersonIds.Contains(m.personId)).ToList();
		foreach (Musician m in writers) {
			AddTrust(m, -GermainTrustHit);
			Musician other = a.members.FirstOrDefault(x => x != m && x.isActive);
			MemberRelation edge = BandLifeService.WorstEdgeOf(a, m) ?? (other != null ? BandLifeService.Edge(a, m, other) : null);
			if (edge == null) continue;
			BandLifeService.AddStrain(edge, StrainCause.Outsider, GermainStrain, YearNow);
			BandLifeService.AddStrain(edge, StrainCause.CreditAndMoney, GermainStrain, YearNow);
		}
		string who = writers.Count == 0 ? a.stageName : string.Join(" and ", writers.Select(w => w.firstName ?? w.FullName));
		Note($"{GermainName}'s name went on {a.stageName}'s new original, a quarter of the writing. {who} {(writers.Count > 1 ? "were" : "was")} not pleased." +
			(lien.CutInSongsLeft > 0 ? $" He is owed {lien.CutInSongsLeft} more." : ""));
	}

	/// <summary>Money paid to Jack out of a song's writer money, from any of the chains that carry his name (the books' ledger of him).</summary>
	private void OnOutsiderCutInPaid(float amount, int units) {
		if (lien == null || lien.Holder != LienHolder.Germain || amount <= 0f) return;
		lien.CutInPaid = R2(lien.CutInPaid + amount);
	}

	// ── his boys ──────────────────────────────────────────────────────────────────────────────────────

	/// <summary>The newest single of yours that is still new enough to be worth pushing, or null.</summary>
	public RecordRuntimeData GermainPushRecord() =>
		ReleasedRecords.Where(r => r.baseRecord != null && r.baseRecord.format == ReleaseFormat.Single && r.weeksSinceRelease <= GermainNetworkRecordWeeks)
			.OrderBy(r => r.weeksSinceRelease).ThenBy(r => r.baseRecord.recordId, StringComparer.Ordinal).FirstOrDefault();

	/// <summary>The reporter stations his boys work: half the home region's, in id order, up to six.</summary>
	public List<RadioStation> GermainStations() {
		var city = Label == null ? null : DistanceModel.GetCityById(Label.homeCityId);
		if (city == null) return new();
		var all = (ChartManager.Instance?.ReporterStationsInRegion(city.parentRegionId) ?? Array.Empty<RadioStation>()).Where(s => s != null).ToList();
		return all.OrderBy(s => s.stationId, StringComparer.Ordinal).Take(GermainNetworkStationCount(all.Count)).ToList();
	}

	public static int GermainNetworkStationCount(int regionReporters) => Math.Min(GermainNetworkMaxStations, (regionReporters + 1) / 2);

	/// <summary>The Concede options only Jack offers: his name, and his boys.</summary>
	private List<LienOption> GermainConcedeOptions() {
		var list = new List<LienOption>();
		if (lien == null || lien.Holder != LienHolder.Germain || Label == null) return list;

		int writers = GermainWritingActs;
		bool owed = lien.CutInSongsLeft > 0;
		list.Add(new LienOption {
			verb = LienVerb.ConcedeCutIn, label = "Put Jack's name on a song or two",
			subLabel = $"{GermainName} is credited with {GermainCutInShare:P0} of the writing on your acts' next {GermainCutInSongs} originals, and paid it. The writers lose trust in you each time.",
			enabled = !owed && writers > 0,
			disabledReason = owed ? $"He's still waiting on {Plural.Of(lien.CutInSongsLeft, "song")} from the last time."
				: "None of your acts writes its own songs. There's nothing to put his name on.",
		});

		var record = GermainPushRecord();
		var stations = GermainStations();
		float cash = Mathf.Max(0f, Label.cashReserves);
		string reason = record == null ? "You haven't a single out for his boys to push."
			: stations.Count == 0 ? "He has no stations in your home region to call."
			: $"His consulting fee is ${GermainNetworkFee:N0}. You have ${cash:N0}.";
		list.Add(new LienOption {
			verb = LienVerb.ConcedeNetwork, label = record == null ? "Send Jack's boys out" : $"Send Jack's boys out on \"{TitleForRecord(record.baseRecord.recordId)}\"",
			subLabel = $"{Plural.Of(stations.Count, "station")} in your home region for {GermainNetworkWeeks} weeks, for a ${GermainNetworkFee:N0} consulting fee. It works. It can get you caught, and he's more careless every year you use him.",
			enabled = record != null && stations.Count > 0 && cash >= GermainNetworkFee, disabledReason = reason,
		});
		return list;
	}

	/// <summary>Concede: his name. The voices speak only about what is true: the writing share, and who writes.</summary>
	private void GiveGermainHisName(LienSittingLive live, GameDate today) {
		lien.CutInSongsLeft = GermainCutInSongs;
		lien.Add(today, LienLedgerKind.GaveName, 0f, $"{GermainCutInSongs} songs");
		var inst = InstinctProfile;
		if (inst.TheEar >= 3) live.Say("A man who's never written a bar, on their song. They'll see it on the label copy and know what you are.", "", ExecutiveVoice.Ear);
		if (inst.TheSuit >= 3) live.Say($"A quarter of the writer's half, on {GermainCutInSongs} songs. It's not the money. It's that the writer is paying for your year.", "", ExecutiveVoice.Suit);
		Note($"{GermainName} will be credited on {Plural.Of(GermainCutInSongs, "original")}: {GermainCutInShare:P0} of the writing, paid to him.");
	}

	/// <summary>Concede: his boys on your newest single. False (with a reason) when there is nothing to push or no cash.</summary>
	private bool SendGermainsBoys(LienSittingLive live, GameDate today, out string message) {
		message = null;
		var record = GermainPushRecord();
		var stations = GermainStations();
		if (record == null) { message = "You haven't a single out for his boys to push."; return false; }
		if (stations.Count == 0) { message = "He has no stations in your home region to call."; return false; }
		if (Label.cashReserves < GermainNetworkFee) { message = $"His consulting fee is ${GermainNetworkFee:N0}."; return false; }
		float discretion = GermainDiscretionNow;
		var placed = ChartManager.Instance?.PlacePromoterNetwork(GermainPromoterId, GermainName + "'s boys", record.baseRecord.recordId, Label.labelId,
			stations.Select(s => s.stationId).ToArray(), GermainNetworkEffectiveness, discretion, GermainNetworkMob, GermainNetworkWeeks, GermainNetworkFee);
		if (placed == null || placed.Count == 0) { message = "His boys couldn't get anyone to pick up the phone."; return false; }
		Label.cashReserves -= GermainNetworkFee;
		Label.monthlyExpenses += GermainNetworkFee;
		ChartManager.Instance?.AddPayolaExposure(Label.labelId, GermainFeeExposure);
		lien.NetworkUses++;
		string title = TitleForRecord(record.baseRecord.recordId);
		lien.Add(today, LienLedgerKind.SentBoys, GermainNetworkFee, title);
		var inst = InstinctProfile;
		if (inst.TheStreet >= 3) live.Say($"{Plural.Of(placed.Count, "station")} will take his call. Every one took his money first.", "", ExecutiveVoice.Street);
		if (inst.TheFixer >= 3) live.Say(discretion <= GermainDiscretionFloor + 0.001f
			? "He's not even trying to hide it any more. Somebody's going to notice."
			: "It'll work. It always works with Jack. That's how he got caught the first time.", "", ExecutiveVoice.Fixer);
		Note($"{GermainName}'s boys are working \"{title}\" on {Plural.Of(placed.Count, "station")} for {GermainNetworkWeeks} weeks. ${GermainNetworkFee:N0} goes on the books as consulting.");
		return true;
	}
}
