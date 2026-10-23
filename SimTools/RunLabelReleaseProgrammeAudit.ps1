[CmdletBinding()]
param(
 [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='slate-v1',
 [int[]]$Seeds=@(1001,2002,3003),
 [ValidateRange(13,574)][int]$Weeks=265,
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$programmeRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$programmeOutput=Join-Path $programmeRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $programmeOutput){throw 'Choose a new run tag.'}
if(!$Seeds.Count -or @($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Use distinct seeds.'}
New-Item -ItemType Directory -Path $programmeOutput | Out-Null
# Accepted working bundle plus the retained cadence and retention mechanisms, shared by every arm.
$programmeBundle=@('--single-commercial-fit=2.5','--single-commercial-fit-hit-share=0.15',
 '--single-commercial-fit-regional=0.5','--single-commercial-fit-scale=0.88',
 '--single-commercial-fit-launch-weight=1.25','--single-commercial-fit-floor=0.12',
 '--launch-stock-share=0.1','--sell-through-reorders','--ai-master-deals','--ai-backers',
 '--ai-master-national-pickups','--ai-master-capacity-decisions',
 '--major-offchart-singles=0.12','--major-offchart-singles-hold-units',
 '--home-operated-micro-labels','--home-label-running-cost=25','--founder-label-dormancy',
 '--founder-surplus-compensation','--distress-master-sales','--prior-year-corporate-tax',
 '--hit-act-cadence','--hit-act-retention','--profile-performance')
$programmeAssembly=Join-Path $programmeRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$programmeHash=(Get-FileHash -LiteralPath $programmeAssembly).Hash
$programmePlan=[ordered]@{weeks=$Weeks;seeds=$Seeds;assemblySha256=$programmeHash;bundle=$programmeBundle;checks=@();groups=@()}
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
