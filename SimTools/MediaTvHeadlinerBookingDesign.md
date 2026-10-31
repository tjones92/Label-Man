# TV headliner booking and the television ecosystem

**Status: built 2026-10-30, branch `media-atmosphere`, observe-only (directive §12.1).** Picks up the item left open at
the end of [MediaAtmospherePhase3Notes.md](MediaAtmospherePhase3Notes.md) and seam 1 of
[CelebrityAuditHandoff.md](CelebrityAuditHandoff.md). It changes who is on a bill and what they sing, never an economic
input. Phase 4 (player costs, singles demand adapter) comes after it. `MediaAtmosphere.ConfigVersion` is now 4, so every
keyed media draw differs from earlier arms.

## The problem

National programs built their pool from this week's singles chart only (`MediaAtmosphereService.Candidates`). Media read
no fame. A Superstar crooner with no current top-25 single couldn't get on Sullivan, and the only primetime variety
show before 1964 was Sullivan.

## Author decisions (2026-10-30)

1. **What a star with no current single performs:** whatever such acts performed historically.
2. **Bill mix:** set from history.
3. **Catalog:** build the full 1960s television ecosystem. Comedy recording acts (the game has comedy albums) fill the
   comedy spots.

## Research

### Sullivan bills, 1959-70

Source: the CTVA episode guides for seasons 12-22 (`ctva.biz/US/MusicVariety/EdSullivan_NN_(YYYY-YY).htm`), which list
each guest and usually the songs. 452 episodes, 1,931 musical-guest entries. Scripts and cached pages are in the session
scratchpad (`tv/scripts`: `parse_ctva.py`, `music_entries.py`, `song_sources.py`, `bill_mix.py`, `standing_mix.py`).
Classification is by keyword and name lists, so read these as approximate shares.

| Season | Music acts / episode | Youth-market share | Current hitmaker share¹ | Non-record performers² | Comedy entries / episode |
|---|---:|---:|---:|---:|---:|
| 1959-60 | 3.9 | 9% | 11% | 34% | 2.4 |
| 1960-61 | 3.9 | 5% | 4% | 35% | 2.0 |
| 1961-62 | 3.5 | 20% | 20% | 25% | 2.6 |
| 1962-63 | 3.8 | 13% | 14% | 27% | 2.6 |
| 1963-64 | 4.1 | 18% | 15% | 26% | 1.4 |
| 1964-65 | 4.5 | 15% | 14% | 26% | 1.5 |
| 1965-66 | 4.3 | 25% | 22% | 24% | 1.6 |
| 1966-67 | 4.0 | 23% | 23% | 30% | 2.0 |
| 1967-68 | 4.6 | 21% | 25% | 25% | 1.9 |
| 1968-69 | 4.5 | 25% | 22% | 19% | 1.1 |
| 1969-70 | 4.7 | 21% | 13% | 21% | 1.2 |

¹ Act has a Billboard year-end Hot 100 single this year or last (Wikipedia year-end lists). The weekly 1960-64 Hot 100
(`SimLogs/historical-hot100-1960-64`) gives 18% on the Hot 100 within eight weeks, 20% earlier chart acts, 62% never on
the 1960-64 Hot 100. Both undercount pre-1959 hitmakers (Patti Page, Kate Smith, the McGuire Sisters).
² Opera, Broadway casts, concert artists, choirs, dancers and foreign variety acts, by keyword.

Sullivan stayed adult-led all decade. Even in 1965-69 three quarters of its musical guests were not youth-market acts,
and only about one in five had a current hit. The rest were recording stars booked on their names, plus a quarter who
were not record-business guests at all.

### What standing acts sang

The songs of 1960-64 Sullivan musical guests (1,434 titles), matched against the weekly Hot 100:

| Song | Share |
|---|---:|
| Standard, show tune or album song (never a 1960-64 Hot 100 title) | 79.5% |
| Another act's Hot 100 song | 13.6% |
| The act's own current or recent hit (−26 to +12 weeks) | 5.6% |
| The act's own older hit | 1.3% |

