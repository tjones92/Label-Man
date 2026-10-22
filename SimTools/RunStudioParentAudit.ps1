[CmdletBinding()]
param(
    [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='spl-v1',
    [int[]]$Seeds=@(1001,2002,3003),
    [ValidateRange(53,574)][int]$Weeks=265,
    [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$splRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$splOutput=Join-Path $splRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $splOutput){throw 'Choose a new run tag.'}
if(!$Seeds.Count -or @($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Use distinct seeds.'}
New-Item -ItemType Directory -Path $splOutput | Out-Null
# The ttx-v1-tax working bundle (prior-year tax adopted into it): the matched control is that existing group, same seeds.
$splBundle=@('--single-commercial-fit=2.5','--single-commercial-fit-hit-share=0.15',
 '--single-commercial-fit-regional=0.5','--single-commercial-fit-scale=0.88',
 '--single-commercial-fit-launch-weight=1.25','--single-commercial-fit-floor=0.12',
 '--launch-stock-share=0.1','--sell-through-reorders','--ai-master-deals','--ai-backers',
 '--ai-master-national-pickups','--ai-master-capacity-decisions',
 '--major-offchart-singles=0.12','--major-offchart-singles-hold-units',
 '--home-operated-micro-labels','--home-label-running-cost=25','--founder-label-dormancy',
 '--founder-surplus-compensation','--distress-master-sales','--prior-year-corporate-tax')
$splAssembly=Join-Path $splRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$splHash=(Get-FileHash -LiteralPath $splAssembly).Hash
$splPlan=[ordered]@{weeks=$Weeks;seeds=$Seeds;assemblySha256=$splHash;bundle=$splBundle;
 control='Existing ttx-v1-tax, same seeds, 265 weeks';checks=@();completedGroups=@()}
$splChecks=@(
 @{name='studio';flags=@('--studio-parent-labels-check');marker='STUDIO_PARENT_LABELS_CHECK_PASSED'},
 @{name='tax';flags=@('--tax-timing-check');marker='TAX_TIMING_CHECK_PASSED'},
 @{name='distress';flags=@('--distress-master-sale-check');marker='DISTRESS_MASTER_SALE_CHECK_PASSED'},
 @{name='compensation';flags=@('--founder-compensation-check');marker='FOUNDER_COMPENSATION_CHECK_PASSED'},
 @{name='founder';flags=@('--founder-label-check');marker='FOUNDER_LABEL_CHECK_PASSED'},
 @{name='company';flags=@('--label-company-check');marker='LABEL_COMPANY_CHECK passed'},
 @{name='economic';flags=@('--economic-label-check');marker='ECONOMIC_LABEL_CHECK'},
 @{name='stocking';flags=@('--demand-stocking-check');marker='DEMAND_STOCKING_CHECK'},
 @{name='roundtrip';flags=@('--weeks=13','--seed=1001')+$splBundle+@('--studio-parent-labels');marker='SAVELOAD_ROUNDTRIP_PASS'}
)
foreach($splCheck in $splChecks){
 $splStdout=Join-Path $splOutput "$($splCheck.name)-console.log"
 $splStderr=Join-Path $splOutput "$($splCheck.name)-errors.log"
 $splArgs=@('--headless','--path','.', 'SimTools/SaveLoadRoundTripRunner.tscn','--')+$splCheck.flags
 $splProcess=Start-Process -FilePath $Godot -ArgumentList $splArgs -WorkingDirectory $splRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $splStdout -RedirectStandardError $splStderr
 $null=$splProcess.Handle
 while(!$splProcess.WaitForExit(10000)){Write-Output "Checking $($splCheck.name)."}
 $splProcess.WaitForExit()
 $splPassed=$splProcess.ExitCode -eq 0 -and (Get-Content -LiteralPath $splStdout -Raw).Contains($splCheck.marker) -and !(Select-String -LiteralPath $splStderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
 $splPlan.checks+=@{name=$splCheck.name;passed=$splPassed;exitCode=$splProcess.ExitCode;args=$splArgs}
 $splPlan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $splOutput 'plan.json') -Encoding utf8
 if(!$splPassed){throw "Failed $($splCheck.name); inspect logs."}
 Write-Output "Passed $($splCheck.name)."
}
$splGroups=@(
 @{tag="$RunTag-off";seeds=@($Seeds[0]);flags=$splBundle},
 @{tag="$RunTag-studio";seeds=$Seeds;flags=@($splBundle+'--studio-parent-labels')}
)
foreach($splGroup in $splGroups){
 if((Get-FileHash -LiteralPath $splAssembly).Hash -ne $splHash){throw 'Assembly changed during screen.'}
 & (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag $splGroup.tag -Seeds $splGroup.seeds -Modes default -Weeks $Weeks -Profile game -ExtraFlags $splGroup.flags -Godot $Godot
 $splPlan.completedGroups+=@($splGroup.tag)
 $splPlan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $splOutput 'plan.json') -Encoding utf8
}
