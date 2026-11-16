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
				st.peakPosition = 4; st.baseRecord.album.albumFormat = AlbumFormat.Standard;
				Check(SoundtrackHitTail.Renewal(st) == 1f, "a non-soundtrack is untouched");
				SoundtrackHitTail.SetForChecks(0f);
				OpeningSoundtrackCatalogue.SetForChecks(0);
				Check(!OpeningSoundtrackCatalogue.Enabled && OpeningSoundtrackCatalogue.Count == 0, "off seeds no opening soundtracks");
				OpeningSoundtrackCatalogue.SetForChecks(8);
				Check(OpeningSoundtrackCatalogue.Enabled && OpeningSoundtrackCatalogue.Count == 8, "opening soundtrack count reads");
				OpeningSoundtrackCatalogue.SetForChecks(0);
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
			LpReception.SetForChecks(0f);

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
