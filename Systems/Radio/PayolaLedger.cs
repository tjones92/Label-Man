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
	public string labelId, stationId, djId, regionId;
	public PayolaMethod method;
	public ScandalSeverity severity;
	public float financialPenalty;
	public float reputationDamage;   // to the LABEL
	public string description;
}

/// <summary>
/// The payola ledger (design doc d). PLAYER-FACING: the player places Cash/CutIn/IndiePromoter
/// arrangements; AI labels do not use it, so in a headless audit there are no actions and this is
/// entirely inert -- ActivePayola returns 0, candidacy is unchanged, the economy is untouched. The
/// risk is the mechanic: the regulatory-heat arc (RadioEra) makes the three methods trade off across
/// the decade, and a bust destroys cultivated relationships and can ban a DJ.
/// </summary>
public sealed class PayolaLedger {
	private readonly Dictionary<string, PayolaAction> actions = new(StringComparer.Ordinal);
	private readonly Dictionary<(string rec, string stn), float> boostCache = new();
	private readonly Dictionary<string, IndiePromoter> promoters = new(StringComparer.Ordinal);
	private readonly StationNetwork network;
	private readonly RandomNumberGenerator rng = new();

	public readonly List<ScandalEvent> pendingScandals = new();

	private const float CASH_UNIT_COST = 500f;             // $ per intensity unit at zero regulatory heat
	private const float CUTIN_ROYALTY_TO_INTENSITY = 4f;   // 0.2 royalty share -> ~0.8 base intensity
	private const float BASE_DETECTION = 0.005f;           // per-arrangement weekly baseline

	public PayolaLedger(StationNetwork network, ulong seed) {
		this.network = network;
		rng.Seed = seed;
	}

	public void RegisterPromoter(IndiePromoter p) { if (p?.promoterId != null) promoters[p.promoterId] = p; }

	/// <summary>Read by candidacy (StationNetwork.ActivePayolaLookup). Summed intensity for this pair, 0..~1+.</summary>
	public float ActivePayola(string recordId, string stationId) =>
		boostCache.TryGetValue((recordId, stationId), out float v) ? v : 0f;

	// ---- placing arrangements (player actions) ----
	public PayolaAction PlaceCash(string recordId, string labelId, string stationId, float budget, int week, int year, int month) {
		RadioStation s = network.GetStation(stationId);
		if (s == null) return null;
		Deejay dj = network.GetDeejay(s.leadDjId);
		float heat = RadioEra.RegulatoryHeat(year, month);
		float receptiveness = Mathf.Clamp((dj?.greed ?? 0.3f) * s.payolaSusceptibility * 1.5f, 0f, 1f);
		float priceMult = 1f + heat * 1.5f;   // DJs demand a risk premium as heat rises
		float intensity = Mathf.Clamp(budget / (CASH_UNIT_COST * priceMult) * receptiveness, 0f, 1f);
		return Register(new PayolaAction {
			actionId = NewId(), recordId = recordId, labelId = labelId, method = PayolaMethod.Cash,
			targetStationId = stationId, intensity = intensity, weeklyDecay = 0.35f,   // cash fades fast
			startedWeek = week, expiresWeek = week + 3
		});
	}

	public PayolaAction PlaceCutIn(string recordId, string labelId, string stationId, float royaltyShare, int week, int year, int month) {
		RadioStation s = network.GetStation(stationId);
		if (s == null) return null;
		Deejay dj = network.GetDeejay(s.leadDjId);
		float egoAppeal = 0.5f + (dj?.ego ?? 0.4f) * 0.5f;   // ego DJs like the co-writer credit
		float intensity = Mathf.Clamp(royaltyShare * CUTIN_ROYALTY_TO_INTENSITY * egoAppeal, 0f, 0.8f);
		return Register(new PayolaAction {
			actionId = NewId(), recordId = recordId, labelId = labelId, method = PayolaMethod.CutIn,
			targetStationId = stationId, intensity = intensity, weeklyDecay = 0.08f,   // sticky -- the DJ's invested
			cutInRoyaltyShare = royaltyShare, startedWeek = week, expiresWeek = week + 12
		});
	}

	public List<PayolaAction> PlaceIndiePromoter(string recordId, string labelId, string promoterId, int week, int year, int month) {
		if (!promoters.TryGetValue(promoterId, out IndiePromoter p)) return null;
		var placed = new List<PayolaAction>();
		foreach (string stationId in p.stationIds) {
			if (network.GetStation(stationId) == null) continue;
			float intensity = Mathf.Clamp(p.effectiveness * 0.7f, 0f, 0.9f);
			placed.Add(Register(new PayolaAction {
				actionId = NewId(), recordId = recordId, labelId = labelId, method = PayolaMethod.IndiePromoter,
				targetStationId = stationId, promoterId = promoterId, intensity = intensity, weeklyDecay = 0.15f,
				startedWeek = week, expiresWeek = week + 6
			}));
		}
		return placed;
	}

