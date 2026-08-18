# Scouting Mechanic Directive

Branch: `scouting-mechanic`. Source: three design docs (`scoutingdirective{a,b,c}.doc`,
authored 2026-08-09) reviewing a "Gemini sketch" against the actual codebase. This file is the
consolidated, code-verified plan.

## The reframe

Scouting is **not** missing — it is already a lazy, budget-throttled, per-region, ability-scaled,
two-phase collision market (`RosterManager.ProcessDailyTalentMarket` → `NominateFromDailySnapshot`
→ collision resolve → `CanCommitDailyOffer`). Do **not** rebuild the chassis; it is
calibration-frozen (deterministic daily market, preserved RNG call order, byte-identical replays).

The one genuine gap: **perception is omniscient**. Everywhere scouting reads quality it reads the
true `artist.CalculateBaseQuality()`. Selection has noise (`SelectAffordableCandidate`), but the
*read* is perfect. The work is to insert a perception (fog) layer, then layer buzz / managers /
contracts on top of the signing that already works.

## Code-verified findings (all confirmed against the tree)

- `ChartManager.ReleaseRecord` already uses the target noise band: `Lerp(0.30, 0.10, scoutingAbility)`
  (ChartManager.cs:696) — but via `GD.RandRange`. The scouting perception layer must be a **pure
  stable hash**, never a random draw, or it desyncs replays.
- Omniscient read sites: `AILabel.EvaluateSigning` (:600), `AILabel.EvaluateFreshPotential` (:625),
  `RosterManager.GetEnabledSupplyCandidates` final sort (:1007).
- `SelectAffordableCandidate` scales selection by `Lerp(.25f, 5f, ability²)` (RosterManager.cs:738).
- `DiscoveryRefreshWindowWeeks = 4`; `GetStableDiscoveryKey` is FNV-1a over `label|artist|window`.
- Live signing paths: daily market (`NominateFromDailySnapshot`, :530/531/542) and monthly
  `TrySignNewArtist` (:783). Frozen launch roster uses a **separate** path
  (`PopulateInitialRoster` → `ScoreArtistForLabel`) — leave it omniscient; fogging it reshapes the
  seeded 1960 industry and breaks calibration.
- Correction to the source docs: their proposed `EvaluateSigning` rewrite invents momentum/reputation
  terms; the real bodies already have them. Preserve the real bodies, swap only the quality read.

## Phased plan (ordered low → high determinism/calibration risk)

### Phase 1 — ScoutingPerception (the fog) — IMPLEMENTED
`Systems/ScoutingPerception.cs`: pure stable-hash `PerceivedQuality(artist, label, window)`, error
inversely correlated to `scoutingAbility` (±0.30 → ±0.10), stable per (label, artist, 4-week window)
so re-evaluation can't launder the fog; the window advances so a scout's reads drift/converge over
time (the "scout deeper" progression, emergent). Threaded via **additive overloads**:
`EvaluateSigning(cands)` / `EvaluateFreshPotential(cands)` stay exactly omniscient (probe/frozen
callers unchanged); new `(cands, int discoveryWindow)` overloads fog when `window >= 0`. Sentinel is
`-1` (not 0 — window 0 is a real early-game window that *should* fog). Live call sites pass the real
window. `GetEnabledSupplyCandidates` slate sort fogged in place. Only latent quality is fogged;
momentum/reputation are the artist's public chart record and stay clear. **Zero new RNG draws → launch
roster byte-identical; only live daily/monthly signing choices change (measured).** Also exposes
`PerceivedRange` for the player-facing progressive reveal.

### Phase 2 — Label buzz + artist-choice reweight — IMPLEMENTED
`CalculateArtistChoiceUtility` (RosterManager.cs:596) weights static tier `reputation` 0.10 and
distribution/reach 0.10 — so a big Major beats a hot indie (the inversion to fix). Add `GetLabelBuzz`
(live chart presence: top-10/top-40 records, recent #1s, `momentumScore`; distribution does NOT
enter buzz; weekly-cached like `artistHeatCache`). Reweight so buzz sits in the reputation slot at
~0.20–0.28 (ambition-scaled) and reach is demoted 0.10 → 0.07 — heat outweighs money ~3.4×
(Motown-over-a-major). No new RNG; collision outcomes change (measured). Reuse the struct's
`reputation` field to carry buzz (avoid telemetry struct churn).

### Phase 3 — Manager stamp + term sheet + affordability gate — IMPLEMENTED (behind `--enable-managers`, default OFF)
Landed with the two checkpoint decisions: **version-flagged** (`Systems/ManagerSystem.cs`, `--enable-managers`)
so managers-off is byte-identical and managers-on accepts the reseed; and the **affordability gate uses
the manager-adjusted advance**. Determinism care taken: `CalculateContractLength`/`SinglesObligation`
draw RNG, so `GenerateTermSheet` is NOT idempotent — the gate uses a pure advance-only helper
(`AILabel.CalculateManagerAdjustedAdvance`) and the full sheet is generated once inside `SignArtist`
in the legacy draw order (year passed explicitly so the singles draw matches byte-for-byte on None).
Original checkpoint note follows.


