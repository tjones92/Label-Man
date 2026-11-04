# Genre hook calibration: structural investigation

2026-10-19. This records an investigation of the hook-scale + rock-hit-writers + TraditionalPop x1.07 candidate.
The supplied handoff and earlier directives were treated as evidence and hypotheses, not additional instructions.
All gameplay defaults remain unchanged. The new recording change is an opt-in experiment.

## Findings

The stalled calibration has three different causes: some apparent levers are bypassed in the live material path;
the production gap attributed to genres mostly comes from different recording paths; and small concentration
changes are being judged against an unusually favorable five-seed control. These need separating before further
genre-specific tuning.

**Decision: reject the studio experiment as an adoption bundle.** Its apparent 1960 pass on the original five
seeds does not reproduce on the independent ten, and the five-year screen raises later Big-5 shares sharply.
The existing candidate remains the reference; this investigation does not establish a new bundle meeting all
the requested guards. The strongest next lead is the effectively universal commercial promo attached to artist
LP projects, detailed in the follow-up section. Further hook tuning would conceal that allocation problem.

## 1. The old control is not a stable 48.1 reference

Twenty fresh 52-week simulations compared the old control with the existing candidate on ten previously unused
seeds: 6006, 7007, ..., 15015. Same F=0.75, radio-push-spread=0.1 and other flags as the handoff. Every run
completed normally. Original five-seed metrics were independently recomputed from the saved CSVs.

| 1960 measure | Original five: control / candidate | New ten: control / candidate | All fifteen: control / candidate |
|---|---:|---:|---:|
| Top 10 CR8 | 48.15 / 44.12 | 44.81 / 45.17 | 45.92 / 44.82 |
| Big-5 Top 10 share | 33.81 / 30.35 | 30.13 / 29.33 | 31.36 / 29.67 |

Across all fifteen seeds, the candidate-control CR8 difference is -1.10 percentage points with paired SE 2.00.
The original five gave -4.04 with SE 4.73; the new ten give +0.37 with SE 1.90. The sign does not replicate.
This does not establish equivalence, but it does overturn the claim that the four-point loss has been isolated
as a hook-scale effect. The candidate still misses the design floor; the control's broader mean also misses it.

The historical band is a design review band for mean outcomes, not a confidence interval or a requirement for
each individual seed. These statements use seed-level uncertainty, not thousands of correlated record-weeks as
independent observations.

Evidence: `SimLogs/genre-hook-opening2-plan{,-runs}.json`,
`genre-hook-opening-holdout-{seeds,summary,paired}.csv`, `genre-hook-investigation-{seeds,summary,paired}.csv`.

## 2. Several plausible material knobs do not control the live choice

The current normal path uses `PolarSongBehavior.UsePolarFitSelection`.

| Input | Actual effect in that path |
|---|---|
| Professional availability | `BuildProfessional` checks only `availability <= .01`. Values .60 and .30 both leave the pool open. There is no fractional availability multiplier in the live choice. |
| Professional source anchor | Active: `SourceMix` is multiplied by the selected candidate's polar fit score. |
| `labelAccess`, `artistNeedsSong`, `commercialNeed`, `ExternalPenalty` in the professional builder | They contribute to its legacy score. `ChooseMaterial` replaces that score with `SourceMix * SelectionScore(fit)` after candidate admission. Ordinary positive changes therefore do not produce the advertised economic preference. |
| Professional `ExpectedHook` and polish bonus | The modern recording branch uses the raw composition hook, performance hook and execution factor, then returns before the legacy expected-hook/polish blend. |
| Publisher pitch slicing | Active: it changes the actual professional candidate pool. It also sorts/reindexes that pool, so a treatment changes sampled song identities as well as quality coverage. |
| Radio acceptance | Active, but its early chart leverage is limited by sales and it has shared-segment feedback. |

Consequently the measured Major/Independent professional-hook difference cannot simply be attributed to
`labelAccess * polish * .10`. That direct arithmetic is bypassed. Act composition, capability and candidate fit
can still produce a tier difference.

Sources: `Systems/SongMaterialSelectionService.cs`, `Systems/SongMaterialApplicationService.cs`,
`Systems/PublisherPitchOrder.cs`. This investigation does not reactivate all of the legacy score factors: doing
that would be a different behavioral change requiring its own design and validation.

## 3. The alleged genre execution tax is predominantly a format/path mixture

On the existing candidate, Independent singles in the original five 260-week runs have these means:

| Genre | Orphan production | Album-promo production |
|---|---:|---:|
| Soul | .681 | .532 |
| TraditionalPop | .670 | .538 |
| RockAndRoll | .669 | .484 |
| TeenPop | .670 | .490 |

