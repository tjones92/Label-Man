# Celebrity / economy audit — remediation progress

Working log for [CelebrityEconomyAudit-2026-10-15.md](CelebrityEconomyAudit-2026-10-15.md). Each fix gets a
short (52–104 week) catastrophe check when it lands; **one two-seed decade A/B runs at the very end**
(decade runs take ~2.5 h). Calibration-bearing changes are listed in the watch list below so that
final run knows what to look at.

Base: `845cc49` (reference control `SimLogs/ref-845cc49-1001-104`).

## Step 1 — cash identities and discovery caps

| Audit § | Change | Short check | Status |
|---|---|---|---|
| 5 | `ApplyBreakoutDiscovery` regional deposits add into headroom only (`AddWithinHeadroom`); a deposit never lowers awareness, radio, jukebox or word-of-mouth earned elsewhere. Gain telemetry records the applied delta. | `headroom-1001-104` vs ref: no catastrophe | Done |
| 5 | The national 0.60 clamp is **kept**, renamed `NationalSingleAwarenessCeiling`. It is not a discovery cap: it trims every single's national awareness weekly and the economy was calibrated under it. Removing it is a separate, measured decision. | n/a (behavior preserved) | Deferred |
| 13 | Player returns reserve is withheld **out of** the collectable receivable (receivable + reserve = collectable), so receipts + losses = billed. Player-only; AI path byte-identical. | Not exercised by AI-only audit runs | Done |
| 14 | AI labels expense only `royaltyToArtist`; the recouped slice is no longer destroyed. Seeded 1960 advance balances are kept (pre-1960 advances; `HasRecoupedCurrentContract` reads them). | `recoup-1001-104` vs `headroom-1001-104`: no catastrophe (closures 169 vs 166, tier transitions 44 vs 45) | Done |

## Decade watch list (end-of-audit A/B)

- **Major-family chart share / concentration.** Short run, 1961: Major-family share 47.5% → 56.7%, C4 0.150 →
  0.206 with the headroom fix. Plausible mechanism: big Major launches start above the regional discovery
  caps and the old code clipped them back weekly — an incidental leveler. Single seed, two years; may invert.
- **AI label survival, roster growth, tier distribution** after the recoupment fix (§14): the AI economy
  was tuned with the double-pay in place. Majors carry the largest advances and recover the most.
  `--calibration` suppresses `label-finance.csv`, so the final A/B needs one arm per seed with finance
  telemetry on (or a finance-only override) to compare cash by tier.
- Major-family share 1961 read 47.5 / 56.7 / 51.8% across control / headroom / headroom+recoup — the
  spread is single-seed noise of the order of the effect, which is why this waits for the decade.
