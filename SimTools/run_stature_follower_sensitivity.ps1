param(
    [Parameter(Mandatory)][string]$Python,
    [Parameter(Mandatory)][string]$Evidence,
    [Parameter(Mandatory)][ValidatePattern('^[a-zA-Z0-9-]+$')][string]$RunPrefix
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$dir = Join-Path $root 'SimLogs/writing-calibration'
$reference = Join-Path $dir 'stature-fit-26w-v2/reference.json'
$regional = Join-Path $dir 'stature-fit-26w-v2/regional-p99.json'
$policy = Join-Path $dir 'stature-experimental-pooled-p99-v1.json'
$paths = @{}
foreach ($suffix in @('adjusted.csv', 'adjusted.json', 'control-components.csv', 'control-components.json',
                       'adjusted-components.csv', 'adjusted-components.json', 'control-regional.csv', 'adjusted-regional.csv',
                       'control-shadow.csv', 'adjusted-shadow.csv', 'comparison.json', 'request.json')) {
    $path = Join-Path $dir "$RunPrefix-$suffix"
    if (Test-Path -LiteralPath $path) { throw "Output already exists: $path" }
    $paths[$suffix] = $path
}
$sourceHash = (Get-FileHash -LiteralPath $Evidence).Hash
$referenceHash = (Get-FileHash -LiteralPath $reference).Hash
$regionalHash = (Get-FileHash -LiteralPath $regional).Hash
$policyHash = (Get-FileHash -LiteralPath $policy).Hash
@{ inputSha256=$sourceHash; referenceSha256=$referenceHash; regionalSha256=$regionalHash; policySha256=$policyHash;
   halfLifeWeeks=4; topN=20; minimumPeers=25; boundPrunedFollowing=$true; holdoutOnly=$true;
   caveat='Offline roster-only maximum-grant sensitivity; fixed market totals and unchanged following.' } |
    ConvertTo-Json | Set-Content -LiteralPath $paths['request.json']
function Invoke-Step([string[]]$Arguments) {
    & $Python @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Analysis failed: $($Arguments[0])" }
}
Invoke-Step -Arguments @('SimTools/stature_follower_sensitivity.py', $Evidence, $paths['adjusted.csv'], $paths['adjusted.json'])
foreach ($arm in @('control', 'adjusted')) {
    $inputTrace = if ($arm -eq 'control') { $Evidence } else { $paths['adjusted.csv'] }
    Invoke-Step -Arguments @('SimTools/stature_normalization.py', $inputTrace, '--fixed-reference', $reference, '--half-life-weeks', '4',
        '--top-n', '20', '--min-peers', '25', '--output', $paths["$arm-components.csv"], '--manifest', $paths["$arm-components.json"])
    Invoke-Step -Arguments @('SimTools/stature_regional_evidence.py', $inputTrace, $regional, $paths["$arm-regional.csv"])
    Invoke-Step -Arguments @('SimTools/stature_shadow_trace.py', $paths["$arm-components.csv"], $paths["$arm-regional.csv"], $policy,
        $paths["$arm-shadow.csv"], '--score', 'peerScore', '--holdout-only', '--bound-pruned-following')
}
Invoke-Step -Arguments @('SimTools/compare_stature_follower_shadow.py', $paths['control-shadow.csv'], $paths['adjusted-shadow.csv'], $paths['comparison.json'])
if ((Get-FileHash -LiteralPath $Evidence).Hash -ne $sourceHash -or (Get-FileHash -LiteralPath $reference).Hash -ne $referenceHash -or
    (Get-FileHash -LiteralPath $regional).Hash -ne $regionalHash -or (Get-FileHash -LiteralPath $policy).Hash -ne $policyHash) {
    throw 'Input or calibration changed during analysis.'
}
Write-Host "Sensitivity completed: $RunPrefix"
