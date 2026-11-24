param(
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe',
    [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$RunPrefix = 'stature-marketing-trial',
    [ValidateRange(27, 522)][int]$Weeks = 52,
    [int[]]$Seeds = @(1001, 2002),
    [string]$Calibration = 'SimTools/StatureShadowExperimental.json',
    [string]$ControlCalibration,
    [ValidateSet('control', 'treatment')][string]$CalibrationArm,
    [string]$Python = 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe',
    [switch]$CompressOutputs,
    [switch]$CausalOnly,
    [switch]$ProfilePerformance,
    [switch]$TreatmentOnly,
    [ValidateRange(0.0, 1.0)][double]$MarketingBlend = 1.0,
    [switch]$LegacyCampaignShare,
    [switch]$ObserveMediaDemandBookings,
    [string]$TvAllocationOverride,
    [switch]$OpeningOrigin,
    [switch]$OpeningGeography
)
$ErrorActionPreference = 'Stop'
if ($OpeningGeography) { $OpeningOrigin = $true }
if ($OpeningOrigin -and !$CausalOnly) { throw 'Opening provenance requires -CausalOnly.' }
if ($ProfilePerformance -and !$CausalOnly) { throw 'Performance profiling requires -CausalOnly; timing CSVs are nondeterministic and excluded from neutrality screens.' }
if ($TreatmentOnly -and !$CausalOnly) { throw '-TreatmentOnly requires -CausalOnly and completed matching controls.' }
if ($ControlCalibration -and (!$CausalOnly -or $TreatmentOnly)) { throw '-ControlCalibration requires a full causal pair.' }
if ($CalibrationArm -and !$ControlCalibration) { throw '-CalibrationArm requires -ControlCalibration.' }
if ($LegacyCampaignShare -and !$ControlCalibration) { throw '-LegacyCampaignShare requires a paired calibration comparison.' }
if ($TvAllocationOverride -and (!$CalibrationArm -or !$ObserveMediaDemandBookings)) { throw '-TvAllocationOverride requires an explicit calibration arm and booking observation.' }
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$dir = Join-Path $root 'SimLogs/writing-calibration'
$arms = if ($CalibrationArm) { @($CalibrationArm) } elseif ($TreatmentOnly) { @('treatment') } elseif ($CausalOnly) { @('control', 'treatment') } else { @('disabled', 'control', 'treatment') }
$sourcePath = Join-Path $dir 'veteran-follower-on-market-2002-request.json'
$source = Get-Content -Raw -LiteralPath $sourcePath | ConvertFrom-Json
if (@($source.flags | Where-Object { $_ -like '*stature*' }).Count) { throw 'Reference contains stature flags.' }
$assembly = Join-Path $root '.godot/mono/temp/bin/Debug/Label Man.dll'
$calibrationPath = (Resolve-Path -LiteralPath $Calibration).Path.Replace('\', '/')
$frozen = @($assembly, $Godot, $calibrationPath, $sourcePath, (Join-Path $root 'Systems/SimulationArguments.cs'))
if ($ControlCalibration) {
    $controlCalibrationPath = (Resolve-Path -LiteralPath $ControlCalibration).Path.Replace('\', '/')
    & $Python -c 'import json,sys;sys.path.insert(0,"SimTools");from verify_national_stature_policy import candidate_is_scoped;candidate_is_scoped(*(json.load(open(p, encoding="utf-8-sig")) for p in sys.argv[1:]))' $controlCalibrationPath $calibrationPath
    if ($LASTEXITCODE -ne 0) { throw 'Invalid national high-tier calibration pair.' }
    $frozen += $controlCalibrationPath
}
$hashes = @{}; foreach ($path in $frozen) { $hashes[$path] = (Get-FileHash -LiteralPath $path).Hash }
$revision = (git rev-parse HEAD).Trim()
$dirty = @(git status --porcelain --untracked-files=no).Count -ne 0
# Freeze content hashes too: the existing workspace intentionally includes uncommitted implementation.
$sourceHashes = @{}
foreach ($folder in @('Data', 'Systems', 'SimTools', 'UI')) {
    Get-ChildItem -LiteralPath (Join-Path $root $folder) -Filter '*.cs' -Recurse |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
        ForEach-Object { $sourceHashes[$_.FullName.Substring($root.Length + 1)] = (Get-FileHash -LiteralPath $_.FullName).Hash }
}
$argumentSource = Get-Content -Raw -LiteralPath (Join-Path $root 'Systems/SimulationArguments.cs')
$match = [regex]::Match($argumentSource, '(?s)private static readonly string\[\] PreviousProfile = \{(.*?)\};')
if (!$match.Success) { throw 'Cannot resolve previous profile.' }
$profile = @([regex]::Matches($match.Groups[1].Value, '"(--[^"\r\n]+)"') | ForEach-Object { $_.Groups[1].Value })
if ($profile.Count -lt 50 -or @($source.flags | Where-Object { $_ -eq '--lp-retail-v2-profile' }).Count -ne 1) { throw 'Unexpected reference profile.' }
foreach ($seed in $Seeds) {
    if ($TreatmentOnly) {
        $controlName = "$RunPrefix-$seed-control"
        if (!(Select-String -LiteralPath (Join-Path $dir "$controlName.log") -Pattern "^CHART_AUDIT_COMPLETE run=$controlName weeks=$Weeks$" -Quiet) -or
            (Get-Item -LiteralPath (Join-Path $dir "$controlName-errors.log")).Length -or
            !(Test-Path -LiteralPath (Join-Path $dir "$controlName-outputs.json"))) { throw "Control is not complete: $controlName" }
        $controlRequest = Get-Content -Raw -LiteralPath (Join-Path $dir "$controlName-request.json") | ConvertFrom-Json -AsHashtable
        if ($controlRequest.assemblySha256 -ne $hashes[$assembly] -or $controlRequest.engineSha256 -ne $hashes[$Godot] -or
            $controlRequest.calibrationSha256 -ne $hashes[$calibrationPath] -or $controlRequest.sourceRevision -ne $revision -or
            $controlRequest.sourceContentSha256.Count -ne $sourceHashes.Count) { throw "Control provenance differs: $controlName" }
        foreach ($sourceFile in $sourceHashes.Keys) {
            if ($controlRequest.sourceContentSha256[$sourceFile] -ne $sourceHashes[$sourceFile]) { throw "Control source differs: $sourceFile" }
        }
    }
    foreach ($arm in $arms) {
        $name = "$RunPrefix-$seed-$arm"
        if ((Test-Path -LiteralPath (Join-Path $dir "$name-request.json")) -or
            (Get-ChildItem -LiteralPath (Join-Path $root 'SimLogs') -Filter "$name-*.csv*" | Select-Object -First 1)) { throw "Run exists: $name" }
    }
}
$priorAppData = $env:APPDATA; $priorGc = $env:DOTNET_gcServer; $priorHeapCount = $env:DOTNET_GCHeapCount
try {
    . (Join-Path $PSScriptRoot 'AuditGcEnvironment.ps1')
    foreach ($seed in $Seeds) {
        foreach ($arm in $arms) {
            $name = "$RunPrefix-$seed-$arm"
            $flags = @($source.flags | ForEach-Object {
                if ($_ -like '--run=*') { "--run=$name" }
                elseif ($_ -like '--seed=*') { "--seed=$seed" }
                elseif ($_ -like '--weeks=*') { "--weeks=$Weeks" }
                else { $_ }
            }) + @('--label-decision-audit')
            if ($OpeningOrigin) { $flags += '--observe-opening-following-origin' }
            if ($OpeningGeography) { $flags += '--observe-opening-geography' }
            if ($ProfilePerformance) { $flags += '--profile-performance' }
            $armCalibrationPath = if ($arm -eq 'control' -and $ControlCalibration) { $controlCalibrationPath } else { $calibrationPath }
            if ($arm -ne 'disabled') { $flags += @("--stature-calibration=$armCalibrationPath", '--persist-stature-shadow', '--observe-stature-marketing') }
            if ($arm -eq 'treatment' -or $ControlCalibration) { $flags += @('--stature-marketing-reader', ('--stature-marketing-blend=' + $MarketingBlend.ToString([Globalization.CultureInfo]::InvariantCulture))) }
            if ($LegacyCampaignShare) { $flags += '--stature-marketing-legacy-campaign-share' }
            if ($ObserveMediaDemandBookings) { $flags += '--observe-media-demand-bookings' }
            if ($TvAllocationOverride) { $flags += "--audit-tv-allocation-override=$TvAllocationOverride" }
            $keys = @{}; foreach ($flag in $flags) { $keys[($flag -split '=', 2)[0]] = $true }
            $effective = @($profile | Where-Object { !$keys.ContainsKey(($_ -split '=', 2)[0]) }) + $flags
            if ($TreatmentOnly) {
                $pairedControl = Get-Content -Raw -LiteralPath (Join-Path $dir "$RunPrefix-$seed-control-request.json") | ConvertFrom-Json
                $economicFilter = { $_ -ne '--profile-performance' -and $_ -notmatch '^--(run=|stature-|observe-stature-|persist-stature-)' }
                if ([string]::Join("`n", @($effective | Where-Object $economicFilter)) -cne
                    [string]::Join("`n", @($pairedControl.effectiveArguments | Where-Object $economicFilter))) { throw 'Control economic configuration differs.' }
            }
            $env:APPDATA = Join-Path $dir "userdata-$name"
            New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
            foreach ($path in $frozen) { if ((Get-FileHash -LiteralPath $path).Hash -ne $hashes[$path]) { throw "Frozen input changed: $path" } }
            @{ flags=$flags; effectiveArguments=$effective; profile='lp-retail-v2'; sourceRevision=$revision;
               trackedSourceDirty=$dirty; sourceContentSha256=$sourceHashes; assemblySha256=$hashes[$assembly];
               engineSha256=$hashes[$Godot]; calibrationSha256=$hashes[$armCalibrationPath];
               referenceRequestSha256=$hashes[$sourcePath]; validationMode=$(if ($ControlCalibration) { 'calibration-pair' } elseif ($CausalOnly) { 'causal-pair' } else { 'neutrality-and-causal' });
               gcEnvironment=@{ DOTNET_gcServer=$env:DOTNET_gcServer; DOTNET_GCHeapCount=$env:DOTNET_GCHeapCount; LABELMAN_WORKSTATION_GC=$env:LABELMAN_WORKSTATION_GC };
               startedUtc=[datetime]::UtcNow.ToString('o') } |
                ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $dir "$name-request.json")
            & $Godot --headless --path $root --log-file (Join-Path $dir "$name-engine.log") SimTools/ChartAuditRunner.tscn -- @flags > (Join-Path $dir "$name.log") 2> (Join-Path $dir "$name-errors.log")
            if ($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath (Join-Path $dir "$name.log") -Pattern "^CHART_AUDIT_COMPLETE run=$name weeks=$Weeks$" -Quiet)) { throw "Run did not complete: $name" }
            if ((Get-Item -LiteralPath (Join-Path $dir "$name-errors.log")).Length) { throw "Run produced errors: $name" }
            foreach ($path in $frozen) { if ((Get-FileHash -LiteralPath $path).Hash -ne $hashes[$path]) { throw "Frozen input changed: $path" } }
            $files = @(Get-ChildItem -LiteralPath (Join-Path $root 'SimLogs') -Filter "$name-*.csv")
            if (!$files.Count) { throw "Missing outputs: $name" }
            $files | ForEach-Object { @{ name=$_.Name; sha256=(Get-FileHash -LiteralPath $_.FullName).Hash; bytes=$_.Length } } |
                ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $dir "$name-outputs.json")
            Write-Host "Completed $name"
            if ($CompressOutputs) {
                & $Python SimTools/compress_stature_outputs.py --name $name
                if ($LASTEXITCODE -ne 0) { throw "Output compression failed: $name" }
            }
        }
        if ($CalibrationArm) { continue }
        if ($CausalOnly) {
            $summaryMode = if ($ControlCalibration) { '--calibration-pair' } else { '--causal-only' }
            & $Python SimTools/summarize_stature_marketing_trial.py --prefix $RunPrefix --seed $seed $summaryMode
            if ($LASTEXITCODE -ne 0) { throw 'Causal-pair summary failed.' }
            Write-Host "Completed and summarized causal pair for seed $seed (no new neutrality claim)"
            continue
        }
        if ($CompressOutputs) {
            & $Python SimTools/summarize_stature_marketing_trial.py --prefix $RunPrefix --seed $seed --write-neutrality
            if ($LASTEXITCODE -ne 0) { throw 'Neutrality check failed.' }
            & $Python SimTools/summarize_stature_marketing_trial.py --prefix $RunPrefix --seed $seed
            if ($LASTEXITCODE -ne 0) { throw 'Marketing summary failed.' }
            Write-Host "Compared archived CSVs and summarized seed $seed"
            continue
        }
        $disabled = "$RunPrefix-$seed-disabled"; $control = "$RunPrefix-$seed-control"
        $files = @(Get-ChildItem -LiteralPath (Join-Path $root 'SimLogs') -Filter "$disabled-*.csv")
        foreach ($file in $files) {
            $suffix = $file.Name.Substring($disabled.Length)
            $other = Join-Path $root "SimLogs/$control$suffix"
            if (!(Test-Path -LiteralPath $other) -or (Get-FileHash -LiteralPath $file.FullName).Hash -ne (Get-FileHash -LiteralPath $other).Hash) { throw "Neutrality failed: $seed $suffix" }
        }
        $extras = @(Get-ChildItem -LiteralPath (Join-Path $root 'SimLogs') -Filter "$control-*.csv" | Where-Object {
            $_.Name.Substring($control.Length) -notin @('-stature-shadow.csv', '-stature-decisions.csv', '-stature-marketing.csv') -and
            !(Test-Path -LiteralPath (Join-Path $root ('SimLogs/' + $disabled + $_.Name.Substring($control.Length))))
        })
        if ($extras.Count) { throw 'Unexpected control-only output.' }
        foreach ($arm in @('control', 'treatment')) {
            if (!(Test-Path -LiteralPath (Join-Path $root "SimLogs/$RunPrefix-$seed-$arm-stature-marketing.csv"))) { throw 'Missing marketing trace.' }
        }
        @{ seed=$seed; commonCsvCount=$files.Count; byteIdentical=$true; weeks=$Weeks } |
            ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dir "$RunPrefix-$seed-neutrality.json")
        & $Python SimTools/summarize_stature_marketing_trial.py --prefix $RunPrefix --seed $seed
        if ($LASTEXITCODE -ne 0) { throw 'Marketing summary failed.' }
        Write-Host "Matched $($files.Count) common CSV outputs and summarized seed $seed"
    }
} finally {
    $env:APPDATA = $priorAppData; $env:DOTNET_gcServer = $priorGc; $env:DOTNET_GCHeapCount = $priorHeapCount
}
