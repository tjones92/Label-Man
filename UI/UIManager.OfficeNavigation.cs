using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class UIManager {
	private readonly Dictionary<Button, Label> roomLabels = new();
	private Button officeDoor, officeBook, officeWait;
	private PanelContainer officeWaitMenu;
	private readonly Node3D[] ashtrayWaste = new Node3D[PlayerDesk.AshtrayCapacity];
	private Label officeYearGoal, officeWeekGoal;
	private Camera3D officePhoneCamera;
	private Transform3D officeWideShot;
	private Tween officePhoneTween;
	private bool officePhoneFocused;

	private void UpdateAshtray(int batches) {
		bool changed = false;
		for (int i = 0; i < ashtrayWaste.Length; i++) {
			bool show = i < batches;
			if (ashtrayWaste[i] == null || ashtrayWaste[i].Visible == show) continue;
			ashtrayWaste[i].Visible = show;
			changed = true;
		}
		if (changed && officeViewport.RenderTargetUpdateMode != SubViewport.UpdateMode.Always)
			officeViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
	}

	private bool CanUseOfficeNavigation() => PlayerDesk.Instance is { HasLabel: true, IsGameOver: false, LienMorningPending: false };

	private void BuildOfficeNavigation() {
		officeDoor = AddRoomHotspot("OfficeDoor", "GO OUT", new Rect2(530, 150, 213, 230), OpenOfficeDoor);
		officeBook = AddRoomHotspot("OfficeBook", "THE BOOK", new Rect2(974, 680, 286, 185),
			() => deskPanel?.OpenAppointmentBook(PlayerDesk.Instance, this));
		officeWait = AddRoomHotspot("OfficeAshtray", "WAIT", new Rect2(1443, 752, 475, 244), OpenOfficeClock);
		BuildOfficeWaitMenu();
		AddRoomHotspot("OfficeDrawer", "THE PAPER", new Rect2(797, 479, 468, 77), () => OpenOfficeAt("THE PAPER"));
		AddRoomHotspot("OfficeNoticeboard", "OFFICE SERVICES", new Rect2(75, 351, 180, 52), () => OpenOfficeAt("OFFICE SERVICES"));
		AddRoomHotspot("OfficePhoneRolodex", "PHONE & ROLODEX", new Rect2(1480, 320, 440, 164), OpenOfficePhone);
		officePhoneCamera = officeViewport?.GetCamera3D();
		if (officePhoneCamera != null) officeWideShot = officePhoneCamera.GlobalTransform;
		if (calendarButton != null) AddRoomLabel(calendarButton, "CALENDAR");
	}

	private Button AddRoomHotspot(string name, string caption, Rect2 area, Action open) {
		var button = new Button {
			Name = name, Flat = true, Position = area.Position, Size = area.Size,
			MouseDefaultCursorShape = Control.CursorShape.PointingHand, TooltipText = caption
		};
		StyleHotspot(button);
		button.Pressed += () => { if (CanUseOfficeNavigation() && !DeskCovered() && !PaperModal.IsOpen) open(); };
		AddChild(button);
		if (deskPanel != null) MoveChild(button, deskPanel.GetIndex());
		deskProps.Add(button);
		AddRoomLabel(button, caption);
		return button;
	}

	private void AddRoomLabel(Button button, string caption) {
		var tag = new PanelContainer {
			MouseFilter = Control.MouseFilterEnum.Ignore,
			GrowHorizontal = Control.GrowDirection.Both,
			GrowVertical = Control.GrowDirection.Begin
		};
		tag.AddThemeStyleboxOverride("panel", PaperStyleBox.Sheet(new Color("f1e5c8"), 9, 3, 3, PaperTheme.Rust));
		var text = new Label { Text = caption, MouseFilter = Control.MouseFilterEnum.Ignore };
		text.AddThemeFontOverride("font", PaperTheme.TypedBold);
		text.AddThemeFontSizeOverride("font_size", 15);
		text.AddThemeColorOverride("font_color", PaperTheme.Ink);
		tag.AddChild(text);
		button.AddChild(tag);
		tag.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.CenterBottom);
		roomLabels[button] = text;
		foreach (var signal in new[] { Control.SignalName.MouseEntered, Control.SignalName.MouseExited })
			button.Connect(signal, Callable.From(() => UpdateOfficeNavigation(PlayerDesk.Instance, DeskCovered())));
	}

	private void BuildOfficeGoals(VBoxContainer stack) {
		Label Line() {
			var label = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = Control.MouseFilterEnum.Ignore };
			label.AddThemeFontOverride("font", PaperTheme.TypedBold);
			label.AddThemeFontSizeOverride("font_size", 15);
			label.AddThemeColorOverride("font_color", PaperTheme.Ink);
			stack.AddChild(label);
			return label;
		}
		officeYearGoal = Line();
		officeWeekGoal = Line();
	}

	private void UpdateOfficeNavigation(PlayerDesk desk, bool covered) {
		bool available = CanUseOfficeNavigation() && !covered;
		bool teaching = desk?.FirstWeekCurrent != null;
		foreach (var (prop, label) in roomLabels) {
			prop.Visible = available;
			label.GetParent<PanelContainer>().Visible = available && prop.IsHovered() &&
				(prop != officeWait || officeWaitMenu?.Visible != true);
		}
		if (officeDoor != null) {
			bool knocking = desk?.Calling == PlayerDesk.CallerAt.Door;
			roomLabels[officeDoor].Text = knocking ? "ANSWER THE DOOR" : "GO OUT";
			officeDoor.TooltipText = roomLabels[officeDoor].Text;
		}
		if (!available || PaperModal.IsOpen) officeWaitMenu?.Hide();
		if (officeYearGoal == null) return;
		officeYearGoal.Visible = available && desk?.Lien != null;
		if (officeYearGoal.Visible) {
			var show = desk.LienShow();
			officeYearGoal.Text = $"THIS YEAR  ·  {show.What}";
		}
		officeWeekGoal.Visible = available && teaching;
		officeWeekGoal.Text = teaching ? "FIRST SINGLE  ·  Sign an act and get a record out." : "";
	}

	private static string OfficeNextAction(PlayerDesk desk) => desk.FirstWeekCurrent switch {
		FirstWeekStep.Hear => "Click the door and pick a set to hear.",
		FirstWeekStep.Answer when desk.FirstWeekVisitLater => "There’s time before your appointment. Check today’s sets or wait at the desk.",
		FirstWeekStep.Answer when desk.FirstWeekVisitToday => "Check the appointment book.",
		FirstWeekStep.Record or FirstWeekStep.Cut when desk.FirstWeekStudioTomorrow => "It’s too late to book today. Turn in at the calendar.",
		FirstWeekStep.Record => "Choose Studio at the door to set up your first record.",
		FirstWeekStep.Cut when desk.FirstWeekStudioOvernight => "Turn in at the calendar. The studio is booked again tomorrow.",
		FirstWeekStep.Cut when desk.Session != null => "Go back to the studio and print the sides.",
		FirstWeekStep.Cut => "Choose Studio at the door to cut the flip.",
		FirstWeekStep.Press or FirstWeekStep.Date when !desk.AtHome => "Drive home before doing the plant paperwork.",
		FirstWeekStep.Press when desk.FirstWeekPressShort => "Open the books and look at the cash options.",
		FirstWeekStep.Date when desk.FirstWeekDateTomorrow => "Turn in at the calendar and set the ship date tomorrow.",
		FirstWeekStep.Release => $"The single ships {desk.FirstWeekSingle?.Date.ToHeadlineString()}. Use the calendar to move ahead.",
		FirstWeekStep.Assemble or FirstWeekStep.Press or FirstWeekStep.Date => PlayerDesk.FirstWeekTitle(desk.FirstWeekCurrent.Value) + " in the catalog.",
		_ => PlayerDeskPanel.NextUpHint(desk)
	};

	private Control OfficeFirstWeekTarget(PlayerDesk desk) {
		if (DeskCovered() || PaperModal.IsOpen) return null;
		return desk.FirstWeekCurrent switch {
			FirstWeekStep.Hear or FirstWeekStep.Ask => officeDoor,
			FirstWeekStep.Answer when desk.FirstWeekVisitLater => officeWait,
			FirstWeekStep.Answer when desk.FirstWeekVisitToday || desk.FirstWeekVisit is { Minute: null } => officeBook,
			FirstWeekStep.Answer when desk.FirstWeekNeedsAnotherAct || desk.FirstWeekHearAgainId != null => officeDoor,
			FirstWeekStep.Answer => calendarButton,
			FirstWeekStep.Record or FirstWeekStep.Cut when desk.FirstWeekStudioTomorrow => calendarButton,
			FirstWeekStep.Cut when desk.FirstWeekStudioOvernight => calendarButton,
			FirstWeekStep.Press or FirstWeekStep.Date when !desk.AtHome => officeDoor,
			FirstWeekStep.Press when desk.FirstWeekPressShort => deskProps.FirstOrDefault(p => p.Name.ToString().StartsWith("PropBOOKS", StringComparison.Ordinal)),
			FirstWeekStep.Date when desk.FirstWeekDateTomorrow => calendarButton,
			FirstWeekStep.Release => calendarButton,
			FirstWeekStep.Assemble or FirstWeekStep.Press or FirstWeekStep.Date => deskProps.FirstOrDefault(p => p.Name.ToString().StartsWith("PropCATALOG", StringComparison.Ordinal)),
			_ => officeDoor
		};
	}

	internal List<Control> OfficeFirstWeekTargets(PlayerDesk desk) {
		return OfficeFirstWeekTarget(desk) is { } target ? new List<Control> { target } : null;
	}

	private void OpenOfficeDoor() {
		var desk = PlayerDesk.Instance;
		if (desk.Calling == PlayerDesk.CallerAt.Door) { PickUpFromTheDesk(); return; }
		OpenDoorDestination(() => deskPanel.OpenCityMap());
	}

	internal void OpenOfficeStudio() {
		var desk = PlayerDesk.Instance;
		if (desk.Session is { } session) { OpenStudio(session.ArtistId); return; }
		if (desk.ActiveStudioTrip is { } trip && trip.CityId == desk.CurrentCityId) { OpenStudio(trip.ArtistId); return; }
		var acts = desk.Roster.ToList();
		if (acts.Count == 1) { OpenStudio(acts[0].artistId); return; }
		var modal = PaperModal.Open(deskPanel, "WHO'S GOING INTO THE STUDIO?");
		foreach (var act in acts) {
			var choice = new Button { Text = act.stageName, Alignment = HorizontalAlignment.Left };
			choice.Pressed += () => { modal.Close(); OpenStudio(act.artistId); };
			modal.Body.AddChild(choice);
			choice.SetMeta("artist_id", act.artistId);
		}
		modal.AddButton("STAY HERE", null);
	}

	private void OpenDoorDestination(Action open) {
		open();
		isUIOpen = true;
		UpdateMainHud();
	}

	internal void FromFilesToDoor() {
		OpenDoorDestination(() => deskPanel.OpenCityMap());
	}

	private void OpenOfficePhone() {
		if (PlayerDesk.Instance?.Calling == PlayerDesk.CallerAt.Phone) PickUpFromTheDesk();
		else deskPanel?.OpenRolodex();
	}

	internal bool IsOfficePanel(PlayerDeskPanel panel) => deskPanel == panel;

	internal void FocusOfficePhone(bool focused) {
		if (officePhoneCamera == null || officePhoneFocused == focused) return;
		officePhoneFocused = focused;
		officePhoneTween?.Kill();
		paintedLayer.Hide();
		var from = officePhoneCamera.GlobalTransform;
		// Keep the props on the right, beside the 2D tray and phone controls.
		var closePosition = new Vector3(.31f, 1.31f, -.79f);
		var cornerShot = new Transform3D(Basis.Identity, closePosition).LookingAt(new Vector3(-.03f, .91f, -2.3f), Vector3.Up);
		var to = focused ? cornerShot : officeWideShot;
		officePhoneTween = CreateTween().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		officePhoneTween.TweenMethod(Callable.From<float>(weight => {
			officePhoneCamera.GlobalTransform = from.InterpolateWith(to, weight);
			if (officeViewport.RenderTargetUpdateMode != SubViewport.UpdateMode.Always)
				officeViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
		}), 0f, 1f, .2);
		officePhoneTween.Finished += () => {
			if (!focused) paintedLayer.Show();
			UpdateMainHud();
		};
	}

	private void BuildOfficeWaitMenu() {
		officeWaitMenu = new PanelContainer {
			Name = "OfficeWaitMenu", Visible = false, ZIndex = 30, Theme = PaperTheme.Build()
		};
		officeWaitMenu.AddThemeStyleboxOverride("panel", PaperStyleBox.Sheet(new Color("f1e5c8"), 10, 8, 8, PaperTheme.Rust));
		var choices = new HBoxContainer();
		officeWaitMenu.AddChild(choices);
		AddChild(officeWaitMenu);
	}

	private void OpenOfficeClock() {
		if (officeWaitMenu.Visible) { officeWaitMenu.Hide(); return; }
		var desk = PlayerDesk.Instance;
		var choices = officeWaitMenu.GetChild<HBoxContainer>(0);
		foreach (Node child in choices.GetChildren()) { choices.RemoveChild(child); child.QueueFree(); }
		void AddWait(string caption, int hours) {
			var button = new Button { Text = caption, Disabled = desk.WaitMinutes(hours) == 0 };
			if (!button.Disabled) PlayerDeskPanel.SetWaitPreview(button, desk, hours);
			else button.TooltipText = hours == 0 ? "Evening hours have already begun." : "Turn in for the night.";
			button.Pressed += () => {
				officeWaitMenu.Hide();
				if (!desk.PassTime(hours, out var message, stopForCallers: true)) ShowOfficeTimeMessage(message);
			};
			choices.AddChild(button);
		}
		AddWait("WAIT 1h", 1);
		AddWait("WAIT 3h", 3);
		AddWait("WAIT FOR EVENING", desk.HoursUntil(17));
		officeWaitMenu.ResetSize();
		officeWaitMenu.Position = officeWait.Position + new Vector2((officeWait.Size.X - officeWaitMenu.Size.X) / 2, 28);
		officeWaitMenu.Show();
		roomLabels[officeWait].GetParent<PanelContainer>().Hide();
		choices.GetChildren().OfType<Button>().FirstOrDefault(button => !button.Disabled)?.GrabFocus();
	}

	internal Control FirstWeekCalendarTarget(PlayerDesk desk) {
		if (desk.FirstWeekCurrent != FirstWeekStep.Release && !desk.FirstWeekDateTomorrow) return null;
		if (morningPaper?.Visible == true) return morningPaper.FindChild("FoldThePaper", true, false) as Control;
		if (calendarPopup?.Visible == true && calendarPopup.GetNode<Button>("Margin/Options/NextEvent") is { Disabled: false } next)
			return next;
		return null;
	}

	internal void ShowOfficeTimeMessage(string message) => ShowAnnouncement(message);

	private bool HandleOfficeShortcut(InputEvent input) {
		if (input is not InputEventKey { Pressed: true, Echo: false, AltPressed: false, CtrlPressed: false, MetaPressed: false } key
			|| PaperModal.IsOpen || !CanUseOfficeNavigation() || (DeskCovered() && deskPanel?.FilesOpen != true)
			|| gameMenu?.Visible == true || morningPaper?.Visible == true
			|| GetViewport().GuiGetFocusOwner() is LineEdit or TextEdit) return false;
		string tab = key.Keycode switch {
			Key.Key1 => "ROSTER", Key.Key2 => "CATALOG", Key.Key3 => "DISTRIBUTION", Key.Key4 => "BOOKS", _ => null
		};
		if (tab != null) OpenDoorDestination(() => deskPanel.OpenAtTab(tab));
		else if (key.Keycode is Key.D or Key.P) {
			if (deskPanel.FilesOpen) deskPanel.ClosePanel();
			if (key.Keycode == Key.D) OpenOfficeDoor();
			else OpenOfficePhone();
		} else return false;
		officeWaitMenu?.Hide();
		GetViewport().SetInputAsHandled();
		return true;
	}
}
