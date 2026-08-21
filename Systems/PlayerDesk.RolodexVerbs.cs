using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// ROLODEX VERBS -- approach selection, pushback, counters, and resolution.
//
// The shape of every approach is the same: you pick a verb, he raises ONE objection drawn from a
// real condition, and then you either answer that specific objection (with an option your
// instincts let you see AND the facts let you make), press it anyway, bluff, or drop it. Only
// then does the roll happen.
//
// The counter is where the call stops being a button. Answering the objection he actually raised
// is worth roughly twice pressing the point, and the counters that are worth the most are the ones
// gated on a fact being TRUE -- the sim will not let you claim the city is moving toward your
// sound if the region's genre momentum says otherwise, except as a bluff he can call.
// ============================================================================================
public partial class PlayerDesk : Node {

	public enum AdBuyTier { Small, Medium, Large }
	public enum PayolaTier { Small, Medium, Large }

	public static float AdBuyCost(AdBuyTier tier) => tier switch {
		AdBuyTier.Small => 150f, AdBuyTier.Medium => 400f, AdBuyTier.Large => 800f, _ => 150f };
	public static string AdBuyTierName(AdBuyTier tier) => tier switch {
		AdBuyTier.Small => "Spot Buy", AdBuyTier.Medium => "Promo Package", AdBuyTier.Large => "Full Sponsorship", _ => "Buy" };
	private static float AdBuyRapportGain(AdBuyTier tier) => tier switch {
		AdBuyTier.Small => 0.02f, AdBuyTier.Medium => 0.035f, AdBuyTier.Large => 0.05f, _ => 0.02f };
	private static float AdBuyAdvocacy(AdBuyTier tier) => tier switch {
		AdBuyTier.Small => 0.08f, AdBuyTier.Medium => 0.15f, AdBuyTier.Large => 0.24f, _ => 0.08f };
	private static int AdBuyWeeks(AdBuyTier tier) => tier switch {
		AdBuyTier.Small => 2, AdBuyTier.Medium => 3, AdBuyTier.Large => 4, _ => 2 };

	public static float PayolaCost(PayolaTier tier) => tier switch {
		PayolaTier.Small => 200f, PayolaTier.Medium => 500f, PayolaTier.Large => 1000f, _ => 200f };
	public static string PayolaTierName(PayolaTier tier) => tier switch {
		PayolaTier.Small => "Envelope", PayolaTier.Medium => "Package", PayolaTier.Large => "Heavy Package", _ => "Envelope" };

	// FIXER minimum to even see the Payola verb -- pricing an envelope without reading greed and
	// suspicion first is just throwing money at a stranger.
	public const int PayolaMinFixer = 3;

	// ========================================================================================
	// BEAT 4: the approaches on offer
	// ========================================================================================

	/// <summary>What you can try, given who you are and what is actually true. An approach that the
	/// situation forbids is listed with the reason rather than hidden, so the player learns the rule.</summary>
	public List<CallOption> ApproachOptions(RolodexCall call) {
		RolodexCallContext c = call.ctx;
		ExecutiveInstinctProfile inst = c.instincts;
		var options = new List<CallOption>();

		if (c.HasRecord) {
			// Anyone can argue for their own record; only a real EAR gets to frame it as an insight. A
			// voice tag is a claim that your instincts surfaced the line -- tagging an option the player
			// would see regardless is a lie about their own character.
			var pitch = new CallOption {
				label = inst.TheEar >= 3
					? "Play him the chorus down the phone. Let him hear the structure."
					: "Pitch the record",
				subLabel = inst.TheEar >= 3
					? $"{PersonalPitchMinMinutes}-{PersonalPitchMaxMinutes} min · you know what in it is worth defending"
					: $"{PersonalPitchMinMinutes}-{PersonalPitchMaxMinutes} min · argue it on the music, as best you can",
				voice = inst.TheEar >= 3 ? ExecutiveVoice.Ear : ExecutiveVoice.None,
				approach = RolodexApproach.PersonalPitch,
				minutes = PersonalPitchMinMinutes,
			};
			if (c.professionallyBurned) { pitch.enabled = false; pitch.disabledReason = "He doesn't take your word any more."; }
			else if (c.advocacyAlready > 0.01f) { pitch.enabled = false; pitch.disabledReason = CarryingStatus(c); }
			options.Add(pitch);

			// Buying airtime is money, not insight -- it is on the table for everybody. What a SUIT adds
			// is knowing what it is worth, which is why only a SUIT gets the read and the voice tag.
			options.Add(new CallOption {
				label = "Offer to buy time around it",
				subLabel = inst.TheSuit >= 3
					? $"${AdBuyCost(AdBuyTier.Small):N0}-${AdBuyCost(AdBuyTier.Large):N0} · " +
					  (c.djInfluence > 0.6f ? "his reach makes this worth the money." : "small station. Do not overpay for it.")
					: $"${AdBuyCost(AdBuyTier.Small):N0}-${AdBuyCost(AdBuyTier.Large):N0} · cash for the spot. You could not say if it is a fair price.",
				voice = inst.TheSuit >= 3 ? ExecutiveVoice.Suit : ExecutiveVoice.None,
				approach = RolodexApproach.CommercialPitch,
				minutes = 10,
			});

			if (inst.TheStreet >= 3) {
				bool grounded = c.regionalGenreMomentum > 0.08f || c.regionalAwareness > 0.10f;
				options.Add(new CallOption {
					label = "Tell him the station across town is already asking",
					subLabel = grounded
						? "The scene really is moving. He can check, and it will hold up."
						: "You have nothing behind this. If he checks, he will find out.",
					voice = ExecutiveVoice.Street,
					approach = RolodexApproach.RivalPressure,
					isBluff = !grounded,
					minutes = 10,
				});
			}

			if (inst.TheFixer >= PayolaMinFixer) {
				var payola = new CallOption {
					label = "There is cash in the sleeve",
					subLabel = $"${PayolaCost(PayolaTier.Small):N0}-${PayolaCost(PayolaTier.Large):N0} · through the ledger, with everything that follows",
					voice = ExecutiveVoice.Fixer,
					approach = RolodexApproach.OfferPayola,
					minutes = 10,
				};
				if (c.payolaBurned) { payola.enabled = false; payola.disabledReason = "He won't touch your money again."; }
				options.Add(payola);
			}
		} else {
			options.Add(new CallOption {
				label = "Nothing to pitch", enabled = false,
				disabledReason = "You have no record in the market. Release something first.",
			});
		}

		if (c.theyOweYou) {
			options.Add(new CallOption {
				label = "\"You said you owed me one.\"",
				subLabel = $"{AskAFavorMinMinutes}-{AskAFavorMaxMinutes} min · guaranteed, and spent forever",
				approach = RolodexApproach.AskForFavor,
				minutes = AskAFavorMinMinutes,
			});
		}

		if (call.entry.state == DiscoveryState.Trusted) {
			options.Add(new CallOption {
				label = "Ask who else you should be talking to",
				subLabel = $"{IntroductionMinutes} min · a vouched-for name, not a cold call",
				approach = RolodexApproach.AskForIntroduction,
				minutes = IntroductionMinutes,
			});
		}

		options.Add(new CallOption { label = "Hang up", approach = RolodexApproach.HangUp });
		return options;
	}

