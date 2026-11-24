param(
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe',
    [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$RunPrefix = 'lpe-v1',
    [ValidateRange(27, 522)][int]$Weeks = 208,
    [int]$Seed = 2002,
    [string]$Reference = 'SimLogs/writing-calibration/rg10-v1-2002-lw0up1_ao2_lh1_he095-request.json',
    [string[]]$Arms = @('le', 'lep'),
    [double]$LpScale = 1.6,
    [string[]]$Extra = @()
)
# LP earned-demand screen (GameyBehaviorAudit.md finding 1 and 9) on the rg10 Top 10 candidate.
# The reference is the rg10 RUN request (it carries the lw0up1/ao2/lh1/he095 arm flags the reference request lacks).
# off = reference as is (inertness, and the control for contract-expiry telemetry), le = --lp-earned-discovery=0
# --lp-earned-launch, lep = le + --lp-label-demand-parity, an = --anniversary-opening-contracts,
# rc = --receivable-overdraft-charge=0.0125 (the adopted 1.25% a month against rg10's 1.5%).
# re = --renewal-live-evidence (count live records at renewal; AI deals mature on the date only).
# eo = --renewal-earned-option (renew on the deal's realized net plus a loyalty allowance, not a loyalty roll).
# lw = le + --lp-buyer-word-of-mouth=10000 (LP word of mouth from buyers, not appeal).
# bn = --budget-noncurrent-weeks=156 (budget editions only of retired masters, 3+ years old; audit 2.8A).
# bns = bn + --budget-appeal-saturation=25000 (secondary titles weigh like former hits on the budget shelf).
# lwb = lw + bn + --lp-demand-scale=$LpScale (earned LP demand, volume restored by a global LP purchase scale,
# budget editions from noncurrent catalogue). lwbs = lwb + --budget-appeal-saturation=25000.
# lwbr = lwbs + --lp-demand-scale-stores-only + --budget-rank-by-weight (2.9b next arm).
# LP channel handoff arms (2026-11-15), all on lwbr and all with --album-chart-history (telemetry only):
# lref = lwbr as is. fa = lwbr + --album-affinity=Folk=0.44,BossaNova=0.40 (Folk demand per title cut).
# fm = fa + TraditionalPop=0.46,Classical=0.60,Comedy=0.50 (the cut moved to the genres history had).
# rk = lwbr + --rack-store-standing-bound=2 (issue 2). bb = lwbr + --budget-source-bound=1 (issue 3a).
# fm2 = fa + TraditionalPop=0.41,EasyListening=0.28,Classical=0.60 (run with -LpScale 1.8 to restore store volume).
# bk = lwbr with --budget-appeal-saturation=100000 (issue 3b). -Extra adds flags to every arm.
# kn = the reference (run against candidate-request.json, which carries lwbr + --budget-source-bound=1 since 2026-11-15)
# with --lp-demand-scale-known, and the reference's --lp-demand-scale replaced by -LpScale (audit 2.12).
# sc = the reference with its --lp-demand-scale replaced by -LpScale only (audit 2.15).
# Run eo against -Reference SimLogs/writing-calibration/candidate-request.json with -RunPrefix cand-v1.
# All arms in -Arms start at once and are awaited; run two at a time on a low-RAM machine.
$ErrorActionPreference = 'Stop'
# -File passes 'base,gap' as one string; split it and check each arm.
$Arms = @($Arms | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
foreach ($a in $Arms) { if ($a -notin @('off', 'le', 'lep', 'an', 'rc', 're', 'eo', 'lw', 'bn', 'bns', 'lwb', 'lwbs', 'lwbr', 'lref', 'fa', 'fm', 'rk', 'bb', 'bk', 'all', 'fm2', 'kn', 'sc')) { throw "Unknown arm: $a" } }
$Extra = @($Extra | ForEach-Object { $_ -split ',(?=--)' } | Where-Object { $_ })
$handoff = @('lref', 'fa', 'fm', 'fm2', 'rk', 'bb', 'bk', 'all')
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
        if ($_ -like '--run=*') { "--run=$name" } elseif ($_ -like '--seed=*') { "--seed=$Seed" } elseif ($_ -like '--weeks=*') { "--weeks=$Weeks" }
        elseif ($arm -in @('kn', 'sc') -and $_ -like '--lp-demand-scale=*') { "--lp-demand-scale=$($LpScale.ToString([Globalization.CultureInfo]::InvariantCulture))" } else { $_ }
    })
    if ($arm -eq 'kn') {
        if (-not ($flags -contains '--lp-demand-scale-stores-only')) { throw 'kn needs a reference with the stores-only LP scale.' }
        $flags += '--lp-demand-scale-known'
    }
    if ($arm -in @('le', 'lep', 'lw', 'lwb', 'lwbs', 'lwbr') + $handoff) { $flags += @('--lp-earned-discovery=0', '--lp-earned-launch') }
    if ($arm -in @('lw', 'lwb', 'lwbs', 'lwbr') + $handoff) { $flags += '--lp-buyer-word-of-mouth=10000' }
    if ($arm -eq 'lep') { $flags += '--lp-label-demand-parity' }
    if ($arm -eq 'an') { $flags += '--anniversary-opening-contracts' }
    if ($arm -eq 'rc') { $flags += '--receivable-overdraft-charge=0.0125' }
    if ($arm -eq 're') { $flags += '--renewal-live-evidence' }
    if ($arm -eq 'eo') { $flags += '--renewal-earned-option' }
    if ($arm -in @('bn', 'bns', 'lwb', 'lwbs', 'lwbr') + $handoff) { $flags += '--budget-noncurrent-weeks=156' }
    if ($arm -in @('lwb', 'lwbs', 'lwbr') + $handoff) { $flags += "--lp-demand-scale=$($LpScale.ToString([Globalization.CultureInfo]::InvariantCulture))" }
    if ($arm -in @('bns', 'lwbs', 'lwbr', 'lref', 'fa', 'fm', 'fm2', 'rk', 'bb', 'all')) { $flags += '--budget-appeal-saturation=25000' }
    if ($arm -eq 'bk') { $flags += '--budget-appeal-saturation=100000' }
    if ($arm -in @('lwbr') + $handoff) { $flags += @('--lp-demand-scale-stores-only', '--budget-rank-by-weight') }
    if ($arm -in $handoff) { $flags += '--album-chart-history' }
    if ($arm -eq 'fa') { $flags += '--album-affinity=Folk=0.44,BossaNova=0.40' }
    if ($arm -eq 'fm') { $flags += '--album-affinity=Folk=0.44,BossaNova=0.40,TraditionalPop=0.46,Classical=0.60,Comedy=0.50' }
    if ($arm -in @('fm2', 'all')) { $flags += '--album-affinity=Folk=0.44,BossaNova=0.40,TraditionalPop=0.41,EasyListening=0.28,Classical=0.60' }
    if ($arm -in @('rk', 'all')) { $flags += '--rack-store-standing-bound=2' }
    if ($arm -in @('bb', 'all')) { $flags += '--budget-source-bound=1' }
    $flags += $Extra
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
