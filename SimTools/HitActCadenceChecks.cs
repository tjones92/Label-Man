using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// --hit-act-cadence-check: identity when off; when on, the clocks, the due rule, the lane's pick, the forced single,
/// the split share and predecessor cut, and both retention rules.
/// </summary>
public static class HitActCadenceChecks {
	public static void Run() {
		void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("Hit-act cadence: " + why); }
		bool wasLane = HitActCadence.LaneEnabled, wasRetention = HitActCadence.RetentionEnabled,
			wasAwareness = HitActCadence.AwarenessSplitEnabled;
		try {
			// Off: nothing is written and every read is the identity.
			HitActCadence.SetForChecks(false, false);
			var off = new SimulatedArtist { artistId = "a_off", weeksSinceLastRelease = 40, contractTop40Hits = 3 };
			HitActCadence.OnTop40(off, 10);
			Check(off.hitCadenceLastTop40Week == HitActCadence.Unset,
				"off must not write the clock");
			Check(HitActCadence.LaunchShare(3) == 1f, "off must give the launch its whole share");
			Check(HitActCadence.ChartingPredecessors("a_off", "r").Count == 0, "off must find no predecessors");
			Check(!HitActCadence.ShouldForceSingle(true), "off must never force a single");
			Check(!HitActCadence.ShouldRenew(off), "off must never renew");
			Check(!HitActCadence.ProtectsFromDrop(off, 12), "off must never protect");
			var offRec = new RecordRuntimeData { radioHeat = 0.5f };
			HitActCadence.CutPredecessors(new List<RecordRuntimeData> { offRec });
			Check(offRec.radioHeat == 0.5f, "off must not cut a predecessor's radio");

			// Clocks: written on, seeded from an opening career's public history, unset otherwise.
			HitActCadence.SetForChecks(true, true);
			var vet = new SimulatedArtist();
			vet.publicHistory.Add(new PublicReleaseEntry { year = 1958, peakPosition = 30 });
			vet.publicHistory.Add(new PublicReleaseEntry { year = 1959, peakPosition = 55 });
			Check(HitActCadence.WeeksSinceTop40(vet, 0) == 78, "a 1958 Top 40 seeds 78 weeks before week 0; a 1959 #55 does not count");
			var fresh = new SimulatedArtist();
			Check(HitActCadence.WeeksSinceTop40(fresh, 50) == int.MaxValue, "an act with no hit has never been hot");
			HitActCadence.OnTop40(fresh, 40);
			Check(HitActCadence.WeeksSinceTop40(fresh, 50) == 10, "a Top 40 sets the clock");

			// Due: a Top 40 within 52 weeks and 13 weeks since the last release, both edges inclusive.
			var hot = new SimulatedArtist { artistId = "a_hot", weeksSinceLastRelease = HitActCadence.DueWeeks };
			HitActCadence.OnTop40(hot, 100);
			Check(HitActCadence.IsDue(hot, 100 + HitActCadence.HotWindowWeeks), "52 weeks after a Top 40 is still hot");
			Check(!HitActCadence.IsDue(hot, 101 + HitActCadence.HotWindowWeeks), "53 weeks after is not");
			hot.weeksSinceLastRelease = HitActCadence.DueWeeks - 1;
			Check(!HitActCadence.IsDue(hot, 110), "12 weeks since a release is not due");

			// The pick: the longest wait wins, ties go to the lower artist id, none due falls through.
			var b = new SimulatedArtist { artistId = "b", weeksSinceLastRelease = 20 };
			var a = new SimulatedArtist { artistId = "a", weeksSinceLastRelease = 20 };
			var c = new SimulatedArtist { artistId = "c", weeksSinceLastRelease = 30 };
			var cold = new SimulatedArtist { artistId = "cold", weeksSinceLastRelease = 99 };
			foreach (var x in new[] { a, b, c }) HitActCadence.OnTop40(x, 200);
			Check(HitActCadence.PickDue(new[] { a, b, cold }, 210) == a, "a tie must go to the lower artist id");
			Check(HitActCadence.PickDue(new[] { a, b, c, cold }, 210) == c, "the longest wait must win");
			Check(HitActCadence.PickDue(new[] { cold, hot }, 210) == null, "no due act must fall through to the lottery");

			// The forced single: only for a lane pick that chose an album.
			HitActCadence.MarkPick(false);
			Check(!HitActCadence.ShouldForceSingle(true), "a lottery pick keeps its album");
			HitActCadence.MarkPick(true);
			Check(!HitActCadence.ShouldForceSingle(false), "a lane single needs no override");
			Check(HitActCadence.ShouldForceSingle(true) && HitActCadence.LaneAlbumOverrides == 1, "a lane album becomes a single");

			// The split: share by concurrent count, predecessors filtered to the act's charting singles, radio cut once.
			Check(HitActCadence.LaunchShare(0) == 1f && HitActCadence.LaunchShare(1) == 0.5f &&
				Math.Abs(HitActCadence.LaunchShare(2) - 1f / 3f) < 1e-6f, "share must be 1 / (1 + predecessors)");
			HitActCadence.SetForChecks(true, true, false);
			Check(!HitActCadence.AwarenessSplitEnabled, "the working cadence split leaves launch awareness whole");
			HitActCadence.SetForChecks(true, true, true);
			Check(HitActCadence.AwarenessSplitEnabled, "awareness sharing is a separate explicit probe");
			Check(Math.Abs(HitActCadence.LaunchAwareness(.08f, .2f, .3f, .1f, .5f) - .38f) < 1e-6f,
				"concurrent launches must share fame and campaign awareness while preserving discovery");
			RecordRuntimeData Rec(string id, string artist, int pos, ReleaseFormat format) =>
				new RecordRuntimeData { baseRecord = new Record { recordId = id, artistId = artist, format = format }, currentPosition = pos, radioHeat = 0.5f };
			var chart = new List<RecordRuntimeData> {
				Rec("p1", "act", 12, ReleaseFormat.Single), Rec("p2", "act", 88, ReleaseFormat.Single), Rec("new", "act", 99, ReleaseFormat.Single),
				Rec("lp", "act", 40, ReleaseFormat.Album), Rec("x", "other", 5, ReleaseFormat.Single), null
			};
			List<RecordRuntimeData> preds = HitActCadence.FilterPredecessors(chart, "act", "new");
			Check(preds.Count == 2 && preds[0].baseRecord.recordId == "p1" && preds[1].baseRecord.recordId == "p2",
				"predecessors must be the act's other charting singles only");
			HitActCadence.CutPredecessors(preds);
			Check(Math.Abs(preds[0].radioHeat - 0.5f * (1f - HitActCadence.PredecessorRadioCut)) < 1e-6f &&
				chart[4].radioHeat == 0.5f, "only predecessors lose radio, by the cut");

			// Retention: renew on a contract Top 40; protect within 52 weeks of a Top 40.
			var keeper = new SimulatedArtist { contractTop40Hits = 1 };
			var miss = new SimulatedArtist { contractTop40Hits = 0 };
			Check(HitActCadence.ShouldRenew(keeper) && !HitActCadence.ShouldRenew(miss), "renewal must turn on a contract Top 40");
			var faded = new SimulatedArtist { careerState = CareerState.Declining, consecutiveFlops = 3 };
			HitActCadence.OnTop40(faded, 300);
			Check(HitActCadence.ProtectsFromDrop(faded, 300 + HitActCadence.RetentionWeeks), "52 weeks after a Top 40 is protected");
			Check(!HitActCadence.ProtectsFromDrop(faded, 301 + HitActCadence.RetentionWeeks), "53 weeks after is not");
			Check(!HitActCadence.ProtectsFromDrop(new SimulatedArtist(), 10), "an act with no Top 40 is not protected");
			// The ladder's own fall: Declining with three flops drops, unless a Top 40 is inside the year.
			int now = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
			var ladderHit = new SimulatedArtist { careerState = CareerState.Declining, consecutiveFlops = 3 };
			HitActCadence.OnTop40(ladderHit, now);
			ladderHit.RegisterChartEntry();
			Check(ladderHit.careerState == CareerState.Declining, "retention must hold the ladder's drop inside the year");
			var ladderCold = new SimulatedArtist { careerState = CareerState.Declining, consecutiveFlops = 3 };
			ladderCold.RegisterChartEntry();
			Check(ladderCold.careerState == CareerState.Dropped, "an act with no recent Top 40 still falls to Dropped");

			// Lane alone leaves retention off, and the reverse.
			HitActCadence.SetForChecks(true, false);
			Check(!HitActCadence.ShouldRenew(keeper), "lane-only must not renew");
			HitActCadence.SetForChecks(false, true);
			Check(HitActCadence.LaunchShare(2) == 1f && !HitActCadence.LaneEnabled, "retention-only must not split or run the lane");
			HitActCadence.SetForChecks(false, false);
			var offLadder = new SimulatedArtist { careerState = CareerState.Declining, consecutiveFlops = 3, hitCadenceLastTop40Week = now };
			offLadder.RegisterChartEntry();
			Check(offLadder.careerState == CareerState.Dropped, "off must leave the ladder's drop untouched");
			GD.Print("HIT_ACT_CADENCE_CHECK passed");
		} finally {
			HitActCadence.SetForChecks(wasLane, wasRetention, wasAwareness);
		}
	}
}
