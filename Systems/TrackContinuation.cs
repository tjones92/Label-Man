using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Preserves actual mutable track aliases, never inferred from a source/master ID.</summary>
public static class TrackContinuation {
	private sealed record Slot(string Path, AlbumTrack Track, Action<AlbumTrack> Set);
	private static List<Slot> Slots(WorldSaveData world) {
		var slots = new List<Slot>();
		void AlbumSlots(string path, Album album) {
			if (album == null) return;
			void Tracks(string field, AlbumTrack[] tracks) {
				if (tracks == null) return;
				for (int i = 0; i < tracks.Length; i++) {
					int index = i;
					if (tracks[i] != null) slots.Add(new(path + "/" + field + "/" + i, tracks[i], t => tracks[index] = t));
				}
			}
			Tracks("refs", album.trackRefs); Tracks("originals", album.nonSingleTracks);
		}
		for (int i = 0; i < world.Records.Count; i++) {
			AlbumSlots("records/" + i + "/base", world.Records[i]?.baseRecord?.album);
			AlbumSlots("records/" + i + "/runtime", world.Records[i]?.albumRuntime?.baseAlbum);
		}
		void Projects(string path, List<AlbumProject> projects) {
			if (projects == null) return;
			for (int i = 0; i < projects.Count; i++) {
				AlbumSlots(path + "/" + i + "/album", projects[i].albumRecord?.album);
				AlbumSlots(path + "/" + i + "/promo", projects[i].promoSingleRecord?.album);
			}
		}
		Projects("history", world.Competitor?.AlbumProjects);
		Projects("pending", world.Competitor?.PendingAlbumProjects);
		var programme = world.Competitor?.ReleaseProgramme;
		if (programme != null) {
			for (int i = 0; i < programme.Items.Count; i++) AlbumSlots("slate/" + i, programme.Items[i].PreparedRecord?.album);
			if (programme.Shelf != null)
				for (int i = 0; i < programme.Shelf.Count; i++) AlbumSlots("shelf/" + i, programme.Shelf[i].Record?.album);
			if (programme.OpeningMasters != null)
				foreach (var p in programme.OpeningMasters.OrderBy(p => p.Key, StringComparer.Ordinal)) {
					var tracks = p.Value;
					for (int i = 0; i < tracks.Count; i++) {
						int index = i;
						if (tracks[i] != null) slots.Add(new("opening/" + p.Key + "/" + i, tracks[i], t => tracks[index] = t));
					}
				}
		}
		foreach (var p in world.RetiredTrackArchive.OrderBy(p => p.Key, StringComparer.Ordinal)) {
			string key = p.Key;
			if (p.Value != null) slots.Add(new("archive/" + key, p.Value, t => world.RetiredTrackArchive[key] = t));
		}
		return slots;
	}
	public static List<List<string>> Capture(WorldSaveData world) => Slots(world)
		.GroupBy(s => s.Track, ReferenceEqualityComparer.Instance).Where(g => g.Count() > 1)
		.Select(g => g.Select(s => s.Path).ToList()).ToList();
	public static void Restore(WorldSaveData world, List<List<string>> aliases) {
		if (aliases == null) return; // Older experimental checkpoints retain their previous behavior.
		var slots = Slots(world).ToDictionary(s => s.Path, StringComparer.Ordinal);
		var used = new HashSet<string>(StringComparer.Ordinal);
		// Validate the entire graph before changing any references.
		foreach (var group in aliases) {
			if (group == null || group.Count < 2) throw new InvalidOperationException("Invalid track alias group.");
			foreach (string path in group)
				if (!slots.ContainsKey(path) || !used.Add(path)) throw new InvalidOperationException("Invalid track alias slot: " + path);
		}
		foreach (var group in aliases) {
			AlbumTrack canonical = slots[group[0]].Track;
			foreach (string path in group) slots[path].Set(canonical);
		}
	}
}