	/// <summary>What he is already doing with this record, in plain words. Replaces the old
	/// "he's already carrying this one" dead end, which told the player nothing about whether the
	/// call they already made had worked.</summary>
	private string CarryingStatus(RolodexCallContext c) {
		var chart = ChartManager.Instance;
		if (chart == null || c.baseRecord == null) return "He's already carrying this one.";
		SpinTier tier = chart.SpinTierOf(c.entry.stationId, c.baseRecord.recordId);
		StationAdvocacy a = chart.Advocacy.Find(c.baseRecord.recordId, c.entry.stationId);
		int weeksLeft = a == null ? 0 : Mathf.Max(0, a.expiresWeek - chart.GetCurrentChartWeek() + 1);

		if (tier != SpinTier.None)
			return $"Already done. {c.station.callsign} has it in {TierWord(tier)} rotation right now — " +
				"asking again is how you spend goodwill for nothing.";
		return weeksLeft > 0
			? $"He's still arguing for it — {weeksLeft} more playlist meeting(s). It hasn't been picked up yet."
			: "He argued for it and the sheet went another way. Give it a week before you push again.";
	}

	// ========================================================================================
	// BEAT 5: he pushes back
	// ========================================================================================

	/// <summary>Commit to an approach. He answers with ONE objection, chosen as the most severe thing
	/// that is actually true about this record at this station right now, and the scene moves to the
	/// pushback stage where you get to answer it.</summary>
	public void ChooseApproach(RolodexCall call, RolodexApproach approach, object payload, out string message) {
		message = "";
		RolodexCallContext c = call.ctx;
		if (call.stage != CallStage.Open) { message = "Not at that point in the call."; return; }

		if (approach == RolodexApproach.HangUp) { EndCall(call); return; }

		// Defensive: the record or the man can go away between the panel drawing a button and the
		// player pressing it (a DJ can lose his chair to a payola bust on the week boundary).
		bool needsRecord = approach is RolodexApproach.PersonalPitch or RolodexApproach.CommercialPitch
			or RolodexApproach.OfferPayola or RolodexApproach.RivalPressure;
		if (c.station == null || (needsRecord && !c.HasRecord)) {
			call.Say(RolodexSceneBeat.Failure, "The line goes dead in the middle of your sentence.");
			call.stage = CallStage.Resolved;
			message = "The call fell apart.";
			Changed?.Invoke();
			return;
		}

		call.pendingApproach = approach;
		call.pendingPayload = payload;
		call.Say(RolodexSceneBeat.PlayerPitch, PlayerOpeningLine(approach, c, payload), isPlayer: true);

		// Favours and introductions are not negotiations -- you are cashing something that already
		// exists, or asking for a name. They resolve straight away.
		if (approach is RolodexApproach.AskForFavor or RolodexApproach.AskForIntroduction) {
			call.baseChance = 1f;
			Resolve(call, out message);
			return;
		}

		call.objection = PickObjection(c, approach);
		call.baseChance = BaseChance(approach, c, payload);

		string line = RolodexFragments.Pick(RolodexSceneBeat.Pushback, c, call.objection);
		call.Say(RolodexSceneBeat.Pushback, line ?? "\"I'll have to think about it.\"", speaker: call.entry.displayName);
		call.stage = CallStage.Pushback;
		Changed?.Invoke();
	}

	/// <summary>The objection he raises. Ordered by how badly the fact actually hurts you, so the thing
	/// he names is the thing most wrong with the pitch -- not a random flavour draw.</summary>
	private static Objection PickObjection(RolodexCallContext c, RolodexApproach approach) {
		if (c.professionallyBurned || (c.youOweThem && GD.Randf() < 0.5f)) return Objection.YouBurnedMeBefore;
		if (c.HasRecord && c.formatAdmittance < 0.08f)                     return Objection.FormatShutOut;
		if (c.managerPressureHigh)                                         return Objection.ManagerHeat;
		if (c.djGreed > 0.55f && c.rapport < 0.15f)                        return Objection.WhatsInItForMe;
		if (c.HasRecord && c.recordProduction < 0.38f)                     return Objection.ProductionRough;
		if (c.regionalGenreAcceptance < 0.38f)                             return Objection.NoLocalAudience;
		if (c.artistRecognition < 0.12f && c.unitsTotal < 200)             return Objection.UnknownArtist;
		if (c.HasRecord && (c.salesSupport < 0.35f || c.unitsTotal < 50))  return Objection.NoSalesSupport;
		return GD.Randf() < 0.5f ? Objection.PlaylistFull : Objection.UnknownArtist;
	}

	/// <summary>The chance before any counter. Every term is a real number from the context.</summary>
	private static float BaseChance(RolodexApproach approach, RolodexCallContext c, object payload) {
		float chance = approach switch {
			RolodexApproach.PersonalPitch =>
				0.22f
				+ (c.recordQuality - 0.5f) * 0.40f
				+ (c.djGenreAffinity - 1f) * 0.30f
				+ (c.djTaste - 0.5f) * 0.20f
				+ c.rapport * 0.30f
				+ c.instincts.TheEar * 0.045f,
			RolodexApproach.CommercialPitch =>
				0.42f
				+ c.rapport * 0.20f
				+ c.instincts.TheSuit * 0.035f
				+ (payload is AdBuyTier t ? (int)t * 0.09f : 0f)
				- (c.managerPressureHigh ? 0.08f : 0f),
			RolodexApproach.OfferPayola =>
				0.18f
				+ c.djGreed * 0.55f
				+ c.rapport * 0.15f
				+ c.instincts.TheFixer * 0.04f
				+ (payload is PayolaTier p ? (int)p * 0.10f : 0f)
				- c.djSuspicion * 0.40f
				- (c.managerPressureHigh ? 0.10f : 0f),
			RolodexApproach.RivalPressure =>
				0.16f
				+ Mathf.Max(0f, c.regionalGenreMomentum) * 1.20f
				+ c.regionalAwareness * 0.40f
				+ c.instincts.TheStreet * 0.04f
				- (c.dj != null && c.dj.archetype == DJArchetype.Regional ? 0.12f : 0f),
			_ => 0.5f,
		};
		// Format is not an argument you can win. It is the wall.
		if (c.HasRecord && c.formatAdmittance < 0.08f) chance *= 0.10f;
		else if (c.HasRecord && c.formatAdmittance < 0.30f) chance *= 0.65f;
		return Mathf.Clamp(chance, 0.02f, 0.92f);
	}

