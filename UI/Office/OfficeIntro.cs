using System;
using System.Collections.Generic;
using Godot;

/// <summary>The morning of 1 January 1960 (inner-life §3.1): a hungover walk into the office, the lawyer's letter on the
/// desk, the drawer, the voice that talks him out of it, and then the desk camera, where "What do you remember?" opens
/// the founding pages. Nothing here touches the sim: the holder isn't picked until the founding answers are in.</summary>
public partial class OfficeIntro : Control {
	// The walk: door, threshold, past the credenza and round the desk to stand over the letter.
	private const float WalkDuration = 10.45f, LetterAt = 9.95f;
	// Seated timeline (s from the moment the letter goes down).
	private const float SitEnd = 1.3f, LookDrawer = 2.2f, DrawerOpens = 3.1f, LiftFrom = 4.2f, LiftTo = 5.6f,
		CockAt = 5.9f, RaiseFrom = 6.3f, RaiseTo = 8.3f, VoiceAt = 7.75f;
	// Return timeline (s from the choice).
	private const float LowerTo = 1.0f, DropFrom = 1.3f, DropTo = 2.5f, CloseFrom = 2.6f, CloseTo = 3.3f, ReturnEnd = 4.1f;
	private const float DrawerTravel = .32f;

	private enum Beat { Walk, Letter, Seat, Voice, Return }

	private readonly record struct Shot(float Time, Vector3 Position, Vector3 Target, float Roll, float Fov) {
		public Quaternion Facing => Look(Position, Target, Roll);
	}

	private static readonly Vector3 LetterSpot = new(-.05f, .849f, -.52f);
	private static readonly Vector3 LetterStand = new(-.42f, 1.52f, .02f);
	// The right-hand pedestal's top drawer, front face centre (build_office.py: x 0.88, front y .28, z .6515).
	private static readonly Vector3 DrawerFront = new(.88f, .6515f, -.268f);
	private static readonly Transform3D GunRest = new(Basis.FromEuler(new Vector3(0, Mathf.DegToRad(-62f), Mathf.DegToRad(90f))), new Vector3(.02f, -.044f, -.12f));
	private static readonly Transform3D GunHeld = new(Basis.FromEuler(new Vector3(Mathf.DegToRad(6f), Mathf.DegToRad(14f), Mathf.DegToRad(-6f))), new Vector3(.09f, -.11f, -.40f));
	private static readonly Vector3 GunNearFace = new(.01f, -.05f, -.28f), Mouth = new(0, -.075f, 0);

	private ColorRect fade, haze;
	private ShaderMaterial hazeMaterial;
	private Control letter, voicePanel;
	private VBoxContainer voiceLog, voiceOptions;
	private Button skip;
	private AudioStreamPlayer foley, sfx;
	private Camera3D deskCamera, camera;
	private Node3D door, set, drawer, gun, hammer, hand;
	private MeshInstance3D slot;
	private MeshInstance3D letterSheet, sleeve;
	private OmniLight3D hallLight, ceilingLight;
	private Vector3 closedDoorRotation, seat, deskForward;
	private Viewport.Msaa deskMsaa;
	private Shot[] shots;
	private Action onSeated;
	private Beat beat;
	private float elapsed, beatTime, clock, deskFov;
	private float hazeNow = 1, vignetteNow = 1.1f, desatNow, hazeGoal = 1, vignetteGoal = 1.1f, desatGoal;
	private readonly HashSet<string> cues = new();
	private RichTextLabel typing;
	private float typed;
	private Action afterTyping;
	private bool bulletRefused;
	private readonly Dictionary<string, AudioStream> sounds = new();

	public SubViewport Room { get; init; }
	public bool IsPlaying { get; private set; }

