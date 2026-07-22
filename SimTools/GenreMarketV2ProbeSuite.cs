using System;
using System.Collections.Generic;
using System.Linq;

public static class GenreMarketV2ProbeSuite {
	public static IReadOnlyList<string> Run() {
		var results = new List<string>();
		GenreCatalog.Validate();
		Require(GenreCatalog.All.Count == 42, "catalog count");
		Require(GenreCatalog.All.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() == 42, "stable ids unique");
		foreach (GenreProfile p in GenreCatalog.All) {
			Require(p.BaselineKeyframes.Length == 7, p.Id + " keyframes");
			int[] years = { 1960, 1962, 1964, 1966, 1967, 1968, 1969 };
			for (int i = 0; i < years.Length; i++) Require(Math.Abs(p.GetBaseline(years[i]) - p.BaselineKeyframes[i]) < 0.000001f, p.Id + " baseline " + years[i]);
			Require(Math.Abs(p.GetBaseline(1950) - p.BaselineKeyframes[0]) < 0.000001f, p.Id + " pre clamp");
			Require(Math.Abs(p.GetBaseline(1975) - p.BaselineKeyframes[6]) < 0.000001f, p.Id + " post clamp");
			Require(Math.Abs(p.GetBaseline(1961) - (p.BaselineKeyframes[0] + p.BaselineKeyframes[1]) / 2f) < 0.000001f, p.Id + " interpolation");
		}
		Require(GenreCatalog.Get(Genre.AcidRock).GetLifecycle(1962) == GenreLifecycleState.PreEmergent, "pre-emergent lifecycle");
		Require(GenreCatalog.Get(Genre.AcidRock).GetLifecycle(1966) == GenreLifecycleState.Emerging, "emerging lifecycle");
		Require(GenreCatalog.Get(Genre.DooWop).GetLifecycle(1967) == GenreLifecycleState.Legacy, "legacy lifecycle");
		ProbeMigration(Genre.Psychedelic, null, 1966, Genre.PsychedelicRock, Array.Empty<string>());
		ProbeMigration(Genre.BritishInvasion, null, 1964, Genre.BritishBeat, new[] { "british" });
		ProbeMigration(Genre.Motown, null, 1964, Genre.Soul, new[] { "motown" });
		ProbeMigration(Genre.Skiffle, null, 1960, Genre.Folk, new[] { "british", "skiffle" });
		ProbeMigration(Genre.GirlGroup, Genre.Soul, 1964, Genre.Soul, new[] { "girl-group" });
		ProbeMigration(Genre.GirlGroup, Genre.Country, 1964, Genre.TeenPop, new[] { "girl-group" });
		ProbeMigration(Genre.SkaRocksteady, null, 1965, Genre.Ska, new[] { "jamaican" });
		ProbeMigration(Genre.SkaRocksteady, null, 1966, Genre.Rocksteady, new[] { "jamaican" });
		ProbeMigration(Genre.SkaRocksteady, null, 1968, Genre.Reggae, new[] { "jamaican" });
		ProbeMigration(Genre.SkaRocksteady, null, 0, Genre.Ska, new[] { "jamaican" });
		ProbeEnabledInitialSeeding();
		Require(GenreNameFormatter.Format(Genre.RnB) == "R&B" && GenreNameFormatter.Format(Genre.Childrens) == "Children's", "canonical formatter");
		results.Add("catalog/keyframe/interpolation/clamp/lifecycle/migration/enabled-seeding/formatter probes passed");
		if (GenreMarketV2.Enabled) {
			string singleReconciliation = ProbePhase2RoutingAndOrientation();
			ProbeSingleDemandStages();
			results.Add("segment normalization/conservation/FM/texture/R&B/format-prior/AI-market and Single demand-stage probes passed; " + singleReconciliation);
		}
		return results;
	}

	private static void ProbeSingleDemandStages() {
		SingleDemandStages neutral = ChartSimulator.CalculateSingleDemandStages(1000f, .20f, 1f, 1f, 1f, .5f, .5f, 1f, 1f);
		SingleDemandStages weak = ChartSimulator.CalculateSingleDemandStages(1000f, .20f, .8f, .8f, .8f, .5f, .5f, 1f, 1f);
		SingleDemandStages chart = ChartSimulator.CalculateSingleDemandStages(1000f, .20f, 1.5f, 1f, 1f, .5f, .5f, 1f, 1f);
		SingleDemandStages stacked = ChartSimulator.CalculateSingleDemandStages(1000f, .20f, 1.5f, 1.5f, 1.5f, .5f, .5f, 1f, 1f);
		SingleDemandStages stronger = ChartSimulator.CalculateSingleDemandStages(1000f, .20f, 2f, 1.5f, 1.5f, .5f, .5f, 1f, 1f);
		Require(Math.Abs(neutral.AwareBuyers - 200f) < .0001f && neutral.AwareBuyers <= neutral.PotentialAudience,
			"Single discovery neutral/bounded awareness");
		Require(chart.AwareBuyers > neutral.AwareBuyers && stacked.AwareBuyers > chart.AwareBuyers && stronger.AwareBuyers > stacked.AwareBuyers,
			"Single discovery monotonicity");
		Require(weak.AwareBuyers < neutral.AwareBuyers, "Single discovery below-neutral suppression");
		Require((stronger.AwareBuyers - stacked.AwareBuyers) < (chart.AwareBuyers - neutral.AwareBuyers),
			"Single discovery diminishing returns");
		Require(Math.Abs(neutral.AwareBuyers * neutral.IntrinsicConversionRate - 7f) < .0001f,
			"Single raw-demand stage reconstruction");
		SingleDemandStages oneWeak = ChartSimulator.CalculateSingleDemandStages(1000f, .20f, .8f, 1f, 1f, .5f, .5f, 1f, 1f);
		Require(Math.Abs(weak.AwareBuyers - (1000f / 6f)) < .0001f && oneWeak.AwareBuyers > weak.AwareBuyers,
			"correlated discovery uses a geometric mean rather than multiplying three audience copies");
	}

