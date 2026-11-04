using System;
using System.Collections.Generic;

public enum WritingContractKind { Freelance, Preferred, Exclusive }
public enum WritingOrderStatus { Pending, Delivered, Cancelled }
public enum WritingDemoStatus { Available, Shelved, Licensed, Recorded }

[Serializable]
public sealed class WritingSupplier {
	public string WriterId;
	public float Reputation;
	public float CommissionFees, LicenseFees, RetainerFees;
	public int Assignments, Hits, BusyUntilWeek, NextWritingWeek;
}

[Serializable]
public sealed class WritingContract {
	public string WriterId, LabelId;
	public WritingContractKind Kind;
	public int StartWeek, EndWeek, NextPaymentWeek;
	public float Retainer;
	public bool Ended;
}

[Serializable]
public sealed class WritingOrder {
	public string Id, WriterId, LabelId, ArtistId, SongId, CoWriterPersonId;
	public Genre Genre;
	public GameDate DueDate;
	public int StartedWeek, DeliveryWeek;
	public float Fee;
	public WritingOrderStatus Status;
	public bool PlayerNotified;
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public string ReleasePlanId;
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
	public bool DeliveryOffered;
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
	public bool PlanReminder;
}

[Serializable]
public sealed class WritingDemo {
	public string SongId, WriterId, LabelId, ArtistId;
	public int AvailableWeek, PriorityUntilWeek, HoldUntilWeek;
	public float FeePaid;
	public WritingDemoStatus Status;
	public bool Released;
}

[Serializable]
public sealed class ProfessionalWritingPartnership {
	public string LabelId, ArtistId, WriterId;
	public int Assignments, Recordings, Hits, LastAssignmentWeek = -100;
}

[Serializable]
public sealed class WritingPlanReminder {
	public string LabelId, ArtistId;
	public int ContractSequence, CreatedWeek, ExpiresWeek;
}

public sealed class ProfessionalWritingMarketState {
	public int Version = 1, LastWeek, Sequence;
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
	public bool PlannedAssignments, DeliveryOffers;
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
	public bool PlanReminders;
	[System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
	public List<WritingPlanReminder> Reminders;
	public Dictionary<string, WritingSupplier> Suppliers = new(StringComparer.Ordinal);
	public Dictionary<string, WritingDemo> Demos = new(StringComparer.Ordinal);
	public List<WritingContract> Contracts = new();
	public List<WritingOrder> Orders = new();
	public Dictionary<string, ProfessionalWritingPartnership> Partnerships = new(StringComparer.Ordinal);
	public HashSet<string> ObservedHits = new(StringComparer.Ordinal);
}

public sealed class WritingOffer {
	public string WriterId, Name;
	public float CommissionFee, LicenseFee, PreferredRetainer, ExclusiveRetainer, Reputation;
	public int DeliveryWeeks, PreviousAssignments, PreviousHits;
	public bool Available;
}