Examples: Teddy Randazzo sang "Lady of Spain" and "Autumn in New York"; Patti Page sang "Just in Time", "The Boll Weevil
Song" (a current hit by Brook Benton) and "Come On Home"; Pat Boone sang "My Baby Just Cares for Me", his new single
"Beach Girl", and "Exodus". Medleys of standards were routine, and acts with current hits (Chubby Checker, Connie
Francis, Roy Orbison) sang them.

### The program registry

Run dates come from Wikipedia infoboxes and schedule sections (cached pages in the scratchpad `tv/wiki`). Wherever a
time slot comes from memory rather than a source, the schedule is marked Approximate. Each program's `Source` field cites
where its dates came from.

## What was built

### Catalog (`Systems/Media/MediaOutletCatalog.Television.cs`)

- **Two new program kinds**: `TalkShow` (late-night and daytime talk) and `MusicShow` (country, soul and folk hours).
- **`BillPlan`**: each program's bill from a date, split into chart, standing and comedy slots. A program with no plan
  keeps the old behavior: every guest slot is a chart slot (the teen and dance shows).
- **`BroadcastScheduleVersion.IntervalWeeks`**: for shows that became monthly specials (Como 1963-67) or ran every other
  week (Bell Telephone Hour, approximate).
- **`TvNetwork.Syndicated`** daily talk shows carry a `SyndicatedClearance` (0.35-0.55) instead of full coverage.
- **New fit curves**: `TalkFit`, `CountryFit`, `CountryPopFit`, `SoulFit`, `FolkFit`, `ConcertFit`.
- **36 new programs:**
  - Network variety: Perry Como's Kraft Music Hall, The Kraft Music Hall (1967-), Dinah Shore, Garry Moore, Bell
    Telephone Hour, Andy Williams, Danny Kaye, Judy Garland, Jackie Gleason, Red Skelton, Jimmy Dean, Dean Martin,
    Carol Burnett, Glen Campbell, Johnny Cash, Hee Haw, The Lively Ones, Steve Lawrence, Roger Miller, and Kraft Summer
    Music Hall.
  - Network teen and specialist: The Dick Clark Show (to Sep 1960), Happening '68, Jubilee USA (to Sep 1960), and
    Hootenanny.
  - Talk: Tonight (Paar, the 1962 guest hosts, Carson), The Jack Paar Program, Merv Griffin (NBC daytime, Westinghouse,
    CBS), Mike Douglas (Cleveland local, then syndicated), Steve Allen (Westinghouse), and Joey Bishop.
  - Syndicated country from Nashville: Porter Wagoner, Wilburn Brothers, Ernest Tubb, and Bill Anderson.
  - Soul: Night Train (Nashville), The !!!! Beat (Dallas, syndicated), and Soul! (New York public TV).
  - More documented teen dance shows: Clay Cole (New York), Milt Grant (Washington), Swingin' Time (Detroit/Windsor),
    Shebang, Shivaree, and Hollywood a Go Go (Los Angeles).
- **Left out**: Lawrence Welk, Sing Along with Mitch and The King Family (no outside guests), and This Is Tom Jones
  (made in London; the game has no London).
- **Bill plans set from history:**
  - Sullivan: 1 chart, 2 standing and 1 comedy slot to 1963; 2 chart from 1964.
  - Palace: 1/2/1.
  - The singers' own hours lean standing.
  - Talk shows are mostly 1/1/1.
  - The "non-record" quarter of a real variety bill is not modelled; those slots simply don't exist.
- Fictional local dance shows are now clipped only around documented **dance** shows, not around a country hour or a
  talk show in the same town.

### Booking (`MediaAtmosphereService`)

- **Standing pool** (`Evidence.StandingActs`, built once per pass):
  - Who qualifies: signed, active, rival acts with `EffectiveRecognition ≥ 0.05` and a live record. The date is charged
    to that record's campaign, so the AI envelope logic (§12.2) works unchanged.
  - Score: recognition × genre fit × `1 / (1 + 0.12 × years since last top-40 hit)`, where an act with no hits counts
    as 6 years.
  - The last hit comes from live chart records, the opening careers' `publicHistory`, and the hit-cadence clock (read,
    never seeded).
  - A program only calls acts at or above its `StandingFloor`: 0.20 by default, 0.05-0.12 for the specialist shows.
  - A current top-40 act is left to the chart lane when the bill has one.
  - A local program's standing guests come from its own region.
