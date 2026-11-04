# Celebrity fanbase Phase 2c notes: event hype and registration fit

Status: built 2026-11-01, off unless named, **not adopted**. The author scoped breakouts to phenomena instead (design
§3.4.6, decision 8). The constant registration fit now runs only under `--event-hype`; without it, act registration
reads `AudienceFit` as Phase 2 was screened. The Phase 2 fixtures are back to 0.016 / 0.078 / 0.011, and 218 checks
pass. Design: `CelebrityFanbaseDesign.md` §3.4. It
follows Phase 2's finding (`CelebrityFanbasePhase2Notes.md`, *What keeps the biggest airings small*).

## What is built

| Piece | Where | What it does |
|---|---|---|
| `--event-hype` | `MediaFans.EventHype`, read in `MediaAtmosphere.Configure` | Turns on the three pieces below. |
| Episode memory | `MediaFanState.RecentHigh/RecentHighDay`, `MediaFans.NoteRecentHigh/RecentHighOf` | The strongest fan-episode state in the last 56 days. Cooldown counts as none. Written only under the switch. |
| Episode hype | `Hype` (Press.cs) | Hype = max(precursor, EpisodeHype) × novelty × (0.3 + 0.7 × coverage), capped by `HypeCap` of the remembered state. EpisodeHype: Regional Favorite 0.35, Sensation 0.70, Phenomenon 1.0. |
| Bookers chase | `PlaceHolds`, `HoldPrecursor`, `ProgramReach` | A national episode in memory earns a headline date while its record is in the Top 40, at the episode's hype. Programs are tried biggest audience first. The advance press reads the same figure. |
| Registration fit | `MediaRecognition.RegistrationFit` = 0.6 | Under `--fame-registration`, act registration ignores taste (not gated by `--event-hype`). Member fame (Phase 1) still reads `AudienceFit`. |
| Telemetry | `MEDIA_EVENT` (+ precursor, episode, state, novelty, coverage, outcome), `MEDIA_EVENT_HOLD` per chased date | |

Not built: the player's act being *offered* a chased date (the design's §3.4.1 point 2 for the player). Rival acts only,
as `PlaceHolds` already was.

**Fixtures** (`MediaAtmosphereChecks`, 217 pass at `--seed=1001`, 6 new):
- a cooled sensation still lifts within 8 weeks, and not after;
- Sullivan's audience: an ordinary No. 1 draws the usual audience, a sensation's first event night 1.31× it, a
  phenomenon's 2.5×;
- a second event night on a program is damped back to routine;
- at the world's Sullivan base (0.166), routine nights register 0.025 (0.040 electrifying), never a breakout;
- a teen act registers as well as a comedy act;
- a sensation's first event night breaks out only when electrifying (0.052); a phenomenon's on any decent night (0.062).

The Phase 2 registration fixtures still pass at the new fit: a routine Sullivan spot 0.021, the 38% night 0.091, the
next week 0.012.

## Screens

**Shadow inertness:** 104 weeks, ref flags + `--observe-media-recognition --fame-registration --observe-following
--event-hype`, seeds 1001 and 2002 (`fe2cobs-<seed>-104`): **98/98 CSVs byte-identical** to `ref-f50b9a4` on both.

**Causal 1963-66 resumes:** `fe2c-t` / `fe2c-t2002`. Flags: Phase 2's `fr2-t` set + `--event-hype --lean-probe`.
Economy against Phase 1's `mf1-c` / `mf1-c2002`.

