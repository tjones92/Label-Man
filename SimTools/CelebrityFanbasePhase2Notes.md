# Celebrity fanbase Phase 2 notes: registration, press and controversy write the act's name

Status: built 2026-11-01, off unless named. Design: `CelebrityFanbaseDesign.md` §3.1-§3.2, §4, §10 Phase 2. This phase
uses directive amendment §0.3 (press and fan magazines may write canonical recognition), recorded in
`MediaAtmosphereDirective.md` §12.4.

## What is built

| Piece | Where | What it does |
|---|---|---|
| Switch | `MediaRecognition.cs` (`--fame-registration`) | Modifies the existing TV recognition adapter. With `--observe-media-recognition` it is shadow (media ledger only); with `--media-recognition` it is causal (writes `publicRecognition`). Alone it throws. |
| TV registration | `RecognitionDeposit`, `MediaRecognition.Registered` | Replaces K × reach growth and the 0.06 yearly cap. |
| Press writer | `MediaAtmosphereService.FameWriters.cs`, `RecognitionPressWeek`, after the press pass | Each item whose issue came out this week deposits its registered readers. |
| Root-event budget | `MediaSaveData.FameRoots`, `CreditRoot` | One story counts once, at the strength of the widest outlet that ran it. The TV appearance credits its own root (`tv|<appearance>`), so a notice of the same night adds only readers beyond its viewers. Roots are forgotten after 26 weeks. |
| Controversy writer | `ControversyFame`, called where a defied standards query becomes a controversy | Notoriety into recognition; the following split (§4.2). |
| Reader joiners | `FollowingWeek` (with `--observe-following`) | Teen fan and specialist readers join followings (`cRead`). |
| Telemetry | `MEDIA_FAME_WRITERS` (year end), `MEDIA_TV_BREAKOUT` (per airing), `joinPress` and `controversy` in `MEDIA_FOLLOWING` | Press items and applied gain by family, items absorbed by their root, controversies, critics' items, breakouts, media share of all recognition gained, the recognition distribution. |

### Arithmetic, as built

