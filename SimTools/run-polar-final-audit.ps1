param(
    [string]$Godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe',
    [ValidateSet(1001,1002)][int]$Seed = 1001,
    [ValidateSet('on','off')][string]$Mode = 'on',
    [ValidateRange(1,209)][int]$Weeks = 2,
    [string]$Run = "polar-final-$Mode-$Seed",
    [switch]$NoCensus,
    [ValidateSet('sample','full','none')][string]$Census = 'none',
    [ValidateRange(1,900)][int]$MaxSeconds = 300
)
$ErrorActionPreference = 'Stop'
$polarWorkspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location -LiteralPath $polarWorkspace
try {
    if ($Run -notmatch '^[a-zA-Z0-9-]+$') { throw 'Invalid run name.' }
    if (Get-ChildItem -LiteralPath SimLogs -Filter "$Run*" -ErrorAction SilentlyContinue) { throw "Run artifacts already exist: $Run" }
    $polarArgs = @('--headless','--path','.', 'SimTools/ChartAuditRunner.tscn','--', "--seed=$Seed", "--run=$Run", "--weeks=$Weeks",
        '--enable-genre-market-v2','--enable-artist-population-lifecycle','--calibration','--polar-final-audit',"--polar-census=$Census","--polar-max-seconds=$MaxSeconds")
    if ($Mode -eq 'off') { $polarArgs += '--disable-polar-fit-selection' }
    if ($NoCensus) { $polarArgs += '--polar-no-census' }
    [ordered]@{run=$Run; seed=$Seed; mode=$Mode; weeks=$Weeks; arguments=$polarArgs;
        startedUtc=[DateTime]::UtcNow.ToString('o');
        tableSha256=(Get-FileHash Data/PolarSongTable.json).Hash;
        perceptionTableSha256=(Get-FileHash Data/PolarPerceptionTable.json).Hash;
        assemblySha256=(Get-FileHash '.godot/mono/temp/bin/Debug/Label Man.dll').Hash
    } | ConvertTo-Json -Depth 4 | Set-Content "SimLogs/$Run-invocation.json"
    $ErrorActionPreference = 'Continue'
    & $Godot @polarArgs *> "SimLogs/$Run.log"
    $polarExit = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    if ($polarExit -ne 0 -or -not (Select-String -LiteralPath "SimLogs/$Run.log" -SimpleMatch "CHART_AUDIT_COMPLETE run=$Run weeks=$Weeks" -Quiet)) {
        throw "Incomplete audit $Run (exit $polarExit)."
    }
    if ($Weeks -eq 521 -and -not (Select-String -LiteralPath "SimLogs/$Run.log" -SimpleMatch 'POLAR_FINAL_DATE date=12/31/1969' -Quiet)) {
        throw "Decade endpoint missing: $Run"
    }
    Write-Output "Completed $Run"
} finally { Pop-Location }