| | Seed 1001 | Seed 2002 |
|---|---|---|
| Breakouts | **0** | **0** |
| Chased headline dates | 22 (16 on Sullivan) | 15 (7 on Sullivan) |
| Chased nights with any audience lift | 0 | 1 (Hollywood Palace, hype 0.51, lift 0.32, 8.2M) |
| Novelty on chased nights | 0.34 on all but one | 0.34 on all but one |
| Advance coverage on chased nights | 0.2-1.0, mostly 0.6-1.0 | 0.4-0.8 |
| TV applied, 1963 / 64 / 65 / 66 | 3.8 / 3.2 / 4.1 / 3.6 (Phase 2: 3.0 / 2.2 / 3.1 / 2.7) | 4.1 / 3.4 / 4.4 / 3.9 (3.0 / 2.6 / 3.3 / 2.9) |
| TV share of recognition gained | 4.4-9.4% | 4.8-10.6% |
| Top 10% acts' slot-weeks (kill +3 pt) | **+2.38 pt** (Phase 2: +1.27) | +0.89 pt (+1.72) |
| Major-family share 1963 / 64 / 65 / 66 | −0.5 / −2.5 / −5.4 / −7.1 | +0.5 / −4.2 / +1.5 / −10.7 |
| Genre beyond ±1 pt | **Soul +2.7**, RnB −1.3, Country −1.0 | **Soul +1.4** |
| Albums | +0.6 / +1.6 / +1.0 / +0.2% | +1.0 / −1.5 / +0.1 / **−3.1%** |
| Closures | 108 → 109 | 105 → 95 |

### Why the event nights didn't happen

**A sensation has always played the show already.** Entering National Sensation needs national exposure in the last
four weeks (`MediaFans.Level`), and Sullivan books two current hits a week. By the time an act is a sensation it has
been on Sullivan as a chart guest. Hype's novelty term is ×0.4 for a return to the program and ×0.85 for primetime:
0.70 × 0.34 × (0.3 + 0.7 × 0.8) = 0.20. That is under the 0.25 where lift begins. The design's "sensation's first event
night" has no route in the world.

Lifting the novelty term alone wouldn't produce breakouts either. Sullivan's regulars already saw the act, so the
act's recently-seen stock holds most of a lifted audience, and registration novelty (national dates before) is
0.4-0.65 by then. A Sullivan return at full lift adds about a third more viewers, mostly fresh: roughly 0.05 × 0.25 ×
0.6 × 0.5 ≈ 0.004.

**So in this world TV meets hot acts early, as a routine guest, and spends their novelty before they are hot.** A
TV-made breakout would need an act's *first* big exposure to be an advertised event night. That means the booker
spotting a rising act before the routine chart booking finds it, as Sullivan did with the Beatles.

### Economy

- Registration fit 0.6 raised TV recognition about 1.3× on both seeds, as predicted.
- **Soul gained on both seeds (+2.7, +1.4)**, the risk the design named. Soul acts on family shows had the lowest fit
  (0.37) and gain the most from a flat 0.6. Phase 2 had Soul +1.3 on seed 1001 only. Two seeds with the same sign is
  the first genre move here that doesn't read as reshuffle.
- Seed 1001's top-10% share is up 2.38 pt, inside the kill rule but twice Phase 2's.
- Major share falls by 1966 on both seeds (−7.1, −10.7). Phase 4 and Phase 2 showed the same on these worlds, in both
  directions between years.
- Seed 2002's albums fall in 1966 again (−3.1%).

**Verdict.** Inert where it should be, and the writers and telemetry work. But the event-hype half of the mechanism
never fires, and the fit half is a genre lever (Soul) more than a breakout lever. Neither should be adopted as it
stands.

## Book the rising act (option 1, built 2026-11-01)

Author's choice after the first screen.

**What is built** (all under `--event-hype`):
- `RisingAct`: a Regional Favorite or more in the last 8 weeks, with no primetime variety appearance yet.
  - Its record earns a hold down to No. 20 (`MediaFans.RisingChartDepth`), at hype `RisingHype` 0.75 (the calibration
    knob), capped as a sensation (`RisingCapState`).
  - The hold is marked `MediaHold.Rising`; the advance press reads it.
