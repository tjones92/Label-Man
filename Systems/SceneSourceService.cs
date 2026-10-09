using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>US-facing foreign sourcing. No foreign births, consumer markets, cash ledgers or travel model.
/// Numerical shares and label connections are explicit gameplay priors, not historical measurements.</summary>
public static class SceneSourceService {
    public const int Version = 1;
    public static IReadOnlyList<SceneSourceProfile> Profiles { get; } = Array.AsReadOnly(new[] {
        new SceneSourceProfile("gb_london", "jazz, skiffle, folk and professional pop", "blues, beat, pop and continuing acoustic work",
            "publisher and recording-office introductions", "https://www.abbeyroad.com/our-story; practice mix provisional", true),
        new SceneSourceProfile("gb_liverpool", "traditional jazz, skiffle and emerging beat groups", "beat groups alongside continuing jazz",
            "club booker to agency to US A&R", "https://www.cavernclub.com/history/1950s/", false),
        new SceneSourceProfile("gb_manchester", "dance bands, jazz and youth guitar groups", "beat and R&B club work alongside jazz",
            "regional bookers and agency demo submissions", "https://stories.manchester.ac.uk/british-pop-archive/; exact programming provisional", true),
        new SceneSourceProfile("gb_birmingham", "dance halls, community music and youth groups", "beat, blues and continuing community work",
            "dance-hall bookers and regional agencies", "https://www.birminghammuseums.org.uk/collection; practice mix provisional", true),
        new SceneSourceProfile("gb_glasgow", "community, folk and dance work", "beat groups alongside continuing folk and dance work",
            "Scottish booking contacts and agency demos", "https://www.glasgowlife.org.uk/arts-music-and-culture/venues/kelvingrove-bandstand-and-amphitheatre/the-story-of-kelvingrove-bandstand-celebrating-100-years-of-music/the-events-and-performers; wider mix provisional", true),
        new SceneSourceProfile("gb_bristol", "jazz, skiffle and regional dance work", "beat and R&B alongside jazz and community music",
            "regional club introductions and agency submissions", "https://exhibitions.bristolmuseums.org.uk/bristol-music/; exact mix provisional", true)
    });
    public static IReadOnlyList<SceneSpecialistInstitution> Institutions { get; } = Array.AsReadOnly(new[] {
        new SceneSpecialistInstitution("muscle_shoals:session-exchange", "muscle_shoals", SceneInstitutionKind.Studio,
            "Southern soul, R&B and session introductions", Array.AsReadOnly(new[] { GenreFamily.RhythmAndSoul, GenreFamily.Blues }),
            "https://famestudios.com/our-history/; fictional supporting exchange, not FAME"),
        new SceneSpecialistInstitution("bakersfield:working-country-office", "bakersfield", SceneInstitutionKind.BookingOffice,
            "working country circuit and recording introductions", Array.AsReadOnly(new[] { GenreFamily.Country }),
            "https://cmhof.imgix.net/content/uploads/2019/04/BFL-TeacherGuide-Full.pdf; fictional supporting office"),
        new SceneSpecialistInstitution("clovis_nm:demo-exchange", "clovis_nm", SceneInstitutionKind.Studio,
            "guitar, rockabilly and regional recording introductions", Array.AsReadOnly(new[] { GenreFamily.Rock, GenreFamily.Country }),
            "https://www.newmexico.org/listing/7th-st-studios/1833/; fictional supporting exchange")
    });
    public static SceneSourceProfile Profile(string placeId) => Profiles.FirstOrDefault(p => p.PlaceId == placeId);

    /// <summary>Only the existing population owner calls this for a new act with no supplied place.
    /// British genre names alone never reclassify an established/legacy act.</summary>
    public static string FormationPlace(ulong seed, string id, Genre genre) {
        if (!LocalScenes.ExtendedWorld) return null;
        bool british = genre is Genre.BritishBeat or Genre.BritishPop or Genre.BritishBlues or Genre.BritishInvasion or Genre.Skiffle;
        // A small background slice demonstrates musical life before US commercial genre gates open.
        bool roots = genre is Genre.Folk or Genre.Jazz or Genre.RockAndRoll;
        var specialists = Institutions.Where(i => i.Families.Contains(GenreCatalog.Get(GenreCatalog.MapLegacy(genre, 1960)).Family)).ToArray();
        if (!british && specialists.Length > 0 && LocalSceneIdentityService.AssignmentUnit(seed, "specialist-base-v1|" + id) < .03)
            return specialists[(int)(LocalSceneIdentityService.AssignmentUnit(seed, "specialist-place-v1|" + id) * specialists.Length)].PlaceId;
        if (!british && (!roots || LocalSceneIdentityService.AssignmentUnit(seed, "foreign-roots-v1|" + id) >= .01)) return null;
        double draw = LocalSceneIdentityService.AssignmentUnit(seed, "foreign-source-v1|" + id);
        // Genre-conditioned opportunity priors; never a quality or sales boost.
        double[] weights = genre == Genre.BritishBlues ? new double[] { 5, 1, 2, 2, 1, 1 } : new double[] { 4, 4, 2, 2, 1, 1 };
        draw *= weights.Sum();
        for (int i = 0; i < weights.Length; i++) { draw -= weights[i]; if (draw < 0) return Profiles[i].PlaceId; }
        return Profiles[^1].PlaceId;
    }

