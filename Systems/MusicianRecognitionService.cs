using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Phase E-lite: attribute a fraction of each realized act-recognition gain down to the active
/// members, weighted by role and stage visibility. This is the ONE live person-level mechanic --
/// a pure read-down from events that already fire (the milestone/run-complete gains in
/// <see cref="ArtistRecognitionService"/>), so it can never be a dead wire and it allocates
/// nothing on an off run.
/// <para>
/// The transfer-on-departure half of the sketches (recognition following a departing singer, solo
/// spin-offs) is BLOCKED: no lineup churn fires at runtime ([[lineup-churn-never-fires]]). See
/// SimTools/CelebrityRecognitionDirective.md §8.
/// </para>
/// </summary>
public static class MusicianRecognitionService {
	/// <summary>Share of a realized act gain distributed across the members (the rest is the act's own name).</summary>
	public const float MemberShare = 0.45f;

	/// <summary>
	/// How much of the public's attention a member catches. The front person carries the act's
	/// name; a sideman carries little. Never zero, so every credited member accrues something.
	/// </summary>
	private static float VisibilityWeight(Musician m) =>
		0.20f + (m.isLeadVocalist ? 0.40f : 0f) + (m.isBandLeader ? 0.15f : 0f) + m.stagePresence * 0.35f;

	/// <summary>
	/// Distribute a realized, bounded act-recognition gain to the current lineup. Called from
	/// <see cref="ArtistRecognitionService.AddPublicRecognition"/> with the ACTUAL delta after
	/// diminishing returns, so members inherit the same saturation the act sees.
	/// </summary>
	public static void ShareArtistRecognitionGain(SimulatedArtist act, float realizedGain) {
		if (!ArtistRecognition.Observing || act == null || realizedGain <= 0f) return;
		List<Musician> members = act.members?.Where(m => m != null && m.isActive).ToList();
		if (members == null || members.Count == 0) return;
		float totalWeight = members.Sum(VisibilityWeight);
		if (totalWeight <= 0f) return;
		float pool = realizedGain * MemberShare;
		foreach (Musician m in members) {
			float share = pool * (VisibilityWeight(m) / totalWeight);
			if (share <= 0f) continue;
			m.personalRecognition = Gain(m.personalRecognition, share);
			// The performer's name and the maker's name are the two durable person-level channels
			// (§8.1/§8.3). A member can hold both; a session drummer holds neither strongly.
			if (m.isLeadVocalist || m.stagePresence > 0.6f)
				m.liveReputation = Gain(m.liveReputation, share * Mathf.Max(m.stagePresence, m.isLeadVocalist ? 0.6f : 0f));
			if (m.isPrimaryWriter || m.creativity > 0.7f)
				m.creativeReputation = Gain(m.creativeReputation, share * Mathf.Max(m.creativity, m.isPrimaryWriter ? 0.6f : 0f));
		}
	}

	private static float Gain(float stock, float raw) => Mathf.Clamp(stock + raw * (1f - stock), 0f, 1f);
}
