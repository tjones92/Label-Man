using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// THE ROLODEX -- discovery, calls, and the sim writes a call is allowed to make.
//
// The scene layer (RolodexScene.cs / RolodexCall.cs) owns the conversation; this file owns every
// write. The division matters: a fragment can only ever say something the context already knows,
// and only the methods here touch rapport, the payola ledger, the advocacy service, or cash.
// ============================================================================================
public partial class PlayerDesk : Node {

	// ── Timing ───────────────────────────────────────────────────────────────────────────────
	public const int DialMinutes = 5;                 // placing the call and getting past the switchboard
	public const int WorkThePhonesMinMinutes = 20;
	public const int WorkThePhonesMaxMinutes = 40;
	public const int PersonalPitchMinMinutes = 20;
	public const int PersonalPitchMaxMinutes = 35;
	public const int AskAFavorMinMinutes = 10;
	public const int AskAFavorMaxMinutes = 20;
	public const int IntroductionMinutes = 15;
	public const int CounterMinutes = 5;              // pressing the point costs a little more of the day
	// Directive §3.3: how long a soft appointment (OfferToBringIt) holds before it lapses unfulfilled.
	public const int AppointmentExpiryWeeks = 4;

	// ── Advocacy sizing ──────────────────────────────────────────────────────────────────────
	// What a won argument is actually worth at the next playlist meeting. These are candidacy
	// multipliers minus one (see StationNetwork ADVOCACY_CAP = 0.9), so 0.20 means "this record
	// scores 20% higher in his meeting than it otherwise would" -- a real edge, never a guarantee.
	private const float PitchAdvocacyBase = 0.14f;
	private const float PitchAdvocacyInfluenceBonus = 0.12f;
	private const int   PitchAdvocacyWeeks = 3;
	// Weeks of "I'll defend it" behind a record the jock has ALREADY put on the air himself. Shorter
	// than the meeting version because the hard part is done -- the record is an incumbent now.
	private const int   PitchSpinNowWeeks = 2;
	// djAutonomy at or above which a DJ can simply put the record on, instead of taking it to a
	// playlist meeting. FullService/UndergroundFM sit at 0.85, pre-Boss Top40/RnB at ~0.5, and the
	// Boss Radio conversion drives a station to 0.10 -- so this line is the personality era itself.
	private const float DirectSpinAutonomy = 0.45f;
	private const float FavorAdvocacy = 0.34f;
	private const int   FavorAdvocacyWeeks = 4;
	private const int   RecordMemoryEvalWeeks = 4;
	private const int   RecordMemoryGoodUnitsThreshold = 300;
	private const int   RecordMemoryBadUnitsThreshold = 40;
	private const float RecordMemoryGoodRapportBonus = 0.05f;
	private const float RecordMemoryBadRapportPenalty = 0.06f;

	// ── Call-attempt pressure ────────────────────────────────────────────────────────────────
	// How many times you have gone to the phones today. The industry stops taking your call after
	// you have burned the good hours on it, which is what stops the whole Rolodex being a click-loop.
	private int callAttemptsToday;
	private GameDate callAttemptsDate = GameDate.StartDate;
	public int CallAttemptsToday { get { RollAttemptDay(); return callAttemptsToday; } }

	// Per-DJ pressure for the day, so a man who does not pick up cannot be spam-redialled until he does,
	// and a man you have already had a real conversation with is done with you until tomorrow. Transient
	// (not saved) -- the exploit it closes is a within-a-day click-loop, and a reload mid-day is a fair reset.
	private const int MaxDialAttemptsPerDjPerDay = 3;   // he stops answering after you have burned three tries
	private readonly Dictionary<string, int> djDialsToday = new(StringComparer.Ordinal);
	private readonly HashSet<string> djReachedToday = new(StringComparer.Ordinal);

	private void RollAttemptDay() {
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		if (!(today == callAttemptsDate)) {
			callAttemptsDate = today; callAttemptsToday = 0;
			djDialsToday.Clear(); djReachedToday.Clear();
		}
	}

	/// <summary>The live call, if one is open. Held here (not in the UI) so it survives a panel refresh.</summary>
	public RolodexCall ActiveCall { get; private set; }

	// ========================================================================================
	// DISCOVERY -- working the phones
	// ========================================================================================