	private static string ProbePhase2RoutingAndOrientation() {
		MarketRegion neutral = CreateRegion("greatlakes", fm: true, integration: .5f, church: .25f);
		foreach (GenreProfile profile in GenreCatalog.All) {
			float sum = profile.SegmentWeights.Values.Sum();
			Require(Math.Abs(sum - 1f) < .000001f, profile.Id + " segment normalization");
		}
		float jazzBaseline = GenreCatalog.Get(Genre.Jazz).GetBaseline(1964f);
		Require(Math.Abs(GenreAcceptanceService.GetRegionalDemandAcceptance(Genre.Jazz, Genre.Jazz, neutral, 1964f) - jazzBaseline) < .00001f, "one-population conservation");
		Require(Math.Abs(GenreAcceptanceService.GetEnabledSingleDemandMultiplier(0f)) < .000001f, "single demand zero transfer");
		Require(GenreAcceptanceService.GetEnabledSingleDemandMultiplier(.01f) < .03f, "single demand near-zero transfer");
		Require(Math.Abs(GenreAcceptanceService.GetEnabledSingleDemandMultiplier(.5f) - .85f) < .000001f, "single demand legacy midpoint transfer");
		Require(Math.Abs(GenreAcceptanceService.GetEnabledSingleDemandMultiplier(1f) - 1.10f) < .000001f, "single demand legacy high-end transfer");
		GenreAcceptanceService.SingleOpportunityReconciliation single1960 =
			GenreAcceptanceService.GetSingleOpportunityReconciliation(new[] { neutral }, 1960f);
		GenreAcceptanceService.SingleOpportunityReconciliation single1968 =
			GenreAcceptanceService.GetSingleOpportunityReconciliation(new[] { neutral }, 1968f);
		GenreAcceptanceService.SingleOpportunityReconciliation single1964 =
			GenreAcceptanceService.GetSingleOpportunityReconciliation(new[] { neutral }, 1964f);
		GenreAcceptanceService.SingleOpportunityReconciliation single1966 =
			GenreAcceptanceService.GetSingleOpportunityReconciliation(new[] { neutral }, 1966f);
		GenreAcceptanceService.SingleOpportunityReconciliation single1969 =
			GenreAcceptanceService.GetSingleOpportunityReconciliation(new[] { neutral }, 1969f);
		Require(Math.Abs(single1960.Normalization - 1f) < .000001f,
			"Single supplied-portfolio reconciliation is neutral at the 1960 anchor");
		Require(Math.Abs(single1964.Normalization - 1f) < .000001f,
			"Single supplied-portfolio reconciliation preserves the pre-expansion boundary");
		Require(single1968.Normalization >= .90f && single1968.Normalization <= 1.10f &&
			single1968.Normalization < single1960.Normalization,
			"Single supplied-portfolio reconciliation is bounded and corrects late drift");
		Require(Math.Abs(GenreAcceptanceService.GetLiveSingleOpportunityNormalization(new[] { neutral }, 1968f, live: false) - 1f) < .000001f,
			"disabled Single supplied-portfolio reconciliation is neutral");
		// Fixed-input Album seam: compare the routed calculation with the accepted
		// regional baseline before any record count, awareness, stock, or quality can
		// influence the result.  These are shared 1960 genres in both paths.
		Genre[] albumProbeGenres = { Genre.TraditionalPop, Genre.Jazz, Genre.Folk, Genre.Country, Genre.Gospel, Genre.RnB, Genre.DooWop };
		neutral.SetGenreMarketV2Live(true);
		foreach (Genre genre in albumProbeGenres) {
			MarketRegion.AlbumDemandExplanation album = neutral.GetAlbumDemandExplanation(genre, 1960f);
			float enabledPool = neutral.GetAlbumMarketSize(genre, 1960);
			Require(Math.Abs(enabledPool - album.AcceptedPreTiltBuyerPool) < .001f,
				"Album accepted pre-tilt buyer-pool reconciliation " + genre);
			float acceptedAlbumOpportunity = neutral.GetAcceptedAlbumOpportunityWeight(genre, 1960f);
			float singleTilt = GenreAcceptanceService.GetFormatMultiplier(genre, genre, ReleaseFormat.Single, 1960f, acceptedAlbumOpportunity);
			float albumTilt = GenreAcceptanceService.GetFormatMultiplier(genre, genre, ReleaseFormat.Album, 1960f, acceptedAlbumOpportunity);
			Require(acceptedAlbumOpportunity > 0f && Math.Abs((1f - acceptedAlbumOpportunity) * singleTilt + acceptedAlbumOpportunity * albumTilt - 1f) < .000001f,
				"Album accepted opportunity centering " + genre);
			CompetitorManager.AlbumPriorExplanation prior = CompetitorManager.GetAlbumPriorExplanation(genre,
				new[] { neutral }, 1960, live: true);
			Require(Math.Abs(prior.AcceptedAlbumPool - album.AcceptedPreTiltBuyerPool) < .001f &&
				Math.Abs(prior.AcceptedLegacyGenrePool - neutral.GetAcceptedLegacyGenreMarketSize(genre, 1960f)) < .001f,
				"Album AI-prior accepted pool decomposition " + genre);
			Require(Math.Abs(prior.UntiltedAlbumDemandFactor - acceptedAlbumOpportunity) < .000001f &&
				Math.Abs(prior.FormatTilt - albumTilt) < .000001f &&
				Math.Abs(prior.AlbumPrior - prior.UntiltedAlbumDemandFactor * prior.MarketReconciliation * prior.FormatTilt) < .000001f,
				"Album AI-prior denominator/centering/tilt parity " + genre);
			// Fixed cohort: no revenue memory and unit noise. The neutral-orientation
			// counterfactual retains accepted opportunity and changes only the two
			// catalog orientation multipliers.
			CompetitorManager.FormatDecisionExplanation oriented = CompetitorManager.ExplainFixedFormatDecision(
				50000f, 50000f * acceptedAlbumOpportunity, prior.AlbumAffinity, acceptedAlbumOpportunity,
				singleTilt, albumTilt, 1000f, 2500f);
			CompetitorManager.FormatDecisionExplanation neutralOrientation = CompetitorManager.ExplainFixedFormatDecision(
				50000f, 50000f * acceptedAlbumOpportunity, prior.AlbumAffinity, acceptedAlbumOpportunity,
				1f, 1f, 1000f, 2500f);
			Require(Math.Abs(oriented.AcceptedOpportunity - neutralOrientation.AcceptedOpportunity) < .000001f &&
				Math.Abs(oriented.SingleMemoryBlend - (oriented.SinglePreTiltContribution * oriented.SingleTilt - oriented.SingleProductionCost)) < .000001f &&
				Math.Abs(oriented.AlbumMemoryBlend - (oriented.AlbumPreTiltContribution * oriented.AlbumTilt - oriented.AlbumProductionCost)) < .000001f &&
				Math.Abs(oriented.FinalSingleMargin - oriented.SingleMemoryBlend) < .000001f &&
				Math.Abs(oriented.FinalAlbumMargin - oriented.AlbumMemoryBlend) < .000001f,
				"fixed format-decision orientation counterfactual " + genre);
		}
		neutral.SetGenreMarketV2Live(false);
		Require(Math.Abs(CompetitorManager.GetAlbumPriorExplanation(Genre.Gospel, new[] { neutral }, 1960, live: false).MarketReconciliation - 1f) < .000001f,
			"disabled Album prior market reconciliation is neutral");
		Require(neutral.GetAcceptedAlbumOpportunityWeight(Genre.Jazz, 1960f) > 0f, "1960 Album opportunity is nonzero");
		Genre[] suppliedGenres = GenreSupplyService.GetAvailableGenres(1960f).ToArray();
		float[] suppliedMarkets = suppliedGenres.Select(genre => GenreCatalog.Get(genre).GetBaseline(1960f) * 1000000f).ToArray();
		float[] suppliedFactors = suppliedGenres.Select(genre =>
			CompetitorManager.CalculateRelativeSingleMarketFactor(GenreCatalog.Get(genre).GetBaseline(1960f) * 1000000f, suppliedMarkets)).ToArray();
		Require(suppliedFactors.Distinct().Count() > 3 && suppliedFactors.Any(factor => factor < .90f) &&
			suppliedFactors.Any(factor => factor > 1.10f) && suppliedFactors.Count(factor => factor >= 1.30f) < suppliedFactors.Length / 2,
			"supplied AI market factors differentiate without saturation");
		Require(GenreSupplyService.IsAvailableForNewSupply(Genre.EasyListening, 1960f), "established supply available");
		Require(!GenreSupplyService.IsAvailableForNewSupply(Genre.PsychedelicRock, 1965f), "pre-emergent supply unavailable");
		Require(GenreSupplyService.CanRetainExistingProjectGenre(Genre.PsychedelicRock, 1965f),
			"existing pre-emergent identity retains seed-scene genre");
		Require(GenreSupplyService.CanRetainExistingProjectGenre(Genre.Soul, 1960f),
			"existing Soul identity retained before commercial emergence");
		Require(GenreSupplyService.IsAvailableForNewSupply(Genre.PsychedelicRock, 1966f), "emerging supply available");
		Require(!GenreSupplyService.IsAvailableForNewSupply(Genre.DooWop, 1966f), "legacy supply unavailable");
		Require(!GenreSupplyService.IsAvailableForNewSupply(Genre.BritishBeat, 1962f), "British supply absent before bridge");
		Require(!GenreSupplyService.CanRetainExistingProjectGenre(Genre.BritishBeat, 1962f),
			"British identity cannot bypass import bridge");
		Require(GenreSupplyService.IsAvailableForNewSupply(Genre.BritishBeat, 1964f), "British Beat bridge begins in 1964");
		Require(GenreSupplyService.IsAvailableForNewSupply(Genre.BritishPop, 1964f), "British Pop bridge begins in 1964");
		Require(!GenreSupplyService.IsAvailableForNewSupply(Genre.BritishBlues, 1964f) &&
			GenreSupplyService.IsAvailableForNewSupply(Genre.BritishBlues, 1965f), "British Blues bridge timing");
		Require(GenreSupplyService.GetAvailableGenres(1960f).Contains(Genre.Blues), "Blues supply included");
		Require(GenreSupplyService.GetAvailableGenres(1960f).Contains(Genre.Classical), "Classical supply included");
		Require(GenreSupplyService.GetAvailableGenres(1960f).Contains(Genre.Childrens), "Childrens supply included");
		var supplyArtist = new SimulatedArtist { primaryGenre = Genre.Soul, secondaryGenre = Genre.RnB };
		Require(GenreSupplyService.ChooseGenre(null, supplyArtist, null, 1967f, null, .50f) == Genre.Soul,
			"established artist identity retention");
		Require(GenreSupplyService.ChooseGenre(null, supplyArtist, null, 1962f, null, .99f) != Genre.BritishBeat,
			"exploration excludes pre-bridge British supply");
		var legacyArtist = new SimulatedArtist { primaryGenre = Genre.DooWop, isActive = true, weeksSinceLastRelease = 52 };
		Require(!GenreSupplyService.IsAvailableForNewSupply(Genre.DooWop, 1967f) &&
			GenreSupplyService.IsEligibleExistingArtistForRelease(legacyArtist) &&
			GenreSupplyService.CanRetainExistingProjectGenre(Genre.DooWop, 1967f), "legacy artist release and project transition eligibility");
		Require(GenreSupplyService.ChooseGenre(null, legacyArtist, null, 1967f, null, .119f) == Genre.DooWop &&
			GenreSupplyService.ChooseGenre(null, legacyArtist, null, 1967f, null, .121f) != Genre.DooWop,
			"legacy identity retention threshold");
		ProbeProspectivePsychedelicCompatibility();
		var legacyOnlyLabel = new AILabel { roster = new List<SimulatedArtist> { legacyArtist } };
		Require(legacyOnlyLabel.CountArtistsEligibleForRelease(1967) == 1, "legacy-only roster has no phantom release roll");
		var globallyConcentrated = new Dictionary<Genre, int> { [Genre.Soul] = 20, [Genre.Country] = 1 };
		var globallyBalanced = new Dictionary<Genre, int> { [Genre.TraditionalPop] = 20, [Genre.Country] = 1 };
		float concentratedSoulRelativeWeight = GenreSupplyService.GetSupplyWeight(Genre.Soul, null, null, null, 1961f, null, globallyConcentrated) /
			GenreSupplyService.GetSupplyWeight(Genre.Country, null, null, null, 1961f, null, globallyConcentrated);
		float balancedSoulRelativeWeight = GenreSupplyService.GetSupplyWeight(Genre.Soul, null, null, null, 1961f, null, globallyBalanced) /
			GenreSupplyService.GetSupplyWeight(Genre.Country, null, null, null, 1961f, null, globallyBalanced);
		Require(concentratedSoulRelativeWeight < balancedSoulRelativeWeight, "global supply concentration reduces relative selection weight");
		MarketRegion supplySouthwest = CreateRegion("southwest", fm: true, integration: .5f, church: .25f);
		MarketRegion supplyEastCoast = CreateRegion("eastcoast", fm: true, integration: .5f, church: .25f);
		supplySouthwest.SetGenreMarketV2Live(true);
		supplyEastCoast.SetGenreMarketV2Live(true);
		float routedSupplyRatio = (.20f + .80f * supplySouthwest.GetGenreAcceptance(Genre.Country, 1964f)) /
			(.20f + .80f * supplyEastCoast.GetGenreAcceptance(Genre.Country, 1964f));
		float protectedSupplyRatio = (.20f + .80f * GenreSupplyService.GetProspectiveSupplyAcceptanceForProbe(Genre.Country, supplySouthwest, 1964f)) /
			(.20f + .80f * GenreSupplyService.GetProspectiveSupplyAcceptanceForProbe(Genre.Country, supplyEastCoast, 1964f));
		float actualSupplyRatio = GenreSupplyService.GetSupplyWeight(Genre.Country, null, null, supplySouthwest, 1964f) /
			GenreSupplyService.GetSupplyWeight(Genre.Country, null, null, supplyEastCoast, 1964f);
		Require(Math.Abs(actualSupplyRatio - protectedSupplyRatio) < .000001f &&
			Math.Abs(actualSupplyRatio - routedSupplyRatio) > .000001f,
			"prospective supply bypasses live routed texture");
		float jazzProtectedWeight = GenreSupplyService.GetSupplyWeight(Genre.Jazz, null, null, supplyEastCoast, 1964f);
		float countryProtectedWeight = GenreSupplyService.GetSupplyWeight(Genre.Country, null, null, supplySouthwest, 1964f);
		float texMexProtectedWeight = GenreSupplyService.GetSupplyWeight(Genre.TexMex, null, null, supplySouthwest, 1964f);
		Require(Math.Abs(jazzProtectedWeight - .52f) < .000001f && Math.Abs(countryProtectedWeight - .64f) < .000001f &&
			Math.Abs(texMexProtectedWeight - .40f) < .000001f,
			$"protected prospective supply weights match explicit pre-texture V2 values actual={jazzProtectedWeight:F6}/{countryProtectedWeight:F6}/{texMexProtectedWeight:F6}");
		Genre[] specialistCandidates = { Genre.Country, Genre.TexMex };
		Require(GenreSupplyService.ChooseGenre(null, null, supplySouthwest, 1964f, null, .61f, specialistCandidates) == Genre.Country &&
			GenreSupplyService.ChooseGenre(null, null, supplySouthwest, 1964f, null, .62f, specialistCandidates) == Genre.TexMex,
			"protected prospective specialist selections match explicit pre-texture V2 boundaries");
		var britishBeatCandidates = new[] { Genre.BritishBeat, Genre.Country };
		var britishPopCandidates = new[] { Genre.BritishPop, Genre.Country };
		Require(GenreSupplyService.ChooseGenre(null, null, null, 1962f, null, 0f, britishBeatCandidates) == Genre.Country &&
			GenreSupplyService.ChooseGenre(null, null, null, 1962f, null, 0f, britishPopCandidates) == Genre.Country,
			"British Beat and Pop are unselectable before bridge");
		Require(GenreSupplyService.ChooseGenre(null, null, null, 1964f, null, 0f, britishBeatCandidates) == Genre.BritishBeat &&
			GenreSupplyService.ChooseGenre(null, null, null, 1964f, null, 0f, britishPopCandidates) == Genre.BritishPop,
			"British Beat and Pop are selectable at bridge");
		GenreAcceptanceExplanation momentumProbe = GenreAcceptanceService.Evaluate(Genre.BritishBeat, neutral, AudienceSegment.MainstreamAM, 1960f, 1f);
		Require(Math.Abs(momentumProbe.MomentumContribution - .3f) < .000001f, "legacy momentum configured influence");

		MarketRegion noFm = CreateRegion("westcoast", fm: false, integration: .5f, church: .25f);
		MarketRegion yesFm = CreateRegion("westcoast", fm: true, integration: .5f, church: .25f);
		Require(GenreAcceptanceService.GetRegionalRadioOpportunity(Genre.ProgressiveRock, Genre.ProgressiveRock, yesFm, 1968f) >
			GenreAcceptanceService.GetRegionalRadioOpportunity(Genre.ProgressiveRock, Genre.ProgressiveRock, noFm, 1968f), "FM gating");
		float routedProg = GenreAcceptanceService.GetRegionalDemandAcceptance(Genre.ProgressiveRock, Genre.ProgressiveRock, yesFm, 1968f);
		Require(Math.Abs(GenreAcceptanceService.GetRegionalRadioOpportunity(Genre.ProgressiveRock, Genre.ProgressiveRock, yesFm, 1968f) -
			GenreAcceptanceService.GetRegionalRadioOpportunity(Genre.ProgressiveRock, yesFm, 1968f, routedProg)) < .000001f,
			"cached radio opportunity parity");

		string specialistRouting = ProbeCenteredSpecialistTextures();
		string repairSurfaces = ProbeSpecialistFulfillmentAndEmergingMemoryRepairs();
		string rosterLifecycle = ProbeDroppedArtistRosterLifecycle();
		MarketRegion south = CreateRegion("deepsouth", fm: true, integration: .5f, church: .80f);
		MarketRegion coast = CreateRegion("eastcoast", fm: true, integration: .5f, church: .10f);
		MarketRegion southwest = CreateRegion("southwest", fm: true, integration: .5f, church: .25f);
		Require(GenreAcceptanceService.GetRegionalDemandAcceptance(Genre.Country, Genre.Country, south, 1964f) >
			GenreAcceptanceService.GetRegionalDemandAcceptance(Genre.Country, Genre.Country, coast, 1964f), "regional texture");
		Require(GenreAcceptanceService.GetRegionalDemandAcceptance(Genre.TexMex, Genre.TexMex, southwest, 1964f) >
			GenreAcceptanceService.GetRegionalDemandAcceptance(Genre.TexMex, Genre.TexMex, south, 1964f), "TexMex Southwest routing");
		MarketRegion lowIntegration = CreateRegion("eastcoast", fm: true, integration: 0f, church: .25f);
		MarketRegion highIntegration = CreateRegion("eastcoast", fm: true, integration: 1f, church: .25f);
		Require(GenreAcceptanceService.Evaluate(Genre.RnB, highIntegration, AudienceSegment.MainstreamAM, 1964f, 0f).Effective >
			GenreAcceptanceService.Evaluate(Genre.RnB, lowIntegration, AudienceSegment.MainstreamAM, 1964f, 0f).Effective, "R&B integration crossover");

		float bubbleSingle = GenreAcceptanceService.GetFormatMultiplier(Genre.Bubblegum, Genre.Bubblegum, ReleaseFormat.Single, 1968f);
		float bubbleAlbum = GenreAcceptanceService.GetFormatMultiplier(Genre.Bubblegum, Genre.Bubblegum, ReleaseFormat.Album, 1968f);
		float progSingle = GenreAcceptanceService.GetFormatMultiplier(Genre.ProgressiveRock, Genre.ProgressiveRock, ReleaseFormat.Single, 1968f);
		float progAlbum = GenreAcceptanceService.GetFormatMultiplier(Genre.ProgressiveRock, Genre.ProgressiveRock, ReleaseFormat.Album, 1968f);
		Require(Math.Abs((bubbleSingle + bubbleAlbum) - 2f) <= .000001f && Math.Abs((progSingle + progAlbum) - 2f) <= .000001f, "combined format opportunity conservation");
		Require(bubbleSingle > bubbleAlbum && progAlbum > progSingle, "centered orientation contrast");
		Require(Math.Abs(GenreAcceptanceService.GetLiveFormatMultiplier(Genre.Bubblegum, Genre.Bubblegum,
			ReleaseFormat.Single, 1968f, .5f, live: false) - 1f) < .000001f, "prewarm format neutrality");
		Require(Math.Abs(GenreAcceptanceService.GetLiveFormatMultiplier(Genre.Bubblegum, Genre.Bubblegum,
			ReleaseFormat.Single, 1968f, .5f, live: true) - bubbleSingle) < .000001f, "live format activation");
		Require(Math.Abs(bubbleSingle - CompetitorManager.GetFormatPriorMultiplier(Genre.Bubblegum, ReleaseFormat.Single, 1968,
			liveOverride: true, albumOpportunityOverride: .5f)) < .000001f &&
			Math.Abs(progAlbum - CompetitorManager.GetFormatPriorMultiplier(Genre.ProgressiveRock, ReleaseFormat.Album, 1968,
				liveOverride: true, albumOpportunityOverride: .5f)) < .000001f, "AI realized format-prior parity");
		Require(Math.Abs(CompetitorManager.GetFormatPriorMultiplier(Genre.Bubblegum, ReleaseFormat.Single, 1968, liveOverride: false) - 1f) < .000001f,
			"AI prior prewarm neutrality");
		float earlySingle = GenreAcceptanceService.GetFormatMultiplier(Genre.Bubblegum, Genre.Bubblegum, ReleaseFormat.Single, 1960f, 0f);
		float earlyAlbum = GenreAcceptanceService.GetFormatMultiplier(Genre.Bubblegum, Genre.Bubblegum, ReleaseFormat.Album, 1960f, 0f);
		Require(Math.Abs(earlySingle - 1f) < .000001f && earlyAlbum < 1f, "early Single-market conservation");
		const float albumOpportunity = .25f;
		float weightedSingle = GenreAcceptanceService.GetFormatMultiplier(Genre.ProgressiveRock, Genre.ProgressiveRock, ReleaseFormat.Single, 1966f, albumOpportunity);
		float weightedAlbum = GenreAcceptanceService.GetFormatMultiplier(Genre.ProgressiveRock, Genre.ProgressiveRock, ReleaseFormat.Album, 1966f, albumOpportunity);
		Require(Math.Abs((1f - albumOpportunity) * weightedSingle + albumOpportunity * weightedAlbum - 1f) < .000001f,
			"era-weighted format opportunity conservation");
		return $"Single portfolio normalization 1960/64/66/68/69={single1960.Normalization:F4}/" +
			$"{single1964.Normalization:F4}/{single1966.Normalization:F4}/{single1968.Normalization:F4}/{single1969.Normalization:F4}, " +
			$"enabled/accepted drift {single1960.EnabledToAcceptedRatio:F4}->{single1969.EnabledToAcceptedRatio:F4}; {specialistRouting}; {repairSurfaces}; {rosterLifecycle}";
	}