- **Comedy lane**: the same pool, restricted to Comedy-genre acts. A comedian does "a routine from \"<LP>\"".
- **What a standing act sings** (`ChooseSong`, keyed per episode and act):
  - Its own charting single half the time, a fresh one (12 weeks or less) that isn't charting a fifth of the time.
  - Otherwise: its best-known own hit 10% of the time, another act's current top-30 hit in its genre family 18%, and
    otherwise a standard. The standard is drawn from the 40 most adult-familiar entries in
    `CompositionCatalogService.GetStandardsForFamily`, a read-only list.
  - The live split is reported in the results below.
- **Weekly cap**: a name booked on standing or comedy gets at most one national date a week. A current hit still allows
  two.
- **`MediaAppearance.Lane` and `.Song`**: `RecordId` stays the campaign the date was charged to. Only
  `PerformedSong.Record` means the song on the air was that record, and that is what the Phase 4 adapter must read.
- **Standards review** runs only when the act sings the booked record.
- **Fans**: standing and comedy appearances give no TV-response evidence for teen attachment.
- **Holds** (event nights): network variety and teen shows with chart slots only.
- **Player route**: an act whose recognition clears a program's floor can be offered a standing (or comedy) spot without
  a current record. Shows with no chart slots decline records outright ("they book names"). The offer keeps its lane
  (`MediaPitch.Lane`), and the confirmed date reserves that lane.
- **Not done**: the AI planner's pitch appeal still wants a chart record. Producers call standing acts directly, so a
  pitch only adds interest. AI labels don't yet send discs for their standing acts.

## Verification

- `MediaAtmosphereChecks` adds `TelevisionEcosystem()`. It checks:
  - Paar, the guest hosts and Carson take over Tonight on the right dates.
  - No Hootenanny before April 6 1963.
  - Jimmy Dean moves to Fridays.
  - Como goes from weekly to monthly.
  - Sullivan's bills by year.
  - Standing spots go to recognized acts, including one with no current top-40 record.
  - Standards, covers and old hits are not reviewed as the record.
  - Comedy spots go to comedians.
  - Chart bookings sing their record.
  - Lane caps hold.
  - Lane and song survive a save round trip.
- The economy-inertness gate is 104-week runs at HEAD and with this change, seeds 1001 and 2002, comparing every economy
  CSV byte for byte. Results below.

## Results

**Checks**: `MEDIA_ATMOSPHERE_CHECKS_PASS checks=138` (seed 1001; 122 before).

Two press rules changed on the way, because the wider bills tripped the "mainstream magazines rarely cover popular
music" guard: 9 items in four weeks, against a limit of 4.
- **A live on-air breakdown reaches the news magazines only for a known name** (top-10 record or recognition ≥ 0.45),
  and then one keyed magazine runs it, not all four.
- **Advertised headline nights stay with the original seven national hours** (`TvProgram.AnnouncesHeadliners`). The
  singers' hours, talk and specialist shows book without an event build-up.

The underlying cause is the existing performance model. Low-skill chart acts break down live, and Sullivan now has four
guests instead of two. Sullivan's live breakdown rate looks high for a heavily rehearsed production; that is worth its own
look.

**Economy inertness**: compared against fresh HEAD references (`SimLogs/ref-f50b9a4-<seed>-104`, flags
`--enable-genre-market-v2 --enable-artist-population-lifecycle --aggregate-only --log-band-life-members --contact-network-audit`).

| Seed | Economy CSVs |
|---|---|
| 1001 | 106/106 byte-identical |
| 2002 | 106/106 byte-identical |

**1960 bookings, seed 1001, 53 weeks** (`SimLogs/tvbook/tvbook-c-1001-53-console.log`). The year-end ledger covers the
whole year; appearance lists keep the last 26 weeks.

