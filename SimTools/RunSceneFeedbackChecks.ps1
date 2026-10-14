[CmdletBinding()]
param([string]$RunTag='scene5-live-feedback-fixed-v1',
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe')
$ErrorActionPreference='Stop'
$sceneRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sceneFolder=Join-Path $sceneRoot "SimLogs/$RunTag"
if($RunTag -notmatch '^[a-z0-9-]+$' -or (Test-Path $sceneFolder)){throw 'Use a fresh run tag.'}
$sceneFlags=@('--enable-genre-market-v2','--enable-artist-population-lifecycle','--enable-artist-evolution',
 '--enable-artist-recognition','--enable-managers','--seed-star-canopy','--enable-cowriting','--enable-member-axes',
 '--observe-band-life','--enable-lineup-churn=world','--member-fame-share=0.90')
$sceneJobs=@()
foreach($sceneSeed in @(1001,2002)){
 $sceneJobs+=@{name="feedback-$sceneSeed";seed=$sceneSeed;weeks=0;driver="SaveLoadRoundTripRunner";flags=@("--enable-scene-attention-feedback","--scene-feedback-check");marker="SCENE_FEEDBACK_CHECK_PASS"}
}
foreach($sceneSeed in @(1001,2002)){
 $sceneJobs+=@{name="dynamics-$sceneSeed";seed=$sceneSeed;weeks=0;driver='SaveLoadRoundTripRunner';flags=@('--scene-dynamics-check');marker='SCENE_DYNAMICS_CHECK_PASS'}
 $sceneJobs+=@{name="recruitment-default-$sceneSeed";seed=$sceneSeed;weeks=0;driver='SaveLoadRoundTripRunner';flags=@('--scene-recruitment-check');marker='SCENE_RECRUITMENT_CHECK_PASS'}
}
$sceneJobs+=@{name='roundtrip-1001';seed=1001;weeks=26;driver='SaveLoadRoundTripRunner';flags=@('--enable-scene-attention-feedback');marker='SAVELOAD_ROUNDTRIP_PASS'}
$sceneJobs+=@{name='rooms-default';seed=1001;weeks=0;driver='SaveLoadRoundTripRunner';flags=@('--scene-room-check');marker='SCENE_ROOM_CHECK_PASS'}
foreach($sceneMode in @('default','explicit','observe')){
 $sceneExtra=if($sceneMode -eq 'explicit'){@('--enable-scene-recruitment')}elseif($sceneMode -eq 'observe'){@('--observe-scene-dynamics')}else{@()}
 $sceneName="$RunTag-$sceneMode-1001"
 $sceneJobs+=@{name=$sceneName;seed=1001;weeks=8;driver='ChartAuditRunner';flags=@('--aggregate-only','--local-scene-identity-audit','--local-scene-persistence-audit','--scene-room-audit','--scene-recruitment-audit')+$sceneExtra;marker="CHART_AUDIT_COMPLETE run=$sceneName weeks=8"}
}
New-Item -ItemType Directory -Path $sceneFolder | Out-Null
$sceneManifest=[ordered]@{revision=(& git -C $sceneRoot rev-parse HEAD).Trim();assemblySha256=(Get-FileHash (Join-Path $sceneRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash;sourceHashes=@(Get-ChildItem (Join-Path $sceneRoot 'Systems'),(Join-Path $sceneRoot 'Data'),(Join-Path $sceneRoot 'SimTools') -Filter '*.cs' | Get-FileHash | Select-Object Path,Hash);jobs=$sceneJobs;results=@();maximumParallel=1}
foreach($sceneJob in $sceneJobs){
 if(@(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$').Count){throw 'Another game is active.'}
 $sceneArgs=@('--headless','--path','.',"SimTools/$($sceneJob.driver).tscn",'--',"--seed=$($sceneJob.seed)","--weeks=$($sceneJob.weeks)")+$sceneFlags+$sceneJob.flags
 if($sceneJob.driver -eq 'ChartAuditRunner'){$sceneArgs+=@("--run=$($sceneJob.name)")}
 $sceneStdout=Join-Path $sceneFolder "$($sceneJob.name)-console.log"
 $sceneStart=[datetime]::UtcNow
 $sceneProcess=Start-Process -FilePath $Godot -ArgumentList $sceneArgs -WorkingDirectory $sceneRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $sceneStdout -RedirectStandardError (Join-Path $sceneFolder "$($sceneJob.name)-errors.log"); $null=$sceneProcess.Handle
 while(!$sceneProcess.WaitForExit(10000)){}
 $sceneProcess.WaitForExit()
 $scenePassed=$sceneProcess.ExitCode -eq 0 -and (Get-Content $sceneStdout -Raw).Contains($sceneJob.marker)
 $sceneManifest.results+=@{name=$sceneJob.name;passed=$scenePassed;exitCode=$sceneProcess.ExitCode;args=$sceneArgs;seconds=([datetime]::UtcNow-$sceneStart).TotalSeconds}
 $sceneManifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $sceneFolder 'runs.json') -Encoding utf8
 if(!$scenePassed){throw "Failed $($sceneJob.name); inspect logs."}
 Write-Output "Passed $($sceneJob.name)."
}
