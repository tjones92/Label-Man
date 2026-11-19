using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>
/// --singles-phenomenon=S (LpGenreAllocationFollowup.md §73 stage C, §97). The singles purse is fixed and every title in a
/// region clears the same fraction of its intent (about 11% in 1960-61), so a record's units are its share of the week's
/// intent and no act's smoothed share of the national singles market passes 4-5% in any seed. A 1964-style act held the
/// top five of the Hot 100: buyers who wanted several records bought that act's first, and buyers who did not follow its
/// genre bought it too.
/// <para>Each act keeps an exponentially smoothed share of the national singles units cleared each week (half-life
/// <c>--singles-phenomenon-half-life</c>, 8 weeks). The first week that share reaches S the act becomes a phenomenon,
/// once in its career. From then an envelope e rises linearly over <c>--singles-phenomenon-ramp</c> weeks (4) and halves
/// every <c>--singles-phenomenon-fade</c> weeks (52). While e is up, the act's singles clear ahead of the ration with
/// weight 1 + (W − 1)·e in their region's purse (<c>--singles-phenomenon-weight=W</c>, 6; capped at each title's own
/// intent, the purse spent exactly) and their raw demand is multiplied by 1 + (A − 1)·e
/// (<c>--singles-phenomenon-audience=A</c>, 1.5: buyers from outside the act's genre). Raw demand is still clipped by
/// the record's stock and shelf space. The purse does not grow: what the act gains, other titles lose.</para>
/// <para>Rarity is set by S. In <c>stB</c> (4 seeds, 104 weeks) the largest 8-week smoothed share any act reached was
/// 3.2-4.0%. Fresh-world only (the share ledger is not saved). No draws. Off: never read, and the ration runs today's code.</para>
/// </summary>
public static class SinglesPhenomenon {
	public const string Switch = "--singles-phenomenon", WeightSwitch = "--singles-phenomenon-weight",
		AudienceSwitch = "--singles-phenomenon-audience", HalfLifeSwitch = "--singles-phenomenon-half-life",
		FadeSwitch = "--singles-phenomenon-fade", RampSwitch = "--singles-phenomenon-ramp";
	public const float DefaultWeight = 6f, DefaultAudience = 1.5f, DefaultHalfLife = 8f, DefaultFade = 52f, DefaultRamp = 4f;
	/// <summary>Below this envelope a phenomenon is over (its weight and audience are within 2% of 1).</summary>
	public const float EndEnvelope = 0.02f;

	private static bool read;
	private static float threshold, weight = DefaultWeight, audience = DefaultAudience, fade = DefaultFade, ramp = DefaultRamp;
	private static double retain = Math.Pow(0.5, 1.0 / DefaultHalfLife);
	private static int lastWeek = int.MinValue, currentWeek = int.MinValue;
	private static readonly Dictionary<string, double> share = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, long> pending = new(StringComparer.Ordinal);
	private static long pendingTotal;
	private static readonly Dictionary<string, int> onset = new(StringComparer.Ordinal);
	private static readonly Dictionary<string, long> lastUnits = new(StringComparer.Ordinal);

