// Scripts/Systems/Naming/NameDatabase.cs

using System.Collections.Generic;
using System.Linq;
using Godot;

[GlobalClass]
public partial class NameDatabase : Resource {
	
	public static NameDatabase Instance { get; private set; }
	
	private HashSet<string> usedArtistNames = new HashSet<string>();
	private HashSet<string> usedSongTitles = new HashSet<string>();
	private HashSet<string> usedAlbumTitles = new HashSet<string>();
	
	public void Initialize() {
		Instance = this;
		usedArtistNames.Clear();
		usedSongTitles.Clear();
		usedAlbumTitles.Clear();
	}
	
	public bool TryRegisterArtistName(string name) {
		if (usedArtistNames.Contains(name.ToLower())) return false;
		usedArtistNames.Add(name.ToLower());
		return true;
	}
	
	public bool TryRegisterSongTitle(string title, string artistName) {
		string key = $"{artistName}|{title}".ToLower();
		if (usedSongTitles.Contains(key)) return false;
		usedSongTitles.Add(key);
		return true;
	}
	
	// ========================================================================
	// FIRST NAMES - Organized by demographic for authenticity
	// ========================================================================
	
	public string[] maleNamesWhite = {
		"Johnny", "Bobby", "Jimmy", "Billy", "Tommy", "Ricky", "Eddie", "Frankie",
		"Gary", "Larry", "Barry", "Jerry", "Terry", "Denny", "Kenny", "Lenny",
		"Buddy", "Sonny", "Danny", "Donny", "Ronny", "Stevie", "Petey", "Joey",
		"Timmy", "Sammy", "Mickey", "Andy", "Freddy", "Teddy", "Louie", "Georgie",
		"Roy", "Del", "Gene", "Dean", "Wayne", "Duane", "Shane", "Blaine",
		"Paul", "John", "George", "Brian", "Keith", "Mick", "Eric", "Jeff",
		"Bob", "Tom", "Jim", "Bill", "Phil", "Neil", "Carl", "Glen",
		"Ray", "Jay", "Trey", "Vince", "Lance", "Brent", "Troy", "Dale",
		"Chad", "Brad", "Scott", "Mark", "Craig", "Cliff", "Grant", "Blake",
		"Dion", "Fabian", "Frankie", "Tony", "Sal", "Vinnie", "Nicky", "Carmine",
		"Angelo", "Rocco", "Dominic", "Paulie", "Vito", "Gino", "Dino", "Enzo",
		"Roger", "Peter", "Andrew", "Simon", "Graham", "Allan", "Gerry", "Colin",
		"Nigel", "Trevor", "Clive", "Malcolm", "Derek", "Neville", "Ian", "Alvin",
		"Robin", "Martin", "David", "Stuart", "Spencer", "Julian", "Rodney", "Terrence",
		"Robert", "Richard", "William", "James", "Charles", "Edward", "Henry", "Arthur",
		"Douglas", "Gordon", "Russell", "Lawrence", "Kenneth", "Leonard", "Bernard", "Gerald",
		"Butch", "Buzz", "Skip", "Chip", "Buck", "Biff", "Dutch", "Rip",
		"Hank", "Frank", "Earl", "Clyde", "Floyd", "Lloyd", "Vernon", "Chester"
	};
	
	public string[] femaleNamesWhite = {
		"Mary", "Peggy", "Betty", "Linda", "Barbara", "Nancy", "Susan", "Karen",
		"Donna", "Brenda", "Sharon", "Patricia", "Carol", "Sandra", "Diane", "Janet",
		"Judith", "Dorothy", "Helen", "Joyce", "Virginia", "Marilyn", "Carolyn", "Kathleen",
		"Connie", "Bonnie", "Carla", "Wanda", "Glenda", "Tina", "Gina", "Rita",
		"Patsy", "Kathy", "Sally", "Molly", "Polly", "Holly", "Dolly", "Shelly",
		"Sherry", "Terri", "Debbie", "Bobbie", "Jackie", "Vickie", "Nikki", "Ricki",
		"Dusty", "Lulu", "Cilla", "Sandie", "Marianne", "Petula", "Twiggy", "Pattie",
		"Chrissie", "Julie", "Jane", "Jean", "June", "Joy", "Jill", "Joan",
		"Grace", "Joni", "Judy", "Sandy", "Cindy", "Mindy", "Wendy", "Mandy",
		"Leslie", "Laurie", "Robin", "Kim", "Lynn", "Ann", "Marie", "Lee",
		"Janis", "Joni", "Judy", "Joan", "Melanie", "Carly", "Carole", "Laura",
		"Michelle", "Nicole", "Danielle", "Simone", "Colette", "Claudette", "Denise", "Renee"
	};
	
	public string[] maleNamesBlack = {
		"Marvin", "Smokey", "Stevie", "David", "Dennis", "Melvin", "Otis", "Curtis",
		"Isaac", "Rufus", "Wilson", "Levi", "Solomon", "Percy", "Tyrone", "Clarence",
		"Maurice", "Verdine", "Philip", "Roland", "Teddy", "Harold", "Lamont", "Ronald",
		"Reverend", "Bishop", "Deacon", "Brother", "Elder", "Prophet",
		"Junior", "Little", "Big", "Sly", "Booker", "Bo", "Lightning", "Muddy",
		"Slim", "Fathead", "Blue", "Sugar", "Sweet", "Honey", "Butter", "Guitar",
		"Jackie", "Tito", "Jermaine", "Michael", "Randy", "Marlon", "LaToya", "Janet",
		"Albert", "Freddie", "B.B.", "Buddy", "Junior", "Howlin'", "Lowell", "Elmore",
		"Little Walter", "Sonny Boy", "Magic Sam", "Pinetop", "Champion Jack", "Professor",
		"William", "James", "Robert", "Charles", "Samuel", "Benjamin", "Joseph", "Thomas",
		"Frederick", "Theodore", "Cleveland", "Roosevelt", "Washington", "Lincoln", "Jefferson"
	};
	
