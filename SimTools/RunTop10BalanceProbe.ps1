[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string]$RunTag,
    [int]$Weeks=52,
    [int[]]$Seeds=@(1001,2002,3003),
    [string[]]$ExtraArgs=@('--chart-inertia-base=0.4','--radio-push-spread=0.5'),
    [switch]$AllowConcurrentProbes,
    [string]$Godot='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
. (Join-Path $PSScriptRoot 'AuditGcEnvironment.ps1')
$ErrorActionPreference='Stop'
$probeRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$probeFolder=Join-Path $probeRoot "SimLogs/$RunTag"
if($RunTag -notmatch '^[a-z0-9-]+$' -or (Test-Path -LiteralPath $probeFolder) -or $Weeks -lt 1){throw 'Choose a new run tag and positive duration.'}
if($Seeds.Count -lt 1 -or $Seeds.Count -gt 5 -or @($Seeds | Select-Object -Unique).Count -ne $Seeds.Count){throw 'Choose one to five distinct seeds.'}
$probeActive=@(Get-Process | Where-Object ProcessName -Match '^Godot|^Label Man$')
if($probeActive.Count -and !$AllowConcurrentProbes){throw 'Another game is active.'}
# The Windows console executable also owns a separate engine process.
$probeActiveCount=@($probeActive | Where-Object ProcessName -NotMatch '_console$').Count
if($probeActiveCount+$Seeds.Count -gt 5){throw 'Limit concurrent simulations to five.'}
# Same default/compact flags as RunSmallCostPilot.ps1; only the seed dispatch is parallel.
$probeFlags=@('--enable-genre-market-v2','--enable-artist-population-lifecycle',
    '--local-signals-read-demand','--regional-arrival','--regional-retail-access',
    '--regional-campaign-focus','--economic-label-exits')+$ExtraArgs
$probeAssembly=Join-Path $probeRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$probeHash=(Get-FileHash -LiteralPath $probeAssembly).Hash
foreach($probeSeed in $Seeds){
    if(Test-Path -LiteralPath (Join-Path $probeRoot "SimLogs/$RunTag-default-$probeSeed-$Weeks-weeks.csv")){throw 'Refusing to overwrite a run.'}
}
New-Item -ItemType Directory -Path $probeFolder | Out-Null
$probeManifest=[ordered]@{weeks=$Weeks;flags=$probeFlags;startedUtc=[datetime]::UtcNow.ToString('o');
    assemblySha256=$probeHash;execution='parallel seeds, default mode, compact logging';results=@()}
$probeJobs=@()
try {
    foreach($probeSeed in $Seeds){
        $probeName="$RunTag-default-$probeSeed-$Weeks"
        $probeArgs=@('--headless','--path','.','SimTools/ChartAuditRunner.tscn','--',
            "--weeks=$Weeks","--run=$probeName","--seed=$probeSeed")+$probeFlags+@('--calibration','--small-label-finance')
        $probeStdout=Join-Path $probeFolder "$probeName-console.log"
        $probeStderr=Join-Path $probeFolder "$probeName-errors.log"
        $probeStart=[datetime]::UtcNow
        $probeProcess=Start-Process -FilePath $Godot -ArgumentList $probeArgs -WorkingDirectory $probeRoot -WindowStyle Hidden -PassThru -RedirectStandardOutput $probeStdout -RedirectStandardError $probeStderr
        $null=$probeProcess.Handle
        $probeJobs+=@{name=$probeName;process=$probeProcess;start=$probeStart;args=$probeArgs;stdout=$probeStdout;stderr=$probeStderr;done=$false}
        Write-Output "Started $probeName (PID $($probeProcess.Id))."
    }
    $probeManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $probeFolder 'runs.json') -Encoding utf8
    $probeLastReport=[datetime]::UtcNow
    while(@($probeJobs | Where-Object { !$_.done }).Count){
        foreach($probeJob in $probeJobs | Where-Object { !$_.done }){
            if(!$probeJob.process.WaitForExit(1)){continue}
            $probeJob.process.WaitForExit()
            # weeks.csv is header-only in compact mode. Count chart weeks from records.csv instead.
            $probeRecordsFile=Join-Path $probeRoot "SimLogs/$($probeJob.name)-records.csv"
            $probeSeenWeeks=[Collections.Generic.HashSet[int]]::new()
            if(Test-Path -LiteralPath $probeRecordsFile){
                foreach($probeLine in [IO.File]::ReadLines($probeRecordsFile)){
                    $probeWeek=0
                    if([int]::TryParse($probeLine.Split(',')[0],[ref]$probeWeek)){$null=$probeSeenWeeks.Add($probeWeek)}
                }
            }
            $probeObservedWeeks=$probeSeenWeeks.Count
            $probePassed=$probeJob.process.ExitCode -eq 0 -and $probeObservedWeeks -eq $Weeks -and
                (Get-Content -LiteralPath $probeJob.stdout -Raw).Contains("CHART_AUDIT_COMPLETE run=$($probeJob.name) weeks=$Weeks") -and
                !(Select-String -LiteralPath $probeJob.stderr -Pattern 'SCRIPT ERROR|ERROR:' -Quiet)
            $probeManifest.results+=@{name=$probeJob.name;passed=$probePassed;exitCode=$probeJob.process.ExitCode;
                seconds=([datetime]::UtcNow-$probeJob.start).TotalSeconds;observedWeeks=$probeObservedWeeks;args=$probeJob.args}
            $probeJob.done=$true
            $probeManifest | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $probeFolder 'runs.json') -Encoding utf8
            Write-Output "Finished $($probeJob.name): passed=$probePassed, weeks=$probeObservedWeeks."
        }
        if(([datetime]::UtcNow-$probeLastReport).TotalSeconds -ge 60){
            foreach($probeJob in $probeJobs | Where-Object { !$_.done }){
                $probeRecordFile=Join-Path $probeRoot "SimLogs/$($probeJob.name)-records.csv"
                # A records file can exist before its first line is written; an empty tail must not abort the launcher.
                $probeTail=if(Test-Path -LiteralPath $probeRecordFile){Get-Content -LiteralPath $probeRecordFile -Tail 1}else{$null}
                $probeLatest=if($probeTail){"$probeTail".Split(',')[0]}else{'starting'}
                Write-Output "$($probeJob.name): week $probeLatest"
            }
            $probeLastReport=[datetime]::UtcNow
        }
        if(@($probeJobs | Where-Object { !$_.done }).Count){Start-Sleep -Seconds 5}
    }
    if((Get-FileHash -LiteralPath $probeAssembly).Hash -ne $probeHash){throw 'Assembly changed during the probe; results cannot be reviewed together.'}
    if(@($probeManifest.results | Where-Object { !$_.passed }).Count){throw 'Probe failed; inspect logs.'}
} finally {
    # Stop only this launcher's children if dispatch or monitoring fails.
    foreach($probeJob in $probeJobs){
        # The console shim owns a separate engine; terminating only the shim
        # can leave the simulation running. Verify both parent and run identity.
        $probeChildren=@(Get-CimInstance Win32_Process -Filter "ParentProcessId = $($probeJob.process.Id)" -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like 'Godot*' -and $_.CommandLine -like "*--run=$($probeJob.name) *" })
        foreach($probeChild in $probeChildren){Stop-Process -Id $probeChild.ProcessId -ErrorAction SilentlyContinue}
        if(!$probeJob.process.HasExited){Stop-Process -Id $probeJob.process.Id -ErrorAction SilentlyContinue}
    }
}
