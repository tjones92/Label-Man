using System.Collections.Generic;
using System.Linq;
using Godot;

// The origination engine for externally-sourced albums (film soundtracks, stage-cast recordings,
// film-song tie-ins). This is the ONE genuinely new subsystem the soundtrack work adds: soundtracks
// must NOT be born from the artist-album format fork (that was the old cosmetic AlbumFormat.Soundtrack
// dice roll, now removed). Instead, a few times a year an opportunity is generated, a capable label is
// picked to license it, and CompetitorManager mints + releases the resulting Soundtrack album.
//
// This class holds the pure decision logic (profile generation, genre mapping, label selection). The
// engine mutation -- record minting, cash spend, ChartManager.ReleaseRecord -- stays in
// CompetitorManager, which owns the record counter and the label roster. See
// SimTools/D7SoundtrackCastAlbumHandoff.md §3.2, §3.5, §5.
public static class ExternalMediaService {
	// Calibration levers (tuned to the ~7-14% year-end album-chart share target, decade-run scored).
	// Origination rate is per YEAR; the weekly hook converts it to a per-week probability. Soundtracks
	// live long (the gentle Soundtrack catalog-decay branch), so a modest annual rate sustains a healthy
	// standing chart population. FIRST GUESS -- calibrate on a decade run against the share target.
	// 14 -> 22: the two-seed decade run landed soundtrack share at 2-7% against a 13-25% target
	// (under-scaled ~3x). Raising origination ~1.6x, together with the longer class life (box-office
	// floor lift + blockbuster ultra-tail), pushes standing soundtrack presence toward the target band.
	public const float OriginationsPerYear = 22f;
	// ERA RAMP (2026-08): the flat 22 left share UNDER early (6% vs ~13) AND fading late (2.6% vs ~6),
	// because the album chart grows to ~200 slots by 1969 and a flat origination dilutes against the
	// growing denominator. A first ramp 28->46 fixed the early/mid band (12-15% through 1966) but late
	// still faded (7/4.7/2.8% at 1967-69) -- the late chart grows faster than a 46/yr feed. End raised
	// 46 -> 60 to hold late share; the blockbuster cap still guards against monoculture. Flat const kept
	// for reference/probes; the weekly loop uses this.
	public static float OriginationsForYear(int year) {
		float t = Mathf.Clamp((year - 1960f) / 9f, 0f, 1f);
		return Mathf.Lerp(28f, 60f, t);
	}
	// Hard anti-monoculture cap: at most this many genuine blockbusters across a decade (handoff §3.2).
	public const int BlockbusterDecadeCap = 3;

	// Source-type mix. StageCast + FilmScore dominate the early-60s album chart (Broadway cast albums,
	// orchestral film scores); FilmSong (beach/rock pictures) is a mid-60s phenomenon. Kept flat here;
	// an era ramp is a later refinement if the genre mix needs it.
	public static ExternalMediaSourceType RollSourceType() {
		float r = GD.Randf();
		if (r < 0.40f) return ExternalMediaSourceType.StageCast;
		if (r < 0.75f) return ExternalMediaSourceType.FilmScore;
		return ExternalMediaSourceType.FilmSong;
	}

	// Soundtracks are a VESSEL for a real genre, so they appear under both that genre in
	// genre-decade-shape AND under Soundtrack in the album chart (handoff §3.1).
	public static Genre MapGenre(ExternalMediaSourceType sourceType) {
		switch (sourceType) {
			case ExternalMediaSourceType.StageCast:
				return GD.Randf() < 0.65f ? Genre.TraditionalPop : Genre.Comedy;
			case ExternalMediaSourceType.FilmScore:
				return GD.Randf() < 0.60f ? Genre.Classical : Genre.EasyListening;
			case ExternalMediaSourceType.FilmSong:
			default:
				float r = GD.Randf();
				return r < 0.45f ? Genre.RockAndRoll : r < 0.80f ? Genre.SurfRock : Genre.FolkRock;
		}
	}

