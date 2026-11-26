using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

/// <summary>
/// How often an act on a bill can actually be signed (playtest complaint 2026-11-25: "it's always not looking, playing
/// locally, or signed"). Founds a player label, then each day for --days (default 56) reads every bill in every scene town
/// and labels each act the way its notebook card would: under contract, no deal yet (askable: the act answers from its
/// disposition, ActDisposition), asking (on the market now), or not taking offers (on the market but blocked). Read-only
/// on the world; it only ends days.
///
/// Step 1 of SimTools/SigningRealismBreakdown.md adds three read-only measurements:
/// <list type="bullet">
/// <item><b>Who the signed acts on bills are</b>: label tier, career state, charted or not, weeks left on the deal. A signed
/// act with a national career (charted, Established and up, or on a Major) is the candidate for leaving the small rooms
/// (§3.5a).</item>
/// <item><b>The resident census</b>: every act present in each town each week, billed or not, by standing. Unsigned acts
/// that never took a stage that week are the supply that could fill slots a national act gives up.</item>
/// <item><b>The disposition</b> (§3.1, built in step 2 as ActDisposition) for each unsigned act heard, from real state only: ambition, day jobs,
/// ages, the share of the set that is their own, wealth, a standing engagement, a past drop, a manager. Prints the drive
/// distribution and the Hungry / Careful / Comfortable mix at the draft thresholds, plus frictions that would slow a
/// signing without being reluctance (members under 21, a manager).</item>
/// </list>
/// Writes SimLogs/signing-availability.csv (one row per bill slot) and SimLogs/signing-dispositions.csv (one row per
/// distinct unsigned act heard).
///   --path . SimTools/SigningAvailabilityProbe.tscn -- --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle
/// </summary>
public partial class SigningAvailabilityProbe : Node {
	private enum Standing { Signed, NotLooking, Asking, NotTaking, Gone }

	private sealed class Slot {
		public int Week, Day; public string Place, Kind, Role, ArtistId; public bool Residency; public Standing S; public int Contracts;
		public string Tier, Career; public int Charted, WeeksLeft; public bool National; public float Originals; public int SetSize;
	}

	/// <summary>The draft disposition inputs for one unsigned act, read once (the last time it was heard).</summary>
	private sealed class Read {
		public string ArtistId, Place, Name; public int Members, MedianAge, Minors; public float Ambition, MinAmbition, Wealth, Originals;
		public bool DayJobs, Engaged, Burned, Managed, HadContract, NeverSigned; public float Drive; public string Disposition, Reason, Manager;
		/// <summary>A hungry act that could sign on the spot: no member under 21, no holdout.</summary>
		public bool OnTheSpot;
		/// <summary>Step 9: the holdout's objection, whether its true answer is open to a new label the first night (no second
		/// night heard, no record out), and whether the band could sign without them. Comfortable acts: the hearing's objection.</summary>
		public CourtshipObjection Holdout, Hearing; public bool HoldoutOpen, HearingOpen, LeaveBehind;
	}

	// Draft thresholds (SigningRealismBreakdown §3.1). Design inputs for the probe only; step 2 owns the real ones.
	private const float ComfortableBelow = ActDisposition.ComfortableBelow, HungryFrom = ActDisposition.HungryFrom;

