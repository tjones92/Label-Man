# Headless Godot Execution Note

For future ChartAuditRunner simulation runs, use the Downloads console build of
Godot 4.7 Mono in headless mode:

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- <runner arguments>
```

Run from the repository root. Use a unique `--run=<prefix>` for every attempt;
never overwrite an existing artifact family.

Long runs can outlive a command-wrapper timeout. If the wrapper times out,
check the Godot child process and the tail of the matching `SimLogs/<prefix>-weeks.csv`
before concluding that the run failed. Do not relaunch while that child remains
active. A successful run still requires process exit, the expected completed
week count, a `CHART_AUDIT_COMPLETE` marker when console output is retained, and
the required audit artifacts. The known post-completion `MissingSingletonsTemp.cs`
autoload diagnostic is non-fatal only when those completion conditions are met.
