param(
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe',
    [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$Run = 'stature-continuation',
    [int]$Seed = 1001,
    [ValidateRange(1, 522)][int]$Weeks = 28,
    [ValidateRange(1, 52)][int]$ReplayWeeks = 3,
    [string]$TraceRecord,
    [string]$TraceRegion,
    [switch]$TraceWorld
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$dir = Join-Path $root 'SimLogs/writing-calibration'
if (Test-Path -LiteralPath (Join-Path $dir "$Run-request.json")) { throw 'Run already exists.' }
if ([bool]$TraceRecord -ne [bool]$TraceRegion) { throw 'Supply both trace record and region.' }
$source = Get-Content -Raw (Join-Path $dir 'stature-live-smoke-v3-1001-control-request.json') | ConvertFrom-Json
$flags = @($source.flags | Where-Object { $_ -notlike '--weeks=*' -and $_ -notlike '--run=*' -and $_ -notlike '--seed=*' })
$flags += "--seed=$Seed", "--weeks=$Weeks", '--stature-live-roundtrip=SimTools/StatureShadowExperimental.json', "--stature-replay-weeks=$ReplayWeeks"
if ($TraceRecord) { $flags += "--stature-replay-record=$TraceRecord", "--stature-replay-region=$TraceRegion" }
if ($TraceWorld) { $flags += '--stature-replay-world-trace' }
$assembly = Join-Path $root '.godot/mono/temp/bin/Debug/Label Man.dll'
$assemblyHash = (Get-FileHash -LiteralPath $assembly).Hash
@{ flags=$flags; assemblySha256=$assemblyHash; sourceRevision=(git rev-parse HEAD); trackedSourceDirty=$true;
   calibrationSha256=(Get-FileHash SimTools/StatureShadowExperimental.json).Hash; startedUtc=[datetime]::UtcNow.ToString('o') } |
    ConvertTo-Json -Depth 5 | Set-Content (Join-Path $dir "$Run-request.json")
$env:APPDATA = Join-Path $dir "userdata-$Run"
New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
$env:DOTNET_gcServer = '0'
$env:LABELMAN_WORKSTATION_GC = '1'
& $Godot --headless --path $root --log-file (Join-Path $dir "$Run-engine.log") SimTools/SaveLoadRoundTripRunner.tscn -- @flags > (Join-Path $dir "$Run.log") 2> (Join-Path $dir "$Run-errors.log")
if ($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath (Join-Path $dir "$Run.log") -SimpleMatch "STATURE_REPLAY_PASS weeks=$ReplayWeeks" -Quiet)) { throw "Continuation failed: $Run" }
if ((Get-Item -LiteralPath (Join-Path $dir "$Run-errors.log")).Length -ne 0) { throw 'Continuation produced errors.' }
if ((Get-FileHash -LiteralPath $assembly).Hash -ne $assemblyHash) { throw 'Assembly changed during run.' }
Write-Host "Passed $Run"
