[CmdletBinding()]
param([string]$RunTag='scene4-major-fill-seeds-v1', [int]$Weeks=1, [int[]]$Seeds=@(3003,4004,5005,6006,7007,8008,9009,10010),
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe')
$ErrorActionPreference='Stop'
$sceneRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sceneFolder=Join-Path $sceneRoot "SimLogs/$RunTag"
if($RunTag -notmatch '^[a-z0-9-]+$' -or (Test-Path $sceneFolder) -or $Weeks -lt 1){throw 'Choose a new valid run tag and positive duration.'}
$sceneFlags=@('--enable-genre-market-v2','--enable-artist-population-lifecycle','--enable-artist-evolution',
 '--enable-artist-recognition','--enable-managers','--seed-star-canopy','--enable-cowriting','--enable-member-axes',
 '--observe-band-life','--enable-lineup-churn=world','--member-fame-share=0.90','--disable-scene-extended-world','--aggregate-only',
 '--local-scene-identity-audit','--local-scene-persistence-audit','--scene-room-audit','--scene-recruitment-audit')
$sceneJobs=@($Seeds | ForEach-Object { @{mode='recruitment';seed=$_} })
New-Item -ItemType Directory -Path $sceneFolder | Out-Null
$sceneManifest=[ordered]@{revision=(& git -C $sceneRoot rev-parse HEAD).Trim();weeks=$Weeks;flags=$sceneFlags;
 assemblySha256=(Get-FileHash (Join-Path $sceneRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash;
 sourceHashes=@(Get-ChildItem (Join-Path $sceneRoot 'Systems'),(Join-Path $sceneRoot 'Data'),(Join-Path $sceneRoot 'SimTools') -Filter '*.cs' | Get-FileHash | Select-Object Path,Hash);
 godotSha256=(Get-FileHash $Godot).Hash;jobs=$sceneJobs;results=@();maximumParallel=1}
foreach($sceneJob in $sceneJobs){
 $sceneJobWeeks=$Weeks
 if(@(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$').Count){throw 'Another game is active.'}
 if([math]::Floor((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory/1024) -lt 650){throw 'Insufficient memory before audit.'}
 $sceneName="$RunTag-$($sceneJob.mode)-$($sceneJob.seed)"
 $sceneExtra=if($sceneJob.mode -eq 'recruitment'){'--enable-scene-recruitment'}else{'--disable-scene-recruitment'}
 $sceneArgs=@('--headless','--path','.','SimTools/ChartAuditRunner.tscn','--',"--weeks=$sceneJobWeeks","--run=$sceneName","--seed=$($sceneJob.seed)")+$sceneFlags+@($sceneExtra)
 $sceneStdout=Join-Path $sceneFolder "$sceneName-console.log"
 $sceneStart=[datetime]::UtcNow
 $sceneProcess=Start-Process -FilePath $Godot -ArgumentList $sceneArgs -WorkingDirectory $sceneRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $sceneStdout -RedirectStandardError (Join-Path $sceneFolder "$sceneName-errors.log"); $null=$sceneProcess.Handle
 while(!$sceneProcess.WaitForExit(10000)){Write-Output "Running $sceneName."}
 $sceneProcess.WaitForExit()
 $sceneRows=@(Import-Csv (Join-Path $sceneRoot "SimLogs/$sceneName-weeks.csv"))
 $scenePassed=$sceneProcess.ExitCode -eq 0 -and $sceneRows.Count -eq $sceneJobWeeks -and (Get-Content $sceneStdout -Raw).Contains("CHART_AUDIT_COMPLETE run=$sceneName weeks=$sceneJobWeeks")
 $sceneManifest.results+=@{name=$sceneName;mode=$sceneJob.mode;seed=$sceneJob.seed;passed=$scenePassed;exitCode=$sceneProcess.ExitCode;weeks=$sceneRows.Count;seconds=([datetime]::UtcNow-$sceneStart).TotalSeconds;args=$sceneArgs}
 $sceneManifest | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $sceneFolder 'runs.json') -Encoding utf8
 if(!$scenePassed){throw "Failed $sceneName; inspect logs."}
 Write-Output "Passed $sceneName."
}