	public string[] femaleNamesBlack = {
		"Diana", "Aretha", "Etta", "Tina", "Gladys", "Patti", "Martha", "Mary",
		"Florence", "Cindy", "Betty", "Barbara", "Claudette", "Tammi", "Kim", "Brenda",
		"Mahalia", "Sister", "Mother", "Evangelist", "Apostle",
		"Dionne", "Darlene", "Ronnie", "Estelle", "Merry", "Dee Dee", "LaVern", "Ruth",
		"Carla", "Mavis", "Fontella", "Irma", "Jean", "Maxine", "Bonnie", "Freda",
		"Ann", "Linda", "Shirley", "Doris", "Gloria", "Mable", "Jackie", "Ruby",
		"Pearl", "Opal", "Ivory", "Ebony", "Jewel", "Crystal", "Precious", "Princess",
		"LaVerne", "Delphine", "Charlene", "Marlene", "Darlene", "Earline", "Geraldine",
		"Ernestine", "Josephine", "Clementine", "Albertina", "Wilhelmina", "Georgina"
	};
	
	public string[] maleNamesCountry = {
		"Johnny", "Waylon", "Willie", "Merle", "Buck", "Hank", "George", "Conway",
		"Charley", "Porter", "Marty", "Faron", "Ferlin", "Webb", "Chet", "Floyd",
		"Tex", "Tennessee", "Slim", "Lefty", "Cowboy", "Boxcar", "Dusty", "Lucky",
		"Curly", "Hoss", "Bubba", "Jimbo", "Billy Bob", "Bobby Joe", "Jimmy Don",
		"Earl", "Lester", "Bill", "Carl", "Sonny", "Harlan", "Amos", "Ezra",
		"Jethro", "Cletus", "Elmer", "Homer", "Virgil", "Claude", "Rufus", "Otis",
		"Eddy", "Don", "Glen", "Roger", "Ray", "Jimmy", "Tom", "Mel",
		"Bobby", "Randy", "Ricky", "Ronnie", "Dickey", "Mickey", "Denny"
	};
	
	public string[] femaleNamesCountry = {
		"Patsy", "Loretta", "Tammy", "Dolly", "Kitty", "Dottie", "Jeannie", "Jan",
		"Skeeter", "Norma", "Wanda", "Brenda", "Connie", "Lynn", "Tanya", "Crystal",
		"Barbara", "Melba", "Bobbie", "Billie Jo", "Jeanne", "Jody", "Sammi", "Sandy",
		"Jolene", "Joleen", "Marlene", "Darlene", "Charlene", "Nadine", "Pauline",
		"Ruby", "Pearl", "Opal", "Rose", "Lily", "Daisy", "Violet", "Iris",
		"Mae", "Faye", "Rae", "Kay", "Joy", "Faith", "Hope", "Grace"
	};
	
	// ========================================================================
	// LAST NAMES
	// ========================================================================
	
	public string[] lastNamesGeneric = {
		"Smith", "Jones", "Williams", "Brown", "Johnson", "Davis", "Miller", "Wilson",
		"Moore", "Taylor", "Anderson", "Thomas", "Jackson", "White", "Harris", "Martin",
		"Thompson", "Robinson", "Clark", "Lewis", "Lee", "Walker", "Hall", "Allen",
		"Young", "King", "Wright", "Hill", "Scott", "Green", "Adams", "Baker",
		"Nelson", "Carter", "Mitchell", "Roberts", "Turner", "Phillips", "Campbell", "Parker",
		"Evans", "Edwards", "Collins", "Stewart", "Morris", "Murphy", "Rogers", "Reed",
		"Cook", "Morgan", "Bell", "Cooper", "Richardson", "Cox", "Howard", "Ward",
		"Brooks", "Watson", "Wood", "James", "Bennett", "Gray", "Price", "Myers",
		"Long", "Foster", "Sanders", "Ross", "Powell", "Sullivan", "Russell", "Perry",
		"Butler", "Barnes", "Fisher", "Henderson", "Coleman", "Simmons", "Patterson", "Jordan",
		"Reynolds", "Hamilton", "Graham", "Wallace", "Woods", "West", "Cole", "Hunt",
		"Hayes", "Dean", "Stone", "Hart", "Palmer", "Webb", "Burke", "Watts"
	};
	
	public string[] lastNamesItalian = {
		"Martino", "Romano", "Greco", "Russo", "Bruno", "Marino", "Rizzo", "Costa",
		"Lombardi", "Gallo", "Conti", "DeLuca", "Ferrara", "Esposito", "Bianchi", "Leone",
		"Caruso", "Rossi", "Moretti", "Ferrari", "Barbieri", "Santini", "Marchetti", "Vitale",
		"Calabrese", "DiMaggio", "Pagano", "Fiorello", "Castellano", "Benedetto", "Catalano",
		"Vale", "Darin", "Avalon", "Rydell", "Vinton", "Vincent", "Francis", "Martin",
		"Lane", "Dean", "Day", "Darren", "Dante", "Damone", "Fontaine", "Stevens"
	};
	
	public string[] lastNamesCountry = {
		"Cash", "Cline", "Lynn", "Parton", "Owens", "Haggard", "Jones", "Pride",
		"Arnold", "Atkins", "Travis", "Dean", "Gibson", "Tubb", "Acuff", "Snow",
		"Wagoner", "Tillis", "Drusky", "Stuckey", "Husky", "Frazier", "Twitty",
		"Caldwell", "Dalton", "Hatfield", "McCoy", "Boone", "Crockett", "Houston", "Austin",
		"Carson", "Wheeler", "Tucker", "Lawson", "Allison", "Adkins", "Mullins", "Combs",
		"Sloane", "Pickett", "Cantrell", "Blanton", "Skaggs", "Scruggs", "Flatt", "Monroe"
	};
	
