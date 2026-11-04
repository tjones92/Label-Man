param(
    [Parameter(Mandatory=$true)][string]$RunTag,
    [int[]]$Seeds=@(1001,2002,3003),
    [ValidateSet('original','control','commitment','larger','volume')][string[]]$Modes=@('commitment'),
    [int]$Weeks=260,
    [ValidateRange(.25,1)][double]$AlbumVolume=.75,
    [ValidateRange(.5,2)][double]$AlbumInitial=1,
    [ValidateRange(3,26)][double]$AlbumRepress=8,
    [ValidateRange(0,2000)][double]$OperatorPay=0,
    [string]$Godot='C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$retuneRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$retuneFolder=Join-Path $retuneRoot "SimLogs/$RunTag"
if($RunTag -notmatch '^[a-z0-9-]+$' -or (Test-Path -LiteralPath $retuneFolder) -or $Weeks -lt 1){throw 'Choose a new run tag and positive duration.'}
if(@(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$').Count){throw 'Another game is active.'}
if(@($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Seeds must be distinct.'}
$retuneBuild=& dotnet build (Join-Path $retuneRoot 'Label Man.sln') --no-restore 2>&1
if($LASTEXITCODE -ne 0){throw ($retuneBuild -join "`n")}
New-Item -ItemType Directory -Path $retuneFolder | Out-Null
# Corrected modes retain the full audit stack. Original is the explicit pre-audit reference.
$retuneFlags=@('--enable-genre-market-v2','--enable-artist-population-lifecycle','--calibration','--small-label-finance',
 '--format-memory-maturity-curve','--single-prior-recalibration','--career-banded-format-memory',
 '--album-prior-recalibration','--album-master-costs','--release-all-in-economics','--promo-synergy-feedback',
 '--album-scale-career-milestones','--distribution-advance-recoupment','--period-royalty-tiers',
 '--record-campaign-spend','--period-ai-publishing','--cross-collateralized-renewal','--album-launch-reserve',
 '--period-list-prices','--single-studio-modifier','--self-excluded-artist-heat')
$retuneManifest=[ordered]@{weeks=$Weeks;flags=$retuneFlags;startedUtc=[datetime]::UtcNow.ToString('o');
 assemblySha256=(Get-FileHash (Join-Path $retuneRoot '.godot/mono/temp/bin/Debug/Label Man.dll')).Hash;results=@()}
# Keep the desktop responsive and the long runs inside the previously used managed-heap envelope.
$env:LABELMAN_WORKSTATION_GC='1'; $env:DOTNET_gcServer='0'; $env:DOTNET_GCHeapHardLimit='A0000000'
foreach($retuneSeed in $Seeds){foreach($retuneMode in $Modes){
 if(@(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$').Count){throw 'Another game is active.'}
 $retuneName="$RunTag-$retuneMode-$retuneSeed-$Weeks"
 if(Test-Path -LiteralPath (Join-Path $retuneRoot "SimLogs/$retuneName-weeks.csv")){throw 'Refusing to overwrite a run.'}
 $retuneModeFlags=if($retuneMode -eq 'original'){@($retuneFlags[0..3])}else{$retuneFlags}
 $retuneArgs=@('--headless','--path','.','SimTools/ChartAuditRunner.tscn','--',"--weeks=$Weeks","--run=$retuneName","--seed=$retuneSeed")+$retuneModeFlags
 if($retuneMode -eq 'commitment'){$retuneArgs+=@('--pressing-initial-forecast-share=1','--pressing-repress-weeks=8')}
 if($retuneMode -eq 'larger'){$retuneArgs+=@('--pressing-initial-forecast-share=1.5','--pressing-repress-weeks=12')}
 if($retuneMode -eq 'volume'){$retuneArgs+=@('--pressing-initial-forecast-share=1.5','--pressing-repress-weeks=12',
  "--pressing-album-initial-forecast-share=$($AlbumInitial.ToString([Globalization.CultureInfo]::InvariantCulture))",
  "--pressing-album-repress-weeks=$($AlbumRepress.ToString([Globalization.CultureInfo]::InvariantCulture))",
  "--album-purchase-volume=$($AlbumVolume.ToString([Globalization.CultureInfo]::InvariantCulture))")}
 if($OperatorPay -gt 0 -and $retuneMode -ne 'original'){$retuneArgs+="--label-operator-monthly-pay=$($OperatorPay.ToString([Globalization.CultureInfo]::InvariantCulture))"}
 $retuneStart=[datetime]::UtcNow
 $retuneStdout=Join-Path $retuneFolder "$retuneName-console.log"
 $retuneStderr=Join-Path $retuneFolder "$retuneName-errors.log"
 $retuneProcess=Start-Process -FilePath $Godot -ArgumentList $retuneArgs -WorkingDirectory $retuneRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $retuneStdout -RedirectStandardError $retuneStderr
 $null=$retuneProcess.Handle
 while(!$retuneProcess.WaitForExit(10000)){Write-Output "Running $retuneName ($([int]([datetime]::UtcNow-$retuneStart).TotalSeconds)s)."}
 $retuneProcess.WaitForExit()
 $retunePassed=$retuneProcess.ExitCode -eq 0 -and (Get-Content -LiteralPath $retuneStdout -Raw).Contains("CHART_AUDIT_COMPLETE run=$retuneName weeks=$Weeks") -and !(Select-String -LiteralPath $retuneStderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
 $retuneManifest.results+=@{name=$retuneName;seed=$retuneSeed;mode=$retuneMode;passed=$retunePassed;exitCode=$retuneProcess.ExitCode;seconds=([datetime]::UtcNow-$retuneStart).TotalSeconds;args=$retuneArgs}
 $retuneManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $retuneFolder 'runs.json') -Encoding utf8
 if(!$retunePassed){throw "Failed $retuneName; inspect logs."}
 Write-Output "Passed $retuneName."
}}