	private static void ProbeProspectivePsychedelicCompatibility() {
		const float year = 1966f;
		Genre[] candidates = { Genre.PsychedelicRock, Genre.Country };
		Require(!GenreSupplyService.IsPsychedelicTransitionCompatible(Genre.TeenPop, year) &&
			!GenreSupplyService.IsPsychedelicTransitionCompatible(Genre.DooWop, year),
			"Teen Pop and Doo Wop prospective Psychedelic transitions rejected");
		Require(GenreSupplyService.IsPsychedelicTransitionCompatible(Genre.RockAndRoll, year) &&
			GenreSupplyService.IsPsychedelicTransitionCompatible(Genre.AcidRock, year),
			"authored family and adjacency Psychedelic transitions retained");
		Require(GenreSupplyService.GetProspectivePsychedelicCandidatesForProbe(candidates, Genre.TeenPop, year, true)
			.SequenceEqual(new[] { Genre.Country }) &&
			GenreSupplyService.GetProspectivePsychedelicCandidatesForProbe(candidates, Genre.TeenPop, year, false)
			.SequenceEqual(candidates), "Psychedelic compatibility is static and disabled/prewarm bypass is exact");
		var teen = new SimulatedArtist { primaryGenre = Genre.TeenPop, secondaryGenre = Genre.TraditionalPop };
		GenreSelectionFromAnnualFloor(teen, year, new[] { Genre.PsychedelicRock });
		var retained = new SimulatedArtist { primaryGenre = Genre.PsychedelicRock, secondaryGenre = Genre.RockAndRoll };
		GenreSupplyService.GenreSelection retainedEnabled = GenreSupplyService.ChooseGenreWithSelection(null, retained, null, year, null, .01f,
			globalRecentSupply: null, applyPsychedelicTransitionCompatibility: true);
		GenreSupplyService.GenreSelection retainedBaseline = GenreSupplyService.ChooseGenreWithSelection(null, retained, null, year, null, .01f);
		Require(retainedEnabled.RetainedIdentity && retainedBaseline.RetainedIdentity && retainedEnabled.Genre == retainedBaseline.Genre,
			"retained Psychedelic identity bypass is neutral");
		MarketRegion neutral = CreateRegion("greatlakes", fm: true, integration: .5f, church: .25f);
		float blended = GenreAcceptanceService.GetRegionalDemandAcceptance(Genre.PsychedelicRock, Genre.RockAndRoll, neutral, year);
		float expectedBlend = .8f * GenreAcceptanceService.GetRegionalDemandAcceptance(Genre.PsychedelicRock, Genre.PsychedelicRock, neutral, year) +
			.2f * GenreAcceptanceService.GetRegionalDemandAcceptance(Genre.RockAndRoll, Genre.RockAndRoll, neutral, year);
		Require(Math.Abs(blended - expectedBlend) < .000001f, "compatible Psychedelic record retains 80/20 blend");
	}

