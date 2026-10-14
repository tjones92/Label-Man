# Scouting continuation: rival courtship, historical advances and Phase 5 feedback

October 11, 2026. Branch `scouting-experience-expansion` (worktree `C:/Project/Label-Man-scouting`). It builds on commit `808527e`, which committed Phases 1-4 from the old `tmp/scouting-unsigned` snapshot. Everything new is **on by default**, per the user's instruction. The decade runs are deferred until Phase 6 is finished.

## 1. Historical advances (`Systems/ContractPricing.cs`)

**The research.** An act with no record behind it got little or nothing up front:
- The Beatles' 1962 EMI contract paid one penny per single on 85% of sales.
- 1950s royalties ran 3-5% on 90% of sales.
- Labels recouped recording costs from the act's royalties.

What a major offered an unknown was distribution, promotion men, a studio and its name. It did not offer a bigger cheque. Real money followed a track record: RCA's $35,000 buyout of Elvis came after Sun had already made him a regional star. The legacy model contradicted this. Its AI price started a major at $5,000 and multiplied by 1.5 for "competition", so the fixture act's major offer came out at $9,867.

**The new model:**
- **Going rate.** The advance is the act's own going rate. It uses the same room bands as the player's ask, which now live in `ContractPricing.VenueBase`.
- **Tier premium.** A bigger buyer pays a modest premium: Major 1.6×, MidTier 1.35×, Boutique 1.1×, Independent 1.0×, Small 0.8×.
- **Track record.** The advance moves geometrically to the legacy figure as the act's record proves out. `ProvenShare` reads the best of `careerState` and `careerStateBeforeDrop`, and any Top 40 hit counts at least half.
- **Fixture act.** It asks the player $30. A major now offers $53, an independent $33, and the legacy price was $9,867.

**Related changes:**
- The scouting gate estimate is now a working professional's going rate at the label's tier premium, instead of a flat $800 to $5,000.
- AI artist choice reads the money term against the best offer the act could get, a major's price for it, instead of a flat $12,000. Money still ranks the bidders the same way.
- The **Promotion** capability adds live chart buzz (`RosterManager.LabelBuzz`): 0.35 marketing, 0.25 Top 40 record, 0.25 buzz, 0.15 reputation.
- Against a real rival offer, capability weighs more: `RivalEdgeScale` is 1.0 with bounds of −0.30 to +0.15. Before, it reused the outlook's 0.6 scale with bounds of −0.22 to +0.08.
- Royalty, term and sides are unchanged.

**Flag:** `--disable-historical-advances` restores the legacy prices exactly.

## 2. Rival courtship (`Systems/PlayerDesk.RivalCourtship.cs`)

**The stages.** A rival's interest now goes inquiry → meeting (2-4 days later) → offer (1-2 days later) → decision (5 days later).
- A quarter of inquiries fade ("heard them and passed").
- A courtship fades if the rival can no longer take the act on.
- A handshake makes the rival wait.
- Everything runs on stable hashes plus the recruitment owner's access, roster and cash checks. It draws no global RNG and does nothing without a player label.

**Origins:**
- **The crowding roll.** This now covers every act the player is looking at, including untouched pad acts, which used to sign on the spot.
- **The AI daily market choosing an act the player is pursuing.** `RosterManager.PlayerPursuitInterceptor` turns that pick into a courtship that starts at the meeting stage, instead of an instant signing. "Pursuing" means notebook, pad, a kept talk, or a dossier marked Watch or Pursue.
- **Renewal poaching.** A player act whose contract has matured is approached at `0.015 × heat` per day. The finder is `RosterManager.FindRivalForPlayerRenewal`. The act's side always tells the player, because it's leverage.
  - The renewal must beat the offer: `BeatsRivalOffer` now applies to renewals.
  - An easy renewal that was already open becomes a negotiation.
  - On the decision day, an act that hasn't been renewed is released as `ContractExpired` and signs with the poacher on exactly the offered terms. The departure is recorded as `SignedElsewhere`.

**What the player learns, and from whom.** Each item is stored as a `RivalSighting` on the courtship and as a dated `RivalInterest` notebook observation, with its source.
| Channel | Stages | When the player hears |
|---|---|---|
| The act's side | meeting, offer | The same day, once the player has talked terms or met the act; always for the player's own roster act |
| The room's booker | inquiry | The next time the player is in that room, within 21 days. Derived from the player's own visit evidence, so a reload doesn't change it |
| A musician on the roster with a contact edge to the act | inquiry, meeting, offer | The next day |
| Scene talk | meeting, offer | The next day, only if the player acquired evidence in that act's scene in the last 14 days |

