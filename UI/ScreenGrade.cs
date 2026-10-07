using Godot;

/// <summary>
/// One warm grade and a vignette laid over the whole screen, painting and UI together, so the cut-and-paste paper and
/// the painted desk read as one photograph instead of two layers: a multiply tint that pulls every colour a little
/// toward lamplight, and a dark-brown fall-off into the corners. It sits on a CanvasLayer above the modals, ignores
/// the mouse, and is the only thing that touches both, so changing the look of the room is one place.
/// </summary>
public partial class ScreenGrade : CanvasLayer {
	private const string NodeName = "ScreenGrade";

	/// <summary>The multiply colour. White would do nothing; this takes blue down hardest.</summary>
	public static readonly Color Warmth = new(1.0f, 0.975f, 0.93f);
	/// <summary>How dark the corners go (alpha of the vignette's edge).</summary>
	public const float VignetteStrength = 0.22f;

	/// <summary>Adds the grade to <paramref name="host"/> once; a second call does nothing.</summary>
	public static void Install(Node host) {
		if (host.GetNodeOrNull(NodeName) != null) return;
		host.AddChild(new ScreenGrade { Name = NodeName, Layer = 90 });
	}

	public override void _Ready() {
		var warm = new ColorRect { Color = Warmth, MouseFilter = Control.MouseFilterEnum.Ignore, Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Mul } };
		warm.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(warm);

		var gradient = new Gradient();
		gradient.Offsets = new[] { 0f, 0.66f, 1f };
		gradient.Colors = new[] { new Color(0.09f, 0.05f, 0.02f, 0f), new Color(0.09f, 0.05f, 0.02f, 0f), new Color(0.09f, 0.05f, 0.02f, VignetteStrength) };
		var vignette = new TextureRect {
			Texture = new GradientTexture2D { Gradient = gradient, Fill = GradientTexture2D.FillEnum.Radial, FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1.0f, 0.5f), Width = 256, Height = 144 },
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		vignette.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		AddChild(vignette);
	}
}
