using System.Collections.Generic;
using Godot;

/// <summary>How a piece of advocacy was won. Audit tag only -- the candidacy meeting reads the boost,
/// not the method -- but it is what the card history and any later scandal attribution key off.</summary>
public enum AdvocacyMethod {
	PersonalPitch,
	FavorCalledIn,
	AdvertisingBuy,
	RivalPressure,
	// Promo mechanic directive §7.2: a reporting dealer's counter numbers, not the DJ's own
	// discretionary taste -- excluded from PersonalPicksAt below exactly like AdvertisingBuy.
	DealerReport,
	// Promo mechanic directive §8: a jock who MC'd a hop and watched the room react in person --
	// the biggest legal advocacy in the game, and the DJ's own discretionary pick every bit as
	// much as a won phone pitch, so it belongs in PersonalPicksAt alongside PersonalPitch.
	RecordHop,
}

/// <summary>
/// A bounded, expiring commitment by one station to give ONE record a real look. This is the answer to
/// "did he actually put it in rotation?": he did not -- he agreed to carry it into the next playlist
/// meeting, which the sim still runs in full. The boost is multiplied into
/// <c>StationNetwork.Candidacy</c> alongside the payola term, so format, sales support, freshness, the
/// chart guard, and the DJ's own ear all still get their vote. The player opens the door; the market
/// decides whether the record stays in the room.
/// </summary>
public sealed class StationAdvocacy {
	public string stationId;
	public string recordId;
	public string labelId;
	public string sourceDjId;
	public int createdWeek;
	public int expiresWeek;     // inclusive: live while currentWeek <= expiresWeek
	public float candidacyBoost;
	public AdvocacyMethod method;

	/// <summary>Past its weeks. The boost stops applying, but the row is kept as a WATCH so the desk can
	/// still report the record being dropped later -- the outcome is the point, and it usually lands
	/// after the argument has stopped counting.</summary>
	public bool expired;
	/// <summary>Spin tier this station had the record at when the desk last looked. The diff against the
	/// live tier is what produces "WSUV added it" / "WSUV dropped it" in the log.</summary>
	public SpinTier lastSeenTier = SpinTier.None;
	/// <summary>True once the station has actually spun it at least once. An advocacy that expires having
	/// never been played is the honest "you opened the door and the market said no".</summary>
	public bool everPlayed;
}

/// <summary>
/// The live set of player-won station advocacies. Player-only, exactly like <see cref="PayolaLedger"/>'s
/// arrangements: an AI-only headless run never creates one, so the lookup returns 0 and the candidacy
/// term is neutral -- the base simulation is untouched.
/// </summary>
public sealed class StationAdvocacyService {
	private readonly List<StationAdvocacy> active = new();

	public IReadOnlyList<StationAdvocacy> Active => active;

	/// <summary>Grant advocacy for one (record, station). A second grant on the same pair does not stack
	/// into a runaway: it takes the stronger boost and the later expiry, so re-pitching the same record
	/// refreshes the commitment rather than multiplying it.</summary>
	public void Grant(string recordId, string stationId, string labelId, string djId,
			float boost, int week, int durationWeeks, AdvocacyMethod method) {
		if (string.IsNullOrEmpty(recordId) || string.IsNullOrEmpty(stationId)) return;
		// expiresWeek is INCLUSIVE, so a 3-week grant covers weeks W, W+1, W+2 -- hence the -1. Without
		// it a "3 weeks" promise displayed and behaved as four.
		int expires = week + Mathf.Max(1, durationWeeks) - 1;
		StationAdvocacy existing = active.Find(a =>
			a.recordId == recordId && a.stationId == stationId && a.labelId == labelId);
		if (existing != null) {
			existing.candidacyBoost = Mathf.Max(existing.candidacyBoost, boost);
			existing.expiresWeek = Mathf.Max(existing.expiresWeek, expires);
			existing.expired = false;
			existing.method = method;
			return;
		}
		active.Add(new StationAdvocacy {
			recordId = recordId, stationId = stationId, labelId = labelId, sourceDjId = djId,
			createdWeek = week, expiresWeek = expires,
			candidacyBoost = boost, method = method,
		});
	}

	/// <summary>Candidacy multiplier contribution for one (record, station). Matches the shape of
	/// <c>PayolaLedger.ActivePayola</c> so <c>StationNetwork</c> can consume it the same way.</summary>
	public float ActiveAdvocacy(string recordId, string stationId) {
		float best = 0f;
		foreach (StationAdvocacy a in active)
			if (!a.expired && a.recordId == recordId && a.stationId == stationId && a.candidacyBoost > best)
				best = a.candidacyBoost;
		return best;
	}

	/// <summary>Mark what has run out. Expired rows are KEPT as watches (so a later drop still gets
	/// reported) and are only forgotten once the station is no longer spinning the record.</summary>
	public void ExpireThrough(int week) {
		foreach (StationAdvocacy a in active)
			if (!a.expired && a.expiresWeek < week) a.expired = true;
	}

