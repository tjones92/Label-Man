using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;
using static LabelManFatigueState;

/// <summary>F1 on the real clock, desk, and save payload. Run again with --disable-label-man-fatigue for the control.</summary>
public partial class LabelManFatigueChecks : Node {
	private readonly bool disabled = OS.GetCmdlineUserArgs().Contains(PlayerDesk.DisableFatigueSwitch);
	private PlayerDesk Desk => PlayerDesk.Instance;
	private TimeManager Clock => TimeManager.Instance;
	private int checks;

	private void Check(bool ok, string message) {
		if (!ok) throw new InvalidOperationException(message);
		checks++;
		GD.Print("ok: " + message);
	}

	public override async void _Ready() {
		try {
			PlayerDesk.AutosaveOffForChecks = true;
			Check(Desk.FatigueState == Fresh && Desk.FatigueOwedMinutes == 0, "No fatigue before founding");
			Check(Desk.FoundLabel("Fatigue Checks", "nashville", out string message), message);
			Desk.DismissLienMorning();
			Desk.Label.cashReserves = 80000;
			Check(Desk.NowMinute == 9 * 60 && Desk.FatigueState == (disabled ? Fresh : Worn) && Desk.FatigueOwedMinutes == (disabled ? 0 : 240),
				"First morning starts at nine with the expected waking state");
			CheckEdges(disabled ? 0 : 1);
			Check(Desk.PassTime(3, out message, stopForCallers: true), message);
			Check(Desk.CaptureFirstWeekForChecks().LongWaitLesson, "The first three-hour desk wait records its opportunity-cost lesson");
			Check(Desk.PassTime(6, out message), message);
			Check(disabled || Desk.TonightFatiguePreview(21 * 60)?.Contains("wake worn") == true,
				"The opening forecast compares staying out with turning in at six");
			Check(Desk.TonightFatiguePreview(18 * 60 + 5) == null, "An action that keeps tomorrow's state has no fatigue warning");
			if (!disabled) {
				var atSix = Desk.CaptureState();
				Check(Clock.SpendMinutes(1) && Desk.CaptureFirstWeekForChecks().OvertimeLesson, "The opening overtime lesson triggers when time crosses six");
				Clock.RestoreClock(GameDate.StartDate, 18);
				Check(Desk.RestoreState(atSix, out message) && Desk.CaptureFirstWeekForChecks().LongWaitLesson, "A reload at six preserves the lesson already heard");
			}
			Clock.EndDay();
			Check(Desk.FatigueState == Fresh && Desk.FatigueOwedMinutes == 0, "Ending at six clears the opening hangover");
			if (!disabled) CheckOvertimeAndSave();
			await CheckLateStudio();
			if (!disabled) CheckBus();
			else Check(Desk.CaptureState().Fatigue == null, "The disabled control writes no fatigue payload");
			await CheckNight();
			if (!disabled) CheckLateRecovery();
			GD.Print($"LABEL_MAN_FATIGUE_CHECKS_PASS checks={checks} disabled={disabled}");
			GetTree().Quit(0);
		} catch (Exception error) {
			GD.PrintErr("LABEL_MAN_FATIGUE_CHECKS_FAIL " + error);
			GetTree().Quit(1);
		}
	}

