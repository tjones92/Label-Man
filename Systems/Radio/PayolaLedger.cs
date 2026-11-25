using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum PayolaMethod { Cash, CutIn, IndiePromoter }
public enum ScandalSeverity { Warning, Fine, DJBan, RegionPoisoned }

/// <summary>
/// One active payola arrangement (design doc d). Cash/CutIn target a single station; IndiePromoter
/// targets a promoter's whole network. Arrangements decay and expire; they feed ONLY the candidacy
/// payolaBoost term -- never radioHeat, chart points, or demand. A payola'd record still has to sell,
/// or salesSupport collapses and it gets dropped anyway (payola can't save a dog).
/// </summary>
public sealed class PayolaAction {
	public string actionId;
	public string recordId;
	public string labelId;          // who's paying (the exposure surface for scandal)
	public PayolaMethod method;
	public string targetStationId;  // for Cash/CutIn
	public string promoterId;       // for IndiePromoter
	public float intensity;         // 0..1 current strength
	public float weeklyDecay;       // intensity fades unless renewed
	public int startedWeek;
	public int expiresWeek;
	public float cutInRoyaltyShare; // CutIn: permanent royalty fraction ceded on this record
	public float paid;              // dollars behind THIS arrangement (a network's fee is split across its stations); sizes the fine
	public bool exposed;            // flipped when a scandal implicates this action
}

public sealed class IndiePromoter {
	public string promoterId;
	public string name;
	public string[] stationIds;      // the network this promoter controls access to
	public float effectiveness;      // 0..1 boost delivered
	public float discretion;         // 0..1 inverse of detection risk he adds
	public float mobConnection;      // 0..1 raises severity if busted
	public float retainerWeekly;
	public float perRecordFee;
}

public sealed class ScandalEvent {
	public string labelId, stationId, djId, regionId, promoterId;
	public PayolaMethod method;
	public ScandalSeverity severity;
	public float financialPenalty;
	public float reputationDamage;   // to the LABEL
	public int networkStationsLost;  // IndiePromoter: the rest of his network that went down with this one
	public string description;
}

/// <summary>
/// The payola ledger (design doc d). PLAYER-FACING: the player places Cash/CutIn/IndiePromoter
/// arrangements; AI labels do not use it, so in a headless audit there are no actions and this is
/// entirely inert -- ActivePayola returns 0, candidacy is unchanged, the economy is untouched. The
/// risk is the mechanic: the regulatory-heat arc (RadioEra) makes the three methods trade off across
/// the decade, and a bust destroys cultivated relationships and can ban a DJ.
///
/// How the risk is shaped (inner-life directive §3.5 H5 pass, 2026-11-24; numbers measured by SimTools/PayolaMeasureProbe):
///  - A bust's fine is a lawyer's fee by severity plus a multiple of the money behind the arrangement, so a $25 promo man and
///    a $1,000 envelope are not fined the same. The old flat $5k/$15k/$40k was sized for a Major and ended a small label.
///  - Every arrangement leaves a paper trail on the LABEL (exposure). Cash leaves the most, a discreet promoter the least,
///    and the trail is what gets you caught next time: exposure raises detection on everything you place and the severity
///    of what is found. It cools over months. Some things that are not arrangements add to it too (a payola jock's name on
///    your song, his "consulting fees" on your books), through <see cref="AddExposure"/>.
///  - A station's scandal heat cools as well (it never did, so one bust marked a station for the rest of the decade).
///  - A promoter's network is one deal: when one of his jocks is caught, the rest of the network goes dark with him.
/// </summary>
public sealed class PayolaLedger {
	private readonly Dictionary<string, PayolaAction> actions = new(StringComparer.Ordinal);
	private readonly Dictionary<(string rec, string stn), float> boostCache = new();
	private readonly Dictionary<string, IndiePromoter> promoters = new(StringComparer.Ordinal);
	private readonly Dictionary<string, float> labelExposure = new(StringComparer.Ordinal);
	private readonly Dictionary<string, float> stationHeat = new(StringComparer.Ordinal);
	private readonly StationNetwork network;
	private readonly PcgRandom rng = new();

	public readonly List<ScandalEvent> pendingScandals = new();