If nobody could know, the offer's disclosure is `Unknown`. It still stands and the act may sign elsewhere, but there is no line, no note and nothing in the notebook. Word that travels overnight is held until its day (`Delivered`).

## 3. Phase 5 behavioural feedback

**Visibility.** Signals are a pure projection over the player's dossier observations: one signal per event, with reach, delay and expiry set by kind. Repeating a cheap action adds nothing.
| Kind | Reach | Delay | Expiry |
|---|---|---|---|
| Listening | .04 | 2 days | 14 days |
| Booker question | .08 | 1 day | 21 days |
| Meeting | .12 | 1 day | 21 days |
| Demo | .30 | 2 days | 30 days |
| Terms on the table | .20 | 2 days | 30 days |

Effects:
- **On rivals.** The daily inquiry chance is multiplied by 1 + 2×visibility.
- **On AI slates.** The act gets a weight of 1 + 0.25×visibility through `SceneRecruitmentService.ActVisibility`. It is combined with scene attention by `max`, never multiplied, so the two can't compound. The weights are cached per clock minute.

**Audience attribution.**
- **Per set.** Each set's `ScenePerformanceSnapshot.Audience` records who was present, who came in for the set, the share that stayed, and requests. It is resolved once when the set ends, whether or not anyone attends, using keyed draws only. The room's own .30 of the crowd is never credited to an act.
- **Room record.** Each room keeps `DrawSets`, `DrawTotal` and `StayedTotal` per act.
- **Booker question.** A fifth question, "Who comes in for them?", is answered from that record and rounded to 5.
- **Notebook.** Draw is shown in bands; the player never sees a head count. The notebook says whether the draw is seen in one room only, several rooms in one town, or across towns.
- **Recognition.** Each set adds a bounded amount to the act's public recognition through the existing owner: 2e-6 per head, capped at 60 heads, so at most 1.2e-4 per set. Diminishing returns are applied there. Flag: `--disable-live-draw-recognition`.

## Save

The envelope stays v13. New optional fields:
- `PlayerSaveData.RivalCourtships`
- `RivalOffer.CourtshipId` and `RivalOffer.Renewal`
- the scene-room standing draw fields
- the set audience on snapshots
- the observation `Set*` and `DrawNights` fields

Appended enum values: `RivalOfferDisclosure.Unknown`, `ScoutingEvidenceKind.RivalInterest`, `ScoutingSourceQuestion.ActDraw`. Courtships are validated before any mutation. Older saves invent nothing.

## Verification

- **Build:** 0 warnings, 0 errors.
- **`--scouting-rival-courtship-check`:** 93 checks (s1001) covering:
  - flags;
  - pricing for unproven, rising, proven and legacy acts;
  - visibility delay, expiry and deduplication;
  - each knowledge channel and its timing;
  - fade;
  - handshake wait;
  - the AI nomination being taken over;
  - an unseen offer;
  - withdrawal;
  - save/load and six malformed saves;
  - renewal poaching, both lost and won;
  - set audience, the booker's draw count, and portability.
  It caught a real save bug: a courtship at the Offer stage was rejected on load.
- **AI-inertness:** a 52-week ChartAuditRunner run with `--disable-historical-advances --disable-live-draw-recognition` (`p5-off-1001-52`) is **byte-identical on 84/84 CSVs** to the Phase 4 reference `co-def-1001-52`.
- **Defaults vs control (`p5-def` vs `p5-off`, s1001, 52 weeks):**
  - 619 vs 617 labels alive.
  - Total AI cash essentially flat by tier (advances were never a large share of spend).
  - First signings 140 vs 135; attempts 168 vs 162; no affordability rejections in either.
  - Year-end genre slots moved 24 in total, inside the single-seed noise floor.
  - About 300 more musicians carry some act recognition; the p90 is flat.
- **Regression:** `sh SimLogs/credible-offers-regression.sh p5` (now including the new check at both seeds) passes all 30 runs with every pre-existing check count unchanged. After the final visibility-cache build, the new check reran at 93 checks on s1001 and s2002.

## Open

- **Uncalibrated first values:** fade share, stage gaps, visibility reaches, the 2× inquiry gain, renewal heat, the scene-talk and booker windows, the tier premiums, and the recognition per head.
- **Decade runs:** the paired-seed decade runs at the end of Phase 6 must cover historical advances and live-draw recognition. Those are the two AI-affecting changes.
- **Rendering:** the notebook and roster-card lines are wired, but they have not been rendered or checked in the UI runner yet.
