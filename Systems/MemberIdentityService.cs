using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Phase 7 of SimTools/BandMemberSimulationDirective.md (§4.12), behind <c>--enable-member-identity</c>: identity lives
/// on people.
/// <para>
/// <b>Generation.</b> A person is drawn near their act's genre prior (bands form from people who want the same thing),
/// with a small keyed spread inside each band, plus the bounded trait adjustments the act deriver used to apply to the
/// whole act (temperament -> toughness, versatility -> sophistication, reliability -> sincerity) and a little maturity
/// with age. <b>Drift.</b> Each year a member moves a step toward the prior of the music their act now plays (the
/// exposure that ratified evolution leaves behind), and a settled partner pulls toward maturity and sincerity.
/// </para>
/// <para>
/// <b>Readers.</b> The act's identity axes in <see cref="PolarActProfileDeriver"/> become the voice-weighted mean of its
/// members (the evolution ratifier and the project-history blend still apply on top -- this does not replace them);
/// the spread of identities inside a band feeds the Direction strain cause and a bounded interpretive-reach term.
/// </para>
/// </summary>
public static class MemberIdentityService {
	public const int CurrentIdentityVersion = 1;
	public const float InBandSpread = 0.06f;
	public const float MaturityPerYearOfAge = 0.006f, MaturityAgeCap = 0.08f;
	public const float ExposureDrift = 0.08f;
	public const float PartnerMaturityPull = 0.06f, ChildrenMaturityPull = 0.05f, PartnerSincerityPull = 0.03f, PartnerPullRate = 0.10f;
	public const float DriftBound = 0.25f;
	/// <summary>Direction's identity half (§14): a power of the distance, so small differences inside a band are livable
	/// and large ones blow up. Fitted on the 1967 pass of the 1968 world (probe-dir-on/off pair logs): a linear term at
	/// the proxy's median had its 90th/99th percentiles at .35/.48 against the proxy's .46/.75 and cut Direction's share
	/// of departures 22% -> 13%; the power 1.58 matches median and tail.</summary>
	public const float DirectionIdentityShare = 0.50f, DirectionIdentityMedianRaw = 0.1536f, DirectionProxyMedian = 0.211f,
		DirectionIdentityPower = 1.58f;
	public const float ReachPerSpread = 0.50f, ReachCap = 0.06f;

	public static bool Enabled => BandLife.MemberIdentityEnabled;

	public static float[] Of(Musician m) => m.identityVersion > 0
		? new[] { m.identityToughness, m.identitySophistication, m.identitySincerity, m.identityMaturity } : null;

	/// <summary>Generates a person's identity if they don't have one. Keyed on the world seed; touches no stream.</summary>
	public static void EnsureIdentity(Musician m, SimulatedArtist artist, int year) {
		if (!Enabled || m == null || m.identityVersion >= CurrentIdentityVersion || artist == null) return;
		PolarSongTable table = PolarSongTable.Current;
		float[] prior = table.Prior(artist.primaryGenre).Identity;
		string k = $"identity|{m.personId}";
		float neutral = table.N("neutralTrait"), adjust = table.N("traitIdentityAdjustment");
		float age = m.GetAge(year);
		m.identityToughness = Clamp01(prior[0] + InBandSpread * BandLife.Normal(k + "|t") + (neutral - m.temperament) * adjust);
		m.identitySophistication = Clamp01(prior[1] + InBandSpread * BandLife.Normal(k + "|s") + (m.musicalVersatility - neutral) * adjust);
		m.identitySincerity = Clamp01(prior[2] + InBandSpread * BandLife.Normal(k + "|n") + (m.reliability - neutral) * adjust);
		m.identityMaturity = Clamp01(prior[3] + InBandSpread * BandLife.Normal(k + "|m") +
			Mathf.Clamp((age - 25f) * MaturityPerYearOfAge, -MaturityAgeCap, MaturityAgeCap));
		m.identityGenerated = new[] { m.identityToughness, m.identitySophistication, m.identitySincerity, m.identityMaturity };
		m.identityVersion = CurrentIdentityVersion;
	}

