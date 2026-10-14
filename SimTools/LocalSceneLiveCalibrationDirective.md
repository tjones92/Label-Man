# Local Scene Live Calibration — Rooms, Audiences, Live-Work Money

Branch: `band-member-simulation` (scene Phases 0-7 merged 2026-10-09). This directive picks up the handoff item
"historical content and behavior calibration: sourced dated institution/era changes, equipment/prestige/access and
audience behavior, deeper uncertain small-city/community profiles, and validation of live-work substitution/earnings"
(`LocalScenePhase7Handoff.md`). It follows the atlas's evidence rules (`LocalSceneHistoricalAtlas.md` §4-§5): bounded
anchors, every number marked as a historical anchor or a design input, no false precision.

## 1. Starting point (measured, not assumed)

Codex's 1960 year run (`scene7-source-year-v1-source-1001`, scene work ledger at week 52):

| Measure | Value |
|---|---:|
| People with any room work | 3,008 (1,009 acts) |
| Stage hours per person-year, median / p90 / max | 26 / 52 / 182 |
| Room pay per person-year, median / mean / p90 | $0 / $79 / $189 |
| Bills with both slots filled | 616 of 620 |

Code facts:

- Every city has the same six fictional rooms, with the same capacities (club 120, listening 80, coffeehouse 45,
  community 100, roadhouse 65, trade 20), whatever the town's size.
- Admission was $1 (club, listening) or $0 (everything else), flat for the whole decade.
- Every room paid acts half the door, so coffeehouses, community halls and roadhouses paid **nothing**.
- **Room money reaches nothing.** `SceneWorkAccount.FeeShare`, `SceneBill.GrossReceipts` and `SceneAppearance.Fee`
  are written and only read by the audit census. Member wealth (`MemberWealthService`) is paid from record royalties
  and writer mechanicals alone. The one live-money path in the economy is the player's `BookRoad`
  (`ResidencyWeeklyPay` $22, `TourWeeklyNet` $30 per week to the label).
- Band-life growth assumes 900 club-room or 450 other-room hours a year (`MemberGrowthService`). Rooms realize
  ~3-6% of that; the rest is "background" by design (`AttributeBudget` substitutes real hours inside the allowance).

## 2. Evidence

