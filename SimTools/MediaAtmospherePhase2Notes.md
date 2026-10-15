# Media Atmosphere — Phase 2 implementation notes

**October 15, 2026, branch `media-atmosphere`.** Phase 2 of `MediaAtmosphereDirective.md`: journalist and producer
interactions, observe-only. It also answers a design question the author raised: *is a Beatles-night audience possible,
with proper hype?* It is now, rarely, and nothing routine gets near it (see "Event nights").

## What is built

| Piece | Files | Notes |
|---|---|---|
| Performance results (§6) | `Systems/Media/MediaPerformance.cs` | Musical and production results kept apart. The method decides what's exposed: a mime exposes no musicianship (the master is the sound, so a failure is a picture problem); a live vocal exposes the lead singer's own `vocalControl/vocalPower/diction`; a live band mixes the singer with the players, the weakest hands pulling it down (a great guitarist doesn't rescue a weak singer). Presence reads `stagePresence`, lead-weighted. Preparation (Minimal/Standard/Extra) has diminishing returns. Equipment trouble is its own draw, never the act's fault. Tape allows a retake with the producer's cooperation; a taped breakdown nobody retakes is cut in the edit. Outcomes: Breakdown, Weak, FlatAdequate, Engaging, Electrifying, with fault attribution and a one-line account. Pure reads, keyed draws. |
| Standards desk (§7) | `Systems/Media/MediaStandards.cs` | Per-program strictness by tag, severity, year and region (family variety strictest; the Smothers' network tightens on war and politics in 1968-69; a Deep South local station is warier of religion and sex). Reads the authored `SongContentTags` from the record's composition, naming the song when the disc goes out under another title. A keyed draw decides uncertain cases, so an ambiguous word doesn't always trigger a ban. Responses: agree to the edit, offer another song, ask the producer to fight for it, withdraw, promise then defy, accept removal. A mime can't edit a lyric or defy. Network clearance can diverge from producer support. Consent is the act's: an act with creative control decides on edits, and a label can never order a defiance on someone else's microphone. Rival acts answer from their existing disposition (`ArtistEvolutionProfile`), creative control and manager. |
| Consequences | `MediaAtmosphereService.Press.cs` (`AfterAir`) | Settled once, when the date passes. Program memory (§2): delivered, withdrawn, breaches. A breach shuts that program's door for three years and puts the act under a one-year network review (its other shows book it at half the rate), never an industry blacklist. Media-owned audience attitudes ([artistic, family, teen], 10%/week back to neutral): a defiance warms the artistic audience and cools the family one; a compromise costs artistic credibility only if it becomes public. A defiance seeds one verified controversy episode with a 12-week life. |
| Event nights | `BroadcastExposure.cs`, `MediaAtmosphereService.Press.cs` | See below. |
| Press desks (§8-§9) | `MediaPress.cs`, `MediaOutletCatalog.cs`, `MediaAtmosphereService.Press.cs` | Outlets now have cadence, issue day, print lead time, a finite music budget, a home city and an audience fit. Added: nine Black newspapers plus Jet and Ebony, Country Song Roundup, Music City News (1963), Down Beat, Sing Out!, Broadside (1962), four underground papers (LA Free Press 1964, Berkeley Barb and East Village Other 1965), and a fictional daily in each of the 31 cities. Roles (editor, reporter, reviewer, columnist, photographer) with stable hashed names. Weekly pass after the TV step: chart news, newly breaking markets, aired segments with their outcomes, announced headline dates, public standards disputes, teen features, cultural-salience features, critics' reviews and the label's own press sheets become leads. Editors take what interests their readers, one item per story root, into the first issue the lead time allows that has room; the rest are killed. Critics' tone is their own (record craft plus a keyed taste). |
| Evidence and privacy | `MediaPress.cs` | Every item carries evidence records: source, fact, firsthand or not, confidence, day. The source enum has no member that could carry private state: chart, regional sales, broadcast, witnessed public incident, label statement, other publication, the record itself. Nothing in the media code reads `substanceLoad`, fatigue, wealth or band-room events. |
| Player verbs | `MediaAtmosphereService.Player.cs`, `PlayerDesk.Media.cs`, `UI/PlayerDeskPanel.Media.cs` | PRESS & TELEVISION in the office. Send a disc to a producer (local and syndicated shows on the home and current dials, any network show): answered in 2-4 days on fit, local evidence, the relationship, the discovery slot, and for networks the chart. An offer names a real unbooked date, the slot, the on-camera method, an audience range and teen share, travel and preparation costs, and any standards question, with a four-day deadline. Confirming reserves the slot against that episode before the booking pass fills it. After the air date the office hears how it went, the producer remembers it, and some letters arrive. Send a press sheet to a desk with an angle; the editor decides at the next issue. Reporters call before an issue closes on a breaking record: real figure, inflated figure (checked against dealers' reports three to six weeks later; a follow-up runs if it didn't hold), or no comment. Teen editors ask for photographs: exclusive (named outlet, four-week expiry), supplied, or declined, with a romance policy; the act can refuse; a planted romance is stored as a claim and may be contradicted later. Ask for a correction: it runs under the same root and the original stays in history. |
| Observe-mode costs | | Every cost is printed as an estimate and nothing is charged: no hours, promo copies or cash (§12.1). Checked. |
| Paper and Rolodex | `PlayerDesk.Media.cs`, `UI/UIManager.cs`, `RolodexDirectory.Press.cs` | IN THE PAPERS rubric: today's items about the label first, then the town's paper, then the biggest national items, one per story root. Producer cards now show what the producer remembers of the label and link to the office; press desk cards for the town's papers and every desk the label has dealt with, with rhythm, what the desk wants, what it ran lately, and concrete memories ("Your figure on X didn't hold up against the dealers' reports"). |

## Event nights: the Beatles-night question

**Before Phase 2 it was impossible by construction.** Program share took a guest's draw only up to +10% of baseline, so
the hottest act on the hottest Sunday reached about 21M.

**The mechanism now.** Network bookers *announce* a headline date 2-4 weeks ahead (a hold) for a record that already
looks like an event. The announcement gives the press time to write about it. At the air date the headliner's hype is

```text
precursor = (0.45 × reign + 0.35 × size + 0.20 × several hits) × share of regional markets broken
    reign = weeks at No. 1 / 6 (a season, not the usual two or three weeks), plus a top-three bonus
    size  = how far this week's sales exceed an ordinary No. 1 (media's own running average)
    several hits = other records by the act in the top 20 at once
hype = precursor × novelty (first visit to the show, and to primetime) × (0.3 + 0.7 × advance coverage)
    advance coverage = distinct press families running an item in the six weeks before air, mainstream counting twice
```

Hype drives two bounded effects on the *whole episode* (an advertised headliner brings viewers for the bill):
share taken from competing programs, up to +90% of baseline, and, above hype 0.5, tune-in, where up to 35% of the people
who would not have watched at that hour switch on. Below hype 0.25 nothing happens. Network teen shows get 60% of the
effect; local shows none. A routine guest's +10% cap is unchanged.

Sullivan on Sunday, February 9, 1964, closing spot (`MediaAtmosphereChecks`):

| Hype | Reached |
|---|---|
| 0 (routine) | 20.7M |
| 0.45 | 24.6M |
| 0.7 | 38.4M |
| 1.0 (maximum) | 49.7M, 31% of the model's 158M; 40.2M simultaneous |

Hype near 1 needs six weeks at No. 1, sales about double an ordinary No. 1, two more top-20 records at once, every
region broken, a first appearance, and advance coverage across five press families. Each of those is rare; together
they may never happen in a given world, which is what the author asked for. The absolute numbers sit below the
historical 73M because the model reports one segment's unique reach in a 158M population, and its routine audiences
already run below household-based estimates; the ratio to a routine night (about 2.4×) is the meaningful figure. If
absolute parity matters, the viewing curves are the knob, not the event ceiling. Phase 3's fan episodes join the hype as
the proper "cultural phenomenon" gate.

## Verification

- `MEDIA_ATMOSPHERE_CHECKS_PASS checks=91` (seed 1001; 49 from Phase 1, 42 new): fixtures 1, 6, 7, 8, 9, 10 and the
  Phase 2 parts of 15, the event-night ladder, page budgets, one item per root per outlet, outlet dates, observe-mode
  costs uncharged, no RNG draws on reads, and the Phase 2 save round trip.
- 104-week inertness, ref flag set, hashed against `ref-a358946-<seed>-104`:

  | Arm | Seed | Economy CSVs | Wall |
  |---|---|---|---|
  | Observe (default), before the outcome-threshold retune | 1001 | 92/92 identical | 1083 s (3 parallel) |
  | Observe (default), before the retune | 2002 | 92/92 identical | 1028 s (3 parallel) |
  | `--disable-media-atmosphere` | 1001 | 92/92 identical | 1078 s (3 parallel) |
  | Observe, final code | 1001 | 92/92 identical | 741 s (2 parallel) |

  The weekly media step costs ~3.3 s per simulated year (~63 ms/week, up from ~20 ms in Phase 1), still under 1% of wall.
  In 1960: ~1,050 press items in the last 26 weeks (local dailies 463, specialist 307, trades 230, teen 30, critics 20,
  mainstream 2), one standards query, and segment outcomes of 9% electrifying, 31% engaging, 55% flat but adequate and
  4.5% weak or broken down. No headline date in 1960-61 reached the 0.25 hype floor (the highest was 0.18), so no event
  night happened. That is the intended rarity, but it rests on calibration nobody has checked against mid-decade worlds yet.
- Office UI exercised in the real game (temporary driver scene, Tex Records save, Dallas): the PRESS & TELEVISION
  section renders with both approaches, local-clock slots and estimate-only cost lines, and media notes reach the paper.

## Known limits and next steps

- Standards queries are rare in the early 1960s: only 197 of 4,980 catalog titles carry any tag, mostly mild alcohol words.
  That is a property of the title-only lyric evidence, not of the desk.
- Rival acts' standards answers use a simple disposition rule; the Phase 3 planner replaces it.
- Teen attachment, fan episodes and member spotlight are Phase 3. Hype's "cultural phenomenon" leg waits for them.
- Scenes are office cards, not Rolodex-style beat-assembled calls. A producer call scene would reuse the Rolodex grammar.
- Rival press items are kept seven weeks (what the hype window reads); the label's own clippings keep 26 weeks.