	private PayolaAction Register(PayolaAction a) { actions[a.actionId] = a; return a; }
	private string NewId() => $"pay-{actions.Count}-{rng.Randi()}";

	// ---- weekly tick: decay, expire, adjudicate scandal, rebuild the boost cache ----
	public void Tick(int week, int year, int month) {
		pendingScandals.Clear();
		if (actions.Count == 0) { boostCache.Clear(); return; }   // inert fast path (no player actions)
		float heat = RadioEra.RegulatoryHeat(year, month);

		var expired = new List<string>();
		foreach (PayolaAction a in actions.Values) {
			a.intensity *= 1f - a.weeklyDecay;
			if (week >= a.expiresWeek || a.intensity < 0.02f) expired.Add(a.actionId);
		}
		foreach (string id in expired) actions.Remove(id);

		AdjudicateDetection(year, month, heat);
		RebuildBoostCache();

		foreach (PayolaAction a in actions.Values) {
			RadioStation s = network.GetStation(a.targetStationId);
			if (s?.rt != null) s.rt.scandalHeat = Mathf.Min(1f, s.rt.scandalHeat + a.intensity * 0.02f);
		}
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
		foreach (PayolaAction a in actions.Values.Where(a => !a.exposed).ToList()) {
			RadioStation s = network.GetStation(a.targetStationId);
			if (s == null) continue;
			float methodExposure = a.method switch {
				PayolaMethod.Cash => 1.0f,
				PayolaMethod.CutIn => 0.25f,            // laundered -- looks like legitimate co-writing
				PayolaMethod.IndiePromoter => 0.6f,
				_ => 1f
			};
			float promoterDiscretion = a.method == PayolaMethod.IndiePromoter &&
				promoters.TryGetValue(a.promoterId, out IndiePromoter p) ? 1f - p.discretion : 1f;
			float weeklyChance = BASE_DETECTION
				* (0.5f + heat * 2f)              // the era arc
				* methodExposure * promoterDiscretion
				* (0.5f + s.integrityRisk)        // a reckless station gets everyone caught
				* (0.5f + a.intensity)            // bigger payments are more visible
				* (1f + (s.rt?.scandalHeat ?? 0f));
			if (rng.Randf() < weeklyChance) RaiseScandal(a, s, heat);
		}
	}

	private void RaiseScandal(PayolaAction a, RadioStation s, float heat) {
		a.exposed = true;   // stops delivering boost immediately
		float severityRoll = heat + a.intensity * 0.3f;
		if (a.method == PayolaMethod.IndiePromoter && promoters.TryGetValue(a.promoterId, out IndiePromoter p))
			severityRoll += p.mobConnection * 0.5f;

		ScandalSeverity severity =
			severityRoll > 1.1f ? ScandalSeverity.RegionPoisoned :
			severityRoll > 0.8f ? ScandalSeverity.DJBan :
			severityRoll > 0.5f ? ScandalSeverity.Fine : ScandalSeverity.Warning;

		Deejay dj = network.GetDeejay(s.leadDjId);
		// TEETH: destroy the cultivated relationship the player invested in.
		s.rt?.labelRapport.Remove(a.labelId);
		if (dj != null) { dj.suspicion = Mathf.Min(1f, dj.suspicion + 0.5f); dj.labelRapport.Remove(a.labelId); }

		if (severity >= ScandalSeverity.DJBan) network.SackDeejay(s);          // the jock is banned
		if (severity == ScandalSeverity.RegionPoisoned)                        // scrutiny spreads region-wide
			foreach (RadioStation peer in network.ReportersInRegion(s.regionId))
				if (peer.rt != null) peer.rt.scandalHeat = Mathf.Min(1f, peer.rt.scandalHeat + 0.3f);

		pendingScandals.Add(new ScandalEvent {
			labelId = a.labelId, stationId = s.stationId, djId = s.leadDjId, regionId = s.regionId,
			method = a.method, severity = severity,
			financialPenalty = severity switch { ScandalSeverity.Fine => 5000f, ScandalSeverity.DJBan => 15000f, ScandalSeverity.RegionPoisoned => 40000f, _ => 0f },
			reputationDamage = 0.1f + severityRoll * 0.2f,
			description = $"{a.method} payola on {s.callsign} exposed ({severity})."
		});
	}
}
