[CmdletBinding()]
param(
    [int[]]$Seeds = @(1001,2002),
    [int]$Weeks = 104,
    [string]$Tag = 'writing-combined-integration',
    [switch]$WithoutLpAssembly,
    [string]$DebutLpPriorScale = '',
    [string]$ExpectedAssemblySha256 = '511E9709FE20879CE4636C7FFE39F9C62E9CE825DC8EB01A110041711D221AC5'
)
$ErrorActionPreference = 'Stop'
$integrationFlags = @(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'writing_combined_integration_flags.txt') |
    ForEach-Object { $_.Trim() } | Where-Object { $_ -and !$_.StartsWith('#') })
if ($WithoutLpAssembly) {
    if ($Tag -eq 'writing-combined-integration') { throw 'Choose a distinct tag for the LP assembly ablation.' }
    $integrationFlags = @($integrationFlags | Where-Object { $_ -ne '--lp-assembly' })
}
if ($DebutLpPriorScale) {
    if ($Tag -eq 'writing-combined-integration') { throw 'Choose a distinct tag for the debut LP prior treatment.' }
    $integrationFlags += "--album-prior-debut-scale=$DebutLpPriorScale"
}
foreach ($integrationSeed in $Seeds) {
    $integrationName = "$Tag-market-$integrationSeed"
    if (Test-Path -LiteralPath (Join-Path $PSScriptRoot "../SimLogs/writing-calibration/$integrationName.log")) {
        throw "Refusing to overwrite $integrationName; use a fresh tag."
    }
}
& (Join-Path $PSScriptRoot 'RunWritingMarketCalibration.ps1') -Arm market -Workflow combined `
    -Seeds $Seeds -Weeks $Weeks -Tag $Tag -CausalFanbase -DefaultProfile -ConcentrationAudit `
    -MaterialFunnelAudit -ExtraFlags $integrationFlags -ExpectedAssemblySha256 $ExpectedAssemblySha256
