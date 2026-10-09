param([string]$RunTag='scene6-final-v1', [string]$PriorRoot='C:\Users\grohl\.codex\worktrees\local-scene-phase-5-feedback\Label-Man')
$ErrorActionPreference='Stop'
$sceneRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$scenePrior=$PriorRoot
$sceneFolder=Join-Path $sceneRoot "SimLogs/$RunTag"
if(Test-Path $sceneFolder){throw 'Use a fresh tag.'}
New-Item -ItemType Directory -Path $sceneFolder | Out-Null
$sceneGodot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
$sceneFlags=@('--enable-genre-market-v2','--enable-artist-population-lifecycle','--enable-artist-evolution','--enable-artist-recognition','--enable-managers','--seed-star-canopy','--enable-cowriting','--enable-member-axes','--observe-band-life','--enable-lineup-churn=world','--member-fame-share=0.90')
$sceneSlices=@('--enable-scene-institutions','--enable-scene-relocation','--enable-scene-price-feedback')
$sceneJobs=@(
 @{name='information-1001';root=$sceneRoot;driver='SaveLoadRoundTripRunner';seed=1001;weeks=0;flags=@('--scene-information-check');marker='SCENE_INFORMATION_CHECK_PASS'},
 @{name='information-2002';root=$sceneRoot;driver='SaveLoadRoundTripRunner';seed=2002;weeks=0;flags=@('--scene-information-check');marker='SCENE_INFORMATION_CHECK_PASS'},
 @{name='integration-1001';root=$sceneRoot;driver='SaveLoadRoundTripRunner';seed=1001;weeks=0;flags=@('--integration');marker='SAVELOAD_INTEGRATION_PASS'},
 @{name='ui-1001';root=$sceneRoot;driver='SceneRoomUiRunner';seed=1001;weeks=0;flags=@('--scene-information-ui-check');marker='SCENE_INFORMATION_UI_PASS'},
 @{name="$RunTag-control-1001";root=$scenePrior;driver='ChartAuditRunner';seed=1001;weeks=8;flags=@('--aggregate-only')+$sceneSlices;marker='CHART_AUDIT_COMPLETE'},
 @{name="$RunTag-default-1001";root=$sceneRoot;driver='ChartAuditRunner';seed=1001;weeks=8;flags=@('--aggregate-only');marker='CHART_AUDIT_COMPLETE'},
 @{name="$RunTag-explicit-1001";root=$sceneRoot;driver='ChartAuditRunner';seed=1001;weeks=8;flags=@('--aggregate-only')+$sceneSlices;marker='CHART_AUDIT_COMPLETE'}
)
$sceneManifest=[ordered]@{assemblySha256=(Get-FileHash (Join-Path $sceneRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash;priorAssemblySha256=(Get-FileHash (Join-Path $scenePrior '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash;sourceHashes=@(Get-ChildItem (Join-Path $sceneRoot 'Systems'),(Join-Path $sceneRoot 'Data'),(Join-Path $sceneRoot 'SimTools') -Filter '*.cs' | Get-FileHash | Select-Object Path,Hash);jobs=$sceneJobs;results=@();maximumParallel=1}
foreach($sceneJob in $sceneJobs){
 if(@(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$').Count){throw 'Another game is active.'}
 $sceneArgs=@(if($sceneJob.driver -eq 'SceneRoomUiRunner'){@('--rendering-method','gl_compatibility','--resolution','1400x1000')}else{@('--headless')})
 $sceneArgs+=@('--path','.',"SimTools/$($sceneJob.driver).tscn",'--',"--seed=$($sceneJob.seed)","--weeks=$($sceneJob.weeks)")+$sceneFlags+$sceneJob.flags
 if($sceneJob.driver -eq 'ChartAuditRunner'){$sceneArgs+=@("--run=$($sceneJob.name)")}
 $sceneOut=Join-Path $sceneFolder "$($sceneJob.name)-console.log"
 $sceneStart=[datetime]::UtcNow
 $sceneProcess=Start-Process -FilePath $sceneGodot -ArgumentList $sceneArgs -WorkingDirectory $sceneJob.root -WindowStyle Hidden -PassThru -RedirectStandardOutput $sceneOut -RedirectStandardError (Join-Path $sceneFolder "$($sceneJob.name)-errors.log")
 while(!$sceneProcess.WaitForExit(10000)){}
 $sceneProcess.WaitForExit()
 $scenePassed=$sceneProcess.ExitCode -eq 0 -and (Get-Content $sceneOut -Raw).Contains($sceneJob.marker)
 $sceneManifest.results+=@{name=$sceneJob.name;passed=$scenePassed;exitCode=$sceneProcess.ExitCode;args=$sceneArgs;seconds=([datetime]::UtcNow-$sceneStart).TotalSeconds}
 $sceneManifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $sceneFolder 'runs.json') -Encoding utf8
 if(!$scenePassed){throw "Failed $($sceneJob.name); inspect logs."}
 Write-Output "Passed $($sceneJob.name)."
}

