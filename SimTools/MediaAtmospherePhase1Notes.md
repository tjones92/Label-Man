# Media Atmosphere — Phase 1 implementation notes

**October 15, 2026, branch `media-atmosphere`.** Phase 1 of `MediaAtmosphereDirective.md`: historical institutions,
peak audiences and exposure accounting, observe-only. Three requested additions ride along: a PRESS & TV Rolodex role,
authored song content tags, and an emergent rework of how trade signals combine.

## What is built

| Piece | Files | Notes |
|---|---|---|
| Switch + keyed draws | `Systems/Media/MediaAtmosphere.cs` | `Observe` is the scene default (`ChartManager.mediaAtmosphereObserve`). Flags: `--disable-media-atmosphere`, `--observe-media-atmosphere`. Causal flags are rejected, not ignored. `Unit(key)` = world seed + `|media|v1|` + key; no stream is touched. |
| Catalog | `Systems/Media/MediaOutletCatalog.cs` | National: American Bandstand (weekday → Saturday Sep 7 1963, Philadelphia → Los Angeles Feb 8 1964), Where the Action Is, Shindig!, Hullabaloo, Ed Sullivan, Hollywood Palace, Smothers Brothers. Documented locals: Buddy Deane (Baltimore, to Jan 1964), Lloyd Thaxton (LA, syndicated from 1964), The Big 5 Show/Upbeat (Cleveland, syndicated from 1966). Fictional local dance show in every one of the 31 player cities, two in New York and Chicago, keyed per world and clipped around the documented shows. Press outlets with real opening dates. Every schedule carries `SourceConfidence` and a source note. |
| Exposure | `Systems/Media/BroadcastExposure.cs` | Four viewer groups (viewing only, no purchasing). 15-minute local-time integral of `population × group share × TV access × viewing likelihood × clearance × program share × segment retention`. Separate peak (absolute-time buckets), average, viewer-minutes and unique reach. Carriage rules: network primetime same clock East/West, an hour early Central/Mountain; network daytime simultaneous, with the West Coast taping an Eastern afternoon teen show into after school. ABC clearance reads each region's `bandstandReach` prior; CBS/NBC clear ~95-97%. Guest draw capped at +10% of baseline share. Repeats run at 0.55 novelty. |
| Bookings and ledger | `Systems/Media/MediaAtmosphereService.cs` | An explicit step in `ChartManager.OnWeekEnded` after settlement: tally last week's airings, then book Saturday-Friday from the chart that just froze. Producers pick on genre fit × evidence (national chart position, or the region's best sellers with a hometown bonus for locals), with a keyed spread. One discovery slot per teen show goes to a record breaking regionally or off the top of the chart. Finite calendars: one place per act per day, a rebooking gap per program, at most two national dates a week. Summer reruns replay an appearance from ~16 weeks earlier. Syndicated tapes become their own dated airings per city. Rival acts only; player acts wait for Phase 2 negotiation. 26 weeks of history kept; per-act tallies kept forever. Saved in `WorldSaveData.Media`. |
| Paper | `Systems/Media/PlayerDesk.MediaPaper.cs`, `UI/UIManager.cs` | "ON TELEVISION" rubric beside THE TRADE: today's listings in the player's town (network shows that clear there plus its local shows), last night's biggest network audience as a rounded range, and yesterday's local dance-show audience in teenagers. Read-only; audiences are printed only after airing. |
| Rolodex | `Systems/Media/RolodexDirectory.Press.cs`, `Systems/RolodexDirectory.cs` | New `RolodexRole.Press` ("PRESS & TV"), appended last so saved enum values are unchanged. Cards: producers of local shows on the dial in the home and current towns (syndicated tapes included), talent bookers of network shows that clear there, and the Cash Box singles desk once the label has submitted a record. Facts: slot in the town's local clock, what they book, on-camera method, recent guests, repeat months. "Pitching isn't open yet" is stated, not hidden. |
| Content tags | `Systems/Media/SongContentTags.cs`, `Data/SongComposition.cs`, `Systems/PolarSongMetadataService.cs` | See below. |
| Trade signals | `Systems/PlayerDesk.TradePress.cs`, `Systems/PlayerDesk.cs` | See below. |
| Checks | `SimTools/MediaAtmosphereChecks.{cs,tscn}` | 49 checks covering fixtures 2-5 and 15 of §16.1, content tags, trade arithmetic, real bookings, no-RNG reads, and the save round trip. |

