using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

/// <summary>
/// The first-session trace (SimTools/OnboardingTutorialDirective.md §4 "Measure it"): player-only telemetry for a new
/// player's first days, written to a local file so a cold playtest gives numbers, not impressions.
///
/// It starts the first time the desk shows a label founded in this session (or a loaded one still on its first-week
/// card, marked resumed), and stops at the first single released (the
/// first-week card's last line), at game over, or after <see cref="MaxInGameDays"/> in-game days. It records:
/// - every button press anywhere in the tree (the desk, the office, modals, the studio), with the in-game time, the tab,
///   and whether it was the first-week card's glowing button;
/// - what each press did: <b>ok</b> (the clock moved, the desk changed, or the page redrew), <b>refused</b> (the desk said
///   no), or <b>no visible change</b>; and clicks on greyed buttons, with the reason they gave;
/// - the tabs opened, the NEXT UP lines shown, and the first-week lines as they tick (the milestones).
///
/// A summary is rewritten at each milestone and at the end, so a crash or a quit still leaves the numbers. It goes to
/// user://first-session/ (%APPDATA%\Godot\app_userdata\Label Man\first-session); UI checks write to SimLogs instead.
/// Nothing here is saved with the game, read by the sim or sent anywhere.
/// </summary>
public partial class FirstSessionTrace : Node {
	public static FirstSessionTrace Current { get; private set; }
	public const int MaxInGameDays = 90;

	private static AILabel tracedLabel;   // one trace per founding

	/// <summary>Called by the desk each time it shows an in-game page: starts the trace for a label founded in this session.</summary>
	public static void MaybeStart(Node from, PlayerDesk desk) {
		// A label founded in this session, or a loaded one still on its first week (playtest 2026-11-26: the first cold
		// playtest loaded a save and the trace never started). A loaded one is marked as resumed in the summary.
		if (desk?.Label == null || !(desk.FoundedThisSession || desk.FirstWeekActive) || desk.Label == tracedLabel || desk.IsGameOver) return;
		if (Current != null && !Current.finished) return;   // still following the last founding
		Current?.QueueFree();   // a new game in the same session: the finished trace's files stay on disk
		tracedLabel = desk.Label;
		var trace = new FirstSessionTrace { Name = "FirstSessionTrace", resumed = !desk.FoundedThisSession };
		Current = trace;
		from.GetTree().Root.CallDeferred(Node.MethodName.AddChild, trace);
	}

	private sealed class PressRecord {
		public int N; public double Seconds; public string When; public string Tab; public string Text; public bool Glow; public string Outcome = "";
	}

	private readonly List<PressRecord> log = new();
	private readonly List<(string What, int Press, string When, double Seconds, int GameMinutes)> milestones = new();
	private readonly List<(string Tab, int Press)> tabsOpened = new();
	private readonly List<(string Line, int Press, string When)> nextUpLines = new();
	private readonly HashSet<FirstWeekStep> stepsSeen = new();
	private ulong startMs;
	private DateTime startReal;
	private GameDate startDate;
	private int startMinuteOfDay;
	private string currentTab = "A&R";
	private string filePrefix;
	private int deskChanges, nodeChurn;
	private string lastRefusal;
	private int refusals;
	private bool finished, putAway, resumed;

	public int Presses => log.Count(p => p.Outcome != "disabled");
	public int GlowPresses => log.Count(p => p.Glow);
	public IReadOnlyList<string> Milestones => milestones.Select(m => m.What).ToList();
	public IReadOnlyList<(string Text, string Outcome)> PressOutcomes => log.Select(p => (p.Text, p.Outcome)).ToList();
	public string SummaryPath => filePrefix + "-summary.txt";
	public bool Finished => finished;