	private static string PlayerOpeningLine(RolodexApproach approach, RolodexCallContext c, object payload) => approach switch {
		RolodexApproach.PersonalPitch =>
			$"You tell him about \"{c.baseRecord?.title}\" — what it is, who made it, and why you picked up the phone.",
		RolodexApproach.CommercialPitch =>
			$"\"We're buying time in this market anyway. We'd rather buy it around a record we believe in.\"",
		RolodexApproach.OfferPayola =>
			"You let the sentence trail off in the place where the number goes. He does not fill the silence, so you do.",
		RolodexApproach.RivalPressure =>
			"\"I'll be straight with you — you're not the only station in this town I've called about it.\"",
		RolodexApproach.AskForFavor =>
			"\"You said you owed me one. I'm calling it.\"",
		RolodexApproach.AskForIntroduction =>
			"\"Who else in this market should I know? Somebody who'd actually take the call.\"",
		_ => "",
	};

	// ========================================================================================
	// BEAT 6: answering him
	// ========================================================================================

	/// <summary>
	/// The counters on the table, given the objection he raised, your instincts, and what is TRUE. The
	/// gate is deliberately two-part: an instinct score decides whether you would think of the answer,
	/// and a fact decides whether the answer is real. Where the instinct is there but the fact is not,
	/// the option is still offered -- flagged as a bluff, and he can call it.
	/// </summary>
	public List<CallOption> CounterOptions(RolodexCall call) {
		RolodexCallContext c = call.ctx;
		ExecutiveInstinctProfile inst = c.instincts;
		var opts = new List<CallOption>();

		void Offer(CallCounter counter, ExecutiveVoice voice, string label, string sub, bool bluff = false) =>
			opts.Add(new CallOption {
				label = label, subLabel = sub, voice = voice,
				counter = counter, isBluff = bluff, minutes = CounterMinutes,
			});

		switch (call.objection) {
			case Objection.ProductionRough:
				if (inst.TheEar >= 3 && c.recordHook > 0.55f)
					Offer(CallCounter.EarChorus, ExecutiveVoice.Ear,
						"\"The drums are rough. The chorus isn't. Listen to the chorus.\"",
						"True: the hook is genuinely strong under the noise.");
				if (inst.TheEar >= 4)
					Offer(CallCounter.EarConcede, ExecutiveVoice.Ear,
						"\"You're right. It's a cheap record. That's what it's supposed to sound like.\"",
						"Concede the point and reframe it. Costs you nothing but pride.");
				if (inst.TheEar >= 3 && c.recordHook <= 0.55f)
					Offer(CallCounter.EarChorus, ExecutiveVoice.Ear,
						"\"The drums are rough. The chorus isn't.\"",
						"There is no chorus to point at. He has ears.", bluff: true);
				break;

			case Objection.NoLocalAudience:
				if (inst.TheStreet >= 3 && c.regionalGenreMomentum > 0.08f)
					Offer(CallCounter.StreetScene, ExecutiveVoice.Street,
						"\"This town wanted soul records last year. Ask your own request line what changed.\"",
						$"True: this region is moving toward this sound right now.");
				else if (inst.TheStreet >= 3)
					Offer(CallCounter.StreetBluff, ExecutiveVoice.Street,
						"\"The city's turning. You just can't hear it from inside the building.\"",
						"It is not turning. He knows this market.", bluff: true);
				if (inst.TheSuit >= 3)
					Offer(CallCounter.SuitLateNight, ExecutiveVoice.Suit,
						"\"Then don't put it in daytime. Give me the graveyard and let the phones decide.\"",
						"Ask for less. A smaller yes is still a yes.");
				break;

			case Objection.UnknownArtist:
			case Objection.NoSalesSupport:
				if (inst.TheSuit >= 3 && c.unitsTotal > 200)
					Offer(CallCounter.SuitNumbers, ExecutiveVoice.Suit,
						$"\"{c.unitsTotal:N0} copies. That's not a rumour, that's a shipping ledger.\"",
						"True: you have real numbers to quote.");
				else if (inst.TheSuit >= 3)
					Offer(CallCounter.SuitNumbers, ExecutiveVoice.Suit,
						"\"The numbers are better than you think.\"",
						$"You have moved {c.unitsTotal:N0} copies. He can look this up.", bluff: true);
				if (inst.TheSuit >= 3 && c.labelCash >= AdBuyCost(AdBuyTier.Small))
					Offer(CallCounter.SuitUnderwrite, ExecutiveVoice.Suit,
						$"\"Then let me take the risk off you. We'll buy the time.\" (${AdBuyCost(AdBuyTier.Small):N0})",
						"Put your own money behind it so he does not have to defend it alone.");
				if (inst.TheStreet >= 3 && c.regionalGenreMomentum > 0.08f)
					Offer(CallCounter.StreetScene, ExecutiveVoice.Street,
						"\"Nobody had heard of them here last month either. Look at what the sound is doing.\"",
						"True: the genre is climbing in this region.");
				break;

			case Objection.ManagerHeat:
				if (inst.TheSuit >= 3)
					Offer(CallCounter.SuitLateNight, ExecutiveVoice.Suit,
						"\"Nobody upstairs is listening at midnight. Neither am I. Put it there.\"",
						"Ask for the slot he can give without asking permission.");
				if (inst.TheSuit >= 4 && c.labelCash >= AdBuyCost(AdBuyTier.Small))
					Offer(CallCounter.SuitUnderwrite, ExecutiveVoice.Suit,
						$"\"Then it's an advertiser's record, not yours.\" (${AdBuyCost(AdBuyTier.Small):N0})",
						"Buy him the paperwork he needs. Cash, not a bribe.");
				if (inst.TheFixer >= 4 && c.djSuspicion < 0.5f)
					Offer(CallCounter.FixerSweeten, ExecutiveVoice.Fixer,
						"\"Everybody upstairs has a price too. What's his?\"",
						"Dangerous. He is under watch and you are asking him to look sideways.");
				break;

			case Objection.WhatsInItForMe:
				if (inst.TheFixer >= 3)
					Offer(CallCounter.FixerSweeten, ExecutiveVoice.Fixer,
						"\"Enough that saying yes feels like a decision, not a habit.\"",
						"Name a real number. He has been waiting for you to.");
				if (inst.TheEar >= 4 && c.djEgo > 0.5f)
					Offer(CallCounter.EarConcede, ExecutiveVoice.Ear,
						"\"You get to be the man who played it first. That's the whole offer.\"",
						"Ego, not money. Cheaper, and he will remember it longer.");
				if (inst.TheSuit >= 3)
					Offer(CallCounter.SuitNumbers, ExecutiveVoice.Suit,
						"\"A hit out of your station is worth more to you than to me.\"",
						"Argue his interest, not yours.");
				break;

			case Objection.FormatShutOut:
				if (inst.TheSuit >= 3)
					Offer(CallCounter.SuitLateNight, ExecutiveVoice.Suit,
						"\"Then not the format. One spin, off the sheet, after midnight.\"",
						"Even this barely moves a wall. Format is the one thing talk does not fix.");
				break;

			case Objection.YouBurnedMeBefore:
				if (c.theyOweYou)
					Offer(CallCounter.CallInFavor, ExecutiveVoice.None,
						"\"And the time before that I was right. You said so yourself.\"",
						"Spends the favour he owes you to buy back your word.");
				if (inst.TheEar >= 4)
					Offer(CallCounter.EarConcede, ExecutiveVoice.Ear,
						"\"I was wrong about that one. I'm not wrong about this one.\"",
						"No defence, just the admission. Sometimes it works.");
				break;

			case Objection.PlaylistFull:
				if (inst.TheSuit >= 3)
					Offer(CallCounter.SuitLateNight, ExecutiveVoice.Suit,
						"\"I don't want a slot. I want the overnight, where nothing's spoken for.\"",
						"Ask for the hours nobody is fighting over.");
				if (inst.TheStreet >= 3)
					Offer(CallCounter.StreetRival, ExecutiveVoice.Street,
						"\"Then it goes across town and you hear it on their air in three weeks.\"",
						c.regionalGenreMomentum > 0.08f
							? "Plausible: the sound is moving here."
							: "Nothing behind it. He may well shrug.",
						bluff: c.regionalGenreMomentum <= 0.08f);
				break;
		}

		opts.Add(new CallOption {
			label = "Press the point", subLabel = "Say it again, louder. It sometimes works.",
			counter = CallCounter.PressIt, minutes = CounterMinutes,
		});
		opts.Add(new CallOption {
			label = "Let it go", subLabel = "Drop the ask. Nothing gained, nothing burned.",
			counter = CallCounter.BackOff,
		});
		return opts;
	}

