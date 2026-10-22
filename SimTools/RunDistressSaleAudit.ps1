[CmdletBinding()]
param(
    [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='dms-v1',
    [int[]]$Seeds=@(1001,2002,3003),
    [ValidateRange(53,574)][int]$Weeks=265,
    [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$dmsRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$dmsOutput=Join-Path $dmsRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $dmsOutput){throw 'Choose a new run tag.'}
if(!$Seeds.Count -or @($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Use distinct seeds.'}
New-Item -ItemType Directory -Path $dmsOutput | Out-Null
# The bcs-v1-pay bundle: the matched control is that existing group, same seeds.
$dmsBundle=@('--single-commercial-fit=2.5','--single-commercial-fit-hit-share=0.15',
 '--single-commercial-fit-regional=0.5','--single-commercial-fit-scale=0.88',
 '--single-commercial-fit-launch-weight=1.25','--single-commercial-fit-floor=0.12',
 '--launch-stock-share=0.1','--sell-through-reorders','--ai-master-deals','--ai-backers',
 '--ai-master-national-pickups','--ai-master-capacity-decisions',
 '--major-offchart-singles=0.12','--major-offchart-singles-hold-units',
 '--home-operated-micro-labels','--home-label-running-cost=25','--founder-label-dormancy',
 '--founder-surplus-compensation')
$dmsAssembly=Join-Path $dmsRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$dmsHash=(Get-FileHash -LiteralPath $dmsAssembly).Hash
$dmsPlan=[ordered]@{weeks=$Weeks;seeds=$Seeds;assemblySha256=$dmsHash;bundle=$dmsBundle;
 control='Existing bcs-v1-pay, same seeds, 265 weeks';checks=@();completedGroups=@()}
$dmsChecks=@(
 @{name='distress';flags=@('--distress-master-sale-check');marker='DISTRESS_MASTER_SALE_CHECK_PASSED'},
 @{name='compensation';flags=@('--founder-compensation-check');marker='FOUNDER_COMPENSATION_CHECK_PASSED'},
 @{name='founder';flags=@('--founder-label-check');marker='FOUNDER_LABEL_CHECK_PASSED'},
 @{name='company';flags=@('--label-company-check');marker='LABEL_COMPANY_CHECK passed'},
 @{name='economic';flags=@('--economic-label-check');marker='ECONOMIC_LABEL_CHECK'},
 @{name='stocking';flags=@('--demand-stocking-check');marker='DEMAND_STOCKING_CHECK'},
 @{name='roundtrip';flags=@('--weeks=13','--seed=1001')+$dmsBundle+@('--distress-master-sales');marker='SAVELOAD_ROUNDTRIP_PASS'}
)
foreach($dmsCheck in $dmsChecks){
 $dmsStdout=Join-Path $dmsOutput "$($dmsCheck.name)-console.log"
 $dmsStderr=Join-Path $dmsOutput "$($dmsCheck.name)-errors.log"
 $dmsArgs=@('--headless','--path','.', 'SimTools/SaveLoadRoundTripRunner.tscn','--')+$dmsCheck.flags
 $dmsProcess=Start-Process -FilePath $Godot -ArgumentList $dmsArgs -WorkingDirectory $dmsRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $dmsStdout -RedirectStandardError $dmsStderr
 $null=$dmsProcess.Handle
 while(!$dmsProcess.WaitForExit(10000)){Write-Output "Checking $($dmsCheck.name)."}
 $dmsProcess.WaitForExit()
 $dmsPassed=$dmsProcess.ExitCode -eq 0 -and (Get-Content -LiteralPath $dmsStdout -Raw).Contains($dmsCheck.marker) -and !(Select-String -LiteralPath $dmsStderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
 $dmsPlan.checks+=@{name=$dmsCheck.name;passed=$dmsPassed;exitCode=$dmsProcess.ExitCode;args=$dmsArgs}
 $dmsPlan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $dmsOutput 'plan.json') -Encoding utf8
 if(!$dmsPassed){throw "Failed $($dmsCheck.name); inspect logs."}
 Write-Output "Passed $($dmsCheck.name)."
}
$dmsGroups=@(
 @{tag="$RunTag-off";seeds=@($Seeds[0]);flags=$dmsBundle},
 @{tag="$RunTag-sale";seeds=$Seeds;flags=@($dmsBundle+'--distress-master-sales')}
)
foreach($dmsGroup in $dmsGroups){
 if((Get-FileHash -LiteralPath $dmsAssembly).Hash -ne $dmsHash){throw 'Assembly changed during screen.'}
 & (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag $dmsGroup.tag -Seeds $dmsGroup.seeds -Modes default -Weeks $Weeks -Profile game -ExtraFlags $dmsGroup.flags -Godot $Godot
 $dmsPlan.completedGroups+=@($dmsGroup.tag)
 $dmsPlan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $dmsOutput 'plan.json') -Encoding utf8
}
