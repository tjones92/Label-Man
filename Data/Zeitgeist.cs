using System;
using System.Collections.Generic;
using Godot;

public class Zeitgeist {
	public Dictionary<Genre, float> genreAcceptance;

	public float youthInfluence;
	public float counterCultureStrength;
	public float racialIntegration;
	public float britishInfluence;
	public float experimentalism;
	public float politicalAwareness;

	private const float UnestablishedAcceptance = 0.3f;
	private static readonly int[] DefinedYears = { 1960, 1962, 1964, 1966, 1967, 1968, 1969 };

	/// <summary>
	/// Each defined year is a sparse override applied on top of the years before it, so a
	/// genre left out of a table keeps the acceptance it last held. The tables used to be
	/// rebuilt from a flat 0.3 default, which made omission mean "revert to the acceptance
	/// of a genre nobody has an opinion about" rather than "unchanged". That silently
	/// snapped five established genres — Easy Listening .55, Teen Pop .50, Folk .60, Surf
	/// Rock .65, Bossa Nova .55 — down to 0.3 in a single step at 1966, while the offsetting
	/// gains all went to genres that emerge in 1965-66 with no roster behind them yet. Total
	/// acceptance mass barely moved, so it read as a market-wide demand cliff rather than
	/// as an authoring artifact. It cut the other way too: doo-wop rose .05 -> .30 in 1968
	/// before falling to .02 in 1969, and hard rock, proto-metal, prog and proto-punk all
	/// sat at .30 through the early decade after being authored at .01 in 1960.
	///
	/// Genres that were never named before the year in question still start at 0.3. That is
	/// the genuine "no prior value" case, and the legacy artist generator does not place
	/// records in those genres that early, so the value is inert where it is wrong.
	/// </summary>
	public static Zeitgeist GetForYear(int year) {
		if (Array.IndexOf(DefinedYears, year) < 0) return InterpolateZeitgeist(year);
		var z = new Zeitgeist {
			genreAcceptance = new Dictionary<Genre, float>()
		};
		foreach (Genre g in GenreDomains.Current) {
			z.genreAcceptance[g] = UnestablishedAcceptance;
		}
		foreach (int defined in DefinedYears) {
			if (defined > year) break;
			ApplyDefinedYear(z, defined);
		}
		return z;
	}

