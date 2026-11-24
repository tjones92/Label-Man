# Big-5 volume and per-launch premium: handoff (2026-11-17)

Branch `stature-fanbase`. Detail lives in `SimTools/LpGenreAllocationFollowup.md` §49-57; this file is the summary and the
open problem. Earlier root-cause work: `SimTools/MajorShareRootCauseHandoff.md` (finding of 2026-11-13).

## Goal

Make the 1960-61 major-label picture historical **by mechanism, not by a tax**:

| Target (history) | Value | Sim now (reference `zfg`, 4 seeds) |
|---|---:|---:|
| Big-5 share of singles issued | ~16-19% (~1,080 of 5,800-6,700 in 1960) | 7-8% (1960), 10.5-13% (1961) |
| Big-5 Top 10 slot share 1960 | 34.2% | 16.6-33.6% (mean ~27) |
| Big-5 Top 10 slot share 1961 | 20.6% | 26.2-37.9% (mean ~32) |
| CR4 on Top 10 slots 1960 / 1961 | 34.6 / 27.9 | ~28 / ~32 |
| Big-5 newcomer Top 10 records a year (1961-64) | ~4.5 | 5-9 |
| Big-5 per-release Top 10 premium over the field | ~1.2x | 2-4x (5-7x on older controls) |

The user's standing decisions:
- Five historical US majors only (`--historical-majors`); no procedural Majors.
- Majors keep their national distribution edge (historically real, §56).
- Big-5 volume should match history's **share** of issues (~18%, ~750 singles at the sim's ~4,200 total), taken from
  Independent / Small / Boutique output; MidTiers are already below history and are held.
- `--zero-fee-material-gate` is kept (committed 0f8ec03; on every arm from here).
- Closures must be earned, not imposed. Don't add a Major tax.

## The open problem: the per-launch premium

Each Big-5 single converts to Top 10 at 2-4x a field single's rate, where history shows ~1.2x. Every lever that changes how
many *distinct acts* the Big-5 launch multiplies that premium. This is why putting Big-5 volume on history (§57) pushes chart
share further from history. **Fix the premium first, then re-screen the volume package.**

What the 2026-11-13 decomposition said (B5 ÷ Small T40 per connected single 4.11x, log share): quality mix ~35% (production via
`label.productionQuality`, and slate picks from 34-42 candidates per slot against 2.8 for small labels), act-heat mix ~20% (hot acts 38%
of Big-5 launches vs 8%), launch footprint ~28% (arrival hold, fit exposure 6.5 vs 3.7 regions, uncovered stock, regional
launch factor), residual ~17%. Promotion terms add ~0 once the footprint is open. The commercial-fit draw is label-blind.

### Already ruled out (don't rebuild)

| Lever | Result | Where |
|---|---|---|
| Newcomer launch package (impact + first-run stock −40%) | inert on newcomer entry / T10 | §51, `--major-newcomer-package` |
| Promotion follows action (weekly push cut until action) | inert: newcomer hits show action by week 3 | §55, `--major-newcomer-action-push` |
| Home-first launch for Major newcomers | not built: history shows majors shipped nationally (1960 Big-5 Top 10: 0 of 20 with a 2-week city lead; indie 18.5%) | §56 |
| Second-rank earned capacity / hit-act package | buys entries, not Top 10 slots | §52-54 |
| Softened footprint (fit base, connected arrival, craft floor) | raises Big-5 share or not adoptable | MajorShareRootCauseHandoff 2026-11-13 |
| Launch push saturation, act-earned impact, momentum penalty | ≤9 pts, seeds disagree | memory `big5-premium-constant-1961-is-volume` |

### Candidates not yet screened