## Content tags (§7.1)

The game's only lyric text is the title, assembled from a tagged lexicon, plus the lyric mode some songs carry. Tags
come from that evidence and nothing else. A whole-word match on curated cues (the lexicon's `vice`, `protest` and
`conflict` groups plus the period standards vocabulary) yields `alcohol`, `drug-reference`, `sexual-suggestion`,
`religious-provocation`, `political-protest`, `anti-war`, `violence-crime` or `explicit-language`, each Mild or Marked.
`TopicalTestimony`/`InvectiveAccusation` lyric modes promote protest and war words to Marked. A sacred song's sin and
devil are not provocations. Each song stores tag ids, `tag|severity|evidence` lines, the basis (`title-v1`) and the
title it was read from, so a retitled song is re-read fresh. No tag means "nothing in the title", never "proved clean".
Authored in `EnsureComposition`: every new song and every reload. Draw-free. On seed 1001, 197 of 4,980 catalog
songs carry a tag, mostly Mild alcohol words in pop titles ("Take Me to the Wine"). Nothing reads the tags yet; the
standards desk consumes them in Phase 2.

## Trade signals: emergent saturation, not a cap

**What was wrong.** A pick, a breakout and an ad simply added together: mailing landing up to +0.62, inbound
multiplier up to 5.9×. The inbound multiplier barely mattered at the top end, since the weekly stranger-call chance
(0.35 × multiplier) already clamps at 1 with a single breakout. What actually capped a hit was
`StrangerCallsPerRecordPerWeek = 1`: a record breaking in every region at once still rang the office once a week. So
the old code over-rewarded buying attention and under-rewarded a genuine phenomenon.

**The model.**
1. **Press is one voice.** A review pick and a paid ad tell the same readers the same thing ("this label is working
   this record"). They combine as `max + 0.35 × min`, which saturates instead of stacking. Spotlight plus a full page
   is 0.327 on mailing, where it used to be 0.47.
2. **Evidence is a different signal and grows with breadth.** The breakout term scales as `1 + 0.5(√n − 1)` over n
   breakout regions: one market is the old value, seven is 1.82×. On mailing it combines with press as two independent
   reasons to open the envelope: `1 − (1 − press)(1 − evidence)`.
3. **The flood is earned, and it cools.** The weekly stranger-call allowance is 1, plus one for every two further
   markets breaking *right now* (current `breakoutScore`, not the peak): 3 live markets give 2 calls, 7 give 4. Press
   and ads never raise it. Calls still land region by region, so a national breakout rings from several towns.
4. **Unchanged:** any single signal alone keeps its exact old value; house-visit acceptance and the ad's connect bonus
   were already label-wide maxima.

This is a design argument from how 1960s distributors read the trades: picks and ads as a label's claim, regional
breakout reports as evidence. It is not a measured calibration. It is player-only, and the AI economy is untouched.

## Verification

- Observe-mode booking core (before tags, trade and paper): 92/92 economy CSVs byte-identical to
  `ref-a358946-{1001,2002}-104`.
- Final code, 104 weeks, ref flag set, hashed against `ref-a358946-<seed>-104`:

  | Arm | Seed | Economy CSVs | Wall (3 runs in parallel) |
  |---|---|---|---|
  | Observe (default) | 1001 | 92/92 identical | 659 s |
  | Observe (default) | 2002 | 92/92 identical | 627 s |
  | `--disable-media-atmosphere` | 1001 | 92/92 identical | 654 s |

  The weekly media step costs ~1.0-1.1 s per simulated year (~20 ms/week), under 1% of wall time. In 1960, ~400 acts
  appear on television, with 296 national appearances. The most-seen act's estimated reach is 42-46M of ~158M, after
  return visits to the same program were discounted to 25% new viewers (it was 81-87M before).
- `MEDIA_ATMOSPHERE_CHECKS_PASS checks=49` (seed 1001).

## Known limits and next steps

- Program-share attractions, viewing curves and metro populations are initial design values. Bandstand weekday hours
  in 1960-62 and Hullabaloo's 1965 hour are marked approximate. The Upbeat syndication footprint is a composite.
- Bookings read records and chart positions only; performance quality, rehearsal and travel feasibility are Phase 2.
- Phase 2: contacts and verbs (pitch a producer, interviews, standards queries that consume the content tags,
  photo/exclusivity terms).
