using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// CONTRACT NEGOTIATION -- Part 2 of the directive, plus deliverables, renewal, and mid-contract
// managers. Owns everything the negotiation scene reads and writes: posture, the reservation
// package, the objection/counter loop, and the sign/renew-or-walk resolution. Entirely
// player-turn-local -- no GD.Rand* call anywhere in this file touches the AI economy's RNG stream
// or call order. Acceptance is a deterministic threshold check; the one fogged read uses a stable
// hash (same discipline as ScoutingPerception); the manager-pickup roll and the royalty/deliverables
// asks use GD.Randf()/GD.RandRange() exactly like the rest of the player-only desk (RolodexVerbs.cs
// does the same) -- safe because none of it runs inside population generation or the AI's weekly tick.
// ============================================================================================
public partial class PlayerDesk : Node {

	public const int NegotiationRoundHours = ActionCosts.QuickMeeting;   // 2h to table, sweeten, promise, or hold firm
	private const int PromiseSinglesBump = 2;                            // extra sides promised, not paid for
	private const int ForcedWalkCooldownDays = 10;                       // patience exhausted on a NEW signing -- give it time

	/// <summary>The live renewal in progress, if any -- held here (not the UI) so it survives a
	/// panel refresh, same as <see cref="ActiveCall"/> and a Prospect's own <see cref="ContractTalk"/>.</summary>
	public RenewalOffer PendingRenewal { get; private set; }

	// Contracts already nagged about this "up for renewal" spell, so the monthly check speaks once,
	// not every month it stays unrenewed. Cleared the moment the artist renews, leaves, or is dropped.
	private readonly HashSet<string> maturedNotified = new(StringComparer.Ordinal);

	// ========================================================================================
	// POSTURE -- most signings (and renewals) stay one click
	// ========================================================================================

	/// <summary>What kind of interaction this signing needs. Difficulty comes off the manager
	/// lookup, drama off the lineup's already-computed GetDramaRisk, heat off the act's own public
	/// standing. All three are real reads, none are invented -- and for a renewal these are all
	/// CURRENT values, so a Star who broke out since signing negotiates like one even if they were
	/// a Pushover bar band on the way in.</summary>
	public static NegotiationPosture PostureOf(SimulatedArtist artist) {
		if (artist == null) return NegotiationPosture.Pushover;
		float difficulty = ManagerProfile.Of(artist.manager).NegotiationDifficulty;
		float drama = DramaOf(artist);
		bool heatLow = artist.reputation < 0.20f && artist.momentum < 0.20f;
		if (difficulty < 0.25f && drama < 0.55f && heatLow) return NegotiationPosture.Pushover;
		if (difficulty < 0.60f || drama < 0.75f) return NegotiationPosture.Firm;
		return NegotiationPosture.Hardball;
	}

	private static float DramaOf(SimulatedArtist artist) {
		List<Musician> active = artist.members?.Where(m => m.isActive).ToList() ?? new List<Musician>();
		return active.Count > 0 ? active.Average(m => m.GetDramaRisk()) : 0.30f;
	}

	// ========================================================================================
	// THE RESERVATION PACKAGE
	// ========================================================================================

	/// <summary>Which axis this act actually weighs, and how much. Cash axes track commercial
	/// pragmatism; the control axes track artistic ambition and whatever the manager themselves
	/// demands (a Svengali wants label control, a Visionary wants both back); deliverables tracks
	/// artistic ambition too -- a heavier release quota is the label milking the act for product,
	/// and an ambitious act resents that more than a pragmatic one does. No new fields -- this reads
	/// ArtistEvolutionProfile and ManagerProfile, both already live.</summary>
	private static Dictionary<ContractAxis, float> NegotiationWeights(SimulatedArtist artist) {
		float ambition = artist?.evolution?.artisticAmbition ?? 0.5f;
		float pragmatism = artist?.evolution?.commercialPragmatism ?? 0.5f;
		ManagerProfile.Modifiers mods = ManagerProfile.Of(artist?.manager ?? ManagerArchetype.None);
		var w = new Dictionary<ContractAxis, float> {
			[ContractAxis.Advance] = 0.20f + pragmatism * 0.28f,
			[ContractAxis.Royalty] = 0.18f + pragmatism * 0.20f,
			[ContractAxis.Term] = 0.09f,
			[ContractAxis.Deliverables] = 0.08f + ambition * 0.10f,
			[ContractAxis.Publishing] = 0.13f + ambition * 0.24f + (mods.DemandsArtistPublishing ? 0.12f : 0f),
			[ContractAxis.CreativeControl] = 0.13f + ambition * 0.28f + (mods.DemandsArtistControl ? 0.12f : 0f),
		};
		float total = w.Values.Sum();
		if (total <= 0f) return w;
		foreach (ContractAxis axis in w.Keys.ToList()) w[axis] /= total;
		return w;
	}

