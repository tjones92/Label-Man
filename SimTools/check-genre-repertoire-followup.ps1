param([string]$RunTag='v1',[string]$Godot='C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe')
$ErrorActionPreference='Stop'
$genreCheckRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location -LiteralPath $genreCheckRoot
try {
 $genreBuildStart=[DateTime]::UtcNow.ToString('o')
 & dotnet build --no-restore -v quiet *> SimLogs/genre-followup-build.log
 $genreBuildExit=$LASTEXITCODE
 [ordered]@{command=@('dotnet','build','--no-restore','-v','quiet');startedUtc=$genreBuildStart;exitCode=$genreBuildExit;assemblySha256=(Get-FileHash -LiteralPath '.godot/mono/temp/bin/Debug/Label Man.dll').Hash;log='SimLogs/genre-followup-build.log';logSha256=(Get-FileHash -LiteralPath 'SimLogs/genre-followup-build.log').Hash} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath SimLogs/genre-followup-build.json
 if($genreBuildExit -ne 0){throw 'Diagnostic build failed; see SimLogs/genre-followup-build.log'}
 $genreCheckResults=@()
 foreach($genreCheckSeed in @(1001,1002)) {
  $genreCheckArgs=@('--headless','--path','.', 'SimTools/SaveLoadRoundTripRunner.tscn','--',"--seed=$genreCheckSeed",'--enable-genre-market-v2','--enable-artist-population-lifecycle','--composition-shape-v1=on','--composition-shape-v2=on','--folk-easy-check')
  $genreCheckLog="SimLogs/genre-followup-$RunTag-folk-easy-check-$genreCheckSeed.log"
  $ErrorActionPreference='Continue'
  & $Godot @genreCheckArgs *> $genreCheckLog
  $genreCheckExit=$LASTEXITCODE
  $ErrorActionPreference='Stop'
  $genreCheckPassed=$genreCheckExit -eq 0 -and (Select-String -LiteralPath $genreCheckLog -SimpleMatch 'FOLK_EASY_CHECK_PASS' -Quiet)
  $genreCheckResults+=@{seed=$genreCheckSeed;arguments=$genreCheckArgs;exitCode=$genreCheckExit;passed=$genreCheckPassed;log=$genreCheckLog;logSha256=(Get-FileHash -LiteralPath $genreCheckLog).Hash;assemblySha256=(Get-FileHash -LiteralPath '.godot/mono/temp/bin/Debug/Label Man.dll').Hash}
  if(-not $genreCheckPassed){throw "Folk/Easy check failed for $genreCheckSeed. See $genreCheckLog"}
 }
 $genreCheckResults | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath "SimLogs/genre-followup-$RunTag-checks.json"
 Write-Output 'Folk/Easy checks pass on seeds 1001 and 1002.'
} finally {Pop-Location}
