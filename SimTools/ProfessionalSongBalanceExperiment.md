# Bringing covers and professional songs toward the historical ratio

2026-11-03. Follows `ProfessionalOfficeSongBalanceHandoff.md`. Target: the working
early-1960s singles assumption in `FreshMaterialHistoricalCalibration.md`, a central
**1.33 covers per fresh professional song** with **1–2** as a broad sensitivity range.
This is a calibration hypothesis, not a quota. Covers are `CoverStandard` plus
`CoverRecentHit`; professional is `ExternalProfessional`. The measure is the mature
issued single intention cohort used throughout the writing investigations.

## Diagnosis: professional singles are supply-bound, and LPs take the supply

In the combined control (seed 1001):

- **About 93% of professional arrivals find no song.** Of ~11,600 professional
  arrivals to act shortlists, 328 licensed a demo and 254 ordered a commission.
  Commissions were refused 7,487 times because no single plan met the minimum
  delivery time, and 3,329 times because the act already had two candidates.
  Covers draw on large catalogue pools and practically never come up empty.
  Realized arrivals were 897 professional against 10,681 covers. The attempted
  rates follow the source-mix anchors; the realized ones do not.
- **Arrivals that do find a song convert well.** 897 professional arrivals became
  718 mature professional singles, against 2,726 from 10,681 cover arrivals.
  Professional singles reach the Top 40 13% of the time, covers 5.8%.
- **The writers' output is small, and LPs use most of it.** 91 writers produce about
  22 demos a week, around 2,300 over two years. LP non-single cuts draw on the same
  visible pool and license demos at the same rate as singles: 1,537 LP cuts used
  professional songs, and 1,100 of 1,846 newly recorded demos appeared only on LPs.
- **Genre filing hides some of the stock.** An office song takes one random focus
  genre (Detroit: Motown/Soul/RnB; LA: SunshinePop/TeenPop/Bubblegum/EasyListening),
  and AI acts saw only exact primary-genre matches. At week 78, Detroit held 135
  unplaced demos while R&B and Soul arrivals came up empty 2,825 times. R&R and
  TeenPop are truly short: their offices had 2 and 31 demos available.

## Levers (opt-in, inert when off)

All three are in `Systems/ProfessionalSongBalance.cs`. The flag-off build reproduced
the original combined worlds byte for byte (label finance, slate, profit gates,
records, company-monthly) on both seeds.

| Flag | Change |
|---|---|
| `--lp-filler-spares-fresh-demos` | A non-single album cut may use only a demo the act already holds. It never licenses a fresh one and falls through to the ordinary source mix. |
| `--professional-family-pool` | AI acts are pitched professional demos from their whole genre family, own genre first, as cover pools already fall back to the family (`CompositionCatalogService.GetProfessionalForAct`). |
| `--writing-market-capacity=X` | Writers start a demo X times as often. The cap of eight unplaced demos per writer is unchanged. |

The wrapper exposes them as `-LpFillerSparesFreshDemos`, `-ProfessionalFamilyPool`
and `-WriterCapacity`.

## Results

Two seeds (1001, 2002), 104 weeks, assembly
`7D4AD086C4A828E4EBA500EA233932617088D7B9FDD42541D5C81C2CC0CF042E`, all against
`writing-balance2-off`.

| Arm | Pro singles | Covers | Covers/pro (by seed) | Singles | Artist-written | LPs | Closures | Writing fees | Hit acts | Top 40 entries | Fixed top-30 share |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Control | 1,350 | 5,536 | 4.10 (3.80, 4.45) | 10,861 | 3,239 | 8,109 | 232 | $0.99M | 573 | 1,012 | .286 |
| Filler + family | 2,041 | 5,135 | 2.52 (2.34, 2.70) | 10,859 | 2,986 | 8,114 | 233 | $1.31M | 565 | 1,025 | .297 |
| **+ capacity ×2** | **2,506** | **4,860** | **1.94 (1.82, 2.06)** | 10,762 | 2,744 | 8,162 | 240 | $1.86M | 574 | 1,023 | .286 |
| + capacity ×3 | 2,770 | 4,660 | 1.68 (1.67, 1.69) | 10,563 | 2,544 | 7,963 | 268 | $2.20M | 583 | 1,007 | .262 |
| + capacity ×4 | 2,931 | 4,544 | 1.55 (1.48, 1.62) | 10,515 | 2,451 | 7,933 | 278 | $2.40M | 550 | 1,019 | .283 |

Single-lever screens on seed 1001 (an earlier assembly with its own byte-identical
control): LP filler alone 3.80 → 2.55, capacity ×2 alone 3.80 → 3.19, and both
together 2.15. The family pool alone *worsened* the ratio to 4.30, because LPs
absorbed the newly visible demos (album-only demos 1,100 → 1,219). It helps only
once the filler rule protects them (filler 2.55 → filler + family 2.34).

Covers per professional song by genre, pooled:

