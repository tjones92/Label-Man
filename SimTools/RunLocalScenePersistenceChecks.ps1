[CmdletBinding()]
param([string]$RunTag='scene2-fixed',
    [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe')
# Run only within the user's authorized game test window; always serial.
$ErrorActionPreference='Stop'
$sceneRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if($RunTag -notmatch '^[a-z0-9-]+$'){throw 'Invalid run tag.'}
$sceneFolder=Join-Path $sceneRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $sceneFolder){throw 'Choose a new run tag.'}
$sceneFlags=@('--enable-genre-market-v2','--enable-artist-population-lifecycle','--enable-artist-evolution',
 '--enable-artist-recognition','--enable-managers','--seed-star-canopy','--enable-cowriting','--enable-member-axes',
 '--observe-band-life','--enable-lineup-churn=world','--member-fame-share=0.90','--enable-persistent-scenes')
$sceneJobs=@(
 @{name='checks-1001';seed=1001;weeks=0;flags=@('--local-scene-persistence-check');marker='SCENE_PERSISTENCE_CHECK_PASS'},
 @{name='checks-2002';seed=2002;weeks=0;flags=@('--local-scene-persistence-check');marker='SCENE_PERSISTENCE_CHECK_PASS'},
 @{name='roundtrip-1001';seed=1001;weeks=26;flags=@();marker='SAVELOAD_ROUNDTRIP_PASS'},
 @{name='roundtrip-2002';seed=2002;weeks=26;flags=@();marker='SAVELOAD_ROUNDTRIP_PASS'}
)
New-Item -ItemType Directory -Path $sceneFolder | Out-Null
$sceneManifest=[ordered]@{revision=(& git -C $sceneRoot rev-parse HEAD).Trim();flags=$sceneFlags;
 assemblySha256=(Get-FileHash -LiteralPath (Join-Path $sceneRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash;
 godotSha256=(Get-FileHash -LiteralPath $Godot).Hash;jobs=$sceneJobs;results=@()}
$sceneManifestPath=Join-Path $sceneFolder 'runs.json'
foreach($sceneJob in $sceneJobs){
 if(@(Get-Process | Where-Object {$_.ProcessName -match '^Godot|^Label Man$'}).Count -gt 0){throw 'Another game is active.'}
 $sceneFree=[math]::Floor((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory/1024)
 if($sceneFree -lt 768){throw "Only $sceneFree MiB available."}
 $sceneStdout=Join-Path $sceneFolder "$($sceneJob.name)-console.log"
 $sceneStderr=Join-Path $sceneFolder "$($sceneJob.name)-errors.log"
 $sceneStart=[datetime]::UtcNow
 $sceneArgs=@('--headless','--path','.','SimTools/SaveLoadRoundTripRunner.tscn','--',"--seed=$($sceneJob.seed)","--weeks=$($sceneJob.weeks)")+$sceneFlags+$sceneJob.flags
 $sceneProcess=Start-Process -FilePath $Godot -ArgumentList $sceneArgs -WorkingDirectory $sceneRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $sceneStdout -RedirectStandardError $sceneStderr
 while(!$sceneProcess.WaitForExit(10000)){Write-Output "Running $($sceneJob.name)."}
 $sceneProcess.WaitForExit()
 $scenePassed=$sceneProcess.ExitCode -eq 0 -and (Get-Content -LiteralPath $sceneStdout -Raw).Contains($sceneJob.marker)
 $sceneManifest.results+=@{name=$sceneJob.name;exitCode=$sceneProcess.ExitCode;passed=$scenePassed;seconds=([datetime]::UtcNow-$sceneStart).TotalSeconds}
 $sceneManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $sceneManifestPath -Encoding utf8
 if(!$scenePassed){throw "Failed $($sceneJob.name). Inspect the logs."}
 Write-Output "Passed $($sceneJob.name)."
}