	public string[] lastNamesBritish = {
		"Jones", "Davies", "Evans", "Thomas", "Roberts", "Williams", "Hughes", "Edwards",
		"Morgan", "Lewis", "Price", "Bennett", "Wood", "Thompson", "Wright", "Green",
		"Ashworth", "Blackwood", "Carmichael", "Dunbar", "Fairfax", "Gallagher", "Holloway",
		"Kershaw", "Lancaster", "Marsden", "Newbury", "Osborne", "Prescott", "Radcliffe",
		"Sinclair", "Thornton", "Underwood", "Wakefield", "Whitfield", "Yardley",
		"Banks", "Booth", "Cross", "Drake", "Finch", "Frost", "Grant", "Heath",
		"Lane", "Marsh", "Nash", "Page", "Quinn", "Shaw", "Stone", "Swift",
		"Thorn", "Wade", "York", "Penn", "Locke", "Brook", "Field", "Grove"
	};
	
	public string[] lastNamesJewish = {
		"Gold", "Silver", "Diamond", "Pearl", "Ruby", "Crystal", "Sapphire",
		"Goldberg", "Silverstein", "Rosenberg", "Weinberg", "Greenberg", "Steinberg",
		"Rosen", "Bloom", "Stern", "Stein", "Berg", "Mann", "Feld", "Baum",
		"Schwartz", "Weiss", "Klein", "Gross", "Cohen", "Levy", "Shapiro", "Kaplan",
		"Segal", "Siegel", "Sherman", "Goodman", "Friedman", "Kaufman", "Hoffman"
	};
	
	// ========================================================================
	// NICKNAMES & PREFIXES
	// ========================================================================
	
	public string[] nicknamesPrefixes = {
		"Little", "Big", "Tiny", "Slim", "Fats", "Chubby", "Skinny", "Shorty", "Long Tall",
		"Screamin'", "Howlin'", "Wailin'", "Shoutin'", "Rockin'", "Rollin'", "Swingin'",
		"Jumpin'", "Stompin'", "Shoutin'", "Crying", "Laughing", "Smiling",
		"Wild", "Crazy", "Mad", "Cool", "Slick", "Smooth", "Sweet", "Hot", "Fast",
		"Mean", "Clean", "Keen", "Hip", "Groovy", "Heavy", "Mellow", "Soulful",
		"Guitar", "Piano", "Sax", "Harmonica", "Boogie", "Honky Tonk", "Slide",
		"Ivory", "Fingers", "Thumbs", "Hands", "Lips", "Golden Voice"
	};
	
	public string[] nicknamesTitles = {
		"King", "Queen", "Duke", "Earl", "Count", "Prince", "Sir", "Lady",
		"Lord", "Baron", "Empress", "Sultan", "Rajah", "Pharaoh",
		"Professor", "Doctor", "Reverend", "Judge", "Captain", "Major", "General",
		"Senator", "Governor", "Mayor", "Chief", "Boss", "Mister", "Madam",
		"The Great", "The Mighty", "The One and Only", "The Fabulous", "The Incredible",
		"The Amazing", "The Sensational", "The Legendary", "The Original", "The Real"
	};
	
	public string[] nicknamesQuoted = {
		"Guitar Slim", "Piano Red", "Sax Gordon", "Trumpet King", "Harp Daddy",
		"Fingers", "Thumbs", "Lightfoot", "Quicksilver", "Thunderfingers",
		"Killer", "Wicked", "Bad Boy", "Hound Dog", "Cool Cat", "Slick",
		"Snake", "Spider", "Rooster", "Gator", "Bull", "Bear", "Fox",
		"Memphis", "Chicago", "Detroit", "Philly", "Frisco", "Brooklyn",
		"Harlem", "Delta", "Swamp", "Bayou", "Hill Country", "West Side"
	};
	
	// ========================================================================
	// GROUP NAME COMPONENTS
	// ========================================================================
	
	public string[] groupNounsBirds = {
		"Ravens", "Orioles", "Crows", "Larks", "Robins", "Flamingos", "Penguins",
		"Cardinals", "Jays", "Wrens", "Doves", "Sparrows", "Eagles", "Hawks", "Falcons",
		"Nightingales", "Mockingbirds", "Bluebirds", "Blackbirds", "Swallows", "Starlings",
		"Pelicans", "Peacocks", "Swans", "Canaries", "Parrots", "Hummingbirds", "Owls",
		"Finches", "Thrushes", "Kingfishers", "Herons", "Cranes", "Seagulls", "Albatross",
		"Phoenixes", "Thunderbirds", "Firebirds", "Sunbirds", "Moonbirds", "Lovebirds"
	};
	
	public string[] groupNounsGems = {
		"Diamonds", "Pearls", "Emeralds", "Rubies", "Sapphires", "Crystals", "Jewels",
		"Opals", "Garnets", "Amethysts", "Topaz", "Jades", "Silvers", "Golds", "Platinums",
		"Velvetines", "Satins", "Silks", "Laces", "Chiffons", "Organzas", "Taffetas",
		"Sequins", "Rhinestones", "Glitters", "Sparkles", "Shimmers", "Gleams"
	};
	
	public string[] groupNounsMusical = {
		"Tones", "Notes", "Chords", "Tempos", "Rhythms", "Harmonies", "Melodies",
		"Echoes", "Sounds", "Beats", "Vibes", "Grooves", "Riffs", "Swings",
		"Temptations", "Impressions", "Sensations", "Vibrations", "Revelations", "Inspirations",
		"Devotions", "Emotions", "Passions", "Visions", "Dreams", "Fantasies", "Illusions",
		"Delights", "Supremes", "Excellents", "Magnificents", "Marvelettes", "Miracles"
	};
	