| | Before (HEAD) | After |
|---|---:|---|
| National appearances, last 26 weeks | 296 | 718 |
| Network slots filled | 620 | 1,575 (304 empty, nearly all comedy) |
| Standing slots | – | 623 filled, 17 empty |
| Comedy slots | – | 147 filled, 295 empty |
| Local dance-show slots filled | 3,335 of 5,509 (61%) | 2,436 of 4,197 (58%) |

- **Local slot counts** fall because fictional dance shows now clip around the newly documented ones (Clay Cole, Milt
  Grant).
- **The fill rate** dips because national dates now draw on the same AI TV envelopes.
- **What standing acts sang**: standard 379 (61%), their own record 130 (21%), a current cover 66 (11%), their own
  earlier hit 48 (8%). History for 1960-64 Sullivan, all guests: 80 / 6 / 14 / 1, before counting pre-1960 hits. The
  sim's "own record" share is higher because a standing guest here is always a recording act with a live release.
- **Busiest national guests** (last 26 weeks): 15-18 dates each for the top five, about one a week. Two of the top six
  are comedians: only 16 comedy acts have a live record, so the few signed comics get every couch.
- **Empty comedy slots**: in seed 2002, 30 of 442 comedy slots filled. Read an empty comedy slot as a club comic who
  doesn't make records, which is most of what Sullivan's comedians were.

## Follow-up: live breakdowns, star coverage, return gap

### Research

- **Sullivan return gaps**, from the same CTVA guides: 1,801 music appearances, 978 acts, 823 returns.

  | Gap to the act's next Sullivan date | Returns | Share |
  |---|---:|---:|
  | 1 week | 4 | 0.5% |
  | 2-4 weeks | 13 | 1.6% |
  | 5-8 weeks | 80 | 10% |
  | 9-13 weeks | 62 | 7.5% |
  | over 13 weeks | 664 | 81% |

  The four consecutive-week returns are all British Invasion package deals: the Beatles' three-show contract, the Dave
  Clark Five, and Gerry and the Pacemakers. The most-booked music act, Connie Francis, made 22 appearances in 11
  seasons. No dated source turned up for one act on two different national shows in the same week.
- **Live-TV incidents.** The well-known ones are mostly not musical collapses:
  - Defying the network's standards: the Doors on Sullivan, 1967. The game already sends a defied standards query to
    every news magazine, the teen magazines and the critics.
  - Production or equipment trouble: Sam Cooke cut off when Sullivan overran (1957), and the Who's flash powder on
    the Smothers Brothers (1967).
  - No Sullivan musical guest is remembered for simply falling apart on the air.
- **Rehearsal.** Sullivan's show day was a camera rehearsal, a dress rehearsal before an audience, then the live show.
  The Beatles' 1964 and 1965 days followed that pattern.
- **Target:** one or two genuine on-air musical collapses a year across all live network television.

### Changes

- **`TvProgram.ProductionReadiness`**: the rehearsal the production runs itself. Network variety 0.80; talk and music
  hours 0.55; teen and dance shows 0.
  - It scales the share of bad nights that become a breakdown by (1 − production)². A rehearsed act that slips
    finishes audibly Weak.
  - It doesn't replace the label's preparation. That still sets how often an act slips and how far past adequate a
    good night goes. Checked: on Sullivan, Extra preparation gives about 100-115 weak-or-worse nights in 800, against
    about 155-160 with Minimal.
- **Star coverage**: a live breakdown by a known name still goes to one keyed news magazine. A star's goes to all of
  them. A star is a top-3 record, recognition ≥ 0.70, or a national sensation or cultural phenomenon fan state.
- **Return gap**: a name booked on standing waits 13 weeks (`StandingReturnWeeks`) before returning to the same
  primetime variety hour. Chart bookings keep the program's own `RebookWeeks`.
- **Logging**: `MEDIA_PRESS` adds `nationalLive=[...]`, the outcomes of live, non-mimed national segments.
- **Checks** (140):
  - Added: a rehearsed network production rarely sees a collapse; the label's own rehearsal still matters on a
    network show.
  - The Rolodex and player-loop fixtures assumed Cleveland always had a local show on air. The ConfigVersion 4 re-key
    can leave it between shows, so the label now founds in a town with a local dance show on the air, Cleveland when
    it has one. The producer check is now relational.