	/// <summary>Play a counter and resolve. A grounded answer to the objection he actually raised is
	/// worth roughly double pressing the point; a bluff he catches costs you the roll and some trust.</summary>
	public void PlayCounter(RolodexCall call, CallCounter counter, out string message) {
		message = "";
		RolodexCallContext c = call.ctx;
		if (call.stage != CallStage.Pushback) { message = "Not at that point in the call."; return; }

		if (counter == CallCounter.BackOff) {
			call.Say(RolodexSceneBeat.ActiveCheckPrompt, "You let it drop. Some other day, maybe.", isPlayer: true);
			call.pendingApproach = RolodexApproach.HangUp;
			call.stage = CallStage.Open;
			call.objection = Objection.None;
			call.chanceModifier = 0f;
			Changed?.Invoke();
			return;
		}

		if (counter != CallCounter.PressIt && TimeManager.Instance?.CanAffordMinutes(CounterMinutes) == true)
			SpendMinutes(CounterMinutes);

		CallOption played = CounterOptions(call).FirstOrDefault(o => o.counter == counter);
		if (played != null) call.Say(RolodexSceneBeat.ActiveCheckPrompt, played.label, isPlayer: true);
		call.counterUsed = true;

		// A bluff is a second roll before the first one. He catches it on taste, on knowing his own
		// market, or on simply having been in this business longer than you.
		if (played?.isBluff == true) {
			float catchChance = Mathf.Clamp(
				0.30f + c.djTaste * 0.35f
					+ (c.dj != null && c.dj.archetype == DJArchetype.Regional ? 0.20f : 0f)
					- c.instincts.TheStreet * 0.05f
					- c.rapport * 0.25f,
				0.10f, 0.90f);
			if (GD.Randf() < catchChance) {
				call.Say(RolodexSceneBeat.Pushback,
					"There is a silence with the shape of a man checking something. \"No,\" he says. \"That isn't true, is it.\"",
					speaker: call.entry.displayName);
				ApplyRapport(call.entry, -0.05f, floorAtZero: true);
				call.entry.log.Insert(0, $"{Today()} — Tried it on and got caught. He does not like being handled.");
				call.lastSucceeded = false;
				call.stage = CallStage.Resolved;
				call.Say(RolodexSceneBeat.Failure, "\"Send me the record if you want. Don't send me the story.\"",
					speaker: call.entry.displayName);
				call.Say(RolodexSceneBeat.RelationshipAftermath,
					RolodexFragments.Pick(RolodexSceneBeat.RelationshipAftermath, c));
				message = "He caught it.";
				TrimLog(call.entry);
				Changed?.Invoke();
				return;
			}
			call.chanceModifier += 0.10f;   // it landed, but a bluff never buys what the truth buys
		} else {
			call.chanceModifier += CounterWeight(counter, c);
		}

		// FixerSweeten converts the approach into an actual payola arrangement -- the sentence had a
		// number in it, so the ledger has to hear about it.
		if (counter == CallCounter.FixerSweeten && call.pendingApproach != RolodexApproach.OfferPayola) {
			call.pendingApproach = RolodexApproach.OfferPayola;
			call.pendingPayload = PayolaTier.Small;
		}
		if (counter == CallCounter.SuitUnderwrite && call.pendingApproach != RolodexApproach.CommercialPitch) {
			call.pendingApproach = RolodexApproach.CommercialPitch;
			call.pendingPayload = AdBuyTier.Small;
		}
		if (counter == CallCounter.CallInFavor) call.entry.theyOweThem = false;

		Resolve(call, out message);
	}

	/// <summary>How much a truthful counter is worth. The ones that answer the objection with a real
	/// fact are worth the most; asking for a smaller yes is worth a lot because it is a smaller ask.</summary>
	private static float CounterWeight(CallCounter counter, RolodexCallContext c) => counter switch {
		CallCounter.PressIt        => 0.05f,
		CallCounter.EarChorus      => 0.20f + (c.recordHook - 0.55f) * 0.30f,
		CallCounter.EarConcede     => 0.14f + c.djEgo * 0.12f,
		CallCounter.StreetScene    => 0.18f + Mathf.Max(0f, c.regionalGenreMomentum) * 0.80f,
		CallCounter.StreetRival    => 0.12f,
		CallCounter.SuitNumbers    => 0.16f + Mathf.Min(0.14f, c.unitsTotal / 4000f),
		CallCounter.SuitLateNight  => 0.26f,   // a smaller ask is a much easier yes
		CallCounter.SuitUnderwrite => 0.22f,
		CallCounter.FixerSweeten   => 0.24f * (1f - c.djSuspicion),
		CallCounter.CallInFavor    => 0.40f,
		_ => 0f,
	};

	// ========================================================================================
	// BEATS 7-9: outcome, consequence, hook
	// ========================================================================================

