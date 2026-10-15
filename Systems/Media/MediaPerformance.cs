using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>How a television segment came off (§6). Flat-but-adequate is the common professional result, not a failure.</summary>
public enum PerformanceOutcome { Breakdown, Weak, FlatAdequate, Engaging, Electrifying }

/// <summary>Who a problem belongs to (§6): the act, the label's preparation, the production team, or the equipment.</summary>
public enum PerformanceFault { None, Artist, Preparation, Production, Equipment }

/// <summary>Rehearsal the label arranged (§5.3). Extra rehearsal has diminishing returns: it trims avoidable errors, not nerves.</summary>
public enum MediaPreparation { Minimal = 0, Standard = 1, Extra = 2 }

/// <summary>The resolved segment: the musical result and the production result kept apart (§6).</summary>
public sealed class MediaPerformanceResult {
	public PerformanceOutcome Outcome;
	public PerformanceFault Fault;
	/// <summary>What the audience heard was the record itself (a mime): the sound can't fail, only the picture.</summary>
	public bool SoundFromMaster;
	/// <summary>A taped segment that went wrong and was shot again with the producer's cooperation.</summary>
	public bool Retaken;
	/// <summary>Plain account of what happened, built from the resolved facts (never a second engine).</summary>
	public string Account;
	/// <summary>Whose voice or presence decided it: the lead singer's name, never a band average.</summary>
	public string KeyPerformer;
}

/// <summary>
/// Resolves one television segment (directive §6). A great record is not a great television act: the musical result reads
/// the people who actually perform the parts (the lead singer's own voice, the instrumentalists' own hands, the weakest
/// player pulling a live band down) and the production result reads presence, preparation and the broadcast method. One
/// keyed draw per purpose on the appearance id, so the same booking always resolves the same way and nothing reads a
/// global stream. Pure reads: no musician, record or master value is written; a poor segment never changes the master.
/// </summary>
public static class MediaPerformance {
	/// <summary>Preparation's share of readiness (diminishing returns).</summary>
	private static float PrepReadiness(MediaPreparation prep) => prep switch { MediaPreparation.Extra => 0.80f, MediaPreparation.Standard => 0.55f, _ => 0.15f };

	private static float Vocal(Musician m) {
		if (m == null) return 0.45f;
		float axes = m.vocalControl + m.vocalPower + m.diction;
		// Older members without vocal axes fall back to their general musicianship.
		return axes > 0.01f ? 0.5f * m.vocalControl + 0.3f * m.vocalPower + 0.2f * m.diction : m.technicalSkill;
	}

	private static float Hands(Musician m) => m == null ? 0.45f : m.instrumentalSkill > 0.01f ? m.instrumentalSkill : m.technicalSkill;

	/// <summary>The musical skill the method exposes. A mime exposes none: the prerecorded master supplies the sound.</summary>
	public static float MusicalSkill(PerformanceMethod method, SimulatedArtist artist) {
		var members = artist?.members?.Where(m => m.isActive).ToList() ?? new List<Musician>();
		Musician lead = members.FirstOrDefault(m => m.isLeadVocalist) ?? members.FirstOrDefault();
		float vocal = Vocal(lead);
		var players = members.Where(m => m != lead || members.Count == 1).ToList();
		float handsAvg = players.Count == 0 ? 0.45f : players.Average(Hands);
		float handsMin = players.Count == 0 ? 0.45f : players.Min(Hands);
		// A strong guitarist doesn't rescue a weak singer by raising the band average: the weakest hands pull a live band down.
		float band = 0.7f * handsAvg + 0.3f * handsMin;
		bool instrumental = artist?.instrumentalPerformance == true;
		return method switch {
			PerformanceMethod.Mime => 1f,
			PerformanceMethod.LiveVocalOverTrack => instrumental ? band : vocal,
			PerformanceMethod.LiveBandAndVocal => instrumental ? band : 0.55f * vocal + 0.45f * band,
			// Unfamiliar accompaniment: the voice carries it, reading the chart helps.
			_ => instrumental ? 0.7f * band + 0.3f * (lead?.sightReading ?? 0.4f) : 0.8f * vocal + 0.2f * (lead?.sightReading ?? lead?.technicalSkill ?? 0.4f),
		};
	}

	/// <summary>Presence on camera: the front person counts most, the rest of the group still shows.</summary>
	public static float Presence(SimulatedArtist artist) {
		var members = artist?.members?.Where(m => m.isActive).ToList() ?? new List<Musician>();
		if (members.Count == 0) return 0.45f;
		Musician lead = members.FirstOrDefault(m => m.isLeadVocalist) ?? members[0];
		return members.Count == 1 ? lead.stagePresence : 0.6f * lead.stagePresence + 0.4f * members.Average(m => m.stagePresence);
	}

