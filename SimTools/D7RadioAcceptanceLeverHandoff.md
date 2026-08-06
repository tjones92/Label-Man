# D7 radio-acceptance chart-efficiency lever — session handoff

Opened August 5, 2026. Branch `d7-genre-decade-calibration`.

Child of `D7EmergentGenreFormationHandoff.md` §8/§11 (the missing chart-efficiency dimension and
the two-sided design §11.5). Read that parent's §11.4/§11.5 first — this doc implements step 1 and
reports what step 2 actually does.

## 0. One-line brief

**Step 1 (the radio-acceptance split) works and is the best result of the arc so far: hand-count
decade slot error 329 → 296.** A follow-up bundle that added step-2's `SingleOrientation` lever
and a SunshinePop keyframe raise REGRESSED to 333, and both additions are now understood to be
wrong tools. Next session: keep the radio mechanism, drop the SingleOrientation changes, re-tune
SunshinePop gently. Everything is **uncommitted**; the working tree currently holds the *bundle*
(regressed) values — §4 lists exactly what to revert.

## 1. State / runs

| run | what it is | slot error |
|---|---|---:|
| `d7-formationbase-decade-522-1001` | prior HEAD (parent doc's CURRENT) | 329 |
| `d7-radioacc-decade-522-1001` | **step-1 radio-only. BEST.** | **296** |
| `d7-radioacc2-decade-522-1001` | bundle (radio + SingleOrientation + SunshinePop keyframe) | 333 |

Slot error = sum over 20 genres of |model decade year-end slots − hand count|, RockAndRoll excluded
(misclassification caveat). Analysis scripts in the scratchpad: `radioacc2_analysis.py` (three-run
slot table + share) — hand-count benchmark is encoded there, derived from parent §11.2c/§11.4.

Godot/Python invocation notes: godot is
`/c/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe`;
Python is `/c/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe` (bash `python` is not
on PATH). Decade run command (redirect to an EXPLICIT log path — `$TMPDIR` is empty in the Bash
tool and a bare redirect silently fails):

```
"$GODOT" --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=<name> --seed=1001 \
  --enable-genre-market-v2 --enable-artist-population-lifecycle --lean-probe --profile-performance > <log> 2>&1
```

Probe suites (both must pass): add `--weeks=52 --genre-market-v2-probes --artist-population-lifecycle-probes`.

### 1a. ENVIRONMENT GOTCHA that cost this session ~2 hours — read this
Godot 4.7-mono needs the **x64 .NET 8 runtime**. Mid-session a background update left the machine on
.NET 10 only, and `GodotPlugins` segfaults at init (`godot_plugins_initialize is null`, c0000005)
when its `rollForward: LatestMajor` grabs .NET 10. Symptoms and the full dead-end trail:
- The crash is NOT your code — it reproduces with a bare `godot --headless --quit`, no project.
- `.NET 8` must be **x64** in `C:\Program Files\dotnet` (an x86 install in `Program Files (x86)` is
  useless to 64-bit Godot).
- Patching `GodotSharp/Api/{Debug,Release}/GodotPlugins.runtimeconfig.json` `LatestMajor`→`LatestMinor`
  (done this session, `.bak` files beside them) pins it to .NET 8 but was NOT sufficient alone.
- **A REBOOT fixed it.** A mid-session runtime swap leaves stale hostfxr/registry/side-by-side state
  that only a reboot clears. If Godot segfaults at .NET init after any runtime change: reboot first.

## 2. The mechanism (step 1) — KEEP THIS

`Data/GenreCatalog.cs`: `RadioAcceptanceOverrides` table + `GetRadioAcceptance(canonical)`, default
1.0. `Systems/ChartManager.cs` (~line 758, the sole caller of `GetNationalDemandAcceptance`)
multiplies the national radio acceptance by the primary genre's value, clamped [0,1]. It rides ONLY
into `UpdateRadioHeat`; sales (`GetRegionalDemandAcceptance`) and the divided-out radio access are
untouched — verified in the D5 probe (Country sales acceptance stayed 0.6292). Amplified by
`AIRPLAY_CONVEXITY=5` and concentrated late by construction (airplay is 14% of chart points in 1960,
58% in 1969), so it is a **late-decade lever only** — which is why it fixes late over/under-charting
and cannot reach early-decade Jazz/Folk. This is the "split by WHEN the error happens" design.

Radio-only tuning that scored 296: **SunshinePop 1.90, Country 0.45, PsychedelicRock 0.82**. Result:
Country 89→46 (hand 42), SunshinePop 3→23 (30), PsychRock 44→17 (26). Cost, both acceptable/known:
- Country market share fell 11.3→8.0% at 1969 (visibility→sales feedback of the airplay cut). Its
  slot target is met; its share deficit is a separate baseline issue.
- Freed fixed-100 slots leaked to album genres Jazz (44→59) and EasyListening (44→67), which the
  airplay lever cannot touch.

## 3. Two findings from the bundle — do not repeat these mistakes

1. **`SingleOrientation` INFLATES a genre; it does not thin its singles-chart slots.** Lowering Jazz
   .30→.12 / Folk .50→.32 / EasyListening .35→.28 RAISED their release counts (Jazz newReleases
   3214→3630, Folk 2631→2963) via `AlbumModel.GetCompilationChance` (lower orientation ⇒ higher
   album/compilation chance ⇒ more releases), and left year-end slots flat (Jazz 59→60 vs hand 7).
   The chart is a ranked fixed-100 competition, so surviving singles still outrank the field.
   **Early Jazz/Folk over-charting therefore has no clean lever** (airplay too small early; format
   inflates). The only redirect for freed slots is radio UP-levers on LATE singles under-charters.
   Memory: `single-orientation-inflates-not-thins`.
2. **Keyframe and radio-mult COMPOUND in the airplay channel.** Baseline feeds national acceptance →
   ×radioMult → inside the 5th power. SunshinePop keyframe 1968/69 .35/.22→.40/.40 *on top of* radio
   1.90 took it to 55 slots (hand 30) and share 0.70→4.33% (target 0.73), and stole 26 slots from
   Soul (179→153, breaking the calibrated control). The keyframe alone added ~32 slots vs parent
   §11.5's "~1 slot" estimate. Tune ONE lever at a time, gently. Memory:
   `keyframe-and-radio-mult-compound-in-airplay`.

**What DID work in the bundle: GarageRock 1.55 up-lever, 3→14.** Radio up-levers on late singles
under-charters are the correct instrument for redirecting the slots freed by the down-levers away
from the album genres that passively absorb them.

## 4. NEXT SESSION — the corrective run

The working tree currently holds the regressed bundle values. Apply these edits, then run one decade.

**`Data/GenreCatalog.cs` — REVERT the SingleOrientation changes (finding 1):**
- Jazz SingleOrientation `.12f` → `.30f` (restore; remove the step-2 comment block I added)
- Folk SingleOrientation `.32f` → `.50f` (restore; remove comment block)
- EasyListening SingleOrientation `.28f` → `.35f` (restore; remove comment block)

**`Data/GenreCatalog.cs` — SunshinePop, gentle keyframe + lower radio (finding 2, author's choice):**
- baseline 1968/69 `.40f,.40f` → **`.35f,.28f`** (gentle correction from the ORIGINAL .35/.22 — only
  the erroneous 1969 collapse is softened, and only to .28, not .40)
- `RadioAcceptanceOverrides[Genre.SunshinePop]` `1.90f` → **`1.40f`**
- Target: ~28-32 SunshinePop slots WITHOUT stealing from Soul; watch Soul stays ~179 and SunshinePop
  1969 share stays under ~2%.

**`Data/GenreCatalog.cs` — KEEP as-is (these are the wins):**
- `[Genre.Country] = 0.45f`, `[Genre.GarageRock] = 1.55f`, `[Genre.PsychedelicRock] = 0.90f`
  (PsychRock read −12 in the bundle but that was field-contaminated by the SunshinePop overshoot;
  with that reverted, 0.90 should land nearer hand 26 — verify, nudge to 0.95 if still under).

**Then:** build, run both probe suites, run the decade, score with `radioacc2_analysis.py` (point
its BUNDLE run name at the new run). **Goal: beat 296**, with Soul back at ~179 and no genre blown
out. If freed slots still pile onto Jazz/EasyListening, the next move is more radio UP-levers on late
singles under-charters (HardRock, Bubblegum are late; TeenPop/DooWop/SurfRock are early and out of
reach), NOT touching SingleOrientation.

## 5. Runtime regression (flagged, low priority)

The bundle decade run took **47 min** vs the radio-only **34 min** (per-year `wallSeconds` summed
from the `-performance-profile.csv`). Active records grew only 3.5% (16996→17588), but at 1969:
`bookSettlement` 53→106s (2×), `captureWeek` 145→192s, `populationLifecycle` 37→67s,
`labelLifecycleMonth` 13→32s — all superlinear in the release-count inflation that finding 1 caused.
Reverting SingleOrientation should return runtime toward ~34 min. The ~34-min baseline itself is the
5× formation-base artist pool from the prior session (not new). If faster iteration is wanted,
`captureWeek` + `bookSettlement` + `simulateWeek` scaling with the enlarged population is the target,
and `bookSettlement`'s superlinear response to release count is worth a look on its own.

## 6. Do not redo
- Do not use `SingleOrientation` to cut year-end slots (§3.1). It inflates.
- Do not raise a genre's keyframe and radio multiplier together, or raise either aggressively (§3.2).
- Do not remove the `AIRPLAY_CONVEXITY` genre amplifier (parent §11 / ChartSimulator TRIED-AND-REJECTED).
- Do not chase Country's market-share drop by softening its radio cut before trying a baseline raise;
  the slot fix is the priority and the share is baseline-controlled.
- Do not re-derive the environment fix — see §1a, reboot after any .NET runtime change.
