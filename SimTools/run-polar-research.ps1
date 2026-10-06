param(
 [ValidateRange(1,2147483647)][int]$Seed = 1001,
 [ValidateSet('on','off')][string]$Mode = 'on',
 [ValidateRange(1,209)][int]$Weeks = 2,
 [Parameter(Mandatory=$true)][string]$Run,
 [ValidateSet('sample','full','none')][string]$Census = 'none',
 [ValidateRange(1,100)][int]$SamplePerStratum = 4,
 [ValidateRange(0,100)][int]$DiagnosticActs = 0,
 [ValidateRange(1,900)][int]$MaxSeconds = 300,
 [ValidateRange(0,100000)][int]$StopAfterActs = 0,
 [string]$LoadSnapshot,
 [switch]$VerifySnapshot,
 [ValidateSet('on','off')][string]$ShapeVariation = 'on',
 [switch]$Directive2Trajectory,
 [ValidateSet('on','off')][string]$ShapeVariationV2 = 'off',
 [switch]$Directive3Trajectory,
 [switch]$Directive3AReplay,
 [switch]$FolkEasyTrajectory,
 [switch]$GenreFollowUpCensus,
 [switch]$GenreRepairCensus,
 [ValidateRange(1960,1963)][int]$CensusFromYear = 1960,
 [int[]]$CensusMonths = @(),
 [string]$Godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference = 'Stop'
$polarResearchRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location -LiteralPath $polarResearchRoot
try {
 if ($Run -notmatch '^[a-zA-Z0-9-]+$') { throw 'Invalid run name.' }
 if (Get-ChildItem -LiteralPath SimLogs -Filter "$Run*" -ErrorAction SilentlyContinue) { throw "Existing artifacts: $Run" }
 if ($Census -eq 'full' -and $Weeks -gt 8) { throw 'Full census is limited to eight weeks. Use sampling for longer windows.' }
 $polarResearchArgs = @('--headless','--path','.', 'SimTools/ChartAuditRunner.tscn','--',"--seed=$Seed","--run=$Run","--weeks=$Weeks",'--enable-genre-market-v2','--enable-artist-population-lifecycle','--calibration','--polar-final-audit','--polar-followup-audit',"--polar-census=$Census","--polar-sample-per-stratum=$SamplePerStratum","--polar-diagnostic-acts=$DiagnosticActs","--polar-max-seconds=$MaxSeconds")
 if ($Mode -eq 'off') { $polarResearchArgs += '--disable-polar-fit-selection' }
 if ($StopAfterActs -gt 0) { $polarResearchArgs += "--polar-stop-after-acts=$StopAfterActs" }
 if ($LoadSnapshot) { $polarResearchArgs += "--polar-load-snapshot=$([IO.Path]::GetFullPath($LoadSnapshot))" }
 if ($VerifySnapshot) { $polarResearchArgs += '--polar-verify-snapshot' }
 $polarResearchArgs += "--composition-shape-v1=$ShapeVariation"
 if ($Directive2Trajectory) { $polarResearchArgs += '--polar-directive2-trajectory' }
 $polarResearchArgs += "--composition-shape-v2=$ShapeVariationV2"
 if ($Directive3Trajectory) { $polarResearchArgs += @('--polar-directive2-trajectory','--polar-directive3-trajectory') }
 if ($Directive3AReplay) { $polarResearchArgs += @('--polar-directive3-a-replay','--polar-repair-legacy-world') }
 if ($FolkEasyTrajectory) { $polarResearchArgs += '--folk-easy-trajectory' }
 if ($GenreFollowUpCensus) { $polarResearchArgs += '--genre-followup-census' }
 if ($GenreRepairCensus) { $polarResearchArgs += @('--genre-followup-census','--genre-repertoire-repair-census') }
 $polarResearchArgs += "--polar-census-from-year=$CensusFromYear"
 if ($CensusMonths.Count) { $polarResearchArgs += "--polar-census-months=$($CensusMonths -join ',')" }
 $polarResearchWatch = [Diagnostics.Stopwatch]::StartNew()
 $polarInvocation = [ordered]@{seed=$Seed;mode=$Mode;weeks=$Weeks;run=$Run;census=$Census;samplePerStratum=$SamplePerStratum;diagnosticActs=$DiagnosticActs;maxSeconds=$MaxSeconds;arguments=$polarResearchArgs;startedUtc=[DateTime]::UtcNow.ToString('o');assemblySha256=(Get-FileHash '.godot/mono/temp/bin/Debug/Label Man.dll').Hash;tableSha256=(Get-FileHash Data/PolarSongTable.json).Hash;repertoireTableSha256=(Get-FileHash Data/PolarRepertoireTable.json).Hash}
 $polarInvocation | ConvertTo-Json -Depth 5 | Set-Content "SimLogs/$Run-invocation.json"
 $ErrorActionPreference = 'Continue'
 & $Godot @polarResearchArgs *> "SimLogs/$Run.log"
 $polarExit = $LASTEXITCODE
 $ErrorActionPreference = 'Stop'
 $polarInvocation.elapsedSeconds = $polarResearchWatch.Elapsed.TotalSeconds
 $polarInvocation.exitCode = $polarExit
 $polarInvocation | ConvertTo-Json -Depth 5 | Set-Content "SimLogs/$Run-invocation.json"
 if ($polarExit -ne 0) { throw "Failed audit $Run (exit $polarExit). See SimLogs/$Run.log" }
 if (Select-String -LiteralPath "SimLogs/$Run.log" -SimpleMatch "CHART_AUDIT_STOPPED run=$Run " -Quiet) {
  Write-Output "Stopped safely: $Run; checkpoint and partial evidence retained."
 } elseif (Select-String -LiteralPath "SimLogs/$Run.log" -SimpleMatch "CHART_AUDIT_COMPLETE run=$Run weeks=$Weeks" -Quiet) {
  Write-Output "Completed $Run in $($polarResearchWatch.Elapsed.TotalSeconds.ToString('F1')) seconds."
 } else { throw "Missing completion/stop marker: $Run" }
} finally { Pop-Location }
