# Media Atmosphere Phase 4 notes: causal mode, player costs, the singles demand adapter

Status: built 2026-10-31, everything off by default. Directive: `MediaAtmosphereDirective.md` §5.4, §12.1-12.3, §15.2,
§16 Phase 4. The recognition writer is its own adapter (`MediaRecognitionAdapterDesign.md`). Phase 5, the paired decade
A/B, is still to run; these are short screens that can only catch a catastrophe.

## What is built

| Piece | Where | What it does |
|---|---|---|
| Causal mode | `MediaAtmosphere.cs` (`--causal-media-atmosphere`) | Runs the same media world as observe. Adds the player's real media costs, and lets the named writers run. `Observing` now means "the media world runs" (observe or causal); `Causal` is the new mode. AI media stays an allocation of the promotion envelope it already pays (§12.2), so causal mode adds no AI debit. |
| Activation boundary | `MediaSaveData.CausalFromDay` | Set to the first day the world runs causal (§15.2). Nothing before it is charged, and no airing before it asks for demand or recognition. A date confirmed in observe mode stays free. |
| Receipts | `MediaSaveData.Receipts` | One per debit or payment: `pitch\|id`, `produced\|id`, `fee\|id`. Saved, so reloading never charges twice. |
| Player costs | `MediaAtmosphereService.Player.cs`, `PlayerDesk.Media.cs` | A TV pitch costs one promo copy (of a record the label holds stock of), $0.15 postage and an hour at the desk. A press pitch adds an $1 photo. Confirming a date checks the label can cover its preparation (Standard $15, Extra $40, Minimal $0) and local travel. The bill comes when the date is produced, aired or cut; a preempted date is never billed. A network production pays guests' fares. |
| Performer fees | `MediaSaveData.PerformerFees`, `MemberWealthService.OnActYear` | A network date pays each active performer $150 scale (simplified assumption, AFTRA-style). It is booked by person and year and enters member wealth beside session pay, never label cash (§5.4). Local shows pay nothing. Player bookings only; AI fees stay estimates. |
| Demand adapter | `MediaDemand.cs`, `ChartManager.ApplyMediaDemand` | §12.3, singles only. See below. `--observe-media-demand` (shadow, logs only) or `--media-demand` (causal, requires causal mode). |
| Regional reach | `ExposureEstimate.ReachByRegion` | Each airing's unique reach split by market region. Null on airings saved before Phase 4, so old airings ask for nothing. |
| Recognition switch | `MediaRecognition.cs` | `--media-recognition` now requires causal mode (it is an economic writer) and only credits airings inside the causal window. |
| Telemetry | `MEDIA_DEMAND` yearly line, `<run>-media-demand.csv` | Per title and region-week: baseline, physical limit, request, frenzy, wanted, granted, adjusted, role, and the region's intent, transfer cap and units moved. |

### The demand adapter

- **Request.** An airing where the act sang the booked record itself (`PerformedSong.Record`), not cut, adds to that
  record in each region it reached:
  `Gain (0.35) × performance × fresh share × regional reach / regional population`.
  - Performance: Breakdown −0.5, Weak 0.3, Flat 1, Engaging 1.3, Electrifying 1.6.
  - Fresh share: 1 for a first visit to a program, 0.25 for a return, matching the reach tally.
  - The request carries at half strength each week after, so a date's effect is about two weeks of the first week.
- **Allocation, per region per week**, after serviceable intent and physical limits, before format budgets:
  - Each request is clipped to ±5% of the title (±10% when the act is a national sensation or cultural phenomenon,
    replacing the 5%).
  - A gain is also clipped to physical headroom.
  - A title with zero intent stays at zero.
  - At most 1% of the region's singles intent moves.
  - Gains draw first on titles asking down, then on neutral titles (each up to 5% of itself, in proportion).
  - Losses go first to titles asking up, then to neutral titles with headroom.
  - A title is a donor or a recipient, never both. Unmatched requests are cancelled.
  - Integer shares are floors plus largest remainders, ties broken by record id, so every cap holds after rounding.
- **Not included in the first build:** controversy sentiment, fan-magazine attachment and press items. They are the
  natural next request sources under the same ledger.

## Evidence

