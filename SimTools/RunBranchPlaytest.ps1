[CmdletBinding()]
param(
    [string]$Godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64.exe',
    # Opt-in extras, e.g. -Extra '--media-demand-press','--media-ai-performer-fees'
    [string[]]$Extra = @(),
    [switch]$NoBuild
)
# Plays the ordinary game (the lp-retail-v2 profile is applied automatically to a windowed game) with this branch's
# configuration from branch_playtest_flags.txt. Logs go to SimLogs/playtest/<timestamp>.log.
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
    $log = Join-Path $logDir ("branch-playtest-" + (Get-Date -Format 'yyyyMMdd-HHmmss') + ".log")
    $gameArgs = @('--path', '.', '--log-file', $log, '--') + $flags
    Write-Host "Launching with $($flags.Count) flags; log: $log"
    Start-Process -FilePath $Godot -ArgumentList $gameArgs -WorkingDirectory $root
} finally { Pop-Location }
