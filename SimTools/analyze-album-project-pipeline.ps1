param([string[]]$Runs = @('a1-1001a', 'a1-1002', 'a1-1003'))

$adult = @('Jazz', 'EasyListening', 'Folk', 'TraditionalPop', 'BossaNova', 'Country')
$youth = @('RockAndRoll', 'TeenPop', 'RnB', 'DooWop', 'GirlGroup')

function Get-Mean($Values) {
    if ($Values.Count -eq 0) { return [double]::NaN }
    return ($Values | Measure-Object -Average).Average
}

function Get-Median($Values) {
    $sorted = @($Values | Sort-Object)
    if ($sorted.Count -eq 0) { return [double]::NaN }
    $middle = [int][Math]::Floor(($sorted.Count - 1) / 2)
    if ($sorted.Count % 2) { return [double]$sorted[$middle] }
    return ([double]$sorted[$middle] + [double]$sorted[$middle + 1]) / 2
}

function Get-Correlation($Xs, $Ys) {
    if ($Xs.Count -lt 2) { return [double]::NaN }
    $meanX = Get-Mean $Xs
    $meanY = Get-Mean $Ys
    $covariance = 0.0
    $varianceX = 0.0
    $varianceY = 0.0
    for ($index = 0; $index -lt $Xs.Count; $index++) {
        $deltaX = [double]$Xs[$index] - $meanX
        $deltaY = [double]$Ys[$index] - $meanY
        $covariance += $deltaX * $deltaY
        $varianceX += $deltaX * $deltaX
        $varianceY += $deltaY * $deltaY
    }
    return $covariance / [Math]::Sqrt($varianceX * $varianceY)
}

