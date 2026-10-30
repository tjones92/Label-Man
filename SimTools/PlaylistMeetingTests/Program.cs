using System;
using System.IO;
using System.Linq;
using System.Text.Json;

static class Program {
	static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
	static PlaylistMeeting.Candidate C(string id, float score, bool readd = false) => new() { Id = id, Score = score, Readd = readd };
	static void Main(string[] args) {
		if (args.Length == 0) AudienceChecks.Run();
		if (args.Length > 0) {
			int count = 0;
			foreach (var line in File.ReadLines(args[0])) {
				var capture = JsonSerializer.Deserialize<PlaylistMeeting.Capture>(line);
				var replay = PlaylistMeeting.Resolve(capture.Input);
				Check(JsonSerializer.Serialize(replay) == JsonSerializer.Serialize(capture.Result), $"Replay mismatch at meeting {count}");
				count++;
			}
			Check(count > 0, "Empty capture");
			Console.WriteLine($"{count} captured meetings reproduced exactly; frozen scores and prior state, no demand forecast.");
			// Only after the unchanged replay passes, write one-meeting policy sensitivities.
			// Prior state and scores stay captured: these are not a multiweek simulation.
			if (args.Length is 4 or 5) {
				int duration = int.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);
				float margin = float.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture);
				Check(duration > 0 && float.IsFinite(margin) && margin >= 1, "Use positive trial weeks and margin >= 1");
				// Optional "extra": picks are added on top of the tier instead of reserved from it.
				Check(args.Length == 4 || args[4] == "extra", "Fifth argument may only be extra");
				bool extra = args.Length == 5;
				Check(!string.Equals(Path.GetFullPath(args[0]), Path.GetFullPath(args[1]), StringComparison.OrdinalIgnoreCase), "Output must differ from input");
				using var output = new StreamWriter(args[1]);
				foreach (var line in File.ReadLines(args[0])) {
					var capture = JsonSerializer.Deserialize<PlaylistMeeting.Capture>(line);
					capture.Input.TrialWeeks = duration;
					capture.Input.ReaddMargin = margin;
					if (extra) capture.Input.Extra = true;
					capture.Result = PlaylistMeeting.Resolve(capture.Input);
					output.WriteLine(JsonSerializer.Serialize(capture));
				}
			}
			return;
		}
		var m = new PlaylistMeeting.Input { Week = 10, Light = 2, PickTier = SpinTier.Light };
		var probe = new PlaylistMeeting.Input { Week = 10, Light = 2, ResearchSlots = 1 };
		var audition = C("audition", 1000);
		audition.ResearchHistory = true; audition.ResearchActive = true; audition.ResearchStart = 9;
		probe.Candidates.AddRange(new[] { C("ordinary", 1), audition });
		var pr = PlaylistMeeting.Resolve(probe);
		Check(pr.Seats.Count == 2 && pr.Seats.Single(s => s.Id == "audition").Source == "research-trial", "National score cannot graduate an unaccepted research trial");
		probe.Week = 12;
		Check(PlaylistMeeting.Resolve(probe).Seats.All(s => s.Id != "audition"), "Research trial expires without destination acceptance");
		audition.ResearchAccepted = true;
		Check(PlaylistMeeting.Resolve(probe).Seats.Single(s => s.Id == "audition").Source == "research-graduated", "Destination acceptance permits graduation");
		audition.ResearchAccepted = false; audition.ResearchActive = false; audition.ResearchOffer = true;
		Check(PlaylistMeeting.Resolve(probe).Seats.All(s => s.Id != "audition"), "Expired research trial cannot restart on a repeated offer");
		audition.ResearchHistory = false; audition.Score = 0; audition.ResearchScore = 2;
		Check(PlaylistMeeting.Resolve(probe).Seats.Count == 2, "New research audition uses reserved capacity");
		audition.Rejection = "cooldown";
		Check(PlaylistMeeting.Resolve(probe).Seats.All(s => s.Id != "audition"), "Research recommendations respect eligibility exclusions");
		m.Candidates.AddRange(new[] { C("incumbent", 1), C("weak", .5f), C("return", .7f, true) });
		var r = PlaylistMeeting.Resolve(m);
		Check(r.Seats.Count == 2 && r.Seats.Any(s => s.Id == "return") && r.Seats.All(s => s.Id != "weak"), "Full sheet must admit a convincing return");
		m.Candidates[2].Score = .59f;
		Check(PlaylistMeeting.Resolve(m).Seats.All(s => s.Id != "return"), "Hysteresis must hold");
		m.Candidates[2].Score = 9;
		m.Candidates[2].Rejection = "cooldown";
		Check(PlaylistMeeting.Resolve(m).Seats.All(s => s.Id != "return"), "Locked return must stay out");
		m.Candidates[2].Rejection = null;
		m.High = 1; m.Light = 0;
		Check(PlaylistMeeting.Resolve(m).Seats.All(s => s.Id != "return"), "Return cannot displace High");
		m.High = 0; m.Light = 2; m.PickSlots = 1; m.TrialWeeks = 3;
		m.Candidates[1].ActiveTrial = true; m.Candidates[1].TrialStart = 8;
		m.Candidates[1].PickScore = .1f;
		m.Candidates[2].Score = 0;
		r = PlaylistMeeting.Resolve(m);
		Check(r.Seats.Any(s => s.Id == "weak" && s.Source == "trial"), "Third station week must retain trial beyond release age");
		m.Week = 11;
		Check(PlaylistMeeting.Resolve(m).Seats.All(s => s.Id != "weak"), "Trial must expire after three station weeks");
		m.Candidates[1].Score = 2;
		Check(PlaylistMeeting.Resolve(m).Seats.Any(s => s.Id == "weak" && s.Source == "graduated"), "Trial must graduate on merit");
		m.Week = 10; m.Candidates[1].Score = .5f; m.Candidates[2].Score = 9;
		Check(PlaylistMeeting.Resolve(m).Seats.Any(s => s.Id == "weak" && s.Source == "trial"), "Return must not displace protected trial");
		m.Advocacy.Add("incumbent"); m.AdvocacySlots = 1;
		Check(PlaylistMeeting.Resolve(m).Seats.Count == 2, "Advocacy and reservations must not overfill Light");
		m.Extra = true;
		Check(PlaylistMeeting.Resolve(m).Seats.Count <= 3, "Extra trial capacity must be bounded");
		// Capturing and reading float scores must preserve decisions and ordering exactly.
		var captureTest = new PlaylistMeeting.Capture { Input = m, Result = PlaylistMeeting.Resolve(m) };
		var roundtrip = JsonSerializer.Deserialize<PlaylistMeeting.Capture>(JsonSerializer.Serialize(captureTest));
		Check(JsonSerializer.Serialize(PlaylistMeeting.Resolve(roundtrip.Input)) == JsonSerializer.Serialize(roundtrip.Result), "Capture round trip");
		Console.WriteLine("Playlist checks passed: displacement, hysteresis, cooldown, tiers, probation, graduation, capacity, replay.");
		ExposureChecks.Run();
	}
}
