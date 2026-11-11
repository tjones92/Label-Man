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
$fitFiles = @{ fixed=(Join-Path $dir 'stature-fixed-reference-fit-v1.json'); peer=(Join-Path $dir 'stature-peer-reference-fit-v1.json') }
$paths = @{}
foreach ($suffix in @('request.json', 'components.csv', 'components.json', 'regional.csv', 'fixed-policy.json', 'peer-policy.json', 'fixed-shadow.csv', 'peer-shadow.csv', 'comparison.json')) {
    $path = Join-Path $dir "$RunPrefix-$suffix"
    if (Test-Path -LiteralPath $path) { throw "Output already exists: $path" }
    $paths[$suffix] = $path
}
$hashes = @{ evidence=(Get-FileHash -LiteralPath $Evidence).Hash; reference=(Get-FileHash -LiteralPath $reference).Hash;
             regional=(Get-FileHash -LiteralPath $regional).Hash; fixedFit=(Get-FileHash -LiteralPath $fitFiles.fixed).Hash;
             peerFit=(Get-FileHash -LiteralPath $fitFiles.peer).Hash }
foreach ($name in @('fixed','peer')) {
    $fit = Get-Content -Raw -LiteralPath $fitFiles[$name] | ConvertFrom-Json
    $expectedScore = if ($name -eq 'fixed') { 'fixedScore' } else { 'peerScore' }
    if ($fit.status -ne 'candidate_only' -or $fit.scoreField -ne $expectedScore -or $null -eq $fit.calibration -or
        $fit.calibration.effectiveDate -ne '1961-01-01') {
        throw "Invalid $name fit report"
    }
    $fit.calibration | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $paths["$name-policy.json"]
}
@{ inputHashes=$hashes; halfLifeWeeks=4; topN=20; minimumPeers=25; boundPrunedFollowing=$true; holdoutOnly=$true;
   caveat='Offline separately fitted experimental references; 1961 evaluation, roster-only histories.' } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $paths['request.json']
function Invoke-Step([string[]]$Arguments) {
    & $Python @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Analysis failed: $($Arguments[0])" }
}
Invoke-Step -Arguments @('SimTools/stature_normalization.py', $Evidence, '--fixed-reference', $reference, '--half-life-weeks', '4',
    '--top-n', '20', '--min-peers', '25', '--output', $paths['components.csv'], '--manifest', $paths['components.json'])
Invoke-Step -Arguments @('SimTools/stature_regional_evidence.py', $Evidence, $regional, $paths['regional.csv'])
foreach ($name in @('fixed','peer')) {
    $score = if ($name -eq 'fixed') { 'fixedScore' } else { 'peerScore' }
    Invoke-Step -Arguments @('SimTools/stature_shadow_trace.py', $paths['components.csv'], $paths['regional.csv'], $paths["$name-policy.json"],
        $paths["$name-shadow.csv"], '--score', $score, '--holdout-only', '--bound-pruned-following')
}
Invoke-Step -Arguments @('SimTools/compare_stature_references.py', $paths['fixed-shadow.csv'], $paths['peer-shadow.csv'], $paths['comparison.json'])
if ((Get-FileHash -LiteralPath $Evidence).Hash -ne $hashes.evidence -or (Get-FileHash -LiteralPath $reference).Hash -ne $hashes.reference -or
    (Get-FileHash -LiteralPath $regional).Hash -ne $hashes.regional -or (Get-FileHash -LiteralPath $fitFiles.fixed).Hash -ne $hashes.fixedFit -or
    (Get-FileHash -LiteralPath $fitFiles.peer).Hash -ne $hashes.peerFit) { throw 'Inputs changed during analysis.' }
Write-Host "Reference comparison completed: $RunPrefix"