    /// <summary>One-time procedural business identity. Saved lists, including empty lists, win on load.
    /// A capable US major has a finite agency portfolio; other labels need actual dated visitors.</summary>
    public static void EnsureConnections(AILabel label) {
        if (!LocalScenes.ExtendedWorld || label == null || label.sceneBusinessConnections != null) return;
        label.sceneBusinessConnections = new();
        if (ScenePlaceRegistry.Get(label.geography?.basePlaceId)?.CountryCode is not ("US" or "GB") ||
            label.tier != LabelTier.Major || label.scoutingAbility < .5f || label.nationalReach < .5f) return;
        int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
        // Each label reaches London and two regional offices. Sorted keyed picks don't consume RNG.
        var sources = Profiles.Where(p => p.PlaceId != "gb_london")
            .OrderBy(p => LocalSceneIdentityService.AssignmentUnit(LocalSceneIdentityService.KeyedSeed, $"agency-portfolio-v1|{label.labelId}|{p.PlaceId}"))
            .ThenBy(p => p.PlaceId, StringComparer.Ordinal).Take(2).Select(p => p.PlaceId).Prepend("gb_london");
        foreach (string place in sources) label.sceneBusinessConnections.Add(new() {
            Id = $"source-agency-v1|{label.labelId}|{place}", SourcePlaceId = place,
            FromWeek = week, ThroughWeek = 521,
            Provenance = "Simulated US-market major's named agency-demo connection; no foreign master rights, travel or distribution granted"
        });
    }
    public static SceneBusinessConnection Connection(AILabel label, string placeId, int week) =>
        !LocalScenes.ExtendedWorld ? null : label?.sceneBusinessConnections?
            .Where(c => c != null && c.SourcePlaceId == placeId && c.FromWeek >= 0 && c.FromWeek <= week &&
                c.ThroughWeek >= week && !string.IsNullOrWhiteSpace(c.Id) && !string.IsNullOrWhiteSpace(c.Provenance))
            .OrderBy(c => c.Id, StringComparer.Ordinal).FirstOrDefault();

    public static bool IsSource(string placeId) => Profile(placeId) != null || Institutions.Any(i => i.PlaceId == placeId);
    /// <summary>Legacy US region matching cannot move an existing person across an ocean.
    /// New keyed people are placed by their birth owner; this predicate applies to pooled people.</summary>
    public static bool CanJoinFromPool(Musician person, SimulatedArtist act) {
        string from = ScenePlaceRegistry.Get(person?.geography?.basePlaceId)?.CountryCode;
        string to = ScenePlaceRegistry.Get(act?.geography?.basePlaceId)?.CountryCode;
        if ((from is null or "US") && (to is null or "US")) return true;
        return from != null && from == to;
    }
    /// <summary>Paid introduction by a current US company. Does not lease a master or import an act.</summary>
    public static bool ArrangeIntroduction(AILabel label, string placeId, out string message) {
        message = "Source introductions require an active US label and an available office.";
        if (!LocalScenes.ExtendedWorld || !IsSource(placeId) || label == null || !label.IsActive ||
            ScenePlaceRegistry.Get(label.geography?.basePlaceId)?.CountryCode != "US" ||
            !(ReferenceEquals(label, PlayerDesk.Instance?.Label) || ChartManager.Instance.GetAllLabels().Any(l => ReferenceEquals(l, label)))) return false;
        int week = ChartManager.Instance.GetCurrentChartWeek();
        if (Connection(label, placeId, week) != null) { message = "You already have a current introduction to this office."; return false; }
        const float cost = 150;
        if (!label.CanAffordToSign(cost)) { message = "The introduction would consume the label's operating reserve."; return false; }
        var connection = new SceneBusinessConnection { Id = $"paid-source-v1|{label.labelId}|{placeId}|{week}",
            SourcePlaceId = placeId, FromWeek = week, ThroughWeek = week + 51,
            Provenance = "Paid $150 for a fictional agency/demo-office introduction, valid for 52 weeks; no master rights or travel included" };
        CompetitorManager.Instance.RecordExpense(label, cost);
        label.sceneBusinessConnections ??= new();
        label.sceneBusinessConnections.RemoveAll(c => c?.SourcePlaceId == placeId);
        label.sceneBusinessConnections.Add(connection);
        message = $"Introduced to the {ScenePlaceRegistry.Get(placeId).Name} demo office for 52 weeks ($150). A&R access only; each record contract remains separate.";
        return true;
    }
}