**Fixtures** (`MediaAtmosphereChecks`, 165 checks pass at `--seed=1001`):
- §16.1 #12-13:
  - a routine request is clipped to 5%, and a national sensation gets 10% instead;
  - three 5% requests in one region move only 1% of the region;
  - a title with no intent stays at zero, and a stock-bound title gets only its headroom;
  - all-up and all-down regions with no counterpart move nothing;
  - a bad night sends units to neutral titles with headroom;
  - six units in a region move nothing;
  - 600 random regions keep every invariant after rounding.
- §16.1 #14-15:
  - a causal pitch charges $0.15 and one hour once, with a receipt;
  - a produced Sullivan date bills the $40 rehearsal once, and pays four performers $600 once, none of it to the label;
  - after a save round trip the receipts hold;
  - a hometown local date with minimal preparation costs nothing and pays no scale;
  - dates confirmed in observe mode stay free.
- A live shadow pass: two weeks of airings leave requests on 74 records, all of them sung on the air (largest 4.3%).
  With the adapter off, nothing is written or decayed.

**Inertness**, 104 weeks, ref flags, against `ref-f50b9a4-<seed>-104`:

| Arm | Seed 1001 | Seed 2002 |
|---|---|---|
| `--causal-media-atmosphere` alone (`p4causal`) | 106/106 identical | 106/106 identical |
| `--observe-media-demand` (`p4dshadow`) | 106/106 identical | 106/106 identical |

The shadow's live rows keep every cap:
- routine maximum exactly 5.0%;
- no gain past a physical limit;
- no region over 1% (maximum 0.92%).

About 1,000 records gain, and the top 10 take 20% of the units moved.

**Causal screens** (`SimTools/analyze_media_recognition_screen.py`):

| | 104 wk, seed 1001 (`p4dmd`) | 104 wk, seed 2002 | 1963-66 resume (`dmd-c` vs `tvbook-s`) |
|---|---|---|---|
| Singles intent moved | 0.32% (1960) | 0.37% | 0.26 / 0.36 / 0.53 / 0.45% |
| Top 10% acts' slot-weeks | +0.79 pt | +0.10 pt | +0.16 pt |
| Singles units, worst year | +1.31% | +1.01% | +0.59% |
| Album units, worst year | −6.24% | −8.03% | −1.37% |
| Major-family share | +3.3 / +0.5 pt | −2.7 / +3.0 pt | −1.5 / −2.4 / −6.8 / −10.5 pt |
| Genres beyond ±1 pt | 5 (RnB +3.1, Soul −2.1) | 5 (RnB −2.6, TradPop +1.8) | Soul +1.4 |
| Closures | 88 → 85 | 73 → 70 | 108 → 108 |

- **The adapter is small and spread out.** It moves 0.26-0.53% of singles intent a year, the 1% region cap almost
  never binds, and concentration stays far inside the kill rule.
- **Where the units go:** 21-25% of the units granted each year go to Major-label records, drawn mostly from
  Independents (−15 to −22%). That is the chart lane: shows book what is charting.
- **Album units in the 104-week runs fail the ±1% limit on both seeds.** AI labels released fewer LPs (album decision
  share −0.7 and −2.5 pts in 1960). The resume does not reproduce it: −1.37% in its worst year, and album decision share
  matches the other arms. Two readings are open:
  - a real route through format choice, in the young 1960 LP market;
  - reshuffle, which these runs can't separate.

  The permutation diagnostic below rules out the first reading's dependence on targeting.
- **Major-family share in the resume falls 10.5 pts in 1966**, past the −6.0 that Recovery alone moved. The adapter's
  direct transfer points the other way (toward Majors), so the fall isn't its first-order effect. Read it in Phase 5.
- **Genre moves on the 104-week seeds go in opposite directions on the two seeds**, so they are noise at this length.
- **Cost:** the media step's cumulative time in the resume (77 s by 1966) matches the same-session recognition arms
  (84-93 s). The per-title CSV is about 210 MB for a 209-week resume.

**Combined, demand + recognition** (`dmd-rec` vs `tvbook-s`):
- top-10% acts' slot-weeks −0.49 pt;
- singles −0.69% to +1.37%, albums −2.66% to +1.73%;
- major-family share −1.6 / −2.5 / −9.2 / −12.7 pt;
- closures 108 → 123, the recognition writer's +15-19 seen before.

