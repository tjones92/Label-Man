# Celebrity fanbase Phase 2b notes: critics count through acclaim

Status: built 2026-11-01, off unless named. As specified it halved all acclaim (not adoptable); with the neutral floor it is adopted (see the last section).
Design: `CelebrityFanbaseDesign.md` §4.3. Directive amendment recorded in `MediaAtmosphereDirective.md` §12.4.

## What is built

| Piece | Where | What it does |
|---|---|---|
| Switches | `MediaCritics.cs` | `--observe-critic-acclaim` (shadow: a media-owned shadow acclaim per act) and `--critic-acclaim` (causal: writes `criticalAcclaim`, requires `--causal-media-atmosphere`). |
| Verdict | `MediaCritics.Heard`, `MediaPress.CriticTone` | Merit craft (`ArtistCriticalAcclaimService.RecordCraft`) + τ × (taste − 0.5), τ = 0.35. The printed tone reads the same verdict, keyed the same way. |
| Review column | `MediaAtmosphereService.Critics.cs`, `CriticsWeek` | Each week, every publishing critic and specialist paper fills its next issue's column from LPs released that week in its readers' music (outlet fit ≥ 0.7; compilations and soundtracks excluded). Picks read fit, the act's public standing and a keyed spread, never craft. Slots per issue: Down Beat, Sing Out!, Crawdaddy!, Rolling Stone 6; Ebony and the country papers 3; Jet, Defender, Voice, Broadside 2; others 1. |
| Ledger | `MediaSaveData.Reviews`, `ReviewColumn`, `ShadowAcclaim` | Reviews by record (outlet, issue day, authority, taste), one per outlet. Chart-entry reviews from the news pass are logged too. Kept apart from the news items, so no other media reader sees the column. Saved. |
| Authority | `MediaCritics.BaseAuthority` × outlet fit | Down Beat 0.6, Sing Out! 0.5, Broadside 0.2, Jet/Ebony/Defender 0.3, Voice 0.15 → 0.35 by 1967, Crawdaddy! 0.3, Rolling Stone 0.3 → 0.6 by 1969, other specialist and underground papers 0.1. |
| Acclaim | `ArtistCriticalAcclaimService.OnChartRunComplete` → `MediaAtmosphereService.CriticAcclaimDelta` | At a completed run the record's published reviews are consumed once. Coverage = 1 − (1 − 0.5) ∏(1 − a); verdict = craft + authority-weighted taste (the trades' floor reads craft). The climb × coverage; erosion unscaled. |
| Telemetry | `MEDIA_CRITICS` (year end), `LANDMARK` (per landmark, under `--enable-album-legitimacy`) | Reviews by source and outlet, LPs eligible/reviewed, verdict shift, coverage and old/new climb and erosion by Art/Pop × LP/single, acclaim distribution (real and shadow). |

**Fixtures** (`MediaAtmosphereChecks`, 230 pass at `--seed=1001`, 12 new): unreviewed = half the climb; erosion unscaled;
Down Beat's authority for jazz vs a teen record; the authority ramps; taste centred; a harsh review sinks a record just
over the bar, a warm one lifts it; a reviewed underrated LP keeps 90% of its climb; in the world the column reviews only
LPs, in the readers' music, one per outlet, never as news items; the ledger survives a save round trip; consumption
reads once (shadow leaves real acclaim alone, causal returns the delta, a second read is unreviewed).

## Shadow inertness

104 weeks, ref flags (`--aggregate-only`) + `--observe-critic-acclaim`, against `ref-f50b9a4-<seed>-104`
(`cr2bobs-<seed>-104`): **98/98 written CSVs byte-identical on seeds 1001 and 2002.** The 8 census files were not
requested, as in Phase 2.

1960 shadow (seed 1001 / 2002):

| | |
|---|---|
| Reviews, column / chart entries | 849 / 29; 824 / 30 |
| LPs eligible / reviewed / other genres | 283 / 253 / 318; 274 / 246 / 275 |
| Mean authority | 0.218 |
| Verdict shift, mean / absolute | +0.001 / 0.033; −0.015 / 0.037 |
| Runs consumed (Art LP / Art single / Pop LP / Pop single) | 60 / 580 / 96 / 2,070 |
| Of them reviewed | 15 / 13 / 1 / 0 |
| Climb, old → new: Art LP / Art single / Pop single | 0.00 → 0.03 / 16.9 → 8.5 / 78.0 → 39.0 (seed 1001) |
| Erosion, old → new | unchanged |
| Acclaim of active acts, mean (art / pop), real → shadow | 0.012 → 0.008 / 0.014 → 0.008 |

## Findings

1. **No LP earns acclaim before 1966, reviewed or not. A baseline bug blocks the design's main case.**
   `ArtistCriticalAcclaimService.GetCraftScore` calls `ArtisticMeritService.GetCraft` without `bodyOfWork`, so an album's
   coherence term is `thematicCohesion`, pinned at its 0.08 era floor before 1966 ([[smoothstep-cohesion-bug]]). Album
   craft is then about 0.4 × originality + 0.03 + 0.25 × label production, which is under the 0.58 bar for nearly every
   record. The merit path the landmark rule reads does pass `bodyOfWork`. The service's own comment says critics and
   the landmark rule are "demonstrably reading the same record"; they aren't. In 1960 the LP climb is 0.00-0.02 in total
   across 150 LPs, and every LP only erodes.
