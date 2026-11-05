using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Experimental weekly shadow calculator. Its caller supplies completed-week
/// market evidence and a frozen calibration. It does not affect AI decisions.
/// </summary>
public sealed class StatureWeeklyShadow {
	public sealed class Region {
		public string Id { get; set; }
		public double PopulationMillions { get; set; }
		public double?[] SalesShares { get; set; }
		public double? Following { get; set; }
	}
	public sealed class ArtistEvidence {
		// Four national sales shares, two TV estimates, two following totals.
		public double?[] Components { get; set; }
		public List<Region> Regions { get; set; }
		public bool PeerEligible { get; set; } = true;
	}
	public sealed class Config {
		public string Version { get; set; }
		public StaturePeerNormalizer.Config Normalization { get; set; }
		public StatureShadowPolicy.Calibration Policy { get; set; }
		public double LocalSalesShareReference { get; set; }
		public double FollowingPerMillionReference { get; set; }
		public double RegionalHalfLifeWeeks { get; set; }
		public double MarketFloorUnits { get; set; }
	}
	public sealed class Snapshot {
		public DateTime Date { get; set; }
		public StatureShadowPolicy.Band Band { get; set; }
		public StatureShadowPolicy.ResultStatus Status { get; set; }
		public double?[] Components { get; set; }
		public double?[] References { get; set; }
		public double?[] Scores { get; set; }
		public double? RegionalResponse { get; set; }
		public double? RegionalDraw { get; set; }
		public double? RecordReach { get; set; }
		public double? ConversionReach { get; set; }
		public StatureShadowPolicy.Band PreviousBand { get; set; }
		public double[] SupportLower { get; set; }
		public double[] SupportUpper { get; set; }
		public double[] Boundaries { get; set; }
		public double[][] RouteSupportLower { get; set; }
	}
	public StaturePeerNormalizer Normalizer { get; set; } = new();
	public StatureShadowLedger Ledger { get; set; } = new();
	public Dictionary<string, Dictionary<string, double?[]>> RegionalHistory { get; set; } = new(StringComparer.Ordinal);
	public Dictionary<string, Snapshot> Snapshots { get; set; } = new(StringComparer.Ordinal);
	public string ConfigVersion { get; set; }

	private static StatureShadowPolicy.Range Maximum(params double?[] values) =>
		values.All(x => x.HasValue) ? StatureShadowPolicy.Range.Exact(values.Max(x => x.Value)) : StatureShadowPolicy.Range.Missing;
	private static double? Weighted(List<(double? Value, double Population)> values, double total) =>
		values.All(x => x.Value.HasValue) ? values.Sum(x => x.Value.Value * x.Population) / total : null;