	/// <summary>Roll it, apply the real effect, and play the closing beats.</summary>
	private void Resolve(RolodexCall call, out string message) {
		RolodexCallContext c = call.ctx;
		RolodexEntry entry = call.entry;
		message = "";

		int minutes = call.pendingApproach switch {
			RolodexApproach.PersonalPitch => PersonalPitchMinMinutes + (int)GD.RandRange(0, PersonalPitchMaxMinutes - PersonalPitchMinMinutes),
			RolodexApproach.AskForFavor   => AskAFavorMinMinutes + (int)GD.RandRange(0, AskAFavorMaxMinutes - AskAFavorMinMinutes),
			RolodexApproach.AskForIntroduction => IntroductionMinutes,
			_ => 10,
		};
		if (TimeManager.Instance?.CanAffordMinutes(minutes) == true) { SpendMinutes(minutes); call.minutesSpent += minutes; }

		bool success = call.pendingApproach is RolodexApproach.AskForFavor or RolodexApproach.AskForIntroduction
			|| GD.Randf() < call.EffectiveChance;
		call.lastSucceeded = success;
		call.stage = CallStage.Resolved;

		switch (call.pendingApproach) {
			case RolodexApproach.PersonalPitch:      ResolvePitch(call, success, out message); break;
			case RolodexApproach.CommercialPitch:    ResolveAdBuy(call, success, out message); break;
			case RolodexApproach.OfferPayola:        ResolvePayola(call, success, out message); break;
			case RolodexApproach.RivalPressure:      ResolveRivalPressure(call, success, out message); break;
			case RolodexApproach.AskForFavor:        ResolveFavor(call, out message); break;
			case RolodexApproach.AskForIntroduction: ResolveIntroduction(call, out message); break;
		}

		call.Say(RolodexSceneBeat.RelationshipAftermath,
			RolodexFragments.Pick(RolodexSceneBeat.RelationshipAftermath, c));
		if (success && GD.Randf() < 0.5f)
			call.Say(RolodexSceneBeat.Exit, RolodexFragments.Pick(RolodexSceneBeat.Exit, c));

		TrimLog(entry);
		Changed?.Invoke();
	}

	private void ResolvePitch(RolodexCall call, bool success, out string message) {
		RolodexCallContext c = call.ctx;
		RolodexEntry entry = call.entry;
		var chart = ChartManager.Instance;

		if (!success) {
			call.Say(RolodexSceneBeat.Failure, RolodexFragments.Pick(RolodexSceneBeat.Failure, c) ?? "\"No.\"",
				speaker: entry.displayName);
			entry.log.Insert(0, $"{Today()} — Pitched \"{c.baseRecord.title}\". He passed.");
			Note($"Pitched \"{c.baseRecord.title}\" to {entry.displayName} ({c.station.callsign}) -- no sale.");
			message = "He passed.";
			return;
		}

		call.Say(RolodexSceneBeat.Success, RolodexFragments.Pick(RolodexSceneBeat.Success, c) ?? "\"All right.\"",
			speaker: entry.displayName);

		// A won pitch buys RAPPORT (label-wide, slow, helps everything you ever release here) and
		// ADVOCACY (this record, this station, expiring). What it does with the advocacy depends
		// entirely on whether this man is allowed to decide anything.
		float gain = 0.09f * Mathf.Lerp(0.4f, 1f, c.recordQuality);
		float rapportAfter = ApplyRapport(entry, gain);
		entry.MaybePromoteState(rapportAfter);

		float boost = PitchAdvocacyBase + c.djInfluence * PitchAdvocacyInfluenceBonus
			+ (call.counterUsed ? 0.04f : 0f);
		int week = chart?.GetCurrentChartWeek() ?? 0;

		// A pitch stakes your word. It settles against the record's ACTUAL sales in a month.
		entry.pendingMemories.Add(new RolodexEntry.PendingRecordMemory {
			recordId = c.baseRecord.recordId, recordTitle = c.baseRecord.title,
			evalWeek = week + RecordMemoryEvalWeeks,
			unitsAtPitch = c.record.totalUnitsSold,
		});

		// THE SPLIT. djAutonomy is the sim's own measure of how much this jock's opinion counts at his
		// own station -- it is what scales the taste term in the candidacy meeting, and the Boss Radio
		// conversion drives it to 0.1. So it is exactly the right gate: before Boss, a personality jock
		// does not "take it to a meeting", he cues it up. After Boss, the sheet decides and he does not.
		bool decidesHimself = c.stationAutonomy >= DirectSpinAutonomy;

		if (decidesHimself) {
			bool added = chart?.PlayerSpinNow(entry.stationId, c.baseRecord.recordId) ?? false;
			// Shorter advocacy behind it: he has already done the thing, this is just him defending it
			// for a couple of weeks when the ordinary meeting comes round.
			chart?.Advocacy.Grant(c.baseRecord.recordId, entry.stationId, Label.labelId, entry.djId,
				boost, week, PitchSpinNowWeeks, AdvocacyMethod.PersonalPitch);
			call.Say(RolodexSceneBeat.RelationshipAftermath, added
				? $"No meeting, no memo. {c.station.callsign} is spinning \"{c.baseRecord.title}\" from " +
				  "tonight, in light rotation. Whether it stays there is between the record and the phones."
				: $"{c.station.callsign} already had it on. He agrees to keep it there a while longer.");
			entry.log.Insert(0, $"{Today()} — Talked him onto \"{c.baseRecord.title}\". He put it straight on the air. Rapport +{gain:F2}.");
			Note($"{c.station.callsign} is spinning \"{c.baseRecord.title}\" -- {entry.displayName} put it on himself.");
			message = $"{c.station.callsign} is playing it, starting tonight.";
			return;
		}

		chart?.Advocacy.Grant(c.baseRecord.recordId, entry.stationId, Label.labelId, entry.djId,
			boost, week, PitchAdvocacyWeeks, AdvocacyMethod.PersonalPitch);
		call.Say(RolodexSceneBeat.RelationshipAftermath,
			$"He cannot just play it -- {c.station.callsign} runs off a sheet he does not write. He will argue " +
			$"for \"{c.baseRecord.title}\" at the next {PitchAdvocacyWeeks} playlist meetings. The meeting decides.");
		entry.log.Insert(0, $"{Today()} — Talked him onto \"{c.baseRecord.title}\". Arguing it for {PitchAdvocacyWeeks} weeks. Rapport +{gain:F2}.");
		Note($"{entry.displayName} ({c.station.callsign}) will argue \"{c.baseRecord.title}\" at the playlist meeting.");
		message = $"He'll push it at the meeting -- {c.station.callsign} isn't his to decide.";
	}

