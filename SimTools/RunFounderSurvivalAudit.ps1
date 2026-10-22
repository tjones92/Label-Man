[CmdletBinding()]
param(
    [ValidatePattern('^[a-z0-9-]+$')][string]$RunTag='dfs-v1',
    [int[]]$Seeds=@(1001,2002,3003),
    [ValidateRange(53,574)][int]$Weeks=265,
    [ValidateRange(0,100)][double]$HomeRunningCost=25
)
$ErrorActionPreference='Stop'
if(!$Seeds.Count -or @($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Use distinct seeds.'}
$founderRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$founderOutput=Join-Path $founderRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $founderOutput){throw 'Choose a new run tag.'}
$founderBundle=@('--single-commercial-fit=2.5','--single-commercial-fit-hit-share=0.15',
 '--single-commercial-fit-regional=0.5','--single-commercial-fit-scale=0.88',
 '--single-commercial-fit-launch-weight=1.25','--single-commercial-fit-floor=0.12',
 '--launch-stock-share=0.1','--sell-through-reorders','--ai-master-deals','--ai-backers',
 '--ai-master-national-pickups','--ai-master-capacity-decisions',
 '--major-offchart-singles=0.12','--major-offchart-singles-hold-units')
$founderHome=@('--home-operated-micro-labels',"--home-label-running-cost=$($HomeRunningCost.ToString([Globalization.CultureInfo]::InvariantCulture))")
# Complete a matched mechanism comparison first, then the remaining seed pairs.
$founderGroups=@(
 @{tag="$RunTag-current1";seeds=@($Seeds[0]);flags=$founderBundle},
 @{tag="$RunTag-home";seeds=@($Seeds[0]);flags=@($founderBundle+$founderHome)},
 @{tag="$RunTag-founder";seeds=@($Seeds);flags=@($founderBundle+$founderHome+'--founder-label-dormancy')}
)
if($Seeds.Count -gt 1){$founderGroups+=@{tag="$RunTag-current-rest";seeds=@($Seeds | Select-Object -Skip 1);flags=$founderBundle}}
New-Item -ItemType Directory -Path $founderOutput | Out-Null
$founderAssembly=Join-Path $founderRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$founderHash=(Get-FileHash -LiteralPath $founderAssembly).Hash
$founderPlan=[ordered]@{weeks=$Weeks;seeds=$Seeds;homeRunningCost=$HomeRunningCost;assemblySha256=$founderHash;groups=$founderGroups;completedGroups=@()}
$founderPlan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $founderOutput 'plan.json') -Encoding utf8
foreach($founderGroup in $founderGroups){
 if((Get-FileHash -LiteralPath $founderAssembly).Hash -ne $founderHash){throw 'Assembly changed during matrix.'}
 & (Join-Path $PSScriptRoot 'RunCorporateEconomyAudit.ps1') -RunTag $founderGroup.tag -Seeds $founderGroup.seeds -Modes default -Weeks $Weeks -Profile game -ExtraFlags $founderGroup.flags
 $founderPlan.completedGroups+=@($founderGroup.tag)
 $founderPlan | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $founderOutput 'plan.json') -Encoding utf8
}