**Permutation diagnostic** (`--media-demand-permute`): the same requests are rotated among unrelated titles of about
the same size (blocks of five by intent), so about the same volume moves (0.26-0.50% a year) without television
choosing who gains.

| | Album units, worst year | Major-family share |
|---|---|---|
| 104 wk seed 1001: targeted `p4dmd` / permuted `p4perm2` | −6.24% / −5.49% | +3.3, +0.5 / +2.0, −3.6 pt |
| 104 wk seed 2002: targeted / permuted | −8.03% / −8.22% | −2.7, +3.0 / −10.4, +1.3 pt |
| 1963 resume: targeted `dmd-c` / permuted `dmd-perm` | −1.37% / −1.76% | 1965-66 −6.8, −10.5 / −1.3, −0.2 pt |

- **The 1960-61 album drop doesn't depend on who gains.** It appears with permuted requests too. It comes from moving
  singles intent at all (losses spread thinly over every neutral title, gains concentrated), or from reshuffle, in a
  young LP market where release decisions are marginal. By 1963-66 the LP market absorbs it.
- **The resume's Major fall goes with targeting on this seed:** permuted shows −0.2 to −3.2 pt, while both targeted
  arms show −10 to −13 by 1966. But the permuted seed-2002 run also moved Major share −10.4 pt in 1960, so a single
  seed swings that much on its own. The four-seed decade A/B decides it.

## Phase 5 decade A/B: preregistered plan (written 2026-10-31, before any decade result)

- **Runner:** `SimTools/RunCorporateEconomyAudit.ps1 -Profile game -Modes default -Weeks 522`, with the working bundle
  from `SimLogs/working-bundle/plan.json` as `-ExtraFlags`. One run at a time: the machine is shared, and the runner
  refuses to start while another Godot is open.
- **Seeds:** 1001 only. (Planned as 1001 and 1002 plus holdouts 3003 and 6006; on 2026-10-31 the author cut it to one
  paired seed, since each decade takes about two hours.)
- **Arms:**
  - `p5-ctl`: the bundle alone.
  - `p5-trt`: the bundle plus `--hit-history-signing-recovery --causal-media-atmosphere --media-recognition --media-demand`.
- **Read per seed and per year, with `analyze_media_recognition_screen.py` plus the §12.5 table:**
  - singles, album and combined units within ±1%;
  - active labels and artists within ±1%;
  - genre within 1 pt;
  - top-10% act slot-weeks (kill above +3 pt);
  - major-family share and CR4, reported explicitly;
  - closures by tier;
  - standing dates by years since the last hit, including 4y+ in 1967-69;
  - Sullivan fill.

  A pooled mean can't excuse a failing seed or year.
- **If the treatment fails:** attribution arms, one writer at a time, on the failing seed.
- **Calibration runs** write no lane log. `record-acts.csv` (added beside the lifecycles) gives the screens their act
  attribution.

## Phase 5 decade A/B result: seed 1001 (`p5ctl-s1001` vs `p5trt-s1001`, 2026-10-31)

Game profile, working bundle, 522 weeks. Treatment = `--hit-history-signing-recovery --causal-media-atmosphere
--media-recognition --media-demand`. Control 1h42, treatment 3h06 (part of the gap is paging: the machine had under
1 GB free).

**What the media writers were meant to do, they do:**

| | Control | Treatment |
|---|---|---|
| Standing dates to acts last hit 4+ years back, 1964 / 1966 / 1967 / 1968 / 1969 | 8 / 2 / 10 / 9 / 1 | 76 / 112 / 116 / 89 / 69 |
| Sullivan standing filled (of 36-38), 1964 / 1966 / 1967 / 1968 / 1969 | 21 / 14 / 19 / 17 / 23 | 36 / 32 / 36 / 38 / 36 |
| TV share of recognition gained | — | 11-16% |
| TV-active hitmakers' recognition, 1969 (no TV → with) | — | 0.252 → 0.289 |
| Singles intent moved by the adapter | — | 0.67-0.80% a year |

**§12.5 economy read (one seed, all four writers together, so not attributable):**

