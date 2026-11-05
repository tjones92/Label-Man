param(
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe',
    [int[]]$Seeds = @(1001, 2002),
    [ValidateRange(1, 522)][int]$Weeks = 522,
    [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$RunPrefix = 'stature-baseline-v1'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$dir = Join-Path $root 'SimLogs/writing-calibration'
$source = Get-Content -Raw (Join-Path $dir 'veteran-follower-on-market-2002-request.json') | ConvertFrom-Json
if (@(git status --porcelain --untracked-files=no).Count -ne 0) { throw 'Tracked source has local modifications; freeze them before building.' }
if (@(git ls-files --others --exclude-standard -- '*.cs').Count -ne 0) { throw 'Untracked C# source could affect the build; freeze or remove it first.' }
$revision = (git rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot determine source revision.' }
if (-not (Test-Path -LiteralPath $Godot)) { throw 'Godot executable not found.' }
foreach ($seed in $Seeds) {
    $name = "$RunPrefix-$seed"
    if (Test-Path -LiteralPath (Join-Path $dir "$name-request.json")) { throw "Run $name already exists; refusing to overwrite it." }
    if (Get-ChildItem -Path (Join-Path $root 'SimLogs') -Filter "$name-*.csv" | Select-Object -First 1) { throw "Output for $name already exists." }
}

dotnet build 'Label Man.csproj' --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
if (@(git status --porcelain --untracked-files=no).Count -ne 0 -or (git rev-parse HEAD).Trim() -ne $revision) { throw 'Source changed during build.' }
$assembly = Join-Path $root '.godot/mono/temp/bin/Debug/Label Man.dll'
$assemblyHash = (Get-FileHash -LiteralPath $assembly).Hash
# SimulationArguments.Resolve prepends the selected profile, excluding keys supplied explicitly.
# These are the effective argument strings, not a claim that every subsystem logged its internal settings.
$argumentSource = Get-Content -Raw (Join-Path $root 'Systems/SimulationArguments.cs')
$match = [regex]::Match($argumentSource, '(?s)private static readonly string\[\] PreviousProfile = \{(.*?)\};')
if (-not $match.Success) { throw 'Cannot extract the previous profile.' }
$profile = @([regex]::Matches($match.Groups[1].Value, '"(--[^"\r\n]+)"') | ForEach-Object { $_.Groups[1].Value })
if ($profile.Count -lt 50) { throw 'Previous profile extraction was incomplete.' }

foreach ($seed in $Seeds) {
    $name = "$RunPrefix-$seed"
    $flags = @($source.flags | ForEach-Object {
        if ($_ -like '--run=*') { "--run=$name" }
        elseif ($_ -like '--seed=*') { "--seed=$seed" }
        elseif ($_ -like '--weeks=*') { "--weeks=$Weeks" }
        else { $_ }
    })
    if (@($flags | Where-Object { $_ -eq '--lp-retail-v2-profile' }).Count -ne 1) { throw 'Expected the previous profile explicitly.' }
    $keys = @{}; foreach ($flag in $flags) { $keys[($flag -split '=', 2)[0]] = $true }
    $effective = @($profile | Where-Object { -not $keys.ContainsKey(($_ -split '=', 2)[0]) }) + $flags
    $env:APPDATA = Join-Path $dir "userdata-$name"
    $env:DOTNET_gcServer = '0'
    $env:LABELMAN_WORKSTATION_GC = '1'
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    $request = [ordered]@{ flags = $flags; effectiveArguments = $effective; profile = 'lp-retail-v2';
        sourceRevision = $revision; trackedSourceClean = $true; untrackedCSharp = $false;
        buildCommand = 'dotnet build Label Man.csproj --no-restore'; assemblySha256 = $assemblyHash;
        sourceRequestSha256 = (Get-FileHash -LiteralPath (Join-Path $dir 'veteran-follower-on-market-2002-request.json')).Hash;
        combinedFlagsSha256 = (Get-FileHash -LiteralPath (Join-Path $root 'SimTools/writing_combined_integration_flags.txt')).Hash;
        godotSha256 = (Get-FileHash -LiteralPath $Godot).Hash; startedUtc = [datetime]::UtcNow.ToString('o') }
    $request | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $dir "$name-request.json")
    if ((Get-FileHash -LiteralPath $assembly).Hash -ne $assemblyHash) { throw 'Assembly changed before run.' }
    & $Godot --headless --path $root --log-file (Join-Path $dir "$name-engine.log") SimTools/ChartAuditRunner.tscn -- @flags > (Join-Path $dir "$name.log") 2> (Join-Path $dir "$name-errors.log")
    if ($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath (Join-Path $dir "$name.log") -SimpleMatch "CHART_AUDIT_COMPLETE run=$name weeks=$Weeks" -Quiet)) { throw "Run $name did not complete successfully." }
    if ((Get-Item -LiteralPath (Join-Path $dir "$name-errors.log")).Length -ne 0) { throw "Run $name produced errors." }
    if ((Get-FileHash -LiteralPath $assembly).Hash -ne $assemblyHash) { throw 'Assembly changed during run.' }
    Write-Host "Completed $name"
}
