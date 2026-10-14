# Scouting continuation: credible offers (rest of Phase 4) and room prewarm on

October 11, 2026. Continues the working-week snapshot in `C:/Project/Label-Man/tmp/scouting-unsigned`. Changes are uncommitted. Primary gameplay sources and other checkouts are unchanged.

## What it does

### 1. Real rival offers with decision days (`Systems/PlayerDesk.RivalOffers.cs`)

The crowding roll used to give a notebook act a three-day "circling" warning, then sign it to the rival. Now the rival makes an **offer**:

- **The terms are real.** They use the rival's own pure pricing (`CalculateManagerAdjustedAdvance`, royalty with the manager's demand), with the term and sides from a stable hash. On the day, the act signs with **that label only, on exactly those terms**, through `RosterManager.SignRivalOfferToPlayerAct`. No other label stands in, and no global RNG is drawn.
- **Who gets offers.** Notebook acts, and acts you're in a terms discussion with (on the pad or off it). Untouched pad acts keep the old immediate signing.
- **The decision day** is 5 days out (`RivalOfferDecisionDays`). At dawn on that day:
  - **Accepted:** the act signs with the rival.
  - **Withdrawn:** the rival can no longer reach, roster or afford the act; the act stays unsigned.
  - **Lapsed:** the act has gone, retired or signed elsewhere.
  - **Declined:** you signed the act first.
- **What you know.**
  - **Rumored:** before any talk you hear who and when, not the terms.
  - **Disclosed:** once you're in talks or have met them, the act's side tells you the terms. Opening talks discloses them.
  - The notebook keeps the history of every offer and how it ended.