	public string[] groupNounsRoyalty = {
		"Royales", "Kings", "Queens", "Counts", "Dukes", "Earls", "Lords", "Barons",
		"Knights", "Princes", "Princesses", "Emperors", "Sultans", "Sheiks", "Pharaohs",
		"Monarchs", "Majestics", "Nobles", "Regals", "Imperials", "Sovereigns",
		"Chancellors", "Esquires", "Czars", "Kaisers", "Pashas", "Viceroys"
	};
	
	public string[] groupNounsAnimals = {
		"Crickets", "Spiders", "Beetles", "Ants", "Bees", "Wasps", "Moths", "Butterflies",
		"Tigers", "Lions", "Bears", "Wolves", "Foxes", "Panthers", "Jaguars", "Leopards",
		"Monkeys", "Turtles", "Frogs", "Snakes", "Lizards", "Gators", "Crocodiles",
		"Cats", "Dogs", "Hounds", "Badgers", "Otters", "Weasels", "Ferrets", "Minks",
		"Stallions", "Mustangs", "Broncos", "Colts", "Rams", "Bulls", "Bucks", "Stags"
	};
	
	public string[] groupNounsBritish = {
		"Shadows", "Echoes", "Phantoms", "Spectres", "Ghosts", "Wraiths", "Shades",
		"Strangers", "Outcasts", "Rebels", "Outlaws", "Renegades", "Mavericks",
		"Invaders", "Raiders", "Crusaders", "Conquerors", "Challengers", "Contenders",
		"Moods", "Modes", "Manners", "Pretenders", "Pretenses", "Facades",
		"Rumors", "Secrets", "Whispers", "Mysteries", "Enigmas", "Riddles",
		"Searchers", "Seekers", "Wanderers", "Rovers", "Nomads", "Drifters",
		"Travelers", "Ramblers", "Strollers", "Marchers", "Striders", "Pacers",
		"Mirrors", "Windows", "Doorways", "Gateways", "Pathways", "Crossroads",
		"Clockworks", "Gearworks", "Steamworks", "Metalworks", "Ironworks",
		"Stones", "Bricks", "Steels", "Irons", "Bronzes", "Coppers"
	};
	
	public string[] groupNounsPsych = {
		"Machine", "Engine", "Generator", "Reactor", "Oscillator", "Amplifier",
		"Transmitter", "Receiver", "Antenna", "Beacon", "Signal", "Frequency",
		"Airplane", "Rocket", "Capsule", "Satellite", "Orbiter", "Glider",
		"Balloon", "Zeppelin", "Dirigible", "Gondola", "Caravan", "Carriage",
		"Circus", "Carousel", "Carnival", "Sideshow", "Menagerie", "Pavilion",
		"Kaleidoscope", "Prism", "Spectrum", "Panorama", "Phantasmagoria",
		"Garden", "Meadow", "Forest", "Grove", "Orchard", "Vineyard",
		"Constellation", "Galaxy", "Nebula", "Cosmos", "Universe", "Infinity",
		"Eclipse", "Aurora", "Twilight", "Dawn", "Dusk", "Midnight",
		"Experience", "Experiment", "Expedition", "Exploration", "Discovery",
		"Society", "Association", "Corporation", "Foundation", "Collective",
		"Movement", "Revolution", "Evolution", "Transformation", "Metamorphosis",
		"Dream", "Vision", "Reverie", "Fantasy", "Illusion", "Mirage", "Phantasm",
		"Consciousness", "Awareness", "Perception", "Sensation", "Intuition"
	};
	
	public string[] groupNounsSurf = {
		"Surfmen", "Beachcombers", "Coasters", "Shoremen", "Tidemen", "Wavemen",
		"Sandpipers", "Shorebirds", "Pelicans", "Dolphins", "Barracudas", "Sharks",
		"Challengers", "Champions", "Competitors", "Contenders", "Winners", "Victors",
		"Ventures", "Adventures", "Exploits", "Escapades", "Expeditions",
		"Roadsters", "Dragsters", "Speedsters", "Customs", "Hot Rods", "Strokers",
		"Gassers", "Flatheads", "Hemi-Heads", "Fuel-Injectors", "Turbochargers",
		"Impacts", "Impalas", "Impulses", "Instincts", "Intuitions",
		"Sentinels", "Centurions", "Gladiators", "Warriors", "Champions"
	};
	
	// ========================================================================
	// ADJECTIVES
	// ========================================================================
	
	public string[] adjectivesUniversal = {
		"Blue", "Red", "Green", "Golden", "Silver", "White", "Black", "Purple",
		"True", "New", "Sweet", "Lonely", "Happy", "Sad", "Wild", "Young",
		"Little", "Big", "Good", "Bad", "Dark", "Bright", "Pretty", "Lovely",
		"Royal", "Fabulous", "Famous", "Fantastic", "Incredible", "Amazing",
		"Wonderful", "Marvelous", "Sensational", "Spectacular", "Supreme", "Ultimate"
	};
	
	public string[] adjectivesEarly60s = {
		"Dreamy", "Heavenly", "Angelic", "Divine", "Tender", "Devoted", "Faithful",
		"Sincere", "Precious", "Darling", "Bashful", "Shy", "Coy", "Demure",
		"Enchanting", "Charming", "Captivating", "Bewitching", "Spellbinding",
		"Neat", "Swell", "Keen", "Groovy", "Cool", "Hip", "Sharp", "Smart",
		"Crazy", "Wild", "Hep", "Gone", "Real Gone", "Way Out", "Far Out"
	};
	
