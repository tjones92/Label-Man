[CmdletBinding()]
param(
    [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='ttx-v1',
    [int[]]$Seeds=@(1001,2002,3003),
    [ValidateRange(53,574)][int]$Weeks=265,
    [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$ttxRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$ttxOutput=Join-Path $ttxRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $ttxOutput){throw 'Choose a new run tag.'}
if(!$Seeds.Count -or @($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Use distinct seeds.'}
New-Item -ItemType Directory -Path $ttxOutput | Out-Null
# The dms-v1-sale bundle (distress master sales adopted into it): the matched control is that existing group, same seeds.
$ttxBundle=@('--single-commercial-fit=2.5','--single-commercial-fit-hit-share=0.15',
 '--single-commercial-fit-regional=0.5','--single-commercial-fit-scale=0.88',
 '--single-commercial-fit-launch-weight=1.25','--single-commercial-fit-floor=0.12',
 '--launch-stock-share=0.1','--sell-through-reorders','--ai-master-deals','--ai-backers',
 '--ai-master-national-pickups','--ai-master-capacity-decisions',
 '--major-offchart-singles=0.12','--major-offchart-singles-hold-units',
 '--home-operated-micro-labels','--home-label-running-cost=25','--founder-label-dormancy',
 '--founder-surplus-compensation','--distress-master-sales')
$ttxAssembly=Join-Path $ttxRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$ttxHash=(Get-FileHash -LiteralPath $ttxAssembly).Hash
$ttxPlan=[ordered]@{weeks=$Weeks;seeds=$Seeds;assemblySha256=$ttxHash;bundle=$ttxBundle;
 control='Existing dms-v1-sale, same seeds, 265 weeks';checks=@();completedGroups=@()}
$ttxChecks=@(
 @{name='tax';flags=@('--tax-timing-check');marker='TAX_TIMING_CHECK_PASSED'},
 @{name='distress';flags=@('--distress-master-sale-check');marker='DISTRESS_MASTER_SALE_CHECK_PASSED'},
 @{name='compensation';flags=@('--founder-compensation-check');marker='FOUNDER_COMPENSATION_CHECK_PASSED'},
 @{name='founder';flags=@('--founder-label-check');marker='FOUNDER_LABEL_CHECK_PASSED'},
 @{name='company';flags=@('--label-company-check');marker='LABEL_COMPANY_CHECK passed'},
 @{name='economic';flags=@('--economic-label-check');marker='ECONOMIC_LABEL_CHECK'},
 @{name='stocking';flags=@('--demand-stocking-check');marker='DEMAND_STOCKING_CHECK'},
 @{name='roundtrip';flags=@('--weeks=13','--seed=1001')+$ttxBundle+@('--prior-year-corporate-tax');marker='SAVELOAD_ROUNDTRIP_PASS'}
)
foreach($ttxCheck in $ttxChecks){
 $ttxStdout=Join-Path $ttxOutput "$($ttxCheck.name)-console.log"
 $ttxStderr=Join-Path $ttxOutput "$($ttxCheck.name)-errors.log"
 $ttxArgs=@('--headless','--path','.', 'SimTools/SaveLoadRoundTripRunner.tscn','--')+$ttxCheck.flags
 $ttxProcess=Start-Process -FilePath $Godot -ArgumentList $ttxArgs -WorkingDirectory $ttxRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $ttxStdout -RedirectStandardError $ttxStderr
 $null=$ttxProcess.Handle
 while(!$ttxProcess.WaitForExit(10000)){Write-Output "Checking $($ttxCheck.name)."}
 $ttxProcess.WaitForExit()
 $ttxPassed=$ttxProcess.ExitCode -eq 0 -and (Get-Content -LiteralPath $ttxStdout -Raw).Contains($ttxCheck.marker) -and !(Select-String -LiteralPath $ttxStderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
 $ttxPlan.checks+=@{name=$ttxCheck.name;passed=$ttxPassed;exitCode=$ttxProcess.ExitCode;args=$ttxArgs}
 $ttxPlan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $ttxOutput 'plan.json') -Encoding utf8
 if(!$ttxPassed){throw "Failed $($ttxCheck.name); inspect logs."}
 Write-Output "Passed $($ttxCheck.name)."
}
$ttxGroups=@(
 @{tag="$RunTag-off";seeds=@($Seeds[0]);flags=$ttxBundle},
 @{tag="$RunTag-tax";seeds=$Seeds;flags=@($ttxBundle+'--prior-year-corporate-tax')}
)
foreach($ttxGroup in $ttxGroups){
 if((Get-FileHash -LiteralPath $ttxAssembly).Hash -ne $ttxHash){throw 'Assembly changed during screen.'}
 & (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag $ttxGroup.tag -Seeds $ttxGroup.seeds -Modes default -Weeks $Weeks -Profile game -ExtraFlags $ttxGroup.flags -Godot $Godot
 $ttxPlan.completedGroups+=@($ttxGroup.tag)
 $ttxPlan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $ttxOutput 'plan.json') -Encoding utf8
}
