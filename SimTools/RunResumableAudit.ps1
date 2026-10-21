param(
    [Parameter(Mandatory=$true)][string]$Tag,
    [Parameter(Mandatory=$true)][string]$OutputDir,
    # The run's own arguments, including the TOTAL --weeks and --seed; no --run, --checkpoint-world or --resume-world.
    [Parameter(Mandatory=$true)][string[]]$AuditArgs,
    [int]$ManagedMB=2560,
    # Ask the run to stop (finish the week, flush, exit 3) once free RAM stays below this for LowSeconds.
    [int]$ReserveMB=640,
    [int]$LowSeconds=20,
    # Start or restart only when this much is free.
    [int]$StartMB=1400,
    # If a stopping run has not exited and free RAM stays below this, kill it outright.
    [int]$HardFloorMB=250,
    [int]$StopGraceSeconds=300,
    # Wait this long (polling each minute) for memory to come back before giving up; rerunning the script continues.
    [int]$MaxWaitMinutes=720,
    # Test only: ask segment 0 to stop once it has written a checkpoint at or after this audit week.
    [int]$TestStopAfterCheckpointWeek=0
)
# Restartable ChartAuditRunner launcher. Segment 0 runs as <Tag> with --checkpoint-world=<Tag>, which writes a world
# at the end of every simulated year. When memory runs low the run is asked to stop; once memory is back the run
# continues from the last checkpoint as <Tag>_s<k> (--continue-checkpoint: exact stream, continuous week numbers),
# and SimTools/stitch_resumed_run.py folds each continuation back into <Tag>'s CSVs. State lives in
# <OutputDir>\<Tag>-resume.json, so rerunning this script after it gives up, or after a reboot, picks up where it was.
$ErrorActionPreference='Stop'
$root='C:\Project\Label-Man'
$godot='C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
$python=Join-Path $env:LOCALAPPDATA 'Programs\Python\Python314\python.exe'
$worlds=Join-Path $root 'SimLogs\worlds'
New-Item -ItemType Directory -Force $OutputDir | Out-Null
if (!("ResumableAuditMemory" -as [type])) {
    Add-Type -TypeDefinition 'using System; using System.Runtime.InteropServices; public static class ResumableAuditMemory { [StructLayout(LayoutKind.Sequential)] public struct Status { public uint Length; public uint Load; public ulong TotalPhysical; public ulong AvailablePhysical; public ulong TotalPageFile; public ulong AvailablePageFile; public ulong TotalVirtual; public ulong AvailableVirtual; public ulong AvailableExtendedVirtual; } [DllImport("kernel32.dll", SetLastError=true)] public static extern bool GlobalMemoryStatusEx(ref Status status); public static long AvailableMB() { Status s = new Status(); s.Length = (uint)Marshal.SizeOf<Status>(); if (!GlobalMemoryStatusEx(ref s)) throw new Exception("Memory status unavailable"); return (long)(s.AvailablePhysical / 1048576UL); }}'
}
$statePath=Join-Path $OutputDir "$Tag-resume.json"
$state=if (Test-Path $statePath) { Get-Content $statePath -Raw | ConvertFrom-Json } else { [pscustomobject]@{tag=$Tag;done=$false;segments=@();checkpoints=@();resumeFrom=$null} }
function Save-State { $state | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $statePath }
function Checkpoints-In([string]$log) {
    if (!(Test-Path $log)) { return @() }
    @(Select-String -LiteralPath $log -Pattern 'CHART_AUDIT_CHECKPOINT name=\S+ auditWeek=(\d+) date=(\d+)/(\d+)/(\d+)' | ForEach-Object {
        [pscustomobject]@{week=[int]$_.Matches[0].Groups[1].Value; year=[int]$_.Matches[0].Groups[4].Value} })
}
function Stitch($cont, $from, $until) {
    $stitchArgs=@('-I', (Join-Path $root 'SimTools\stitch_resumed_run.py'), (Join-Path $root "SimLogs\$Tag"), (Join-Path $root "SimLogs\$cont"),
        '--checkpoint-week', $from.week, '--checkpoint-year', $from.year)
    if ($until) { $stitchArgs+=@('--until-week', $until.week, '--until-year', $until.year) }
    & $python @stitchArgs
    if ($LASTEXITCODE -ne 0) { throw "Stitching $cont failed." }
}