| Genre | Control | + cap ×2 | + cap ×3 | Design anchor (1960, covers/pro) |
|---|---:|---:|---:|---:|
| RockAndRoll | 3.37 | 2.57 | 2.05 | 0.86 (rock-hit-writers anchor) |
| TeenPop | 1.64 | 0.66 | 0.60 | 0.50 |
| RnB | 5.31 | 3.14 | 3.03 | 0.80 |
| Country | 4.15 | 1.48 | 1.31 | 0.89 |
| TraditionalPop | 12.79 | 3.20 | 2.57 | 1.79 |
| Soul | 3.19 | 1.29 | 0.97 | 0.80 |
| DooWop | 2.13 | 1.22 | 1.25 | 0.80 |
| EasyListening | 3.20 | 1.69 | 1.30 | 1.71 |

Design anchors are the 1960 `Anchor1960` shares before `StandardShareFactor`, so
they overstate covers for non-songbook genres. They show direction, not targets.

### Side effects

- **Quality and charts hold.** In seed 1001's all-lever world, professional singles
  keep a mean record hook of .73–.76 and the all-single mean rises .684 → .694.
  Top 40 entries, hit acts and top-30 concentration stay within seed noise up to ×2.
- **Smaller labels gain most.** Professional singles by tier, seed 1001, control →
  all levers: Small 161 → 335, Independent 251 → 467, Boutique 97 → 198,
  MidTier 114 → 189, Major 274 → 336.
- **Artist-written singles are displaced.** Self-written singles fall from 3,239 to
  2,744 at ×2 (30% → 25% of singles), which is closer to the 1960 anchor (~20–22%).
- **Higher capacity costs closures.** ×2 is within noise (+8). ×3 and ×4 add 36 and
  46 closures as writing fees double: more assignments raise writer demand and
  reputation, and with them `Rate()`. Fee-driven turnover at ×3+ was not traced.
- **LPs change only modestly.** Album releases hold through ×2 and fall about 2%
  at ×3–×4. LP cuts shift from professional songs to covers, originals and
  traditional material.

## Recommendation

Use **filler + family + capacity ×2** as the candidate configuration. It is the first
point inside the 1–2 range on both seeds' pooled count (1.94), and costs nothing
measurable in closures, chart breadth or quality. Capacity ×3 reaches 1.68 but adds
~36 closures. Before pushing toward the 1.33 central assumption, look at writer fee
inflation or a cheaper supply route (for example, a larger writer pool at fixed fees
rather than faster writers) rather than raw cadence. R&R and R&B stay furthest from
their anchors at every dose. Their gap is office supply (one R&R office; Detroit's
three-way focus), not visibility, so per-office capacity or extra offices are the
next genre-specific lever.

Limits: two seeds, two years. The LP filler rule removes fresh professional songs
from album filler entirely; history had some. A softer version, such as allowing
fresh demos on LPs only for acts with no open single plan, is untested. Decade-length
and later-era behaviour, including the 1962–68 shift toward self-writing, is
unverified.

## Follow-up: more offices and writers instead of faster writers

The opening world has 6 offices and 120 writers (about 91 active). A ballpark for the
1960–62 market is 50–90 publishing offices with staff and demo operations, and 400–700
working professional writers, of whom 150–250 were core hit writers. That estimate comes
from 1961 society rolls (ASCAP 5,679 active writers and 1,902 publishers; BMI about
5,300 writers and 4,500 publishers) and the Brill Building's ~165 music businesses in
1962. Most rolled names were inactive or vanity firms. The model also has no office
where much R&B and R&R writing happened: the regional label houses (Chess/Arc, Vee-Jay,
King/Lois, Stax/East and Hi, Minit/Imperial, Cameo-Parkway, plus Atlantic/Leiber–Stoller
in New York).

Two more opt-in levers in `Systems/ProfessionalSongBalance.cs`:

| Flag | Change |
|---|---|
| `--writing-market-regional-offices` | Six label-house offices: New York R&B (RnB/R&R/DooWop, 18 writers), Chicago (RnB/DooWop/Blues, 14), Cincinnati (RnB/R&R/Country, 10), Memphis (Soul/RnB/R&R, 12), New Orleans (RnB/R&R, 8), Philadelphia (TeenPop/R&R/DooWop, 12). 74 writers, 45 opening titles each. |
| `--writing-market-extra-writers=N` | N more writers dealt uniformly across the six opening offices, as the original 120 were. No opening catalogue; ordinary cadence and fees. |