	/// <summary>How well one axis of the offer reads against the ask, 1.0 at the ask itself. Royalty
	/// uses the exact convex shortfall the directive specifies (shallow near the ask, steep as you
	/// cut). The rest use the same shape by construction: neutral at the ask, a real bonus for giving
	/// more, a harsher penalty for taking something away than the bonus for matching it.</summary>
	private static float AxisTerm(ContractAxis axis, ContractTermSheet offer, ContractTermSheet ask) => axis switch {
		ContractAxis.Advance => ask.Advance > 0f ? Mathf.Clamp(offer.Advance / ask.Advance, 0f, 1.5f) : 1f,
		ContractAxis.Royalty => RoyaltyTerm(offer.RoyaltyRate, ask.RoyaltyRate),
		ContractAxis.Term => Mathf.Clamp(ask.TermYears / (float)Mathf.Max(1, offer.TermYears), 0.4f, 1.6f),
		ContractAxis.Deliverables => DeliverablesTerm(offer.SinglesObligation, ask.SinglesObligation),
		ContractAxis.Publishing => offer.LabelOwnsPublishing == ask.LabelOwnsPublishing ? 1f
			: (!offer.LabelOwnsPublishing ? 1.25f : 0.65f),
		ContractAxis.CreativeControl => offer.ArtistCreativeControl == ask.ArtistCreativeControl ? 1f
			: (offer.ArtistCreativeControl ? 1.25f : 0.65f),
		_ => 1f,
	};

	/// <summary>shortfall = 0 at the ask, 1 at zero points; royaltyTerm = 1 - shortfall^1.5. Exact
	/// formula from the directive -- shallow near the ask, steep as you cut.</summary>
	private static float RoyaltyTerm(float offered, float baseline) {
		if (baseline <= 0f) return 1f;
		float shortfall = Mathf.Clamp((baseline - offered) / baseline, 0f, 1f);
		return 1f - Mathf.Pow(shortfall, 1.5f);
	}

	/// <summary>Same shape as Term: fewer sides asked of them than they expected is a concession
	/// (ratio above 1), more is a heavier workload (ratio below 1). An act that expected NO quota
	/// (established enough that CalculateContractSinglesObligation retires it) reads a demanded one
	/// as a straight imposition rather than a ratio, since there's no baseline to divide against.</summary>
	private static float DeliverablesTerm(int offered, int asked) {
		if (asked <= 0) return offered <= 0 ? 1f : Mathf.Clamp(1f - offered * 0.05f, 0.4f, 1f);
		return Mathf.Clamp(asked / (float)Mathf.Max(1, offered), 0.4f, 1.6f);
	}

	/// <summary>The weighted sum. Value(ask) is always exactly 1.0 by construction, which is what
	/// makes the reservation a clean fraction of it.</summary>
	private static float PackageValue(ContractTermSheet offer, ContractTermSheet ask, Dictionary<ContractAxis, float> weights) {
		float value = 0f;
		foreach (KeyValuePair<ContractAxis, float> kv in weights) value += kv.Value * AxisTerm(kv.Key, offer, ask);
		return value;
	}

	/// <summary>The objection he raises: the axis contributing the most to the shortfall, weighted
	/// by how much he actually cares about it -- not just whichever number happens to be furthest off.</summary>
	private static ContractAxis WorstAxis(ContractTermSheet offer, ContractTermSheet ask, Dictionary<ContractAxis, float> weights) {
		ContractAxis worst = ContractAxis.Advance;
		float worstDeficit = -1f;
		foreach (KeyValuePair<ContractAxis, float> kv in weights) {
			float deficit = kv.Value * Mathf.Max(0f, 1f - AxisTerm(kv.Key, offer, ask));
			if (deficit > worstDeficit) { worstDeficit = deficit; worst = kv.Key; }
		}
		return worst;
	}

