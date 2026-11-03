[CmdletBinding()]
param(
 [string]$RunTag='followup-v2',
 [int[]]$Seeds=@(1001,2002,3003,4004),
 [int]$Weeks=209,
 [switch]$DefaultProfile,
 [switch]$FreshWorld,
 [switch]$MaterialDeadline,
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
if($RunTag -notmatch '^[a-z0-9-]+$'){throw 'Use a simple run tag.'}
$parallelRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$parallelDir=Join-Path $parallelRoot "SimLogs/$RunTag"
New-Item -ItemType Directory -Force -Path $parallelDir | Out-Null
$env:LABELMAN_WORKSTATION_GC='1'; $env:DOTNET_gcServer='0'; $env:DOTNET_GCHeapHardLimit='A0000000'
$parallelAssembly=Join-Path $parallelRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$parallelHash=(Get-FileHash $parallelAssembly).Hash
$parallelCommon=@('--enable-genre-market-v2','--enable-artist-population-lifecycle',
 '--causal-media-atmosphere','--media-recognition','--fame-registration','--observe-following','--media-residencies',
 '--follower-demand','--calibration','--chart-history','--label-release-slate','--label-release-capacity','--label-decision-audit')
if($DefaultProfile){$parallelCommon+='--default-simulation-profile'}
$parallelWorlds=@{1001='media-p3-1001-1963';2002='asc-2002-1963';3003='fdw-3003-1963';4004='fdw-4004-1963'}
$parallelPlanPath=Join-Path $parallelDir 'parallel-plan.json'
if(Test-Path $parallelPlanPath){
 $parallelPrevious=Get-Content $parallelPlanPath -Raw | ConvertFrom-Json -AsHashtable
 if($parallelPrevious.assemblySha256 -ne $parallelHash -or $parallelPrevious.weeks -ne $Weeks -or
    ($parallelPrevious.seeds -join ',') -ne ($Seeds -join ',') -or
    ($parallelPrevious.commonFlags -join ',') -ne ($parallelCommon -join ',') -or
    [bool]$parallelPrevious.freshWorld -ne [bool]$FreshWorld -or
    [bool]$parallelPrevious.materialDeadline -ne [bool]$MaterialDeadline){throw 'Existing parallel plan has a different assembly/window/seeds/profile.'}
}
$parallelSerialPath=Join-Path $parallelDir 'plan.json'
if(Test-Path $parallelSerialPath){
 $parallelSerial=Get-Content $parallelSerialPath -Raw | ConvertFrom-Json -AsHashtable
 if($parallelSerial.assemblySha256 -ne $parallelHash){throw 'Serial handoff uses a different assembly.'}
}
$parallelPlan=[ordered]@{assemblySha256=$parallelHash;weeks=$Weeks;seeds=$Seeds;commonFlags=$parallelCommon;
 treatment=@('--programme-follow-up-scheduling')+$(if($MaterialDeadline){@('--programme-follow-up-material')}else{@('--programme-follow-up-delivery')});maximumParallel=2;
 startUtc=[datetime]::UtcNow.ToString('o');runs=@();adopted=@();
 profile=if($DefaultProfile){'lp-retail-v2 plus accepted causal media/follower rates'}else{'Phase 4 media screen plus raw programme in both arms'};
 freshWorld=[bool]$FreshWorld;
 materialDeadline=[bool]$MaterialDeadline;
 profileSourceSha256=(Get-FileHash (Join-Path $parallelRoot 'Systems/SimulationArguments.cs')).Hash}
$parallelJobs=@()
foreach($parallelSeed in $Seeds){foreach($parallelArm in @('control','treatment')){
 if(!$parallelWorlds.ContainsKey($parallelSeed)){throw 'No matched world for seed.'}
 $parallelName="$RunTag-$parallelArm-$parallelSeed"
 $parallelJobs+=@{name=$parallelName;arm=$parallelArm;seed=$parallelSeed;world=if($FreshWorld){$null}else{$parallelWorlds[$parallelSeed]}}
}}
function Test-ParallelComplete($job){
 $log=Join-Path $parallelRoot "SimLogs/$($job.name)-console.log"
 $errors=Join-Path $parallelRoot "SimLogs/$($job.name)-errors.log"
 return (Test-Path $log) -and (Select-String -LiteralPath $log -SimpleMatch "CHART_AUDIT_COMPLETE run=$($job.name) weeks=$Weeks" -Quiet) -and
  (!(Test-Path $errors) -or !(Select-String -LiteralPath $errors -Pattern 'ERROR:|Exception' -Quiet))
}
$parallelActive=@{}
$parallelProcesses=@(Get-CimInstance Win32_Process | Where-Object {$_.Name -like '*Godot*console.exe'})
foreach($parallelProcess in $parallelProcesses){
 foreach($parallelJob in $parallelJobs){
  if($parallelProcess.CommandLine -match ('--run='+[regex]::Escape($parallelJob.name)+'(?:\s|$)')){
   $parallelHandle=[Diagnostics.Process]::GetProcessById($parallelProcess.ProcessId); $null=$parallelHandle.Handle
   $parallelActive[$parallelJob.name]=@{job=$parallelJob;process=$parallelHandle;started=$parallelHandle.StartTime.ToUniversalTime();adopted=$true}
   $parallelPlan.adopted+=@{name=$parallelJob.name;processId=$parallelProcess.ProcessId}
   Write-Output "Adopted $($parallelJob.name) pid=$($parallelProcess.ProcessId)."
  }
 }
}
if($parallelActive.Count -gt 2){throw 'More than two active worlds already exist.'}
$parallelPending=New-Object 'System.Collections.Generic.List[object]'
foreach($parallelJob in $parallelJobs){
 if($parallelActive.ContainsKey($parallelJob.name)){continue}
 if(Test-ParallelComplete $parallelJob){
  $parallelPlan.runs+=@{name=$parallelJob.name;seed=$parallelJob.seed;arm=$parallelJob.arm;passed=$true;reused=$true}
 }else{$parallelPending.Add($parallelJob)}
}
while($parallelPending.Count -or $parallelActive.Count){
 while($parallelPending.Count -and $parallelActive.Count -lt 2){
  if((Get-FileHash $parallelAssembly).Hash -ne $parallelHash){throw 'Assembly changed during screen.'}
  $parallelJob=$parallelPending[0]; $parallelPending.RemoveAt(0)
  $parallelName=$parallelJob.name
  $parallelUser=Join-Path $parallelDir "userdata/$parallelName"
  New-Item -ItemType Directory -Force -Path $parallelUser | Out-Null
  $env:APPDATA=$parallelUser
  $parallelFlags=$parallelCommon+@("--weeks=$Weeks","--seed=$($parallelJob.seed)","--run=$parallelName","--checkpoint-world=$parallelName-checkpoint")
  if($parallelJob.world){$parallelFlags+="--resume-world=$($parallelJob.world)"}
  if($parallelJob.arm -eq 'treatment'){$parallelFlags+=$parallelPlan.treatment}
  $parallelArgs=@('--headless','--path',$parallelRoot,'--log-file',"$parallelRoot/SimLogs/$parallelName-engine.log",'SimTools/ChartAuditRunner.tscn','--')+$parallelFlags
  $parallelStarted=[datetime]::UtcNow
  $parallelChild=Start-Process -FilePath $Godot -ArgumentList $parallelArgs -WorkingDirectory $parallelRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput "$parallelRoot/SimLogs/$parallelName-console.log" -RedirectStandardError "$parallelRoot/SimLogs/$parallelName-errors.log"
  $null=$parallelChild.Handle
  $parallelActive[$parallelName]=@{job=$parallelJob;process=$parallelChild;started=$parallelStarted;args=$parallelArgs;adopted=$false}
  Write-Output "Started $parallelName pid=$($parallelChild.Id); active=$($parallelActive.Count)."
 }
 foreach($parallelName in @($parallelActive.Keys)){
  $parallelItem=$parallelActive[$parallelName]
  if(!$parallelItem.process.HasExited){continue}
  $parallelItem.process.WaitForExit()
  $parallelPassed=$parallelItem.process.ExitCode -eq 0 -and (Test-ParallelComplete $parallelItem.job)
  $parallelWorldHash=if($parallelItem.job.world){(Get-FileHash (Join-Path $parallelRoot "SimLogs/worlds/$($parallelItem.job.world).world.json.gz")).Hash}else{$null}
  $parallelPlan.runs+=@{name=$parallelName;seed=$parallelItem.job.seed;arm=$parallelItem.job.arm;passed=$parallelPassed;
   exitCode=$parallelItem.process.ExitCode;seconds=([datetime]::UtcNow-$parallelItem.started).TotalSeconds;
   args=$parallelItem.args;adopted=$parallelItem.adopted;worldSha256=$parallelWorldHash}
  $parallelActive.Remove($parallelName)
  Write-Output "Completed $parallelName passed=$parallelPassed."
  $parallelPlan | ConvertTo-Json -Depth 10 | Set-Content $parallelPlanPath -Encoding utf8
  if(!$parallelPassed){throw "$parallelName failed; the other running world is left intact."}
 }
 $parallelPlan | ConvertTo-Json -Depth 10 | Set-Content $parallelPlanPath -Encoding utf8
 if($parallelActive.Count){
  Start-Sleep -Seconds 20
  foreach($parallelName in @($parallelActive.Keys)){
   $parallelLast=Get-Content "$parallelRoot/SimLogs/$parallelName-console.log" -Tail 1 -ErrorAction SilentlyContinue
   Write-Output "$parallelName : $parallelLast"
  }
 }
}
