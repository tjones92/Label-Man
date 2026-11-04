using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public static class BudgetLpCatalogueChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool ok, string name) { if (!ok) throw new InvalidOperationException("Budget LP: " + name); checks++; }
		var labels = new[] {
			new AILabel { labelId = "a", tier = LabelTier.Major },
			new AILabel { labelId = "closed", status = LabelStatus.Defunct },
			new AILabel { labelId = "player", isPlayerOwned = true }
		};
		RecordRuntimeData Album(string id, int age, int units = 100, string label = "a") => new(new Record {
			recordId = id, labelId = label, artistId = "act", format = ReleaseFormat.Album, primaryGenre = Genre.TraditionalPop
		}) { weeksSinceRelease = age, totalUnitsSold = units };
		var live = Album("live", 52);
		var records = new List<RecordRuntimeData> { live, Album("young", 51), Album("zero", 60, 0), Album("closed", 60, label: "closed") };
		var player = Album("player", 60); player.baseRecord.isPlayerOwned = true; records.Add(player);
		var single = Album("single", 60); single.baseRecord.format = ReleaseFormat.Single; records.Add(single);
		var state = new RecordClubState();
		state.Entries["a|act"] = new RecordClubEntry { LabelId = "a", ArtistId = "act", Titles = 2, Appeal = 200, AdultAppeal = 80 };
		state.Entries["closed|act"] = new RecordClubEntry { LabelId = "closed", ArtistId = "act", Titles = 1, Appeal = 50 };
		state.OpeningSeed["a"] = new RecordClubSeed { Appeal = 300, AdultAppeal = 150 };
		state.OpeningSeed["player"] = new RecordClubSeed { Appeal = 300 };
		string before = JsonSerializer.Serialize(state.Normalized());
		var rows = BudgetLpCatalogue.Snapshot(state, labels, records, 1961);
		Check(rows.Count == 3 && rows.All(r => r.LabelId == "a"), "inactive, player and ineligible live supply excluded");
		Check(rows.Single(r => r.Kind == "LiveAged52").Titles == 1 && rows.Single(r => r.Kind == "LiveAged52").Appeal == 100,
			"52-week screen includes the boundary, excludes younger and zero-sale records");
		Check(rows.Single(r => r.Kind == "Retired").Titles == 2 && rows.Single(r => r.Kind == "Retired").Appeal == 200,
			"retired aggregates remain distinct from live supply for the same act");
		Check(rows.Single(r => r.Kind == "OpeningSynthetic").Titles == null && rows.Single(r => r.Kind == "OpeningSynthetic").ArtistId == "",
			"opening supply does not invent titles or acts");
		Check(JsonSerializer.Serialize(state.Normalized()) == before && live.totalUnitsSold == 100 && live.rackUnitsThisWeek == 0,
			"snapshot leaves saved state and live sales untouched");
		var jsonOptions = new JsonSerializerOptions { IncludeFields = true };
		Check(JsonSerializer.Serialize(rows, jsonOptions) == JsonSerializer.Serialize(BudgetLpCatalogue.Snapshot(state, labels.Reverse(), records.AsEnumerable().Reverse(), 1961), jsonOptions),
			"source ordering is deterministic");
		// Migrate a retiring title: the next snapshot counts it once in the retired bucket.
		records.Remove(live); state.Entries["a|act"].Titles++; state.Entries["a|act"].Appeal += 100;
		var after = BudgetLpCatalogue.Snapshot(state, labels, records, 1961);
		Check(after.Count == 2 && after.Single(r => r.Kind == "Retired").Titles == 3 && after.Sum(r => r.Appeal) == rows.Sum(r => r.Appeal),
			"retirement migrates supply without duplicating it");
		Check(BudgetLpCatalogue.Snapshot(null, labels, Array.Empty<RecordRuntimeData>(), 1961).Count == 0, "missing ledger stays empty");
		Godot.GD.Print($"BUDGET_LP_CATALOGUE_CHECKS_OK {checks}");
	}
}
