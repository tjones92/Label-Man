/// <summary>
/// Keyed name picks for people the band-member simulation brings into the world after generation: replacement
/// hires and partners. Deliberately NOT routed through NameGenerator: its stream also produces titles, and
/// titles are hashed into track traits ([[song-titles-feed-track-traits]]), so a draw there from new code would
/// quietly move the economy. A small period name bank and a keyed pick cost nothing and draw nothing.
/// </summary>
public static class BandLifeNames {
	private static readonly string[] Male = {
		"Bobby", "Jimmy", "Johnny", "Eddie", "Tommy", "Billy", "Ronnie", "Danny", "Frankie", "Richie", "Larry", "Gary",
		"Dennis", "Kenny", "Mike", "Steve", "Dave", "Pete", "Paul", "George", "Ray", "Earl", "Clarence", "Curtis", "Otis",
		"Marvin", "Leroy", "Willie", "Sam", "Al", "Joe", "Carl", "Roy", "Don", "Jerry", "Phil", "Ralph", "Walter", "Harold",
		"Lou", "Vince", "Sal", "Tony", "Nick", "Chuck", "Buddy", "Hank", "Floyd", "Wayne", "Dwight", "Glen", "Russ", "Ted",
		"Norman", "Ernie", "Melvin", "Leon", "Arthur", "Benny", "Rudy"
	};
	private static readonly string[] Female = {
		"Mary", "Linda", "Carol", "Barbara", "Patricia", "Judy", "Susan", "Donna", "Sharon", "Diane", "Brenda", "Joyce",
		"Sandra", "Peggy", "Shirley", "Betty", "Dottie", "Connie", "Marlene", "Arlene", "Gloria", "Loretta", "Darlene",
		"Ronnie", "Estelle", "Claudette", "Florence", "Martha", "Ruby", "Mavis", "Irma", "Wanda", "Rita", "Nancy", "Janet",
		"Kathy", "Joan", "Ellie", "Annette", "Sue", "Vicki", "Gail", "Lesley", "Dee Dee", "Cynthia", "Maxine", "Bonnie",
		"Jackie", "Rosalind", "Yvonne", "Dolores", "Lorraine", "Phyllis", "Bernadette", "Anita", "Laverne", "Theresa"
	};
	private static readonly string[] Surname = {
		"Miller", "Davis", "Wilson", "Moore", "Taylor", "Anderson", "Thomas", "Jackson", "White", "Harris", "Martin",
		"Thompson", "Robinson", "Clark", "Lewis", "Walker", "Hall", "Allen", "Young", "King", "Wright", "Scott", "Green",
		"Baker", "Adams", "Nelson", "Hill", "Campbell", "Mitchell", "Carter", "Roberts", "Turner", "Phillips", "Parker",
		"Evans", "Edwards", "Collins", "Stewart", "Morris", "Murphy", "Cook", "Rogers", "Morgan", "Cooper", "Peterson",
		"Reed", "Bailey", "Bell", "Kelly", "Howard", "Ward", "Cox", "Richardson", "Wood", "Watson", "Brooks", "Bennett",
		"Gray", "James", "Hughes", "Price", "Sanders", "Russo", "Romano", "DeLuca", "Kowalski", "Novak", "Schultz",
		"Weiss", "Kaplan", "Garcia", "Lopez", "Ramirez", "Fields", "Gaines", "Pickett", "Tolliver", "Dupree", "Fontaine"
	};

	public static (string First, string Last) Person(bool isMale, string key) {
		string[] firsts = isMale ? Male : Female;
		return (firsts[Index(firsts.Length, key + "|first")], Surname[Index(Surname.Length, key + "|last")]);
	}

	public static string FirstName(bool isMale, string key) {
		string[] firsts = isMale ? Male : Female;
		return firsts[Index(firsts.Length, key + "|first")];
	}

	private static int Index(int length, string key) {
		int i = (int)(BandLife.Unit(key) * length);
		return i < 0 ? 0 : i >= length ? length - 1 : i;
	}
}
