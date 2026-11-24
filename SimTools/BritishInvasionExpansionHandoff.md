# British Invasion expansion: hand-off (barebones)

Status: sketch, 2026-11-19. Nothing here is built. This expansion carries one committed item, the emergent singles
phenomenon (below). The rest is the frame to fill in when the expansion starts.

## Why this expansion

From 1964 the US singles and LP charts were reshaped by acts from a market the game does not model. The Beatles held the top five of the
Hot 100 on 4 April 1964, and "I Want to Hold Your Hand" sold about 3.4M in the US by March 1964. Nothing in 1960-63 came close
(LpGenreAllocationFollowup.md §97: Elvis's "Are You Lonesome Tonight?" passed 2M in under three months, and most No. 1s sold 0.8-1M).
A popularity explosion of that size belongs with the arrival of a new scene and a new audience, not with the 1960 US market.

## What already exists

- Genres: `BritishBeat` (window 1963-67) and `BritishPop` in `Data/GenreCatalog.cs`, with era chart weights (BritishBeat
  peaks at 24 in 1964). Legacy `BritishInvasion` maps to `BritishBeat`. `MarketRegion` gives BritishInvasion a 1964-67 term.
- Labels: four London templates (Parlophone, Decca UK, Pye, EMI) in `Systems/AILabelFactory.cs`, dropped under
  `--historical-majors` / British label deferral (they were never part of the 1960 US game). UK cities map to the
  `eastcoast` region. There is no UK market, no UK chart and no licensing of foreign masters.
- Phenomenon prototype: `--singles-phenomenon=S` (`Systems/SinglesPhenomenon.cs`, §97, research switch, not adopted). It has
  an act-level smoothed national singles share, a once-per-career onset, a weighted ration
  (`SinglesPhenomenon.Shares`, capped at intent, purse spent exactly), checks in `GameyAuditSwitchChecks`, and the scorer
  `SimTools/score_singles_phenomenon.py`.

## Scope sketch (to be designed)

1. **A British pool, not a UK market** (author, 2026-11-19). No UK chart or UK economy. The game models how the US market
   received the imports: an abstract pool of Britain's top acts (each carrying a UK standing such as "UK No. 1s" or "home following", plus
   genre, quality and a UK label), refreshed as the scene develops. US labels draw from it.
2. **Transatlantic licensing.** US labels license or pass on UK masters (historically Capitol, Vee-Jay, Swan and London
   Records), and a pass costs the US label the act.
3. **Arrival and the scene wave.** After a breakthrough, other UK acts and US imitators (garage, folk rock) follow, through
   the existing formation and genre-market machinery.
4. **The emergent phenomenon** (committed, below).
5. **Player challenge.** A US act can also become a phenomenon after 1964 (author). Turning the tide against the British is
   meant to be a goal for the player, so the mechanism must be open to any act with the audience, not reserved for imports.
6. **Guards.** 1960-63 must be untouched. Big-5 family share bands need re-basing for 1962-64 (§96: Epic, Smash and Philips matter from
   1962). Top 10 turnover, closures, and the LP chain all need checks: stage A's act audience lifts a phenomenon act's LP only to about 0.6-0.8M,
   against *Meet the Beatles!* at 3.65M in about ten weeks.

## The emergent phenomenon: to implement with this expansion

**What §97 showed.** The prototype's onset is emergent: it picks the act that already leads the market. Its size is not.
Against the same world without the switch, about 80% of a phenomenon's share came from the fixed ration weight W, about 10% from
the fixed audience term A, and under 10% from chart feedback. The act's own demand barely changed. So the prototype gives a natural leader a
number we chose.

**What the redesign must do instead.** The size has to come from the act's audience:
- **A growing following.** Follower recruitment should compound while the act dominates (radio, TV, press and peer word
  of mouth: the media atmosphere and follower systems already exist), and those followers should buy on sight, with first claim
  on the purse (the die-hard tier, author decision of 2026-11-10).
- **New buyers entering the market.** Beatlemania brought new young buyers in. Part of the act's demand should add to the
  singles purse rather than displace other records. Size it from history (author): the US singles market's actual 1963 to 1964 growth. Not yet found (2026-11-19 searches): no
  annual US singles units for 1963-64 surfaced. Leads: Billboard 1964 issues on worldradiohistory (a 1964 item calls August
  "the highest [singles month] in the history of the industry"; a January 1964 report says hits from abroad "more than tripled in 1963"),
  the January 1965 *Survey of Current Business* (FRASER), and *Music Business* 1964.
- **Earned fade.** It ends when the audience cools (no new records, follower ageing and cold loss), not on a fixed clock.

**Constraints already set by the author.**
- Rare: a Beatles-scale act should be possible but should not appear in every run. Score the distribution, not the peak
  (memory: beatles-ceiling-is-rare-not-a-target).
- Not before late 1963: a very, very small chance in 1963 is allowed (author, 2026-11-19), none in 1960-62. The 1960-61
  control already spans the pre-1964 singles range without any phenomenon (biggest single 1.4-2.0M).
- No fixed multipliers. If a weight is needed, it must be derived from the act's audience state.

**Reuse from the prototype:** the share ledger and onset telemetry, the weighted ration helper, the scorer, the inertness
test pattern (byte-identical below onset), and §97's decomposition method (arm against control, same world to onset).
**Drop:** the fixed W and A and the 52-week fade clock.

## Author's answers (2026-11-19)

- UK market: no full market. Simulate the US receiving the imports, with an abstract pool of Britain's top acts.
- Domestic phenomena: yes, after 1964. A US act can turn the tide, and that is a player goal.
- Market growth: size it from history (sourcing still open, see above).
- Timing: a very, very small chance in 1963 is allowed.

## Pointers

- LpGenreAllocationFollowup.md §69, §73 (stages A-C), §74-76 (stage A), §97 (the prototype and its decomposition).
- Memory: singles-phenomenon-stage-c, singles-ceiling-is-flat-ration-not-followers, follower-is-die-hard-plus-casual,
  singles-volume-is-a-fixed-purse.
