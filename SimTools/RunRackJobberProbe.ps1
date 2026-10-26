[CmdletBinding()]
param(
 [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='rkj-v1',
 # The rcl-v1 bundle (lpc-v1-both + LP demand audit); clubs are added to every arm here.
 [string]$Reference='rcl-v1',
 # Its clubs arm is the flag-off replay reference for the 13-week check.
 [string]$ReplayRun='rcl-v1-clubs-default-1001-106',
 [ValidateSet('control','racks','overlap','returns10','returns20','budget','budgetadd','budgetturnover45','budgetprice125','budgetfullswitch')][string[]]$Arms=@('control','racks'),
 # 0 keeps the code's frozen default scale.
 [double]$Scale=0,
 # V2 supply screen only: emits catalogue aggregates; books no budget units.
 [switch]$BudgetLpAudit,
 # Same simulation with compact LP settlement diagnostics instead of regional raw-demand shadows.
 [switch]$Compact,
 [ValidateRange(13,530)][int]$Weeks=106,
 [int[]]$Seeds=@(1001),
 [ValidateRange(1,2)][int]$MaxParallel=2,
 [switch]$SkipChecks,
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$probeRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$probeDir=Join-Path $probeRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $probeDir){throw 'Choose an unused run tag.'}
if(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$'){throw 'Another game is active.'}
if(!$Seeds.Count -or @($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Use distinct seeds.'}
$probeReference=Get-Content -LiteralPath (Join-Path $probeRoot "SimLogs/$Reference/plan.json") -Raw | ConvertFrom-Json
$probeFlags=@($probeReference.bundle)
if(@($probeFlags | Where-Object {$_ -match '^--(rack-|record-club|budget-)'}).Count){throw 'Reference bundle must exclude clubs, racks and budget products.'}
$probeFlags+=@('--record-clubs')
$probeReplayFlags=@($probeFlags)
if($Compact){$probeFlags=@($probeFlags | Where-Object {$_ -notin @('--lp-demand-audit','--followup-chart-trace','--head-hit-attribution','--lp-capacity-audit')}); $probeFlags+='--rack-validation-audit'}
$probeRackFlags=@('--rack-jobbers'); if($Scale -gt 0){$probeRackFlags+=('--rack-shopper-scale='+$Scale.ToString([Globalization.CultureInfo]::InvariantCulture))}
if($BudgetLpAudit){$probeRackFlags+='--budget-lp-audit'}
$probeHash=(Get-FileHash -LiteralPath (Join-Path $probeRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash
New-Item -ItemType Directory -Path $probeDir | Out-Null
$probePlan=[ordered]@{reference=$Reference;replayRun=$ReplayRun;arms=$Arms;weeks=$Weeks;seeds=$Seeds;scale=$Scale;assemblySha256=$probeHash;bundle=$probeFlags;rackFlags=$probeRackFlags;scenarioDefaults=@{budgetPrice=1.75;budgetShopperScale=0.08;budgetSubstitution=0.25;rackTurnover=30};checks=@();runs=@();calibrationAccepted=$false}
$probePlan | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $probeDir 'plan.json') -Encoding utf8
$probeChecks=if($SkipChecks){@()}else{@('native','off13','roundtrip')}
if($BudgetLpAudit -and !$SkipChecks){$probeChecks+=@('budgetnative')}
if(@($Arms | Where-Object {$_ -like 'budget*'}).Count -and !$SkipChecks){$probeChecks+=@('budgetproducts','budgetroundtrip')}
foreach($probeCheck in $probeChecks) {
 $probeName="$RunTag-$probeCheck"
 $probeScene=if($probeCheck -eq 'off13'){'SimTools/ChartAuditRunner.tscn'}else{'SimTools/SaveLoadRoundTripRunner.tscn'}
 $probeExtra=if($probeCheck -eq 'native'){@('--rack-jobber-check')}elseif($probeCheck -eq 'budgetnative'){@('--budget-lp-catalogue-check')}elseif($probeCheck -eq 'budgetproducts'){@('--budget-lp-check')}else{@('--weeks=13',"--seed=$($Seeds[0])",'--calibration','--small-label-finance')+$probeFlags}
 if($probeCheck -eq 'off13'){$probeExtra=@('--weeks=13',"--seed=$($Seeds[0])",'--calibration','--small-label-finance')+$probeReplayFlags}
 if($probeCheck -eq 'off13'){$probeExtra+="--run=$probeName"}
 if($probeCheck -eq 'roundtrip'){$probeExtra+=$probeRackFlags}
 if($probeCheck -eq 'budgetroundtrip'){$probeExtra+=$probeRackFlags; $probeExtra+='--budget-lps'}
 $probeArgs=@('--headless','--path','.', $probeScene,'--')+$probeExtra
 $probeStdout=Join-Path $probeDir "$probeCheck-console.log"; $probeStderr=Join-Path $probeDir "$probeCheck-errors.log"
 $probeProcess=Start-Process -FilePath $Godot -ArgumentList $probeArgs -WorkingDirectory $probeRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $probeStdout -RedirectStandardError $probeStderr
 $null=$probeProcess.Handle
 while(!$probeProcess.WaitForExit(10000)){Write-Output "Rack jobbers $probeCheck running."}
 $probeProcess.WaitForExit()
 $probeMarker=switch($probeCheck){'native'{'RACK_JOBBER_CHECKS_OK'} 'budgetnative'{'BUDGET_LP_CATALOGUE_CHECKS_OK'} 'budgetproducts'{'BUDGET_LP_CHECKS_OK'} 'off13'{"CHART_AUDIT_COMPLETE run=$probeName weeks=13"} default{'SAVELOAD_ROUNDTRIP_PASS'}}
 $probePassed=$probeProcess.ExitCode -eq 0 -and (Get-Content -LiteralPath $probeStdout -Raw).Contains($probeMarker) -and !(Select-String -LiteralPath $probeStderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
 $probePlan.checks+=@{check=$probeCheck;passed=$probePassed;args=$probeArgs}
 $probePlan | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $probeDir 'plan.json') -Encoding utf8
 if(!$probePassed){throw "Rack jobbers $probeCheck failed."}
}
$probeJobs=[Collections.Generic.Queue[object]]::new()
foreach($probeArm in $Arms) {foreach($probeSeed in $Seeds) {
 $probeName="$RunTag-$probeArm-default-$probeSeed-$Weeks"
 if(Test-Path -LiteralPath (Join-Path $probeRoot "SimLogs/$probeName-label-sales-weekly.csv")){throw 'Refusing to overwrite output.'}
 $probeExtra=@($probeFlags); if($probeArm -ne 'control'){$probeExtra+=$probeRackFlags}
 if($probeArm -eq 'overlap'){$probeExtra+='--rack-shared-exhaustion'}
 if($probeArm -eq 'returns10'){$probeExtra+='--rack-returns=0.1'}
 if($probeArm -eq 'returns20'){$probeExtra+='--rack-returns=0.2'}
 if($probeArm -like 'budget*'){$probeExtra+='--budget-lps'; $probeBudgetPool=if($probeArm -eq 'budgetfullswitch'){'0'}else{'0.08'}; $probeExtra+="--budget-shopper-scale=$probeBudgetPool"; $probeBudgetPrice=if($probeArm -eq 'budgetprice125'){'1.25'}else{'1.75'}; $probeExtra+="--budget-price=$probeBudgetPrice"}
 if($probeArm -eq 'budgetadd'){$probeExtra+='--budget-substitution=0'}
 if($probeArm -eq 'budgetfullswitch'){$probeExtra+='--budget-substitution=1'}
 if($probeArm -eq 'budgetturnover45'){$probeExtra+='--rack-turnover=45'}
 $probeJobs.Enqueue(@{arm=$probeArm;seed=$probeSeed;name=$probeName;flags=$probeExtra})
}}
$probeActive=[Collections.Generic.List[object]]::new()
# Windows PowerShell 5.1 has no Start-Process -Environment: the arms inherit these from this process instead.
$probeSavedEnv=@{}
foreach($probeVar in @{LABELMAN_WORKSTATION_GC='1';DOTNET_gcServer='0';DOTNET_GCHeapHardLimit='70000000'}.GetEnumerator()){
 $probeSavedEnv[$probeVar.Key]=[Environment]::GetEnvironmentVariable($probeVar.Key)
 [Environment]::SetEnvironmentVariable($probeVar.Key,$probeVar.Value)
}
try {
 while($probeJobs.Count -or $probeActive.Count) {
  while($probeJobs.Count -and $probeActive.Count -lt $MaxParallel) {
   if((Get-FileHash -LiteralPath (Join-Path $probeRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash -ne $probeHash){throw 'Assembly changed.'}
   $probeJob=$probeJobs.Dequeue()
   $probeJob.stdout=Join-Path $probeDir "$($probeJob.name)-console.log"; $probeJob.stderr=Join-Path $probeDir "$($probeJob.name)-errors.log"
   $probeJob.args=@('--headless','--log-file',(Join-Path $probeDir "$($probeJob.name)-engine.log"),'--path','.', 'SimTools/ChartAuditRunner.tscn','--',"--weeks=$Weeks","--run=$($probeJob.name)","--seed=$($probeJob.seed)",'--calibration','--small-label-finance')+$probeJob.flags
   $probeJob.started=[datetime]::UtcNow
   $probeJob.process=Start-Process -FilePath $Godot -ArgumentList $probeJob.args -WorkingDirectory $probeRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $probeJob.stdout -RedirectStandardError $probeJob.stderr
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
  if($probeActive.Count){Start-Sleep -Seconds 10}
 }
 if((Get-FileHash -LiteralPath (Join-Path $probeRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash -ne $probeHash){throw 'Assembly changed during screen.'}
} finally {
 foreach($probeVar in $probeSavedEnv.GetEnumerator()){[Environment]::SetEnvironmentVariable($probeVar.Key,$probeVar.Value)}
 foreach($probeJob in $probeActive){
  $probeOwned=Get-CimInstance Win32_Process | Where-Object {$_.Name -match '^Godot' -and $_.CommandLine -like "*--run=$($probeJob.name)*"}
  foreach($probeProcess in $probeOwned){Stop-Process -Id $probeProcess.ProcessId -ErrorAction SilentlyContinue}
 }
}