### Results (1960, 53 weeks, last 26 weeks)

| Seed | Live non-mime national segments | Breakdown | Weak | Busiest standing acts (dates) |
|---|---:|---:|---:|---|
| 1001 | 161 | 0 | 18 | 15, 14, 14, 13 (was 18, 17, 15, 14) |
| 2002 | 142 | 0 | 17 | 14, 13, 13, 13 |

- **Breakdowns**: about 300 live national segments a year at 0.2-0.5% should give one or two collapses a year; zero in
  52 seed-half-years is inside that. The remaining 21-25 breakdowns a half-year are on local, mimed or taped shows.
- **Busiest guests** barely moved under the per-show gap alone: a standing name rotates across some 20 variety and
  talk shows. History suggests roughly 10-20 national appearances a year for a top singer; the sim gave 26-30.

### Season limit

A name booked on standing or as a comic can hold at most four national dates in any 13 weeks
(`MaxNationalByNamePerSeason`), about 16 a year. Dates in any lane count toward it, including ones already booked
ahead. A current hit still books through the chart lane without it.

| Seed | Busiest acts, last 26 weeks | Standing slots empty | Comedy slots filled | Network slots filled |
|---|---|---:|---:|---:|
| 1001, gap only | 16, 15, 15, 15, 14, 13 | 25 of 640 | 147 | 1,567 |
| 1001, with limit | 11 (10 chart), 9, 9, 9, 9, 8 | 121 of 640 | 81 | 1,405 |
| 2002, gap only | 18, 18, 15, 15, 15, 14 | 70 of 640 | 25 | 1,401 |
| 2002, with limit | 11, 9, 9, 9, 8, 8 | 225 of 640 | 14 | 1,235 |

- The busiest names now get 16-22 dates a year, and the top of the list is mostly acts with current hits.
- **The cost**: 1960's pool of recognized names is thin, so 19-35% of standing slots now go unfilled and comedy fills
  halve. In history about a quarter of a Sullivan bill was not record-business guests at all (opera, Broadway,
  dancers, variety), a share the game doesn't model; read an empty standing slot as one of those. Whether it holds
  once the decade's standard-bearers build recognition needs a decade run.
- Live national segments, last 26 weeks: 149 (seed 1001) and 133 (seed 2002), 0 breakdowns in either.
- **Economy inertness**: 104 weeks, seeds 1001 and 2002: 106/106 economy CSVs byte-identical to `ref-f50b9a4` for each.

**Economy inertness**: 104-week runs at seeds 1001 and 2002 against `SimLogs/ref-f50b9a4-<seed>-104`, same flags as
above: 106/106 economy CSVs byte-identical for each seed.

## Mid-decade check (1963-66, seed 1001)

Resumed `SimLogs/worlds/media-p3-1001-1963.world.json.gz` (1/4/1963) for 209 weeks with
`--enable-genre-market-v2 --enable-artist-population-lifecycle --aggregate-only`, 32 minutes. The world was saved under
ConfigVersion 3 and loads cleanly. Logs: `SimLogs/tvbook/tvbook-{h,i,j}-1001-1963-209-console.log`. The three runs differ
only in year-end diagnostics; their bookings are identical.

Two new year-end lines:
- `MEDIA_STANDING`: name-booked national dates, their acts, recognition quartiles, and how many dates went to acts signed
  by 1960 or to opening careers.
- `MEDIA_PROGRAMS`: chart/standing/comedy appearances per national program.

`MEDIA_AI` also gains `standing-lasthit-*`, the years since the last hit as the booker reads them, counted at booking.
The `weeklyStepMs` field on `MEDIA_YEAR` is cumulative across the process, not per year. Media's own step is about
20-26 s per sim year, roughly 5% of the run.

### Schedule transitions

All of these happened on time:
- American Bandstand: weekday to weekly in Sep 1963 (126 → 52-54 chart dates per half-year).
- Hootenanny: 1963 to Sep 1964.
- Hollywood Palace: from 1964.
- Shindig: 1964-65, about 130 dates per half-year at two a week; off the air in 1966.
- Hullabaloo: 1965.
- Where the Action Is: 1965-66, two chart acts a weekday.
- Dean Martin and Roger Miller: on their start dates.

