param([ValidateSet(1001,1002)][int[]]$Seeds=@(1001,1002), [ValidateSet('off','on')][string[]]$Shapes=@('off','on'), [string]$RunTag='monthly')
$ErrorActionPreference='Stop'
$polarTrajectoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location -LiteralPath $polarTrajectoryRoot
try {
 if ($RunTag -notmatch '^[a-zA-Z0-9-]+$') {throw 'Invalid trajectory run tag'}
 $polarRuns=@()
 foreach($polarShape in $Shapes) { foreach($polarSeed in $Seeds) {
  $polarSnapshot=$null; $polarRemaining=91; $polarSlice=0
  # Resume only this task's matching fresh run tag. Completed slices are read, never overwritten.
  while(Test-Path -LiteralPath "SimLogs/polar-directive2-fresh-$RunTag-$polarShape-$polarSeed-s$($polarSlice+1)-invocation.json") {
   $polarSlice++;$polarPriorRun="polar-directive2-fresh-$RunTag-$polarShape-$polarSeed-s$polarSlice"
   $polarSnapshot="SimLogs/$polarPriorRun-polar-checkpoint.json.gz"
   $polarPriorLog=Get-Content -LiteralPath "SimLogs/$polarPriorRun.log" -Raw
   if($polarPriorLog -notmatch 'CHART_AUDIT_(STOPPED|COMPLETE)') {throw 'Existing slice has no safe completion marker'}
   $polarPriorState=& node "$PSScriptRoot/read-polar-snapshot-metadata.mjs" $polarSnapshot | ConvertFrom-Json
   if($LASTEXITCODE -ne 0) {throw 'Could not stream existing snapshot metadata'}
   if($polarPriorState.shape -ne $(if($polarShape -eq 'on'){1}else{0})) {throw 'Existing snapshot version mismatch'}
   $polarRemaining-=$polarPriorState.weeks
   $polarRuns+=@{run=$polarPriorRun;seed=$polarSeed;shape=$polarShape;slice=$polarSlice;date=$polarPriorState.date;weeks=$polarPriorState.weeks;remaining=$polarRemaining;snapshot=$polarSnapshot}
  }
  while($polarRemaining -gt 0) {
   $polarSlice++
   if($polarSlice -gt 12) {throw 'Fresh trajectory exceeded bounded slice count; preserve partial evidence.'}
   $polarRun="polar-directive2-fresh-$RunTag-$polarShape-$polarSeed-s$polarSlice"
   $polarArgs=@{Seed=$polarSeed;Run=$polarRun;Weeks=$polarRemaining;Census='none';MaxSeconds=180;ShapeVariation=$polarShape;Directive2Trajectory=$true}
   if($polarSnapshot) {$polarArgs.LoadSnapshot=$polarSnapshot; $polarArgs.VerifySnapshot=$true}
   & "$PSScriptRoot/run-polar-research.ps1" @polarArgs
   $polarSnapshot="SimLogs/$polarRun-polar-checkpoint.json.gz"
   $polarState=& node "$PSScriptRoot/read-polar-snapshot-metadata.mjs" $polarSnapshot | ConvertFrom-Json
   if($LASTEXITCODE -ne 0) {throw 'Could not stream snapshot metadata'}
   if($polarState.shape -ne $(if($polarShape -eq 'on'){1}else{0})) {throw 'Shape version changed during trajectory'}
   $polarRemaining-=$polarState.weeks
   if($polarState.weeks -eq 0) {throw 'No trajectory progress within 180-second slice'}
   $polarRuns+=@{run=$polarRun;seed=$polarSeed;shape=$polarShape;slice=$polarSlice;date=$polarState.date;weeks=$polarState.weeks;remaining=$polarRemaining;snapshot=$polarSnapshot}
   $polarRuns | ConvertTo-Json -Depth 5 | Set-Content "SimLogs/polar-directive2-trajectory-$RunTag-runs.json"
   Write-Output "Fresh trajectory $polarShape seed $polarSeed slice $polarSlice date $($polarState.date -join '-') remaining weeks $polarRemaining"
  }
 }}
} finally {Pop-Location}
