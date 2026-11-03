# Professional material funnel and sampling screen

Status: two 104-week observer runs and a four-run, 52-week controlled sampling
screen completed. The candidate remains an experimental switch; no default
game policy has been changed.

The observer is opt-in under `--material-funnel-audit`. For the programme's first
eligible material-search review of each mature (26-week follow-up) single plan,
it records the exact primary-genre pool, unique bounded draws, mutually ordered
access failures, owned delivery, candidate reads, positive net offers, selected
source and whether work starts. It saves ability and effective craft at the
review, not inferred from release source. It observes this one programme path;
repertoire arrivals, commissions, and plans without reviews have different
exposure. `CanLicense` combines availability/status, holds/priority and the
label's affordability, so the `license` column does not isolate these causes.
The original 104-week runs used the first version of the observer. The expanded
version adds start-block reasons and a separate professional-arrival outcome
trace. It does not trace all commission refusal causes.

The two 104-week combined-workflow runs share a frozen assembly. Each reported
normal completion; the analyzer checks endpoint and deduplicates by plan. Of
12,909 mature single plans, 12,637 had a recorded programme-search review within
26 weeks. Across those first reviews, 44,843 unique professional pool draws were
observed; 42,817 (95.5%) failed the combined market license check and 2,026
passed all sampled label-level checks. 10,778 reviews (85.3%) had neither an
accessible sampled professional song nor an owned delivery. Those measures count
sampled entries or reviews, not distinct world songs or acts. Some plans are not
reviewed, and the same song can be drawn for different plans.

A professional proposal had positive net value in 1,037 first reviews, was chosen
in 576 and actually started in 221. A better cover can win even where a
professional proposal is positive. The remaining starts can fail timing,
capacity, cash, or artist-specific licensing. Neither the gap nor the sampling
loss is a causal estimate of extra profitable professional releases.

A 13-week observer-off/on pair on the initial build matched all 105 comparable
CSV files byte for byte. After expanding the observer, a second 13-week pair on
the new build also matched all 105. This is a bounded noninterference check, not
proof for every save or state. The original observer's two full 104-week runs
also match their earlier `opportunity-combined` replays on nine chart, release,
finance, supplier, order, partnership and provenance files in both seeds. The
observer analyzer's fixture tests cover
first-review deduplication and incomplete windows.

The isolated experimental switch `--material-accessible-sampling` prefilters the
professional genre pool using the existing label-level access predicate before
the same four deterministic draws. It does not add stock or relax rights,
pricing, payment, fit or final artist-specific licensing. It changes the makeup
of the sample used to form the baseline as well as the available proposals;
thus the trial is an end-to-end test of discovery order, not merely an extra draw.
Four matched 52-week runs, two seeds, compare switch off and on under the combined
workflow. This screen cannot establish full 52-week career outcomes or a
later-decade holdout.

## Controlled screen results

All four runs completed without logged errors on the same assembly. Counts below
are distinct single intentions proposed through week 26 and issued within 26
weeks; they are not every single released during the run. Covers combine
standards and recent hits; traditional/public-domain songs remain separate.

| Seed | Arm | Mature plans | Issued | Covers | Professional | Cover : professional |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| 1001 | Control | 2,544 | 2,058 | 1,280 | 220 | 5.82:1 |
| 1001 | Accessible sampling | 2,551 | 2,040 | 1,203 | 295 | 4.08:1 |
| 2002 | Control | 2,461 | 2,045 | 1,251 | 223 | 5.61:1 |
| 2002 | Accessible sampling | 2,442 | 2,039 | 1,183 | 312 | 3.79:1 |

Pooled professional use rises from 443 to 607, covers fall from 2,531 to 2,386,
and the ratio moves from 5.71:1 to 3.93:1. This is directionally consistent in
both seeds but remains outside the proposed 1:1–2:1 sensitivity range. New
signing professional issues increase from 148 to 195 and 137 to 201; issued
new-signing plans decrease from 1,661 to 1,638 and from 1,622 to 1,609. The
populations of issued plans can differ, so these are not paired song swaps.

