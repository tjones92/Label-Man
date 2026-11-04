# Handoff: stature for AI readers (celebrity Phase 5, its own branch)

Written 2026-11-04 at the end of `media-atmosphere`. This is a starting brief for a session that will sketch a broader
directive. It is not a directive itself. Source design: `CelebrityFanbaseDesign.md` §6 and the Phase 5 row of §10.

## The idea in one paragraph

AI labels decide how much to back an act from its career-ladder rung (`careerState`: Rising, Established, Star,
Superstar...). The rung is earned only through chart milestones. Historically, labels paid for *draw*, and draw
included TV fame, fan followings and adult recognition (Motown rationed promotion by a visible stature order; Majors
bought proven stars from smaller labels). Phase 5 adds an observed **stature** score in [0,1] that the AI's money
decisions read in place of the rung. The rung stays as the act's label in the UI and career story. In short: more
naturalistic AI logic for celebrity promotion.

```text
stature = max(chart form (last two years' peak units, as the ladder uses now),
              advance orders / follower size relative to the national top,
              recognition × 0.7)
```

Taking the max lets each route count on its own: the chart act, the TV-made idol with a following, the adult LP star.
Stature maps back onto the existing tables, so an act whose stature matches its rung gets exactly today's money. Only acts
the ladder undervalues move.

## Where the rung is read today (the candidate readers)

Line numbers are as of `6a86c05`. Check each one before relying on it.

| Reader | Where | What the rung sets |
|---|---|---|
| Album forecast | `CompetitorManager.GetAlbumPriorCareerMultiplier` (~3436), used at ~3377 | Superstar 2.5×, Star 2×, Established 1.5× on the LP prior |
| Format-memory band | `CompetitorManager.GetCareerBandIndex` (~3463) | Which career band's release memory a decision reads |
| Launch promotion | `CompetitorManager.ApplyPromotionSnapshot` (~4226), `ApplyReleasePromotion` (~4349) | Career multiplier on the launch campaign (2.5 / 2.0 / 1.5) |
| Contract price / leverage | `ContractPricing.cs` (~80) | Royalty share and leverage by rung (uses max of current and before-drop) |
| Renewal / drop protection | `RosterManager.ShouldResignArtist` (~1444) | Star and above are always re-signed |
| Release cadence | `--hit-act-cadence` / `--hit-act-retention` (`HitActCadence`), plus the slate's follow-up scheduling | Who gets the next single slot |

The inputs already exist on this branch: following (`MediaFollowing`, `--observe-following`), follower demand and
advance orders (`--follower-demand`), and recognition with media writers (`--media-recognition`, `--fame-registration`).

## Already done (don't redo)

- **Scheduling slice:** `--programme-follow-up-scheduling`, `-delivery` and `-material` give established acts timelier
  follow-ups inside the release programme, preserving LP plans (`CelebrityFanbasePhase5SchedulingNotes.md`). These are
  in the combined flag file. They're a scheduling screen, not the stature readers.
- **Phase 4 diagnosis:** raising fan singles demand improves repeat share but not leading acts' entry share. The
  blanket hit-act cadence raises concentration but swaps LPs for singles. Neither substitutes for stature.

## Open questions for the directive sketch

1. **Which readers first?** The design says budgets and leverage. Promotion multipliers are the most sensitive
   (memory note big5-entry-edge-is-campaign-impact: the Big-5 edge is campaign impact). Consider shadow-logging stature beside
   the rung for every reader before switching any of them.
2. **Normalisation.** "Relative to the national top" needs a stable reference (rolling top-N follower size?), so stature
   doesn't inflate as following grows over the decade.
3. **Bidding on stature.** Should Majors buy stars from smaller labels on stature? It's the plausible route to the
   1968-69 consolidation goal (memory note major-share-late-decade-consolidation-goal) and also the riskiest economic change.
   It probably needs its own sub-phase.
4. **Player-facing.** Does the player see stature (for example in the manager's report), or only its effects?
5. **Mapping.** Fit the stature → table mapping so the control's money at each rung is unchanged, then verify that on a
   decade with stature computed but not read (a byte-identical shadow).

## Validation (from the design)

This is the last phase and gets its own decade A/B. Measures: budgets, contract terms, Major share, closures, repeat
hits, entry concentration, and the preserved LP programme. Judge against history, per the 2026-11-01 §0.4 decision, not
"no change". Start from the merged `media-atmosphere` branch with `SimTools/writing_combined_integration_flags.txt` as the
base configuration.