	private void ResolveAdBuy(RolodexCall call, bool success, out string message) {
		RolodexCallContext c = call.ctx;
		RolodexEntry entry = call.entry;
		AdBuyTier tier = call.pendingPayload is AdBuyTier t ? t : AdBuyTier.Small;
		float cost = AdBuyCost(tier);

		if (Label.cashReserves < cost) {
			call.Say(RolodexSceneBeat.Failure,
				"You do the arithmetic while he waits, and the arithmetic says no. You change the subject.");
			message = $"You're ${cost - Label.cashReserves:N0} short of a ${cost:N0} buy.";
			return;
		}
		if (!success) {
			call.Say(RolodexSceneBeat.Failure,
				"\"We're sold out on that daypart. Talk to the sales manager, he'll tell you the same thing slower.\"",
				speaker: entry.displayName);
			entry.log.Insert(0, $"{Today()} — Tried to buy time around \"{c.baseRecord.title}\". No inventory.");
			message = "No airtime to sell you this week.";
			return;
		}

		Label.cashReserves -= cost;
		Label.monthlyExpenses += cost;

		float gain = AdBuyRapportGain(tier);
		float after = ApplyRapport(entry, gain);
		entry.MaybePromoteState(after);

		var chart = ChartManager.Instance;
		chart?.Advocacy.Grant(c.baseRecord.recordId, entry.stationId, Label.labelId, entry.djId,
			AdBuyAdvocacy(tier), chart.GetCurrentChartWeek(), AdBuyWeeks(tier), AdvocacyMethod.AdvertisingBuy);

		call.Say(RolodexSceneBeat.Success,
			"\"Fine. I'll have traffic cut the spots. It's an advertiser's record now — that's a different conversation upstairs.\"",
			speaker: entry.displayName);
		call.Say(RolodexSceneBeat.RelationshipAftermath,
			$"Bought and paid for: ${cost:N0}. The record gets {AdBuyWeeks(tier)} weeks of a warmer hearing at {c.station.callsign}. " +
			"It does not get played because you paid — it gets considered.");
		entry.log.Insert(0, $"{Today()} — Bought a {AdBuyTierName(tier)} around \"{c.baseRecord.title}\" (${cost:N0}).");
		Note($"Bought {AdBuyTierName(tier)} promo time at {c.station.callsign} for \"{c.baseRecord.title}\" -- ${cost:N0}.");
		message = $"Spot's booked at {c.station.callsign}.";
	}

	private void ResolvePayola(RolodexCall call, bool success, out string message) {
		RolodexCallContext c = call.ctx;
		RolodexEntry entry = call.entry;
		PayolaTier tier = call.pendingPayload is PayolaTier t ? t : PayolaTier.Small;
		float budget = PayolaCost(tier);

		if (Label.cashReserves < budget) {
			call.Say(RolodexSceneBeat.Failure, "The number you were about to say is a number you do not have.");
			message = $"You're ${budget - Label.cashReserves:N0} short of a ${budget:N0} envelope.";
			return;
		}
		if (!success) {
			call.Say(RolodexSceneBeat.Failure,
				c.djSuspicion > 0.5f
					? "\"Don't.\" One syllable, and all the warmth gone out of the line. \"Not this month. Not from you.\""
					: "\"I'm going to pretend the connection dropped for a second there.\"",
				speaker: entry.displayName);
			ApplyRapport(entry, -0.04f, floorAtZero: true);
			entry.log.Insert(0, $"{Today()} — Offered him money. He didn't take it, and he remembers being asked.");
			message = "He didn't take it.";
			return;
		}

		Label.cashReserves -= budget;
		Label.monthlyExpenses += budget;
		ChartManager.Instance?.PlacePayolaCash(c.baseRecord.recordId, Label.labelId, entry.stationId, budget);

		call.Say(RolodexSceneBeat.Success,
			"\"Send it to the station. Not my name on it. Somebody in traffic will know what to do with it.\"",
			speaker: entry.displayName);
		call.Say(RolodexSceneBeat.RelationshipAftermath,
			"It goes in the ledger the way these things always go in the ledger: quietly, and permanently.");
		entry.log.Insert(0, $"{Today()} — Slipped {entry.displayName} a {PayolaTierName(tier)} for \"{c.baseRecord.title}\" (${budget:N0}).");
		Note($"Payola: {PayolaTierName(tier)} (${budget:N0}) to {entry.displayName} ({c.station.callsign}) for \"{c.baseRecord.title}\".");
		message = $"{c.station.callsign} owes your record a spin -- for now.";
	}

	private void ResolveRivalPressure(RolodexCall call, bool success, out string message) {
		RolodexCallContext c = call.ctx;
		RolodexEntry entry = call.entry;
		var chart = ChartManager.Instance;

		if (!success) {
			call.Say(RolodexSceneBeat.Failure,
				"\"Good,\" he says. \"Let them have it. Saves me a slot.\"", speaker: entry.displayName);
			entry.log.Insert(0, $"{Today()} — Tried the rival angle on \"{c.baseRecord.title}\". He shrugged.");
			message = "He shrugged it off.";
			return;
		}

		// Urgency is short and shallow by design: it buys you a look, not a relationship.
		chart?.Advocacy.Grant(c.baseRecord.recordId, entry.stationId, Label.labelId, entry.djId,
			0.11f, chart.GetCurrentChartWeek(), 2, AdvocacyMethod.RivalPressure);
		call.Say(RolodexSceneBeat.Success,
			"\"Then I'll look at it before they do. That's not a favour, that's arithmetic.\"", speaker: entry.displayName);
		call.Say(RolodexSceneBeat.RelationshipAftermath,
			$"Two weeks of urgency at {c.station.callsign}. Nothing warmer than that — he did it to win, not to help.");
		entry.log.Insert(0, $"{Today()} — Rival angle landed on \"{c.baseRecord.title}\". Two weeks of urgency.");
		message = "He'll look at it, out of spite if nothing else.";
	}

	private void ResolveFavor(RolodexCall call, out string message) {
		RolodexCallContext c = call.ctx;
		RolodexEntry entry = call.entry;
		var chart = ChartManager.Instance;

		entry.theyOweThem = false;
		float after = ApplyRapport(entry, 0.12f);
		entry.MaybePromoteState(after);

		if (c.HasRecord)
			chart?.Advocacy.Grant(c.baseRecord.recordId, entry.stationId, Label.labelId, entry.djId,
				FavorAdvocacy, chart.GetCurrentChartWeek(), FavorAdvocacyWeeks, AdvocacyMethod.FavorCalledIn);

		call.Say(RolodexSceneBeat.Success,
			"A pause. \"Yeah. I did say that.\" Then, flatter: \"Consider it even.\"", speaker: entry.displayName);
		call.Say(RolodexSceneBeat.RelationshipAftermath, c.HasRecord
			? $"He puts real weight behind \"{c.baseRecord.title}\" for {FavorAdvocacyWeeks} weeks. He will not do it twice."
			: "You spent it on nothing in particular. He noticed.");
		entry.log.Insert(0, $"{Today()} — Called in the favour.");
		Note($"Called in a favour with {entry.displayName} ({c.station.callsign}).");
		message = "He came through. Consider it even.";
	}

