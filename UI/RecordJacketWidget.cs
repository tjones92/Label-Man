using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The record jacket: a small card in the top-right of the desk scene, opposite the OPEN THE OFFICE button,
/// that keeps every record in the works in sight. Each row is one record's trip through Songs -> Takes ->
/// Master -> Pressing -> Ship date, with the date it lands. It only reads <see cref="PlayerDesk"/> state the
/// CATALOG's pipeline already shows; clicking it opens that tab.
/// </summary>
public partial class RecordJacketWidget : PanelContainer {
	private static readonly string[] JacketStages = { "SONGS", "TAKES", "MASTER", "PRESSING", "SHIP DATE" };
	// Where each rectangle's work is done, for the hover on a stage that is done or still ahead.
	private static readonly string[] JacketSections = {
		"Roster → Manage", "Roster → Manage", "Catalog", "Catalog", "Catalog"
	};
	private static readonly string[] JacketStageNames = { "Songs", "Takes", "Master", "Pressing", "Ship date" };
	private const int JacketMaxRows = 3;
	public const float JacketWidth = 580f;

	private static readonly Color Ink = new("2b2115");
	private static readonly Color Heard = new("6b5a3a");
	private static readonly Color JacketDone = new("6b5a3a");
	private static readonly Color JacketNow = new("b5541c");
	private static readonly Color JacketLater = new("d3c294");

	private readonly VBoxContainer rows;

	/// <summary>Raised when the player clicks the card; the desk scene opens the Catalog.</summary>
	public event Action Clicked;

	private sealed class JacketEntry {
		public string Title;
		public int Stage;          // index into JacketStages: the stage the record is in right now
		public string Detail;      // short, after the stage name: "lands Mar 21 (19d)"
		public string NowTip;      // what the current rectangle says on hover: the step and its ETA
		public int SortDays = int.MaxValue;
		public string Tooltip;
	}

	public RecordJacketWidget() {
		CustomMinimumSize = new Vector2(JacketWidth, 0);
		MouseDefaultCursorShape = CursorShape.PointingHand;
		AddThemeStyleboxOverride("panel", new StyleBoxFlat {
			BgColor = new Color(0.91f, 0.84f, 0.65f, 0.94f), BorderColor = new Color("8a7048"),
			BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
			CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
			ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 6, ContentMarginBottom = 8
		});
		rows = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		rows.AddThemeConstantOverride("separation", 3);
		AddChild(rows);
	}

