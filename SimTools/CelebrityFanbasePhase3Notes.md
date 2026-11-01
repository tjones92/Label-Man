# Celebrity fanbase Phase 3a notes: residencies for rival acts

Status: built and screened 2026-11-01, off unless named (`--media-residencies`). Shadow-inert; causal economy within
reshuffle noise; residents' followings inside §9 after one calibration.
Design: `CelebrityFanbaseDesign.md` §3.3.1. Phase 3b (the player's negotiation and the diary cost, §3.3.2) is built: see the end.

## What is built

| Piece | Where | What it does |
|---|---|---|
| Switch | `MediaResidencies.cs` | `--media-residencies`. Runs wherever media observes; in observe mode it books in the media world only. The series term also needs `--observe-following`. |
| Chairs | `MediaResidencies.Chairs` | Shindig! 1, Where the Action Is 2, Happening '68 1, The Jimmy Dean Show 1. Each chair is cast for a season: to the end of August at least 13 weeks on, or the program's last episode (`SeasonEndDay`). Not cast with under 8 weeks left. |
| Casting | `MediaAtmosphereService.Residencies.cs`, `ResidenciesWeek` | Weekly, before next week is booked. Prospects: active, signed to a rival label, not a Superstar, not resident anywhere, with a Top 40 single in two years, a regional break in the last year, or recognition ≥ 0.10. Chair fit ≥ 0.8. Score = fit × (0.5 + recognition + young following ÷ 500k) × keyed 0.55-1.45; the incumbent ×1.5 at a recast. |
| Vehicles | `VehicleSeason`, `MediaResidencies.BuildVehicle` | Each August from 1966: renew a first-season vehicle whose act's following grew (recognition, without the following switch) and had a Top 40 during it; then one new vehicle for the best teen act (teen fit ≥ 0.9) that is a Regional Favorite or better, or has 100k young followers. The program is a guest-less taped half hour at 7:30 Eastern on a keyed weeknight and network, originals mid-September to April, reruns May to August. Rebuilt from the save (`MediaOutletCatalog.SetVehicles`), never saved itself; never pitchable by the AI planner or the player. |
| On the air | `BookEpisode`, `ResidentCandidates` | Residents open every original episode outside the bill (new `BillLane.Resident`). They sing their current single (12 weeks or on the chart), else their best-known hit or a standard. No campaign charge (the producer pays; AI TV fees aren't modelled) and no standards review. A program's regular is never also its guest; resident dates hold their days but don't use up the guest calendar. |
| Fresh audience | `TallyAiredThrough` | A resident's returning airing is 10% new per week of episodes (2% an episode on a weekday show), against 25% for a returning guest. Recognition, member fame and the demand ledger read this. |
| Series term | `FollowingWeek` | Per act and program, the week's best original episode: reach by region × √(episodes that week, at most 2×) × young or adult share × audience fit × performance × `CSeries` 0.008, adults × 0.25 (calibration 1, below), into the same saturating join as every other joiner. |
| Telemetry | console | `MEDIA_RESIDENCY` (cast), `MEDIA_RESIDENCY_END`, `MEDIA_RESIDENCY_RENEWAL`, `MEDIA_RESIDENCY_VEHICLE` (none found), `MEDIA_TV_MADE_STAR` (first Top 40 within a year of the start, for an act with none before), `MEDIA_RESIDENCY_YEAR`; `MEDIA_FOLLOWING` gains `joinSeries`. `MEDIA_STANDING` now counts only the standing and comedy lanes (it had counted resident episodes as standing dates). |

**Fixtures** (`MediaAtmosphereChecks`, 244 pass at `--seed=1001`, 14 new): the score's terms and spread; new share 2% daily,
10% weekly; a 12M-viewer vehicle converts 46k young a week (1.38M a season); season ends for Shindig! and Action; every
chair on a catalog program; the vehicle's schedule (premiere, reruns, off after August, no slots, title). In the world: a
cast chair on Bandstand goes to a fitting rival act and opens every episode booked after it (10 of 10), never also as a
guest; a vehicle goes on the dial, is never pitchable, and carries only its star; both build series joiners (8.5k and
43k in two weeks); residencies and resident appearances survive a save round trip, and a load puts the vehicles back;
with the switch off no resident is booked.

## Screen

- **Shadow gate.** `res3obs-1001-104` (ref flags + `--media-residencies --observe-following`) against
  `ref-f50b9a4-1001-104`: **98/98 CSVs byte-identical.** No chair opens before September 1963, so this proves only that
  the new code is inert with no residency on the books.
- **Causal resumes.** `res3-t` / `res3-t2002`, then `res3b-t` / `res3b-t2002` after calibration: 1963-66 resumes of
  `media-p3-1001-1963` and `asc-2002-1963` with `--causal-media-atmosphere --media-recognition --fame-registration
  --observe-following --media-residencies --lean-probe`. Controls: Phase 2's `fr2-t` / `fr2-t2002` (the same flags without
  the switch; Phase 2c was screened against them too). Both worlds match their controls exactly through 1963, when the
  first chair opens in mid-September, so the controls are valid. About 14 minutes a run.

### What the residencies did

| | Seed 1001 | Seed 2002 |
|---|---|---|
| Residencies on the air 1963 / 64 / 65 / 66 | 1 / 3 / 7 / 8 | 1 / 4 / 6 / 7 |
| Recasts kept the incumbent | 3 | 3 |
| Vehicles | 1, fall 1966 (TeenPop) | 1, fall 1966 (BritishBeat) |
| Residents with a Top 40 during the chair | 2 / 5 / 6 in 1964-66 | 1 / 5 / 4 |
| TV-made stars (first Top 40 within a year of the start) | 1 (an Action regular, six weeks in) | 0 |
| Series joiners a year, 1964-66 (calibrated) | 1.0M / 2.5M / 2.7M | 1.0M / 2.7M / 3.0M |
| For scale: buyer joiners a year | 16-19M | 17-18M |

**Calibrated followings by chair** (start → end of the engagement):

| Chair | Seed 1001 | Seed 2002 |
|---|---|---|
| Where the Action Is (daily, 14 months) | 0.29 → 1.09M; 0.12 → 0.96M | 0.22 → 1.41M; 0.35 → 1.50M |
| Shindig! (weekly, a season) | 0.17 → 0.47M; 0.06 → 0.43M; 0.15 → 0.77M | 0.48 → 1.06M, kept, → 1.49M |
| Jimmy Dean (adult primetime, a season) | 0.03 → 0.91M, kept, → 1.43M, kept, → 1.71M | 0.04 → 0.52M; 0.13 → 0.43M, kept, → 1.19M; 0.23 → 0.60M |
| Vehicle, Sep-Dec 1966 | 0.87M at year end | 0.98M at year end |

- The world's largest followings in 1966 are residents on both seeds: 1.4M and 1.6-1.7M, inside §9's 1-3M for a top
  teen act. Before residencies the top following was under 0.8M.
- A vehicle's first 3.5 months build about 0.9M. A full season should reach the design's 1-1.5M; the renewal rule is
  untested, because 1967 is outside the screen.

### Calibration 1: adults a quarter, a daily week up to 2×

The first round (`res3-t`) gave the Jimmy Dean regular on seed 1001 1.9M followers in one season and 4.0M in three,
mostly adult: the largest following in either world, against a history in which that show's regulars were not stars.
The daily Action chair, the Raiders' route, built only 0.5-0.7M in 14 months, because the series term read one
episode a week.
- Adults now join at a quarter of the young rate (`AdultSeriesShare` 0.25).
- A week's viewers are the best episode × √episodes, at most 2× (`WeekViewers`).

Calibration changes followings only: `res3b` matches `res3` in 93 of 94 CSVs on both seeds; only `following.csv` differs.

### Economy (t − c, same in both rounds)

| | Seed 1001 | Seed 2002 |
|---|---|---|
| Top 10% acts' slot-weeks (kill +3 pt) | −1.11 pt | +1.25 pt |
| Major-family share 1964 / 65 / 66 | −6.4 / −8.6 / +3.7 | −1.2 / −7.2 / −3.9 |
| CR4 1966 | +7.0 pt | −2.9 pt |
| Singles / albums, worst year | +0.8% / −1.9% (1965) | −0.8% / +2.1% (1965) |
| Genre beyond ±1 pt | Soul +1.6 | none |
| Closures | 117 → 113 | 105 → 115 |

- In these runs the only economic channel is recognition: residents' first nights register through
  `--fame-registration`, about +0.3 of applied TV recognition a year. Booking also changes, since a program's regular
  isn't its guest. Either one reshuffles the world from September 1963.
- No catastrophe. Concentration moves both ways, inside the kill rule.
- **Watch item: Major share falls in 1965 on both seeds (−8.6, −7.2).** Phase 2b's near-inert writer moved it −5.6 to
  +5.4 on the same worlds, so one seed per world can't separate this from noise. The decade A/B at the end of the
  branch should report it.

## Verdict

Adoptable as the Phase 3a writer, off by default. Residencies are now the route by which a TV regular gets a
first-rank following: Action and Shindig! regulars reach 1-1.7M, and adult regulars stay smaller. The economic test
waits for Phase 4, when followers buy. Open:
- the vehicle's full season and renewal (1967);
- the Major-share watch item;
- Phase 3b, the player's negotiation and the touring cost (built below).

## Phase 3b: the player's residency (built 2026-11-01)

Design: `CelebrityFanbaseDesign.md` §3.3.2. Same switch, `--media-residencies`. Every path needs a player label, and the
audit runner never founds one, so audit worlds cast exactly as 3a.

| Piece | Where | What it does |
|---|---|---|
| Offers | `MediaAtmosphereService.PlayerResidencies.cs`, `OfferChair` / `OfferVehicle` | The player's acts are prospects (`Prospects`, only while a player label exists). When one tops a chair's or a vehicle season's ranking, the producer or network offers it and the chair waits a week. If the offer is declined or lapses, the act is skipped for that program's season (`TurnedDown`). A vehicle is recast the next week if the premiere is more than a week off. |
| Terms | `MediaResidencies.AcceptanceChance`, `AnswerResidencyOffer` | The producer's terms are always accepted. The label may counter once on four terms: length (13-week trial), fee (1.5× / 2× scale), material (own records or the show's songs) and diary (blocks). The counter is accepted on a keyed draw: the product of the term factors × leverage (best ÷ runner-up score, 1-1.5), capped at 0.95. A refused counter costs 0.05 trust and leaves the original terms. |
| Pitch | `PitchResidency`, `AnswerResidencyPitch` | Put an act up for a chair program or "a series of their own" (1h, postage). The answer comes in 2-4 days: declined on fit, an existing residency or no evidence, else listed. A listed act scores 1.25× at the next casting; the list lasts 26 weeks, or to August for a vehicle. |
| On the air | `ResidentCandidates` | Same as 3a. On the show's songs the act sings "the show's song this week" (a standard), which makes no demand-ledger request for its single. |
| Fees | `ResidencyPlayerDay` | Scale per performer per episode, $150 weekly and $60 daily, × the fee multiple. Paid through `PerformerFees` once per appearance (receipt `resident-fee\|id`), only on causal terms. Never label cash. |
| Diary | `PlayerDesk.BookRoad`, `TrimRoadForTelevision`, `PlayerRoadLoadFor` | Diary weeks = engaged weeks in the calendar year × the share (weekly 0.35 / 0.15 in blocks, daily 0.60 / 0.30, vehicle 0.80 / 0.50). Road weeks are capped at 48 − diary. Accepting cancels tour weeks past the cap; club runs stand. Road load gains 0.015 a diary week. |
| Office | `UI/PlayerDeskPanel.Media.cs` | SERIES OFFER card with the four term pickers (take their terms, propose, turn down), the series in hand, pitches waiting, and the PUT THEM UP row. A monthly line reports episodes, the best audience and the fees. |
| Telemetry | console | `MEDIA_RESIDENCY_OFFER` (made, declined, lapsed, counter-refused, accepted, counter-accepted), `MEDIA_RESIDENCY_PITCH`, and `MEDIA_RESIDENCY player=`. |

