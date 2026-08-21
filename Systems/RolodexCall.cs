using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// THE LIVE CALL
//
// RolodexCall is the scene: it owns the transcript, the stage, and the options on offer. It does
// no simulation writing of its own -- when a resolution lands it hands off to PlayerDesk, which
// owns every write into rapport, the payola ledger, the advocacy service and the books.
//
// Shape of a call:
//   Dialing    -> you place it and it costs minutes. It can fail to connect, for reasons that are
//                 real: he is not on shift, he is on air, the switchboard is screening a label
//                 nobody has heard of.
//   Open       -> Opening beat, passive reads, situation read. You pick an approach.
//   Pushback   -> he raises ONE grounded objection. You press it, answer it with an instinct-gated
//                 counter (available only when the fact behind it is true), bluff it, or drop it.
//   Resolved   -> the roll resolves, the effect is applied, the aftermath and hook play out.
// ============================================================================================

/// <summary>Why a call did not connect. Every one of these is grounded in a real fact.</summary>
public enum ConnectFailure { None, OffShift, OnAir, Gatekeeper, NoAnswer, LineBusy }

/// <summary>The answers you can give to a pushback. Availability is gated on BOTH an instinct
/// score (you have to be the kind of person who would think of it) and the underlying fact being
/// true (otherwise it is offered as an explicit bluff, or not at all).</summary>
public enum CallCounter {
	None,
	PressIt,            // just insist. Always available, always slightly worse than a real answer.
	BackOff,            // withdraw the approach. No roll, no cost beyond the minutes already gone.
	EarChorus,          // ProductionRough: the drums are rough, the chorus is not
	EarConcede,         // ProductionRough: agree with him, then reframe -- costs nothing, earns respect
	StreetScene,        // NoLocalAudience: the region really is moving toward this
	StreetBluff,        // NoLocalAudience: it is NOT moving. A bluff, and he may call it.
	StreetRival,        // any: the station across town is looking at it (bluff unless it is fitting)
	SuitNumbers,        // NoSalesSupport / UnknownArtist: real units, quoted back at him
	SuitLateNight,      // FormatShutOut / ManagerHeat: stop asking for daytime, ask for the graveyard
	SuitUnderwrite,     // ManagerHeat / NoSalesSupport: put money behind it so he can defend it
	FixerSweeten,       // WhatsInItForMe / ManagerHeat: there is cash in the sleeve
	CallInFavor,        // any: you owe me one
}

/// <summary>One button the player can press at the current stage.</summary>
public sealed class CallOption {
	public string label;
	public string subLabel;              // cost / consequence, shown small
	public ExecutiveVoice voice;
	public RolodexApproach approach = RolodexApproach.HangUp;
	public CallCounter counter = CallCounter.None;
	public bool isBluff;
	public bool enabled = true;
	public string disabledReason;
	public int minutes;
	public float cash;
}

/// <summary>
/// One phone call, from dialling to hanging up. Held live on PlayerDesk so it survives a UI
/// refresh; discarded when the player hangs up or the day ends.
/// </summary>
public sealed class RolodexCall {
	public RolodexEntry entry;
	public RolodexCallContext ctx;
	public CallStage stage = CallStage.Dialing;
	public ConnectFailure failure;
	public readonly List<CallLine> transcript = new();

	// Live approach being negotiated (set at Open, resolved at Pushback).
	public RolodexApproach pendingApproach = RolodexApproach.HangUp;
	public object pendingPayload;
	public Objection objection = Objection.None;
	public float baseChance;
	public float chanceModifier;
	public int minutesSpent;
	public bool counterUsed;
	public bool lastSucceeded;

	// The record under discussion. Changing it restarts the situation read.
	public string recordId;

	public void Say(RolodexSceneBeat beat, string text, string speaker = null,
			ExecutiveVoice voice = ExecutiveVoice.None, bool isPlayer = false) {
		if (string.IsNullOrEmpty(text)) return;
		transcript.Add(new CallLine { beat = beat, text = text, speaker = speaker, voice = voice, isPlayer = isPlayer });
	}

	/// <summary>The chance the pending approach lands, after every counter and objection modifier.
	/// Clamped so nothing is ever certain and nothing is ever hopeless.</summary>
	public float EffectiveChance => Mathf.Clamp(baseChance + chanceModifier, 0.03f, 0.95f);
}

// --------------------------------------------------------------------------------------------
// SHIFTS -- what makes the clock matter
// --------------------------------------------------------------------------------------------

public static class RolodexShifts {
	/// <summary>This jock's on-air shift. Deterministic on (archetype, djId) so it is the same jock
	/// every time you call him, across a save and load, with no extra state to persist. Personality
	/// jocks skew late (that is where the format lets a personality exist); Company Men hold the
	/// drive-time slots the station actually sells.</summary>
	public static Daypart ShiftOf(Deejay dj) {
		if (dj == null) return Daypart.Midday;
		int h = StableHash(dj.djId ?? "");
		return dj.archetype switch {
			DJArchetype.Personality => (h % 3) switch { 0 => Daypart.Evening, 1 => Daypart.Overnight, _ => Daypart.Afternoon },
			DJArchetype.Tastemaker  => (h % 3) switch { 0 => Daypart.Evening, 1 => Daypart.Evening, _ => Daypart.Overnight },
			DJArchetype.Hustler     => (h % 3) switch { 0 => Daypart.Afternoon, 1 => Daypart.Midday, _ => Daypart.Evening },
			DJArchetype.CompanyMan  => (h % 3) switch { 0 => Daypart.Morning, 1 => Daypart.Midday, _ => Daypart.Afternoon },
			DJArchetype.Regional    => (h % 3) switch { 0 => Daypart.Morning, 1 => Daypart.Afternoon, _ => Daypart.Midday },
			_ => Daypart.Midday,
		};
	}

	private static int StableHash(string s) {
		unchecked {
			int h = 17;
			foreach (char c in s) h = h * 31 + c;
			return Math.Abs(h);
		}
	}

	public static string Label(Daypart part) => part switch {
		Daypart.Morning   => "morning drive",
		Daypart.Midday    => "middays",
		Daypart.Afternoon => "afternoon drive",
		Daypart.Evening   => "evenings",
		Daypart.Overnight => "the graveyard shift",
		_ => "middays",
	};

	/// <summary>The hours he is reachable AT THE STATION -- on shift, or in the couple of hours either
	/// side of it when he is prepping or winding down. A 9-to-6 office day with three overtime hours
	/// can reach a morning man easily and a graveyard jock only by working late.</summary>
	public static (int From, int To) ReachableWindow(Daypart part) => part switch {
		Daypart.Morning   => (9, 11),
		Daypart.Midday    => (9, 16),
		Daypart.Afternoon => (13, 19),
		Daypart.Evening   => (16, 21),
		Daypart.Overnight => (17, 21),
		_ => (9, 17),
	};

	public static bool ReachableAt(Daypart part, int hour) {
		(int from, int to) = ReachableWindow(part);
		return hour >= from && hour <= to;
	}

	/// <summary>Plain-English advice for the card: when to try him.</summary>
	public static string WindowAdvice(Daypart part) {
		(int from, int to) = ReachableWindow(part);
		return $"Works {Label(part)}. Reachable roughly {Clock(from)}-{Clock(to)}.";
	}

	private static string Clock(int hour24) {
		int h = hour24 % 12; if (h == 0) h = 12;
		return $"{h}{(hour24 >= 12 ? "pm" : "am")}";
	}
}