`ManagerArchetype` enum {None, LocalHustler, Shark, Svengali, Visionary} + immutable `ManagerProfile`
static modifier table. Stamp `artist.manager` at `ArtistManager.GenerateArtist` via
`RollManagerArchetype` (quality-correlated: pros circle high-quality acts) using the stream-aware
`Randf()`. `ContractTermSheet` + `AILabel.GenerateTermSheet` transforms the label's baseline offer
into concrete, player-legible demands (`DemandSummary`). Affordability gate switches from
`CalculateAdvanceOffer` to `GenerateTermSheet(...).Advance` so a Shark's inflated demand really gates
cash-poor labels. **⚠ Adds one RNG draw per artist generation → reseeds the entire 1960 population.
Decide before landing: version-flag the roll, or accept the reseed. Confirm it doesn't starve any
emergent genre's roster access.**

### Phase 4 — Publishing as a settlement claimant — IMPLEMENTED
`labelOwnsPublishing` bit-flag on the artist (default label-favorable; a Visionary deal flips it).
`CompetitorManager.PublishingShareOfGross = 0.11` (a calibration guess — re-derive from measured label
profitability). **Modeled as a REALLOCATION, not a new stream (diverges from the source doc, which
adds to LabelNet and would inject 11%-of-gross into every label and blow up the calibrated economy):**
label-owns (default, and always when managers off) → nothing moves, `PublishingIncome` is informational
inside `LabelNet`; artist-owns → the slice comes off `recordRevenue`/`LabelNet` and accrues to the
artist (`totalRoyaltyEarnings`, not advance-recoupable). Two new settlement columns
(`publishingIncome`, `artistOwnsPublishing`). Verified on a 3yr run: label-owned rows show
pubIncome/gross = 0.11 exactly; artist-owned rows show pubIncome 0 and royalty share ≈ base + 0.11.

### Phase 5 — Passive career auras — IMPLEMENTED (Svengali + Shark; Visionary deferred)
Additive, bounded, `None → +0` (byte-identical when managers off): Svengali `ProductionBonus` lifts
`realizedQuality` in `ChartManager.ReleaseRecord` (→ launch promotion stock); Shark `ChartVisibilityAura`
adds a standing push at `CalculateArtistHeat`'s return (reaches even a not-yet-charting act). Visionary
`PrestigeBonus` NOT wired — it would feed `criticalAcclaim`, a confirmed dead field
([[criticalacclaim-is-a-dead-field]]); deferred until critical reception exists.

### Star canopy — IMPLEMENTED (behind `--seed-star-canopy`, default OFF)
The base roster seeding (`InitialSignArtist`) caps careerState at Established, so the 1960 world had
no Star/Superstar incumbents even though the runtime ladder grows them — a star-less ecosystem at day
one. `Systems/StarCanopy.cs` flag + a deterministic post-pass in `RosterManager.SeedInitialStarCanopy`
promotes the best acts on Major/MidTier labels to **6 Superstars + 24 Stars** (per-label cap to spread
them), each stamped with a coherent hit history (numberOnes/top10/consecutiveHits, high
momentum/reputation) so the ladder holds them and their releases launch big through the fame-gated
stock. No RNG (quality rank + stable id tiebreak); flag-off is byte-identical. Verified on a 3yr run
(seed 1001, managers+canopy on): exit 0, 0 exceptions, 600 labels, economy matches baseline, and the
star tier is live — 1,637 Superstar- and 5,356 Star-launched records (vs an Established ceiling before).

### Validation status
3-year runs (seed 1001, `--enable-managers`) clean: exit 0, zero exceptions, healthy label churn and
economics, publishing reallocation firing (~2.8% of settlement rows artist-owned). STILL OWED: a
measured decade A/B — managers-off vs pre-branch baseline (isolates the always-on fog+buzz P1/P2
impact), then managers-on. The probe-suite path is blocked by a pre-existing D5 "catalog count"
failure (fails on the clean baseline too; `MissingSingletonsTemp.cs` autoload-not-a-Node).

### Deferred (per the docs)
Creative-control *effects* (auto genre-drift, artist-dictated formats), the counter-offer negotiation
minigame, scene-bonus discovery, manager-driven fatigue/lawsuits, and the player-facing scouting UI
surface (its own Godot pass; the sim already produces perceived-quality + confidence + progressive
reveal data).

## Hard determinism flags
1. Perception must stay a pure stable hash — never `GD.RandRange`.
2. Zero-arg Evaluate overloads must stay byte-identical (probes + frozen prewarm).
3. Phase 3's generation-time roll shifts the population RNG schedule — existing replay seeds change.
4. Publishing share directly moves `LabelNet` — interacts with bankruptcy; re-derive, don't trust 0.11.
