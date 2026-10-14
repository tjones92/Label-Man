[CmdletBinding()]
param([string]$RunTag='scene7-source-v1',
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe')
$ErrorActionPreference='Stop'
$sceneRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sceneFolder=Join-Path $sceneRoot "SimLogs/$RunTag"
if($RunTag -notmatch '^[a-z0-9-]+$' -or (Test-Path $sceneFolder)){throw 'Use a fresh run tag.'}
New-Item -ItemType Directory -Path $sceneFolder | Out-Null
$sceneFlags=@('--enable-genre-market-v2','--enable-artist-population-lifecycle','--enable-artist-evolution',
 '--enable-artist-recognition','--enable-managers','--seed-star-canopy','--enable-cowriting','--enable-member-axes',
 '--observe-band-life','--enable-lineup-churn=world','--member-fame-share=0.90')
$sceneJobs=@()
foreach($sceneSeed in @(1001,2002)) {
 foreach($sceneCheck in @('source','recruitment','information','ecosystem','city-placement')) {
  $sceneJobs+=@{name="$sceneCheck-$sceneSeed";seed=$sceneSeed;weeks=0;flags=@("--scene-$sceneCheck-check");marker=('SCENE_'+$sceneCheck.ToUpper().Replace('-','_')+'_CHECK_PASS')}
 }
}
$sceneJobs+=@{name='rooms-1001';seed=1001;weeks=0;flags=@('--scene-room-check');marker='SCENE_ROOM_CHECK_PASS'}
$sceneJobs+=@{name='roundtrip-1001';seed=1001;weeks=26;flags=@();marker='SAVELOAD_ROUNDTRIP_PASS'}
$sceneManifest=[ordered]@{revision=(& git -C $sceneRoot rev-parse HEAD).Trim();assemblySha256=(Get-FileHash (Join-Path $sceneRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash;sourceHashes=@(Get-ChildItem (Join-Path $sceneRoot 'Systems'),(Join-Path $sceneRoot 'Data'),(Join-Path $sceneRoot 'SimTools') -Filter '*.cs' | Get-FileHash | Select-Object Path,Hash);jobs=$sceneJobs;results=@();maximumParallel=1}
foreach($sceneJob in $sceneJobs) {
 if(@(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$').Count){throw 'Another game is active.'}
 $sceneArgs=@('--headless','--path','.','SimTools/SaveLoadRoundTripRunner.tscn','--',"--seed=$($sceneJob.seed)","--weeks=$($sceneJob.weeks)")+$sceneFlags+$sceneJob.flags
 $sceneOut=Join-Path $sceneFolder "$($sceneJob.name)-console.log"
 $sceneProcess=Start-Process -FilePath $Godot -ArgumentList $sceneArgs -WorkingDirectory $sceneRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $sceneOut -RedirectStandardError (Join-Path $sceneFolder "$($sceneJob.name)-errors.log"); $null=$sceneProcess.Handle
 while(!$sceneProcess.WaitForExit(10000)){}
 $sceneProcess.WaitForExit()
 $scenePassed=$sceneProcess.ExitCode -eq 0 -and (Get-Content $sceneOut -Raw).Contains($sceneJob.marker)
 $sceneManifest.results+=@{name=$sceneJob.name;passed=$scenePassed;exitCode=$sceneProcess.ExitCode;args=$sceneArgs}
 $sceneManifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $sceneFolder 'runs.json') -Encoding utf8
 if(!$scenePassed){throw "Failed $($sceneJob.name); inspect logs."}
 Write-Output "Passed $($sceneJob.name)."
}
