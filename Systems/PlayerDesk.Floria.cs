using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class PlayerDesk {
	public const float FloriaValuationBar = 1_250_000f;
	private const int FloriaCatalogueWeeks = 52;
	private const float FloriaDismissalShare = .25f;
	private const float FloriaShowMultiple = 1.5f;

	internal sealed record FloriaBid(AILabel Buyer, float Price);

	private bool FloriaHasBuyoutClause => lien?.Holder == LienHolder.Floria && !lien.FloriaDismissed && !lien.FloriaBuyoutWaived;
	public bool FloriaPurchaseOptionPending => FloriaHasBuyoutClause && lien.Deadline.month == 1 && FloriaOfferYear();

	private static string FloriaOwnerId(AILabel label, IReadOnlyDictionary<string, AILabel> labels) {
		for (int depth = 0; depth < labels.Count && label?.ownerLabelId is { } parent && labels.TryGetValue(parent, out var owner) && owner != label; depth++)
			label = owner;
		return label?.labelId;
	}

	private (List<AILabel> Majors, Dictionary<string, long> Sales, long Total) FloriaMajorMarket() {
		var chart = ChartManager.Instance;
		var labels = (chart?.GetAllLabels() ?? new()).ToDictionary(label => label.labelId, StringComparer.Ordinal);
		var majors = labels.Values.Where(label => label.IsActive && !label.IsSubsidiary && label.tier == LabelTier.Major && !label.isPlayerOwned).ToList();
		var sales = new Dictionary<string, long>(StringComparer.Ordinal);
		string region = DistanceModel.GetCityById(Label?.homeCityId)?.parentRegionId;
		long total = 0;
		if (chart == null || region == null) return (majors, sales, total);
		var owners = labels.Values.ToDictionary(label => label.labelId, label => FloriaOwnerId(label, labels), StringComparer.Ordinal);
		foreach (var record in chart.GetAllRecords()) {
			if (record.weeksSinceRelease >= FloriaCatalogueWeeks || record.baseRecord?.labelId == null ||
				!record.regionalData.TryGetValue(region, out var local)) continue;
			int units = Math.Max(0, local.unitsSoldTotal);
			total += units;
			string owner = owners.GetValueOrDefault(record.baseRecord.labelId);
			if (owner != null) sales[owner] = sales.GetValueOrDefault(owner) + units;
		}
		return (majors, sales, total);
	}

	private static float FloriaShare(string id, Dictionary<string, long> sales, long total) =>
		id != null && total > 0 ? (float)(sales.GetValueOrDefault(id) / (double)total) : 0f;

	private void BindFloriaMajor() {
		if (lien?.Holder != LienHolder.Floria || lien.FloriaMajorId != null) return;
		var (majors, sales, total) = FloriaMajorMarket();
		var major = majors.OrderByDescending(label => sales.GetValueOrDefault(label.labelId))
			.ThenBy(label => label.labelId, StringComparer.Ordinal).FirstOrDefault();
		lien.FloriaMajorId = major?.labelId;
		lien.FloriaOpeningShare = FloriaShare(major?.labelId, sales, total);
	}

	public (float Value, int Regions) FloriaValuation() {
		var catalogue = ReleasedRecords.Where(record => record.weeksSinceRelease < FloriaCatalogueWeeks &&
			!IsMasterOut(record.baseRecord.recordId)).ToList();
		float value = R2(2f * Math.Max(0f, catalogue.Sum(record => record.lifetimeLabelNet)));
		var regionalSales = catalogue.SelectMany(record => record.regionalData)
			.GroupBy(entry => entry.Key, StringComparer.Ordinal)
			.Select(group => group.Sum(entry => (long)Math.Max(0, entry.Value.unitsSoldTotal))).ToList();
		long total = regionalSales.Sum();
		int regions = regionalSales.Count(units => units >= 1000 && units >= .05 * total);
		return (value, regions);
	}

	private bool FloriaSittingSeason(GameDate date) => FloriaHasBuyoutClause && !lien.Extended &&
		lien.Deadline.month == 1 && date >= AddDays(lien.Deadline, -LienWindowDays) && lien.BarYear >= 1966;

	private bool FloriaOfferYear() => lien.BarYear >= 1969 || (lien.BarYear == 1968 && LienShow().Met);

	private string ReviewFloriaPosition(GameDate date, out ExecutiveVoice voice) {
		voice = ExecutiveVoice.None;
		if (!FloriaSittingSeason(date)) return null;
		BindFloriaMajor();
		var (majors, sales, total) = FloriaMajorMarket();
		var major = majors.FirstOrDefault(label => label.labelId == lien.FloriaMajorId);
		float share = FloriaShare(lien.FloriaMajorId, sales, total);
		if (major == null || (total > 0 && lien.FloriaOpeningShare > 0f && share < lien.FloriaOpeningShare * FloriaDismissalShare)) {
			lien.FloriaDismissed = true;
			string line = "Floria is out. His successor found the paper in a drawer and will take an ordinary payment.";
			lien.Add(date, LienLedgerKind.FloriaDismissed, 0f, major?.labelName);
			Note(line);
			return line;
		}
		if (lien.BarYear >= 1968) {
			var (value, regions) = FloriaValuation();
			if (value >= FloriaValuationBar && regions >= 4) {
				lien.FloriaBuyoutWaived = true;
				string line = $"{major.labelName} passed. At ${value:N0} with business in {regions} regions, you cost more than Floria told them. The buyout clause is off the table.";
				lien.Add(date, LienLedgerKind.FloriaBuyoutWaived, value, major.labelName);
				Note(line);
				return line;
			}
			if (!FloriaOfferYear()) return "New York wants another year of figures before it makes an offer. The ordinary bars still apply this year.";
		}
		if (InstinctProfile.TheStreet >= 3 && lien.FloriaOpeningShare > 0f && share < lien.FloriaOpeningShare * .375f) {
			voice = ExecutiveVoice.Street;
			return $"{major.labelName}'s share here is slipping. Floria's own job may be on the paper soon.";
		}
		return null;
	}

	private (FloriaBid Forced, FloriaBid Rival) FloriaBids(GameDate date) {
		if (!FloriaSittingSeason(date) || !FloriaOfferYear()) return (null, null);
		var chart = ChartManager.Instance;
		var manager = CompetitorManager.Instance;
		var major = chart?.GetLabelById(lien.FloriaMajorId);
		if (major == null || !major.IsActive || manager == null || Label.IsSubsidiary) return (null, null);
		var (value, _) = FloriaValuation();
		float debt = R2(lien.Outstanding + lien.InterestDueBy(date));
		float Budget(AILabel candidate) => Math.Max(0f, manager.AvailableDistributionFunding(candidate));
		var forced = new FloriaBid(major, R2(Math.Min(value * .65f, Budget(major))));
		if (Label.top40Hits <= 0) return (forced, null);
		var rival = chart.GetAllLabels()
			.Where(candidate => candidate != major && candidate != Label && candidate.IsActive && !candidate.IsSubsidiary &&
				candidate.tier is LabelTier.Major or LabelTier.MidTier)
			.Select(candidate => new FloriaBid(candidate, R2(Math.Min(value * (candidate.tier == LabelTier.Major ? .95f : .85f), Budget(candidate)))))
			.Where(bid => bid.Price >= debt && bid.Price > forced.Price)
			.OrderByDescending(bid => bid.Price).ThenBy(bid => bid.Buyer.labelId, StringComparer.Ordinal).FirstOrDefault();
		return (forced, rival);
	}

	private LienOption FloriaSaleOption(FloriaBid bid, bool rival, GameDate date) {
		float remaining = R2(Math.Max(0f, bid.Price - lien.Outstanding - lien.InterestDueBy(date)));
		return new LienOption {
			verb = rival ? LienVerb.FloriaRivalBid : LienVerb.FloriaSign,
			label = rival ? $"Accept {bid.Buyer.labelName}'s bid" : $"Sign with {bid.Buyer.labelName}",
			subLabel = $"${bid.Price:N0} for the label. ${remaining:N0} after the lien. The label becomes a subsidiary and the run ends.",
			cash = bid.Price,
		};
	}

	private void CompleteFloriaSale(FloriaBid bid, bool rival, GameDate date, LienSittingLive live = null) {
		lien.AccrueTo(date);
		float balance = R2(lien.Outstanding + lien.InterestDue);
		float proceeds = R2(Math.Max(0f, bid.Price - balance));
		LabelCompanyAccounts.ChangeCash(bid.Buyer, -bid.Price, LabelCashKind.Financing);
		LabelCompanyAccounts.ChangeCash(Label, proceeds, LabelCashKind.Financing);
		if (Label.activeDeal is { } deal)
			CompetitorManager.Instance.SettleEndedDistributionAdvance(Label, deal, deal.distributorId == bid.Buyer.labelId ? DealResolution.Absorb : DealResolution.Exit);
		CompetitorManager.ApplySubsidiaryAbsorption(Label, bid.Buyer);
		LabelLifecycleManager.Instance?.MarkLabelSubsidiary(Label, bid.Buyer);
		lien.Add(date, LienLedgerKind.FloriaBuyout, bid.Price, bid.Buyer.labelName);
		lien.InterestPaid = lien.InterestAccrued;
		lien.PrincipalPaid = lien.FaceSum;
		lien.Ending = rival ? "SoldToMajor" : "Taken";
		GameOverTitle = rival ? "SOLD TO A MAJOR" : "TAKEN";
		string line = rival
			? $"{bid.Buyer.labelName} outbid Floria and settled the paper. You walk away with ${proceeds:N0}, and {Label.labelName} goes on as its subsidiary."
			: $"{bid.Buyer.labelName} enforced the buyout. The paper is settled and {Label.labelName} goes on as its subsidiary. You get ${proceeds:N0} after the lien.";
		if (live != null) { live.Say(line, LienProfile.Lawyer); live.stage = CallStage.Ended; }
		GameOver(line, bankrupt: false);
	}
}