foreach ($run in $Runs) {
    $strategies = @(Import-Csv "SimLogs/$run-release-strategy.csv")
    $outcomes = @(Import-Csv "SimLogs/$run-release-outcomes.csv" | Where-Object memoryEligible -eq 'true')
    $liveRecords = @(Import-Csv "SimLogs/$run-live-records-snapshot.csv")
    $recordRows = @(Import-Csv "SimLogs/$run-records.csv")
    $lifecycles = @(Import-Csv "SimLogs/$run-lifecycles.csv")
    $albumChart = @(Import-Csv "SimLogs/$run-album-chart.csv")
    $costAssumptions = @(Import-Csv "SimLogs/$run-prior-cost-assumptions.csv")

    $albums = @($strategies | Where-Object chosenFormat -eq 'Album')
    $adultStrategies = @($strategies | Where-Object genre -in $adult)
    $youthStrategies = @($strategies | Where-Object genre -in $youth)
    $adultAlbums = @($adultStrategies | Where-Object chosenFormat -eq 'Album')
    $youthAlbums = @($youthStrategies | Where-Object chosenFormat -eq 'Album')
    $adultAlbumRows = @($albumChart | Where-Object genre -in $adult)
    $chartedRows = @($recordRows | Where-Object { [int]$_.currentPosition -gt 0 })
    $adultChartRows = @($chartedRows | Where-Object genre -in $adult)
    $closedTop40 = @($lifecycles | Where-Object { [int]$_.peakPosition -gt 0 -and [int]$_.peakPosition -le 40 } | ForEach-Object { [double]$_.weeksOnChart })

    $firstRecordRow = @{}
    foreach ($row in $recordRows) {
        if (-not $firstRecordRow.ContainsKey($row.recordId)) { $firstRecordRow[$row.recordId] = $row }
    }
    $qualities = @()
    $lifetimeUnits = @()
    foreach ($row in $lifecycles) {
        if ($row.leftCensoredAtRunStart -eq 'true') { continue }
        $first = $firstRecordRow[$row.recordId]
        if ($null -ne $first) {
            $qualities += [double]$first.quality
            $lifetimeUnits += [double]$row.lifetimeUnitsSold
        }
    }

    $strategyById = @{}
    foreach ($row in $strategies) { $strategyById[$row.recordId] = $row }
    Write-Output "=== $run ==="
    Write-Output ("decisions={0} albums={1} share={2:P2} adultAlbum={3:P2} ({4}/{5}) youthAlbum={6:P2} ({7}/{8})" -f
        $strategies.Count, $albums.Count, ($albums.Count / $strategies.Count), ($adultAlbums.Count / $adultStrategies.Count),
        $adultAlbums.Count, $adultStrategies.Count, ($youthAlbums.Count / $youthStrategies.Count), $youthAlbums.Count, $youthStrategies.Count)
    Write-Output ("albumChartAdult={0:P2} ({1}/{2}); singlesChartAdult={3:P2} ({4}/{5}); closed40Median={6}; pearson={7} N={8}" -f
        ($adultAlbumRows.Count / $albumChart.Count), $adultAlbumRows.Count, $albumChart.Count,
        ($adultChartRows.Count / $chartedRows.Count), $adultChartRows.Count, $chartedRows.Count,
        (Get-Median $closedTop40), (Get-Correlation $qualities $lifetimeUnits), $qualities.Count)

    foreach ($format in @('Single', 'Album')) {
        $formatOutcomes = @($outcomes | Where-Object format -eq $format)
        $errors = @()
        foreach ($row in $formatOutcomes) {
            $strategy = $strategyById[$row.recordId]
            if ($null -eq $strategy) { continue }
            $projected = if ($format -eq 'Single') { [double]$strategy.projectedSingleNet } else { [double]$strategy.projectedAlbumNet }
            $errors += $projected - [double]$row.realizedNet
        }
        $live = @($liveRecords | Where-Object { $_.format -eq $format -and $strategyById.ContainsKey($_.recordId) })
        $ceilingErrors = @($errors)
        foreach ($row in $live) {
            $strategy = $strategyById[$row.recordId]
            $projected = if ($format -eq 'Single') { [double]$strategy.projectedSingleNet } else { [double]$strategy.projectedAlbumNet }
            $ceilingErrors += $projected - [double]$row.observedNetLowerBound
        }
        $matched = $errors.Count + $live.Count
        $formatStrategies = @($strategies | Where-Object chosenFormat -eq $format)
        Write-Output ("{0}: retired={1} live={2} unmatched={3} exactError={4:N2} N={5} ceiling={6:N2} N={7}" -f
            $format, $errors.Count, $live.Count, ($formatStrategies.Count - $matched), (Get-Mean $errors), $errors.Count,
            (Get-Mean $ceilingErrors), $ceilingErrors.Count)
        foreach ($group in @(
            @('NewUnsigned', @('NewSigning', 'Unsigned')),
            @('Rising', @('Rising')),
            @('Established', @('Established')),
            @('StarSuperstar', @('Star', 'Superstar'))
        )) {
            $groupErrors = @()
            foreach ($row in $formatOutcomes) {
                $strategy = $strategyById[$row.recordId]
                if ($null -eq $strategy -or $strategy.careerState -notin $group[1]) { continue }
                $projected = if ($format -eq 'Single') { [double]$strategy.projectedSingleNet } else { [double]$strategy.projectedAlbumNet }
                $groupErrors += $projected - [double]$row.realizedNet
            }
            if ($groupErrors.Count) {
                Write-Output ("  {0}: N={1} exactError={2:N2}" -f $group[0], $groupErrors.Count, (Get-Mean $groupErrors))
            }
        }
    }

    $youthAlbumIds = @{}
    foreach ($row in $youthAlbums) { $youthAlbumIds[$row.recordId] = $true }
    $youthCompilations = @($costAssumptions | Where-Object { $_.actualAlbumFormat -eq 'Compilation' -and $youthAlbumIds.ContainsKey($_.recordId) })
    Write-Output "youthAlbumCompilations=$($youthCompilations.Count)/$($youthAlbums.Count)"
}