- **Television** (§3.1): fresh people = growth of the act's recently-seen stock (`RecognitionReach`), so a return to the
  same audience adds almost nothing. Registered = fresh × r0 0.25 × audience fit (weighted by the airing's young and
  adult viewers) × performance (0.6 Breakdown … 1.6 Electrifying) × novelty (1 → 0.4 with national dates already seen)
  × attention. Gain = registered ÷ population, then `AddPublicRecognition`'s diminishing returns. **No yearly cap.**
  - The constants are Phase 0's and Phase 1's (`MediaFollowing.R0`, `RegistrationPerformance`, `Novelty`, `Attention`).
  - Attention is read from the followings, so it is 1 unless `--observe-following` also runs.
  - A routine first Sullivan spot for an adult-leaning act registers about 0.016 (the design's 0.035 is before fit).
    An electrifying night before 38% of the country for a teen act registers 0.078, past the old yearly cap in one
    night. The same show the following week registers 0.011.
  - A **breakout** is an airing worth a Top 10 milestone on its own (raw ≥ 0.045).
- **Press** (§4), registered readers per item:

  | Family | Registers | Size of one item |
  |---|---|---|
  | Mainstream (Life, Look, Time, Newsweek) | circulation × 0.2 | Life: 4M people, 0.025 of the country, about a Top 40 hit. The existing 0.85 editor bar keeps these rare. |
  | Specialist (Black press, Country, Jazz, Folk) | circulation × r0 × the paper's fit | Ebony: about 0.005 for a Soul act |
  | Local daily | circulation × 0.05 | The first clipping: a big-city daily is about 0.0003 |
  | Teen fan | nobody's name | Readers join the following and learn faces (Phase 1) instead |
  | Trades | nothing | Business evidence (directive §8.1) |
  | Critics | nothing; items counted | Cultural standing isn't opened to media by §12.4 or design §0. See *Open* below. |

- **Controversy** (§4.2). Today a controversy arises only when an act defies a standards agreement on air.
  - Recognition rises by severity × 0.035: 0.021 for a national defiance (severity 0.6), about a Top 40 hit. It is
    charged to the controversy's root, so the papers retelling it add only beyond that.
  - With followings kept: the adult following and adult regard lose severity × 0.5 (30% at 0.6), and the young
    following loses severity × 0.15 (9%). The core hardens: disappointment churn is halved for a year.
  - Booking access already falls through the breach memory: that program for three years, a network review for one.
    Nothing new is added there.
- **Reader joiners** (§2.2): readers × the outlet's fit × `cRead` 0.03, one root per act per week at the widest
  outlet. A teen magazine's readers are young. A specialist paper's split like an LP's buyers of the act's genre. A
  national magazine spreads by each region's population; a city paper stays in its home region. A 1.5M-reader feature
  in *16* is about 45k joiners before saturation.

**Fixtures** (`MediaAtmosphereChecks`, 211 checks pass at `--seed=1001`, 12 of them new):
- a routine first Sullivan spot registers 0.016, upkeep;
- one electrifying night before 38% of the country registers 0.078, past the old yearly cap;
- the same show the next week registers 0.011;
- a twentieth national date registers less than half a first; no audience, nothing;
- a Life story registers a fifth of its readers, 0.025 of the country;
- trades, teen magazines and critics register no act's name; the Black press does;
- Look then Life on one root credits Life's readers once; a retelling adds nothing;
- two shadow weeks credit 262 roots, 121 of them from the papers, every act's media familiarity in [0,1];
- root credits survive a save round trip;
- causal: registration and the papers reach 177 acts' real recognition, none short of opening + earned;
- a national defiance raises recognition 0.064 → 0.084, and costs 30% of the adult and 9% of the young following,
  hardening the core for a year;
- the same controversy retold adds no recognition.

## Screens

### Calibration: reader joiners

The first shadow read (`fr2obs-<seed>-104`) had readers outjoining buyers: 16.5-16.8M against 14.4-14.7M in 1960. The
specialist papers run about 1,570 items a year, mostly chart news, and *Jet* or *Ebony* at full `cRead` is 60-90k
joiners an item. The design's `cRead` is for "features about the act itself", so now:
- feature angles convert at `cRead` 0.03 (teen feature, fan scene, phenomenon, hometown, community news);
- news converts at a fifth (`MediaFollowing.NewsReadShare`).

`MEDIA_FOLLOWING` now prints joiners offered by teen and specialist papers.

### Shadow inertness

104 weeks, ref flags (`--aggregate-only`) + `--observe-media-recognition --fame-registration --observe-following`,
against `ref-f50b9a4-<seed>-104`. Both before and after the calibration (`fr2obs`, `fr2obs2`), seeds 1001 and 2002:
98/98 written CSVs byte-identical. The 8 census files were not requested.

1960 shadow sizes (`fr2obs2`):

| | Seed 1001 | Seed 2002 |
|---|---:|---:|
| TV applied / share of all recognition gained | 2.86 / 3.4% | 2.35 / 3.0% |
| Press items: mainstream / specialist / local daily | 4 / 1,570 / 2,707 | 1 / 1,567 / 2,652 |
| Items absorbed by an already-credited root | 683 | 785 |
| Press applied: mainstream / specialist / local | 0.022 / 0.47 / 0.48 | 0.012 / 0.45 / 0.47 |
| Media share of recognition gained (TV + press) | 4.5% | 4.1% |
| Breakouts | 0 | 0 |
| Joiners: buyers / TV / readers | 14.4M / 3.3M / 4.7M | 14.7M / 2.7M / 4.7M |

### Causal screens, 1963-66 resumes

Treatment `fr2-t` / `fr2-t2002`: `--causal-media-atmosphere --media-recognition --fame-registration
--observe-following`. Controls are Phase 1's `mf1-c` / `mf1-c2002` (`--causal-media-atmosphere --observe-member-fame`,
economically the same as causal mode alone). `mrec-c` is the old capped TV writer on the seed 1001 world.