while (-not $state.done) {
    $k=@($state.segments).Count
    $deadline=(Get-Date).AddMinutes($MaxWaitMinutes)
    while ([ResumableAuditMemory]::AvailableMB() -lt $StartMB -or (Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$')) {
        if ((Get-Date) -gt $deadline) { Save-State; throw "Waited $MaxWaitMinutes min for $StartMB MB free; rerun this script to continue $Tag." }
        Start-Sleep -Seconds 60
    }
    $run=if ($k -eq 0) { $Tag } else { "${Tag}_s$k" }
    $from=$state.resumeFrom
    $runArgs=@($AuditArgs)+@("--checkpoint-world=$Tag","--run=$run")
    if ($k -gt 0) { $runArgs+=@("--resume-world=$($from.world)",'--continue-checkpoint') }
    $env:LABELMAN_WORKSTATION_GC='1'; $env:DOTNET_gcServer='0'; $env:DOTNET_GCHeapHardLimit=([long]$ManagedMB*1MB).ToString('X')
    $arguments=@('--headless','--path','.','SimTools/ChartAuditRunner.tscn','--')+$runArgs
    $console=Join-Path $OutputDir "$run-console.log"
    $segment=[ordered]@{run=$run;arguments=$arguments;startedUtc=[datetime]::UtcNow.ToString('o');resumedFrom=$from;
        assemblySha256=(Get-FileHash -LiteralPath (Join-Path $root '.godot\mono\temp\bin\Debug\Label Man.dll')).Hash;
        minimumAvailableMB=[ResumableAuditMemory]::AvailableMB();peakWorkingSetMB=0;stopRequested=$false;killed=$false;exitCode=$null}
    $process=Start-Process -FilePath $godot -ArgumentList $arguments -WorkingDirectory $root -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput $console -RedirectStandardError (Join-Path $OutputDir "$run-errors.log")
    $null=$process.Handle
    $low=0; $critical=0; $stopAt=$null
    # Loading a checkpoint and the first week spike memory briefly; low samples count only once the run is under way.
    $settled=$false
    while (!$process.WaitForExit(1000)) {
        $available=[ResumableAuditMemory]::AvailableMB()
        $segment.minimumAvailableMB=[math]::Min($segment.minimumAvailableMB,$available)
        $ws=(Get-Process -Name 'Godot*' -ErrorAction SilentlyContinue | Measure-Object -Property WorkingSet64 -Sum).Sum
        $segment.peakWorkingSetMB=[math]::Max($segment.peakWorkingSetMB,[math]::Ceiling($ws/1MB))
        if (-not $settled) { $settled=[bool](Select-String -LiteralPath $console -Pattern '^Advanced to' -Quiet -ErrorAction SilentlyContinue) }
        if (-not $segment.stopRequested) {
            if ($settled -and $available -lt $ReserveMB) { $low++ } else { $low=0 }
            if ($TestStopAfterCheckpointWeek -gt 0 -and $k -eq 0 -and @(Checkpoints-In $console | Where-Object { $_.week -ge $TestStopAfterCheckpointWeek }).Count -gt 0) { $low=$LowSeconds }
            if ($low -ge $LowSeconds) { Set-Content -LiteralPath (Join-Path $worlds "$Tag.stop") 'stop'; $segment.stopRequested=$true; $stopAt=Get-Date }
        } else {
            if ($available -lt $HardFloorMB) { $critical++ } else { $critical=0 }
            if ($critical -ge 10 -or ((Get-Date) - $stopAt).TotalSeconds -gt $StopGraceSeconds) {
                & taskkill.exe /PID $process.Id /T /F | Out-Null; $process.WaitForExit(); $segment.killed=$true; break
            }
        }
    }
    $segment.exitCode=$process.ExitCode
    $segment.finishedUtc=[datetime]::UtcNow.ToString('o')
    Remove-Item -LiteralPath (Join-Path $worlds "$Tag.stop") -ErrorAction SilentlyContinue
    $written=@(Checkpoints-In $console)
    $completed=-not $segment.killed -and $segment.exitCode -eq 0
    $stopped=-not $segment.killed -and $segment.exitCode -eq 3
    if (-not ($completed -or $stopped -or $segment.killed)) { $state.segments=@($state.segments)+[pscustomobject]$segment; Save-State; throw "$run exited with $($segment.exitCode); see $console." }
    $all=@($state.checkpoints)+$written
    $until=$null
    if ($completed) { $next=$null }
    elseif ($written.Count -eq 0) { $next=$from }   # nothing new is whole: resume where this segment started
    elseif ($segment.killed) {
        # A killed run's newest checkpoint may lack that year's annual rows: fall back to the one before it.
        if ($all.Count -lt 2) { $next=$null } else {
            $prev=$all[$all.Count-2]; $until=$prev
            $next=[pscustomobject]@{world="$Tag-prev";week=$prev.week;year=$prev.year}
        }
    } else { $last=$written[$written.Count-1]; $next=[pscustomobject]@{world=$Tag;week=$last.week;year=$last.year} }
    if (-not $completed -and $null -eq $next) {
        # Stopped before any usable checkpoint: segment 0 starts over from 1960.
        $state=[pscustomobject]@{tag=$Tag;done=$false;segments=@();checkpoints=@();resumeFrom=$null}
        Save-State
        "{0}: stopped before a usable checkpoint; starting over" -f $run
        continue
    }
    if ($k -gt 0 -and ($completed -or $written.Count -gt 0)) { Stitch $run $from $until }
    $state.checkpoints=$all
    $state.segments=@($state.segments)+[pscustomobject]$segment
    $state.resumeFrom=$next
    $state.done=$completed
    Save-State
    "{0}: {1}" -f $run, $(if ($completed) { 'complete' } elseif ($stopped) { "stopped, resuming from audit week $($next.week)" } else { "killed, resuming from audit week $($next.week)" })
}
