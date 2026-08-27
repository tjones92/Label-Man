# Headless Godot Execution Note

## The command

Run from the repository root. **Build first** — Godot loads the compiled assembly
from `.godot/mono/temp/bin/Debug/`, not your source tree, so an unbuilt change is
silently a stale run.

```powershell
dotnet build -c Debug "Label Man.sln"
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=<prefix> --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle > SimLogs\<prefix>-console.log 2>&1
```

- Use the **`_console.exe`** in Downloads, not the windowed exe.
- The `--` separator is required; everything after it goes to the runner.
- A valid decade/comparison run **requires** `--enable-genre-market-v2
  --enable-artist-population-lifecycle`. Without them the sim is roughly half
  the size and is not comparable to any recorded baseline.
- Use a unique `--run=<prefix>` every attempt; never overwrite an artifact family.
- **Redirect to a file. Do not pipe** a long run into `grep`/`tail`/`Select-String` —
  it hangs at ~1% CPU when backgrounded.
- Long runs can outlive a command-wrapper timeout. If the wrapper times out, check
  the Godot child process and the tail of `SimLogs/<prefix>-weeks.csv` before
  concluding failure. Do not relaunch while that child is still alive.
- Success requires: process exit, the expected completed week count, a
  `CHART_AUDIT_COMPLETE` marker when console output is retained, and the audit
  artifacts. The post-completion `MissingSingletonsTemp.cs` autoload diagnostic is
  non-fatal *only* when those conditions hold.

## Failure signature: "Cannot instantiate C# script"

If a run dies immediately with a wall of

```
ERROR: Failed to instantiate an autoload, script 'res://Systems/TimeManager.cs' does not inherit from 'Node'.
ERROR: Cannot instantiate C# script because the associated class could not be found. Script: 'res://Data/GenrePreference.cs'.
```

for **every** script in the project, the problem is not your branch, not the
`.tscn`, and not a corrupt DLL. It means `Godot.SourceGenerators` did not run
during the build.

That generator is what stamps `[ScriptPath("res://Systems/ChartManager.cs")]` onto
each class. It is how Godot maps a `.cs` resource to a CLR type. Without it the
build still succeeds with 0 errors and produces a normal-looking assembly that
Godot loads fine — and then cannot match a single script to a class. It is not
optional and there is no flag to work around it.

**Diagnose in two commands.** The build warning is the proof:

```bash
dotnet build -c Debug "Label Man.csproj" -v n 2>&1 | grep -i "CS8034\|SourceGenerators"
```

A healthy build prints nothing. A broken one prints:

```
CSC : warning CS8034: Unable to load Analyzer assembly
  ...\godot.sourcegenerators\4.7.0\analyzers\dotnet\cs\Godot.SourceGenerators.dll
  : Unable to load Godot.SourceGenerators
```

Confirm on the artifact itself — a good assembly contains the script paths, a bad
one contains none:

```bash
grep -a -o "res://[A-Za-z0-9_/.-]*\.cs" ".godot/mono/temp/bin/Debug/Label Man.dll" | sort -u | wc -l
```

Expect ~200. If it prints `0`, the generator did not run. (`data_Label
Man_windows_x86_64/Label Man.dll`, the Aug 14 export, is a known-good reference.)

**Known cause: Smart App Control.** Windows 11 SAC is a kernel code-integrity
policy that refuses to load unsigned binaries. `Godot.SourceGenerators.dll` ships
from NuGet unsigned, so when SAC is enforcing, `csc`/`VBCSCompiler` cannot load it
and downgrades to a warning instead of an error. Check for it:

```powershell
(Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy').VerifiedAndReputablePolicyState
# 0 = Off, 1 = Enforced, 2 = Evaluation
Get-WinEvent -FilterHashtable @{LogName='Microsoft-Windows-CodeIntegrity/Operational'; Id=3077} -MaxEvents 5 | Format-List TimeCreated, Message
```

Event 3077 naming `Godot.SourceGenerators.dll` is conclusive.

**Why it can start "suddenly" after weeks of working runs.** SAC's verdict on an
unsigned file comes from a cloud reputation lookup that is cached, and the cache
does not survive a reboot. On 2026-08-26 the machine rebooted at 19:10 and the
first block landed at 19:38 — nothing in the toolchain, the branch, or the way the
run was launched had changed. The same policy had already done this once before to
a different file: `GodotSharp.dll` was blocked 37 times between 8/5 and 8/18 and
then quietly stopped. Expect it to be able to recur, and expect it to look like
whatever you happened to be editing that day. This is an
environmental fault: it reproduces on a stashed-clean tree, on any branch, at any
commit, because it is a property of the machine and not the code. Do not go
looking for it in the diff, and do not delete `.godot/` or clear the NuGet cache
chasing it.

The fix is a Windows Security setting and belongs to the human, not to the agent:
Windows Security → App & browser control → Smart App Control settings → **Off**.
Note for whoever asks: turning SAC off is **one-way** — it cannot be re-enabled
without resetting or reinstalling Windows.

Things that are *not* the cause, all checked and ruled out on 2026-08-26:
the .NET SDK version (fails identically pinned to 8.0.423 and on 10.0.301), a
corrupt or truncated generator DLL (intact, 119,296 bytes, valid PE), a
Mark-of-the-Web `Zone.Identifier` stream (absent), Defender quarantine (no
detections), ASR rules (none configured), disk space, and the Roslyn analyzer
shadow-copy cache (copy succeeds; the *load* is what is refused).