	/// <summary>Forget a watch entirely -- called once its record is off the air and the outcome has
	/// been reported, so the list does not grow without bound.</summary>
	public void Forget(StationAdvocacy a) => active.Remove(a);

	/// <summary>Everything this station is holding for you, live or being watched, newest first.</summary>
	public List<StationAdvocacy> ForStation(string stationId) {
		var list = new List<StationAdvocacy>();
		foreach (StationAdvocacy a in active)
			if (a.stationId == stationId) list.Add(a);
		list.Sort((x, y) => y.createdWeek.CompareTo(x.createdWeek));
		return list;
	}

	/// <summary>Records this station's DJ has PERSONALLY committed to and is still standing behind: a
	/// won pitch or a called-in favour. A paid advertising spot is deliberately excluded -- money buys
	/// a hearing in the ranking, it does not buy the man's own discretionary picks. Returns null when
	/// there is nothing, so the meeting's hot path allocates nothing on an AI-only run.</summary>
	public IReadOnlyList<string> PersonalPicksAt(string stationId) {
		List<string> picks = null;
		foreach (StationAdvocacy a in active) {
			if (a.expired || a.stationId != stationId) continue;
			if (a.method is not (AdvocacyMethod.PersonalPitch or AdvocacyMethod.FavorCalledIn or AdvocacyMethod.RecordHop)) continue;
			(picks ??= new List<string>()).Add(a.recordId);
		}
		return picks;
	}

	/// <summary>The live row for one (record, station), or null. Used by the call to tell the player
	/// what is already outstanding instead of just greying the button out.</summary>
	public StationAdvocacy Find(string recordId, string stationId) =>
		active.Find(a => a.recordId == recordId && a.stationId == stationId);

	public void Clear() => active.Clear();

	public void Restore(IEnumerable<StationAdvocacy> saved) {
		active.Clear();
		if (saved != null) active.AddRange(saved);
	}
}

/// <summary>
/// Player-earned reporter-station state that a load would otherwise destroy.
///
/// The reporter panel is rebuilt from the station seed on load, which throws away every
/// <see cref="StationRuntime"/> -- by design, because AI playlists are re-derived within a week and
/// nothing is lost. But two things on that runtime are NOT re-derivable: the rapport the player
/// cultivated by hand, and any of the player's own records a DJ has put into rotation. Both are the
/// direct product of player actions and both silently vanished on every save/load before this.
/// </summary>
public sealed class StationPlayerStateSaveData {
	public string StationId { get; set; }
	public float  Rapport   { get; set; }
	/// <summary>recordId -> spin tier ordinal, for the PLAYER's records only.</summary>
	public System.Collections.Generic.Dictionary<string, int> PlayerSpins { get; set; } = new();
	/// <summary>recordId -> weeks already served in this station's rotation (drives burn).</summary>
	public System.Collections.Generic.Dictionary<string, int> PlayerSpinWeeks { get; set; } = new();
}

/// <summary>Flat save record for one live <see cref="StationAdvocacy"/>.</summary>
public sealed class StationAdvocacySaveData {
	public string StationId  { get; set; }
	public string RecordId   { get; set; }
	public string LabelId    { get; set; }
	public string SourceDjId { get; set; }
	public int   CreatedWeek { get; set; }
	public int   ExpiresWeek { get; set; }
	public float CandidacyBoost { get; set; }
	public int   MethodOrdinal  { get; set; }
	public bool  Expired        { get; set; }
	public int   LastSeenTier   { get; set; }
	public bool  EverPlayed     { get; set; }

	public static StationAdvocacySaveData From(StationAdvocacy a) => new() {
		StationId = a.stationId, RecordId = a.recordId, LabelId = a.labelId, SourceDjId = a.sourceDjId,
		CreatedWeek = a.createdWeek, ExpiresWeek = a.expiresWeek,
		CandidacyBoost = a.candidacyBoost, MethodOrdinal = (int)a.method,
		Expired = a.expired, LastSeenTier = (int)a.lastSeenTier, EverPlayed = a.everPlayed,
	};

	public StationAdvocacy ToAdvocacy() => new() {
		stationId = StationId, recordId = RecordId, labelId = LabelId, sourceDjId = SourceDjId,
		createdWeek = CreatedWeek, expiresWeek = ExpiresWeek,
		candidacyBoost = CandidacyBoost,
		// Was clamped to (0, 3), silently corrupting a saved DealerReport (4) into RivalPressure (3) on
		// every load -- fixed to check membership instead of hard-coding the enum's old top ordinal,
		// so a later addition (RecordHop, directive §8) doesn't repeat the bug a third time.
		method = System.Enum.IsDefined(typeof(AdvocacyMethod), MethodOrdinal) ? (AdvocacyMethod)MethodOrdinal : AdvocacyMethod.PersonalPitch,
		expired = Expired, lastSeenTier = (SpinTier)System.Math.Clamp(LastSeenTier, 0, 3), everPlayed = EverPlayed,
	};
}
