# Release Judgement Directive

Status: **design draft, nothing built** (2026-10-18). Source: the Top 10 review arc in
`HistoricalLabelTierBalanceResearch.md`, §17x-§17af, and the author's direction after §17af. A label should
release a single because it has a good song and needs a single. It may wait a few weeks to write, commission
or cover a better one rather than ship whatever the act has. Albums come out when they make sense: regular LPs
where the genre wants them, hit compilations for youth acts.

## The reframe

The AI release path makes three choices in a fixed order, and the song comes last:

1. **Who:** a priority lottery over roster acts off cooldown.
2. **What format:** a margin comparison that reads the artist's base quality, revenue memory and noise.
3. **What song:** generated on the spot.

The song never feeds back. A label cannot pass on a weak song or hold a strong one for the right moment. It cannot
go and find better material, and it cannot know it has a hit in hand. The only song selection anywhere in the AI
path is inside an album: its promo single is the best of its tracks.

The player already has the missing objects: a per-act repertoire with readable songs, covers that are rehearsed in,
and commissions delivered to order (`PlayerDesk.RepertoireItem`). The AI has none of them. This directive gives the
AI labels the same judgement through the same objects, staged so that each step can be measured.

## Code-verified findings (checked against the tree, 2026-10-18)

- **The roll.** `CompetitorManager.TryReleaseRecord` (CompetitorManager.cs:1927) runs:
  1. `AILabel.GetArtistForRelease` (Data/AILabel.cs:557)
  2. `ChooseEnabledGenreSupply`
  3. `DecideRelease` (:2409)
  4. the hit-act lane's format override, when on (:1967)
  5. `GenerateRecordFromArtist` (:3351), or `TryInitiateAlbumProject` (:2174)
  6. production, marketing and release

  Roll frequency is `releasesPerMonth / 4` a week (`CalculateLabelReleaseCapacityChance`, :1601).
- **Who.** `GetArtistForRelease` is a weighted lottery on `CalculateReleasePriority`: career priority, 0.4 ×
  momentum, and a cooldown bonus. It covers acts past `GetMinimumReleaseCooldown`: 12 weeks, 10 for Star or above,
  and 8 for a Superstar at a Major. With `--hit-act-cadence` on, a due hot act takes the roll first.
- **Format.** `DecideRelease` reads `artist.CalculateBaseQuality()` (`BuildDecisionContext`, :2879), the format
  revenue memories and two noise draws. It also reads an album portfolio credit and a `HitInventory`
  (`ResolveHitInventory`) that already feeds the album prior. No specific song exists yet when it runs. The choice is
  margin-driven ([[album-creation-is-margin-driven]]).
- **Orphan single material.** `GenerateRecordFromArtist` makes one song. Its hook is `songwriting × 0.55 +
  baseQuality × 0.35 + U(−0.12, 0.18)`. Its production is `label × 0.4 + studioPerformance × 0.3 + studio × 0.2 +
  U(−0.05, 0.10)`. `SongMaterialSelectionService.ChooseMaterial` (:60) builds one candidate per source (artist-written,
  professional, standard, recent-hit cover, traditional). It makes a **weighted-random** pick by the `SourceMix`
  targets (`WeightedPick`), not by hook, so a strong song is never preferred.
- **Promo single material.** `CreatePromoSingleFromAlbum` (:3582) takes the best of the album's `nonSingleTracks` by
  `GetLeadSingleSuitability`. That score comes from title-hashed traits ([[song-titles-feed-track-traits]]).
- **Promo timing is modelled.** The promo ships on the roll, and the album drops 3-5 weeks later
  (`albumDropGapWeeksMin/Max = 3/5`; V1 seed 1001 mean 4.0, n 13,112). The album's stock and awareness carry a
  promo-performance synergy (`synergyStockMultiplier`, mean 0.87). This was built under Directive 3B
  (`AlbumProjectPipelineDirective.md`).
- **Compilations are a dice roll, not a reaction to hits.** In `GenerateAlbum`, `GetCompilationChance(genre, year)`
  (AlbumModel.cs:215) is the genre's `SingleOrientation` decayed by era. The compilation takes the act's **last four
  released singles**, whether they charted or not. Directive 3B noted there is no four-single gate.
- **The hit-act lane** (`HitActCadence`) forces album picks to orphan singles on 48% of its picks (§17af).
- **Player parity objects exist:** `RepertoireItem` (ReadHook, ReadQuality, IsCommission, SongId, Recorded),
  `TeachCover` / `ProcessCoverRehearsals`, and `CommissionSong` with `BuildProfessionalForSong`
  ([[material-mix-and-commission]]).

## Measured baseline (V1, five seeds, single launches 1961-64)

The chart reads **realized quality**, `(hook + production) / 2`. Promo singles carry a higher hook but a lower
production read, so their realized-quality edge is small:

| Path | Launches a year (per seed) | Hook | Production | Realized quality | Hot 100 entry | Top 40 |
|---|---:|---:|---:|---:|---:|---:|
| Album promo single | 2,781 | 0.716 | 0.535 | 0.626 | 12.6% | 4.0% |
| Orphan single | 2,836 | 0.526 | 0.671 | 0.598 | 11.6% | 3.9% |