The intended discovery mechanism is visible: among first reviews, professional
offers selected increase from 234 to 767 pooled, and professional starts from
107 to 348. Plans with neither an accessible sampled professional song nor an
owned delivery fall from 3,974/4,793 reviewed plans to 2,863/4,782. Pool size
means different things between arms: in treatment it is the filtered pool.
Access is still label-level; the later act-specific license can fail. The change
does not force the selected material to issue. Among professional selections that
did not start, label capacity is the largest observed category: 86 in controls
and 244 in treatments. Other or artist-specific-license failures are 26 and 94;
cash failures 11 and 42, supplier-capacity failures 4 and 39. These figures are
first reviews only; retries and changed eligible plans make them descriptive,
not an estimate of the effect of changing any guard.

Professional-arrival observations cover events, not unique plans: ordinary
arrivals alone hit the candidate-count commission gate 1,250/1,337 times in the
controls and 1,252/1,382 in treatment. Many more attempts report
`commission-refused`; that combines pending or recent work, owned delivery,
plan timing, eight-fee reserve and other guards, and cannot identify which one
caused a refusal. Initialization is reported separately in the JSON. Neither
arrival counts nor the programme funnel rank the global causes of low release
share by themselves.

| Seed | Arm | All writing fees, 52 weeks | Licensed songs at endpoint | Failed labels | Fixed top-30 act chart-slot share |
| --- | --- | ---: | ---: | ---: | ---: |
| 1001 | Control | $223,638 | 46 | 58 | 39.7% |
| 1001 | Accessible sampling | $231,217 | 64 | 61 | 45.2% |
| 2002 | Control | $236,016 | 42 | 52 | 43.2% |
| 2002 | Accessible sampling | $228,284 | 58 | 54 | 45.5% |

Writing fees include commissions, licenses and retainers for the entire run;
they are not limited to mature single intentions or equivalent to profit.
Endpoint licensed stock is a snapshot, not a count of songs that will never be
used. Fixed top-30 acts are identified retrospectively in each run; the share
increase is a warning about concentration, not a matched-cohort causal effect.
There are only two seeds and 52 weeks. Complete 52-week post-hit career cohorts,
longer-term label economics and later-decade behavior are not established.

## Decision and reproduction

Do not promote this switch to a default based on this screen. It improves access
and the ratio without reaching the proposed range, while concentration increases
in both seeds and endpoint licensed stock rises. Do not simply relax the capacity
guard or repeat the change until a ratio is hit. The next useful research is to
classify the grouped commission refusals and follow up the capacity consequences,
then test any further change separately against a 104-week matched control with
complete 13-week chart and 52-week career windows. Include later-decade and
additional-seed holdouts before a default decision. The experimental flag is
for fresh research runs; it is not persisted as a save compatibility setting.

`SimLogs/writing-calibration/material-funnel-observed.json` contains the original
two-seed observer results. `sample-screen-material.json`, `sample-screen-funnel.json`,
`sample-screen-economy.json` and `sample-screen-concentration.json` contain the
four-run screen. For reproduction, run `RunWritingMarketCalibration.ps1` with
`-Workflow combined -Weeks 52 -MaterialFunnelAudit -ConcentrationAudit` on seeds
1001 and 2002, with `-AccessibleProfessionalSampling` only on treatment runs.
Use `analyze_fresh_material_access.py --weeks 52`,
`analyze_material_funnel.py --weeks 52`, `analyze_writing_market.py`, and
`analyze_writing_concentration.py --fixed-leaders 30` on the four run names.
The material and funnel analyzers reject mixed assembly hashes and incomplete
endpoints; the concentration analysis reports zero missing provenance. The
existing workflow analysis describes the larger 104-week safeguards.
