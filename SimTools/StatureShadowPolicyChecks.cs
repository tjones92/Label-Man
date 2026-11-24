using System;
using System.Linq;

public static class StatureShadowPolicyChecks {
	public static void Run() {
		CheckNationalEligibility();
		var calibration = new StatureShadowPolicy.Calibration {
			Version = "fixture-only", EffectiveDate = new DateTime(1960, 1, 1),
			Bands = new StatureShadowPolicy.Boundary[4]
		};
		for (int i = 0; i < 4; i++) calibration.Bands[i] = new StatureShadowPolicy.Boundary {
			Entry = .2 * (i + 1), Exit = .2 * (i + 1) - .05,
			ReachWeights = new[] { .2 * i, .2 * i, .2 * i }
		};
		StatureShadowPolicy.Route Route(double response, double draw, double reach) => new(
			StatureShadowPolicy.Range.Exact(response), StatureShadowPolicy.Range.Exact(draw), StatureShadowPolicy.Range.Exact(reach));
		var routes = new[] { Route(.85, 0, 1), Route(0, 0, 1), Route(0, 0, 1) };
		var date = new DateTime(1960, 1, 8);
		if (StatureShadowPolicy.Evaluate(routes, calibration, date).Band != StatureShadowPolicy.Band.Superstar)
			throw new InvalidOperationException("Stature breakout must skip bands.");
		routes[0] = Route(.57, 0, 1);
		if (StatureShadowPolicy.Evaluate(routes, calibration, date, StatureShadowPolicy.Band.Star).Band != StatureShadowPolicy.Band.Star ||
			StatureShadowPolicy.Evaluate(routes, calibration, date).Band != StatureShadowPolicy.Band.Established)
			throw new InvalidOperationException("Stature entry and exit boundaries must differ.");
		routes[0] = new StatureShadowPolicy.Route(StatureShadowPolicy.Range.Exact(0),
			StatureShadowPolicy.Range.Missing, StatureShadowPolicy.Range.Exact(1));
		if (StatureShadowPolicy.Evaluate(routes, calibration, date).Status != StatureShadowPolicy.ResultStatus.MissingEvidence)
			throw new InvalidOperationException("Missing evidence must not become zero.");
		routes[0] = new StatureShadowPolicy.Route(StatureShadowPolicy.Range.Exact(0),
			new StatureShadowPolicy.Range(0, .05), StatureShadowPolicy.Range.Exact(1));
		if (StatureShadowPolicy.Evaluate(routes, calibration, date).Status != StatureShadowPolicy.ResultStatus.BelowRising)
			throw new InvalidOperationException("Bounded quiet evidence must be distinguishable from missing.");
		var ledger = new StatureShadowLedger();
		routes[0] = Route(.85, 0, 1);
		var evidence = new System.Collections.Generic.Dictionary<string, StatureShadowPolicy.Route[]> { ["a"] = routes };
		if (!ledger.Advance(date, evidence, calibration) || ledger.Advance(date, evidence, calibration) ||
			ledger.Artists["a"].PriorBand != StatureShadowPolicy.Band.Superstar)
			throw new InvalidOperationException("Weekly publication must be idempotent.");
		routes[0] = new StatureShadowPolicy.Route(StatureShadowPolicy.Range.Exact(0),
			StatureShadowPolicy.Range.Missing, StatureShadowPolicy.Range.Exact(1));
		ledger.Advance(date.AddDays(7), evidence, calibration);
		if (ledger.Artists["a"].PriorBand != StatureShadowPolicy.Band.Superstar ||
			ledger.Artists["a"].Status != StatureShadowPolicy.ResultStatus.MissingEvidence)
			throw new InvalidOperationException("An unknown week must retain hysteresis while exposing the missing result.");
		string saved = System.Text.Json.JsonSerializer.Serialize(ledger);
		var restored = System.Text.Json.JsonSerializer.Deserialize<StatureShadowLedger>(saved);
		if (restored == null || restored.LastProcessedDate != ledger.LastProcessedDate ||
			restored.CalibrationVersion != calibration.Version || restored.Artists["a"].PriorBand != StatureShadowPolicy.Band.Superstar ||
			restored.Advance(date.AddDays(7), evidence, calibration))
			throw new InvalidOperationException("Ledger state must survive serialization with its processed clock.");
		routes[0] = Route(.3, 0, 1);
		restored.Advance(date.AddDays(14), evidence, calibration);
		if (restored.Artists["a"].PriorBand != StatureShadowPolicy.Band.Rising)
			throw new InvalidOperationException("Restored ledger must continue cooling on the next week.");
		try { ledger.Advance(date.AddDays(21), evidence, calibration); throw new InvalidOperationException("Week gap accepted."); }
		catch (InvalidOperationException ex) when (ex.Message != "Week gap accepted.") { }
		evidence["bad"] = Array.Empty<StatureShadowPolicy.Route>();
		try { ledger.Advance(date.AddDays(14), evidence, calibration); throw new InvalidOperationException("Invalid route accepted."); }
		catch (ArgumentException) { }
		if (ledger.LastProcessedDate != date.AddDays(7) || ledger.Artists.ContainsKey("bad"))
			throw new InvalidOperationException("Invalid evidence must not publish a partial week.");
		var normalization = new StaturePeerNormalizer.Config { Version = "fixture", HalfLifeWeeks = 1, TopN = 1, MinimumPeers = 1,
			Components = new[] { new StaturePeerNormalizer.ComponentConfig { Floor = .001, SmoothResponse = true },
				new StaturePeerNormalizer.ComponentConfig { Floor = 1, SmoothResponse = false } } };
		var normalizer = new StaturePeerNormalizer();
		var input = new System.Collections.Generic.Dictionary<string, double?[]> {
			["a"] = new double?[] { .2, 50 }, ["b"] = new double?[] { .4, 50 }, ["c"] = new double?[] { .4, null }
		};
		normalizer.Advance(date, input, normalization);
		if (normalizer.Latest["a"].Reference[0].HasValue) throw new InvalidOperationException("First week cannot read future peers.");
		string normalizerSaved = System.Text.Json.JsonSerializer.Serialize(normalizer);
		var resumed = System.Text.Json.JsonSerializer.Deserialize<StaturePeerNormalizer>(normalizerSaved);
		input["a"] = new double?[] { 0, 50 };
		normalizer.Advance(date.AddDays(7), input, normalization);
		resumed.Advance(date.AddDays(7), input, normalization);
		var read = normalizer.Latest["a"];
		if (Math.Abs(read.Smoothed[0].Value - .1) > 1e-10 || Math.Abs(read.Reference[0].Value - .4) > 1e-10 ||
			Math.Abs(read.Score[0].Value - .25) > 1e-10 || resumed.Latest["a"].Score[0] != read.Score[0] ||
			normalizer.Latest["c"].Score[1].HasValue)
			throw new InvalidOperationException("Peer exclusion, smoothing, missing input or restored normalization failed.");
		var leader = new StaturePeerNormalizer();
		input["a"] = new double?[] { .9, 0 }; input["b"] = new double?[] { .2, 0 }; input["c"] = new double?[] { .1, 0 };
		leader.Advance(date, input, normalization);
		leader.Advance(date.AddDays(7), input, normalization);
		if (Math.Abs(leader.Latest["a"].Reference[0].Value - .2) > 1e-10 ||
			leader.Latest["a"].Reference[1] != 1 || leader.Latest["a"].Score[1] != 0)
			throw new InvalidOperationException("The leader must exclude itself and a zero peer reference must use the floor.");
		var rosterOnly = new StaturePeerNormalizer();
		var peersOnly = new System.Collections.Generic.HashSet<string> { "b", "c" };
		rosterOnly.Advance(date, input, normalization, peersOnly);
		rosterOnly.Advance(date.AddDays(7), input, normalization, peersOnly);
		if (Math.Abs(rosterOnly.Latest["b"].Reference[0].Value - .1) > 1e-10 ||
			Math.Abs(rosterOnly.Latest["a"].Reference[0].Value - .2) > 1e-10)
			throw new InvalidOperationException("Off-roster evidence must advance without entering the peer population.");
		var weeklyConfig = new StatureWeeklyShadow.Config { Version = "fixture-weekly", Policy = calibration,
			LocalSalesShareReference = .1, FollowingPerMillionReference = 100, RegionalHalfLifeWeeks = 1, MarketFloorUnits = 1,
			Normalization = new StaturePeerNormalizer.Config { Version = "fixture-eight", HalfLifeWeeks = 1, TopN = 1, MinimumPeers = 1,
				Components = System.Linq.Enumerable.Range(0, 8).Select(i => new StaturePeerNormalizer.ComponentConfig { Floor = .001, SmoothResponse = i < 6 }).ToArray() } };
		StatureWeeklyShadow.ArtistEvidence Week(double response) => new() {
			Components = new double?[] { response, response, response, response, 0, 0, 10, 10 },
			Regions = new() { new StatureWeeklyShadow.Region { Id = "r", PopulationMillions = 1, Following = 20,
				SalesShares = new double?[] { response, response, response, response } } } };
		var weekly = new StatureWeeklyShadow();
		var weeklyEvidence = new System.Collections.Generic.Dictionary<string, StatureWeeklyShadow.ArtistEvidence> { ["a"] = Week(.1), ["b"] = Week(.2) };
		weekly.Advance(date, weeklyEvidence, weeklyConfig);
		string disabledMedia = System.Text.Json.JsonSerializer.Serialize(new MediaSaveData(), SaveGameService.TestJsonOptions);
		if (disabledMedia.Contains("experimentalStature", StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("Disabled saves must omit shadow state.");
		string weeklySaved = System.Text.Json.JsonSerializer.Serialize(new MediaSaveData { ExperimentalStature = weekly }, SaveGameService.TestJsonOptions);
		var weeklyRestored = System.Text.Json.JsonSerializer.Deserialize<MediaSaveData>(weeklySaved, SaveGameService.TestJsonOptions)?.ExperimentalStature;
		if (weeklyRestored == null) throw new InvalidOperationException("Media save dropped the opt-in weekly shadow.");
		weekly.Advance(date.AddDays(7), weeklyEvidence, weeklyConfig);
		weeklyRestored.Advance(date.AddDays(7), weeklyEvidence, weeklyConfig);
		if (weeklyRestored.Snapshots["a"].Band != weekly.Snapshots["a"].Band ||
			weeklyRestored.Snapshots["a"].Scores[0] != weekly.Snapshots["a"].Scores[0] ||
			weeklyRestored.Advance(date.AddDays(7), weeklyEvidence, weeklyConfig))
			throw new InvalidOperationException("Weekly shadow restore or idempotence failed.");
		// Exercise the complete state after a checkpoint with identical varied evidence in both paths.
		string checkpoint = System.Text.Json.JsonSerializer.Serialize(weekly, SaveGameService.TestJsonOptions);
		var uninterrupted = System.Text.Json.JsonSerializer.Deserialize<StatureWeeklyShadow>(checkpoint, SaveGameService.TestJsonOptions);
		var replay = System.Text.Json.JsonSerializer.Deserialize<StatureWeeklyShadow>(checkpoint, SaveGameService.TestJsonOptions);
		for (int i = 0; i < 4; i++) {
			var act = Week(i == 0 ? 0 : .03 * i);
			if (i == 1) { act.Components[6] = act.Components[7] = null; act.Regions[0].Following = null; }
			if (i == 2) act.PeerEligible = false;
			var recorded = new System.Collections.Generic.Dictionary<string, StatureWeeklyShadow.ArtistEvidence> { ["a"] = act, ["b"] = Week(.2) };
			DateTime observation = date.AddDays(14 + 7 * i);
			uninterrupted.Advance(observation, recorded, weeklyConfig);
			replay.Advance(observation, recorded, weeklyConfig);
			if (System.Text.Json.JsonSerializer.Serialize(uninterrupted, SaveGameService.TestJsonOptions) !=
				System.Text.Json.JsonSerializer.Serialize(replay, SaveGameService.TestJsonOptions) || replay.Advance(observation, recorded, weeklyConfig))
				throw new InvalidOperationException("Complete shadow continuation diverged after restore.");
		}
		weeklyEvidence["a"].PeerEligible = false;
		weekly.Advance(date.AddDays(14), weeklyEvidence, weeklyConfig);
		weeklyEvidence["a"].PeerEligible = true;
		weekly.Advance(date.AddDays(21), weeklyEvidence, weeklyConfig);
		if (weekly.Snapshots["a"].Date != date.AddDays(21) || weekly.Normalizer.Previous["a"][0] == null)
			throw new InvalidOperationException("Off-roster evidence must carry through a re-signing.");
		var sample = weekly.Snapshots["a"];
		if (StatureDecisionSnapshot.Resolve(sample, sample.Date, CareerState.NewSigning, false).Status != StatureDecisionSnapshot.Status.SameDayOrFuture ||
			StatureDecisionSnapshot.Resolve(sample, sample.Date.AddDays(8), CareerState.NewSigning, false).Status != StatureDecisionSnapshot.Status.Stale ||
			StatureDecisionSnapshot.Resolve(sample, sample.Date.AddDays(7), CareerState.Retired, false).Status != StatureDecisionSnapshot.Status.Terminal ||
			StatureDecisionSnapshot.Resolve(sample, sample.Date.AddDays(7), CareerState.Rising, false, ArtistLifecycleStatus.Disbanded).Status != StatureDecisionSnapshot.Status.Terminal ||
			StatureDecisionSnapshot.Resolve(sample, sample.Date.AddDays(7), CareerState.Dropped, false).Status != StatureDecisionSnapshot.Status.LifecycleUnresolved ||
			StatureDecisionSnapshot.Resolve(sample, sample.Date.AddDays(7), CareerState.Rising, true).Status != StatureDecisionSnapshot.Status.PlayerOwned)
			throw new InvalidOperationException("Decision timing or lifecycle fallback failed.");
		var below = new StatureWeeklyShadow.Snapshot { Date = date, Band = StatureShadowPolicy.Band.BelowRising,
			Status = StatureShadowPolicy.ResultStatus.BelowRising };
		var starSnapshot = new StatureWeeklyShadow.Snapshot { Date = date, Band = StatureShadowPolicy.Band.Star,
			Status = StatureShadowPolicy.ResultStatus.Observed };
		CareerState? Select(bool enabled, StatureWeeklyShadow.Snapshot s, DateTime when, CareerState current, bool player = false) =>
			ArtistStature.SelectProgrammeAlbumCareer(enabled, s, when, current, player, ArtistLifecycleStatus.Active);
		if (Select(false, starSnapshot, date.AddDays(7), CareerState.Rising) != null ||
			Select(true, starSnapshot, date.AddDays(7), CareerState.Rising) != CareerState.Star ||
			Select(true, starSnapshot, date, CareerState.Rising) != null ||
			Select(true, starSnapshot, date.AddDays(8), CareerState.Rising) != null ||
			Select(true, starSnapshot, date.AddDays(7), CareerState.Rising, true) != null ||
			Select(true, starSnapshot, date.AddDays(7), CareerState.Dropped) != null ||
			Select(true, below, date.AddDays(7), CareerState.NewSigning) != CareerState.NewSigning)
			throw new InvalidOperationException("Programme album reader must use only enabled, fresh, eligible snapshots.");
		if (CompetitorManager.GetAlbumPriorCareerMultiplier(CareerState.Star, false) != 2f ||
			CompetitorManager.GetAlbumPriorCareerMultiplier(Select(true, starSnapshot, date.AddDays(7), CareerState.Star).Value, false) !=
				CompetitorManager.GetAlbumPriorCareerMultiplier(CareerState.Star, false))
			throw new InvalidOperationException("A rung-equivalent programme read must keep the existing multiplier.");
		if (StatureDecisionSnapshot.Resolve(below, date.AddDays(7), CareerState.NewSigning, false).CareerState != CareerState.NewSigning ||
			StatureDecisionSnapshot.Resolve(below, date.AddDays(7), CareerState.Rising, false).Status != StatureDecisionSnapshot.Status.LifecycleUnresolved)
			throw new InvalidOperationException("Below Rising must preserve signing context and leave other mappings unresolved.");
		below.Status = StatureShadowPolicy.ResultStatus.MissingEvidence; below.Band = StatureShadowPolicy.Band.Superstar;
		if (StatureDecisionSnapshot.Resolve(below, date.AddDays(7), CareerState.Rising, false).Status != StatureDecisionSnapshot.Status.MissingEvidence)
			throw new InvalidOperationException("A retained hysteresis band must not hide missing evidence.");
		Godot.GD.Print("STATURE_SHADOW_POLICY_CHECK passed");
		string replayArgument = Godot.OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--stature-policy-replay=", StringComparison.Ordinal));
		if (replayArgument != null) CheckReplay(replayArgument["--stature-policy-replay=".Length..]);
	}

	private static void CheckNationalEligibility() {
		var legacy = System.Text.Json.JsonSerializer.Deserialize<StatureWeeklyShadow.Config>(
			System.IO.File.ReadAllText("SimTools/StatureShadowExperimental.json"), SaveGameService.TestJsonOptions);
		var candidate = System.Text.Json.JsonSerializer.Deserialize<StatureWeeklyShadow.Config>(
			System.IO.File.ReadAllText("SimTools/StatureShadowNationalHighTiers.json"), SaveGameService.TestJsonOptions);
		StatureShadowPolicy.Validate(candidate.Policy);
		StatureShadowPolicy.Route Route(double magnitude, double reach) => new(
			StatureShadowPolicy.Range.Exact(0), StatureShadowPolicy.Range.Exact(magnitude), StatureShadowPolicy.Range.Exact(reach));
		var date = candidate.Policy.EffectiveDate;
		var routes = new[] { Route(1, .04), Route(0, .04), Route(0, .04) };
		if (StatureShadowPolicy.Evaluate(routes, legacy.Policy, date).Band != StatureShadowPolicy.Band.Star)
			throw new InvalidOperationException("Legacy concentrated draw fixture changed.");
		foreach (var prior in new[] { StatureShadowPolicy.Band.BelowRising, StatureShadowPolicy.Band.Star, StatureShadowPolicy.Band.Superstar }) {
			var result = StatureShadowPolicy.Evaluate(routes, candidate.Policy, date, prior);
			if (result.Band != StatureShadowPolicy.Band.Established || result.Bands[2].Routes[0].Upper != 0 || result.Bands[3].Routes[0].Upper != 0)
				throw new InvalidOperationException("Regional evidence must neither enter nor retain national high tiers.");
		}
		routes[0] = new(StatureShadowPolicy.Range.Missing, StatureShadowPolicy.Range.Missing, StatureShadowPolicy.Range.Missing);
		routes[1] = Route(1, 1);
		if (StatureShadowPolicy.Evaluate(routes, candidate.Policy, date).Band != StatureShadowPolicy.Band.Superstar)
			throw new InvalidOperationException("Missing ineligible regional evidence must not block a known national Superstar.");
		routes[1] = new(StatureShadowPolicy.Range.Missing, StatureShadowPolicy.Range.Missing, StatureShadowPolicy.Range.Missing);
		if (StatureShadowPolicy.Evaluate(routes, candidate.Policy, date).Status != StatureShadowPolicy.ResultStatus.MissingEvidence)
			throw new InvalidOperationException("Missing national evidence must remain unknown.");
		string oldJson = System.Text.Json.JsonSerializer.Serialize(legacy, SaveGameService.TestJsonOptions);
		if (oldJson.Contains("EligibleRoutes", StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("Legacy saves must omit default route eligibility.");
		string saved = System.Text.Json.JsonSerializer.Serialize(new StatureReaderSaveData { Calibration = candidate }, SaveGameService.TestJsonOptions);
		var restored = System.Text.Json.JsonSerializer.Deserialize<StatureReaderSaveData>(saved, SaveGameService.TestJsonOptions);
		if (restored?.Calibration?.Policy?.Bands[2].EligibleRoutes?[0] != false ||
			System.Text.Json.JsonSerializer.Serialize(restored, SaveGameService.TestJsonOptions) != saved)
			throw new InvalidOperationException("Reader saves must preserve national high-tier eligibility exactly.");
		foreach (var flags in new[] { Array.Empty<bool>(), new[] { true }, new[] { true, true, true, true }, new[] { false, false, false } }) {
			candidate.Policy.Bands[2].EligibleRoutes = flags;
			try { StatureShadowPolicy.Validate(candidate.Policy); throw new InvalidOperationException("Invalid eligibility accepted."); }
			catch (ArgumentException) { }
		}
		candidate.Policy.Bands[2].EligibleRoutes = new[] { false, true, true };
		candidate.Policy.Bands[3].EligibleRoutes = null;
		try { StatureShadowPolicy.Validate(candidate.Policy); throw new InvalidOperationException("Higher-band route re-enabling accepted."); }
		catch (ArgumentException) { }
		Godot.GD.Print("STATURE_NATIONAL_ELIGIBILITY_CHECK passed");
	}

	private static void CheckReplay(string path) {
		using var stream = System.IO.File.OpenText(path);
		using var header = System.Text.Json.JsonDocument.Parse(stream.ReadLine());
		var calibrations = header.RootElement.GetProperty("calibrations").EnumerateObject().ToDictionary(
			p => p.Name, p => System.Text.Json.JsonSerializer.Deserialize<StatureShadowPolicy.Calibration>(p.Value.GetRawText(), SaveGameService.TestJsonOptions));
		var histories = new System.Collections.Generic.Dictionary<string, (DateTime Date, StatureShadowPolicy.Band Band)>();
		int count = 0;
		string line;
		while ((line = stream.ReadLine()) != null) {
			using var document = System.Text.Json.JsonDocument.Parse(line);
			var row = document.RootElement;
			string key = row.GetProperty("key").GetString();
			var date = DateTime.ParseExact(row.GetProperty("date").GetString(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
			var calibration = calibrations[row.GetProperty("calibration").GetString()];
			var prior = histories.TryGetValue(key, out var history) ? history.Band : StatureShadowPolicy.Band.BelowRising;
			if (histories.ContainsKey(key) ? (date - history.Date).Days != 7 : date != calibration.EffectiveDate.Date)
				throw new InvalidOperationException("Replay history is not consecutive: " + key);
			StatureShadowPolicy.Range Range(System.Text.Json.JsonElement value) => value.ValueKind == System.Text.Json.JsonValueKind.Null
				? StatureShadowPolicy.Range.Missing : StatureShadowPolicy.Range.Exact(value.GetDouble());
			var routes = row.GetProperty("routes").EnumerateArray().Select(r => new StatureShadowPolicy.Route(Range(r[0]), Range(r[1]), Range(r[2]))).ToArray();
			var actual = StatureShadowPolicy.Evaluate(routes, calibration, date, prior);
			var expected = row.GetProperty("expected");
			if (actual.Band.ToString() != expected.GetProperty("band").GetString() || actual.Status.ToString() != expected.GetProperty("status").GetString())
				throw new InvalidOperationException($"Replay band/status mismatch at {key} {date:yyyy-MM-dd}.");
			for (int i = 0; i < 4; i++) {
				var support = actual.Bands[i]; var wanted = expected.GetProperty("supports")[i];
				var values = new[] { support.Value.Lower, support.Value.Upper, support.Boundary }
					.Concat(support.Routes.SelectMany(r => new[] { r.Lower, r.Upper })).ToArray();
				if (values.Where((value, j) => Math.Abs(value - wanted[j].GetDouble()) > 1e-12).Any())
					throw new InvalidOperationException($"Replay support mismatch at {key} {date:yyyy-MM-dd} band={i}.");
			}
			histories[key] = (date, actual.Status == StatureShadowPolicy.ResultStatus.MissingEvidence ? prior : actual.Band);
			count++;
		}
		if (count != header.RootElement.GetProperty("expectedCases").GetInt32())
			throw new InvalidOperationException("Replay case count differs.");
		Godot.GD.Print($"STATURE_POLICY_REPLAY_CHECK passed cases={count} histories={histories.Count}");
	}
}
