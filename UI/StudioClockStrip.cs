using Godot;

/// <summary>
/// The booked date as a strip of studio time, the way an engineer reads the wall clock against the booking: the
/// time already worked is inked in, the time still free is blank, and the finishing time held at the end of the
/// date is hatched. Hour ticks sit under the strip. Look only: the numbers are the session's own.
/// </summary>
public partial class StudioClockStrip : Control {
    private int start, total = 60, used, reserve;
    private const float BarTop = 2f, BarHeight = 18f;

    public StudioClockStrip() {
        CustomMinimumSize = new Vector2(0, 40);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public StudioClockStrip Set(int startMinute, int totalMinutes, int usedMinutes, int reservedMinutes) {
        start = startMinute; total = Mathf.Max(1, totalMinutes);
        used = Mathf.Clamp(usedMinutes, 0, total); reserve = Mathf.Clamp(reservedMinutes, 0, total);
        QueueRedraw();
        return this;
    }

    public static string ClockText(int minuteOfDay) {
        int minute = ((minuteOfDay % 1440) + 1440) % 1440, hour = minute / 60;
        int shown = hour % 12 == 0 ? 12 : hour % 12;
        return $"{shown}:{minute % 60:00} {(hour < 12 ? "AM" : "PM")}";
    }

    public static string DurationText(int minutes) =>
        minutes >= 60 ? $"{minutes / 60}h {minutes % 60:00}m" : $"{minutes}m";

    public override void _Draw() {
        float width = Size.X;
        float X(int minutes) => width * Mathf.Clamp(minutes / (float)total, 0f, 1f);
        var bar = new Rect2(0, BarTop, width, BarHeight);
        DrawRect(bar, new Color("fffdf2"));

        // Finishing time held back at the end of the date, hatched like a reserved block on a booking sheet.
        float reserveX = X(total - reserve);
        if (reserve > 0) {
            DrawRect(new Rect2(reserveX, BarTop, width - reserveX, BarHeight), new Color(PaperTheme.Rust, 0.16f));
            var hatch = new Color(PaperTheme.Rust, 0.55f);
            for (float x = reserveX - BarHeight; x < width; x += 6f) {
                float x0 = x, y0 = BarTop + BarHeight, x1 = x + BarHeight, y1 = BarTop;
                if (x0 < reserveX) { y0 -= reserveX - x0; x0 = reserveX; }
                if (x1 > width) { y1 += x1 - width; x1 = width; }
                if (x1 > x0) DrawLine(new Vector2(x0, y0), new Vector2(x1, y1), hatch, 1f);
            }
        }
        if (used > 0) DrawRect(new Rect2(0, BarTop, X(used), BarHeight), new Color(PaperTheme.Ink, 0.78f));
        DrawRect(bar, new Color(WorkOrder.FormInk, 0.8f), false, 1.2f);

        Font font = PaperTheme.SansSemiBold;
        var ink = new Color(WorkOrder.FormInk, 0.85f);
        float labelY = BarTop + BarHeight + 15f;
        DrawString(font, new Vector2(0, labelY), ClockText(start), HorizontalAlignment.Left, 80, 11, ink);
        DrawString(font, new Vector2(width - 80, labelY), ClockText(start + total), HorizontalAlignment.Right, 80, 11, ink);
        for (int m = 60 - start % 60; m < total; m += 60) {
            if (m <= 0) continue;
            float x = X(m);
            DrawLine(new Vector2(x, BarTop + BarHeight), new Vector2(x, BarTop + BarHeight + 4f), ink, 1f);
            if (x > 70 && x < width - 70) DrawString(font, new Vector2(x - 40, labelY), ClockText(start + m), HorizontalAlignment.Center, 80, 11, ink);
        }
    }
}
