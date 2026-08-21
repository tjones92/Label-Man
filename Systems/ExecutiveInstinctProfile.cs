using System.Collections.Generic;

/// <summary>
/// The four lenses the player reads the world through. Set once at founding by the chosen
/// <see cref="FoundingArchetype"/>. Affects which dialogue options appear in the Rolodex, how
/// accurately the player reads hidden DJ stats, and action success modifiers. Stats are 1–5.
/// </summary>
public sealed class ExecutiveInstinctProfile {
    public int TheEar    { get; set; }   // record quality, genre fit, DJ taste
    public int TheStreet { get; set; }   // regional momentum, room temperature
    public int TheSuit   { get; set; }   // business stats, reach, cost-effectiveness
    public int TheFixer  { get; set; }   // DJ greed/suspicion, payola risk, leverage
}

/// <summary>Who the player was before opening the label. Sets instincts and founding stat block.</summary>
public enum FoundingArchetype {
    PawnShopOwner,
    ExMusician,
    PromoMan,
    TradeInsider,
}

/// <summary>Static data for each founding archetype.</summary>
public static class FoundingArchetypeData {
    public sealed class ArchetypeProfile {
        public string Name             { get; init; }
        public string Tagline          { get; init; }
        public string Description      { get; init; }
        public ExecutiveInstinctProfile Instincts      { get; init; }
        public float Capital           { get; init; }
        public float ScoutingAbility   { get; init; }
        public float ProductionQuality { get; init; }
        public float MarketingPower    { get; init; }
        public float RiskTolerance     { get; init; }
        public float ArtistLoyalty     { get; init; }
        public float PayolaWillingness { get; init; }
    }

    public static readonly IReadOnlyDictionary<FoundingArchetype, ArchetypeProfile> All =
        new Dictionary<FoundingArchetype, ArchetypeProfile> {
            [FoundingArchetype.PawnShopOwner] = new() {
                Name        = "The Pawn-Shop Owner",
                Tagline     = "Knows money, not music.",
                Description =
                    "You have moved goods your whole life — appliances, instruments, whatever paid. The " +
                    "records are just another line. More starting capital and a better head for margins. " +
                    "Your ear is tin though, and the acts can feel it.",
                Instincts = new() { TheEar = 1, TheStreet = 2, TheSuit = 5, TheFixer = 4 },
                Capital = 1400f, ScoutingAbility = 0.30f, ProductionQuality = 0.30f,
                MarketingPower = 0.50f, RiskTolerance = 0.40f,
                ArtistLoyalty = 0.50f, PayolaWillingness = 0.15f,
            },
            [FoundingArchetype.ExMusician] = new() {
                Name        = "The Ex-Musician",
                Tagline     = "Good ear, bad business.",
                Description =
                    "You played the rooms. You know what it sounds like when something is real. The acts " +
                    "trust you because you were one of them. Thin capital, no credit line to speak of — " +
                    "but the music is never wrong and neither is the room.",
                Instincts = new() { TheEar = 5, TheStreet = 4, TheSuit = 1, TheFixer = 2 },
                Capital = 800f, ScoutingAbility = 0.75f, ProductionQuality = 0.65f,
                MarketingPower = 0.20f, RiskTolerance = 0.70f,
                ArtistLoyalty = 0.75f, PayolaWillingness = 0.02f,
            },
            [FoundingArchetype.PromoMan] = new() {
                Name        = "The Promo Man",
                Tagline     = "Works the phones.",
                Description =
                    "You know everybody and everybody owes you one. Best at turning contacts into spins. " +
                    "You also know how to make a situation disappear — for a price. You burn fast and the " +
                    "heat follows you, but sometimes that is the only door.",
                Instincts = new() { TheEar = 2, TheStreet = 4, TheSuit = 2, TheFixer = 5 },
                Capital = 800f, ScoutingAbility = 0.40f, ProductionQuality = 0.35f,
                MarketingPower = 0.55f, RiskTolerance = 0.65f,
                ArtistLoyalty = 0.50f, PayolaWillingness = 0.25f,
            },
            [FoundingArchetype.TradeInsider] = new() {
                Name        = "The Trade Insider",
                Tagline     = "Knows the industry.",
                Description =
                    "You came up through the business — publisher, agent, maybe a stint at a Major. You " +
                    "know the channels, the rates, and roughly who to call. No great strengths and no blind " +
                    "spots. The measured start.",
                Instincts = new() { TheEar = 3, TheStreet = 2, TheSuit = 4, TheFixer = 3 },
                Capital = 900f, ScoutingAbility = 0.50f, ProductionQuality = 0.45f,
                MarketingPower = 0.40f, RiskTolerance = 0.45f,
                ArtistLoyalty = 0.60f, PayolaWillingness = 0.08f,
            },
        };

    public static ArchetypeProfile Get(FoundingArchetype archetype) => All[archetype];
}