	// ========================================================================================
	// OPENING THE TABLE
	// ========================================================================================

	/// <summary>Opens the negotiation scene for a Firm or Hardball act, new signing or renewal
	/// alike -- everything from here down reads only <see cref="ContractTalk.Artist"/> and
	/// <see cref="ContractTalk.ask"/>, never the caller's shape.</summary>
	private static ContractTalk BuildTalk(SimulatedArtist artist, ContractTermSheet ask, NegotiationPosture posture) {
		Dictionary<ContractAxis, float> weights = NegotiationWeights(artist);
		float drama = DramaOf(artist);
		float room = Mathf.Lerp(0.65f, 0.10f, Mathf.Clamp(0.6f * ask.NegotiationDifficulty + 0.4f * drama, 0f, 1f));
		int patience = 2 + Mathf.RoundToInt(3f * (1f - ask.NegotiationDifficulty));
		return new ContractTalk {
			ask = ask, posture = posture, weights = weights,
			reservation = 1f - room, patienceMax = patience, patienceLeft = patience,
		};
	}

	/// <summary>Called from <see cref="ApproachToSign"/> right after the baseline ask is built --
	/// Pushover acts never get one of these; they stay on the existing single-click ContractForm.</summary>
	private ContractTalk OpenNegotiation(Prospect prospect) {
		ContractTalk talk = BuildTalk(prospect.Artist, prospect.Baseline, prospect.Posture);
		talk.prospect = prospect;
		prospect.Talk = talk;
		return talk;
	}

	/// <summary>Prefill for the tabling form: the last thing you tabled, or the ask itself for round one.</summary>
	public static ContractTermSheet CurrentOffer(ContractTalk talk) => talk.lastOffer ?? talk.ask;

	/// <summary>Whether a give-back-for-cash trade is even on the table -- there has to be a control
	/// axis the label is currently holding for the artist to want it back.</summary>
	public static bool CanTradeAxes(ContractTalk talk) {
		ContractTermSheet o = CurrentOffer(talk);
		return o.LabelOwnsPublishing || !o.ArtistCreativeControl;
	}

	// ========================================================================================
	// TABLING A ROUND
	// ========================================================================================

	/// <summary>Puts a concrete offer on the table for a Firm/Hardball act. Costs
	/// <see cref="NegotiationRoundHours"/> every time, win or not -- a Hardball act with several
	/// rounds genuinely eats a day, where a Pushover signing stays the flat <see cref="SignHours"/>.</summary>
	public bool TableOffer(ContractTalk talk, float advance, float royaltyRate, int termYears, int singlesObligation,
			bool labelOwnsPublishing, bool artistCreativeControl, out string message) {
		message = "";
		if (talk?.Artist == null) { message = "No negotiation open."; return false; }
		if (talk.stage != ContractTalkStage.Tabling) { message = "Not at that point in the talks."; return false; }
		if (!Require(NegotiationRoundHours, out message)) return false;
		if (!talk.IsRenewal && !Label.HasRosterSpace) { message = "Roster is full."; return false; }
		bool ownershipOk = talk.IsRenewal ? talk.Artist.labelId == Label.labelId : string.IsNullOrEmpty(talk.Artist.labelId);
		if (!ownershipOk) { message = talk.IsRenewal ? "They're not on your roster any more." : "Somebody signed them first."; return false; }

		advance = Mathf.Max(0f, advance);
		if (!Label.CanAffordToSign(advance)) {
			message = $"You can't cover a ${advance:N0} advance and hold next month's overhead.";
			return false;
		}

		var offer = new ContractTermSheet(advance, Mathf.Clamp(royaltyRate, PlayerRoyaltyFloor, 0.15f),
			Mathf.Clamp(termYears, 1, 7), Mathf.Clamp(singlesObligation, 0, 30),
			labelOwnsPublishing, artistCreativeControl,
			talk.ask.NegotiationDifficulty, talk.ask.Manager, talk.ask.ManagerName, talk.ask.DemandSummary);

		Spend(NegotiationRoundHours);
		talk.roundsPlayed++;
		talk.lastOffer = offer;
		talk.lastOfferValue = PackageValue(offer, talk.ask, talk.weights);

		if (talk.lastOfferValue >= talk.reservation) { FinalizeSign(talk, offer, out message); return true; }

		talk.patienceLeft--;
		if (talk.patienceLeft <= 0) { WalkAway(talk, forced: true, out message); return true; }

		talk.objectionAxis = WorstAxis(offer, talk.ask, talk.weights);
		talk.stage = ContractTalkStage.Objection;
		string line = ObjectionLine(talk, offer);
		talk.log.Insert(0, line);
		message = line;
		Changed?.Invoke();
		return true;
	}

