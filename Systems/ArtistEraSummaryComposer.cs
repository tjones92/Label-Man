using System;
using System.Text;

/// <summary>
/// Writes the one-line summary an era carries in the discography panel.
/// <para>
/// GENERATED, NOT TEMPLATED -- but generated from a CLOSED grammar, which is what makes
/// coherence a guarantee rather than a hope. A Markov or free-form generator cannot
/// promise that every sentence it emits about a band is true or even grammatical. This
/// composes each line out of authored sentence-level units chosen by the era's own
/// measured facts (trigger, phase, genre family, chart record), with slots that carry
/// only genre names and integers. Every reachable output is therefore a sentence a
/// person wrote, filled with numbers that actually happened.
/// </para>
/// <para>
/// Variety without randomness: variant choice is an FNV hash of artistId|eraIndex, the
/// same deterministic-roll pattern the supply selection uses. Evolution draws nothing
/// from the global stream.
/// </para>
/// </summary>
public static class ArtistEraSummaryComposer {
	public static string Compose(SimulatedArtist artist, ArtistEraRecord era, Genre priorGenre) {
		if (era == null) return string.Empty;
		var line = new StringBuilder();
		line.Append(Opening(artist, era, priorGenre));
		string outcome = Outcome(artist, era);
		if (outcome.Length > 0) line.Append(' ').Append(outcome);
		return line.ToString();
	}

	/// <summary>The era's short title, e.g. "1964-1965 · Soul crossover".</summary>
	public static string Title(ArtistEraRecord era) {
		string span = era.IsOpen ? $"{era.startYear}-" : $"{era.startYear}-{era.endYear}";
		return $"{span} · {Label(era)}";
	}

	private static string Label(ArtistEraRecord era) {
		string genre = Display(era.primaryGenre);
		return era.phase switch {
			ArtistArcPhase.Formative => $"{genre} club years",
			ArtistArcPhase.HitSeeking => $"{genre} singles",
			ArtistArcPhase.Breakthrough => $"{genre} breakthrough",
			ArtistArcPhase.Consolidation => $"{genre} at the top",
			ArtistArcPhase.Experimental => $"{genre} studio experiment",
			ArtistArcPhase.Conceptual => $"the {genre} statement",
			ArtistArcPhase.RootsReturn => $"back to {genre}",
			ArtistArcPhase.CommercialPivot => $"{genre} pivot",
			ArtistArcPhase.Declining => $"{genre} late period",
			ArtistArcPhase.Legacy => $"{genre} legacy",
			_ => genre
		};
	}

	// --- sentence inventory. Each entry is a complete authored sentence; none depends on
	// what any other slot chose, so no combination can produce a disagreement.

	private static readonly string[] FormativeOpenings = {
		"Early sides stayed close to the sound they had been playing live.",
		"The first records were cut fast and cheap, and sounded like it.",
		"They started where everybody in the room started."
	};

	private static string Opening(SimulatedArtist artist, ArtistEraRecord era, Genre priorGenre) {
		if (era.eraIndex == 0) return Pick(FormativeOpenings, artist.artistId, era.eraIndex);
		string from = Display(priorGenre), to = Display(era.primaryGenre);
		string[] variants = era.trigger switch {
			ArtistEvolutionTrigger.BackToRoots => new[] {
				$"After the {from.ToLowerInvariant()} records stopped landing, they stripped back toward the {to.ToLowerInvariant()} material they started on.",
				$"They went home to {to.ToLowerInvariant()}, and stopped apologising for it."
			},
			ArtistEvolutionTrigger.CommercialFailure => new[] {
				$"Two records nobody bought ended the {from.ToLowerInvariant()} experiment; the next one was {to.ToLowerInvariant()}.",
				$"With the {from.ToLowerInvariant()} sides failing, they took the {to.ToLowerInvariant()} route the label had been pushing."
			},
			ArtistEvolutionTrigger.CommercialBreakthrough => new[] {
				$"A hit off the {to.ToLowerInvariant()} side settled the argument about what the band was.",
				$"The {to.ToLowerInvariant()} record sold, so the {to.ToLowerInvariant()} record became the band."
			},
			ArtistEvolutionTrigger.CriticalBreakthrough => new[] {
				$"The critics heard the {to.ToLowerInvariant()} record before radio did.",
				$"The {to.ToLowerInvariant()} sides earned them a reputation before they earned a hit."
			},
			ArtistEvolutionTrigger.GenreClimateShift => new[] {
				$"They were {from.ToLowerInvariant()} musicians when {to.ToLowerInvariant()} arrived, and they plugged in with everyone else.",
				$"The scene turned toward {to.ToLowerInvariant()} and they turned with it."
			},
			ArtistEvolutionTrigger.PeerInfluence => new[] {
				$"After hearing what acts they had shared bills with were cutting, the sessions turned {to.ToLowerInvariant()}.",
				$"Somebody else's {to.ToLowerInvariant()} record changed what they thought a record could be."
			},
			ArtistEvolutionTrigger.CohesiveAlbumMovement => new[] {
				$"Following two albums that hung together as records rather than collections, they reached for the same thing in {to.ToLowerInvariant()}.",
				$"The album, not the single, became the unit -- and the album was {to.ToLowerInvariant()}."
			},
			ArtistEvolutionTrigger.LabelPressure => new[] {
				$"The label wanted {to.ToLowerInvariant()} sides, and the label was paying.",
				$"Pressure from upstairs moved the sessions from {from.ToLowerInvariant()} to {to.ToLowerInvariant()}."
			},
			ArtistEvolutionTrigger.InternalTension => new[] {
				$"An argument inside the band about the {from.ToLowerInvariant()} material ended with {to.ToLowerInvariant()} records.",
				$"The lineup stopped agreeing about {from.ToLowerInvariant()}; what came out was {to.ToLowerInvariant()}."
			},
			_ => new[] {
				$"The {from.ToLowerInvariant()} records gave way to {to.ToLowerInvariant()} without anyone announcing it.",
				$"By this point they had been cutting {to.ToLowerInvariant()} sides long enough to be a {to.ToLowerInvariant()} act."
			}
		};
		return Pick(variants, artist.artistId, era.eraIndex);
	}

	private static string Outcome(SimulatedArtist artist, ArtistEraRecord era) {
		if (era.releases == 0) return string.Empty;
		if (era.top40Releases > 0 && era.bestPeakPosition == 1)
			return $"{era.releases} sides, one of them a number one.";
		if (era.top40Releases > 1)
			return $"{era.top40Releases} of {era.releases} sides reached the Top 40.";
		if (era.top40Releases == 1)
			return $"One of the {era.releases} sides broke the Top 40, peaking at {era.bestPeakPosition}.";
		if (era.chartedReleases > 0)
			return $"{era.chartedReleases} of {era.releases} charted, none of them high.";
		return era.releases == 1 ? "The one record went nowhere." : $"All {era.releases} sides missed.";
	}

	/// <summary>Deterministic variant choice. Same FNV shape as the supply roll; no draw.</summary>
	private static string Pick(string[] variants, string artistId, int eraIndex) {
		if (variants.Length == 1) return variants[0];
		uint hash = 2166136261u;
		foreach (char value in $"{artistId}|era|{eraIndex}") {
			hash ^= value;
			hash *= 16777619u;
		}
		return variants[hash % (uint)variants.Length];
	}

	/// <summary>The panel's existing genre spelling, so eras read the same as everything else.</summary>
	private static string Display(Genre genre) => GenreNameFormatter.Format(genre);
}