	public override void _Ready() {
		startMs = Time.GetTicksMsec();
		startReal = DateTime.Now;
		var clock = TimeManager.Instance;
		startDate = clock?.CurrentDate ?? GameDate.StartDate;
		startMinuteOfDay = clock == null ? 9 * 60 : clock.CurrentHour * 60 + clock.CurrentMinute;
		string dir = PlayerDesk.AutosaveOffForChecks ? ProjectSettings.GlobalizePath("res://SimLogs/first-session") : ProjectSettings.GlobalizePath("user://first-session");
		DirAccess.MakeDirRecursiveAbsolute(dir);
		filePrefix = System.IO.Path.Combine(dir, "first-session-" + startReal.ToString("yyyyMMdd-HHmmss"));

		GetTree().NodeAdded += OnNodeAdded;
		GetTree().NodeRemoved += OnNodeRemoved;
		foreach (var button in Nodes(GetTree().Root).OfType<BaseButton>()) Wire(button);
		if (PlayerDesk.Instance != null) PlayerDesk.Instance.Changed += OnDeskChanged;
		Milestone("Desk opened");
		CheckSteps();
		Write();
	}

	public override void _ExitTree() {
		if (GetTree() != null) { GetTree().NodeAdded -= OnNodeAdded; GetTree().NodeRemoved -= OnNodeRemoved; }
		if (PlayerDesk.Instance != null) PlayerDesk.Instance.Changed -= OnDeskChanged;
		if (!finished) { Milestone("Session closed"); Write(); }
		if (Current == this) Current = null;
	}

	public override void _Notification(int what) {
		if (what == NotificationWMCloseRequest && !finished) { Milestone("Game closed"); Write(); }
	}

	// ---- hooks the desk calls ------------------------------------------------------------------------------------------

	/// <summary>The desk switched to a tab, by any route (a tab, SHOW ME, an office object, NEXT UP).</summary>
	public static void TabOpened(string title) {
		var t = Current; if (t == null || t.finished || string.IsNullOrEmpty(title)) return;
		if (title == t.currentTab && t.tabsOpened.Count > 0) return;
		t.currentTab = title;
		t.tabsOpened.Add((title, t.log.Count));
	}

	/// <summary>NEXT UP is on the desk with this line. Each distinct line is kept once, with when it first showed.</summary>
	public static void NextUpShown(string line) {
		var t = Current; if (t == null || t.finished || string.IsNullOrEmpty(line)) return;
		if (t.nextUpLines.Count > 0 && t.nextUpLines[^1].Line == line) return;
		if (t.nextUpLines.Any(l => l.Line == line)) return;
		t.nextUpLines.Add((line, t.log.Count, Now()));
	}

	/// <summary>The desk refused the action just taken (a red Post-it).</summary>
	public static void Refused(string message) {
		var t = Current; if (t == null || t.finished) return;
		t.lastRefusal = message; t.refusals++;
	}

	/// <summary>The first-week card was put away ("I know the way").</summary>
	public static void CardPutAway() {
		var t = Current; if (t == null || t.finished || t.putAway) return;
		t.putAway = true; t.Milestone("Card put away"); t.Write();
	}

	// ---- presses -------------------------------------------------------------------------------------------------------

	private void OnNodeAdded(Node node) { nodeChurn++; if (node is BaseButton b) Wire(b); }
	private void OnNodeRemoved(Node node) => nodeChurn++;
	private void OnDeskChanged() => deskChanges++;

	private void Wire(BaseButton button) {
		if (button.HasMeta("first_session_wired")) return;
		button.SetMeta("first_session_wired", true);
		button.Pressed += () => OnPressed(button);
		button.GuiInput += e => {
			if (button.Disabled && e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) OnDisabledClick(button);
		};
	}

