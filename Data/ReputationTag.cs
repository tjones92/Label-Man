// Scripts/Data/ReputationTag.cs

using Godot;

public enum ReputationTag {
	// Career Status
	RisingStar,
	Established,
	Veteran,
	Legend,
	OneHitWonder,
	Comeback,
	FadingFast,
	
	// Critical Reception
	CriticsDarling,
	CriticallyPanned,
	UndergroundFavorite,
	GuiltyPleasure,
	ArtistToWatch,
	Overrated,
	Underrated,
	
	// Commercial
	HitMachine,
	CommercialDisappointment,
	SleeperHit,
	Crossover,
	CultFollowing,
	MainstreamAppeal,
	
	// Artistic
	Innovator,
	Trendsetter,
	Derivative,
	Experimental,
	Traditional,
	GenreBending,
	Authentic,
	Manufactured,
	
	// Personality/Reputation
	Controversial,
	Squeaky_Clean,
	Rebellious,
	Professional,
	Difficult,
	Reliable,
	Unpredictable,
	MediaDarling,
	Reclusive,
	
	// Live Performance
	ElectricLive,
	StudioArtist,
	RoadWarrior,
	
	// Industry
	LabelFavorite,
	IndependentSpirit,
	RadioFriendly,
	PayolaKing
}

public static class ReputationTagExtensions {
	public static string ToDisplayString(this ReputationTag tag) {
		return tag switch {
			ReputationTag.RisingStar => "RISING STAR",
			ReputationTag.Established => "ESTABLISHED ACT",
			ReputationTag.Veteran => "INDUSTRY VETERAN",
			ReputationTag.Legend => "LIVING LEGEND",
			ReputationTag.OneHitWonder => "ONE-HIT WONDER?",
			ReputationTag.Comeback => "COMEBACK KID",
			ReputationTag.FadingFast => "FADING FAST",
			
			ReputationTag.CriticsDarling => "CRITICS' DARLING",
			ReputationTag.CriticallyPanned => "CRITICALLY PANNED",
			ReputationTag.UndergroundFavorite => "UNDERGROUND FAVORITE",
			ReputationTag.GuiltyPleasure => "GUILTY PLEASURE",
			ReputationTag.ArtistToWatch => "ONE TO WATCH",
			ReputationTag.Overrated => "OVERRATED",
			ReputationTag.Underrated => "HIDDEN GEM",
			
			ReputationTag.HitMachine => "HIT MACHINE",
			ReputationTag.CommercialDisappointment => "COMMERCIAL DUD",
			ReputationTag.SleeperHit => "SLEEPER HIT",
			ReputationTag.Crossover => "CROSSOVER SUCCESS",
			ReputationTag.CultFollowing => "CULT FOLLOWING",
			ReputationTag.MainstreamAppeal => "MAINSTREAM APPEAL",
			
			ReputationTag.Innovator => "INNOVATOR",
			ReputationTag.Trendsetter => "TRENDSETTER",
			ReputationTag.Derivative => "DERIVATIVE",
			ReputationTag.Experimental => "EXPERIMENTAL",
			ReputationTag.Traditional => "TRADITIONALIST",
			ReputationTag.GenreBending => "GENRE-BENDING",
			ReputationTag.Authentic => "THE REAL DEAL",
			ReputationTag.Manufactured => "MANUFACTURED",
			
			ReputationTag.Controversial => "CONTROVERSIAL",
			ReputationTag.Squeaky_Clean => "SQUEAKY CLEAN",
			ReputationTag.Rebellious => "REBELLIOUS",
			ReputationTag.Professional => "PROFESSIONAL",
			ReputationTag.Difficult => "DIFFICULT",
			ReputationTag.Reliable => "RELIABLE",
			ReputationTag.Unpredictable => "UNPREDICTABLE",
			ReputationTag.MediaDarling => "MEDIA DARLING",
			ReputationTag.Reclusive => "RECLUSIVE",
			
			ReputationTag.ElectricLive => "ELECTRIC LIVE",
			ReputationTag.StudioArtist => "STUDIO ARTIST",
			ReputationTag.RoadWarrior => "ROAD WARRIOR",
			
			ReputationTag.LabelFavorite => "LABEL FAVORITE",
			ReputationTag.IndependentSpirit => "INDEPENDENT SPIRIT",
			ReputationTag.RadioFriendly => "RADIO FRIENDLY",
			ReputationTag.PayolaKing => "WELL-CONNECTED",
			
			_ => tag.ToString().ToUpper()
		};
	}
	
	public static Color GetColor(this ReputationTag tag) {
		return tag switch {
			// Positive (green tones)
			ReputationTag.RisingStar => new Color(0.2f, 0.7f, 0.3f),
			ReputationTag.Legend => new Color(0.9f, 0.75f, 0.3f),
			ReputationTag.HitMachine => new Color(0.2f, 0.7f, 0.3f),
			ReputationTag.CriticsDarling => new Color(0.3f, 0.6f, 0.8f),
			ReputationTag.Innovator => new Color(0.6f, 0.4f, 0.8f),
			ReputationTag.Trendsetter => new Color(0.6f, 0.4f, 0.8f),
			ReputationTag.ElectricLive => new Color(0.9f, 0.5f, 0.2f),
			
			// Negative (red/brown tones)
			ReputationTag.FadingFast => new Color(0.7f, 0.3f, 0.2f),
			ReputationTag.OneHitWonder => new Color(0.6f, 0.5f, 0.3f),
			ReputationTag.Difficult => new Color(0.7f, 0.4f, 0.3f),
			ReputationTag.CriticallyPanned => new Color(0.6f, 0.3f, 0.3f),
			ReputationTag.Derivative => new Color(0.5f, 0.4f, 0.4f),
			
			// Neutral (gray/beige tones)
			ReputationTag.Established => new Color(0.6f, 0.6f, 0.5f),
			ReputationTag.Professional => new Color(0.5f, 0.5f, 0.6f),
			ReputationTag.Reliable => new Color(0.5f, 0.6f, 0.5f),
			ReputationTag.Traditional => new Color(0.6f, 0.5f, 0.4f),
			
			// Interesting/Ambiguous (yellow/orange)
			ReputationTag.Controversial => new Color(0.9f, 0.6f, 0.2f),
			ReputationTag.Experimental => new Color(0.7f, 0.5f, 0.7f),
			ReputationTag.Unpredictable => new Color(0.8f, 0.6f, 0.3f),
			ReputationTag.CultFollowing => new Color(0.6f, 0.5f, 0.7f),
			
			_ => new Color(0.6f, 0.6f, 0.6f)
		};
	}
}
