using System;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>--contact-network-check (run after a year boundary, e.g. --weeks=60): edge invariants, session and
/// house-band ledgers, the audition sentence, the player's "ask around" tips, and save round-trips of both.</summary>
public static class ContactNetworkChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool ok, string reason) { if (!ok) throw new InvalidOperationException("CONTACT_NETWORK_CHECK_FAILED: " + reason); checks++; }
		string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);

		// Flags.
		ContactNetworkService.Configure(new[] { "--disable-contact-network", "--disable-session-employment", "--disable-employment-growth" });
		Check(!ContactNetworkService.Enabled && !SessionEmploymentService.Enabled && !SessionEmploymentService.EmploymentGrowth, "disable flags");
		bool rejected = false;
		try { ContactNetworkService.Configure(new[] { "--enable-contact-network", "--disable-contact-network" }); } catch (ArgumentException) { rejected = true; }
		Check(rejected, "contradictory contact flags rejected");
		ContactNetworkService.Configure(Array.Empty<string>());
		Check(ContactNetworkService.Enabled && SessionEmploymentService.Enabled && SessionEmploymentService.EmploymentGrowth, "defaults on");

		// Edge invariants on the live world.
		var edges = ContactNetworkService.Edges.ToList();
		Check(edges.Count > 0, "edges exist after a year");
		Check(edges.All(e => e.A != e.B && string.CompareOrdinal(e.A, e.B) < 0), "edges ordered, no self edges");
		Check(edges.All(e => ContactNetworkService.ContactsOf(e.A).Contains(e.B) && ContactNetworkService.ContactsOf(e.B).Contains(e.A)), "adjacency symmetric");
		Check(edges.Any(e => e.Kinds.HasFlag(ContactKind.FormerBandmate)) && edges.Any(e => e.Kinds.HasFlag(ContactKind.Session)) &&
			edges.Any(e => e.Kinds.HasFlag(ContactKind.HouseBand)), "all three edge kinds occur");
		Check(edges.All(e => e.Kinds != ContactKind.None && e.Jobs > 0 && e.FirstYear <= e.LastYear), "edge fields well formed");

		// Linking semantics on a synthetic pair.
		ContactNetworkService.Link("zz_probe_a", "zz_probe_b", ContactKind.Session, 1961, 2, via: "label_0001");
		ContactNetworkService.Link("zz_probe_b", "zz_probe_a", ContactKind.FormerBandmate, 1962, 3, fallout: true);
		ContactNetworkService.Link("zz_probe_a", "zz_probe_b", ContactKind.Session, 1963);
		var probe = ContactNetworkService.Edge("zz_probe_b", "zz_probe_a");
		Check(probe != null && probe.Jobs == 6 && probe.Fallout && probe.Kinds == (ContactKind.Session | ContactKind.FormerBandmate) &&
			probe.FirstYear == 1961 && probe.LastYear == 1963, "repeated work deepens one undirected edge; fallout is sticky");

		// Sessions and house bands.
		Check(SessionEmploymentService.RecordsCrewed > 0, "AI records hire session crews");
		Check(SessionEmploymentService.Accounts.All(a => a.Sessions > 0 &&
			Math.Abs(a.Pay - a.Sessions * SessionEmploymentService.ScalePerSession1960 * SceneLiveEconomics.PriceLevel(a.Year)) < 0.01f * a.Pay + 0.01f),
			"session pay is scale x sessions");
		Check(LocalSceneRoomService.HouseStanding.Any(h => h.Shows > 0), "house bands back acts in the rooms");

		// The audition sentence names a real member.
		var acts = ArtistManager.Instance.GetAllArtists().Where(a => a.lifecycleStatus == ArtistLifecycleStatus.Active && a.members != null).ToList();
		var described = acts.SelectMany(a => a.members.Where(m => m.isActive).SelectMany(m => ContactNetworkService.ContactsOf(m.personId)
				.Select(o => (act: a, member: m, other: ArtistManager.Instance.GetMusician(o)))))
			.FirstOrDefault(x => x.other != null && !x.act.members.Contains(x.other) && ContactNetworkService.Edge(x.member.personId, x.other.personId) is { Fallout: false });
		Check(described.other != null, "someone outside an act knows a member");
		string sentence = ContactNetworkService.Describe(described.other, described.act);
		Check(sentence != null && sentence.Contains(described.member.firstName), "audition note names the member they know");

		// The player asks around: tips come from real edges, never repeat, and survive a player save.
		var desk = PlayerDesk.Instance;
		var date = TimeManager.Instance.CurrentDate;
		Check(desk.FoundLabel("Contact Probe Records", described.act.geography?.basePlaceId ?? "new_york", out _), "player label founded");
		string originalLabel = described.act.labelId;
		described.act.labelId = desk.Label.labelId;
		try {
			TimeManager.Instance.RestoreClock(date, 9);
			Check(desk.AskAround(out string said), "asking around returns tips: " + said);
			var notes = desk.ContactTipNotes();
			Check(notes.Count > 0 && notes.Count <= PlayerDesk.TipsPerAsk, "tips recorded, at most three per ask");
			string firstNotes = Json(notes);
			desk.AskAround(out _);
			Check(desk.ContactTipNotes().Distinct().Count() == desk.ContactTipNotes().Count, "no duplicate tips");
			var saved = desk.CaptureState();
			var copy = JsonSerializer.Deserialize<PlayerSaveData>(Json(saved), SaveGameService.TestJsonOptions);
			Check(desk.RestoreState(copy, out string restoreMessage), "player state restores: " + restoreMessage);
			Check(Json(desk.ContactTipNotes()).Length > 0 && desk.ContactTipNotes().Any(n => firstNotes.Contains(n.Split(" · ")[1].Split(" (That")[0])),
				"tips survive the player save");
		} finally { described.act.labelId = originalLabel; }

		// World round-trip of the network and both ledgers.
		var world = new WorldSaveData();
		ContactNetworkService.CaptureWorld(world);
		string before = Json(world.ContactNetwork);
		var restored = JsonSerializer.Deserialize<WorldSaveData>(Json(world), SaveGameService.TestJsonOptions);
		ContactNetworkService.RehydrateWorld(restored);
		var again = new WorldSaveData();
		ContactNetworkService.CaptureWorld(again);
		Check(Json(again.ContactNetwork) == before, "contact network, session work and regulars round-trip byte-identically");
		GD.Print($"CONTACT_NETWORK_CHECK_PASS checks={checks} edges={ContactNetworkService.Count}");
	}
}