	public string[] adjectivesSurf = {
		"Wild", "Wet", "Hot", "Cool", "Boss", "Tough", "Mean", "Wicked",
		"Bitchin'", "Gnarly", "Rad", "Killer", "Insane", "Crazy", "Intense",
		"California", "Malibu", "Waikiki", "Pipeline", "Rincon", "Huntington",
		"Laguna", "Sunset", "Pacific", "Coastal", "Shoreline", "Oceanside",
		"Tropical", "Hawaiian", "Island", "Beach", "Sandy", "Sunny"
	};
	
	public string[] adjectivesPsych = {
		"Electric", "Purple", "Crimson", "Vermillion", "Scarlet", "Indigo", "Violet",
		"Chartreuse", "Magenta", "Turquoise", "Ochre", "Azure", "Cobalt", "Cerulean",
		"Velvet", "Crystal", "Glass", "Liquid", "Plastic", "Neon", "Chrome",
		"Silk", "Satin", "Gossamer", "Iridescent", "Opalescent", "Luminescent",
		"Silver", "Golden", "Copper", "Bronze", "Iron", "Tin", "Mercury",
		"Strawberry", "Tangerine", "Lemon", "Orange", "Raspberry", "Vanilla",
		"Peach", "Apricot", "Plum", "Cherry", "Grape", "Lime", "Mango",
		"Cosmic", "Astral", "Stellar", "Celestial", "Ethereal", "Mystical", "Magical",
		"Lunar", "Solar", "Galactic", "Planetary", "Orbital", "Interstellar",
		"Strange", "Weird", "Freaky", "Groovy", "Trippy", "Heavy", "Mind-Blown",
		"Far-Out", "Spacey", "Hazy", "Fuzzy", "Dreamy", "Surreal", "Hypnotic",
		"Burning", "Frozen", "Melting", "Floating", "Spinning", "Whirling",
		"Trembling", "Vibrating", "Pulsing", "Throbbing", "Glowing", "Flickering"
	};
	
	public string[] adjectivesCountry = {
		"Lonesome", "Weary", "Tired", "Broken", "Aching", "Crying", "Hurting",
		"Grieving", "Mourning", "Suffering", "Pining", "Yearning", "Longing",
		"Cheating", "Lying", "Rambling", "Roaming", "Wandering", "Drifting",
		"Gambling", "Drinking", "Fighting", "Running", "Hiding", "Searching",
		"Old", "Rusty", "Dusty", "Muddy", "Dirty", "Ragged", "Worn", "Weathered",
		"Faded", "Tattered", "Crumbling", "Creaking", "Leaking", "Broken-Down",
		"Cold", "Frozen", "Bitter", "Empty", "Hollow", "Forgotten", "Forsaken",
		"Stormy", "Rainy", "Misty", "Foggy", "Cloudy", "Windy", "Sunny"
	};
	
	public string[] adjectivesSoul = {
		"Soul", "Soulful", "Funky", "Groovy", "Mellow", "Smooth", "Cool", "Hip",
		"Hot", "Burning", "Flaming", "Smoldering", "Heated", "Steamy", "Warm",
		"Deep", "Heavy", "Strong", "Powerful", "Mighty", "Intense", "Fierce",
		"Passionate", "Urgent", "Desperate", "Hungry", "Thirsty", "Starving",
		"Righteous", "Beautiful", "Wonderful", "Glorious", "Magnificent", "Fantastic",
		"Incredible", "Unbelievable", "Outstanding", "Phenomenal", "Tremendous"
	};
	
	// ========================================================================
	// SONG TITLE NOUNS
	// ========================================================================
	
	public string[] nounsUniversal = {
		"Heart", "Dream", "Night", "Day", "Time", "Way", "World", "Sky", "Star",
		"Moon", "Sun", "Heaven", "Earth", "Sea", "Wind", "Fire", "Rain",
		"Love", "Life", "Soul", "Mind", "Spirit", "Hope", "Faith", "Truth",
		"Peace", "Joy", "Pain", "Sorrow", "Tears", "Smile", "Laughter", "Silence",
		"Light", "Shadow", "Darkness", "Dawn", "Dusk", "Twilight", "Midnight",
		"Sunrise", "Sunset", "Moonlight", "Starlight", "Sunshine", "Rainbow"
	};
	
	public string[] nounsEarly60s = {
		"Angel", "Baby", "Boy", "Girl", "Guy", "Gal", "Teenager", "Senior",
		"Date", "Prom", "School", "Class", "Locker", "Hallway", "Gym", "Cafeteria",
		"Kiss", "Dance", "Party", "Twist", "Shake", "Stomp", "Hop", "Bop",
		"Letter", "Phone", "Ring", "Promise", "Vow", "Oath", "Pledge",
		"Chapel", "Church", "Altar", "Wedding", "Honeymoon", "Aisle", "Veil",
		"Bride", "Groom", "Preacher", "Bell", "Rice", "Bouquet",
		"Lipstick", "Mascara", "Perfume", "Roses", "Flowers", "Candy", "Chocolate",
		"Ribbon", "Bow", "Lace", "Silk", "Satin", "Velvet", "Pearl",
		"Twist", "Mashed Potato", "Pony", "Watusi", "Frug", "Swim", "Monkey",
		"Hitchhike", "Locomotion", "Limbo", "Boogaloo", "Jerk", "Shimmy", "Shake"
	};
	
	public string[] nounsSurf = {
		"Surfer", "Wave", "Beach", "Sand", "Ocean", "Sea", "Tide", "Shore",
		"Breaker", "Curl", "Swell", "Foam", "Spray", "Reef", "Lagoon", "Cove",
		"Woody", "Coupe", "Roadster", "Deuce", "Corvette", "Impala", "Mustang",
		"T-Bird", "GTO", "Charger", "Cuda", "Stingray", "Cobra", "Firebird",
		"Drag", "Strip", "Race", "Speed", "Chrome", "Engine", "Fuel", "Thunder",
		"Nitro", "Burnout", "Blacktop", "Asphalt", "Straightaway", "Finish Line",
		"Safari", "City", "Summer", "Vacation", "Holiday", "Weekend", "Sunset",
		"Bonfire", "Cookout", "Luau", "Tiki", "Hut", "Shack", "Pier", "Boardwalk"
	};
	
