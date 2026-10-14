using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// A page of the books: ledger paper with a double red margin rule, ruled lines under every entry, typewriter figures
/// right-aligned in their own ruled columns, negatives in parentheses in stamp red, and totals set off by a single rule
/// above and a double rule below. It draws itself (no cells, no labels), so a long page of rows costs one control.
///
/// The first column is the account (left-aligned, trimmed with an ellipsis if it is too long); every other column holds figures.
/// </summary>
public partial class LedgerTable : Control {
	public enum RowKind { Entry, Total, Heading }

	public sealed class Row {
		public string[] Cells;
		public RowKind Kind;
	}

	public static Row Entry(params string[] cells) => new() { Cells = cells, Kind = RowKind.Entry };
	public static Row Total(params string[] cells) => new() { Cells = cells, Kind = RowKind.Total };
	public static Row Heading(string text) => new() { Cells = new[] { text }, Kind = RowKind.Heading };

	/// <summary>A figure in the ledger's convention: dollars with separators, a loss in parentheses (and so in red).</summary>
	public static string Money(float amount) => amount < -0.5f ? $"(${-amount:N0})" : $"${Math.Max(0f, amount):N0}";
	/// <summary>A deduction: always a parenthesised amount, or a dash when there is nothing to take off.</summary>
	public static string Deduct(float amount) => amount < 0.5f ? "—" : $"(${amount:N0})";

	private static readonly Color Margin = new("c2574a");
	private static readonly Color RuleBlue = new("93b3c6");
	private static readonly Color Loss = new("a8322a");

	private const float RowHeight = 31f, CaptionHeight = 38f, HeaderHeight = 32f, MarginLeft = 42f, PadRight = 18f, ColumnPad = 30f;
	private const int FigureSize = 16;

	private readonly PaperStyleBox sheet = new() { Fill = new Color("f7eed4"), Border = new Color("8c6f38"), ShadowSize = 6, ShadowAlpha = 0.3f, ShadowOffset = new Vector2(1, 3), Radius = 1, Burn = 0.55f };
	private string caption = "";
	private string[] headers;
	private List<Row> rows = new();
	private float[] figureWidths = Array.Empty<float>();
	private int columns;

	public LedgerTable() {
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		MouseFilter = MouseFilterEnum.Ignore;
	}

	/// <summary>Sets the page: a typed caption across the top, optional column heads, and the rows.</summary>
	public LedgerTable Set(string captionText, string[] columnHeads, IEnumerable<Row> pageRows) {
		caption = captionText ?? "";
		headers = columnHeads;
		rows = pageRows.ToList();
		columns = Math.Max(headers?.Length ?? 0, rows.Where(r => r.Kind != RowKind.Heading).Select(r => r.Cells.Length).DefaultIfEmpty(0).Max());
		Font typed = PaperTheme.Typed, bold = PaperTheme.TypedBold;
		figureWidths = new float[Math.Max(0, columns - 1)];
		for (int c = 1; c < columns; c++) {
			float widest = headers != null && c < headers.Length ? bold.GetStringSize(headers[c].ToUpperInvariant(), HorizontalAlignment.Left, -1, 13).X : 0f;
			foreach (Row row in rows) {
				if (row.Kind == RowKind.Heading || c >= row.Cells.Length) continue;
				widest = Mathf.Max(widest, (row.Kind == RowKind.Total ? bold : typed).GetStringSize(row.Cells[c], HorizontalAlignment.Left, -1, FigureSize).X);
			}
			figureWidths[c - 1] = Mathf.Max(92f, widest + ColumnPad);
		}
		UpdateMinimumSize();
		QueueRedraw();
		return this;
	}

	private float TableTop => (string.IsNullOrEmpty(caption) ? 8f : CaptionHeight) + (headers != null ? HeaderHeight : 0f);

	public override Vector2 _GetMinimumSize() =>
		new(MarginLeft + 220f + figureWidths.Sum() + PadRight, TableTop + rows.Count * RowHeight + 14f);

