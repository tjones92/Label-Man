using System;
using System.Globalization;
using System.Linq;
using Godot;

/// <summary>
/// Calibration probe (SimTools/ReleaseJudgementDirective.md, 2026-10-27 self-penned audit), off unless named.
/// --self-writing-craft-gate=P makes an AI single's choice to self-write depend on the act's writing craft.
///
/// At matched realized hook an artist-written side charts as well as a professional one, so the self-penned deficit is
/// not the source tag. It is who self-writes: new signings' originals average a hook of ~.48 against ~.66 for staff
/// songs in every genre, because the source choice barely reads craft. Under the polar fit the artist-written score is
/// <c>SourceMix × fit</c> with no craft term, and a release-judgement arrival weights craft only 0.4 + 1.2 × craft.
/// Historically a weak writer was handed songs and a strong one kept their own. Under the switch the artist-written
/// share is multiplied by <c>g = Floor + (1 - Floor) × logistic((craft - P) / Width)</c>. In the legacy pick the other
/// candidates absorb the freed weight through the weighted draw. In release-judgement arrivals the other sources are
/// scaled up so total arrivals hold. Albums, the player and the prewarm are untouched. Pure arithmetic, no draws.
/// </summary>
public static class SelfWritingCraftGate {
	public const string Prefix = "--self-writing-craft-gate=";
	public const float Width = 0.06f, Floor = 0.10f;
	public static readonly float Pivot = Read();
	public static bool Enabled => Pivot >= 0f;

	private static float Read() {
		var values = SimulationArguments.Get().Where(a => a.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();
		if (values.Length == 0) return -1f;
		if (values.Length > 1) throw new ArgumentException("Name " + Prefix + " once.");
		if (!float.TryParse(values[0].Substring(Prefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float p) || !(p >= 0f && p <= 1f))
			throw new ArgumentException(Prefix + " needs a craft pivot in [0, 1].");
		return p;
	}

	/// <summary>Live weeks only. The pre-1960 prewarm (chart week 0) keeps its own path, so the opening world is the same
	/// with the switch on or off and an A/B reads the gate, not a reshuffled opening chart.</summary>
	private static bool Live => (ChartManager.Instance?.GetCurrentChartWeek() ?? 0) > 0;

	/// <summary>The multiplier on the artist-written share for a writer of this craft: 1 when off or in the prewarm.</summary>
	public static float Multiplier(float writerCraft) =>
		!Enabled || !Live ? 1f : Floor + (1f - Floor) / (1f + Mathf.Exp(-(writerCraft - Pivot) / Width));

	/// <summary>Gate the artist-written share and hand what it frees to the other sources in proportion.</summary>
	public static SourceShares Apply(SourceShares s, float writerCraft) {
		float g = Multiplier(writerCraft);
		if (g >= 1f || s.Aw >= 1f) return s;
		float rest = 1f - s.Aw, scale = (1f - s.Aw * g) / rest;
		return new SourceShares { Aw = s.Aw * g, Pro = s.Pro * scale, Std = s.Std * scale, Hit = s.Hit * scale, Trad = s.Trad * scale };
	}
}
