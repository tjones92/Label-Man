using Godot;

public partial class PlayerDesk {
	public enum ShopRepertoire { Popular, RhythmAndBlues, Country, Jazz, Folk, Gospel, Latin, Classical }

	public static bool KnowsRecordShop(PlayerStop shop) => shop.Relationship > 0f || shop.LastVisitWeek > 0;

	public static string ShopWindow(PlayerStop shop) => shop.CrowdSize switch {
		< 0.33f => "One narrow window. The clerk can reach the whole singles rack without leaving the till.",
		< 0.67f => "A row of new 45s in the window, and a steady succession of people at the counter.",
		_ => "Two front windows, a listening counter, and customers waiting for the clerk to look up."
	};

	public static string ShopClerk(PlayerStop shop) {
		string stock = shop.Repertoire switch {
			ShopRepertoire.RhythmAndBlues => "Rhythm and blues. They already know which side they want when they walk in.",
			ShopRepertoire.Country => "Country. Someone hears a side on the radio on the way to work and asks for it at lunch.",
			ShopRepertoire.Jazz => "Jazz. Most of my regulars come in asking who played on it.",
			ShopRepertoire.Folk => "Folk. A few of them come straight from the coffeehouse and ask for what they just heard.",
			ShopRepertoire.Gospel => "Gospel. I put a few aside for the church crowd every week.",
			ShopRepertoire.Latin => "Latin records. The dancers ask for an orchestra by name.",
			ShopRepertoire.Classical => "Classical. Most of my regulars have a particular performance in mind.",
			_ => "The hit 45s. The school crowd keeps me busy after three."
		};
		return shop.Specialist ? $"“Most of the people who walk in here want one thing. {stock}”"
			: $"“A bit of everything. What do my regulars ask for? {stock}”";
	}

	public bool VisitRecordShop(string stopId, out string message) {
		var shop = GetStop(stopId);
		if (Label == null || IsGameOver || shop?.Kind != StopKind.Shop || shop.CityId != CurrentCityId) {
			message = "You have to be in town to call on that shop.";
			return false;
		}
		if (KnowsRecordShop(shop)) { message = "You've already met the clerk."; return false; }
		if (!RequireErrand(1, out message)) return false;
		SpendErrand(1);
		TouchStop(shop, 0.025f);
		workedCities.Add(shop.CityId);
		message = $"You introduced the label at {shop.DisplayName}.";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	public string ShopRecordRead(PlayerStop shop, string recordId) {
		if (!KnowsRecordShop(shop) || InstinctProfile.TheStreet < 3) return null;
		return ShopCustomerFit(shop, recordId) switch {
			> 1f => "The Street: That's the kind of side this crowd comes in asking for.",
			< 0.8f => "The Street: The clerk could like it fine and still have nobody to put it in front of.",
			_ => null
		};
	}

	private float ShopCustomerFit(PlayerStop shop, string recordId) => shop?.Kind == StopKind.Shop && !string.IsNullOrEmpty(recordId)
		? ShopCustomerFit(shop, GenreForRecord(recordId)) : 1f;

	private string CounterPassReason(PlayerStop stop, string recordId, float access) {
		if (ShopCustomerFit(stop, recordId) < 0.8f) return "I don't have the customers for that one";
		return access < 0.6f ? "that trade hasn't found its way to this side of town yet" : "come back once it's played somewhere";
	}

	private static float ShopCustomerFit(PlayerStop shop, Genre genre) {
		var family = GenreCatalog.Get(GenreCatalog.MapLegacy(genre)).Family;
		bool fits = shop.Repertoire switch {
			ShopRepertoire.RhythmAndBlues => family is GenreFamily.RhythmAndSoul or GenreFamily.Blues,
			ShopRepertoire.Country => family == GenreFamily.Country,
			ShopRepertoire.Jazz => family == GenreFamily.Jazz,
			ShopRepertoire.Folk => family == GenreFamily.Folk,
			ShopRepertoire.Gospel => family == GenreFamily.Gospel,
			ShopRepertoire.Latin => family == GenreFamily.Latin,
			ShopRepertoire.Classical => family == GenreFamily.Classical,
			_ => family is GenreFamily.Pop or GenreFamily.Rock
		};
		if (shop.Specialist) return fits ? 1.25f : 0.55f;
		return fits ? 1.12f : 1f;
	}
}
