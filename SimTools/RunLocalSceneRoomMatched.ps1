[CmdletBinding()]
param([string]$RunTag='scene3-matched-v1',
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe',
 [string]$ControlRoot='C:/Users/grohl/.codex/worktrees/local-scene-control/Label-Man')
$ErrorActionPreference='Stop'
$sceneRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sceneFolder=Join-Path $sceneRoot "SimLogs/$RunTag"
if($RunTag -notmatch '^[a-z0-9-]+$' -or (Test-Path -LiteralPath $sceneFolder)){throw 'Choose a new valid run tag.'}
if(@(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$').Count){throw 'Another game is active.'}
$sceneFlags=@('--enable-genre-market-v2','--enable-artist-population-lifecycle','--enable-artist-evolution',
 '--enable-artist-recognition','--enable-managers','--seed-star-canopy','--enable-cowriting','--enable-member-axes',
 '--observe-band-life','--enable-lineup-churn=world','--member-fame-share=0.90','--aggregate-only',
 '--local-scene-identity-audit','--local-scene-persistence-audit','--scene-room-audit')
$sceneJobs=@()
$sceneResults=@()
foreach($sceneSeed in @(1001,2002)){
 $sceneName="scene1-matched-v3-control-$sceneSeed"
 $scenePrior=Get-Content (Join-Path $sceneRoot 'SimLogs/scene1-matched-v3/runs.json') -Raw | ConvertFrom-Json
 $sceneResult=@($scenePrior.results | Where-Object name -EQ $sceneName)[0]
 if(!$sceneResult.passed -or $sceneResult.exitCode -ne 0 -or $sceneResult.weeks -ne 52){throw 'Frozen control incomplete.'}
 $sceneJobs+=@{name=$sceneName;seed=$sceneSeed;mode='control';root=$ControlRoot;reused=$true}
 $sceneResults+=@{name=$sceneName;passed=$true;reused=$true;sourceManifest='scene1-matched-v3/runs.json';weeks=52;exitCode=0}
}
foreach($sceneSpec in @(@{mode='rooms';seed=1001},@{mode='rooms';seed=2002},@{mode='off';seed=1001})){
 $sceneJobs+=@{name="$RunTag-$($sceneSpec.mode)-$($sceneSpec.seed)";seed=$sceneSpec.seed;mode=$sceneSpec.mode;root=$sceneRoot;
 extra=@($(if($sceneSpec.mode -eq 'off'){'--disable-local-scenes'}else{'--enable-scene-rooms'}))}
}
New-Item -ItemType Directory -Path $sceneFolder | Out-Null
$sceneManifest=[ordered]@{revision=(& git -C $sceneRoot rev-parse HEAD).Trim();weeks=52;flags=$sceneFlags;
 assemblySha256=(Get-FileHash (Join-Path $sceneRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash;
 godotSha256=(Get-FileHash $Godot).Hash;jobs=$sceneJobs;results=$sceneResults;maximumParallel=2}
$sceneManifestPath=Join-Path $sceneFolder 'runs.json'
$sceneManifest | ConvertTo-Json -Depth 8 | Set-Content $sceneManifestPath -Encoding utf8
# Independent audit outputs; no player-save slots. Only two engines run concurrently.
$sceneActive=@()
$scenePending=@($sceneJobs | Where-Object mode -NE 'control')
while($scenePending.Count -or $sceneActive.Count){
 while($scenePending.Count -and $sceneActive.Count -lt 2){
  $sceneFree=[math]::Floor((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory/1024)
  if($sceneFree -lt 650 -and $sceneActive.Count){break}
  if($sceneFree -lt 512){throw "Only $sceneFree MiB free before launch."}
  $sceneJob=$scenePending[0]; $scenePending=@($scenePending | Select-Object -Skip 1)
  $sceneArgs=@('--headless','--path','.','SimTools/ChartAuditRunner.tscn','--','--weeks=52',"--run=$($sceneJob.name)","--seed=$($sceneJob.seed)",
   '--save-world-at-year=1960',"--save-world=$($sceneJob.name)-start")+$sceneFlags+$sceneJob.extra
  $sceneProcess=Start-Process -FilePath $Godot -ArgumentList $sceneArgs -WorkingDirectory $sceneRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $sceneFolder "$($sceneJob.name)-console.log") -RedirectStandardError (Join-Path $sceneFolder "$($sceneJob.name)-errors.log")
  $sceneActive+=@{job=$sceneJob;process=$sceneProcess;start=[datetime]::UtcNow;args=$sceneArgs}
  Write-Output "Started $($sceneJob.name)."
 }
 foreach($sceneRun in @($sceneActive)){
  if(!$sceneRun.process.HasExited){continue}
  $sceneRun.process.WaitForExit();$sceneName=$sceneRun.job.name
  $sceneLog=Get-Content (Join-Path $sceneFolder "$sceneName-console.log") -Raw
  $sceneRows=@(Import-Csv (Join-Path $sceneRoot "SimLogs/$sceneName-weeks.csv"))
  $scenePassed=$sceneRun.process.ExitCode -eq 0 -and $sceneRows.Count -eq 52 -and $sceneLog.Contains("CHART_AUDIT_COMPLETE run=$sceneName weeks=52")
  $sceneManifest.results+=@{name=$sceneName;passed=$scenePassed;exitCode=$sceneRun.process.ExitCode;weeks=$sceneRows.Count;seconds=([datetime]::UtcNow-$sceneRun.start).TotalSeconds;args=$sceneRun.args}
  $sceneManifest | ConvertTo-Json -Depth 8 | Set-Content $sceneManifestPath -Encoding utf8
  $sceneActive=@($sceneActive | Where-Object {$_ -ne $sceneRun})
  if(!$scenePassed){throw "Failed $sceneName; inspect logs."}
  Write-Output "Passed $sceneName."
 }
 if($sceneActive.Count){Start-Sleep -Seconds 5}
}