	public override void _GuiInput(InputEvent @event) {
		if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) {
			Clicked?.Invoke();
			AcceptEvent();
		}
	}

	public void Refresh(PlayerDesk desk) {
		Visible = desk?.HasLabel == true && !desk.IsGameOver;
		if (!Visible) return;
		foreach (Node child in rows.GetChildren()) { rows.RemoveChild(child); child.QueueFree(); }

		// The sleeve's die-cut window shows the label's own 45 centre label.
		var header = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
		var caption = new Label { Text = "RECORDS IN THE WORKS", MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter };
		caption.AddThemeFontSizeOverride("font_size", 13);
		caption.AddThemeColorOverride("font_color", Heard);
		header.AddChild(caption);
		header.AddChild(new LabelCrest().Set(LabelBrand.For(desk.Label), desk.Label.labelName, 40f, LabelCrest.Mode.Disc45));
		rows.AddChild(header);

		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		List<JacketEntry> entries = JacketEntries(desk, today);
		TooltipText = "Click to open the Catalog.";
		if (entries.Count == 0) {
			var empty = new Label {
				Text = "Nothing in the works. Book an act's studio time to start one.",
				AutowrapMode = TextServer.AutowrapMode.WordSmart, MouseFilter = MouseFilterEnum.Ignore
			};
			empty.AddThemeFontSizeOverride("font_size", 16);
			empty.AddThemeColorOverride("font_color", Heard);
			rows.AddChild(empty);
			return;
		}

		foreach (JacketEntry entry in entries.Take(JacketMaxRows)) rows.AddChild(JacketRow(entry));
		if (entries.Count > JacketMaxRows) {
			var more = new Label { Text = $"+ {entries.Count - JacketMaxRows} more in the Catalog", MouseFilter = MouseFilterEnum.Ignore };
			more.AddThemeFontSizeOverride("font_size", 14);
			more.AddThemeColorOverride("font_color", Heard);
			rows.AddChild(more);
			TooltipText += "\n\n" + string.Join("\n", entries.Skip(JacketMaxRows).Select(item => item.Tooltip));
		}
	}

	private static Control JacketRow(JacketEntry entry) {
		var row = new HBoxContainer { TooltipText = entry.Tooltip, MouseFilter = MouseFilterEnum.Pass };
		row.AddThemeConstantOverride("separation", 8);

		var title = new Label {
			Text = entry.Title, ClipText = true, CustomMinimumSize = new Vector2(176, 0),
			MouseFilter = MouseFilterEnum.Ignore
		};
		title.AddThemeFontSizeOverride("font_size", 17);
		title.AddThemeColorOverride("font_color", Ink);
		row.AddChild(title);

		var pips = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
		pips.AddThemeConstantOverride("separation", 2);
		for (int stage = 0; stage < JacketStages.Length; stage++)
			pips.AddChild(new ColorRect {
				CustomMinimumSize = new Vector2(20, 11), MouseFilter = MouseFilterEnum.Pass,
				Color = stage < entry.Stage ? JacketDone : stage == entry.Stage ? JacketNow : JacketLater,
				TooltipText = stage == entry.Stage ? entry.NowTip ?? $"{JacketStageNames[stage]} — {entry.Detail}"
					: $"{JacketStageNames[stage]}{(stage < entry.Stage ? " — done" : "")} · {JacketSections[stage]}"
			});
		row.AddChild(pips);

		var detail = new Label {
			Text = $"{JacketStages[entry.Stage]} · {entry.Detail}", ClipText = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore
		};
		detail.AddThemeFontSizeOverride("font_size", 15);
		detail.AddThemeColorOverride("font_color", Heard);
		row.AddChild(detail);
		return row;
	}

	/// <summary>One entry per record in the works, nearest to shipping first. A record with a plant order is
	/// in PRESSING until the vinyl lands; after that it waits in SHIP DATE until it goes out, and then it
	/// leaves the jacket (the Catalog's "In the market" takes over).</summary>
	private static List<JacketEntry> JacketEntries(PlayerDesk desk, GameDate today) {
		var entries = new List<JacketEntry>();

		foreach (SimulatedArtist artist in desk.Label.roster)
			foreach (PlayerDesk.CoverRehearsal work in desk.RehearsalsFor(artist.artistId)) {
				int days = DaysFrom(today, work.ReadyDate);
				entries.Add(new JacketEntry {
					Title = work.Title, Stage = 0, SortDays = days,
					Detail = $"ready {ShortDay(work.ReadyDate)} ({days}d)",
					Tooltip = $"\"{work.Title}\" for {artist.stageName}: {(work.IsCommission ? "commissioned, due" : "being learned, ready")} {work.ReadyDate.ToHeadlineString()}."
				});
			}

		if (desk.Session != null) {
			string act = ArtistManager.Instance?.GetArtist(desk.Session.ArtistId)?.stageName ?? "The act";
			int songs = desk.Session.Cuts.Count;
			entries.Add(new JacketEntry {
				Title = act, Stage = 1, Detail = "pick takes, print masters",
				Tooltip = $"{act}: {songs} {(songs == 1 ? "song" : "songs")} in the can. Keep a take for each and print the masters."
			});
		}

		var bSides = new HashSet<PlayerDesk.Master>(desk.Planned.Where(item => item.BSide != null).Select(item => item.BSide));
		foreach (PlayerDesk.Master master in desk.Masters.Where(item => !item.Released && !bSides.Contains(item))) {
			string id = master.Record?.recordId;
			if (string.IsNullOrEmpty(id)) continue;
			PlayerDesk.PlannedRelease single = desk.Planned.FirstOrDefault(item => item.Master == master);
			PlayerDesk.PressOrder order = desk.PressingOrderFor(id);
			bool pressed = order == null && (desk.StockFor(id)?.TotalPressed ?? 0) > 0;
			string ships = single?.Dated == true ? $"ships {single.Date.ToHeadlineString()} ({DaysFrom(today, single.Date)}d)" : null;
			string shipsShort = single?.Dated == true ? $"ships {ShortDay(single.Date)} ({DaysFrom(today, single.Date)}d)" : null;
			var entry = new JacketEntry { Title = master.SongTitle };
			string by = master.Record.artistName;

			if (single == null) {
				entry.Stage = 2;
				entry.Detail = "assemble a single";
				entry.Tooltip = $"\"{master.SongTitle}\" by {by}: master cut {master.Cut.ToHeadlineString()}. Pair it into a 45 in the Catalog.";
			} else if (order != null) {
				int days = DaysFrom(today, order.Arrives);
				entry.Stage = 3;
				entry.SortDays = days;
				entry.Detail = $"lands {ShortDay(order.Arrives)} ({days}d)";
				(string step, GameDate ends) = PlayerDeskPanel.PipelinePressStep(order, today);
				int stepDays = DaysFrom(today, ends);
				entry.NowTip = $"{step} — {(stepDays == 0 ? "done today" : $"done in {stepDays}d")} ({ShortDay(ends)})";
				entry.Tooltip = $"\"{master.SongTitle}\" by {by}: on order, vinyl lands {order.Arrives.ToHeadlineString()}." + (ships != null ? $" Then {ships}." : " No ship date set yet.");
			} else if (pressed) {
				entry.Stage = 4;
				entry.SortDays = single.Dated ? DaysFrom(today, single.Date) : int.MaxValue;
				entry.Detail = shipsShort ?? "set a date";
				entry.Tooltip = $"\"{master.SongTitle}\" by {by}: pressed and in the office. " + (ships != null ? $"It {ships}." : "Set its release date in the Catalog.");
			} else {
				entry.Stage = 3;
				entry.Detail = "not ordered yet";
				entry.Tooltip = $"\"{master.SongTitle}\" by {by}: assembled. Order a pressing in the Catalog" + (ships != null ? $"; it is dated to ship {single.Date.ToHeadlineString()}." : ".");
			}
			entries.Add(entry);
		}

		return entries.OrderByDescending(item => item.Stage).ThenBy(item => item.SortDays).ToList();
	}

	private static string ShortDay(GameDate date) => $"{date.ShortMonthName} {date.day}";

	private static int DaysFrom(GameDate today, GameDate date) =>
		Math.Max(0, (new DateTime(date.year, date.month, date.day) - new DateTime(today.year, today.month, today.day)).Days);
}
