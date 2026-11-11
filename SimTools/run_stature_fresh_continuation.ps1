param(
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe',
    [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$Run = 'stature-fresh-continuation',
    [int]$Seed = 1001,
    [ValidateRange(1, 522)][int]$Weeks = 28,
    [ValidateRange(1, 52)][int]$ReplayWeeks = 13,
    [switch]$TraceWorld,
    [switch]$PlayerSave,
    [switch]$ActivePlayer
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$dir = Join-Path $root 'SimLogs/writing-calibration'
$checkpoint = Join-Path $dir "$Run-checkpoint"
if ((Test-Path -LiteralPath (Join-Path $dir "$Run-request.json")) -or (Test-Path -LiteralPath $checkpoint)) { throw 'Run already exists.' }
$source = Get-Content -Raw (Join-Path $dir 'stature-live-smoke-v3-1001-control-request.json') | ConvertFrom-Json
$flags = @($source.flags | Where-Object { $_ -notlike '--weeks=*' -and $_ -notlike '--run=*' -and $_ -notlike '--seed=*' })
$flags += "--seed=$Seed", "--weeks=$Weeks", '--stature-live-roundtrip=SimTools/StatureShadowExperimental.json', "--stature-replay-weeks=$ReplayWeeks"
if ($TraceWorld) { $flags += '--stature-replay-world-trace' }
if ($PlayerSave) { $flags += '--stature-player-save' }
if ($ActivePlayer) {
    if (!$PlayerSave) { throw 'ActivePlayer requires PlayerSave.' }
    $flags += '--stature-active-player'
}
$assembly = Join-Path $root '.godot/mono/temp/bin/Debug/Label Man.dll'
$assemblyHash = (Get-FileHash -LiteralPath $assembly).Hash
$engineHash = (Get-FileHash -LiteralPath $Godot).Hash
$calibrationHash = (Get-FileHash SimTools/StatureShadowExperimental.json).Hash
@{ flags=$flags; assemblySha256=$assemblyHash; engineSha256=$engineHash; calibrationSha256=$calibrationHash;
   checkpoint=$checkpoint; sourceRevision=(git rev-parse HEAD); trackedSourceDirty=$true; startedUtc=[datetime]::UtcNow.ToString('o') } |
    ConvertTo-Json -Depth 5 | Set-Content (Join-Path $dir "$Run-request.json")
foreach ($arm in @('record','resume')) {
    if ((Get-FileHash -LiteralPath $assembly).Hash -ne $assemblyHash) { throw 'Assembly changed before run.' }
    $env:APPDATA = Join-Path $dir "userdata-$Run-$arm"
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    $env:DOTNET_gcServer = '0'
    $env:LABELMAN_WORKSTATION_GC = '1'
    $mode = if ($arm -eq 'record') { "--stature-checkpoint-write=$checkpoint" } else { "--stature-checkpoint-read=$checkpoint" }
    & $Godot --headless --path $root --log-file (Join-Path $dir "$Run-$arm-engine.log") SimTools/SaveLoadRoundTripRunner.tscn -- @flags $mode > (Join-Path $dir "$Run-$arm.log") 2> (Join-Path $dir "$Run-$arm-errors.log")
    $code = $LASTEXITCODE
    $marker = if ($arm -eq 'record') { "STATURE_FRESH_REFERENCE_PASS weeks=$ReplayWeeks" } else { "STATURE_FRESH_REPLAY_PASS weeks=$ReplayWeeks" }
    if ($code -ne 0 -or !(Select-String -LiteralPath (Join-Path $dir "$Run-$arm.log") -SimpleMatch $marker -Quiet)) { throw "Fresh continuation failed: $Run $arm" }
    if ((Get-Item -LiteralPath (Join-Path $dir "$Run-$arm-errors.log")).Length -ne 0) { throw "Nonempty stderr: $arm" }
    if ((Get-FileHash -LiteralPath $assembly).Hash -ne $assemblyHash -or (Get-FileHash -LiteralPath $Godot).Hash -ne $engineHash -or (Get-FileHash SimTools/StatureShadowExperimental.json).Hash -ne $calibrationHash) { throw 'Build or calibration changed during run.' }
    Write-Host "Passed $Run $arm"
}
if ($TraceWorld) {
    foreach ($week in (@(1, $ReplayWeeks) | Select-Object -Unique)) {
        & (Join-Path $PSScriptRoot 'inspect_stature_saved_world.ps1') -Checkpoint $checkpoint -Week $week -FailOnDifferences
    }
    Write-Host "STATURE_FRESH_WORLD_PASS first=1 final=$ReplayWeeks"
}
