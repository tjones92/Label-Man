param([int[]]$Seeds=@(1001,1002,42001,42002,42003,42004),[ValidateSet('startup','late','baseline','matched')][string]$Phase='startup',[string]$RunTag='v1')
$ErrorActionPreference='Stop'
foreach($genreFollowUpSeed in $Seeds) {
 if($Phase -eq 'startup') {
  & "$PSScriptRoot/run-polar-research.ps1" -Seed $genreFollowUpSeed -Run "genre-followup-$RunTag-start-$genreFollowUpSeed" -Weeks 1 -Census sample -SamplePerStratum 12 -MaxSeconds 300 -ShapeVariationV2 on -GenreFollowUpCensus
 } elseif($Phase -eq 'matched') {
  if($genreFollowUpSeed -notin @(1001,1002)){throw 'Matched-window restore only needed for original seeds'}
  $genreFollowUpWeeks=if($genreFollowUpSeed -eq 1001){22}else{18}
  & "$PSScriptRoot/run-polar-research.ps1" -Seed $genreFollowUpSeed -Run "genre-followup-$RunTag-matched-$genreFollowUpSeed" -Weeks $genreFollowUpWeeks -Census sample -SamplePerStratum 12 -MaxSeconds 600 -ShapeVariationV2 on -GenreFollowUpCensus -CensusFromYear 1963 -CensusMonths @(1,2) -LoadSnapshot "SimLogs/folk-easy-bandleaders-v2-$genreFollowUpSeed-s5-polar-checkpoint.json.gz" -VerifySnapshot
 } elseif($Phase -eq 'baseline') {
  if($genreFollowUpSeed -notin @(1001,1002)){throw 'Baseline replay only has snapshots for 1001 and 1002'}
  & "$PSScriptRoot/run-polar-research.ps1" -Seed $genreFollowUpSeed -Run "genre-followup-$RunTag-baseline-$genreFollowUpSeed" -Weeks 1 -Census sample -SamplePerStratum 3 -MaxSeconds 300 -ShapeVariationV2 on -GenreFollowUpCensus -LoadSnapshot "SimLogs/folk-easy-bandleaders-v2-$genreFollowUpSeed-s9-polar-checkpoint.json.gz" -VerifySnapshot
 } elseif($genreFollowUpSeed -in @(1001,1002)) {
  foreach($genreFollowUpSlice in @(8,9)) {
   & "$PSScriptRoot/run-polar-research.ps1" -Seed $genreFollowUpSeed -Run "genre-followup-$RunTag-late-$genreFollowUpSeed-s$genreFollowUpSlice" -Weeks 1 -Census sample -SamplePerStratum 12 -MaxSeconds 600 -ShapeVariationV2 on -GenreFollowUpCensus -LoadSnapshot "SimLogs/folk-easy-bandleaders-v2-$genreFollowUpSeed-s$genreFollowUpSlice-polar-checkpoint.json.gz" -VerifySnapshot
  }
 } else {
  # Advance only as far as needed to retain January and February 1963 censuses.
  # Months before 1963 run ordinary simulation ticks without census/checkpoint work.
  & "$PSScriptRoot/run-polar-research.ps1" -Seed $genreFollowUpSeed -Run "genre-followup-$RunTag-late-$genreFollowUpSeed" -Weeks 166 -Census sample -SamplePerStratum 12 -MaxSeconds 900 -ShapeVariationV2 on -GenreFollowUpCensus -CensusFromYear 1963 -CensusMonths @(1,2)
  $genreFollowUpLastRun="genre-followup-$RunTag-late-$genreFollowUpSeed"
  $genreFollowUpCompletedDates=@()
  $genreFollowUpRemaining=166
  for($genreFollowUpAttempt=0;$genreFollowUpAttempt -le 3;$genreFollowUpAttempt++) {
   $genreFollowUpManifest=Get-Content -LiteralPath "SimLogs/$genreFollowUpLastRun-polar-research.json" -Raw | ConvertFrom-Json
   $genreFollowUpCompletedDates+=@($genreFollowUpManifest.months | Where-Object Status -eq 'complete' | ForEach-Object Date)
   if(@($genreFollowUpCompletedDates | Select-Object -Unique).Count -ge 2){break}
   if($genreFollowUpAttempt -eq 3){throw "Could not retain two censuses for $genreFollowUpSeed; partial evidence retained"}
   $genreFollowUpSnapshot="SimLogs/$genreFollowUpLastRun-polar-checkpoint.json.gz"
   $genreFollowUpMetadata=& node "$PSScriptRoot/read-polar-snapshot-metadata.mjs" $genreFollowUpSnapshot | ConvertFrom-Json
   if($LASTEXITCODE -ne 0){throw 'Invalid checkpoint metadata'}
   $genreFollowUpRemaining=[Math]::Max(1,$genreFollowUpRemaining-$genreFollowUpMetadata.weeks)
   $genreFollowUpLastRun="genre-followup-$RunTag-late-$genreFollowUpSeed-resume$($genreFollowUpAttempt+1)"
   & "$PSScriptRoot/run-polar-research.ps1" -Seed $genreFollowUpSeed -Run $genreFollowUpLastRun -Weeks $genreFollowUpRemaining -Census sample -SamplePerStratum 12 -MaxSeconds 600 -ShapeVariationV2 on -GenreFollowUpCensus -CensusFromYear 1963 -CensusMonths @(1,2) -LoadSnapshot $genreFollowUpSnapshot -VerifySnapshot
  }
 }
}
