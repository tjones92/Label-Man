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

## Open

- **Career effect of a TV performance**: the outcome feeds fan attachment (`Spark`), press tone and outlets, the
  player's producer trust and letters. Nothing reaches recognition, sales or career state; media is observe-only until
  the Phase 4 adapter. A new act's poorly rehearsed national debut therefore costs it fan evidence and press, not
  fame or units.
- **Empty standing slots** with the season limit: see the season-limit results; check on a decade run.
- **AI planner pitches** still need a chart record. Standing acts get booked because producers call them directly, so a
  standing act's label never sends a disc.
- **Recognition durability** for the 1960 standard-bearers through the decade is unmeasured. The 53-week run shows them
  booked steadily in 1960; a decade run will show whether they fade off the bills by about 1963.
- **The comedy pool** depends on how many comedy acts the world signs. If comedy records stay scarce, the comedy lane
  mostly stays empty, which matches history only by accident.
- **The game profile**: these runs used bare audit flags. Local shows read regional best sellers, so local bills
  differ under `lp-retail-v2`.