	private static float Reliability(SimulatedArtist artist) {
		var members = artist?.members?.Where(m => m.isActive).ToList() ?? new List<Musician>();
		return members.Count == 0 ? 0.6f : 0.6f * members.Average(m => m.reliability) + 0.4f * members.Min(m => m.reliability);
	}

	/// <summary>
	/// Resolve a segment. <paramref name="key"/> is the appearance id: every draw is keyed on it and a purpose word.
	/// Prepared, skilled acts usually deliver; sloppy, under-rehearsed ones carry a meaningful bad-show tail. Equipment
	/// trouble is its own small draw, never charged to the act. A taped show can retake a failure if the producer will.
	/// </summary>
	public static MediaPerformanceResult Resolve(string key, PerformanceMethod method, BroadcastMethod broadcast, SimulatedArtist artist, MediaPreparation prep) {
		float U(string purpose) => MediaAtmosphere.Unit("perform|" + key + "|" + purpose);
		float musical = MusicalSkill(method, artist);
		float presence = Presence(artist);
		float reliability = Reliability(artist);
		float ready = PrepReadiness(prep);
		bool mime = method == PerformanceMethod.Mime;
		Musician lead = artist?.members?.FirstOrDefault(m => m.isActive && m.isLeadVocalist) ?? artist?.members?.FirstOrDefault(m => m.isActive);
		string leadName = lead == null ? artist?.stageName : (lead.firstName + " " + lead.lastName).Trim();

		var result = new MediaPerformanceResult { SoundFromMaster = mime, KeyPerformer = leadName };

		// Equipment and production accidents: small, separate, and never the act's fault.
		float accident = broadcast == BroadcastMethod.Live ? 0.015f : 0.010f;
		bool equipment = U("equipment") < accident;

		// The bad-show tail. Skill and preparation shrink it; a mime can only fail on camera (wrong speed, an obvious miss).
		float skill = mime ? 0.35f + 0.65f * presence : 0.6f * musical + 0.25f * presence + 0.15f * reliability;
		float badChance = mime
			? 0.01f + 0.05f * (1f - ready) * (1f - 0.5f * reliability)
			: 0.01f + 0.30f * MathF.Pow(1f - skill, 1.5f) * (1f - 0.55f * ready);
		bool bad = U("tail") < badChance;

		if (equipment || bad) {
			bool severe = U("severity") < (mime ? 0.25f : 0.35f + 0.3f * (1f - skill));
			result.Outcome = severe ? PerformanceOutcome.Breakdown : PerformanceOutcome.Weak;
			result.Fault = equipment ? PerformanceFault.Equipment
				: ready < 0.5f && U("blame") < 0.6f ? PerformanceFault.Preparation : PerformanceFault.Artist;
			// Tape forgives: with time and a willing producer, the bad take is shot again. Live is public.
			if (broadcast == BroadcastMethod.Taped && U("retake") < 0.70f) {
				result.Retaken = true;
				result.Outcome = PerformanceOutcome.FlatAdequate;
			}
		} else {
			// Above the tail the act's skill decides how far past adequate it goes, with a keyed spread for the night.
			float spark = (mime ? 0.35f + 0.65f * presence : 0.55f * musical + 0.45f * presence) + 0.10f * ready + 0.28f * (U("spark") - 0.5f);
			result.Outcome = spark > 0.97f ? PerformanceOutcome.Electrifying : spark > 0.76f ? PerformanceOutcome.Engaging : PerformanceOutcome.FlatAdequate;
		}
		result.Account = Account(result, method, broadcast, leadName);
		return result;
	}

	private static string Account(MediaPerformanceResult r, PerformanceMethod method, BroadcastMethod broadcast, string lead) {
		string who = string.IsNullOrEmpty(lead) ? "the act" : lead;
		if (r.Retaken) return r.Fault == PerformanceFault.Equipment
			? "A playback fault stopped the first take; they shot it again and the aired version is clean."
			: $"The first take went wrong and the producer let them go again; the aired version is serviceable.";
		return r.Outcome switch {
			PerformanceOutcome.Breakdown => r.Fault switch {
				PerformanceFault.Equipment => method == PerformanceMethod.Mime ? "The playback ran at the wrong speed on air." : "The sound failed on air.",
				PerformanceFault.Preparation => $"Under-rehearsed and it showed: the number fell apart{(broadcast == BroadcastMethod.Live ? " on live television" : "")}.",
				_ => method == PerformanceMethod.Mime ? $"{who} lost the mime badly; the record kept going without them." : $"{who}'s voice gave out and the number came apart.",
			},
			PerformanceOutcome.Weak => method == PerformanceMethod.Mime ? "The mime was visibly out of step." : $"Audibly weak: {who} was off the note in places.",
			PerformanceOutcome.Electrifying => method == PerformanceMethod.Mime ? $"{who} owned the cameras; the dancers stopped to watch." : $"{who} was electrifying; the studio audience wouldn't settle.",
			PerformanceOutcome.Engaging => "A lively, confident segment.",
			_ => "Flat but professional.",
		};
	}
}
