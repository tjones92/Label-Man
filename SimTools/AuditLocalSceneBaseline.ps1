[CmdletBinding()]
param([string]$OutputDirectory = 'SimLogs/local-scene-phase-0')

# Static source audit only: never starts Godot, builds the game, or loads a save.
$ErrorActionPreference = 'Stop'
$sceneRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$sceneOutput = [IO.Path]::GetFullPath((Join-Path $sceneRoot $OutputDirectory))
$sceneLogs = [IO.Path]::GetFullPath((Join-Path $sceneRoot 'SimLogs'))
if (!$sceneOutput.StartsWith($sceneLogs + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'OutputDirectory must be a child of this checkout''s SimLogs directory.'
}
$tracked = @(& git -C $sceneRoot ls-files)
if ($LASTEXITCODE -ne 0) { throw 'git ls-files failed' }
$sourcePaths = @($tracked | Where-Object { $_ -match '^(Systems|Data|UI)/.*\.cs$|^(project\.godot|chart_manager\.tscn|Label Man\.csproj)$' } | Sort-Object)
$hashes = @($sourcePaths | ForEach-Object {
    [ordered]@{ path = $_; sha256 = (Get-FileHash -LiteralPath (Join-Path $sceneRoot $_) -Algorithm SHA256).Hash.ToLowerInvariant() }
})
$distanceSource = Get-Content -LiteralPath (Join-Path $sceneRoot 'Systems/DistanceModel.cs') -Raw
$cityPattern = 'AddCity\("(?<id>[^"]+)", "(?<name>[^"]+)", (?<lat>-?[\d.]+)f, (?<lon>-?[\d.]+)f, "(?<region>[^"]+)".*?, (?<population>[\d.]+)f\);'
$cities = @([regex]::Matches($distanceSource, $cityPattern) | ForEach-Object {
    [ordered]@{ cityId = $_.Groups['id'].Value; name = $_.Groups['name'].Value; regionId = $_.Groups['region'].Value;
        latitude = $_.Groups['lat'].Value; longitude = $_.Groups['lon'].Value;
        legacyStoreSeedPopulationMillions = $_.Groups['population'].Value; populationEvidence = 'unsourced simulation input; not a historical census' }
})
$atlas = Get-Content -LiteralPath (Join-Path $sceneRoot 'SimTools/LocalSceneHistoricalAtlas.md') -Raw
$atlasIds = @([regex]::Matches($atlas, '(?m)^\| `(?<id>[a-z_]+)` \|') | ForEach-Object { $_.Groups['id'].Value })
if ($cities.Count -ne 31 -or @($cities.cityId | Sort-Object -Unique).Count -ne 31) { throw 'Expected 31 unique playable cities; review registry parser/content.' }
if (@(Compare-Object @($cities.cityId | Sort-Object) @($atlasIds | Sort-Object)).Count -ne 0) { throw 'Atlas and playable city IDs differ.' }
$callPattern = 'GenerateArtist\(|new SimulatedArtist|CreateSoloSpinOut\(|RemoveUnsignedArtist\(|artistRegistry\.(Remove|Clear)|artistRegistry\[|SignArtist\(|DropArtist\(|NationalFreshRecovery|GetEnabledSupplyCandidates\(|RestoreArtist\(|generatedProspectIds|homeCity\b'
$seams = @()
$flags = @()
foreach ($relative in $sourcePaths) {
    $lines = @(Get-Content -LiteralPath (Join-Path $sceneRoot $relative))
    for ($index = 0; $index -lt $lines.Count; $index++) {
        $line = $lines[$index]
        if ($line -match $callPattern) { $seams += [ordered]@{ path = $relative; line = $index + 1; text = $line.Trim() } }
        foreach ($flagMatch in [regex]::Matches($line, '--(?:enable|disable|observe|shadow|polar)-[a-z0-9-]+(?:=[a-z]+)?')) {
            $flags += [ordered]@{ flag = $flagMatch.Value; path = $relative; line = $index + 1 }
        }
    }
}
$sceneDefaults = @(Get-Content -LiteralPath (Join-Path $sceneRoot 'chart_manager.tscn') | Where-Object { $_ -match '^\w+Enabled = (true|false)$' })
$revision = (& git -C $sceneRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'git rev-parse failed' }
$manifest = [ordered]@{
    schemaVersion = 1; revision = $revision; scope = 'static source baseline; no runtime or economic acceptance implied';
    saveVersion = [regex]::Match((Get-Content -LiteralPath (Join-Path $sceneRoot 'Systems/SaveGameService.cs') -Raw), 'CurrentVersion = (\d+)').Groups[1].Value;
    runtimeCensusStatus = 'pending coordinated Godot test window'; cityCount = $cities.Count;
    sceneDefaults = $sceneDefaults; sourceHashes = $hashes; flags = $flags; ownershipSeams = $seams; cities = $cities
}
New-Item -ItemType Directory -Path $sceneOutput -Force | Out-Null
$manifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $sceneOutput 'source-baseline.json') -Encoding utf8
$cities | ForEach-Object { [pscustomobject]$_ } | Export-Csv -LiteralPath (Join-Path $sceneOutput 'geography.csv') -NoTypeInformation -Encoding utf8
Write-Output "Static scene audit passed: $($cities.Count) cities, $($hashes.Count) source hashes, $($seams.Count) ownership/geography references."
Write-Output "Artifacts: $sceneOutput"
