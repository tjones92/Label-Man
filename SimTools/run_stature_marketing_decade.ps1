param(
    [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$RunPrefix = 'stature-marketing-decade-v2',
    [ValidateRange(27, 522)][int]$Weeks = 522,
    [ValidateCount(1, 2)][int[]]$Seeds = @(1001, 2002),
    [switch]$ProfilePerformance = $true,
    [switch]$TreatmentOnly
)
$ErrorActionPreference = 'Stop'
if (@($Seeds | Select-Object -Unique).Count -ne $Seeds.Count) { throw 'Duplicate seeds.' }
$root = Split-Path -Parent $PSScriptRoot
$dir = Join-Path $root 'SimLogs/writing-calibration'
$batchName = if ($TreatmentOnly) { "$RunPrefix-treatment-restart" } else { $RunPrefix }
$launcherLogs = @("$batchName-batch.log", "$batchName-batch-errors.log")
if (Get-ChildItem -LiteralPath $dir -Filter "$batchName*" | Where-Object { $_.Name -notin $launcherLogs } | Select-Object -First 1) { throw 'Batch prefix already exists.' }
$shell = (Get-Command pwsh).Source
$workers = @()
$state = [ordered]@{ prefix=$RunPrefix; weeks=$Weeks; seeds=$Seeds; arms=$(if ($TreatmentOnly) { @('treatment') } else { @('control','treatment') }); concurrency=$Seeds.Count;
    profilePerformance=[bool]$ProfilePerformance;
    status='launching'; startedUtc=[datetime]::UtcNow.ToString('o');
    assemblySha256=(Get-FileHash -LiteralPath (Join-Path $root '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash;
    neutrality='Not repeated at decade duration; retain separate one-year neutrality evidence'; workers=@() }
$statePath = Join-Path $dir "$batchName-batch.json"
try {
    foreach ($seed in $Seeds) {
        $arguments = @('-NoProfile', '-File', ('"' + (Join-Path $PSScriptRoot 'run_stature_marketing_trial.ps1') + '"'),
            '-RunPrefix', $RunPrefix, '-Weeks', $Weeks, '-Seeds', $seed, '-CausalOnly', '-CompressOutputs')
        if ($ProfilePerformance) { $arguments += '-ProfilePerformance' }
        if ($TreatmentOnly) { $arguments += '-TreatmentOnly' }
        $process = Start-Process -FilePath $shell -ArgumentList $arguments -WorkingDirectory $root -WindowStyle Hidden -PassThru `
            -RedirectStandardOutput (Join-Path $dir "$batchName-$seed-worker.log") `
            -RedirectStandardError (Join-Path $dir "$batchName-$seed-worker-errors.log")
        $workers += $process
        $state.workers += @{ seed=$seed; pid=$process.Id; status='running' }
    }
    $state.status = 'running'
    $state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $statePath
    Write-Host "Launched $($workers.Count) concurrent seed workers; arms=$($state.arms -join ',')."
    for ($index = 0; $index -lt $workers.Count; $index++) {
        $workers[$index].WaitForExit()
        $state.workers[$index].exitCode = $workers[$index].ExitCode
        $state.workers[$index].status = if ($workers[$index].ExitCode -eq 0) { 'complete' } else { 'failed' }
        $state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $statePath
    }
    $state.status = if (@($state.workers | Where-Object { $_.status -ne 'complete' }).Count) { 'failed' } else { 'complete' }
    $state.finishedUtc = [datetime]::UtcNow.ToString('o')
    $state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $statePath
    if ($state.status -ne 'complete') { throw 'One or more seed workers failed; inspect their logs.' }
} catch {
    $state.status = 'failed'
    $state.error = $_.Exception.Message
    $state | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $statePath
    throw
}
