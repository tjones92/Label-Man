param([ValidateSet(1001,1002)][int]$Seed=1001,[Parameter(Mandatory=$true)][string]$RunTag,[ValidateRange(1,209)][int]$Weeks=209,[switch]$Resume)
$ErrorActionPreference='Stop'
$folkEasyRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location -LiteralPath $folkEasyRoot
try {
 if($RunTag -notmatch '^[a-zA-Z0-9-]+$'){throw 'Invalid run tag'}
 $folkEasyRuns=@();$folkEasyRemaining=$Weeks;$folkEasySlice=0;$folkEasySnapshot=$null
 $folkEasyManifest="SimLogs/folk-easy-$RunTag-$Seed-runs.json"
 if($Resume) {
  if(-not(Test-Path -LiteralPath $folkEasyManifest)){throw 'No saved trajectory manifest to resume'}
  $folkEasyRuns=@(Get-Content -LiteralPath $folkEasyManifest -Raw | ConvertFrom-Json)
  $folkEasyRemaining=$Weeks-($folkEasyRuns | Measure-Object -Property weeks -Sum).Sum
  if($folkEasyRemaining -lt 0){throw 'Requested window is shorter than retained progress'}
  $folkEasySlice=$folkEasyRuns[-1].slice;$folkEasySnapshot=$folkEasyRuns[-1].snapshot
 }
 while($folkEasyRemaining -gt 0) {
  $folkEasySlice++;if($folkEasySlice -gt 24){throw 'Exceeded bounded slices; partial evidence retained'}
  $folkEasyRun="folk-easy-$RunTag-$Seed-s$folkEasySlice"
  $folkEasyRunBase=$folkEasyRun;$folkEasyRetry=0
  while(Test-Path -LiteralPath "SimLogs/$folkEasyRun-invocation.json") {
   if((Test-Path -LiteralPath "SimLogs/$folkEasyRun-polar-checkpoint.json.gz") -and
     (Select-String -LiteralPath "SimLogs/$folkEasyRun.log" -Pattern 'CHART_AUDIT_(COMPLETE|STOPPED)' -Quiet)){break}
   $folkEasyRetry++;if($folkEasyRetry -gt 3){throw 'Three failed attempts retained; inspect the cause'}
   $folkEasyRun="$folkEasyRunBase-retry$folkEasyRetry"
  }
  $folkEasyArgs=@{Seed=$Seed;Run=$folkEasyRun;Weeks=$folkEasyRemaining;Census='none';MaxSeconds=180;ShapeVariation='on';ShapeVariationV2='on';FolkEasyTrajectory=$true}
  if($folkEasySnapshot){$folkEasyArgs.LoadSnapshot=$folkEasySnapshot;$folkEasyArgs.VerifySnapshot=$true}
  if(-not(Test-Path -LiteralPath "SimLogs/$folkEasyRun-invocation.json")){& "$PSScriptRoot/run-polar-research.ps1" @folkEasyArgs}
  $folkEasySnapshot="SimLogs/$folkEasyRun-polar-checkpoint.json.gz"
  $folkEasyState=& node "$PSScriptRoot/read-polar-snapshot-metadata.mjs" $folkEasySnapshot | ConvertFrom-Json
  if($LASTEXITCODE -ne 0 -or $folkEasyState.weeks -le 0 -or $folkEasyState.weeks -gt $folkEasyRemaining -or $folkEasyState.shape -ne 2){throw 'Invalid trajectory progress'}
  $folkEasyRemaining-=$folkEasyState.weeks
  $folkEasyRuns+=@{run=$folkEasyRun;seed=$Seed;slice=$folkEasySlice;date=$folkEasyState.date;weeks=$folkEasyState.weeks;remaining=$folkEasyRemaining;snapshot=$folkEasySnapshot}
  $folkEasyRuns | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $folkEasyManifest
  Write-Output "$folkEasyRun date $($folkEasyState.date -join '-') remaining $folkEasyRemaining"
 }
} finally {Pop-Location}