	private void CheckOvertimeAndSave() {
		for (int day = 0; day < 2; day++) {
			Check(Desk.PassTime(12, out string message), message);
			Check(Desk.FatigueOwedMinutes == (day + 3) * 60 && Desk.FatigueState == Fresh, "A full overtime day adds three hours and keeps the waking state");
			Clock.EndDay();
			Check(Desk.FatigueOwedMinutes == (day + 1) * 60 && Desk.NowMinute == 9 * 60 && Desk.FatigueState == Fresh,
				"A 9 PM finish recovers two hours and the next morning starts at nine");
		}

		Check(Clock.SpendMinutes(8 * 60 + 50) && Clock.SpendMinutes(25) && Desk.FatigueOwedMinutes == 135,
			"A short action crossing six charges only its fifteen overtime minutes");
		Check(Clock.SpendHours(1, allowOvertime: true) && Desk.FatigueOwedMinutes == 195 && Desk.FatigueState == Fresh,
			"Owed time can cross the WORN threshold while the waking state stays FRESH");
		var date = Clock.CurrentDate;
		var saved = Desk.CaptureState();
		string json = JsonSerializer.Serialize(saved, SaveGameService.TestJsonOptions);
		Check(Clock.SpendMinutes(15) && Desk.FatigueOwedMinutes == 210, "The live clock keeps charging after the save");
		Clock.RestoreClock(date, 19, 15);
		Check(Desk.RestoreState(JsonSerializer.Deserialize<PlayerSaveData>(json, SaveGameService.TestJsonOptions), out string loaded), loaded);
		var restored = Desk.CaptureState().Fatigue;
		Check(Desk.FatigueOwedMinutes == 195 && Desk.FatigueState == Fresh && restored.SpentTodayMinutes == 615 && restored.LastDayEndMinute == 21 * 60,
			"The player save restores minute debt, waking state, time spent and the last finish");
		Check(Clock.SpendMinutes(105) && Desk.FatigueOwedMinutes == 300 && !Clock.SpendMinutes(1) && Desk.FatigueOwedMinutes == 300,
			"The restored evening charges remaining minutes once and a refused action charges nothing");
		Clock.EndDay();
		Check(Desk.FatigueState == Worn && Desk.FatigueOwedMinutes == 180 && Desk.NowMinute == 9 * 60,
			"Three full overtime evenings leave the next morning WORN at nine");
		Check(Desk.FatigueStatusTip().Contains("3 evenings past 6 PM") && !Desk.FatigueStatusTip().Contains("night past"),
			"The tooltip counts completed overtime evenings, including the two preserved across the reload");

		var legacy = Desk.CaptureState();
		legacy.Fatigue.RecentFinishMinutes = null;
		Check(Desk.RestoreState(legacy, out loaded) && Desk.FatigueStatusTip().Contains("1 evening past 6 PM"),
			"A save from before the history field uses its last recorded finish");
		legacy.Fatigue = null;
		string oldJson = JsonSerializer.Serialize(legacy, SaveGameService.TestJsonOptions);
		Check(Desk.RestoreState(JsonSerializer.Deserialize<PlayerSaveData>(oldJson, SaveGameService.TestJsonOptions), out loaded), loaded);
		Check(Desk.FatigueState == Fresh && Desk.FatigueOwedMinutes == 0, "A save with no fatigue fields loads FRESH");
	}

	private async Task CheckLateStudio() {
		var date = Clock.CurrentDate;
		var band = ArtistManager.Instance.GetAllArtists().Where(a => a.labelId != null && a.lifecycleStatus == ArtistLifecycleStatus.Active &&
			!a.instrumentalPerformance && a.members.Count(m => m.isActive && m.lifeState == MemberLifeState.Active) >= 3)
			.OrderBy(a => a.artistId, StringComparer.Ordinal)
			.First(a => LocalSceneRoomService.StudioParticipantsFree(Desk.Label.labelId, a.members.Select(m => m.personId), date, 18 * 60, 26 * 60));
		ChartManager.Instance.GetLabelById(band.labelId)?.roster.Remove(band);
		band.labelId = Desk.Label.labelId;
		band.artistCreativeControl = false;
		Desk.Label.roster.Add(band);
		var room = Desk.StudioRoomIds().Select(id => Desk.StudioOffer(id, band.artistId))
			.First(offer => offer is { Local: true, Locked: null, Room.EveningWork: true, Room.Specialist: false });
		PlayerDesk.MaterialChoice[] songs = { new() { Kind = PlayerDesk.MaterialKind.Original, Title = "Night Side" } };
		Check(Desk.PassTime(9, out string message), message);
		Check(Desk.StartSession(band, songs, room.Tier, 8, out message, overrideRefusal: true, roomId: room.Room.Id,
			options: new StudioBookingOptions { Evening = true, LateNight = true }), "Book an evening studio date: " + message);
		Check(Desk.BreakStudio(out message) && Clock.SpendMinutes(25 * 60 - Desk.NowMinute), "The paid date and clock run to 1 AM: " + message);
		var strip = new DeskHourStrip();
		strip.Set(Clock.WorkdayStartHour, Clock.RegularWorkdayEndHour, Clock.HardStopHour, Desk.NowMinute, Desk.FatigueWakeMinute);
		Check(strip.Blocks == 16 && strip.UsedMinutes == 16 * 60, "The hour strip adds four late blocks at 1 AM");
		strip.Free();
		Check(Desk.FatigueOwedMinutes == (disabled ? 0 : 660) && Desk.FatigueState == Fresh, disabled
			? "The control keeps debt at zero through the 1 AM finish"
			: "The 1 AM finish charges eleven hours and the state stays FRESH until morning");
		Desk.ScrapSession();
		Clock.EndDay();
		Check(Clock.CurrentDate == date.NextDay() && Desk.NowMinute == (disabled ? 9 : 12) * 60 && Desk.FatigueState == (disabled ? Fresh : Spent),
			"The studio night advances one day and wakes at the expected hour");
		CheckEdges(disabled ? 0 : 2);
		if (disabled) return;
		Check(Desk.FatigueStatusTip().Contains("1 night past 9 PM") && !Desk.FatigueStatusTip().Contains("evening past 6 PM"),
			"The tooltip counts the 1 AM finish as a late night");
		if (OS.GetCmdlineUserArgs().Contains("--fatigue-ui")) await CheckMorningUi(band);
		Check(Desk.PassTime(3, out message), message);
		Clock.EndDay();
		Check(Desk.NowMinute == 9 * 60 && Desk.FatigueState == Worn && Desk.FatigueOwedMinutes == 240, "Three hours of work after sleeping in still qualifies as a quiet day");
		Clock.SkipDays(1);
		Check(Desk.NowMinute == 9 * 60 && Desk.FatigueState == Fresh && Desk.FatigueOwedMinutes == 0, "A second quiet day clears the debt through the calendar skip");
		Check(Desk.FatigueQuietVoice == ExecutiveVoice.None && Desk.FatigueSigningBlock == null, "The voice and signing return after recovery");
	}

