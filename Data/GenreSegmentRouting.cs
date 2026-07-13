using System.Collections.Generic;

/// <summary>Authored Phase-2 conversion of the Directive 5 source rows into the eleven runtime segments.</summary>
public static class GenreSegmentRouting {
	public static Dictionary<string, float> Create(Genre genre, GenreFamily family, float lean) {
		(float am, float mor, float rb, float country, float college, float fm) source = genre switch {
			Genre.TraditionalPop => (.35f,.60f,0,0,0,0), Genre.TeenPop => (.80f,0,0,0,.05f,0),
			Genre.BaroquePop => (.50f,.30f,0,0,0,.20f), Genre.SunshinePop => (.60f,.25f,0,0,0,.15f),
			Genre.Bubblegum => (.85f,0,0,0,0,0), Genre.EasyListening => (.25f,.70f,0,0,0,0), Genre.BritishPop => (.85f,0,0,0,0,0),
			Genre.RockAndRoll => (.75f,0,.20f,0,0,0), Genre.SurfRock => (.70f,0,0,0,.10f,0), Genre.GarageRock => (.60f,0,0,0,.20f,.10f),
			Genre.PsychedelicRock => (.30f,0,0,0,.25f,.40f), Genre.AcidRock => (.20f,0,0,0,.30f,.45f), Genre.HardRock => (.30f,0,0,0,.25f,.40f),
			Genre.ProtoMetal => (.15f,0,0,0,.30f,.50f), Genre.ProgressiveRock => (.10f,0,0,0,.30f,.55f), Genre.BluesRock => (.25f,0,.10f,0,.25f,.40f),
			Genre.ProtoPunk => (.05f,0,0,0,.40f,.50f), Genre.BritishBeat => (.80f,0,0,0,.10f,0), Genre.BritishBlues => (.50f,0,.10f,0,.15f,.25f),
			Genre.RnB => (.30f,0,.60f,0,0,0), Genre.Soul => (.40f,0,.50f,0,.10f,0), Genre.Funk => (.30f,0,.55f,0,.10f,.05f), Genre.DooWop => (.50f,0,.40f,0,0,0),
			Genre.Gospel => (.20f,.30f,.50f,0,0,0), Genre.Country => (.40f,.40f,0,0,.05f,0), Genre.CountryRock => (.25f,0,0,0,.30f,.35f),
			Genre.Folk => (.20f,.25f,0,0,.50f,0), Genre.FolkRock => (.40f,0,0,0,.30f,.20f), Genre.ContemporaryFolk => (.20f,.20f,0,0,.55f,0), Genre.SingerSongwriter => (0,.15f,0,0,.35f,.40f),
			Genre.Jazz => (0,.50f,.15f,0,.25f,.10f), Genre.BossaNova => (.20f,.55f,0,0,.20f,0), Genre.Blues => (0,.15f,.40f,0,.25f,.20f), Genre.Classical => (0,.70f,0,0,.25f,0),
			Genre.Boogaloo => (.30f,0,.40f,0,0,0), Genre.TexMex => (.40f,0,.20f,0,0,0), Genre.LatinPop => (.40f,.30f,0,0,0,0),
			Genre.Ska => (0,0,.40f,0,.20f,0), Genre.Rocksteady => (0,0,.40f,0,.20f,0), Genre.Reggae => (.20f,0,.35f,0,.20f,0),
			Genre.Comedy => (.20f,.40f,0,0,.30f,0), Genre.Childrens => (0,.60f,0,0,0,0), _ => (.35f,.20f,0,0,.10f,.05f)
		};
		var (am, mor, rb, country, college, fm) = source;
		var w = new Dictionary<string, float> {
			["MainstreamAM"] = am * (1f - (.35f + .45f * lean)), ["Youth"] = am * (.35f + .45f * lean),
			["AdultMOR"] = mor, ["UrbanRnB"] = rb, ["CountryWestern"] = country,
			["CollegeFolk"] = college, ["UndergroundFM"] = fm, ["JazzHiFiClassical"] = 0f,
			["GospelChurch"] = 0f, ["RegionalLatin"] = 0f, ["FamilyChildrens"] = 0f
		};
		if (family is GenreFamily.Jazz or GenreFamily.Classical) { w["JazzHiFiClassical"] += w["AdultMOR"]; w["AdultMOR"] = 0f; }
		else if (genre == Genre.Childrens) { w["FamilyChildrens"] += w["AdultMOR"]; w["AdultMOR"] = 0f; }
		else if (genre == Genre.Gospel) { w["GospelChurch"] += w["AdultMOR"]; w["AdultMOR"] = 0f; }

		if (family == GenreFamily.Country) EnsureMinimum(w, "CountryWestern", .35f);
		if (family == GenreFamily.Gospel) EnsureMinimum(w, "GospelChurch", .40f);
		if (family == GenreFamily.Latin) EnsureMinimum(w, "RegionalLatin", .30f);
		if (family is GenreFamily.Jazz or GenreFamily.Classical) EnsureMinimum(w, "JazzHiFiClassical", .40f);
		if (genre == Genre.Childrens) EnsureMinimum(w, "FamilyChildrens", .60f);

		float sum = 0f; foreach (float value in w.Values) sum += value;
		var keys = new List<string>(w.Keys); foreach (string key in keys) w[key] /= sum;
		return w;
	}

	private static void EnsureMinimum(Dictionary<string, float> weights, string specialist, float minimum) {
		float deficit = minimum - weights[specialist];
		if (deficit <= 0f) return;
		string largest = null; float largestValue = -1f;
		foreach ((string key, float value) in weights) if (key != specialist && value > largestValue) { largest = key; largestValue = value; }
		float moved = System.MathF.Min(deficit, System.MathF.Max(0f, largestValue));
		weights[largest] -= moved; weights[specialist] += moved;
	}
}
