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
			ProbePhase2RoutingAndOrientation();
			results.Add("segment normalization/conservation/FM/texture/R&B/format-prior/AI-market probes passed");
		}
		return results;
	}

	private static void ProbePhase2RoutingAndOrientation() {
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
		var legacyOnlyLabel = new AILabel { roster = new List<SimulatedArtist> { legacyArtist } };
		Require(legacyOnlyLabel.CountArtistsEligibleForRelease(1967) == 1, "legacy-only roster has no phantom release roll");
		var globallyConcentrated = new Dictionary<Genre, int> { [Genre.Soul] = 20, [Genre.Country] = 1 };
		var globallyBalanced = new Dictionary<Genre, int> { [Genre.TraditionalPop] = 20, [Genre.Country] = 1 };
		float concentratedSoulRelativeWeight = GenreSupplyService.GetSupplyWeight(Genre.Soul, null, null, null, 1961f, null, globallyConcentrated) /
			GenreSupplyService.GetSupplyWeight(Genre.Country, null, null, null, 1961f, null, globallyConcentrated);
		float balancedSoulRelativeWeight = GenreSupplyService.GetSupplyWeight(Genre.Soul, null, null, null, 1961f, null, globallyBalanced) /
			GenreSupplyService.GetSupplyWeight(Genre.Country, null, null, null, 1961f, null, globallyBalanced);
		Require(concentratedSoulRelativeWeight < balancedSoulRelativeWeight, "global supply concentration reduces relative selection weight");
		MarketRegion youthHeavy = CreateRegion("greatlakes", fm: true, integration: .5f, church: .25f);
		MarketRegion adultHeavy = CreateRegion("greatlakes", fm: true, integration: .5f, church: .25f);
		youthHeavy.youthPercentage = .55f;
		adultHeavy.youthPercentage = 0f;
		youthHeavy.segmentCapacities = SegmentCapacityModel.Create(youthHeavy, 1964);
		adultHeavy.segmentCapacities = SegmentCapacityModel.Create(adultHeavy, 1964);
		youthHeavy.SetGenreMarketV2Live(true);
		adultHeavy.SetGenreMarketV2Live(true);
		float youthAcceptance = youthHeavy.GetGenreAcceptance(Genre.Jazz, 1964f);
		float adultAcceptance = adultHeavy.GetGenreAcceptance(Genre.Jazz, 1964f);
		float expectedRoutedDemandRatio = (.20f + .80f * youthAcceptance) / (.20f + .80f * adultAcceptance);
		float actualSupplyRatio = GenreSupplyService.GetSupplyWeight(Genre.Jazz, null, null, youthHeavy, 1964f) /
			GenreSupplyService.GetSupplyWeight(Genre.Jazz, null, null, adultHeavy, 1964f);
		Require(Math.Abs(actualSupplyRatio - expectedRoutedDemandRatio) < .000001f,
			"supply regional acceptance enters exactly once");
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