	public override void _Ready() {
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		set = Room?.GetNodeOrNull<Node3D>("Office/OfficeIntroSet");
		set?.Hide();

		hazeMaterial = new ShaderMaterial { Shader = new Shader { Code = HazeShader } };
		haze = new ColorRect { Name = "Hangover", Material = hazeMaterial, MouseFilter = MouseFilterEnum.Ignore };
		AddChild(haze);
		haze.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		fade = new ColorRect { Name = "Fade", Color = Colors.Black, MouseFilter = MouseFilterEnum.Ignore };
		AddChild(fade);
		fade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

		letter = BuildLetter();
		AddChild(letter);
		voicePanel = BuildVoicePanel();
		AddChild(voicePanel);

		skip = new Button { Name = "SkipIntro", Text = "SKIP INTRO  ·  ESC", MouseDefaultCursorShape = CursorShape.PointingHand };
		skip.AddThemeFontOverride("font", PaperTheme.SansBold);
		skip.AddThemeFontSizeOverride("font_size", 16);
		AddChild(skip);
		skip.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomRight);
		skip.OffsetLeft = -216;
		skip.OffsetRight = -28;
		skip.OffsetTop = -67;
		skip.OffsetBottom = -25;
		skip.Pressed += Skip;
		foley = new AudioStreamPlayer { Stream = GD.Load<AudioStream>("res://UI/Office/office_entry.wav"), VolumeDb = -5f };
		AddChild(foley);
		sfx = new AudioStreamPlayer { VolumeDb = -4f, MaxPolyphony = 4 };
		AddChild(sfx);
		BuildSounds();
		SetProcess(false);
		SetProcessInput(false);
		Hide();
	}

	public bool Play(Action finished) {
		if (Visible || Room == null) return false;
		deskCamera = Room.GetCamera3D();
		door = Room.GetNodeOrNull<Node3D>("Office/OfficeDoorHinge");
		Node3D seatEye = Room.GetNodeOrNull<Node3D>("Office/OfficeSeatEye");
		hallLight = Room.GetNodeOrNull<OmniLight3D>("IntroHallLight");
		ceilingLight = Room.GetNodeOrNull<OmniLight3D>("IntroCeilingLight");
		if (deskCamera == null || door == null || seatEye == null || set == null) return false;

		closedDoorRotation = door.RotationDegrees;
		if (set is MeshInstance3D mesh) mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		set.Show();
		hallLight?.Show();
		ceilingLight?.Show();
		camera = new Camera3D { Name = "IntroCamera", Near = .025f, Far = deskCamera.Far };
		Room.AddChild(camera);
		seat = seatEye.GlobalPosition;
		deskForward = -deskCamera.GlobalTransform.Basis.Z;
		deskFov = deskCamera.Fov;
		shots = EntranceShots();
		BuildProps();
		onSeated = finished;
		elapsed = beatTime = clock = 0;
		beat = Beat.Walk;
		cues.Clear();
		bulletRefused = false;
		hazeNow = hazeGoal = 1; vignetteNow = vignetteGoal = 1.1f; desatNow = desatGoal = 0;
		IsPlaying = true;
		skip.Disabled = false;
		skip.Show();
		fade.Color = Colors.Black;
		haze.Show();
		letter.Hide();
		voicePanel.Hide();
		Show();
		SetProcess(true);
		SetProcessInput(true);
		PlaceWalk(0);
		camera.MakeCurrent();
		// The desk is rendered once with MSAA; the walk renders every frame, where FXAA costs a third less on
		// integrated GPUs under the Compatibility renderer (24 -> 34 fps measured on an AMD APU).
		deskMsaa = Room.Msaa3D;
		Room.Msaa3D = Viewport.Msaa.Disabled;
		Room.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Fxaa;
		Room.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
		foley.Play();
		return true;
	}

	// ── input ───────────────────────────────────────────────────────────────────────────────────────

	public override void _Input(InputEvent input) {
		if (input.IsEcho() || !input.IsPressed()) return;
		if (input.IsActionPressed("ui_cancel")) {
			GetViewport().SetInputAsHandled();
			if (IsPlaying && !skip.Disabled) Skip();
			return;
		}
		if (beat == Beat.Letter && letter.Visible && input.IsActionPressed("ui_accept")) {
			GetViewport().SetInputAsHandled();
			SetLetterDown();
		} else if (beat == Beat.Voice) {
			if (typing != null && (input.IsActionPressed("ui_accept") || input is InputEventMouseButton)) {
				FinishTyping();
				GetViewport().SetInputAsHandled();
			} else if (input is InputEventKey key && key.Keycode is >= Key.Key1 and <= Key.Key9) {
				int index = (int)(key.Keycode - Key.Key1);
				if (index < voiceOptions.GetChildCount() && voiceOptions.GetChild(index) is Button { Disabled: false } option) {
					GetViewport().SetInputAsHandled();
					option.EmitSignal(BaseButton.SignalName.Pressed);
				}
			}
		}
	}

	// ── the clock ───────────────────────────────────────────────────────────────────────────────────

	public override void _Process(double delta) {
		float dt = (float)delta;
		clock += dt;
		beatTime += dt;
		switch (beat) {
			case Beat.Walk:
				elapsed = Mathf.Min(elapsed + dt, WalkDuration);
				PlaceWalk(elapsed);
				// Eyes open slowly, and shut once on the way across the room.
				float open = 1f - Mathf.SmoothStep(.04f, .9f, elapsed);
				float blink = Bump(6.30f, 6.48f, 6.80f, elapsed) * .88f;
				fade.Color = new Color(0, 0, 0, Mathf.Max(open, blink));
				skip.Modulate = new Color(1, 1, 1, Mathf.SmoothStep(.25f, .85f, elapsed));
				hazeGoal = 1f - .3f * Mathf.SmoothStep(3f, 9f, elapsed);
				vignetteGoal = .62f + .6f * (1f - Mathf.SmoothStep(.4f, 3.2f, elapsed));
				if (elapsed >= LetterAt) PickUpLetter();
				break;
			case Beat.Letter:
				PlaceWalk(WalkDuration, .7f);
				break;
			case Beat.Seat:
			case Beat.Voice:
				SeatedBeat(beat == Beat.Voice ? Mathf.Min(beatTime, RaiseTo) : beatTime);
				if (beat == Beat.Seat && beatTime >= VoiceAt) OpenVoice();
				if (beat == Beat.Voice) {
					// Colour comes back while he talks: paradise awaits.
					desatGoal = .12f;
					vignetteGoal = .8f;
					hazeGoal = .45f;
					Type(dt);
				}
				break;
			case Beat.Return:
				ReturnBeat(beatTime);
				break;
		}
		float k = 1f - Mathf.Exp(-dt * 2.5f);
		hazeNow = Mathf.Lerp(hazeNow, hazeGoal, k);
		vignetteNow = Mathf.Lerp(vignetteNow, vignetteGoal, k);
		desatNow = Mathf.Lerp(desatNow, desatGoal, k);
		hazeMaterial.SetShaderParameter("haze", hazeNow);
		hazeMaterial.SetShaderParameter("vignette", vignetteNow);
		hazeMaterial.SetShaderParameter("desat", desatNow);
		hazeMaterial.SetShaderParameter("t", clock);
	}

	// ── the walk ────────────────────────────────────────────────────────────────────────────────────

	private void PlaceWalk(float time, float sway = 1f) {
		int i = 0;
		while (i < shots.Length - 2 && time >= shots[i + 1].Time) i++;
		Shot a = shots[i], b = shots[i + 1];
		Shot before = shots[Math.Max(0, i - 1)], after = shots[Math.Min(i + 2, shots.Length - 1)];
		float span = b.Time - a.Time;
		float t = Mathf.Clamp((time - a.Time) / span, 0, 1);
		float ease = Mathf.SmoothStep(0, 1, t);
		float stride = (Mathf.SmoothStep(.85f, 1.3f, time) * (1 - Mathf.SmoothStep(2.75f, 3.10f, time)))
			+ (Mathf.SmoothStep(4.15f, 4.6f, time) * (1 - Mathf.SmoothStep(9.38f, 9.98f, time)));
		float gait = Mathf.Sin((time - .75f) * Mathf.Tau / .64f);
		Vector3 position = a.Position.CubicInterpolateInTime(b.Position, before.Position, after.Position,
			t, span, before.Time - a.Time, after.Time - a.Time);
		// A heavy, uneven step: the low beat lands harder on one foot than the other.
		position += Vector3.Up * (.034f * stride * gait) + Vector3.Down * (.012f * stride * Mathf.Max(0, Mathf.Sin((time - .75f) * Mathf.Tau / 1.28f)));
		// He catches himself once, lurching into the room off the bookcase side.
		float lurch = Bump(5.05f, 5.35f, 5.95f, time);
		position += new Vector3(.07f, -.06f, .02f) * lurch;
		PlaceCamera(position, a.Facing.Slerp(b.Facing, ease), Mathf.Lerp(a.Fov, b.Fov, ease), sway, 6f * lurch, -5f * lurch);
		// Fully open before the first step across; shut and latched before the walk passes in front of it.
		float swing = Mathf.SmoothStep(.25f, 1.30f, time) * (1 - Mathf.SmoothStep(3.05f, 4.15f, time));
		door.RotationDegrees = closedDoorRotation + Vector3.Up * (-91f * swing);
	}

	/// <summary>Every pose goes through here: a slow, seasick drift on top, in the camera's own frame.</summary>
	private void PlaceCamera(Vector3 position, Quaternion facing, float fov, float sway, float extraRoll = 0, float extraPitch = 0) {
		float t = clock;
		var basis = new Basis(facing);
		Vector3 drift = new(Mathf.Sin(t * 1.13f) * .020f + Mathf.Sin(t * .47f + 1.3f) * .013f, Mathf.Sin(t * .83f + .4f) * .008f, 0);
		float roll = (Mathf.Sin(t * .91f + .7f) * 2.0f + Mathf.Sin(t * .37f) * 1.3f) * sway + extraRoll;
		float yaw = Mathf.Sin(t * .61f + 2f) * 1.1f * sway;
		float pitch = Mathf.Sin(t * .73f) * .8f * sway + extraPitch;
		basis *= Basis.FromEuler(new Vector3(Mathf.DegToRad(pitch), Mathf.DegToRad(yaw), Mathf.DegToRad(roll)));
		camera.GlobalTransform = new Transform3D(basis, position + basis * drift * sway);
		camera.Fov = fov;
	}

	// ── the letter ──────────────────────────────────────────────────────────────────────────────────

	private void PickUpLetter() {
		beat = Beat.Letter;
		beatTime = 0;
		letterSheet.Hide();
		Play("paper");
		skip.Modulate = Colors.White;
		letter.Show();
		letter.Modulate = new Color(1, 1, 1, 0);
		letter.Position = new Vector2(0, 120);
		var rise = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
		rise.TweenProperty(letter, "position:y", 0f, .65f);
		rise.TweenProperty(letter, "modulate:a", 1f, .45f);
		hazeGoal = .55f;
		vignetteGoal = .75f;
	}

	private void SetLetterDown() {
		if (beat != Beat.Letter) return;
		beat = Beat.Seat;
		beatTime = 0;
		letterSheet.Show();
		Play("paper");
		var drop = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.In);
		drop.TweenProperty(letter, "position:y", 160f, .45f);
		drop.TweenProperty(letter, "modulate:a", 0f, .40f);
		drop.Chain().TweenCallback(Callable.From(letter.Hide));
		hazeGoal = .6f;
		vignetteGoal = .7f;
	}

	// ── the chair, the drawer ───────────────────────────────────────────────────────────────────────

	private Quaternion SeatFacing(Vector3 target) => Look(seat, target, 0);
	private Vector3 DrawerLook => DrawerFront + new Vector3(0, -.03f, DrawerTravel * .55f);
	private Vector3 HeldLook => seat + deskForward.Slide(Vector3.Up).Normalized() + Vector3.Down * .42f;

	private void SeatedBeat(float t) {
		float sit = Mathf.SmoothStep(0, SitEnd, t);
		Vector3 position = LetterStand.Lerp(seat, sit) + Vector3.Down * (.05f * Bump(.85f, 1.05f, 1.45f, t));
		Quaternion facing = Look(LetterStand, LetterSpot, 0).Slerp(SeatFacing(seat + deskForward), Mathf.SmoothStep(.1f, 1.4f, t));
		facing = facing.Slerp(SeatFacing(DrawerLook), Mathf.SmoothStep(LookDrawer, LookDrawer + 1.1f, t));
		facing = facing.Slerp(SeatFacing(HeldLook), Mathf.SmoothStep(LiftFrom + .2f, LiftTo, t));
		// The room narrows as the gun comes up.
		float raise = Mathf.SmoothStep(RaiseFrom, RaiseTo, t);
		PlaceCamera(position, facing, Mathf.Lerp(Mathf.Lerp(52f, deskFov, sit), deskFov - 7f, raise), Mathf.Lerp(.7f, .3f, sit));

		Cue("sit", .75f, t, "sit");
		Cue("drawer", DrawerOpens, t, "drawer-open");
		SetDrawer(Mathf.SmoothStep(DrawerOpens, DrawerOpens + .85f, t));

		hand.Visible = sleeve.Visible = t >= LiftFrom;
		Transform3D rest = drawer.GlobalTransform * GunRest;
		if (t < LiftFrom) gun.GlobalTransform = rest;
		else if (t < LiftTo) gun.GlobalTransform = Arc(rest, Held(), Mathf.SmoothStep(LiftFrom, LiftTo, t), .07f);
		else gun.GlobalTransform = Held().InterpolateWith(NearFace(beat == Beat.Voice ? 1f : 0f), raise);
		Cue("cock", CockAt, t, "cock");
		hammer.Rotation = new Vector3(Mathf.DegToRad(-42f) * Mathf.SmoothStep(CockAt, CockAt + .14f, t), 0, 0);
		UpdateSleeve();

		if (beat == Beat.Seat && t > RaiseFrom) {
			desatGoal = .6f * raise;
			vignetteGoal = .7f + .6f * raise;
			hazeGoal = .6f - .3f * raise;
		}
	}

	private void ReturnBeat(float t) {
		Quaternion facing = SeatFacing(HeldLook).Slerp(SeatFacing(DrawerLook), Mathf.SmoothStep(DropFrom, DropTo - .1f, t));
		facing = facing.Slerp(SeatFacing(seat + deskForward), Mathf.SmoothStep(CloseFrom + .1f, CloseTo + .4f, t));
		PlaceCamera(seat, facing, Mathf.Lerp(deskFov - 7f, deskFov, Mathf.SmoothStep(0, LowerTo, t)), .3f * (1f - Mathf.SmoothStep(CloseTo, ReturnEnd - .2f, t)));

		Transform3D rest = drawer.GlobalTransform * GunRest;
		if (t < LowerTo) gun.GlobalTransform = NearFace(0).InterpolateWith(Held(), Mathf.SmoothStep(0, LowerTo, t));
		else if (t < DropFrom) gun.GlobalTransform = Held();
		else gun.GlobalTransform = Arc(Held(), rest, Mathf.SmoothStep(DropFrom, DropTo, t), .06f);
		Cue("uncock", LowerTo, t, "uncock");
		hammer.Rotation = new Vector3(Mathf.DegToRad(-42f) * (1f - Mathf.SmoothStep(LowerTo, LowerTo + .12f, t)), 0, 0);
		hand.Visible = sleeve.Visible = t < DropTo;
		UpdateSleeve();

		Cue("close", CloseFrom, t, "drawer-close");
		SetDrawer(1f - Mathf.SmoothStep(CloseFrom, CloseTo, t));
		desatGoal = 0;
		vignetteGoal = .5f;
		hazeGoal = .35f;
		if (t >= ReturnEnd) SeatPlayer();
	}

	private Transform3D Held() => camera.GlobalTransform * GunHeld;

	/// <summary>Muzzle toward the mouth, stopped short. While the voice talks the hand shakes.</summary>
	private Transform3D NearFace(float shake) {
		Vector3 at = GunNearFace + new Vector3(Mathf.Sin(clock * 23f) * .0012f, Mathf.Sin(clock * 17f + 1f) * .0010f, Mathf.Sin(clock * 11f) * .0015f) * shake;
		var local = new Transform3D(Basis.LookingAt(Mouth - at, Vector3.Up), at);
		return camera.GlobalTransform * local;
	}

	private static Transform3D Arc(Transform3D from, Transform3D to, float w, float lift) {
		Transform3D x = from.InterpolateWith(to, w);
		x.Origin += Vector3.Up * (lift * Mathf.Sin(Mathf.Pi * w));
		return x;
	}

	private void SetDrawer(float open) {
		drawer.GlobalPosition = DrawerFront + new Vector3(0, 0, .003f + DrawerTravel * open);
		drawer.Visible = open > .001f || beat is Beat.Seat or Beat.Voice;
		// The office's own drawer front is baked into the desk mesh; once ours is clear of it, a dark slot hides it.
		slot.Visible = open > .15f;
	}

	private void UpdateSleeve() {
		if (!sleeve.Visible) return;
		Vector3 wrist = gun.GlobalTransform * new Vector3(0, -.075f, .075f);
		Vector3 elbow = camera.GlobalTransform * new Vector3(.34f, -.52f, -.05f);
		sleeve.GlobalTransform = Segment(wrist, elbow, .043f);
	}

	private static Transform3D Segment(Vector3 a, Vector3 b, float radius) {
		Vector3 y = b - a;
		Vector3 side = Mathf.Abs(y.Normalized().Dot(Vector3.Up)) > .9f ? Vector3.Right : Vector3.Up;
		Vector3 x = y.Cross(side).Normalized() * radius;
		Vector3 z = x.Cross(y).Normalized() * radius;
		return new Transform3D(new Basis(x, y, z), (a + b) / 2);
	}

	// ── the voice ───────────────────────────────────────────────────────────────────────────────────

	private const string VoiceName = "A VOICE, SOMEWHERE BEHIND THE HANGOVER";
	private static readonly Color VoiceInk = new("e0a94a"), NarrationInk = new("cbbfa6"), YouInk = new("8fb3c9");

	private void OpenVoice() {
		beat = Beat.Voice;
		foreach (Node child in voiceLog.GetChildren()) child.QueueFree();
		ClearOptions();
		voicePanel.Show();
		voicePanel.Modulate = new Color(1, 1, 1, 0);
		CreateTween().TweenProperty(voicePanel, "modulate:a", 1f, .25f);
		Say(VoiceName, VoiceInk,
			"Whoa, whoa, hold on there, buddy! We're not gonna let you do that. Listen, this is the deal: paradise awaits. " +
			"We have a whole new decade before us—unlimited potential. Are you gonna let one bad deal take that from you? " +
			"Now, calm down, and let's make a smart choice. You can either put that bullet in our head, or you can offer " +
			"yourself up to the ever-hungry maw of infinite, living possibility. What's it gonna be, [i]Label Man[/i]?",
			ShowChoice);
	}

	private void ShowChoice() {
		ClearOptions();
		AddOption("Put that bullet in our head.", bulletRefused, () => {
			bulletRefused = true;
			ClearOptions();
			Say(null, NarrationInk, "Your hand won't do it. Your hand, it turns out, has plans for the decade.", ShowChoice);
		});
		AddOption("Offer yourself up to the ever-hungry maw of infinite, living possibility.", false, () => {
			ClearOptions();
			Say("YOU", YouInk, "\"Okay. Okay. The maw.\"", () =>
				Say(VoiceName, VoiceInk, "Atta boy. Now put that thing back in the drawer, nice and slow. We've got a label to start.", () =>
					AddOption("[Put it back in the drawer.]", false, PutItBack)));
		});
	}

	private void PutItBack() {
		ClearOptions();
		var close = CreateTween();
		close.TweenProperty(voicePanel, "modulate:a", 0f, .35f);
		close.TweenCallback(Callable.From(voicePanel.Hide));
		beat = Beat.Return;
		beatTime = 0;
	}

	private void Say(string speaker, Color ink, string text, Action then) {
		var line = new RichTextLabel {
			BbcodeEnabled = true, FitContent = true, ScrollActive = false, SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore, AutowrapMode = TextServer.AutowrapMode.WordSmart,
		};
		line.AddThemeFontOverride("normal_font", PaperTheme.Serif);
		line.AddThemeFontOverride("italics_font", PaperTheme.SerifItalic);
		line.AddThemeFontOverride("bold_font", PaperTheme.SansBold);
		line.AddThemeFontSizeOverride("normal_font_size", 19);
		line.AddThemeFontSizeOverride("italics_font_size", 19);
		line.AddThemeFontSizeOverride("bold_font_size", 15);
		line.AddThemeColorOverride("default_color", new Color("ece3cf"));
		string head = speaker == null ? "" : $"[color=#{ink.ToHtml(false)}][b]{speaker}[/b][/color] — ";
		line.Text = speaker == null ? $"[color=#{ink.ToHtml(false)}][i]{text}[/i][/color]" : head + text;
		line.VisibleCharacters = speaker == null ? 0 : speaker.Length + 3;
		voiceLog.AddChild(line);
		typing = line;
		typed = line.VisibleCharacters;
		afterTyping = then;
	}

	private void Type(float dt) {
		if (typing == null) return;
		typed += dt * 62f;
		typing.VisibleCharacters = (int)typed;
		if (typed >= typing.GetTotalCharacterCount()) FinishTyping();
	}

	private void FinishTyping() {
		if (typing == null) return;
		typing.VisibleCharacters = -1;
		typing = null;
		Action then = afterTyping;
		afterTyping = null;
		then?.Invoke();
	}

	private void ClearOptions() {
		foreach (Node child in voiceOptions.GetChildren()) {
			voiceOptions.RemoveChild(child);
			child.QueueFree();
		}
	}

	private void AddOption(string text, bool spent, Action pick) {
		int number = voiceOptions.GetChildCount() + 1;
		var option = new Button {
			Text = $"{number}.  {text}", Disabled = spent, Alignment = HorizontalAlignment.Left,
			AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseDefaultCursorShape = CursorShape.PointingHand, FocusMode = FocusModeEnum.None,
		};
		option.AddThemeFontOverride("font", PaperTheme.SansSemiBold);
		option.AddThemeFontSizeOverride("font_size", 17);
		option.AddThemeColorOverride("font_color", new Color("d8643a"));
		option.AddThemeColorOverride("font_hover_color", new Color("ffd7a1"));
		option.AddThemeColorOverride("font_pressed_color", new Color("ffd7a1"));
		option.AddThemeColorOverride("font_disabled_color", new Color(1, 1, 1, .28f));
		var flat = new StyleBoxFlat { BgColor = Colors.Transparent, ContentMarginTop = 6, ContentMarginBottom = 6, ContentMarginLeft = 4 };
		var hover = new StyleBoxFlat { BgColor = new Color(1, 1, 1, .06f), ContentMarginTop = 6, ContentMarginBottom = 6, ContentMarginLeft = 4 };
		foreach (string state in new[] { "normal", "pressed", "disabled", "focus" }) option.AddThemeStyleboxOverride(state, flat);
		option.AddThemeStyleboxOverride("hover", hover);
		option.Pressed += () => { if (!option.Disabled) pick(); };
		voiceOptions.AddChild(option);
	}

	// ── skip and hand-off ───────────────────────────────────────────────────────────────────────────

	private void Skip() {
		if (!IsPlaying || skip.Disabled) return;
		SetProcess(false);
		skip.Disabled = true;
		foley.Stop();
		sfx.Stop();
		var cut = CreateTween();
		cut.TweenProperty(fade, "color:a", 1f, .21f);
		cut.TweenCallback(Callable.From(SeatPlayer));
	}

	private void SeatPlayer() {
		if (!IsPlaying) return;
		SetProcess(false);
		skip.Hide();
		foley.Stop();
		letter.Hide();
		voicePanel.Hide();
		haze.Hide();
		typing = null;
		afterTyping = null;
		deskCamera.MakeCurrent();
		camera.QueueFree();
		foreach (Node prop in new Node[] { drawer, gun, letterSheet, sleeve, slot }) prop?.QueueFree();
		drawer = gun = null;
		letterSheet = sleeve = slot = null;
		door.RotationDegrees = closedDoorRotation;
		set.Hide();
		hallLight?.Hide();
		ceilingLight?.Hide();
		Room.Msaa3D = deskMsaa;
		Room.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
		Room.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
		IsPlaying = false;
		onSeated?.Invoke();
		onSeated = null;

		if (fade.Color.A < .001f) {
			EndTransition();
			return;
		}
		var reveal = CreateTween();
		reveal.TweenProperty(fade, "color:a", 0f, .43f).SetTrans(Tween.TransitionType.Sine);
		reveal.TweenCallback(Callable.From(EndTransition));
	}

	private void EndTransition() {
		Hide();
		SetProcessInput(false);
	}

	// ── shots ───────────────────────────────────────────────────────────────────────────────────────

	private static Quaternion Look(Vector3 from, Vector3 to, float roll) =>
		(Basis.LookingAt(to - from) * new Basis(Vector3.Back, Mathf.DegToRad(roll))).GetRotationQuaternion();

	private static float Bump(float rise, float peak, float fall, float t) =>
		Mathf.SmoothStep(rise, peak, t) * (1f - Mathf.SmoothStep(peak, fall, t));

	private Shot[] EntranceShots() => new Shot[] {
		// Wait in the hall while the door swings in, step through into the pocket right of its arc (clear of the
		// credenza and the guest chair) and glance at the credenza while it swings shut behind, then walk left past
		// it once it has latched. The door's edge never comes within a quarter metre of the eye, nor into view.
		// The last leg comes round the left of the chair and stops over the letter instead of sitting down.
		new(0.00f, new(-1.70f, 1.57f, -3.58f), new(-1.60f, 1.28f, -2.74f), -4.2f, 51f),
		new(.75f, new(-1.70f, 1.56f, -3.50f), new(-1.62f, 1.28f, -2.72f), -1.1f, 51f),
		new(1.60f, new(-1.66f, 1.60f, -3.02f), new(-1.40f, 1.40f, -1.10f), 3.4f, 54f),
		new(2.35f, new(-1.30f, 1.57f, -2.50f), new(-1.05f, 1.60f, -.40f), -2.4f, 54f),
		new(3.05f, new(-1.00f, 1.55f, -2.36f), new(.45f, 1.13f, -2.30f), 2.5f, 54f),
		new(3.85f, new(-1.02f, 1.56f, -2.35f), new(-.30f, 1.23f, -1.00f), -2.0f, 54f),
		new(4.75f, new(-1.95f, 1.58f, -2.25f), new(-2.94f, 1.46f, -1.05f), -3.1f, 52f),
		new(5.42f, new(-2.48f, 1.53f, -1.91f), new(-2.19f, 1.04f, -.72f), -3.8f, 52f),
		new(6.16f, new(-2.52f, 1.57f, -1.31f), new(-1.70f, 1.20f, -.22f), 3.4f, 52f),
		new(6.88f, new(-2.50f, 1.56f, -.59f), seat + new Vector3(0, -.44f, .11f), -2.4f, 53f),
		new(7.61f, new(-2.28f, 1.53f, .19f), seat + new Vector3(0, -.60f, .07f), 2.5f, 52f),
		new(8.39f, new(-1.56f, 1.51f, .29f), seat + new Vector3(.06f, -.49f, .13f), -2.9f, 52f),
		new(9.20f, new(-.90f, 1.53f, .24f), LetterSpot + new Vector3(0, .05f, 0), 1.4f, 52f),
		new(LetterAt, LetterStand, LetterSpot, -1.2f, 52f),
		new(WalkDuration, LetterStand, LetterSpot, 0f, 52f),
	};

	// ── props ───────────────────────────────────────────────────────────────────────────────────────

	private void BuildProps() {
		var walnut = Mat("5a3520", .42f);
		var inside = Mat("2e1d10", .85f);
		var brass = Mat("b08d57", .35f, .8f);
		var blued = Mat("202228", .32f, .85f);
		var grip = Mat("3b2414", .5f);
		var skin = Mat("d6b39b", .65f);
		var shirt = Mat("e6e0d2", .9f);

		drawer = new Node3D { Name = "IntroDrawer" };
		Room.AddChild(drawer);
		Part(drawer, new BoxMesh { Size = new Vector3(.410f, .157f, .024f) }, walnut, new Vector3(0, 0, 0));
		Part(drawer, new BoxMesh { Size = new Vector3(.012f, .125f, .40f) }, inside, new Vector3(-.188f, -.008f, -.21f));
		Part(drawer, new BoxMesh { Size = new Vector3(.012f, .125f, .40f) }, inside, new Vector3(.188f, -.008f, -.21f));
		Part(drawer, new BoxMesh { Size = new Vector3(.376f, .125f, .012f) }, inside, new Vector3(0, -.008f, -.405f));
		Part(drawer, new BoxMesh { Size = new Vector3(.376f, .010f, .40f) }, inside, new Vector3(0, -.065f, -.21f));
		foreach (float dx in new[] { -.055f, .055f })
			Part(drawer, Cyl(.0055f, .024f), brass, new Vector3(dx, 0, .024f), new Vector3(Mathf.Pi / 2, 0, 0));
		Part(drawer, Cyl(.0075f, .15f), brass, new Vector3(0, 0, .036f), new Vector3(0, 0, Mathf.Pi / 2));
		// What else is in a drawer like this: a box of matches and a few loose index cards.
		Part(drawer, new BoxMesh { Size = new Vector3(.05f, .014f, .036f) }, Mat("b8432c", .7f), new Vector3(-.11f, -.053f, -.07f), new Vector3(0, .4f, 0));
		Part(drawer, new BoxMesh { Size = new Vector3(.127f, .003f, .076f) }, Mat("efe7d2", .9f), new Vector3(-.07f, -.058f, -.19f), new Vector3(0, -.2f, 0));
		slot = new MeshInstance3D { Name = "IntroDrawerSlot", Mesh = new BoxMesh { Size = new Vector3(.406f, .153f, .042f) },
			MaterialOverride = Mat("0d0805", .95f), CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
		Room.AddChild(slot);
		slot.GlobalPosition = new Vector3(DrawerFront.X, DrawerFront.Y, -.252f);
		SetDrawer(0);
		drawer.Hide();

		// A .38 revolver, muzzle along -Z.
		gun = new Node3D { Name = "IntroRevolver" };
		Room.AddChild(gun);
		Part(gun, Cyl(.0085f, .10f), blued, new Vector3(0, .014f, -.085f), new Vector3(Mathf.Pi / 2, 0, 0));
		Part(gun, Cyl(.0048f, .004f, 12), new StandardMaterial3D { AlbedoColor = Colors.Black, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded },
			new Vector3(0, .014f, -.1342f), new Vector3(Mathf.Pi / 2, 0, 0));
		Part(gun, new BoxMesh { Size = new Vector3(.010f, .010f, .10f) }, blued, new Vector3(0, .024f, -.085f));
		Part(gun, new BoxMesh { Size = new Vector3(.004f, .008f, .006f) }, blued, new Vector3(0, .032f, -.13f));
		Part(gun, Cyl(.019f, .036f, 6), blued, new Vector3(0, .008f, -.016f), new Vector3(Mathf.Pi / 2, 0, 0));
		Part(gun, new BoxMesh { Size = new Vector3(.016f, .048f, .056f) }, blued, new Vector3(0, .004f, .006f));
		Part(gun, new BoxMesh { Size = new Vector3(.021f, .078f, .032f) }, grip, new Vector3(0, -.045f, .036f), new Vector3(Mathf.DegToRad(-22f), 0, 0));
		Part(gun, new BoxMesh { Size = new Vector3(.004f, .022f, .004f) }, blued, new Vector3(0, -.024f, -.006f), new Vector3(Mathf.DegToRad(18f), 0, 0));
		Part(gun, new BoxMesh { Size = new Vector3(.006f, .004f, .036f) }, blued, new Vector3(0, -.036f, -.004f));
		hammer = new Node3D { Position = new Vector3(0, .022f, .030f) };
		gun.AddChild(hammer);
		Part(hammer, new BoxMesh { Size = new Vector3(.006f, .020f, .008f) }, blued, new Vector3(0, .008f, .002f), new Vector3(Mathf.DegToRad(-25f), 0, 0));
		hand = new Node3D { Name = "Hand" };
		gun.AddChild(hand);
		Part(hand, new SphereMesh { Radius = .5f, Height = 1f }, skin, new Vector3(.004f, -.046f, .044f), Vector3.Zero, new Vector3(.050f, .072f, .062f));
		Part(hand, new CapsuleMesh { Radius = .011f, Height = .06f }, skin, new Vector3(.010f, -.012f, .004f), new Vector3(Mathf.DegToRad(80f), 0, 0));
		hand.Hide();
		sleeve = new MeshInstance3D { Name = "Sleeve", Mesh = new CylinderMesh { TopRadius = 1, BottomRadius = 1, Height = 1, RadialSegments = 14 },
			MaterialOverride = shirt, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
		Room.AddChild(sleeve);
		sleeve.Hide();
		gun.GlobalTransform = drawer.GlobalTransform * GunRest;

		letterSheet = new MeshInstance3D { Name = "LawyersLetter", Mesh = new PlaneMesh { Size = new Vector2(.216f, .279f) },
			MaterialOverride = new StandardMaterial3D { AlbedoTexture = LetterTexture(), Roughness = .95f } };
		Room.AddChild(letterSheet);
		letterSheet.GlobalTransform = new Transform3D(Basis.FromEuler(new Vector3(0, Mathf.DegToRad(9f), 0)), LetterSpot);
	}

	private static StandardMaterial3D Mat(string hex, float roughness, float metallic = 0) =>
		new() { AlbedoColor = new Color(hex), Roughness = roughness, Metallic = metallic };

	private static CylinderMesh Cyl(float radius, float height, int sides = 16) =>
		new() { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = sides };

	private static MeshInstance3D Part(Node3D parent, Mesh mesh, Material material, Vector3 position, Vector3? rotation = null, Vector3? scale = null) {
		var part = new MeshInstance3D { Mesh = mesh, MaterialOverride = material, Position = position, Rotation = rotation ?? Vector3.Zero,
			Scale = scale ?? Vector3.One, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
		parent.AddChild(part);
		return part;
	}

	/// <summary>The letter as it lies on the blotter: blue handwriting across the top, typed lines, the sum in bold.</summary>
	private static ImageTexture LetterTexture() {
		const int w = 216, h = 279;
		var image = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		image.Fill(new Color("f2ead6"));
		var rng = new Random(1231);
		var blue = new Color("2a4378");
		for (int x = 26; x < 178; x++) {
			int y = 26 + (int)(Mathf.Sin(x * .31f) * 4 + Mathf.Sin(x * .07f) * 3);
			image.FillRect(new Rect2I(x, y, 1, 2 + rng.Next(3)), blue);
		}
		var typed = new Color("4a463f");
		int row = 58;
		void Lines(int count) {
			for (int i = 0; i < count; i++, row += 6)
				image.FillRect(new Rect2I(22, row, (i == count - 1 ? 60 : 150) + rng.Next(22), 2), typed);
		}
		image.FillRect(new Rect2I(140, 46, 52, 2), typed);
		Lines(6);
		row += 6;
		image.FillRect(new Rect2I(70, row, 76, 7), new Color("1d1b18"));
		row += 16;
		Lines(7);
		row += 6;
		Lines(2);
		for (int x = 24; x < 96; x++) {
			int y = row + 12 + (int)(Mathf.Sin(x * .5f) * 5 * Mathf.Sin(x * .05f));
			image.FillRect(new Rect2I(x, y, 1, 2), blue);
		}
		image.FillRect(new Rect2I(22, row + 26, 70, 2), typed);
		return ImageTexture.CreateFromImage(image);
	}

	// ── overlays ────────────────────────────────────────────────────────────────────────────────────

	private Control BuildLetter() {
		var root = new Control { Name = "Letter", MouseFilter = MouseFilterEnum.Ignore };
		root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		root.Theme = PaperTheme.Build();
		var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
		center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		root.AddChild(center);
		var sheet = new PanelContainer { CustomMinimumSize = new Vector2(620, 0) };
		sheet.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = new Color("f4ecd8"), BorderColor = new Color("cdbf9e"), BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
			ShadowColor = new Color(0, 0, 0, .55f), ShadowSize = 28, ShadowOffset = new Vector2(0, 12),
			ContentMarginLeft = 56, ContentMarginRight = 56, ContentMarginTop = 30, ContentMarginBottom = 28,
		});
		center.AddChild(sheet);
		var column = new VBoxContainer();
		column.AddThemeConstantOverride("separation", 10);
		sheet.AddChild(column);

		var pen = new Color("23407a");
		column.AddChild(Text("Congrats on the office!", PaperTheme.Lettering(LetteringStyle.Script), 40, pen));
		var date = Text("1 January 1960", PaperTheme.Typed, 15, PaperTheme.Ink);
		date.HorizontalAlignment = HorizontalAlignment.Right;
		column.AddChild(date);
		column.AddChild(Text("Dear Sir,", PaperTheme.Typed, 15, PaperTheme.Ink));
		column.AddChild(Text("Further to the instrument you executed in the small hours of this morning, in the presence of a witness, " +
			"I enclose your copy. It is a LIEN upon everything you now own, and upon everything the record company you have " +
			"undertaken to form shall ever own, securing the sum of", PaperTheme.Typed, 15, PaperTheme.Ink));
		var sum = Text("$20,000.00", PaperTheme.TypedBold, 34, PaperTheme.Ink);
		sum.HorizontalAlignment = HorizontalAlignment.Center;
		column.AddChild(sum);
		column.AddChild(Text("together with interest at the rate agreed, due and payable in full on the first day of January, 1961. " +
			"The instrument does not admit of early payment. Application for an extension may be made at a sitting in the " +
			"last two weeks of December, when my client will hear you.", PaperTheme.Typed, 15, PaperTheme.Ink));
		column.AddChild(Text("My client asks me to say that he very much enjoyed your company.", PaperTheme.Typed, 15, PaperTheme.Ink));
		column.AddChild(Text("Yours faithfully,", PaperTheme.Typed, 15, PaperTheme.Ink));
		var scrawl = new Control { CustomMinimumSize = new Vector2(240, 36), MouseFilter = MouseFilterEnum.Ignore };
		var ink = new Line2D { Width = 2.2f, DefaultColor = pen, JointMode = Line2D.LineJointMode.Round, Antialiased = true };
		for (int i = 0; i <= 60; i++) {
			float x = 6 + i * 3.6f;
			ink.AddPoint(new Vector2(x, 20 + Mathf.Sin(i * .9f) * 11 * Mathf.Sin(i * .11f + .3f) - i * .12f));
		}
		scrawl.AddChild(ink);
		column.AddChild(scrawl);
		column.AddChild(Text("Counsel, for the holder", PaperTheme.Typed, 15, PaperTheme.Ink));

		var row = new HBoxContainer();
		row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
		var down = new Button { Text = "SET  IT  DOWN", ThemeTypeVariation = PaperTheme.Primary, CustomMinimumSize = new Vector2(190, 42),
			MouseDefaultCursorShape = CursorShape.PointingHand };
		down.Pressed += SetLetterDown;
		row.AddChild(down);
		column.AddChild(row);
		root.Hide();
		return root;
	}

	private Control BuildVoicePanel() {
		var panel = new PanelContainer { Name = "Voice", MouseFilter = MouseFilterEnum.Stop };
		panel.SetAnchorsAndOffsetsPreset(LayoutPreset.RightWide);
		panel.OffsetLeft = -560;
		panel.OffsetRight = -36;
		panel.OffsetTop = 70;
		panel.OffsetBottom = -96;
		panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = new Color(.05f, .04f, .03f, .86f), BorderColor = new Color(.88f, .66f, .29f, .35f), BorderWidthLeft = 2,
			ContentMarginLeft = 30, ContentMarginRight = 28, ContentMarginTop = 28, ContentMarginBottom = 24,
		});
		panel.GuiInput += input => {
			if (input is InputEventMouseButton { Pressed: true } && typing != null) FinishTyping();
		};
		var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
		column.AddThemeConstantOverride("separation", 18);
		panel.AddChild(column);
		voiceLog = new VBoxContainer();
		voiceLog.AddThemeConstantOverride("separation", 16);
		column.AddChild(voiceLog);
		voiceOptions = new VBoxContainer();
		voiceOptions.AddThemeConstantOverride("separation", 2);
		column.AddChild(voiceOptions);
		panel.Hide();
		return panel;
	}

	private static Label Text(string text, Font font, int size, Color color) {
		var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	// ── sound ───────────────────────────────────────────────────────────────────────────────────────

	private void Cue(string key, float at, float t, string sound) {
		if (t < at || !cues.Add(key)) return;
		Play(sound);
	}

	private void Play(string sound) {
		if (!sounds.TryGetValue(sound, out AudioStream stream)) return;
		sfx.Stream = stream;
		sfx.Play();
	}

	/// <summary>Small foley made here rather than shipped: two clicks, a drawer each way, a chair, a sheet of paper.</summary>
	private void BuildSounds() {
		const float rate = 22050;
		var noise = new Random(1959);
		float N() => (float)(noise.NextDouble() * 2 - 1);
		float Tick(float t, float at, float pitch, float gain) {
			if (t < at) return 0;
			float s = t - at;
			return gain * (N() * Mathf.Exp(-s * 900) + .5f * Mathf.Sin(Mathf.Tau * pitch * s) * Mathf.Exp(-s * 380));
		}
		float Thunk(float t, float at, float pitch, float gain) {
			if (t < at) return 0;
			float s = t - at;
			return gain * (Mathf.Sin(Mathf.Tau * pitch * s) * Mathf.Exp(-s * 28) + .3f * N() * Mathf.Exp(-s * 90));
		}
		float low = 0;
		float Rumble(float cut) => low += (N() - low) * cut;

		sounds["cock"] = Synth(.25f, t => Tick(t, .005f, 3400, .7f) + Tick(t, .085f, 2700, 1f));
		sounds["uncock"] = Synth(.2f, t => Tick(t, .005f, 2300, .55f));
		sounds["drawer-open"] = Synth(.85f, t => Rumble(.18f) * 1.6f * Bump(0, .1f, .62f, t) + Thunk(t, .6f, 120, .45f) + Tick(t, .61f, 1800, .25f));
		sounds["drawer-close"] = Synth(.85f, t => Rumble(.18f) * 1.5f * Bump(0, .08f, .55f, t) + Thunk(t, .55f, 95, .9f));
		sounds["sit"] = Synth(.9f, t => Thunk(t, 0, 70, .8f) + Rumble(.05f) * 2.2f * Bump(0, .1f, .5f, t)
			+ .18f * Mathf.Sin(Mathf.Tau * (520 - 140 * t) * t) * Bump(.15f, .3f, .7f, t) * (.6f + .4f * Mathf.Sin(t * 70)));
		sounds["paper"] = Synth(.45f, t => N() * (.22f + .5f * Mathf.Max(0, Mathf.Sin(t * 47 + Mathf.Sin(t * 13) * 3))) * Bump(0, .05f, .42f, t));

		AudioStreamWav Synth(float seconds, Func<float, float> wave) {
			int count = (int)(seconds * rate);
			var data = new byte[count * 2];
			low = 0;
			for (int i = 0; i < count; i++) {
				short v = (short)(Mathf.Clamp(wave(i / rate), -1f, 1f) * 30000);
				data[2 * i] = (byte)(v & 0xff);
				data[2 * i + 1] = (byte)((v >> 8) & 0xff);
			}
			return new AudioStreamWav { Format = AudioStreamWav.FormatEnum.Format16Bits, MixRate = (int)rate, Stereo = false, Data = data };
		}
	}

	// ── the hangover ────────────────────────────────────────────────────────────────────────────────

	/// <summary>The hangover look, shared with the founding pages (the headache lifts as the questions go by).</summary>
	public const string HazeShader = @"
shader_type canvas_item;
uniform sampler2D screen_tex : hint_screen_texture, filter_linear;
uniform float haze = 1.0;
uniform float vignette = 0.6;
uniform float desat = 0.0;
uniform float t = 0.0;

void fragment() {
	vec2 uv = SCREEN_UV;
	vec2 c = uv - 0.5;
	c.x *= SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x;
	float d = length(c);
	// Soft at the edges, swimming a little: eight taps on a ring that turns.
	float r = haze * (0.0015 + 0.010 * smoothstep(0.12, 0.75, d));
	vec3 col = texture(screen_tex, uv).rgb * 0.2;
	for (int i = 0; i < 8; i++) {
		float a = float(i) * 0.785398 + t * 0.3;
		col += texture(screen_tex, uv + vec2(cos(a), sin(a)) * r).rgb * 0.1;
	}
	// Double vision that drifts in and out.
	float drift = 0.5 + 0.5 * sin(t * 0.55);
	vec2 g = vec2(0.009 * (0.4 + 0.6 * drift), 0.003 * sin(t * 0.63 + 1.0)) * haze;
	col = mix(col, texture(screen_tex, uv + g).rgb, 0.32 * haze * (0.5 + 0.5 * drift));
	// Daylight hurts: highlights bloom warm.
	col += vec3(0.07, 0.05, 0.02) * haze * smoothstep(0.35, 0.9, dot(col, vec3(0.333)));
	float lum = dot(col, vec3(0.299, 0.587, 0.114));
	col = mix(col, vec3(lum) * vec3(1.02, 1.0, 0.97), desat);
	// The edges close in on the pulse. A squared-off falloff that follows the frame (a superellipse in screen
	// space), so the dark comes in from all four sides rather than as a porthole.
	float beat = pow(max(sin(t * 2.3), 0.0), 14.0) + 0.6 * pow(max(sin(t * 2.3 - 0.45), 0.0), 14.0);
	vec2 q = abs(uv - 0.5) * 2.0;
	float edge = pow(pow(q.x, 3.5) + pow(q.y, 3.5), 1.0 / 3.5);
	float v = 1.0 - smoothstep(0.5, 1.3, edge * (0.72 + 0.42 * vignette + 0.08 * beat * vignette));
	col *= mix(0.05, 1.0, v);
	COLOR = vec4(col, 1.0);
}";
}
