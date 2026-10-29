#!/bin/bash
# Big-5 handoff lead 1: §9s §9u relive screen. Usage: run_pf.sh "seed tag flags..." ...
G='C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe'
OUT=SimLogs/regprobe
cd /c/Project/Label-Man; mkdir -p $OUT
LW=$1; shift
mapfile -t RAW < /c/Project/Label-Man/SimTools/regional_fit_profile_flags.txt
PROFILE=(); for f in "${RAW[@]}"; do [ "${f%%=*}" != "--slate-work-budget-midtier" ] && PROFILE+=("$f"); done
PROFILE+=(--slate-work-budget-midtier=2.5); P2=(); for f in "${PROFILE[@]}"; do [ "${f%%=*}" != "--single-commercial-fit-launch-weight" ] && P2+=("$f"); done; PROFILE=("${P2[@]}" --single-commercial-fit-launch-weight=$LW)
BUNDLE=(--artist-written-hook-scale --self-writing-craft-gate=0.5 --rock-hit-writers --rock-hit-office-placement=2 --rock-hit-office-opening)
run() { local s=$1 tag=$2; shift 2; local name=hs52op$s-SMP-HGRPO-$tag
  if grep -q CHART_AUDIT_COMPLETE "$OUT/$name-console.log" 2>/dev/null; then echo "$name done"; return; fi
  start=$(date +%s)
  "$G" --headless --log-file "$OUT/$name-engine.log" --path . SimTools/ChartAuditRunner.tscn -- --run=$name     --weeks=52 --seed=$s --small-label-finance --profile-performance --label-decision-audit --historical-independent-stars     --slate-work-budget-named-independents=2.5 --legacy-simulation-profile "${PROFILE[@]}" "${BUNDLE[@]}" "$@" > "$OUT/$name-console.log" 2>&1
  echo "$name exit $? $(( $(date +%s) - start ))s"; }
for j in "$@"; do
  while [ "$(jobs -rp | wc -l)" -ge 3 ]; do wait -n; done
  run $j --station-playlists &
done
wait; echo "screen finished $(date)"
