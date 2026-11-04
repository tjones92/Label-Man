[CmdletBinding()]
param(
 [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='hfs-v1',
 [int[]]$Seeds=@(1001),
 [ValidateRange(66,265)][int]$Weeks=106,
 [ValidateRange(.12,1)][double[]]$Floors=@(.6,1),
 [switch]$SkipChecks,
 [switch]$SkipOffReplay,
 [string]$ReferencePlan='SimLogs/fct-v2/runs.json',
 [string]$Python=(Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'),
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$auditRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location -LiteralPath $auditRoot
$folder=Join-Path $auditRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $folder){throw 'Choose an unused audit tag.'}
if(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$'){throw 'Another game is active.'}
$reference=Get-Content -LiteralPath $ReferencePlan -Raw | ConvertFrom-Json
if($reference.profile -ne 'game'){throw 'Need a game-profile reference.'}
$flags=@($reference.extraFlags)
if(@($flags | Where-Object {$_ -like '--hit-act-fit-floor=*'}).Count){throw 'Reference must have fit support off.'}
if($flags -notcontains '--followup-chart-trace' -or $flags -notcontains '--head-hit-attribution'){throw 'Reference must include launch and follow-up tracing.'}
New-Item -ItemType Directory -Path $folder | Out-Null
$assembly=Join-Path $auditRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$hash=(Get-FileHash -LiteralPath $assembly).Hash
$plan=[ordered]@{weeks=$Weeks;seeds=$Seeds;floors=$Floors;referencePlan=$ReferencePlan;assemblySha256=$hash;
 baseFlags=$flags;checks=@();arms=@{};status='checking'}
function Save-Plan { $plan | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath "$folder/plan.json" -Encoding utf8 }
$env:LABELMAN_WORKSTATION_GC='1'; $env:DOTNET_gcServer='0'; $env:DOTNET_GCHeapHardLimit='A0000000'
Save-Plan
if(!$SkipChecks){
 foreach($check in @('hit-act-fit-support','demand-stocking','roundtrip')){
  $env:DOTNET_GCHeapHardLimit=if($check -eq 'roundtrip'){'E0000000'}else{'A0000000'}
  $checkFlags=if($check -eq 'roundtrip'){@('--weeks=13')}else{@("--$check-check")}
  $checkArgs=@('--headless','--path','.', 'SimTools/SaveLoadRoundTripRunner.tscn','--')+$flags+@('--hit-act-fit-floor=1',"--seed=$($Seeds[0])")+$checkFlags
  $stdout="$folder/$check-console.log"; $stderr="$folder/$check-errors.log"
  $p=Start-Process -FilePath $Godot -ArgumentList $checkArgs -WorkingDirectory $auditRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
  $null=$p.Handle
  while(!$p.WaitForExit(10000)){Write-Output "Checking $check."}
  $p.WaitForExit()
  $log=Get-Content -LiteralPath $stdout -Raw
  $marker=if($check -eq 'roundtrip'){'SAVELOAD_ROUNDTRIP_PASS'}elseif($check -eq 'demand-stocking'){'DEMAND_STOCKING_CHECK_PASSED'}else{'CHECK passed'}
  $passed=$p.ExitCode -eq 0 -and $log.Contains($marker) -and !(Select-String -LiteralPath $stderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
  if($check -eq 'roundtrip' -and (!$log.Contains('gzipIo=ok') -or !$log.Contains('HIT_ACT_FIT_SUPPORT_LIVE passed'))){$passed=$false}
  $plan.checks+=@{name=$check;passed=$passed;args=$checkArgs};Save-Plan
  if(!$passed){throw "Failed $check; inspect logs."}
  Write-Output "Passed $check."
 }
}
if(!$SkipOffReplay){
 $plan.status='off-replay';Save-Plan
 & (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag "$RunTag-off" -Seeds @($Seeds[0]) -Modes default -Weeks $Weeks -Profile game -ExtraFlags $flags -Godot $Godot
 $referenceRun=@($reference.results | Where-Object {$_.seed -eq $Seeds[0] -and $_.passed})
 if($referenceRun.Count -ne 1 -or $reference.weeks -lt $Weeks){throw 'Off replay needs a completed reference of sufficient duration.'}
 & $Python (Join-Path $PSScriptRoot 'analyze_followup_chart_trace.py') --reference $referenceRun[0].name --trace "$RunTag-off-default-$($Seeds[0])-$Weeks" --weeks $Weeks --output "$folder/off-identity"
 if($LASTEXITCODE -ne 0){throw 'Off replay or trace identity failed.'}
 $plan.arms.off="$RunTag-off"; Save-Plan
}
$plan.status='running';Save-Plan
foreach($floor in $Floors){
 if((Get-FileHash -LiteralPath $assembly).Hash -ne $hash){throw 'Assembly changed during screen.'}
 $value=$floor.ToString('0.##',[Globalization.CultureInfo]::InvariantCulture)
 $arm='f'+$value.Replace('.','p');$tag="$RunTag-$arm"
 $plan.arms[$arm]=$tag;Save-Plan
 & (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag $tag -Seeds $Seeds -Modes default -Weeks $Weeks -Profile game -ExtraFlags ($flags+@("--hit-act-fit-floor=$value")) -Godot $Godot
}
$plan.status='native-complete';Save-Plan
Write-Output "HIT_ACT_FIT_SUPPORT_NATIVE_COMPLETE tag=$RunTag"
