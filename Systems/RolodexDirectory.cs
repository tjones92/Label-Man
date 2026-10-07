using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>The identity function of a person in the book. Each is a tab of the Rolodex.</summary>
public enum RolodexRole { Deejay, Manager, Publisher, Plant, Distributor, Dealer }

/// <summary>One card in the book. Deejay cards carry the playable <see cref="RolodexEntry"/>; the rest are the
/// people behind things the player already deals with, read off real state.</summary>
public sealed class DirectoryCard {
	public RolodexRole Role;
	/// <summary>Stable id: seeds the face and the name, so the same person looks the same every time the book is opened.</summary>
	public string Key;
	public string Name, Title, Firm, Place;
	public List<(string Text, bool Warn)> Facts = new();
	public RolodexEntry Entry;
	public string GoToTab, GoToLabel;
	public PortraitFigure Figure;
}

/// <summary>
/// The Rolodex beyond the disc jockeys: the managers across the table, the publishers' song pluggers, the foreman at the
/// pressing plant, the distributor's man, the shop and jukebox accounts. None of this is new simulation. A manager is
/// the name already on an act; the plant, the distributor and the accounts are the plant orders, the distribution deal
/// and the stops the desk already runs. What is new is the person: a stable name and a face, derived from an FNV hash
/// of the thing they belong to, never from a random stream, so building the book cannot move a seeded run.
/// </summary>
public static class RolodexDirectory {
	private static readonly string[] MaleFirst = { "Al", "Arnold", "Bernie", "Carl", "Chet", "Dave", "Eddie", "Frank", "Gene", "Hal", "Harvey", "Irv", "Jack", "Jerry", "Lou",
		"Marty", "Mel", "Morrie", "Nate", "Norm", "Ozzie", "Paul", "Ray", "Sid", "Sol", "Stan", "Tony", "Vince", "Walt", "Wes" };
	private static readonly string[] FemaleFirst = { "Barbara", "Dolores", "Edna", "Flo", "Gloria", "Irene", "Joan", "Lois", "Marge", "Nell", "Pat", "Ruth", "Sylvia", "Vera", "Wanda" };
	private static readonly string[] Surnames = { "Abrams", "Baxter", "Bellamy", "Brandt", "Castellano", "Dunleavy", "Ellison", "Fenwick", "Gallo", "Hartigan", "Imhoff", "Jessup",
		"Kowalski", "Lindqvist", "Maddox", "Novak", "O'Hara", "Pruitt", "Quinlan", "Rosenthal", "Stavros", "Tillman", "Underhill", "Vance", "Whitcomb", "Yarborough", "Zeller",
		"Moretti", "Pelletier", "Bascombe" };
	private static readonly string[] PlantNames = { "Apex Record Pressing", "Monarch Plastics", "Allied Pressing Company", "Bell-Tone Processing", "Keystone Matrix & Pressing" };

	public static string PersonName(string key, bool female = false) {
		uint h = Portraits.Hash(key);
		string[] pool = female ? FemaleFirst : MaleFirst;
		return $"{pool[h % (uint)pool.Length]} {Surnames[(h / 31u) % (uint)Surnames.Length]}";
	}

	public static string TabName(RolodexRole role) => role switch {
		RolodexRole.Deejay => "DEEJAYS",
		RolodexRole.Manager => "MANAGERS",
		RolodexRole.Publisher => "PUBLISHERS",
		RolodexRole.Plant => "PLANT",
		RolodexRole.Distributor => "DISTRIBUTOR",
		_ => "SHOPS & OPS",
	};

	public static string EmptyNote(RolodexRole role) => role switch {
		RolodexRole.Deejay => "Nobody in the business knows your name yet. Work the phones.",
		RolodexRole.Manager => "None of your acts, or anyone you have on the pad, is managed by anybody.",
		RolodexRole.Publisher => "The publishers have not been catalogued yet.",
		RolodexRole.Plant => "You have no label to press for.",
		RolodexRole.Distributor => "You have no distribution deal. Nobody is carrying your records but you.",
		_ => "No shop or jukebox operator has taken a record from you yet. Get out of the office and work a town.",
	};

	public static List<DirectoryCard> Cards(PlayerDesk desk, RolodexRole role) {
		if (desk == null) return new List<DirectoryCard>();
		switch (role) {
			case RolodexRole.Manager: return Managers(desk);
			case RolodexRole.Publisher: return Publishers();
			case RolodexRole.Plant: return Plant(desk);
			case RolodexRole.Distributor: return Distributor(desk);
			case RolodexRole.Dealer: return Dealers(desk);
			default: return new List<DirectoryCard>();
		}
	}