	/// <summary>
	/// Applies one keyframe's overrides in place. Genre entries are cumulative; the mood
	/// scalars are absolute and every keyframe sets all six, so they need no carry-forward.
	/// Values marked "decay" restore a fade that the flat-default rebuild used to supply as
	/// a side effect. They follow the same direction as the canonical GenreCatalog curve for
	/// that genre, anchored on the legacy table's own level rather than the catalog's, so the
	/// two routes agree on shape without this table being re-levelled underneath the
	/// disabled-route calibration.
	/// </summary>
	private static void ApplyDefinedYear(Zeitgeist z, int year) {
		switch (year) {
			case 1960:
				z.genreAcceptance[Genre.TraditionalPop] = 0.9f;
				z.genreAcceptance[Genre.EasyListening] = 0.8f;
				z.genreAcceptance[Genre.DooWop] = 0.75f;
				z.genreAcceptance[Genre.TeenPop] = 0.7f;
				z.genreAcceptance[Genre.RockAndRoll] = 0.6f;
				z.genreAcceptance[Genre.RnB] = 0.4f;
				z.genreAcceptance[Genre.Country] = 0.65f;
				z.genreAcceptance[Genre.Folk] = 0.4f;
				z.genreAcceptance[Genre.Jazz] = 0.5f;
				z.genreAcceptance[Genre.BossaNova] = 0.3f;
				z.genreAcceptance[Genre.Gospel] = 0.35f;
				z.genreAcceptance[Genre.BritishInvasion] = 0.05f;
				z.genreAcceptance[Genre.Psychedelic] = 0.02f;
				z.genreAcceptance[Genre.GarageRock] = 0.1f;
				z.genreAcceptance[Genre.HardRock] = 0.01f;
				z.genreAcceptance[Genre.ProtoMetal] = 0.01f;
				z.genreAcceptance[Genre.ProgressiveRock] = 0.01f;
				z.genreAcceptance[Genre.ProtoPunk] = 0.01f;
				z.youthInfluence = 0.4f;
				z.counterCultureStrength = 0.1f;
				z.racialIntegration = 0.3f;
				z.britishInfluence = 0.1f;
				z.experimentalism = 0.15f;
				z.politicalAwareness = 0.2f;
				break;
			case 1962:
				z.genreAcceptance[Genre.TraditionalPop] = 0.75f;
				z.genreAcceptance[Genre.EasyListening] = 0.7f;
				z.genreAcceptance[Genre.DooWop] = 0.5f;
				z.genreAcceptance[Genre.TeenPop] = 0.75f;
				z.genreAcceptance[Genre.RockAndRoll] = 0.65f;
				z.genreAcceptance[Genre.GirlGroup] = 0.8f;
				z.genreAcceptance[Genre.RnB] = 0.5f;
				z.genreAcceptance[Genre.Soul] = 0.55f;
				z.genreAcceptance[Genre.Folk] = 0.5f;
				z.genreAcceptance[Genre.SurfRock] = 0.6f;
				z.genreAcceptance[Genre.BossaNova] = 0.5f;
				// Decay: jazz and country hold large, stable record markets all decade, but
				// their share of the pop chart this table describes is already eroding.
				z.genreAcceptance[Genre.Jazz] = 0.42f;
				z.genreAcceptance[Genre.Country] = 0.6f;
				z.youthInfluence = 0.55f;
				z.counterCultureStrength = 0.15f;
				z.racialIntegration = 0.4f;
				z.britishInfluence = 0.15f;
				z.experimentalism = 0.2f;
				z.politicalAwareness = 0.3f;
				break;
			case 1964:
				z.genreAcceptance[Genre.BritishInvasion] = 0.95f;
				z.genreAcceptance[Genre.DooWop] = 0.2f;
				z.genreAcceptance[Genre.TraditionalPop] = 0.5f;
				z.genreAcceptance[Genre.EasyListening] = 0.55f;
				z.genreAcceptance[Genre.Motown] = 0.85f;
				z.genreAcceptance[Genre.Soul] = 0.75f;
				z.genreAcceptance[Genre.GirlGroup] = 0.7f;
				z.genreAcceptance[Genre.SurfRock] = 0.65f;
				z.genreAcceptance[Genre.GarageRock] = 0.5f;
				z.genreAcceptance[Genre.Folk] = 0.6f;
				z.genreAcceptance[Genre.TeenPop] = 0.5f;
				z.genreAcceptance[Genre.BossaNova] = 0.55f;
				z.genreAcceptance[Genre.Psychedelic] = 0.15f;
				// Decay: rock and roll is displaced as a chart label by the British wave;
				// R&B is carried up by the same integration the Motown entry above records.
				z.genreAcceptance[Genre.RockAndRoll] = 0.5f;
				z.genreAcceptance[Genre.RnB] = 0.55f;
				z.genreAcceptance[Genre.Jazz] = 0.34f;
				z.genreAcceptance[Genre.Country] = 0.58f;
				z.youthInfluence = 0.75f;
				z.counterCultureStrength = 0.3f;
				z.racialIntegration = 0.55f;
				z.britishInfluence = 0.9f;
				z.experimentalism = 0.35f;
				z.politicalAwareness = 0.45f;
				break;
			case 1966:
				z.genreAcceptance[Genre.BritishInvasion] = 0.8f;
				z.genreAcceptance[Genre.Psychedelic] = 0.5f;
				z.genreAcceptance[Genre.GarageRock] = 0.65f;
				z.genreAcceptance[Genre.FolkRock] = 0.75f;
				z.genreAcceptance[Genre.Soul] = 0.85f;
				z.genreAcceptance[Genre.Motown] = 0.9f;
				z.genreAcceptance[Genre.BaroquePop] = 0.5f;
				z.genreAcceptance[Genre.SunshinePop] = 0.55f;
				z.genreAcceptance[Genre.BluesRock] = 0.45f;
				z.genreAcceptance[Genre.DooWop] = 0.1f;
				z.genreAcceptance[Genre.GirlGroup] = 0.45f;
				z.genreAcceptance[Genre.TraditionalPop] = 0.4f;
				// Decay: this is the year the flat-default rebuild used to erase five
				// established genres at once. They do decline, and folk, surf, bossa nova
				// and teen pop decline steeply, but 1966 is a strong Singles year overall —
				// they slide, they do not vanish inside twelve months.
				z.genreAcceptance[Genre.EasyListening] = 0.52f;
				z.genreAcceptance[Genre.TeenPop] = 0.35f;
				z.genreAcceptance[Genre.Folk] = 0.45f;
				z.genreAcceptance[Genre.SurfRock] = 0.4f;
				z.genreAcceptance[Genre.BossaNova] = 0.4f;
				z.genreAcceptance[Genre.RockAndRoll] = 0.24f;
				z.genreAcceptance[Genre.RnB] = 0.5f;
				z.genreAcceptance[Genre.Jazz] = 0.3f;
				z.genreAcceptance[Genre.Country] = 0.56f;
				z.youthInfluence = 0.8f;
				z.counterCultureStrength = 0.5f;
				z.racialIntegration = 0.6f;
				z.britishInfluence = 0.8f;
				z.experimentalism = 0.55f;
				z.politicalAwareness = 0.55f;
				break;
			case 1967:
				z.genreAcceptance[Genre.Psychedelic] = 0.85f;
				z.genreAcceptance[Genre.AcidRock] = 0.65f;
				z.genreAcceptance[Genre.Soul] = 0.9f;
				z.genreAcceptance[Genre.Motown] = 0.85f;
				z.genreAcceptance[Genre.FolkRock] = 0.7f;
				z.genreAcceptance[Genre.BaroquePop] = 0.65f;
				z.genreAcceptance[Genre.SunshinePop] = 0.7f;
				z.genreAcceptance[Genre.BluesRock] = 0.6f;
				z.genreAcceptance[Genre.GarageRock] = 0.55f;
				z.genreAcceptance[Genre.BritishInvasion] = 0.7f;
				z.genreAcceptance[Genre.DooWop] = 0.05f;
				z.genreAcceptance[Genre.TraditionalPop] = 0.35f;
				z.genreAcceptance[Genre.ProtoPunk] = 0.15f;
				z.genreAcceptance[Genre.HardRock] = 0.3f;
				// Decay.
				z.genreAcceptance[Genre.EasyListening] = 0.44f;
				z.genreAcceptance[Genre.TeenPop] = 0.3f;
				z.genreAcceptance[Genre.Folk] = 0.35f;
				z.genreAcceptance[Genre.SurfRock] = 0.3f;
				z.genreAcceptance[Genre.BossaNova] = 0.3f;
				z.genreAcceptance[Genre.RockAndRoll] = 0.16f;
				z.genreAcceptance[Genre.RnB] = 0.48f;
				z.genreAcceptance[Genre.Jazz] = 0.29f;
				z.genreAcceptance[Genre.Country] = 0.55f;
				z.genreAcceptance[Genre.GirlGroup] = 0.35f;
				z.youthInfluence = 0.85f;
				z.counterCultureStrength = 0.7f;
				z.racialIntegration = 0.65f;
				z.britishInfluence = 0.75f;
				z.experimentalism = 0.8f;
				z.politicalAwareness = 0.7f;
				break;
			case 1968:
				z.genreAcceptance[Genre.Soul] = 0.9f;
				z.genreAcceptance[Genre.Psychedelic] = 0.75f;
				z.genreAcceptance[Genre.AcidRock] = 0.7f;
				z.genreAcceptance[Genre.BluesRock] = 0.7f;
				z.genreAcceptance[Genre.HardRock] = 0.5f;
				z.genreAcceptance[Genre.Funk] = 0.55f;
				z.genreAcceptance[Genre.FolkRock] = 0.6f;
				z.genreAcceptance[Genre.CountryRock] = 0.4f;
				z.genreAcceptance[Genre.Bubblegum] = 0.65f;
				z.genreAcceptance[Genre.SunshinePop] = 0.6f;
				z.genreAcceptance[Genre.ProtoPunk] = 0.25f;
				z.genreAcceptance[Genre.ProtoMetal] = 0.2f;
				// Decay. The British Invasion as a distinct chart wave is spent by 1968 —
				// its acts are still selling, but under psychedelia and blues rock above.
				z.genreAcceptance[Genre.TraditionalPop] = 0.32f;
				z.genreAcceptance[Genre.EasyListening] = 0.37f;
				z.genreAcceptance[Genre.TeenPop] = 0.28f;
				z.genreAcceptance[Genre.Folk] = 0.3f;
				z.genreAcceptance[Genre.RockAndRoll] = 0.1f;
				z.genreAcceptance[Genre.RnB] = 0.45f;
				z.genreAcceptance[Genre.Jazz] = 0.28f;
				z.genreAcceptance[Genre.Country] = 0.55f;
				z.genreAcceptance[Genre.GirlGroup] = 0.25f;
				z.genreAcceptance[Genre.BritishInvasion] = 0.5f;
				z.genreAcceptance[Genre.GarageRock] = 0.4f;
				z.genreAcceptance[Genre.BaroquePop] = 0.5f;
				z.youthInfluence = 0.9f;
				z.counterCultureStrength = 0.8f;
				z.racialIntegration = 0.65f;
				z.britishInfluence = 0.65f;
				z.experimentalism = 0.75f;
				z.politicalAwareness = 0.85f;
				break;
			case 1969:
				z.genreAcceptance[Genre.Soul] = 0.9f;
				z.genreAcceptance[Genre.Funk] = 0.7f;
				z.genreAcceptance[Genre.Psychedelic] = 0.65f;
				z.genreAcceptance[Genre.AcidRock] = 0.65f;
				z.genreAcceptance[Genre.HardRock] = 0.65f;
				z.genreAcceptance[Genre.BluesRock] = 0.7f;
				z.genreAcceptance[Genre.ProtoMetal] = 0.35f;
				z.genreAcceptance[Genre.ProgressiveRock] = 0.4f;
				z.genreAcceptance[Genre.CountryRock] = 0.55f;
				z.genreAcceptance[Genre.Bubblegum] = 0.6f;
				z.genreAcceptance[Genre.ProtoPunk] = 0.3f;
				z.genreAcceptance[Genre.SkaRocksteady] = 0.3f;
				z.genreAcceptance[Genre.DooWop] = 0.02f;
				z.genreAcceptance[Genre.GirlGroup] = 0.2f;
				z.genreAcceptance[Genre.TraditionalPop] = 0.3f;
				// Decay.
				z.genreAcceptance[Genre.EasyListening] = 0.3f;
				z.genreAcceptance[Genre.TeenPop] = 0.25f;
				z.genreAcceptance[Genre.RockAndRoll] = 0.07f;
				z.genreAcceptance[Genre.RnB] = 0.42f;
				z.genreAcceptance[Genre.Jazz] = 0.27f;
				z.genreAcceptance[Genre.Country] = 0.56f;
				z.genreAcceptance[Genre.BritishInvasion] = 0.4f;
				z.genreAcceptance[Genre.GarageRock] = 0.3f;
				z.genreAcceptance[Genre.BaroquePop] = 0.4f;
				z.genreAcceptance[Genre.FolkRock] = 0.55f;
				z.genreAcceptance[Genre.SunshinePop] = 0.5f;
				z.genreAcceptance[Genre.Motown] = 0.8f;
				z.youthInfluence = 0.9f;
				z.counterCultureStrength = 0.85f;
				z.racialIntegration = 0.7f;
				z.britishInfluence = 0.6f;
				z.experimentalism = 0.8f;
				z.politicalAwareness = 0.85f;
				break;
		}
	}