	/// <summary>
	/// Spend part of the day cold-calling stations looking for someone who will talk to you. This is
	/// NOT guaranteed to produce a contact. You are a nobody with a nobody's label, and the outcomes
	/// are the real ones: nobody picks up, a secretary takes a message, you get a name but not the
	/// man, or -- if you are good, or lucky, or calling at the right hour -- you actually get through.
	/// STREET raises both the odds and the quality of who you land on.
	/// </summary>
	public bool WorkThePhones(out string message) {
		if (!RequireHome(out message)) return false;
		if (Label == null) { message = "You don't have a label yet."; return false; }
		if (IsGameOver) { message = "The label has folded -- load a save to keep playing."; return false; }
		if (TimeManager.Instance?.IsDayOver == true) { message = "It's too late to be making calls. Come back tomorrow."; return false; }

		var chart = ChartManager.Instance;
		if (chart == null) { message = "No market data."; return false; }

		var knownStationIds = new HashSet<string>(rolodex.Select(e => e.stationId), StringComparer.Ordinal);
		var candidates = chart.ReporterStationsInRegion(Label.homeRegion)
			.Where(s => !knownStationIds.Contains(s.stationId)
				&& !string.IsNullOrEmpty(s.leadDjId)
				&& chart.GetDeejay(s.leadDjId) != null)
			.ToList();

		if (candidates.Count == 0) {
			message = "You've already got a lead on every reporter station in your region. Expand to other markets first.";
			return false;
		}

		int minutes = WorkThePhonesMinMinutes + (int)GD.RandRange(0, WorkThePhonesMaxMinutes - WorkThePhonesMinMinutes);
		if (TimeManager.Instance?.CanAffordMinutes(minutes) != true) {
			message = "There isn't enough left of the day to sit on the phone.";
			return false;
		}
		SpendMinutes(minutes);

		RollAttemptDay();
		callAttemptsToday++;
		int hour = TimeManager.Instance?.CurrentHour ?? 12;
		int street = InstinctProfile.TheStreet;
		string date = TimeManager.Instance?.CurrentDate.ToShortString() ?? "?";

		// Odds of getting anywhere at all. Built from things that are true:
		//  - STREET: knowing who to ask for is most of this job.
		//  - The hour: the industry answers the phone mid-morning and again after lunch. Late in the
		//    day everyone worth reaching is on air or gone.
		//  - Diminishing returns: the fourth round of calls in one day is not the first.
		//  - The book you already have: people call back a label that other people take calls from.
		float hourTerm = hour switch {
			<= 9  => -0.05f,          // switchboard is barely staffed
			<= 11 =>  0.12f,          // the good window
			<= 13 => -0.04f,          // lunch
			<= 15 =>  0.06f,
			<= 17 => -0.02f,
			_     => -0.18f,          // everyone is on air or gone home
		};
		float fatigue = Mathf.Max(0f, (callAttemptsToday - 1) * 0.13f);
		float networkTerm = Mathf.Min(0.12f, rolodex.Count * 0.03f);
		// Directive §6.2: "a bonus to cold Rolodex connect rolls (the switchboard has seen the name)."
		float reach = Mathf.Clamp(0.22f + street * 0.09f + hourTerm + networkTerm - fatigue + TradeAdConnectBonus(), 0.05f, 0.88f);

		float roll = GD.Randf();

		// Total miss: the day is gone and you have nothing.
		if (roll > reach) {
			string[] misses = {
				"Four numbers, four secretaries, no names. One of them asks you to spell the label twice.",
				"You get a program director who says he'll call back. He won't.",
				"Busy signal, busy signal, and a man who says the jock you want quit in March.",
				"Somebody takes a message. You can hear him not writing it down.",
			};
			string miss = misses[(int)GD.RandRange(0, misses.Length - 1)];
			Note($"Worked the phones ({minutes} min) -- nothing. {miss}");
			message = miss;
			Changed?.Invoke();
			return true;
		}

		// Something landed. Who, and how far in, is the STREET read.
		RadioStation station = PickPhoneTarget(candidates, street);
		Deejay dj = chart.GetDeejay(station.leadDjId);
		string djName = SynthesizeDJName(dj, station);
		Daypart shift = RolodexShifts.ShiftOf(dj);

		// Getting THROUGH to the man is a second, harder gate than getting his name. An influential
		// jock is screened; being on shift when you call is most of the difference.
		float throughChance = Mathf.Clamp(
			0.28f + street * 0.07f
				+ (RolodexShifts.ReachableAt(shift, hour) ? 0.22f : -0.20f)
				- dj.influence * 0.25f,
			0.05f, 0.85f);
		bool gotThrough = GD.Randf() < throughChance;

		var entry = new RolodexEntry {
			djId = dj.djId, stationId = station.stationId,
			state = gotThrough ? DiscoveryState.Introduced : DiscoveryState.HeardOf,
			displayName = djName, portraitKey = dj.archetype.ToString(),
			// You only learn his hours by actually reaching him. A name off somebody comes with no
			// hours -- and the card says so, so the discovery toast must not leak them either.
			shiftKnown = gotThrough,
		};
		entry.log.Add($"{date} — " + (gotThrough
			? $"Got him on the line: {djName}, {station.callsign}."
			: $"Got a name off somebody: {djName} at {station.callsign}. Haven't reached him yet."));
		rolodex.Add(entry);

		Note($"Worked the phones ({minutes} min): {(gotThrough ? "reached" : "heard of")} " +
			$"{djName} — {station.callsign} ({station.format}, {station.cityName}).");
		message = gotThrough
			? $"You got through. {djName} at {station.callsign} picked up his own phone. {RolodexShifts.WindowAdvice(shift)}"
			: $"A name, nothing more: {djName} at {station.callsign}. You don't know his hours yet.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>Pick a reporter station to cold-call, weighted by DJ influence when STREET is high.</summary>
	private static RadioStation PickPhoneTarget(List<RadioStation> candidates, int street) {
		if (candidates.Count == 1) return candidates[0];
		if (street <= 2) return candidates[(int)GD.RandRange(0, candidates.Count - 1)];
		float weightPow = (street - 2) * 0.5f;
		float[] weights = candidates.Select(s => {
			Deejay dj = ChartManager.Instance?.GetDeejay(s.leadDjId);
			return Mathf.Pow(dj?.influence ?? 0.5f, weightPow);
		}).ToArray();
		float total = weights.Sum();
		float roll = GD.Randf() * total;
		for (int i = 0; i < candidates.Count; i++) { roll -= weights[i]; if (roll <= 0f) return candidates[i]; }
		return candidates[^1];
	}

	/// <summary>Synthesize a DJ radio name at first discovery. Draws from the NameGenerator act stream
	/// (player action only; never called from the weekly sim, so byte-identity is preserved).</summary>
	private static string SynthesizeDJName(Deejay dj, RadioStation station) {
		Genre genreHint = station.format switch {
			StationFormat.RnB           => Genre.RnB,
			StationFormat.Country       => Genre.Country,
			StationFormat.Gospel        => Genre.Gospel,
			StationFormat.Jazz          => Genre.Jazz,
			StationFormat.UndergroundFM => Genre.Psychedelic,
			_                           => Genre.TraditionalPop
		};
		(string first, string last) = NameGenerator.Instance?.GeneratePersonName(isMale: true, genre: genreHint)
			?? ("James", "Williams");
		return dj.archetype switch {
			DJArchetype.Personality => $"{PickPersonalityNick()} {last}",
			DJArchetype.Hustler     => $"{PickHustlerNick()} {last}",
			DJArchetype.Regional    => GD.Randf() < 0.35f ? $"Country {last}" : $"{first} {last}",
			_                       => $"{first} {last}"
		};
	}

	private static string PickPersonalityNick() {
		string[] nicks = { "Wolfman", "Daddy", "Cool Papa", "Mad", "The Baron",
			"Mellow", "Sweet Daddy", "Big", "Hot", "King", "Duke", "Fast" };
		return nicks[(int)GD.RandRange(0, nicks.Length - 1)];
	}

	private static string PickHustlerNick() {
		string[] nicks = { "Slick", "Fast Eddie", "Smooth", "Crazy", "Wild", "Lucky", "Easy" };
		return nicks[(int)GD.RandRange(0, nicks.Length - 1)];
	}

	// ========================================================================================
	// CONTEXT -- the one place a call is allowed to learn anything
	// ========================================================================================

	/// <summary>Gather every real fact the scene may reference. Called once when the line connects and
	/// again whenever the record under discussion changes.</summary>
	public RolodexCallContext BuildCallContext(RolodexEntry entry, string recordId) {
		var chart = ChartManager.Instance;
		var c = new RolodexCallContext {
			entry = entry,
			dj = chart?.GetDeejay(entry.djId),
			station = chart?.GetRadioStation(entry.stationId),
			playerLabel = Label,
			instincts = InstinctProfile,
			year = TimeManager.Instance?.CurrentDate.year ?? 1960,
			week = chart?.GetCurrentChartWeek() ?? 0,
			hour = TimeManager.Instance?.CurrentHour ?? 12,
			labelCash = Label?.cashReserves ?? 0f,
			theyOweYou = entry.theyOweThem,
			youOweThem = entry.youOweThem,
			payolaBurned = entry.payolaBurned,
			professionallyBurned = entry.professionallyBurned,
		};
		c.region = chart?.GetRegionById(c.station?.regionId ?? Label?.homeRegion ?? "");
		c.rapport = c.station?.rt?.Rapport(Label?.labelId ?? "") ?? 0f;
		c.tier = RolodexEntry.EffectiveTier(entry, c.rapport);

		if (c.dj != null) {
			c.djTaste = c.dj.taste; c.djGreed = c.dj.greed; c.djInfluence = c.dj.influence;
			c.djEgo = c.dj.ego; c.djSuspicion = c.dj.suspicion;
			c.shift = RolodexShifts.ShiftOf(c.dj);
		}
		if (c.station != null) {
			c.stationAutonomy = c.station.djAutonomy;
			// "Manager pressure" is not a new stat: it is the inverse of the autonomy the meeting
			// already scores with, plus whatever regulatory heat the jock is personally carrying.
			c.managerPressureHigh = c.station.djAutonomy < 0.40f || c.djSuspicion > 0.55f;
		}

		c.record = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId);
		c.baseRecord = c.record?.baseRecord;
		if (c.baseRecord != null) {
			c.recordHook = c.baseRecord.hookStrength;
			c.recordProduction = c.baseRecord.productionQuality;
			c.recordOriginality = c.baseRecord.originality;
			c.recordQuality = (c.recordHook + c.recordProduction + c.recordOriginality) / 3f;
			c.salesSupport = ChartSimulator.GetSalesSupportRatio(c.record);
			c.unitsTotal = c.record.totalUnitsSold;
			c.unitsThisWeek = c.record.unitsThisWeek;
			c.djGenreAffinity = c.dj?.GenreAffinity(c.baseRecord.primaryGenre) ?? 1f;
			c.isServiced = IsServiced(c.baseRecord.recordId, entry.stationId);
			c.servicingConviction = ServicingConviction(c.baseRecord.recordId, entry.stationId);
			TradeOutcome pick = ActiveTradeOutcome(c.baseRecord.recordId);
			c.hasTradePick = pick != TradeOutcome.Nothing;
			c.tradePickLabel = pick switch {
				TradeOutcome.Spotlight => "Cash Box put it in the Spotlight column. Go on, look it up.",
				TradeOutcome.FourStar => "The trades called it a Best Bet. That's not nothing.",
				TradeOutcome.TwoLineMention => "It got a mention in the trades, for what that's worth.",
				_ => "",
			};
			c.tradePickWeight = pick switch {
				TradeOutcome.Spotlight => 0.32f,
				TradeOutcome.FourStar => 0.20f,
				TradeOutcome.TwoLineMention => 0.10f,
				_ => 0f,
			};
			c.tradeAdCommercialBonus = TradeAdConnectBonus();
			c.artist = ArtistManager.Instance?.GetArtist(c.baseRecord.artistId);
			c.artistRecognition = c.artist != null
				? Mathf.Max(c.artist.publicRecognition, c.artist.momentum * 0.5f)
				: 0f;
			c.advocacyAlready = chart?.AdvocacyOn(c.baseRecord.recordId, entry.stationId) ?? 0f;

			// Directive §10: SuitSurvey's fact -- a real out-of-region spin (a DIFFERENT region's
			// reporter station, so this genuinely converts a win elsewhere into leverage here, not just
			// a bigger number from the same market) or a live breakout listing (§6.3).
			RadioStation bestOutOfRegion = null; SpinTier bestOutOfRegionTier = SpinTier.None;
			foreach (RadioStation other in chart?.AllReporterStations ?? Enumerable.Empty<RadioStation>()) {
				if (other.stationId == entry.stationId || other.regionId == c.station?.regionId) continue;
				SpinTier tier = chart.SpinTierOf(other.stationId, c.baseRecord.recordId);
				if (tier > bestOutOfRegionTier) { bestOutOfRegionTier = tier; bestOutOfRegion = other; }
			}
			if (bestOutOfRegion != null && bestOutOfRegionTier >= SpinTier.Mid) {
				c.hasOutOfRegionProof = true;
				c.outOfRegionProofLabel = $"It's {TierWord(bestOutOfRegionTier)} rotation on {bestOutOfRegion.callsign} in {bestOutOfRegion.cityName}.";
				c.outOfRegionProofWeight = bestOutOfRegionTier == SpinTier.High ? 0.30f : 0.18f;
			} else {
				string breakoutRegion = BreakoutRegionNames(c.baseRecord.recordId).FirstOrDefault();
				if (!string.IsNullOrEmpty(breakoutRegion)) {
					c.hasOutOfRegionProof = true;
					c.outOfRegionProofLabel = $"It's breaking out in {breakoutRegion}.";
					c.outOfRegionProofWeight = 0.24f;
				}
			}

			if (c.region != null) {
				Genre g = c.baseRecord.primaryGenre;
				c.regionalGenreAcceptance = c.region.currentGenreAcceptance != null
					&& c.region.currentGenreAcceptance.TryGetValue(g, out float acc) ? acc : 0.5f;
				c.regionalGenreMomentum = c.region.genreMomentum != null
					&& c.region.genreMomentum.TryGetValue(g, out float mom) ? mom : 0f;
				c.regionalAwareness = c.record.regionalData != null
					&& c.record.regionalData.TryGetValue(c.region.regionId, out RegionalRecordData rd) ? rd.awareness : 0f;
				c.formatAdmittance = chart?.FormatAdmittanceFor(c.baseRecord.primaryGenre, c.station, c.year) ?? 0f;
			}
		}
		return c;
	}