Soul's orphan singles do not have the alleged production deficit. R&R and TeenPop actually have *lower* promo
production than Soul. Their higher aggregate production comes from issuing many more orphan singles. Genre-only
averages confound the release path with genre.

The new read-only audit also records the committed fit and actual execution factor. In the untouched candidate,
seed 6006, Independent Soul execution averages .9989 on promos and .9995 on orphans; R&R averages .9990 and .9972.
A 10-16% aggregate production difference cannot be explained by those execution factors. This is a one-seed
mechanism check, supported by the five-seed path-stratified production comparison, not a claim that all capability
penalties everywhere are negligible.

The code explains the gap:

- Orphan production = .4 label craft + .3 performer studio skill + .2 regional studio modifier + session noise.
- Album tracks start production from their latent track appeal plus title-derived noise. That appeal includes
  format/filler scaling and track-spread assumptions. It does not use the orphan's studio formula.
- Extracting a promo correctly reuses its recorded album master. It inherits the production deficit; it does
  not receive another studio session or a second execution adjustment.

The repair candidate therefore acts at album-track recording, before the shared material realization. Boosting
the promo after extraction would break master identity and would hide the actual source of the discrepancy.

Evidence: `SimLogs/genre-hook-investigation-cohorts.csv`,
`genre-hook-audit-identity-execution.csv`, `Systems/CompetitorManager.cs`,
`Systems/SongMaterialApplicationService.cs`.

## 4. Combined market share does not identify singles chart conversion

`genre-decade-shape.csv` accumulates commercial units from **all record formats**. The Hot 100 uses singles.
The distinction is real in these runs, not merely terminology. In the untouched seed-6006 candidate in 1960:

| Genre | Share of single units | Share of album units |
|---|---:|---:|
| RockAndRoll | 20.38% | 2.12% |
| TeenPop | 22.41% | 1.73% |
| TraditionalPop | 12.19% | 23.97% |
| Soul | 5.78% | 6.10% |

Thus, matching a combined units target while taking more chart slots is not sufficient evidence for a chart-only
defect. First separate singles versus LP allocation, then hit conversion conditional on the single, then chart
duration. The new `--genre-hook-audit` writes annual physical-format units using the exact records and canonical
genre mapping used by the existing genre total. The validator checks that they reconcile exactly.

The target workbook is described in the older calibration handoff as historically reasoned estimates of the
record market, not audited statistics. Keep its broad shape and use it to discipline demand; do not infer precise
single-side elasticities from its combined annual cells.

## 5. Why the isolated 1960 R&R cut backfires

`GenreSupplyService.GetProjectIdentityRetention` divides the current baseline by
`profile.BaselineKeyframes.Max()`. It also applies an extra decline multiplier whenever the annual slope is
negative. A keyframe therefore changes both audience quantity and the probability an act retains that identity.

For the rejected 1960-only x.92 arm:

| Quantity, at 1962 | Existing candidate | 1960-only cut |
|---|---:|---:|
| 1960 baseline | .450 | .414 |
| Maximum keyframe | .450 | .420 |
| Current baseline / maximum | .9333 | 1.0000 |
| One-year slope | -.015 | +.003 |
| Retention before artist-history bonus | .7309 | .8200 |

That is a 12.2% relative increase in the base retention probability at 1962 despite leaving the 1962 keyframe
unchanged. The retained-identity bonus is bounded and clips for experienced acts, so this is not a universal
12.2% increase in releases. It identifies a real direction of feedback, consistent with the saved late-year
supply increase. In the flat 1963-64 stretch, the base retention rises from .78 to .82.

Trimming 1960 and 1962 together does not generally eliminate this coupling: a later unchanged keyframe can
become the maximum. Nor is baseline-squared a universal local law here: `GenreCatalog.cs` already records an
early R&R exponent around 1.15 in a previous configuration. Re-estimate local responses after structural fixes;
do not repeatedly apply sqrt(target/current) as if retention, format allocation and competition were fixed.

## 6. Publisher history does not justify a universal advantage that expires in 1961