	public string[] nounsPsych = {
		"Mind", "Brain", "Consciousness", "Perception", "Awareness", "Senses",
		"Trip", "Journey", "Voyage", "Quest", "Odyssey", "Expedition",
		"Dream", "Vision", "Hallucination", "Illusion", "Delusion", "Fantasy",
		"Cloud", "Sky", "Space", "Star", "Galaxy", "Universe", "Cosmos", "Void",
		"Nebula", "Constellation", "Planet", "Moon", "Comet", "Asteroid", "Meteor",
		"Flower", "Petal", "Blossom", "Garden", "Forest", "Meadow", "Field",
		"Mountain", "Valley", "River", "Stream", "Waterfall", "Spring", "Pool",
		"Marmalade", "Tangerine", "Lemon", "Orange", "Strawberry", "Raspberry",
		"Peppermint", "Cinnamon", "Vanilla", "Chocolate", "Honey", "Sugar",
		"Kaleidoscope", "Prism", "Mirror", "Window", "Lens", "Crystal", "Glass",
		"Reflection", "Refraction", "Spectrum", "Rainbow", "Aurora", "Halo",
		"Clock", "Watch", "Time", "Moment", "Instant", "Eternity", "Infinity",
		"Tomorrow", "Yesterday", "Forever", "Never", "Always", "Sometimes",
		"Balloon", "Carousel", "Circus", "Carnival", "Fair", "Festival",
		"Ferris Wheel", "Merry-Go-Round", "Funhouse", "Midway", "Sideshow",
		"Smoke", "Mist", "Fog", "Haze", "Vapor", "Steam", "Cloud",
		"Incense", "Perfume", "Essence", "Spirit", "Elixir", "Nectar"
	};
	
	public string[] nounsCountry = {
		"Train", "Railroad", "Station", "Depot", "Track", "Whistle", "Smoke",
		"Freight", "Boxcar", "Caboose", "Engine", "Conductor", "Engineer",
		"Mama", "Daddy", "Home", "Cabin", "Shack", "Farm", "Ranch", "Barn",
		"Porch", "Rocking Chair", "Fireplace", "Kitchen", "Bedroom", "Cradle",
		"Prison", "Jail", "Cell", "Chain", "Bars", "Walls", "Freedom", "Parole",
		"Sheriff", "Deputy", "Judge", "Jury", "Sentence", "Verdict", "Trial",
		"River", "Creek", "Stream", "Lake", "Pond", "Swamp", "Bayou", "Delta",
		"Mountain", "Valley", "Hill", "Hollow", "Holler", "Ridge", "Mesa", "Plains",
		"Highway", "Road", "Trail", "Path", "Crossroads", "Junction", "Mile",
		"Bridge", "Tunnel", "Pass", "Fork", "Curve", "Bend", "Grade",
		"Bottle", "Glass", "Bar", "Saloon", "Jukebox", "Honky Tonk", "Dive", "Joint",
		"Whiskey", "Beer", "Wine", "Moonshine", "Brew", "Shot", "Chaser",
		"Boots", "Saddle", "Horse", "Truck", "Pickup", "Trailer", "Rodeo",
		"Cowboy", "Cowgirl", "Ranch Hand", "Wrangler", "Bronc", "Bull", "Steer"
	};
	
	public string[] nounsSoul = {
		"Respect", "Pride", "Dignity", "Honor", "Truth", "Faith", "Hope", "Glory",
		"Freedom", "Justice", "Peace", "Brotherhood", "Sisterhood", "Unity",
		"Emotion", "Feeling", "Passion", "Desire", "Need", "Want", "Hunger", "Thirst",
		"Longing", "Yearning", "Aching", "Burning", "Trembling", "Shaking",
		"Groove", "Beat", "Rhythm", "Soul", "Spirit", "Vibe", "Funk", "Heat",
		"Motion", "Action", "Dance", "Step", "Slide", "Glide", "Stride",
		"Arms", "Hands", "Lips", "Touch", "Embrace", "Hold", "Squeeze", "Caress",
		"Heart", "Body", "Soul", "Mind", "Eyes", "Smile", "Voice", "Skin",
		"Man", "Woman", "Lady", "Gentleman", "Brother", "Sister", "Baby", "Darling",
		"Lover", "Partner", "Friend", "Companion", "Sweetheart", "Soulmate",
		"Sugar", "Honey", "Sweetness", "Tenderness", "Lovin'", "Huggin'", "Kissin'",
		"Sunshine", "Angel", "Treasure", "Precious", "Blessing", "Gift",
		"Mountain", "River", "Valley", "Storm", "Thunder", "Lightning", "Rain",
		"Sunshine", "Fire", "Flame", "Ocean", "Wave", "Wind", "Earthquake"
	};
	
	// ========================================================================
	// SONG TITLE VERBS & PHRASES
	// ========================================================================
	
	public string[] verbsUniversal = {
		"Love", "Need", "Want", "Miss", "Hold", "Kiss", "Touch", "Feel",
		"See", "Hear", "Know", "Think", "Dream", "Hope", "Wish", "Pray",
		"Come", "Go", "Stay", "Leave", "Run", "Walk", "Dance", "Sing",
		"Cry", "Smile", "Laugh", "Sigh", "Whisper", "Shout", "Scream", "Call"
	};
	
