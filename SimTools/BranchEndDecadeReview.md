# Branch-end decade review (media-atmosphere, 2026-11-04)

These are the end-of-branch decade seeds for the professional song balance work. They ran with every song-balance flag
on, on top of the combined writing profile (`RunWritingCombinedIntegration.ps1`). This review covers what the two
seeds showed, the LP gate bug they exposed, and the runtime problem behind the five-hour decades.

Outputs (never committed): `SimLogs/branch-end-decade-market-{1001,2002}`, `SimLogs/lpgate-decade-market-2002`,
`SimLogs/songflags-off-decade-market-2002`.

## Runs

| run | build | flags | weeks |
|---|---|---|---|
| `branch-end-decade-market-1001`, `-2002` | A4083C70 | combined profile + all ten song-balance flags | 522 |
| `lpgate-decade-market-2002` | D22F957D | the same + `--lp-assembly-gate-uncredited` | 522 |
| `songflags-off-decade-market-2002` | D22F957D | combined profile, no song-balance flags | 522 |

The D22F957D build differs from A4083C70 only by the opt-in LP gate switch. Run without the switch, it is
byte-identical (145/145 files: `lpgate-off-market-1001` vs `writing-balance8-gate-market-1001`). A one-paired-seed
decade A/B is the standing method.

## Song balance: holds for the whole decade

Seed 2002, singles by year:

| | 1960 | 1963 | 1966 | 1969 |
|---|---|---|---|---|
| covers / professional, flags on | 1.14 | 1.40 | 1.30 | 1.20 |
| covers / professional, flags off | 4.96 | 5.37 | 5.22 | 4.18 |
| professional share of Top 40, on | 0.45 | 0.48 | 0.40 | 0.33 |
| professional share of Top 40, off | 0.18 | 0.23 | 0.19 | 0.19 |
| artist-written share of launches, on | 0.17 | 0.19 | 0.26 | 0.34 |

Artist-written material rises through the decade either way. On LPs it goes from about 24% to 63% of tracks, and
professional songs stay at 3-4% of LP tracks. The professional Top 40 share falls back to about a third in 1969, as
it should.

## Concerning: LP volume halves after 1965 (fixed, opt-in)

On branch-end seed 2002, store LP units went 71M (1962) → 35M (1969), the album decision share 0.44 → 0.22, and
active albums 5.5k → 2.7k. Seed 1001 matched (74M → 34M). Store LPs should hold at about 54-56M (Billboard, 1966-68;
see the all-channel table below). The song flags are not the cause: `songflags-off` slides the same way (1969: 32M, share 0.20).

**Cause: the LP profit gate subtracted the album portfolio credit twice.**
1. `DecideRelease` adds the portfolio credit to the album projection.
2. The release-slate profit gate (`PassesReleaseProfitGate`) removes it again, to test the album on its own.
3. With `--lp-assembly`, `PrepareSlateMasterCore` first *replaces* the projection with the raw assembly prior
   (`CalculateAlbumPriorNet`). That prior never contained the credit, so step 2 removed a credit that wasn't there.

The credit is tied to the album-era weight. It's about $1 in 1960-61, so this didn't show in short runs. From 1966
it's about $28k, while the median refused LP's assembly profit was about −$5.4k. LP gate pass rates fell from 0.84
(1964) to 0.09 (1969), and refusals rose to about 25k a year.

**Fix: `--lp-assembly-gate-uncredited`.** With the assembly prior, the gate removes no credit. It subtracts the
expected campaign instead, which the assembly prior also lacked. Seed 2002, branch-end → LP fix:

| | 1965 | 1966 | 1967 | 1968 | 1969 |
|---|---|---|---|---|---|
| store LP units (M) | 65 → 63 | 54 → 59 | 42 → 59 | 39 → 56 | 35 → 53 |
| store LP share of units | .33 → .33 | .30 → .32 | .25 → .32 | .23 → .30 | .22 → .29 |
| album decision share | .43 → .46 | .37 → .45 | .30 → .46 | .25 → .45 | .22 → .46 |
| LP gate pass rate | .65 → .97 | .29 → .98 | .17 → .99 | .11 → 1.00 | .09 → 1.00 |
| major family chart share | .69 → .67 | .59 → .66 | .57 → .65 | .58 → .64 | .58 → .62 |
| active MidTier labels (Dec) | 16 → 20 | 15 → 20 | 11 → 19 | 9 → 23 | 7 → 23 |

The fix removes the decline and the late MidTier collapse (MidTiers depend on LP margin). With the song flags off,
MidTiers fall to 5 by 1969. Singles are unchanged at 128M a year.

### All LP channels match Billboard: the "flat LP share" was a measurement error