	private static string NameOf(BaseButton b) {
		string text = (b as Button)?.Text;
		if (string.IsNullOrWhiteSpace(text)) text = b.TooltipText;
		if (string.IsNullOrWhiteSpace(text)) text = "[" + b.Name + "]";
		text = string.Join(" ", text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
		return text.Length > 70 ? text[..67] + "..." : text;
	}

	// The state as the last frame left it. A press's own handlers run before this one (they were connected first), so
	// "before the press" has to be taken ahead of input, not when the Pressed signal reaches the trace.
	private int frameClock, frameChanges, frameChurn, frameRefusals;
	private string frameWhen = "", frameTab = "A&R";

	public override void _Process(double delta) {
		frameClock = GameMinutes(); frameChanges = deskChanges; frameChurn = nodeChurn; frameRefusals = refusals;
		frameWhen = Now(); frameTab = currentTab;
	}

	private async void OnPressed(BaseButton button) {
		if (finished || !IsInsideTree()) return;
		var record = new PressRecord {
			N = log.Count + 1, Seconds = Elapsed(), When = frameWhen, Tab = frameTab, Text = NameOf(button),
			Glow = button.HasMeta("first_week_glow"),
		};
		log.Add(record);
		int clockBefore = frameClock, changesBefore = frameChanges, churnBefore = frameChurn, refusalsBefore = frameRefusals;
		// A button that only shows or hides something of the desk's own (the voice slip's ×) says so: hiding moves no node.
		bool toggle = button.ToggleMode || button.HasMeta("first_session_ok");
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		if (!IsInsideTree()) return;
		record.Outcome = refusals > refusalsBefore ? "refused: " + lastRefusal
			: toggle || GameMinutes() != clockBefore || deskChanges != changesBefore || nodeChurn != churnBefore ? "ok"
			: "no visible change";
		CheckSteps();
		if (log.Count % 10 == 0) Write();
	}

	private void OnDisabledClick(BaseButton button) {
		if (finished) return;
		string why = string.IsNullOrWhiteSpace(button.TooltipText) ? "" : " (" + string.Join(" ", button.TooltipText.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)) + ")";
		log.Add(new PressRecord { N = log.Count + 1, Seconds = Elapsed(), When = Now(), Tab = currentTab, Text = NameOf(button), Outcome = "disabled" + why });
	}

	// ---- milestones ----------------------------------------------------------------------------------------------------

	private void CheckSteps() {
		var desk = PlayerDesk.Instance;
		if (desk?.Label == null || finished) return;
		foreach (var step in PlayerDesk.FirstWeekSteps)
			if (!stepsSeen.Contains(step) && desk.FirstWeekDone(step)) {
				stepsSeen.Add(step);
				Milestone("✓ " + PlayerDesk.FirstWeekTitle(step));
				Write();
			}
		if (desk.FirstWeekDone(FirstWeekStep.Release)) Finish("First single released");
		else if (desk.IsGameOver) Finish("Game over");
		else if (GameMinutes() >= MaxInGameDays * 24 * 60) Finish($"{MaxInGameDays} in-game days");
	}

	private void Finish(string why) {
		if (finished) return;
		Milestone("End: " + why);
		finished = true;
		Write();
		GD.Print("First-session trace written: " + SummaryPath);
	}

	private void Milestone(string what) => milestones.Add((what, log.Count(p => p.Outcome != "disabled"), Now(), Elapsed(), GameMinutes()));

	// ---- clocks --------------------------------------------------------------------------------------------------------

	private double Elapsed() => (Time.GetTicksMsec() - startMs) / 1000.0;

	/// <summary>In-game minutes since the trace started.</summary>
	private int GameMinutes() {
		var clock = TimeManager.Instance;
		if (clock == null) return 0;
		var d = clock.CurrentDate;
		int days = (int)(new DateTime(d.year, d.month, d.day) - new DateTime(startDate.year, startDate.month, startDate.day)).TotalDays;
		return days * 24 * 60 + clock.CurrentHour * 60 + clock.CurrentMinute - startMinuteOfDay;
	}

	private static string Now() {
		var clock = TimeManager.Instance;
		return clock == null ? "" : $"{clock.CurrentDate.ShortMonthName} {clock.CurrentDate.day} {clock.CurrentHour:00}:{clock.CurrentMinute:00}";
	}

	private static string Real(double seconds) => $"{(int)(seconds / 60)}m{(int)(seconds % 60):00}s";
	private static string Game(int minutes) => minutes < 24 * 60 ? $"{minutes / 60}h{minutes % 60:00}" : $"{minutes / (24 * 60)}d {minutes % (24 * 60) / 60}h";

