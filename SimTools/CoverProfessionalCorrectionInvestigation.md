# Cover-to-professional imbalance: controlled workflow evidence

2026-11-02. This is a reanalysis of the existing corrected workflow matrix, not a
new intervention. The historical 1:1–2:1 range is an uncertain US early-1960s
singles calibration hypothesis, not a quota or measured distribution. See
`FreshMaterialHistoricalCalibration.md` for definitions and historical sources.

## What has already been tested

The eight `writing-workflow-v2` runs have two seeds, a common frozen assembly,
104-week endpoints and the four policies described in
`ProfessionalWritingWorkflowExperiment.md`. All use the finite writing market,
causal repertoire selection and the release programme. We applied the existing
fresh-material analyzer to each run. It counts each single intention proposed by
week 78 once and includes it if issued within 26 weeks, with actual material
provenance. Covers mean `CoverStandard` plus `CoverRecentHit`; traditional/public
domain is separate. This is not a census of all releases or all signed artists.

| Workflow | Issued | Covers | Fresh professional | Pooled ratio | Ratio seed 1001 / 2002 | All writing fees, pooled | Orders pooled |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| Control | 9,939 | 6,126 | 762 | 8.04:1 | 7.69 / 8.42 | $2,113,892 | 4,260 |
| Planned only | 9,997 | 6,313 | 600 | 10.52:1 | 10.40 / 10.65 | $958,023 | 1,100 |
| Delivery only | 9,952 | 5,887 | 1,227 | 4.80:1 | 4.73 / 4.87 | $1,997,654 | 3,957 |
| Both | 9,956 | 6,120 | 948 | 6.46:1 | 6.52 / 6.39 | $962,896 | 1,203 |

Fees and orders cover the entire 104-week run, whereas material mix covers only
mature intentions. Fees include commissions, separate licenses and retainers;
these totals are not profit or costs per issued plan. Rows pool seeds, while the
last ratio column shows seed-level consistency. Different arms need not issue the
same intentions; aggregate changes do not identify individual substitutions.

Delivery-only moves the ratio toward the range in both seeds: pooled fresh
professional issues increase by 465 and covers decrease by 239 versus control.
Planning alone moves it away: fresh professional issues decrease by 162 and
covers increase by 187. Combining the two improves on control but trails delivery
alone. Thus improving delivery access helps, but this specific change is
insufficient; reducing orders through planning also changes the stock of demos
available for subsequent licenses. The experiment does not isolate why the
planned arm has fewer professional issues. Reinstating all unplanned buying is
not established as a desirable solution: client usage of mature deliveries was
low and the original experiment found mixed or adverse concentration effects.
The existing report documents the economic and career tradeoffs in detail.

The result also does not prove shortage: the companion opportunity audit observes
substantial world demo stock, but that stock is not the pool each act can access,
afford or use. Commission order counts are not unique accessible songs or the
number of professional issues. The actual marginal bottleneck could be demand,
genre, visibility, price, fit, bounded sampling or subsequent selection.

## Scale of correction

In the combined arm, 6,120 covers and 948 professional issues yield 6.46:1. As
an accounting illustration only, if each change replaced exactly one cover with
one additional fresh professional issue and all else stayed fixed, at least
1,408 such replacements among 9,956 issues would reach 2:1; 2,080 would reach
4:3. That is approximately 23% and 34% of the original cover issues. These are
not forecasts or an estimate of viable opportunities: in a live simulation,
release counts, quality, costs, other sources and future supply all change.
The illustration shows that a small improvement cannot by itself close this gap.

## Recommended controlled investigation

1. Add an opt-in, non-mutating decision observer for eligible release plans and
   repertoire arrivals, including act craft, professional pool size, visibility,
   deduplication, rights, cash, fit, candidate values and commission refusal
   reasons. Use existing deterministic reads and no additional random draws.
   Retain plans without proposals and those not issued; deduplicate repeated
   reviews. Replay matched seeds and verify core outcome files byte-identical.
2. Separate demand from discovery before tuning. Record how often a feasible,
   funded plan with a weak best perceived candidate encounters no professional
   arrival, an inaccessible or unsuitable sample, or a worthwhile professional
   offer displaced by a better cover. In parallel record when a professional
   arrival finds no usable demo but candidate count blocks commissioning. Do not
   equate a declined event or unused world stock with any one reason.
3. Select one dominant, measurable restriction for an opt-in intervention.
   A candidate is a quality-sensitive commission decision for a feasible single
   rather than the active-candidate count, while retaining budget, capacity,
   existing-delivery, duplicate-order and timing safeguards. A separate candidate
   is broader sampling of accessible professional demos, if observed sampling
   loss dominates. Do not combine these in the first screen; the current data
   cannot choose between them. Instrument any trial's triggered and accepted
   decisions so a null result is interpretable.
4. Run matched control and treatment on multiple seeds with a frozen assembly;
   compare all five issue sources, the ratio by genre/career and craft band,
   releases and timing, professional quality, paid-but-unused material, writing
   and label economics, and complete 13-week chart outcomes. Retain the prior
   career-concentration and fixed-leader checks. Expand to a later-decade holdout
   before considering a default. Treat a ratio within 1:1–2:1 as a plausibility
   signal, not sufficient evidence to ship, and investigate any genre-specific
   distortion or excessive expense.

No gameplay settings were changed. The present matrix identifies a promising
routing mechanism and a counterproductive planning-only result; it does not yet
establish a safe correction to adopt.

## Reproduction and checks

Run the following from the repository root with Python 3:

```text
python SimTools/analyze_fresh_material_access.py writing-workflow-v2-control-market-1001 writing-workflow-v2-control-market-2002 writing-workflow-v2-planned-market-1001 writing-workflow-v2-planned-market-2002 writing-workflow-v2-delivery-market-1001 writing-workflow-v2-delivery-market-2002 writing-workflow-v2-combined-market-1001 writing-workflow-v2-combined-market-2002 --output SimLogs/writing-calibration/fresh-material-workflow-matrix.json
```

The analyzer rejects mixed assembly hashes, incomplete 104-week runs and missing
issued provenance. The control and combined totals match the independent
opportunity-observer replay already checked in the prior investigation. Fee,
order and career results come from `workflow-v2-results.json` and its accompanying
experiment report. This comparison is limited to two seeds and a historical
working assumption, and cannot determine the effect of an untested change.
