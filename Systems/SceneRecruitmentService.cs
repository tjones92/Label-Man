using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Recruitment access, not distribution access. Numerical ranges are provisional gameplay
/// parameters. Queries never invent referrals, tours, moves, births or random-stream draws.</summary>
public static class SceneRecruitmentService {
    public readonly record struct Access(bool Eligible, string Route, string HqPlaceId, string BasePlaceId,
        float? RoadMiles, float Weight, string Evidence) {
        public float Locality => Route == "Catchment" ? 1f : Route == "LocalVisitor" ? .8f :
            Route == "RoadCircuit" ? .5f : 0f;
    }

    public static Access Explain(AILabel label, SimulatedArtist artist, int week) {
        ScenePlace hq = ScenePlaceRegistry.Get(label?.geography?.basePlaceId);
        ScenePlace home = ScenePlaceRegistry.Get(artist?.geography?.basePlaceId);
        Access Result(bool eligible, string route, float? miles = null, float weight = 0f, string evidence = null) =>
            new(eligible, route, hq?.Id, home?.Id, miles, weight, evidence);
        if (hq == null || home == null) return Result(false, "UnknownPlace");
        string hqScene = hq.PlayableCityId ?? hq.Id;
        string homeScene = home.PlayableCityId ?? home.Id;
        if (hq.CountryCode == home.CountryCode && hqScene == homeScene)
            return Result(true, "Catchment", 0f, 1f);
        // A dated guest encounter is a real nonlocal route. Expired/future/alumnus rows do not qualify.
        var guest = artist.sceneParticipations?.Where(p => p.sceneId == hqScene &&
            p.relationship is not (SceneRelationship.Resident or SceneRelationship.Alumnus) &&
            p.startWeek <= week && p.endWeek.HasValue && p.endWeek.Value >= week &&
            !string.IsNullOrWhiteSpace(p.provenance)).OrderBy(p => p.startWeek)
            .ThenBy(p => p.provenance, StringComparer.Ordinal).FirstOrDefault();
        if (guest != null)
            return Result(true, "LocalVisitor", null, .85f, guest.provenance);
        if (hq.CountryCode != "US" || home.CountryCode != "US")
            return Result(false, "NoInternationalConnection");
        // Never use legacy distribution proxies as a literal satellite HQ or unknown road route.
        float? miles = hq.PlayableCityId != null && home.PlayableCityId != null
            ? DistanceModel.GetRoadMilesBetween(hq.PlayableCityId, home.PlayableCityId)
            : ScenePlaceRegistry.TryDomesticRoadMiles(hq.Id, home.Id, out double roadMiles) ? (float)roadMiles : null;
        float radius = label.tier switch {
            LabelTier.Major => 900f, LabelTier.MidTier => 650f,
            LabelTier.Independent => 350f, _ => 225f
        };
        if (miles.HasValue && float.IsFinite(miles.Value) && miles.Value > 0f && miles.Value <= radius)
            return Result(true, "RoadCircuit", miles, 1f / (1f + miles.Value / 150f));
        // Native A&R capacity only: borrowed trucks/wholesalers confer no talent connection.
        if (label.tier == LabelTier.Major && label.scoutingAbility >= .5f && label.nationalReach >= .5f)
            return Result(true, "NationalAr", miles, .12f);
        return Result(false, miles.HasValue ? "OutsideReach" : "UnknownRoadRoute", miles);
    }

    public static List<SimulatedArtist> Slate(AILabel label, IEnumerable<SimulatedArtist> pool,
        int week, int window, int count, out int accessibleCount) {
        var accessible = pool.Select(a => (Artist: a, Access: Explain(label, a, week)))
            .Where(x => x.Access.Eligible).ToList();
        accessibleCount = accessible.Count;
        // Weighted sampling without replacement. Attention remains finite, including recovery.
        double Key(SimulatedArtist artist) => -Math.Log(Math.Max(1e-12,
            LocalSceneIdentityService.AssignmentUnit(LocalSceneIdentityService.KeyedSeed,
                $"recruitment-v1|{label.labelId}|{artist.artistId}|{window}")));
        if (!LocalScenes.AttentionFeedback)
            return accessible.OrderBy(x => Key(x.Artist) / x.Access.Weight)
                .ThenBy(x => x.Artist.artistId, StringComparer.Ordinal).Take(count).Select(x => x.Artist).ToList();
        var feedback = SceneAttentionFeedback.Capture(week);
        var weighted = accessible.Select(x => (x.Artist, x.Access,
            Multiplier: feedback.Multiplier(x.Access.BasePlaceId, x.Artist.primaryGenre), Key: Key(x.Artist))).ToArray();
        var selected = weighted.OrderBy(x => x.Key / (x.Access.Weight * x.Multiplier))
            .ThenBy(x => x.Artist.artistId, StringComparer.Ordinal).Take(count).ToArray();
        if (LocalScenes.AttentionFeedbackAudit) {
            var baseline = weighted.OrderBy(x => x.Key / x.Access.Weight)
                .ThenBy(x => x.Artist.artistId, StringComparer.Ordinal).Take(count);
            SceneAttentionFeedback.Observe(weighted.Select(x => x.Multiplier), selected.Select(x => x.Multiplier),
                baseline.Select(x => x.Artist.artistId), selected.Select(x => x.Artist.artistId));
        }
        return selected.Select(x => x.Artist).ToList();
    }

    public static bool CanAccess(AILabel label, SimulatedArtist artist, int week) =>
        !LocalScenes.Recruitment || Explain(label, artist, week).Eligible;

    public static void RecordSigning(AILabel label, SimulatedArtist artist, int week, int year, string phase) {
        if (!LocalScenes.Recruitment) return;
        Access access = Explain(label, artist, week);
        if (!access.Eligible) throw new InvalidOperationException("Recruitment committed without geographic access.");
        artist.sceneRecruitmentHistory ??= new List<SceneRecruitmentRecord>();
        artist.sceneRecruitmentHistory.Add(new SceneRecruitmentRecord {
            LabelId = label.labelId, ArtistId = artist.artistId, SigningGenre = artist.primaryGenre, Week = week, Year = year,
            Phase = phase, Route = access.Route, HqPlaceId = access.HqPlaceId,
            BasePlaceId = access.BasePlaceId, RoadMiles = access.RoadMiles,
            AccessEvidence = access.Evidence,
            OriginPlaceId = artist.geography?.originPlaceId,
            Explanation = access.Route == "LocalVisitor" ? "Dated guest participation at HQ; base retained" :
                "Recruitment access v1; contract retains origin and working base; no relocation required"
        });
    }
}