	public static int Count(PlayerDesk desk, RolodexRole role) =>
		role == RolodexRole.Deejay ? desk?.Rolodex.Count ?? 0 : Cards(desk, role).Count;

	// ---- managers ----------------------------------------------------------------------------------------

	private static List<DirectoryCard> Managers(PlayerDesk desk) {
		var seen = new HashSet<string>(StringComparer.Ordinal);
		var cards = new List<DirectoryCard>();
		IEnumerable<SimulatedArtist> acts = desk.Roster.Concat(desk.Notebook.Select(entry => entry.Artist).Where(a => a != null));
		foreach (SimulatedArtist act in acts) {
			if (act.manager == ManagerArchetype.None || string.IsNullOrEmpty(act.managerName) || !seen.Add(act.managerName)) continue;
			(_, string helps, string costs) = ManagerProfile.Describe(act.manager);
			bool yours = desk.Roster.Any(a => a.artistId == act.artistId);
			var card = new DirectoryCard {
				Role = RolodexRole.Manager, Key = "mgr:" + act.managerName, Name = act.managerName, Title = "Personal manager",
				Firm = $"for {act.stageName}", Place = yours ? "your roster" : "on your A&R pad",
				Figure = Portraits.Businessman("mgr:" + act.managerName),
				GoToTab = yours ? "ROSTER" : null, GoToLabel = "OPEN THE ROSTER"
			};
			card.Facts.Add(("What they do for you: " + helps, false));
			card.Facts.Add(("What they cost you: " + costs, false));
			card.Facts.Add(("The manager follows the act. When the contract comes up, the same man is across the table.", false));
			cards.Add(card);
		}
		return cards;
	}

	// ---- publishers --------------------------------------------------------------------------------------

	private static string SceneName(PublishingScene scene) => scene switch {
		PublishingScene.NewYorkPopFactory => "New York",
		PublishingScene.Nashville => "Nashville",
		PublishingScene.LosAngelesPop => "Los Angeles",
		PublishingScene.LegacyTinPanAlley => "New York, Tin Pan Alley",
		PublishingScene.DetroitInHouse => "Detroit",
		_ => "",
	};

	private static string Level(float value, string low, string mid, string high) => value < 0.42f ? low : value < 0.62f ? mid : high;

	private static List<DirectoryCard> Publishers() {
		var cards = new List<DirectoryCard>();
		foreach (MusicPublisher house in CompositionCatalogService.Publishers) {
			bool female = Portraits.Hash("plug:" + house.publisherId) % 3u == 0u;
			var card = new DirectoryCard {
				Role = RolodexRole.Publisher, Key = "pub:" + house.publisherId, Name = PersonName("plug:" + house.publisherId, female), Title = "Professional manager",
				Firm = house.publisherName, Place = SceneName(house.scene),
				Figure = Portraits.Businessman("plug:" + house.publisherId, female),
				GoToTab = "ROSTER", GoToLabel = "OPEN THE ROSTER (TEACH A COVER)"
			};
			string focus = house.focusGenres.Length == 0 ? "no one style" : string.Join(", ", house.focusGenres.Take(3).Select(g => GenreNameFormatter.Format(g)));
			card.Facts.Add(($"Plugs songs in: {focus}.", false));
			card.Facts.Add((Level(house.catalogQuality, "A thin catalogue.", "A catalogue with some good copyrights in it.", "A catalogue other houses envy."), false));
			card.Facts.Add((Level(house.commercialAggression, "Does not push a song hard.", "Works the songs he has.", "Will leave a demo on every desk in town."), false));
			card.Facts.Add((Level(house.artistFriendly, "Writers say he keeps most of the money.", "Fair enough to a writer.", "Writers like working for him."), false));
			card.Facts.Add((Level(house.buyoutWillingness, "Will not sell a copyright.", "Will talk about a buyout, at a price.", "Will sell a copyright outright if the cheque is right."), false));
			cards.Add(card);
		}
		return cards;
	}

	// ---- the pressing plant ------------------------------------------------------------------------------

