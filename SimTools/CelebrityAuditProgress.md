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

| 3f | Media fan sales evidence (observe-only): `SalesStandingWeek` ranks each act's smoothed share of a region's single sales (all its singles summed; rises at once, 3-week half-life down); top-seller latch enters at rank 9, leaves outside rank 17. `FanWeek.SellingRegions` → `TopSellerRegions`. Switch `MediaFans.SoftSalesStanding` (default on), control `--media-hard-sales-rank`. State in `MediaSaveData.SalesStanding / TopSellerIn`. | `local3f-1001-104` and `local3f-hard-1001-104` vs `ref-781a445-1001-104`: both 84/84 economy CSVs byte-identical. Media checks 125/125 (3 new). | Done |

| 3b | `RegionalAudience.LocalLifecycleClock` (new `Systems/RegionalAudience.cs`; scene default on, `--no-local-lifecycle-clock` control): in live demand the post-week-8 `DEMAND_AGE_DECAY_RATE` term reads the region's `LocalAge`; an unreached region is not aged. Release ramp unchanged. | `local3b-1001-104` vs `ref-781a445-1001-104`: no catastrophe (table below) | Done |

| 3c | `RegionalAudience.LocalRadioFatigue` (scene default on, `--no-local-radio-fatigue`): live weeks only. Station drops use the region's own support (`units / localPeakUnits`) and weeks since its local peak; the regional rotation target uses `Lerp(radioHeat, localHeat, 0.35)`, where `localHeat` = the record-wide radio target + the position bonus on the region's own sales rank (local units restated at national scale for the 15k gate) × fatigue from the local peak. `UpdateRadioHeat` refactored into `GetRadioPositionBonus` / `GetRadioFatigue` (exact). New `localSalesRank`, `radioHeatBaseTargetThisWeek` (week-local). | `local3c-off-1001-104` vs `local3b-1001-104`: 84/84 byte-identical (refactor exact). `local3c-1001-104`: no catastrophe; no convexity renormalization needed | Done, **no sync effect** |

3c result (vs control / 3b): single units 153.2M / 154.2M (control 152.0 / 154.4); airplay share of top-40 points
15.8% / 17.3% (control 16.2 / 16.5, inside ±3 pts); #1 records 24 / 24 (3b's 32 did not recur, so it was likely
noise); Major-family share 52.8% / 58.1%; closures / bankruptcies 167 / 57 (control 154 / 45; step-1/2 runs ranged
161–169 / 55, so inside the single-seed spread); media all-7 → 0: 7.

Sync is unchanged from 3b: local share of weekly change 1.16% (3b 1.15%, control 0.39%), local share of awareness
change 0.45% (3b 0.44%), peak-week spread median 1 week with 9.9% ≥ 4 and 0.41% ≥ 8, top-11 overlap 87.0%.

**Why 3b and 3c cannot move it alone.** Both make each region's clocks and radio read that region's own state, and
that state is now local. But every input to it is common: the record-wide radio target, quality, label scale,
national launch, and genre acceptance, which differs by region but is fixed per genre. Regions therefore reach the
same local ranks and peak in the same week, and the local feedback copies the national result. A region-local
mechanism only diverges if something local seeds the difference. In the current model only two things do: label
distribution coverage at launch and the 0.13-weight reporter panel. See the design's "3g" section.

3b result (seed 1001, 104 weeks; `SimTools/analyze_regional_guards.py`, `analyze_regional_sync.py`):

| | Control | 3b |
|---|---:|---:|
| Single units 1960 / 1961 | 152.0M / 154.4M | 151.7M / 154.1M |
| Airplay share of top-40 points 1960 / 1961 | 16.2% / 16.5% | 17.0% / 18.8% |
| Major-family chart share 1960 / 1961 | 57.9% / 56.0% | 54.7% / 59.5% |
| #1 records 1960 / 1961 | 24 / 23 | 23 / **32** |
| Units at #1 / #10 / #100, 1961 (median week) | 45.3k / 31.5k / 3.7k | 51.0k / 37.0k / 3.7k |
| Genre chart-vs-market gap 1960 / 1961 | 0.316 / 0.374 | 0.341 / 0.402 |
| Closures / bankruptcies | 154 / 45 | 142 / 36 |
| Local share of weekly demand change | 0.4% | 1.2% |
| Local share of conversion change | 0.01% | 0.7% |
| Peak-week spread median / ≥4 wk / ≥8 wk | 1 / 9.6% / 0.21% | 1 / 10.7% / 0.35% |
| Top-11 overlap | 88.3% | 87.9% |
| Media all-7 → 0 in one week | 6 (3f arm) | 11 |

The clock works but has little to act on: launch gives covered regions ≥ ~0.55× national awareness, and regional
radio (lerping to national `radioHeat`, +0.12×radioPlay awareness a week) lifts the rest over the 0.08 introduction
line within a week or two, so almost every region's clock starts in weeks 1–3. The arrival is still national. 3b
should show its effect once 3c makes regional radio local; scoring it alone against the timing targets is not
meaningful. With 3b on by default, `local3b-1001-104` is the control for 3c. Inference from the launch and radio code, not measured: `localIntroAge` is not exported to telemetry.

3f result (seed 1001, 104 weeks, from `MEDIA_FANS_TOPSELLERS` diagnostics; script `scratchpad/topsellers.py` → fold
into SimTools if reused):

| Arm | All 7 → 0 in one week | Drops of ≥4 regions | First entries | National episodes |
|---|---:|---:|---:|---:|
| Hard rank (old) | 58 (median act still selling 23.7k units; one at #8) | 164 | 253 | 0 |
| Soft standing | 6 (all at #31–77, 5–11k units) | 103 | 170 | 1 sensation → cooldown |

Drops of 4+ regions remain common because the regions' sales still fall together: that is the demand-side
synchronization 3b/3c target, not the media rule. Thresholds were set on rank, not share, because shares drift
with record count (1960-61 median share: #1 2.5%, #9 1.2%, #17 0.74%).

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
- **Local lifecycle clock (3b).** 1961 had 32 distinct #1 records vs 23 in the control (52 weeks either way), with
  units at #1 and #10 up ~12-17%; 1960 was unchanged (23 vs 24). Single seed, one year: may be noise, but more #1s
  means shorter tenure, which works against the #1-returns history (memory: tenure is returns, not run length).