	// $ per intensity unit at zero regulatory heat. Was 500, which priced an envelope at ~3x a legal ad buy per boost-week
	// (PayolaMeasureProbe): nobody would bribe. At 150 an average jock's envelope is at par with a Full Sponsorship in the 1960
	// crackdown (so it is not worth the risk) and ~20% under it from 1964; a greedy jock is half that.
	public const float CASH_UNIT_COST = 150f;
	private const float CUTIN_ROYALTY_TO_INTENSITY = 4f;   // 0.2 royalty share -> ~0.8 base intensity
	public const float BASE_DETECTION = 0.005f;           // per-arrangement weekly baseline

	// ---- the paper trail (label exposure) ----
	/// <summary>Exposure every arrangement leaves just for existing, times its trail weight.</summary>
	public const float EXPOSURE_PER_ARRANGEMENT = 0.05f;
	/// <summary>Exposure per dollar behind it, times its trail weight: $1,000 in cash is a trail of 0.5.</summary>
	public const float EXPOSURE_PER_DOLLAR = 0.0005f;
	/// <summary>Weekly cooling: a trail loses about half in 23 weeks and four-fifths in a year.</summary>
	public const float EXPOSURE_WEEKLY_KEEP = 0.97f;
	public const float EXPOSURE_CAP = 1.5f;
	/// <summary>Detection on every arrangement scales by (1 + this x exposure); severity rises by SEVERITY_PER_EXPOSURE x exposure (exposure capped at 1 there).</summary>
	public const float DETECTION_PER_EXPOSURE = 1.5f;
	public const float SEVERITY_PER_EXPOSURE = 0.25f;
	/// <summary>Station scandal heat: what each live arrangement adds a week (x intensity), and how much survives a week.</summary>
	public const float STATION_HEAT_PER_WEEK = 0.02f;
	public const float STATION_HEAT_WEEKLY_KEEP = 0.95f;
	public const float MOB_SEVERITY = 0.3f;

	public PayolaLedger(StationNetwork network, ulong seed) {
		this.network = network;
		rng.Seed = seed;
	}

	public void RegisterPromoter(IndiePromoter p) { if (p?.promoterId != null) promoters[p.promoterId] = p; }

	/// <summary>Read by candidacy (StationNetwork.ActivePayolaLookup). Summed intensity for this pair, 0..~1+.</summary>
	public float ActivePayola(string recordId, string stationId) =>
		boostCache.TryGetValue((recordId, stationId), out float v) ? v : 0f;

	/// <summary>The paper trail pointing at this label, 0..<see cref="EXPOSURE_CAP"/>.</summary>
	public float Exposure(string labelId) => labelId != null && labelExposure.TryGetValue(labelId, out float e) ? e : 0f;

	/// <summary>Add to a label's paper trail from something that is not an arrangement (a payola jock's name on a song, his fees
	/// on the books). Player-only like everything here.</summary>
	public void AddExposure(string labelId, float amount) {
		if (labelId == null || amount <= 0f) return;
		labelExposure[labelId] = Mathf.Min(EXPOSURE_CAP, Exposure(labelId) + amount);
	}

	public float StationHeat(string stationId) => stationId != null && stationHeat.TryGetValue(stationId, out float h) ? h : 0f;

	// ---- the rules, as pure functions (the ledger and SimTools/PayolaMeasureProbe share them) ----

	/// <summary>How much of the trail an arrangement leaves: cash in a jock's hand is all trail; a cut-in looks like songwriting;
	/// a promoter is one step removed, more so the more discreet he is.</summary>
	public static float TrailWeight(PayolaMethod method, float promoterDiscretion) => method switch {
		PayolaMethod.Cash => 1.0f,
		PayolaMethod.CutIn => 0.35f,
		PayolaMethod.IndiePromoter => 0.3f + 0.7f * (1f - Mathf.Clamp(promoterDiscretion, 0f, 1f)),
		_ => 1f,
	};

	public static float ExposureFor(PayolaMethod method, float promoterDiscretion, float paid) =>
		TrailWeight(method, promoterDiscretion) * (EXPOSURE_PER_ARRANGEMENT + EXPOSURE_PER_DOLLAR * Mathf.Max(0f, paid));

	public static float CashIntensity(float budget, float heat, float djGreed, float payolaSusceptibility) {
		float receptiveness = Mathf.Clamp(djGreed * payolaSusceptibility * 1.5f, 0f, 1f);
		float priceMult = 1f + heat * 1.5f;   // DJs demand a risk premium as heat rises
		return Mathf.Clamp(budget / (CASH_UNIT_COST * priceMult) * receptiveness, 0f, 1f);
	}