0. **Screened 2026-11-17, rejected: `--major-slate-pool=4`** (`Systems/MajorVolumeSwitches.cs`; a Major's ordinary single slot
   reviews hot acts + 4 keyed others; off run `mpoff27-2002-sc` byte-identical to `zfg` through week 26). Arms `mpool4-{2002,1001,3003,4004}-sc`
   vs `zfg`. 1961 newcomer Top 10 premium (Big-5 ÷ field): 3.5→1.9, 3.6→5.4, 4.1→5.3, 1.8→2.9 (up on 3 of 4). Established Big-5 Top 10
   per launch up 4 of 4 (mean 9.3→12.4); 1961 Big-5 Top 10 share up 3 of 4 (31.9→38.2); Big-5 issues down 4 of 4 (more gate
   cancellations). Big-5 newcomer quality unchanged (1961 .719 → .720): the slate's depth does not select quality, so the quality
   share of the premium is production, not picking. Next: candidate 2.
   **Candidate 2 diagnostic 2026-11-17, `--major-newcomer-craft=0.65`** (a Major newcomer single's label-craft term reads
   min(craft, .65); off run `ncoff27-2002-sc` identical). Arms `ncraft65-*-sc`. Big-5 newcomer production −.059 to −.069 on all
   seeds, quality only −.008 to −.021 (hooks +.002 to +.018, 4 of 4: labels pick slightly stronger material); quality gap to field
   newcomers .043 → .030. 1961 newcomer Top 10 premium 3.5→1.6, 3.6→4.0, 4.1→2.5, 1.8→3.2 (split 2/2; mean 3.3 → 2.8); Hot 100 entry
   premium up 3 of 4; 1961 Big-5 Top 10 share split 2/2. Removing most of the newcomer craft edge does not move the premium past
   seed noise, so producer scarcity (a mechanism that can only do less than this cap) cannot close a ~3x gap. Not built.
   **Taste fit, 2026-11-17 (no runs; `zfg` x4, launches to week 92, offline fit replay via `keyed_fit_draws.py`).** Genre mix is not
   the premium: reweighting field newcomers to the Big-5 genre mix moves the newcomer Top 10 premium 2.86 → 2.71, and Big-5
   newcomers out-convert the field in nearly every genre (R&R H100 2.3x, TeenPop 1.5x, Country 1.7x). The fit is label-blind for
   newcomers in the reference (launch weight 0, push weight 0, act record 0): P(connected) 15.5% Big-5 vs 15.1% field; 96-99% of
   newcomer Top 10s come from connected draws. The whole premium is realization of a connected draw: connected Big-5 newcomers
   reach the Hot 100 67% vs 37% (1.8x) and convert entry to Top 10 35% vs 23% (1.5x); T10 per connected 23.5% vs 8.7% (2.7x; per
   seed 3.5 / 2.7 / 2.5 / 1.7). Connected inputs: quality .72 vs .67, launch stock 5,410 vs 2,883, campaign impact .55 vs .33. A
   label-dependent taste fit would be a handicap on an already label-blind draw; not built. Open question: why 63% of connected
   field newcomers never reach the Hot 100 (history: indie hits broke regionally and still charted).
   **Connected-newcomer trace, 2026-11-17.** New telemetry switch `--newcomer-connect-trace` (ChartAuditRunner; per region and week,
   first 12 weeks of every live AI newcomer single whose draw is `SingleCommercialFit.Connected`, i.e. the upper half of draws).
   Replays `nctrace-{2002,3003}-sc` are byte-identical to `zfg` on all 146 files. 330 Big-5 / 5,839 field records; Hot 100 23.0% vs
   12.6%. (1) Entry is a weeks 1-3 sales bar and is label-blind at matched units: H100 69/51% at 1-3k units, 83/94% at 3-6k, 100%
   above. (2) A field newcomer covers 3.4 of 7 regions; every uncovered region is held (96-98% still held at week 3), 98-99% of its
   weeks 1-3 units come from covered regions, and held regions open only from week 5-6, after entry. (3) Logit of H100 entry: Big-5
   odds x2.07; + covered regions x1.49; + quality x1.15; + campaign impact x1.00 (stock adds nothing). So the premium is coverage
   ~45%, record quality ~35% (quality .71 vs .67, mostly hook .74 vs .69), campaign impact ~20%, label residual ~0. Field at 7
   covered regions still enters at 14% vs Big-5 23% (quality + impact). Two seeds.
   **Arrival check, same traces.** Pre-entry, a held region opens only by the neighbour test (`ChartManager.ApplyBreakoutDiscovery`:
   a covered source at RegionalBreakout with 2 traction weeks adds strength × (0.25 + 0.45 reach + 0.30 distribution) × 0.10 a
   week, decay 0.78, threshold 0.08 in `RegionalAudience.TryArrive`), so 3-5 weeks after the source breaks. Field newcomers that
   chart: a covered region reaches RegionalBreakout at median week 3 (90% before debut), debut median week 6, held regions open
   median week 6: 38% before debut, 28% within 2 weeks after, 21% 3+ weeks after, 14% not by week 12. That is close to history's
   indie spread (§56: ~80% of indie Top 10 records reached LA/Minneapolis by debut +2 weeks; sim indie lead≥2 20.7 vs 18.5).
   Misses: 40% have a covered region at RegionalBreakout (median week 4), but only 29% of their held regions open by week 12.
   Faster arrival was already screened as `ca` (`--connected-arrival=1`, only with `fb`, 2026-11-13): it raised Big-5 T40/H100
   share and pushed the indie stagger below history (lead≥2 20.7 → 13.5). Arrival is not a clean lever: the sim's spread for
   charting indies is already near history, and speeding it erases the historical city lead.
   **Hook source, 2026-11-17 (no runs; `zfg` x4; doc §58; `SimTools/analyze_newcomer_hook_source.py`).** Record hook is 90% the
   song's composition hook. Big-5 newcomer composition hook +.029 (.755 vs .726), split: source × genre mix +.005, act writer
   craft ~+.007 (all of the own-song edge), path mix ~−.004, label-side picking ~+.020. The picking comes from deeper shortlists
   (2.39 vs 1.93 recordable; pro arrivals scale with 0.5 + 0.63 × scouting), sharper reads (band .099 vs .118, all read bands
   scale with scouting) and a higher material-search bar (.715 vs .618). Not writers or offices: publisher mix +.002, writer
   quartile mix +.008 within pro songs, R&R hit office 5.5% vs 6.1%. Worth: at field hooks the newcomer Top 10 premium falls
   2.86 → 2.32 (×0.77-0.84 on 4 of 4 seeds), ~20% in log terms (cross-sectional upper bound); the picking part ~12%. A
   writing-talent bank would act on writer access (~.003) or on free pitches the Majors could best afford. Not built. Record quality
   (slate depth, production, hook) is now ruled out as the carrier; candidate 3 (footprint) is what is left.