### Standing and comedy slots, whole year

| Year | Standing filled | Standing empty | Comedy filled / slots | Last hit 0-1 y / 2-3 y / 4 y+ / never |
|---|---:|---:|---:|---|
| 1960 (53-week run) | 519 | 121 (19%) | 81 / 442 | – |
| 1963 | 1,054 | 491 (32%) | 72 / 1,016 | 903 / 30 / 0 / 27 |
| 1964 | 978 | 492 (33%) | 64 / 811 | 707 / 103 / 0 / 70 |
| 1965 | 1,119 | 378 (25%) | 71 / 864 | 904 / 24 / 0 / 34 |
| 1966 | 1,198 | 420 (26%) | 57 / 953 | 956 / 68 / 0 / 61 |

- **Empty standing slots** run 25-33%. That is close to the non-record quarter of a real Sullivan bill, so the season
  limit holds through the decade.
- **Standing guests are recent hitmakers, not standard-bearers.** About 90% of name-booked dates go to an act with a
  top-40 hit in the last year that isn't on this week's top 40. No act whose last hit was four or more years back got a
  single date. History is the other way round: Sullivan's standing guests were mostly long-established names. Acts
  signed by 1960 hold 73% of last-26-week standing dates in 1963, then 64%, 52% and 44% by 1966. The ones still booked
  are the ones still charting. Opening careers are off in this profile (`openingCareersEnabled = false`), so no seeded
  veteran was in the pool. The cause is in the census below.

### Where the old hitmakers went

`MEDIA_VETERANS` (year end) takes every rival act with a lifetime top-40 hit. It splits them by whether a hit record
is still live in the market or has been culled, then sorts each act by the first standing-pool gate it fails.

| Year | Acts with culled hits | Unsigned | Retired | Signed, recognition < 0.20 | Signed, no live record | Eligible | Signed median recognition |
|---|---:|---:|---:|---:|---:|---:|---:|
| 1963 | 860 | 625 (73%) | 84 | 121 | 22 | 8 | 0.04 |
| 1964 | 1,066 | 815 (76%) | 122 | 112 | 15 | 2 | 0.06 |
| 1965 | 1,268 | 977 (77%) | 161 | 110 | 16 | 4 | 0.06 |
| 1966 | 1,458 | 1,103 (76%) | 234 | 103 | 15 | 3 | 0.06 |

For comparison, the acts with a hit still live number 323-356, and 63-97 of them are eligible.

- **Mostly they were dropped.** Three quarters of past hitmakers are off every roster, matching the two-record careers
  finding. Media can't book an unsigned act, because the date has to be charged to a label's campaign.
- **The few still signed have lost their fame.** Their median recognition is 0.04-0.06, so nearly all sit under the
  0.20 floor. Canonical recognition decays without new hits, and nothing preserves a past star's name.
- **The live-record gate barely matters**: 15-22 acts a year.
- **The pool can't see their hits anyway.** `CullDeadRecords` removes old records from `GetAllRecords`, so the pool
  reads a culled-hit act as "never had a hit" (6 years in the recency weight). That only matters for the handful who
  pass the other gates.

### Why hit acts leave rosters

The run is `tvbook-k`, using its `artist-population-events.csv` and `act-release-years.csv`. The script was a one-off in
the session scratchpad.

**Exits.** 711 roster exits in 1963-66 were by acts with a lifetime top-40 hit. Only 9 of them were signed again
before the run ended. "Within a year of a hit" means a hit this calendar year or last.

| Exit | All | Within a year of a hit | Hit two or more years back, or pre-1963 |
|---|---:|---:|---:|
| Performance drop (Declining, flops) | 396 | 169 | 227 |
| Contract expired, not renewed | 286 | 212 | 74 |
| Exhaustion, reconciliation, label closure | 29 | 8 | 21 |

- **Hot exits.** 389 exits (55%) came within a year of a hit. Nearly all were one-hit acts (contract top-40 = 1).
  Most were Rising (205) or had already slid to Declining (174).