	public override void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			var args = OS.GetCmdlineUserArgs();
			int days = int.TryParse(args.FirstOrDefault(a => a.StartsWith("--days="))?.Substring(7), out int d) ? d : 56;
			string home = args.FirstOrDefault(a => a.StartsWith("--home="))?.Substring(7) ?? "detroit";
			var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
			if (!desk.FoundLabel("Availability Probe", home, out string message)) throw new InvalidOperationException(message);
			desk.DismissLienMorning();
			var places = SceneRoomCatalog.All.Select(r => r.PlaceId).Distinct().OrderBy(p => p).ToList();
			var rows = new List<Slot>();
			var reads = new Dictionary<string, Read>(StringComparer.Ordinal);
			// Census: per (week, town) every present act and its standing; billed ids per (week, town).
			var census = new List<(int Week, string Place, string ArtistId, Standing S, bool National, bool DayJobs)>();
			var billedThisWeek = new HashSet<(int Week, string Place, string ArtistId)>();
			for (int day = 0; day < days && !clock.IsGameOver; day++) {
				int today = LocalSceneRoomService.Day(clock.CurrentDate), week = ChartManager.Instance.GetCurrentChartWeek();
				int year = clock.CurrentDate.year;
				if (day % 7 == 0)
					foreach (string place in places) {
						string scene = LocalScenePersistenceService.SceneIdFor(place);
						if (scene == null || census.Any(c => c.Week == day / 7 + 1 && c.Place == scene)) continue;
						foreach (string id in LocalScenePersistenceService.MembersOf(scene)) {
							var a = ArtistManager.Instance.GetArtist(id);
							if (a == null || !LocalScenePersistenceService.Present(a, scene, today / 7)) continue;
							census.Add((day / 7 + 1, scene, id, StandingOf(a, week), National(a), !LocalSceneRoomService.FullTime(a, today)));
						}
					}
				foreach (string place in places)
					foreach (var bill in LocalSceneRoomService.Calendar(place, clock.CurrentDate).Where(b => b.Day == today))
						foreach (var slot in bill.Appearances) {
							var a = ArtistManager.Instance.GetArtist(slot.ArtistId);
							Standing s = StandingOf(a, week);
							var label = string.IsNullOrEmpty(a?.labelId) ? null : ChartManager.Instance.GetLabelById(a.labelId);
							var set = slot.Set ?? new();
							var row = new Slot { Week = day / 7 + 1, Day = today, Place = place, Kind = SceneRoomCatalog.Get(bill.RoomId)?.Kind.ToString() ?? "?",
								Role = slot.Role ?? "", Residency = slot.Residency, ArtistId = slot.ArtistId, S = s, Contracts = a?.contractSequence ?? 0,
								Tier = label?.tier.ToString() ?? "", Career = a?.careerState.ToString() ?? "", Charted = a?.charted ?? 0,
								WeeksLeft = a != null && s == Standing.Signed && a.contractExpiresWeek > 0 ? a.contractExpiresWeek - week : -1,
								National = a != null && s == Standing.Signed && National(a),
								Originals = set.Count == 0 ? -1f : set.Count(x => x.Original) / (float)set.Count, SetSize = set.Count };
							rows.Add(row);
							string scene = LocalScenePersistenceService.SceneIdFor(place);
							if (scene != null) billedThisWeek.Add((row.Week, scene, slot.ArtistId));
							if (a != null && s is Standing.NotLooking or Standing.Asking or Standing.NotTaking)
								reads[a.artistId] = ReadOf(a, place, year, today, slot.Residency, row.Originals);
						}
				clock.EndDay();
				if (GetTree().Root.FindChild("MorningPaper", true, false) is Control paper) paper.Visible = false;
			}

