// Systems/Naming/TitleNaming.cs
// Song- and album-title naming policy, Godot-free (the act/label counterpart is ActNaming.cs).
// NameGenerator delegates here and SimTools/TitleCensus runs this same class, so the census
// measures the game's own code path: routing, the self-titled / format branches, the
// uniqueness buckets.
//
// Titles are not cosmetic: CompetitorManager.GetDeterministicTrackTraits hashes each track title
// into hook / production / danceability. Any change to what this class returns re-baselines the sim
// (see SimTools/NamingFidelityNotes.md).

using System;
using LabelMan.Naming;

public sealed class TitleNaming {
	private readonly ActNaming _acts;
	private NameEngine Engine => _acts.Engine;
	private TemplateRouter Router => _acts.Router;

	public TitleNaming(ActNaming acts) { _acts = acts; }

	/// <summary>Avoid a title already used anywhere in the world while the draw budget allows.
	/// Off reproduces the pre-census behaviour (per-act uniqueness only); the census uses it for the baseline.</summary>
	public bool WorldUniqueness = true;
	private NameEngine.UniqueOptions SongOpts {
		get {
			if (!WorldUniqueness && !TitleSelection.Enabled) return null;
			return new NameEngine.UniqueOptions {
				SoftBucket = WorldUniqueness ? WorldSongBucket : null, SoftAttempts = 24,
				Finalize = TitleSelection.Enabled ? TitlePolish.Title : null,
				Accept = TitleSelection.Enabled ? t => !_acts.IsReservedSong(t) : null,
			};
		}
	}

	/// <summary>Bucket a title's world-wide reuse is tracked in (song titles only).</summary>
	public const string WorldSongBucket = "song|world";

	// ================================================================ SONGS
	/// <summary>The set (or grammar symbol) a genre's song titles come from.</summary>
	public string ChooseSongSymbol(Genre genre, int year, IRandom rng) {
		// Ancestry-driven routing (docs D/E). A genre with no set and no family rung falls to the era blend.
		string key = Router.Resolve("songTitle", genre.ToString());
		if (key != null) return key;
		if (year < 1964) return "songTitle.early60s";
		if (year < 1967) return rng.Chance(0.5) ? "songTitle.early60s" : "songTitle.soul";
		return rng.Next(3) switch { 0 => "songTitle.psych", 1 => "songTitle.soul", _ => "songTitle.early60s" };
	}

	/// <summary>A runtime song title. <paramref name="artistKey"/> is a stable act id (never a display
	/// name): an act never repeats its own title, and a title already used anywhere in the world is
	/// avoided while the draw budget allows -- the period had several songs called "Forever", not
	/// twenty-two called "I Miss You".</summary>
	public string SongTitle(Genre genre, int year, string artistKey, IRandom rng) {
		var ctx = _acts.MakeContext(genre, year, ArtistType.Unknown, rng);
		string symbol = ChooseSongSymbol(genre, year, rng);
		return Engine.Generate(symbol, ctx, "song|" + (artistKey ?? ""), nearDup: false, attempts: 30, SongOpts);
	}

	/// <summary>A pre-game catalog title (legacy standards, recent-hit covers), in its own per-genre bucket.</summary>
	public string CatalogTitle(Genre genre, int year, IRandom rng) {
		var ctx = _acts.MakeContext(genre, year, ArtistType.Unknown, rng);
		string symbol = ChooseSongSymbol(genre, year, rng);
		return Engine.Generate(symbol, ctx, "song|catalog|" + genre, nearDup: false, attempts: 30, SongOpts);
	}

	/// <summary>A title that is a pure function of the rng it is handed (no uniqueness bookkeeping).</summary>
	public string KeyedSongTitle(Genre genre, int year, IRandom rng) {
		var ctx = _acts.MakeContext(genre, year, ArtistType.Unknown, rng);
		string title = Engine.ExpandRouted(ChooseSongSymbol(genre, year, rng), ctx);
		if (TitleSelection.Enabled) title = TitlePolish.Title(title);
		return string.IsNullOrWhiteSpace(title) ? null : title.Trim();
	}

	// ================================================================ ALBUMS
	/// <summary>Genre-flavoured album title. <paramref name="leadSingle"/> binds %leadSingle% /
	/// %titleTrack%; %selfTitle% binds the act's name. <paramref name="artistKey"/> keys the
	/// near-duplicate bucket (an id, not a display name).</summary>
	public string AlbumTitle(Genre genre, int year, string artistName, bool isCompilation, string leadSingle,
							 string artistKey, IRandom rng) {
		var ctx = _acts.MakeContext(genre, year, ArtistType.Unknown, rng);
		ctx.Slots["artist"] = artistName;
		ctx.Slots["selfTitle"] = artistName;
		if (!string.IsNullOrWhiteSpace(leadSingle)) { ctx.Slots["leadSingle"] = leadSingle; ctx.Slots["titleTrack"] = leadSingle; }
		string Polish(string s) => TitleSelection.Enabled ? TitlePolish.Album(s, artistName, leadSingle) : s;
		if (isCompilation) return Polish(Engine.ExpandOnce("compilationTitle", ctx));
		double r = rng.NextDouble();
		if (r < 0.08) return artistName;                          // self-titled (rare; the sets also self-title)
		// genre-blind act formulae ("Meet ...", "Here's ...") -- the genre sets carry their own act frames
		if (r < (TitleSelection.Enabled ? 0.15 : 0.20)) return Polish(Engine.ExpandRouted("albumFormat", ctx));
		string key = Router?.Resolve("albumTitle", genre.ToString()) ?? "albumTitle";
		return Engine.Generate(key, ctx, "album|" + (artistKey ?? artistName), nearDup: false, attempts: 30,
			TitleSelection.Enabled ? new NameEngine.UniqueOptions { Finalize = Polish } : null);
	}
}
