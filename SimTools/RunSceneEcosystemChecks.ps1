[CmdletBinding()]
param([string]$RunTag='scene5-ecosystem-fixed-v1', [switch]$SkipRegression, [switch]$SkipRoundTrip, [switch]$Timeline,
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe')
$ErrorActionPreference='Stop'
$sceneRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sceneFolder=Join-Path $sceneRoot "SimLogs/$RunTag"
if($RunTag -notmatch '^[a-z0-9-]+$' -or (Test-Path $sceneFolder)){throw 'Use a fresh run tag.'}
New-Item -ItemType Directory -Path $sceneFolder | Out-Null
$sceneFlags=@('--enable-genre-market-v2','--enable-artist-population-lifecycle','--enable-artist-evolution',
 '--enable-artist-recognition','--enable-managers','--seed-star-canopy','--enable-cowriting','--enable-member-axes',
 '--observe-band-life','--enable-lineup-churn=world','--member-fame-share=0.90')
$sceneJobs=@(@{name='ecosystem-1001';seed=1001;weeks=0;flags=@('--scene-ecosystem-check');marker='SCENE_ECOSYSTEM_CHECK_PASS'},
 @{name='ecosystem-2002';seed=2002;weeks=0;flags=@('--scene-ecosystem-check');marker='SCENE_ECOSYSTEM_CHECK_PASS'},
 @{name='roundtrip-all-1001';seed=1001;weeks=26;flags=@('--enable-scene-institutions','--enable-scene-relocation','--enable-scene-price-feedback');marker='SAVELOAD_ROUNDTRIP_PASS'})
if($SkipRoundTrip){ $sceneJobs=@($sceneJobs | Where-Object weeks -eq 0) }
if($Timeline){ foreach($sceneJob in $sceneJobs){ if($sceneJob.weeks -eq 0){$sceneJob.flags+=@('--scene-ecosystem-timeline-check')} } }
$sceneManifest=[ordered]@{revision=(& git -C $sceneRoot rev-parse HEAD).Trim();assemblySha256=(Get-FileHash (Join-Path $sceneRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash;sourceHashes=@(Get-ChildItem (Join-Path $sceneRoot 'Systems'),(Join-Path $sceneRoot 'Data'),(Join-Path $sceneRoot 'SimTools') -Filter '*.cs' | Get-FileHash | Select-Object Path,Hash);jobs=$sceneJobs;results=@();maximumParallel=1}
foreach($sceneJob in $sceneJobs){
 if(@(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$').Count){throw 'Another game is active.'}
 $sceneArgs=@('--headless','--path','.','SimTools/SaveLoadRoundTripRunner.tscn','--',"--seed=$($sceneJob.seed)","--weeks=$($sceneJob.weeks)")+$sceneFlags+$sceneJob.flags
 $sceneStdout=Join-Path $sceneFolder "$($sceneJob.name)-console.log"
 $sceneProcess=Start-Process -FilePath $Godot -ArgumentList $sceneArgs -WorkingDirectory $sceneRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $sceneStdout -RedirectStandardError (Join-Path $sceneFolder "$($sceneJob.name)-errors.log")
 while(!$sceneProcess.WaitForExit(10000)){}
 $sceneProcess.WaitForExit()
 $scenePassed=$sceneProcess.ExitCode -eq 0 -and (Get-Content $sceneStdout -Raw).Contains($sceneJob.marker)
 $sceneManifest.results+=@{name=$sceneJob.name;passed=$scenePassed;exitCode=$sceneProcess.ExitCode;args=$sceneArgs}
 $sceneManifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $sceneFolder 'runs.json') -Encoding utf8
 if(!$scenePassed){throw "Failed $($sceneJob.name); inspect logs."}
 Write-Output "Passed $($sceneJob.name)."
}
if(!$SkipRegression){ & (Join-Path $PSScriptRoot 'RunSceneFeedbackChecks.ps1') -RunTag "$RunTag-regression" -Godot $Godot }