			string Pct(int n, int of) => of == 0 ? "-" : $"{100.0 * n / of,5:0.0}%";
			string Line(string label, IEnumerable<Standing> group) {
				var g = group.ToList(); int n = g.Count;
				return $"{label,-22} n={n,5}  signed {Pct(g.Count(x => x == Standing.Signed), n)}  not-looking {Pct(g.Count(x => x == Standing.NotLooking), n)}  " +
					$"asking {Pct(g.Count(x => x == Standing.Asking), n)}  not-taking {Pct(g.Count(x => x == Standing.NotTaking), n)}  gone {Pct(g.Count(x => x == Standing.Gone), n)}";
			}
			GD.Print($"=== SIGNING AVAILABILITY: {days} days from {home}, {places.Count} towns ===");
			GD.Print(Line("ALL bill slots", rows.Select(r => r.S)));
			// One act heard many nights counts once per week here: what a player sees walking into rooms that week.
			GD.Print(Line("distinct act-weeks", rows.GroupBy(r => (r.Week, r.ArtistId)).Select(g => g.Last().S)));
			GD.Print(Line("distinct acts (last)", rows.GroupBy(r => r.ArtistId).Select(g => g.Last().S)));
			GD.Print("-- by week --");
			foreach (var g in rows.GroupBy(r => r.Week).OrderBy(g => g.Key)) GD.Print(Line("week " + g.Key, g.Select(r => r.S)));
			GD.Print("-- by town --");
			foreach (var g in rows.GroupBy(r => r.Place).OrderBy(g => g.Key)) GD.Print(Line(g.Key, g.Select(r => r.S)));
			GD.Print("-- by room kind --");
			foreach (var g in rows.GroupBy(r => r.Kind).OrderBy(g => g.Key)) GD.Print(Line(g.Key, g.Select(r => r.S)));
			GD.Print("-- residency vs booking --");
			foreach (var g in rows.GroupBy(r => r.Residency).OrderBy(g => g.Key)) GD.Print(Line(g.Key ? "residency" : "announced booking", g.Select(r => r.S)));
			GD.Print("-- unsigned by career --");
			foreach (var g in rows.Where(r => r.S != Standing.Signed).GroupBy(r => r.Contracts > 0 ? "had a contract" : "never signed").OrderBy(g => g.Key))
				GD.Print(Line(g.Key, g.Select(r => r.S)));
			int open = rows.Count(r => r.S == Standing.Asking);
			GD.Print($"Per bill slot: can talk terms now {Pct(open, rows.Count)}; unsigned and nobody's asked {Pct(rows.Count(r => r.S == Standing.NotLooking), rows.Count)} " +
				"(the ask now answers from ActDisposition: see DISPOSITION below); " +
				$"under contract / not taking / gone {Pct(rows.Count(r => r.S is Standing.Signed or Standing.NotTaking or Standing.Gone), rows.Count)}.");

			// ---- 1. Who the signed acts on bills are ----
			var signed = rows.Where(r => r.S == Standing.Signed).ToList();
			var signedActs = signed.GroupBy(r => r.ArtistId).Select(g => g.Last()).ToList();
			GD.Print($"=== SIGNED ACTS ON BILLS: {signed.Count} slots, {signedActs.Count} acts ===");
			GD.Print($"national career (charted, Established+, or on a Major): slots {Pct(signed.Count(r => r.National), signed.Count)}, acts {Pct(signedActs.Count(r => r.National), signedActs.Count)}; " +
				$"= {Pct(signed.Count(r => r.National), rows.Count)} of ALL bill slots");
			GD.Print("-- by label tier (slots / acts) --");
			foreach (var g in signed.GroupBy(r => r.Tier).OrderByDescending(g => g.Count()))
				GD.Print($"  {g.Key,-14} slots {Pct(g.Count(), signed.Count)}  acts {Pct(g.Select(r => r.ArtistId).Distinct().Count(), signedActs.Count)}");
			GD.Print("-- by career state (acts) --");
			foreach (var g in signedActs.GroupBy(r => r.Career).OrderByDescending(g => g.Count()))
				GD.Print($"  {g.Key,-14} {Pct(g.Count(), signedActs.Count)}  charted {Pct(g.Count(r => r.Charted > 0), g.Count())}");
			var left = signedActs.Where(r => r.WeeksLeft >= 0).Select(r => r.WeeksLeft).OrderBy(x => x).ToList();
			if (left.Count > 0)
				GD.Print($"-- weeks left on the deal (acts, at last hearing) -- n={left.Count} p10 {Q(left, .1)} p25 {Q(left, .25)} median {Q(left, .5)} p75 {Q(left, .75)} p90 {Q(left, .9)}; " +
					$"ending within 13 wk {Pct(left.Count(x => x <= 13), left.Count)}, within 26 wk {Pct(left.Count(x => x <= 26), left.Count)}");
			GD.Print($"no expiry week set: {Pct(signedActs.Count(r => r.WeeksLeft < 0), signedActs.Count)} of signed acts");

