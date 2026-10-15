# Media Atmosphere — Phase 3 implementation notes

**October 15, 2026, branch `media-atmosphere`.** Phase 3 of `MediaAtmosphereDirective.md`: AI characterization and fan
episodes, observe-only. Rival labels now act on the same programs, slots, desks and page budgets as the player. Acts,
the player's and the rivals', build teen followings from their own careers. The Phase 2 event-night hype gets its
"cultural phenomenon" gate.

## What is built

| Piece | Files | Notes |
|---|---|---|
| Teen attachment (§2, §10.1) | `Systems/Media/MediaFans.cs`, `MediaAtmosphereService.Fans.cs` | Per act and region, `[0,1]`, eight-week half-life, `a += gain × (1 − a)` with a 0.12 weekly cap. The week's gain reads three sources. **Sales**: the act's best rank in the region's top 20 sellers. **Television**: the share of the region's teenagers who saw it (national airings spread by population × clearance, local and syndicated airings to their own region), scaled by how the segment went. **Coverage**: distinct teen, mainstream or local outlets running a fan-relevant item. Chart news and breakout items are excluded because they retell the hit. Youthful fit scales the gain (`0.3 + 0.7 × TeenFit`). Television without a selling record gets 30% of its weight: familiarity, not attachment. |
| Fan episodes (§10.2) | `MediaFans.cs` | Local excitement, regional favorite, national sensation, cultural phenomenon, cooldown. Evaluated weekly over a four-week window. Local excitement needs one qualifying week, regional favorite two, national sensation two, and cultural phenomenon four in a row. A national episode that fades steps down: a phenomenon becomes a sensation, and a sensation ends in an eight-week cooldown in which nothing national can start. Evidence families are counted as kinds (sales, television response, coverage), so ten papers repeating one event count as one. National levels need at least one audience-behavior family. A long run on the same record sets a plateau flag ("editors want a new angle"). |
| Member spotlight (§10.2, §12.4) | `MediaFans.cs`, `.Fans.cs` | A media-only per-member score that decays over about 26 weeks. Each airing splits the camera across members by keyed shares: the lead singer usually, but stage presence, youth and a per-appearance draw let a drummer catch it (lead on top 95% of the time in the fixture). Teen-magazine photo sessions put one member on the cover. The letters name a member only when one clearly leads. It never touches `personalRecognition`. |
| Hype gate | `MediaAtmosphereService.Press.cs` | Hype is capped by the act's episode. With no episode (or in cooldown) it stops at 0.5, the tune-in threshold. A regional favorite reaches 0.55, a national sensation 0.8, and only a cultural phenomenon reaches 1.0. The Beatles-night ceiling now requires fans to be part of the story. |
| Fan stories (§10.2 world response) | `.Fans.cs` (`FanLeads`), `MediaPress.cs` | When an act's episode changes state, it becomes a lead. Local excitement goes to the home daily ("Cleveland teenagers turn out for …"); a regional favorite to the region's papers and the teen magazines; a sensation to the teen magazines and trades; a phenomenon to the mainstream magazines; a cooldown to "Whatever happened to …?". Facts come only from sales reports, mail to the programs, and what other papers ran. Teen features rank higher with a following, and their copy names the face the letters are about. The mainstream "cultural salience" assignment now requires a national episode as well as the chart run. |
| AI policy (§13) | `Systems/Media/AiMediaPlanner.cs` | Teen-pop cultivation (TeenHitMachine, SoulFactory), commercial pragmatism (CorporateGiant), artistic protection (FolkBoutique, JazzPrestige), specialist/community (BluesRoots, Country, Gospel), opportunistic (RockRebel, RegionalHustler). Per act: a live controversy makes any house cautious, and a Visionary manager or an ambitious act with its own creative control means artistic protection. Policies are derived, never rerolled. |
| Allocation view (§12.2, fixture 14) | `AiMediaPlanner.cs`, `MediaAtmosphereService.Ai.cs` | Per campaign record, the envelope estimate is `AILabel.GetMarketingBudget(artist)`, a pure read of what the release charge budgets. It is split in whole cents into radio, TV, press, other and reserve by policy, with the reserve taking the remainder, so the parts always sum exactly. Bookings and sheets reserve against their line under one receipt each. A withdrawal at standards releases the reservation. Nothing is debited, and the release charge in `CompetitorManager` stays the accounting authority. |
| Costs | `AiMediaPlanner.cs` | Shared with the player's offer note. Rehearsal: $0, $15 or $40. A local show in another town costs the trip: a day's drive up to 150 miles, a car and one night up to 600, fares and two nights beyond that, two people to a room. **A network production pays its guests' fares and hotel**, so a national date costs the label only the rehearsal. Without this, half of every network bill went empty because small labels couldn't fly their acts in. |
| Producer calls (§13) | `MediaAtmosphereService.cs` (`BookEpisode`), `.Ai.cs` (`AiAccepts`) | When a producer reaches a rival record (headline hold, discovery slot or regular bill), the label answers. Policy and the act's consent come first: a specialist house won't chase a national teen slot for a non-teen record; an artistic act sometimes refuses to mime for the dancers; a house in crisis keeps its act off primetime. Then the TV line must cover the date, trying minimal rehearsal before declining. A declined slot passes to the next act in line. Headline holds are announced only for acts whose label would take the date. |
| Weekly planner (§13) | `.Ai.cs` (`AiPlanWeek`) | Each rival label picks at most two campaign records: on the national chart, or in a region's top 10. For each it sends at most one disc and one sheet, chosen by the policy's audience fit and what the envelope can afford. A pitched record gets a 1.2× look from that producer; nothing is guaranteed. Sheets become leads for exactly the desk they were sent to and are scored by the same `SheetInterest` rule as the player's, so editors choose from one combined pool. |
| AI photo answers (§8.2) | `.Ai.cs` (`AiAfterDecide`) | When a teen feature about a rival act is scheduled, the label answers by policy: teen-pop houses give exclusives, commercial houses supply photos, artistic acts often decline ("the magazine ran an old publicity still"). The player's own photo answers now also put a face in the spotlight. |
| Standards (§13) | `MediaStandards.AiChoice` | Rival answers come from policy first. Commercial, cautious and teen-pop houses take the edit if the act consents. Artistic houses offer another song or ask the producer to fight, unless a volatile act with control makes its own call. Specialist houses send a different record. |
| Player view | `PlayerDesk.Media.cs` (`FanLines`), `UI/PlayerDeskPanel.Media.cs`, `RolodexDirectory.Press.cs` | PRESS & TELEVISION now shows THE FANS: where the label's acts have a following, their letters, and whose face the mail is about, plus the loudest rival episodes. It is labeled as distinct from the act's standing in the business. Teen-magazine desk cards say which faces the readers keep writing in about. |
| Lead truncation fix | `MediaPress.Decide` | The weekly lead cap (400) used to take leads in arrival order, and player and AI sheets arrived last. Leads are now read player-first, then by interest, so a crowded week drops the weakest lead, never a sent sheet. |

