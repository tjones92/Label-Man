param(
    [Parameter(Mandatory=$true)][string]$Godot,
    [string[]]$Cases = @('base', 'no-hook', 'no-capability', 'no-critic', 'no-moment', 'legacy-group'),
    [int[]]$Seeds = @(1001, 1002)
)
$ErrorActionPreference = 'Stop'
$switches = @{
    'base' = '--use-polar-fit-selection'
    'no-hook' = '--polar-fit-audit-no-hook-ceiling'
    'no-capability' = '--polar-fit-audit-no-capability-penalty'
    'no-critic' = '--polar-fit-audit-no-critic-adjustment'
    'no-moment' = '--polar-fit-audit-no-moment-conversion'
    'legacy-group' = '--polar-fit-audit-legacy-realization'
    'candidate' = '--use-polar-fit-selection'
    'refined' = '--use-polar-fit-selection'
    'calibrated' = '--use-polar-fit-selection'
    'replay' = '--polar-fit-audit-original-realization'
    'off' = ''
    'final-off' = ''
    'verified-off' = ''
}
foreach ($case in $Cases) {
    if (-not $switches.ContainsKey($case)) { throw "Unknown case: $case" }
    foreach ($seed in $Seeds) {
        $run = "polar-realization-$case-$seed"
        $log = Join-Path 'SimLogs' "$run.log"
        if (Test-Path -LiteralPath $log) { throw "Audit output already exists: $log" }
        $auditArgs = @('--headless', '--path', '.', 'SimTools/ChartAuditRunner.tscn', '--',
            '--weeks=52', "--run=$run", "--seed=$seed", '--enable-genre-market-v2', '--enable-artist-population-lifecycle')
        if ($case -notin @('off', 'final-off', 'verified-off')) { $auditArgs += @('--use-polar-fit-selection', '--polar-song-shadow') }
        if ($case -in @('base', 'no-hook', 'no-capability', 'no-critic', 'no-moment', 'legacy-group')) {
            $auditArgs += '--polar-fit-audit-original-realization'
        }
        if ($switches[$case]) { $auditArgs += $switches[$case] }
        [ordered]@{ run=$run; seed=$seed; weeks=52; arguments=$auditArgs;
            tableSha256=(Get-FileHash -LiteralPath 'Data/PolarSongTable.json').Hash;
            assemblySha256=(Get-FileHash -LiteralPath '.godot/mono/temp/bin/Debug/Label Man.dll').Hash
        } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path 'SimLogs' "$run-invocation.json")
        # Godot emits a known missing-autoload diagnostic on successful audits too.
        $ErrorActionPreference = 'Continue'
        & $Godot @auditArgs > $log 2>&1
        $ErrorActionPreference = 'Stop'
        if (-not (Select-String -LiteralPath $log -Pattern "CHART_AUDIT_COMPLETE run=$run weeks=52" -Quiet)) {
            throw "Incomplete audit: $run (exit $LASTEXITCODE)"
        }
        Write-Output "Completed $run"
    }
}
