using System;
using Godot;

/// <summary>
/// Hop-table crowd saturation (fairness batch, 2026-10-14). A hop, club or church table sells to the room's regulars,
/// and the same regulars don't buy the same 45 twice. Before this, a table sold a full table's worth on every visit,
/// every day, forever: 77% of a probe label's units came off tables worked up to 46 times, every hook quartile sold
/// out its pressing, and a weak record sold as well as a good one. Now each room has a crowd (a fixed trait), the
/// record's appeal sets what share of it will ever buy, and buyers already served are remembered per title. New
/// faces arrive slowly. Airplay raises the record's appeal through local buzz, which widens the share that wants it.
/// Player-only: AI labels never touch a table.
/// </summary>
public partial class PlayerDesk {
	public sealed class TableCrowd {
		/// <summary>Copies this room's crowd has bought of the title, decayed for new faces as of <see cref="LastSale"/>.</summary>
		public float Bought;
		public GameDate LastSale;
	}

	// Authored values, not historical claims: a hop or church-hall crowd of 80-260 regulars; a weak record reaches a few
	// percent of them and a strong one about a third; about 5% of the crowd is new each week.
	private const float TableCrowdMin = 80f, TableCrowdMax = 260f;
	private const float TableTakeUpMin = 0.04f, TableTakeUpMax = 0.32f;
	public const float TableCrowdTurnoverPerWeek = 0.05f;

	/// <summary>How many of this room's crowd would ever buy the title, on its appeal here today.</summary>
	private float TableCrowdCeiling(PlayerStop stop, string recordId) {
		float crowd = Mathf.Lerp(TableCrowdMin, TableCrowdMax, stop.CrowdSize);
		float appeal = RecordAppeal(recordId, stop.CityId) ?? EarFloorAppeal;
		return crowd * Mathf.Lerp(TableTakeUpMin, TableTakeUpMax, Mathf.InverseLerp(EarFloorAppeal, EarCeilingAppeal, appeal));
	}

	/// <summary>Copies already bought by this room's crowd, less the share that has since turned over.</summary>
	private static float TableBought(PlayerStop stop, string recordId, GameDate today) {
		if (!stop.TableCrowds.TryGetValue(recordId, out TableCrowd c)) return 0f;
		float weeks = Math.Max(0, DaysBetween(c.LastSale, today)) / 7f;
		return c.Bought * Mathf.Pow(1f - TableCrowdTurnoverPerWeek, weeks);
	}

	/// <summary>What the table sells today: the table's usual size, thinned by the share of interested regulars who
	/// already have it, and never more than are left.</summary>
	private int TableTake(PlayerStop stop, string recordId, int tableSize, GameDate today) {
		float ceiling = TableCrowdCeiling(stop, recordId);
		float left = Mathf.Max(0f, ceiling - TableBought(stop, recordId, today));
		if (ceiling <= 0f || left < 1f) return 0;
		return Mathf.Clamp(Mathf.RoundToInt(tableSize * left / ceiling), left >= 1f ? 1 : 0, Mathf.FloorToInt(left));
	}

	private static void NoteTableSale(PlayerStop stop, string recordId, int qty, GameDate today) {
		float bought = TableBought(stop, recordId, today) + qty;
		stop.TableCrowds[recordId] = new TableCrowd { Bought = bought, LastSale = today };
	}

	/// <summary>Read-only for probes and UI: interested regulars left at a table for a title.</summary>
	public int TableBuyersLeft(string stopId, string recordId) {
		PlayerStop stop = GetStop(stopId);
		if (stop == null || stop.Kind != StopKind.Venue) return 0;
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		return Mathf.FloorToInt(Mathf.Max(0f, TableCrowdCeiling(stop, recordId) - TableBought(stop, recordId, today)));
	}
}
