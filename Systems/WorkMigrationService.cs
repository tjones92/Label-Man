using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Musicians move for work and contacts (SimTools/ContactNetworkDirective.md §5.2). At the band-life year end, a
/// pooled player who found no work all year (no sessions, no house-band sets) may move to a town where someone they
/// know is based and where the session and club trade is busier than at home: the way players reached New York,
/// Nashville or Los Angeles, on a friend's word that there was work. The move is a dated personal decision: keyed
/// consent, paid from their own wealth, a two-year cooldown, domestic only. Their origin never changes. No RNG.
/// <c>--disable-work-migration</c> turns it off.
/// </summary>
public static class WorkMigrationService {
	/// <summary>A move's cost in 1960 dollars: a month's room (~$40) and the bus fare (Greyhound ran ~2.5 cents a mile
	/// in 1960). Provisional; CPI-adjusted by year.</summary>
	public const float MoveBase1960 = 40f, MovePerMile1960 = 0.025f;
	/// <summary>A destination needs at least this many working players (session or house-band) to be worth the trip.</summary>
	public const int MinimumWorkers = 8;
	/// <summary>No second move within this many years of the last.</summary>
	public const int CooldownYears = 2;

	public static bool Enabled { get; private set; } = true;

	// Diagnostics, not saved.
	public static int Considered { get; private set; }
	public static int Declined { get; private set; }
	public static int Unaffordable { get; private set; }
	public static int Moved { get; private set; }
	public static readonly List<WorkMove> Log = new();
	public sealed record WorkMove(int Year, string PersonId, string From, string To, string ContactId, double Miles, float Cost, int DestinationWorkers, int HomeWorkers);

	public static void Configure(string[] args) {
		bool enable = args.Contains("--enable-work-migration", StringComparer.Ordinal);
		bool disable = args.Contains("--disable-work-migration", StringComparer.Ordinal);
		if (enable && disable) throw new ArgumentException("--enable-work-migration and --disable-work-migration cannot be used together.");
		Enabled = !disable;
	}
	public static void Reset() { Considered = Declined = Unaffordable = Moved = 0; Log.Clear(); }

	private static string Scene(Musician m) => LocalScenePersistenceService.SceneIdFor(m?.geography?.basePlaceId);
	private static bool Working(string person, int year) =>
		SessionEmploymentService.WorkedIn(person, year) || LocalSceneRoomService.Backing(person, year).Hours > 0f;

	/// <summary>Consent: ambition pushes, children and road fatigue hold back, and older players stay put.</summary>
	public static float ConsentChance(Musician m, int year) => Math.Clamp(.25f + .35f * m.ambition - (m.hasChildren ? .20f : 0f)
		- .15f * m.fatigue - (m.GetAge(year) >= 40 ? .15f : 0f), .05f, .80f);

	/// <summary>Year end, after the pool expires its stale members and before contacts are pruned.</summary>
	public static void OnYearEnd(int year) {
		if (!Enabled || !ContactNetworkService.Enabled || !LocalScenes.Persisting || ArtistManager.Instance == null) return;
		// Working players by town this year: session and house-band work alike.
		var workers = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (string id in SessionEmploymentService.Accounts.Where(a => a.Year == year).Select(a => a.PersonId)
			.Concat(LocalSceneRoomService.BackersIn(year)).Distinct(StringComparer.Ordinal)) {
			string scene = Scene(ArtistManager.Instance.GetMusician(id));
			if (scene != null) workers[scene] = workers.GetValueOrDefault(scene) + 1;
		}
		foreach (PooledPerson p in PersonPool.Ordered().ToList()) {
			Musician m = p.person;
			if (m.lifeState != MemberLifeState.Active || Working(m.personId, year)) continue;
			string home = Scene(m);
			if (home == null || ScenePlaceRegistry.Get(home)?.CountryCode != "US") continue;
			if (m.geography.moves?.Any(x => x.year > year - CooldownYears) == true) continue;
			int homeWork = workers.GetValueOrDefault(home);
			// Where the people they know (without a fallout) are based, weighted by how much work they shared.
			var leads = new Dictionary<string, (int Jobs, string Contact, int ContactJobs)>(StringComparer.Ordinal);
			foreach (string other in ContactNetworkService.ContactsOf(m.personId)) {
				var edge = ContactNetworkService.Edge(m.personId, other);
				var friend = ArtistManager.Instance.GetMusician(other);
				if (edge == null || edge.Fallout || friend?.lifeState != MemberLifeState.Active) continue;
				string there = Scene(friend);
				if (there == null || there == home || ScenePlaceRegistry.Get(there)?.CountryCode != "US") continue;
				if (workers.GetValueOrDefault(there) < MinimumWorkers || workers.GetValueOrDefault(there) <= homeWork) continue;
				var lead = leads.GetValueOrDefault(there);
				// The person who tells them about it: the one they worked with most.
				bool closer = lead.Contact == null || edge.Jobs > lead.ContactJobs ||
					edge.Jobs == lead.ContactJobs && string.CompareOrdinal(other, lead.Contact) < 0;
				leads[there] = (lead.Jobs + edge.Jobs, closer ? other : lead.Contact, closer ? edge.Jobs : lead.ContactJobs);
			}
			if (leads.Count == 0) continue;
			var pick = leads.Select(kv => (Place: kv.Key, kv.Value.Jobs, kv.Value.Contact,
					Score: Math.Log(1 + kv.Value.Jobs) + Math.Log(1 + workers.GetValueOrDefault(kv.Key))))
				.OrderByDescending(x => x.Score).ThenBy(x => x.Place, StringComparer.Ordinal).First();
			if (!ScenePlaceRegistry.TryDomesticRoadMiles(home, pick.Place, out double miles) || !(miles > 0)) continue;
			Considered++;
			if (BandLife.Unit($"work-move|{m.personId}|{pick.Place}|{year}") >= ConsentChance(m, year)) { Declined++; continue; }
			float cost = (MoveBase1960 + MovePerMile1960 * (float)miles) * SceneLiveEconomics.PriceLevel(year);
			if (m.wealth < cost) { Unaffordable++; continue; }
			m.wealth -= cost;
			var friendName = ArtistManager.Instance.GetMusician(pick.Contact)?.FullName ?? "a contact";
			(m.geography.moves ??= new()).Add(new PlaceMove { fromPlaceId = m.geography.basePlaceId, toPlaceId = pick.Place,
				year = year, month = 12, day = 31, precision = PlaceDatePrecision.Year, evidence = PlaceEvidence.Simulated,
				reason = $"No work at home; {friendName} knew of session and club work" });
			m.geography.basePlaceId = pick.Place;
			m.geography.baseEvidence = PlaceEvidence.Simulated;
			p.homeRegion = ScenePlaceRegistry.Get(pick.Place)?.MarketRegionId ?? p.homeRegion;
			Moved++;
			Log.Add(new WorkMove(year, m.personId, home, pick.Place, pick.Contact, miles, cost, workers.GetValueOrDefault(pick.Place), homeWork));
		}
	}
}