	// ========================================================================================
	// ANSWERING THE OBJECTION
	// ========================================================================================

	public bool PlayNegotiationCounter(ContractTalk talk, ContractCounter counter, out string message) {
		message = "";
		if (talk?.Artist == null) { message = "No negotiation open."; return false; }
		if (talk.stage != ContractTalkStage.Objection) { message = "Not at that point in the talks."; return false; }

		switch (counter) {
			case ContractCounter.SweetenAxis:
				talk.stage = ContractTalkStage.Tabling;
				message = "Back to the table -- raise the number he's actually stuck on.";
				Changed?.Invoke();
				return true;

			case ContractCounter.TradeAxes: {
				if (!CanTradeAxes(talk)) { message = "Nothing left to trade -- they already hold nothing you can give back."; return false; }
				ContractTermSheet o = CurrentOffer(talk);
				talk.lastOffer = new ContractTermSheet(
					Mathf.Round(o.Advance * 0.75f / 5f) * 5f, o.RoyaltyRate, o.TermYears, o.SinglesObligation,
					false, true,
					o.NegotiationDifficulty, o.Manager, o.ManagerName, o.DemandSummary);
				talk.stage = ContractTalkStage.Tabling;
				message = "You give back the publishing and the final word, and pencil the advance down to match.";
				Changed?.Invoke();
				return true;
			}

			case ContractCounter.Promise: return PlayPromise(talk, out message);
			case ContractCounter.HoldFirm: return PlayHoldFirm(talk, out message);

			case ContractCounter.Walk:
				WalkAway(talk, forced: false, out message);
				return true;

			default:
				message = "Nothing to do there.";
				return false;
		}
	}

	/// <summary>Costs no cash today: more sides promised, a real push behind the next release. Reads
	/// as a straight package-value bonus weighted by commercial pragmatism -- the act that will chase
	/// a hit under pressure is the one a promise actually moves -- and writes the extra obligation
	/// onto the eventual contract via the existing contractSinglesObligation.</summary>
	private bool PlayPromise(ContractTalk talk, out string message) {
		if (!Require(NegotiationRoundHours, out message)) return false;
		Spend(NegotiationRoundHours);

		ContractTermSheet o = CurrentOffer(talk);
		var promised = new ContractTermSheet(o.Advance, o.RoyaltyRate, o.TermYears,
			Mathf.Clamp(o.SinglesObligation + PromiseSinglesBump, 0, 30), o.LabelOwnsPublishing, o.ArtistCreativeControl,
			o.NegotiationDifficulty, o.Manager, o.ManagerName, o.DemandSummary);
		float pragmatism = talk.Artist.evolution?.commercialPragmatism ?? 0.5f;
		float credit = Mathf.Lerp(0.05f, 0.16f, pragmatism);

		talk.lastOffer = promised;
		talk.lastOfferValue = PackageValue(promised, talk.ask, talk.weights) + credit;

		if (talk.lastOfferValue >= talk.reservation) { FinalizeSign(talk, promised, out message); return true; }

		talk.patienceLeft--;
		if (talk.patienceLeft <= 0) { WalkAway(talk, forced: true, out message); return true; }

		talk.objectionAxis = WorstAxis(promised, talk.ask, talk.weights);
		string line = $"\"{PromiseSinglesBump} more sides, and I'll make sure the next one gets pushed properly.\" " +
			"He weighs it -- a promise is not a number, and he knows it.";
		talk.log.Insert(0, line);
		message = line;
		Changed?.Invoke();
		return true;
	}

