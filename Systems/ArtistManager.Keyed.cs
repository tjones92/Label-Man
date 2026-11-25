using System.Collections.Generic;
using Godot;

public partial class ArtistManager {
	private const int KeyedArtistIdBase = 90000;
	private int keyedArtistsMade;

	/// <summary>
	/// An authored act for the player's story (the lien holder's girlfriend, §3.5 H1): generated like any other artist but from a
	/// private stream keyed on the world seed and <paramref name="key"/>, with an id out of its own range. Nothing else in the world
	/// moves: the global stream, the population stream and the artist id counter are all put back as they were, so a run that
	/// seeds one and a run that doesn't draw the same world. Player-only; the AI economy never reaches this.
	/// </summary>
	public SimulatedArtist GenerateKeyedArtist(ArtistType type, Genre genre, int year, string region, string key, ScenePlace formationPlace = null) {
		ulong hash = 14695981039346656037UL;
		foreach (char c in key) { hash ^= c; hash *= 1099511628211UL; }
		var savedPopulation = populationRng;
		bool savedFlag = generatingRuntimePopulation;
		int savedCounter = artistIdCounter;
		ulong savedGlobal = GameRandom.State;
		try {
			populationRng = new PcgRandom { Seed = (SimulationSeedBootstrap.RequestedSeed ?? 0UL) ^ hash };
			generatingRuntimePopulation = true;
			artistIdCounter = KeyedArtistIdBase + keyedArtistsMade;
			return GenerateArtist(type, genre, year, region, formationPlace);
		} finally {
			keyedArtistsMade++;
			populationRng = savedPopulation;
			generatingRuntimePopulation = savedFlag;
			artistIdCounter = savedCounter;
			GameRandom.State = savedGlobal;
		}
	}
}
