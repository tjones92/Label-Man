using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Recognizable publicity tendencies (directive §13). Derived from the label and the act; never a weekly reroll.</summary>
public enum MediaPolicy { TeenPopCultivation, CommercialPragmatism, ArtisticProtection, SpecialistCommunity, OpportunisticPublicity, CautiousCrisis }

/// <summary>
/// An AI campaign's existing promotion envelope, viewed as allocations (§12.2): radio + TV + press + other + reserve equals
/// the envelope exactly, in cents. In observe mode it is an ESTIMATE of the amount the release charge already covered:
/// nothing is debited, the charge in CompetitorManager stays the accounting authority, and reservations against the TV and
/// press lines are receipts, once each, never a second payment.
/// </summary>
public sealed class AiMediaAllocation {
	public string RecordId, LabelId, ArtistId;
	public int Day;
	public MediaPolicy Policy;
	public long EnvelopeCents, RadioCents, TvCents, PressCents, OtherCents, ReserveCents;
	public long TvReservedCents, PressReservedCents;
	/// <summary>Reservation receipts: one per booking or sheet, keyed so a re-read can never reserve twice.</summary>
	public Dictionary<string, long> Receipts = new(StringComparer.Ordinal);
	public int Declined, Accepted;
	public long TvRemaining => TvCents - TvReservedCents;
	public long PressRemaining => PressCents - PressReservedCents;
	public bool Balanced => RadioCents + TvCents + PressCents + OtherCents + ReserveCents == EnvelopeCents && RadioCents >= 0 && ReserveCents >= 0;
}

/// <summary>An AI label's approach to a producer or a desk, from the weekly planner (§13). Same outlets, same slots, same rules as the player.</summary>
public sealed class AiMediaPitch {
	public string Id, LabelId, RecordId, ArtistId, ArtistName, ProgramId, OutletId;
	public PitchKind Kind;
	public MediaPolicy Policy;
	public int Day, ExpiresDay;
	public PitchStatus Status;
}

/// <summary>
/// The bounded AI publicity planner's rules (directive §13, Phase 3, observe-only). Policy comes from the label's archetype
/// and, per act, its manager, creative control and any public controversy. Allocation reads the label's existing
/// <see cref="AILabel.GetMarketingBudget"/> (a pure read) and splits it; costs are the player's own estimate rules, so an
/// AI label too poor for a cross-country trip declines it and takes a cheaper date. No RNG stream, no writes outside media.
/// </summary>
public static class AiMediaPlanner {
	public const int FocusRecordsPerLabel = 2;
	public const int PitchLifeDays = 14;

	public static MediaPolicy LabelPolicy(AILabel label) => label?.archetype switch {
		LabelArchetype.TeenHitMachine or LabelArchetype.SoulFactory => MediaPolicy.TeenPopCultivation,
		LabelArchetype.CorporateGiant => MediaPolicy.CommercialPragmatism,
		LabelArchetype.FolkBoutique or LabelArchetype.JazzPrestige => MediaPolicy.ArtisticProtection,
		LabelArchetype.BluesRoots or LabelArchetype.CountrySpecialist or LabelArchetype.GospelPowerhouse => MediaPolicy.SpecialistCommunity,
		LabelArchetype.RockRebel or LabelArchetype.RegionalHustler => MediaPolicy.OpportunisticPublicity,
		_ => MediaPolicy.CommercialPragmatism,
	};

	/// <summary>The policy for one act: a live public controversy makes any house cautious; a Visionary manager or an
	/// ambitious act with its own creative control protects the work whatever the label's habit.</summary>
	public static MediaPolicy PolicyFor(AILabel label, SimulatedArtist artist, bool inControversy) {
		if (inControversy) return MediaPolicy.CautiousCrisis;
		MediaPolicy policy = LabelPolicy(label);
		if (artist != null && (artist.manager == ManagerArchetype.Visionary || artist.artistCreativeControl && (artist.evolution?.artisticAmbition ?? 0f) > 0.65f))
			return MediaPolicy.ArtisticProtection;
		return policy;
	}