Professional rock writers are historically well supported. A universal major-label quality partition is much
harder to defend. Leiber/Stoller's own production list documents Atco, Atlantic and Big Top placements before
1960 as well as Scepter/Wand placements later. Independent access did not begin at a single 1961 boundary.
[Leiber & Stoller production discography](https://www.leiberstoller.com/Productions.html).

The Elvis/Hill & Range arrangement is evidence for an act/publisher relationship, not a rule giving every major's
unknown act first access to every publisher's best songs. [Graceland's chronology](https://www.graceland.com/1954-1957)
records the separate Elvis Presley Music agreement alongside the RCA signing.

The local historical Top 10 reconstruction also offers a simpler warning about overfitting the 1960-61 drop.
Big-5 slot-weeks fall from 178 to 107. Five credited acts—Johnny Preston, Jim Reeves, Percy Faith's orchestra,
the Brothers Four and Dinah Washington/Brook Benton—account for 57 slots in 1960 and zero in 1961. That descriptive
gross loss equals 80% of the net decline, before gains by other acts. This is not a causal decomposition of
publisher access. It shows how much of a single-year concentration movement can come from the particular hit
roster. Artist credit variants also need canonicalization for broader act-return analysis.

The untouched candidate's new audit shows the current pool is not a scarce, exclusively allocated resource:
one professional R&R song reaches 36 different labels in seed 6006's 1960 launches, and one Soul composition
reaches 107. The existing slice probe partitions a reusable catalogue by true hook. Its successful head effect
does not validate its institutional explanation. A future access model should distinguish publisher relationships,
first recording opportunities and competing versions, including independent hit factories.

## 7. Studio-production experiment

New flag: `--album-session-production`, off by default. Implementation:
`Systems/AlbumSessionProduction.cs` and the album-track creation branch in `Systems/CompetitorManager.cs`.

It gives newly recorded album tracks the orphan studio formula. Session variance uses a stable album-ID/slot
hash in the same [-.05, .10] range, consuming no additional global RNG draws. It updates latent track appeal by
the existing recording production weight (.30) times the production delta, then leaves the existing composition
selection and capability realization in place. Promo extraction reuses the completed master. There is no genre
term, date curve, major-label exception, or blanket hook cap.

All other candidate flags and keyframes are unchanged.

### Opening screen: original five seeds, 1960

| Measure | Existing candidate | Studio experiment | Historical reference |
|---|---:|---:|---:|
| Top 10 CR8 | 44.12 | 48.23 | 48-58 design band |
| Big-5 Top 10 share | 30.35 | 34.73 | 34.2 |
| R&R combined units share | 15.97 | 13.80 | 13.54 |
| TraditionalPop combined units share | 13.37 | 14.94 | 15.00 |
| Soul combined units share | 7.16 | 7.30 | 7.92 |
| Total absolute share error | 25.42 | 22.98 | lower is better |

Paired SEs are 2.79 for CR8, 2.69 for Big-5 share, .52 for R&R share and .84 for TraditionalPop share. The genre
quantity response is clearer than the concentration response. CR8 improves in four of five seeds; one seed
still has CR8 38.27. Independent holdouts are essential.

### Completed five-year confirmation

The five 260-week studio runs, one 260-week byte-identity reference, and ten independent 52-week studio holdouts
all completed normally using `SimLogs/genre-hook-studio-confirm-plan.json`.

| Measure | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---:|---:|---:|---:|---:|
| Existing candidate CR8 | 44.12 | 40.23 | 39.04 | 42.00 | 37.62 |
| Studio CR8 | 48.23 | 50.50 | 49.77 | 53.27 | 51.31 |
| Existing candidate Big-5 Top 10 | 30.35 | 25.58 | 26.54 | 26.85 | 22.08 |
| Studio Big-5 Top 10 | 34.73 | 34.12 | 34.42 | 36.38 | 34.04 |
| Historical Big-5 Top 10 | 34.2 | 20.6 | 21.2 | 26.0 | 18.7 |
| Studio minus candidate Big-5, paired SE | 4.38 (2.69) | 8.54 (1.05) | 7.88 (4.22) | 9.54 (2.62) | 11.96 (1.74) |
| Studio R&R combined share | 13.80 | 13.14 | 11.10 | 9.42 | 7.79 |
| Studio TraditionalPop combined share | 14.94 | 14.15 | 14.35 | 12.81 | 10.78 |
| Studio Soul combined share | 7.30 | 8.38 | 8.88 | 10.43 | 10.70 |

The later R&R gains are retained, and TraditionalPop's combined quantity is much better. But every later year's
Big-5 mean moves further above history, and every later year's CR8 exceeds its 38-48 band. This fails the central
ownership guard. It also materially changes the economy: 1960 LP units rise 22.2%, while single units fall 2.5%.
Neither arm has a Big-5 demotion, but preserving those firms' survival does not establish economic identity.

### Independent opening-year check

| 1960 measure | Existing candidate, new ten | Studio, new ten | Existing candidate, all fifteen | Studio, all fifteen |
|---|---:|---:|---:|---:|
| CR8 | 45.17 | 46.81 | 44.82 | 47.28 |
| Big-5 Top 10 share | 29.33 | 31.10 | 29.67 | 32.31 |
| R&R combined share | 14.99 | 14.14 | 15.32 | 14.02 |
| TraditionalPop combined share | 13.70 | 14.67 | 13.59 | 14.76 |
| Soul combined share | 6.67 | 7.99 | 6.83 | 7.76 |
| Total absolute share error | 25.13 | 25.90 | 25.23 | 24.93 |

The studio CR8 mean is still below 48 on both the independent and pooled samples. The original five-seed
pass was fragile. The three focal genre quantities improve across the larger sample, but whole-catalog share
error barely changes. This is useful mechanism evidence, not a confirmed overall calibration win.

Evidence: `SimLogs/genre-hook-studio-confirm-{seeds,summary,paired}.csv`,
`genre-hook-studio-holdout-{seeds,summary,paired}.csv`, `genre-hook-studio-pooled-summary.csv`.

## 8. Validation and measurement cautions

- `dotnet build 'Label Man.sln' --no-restore`: zero warnings and errors.
- New code with both new switches absent: 84/84 existing CSVs byte-identical to the pre-edit seed-6006 candidate.
- Audit enabled, studio switch absent: the same 84/84 existing CSVs remain byte-identical; only two audit CSVs
  are added. Their annual format units, launch identities, hooks and production reconcile to existing outputs.
- A 260-week seed-1001 audit also matches the original candidate in all 84 existing CSVs. Its 28,825 launch
  identities and quality values reconcile, as do its annual format units.
- All 43 new simulations completed normally: twenty opening-year control/candidate runs, five studio opening
  screens, two opening identity runs, six five-year runs, and ten independent studio opening holdouts.
- A sandboxed Godot startup crashed before simulation. Fresh runs outside that restriction completed; the failed
  startup outputs are excluded. No failed run or partial year is used as a completed observation.

The hand-counted slot reference is overlapping, not a mutually exclusive histogram. Its live table sums to
110 / 105 / 110 / 107 / 99 for 1960-64, whereas the simulated recap has exactly 100 slots. Raw absolute slot error
has a nonzero mathematical lower bound even before classification disagreement. The older handoff documents
double-tagging. The analyzer retains raw error for continuity and adds a normalized sensitivity measure; the
normalized version is not promoted to historical truth. A record-level fractional or adjudicated mapping is the
proper comparison if individual genre slot differences are to become tight constraints.

No decade adoption run was launched: this studio bundle already fails the five-year ownership guard and the
independent opening-year floor. A future repaired bundle still needs late-decade validation before adoption.

## Reproduction tools

- `SimTools/analyze_genre_hook_calibration.py`: complete-year checks, seed means and paired SEs, startup
  decomposition, raw/normalized slot sensitivity, and format shares when audit files exist.
- `SimTools/validate_genre_hook_audit.py`: all-CSV hash boundary, format-unit reconciliation, committed execution
  and professional-composition reuse.
- `SimTools/run_genre_hook_experiments.py`: explicit plans, bounded concurrent jobs, exact flags and assembly
  hashes, no overwrites, nonzero-exit/native-error/completion checks. Plans and generated evidence stay in SimLogs.

The original candidate's TraditionalPop result should also be corrected in any subsequent handoff: with rock
writers included it is 13.37 / 13.48 / 12.60 / 11.74 / 10.29, not the earlier no-writers arm's near-target series.

## Follow-up: how often TraditionalPop launches album promos

The user's follow-up asked whether the apparent rescue through TraditionalPop album promos was itself
historically plausible. The counts below use the **existing candidate**, before the studio experiment, across
all five complete seeds. Each entry is mean commercial promo singles per simulated year, followed by their
percentage of all that genre's single launches.

| Genre | 1960 | 1961 | 1962 | 1963 | 1964 |
|---|---:|---:|---:|---:|---:|
| TraditionalPop | 253.6 (51.5%) | 277.2 (55.2%) | 314.0 (65.9%) | 313.8 (72.4%) | 339.0 (84.2%) |
| TeenPop | 44.8 (7.6%) | 53.8 (9.3%) | 73.6 (13.2%) | 87.0 (17.3%) | 128.2 (25.5%) |
| RockAndRoll | 57.6 (6.3%) | 68.4 (8.1%) | 94.8 (13.1%) | 99.0 (17.1%) | 150.8 (27.5%) |
| RnB | 48.4 (7.3%) | 62.4 (8.7%) | 95.4 (12.9%) | 116.8 (20.4%) | 156.6 (29.1%) |
| DooWop | 21.4 (7.5%) | 21.8 (7.9%) | 19.8 (8.8%) | 24.4 (12.7%) | 20.6 (17.5%) |
| SurfRock | 4.6 (35.9%) | 15.0 (36.6%) | 48.8 (53.4%) | 109.0 (62.6%) | 150.8 (76.9%) |
| BritishBeat | — | — | — | 4.6 (67.6%) | 179.4 (83.6%) |

TraditionalPop has fewer total single launches than TeenPop in 1960 (492.8 versus 589.8), but 5.7 times as many
album-derived single launches. The distinction is not simply adult versus youth: BritishBeat and SurfRock also
become strongly album-derived.

There are **63,354 logged AI artist AlbumWithPromo projects and zero AlbumStandalone projects** in these runs.
This includes 2,169 Classical album projects. External film/cast album origination is a separate path; this count
must not be described as every album in the entire market. Nor is a sim promo merely an LP track sent to radio:
it is a separately sold, stocked, chart-eligible commercial single, emitted before the LP.

This outcome is not a hardcoded prohibition on LP-only releases. It is a dominance problem in the current
economic comparison. The format decision precedes song selection. Its incremental promo forecast is:

```
fixed awareness lift + modelled recruited LP contribution + predicted single contribution
    - explicitly charged LP cannibalization
```

The awareness term is `(1 - projectedLaunchAwareness) * 10000`, in money added directly to the comparison. A
separate recruitment term uses `PromoAlbumConversionK = 1.50`, LP demand, awareness headroom with a .25 floor,
expected single units and LP margin. The source comments explicitly document calibrating the recruitment
coefficient against late-decade Major chart-entry share. These terms can motivate a commercial single before
any particular album track has been assessed for that use.

For selected TraditionalPop promo projects, mean predicted extra promo contribution rises from $27,324 in
1960 to $47,736 in 1964. The fixed awareness term alone contributes approximately $8,000 in each year.

However, this cannot be dismissed as forecast optimism alone. All 1,268 TraditionalPop promos launched in 1960
have closed by the end of the five-year observation: mean forecast single contribution is $9,717, realized mean
is $11,679, and 1,253 have positive booked contribution. The corresponding 1961 and 1962 realized means are
$9,611 and $9,253. These figures exclude marketing and the counterfactual LP externality; they do not establish
the profitability of the entire promotional decision. They do show that simply making beliefs more pessimistic
would fight actual commercial incentives in this simulated world.

The studio experiment's 1960 Big-5 gain decomposes into +40.6 promo slot-weeks and -17.8 orphan slot-weeks per
seed; carryover contributes zero net change. TraditionalPop promos alone add 24.8 Big-5 slot-weeks. Improving
production on an effectively universal LP-to-single pipeline therefore exposes a broader allocation problem.
The observed path difference does **not**, by itself, establish that every LP track deserves a free quality
upgrade at the existing project cost.

Historically, adult-pop LP and single catalogues were related without being mechanically identical. The label's
own Mathis documentation distinguishes the ten Global/Mercury LPs of 1963-67 from non-album singles collected
separately. [Legacy Recordings' release documentation](https://www.prnewswire.com/news-releases/johnny-mathis-set-to-release-the-complete-global-albums-collection-on-november-17-274897031.html).
This supports separate routes; it does not supply a numerical genre-wide extraction rate. A song appearing on
a compilation later also does not make the original single an LP-launch promo.

The next investigation should give LP-only projects, independent single releases, and selectively chosen LP
extractions a meaningful economic comparison, with separate recording/release commitments. It should distinguish
trade promotion from a commercial single and evaluate incremental campaign cost, audience substitution and
the candidate material. Suppressing promo projects within the current shared release roll would also remove
singles opportunities; the source comments already document that macro failure. A historical promo percentage
or a genre-specific hook penalty would obscure this mechanism instead of repairing it.

Evidence: `SimLogs/genre-hook-promo-launches-by-genre.csv`, `genre-hook-project-strategies.csv`,
`genre-hook-tradpop-promo-closed.csv`, `genre-hook-studio-opening-head-paths.csv`.

The requested historical research and completed release-practice experiment are now recorded in
[AlbumReleasePracticeResearch.md](AlbumReleasePracticeResearch.md). The opt-in candidate removes the
universal advance-single pipeline and distinguishes later LP extractions, but the five-seed, five-year
comparison does not establish an acceptable balance bundle. Its current parameters remain experimental.
