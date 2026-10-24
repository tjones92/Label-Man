using System.Collections.Generic;

// Observational snapshots of the existing allocator, not additional AI inputs.
// Created only while an audit subscriber is attached; nothing is persisted.
public sealed class ProgrammeCandidateAudit {
	public string ArtistId, CareerState, Reason;
	public bool Eligible, Selected, Hot, Due;
	public int Rank, Gap;
	public float Score, ExpectedNet, Contribution, QualityEstimate, Priority, Momentum, Obligations;
	public double AttachedAudienceAtAudit;
}

public sealed class ProgrammeChoiceAudit {
	public string LabelId, Tier, Status, SlotId;
	public ReleaseFormat Format;
	public int Week, Year, IntendedWeek, IntendedYear;
	public bool Financed;
	public IReadOnlyList<ProgrammeCandidateAudit> Candidates;
}