	private static void Read() {
		if (read) return;
		threshold = GameyAuditArguments.Value(Switch, 0.005f, 0.5f) ?? 0f;
		float? w = GameyAuditArguments.Value(WeightSwitch, 1f, 20f), a = GameyAuditArguments.Value(AudienceSwitch, 1f, 5f),
			h = GameyAuditArguments.Value(HalfLifeSwitch, 1f, 52f), f = GameyAuditArguments.Value(FadeSwitch, 4f, 520f),
			r = GameyAuditArguments.Value(RampSwitch, 1f, 26f);
		if (threshold <= 0f && (w ?? a ?? h ?? f ?? r) != null)
			throw new ArgumentException("The --singles-phenomenon-* settings need " + Switch + "=S.");
		if (threshold > 0f) {
			string[] args = SimulationArguments.Get();
			if (args.Any(x => x.StartsWith("--resume-world", StringComparison.Ordinal) || x.StartsWith("--save-world", StringComparison.Ordinal)))
				throw new ArgumentException(Switch + " is a fresh-world research switch; the share ledger is not saved.");
			if (args.Any(x => x.StartsWith(SinglesRationExponent.Switch, StringComparison.Ordinal) && x != SinglesRationExponent.Switch + "1.0" && x != SinglesRationExponent.Switch + "1"))
				throw new ArgumentException(Switch + " runs on the proportional ration; drop " + SinglesRationExponent.Switch + ".");
			weight = w ?? DefaultWeight; audience = a ?? DefaultAudience; fade = f ?? DefaultFade; ramp = r ?? DefaultRamp;
			retain = Math.Pow(0.5, 1.0 / (h ?? DefaultHalfLife));
			Godot.GD.Print(string.Format(CultureInfo.InvariantCulture,
				"SINGLES_PHENOMENON threshold={0} weight={1} audience={2} halfLife={3} fade={4} ramp={5}",
				threshold, weight, audience, h ?? DefaultHalfLife, fade, ramp));
		}
		read = true;
	}

	internal static void SetForChecks(float s, float w = DefaultWeight, float a = DefaultAudience, float halfLife = DefaultHalfLife,
		float fadeWeeks = DefaultFade, float rampWeeks = DefaultRamp) {
		read = true; threshold = s; weight = w; audience = a; fade = fadeWeeks; ramp = rampWeeks;
		retain = Math.Pow(0.5, 1.0 / halfLife);
		lastWeek = currentWeek = int.MinValue; share.Clear(); pending.Clear(); pendingTotal = 0; onset.Clear(); lastUnits.Clear();
	}

	public static bool Enabled { get { Read(); return threshold > 0f; } }

	/// <summary>The act's smoothed share of national singles units (0 when unknown).</summary>
	public static double Share(string artistId) => !string.IsNullOrEmpty(artistId) && share.TryGetValue(artistId, out double s) ? s : 0d;
	public static bool IsPhenomenon(string artistId) => !string.IsNullOrEmpty(artistId) && onset.ContainsKey(artistId);

	/// <summary>The envelope at <see cref="currentWeek"/>: 0 before onset; rises over the ramp, halves every fade.</summary>
	public static float Envelope(string artistId) {
		if (!Enabled || string.IsNullOrEmpty(artistId) || !onset.TryGetValue(artistId, out int start)) return 0f;
		return EnvelopeAt(currentWeek - start, ramp, fade);
	}

	internal static float EnvelopeAt(int weeksSinceOnset, float rampWeeks, float fadeWeeks) {
		if (weeksSinceOnset < 0) return 0f;
		float e = Math.Min(1f, (weeksSinceOnset + 1) / rampWeeks) * (float)Math.Pow(0.5, weeksSinceOnset / fadeWeeks);
		return e < EndEnvelope ? 0f : e;
	}

	/// <summary>Ration weight of the act's singles: 1 off or outside a phenomenon.</summary>
	public static float RationWeight(string artistId) => 1f + (weight - 1f) * Envelope(artistId);
	/// <summary>Multiplier on a phenomenon act's raw singles demand: 1 off or outside a phenomenon.</summary>
	public static float AudienceMultiplier(string artistId) => 1f + (audience - 1f) * Envelope(artistId);

	/// <summary>After clearing: a single's cleared units (local and spillover) join its act's week.</summary>
	public static void AddSingleUnits(Record record, int units) {
		if (!Enabled || units <= 0 || record == null || record.format == ReleaseFormat.Album) return;
		pendingTotal += units;
		if (string.IsNullOrEmpty(record.artistId)) return;
		pending[record.artistId] = pending.GetValueOrDefault(record.artistId) + units;
	}