The extension draws from a private stream after the opening catalogue and takes its own
writer and song ids. Live songs from an added office get an office salt in their keyed
stream. In the opening offices, a live song's traits are keyed by serial alone, so
serial n repeats across those offices; that quirk is left as is. The flag-off build
reproduced `writing-balance2-off` byte for byte on both seeds (146 and 145 output files;
only the request file's run name and timestamp differ). Assembly
`2CE46559C06A3E72FB3969199BBC11A474E3328173B48E42E4ED8829C6BCC384`.

Pooled seeds 1001 and 2002, 104 weeks. All arms include filler + family. The control
and the earlier arms are the same worlds as above.

| Arm | Writers | Pro singles | Covers | Covers/pro (by seed) | Singles | LPs | Closures | Writing fees | Fees per pro single | Hit acts | Top 40 entries |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Control | 120 | 1,350 | 5,536 | 4.10 (3.80, 4.45) | 10,861 | 8,109 | 232 | $0.99M | $733 | 573 | 1,012 |
| + capacity ×2 (earlier) | 120 | 2,506 | 4,860 | 1.94 (1.82, 2.06) | 10,762 | 8,162 | 240 | $1.86M | $742 | 574 | 1,023 |
| **+ regional offices** | 194 | **3,030** | 4,585 | **1.51 (1.43, 1.60)** | 10,551 | 8,013 | 261 | $2.18M | $719 | 557 | 1,018 |
| + regional + 120 writers | 314 | 3,528 | 4,223 | 1.20 (1.10, 1.30) | 10,470 | 7,853 | 292 | $3.08M | $873 | 553 | 1,025 |

Covers per professional song by genre (pooled seeds; anchors as above):

| Genre | Control | + cap ×2 | + regional | + regional + 120 | Anchor |
|---|---:|---:|---:|---:|---:|
| RockAndRoll | 3.37 | 2.57 | 0.87 | 0.91 | 0.86 |
| TeenPop | 1.64 | 0.66 | 0.83 | 0.56 | 0.50 |
| RnB | 5.31 | 3.14 | 1.88 | 1.50 | 0.80 |
| Country | 4.15 | 1.48 | 2.07 | 1.23 | 0.89 |
| TraditionalPop | 12.79 | 3.20 | 4.06 | 2.52 | 1.79 |
| Soul | 3.19 | 1.29 | 0.64 | 0.61 | 0.80 |
| DooWop | 2.13 | 1.22 | 0.74 | 0.66 | 0.80 |
| EasyListening | 3.20 | 1.69 | 1.63 | 1.22 | 1.71 |

(Genre ratios pool each seed's covers and professional singles for that genre.)

Findings:

- **Offices beat speed.** 74 regional writers (+62% headcount) do more than doubling every
  writer's cadence: 1.51 against 1.94, with 524 more professional singles. Most of the
  gain is in the genres the regional offices serve. R&R lands on its anchor (0.87 vs
  0.86); Soul and DooWop move just past theirs. Commissions fall (254 → ~90 per seed)
  because licensed demos now meet the arrivals: empty arrivals drop from ~10,800 to
  ~7,800 per seed.
- **The extra 120 writers mostly reach the non-R&B genres.** Country, TraditionalPop and
  EasyListening gain the most, since the writers go to the opening offices. The overall
  ratio overshoots the 1.33 centre to 1.20, and seed 1001 lands at 1.10.
- **Closures follow total writing spend, not cadence.** Closures run 261 (+29 on the
  control) for regional and 292 (+60) for regional + 120, about where capacity ×3–×4
  landed (268, 278). Fees per professional single hold at ~$720–740 through regional, so
  the regional cost is volume, not unit price. At +120 writers the unit fee rises to
  $873, partly because LP cuts take professional songs again (album-only demos 439 →
  691 on seed 1001; LP professional cuts 2,157 → 3,020 pooled). The per-single fee
  trajectory was not traced to `Rate()` versus LP licensing.
- **Self-written and cover singles give way.** "Other" (mostly self-written) launches fall
  from 4,841 to 3,660 (regional) and 3,407 (+120), pooled. Hit acts slip 573 → 557 → 553
  and Top 40 entries hold.
- **R&B and TraditionalPop stay high.** R&B is still about twice its anchor; five of six
  regional offices file a share of their songs as RnB, but R&B single demand is large.
  TraditionalPop's gap is the songbook: standards supply most of its covers.

### Recommendation (follow-up)

Use **filler + family + regional offices** as the candidate, in place of capacity ×2. It is
the closest arm to 1.33 on both seeds, fixes R&R, and costs closures about halfway
between ×2 and ×3. A modest extra-writer dose (roughly 40–60) would centre the pooled ratio
near 1.33. The +120 dose overshoots and costs about 30 more closures.

The closure cost is now the binding question, and it argues for looking at fees after
all. Historically, publishers did not charge labels a fee to record a song: they pitched
for free and earned the statutory mechanical royalty (2¢ a side). A first-recording
license priced at $55+ per demo is a model choice. A royalty-funded licence, rather than
fee inflation, is the likely route to more professional songs without more closures.
That is untested.

Limits: two seeds and two years. Office staffs and focus mixes are ballpark. The extra
writers keep the original uniform dealing, so New York is not weighted to its real size.
Later-era behaviour is unverified.

## Follow-up: royalty-funded licences

An AI record already pays the 2¢ mechanical on every composition through COGS, and
`PeriodAiPublishing` routes it to the song's controller, so an external office is paid
for every copy sold. In the market, a first-recording licence also charges an upfront
fee (`Rate()`, from $55), so a professional song is paid for twice while a cover is paid
for once. Period publishers pitched for free and lived on the mechanical.

| Flag | Change |
|---|---|
| `--writing-market-royalty-licences` | `LicenseFee` is zero, so recording an office demo first costs nothing upfront. Commissions, retainers, the 12-week first-recording hold and the 8-demo cap are unchanged. |

Two runs only (seeds 1001 and 2002), on assembly
`611212D2137D1EFACF92F4BB2C0DFEC2DF7C75F0B20D56D0A3E238FE33EEA975`. Each differs from
`writing-balance3-ff-regional` by this flag alone. The build adds the one gated return
and its switch. Flag-off inertness is byte-checked: `writing-balance4-off` on this build
reproduces `writing-balance3-off` (assembly `2CE46559…`) on both seeds, 145 of 145
output files identical per seed (logs, manifest and request excluded). The analyzer refuses mixed assemblies, so the comparison was computed
from its `arm()` output directly.

| Arm (all with filler + family) | Pro singles | Covers | Covers/pro (by seed) | Singles | LPs | Closures (by seed) | Writing fees | Hit acts | Top 40 entries |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Control (no levers) | 1,350 | 5,536 | 4.10 (3.80, 4.45) | 10,861 | 8,109 | 232 (126, 106) | $0.99M | 573 | 1,012 |
| + regional offices | 3,030 | 4,585 | 1.51 (1.43, 1.60) | 10,551 | 8,013 | 261 (132, 129) | $2.18M | 557 | 1,018 |
| **+ regional + royalty licences** | **3,255** | 4,505 | **1.38 (1.33, 1.44)** | 10,899 | 8,301 | **213 (110, 103)** | $0.46M | 550 | 1,015 |

By genre (pooled): RockAndRoll 0.94, TeenPop 0.72, RnB 1.67, Country 1.60,
TraditionalPop 3.98, Soul 0.59, DooWop 0.74, EasyListening 1.39.

- **The ratio lands near the 1.33 centre**, at 1.38 pooled; seed 1001 is at 1.33.
- **Closures fall below the control**: 213 against 232 with no levers and 261 with
  regional offices alone. Bankruptcies fall on both seeds (96 → 80 and 107 → 83 against
  regional). Writing spend is now commissions and retainers only, $0.46M against the
  control's $0.99M. So the closure gain is mostly money that no longer leaves the
  labels. The closure cost of the regional arm was the licence fee.
- **Volume returns.** Singles and LPs recover to slightly above the control (10,899
  singles and 8,301 LPs, against 10,861 and 8,109), because the cash stays on the books.
- **Hit acts keep slipping**: 573 → 557 → 550. Top 40 entries hold, so the chart is
  shared among slightly more acts per hit act rather than shrinking. Two seeds cannot
  separate that from noise.
- R&B (1.67) and TraditionalPop (3.98) remain the outliers, as before.

### Recommendation (royalty licences)

Use **filler + family + regional offices + royalty licences** as the candidate. On two
seeds it is the only arm that is both inside ±0.1 of the 1.33 centre and costs no
closures. It also matches the period: publishers pitched demos free and were paid by
mechanicals. Extra writers are no longer needed to hit the target. Flag-off inertness is now byte-checked. Before adoption, run a longer horizon (planned for
the branch-end decade A/B). A free licence
removes the price signal that rationed demos, and its effect after 1962, when
self-writing rises, is unverified.

## Follow-up: why R&B and TraditionalPop stay high

Same two worlds per seed (`writing-balance3-off` and `writing-balance4-ff-regional-royalty`), pooled.
Single-source counts are from issued slate singles by record genre; arrival outcomes are from
`professional-arrivals`, keyed by the act's genre.

| Genre | Std covers | Recent-hit covers | Pro | Covers/pro | Design covers/pro | Pro arrivals | Licensed | Empty | Writer-equivalents |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| RnB | 62 | 516 | 347 | 1.67 | **0.46** | ~4,220 | 1,249 (30%) | 2,885 (68%) | ~29 |
| TraditionalPop | 560 | 259 | 206 | 3.98 | **1.79** | ~1,630 | 262 (16%) | 1,356 (83%) | ~13 |
| Country | 214 | 251 | 291 | 1.60 | 0.52 | ~2,380 | 404 (17%) | 1,924 (81%) | ~13 |
| RockAndRoll | 17 | 607 | 664 | 0.94 | ≤0.86 | ~4,440 | 835 (19%) | 3,518 (79%) | ~21 + R&R office |

"Design" is the 1960 `Anchor1960` mix *after* `StandardShareFactor`, which the earlier tables
omitted. That scaling moves R&B's design from 0.80 to 0.46 (factor 0.15 on standards) and Country's
from 0.89 to 0.52 (factor 0.40). For the songbook genres it is 1 and changes nothing. Rock & roll's
standards use `rockStandardFactor`, so its value is the earlier unscaled 0.86 as an upper bound.
Country is as far off its design as R&B (3.1×) and shares the cause: 81% of its professional arrivals
come up empty. Writer-equivalents count each office's writers divided by its
number of focus genres (opening offices hold 20 writers each, with `--rock-hit-writers` on).

- **Both are still supply-bound, not losing at selection.** "Empty" is an arrival that found no demo
  (`empty-count-blocked` or `no-plan-minimum-delivery`). A demo that does arrive usually becomes the
  single: TraditionalPop turns 262 licensed arrivals into 206 issued professional singles. Covers
  never come up empty, so they fill the slots professional arrivals leave. The `material-funnel`
  contests, where both a professional and a cover candidate are positive, already favour
  professional songs (R&B 1,119 to 788; TraditionalPop 1,112 to 672).
- **R&B has the largest professional demand of any genre.** Its design gives professional songs 46%
  of singles, because the standards share it gives up is redistributed. It attempts about 4,220
  professional arrivals, as many as rock & roll, across ~29 writer-equivalents, and all five
  label-house offices that file R&B split it with R&R, DooWop, Blues, Country or Soul. Recent-hit
  covers fill the gap: 42% of R&B singles against an 18% design share.
- **TraditionalPop has the least professional supply per arrival.** Two offices file it (New York
  Pop and Legacy Tin Pan Alley), each splitting it three ways, so ~13 writer-equivalents. With the
  family pool on, TeenPop, GirlGroup and EasyListening acts draw on the same demos. Standards fill
  the rest: 560 standard covers against 206 professional singles.
- **TraditionalPop's own design is standards-heavy.** The 1960 mix is Std .40 against Pro .28, the
  same mix LPs use (`AlbumMaterialPlan`). That fits the period's LPs; on singles it is probably too
  high, since 1960 pop singles were mostly new Tin Pan Alley songs. Even so, 3.98 is 2.2× its own
  design. R&B's 1.67 is 3.6× its design.
- **The pooled 1.38 balances these against genres under their anchor.** Soul (0.59), TeenPop (0.72)
  and DooWop (0.74) sit below theirs. Doubling TraditionalPop's professional singles alone would move
  the pooled ratio to about 1.24 (4,299 covers / 3,461 professional). Fixing the outliers means
  re-centring the total, not just adding writers.

Candidate levers, untested: weight the regional offices' R&B filing above their other focus genres,
or give R&B a dedicated office; add a staffed Tin Pan Alley/Brill pop office for TraditionalPop; and
consider a singles-only TraditionalPop/EasyListening mix with fewer standards than the LP mix.

## Follow-up: stranded legacy-genre demos and specialist offices

Two more opt-in levers in `Systems/ProfessionalSongBalance.cs`:

| Flag | Change |
|---|---|
| `--writing-market-canonical-office-genres` | An office song's genre is filed through `GenreCatalog.MapLegacy`. The draw (and the title bank) is unchanged, so no shared stream moves. |
| `--writing-market-specialist-offices` | Seven single-genre offices on the private extension stream: Broadway & Film and Big Three (TraditionalPop, 16 and 14 writers), Brill Teen (TeenPop, 16), Mood & Instrumental (EasyListening, 10), and Houston, Los Angeles and Harlem R&B houses (10 each). 86 writers, 45 opening titles each. Acts are pitched their own genre's demos first and reach into the family only when their own shelf is empty, in all three professional-pool consumers. |

**The bug.** New York Pop's focus list is `{TeenPop, GirlGroup, TraditionalPop}` and Detroit's is
`{Motown, Soul, RnB}`. Both `GirlGroup` and `Motown` are legacy values (`MapLegacy` maps them to TeenPop and
Soul), but the catalogue never maps an office song, indexes it by raw genre, and the legacy values have no
genre profile, so neither exact nor family pools reach them. No act or record carries those genres. In the
royalty world (seed 1001, week 104), Detroit holds 110 unplaced demos and 7 of its 19 writers sit at the
eight-demo cap; New York Pop holds 27, with 5 of 9 writers at the cap, and wrote only 156 live songs against
Tin Pan Alley's 449. Every other office ends with 0–2 available demos except Tin Pan (24) and Nashville (19).

Build `25DDCEAA1B1A194C637EFBDC22D7AFA3F97E0901CFF2F51CFC2FCBF39C451051`; both arms add to filler + family +
regional + royalty licences. Two seeds, 104 weeks. Flag-off, this build reproduces `writing-balance4-off` byte for
byte on seed 1001 (145 of 145 output files).

| Arm | Pro singles | Covers | Covers/pro (by seed) | Closures (by seed) | Hit acts | Top 40 entries | Writing fees |
|---|---:|---:|---:|---:|---:|---:|---:|
| Royalty candidate (previous build) | 3,255 | 4,505 | 1.38 (1.33, 1.44) | 213 (110, 103) | 550 | 1,015 | $0.46M |
| + canonical genres | 3,380 | 4,467 | 1.32 (1.20, 1.46) | 235 (130, 105) | 541 | 1,023 | $0.42M |
| + canonical + specialist | 4,005 | 4,161 | **1.04 (1.01, 1.06)** | 223 (120, 103) | 576 | 1,025 | $0.40M |

Seed 1001's closures move 110 → 130 → 120 across arms with no matching cost signal (fees fall), so they read
as reshuffle noise of about ±10 per seed.

| Genre | Royalty | + canonical | + specialist | Licensed share of arrivals (specialist) | Design |
|---|---:|---:|---:|---:|---:|
| RnB | 1.67 | 1.41 | 0.90 | 76% | 0.46 |
| TraditionalPop | 3.98 | 3.01 | 2.28 | 79% | 1.79 |
| Country | 1.60 | 1.55 | 1.58 | 15% | 0.52 |
| RockAndRoll | 0.94 | 0.92 | 1.02 | 21% | ≤0.86 |
| Soul | 0.59 | 0.57 | 0.51 | 73% | 0.46 |
| DooWop | 0.74 | 0.71 | 0.63 | 79% | ~0.46 |
| TeenPop | 0.72 | 0.78 | 0.42 | 69% | ~0.35 |
| EasyListening | 1.39 | 1.45 | 0.89 | 78% | 1.71 |

- **The canonical fix is worth keeping on its own.** It frees Detroit and New York Pop, R&B falls 1.67 → 1.41
  and TraditionalPop 3.98 → 3.01, and the pooled ratio lands on 1.32. It changes no draw.
- **Specialist offices end supply scarcity for the genres they serve.** 69–79% of professional arrivals now
  license a demo, against 16–30% before. R&B (0.90) and TraditionalPop (2.28) close most of the gap to their
  designs. Country, which no specialist office serves, is unchanged at 1.58 with 15% licensed. Nashville is the
  next office gap.
- **With supply no longer binding, the design mixes set the ratio, and they are below the target.** If each of
  these eight genres sat exactly on its design, they would pool at about 0.71 covers per professional song (the
  specialist arm pools them at 0.92). The 1.33 central assumption from `FreshMaterialHistoricalCalibration.md`
  and the 1960 `Anchor1960` source mixes disagree. Until now writer scarcity hid that by manufacturing covers.
  EasyListening is already below its own design (0.89 vs 1.71), and TeenPop is near its own (0.42 vs ~0.35).
- Hit acts recover to 576 and Top 40 entries hold. Self-written singles fall a further ~150.

So there are two ways to land near 1.33. One keeps the offices realistic and recalibrates the 1960 source
mixes (more cover share, or less professional share, in the arrival mix) so that ample supply lands on the
historical ratio. The other trims offices until scarcity produces it. The first is the more stable model,
because a ratio produced by scarcity drifts whenever writer output or demand moves.

## Follow-up: Country offices and recalibrated 1960 mixes

The 1.33 target comes from the working central mix in `FreshMaterialHistoricalCalibration.md`: 20% artist-written,
30% professional, 20% standards, 20% recent-hit covers, 10% traditional. The 1960 `Anchor1960` mixes average close to
that, but only *before* `StandardShareFactor`, which keeps 15% (R&B, Soul), 40% (pop, country, teen) or 2% (rock) of
a genre's standards share. Normalization then spreads the removed share over every source, so total cover share
roughly halves for the non-songbook genres.

| Flag | Change |
|---|---|
| `--source-mix-standards-to-recent-hits` | In `SourceMix`, the standards share a genre gives up is added to recent-hit covers, so its total cover share stays at the anchor's. All consumers read `SourceMix`: single arrivals, material search, and the LP plan (`GetSourceMixShares`). |
| `--writing-market-specialist-offices` (extended) | Adds three Country offices: Acuff-Rose (14 writers), Tree & Cedarwood (12) and a Bakersfield house (8). Ten offices and 120 writers in all. |

Design covers/pro after the change: R&B, Soul and DooWop 0.46 → 0.80; Country 0.52 → 0.89; TeenPop ~0.35 → 0.50.
TraditionalPop and EasyListening keep all their standards and are unchanged.

Build `3FA4004C4C46FC7723655EDA23C0B0A9AA9B3849A27950DF8FAD91688D0BA6FE`. Both arms run filler + family + regional +
royalty + canonical genres + specialist offices (with Country). Two seeds, 104 weeks. Flag-off, this build reproduces
`writing-balance5-off` byte for byte on seed 1001 (145 of 145 output files).

| Arm | Pro singles | Covers | Covers/pro (by seed) | Self-written | Closures (by seed) | Hit acts | Top 40 entries |
|---|---:|---:|---:|---:|---:|---:|---:|
| Specialist, no Country (previous build) | 4,005 | 4,161 | 1.04 (1.01, 1.06) | 2,238 | 223 (120, 103) | 576 | 1,025 |
| Specialist + Country | 4,398 | 4,002 | 0.91 (0.92, 0.90) | 2,073 | 224 (110, 114) | 547 | 1,031 |
| **+ standards to recent hits** | 4,124 | 4,456 | **1.08 (1.02, 1.14)** | 1,914 | 228 (124, 104) | 579 | 1,023 |

| Genre | Specialist + Country | + mix change | Design (after change) |
|---|---:|---:|---:|
| RnB | 0.95 | 1.16 | 0.80 |
| TraditionalPop | 2.92 | 2.79 | 1.79 |
| Country | 0.55 | 0.68 | 0.89 |
| RockAndRoll | 0.96 | 1.10 | ≤0.86 |
| Soul | 0.48 | 0.62 | 0.80 |
| DooWop | 0.54 | 0.79 | 0.80 |
| TeenPop | 0.40 | 0.61 | 0.50 |
| EasyListening | 0.80 | 0.92 | 1.71 |

- **Country offices fix Country.** Country falls 1.58 → 0.55, and 90% of its professional arrivals license a demo,
  against 15%. With the mix change it is 0.68, below its design.
- **The mix change moves the pooled ratio less than the design does.** The design-implied ratio for the eight
  genres rises about 1.25×; the realized pooled ratio rises 1.19× (0.91 → 1.08). The likely reason is selection:
  where a professional and a cover candidate compete, professional songs win most contests (the earlier
  `material-funnel` read), so extra cover arrivals convert at less than their share. That was not traced in these runs.
- **LP cuts follow the mix.** Recent-hit covers on LPs rise ~22% per seed (12,115 → 14,682 and 11,102 → 14,015),
  mostly at the expense of self-written (−1,500) and traditional (−750) cuts per seed. Professional LP cuts barely move,
  because the filler rule keeps fresh demos for singles.
- **No cost.** Closures (228) sit inside the ±10-per-seed noise, hit acts recover to 579, Top 40 entries hold.
- TraditionalPop (2.79) stays the outlier against its 1.79 design. It now licenses 79% of professional arrivals,
  so its remaining gap is selection and its standards-heavy design, not supply.

The candidate is now filler + family + regional + royalty licences + canonical genres + specialist offices +
standards to recent hits: 1.08, inside the 1–2 range but below the 1.33 centre. Reaching the centre would need
the 1960 professional shares themselves to come down. TeenPop (56%), the R&B family (40%) and Country (36%) sit above
the central 30%. That is a further, separate recalibration.

## Follow-up: genre-specific 1960 revisions and the programme search gate

TeenPop's high professional share is historical (manufactured hits), so it stays. Instead, the genres that lean on
office songs more than their period did are revised, and TraditionalPop gets separate singles and LP mixes.

| Flag | Change |
|---|---|
| `--source-mix-1960-pro-trims` | 1960 anchors (`SongMaterialSelectionService.Revised1960`): DooWop gets its own mix (self-written .32, professional .26) instead of the R&B family's (.18, .40); Folk professional .06 → .03; Blues .08 → .05; Jazz .14 → .08; Classical, which fell through to the generic default (professional .30), gets .03 with the canon carried as standards. |
| `--tradpop-singles-new-songs` | TraditionalPop *singles* use a new-song mix (.08, .45, .25, .10, .12). LPs (the `album` argument to `GetSourceMixShares`, which `AlbumMaterialPlan` and album filler pass) keep the standards-heavy anchor. |
| `--programme-search-follows-mix` | The slate's material search samples professional demos for a slot only with probability min(1, genre professional share / 0.30). |

**Why the trims alone did nothing.** With trims on, DooWop, Folk, Blues and Jazz professional shares barely moved
(DooWop 45 → 43%, Folk 22 → 22%, Blues 27 → 22%, Jazz 22 → 19%). Half to three-quarters of professional singles come
through the slate's programme material search, not the arrival shortlist: Folk 76%, Blues 76%, rock & roll 69%,
Jazz 65%, R&B 49%. The search samples professional demos and standards and takes the best-valued offer; the source mix
enters only as the baseline, so a folk label hunted office songs as hard as a teen-pop label. Where the competing
material is weak (traditional songs, self-written), a fresh office demo wins. Folk's professional singles were mostly
Nashville songs filed as Folk, Blues' were Chicago songs filed as Blues, and Jazz's were Tin Pan songs filed as Jazz.

**TradPop.** It was not writer-short (79% of professional arrivals licensed), and its standards pool is not too small:
1,020 TradPop standards (plus 500 jazz standards with TradPop as second genre), of which singles used 190–257
distinct songs per seed over two years, the most-covered 5–6 times. Selection did not favour standards either (issued
hook .765 standard vs .763 professional). Its 43% standards share followed its anchor, which is the LP-shaped mix
(standards .40) applied to singles as well. Period pop singers issued mostly new publisher songs as singles
(Sinatra's "Nice 'n' Easy", "The Second Time Around", "Pocketful of Miracles"; Como's "Delaware") and recorded the
standards on albums, with some standard revivals as singles ("Beyond the Sea", "Who's Sorry Now", "Granada").

Builds `DD2740B4…` (trims, TradPop) and `A4083C700F817F0C979B26846983EC3FB1188463488A22A22E05250B77BFEE6F` (gate). All
arms add to the full previous candidate (filler + family + regional + royalty + canonical + specialist with Country +
standards to recent hits). Flag-off, the gate build reproduces `writing-balance6-off` byte for byte on seed 1001
(145 of 145).

| Arm | Pro | Covers | Covers/pro (by seed) | Closures (by seed) | Hit acts | Top 40 | LPs |
|---|---:|---:|---:|---:|---:|---:|---:|
| Previous candidate | 4,124 | 4,456 | 1.08 (1.02, 1.14) | 228 (124, 104) | 579 | 1,023 | 8,284 |
| + trims | 4,089 | 4,352 | 1.06 (1.05, 1.08) | 242 (125, 117) | 570 | 1,032 | 8,051 |
| + trims + TradPop singles | 4,196 | 4,243 | 1.01 (0.97, 1.06) | 246 (141, 105) | 603 | 1,017 | 8,031 |
| **+ trims + TradPop singles + gate** | 4,072 | 4,332 | **1.06 (1.06, 1.07)** | 229 (116, 113) | 577 | 1,025 | 8,225 |

| Genre (pro share of singles) | Previous candidate | Final | | Genre (covers/pro) | Previous | Final |
|---|---:|---:|---|---|---:|---:|
| Folk | 22% | 11% | | TraditionalPop | 2.79 | 1.84 |
| Blues | 27% | 14% | | RnB | 1.16 | 1.23 |
| Jazz | 22% | 13% | | Country | 0.68 | 0.67 |
| Classical | 16% | 10% | | TeenPop | 0.61 | 0.53 |
| DooWop | 45% | 43% | | EasyListening | 0.92 | 0.76 |
| Gospel | 19% | 21% | | Soul | 0.62 | 0.59 |

- Folk, Blues, Jazz and Classical now sit near their period mixes. DooWop does not move: its gate is 0.87 and its fresh
  demos still win shortlist picks. Given how much 1960 doo-wop came from Brill and Leiber–Stoller writers, that is
  left as is.
- TraditionalPop's 1.84 includes LP promo singles, which follow the album mix.
- Closures, hit acts, Top 40 entries and LP releases are within seed noise of the previous candidate.

**Adopted candidate for the decade run:** filler + family + regional offices + royalty licences + canonical genres +
specialist offices (with Country) + standards to recent hits + 1960 trims + TradPop singles + programme search gate.
Pooled 1.06 (1.06, 1.07), inside the 1–2 range. The remaining gap to 1.33 comes from genre-grounded mixes, and the
central figure is itself a judgment chosen for testing (`FreshMaterialHistoricalCalibration.md`); with every large
genre near a defensible period mix, the pooled 1960–61 ratio lands nearer 1:1.

## Reproduction

```powershell
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001 -Tag writing-balance2-off -ExpectedAssemblySha256 <hash>
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001 -Tag writing-balance2-all -LpFillerSparesFreshDemos -ProfessionalFamilyPool -WriterCapacity 2 -ExpectedAssemblySha256 <hash>
py SimTools/analyze_professional_song_balance.py --control-tag writing-balance2-off --treatment-tags writing-balance2-filler-family writing-balance2-all writing-balance2-all-cap3 writing-balance2-all-cap4 --seeds 1001 2002
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001,2002 -Tag writing-balance3-ff-regional -LpFillerSparesFreshDemos -ProfessionalFamilyPool -RegionalOffices -ExpectedAssemblySha256 <hash>
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001,2002 -Tag writing-balance3-ff-regional-w120 -LpFillerSparesFreshDemos -ProfessionalFamilyPool -RegionalOffices -ExtraWriters 120 -ExpectedAssemblySha256 <hash>
py SimTools/analyze_professional_song_balance.py --control-tag writing-balance3-off --treatment-tags writing-balance3-ff-regional writing-balance3-ff-regional-w120 --seeds 1001 2002 --output SimLogs/writing-calibration/writing-professional-song-balance3.json
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001,2002 -Tag writing-balance4-ff-regional-royalty -LpFillerSparesFreshDemos -ProfessionalFamilyPool -RegionalOffices -RoyaltyLicences -ExpectedAssemblySha256 <hash>
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001,2002 -Tag writing-balance4-off -ExpectedAssemblySha256 <hash>
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001,2002 -Tag writing-balance5-ffrr-canon -LpFillerSparesFreshDemos -ProfessionalFamilyPool -RegionalOffices -RoyaltyLicences -CanonicalOfficeGenres -ExpectedAssemblySha256 <hash>
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001,2002 -Tag writing-balance5-ffrr-canon-spec -LpFillerSparesFreshDemos -ProfessionalFamilyPool -RegionalOffices -RoyaltyLicences -CanonicalOfficeGenres -SpecialistOffices -ExpectedAssemblySha256 <hash>
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001 -Tag writing-balance5-off -ExpectedAssemblySha256 <hash>
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001,2002 -Tag writing-balance6-spec-country -LpFillerSparesFreshDemos -ProfessionalFamilyPool -RegionalOffices -RoyaltyLicences -CanonicalOfficeGenres -SpecialistOffices -ExpectedAssemblySha256 <hash>
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001,2002 -Tag writing-balance6-spec-country-mix -LpFillerSparesFreshDemos -ProfessionalFamilyPool -RegionalOffices -RoyaltyLicences -CanonicalOfficeGenres -SpecialistOffices -StandardsToRecentHits -ExpectedAssemblySha256 <hash>
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001 -Tag writing-balance6-off -ExpectedAssemblySha256 <hash>
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001,2002 -Tag writing-balance7-trims <previous candidate switches> -ProTrims1960 -ExpectedAssemblySha256 <hash>
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001,2002 -Tag writing-balance7-trims-tps <previous candidate switches> -ProTrims1960 -TradPopSinglesNewSongs -ExpectedAssemblySha256 <hash>
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001,2002 -Tag writing-balance8-gate <previous candidate switches> -ProTrims1960 -TradPopSinglesNewSongs -ProgrammeSearchFollowsMix -ExpectedAssemblySha256 <hash>
./SimTools/RunWritingCombinedIntegration.ps1 -Seeds 1001 -Tag writing-balance8-off -ExpectedAssemblySha256 <hash>
```

Run one task per seed in parallel and pass an absolute script path from background
shells. Output: `SimLogs/writing-calibration/writing-professional-song-balance.json`.
The analyzer rejects mixed assemblies, incomplete runs, engine errors, and any flag
difference outside the balance levers.
