[CmdletBinding()]
param(
    [string]$RunTag = 'scene1-fixed-v1',
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe',
    [int]$MinimumFreeMiB = 1536
)

# Invoke only within a user-authorized game test window. Tests run serially.
$ErrorActionPreference = 'Stop'
$sceneRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ($RunTag -notmatch '^[a-z0-9-]+$') { throw 'RunTag must contain lowercase letters, digits and hyphens.' }
if ($MinimumFreeMiB -lt 1536) { throw 'Short suite requires at least 1536 MiB of available RAM before each job.' }
if (!(Test-Path -LiteralPath $Godot)) { throw 'Godot binary missing.' }
$sceneArtifactPath = Join-Path $sceneRoot "SimLogs/$RunTag"
if (Test-Path -LiteralPath $sceneArtifactPath) { throw 'Run family exists; choose a new RunTag.' }
$sceneAssembly = Join-Path $sceneRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
if (!(Test-Path -LiteralPath $sceneAssembly)) { throw 'Build the Debug assembly first.' }
$sceneFlags = @('--enable-genre-market-v2', '--enable-artist-population-lifecycle', '--enable-artist-evolution',
    '--enable-artist-recognition', '--enable-managers', '--seed-star-canopy', '--enable-cowriting',
    '--enable-member-axes', '--observe-band-life', '--enable-lineup-churn=world', '--member-fame-share=0.90')
$sceneJobs = @(
    @{ name = 'checks-off'; weeks = 0; flags = @('--disable-local-scenes', '--local-scene-identity-check'); marker = 'SCENE_IDENTITY_CHECK_PASS' },
    @{ name = 'checks-observe'; weeks = 0; flags = @('--observe-local-scenes', '--local-scene-identity-check'); marker = 'SCENE_IDENTITY_CHECK_PASS' },
    @{ name = 'roundtrip-off-0'; weeks = 0; flags = @('--disable-local-scenes'); marker = 'SAVELOAD_ROUNDTRIP_PASS' },
    @{ name = 'roundtrip-observe-0'; weeks = 0; flags = @('--observe-local-scenes'); marker = 'SAVELOAD_ROUNDTRIP_PASS' },
    @{ name = 'roundtrip-off-26'; weeks = 26; flags = @('--disable-local-scenes'); marker = 'SAVELOAD_ROUNDTRIP_PASS' },
    @{ name = 'roundtrip-observe-26'; weeks = 26; flags = @('--observe-local-scenes'); marker = 'SAVELOAD_ROUNDTRIP_PASS' }
)
function Assert-SceneTestCapacity {
    if (@(Get-Process | Where-Object { $_.ProcessName -match '^Godot|^Label Man$' }).Count -gt 0) {
        throw 'Another game process is active. This serial suite waits for a clear test window.'
    }
    $sceneFreeMiB = [math]::Floor((Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1024)
    if ($sceneFreeMiB -lt $MinimumFreeMiB) { throw "Insufficient RAM headroom: $sceneFreeMiB MiB available; $MinimumFreeMiB MiB required." }
    return $sceneFreeMiB
}
$sceneInitialFree = Assert-SceneTestCapacity
New-Item -ItemType Directory -Path $sceneArtifactPath | Out-Null
$sceneManifest = [ordered]@{ revision = (& git -C $sceneRoot rev-parse HEAD).Trim();
    godot = $Godot; godotSha256 = (Get-FileHash -LiteralPath $Godot).Hash;
    assemblySha256 = (Get-FileHash -LiteralPath $sceneAssembly).Hash; commonFlags = $sceneFlags;
    seed = 1001; initialFreeMiB = $sceneInitialFree; jobs = $sceneJobs; results = @();
    purpose = 'Phase 1 fixed identity and world/gzip save probes; economic and real-player acceptance remain separate' }
$sceneManifestFile = Join-Path $sceneArtifactPath 'runs.json'
$sceneManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $sceneManifestFile -Encoding utf8
foreach ($sceneJob in $sceneJobs) {
    $sceneFreeBefore = Assert-SceneTestCapacity
    $sceneStdout = Join-Path $sceneArtifactPath "$($sceneJob.name)-console.log"
    $sceneStderr = Join-Path $sceneArtifactPath "$($sceneJob.name)-errors.log"
    $sceneArgs = @('--headless', '--path', '.', 'SimTools/SaveLoadRoundTripRunner.tscn', '--', '--seed=1001', "--weeks=$($sceneJob.weeks)") + $sceneFlags + $sceneJob.flags
    Write-Output "Starting $($sceneJob.name), available RAM $sceneFreeBefore MiB."
    $sceneStarted = [DateTime]::UtcNow
    $sceneProcess = Start-Process -FilePath $Godot -ArgumentList $sceneArgs -WorkingDirectory $sceneRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $sceneStdout -RedirectStandardError $sceneStderr
    # The console launcher owns the engine child. Do not kill other runs or impose a launcher-only timeout.
    while (!$sceneProcess.WaitForExit(10000)) { Write-Output "Running $($sceneJob.name): $([int]([DateTime]::UtcNow - $sceneStarted).TotalSeconds)s." }
    $sceneProcess.WaitForExit()
    $sceneOutput = Get-Content -LiteralPath $sceneStdout -Raw
    $scenePassed = $sceneProcess.ExitCode -eq 0 -and $sceneOutput.Contains($sceneJob.marker)
    $sceneManifest.results += [ordered]@{ name = $sceneJob.name; exitCode = $sceneProcess.ExitCode;
        passed = $scenePassed; freeMiBBefore = $sceneFreeBefore; elapsedSeconds = ([DateTime]::UtcNow - $sceneStarted).TotalSeconds }
    $sceneManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $sceneManifestFile -Encoding utf8
    if (!$scenePassed) { throw "Failed $($sceneJob.name). Inspect $sceneStdout and $sceneStderr before continuing." }
    Write-Output "Passed $($sceneJob.name)."
}
Write-Output "Short suite complete. Manifest: $sceneManifestFile"