**Media sizes**

| | Seed 1001 | Seed 2002 | Old capped writer, seed 1001 |
|---|---|---|---|
| TV applied, 1963 / 64 / 65 / 66 | 3.0 / 2.2 / 3.1 / 2.7 | 3.0 / 2.6 / 3.3 / 2.9 | 5.6 / 4.9 / 6.4 / 5.3 |
| TV share of recognition gained | 3.5-7.5% | 3.5-7.9% | 6.3-13.9% |
| Media share (TV, press, controversy) | 4.7-9.9% | 4.7-10.2% | |
| Breakouts | 0 | 0 | |
| Mainstream items / controversies | 0 / 0 | 1 / 0 | |
| Sensations | 0 entered (2 transitions in 1966) | 0 | |
| Joiners, buyers / TV / readers | 15-19M / 2.8-3.9M / 5.6-6.7M | 16-19M / 3.2-4.1M / 5.7-6.7M | |

- **Registration writes less than the cap allowed, not more.** A routine airing registers r0 × fit × novelty, about
  0.06-0.12 of its fresh viewers, against the old K of 0.15. The cap was clipping 14-54 acts a year of routine upkeep;
  it was not what kept big nights small.
- **No breakout in eight world-years.** The biggest act's whole year of television is about 0.05. A breakout needs a
  fresh national audience, a fitting act and an electrifying night together, and these worlds also have no sensation in
  1963-66. Design §9 asks for "a handful a decade" of TV-made stars. This needs either r0 calibration or the residencies
  of Phase 3, which are the design's main route.
- **Two of the four writers never fired in the world.** Mainstream editors' 0.85 bar admitted one item in eight years,
  and no act defied a standards agreement. They are tested only by the fixtures.
- **Specialist and local papers are steady and small:** about 1.0 of recognition a year between them, a quarter of
  media's total, spread over 4,400 items.

**Economy** (t − c; single seed per world, reshuffle-sized limits)

| | Seed 1001 | Seed 2002 |
|---|---|---|
| Top 10% acts' slot-weeks (kill rule +3 pt) | +1.27 pt | +1.72 pt |
| Major-family share, 1963 / 64 / 65 / 66 | +0.9 / +1.2 / +0.3 / −6.8 pts | +1.7 / −3.7 / +11.3 / −1.8 pts |
| CR4, 1966 | −7.6 pts | +7.0 pts |
| Single units | −0.4 / −0.5 / +0.3 / +0.8% | +0.4 / **+1.5** / +0.9 / **+1.9%** |
| Album units | +1.0 / **+1.2** / −0.7 / **−1.6%** | **−1.1 / −3.8 / −2.6 / −4.3%** |
| Genre beyond ±1 pt | Soul +1.3, Country −1.2 | TraditionalPop +1.3 |
| Closures | 108 → 117 | 105 → 105 |

- **No catastrophe.** Concentration stays well under the kill rule. Major share and CR4 move with both signs across the
  two worlds and between years, so they read as reshuffle.
- **Seed 2002's albums fall in every year (−1 to −4%), and singles rise.** Phase 1 showed the same on this world
  (singles +1.7%, albums −3.7% in 1966), against the same control. Two different recognition writers giving one shape on
  one world could be:
  - a shared mechanism: more recognized acts tilting release decisions toward singles;
  - this world's reshuffle sensitivity. No reshuffle-only reference exists for it.

  On seed 1001 albums are mixed (+1.2% to −1.6%). The paired decade A/B at the branch end has to settle this, and it
  should look at the release mix of acts whose recognition media raised.

**Verdict.** The writers do what was specified:
- recognition from people who registered the act;
- root-deduplicated press;
- notoriety and the following split, in the fixtures.

