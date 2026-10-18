using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>Exact ordering regression for calendar selection, including stable ties and partial/repeated enumeration.</summary>
public static class SimulationOptimizationChecks {
    public static void Run() {
        int checks = 0;
        void Check(bool condition, string message) {
            if (!condition) throw new InvalidOperationException("SIMULATION_OPTIMIZATION_CHECK_FAILED: " + message);
            checks++;
        }
        foreach (int count in new[] { 0, 1, 3, 32, 1024 }) {
            var members = Enumerable.Range(0, count).Select(i => new SimulatedArtist {
                artistId = "order-" + (i % 37).ToString("D3"), reputation = i % 11
            }).ToList();
            if (count > 3) members.Add(members[3]); // Repeated object, not just repeated ID.
            foreach (int regularEvery in new[] { 0, 1, 5 }) {
                var regulars = members.Where((a, i) => regularEvery > 0 && i % regularEvery == 0)
                    .Select(a => a.artistId).ToHashSet(StringComparer.Ordinal);
                foreach (bool exceptional in new[] { false, true }) {
                    double Key(SimulatedArtist a) => exceptional && a.reputation == 0 ? double.NaN :
                        exceptional && a.reputation == 1 ? double.PositiveInfinity : a.reputation;
                    var expected = members.OrderByDescending(Key).ThenBy(a => a.artistId, StringComparer.Ordinal).ToList();
                    var top = expected.FirstOrDefault(a => regulars.Contains(a.artistId));
                    var reference = top == null ? expected : expected.Where(a => !ReferenceEquals(a, top)).Prepend(top).ToList();
                    var actual = LocalSceneRoomService.OrderBookings(members, Key, regulars);
                    Check(reference.SequenceEqual(actual), $"full order n={count} regular={regularEvery} exceptional={exceptional}");
                    Check(reference.Take(3).SequenceEqual(actual.Take(3)), "prefix after full enumeration");
                    Check(reference.SequenceEqual(actual), "repeat enumeration");
                }
            }
        }
        GD.Print($"SIMULATION_OPTIMIZATION_CHECK_PASS checks={checks}");
    }
}
