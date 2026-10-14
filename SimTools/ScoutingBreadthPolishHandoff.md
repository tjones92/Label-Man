# Scouting Phase 6: breadth and polish

October 11, 2026. Branch `scouting-experience-expansion` (worktree `C:/Project/Label-Man-scouting`), on top of `779a0a9` (Phases 3-5). Phase 6 of `SimTools/ScoutingExperienceExpansionDirective.md`: all-city opening validation, differentiated rooms and contacts, founding history, accessibility and pacing.

## What already existed

Most of Phase 6's list had landed in earlier slices, so this slice does not rebuild it:
- **Founding history and its consequences** (scenario 13): the first signature's durable memory (`ScoutingFoundingHistoryHandoff.md`), plus broken versus fair departures and home-town word of mouth (`ScoutingCredibleOffersHandoff.md` §4).
- **Lost-lead follow-through** (scenario 12's "next path"): `ScoutingLossFollowThroughHandoff.md`.
- **Listening reachability**: every founding city and purse has an affordable posted set (`ScoutingOpeningPacingHandoff.md`).
- **Structural room differences**: capacity scales with 1960 city population, and admission and pay terms vary by kind of room (`SceneLiveEconomics`).

## 1. The opening runway was broken (the main finding)

The opening check only proved a player could *hear* music. The new `--scouting-runway-check` asks whether a player can reach an act that would *discuss a deal*.

**On opening day the answer was no, everywhere.**
- Every unsigned act on any bill, in all 31 towns, was latent: off the market.
- Only 2 acts in the whole world were seeking a deal, and neither was on a bill.
- Each week the population owner activates latent acts at random across the country, never the ones a label is actually courting.

**A 12-week opening confirmed it.** The policy was one set a day, with every unsigned act heard put on watch and an offer made the moment one would talk.
| Town | First willing act | Listens to get there |
|---|---|---|
| Albuquerque | day 36 | 32 |
| Nashville | day 54 | 47 |
| New York | day 63 | 55 |
| Billings | never (84 days) | 59; only 5 acts heard, and the label ended at −$142 |

That is exactly the grind §11 forbids.

**The fix is an ask that goes through the existing owner** (`Systems/PlayerDesk.DirectApproach.cs`).
- **The ask:** a heard act that is off the market gets **ASK IF THEY'D HEAR AN OFFER** on the pad instead of a dead "Discuss terms". It costs no time or money.
- **The answer:** on a keyed draw per label, act and 91-day season, the act either agrees to think it over (0.65) or wants to keep playing locally this season.
  - The decline states the date the player can ask again.
  - A repeat ask in the same season can't reroll.
- **Who activates the act:** never the player. `ArtistManager.DirectApproachProvider` hands the acts that are thinking it over to the weekly activation pass.
  - The list is capped by the label's open roster slots.
  - These activations sit on top of the AI's budget, so no AI activation is displaced.
  - Without a player label the provider is null and the pass is unchanged.
- **What happens next:** the act's side answers at dawn ("they'll hear an offer from you now") within 8 days.
  - Once back on the market the act is an ordinary prospect: rivals can bid, and the pursuit interceptor turns a rival's pick into a courtship.
  - A rival signing them before the answer lapses the approach. So does a full roster at the pass.
- **Notebook:** shows the approach's state and date.
- **Save:** an optional `ScoutingKnowledgeSaveData.Approaches` list. Malformed entries are dropped on load.

**After the fix, the same 12-week policy gives:**
| Town | First willing act | Outcome |
|---|---|---|
| Billings | day 1 | talks opened (Hardball) |
| Albuquerque | day 8 | talks opened (Hardball) |
| Nashville | day 1 | signed |
| New York | day 1 | talks opened (Firm) |

Openings took 4-19 actions and asks ran $25-35 against an $800 purse. All 31 towns × 4 purses now have a reachable act to propose to (`thin=0`).

**Open question (uncalibrated):** the opening may now be too quick. A label can sign on day 1 after one listen. The directive allows a fast, well-judged signing, but the 0.65 consider share and the 8-day answer are first values.

## 2. Rooms you can tell apart (`Systems/SceneRoomCharacter.cs`)

This is player-side only. Every value is a fixed hash of the room id, so it is the same in every world and draws nothing.

**Named bookers.**
- Each of the 186 rooms has a booker with a name, a year they started booking, and a stated remit: "knows this room's nights and acts, not the rest of town; filling the room is the job, so the regulars are the ones they talk up."
- `ContactName` is the name. The new `ContactTitle` ("Ruth Calloway, booker at the Uptown Lantern") carries provenance in source claims, appointment notices, lead notes and rival sightings.

**The room in plain words.** The board shows the kind of room, its size, how it pays, its crowd, what you can hear there, and where you get a word with the players. Raw enum names (`ListeningRoom`) no longer reach the board or the evidence conditions.

**Rooms change what you can hear.**
- `ScenePerformanceService.Interpret` takes the room kind and adds per-axis noise. For example, a club hides words (+.07 material) and vocal detail (+.05), and a listening room's polite crowd hides stage command (+.05).
- This applies to every listener alike, never to the acts. It says nothing about talent or reliability.
- **Notebook:** marks the hidden axis ("Material: …(the words were hard to follow over the club)"). When every room so far hid a question, it names the kind of room that would answer it ("Rooms so far: heard only in clubs. A quieter room (a coffeehouse or listening room) would answer the material question better."). That gives a second visit a reason.

**Fixed:** two mojibake separators (`Â·`, `â€”`) in `PlayerDesk.SceneInformation.cs` lead/tip lines.

## 3. Accessibility and pacing

- **Color:** no scouting surface encodes state by color alone. All status is text.
- **Pacing:** the runway plays above are the pacing measure. They have no compulsory dialogue: one listen and one ask can lead to an offer.

## Verification

- **Build:** 0 warnings, 0 errors.
- **`--scouting-runway-check`:** passes at both seeds (212 checks on s1001, 208 on s2002), with `thin=0`.
  - **Part A:** every city × purse, as a projection.
  - **Part B:** 12-week openings in Billings, Albuquerque, Nashville and New York, from one restored world.
  - **Part C:** the ask itself:
    - an unheard act can't be asked;
    - asking costs nothing and changes no world state;
    - a repeat ask doesn't reroll;
    - the answer survives save/load, and a malformed entry is dropped;
    - the notebook shows the approach;
    - a full roster sends the owner nobody;
    - signing elsewhere lapses the approach.
  - **Outputs:** `SimLogs/scouting-runway-{audit,play}-<seed>.json`.
  - **Options:** `--runway-report-only` skips the strict town check. `--runway-no-direct-asks` reproduces the before-fix opening.
- **Rendered UI:** `--scouting-rooms-ui-check` gives `SCOUTING_ROOMS_UI_PASS`. It checks the room description, the named booker and their remit, the tip button naming the booker, no raw enum names, and the club caveat and room-gap line in the notebook. Screenshots are in `SimLogs/scouting-rooms-ui-v1/` and were inspected at 1600x900.
- **Regression:** `sh SimLogs/credible-offers-regression.sh p6` passes all 32 runs (now including the runway check at both seeds). Every pre-existing check count is unchanged from p5.
- **AI-inertness:** two 52-week s1001 runs are **byte-identical on 84/84 CSVs** to their Phase 5 references.
  - Defaults (`p6-def-1001-52`) against `p5-def-1001-52`.
  - Both AI switches off (`p6-off-1001-52`) against `p5-off-1001-52`.

  Nothing in Phase 6 runs without a player label.

## Open

- **Decade runs:** the user will run the paired-seed decade A/B themselves. It still needs to cover `--disable-historical-advances` and `--disable-live-draw-recognition`, the only AI-affecting switches on this branch.
- **Uncalibrated values:** the direct-approach values (0.65 consider share, 8-day answer, 91-day season) and the room hearing noise.
- **Opening speed:** watch whether the opening is now too quick in human play.
- **Billings:** its home scene is thin. Only 5 acts were heard locally in 12 weeks before the fix, and the circuit drive is the intended remedy. The runway play only listens locally.