	public string[] verbsAction = {
		"Twist", "Shake", "Shimmy", "Stomp", "Hop", "Jump", "Spin", "Turn",
		"Rock", "Roll", "Swing", "Sway", "Groove", "Move", "Glide", "Slide"
	};
	
	public string[] phrasesEarly60sOpeners = {
		"Do You", "Will You", "Won't You", "Can't You", "Don't You", "Didn't You",
		"Why Do", "Why Don't", "How Can", "What Makes", "Who Taught",
		"I Can't", "I Won't", "I Don't", "I Didn't", "I Couldn't", "I Shouldn't",
		"I'm So", "I'm Not", "I'll Never", "I'll Always", "I've Got", "I've Been",
		"Please Don't", "Please Let", "Tell Me", "Show Me", "Give Me", "Take Me",
		"Hold Me", "Kiss Me", "Love Me", "Touch Me", "Help Me", "Save Me",
		"Be My", "You're My", "You Make Me", "You Got Me", "You Drive Me",
		"It's My", "It's Your", "That's My", "That's Your", "This Is", "Here's"
	};
	
	public string[] phrasesSoulOpeners = {
		"Baby Please", "Darling Please", "Honey Won't You", "Sugar Don't",
		"Baby I", "Baby You", "Baby We", "Baby Let's",
		"Ain't No", "Ain't Got", "Ain't Gonna", "Ain't Never",
		"Got To", "Gotta Have", "Gotta Get", "Gotta Find",
		"Can't Get", "Can't Stop", "Can't Help", "Can't Live",
		"What Becomes Of", "What's Going On", "What's Happening", "What Did I Do",
		"Where Did Our Love Go", "Where Are You", "When Will I", "How Long",
		"Stop!", "Wait!", "Hold On!", "Listen!", "Look!", "Watch!",
		"(I Know) I'm", "(What) You", "(When) We", "(How) They"
	};
	
	public string[] phrasesPsychOpeners = {
		"Within The", "Beyond The", "Beneath The", "Above The", "Between The",
		"I Am The", "You Are The", "We Are The", "They Are The",
		"This Is The", "Here Comes The", "There Goes The",
		"Tomorrow", "Yesterday", "Today", "Tonight", "Forever", "Never",
		"Before The", "After The", "During The", "Until The",
		"Two", "Three", "Five", "Seven", "Eight", "Ten", "Hundred", "Thousand",
		"Who Knows", "Nobody Knows", "Everybody Knows", "Somebody",
		"Nothing Is", "Everything Is", "Something", "Anything"
	};
	
	public string[] phrasesPsychStructures = {
		" Miles High", " Miles Low", " Miles Away", " Miles Down",
		" Fields Forever", " Skies Forever", " Dreams Forever",
		"Floating On", "Drifting On", "Flying On", "Sailing On",
		"Slowly", "Softly", "Gently", "Quietly", "Silently"
	};
	
	public string[] phrasesCountryOpeners = {
		"I Walk The", "I Ride The", "I Hear The", "I See The",
		"He Was", "She Was", "They Were", "We Were", "It Was",
		"There's A", "There Was", "There'll Be", "There Ain't",
		"Stand By Your", "Walking Behind", "Standing Beside", "Lying Next To",
		"She Thinks I", "He Thinks I", "They Think I", "You Think I",
		"Down In", "Up In", "Out In", "Back In", "Over In",
		"At The", "By The", "On The", "In The", "Near The",
		"When I Was", "Before I", "After I", "Since I", "Until I",
		"Last Night", "This Morning", "Tomorrow Night", "Sunday Morning"
	};
	
	// ========================================================================
	// LOCATION NAMES
	// ========================================================================
	
	public string[] citiesGeneral = {
		"Memphis", "Detroit", "Chicago", "Philadelphia", "New Orleans", "Nashville",
		"Los Angeles", "San Francisco", "New York", "Boston", "Baltimore", "Cleveland",
		"Atlanta", "Miami", "Houston", "Dallas", "Kansas City", "St. Louis", "Cincinnati",
		"Pittsburgh", "Washington D.C.", "Newark", "Buffalo", "Milwaukee", "Indianapolis",
		"Oakland", "Seattle", "Denver", "Phoenix", "Minneapolis", "Louisville"
	};
	
	public string[] citiesSurf = {
		"California", "Malibu", "Laguna", "Huntington", "Rincon", "Santa Cruz",
		"Venice", "Santa Monica", "Redondo", "Hermosa", "Manhattan Beach", "Newport",
		"Oceanside", "Encinitas", "Del Mar", "La Jolla", "Pacific Beach", "Coronado",
		"Waikiki", "Honolulu", "Pipeline", "Sunset", "Banzai", "Makaha", "Hanalei",
		"Surfside", "Bayshore", "Seabreeze", "Coral Bay", "Palm Beach", "Crystal Cove"
	};
	
	public string[] citiesBritish = {
		"Liverpool", "London", "Manchester", "Birmingham", "Newcastle", "Glasgow",
		"Bristol", "Leeds", "Sheffield", "Nottingham", "Cambridge", "Oxford",
		"Brighton", "Blackpool", "Bournemouth", "Southampton", "Portsmouth", "Plymouth",
		"Cardiff", "Edinburgh", "Belfast", "Dublin", "Cork", "Galway"
	};
	
	public string[] citiesCountry = {
		"Nashville", "Memphis", "Austin", "Bakersfield", "Tulsa", "Oklahoma City",
		"Lubbock", "Amarillo", "Fort Worth", "San Antonio", "Shreveport", "Little Rock",
		"Wichita", "Abilene", "El Paso", "Reno", "Cheyenne", "Laramie",
		"Dodge City", "Tombstone", "Deadwood", "Jackson Hole", "Flagstaff", "Durango"
	};
	
