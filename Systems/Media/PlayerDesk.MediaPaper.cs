using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The paper's television column (media directive §14, Phase 1). Read-only: it builds lines from bookings and airings
/// the weekly step already settled, so opening the paper twice, or never, changes nothing. Tonight's listings show who is
/// booked; audiences are printed only for airings already past, as rounded estimates (§4.5).
/// </summary>
public partial class PlayerDesk {
	public IReadOnlyList<TradeSubmission> TradeSubmissions => tradeSubmissions;

	private const int TelevisionListingLines = 3;

	public IReadOnlyList<string> TelevisionColumn(GameDate today) {
		var lines = new List<string>();
		if (!MediaAtmosphere.Observing) return lines;
		string city = CurrentCityId;
		string town = DistanceModel.GetCityById(city)?.name ?? "town";

		// Tonight on the dial here: network shows that clear in this town, and its own local programs.
		var tonight = MediaAtmosphereService.AiringIn(city, today)
			.Where(x => !x.Airing.Repeat)
			.GroupBy(x => x.Program.Id)
			.OrderBy(g => g.Min(x => x.LocalMinute))
			.Take(TelevisionListingLines);
		foreach (var program in tonight) {
			var first = program.First();
			string guests = string.Join("; ", program.OrderBy(x => x.Appearance.SlotIndex).Select(x => $"{x.Appearance.ArtistName}, \"{x.Appearance.Title}\""));
			string when = first.LocalMinute >= 17 * 60 ? "Tonight" : "Today";
			lines.Add($"{when} {MediaText.Clock(first.LocalMinute)}, {MediaOutletCatalog.DisplayName(first.Program, today)} ({MediaText.NetworkName(first.Program)}): {guests}.");
		}

		// Last night's biggest network moment, with an estimated audience now that it has aired.
		GameDate yesterday = today.AddDays(-1);
		var big = MediaAtmosphereService.BiggestAired(yesterday, today, 1).FirstOrDefault();
		if (big.Appearance != null && big.Airing.Exposure.UniqueReach >= 1_000_000) {
			string repeat = big.Airing.Repeat ? " in a repeat" : "";
			lines.Add($"Last night an estimated {MediaText.Audience(big.Airing.Exposure.UniqueReach)} saw {big.Appearance.ArtistName} {MediaText.Verb(big.Appearance.Method)} \"{big.Appearance.Title}\" on {MediaOutletCatalog.DisplayName(big.Program, yesterday)}{repeat}.");
		}

		// The local dance show, if it ran here yesterday: a teen audience the size of a town, not a nation.
		var local = MediaAtmosphereService.AiringIn(city, yesterday)
			.Where(x => x.Program.Network == TvNetwork.Local && !x.Airing.Repeat)
			.OrderBy(x => x.Appearance.SlotIndex).ToList();
		if (local.Count > 0) {
			double teens = local.Max(x => x.Airing.Exposure.AverageByGroup[(int)ViewerGroup.Teen]);
			string acts = string.Join(" and ", local.Select(x => x.Appearance.ArtistName).Distinct());
			lines.Add($"On {MediaOutletCatalog.DisplayName(local[0].Program, yesterday)} yesterday: {acts}, before {MediaText.Audience(teens)} {town} teenagers.");
		}
		return lines;
	}
}
