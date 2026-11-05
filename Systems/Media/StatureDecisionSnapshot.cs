using System;

/// <summary>Conservative eligibility and timing for an experimental AI prestige read.</summary>
public static class StatureDecisionSnapshot {
	public enum Status { Available, Missing, Stale, SameDayOrFuture, MissingEvidence, PlayerOwned, Terminal, LifecycleUnresolved }
	public readonly struct Read {
		public readonly Status Status;
		public readonly CareerState CareerState;
		public Read(Status status, CareerState careerState) { Status = status; CareerState = careerState; }
	}

	/// <summary>
	/// A decision reads the immediately preceding completed week. Terminal
	/// lifecycle states and player ownership remain authoritative. Below Rising
	/// retains NewSigning; declining and dropped are unresolved in this pilot.
	/// This function does not modify the artist or apply a financial table.
	/// </summary>
	public static Read Resolve(StatureWeeklyShadow.Snapshot snapshot, DateTime decisionDate, CareerState current, bool playerOwned,
		ArtistLifecycleStatus lifecycle = ArtistLifecycleStatus.Active) {
		if (playerOwned) return new(Status.PlayerOwned, current);
		if (lifecycle is ArtistLifecycleStatus.Retired or ArtistLifecycleStatus.Disbanded || current is CareerState.Retired or CareerState.Disbanded)
			return new(Status.Terminal, current);
		if (lifecycle != ArtistLifecycleStatus.Active) return new(Status.LifecycleUnresolved, current);
		if (current is CareerState.Declining or CareerState.Dropped) return new(Status.LifecycleUnresolved, current);
		if (current is not (CareerState.NewSigning or CareerState.Rising or CareerState.Established or CareerState.Star or CareerState.Superstar))
			return new(Status.LifecycleUnresolved, current);
		if (snapshot == null || snapshot.Date == default || decisionDate == default) return new(Status.Missing, current);
		int age = (decisionDate.Date - snapshot.Date.Date).Days;
		if (age <= 0) return new(Status.SameDayOrFuture, current);
		if (age > 7) return new(Status.Stale, current);
		if (snapshot.Status == StatureShadowPolicy.ResultStatus.MissingEvidence) return new(Status.MissingEvidence, current);
		if (snapshot.Band == StatureShadowPolicy.Band.BelowRising)
			return current == CareerState.NewSigning ? new(Status.Available, current) : new(Status.LifecycleUnresolved, current);
		return snapshot.Band switch {
			StatureShadowPolicy.Band.Rising => new(Status.Available, CareerState.Rising),
			StatureShadowPolicy.Band.Established => new(Status.Available, CareerState.Established),
			StatureShadowPolicy.Band.Star => new(Status.Available, CareerState.Star),
			StatureShadowPolicy.Band.Superstar => new(Status.Available, CareerState.Superstar),
			_ => new(Status.MissingEvidence, current)
		};
	}
}
