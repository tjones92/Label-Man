[CmdletBinding()]
param(
 [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='scr-v3',
 [int[]]$Seeds=@(1001),
 [ValidateRange(53,574)][int]$Weeks=53,
 [ValidateRange(.001,1)][double]$CapitalFraction=1,
 [ValidateSet('observe','mean','log')][string[]]$Policies=@('observe','mean','log'),
 [string]$Reference='erc-v2-w17',
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$riskRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$riskOutput=Join-Path $riskRoot "SimLogs/$RunTag-checks"
if(Test-Path -LiteralPath $riskOutput){throw 'Choose an unused run tag.'}
New-Item -ItemType Directory -Path $riskOutput | Out-Null
$riskReference=Get-Content -LiteralPath (Join-Path $riskRoot "SimLogs/$Reference/runs.json") -Raw | ConvertFrom-Json
$riskFlags=@($riskReference.extraFlags)
if(@($riskFlags | Where-Object {$_ -match '^--single-(capital-risk|capital-risk-policy|forecast-realization)='}).Count){throw 'Reference must exclude both probes.'}
if(!$Policies.Count -or @($Policies | Select-Object -Unique).Count -ne $Policies.Count){throw 'Use distinct policies.'}
$riskSwitch='--single-capital-risk='+$CapitalFraction.ToString([Globalization.CultureInfo]::InvariantCulture)
$riskAssembly=Join-Path $riskRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$riskHash=(Get-FileHash -LiteralPath $riskAssembly).Hash
$riskPlan=[ordered]@{modelVersion=3;reference=$Reference;seeds=$Seeds;weeks=$Weeks;policies=$Policies;capitalFraction=$CapitalFraction;assemblySha256=$riskHash;bundle=$riskFlags;checks=@()}
foreach($riskCheck in @('native','roundtrip')) {
 $riskExtra=if($riskCheck -eq 'native'){@('--single-capital-risk-check')}else{@('--weeks=13',"--seed=$($Seeds[0])",'--calibration','--small-label-finance')+$riskFlags+@($riskSwitch)}
 $riskArgs=@('--headless','--path','.', 'SimTools/SaveLoadRoundTripRunner.tscn','--')+$riskExtra
 $riskConsole=Join-Path $riskOutput "$riskCheck-console.log"
 $riskErrors=Join-Path $riskOutput "$riskCheck-errors.log"
 $riskProcess=Start-Process -FilePath $Godot -ArgumentList $riskArgs -WorkingDirectory $riskRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $riskConsole -RedirectStandardError $riskErrors
 $null=$riskProcess.Handle
 while(!$riskProcess.WaitForExit(10000)){Write-Output "Running $riskCheck checks."}
 $riskProcess.WaitForExit()
 $riskMarker=if($riskCheck -eq 'native'){'SINGLE_CAPITAL_RISK_CHECK passed'}else{'SAVELOAD_ROUNDTRIP_PASS'}
 $riskPassed=$riskProcess.ExitCode -eq 0 -and (Get-Content -LiteralPath $riskConsole -Raw).Contains($riskMarker) -and !(Select-String -LiteralPath $riskErrors -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
 $riskPlan.checks+=@{check=$riskCheck;passed=$riskPassed;args=$riskArgs}
 $riskPlan | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $riskOutput 'plan.json') -Encoding utf8
 if(!$riskPassed){throw "Failed $riskCheck; inspect logs."}
}
& (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag "$RunTag-off13" -Seeds @($Seeds[0]) -Modes default -Weeks 13 -Profile game -ExtraFlags $riskFlags -Godot $Godot
if((Get-FileHash -LiteralPath $riskAssembly).Hash -ne $riskHash){throw 'Assembly changed during checks.'}
foreach($riskPolicy in $Policies) {
 if((Get-FileHash -LiteralPath $riskAssembly).Hash -ne $riskHash){throw 'Assembly changed during screen.'}
 & (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag "$RunTag-$riskPolicy" -Seeds $Seeds -Modes default -Weeks $Weeks -Profile game -ExtraFlags ($riskFlags+@($riskSwitch,"--single-capital-risk-policy=$riskPolicy")) -Godot $Godot
}
