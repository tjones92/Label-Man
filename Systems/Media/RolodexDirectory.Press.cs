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
		if (MediaAtmosphere.Observing && desk.Label != null) {
			var outlets = new List<(PressOutlet, PressRole)>();
			foreach (PressOutlet o in MediaOutletCatalog.Press.Where(o => o.Publishing(today) && o.HomeCityId != null && o.HomeCityId == desk.Label.homeCityId))
				outlets.Add((o, MediaPress.RoleFor(o, StoryAngle.LabelPitch)));
			// Anyone the label has dealt with stays in the book, wherever they are.
			foreach (PressOutlet o in MediaOutletCatalog.Press)
				foreach (PressRole role in Enum.GetValues<PressRole>())
					if (MediaAtmosphereService.ContactFor(MediaPress.ContactKey(o, role)) != null && !outlets.Contains((o, role))) outlets.Add((o, role));
			foreach (var (o, role) in outlets) cards.Add(PressDeskCard(o, role, today));
		}
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
		AddMemory(card, MediaAtmosphereService.ContactFor(MediaAtmosphereService.ProducerKey(program)));
		card.GoToTab = "OFFICE";
		card.GoToLabel = "PITCH FROM THE OFFICE";
		return card;
	}

	/// <summary>Concrete memory instead of a friendship percentage (§14): what this person remembers of the label.</summary>
	private static void AddMemory(DirectoryCard card, MediaContactMemory memory) {
		if (memory == null || memory.Lines.Count == 0) return;
		foreach (string line in memory.Lines.Take(3)) card.Facts.Add(("Remembers: " + line, memory.Trust < -0.2f));
	}

	/// <summary>A press desk the label can reach or has dealt with: its town's daily and community papers, and every desk
	/// that has a memory of the label.</summary>
	private static DirectoryCard PressDeskCard(PressOutlet outlet, PressRole role, GameDate today) {
		string key = MediaPress.ContactKey(outlet, role);
		bool female = Portraits.Hash(key + "|female") % 3 == 0;
		var card = new DirectoryCard {
			Role = RolodexRole.Press, Key = key, Name = MediaPress.ContactName(outlet, role), Title = MediaPress.RoleTitle(outlet, role),
			Firm = outlet.Name, Place = outlet.HomeCityId == null ? "National" : DistanceModel.GetCityById(outlet.HomeCityId)?.name,
			Figure = Portraits.Businessman(key, female), GoToTab = "OFFICE", GoToLabel = "SEND A SHEET FROM THE OFFICE",
		};
		string rhythm = outlet.Cadence switch {
			PressCadence.Daily => "Daily; the music items fit in a few inches a week.",
			PressCadence.Monthly => $"Monthly, closing about {outlet.LeadDays / 7} weeks before it reaches the stands.",
			_ => $"Weekly, out {outlet.IssueDay}s; copy closes about {outlet.LeadDays} days ahead.",
		};
		card.Facts.Add((rhythm, false));
		card.Facts.Add((outlet.Family switch {
			PressFamily.LocalDaily => "Wants a home-town angle: a local act, a local record business, a show nearby.",
			PressFamily.Specialist => "Writes for its own readers, and they listen to it. " + (outlet.Note ?? ""),
			PressFamily.Trade => "Wants business evidence: orders, regional action, distribution. Figures get checked.",
			PressFamily.TeenFan => "Prints the faces the readers write in about.",
			PressFamily.RockCriticism => "Responds to the record itself, and may not like it.",
			_ => "Covers popular music only when it's become a story about everyone.",
		}, false));
		var recent = MediaAtmosphereService.Stories.Where(s => s.OutletId == outlet.Id && s.Status == StoryStatus.Scheduled && s.IssueDay < BroadcastExposure.DayIndex(today))
			.OrderByDescending(s => s.IssueDay).Take(2).Select(s => MediaPress.Headline(s)).ToList();
		if (recent.Count > 0) card.Facts.Add(("Recently ran: " + string.Join("; ", recent) + ".", false));
		AddMemory(card, MediaAtmosphereService.ContactFor(key));
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
