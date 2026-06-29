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
	
	public static Zeitgeist GetForYear(int year) {
		var z = new Zeitgeist {
			genreAcceptance = new Dictionary<Genre, float>()
		};
		
		foreach (Genre g in Enum.GetValues(typeof(Genre))) {
			z.genreAcceptance[g] = 0.3f;
		}
		
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
				z.youthInfluence = 0.9f;
				z.counterCultureStrength = 0.85f;
				z.racialIntegration = 0.7f;
				z.britishInfluence = 0.6f;
				z.experimentalism = 0.8f;
				z.politicalAwareness = 0.85f;
				break;
			default:
				return InterpolateZeitgeist(year);
		}
		return z;
	}
	
	private static Zeitgeist InterpolateZeitgeist(int year) {
		int[] definedYears = { 1960, 1962, 1964, 1966, 1967, 1968, 1969 };
		int lowerYear = 1960;
		int upperYear = 1969;
		
		for (int i = 0; i < definedYears.Length - 1; i++) {
			if (year >= definedYears[i] && year < definedYears[i + 1]) {
				lowerYear = definedYears[i];
				upperYear = definedYears[i + 1];
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
		
		foreach (Genre g in Enum.GetValues(typeof(Genre))) {
			float aVal = a.genreAcceptance.ContainsKey(g) ? a.genreAcceptance[g] : 0.3f;
			float bVal = b.genreAcceptance.ContainsKey(g) ? b.genreAcceptance[g] : 0.3f;
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
