[CmdletBinding()]
param(
 [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='label-balance-v1-pilot',
 [int[]]$Seeds=@(1001),
 [ValidateRange(66,265)][int]$Weeks=106,
 [ValidateRange(6,13)][int]$SingleFloor=13,
 [string]$BaseManifest='SimLogs/afb-v1-blend/runs.json',
 [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$balanceRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Set-Location -LiteralPath $balanceRoot
$folder=Join-Path $balanceRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $folder){throw 'Choose an unused audit tag.'}
if(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$'){throw 'Another game is active.'}
$base=Get-Content -LiteralPath $BaseManifest -Raw | ConvertFrom-Json
if($base.profile -ne 'game' -or $base.extraFlags -notcontains '--followup-chart-trace' -or
 $base.extraFlags -notcontains '--artist-fanbase=0.25' -or $base.extraFlags -notcontains '--hit-act-fit-floor=0.6'){
 throw 'Need the completed, traced .6 + audience reference.'
}
if(!@($base.results | Where-Object {$_.passed -and $_.exitCode -eq 0}).Count){throw 'Reference has no completed native run.'}
if(@($base.extraFlags | Where-Object {$_ -like '--slate-single-floor=*'}).Count){throw 'Reference must use the six-week default floor.'}
dotnet build 'Label Man.csproj' --no-restore --verbosity quiet
if($LASTEXITCODE -ne 0){throw 'Build failed.'}
New-Item -ItemType Directory -Path $folder | Out-Null
$assembly=Join-Path $balanceRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$hash=(Get-FileHash -LiteralPath $assembly).Hash
$plan=[ordered]@{weeks=$Weeks;seeds=$Seeds;baseManifest=$BaseManifest;baseFlags=@($base.extraFlags);
 assemblySha256=$hash;singleFloor=$SingleFloor;arms=@{};status='running'}
function Save-BalancePlan {$plan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath "$folder/plan.json" -Encoding utf8}
Save-BalancePlan
try {
 foreach($arm in @('control',"floor$SingleFloor")){
  if((Get-FileHash -LiteralPath $assembly).Hash -ne $hash){throw 'Assembly changed during audit.'}
  $tag="$RunTag-$arm";$plan.arms[$arm]=$tag;Save-BalancePlan
  $flags=@($base.extraFlags)
  if($arm -ne 'control'){$flags+=@("--slate-single-floor=$SingleFloor")}
  & (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag $tag -Seeds $Seeds -Weeks $Weeks -Modes default -Profile game -ExtraFlags $flags -Godot $Godot
 }
 $plan.status='native-complete';Save-BalancePlan
} catch {
 $plan.status='failed';$plan.error=$_.Exception.Message;Save-BalancePlan
 throw
}
Write-Output "LABEL_BALANCE_NATIVE_COMPLETE tag=$RunTag"
