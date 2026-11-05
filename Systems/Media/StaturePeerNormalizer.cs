using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Deterministic weekly normalization for shadow evidence. Inputs are already
/// measured; this class neither gathers evidence nor selects parameter values.
/// Each response component is smoothed once; draw components are already
/// churned and are read directly. References use the previous complete week,
/// exclude the evaluated act, and break ties by ordinal artist ID.
/// </summary>
public sealed class StaturePeerNormalizer {
	public sealed class ComponentConfig {
		public double Floor { get; set; }
		public bool SmoothResponse { get; set; }
	}

	public sealed class Config {
		public string Version { get; set; }
		public double HalfLifeWeeks { get; set; }
		public int TopN { get; set; }
		public int MinimumPeers { get; set; }
		public ComponentConfig[] Components { get; set; }
	}

	public sealed class Read {
		public double?[] Smoothed { get; set; }
		public double?[] Reference { get; set; }
		public double?[] Score { get; set; }
	}

	public DateTime LastProcessedDate { get; set; }
	public string ConfigVersion { get; set; }
	public Dictionary<string, double?[]> Previous { get; set; } = new(StringComparer.Ordinal);
	public HashSet<string> PreviousPeerEligible { get; set; } = new(StringComparer.Ordinal);
	public Dictionary<string, Read> Latest { get; set; } = new(StringComparer.Ordinal);

	public bool Advance(DateTime date, IReadOnlyDictionary<string, double?[]> raw, Config config, ISet<string> peerEligible = null) {
		date = date.Date;
		if (date == default || raw == null || config == null || string.IsNullOrWhiteSpace(config.Version) ||
			!double.IsFinite(config.HalfLifeWeeks) || config.HalfLifeWeeks <= 0 || config.TopN < 1 || config.MinimumPeers < 1 ||
			config.Components == null || config.Components.Length == 0 || config.Components.Any(c => c == null || !double.IsFinite(c.Floor) || c.Floor <= 0))
			throw new ArgumentException("Valid evidence, date and normalization parameters are required.");
		if (LastProcessedDate != default && !string.Equals(ConfigVersion, config.Version, StringComparison.Ordinal))
			throw new InvalidOperationException("A normalizer cannot silently change its configuration.");
		if (date == LastProcessedDate.Date) return false;
		if (LastProcessedDate != default && (date - LastProcessedDate.Date).Days != 7)
			throw new InvalidOperationException("Weeks must advance exactly once in chronological order.");
		if (Previous == null || Latest == null || PreviousPeerEligible == null) throw new InvalidOperationException("Missing normalizer state.");
		if (peerEligible != null && peerEligible.Any(id => !raw.ContainsKey(id))) throw new ArgumentException("Peer eligibility must refer to an observed act.");
		int count = config.Components.Length;
		if (Previous.Any(p => p.Value == null || p.Value.Length != count ||
			p.Value.Any(v => v.HasValue && (!double.IsFinite(v.Value) || v.Value < 0))))
			throw new InvalidOperationException("Invalid prior evidence.");
		foreach (var item in raw) {
			if (string.IsNullOrEmpty(item.Key) || item.Value == null || item.Value.Length != count ||
				item.Value.Any(v => v.HasValue && (!double.IsFinite(v.Value) || v.Value < 0)))
				throw new ArgumentException("Invalid artist evidence.");
		}
		double alpha = 1 - Math.Pow(.5, 1 / config.HalfLifeWeeks);
		var ranked = new List<(string Artist, double Value)>[count];
		var peerCounts = new int[count];
		for (int i = 0; i < count; i++) {
			int index = i;
			var peers = Previous.Where(p => PreviousPeerEligible.Contains(p.Key) && p.Value[index].HasValue).ToList();
			peerCounts[i] = peers.Count;
			ranked[i] = peers.Select(p => (Artist: p.Key, Value: p.Value[index].Value))
				.OrderByDescending(p => p.Value).ThenBy(p => p.Artist, StringComparer.Ordinal)
				.Take(config.TopN == int.MaxValue ? int.MaxValue : config.TopN + 1).ToList();
		}
		var next = new Dictionary<string, double?[]>(StringComparer.Ordinal);
		var reads = new Dictionary<string, Read>(StringComparer.Ordinal);
		foreach (var item in raw.OrderBy(p => p.Key, StringComparer.Ordinal)) {
			Previous.TryGetValue(item.Key, out var old);
			var values = new double?[count]; var references = new double?[count]; var scores = new double?[count];
			for (int i = 0; i < count; i++) {
				double? current = item.Value[i], prior = old?[i];
				values[i] = current.HasValue && config.Components[i].SmoothResponse && prior.HasValue
					? alpha * current.Value + (1 - alpha) * prior.Value : current;
				int available = peerCounts[i] - (prior.HasValue && PreviousPeerEligible.Contains(item.Key) ? 1 : 0);
				if (available < config.MinimumPeers) continue;
				double reference = Math.Max(config.Components[i].Floor,
					ranked[i].Where(p => !string.Equals(p.Artist, item.Key, StringComparison.Ordinal)).Take(config.TopN).Average(p => p.Value));
				references[i] = reference;
				if (values[i].HasValue) scores[i] = Math.Clamp(values[i].Value / reference, 0, 1);
			}
			next.Add(item.Key, (double?[])values.Clone());
			reads.Add(item.Key, new Read { Smoothed = values, Reference = references, Score = scores });
		}
		Previous = next; PreviousPeerEligible = new HashSet<string>((IEnumerable<string>)peerEligible ?? raw.Keys, StringComparer.Ordinal);
		Latest = reads; ConfigVersion = config.Version; LastProcessedDate = date;
		return true;
	}
}