	// ========================================================================================
	// PLACING THE CALL
	// ========================================================================================

	/// <summary>
	/// Dial. Costs <see cref="DialMinutes"/> whether or not anybody picks up, and can fail for
	/// reasons that are all real: he is not on shift, he is live on air, or the switchboard is
	/// screening a label nobody has heard of. Getting a man on the phone is the first thing you earn.
	/// </summary>
	public RolodexCall PlaceCall(RolodexEntry entry, string recordId, out string message) {
		message = "";
		if (Label == null) { message = "You don't have a label yet."; return null; }
		if (entry == null) { message = "No one to call."; return null; }
		if (!RequireHome(out message)) return null;   // the phone is on the office desk
		if (TimeManager.Instance?.IsDayOver == true) { message = "The day's gone. Try him tomorrow."; return null; }
		if (TimeManager.Instance?.CanAffordMinutes(DialMinutes) != true) {
			message = "Not enough of the day left to start a call.";
			return null;
		}

		RolodexCallContext c = BuildCallContext(entry, recordId);
		if (c.dj == null || c.station == null) { message = "That line is dead -- he's not at the station any more."; return null; }

		// You get one real conversation with a man per day, and only so many attempts to reach him at all.
		// Without this, a failed call could be redialled on a loop until he happened to pick up.
		RollAttemptDay();
		if (djReachedToday.Contains(entry.djId)) {
			message = $"You've already had {entry.displayName} on the line today. Any more and you're pestering him -- try tomorrow.";
			return null;
		}
		djDialsToday.TryGetValue(entry.djId, out int dialsSoFar);
		if (dialsSoFar >= MaxDialAttemptsPerDjPerDay) {
			message = $"{entry.displayName} isn't picking up for you today. Leave it and try him tomorrow.";
			return null;
		}
		djDialsToday[entry.djId] = dialsSoFar + 1;

		SpendMinutes(DialMinutes);
		callAttemptsToday++;

		var call = new RolodexCall { entry = entry, ctx = c, recordId = recordId, minutesSpent = DialMinutes };
		ActiveCall = call;

		if (entry.payolaBurned && entry.professionallyBurned) {
			call.stage = CallStage.NotConnected;
			call.failure = ConnectFailure.Gatekeeper;
			call.Say(RolodexSceneBeat.Opening,
				"Somebody picks up, hears your name, and puts the receiver down on the desk. Nobody comes back to it.");
			message = "He won't take your calls.";
			Changed?.Invoke();
			return call;
		}

		ConnectFailure fail = RollConnection(c, entry);
		if (fail != ConnectFailure.None) {
			call.stage = CallStage.NotConnected;
			call.failure = fail;
			call.Say(RolodexSceneBeat.Opening, ConnectFailureLine(fail, c));
			// A miss you can learn from: the card now knows his shift.
			if (fail == ConnectFailure.OffShift || fail == ConnectFailure.OnAir) entry.shiftKnown = true;
			message = ConnectFailureSummary(fail, c);
			Note($"Called {entry.displayName} ({c.station.callsign}) -- {message}");
			Changed?.Invoke();
			return call;
		}

		// Connected. You have had your shot at him for the day -- no redialling him after this call ends.
		djReachedToday.Add(entry.djId);
		// Ratchet HeardOf -> Introduced: you have now actually spoken to him.
		if (entry.state == DiscoveryState.HeardOf) {
			entry.state = DiscoveryState.Introduced;
			entry.log.Insert(0, $"{Today()} — First time you actually got him on the phone.");
		}
		entry.shiftKnown = true;
		call.stage = CallStage.Open;
		OpenBeats(call);
		message = "";
		Changed?.Invoke();
		return call;
	}

