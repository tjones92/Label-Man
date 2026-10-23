[CmdletBinding()]
param(
 [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='slate-v1',
 [int[]]$Seeds=@(1001),
 [ValidateRange(13,574)][int]$Weeks=53,
 [string]$WorkingPlan='SimLogs/working-bundle/plan.json',
 [string]$ReuseChecksFrom='',
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$programmeRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$programmeOutput=Join-Path $programmeRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $programmeOutput){throw 'Choose a new run tag.'}
if(!$Seeds.Count -or @($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Use distinct seeds.'}
New-Item -ItemType Directory -Path $programmeOutput | Out-Null
# Resolve the accepted bundle at invocation, so subsequent adoption changes cannot silently leave this screen on an old base.
$programmeWorkingPath=if([IO.Path]::IsPathRooted($WorkingPlan)){$WorkingPlan}else{Join-Path $programmeRoot $WorkingPlan}
$programmeWorking=Get-Content -LiteralPath $programmeWorkingPath -Raw | ConvertFrom-Json
$programmeTreatments=@('--label-release-slate','--label-release-capacity','--studio-parent-labels')
$programmeSourceBundle=@($programmeWorking.bundle)
# Reconstruct the untreated base explicitly when the working bundle already adopts the scheduler/studio arms.
$programmeBundle=@($programmeSourceBundle | Where-Object {$_ -notin $programmeTreatments})
if($programmeWorking.profile -ne 'game' -or !$programmeBundle.Count){throw 'Working plan must contain a game-profile bundle.'}
if(@($programmeBundle | Select-Object -Unique).Count -ne $programmeBundle.Count){throw 'Working bundle has duplicate flags.'}
foreach($programmeRequired in @('--hit-act-cadence','--hit-act-retention')){
 if($programmeRequired -notin $programmeBundle){throw "Working bundle must retain $programmeRequired."}
}
if(@($programmeBundle | Where-Object {$_ -match '^--(label-release-|studio-parent-labels)'}).Count){throw 'Working bundle contains an unsupported factorial treatment.'}
$programmeWorkingHash=(Get-FileHash -LiteralPath $programmeWorkingPath).Hash
$programmeWorking | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $programmeOutput 'working-plan.json') -Encoding utf8
$programmeAssembly=Join-Path $programmeRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$programmeHash=(Get-FileHash -LiteralPath $programmeAssembly).Hash
$programmePlan=[ordered]@{weeks=$Weeks;seeds=$Seeds;assemblySha256=$programmeHash;bundle=$programmeBundle;
 workingPlan=$programmeWorkingPath;workingPlanSha256=$programmeWorkingHash;controlReference='mtn5-pair';checks=@();groups=@()}
$programmePlan['factorialTreatmentsExcluded']=$programmeTreatments
if($ReuseChecksFrom){
 $programmeChecksPath=if([IO.Path]::IsPathRooted($ReuseChecksFrom)){$ReuseChecksFrom}else{Join-Path $programmeRoot $ReuseChecksFrom}
 $programmePrior=Get-Content -LiteralPath $programmeChecksPath -Raw | ConvertFrom-Json
 if($programmePrior.assemblySha256 -ne $programmeHash -or ($programmePrior.bundle -join "`n") -cne ($programmeBundle -join "`n")){
  throw 'Reused native checks must have the identical assembly and working bundle.'
 }
 foreach($programmeStudio in @('off','on')){foreach($programmeCheck in @('mechanism','roundtrip')){
  $programmeMatched=@($programmePrior.checks | Where-Object {$_.studio -eq $programmeStudio -and $_.check -eq $programmeCheck})
  if($programmeMatched.Count -ne 1 -or !$programmeMatched[0].passed){throw 'Reused plan must contain all four passed native checks.'}
 }}
 $programmePlan.checks=@($programmePrior.checks)
 $programmePlan['checksSource']=$programmeChecksPath
 $programmePlan | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $programmeOutput 'plan.json') -Encoding utf8
}
if(!$ReuseChecksFrom){
foreach($programmeStudio in @('off','on')){
 $programmeStudioFlags=@()
 if($programmeStudio -eq 'on'){$programmeStudioFlags=@('--studio-parent-labels')}
 foreach($programmeCheck in @('mechanism','roundtrip')){
  $programmeCheckFlags=if($programmeCheck -eq 'mechanism'){@('--label-release-programme-check')}else{@('--weeks=13',"--seed=$($Seeds[0])")}
  $programmeArgs=@('--headless','--path','.', 'SimTools/SaveLoadRoundTripRunner.tscn','--')+$programmeBundle+$programmeStudioFlags+@('--label-release-slate','--label-release-capacity')+$programmeCheckFlags
  $programmeLog=Join-Path $programmeOutput "$programmeStudio-$programmeCheck-console.log"
  $programmeErrors=Join-Path $programmeOutput "$programmeStudio-$programmeCheck-errors.log"
  $programmeProcess=Start-Process -FilePath $Godot -ArgumentList $programmeArgs -WorkingDirectory $programmeRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $programmeLog -RedirectStandardError $programmeErrors
  $null=$programmeProcess.Handle
  while(!$programmeProcess.WaitForExit(10000)){Write-Output "Checking studio-$programmeStudio $programmeCheck."}
  $programmeProcess.WaitForExit()
  $programmeMarker=if($programmeCheck -eq 'mechanism'){'LABEL_RELEASE_PROGRAMME_CHECK passed'}else{'SAVELOAD_ROUNDTRIP_PASS'}
  $programmePassed=$programmeProcess.ExitCode -eq 0 -and (Get-Content -LiteralPath $programmeLog -Raw).Contains($programmeMarker) -and !(Select-String -LiteralPath $programmeErrors -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
  $programmePlan.checks+=@{studio=$programmeStudio;check=$programmeCheck;passed=$programmePassed;args=$programmeArgs}
  $programmePlan | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $programmeOutput 'plan.json') -Encoding utf8
  if(!$programmePassed){throw "Failed studio-$programmeStudio $programmeCheck; inspect logs."}
 }
}
}
foreach($programmeStudio in @('off','on')){
 foreach($programmeArm in @('control','allocation','capacity','both')){
  if((Get-FileHash -LiteralPath $programmeAssembly).Hash -ne $programmeHash){throw 'Assembly changed during screen.'}
  $programmeFlags=@($programmeBundle)
  if($programmeStudio -eq 'on'){$programmeFlags+='--studio-parent-labels'}
  if($programmeArm -in @('allocation','both')){$programmeFlags+='--label-release-slate'}
  if($programmeArm -in @('capacity','both')){$programmeFlags+='--label-release-capacity'}
  $programmeTag="$RunTag-$programmeStudio-$programmeArm"
  & (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag $programmeTag -Seeds $Seeds -Modes default -Weeks $Weeks -Profile game -ExtraFlags $programmeFlags -Godot $Godot
  $programmePlan.groups+=@{tag=$programmeTag;studio=$programmeStudio;arm=$programmeArm}
  $programmePlan | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $programmeOutput 'plan.json') -Encoding utf8
 }
}
