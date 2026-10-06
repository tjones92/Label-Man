param([int[]]$Seeds=@(1001,1002),[string]$RunTag='v2',[string]$SnapshotTag='easy-jazz-v2')
$ErrorActionPreference='Stop'
# Matched census for the Easy recent-hit channel and the Jazz screen opportunity. The reference
# disables only those two channels (same world, same private census RNG). The Jazz instrumental
# lineups are a world change and are present on both sides; snapshots come from a fresh trajectory.
foreach($followUpSeed in $Seeds) {
 if($followUpSeed -notin @(1001,1002)){throw 'Additional seeds require an explicit instability justification.'}
 $followUpRemaining=if($followUpSeed -eq 1001){22}else{18}
 $followUpManifest=Get-Content -LiteralPath "SimLogs/folk-easy-$SnapshotTag-$followUpSeed-runs.json" -Raw | ConvertFrom-Json
 $followUpSnapshot=@($followUpManifest)[-1].snapshot
 $followUpDates=@()
 for($followUpAttempt=0;$followUpAttempt -le 5;$followUpAttempt++) {
  $followUpRun="easy-jazz-$RunTag-matched-$followUpSeed"+$(if($followUpAttempt){"-resume$followUpAttempt"}else{''})
  $followUpSliceWeeks=if($followUpAttempt -eq 0){if($followUpSeed -eq 1001){13}else{9}}else{$followUpRemaining}
  & "$PSScriptRoot/run-polar-research.ps1" -Seed $followUpSeed -Run $followUpRun -Weeks $followUpSliceWeeks -Census sample -SamplePerStratum 12 -MaxSeconds 900 -ShapeVariationV2 on -EasyJazzFollowUpCensus -CensusFromYear 1963 -CensusMonths @(1,2) -LoadSnapshot $followUpSnapshot -VerifySnapshot
  $followUpRunManifest=Get-Content -LiteralPath "SimLogs/$followUpRun-polar-research.json" -Raw | ConvertFrom-Json
  $followUpDates+=@($followUpRunManifest.months | Where-Object Status -eq 'complete' | ForEach-Object Date)
  if(@($followUpDates | Select-Object -Unique).Count -ge 2){break}
  if($followUpAttempt -eq 5){throw 'Incomplete matched census; evidence retained.'}
  $followUpSnapshot="SimLogs/$followUpRun-polar-checkpoint.json.gz"
  $followUpMetadata=& node "$PSScriptRoot/read-polar-snapshot-metadata.mjs" $followUpSnapshot | ConvertFrom-Json
  if($LASTEXITCODE -ne 0){throw 'Checkpoint metadata failed'}
  $followUpRemaining=[Math]::Max(1,$followUpRemaining-$followUpMetadata.weeks)
 }
}
