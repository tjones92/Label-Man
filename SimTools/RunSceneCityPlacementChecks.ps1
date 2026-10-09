[CmdletBinding()]
param([string]$RunTag='scene4-city-placement-v1',
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe')
$ErrorActionPreference='Stop'
$sceneRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sceneFolder=Join-Path $sceneRoot "SimLogs/$RunTag"
if($RunTag -notmatch '^[a-z0-9-]+$' -or (Test-Path -LiteralPath $sceneFolder)){throw 'Choose a new valid run tag.'}
$sceneFlags=@('--enable-genre-market-v2','--enable-artist-population-lifecycle','--enable-artist-evolution',
 '--enable-artist-recognition','--enable-managers','--seed-star-canopy','--enable-cowriting','--enable-member-axes',
 '--observe-band-life','--enable-lineup-churn=world','--member-fame-share=0.90')
$sceneJobs=@()
foreach($sceneSeed in @(1001,2002)) {
 $sceneJobs+=@{name="placement-$sceneSeed";seed=$sceneSeed;weeks=0;flags=@('--scene-city-placement-check');marker='SCENE_CITY_PLACEMENT_CHECK_PASS'}
 $sceneJobs+=@{name="recruitment-$sceneSeed";seed=$sceneSeed;weeks=0;flags=@('--enable-scene-recruitment','--scene-recruitment-check');marker='SCENE_RECRUITMENT_CHECK_PASS'}
 $sceneJobs+=@{name="roundtrip-$sceneSeed";seed=$sceneSeed;weeks=26;flags=@('--enable-scene-recruitment');marker='SAVELOAD_ROUNDTRIP_PASS'}
}
$sceneJobs+=@{name='rooms-control';seed=1001;weeks=0;flags=@('--scene-room-check');marker='SCENE_ROOM_CHECK_PASS'}
New-Item -ItemType Directory -Path $sceneFolder | Out-Null
$sceneManifest=[ordered]@{revision=(& git -C $sceneRoot rev-parse HEAD).Trim();flags=$sceneFlags;
 assemblySha256=(Get-FileHash (Join-Path $sceneRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash;
 sourceHashes=@(Get-ChildItem (Join-Path $sceneRoot 'Systems'),(Join-Path $sceneRoot 'Data'),(Join-Path $sceneRoot 'SimTools') -Filter '*.cs' | Get-FileHash | Select-Object Path,Hash);
 godotSha256=(Get-FileHash $Godot).Hash;jobs=$sceneJobs;results=@();maximumParallel=1}
foreach($sceneJob in $sceneJobs){
 if(@(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$').Count){throw 'Another game is active.'}
 $sceneFree=[math]::Floor((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory/1024)
 if($sceneFree -lt 768){throw "Only $sceneFree MiB free."}
 $sceneArgs=@('--headless','--path','.','SimTools/SaveLoadRoundTripRunner.tscn','--',"--seed=$($sceneJob.seed)","--weeks=$($sceneJob.weeks)")+$sceneFlags+$sceneJob.flags
 $sceneStart=[datetime]::UtcNow
 $sceneStdout=Join-Path $sceneFolder "$($sceneJob.name)-console.log"
 $sceneProcess=Start-Process -FilePath $Godot -ArgumentList $sceneArgs -WorkingDirectory $sceneRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $sceneStdout -RedirectStandardError (Join-Path $sceneFolder "$($sceneJob.name)-errors.log")
 while(!$sceneProcess.WaitForExit(10000)){Write-Output "Running $($sceneJob.name)."}
 $sceneProcess.WaitForExit()
 $scenePassed=$sceneProcess.ExitCode -eq 0 -and (Get-Content $sceneStdout -Raw).Contains($sceneJob.marker)
 $sceneManifest.results+=@{name=$sceneJob.name;exitCode=$sceneProcess.ExitCode;passed=$scenePassed;seconds=([datetime]::UtcNow-$sceneStart).TotalSeconds;args=$sceneArgs}
 $sceneManifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $sceneFolder 'runs.json') -Encoding utf8
 if(!$scenePassed){throw "Failed $($sceneJob.name); inspect logs."}
 Write-Output "Passed $($sceneJob.name)."
}
