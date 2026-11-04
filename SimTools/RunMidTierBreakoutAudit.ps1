[CmdletBinding()]
param(
 [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='mbk-v1',
 [int[]]$Seeds=@(1001,2002,3003),
 [ValidateRange(106,574)][int]$Weeks=265,
 [string]$CandidatePlan='SimLogs/wbg-v3-gc10c/runs.json',
 [string]$Baseline='tpk-v2-t90',
 [switch]$FinalizeOnly,
 [string]$Python=(Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'),
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$screenRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location -LiteralPath $screenRoot
$screenFolder=Join-Path $screenRoot "SimLogs/$RunTag"
function Complete-ScreenReview {
 & $Python (Join-Path $PSScriptRoot 'analyze_midtier_breakout.py') --tag $RunTag
 if($LASTEXITCODE -ne 0){throw 'Screen analysis failed; inspect analyzer output.'}
 $screenPlan.status='review-complete'
 $screenPlan | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath "$screenFolder/plan.json" -Encoding utf8
}
if($FinalizeOnly){
 # Attach report generation to an already running screen without launching or modifying its native worlds.
 $deadline=[datetime]::UtcNow.AddHours(3)
 do {
  $screenPlan=Get-Content -LiteralPath "$screenFolder/plan.json" -Raw | ConvertFrom-Json
  if($screenPlan.status -eq 'review-complete'){return}
  if($screenPlan.status -eq 'native-complete'){break}
  if([datetime]::UtcNow -gt $deadline){throw 'Timed out waiting for native completion.'}
  $driverLog=Join-Path $screenRoot "SimLogs/$RunTag-driver.log"
  if((Test-Path -LiteralPath $driverLog) -and (Get-Item -LiteralPath $driverLog).LastWriteTimeUtc -lt [datetime]::UtcNow.AddMinutes(-5)){
   throw 'Native driver stopped reporting progress; inspect its logs.'
  }
  Start-Sleep -Seconds 15
 } while($true)
 Complete-ScreenReview
 return
}
if(Test-Path -LiteralPath $screenFolder){throw 'Choose a new run tag.'}
if($Seeds.Count -lt 3 -or @($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Use at least three distinct seeds.'}
$candidate=Get-Content -LiteralPath $CandidatePlan -Raw | ConvertFrom-Json
$baselinePlan=Get-Content -LiteralPath "SimLogs/$Baseline/runs.json" -Raw | ConvertFrom-Json
$budgetFlags=@('--slate-work-budget=0.67','--slate-work-budget-midtier=1','--slate-going-concern-reserve')
$baseFlags=@($candidate.extraFlags | Where-Object {$_ -notin $budgetFlags})
if((($baseFlags | Sort-Object) -join "`n") -cne (($baselinePlan.extraFlags | Sort-Object) -join "`n")) {throw 'Candidate must be the baseline plus exactly the MidTier repair.'}
if($baselinePlan.weeks -ne $Weeks){throw 'Baseline and treatment durations must match.'}
foreach($seed in $Seeds){
 $match=@($baselinePlan.results | Where-Object {$_.seed -eq $seed -and $_.passed})
 if($match.Count -ne 1){throw "Baseline incomplete for seed $seed."}
}
if(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$'){throw 'Another game is active.'}
New-Item -ItemType Directory -Path $screenFolder | Out-Null
$assembly=Join-Path $screenRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$assemblyHash=(Get-FileHash -LiteralPath $assembly).Hash
$screenPlan=[ordered]@{baseline=$Baseline;weeks=$Weeks;seeds=$Seeds;assemblySha256=$assemblyHash;
 candidateSource=$CandidatePlan;candidateSourceSha256=(Get-FileHash -LiteralPath $CandidatePlan).Hash;
 arms=@{repair="$RunTag-repair";breakout="$RunTag-breakout"};checks=@();status='checking'}
function Save-ScreenPlan { $screenPlan | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath "$screenFolder/plan.json" -Encoding utf8 }
Save-ScreenPlan
$env:LABELMAN_WORKSTATION_GC='1'; $env:DOTNET_gcServer='0'; $env:DOTNET_GCHeapHardLimit='A0000000'
foreach($check in @('slate-breakout-planning','label-release-programme','slate-work-budget','roundtrip')) {
 $env:DOTNET_GCHeapHardLimit=if($check -eq 'roundtrip'){'E0000000'}else{'A0000000'}
 $checkFlags=if($check -eq 'roundtrip'){@('--weeks=13',"--seed=$($Seeds[0])")}else{@("--$check-check", "--seed=$($Seeds[0])")}
 $checkArgs=@('--headless','--path','.', 'SimTools/SaveLoadRoundTripRunner.tscn','--')+@($candidate.extraFlags)+@('--slate-breakout-planning')+$checkFlags
 $stdout="$screenFolder/$check-console.log"; $stderr="$screenFolder/$check-errors.log"
 $p=Start-Process -FilePath $Godot -ArgumentList $checkArgs -WorkingDirectory $screenRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
 $null=$p.Handle
 while(!$p.WaitForExit(10000)){Write-Output "Checking $check."}
 $p.WaitForExit()
 $log=Get-Content -LiteralPath $stdout -Raw
 $marker=if($check -eq 'roundtrip'){'SAVELOAD_ROUNDTRIP_PASS'}else{'CHECK passed'}
 $passed=$p.ExitCode -eq 0 -and $log.Contains($marker) -and !(Select-String -LiteralPath $stderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
 if($check -eq 'roundtrip' -and $log -match 'gzipIo=error:'){$passed=$false}
 $screenPlan.checks+=@{name=$check;passed=$passed;args=$checkArgs}; Save-ScreenPlan
 if(!$passed){throw "Failed $check; inspect logs."}
}
# A fresh repair-off replay establishes compatibility with the archived baseline before reusing its five-year runs.
$screenPlan.status='off-replay'; Save-ScreenPlan
& (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag "$RunTag-off" -Seeds @($Seeds[0]) -Modes default -Weeks 53 -Profile game -ExtraFlags $baseFlags -Godot $Godot
& $Python (Join-Path $PSScriptRoot 'analyze_midtier_breakout.py') --tag $RunTag --identity-only
if($LASTEXITCODE -ne 0){throw 'Off replay does not match the archived baseline.'}
$screenPlan.status='running'; Save-ScreenPlan
foreach($arm in @('repair','breakout')){
 if((Get-FileHash -LiteralPath $assembly).Hash -ne $assemblyHash){throw 'Assembly changed during screen.'}
 $armFlags=@($candidate.extraFlags)
 if($arm -eq 'breakout'){$armFlags+='--slate-breakout-planning'}
 & (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag "$RunTag-$arm" -Seeds $Seeds -Modes default -Weeks $Weeks -Profile game -ExtraFlags $armFlags -Godot $Godot
}
$screenPlan.status='native-complete'; Save-ScreenPlan
Write-Output "MIDTIER_BREAKOUT_NATIVE_COMPLETE tag=$RunTag"
Complete-ScreenReview
