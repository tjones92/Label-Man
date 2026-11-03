[CmdletBinding()]
param(
 [string]$RunTag='followup-v2',
 [int[]]$Seeds=@(1001,2002,3003,4004),
 [int]$Weeks=209,
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
if($RunTag -notmatch '^[a-z0-9-]+$'){throw 'Use a simple run tag.'}
$screenRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$screenDir=Join-Path $screenRoot "SimLogs/$RunTag"
New-Item -ItemType Directory -Force -Path $screenDir | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $screenDir 'userdata') | Out-Null
$env:APPDATA=Join-Path $screenDir 'userdata'
$env:LABELMAN_WORKSTATION_GC='1'; $env:DOTNET_gcServer='0'; $env:DOTNET_GCHeapHardLimit='A0000000'
$screenAssembly=Join-Path $screenRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$screenHash=(Get-FileHash $screenAssembly).Hash
$screenCommon=@('--enable-genre-market-v2','--enable-artist-population-lifecycle',
 '--causal-media-atmosphere','--media-recognition','--fame-registration','--observe-following','--media-residencies',
 '--follower-demand','--calibration','--chart-history','--label-release-slate','--label-release-capacity','--label-decision-audit')
$screenWorlds=@{1001='media-p3-1001-1963';2002='asc-2002-1963';3003='fdw-3003-1963';4004='fdw-4004-1963'}
$screenPlanPath=Join-Path $screenDir 'plan.json'
if(Test-Path $screenPlanPath){
 $screenPlan=Get-Content $screenPlanPath -Raw | ConvertFrom-Json -AsHashtable
 if($screenPlan.assemblySha256 -ne $screenHash -or $screenPlan.weeks -ne $Weeks -or
    ($screenPlan.seeds -join ',') -ne ($Seeds -join ',')){throw 'Existing plan does not match this assembly/window/seeds.'}
}else{
 $screenPlan=[ordered]@{assemblySha256=$screenHash;weeks=$Weeks;seeds=$Seeds;commonFlags=$screenCommon;
  treatment=@('--programme-follow-up-scheduling','--programme-follow-up-delivery');runs=@();
  startUtc=[datetime]::UtcNow.ToString('o');profile='Phase 4 media screen plus programme in both arms; not game default'}
}
foreach($screenSeed in $Seeds){foreach($screenArm in @('control','treatment')){
 $screenName="$RunTag-$screenArm-$screenSeed"
 if(@($screenPlan.runs | Where-Object {$_.name -eq $screenName -and $_.passed}).Count){continue}
 if((Get-FileHash $screenAssembly).Hash -ne $screenHash){throw 'Assembly changed during screen.'}
 if(@(Get-Process | Where-Object ProcessName -Match '^Godot').Count){throw 'Another Godot process is active.'}
 if(!$screenWorlds.ContainsKey($screenSeed)){throw "No matched world for $screenSeed"}
 $screenWorld=Join-Path $screenRoot "SimLogs/worlds/$($screenWorlds[$screenSeed]).world.json.gz"
 $screenFlags=$screenCommon+@("--weeks=$Weeks","--seed=$screenSeed","--run=$screenName","--resume-world=$($screenWorlds[$screenSeed])","--checkpoint-world=$screenName-checkpoint")
 if($screenArm -eq 'treatment'){$screenFlags+=$screenPlan.treatment}
 $screenLog=Join-Path $screenRoot "SimLogs/$screenName-console.log"
 $screenErrors=Join-Path $screenRoot "SimLogs/$screenName-errors.log"
 $screenEngine=Join-Path $screenRoot "SimLogs/$screenName-engine.log"
 $screenArgs=@('--headless','--path',$screenRoot,'--log-file',$screenEngine,'SimTools/ChartAuditRunner.tscn','--')+$screenFlags
 $screenStarted=[datetime]::UtcNow
 $screenProcess=Start-Process -FilePath $Godot -ArgumentList $screenArgs -WorkingDirectory $screenRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $screenLog -RedirectStandardError $screenErrors
 $null=$screenProcess.Handle
 Write-Output "Started $screenName ($Weeks weeks)."
 while(!$screenProcess.WaitForExit(30000)){
  $screenLast=Get-Content $screenLog -Tail 1 -ErrorAction SilentlyContinue
  Write-Output "$screenName : $screenLast"
 }
 $screenProcess.WaitForExit()
 $screenPassed=$screenProcess.ExitCode -eq 0 -and (Select-String -LiteralPath $screenLog -SimpleMatch "CHART_AUDIT_COMPLETE run=$screenName weeks=$Weeks" -Quiet) -and !(Select-String -LiteralPath $screenErrors -Pattern 'ERROR:|Exception' -Quiet)
 $screenPlan.runs+=@{name=$screenName;arm=$screenArm;seed=$screenSeed;passed=$screenPassed;exitCode=$screenProcess.ExitCode;
  seconds=([datetime]::UtcNow-$screenStarted).TotalSeconds;args=$screenArgs;worldSha256=(Get-FileHash $screenWorld).Hash}
 $screenPlan | ConvertTo-Json -Depth 10 | Set-Content $screenPlanPath -Encoding utf8
 if(!$screenPassed){throw "$screenName failed; inspect logs."}
 Write-Output "Completed $screenName."
}}