	public static float PromoterIntensity(float effectiveness) => Mathf.Clamp(effectiveness * 0.7f, 0f, 0.9f);

	public static float MethodExposure(PayolaMethod method) => method switch {
		PayolaMethod.Cash => 1.0f,
		PayolaMethod.CutIn => 0.25f,            // laundered -- looks like legitimate co-writing
		PayolaMethod.IndiePromoter => 0.6f,
		_ => 1f
	};

	public static float WeeklyDetectionChance(PayolaMethod method, float promoterDiscretion, float heat, float integrityRisk,
			float intensity, float stationScandalHeat, float labelExposure) =>
		BASE_DETECTION
		* (0.5f + heat * 2f)                                                  // the era arc
		* MethodExposure(method)
		* (method == PayolaMethod.IndiePromoter ? 1f - promoterDiscretion : 1f)
		* (0.5f + integrityRisk)                                              // a reckless station gets everyone caught
		* (0.5f + intensity)                                                  // bigger payments are more visible
		* (1f + stationScandalHeat)
		* (1f + DETECTION_PER_EXPOSURE * labelExposure);                       // the trail you've already left

	public static float SeverityRoll(float heat, float intensity, float mobConnection, float labelExposure) =>
		heat + intensity * 0.3f + mobConnection * MOB_SEVERITY + SEVERITY_PER_EXPOSURE * Mathf.Min(1f, labelExposure);

	public static ScandalSeverity SeverityOf(float roll) =>
		roll > 1.1f ? ScandalSeverity.RegionPoisoned :
		roll > 0.8f ? ScandalSeverity.DJBan :
		roll > 0.5f ? ScandalSeverity.Fine : ScandalSeverity.Warning;

	/// <summary>The lawyer by severity, plus a multiple of the money behind what was found (FTC consent order, a grand jury's
	/// attention, a region's stations told to clean house). Sized for a small label in 1960 dollars.</summary>
	public static float Penalty(ScandalSeverity severity, float paid) {
		paid = Mathf.Max(0f, paid);
		return severity switch {
			ScandalSeverity.Fine => 250f + 2f * paid,
			ScandalSeverity.DJBan => 500f + 4f * paid,
			ScandalSeverity.RegionPoisoned => 1000f + 6f * paid,
			_ => 0f,
		};
	}

	// ---- placing arrangements (player actions) ----
	public PayolaAction PlaceCash(string recordId, string labelId, string stationId, float budget, int week, int year, int month) {
		RadioStation s = network.GetStation(stationId);
		if (s == null) return null;
		Deejay dj = network.GetDeejay(s.leadDjId);
		float heat = RadioEra.RegulatoryHeat(year, month);
		float intensity = CashIntensity(budget, heat, dj?.greed ?? 0.3f, s.payolaSusceptibility);
		var a = Register(new PayolaAction {
			actionId = NewId(), recordId = recordId, labelId = labelId, method = PayolaMethod.Cash,
			targetStationId = stationId, intensity = intensity, weeklyDecay = 0.35f,   // cash fades fast
			startedWeek = week, expiresWeek = week + 3, paid = budget,
		});
		AddExposure(labelId, ExposureFor(PayolaMethod.Cash, 0f, budget));
		return a;
	}

	public PayolaAction PlaceCutIn(string recordId, string labelId, string stationId, float royaltyShare, int week, int year, int month) {
		RadioStation s = network.GetStation(stationId);
		if (s == null) return null;
		Deejay dj = network.GetDeejay(s.leadDjId);
		float egoAppeal = 0.5f + (dj?.ego ?? 0.4f) * 0.5f;   // ego DJs like the co-writer credit
		float intensity = Mathf.Clamp(royaltyShare * CUTIN_ROYALTY_TO_INTENSITY * egoAppeal, 0f, 0.8f);
		var a = Register(new PayolaAction {
			actionId = NewId(), recordId = recordId, labelId = labelId, method = PayolaMethod.CutIn,
			targetStationId = stationId, intensity = intensity, weeklyDecay = 0.08f,   // sticky -- the DJ's invested
			cutInRoyaltyShare = royaltyShare, startedWeek = week, expiresWeek = week + 12
		});
		AddExposure(labelId, ExposureFor(PayolaMethod.CutIn, 0f, 0f));
		return a;
	}

