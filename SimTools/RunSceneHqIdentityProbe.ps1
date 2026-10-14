[CmdletBinding()]
param([string]$RunTag='scene-hq-identity-v1')
$ErrorActionPreference='Stop'
$sceneRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sceneGodot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
if($RunTag -notmatch '^[a-z0-9-]+$'){throw 'Invalid run tag.'}
$sceneFolder=Join-Path $sceneRoot "SimLogs/$RunTag"
if(Test-Path $sceneFolder){throw 'Probe already exists.'}
if(@(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$').Count){throw 'Another game is active.'}
New-Item -ItemType Directory -Path $sceneFolder | Out-Null
$sceneArgs=@('--headless','--path','.','SimTools/SaveLoadRoundTripRunner.tscn','--','--seed=1001','--weeks=0','--enable-genre-market-v2','--enable-artist-population-lifecycle','--enable-artist-evolution','--enable-artist-recognition','--enable-managers','--seed-star-canopy','--enable-cowriting','--enable-member-axes','--observe-band-life','--enable-lineup-churn=world','--member-fame-share=0.90','--observe-local-scenes','--local-scene-identity-check')
$sceneOut=Join-Path $sceneFolder 'console.log'
$sceneProcess=Start-Process -FilePath $sceneGodot -ArgumentList $sceneArgs -WorkingDirectory $sceneRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $sceneOut -RedirectStandardError (Join-Path $sceneFolder 'errors.log'); $null=$sceneProcess.Handle
while(!$sceneProcess.WaitForExit(10000)){}
$sceneProcess.WaitForExit()
$scenePassed=$sceneProcess.ExitCode -eq 0 -and (Get-Content $sceneOut -Raw).Contains('SCENE_IDENTITY_CHECK_PASS')
@{passed=$scenePassed;exitCode=$sceneProcess.ExitCode;args=$sceneArgs;assemblySha256=(Get-FileHash (Join-Path $sceneRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $sceneFolder 'result.json') -Encoding utf8
if(!$scenePassed){throw 'HQ identity probe failed.'}
Write-Output 'Passed HQ identity and migration probe.'
