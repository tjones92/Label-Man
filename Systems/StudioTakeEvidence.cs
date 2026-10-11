using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

/// <summary>Frozen, attributed listening impressions. These are evidence, never the take's true ratings.
/// Observation draws are keyed independently and cannot perturb gameplay RNG or be improved by refreshing.</summary>
public static class StudioTakeEvidence {
    private static readonly string[] Delivery = {
        "The delivery feels guarded; I would try another pass.",
        "There is something here, though the delivery still feels uneven.",
        "The performance carries the song; I could keep this one.",
        "This pass has conviction. I would be happy to commit it."
    };
    private static readonly string[] Capture = {
        "The vocal is hard to follow in the room balance.",
        "The band sometimes crowds the vocal, but the words come through.",
        "The vocal and the band sit together clearly enough to finish.",
        "The balance sounds clear and settled to me."
    };

    public static void Observe(PlayerDesk.PendingSession session, PlayerDesk.SessionCut cut, int side, float producerCraft, float performerCraft) {
        string key = session.SessionId ?? $"legacy:{session.ArtistId}:{session.Date}:{session.Tier}:{session.Hours}";
        foreach (var take in cut.Takes) {
            string takeKey = $"{key}|{side}|{take.Number}";
            take.ProducerNote ??= Delivery[Read(take.Hook, producerCraft, takeKey + "|producer")];
            take.EngineerNote ??= Capture[Read(take.Production, .65f, takeKey + "|engineer")];
            int feeling = Read(take.Hook, performerCraft, takeKey + "|performer");
            take.PerformerNote ??= feeling >= 2 ? "That pass felt like we meant it." : "I think we can settle into it more.";
        }
    }

    private static int Read(float truth, float expertise, string key) {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        double draw = ((uint)hash[0] | (uint)hash[1] << 8 | (uint)hash[2] << 16 | (uint)hash[3] << 24) / (double)uint.MaxValue;
        double uncertainty = .10 + .18 * (1 - Math.Clamp(expertise, 0f, 1f));
        double impression = truth + (draw * 2 - 1) * uncertainty;
        return impression < .30 ? 0 : impression < .45 ? 1 : impression < .62 ? 2 : 3;
    }

    public static int Choose(IReadOnlyList<PlayerDesk.SessionTake> takes) => Enumerable.Range(0, takes.Count)
        .OrderByDescending(i => Array.IndexOf(Delivery, takes[i].ProducerNote))
        .ThenByDescending(i => Array.IndexOf(Capture, takes[i].EngineerNote)).ThenBy(i => i).First();
}