- **At the table:**
  - An act holding an offer always talks terms. An easy act already in a one-click discussion is moved to a negotiation.
  - The outlook states the offer by name and drops the "a label with wider distribution might…" belief.
  - Signing needs **both** checks: the act's own reservation, as before, **and** beating the rival's paper. Beating it counts package value, your promises, your relationship, and what each label can do (`RivalOfferEdge`, the outlook's capability reading against the offering label).
  - If only the rival check fails, the objection names the rival's paper and costs a round.
- **Money counts in full.** Both papers are measured against the larger of the act's ask and the rival's advance. The first version measured them against the act's own ask, where the advance term caps at 1.5×. So $45 "beat" a $9,870 offer from a major. The fixture now asserts this can't happen.
- **A handshake can't hold an act past another label's offer.** It still works before an offer arrives, and a hold still blocks new offers.
- **Flag:** `--disable-rival-offers` restores the circling warning. An older save's circling warning resolves the old way.

### 2. Distribution promise

A new `ContractCommitmentKind.Distribution` uses the same commitment record as the session and promotion promises:

- **The promise:** a wholesale house carries their first record within 8 weeks of its release. The record itself must be out within 39 weeks of signing.
- **Kept** at dawn, when a placed line or a distribution deal covering the record exists. It is enforced by state, not by an action.
- **Free to propose** (no cash quote). It can only be promised by a label with **no** line yet: a line the act can already see isn't a promise.
- **Scored** at 0.02–0.08, weighted to pragmatism and prospects.
- **Missed:** recorded once in career history. The renewal ask's 25% premium names it.
- **UI:** add/drop buttons on the form, a PROMISE DISTRIBUTION counter, and a roster-card line.

### 3. Relationship credit (`Systems/PlayerDesk.Reputation.cs`, read by the outlook)

All of these are bounded, dated and causal:

- **Introduction:** one of their members has a contact-network edge to a member on your roster. Worth +0.015; a fallout is −0.01. An act you broke an unanswered promise to vouches for nobody.
- **Earlier dealings:** promises kept or broken to *this* act now count on a fresh signing too, not only at renewal. An act that left on fair terms says so (+0.01).
- **Word of mouth:** kept (+0.01) or unanswered broken (−0.02) promises to acts their people know, through the contact network including former members. Stories are only told for 3 years (`WordOfMouthDays`). The total is clamped to [−0.05, +0.02] inside the existing relationship bounds.

### 4. The founding act

- **A broken promise to the first name on the label is known across its home town.** Every act based in that scene hears it: −0.03, stated as "Everybody around Nashville knows…". A kept promise there is +0.015.
- **Departures are recorded at dawn** (`RosterDeparture`), with the kind (talks broke down, disbanded, retired, signed elsewhere) and any broken promises still unanswered.
  - Leaving over an unanswered broken promise brings the story back up from the departure day.
  - A fair departure carries **no** penalty.
- **The founding history** now says what you kept, what you broke, and how they left.

### 5. Room prewarm on by default

`RoomPrewarm = Rooms && !disablePrewarm`. The prewarm fixture now runs with `--disable-scene-room-prewarm` as its control world and asserts default-on.

## AI-inertness

A 52-week ChartAuditRunner run of this build's defaults (prewarm on, rival offers on), `co-def-1001-52`, is **byte-identical on all 84 CSVs** to `ww-on-1001-52`, the previous build's defaults. Nothing new runs without a player label.

## Save

The save envelope stays v13. These fields are optional:
- `PlayerSaveData.RivalOffers`
- `PlayerSaveData.RosterDepartures`
- `ContractTalk.distributionPromised`

Enums are appended at the end, because they serialize as ints. Validation rejects malformed offers and departures before any mutation: a wrong label, two pending offers for one act, a decision day before the offer, disclosed-without-day, non-finite terms, or undefined enums. Older saves invent no offers and no departures. The roster watch restarts from the restored roster.

## Verification

- **Build:** 0 warnings, 0 errors.
- **`--scouting-credible-offers-check`:** 79 checks (s1001) and 78 (s2002). One fallout check needs a two-member act. The coverage:
  - flags;
  - offer purity (no RNG, no world write) and determinism;
  - rumored vs disclosed;
  - handshake refused;
  - an act with paper in hand talks terms, and the outlook states the offer;
  - losing and winning packages found by search, with the objection naming the rival;
  - money far above the ask still counts;
  - the decision day signs on the exact terms with no RNG, and the player's table is closed after;
  - withdrawn and lapsed outcomes;
  - save/load, six malformed saves, and a legacy save;
  - distribution promise: free, scored, refused with a line, signed, release clock, kept by a line, missed once, renewal premium;
  - word of mouth: the hometown story kept and broken, connected vs unconnected acts, the 3-year window;
  - founding departure, broken vs fair, and its save/load;
  - re-signing a former act;
  - introduction and fallout.
- **Rendered `--scouting-credible-offers-ui-check`:** passes. Screenshots `SimLogs/scouting-credible-offers-ui-v1/{offer,tabling,objection,notebook,roster}.png`; offer and objection inspected at 1600×900.
- **Regression:** `sh SimLogs/credible-offers-regression.sh co` passes all 28 runs, with every pre-existing check count unchanged:
  - credible offers ×2, prewarm 30, rotation ×2, ecosystem 51;
  - outlook 63 ×2, session promise 65 ×2, continuity 50 ×2;
  - offer planning 27, business 34, experience 131, terms 42, meeting 43, appointment 59, opening 544;
  - commitment planning 19, founding history 40, audience 37, comparison 32, market history 9;
  - listener 40, hired listener 43, scene room 83, scene information 43.
- **Records:** markers and hashes are in `SimLogs/credible-offers-validation.json`.
- **Not done:** no human playthrough and no decade run (deferred to the end of the directive).

```text
dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-credible-offers-check
--path . --resolution 1600x900 SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-credible-offers-ui-check
--headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --seed=1001 --run=co-def-1001-52
sh SimLogs/credible-offers-regression.sh co
```

## Portable delta

`SimTools/ScoutingCredibleOffers.patch` covers 17 files: 14 changed, 3 new. It is generated outside the repo against `SimLogs/credible-offers-baseline/*.orig`. `git -c core.autocrlf=false apply` reproduces every file byte for byte. Line endings are mixed per file, and each file kept its own.

## Open

- **Scale mismatch:** AI advances are tier-priced, so a major offers about $10k to an act whose venue ask is $30–40. A cash-poor label mostly can't match the money and wins on gives (publishing, control, term) and fit. Ambitious acts weigh those heavily enough that a near-zero-money package with every give can beat a major. Revisit with the weights, or price AI offers by venue, at calibration.
- **Uncalibrated first values:**
  - the 5-day decision window;
  - the distribution promise range;
  - the word-of-mouth amounts and 3-year window;
  - the introduction and fallout credits.
- **Not modeled:**
  - Rivals poaching acts at renewal: only unsigned acts get offers.
  - Offers on untouched pad acts: those still sign immediately, as before.
- **Duplication on the tabling form:** it repeats the offer line already in "What they're weighing". It could be trimmed.
