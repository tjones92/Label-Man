param(
 [ValidateSet(1001,1002)][int]$Seed = 1001,
 [ValidateSet('on','off')][string]$Mode = 'on',
 [ValidateRange(1,209)][int]$Weeks = 2,
 [Parameter(Mandatory=$true)][string]$Run,
 [switch]$Diversity,
 [ValidateSet('sample','full','none')][string]$Census = 'none',
 [ValidateRange(1,900)][int]$MaxSeconds = 300,
 [ValidateRange(-1,5)][int]$RepertoirePhase = -1,
 [string]$Godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
)
$ErrorActionPreference='Stop'
$polarRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location -LiteralPath $polarRoot
try {
 if($Run -notmatch '^[a-zA-Z0-9-]+$'){throw 'Invalid run name'}
 if(Get-ChildItem -LiteralPath SimLogs -Filter "$Run*" -ErrorAction SilentlyContinue){throw "Existing artifacts: $Run"}
 $polarArgs=@('--headless','--path','.', 'SimTools/ChartAuditRunner.tscn','--',"--seed=$Seed","--run=$Run","--weeks=$Weeks",'--enable-genre-market-v2','--enable-artist-population-lifecycle','--calibration','--polar-final-audit','--polar-followup-audit')
 if($Mode -eq 'off'){$polarArgs+='--disable-polar-fit-selection'}
 $polarArgs+=@("--polar-census=$Census","--polar-max-seconds=$MaxSeconds")
 if($Diversity){$polarArgs+='--polar-diversity-audit'}
 if($RepertoirePhase -ge 0){$polarArgs+="--repertoire-audit-phase=$RepertoirePhase"}
 [ordered]@{seed=$Seed;mode=$Mode;weeks=$Weeks;run=$Run;arguments=$polarArgs;startedUtc=[DateTime]::UtcNow.ToString('o');assemblySha256=(Get-FileHash '.godot/mono/temp/bin/Debug/Label Man.dll').Hash;tableSha256=(Get-FileHash Data/PolarSongTable.json).Hash;repertoireTableSha256=(Get-FileHash Data/PolarRepertoireTable.json).Hash;perceptionTableSha256=(Get-FileHash Data/PolarPerceptionTable.json).Hash}|ConvertTo-Json -Depth 4|Set-Content "SimLogs/$Run-invocation.json"
 $ErrorActionPreference='Continue'
 & $Godot @polarArgs *> "SimLogs/$Run.log"
 $polarExit=$LASTEXITCODE
 $ErrorActionPreference='Stop'
 if($polarExit -ne 0 -or -not(Select-String -LiteralPath "SimLogs/$Run.log" -SimpleMatch "CHART_AUDIT_COMPLETE run=$Run weeks=$Weeks" -Quiet)){throw "Incomplete $Run exit=$polarExit"}
 Write-Output "Completed $Run"
}finally{Pop-Location}
