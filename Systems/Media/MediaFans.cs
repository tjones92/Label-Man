using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Fan episode states (directive §10.2). Cooldown is the minimum rest after a national episode ends.</summary>
public enum FanEpisodeState { None, LocalExcitement, RegionalFavorite, NationalSensation, CulturalPhenomenon, Cooldown }

/// <summary>
/// Independent evidence families observed in one week (§10.1, §10.2). Sales and television response are audience
/// behavior; coverage is not. Ten papers repeating one item are one Coverage, never ten families.
/// </summary>
[Flags]
public enum FanEvidence { None = 0, Sales = 1, TvResponse = 2, Coverage = 4 }

/// <summary>One completed week's frozen fan evidence for an act: what the weekly evaluation reads.</summary>
public sealed class FanWeek {
	public int Day;
	public FanEvidence Families;
	/// <summary>Regions with attachment at the warm and hot thresholds, and regions where a record of the act sells near the top.</summary>
	public int WarmRegions, HotRegions, SellingRegions;
	/// <summary>A national airing or a teen-magazine item in the last four weeks.</summary>
	public bool NationalExposure;
	/// <summary>Extraordinary response: a top-three record broken in most regional markets, or an event-sized precursor.</summary>
	public bool Extraordinary;
}

/// <summary>
/// One act's media-owned fan record (§2 teenAttachment, fan frenzy episode; §10.3 member spotlight). Nothing outside media
/// reads it: no recognition, momentum, sales, signing or band-life system. Sparse by region and by face.
/// </summary>
public sealed class MediaFanState {
	public string ArtistId, ArtistName;
	/// <summary>Teen attachment by serviced region, [0,1], eight-week half-life without fresh response.</summary>
	public Dictionary<string, float> Attachment = new(StringComparer.Ordinal);
	public FanEpisodeState State;
	public int StateSinceDay, CooldownUntilDay = -1;
	public FanEpisodeState Peak;
	public int PeakDay = -1;
	/// <summary>The qualifying level of each of the last four completed weeks, newest last (§10.2 persistence).</summary>
	public List<int> Levels = new();
	public FanEvidence LastFamilies;
	/// <summary>Media spotlight by member (personId): which faces the cameras and the photographers found. Never canonical recognition (§12.4).</summary>
	public Dictionary<string, float> Spotlight = new(StringComparer.Ordinal);
	public Dictionary<string, string> Faces = new(StringComparer.Ordinal);
	/// <summary>A national episode running a long time on the same record: editors want a new angle (§10.2 plateau).</summary>
	public bool Plateau;
	public string BestRecordId;
	public int BestRecordSinceDay;
	/// <summary>Estimated fan letters after television, cumulative (color only).</summary>
	public long Letters;
	public int TransitionDay = -1;
	public FanEpisodeState TransitionFrom;
	public List<string> History = new();
	public int LastActiveDay;
}

/// <summary>
/// The arithmetic of attachment and episodes, pure so the fixtures can drive it with synthetic weeks (§10, §16.1 fixture 11).
/// Initial design values: eight-week half-life; a four-week window; two qualifying weeks to enter a national sensation,
/// four to enter a cultural phenomenon; an eight-week cooldown after a national episode ends. No quota, no guarantee.
/// </summary>
public static class MediaFans {
	public const int WindowWeeks = 4;
	public const int CooldownDays = 56;
	public const float WarmAt = 0.25f, HotAt = 0.45f;
	/// <summary>Weekly retention for an eight-week half-life.</summary>
	public static readonly float WeeklyRetention = (float)Math.Pow(0.5, 1.0 / 8.0);
	/// <summary>Spotlight fades over about half a year.</summary>
	public static readonly float SpotlightRetention = (float)Math.Pow(0.5, 1.0 / 26.0);
	/// <summary>Most any one region's attachment can move in a week, however much happened (§10.1 weekly saturation).</summary>
	public const float MaxWeeklyGain = 0.12f;
	public const int PlateauDays = 7 * 16;

	/// <summary>attachment += eligibleGain × (1 − attachment), the gain capped per week (§10.1).</summary>
	public static float Deposit(float attachment, float gain) {
		gain = Math.Clamp(gain, 0f, MaxWeeklyGain);
		return Math.Clamp(attachment + gain * (1f - attachment), 0f, 1f);
	}

	public static bool Behavior(FanEvidence f) => (f & (FanEvidence.Sales | FanEvidence.TvResponse)) != 0;
	public static int Count(FanEvidence f) => ((f & FanEvidence.Sales) != 0 ? 1 : 0) + ((f & FanEvidence.TvResponse) != 0 ? 1 : 0) + ((f & FanEvidence.Coverage) != 0 ? 1 : 0);

	/// <summary>The highest episode level one week's evidence qualifies for, 0-4.</summary>
	public static int Level(FanWeek w, int regions) {
		if (regions <= 0) return 0;
		bool sales = (w.Families & FanEvidence.Sales) != 0;
		int level = 0;
		if (w.WarmRegions >= 1 && sales) level = 1;
		if (level == 1 && w.WarmRegions >= 2 && w.SellingRegions >= 2) level = 2;
		bool national = level == 2 && w.HotRegions >= Math.Max(3, (int)Math.Ceiling(0.3 * regions)) && w.WarmRegions >= (int)Math.Ceiling(0.5 * regions)
			&& w.NationalExposure && Count(w.Families) >= 2 && Behavior(w.Families);
		if (national) level = 3;
		if (national && w.Extraordinary && w.HotRegions >= (int)Math.Ceiling(0.6 * regions)) level = 4;
		return level;
	}

