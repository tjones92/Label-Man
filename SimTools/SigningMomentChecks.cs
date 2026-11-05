using System;
using System.Linq;
using Godot;

/// <summary>The signing moment and the easy act's push-back (PlayerDesk.Signing.cs): the push-back only fires between
/// the floor and the ask, holding firm still signs, the first signing leads the paper with a catalogue number, and
/// each moment is handed to the UI once. Run with --signing-moment-check.</summary>
public static class SigningMomentChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SIGNING_MOMENT_CHECK_FAILED: " + why); checks++; }
		var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;

		Check(PlayerDesk.CatalogPrefix("Lantern Records") == "LR", "prefix from two words");
		Check(PlayerDesk.CatalogPrefix("The Blue Bayou Recording Co.") == "BBR", "prefix skips The/Co. and caps at three");
		Check(PlayerDesk.CatalogPrefix("") == "LBL", "blank prefix falls back");

		Check(desk.FoundLabel("Signing Moment Records", "memphis", out string message), "found label: " + message);
		clock.RestoreClock(GameDate.StartDate, 9);
		var next = desk.ScoutingItinerary().First();
		if (!desk.WaitForSceneAppearance(next.BillId, next.EventId, out message) ||
			!desk.ListenSceneAppearance(next.BillId, next.EventId, out message)) throw new InvalidOperationException(message);
		var act = ArtistManager.Instance.GetArtist(next.ArtistId);
		act.labelId = null; act.careerState = CareerState.Unsigned; act.prospectMarketStatus = ProspectMarketStatus.Seeking;
		act.lifecycleStatus = ArtistLifecycleStatus.Active; act.isActive = true; act.manager = ManagerArchetype.None;
		desk.Label.cashReserves = 100000;
		clock.RestoreClock(clock.CurrentDate, 9);
		if (!desk.OpenScoutingDossier(act.artistId, out message)) throw new InvalidOperationException(message);
		var prospect = desk.Slate.Single(p => p.Artist == act);
		if (!desk.ApproachToSign(prospect, out message)) throw new InvalidOperationException(message);
		prospect.Posture = NegotiationPosture.Pushover; prospect.Talk = null;
		var ask = prospect.Baseline;
		Check(ask.Advance >= 10f, "the act asks for an advance worth haggling over: " + ask.Advance);

		ContractTermSheet At(float advanceShare) => new(Mathf.Ceil(ask.Advance * advanceShare), ask.RoyaltyRate, ask.TermYears,
			ask.SinglesObligation, ask.LabelOwnsPublishing, ask.ArtistCreativeControl, ask.NegotiationDifficulty, ask.Manager, ask.ManagerName, ask.DemandSummary);

		Check(desk.PushbackFor(prospect, At(1f)) == null, "no push-back at the ask");
		Check(desk.PushbackFor(prospect, At(0.8f)) == null, "no push-back 20% under");
		Check(desk.PushbackFor(prospect, At(0.05f)) == null, "far under the floor goes to the existing counter, not a push-back");
		var lowball = At(0.55f);
		var bump = desk.PushbackFor(prospect, lowball);
		Check(bump.HasValue, "push-back between the floor and 35% under");
		Check(bump.Value.Advance > lowball.Advance && bump.Value.Advance <= ask.Advance && bump.Value.Advance % 5f == 0f,
			$"bump lands between the offer and the ask on a $5 step: {lowball.Advance} -> {bump.Value.Advance} (ask {ask.Advance})");
		Check(desk.PushbackLine(prospect, lowball, bump.Value).Contains($"${bump.Value.Advance:N0}"), "the line names the bump");

		// Holding firm still signs: the push-back is a question, not a refusal.
		Check(desk.OfferContract(prospect, lowball.Advance, lowball.RoyaltyRate, lowball.TermYears, lowball.SinglesObligation,
			lowball.LabelOwnsPublishing, lowball.ArtistCreativeControl, out message), "holding at the lowball signs: " + message);
		var moment = desk.TakeSigningMoment();
		Check(moment != null && moment.ArtistId == act.artistId, "the signing leaves a moment for the UI");
		Check(moment.First && moment.CatalogNumber == "SMR-001", "first signing is SMR-001: " + moment.CatalogNumber);
		Check(!string.IsNullOrEmpty(moment.Handshake) && moment.Handshake.Contains("renewal"), "a cheap deal's handshake remembers it: " + moment.Handshake);
		Check(!string.IsNullOrEmpty(moment.Reaction), "a lowball gets a pause before the pen");
		Check(desk.TakeSigningMoment() == null, "each moment is handed over once");
		Check(desk.CatalogNumberFor(act.artistId) == "SMR-001", "the number sticks to the act");
		string story = desk.Log.FirstOrDefault(l => l.Contains("FIRST SIGNING:"));
		Check(story != null && story.Contains(act.stageName) && story.Contains("SMR-001"), "the first signing is a story in the log");
		Check(PlayerDesk.MorningStoryRank(story) == 0, "the first signing leads the paper");

		GD.Print($"SIGNING_MOMENT_CHECK_PASS checks={checks} push {lowball.Advance}->{bump.Value.Advance} of {ask.Advance}; handshake: {moment.Handshake}");
	}
}
