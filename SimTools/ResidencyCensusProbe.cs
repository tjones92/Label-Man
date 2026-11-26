using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
/// Step 8 of SimTools/SigningRealismBreakdown.md, measure first (§3.5 a′): who holds the rooms' standing engagements, and
/// who would get one if a signed resident gave it up. An AI-only world (no player label); each week it writes every
/// resident with its deal and record (tier, the label's national reach, career, charted, releases), its booking weight,
/// and the room's next pick by the same weight. Read-only: it only ends days.
///   --path . SimTools/ResidencyCensusProbe.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle [--weeks=26] [--out=residency-census]
/// Writes SimLogs/&lt;out&gt;.csv. Prints RESIDENCY_CENSUS_DONE.
/// </summary>
public partial class ResidencyCensusProbe : Node {
	public override void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			var args = OS.GetCmdlineUserArgs();
			int weeks = int.TryParse(args.FirstOrDefault(a => a.StartsWith("--weeks="))?.Substring(8), out int w) ? w : 26;
			string outName = args.FirstOrDefault(a => a.StartsWith("--out="))?.Substring(6) ?? "residency-census";
			var clock = TimeManager.Instance; var am = ArtistManager.Instance; var charts = ChartManager.Instance;
			var sb = new StringBuilder("week,place,room,artist,signed,tier,reach,career,charted,top40,releases,contract_releases,live,rep,weight," +
				"next,next_signed,next_tier,next_career,next_charted,next_releases,next_live,next_rep,next_weight\n");
			string F(float x) => x.ToString("0.###", CultureInfo.InvariantCulture);
			string D(double x) => x.ToString("0.###", CultureInfo.InvariantCulture);
			string Act(SimulatedArtist a, bool full) {
				var label = string.IsNullOrEmpty(a?.labelId) ? null : charts.GetLabelById(a.labelId);
				string signed = a == null ? "" : string.IsNullOrEmpty(a.labelId) ? "0" : "1";
				return full
					? $"{signed},{label?.tier.ToString() ?? ""},{F(label?.effectiveNationalReach ?? 0f)},{a?.careerState},{a?.charted},{a?.top40Hits},{a?.totalReleases},{a?.contractReleases},{F(a?.livePerformance ?? 0f)},{F(a?.reputation ?? 0f)}"
					: $"{signed},{label?.tier.ToString() ?? ""},{a?.careerState},{a?.charted},{a?.totalReleases},{F(a?.livePerformance ?? 0f)},{F(a?.reputation ?? 0f)}";
			}
			int rows = 0;
			for (int week = 0; week < weeks; week++) {
				LocalSceneRoomService.EnsureCalendar(clock.CurrentDate);
				int day = LocalSceneRoomService.Day(clock.CurrentDate);
				foreach (var r in LocalSceneRoomService.ResidencyCensusForProbe(day)) {
					var act = am.GetArtist(r.ArtistId); var next = r.NextId == null ? null : am.GetArtist(r.NextId);
					sb.Append($"{week},{r.PlaceId},{r.RoomId},{r.ArtistId},{Act(act, true)},{D(r.Weight)},{r.NextId},{Act(next, false)},{D(r.NextWeight)}\n");
					rows++;
				}
				for (int d = 0; d < 7; d++) clock.EndDay();
			}
			string path = ProjectSettings.GlobalizePath($"res://SimLogs/{outName}.csv");
			File.WriteAllText(path, sb.ToString());
			GD.Print($"RESIDENCY_CENSUS_DONE rows={rows} weeks={weeks} -> {path}");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("RESIDENCY_CENSUS_FAIL: " + e.Message + "\n" + e.StackTrace);
			GetTree().Quit(1);
		}
	}
}
