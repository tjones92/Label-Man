param(
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe',
    [Parameter(Mandatory)][string]$Reference,
    [Parameter(Mandatory)][ValidatePattern('^[a-zA-Z0-9-]+$')][string]$Name,
    [Parameter(Mandatory)][int]$Seed,
    [ValidateRange(27, 522)][int]$Weeks = 104,
    [string[]]$Extra = @(),
    # Plain .csv telemetry instead of the default --gzip-telemetry (.csv.gz, about a tenth of the size).
    [switch]$PlainTelemetry
)
# One seed of a screen arm: the reference request's flags with --run/--seed/--weeks replaced and -Extra appended.
# Launch one background task per seed (short-gate-runs-go-parallel). Writes NAME-request.json, NAME.log, NAME-engine.log.
$ErrorActionPreference = 'Stop'
$Extra = @($Extra | ForEach-Object { $_ -split ',(?=--)' } | Where-Object { $_ })
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root
$dir = Join-Path $root 'SimLogs/writing-calibration'
$ref = Get-Content -Raw -LiteralPath $Reference | ConvertFrom-Json
if (@($ref.flags).Count -lt 100) { throw 'Reference flags missing.' }
if (Get-ChildItem -LiteralPath (Join-Path $root 'SimLogs') -Filter "$Name-*.csv*" | Select-Object -First 1) { throw "Run exists: $Name" }
$flags = @($ref.flags | ForEach-Object {
    if ($_ -like '--run=*') { "--run=$Name" } elseif ($_ -like '--seed=*') { "--seed=$Seed" } elseif ($_ -like '--weeks=*') { "--weeks=$Weeks" } else { $_ }
}) + $Extra + $(if ($PlainTelemetry -or $Extra -contains "--gzip-telemetry") { @() } else { @("--gzip-telemetry") })
$assembly = Join-Path $root '.godot/mono/temp/bin/Debug/Label Man.dll'
@{ flags=$flags; reference=$Reference; sourceRevision=(git rev-parse HEAD).Trim(); assemblySha256=(Get-FileHash -LiteralPath $assembly).Hash;
   startedUtc=[datetime]::UtcNow.ToString('o') } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $dir "$Name-request.json")
. (Join-Path $PSScriptRoot 'AuditGcEnvironment.ps1')
$userData = Join-Path $dir "userdata-$Name"
New-Item -ItemType Directory -Force -Path $userData | Out-Null
$env:APPDATA = $userData
if (@($flags + $root + $dir | Where-Object { $_ -match '\s' }).Count) { throw 'An argument contains whitespace.' }
$argList = (@('--headless', '--path', [string]$root, '--log-file', [string](Join-Path $dir "$Name-engine.log"),
    'SimTools/ChartAuditRunner.tscn', '--') + @($flags | ForEach-Object { [string]$_ })) -join ' '
$p = Start-Process -FilePath $Godot -ArgumentList $argList -WorkingDirectory $root -WindowStyle Hidden -PassThru `
    -RedirectStandardOutput (Join-Path $dir "$Name.log") -RedirectStandardError (Join-Path $dir "$Name-errors.log")
Write-Host "Started $Name"
$p.WaitForExit()
Write-Host "$Name exit=$($p.ExitCode)"
exit $p.ExitCode