	/// <summary>Initial design shares of the envelope: (radio, TV, press, other); reserve takes the remainder.</summary>
	public static (float Radio, float Tv, float Press, float Other) Shares(MediaPolicy policy) => policy switch {
		MediaPolicy.TeenPopCultivation => (0.40f, 0.22f, 0.14f, 0.14f),
		MediaPolicy.CommercialPragmatism => (0.50f, 0.18f, 0.10f, 0.12f),
		MediaPolicy.ArtisticProtection => (0.45f, 0.10f, 0.15f, 0.15f),
		MediaPolicy.SpecialistCommunity => (0.55f, 0.08f, 0.12f, 0.15f),
		MediaPolicy.OpportunisticPublicity => (0.45f, 0.15f, 0.15f, 0.10f),
		_ => (0.50f, 0.05f, 0.05f, 0.10f),
	};

	/// <summary>Split an envelope into whole cents that sum to it exactly (§12.2; fixture 14).</summary>
	public static AiMediaAllocation Allocate(string recordId, string labelId, string artistId, MediaPolicy policy, float envelopeDollars, int day) {
		long envelope = Math.Max(0L, (long)Math.Round(envelopeDollars * 100.0));
		var (radio, tv, press, other) = Shares(policy);
		var a = new AiMediaAllocation {
			RecordId = recordId, LabelId = labelId, ArtistId = artistId, Policy = policy, Day = day, EnvelopeCents = envelope,
			RadioCents = (long)Math.Floor(envelope * (double)radio), TvCents = (long)Math.Floor(envelope * (double)tv),
			PressCents = (long)Math.Floor(envelope * (double)press), OtherCents = (long)Math.Floor(envelope * (double)other),
		};
		a.ReserveCents = envelope - a.RadioCents - a.TvCents - a.PressCents - a.OtherCents;
		return a;
	}

	/// <summary>The campaign envelope estimate: what the label's release charge budgets for this act (pure read).</summary>
	public static float EnvelopeEstimate(AILabel label, SimulatedArtist artist) => label == null || artist == null ? 0f : Math.Max(0f, label.GetMarketingBudget(artist));

	public static long PreparationCents(MediaPreparation prep) => prep switch { MediaPreparation.Extra => 4000, MediaPreparation.Minimal => 0, _ => 1500 };

	/// <summary>
	/// What a date costs the label in travel and preparation: the same rules the player's offer prints (fares by distance,
	/// hotel nights per member). A date in the act's own town costs only the rehearsal.
	/// </summary>
	public static long AppearanceCents(TvProgram program, GameDate date, string actCityId, int members, MediaPreparation prep) {
		long cents = PreparationCents(prep);
		// A network production brings its guests in: fares and hotel are the program's cost, not the label's. A local
		// show pays nobody's way, so a date in a distant town costs the label the trip.
		if (program.Network != TvNetwork.Local) return cents;
		string where = program.HomeCityId;
		if (where == null || actCityId == null || where == actCityId) return cents;
		return cents + TravelCents(DistanceModel.GetRoadMilesBetween(actCityId, where), members);
	}

	/// <summary>
	/// Getting the act there and back (initial design values, shared with the player's offer note): a day trip by car under
	/// 150 miles, a car and one night up to 600, fares and two nights beyond that.
	/// </summary>
	public static long TravelCents(float miles, int members) {
		if (miles <= 0f) return 0;
		int people = Math.Clamp(members, 1, 8);
		int rooms = (people + 1) / 2; // two to a room on the road
		double dollars = miles <= 150 ? CarPerMile * miles * 2
			: miles <= 600 ? CarPerMile * miles * 2 + TravelNights(miles) * PlayerDesk.HotelNightly * rooms
			: FarePerMile * miles * 2 * people + TravelNights(miles) * PlayerDesk.HotelNightly * rooms;
		return (long)Math.Round(dollars * 100.0);
	}

	public const float CarPerMile = 0.06f, FarePerMile = 0.05f;
	public static int TravelNights(float miles) => miles <= 150 ? 0 : miles <= 600 ? 1 : 2;

	public static long PressSheetCents => 150; // a promo copy, a photograph, postage