- **Contracts.** Contracts run 1-3 years, and 182 of the 286 expiries matured by delivery (releases = obligation). At
  expiry a Rising act is renewed only with momentum > 0.2, otherwise on a loyalty coin flip. A top-40 hit adds just
  0.02 momentum (top 10 adds 0.10), and each flop takes 0.05-0.15 away.
- **Flops.** Any record peaking below 60 or not charting counts as a flop. Two in a row turn Rising into Declining. A
  Declining act is never renewed and is dropped at 50% per review after two more flops.

**Persistence against history.** Acts with a top-40 hit in year Y, against the 1960-64 weekly Hot 100:

| | Any Hot 100 entry, Y+2 | Top 40, Y+2 | Any Hot 100 entry, Y+3 |
|---|---:|---:|---:|
| History (Y = 1960-62) | 38-42% | 22-29% | 31-32% |
| Sim (Y = 1963-64) | 29-33% | 24% | 22% |

Most of the attrition is historical. In history too, about 60% of a year's top-40 acts had no Hot 100 entry two years
later, and repeat top-40 careers in the sim last about as long. The sim falls short only on lower-chart presence,
which fits the known shortfall in hit acts' release volume.

**What looks ahistorical:**
1. **A free agent with a hit is almost never signed again** (9 of 711). Labels did sign proven names in the 1960s:
   the Everly Brothers to Warner (1960), Sam Cooke to RCA, Ray Charles to ABC. Two things block it here:
   - The signing score (`AILabel.EvaluateSigning`) reads a past hit only through momentum and reputation, which a
     top-40 moves by 0.02 and 0.01.
   - Discovery slates are a keyed random sample of the regional pool, so no label goes looking for a known name.
2. **Not renewing a Rising act right after its first hit** (212 expiries). With a single top-40 the momentum test
   almost never passes, so renewal comes down to the loyalty coin.
3. **Not checked: album-selling acts dropped for singles flops.** Albums count toward the flop streak. An adult-pop
   act that stopped making hit singles but sold LPs, the kind of name variety TV booked on, would go Declining and
   then be dropped like anyone else. That needs a look at the dropped acts' LP net before calling it a cause.

### With `--album-scale-career-milestones --hit-act-retention`

The same resume was rerun with both off-by-default switches (`tvbook-l`). The saved 1963 world was built without them,
so the runner prints `RESUME_FLAG_MISMATCH`. Only 1963-66 runs under the new rules. This is one seed, and the switches
reshuffle the stream, so read the small counts as noisy.

| | Control (`tvbook-k`) | Both switches (`tvbook-l`) |
|---|---:|---:|
| Exits of acts with a lifetime top-40 | 711 | 568 |
| Of which within a year of a hit | 389 | 222 |
| Performance drops (hot) | 396 (169) | 493 (195) |
| Contract expiries (hot) | 286 (212) | 35 (8) |
| Signed again after the exit | 9 | 8 |
| 1963 top-40 acts: any Hot 100 entry Y+2 / top 40 Y+2 | 29% / 24% (of 219) | 32% / 29% (of 251) |
| 1964 top-40 acts: any Hot 100 entry Y+2 | 33% | 35% |
| 1963 top-40 acts: any Hot 100 entry Y+3 | 22% | 23% |
| Name-booked national dates, last 26 weeks of 1966 | 569 | 685 |
| Standing slots empty, 1966 | 420 of 1,618 | 213 of 1,618 |
| Culled-hit acts unsigned, end of 1966 | 1,103 | 1,034 |
| Standing dates to acts with last hit 4+ years back | 0 | 0 |

- **Retention closes the hot contract expiries.** They fall from 212 to 8. The 254 renewals by the end of 1965 and
  407 by the end of 1966 are the retention rule at work.
- **Performance drops rise** from 396 to 493. The retained acts are dropped on flops later, once the 52-week
  protection runs out. Some still count as hot here because "within a year" means a hit this calendar year or last.
- **Free agents are still almost never signed again** (8). Neither switch touches the signing score or discovery, so
  that is still the open ahistorical cause.