	/// <summary>Re-table the same numbers and see who blinks. A patient act (at least half its rounds
	/// left) softens a little; an impatient one walks right there -- the bluff, and it can be called.</summary>
	private bool PlayHoldFirm(ContractTalk talk, out string message) {
		if (!Require(NegotiationRoundHours, out message)) return false;
		Spend(NegotiationRoundHours);

		bool patient = talk.patienceLeft >= Mathf.CeilToInt(talk.patienceMax * 0.5f);
		talk.patienceLeft--;
		if (!patient) { WalkAway(talk, forced: true, out message); return true; }

		talk.reservation = Mathf.Max(0.55f, talk.reservation - 0.03f);
		if (talk.lastOfferValue >= talk.reservation) { FinalizeSign(talk, CurrentOffer(talk), out message); return true; }
		if (talk.patienceLeft <= 0) { WalkAway(talk, forced: true, out message); return true; }

		talk.objectionAxis = WorstAxis(CurrentOffer(talk), talk.ask, talk.weights);
		string line = "He doesn't move much, but he doesn't hang up either.";
		talk.log.Insert(0, line);
		message = line;
		Changed?.Invoke();
		return true;
	}

	// ========================================================================================
	// RESOLUTION
	// ========================================================================================

	private void FinalizeSign(ContractTalk talk, ContractTermSheet sheet, out string message) {
		talk.stage = ContractTalkStage.Done;
		if (talk.IsRenewal) {
			FinalizeRenewal(talk.renewalArtist, sheet, out message);
			PendingRenewal = null;
		} else {
			FinalizeSigning(talk.prospect, sheet, out message);
			talk.prospect.Talk = null;
		}
		Changed?.Invoke();
	}

	/// <summary>Voluntary and forced walks land very differently depending on what was on the table.
	/// A new signing that falls through just costs a cooldown -- there was never a deal to lose. A
	/// renewal that falls through for good (patience exhausted) means the act actually leaves: the old
	/// paper has already run out, so there is nothing left holding them to the roster.</summary>
	private void WalkAway(ContractTalk talk, bool forced, out string message) {
		talk.stage = ContractTalkStage.Done;
		SimulatedArtist artist = talk.Artist;

		if (talk.IsRenewal) {
			if (forced) {
				int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
				ArtistManager.Instance?.DropArtist(artist, year, Label, ArtistDropReason.ContractExpired);
				maturedNotified.Remove(artist.artistId);
				Note($"{artist.stageName} walked -- the deal fell apart for good and they're off the roster.");
				message = $"{artist.stageName} is gone. Talks broke down for good.";
			} else {
				Note($"You stepped back from {artist.stageName}'s renewal for now. The old terms hold.");
				message = "You step back. The old terms hold until you try again.";
			}
			PendingRenewal = null;
		} else {
			if (forced) {
				GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
				talk.prospect.CooldownUntil = today.AddDays(ForcedWalkCooldownDays);
				Note($"{artist.stageName} walked from the table. Give it time before you go back.");
				message = $"{artist.stageName} isn't signing today.";
			} else {
				Note($"You stepped back from the table with {artist.stageName}. Nothing burned.");
				message = "You step back. Nothing's burned -- come back whenever.";
			}
			talk.prospect.Talk = null;
		}
		Changed?.Invoke();
	}

	/// <summary>Voluntary walk from the UI's own button -- same resolution as an in-scene Walk counter.</summary>
	public bool WalkFromTalk(ContractTalk talk, out string message) {
		message = "";
		if (talk == null) { message = "No negotiation open."; return false; }
		WalkAway(talk, forced: false, out message);
		return true;
	}

	// ========================================================================================
	// THE FOGGED READ
	// ========================================================================================

