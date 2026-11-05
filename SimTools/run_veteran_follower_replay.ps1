param([switch]$On)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$dir = Join-Path $root 'SimLogs/writing-calibration'
$source = Get-Content -Raw (Join-Path $dir 'veteran-recovery-on-market-2002-request.json') | ConvertFrom-Json
$assembly = Join-Path $root '.godot/mono/temp/bin/Debug/Label Man.dll'
if ((Get-FileHash -LiteralPath $assembly).Hash -ne $source.assemblySha256) { throw 'Assembly differs from control.' }
$name = if ($On) { 'veteran-follower-on-market-2002' } else { 'veteran-follower-off-market-2002' }
$flags = @($source.flags | Where-Object { $On -or $_ -ne '--follower-demand' } | ForEach-Object { if ($_ -like '--run=*') { "--run=$name" } else { $_ } })
$env:APPDATA = Join-Path $dir "userdata-$name"
$env:DOTNET_gcServer = '0'
$env:LABELMAN_WORKSTATION_GC = '1'
New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
@{flags=$flags; assemblySha256=$source.assemblySha256; startedUtc=[datetime]::UtcNow.ToString('o')} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $dir "$name-request.json")
$godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path $root --log-file (Join-Path $dir "$name-engine.log") SimTools/ChartAuditRunner.tscn -- @flags > (Join-Path $dir "$name.log") 2> (Join-Path $dir "$name-errors.log")
if ($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath (Join-Path $dir "$name.log") -SimpleMatch "CHART_AUDIT_COMPLETE run=$name weeks=522" -Quiet)) { throw 'Replay did not complete successfully.' }
