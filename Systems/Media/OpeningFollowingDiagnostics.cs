using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

/// <summary>Read-only, fresh-start diagnostic. Keeps observed peer references,
/// sales and TV fixed, removes only surviving original followers before clipping,
/// and evolves independent hysteresis for half/zero opening-stock inputs.
/// Its sensitivity ledgers are deliberately not a saved causal reader.</summary>
public static class OpeningFollowingDiagnostics {
	private sealed record SeedRow(GameDate Date, string Artist, string Name, string Archetype, string Career,
		string Region, double Young, double Adult);
	private static readonly List<SeedRow> seeds = new();
	private static readonly HashSet<string> seededActs = new(StringComparer.Ordinal);
	private static StatureShadowLedger half = new(), zero = new();
	private static DateTime latestDate;
	private static TextWriter shadowLog, marketingLog;
	private static string F(double n) => n.ToString("R", CultureInfo.InvariantCulture);
	private static string Q(string s) => "\"" + (s ?? "").Replace("\"", "\"\"") + "\"";
	public static void Reset() {
		OpeningGeographyDiagnostics.Reset();
		seeds.Clear(); seededActs.Clear(); half = new(); zero = new(); latestDate = default;
	}
	internal static void RecordSeed(SimulatedArtist artist, MediaFollowingState following, GameDate date) {
		seededActs.Add(artist.artistId);
		foreach (var p in following.Regions.OrderBy(p => p.Key, StringComparer.Ordinal))
			seeds.Add(new(date, artist.artistId, artist.stageName, artist.openingArchetype.ToString(), artist.careerState.ToString(),
				p.Key, p.Value.YoungTotal(), p.Value.Adult));
	}
	public static void Open(TextWriter census, TextWriter shadow, TextWriter marketing) {
		shadowLog = shadow; marketingLog = marketing;
		census.WriteLine("date,artistId,artist,openingArchetype,careerState,region,seedYoung,seedAdult");
		foreach (var r in seeds) census.WriteLine(string.Join(",", Q(r.Date.ToShortString()), Q(r.Artist), Q(r.Name),
			r.Archetype, r.Career, Q(r.Region), F(r.Young), F(r.Adult)));
		shadow.WriteLine("date,artistId,followingYoung,followingAdult,openingYoung,openingAdult,actualBand,actualStatus,halfOpeningBand,halfOpeningStatus,zeroOpeningBand,zeroOpeningStatus,followingYoungReference,followingAdultReference,actualRegionalDraw,halfRegionalDraw,zeroRegionalDraw,actualRecordReach,halfRecordReach,zeroRecordReach,actualConversionReach,halfConversionReach,zeroConversionReach");
		marketing.WriteLine("date,artistId,labelId,context,snapshotDate,status,legacyCareer,actualStandingCareer,legacyBudget,actualRequestedBudget,actualProposedBudget,halfOpeningBand,halfOpeningBudget,zeroOpeningBand,zeroOpeningBudget");
		ArtistStature.OnMarketingRead += OnMarketing;
	}
	public static void Close() {
		ArtistStature.OnMarketingRead -= OnMarketing;
		shadowLog = marketingLog = null;
	}
	private static StatureShadowPolicy.Range Range(double? n) => n.HasValue ? StatureShadowPolicy.Range.Exact(n.Value) : StatureShadowPolicy.Range.Missing;
	private static StatureShadowPolicy.Range Maximum(params double?[] n) => n.All(x => x.HasValue)
		? StatureShadowPolicy.Range.Exact(n.Max(x => x.Value)) : StatureShadowPolicy.Range.Missing;
	internal sealed record Derived(StatureShadowPolicy.Route[] Routes, double? Draw, double? Record, double? Conversion);
	internal static Derived Derive(string artist, StatureWeeklyShadow.ArtistEvidence evidence, StatureWeeklyShadow shadow,
		StatureWeeklyShadow.Snapshot snapshot, StatureWeeklyShadow.Config config, double retainedOpening) {
		var fs = MediaAtmosphereService.FollowingFor(artist);
		double taggedYoung = 0, taggedAdult = 0, weightedRecord = 0, weightedConversion = 0, population = 0;
		bool drawKnown = true, recordKnown = true;
		var draws = new List<double>();
		foreach (var region in evidence.Regions) {
			FollowingRegion fr = null; fs?.Regions.TryGetValue(region.Id, out fr);
			fr?.OpeningOrigin?.Validate(fr);
			double young = fr?.OpeningOrigin?.YoungTotal ?? 0, adult = fr?.OpeningOrigin?.Adult ?? 0;
			taggedYoung += young; taggedAdult += adult;
			double? remaining = region.Following.HasValue ? Math.Max(0, region.Following.Value - (1 - retainedOpening) * (young + adult)) : null;
			double? density = remaining.HasValue ? Math.Clamp(remaining.Value / region.PopulationMillions / config.FollowingPerMillionReference, 0, 1) : null;
			var smooth = shadow.RegionalHistory[artist][region.Id];
			double? response = smooth.All(n => n.HasValue) ? Math.Min(1, smooth.Max(n => n.Value) / config.LocalSalesShareReference) : null;
			population += region.PopulationMillions;
			if (density.HasValue) {
				draws.Add(density.Value);
				weightedConversion += density.Value * region.PopulationMillions;
			} else drawKnown = false;
			if (density.HasValue && response.HasValue) weightedRecord += Math.Max(response.Value, density.Value) * region.PopulationMillions;
			else recordKnown = false;
		}
		var values = (double?[])snapshot.Scores.Clone();
		for (int i = 6; i < 8; i++) {
			double tagged = i == 6 ? taggedYoung : taggedAdult;
			values[i] = snapshot.Components[i].HasValue && snapshot.References[i].HasValue
				? Math.Clamp(Math.Max(0, snapshot.Components[i].Value - (1 - retainedOpening) * tagged) / snapshot.References[i].Value, 0, 1) : null;
		}
		double? draw = drawKnown ? draws.Max() : null, record = recordKnown ? weightedRecord / population : null,
			conversion = drawKnown ? weightedConversion / population : null;
		var following = Maximum(values[6], values[7]);
		return new(new[] {
			new StatureShadowPolicy.Route(Range(snapshot.RegionalResponse), Range(draw), Range(record)),
			new StatureShadowPolicy.Route(Maximum(values[0], values[1], values[2], values[3]), following, Range(record)),
			new StatureShadowPolicy.Route(Maximum(values[4], values[5]), following, Range(conversion)) }, draw, record, conversion);
	}
	internal static void Observe(GameDate date, IReadOnlyDictionary<string, StatureWeeklyShadow.ArtistEvidence> evidence,
		StatureWeeklyShadow shadow, StatureWeeklyShadow.Config config) {
		if (!MediaFollowing.ObserveOpeningOrigin || shadowLog == null) return;
		var day = new DateTime(date.year, date.month, date.day);
		if (day == latestDate) return;
		if (config.Normalization.Components[6].SmoothResponse || config.Normalization.Components[7].SmoothResponse)
			throw new InvalidOperationException("Opening-stock diagnostic requires unsmoothed following components.");
		var derived = new Dictionary<string, (Derived Half, Derived Zero)>();
		var halfRoutes = new Dictionary<string, StatureShadowPolicy.Route[]>();
		var zeroRoutes = new Dictionary<string, StatureShadowPolicy.Route[]>();
		if (day >= config.Policy.EffectiveDate.Date) {
			foreach (string artist in seededActs.OrderBy(x => x, StringComparer.Ordinal)) {
				if (!evidence.TryGetValue(artist, out var e) || !shadow.Snapshots.TryGetValue(artist, out var s)) continue;
				var neutral = Derive(artist, e, shadow, s, config, 1);
				OpeningGeographyDiagnostics.Observe(date, artist, e, shadow, config);
				var result = StatureShadowPolicy.Evaluate(neutral.Routes, config.Policy, day, s.PreviousBand);
				if (result.Status != s.Status || result.Status != StatureShadowPolicy.ResultStatus.MissingEvidence && result.Band != s.Band)
					throw new InvalidOperationException("Opening-origin neutral reconstruction differs: " + artist);
				var h = Derive(artist, e, shadow, s, config, .5); var z = Derive(artist, e, shadow, s, config, 0);
				derived.Add(artist, (h, z)); halfRoutes.Add(artist, h.Routes); zeroRoutes.Add(artist, z.Routes);
			}
			half.Advance(day, halfRoutes, config.Policy); zero.Advance(day, zeroRoutes, config.Policy);
		}
		foreach (string artist in seededActs.OrderBy(x => x, StringComparer.Ordinal)) {
			var fs = MediaAtmosphereService.FollowingFor(artist);
			double originYoung = fs?.Regions.Values.Sum(fr => fr.OpeningOrigin?.YoungTotal ?? 0) ?? 0;
			double originAdult = fs?.Regions.Values.Sum(fr => fr.OpeningOrigin?.Adult ?? 0) ?? 0;
			shadow.Snapshots.TryGetValue(artist, out var s); half.Artists.TryGetValue(artist, out var h); zero.Artists.TryGetValue(artist, out var z);
			derived.TryGetValue(artist, out var d);
			string N(double? n) => n.HasValue ? F(n.Value) : "";
			shadowLog.WriteLine(string.Join(",", Q(date.ToShortString()), Q(artist), F(fs?.YoungTotal() ?? 0), F(fs?.AdultTotal() ?? 0),
				F(originYoung), F(originAdult), s?.Band.ToString() ?? "", s?.Status.ToString() ?? "warming_up",
				h?.PriorBand.ToString() ?? "", h?.Status.ToString() ?? "warming_up", z?.PriorBand.ToString() ?? "", z?.Status.ToString() ?? "warming_up",
				N(s?.References[6]), N(s?.References[7]), N(s?.RegionalDraw), N(d.Half?.Draw), N(d.Zero?.Draw),
				N(s?.RecordReach), N(d.Half?.Record), N(d.Zero?.Record), N(s?.ConversionReach), N(d.Half?.Conversion), N(d.Zero?.Conversion)));
		}
		latestDate = day;
	}
	private static void OnMarketing(ArtistStature.MarketingRead read) {
		if (marketingLog == null || !seededActs.Contains(read.ArtistId)) return;
		float Budget(StatureShadowLedger ledger, out string band) {
			band = "";
			if (read.Status != "Available" || !ledger.Artists.TryGetValue(read.ArtistId, out var s) ||
				s.ObservationDate != read.SnapshotDate || s.Status == StatureShadowPolicy.ResultStatus.MissingEvidence) return read.LegacyBudget;
			band = s.PriorBand.ToString();
			CareerState career = s.PriorBand switch {
				StatureShadowPolicy.Band.Rising => CareerState.Rising, StatureShadowPolicy.Band.Established => CareerState.Established,
				StatureShadowPolicy.Band.Star => CareerState.Star, StatureShadowPolicy.Band.Superstar => CareerState.Superstar,
				_ => read.LegacyCareer == CareerState.NewSigning ? CareerState.NewSigning : CareerState.Declining };
			var label = ChartManager.Instance.GetLabelById(read.LabelId);
			return label == null ? read.LegacyBudget : label.MarketingBudgetForCareer(career);
		}
		float h = Budget(half, out string hb), z = Budget(zero, out string zb);
		marketingLog.WriteLine(string.Join(",", Q(read.DecisionDate.ToShortString()), Q(read.ArtistId), Q(read.LabelId), Q(read.Context),
			read.SnapshotDate == default ? "" : read.SnapshotDate.ToString("yyyy-MM-dd"), read.Status, read.LegacyCareer,
			read.StandingCareer?.ToString() ?? "", F(read.LegacyBudget), F(read.Budget), F(read.ProposedBudget), hb, F(h), zb, F(z)));
	}
}