2. **So acclaim is made by singles, and singles are almost never reviewed.** The press reviews chart-entry singles only
   in critic-fit genres (13 of 580 art singles, none of 2,070 pop singles in 1960). In practice 2b today halves every
   climb through the trades' floor.
3. **Acclaim is tiny everywhere.** Mean 0.012-0.014, p90 0.006-0.033, 9-18 acts at the "critics' darling" 0.55. The
   landmark rule reads max(reputation, acclaim) × 0.35, so reputation dominates there. Acclaim's other economic reader is
   the critical term in evolution pressure.
4. **Acclaim has no economic reader under the canonical flags.** Landmarks need `--enable-album-legitimacy`, cultural
   memory `--enable-cultural-memory`, the acclaim pressure `--enable-evolution-pressure`. A causal screen has to carry
   them in both arms, so it needs its own control. Planned: 1963-66 resumes (`--weeks=209`) of `media-p3-1001-1963` and
   `asc-2002-1963`, base flags + `--aggregate-only --lean-probe --causal-media-atmosphere --enable-album-legitimacy
   --enable-cultural-memory --enable-evolution-pressure`; the treatment adds `--critic-acclaim`.

## The LP craft fix (author's go-ahead, 2026-11-01)

`GetCraftScore` now passes the album's `bodyOfWork` to `GetCraft`, as merit does. Not switched: acclaim has no reader
under the canonical flags. Re-gated (`cr2bobs2-<seed>-104`): **98/98 byte-identical on both seeds**, 230 fixtures pass.
The 1960 LP climb (old delta) went from 0.00-0.02 to 3.8-4.8 for art LPs and 5.4-6.6 for pop LPs.

## Causal screen, 1963-66 resumes

`cr2b-c` / `cr2b-t` (`media-p3-1001-1963`) and `cr2b2002-c` / `cr2b2002-t` (`asc-2002-1963`), flags as planned above.
Both arms carry the LP craft fix. Slot-weeks are `lifecycles` weeksOnChart joined to `record-acts` (`--aggregate-only`
leaves `records.csv` empty); this reproduces Phase 2's `fr2-t` vs `mf1-c` at +1.15 pt against the recorded +1.27.

**What the critics did** (treatment, seed 1001 / 2002, per year 1963 → 1966):

| | |
|---|---|
| Reviews, column / chart entries | 1,086 → 1,428 / 25 → 134; 1,076 → 1,411 / 19 → 106 |
| LPs eligible → reviewed | 592 → 466 … 1,667 → 892; 610 → 468 … 1,420 → 838 |
| Art LPs reviewed at run end | 7% (1963) → 35% (1966); 5% → 36% |
| Pop singles reviewed | 0 in every year |
| Coverage, mean: art LP / pop single | 0.52-0.58 / 0.50 |
| Climb, new ÷ old: art LP / pop LP / singles | 0.52-0.59 / 0.50-0.51 / 0.50 |
| Verdict shift, mean / absolute | ±0.004 / 0.030-0.037 (no drift) |
| Acclaim, mean, art / pop (1966) | 0.030 / 0.036; 0.027 / 0.032 |
| Critics' darlings (≥ 0.55), art / pop (1966) | 19 / 36; 17 / 65 |

**What acclaim's readers did** (control → treatment):

| | Seed 1001 | Seed 2002 |
|---|---|---|
| Landmarks 1963-66 | **2 → 0** | 0 → 0 |
| CriticalBreakthrough conversions | **60 → 23** | **51 → 24** |
| All ratified conversions | 568 → 547 | 498 → 515 |
| Mean critical pressure | 0.113 → 0.073 | 0.116 → 0.077 |

The control's two landmarks are both 1966 LPs that peaked outside the Top 40 (No. 68 rock and roll, No. 56 bossa nova),
carried by acclaim (0.72, 0.68) well above reputation (0.29, 0.05): the underrated case the design is for. The
treatment loses both.

**Economy** (t − c):

| | Seed 1001 | Seed 2002 |
|---|---|---|
| Top 10% acts' slot-weeks (kill +3 pt) | −0.31 pt | +1.02 pt |
| Major-family share 1963 / 64 / 65 / 66 | 0.0 / +0.9 / +5.4 / −1.9 | +2.5 / −5.6 / +2.8 / −2.2 |
| CR4 1966 | −2.1 | −0.3 |
| Singles / albums 1966 | −0.2% / +0.4% | +1.9% / **−4.8%** |
| Genre beyond ±1 pt | none | **Soul −3.1**, RnB +1.9, Country −1.1 |
| Closures | 121 → 122 | 84 → 101 |

