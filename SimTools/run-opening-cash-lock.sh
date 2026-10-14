#!/usr/bin/env bash
# Early-game directive V2 Phase 0: the opening cash-lock matrix (§10). Report-only probe runs, 4 at a time
# (7.2 GB RAM box). Build Debug first: Godot headless loads bin/Debug and does not rebuild.
#   SimTools/run-opening-cash-lock.sh TAG [DAYS]
# Writes SimLogs/ocl-TAG-*-opening.json; summarise with SimTools/opening_cash_lock_summary.py TAG.
set -u
cd "$(dirname "$0")/.."
TAG=${1:?tag}; DAYS=${2:-180}; P=${P:-4}   # P=lanes; REVERSE=1 walks the list backwards (a second worker beside a running one)
GODOT=${GODOT:-/c/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe}
[ -n "${SKIP_BUILD:-}" ] || dotnet build "Label Man.sln" -c Debug -v q -nologo | tail -2
jobs_file=$(mktemp)
if [ "${MATRIX:-base}" = prep ]; then
  # §6.4: distinct preparation plans against waiting, matched fixtures (same seed, town, act policy).
  for seed in 1001 1002; do for town in new_york detroit; do
    for prep in none dealer radio live; do echo "$seed $town ExMusician discerning none $prep"; done; done; done > "$jobs_file"
elif [ "${MATRIX:-base}" = fair ]; then
  # Fairness: every selling channel worked (--sell=working). One-single openings (careless, competent, truth-picked)
  # beside the playtest's order: a second session cut and pressed after the first single is out (staggered).
  for seed in 1001 1002 1003 1004; do for town in new_york los_angeles detroit memphis billings; do
    for policy in frugal discerning oracle; do echo "$seed $town ExMusician $policy none none working batch"; done
    for policy in eager replay; do echo "$seed $town ExMusician $policy none none working staggered"; done
    # Residencies are a booker's yes at a room the act has played (2026-10-14): the same openings working the rooms.
    echo "$seed $town ExMusician discerning residency none working batch"
    echo "$seed $town ExMusician eager residency none working staggered"
  done; done > "$jobs_file"
elif [ "${MATRIX:-base}" = quality ]; then
  # Closures earned or imposed: a careless pick (frugal) against a competent one (discerning), more seeds, same towns.
  for seed in 1001 1002 1003 1004; do for town in new_york los_angeles detroit memphis billings; do
    for policy in frugal discerning; do echo "$seed $town ExMusician $policy none"; done; done; done > "$jobs_file"
else
for seed in 1001 1002; do
  for town in new_york los_angeles detroit memphis billings; do
    for arch in ExMusician PawnShopOwner PromoMan TradeInsider; do
      for policy in frugal eager replay extras; do echo "$seed $town $arch $policy none"; done
    done
    # §10 fixture: two residency bookings (eager = two acts, each booked) against the no-live-income default.
    echo "$seed $town ExMusician eager residency"
  done
done > "$jobs_file"
fi
run_one() {
  read -r seed town arch policy road prep sell seq <<<"$1"; prep=${prep:-none}; sell=${sell:-home}; seq=${seq:-batch}
  name="ocl-$TAG-$seed-$town-$arch-$policy-$road"; [ "$prep" = none ] || name="$name-$prep"; [ "$seq" = batch ] || name="$name-$seq"; name=${name//_/-}
  [ -f "SimLogs/$name-opening.json" ] && return 0
  # The reverse worker never touches a job the forward one has started (its log exists).
  [ -n "${REVERSE:-}" ] && [ -f "SimLogs/$name.log" ] && return 0
  "$GODOT" --headless --path . SimTools/OpeningCashLockRunner.tscn -- --seed=$seed --enable-genre-market-v2 \
    --enable-artist-population-lifecycle --archetype=$arch --town=$town --policy=$policy --road=$road --prep=$prep --sell=$sell --sequence=$seq \
    --days=$DAYS --run=$name > "SimLogs/$name.log" 2>&1
  grep -q OPENING_CASH_LOCK_DONE "SimLogs/$name.log" && echo "ok   $name" || echo "FAIL $name"
}
export -f run_one; export GODOT TAG DAYS REVERSE
{ if [ -n "${REVERSE:-}" ]; then tac "$jobs_file"; else cat "$jobs_file"; fi; } | xargs -P "$P" -I{} bash -c 'run_one "{}"'
rm -f "$jobs_file"