The rows above use `decade-annual-rollup` `albumUnits`, which counts **record-store sales only**. The default
simulation profile also turns on record clubs, rack jobbers, budget LPs and the retail purchase envelope
(`--record-clubs --rack-jobbers --budget-lps --retail-purchase-envelope=1`, `SimulationArguments.cs`). After store
clearing those channels move LP buyers out of the store cohort (`lp-channel-choice-weekly`: about 20M a year switch to
racks from 1965). Store units are 38-60% of the regional album budget, and that isn't lost demand.

All channels, seed 2002, millions of LPs. Racks come from `rack-region-weekly`, budget LPs from `budget-region-weekly`,
clubs from `record-club-annual` without its `Licensor` rows (those repeat the licensed operators' units, so summing
every role double-counts about 20M). Billboard figures are from the 1966 Buyers Guide (1961-65) and the 1969-70
directory (1966-68):

| year | stores | racks + budget | clubs | LP total | Billboard total | Billboard stores / racks / clubs |
|---|---|---|---|---|---|---|
| 1961 | 65.6 | 50.3 | 45 | 160.9 | 173 | 68 / 60 / 45 |
| 1963 | 70.0 | 60.9 | 41 | 171.9 | 186 | 66 / 79 / 41 |
| 1965 | 63.4 | 69.9 | 46 | 179.3 | 228 (older, soft) | 76 / 106 / 46 |
| 1966 | 59.1 | 72.2 | 47 | 178.2 | 180 | 56 / 77 / 47 |
| 1967 | 58.9 | 81.7 | 49 | 189.5 | 192 | 56 / 87 / 49 |
| 1968 | 55.6 | 93.8 | 49 | 198.4 | 196 | 54 / 93 / 49 |
| 1969 | 53.5 | 103.0 | 49 | 205.5 | (n/a) | |

With the LP fix every channel tracks the period totals from 1966 on. 1961-64 run 7-12% low, mostly racks. Without
the fix, store LPs fall to 39M in 1968 against Billboard's 54M, and racks partly mask it: the total is 184.5M against
198.4M. So the fix also corrects which channel the LPs sell through.

Against singles the LP unit share is .56-.62. Billboard's all-format share is .46-.52, but that includes jukebox singles
(50-55M a year), which the model has no channel for. Without jukeboxes, Billboard is .57 (1961), .53 (1964) and .54
(1965), the same as the model. The rough author curve (30% in 1960 → 55% in 1969) predates the club, rack and budget
channels, and the Billboard tables replace it.

What genuinely differs is the singles side. Model singles are a flat 128M a year (the fixed purse,
`singles-volume-is-a-fixed-purse`). Billboard stores + racks singles grow 132 → 192M from 1961 to 1965, plus jukeboxes.
That's a separate design question, not an LP problem.

Recommendation: add `--lp-assembly-gate-uncredited` to the working bundle wherever `--lp-assembly` is on.

**Adopted 2026-11-04.** The switch is now in `writing_combined_integration_flags.txt`, next to `--lp-assembly`.
`RunWritingCombinedIntegration.ps1 -WithoutLpAssemblyGateUncredited` is the ablation that restores the old double
credit, and `-WithoutLpAssembly` drops both.

## Long-standing, not caused by this branch

Year-end Hot 100 slots, 1965-69 total, seed 2002 (benchmark from `GenreCatalog.ChartSlotBenchmark`):

| genre | branch-end | LP fix | song flags off | benchmark |
|---|---|---|---|---|
| RnB | 43 | 53 | 48 | about 5 |
| Country | 59 | 47 | 63 | about 9 |
| FolkRock | 4 | 9 | 7 | about 40 |
| Soul | 108 | 111 | 91 | about 128 |

RnB and Country over-chart late, and FolkRock under-charts, with or without the song flags and the new specialist
offices. The specialist offices are not the cause. FolkRock is supply-bound (`folk-rock-conversion-is-supply-bound`).
The flags move a few smaller genres by single-seed amounts: Soul +17, TradPop −12, TeenPop −10, SunshinePop −9,
GarageRock +10.

## Runtime: one quadratic, not the album count

Each branch-end decade took about 5.1 h of CPU (18.1-18.8k s) running as a pair. Album count wasn't the cause:
active albums *fall* to 2.7k by 1969, against about 18k in the old profiled decade.

A profiled 1960 (`--profile-performance`, now `-ProfilePerformance` on the combined wrapper) took 417 s, against
42 s for the October profile (`mtn5-control-default-1001-265`), with fewer records (2.7k vs 3.4k):

| phase | Oct 1960 | branch-end 1960 |
|---|---|---|
| competitor week | 10.0 | 289.2 |
| material selection | 3.6 | 35.6 |
| simulate week | 8.6 | 18.2 |
| playlists | 2.7 | 11.4 |
| capture week | 0.8 | 6.3 |
| competitor month | 0.9 | 4.2 |
| population prospects | 0.5 | 3.7 |
| GC pauses | 4.8 | 9.2 |

