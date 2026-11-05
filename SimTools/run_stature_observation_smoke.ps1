param(
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe',
    [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$RunPrefix = 'stature-observation-smoke',
    [ValidateRange(1, 522)][int]$Weeks = 2,
    [int[]]$Seeds = @(1001, 2002),
    [string[]]$AdditionalFlags = @(),
    [string[]]$ObservationFlags = @()
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$dir = Join-Path $root 'SimLogs/writing-calibration'
$source = Get-Content -Raw (Join-Path $dir 'veteran-follower-on-market-2002-request.json') | ConvertFrom-Json
$assembly = Join-Path $root '.godot/mono/temp/bin/Debug/Label Man.dll'
$assemblyHash = (Get-FileHash -LiteralPath $assembly).Hash
$shadowFlags = @($ObservationFlags | Where-Object { $_ -like '--observe-stature-shadow=*' })
if ($shadowFlags.Count -gt 1) { throw 'Specify only one shadow calibration.' }
$calibrationPath = if ($shadowFlags.Count -eq 1) { $shadowFlags[0].Substring('--observe-stature-shadow='.Length) } else { $null }
$calibrationHash = if ($calibrationPath) { (Get-FileHash -LiteralPath $calibrationPath).Hash } else { $null }
foreach ($seed in $Seeds) {
    foreach ($arm in @('control', 'observe')) {
        $name = "$RunPrefix-$seed-$arm"
        if (Test-Path -LiteralPath (Join-Path $dir "$name-request.json")) { throw "Run $name exists." }
        if (Get-ChildItem -Path (Join-Path $root 'SimLogs') -Filter "$name-*.csv" | Select-Object -First 1) { throw "Output for $name exists." }
    }
}
foreach ($seed in $Seeds) {
    foreach ($arm in @('control', 'observe')) {
        $name = "$RunPrefix-$seed-$arm"
        $flags = @($source.flags | ForEach-Object {
            if ($_ -like '--run=*') { "--run=$name" }
            elseif ($_ -like '--seed=*') { "--seed=$seed" }
            elseif ($_ -like '--weeks=*') { "--weeks=$Weeks" }
            else { $_ }
        })
        $flags += $AdditionalFlags
        if ($arm -eq 'observe') { $flags += '--observe-stature-evidence'; $flags += $ObservationFlags }
        $env:APPDATA = Join-Path $dir "userdata-$name"
        $env:DOTNET_gcServer = '0'
        $env:LABELMAN_WORKSTATION_GC = '1'
        New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
        @{ flags=$flags; assemblySha256=$assemblyHash; statureCalibrationSha256=$(if ($arm -eq 'observe') { $calibrationHash } else { $null }); sourceRevision=(git rev-parse HEAD); trackedSourceDirty=$true;
            startedUtc=[datetime]::UtcNow.ToString('o') } | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $dir "$name-request.json")
        if ((Get-FileHash -LiteralPath $assembly).Hash -ne $assemblyHash) { throw 'Assembly changed before run.' }
        & $Godot --headless --path $root --log-file (Join-Path $dir "$name-engine.log") SimTools/ChartAuditRunner.tscn -- @flags > (Join-Path $dir "$name.log") 2> (Join-Path $dir "$name-errors.log")
        if ($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath (Join-Path $dir "$name.log") -SimpleMatch "CHART_AUDIT_COMPLETE run=$name weeks=$Weeks" -Quiet)) { throw "Run $name did not complete." }
        if ((Get-Item -LiteralPath (Join-Path $dir "$name-errors.log")).Length -ne 0) { throw "Run $name produced errors." }
        if ((Get-FileHash -LiteralPath $assembly).Hash -ne $assemblyHash) { throw 'Assembly changed during run.' }
        if ($calibrationHash -and (Get-FileHash -LiteralPath $calibrationPath).Hash -ne $calibrationHash) { throw 'Shadow calibration changed during run.' }
        Write-Host "Completed $name"
    }
    $control = "$RunPrefix-$seed-control"
    $observed = "$RunPrefix-$seed-observe"
    $files = @(Get-ChildItem -LiteralPath (Join-Path $root 'SimLogs') -Filter "$control-*.csv")
    if ($files.Count -eq 0) { throw 'No control outputs.' }
    foreach ($file in $files) {
        $suffix = $file.Name.Substring($control.Length)
        $other = Join-Path (Join-Path $root 'SimLogs') "$observed$suffix"
        if (!(Test-Path -LiteralPath $other) -or (Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $other).Hash) { throw "Output differs: $suffix" }
    }
    $extras = @(Get-ChildItem -LiteralPath (Join-Path $root 'SimLogs') -Filter "$observed-*.csv" | Where-Object { $_.Name -notin @("$observed-stature-evidence.csv", "$observed-stature-shadow.csv", "$observed-stature-decisions.csv") -and !(Test-Path -LiteralPath (Join-Path (Join-Path $root 'SimLogs') ($control + $_.Name.Substring($observed.Length)))) })
    if ($extras.Count -ne 0) { throw 'Unexpected observation-only outputs.' }
    if (!(Test-Path -LiteralPath (Join-Path $root "SimLogs/$observed-stature-evidence.csv"))) { throw 'Missing stature evidence.' }
    Write-Host "Matched $($files.Count) common CSV outputs for seed $seed"
}
