using System;
using System.Collections.Generic;
using Godot;

// The signing moment and the easy act's one push-back (PlayerDesk.Signing.cs holds the lines and numbers).
// Playtest 2026-11-04: the first signing was a status line, and a 50%-under offer signed without a word.
public partial class PlayerDeskPanel {
	private static readonly Color CarbonInk = new("3d4a8f");
	// Acts that have already pushed back on the offer now on the table: the next submit signs, whatever it says.
	private readonly HashSet<string> pushedBack = new();

	/// <summary>The easy act's contract submit: one push-back first when the offer clears their floor but sits well
	/// under the ask. Taking the bump or holding firm both go to OfferContract, which decides as it always has.</summary>
	private void SubmitEasyOffer(PlayerDesk.Prospect prospect, ContractTermSheet offer, Action<ContractTermSheet> submit) {
		PlayerDesk desk = PlayerDesk.Instance;
		string id = prospect.Artist.artistId;
		if (pushedBack.Remove(id) || desk.PushbackFor(prospect, offer) is not ContractTermSheet bump) { submit(offer); return; }
		pushedBack.Add(id);
		var modal = PaperModal.Open(this, "BEFORE THE PEN GOES DOWN", 600, onCancel: () => pushedBack.Remove(id));
		var line = modal.AddText(desk.PushbackLine(prospect, offer, bump));
		line.AddThemeFontOverride("font", PaperTheme.SerifItalic);
		line.AddThemeFontSizeOverride("font_size", 19);
		var note = modal.AddText("Hold firm and they still sign, but they'll remember how cheaply when the paper comes up for renewal.");
		note.AddThemeFontSizeOverride("font_size", 14);
		string Terms(ContractTermSheet t) => t.Advance != offer.Advance || t.RoyaltyRate == offer.RoyaltyRate
			? $"${t.Advance:N0}" : $"{t.RoyaltyRate:P1}";
		modal.AddButton($"HOLD AT {Terms(offer)}", () => submit(offer));
		modal.AddButton($"MAKE IT {Terms(bump)}", () => submit(bump), PaperModal.ButtonKind.Primary);
	}

	/// <summary>Shows a signing the desk hasn't shown yet. Called on every refresh; the desk hands each one over once.</summary>
	private void ShowPendingSigningMoment(PlayerDesk desk) {
		PlayerDesk.SigningMoment moment = desk?.TakeSigningMoment();
		if (moment == null) return;
		SimulatedArtist act = ArtistManager.Instance?.GetArtist(moment.ArtistId);
		LabelBrand brand = desk.Label != null ? LabelBrand.For(desk.Label) : null;

		var modal = PaperModal.Open(this, moment.First ? "THE FIRST NAME ON THE LABEL" : "A NEW NAME ON THE LABEL", 640);

		// Who: the act's photo beside its name, its music and its number in the label's series.
		var head = new HBoxContainer();
		head.AddThemeConstantOverride("separation", 18);
		modal.Body.AddChild(head);
		if (act != null) head.AddChild(new PortraitPhoto().Set(Portraits.ForAct(act), new Vector2(112, 140), act.artistId));
		var who = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
		who.AddThemeConstantOverride("separation", 4);
		head.AddChild(who);
		who.AddChild(SigningText(moment.ActName, 30, Ink, PaperTheme.SerifBold));
		if (act != null) who.AddChild(SigningText(GenreNameFormatter.Format(act.primaryGenre), 15, Rust, PaperTheme.SansSemiBold));
		var number = SigningText(moment.CatalogNumber, brand != null ? 26 : 20, brand?.Pair.Ink ?? Rust,
			brand != null ? PaperTheme.Lettering(brand.Lettering) : PaperTheme.TypedBold);
		number.TooltipText = "Catalogue number: the act's place in your label's signing order.";
		number.MouseFilter = MouseFilterEnum.Pass;
		who.AddChild(number);

		if (!string.IsNullOrEmpty(moment.Reaction)) modal.Body.AddChild(SigningText(moment.Reaction, 17, Ink, PaperTheme.SerifItalic));

		// The signature writes itself onto the line, then the stamp comes down beside it.
		var signature = new SignatureLine(moment.Signer ?? moment.ActName, desk.Label?.labelName);
		modal.Body.AddChild(signature);
		modal.Body.AddChild(SigningText(moment.Handshake, 18, Ink, PaperTheme.SerifItalic));
		if (moment.First)
			modal.Body.AddChild(SigningText("Next: choose their material and cut a record — ROSTER, then MANAGE.", 14, Rust, PaperTheme.SansSemiBold));

		if (moment.First) {
			modal.AddButton("CLOSE", null);
			modal.AddButton("TO THE ROSTER", () => GoToTab(1, PageRoster), PaperModal.ButtonKind.Primary);
		} else modal.AddButton("DONE", null, PaperModal.ButtonKind.Primary);
		signature.Play();
	}