- **Exclusivity** (`Available`): a rising act on hold plays no primetime variety show before its date.
- Fixture: the rising night (hype 0.64, 0.285 of the country) breaks out on Engaging (0.049) and not on a routine night
  (0.043). 218 checks pass.

**Gates:** `fe2robs-<seed>-104`, 98/98 byte-identical on both seeds.

**Causal resumes** `fe2r-t` / `fe2r-t2002`, same flags as `fe2c-t`:

| | Seed 1001 | Seed 2002 |
|---|---|---|
| Breakouts | **0** | **0** |
| Rising holds / chased national holds | 6 / 28 | 8 / 15 |
| Rising nights with lift, and the largest | 4 of 5 aired; Sullivan 9/4/66 hype 0.75, lift 0.78, **41.5M** | 6 of 8; Sullivan 9/26/65 hype 0.75, lift 0.78, **41.5M** |
| Outcomes on rising nights | all FlatAdequate | FlatAdequate 6, Weak 2 |
| Headliner outcomes, both seeds, all events | Electrifying 1, Engaging 20, FlatAdequate 63, Weak 6 | |
| Largest single gain | 0.0245 (an ordinary Sullivan first spot) | 0.0245 |
| TV applied, 1963 / 64 / 65 / 66 | 3.9 / 3.6 / 4.3 / 4.0 | 4.0 / 3.4 / 4.3 / 3.9 |
| Top 10% acts' slot-weeks | +0.95 pt | +1.52 pt |
| Soul / other genre beyond ±1 pt | **+1.6**; RnB −1.1, EasyListening −1.4 | **+1.7**; TeenPop −1.5 |
| Albums 1966 / singles 1966 | **−3.7% / +1.8%** | **−4.8% / +2.1%** |
| Closures | 108 → 124 | 105 → 96 |

**The event night now happens.** Two Sullivan nights reached full lift, 41.5M viewers against Sullivan's usual
20-27M: the Beatles-shaped night the model never produced before. Rising holds cluster in August-September: through the
season, routine chart booking reaches rising acts before a rising hold can.

**It still made no breakout, for three reasons:**
1. **No rising night went well.** 0 of 13 aired were Engaging, against about 22% of all headliners. The design needed
   an Engaging night.
2. **The headliner's own fresh audience is smaller than the night's.** The Turner Family's 41.5M night isn't among the
   year's top gains, while another act on the same bill registered fresh 0.296. The rising test excludes primetime
   only, so daytime network shows (Bandstand) have already shown the act to much of the audience.
3. **The lift goes to the whole bill.** The event audience stays for everyone, as the exposure model says. The
   supporting act on a full-lift night got the freshest audience in the run.

**Economy.**
- Soul +1.6 / +1.7 on both seeds again, from the flat fit.
- New this time: 1966 albums fall on both seeds (−3.7%, −4.8%) and singles rise (+1.8%, +2.1%).
- Concentration is within the kill rule.
- Major share moves both ways.

**Verdict.** Event nights exist now. Breakouts don't, and the album/singles shift and Soul's gain are two-seed signals
against it. Not adoptable as is.

## Options

1. **Book the rising act first.** Chase a Regional Favorite with strong breadth, before routine booking finds it,
   with novelty intact. Hype 0.35 × 1 × (0.3 + 0.7 coverage) ≈ 0.25-0.35, so EpisodeHype for a Regional Favorite would
   have to rise to lift the audience at all. Risk: dates handed to acts that never become hits.
2. **Hold the routine chart booking back for a rising teen act** until its headline date, so its first Sullivan is
   the event. That changes what Sullivan books every week, which is a bigger economic change.
3. **Scope breakouts to phenomena** (0-1 a decade, design §11 decision 1). TV-made stars then come through followings
   and residencies (Phase 3), not single nights. Drop or rescale the "handful a decade" target in §9.
4. **Registration fit:** revert to `AudienceFit`, or a middle value (e.g. 0.5 + 0.5 × AudienceFit), to keep the Soul
   move under 1 pt.
