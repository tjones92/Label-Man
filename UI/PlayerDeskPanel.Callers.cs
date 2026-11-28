using System.Linq;
using Godot;

/// <summary>The phone ringing (or a knock) while he waits at the desk, what they said when he picked up, the booker's open
/// set on THE PHONE, and the notes left in the door (PlayerDesk.Callers). The 3D phone on the desk rings and rattles too
/// (UIManager.Callers); this is the folder's half: a PICK UP button that pulses beside the waits, and the card.</summary>
public partial class PlayerDeskPanel {
	private Button pickUpButton;
	private Tween pickUpPulse;

	/// <summary>Built into the waits row, first in line: hidden until something rings.</summary>
	private void BuildPickUpButton(HBoxContainer row) {
		pickUpButton = Btn("PICK UP THE PHONE");
		pickUpButton.Visible = false;
		pickUpButton.AddThemeColorOverride("font_color", new Color("fff3dc"));
		pickUpButton.AddThemeColorOverride("font_hover_color", Colors.White);
		var style = new StyleBoxFlat { BgColor = new Color("8c2a1c"), BorderColor = new Color("4a120a"),
			BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
			ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 4, ContentMarginBottom = 4 };
		pickUpButton.AddThemeStyleboxOverride("normal", style);
		pickUpButton.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color("a83322"), BorderColor = style.BorderColor,
			BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
			ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 4, ContentMarginBottom = 4 });
		pickUpButton.Pressed += AnswerCaller;
		row.AddChild(pickUpButton);
		row.MoveChild(pickUpButton, 0);
	}

	private void RefreshPickUp(PlayerDesk desk) {
		if (pickUpButton == null) return;
		var calling = desk?.Calling ?? PlayerDesk.CallerAt.None;
		bool show = calling != PlayerDesk.CallerAt.None;
		pickUpButton.Text = calling == PlayerDesk.CallerAt.Door ? "ANSWER THE DOOR" : "PICK UP THE PHONE";
		pickUpButton.TooltipText = calling == PlayerDesk.CallerAt.Door
			? "Somebody's knocking. If you go on with something else, they'll leave a note in the door."
			: $"It's ringing. Picking up takes {PlayerDesk.PickUpMinutes} minutes. Let it ring and they're gone: most callers don't try twice.";
		if (show == pickUpButton.Visible) return;
		pickUpButton.Visible = show;
		pickUpPulse?.Kill();
		pickUpButton.Modulate = Colors.White;
		if (!show) return;
		pickUpPulse = pickUpButton.CreateTween().SetLoops();
		pickUpPulse.TweenProperty(pickUpButton, "modulate", new Color(1f, .78f, .70f), .45f).SetTrans(Tween.TransitionType.Sine);
		pickUpPulse.TweenProperty(pickUpButton, "modulate", Colors.White, .45f).SetTrans(Tween.TransitionType.Sine);
	}

	/// <summary>Pick up (or open the door) and show who it is. Called from the button and from the phone on the desk.</summary>
	public void AnswerCaller() {
		var desk = PlayerDesk.Instance;
		if (desk == null || desk.Calling == PlayerDesk.CallerAt.None || PaperModal.IsOpen) return;
		CallerCard card = desk.PickUp(out string message);
		if (card == null) { Say(message, false); return; }
		ShowCallerCard(this, card);
		Refresh();
	}

	/// <summary>The card: who it was, what they said, and anything to answer while they're still on the line or at the door.</summary>
	public static void ShowCallerCard(Node host, CallerCard card) {
		var desk = PlayerDesk.Instance;
		var modal = PaperModal.Open(host, card.AtTheDoor ? "AT THE DOOR" : "THE PHONE", 600);
		var from = modal.AddText(card.From);
		from.AddThemeFontOverride("font", PaperTheme.SansBold);
		modal.AddText(card.Said);
		if (!string.IsNullOrEmpty(card.After)) {
			var after = modal.AddText(card.After);
			after.AddThemeColorOverride("font_color", new Color("6b5a3e"));
		}
		if (card.OfferId != null && desk?.BookerOffer(card.OfferId) != null) {
			var acts = desk.ActsForOffer(card.OfferId);
			if (acts.Count == 0) modal.AddText("None of your acts can work it that night.");
			foreach (var act in acts.Take(3)) {
				string id = act.artistId;
				modal.AddButton($"PUT {act.stageName.ToUpperInvariant()} IN", () => {
					bool ok = PlayerDesk.Instance.TakeBookerOffer(card.OfferId, id, onTheLine: true, out string m);
					(host as PlayerDeskPanel)?.Say(m, ok);
				}, PaperModal.ButtonKind.Primary);
			}
			modal.AddButton("NO THANKS", () => PlayerDesk.Instance.TurnDownBookerOffer(card.OfferId));
			modal.AddButton("I'LL CALL YOU BACK", null);
			return;
		}
		if (card.ArtistId != null && ArtistManager.Instance?.GetArtist(card.ArtistId) is { } artist && desk?.StepNeedingATime(artist) is { } step) {
			for (int i = 0; i < step.Offered.Count; i++) {
				int choice = i;
				var t = step.Offered[i];
				modal.AddButton(PlayerDesk.WhenText(t.Day, t.Minute).ToUpperInvariant(), () => {
					bool ok = PlayerDesk.Instance.BookApproachStep(card.ArtistId, step.Kind, choice, out string m);
					(host as PlayerDeskPanel)?.Say(m, ok);
				}, PaperModal.ButtonKind.Primary);
			}
			modal.AddButton("I'LL LET YOU KNOW", null);
			return;
		}
		modal.AddButton(card.AtTheDoor ? "CLOSE THE DOOR" : "HANG UP", null, PaperModal.ButtonKind.Primary);
	}

	/// <summary>A note taken out of the door.</summary>
	public static void ShowDoorNote(Node host, DoorNoteSaveData note) {
		if (note == null) return;
		var modal = PaperModal.Open(host, "LEFT IN THE DOOR", 520);
		var from = modal.AddText(note.From);
		from.AddThemeFontOverride("font", PaperTheme.SansBold);
		var when = modal.AddText(LocalSceneRoomService.Date(note.Day).ToHeadlineString());
		when.AddThemeColorOverride("font_color", new Color("6b5a3e"));
		modal.AddText($"\"{note.Text}\"");
		var desk = PlayerDesk.Instance;
		if (note.ArtistId != null && ArtistManager.Instance?.GetArtist(note.ArtistId) is { } artist && desk?.StepNeedingATime(artist) is { } step) {
			for (int i = 0; i < step.Offered.Count; i++) {
				int choice = i;
				var t = step.Offered[i];
				if (t.Day < desk.SceneDay || t.Day == desk.SceneDay && t.Minute <= desk.NowMinute) continue;
				modal.AddButton($"WRITE IN {PlayerDesk.WhenText(t.Day, t.Minute).ToUpperInvariant()}", () =>
					PlayerDesk.Instance.BookApproachStep(note.ArtistId, step.Kind, choice, out _), PaperModal.ButtonKind.Primary);
			}
		}
		modal.AddButton("PUT IT DOWN", null);
	}

	/// <summary>THE PHONE's open sets: a booker rang today with a set to fill. Ring him back before tonight.</summary>
	private void BookerOffersSection(PlayerDesk desk, bool compact) {
		var offers = desk.BookerOffers;
		if (offers.Count == 0) return;
		Body("A booker rang with a set to fill. Call back today to claim it.");
		foreach (var offer in offers) {
			BoxContainer row = compact ? new VBoxContainer() : new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);
			var lbl = new Label { Text = "    " + desk.OfferText(offer), AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = SizeFlags.ExpandFill };
			lbl.AddThemeColorOverride("font_color", Ink);
			row.AddChild(lbl);
			string id = offer.Id;
			foreach (var act in desk.ActsForOffer(id).Take(3)) {
				string artistId = act.artistId;
				var put = Btn($"PUT {act.stageName.ToUpperInvariant()} IN ({PlayerDesk.CallBackBookerMinutes}m)");
				if (compact) {
					put.AutowrapMode = TextServer.AutowrapMode.WordSmart;
					put.SizeFlagsHorizontal = SizeFlags.ExpandFill;
				}
				put.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.TakeBookerOffer(id, artistId, onTheLine: false, out string m); Say(m, ok); return ok; });
				row.AddChild(put);
			}
			var no = Btn("NO THANKS");
			no.Pressed += () => Act(() => { PlayerDesk.Instance.TurnDownBookerOffer(id); return true; });
			row.AddChild(no);
			content.AddChild(row);
		}
	}
}
