[CmdletBinding()]
param(
    [string]$Godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe',
    # Opt-in extras, e.g. -Extra '--media-demand-controversy'
    [string[]]$Extra = @(),
    [switch]$NoBuild
)
# Plays the ordinary game with branch_playtest_flags.txt. Since media-atmosphere-v3 became the default profile
# (Systems/SimulationArguments.cs) those flags are already applied to any windowed game; the file now matters for
# -Extra trials and for a run name (the audition log). Logs go to SimLogs/playtest/<timestamp>.log.
$ErrorActionPreference = 'Stop'
$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location -LiteralPath $root
try {
    if (-not $NoBuild) {
        dotnet build 'Label Man.sln' -c Debug
        if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    }
    $flags = @(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'branch_playtest_flags.txt') |
        ForEach-Object { $_.Trim() } | Where-Object { $_ -and !$_.StartsWith('#') }) + $Extra
    $logDir = Join-Path $root 'SimLogs/playtest'
    New-Item -ItemType Directory -Force -Path $logDir | Out-Null
    $runName = "branch-playtest-" + (Get-Date -Format 'yyyyMMdd-HHmmss')
    $log = Join-Path $logDir ($runName + ".log")
    # --response-auditions (in the decade set) needs a plain run name for its SimLogs/<run>-response-auditions.jsonl.
    $gameArgs = @('--path', '.', '--log-file', $log, '--', "--run=$runName") + $flags
    Write-Host "Launching with $($flags.Count) flags; log: $log"
    Start-Process -FilePath $Godot -ArgumentList $gameArgs -WorkingDirectory $root
} finally { Pop-Location }
