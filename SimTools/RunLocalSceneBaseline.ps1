[CmdletBinding()]
param([string]$RunTag = 'scene0-control-v1', [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe')

# Run only during a user-authorized simulation window. One engine at a time.
$ErrorActionPreference = 'Stop'
$sceneRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ($RunTag -notmatch '^[a-z0-9-]+$') { throw 'RunTag must contain lowercase letters, digits and hyphens.' }
if (!(Test-Path -LiteralPath $Godot)) { throw 'Godot binary missing.' }
$sceneArtifacts = Join-Path $sceneRoot "SimLogs/$RunTag"
if (Test-Path -LiteralPath $sceneArtifacts) { throw 'Run family exists; choose a new RunTag.' }
$sceneFlags = @('--disable-local-scenes', '--enable-genre-market-v2', '--enable-artist-population-lifecycle', '--enable-artist-evolution', '--enable-artist-recognition', '--enable-managers', '--seed-star-canopy', '--enable-cowriting', '--enable-member-axes', '--observe-band-life', '--enable-lineup-churn=world', '--member-fame-share=0.45')
$sceneJobs = @(
    @{ name = "$RunTag-1001"; seed = 1001; weeks = 52; extra = @('--save-world-at-year=1960', "--save-world=$RunTag-w0-1001") },
    @{ name = "$RunTag-repeat-1001"; seed = 1001; weeks = 52; extra = @('--save-world-at-year=1960', "--save-world=$RunTag-w0-repeat-1001") },
    @{ name = "$RunTag-2002"; seed = 2002; weeks = 52; extra = @('--save-world-at-year=1960', "--save-world=$RunTag-w0-2002") },
    @{ name = "$RunTag-replay-a-1001"; seed = 1001; weeks = 8; extra = @("--resume-world=$RunTag-w0-1001") },
    @{ name = "$RunTag-replay-b-1001"; seed = 1001; weeks = 8; extra = @("--resume-world=$RunTag-w0-1001") }
)
foreach ($sceneJob in $sceneJobs) {
    if (@(Get-ChildItem -LiteralPath (Join-Path $sceneRoot 'SimLogs') -Filter "$($sceneJob.name)-*").Count -gt 0) { throw "Artifacts exist for $($sceneJob.name)." }
}
New-Item -ItemType Directory -Path $sceneArtifacts | Out-Null
$sceneManifest = [ordered]@{ revision = (& git -C $sceneRoot rev-parse HEAD).Trim(); godot = $Godot;
    godotSha256 = (Get-FileHash -LiteralPath $Godot).Hash; assemblySha256 = (Get-FileHash -LiteralPath (Join-Path $sceneRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash;
    flags = $sceneFlags; telemetryFlags = @('--aggregate-only'); jobs = $sceneJobs; results = @(); purpose = 'Phase 0 bounded controls, census and reload repeatability; no treatment or decade economic acceptance' }
$sceneManifestPath = Join-Path $sceneArtifacts 'runs.json'
$sceneManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $sceneManifestPath -Encoding utf8
foreach ($sceneJob in $sceneJobs) {
    if (@(Get-Process | Where-Object { $_.ProcessName -match '^Godot|^Label Man$' }).Count -gt 0) { throw 'Another Godot/game process is active; stop and coordinate before retrying.' }
    $sceneArgs = @('--headless', '--path', '.', 'SimTools/ChartAuditRunner.tscn', '--', "--weeks=$($sceneJob.weeks)", "--run=$($sceneJob.name)", "--seed=$($sceneJob.seed)", '--aggregate-only') + $sceneFlags + $sceneJob.extra
    Write-Output "Starting $($sceneJob.name): $($sceneJob.weeks) weeks, seed $($sceneJob.seed)."
    $sceneProcess = Start-Process -FilePath $Godot -ArgumentList $sceneArgs -WorkingDirectory $sceneRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $sceneArtifacts "$($sceneJob.name)-console.log") -RedirectStandardError (Join-Path $sceneArtifacts "$($sceneJob.name)-errors.log")
    $sceneStarted = [DateTime]::UtcNow
    while (!$sceneProcess.WaitForExit(10000)) {
        if (([DateTime]::UtcNow - $sceneStarted).TotalMinutes -gt 30) {
            Stop-Process -Id $sceneProcess.Id
            throw "Run timed out: $($sceneJob.name)."
        }
    }
    $sceneProcess.WaitForExit()
    $sceneLog = Get-Content -LiteralPath (Join-Path $sceneArtifacts "$($sceneJob.name)-console.log") -Raw
    $sceneRows = @(Import-Csv -LiteralPath (Join-Path $sceneRoot "SimLogs/$($sceneJob.name)-weeks.csv"))
    $sceneResult = [ordered]@{ name = $sceneJob.name; exitCode = $sceneProcess.ExitCode; weeks = $sceneRows.Count;
        completed = $sceneLog.Contains("CHART_AUDIT_COMPLETE run=$($sceneJob.name) weeks=$($sceneJob.weeks)"); elapsedSeconds = ([DateTime]::UtcNow - $sceneStarted).TotalSeconds }
    $sceneManifest.results += $sceneResult
    $sceneManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $sceneManifestPath -Encoding utf8
    if ($sceneProcess.ExitCode -ne 0 -or !$sceneResult.completed -or $sceneRows.Count -ne $sceneJob.weeks) { throw "Incomplete baseline: $($sceneJob.name). See run logs." }
    Write-Output "Completed $($sceneJob.name): exit 0, $($sceneRows.Count) weeks."
}
Write-Output "All Phase 0 controls completed. Manifest: $sceneManifestPath"
