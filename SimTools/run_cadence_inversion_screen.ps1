param(
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe',
    [ValidatePattern('^[a-zA-Z0-9-]+$')][string]$RunPrefix = 'cadence-inv-v1',
    [ValidateRange(27, 522)][int]$Weeks = 265,
    [int]$Seed = 2002,
    [string]$Reference = 'SimLogs/writing-calibration/fanbase-part-a-v3-2002-volume-request.json',
    [string[]]$Arms = @('base', 'gap'),
    [ValidateRange(7, 52)][int]$ColdGap = 17,
    [ValidateRange(1, 16)][int]$MajorSingles = 16
)
# Cadence-inversion screen (major-share-is-career-mix-adult60-is-volume): the full-bundle flags plus
#   gap  = --slate-cold-act-gap (acts without a Top 40 in 26 weeks wait ColdGap weeks between singles)
#   maj  = --programme-major-singles (Majors' monthly single capacity, default 10)
#   both = gap + maj
# All arms in -Arms start at once and are awaited; run two at a time on a low-RAM machine.
$ErrorActionPreference = 'Stop'
# -File passes 'base,gap' as one string; split it and check each arm.
$Arms = @($Arms | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
foreach ($a in $Arms) { if ($a -notin @('base', 'gap', 'maj', 'both')) { throw "Unknown arm: $a" } }
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
    if ($arm -in @('gap', 'both')) { $flags += "--slate-cold-act-gap=$ColdGap" }
    if ($arm -in @('maj', 'both')) { $flags += "--programme-major-singles=$MajorSingles" }
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
