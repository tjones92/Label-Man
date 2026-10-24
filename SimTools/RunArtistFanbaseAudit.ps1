[CmdletBinding()]
param(
 [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='afb-v1',
 [int[]]$Seeds=@(1001),
 [ValidateRange(66,265)][int]$Weeks=106,
 [ValidateRange(.01,1)][double]$TrialRate=.25,
 [ValidateRange(0,1)][double]$Recruitment=.35,
 [ValidateRange(13,520)][int]$HalfLife=104,
 [ValidateSet('observe','fan','blend')][string[]]$Arms=@('observe','fan','blend'),
 [switch]$SkipChecks,
 [string[]]$ExtraFlags=@(),
 [string]$BasePlan='SimLogs/hfs-v2/plan.json',
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$fanRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location -LiteralPath $fanRoot
$folder=Join-Path $fanRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $folder){throw 'Choose an unused audit tag.'}
if(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$'){throw 'Another game is active.'}
New-Item -ItemType Directory -Path $folder | Out-Null
$base=Get-Content -LiteralPath $BasePlan -Raw | ConvertFrom-Json
$flags=@($base.baseFlags | Where-Object {$_ -notlike '--hit-act-fit-floor=*' -and $_ -notlike '--artist-fanbase*'})+$ExtraFlags
if(!$flags.Count -or $flags -notcontains '--followup-chart-trace'){throw 'Need the completed traced repair/planning bundle.'}
$assembly=Join-Path $fanRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$hash=(Get-FileHash -LiteralPath $assembly).Hash
$culture=[Globalization.CultureInfo]::InvariantCulture
$trial=$TrialRate.ToString('0.###',$culture);$recruit=$Recruitment.ToString('0.###',$culture)
$common=@("--artist-fanbase-recruit=$recruit","--artist-fanbase-half-life=$HalfLife")
$plan=[ordered]@{weeks=$Weeks;seeds=$Seeds;trialRate=$TrialRate;recruitment=$Recruitment;halfLife=$HalfLife;
 basePlan=$BasePlan;baseFlags=$flags;extraFlags=$ExtraFlags;assemblySha256=$hash;checks=@();arms=@{};status='checking'}
function Save-Plan {$plan | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath "$folder/plan.json" -Encoding utf8}
$env:LABELMAN_WORKSTATION_GC='1';$env:DOTNET_gcServer='0';$env:DOTNET_GCHeapHardLimit='A0000000'
Save-Plan
if(!$SkipChecks){
 foreach($check in @('artist-fanbase','demand-stocking','roundtrip')){
  $env:DOTNET_GCHeapHardLimit=if($check -eq 'roundtrip'){'E0000000'}else{'A0000000'}
  $checkFlags=if($check -eq 'roundtrip'){@('--weeks=13')}else{@("--$check-check")}
  $checkArgs=@('--headless','--path','.', 'SimTools/SaveLoadRoundTripRunner.tscn','--')+$flags+$common+@("--artist-fanbase=$trial",'--hit-act-fit-floor=0.6',"--seed=$($Seeds[0])")+$checkFlags
  $stdout="$folder/$check-console.log";$stderr="$folder/$check-errors.log"
  $p=Start-Process -FilePath $Godot -ArgumentList $checkArgs -WorkingDirectory $fanRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
  $null=$p.Handle
  while(!$p.WaitForExit(10000)){Write-Output "Checking $check."}
  $p.WaitForExit();$log=Get-Content -LiteralPath $stdout -Raw
  $marker=if($check -eq 'roundtrip'){'SAVELOAD_ROUNDTRIP_PASS'}elseif($check -eq 'demand-stocking'){'DEMAND_STOCKING_CHECK_PASSED'}else{'ARTIST_FANBASE_CHECK passed'}
  $passed=$p.ExitCode -eq 0 -and $log.Contains($marker) -and !(Select-String -LiteralPath $stderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
  if($check -eq 'roundtrip' -and (!$log.Contains('gzipIo=ok') -or !$log.Contains('ARTIST_FANBASE_LIVE passed'))){$passed=$false}
  $plan.checks+=@{name=$check;passed=$passed;args=$checkArgs};Save-Plan
  if(!$passed){throw "Failed $check; inspect logs."}
  Write-Output "Passed $check."
 }
}
$plan.status='running';Save-Plan
foreach($arm in $Arms){
 if((Get-FileHash -LiteralPath $assembly).Hash -ne $hash){throw 'Assembly changed during screen.'}
 $tag="$RunTag-$arm";$plan.arms[$arm]=$tag;Save-Plan
 $treatment=if($arm -eq 'observe'){@('--artist-fanbase=0','--hit-act-fit-floor=0.6')}elseif($arm -eq 'blend'){@("--artist-fanbase=$trial",'--hit-act-fit-floor=0.6')}else{@("--artist-fanbase=$trial")}
 & (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag $tag -Seeds $Seeds -Modes default -Weeks $Weeks -Profile game -ExtraFlags ($flags+$common+$treatment) -Godot $Godot
}
$plan.status='native-complete';Save-Plan
Write-Output "ARTIST_FANBASE_NATIVE_COMPLETE tag=$RunTag"