	private static List<DirectoryCard> Plant(PlayerDesk desk) {
		AILabel label = desk.Label;
		if (label == null) return new List<DirectoryCard>();
		string key = "plant:" + label.labelId;
		uint h = Portraits.Hash(key);
		var card = new DirectoryCard {
			Role = RolodexRole.Plant, Key = key, Name = PersonName(key), Title = "Plant foreman",
			Firm = PlantNames[h % (uint)PlantNames.Length], Place = string.IsNullOrEmpty(label.headquartersCity) ? "your town" : label.headquartersCity,
			Figure = Portraits.Foreman(key), GoToTab = "CATALOG", GoToLabel = "OPEN THE CATALOG"
		};
		var orders = desk.PendingPressings().ToList();
		if (orders.Count == 0) card.Facts.Add(("Nothing of yours is on his presses right now.", false));
		foreach (var order in orders.Take(4))
			card.Facts.Add(($"On the presses: {order.Quantity:N0} of \"{order.Title}\", due {order.Arrives.ToHeadlineString()}.", false));
		if (orders.Count > 4) card.Facts.Add(($"…and {orders.Count - 4} more runs behind those.", false));
		var credit = desk.PlantCreditOwed;
		if (credit != null)
			card.Facts.Add(($"He fronted you a run and wants ${credit.Value.Amount:N0} back in {credit.Value.WeeksAway} week{(credit.Value.WeeksAway == 1 ? "" : "s")}. He will collect whatever the record does.", true));
		card.Facts.Add(("Plating, then the press queue, then the mail: allow weeks, not days.", false));
		return new List<DirectoryCard> { card };
	}

	// ---- the distributor ---------------------------------------------------------------------------------

	private static List<DirectoryCard> Distributor(PlayerDesk desk) {
		DistributionDeal deal = desk.Label?.activeDeal;
		if (deal == null) return new List<DirectoryCard>();
		AILabel firm = ChartManager.Instance?.GetLabelById(deal.distributorId);
		string key = "dist:" + deal.distributorId;
		var card = new DirectoryCard {
			Role = RolodexRole.Distributor, Key = key, Name = PersonName(key), Title = "Branch manager",
			Firm = firm?.labelName ?? "Your distributor", Place = firm == null ? "" : (ChartManager.Instance?.GetRegionById(firm.homeRegion)?.regionName ?? ""),
			Figure = Portraits.Businessman(key), GoToTab = "DISTRIBUTION", GoToLabel = "OPEN DISTRIBUTION"
		};
		int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
		int left = Math.Max(0, deal.signedWeek + deal.termWeeks - week);
		card.Facts.Add(($"Carries your records into {deal.grantedRegions?.Length ?? 0} market{((deal.grantedRegions?.Length ?? 0) == 1 ? "" : "s")}, at {deal.marginSkim * 100f:0}% off the top.", false));
		card.Facts.Add(($"The agreement has about {left} week{(left == 1 ? "" : "s")} to run.", left <= 8));
		if (deal.unrecoupedAdvance > 0f) card.Facts.Add(($"You still owe him ${deal.unrecoupedAdvance:N0} of the advance.", true));
		if (deal.ownsMasters) card.Facts.Add(("He holds the masters on what he carries.", true));
		return new List<DirectoryCard> { card };
	}

	// ---- shops, operators, one-stops ---------------------------------------------------------------------

	private static List<DirectoryCard> Dealers(PlayerDesk desk) {
		var cards = new List<DirectoryCard>();
		foreach (PlayerDesk.PlayerStop stop in desk.KnownAccounts().OrderByDescending(s => s.Relationship).ThenBy(s => s.DisplayName, StringComparer.Ordinal)) {
			string key = "stop:" + stop.StopId;
			string title = stop.Kind switch {
				PlayerDesk.StopKind.Op => "Jukebox operator",
				PlayerDesk.StopKind.OneStop => "One-stop buyer",
				_ => "Record buyer",
			};
			string city = DistanceModel.GetCityById(stop.CityId)?.name ?? "";
			var card = new DirectoryCard {
				Role = RolodexRole.Dealer, Key = key, Name = PersonName(key, stop.Kind == PlayerDesk.StopKind.Shop && Portraits.Hash(key) % 4u == 0u),
				Title = title, Firm = stop.DisplayName, Place = city,
				Figure = Portraits.Businessman(key), GoToTab = "DISTRIBUTION", GoToLabel = "OPEN DISTRIBUTION"
			};
			card.Facts.Add((stop.Relationship >= 0.6f ? "He is a friend of the house." : stop.Relationship >= 0.35f ? "He knows you and takes your calls." : stop.Relationship >= 0.12f ? "He has seen you around." : "He is still making up his mind about you.", false));
			int onHand = stop.OnHand.Values.Sum(lot => lot.Remaining);
			card.Facts.Add((onHand > 0 ? $"He is holding {onHand:N0} of your records." : "He has nothing of yours on the counter.", onHand == 0));
			card.Facts.Add((stop.LastVisitWeek > 0 ? "You have been through his door." : "You have not been in yet; he came to you.", false));
			cards.Add(card);
		}
		return cards;
	}
}