- **Persistence moves a little toward history.** Top 40 at Y+2 rises from 24% to 29%, at the top of history's 22-29%.
  The Y+3 Hot 100 gap (23% against 31-32%) stays.
  - Caveat: under B1 a charting LP in the album chart's top 40% counts as a top-40 hit. More acts qualify (251
    against 219), and the history comparison is singles-only.
- **For TV**, the bills fill better: empty standing slots in 1966 halve and name-booked dates rise 20%. But the extra
  dates still go to recent hitmakers, and no act whose last hit was four or more years back gets a date. Past
  hitmakers are still mostly unsigned (1,034 against 1,103), because the re-signing route didn't change.

Both main causes sit upstream of media, in roster drops and recognition decay. A media-side fix would need two changes:
- A booker's memory of a name that doesn't decay with canonical recognition, built from existing fields such as
  lifetime `top40Hits`.
- A way to book an act without a label: the agent books the nightclub name, and nothing is charged to an AI envelope.

Both are design choices, not tuning.
- **What standing acts sang** (1963-66, 4,349 dates): standard 72%, current cover 14%, own earlier hit 9%, own record
  5%. History for 1960-64 Sullivan is 80 / 14 / 1 / 6. Closer than 1960's 61% standards; the own-hit share is still high.
- **Comedy** fills 6-8% of its slots, down from 18% in 1960. The comedy pool did not grow.
- **Recognition of standing guests** rises over the decade: the median goes 0.23 → 0.31 → 0.32 → 0.33.

### Sullivan's bill

Last 26 weeks of each year; Sullivan reruns in July and August, so that window holds about 18 new episodes.

| Year | Chart (filled / max) | Standing | Comedy |
|---|---|---|---|
| 1963 (1/2/1) | 19 / 18 | 24 / 36 | 6 / 18 |
| 1964 (2/2/1) | 36 / 36 | 17 / 36 | 2 / 18 |
| 1965 | 34 / 36 | 15 / 36 | 1 / 18 |
| 1966 | 34 / 36 | 23 / 36 | 0 / 18 |

Sullivan averages about three guests an episode against history's 4-4.6 musical acts plus one or two comedians. Its
standing slots fill only 42-67% of the time. Over the same half-years the three daily talk shows (Tonight, Merv Griffin,
Mike Douglas) booked 250-305 standing dates. Since every national date counts toward the four-per-13-weeks name limit,
the likely cause is the weeknight couches using up the limited pool of names before Sunday. This is a hypothesis; no
arm with talk dates exempted has been run.

### Busiest national guests

The top six had 14-23 dates in the last 26 weeks, almost all in the chart lane. 1965 peaks at 21-23, double 1960's 11.
The season limit doesn't apply to the chart lane, and Where the Action Is, Shindig and the talk shows add five or more
weekday chart slots. A hot 1965 act doing about 40 national spots a year is plausible with daily teen television, but
unsourced.

## Open

- **Standing guests are recent hitmakers** (mid-decade check). Of the acts whose hit records have been culled, 76% are
  unsigned and the signed ones sit at about 0.05 recognition, so no old name is bookable. Fixing that needs either a
  booker's memory of a name that doesn't decay, or label-free bookings, or both. Author decision.
- **Talk shows starve Sullivan's standing slots** (hypothesis). Options: exempt talk dates from the season limit, or
  book primetime variety first each week.
- **Career effect of a TV performance**: the outcome feeds fan attachment (`Spark`), press tone and outlets, the
  player's producer trust and letters. Nothing reaches recognition, sales or career state; media is observe-only until
  the Phase 4 adapter. A new act's poorly rehearsed national debut therefore costs it fan evidence and press, not
  fame or units.
- **Empty standing slots** with the season limit: 25-33% through 1963-66 (mid-decade check). Checked only through
  1966; 1967-69 is still unrun.
- **AI planner pitches** still need a chart record. Standing acts get booked because producers call them directly, so a
  standing act's label never sends a disc.
- **The comedy pool** stayed scarce: 6-8% of comedy slots filled in 1963-66. The lane mostly stays empty, which matches
  history only by accident.
- **The game profile**: these runs used bare audit flags. Local shows read regional best sellers, so local bills
  differ under `lp-retail-v2`.
