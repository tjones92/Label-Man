[CmdletBinding()]
param(
 [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='lpd-v1',
 [string]$Reference='lpc-v1-both',
 [ValidateRange(106,210)][int]$Weeks=106,
 [int[]]$Seeds=@(1001),
 [ValidateRange(.93,1)][double]$WeeklyDecay=.985,
 [ValidateRange(1,2)][int]$MaxParallel=2,
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$probeRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$probeDir=Join-Path $probeRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $probeDir){throw 'Choose an unused run tag.'}
if(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$'){throw 'Another game is active.'}
if(!$Seeds.Count -or @($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Use distinct seeds.'}
$probeReference=Get-Content -LiteralPath (Join-Path $probeRoot "SimLogs/$Reference/runs.json") -Raw | ConvertFrom-Json
$probeFlags=@($probeReference.extraFlags)
if(@($probeFlags | Where-Object {$_ -match '^--lp-(demand|catalog)'}).Count){throw 'Reference must exclude the demand probe.'}
$probeHash=(Get-FileHash -LiteralPath (Join-Path $probeRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash
$probeSlow='--lp-catalog-weekly-decay='+$WeeklyDecay.ToString([Globalization.CultureInfo]::InvariantCulture)
New-Item -ItemType Directory -Path $probeDir | Out-Null
$probePlan=[ordered]@{reference=$Reference;weeks=$Weeks;seeds=$Seeds;weeklyDecay=$WeeklyDecay;assemblySha256=$probeHash;bundle=$probeFlags;checks=@();runs=@();calibrationAccepted=$false}
foreach($probeCheck in @('native','off13','roundtrip')) {
 $probeName="$RunTag-$probeCheck"
 $probeScene=if($probeCheck -eq 'off13'){'SimTools/ChartAuditRunner.tscn'}else{'SimTools/SaveLoadRoundTripRunner.tscn'}
 $probeExtra=if($probeCheck -eq 'native'){@('--lp-demand-check')}else{@('--weeks=13',"--seed=$($Seeds[0])",'--calibration','--small-label-finance')+$probeFlags}
 if($probeCheck -eq 'off13'){$probeExtra+="--run=$probeName"}
 if($probeCheck -eq 'roundtrip'){$probeExtra+=$probeSlow}
 $probeArgs=@('--headless','--path','.', $probeScene,'--')+$probeExtra
 $probeStdout=Join-Path $probeDir "$probeCheck-console.log"; $probeStderr=Join-Path $probeDir "$probeCheck-errors.log"
 $probeProcess=Start-Process -FilePath $Godot -ArgumentList $probeArgs -WorkingDirectory $probeRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $probeStdout -RedirectStandardError $probeStderr
 $null=$probeProcess.Handle
 while(!$probeProcess.WaitForExit(10000)){Write-Output "LP demand $probeCheck running."}
 $probeProcess.WaitForExit()
 $probeMarker=switch($probeCheck){'native'{'LP_DEMAND_CHECK passed'} 'off13'{"CHART_AUDIT_COMPLETE run=$probeName weeks=13"} default{'SAVELOAD_ROUNDTRIP_PASS'}}
 $probePassed=$probeProcess.ExitCode -eq 0 -and (Get-Content -LiteralPath $probeStdout -Raw).Contains($probeMarker) -and !(Select-String -LiteralPath $probeStderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
 $probePlan.checks+=@{check=$probeCheck;passed=$probePassed;args=$probeArgs}
 $probePlan | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $probeDir 'plan.json') -Encoding utf8
 if(!$probePassed){throw "LP demand $probeCheck failed."}
}
$probeJobs=[Collections.Generic.Queue[object]]::new()
foreach($probeArm in @('observe','slow')) {foreach($probeSeed in $Seeds) {
 $probeName="$RunTag-$probeArm-default-$probeSeed-$Weeks"
 if(Test-Path -LiteralPath (Join-Path $probeRoot "SimLogs/$probeName-label-sales-weekly.csv")){throw 'Refusing to overwrite output.'}
 $probeExtra=@($probeFlags)+@('--lp-demand-audit'); if($probeArm -eq 'slow'){$probeExtra+=$probeSlow}
 $probeJobs.Enqueue(@{arm=$probeArm;seed=$probeSeed;name=$probeName;flags=$probeExtra})
}}
$probeActive=[Collections.Generic.List[object]]::new()
try {
 while($probeJobs.Count -or $probeActive.Count) {
  while($probeJobs.Count -and $probeActive.Count -lt $MaxParallel) {
   if((Get-FileHash -LiteralPath (Join-Path $probeRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash -ne $probeHash){throw 'Assembly changed.'}
   $probeJob=$probeJobs.Dequeue()
   $probeJob.stdout=Join-Path $probeDir "$($probeJob.name)-console.log"; $probeJob.stderr=Join-Path $probeDir "$($probeJob.name)-errors.log"
   $probeJob.args=@('--headless','--log-file',(Join-Path $probeDir "$($probeJob.name)-engine.log"),'--path','.', 'SimTools/ChartAuditRunner.tscn','--',"--weeks=$Weeks","--run=$($probeJob.name)","--seed=$($probeJob.seed)",'--calibration','--small-label-finance')+$probeJob.flags
   $probeJob.started=[datetime]::UtcNow
   $probeJob.process=Start-Process -FilePath $Godot -ArgumentList $probeJob.args -WorkingDirectory $probeRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $probeJob.stdout -RedirectStandardError $probeJob.stderr -Environment @{LABELMAN_WORKSTATION_GC='1';DOTNET_gcServer='0';DOTNET_GCHeapHardLimit='70000000'}
   $null=$probeJob.process.Handle; $probeActive.Add($probeJob)
   Write-Output "Started $($probeJob.name)."
  }
  foreach($probeJob in @($probeActive.ToArray())) {
   if(!$probeJob.process.HasExited){continue}
   $probeJob.process.WaitForExit()
   $probePassed=$probeJob.process.ExitCode -eq 0 -and (Get-Content -LiteralPath $probeJob.stdout -Raw).Contains("CHART_AUDIT_COMPLETE run=$($probeJob.name) weeks=$Weeks") -and !(Select-String -LiteralPath $probeJob.stderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
   $probePlan.runs+=@{arm=$probeJob.arm;seed=$probeJob.seed;name=$probeJob.name;passed=$probePassed;exitCode=$probeJob.process.ExitCode;seconds=([datetime]::UtcNow-$probeJob.started).TotalSeconds;args=$probeJob.args}
   $probePlan | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $probeDir 'plan.json') -Encoding utf8
   $null=$probeActive.Remove($probeJob)
   if(!$probePassed){throw "Failed $($probeJob.name)."}
   Write-Output "Passed $($probeJob.name)."
  }
  if($probeActive.Count){Start-Sleep -Seconds 10; Write-Output "LP demand: $($probePlan.runs.Count) completed; $($probeActive.Count) running."}
 }
 if((Get-FileHash -LiteralPath (Join-Path $probeRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash -ne $probeHash){throw 'Assembly changed during screen.'}
} finally {
 foreach($probeJob in $probeActive){
  $probeOwned=Get-CimInstance Win32_Process | Where-Object {$_.Name -match '^Godot' -and $_.CommandLine -like "*--run=$($probeJob.name)*"}
  foreach($probeProcess in $probeOwned){Stop-Process -Id $probeProcess.ProcessId -ErrorAction SilentlyContinue}
 }
}
