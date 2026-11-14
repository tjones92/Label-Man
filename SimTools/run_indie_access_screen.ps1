param(
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe',
    [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$RunPrefix = 'indie-access-v1',
    [ValidateRange(27, 522)][int]$Weeks = 104,
    [int]$Seed = 2002,
    [string]$Reference = 'SimLogs/writing-calibration/major-scale-bundle-request.json',
    [string[]]$Arms = @('ia'),
    [double]$Access = 0.6,
    [double]$Coverage = 1.0,
    [double]$Impact = 0.55,
    [double]$Parity = 1.0,
    [int]$ArrivalDepth = 100,
    [double]$FitBase = 1.0,
    [int]$WholesaleDepth = 40,
    [int]$TermWeeks = 10,
    [double]$SoftBase = 0.6,
    [int]$ConnectedLag = 1,
    [double]$CraftFloor = 0.5,
    [switch]$WholesaleWeekly,
    [string[]]$Extra = @(),
    # CSVs no Major-share scorer reads (~55% of a lean run); pass -SkipTelemetry none to write them all.
    [string[]]$SkipTelemetry = @('stature-shadow', 'writing-demos', 'writing-partnerships', 'artist-fanbase-weekly', 'media-demand', 'single-capital-risk'),
    [switch]$Trace
)
# Indie distributor access probe: the major-scale bundle plus ia = --indie-distributor-access=Access (IndieDistributorAccess.cs).
# All arms in -Arms start at once and are awaited; run two at a time on a low-RAM machine.
$ErrorActionPreference = 'Stop'
# -File passes 'base,gap' as one string; split it and check each arm.
$Arms = @($Arms | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
# An arm is one switch or several joined by '_' (ec_aci_nas = the three together). -Extra adds raw flags to every arm.
foreach ($a in $Arms) { foreach ($part in ($a -split '_')) { if ($part -notin @('base', 'ia', 'nas', 'ec', 'ncp', 'aci', 'dp', 'ad', 'cp', 'cl', 'fx', 'fn', 'cw', 'ut', 'gc', 'of', 'fb', 'ca', 'cf', 'lf', 'ls', 'lc', 'lh', 'lx', 'lfoot', 'ldh', 'ldf', 'x') -and $part -notmatch '^(lw\d+up\d+|ar\d+|ao\d+|cap\d+|lh\d+|he\d+|hc\d+|hy\d+|fd\d+)$') { throw "Unknown arm: $a" } } }
$Extra = @($Extra | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
$SkipTelemetry = @($SkipTelemetry | ForEach-Object { $_ -split ',' } | Where-Object { $_ -and $_ -ne 'none' })
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$dir = Join-Path $root 'SimLogs/writing-calibration'
$referenceRequest = Get-Content -Raw -LiteralPath $Reference | ConvertFrom-Json
if (@($referenceRequest.flags).Count -lt 100) { throw "Reference flags missing." }
$assembly = Join-Path $root '.godot/mono/temp/bin/Debug/Label Man.dll'
$assemblyHash = (Get-FileHash -LiteralPath $assembly).Hash
$revision = (git rev-parse HEAD).Trim()
. (Join-Path $PSScriptRoot 'AuditGcEnvironment.ps1')
$procs = @()
foreach ($arm in $Arms) {
    $name = "$RunPrefix-$Seed-$arm"
    if (Get-ChildItem -LiteralPath (Join-Path $root 'SimLogs') -Filter "$name-*.csv*" | Select-Object -First 1) { throw "Run exists: $name" }
    $flags = @($referenceRequest.flags | ForEach-Object {
        if ($_ -like '--run=*') { "--run=$name" } elseif ($_ -like '--seed=*') { "--seed=$Seed" } elseif ($_ -like '--weeks=*') { "--weeks=$Weeks" } else { $_ }
    })
    $parts = @($arm -split '_')
    if ($parts -contains 'ia') { $flags += "--indie-distributor-access=$Access" }
    if ($parts -contains 'nas') { $flags += '--follower-demand-no-advance-stock' }
    if ($parts -contains 'ec') { $flags += "--earned-indie-coverage=$Coverage" }
    # ncp: drop the never-charted single momentum penalty (audit B2), keep album-scale milestones (B1).
    if ($parts -contains 'aci') { $flags += "--act-earned-campaign-impact=$Impact" }
    if ($parts -contains 'ncp') { $flags += '--album-scale-career-milestones-part=b1' }
    # dp/ad/cp: LabelFootprintParity.cs -- flat label demand scale, Hot 100 arrival in held regions, Major craft parity.
    if ($parts -contains 'dp') { $flags += "--label-demand-parity=$Parity" }
    if ($parts -contains 'ad') { $flags += "--regional-arrival-chart-depth=$ArrivalDepth" }
    if ($parts -contains 'cp') { $flags += '--major-craft-parity' }
    if ($parts -contains 'cl') { $flags += '--covered-launch-parity=1' }
    if ($parts -contains 'fn') { $flags += "--regional-fit-exposure-national-rank=$ArrivalDepth" }
    # fx: every region is fit-exposed at launch for every label (RegionalFitExposure launch chance 0.3 + 0.7 x reach -> 1).
    if ($parts -contains 'fx') { $flags += "--regional-fit-exposure-launch-base=$FitBase" }
    # cw: a record on the national chart at 1..WholesaleDepth is wholesale-house proof in every market (ChartProvenWholesale.cs).
    if ($parts -contains 'cw') { $flags += "--chart-proven-wholesale=$WholesaleDepth"; if ($WholesaleWeekly) { $flags += '--chart-proven-wholesale-weekly' } }
    # ut: an AI label's sales in markets it does not ship to pay on one-stop terms (UncoveredSalesTerms.cs).
    if ($parts -contains 'ut') { $flags += "--uncovered-sales-terms=$TermWeeks" }
    # gc / of: ProgrammeFundingRelief.cs -- going-concern release reserve for every tier; hot-act releases draw the overdraft.
    if ($parts -contains 'gc') { $flags += '--slate-going-concern-reserve-all-tiers' }
    if ($parts -contains 'of') { $flags += '--programme-overdraft-funding' }
    # fb / ca / cf: FootprintCraftSoftening.cs -- softer fit-exposure launch base, connected singles reach fit-exposed
    # held regions on a distributor's lag, and the label craft term lifted toward the Major reference.
    if ($parts -contains 'fb') { $flags += "--regional-fit-exposure-launch-base=$SoftBase" }
    if ($parts -contains 'ca') { $flags += "--connected-arrival=$ConnectedLag" }
    if ($parts -contains 'cf') { $flags += "--label-craft-floor=$CraftFloor" }
    # lf / ls / lc / lh / lx / lfoot: LabelBlindLaunch.cs foot parts -- launch factor, launch stock, city reach,
    # arrival hold, fit exposure; lfoot = all five.
    $blind = @(@{a='lf';p='factor'}, @{a='ls';p='stock'}, @{a='lc';p='city'}, @{a='lh';p='hold'}, @{a='lx';p='expose'}, @{a='lfoot';p='foot'}) |
        Where-Object { $parts -contains $_.a } | ForEach-Object { $_.p }
    if (@($blind).Count) { $flags += "--label-blind-launch=$(@($blind) -join ',')" }
    # ldh / ldf: --label-blind-launch-dose 0.5 / 1 -- a keyed share of a non-Major single's regions opened to the reference
    # footprint (launch factor, city reach, arrival hold, fit exposure together; lift only; stock and Majors untouched).
    if ($parts -contains 'ldh') { $flags += '--label-blind-launch-dose=0.5' }
    if ($parts -contains 'ldf') { $flags += '--label-blind-launch-dose=1' }
    # lwAupB: SingleCommercialFit launch weight A and upside B; a leading 0 reads as a decimal point (lw025up07 = 0.25 / 0.7, lw0up1 = 0 / 1).
    foreach ($part in $parts) { if ($part -match '^lw(\d+)up(\d+)$') {
        $fitValue = { param($d) if ($d.Length -gt 1 -and $d[0] -eq '0') { [double]('0.' + $d.Substring(1)) } else { [double]$d } }
        $flags += "--single-commercial-fit-launch-weight=$(& $fitValue $Matches[1])"
        $flags += "--single-commercial-fit-upside=$(& $fitValue $Matches[2])" } }
    # arN: --single-commercial-fit-act-record-weight (the act's last Top 40 moves the kink); capN: --single-commercial-fit-upside-cap.
    foreach ($part in $parts) {
        $fitValue = { param($d) if ($d.Length -gt 1 -and $d[0] -eq '0') { [double]('0.' + $d.Substring(1)) } else { [double]$d } }
        if ($part -match '^ar(\d+)$') { $flags += "--single-commercial-fit-act-record-weight=$(& $fitValue $Matches[1])" }
        # aoN: act-record weight N with --single-commercial-fit-act-record-odds-only (raises connect odds, not hit size).
        if ($part -match '^ao(\d+)$') { $flags += "--single-commercial-fit-act-record-weight=$(& $fitValue $Matches[1])"; $flags += '--single-commercial-fit-act-record-odds-only' }
        # lhN: --label-blind-launch-dose=N for hit acts' singles only (--label-blind-launch-dose-hot-acts; Top 40 in the last 26 weeks).
        if ($part -match '^lh(\d+)$') { $flags += "--label-blind-launch-dose=$(& $fitValue $Matches[1])"; $flags += '--label-blind-launch-dose-hot-acts' }
        # heN: --label-blind-launch-dose-exposure=N, the fit-exposure chance an opened region reads at least (with lhN; he095 = 0.95).
        if ($part -match '^he(\d+)$') { $flags += "--label-blind-launch-dose-exposure=$(& $fitValue $Matches[1])" }
        # hcN: --hit-act-craft-floor=N, a hot act's non-Major single reads label craft N of the way to the Major reference.
        if ($part -match '^hc(\d+)$') { $flags += "--hit-act-craft-floor=$(& $fitValue $Matches[1])" }
        # hyN: --hit-act-hype=N, launch awareness + N x the act's hit recency (HitActHype.cs; hy03 = 0.3).
        # fdN: --hit-act-due-weeks-field=N, a non-Major label's hot act is due its follow-up after N weeks (HitActCadence.cs; Majors keep 13).
        if ($part -match '^fd(\d+)$') { $flags += "--hit-act-due-weeks-field=$($Matches[1])" }
        if ($part -match '^hy(\d+)$') { $flags += "--hit-act-hype=$(& $fitValue $Matches[1])" }
        if ($part -match '^cap(\d+)$') { $flags += "--single-commercial-fit-upside-cap=$(& $fitValue $Matches[1])" } }
    $flags += $Extra
    # --skip-telemetry (ChartAuditRunner.CreateWriter): telemetry only, one flag per file so -Extra's comma split cannot cut the list.
    foreach ($t in $SkipTelemetry) { $flags += "--skip-telemetry=$t" }
    if ($Trace) { $flags += '--launch-stock-trace' }
    $userData = Join-Path $dir "userdata-$name"
    New-Item -ItemType Directory -Force -Path $userData | Out-Null
    @{ flags=$flags; sourceRevision=$revision; assemblySha256=$assemblyHash; startedUtc=[datetime]::UtcNow.ToString('o') } |
        ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $dir "$name-request.json")
    $env:APPDATA = $userData
    if (@($flags + $root + $dir | Where-Object { $_ -match '\s' }).Count) { throw 'An argument contains whitespace.' }
    $argList = (@('--headless', '--path', [string]$root, '--log-file', [string](Join-Path $dir "$name-engine.log"),
        'SimTools/ChartAuditRunner.tscn', '--') + @($flags | ForEach-Object { [string]$_ })) -join ' '
    $procs += @{ name=$name; p=(Start-Process -FilePath $Godot -ArgumentList $argList -WorkingDirectory $root -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput (Join-Path $dir "$name.log") -RedirectStandardError (Join-Path $dir "$name-errors.log")) }
    Write-Host "Started $name"
}
foreach ($x in $procs) { $x.p.WaitForExit(); Write-Host "$($x.name) exit=$($x.p.ExitCode)" }
