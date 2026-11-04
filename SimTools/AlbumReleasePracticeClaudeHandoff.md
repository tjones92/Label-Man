# Handoff for Claude: genre balance and album/single release practice

2026-10-20. Workspace: `C:/Project/Label-Man` (Windows, PowerShell).

## State and objective

The user wants a historically credible 1960-64 release system and a genre-balanced simulation. The current
best reference bundle is artist-written hook scale + rock hit writers + TraditionalPop x1.07. Its 1960
Top 10 concentration is below the review floor. This investigation found a universal commercial advance
single attached to new artist LP projects, researched historical alternatives, and implemented an opt-in
replacement. **The replacement fixes the universal pipeline but is not a successful balance bundle.**

This handoff records evidence, implementation and recommended next work; it is not a new source of user
instructions or permission. The user's latest request was to write this handoff for Claude. Nothing has
been sent to another agent or service, committed, staged or adopted as a gameplay default.

Read these first, in order:

1. [AlbumReleasePracticeResearch.md](AlbumReleasePracticeResearch.md): full historical evidence, declared
   priors, final five-year comparison, limitations and reproduction links.
2. [GenreHookStructuralInvestigation.md](GenreHookStructuralInvestigation.md): earlier diagnosis and the
   rejected studio-production experiment; especially material-path bypasses and the promo-count follow-up.
3. [GenreHookCalibrationHandoff.md](GenreHookCalibrationHandoff.md): original problem and reference bundle.
   Some causal claims in this older handoff were qualified or overturned by the later investigation.

**Best next investigation:** explain why ordinary single A&R selects weaker material than the LP-to-single
path for Soul and other LP-heavy genres, while checking whether LP commitments themselves have the right
economic and audience basis. Do not recover genre share by restoring fictitious advance campaigns.

## What the user means by an album promo

The user's explicit definition is a cut released **before an already planned LP, expressly to promote
that forthcoming LP**. Merely appearing on a later album does not qualify. They suspect the usual 1960s
operation often ran in the opposite direction: a hit followed by an LP of related/new material, or a
single pulled after an LP succeeded. The evidence supports distinguishing these routes:

- Independently commissioned commercial single; possible later LP inclusion.
- LP packaged around prior hit singles, sometimes given a theme.
- Commercial single extracted after LP release and observed response.
- Deliberately commissioned advance campaign for a planned LP.

A radio service disc, an LP advertised to dealers and a retail/chart-eligible single are different things.
An advance campaign can use an existing composition; it need not be newly written expressly for the LP.

The research indexed twelve contemporary Billboard issues (859 pages) and reviewed targeted relevant
pages, plus artist/label documentation and oral history. Selected famous cases establish mechanisms,
**not representative genre-wide rates**. No defensible national LP denominator with dated advance intent
was found. Do not present the model's percentages as measured history or claim a date when a modern
pipeline became dominant.

Useful primary examples (details/caveats are in the research report):

