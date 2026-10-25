[CmdletBinding()]
param(
 [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='label-project-v1',
 [string]$ReferenceTag='label-balance-v1-pilot-local-control',
 [int]$Seed=1001,
 [ValidateRange(66,265)][int]$Weeks=106,
 [string]$Python=(Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'),
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location -LiteralPath $projectRoot
$reference=Get-Content -LiteralPath "SimLogs/$ReferenceTag/runs.json" -Raw | ConvertFrom-Json
$completed=@($reference.results | Where-Object {$_.seed -eq $Seed -and $_.passed -and $_.exitCode -eq 0})
if($reference.profile -ne 'game' -or $reference.weeks -lt $Weeks -or $completed.Count -ne 1 -or
 $reference.extraFlags -notcontains '--label-release-slate'){throw 'Need a completed game-profile slate reference covering the window.'}
$connections=@('--anticipatory-material-search','--release-judgement-shadow','--release-judgement-select=perceived')
if(@($reference.extraFlags | Where-Object {$_ -in $connections -or $_ -eq '--label-project-judgement'}).Count){throw 'Reference already enables an intervention.'}
dotnet build 'Label Man.csproj' --no-restore --verbosity quiet
if($LASTEXITCODE -ne 0){throw 'Build failed.'}
$arms=[ordered]@{control=@();connected=$connections;judged=@('--label-project-judgement')}
foreach($arm in $arms.Keys){
 & (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag "$RunTag-$arm" -Seeds @($Seed) -Weeks $Weeks -Modes default -Profile game -ExtraFlags (@($reference.extraFlags)+@($arms[$arm])) -Godot $Godot
}
& $Python (Join-Path $PSScriptRoot 'analyze_label_projects.py') --tag $RunTag --reference $ReferenceTag --seed $Seed --weeks $Weeks
if($LASTEXITCODE -ne 0){throw 'Project audit validation failed.'}
Write-Output "LABEL_PROJECT_AUDIT_COMPLETE tag=$RunTag"