	private static void GenreSelectionFromAnnualFloor(SimulatedArtist artist, float year, IReadOnlyList<Genre> annualFloorCandidates) {
		const float roll = .137f;
		GenreSupplyService.GenreSelection fallback = GenreSupplyService.ChooseGenreWithSelection(null, artist, null, year, null, roll,
			annualFloorCandidates, null, applyPsychedelicTransitionCompatibility: true);
		GenreSupplyService.GenreSelection normal = GenreSupplyService.ChooseGenreWithSelection(null, artist, null, year, null, roll,
			GenreSupplyService.GetAvailableGenres(year), null, applyPsychedelicTransitionCompatibility: true);
		Require(fallback.Genre == normal.Genre && fallback.Genre != Genre.PsychedelicRock && !fallback.RetainedIdentity &&
			!fallback.UsedCandidateOverride,
			"annual-floor fallback reroutes with the same roll and reports weighted transition");
	}

	private static string ProbeCenteredSpecialistTextures() {
		const float year = 1968f;
		float Texture(Genre genre, string region) => GenreAcceptanceService.GetCenteredSpecialistTextureForProbe(genre, year, region);
		Require(Texture(Genre.Country, "southwest") > Texture(Genre.Country, "deepsouth") &&
			Texture(Genre.Country, "southwest") > Texture(Genre.Country, "greatplains") &&
			Texture(Genre.Country, "deepsouth") > Texture(Genre.Country, "eastcoast") &&
			Texture(Genre.Country, "greatplains") > Texture(Genre.Country, "eastcoast"), "Country centered specialist order");
		Require(Texture(Genre.TexMex, "southwest") > Texture(Genre.TexMex, "deepsouth") &&
			Texture(Genre.TexMex, "southwest") > Texture(Genre.TexMex, "greatplains") &&
			Texture(Genre.TexMex, "deepsouth") > Texture(Genre.TexMex, "eastcoast") &&
			Texture(Genre.TexMex, "greatplains") > Texture(Genre.TexMex, "eastcoast"), "TexMex centered specialist order");
		Require(new[] { "greatlakes", "greatplains", "deepsouth", "southwest", "rockies", "westcoast" }
			.All(region => Texture(Genre.Boogaloo, "eastcoast") > Texture(Genre.Boogaloo, region)), "Boogaloo centered specialist order");
		// Centered texture is population-conserved before clamping.  The fixed
		// Country route may lose a bounded amount at the acceptance cap, so enforce
		// a 1% post-routing tolerance against the explicit texture-free V2 baseline.
		const float nationalOpportunityTolerance = .01f;
		var routing = new List<string>();
		foreach (Genre specialist in new[] { Genre.Country, Genre.TexMex, Genre.Boogaloo }) {
			SpecialistRoutingProbe postRouting = GenreAcceptanceService.GetFixedInputSpecialistRoutingProbe(specialist, year);
			Require(float.IsFinite(postRouting.EffectiveAcceptance) && postRouting.EffectiveAcceptance >= 0f && postRouting.EffectiveAcceptance <= 1f &&
				float.IsFinite(postRouting.ClampLoss) && postRouting.ClampLoss <= .000001f &&
				float.IsFinite(postRouting.FinalSingleOpportunity) && postRouting.FinalSingleOpportunity > 0f,
				specialist + " post-routing specialist probe");
			Require(Math.Abs(postRouting.EffectiveAcceptance - postRouting.ProtectedEffectiveAcceptance) <= nationalOpportunityTolerance &&
				Math.Abs(postRouting.ClampLoss - postRouting.ProtectedClampLoss) <= nationalOpportunityTolerance &&
				Math.Abs(postRouting.NormalizedFinalSingleOpportunity - postRouting.ProtectedFinalSingleOpportunity) < .000001f,
				$"{specialist} population-weighted post-routing opportunity matches protected baseline actual=" +
				$"{postRouting.EffectiveAcceptance:F6}/{postRouting.ClampLoss:F6}/{postRouting.NormalizedFinalSingleOpportunity:F6} protected=" +
				$"{postRouting.ProtectedEffectiveAcceptance:F6}/{postRouting.ProtectedClampLoss:F6}/{postRouting.ProtectedFinalSingleOpportunity:F6}");
			routing.Add($"{specialist} effective/clamp/single={postRouting.EffectiveAcceptance:F4}/{postRouting.ClampLoss:F4}/{postRouting.FinalSingleOpportunity:F4}" +
				$" normalized={postRouting.NormalizedFinalSingleOpportunity:F4} normalizer={postRouting.SingleOpportunityNormalizer:F4}" +
				$" protected={postRouting.ProtectedEffectiveAcceptance:F4}/{postRouting.ProtectedClampLoss:F4}/{postRouting.ProtectedFinalSingleOpportunity:F4}");
		}
		SpecialistRoutingProbe blendedPair = GenreAcceptanceService.GetFixedInputSpecialistRoutingProbe(Genre.TexMex, Genre.Country, year);
		Require(Math.Abs(blendedPair.NormalizedFinalSingleOpportunity - blendedPair.ProtectedFinalSingleOpportunity) < .000001f &&
			GenreAcceptanceService.GetLiveSpecialistSingleOpportunityNormalizer(Genre.TexMex, Genre.Country, (int)year, live: false) == 1f &&
			GenreAcceptanceService.GetLiveSpecialistSingleOpportunityNormalizer(Genre.Jazz, Genre.TraditionalPop, (int)year, live: true) == 1f,
			"blended specialist normalizer is exact and disabled/prewarm-neutral");
		Require(Math.Abs(Texture(Genre.Gospel, "deepsouth") - 1f) < .000001f &&
			Math.Abs(Texture(Genre.RnB, "eastcoast") - 1f) < .000001f, "unaffected genres bypass specialist texture");
		return string.Join(", ", routing);
	}

