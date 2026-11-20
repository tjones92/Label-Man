using System;
using System.Linq;
using Godot;

/// <summary>--gamey-audit-switches-check: the three audit switches are the identity when off and do what
/// SimTools/GameyBehaviorAudit.md says when on.</summary>
public static class GameyAuditSwitchChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Gamey audit switches: " + why); }
		bool Near(float a, float b) => Mathf.Abs(a - b) < 1e-5f;
		bool Rel(float a, float b) => Mathf.Abs(a - b) <= 1e-4f * Mathf.Max(1f, Mathf.Abs(b));
		float floorWas = LpEarnedDiscovery.FloorForChecks, shareWas = ReceivableBackedOverdraft.Share;
		bool staggerWas = StaggeredOpeningContracts.Enabled, launchWas = LpEarnedLaunch.Enabled, anniversaryWas = AnniversaryOpeningContracts.Enabled, parityWas = LpLabelDemandParity.Enabled;
		try {
			// LP earned discovery.
			LpEarnedDiscovery.SetForChecks(1f);
			Check(!LpEarnedDiscovery.Enabled && LpEarnedDiscovery.Factor(0f, 0f) == 1f, "off must leave the discovery term alone");
			LpEarnedDiscovery.SetForChecks(0.25f);
			Check(Near(LpEarnedDiscovery.Factor(0f, 0f), 0.25f), "an unknown act keeps the floor");
			Check(Near(LpEarnedDiscovery.Factor(1f, 0f), 1f) && Near(LpEarnedDiscovery.Factor(0f, LpEarnedDiscovery.KnownActBonus), 1f),
				"a hot act or a known act keeps the whole term");
			Check(Near(LpEarnedDiscovery.Factor(0f, LpEarnedDiscovery.KnownActBonus / 2f), 0.625f), "half known is half way");
			Check(LpEarnedDiscovery.Factor(0.2f, 0.03f) < LpEarnedDiscovery.Factor(0.4f, 0.03f), "more heat must mean more discovery");

			// LP earned launch and LP label demand parity.
			LpEarnedLaunch.SetForChecks(false);
			Check(LpEarnedLaunch.Base(0.04f, 0f) == 0.04f, "off must keep the launch base");
			LpEarnedLaunch.SetForChecks(true);
			Check(LpEarnedLaunch.Base(0.04f, 0f) == 0f && Near(LpEarnedLaunch.Base(0.04f, LpEarnedDiscovery.KnownActBonus), 0.04f), "an unknown act earns no base, a known act the whole base");
			LpLabelDemandParity.SetForChecks(false);
			Check(LpLabelDemandParity.Apply(0.5f) == 0.5f, "off must keep the tier scale");
			LpLabelDemandParity.SetForChecks(true);
			Check(Near(LpLabelDemandParity.Apply(0.5f), LabelFootprintParity.Demand(0.5f)), "on must follow the singles parity share");

			// Anniversary opening contracts.
			int[] sw = Enumerable.Range(1, 2000).Select(i => AnniversaryOpeningContracts.SigningWeek($"artist_{i:D5}")).ToArray();
			Check(sw.Min() == 0 && sw.Max() == 51 && sw.GroupBy(w => w / 13).All(g => g.Count() > 400 && g.Count() < 600), "signing weeks must spread over the year");
			foreach (string id in new[] { "artist_00001", "artist_00450", "artist_01999" }) {
				int w = AnniversaryOpeningContracts.SigningWeek(id);
				Check(AnniversaryOpeningContracts.ExpiryWeek(id, 1958, 3, 1960) == w - 104 + 156, "a live deal matures on its anniversary after its term");
				int lapsed = AnniversaryOpeningContracts.ExpiryWeek(id, 1955, 3, 1960);
				Check(lapsed >= 1 && lapsed <= 52 && (lapsed - w) % 52 == 0, "a lapsed deal comes due on its 1960 anniversary");
				int fresh = AnniversaryOpeningContracts.ExpiryWeek(id, 1960, 3, 1960);
				Check(fresh >= 105 && fresh <= 156 && (fresh - w) % 52 == 0, "a 1960 signing is dated in the year before the opening");
			}

			// LP buyer word of mouth.
			LpBuyerWordOfMouth.SetForChecks(0f);
			Check(!LpBuyerWordOfMouth.Enabled && LpBuyerWordOfMouth.Reach(0) == 1f, "off must leave word of mouth whole");
			LpBuyerWordOfMouth.SetForChecks(10000f);
			Check(LpBuyerWordOfMouth.Reach(0) == 0f && Near(LpBuyerWordOfMouth.Reach(10000), 0.5f) && LpBuyerWordOfMouth.Reach(1000000) > 0.98f, "no buyers no talk; K buyers half");
			LpBuyerWordOfMouth.SetForChecks(0f);
			// Hit-package demand (buyers of the carried singles join a Compilation's pool).
			var package = new Album { albumFormat = AlbumFormat.Compilation, trackRefs = new[] {
				new AlbumTrack { isReleasedSingle = true, peakPosition = 1 }, new AlbumTrack { isReleasedSingle = true, peakPosition = 30 },
				new AlbumTrack { isReleasedSingle = true, peakPosition = 0 }, new AlbumTrack { isReleasedSingle = false, peakPosition = 5 } } };
			var studio = new Album { albumFormat = AlbumFormat.Standard, trackRefs = package.trackRefs };
			var half = new MarketRegion { population = 5f };
			HitPackageDemand.SetForChecks(0f, 0f);
			HitPackageDemand.SetPopulationForChecks(10.0);
			Check(!HitPackageDemand.Enabled && !HitPackageDemand.DecayEnabled && HitPackageDemand.RegionalBuyers(package, half, Genre.TraditionalPop, 1961) == 0f, "off adds no buyers");
			Check(Rel(HitPackageDemand.SingleBuyers(1), 878000f) && Rel(HitPackageDemand.SingleBuyers(30), 165000f) && HitPackageDemand.SingleBuyers(0) == 0f
				&& HitPackageDemand.SingleBuyers(101) == 0f && HitPackageDemand.SingleBuyers(20) < HitPackageDemand.SingleBuyers(15)
				&& HitPackageDemand.SingleBuyers(20) > HitPackageDemand.SingleBuyers(30) && HitPackageDemand.SingleBuyers(100) > 0f, "buyers fall with peak; uncharted none");
			Check(Rel(HitPackageDemand.PackageBuyers(package), 1043000f), "only charted released singles count");
			HitPackageDemand.SetForChecks(0.5f, 0.97f);
			Check(Rel(HitPackageDemand.RegionalBuyers(package, half, Genre.TraditionalPop, 1961), 0.5f * 1043000f * 0.5f) && HitPackageDemand.RegionalBuyers(studio, half, Genre.TraditionalPop, 1961) == 0f
				&& HitPackageDemand.Decay == 0.97f, "K x buyers x population share; studio LPs untouched");
			HitPackageDemand.SetForChecks(0.5f, 0f, true);
			float teen = HitPackageDemand.YouthWeight(Genre.TeenPop, 1961), trad = HitPackageDemand.YouthWeight(Genre.TraditionalPop, 1961);
			Check(teen > 0.6f && trad > 0f && trad < 0.2f && HitPackageDemand.YouthWeight(Genre.Jazz, 1961) == 0f, "youth weights are the segment routing");
			Check(Rel(HitPackageDemand.RegionalBuyers(package, half, Genre.TeenPop, 1961), 0.5f * teen * 1043000f * 0.5f)
				&& Rel(HitPackageDemand.RegionalBuyers(package, half, Genre.TraditionalPop, 1961), 0.5f * trad * 1043000f * 0.5f)
				&& HitPackageDemand.RegionalBuyers(package, half, Genre.Jazz, 1961) == 0f, "youth mode scales K by the genre's youth weight");
			HitPackageDemand.SetForChecks(0.5f, 0f, false, 0.4f);
			var soundtrack = new Album { albumFormat = AlbumFormat.Soundtrack, trackRefs = package.trackRefs };
			Check(Rel(HitPackageDemand.RegionalBuyers(studio, half, Genre.TraditionalPop, 1961), 0.4f * 0.5f * 1043000f * 0.5f)
				&& Rel(HitPackageDemand.RegionalBuyers(package, half, Genre.TraditionalPop, 1961), 0.5f * 1043000f * 0.5f)
				&& HitPackageDemand.RegionalBuyers(soundtrack, half, Genre.TraditionalPop, 1961) == 0f, "studio LPs get S x K; soundtracks none");
			HitPackageDemand.SetForChecks(0f, 0f);
			HitPackageDemand.SetPopulationForChecks(0.0);
			// --act-lp-audience=K: a decayed ledger of an act's singles buyers by region.
			{
				var r1 = new MarketRegion { regionId = "r1", population = 5f };
				Record Single(string artist, ReleaseFormat format = ReleaseFormat.Single) => new() { recordId = "s_" + artist, artistId = artist, format = format };
				RecordRuntimeData Lp(string artist, AlbumFormat format, Genre genre) => new(new Record { recordId = "lp_" + artist, artistId = artist,
					format = ReleaseFormat.Album, primaryGenre = genre, album = new Album { albumFormat = format } });
				ActLpAudience.SetForChecks(0f);
				ActLpAudience.AddSingleUnits(Single("a"), "r1", 1000);
				Check(!ActLpAudience.Enabled && ActLpAudience.ActBuyers("a", "r1") == 0d && ActLpAudience.RegionalBuyers(Lp("a", AlbumFormat.Standard, Genre.TeenPop), r1, 1961) == 0f,
					"act LP audience off: no ledger, no buyers");
				ActLpAudience.SetForChecks(0.5f, 13f);
				ActLpAudience.AdvanceWeek(10);
				ActLpAudience.AddSingleUnits(Single("a"), "r1", 1000);
				ActLpAudience.AddSingleUnits(Single("a", ReleaseFormat.EP), "r1", 200);
				ActLpAudience.AddSingleUnits(new Record { recordId = "lp", artistId = "a", format = ReleaseFormat.Album }, "r1", 5000);
				ActLpAudience.AddSingleUnits(Single(""), "r1", 5000);
				Check(ActLpAudience.ActBuyers("a", "r1") == 1200d && ActLpAudience.ActBuyers("a", "r2") == 0d && ActLpAudience.ActBuyers("b", "r1") == 0d,
					"singles and EPs join the act's regional ledger; LPs and actless records do not");
				ActLpAudience.AdvanceWeek(10);
				Check(ActLpAudience.ActBuyers("a", "r1") == 1200d, "one decay a week");
				ActLpAudience.AdvanceWeek(23);
				Check(Math.Abs(ActLpAudience.ActBuyers("a", "r1") - 600d) < 1e-6, "half-life 13 weeks halves the ledger in 13 weeks");
				Check(Rel(ActLpAudience.RegionalBuyers(Lp("a", AlbumFormat.Standard, Genre.TeenPop), r1, 1961), 0.5f * 600f)
					&& Rel(ActLpAudience.RegionalBuyers(Lp("a", AlbumFormat.Compilation, Genre.Jazz), r1, 1961), 0.5f * 600f)
					&& ActLpAudience.RegionalBuyers(Lp("a", AlbumFormat.Soundtrack, Genre.TeenPop), r1, 1961) == 0f
					&& ActLpAudience.RegionalBuyers(Lp("", AlbumFormat.Standard, Genre.TeenPop), r1, 1961) == 0f, "K x ledger for studio and compilation LPs; soundtracks and actless LPs none");
				HitPackageDemand.SetForChecks(0.4f, 0f, true);
				Check(Rel(ActLpAudience.RegionalBuyers(Lp("a", AlbumFormat.Standard, Genre.TeenPop), r1, 1961), 0.5f * teen * 600f)
					&& ActLpAudience.RegionalBuyers(Lp("a", AlbumFormat.Standard, Genre.Jazz), r1, 1961) == 0f, "youth-weighted with --hit-package-demand-youth");
				HitPackageDemand.SetForChecks(0f, 0f);
				// --act-lp-audience-net-credit: the act reads an LP's run net of the increment over the hit-package term.
				Check(ActLpAudience.Increment(300f, 1000f) == 700f && ActLpAudience.Increment(1000f, 300f) == 0f, "increment is the act audience above the hit-package term");
				Check(ActLpAudience.NetPoints(100f, 250d, 1000f) == 75f && ActLpAudience.NetPoints(100f, 2000d, 1000f) == 0f
					&& ActLpAudience.NetPoints(100f, 0d, 0f) == 100f, "net points lose the audience share of chart units, clamped");
				float[] others = { 900f, 500f, 400f, 100f };
				Check(ActLpAudience.NetPosition(450f, others, 40) == 3 && ActLpAudience.NetPosition(1000f, others, 40) == 1
					&& ActLpAudience.NetPosition(400f, others, 40) == 4 && ActLpAudience.NetPosition(50f, others, 4) == 0, "net position ranks among the others; past the chart is 0");
				ActLpAudience.SetForChecks(0.5f, 13f, net: true);
				AlbumScaleCareerMilestones.SetForChecks(true);
				var netLp = Lp("n", AlbumFormat.Standard, Genre.TeenPop);
				netLp.currentPosition = 3; netLp.peakPosition = 3; netLp.albumActNetPosition = 30;
				int actPeak = AlbumScaleCareerMilestones.ObserveActCareerPeak(netLp, 40, 1961);
				Check(actPeak == 75 && netLp.albumCareerPeak == 8 && AlbumScaleCareerMilestones.CareerPeak(netLp, 40) == 8
					&& AlbumScaleCareerMilestones.ActCareerPeak(netLp, 40, 1961) == 75, "the act reads the net equivalent (#30 of 40 = 75); the label keeps the real one (#3 = 8)");
				netLp.currentPosition = 2; netLp.albumActNetPosition = 0;
				Check(AlbumScaleCareerMilestones.ObserveActCareerPeak(netLp, 40, 1961) == 75, "a week off the chart net keeps the run's best net");
				var onlyAudience = Lp("o", AlbumFormat.Standard, Genre.TeenPop);
				onlyAudience.currentPosition = 5; onlyAudience.peakPosition = 5; onlyAudience.albumActNetPosition = 0;
				Check(AlbumScaleCareerMilestones.ObserveActCareerPeak(onlyAudience, 40, 1961) == 0
					&& AlbumScaleCareerMilestones.ActCareerPeak(onlyAudience, 40, 1961) == 0, "an LP that charts only on its act audience never charts for the act");
				ActLpAudience.SetForChecks(0.5f, 13f);
				var plainLp = Lp("p", AlbumFormat.Standard, Genre.TeenPop);
				plainLp.currentPosition = 3; plainLp.peakPosition = 3; plainLp.albumActNetPosition = 30;
				Check(AlbumScaleCareerMilestones.ObserveActCareerPeak(plainLp, 40, 1961) == 8, "net credit off: the act reads the real position");
				AlbumScaleCareerMilestones.SetForChecks(false);
				ActLpAudience.SetForChecks(0f);
			}
			// --singles-phenomenon (§97): smoothed national share, one onset per act, envelope, weighted ration.
			{
				Record Single(string artist, ReleaseFormat format = ReleaseFormat.Single) => new() { recordId = "s_" + artist, artistId = artist, format = format };
				SinglesPhenomenon.SetForChecks(0f);
				SinglesPhenomenon.AddSingleUnits(Single("a"), 1000);
				SinglesPhenomenon.AdvanceWeek(1);
				Check(!SinglesPhenomenon.Enabled && SinglesPhenomenon.RationWeight("a") == 1f && SinglesPhenomenon.AudienceMultiplier("a") == 1f
					&& SinglesPhenomenon.Share("a") == 0d, "phenomenon off: no ledger, weight and audience 1");
				SinglesPhenomenon.SetForChecks(0.2f, 6f, 1.5f, 1f, 52f, 4f);
				SinglesPhenomenon.AdvanceWeek(0);
				SinglesPhenomenon.AddSingleUnits(Single("a"), 300);
				SinglesPhenomenon.AddSingleUnits(Single("b"), 700);
				SinglesPhenomenon.AddSingleUnits(Single("a", ReleaseFormat.Album), 5000);
				SinglesPhenomenon.AddSingleUnits(Single(""), 1000);
				SinglesPhenomenon.AdvanceWeek(1);
				Check(Math.Abs(SinglesPhenomenon.Share("a") - 0.075d) < 1e-9 && Math.Abs(SinglesPhenomenon.Share("b") - 0.175d) < 1e-9,
					"half-life 1: half of last week's national share (actless singles count in the total, LPs not)");
				Check(!SinglesPhenomenon.IsPhenomenon("a") && !SinglesPhenomenon.IsPhenomenon("b") && SinglesPhenomenon.RationWeight("b") == 1f,
					"below the threshold nobody is a phenomenon");
				SinglesPhenomenon.AddSingleUnits(Single("b"), 1000);
				SinglesPhenomenon.AdvanceWeek(2);
				Check(SinglesPhenomenon.IsPhenomenon("b") && !SinglesPhenomenon.IsPhenomenon("a") && Near(SinglesPhenomenon.Envelope("b"), 0.25f)
					&& Near(SinglesPhenomenon.RationWeight("b"), 2.25f) && Near(SinglesPhenomenon.AudienceMultiplier("b"), 1.125f),
					"onset at the threshold; the envelope ramps over 4 weeks");
				SinglesPhenomenon.AdvanceWeek(2);
				Check(Near(SinglesPhenomenon.Envelope("b"), 0.25f), "one update a week");
				Check(Near(SinglesPhenomenon.EnvelopeAt(3, 4f, 52f), (float)Math.Pow(0.5, 3 / 52.0))
					&& Near(SinglesPhenomenon.EnvelopeAt(55, 4f, 52f), 0.5f * (float)Math.Pow(0.5, 3 / 52.0))
					&& SinglesPhenomenon.EnvelopeAt(52 * 6, 4f, 52f) == 0f && SinglesPhenomenon.EnvelopeAt(-1, 4f, 52f) == 0f,
					"full at the end of the ramp, half a fade later, over below 2%");
				SinglesPhenomenon.AddSingleUnits(Single("a"), 1000);
				SinglesPhenomenon.AdvanceWeek(3);
				Check(Math.Abs(SinglesPhenomenon.Share("b") - 0.29375d) < 1e-9 && Near(SinglesPhenomenon.Envelope("b"), 0.5f * (float)Math.Pow(0.5, 1 / 52.0))
					&& SinglesPhenomenon.IsPhenomenon("a"), "the onset week stays put as the envelope ramps; a later act can cross too");
				// Weighted ration: all weights 1 is the proportional ration; a weight-4 title clears 4x per unit of intent, capped at intent.
				double[] flat = SinglesPhenomenon.Shares(new[] { 100, 300, 600 }, new[] { 1f, 1f, 1f }, 500);
				Check(Math.Abs(flat[0] - 50) < 1e-9 && Math.Abs(flat[1] - 150) < 1e-9 && Math.Abs(flat[2] - 300) < 1e-9, "weights 1: proportional");
				double[] w4 = SinglesPhenomenon.Shares(new[] { 100, 300, 600 }, new[] { 4f, 1f, 1f }, 260);
				Check(Math.Abs(w4[0] - 1040.0 / 13) < 1e-6 && Math.Abs(w4.Sum() - 260) < 1e-6 && Math.Abs(w4[1] / w4[0] - 0.75) < 1e-6,
					"weight 4 clears 4x per unit of intent; the purse is spent exactly");
				double[] cap = SinglesPhenomenon.Shares(new[] { 100, 300, 600 }, new[] { 20f, 1f, 1f }, 400);
				Check(cap[0] == 100 && Math.Abs(cap[1] - 100) < 1e-6 && Math.Abs(cap[2] - 200) < 1e-6, "a phenomenon title stops at its own intent; the rest share what is left");
				Check(!SinglesPhenomenon.AnyWeighted(new[] { 5, 0 }, new[] { 1f, 3f }) && SinglesPhenomenon.AnyWeighted(new[] { 5, 1 }, new[] { 1f, 3f }),
					"weighted path only when a weighted title has intent");
				SinglesPhenomenon.SetForChecks(0f);
			}
			// --local-promotion (§98): per-record intent shift toward the footprint, national intent held; Majors untouched.
			{
				LocalPromotion.SetForChecks(1f);
				Check(!LocalPromotion.Enabled && !LocalPromotion.Shifts(new RecordRuntimeData { baseRecord = new Record { format = ReleaseFormat.Single } },
					new AILabel { homeRegion = "home" }), "local promotion off: nothing shifts");
				Check(LocalPromotion.Weight(new[] { "home" }, "home", 3f) == 3f && LocalPromotion.Weight(new[] { "home" }, "b", 3f) == 1f
					&& LocalPromotion.Weight(new[] { "home", "b" }, "b", 3f) == 2f && LocalPromotion.Weight(new string[0], "home", 3f) == 1f,
					"weights: 1 + (K - 1)/n in the footprint, 1 elsewhere");
				string[] three = { "home", "b", "c" };
				Check(Math.Abs(LocalPromotion.Weight(three, "home", 3f, "home", .75f) - 2.5f) < 1e-6
					&& Math.Abs(LocalPromotion.Weight(three, "b", 3f, "home", .75f) - 1.25f) < 1e-6
					&& LocalPromotion.Weight(three, "d", 3f, "home", .75f) == 1f
					&& LocalPromotion.Weight(new[] { "home" }, "home", 3f, "home", .75f) == 3f
					&& Math.Abs(LocalPromotion.Weight(three, "home", 3f, "home", .1f) - LocalPromotion.Weight(three, "b", 3f, "home", .1f)) < 1e-6
					&& Math.Abs(LocalPromotion.Weight(three, "b", 3f, "elsewhere", .75f) - (1f + 2f / 3f)) < 1e-6,
					"home share: home 1 + (K - 1)H, branches split the rest; never below an equal split; a home-only label keeps K");
				int[] big = { int.MaxValue, int.MaxValue, int.MaxValue };
				int[] s = LocalPromotion.ShiftValues(new[] { 3f, 1f, 1f }, new[] { 100, 200, 300 }, big);
				Check(s.Sum() == 600 && Math.Abs(s[0] / (double)s[1] - 1.5) < 0.02 && Math.Abs(s[2] / (double)s[1] - 1.5) < 0.02,
					"national intent held; home x3 relative to away");
				Check(LocalPromotion.ShiftValues(new[] { 1f, 1f, 1f }, new[] { 100, 200, 300 }, big).SequenceEqual(new[] { 100, 200, 300 }),
					"equal weights are the identity");
				Check(LocalPromotion.ShiftValues(new[] { 3f, 1f }, new[] { 0, 0 }, big).SequenceEqual(new[] { 0, 0 })
					&& LocalPromotion.ShiftValues(new[] { 3f, 1f }, new[] { 0, 50 }, big).SequenceEqual(new[] { 0, 50 }),
					"no intent at home: nothing to move");
				int[] capShift = LocalPromotion.ShiftValues(new[] { 3f, 1f, 1f }, new[] { 100, 200, 300 }, new[] { 120, int.MaxValue, int.MaxValue });
				Check(capShift[0] == 120 && capShift.Sum() == 600 && Math.Abs(capShift[2] / (double)capShift[1] - 1.5) < 0.02,
					"a region stays inside its physical limit; its excess goes to the record's other regions");
				int[] tight = LocalPromotion.ShiftValues(new[] { 3f, 1f }, new[] { 100, 100 }, new[] { 110, 100 });
				Check(tight.SequenceEqual(new[] { 110, 90 }), "both limits respected, total kept where room allows");
				Check(LocalPromotion.LedgerUnits(50, 200, 100) == 50 && LocalPromotion.LedgerUnits(50, 200, -1) == 50,
					"ledger diagnostic off: the act ledger books cleared units");
				var fp = LocalPromotion.Footprint(new AILabel { homeRegion = "home", distributionRegions = new[] { "home", "b" }, strongRegions = new[] { "c" } });
				Check(fp.SetEquals(new[] { "home", "b", "c" }), "footprint = home + owned distribution + strong regions");
				var wholesale = new AILabel { homeRegion = "home", distributionRegions = new[] { "home" } };
				wholesale.independentDistributionRegions.Add("b");
				Check(LocalPromotion.Footprint(wholesale).SetEquals(new[] { "home" }), "wholesale coverage ships but does not promote");
				// --local-promotion-midtier (§101): MidTiers promote through national staff, not an owner at home.
				var single = new RecordRuntimeData { baseRecord = new Record { format = ReleaseFormat.Single } };
				AILabel Lab(string id, LabelTier t) => new AILabel { labelId = id, tier = t, homeRegion = "home", distributionRegions = new[] { "home", "b", "c" } };
				string[] regs = { "home", "b", "c", "d" };
				int[] even = { 100, 100, 100, 100 }, room = { int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue };
				LocalPromotion.SetForChecks(3f, .75f);
				int[] ownerMid = LocalPromotion.Shift(Lab("m1", LabelTier.MidTier), regs, even, room);
				Check(LocalPromotion.Shifts(single, Lab("m1", LabelTier.MidTier)) && ownerMid[0] > ownerMid[1] + 50,
					"midtier switch absent: a MidTier gets the owner home share like any label");
				LocalPromotion.SetForChecks(3f, .75f, LocalPromotion.MidTierMode.Equal);
				int[] eqMid = LocalPromotion.Shift(Lab("m2", LabelTier.MidTier), regs, even, room);
				int[] eqIndie = LocalPromotion.Shift(Lab("i2", LabelTier.Independent), regs, even, room);
				Check(Math.Abs(eqMid[0] - eqMid[1]) <= 1 && eqMid[1] > eqMid[3] && eqMid.Sum() == 400 && eqIndie.SequenceEqual(ownerMid),
					"midtier=equal: a MidTier's footprint splits the boost equally; other tiers keep the home share");
				LocalPromotion.SetForChecks(3f, .75f, LocalPromotion.MidTierMode.Off);
				Check(!LocalPromotion.Shifts(single, Lab("m3", LabelTier.MidTier)) && LocalPromotion.Shifts(single, Lab("i3", LabelTier.Independent)),
					"midtier=off: MidTier records are not shifted; other tiers are");
				// --local-promotion-feedback=preshift (§103): restated reports keep the record's total.
				Check(LocalPromotion.RestateUnits(new[] { 10, 20, 30 }, new[] { 50, 100, 150 }, new[] { 50, 100, 150 }).SequenceEqual(new[] { 10, 20, 30 }),
					"restate: an unshifted record is unchanged");
				int[] back = LocalPromotion.RestateUnits(new[] { 60, 20, 20 }, new[] { 100, 200, 200 }, new[] { 300, 100, 100 });
				Check(back.Sum() == 100 && back[0] == 20 && back[1] == 40 && back[2] == 40,
					"restate: home sales return to the pre-shift geography at each region's fill, total kept");
				int[] odd = LocalPromotion.RestateUnits(new[] { 7, 3, 1 }, new[] { 10, 10, 10 }, new[] { 20, 6, 4 });
				Check(odd.Sum() == 11 && LocalPromotion.RestateUnits(new[] { 0, 0 }, new[] { 5, 5 }, new[] { 9, 1 }).SequenceEqual(new[] { 0, 0 }),
					"restate: integer total kept by largest remainders; nothing sold, nothing reported");
				// --local-promotion-feedback=money,supply,audience (§104): only the named groups read the restatement.
				LocalPromotion.SetForChecks(3f, .75f, LocalPromotion.MidTierMode.Owner, LocalPromotion.FeedbackGroup.Supply | LocalPromotion.FeedbackGroup.Audience);
				var fed = new RegionalRecordData("home") { unitsSoldThisWeek = 60, unitsSoldTotal = 100, feedbackUnitsDelta = -40, feedbackUnitsTotalDelta = -50 };
				Check(LocalPromotion.Units(fed, LocalPromotion.FeedbackGroup.Money) == 60 && LocalPromotion.Units(fed, LocalPromotion.FeedbackGroup.Supply) == 20
					&& LocalPromotion.Units(fed, LocalPromotion.FeedbackGroup.Audience) == 20 && LocalPromotion.UnitsTotal(fed, LocalPromotion.FeedbackGroup.Audience) == 50
					&& LocalPromotion.UnitsTotal(fed, LocalPromotion.FeedbackGroup.Money) == 100 && !LocalPromotion.FeedbackPreShift,
					"feedback groups: named groups read restated units, the rest the cleared units");
				LocalPromotion.SetForChecks(3f);
				Check(LocalPromotion.Units(fed, LocalPromotion.FeedbackGroup.Audience) == 60, "feedback groups off: every reader sees the cleared units");
				// --away-restock-lag (§105): an away reorder lands L weeks on; none is placed while it is in transit.
				AwayRestockLag.SetForChecks(2);
				var lagged = new RegionalRecordData("away") { unitsInStores = 10, unitsBackordered = 50 };
				AwayRestockLag.Order(lagged, 40, 5);
				bool wk6 = AwayRestockLag.Land(lagged, 6) && lagged.unitsInStores == 10;
				bool wk7 = !AwayRestockLag.Land(lagged, 7) && lagged.unitsInStores == 50 && lagged.unitsBackordered == 10 && lagged.restockInTransit == 0;
				Check(wk6 && wk7 && !AwayRestockLag.Land(lagged, 8) && AwayRestockLag.SmallTier(LabelTier.Boutique) && !AwayRestockLag.SmallTier(LabelTier.MidTier),
					"away restock lag: in transit until week + L, then stock lands and backorders clear; small tiers only");
				AwayRestockLag.SetForChecks(0);
				LocalPromotion.SetForChecks(1f);
			}
			// Album chart reporting weight and soundtrack pool breadth.
			AlbumChartReporting.SetForChecks(null);
			Check(!AlbumChartReporting.Enabled && AlbumChartReporting.Weight(Genre.Country, AlbumFormat.Standard) == 1f, "off reports every genre whole");
			AlbumChartReporting.SetForChecks(new System.Collections.Generic.Dictionary<Genre, float> { [Genre.Country] = 0.5f });
			Check(AlbumChartReporting.Weight(Genre.Country, AlbumFormat.Standard) == 0.5f && AlbumChartReporting.Weight(Genre.Country, AlbumFormat.Compilation) == 0.5f
				&& AlbumChartReporting.Weight(Genre.Country, AlbumFormat.Soundtrack) == 1f && AlbumChartReporting.Weight(Genre.TraditionalPop, AlbumFormat.Standard) == 1f,
				"named genre weighted; soundtracks and others whole");
			AlbumChartReporting.SetForChecks(null);
			SoundtrackPoolBreadth.SetForChecks(0f);
			Check(!SoundtrackPoolBreadth.Enabled && SoundtrackPoolBreadth.Breadth == 1f, "off keeps the soundtrack pool");
			SoundtrackPoolBreadth.SetForChecks(3f);
			Check(SoundtrackPoolBreadth.Enabled && SoundtrackPoolBreadth.Breadth == 3f, "breadth reads");
			SoundtrackPoolBreadth.SetForChecks(0f);
			{
				var st = new RecordRuntimeData { baseRecord = new Record { album = new Album { albumFormat = AlbumFormat.Soundtrack, externalMedia = new ExternalMediaProfile() } },
					peakPosition = 4, weeksSinceRelease = 32 };
				SoundtrackHitTail.SetForChecks(0f);
				Check(!SoundtrackHitTail.Enabled && SoundtrackHitTail.Renewal(st) == 1f, "off leaves soundtrack exhaustion whole");
				SoundtrackHitTail.SetForChecks(0.05f);
				Check(Near(SoundtrackHitTail.Renewal(st), 2f), "top-10 soundtrack renews 1 + R per week after 12");
				st.weeksSinceRelease = 10;
				Check(SoundtrackHitTail.Renewal(st) == 1f, "no renewal before week 12");
				st.weeksSinceRelease = 32; st.peakPosition = 11;
				Check(SoundtrackHitTail.Renewal(st) == 1f, "a soundtrack outside the top 10 is untouched");
				SoundtrackHitTail.SetForChecks(0.05f, 20);
				Check(Near(SoundtrackHitTail.Renewal(st), 2f), "a lower hit bar renews a #11 soundtrack");
				SoundtrackHitTail.SetForChecks(0.05f);
				st.peakPosition = 4; st.baseRecord.album.albumFormat = AlbumFormat.Standard;
				Check(SoundtrackHitTail.Renewal(st) == 1f, "a non-soundtrack is untouched");
				SoundtrackHitTail.SetForChecks(0f);
				// --hl-hit-life-top / -share (§77).
				var hl = new RecordRuntimeData { baseRecord = new Record { album = new Album { albumFormat = AlbumFormat.Standard } },
					currentPosition = 3, peakWeeklyUnits = 1000, unitsThisWeek = 200, weeksSinceRelease = 20 };
				LpHitLife.SetForChecks(0, 0f);
				LpHitLife.Advance(hl, 8f);
				Check(!LpHitLife.Enabled && !LpHitLife.Held(hl) && hl.albumHitLifePausedWeeks == 0f && LpHitLife.DecayAge(hl, 20) == 20f, "off holds nothing");
				LpHitLife.SetForChecks(10, 0f);
				LpHitLife.Advance(hl, 8f);
				Check(hl.albumHitLifePausedWeeks == 1f && LpHitLife.DecayAge(hl, 20) == 19f, "a top-10 LP's clock stops a week");
				Check(Near(AlbumSimulator.GetAlbumTimeDecayMultiplier(hl.baseRecord.album, 20, 6, 19f), Mathf.Pow(LpDemandProbe.WeeklyDecay, 11f))
					&& AlbumSimulator.GetAlbumTimeDecayMultiplier(hl.baseRecord.album, 20, 6, 20f) == AlbumSimulator.GetAlbumTimeDecayMultiplier(hl.baseRecord.album, 20, 6),
					"decay reads the paused age; an unpaused age matches the legacy call");
				hl.currentPosition = 11;
				LpHitLife.Advance(hl, 8f);
				Check(hl.albumHitLifePausedWeeks == 1f, "an LP below the top N decays");
				LpHitLife.SetForChecks(10, 0.5f);
				Check(!LpHitLife.Held(hl), "below half its peak is not held");
				hl.unitsThisWeek = 500;
				Check(LpHitLife.Held(hl), "half its peak holds it");
				LpHitLife.SetForChecks(10, 0.5f, 0.5f, 1.25f);
				LpHitLife.Advance(hl, 8f);
				Check(Near(hl.albumHitLifePausedWeeks, 1.25f), "rate slows the clock; the cap binds");
				hl.albumHitLifePausedWeeks = 0f; hl.weeksSinceRelease = 6;
				LpHitLife.Advance(hl, 8f);
				Check(hl.albumHitLifePausedWeeks == 0f, "no pause before decay starts");
				hl.weeksSinceRelease = 20; hl.baseRecord.album.albumFormat = AlbumFormat.Soundtrack; hl.baseRecord.album.externalMedia = new ExternalMediaProfile();
				LpHitLife.Advance(hl, 8f);
				Check(hl.albumHitLifePausedWeeks == 0f, "a profiled soundtrack keeps its box-office path");
				hl.baseRecord.album.albumFormat = AlbumFormat.Standard; hl.baseRecord.album.externalMedia = null;
				LpHitLife.SetForChecks(0, 0f, tailTopN: 10, tailDecay: 0.98f);
				hl.peakPosition = 4; hl.currentPosition = 5;
				LpHitLife.Advance(hl, 8f);
				Check(LpHitLife.Enabled && hl.albumHitLifePausedWeeks == 0f, "the tail leaves a top-10 week on the normal clock");
				hl.currentPosition = 25;
				LpHitLife.Advance(hl, 8f);
				float tailAge = 20f - hl.albumHitLifePausedWeeks;
				Check(Near(Mathf.Pow(LpDemandProbe.WeeklyDecay, tailAge - 8f), Mathf.Pow(LpDemandProbe.WeeklyDecay, 11f) * 0.98f),
					"a past-peak week outside the top N decays at D");
				hl.albumHitLifePausedWeeks = 0f; hl.peakPosition = 11;
				LpHitLife.Advance(hl, 8f);
				Check(hl.albumHitLifePausedWeeks == 0f, "an LP that never reached the top N keeps the normal decay");
				LpHitLife.SetForChecks(0, 0f);
				Check(!LpHitLife.RenewalEnabled && LpHitLife.Renewal(hl) == 1f, "renewal off divides nothing");
				LpHitLife.SetForChecks(0, 0f, renewalRate: 0.05f);
				hl.peakPosition = 4; hl.weeksSinceRelease = 32;
				Check(!LpHitLife.Enabled && Near(LpHitLife.Renewal(hl), 2f), "a top-10 LP renews 1 + R per week after 12, decay modes off");
				hl.peakPosition = 11;
				Check(LpHitLife.Renewal(hl) == 1f, "an LP that peaked below the bar is untouched");
				hl.peakPosition = 4; hl.baseRecord.album.albumFormat = AlbumFormat.Soundtrack; hl.baseRecord.album.externalMedia = new ExternalMediaProfile();
				Check(LpHitLife.Renewal(hl) == 1f, "a profiled soundtrack keeps its own hit tail");
				LpHitLife.SetRenewalRacksOnlyForChecks(true);
				Check(LpHitLife.RenewalEnabled && !LpHitLife.StoreRenewalEnabled, "racks-only leaves store penetration whole");
				LpHitLife.SetForChecks(0, 0f);
				OpeningSoundtrackCatalogue.SetForChecks(0);
				Check(!OpeningSoundtrackCatalogue.Enabled && OpeningSoundtrackCatalogue.Count == 0, "off seeds no opening soundtracks");
				OpeningSoundtrackCatalogue.SetForChecks(8);
				Check(OpeningSoundtrackCatalogue.Enabled && OpeningSoundtrackCatalogue.Count == 8, "opening soundtrack count reads");
				OpeningSoundtrackCatalogue.SetForChecks(0);
				// --opening-evergreen-lps (§81).
				OpeningEvergreenLps.SetForChecks(0);
				Check(!OpeningEvergreenLps.Enabled && OpeningEvergreenLps.Count == 0, "off seeds no evergreen LPs");
				OpeningEvergreenLps.SetForChecks(8);
				Check(OpeningEvergreenLps.Enabled && OpeningEvergreenLps.Count == 8, "evergreen count reads");
				Check(OpeningEvergreenLps.Inventory.Length == 16 && OpeningEvergreenLps.Inventory.All(x => x.Weeks >= 40),
					"the inventory is the 16 Essential Inventory artist LPs, all 40+ weeks");
				Check(AlbumSimulator.CalculateAlbumExhaustion(OpeningEvergreenLps.SpentPool) == 0.15f, "the spent pool sits at the exhaustion floor");
				LpHitLife.SetForChecks(0, 0f);
				Check(OpeningEvergreenLps.PausedWeeks(54, 3, 8f) == 0f, "no tail, no paused weeks");
				LpHitLife.SetForChecks(0, 0f, tailTopN: 10, tailDecay: 0.98f);
				float evPaused = OpeningEvergreenLps.PausedWeeks(54, 3, 8f);
				Check(Near(Mathf.Pow(LpDemandProbe.WeeklyDecay, 54f - evPaused - 8f), Mathf.Pow(LpDemandProbe.WeeklyDecay, 6f) * Mathf.Pow(0.98f, 40f)),
					"an evergreen's clock reads 6 held weeks then 40 tail weeks");
				Check(OpeningEvergreenLps.PausedWeeks(54, 11, 8f) == 0f && OpeningEvergreenLps.PausedWeeks(12, 3, 8f) == 0f,
					"no pause outside the tail's top N or before the held weeks end");
				LpHitLife.SetForChecks(0, 0f);
				var evZ = Enumerable.Range(0, 2001).Select(i => OpeningEvergreenLps.ReceptionZ(1001UL, $"gen_{i}")).OrderBy(z => z).ToArray();
				Check(evZ[0] >= OpeningEvergreenLps.ReceptionZFloor && OpeningEvergreenLps.ReceptionZ(1001UL, "gen_7") == OpeningEvergreenLps.ReceptionZ(1001UL, "gen_7")
					&& Math.Abs(evZ[1000] - 1.2) < 0.06, "evergreen reception: keyed, at or above the floor, median about 1.2");
				OpeningEvergreenLps.SetForChecks(0);
				// --opening-programme-clocks (§86).
				var ocAct = new SimulatedArtist { artistId = "artist_00042", weeksSinceLastRelease = 23, publicHistory = new System.Collections.Generic.List<PublicReleaseEntry>() };
				OpeningProgrammeClocks.SetForChecks(false);
				OpeningProgrammeClocks.SetOpeningActsForChecks(new[] { ocAct.artistId }, 1960);
				Check(!OpeningProgrammeClocks.Applies(ocAct), "off dates no act from its history");
				OpeningProgrammeClocks.SetForChecks(true);
				Check(OpeningProgrammeClocks.Applies(ocAct) && !OpeningProgrammeClocks.Applies(new SimulatedArtist { artistId = "artist_09999" }),
					"only opening-roster acts are dated from history");
				Check(OpeningProgrammeClocks.Age(ocAct, ReleaseFormat.Single) == 23, "a single is dated from the act's elapsed interval");
				var ocAges = Enumerable.Range(0, 4000).Select(i => OpeningProgrammeClocks.Age(new SimulatedArtist { artistId = $"artist_{i:D5}",
					publicHistory = new System.Collections.Generic.List<PublicReleaseEntry>() }, ReleaseFormat.Album)).OrderBy(a => a).ToArray();
				Check(ocAges[0] >= 0 && ocAges[^1] < 2 * LabelReleaseProgramme.AlbumFloor && Math.Abs(ocAges[2000] - LabelReleaseProgramme.AlbumFloor) <= 2
					&& Math.Abs(ocAges[1000] - 9) <= 2 && Math.Abs(ocAges[3000] - 27) <= 2, "no LP history: keyed uniform age over twice the LP floor");
				Check(OpeningProgrammeClocks.Age(ocAct, ReleaseFormat.Album) == OpeningProgrammeClocks.Age(ocAct, ReleaseFormat.Album), "keyed, so repeatable");
				ocAct.publicHistory.Add(new PublicReleaseEntry { year = 1958, format = ReleaseFormat.Album });
				ocAct.publicHistory.Add(new PublicReleaseEntry { year = 1959, format = ReleaseFormat.Single });
				int ocLp = OpeningProgrammeClocks.Age(ocAct, ReleaseFormat.Album);
				Check(ocLp > 52 && ocLp <= 104, "a 1958 LP dates the clock within 1958");
				OpeningProgrammeClocks.SetForChecks(false);
				OpeningProgrammeClocks.SetOpeningActsForChecks(null, 1960);
				// --opening-star-tier-share / --opening-catalogue-major-share (§88).
				OpeningTierQuotas.SetForChecks(-1f, -1f);
				Check(!OpeningTierQuotas.StarsEnabled && !OpeningTierQuotas.CatalogueEnabled && OpeningTierQuotas.MajorWeightScale(925f, 4000f) == 1f,
					"off: no reservation and the Majors' catalogue weight is unchanged");
				OpeningTierQuotas.SetForChecks(0.37f, 0.2f);
				int otqMajors = 0;
				var otqSplit = Enumerable.Range(0, 24).Select(i => { bool m = OpeningTierQuotas.NextIsMajor(i, otqMajors); if (m) otqMajors++; return m; }).ToArray();
				Check(otqMajors == 9 && !otqSplit[0] && otqSplit.Take(6).Count(x => x) == 2, "24 Stars at 0.37: 9 Majors, interleaved down the order");
				int otqSup = 0; for (int i = 0; i < 6; i++) if (OpeningTierQuotas.NextIsMajor(i, otqSup)) otqSup++;
				Check(otqSup == 2, "6 Superstars at 0.37: 2 Majors");
				float otqScale = OpeningTierQuotas.MajorWeightScale(1850f, 4000f);
				Check(Math.Abs(1850f * otqScale / (1850f * otqScale + 4000f) - 0.2f) < 1e-5f, "the scaled Major weight is the named share of the total");
				OpeningTierQuotas.SetArchetypeSharesForChecks(new System.Collections.Generic.Dictionary<OpeningCareerArchetype, float> {
					[OpeningCareerArchetype.AdultStandardBearer] = 0.85f, [OpeningCareerArchetype.CurrentHitmaker] = 0.25f,
					[OpeningCareerArchetype.EstablishedHitmaker] = 0.6f });
				int otqAdult = 0; for (int i = 0; i < 8; i++) if (OpeningTierQuotas.NextIsMajor(OpeningTierQuotas.StarShareFor(OpeningCareerArchetype.AdultStandardBearer), i, otqAdult)) otqAdult++;
				Check(OpeningTierQuotas.ByArchetype && otqAdult == 7 && OpeningTierQuotas.StarShareFor(OpeningCareerArchetype.CurrentHitmaker) == 0.25f,
					"archetype shares: 8 standard-bearers at 0.85 put 7 on Majors; each archetype reads its own share");
				OpeningTierQuotas.SetForChecks(-1f, -1f);
				Check(!OpeningTierQuotas.ByArchetype, "the single-share form clears the archetype shares");
				// --opening-staff-catch-up (§89).
				OpeningStaffCatchUp.SetForChecks(0, 1960 * 12);
				Check(!OpeningStaffCatchUp.Applies(1960 * 12), "off: no catch-up month");
				OpeningStaffCatchUp.SetForChecks(12, 1960 * 12);
				Check(OpeningStaffCatchUp.Applies(1960 * 12) && OpeningStaffCatchUp.Applies(1960 * 12 + 11) && !OpeningStaffCatchUp.Applies(1961 * 12),
					"12 months: January to December 1960");
				OpeningStaffCatchUp.SetForChecks(0, int.MinValue);
				SoundtrackOriginationEarly.SetForChecks(1f);
				Check(!SoundtrackOriginationEarly.Enabled && SoundtrackOriginationEarly.Scale(1960) == 1f, "off keeps soundtrack origination");
				SoundtrackOriginationEarly.SetForChecks(0.5f);
				Check(SoundtrackOriginationEarly.Scale(1960) == 0.5f && SoundtrackOriginationEarly.Scale(1961) == 0.5f
					&& SoundtrackOriginationEarly.Scale(1962) == 1f, "early origination scaled through 1961 only");
				SoundtrackOriginationEarly.SetForChecks(1f);
				SoundtrackLaunchAwareness.SetForChecks(1f);
				Check(!SoundtrackLaunchAwareness.Enabled && SoundtrackLaunchAwareness.Scale == 1f, "off keeps soundtrack launch awareness");
				SoundtrackLaunchAwareness.SetForChecks(0.4f);
				Check(SoundtrackLaunchAwareness.Enabled && SoundtrackLaunchAwareness.Scale == 0.4f, "launch awareness scale reads");
				SoundtrackLaunchAwareness.SetForChecks(1f);
				SoundtrackFormatTilt.SetForChecks(0f);
				Check(!SoundtrackFormatTilt.Enabled, "off keeps the genre format tilt on soundtracks");
				SoundtrackFormatTilt.SetForChecks(1f);
				Check(SoundtrackFormatTilt.Enabled && SoundtrackFormatTilt.Tilt == 1f, "soundtrack format tilt reads");
				SoundtrackFormatTilt.SetForChecks(0f);
				SoundtrackOriginationSeason.SetForChecks(0f);
				Check(SoundtrackOriginationSeason.Weight(11) == 1f, "off keeps origination flat through the year");
				SoundtrackOriginationSeason.SetForChecks(0.8f);
				float seasonMean = 0f; for (int m = 1; m <= 12; m++) seasonMean += SoundtrackOriginationSeason.Weight(m) / 12f;
				Check(Near(SoundtrackOriginationSeason.Weight(11), 1.8f) && Near(SoundtrackOriginationSeason.Weight(5), 0.2f) && Near(seasonMean, 1f), "November peak, May trough, mean 1");
				SoundtrackOriginationSeason.SetForChecks(0f);
				SoundtrackBuild.SetForChecks(0f);
				Check(SoundtrackBuild.Factor(0f) == 1f, "off keeps the box-office launch");
				SoundtrackBuild.SetForChecks(16f);
				Check(Near(SoundtrackBuild.Factor(0f), 0.35f) && Near(SoundtrackBuild.Factor(8f), 0.675f) && SoundtrackBuild.Factor(16f) == 1f && SoundtrackBuild.Factor(40f) == 1f, "build from .35 to 1 over B weeks");
				SoundtrackBuild.SetForChecks(0f);
				AlbumChartBlend.SetForChecks(1f);
				Check(!AlbumChartBlend.Enabled && AlbumChartBlend.Next(5000f, 9000f, false) == 9000f, "off ranks this week's points");
				AlbumChartBlend.SetForChecks(0.25f);
				Check(Near(AlbumChartBlend.Next(0f, 8000f, false), 2000f) && Near(AlbumChartBlend.Next(4000f, 8000f, false), 5000f)
					&& AlbumChartBlend.Next(0f, 8000f, true) == 8000f, "blend enters low and climbs; seeded LPs start at their points");
				Check(AlbumChartBlend.Seeds(false, 120) && !AlbumChartBlend.Seeds(false, 0) && !AlbumChartBlend.Seeds(true, 120), "only old LPs first seen are seeded");
				{
					var lp = new RecordRuntimeData { currentPosition = 35, albumRawRank = 4 };
					AlbumChartBlend.SetFloorsRawForChecks(true);
					Check(AlbumChartBlend.DemandPosition(lp) == 4, "raw floors read the unblended rank");
					AlbumChartBlend.SetForChecks(1f);
					Check(AlbumChartBlend.DemandPosition(lp) == 35, "blend off: demand reads the chart whatever the floors flag");
					AlbumChartBlend.SetFloorsRawForChecks(false);
				}
				AlbumChartBlend.SetForChecks(1f);
				ExternalMediaProfile op = ExternalMediaService.GenerateOpeningProfile(blockbuster: true);
				Check(op.isBlockbuster && op.boxOfficeTrajectory >= 0.85f && op.sourceType != ExternalMediaSourceType.FilmSong,
					"opening soundtrack is a hit cast or film album");
			}
			LpLaunchStanding.SetForChecks(0f);
			Check(!LpLaunchStanding.Enabled && LpLaunchStanding.Standing(0) == 1f && LpLaunchStanding.Factor(null) == 1f, "off must leave LP launch whole");
			LpLaunchStanding.SetForChecks(30000f);
			Check(LpLaunchStanding.Standing(0) == 0f && Near(LpLaunchStanding.Standing(30000), 0.5f) && LpLaunchStanding.Standing(3000000) > 0.98f
				&& LpLaunchStanding.Standing(-5) == 0f && LpLaunchStanding.Factor(null) == 1f, "nothing sold no label launch; K units half; no act untouched");
			// LP label standing (push and tier premium by the act's launch standing).
			var stamped = new RecordRuntimeData { lpLabelStanding = 0.25f };
			var unstamped = new RecordRuntimeData();
			LpLabelStanding.SetForChecks(false, false);
			Check(!LpLabelStanding.Enabled && LpLabelStanding.Push(stamped) == 1f && LpLabelStanding.Tier(stamped, 1.36f) == 1.36f, "off must leave push and tier whole");
			LpLabelStanding.SetForChecks(true, false);
			Check(Near(LpLabelStanding.Push(stamped), 0.25f) && LpLabelStanding.Push(unstamped) == 1f && LpLabelStanding.Tier(stamped, 1.36f) == 1.36f,
				"push only: push by standing, tier whole, an unstamped record whole");
			LpLabelStanding.SetForChecks(false, true);
			Check(LpLabelStanding.Push(stamped) == 1f && Near(LpLabelStanding.Tier(stamped, 1.36f), 1.09f) && LpLabelStanding.Tier(stamped, 0.5f) == 0.5f
				&& LpLabelStanding.Tier(unstamped, 1.36f) == 1.36f, "tier only: premium above 1 by standing; constants below 1 untouched");
			LpLabelStanding.SetForChecks(true, true);
			var noLaunch = new RecordRuntimeData();
			LpLabelStanding.Stamp(noLaunch, false, new SimulatedArtist());
			LpLabelStanding.Stamp(noLaunch, true, null);
			Check(noLaunch.lpLabelStanding == null, "a single or an LP with no act is never stamped");
			LpLabelStanding.Stamp(noLaunch, true, new SimulatedArtist { artistId = "artist_check_none" });
			Check(noLaunch.lpLabelStanding == 0f, "an act with nothing sold is stamped 0");
			LpLabelStanding.SetForChecks(false, false);
			LpLaunchStanding.SetForChecks(0f);

			// LP reception: off is whole; on is keyed, repeatable and mean-preserving.
			LpReception.SetForChecks(0f);
			var lpRec = new RecordRuntimeData { baseRecord = new Record { recordId = "gen_check_1", artistId = "artist_check", format = ReleaseFormat.Album } };
			Check(!LpReception.Enabled && LpReception.Factor(lpRec) == 1f && float.IsNaN(lpRec.lpReception), "off must leave conversion whole");
			LpReception.SetForChecks(1.2f);
			double zSum = 0, z2Sum = 0, mSum = 0; int below = 0; const int draws = 20000;
			for (int i = 0; i < draws; i++) {
				double z = LpReception.Z(2002UL, "gen_" + i);
				zSum += z; z2Sum += z * z; mSum += LpReception.Multiplier(z); if (LpReception.Multiplier(z) < 1f) below++;
			}
			Check(Math.Abs(zSum / draws) < 0.03 && Math.Abs(z2Sum / draws - 1.0) < 0.05, "z is standard normal");
			Check(Math.Abs(mSum / draws - 1.0) < 0.08 && below > draws / 2, "mean multiplier 1, median below 1");
			Check(LpReception.Z(2002UL, "gen_7") == LpReception.Z(2002UL, "gen_7") && LpReception.Z(2002UL, "gen_7") != LpReception.Z(1001UL, "gen_7"), "keyed on record and seed");
			var artistless = new RecordRuntimeData { baseRecord = new Record { recordId = "gen_check_2", format = ReleaseFormat.Album } };
			Check(LpReception.Factor(artistless) == 1f, "an LP with no roster act is untouched");
			Check(LpReception.Factor(lpRec) > 0f && LpReception.Factor(lpRec) == lpRec.lpReception, "the draw is cached on the record");
			// --album-chart-racks=W.
			AlbumChartRacks.SetForChecks(0f);
			Check(!AlbumChartRacks.Enabled && AlbumChartRacks.ChartUnits(1000, 5000) == 1000f, "album chart racks off: store units only");
			var rackLp = new RecordRuntimeData { baseRecord = new Record { recordId = "gen_check_racks", artistId = "a", format = ReleaseFormat.Album },
				unitsThisWeek = 1000, rackUnitsThisWeek = 3000 };
			float storeOnlyPoints = AlbumSimulator.CalculateChartPoints(rackLp);
			AlbumChartRacks.SetForChecks(1f);
			Check(AlbumChartRacks.Enabled && AlbumChartRacks.ChartUnits(1000, 3000) == 4000f && AlbumChartRacks.ChartUnits(1000, -5) == 1000f,
				"album chart racks on: store + W x rack, negative rack ignored");
			Check(Math.Abs(AlbumSimulator.CalculateChartPoints(rackLp) - 4f * storeOnlyPoints) < 1e-2f, "album chart points scale with store + rack units");
			AlbumChartRacks.SetForChecks(0.5f);
			Check(AlbumChartRacks.ChartUnits(1000, 3000) == 2500f, "album chart racks weight W");
			AlbumChartRacks.SetForChecks(0f);
			Check(Math.Abs(AlbumSimulator.CalculateChartPoints(rackLp) - storeOnlyPoints) < 1e-3f, "album chart racks off restores store points");

			// --lp-reception-upper=K:U.
			LpReception.SetForChecks(1.5f);
			float symAt2 = LpReception.Multiplier(2.0), symAtMinus1 = LpReception.Multiplier(-1.0);
			Check(!LpReception.UpperEnabled && Math.Abs(symAt2 - (float)Math.Exp(3.0 - 1.125)) < 1e-4f, "without the upper switch the symmetric formula is unchanged");
			foreach (var (k, u) in new[] { (0.0, 1.0), (1.0, 0.5) }) {
				LpReception.SetForChecks(1.5f, k, u);
				double mean = 0; const int n = 200000;
				for (int i = 0; i < n; i++) { double z = LpReception.Z(2002UL, "gen_up_" + i); mean += LpReception.Multiplier(z); }
				mean /= n;
				Check(LpReception.UpperEnabled && Math.Abs(mean - 1.0) < 0.02, $"upper {k}:{u} keeps the mean multiplier at 1 (got {mean:F3})");
				Check(Math.Abs(LpReception.Multiplier(-1.0) / LpReception.Multiplier(-2.0) - Math.Exp(1.5)) < 1e-3,
					$"upper {k}:{u} keeps slope 1.5 below the knee");
				Check(Math.Abs(LpReception.Multiplier(k + 2.0) / LpReception.Multiplier(k + 1.0) - Math.Exp(u)) < 1e-3,
					$"upper {k}:{u} uses slope {u} above the knee");
				Check(LpReception.Multiplier(2.0) < symAt2 && LpReception.Multiplier(-1.0) > symAtMinus1,
					$"upper {k}:{u} thins the top and lifts the bottom against the symmetric draw");
			}
			LpReception.SetForChecks(0f);
			Check(!LpReception.UpperEnabled && LpReception.Factor(new RecordRuntimeData { baseRecord = new Record { recordId = "gen_check_3", artistId = "a", format = ReleaseFormat.Album } }) == 1f,
				"off clears the upper shape");

			// Renewal earned option.
			Check(!RenewalEarnedOption.Decide(5000f, 0, 1f, 1000f), "an act never released under the deal lapses");
			Check(RenewalEarnedOption.Decide(0f, 2, 0f, 1000f) && RenewalEarnedOption.Decide(1f, 1, 0f, 0f), "a deal that broke even or paid is renewed");
			Check(RenewalEarnedOption.Decide(-400f, 2, 0.5f, 1000f) && !RenewalEarnedOption.Decide(-600f, 2, 0.5f, 1000f), "loyalty carries a loss up to loyalty x the next advance");
			Check(!RenewalEarnedOption.Decide(-1f, 3, 0f, 5000f), "a label with no loyalty carries no loss");

			// Renewal live evidence.
			Check(RenewalLiveEvidence.IsEvidence(55, 0, 0) && RenewalLiveEvidence.IsEvidence(0, 40, 0) && RenewalLiveEvidence.IsEvidence(0, 0, 1),
				"charting now, charted, or broken out each count");
			Check(!RenewalLiveEvidence.IsEvidence(0, 0, 0) && !RenewalLiveEvidence.IsEvidence(0, 150, 0), "nothing, or an off-chart peak, is no evidence");
			var clock = new SimulatedArtist { contractExpiresWeek = 100, contractSinglesObligation = 4, contractReleases = 4 };
			Check(RosterManager.IsContractMatured(clock, 1961, 60) && !RenewalLiveEvidence.TermMatured(clock, 1961, 60) && RenewalLiveEvidence.TermMatured(clock, 1961, 100),
				"with the switch a delivered obligation waits for the date");

			// Staggered opening contracts.
			StaggeredOpeningContracts.SetForChecks(false);
			Check(StaggeredOpeningContracts.ExpiryWeek("artist_00001", 1961, 1960) == 52, "off must keep the year boundary");
			StaggeredOpeningContracts.SetForChecks(true);
			int[] weeks = Enumerable.Range(1, 2000).Select(i => StaggeredOpeningContracts.ExpiryWeek($"artist_{i:D5}", 1961, 1960)).ToArray();
			Check(weeks.Min() >= 1 && weeks.Max() <= 52, "a 1961 expiry must fall in weeks 1-52");
			Check(weeks.Distinct().Count() == 52, "every week must be used");
			Check(weeks.GroupBy(w => (w - 1) / 13).All(g => g.Count() > 400 && g.Count() < 600), "expiries must spread evenly over the quarters");
			Check(StaggeredOpeningContracts.ExpiryWeek("artist_00007", 1963, 1960) is >= 105 and <= 156, "a later expiry keeps its year");
			Check(StaggeredOpeningContracts.ExpiryWeek("artist_00007", 1963, 1960) == StaggeredOpeningContracts.ExpiryWeek("artist_00007", 1963, 1960),
				"the week must be stable");

			// Receivable-backed overdraft.
			ReceivableBackedOverdraft.SetForChecks(0f);
			Check(!ReceivableBackedOverdraft.Carries(-100f, 1000f), "off must never carry");
			Check(ReceivableBackedOverdraft.MonthlyCharge > 0f && ReceivableBackedOverdraft.MonthlyCharge <= 0.05f, "the charge must be a monthly rate");
			ReceivableBackedOverdraft.SetForChecks(0.5f);
			Check(ReceivableBackedOverdraft.Carries(-400f, 1000f), "an overdraft inside half the receivables is carried");
			Check(!ReceivableBackedOverdraft.Carries(-600f, 1000f), "an overdraft past half the receivables is not");
			Check(!ReceivableBackedOverdraft.Carries(50f, 1000f) && !ReceivableBackedOverdraft.Carries(-50f, 0f), "no overdraft or no receivables: nothing to carry");
			GD.Print("GAMEY_AUDIT_SWITCHES_CHECK passed");
		} finally {
			LpEarnedDiscovery.SetForChecks(floorWas);
			StaggeredOpeningContracts.SetForChecks(staggerWas);
			LpEarnedLaunch.SetForChecks(launchWas);
			AnniversaryOpeningContracts.SetForChecks(anniversaryWas);
			LpLabelDemandParity.SetForChecks(parityWas);
			ReceivableBackedOverdraft.SetForChecks(shareWas);
		}
	}
}
