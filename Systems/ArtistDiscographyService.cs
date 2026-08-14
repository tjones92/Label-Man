using System.Collections.Generic;
using System.Linq;

/// <summary>One era plus the records that were actually released inside it.</summary>
public sealed class ArtistDiscographyEra {
	public ArtistEraRecord Era;
	public string Title;
	public string Summary;
	public List<RecordRuntimeData> Records = new();
}

public sealed class ArtistDiscographyProfile {
	public string ArtistId;
	public List<ArtistDiscographyEra> Eras = new();
	public List<RecordRuntimeData> Unassigned = new();
	public bool HasEras => Eras.Count > 0;
}

/// <summary>
/// Assembles the discography-by-era view ON DEMAND and never stores it on the artist.
/// 22.5k artists must not each carry a UI model; this exists for as long as a panel is
/// open and then goes away.
/// </summary>
public static class ArtistDiscographyService {
	public static ArtistDiscographyProfile Build(SimulatedArtist artist, IEnumerable<RecordRuntimeData> records) {
		var profile = new ArtistDiscographyProfile { ArtistId = artist?.artistId };
		List<RecordRuntimeData> ordered = (records ?? Enumerable.Empty<RecordRuntimeData>())
			.Where(record => record?.baseRecord != null)
			.OrderBy(record => record.baseRecord.releaseDate.year)
			.ThenBy(record => record.baseRecord.releaseDate.month)
			.ToList();
		List<ArtistEraRecord> eras = artist?.evolution?.eras;
		if (eras == null || eras.Count == 0) {
			profile.Unassigned = ordered;
			return profile;
		}
		foreach (ArtistEraRecord era in eras) profile.Eras.Add(new ArtistDiscographyEra {
			Era = era,
			Title = ArtistEraSummaryComposer.Title(era),
			Summary = era.summary ?? string.Empty
		});
		foreach (RecordRuntimeData record in ordered) {
			ArtistDiscographyEra bucket = profile.Eras.LastOrDefault(candidate =>
				record.baseRecord.releaseDate.year >= candidate.Era.startYear &&
				(candidate.Era.IsOpen || record.baseRecord.releaseDate.year <= candidate.Era.endYear));
			if (bucket == null) profile.Unassigned.Add(record);
			else bucket.Records.Add(record);
		}
		return profile;
	}

	/// <summary>
	/// Reputation the arc earned, spending the tags <see cref="ReputationTag"/> already
	/// defines rather than extending the enum.
	/// </summary>
	public static IEnumerable<ReputationTag> DeriveTags(SimulatedArtist artist) {
		ArtistEvolutionProfile evolution = artist?.evolution;
		if (evolution == null) yield break;
		int changes = evolution.eras.Count - 1;
		if (changes >= 2) yield return ReputationTag.GenreBending;
		if (changes == 0 && artist.totalReleases >= 6) yield return ReputationTag.Traditional;
		if (evolution.experimentalAppetite >= .70f) yield return ReputationTag.Experimental;
		if (evolution.eras.Any(era => era.phase is ArtistArcPhase.Conceptual or ArtistArcPhase.Experimental) &&
			evolution.artisticAmbition >= .65f) yield return ReputationTag.Innovator;
		if (evolution.eras.Any(era => era.trigger == ArtistEvolutionTrigger.BackToRoots)) yield return ReputationTag.Authentic;
		if (evolution.volatility >= .65f) yield return ReputationTag.Difficult;
		if (evolution.eras.Any(era => era.trigger == ArtistEvolutionTrigger.GenreClimateShift) &&
			evolution.artisticAmbition < .45f) yield return ReputationTag.Derivative;
		if (artist.criticalAcclaim >= .55f) yield return ReputationTag.CriticsDarling;
	}
}
