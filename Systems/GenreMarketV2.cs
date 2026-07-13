using System;
using System.Collections.Generic;

/// <summary>Owns Directive 5's exact-off boundary. It is configured before population generation.</summary>
public static class GenreMarketV2 {
	private static bool configured;
	private static bool enabled;
	public static bool Enabled => enabled;

	public static void Configure(bool sceneDefault, IEnumerable<string> arguments) {
		if (configured) return;
		bool enable = false, disable = false;
		foreach (string argument in arguments ?? Array.Empty<string>()) {
			if (argument == "--enable-genre-market-v2") enable = true;
			if (argument == "--disable-genre-market-v2") disable = true;
		}
		if (enable && disable) throw new ArgumentException("--enable-genre-market-v2 and --disable-genre-market-v2 cannot be used together.");
		enabled = enable || (!disable && sceneDefault);
		configured = true;
		if (enabled) GenreCatalog.Validate();
	}
}
