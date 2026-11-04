[CmdletBinding()]
param(
 [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='fct-v1',
 [string]$ReferenceTag='mbk-v1-breakout',
 [int]$Seed=1001,
 [ValidateRange(66,265)][int]$Weeks=106,
 [string]$WaitForPlan='',
 [string]$Python=(Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe')
)
$ErrorActionPreference='Stop'
$traceRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location -LiteralPath $traceRoot
if($WaitForPlan){
 $deadline=[datetime]::UtcNow.AddHours(2)
 do {
  $pending=Get-Content -LiteralPath $WaitForPlan -Raw | ConvertFrom-Json
  if($pending.status -eq 'review-complete'){break}
  if([datetime]::UtcNow -gt $deadline){throw 'Existing screen has not completed; active assembly was not changed.'}
  Start-Sleep -Seconds 10
 } while($true)
}
$reference=Get-Content -LiteralPath "SimLogs/$ReferenceTag/runs.json" -Raw | ConvertFrom-Json
if($reference.profile -ne 'game'){throw 'Trace replay currently requires a game-profile reference.'}
$match=@($reference.results | Where-Object {$_.seed -eq $Seed -and $_.passed})
if($match.Count -ne 1 -or $reference.weeks -lt $Weeks){throw 'Need one completed reference covering the trace window.'}
if(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$'){throw 'Another game is active; do not replace its assembly.'}
if(Test-Path -LiteralPath "SimLogs/$RunTag"){throw 'Choose an unused trace tag.'}
dotnet build 'Label Man.csproj' --no-restore --verbosity quiet
if($LASTEXITCODE -ne 0){throw 'Trace build failed.'}
$traceFlags=@($reference.extraFlags)+@('--followup-chart-trace','--head-hit-attribution')
& (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag $RunTag -Seeds @($Seed) -Modes default -Weeks $Weeks -Profile game -ExtraFlags $traceFlags
if($LASTEXITCODE -ne 0){throw 'Trace run failed.'}
$referenceName="$ReferenceTag-default-$Seed-$($reference.weeks)"
$traceName="$RunTag-default-$Seed-$Weeks"
& $Python (Join-Path $PSScriptRoot 'analyze_followup_chart_trace.py') --reference $referenceName --trace $traceName --weeks $Weeks --output "SimLogs/followup-chart-investigation/$RunTag"
if($LASTEXITCODE -ne 0){throw 'Trace identity or diagnostic validation failed.'}
Write-Output "FOLLOWUP_CHART_TRACE_COMPLETE run=$traceName"
