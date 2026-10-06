param([int[]]$Seeds=@(1001,1002),[string]$RunTag='v1')
$ErrorActionPreference='Stop'
foreach($genreRepairSeed in $Seeds) {
 if($genreRepairSeed -notin @(1001,1002)){throw 'Additional seeds require an explicit instability justification.'}
 $genreRepairRemaining=if($genreRepairSeed -eq 1001){22}else{18}
 $genreRepairSnapshot="SimLogs/folk-easy-bandleaders-v2-$genreRepairSeed-s5-polar-checkpoint.json.gz"
 $genreRepairDates=@()
 for($genreRepairAttempt=0;$genreRepairAttempt -le 5;$genreRepairAttempt++) {
  $genreRepairRun="easy-soundtrack-$RunTag-matched-$genreRepairSeed"+$(if($genreRepairAttempt){"-resume$genreRepairAttempt"}else{''})
  # One expensive census frame per invocation avoids spending the time budget
  # on a partial second month that would have to be repeated after resumption.
  $genreRepairSliceWeeks=if($genreRepairAttempt -eq 0){if($genreRepairSeed -eq 1001){13}else{9}}else{$genreRepairRemaining}
  & "$PSScriptRoot/run-polar-research.ps1" -Seed $genreRepairSeed -Run $genreRepairRun -Weeks $genreRepairSliceWeeks -Census sample -SamplePerStratum 12 -MaxSeconds 900 -ShapeVariationV2 on -EasySoundtrackCensus -CensusFromYear 1963 -CensusMonths @(1,2) -LoadSnapshot $genreRepairSnapshot -VerifySnapshot
  $genreRepairManifest=Get-Content -LiteralPath "SimLogs/$genreRepairRun-polar-research.json" -Raw | ConvertFrom-Json
  $genreRepairDates+=@($genreRepairManifest.months | Where-Object Status -eq 'complete' | ForEach-Object Date)
  if(@($genreRepairDates | Select-Object -Unique).Count -ge 2){break}
  if($genreRepairAttempt -eq 5){throw 'Incomplete matched census; evidence retained.'}
  $genreRepairSnapshot="SimLogs/$genreRepairRun-polar-checkpoint.json.gz"
  $genreRepairMetadata=& node "$PSScriptRoot/read-polar-snapshot-metadata.mjs" $genreRepairSnapshot | ConvertFrom-Json
  if($LASTEXITCODE -ne 0){throw 'Checkpoint metadata failed'}
  $genreRepairRemaining=[Math]::Max(1,$genreRepairRemaining-$genreRepairMetadata.weeks)
 }
}