	/// <summary>Did he pick up? Shift is the dominant term -- calling a graveyard jock before lunch is
	/// the single most common reason a call goes nowhere -- then influence (a big jock is screened),
	/// then the relationship, then how many calls you have already burned today.</summary>
	private ConnectFailure RollConnection(RolodexCallContext c, RolodexEntry entry) {
		int hour = c.hour;
		bool onShift = RolodexShifts.ReachableAt(c.shift, hour);
		(int from, int to) = RolodexShifts.ReachableWindow(c.shift);

		// Well outside his window: he is not in the building. This is close to a hard no, and it is
		// the thing the card tells you how to avoid.
		if (!onShift) {
			float slip = hour < from ? from - hour : hour - to;
			float stillReach = Mathf.Clamp(0.30f - slip * 0.10f + c.rapport * 0.35f, 0.02f, 0.35f);
			if (GD.Randf() > stillReach) return ConnectFailure.OffShift;
		}

		// On shift means he might literally be on the air right now.
		if (onShift && hour >= from + 1 && GD.Randf() < 0.14f) return ConnectFailure.OnAir;

		RollAttemptDay();
		float fatigue = Mathf.Max(0f, (callAttemptsToday - 1) * 0.07f);
		float chance = Mathf.Clamp(
			0.52f
			+ c.instincts.TheStreet * 0.05f
			+ c.rapport * 0.60f
			+ (entry.state == DiscoveryState.Trusted ? 0.15f : 0f)
			+ (c.theyOweYou ? 0.10f : 0f)
			- c.djInfluence * 0.22f
			- fatigue,
			0.08f, 0.94f);
		if (GD.Randf() < chance) return ConnectFailure.None;

		// Failed: pick the flavour that matches why. A cold relationship with a big jock reads as
		// screening; otherwise it is just a phone that nobody answers.
		if (c.rapport < 0.10f && c.djInfluence > 0.5f) return ConnectFailure.Gatekeeper;
		return GD.Randf() < 0.5f ? ConnectFailure.NoAnswer : ConnectFailure.LineBusy;
	}