	private static string ProbeSpecialistFulfillmentAndEmergingMemoryRepairs() {
		string[] regions = { "greatplains", "southwest", "eastcoast" };
		int[] baseline = { 1000, 1000, 1000 };
		int[] texMex = ChartSimulator.AllocateSpecialistInitialStockForProbe(Genre.TexMex, 1968, live: true, regions, baseline);
		int[] disabled = ChartSimulator.AllocateSpecialistInitialStockForProbe(Genre.TexMex, 1968, live: false, regions, baseline);
		int[] unaffected = ChartSimulator.AllocateSpecialistInitialStockForProbe(Genre.Jazz, 1968, live: true, regions, baseline);
		Require(texMex.Sum() == baseline.Sum() && texMex[1] > texMex[0] &&
			disabled.SequenceEqual(baseline) && unaffected.SequenceEqual(baseline),
			"specialist launch stock conserves national budget and is disabled/unaffected neutral");
		Require(ChartManager.IsSpecialistUnchartedRestockEligible(Genre.TexMex, live: true, backorders: 183, rawDemand: 35f) &&
			!ChartManager.IsSpecialistUnchartedRestockEligible(Genre.Jazz, live: true, backorders: 183, rawDemand: 35f) &&
			!ChartManager.IsSpecialistUnchartedRestockEligible(Genre.TexMex, live: false, backorders: 183, rawDemand: 35f),
			"specialist uncharted restock serves physical backorders only when live");
		Require(ChartManager.IsAlbumUnchartedRestockEligible(ReleaseFormat.Album, live: true, backorders: 183, rawDemand: 35f) &&
			!ChartManager.IsAlbumUnchartedRestockEligible(ReleaseFormat.Single, live: true, backorders: 183, rawDemand: 35f) &&
			!ChartManager.IsAlbumUnchartedRestockEligible(ReleaseFormat.Album, live: false, backorders: 183, rawDemand: 35f) &&
			!ChartManager.IsAlbumUnchartedRestockEligible(ReleaseFormat.Album, live: true, backorders: 0, rawDemand: 35f),
			"Album uncharted restock opens only the live physical-backorder path");
		Require(Math.Abs(ChartManager.CalculateRestockDemandSignal(rawDemand: 100f, unitsSold: 40,
				backorders: 50, livePhysicalBackorder: true) - 150f) < .000001f &&
			Math.Abs(ChartManager.CalculateRestockDemandSignal(rawDemand: 100f, unitsSold: 40,
				backorders: 50, livePhysicalBackorder: false) - 91.5f) < .000001f,
			"live physical backorders replenish from full current demand plus recent backlog while the frozen blend is unchanged");
		Require(ChartManager.CalculateRestockAmount(rawDemand: 100f, backorders: 50, demandSignal: 150f,
				serviceLevel: .5f, fulfillAlbumBacklog: true, albumRetailMaturity: 0f) == 100 &&
			ChartManager.CalculateRestockAmount(rawDemand: 100f, backorders: 50, demandSignal: 150f,
				serviceLevel: .5f, fulfillAlbumBacklog: true, albumRetailMaturity: 1f) == 150 &&
			ChartManager.CalculateRestockAmount(rawDemand: 100f, backorders: 50, demandSignal: 150f,
				serviceLevel: .5f, fulfillAlbumBacklog: false, albumRetailMaturity: 1f) == 75 &&
			AlbumModel.GetRetailFulfillmentMaturity(1963) == 0f &&
			AlbumModel.GetRetailFulfillmentMaturity(1964) == 1f,
			"Album backlog avoids duplicate attrition and full retail fulfillment begins at the established-era midpoint");
		var ownedNetwork = new AILabel {
			homeCityId = "new_york",
			distributionRegions = new[] { "greatlakes" }
		};
		string[] liveNodes = DistanceModel.GetDistributionNodesForProbe(ownedNetwork, live: true);
		string[] frozenNodes = DistanceModel.GetDistributionNodesForProbe(ownedNetwork, live: false);
		Require(liveNodes.Contains("new_york") && liveNodes.Contains("chicago") &&
			frozenNodes.SequenceEqual(new[] { "new_york" }),
			"live distance nodes include owned regional distribution while the frozen route remains home-only");
		var scopedDealLabel = new AILabel {
			ownedReach = .4f,
			activeDeal = new DistributionDeal {
				marginSkim = .3f,
				grantedRegions = new[] { "eastcoast" }
			}
		};
		Require(Math.Abs(CompetitorManager.GetSettlementDistributionSkimFractionForProbe(scopedDealLabel,
				new Dictionary<string, int> { ["eastcoast"] = 60, ["westcoast"] = 40 }, 100) - .18f) < .000001f &&
			Math.Abs(CompetitorManager.GetSettlementDistributionSkimFractionForProbe(scopedDealLabel,
				new Dictionary<string, int> { ["eastcoast"] = 60, ["westcoast"] = 40 }, 100,
				liveSettlement: false) - .3f) < .000001f &&
			Math.Abs(CompetitorManager.GetSettlementDistributionSkimFractionForProbe(
				new AILabel { ownedReach = .4f }, new Dictionary<string, int> { ["eastcoast"] = 100 }, 100) - .15f) < .000001f &&
			Math.Abs(CompetitorManager.GetSettlementDistributionSkimFractionForProbe(
				new AILabel { ownedReach = .4f }, new Dictionary<string, int> { ["eastcoast"] = 100 }, 100,
				liveSettlement: false) - .15f) < .000001f,
			"distribution-deal margin is region-scoped only live while frozen deal and no-deal formulas remain unchanged");
		Require(ChartManager.CalculateSpilloverExportBudget(100) == 75 &&
			ChartManager.CalculateSpilloverExportBudget(-1) == 0,
			"bounded one-hop spillover exports at most 75 percent of otherwise-idle local capacity");
		var revision = new FormatMemoryObservation();
		Require(revision.lastRevisionAge == -1 &&
			CompetitorManager.TryAdvanceResponsiveMemoryRevision(revision, 13, finalized: false, out bool firstReplaced, out int firstOrdinal) &&
			!firstReplaced && firstOrdinal == 1 && revision.lastRevisionAge == 13 && !revision.finalized &&
			!CompetitorManager.TryAdvanceResponsiveMemoryRevision(revision, 13, finalized: false, out _, out _) &&
			CompetitorManager.TryAdvanceResponsiveMemoryRevision(revision, 13, finalized: true, out bool finalReplaced, out int finalOrdinal) &&
			finalReplaced && finalOrdinal == 2 && revision.finalized &&
			!CompetitorManager.TryAdvanceResponsiveMemoryRevision(revision, 26, finalized: true, out _, out _),
			"responsive memory revisions have an explicit first/replacement/final lifecycle");
		(float provisionalOutcome, float provisionalExpected) =
			CompetitorManager.GetResponsiveMemoryEconomicsForProbe(lifetimeLabelNet: 1000f,
				sunkProductionCost: 200f, terminalExpectedNet: 1800f, maturity: .25f, finalized: false);
		(float finalOutcome, float finalExpected) =
			CompetitorManager.GetResponsiveMemoryEconomicsForProbe(lifetimeLabelNet: 1000f,
				sunkProductionCost: 200f, terminalExpectedNet: 1800f, maturity: .25f, finalized: true);
		Require(Math.Abs(provisionalOutcome - 3800f) < .000001f &&
			Math.Abs(provisionalExpected - 300f) < .000001f &&
			Math.Abs(finalOutcome - 800f) < .000001f &&
			Math.Abs(finalExpected - 1800f) < .000001f,
			"responsive memory annualizes revenue while charging one-time production cost exactly once");
		Require(Math.Abs(CompetitorManager.GetResponsiveMemoryConfidenceForProbe(12f) - .5f) < .000001f &&
			Math.Abs(CompetitorManager.GetResponsiveMemoryConfidenceForProbe(24f) - .65f) < .000001f &&
			CompetitorManager.GetResponsiveMemoryConfidenceForProbe(-1f) == 0f,
			"responsive memory requires twelve effective observations for half confidence and preserves the 0.65 ceiling");
		(bool underperformingProjectAlbumWins, bool underperformingProjectPromoPreferred, float underperformingProjectGate) =
			CompetitorManager.ResolveAlbumDecision(projectedSingle: 100f, projectedAlbumEligibility: 120f, projectedStandaloneAlbum: 120f,
				componentProjectedAlbumWithPromo: 250f, totalProjectMemoryProjection: 40f, promoProjectDelayPremium: .08f);
		(bool positiveProjectAlbumWins, bool positiveProjectPromoPreferred, float positiveProjectGate) =
			CompetitorManager.ResolveAlbumDecision(projectedSingle: 100f, projectedAlbumEligibility: 90f, projectedStandaloneAlbum: 90f,
				componentProjectedAlbumWithPromo: 250f, totalProjectMemoryProjection: 150f, promoProjectDelayPremium: .08f);
		(bool nonviableProjectAlbumWins, bool nonviableProjectPromoPreferred, float nonviableProjectGate) =
			CompetitorManager.ResolveAlbumDecision(projectedSingle: 100f, projectedAlbumEligibility: 120f, projectedStandaloneAlbum: 120f,
				componentProjectedAlbumWithPromo: 250f, totalProjectMemoryProjection: -10f, promoProjectDelayPremium: .08f);
		(bool componentRejectedAlbumWins, bool componentRejectedPromoPreferred, float componentRejectedGate) =
			CompetitorManager.ResolveAlbumDecision(projectedSingle: 100f, projectedAlbumEligibility: 120f, projectedStandaloneAlbum: 120f,
				componentProjectedAlbumWithPromo: 110f, totalProjectMemoryProjection: 150f, promoProjectDelayPremium: .08f);
		(bool capacityRejectedAlbumWins, bool capacityRejectedPromoPreferred, float capacityRejectedGate) =
			CompetitorManager.ResolveAlbumDecision(projectedSingle: 100f, projectedAlbumEligibility: 120f, projectedStandaloneAlbum: 100f,
				componentProjectedAlbumWithPromo: 190f, totalProjectMemoryProjection: 20f, promoProjectDelayPremium: .08f);
		Require(underperformingProjectAlbumWins && underperformingProjectPromoPreferred && Math.Abs(underperformingProjectGate - 120f) < .000001f &&
			!positiveProjectAlbumWins && positiveProjectPromoPreferred && Math.Abs(positiveProjectGate - 90f) < .000001f &&
			nonviableProjectAlbumWins && !nonviableProjectPromoPreferred && Math.Abs(nonviableProjectGate - 120f) < .000001f &&
			componentRejectedAlbumWins && !componentRejectedPromoPreferred && Math.Abs(componentRejectedGate - 120f) < .000001f &&
			!capacityRejectedAlbumWins && capacityRejectedPromoPreferred && Math.Abs(capacityRejectedGate - 95f) < .000001f,
			"physical Album memory owns eligibility, promo projects clear mean component net per product, components rank strategy, and total-project memory guards only viability");
		Require(!CompetitorManager.IsAlbumProjectSharePressureHighForProbe(99, 99) &&
			!CompetitorManager.IsAlbumProjectSharePressureHighForProbe(100, 66) &&
			CompetitorManager.IsAlbumProjectSharePressureHighForProbe(100, 67) &&
			CompetitorManager.CanScheduleAnnualAlbumProjectForProbe(3, albumProjectPressure: false) &&
			CompetitorManager.CanScheduleAnnualAlbumProjectForProbe(1, albumProjectPressure: true) &&
			!CompetitorManager.CanScheduleAnnualAlbumProjectForProbe(2, albumProjectPressure: true),
			"repeat artist Album workload is bounded only after a sampled year exceeds a two-thirds project mix");
		Require(CompetitorManager.GetApplicableEstimatorLanes(ProjectRecordRole.OrphanSingle)
			.SequenceEqual(new[] { RevenueEstimatorLane.OrphanSingle }) &&
			CompetitorManager.GetApplicableEstimatorLanes(ProjectRecordRole.PromoSingle)
			.SequenceEqual(new[] { RevenueEstimatorLane.PromoSingle }) &&
			CompetitorManager.GetApplicableEstimatorLanes(ProjectRecordRole.LinkedAlbum)
			.SequenceEqual(new[] { RevenueEstimatorLane.AlbumComponent }) &&
			CompetitorManager.GetApplicableEstimatorLanes(ProjectRecordRole.StandaloneAlbum)
			.SequenceEqual(new[] { RevenueEstimatorLane.AlbumComponent, RevenueEstimatorLane.StandaloneAlbum }),
			"physical Album outcomes feed eligibility while strategy estimators remain lane-separated");
		bool nonRetainedEmerging = CompetitorManager.IsNonRetainedEmergingProjectForFormatMemory(Genre.PsychedelicRock,
			retainedIdentity: false, year: 1967, live: true);
		Require(nonRetainedEmerging &&
			Math.Abs(CompetitorManager.GetProjectFormatMemoryConfidence(.98f, nonRetainedEmerging)) < .000001f &&
			Math.Abs(CompetitorManager.GetProjectFormatMemoryConfidence(.98f,
				CompetitorManager.IsNonRetainedEmergingProjectForFormatMemory(Genre.PsychedelicRock, retainedIdentity: true, year: 1967, live: true)) - .98f) < .000001f &&
			Math.Abs(CompetitorManager.GetProjectFormatMemoryConfidence(.98f,
				CompetitorManager.IsNonRetainedEmergingProjectForFormatMemory(Genre.PsychedelicRock, retainedIdentity: false, year: 1967, live: false)) - .98f) < .000001f,
			"non-retained emerging projects bypass only label-wide format memory");
		return $"TexMex launch stock GP/SW={texMex[0]}/{texMex[1]}, Album backlog avoids duplicate service attrition, memory revision lifecycle 1->2 final, one-time cost annualization exact, weighted confidence K=12/cap=.65, Album/project estimator ownership exact, emerging-memory confidence=.0000/.9800";
	}

