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

## Step 2 — coherent January 1960 careers

Design: [OpeningCareersDesign.md](OpeningCareersDesign.md). `OpeningCareerService` (switch `OpeningCareers`, scene
default on, `--no-opening-careers` control) replaces the quality-ranked canopy: 75 launch acts in six archetypes get a
keyed pre-1960 `publicHistory`; counters, ages, momentum/reputation, recognition (replayed through the live credits and
decay) and members' fame are derived from it. Teen idols start with media attachment; the artist file lists the
earlier records.

First-week world, seed 1001 (`SimLogs/worlds/opening2-1001-first-week`):

| Archetype | n | Rungs | Lead age | Formed | Median recognition (max) | Standing | Momentum |
|---|---:|---|---|---|---|---:|---:|
| Adult standard-bearer | 8 | 3 Superstar, 5 Star | 40–48 | 1936–49 | 0.541 (0.729) | 0.283 | 0.39 |
| Current hitmaker | 10 | 3 Superstar, 7 Star | 20–25 | 1955–58 | 0.493 (0.646) | 0.018 | 0.85 |
| Established hitmaker | 12 | Star | 25–40 | 1948–56 | 0.442 (0.518) | 0.060 | 0.42 |
| Former star | 10 | 9 Declining, 1 Established | 40–52 | 1935–45 | 0.398 (0.434) | 0.106 | 0.10 |
| Critics' favourite | 15 | Established | 25–50 | 1945–57 | 0.141 (0.331) | 0.195 | 0.17 |
| Regional name | 20 | Established | 27–45 | 1945–56 | 0.068 (0.156) | 0.034 | 0.17 |

No act has more chart entries than releases (all 30 canopy acts did). Registry: 4 acts above 0.60 (was 0), 30 above 0.40.

Short check `opening-1001-104` vs `recoup-1001-104`: no catastrophe (closures 161 vs 169, bankruptcies 55 vs 55, tier
transitions 44 vs 44, units and chart entries within noise). That run predates the regional-name tail fix, which only
lowers regional names' recognition.

Not verified: `--no-opening-careers` byte-identity against the canopy path, and a player save/load of `publicHistory`
(audit-world saves carry it).

## Step 3 — regional audience causality

Design: [RegionalAudienceDesign.md](RegionalAudienceDesign.md) (approved 2026-10-15: λ 0.35, μ 0.30, target band as
written, 3d last and optional). Order 3a → 3f → 3b → 3c → 3d → 3e. Metric: `SimTools/analyze_regional_sync.py <prefix>`
(needs a full-telemetry run, not `--calibration`). Control for this step: `SimLogs/ref-781a445-1001-104` (full telemetry).

Baseline (audit seed-1001 13-week run): demand level 98.3% common, weekly change 99.3% common (conversion 72.9%,
awareness 26.4%), awareness floors bind on 1.5% of rows, top-11 overlap 85.1%. Mid-decade 50k+ records: regional
peak-week spread median 1 week, 1.3% ≥ 8 weeks.

| Sub-step | Change | Short check | Status |
|---|---|---|---|
| 3a | `RegionalRecordData.localIntroAge / localPeakUnits / localPeakAge` (record ages; intro = awareness ≥ 0.08 or units ≥ 150 × regionScale), written weekly for singles before the breakout pass; nothing reads them. Saved (old saves: a region that has sold is in play from age 1). | `local3a-1001-104` vs `ref-781a445-1001-104`: all 84 CSVs byte-identical (inert, as designed) | Done |

Control sync baseline, `ref-781a445-1001-104` (104 weeks, 280,613 record-weeks): level 98.9% common, weekly change
99.6% common (conversion 74.6%, awareness 25.0%), peak-week spread median 1 week (20.3% same week, 9.6% ≥ 4, 0.2% ≥ 8;
1,461 records over 50k units), top-11 overlap 88.3%. Every later sub-step is scored against these numbers.

Not verified: player save/load round-trip of the three new fields (`SaveLoadRoundTripRunner` not run; nothing reads
them yet, so 3b's check is where that matters).

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
- **Opening careers (step 2).** Short run: Major-family share +4.9 / +4.9 pts (1960/61), C4 +3.4 / +3.4 vs the recoup
  run. Expected direction (the seeded stars sit on Majors and now launch with recognition 0.4-0.7 instead of ~0), but
  inside the single-seed spread. Also watch: veteran members (40-55) reaching band-life age exits; former stars
  (Declining) being dropped early; Folk holds 4 of 12 established-hitmaker Stars.