	public override void _Draw() {
		Vector2 size = Size;
		sheet.Draw(GetCanvasItem(), new Rect2(Vector2.Zero, size));
		if (columns == 0) return;
		Font typed = PaperTheme.Typed, bold = PaperTheme.TypedBold;
		float right = size.X - PadRight;
		float figuresStart = right - figureWidths.Sum();
		// The double red margin rule runs the full height of the page.
		DrawLine(new Vector2(MarginLeft - 10f, 2f), new Vector2(MarginLeft - 10f, size.Y - 2f), new Color(Margin, 0.8f), 1.5f);
		DrawLine(new Vector2(MarginLeft - 6f, 2f), new Vector2(MarginLeft - 6f, size.Y - 2f), new Color(Margin, 0.8f), 1.5f);

		float y = 0f;
		if (!string.IsNullOrEmpty(caption)) {
			DrawString(bold, new Vector2(MarginLeft, 25f), caption.ToUpperInvariant(), HorizontalAlignment.Left, size.X - MarginLeft - PadRight, 15, PaperTheme.Rust);
			DrawLine(new Vector2(MarginLeft - 4f, CaptionHeight - 4f), new Vector2(right, CaptionHeight - 4f), new Color(PaperTheme.Ink, 0.85f), 2f);
			y = CaptionHeight;
		} else y = 8f;

		if (headers != null) {
			for (int c = 0; c < columns && c < headers.Length; c++) {
				string head = headers[c].ToUpperInvariant();
				float x = ColumnX(c, figuresStart);
				float width = ColumnWidth(c, figuresStart);
				Vector2 at = new(c == 0 ? x : x + width - ColumnPad / 2f - bold.GetStringSize(head, HorizontalAlignment.Left, -1, 13).X, y + 21f);
				DrawString(bold, at, head, HorizontalAlignment.Left, -1, 13, new Color(PaperTheme.Fade, 0.95f));
			}
			DrawLine(new Vector2(MarginLeft - 4f, y + HeaderHeight - 2f), new Vector2(right, y + HeaderHeight - 2f), new Color(PaperTheme.Ink, 0.7f), 1.2f);
			y += HeaderHeight;
		}

		float rowsTop = y;
		float baselineOffset = (RowHeight + typed.GetAscent(FigureSize) - typed.GetDescent(FigureSize)) / 2f;
		for (int i = 0; i < rows.Count; i++) {
			Row row = rows[i];
			float top = rowsTop + i * RowHeight;
			if (row.Kind == RowKind.Heading) {
				DrawString(bold, new Vector2(MarginLeft, top + baselineOffset), row.Cells[0].ToUpperInvariant(), HorizontalAlignment.Left, -1, 13, new Color(PaperTheme.Rust, 0.95f));
				DrawLine(new Vector2(MarginLeft - 4f, top + RowHeight), new Vector2(right, top + RowHeight), new Color(RuleBlue, 0.6f), 1f);
				continue;
			}
			bool total = row.Kind == RowKind.Total;
			Font font = total ? bold : typed;
			for (int c = 0; c < row.Cells.Length && c < columns; c++) {
				string text = row.Cells[c] ?? "";
				float x = ColumnX(c, figuresStart), width = ColumnWidth(c, figuresStart);
				Color ink = c > 0 && text.StartsWith('(') ? Loss : PaperTheme.Ink;
				if (c == 0) {
					text = Fit(text, font, width - 12f);
					DrawString(font, new Vector2(x, top + baselineOffset), text, HorizontalAlignment.Left, -1, FigureSize, ink);
				} else {
					float textWidth = font.GetStringSize(text, HorizontalAlignment.Left, -1, FigureSize).X;
					DrawString(font, new Vector2(x + width - ColumnPad / 2f - textWidth, top + baselineOffset), text, HorizontalAlignment.Left, -1, FigureSize, ink);
				}
			}
			DrawLine(new Vector2(MarginLeft - 4f, top + RowHeight), new Vector2(right, top + RowHeight), new Color(RuleBlue, 0.6f), 1f);
			if (total) {
				// A single rule over the figures, a double rule beneath: the sum, drawn as an accountant draws it.
				DrawLine(new Vector2(figuresStart, top + 1f), new Vector2(right, top + 1f), new Color(PaperTheme.Ink, 0.9f), 1.4f);
				DrawLine(new Vector2(figuresStart, top + RowHeight - 6f), new Vector2(right, top + RowHeight - 6f), new Color(PaperTheme.Ink, 0.9f), 1.2f);
				DrawLine(new Vector2(figuresStart, top + RowHeight - 3f), new Vector2(right, top + RowHeight - 3f), new Color(PaperTheme.Ink, 0.9f), 1.2f);
			}
		}
		// Ruled columns for the figures.
		float bottom = rowsTop + rows.Count * RowHeight;
		float columnX = figuresStart;
		for (int c = 0; c < figureWidths.Length; c++) {
			DrawLine(new Vector2(columnX, headers != null ? rowsTop - HeaderHeight + 4f : rowsTop), new Vector2(columnX, bottom), new Color(RuleBlue, 0.55f), 1f);
			columnX += figureWidths[c];
		}
	}

	private float ColumnX(int column, float figuresStart) {
		if (column == 0) return MarginLeft;
		float x = figuresStart;
		for (int c = 1; c < column; c++) x += figureWidths[c - 1];
		return x;
	}

	private float ColumnWidth(int column, float figuresStart) => column == 0 ? figuresStart - MarginLeft : figureWidths[column - 1];

	/// <summary>The text, shortened with an ellipsis if it would run past <paramref name="maxWidth"/>.</summary>
	private static string Fit(string text, Font font, float maxWidth) {
		if (font.GetStringSize(text, HorizontalAlignment.Left, -1, FigureSize).X <= maxWidth) return text;
		while (text.Length > 1 && font.GetStringSize(text + "…", HorizontalAlignment.Left, -1, FigureSize).X > maxWidth) text = text[..^1];
		return text + "…";
	}

	public override void _Notification(int what) {
		if (what == NotificationResized) QueueRedraw();
	}
}
