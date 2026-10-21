[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][ValidatePattern('^[a-z0-9-]+$')][string]$RunTag,
    [int[]]$Seeds=@(1001,2002,3003),
    [ValidateSet('off','ledger','staff','premises','tax','distributions','capital','commercial','excise','full','prior','default')][string[]]$Modes=@('full'),
    [ValidateRange(1,574)][int]$Weeks=53,
    [ValidateRange(.5,2)][double]$InitialShare=1.5,
    [ValidateRange(3,26)][double]$RepressWeeks=12,
    [ValidateSet('audit','game')][string]$Profile='audit',
    # Extra switches appended to every run, e.g. a candidate flag bundle on the game profile.
    [string[]]$ExtraFlags=@(),
    [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$corporateRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$corporateOutput=Join-Path $corporateRoot "SimLogs/$RunTag"
if(Test-Path -LiteralPath $corporateOutput){throw 'Choose an unused run tag.'}
if(@($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Seeds must be distinct.'}
if($Profile -eq 'game' -and @($Modes | Where-Object { $_ -notin @('default','full','prior','off') }).Count){
    throw 'Use the audit profile for isolated mechanism/ledger arms, with its common settlement prerequisites.'
}
if(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$'){throw 'Another game is active.'}
New-Item -ItemType Directory -Path $corporateOutput | Out-Null
$corporateFlags=@('--enable-genre-market-v2','--enable-artist-population-lifecycle','--calibration','--small-label-finance',
 '--format-memory-maturity-curve','--single-prior-recalibration','--career-banded-format-memory',
 '--album-prior-recalibration','--album-master-costs','--release-all-in-economics','--promo-synergy-feedback',
 '--album-scale-career-milestones','--distribution-advance-recoupment','--period-royalty-tiers',
 '--record-campaign-spend','--period-ai-publishing','--cross-collateralized-renewal','--album-launch-reserve',
 '--period-list-prices','--single-studio-modifier','--self-excluded-artist-heat',
 "--pressing-initial-forecast-share=$($InitialShare.ToString([Globalization.CultureInfo]::InvariantCulture))",
 "--pressing-repress-weeks=$($RepressWeeks.ToString([Globalization.CultureInfo]::InvariantCulture))")
if ($Profile -eq 'game') { $corporateFlags=@('--calibration','--small-label-finance') }
$corporateFlags+=$ExtraFlags
$corporateAssembly=Join-Path $corporateRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$corporateHash=(Get-FileHash -LiteralPath $corporateAssembly).Hash
$corporateManifest=[ordered]@{weeks=$Weeks;profile=$Profile;extraFlags=@($ExtraFlags);seeds=@($Seeds);modes=@($Modes);startedUtc=[datetime]::UtcNow.ToString('o');assemblySha256=$corporateHash;results=@()}
$env:LABELMAN_WORKSTATION_GC='1'; $env:DOTNET_gcServer='0'; $env:DOTNET_GCHeapHardLimit='A0000000'
foreach($corporateSeed in $Seeds){foreach($corporateMode in $Modes){
    if(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$'){throw 'Another game is active.'}
    $corporateName="$RunTag-$corporateMode-$corporateSeed-$Weeks"
    if(Test-Path -LiteralPath (Join-Path $corporateRoot "SimLogs/$corporateName-label-finance.csv")){throw 'Refusing to overwrite an audit.'}
    $corporateArgs=@('--headless','--path','.', 'SimTools/ChartAuditRunner.tscn','--',"--weeks=$Weeks","--run=$corporateName","--seed=$corporateSeed")+$corporateFlags
    $corporateSwitch=switch($corporateMode){
        'ledger'{'--label-company-ledger'} 'staff'{'--label-staff-costs'} 'premises'{'--label-premises-costs'}
        'tax'{'--label-income-tax'} 'distributions'{'--label-distributions'} 'capital'{'--label-capital-investment'}
        'full'{'--label-corporate-model'} 'off'{'--no-label-corporate-model'}
        'commercial'{'--label-commercial-costs'} 'excise'{'--label-record-excise'}
        'prior'{@('--label-corporate-model','--no-label-commercial-costs','--no-label-record-excise')}
        default{$null}
    }
    if($corporateSwitch){$corporateArgs+=$corporateSwitch}
    $corporateStart=[datetime]::UtcNow
    $corporateStdout=Join-Path $corporateOutput "$corporateName-console.log"
    $corporateStderr=Join-Path $corporateOutput "$corporateName-errors.log"
    $corporateProcess=Start-Process -FilePath $Godot -ArgumentList $corporateArgs -WorkingDirectory $corporateRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $corporateStdout -RedirectStandardError $corporateStderr
    $null=$corporateProcess.Handle
    while(!$corporateProcess.WaitForExit(10000)){Write-Output "Running $corporateName ($([int]([datetime]::UtcNow-$corporateStart).TotalSeconds)s)."}
    $corporateProcess.WaitForExit()
    $corporatePassed=$corporateProcess.ExitCode -eq 0 -and (Get-Content -LiteralPath $corporateStdout -Raw).Contains("CHART_AUDIT_COMPLETE run=$corporateName weeks=$Weeks") -and !(Select-String -LiteralPath $corporateStderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
    if((Get-FileHash -LiteralPath $corporateAssembly).Hash -ne $corporateHash){throw 'Assembly changed during audit.'}
    $corporateManifest.results+=@{name=$corporateName;seed=$corporateSeed;mode=$corporateMode;passed=$corporatePassed;exitCode=$corporateProcess.ExitCode;seconds=([datetime]::UtcNow-$corporateStart).TotalSeconds;args=$corporateArgs}
    $corporateManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $corporateOutput 'runs.json') -Encoding utf8
    if(!$corporatePassed){throw "Failed $corporateName; inspect logs."}
    Write-Output "Passed $corporateName."
}}
