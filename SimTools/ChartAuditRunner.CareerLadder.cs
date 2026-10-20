using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

// Career-ladder readout for --album-scale-career-milestones (economy audit B1/B2). Observation only: reads public
// state after each week and writes new files only, in every mode (calibration included).
//   <run>-career-ladder.csv: one row per week with the registry's career-state census and the week's rung moves
//     (promotions into Rising/Established/Star/Superstar, Star->Established and Superstar->Star demotions).
//   <run>-chart-breadth.csv: one row per year with distinct acts on the Hot 100, its Top 40 and Top 10, and the
//     album chart.
public partial class ChartAuditRunner {
	private sealed class BreadthYear {
		public readonly HashSet<string> Hot100 = new(StringComparer.Ordinal), Top40 = new(StringComparer.Ordinal),
			Top10 = new(StringComparer.Ordinal), Album = new(StringComparer.Ordinal);
	}

	private static readonly CareerState[] LadderStates = (CareerState[])Enum.GetValues(typeof(CareerState));
	private StreamWriter careerLadderWriter;
	private readonly Dictionary<string, CareerState> ladderLastState = new(StringComparer.Ordinal);
	private readonly SortedDictionary<int, BreadthYear> breadthYears = new();
	private string careerLadderDirectory;

	private void OpenCareerLadderOutputs(string directory) {
		careerLadderDirectory = directory;
		careerLadderWriter = CreateWriter(Path.Combine(directory, $"{runName}-career-ladder.csv"));
		careerLadderWriter.WriteLine("week,year," + string.Join(",", LadderStates.Select(s => s.ToString())) +
			",toRising,toEstablished,toStar,toSuperstar,starToEstablished,superstarToStar");
	}

	private void CaptureCareerLadderWeek(int week, int year, List<RecordRuntimeData> chart, List<RecordRuntimeData> albumChart) {
		if (careerLadderWriter == null || ArtistManager.Instance == null) return;
		var counts = new int[LadderStates.Length];
		int toRising = 0, toEstablished = 0, toStar = 0, toSuperstar = 0, starDown = 0, superstarDown = 0;
		foreach (SimulatedArtist artist in ArtistManager.Instance.GetAllArtists()) {
			if (artist == null) continue;
			CareerState now = artist.careerState;
			counts[(int)now]++;
			if (ladderLastState.TryGetValue(artist.artistId, out CareerState before) && before != now) {
				if (now == CareerState.Rising) toRising++;
				else if (now == CareerState.Established && before == CareerState.Star) starDown++;
				else if (now == CareerState.Established) toEstablished++;
				else if (now == CareerState.Star && before == CareerState.Superstar) superstarDown++;
				else if (now == CareerState.Star) toStar++;
				else if (now == CareerState.Superstar) toSuperstar++;
			}
			ladderLastState[artist.artistId] = now;
		}
		careerLadderWriter.WriteLine(string.Join(",", new[] { week.ToString(CultureInfo.InvariantCulture), year.ToString(CultureInfo.InvariantCulture) }
			.Concat(counts.Select(c => c.ToString(CultureInfo.InvariantCulture)))
			.Concat(new[] { toRising, toEstablished, toStar, toSuperstar, starDown, superstarDown }.Select(c => c.ToString(CultureInfo.InvariantCulture)))));

		if (!breadthYears.TryGetValue(year, out BreadthYear breadth)) breadthYears[year] = breadth = new BreadthYear();
		foreach (RecordRuntimeData record in chart ?? new List<RecordRuntimeData>()) {
			string artistId = record?.baseRecord?.artistId;
			if (string.IsNullOrEmpty(artistId) || record.currentPosition <= 0) continue;
			breadth.Hot100.Add(artistId);
			if (record.currentPosition <= 40) breadth.Top40.Add(artistId);
			if (record.currentPosition <= 10) breadth.Top10.Add(artistId);
		}
		foreach (RecordRuntimeData record in albumChart ?? new List<RecordRuntimeData>()) {
			string artistId = record?.baseRecord?.artistId;
			if (!string.IsNullOrEmpty(artistId) && record.currentPosition > 0) breadth.Album.Add(artistId);
		}
	}

	private void CloseCareerLadderOutputs() {
		careerLadderWriter?.Dispose();
		careerLadderWriter = null;
		if (careerLadderDirectory == null) return;
		using StreamWriter breadthWriter = CreateWriter(Path.Combine(careerLadderDirectory, $"{runName}-chart-breadth.csv"));
		breadthWriter.WriteLine("year,hot100Acts,top40Acts,top10Acts,albumChartActs");
		foreach ((int year, BreadthYear b) in breadthYears)
			breadthWriter.WriteLine(string.Join(",", new[] { year, b.Hot100.Count, b.Top40.Count, b.Top10.Count, b.Album.Count }
				.Select(v => v.ToString(CultureInfo.InvariantCulture))));
		careerLadderDirectory = null;
	}
}
