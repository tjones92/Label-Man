param(
 [string]$Run = 'polar-research-kill-check-1001',
 [string]$Godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference = 'Stop'
$polarKillRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ($Run -notmatch '^[a-zA-Z0-9-]+$') { throw 'Invalid run name' }
if (Get-ChildItem -LiteralPath (Join-Path $polarKillRoot 'SimLogs') -Filter "$Run*") { throw 'Existing artifacts' }
$polarKillArgs = @('--headless','--path',"`"$polarKillRoot`"",'SimTools/ChartAuditRunner.tscn','--','--seed=1001',"--run=$Run",'--weeks=2','--enable-genre-market-v2','--enable-artist-population-lifecycle','--calibration','--polar-final-audit','--polar-followup-audit','--polar-census=full','--polar-diagnostic-acts=2','--polar-max-seconds=120')
[ordered]@{seed=1001;weeks=2;run=$Run;census='full';arguments=$polarKillArgs;assemblySha256=(Get-FileHash (Join-Path $polarKillRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash;purpose='Forced interruption of this test process after at least 32 flushed census observations'} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $polarKillRoot "SimLogs/$Run-invocation.json")
$polarKillProcess = Start-Process -FilePath $Godot -ArgumentList $polarKillArgs -WorkingDirectory $polarKillRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $polarKillRoot "SimLogs/$Run.log") -RedirectStandardError (Join-Path $polarKillRoot "SimLogs/$Run.stderr.log")
$polarKillWatch = [Diagnostics.Stopwatch]::StartNew()
$polarKilled = $false
try {
 while (-not $polarKillProcess.HasExited -and $polarKillWatch.Elapsed.TotalSeconds -lt 90) {
  $polarManifestPath = Join-Path $polarKillRoot "SimLogs/$Run-polar-research.json"
  if (Test-Path -LiteralPath $polarManifestPath) {
   $polarManifest = Get-Content -LiteralPath $polarManifestPath -Raw | ConvertFrom-Json
   if ($polarManifest.months.Count -gt 0 -and $polarManifest.months[-1].Status -eq 'inProgress' -and $polarManifest.months[-1].Observed -ge 32) {
    # Only the child returned by Start-Process belongs to this interruption fixture.
    Stop-Process -Id $polarKillProcess.Id -Force
    $polarKilled = $true
    break
   }
  }
  Start-Sleep -Milliseconds 200
 }
 if (-not $polarKilled) { throw 'Did not reach the expected mid-census interruption point within 90 seconds.' }
 Write-Output "Killed fixture $Run after $($polarManifest.months[-1].Observed) flushed observations; pid=$($polarKillProcess.Id)."
} finally {
 if (-not $polarKillProcess.HasExited) { Stop-Process -Id $polarKillProcess.Id -Force }
}