Entry by realized-quality quintile (edges 0.529 / 0.588 / 0.639 / 0.696):

| Quintile | Q1 | Q2 | Q3 | Q4 | Q5 |
|---|---:|---:|---:|---:|---:|
| Promo entry | 0.3% | 0.7% | 2.0% | 7.9% | 44.4% |
| Orphan entry | 0.1% | 0.4% | 2.7% | 12.7% | 51.2% |

**What this says:**

- **Entry is a cliff in realized quality.** Below about 0.64, which is 60% of all singles, a single almost never
  charts (0.1-2.7%). Above about 0.70 it charts about half the time. At equal quality the two paths enter alike, so
  the lever is **the quality of what ships, not the path.**
- **A song bar has a lot to work on.** Most singles are pressed, promoted and costed with essentially no chance.
  Labels that held weak songs back would redistribute supply, not just trim it (see risk 1).
- **Correction to §17af's framing:** its hook contrast (promo 0.80 against orphan 0.59) overstates the gap. Its
  measured entry rates for hot acts stand. The keep-the-album probe is folded into Phase 5 here.

## Design intent

1. **A song pool per AI act.**
   - Acts accumulate candidate songs over time: their own writing (rate and quality from `songwritingAbility` and the
     members' creativity), offers from professional writers (by label tier access, as `BuildProfessional` already
     scales it), and covers (recent hits and standards through the existing `SourceMix`).
   - The pool is small and capped (on the order of 4-10), and songs age: a recent-hit cover goes stale.
   - Pool songs are real compositions with titles fixed when they enter, since titles feed traits.
2. **A single bar.** When the roll lands on an act, the label reads the pool through its own perception. That read
   is noisy and scaled by `scoutingAbility`, in the manner of `ScoutingPerception`, so judgement is imperfect and
   flops still happen ([[closures-must-be-earned-not-imposed]]). A single ships only if the best song read clears
   the label's bar.
3. **Wait, commission or cover.** If nothing clears the bar, the label can:
   - wait: the roll passes to the next eligible act and this act stays due;
   - commission: a professional song, with a fee and a delivery delay, worth it for a hot or valued act; or
   - cut a cover: a recent hit in the genre.

   A maximum wait, and the contract's singles obligation (`SinglesObligation`, Scouting Phase 3), force a release
   eventually. A new signing gets a first-single exception: careers are short
   ([[careers-are-two-records-long]]) and a debut cannot wait for a pool to fill.
4. **Albums when they make sense.** The margin model stays as the economic check, but the format reads the
   situation:
   - **pool depth:** enough good songs to make an album;
   - **genre album orientation:** `GenreProfile.SingleOrientation`;
   - **career:** an established adult act on a regular LP cadence;
   - **hits in hand:** two or three charting singles since the last LP make a hits compilation, the youth-genre
     pattern.

   An album's promo single is its best track, as now, so it comes from the best of the pool.
5. **Compilations after hits.** Gate the compilation on hit inventory, and build it from the act's charting singles
   since its last LP, not its last four releases.
6. **The lane, retired into judgement.** A hot act's due roll goes to the best song in its pool, and the lane's
   album override goes away (§17af).

## Staged plan (low → high calibration risk)

Every phase ships behind its own switch, off by default, and must meet the house discipline:

- byte-identical when off, proved by CSV hash against a reference run
  ([[probe-run-byte-comparison-proves-inertness]]);
- a one-seed smoke;
- a five-seed paired screen against the default controls, and against V1 / LR where the lane is involved, with the
  §17o yearly watch;
- the decade A/B only at the end, on two seeds ([[decade-ab-only-at-audit-end]],
  [[canonical-decade-run-flags]]).

### Phase 0: Measurement only (no behaviour)

- Log each launch's realized quality, path, and the act's weeks since its last single. Most of this is already in
  `single-launch-attribution.csv`, which is on only with `--head-hit-attribution`. Make the realized-quality column
  part of the default compact set.
- Write the baseline tables above for the default controls, as well as V1.
- Find a historical anchor for release volume, such as singles reviewed per year in the trade press, 1960-64. The
  sim releases about 5,600 singles a year per seed. Whether that is high is not known, and Phase 3 depends on it.

### Phase 1: The AI song pool, observe-only

- Build the pool: generation, refill, ageing, cap and save state. Its draws come from stable hashes or a separate
  stream, so the main RNG schedule is untouched ([[one-conversion-reshuffles-the-rng-stream]]).
- Releases still draw songs as now. The pool only logs what it would have offered: its best song's realized quality
  against the shipped song.
- Gate: byte-identical with the switch on.

### Phase 2: Best-of-pool singles, no bar

- An orphan single ships the pool's best perceived song instead of a fresh draw.
- Volume and format are unchanged, so this isolates the **selection** effect.
- Read: the realized-quality distribution of what ships, entry, who charts (head share, genre slots), and Top 10
  turnover. Risk 1 applies in full.

### Phase 3: The quality bar and waiting

- A roll whose act has nothing over the bar passes to the next eligible act, or lapses. That choice is open
  question 2.
