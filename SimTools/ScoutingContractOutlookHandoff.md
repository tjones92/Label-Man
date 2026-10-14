# Scouting continuation: acts weigh the label (Phase 4, credible offers)

October 10, 2026. Continues the funded-session-promise snapshot in `C:/Project/Label-Man/tmp/scouting-unsigned`. Primary gameplay sources and other checkouts are unchanged. Changes are uncommitted.

## Why

The acceptance check compared an offer against a reservation built only from the act and its manager. The label never entered it, so a one-person label and a major faced the same threshold, and "being signed" implied capabilities nobody measured. Most acts were one-click Pushovers. This slice makes an act weigh *this* label against its real alternative, so a label nobody has heard of has to negotiate.

## Delivered behavior

**The contract outlook** (`Systems/PlayerDesk.ContractOutlook.cs`) is a pure, deterministic read: no RNG, no world or AI writes, and the same answer on refresh and reload. It is computed when a table opens (or live for an easy signing or renewal) from:

- **What the label can do**, read from its state, never from a checkbox: reach (`effectiveNationalReach`, `ownedReach`), promotion (marketing power, Top 40 record, reputation), recording (how many sessions its cash would fund, and production quality), attention (fewer acts on the roster is better) and locality (the act's own scene).
- **Credibility.** Reach, promotion and recording are claims until the label has something to show for them: records out, records that charted, promises kept (0.35 for a new label, up to 1). Attention and locality are visible to the act and never discounted. Established AI labels read as fully credible.
- **What the act weighs**, from the existing evolution profile: pragmatic acts want reach and promotion; ambitious acts want studio time and attention; acts with modest prospects value someone local.
- **Its outside option.** Staying as it is (how much it works, read from the room ledger: bookings, a standing engagement, shows played). Or the strongest AI label that can actually reach it (`SceneRecruitmentService`), has an open operating roster slot and wants its genre. Or, if none, the proven label it imagines it could wait for, at a discount. Every pull is scaled by the act's prospects (reputation, momentum, work, prior contract). This is a modeled belief, never an offer: no rival offer is created, recorded, named or shown, and nothing writes AI state.
- **The relationship**, bounded: time spent with the act away from the stage (meeting, rehearsal or demo), earlier walk-outs, and at renewal the promises this label kept or broke.

It produces two effects inside the existing acceptance check:

1. **Credit** (standing plus relationship, about -0.30 to +0.14) added to the package value, beside promise credits. Negative when the act has a better real alternative.
2. **Withheld room.** Stake (work, prospects, ambition) times doubt (one minus credibility) takes up to 80% of the give-room the act would normally concede (`DoubtRoomShare` 1.5). A working week adds 0.05.

**Posture is temperament plus stakes.** An act that would be a Pushover by temperament still talks terms when its standing is negative, it is working most of the week, or its doubt is 0.12 or more. It becomes Hardball at standing -0.15 or doubt 0.45. The outlook never makes an act easier than its temperament.

**Easy acts' floors move**: by the credit, within ±0.10 of the 40%/70% floor. The easy renewal, which previously accepted any terms, now has the same floor and names it in a counter (unfogged; you know this act).

**The label objection.** When terms would clear the act's ordinary reservation and only the outlook stands in the way, or the outlook outweighs the worst single term, the act objects about the label: reach ("who gets the record into shops past this town"), promotion, recording, attention or locality. It never shows a number. Money below the act's minimum advance is still objected to as money.

**What they're weighing** is shown on every contract page: lines built only from facts the game holds (rooms booked, standing engagement, a belief about wider-distribution labels that says "No offer has been made that you know of", what they keep asking about, "They've never heard of your label"). Beside it, the player's own label in plain facts: acts, releases, Top 40, distribution, months of overhead in the bank.

**The promotion promise** shares the session promise's commitment record (`ContractCommitmentKind.FundedPromotion`): at least a half-page trade ad ($250, the era rate card) for the act's first record, within 8 weeks of its release, and the record itself out within 39 weeks of signing. It is free to propose, scored at 0.02–0.08 weighted to pragmatism (the session promise's range, unchanged), included in affordability, kept only by a qualifying `BuyTradeAd` on the act's record, and recorded once as missed ("never pushed the record" or "never put out the record"). The renewal premium names it. Its clock starts at the first release found at dawn. The roster card lists every open promise.

## Measured opening (seed 1001, new Nashville label, week 0)

Of 241 local unsigned acts, one-click signings fell from 154 to 114. The acts that now negotiate are the working and the ambitious ones. No AI label has an open operating slot at week 0, so no act has a real rival at the start and the outlook invents none. All 241 Nashville acts are `Latent` at week 0, and none is signable until the population owner activates them. That is unchanged and outside this slice.

## Ownership and compatibility

- The negotiation owner reads `outlookCredit`, `outlookLift`, `capabilityGap` and `capabilityObjection` from `ContractTalk`. All are copied by `DetachedTalk` and validated on restore (finite, bounded, defined enums); a malformed outlook is rejected before mutation.
- Older talks without an outlook restore with zero credit and lift, so they keep the old arithmetic.
- `ContractCommitment.ReleaseDay` is optional. Save envelope stays v13.
- `--disable-contract-outlook` restores temperament-only posture and no credit. It is independent of the scene flags, which the outlook degrades around.
- The player's label has no scene geography, so its locality compares its home town with the act's base scene.

## Verification

**Build:** zero warnings and zero errors.

**New check:** `--scouting-contract-outlook-check` passes 63 checks at seeds 1001 and 2002. It covers:

- **The outlook itself:**
  - a pure, deterministic read: no RNG, world, player, time or cash change;
  - the credibility discount, applied only to claims;
  - stake and doubt raising posture, with the act stating its reason and showing no numbers;
  - proof of work removing the doubt.
- **The rival belief:**
  - full AI rosters at week 0 giving no rival;
  - a fixture-opened slot producing a real reachable rival that is stated as a belief, never named, and causes no AI or world writes.
- **At the table:**
  - the talk carrying the outlook;
  - a label objection on terms found by search, costing patience and showing no number.
- **The promotion promise:**
  - free to propose and withdraw;
  - affordability checked before time or patience;
  - recorded beside a session promise;
  - the release starting its clock;
  - a quarter-page ad not counting and a half-page ad keeping it;
  - a kept promise raising credibility;
  - a miss with no record recorded once;
  - the renewal ask and the act both remembering it.
- **Easy-act floors** moving both ways.
- **Saves and flags:**
  - a compressed save/load round trip;
  - five malformed saves rejected before mutation;
  - an older talk without an outlook restoring;
  - `--disable-contract-outlook` restoring temperament-only posture.

**Rendered check:** `--scouting-contract-outlook-ui-check` presses the real Promise a Push button and confirms:

- the "what they're weighing" block and the own-label line;
- the label objection and its hint;
- that proposing is free;
- that no hidden value renders;
- that the roster card shows the promotion promise.

Screenshots `SimLogs/scouting-contract-outlook-ui-v1/{weighing,objection,tabling,roster}.png` were inspected at 1600 by 900.

**Regression:**

- Passing headless: contract outlook (both seeds), session promise 65 (both seeds), contract continuity 50 (both seeds), offer planning 27, business 34, experience 131, terms 42, meeting 43, appointment 59, opening 544, commitment planning 19, founding history 40, audience 37, comparison 32, market history 9, listener 40, hired listener 43, scene rooms 83, scene information 43.
- Passing rendered: contract outlook, session promise, contract continuity, offer planning, terms, meeting, appointment, business, founding history, commitment planning, opening.

**Fixture changes, each an intended behavior change:**

- The session-promise search now counts the outlook credit, which is part of every package.
- The meeting UI check accepts the negotiation page as well as the one-click form, because the act it meets is now working and talks terms. It passes unchanged with `--disable-contract-outlook`.

**Gaps in the record:**

- After the full headless run, only fixture code and one display string (the overhead-runway wording) changed. The outlook checks and the rendered check were re-run on the final build.
- Markers and hashes are in `SimLogs/contract-outlook-validation.json`.
- This is deterministic validation, not a human playthrough or a long-run economic acceptance. AI behavior is not read or changed.

```text
dotnet build "Label Man.csproj" --no-restore --nologo -v minimal
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-contract-outlook-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=2002 --weeks=0 --scouting-contract-outlook-check
--path . --resolution 1600x900 SimTools/SceneRoomUiRunner.tscn -- --seed=1001 --scouting-contract-outlook-ui-check
--headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --scouting-contract-outlook-survey=nashville
```

## Portable delta

`SimTools/ScoutingContractOutlook.patch` is a fifteen-file delta (twelve changed, three new) against the session-promise snapshot. `git apply --check` passes on a reconstructed baseline, and applying it reproduces every file byte for byte, line endings included. The baselines are in `SimLogs/contract-outlook-baseline`.

## Open design questions

All constants are first values, not calibrated:

- the credibility ladder;
- `DoubtRoomShare` 1.5 and the 80% cap;
- the posture thresholds (doubt 0.12/0.45, standing -0.03/-0.15, work 0.45);
- the standing scale and clamps;
- the proven-label picture and its 0.5 waiting discount;
- the relationship bounds.

The session and promotion credits are flat, as agreed. Making them grow with the gap they close is the obvious next calibration.

Remaining work:

- **Prewarming live history.** Every act starts with an empty room ledger, so the outlook sees almost no work at week 0. A pre-roll of the existing room owner is recommended as a separate, validated slice.
- Founding-act consequences of a broken promise.
- Human playthroughs and main-branch integration.
