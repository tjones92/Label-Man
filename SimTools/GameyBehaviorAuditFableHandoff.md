# Game-y behaviour audit: handoff to Fable (2026-11-12)

## Goal

Audit the **economy, fanbase and label** systems for game-y behaviour: mechanics that produce outcomes because of how the code is shaped rather than because of how the 1960s record business worked. The aim is better historical accuracy for the model as a whole, not a single metric.

This is an audit first. Report findings ranked by how much they distort history; screen fixes only for the top few, each behind its own flag.

## Scale caveat

The game's population (artists, labels, releases) is **slightly smaller than history**. So:

- Compare **shares, rates and ratios** (Top 10 share by tier, singles per act, closure rate, sell-through, units per charting single), not raw counts.
- When an absolute does matter (weekly units of a #1, label gross), allow for the smaller field before calling it wrong. Say which scale you assumed.
- A count that is far *above* history despite the smaller field is a strong signal.

## What counts as game-y

Look for these patterns:

1. **Ratchets with no decay.** Accumulators that only go up (example: `SimulatedArtist.momentum` has no calendar decay).
2. **Free money or free stock.** Cash, credit or inventory that is never repaid or never costs anything (examples: P&D advances only recoup from the distributor's skim; follower advance stock is free and ~84% unsold).
3. **Fictional credit.** A mechanic that is booked or budgeted for but barely realizes (example: promo-single album credit realizes 1–5%).
4. **Flat rations and fixed purses.** Demand that is split proportionally among however many entrants, so volume taxes hits instead of adding units.
5. **Cliffs and gates.** Hard thresholds where history was gradual, or gates that are unreachable / always passed (example: first career-ladder rung reached by 0.51% of Rising acts).
6. **Accounting timing artefacts.** Bankruptcies or windfalls caused by when money is booked, not by whether the business was sound (example: tax paid on uncollected receivables).
7. **Dead or cosmetic fields.** State that is written and displayed but never feeds behaviour, or feeds it in only one direction.
8. **Self-reinforcing loops.** Positive feedback with no cost, saturation or competitor response (example: earned roster capacity, where idle acts cheapen each other).
9. **Tier-blind or region-blind rules** that history would not support, or tier advantages that come from code paths rather than from scale, money or access.

## Already known (don't re-find; check whether still true)

The memory index (`MEMORY.md`) lists these with detail. Treat them as starting points, and re-verify any you lean on, since several were fixed later.

- **Fixed:** hop-table bottomless well; recoupment double-pay; small labels never losing money (`--dealer-price-pressing`); LP gate double credit.
- **Open:** P&D advances never repaid; follower singles clear at ~10% with free advance stock; artist momentum never decays; promo credit fictional; tax on receivables; earned-roster ratchet; Big-5 Top 10 premium carried by follower books (see `SimTools/MajorTop10ShareFableHandoff.md`); chart-proven wholesale bankrupting indies (uncovered sales book as instant full cash).
- **Cosmetic:** lineup churn never fires; controversy is a record field only.

New findings are worth more than re-confirming these, but a known item that turns out to be the root of several others should be ranked high.

## Historical anchors to score against

Use what the repo already has before researching:

- `historical-hot100-1960-64-recovered` and the Billboard 1968 notes (memory).
- Big-5 Top 10 share ≈ 35 / 20.6 / 21.2 (1960 / 61 / 62); Big-5 release share ≈ 18%.
- #1 single sales median ≈ 0.80M; LP units need all four channels (stores, clubs, racks, licensors).
- Landmark albums ≈ 25–40 per decade; soundtrack longevity 200–350 weeks.

If you need a figure the repo does not have, cite the source and give a range, not a point.

## Run rules

- **Reference bundle:** `SimLogs/writing-calibration/major-scale-bundle-request.json`. Working plan: `SimLogs/working-bundle/plan.json`.
- Decade runs **require** `--enable-genre-market-v2` and `--enable-artist-population-lifecycle`; without them the world is ~2× smaller and not comparable.
- Use `--calibration` (and `--aggregate-only` for gate runs) to cut output. Use `--lean-probe` for media runs.
- **Reuse existing controls**; rerun only if code changed.
- Prefer short runs (104–156 weeks) per finding. A decade A/B is **one paired seed** at the end of the audit, not per fix.
- Any change to stock, demand or ordering reshuffles the RNG stream. Single-seed deltas of ±3–4 Top 10 points are noise. Prove a switch is inert with a byte comparison of probe output.
- Diagnose from decision telemetry and per-record CSVs. **Never parse late-world saves.**
- Big-5 = exactly `label_0001`–`label_0005`. Never compare label IDs as strings.
- All audit output goes to `SimLogs/`; never commit it.
- Don't touch the frozen V3 baseline.

## Design rules for fixes

- **Closures must be earned.** A label folding on a bad act or song is correct; folding on a good record means demand or accounting is wrong. Never rescue with blanket tuning.
- Fix the mechanism, not the calibration constant, where the two differ. Say which one a proposed fix is.
- Every fix goes behind a new switch, default off, with a `--…-check` where practical.
- Prefer costs, saturation, decay and competitor response over caps.

## Deliverable

A markdown report in `SimTools/` (e.g. `GameyBehaviorAudit.md`) with:

1. A ranked table: finding, system, pattern (from the list above), evidence (file:line + run/metric), historical anchor, estimated distortion, proposed fix (mechanism or calibration).
2. For the top 2–3: a screened flag with a short-run A/B on two seeds and the metrics that moved.
3. A list of things checked and found **sound**, so they are not re-audited.
4. Open questions for the author where history is ambiguous.