	/// <summary>
	/// Whether the act and its people take a date (§13 policy, §7.3 consent): a specialist house doesn't chase a national
	/// teen slot for a record that isn't teen music; an artistic act sometimes refuses to mime for the dancers; a house in
	/// crisis keeps its act off live family television. Keyed per program, act and date: re-reading never changes it.
	/// </summary>
	public static bool Considers(TvProgram program, MediaPolicy policy, SimulatedArtist artist, Genre genre, int day, out string reason) {
		reason = null;
		float teen = MediaOutletCatalog.TeenFit(genre);
		float u = MediaAtmosphere.Unit("ai-consider|" + program.Id + "|" + (artist?.artistId ?? "?") + "|" + day);
		switch (policy) {
			case MediaPolicy.SpecialistCommunity when program.Kind == TvProgramKind.NationalTeen && teen < 0.8f:
				reason = "policy"; return false;
			case MediaPolicy.ArtisticProtection when program.Method == PerformanceMethod.Mime && program.Kind != TvProgramKind.PrimetimeVariety && u < 0.4f:
				reason = "image"; return false;
			case MediaPolicy.CautiousCrisis when program.Network != TvNetwork.Local && program.Kind == TvProgramKind.PrimetimeVariety && u < 0.6f:
				reason = "crisis"; return false;
		}
		// The act's own say (§13 artist consent): a restless, ambitious act with control can refuse a pin-up date.
		ArtistEvolutionProfile d = artist?.evolution;
		if (artist?.artistCreativeControl == true && d != null && program.Kind != TvProgramKind.PrimetimeVariety && program.Method == PerformanceMethod.Mime
			&& d.artisticAmbition > 0.7f && d.commercialPragmatism < 0.4f && MediaAtmosphere.Unit("ai-consent|" + program.Id + "|" + artist.artistId + "|" + day) < 0.5f) {
			reason = "consent"; return false;
		}
		return true;
	}

	/// <summary>The programs a policy aims its pitches at, best first (§13): a capped shortlist, never every show.</summary>
	public static float PitchAppeal(TvProgram program, MediaPolicy policy, Genre genre, int chartPosition) {
		float fit = program.GenreFit(genre);
		if (fit < 0.5f) return 0f;
		float kind = program.Kind switch {
			TvProgramKind.LocalTeenDance => policy is MediaPolicy.TeenPopCultivation or MediaPolicy.OpportunisticPublicity ? 0.8f : 0.6f,
			TvProgramKind.NationalTeen => policy == MediaPolicy.TeenPopCultivation ? 1.2f : policy == MediaPolicy.SpecialistCommunity ? 0.2f : 0.8f,
			_ => policy is MediaPolicy.CommercialPragmatism or MediaPolicy.ArtisticProtection ? 1.1f : policy == MediaPolicy.CautiousCrisis ? 0.2f : 0.8f,
		};
		// A network booker wants the chart: a pitch from far down it is wasted postage, and a planner knows that much.
		if (program.Network != TvNetwork.Local && (chartPosition <= 0 || chartPosition > program.ChartCeiling + 20)) kind *= 0.15f;
		return fit * kind;
	}

	/// <summary>Which press family a policy writes to first (§13): teen editors, the trades, the critics, the community papers.</summary>
	public static PressFamily[] PressPreference(MediaPolicy policy) => policy switch {
		MediaPolicy.TeenPopCultivation => new[] { PressFamily.TeenFan, PressFamily.Trade },
		MediaPolicy.CommercialPragmatism => new[] { PressFamily.Trade, PressFamily.LocalDaily },
		MediaPolicy.ArtisticProtection => new[] { PressFamily.RockCriticism, PressFamily.Specialist },
		MediaPolicy.SpecialistCommunity => new[] { PressFamily.Specialist, PressFamily.LocalDaily },
		MediaPolicy.OpportunisticPublicity => new[] { PressFamily.LocalDaily, PressFamily.TeenFan },
		_ => new[] { PressFamily.Trade },
	};

	/// <summary>A teen editor asks a rival label for photographs (§8.2). The answer follows the policy and the act's own
	/// willingness: "exclusive", "supplied" or "declined". A commercial house overuses the pin-up; an artistic act says no.</summary>
	public static string PhotoAnswer(MediaPolicy policy, SimulatedArtist artist, string key) {
		float u = MediaAtmosphere.Unit("ai-photo|" + key);
		float ambition = artist?.evolution?.artisticAmbition ?? 0.5f;
		return policy switch {
			MediaPolicy.TeenPopCultivation => u < 0.55f ? "exclusive" : "supplied",
			MediaPolicy.CommercialPragmatism or MediaPolicy.OpportunisticPublicity => u < 0.85f ? "supplied" : "declined",
			MediaPolicy.ArtisticProtection => u < 0.35f + 0.3f * (1f - ambition) ? "supplied" : "declined",
			MediaPolicy.SpecialistCommunity => u < 0.6f ? "supplied" : "declined",
			_ => "declined",
		};
	}
}