| Claim | Source | Status |
|---|---|---|
| Union scale for a booked folk-club week, New York 1961: $90 for six nights | Tom Paxton recalling Gerde's Folk City, [Bedford + Bowery 2017](https://bedfordandbowery.com/2017/09/musicians-recall-dylans-first-big-gig-and-25-years-of-music-history-at-gerdes-folk-city/) | One oral recollection; anchor |
| Village basket houses paid only what the hat collected (Gaslight, Cafe Wha?) | [Gaslight Cafe](https://en.wikipedia.org/wiki/The_Gaslight_Cafe), [Village Preservation](https://villagepreservation.org/2025/08/06/the-beautiful-history-of-cafe-wha/) | Corroborated; the per-head amount is a design input |
| Monday hoots at Gerde's paid performers nothing | Paxton, same source | Anchor for unpaid open nights (not yet modeled) |
| Southern circuit local sidemen: a $2 night, "usually better than that, but not by much"; no union scale in the South | [Florida Humanities, Traveling Down the Chitlin' Circuit](https://floridahumanities.org/traveling-down-the-chitlin-circuit/) | Recollection; anchor for the flat roadhouse fee |
| Jazz club act fees from under $300 to over $3,000 a week by 1959 | [Black Hawk (nightclub)](https://en.wikipedia.org/wiki/Black_Hawk_(nightclub)) | Headliner scale, above anything the supporting rooms pay |
| Teen club admission $0.50 with $1 membership (San Antonio, 1961); package-show tickets $1.50, $2.50 by 1965 | [Wittliff Teen Canteen](https://thewittliffkeystone.wp.txstate.edu/2019/01/16/teen-canteen-letters-shed-light-on-60s-youth-culture/), [Caravan of Stars](https://en.wikipedia.org/wiki/Caravan_of_Stars) | Brackets the club cover; no teen-dance room yet |
| 1960 city-proper populations | [Census POP-twps0027, Table 19](https://www2.census.gov/library/working-papers/1998/demographics/pop-twps0027/tab19.txt) | Hard data; Billings (below the cutoff) is provisional at 53,000 |
| Price level 1960-69 | BLS CPI-U annual averages (29.6 to 36.7, +24%) | Hard data |

Not found, and therefore **not** modeled as fact: an AFM local scale table for 1960s club sidemen, teen-dance band
fees, supper-room minimums. Sources consulted October 9, 2026.

## 3. Phase A — dated room money and size (built; observation-only)

`Systems/SceneLiveEconomics.cs`, default on with rooms; `--disable-scene-live-calibration` restores the flat catalog.

- **Capacity** scales with the 1960 city-proper population: catalog capacity × (pop / 500k)^0.25, clamped
  0.6-1.6. New York is ×1.6 (club 192), Billings ×0.6 (club 72). The exponent and clamp are design inputs; the
  quarter power keeps the city-proper/metro error (Miami, pre-1963 Nashville) small.
- **Admission** moves with CPI from 1960 bases: club $1.00, listening room $1.50 (provisional), others $0, rounded
  to the nickel. The player pays this per visit.
- **Pay terms** by room kind (`SceneRoomPayTerms`):

| Room | Terms | 1960 rate | Anchor |
|---|---|---|---|
| Club | Door split | half the door, shared by acts | existing catalog split |
| Listening / supper room | House wage | $15 per player-night × going rate | Gerde's $90/6 nights (NY) |
| Coffeehouse | Basket | $0.15 per listener, shared by acts | basket houses; amount provisional |
| Community hall | Offering | $0.05 per listener, shared by acts | design input |
| Roadhouse | Flat fee | $3 per player-night × going rate | circuit $2 night |
| Trade event | None | $0 | listening appointments |

  "Going rate" is the town's authored unsigned-act ask (`CityProfiles.AskScale`) relative to New York, so Omaha's
  room wage is 0.80/1.45 of New York's. Wages and fees rise with CPI.

- Each bill records its capacity (`SceneBill.Capacity`); bills saved before this change read 0 and fall back to the
  catalog. No save-schema bump is needed.
- New diagnostics: `<run>-scene-room-years-<phase>.csv` (per room and year: capacity, admission, sets, attendance,
  receipts, act pay, player-nights). It is not saved, so a resumed world reports from the resume onward.

**Inertness requirement.** Nothing in the world economy reads room money, so the calibrated and flat arms must
write identical economy CSVs. Results are in §6.

## 4. Phase B — live-work volume (built)

Read from the same ledgers (`<run>-scene-work-end.csv`, 1960 rows: the 1961 rows wait for that year's band pass).
No new control runs are needed for this phase.

| 1960, live arm | seed 1001 | seed 2002 |
|---|---:|---:|
| Stage hours per person, mean / p90 | 24.5 / 52 | 23.9 / 39 |
| Synthetic allowance per person, mean | 713 | 741 |
| Share of allowance rooms realize | ~3% | ~3% |
| People with unbudgeted hours | 413 of 3,510 | 340 of 3,590 |
| Unbudgeted share of all room hours | 12.8% | 9.2% |

**Finding: the unbudgeted hours are a coverage gap, not double counting.** Every unbudgeted row has an allowance of
exactly 0. `MemberGrowthService.Hours` gives hours only to signed acts and unsigned acts *seeking* a deal; every other
unsigned act gets (0, 0). Rooms book any present act, so those acts play about 26 hours a year that count toward no
allowance and earn no growth. The ledger exposes it, as the room contract requires.

**Fix (approved 2026-10-09, built).** A member of an act with a zero allowance is credited their realized room hours
at `KindResidency` weight (`MemberGrowthService.OnActYear`; `--disable-room-growth-credit`). Unbudgeted room hours
in 1960 fell to 0 of 115,577 (seed 1001) and 0 of 115,744 (seed 2002).

**Club week (approved, built).** Working clubs run Tuesday to Saturday instead of Friday and Saturday
(`SceneLiveEconomics.OpenOn`, part of live calibration). Club sets rose from ~6,570 to 16,244 a year. More nights grow
realized hours inside the same allowance, so growth is untouched for acts that have one. This is no longer inert:
the relocation service skips acts with future bookings, so moves shift slightly.

Still open: open-mic/hoot nights (unpaid, many short sets) as a bill shape.

## 5. Phase C — live income reaches people (built; decade comparison deferred)

Add a live term to `MemberWealthService.OnActYear`: the member's room `FeeShare` for the year, plus a background
estimate for the unrealized part of the allowance at the town's rate. This is the history: most working musicians
lived on live work, not royalties. It also changes band life: the day-job exit reads `Norm(wealth)`, so steady room
work would keep players in music (the atlas's "a technically accomplished act declines touring because its
residency pays steadily"). It needs a two-seed decade comparison (treatment arms only, against one decade reference per seed at the base commit) on the band-life measures (day-job share, Reliability
share, solo intent, studio-only) before it defaults on. Player-owned acts' room pay should go to the members, not
the label; the existing `BookRoad` label income stays as it is.

**Built (approved 2026-10-09).** `SceneLiveEconomics.MemberLiveIncome`, called from `MemberWealthService.OnActYear`;
`--disable-member-live-income`, and it requires live calibration. A member's gross live pay for the year is:

- realized room pay (`FeeShare`), plus
- the rest of the act's paid live hours (`MemberGrowthService.LiveHours`: road hours for a signed act, 900/450 room
  hours for one seeking a deal, none otherwise) less realized stage hours, at 4 hours a working night, priced at
  `ExpectedPlayerNightPay`: the town's working room for the act's genre family, under that room's pay rules at
  typical fill (0.72) and two acts a bill.

A quarter of it (`LiveSavingsShare`, design input) enters the wealth stock; the rest is rent and food. Only Active
members earn it; studio-only members play no dates. `Musician.lastYearLiveIncome` records the gross, and
`--log-band-life-members` writes it as `liveIncome`.

`live-inc-v1` (104 weeks, seeds 1001/2002, treatment only, compared with the `ref-bc4e829` references). Only the 1960
band-life pass falls inside 104 weeks, so this is a direction check, not acceptance:

| 1960 | seed 1001 | seed 2002 |
|---|---:|---:|
| Gross live pay, unsigned member, median / p90 | $1,161 / $2,560 | $1,208 / $2,572 |
| Gross live pay, signed member, median / p90 | $628 / $1,967 | $628 / $1,968 |
| Wealth, median / p99 | $282 / $2,080 | $295 / $2,214 |
| DayJob departures, ref → treatment | 61 → 59 | 57 → 56 |
| Every other departure kind | unchanged | unchanged (one Busted +1 on 1001) |
| Singles / album units, 2 years | +0.12% / −0.43% | −0.05% / +0.19% |

A working local player grossing about $1,200 is in line with the anchors: union scale was $90 a week, and most of
these players were not on scale. The effect on band life compounds as the stock builds; at a ~0.35 spend share the
median steady state is ~$830 (Norm ≈ 0.10), which lowers the day-job hazard by about a tenth. The decade comparison
measures that.

Known high end: a solo act takes a whole act's door share, so a solo seeking a deal in a New York club grosses
~$7,800 (the p99). That is above a full year at scale ($4,680). It touches ~1% of member-years; a solo-with-house-band
pay rule is the fix if the decade run shows it matters.

**Deferred by the author:** the two-seed decade comparison runs once, at the end, after this and Codex's remaining
two features (employment/AI investment/migration/repertoire/contact networks; foreign licensing/territorial rights/
provenance) are built. Treatment arms only, against one decade reference per seed at the base commit.

## 6. Results — Phase A

`live-cal-v1`: 104 weeks, seeds 1001/2002, calibrated (`live`) vs `--disable-scene-live-calibration` (`flat`),
canonical flags plus `--aggregate-only --scene-room-audit`. Analysis: `py SimTools/analyze_scene_live_calibration.py live-cal-v1`.

**Inert.** All 84 economy CSVs hash identically between arms, on both seeds. The flat arms are kept as the reference
set for this base (`SimLogs/ref-*`), so later observation-only work runs only treatment arms against them.

Pay per player-night, 1960 (seed 1001; seed 2002 within a few cents):

| Room | Flat catalog | Calibrated |
|---|---:|---:|
| Listening / supper room | $4.86 | $10.00 |
| Club (door split) | $7.17 | $7.66 |
| Roadhouse | $0 | $2.02 |
| Coffeehouse (basket) | $0 | $0.95 |
| Community hall (offering) | $0 | $0.61 |

The order matches the anchors: the union-scale room pays best, nearly two-thirds of New York's $15; club doors sit
below scale; the roadhouse lands on the circuit's $2 night; basket and offering money is pocket change. Mean room
pay per person-year rose from $68 to $117, and the median from $0 to $57, because every working room now pays
something. Fill held at 0.69-0.75 in both arms: attendance is proportional to capacity, so bigger rooms in bigger
cities draw proportionally more people, nothing else. 1961 admission rounds to the same nickel as 1960; CPI drift
first shows in admission by the middle of the decade.

Checks rerun on the change (seed 1001, only the ones that read rooms): rooms, information, ecosystem, 26-week save
round-trip. All pass. The source, recruitment and city-placement suites do not read room money or capacity, so they
were not rerun.

## 7. Backlog (needs sourcing before content)

- Dated institution changes: the coffeehouse boom and contraction, teen-dance and armory circuits (1963-66), ballroom
  programming (1966+), the federal cabaret tax (the AFM's history dates its reduction to 10%; repeal lobbying from
  1963). Each needs a dated source before it moves a number.
- Audience behavior (directive §7.3): kind-specific response to repertoire, volume and professionalism.
- Small-city and community profiles: Billings, Omaha, Albuquerque and Salt Lake City are the weakest atlas entries.
- Equipment, prestige and access: separate from capacity; currently absent.