	/// <summary>A promoter works his whole network. <paramref name="fee"/> is what the label paid him for it, split across the stations
	/// so a bust at one is fined on its share.</summary>
	public List<PayolaAction> PlaceIndiePromoter(string recordId, string labelId, string promoterId, int week, int year, int month,
			int durationWeeks = 6, float fee = 0f) {
		if (!promoters.TryGetValue(promoterId, out IndiePromoter p)) return null;
		var stations = p.stationIds.Where(id => network.GetStation(id) != null).ToList();
		var placed = new List<PayolaAction>();
		float share = stations.Count == 0 ? 0f : fee / stations.Count;
		foreach (string stationId in stations) {
			placed.Add(Register(new PayolaAction {
				actionId = NewId(), recordId = recordId, labelId = labelId, method = PayolaMethod.IndiePromoter,
				targetStationId = stationId, promoterId = promoterId, intensity = PromoterIntensity(p.effectiveness), weeklyDecay = 0.15f,
				startedWeek = week, expiresWeek = week + durationWeeks, paid = share,
			}));
			AddExposure(labelId, ExposureFor(PayolaMethod.IndiePromoter, p.discretion, share));
		}
		return placed;
	}

	private PayolaAction Register(PayolaAction a) { actions[a.actionId] = a; return a; }
	private string NewId() => $"pay-{actions.Count}-{rng.Randi()}";

	// ---- save/load: the arrangements, their promoters, the trails and the detection stream ----
	/// <summary>Everything a load needs to carry on exactly: the live arrangements, the promoters they name, the label trails and
	/// station heat, and the ledger's own rng state (so a scandal draw after a load is the draw it would have been). Null when
	/// nothing was ever placed.</summary>
	public PayolaLedgerSaveData Capture() {
		if (actions.Count == 0 && labelExposure.Count == 0 && stationHeat.Count == 0) return null;
		var used = new HashSet<string>(actions.Values.Select(a => a.promoterId).Where(id => id != null), StringComparer.Ordinal);
		return new PayolaLedgerSaveData {
			RngState = rng.State,
			Actions = actions.Values.OrderBy(a => a.actionId, StringComparer.Ordinal).Select(a => new PayolaActionSaveData {
				ActionId = a.actionId, RecordId = a.recordId, LabelId = a.labelId, Method = (int)a.method,
				TargetStationId = a.targetStationId, PromoterId = a.promoterId, Intensity = a.intensity, WeeklyDecay = a.weeklyDecay,
				StartedWeek = a.startedWeek, ExpiresWeek = a.expiresWeek, CutInRoyaltyShare = a.cutInRoyaltyShare, Paid = a.paid, Exposed = a.exposed,
			}).ToList(),
			// Only the promoters a live arrangement names: an expired project promo leaves nothing worth keeping.
			Promoters = promoters.Values.Where(p => used.Contains(p.promoterId)).OrderBy(p => p.promoterId, StringComparer.Ordinal)
				.Select(p => new IndiePromoterSaveData {
					PromoterId = p.promoterId, Name = p.name, StationIds = p.stationIds?.ToList() ?? new(),
					Effectiveness = p.effectiveness, Discretion = p.discretion, MobConnection = p.mobConnection,
					RetainerWeekly = p.retainerWeekly, PerRecordFee = p.perRecordFee,
				}).ToList(),
			LabelExposure = new Dictionary<string, float>(labelExposure),
			StationHeat = new Dictionary<string, float>(stationHeat),
		};
	}

