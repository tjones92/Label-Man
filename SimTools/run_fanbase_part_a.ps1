param(
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe',
    [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$RunPrefix = 'fanbase-part-a-v1',
    [ValidateRange(27, 522)][int]$Weeks = 522,
    [int[]]$Seeds = @(2002, 1001),
    [string]$Reference = 'SimLogs/writing-calibration/stature-marketing-decade-v2-2002-control-request.json',
    [string]$Python = 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe',
    [ValidateSet('control', 'treatment', 'bundle', 'bundle2', 'volume')][string[]]$Arms = @('control', 'treatment'),
    [ValidateRange(0.0, 0.5)][double]$ColdAdultYear = 0.25,
    [ValidateRange(1.0, 3.0)][double]$RationExponent = 1.35,
    [ValidateRange(0.25, 3.0)][double]$WorkBudget = 1.0
)
# FanbaseEfficacyEvidence.md part A: a same-build paired decade per seed, both arms at once.
#   control   = the stature-marketing decade control's flags
#   treatment = control + singles read current albums only + follower purchases recruit no one (both formats)
#   bundle    = treatment + --follower-cold-adult-year + --singles-ration-exponent (-Arms bundle)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$dir = Join-Path $root 'SimLogs/writing-calibration'
$treatmentFlags = @('--single-artist-heat-current-albums', '--follower-recruit-from-new-buyers')
# PowerShell names are case-insensitive: the parsed request must not reuse the [string] $Reference parameter.
$referenceRequest = Get-Content -Raw -LiteralPath $Reference | ConvertFrom-Json
if (@($referenceRequest.flags).Count -lt 100 -or @($referenceRequest.flags) -notcontains '--follower-demand') { throw 'Reference flags missing.' }
$assembly = Join-Path $root '.godot/mono/temp/bin/Debug/Label Man.dll'
$assemblyHash = (Get-FileHash -LiteralPath $assembly).Hash
$revision = (git rev-parse HEAD).Trim()
. (Join-Path $PSScriptRoot 'AuditGcEnvironment.ps1')
foreach ($seed in $Seeds) {
    $procs = @()
    foreach ($arm in $Arms) {
        $name = "$RunPrefix-$seed-$arm"
        if (Get-ChildItem -LiteralPath (Join-Path $root 'SimLogs') -Filter "$name-*.csv*" | Select-Object -First 1) { throw "Run exists: $name" }
        $flags = @($referenceRequest.flags | ForEach-Object {
            if ($_ -like '--run=*') { "--run=$name" }
            elseif ($_ -like '--seed=*') { "--seed=$seed" }
            elseif ($_ -like '--weeks=*') { "--weeks=$Weeks" }
            else { $_ }
        })
        if ($arm -ne 'control') { $flags += $treatmentFlags }
        # bundle = treatment + cold-act adult loss + steeper singles ration (part A cooling, part B ceiling)
        if ($arm -in @('bundle', 'bundle2', 'volume')) {
            $flags += ('--follower-cold-adult-year=' + $ColdAdultYear.ToString([Globalization.CultureInfo]::InvariantCulture))
            $flags += ('--singles-ration-exponent=' + $RationExponent.ToString([Globalization.CultureInfo]::InvariantCulture))
        }
        # bundle2 = bundle + folk material mix + roster delivery bound and tier ceiling; volume = bundle2 + non-Major work budget
        if ($arm -in @('bundle2', 'volume')) { $flags += @('--folk-material-mix', '--roster-delivery-bound=2', '--roster-tier-ceiling') }
        if ($arm -eq 'volume') { $flags += @(('--slate-work-budget=' + $WorkBudget.ToString([Globalization.CultureInfo]::InvariantCulture)), '--slate-going-concern-reserve') }
        $userData = Join-Path $dir "userdata-$name"
        New-Item -ItemType Directory -Force -Path $userData | Out-Null
        @{ flags=$flags; sourceRevision=$revision; assemblySha256=$assemblyHash; startedUtc=[datetime]::UtcNow.ToString('o') } |
            ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $dir "$name-request.json")
        $env:APPDATA = $userData
        # Godot keeps literal quotes on user arguments, so nothing is quoted; no path or flag here contains a space.
        if (@($flags + $root + $dir | Where-Object { $_ -match '\s' }).Count) { throw 'An argument contains whitespace.' }
        $argList = (@('--headless', '--path', [string]$root, '--log-file', [string](Join-Path $dir "$name-engine.log"),
            'SimTools/ChartAuditRunner.tscn', '--') + @($flags | ForEach-Object { [string]$_ })) -join ' '
        $procs += @{ name=$name; p=(Start-Process -FilePath $Godot -ArgumentList $argList -WorkingDirectory $root -WindowStyle Hidden -PassThru `
            -RedirectStandardOutput (Join-Path $dir "$name.log") -RedirectStandardError (Join-Path $dir "$name-errors.log")) }
        Write-Host "Started $name"
    }
    foreach ($x in $procs) {
        $x.p.WaitForExit()
        if (!(Select-String -LiteralPath (Join-Path $dir "$($x.name).log") -Pattern "^CHART_AUDIT_COMPLETE run=$($x.name) weeks=$Weeks$" -Quiet)) { throw "Run did not complete: $($x.name)" }
        if ((Get-FileHash -LiteralPath $assembly).Hash -ne $assemblyHash) { throw 'Assembly changed during the run.' }
        @(Get-ChildItem -LiteralPath (Join-Path $root 'SimLogs') -Filter "$($x.name)-*.csv") |
            ForEach-Object { @{ name=$_.Name; sha256=(Get-FileHash -LiteralPath $_.FullName).Hash; bytes=$_.Length } } |
            ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $dir "$($x.name)-outputs.json")
        & $Python SimTools/compress_stature_outputs.py --name $x.name
        if ($LASTEXITCODE -ne 0) { throw "Output compression failed: $($x.name)" }
        Write-Host "Completed $($x.name)"
    }
}
Write-Host 'FANBASE_PART_A_COMPLETE'