	/// <summary>Same discipline as ScoutingPerception: a pure stable hash, never GD.Rand*, so the
	/// read is reproducible within a round and desyncs nothing. Fogs how precisely the objection's
	/// size is reported -- the axis itself is always the true worst one; what a bad scout loses is
	/// the number, not the subject.</summary>
	private static float StableNegotiationUnit(string labelId, string artistId, int round) {
		const ulong offset = 14695981039346656037UL;
		const ulong prime = 1099511628211UL;
		ulong hash = offset;
		foreach (char c in $"{labelId}|{artistId}|{round}|NegotiationReadV1") { hash ^= c; hash *= prime; }
		return (hash >> 40) * (1f / 16777216f);
	}

	private float NegotiationFogBand() => Mathf.Lerp(0.30f, 0.10f, Mathf.Clamp(Label?.scoutingAbility ?? 0.5f, 0f, 1f));

	private string ObjectionLine(ContractTalk talk, ContractTermSheet offer) {
		ContractAxis axis = talk.objectionAxis ?? ContractAxis.Advance;
		string manager = talk.ask.ManagerName;
		string who = string.IsNullOrEmpty(manager) ? "They" : manager;

		float trueGap = Mathf.Max(0f, 1f - AxisTerm(axis, offer, talk.ask));
		float unit = StableNegotiationUnit(Label?.labelId ?? "", talk.Artist.artistId, talk.roundsPlayed);
		float band = NegotiationFogBand();
		float perceivedGap = Mathf.Max(0f, trueGap * (1f + (unit * 2f - 1f) * band));
		bool sharp = (Label?.scoutingAbility ?? 0.5f) >= 0.6f;

		string sizeWord = perceivedGap switch {
			< 0.15f => "a little",
			< 0.35f => "a fair bit",
			_ => "a long way",
		};

		return axis switch {
			ContractAxis.Advance => sharp
				? $"\"{who} wants real money up front. You're {perceivedGap * 100f:F0}% short of where they'd sign.\""
				: $"\"{who} wants more money up front -- {sizeWord} more than you're offering.\"",
			ContractAxis.Royalty => sharp
				? $"\"The points are the problem. {offer.RoyaltyRate:P1} isn't going to cut it -- you're {sizeWord} off.\""
				: "\"It's the percentage that's sticking.\"",
			ContractAxis.Term => "\"It's the length of the deal. That's too long a leash for them.\"",
			ContractAxis.Deliverables => offer.SinglesObligation > talk.ask.SinglesObligation
				? "\"That's a lot of sides to owe you. They don't want to be grinding out product just to stay clear of you.\""
				: "\"They want more guaranteed shots at the market than that -- fewer sides means fewer chances at a hit.\"",
			ContractAxis.Publishing => "\"They'll come down on the money, but they want to keep the publishing.\"",
			ContractAxis.CreativeControl => "\"They want the final word on what gets cut. That's the sticking point.\"",
			_ => "\"Something in there doesn't sit right with them.\"",
		};
	}

	// ========================================================================================
	// SHARED SIGNING
	// ========================================================================================

	/// <summary>Everything that actually happens when a NEW contract is signed, shared by the
	/// Pushover path (<see cref="OfferContract"/>, which spends the flat SignHours up front) and a
	/// negotiated close (<see cref="FinalizeSign"/>, where the hours were already spent one round at
	/// a time).</summary>
	private void FinalizeSigning(Prospect prospect, ContractTermSheet sheet, out string message) {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		float paid = Label.SignArtist(prospect.Artist, year, sheet);
		CompetitorManager.Instance?.RecordExpense(Label, paid);
		ArtistManager.Instance?.SignArtist(prospect.Artist, Label.labelId, year);
		Label.SetOperatingRosterTarget(Label.CurrentRosterSize, LabelOperatingTargetReason.OrganicGrowth, week);
		repertoire[prospect.Artist.artistId] = new List<RepertoireItem>(prospect.LiveSet);
		generatedProspectIds.Remove(prospect.Artist.artistId);
		slate.Remove(prospect);

		Note($"Signed {prospect.Artist.stageName} -- ${paid:N0} advance, {sheet.RoyaltyRate:P0} royalty, {sheet.TermYears}yr" +
			$"{(sheet.LabelOwnsPublishing ? "" : ", artist keeps publishing")}.");
		message = $"Signed {prospect.Artist.stageName}.";
	}

	// ========================================================================================
	// RENEWAL -- an already-signed act's matured contract
	// ========================================================================================