	/// <summary>Put a captured ledger back on a freshly built one (the radio panel is rebuilt on load). Null = nothing placed.</summary>
	public void Restore(PayolaLedgerSaveData saved) {
		actions.Clear(); promoters.Clear(); boostCache.Clear(); pendingScandals.Clear(); labelExposure.Clear(); stationHeat.Clear();
		if (saved == null) return;
		foreach (IndiePromoterSaveData p in saved.Promoters ?? new())
			RegisterPromoter(new IndiePromoter {
				promoterId = p.PromoterId, name = p.Name, stationIds = (p.StationIds ?? new()).ToArray(),
				effectiveness = p.Effectiveness, discretion = p.Discretion, mobConnection = p.MobConnection,
				retainerWeekly = p.RetainerWeekly, perRecordFee = p.PerRecordFee,
			});
		foreach (PayolaActionSaveData a in saved.Actions ?? new()) {
			if (a?.ActionId == null) continue;
			actions[a.ActionId] = new PayolaAction {
				actionId = a.ActionId, recordId = a.RecordId, labelId = a.LabelId,
				method = Enum.IsDefined(typeof(PayolaMethod), a.Method) ? (PayolaMethod)a.Method : PayolaMethod.Cash,
				targetStationId = a.TargetStationId, promoterId = a.PromoterId, intensity = a.Intensity, weeklyDecay = a.WeeklyDecay,
				startedWeek = a.StartedWeek, expiresWeek = a.ExpiresWeek, cutInRoyaltyShare = a.CutInRoyaltyShare, paid = a.Paid, exposed = a.Exposed,
			};
		}
		foreach (var kv in saved.LabelExposure ?? new()) labelExposure[kv.Key] = kv.Value;
		foreach (var kv in saved.StationHeat ?? new()) {
			stationHeat[kv.Key] = kv.Value;
			RadioStation s = network.GetStation(kv.Key);
			if (s?.rt != null) s.rt.scandalHeat = kv.Value;
		}
		if (saved.RngState != 0UL) rng.State = saved.RngState;
		RebuildBoostCache();   // candidacy reads the boost the moment the load finishes, not a week later
	}

	/// <summary>Live arrangements, read-only (checks and the desk's own readouts).</summary>
	public IReadOnlyCollection<PayolaAction> Actions => actions.Values;

	// ---- weekly tick: cool, decay, expire, adjudicate scandal, rebuild the boost cache ----
	public void Tick(int week, int year, int month) {
		pendingScandals.Clear();
		if (actions.Count == 0 && labelExposure.Count == 0 && stationHeat.Count == 0) { boostCache.Clear(); return; }   // inert fast path (no player payola)
		float heat = RadioEra.RegulatoryHeat(year, month);

		Cool(labelExposure, EXPOSURE_WEEKLY_KEEP, 0.005f);
		Cool(stationHeat, STATION_HEAT_WEEKLY_KEEP, 0.005f);

		var expired = new List<string>();
		foreach (PayolaAction a in actions.Values) {
			a.intensity *= 1f - a.weeklyDecay;
			if (week >= a.expiresWeek || a.intensity < 0.02f) expired.Add(a.actionId);
		}
		foreach (string id in expired) actions.Remove(id);

		AdjudicateDetection(year, month, heat);
		RebuildBoostCache();

		foreach (PayolaAction a in actions.Values) {
			if (a.exposed || a.targetStationId == null) continue;
			AddStationHeat(a.targetStationId, a.intensity * STATION_HEAT_PER_WEEK);
		}
		foreach (var kv in stationHeat) {
			RadioStation s = network.GetStation(kv.Key);
			if (s?.rt != null) s.rt.scandalHeat = kv.Value;
		}
	}

	private void Cool(Dictionary<string, float> map, float keep, float floor) {
		if (map.Count == 0) return;
		foreach (string k in map.Keys.ToList()) {
			float v = map[k] * keep;
			if (v < floor) {
				map.Remove(k);
				if (ReferenceEquals(map, stationHeat) && network.GetStation(k)?.rt is StationRuntime rt) rt.scandalHeat = 0f;
			} else map[k] = v;
		}
	}

	private void AddStationHeat(string stationId, float amount) {
		if (stationId == null || amount <= 0f) return;
		stationHeat[stationId] = Mathf.Min(1f, StationHeat(stationId) + amount);
	}

	private void RebuildBoostCache() {
		boostCache.Clear();
		foreach (PayolaAction a in actions.Values) {
			if (a.exposed) continue;   // an exposed arrangement stops delivering spins
			var key = (a.recordId, a.targetStationId);
			boostCache[key] = boostCache.GetValueOrDefault(key) + a.intensity;
		}
	}