	private void ResolveIntroduction(RolodexCall call, out string message) {
		RolodexEntry entry = call.entry;
		var chart = ChartManager.Instance;

		var known = new HashSet<string>(rolodex.Select(e => e.stationId), StringComparer.Ordinal);
		var candidates = chart?.ReporterStationsInRegion(Label.homeRegion)
			.Where(s => !known.Contains(s.stationId) && !string.IsNullOrEmpty(s.leadDjId) && chart.GetDeejay(s.leadDjId) != null)
			.ToList() ?? new List<RadioStation>();
		if (candidates.Count == 0) {
			call.Say(RolodexSceneBeat.Failure,
				"\"In this market? You already know everybody worth knowing. Go somewhere else.\"", speaker: entry.displayName);
			message = "Nobody left in this region he can introduce you to.";
			return;
		}

		RadioStation target = candidates.OrderByDescending(s => chart.GetDeejay(s.leadDjId)?.influence ?? 0f).First();
		Deejay targetDj = chart.GetDeejay(target.leadDjId);
		string targetName = SynthesizeDJName(targetDj, target);

		var introduced = new RolodexEntry {
			djId = targetDj.djId, stationId = target.stationId,
			state = DiscoveryState.Introduced, displayName = targetName,
			portraitKey = targetDj.archetype.ToString(), shiftKnown = true,
		};
		introduced.log.Add($"{Today()} — Introduced by {entry.displayName}: {targetName}, {target.callsign} ({target.format}, {target.cityName}).");
		rolodex.Add(introduced);

		call.Say(RolodexSceneBeat.Success,
			$"\"Call {targetName} at {target.callsign}. Tell him I gave you the number. Don't tell him more than that.\"",
			speaker: entry.displayName);
		call.Say(RolodexSceneBeat.RelationshipAftermath,
			$"{RolodexShifts.WindowAdvice(RolodexShifts.ShiftOf(targetDj))}");
		entry.log.Insert(0, $"{Today()} — Put you on to {targetName} at {target.callsign}.");
		Note($"{entry.displayName} introduced you to {targetName} at {target.callsign}.");
		message = $"You're in with {targetName} at {target.callsign}.";
	}

	/// <summary>Continue an open call with another approach, or leave.</summary>
	public void ContinueCall(RolodexCall call) {
		if (call == null) return;
		call.stage = CallStage.Open;
		call.objection = Objection.None;
		call.pendingApproach = RolodexApproach.HangUp;
		call.pendingPayload = null;
		call.chanceModifier = 0f;
		call.counterUsed = false;
		// The situation has changed -- he is carrying something now, or the money is gone.
		call.ctx = BuildCallContext(call.entry, call.recordId);
		Changed?.Invoke();
	}

	public void EndCall(RolodexCall call) {
		if (call == null) return;
		call.stage = CallStage.Ended;
		if (ActiveCall == call) ActiveCall = null;
		Changed?.Invoke();
	}

	/// <summary>Switch the record under discussion mid-call and rebuild the context around it.</summary>
	public void SetCallRecord(RolodexCall call, string recordId) {
		if (call == null) return;
		call.recordId = recordId;
		call.ctx = BuildCallContext(call.entry, recordId);
		Changed?.Invoke();
	}

	// ========================================================================================
	// SIM WRITES
	// ========================================================================================

	/// <summary>The single place rapport is written. Gains shrink as the relationship climbs (there is
	/// no path to owning a man through repetition); losses are floored at zero rather than going
	/// negative, so a bad call cools you off without inventing a feud.</summary>
	private float ApplyRapport(RolodexEntry entry, float delta, bool floorAtZero = false) {
		RadioStation station = ChartManager.Instance?.GetRadioStation(entry.stationId);
		if (station?.rt == null || Label == null) return 0f;
		float before = station.rt.Rapport(Label.labelId);
		float after;
		if (delta >= 0f) {
			float headroom = Mathf.Clamp(1f - before / RapportSoftCap, 0.15f, 1f);
			after = before + delta * headroom;
		} else {
			after = floorAtZero ? Mathf.Max(0f, before + delta) : before + delta;
		}
		station.rt.labelRapport[Label.labelId] = after;
		return after;
	}

	// ========================================================================================
	// STATION STATE SAVE / RESTORE
	// ========================================================================================

	/// <summary>
	/// Snapshot the reporter-panel state that is the direct product of player action: cultivated
	/// rapport, and the player's own records' rotation slots. The panel itself is rebuilt from the
	/// station seed on load (AI playlists re-derive within a week, so nothing is lost there), but
	/// these two are not re-derivable and were being silently destroyed by every save/load -- which
	/// made the whole Rolodex look inert to anyone who reloaded.
	/// </summary>
	private List<StationPlayerStateSaveData> CaptureStationState() {
		var list = new List<StationPlayerStateSaveData>();
		var chart = ChartManager.Instance;
		if (chart == null || Label == null) return list;
		var playerRecordIds = new HashSet<string>(
			ReleasedRecords.Where(r => r.baseRecord != null).Select(r => r.baseRecord.recordId), StringComparer.Ordinal);

		foreach (RadioStation station in chart.AllReporterStations) {
			StationRuntime rt = station?.rt;
			if (rt == null) continue;
			float rapport = rt.Rapport(Label.labelId);
			var spins = new Dictionary<string, int>(StringComparer.Ordinal);
			var weeks = new Dictionary<string, int>(StringComparer.Ordinal);
			foreach (var kv in rt.playlist) {
				if (!playerRecordIds.Contains(kv.Key)) continue;   // AI rotations re-derive; ours do not
				spins[kv.Key] = (int)kv.Value;
				weeks[kv.Key] = rt.weeksInPlaylist.TryGetValue(kv.Key, out int w) ? w : 0;
			}
			if (rapport == 0f && spins.Count == 0) continue;       // nothing worth writing
			list.Add(new StationPlayerStateSaveData {
				StationId = station.stationId, Rapport = rapport,
				PlayerSpins = spins, PlayerSpinWeeks = weeks,
			});
		}
		return list;
	}

	/// <summary>Put cultivated rapport and the player's rotation slots back onto the freshly rebuilt
	/// panel. Runs after WorldStateService.Apply, which is what rebuilds it.</summary>
	private void RestoreStationState(List<StationPlayerStateSaveData> saved) {
		var chart = ChartManager.Instance;
		if (chart == null || Label == null || saved == null) return;
		foreach (StationPlayerStateSaveData s in saved) {
			RadioStation station = chart.GetRadioStation(s.StationId);
			StationRuntime rt = station?.rt;
			if (rt == null) continue;
			if (s.Rapport != 0f) rt.labelRapport[Label.labelId] = s.Rapport;
			foreach (var kv in s.PlayerSpins ?? new Dictionary<string, int>()) {
				rt.playlist[kv.Key] = (SpinTier)Math.Clamp(kv.Value, 0, 3);
				rt.weeksInPlaylist[kv.Key] =
					s.PlayerSpinWeeks != null && s.PlayerSpinWeeks.TryGetValue(kv.Key, out int w) ? w : 0;
			}
		}
	}

