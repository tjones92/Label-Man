[CmdletBinding()]
param(
    [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='bcs-v1',
    [int[]]$Seeds=@(1001,2002,3003),
    [ValidateRange(53,574)][int]$Weeks=265,
    [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$compRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$compOutput=Join-Path $compRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $compOutput){throw 'Choose a new run tag.'}
if(!$Seeds.Count -or @($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Use distinct seeds.'}
New-Item -ItemType Directory -Path $compOutput | Out-Null
$compBundle=@('--single-commercial-fit=2.5','--single-commercial-fit-hit-share=0.15',
 '--single-commercial-fit-regional=0.5','--single-commercial-fit-scale=0.88',
 '--single-commercial-fit-launch-weight=1.25','--single-commercial-fit-floor=0.12',
 '--launch-stock-share=0.1','--sell-through-reorders','--ai-master-deals','--ai-backers',
 '--ai-master-national-pickups','--ai-master-capacity-decisions',
 '--major-offchart-singles=0.12','--major-offchart-singles-hold-units',
 '--home-operated-micro-labels','--home-label-running-cost=25','--founder-label-dormancy')
$compAssembly=Join-Path $compRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$compHash=(Get-FileHash -LiteralPath $compAssembly).Hash
$compPlan=[ordered]@{weeks=$Weeks;seeds=$Seeds;assemblySha256=$compHash;bundle=$compBundle;
 control='Existing dfs-v1-founder, same seeds, 265 weeks';checks=@();completedGroups=@()}
$compChecks=@(
 @{name='compensation';flags=@('--founder-compensation-check');marker='FOUNDER_COMPENSATION_CHECK_PASSED'},
 @{name='founder';flags=@('--founder-label-check');marker='FOUNDER_LABEL_CHECK_PASSED'},
 @{name='company';flags=@('--label-company-check');marker='LABEL_COMPANY_CHECK passed'},
 @{name='economic';flags=@('--economic-label-check');marker='ECONOMIC_LABEL_CHECK'},
 @{name='stocking';flags=@('--demand-stocking-check');marker='DEMAND_STOCKING_CHECK'},
 @{name='roundtrip';flags=@('--weeks=13','--seed=1001')+$compBundle+@('--founder-surplus-compensation');marker='SAVELOAD_ROUNDTRIP_PASS'}
)
foreach($compCheck in $compChecks){
 $compStdout=Join-Path $compOutput "$($compCheck.name)-console.log"
 $compStderr=Join-Path $compOutput "$($compCheck.name)-errors.log"
 $compArgs=@('--headless','--path','.', 'SimTools/SaveLoadRoundTripRunner.tscn','--')+$compCheck.flags
 $compProcess=Start-Process -FilePath $Godot -ArgumentList $compArgs -WorkingDirectory $compRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $compStdout -RedirectStandardError $compStderr
 $null=$compProcess.Handle
 while(!$compProcess.WaitForExit(10000)){Write-Output "Checking $($compCheck.name)."}
 $compProcess.WaitForExit()
 $compPassed=$compProcess.ExitCode -eq 0 -and (Get-Content -LiteralPath $compStdout -Raw).Contains($compCheck.marker) -and !(Select-String -LiteralPath $compStderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
 $compPlan.checks+=@{name=$compCheck.name;passed=$compPassed;exitCode=$compProcess.ExitCode;args=$compArgs}
 $compPlan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $compOutput 'plan.json') -Encoding utf8
 if(!$compPassed){throw "Failed $($compCheck.name); inspect logs."}
 Write-Output "Passed $($compCheck.name)."
}
$compGroups=@(
 @{tag="$RunTag-off";seeds=@($Seeds[0]);flags=$compBundle},
 @{tag="$RunTag-pay";seeds=$Seeds;flags=@($compBundle+'--founder-surplus-compensation')}
)
foreach($compGroup in $compGroups){
 if((Get-FileHash -LiteralPath $compAssembly).Hash -ne $compHash){throw 'Assembly changed during screen.'}
 & (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag $compGroup.tag -Seeds $compGroup.seeds -Modes default -Weeks $Weeks -Profile game -ExtraFlags $compGroup.flags -Godot $Godot
 $compPlan.completedGroups+=@($compGroup.tag)
 $compPlan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $compOutput 'plan.json') -Encoding utf8
}