	/// <summary>
	/// Push one completed week and move the state (§10.2). Entering needs persistence; leaving a national episode starts a
	/// cooldown in which only local and regional affection remain. Returns true when the state changed.
	/// </summary>
	public static bool Advance(MediaFanState s, FanWeek w, int regions) {
		int level = Level(w, regions);
		s.Levels.Add(level);
		while (s.Levels.Count > WindowWeeks) s.Levels.RemoveAt(0);
		s.LastFamilies = w.Families;
		int AtLeast(int k) => s.Levels.Count(x => x >= k);
		int target = AtLeast(4) >= 4 ? 4 : AtLeast(3) >= 2 ? 3 : AtLeast(2) >= 2 ? 2 : AtLeast(1) >= 1 ? 1 : 0;
		FanEpisodeState old = s.State;
		FanEpisodeState next;
		if (old == FanEpisodeState.Cooldown && w.Day < s.CooldownUntilDay) next = FanEpisodeState.Cooldown;
		else if ((old is FanEpisodeState.NationalSensation or FanEpisodeState.CulturalPhenomenon) && target < 3) {
			// A phenomenon that slips settles back to a sensation; a sensation that slips is over, and rests (§10.2).
			next = old == FanEpisodeState.CulturalPhenomenon && AtLeast(3) >= 1 ? FanEpisodeState.NationalSensation : FanEpisodeState.Cooldown;
			if (next == FanEpisodeState.Cooldown) s.CooldownUntilDay = w.Day + CooldownDays;
		} else if (old == FanEpisodeState.CulturalPhenomenon && target == 3) next = old; // a phenomenon holds while it stays national
		else next = (FanEpisodeState)target;
		if (next == old) return false;
		s.TransitionFrom = old;
		s.State = next;
		s.StateSinceDay = w.Day;
		s.TransitionDay = w.Day;
		if (next != FanEpisodeState.Cooldown && next > s.Peak) { s.Peak = next; s.PeakDay = w.Day; }
		if (next < FanEpisodeState.NationalSensation || next == FanEpisodeState.Cooldown) s.Plateau = false;
		return true;
	}

	/// <summary>
	/// The hype gate (Phase 2's open leg): an event night's tune-in is reserved for an act the public is already behaving
	/// toward like a phenomenon. Without a national episode, hype stops at the tune-in threshold; a sensation reaches 0.8;
	/// only a cultural phenomenon reaches the ceiling.
	/// </summary>
	public static float HypeCap(FanEpisodeState state) => state switch {
		FanEpisodeState.CulturalPhenomenon => 1f,
		FanEpisodeState.NationalSensation => 0.8f,
		FanEpisodeState.RegionalFavorite => 0.55f,
		_ => 0.5f,
	};

	public static string Describe(FanEpisodeState state) => state switch {
		FanEpisodeState.LocalExcitement => "local excitement",
		FanEpisodeState.RegionalFavorite => "a regional teen favorite",
		FanEpisodeState.NationalSensation => "a national teen sensation",
		FanEpisodeState.CulturalPhenomenon => "a cultural phenomenon",
		FanEpisodeState.Cooldown => "cooling off",
		_ => "no particular following",
	};

	/// <summary>
	/// Where the camera went in one appearance (§10.2 member spotlight): the singer usually, but stage presence and youth
	/// pull the lens, and a keyed per-appearance draw lets the quiet drummer catch it. No beauty score; nothing private.
	/// </summary>
	public static List<(Musician Member, float Weight)> CameraShares(SimulatedArtist artist, string key, int year) {
		var list = new List<(Musician, float)>();
		var active = artist?.members?.Where(m => m != null && m.isActive && !string.IsNullOrEmpty(m.personId)).ToList();
		if (active == null || active.Count == 0) return list;
		float total = 0f;
		foreach (Musician m in active) {
			int age = m.birthYear > 0 ? year - m.birthYear : 25;
			float w = 0.4f + m.stagePresence + (m.isLeadVocalist ? 0.7f : 0f) + (age <= 24 ? 0.25f : age >= 32 ? -0.2f : 0f)
				+ 0.9f * MediaAtmosphere.Unit("camera|" + key + "|" + m.personId);
			w = Math.Max(0.05f, w);
			list.Add((m, w));
			total += w;
		}
		for (int i = 0; i < list.Count; i++) list[i] = (list[i].Item1, list[i].Item2 / total);
		return list;
	}

	/// <summary>The member the fans are writing about, if one stands clearly ahead.</summary>
	public static string Face(MediaFanState s) {
		if (s == null || s.Spotlight.Count < 2) return null;
		var top = s.Spotlight.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal).Take(2).ToList();
		return top[0].Value >= 0.08f && top[0].Value >= 1.25f * top[1].Value && s.Faces.TryGetValue(top[0].Key, out string name) ? name : null;
	}
}