1. **Slate selection depth.** Majors choose from ~122 scored candidates per slot (small labels ~3-16). A Major that releases
   ~2-3x more singles (§57) should pick less selectively per slot; check whether the quality and heat mix of Big-5 launches
   falls under the volume package, and whether a candidate-pool cap per slot brings per-launch conversion toward 1.2x.
2. **Label production quality** (largely answered: the candidate-2 craft cap and the §58 hook split put record quality at
   ≤20-25% of the premium). Major `productionQuality` 0.72-0.98 feeds record quality directly; history's flops came from
   volume at ordinary quality. Under `--major-gate-exemption` the marginal Big-5 single records an own session (lower quality),
   which already lowered newcomer quality a little (.725 → .699 on seed 2002).
3. **Footprint jointly.** The footprint is an interaction (city reach 32%, launch factor 19%, hold + exposure 7% of foot's drop);
   single-part softenings fail. The author wants the national edge kept, so this is the last resort.

### Plan: candidate 3, Major launch depth (written 2026-11-17; stages 0-1 done, doc §59)

**Stage 1 result (2026-11-17).** Built `--label-blind-launch-scope` (check passes; `lbsoff27-2002-sc` identical through week 26).
`mnfoot` cuts the pooled newcomer Top 10 premium 2.86 → 1.84 (down 3 of 4 seeds), Hot 100 premium 1.89 → 1.66, 1961 Big-5 Top 10
share 31.9 → 27.2 (3/4), CR4 31.5 → 28.1 (4/4), newcomer Top 10 records 7.2 → 6.2 a year (1961); Big-5 stagger lead holds (9.4 →
9.1), gap ≥ 6 3.8 → 6.9. Big-5 newcomers drop to 4.5 exposed regions (field 4.1) and field-level weeks 1-3 units. `mjfoot` (every
Major single) adds nothing for newcomers, takes the established premium 1.86 → 1.39, and fails the stagger guard (Big-5 lead ≥ 2
25.8%, history 0): keep established depth. 1960 Big-5 share falls 26.8 → 23.3 (the volume gap). **Go to stage 2.**

**What is already known.** `foot` (every live AI single launches as a covered label at reach .38) cut 1961 Big-5 Top 10 share
37.9 → 32.1 (3 of 4 seeds). Its parts alone each *raised* Big-5 share (`split-v1`); a lift-only dose for non-Majors closed Big-5 ÷
Small but left Big-5 share flat (`dose-v1`). Conclusion then: `foot`'s share effect came from pulling the Majors' own footprint
down. No arm has ever applied `foot` to the Majors alone.

**Why this keeps the national edge.** A Major already covers every region, holds none for arrival and reaches every city
(`CityReach.Launch` territory = distribution). For a Major, `foot` therefore changes only depth: the regional launch factor
(`ChartSimulator.GetRegionalLaunchFactor`: strong 1.35, covered 0.55 + 0.45·reach ≈ .89-1.0 → .72), the fit-exposure launch chance
(`RegionalFitExposure.Launch`: 0.3 + 0.7·reach ≈ .83-1.0 → .57, label home no longer free) and launch stock (inert: §51, dealer
quarter-stock). Breadth (coverage, no hold, every city) is untouched. This is §56's history: majors shipped every single
nationally, but stock and play for an unknown were order-led. Fit exposure is the one read that persists past launch: an
unexposed region stays unexposed until the record reaches the national top N (keyed experiment: OR 1.14 per exposed region on
connected singles). The launch factor seeds launch awareness and radio only, and §51 found launch awareness cuts inert for
newcomers, so expect exposure to carry any effect.

**Stage 0: build (one switch, about 45 minutes).**
- `--label-blind-launch-scope=major-newcomer|major` in `Systems/LabelBlindLaunch.cs`: restrict the existing `foot` parts to live
  singles at Major-tier labels (`major`), or only those by NewSigning/Unsigned acts at launch (`major-newcomer`, the scorer's
  newcomer test; `MajorNewcomerPackage.Applies` uses NewSigning only). The test sits in `Launch()`, which already receives the artist
  (`CompetitorManager.ApplyReleasePromotion`). Requires `--label-blind-launch=...`; no draws.
- Check: new cases in `--label-blind-launch-check` (a field single and a Major's established single read their own footprint
  under the scope; a Major newcomer reads the reference). 27-week off run (`--label-blind-launch-scope` alone is refused, so
  the off run is the reference request) byte-compared to `zfg-2002-sc` (records, single-launch-attribution, label-finance,
  release-slate, programme-choices).
- Scorers: give `analyze_regional_stagger.py` a run-format argument (it hard-codes `hs52op…`) with the `.gz` fallback; add the Hot
  100 entry premium per tier and Big-5 newcomer exposed regions at launch to `analyze_major_volume.py`. Add
  `--newcomer-connect-trace` to stage-1 arms (telemetry only, byte-identical) for exposure and weeks 1-3 units.

**Stage 1: kill tests (8 runs, two batches of four, ~50 minutes; ~450 MB a run, 38 GB free).** Reference `zfg`, 104 weeks, seeds
2002 / 1001 / 3003 / 4004, `run_request_arm.ps1 -Extra`:
- `mnfoot`: `--label-blind-launch=foot,--label-blind-launch-scope=major-newcomer`.
- `mjfoot`: `--label-blind-launch=foot,--label-blind-launch-scope=major` (the bound: every Major single, established acts too).

Read on launches to week 92 (`analyze_major_volume.py`), same sign on 3 of 4 seeds:
- Primary: Big-5 newcomer Top 10 premium on 1960-61 launches to week 92 (`zfg` 4.0 / 2.8 / 3.0 / 1.4, pooled 2.86; 1961 alone
  3.5 / 3.6 / 4.1 / 1.8; history ~1.2) and Hot 100 entry premium (`zfg` pooled 1.89). Secondary: Big-5 newcomer Top 10 records a year (history ~4.5), established Top 10 per launch, 1960 / 1961
  Big-5 Top 10 share and CR4 (history 34.2 / 20.6, 34.6 / 27.9).
- **Go:** `mnfoot` cuts the newcomer Top 10 premium by at least a third in log terms (mean ≤ ~2.0). **Stop:** under ~15% (mean ≥
  ~2.4): launch depth is not the newcomer carrier either. Record it and turn to the post-launch weekly terms (restock, rack shelf,
  uncovered sales terms; the ~17% residual of the 2026-11-13 decomposition), since quality, slate, promotion and footprint would
  all be measured out.
- `mjfoot` against `mnfoot` says how much of the established-act premium sits in launch depth. History supports thin depth only for
  unknowns, so `mjfoot` is a bound, not a candidate.

**Stage 2: attribute and soften (if go; 8-12 runs).** Scoped parts `--label-blind-launch=expose` and `=factor` (stock is inert;
hold and city are no-ops for a Major) to find the carrier, then a period form for the carrier: a keyed share D of a Major
newcomer's regions launch at the reference read (the mirror of `dose-v1`, lower only; D = 0.5 first). Prefer the dose over an
"until action" release: §55 found newcomer hits show action by week 3, before entry.

**Stage 3: guards on the chosen arm.**
- 1960 city stagger (`analyze_regional_stagger.py`): Big-5 Top 10 city lead ≥ 2 and gap ≥ 6 stay at ~0 (history 0 of 20);
  indie lead ≥ 2 / gap ≥ 6 near 18.5 / 14.6 (`zfg` 20.7 / 23.0).
- Big-5 gross and pretax (`label-finance`), closures by week 104 (`zfg` 106 / 101 / 84 / 94), units per single, MidTier issues.

**Stage 4: re-screen the volume package on top** (4 runs): chosen arm + `--programme-major-singles=16 --slate-work-budget=0.85
--slate-work-budget-midtier=1 --major-gate-exemption` (replace `--slate-work-budget=1` in a reference copy). Targets: Big-5 share of
issues 16-19%, 1961 Top 10 share ~21 without 1960 falling below ~30, newcomer Top 10 records ~4.5 a year.

Score any lever on the **per-launch** premium (or share at fixed launch share), not on 1960 vs 1961 share. Single-seed Top 10
share swings ±10 points on reshuffle alone; require the same sign on 3 of 4 seeds.

## What §57 built and found (not adopted)

Switches (all off and identical; `mvoff27-2002-sc` matches `zfg-2002-sc` row for row through week 26):
- `--programme-major-singles=N`: cap raised 16 → 30 (`Systems/LabelReleaseProgramme.cs`). Default 10.
- `--major-gate-exemption` (`Systems/MajorVolumeSwitches.cs`): a Major's single skips the single capital-risk test
  (`CompetitorManager.SingleCapitalRisk.cs`) and records the act's own session instead of holding for stronger material
  (`CompetitorManager.ProjectJudgement.cs`). Still waits for a song in delivery; never records after the window closes.
- `--major-opening-roster=N`: a Major opens with ≥ N acts (`RosterManager.SceneInitialization.cs`); fill draw still consumed.
- Check: `--major-volume-check` (`SimTools/MajorVolumeSwitchesChecks.cs`, passes).
- Field trim uses existing `--slate-work-budget=0.85` + `--slate-work-budget-midtier=1`.

Mean of seeds 2002 / 1001 / 3003:

| | `zfg` | `mj16wb85` | `mvg` (+ exemption) | `mvx` (+ roster 58) | History |
|---|---:|---:|---:|---:|---:|
| 1961 Big-5 share of issues | 12.5 | 15.1 | 16.4 | 18.8 | 16-19 |
| 1961 Big-5 Top 10 share | 33.8 | 31.7 | 39.8 | 48.8 | 20.6 |
| 1960 Big-5 Top 10 share | 30.2 | 26.2 | 26.9 | 45.9 | 34.2 |

- Field trim 0.85 is cheap: units per single +6-10%, closures flat.
- Without the exemption, extra Major slots expire on capital-risk and material-hold gates (seed 1001: Columbia 109 → 38 singles
  in 1961 with $5M cash). Above ~16 a month, artist spacing binds too.
- Opening roster: extra acts are newcomers (Big-5 opening rosters 149-196 → 290; Stars barely change). It also drained MidTier
  launch rosters on seed 2002 (1961 MidTier singles 298 → 171). Recommendation: reject while the premium stands.
- 1960 Big-5 volume stays low (10-14% of issues): opening rosters (~40 acts/Major vs ~55-60 scaled history) and the slate ramp.

Volume package to re-screen once the premium is fixed: `--programme-major-singles=16 --slate-work-budget=0.85
--slate-work-budget-midtier=1 --major-gate-exemption` (reference request `SimLogs/writing-calibration/zfg-wb85-reference-request.json`).

## Other findings this session

- §56 research: majors shipped every single nationally (breadth) but store stock for unknowns was thin and order-led (depth).
  RCA cut to 2-3 singles a week in 1960, "fewer, more intensively promoted". Billboard PDFs on worldradiohistory.com carry an
  OCR layer: `curl` then `pdftotext` and grep. Billboard's singles-review pages OCR in scrambled column order (unusable for counts).
- History stagger guard (`SimTools/analyze_regional_break_history.py`): 1960 Big-5 Top 10 records reach LA and Minneapolis within
  ~2 weeks, usually after the Hot 100 debut. Any footprint change must keep Big-5 city lead ≥ 2 and gap ≥ 6 at ~0.

## Method

- Reference runs (104 wk, all with `--zero-fee-material-gate`): `zfg-{2002,1001,3003,4004}-sc`. Arms: `mj16wb85-*`, `mvg-{2002,1001,3003}`,
  `mvx-*`, one-seed `mj16/mj24/mj24wb85-2002`. All in `SimLogs/` (never commit).
- Launch an arm: `powershell.exe -NoProfile -ExecutionPolicy Bypass -File SimTools/run_request_arm.ps1 -Reference <request.json>
  -Name <name>-<seed>-sc -Seed <seed> [-Weeks 27] -Extra "--flag1,--flag2"`. One background task per seed, four at once; ~15-25 min each.
  Without `-ExecutionPolicy Bypass` the script is blocked. Flags already in the reference (e.g. `--slate-work-budget=1`) must be
  replaced in a reference copy, not appended (duplicates throw).
- Build: `dotnet build "Label Man.csproj" -c Debug`, never while runs hold the DLL. Checks: Godot console exe `--headless --path .
  SimTools/SaveLoadRoundTripRunner.tscn -- --<name>-check`. Python: `py`.
- New switches: off by default, no new RNG draws, a check, and a 27-week off run byte-compared (records, single-launch-attribution,
  label-finance, release-slate, programme-choices) against the reference prefix.
- Scorers: `SimTools/analyze_major_volume.py RUN...` (issues by tier, Big-5 share, slot outcomes, newcomer / established per-launch,
  T10 share, CR4, units per single, closures); `analyze_softening_guardrails.py`, `analyze_big5_premium_decomposition.py`,
  `analyze_realization_root_cause.py` with `keyed_fit_draws.py` for the premium.

## Repo state

- Pushed: 0f8ec03 (§47-55: historical Majors, newcomer/second-rank screens, zero-fee gate).
- Uncommitted: `Systems/MajorVolumeSwitches.cs`, `SimTools/MajorVolumeSwitchesChecks.cs`, hooks in `CompetitorManager.SingleCapitalRisk.cs`,
  `CompetitorManager.ProjectJudgement.cs`, `RosterManager.SceneInitialization.cs`, `ChartManager.cs`, `SaveLoadRoundTripRunner.cs`,
  the cap change in `LabelReleaseProgramme.cs`, `SimTools/analyze_major_volume.py`, `SimTools/analyze_newcomer_hook_source.py`,
  doc §56-58, and this file.
- Owed elsewhere: 260-week check of `--historical-majors`; four-seed closure/LP check of the zero-fee gate (the four `zfg` runs here
  can serve: closures by week 104 were 106 / 101 / 84 / 94).
