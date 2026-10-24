[CmdletBinding()]
param(
 [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='label-ai-v1',
 [string]$ReferenceTag='label-balance-v1-pilot-local-control',
 [int]$Seed=1001,
 [ValidateRange(66,265)][int]$Weeks=106,
 [string]$Python=(Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'),
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$decisionRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location -LiteralPath $decisionRoot
if(Test-Path -LiteralPath "SimLogs/$RunTag"){throw 'Choose an unused run tag.'}
if(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$'){throw 'Another game is active.'}
$reference=Get-Content -LiteralPath "SimLogs/$ReferenceTag/runs.json" -Raw | ConvertFrom-Json
$completed=@($reference.results | Where-Object {$_.seed -eq $Seed -and $_.passed -and $_.exitCode -eq 0})
if($reference.profile -ne 'game' -or $reference.weeks -lt $Weeks -or $completed.Count -ne 1 -or
 $reference.extraFlags -notcontains '--label-release-slate'){throw 'Need one completed game-profile slate reference covering this window.'}
if($reference.extraFlags -contains '--label-decision-audit'){throw 'Reference already contains the choice audit.'}
dotnet build 'Label Man.csproj' --no-restore --verbosity quiet
if($LASTEXITCODE -ne 0){throw 'Build failed.'}
$decisionFlags=@($reference.extraFlags)+@('--label-decision-audit')
& (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag $RunTag -Seeds @($Seed) -Weeks $Weeks -Modes default -Profile game -ExtraFlags $decisionFlags -Godot $Godot
$referencePrefix="SimLogs/$ReferenceTag-default-$Seed-$($reference.weeks)"
$tracePrefix="SimLogs/$RunTag-default-$Seed-$Weeks"
& $Python (Join-Path $PSScriptRoot 'analyze_label_decisions.py') --reference $referencePrefix --trace $tracePrefix --weeks $Weeks --output "SimLogs/$RunTag/review"
if($LASTEXITCODE -ne 0){throw 'Decision audit identity or analysis failed.'}
Write-Output "LABEL_DECISION_AUDIT_COMPLETE tag=$RunTag"
