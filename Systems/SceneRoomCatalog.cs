using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Fictional supporting rooms, not a census or schedules for real historical institutions.
/// Community descriptions follow the atlas; capacities, dates, fees and rhythms are provisional design inputs.</summary>
public static class SceneRoomCatalog {
    public const int Version = 1;
    private sealed record City(string Id, string Club, string Listening, string Acoustic, string Community,
        string Road, string Trade, string Early, string Late, string Source);
    private static readonly City[] Cities = {
        new("new_york", "Uptown Lantern", "Midtown Room", "Village Landing", "Neighborhood Union Hall", "Hudson Roadhouse", "Broadway Song Exchange", "vocal R&B and neighborhood dance bands", "soul and electric neighborhood bands", "MCNY Folk City; other layers provisional"),
        new("boston", "Harbor Lantern", "Back Bay Room", "College Listening Post", "College Union Hall", "Bay State Roadhouse", "Commonwealth Song Exchange", "harbor dance and R&B bands", "college dance and electric bands", "History Cambridge Club 47; professional layers provisional"),
        new("philadelphia", "Broad Street Lantern", "Delaware Room", "Schuylkill Coffeehouse", "Neighborhood Vocal Hall", "Keystone Roadhouse", "Session Players Exchange", "vocal groups and neighborhood R&B", "continuing soul and vocal groups", "Philadelphia Encyclopedia Soul Music"),
        new("baltimore", "Avenue Lantern", "Harbor Supper Room", "Chesapeake Coffeehouse", "Community Gospel Hall", "Maryland Roadhouse", "Theatre Players Exchange", "R&B and theatre circuit bands", "soul and youth dance bands", "Baltimore Heritage Royal Theatre; exact calendars provisional"),
        new("washington", "Capital Lantern", "Potomac Room", "District String Room", "Community Union Hall", "Piedmont Roadhouse", "Capital Players Exchange", "R&B and small dance ensembles", "soul and youth dance ensembles", "DC Bluegrass Union Tom Gray; no go-go programming"),
        new("pittsburgh", "Hill Lantern", "Three Rivers Room", "Bridge Coffeehouse", "Neighborhood Music Hall", "Allegheny Roadhouse", "Steel City Players Exchange", "blues and working R&B bands", "continuing blues and youth dance bands", "Heinz History Center Crawford Grill; 1960s detail provisional"),
        new("chicago", "South Side Lantern", "Lakefront Room", "Old Town String Room", "Neighborhood Gospel Hall", "Prairie Roadhouse", "Record Row Players Exchange", "blues and R&B dance work", "blues continuity and soul dance work", "Encyclopedia of Chicago music clubs, folk and R&B"),
        new("detroit", "River Lantern", "Motor City Room", "Woodward Coffeehouse", "Neighborhood Vocal Hall", "Michigan Roadhouse", "Studio Players Exchange", "soul and vocal dance bands", "soul alongside harder electric bands", "Motown Museum; Fifth Estate 1966 ballroom programming"),
        new("cleveland", "Lake Erie Lantern", "Shore Room", "Campus String Room", "Community Union Hall", "Ohio Roadhouse", "Lakefront Players Exchange", "R&B and youth dance bands", "electric bands alongside R&B", "Case Western La Cave; fictional coffeehouse not La Cave"),
        new("cincinnati", "Queen City Lantern", "Riverbend Room", "Appalachian String Room", "Community Vocal Hall", "Ohio Valley Roadhouse", "Kingdom Players Exchange", "country-rooted R&B and dance bands", "R&B and regional electric bands", "King Records history and Appalachian migration"),
        new("minneapolis", "Twin Cities Lantern", "North Star Room", "Campus Coffeehouse", "Neighborhood Music Hall", "Northern Roadhouse", "Ballroom Players Exchange", "teen dance and working bands", "ballroom garage and electric bands", "Minneapolis Music History 1850-2000; no later Minneapolis Sound"),
        new("st_louis", "River City Lantern", "Gaslight Listening Room", "Square Coffeehouse", "Community Gospel Hall", "Mississippi Roadhouse", "Gateway Players Exchange", "blues and R&B working bands", "continuing blues and electric dance bands", "SLPL Gaslight Square collection; other layers provisional"),
        new("kansas_city", "Crossroads Lantern", "Midtown Jazz Room", "Prairie Coffeehouse", "Community Union Hall", "Heartland Roadhouse", "Regional Players Exchange", "blues and regional dance work", "R&B and younger dance bands", "American Jazz Museum; specifically 1960s mix provisional"),
        new("omaha", "Riverfront Lantern", "Midland Room", "Prairie String Room", "Neighborhood Music Hall", "Plains Roadhouse", "Radio Players Exchange", "regional R&B and dance work", "younger dance bands and R&B", "Nebraska History Storz; performance mix provisional"),
        new("nashville", "Jefferson Lantern", "Cumberland Room", "String Players Coffeehouse", "Community Gospel Hall", "Tennessee Roadhouse", "Publisher Players Exchange", "R&B and neighborhood dance work", "soul and continuing R&B", "Country Music Hall of Fame Night Train to Nashville"),
        new("memphis", "Riverfront Lantern", "Bluff Room", "Crossroads Coffeehouse", "Community Gospel Hall", "Delta Roadhouse", "Studio Players Exchange", "blues, gospel roots and R&B", "soul alongside continuing blues", "Stax Museum chronology; other rooms fictional"),
        new("atlanta", "Peachtree Lantern", "Piedmont Room", "College String Room", "Community Gospel Hall", "Georgia Roadhouse", "Regional Players Exchange", "regional R&B and working dance bands", "soul and youth dance bands", "New Georgia Encyclopedia R&B and soul; city specifics provisional"),
        new("new_orleans", "Crescent Lantern", "Traditional Jazz Room", "River String Room", "Community Music Hall", "Gulf Roadhouse", "Studio Players Exchange", "R&B and neighborhood dance work", "R&B and soul beside living jazz traditions", "Preservation Hall and Jazz Museum continuity; fictional jazz room"),
        new("miami", "Biscayne Lantern", "Tourist Supper Room", "Bayfront Coffeehouse", "Spanish Language Music Hall", "Coastal Roadhouse", "Distributor Players Exchange", "R&B and teen dance bands", "soul and garage dance bands", "HistoryMiami Teen Miami and Cuban Popular Music; exact mixes provisional"),
        new("dallas", "Trinity Lantern", "Metroplex Room", "College String Room", "Community Music Hall", "Texas Roadhouse", "Regional Players Exchange", "R&B and regional rock bands", "garage and electric dance bands", "TSHA rock-and-roll; room-level mix provisional"),
        new("houston", "Bayou Lantern", "Gulf Supper Room", "College Coffeehouse", "Community Gospel Hall", "Lone Star Roadhouse", "Booking Players Exchange", "blues and R&B work", "soul and younger electric bands", "TSHA Blues Duke-Peacock booking networks"),
        new("san_antonio", "West Side Lantern", "Alamo Supper Room", "South Texas String Room", "Conjunto Community Hall", "Borderland Roadhouse", "Regional Players Exchange", "West Side R&B and dance hybrids", "continuing hybrids and youth rock", "TSHA Conjunto and Orquestas Tejanas; Texas State West Side Sound"),
        new("phoenix", "Desert Lantern", "Valley Room", "Desert String Room", "Community Music Hall", "Arizona Roadhouse", "Studio Players Exchange", "guitar and rockabilly-rooted bands", "instrumental and youth electric bands", "Arizona PBS Phoenix Sound; specific room detail provisional"),
        new("albuquerque", "Route Lantern", "Rio Grande Room", "Mesa Coffeehouse", "Community Music Hall", "New Mexico Roadhouse", "Road Circuit Players Exchange", "regional dance and guitar bands", "garage and regional dance bands", "Visit Albuquerque Sound of 66; other layers provisional"),
        new("denver", "Front Range Lantern", "Mountain Room", "Front Range String Room", "College Music Hall", "Colorado Roadhouse", "Regional Players Exchange", "regional R&B and dance bands", "electric bands and regional dance work", "CU AMRC Harry Tuft archive; supporting rooms fictional"),
        new("salt_lake_city", "Wasatch Lantern", "Valley Listening Room", "Community String Room", "Campus Music Hall", "Utah Roadhouse", "Regional Players Exchange", "regional working and dance bands", "youth dance and electric bands", "Utah oral-history finding aids; city room detail provisional"),
        new("billings", "Yellowstone Lantern", "Northern Listening Room", "Montana String Room", "School Music Hall", "High Plains Roadhouse", "Circuit Players Exchange", "small working and teen dance circuit", "garage and regional teen dance circuit", "Wanderers participant account; supporting communities provisional"),
        new("los_angeles", "Eastside Lantern", "Studio City Room", "Canyon String Room", "Neighborhood Music Hall", "Coastal Roadhouse", "Session Players Exchange", "R&B, guitar and coastal dance bands", "electric bands alongside R&B and coastal work", "LOC Sunset Strip and East LA project; particular rooms fictional"),
        new("san_francisco", "Bay Lantern", "Cabaret Jazz Room", "Bay Folk Room", "Bay Community Hall", "Bay Area Roadhouse", "Independent Players Exchange", "R&B and neighborhood dance work", "electric dance bands beside earlier communities", "FoundSF early folk and jazz; later ballroom layer provisional"),
        new("seattle", "Sound Lantern", "Puget Listening Room", "Northwest String Room", "Community Music Hall", "Corridor Roadhouse", "Dance Circuit Players Exchange", "R&B-derived corridor dance bands", "garage and continuing corridor dance bands", "HistoryLink Richard Berry and Spanish Castle; supporting rooms fictional"),
        new("portland", "River Lantern", "Rose City Room", "Northwest Coffeehouse", "Neighborhood Music Hall", "Columbia Roadhouse", "Regional Players Exchange", "R&B and Northwest dance bands", "garage beside continuing jazz and R&B", "Portland historic-resource study and Northwest histories")
    };
    public static IReadOnlyList<SceneRoomProfile> All { get; } = Build();
    private static IReadOnlyList<SceneRoomProfile> Build() {
        var rooms = new List<SceneRoomProfile>();
        foreach (var city in Cities) {
            void Add(string suffix, string name, SceneRoomKind kind, PlayerDesk.ScoutingVenue category, string early,
                string late, int capacity, float admission, int open, params DayOfWeek[] nights) {
                GenreFamily[] families = kind switch {
                    SceneRoomKind.Club => new[] { GenreFamily.RhythmAndSoul, GenreFamily.Blues, GenreFamily.Rock },
                    SceneRoomKind.ListeningRoom => new[] { GenreFamily.Jazz, GenreFamily.Pop },
                    SceneRoomKind.Coffeehouse => new[] { GenreFamily.Folk, GenreFamily.Jazz },
                    SceneRoomKind.CommunityHall when city.Id is "miami" or "san_antonio" => new[] { GenreFamily.Latin, GenreFamily.RhythmAndSoul },
                    SceneRoomKind.CommunityHall => new[] { GenreFamily.Gospel, GenreFamily.Folk, GenreFamily.RhythmAndSoul },
                    SceneRoomKind.Roadhouse => new[] { GenreFamily.Country, GenreFamily.Blues, GenreFamily.Folk },
                    _ => Enum.GetValues<GenreFamily>().Where(f => f != GenreFamily.NonMusic).ToArray()
                };
                string id = city.Id + ":" + suffix;
                rooms.Add(new(id, city.Id, name, kind, category, early, late, city.Source,
                    Array.AsReadOnly(families), Array.AsReadOnly(nights), open, open + 3, capacity, admission,
                    id + ":booker", name + " booker"));
            }
            Add("club", city.Club, SceneRoomKind.Club, PlayerDesk.ScoutingVenue.ClubsAndRoadhouses, city.Early, city.Late, 120, 1, 17, DayOfWeek.Friday, DayOfWeek.Saturday);
            Add("listening", city.Listening, SceneRoomKind.ListeningRoom, PlayerDesk.ScoutingVenue.TheatresAndSupperClubs,
                city.Id == "new_orleans" ? "living traditional jazz and listening audiences" : "jazz ensembles and supper-room professionals",
                city.Id == "new_orleans" ? "continuing traditional jazz and listening audiences" : "jazz ensembles and supper-room professionals", 80, 1, 14, DayOfWeek.Saturday, DayOfWeek.Sunday);
            Add("acoustic", city.Acoustic, SceneRoomKind.Coffeehouse, PlayerDesk.ScoutingVenue.HonkyTonks, "folk standards, acoustic originals and small jazz groups", "continuing acoustic work and new songwriters", 45, 0, 17, DayOfWeek.Thursday, DayOfWeek.Saturday);
            Add("community", city.Community, SceneRoomKind.CommunityHall, PlayerDesk.ScoutingVenue.ClubsAndRoadhouses,
                city.Id is "miami" or "san_antonio" ? "Spanish-language dance repertoire and community ensembles" : "community singing, gospel and acoustic ensembles",
                city.Id is "miami" or "san_antonio" ? "continuing Spanish-language dance and neighborhood hybrids" : "continuing community singing and younger ensembles", 100, 0, 12, DayOfWeek.Sunday);
            Add("road", city.Road, SceneRoomKind.Roadhouse, PlayerDesk.ScoutingVenue.HonkyTonks, "country standards, blues and working string bands", "continuing country, blues and string-band work", 65, 0, 12, DayOfWeek.Wednesday, DayOfWeek.Friday);
            Add("trade", city.Trade, SceneRoomKind.TradeEvent, PlayerDesk.ScoutingVenue.IndustryMeets, "publisher, booker and studio listening appointments", "publisher, booker and studio listening appointments", 20, 0, 10, DayOfWeek.Tuesday);
        }
        return Array.AsReadOnly(rooms.OrderBy(r => r.Id, StringComparer.Ordinal).ToArray());
    }
    public static SceneRoomProfile Get(string id) => All.FirstOrDefault(r => r.Id == id);
    public static IReadOnlyList<SceneRoomProfile> ForPlace(string placeId) => Array.AsReadOnly(All
        .Where(r => r.PlaceId == LocalScenePersistenceService.SceneIdFor(placeId)).ToArray());
}