	// ---- the files -----------------------------------------------------------------------------------------------------

	private void Write() {
		try {
			System.IO.File.WriteAllText(SummaryPath, Summary());
			var sb = new StringBuilder("#\treal\tin game\ttab\tglow\tbutton\toutcome\n");
			foreach (var p in log) sb.Append($"{p.N}\t{Real(p.Seconds)}\t{p.When}\t{p.Tab}\t{(p.Glow ? "*" : "")}\t{p.Text}\t{p.Outcome}\n");
			System.IO.File.WriteAllText(filePrefix + "-presses.tsv", sb.ToString());
		} catch (Exception e) {
			GD.PushWarning("First-session trace: couldn't write " + filePrefix + ": " + e.Message);
		}
	}

	public string Summary() {
		var desk = PlayerDesk.Instance;
		var sb = new StringBuilder();
		sb.AppendLine("FIRST SESSION TRACE");
		if (desk?.Label != null)
			sb.AppendLine($"{desk.Label.labelName} · {desk.Label.headquartersCity} · {desk.Archetype}" + (desk.Lien != null ? $" · holder {desk.Lien.Holder}" : ""));
		sb.AppendLine($"Started {startReal:yyyy-MM-dd HH:mm} real time; in game {startDate.ToHeadlineString()} {startMinuteOfDay / 60:00}:{startMinuteOfDay % 60:00}.");
		if (resumed) sb.AppendLine("RESUMED from a saved game part-way through the first week: presses and time before the load aren't counted.");
		sb.AppendLine(finished ? "Finished." : "Still running when written.");
		sb.AppendLine();

		sb.AppendLine("MILESTONES  (presses so far · real time · in-game time since the start)");
		foreach (var m in milestones) sb.AppendLine($"  {m.Press,4} presses · {Real(m.Seconds),8} · {Game(m.GameMinutes),8} · {m.When,-12}  {m.What}");
		sb.AppendLine();

		var real = log.Where(p => p.Outcome != "disabled").ToList();
		int refused = real.Count(p => p.Outcome.StartsWith("refused")), quiet = real.Count(p => p.Outcome == "no visible change");
		int disabled = log.Count - real.Count;
		sb.AppendLine("PRESSES");
		sb.AppendLine($"  {real.Count} presses; {real.Count(p => p.Glow)} on the glowing button; {real.Count(p => p.Text == "SHOW ME")} SHOW ME.");
		sb.AppendLine($"  Dead clicks: {refused + quiet + disabled} ({refused} refused, {quiet} with no visible change, {disabled} on greyed buttons).");
		sb.AppendLine();

		sb.AppendLine("TABS OPENED  (in order; the press count when it opened)");
		foreach (var g in tabsOpened.GroupBy(t => t.Tab))
			sb.AppendLine($"  {g.Key,-13} ×{g.Count(),-3} first at press {g.First().Press}");
		sb.AppendLine();

		sb.AppendLine("NEXT UP LINES SHOWN");
		if (nextUpLines.Count == 0) sb.AppendLine("  none (the first-week card was up)");
		foreach (var l in nextUpLines) sb.AppendLine($"  press {l.Press,4} · {l.When,-12}  {l.Line}");
		sb.AppendLine();

		sb.AppendLine("DEAD CLICKS");
		foreach (var p in log.Where(p => p.Outcome is not ("ok" or "")))
			sb.AppendLine($"  #{p.N,-4} {p.When,-12} {p.Tab,-12} {p.Text}  →  {p.Outcome}");
		sb.AppendLine();
		sb.AppendLine("Every press is in the -presses.tsv file beside this one.");
		return sb.ToString();
	}

	private static IEnumerable<Node> Nodes(Node root) {
		yield return root;
		foreach (Node child in root.GetChildren()) foreach (var n in Nodes(child)) yield return n;
	}
}