	/// <summary>
	/// Opens a renewal for a matured contract on the player's own roster. The ask is generated fresh
	/// off the act's CURRENT stats and manager (<see cref="AILabel.GenerateTermSheet"/>), so a Star
	/// who broke out under you asks like one and a Shark who signed on since the original deal shows
	/// up at the table -- no separate "success raises the ask" formula needed, it already does.
	/// </summary>
	public bool ApproachRenewal(SimulatedArtist artist, out string message) {
		message = "";
		if (artist == null || Label?.roster == null || !Label.roster.Contains(artist)) {
			message = "That act isn't on your roster.";
			return false;
		}
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		if (!RosterManager.IsContractMatured(artist, year, week)) {
			message = $"{artist.stageName}'s deal still has time on it.";
			return false;
		}

		ContractTermSheet ask = Label.GenerateTermSheet(artist, year);
		NegotiationPosture posture = PostureOf(artist);
		var offer = new RenewalOffer { Artist = artist, Ask = ask, Posture = posture };
		if (posture != NegotiationPosture.Pushover) {
			ContractTalk talk = BuildTalk(artist, ask, posture);
			talk.renewalArtist = artist;
			offer.Talk = talk;
		}
		PendingRenewal = offer;

		message = posture != NegotiationPosture.Pushover
			? $"{artist.stageName} wants to talk terms for the new deal. {ask.DemandSummary}"
			: string.IsNullOrEmpty(ask.DemandSummary) ? $"Put new paper in front of {artist.stageName}." : ask.DemandSummary;
		Changed?.Invoke();
		return true;
	}

	/// <summary>The Pushover renewal: set the terms, put new paper in front of them, done. Cheaper
	/// than a first signing (<see cref="NegotiationRoundHours"/>, not the full <see cref="SignHours"/>)
	/// -- you already know this act, this is paperwork, not due diligence.</summary>
	public bool RenewContract(SimulatedArtist artist, float advance, float royaltyRate, int termYears, int singlesObligation,
			bool labelOwnsPublishing, bool artistCreativeControl, out string message) {
		message = "";
		if (PendingRenewal == null || PendingRenewal.Artist != artist) { message = "Approach them about the renewal first."; return false; }
		if (PendingRenewal.Posture != NegotiationPosture.Pushover) {
			message = "They want to talk terms, not just sign -- work it through the negotiation.";
			return false;
		}
		if (!Require(NegotiationRoundHours, out message)) return false;

		advance = Mathf.Max(0f, advance);
		if (!Label.CanAffordToSign(advance)) {
			message = $"You can't cover a ${advance:N0} advance and hold next month's overhead.";
			return false;
		}

		ContractTermSheet ask = PendingRenewal.Ask;
		var sheet = new ContractTermSheet(advance, Mathf.Clamp(royaltyRate, PlayerRoyaltyFloor, 0.15f),
			Mathf.Clamp(termYears, 1, 7), Mathf.Clamp(singlesObligation, 0, 30), labelOwnsPublishing, artistCreativeControl,
			ask.NegotiationDifficulty, ask.Manager, ask.ManagerName, ask.DemandSummary);

		Spend(NegotiationRoundHours);
		FinalizeRenewal(artist, sheet, out message);
		PendingRenewal = null;
		Changed?.Invoke();
		return true;
	}

	/// <summary>The write path for a successful renewal -- mirrors RosterManager's own AI re-sign
	/// branch (new advance paid, term/expiry/obligation reset, releases-under-this-deal zeroed), plus
	/// the two axes only the player's negotiation actually touches (publishing, creative control).</summary>
	private void FinalizeRenewal(SimulatedArtist artist, ContractTermSheet sheet, out string message) {
		int year = TimeManager.Instance?.CurrentDate.year ?? 1960;
		int currentWeek = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		artist.unrecoupedAdvance = sheet.Advance;
		artist.contractLength = sheet.TermYears;
		artist.contractExpiresYear = year + sheet.TermYears;
		artist.contractExpiresWeek = currentWeek + sheet.TermYears * 52;
		artist.contractSinglesObligation = sheet.SinglesObligation;
		artist.contractReleases = 0;
		artist.royaltyRate = sheet.RoyaltyRate;
		artist.labelOwnsPublishing = sheet.LabelOwnsPublishing;
		artist.artistCreativeControl = sheet.ArtistCreativeControl;
		CompetitorManager.Instance?.RecordExpense(Label, sheet.Advance);
		artist.careerEvents.Add($"{year}: Re-signed with {Label.labelName} (${sheet.Advance:N0} advance, {sheet.TermYears}yr" +
			(sheet.SinglesObligation > 0 ? $", {sheet.SinglesObligation} sides)" : ")"));
		maturedNotified.Remove(artist.artistId);

		Note($"Renewed {artist.stageName} -- ${sheet.Advance:N0} advance, {sheet.RoyaltyRate:P0} royalty, {sheet.TermYears}yr" +
			$"{(sheet.LabelOwnsPublishing ? "" : ", artist keeps publishing")}.");
		message = $"Renewed {artist.stageName}.";
	}

