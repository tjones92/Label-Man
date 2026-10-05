param([ValidateSet(1001,1002)][int[]]$Seeds=@(1001,1002),[string]$RunTag='shape-v2',[ValidateSet('on','off')][string]$ShapeV2='on',[switch]$LegacyAReplay)
$ErrorActionPreference='Stop'
$polarRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location -LiteralPath $polarRoot
try {
 if($RunTag -notmatch '^[a-zA-Z0-9-]+$'){throw 'Invalid run tag'}
 $polarRuns=@()
 foreach($polarSeed in $Seeds) {
  $polarRemaining=91;$polarSlice=0;$polarSnapshot=$null
  while($polarRemaining -gt 0) {
   $polarSlice++;if($polarSlice -gt 12){throw 'Exceeded bounded slices; partial evidence preserved'}
   $polarRun="polar-directive3-fresh-$RunTag-$polarSeed-s$polarSlice"
   $polarRunBase=$polarRun;$polarRetry=0
   while((Test-Path -LiteralPath "SimLogs/$polarRun-invocation.json") -and
     -not((Get-Content -LiteralPath "SimLogs/$polarRun.log" -Raw) -match 'CHART_AUDIT_(STOPPED|COMPLETE)')) {
    $polarRetry++;if($polarRetry -gt 3){throw 'Three failed slice attempts retained; inspect the cause'}
    $polarRun="$polarRunBase-retry$polarRetry"
   }
   if(Test-Path -LiteralPath "SimLogs/$polarRun-invocation.json") {
    $polarPrior=Get-Content -LiteralPath "SimLogs/$polarRun.log" -Raw
    if($polarPrior -notmatch 'CHART_AUDIT_(STOPPED|COMPLETE)'){throw 'Existing slice is incomplete; use a fresh tag'}
   } else {
    $polarArgs=@{Seed=$polarSeed;Run=$polarRun;Weeks=$polarRemaining;Census='none';MaxSeconds=180;ShapeVariation=$(if($LegacyAReplay){'off'}else{'on'});ShapeVariationV2=$(if($LegacyAReplay){'off'}else{$ShapeV2});Directive3Trajectory=$true;Directive3AReplay=$LegacyAReplay}
    if($polarSnapshot){$polarArgs.LoadSnapshot=$polarSnapshot;$polarArgs.VerifySnapshot=$true}
    & "$PSScriptRoot/run-polar-research.ps1" @polarArgs
   }
   $polarSnapshot="SimLogs/$polarRun-polar-checkpoint.json.gz"
   $polarState=& node "$PSScriptRoot/read-polar-snapshot-metadata.mjs" $polarSnapshot | ConvertFrom-Json
   if($LASTEXITCODE -ne 0 -or $polarState.weeks -le 0){throw 'No safe trajectory progress'}
   if($polarState.shape -ne $(if($LegacyAReplay){0}elseif($ShapeV2 -eq 'on'){2}else{1})){throw 'Shape version mismatch'}
   $polarRemaining-=$polarState.weeks
   $polarRuns+=@{run=$polarRun;seed=$polarSeed;shape=$(if($LegacyAReplay){'off'}else{$ShapeV2});shapeVersion=$polarState.shape;legacyAReplay=[bool]$LegacyAReplay;slice=$polarSlice;date=$polarState.date;weeks=$polarState.weeks;remaining=$polarRemaining;snapshot=$polarSnapshot}
   $polarRuns | ConvertTo-Json -Depth 5 | Set-Content "SimLogs/polar-directive3-trajectory-$RunTag-runs.json"
   Write-Output "$polarRun date $($polarState.date -join '-') remaining $polarRemaining"
  }
 }
} finally {Pop-Location}