	private void CheckEdges(int steps) {
		var act = ArtistManager.Instance.GetAllArtists().First(a => string.IsNullOrEmpty(a.labelId));
		var objection = new ScoutingApproachStep { Kind = ScoutingApproachStepKind.Objection, Objection = CourtshipObjection.Songs };
		Check(Desk.ObjectionBluff(objection, act) == null, "Checks stay closed before the first release");
		int releases = Desk.Label.totalReleases;
		Desk.Label.totalReleases = 1;
		var check = Desk.ObjectionBluff(objection, act);
		Check(check != null && (steps == 0 ? !check.Modifiers.Any(m => m.Why is "Tired" or "Spent")
			: check.Modifiers.Contains((steps == 1 ? "Tired" : "Spent", -steps))), "The check preview and roll share the fatigue modifier");
		Desk.Label.totalReleases = releases;
		float[] axes = Enumerable.Repeat(.5f, SongProfile.AxisCount).ToArray();
		PolarObservation Read(AILabel observer) => PolarPlayerPerception.Observe(axes, .5f, .5f, .5f, .5f, observer,
			"fatigue-check", "heard-material:ear", "event", PolarEvidenceGate.FirstListen);
		var heard = Read(Desk.Label);
		var control = Read(new AILabel { labelId = Desk.Label.labelId, scoutingAbility = Desk.Label.scoutingAbility });
		Check(ReferenceEquals(heard, Read(Desk.Label)) && Math.Abs(heard.demandError - control.demandError * (1f + .2f * steps)) < .00001f,
			"Ear readings have deterministic fatigue-specific evidence and wider uncertainty");
		if (steps != 2) {
			Check(Desk.FatigueQuietVoice == ExecutiveVoice.None && Desk.FatigueSigningBlock == null, "The table and voices are available before SPENT");
			return;
		}
		var quiet = Desk.FatigueQuietVoice;
		Check(quiet == PlayerDesk.InstinctVoices.MinBy(v => PlayerDesk.InstinctScore(Desk.InstinctProfile, v)), "The least trusted voice goes quiet");
		var snapshot = JsonSerializer.Deserialize<PlayerSaveData>(JsonSerializer.Serialize(Desk.CaptureState(), SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
		Check(snapshot.Fatigue.QuietVoice == quiet, "The save carries the morning's quiet voice");
		var talk = new ContractTalk { prospect = new PlayerDesk.Prospect { Artist = act }, stage = ContractTalkStage.Tabling };
		int now = Desk.NowMinute;
		Check(!Desk.TableOffer(talk, 20, .05f, 2, 2, true, false, out string reason) && reason == Desk.FatigueSigningBlock
			&& !Desk.OfferContract(talk.prospect, 20, .05f, 2, 2, true, false, out reason) && reason == Desk.FatigueSigningBlock
			&& !Desk.RenewContract(act, 20, .05f, 2, 2, true, false, out reason) && reason == Desk.FatigueSigningBlock
			&& Desk.NowMinute == now, "The Suit blocks table offers and renewals before they spend time");
	}

	private async Task CheckMorningUi(SimulatedArtist band) {
		AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		var panel = GetTree().Root.FindChildren("*", "", true, false).OfType<PlayerDeskPanel>().First();
		panel.OpenAtTab("A&R");
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		var folder = (Control)panel.FindChild("OfficeFolder", owned: false);
		Check(folder.GetGlobalRect().GetCenter().DistanceTo(panel.GetGlobalRect().GetCenter()) < 1,
			$"The folder stays centered with the SPENT card and rent runway ({folder.GetGlobalRect()} in {panel.GetGlobalRect()})");
		var text = panel.FindChildren("*", "Control", true, false).OfType<Control>().Where(c => c.IsVisibleInTree())
			.Select(c => c switch { Label label => label.Text, RichTextLabel rich => rich.Text, _ => "" }).ToArray();
		Check(text.Any(line => line.Contains("12:00 PM")) && text.Any(line => line.Contains(Desk.FatigueQuietNotice)),
			"The desk shows the noon clock and the quiet voice after the studio night");
		Check(text.Any(line => line.StartsWith("SPENT")) && text.Any(line => line.StartsWith("You slept till noon") || line.StartsWith("You slept till 12"))
			&& text.Any(line => line.Contains("rent in hand")), "The folder shows the state, morning line, and rent runway");
		var strip = panel.FindChildren("*", "Control", true, false).OfType<DeskHourStrip>().First(s => s.IsVisibleInTree());
		Check(strip.Blocks == 12 && strip.LostMinutes == 180 && strip.UsedMinutes == 0,
			"The noon strip has three crossed-out hours and no time spent yet");
		Check(panel.FindChildren("*", "PanelContainer", true, false).OfType<TimeCard>()
			.Any(card => card.IsVisibleInTree() && card.TooltipText.Contains("1 night past 9 PM")),
			"The folder clock shows the recent-night history in its tooltip");
		Check(GetTree().Root.FindChild("SittingCountdown", true, false) is Label countdown && countdown.Text.Contains("DAYS TO SITTING"),
			"The painted calendar counts down to the sitting window");
		if (DisplayServer.GetName() != "headless") {
			await ToSignal(GetTree().CreateTimer(.6), Timer.SignalName.Timeout);
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://tmp/fatigue-f4/spent-desk.png"));
		}
		var prospect = new PlayerDesk.Prospect { Artist = band, HasBaseline = true,
			Baseline = new ContractTermSheet(50, .05f, 2, 2, true, false, 0f, ManagerArchetype.None, null, null) };
		typeof(PlayerDeskPanel).GetMethod("ContractForm", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.Invoke(panel, new object[] { prospect });
		Check(panel.FindChildren("*", "Button", true, false).OfType<Button>().Any(b => b.Disabled && b.Text.StartsWith("TABLE OFFER")),
			"The terms sheet disables its table button on the SPENT morning");
	}

	private async Task CheckNight() {
		Clock.EndDay();
		var late = Desk.ScoutingItinerary().FirstOrDefault(o => o.StartMinute >= 21 * 60 && o.EndMinute <= 23 * 60);
		if (disabled) {
			Check(late == null, "The fatigue-off control keeps the old calendar and scouting hours");
			return;
		}
		Check(late != null, "A real after-9 PM set is listed in the local scouting itinerary");
		Check(Desk.WaitForSceneAppearance(late.BillId, late.EventId, out string message), message);
		Check(Desk.NowMinute == late.StartMinute && Desk.IsAtSceneRoom(late.RoomId), "Go & Wait takes you to the late set");
		int debt = Desk.FatigueOwedMinutes;
		Check(Desk.ListenSceneAppearance(late.BillId, late.EventId, out message), message);
		Check(Desk.NowMinute == late.EndMinute && Desk.Slate.Any(p => p.Artist.artistId == late.ArtistId)
			&& Desk.FatigueOwedMinutes == debt + 2 * (late.EndMinute - late.StartMinute), "Listening after nine records the act and charges twice the minutes");
		Check(!Clock.SpendMinutes(1) && Desk.WaitMinutes(1) == 0, "The venue visit leaves the ordinary desk cutoff in force");

		var act = ArtistManager.Instance.GetArtist(late.ArtistId);
		var room = SceneRoomCatalog.Get(late.RoomId);
		int regard = Desk.ReadRegard(act).Score;
		int familiarity = Desk.SceneContactFamiliarity(room.ContactId);
		int owed = Desk.FatigueOwedMinutes;
		var afterSet = Desk.CaptureState();
		var panel = GetTree().Root.FindChildren("*", "", true, false).OfType<PlayerDeskPanel>().FirstOrDefault();
		if (panel != null) {
			typeof(PlayerDeskPanel).GetMethod("OpenTheSet", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
				.Invoke(panel, new object[] { Desk });
			panel.OpenAtTab("A&R");
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			var drinks = panel.FindChildren("*", "Button", true, false).OfType<Button>()
				.Where(b => b.IsVisibleInTree() && b.Text.StartsWith("DRINK WITH ")).ToArray();
			Check(drinks.Length == 2 && drinks.All(b => b.TooltipText.Contains("adds one hour of fatigue")),
				"The set offers both companions and explains the fatigue cost");
			if (DisplayServer.GetName() != "headless") {
				await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
				GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://tmp/fatigue-f4/after-set.png"));
			}
			drinks.Single(b => b.Text == $"DRINK WITH {act.stageName.ToUpperInvariant()}").EmitSignal(Button.SignalName.Pressed);
		} else Check(Desk.ShareAfterSetDrink(late.ArtistId, AfterSetCompanion.Act, out message), message);
		Check(Desk.ReadRegard(act).Score == regard + 1 && Desk.SceneContactFamiliarity(room.ContactId) == familiarity,
			"A drink with the act gives one point of act regard");
		Check(Desk.NowMinute == late.EndMinute + 30 && Desk.FatigueOwedMinutes == owed + 120,
			"A late drink costs 30 minutes at the night rate plus one hour of fatigue");
		var withDrink = JsonSerializer.Deserialize<PlayerSaveData>(
			JsonSerializer.Serialize(Desk.CaptureState(), SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
		Check(Desk.RestoreState(withDrink, out message) && Desk.ReadRegard(act).Score == regard + 1
			&& !Desk.ShareAfterSetDrink(late.ArtistId, AfterSetCompanion.Booker, out _), "Drink regard survives JSON round-trip and a set only offers one drink");
		Clock.RestoreClock(Clock.CurrentDate, late.EndMinute / 60);
		Check(Desk.RestoreState(afterSet, out message) && Desk.ShareAfterSetDrink(late.ArtistId, AfterSetCompanion.Booker, out message), message);
		Check(Desk.KnowsSceneBooker(room.Id) && Desk.SceneContactFamiliarity(room.ContactId) == familiarity + 1 && Desk.ReadRegard(act).Score == regard,
			"A drink with the booker makes an introduction and gives one point of booker familiarity");
	}

	private void CheckLateRecovery() {
		Clock.EndDay();
		for (int day = 0; day < 7; day++) Clock.EndDay();
		Check(Desk.FatigueState == Fresh && !Desk.FatigueStatusTip().Contains("past seven days"),
			"The tooltip forgets finishes older than seven completed days");
		Check(Desk.TonightFatiguePreview(22 * 60 + 30)?.Contains("wake worn") == true
			&& Clock.SpendVenueMinutes(22 * 60 + 30 - Desk.NowMinute, 24 * 60),
			"The desk forecasts WORN for a 10:30 PM finish from fresh");
		Clock.EndDay();
		Check(Desk.FatigueState == Worn && Desk.FatigueOwedMinutes == 4 * 60 && Desk.NowMinute == 9 * 60,
			"A 10:30 PM finish clears two hours overnight and keeps the morning");
		Clock.EndDay();
		Check(Clock.SpendVenueMinutes(23 * 60 + 30 - Desk.NowMinute, 24 * 60), "Stay out until 11:30 PM from fresh");
		Clock.EndDay();
		Check(Desk.FatigueState == Spent && Desk.FatigueOwedMinutes == 6 * 60 + 30 && Desk.NowMinute == 10 * 60,
			"An 11:30 PM finish clears ninety minutes overnight and costs an hour in the morning");
	}

	private void CheckBus() {
		string home = Desk.CurrentCityId;
		var destination = Desk.ReachableCities().First(city => Desk.TravelQuote(home, city.cityId).Hours is >= 4 and <= 9);
		Check(Desk.ActiveMode == PlayerDesk.TransportMode.Bus && Desk.DriveTo(destination.cityId, out _), "Take a long intercity bus leg");
		Check(Desk.FatigueOwedMinutes == 60 && Desk.FatigueState == Fresh, "A long bus leg adds one hour while leaving the waking state fixed");
		Clock.EndDay();
		Desk.SetCarForChecks(PlayerDesk.CarKind.Used);
		Check(Desk.DriveHome(out _) && Desk.FatigueOwedMinutes == 0, "The car trip home adds no travel fatigue");
	}
}