They are safe enough to commit off by default. But the registration model is weaker than the cap it replaces, and it
makes no breakouts. Neither r0 nor Phase 3 is the fix: see *What keeps the biggest airings small* below. Phase 2c
tried event hype and rising-act bookings and made none either (`CelebrityFanbasePhase2cNotes.md`). Breakouts are now
scoped to phenomena, 0-1 a decade (design §3.4.6), so zero in these worlds meets the target.

### What keeps the biggest airings small

`frd-1001` / `frd-2002`: the `fr2-t` resumes and flags again, with `MEDIA_TV_REG_DECOMP` and `MEDIA_TV_REG_TOP` (year
end, national airings only, print-only). The screen counts how many airings would break out with each factor at its ceiling: fit 0.75, performance 1.6, novelty 1.

| Per year, both seeds, 1963-66 | |
|---|---|
| National airings | 2,200-3,200 |
| Largest fresh audience | **0.157-0.166 of the country**, always a Sullivan date. No airing reached 0.20. |
| Airings with fresh ≥ 0.10 | 39-57 (Sullivan, Como, Bell Telephone Hour) |
| Electrifying nights / those before fresh ≥ 0.10 | 20-75 / 0-2 |
| Audience fit, mean / max | 0.46 / 0.58. Sullivan: TeenPop 0.35, Soul 0.37-0.38, Comedy and pop 0.44 |
| Attention, lowest | 0.98-1.00 (never binds) |
| Largest single gain | 0.015-0.021 (the bar is 0.045) |
| Would break out: any one factor at ceiling / any two / all three | 0 / 0-3 / 2-10 |

1. **The audience binds first.** The largest audience is a fixed program size: Sullivan reaches about 16% of the country
   every week, whoever is on. A breakout needs fresh × 0.25 × fit × performance × novelty ≥ 0.045. Even an electrifying
   first date at the best fit any airing reached (0.58) gives 0.166 × 0.25 × 0.58 × 1.6 = 0.038.
   - The fixture's 38% night has no route in the world.
   - The Beatles' 73M was about twice Sullivan's usual audience. The act drew it. Nothing in the model makes a hot act
     enlarge the audience of the night it plays.
2. **Fit runs backwards for the historical case.** Registration reuses `MediaFollowing.AudienceFit`, the share of an
   audience who could *follow* the act. On a family show (28-30% young) that ranks a teen act lowest (0.35). The
   historical breakouts were young acts on family shows (Presley, the Beatles). A teen act on Sullivan can't pass 0.39
   fit, so its ceiling night is 0.166 × 0.25 × 0.39 × 1.6 = 0.026.
3. **Electrifying nights and big fresh audiences rarely meet** (0-2 a year), and novelty is mostly spent on smaller
   programs first. Fixing these alone would do little: the 2-10 "all three at ceiling" airings are still under 0.17 fresh.
4. **Attention is inert** in these worlds: no other act is ever above the sensation bar.

**What this means for Phase 3.** Residencies change neither the audience ceiling nor fit, and registration ignores the
repeat viewers a series is made of. So Phase 3 can make TV-made stars only through followings → purchases, not through
breakouts. Breakouts need one or both of:
- **event audiences:** a booked act's buzz or recognition raises that night's tune-in above the program's base, so the
  act draws the audience;
- **registration fit separate from following fit:** whether a viewer remembers a striking act is not the same as
  whether they could become its fan. A teen act on a family show should register at least as well as the show's own
  kind of act.

Raising r0 alone would lift every routine spot along with the big nights, which is the cap's old problem the other way
round.

## Open

- **Critics and cultural standing.** Directive §8.4 and §12.4 forbid a direct media write to `culturalStanding`, so
  critics write nothing in Phase 2. The author decided (2026-11-01) that critics should count through the acclaim
  pipeline instead: design §4.3, Phase 2b.
