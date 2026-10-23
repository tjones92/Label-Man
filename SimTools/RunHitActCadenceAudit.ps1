[CmdletBinding()]
param(
    [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='hac-v1',
    [int[]]$Seeds=@(1001,2002,3003),
    [ValidateRange(53,574)][int]$Weeks=265,
    [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$hacRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$hacOutput=Join-Path $hacRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $hacOutput){throw 'Choose a new run tag.'}
if(!$Seeds.Count -or @($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Use distinct seeds.'}
New-Item -ItemType Directory -Path $hacOutput | Out-Null
# The ttx-v1-tax working bundle (prior-year tax adopted into it): the matched control is that existing group, same seeds.
$hacBundle=@('--single-commercial-fit=2.5','--single-commercial-fit-hit-share=0.15',
 '--single-commercial-fit-regional=0.5','--single-commercial-fit-scale=0.88',
 '--single-commercial-fit-launch-weight=1.25','--single-commercial-fit-floor=0.12',
 '--launch-stock-share=0.1','--sell-through-reorders','--ai-master-deals','--ai-backers',
 '--ai-master-national-pickups','--ai-master-capacity-decisions',
 '--major-offchart-singles=0.12','--major-offchart-singles-hold-units',
 '--home-operated-micro-labels','--home-label-running-cost=25','--founder-label-dormancy',
 '--founder-surplus-compensation','--distress-master-sales','--prior-year-corporate-tax')
$hacAssembly=Join-Path $hacRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$hacHash=(Get-FileHash -LiteralPath $hacAssembly).Hash
$hacPlan=[ordered]@{weeks=$Weeks;seeds=$Seeds;assemblySha256=$hacHash;bundle=$hacBundle;
 control='Existing ttx-v1-tax, same seeds, 265 weeks';checks=@();completedGroups=@()}
$hacChecks=@(
 @{name='cadence';flags=@('--hit-act-cadence-check');marker='HIT_ACT_CADENCE_CHECK passed'},
 @{name='tax';flags=@('--tax-timing-check');marker='TAX_TIMING_CHECK_PASSED'},
 @{name='distress';flags=@('--distress-master-sale-check');marker='DISTRESS_MASTER_SALE_CHECK_PASSED'},
 @{name='compensation';flags=@('--founder-compensation-check');marker='FOUNDER_COMPENSATION_CHECK_PASSED'},
 @{name='founder';flags=@('--founder-label-check');marker='FOUNDER_LABEL_CHECK_PASSED'},
 @{name='company';flags=@('--label-company-check');marker='LABEL_COMPANY_CHECK passed'},
 @{name='economic';flags=@('--economic-label-check');marker='ECONOMIC_LABEL_CHECK'},
 @{name='stocking';flags=@('--demand-stocking-check');marker='DEMAND_STOCKING_CHECK'},
 @{name='roundtrip';flags=@('--weeks=13','--seed=1001')+$hacBundle+@('--hit-act-cadence','--hit-act-awareness-split','--hit-act-retention');marker='SAVELOAD_ROUNDTRIP_PASS'}
)
foreach($hacCheck in $hacChecks){
 $hacStdout=Join-Path $hacOutput "$($hacCheck.name)-console.log"
 $hacStderr=Join-Path $hacOutput "$($hacCheck.name)-errors.log"
 $hacArgs=@('--headless','--path','.', 'SimTools/SaveLoadRoundTripRunner.tscn','--')+$hacCheck.flags
 $hacProcess=Start-Process -FilePath $Godot -ArgumentList $hacArgs -WorkingDirectory $hacRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $hacStdout -RedirectStandardError $hacStderr
 $null=$hacProcess.Handle
 while(!$hacProcess.WaitForExit(10000)){Write-Output "Checking $($hacCheck.name)."}
 $hacProcess.WaitForExit()
 $hacPassed=$hacProcess.ExitCode -eq 0 -and (Get-Content -LiteralPath $hacStdout -Raw).Contains($hacCheck.marker) -and !(Select-String -LiteralPath $hacStderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
 $hacPlan.checks+=@{name=$hacCheck.name;passed=$hacPassed;exitCode=$hacProcess.ExitCode;args=$hacArgs}
 $hacPlan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $hacOutput 'plan.json') -Encoding utf8
 if(!$hacPassed){throw "Failed $($hacCheck.name); inspect logs."}
 Write-Output "Passed $($hacCheck.name)."
}
$hacGroups=@(
 @{tag="$RunTag-off";seeds=@($Seeds[0]);flags=$hacBundle},
 @{tag="$RunTag-lane";seeds=$Seeds;flags=@($hacBundle+@('--hit-act-cadence','--hit-act-awareness-split'))},
 @{tag="$RunTag-retention";seeds=$Seeds;flags=@($hacBundle+'--hit-act-retention')},
 @{tag="$RunTag-both";seeds=$Seeds;flags=@($hacBundle+@('--hit-act-cadence','--hit-act-awareness-split','--hit-act-retention'))}
)
foreach($hacGroup in $hacGroups){
 if((Get-FileHash -LiteralPath $hacAssembly).Hash -ne $hacHash){throw 'Assembly changed during screen.'}
 & (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag $hacGroup.tag -Seeds $hacGroup.seeds -Modes default -Weeks $Weeks -Profile game -ExtraFlags $hacGroup.flags -Godot $Godot
 $hacPlan.completedGroups+=@($hacGroup.tag)
 $hacPlan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $hacOutput 'plan.json') -Encoding utf8
}