			// ---- 2. Resident census: could unsigned acts fill the slots a national act gives up? ----
			GD.Print("=== RESIDENT CENSUS (acts present in town, per week) ===");
			var weeks = census.GroupBy(c => (c.Week, c.Place)).ToList();
			double Mean(Func<IGrouping<(int, string), (int Week, string Place, string ArtistId, Standing S, bool National, bool DayJobs)>, int> f) =>
				weeks.Count == 0 ? 0 : weeks.Average(f);
			GD.Print($"per town-week: residents {Mean(g => g.Count()):0.0}; signed national {Mean(g => g.Count(c => c.S == Standing.Signed && c.National)):0.0}; " +
				$"signed local {Mean(g => g.Count(c => c.S == Standing.Signed && !c.National)):0.0}; unsigned {Mean(g => g.Count(c => c.S != Standing.Signed && c.S != Standing.Gone)):0.0}");
			var nationalSlotsByTownWeek = rows.Where(r => r.National).Select(r => (r.Week, Scene: LocalScenePersistenceService.SceneIdFor(r.Place)))
				.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
			var table = new List<(string Town, double Residents, double Unsigned, double IdleUnsigned, double IdleFullTime, double NationalSlots, double Slots)>();
			foreach (var town in census.GroupBy(c => c.Place).OrderBy(g => g.Key)) {
				var perWeek = town.GroupBy(c => c.Week).ToList();
				double idle = perWeek.Average(w => w.Count(c => c.S != Standing.Signed && c.S != Standing.Gone && !billedThisWeek.Contains((w.Key, town.Key, c.ArtistId))));
				double idleFull = perWeek.Average(w => w.Count(c => c.S != Standing.Signed && c.S != Standing.Gone && !c.DayJobs && !billedThisWeek.Contains((w.Key, town.Key, c.ArtistId))));
				double natSlots = perWeek.Average(w => nationalSlotsByTownWeek.TryGetValue((w.Key, town.Key), out int n) ? n : 0);
				double slots = perWeek.Average(w => rows.Count(r => r.Week == w.Key && LocalScenePersistenceService.SceneIdFor(r.Place) == town.Key));
				table.Add((town.Key, perWeek.Average(w => w.Count()), perWeek.Average(w => w.Count(c => c.S != Standing.Signed && c.S != Standing.Gone)), idle, idleFull, natSlots, slots));
			}
			GD.Print($"{"town",-16} {"resid",6} {"unsgnd",6} {"idleU",6} {"idleFT",6} {"natSlt",6} {"slots",6}   (per week; idleU = unsigned present but on no bill that week)");
			foreach (var t in table) GD.Print($"{t.Town,-16} {t.Residents,6:0.0} {t.Unsigned,6:0.0} {t.IdleUnsigned,6:0.0} {t.IdleFullTime,6:0.0} {t.NationalSlots,6:0.0} {t.Slots,6:0.0}");
			GD.Print($"towns where idle unsigned acts < national slots: {table.Count(t => t.IdleUnsigned < t.NationalSlots / 3.0)} of {table.Count} " +
				"(a weekend act covers ~3 nights, so idle unsigned acts x3 >= national slots means the slots can be filled)");