	/// <summary>Monthly nag for a matured contract still sitting unrenewed -- once per spell, not
	/// once a month forever. Nothing forces the player's hand; RosterManager already skips the
	/// player's roster for expiration ("Renewals and drops on the player's roster are the player's
	/// calls"), so an unrenewed act just keeps working under the old terms until you act.</summary>
	private void CheckForMaturedContracts(int year) {
		if (Label?.roster == null) return;
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		foreach (SimulatedArtist artist in Label.roster) {
			if (!RosterManager.IsContractMatured(artist, year, week)) { maturedNotified.Remove(artist.artistId); continue; }
			if (maturedNotified.Add(artist.artistId))
				Note($"{artist.stageName}'s contract is up. Renew it from the roster whenever you're ready.");
		}
	}

	// ========================================================================================
	// PICKING UP A MANAGER MID-CONTRACT
	// ========================================================================================

	// How likely an eligible act is to attract a manager in a given month, at full interest (1.0).
	// Scaled down by how much heat they're actually carrying, so a barely-Rising act is a long shot
	// and a hot Star is a near-certainty within a year or two.
	private const float ManagerApproachMonthlyChance = 0.15f;

	/// <summary>
	/// Managers were only ever stamped once, at generation (see ArtistManager.RollManagerArchetype's
	/// doc comment) -- there was no route for a signed act to pick one up after the fact, which meant
	/// an act that broke out under the player stayed permanently unmanaged. This is that route: each
	/// month, an unmanaged act carrying real heat has a chance of drawing interest, using the exact
	/// same quality-correlated table real managers are rolled from at birth. Player-roster only, and
	/// gated behind ManagerSystem.Enabled -- a "--disable-managers" run should never grow one either.
	/// </summary>
	private void CheckForManagerInterest() {
		if (!ManagerSystem.Enabled || Label?.roster == null) return;
		foreach (SimulatedArtist artist in Label.roster) {
			if (artist.manager != ManagerArchetype.None) continue;
			if (!IsActivelyOnCareerTrack(artist.careerState)) continue;
			float interest = Mathf.Clamp(artist.momentum * 0.5f + artist.reputation * 0.5f, 0f, 1f);
			if (interest <= 0f || GD.Randf() > interest * ManagerApproachMonthlyChance) continue;

			ManagerArchetype archetype = ArtistManager.Instance?.RollManagerArchetypeFor(artist) ?? ManagerArchetype.None;
			if (archetype == ManagerArchetype.None) continue;   // rolled "still nobody's biting" this pass
			artist.manager = archetype;
			artist.managerName = GenerateManagerNameFor();
			Note($"{artist.stageName} picked up a manager: {artist.managerName ?? "somebody"} ({archetype}).");
		}
	}

	// CareerState is not ordinally safe past Superstar (Declining/Dropped/Disbanded/Retired sort
	// higher), so this is an explicit allow-list rather than a >= comparison.
	private static bool IsActivelyOnCareerTrack(CareerState state) =>
		state is CareerState.Rising or CareerState.Established or CareerState.Star or CareerState.Superstar;

	private static string GenerateManagerNameFor() {
		if (NameGenerator.Instance == null) return null;
		(string first, string last) = NameGenerator.Instance.GeneratePersonName(GD.Randf() < 0.9f);
		return $"{first} {last}";
	}
}