	/// <summary>Once per clearing week, before demand: last week's units update every act's smoothed share; an act whose
	/// share reaches the threshold for the first time becomes a phenomenon this week.</summary>
	public static void AdvanceWeek(int week) {
		if (!Enabled || week == lastWeek) return;
		if (lastWeek != int.MinValue && pendingTotal > 0) {
			double keep = retain, add = 1.0 - retain;
			foreach (string artist in share.Keys.ToList()) {
				double s = share[artist] * keep;
				if (s < 1e-6 && !pending.ContainsKey(artist)) share.Remove(artist); else share[artist] = s;
			}
			foreach (var pair in pending) share[pair.Key] = share.GetValueOrDefault(pair.Key) + add * pair.Value / pendingTotal;
			lastUnits.Clear();
			foreach (var pair in pending) lastUnits[pair.Key] = pair.Value;
			foreach (var pair in share.Where(p => p.Value >= threshold && !onset.ContainsKey(p.Key)).OrderBy(p => p.Key, StringComparer.Ordinal).ToList()) {
				onset[pair.Key] = week;
				Godot.GD.Print(string.Format(CultureInfo.InvariantCulture, "SINGLES_PHENOMENON_ONSET week={0} artist={1} share={2:0.0000}",
					week, pair.Key, pair.Value));
			}
			long total = pendingTotal;
			currentWeek = week;
			foreach (string artist in onset.Keys.OrderBy(k => k, StringComparer.Ordinal)) {
				float e = Envelope(artist);
				if (e <= 0f && week - onset[artist] > ramp) continue;
				Godot.GD.Print(string.Format(CultureInfo.InvariantCulture,
					"SINGLES_PHENOMENON_WEEK week={0} artist={1} share={2:0.0000} lastWeekShare={3:0.0000} envelope={4:0.000} weight={5:0.00} audience={6:0.00} units={7}",
					week, artist, Share(artist), (double)lastUnits.GetValueOrDefault(artist) / total, e, RationWeight(artist),
					AudienceMultiplier(artist), lastUnits.GetValueOrDefault(artist)));
			}
			if (week % 13 == 0 && share.Count > 0) {
				var top = share.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).First();
				Godot.GD.Print(string.Format(CultureInfo.InvariantCulture, "SINGLES_PHENOMENON_TOP week={0} artist={1} share={2:0.0000}",
					week, top.Key, top.Value));
			}
		}
		pending.Clear(); pendingTotal = 0;
		lastWeek = week; currentWeek = week;
	}

	/// <summary>True when any of these singles belongs to a phenomenon act this week (the weighted ration is needed).</summary>
	public static bool AnyWeighted(IReadOnlyList<int> intents, IReadOnlyList<float> weights) {
		for (int i = 0; i < weights.Count; i++) if (weights[i] != 1f && intents[i] > 0) return true;
		return false;
	}

	/// <summary>
	/// Exact (unrounded) clearing per title: min(intent, λ·m·intent) summing to <paramref name="budget"/>, which must not
	/// exceed the total intent. Titles with the largest weight m reach their intent first. All m = 1 is the proportional ration.
	/// </summary>
	public static double[] Shares(IReadOnlyList<int> intents, IReadOnlyList<float> weights, double budget) {
		int n = intents.Count;
		var exact = new double[n];
		if (n == 0 || budget <= 0) return exact;
		int[] order = Enumerable.Range(0, n).Where(i => intents[i] > 0).OrderByDescending(i => weights[i]).ThenBy(i => i).ToArray();
		double weightLeft = 0;
		foreach (int i in order) weightLeft += (double)weights[i] * intents[i];
		double left = budget, lambda = 0;
		int k = 0;
		while (k < order.Length) {
			lambda = weightLeft > 0 ? left / weightLeft : 0;
			int top = order[k];
			if (lambda * weights[top] < 1.0) break;
			exact[top] = intents[top]; left -= intents[top]; weightLeft -= (double)weights[top] * intents[top]; k++;
		}
		for (int j = k; j < order.Length; j++) exact[order[j]] = Math.Min(intents[order[j]], lambda * weights[order[j]] * intents[order[j]]);
		return exact;
	}
}