- Volume falls. Read release volume against the Phase 0 anchor, label cash and closures, genre supply, and
  debuts per hit act.

### Phase 4: Commission and cover to fill

- Hot or valued acts get a commissioned professional song, or a recent-hit cover, with a fee and a delay. This runs
  through the existing Publishing / Cover-Song objects (`BuildProfessionalForSong`, the cover builders), never a
  parallel economy ([[publishing-cover-song-directive]]).
- Read: label costs, the material mix against `SourceMix`, and the Big-5 share (risk 7).

### Phase 5: Situational format and hit compilations

- Add the format rules in design point 4 on top of the margin model.
- Gate compilations on hits.
- Retire the lane's album override.
- Read: album titles and LP unit share, the album chart's genre mix, runtime, and weak-genre follow-up entry
  (§17af).

### Phase 6: Bundle screen and decade A/B

- The adopted phases together. Five seeds × 260 weeks paired, then the two-seed decade A/B against V3.1 guards.

## Calibration risks

1. **The chart has fixed slots.** Better songs do not add entries: the cutoff rises and the slots change hands. Any
   gain shows up as **who** charts, not how many. So results must be read on entries, slots and turnover, never on
   mean shipped quality. Acts with deep, strong pools (songwriters, acts at labels with writer access) gain
   relative to the rest.
2. **Supply-bound genre share.** Share tracks eligible records (r = 0.938, [[market-share-is-supply-bound]]). A bar
   that holds back more songs in genres whose pools run weaker cuts those genres' supply. Read genre error per seed
   and per genre (§17ae's method), from Phase 2 on.
3. **Album count is calibrated, and runtime depends on it.** Situational formats move album titles. Album titles set
   1960 LP unit share and runtime ([[album-1960-levers-bind-in-sequence]], [[album-unit-share-pinned-by-velocity]],
   [[runtime-is-album-count-bound]]), and the album chart's genre mix.
4. **Label economics.** Fewer, better releases change revenue, production spend and cash. Read Small-label closures
   ([[small-labels-never-lose-money]]), recoupment and advances. Commissioning is a new cost line.
5. **Revenue memory feeds back.** The format memories learn from realized nets. Changing what ships changes what
   the memories learn, and so the next format choice ([[album-creation-is-margin-driven]],
   `RevenueMemoryROIDirective.md`). Watch the album decision share year by year, not only at the end.
6. **RNG and hashes.** Any new draw on the main stream reshuffles the run. Pool titles feed traits through the
   title hash ([[song-titles-feed-track-traits]]), so a pool song's title must be fixed when it enters the pool, and
   must never be regenerated at release.
7. **Head concentration.** Professional-writer access scales with tier (`labelAccess` Major 0.90 against Small
   0.25). Commissioning and best-of-pool therefore favour the head. §17v showed that changes which look label-blind
   can reweight toward label resources on defaults. Read the paired Big-5 share and CR4 / CR8 in every screen.
8. **Omniscient judgement.** A label that reads songs exactly never ships a flop, and flops are part of history
   ([[closures-must-be-earned-not-imposed]]). The bar must read through perception noise, scaled by the label's A&R
   ability.
9. **Player parity.** The AI pool should be the same object as the player's repertoire, so the two compete on equal
   terms and later features (scouting intel on a rival act's material, song placement) can read it. The player's
   own release decisions are untouched.
10. **Save format.** Pool state is saved, so the save version changes. AI pools in a loaded world must restore
    exactly ([[panel-runtime-is-wiped-on-load]] is the failure to avoid).

## Targets and guards (read in every screen)

- **Targets:**
  - Hot 100 debuts per hit act toward history's 3.28 / 2.18 (Big-5 / other; §17ac).
  - Acts on the Hot 100 toward 386 a year.
  - Hot 100 records per charting act toward 2.03.
  - Cold-return slots toward 17 a year.
  - Weak-genre follow-up entry (§17af).
- **Guards:**
  - Hot 100 debuts about 690 a year.
  - Top 10 turnover: 105 records a year at 4.52 weeks each.
  - #1 tenure 2.55 weeks.
  - Big-5 Top 10 share, paired, and CR4 / CR8 in band.
  - Year-end genre error per seed, no worse than the control.
  - Album titles and LP unit share.
  - Small-label closures and label cash.
  - Performance departures within +20%.

## Open questions for the author

1. **The bar's reference:**
   - absolute (a realized-quality floor);
   - act-relative (the top of the act's own range); or
   - a label standard (by tier and archetype; a `TeenHitMachine` releases more and pickier than a small regional
     label).

   The recommendation is a label standard, read through perception.
2. **When nothing clears the bar,** does the roll pass to the next act (capacity kept, the roster's best material
   ships) or lapse (volume falls)? The recommendation is to pass, with a lapse when no act qualifies.
3. **The maximum wait** before an obligation forces a release, and how it relates to the contract's singles
   obligation.
4. **Player visibility:** should a rival act's pool be readable through scouting intel? That would be a later
   feature, but it fixes the object's shape now.
5. **The release-volume anchor:** we need a historical count of singles released a year, 1960-64. Without it,
   Phase 3 has no target.