	public string[] statesAndRegions = {
		"Tennessee", "Texas", "Georgia", "Alabama", "Mississippi", "Louisiana",
		"Kentucky", "Arkansas", "Oklahoma", "Missouri", "Carolina", "Virginia",
		"Appalachia", "The Delta", "The Ozarks", "The Panhandle", "The Valley",
		"The Bayou", "The Holler", "The Mountains", "The Plains", "The Coast"
	};
	
	// ========================================================================
	// INSTRUMENTAL TITLE COMPONENTS
	// ========================================================================
	
	public string[] instrumentalNouns = {
		"Theme", "Mood", "Blues", "Boogie", "Shuffle", "Stomp", "Jump",
		"Beat", "Groove", "Swing", "Bounce", "Rumble", "Thunder", "Fury",
		"Pipeline", "Reef", "Tide", "Breaker", "Crest", "Curl", "Barrel",
		"Apache", "Comanche", "Safari", "Jungle", "Tiki", "Bongo", "Conga",
		"Telstar", "Orbit", "Satellite", "Rocket", "Space", "Mars", "Venus"
	};
	
	public string[] instrumentalAdjectives = {
		"Raunchy", "Rebel", "Wild", "Hot", "Cool", "Blue", "Green", "Red",
		"Sleepy", "Lazy", "Crazy", "Busy", "Dizzy", "Hazy", "Fuzzy",
		"Twistin'", "Rockin'", "Rollin'", "Surfin'", "Swingin'", "Jumpin'"
	};
	
	public string[] instrumentalPlaces = {
		"Harlem", "Tijuana", "Hawaii", "Mexico", "Memphis", "Nashville", "Detroit",
		"Broadway", "Hollywood", "Sunset Strip", "Route 66", "Highway 101",
		"Bourbon Street", "Beale Street", "52nd Street", "Abbey Road"
	};
	
	// ========================================================================
	// 60s SLANG & EXCLAMATIONS
	// ========================================================================
	
	public string[] slangEarly60s = {
		"Neat", "Swell", "Keen", "Peachy", "Dreamy", "Fab", "Gear",
		"Gone", "Real Gone", "Way Out", "Far Out", "Out of Sight",
		"Boss", "Tough", "Sharp", "Cool", "Hip", "Hep", "With It"
	};
	
	public string[] slangLate60s = {
		"Groovy", "Trippy", "Heavy", "Far Out", "Mind-Blowing", "Mind-Bending",
		"Psychedelic", "Cosmic", "Freaky", "Wild", "Righteous", "Outta Sight",
		"Happening", "Where It's At", "Turned On", "Tuned In", "Dropped Out"
	};
	
	public string[] exclamationsEarly60s = {
		"Oh!", "Baby!", "Yeah!", "Ooh!", "Ahh!", "Wow!", "Gee!",
		"Golly!", "Gosh!", "Jeepers!", "Heavens!", "Mercy!", "Lordy!",
		"Sha-La-La!", "Da Doo Ron Ron!", "Doo Wop!", "Shoo-Be-Doo!",
		"Hey!", "Well!", "Now!", "So!", "Say!", "Look!", "Listen!"
	};
	
	public string[] exclamationsLate60s = {
		"Yeah!", "Right On!", "All Right!", "Uh-Huh!", "Oh Yeah!",
		"Sock It To Me!", "Get Down!", "Get Up!", "Hit Me!",
		"Mercy!", "Have Mercy!", "Good God!", "Lord Have Mercy!"
	};
	
	// ========================================================================
	// ALBUM-SPECIFIC COMPONENTS
	// ========================================================================
	
	public string[] albumFormats = {
		"{0}", "{0}!", "Meet {0}", "Introducing {0}", "Here's {0}", "Presenting {0}",
		"{0} Live", "{0} In Concert", "{0} On Stage", "{0} At The",
		"An Evening With {0}", "A Night With {0}", "{0} Performs",
		"The Best Of {0}", "{0}'s Greatest Hits", "Golden Hits Of {0}",
		"{0}'s Biggest Hits", "More Hits By {0}", "The Very Best Of {0}",
		"{0} Sings", "{0} Plays", "{0} Swings", "{0} Rocks",
		"{0} Goes", "{0} Meets", "{0} Salutes", "{0} Celebrates"
	};
	
	public string[] albumThemes = {
		"Songs Of Love", "Songs Of Heartbreak", "Songs For Lovers", "Songs For Dancing",
		"Love Songs", "Sad Songs", "Happy Songs", "Party Songs",
		"Summer Songs", "Songs For A Summer Night", "Midnight Songs",
		"Songs For Swinging Lovers", "Songs For Young Lovers",
		"Songs From The Heart", "Songs From Home", "Songs Of The City",
		"Songs Of The South", "Songs Of The Road", "Songs From Nowhere"
	};
	
	// ========================================================================
	// HELPER METHODS
	// ========================================================================
	
	public string GetRandom(string[] array) {
		if (array == null || array.Length == 0) return "Unknown";
		return array[GD.RandRange(0, array.Length - 1)];
	}
	
	public string GetRandomFromMultiple(params string[][] arrays) {
		var combined = arrays.SelectMany(a => a ?? new string[0]).ToArray();
		return GetRandom(combined);
	}
	
	public string GetWeighted(string[] primary, string[] secondary, float primaryWeight = 0.7f) {
		return GD.Randf() < primaryWeight ? GetRandom(primary) : GetRandom(secondary);
	}
	
	public string GetRandomExcluding(string[] array, HashSet<string> exclusions) {
		if (array == null || array.Length == 0) return "Unknown";
		var filtered = array.Where(s => !exclusions.Contains(s)).ToArray();
		return filtered.Length > 0 ? GetRandom(filtered) : GetRandom(array);
	}
	
	public string[] CombineArrays(params string[][] arrays) {
		return arrays.SelectMany(a => a ?? new string[0]).ToArray();
	}
}
