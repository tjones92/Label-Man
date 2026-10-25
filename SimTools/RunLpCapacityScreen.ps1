[CmdletBinding()]
param(
 [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='lpc-v1',
 [int[]]$Seeds=@(1001),
 [ValidateRange(106,574)][int]$Weeks=106,
 [ValidateRange(1,4)][double]$ProgrammeScale=2,
 [ValidateRange(.35,.65)][double]$PurchaseShare=.48,
 [ValidateRange(1,2)][int]$MaxParallel=2,
 [string]$Reference='label-project-v7-judged',
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$lpRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$lpOutput=Join-Path $lpRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $lpOutput){throw 'Choose a new run tag.'}
if(!$Seeds.Count -or @($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Use distinct seeds.'}
if(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$'){throw 'Another game is active.'}
New-Item -ItemType Directory -Path $lpOutput | Out-Null
$lpPrior=Get-Content -LiteralPath (Join-Path $lpRoot "SimLogs/$Reference/runs.json") -Raw | ConvertFrom-Json
$lpBase=@($lpPrior.extraFlags)
if(@($lpBase | Where-Object {$_ -match '^--(lp-|programme-act-cadence|earned-roster-capacity|single-capital-risk|single-forecast-realization|album-purchase-volume)'}).Count){throw 'Use the judged base without the other probes.'}
$lpProgramme='--lp-programme-scale='+$ProgrammeScale.ToString([Globalization.CultureInfo]::InvariantCulture)
$lpPurchase='--lp-purchase-share='+$PurchaseShare.ToString([Globalization.CultureInfo]::InvariantCulture)
$lpHash=(Get-FileHash -LiteralPath (Join-Path $lpRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash
$lpPlan=[ordered]@{reference=$Reference;weeks=$Weeks;seeds=$Seeds;programmeScale=$ProgrammeScale;purchaseShare=$PurchaseShare;
 maxParallel=$MaxParallel;assemblySha256=$lpHash;bundle=$lpBase;checks=@();groups=@();calibrationAccepted=$false;
 purchaseMechanism='Additional LP-only local capacity after unchanged mixed allocation; no extra single or spillover budget.'}
foreach($lpCheck in @('native','roundtrip')) {
 $lpExtra=if($lpCheck -eq 'native'){@('--lp-capacity-check')}else{@('--weeks=13',"--seed=$($Seeds[0])",'--calibration','--small-label-finance')+$lpBase+@($lpProgramme,$lpPurchase)}
 $lpArgs=@('--headless','--path','.', 'SimTools/SaveLoadRoundTripRunner.tscn','--')+$lpExtra
 $lpStdout=Join-Path $lpOutput "$lpCheck-console.log"; $lpStderr=Join-Path $lpOutput "$lpCheck-errors.log"
 $lpProcess=Start-Process -FilePath $Godot -ArgumentList $lpArgs -WorkingDirectory $lpRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $lpStdout -RedirectStandardError $lpStderr
 $null=$lpProcess.Handle
 while(!$lpProcess.WaitForExit(10000)){Write-Output "Checking LP $lpCheck."}
 $lpProcess.WaitForExit()
 $lpMarker=if($lpCheck -eq 'native'){'LP_CAPACITY_CHECK passed'}else{'SAVELOAD_ROUNDTRIP_PASS'}
 $lpPassed=$lpProcess.ExitCode -eq 0 -and (Get-Content -LiteralPath $lpStdout -Raw).Contains($lpMarker) -and !(Select-String -LiteralPath $lpStderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
 $lpPlan.checks+=@{check=$lpCheck;passed=$lpPassed;args=$lpArgs}
 $lpPlan | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $lpOutput 'plan.json') -Encoding utf8
 if(!$lpPassed){throw "LP $lpCheck failed; inspect logs."}
}
$lpJobs=[Collections.Generic.Queue[object]]::new()
foreach($lpArm in @('control','programme','purchase','both')) {
 $lpExtra=@('--lp-capacity-audit')
 if($lpArm -in @('programme','both')){$lpExtra+=$lpProgramme}
 if($lpArm -in @('purchase','both')){$lpExtra+=$lpPurchase}
 $lpTag="$RunTag-$lpArm"; $lpDir=Join-Path $lpRoot "SimLogs/$lpTag"
 if(Test-Path -LiteralPath $lpDir){throw 'Refusing to overwrite an arm.'}
 New-Item -ItemType Directory -Path $lpDir | Out-Null
 $lpManifest=[ordered]@{weeks=$Weeks;profile='game';extraFlags=@($lpBase+$lpExtra);seeds=$Seeds;modes=@('default');assemblySha256=$lpHash;results=@()}
 $lpPlan.groups+=@{arm=$lpArm;tag=$lpTag;flags=$lpExtra}
 foreach($lpSeed in $Seeds) {
  $lpName="$lpTag-default-$lpSeed-$Weeks"
  $lpJobs.Enqueue(@{arm=$lpArm;tag=$lpTag;seed=$lpSeed;name=$lpName;dir=$lpDir;manifest=$lpManifest})
 }
}
$lpPlan | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $lpOutput 'plan.json') -Encoding utf8
# The audit scene has no UI/autosaves. No save/checkpoint flags are supplied. Each process writes distinct
# CSV/console/engine paths. Native checks (which serialize worlds) above run alone. Cap concurrent heaps.
$lpActive=[Collections.Generic.List[object]]::new()
$lpDone=0; $lpTotal=$lpJobs.Count; $lpLast=[datetime]::UtcNow
try {
 while($lpJobs.Count -or $lpActive.Count) {
  while($lpJobs.Count -and $lpActive.Count -lt $MaxParallel) {
   if((Get-FileHash -LiteralPath (Join-Path $lpRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash -ne $lpHash){throw 'Assembly changed.'}
   $lpJob=$lpJobs.Dequeue()
   $lpJob.stdout=Join-Path $lpJob.dir "$($lpJob.name)-console.log"; $lpJob.stderr=Join-Path $lpJob.dir "$($lpJob.name)-errors.log"
   $lpEngine=Join-Path $lpJob.dir "$($lpJob.name)-engine.log"
   $lpJob.args=@('--headless','--log-file',$lpEngine,'--path','.', 'SimTools/ChartAuditRunner.tscn','--',"--weeks=$Weeks","--run=$($lpJob.name)","--seed=$($lpJob.seed)",'--calibration','--small-label-finance')+@($lpJob.manifest.extraFlags)
   $lpJob.started=[datetime]::UtcNow
   $lpJob.process=Start-Process -FilePath $Godot -ArgumentList $lpJob.args -WorkingDirectory $lpRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $lpJob.stdout -RedirectStandardError $lpJob.stderr -Environment @{LABELMAN_WORKSTATION_GC='1';DOTNET_gcServer='0';DOTNET_GCHeapHardLimit='70000000'}
   $null=$lpJob.process.Handle; $lpActive.Add($lpJob)
   Write-Output "Started $($lpJob.name)."
  }
  foreach($lpJob in @($lpActive.ToArray())) {
   if(!$lpJob.process.HasExited){continue}
   $lpJob.process.WaitForExit()
   $lpPassed=$lpJob.process.ExitCode -eq 0 -and (Get-Content -LiteralPath $lpJob.stdout -Raw).Contains("CHART_AUDIT_COMPLETE run=$($lpJob.name) weeks=$Weeks") -and !(Select-String -LiteralPath $lpJob.stderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
   $lpJob.manifest.results+=@{name=$lpJob.name;seed=$lpJob.seed;mode='default';passed=$lpPassed;exitCode=$lpJob.process.ExitCode;seconds=([datetime]::UtcNow-$lpJob.started).TotalSeconds;args=$lpJob.args}
   $lpJob.manifest | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $lpJob.dir 'runs.json') -Encoding utf8
   $null=$lpActive.Remove($lpJob); $lpDone++
   if(!$lpPassed){throw "Failed $($lpJob.name); inspect logs."}
   Write-Output "Passed $($lpJob.name)."
  }
  if(([datetime]::UtcNow-$lpLast).TotalSeconds -ge 30){Write-Output "LP screen: $lpDone/$lpTotal completed; $($lpActive.Count) running."; $lpLast=[datetime]::UtcNow}
  if($lpActive.Count){Start-Sleep -Seconds 2}
 }
 if((Get-FileHash -LiteralPath (Join-Path $lpRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash -ne $lpHash){throw 'Assembly changed during screen.'}
} finally {
 foreach($lpJob in $lpActive){
  # The console executable can wrap a second Godot process. Stop only this job's verified command line.
  $lpOwned=Get-CimInstance Win32_Process | Where-Object {$_.Name -match '^Godot' -and $_.CommandLine -like "*--run=$($lpJob.name)*"}
  foreach($lpProcess in $lpOwned){Stop-Process -Id $lpProcess.ProcessId -ErrorAction SilentlyContinue}
 }
}