	// Roll one immutable profile with correlated stats. `allowBlockbuster` is false once the decade cap
	// is hit; the vast majority of profiles are mid-tier B-movies / forgotten stage flops regardless.
	public static ExternalMediaProfile GenerateProfile(int year, bool allowBlockbuster) {
		var profile = new ExternalMediaProfile { sourceType = RollSourceType() };

		// Prestige axis: critical standing trades off against youth pull, so a prestige picture is not
		// also a mass-teen smash (and vice versa). This is the primary correlation guard.
		float prestige = GD.Randf();
		profile.criticalPrestige = prestige;
		profile.youthAppeal = Mathf.Clamp(1f - prestige + (float)GD.RandRange(-0.15, 0.15), 0f, 1f);

		bool blockbuster = allowBlockbuster && GD.Randf() < 0.035f;
		profile.isBlockbuster = blockbuster;
		if (blockbuster) {
			profile.boxOfficeTrajectory = (float)GD.RandRange(0.82, 1.0);
			profile.sourcePopularity = (float)GD.RandRange(0.78, 0.95);
			profile.castStarDraw = (float)GD.RandRange(0.6, 1.0);
			profile.studioPromotion = (float)GD.RandRange(1.5, 2.0);
			profile.awardsPrestige = Mathf.Clamp(prestige * (float)GD.RandRange(0.7, 1.0), 0f, 1f);
			profile.licenseSkim = (float)GD.RandRange(0.60, 0.80);
			profile.upfrontLicenseFee = LicenseFeeProductionMultiple(profile); // multiple; resolved at mint
		} else {
			// The long tail: modest scores, B-movies, stage flops -- but a licensed release that a label
			// paid to press is a mid-tier proposition, not mostly zeroes. Skew toward the low-middle, not
			// the floor (the first-pass pow(.,2.2) squash put the whole population near bo=0 and, with the
			// box-office decay, made every modest soundtrack die in ~2wk).
			float bo = Mathf.Pow(GD.Randf(), 1.3f); // mild squash toward low-middle
			profile.boxOfficeTrajectory = Mathf.Clamp(0.20f + bo * 0.60f, 0f, 1f);
			profile.sourcePopularity = Mathf.Clamp(0.25f + profile.boxOfficeTrajectory * 0.45f + (float)GD.RandRange(-0.08, 0.12), 0.1f, 0.9f);
			profile.castStarDraw = (float)GD.RandRange(0.05, 0.55);
			profile.studioPromotion = (float)GD.RandRange(0.7, 1.3);
			profile.awardsPrestige = Mathf.Clamp(prestige * profile.boxOfficeTrajectory * (float)GD.RandRange(0.3, 0.8), 0f, 1f);
			profile.licenseSkim = (float)GD.RandRange(0.40, 0.65);
			profile.upfrontLicenseFee = LicenseFeeProductionMultiple(profile);
		}
		return profile;
	}

	// The upfront license advance, expressed as a MULTIPLE of the licensing label's own album
	// production cost (resolved at mint time). Blockbuster deals carry a large advance that gates
	// Small/Boutique labels off the top tier; they can still catch a cheap indie sleeper.
	public static float LicenseFeeProductionMultiple(ExternalMediaProfile profile) =>
		profile.isBlockbuster ? (float)GD.RandRange(3.5, 5.5)
			: Mathf.Lerp(0.8f, 2.4f, profile.boxOfficeTrajectory);

	// Rank capable labels by reputation + capital + roster reach and pick the licensee. Blockbusters
	// require a label that can actually front the advance, so small labels are gated off that tier but
	// remain eligible for cheaper opportunities. Returns null when nobody can afford the deal.
	public static AILabel SelectLabel(IReadOnlyList<AILabel> labels, ExternalMediaProfile profile) {
		if (labels == null) return null;
		float feeMultiple = profile.upfrontLicenseFee;
		var eligible = labels
			.Where(l => l != null && l.IsActive)
			.Where(l => l.cashReserves > l.GetProductionCost() * feeMultiple + l.GetMonthlyOverhead())
			.ToList();
		if (eligible.Count == 0) return null;

		float Score(AILabel l) =>
			l.reputation * 1.2f
			+ Mathf.Log(Mathf.Max(1f, l.cashReserves)) * 0.35f
			+ l.distributionStrength * 0.8f
			+ l.ownedReach * 0.5f
			+ TierWeight(l.tier);

		// Blockbusters gravitate to the strongest house; ordinary opportunities are spread with a bit
		// of noise so breadth (the 350-500 unique-label target) is not monopolized by the majors.
		if (profile.isBlockbuster)
			return eligible.OrderByDescending(Score).First();

		float noiseCeiling = 0.9f;
		return eligible.OrderByDescending(l => Score(l) + (float)GD.RandRange(0.0, noiseCeiling)).First();
	}

	private static float TierWeight(LabelTier tier) => tier switch {
		LabelTier.Major => 1.5f, LabelTier.MidTier => 1.0f, LabelTier.Independent => 0.6f,
		LabelTier.Boutique => 0.5f, LabelTier.Small => 0.3f, _ => 0.4f
	};

	// A plausible pooled appeal for a licensed soundtrack: these are curated, professionally produced
	// records, so appeal is decent and rises with box-office pull, but with variance.
	public static float PooledAppeal(ExternalMediaProfile profile) =>
		Mathf.Clamp(0.5f + profile.boxOfficeTrajectory * 0.35f + (float)GD.RandRange(-0.12, 0.12), 0.2f, 0.95f);
}