## Where the numbers stand

1960, from the 104-week runs (year-end telemetry prints only after Dec 25, so 1961 has no summary line):

| | Seed 1001 | Seed 2002 |
|---|---|---|
| Acts with a fan record at year end | 283 | 269 |
| Local excitement / regional favorite | 2 / 1 | 3 / 2 |
| Ever a national sensation or phenomenon | 0 | 0 |
| Campaign allocations (all balanced to the cent) | 641 | 612 |
| TV line reserved / available | $52,764 / $71,853 | $44,903 / $71,839 |
| Rival dates accepted: local / network | 3,173 / 620 | 2,577 / 620 |
| Rival TV pitches: local / national teen / primetime | 809 / 847 / 854 | 553 / 862 / 1,097 |
| Rival press sheets (trade, specialist, local, teen, critics) | 675 / 393 / 319 / 274 / 15 | 743 / 385 / 269 / 240 / 18 |
| Rival photo answers: exclusive / supplied / declined | 8 / 37 / 7 | 20 / 27 / 17 |

The decline counters are large (decline-budget 125,000 on seed 1001) because each counts one producer reaching one
candidate for one episode. Daily local shows go down a 40-record list. Network bills fill completely. No 1960 act came
near a national episode, so the hype gate never bound and no headline night exceeded hype 0.18, the same as Phase 2.
That rarity is intended but untested against mid-decade worlds.

## Verification

- `MEDIA_ATMOSPHERE_CHECKS_PASS checks=122` (seed 1001; 91 from Phases 1-2, 31 new). New checks cover fixture 11 (a
  regional favorite stays regional; one week is not a sensation; coverage alone or mail alone never makes an episode;
  a phenomenon needs four extraordinary weeks; cooldown blocks re-entry for eight weeks) and fixture 14 (1,800
  envelopes across all policies sum to the cent; live allocations' receipts equal their reservations; envelope
  estimates and AI answers draw no RNG and move no label's money). They also check that player and rival bookings share
  each episode's finite slots, that the planner is bounded, that fan stories cite only public sources, that spotlight is
  held by real members, that reads are draw-free and repeatable, and the Phase 3 save round trip.
- 104-week inertness, observe (default), ref flags, hashed against `ref-a358946-<seed>-104`:

  | Seed | Economy CSVs | Wall (2 parallel) |
  |---|---|---|
  | 1001 | 92/92 identical | 570 s |
  | 2002 | 92/92 identical | 541 s |

  The disabled arm was not rerun. Every Phase 3 path sits behind `MediaAtmosphere.Observing`, and Phase 2's disabled
  arm on this ref is unchanged (reuse-control-runs convention). The media step's cumulative cost in 1960 was 5.6-6.7 s,
  about double Phase 2 and still around 2% of wall.

## Known limits and next steps

- **Local shows go guestless more often.** Rival labels now have to afford the trip to a distant town's dance show, and
  a small label's TV line covers about one. About 44% of local slot-days in the 1960 fixture went unfilled after
  declines. Most local dance shows were records and dancers on most days, so this reads as plausible, but it is a change
  from Phase 2, where every booking was free. Use the `slots-local-*` ledger counters if it needs tuning.
- AI tours and gigs are not visible to media, so "spare promotional availability" (§12.2) is approximated by one date per
  act per day and the existing weekly network cap. An act passing through a town for a gig doesn't get a free local
  spot.
- Radio interviews and requests (§11) are not in this phase.
- The episode thresholds and gains are initial design values. They need a mid-decade world to check that national
  sensations stay rare and phenomena rarer. A 1960-61 window says little about 1964.
- The office's THE FANS block was exercised headlessly (`FanLines`), not yet in the real game window.