			// ---- 3. Draft disposition of the unsigned acts heard ----
			var rs = reads.Values.ToList();
			GD.Print($"=== DISPOSITION (ActDisposition): {rs.Count} distinct unsigned acts heard (thresholds comfortable < {ComfortableBelow:0.00}, hungry >= {HungryFrom:0.00}) ===");
			var drives = rs.Select(r => r.Drive).OrderBy(x => x).ToList();
			if (drives.Count > 0) GD.Print($"drive deciles: {string.Join(" ", Enumerable.Range(1, 9).Select(i => Q(drives, i / 10.0).ToString("0.00")))}");
			GD.Print($"mean ambition {rs.DefaultIfEmpty().Average(r => r?.Ambition ?? 0):0.00}; day jobs {Pct(rs.Count(r => r.DayJobs), rs.Count)}; standing engagement {Pct(rs.Count(r => r.Engaged), rs.Count)}; " +
				$"own songs >= half {Pct(rs.Count(r => r.Originals >= .5f), rs.Count)}; median age <= 22 {Pct(rs.Count(r => r.MedianAge <= 22), rs.Count)}, >= 32 {Pct(rs.Count(r => r.MedianAge >= 32), rs.Count)}");
			GD.Print($"history: never signed {Pct(rs.Count(r => r.NeverSigned), rs.Count)}; had a contract {Pct(rs.Count(r => r.HadContract), rs.Count)} (dropped for performance {Pct(rs.Count(r => r.Burned), rs.Count)}); demanding manager {Pct(rs.Count(r => r.Managed), rs.Count)}");
			GD.Print("-- mix --");
			foreach (var g in rs.GroupBy(r => r.Disposition).OrderByDescending(g => g.Count())) {
				GD.Print($"  {g.Key,-12} {Pct(g.Count(), rs.Count)}");
				foreach (var why in g.GroupBy(r => r.Reason).OrderByDescending(w => w.Count()).Take(4))
					GD.Print($"      {Pct(why.Count(), g.Count())}  {why.Key}");
			}
			GD.Print("-- the mix at other thresholds (comfortable below / hungry from) --");
			foreach (var (c, h) in new[] { (.42f, .52f), (.46f, .56f), (.48f, .58f), (.49f, .60f), (.52f, .62f) }) {
				int com = rs.Count(r => !r.Burned && r.Drive < c), hun = rs.Count(r => !r.Burned && r.Drive >= h);
				GD.Print($"  {c:0.00}/{h:0.00}: hungry {Pct(hun, rs.Count)}  careful {Pct(rs.Count - hun - com, rs.Count)}  comfortable {Pct(com, rs.Count)}");
			}
			GD.Print($"-- frictions that slow a yes without being reluctance -- a member under 21 {Pct(rs.Count(r => r.Minors > 0), rs.Count)} (all members under 21 {Pct(rs.Count(r => r.Minors == r.Members && r.Members > 0), rs.Count)}); " +
				$"any manager {Pct(rs.Count(r => r.Manager != "None"), rs.Count)}, a demanding one {Pct(rs.Count(r => r.Managed), rs.Count)}; a group (3+) with a low-ambition member (< .35) {Pct(rs.Count(r => r.Members >= 3 && r.MinAmbition < .35f), rs.Count)}");
			GD.Print($"-- manager types (unsigned heard) -- {string.Join("  ", rs.GroupBy(r => r.Manager).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {Pct(g.Count(), rs.Count)}"))}");
			var hungry = rs.Where(r => r.Disposition == "Hungry").ToList();
			GD.Print($"-- hungry acts that could sign tonight (no member under 21, no holdout, no manager or a local hustler) -- {Pct(hungry.Count(r => r.OnTheSpot), hungry.Count)} of hungry = {Pct(hungry.Count(r => r.OnTheSpot), rs.Count)} of unsigned heard; " +
				$"hungry with a member under 21 {Pct(hungry.Count(r => r.Minors > 0), hungry.Count)}; hungry with a holdout {Pct(hungry.Count(r => r.Members >= 3 && r.MinAmbition < .35f), hungry.Count)}");
			// Step 9: what the one who isn't sure says, and whether a new label can answer it the first night.
			var holding = rs.Where(r => r.Holdout != CourtshipObjection.None && r.Disposition != "Comfortable").ToList();
			GD.Print($"-- step 9: holdouts (hungry or careful acts with one) -- {holding.Count} acts, {Pct(holding.Count, rs.Count)} of unsigned heard; " +
				$"true answer open the first night {Pct(holding.Count(r => r.HoldoutOpen), holding.Count)}; could sign without them {Pct(holding.Count(r => r.LeaveBehind), holding.Count)}; " +
				$"neither (take your time is the way) {Pct(holding.Count(r => !r.HoldoutOpen && !r.LeaveBehind), holding.Count)}");
			foreach (var g in holding.GroupBy(r => r.Holdout).OrderByDescending(g => g.Count()))
				GD.Print($"      {g.Key,-10} {Pct(g.Count(), holding.Count)}  open {Pct(g.Count(r => r.HoldoutOpen), g.Count())}  leave behind {Pct(g.Count(r => r.LeaveBehind), g.Count())}");
			var comfy = rs.Where(r => r.Disposition == "Comfortable").ToList();
			GD.Print($"-- step 9: a comfortable act's objection when it hears you out -- {string.Join("  ", comfy.GroupBy(r => r.Hearing).OrderByDescending(g => g.Count()).Select(g => $"{g.Key} {Pct(g.Count(), comfy.Count)} (open to a new label {Pct(g.Count(r => r.HearingOpen), g.Count())})"))}");
			GD.Print("-- by town (hungry / careful / comfortable, acts) --");
			foreach (var g in rs.GroupBy(r => r.Place).OrderBy(g => g.Key))
				GD.Print($"  {g.Key,-16} n={g.Count(),3}  {Pct(g.Count(r => r.Disposition == "Hungry"), g.Count())} / {Pct(g.Count(r => r.Disposition == "Careful"), g.Count())} / {Pct(g.Count(r => r.Disposition == "Comfortable"), g.Count())}");

			string path = ProjectSettings.GlobalizePath("res://SimLogs/signing-availability.csv");
			File.WriteAllLines(path, new[] { "week,day,place,room_kind,role,residency,artist_id,standing,contracts,tier,career,charted,weeks_left,national,originals,set_size" }
				.Concat(rows.Select(r => $"{r.Week},{r.Day},{r.Place},{r.Kind},\"{r.Role}\",{r.Residency},{r.ArtistId},{r.S},{r.Contracts},{r.Tier},{r.Career},{r.Charted},{r.WeeksLeft},{r.National},{r.Originals:0.00},{r.SetSize}")));
			string dpath = ProjectSettings.GlobalizePath("res://SimLogs/signing-dispositions.csv");
			File.WriteAllLines(dpath, new[] { "artist_id,place,members,median_age,minors,ambition,min_ambition,wealth,originals,day_jobs,engaged,had_contract,burned,managed,manager,on_the_spot,drive,disposition,reason" }
				.Concat(rs.Select(r => $"{r.ArtistId},{r.Place},{r.Members},{r.MedianAge},{r.Minors},{r.Ambition:0.000},{r.MinAmbition:0.000},{r.Wealth:0.000},{r.Originals:0.00},{r.DayJobs},{r.Engaged},{r.HadContract},{r.Burned},{r.Managed},{r.Manager},{r.OnTheSpot},{r.Drive:0.000},{r.Disposition},\"{r.Reason}\"")));
			GD.Print("Wrote " + path + " and " + dpath);
			GD.Print("SIGNING_AVAILABILITY_PROBE_DONE");
			GetTree().Quit(0);
		} catch (Exception e) {
			GD.PrintErr("SIGNING_AVAILABILITY_PROBE_FAIL: " + e);
			GetTree().Quit(1);
		}
	}

	private static Standing StandingOf(SimulatedArtist a, int week) =>
		a == null || a.lifecycleStatus != ArtistLifecycleStatus.Active ? Standing.Gone
		: !string.IsNullOrEmpty(a.labelId) ? Standing.Signed
		: a.prospectMarketStatus == ProspectMarketStatus.Latent ? Standing.NotLooking
		: ArtistManager.Instance.IsEligibleForPopulationSigning(a, week) ? Standing.Asking : Standing.NotTaking;

	/// <summary>A signed act whose records travel: it has charted, its career is Established or above, or it's on a Major.</summary>
	private static bool National(SimulatedArtist a) {
		if (a == null || string.IsNullOrEmpty(a.labelId)) return false;
		var label = ChartManager.Instance.GetLabelById(a.labelId);
		return a.charted > 0 || label?.tier == LabelTier.Major ||
			a.careerState is CareerState.Established or CareerState.Star or CareerState.Superstar;
	}

	/// <summary>The act's disposition toward the probe's label, read by the game's own ActDisposition (step 2).</summary>
	private static Read ReadOf(SimulatedArtist a, string place, int year, int today, bool residency, float originals) {
		var d = ActDisposition.Read(a, PlayerDesk.Instance.Label, today, originals < 0 ? null : originals);
		var people = a.members.Where(m => m != null && m.isActive && m.lifeState == MemberLifeState.Active).ToList();
		return new Read { ArtistId = a.artistId, Place = place, Name = a.stageName, Members = d.Members, MedianAge = d.MedianAge,
			Minors = d.Minors.Count, Ambition = d.Ambition, MinAmbition = people.Count == 0 ? .5f : people.Min(m => m.ambition),
			Wealth = people.Count == 0 ? 0f : people.Max(MemberWealthService.Norm), Originals = originals, DayJobs = d.DayJobs,
			Engaged = d.EngagementRoom != null, HadContract = a.contractSequence > 0, NeverSigned = a.contractSequence == 0,
			Burned = d.Reason == DispositionReason.DroppedBefore, Managed = d.DemandingManager, Manager = a.manager.ToString(),
			Drive = d.Drive, Disposition = d.Kind.ToString(), Reason = d.Reason.ToString(), OnTheSpot = d.SignsTonight,
			Holdout = HoldoutOf(a, d, people, today, originals, out bool open, out bool behind), HoldoutOpen = open, LeaveBehind = behind,
			Hearing = d.Kind == DealDisposition.Comfortable ? PlayerDesk.HearingObjection(d) : CourtshipObjection.None,
			HearingOpen = d.Kind == DealDisposition.Comfortable && PlayerDesk.HearingObjection(d) switch {
				CourtshipObjection.Road => HomeTown(a), CourtshipObjection.Money => originals >= PlayerDesk.OwnSongsHeard, _ => false } };
	}

	private static bool HomeTown(SimulatedArtist a) =>
		LocalScenePersistenceService.SceneIdFor(a.geography?.basePlaceId) is { } t && t == PlayerDesk.Instance.Label?.homeCityId;

	/// <summary>The objection the game would raise for this act's holdout (PlayerDesk.Objections), and whether a new label's
	/// true answer is open on the first night: its songs heard, a label in their town, the band's best player, a day job.
	/// School wants the Sunday with the family first, a new label has paid nobody a royalty, and the fun has no answer.</summary>
	private static CourtshipObjection HoldoutOf(SimulatedArtist a, DispositionRead d, List<Musician> people, int today, float originals, out bool open, out bool behind) {
		open = behind = false;
		var m = people.FirstOrDefault(x => x.personId == d.HoldoutId);
		if (m == null) return CourtshipObjection.None;
		var o = PlayerDesk.HoldoutObjection(a, m, today);
		open = o switch {
			CourtshipObjection.Songs => originals >= PlayerDesk.OwnSongsHeard,
			CourtshipObjection.Road => HomeTown(a),
			CourtshipObjection.Money => people.All(x => x.technicalSkill <= m.technicalSkill),
			CourtshipObjection.Job => true,
			_ => false
		};
		behind = people.Count >= PlayerDesk.SignWithoutMinMembers && !m.isBandLeader && !m.isLeadVocalist && !m.isPrimaryWriter;
		return o;
	}

	private static double Q<T>(List<T> sorted, double f) where T : IConvertible =>
		sorted.Count == 0 ? 0 : Convert.ToDouble(sorted[Math.Clamp((int)Math.Ceiling(sorted.Count * f) - 1, 0, sorted.Count - 1)]);
}