	private static string ConnectFailureLine(ConnectFailure fail, RolodexCallContext c) => fail switch {
		ConnectFailure.OffShift =>
			$"A tired voice at the front desk. \"He does {RolodexShifts.Label(c.shift)}. He is not here and he will not be here.\" " +
			"You are given a number to try that is the same number you just dialled.",
		ConnectFailure.OnAir =>
			"You get the studio line and hear, faintly, the record he is playing behind the ring. " +
			"Somebody lifts the receiver, says \"he is ON,\" and puts it back down.",
		ConnectFailure.Gatekeeper =>
			"\"And this is regarding?\" You say the name of your label. There is a pause with an entire " +
			"industry in it. \"I'll see that he gets the message.\"",
		ConnectFailure.NoAnswer =>
			"It rings eleven times. You count them. Then you hang up before it can ring a twelfth.",
		ConnectFailure.LineBusy =>
			"Busy. You wait, dial again, and get the same flat tone. Somebody is having a better " +
			"conversation than the one you wanted.",
		_ => "",
	};

	private static string ConnectFailureSummary(ConnectFailure fail, RolodexCallContext c) => fail switch {
		ConnectFailure.OffShift  => $"he's not in -- he works {RolodexShifts.Label(c.shift)}",
		ConnectFailure.OnAir     => "he's live on air",
		ConnectFailure.Gatekeeper=> "screened at the switchboard",
		ConnectFailure.NoAnswer  => "no answer",
		_                        => "line busy",
	};

