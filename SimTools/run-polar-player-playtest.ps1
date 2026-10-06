param(
    [string]$Godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe',
    [int]$Seed = 1001,
    [string]$Run = "polar-player-$Seed-$(Get-Date -Format 'yyyyMMdd-HHmmss')",
    [switch]$Headless,
    [switch]$KeepOpen,
    [switch]$RefusalUiFixture,
    [string]$OpenSave
)
$ErrorActionPreference = 'Stop'
$workspacePlaytest = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location -LiteralPath $workspacePlaytest
try {
    if ($Headless -and ($KeepOpen -or $OpenSave)) { throw 'An interactive save needs a visible game window.' }
    if ($RefusalUiFixture -and $OpenSave) { throw 'The refusal fixture creates a fresh test label; omit OpenSave.' }
    if ($Run -notmatch '^[a-zA-Z0-9-]+$') { throw 'Run names may contain only letters, numbers and hyphens.' }
    $logPlaytest = Join-Path 'SimLogs' "$Run.log"
    if (Test-Path -LiteralPath $logPlaytest) { throw "Artifact already exists: $logPlaytest" }
    dotnet build 'Label Man.csproj' --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    $argsPlaytest = @('--path', '.', 'SimTools/PolarPlayerPlaytestRunner.tscn', '--', "--seed=$Seed", "--run=$Run",
        '--use-polar-fit-selection', '--enable-genre-market-v2', '--enable-artist-population-lifecycle')
    if ($Headless) { $argsPlaytest = @('--headless') + $argsPlaytest }
    else { $argsPlaytest = @('--windowed', '--resolution', '1600x900') + $argsPlaytest; $argsPlaytest += '--screenshots' }
    if ($KeepOpen) { $argsPlaytest += '--keep-open' }
    if ($RefusalUiFixture) { $argsPlaytest += '--refusal-ui-fixture' }
    if ($OpenSave) { $argsPlaytest += "--open-save=$OpenSave" }
    [ordered]@{ run=$Run; seed=$Seed; arguments=$argsPlaytest;
        tableSha256=(Get-FileHash -LiteralPath 'Data/PolarSongTable.json').Hash;
        perceptionTableSha256=(Get-FileHash -LiteralPath 'Data/PolarPerceptionTable.json').Hash;
        assemblySha256=(Get-FileHash -LiteralPath '.godot/mono/temp/bin/Debug/Label Man.dll').Hash
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path 'SimLogs' "$Run-invocation.json")
    $ErrorActionPreference = 'Continue'
    & $Godot @argsPlaytest > $logPlaytest 2>&1
    $exitPlaytest = $LASTEXITCODE
    $ErrorActionPreference = 'Stop'
    $markerPlaytest = if ($OpenSave) { 'POLAR_PLAYER_PLAYTEST_OPEN' } else { "POLAR_PLAYER_PLAYTEST_PASS run=$Run" }
    if ($exitPlaytest -ne 0 -or -not (Select-String -LiteralPath $logPlaytest -SimpleMatch $markerPlaytest -Quiet)) {
        throw "Playtest failed or incomplete (exit $exitPlaytest). See $logPlaytest"
    }
    Write-Output "Completed $Run. See $logPlaytest"
} finally { Pop-Location }
