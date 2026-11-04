using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

public static class RecordClubChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException("Record clubs: " + name); checks++; }
		AILabel Label(string id, string name, bool historical = false, LabelTier tier = LabelTier.Major) =>
			new AILabel { labelId = id, labelName = name, isHistorical = historical, tier = tier };
		RecordRuntimeData Album(string id, string labelId, string artistId, int units, int thisWeek, int age, Genre genre = Genre.TraditionalPop) =>
			new RecordRuntimeData {
				baseRecord = new Record { recordId = id, labelId = labelId, artistId = artistId, format = ReleaseFormat.Album, primaryGenre = genre },
				totalUnitsSold = units, unitsThisWeek = thisWeek, weeksSinceRelease = age
			};
		try {
			// Switches.
			bool Rejects(params string[] args) {
				try { RecordClubChannel.ResetForChecks(); RecordClubChannel.Configure(args); return false; } catch (ArgumentException) { return true; }
			}
			Check(Rejects(RecordClubChannel.HoldSwitch), "the hold needs the channel");
			Check(!Rejects(RecordClubChannel.Switch, RecordClubChannel.HoldSwitch) && RecordClubChannel.HoldUnits, "hold arm configures");

			// Volume: the Billboard series, exact over 52 weeks; 1960 and 1969 hold their neighbours.
			Check(RecordClubChannel.AnnualUnits(1961) == 45_000_000 && RecordClubChannel.AnnualUnits(1964) == 38_000_000 &&
				RecordClubChannel.AnnualUnits(1968) == 49_000_000, "sourced years");
			Check(RecordClubChannel.AnnualUnits(1960) == 45_000_000 && RecordClubChannel.AnnualUnits(1969) == 49_000_000, "held years");
			for (int year = 1960; year <= 1969; year++)
				Check(Enumerable.Range(1, 52).Sum(w => RecordClubChannel.WeeklyUnits(year, w)) == RecordClubChannel.AnnualUnits(year), "52 weeks sum " + year);

			// Largest remainder: exact, stable, ties by key, non-positive weights excluded.
			var split = RecordClubChannel.LargestRemainder(new List<(string, double)> { ("b", 1), ("a", 1), ("c", 1), ("z", 0), ("n", double.NaN) }, 100);
			Check(split.Values.Sum() == 100 && split["a"] == 34 && split["b"] == 33 && split["c"] == 33 && !split.ContainsKey("z") && !split.ContainsKey("n"), "largest remainder");
			Check(RecordClubChannel.LargestRemainder(new List<(string, double)> { ("a", 1) }, 0).Count == 0, "nothing to split");

			// Off: no allocation, no saved state, no folding.
			RecordClubChannel.SetForChecks(false);
			var columbia = Label("c", "Columbia", true); var rca = Label("r", "RCA Victor Records", true);
			var indie = Label("i", "Indie", false, LabelTier.Independent); var player = Label("p", "Player"); player.isPlayerOwned = true;
			var gone = Label("g", "Gone", false, LabelTier.MidTier);
			var labels = new List<AILabel> { columbia, rca, indie, player, gone };
			var records = new List<RecordRuntimeData> {
				Album("c1", "c", "act-c", 40000, 2000, 30), Album("r1", "r", "act-r", 20000, 1000, 30, Genre.RockAndRoll),
				Album("i1", "i", "act-i", 10000, 500, 30), Album("i2", "i", "act-i2", 5000, 250, 5), Album("g1", "g", "act-g", 8000, 400, 30)
			};
			Check(RecordClubChannel.Allocate(labels, records, 1961).Count == 0 && RecordClubChannel.Capture() == null, "off is empty");
			RecordClubChannel.FoldRetired(records[0], 1961);
			Check(RecordClubChannel.State == null, "off never folds");

			// On: operators are the historical club owners; the backlist is seeded once.
			RecordClubChannel.SetForChecks(true);
			var week = RecordClubChannel.Allocate(labels, records, 1961);
			var state = RecordClubChannel.State;
			Check(state.OperatorIds.SequenceEqual(new[] { "c", "r" }), "historical operators");
			Check(state.OpeningSeed["c"].Appeal == 2000 * RecordClubChannel.OpeningSeedWeeks && !state.OpeningSeed.ContainsKey("p"), "opening backlist");
			long weekly = RecordClubChannel.WeeklyUnits(1961, 1);
			long package = (long)Math.Round(weekly * RecordClubChannel.PackageShare);
			Check(week.Values.Sum(w => w.OwnUnits + w.LicensedInUnits) == weekly - package, "club slice exact");
			Check(week.Values.Sum(w => w.PackageUnits) == package, "package slice exact");
			Check(week.Values.Sum(w => w.LicensedInUnits) == week.Values.Sum(w => w.LicensedOutUnits), "licensed units balance");
			Check(!week.ContainsKey("p"), "the player's label is outside the AI channel");
			Check(state.Licences.Count == 2 && state.Licences.All(l => l.ExpiresYear == 1961 + RecordClubChannel.LicenceTermYears) &&
				state.Licences.Select(l => l.LicensorId).OrderBy(x => x).SequenceEqual(new[] { "g", "i" }), "licences go to the strongest catalogues");
			Check(state.Licences.Single(l => l.LicensorId == "i").OperatorId == "c", "the stronger club picks first");
			Check(week["i"].LicensedOutUnits > 0 && week["c"].LicensedInUnits > 0, "licensed masters sell through the club");
			Check(week["c"].Artists.Any(a => a.ArtistId == "act-c") && week.Values.SelectMany(w => w.Artists).All(a => a.ArtistId != ""), "acts are attributed; the backlist is not");
			Check(week["i"].Artists.All(a => a.ArtistId != "act-i2"), "the holdback keeps new LPs out");

			// Folding, decay and pruning.
			RecordClubChannel.FoldRetired(records[2], 1961);
			RecordClubChannel.FoldRetired(new RecordRuntimeData { baseRecord = new Record { labelId = "i", artistId = "s", format = ReleaseFormat.Single }, totalUnitsSold = 9 }, 1961);
			RecordClubChannel.FoldRetired(new RecordRuntimeData { baseRecord = new Record { labelId = "i", artistId = "z", format = ReleaseFormat.Album }, totalUnitsSold = 0 }, 1961);
			Check(state.Entries.Count == 1 && state.Entries["i|act-i"].Titles == 1 && state.Entries["i|act-i"].Appeal == 10000, "LPs fold once; singles and unsold LPs do not");
			Check(Math.Abs(state.Entries["i|act-i"].AdultAppeal - 10000d * RecordClubChannel.AdultShare(Genre.TraditionalPop, 1961)) < 1e-6, "adult appeal by genre");
			RecordClubChannel.Fold("i", "tiny", 26, 1f);
			for (int w = 0; w < 52; w++) RecordClubChannel.Allocate(labels, records, 1961);
			Check(Math.Abs(state.Entries["i|act-i"].Appeal - 10000 * RecordClubChannel.AnnualRetention) < 1e-6, "a year's decay");
			Check(!state.Entries.ContainsKey("i|tiny"), "dust is pruned");

			// Save: capture is canonical, round-trips exactly and refuses to resume with the channel off.
			var options = SaveGameService.TestJsonOptions;
			string a = JsonSerializer.Serialize(RecordClubChannel.Capture(), options);
			RecordClubChannel.Restore(JsonSerializer.Deserialize<RecordClubState>(a, options));
			Check(JsonSerializer.Serialize(RecordClubChannel.Capture(), options) == a, "state round-trip");
			var saved = RecordClubChannel.Capture();
			RecordClubChannel.SetForChecks(false);
			bool refused = false;
			try { RecordClubChannel.Restore(saved); } catch (InvalidOperationException) { refused = true; }
			Check(refused, "a club world must resume with the channel on");
			RecordClubChannel.SetForChecks(true);
			RecordClubChannel.Restore(saved);

			// Closure drops a catalogue at the next renewal; absorption merges it and passes the club on.
			gone.status = LabelStatus.Bankrupt;
			RecordClubChannel.Allocate(labels, records, 1962);
			Check(RecordClubChannel.State.Licences.All(l => l.LicensorId != "g"), "a closed licensor loses its licence");
			Check(RecordClubChannel.State.OpeningSeed.Keys.All(k => k != "g") && RecordClubChannel.State.Entries.Values.All(e => e.LabelId != "g"), "closure drops the catalogue");
			var restoredState = RecordClubChannel.State;
			double rcaSeed = restoredState.OpeningSeed["r"].Appeal, columbiaSeed = restoredState.OpeningSeed["c"].Appeal;
			RecordClubChannel.OnAcquired("r", "c");
			Check(restoredState.OperatorIds.SequenceEqual(new[] { "c" }), "the acquirer keeps one club");
			Check(Math.Abs(restoredState.OpeningSeed["c"].Appeal - (rcaSeed + columbiaSeed)) < 1e-6 && !restoredState.OpeningSeed.ContainsKey("r"), "backlists merge");
			Check(restoredState.Entries.Values.All(e => e.LabelId != "r"), "the absorbed label's catalogue moves");
			Check(restoredState.Licences.All(l => l.OperatorId != "r"), "licences follow the club");

			// Hold arm: the club's weekly volume leaves the album channel in proportion.
			Check(RecordClubChannel.HeldAlbumChannelCapacity(1000, 1_000_000, 1961) == 1000, "no hold without the switch");
		} finally {
			RecordClubChannel.ResetForChecks();
			RecordClubChannel.Configure(OS.GetCmdlineUserArgs());
		}
		GD.Print($"RECORD_CLUB_CHECK passed checks={checks}");
	}
}
