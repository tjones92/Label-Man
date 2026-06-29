// Scripts/Data/ActionCosts.cs

public static class ActionCosts {
    // Phone calls
    public const int QuickCall = 1;          // "Hey, just checking in"
    public const int StandardCall = 2;       // Normal business call
    public const int LongCall = 3;           // Negotiation, pitch, schmoozing
    
    // Meetings
    public const int QuickMeeting = 2;       // Coffee, quick sync
    public const int StandardMeeting = 4;    // Lunch meeting, office visit
    public const int LongMeeting = 6;        // Contract negotiation, audition
    
    // Creative
    public const int StudioSession = 8;      // Full day in studio
    public const int HalfDayStudio = 4;      // Overdubs, mixing
    public const int Songwriting = 4;        // Writing session
    
    // Admin
    public const int Paperwork = 1;          // Contracts, accounting
    public const int Planning = 2;           // Strategy, scheduling
    
    // Travel (these might just advance days instead)
    public const int LocalTravel = 1;        // Across town
    public const int RegionalTravel = 4;     // Nearby city
    // Long distance travel should probably just skip days
}