	// ========================================================================================
	// BEATS 1-3: opening, passive reads, situation read
	// ========================================================================================

	private void OpenBeats(RolodexCall call) {
		RolodexCallContext c = call.ctx;

		call.Say(RolodexSceneBeat.Opening, RolodexFragments.Pick(RolodexSceneBeat.Opening, c)
			?? "\"Yeah, go ahead.\"", speaker: call.entry.displayName);

		foreach (CallLine read in PassiveReads(c)) call.transcript.Add(read);

		string situation = SituationRead(c);
		if (situation != null) call.Say(RolodexSceneBeat.SituationRead, situation);
	}

	/// <summary>
	/// The four voices, each reading the categories it is actually competent in. Tiered: a low score
	/// gives you nothing, a middling one gives you a hint, a high one gives you a sentence you can
	/// act on. The player never sees the raw stat -- he sees an interpretation, and the interpretation
	/// is only as good as the instinct that produced it.
	/// </summary>
	public List<CallLine> PassiveReads(RolodexCallContext c) {
		var lines = new List<CallLine>();
		void Read(ExecutiveVoice voice, int score, string hint, string clear, string deep) {
			InsightStrength s = score >= 5 ? InsightStrength.DeepRead
				: score >= 4 ? InsightStrength.ClearRead
				: score >= 3 ? InsightStrength.Hint
				: InsightStrength.None;
			string text = s switch {
				InsightStrength.DeepRead  => deep ?? clear ?? hint,
				InsightStrength.ClearRead => clear ?? hint,
				InsightStrength.Hint      => hint,
				_ => null,
			};
			if (text != null)
				lines.Add(new CallLine { beat = RolodexSceneBeat.PassiveRead, voice = voice, text = text });
		}

		// THE EAR -- can he hear, and does he actually mean what he is about to say about the record?
		Read(ExecutiveVoice.Ear, c.instincts.TheEar,
			hint: c.djTaste > 0.6f ? "He listens. Whatever he says about the record, he will have heard it first."
				: c.djTaste < 0.35f ? "He is not really listening to records. He is listening to the room."
				: "Ordinary ears. He will go with whatever everyone else is going with.",
			clear: c.HasRecord
				? (c.recordProduction < 0.38f && c.recordHook > 0.60f
					? "The record has a problem and it is not the problem he is going to name. The drums are rough. The chorus is not."
					: c.recordHook < 0.40f
					? "There is nothing in this to grab onto. He will feel that even if he cannot say why."
					: "The record can survive an honest listen. That is not nothing.")
				: null,
			deep: c.HasRecord && c.djGenreAffinity > 1.15f
				? $"He has a soft spot for this kind of thing. He will not admit it, but the objection he raises will not be about the music."
				: c.HasRecord && c.djGenreAffinity < 0.85f
				? "He does not like this sound and he never has. Anything he says about sales is cover."
				: null);

		// THE STREET -- is the city actually where he says it is?
		Read(ExecutiveVoice.Street, c.instincts.TheStreet,
			hint: c.regionalGenreAcceptance > 0.55f ? "This town is open to this kind of record."
				: c.regionalGenreAcceptance < 0.35f ? "This is not a town that has asked for this."
				: "The city is neither for nor against it.",
			clear: c.regionalGenreMomentum > 0.08f
				? "Whatever the station thinks, the city is moving toward this sound. The numbers are behind the street."
				: c.regionalGenreMomentum < -0.05f
				? "This sound is on its way out here. He is not wrong, he is just early to say so."
				: "Nothing is moving here in either direction. This is a flat market for it.",
			deep: c.HasRecord && c.regionalAwareness > 0.12f
				? "People here have already heard of it. Not many. But it is not a cold start any more."
				: $"He works {RolodexShifts.Label(c.shift)}, which tells you exactly which audience he can hand you.");

		// THE SUIT -- what is this contact worth, what does the institution allow, what will it cost?
		Read(ExecutiveVoice.Suit, c.instincts.TheSuit,
			hint: c.djInfluence > 0.65f ? "He carries real weight in this market."
				: c.djInfluence < 0.35f ? "Small fish. A foot in the door, not a door."
				: "Medium pull. A spin here matters but will not move the needle far.",
			clear: c.HasRecord && c.formatAdmittance < 0.08f
				? "Do not waste the call on this record. This station's format does not admit it, and no favour changes a format."
				: c.managerPressureHigh
				? "He does not decide much. There is somebody upstairs, and that somebody reads the sales sheets."
				: "He has room to move here if he wants to. The decision really is his.",
			deep: c.HasRecord && c.salesSupport < 0.35f && c.unitsTotal > 100
				? "The record peaked and is sliding. He will see that on the same sheet you do."
				: c.HasRecord && c.unitsTotal > 200
				? $"You have {c.unitsTotal:N0} copies out. That is a number you can say out loud."
				: c.HasRecord && c.unitsTotal < 50
				? "You have no numbers. Every argument you make has to be about the music or about him."
				: null);

		// THE FIXER -- what is in it for him, and what would it cost you if it went wrong?
		Read(ExecutiveVoice.Fixer, c.instincts.TheFixer,
			hint: c.djGreed > 0.55f ? "There is cash in the sleeve and he already knows it."
				: c.djGreed < 0.25f ? "Not the type. Offer him money and you will not get a second call."
				: "Open to an arrangement, but he will not be the one to bring it up.",
			clear: c.djSuspicion > 0.50f
				? "He is hot right now. Somebody has been through the logs. Whatever you do, do not do it with money."
				: c.djGreed > 0.55f
				? "He does not want lunch money. He wants enough that saying yes feels like a decision, not a habit."
				: "The money channel is open but narrow. It buys a spin, not a friend.",
			deep: c.djEgo > 0.6f
				? "The thing he actually wants is to be right in public before anyone else was. That is cheaper than cash and worth more to him."
				: c.rapport > 0.3f
				? "He likes you, which is a thing you can spend exactly once before it stops being true."
				: null);

		return lines;
	}

