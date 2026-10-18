[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-zA-Z0-9-]+$')][string]$Run,
    [ValidateRange(1,522)][int]$Weeks = 8,
    [int]$Seed = 1001,
    [string]$ResumeWorld,
    [string]$SaveWorld,
    [string[]]$ExtraArgs = @(),
    [switch]$Detailed,
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference = 'Stop'
$benchmarkRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$benchmarkPrefix = Join-Path $benchmarkRoot "SimLogs/$Run"
if (Get-ChildItem -LiteralPath (Join-Path $benchmarkRoot 'SimLogs') -Filter "$Run-*" -ErrorAction SilentlyContinue) {
    throw "Artifacts already exist for $Run; choose a new run name."
}
if (Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$') { throw 'Another game is active.' }
$benchmarkAssembly = Join-Path $benchmarkRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$benchmarkHash = (Get-FileHash -LiteralPath $benchmarkAssembly).Hash
$benchmarkFlags = @('--enable-genre-market-v2','--enable-artist-population-lifecycle',"--seed=$Seed")
if ($ResumeWorld) {
    if ($ResumeWorld -notmatch '^[a-zA-Z0-9-]+$') { throw 'Invalid world name.' }
    $benchmarkWorldPath = Join-Path $benchmarkRoot "SimLogs/worlds/$ResumeWorld.world.json.gz"
    # Read only the metadata prefix, even for multi-gigabyte decompressed worlds.
    $benchmarkStream = [IO.File]::OpenRead($benchmarkWorldPath)
    try {
        $benchmarkGzip = [IO.Compression.GZipStream]::new($benchmarkStream,[IO.Compression.CompressionMode]::Decompress)
        $benchmarkReader = [IO.StreamReader]::new($benchmarkGzip)
        try {
            $benchmarkBuffer = [char[]]::new(65536)
            $benchmarkCount = $benchmarkReader.ReadBlock($benchmarkBuffer,0,$benchmarkBuffer.Length)
            $benchmarkHeader = [string]::new($benchmarkBuffer,0,$benchmarkCount)
            $benchmarkMatch = [regex]::Match($benchmarkHeader,'"Flags"\s*:\s*(\[.*?\])',[Text.RegularExpressions.RegexOptions]::Singleline)
            if (!$benchmarkMatch.Success) { throw 'World flag metadata is missing.' }
            $benchmarkFlags = @($benchmarkMatch.Groups[1].Value | ConvertFrom-Json)
            if ($benchmarkFlags -notcontains "--seed=$Seed") { throw 'World seed differs from the requested seed.' }
        } finally { $benchmarkReader.Dispose() }
    } finally { $benchmarkStream.Dispose() }
}
$benchmarkArgs = @('--headless','--path','.', 'SimTools/ChartAuditRunner.tscn','--',"--weeks=$Weeks","--run=$Run",'--profile-performance') + $benchmarkFlags
if (!$Detailed) { $benchmarkArgs += @('--calibration','--small-label-finance') }
if ($ResumeWorld) { $benchmarkArgs += "--resume-world=$ResumeWorld" }
if ($SaveWorld) {
    if ($SaveWorld -notmatch '^[a-zA-Z0-9-]+$') { throw 'Invalid output world name.' }
    if (Test-Path -LiteralPath (Join-Path $benchmarkRoot "SimLogs/worlds/$SaveWorld.world.json.gz")) { throw 'Output world already exists.' }
    $benchmarkArgs += "--save-world-at-end=$SaveWorld"
}
$benchmarkArgs += $ExtraArgs
$benchmarkManifest = [ordered]@{run=$Run; weeks=$Weeks; seed=$Seed; resumeWorld=$ResumeWorld;
    assemblySha256=$benchmarkHash; arguments=$benchmarkArgs; startedUtc=[datetime]::UtcNow.ToString('o')}
$benchmarkManifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath "$benchmarkPrefix-invocation.json"
$benchmarkWatch = [Diagnostics.Stopwatch]::StartNew()
Push-Location -LiteralPath $benchmarkRoot
try {
    & $Godot @benchmarkArgs *> "$benchmarkPrefix-console.log"
    $benchmarkExit = $LASTEXITCODE
} finally { Pop-Location }
$benchmarkManifest.elapsedSeconds = $benchmarkWatch.Elapsed.TotalSeconds
$benchmarkManifest.exitCode = $benchmarkExit
$benchmarkManifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath "$benchmarkPrefix-invocation.json"
if ((Get-FileHash -LiteralPath $benchmarkAssembly).Hash -ne $benchmarkHash) { throw 'Assembly changed during benchmark.' }
if ($benchmarkExit -ne 0 -or !(Select-String -LiteralPath "$benchmarkPrefix-console.log" -SimpleMatch "CHART_AUDIT_COMPLETE run=$Run weeks=$Weeks" -Quiet)) {
    throw "Benchmark failed; inspect $benchmarkPrefix-console.log."
}
Import-Csv -LiteralPath "$benchmarkPrefix-performance-profile.csv" |
    Select-Object seed,year,measuredWeeks,wallSeconds,activeRecords,activeAlbums,simulateWeekSeconds,competitorWeekSeconds,dailyTalentMarketSeconds,RoomCalendarSeconds |
    ConvertTo-Json -Compress
Write-Output "Completed $Run in $($benchmarkWatch.Elapsed.TotalSeconds.ToString('F2')) process seconds (startup and snapshot IO included)."
