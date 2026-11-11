param(
    [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$RunPrefix = 'veteran-follower-channels-v1',
    [ValidateSet('full','no-stock','no-intent')][Parameter(Mandatory)][string]$Arm,
    [ValidateRange(1, 522)][int]$Weeks = 522,
    [ValidateRange(1, 2147483647)][int]$Seed = 2002,
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe',
    [string]$Python = 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe',
    [switch]$CompressOutputs
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $root
$dir = Join-Path $root 'SimLogs/writing-calibration'
$name = "$RunPrefix-$Seed-$Arm"
$requestPath = Join-Path $dir "$name-request.json"
if ((Test-Path -LiteralPath $requestPath) -or (Get-ChildItem -LiteralPath 'SimLogs' -Filter "$name-*.csv*" | Select-Object -First 1)) {
    throw "Run already exists: $name"
}
$reference = Join-Path $dir 'veteran-follower-on-market-2002-request.json'
$source = Get-Content -Raw -LiteralPath $reference | ConvertFrom-Json
if (@($source.flags | Where-Object { $_ -eq '--follower-demand' }).Count -ne 1 -or
    @($source.flags | Where-Object { $_ -eq '--lp-retail-v2-profile' }).Count -ne 1) { throw 'Unexpected reference profile.' }
$argumentSource = Join-Path $root 'Systems/SimulationArguments.cs'
$match = [regex]::Match((Get-Content -Raw -LiteralPath $argumentSource), '(?s)private static readonly string\[\] PreviousProfile = \{(.*?)\};')
if (!$match.Success) { throw 'Cannot resolve previous profile.' }
$profile = @([regex]::Matches($match.Groups[1].Value, '"(--[^"\r\n]+)"') | ForEach-Object { $_.Groups[1].Value })
if ($profile.Count -lt 50) { throw 'Incomplete previous profile.' }
$keys = @{}; foreach ($flag in $source.flags) { $keys[($flag -split '=', 2)[0]] = $true }
# Pass the resolved headless profile explicitly so both expensive chart observers can be omitted.
$flags = @((@($profile | Where-Object { !$keys.ContainsKey(($_ -split '=', 2)[0]) }) + $source.flags) | ForEach-Object {
    if ($_ -like '--run=*') { "--run=$name" }
    elseif ($_ -like '--seed=*') { "--seed=$Seed" }
    elseif ($_ -like '--weeks=*') { "--weeks=$Weeks" }
    elseif ($_ -notin @('--lp-retail-v2-profile', '--station-playlists', '--followup-chart-trace')) { $_ }
})
if ($Arm -eq 'no-stock') { $flags += '--follower-demand-no-advance-stock' }
if ($Arm -eq 'no-intent') { $flags += '--follower-demand-no-purchase-intent' }

$assembly = Join-Path $root '.godot/mono/temp/bin/Debug/Label Man.dll'
$frozen = @{}; foreach ($path in @($assembly, $Godot, $reference, $argumentSource)) { $frozen[$path] = (Get-FileHash -LiteralPath $path).Hash }
$sourceHashes = @{}
foreach ($folder in @('Data', 'Systems', 'SimTools', 'UI')) {
    Get-ChildItem -LiteralPath (Join-Path $root $folder) -Filter '*.cs' -Recurse |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
        ForEach-Object { $sourceHashes[$_.FullName.Substring($root.Length + 1)] = (Get-FileHash -LiteralPath $_.FullName).Hash }
}
$env:APPDATA = Join-Path $dir "userdata-$name"
New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
. (Join-Path $PSScriptRoot 'AuditGcEnvironment.ps1')
@{ flags=$flags; effectiveArguments=$flags; arm=$Arm; seed=$Seed; weeks=$Weeks; profile='lp-retail-v2';
   sourceRevision=(git rev-parse HEAD).Trim(); sourceContentSha256=$sourceHashes; assemblySha256=$frozen[$assembly];
   engineSha256=$frozen[$Godot]; referenceRequestSha256=$frozen[$reference];
   gcEnvironment=@{ DOTNET_gcServer=$env:DOTNET_gcServer; DOTNET_GCHeapCount=$env:DOTNET_GCHeapCount; LABELMAN_WORKSTATION_GC=$env:LABELMAN_WORKSTATION_GC };
   startedUtc=[datetime]::UtcNow.ToString('o') } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $requestPath
& $Godot --headless --path $root --log-file (Join-Path $dir "$name-engine.log") SimTools/ChartAuditRunner.tscn -- @flags > (Join-Path $dir "$name.log") 2> (Join-Path $dir "$name-errors.log")
if ($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath (Join-Path $dir "$name.log") -Pattern "^CHART_AUDIT_COMPLETE run=$name weeks=$Weeks$" -Quiet)) { throw "Run did not complete: $name" }
if ((Get-Item -LiteralPath (Join-Path $dir "$name-errors.log")).Length) { throw "Run produced errors: $name" }
foreach ($path in $frozen.Keys) { if ((Get-FileHash -LiteralPath $path).Hash -ne $frozen[$path]) { throw "Frozen input changed: $path" } }
$files = @(Get-ChildItem -LiteralPath 'SimLogs' -Filter "$name-*.csv")
if (!$files.Count) { throw "Missing outputs: $name" }
$files | ForEach-Object { @{ name=$_.Name; sha256=(Get-FileHash -LiteralPath $_.FullName).Hash; bytes=$_.Length } } |
    ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $dir "$name-outputs.json")
Write-Host "Completed $name"
if ($CompressOutputs) {
    & $Python SimTools/compress_stature_outputs.py --name $name
    if ($LASTEXITCODE -ne 0) { throw "Output compression failed: $name" }
}