**Fixtures** (`MediaAtmosphereChecks`, 268 pass at `--seed=1001`, 24 new):
- **Arithmetic:**
  - the producer's terms are always accepted;
  - each counter factor;
  - offering the show's songs sweetens a fee counter;
  - leverage raises the yes, and nothing passes 0.95;
  - the diary shares;
  - diary weeks split by calendar year;
  - which chairs are daily.
- **In the world** (two rival acts lent to the label):
  - Bandstand offers the chair with a week to answer;
  - a 2×-scale, taped-in-blocks counter was refused at chance 0.17, a second counter was blocked, and taking the producer's terms signed the act;
  - the series took 5 diary weeks of 1960;
  - a further road booking is refused;
  - a whole-year vehicle diary cuts a 40-week tour to 2, once;
  - the act opens 10 episodes as the regular;
  - the performers were paid $1,920 for 8 episodes × 4 performers, once, with no label cash;
  - a declined chair went to a rival;
  - a pitch to a program without a chair is refused, and a Jimmy Dean pitch was declined on fit;
  - offers, pitches and the player's terms survive a save round trip.

**Screen.** `res3p-t` re-runs the `res3b-t` causal 1963-66 resume of seed 1001 (same flags) on the 3b code:
**94/94 CSVs byte-identical.** So the shared-casting refactor (the vehicle split and the leaving rule) moves nothing
in a world without a player label.

**Not verified:**
- The office UI compiles, but nobody has seen it in a running game.
- The design's evidence for 3b is a player playtest, which needs a 1963+ save with `--media-residencies`.
