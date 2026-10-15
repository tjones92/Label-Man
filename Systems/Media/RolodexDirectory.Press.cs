using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// The PRESS & TV tab (media directive §9.1, §14). Its own role, never a stretched deejay card: these people carry no
/// station influence and no payola flags. Each card is read off real state: a program actually on the dial in a town the
/// label works from, or the review desk the label has actually mailed. Names and faces are hashed from the program or
/// desk id, so building the book can't move a seeded run.
/// </summary>
public static partial class RolodexDirectory {
	private static List<DirectoryCard> PressAndTelevision(PlayerDesk desk) {
		var cards = new List<DirectoryCard>();
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		if (MediaAtmosphere.Observing) {
			var towns = new List<string>();
			foreach (string city in new[] { desk.Label?.homeCityId, desk.CurrentCityId })
				if (city != null && !towns.Contains(city)) towns.Add(city);
			var seen = new HashSet<string>(StringComparer.Ordinal);
			foreach (string city in towns) {
				foreach (TvProgram program in MediaOutletCatalog.Programs) {
					BroadcastScheduleVersion version = program.VersionOn(today) ?? program.Schedule.FirstOrDefault(v => today >= v.First && today <= v.Last);
					if (version == null || !seen.Add(program.Id)) continue;
					bool local = program.Network == TvNetwork.Local;
					bool syndicatedHere = local && program.HomeCityId != city && program.Syndication.Any(m => m.CityId == city && today >= m.From);
					if (local && program.HomeCityId != city && !syndicatedHere) { seen.Remove(program.Id); continue; }
					if (!local && !MediaAtmosphereService.ClearsIn(program, city)) { seen.Remove(program.Id); continue; }
					cards.Add(ProgramCard(program, version, city, syndicatedHere, today));
				}
			}
		}
		if (desk.TradeSubmissions.Count > 0) cards.Add(TradeDeskCard(desk, today));
		return cards;
	}

	private static DirectoryCard ProgramCard(TvProgram program, BroadcastScheduleVersion version, string city, bool syndicated, GameDate today) {
		bool local = program.Network == TvNetwork.Local;
		string key = "tv:" + program.Id;
		bool female = Portraits.Hash(key + "|female") % 5 == 0;
		var card = new DirectoryCard {
			Role = RolodexRole.Press, Key = key, Name = PersonName(key, female),
			Title = local ? "Producer" : "Talent booker",
			Firm = MediaOutletCatalog.DisplayName(program, today) + " (" + MediaText.NetworkName(program) + ")",
			Place = DistanceModel.GetCityById(local ? program.HomeCityId : version.ProductionCityId)?.name,
			Figure = Portraits.Businessman(key, female),
		};
		string host = string.IsNullOrEmpty(program.HostName) ? "" : $" Host: {program.HostName}.";
		string slot = syndicated
			? $"On tape here, {program.Syndication.First(m => m.CityId == city).DelayWeeks} week(s) behind its home station, Saturdays at {MediaText.Clock(program.Syndication.First(m => m.CityId == city).LocalStartMinute)}."
			: MediaText.SlotIn(program, version, city) + (local ? "." : " in " + (DistanceModel.GetCityById(city)?.name ?? "your town") + ".");
		card.Facts.Add((slot + host, false));
		card.Facts.Add(("What they book: " + MediaText.ProducerWants(program), false));
		card.Facts.Add(("On camera: " + program.MethodNote, false));
		var recent = MediaAtmosphereService.Appearances
			.Where(a => a.ProgramId == program.Id && a.RepeatOfId == null && a.Date < today)
			.OrderByDescending(a => a.Date.year * 10000 + a.Date.month * 100 + a.Date.day).ThenBy(a => a.SlotIndex)
			.Take(3).Select(a => $"{a.ArtistName} (\"{a.Title}\", {a.Date.ToHeadlineString()})").ToList();
		if (recent.Count > 0) card.Facts.Add(("Recent guests: " + string.Join("; ", recent) + ".", false));
		if (version.RepeatMonths.Length > 0)
			card.Facts.Add(($"Repeats in {string.Join(" and ", version.RepeatMonths.Select(m => new GameDate(1960, m, 1).MonthName))}: no new guests booked then.", false));
		card.Facts.Add(("Pitching your own acts to this desk isn't open from the office yet.", true));
		return card;
	}

	private static DirectoryCard TradeDeskCard(PlayerDesk desk, GameDate today) {
		const string key = "press:cash_box_reviews";
		var card = new DirectoryCard {
			Role = RolodexRole.Press, Key = key, Name = PersonName(key), Title = "Singles review editor", Firm = "Cash Box", Place = "New York",
			Figure = Portraits.Businessman(key), GoToTab = "OFFICE", GoToLabel = "OPEN THE OFFICE",
		};
		var subs = desk.TradeSubmissions;
		int picks = subs.Count(s => s.Outcome is TradeOutcome.Spotlight or TradeOutcome.FourStar);
		int pending = subs.Count(s => s.Outcome == TradeOutcome.Pending);
		card.Facts.Add(($"{subs.Count} record(s) sent for review; {picks} made a pick" + (pending > 0 ? $", {pending} still on the desk." : "."), false));
		card.Facts.Add(("A pick talks to the trade: distributors, one-stops and programmers. It never buys a review, and an ad on the same record says much the same thing to the same readers.", false));
		PressOutlet rival = MediaOutletCatalog.Press.FirstOrDefault(p => p.Id == (today < new GameDate(1964, 10, 1) ? "music_vendor" : "record_world"));
		if (rival != null) card.Facts.Add(($"The other trades this season: Billboard and {rival.Name}.", false));
		return card;
	}
}