	/// <summary>The annual drift for one act's people (band-life pass).</summary>
	public static void OnActYear(SimulatedArtist a, List<Musician> present, int year) {
		if (!Enabled || a == null || present == null) return;
		float[] prior = PolarSongTable.Current.Prior(a.primaryGenre).Identity;
		foreach (Musician m in present) {
			EnsureIdentity(m, a, year);
			if (m.identityVersion == 0) continue;
			float[] v = Of(m);
			for (int i = 0; i < 4; i++) v[i] += (prior[i] - v[i]) * ExposureDrift;
			if (m.partner != null && m.partner.state == PartnerState.Married) {
				float maturityTarget = v[3] + PartnerMaturityPull + (m.hasChildren ? ChildrenMaturityPull : 0f);
				v[3] += (maturityTarget - v[3]) * PartnerPullRate;
				v[2] += PartnerSincerityPull * PartnerPullRate;
			}
			float[] g = m.identityGenerated ?? v;
			m.identityToughness = Clamp01(Mathf.Clamp(v[0], g[0] - DriftBound, g[0] + DriftBound));
			m.identitySophistication = Clamp01(Mathf.Clamp(v[1], g[1] - DriftBound, g[1] + DriftBound));
			m.identitySincerity = Clamp01(Mathf.Clamp(v[2], g[2] - DriftBound, g[2] + DriftBound));
			m.identityMaturity = Clamp01(Mathf.Clamp(v[3], g[3] - DriftBound, g[3] + DriftBound));
		}
	}

	/// <summary>The voice-weighted mean identity of an act's active members, or null when none carries one.</summary>
	public static float[] Aggregate(SimulatedArtist a, IEnumerable<Musician> members) {
		float[] sum = new float[4];
		float weight = 0f;
		foreach (Musician m in members) {
			float[] v = Of(m);
			if (v == null) continue;
			float w = BandLifeService.VoiceWeight(m, a);
			for (int i = 0; i < 4; i++) sum[i] += v[i] * w;
			weight += w;
		}
		if (weight <= 0f) return null;
		for (int i = 0; i < 4; i++) sum[i] /= weight;
		return sum;
	}

	/// <summary>Voice-weighted RMS distance of members from the act's mean: how much the band disagrees about who it is.</summary>
	public static float Spread(SimulatedArtist a, IEnumerable<Musician> members) {
		var list = members.Where(m => m.identityVersion > 0).ToList();
		float[] mean = Aggregate(a, list);
		if (mean == null || list.Count < 2) return 0f;
		float total = 0f, weight = 0f;
		foreach (Musician m in list) {
			float w = BandLifeService.VoiceWeight(m, a);
			total += w * Distance2(Of(m), mean);
			weight += w;
		}
		return Mathf.Sqrt(total / weight);
	}

	public static float Distance(Musician x, Musician y) {
		float[] a = Of(x), b = Of(y);
		return a == null || b == null ? 0f : Mathf.Sqrt(Distance2(a, b));
	}

	/// <summary>Reader: the Direction raw term, half the old trait proxy and half identity distance, when on.</summary>
	public static float DirectionRaw(float proxyRaw, Musician x, Musician y) {
		if (!Enabled || x.identityVersion == 0 || y.identityVersion == 0) return proxyRaw;
		float raw = Distance(x, y) * (0.5f + Mathf.Max(x.creativity, y.creativity));
		float identity = DirectionProxyMedian * Mathf.Pow(raw / DirectionIdentityMedianRaw, DirectionIdentityPower);
		return (1f - DirectionIdentityShare) * proxyRaw + DirectionIdentityShare * identity;
	}

	/// <summary>Reader: a band whose people want different things can reach further from its centre.</summary>
	public static float ReachBonus(SimulatedArtist a, IEnumerable<Musician> members) =>
		Enabled ? Mathf.Min(ReachCap, ReachPerSpread * Spread(a, members)) : 0f;

	private static float Distance2(float[] a, float[] b) {
		float d = 0f;
		for (int i = 0; i < 4; i++) d += (a[i] - b[i]) * (a[i] - b[i]);
		return d;
	}

	private static float Clamp01(float v) => Mathf.Clamp(v, 0f, 1f);
}