| Measure | Result | Limit |
|---|---|---|
| Top 1 / 5 / 10% acts' slot-weeks | −2.96 / −2.48 / −1.36 pt (less concentrated) | top 10% not above +3: pass |
| Singles units | within ±1% every year (worst −0.96%) | pass |
| Album units | −2.31, +3.87, +4.24 (1961-63); −3.78, −5.96, −1.71% (1966-68) | ±1%: fails 6 of 10 years |
| Genre slot-weeks, whole run | Soul −2.4, TraditionalPop +1.4, Blues +1.1 | 1 pt: fails |
| Major-family share | 1960-66 within −7.7 / +1.0; 1967 −14.4, 1968 −9.0, 1969 −0.9 pt | reported; against the 1968-69 consolidation goal |
| CR4 | 1965-68 −4.9 to −16.5 pt | reported |
| Closures (bankruptcies) | 467 → 420 | — |

- **The writers keep old names known and on bills.** 4y+ standing dates rise from about 0-10 a year to 69-116, and
  Sullivan's standing slots fill. That is the picture the recognition design asked for.
- **The bundle fails §12.5 on album units and genre, and cuts Major share by 9-14 pts in 1967-68,** the years the
  consolidation goal cares about. The 1963 resume screens pointed at the demand adapter for Major share (targeted arms
  only) and at demand transfers for album units (permuted too). Recovery alone also moved Major share by 6 pts on the
  resume. One seed with four writers together can't separate them.
- **Next, if wanted:** one attribution decade without `--media-demand` (recovery + causal + recognition) against the
  same control, about 2 hours. If album units, genre and Major share come back inside their limits, the demand adapter
  is the cause.

### Diagnosis from the seed 1001 logs (no new runs, 2026-10-31)

The genre and album failures, and part of the Major-share fall, come from **a wave of veteran acts re-signed by
Independents, from `--hit-history-signing-recovery`**. Recovery is in the treatment arm but isn't a media writer. The
demand adapter's direct transfers point the other way on every failing measure. The 1963 resumes, though, show the
targeted adapter cutting Major share by another route (see the end of this section).

- **The demand adapter pushes toward Majors and Soul.** Its net grants go to Major-label records every year 1960-68
  (+0.38M units in 1967, +0.30M in 1968), and +2.3M units to Soul in 1966-68. The treatment's Major chart units are
  about 20M lower in 1967, and Soul is down. The adapter granted 0.37% of the chart units of the veteran records below.
- **Recovery signs about 4x as many experienced acts.** Experienced-lane signings run 180-258 a year (1963-69) against
  32-91 in control. 1962-68: Independents 928 vs 259, Boutiques 233 vs 53, MidTiers and Majors 67 and 61 vs 0.
- **Those acts take the chart in 1966-68.** Acts 5+ years past their first chart hold 26.5% of slot-weeks, against
  15.4% in control:
  - 147 acts, against 38;
  - 47% of their slot-weeks on a label other than their first, against 8%;
  - 34% off Major labels, against 2%. Independent, MidTier and Boutique veterans add about 1,400 slot-weeks.
  - The veterans' genres make the genre signature: TraditionalPop 285 → 814 slot-weeks, RnB 562, Blues 209. Soul,
    carried by new signings, loses (new-act slot-weeks fall from 42.4% to 36.9%).
- **TV recognition doesn't carry them.** The re-signed veterans' launch recognition (0.27 effective, stock ×1.08) is
  lower than the veterans who stayed put (0.70, ×1.21) in both arms. They chart because they were signed and released.
- **The album gap is Major charted LPs, the same acts.** In 1966-68 Major album units are −3.2 / −5.3 / −4.7M, and
  nearly all of it is charted units. Treatment released *more* LPs (albums ever released +1-3%, decisions up), and LP
  revenue from Star and Superstar acts at Majors fell (8.4 → 6.3 $M, 1965-68). The 1961-63 swings (−2.3, +3.9, +4.2%)
  change sign, as the young-LP-market reshuffle in the 104-week screens did.
- **Part of the 1967 gap is the control's own spike.** Control Major share goes .62 → .76 → .67 in 1966-68
  (Decca + Mercury 32% of chart units in 1967). The treatment is flat at .58-.63. By 1967 the two charts share almost
  no acts (Σ|Δ act share| = 1.82 of a possible 2.0).

**The existing 1963-66 resumes of `media-p3-1001-1963` split the two causes.** That world was saved without the
working bundle, so this is a different economy, one realization per arm. Acts signed through the experienced lane
are counted over the whole window. Major share is the difference from control `tvbook-m`, 1964 / 1965 / 1966.

