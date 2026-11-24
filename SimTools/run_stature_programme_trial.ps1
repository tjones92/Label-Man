param(
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe',
    [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$RunPrefix = 'stature-programme-trial',
    [ValidateRange(27, 522)][int]$Weeks = 52,
    [int[]]$Seeds = @(1001, 2002),
    [string]$Calibration = 'SimTools/StatureShadowExperimental.json'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$dir = Join-Path $root 'SimLogs/writing-calibration'
$source = Get-Content -Raw (Join-Path $dir 'veteran-follower-on-market-2002-request.json') | ConvertFrom-Json
$assembly = Join-Path $root '.godot/mono/temp/bin/Debug/Label Man.dll'
$assemblyHash = (Get-FileHash -LiteralPath $assembly).Hash
$engineHash = (Get-FileHash -LiteralPath $Godot).Hash
$calibrationPath = (Resolve-Path -LiteralPath $Calibration).Path.Replace('\', '/')
$calibrationHash = (Get-FileHash -LiteralPath $calibrationPath).Hash
foreach ($seed in $Seeds) {
    foreach ($arm in @('control', 'treatment')) {
        $name = "$RunPrefix-$seed-$arm"
        if (Test-Path -LiteralPath (Join-Path $dir "$name-request.json")) { throw "Run exists: $name" }
        if (Get-ChildItem -Path (Join-Path $root 'SimLogs') -Filter "$name-*.csv" | Select-Object -First 1) { throw "Outputs exist: $name" }
    }
}
foreach ($seed in $Seeds) {
    foreach ($arm in @('control', 'treatment')) {
        $name = "$RunPrefix-$seed-$arm"
        $flags = @($source.flags | ForEach-Object {
            if ($_ -like '--run=*') { "--run=$name" }
            elseif ($_ -like '--seed=*') { "--seed=$seed" }
            elseif ($_ -like '--weeks=*') { "--weeks=$Weeks" }
            else { $_ }
        })
        $flags += @('--label-decision-audit', "--observe-stature-shadow=$calibrationPath", '--persist-stature-shadow')
        if ($arm -eq 'treatment') { $flags += '--stature-programme-album-reader' }
        $env:APPDATA = Join-Path $dir "userdata-$name"
        $env:DOTNET_gcServer = '0'; $env:LABELMAN_WORKSTATION_GC = '1'
        New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
        @{ flags=$flags; assemblySha256=$assemblyHash; engineSha256=$engineHash; calibrationSha256=$calibrationHash;
           sourceRevision=(git rev-parse HEAD); trackedSourceDirty=$true; startedUtc=[datetime]::UtcNow.ToString('o') } |
            ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $dir "$name-request.json")
        if ((Get-FileHash -LiteralPath $assembly).Hash -ne $assemblyHash) { throw 'Assembly changed before run.' }
        & $Godot --headless --path $root --log-file (Join-Path $dir "$name-engine.log") SimTools/ChartAuditRunner.tscn -- @flags > (Join-Path $dir "$name.log") 2> (Join-Path $dir "$name-errors.log")
        if ($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath (Join-Path $dir "$name.log") -SimpleMatch "CHART_AUDIT_COMPLETE run=$name weeks=$Weeks" -Quiet)) { throw "Run did not complete: $name" }
        if ((Get-Item -LiteralPath (Join-Path $dir "$name-errors.log")).Length -ne 0) { throw "Run produced errors: $name" }
        if ((Get-FileHash -LiteralPath $assembly).Hash -ne $assemblyHash -or (Get-FileHash -LiteralPath $calibrationPath).Hash -ne $calibrationHash) { throw 'Assembly or calibration changed during run.' }
        $files = @(Get-ChildItem -LiteralPath (Join-Path $root 'SimLogs') -Filter "$name-*.csv")
        if ($files.Count -eq 0 -or !(Test-Path -LiteralPath (Join-Path $root "SimLogs/$name-stature-decisions.csv"))) { throw "Missing outputs: $name" }
        $files | ForEach-Object { @{ name=$_.Name; sha256=(Get-FileHash -LiteralPath $_.FullName).Hash; bytes=$_.Length } } |
            ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $dir "$name-outputs.json")
        Write-Host "Completed $name"
    }
}