	private static void TrimLog(RolodexEntry entry) {
		if (entry.log.Count > 20) entry.log.RemoveRange(20, entry.log.Count - 20);
	}

	// ========================================================================================
	// WEEKLY SETTLEMENT
	// ========================================================================================

	/// <summary>Run from OnWeekEnded. Expire spent advocacy, apply payola busts, settle staked pitches.</summary>
	private void ProcessRolodexWeek() {
		var chart = ChartManager.Instance;
		if (chart == null) return;
		chart.Advocacy.ExpireThrough(chart.GetCurrentChartWeek());
		ProcessAdvocacyOutcomes();
		ProcessPayolaScandals();
		ProcessRecordMemories();
	}

	/// <summary>
	/// Report what the stations actually DID with the records you talked them into. This is the payoff
	/// half of the Rolodex and it was missing: a won call wrote a real candidacy edge, but nothing ever
	/// told the player whether it turned into airplay, so the whole verb read as inert.
	///
	/// Each advocacy carries the spin tier it last saw. The diff against the live playlist is the news:
	/// added, moved up, moved down, dropped. A watch is forgotten once its record is off the air and
	/// its story has been told.
	/// </summary>
	private void ProcessAdvocacyOutcomes() {
		var chart = ChartManager.Instance;
		if (chart == null || Label == null) return;
		var done = new List<StationAdvocacy>();

		foreach (StationAdvocacy a in chart.Advocacy.Active) {
			if (a.labelId != Label.labelId) continue;
			RolodexEntry entry = rolodex.FirstOrDefault(e => e.stationId == a.stationId);
			RadioStation station = chart.GetRadioStation(a.stationId);
			string call = station?.callsign ?? "the station";
			string title = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == a.recordId)
				?.baseRecord?.title ?? "your record";

			SpinTier now = chart.SpinTierOf(a.stationId, a.recordId);
			SpinTier was = a.lastSeenTier;
			a.lastSeenTier = now;
			if (now != SpinTier.None) a.everPlayed = true;

			if (now != was) {
				string line =
					was == SpinTier.None ? $"{call} put \"{title}\" into {TierWord(now)} rotation."
					: now == SpinTier.None ? $"{call} dropped \"{title}\"."
					: now > was ? $"{call} moved \"{title}\" up to {TierWord(now)} rotation."
					: $"{call} cut \"{title}\" back to {TierWord(now)} rotation.";
				Note(line);
				if (entry != null) { entry.log.Insert(0, $"{Today()} — {line}"); TrimLog(entry); }
			}

			// The watch is finished when the argument has run out AND the record is off the air.
			if (a.expired && now == SpinTier.None) {
				if (!a.everPlayed) {
					string line = $"{call} never did play \"{title}\". {(entry != null ? entry.displayName : "He")} argued for it and lost.";
					Note(line);
					if (entry != null) { entry.log.Insert(0, $"{Today()} — {line}"); TrimLog(entry); }
				}
				done.Add(a);
			}
		}
		foreach (StationAdvocacy a in done) chart.Advocacy.Forget(a);
	}

	public static string TierWord(SpinTier tier) => tier switch {
		SpinTier.High  => "heavy",
		SpinTier.Mid   => "medium",
		SpinTier.Light => "light",
		_              => "no",
	};

	/// <summary>Read this week's payola scandals off ChartManager and apply the teeth: cash penalty,
	/// desk log, and a burned cash channel so the relationship cannot just be re-bought next week.</summary>
	private void ProcessPayolaScandals() {
		var scandals = ChartManager.Instance?.PendingPayolaScandals;
		if (scandals == null || scandals.Count == 0) return;
		foreach (ScandalEvent scandal in scandals) {
			if (scandal.labelId != Label.labelId) continue;
			Label.cashReserves -= scandal.financialPenalty;
			RolodexEntry entry = rolodex.FirstOrDefault(e => e.stationId == scandal.stationId);
			if (entry != null) {
				entry.payolaBurned = true;
				entry.log.Insert(0, $"{Today()} — BUSTED: {scandal.description} He won't touch cash from you again.");
				TrimLog(entry);
			}
			Note($"{scandal.description} -${scandal.financialPenalty:N0}.");
		}
	}

	/// <summary>Settle the pitches you staked your word on, against the record's ACTUAL sales since the
	/// pitch. A real hit deepens trust and leaves him owing you one; a real flop burns the professional
	/// channel specifically -- he will still take your money, he just will not take your word.</summary>
	private void ProcessRecordMemories() {
		var chart = ChartManager.Instance;
		if (chart == null || rolodex.Count == 0) return;
		int week = chart.GetCurrentChartWeek();

		foreach (RolodexEntry entry in rolodex) {
			if (entry.pendingMemories.Count == 0) continue;
			var due = entry.pendingMemories.Where(m => m.evalWeek <= week).ToList();
			if (due.Count == 0) continue;
			foreach (var mem in due) entry.pendingMemories.Remove(mem);

			foreach (RolodexEntry.PendingRecordMemory mem in due) {
				RecordRuntimeData rec = ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == mem.recordId);
				long unitsNow = rec?.totalUnitsSold ?? 0;
				long moved = Math.Max(0, unitsNow - mem.unitsAtPitch);

				if (moved >= RecordMemoryGoodUnitsThreshold) {
					entry.theyOweThem = true;
					float after = ApplyRapport(entry, RecordMemoryGoodRapportBonus);
					entry.MaybePromoteState(after);
					entry.log.Insert(0, $"{Today()} — You told him \"{mem.recordTitle}\" would move. It sold {moved:N0} copies since. He remembers.");
					Note($"{entry.displayName} remembers \"{mem.recordTitle}\" came through -- {moved:N0} copies.");
				} else if (moved < RecordMemoryBadUnitsThreshold) {
					entry.professionallyBurned = true;
					ApplyRapport(entry, -RecordMemoryBadRapportPenalty, floorAtZero: true);
					entry.log.Insert(0, $"{Today()} — You told him \"{mem.recordTitle}\" would move. It sold {moved:N0} copies. He's not taking your word again.");
					Note($"{entry.displayName} isn't happy -- \"{mem.recordTitle}\" only moved {moved:N0}.");
				} else {
					entry.log.Insert(0, $"{Today()} — \"{mem.recordTitle}\" sold {moved:N0} copies since your pitch. Nothing special either way.");
				}
			}
			TrimLog(entry);
		}
		Changed?.Invoke();
	}
}