	private static Label SigningText(string text, int size, Color color, Font font) {
		var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", size);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	/// <summary>A ruled signature line: the act's name in a quick script hand, revealed left to right as if written,
	/// over a caption; then SIGNED stamped across its right end. Under a second and a half in all.</summary>
	private partial class SignatureLine : Control {
		private const float WriteSeconds = 0.9f, StampDelay = 0.15f;
		private readonly string name, labelName;
		private float written;
		private readonly RubberStamp stamp;

		public SignatureLine(string actName, string label) {
			name = actName ?? "";
			labelName = label;
			CustomMinimumSize = new Vector2(0, 92);
			SizeFlagsHorizontal = SizeFlags.ExpandFill;
			MouseFilter = MouseFilterEnum.Ignore;
			stamp = new RubberStamp().Set("Signed", RubberStamp.Red, -0.14f, 24);
			stamp.Modulate = new Color(1, 1, 1, 0);
			AddChild(stamp);
		}

		public void Play() {
			Tween tween = CreateTween();
			tween.TweenMethod(Callable.From<float>(t => { written = t; QueueRedraw(); }), 0f, 1f, WriteSeconds)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
			tween.TweenInterval(StampDelay);
			tween.TweenCallback(Callable.From(() => {
				stamp.PivotOffset = stamp.Size / 2f;
				stamp.Scale = new Vector2(1.7f, 1.7f);
				stamp.Modulate = new Color(1, 1, 1, 0.2f);
			}));
			tween.TweenProperty(stamp, "scale", Vector2.One, 0.11f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			tween.Parallel().TweenProperty(stamp, "modulate", Colors.White, 0.11f);
		}

		public override void _Notification(int what) {
			if (what == NotificationResized) PlaceStamp();
		}

		private void PlaceStamp() {
			Vector2 s = stamp.GetCombinedMinimumSize();
			stamp.Size = s;
			stamp.Position = new Vector2(Size.X - s.X - 18, (Size.Y - s.Y) / 2f - 6);
		}

		public override void _Draw() {
			Font hand = PaperTheme.Lettering(LetteringStyle.Script);
			Font caption = PaperTheme.Sans;
			const int handSize = 38;
			float baseline = Size.Y - 30;
			// The rule and its caption, typed on the carbon.
			DrawLine(new Vector2(0, baseline + 6), new Vector2(Size.X * 0.72f, baseline + 6), new Color(Ink, 0.55f), 1.2f);
			DrawString(caption, new Vector2(0, baseline + 24), "FOR THE ARTIST" + (labelName != null ? $"  ·  WITH {labelName.ToUpperInvariant()}" : ""),
				HorizontalAlignment.Left, -1, 11, new Color(Ink, 0.6f));
			if (written <= 0f || name.Length == 0) return;
			// Written left to right: clip the drawn name to the part the pen has reached.
			Vector2 full = hand.GetStringSize(name, HorizontalAlignment.Left, -1, handSize);
			float reach = 12 + full.X * written;
			int shown = 0;
			float x = 12;
			for (int i = 0; i < name.Length; i++) {
				float w = hand.GetStringSize(name[i].ToString(), HorizontalAlignment.Left, -1, handSize).X;
				if (x + w * 0.5f > reach) break;
				x += w; shown = i + 1;
			}
			if (shown > 0) DrawString(hand, new Vector2(12, baseline), name[..shown], HorizontalAlignment.Left, -1, handSize, CarbonInk);
		}
	}
}