- No catastrophe; concentration within the kill rule, Major share both signs.
- Seed 2002's 1966 albums fall again, as in Phase 1 (−3.7%), Phase 2 (−4.3%) and 2c (−3.1%, −4.8%). Four different
  writers giving the same shape on one world, with 2b's channel unrelated to the others (acclaim → evolution pressure,
  no recognition or demand write), points to this world's reshuffle sensitivity rather than a shared mechanism.
- Soul −3.1 with RnB +1.9 on one seed reads as genre conversions moving (CriticalBreakthrough halved), not demand.

## Verdict: works as specified, works against its purpose

The writer does what §4.3 says. But in these worlds it removes acclaim rather than routing it:
1. **The trades' floor halves every climb**, and the columns lift almost nothing back. Even a reviewed art LP's coverage
   averages 0.58, because most column reviews come from low-authority papers (mean authority 0.24-0.26) and a third of
   art LPs at most get reviewed at all.
2. **Pop is not hit harder than art.** Pop singles keep exactly half, art LPs 52-59%. "Pop acclaim falls before 1966"
   happens only in the sense that all acclaim falls. Pop acts still outnumber art acts among the darlings 2-4 to 1.
3. **It deletes the underrated landmarks.** The two the control produced were the acclaim-led, outside-Top-40 records
   the design wants more of.
4. **Its economic effect is genre conversions**: CriticalBreakthrough conversions halve on both seeds. Other triggers fill
   most of the gap, so total conversions barely move.

Not adoptable as specified.

## Options

1. **Neutral floor (recommended).** Scale the climb by coverage relative to the floor, coverage ÷ 0.5, so an
   unreviewed record keeps today's climb and a review adds to it (Down Beat on a jazz LP ×1.6, Down Beat + Sing Out!
   ×1.8). Critics then add acclaim where they wrote, and nothing else changes. Pop acclaim does not fall; art acclaim
   rises relative to it. Risk: more acclaim-led landmarks and CriticalBreakthrough conversions in jazz, folk and soul.
2. **Genre-dependent floor.** The trades' picks count for full coverage in pop and half in critic-fit genres. Pop
   untouched, art acts must be reviewed to be heard. That is the opposite of the history (critics added to art music),
   so not recommended.
3. **As specified, with wider reach.** More column slots and higher authority so coverage averages near 1 for art LPs.
   This reaches option 1 by calibration and still halves every unreviewed single.

## Neutral floor (author's choice, 2026-11-01): adopted

`MediaCritics.Delta` now scales the climb by coverage ÷ `TradeFloor`, capped at the service's per-release ceiling
(`MaxTotalGainPerRelease`). An unreviewed record keeps today's climb exactly; Down Beat on a jazz LP gives ×1.6, Down
Beat + Sing Out! ×1.8. Fixtures updated, 230 pass. Rescreen: treatments `cr2bn-t` / `cr2bn2002-t` against the same
controls `cr2b-c` / `cr2b2002-c`.

| | Seed 1001 | Seed 2002 |
|---|---|---|
| Economy CSVs vs control | 16 same, 78 differ (identical through 1965; diverges in 1966) | **93 same; only `artist-evolution.csv` differs** |
| Landmarks 1963-66 | 2 → 2 (same records; the bossa nova LP's acclaim 0.68 → 0.86) | 0 → 0 |
| CriticalBreakthrough conversions | 60 → 60 | 51 → 53 |
| All ratified conversions | 568 → 556 | 498 → 498 |
| Top 10% acts' slot-weeks | +0.11 pt | 0.00 |
| Major share / CR4, 1966 | +0.8 / +1.0 pt (1963-65 zero) | 0 / 0 |
| Units, genre beyond ±1 pt, closures | 0.00%, none, 121 → 121 | 0.00%, none, 84 → 84 |
| Climb, new ÷ old, 1966: art LP / art single / pop LP / pop single | 1.19 / 1.01 / 1.02 / 1.00 | 1.18 / 1.02 / 1.02 / 1.00 |
| Acclaim mean 1966, art / pop; darlings art / pop | 0.053 / 0.061; 88 / 187 | 0.050 / 0.053; 102 / 167 |

**Verdict: adoptable, off by default.** The critics now add acclaim where they wrote and nothing elsewhere:
- art LPs gain about 18% more acclaim in 1966;
- singles and pop are unchanged;
- the underrated landmarks survive and are carried more strongly.

In 1963-66 it is close to economically inert: one seed byte-identical, the other diverging only in its last year.

What it does not do:
- **Pop acclaim does not fall before 1966.** That was the price of the neutral floor. Pop acts still lead the darlings
  about 2 to 1.
- **The effect is back-loaded.** Crawdaddy! arrives in 1966, Rolling Stone in late 1967, and the Voice's and Rolling
  Stone's authority ramps to 1967-69. So 1963-66 is the quiet part. The paired decade A/B at the end of the branch
  should carry `--critic-acclaim` with the landmark, cultural-memory and pressure readers on, and should check
  landmarks per decade (25-40) and their genre mix.