| Arm | Experienced signings a year | Their slot-week share | Off Major | Major share, pt | Mean |
|---|---|---|---|---|---|
| `tvbook-m` control | 29-52 | 1.7% | 1.1% | — | — |
| `tvbook-q` Recovery alone | 275-401 | 44.5% | 38.4% | −2.0 / −1.8 / −6.0 | −3.3 |
| `mrec-rc` Recovery + recognition | 249-394 | 38.6% | 33.7% | −2.0 / −4.0 / −0.3 | −2.1 |
| `mrec-c` recognition | 28-70 | 2.0% | 1.7% | +3.8 / −0.5 / +2.5 | +1.9 |
| `dmd-c` demand | 28-66 | 1.5% | 1.2% | −2.4 / −6.8 / −10.5 | −6.6 |
| `dmd-rec` demand + recognition | 26-51 | 1.1% | 0.8% | −2.5 / −9.2 / −12.7 | −8.1 |
| `dmd-perm` demand, permuted | — | — | — | −3.2 / −1.3 / −0.2 | −1.6 |

- **Recovery causes the veteran wave, with or without media.** It appears in both Recovery arms and in no other arm.
- **Targeted demand cuts Major share with no veteran wave.** The fall grows each year to −10 to −13 by 1966, while
  permuted demand stays near zero. The adapter's direct grants favour Majors, so the route is indirect (whose careers
  the TV-targeted units feed), and it's still unmeasured. The demand adapter isn't cleared on Major share.
- **Single-year Major share swings 6-10 pt between arms.** Read means over several years, not one year.

**Decision (2026-10-31): no standalone attribution decade.** The recognition writer is about to be replaced
(`CelebrityFanbaseDesign.md`), so the demand adapter's route to Major share gets a log diagnosis on the 1963 resume
arms, and the paired decade A/B runs once at the end of the branch. The arm considered was the decade without
`--media-demand` (Recovery + causal + recognition). It reuses `p5ctl` and `p5trt`. On the resume evidence:
- If demand is the Major cause, that arm's Major share comes back while genre and the 1966-68 LP gap stay, since they
  follow the veteran wave.
- If Major share stays down as well, Recovery is carrying it in the bundle economy.

### The demand adapter's route to Major share (log diagnosis, 1963-66 resumes, 2026-10-31)

Seven resumes of `media-p3-1001-1963` read together:
- control `tvbook-m`;
- four arms without demand: `tvbook-q`, `mrec-c`, `mrec-rc`, and the permuted `dmd-perm`;
- two targeted demand arms: `dmd-c`, `dmd-rec`.

The five non-demand arms set the reshuffle range. No new runs.

**Verdict: no mechanism found.** The demand adapter's own transfers can't explain the fall. The remaining signal is
weak and overlaps reshuffle noise.

- **Direct transfers favour Majors.** Net grants to Major records are +0.24 to +0.49M units a year. Independents lose
  0.20-0.38M. Both targeted arms show the same pattern.
- **Help before the peak is the same for every tier.** Every record that reached the top 10 got about +0.5% net grants
  before its peak, whatever its label (Independent 0.42-0.49%, Major 0.57-0.58%). Records peaking below the top 10
  were net donors on the way up (−0.4 to −1.1%). The adapter doesn't give small labels' rising records extra
  leverage.
- **Release volume and hit rates match.** Major releases (1,653-1,695), chart rate (0.37), Top 40 rate (0.15-0.17) and
  Independent rates match across all seven arms. Major chart-weeks over 1963-66 are 2-8% below control in the demand
  arms, a few hundred slot-weeks.
- **What does differ is the top 10.** Major top-10 hits from 1963-66 releases:

  | Arm | Major top-10 hits |
  |---|---|
  | Non-demand arms (`tvbook-m`, `tvbook-q`, `mrec-c`, `mrec-rc`, `dmd-perm`) | 139-156 |
  | `dmd-c`, `dmd-rec` (targeted) | 133, 127 |

  - Independent top-10s run 120-149 without demand and 138 and 152 with it.
  - Both targeted arms below all five references has odds of about 1 in 21 by chance, before allowing for how many
    measures were looked at.
  - The deficit is spread across new signings and established acts, and across years (1965 is the clearest). No single
    act or label carries it.