- [Billboard, August 24, 1963, printed p.3](https://www.worldradiohistory.com/Archive-Billboard/60s/1963/Billboard%201963-08-24.pdf):
  Stevie Wonder's *Fingertips* drawn from an already hot-selling LP; reporting describes LP-after-hit as
  the usual direction. Rendered page checked locally.
- [Apple/Capitol's US 1964 album account](https://www.thebeatles.com/us-1964-albums): LPs assembled from
  existing LP/single material; territory-specific order matters.
- [RCA/Legacy on Elvis's 1960/61 programmes](https://www.elvisthemusic.com/elvis-is-back-legacy-edition-2-cd-set-in-stores-march-1st/):
  original LPs versus contemporary hit singles separately added to reissues.
- [Capitol's Little Deuce Coupe liner notes, reproduced](https://albumlinernotes.com/Little_Deuce_Coupe.html):
  existing automotive tracks and newly recorded material made into a themed album.
- [Billboard, January 13, 1962, printed p.4](https://www.worldradiohistory.com/Archive-Billboard/60s/1962/Billboard%201962-01-13.pdf):
  Red Seal LP merchandising through stocking, ads and displays, not evidence of one retail single per LP.
- Nancy Wilson's 1961 single was motivated by earlier LP response but recorded in a separate session.
  It is **not** counted as an extraction. The report reconciles Capitol trade publicity with Gerald
  Wilson's Smithsonian oral history. Blue Note's *Sidewinder* retrospective establishes an edited
  single but not sufficiently precise advance/after timing. Keep those classification limits.

## Reference, candidate and exact scope

Reference arm `awrw-on`, seeds 1001/2002/3003/4004/5005, 260 weeks:

```text
--artist-written-hook-scale
--rock-hit-writers
--genre-keyframe-scale=TraditionalPop@1960=1.07,TraditionalPop@1962=1.07
```

Common runner flags (also saved in every manifest):

```text
--enable-genre-market-v2 --enable-artist-population-lifecycle
--local-signals-read-demand --regional-arrival --regional-retail-access
--regional-campaign-focus --economic-label-exits --chart-inertia-base=0.4
--small-label-finance --calibration --launch-push-saturation=0.75
--release-judgement-shadow --radio-push-spread=0.1
```

Final candidate arm `apr6-main` adds `--album-release-practice` and the read-only `--genre-hook-audit`.
**Keep `--album-session-production` off in the release-practice reference.** This is the promising
**shared studio formula** experiment: newly recorded LP tracks use the orphan-single studio-production
formula, with stable session variation. Subsequent extractions retain the completed master; they are
not re-recorded or given a genre-specific hook adjustment. Implementation remains in
`Systems/AlbumSessionProduction.cs` and the album-track creation branch of `CompetitorManager.cs`.

It initially produced a good 1960 on the original five seeds: CR8 **48.23** versus 44.12, Big-5 share
**34.73** versus 30.35 (benchmark 34.20), and genre-share error **22.98** versus 25.42. However, its
1961-64 CR8 values were **50.50 / 49.77 / 53.27 / 51.31**, all above the later 38-48 review band.
Its corresponding Big-5 shares were **34.12 / 34.42 / 36.38 / 34.04**, versus benchmarks
**20.60 / 21.20 / 26.00 / 18.70**. The 1960 pass also weakened to CR8 46.81 on ten fresh seeds.

Thus the tested bundle failed adoption, while the shared-formula mechanism remains informative.
These results came from the old universal-promo pipeline; they do not establish the effect of combining
the shared formula with the repaired release routes. Any such combination needs a distinct, explicitly
identified experiment. Full results are in section 7 of `GenreHookStructuralInvestigation.md`.
Neither new experiment is on by default.

The candidate is a bundled structural change, not an isolated advance-rate treatment:

1. Commit the LP on its own component economics before considering a campaign.
2. In singles-oriented markets, usually establish a prior commercial single programme before the first
   LP. The final rule uses **resolved singles**, not national pop-chart entries, with exceptions for
   original LP programmes. Existing hit inventory still informs LP forecasts.
3. After recording the LP, consider its actual best unissued master. Only Standard/Concept programmes
   are eligible for generic advances. No generic Classical or NonMusic advance/extraction.
4. Commission with a selective prior and require positive incremental economics. The central prior is
   `.15 * existing SingleOrientation * institution factor`, bounded to [0,.45]. Institution factor:
   Classical/NonMusic 0; Jazz/Gospel .25; Blues/Folk .5; other families 1. No year or Major-label multiplier.
5. Forecast actual master quality, capped LP awareness lift, LP-demand-scale diversion, 15% recording-cost
   preparation, campaign cost, LP delay cost and alternative singles opportunity. Remove synthetic free
   awareness dollars and duplicate recruitment credit from this candidate's campaign comparison.
6. LP-only commitments preserve a bounded independently due singles slot for the **same act**. The normal
   single path can record new material at full cost or select an eligible older LP cut. It cannot extract
   from the just-committed LP, and cannot recursively initiate another LP.
7. A later extraction consumes an ordinary singles opportunity: LP age 8-52 weeks, observed peak 1-40,
   unissued master, positive economics and cash preparation floor, at most one later cut per LP. It retains
   master/song/hook/production identity, source LP, and a separate estimator lane. No new recording roll
   and no advance-awareness bonus. An active cut can substitute for LP demand.

The .15 prior coefficient, family factors, thresholds, .35 later-cut opportunity, 15% preparation cost
and 10% annual delay-cost assumption are **declared modelling priors**, not historical estimates.

Sensitivity/debug switches:

- `--album-promo-campaign-scale=0|0.5|1|2` (finite 0-3 accepted).
- `--album-practice-coupled-calendar`: disable the extra independent opportunity for an ablation.
- `--album-practice-album-first`: disable the prior-single-programme hurdle for an ablation.

## Completed results: do not mistake a route fix for a balance win

All values below are five-seed means. Shares/concentration are percentages; errors are sums of absolute
differences against the project's existing benchmark inputs. CR8 is the eight largest labels' share of
Top 10 slot-weeks, not sales concentration. Review band: 48-58 in 1960, 38-48 in later years.

| Measure | Reference / candidate | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---|---:|---:|---:|---:|---:|
| CR8 | Reference | 44.12 | 40.23 | 39.04 | 42.00 | 37.62 |
| | Candidate | 42.73 | 39.88 | 34.81 | 36.31 | 39.46 |
| Big-5 Top 10 share | Reference | 30.35 | 25.58 | 26.54 | 26.85 | 22.08 |
| | Candidate | 25.19 | 22.92 | 19.85 | 22.19 | 18.19 |
| | Project historical target | 34.20 | 20.60 | 21.20 | 26.00 | 18.70 |
| Soul combined units share | Reference | 7.16 | 7.54 | 8.69 | 9.69 | 10.17 |
| | Candidate | 5.22 | 6.34 | 6.35 | 7.26 | 7.87 |
| | Project historical target | 7.92 | 8.86 | 10.24 | 11.53 | 12.20 |
| Genre-share error | Reference | 25.42 | 22.62 | 22.16 | 23.42 | 28.86 |
| | Candidate | 30.86 | 28.72 | 29.57 | 27.06 | 33.50 |
| Year-end slot error | Reference | 32.00 | 39.00 | 38.40 | 45.40 | 35.00 |
| | Candidate | 36.80 | 37.80 | 43.60 | 56.20 | 43.00 |

1960 paired changes (candidate minus reference): CR8 -1.38 with seed-level SE 2.00; Big-5 -5.15
with SE 1.75; Soul share -1.93 with SE .44. Do not claim a precisely established CR8 penalty. All five
candidate opening CR8 values are below 48. Later Big-5 proximity improves in some years, not all.

Across all five seeds and five years:

| Release measure | Reference | Candidate |
|---|---:|---:|
| New artist LP commitments | 63,354 | 62,761 |
| Advance commercial singles | 63,354 | 1,248 |
| Advances / LP commitments | 100% | 1.99% |
| Later extractions | 0 | 785 |
| Independent singles | 74,126 | 110,509 |
| All single launches | 137,480 | 112,542 |
| Classical LP commitments | 2,169 | 2,623 |
| Classical advances / later cuts | 2,169 / 0 | 0 / 0 |

These are artist-project counts, not all album categories in the entire simulated market. Independent
Classical crossover singles are still possible. Do not describe zero LP cuts as zero Classical singles.

1960 reference TraditionalPop: 1,268 advances across the five seeds, or 51.46% of all its single launches;
TeenPop 7.60%, RockAndRoll 6.26%. Candidate TraditionalPop: 1,361 LP projects, 39 advances, 40 later cuts,
1,713 independent singles; advances are 2.87% of its LP commitments and 2.18% of its singles. It still
commits more LPs than those youth genres; the research does not validate that residual LP count.

Macro sales survive the route change. Mean 1960 singles: 158.17M -> 155.11M (-1.94%); LPs:
37.60M -> 41.77M (+11.10%). By 1964 both formats' total units are nearly equal across arms.
Combined genre share mixes singles and LP units; it is not a measure of single-chart conversion.

Independent opening seeds 6006/7007 (`apr6-opening`, 52 weeks): CR8 46.44 -> 47.50, but genre-share
error 24.53 -> 30.65 and Soul 6.51 -> 4.83. Two seeds do not establish an opening pass. Low/high
commission-scale screens on seed 6006 give .90%/3.47% advances per LP, versus 2.04% centrally.
The zero-advance seed-6006 case has zero advances, 41 later cuts and 155.27M single units versus
160.00M reference: useful opening-year evidence against an unavoidable macro sales collapse, not
proof of historical zero or later-decade viability.

## Diagnosis and limitations Claude should carry forward

**Material selection is the strongest lead, not yet a fully isolated cause.** Untouched seed 6006 Soul:
280 LP promos with mean hook .788, versus 123 independent singles at .623. Final candidate: 358
independent singles at .595, eleven advances at .761 and one later cut at .844. Cohort, artist and
equilibrium changes confound the raw comparison. The code does establish the route difference: the LP
records a batch of tracks and selects its best cut; ordinary single A&R uses a different selection path.

Earlier investigation also established:

- Live polar selection replaces candidate scores with `SourceMix * SelectionScore(fit)`.
  Several tempting legacy access/need/penalty knobs do not control live relative preference.
- Professional availability is effectively an admission cutoff at .01 in the builder, not a fractional
  supply multiplier over ordinary positive values.
- Modern realization returns before the legacy expected-hook/professional-polish blend. Adjusting its
  bypassed terms will not deliver the advertised hook change.
- Apparent genre production penalties were predominantly orphan-versus-LP recording-path mixtures.
  Re-realizing a cut after extraction would break master identity rather than repair selection.
- The old 48.15 five-seed control was unusually favorable: broader fifteen-seed mean was 45.92;
  reference candidate 44.82. The original four-point concentration loss was not robustly isolated.
- Hand-counted year-end genre slot targets overlap and sum to 110/105/110/107/99 for 1960-64.
  Raw error cannot attain zero in every year. Do not fit a mutually exclusive output to overlapping
  counts as if they were exact constraints; retain share, format and chart diagnostics separately.

Known candidate limitations before adoption:

1. The extra singles slot is not two persistent artist release/session clocks. A new LP and independent
   single can ship in the same week, followed by the usual shared artist cooldown.
2. The prior-single hurdle reads resolved masters. A seeded public discography without reusable masters
   can be mistaken for no prior single programme. Separate **known audience/history** from **reusable
   hit-master inventory**; do not require a national pop hit to establish a specialist act.
3. Top-40 LP success and age thresholds can exclude successful niche records. Later extraction is sparse
   and conservative; its exact rates are not independently validated against a release census.
4. Candidate-versus-reference combines LP origination, opportunity allocation, campaign probability,
   costs and forecasts. It cannot isolate an advance-rate-only causal effect.
5. The same rule continues after 1964, but no final-candidate 1965-69/decade adoption validation was run.
6. One unrelated analysis caveat: per-genre summaries in `analyze_genre_hook_calibration.py` are emitted
   for genres appearing on the national single chart. Check seed counts before using sparse-genre
   averages (e.g. Classical) as all-seed format-share estimates. Focal Soul/TradPop/R&R rows have all five.

## Recommended next work, in order

1. Inspect the working diff and the two main implementation files before editing. Preserve the tested
   `apr6` candidate as a named control; use new opt-in flags/unique run prefixes for further treatments.
2. Trace ordinary singles versus LP-track material choice in `SongMaterialSelectionService.cs`,
   `SongMaterialApplicationService.cs` and album creation/promo selection in `CompetitorManager.cs`.
   Compare candidate-pool sizes, source eligibility, fit scoring, rejection/retry behavior and artist/
   publisher access. Record composition hook, selected fit, execution and source separately. Use stable
   read-only diagnostics without extra global random draws. Distinguish source mix from selection
   within a source, and best-of-many selection from intrinsic writer quality.
3. Test a concrete economic A&R correction only after that trace. A label shopping several songs for a
   single may be credible; giving every single an LP-sized free pool or a blanket Soul hook bonus is
   not established by this investigation. Account for search/session cost and actual talent/material
   supply. Keep LP cuts' master identity intact. Use matched artists/opportunities where possible;
   raw cohort hook averages alone cannot identify the treatment.
4. Audit LP commitment incentives and opening public-history recognition. Inspect `DecideRelease`,
   `ResolveHitInventory`, component responsive memory and the physical LP forecast. Distinguish hit-based
   packaging from original/concept LPs. Do not replace a national-chart gate with another niche bias.
5. If the bounded opportunity fork materially drives results, replace it with explicit, funded album
   and single commitments/cooldowns. Ablate separately from material selection so the mechanism remains
   interpretable. Existing coupled-calendar/album-first switches can help, but intermediate screens are
   not already proof for a new final policy.
6. Screen narrowly on paired seeds with route counts, format-unit shares, costs and material provenance;
   then confirm a promising treatment across five full 260-week seeds and fresh opening holdouts.
   Evaluate CR8/Big-5, whole-genre share error, slot error with its mapping caveat, sales, margins, label
   survival and artist cadence together. A 1960 CR8 gain alone is not sufficient.
7. Before any adoption, repair opening-history/calendar limitations, validate the off boundary and
   invariants, and perform later-decade checks. Update the report around the final implementation and
   publish negative results plainly. No balanced replacement has yet been established.

## Code and evidence map

| File | Purpose |
|---|---|
| `Systems/AlbumReleasePractice.cs` | Flag/live guard, declared priors, eligibility, deterministic domain-separated draws, incremental economics |
| `Systems/CompetitorManager.AlbumReleasePractice.cs` | Actual-master campaign forecast, later extraction, independent opportunity and shared best-track selector |
| `Systems/CompetitorManager.cs` | LP commitment/single integration, source-cut cost and estimator roles |
| `Systems/AlbumSimulator.cs` | Bounded LP diversion from active later cuts |
| `Data/AlbumProject.cs`, `Data/AILabel.cs` | Appended extraction strategy/role/estimator enum values; existing ordinals preserved |
| `Data/Record.cs`, `Data/Album.cs` | Source LP and durable post-extraction ID provenance |
| `SimTools/ChartAuditRunner.AlbumPractice.cs` | Decision/component/chronology audit |
| `SimTools/ChartAuditRunner.GenreHook.cs` | Read-only material/format audit; source/master fields added only with candidate enabled |
| `SimTools/AlbumReleasePracticeChecks.cs` / `.tscn` | 36 focused eligibility/economics/RNG/estimator checks |
| `SimTools/analyze_album_release_practice.py` | Route counts and committed-campaign/later-cut/master validation |
| `SimTools/analyze_genre_hook_calibration.py` | Per-seed market/chart errors, paired deltas, format reconciliation |
| `SimTools/validate_genre_hook_audit.py` | Disabled replay byte comparison and launch/unit reconciliation |
| `SimTools/run_genre_hook_experiments.py` | Explicit bounded plans, assembly hash, flags, completion/error checks |
| `SimTools/research_album_promos.py`, `plot_album_release_practice.py` | Corpus index and saved-result comparison plot |

Critical files under ignored `SimLogs/`:

- `album-practice-final-plan.json` and `album-practice-final-plan-runs.json`: ten successful final runs
  (five 260-week runs, four opening/sensitivity runs, one disabled replay).
- `album-practice-fiveyear-{seeds,summary,paired,cohorts}.csv`: final market analysis.
- `album-practice-fiveyear-routes.csv`, `album-practice-fiveyear-checks.json`: five final candidate
  validations pass. Reference `validated:false` means the older runs lack the new master audit, not a
  failed candidate invariant.
- `album-practice-final-opening-*`: final independent opening/sensitivity outputs.
- `album-practice-zero-plan{,-runs}.json`: completed zero-advance sensitivity run.
- `album-practice-final-off-identity.json`: all 86 CSVs byte-identical, including earlier read-only audits.
- `album-practice-checks-console.log`: `ALBUM_RELEASE_PRACTICE_CHECKS_PASS checks=36`.
- `album-practice-calibration.png`: visually checked five-seed comparison plot.
- `album-promo-research/manifest.json`: PDF URLs/dates/hashes and explicit failed download; rendered
  evidence pages and indexed text in the same folder.

Run prefixes: reference `awrw-on-{seed}-260`; final `apr6-main-{seed}-260`; fresh opening
`apr6-opening-{6006|7007}-52`; commissioning `apr6-low-6006-52`, `apr6-high-6006-52`,
`apr6-zero-6006-52`; final disabled `apr6-off-6006-52`. Fresh same-seed reference openings are
`ghc2-base-{6006|7007}-52`; audited off reference `ghc3-audit-6006-52`.

Intermediate `apr1` through `apr5` runs are diagnostic, not final-policy observations. In particular,
`apr1` had a week-one live-guard leak; release decisions precede the chart clock on the first tick.
The final guard uses the competitor pipeline clock while excluding prewarm. The `apr4`/`apr5` prior-chart
gate was rejected for niche bias; four `apr5-main` runs were stopped early and are not completed data.
The `album-practice-confirmation-plan-runs.json` manifest contains only its four completed short runs.

## Runtime, verification and workspace hygiene

Latest build: `dotnet build 'Label Man.sln' --no-restore -v minimal`, zero warnings/errors. Python modules
compile; `git diff --check` clean. Five final full candidate runs pass chronology/economics/master checks.
The final source change after those simulations only corrected a comment describing the independent
slot; the project was rebuilt afterward. Run manifests retain the exact simulation assembly hashes.

Python:
`C:/Users/grohl/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe`

Godot:
`C:/Users/grohl/Downloads/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64/Godot_v4.7-stable_mono_win64_console.exe`

Native Godot crashed in the tool sandbox; successful runs used the permitted escalated execution path.
The runner limits concurrency to four, hides native windows, refuses existing prefixes/manifests and
checks the assembly hash throughout. **Do not rebuild while a plan is running.** Use fresh arm/plan names;
retain completion evidence and exclude partial runs. Runtime is several minutes per 260-week run,
longer with four concurrent jobs. No run was knowingly left active at the end of this work.

Reanalyse without new simulations (PowerShell, project root):

```powershell
$handoffPython = 'C:/Users/grohl/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'
& $handoffPython SimTools/analyze_genre_hook_calibration.py --tags awrw-on apr6-main --seeds 1001 2002 3003 4004 5005 --weeks 260 --output SimLogs/album-practice-fiveyear
$handoffPrefixes = foreach ($tag in 'awrw-on', 'apr6-main') {
    foreach ($seed in 1001, 2002, 3003, 4004, 5005) { "SimLogs/$tag-$seed-260" }
}
& $handoffPython SimTools/analyze_album_release_practice.py @handoffPrefixes --output SimLogs/album-practice-fiveyear
& $handoffPython SimTools/validate_genre_hook_audit.py SimLogs/ghc3-audit-6006-52 SimLogs/apr6-off-6006-52 --output SimLogs/album-practice-final-off-identity.json
```

Matplotlib is installed in ignored `SimLogs/album-promo-research/python-libs`; add that directory to
`PYTHONPATH` to regenerate the plot. Logs, PDFs, plotting dependencies and charts are ignored artifacts:
they are available in this workspace but will need deliberate transfer to another machine.

The working tree contains uncommitted tracked changes and untracked source/report/tool files from this
investigation and the preceding structural investigation. Preserve them. `SimTools/CounterRngTests/bin/`
and `obj/` were already untracked before this work and were not touched. No branch or commit was created,
and no user changes were reverted. Inspect current status before cleaning or selecting a commit scope.
