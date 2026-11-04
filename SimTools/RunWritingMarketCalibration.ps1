[CmdletBinding()]
param(
    [string]$Arm = 'market',
    [int[]]$Seeds = @(1001,2002),
    [int]$Weeks = 104,
    [string]$Tag = 'writing-cal',
    [string]$ExpectedAssemblySha256 = '',
    [switch]$ConcentrationAudit,
    [switch]$OpportunityAudit,
    [switch]$MaterialFunnelAudit,
    [switch]$AccessibleProfessionalSampling,
    [switch]$RockHitWriters,
	[switch]$PlanReminders,
    [switch]$CausalFanbase,
    [switch]$DefaultProfile,
    [switch]$Teams,
    [string[]]$ExtraFlags = @(),
    [ValidateSet('control','planned','delivery','combined')][string]$Workflow = 'control',
    [string]$Godot = 'C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference = 'Stop'
if ($Arm -notin @('stage1','baseline','market') -or $Tag -notmatch '^[a-z0-9-]+$') { throw 'Invalid calibration arm or tag.' }
if ($Workflow -ne 'control' -and $Arm -ne 'market') { throw 'Writing workflow experiments require the market arm.' }
if ($PlanReminders -and ($Arm -ne 'market' -or $Workflow -ne 'combined')) { throw 'Plan reminders require the combined market workflow.' }
$calRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$calDir = Join-Path $calRoot 'SimLogs/writing-calibration'
New-Item -ItemType Directory -Force -Path $calDir | Out-Null
$env:DOTNET_gcServer = '0'
$env:LABELMAN_WORKSTATION_GC = '1'
$calAssembly = Join-Path $calRoot '.godot/mono/temp/bin/Debug/Label Man.dll'
$calHash = (Get-FileHash -LiteralPath $calAssembly).Hash
# Baseline is an evidence label, not a hidden policy toggle. Refuse to silently
# relabel today's treatment as the pre-calibration implementation.
if ($Arm -eq 'baseline' -and !$ExpectedAssemblySha256) { throw 'A baseline run requires the archived baseline assembly hash.' }
if ($ExpectedAssemblySha256 -and $ExpectedAssemblySha256 -ne $calHash) { throw 'Assembly does not match the requested evidence hash.' }
foreach ($calSeed in $Seeds) {
    if ((Get-FileHash -LiteralPath $calAssembly).Hash -ne $calHash) { throw 'Assembly changed during calibration.' }
    $calName = "$Tag-$Arm-$calSeed"
    $env:APPDATA = Join-Path $calDir "userdata-$calName"
    New-Item -ItemType Directory -Force -Path $env:APPDATA | Out-Null
    $calFlags = @("--seed=$calSeed", "--weeks=$Weeks", "--run=$calName", '--release-judgement-shadow',
        '--release-judgement-select=perceived', '--label-release-slate', '--label-release-capacity',
        '--anticipatory-material-search', '--followup-chart-trace', '--chart-history', '--calibration', '--small-label-finance')
    if ($CausalFanbase) { $calFlags += @('--causal-media-atmosphere', '--media-recognition', '--fame-registration', '--observe-following', '--media-residencies', '--follower-demand') }
    if ($DefaultProfile) { $calFlags += '--default-simulation-profile' }
    if ($Arm -eq 'stage1') { $calFlags += '--professional-writing' }
    else { $calFlags += @('--professional-writing-market', '--professional-writing-market-audit') }
    if ($ConcentrationAudit) { $calFlags += '--writing-concentration-audit' }
    if ($OpportunityAudit) { $calFlags += '--label-decision-audit' }
    if ($MaterialFunnelAudit) { $calFlags += '--material-funnel-audit' }
    if ($AccessibleProfessionalSampling) { $calFlags += '--material-accessible-sampling' }
    if ($RockHitWriters) { $calFlags += '--rock-hit-writers' }
    if ($Teams) { $calFlags += '--enable-team-writing' }
    if ($Workflow -in @('planned','combined')) { $calFlags += '--writing-market-planned-assignments' }
    if ($Workflow -in @('delivery','combined')) { $calFlags += '--writing-market-delivery-offers' }
	if ($PlanReminders) { $calFlags += '--writing-market-plan-reminders' }
    $calFlags += $ExtraFlags
    if (@($calFlags | Group-Object { ($_ -split '=',2)[0] } | Where-Object Count -gt 1).Count) { throw 'Duplicate explicit flag keys.' }
    $calLog = Join-Path $calDir "$calName.log"
    $calError = Join-Path $calDir "$calName-errors.log"
    $calStart = [datetime]::UtcNow
    @{run=$calName; assemblySha256=$calHash; flags=$calFlags; startedUtc=$calStart.ToString('o')} |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $calDir "$calName-request.json") -Encoding utf8
    # Host telemetry (wall time alone cannot tell paging from CPU contention): every 15 s, the run's Godot
    # processes' CPU seconds and peak working set, the lowest free RAM seen, and the system page-in rate.
    $calTelemetry = Join-Path $calDir "$calName-host.json"
    $calMonitor = Start-Job -ArgumentList $calName, $calTelemetry -ScriptBlock {
        param($name, $out)
        $peak = 0; $cpu = 0.0; $minFree = [double]::MaxValue; $pageIn = 0.0; $samples = 0
        while ($true) {
            $procs = @(Get-CimInstance Win32_Process -Filter "Name LIKE 'Godot%'" | Where-Object { $_.CommandLine -match "--run=$([regex]::Escape($name))(\s|$)" })
            $ws = 0; $c = 0.0
            foreach ($p in $procs) { $g = Get-Process -Id $p.ProcessId -ErrorAction SilentlyContinue; if ($g) { $ws += $g.WorkingSet64; $c += $g.TotalProcessorTime.TotalSeconds } }
            if ($ws -gt $peak) { $peak = $ws }; if ($c -gt $cpu) { $cpu = $c }
            $free = (Get-CimInstance Win32_OperatingSystem).FreePhysicalMemory / 1KB
            if ($free -lt $minFree) { $minFree = $free }
            try { $pageIn += (Get-Counter '\Memory\Pages Input/sec' -ErrorAction Stop).CounterSamples[0].CookedValue; $samples++ } catch {}
            @{cpuSeconds = [math]::Round($cpu); peakWorkingSetMB = [math]::Round($peak / 1MB); minFreeRamMB = [math]::Round($minFree)
              meanPagesInputPerSec = if ($samples) { [math]::Round($pageIn / $samples, 1) } else { $null }; samples = $samples } |
                ConvertTo-Json | Set-Content -LiteralPath $out -Encoding utf8
            Start-Sleep -Seconds 15
        }
    }
    try {
        & $Godot --headless --path $calRoot --log-file (Join-Path $calDir "$calName-engine.log") SimTools/ChartAuditRunner.tscn -- @calFlags > $calLog 2> $calError
    } finally { Stop-Job $calMonitor; Remove-Job $calMonitor -Force }
    if ($LASTEXITCODE -ne 0 -or !(Select-String -LiteralPath $calLog -SimpleMatch "CHART_AUDIT_COMPLETE run=$calName weeks=$Weeks" -Quiet) -or
        (Select-String -LiteralPath $calError -Pattern 'ERROR:|Exception' -Quiet)) { throw "$calName failed; inspect logs." }
    $calHost = if (Test-Path -LiteralPath $calTelemetry) { Get-Content -LiteralPath $calTelemetry -Raw | ConvertFrom-Json } else { $null }
    @{run=$calName; assemblySha256=$calHash; flags=$calFlags; seconds=([datetime]::UtcNow-$calStart).TotalSeconds; host=$calHost} |
        ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $calDir "$calName-manifest.json") -Encoding utf8
    Write-Output "Completed $calName."
}
