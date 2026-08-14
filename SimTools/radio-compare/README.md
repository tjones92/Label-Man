# Radio-branch genre comparison scripts

Small analysis scripts for comparing `ChartAuditRunner` decade runs on the
`radio-station-network` branch (reporter-panel radio: vitality + vacuum guard work).
Rebuilt 2026-08-12; they read the run CSVs directly out of `SimLogs/`.

## Interpreter
`Import-Csv` dies on the large SimLogs CSVs — use the installed Python:

```
C:\Users\grohl\AppData\Local\Programs\Python\Python314\python.exe
```

Set `PYTHONUTF8=1` before running (`genre_shift.py` prints deltas; without UTF-8
the Windows cp1252 console can choke on non-ASCII).

## Paths
`_common.py` hardcodes the machine paths (portable across sessions on this box):
- `SIMLOGS = C:\Project\Label-Man\SimLogs`
- `TARGETS = C:\Project\Label-Man\SimTools\AdjustedHistoricalGenreShareTargets.csv`

A run is referenced by its `--run=<name>` prefix; the scripts append
`-genre-decade-shape.csv` / `-weeks.csv`. Genre names are normalized so the
enum-named shape CSV (`BritishPop`) matches the space-named target table
(`British Pop`); R&B is aliased (rb→rnb).

## Scripts

- **`returns_no1.py <run> [<run2> ...]`** — returns-to-#1 metric from `<run>-weeks.csv`.
  A #1 SPELL is a maximal run of consecutive weeks with the same `numberOneRecordId`;
  a spell is a RETURN if that record held #1 in an earlier non-adjacent spell.
  `ret% = returns / spells`. Reproduces 13.4% on `radio-w013fix-decade-1001` (the
  pre-vitality reference). Flags >14%. Needs full telemetry — a `--calibration` run
  empties `weeks.csv`.

- **`target_check.py <run> [--genres A,B,C] [--metric marketUnitsShare]`** — per-genre
  share vs the historical target table, per year 1960-69, with a sum-of-abs-error.
  Absolute (no baseline run needed), so one run validates directly against targets.

- **`genre_shift.py <runA> <runB> [--genres ...] [--metric chartWeekShare]`** — per-genre
  metric delta (B − A) per year. Positive = B has more. Use for A/B vs a baseline run.

## Key baselines (seed 1001)
- `v31-1001` — V3.1 frozen baseline (market-bound reference).
- `radio-w013fix-decade-1001` — pre-vitality radio branch (returns 13.4%).
- `radio-vitality-ret-1001` — vitality fix, guard OFF.

## Run command (full telemetry decade, seed 1001)
```
"C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe" \
  --headless --path . SimTools/ChartAuditRunner.tscn -- \
  --weeks=522 --run=<name> --seed=1001 \
  --enable-genre-market-v2 --enable-artist-population-lifecycle
```
Build `-c Debug` first (headless loads `bin/Debug`; stale otherwise). ~1 hr with full
telemetry. Do NOT add `--calibration` if you need returns-to-#1 (it empties weeks.csv).