	private static Zeitgeist InterpolateZeitgeist(int year) {
		int lowerYear = DefinedYears[0];
		int upperYear = DefinedYears[^1];

		for (int i = 0; i < DefinedYears.Length - 1; i++) {
			if (year >= DefinedYears[i] && year < DefinedYears[i + 1]) {
				lowerYear = DefinedYears[i];
				upperYear = DefinedYears[i + 1];
				break;
			}
		}

		float t = (float)(year - lowerYear) / (upperYear - lowerYear);
		return Lerp(GetForYear(lowerYear), GetForYear(upperYear), t);
	}

	private static Zeitgeist Lerp(Zeitgeist a, Zeitgeist b, float t) {
		var result = new Zeitgeist {
			genreAcceptance = new Dictionary<Genre, float>()
		};

		foreach (Genre g in GenreDomains.Current) {
			float aVal = a.genreAcceptance.ContainsKey(g) ? a.genreAcceptance[g] : UnestablishedAcceptance;
			float bVal = b.genreAcceptance.ContainsKey(g) ? b.genreAcceptance[g] : UnestablishedAcceptance;
			result.genreAcceptance[g] = Mathf.Lerp(aVal, bVal, t);
		}

		result.youthInfluence = Mathf.Lerp(a.youthInfluence, b.youthInfluence, t);
		result.counterCultureStrength = Mathf.Lerp(a.counterCultureStrength, b.counterCultureStrength, t);
		result.racialIntegration = Mathf.Lerp(a.racialIntegration, b.racialIntegration, t);
		result.britishInfluence = Mathf.Lerp(a.britishInfluence, b.britishInfluence, t);
		result.experimentalism = Mathf.Lerp(a.experimentalism, b.experimentalism, t);
		result.politicalAwareness = Mathf.Lerp(a.politicalAwareness, b.politicalAwareness, t);

		return result;
	}
}
