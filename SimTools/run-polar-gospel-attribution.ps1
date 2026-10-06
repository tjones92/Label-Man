param(
 [ValidateSet(1001,1002)][int]$Seed = 1001,
 [Parameter(Mandatory=$true)][string]$Run,
 [string]$Snapshot,
 [ValidateRange(1,300)][int]$MaxSeconds = 120,
 [string]$Godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference = 'Stop'
$polarGospelRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location -LiteralPath $polarGospelRoot
try {
 if ($Run -notmatch '^[a-zA-Z0-9-]+$') { throw 'Invalid run name' }
 if (Get-ChildItem -LiteralPath SimLogs -Filter "$Run*") { throw 'Existing artifacts' }
 $polarGospelArgs = @('--headless','--path','.', 'SimTools/PolarGospelAttributionProbe.tscn','--',"--seed=$Seed","--run=$Run",'--enable-genre-market-v2','--enable-artist-population-lifecycle',"--max-seconds=$MaxSeconds")
 if ($Snapshot) { $polarGospelArgs += "--snapshot=$([IO.Path]::GetFullPath($Snapshot))" }
 [ordered]@{seed=$Seed;run=$Run;arguments=$polarGospelArgs;snapshot=$Snapshot;maxSeconds=$MaxSeconds;startedUtc=[DateTime]::UtcNow.ToString('o');assemblySha256=(Get-FileHash '.godot/mono/temp/bin/Debug/Label Man.dll').Hash;tableSha256=(Get-FileHash Data/PolarSongTable.json).Hash;repertoireTableSha256=(Get-FileHash Data/PolarRepertoireTable.json).Hash} | ConvertTo-Json -Depth 4 | Set-Content "SimLogs/$Run-invocation.json"
 $ErrorActionPreference='Continue'
 & $Godot @polarGospelArgs *> "SimLogs/$Run.log"
 $polarGospelExit=$LASTEXITCODE
 $ErrorActionPreference='Stop'
 if ($polarGospelExit -ne 0 -or -not (Select-String -LiteralPath "SimLogs/$Run.log" -SimpleMatch "POLAR_GOSPEL_ATTRIBUTION_PASS run=$Run " -Quiet)) { throw "Failed/incomplete Gospel probe $Run (exit $polarGospelExit)" }
 Write-Output "Completed fixed-world Gospel attribution: $Run"
} finally { Pop-Location }
