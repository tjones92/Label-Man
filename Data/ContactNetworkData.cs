using System;
using System.Collections.Generic;

/// <summary>How two people know each other (SimTools/ContactNetworkDirective.md). Flags: a pair can be both.</summary>
[Flags]
public enum ContactKind { None = 0, FormerBandmate = 1, Session = 2, HouseBand = 4 }

/// <summary>One remembered relationship between two people, allocated when they actually work together or part.
/// A and B are ordinal-sorted person ids; the edge is undirected.</summary>
public sealed class ContactEdge {
	public string A { get; set; }
	public string B { get; set; }
	public ContactKind Kinds { get; set; }
	public int FirstYear { get; set; }
	public int LastYear { get; set; }
	/// <summary>Shared jobs: sessions together, or the act-years of a band they left.</summary>
	public int Jobs { get; set; }
	/// <summary>They parted badly (acrimony, firing, a walkout). A fallout never recommends anyone.</summary>
	public bool Fallout { get; set; }
	/// <summary>Where they last worked together: the act they shared or backed, or the label whose session it was.</summary>
	public string Via { get; set; }
}

/// <summary>A person's paid session work in one year.</summary>
public sealed class SessionWorkAccount {
	public string PersonId { get; set; }
	public int Year { get; set; }
	public int Sessions { get; set; }
	public float Pay { get; set; }
}

/// <summary>A label's standing session crew: how many dates this person has played for it.</summary>
public sealed class SessionRegular {
	public string LabelId { get; set; }
	public string PersonId { get; set; }
	public int Sessions { get; set; }
	public int LastYear { get; set; }
}

public sealed class ContactNetworkSaveData {
	public int SchemaVersion { get; set; } = 1;
	public List<ContactEdge> Edges { get; set; } = new();
	public List<SessionWorkAccount> SessionWork { get; set; } = new();
	public List<SessionRegular> Regulars { get; set; } = new();
}