	// ---- scandal: detection + teeth ----
	private void AdjudicateDetection(int year, int month, float heat) {
		foreach (PayolaAction a in actions.Values.OrderBy(a => a.actionId, StringComparer.Ordinal).ToList()) {
			if (a.exposed) continue;   // a network that went down earlier this week is already exposed
			RadioStation s = network.GetStation(a.targetStationId);
			if (s == null) continue;
			float discretion = a.method == PayolaMethod.IndiePromoter && promoters.TryGetValue(a.promoterId ?? "", out IndiePromoter p) ? p.discretion : 0f;
			float weeklyChance = WeeklyDetectionChance(a.method, discretion, heat, s.integrityRisk, a.intensity, StationHeat(s.stationId), Exposure(a.labelId));
			if (rng.Randf() < weeklyChance) RaiseScandal(a, s, heat);
		}
	}

	private void RaiseScandal(PayolaAction a, RadioStation s, float heat) {
		a.exposed = true;   // stops delivering boost immediately
		IndiePromoter p = a.method == PayolaMethod.IndiePromoter && promoters.TryGetValue(a.promoterId ?? "", out IndiePromoter found) ? found : null;
		float exposure = Exposure(a.labelId);
		float severityRoll = SeverityRoll(heat, a.intensity, p?.mobConnection ?? 0f, exposure);
		ScandalSeverity severity = SeverityOf(severityRoll);

		Deejay dj = network.GetDeejay(s.leadDjId);
		// TEETH: destroy the cultivated relationship the player invested in.
		s.rt?.labelRapport.Remove(a.labelId);
		if (dj != null) { dj.suspicion = Mathf.Min(1f, dj.suspicion + 0.5f); dj.labelRapport.Remove(a.labelId); }

		if (severity >= ScandalSeverity.DJBan) network.SackDeejay(s);          // the jock is banned
		if (severity == ScandalSeverity.RegionPoisoned)                        // scrutiny spreads region-wide
			foreach (RadioStation peer in network.ReportersInRegion(s.regionId)) AddStationHeat(peer.stationId, 0.3f);
		AddStationHeat(s.stationId, 0.3f);

		// A promoter's network is one deal: when one of his jocks is caught, the others stop taking his calls this week. They
		// are not fined (nobody found them), but the boost is gone.
		int lost = 0;
		if (p != null)
			foreach (PayolaAction sib in actions.Values)
				if (!sib.exposed && sib.promoterId == a.promoterId && sib.labelId == a.labelId) { sib.exposed = true; lost++; }

		// Being caught is the biggest trail of all.
		AddExposure(a.labelId, 0.2f + 0.1f * (int)severity);

		pendingScandals.Add(new ScandalEvent {
			labelId = a.labelId, stationId = s.stationId, djId = s.leadDjId, regionId = s.regionId, promoterId = a.promoterId,
			method = a.method, severity = severity,
			financialPenalty = Penalty(severity, a.paid),
			reputationDamage = 0.1f + severityRoll * 0.2f,
			networkStationsLost = lost,
			description = $"{(p != null ? p.name + "'s " : "")}{a.method} payola on {s.callsign} exposed ({severity})."
				+ (lost > 0 ? $" {Plural.Of(lost, "more station")} in the network went quiet." : "")
		});
	}
}

public sealed class PayolaActionSaveData {
	public string ActionId { get; set; }
	public string RecordId { get; set; }
	public string LabelId { get; set; }
	public int Method { get; set; }
	public string TargetStationId { get; set; }
	public string PromoterId { get; set; }
	public float Intensity { get; set; }
	public float WeeklyDecay { get; set; }
	public int StartedWeek { get; set; }
	public int ExpiresWeek { get; set; }
	public float CutInRoyaltyShare { get; set; }
	public float Paid { get; set; }
	public bool Exposed { get; set; }
}

public sealed class IndiePromoterSaveData {
	public string PromoterId { get; set; }
	public string Name { get; set; }
	public List<string> StationIds { get; set; } = new();
	public float Effectiveness { get; set; }
	public float Discretion { get; set; }
	public float MobConnection { get; set; }
	public float RetainerWeekly { get; set; }
	public float PerRecordFee { get; set; }
}

/// <summary>The payola ledger's save. Player-only: AI labels never place anything, so it is null on every headless save.</summary>
public sealed class PayolaLedgerSaveData {
	public ulong RngState { get; set; }
	public List<PayolaActionSaveData> Actions { get; set; } = new();
	public List<IndiePromoterSaveData> Promoters { get; set; } = new();
	public Dictionary<string, float> LabelExposure { get; set; } = new();
	public Dictionary<string, float> StationHeat { get; set; } = new();
}