- **Per-label deltas are reshuffle-shaped.** The per-label changes correlate 0.38-0.58 between the two targeted arms
  and 0.47-0.64 with the permuted arm. The 1966 Independent gain (+9M charted units in both targeted arms) is spread
  over many labels, none above 1-2M.
- **Unit weighting magnifies a few hits.** Major-family share is unit-weighted, and top-10 records carry most units.
  So 10-15% fewer Major top-10 hits shows up as 10-13 points of share in the single year (1966) where a handful of
  hits decide it.

So the targeted adapter may cost Majors about one top-10 hit in ten, through no route the logs can see, or it may be
reshuffle. It doesn't work through who receives the transfers.

**Design consequence** (`CelebrityFanbaseDesign.md`): follower demand doesn't route through this ledger, so this
question doesn't block it. The end-of-branch decade A/B reads Major top-10 counts beside unit share. A decisive test,
if one is wanted, is a resume arm whose requests are permuted only among titles of the same tier and size. If the deficit
survives, targeting within tier is the cause.

Recovery needs its own look either way. It sends old hitmakers to Independents, which works against the 1968-69
consolidation goal.

## Open → built 2026-11-04 (all opt-in, unscreened)

The three leftovers are built in `MediaAtmosphereService.DemandSources.cs`. Each is off by default. None has been
screened in a world yet; each should get its own short screen before it joins a bundle.

| Piece | Switch | What it does |
|---|---|---|
| Press requests | `--media-demand-press` (with `--media-demand` or `--observe-media-demand`) | After the weekly press pass, an item in a teen fan magazine or a national mainstream magazine asks for its record in every region: `PressGain (0.10) × fit-weighted readers × angle (feature 1, news 0.2) × tone (Admiring 1.2, Neutral/Curious 1, Skeptical 0.5, Alarmed 0) ÷ national population`. A Life feature asks ~1.3%; a teen-magazine feature ~0.1%. One credit per root event, at the widest outlet (`MediaSaveData.DemandRoots`). Trades, critics, TV notices/advances (the broadcast's own root), standards/controversy items, corrections and label pitches ask nothing. |
| Controversy requests | `--media-demand-controversy` (same requirement) | A public controversy (today: on-air defiance) asks `−0.12 × severity` of the act's booked record: national everywhere (−7.2% at severity 0.6), local only in the program's region. A national controversy at severity ≥ 0.5 is the §12.3 "major controversy": the title gets the 10% ceiling instead of 5%. No station or retailer refusal (that's a supply adapter). |
| AI performer fees | `--media-ai-performer-fees` (requires causal) | A rival act's produced network date pays each active member $150 scale (a resident's episode pays the residency's per-episode scale) into `PerformerFees`, read by member wealth like a player date. By receipt, inside the causal window only; never label cash. `MEDIA_AI_FEES` yearly line. |
| Player-cost UI | (causal mode) | The office's PRESS & TELEVISION header and pitch lines state real costs in causal mode. Each booked date shows its bill when produced (rehearsal, travel) and the scale the production pays the band. Dates confirmed in observe mode say they're free. A "This year" line totals the label's media cash and the scale paid to its acts' members. |

All three sources write the one `DemandLift` ledger, so the allocator's caps hold across TV, press and controversy
together. The yearly `MEDIA_DEMAND` line adds `press[...]` and `controversy[...]` counters when a source is on.

Fixtures: `MediaAtmosphereChecks` 293 pass at `--seed=1001` (10 new): the Life feature's request size, root dedupe
and repeated passes, the excluded items, teen news at a fifth, national and local controversy, the frenzy ceiling, both
sources inert when off, and AI fees once by receipt, with none on local shows, player bookings or before the causal boundary.

Inertness with the switches off: 104 weeks, seed 1001, `--enable-genre-market-v2 --enable-artist-population-lifecycle
--aggregate-only --log-band-life-members --contact-network-audit --causal-media-atmosphere --media-demand`. The new build
`mdsrc-new-1001-104` and the pre-change control `mdsrc-ctl-1001-104` match on 108/108 CSVs, byte for byte. Under
`--default-simulation-profile` the media suite fails one existing check ("The Ed Sullivan Show declines 7 uncharted
records") on the pre-change code too, so that failure predates this work.

Still to do: a short screen of each source alone (singles and LPs within ±1%, Major top-10 counts), then the AI-fee
accounting look (member wealth and departures by tier, with fees on and off).