	/// <summary>Beat 3: the one-sentence read of the business situation on the table right now.</summary>
	private static string SituationRead(RolodexCallContext c) {
		if (!c.HasRecord) return "You have nothing out to talk about. This is a courtesy call and you both know it.";
		if (c.advocacyAlready > 0.01f)
			return $"He is already carrying \"{c.baseRecord.title}\" into his meetings. Asking twice is how you spend goodwill for nothing.";
		if (c.formatAdmittance < 0.08f)
			return $"{c.station.callsign} is a {c.station.format} station. \"{c.baseRecord.title}\" is not a {c.station.format} record and nothing said on this call will change that.";
		if (c.formatAdmittance < 0.30f)
			return $"The format barely admits this. A spin here would be an exception he has to justify.";
		return $"\"{c.baseRecord.title}\" fits what {c.station.callsign} plays. Whether he cares is a different question.";
	}

	private string Today() => TimeManager.Instance?.CurrentDate.ToShortString() ?? "?";

	/// <summary>Directive §3.3: a soft appointment (OfferToBringIt) nobody kept. Checked daily and
	/// self-clearing, so it never needs its own week cursor -- once the flag is cleared it can't refire.</summary>
	private void ExpireStaleAppointments() {
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		foreach (RolodexEntry entry in rolodex) {
			if (string.IsNullOrEmpty(entry.appointmentRecordId) || week <= entry.appointmentExpiresWeek) continue;
			ApplyRapport(entry, -0.04f, floorAtZero: true);
			entry.log.Insert(0, $"{Today()} — Never brought him the copy you promised. He noticed.");
			entry.appointmentRecordId = "";
			entry.appointmentExpiresWeek = 0;
		}
	}

