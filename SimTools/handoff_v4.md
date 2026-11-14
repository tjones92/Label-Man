
## Screen (2026-11-14): footprint dose for hit acts only — `fit208-v4-{2002,1001}-{lw0up1_ao2_lh05,lw0up1_ao2_lh1,lw0up1_lh1}`

`--label-blind-launch-dose-hot-acts` (with `--label-blind-launch-dose=D`): the dose opens only a non-Major single whose act had a Top 40 in the last 26 weeks (`HitActCadence` clock; checked at launch because the clock is configured after `LabelBlindLaunch`). `--label-blind-launch-check` passes with new cases; 27-week off run `lhd-off-2002-base` byte-identical to the first 27 weeks of `fit208-v1-2002-base`. Script arm `lhN`. Hit-act scorer now takes `name=fmt` args: `SimTools/analyze_hitact_followups_by_tier.py`.

Top 40 % of hit acts' singles (Top 40 ≤ 26 weeks before launch), 1961–63 pooled, Big-5 / MidTier / Independent / Small:

| arm | hit-act T40 % | B5 T10 1961 / 62 / 63 | stars | 
|---|---|---|---|
| control | 24.2 / 25.7 / 9.2 / 9.2 | 29.4 / 37.1 / 38.6 | 1.82 / 1.83 / 1.37 |
| `ao2` | 27.3 / 26.7 / 14.3 / 12.6 | 34.2 / 26.1 / 34.0 | 1.88 / 1.57 / 1.37 |
| `ao2_lh05` | 28.9 / 22.4 / 11.1 / 9.2 | 32.5 / 37.4 / 35.2 | 1.77 / 1.59 / 1.28 |
| `ao2_lh1` | 23.8 / 26.8 / 14.5 / 9.7 | 28.2 / 26.4 / 26.2 | 1.69 / 1.62 / 1.11 |
| `lh1` (no act record) | 14.4 / 10.2 / 8.3 / 4.1 | 32.1 / 24.9 / 29.5 | 0.86 / 0.69 / 0.59 |

- **The launch footprint does not carry the follow-up gap.** Opening every region for field hit acts leaves their follow-ups where they were (Independent 14.3 → 14.5 under `ao2_lh1`; Small 12.6 → 9.7; `lh1` vs `lw0up1` 6.1 → 8.3). Where a gap closes, it closes by the Big-5 falling (27.3 → 23.8), i.e. through chart-slot competition.
- `ao2_lh1` has the lowest Big-5 share of the act-record arms (mean 26.9 vs `ao2` 31.4, history ~22.6) with stars near control in 1961–62, but `ao2_lh05` moves the other way (35.0). Two seeds cannot separate 4-point effects: per-seed Top 10 swings are ±8–15 points (`ao2_lh1` 1963: 17.7 / 34.6).
- No MidTier closures after 1961 in any arm; #1 median 1.02–1.16M.
- Remaining follow-up carriers, from the decomposition: record quality (label craft, slate picks) and launch awareness's label part. Next: four seeds for the candidates before more design, since the act-record arms differ by less than the seed noise.