	public bool Advance(DateTime date, IReadOnlyDictionary<string, ArtistEvidence> evidence, Config config) {
		date = date.Date;
		if (config == null || string.IsNullOrWhiteSpace(config.Version) || config.Normalization?.Components?.Length != 8 ||
			!double.IsFinite(config.LocalSalesShareReference) || config.LocalSalesShareReference <= 0 ||
			!double.IsFinite(config.FollowingPerMillionReference) || config.FollowingPerMillionReference <= 0 ||
			!double.IsFinite(config.MarketFloorUnits) || config.MarketFloorUnits <= 0 ||
			!double.IsFinite(config.RegionalHalfLifeWeeks) || config.RegionalHalfLifeWeeks <= 0 || evidence == null)
			throw new ArgumentException("Invalid weekly shadow configuration.");
		StatureShadowPolicy.Validate(config.Policy);
		if (Normalizer == null || Ledger == null || RegionalHistory == null || Snapshots == null)
			throw new InvalidOperationException("Missing shadow state.");
		if (Normalizer.LastProcessedDate != default && ConfigVersion != config.Version)
			throw new InvalidOperationException("A shadow cannot silently change calibration.");
		if (date == Normalizer.LastProcessedDate.Date) return false;
		if (Normalizer.LastProcessedDate != default && (date - Normalizer.LastProcessedDate.Date).Days != 7)
			throw new InvalidOperationException($"Weekly shadow requires consecutive observations: previous={Normalizer.LastProcessedDate:yyyy-MM-dd}, incoming={date:yyyy-MM-dd}.");
		if (Ledger.LastProcessedDate != default && (date - Ledger.LastProcessedDate.Date).Days != 7)
			throw new InvalidOperationException("Shadow ledger is not aligned with the normalizer.");
		if (Ledger.LastProcessedDate != default && Ledger.CalibrationVersion != config.Policy.Version)
			throw new InvalidOperationException("Shadow ledger calibration does not match.");
		// Validate all regional inputs before advancing any state.
		foreach (var pair in evidence) {
			if (string.IsNullOrEmpty(pair.Key) || pair.Value?.Components?.Length != 8 || pair.Value.Regions == null || pair.Value.Regions.Count == 0 ||
				pair.Value.Regions.Any(r => r == null || string.IsNullOrEmpty(r.Id) || !double.IsFinite(r.PopulationMillions) || r.PopulationMillions <= 0 ||
					r.SalesShares?.Length != 4 || r.SalesShares.Any(x => x.HasValue && (!double.IsFinite(x.Value) || x.Value < 0)) ||
					(r.Following.HasValue && (!double.IsFinite(r.Following.Value) || r.Following.Value < 0))) ||
				pair.Value.Regions.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count() != pair.Value.Regions.Count)
				throw new ArgumentException("Invalid regional evidence.");
		}
		var nextRegions = new Dictionary<string, Dictionary<string, double?[]>>(StringComparer.Ordinal);
		var regional = new Dictionary<string, (double? Response, double? Draw, double? RecordReach, double? ConversionReach)>(StringComparer.Ordinal);
		double alpha = 1 - Math.Pow(.5, 1 / config.RegionalHalfLifeWeeks);
		foreach (var pair in evidence.OrderBy(p => p.Key, StringComparer.Ordinal)) {
			var history = new Dictionary<string, double?[]>(StringComparer.Ordinal);
			RegionalHistory.TryGetValue(pair.Key, out var oldRegions);
			var record = new List<(double? Value, double Population)>();
			var conversion = new List<(double? Value, double Population)>();
			var local = new List<double?>(); var draw = new List<double?>();
			foreach (Region region in pair.Value.Regions) {
				double?[] old = null; oldRegions?.TryGetValue(region.Id, out old);
				var smooth = new double?[4];
				for (int i = 0; i < 4; i++) {
					double? current = region.SalesShares[i], prior = old != null && old.Length == 4 ? old[i] : null;
					smooth[i] = current.HasValue && prior.HasValue ? alpha * current.Value + (1 - alpha) * prior.Value : current;
				}
				history.Add(region.Id, smooth);
				double? strength = smooth.All(x => x.HasValue) ? Math.Min(1, smooth.Max(x => x.Value) / config.LocalSalesShareReference) : null;
				double? density = region.Following.HasValue ? Math.Min(1, region.Following.Value / region.PopulationMillions / config.FollowingPerMillionReference) : null;
				local.Add(strength); draw.Add(density);
				record.Add((strength.HasValue && density.HasValue ? Math.Max(strength.Value, density.Value) : null, region.PopulationMillions));
				conversion.Add((density, region.PopulationMillions));
			}
			double population = pair.Value.Regions.Sum(r => r.PopulationMillions);
			regional.Add(pair.Key, (local.All(x => x.HasValue) ? local.Max(x => x.Value) : null,
				draw.All(x => x.HasValue) ? draw.Max(x => x.Value) : null, Weighted(record, population), Weighted(conversion, population)));
			nextRegions.Add(pair.Key, history);
		}
		Normalizer.Advance(date, evidence.ToDictionary(x => x.Key, x => x.Value.Components, StringComparer.Ordinal), config.Normalization,
			new HashSet<string>(evidence.Where(x => x.Value.PeerEligible).Select(x => x.Key), StringComparer.Ordinal));
		RegionalHistory = nextRegions; ConfigVersion = config.Version;
		if (date < config.Policy.EffectiveDate.Date) return true;
		var routes = new Dictionary<string, StatureShadowPolicy.Route[]>(StringComparer.Ordinal);
		var results = new Dictionary<string, StatureShadowPolicy.Result>(StringComparer.Ordinal);
		var previousBands = new Dictionary<string, StatureShadowPolicy.Band>(StringComparer.Ordinal);
		foreach (var pair in Normalizer.Latest) {
			var v = pair.Value.Score; var r = regional[pair.Key];
			var following = Maximum(v[6], v[7]);
			StatureShadowPolicy.Range Range(double? value) => value.HasValue ? StatureShadowPolicy.Range.Exact(value.Value) : StatureShadowPolicy.Range.Missing;
			var candidateRoutes = new[] {
				new StatureShadowPolicy.Route(Range(r.Response), Range(r.Draw), Range(r.RecordReach)),
				new StatureShadowPolicy.Route(Maximum(v[0], v[1], v[2], v[3]), following, Range(r.RecordReach)),
				new StatureShadowPolicy.Route(Maximum(v[4], v[5]), following, Range(r.ConversionReach)) };
			routes.Add(pair.Key, candidateRoutes);
			var previous = Ledger.Artists.TryGetValue(pair.Key, out var old) ? old.PriorBand : StatureShadowPolicy.Band.BelowRising;
			previousBands.Add(pair.Key, previous);
			results.Add(pair.Key, StatureShadowPolicy.Evaluate(candidateRoutes, config.Policy, date, previous));
		}
		Ledger.Advance(date, routes, config.Policy);
		foreach (var pair in Ledger.Artists.Where(p => p.Value.ObservationDate == date)) {
			var read = Normalizer.Latest[pair.Key]; var evidenceRegional = regional[pair.Key]; var result = results[pair.Key];
			Snapshots[pair.Key] = new Snapshot { Date = date, Band = pair.Value.PriorBand, Status = pair.Value.Status,
				Components = evidence[pair.Key].Components, References = read.Reference, Scores = read.Score,
				RegionalResponse = evidenceRegional.Response, RegionalDraw = evidenceRegional.Draw,
				RecordReach = evidenceRegional.RecordReach, ConversionReach = evidenceRegional.ConversionReach,
				PreviousBand = previousBands[pair.Key], SupportLower = result.Bands.Select(x => x.Value.Lower).ToArray(),
				SupportUpper = result.Bands.Select(x => x.Value.Upper).ToArray(), Boundaries = result.Bands.Select(x => x.Boundary).ToArray(),
				RouteSupportLower = result.Bands.Select(x => x.Routes.Select(r => r.Lower).ToArray()).ToArray() };
		}
		return true;
	}
}
