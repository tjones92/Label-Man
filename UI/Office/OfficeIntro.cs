using System;
using Godot;

/// <summary>The first walk into the office ends at the playable desk camera.</summary>
public partial class OfficeIntro : Control {
	private const float Duration = 11.65f;
	private readonly record struct Shot(float Time, Vector3 Position, Vector3 Target, float Roll, float Fov) {
		public Quaternion Facing => (Basis.LookingAt(Target - Position)
			* new Basis(Vector3.Back, Mathf.DegToRad(Roll))).GetRotationQuaternion();
	}

	private ColorRect fade;
	private Button skip;
	private AudioStreamPlayer foley;
	private Camera3D deskCamera, camera;
	private Node3D door, set;
	private OmniLight3D hallLight, ceilingLight;
	private Vector3 closedDoorRotation;
	private Viewport.Msaa deskMsaa;
	private Shot[] shots;
	private Action onSeated;
	private float elapsed;
	public SubViewport Room { get; init; }
	public bool IsPlaying { get; private set; }

	public override void _Ready() {
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
		MouseFilter = MouseFilterEnum.Stop;
		set = Room?.GetNodeOrNull<Node3D>("Office/OfficeIntroSet");
		set?.Hide();
		fade = new ColorRect { Name = "Fade", Color = Colors.Black, MouseFilter = MouseFilterEnum.Ignore };
		AddChild(fade);
		fade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
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
		foley = new AudioStreamPlayer {
			Stream = GD.Load<AudioStream>("res://UI/Office/office_entry.wav"),
			VolumeDb = -5f
		};
		AddChild(foley);
		SetProcess(false);
		SetProcessInput(false);
		Hide();
	}

	public bool Play(Action finished) {
		if (Visible || Room == null) return false;
		deskCamera = Room.GetCamera3D();
		door = Room.GetNodeOrNull<Node3D>("Office/OfficeDoorHinge");
		Node3D seat = Room.GetNodeOrNull<Node3D>("Office/OfficeSeatEye");
		hallLight = Room.GetNodeOrNull<OmniLight3D>("IntroHallLight");
		ceilingLight = Room.GetNodeOrNull<OmniLight3D>("IntroCeilingLight");
		if (deskCamera == null || door == null || seat == null || set == null) return false;

		closedDoorRotation = door.RotationDegrees;
		if (set is MeshInstance3D mesh) mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		set.Show();
		hallLight?.Show();
		ceilingLight?.Show();
		camera = new Camera3D { Name = "IntroCamera", Near = .025f, Far = deskCamera.Far };
		Room.AddChild(camera);
		shots = EntranceShots(seat.GlobalPosition, deskCamera.GlobalTransform.Basis, deskCamera.Fov);
		onSeated = finished;
		elapsed = 0;
		IsPlaying = true;
		skip.Disabled = false;
		skip.Show();
		fade.Color = Colors.Black;
		Show();
		SetProcess(true);
		SetProcessInput(true);
		SetShot(0);
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

	public override void _Input(InputEvent input) {
		if (!input.IsActionPressed("ui_cancel") || input.IsEcho()) return;
		GetViewport().SetInputAsHandled();
		if (IsPlaying && !skip.Disabled) Skip();
	}

	public override void _Process(double delta) {
		elapsed = Mathf.Min(elapsed + (float)delta, Duration);
		SetShot(elapsed);
		fade.Color = new Color(0, 0, 0, 1f - Mathf.SmoothStep(.04f, .57f, elapsed));
		skip.Modulate = new Color(1, 1, 1, Mathf.SmoothStep(.25f, .85f, elapsed));
		if (elapsed >= Duration - .42f) skip.Hide();
		if (elapsed >= Duration) SeatPlayer();
	}

	private void SetShot(float time) {
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
		var orientation = a.Facing.Slerp(b.Facing, ease);
		camera.GlobalTransform = new Transform3D(new Basis(orientation), position + Vector3.Up * (.027f * stride * gait));
		camera.Fov = Mathf.Lerp(a.Fov, b.Fov, ease);
		// Fully open before the first step across; shut and latched before the walk passes in front of it.
		float swing = Mathf.SmoothStep(.25f, 1.30f, time) * (1 - Mathf.SmoothStep(3.05f, 4.15f, time));
		door.RotationDegrees = closedDoorRotation + Vector3.Up * (-91f * swing);
	}

	private void Skip() {
		if (!IsPlaying || skip.Disabled) return;
		SetProcess(false);
		skip.Disabled = true;
		foley.Stop();
		var cut = CreateTween();
		cut.TweenProperty(fade, "color:a", 1f, .21f);
		cut.TweenCallback(Callable.From(SeatPlayer));
	}

	private void SeatPlayer() {
		SetProcess(false);
		skip.Hide();
		foley.Stop();
		deskCamera.MakeCurrent();
		camera.QueueFree();
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

	private static Shot[] EntranceShots(Vector3 seat, Basis deskFacing, float deskFov) => new Shot[] {
		// Wait in the hall while the door swings in, step through into the pocket right of its arc (clear of the
		// credenza and the guest chair) and glance at the credenza while it swings shut behind, then walk left past
		// it once it has latched. The door's edge never comes within a quarter metre of the eye, nor into view.
		new(0.00f, new(-1.70f, 1.59f, -3.58f), new(-1.60f, 1.32f, -2.74f), -2.2f, 51f),
		new(.75f, new(-1.70f, 1.58f, -3.50f), new(-1.62f, 1.30f, -2.72f), -1.1f, 51f),
		new(1.60f, new(-1.66f, 1.62f, -3.02f), new(-1.40f, 1.42f, -1.10f), 2.4f, 54f),
		new(2.35f, new(-1.30f, 1.59f, -2.50f), new(-1.05f, 1.62f, -.40f), -1.4f, 54f),
		new(3.05f, new(-1.00f, 1.57f, -2.36f), new(.45f, 1.15f, -2.30f), 1.5f, 54f),
		new(3.85f, new(-1.02f, 1.58f, -2.35f), new(-.30f, 1.25f, -1.00f), -1.0f, 54f),
		new(4.75f, new(-1.95f, 1.60f, -2.25f), new(-2.94f, 1.48f, -1.05f), -2.1f, 52f),
		new(5.42f, new(-2.48f, 1.55f, -1.91f), new(-2.19f, 1.06f, -.72f), -2.8f, 52f),
		new(6.16f, new(-2.52f, 1.59f, -1.31f), new(-1.70f, 1.22f, -.22f), 2.4f, 52f),
		new(6.88f, new(-2.50f, 1.58f, -.59f), seat + new Vector3(0, -.44f, .11f), -1.4f, 53f),
		new(7.61f, new(-2.28f, 1.55f, .19f), seat + new Vector3(0, -.60f, .07f), 1.5f, 52f),
		new(8.39f, new(-1.56f, 1.53f, .29f), seat + new Vector3(.06f, -.49f, .13f), -1.9f, 52f),
		new(9.20f, new(-.73f, 1.55f, .30f), seat + new Vector3(.03f, -.46f, .06f), 1.4f, 52f),
		new(9.89f, seat + new Vector3(-.18f, .25f, .18f), new(-1.63f, 1.28f, -2.77f), -2.1f, 51f),
		new(10.52f, seat + new Vector3(.014f, -.032f, .027f), new(-1.63f, 1.10f, -2.77f), 1.3f, 49f),
		new(11.26f, seat, seat - deskFacing.Z, 0f, deskFov),
		new(Duration, seat, seat - deskFacing.Z, 0f, deskFov)
	};
}