A dotnet-trace sample put about 55% of CPU, and half of it inside a single `WhereListIterator.MoveNext`, in linear
scans of the *global* `ReleaseProgrammeState.Items`. That list holds every label's open slate, plus closed items kept
for 52 weeks. The worst path is `CompetitorManager.ProgrammeCashReserved`, which every cash check reaches:
`EconomicLabelLifecycle.CommitmentReserve`, `LabelCorporateModel.ProtectedCash`, and the distribution funding
reserve. It's called once per *song*:
- `CanObtainProgrammeSong` → `ProfessionalWritingMarket.CanLicense` → `Affordable` → `CommitmentReserve`, for every
  song in the professional pool (family-wide with `--professional-family-pool`), for every open single slot, at every
  label, every week (about 16% of CPU);
- `ProfessionalWritingMarket.Visible` in `AiRepertoireService.PickSong` and `SongMaterialSelectionService.BuildProfessional`
  (about 15%; this is the 10x in material selection);
- `AvailableProgrammeCampaign`, once per campaign week (about 8%);
- `AiMasterDeals.CheckPickups`, `SelectDistributor` (every label × every client) and `GetAffordableHiringVacancies`
  (the competitor-month and prospect growth).

So the cost was open slots × pool songs × every label's slate. It got worse over the decade as slates grew.

**Fix, round 1 (always on, byte-identical): a per-label slate index.** `ReleaseProgrammeState.Items` is now a `SlateItemList`.
It saves exactly as a plain list, and every mutation bumps a version. `ItemsForLabel(labelId)` returns that label's
items in global order from a lazily rebuilt index. It rebuilds when the list instance, version or count changes, and
an item's LabelId is fixed at creation. Every label-filtered scan uses it. A filter or Sum over one label's items visits
the same items in the same order, so the arithmetic is unchanged.

A second round, from a trace of the first:
- `ItemsForArtist` handles the act-keyed scans: `HasConflictingIntention`, the open-intention scan in
  `PlanProgramme`'s candidate ranking, and unplanned follow-ups. Breakout planning calls `Items.Touch()` when it
  reassigns a slot's ArtistId.
- The index appends on `Add` instead of rebuilding.
- `ProfessionalWritingMarket.Writer` uses a versioned id dictionary (`CompositionCatalogService.GetProfessionalWriter`)
  instead of `FirstOrDefault` over every writer.
- `AvailableProgrammeCampaign` filters the label's campaigns and holds once per call, not once per campaign week.

Profiled 1960, seed 1001, full branch-end flag set (seconds):

| phase | Oct 1960 | before | round 1 | round 2 |
|---|---|---|---|---|
| wall | 41.9 | 417.1 | 183.1 | 105.6 |
| competitor week | 10.0 | 289.2 | 106.2 | 47.5 |
| material selection | 3.6 | 35.6 | 6.0 | 5.1 |
| competitor month | 0.9 | 4.2 | 0.9 | 0.8 |
| population prospects | 0.5 | 3.7 | 0.7 | 0.6 |
| population lifecycle | 3.8 | 7.3 | 4.4 | 3.9 |

Byte checks: 104 weeks, seed 1001, against `lpgate-off-market-1001` with identical flags. Both rounds matched 145/145
files: `slateidx-check-market-1001` (round 1, 9.0 min) and `slateidx2-check-market-1001` (round 2, 4.8 min). The control
took 30.2 min, but it ran alongside other jobs, so the minutes overstate the speedup; the profile is the clean
measure. The programme check suites pass: label-release-programme, preparation headroom, work budget, breakout
planning, release ownership, distribution funding and ownership, LP assembly, professional writing. The
programme-material check also fails without flags on the pre-change code, so that failure isn't from this change.

**Programme-material check, fixed 2026-11-04.** It passed bare, but failed under `--default-simulation-profile`: it
expected save versions 28, 29 and 30 as fixed numbers. Later features (budget LPs, the retail envelope, the writing
market) raise the version past 30. The suite now reads the world's own version before any material work and checks
that work lifts it to at least 29, and that a world without material work keeps its version. It passes bare, under the
game profile, and with the slate and LP assembly. Under `--professional-writing-market` it skips with a printed note,
because it fixes the legacy commission route ($150 fee booked by the programme). Under the market, fees and supplier
holds belong to the market's demos. No fixture yet covers the market route through programme material; that needs its
own suite built on market demos.

What's left in competitor week (about 47 s, against 10 s in October) is mostly new work: the professional writing
market's weekly advance (office songs, returning clients), resource telemetry, and campaign booking. The `PlanProgramme`
candidate ranking still runs `BuildDecisionContext` per eligible act per slot.

Genuinely new costs, not bugs: playlists (the station network's playlist meetings and response auditions) and capture
week (the professional-writing and writing-concentration audits; paid only with those audit flags on).