	// Surfaced on the Rolodex card once the live weekly drop chance clears this -- "worth a look
	// before it happens" without turning every low-rotation record into a false alarm.
	private const float SlidingDropChanceWarningBar = 0.10f;

	/// <summary>Directive §10: "a station whose spin tier is sliding should be visible on the Rolodex
	/// card BEFORE it drops, so 'drive back to Cleveland this week or lose the market' is a decision the
	/// player gets to make and can lose." Reads the exact same weekly roll ChartManager already applies
	/// (ChartSimulator.GetStationDropChance/IsStationDropCandidate) rather than a second, hand-tuned
	/// early-warning number -- if the read here says it's safe, the sim's own roll agrees.
	/// RegionalRecordData.stationsDropped is a one-way latch (directive §10, RegionalRecordData.cs:30-39):
	/// once true this always reads false, because there is nothing left to warn about.</summary>
	public bool IsSlidingTowardDrop(string recordId, string stationId) {
		RadioStation station = ChartManager.Instance?.GetRadioStation(stationId);
		if (station == null || string.IsNullOrEmpty(station.regionId)) return false;
		RecordRuntimeData rec = FindReleasedRecord(recordId);
		if (rec?.baseRecord == null || rec.regionalData == null
			|| !rec.regionalData.TryGetValue(station.regionId, out RegionalRecordData rd)) return false;
		if (!ChartSimulator.IsStationDropCandidate(rd)) return false;
		float chance = ChartSimulator.GetStationDropChance(ChartSimulator.GetSalesSupportRatio(rec), rec.weeksSincePeakUnits);
		return chance >= SlidingDropChanceWarningBar;
	}
}
