param(
    [Parameter(Mandatory)][string]$Checkpoint,
    [int]$Week = 1,
    [int]$Top = 12
)
$ErrorActionPreference = 'Stop'
function Read-World([string]$Path) {
    $file = [IO.File]::OpenRead($Path)
    try {
        $gzip = [IO.Compression.GZipStream]::new($file, [IO.Compression.CompressionMode]::Decompress)
        try { return [Text.Json.JsonDocument]::Parse($gzip, [Text.Json.JsonDocumentOptions]::new()) }
        finally { $gzip.Dispose() }
    } finally { $file.Dispose() }
}
function Differences($Left, $Right, [string[]]$Fields) {
    $result = foreach ($field in $Fields) {
        $a = [Text.Json.JsonElement]::new(); $b = [Text.Json.JsonElement]::new()
        if ($Left.TryGetProperty($field, [ref]$a) -and $Right.TryGetProperty($field, [ref]$b) -and $a.GetRawText() -ne $b.GetRawText()) {
            "$field=$($a.GetRawText())/$($b.GetRawText())"
        }
    }
    return ($result -join ', ')
}
$name = 'world-week-{0:D3}.json.gz' -f $Week
$left = Read-World (Join-Path $Checkpoint $name)
$right = Read-World (Join-Path $Checkpoint "resumed-$name")
try {
    $byId = @{}
    foreach ($record in $right.RootElement.GetProperty('Records').EnumerateArray()) { $byId[$record.GetProperty('baseRecord').GetProperty('recordId').GetString()] = $record }
    $changes = [Collections.Generic.List[object]]::new()
    foreach ($record in $left.RootElement.GetProperty('Records').EnumerateArray()) {
        $id = $record.GetProperty('baseRecord').GetProperty('recordId').GetString()
        if (!$byId.ContainsKey($id)) { Write-Output "Missing resumed record: $id"; continue }
        $other = $byId[$id]
        foreach ($region in $record.GetProperty('regionalData').EnumerateObject()) {
            $otherRegion = [Text.Json.JsonElement]::new()
            if (!$other.GetProperty('regionalData').TryGetProperty($region.Name, [ref]$otherRegion)) { continue }
            $rawA = $region.Value.GetProperty('rawDemandThisWeek').GetDouble()
            $rawB = $otherRegion.GetProperty('rawDemandThisWeek').GetDouble()
            if ($rawA -eq $rawB) { continue }
            $changes.Add([pscustomobject]@{
                Record=$id; Region=$region.Name; RawUninterrupted=$rawA; RawResumed=$rawB; Delta=[math]::Abs($rawA-$rawB)
                RecordInputs=(Differences $record $other @('artistHeat','artistPreviousHits','awareness','radioHeat','momentum','currentLabelPush','weeksSinceRelease','peakPosition','currentPosition'))
                MasterInputs=(Differences $record.GetProperty('baseRecord') $other.GetProperty('baseRecord') @('hookStrength','productionQuality','danceability','primaryGenre','secondaryGenre','albumProjectId','projectRole'))
                RegionInputs=(Differences $region.Value $otherRegion @('demandIntrinsicConversionRate','demandPotentialAudience','demandBaselineAwareness','demandEarnedDiscoveryExposure','salesEffectiveAwarenessThisWeek','salesRegionalAwarenessThisWeek','salesRegionalRadioPlayThisWeek','genreDemandAcceptanceThisWeek','localSalesRank','breakoutWeekStartStock'))
            })
        }
    }
    Write-Output "Changed raw-demand record/regions: $($changes.Count)"
    $changes | Sort-Object Delta -Descending | Select-Object -First $Top | Format-List
} finally { $left.Dispose(); $right.Dispose() }