	private static string ProbeDroppedArtistRosterLifecycle() {
		var oldLabel = new AILabel { labelId = "old", labelName = "Old Label", roster = new List<SimulatedArtist>() };
		var newLabel = new AILabel { labelId = "new", labelName = "New Label", roster = new List<SimulatedArtist>() };
		Genre availableGenre = GenreSupplyService.GetAvailableGenres(1960f).First();
		var artist = new SimulatedArtist {
			artistId = "dropped-probe", stageName = "Dropped Probe", labelId = oldLabel.labelId,
			careerState = CareerState.Dropped, isActive = true, primaryGenre = availableGenre, secondaryGenre = Genre.RnB
		};
		oldLabel.roster.Add(artist);
		var pool = new List<SimulatedArtist>();
		Require(ArtistManager.ReconcileDroppedArtistForProbe(artist, oldLabel, pool, 1960, "terminal career transition") &&
			artist.careerState == CareerState.Dropped && artist.isActive && string.IsNullOrEmpty(artist.labelId) &&
			oldLabel.roster.Count == 0 && pool.Count(candidate => candidate == artist) == 1,
			"Dropped transition atomically clears roster ownership and enters free-agent pool");
		int eventsAfterTransition = artist.careerEvents.Count;
		Require(!ArtistManager.ReconcileDroppedArtistForProbe(artist, oldLabel, pool, 1960, "terminal career transition") &&
			pool.Count(candidate => candidate == artist) == 1 && artist.careerEvents.Count == eventsAfterTransition,
			"Dropped lifecycle reconciliation is idempotent");
		Require(!GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist) &&
			!CompetitorManager.IsEligibleForEnabledFormatDecision(artist) &&
			GenreSupplyService.IsEligibleExistingArtistForRelease(artist),
			"live terminal release and format guards preserve legacy eligibility predicate");
		Require(ArtistManager.IsEligibleUnsignedCandidateForProbe(artist) &&
			GenreSupplyService.IsAvailableForNewSupply(artist.primaryGenre, 1960f),
			"Dropped active artist is available to enabled signing supply");
		newLabel.SignArtist(artist, 1960);
		ArtistManager.ReconcileSignedArtistForProbe(artist, pool, newLabel.labelId, 1960);
		Require(artist.careerState == CareerState.NewSigning && artist.labelId == newLabel.labelId &&
			newLabel.roster.Count(candidate => candidate == artist) == 1 && pool.Count(candidate => candidate == artist) == 0,
			"re-signing assigns one owner, resets state, and removes free-agent membership");
		foreach (CareerState terminal in new[] { CareerState.Disbanded, CareerState.Retired }) {
			artist.careerState = terminal;
			artist.labelId = null;
			Require(!ArtistManager.IsEligibleUnsignedCandidateForProbe(artist) &&
				!GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(artist), terminal + " is neither signable nor release-eligible");
		}
		return "dropped roster/pool/re-sign/terminal guards passed";
	}

	private static MarketRegion CreateRegion(string id, bool fm, float integration, float church) {
		var region = new MarketRegion {
			regionId = id, population = 10f, youthPercentage = .25f, averageIncome = 1f, urbanization = .6f,
			blackPopulation = .15f, collegeCount = 12, culturalProgressivism = .5f, churchNetworkStrength = church,
			currentIntegration = integration, media = new MediaInfrastructure { hasFMUnderground = fm, radioReach = .5f }
		};
		region.segmentCapacities = SegmentCapacityModel.Create(region, 1968);
		return region;
	}

	private static void ProbeEnabledInitialSeeding() {
		IReadOnlyDictionary<Genre, float> initialPrior = ArtistManager.GetEnabledInitialPrimaryGenrePrior();
		Require(Math.Abs(initialPrior.Values.Sum() - 1f) < .000001f &&
			Math.Abs(initialPrior.GetValueOrDefault(Genre.RockAndRoll) - .1476f) < .000001f &&
			Math.Abs(initialPrior.GetValueOrDefault(Genre.DooWop) - .1286f) < .000001f,
			"enabled initial primary-identity prior matches frozen picker bands");
		var soloRolls = new (float Roll, Genre Legacy)[] {
			(.17f, Genre.RockAndRoll), (.31f, Genre.RnB), (.41f, Genre.TraditionalPop), (.49f, Genre.DooWop),
			(.57f, Genre.Soul), (.63f, Genre.Country), (.68f, Genre.Jazz), (.73f, Genre.Gospel),
			(.78f, Genre.TeenPop), (.83f, Genre.Folk), (.87f, Genre.GirlGroup), (.91f, Genre.Motown),
			(.94f, Genre.SurfRock), (.99f, Genre.BluesRock)
		};
		var vocalRolls = new (float Roll, Genre Legacy)[] {
			(.34f, Genre.DooWop), (.54f, Genre.GirlGroup), (.74f, Genre.Motown),
			(.84f, Genre.Soul), (.91f, Genre.RnB), (.99f, Genre.Gospel)
		};
		foreach ((float roll, Genre legacy) in soloRolls) {
			Require(ArtistManager.GetLegacyInitialGenreForProbe(roll, vocalGroup: false) == legacy,
				"legacy initial solo roll " + legacy);
		}
		foreach ((float roll, Genre legacy) in vocalRolls) {
			Require(ArtistManager.GetLegacyInitialGenreForProbe(roll, vocalGroup: true) == legacy,
				"legacy initial vocal roll " + legacy);
		}
		foreach ((float roll, Genre legacy) in soloRolls.Concat(vocalRolls)) {
			int secondaryDraws = 0;
			Genre secondary = legacy == Genre.GirlGroup ? Genre.Soul : Genre.TraditionalPop;
			(Genre primary, Genre canonicalSecondary) = ArtistManager.CanonicalizeEnabledInitialGenres(legacy, 1964, genre => {
				secondaryDraws++;
				return secondary;
			});
			var expected = new Record { primaryGenre = legacy, secondaryGenre = secondary, releaseDate = new GameDate(1964, 1, 1) };
			GenreMigration.Canonicalize(expected);
			Require(primary == expected.primaryGenre && canonicalSecondary == expected.secondaryGenre && secondaryDraws == 1,
				"enabled seed migration and one secondary draw " + legacy + " at roll " + roll);
		}
		Require(ArtistManager.CanonicalizeEnabledInitialGenres(Genre.GirlGroup, 1964, _ => Genre.TeenPop).Primary == Genre.TeenPop &&
			ArtistManager.CanonicalizeEnabledInitialGenres(Genre.GirlGroup, 1964, _ => Genre.Soul).Primary == Genre.Soul,
			"Girl Group canonical Teen Pop/Soul split");
	}

	private static void ProbeMigration(Genre primary, Genre? secondary, int year, Genre expected, string[] tags) {
		var r = new Record { primaryGenre = primary, secondaryGenre = secondary ?? Genre.TraditionalPop, releaseDate = year == 0 ? default : new GameDate(year, 1, 1), genreTagIds = new[] { "z", "z" } };
		GenreMigration.Canonicalize(r);
		Require(r.primaryGenre == expected && r.primaryGenreId == GenreCatalog.Get(expected).Id, "migration " + primary);
		foreach (string tag in tags) Require(r.genreTagIds.Contains(tag), "tag " + tag);
		Require(r.genreTagIds.SequenceEqual(r.genreTagIds.OrderBy(x => x, StringComparer.Ordinal)) && r.genreTagIds.Distinct(StringComparer.Ordinal).Count() == r.genreTagIds.Length, "tag sorting");
		string id = r.primaryGenreId; string[] before = r.genreTagIds.ToArray(); int schema = r.genreSchemaVersion;
		GenreMigration.Canonicalize(r);
		Require(r.primaryGenreId == id && r.genreSchemaVersion == schema && r.genreTagIds.SequenceEqual(before), "migration idempotence");
	}
	private static void Require(bool condition, string name) { if (!condition) throw new InvalidOperationException("D5 probe failed: " + name); }
}
