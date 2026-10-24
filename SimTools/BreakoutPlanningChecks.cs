using System;
using System.Linq;
using System.Text.Json;
using Godot;

public static class BreakoutPlanningChecks {
	public static void Run() {
		int checks = 0;
		void Check(bool value, string message) { if (!value) throw new InvalidOperationException("Breakout planning: " + message); checks++; }
		var manager = CompetitorManager.Instance;
		var previous = manager.ProgrammeState;
		string[] cmdline = OS.GetCmdlineUserArgs();
		var label = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned && l.IsActive && l.tier == LabelTier.MidTier &&
			l.roster.Count(a => GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(a) && a.members?.Count > 0) >= 2);
		var artist = label.roster.First(a => GenreSupplyService.IsEligibleExistingArtistForEnabledRelease(a) && a.members?.Count > 0);
		var other = label.roster.First(a => a != artist);
		var savedHits = label.roster.ToDictionary(a => a, a => a.hitCadenceLastTop40Week);
		bool player = label.isPlayerOwned;
		LabelTier tier = label.tier;
		int week = manager.ProgrammeWeekForChecks;
		string[] flags = { LabelReleaseProgramme.AllocationSwitch, LabelReleaseProgramme.CapacitySwitch, "--slate-work-budget=0.67" };
		try {
			bool Rejects(params string[] args) { try { LabelReleaseProgramme.Configure(args); return false; } catch (ArgumentException) { return true; } }
			Check(Rejects(LabelReleaseProgramme.BreakoutPlanningSwitch), "requires the work budget");
			Check(Rejects(flags.Concat(new[] { LabelReleaseProgramme.BreakoutPlanningSwitch, LabelReleaseProgramme.HitFollowUpFinanceSwitch }).ToArray()), "rejects extra-slot finance");
			var state = new ReleaseProgrammeState(); manager.SetProgrammeForChecks(state);
			foreach (var a in label.roster) a.hitCadenceLastTop40Week = week - 200;
			artist.hitCadenceLastTop40Week = week;
			state.Clocks[artist.artistId] = new ProgrammeClock { SingleWeek = week - 4, AlbumWeek = week };
			ReleaseSlateItem Slot(string id, string act, int offset) => new() { Id = id, LabelId = label.labelId, ArtistId = act,
				Format = ReleaseFormat.Single, Source = "roster-programme", Evidence = "original", State = ReleaseSlateState.Proposed,
				IntendedWeek = week + offset, DueWeek = week + offset, ReadyWeek = week + offset - 1, ProposedWeek = week,
				ContractSequence = artist.contractSequence, ReservedProduction = 120, ReservedMarketing = 50 };
			var slot = Slot("budget-slot", other.artistId, 7); state.Items.Add(slot);
			LabelReleaseProgramme.Configure(flags);
			Check(manager.PlanBreakoutFollowUps(label) == 0 && slot.ArtistId == other.artistId, "off leaves assignment untouched");
			LabelReleaseProgramme.Configure(flags.Append(LabelReleaseProgramme.BreakoutPlanningSwitch));
			label.tier = LabelTier.Major;
			Check(manager.PlanBreakoutFollowUps(label) == 0, "Majors excluded"); label.tier = tier;
			label.isPlayerOwned = true;
			Check(manager.PlanBreakoutFollowUps(label) == 0, "player excluded"); label.isPlayerOwned = player;
			GameRandom.Seed(17731); float expected = GameRandom.Randf(); GameRandom.Seed(17731);
			Check(manager.PlanBreakoutFollowUps(label) == 1, "a Top 40 entry assigns existing future work before the floor");
			Check(GameRandom.Randf() == expected, "planning takes no global random draws");
			Check(state.Items.Count == 1 && slot.Id == "budget-slot" && slot.IntendedWeek == week + 7 && slot.ProposedWeek == week, "slot budget and original window conserved");
			Check(slot.ArtistId == artist.artistId && slot.Source == "hit-follow-up" && slot.ContractSequence == artist.contractSequence, "rights move to the selected act");
			Check(slot.DueWeek == week + 2 && slot.ReadyWeek == week + 1, "respects the single floor and preparation lead");
			Check(slot.ReservedProduction == 0 && slot.ReservedMarketing == 0, "old holds released for live re-quotation");
			Check(manager.PlanBreakoutFollowUps(label) == 0, "one trigger does not duplicate or repeatedly reschedule work");
			var restored = JsonSerializer.Deserialize<ReleaseProgrammeState>(JsonSerializer.Serialize(state, SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
			manager.SetProgrammeForChecks(restored);
			Check(manager.PlanBreakoutFollowUps(label) == 0 && restored.Items.Count == 1, "saved evidence prevents replay after load");
			manager.SetProgrammeForChecks(state); state.Items.Clear();
			var own = Slot("own", artist.artistId, 6); var donor = Slot("donor", other.artistId, 7);
			own.PreparedRecord = new Record { recordId = "paid-master" }; own.RecordId = "paid-master"; own.PaidProduction = 321;
			own.ReservedProduction = 0; own.PreparedWeek = week - 1; state.Items.Add(own); state.Items.Add(donor);
			Check(manager.PlanBreakoutFollowUps(label) == 1 && donor.ArtistId == other.artistId, "reuses the act's own intention before taking another slot");
			Check(own.PreparedRecord.recordId == "paid-master" && own.PaidProduction == 321 && own.PreparedWeek == week - 1, "paid master and session preserved");
			state.Items.Clear(); donor = Slot("paid-donor", other.artistId, 7); donor.PaidProduction = 10; state.Items.Add(donor);
			Check(manager.PlanBreakoutFollowUps(label) == 0, "paid work cannot be reassigned");
			donor.PaidProduction = 0; donor.MaterialSearch = new ProgrammeMaterialSearch();
			Check(manager.PlanBreakoutFollowUps(label) == 0, "material commitments cannot be reassigned");
			donor.MaterialSearch = null; artist.hitCadenceLastTop40Week = week - 200;
			Check(manager.PlanBreakoutFollowUps(label) == 0, "cold acts create no trigger");
			artist.hitCadenceLastTop40Week = week; state.Clocks[artist.artistId].SingleWeek = week + 1;
			Check(manager.PlanBreakoutFollowUps(label) == 0, "an issued follow-up already satisfies the observed hit");
			state.Clocks[artist.artistId].SingleWeek = week - 4; state.Items.Clear();
			Check(manager.PlanBreakoutFollowUps(label) == 0 && state.Items.Count == 0, "no new work minted when the horizon is empty");
			GD.Print($"BREAKOUT_PLANNING_CHECK passed checks={checks}");
		} finally {
			label.tier = tier; label.isPlayerOwned = player;
			foreach (var pair in savedHits) pair.Key.hitCadenceLastTop40Week = pair.Value;
			manager.SetProgrammeForChecks(previous); LabelReleaseProgramme.Configure(cmdline);
		}
	}
}
