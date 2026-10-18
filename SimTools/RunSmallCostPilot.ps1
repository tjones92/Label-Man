[CmdletBinding()]
param([string]$RunTag='smallcost-v1', [int]$Weeks=26, [int[]]$Seeds=@(2002), [switch]$Compact,
 [string[]]$Modes=@('legacy','period','home'), [string[]]$ExtraArgs=@(),
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe')
. (Join-Path $PSScriptRoot 'AuditGcEnvironment.ps1')
$ErrorActionPreference='Stop'
$pilotRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$pilotFolder=Join-Path $pilotRoot "SimLogs/$RunTag"
if($RunTag -notmatch '^[a-z0-9-]+$' -or (Test-Path -LiteralPath $pilotFolder) -or $Weeks -lt 1){throw 'Choose a new run tag and positive duration.'}
if(@(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$').Count){throw 'Another game is active.'}
$pilotFlags=@('--enable-genre-market-v2','--enable-artist-population-lifecycle',
 '--local-signals-read-demand','--regional-arrival','--regional-retail-access','--regional-campaign-focus','--economic-label-exits')+$ExtraArgs
New-Item -ItemType Directory -Path $pilotFolder | Out-Null
$pilotManifest=[ordered]@{weeks=$Weeks;flags=$pilotFlags;startedUtc=[datetime]::UtcNow.ToString('o');
 assemblySha256=(Get-FileHash (Join-Path $pilotRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash;results=@()}
foreach($pilotSeed in $Seeds){foreach($pilotMode in $Modes){
 if($pilotMode -notin @('legacy','period','home','legacy-home','paired','paired-home','default')){throw "Unknown mode $pilotMode"}
 $pilotName="$RunTag-$pilotMode-$pilotSeed-$Weeks"
 if(Test-Path -LiteralPath (Join-Path $pilotRoot "SimLogs/$pilotName-weeks.csv")){throw 'Refusing to overwrite a run.'}
 $pilotArgs=@('--headless','--path','.','SimTools/ChartAuditRunner.tscn','--',"--weeks=$Weeks","--run=$pilotName","--seed=$pilotSeed")+$pilotFlags
 if($Compact){$pilotArgs+=@('--calibration','--small-label-finance')}
 if($pilotMode -in @('period','home')){$pilotArgs+='--period-single-costs'}
 if($pilotMode -in @('paired','paired-home')){$pilotArgs+='--period-pressing-costs'}
 if($pilotMode -in @('legacy','legacy-home')){$pilotArgs+='--legacy-pressing-costs'}
 if($pilotMode -in @('home','legacy-home','paired-home')){$pilotArgs+='--small-home-territory'}
 elseif($pilotMode -ne 'default'){$pilotArgs+='--no-small-home-territory'}
 $pilotStart=[datetime]::UtcNow
 $pilotStdout=Join-Path $pilotFolder "$pilotName-console.log"
 $pilotStderr=Join-Path $pilotFolder "$pilotName-errors.log"
 $pilotProcess=Start-Process -FilePath $Godot -ArgumentList $pilotArgs -WorkingDirectory $pilotRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $pilotStdout -RedirectStandardError $pilotStderr
 $null=$pilotProcess.Handle
 while(!$pilotProcess.WaitForExit(10000)){Write-Output "Running $pilotName ($([int]([datetime]::UtcNow-$pilotStart).TotalSeconds)s)."}
 $pilotProcess.WaitForExit()
 $pilotPassed=$pilotProcess.ExitCode -eq 0 -and (Get-Content -LiteralPath $pilotStdout -Raw).Contains("CHART_AUDIT_COMPLETE run=$pilotName weeks=$Weeks") -and !(Select-String -LiteralPath $pilotStderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
 $pilotManifest.results+=@{name=$pilotName;passed=$pilotPassed;exitCode=$pilotProcess.ExitCode;seconds=([datetime]::UtcNow-$pilotStart).TotalSeconds;args=$pilotArgs}
 $pilotManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $pilotFolder 'runs.json') -Encoding utf8
 if(!$pilotPassed){throw "Failed $pilotName; inspect logs."}
 Write-Output "Passed $pilotName."
}}
