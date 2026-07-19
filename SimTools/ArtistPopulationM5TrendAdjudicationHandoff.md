# M5 Trend Adjudication Handoff

Status: **M4 PASS / FROZEN SOURCE / M5 REQUIRED BUT NOT LAUNCHED**

Date: 2026-07-18

This handoff records the owner's accepted M4 result and answers the follow-up
question raised by the 1964-to-1965 control-ratio decline. It supplements
`ArtistPopulationResponsiveMemoryCostOnceM4ResumeHandoff.md` and supersedes its
M5 run prefix. It also supersedes any older instruction to restore or run
`analyze-market-clearing-format-memory.mjs`; that analyzer is deliberately
deleted. Adjudicate from the established raw telemetry.

## Decision

M4 does **not** show an absolute 1964-to-1965 market downturn. The candidate's
total units rose from `147,891,363` to `151,673,634` (`+2.56%`), and its Album
units rose from `6,228,774` to `9,320,558` (`+49.64%`). The ratios fell because
the retained control grew faster: total units rose `+10.27%` and Album units
rose `+69.47%`.

The current candidate may be healthier than the control. Its growth is bounded
by regional service and physical inventory rather than national pooling, its
1965 Album-decision share is already above control, and the structural,
settlement, inventory, allocation, and lifecycle checks reconcile.

M4 nevertheless cannot prove that this remains healthy through the late-decade
format transition. It contains only one year after the 1964 ratio break. The
control changes shape sharply in 1966: total units fall from `175,821,672` to
`136,089,004`, Singles fall from `164,692,558` to `116,814,287`, and Albums rise
from `11,129,114` to `19,274,717`. A candidate can therefore look weak against
the control in 1965 and recover in total ratio in 1966 while still having either
a real Album-yield lag or an excessive Single tail.

Run the frozen-source M5 decade to distinguish those possibilities. Do not
change source, rerun M1-M4, or launch any simulation while preparing for M5.

## Accepted M4 evidence

The accepted run is:

```text
d6-bounded-spillover-75-through-1965-1001
```

It completed `313` ticks normally, contains six complete annual rows for
1960-1965, and has a header-only catastrophic stream. It contains 64 artifacts.
The known post-completion `MissingSingletonsTemp.cs` diagnostic remains
non-fatal.

Required M4 gates passed:

| Gate | Result | Floor |
|---|---:|---:|
| 1964 Album units | 0.948457 | 0.80 |
| 1964 label net | 0.939574 | 0.85 |
| 1965 Single units | 0.864356 | 0.85 |
| 1965 Album units | 0.837493 | 0.80 |
| 1965 total units | 0.862656 | 0.85 |
| 1965 gross | 0.859954 | 0.85 |
| 1965 label net | 0.864305 | 0.85 |
| 1965 market net | 0.865290 | 0.85 |

Completed 1963 scheduled Albums are `1,265`, exceeding the minimum `1,013`.
There are zero weekly, annual, clearing, inventory, allocation, or regional
settlement unit mismatches. There are `28,599` scoped first memory observations,
zero memory lifecycle violations, and maximum confidence `0.65`.

The implemented repair remains regional:

- only configured one-hop neighbor edges;
- recipient imports capped at 15%;
- donor exports capped at 75% of unused local capacity;
- no forwarding;
- no national pooling; and
- the common-market capacity multiplier remains `1.34`.

Raising the previously over-restrictive donor-export cap from 50% to 75% added
`813,742` spillover units and `2,561,556` 1965 total units versus the preceding
near-pass. This was a bounded removal of stranded adjacent capacity, not an
increase in base or national capacity.

## Why the ratio decline is not yet a bad-trend finding

The full M4 ratio path is not a monotone decline:

| Year | Single units | Album units | Total units | Gross | Label net | Market net |
|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 0.977558 | 1.038821 | 0.978348 | 0.993477 | 1.014414 | 1.024827 |
| 1961 | 0.988870 | 1.160779 | 0.993959 | 1.009624 | 1.027423 | 1.031122 |
| 1962 | 0.994207 | 1.249192 | 1.003540 | 1.032480 | 1.041577 | 1.043578 |
| 1963 | 1.044074 | 1.137769 | 1.048094 | 1.061113 | 1.068378 | 1.069629 |
| 1964 | 0.926580 | 0.948457 | 0.927481 | 0.931316 | 0.939574 | 0.941284 |
| 1965 | 0.864356 | 0.837493 | 0.862656 | 0.859954 | 0.864305 | 0.865290 |

Regional clearing also does not show a monotone service collapse:

| Year | Serviceable intent | Cleared units | Cleared / serviceable | Spillover | Residual displaced | Unused capacity |
|---:|---:|---:|---:|---:|---:|---:|
| 1960 | 159,660,518 | 141,582,066 | 88.68% | 726,430 | 18,078,452 | 10,018,776 |
| 1961 | 174,132,101 | 149,026,445 | 85.58% | 800,478 | 25,105,656 | 2,648,445 |
| 1962 | 175,179,641 | 149,131,143 | 85.13% | 960,713 | 26,048,498 | 2,703,982 |
| 1963 | 174,608,721 | 149,078,667 | 85.38% | 1,075,743 | 25,530,054 | 2,871,488 |
| 1964 | 164,919,963 | 147,891,363 | 89.67% | 1,258,135 | 17,028,600 | 4,241,184 |
| 1965 | 173,425,379 | 151,673,634 | 87.46% | 1,424,049 | 21,751,745 | 3,526,258 |

The strongest evidence for a healthy transition is format choice: the candidate's
Album-decision share rises from `0.316600` in 1964 to `0.583629` in 1965,
compared with control `0.398137` to `0.551247`. The candidate is not refusing to
choose Albums by 1965.

The unresolved warning is yield and capacity. Candidate 1965 clearing is
`151,673,634 / 153,775,843 = 98.63%` of summed regional base capacity while
`21,751,745` units of residual displaced demand remain. Candidate Album units
are only `0.837493x` control even though its Album-decision share is higher.
That can be a legitimate consequence of healthier bounded service, or it can
indicate a late-decade Album fulfillment/yield lag. M4 alone cannot decide.

## Frozen source identity

Before M5, require the following SHA-256 values. If any differs, stop without
launching M5 and report the mismatch; do not attempt to recreate the source by
guessing.

```text
05C36AA077580176BB9380D005C9BADC493FBABDA9945F510DE285EA9F853412  Data/AILabel.cs
ACE17C624A8CBA3C1CFEC900B781143E27881576F2D07821E1B2F7155E388ED3  Data/AlbumProject.cs
153B9764334951BA97D94152F756D45D801B2402D1A16755DA23AEA1AE7867F8  SimTools/ArtistPopulationLifecycleProbeSuite.cs
814A65E81FF48145E62A0A7704CD55F32012B103F399F09AE440EB9EE34CCB93  SimTools/ChartAuditRunner.cs
C5F0DBF855B4FF83781E54705FFD941D65CED9249FB45507CF4E75987BEEE859  SimTools/GenreMarketV2ProbeSuite.cs
DF6F5B01494314C3D55A6CADB206777B342725B4C5FA055E37057F3D8D800957  Systems/AlbumModel.cs
D11A61E2FBC8034AA1BF09D7B35F3F74D2EB89CC82DD4D9B54043FA9B5D3D52C  Systems/ChartManager.cs
BA6B3039615C0A25481B3FAB79DB029E579DCAAC87901D17F21C235599621349  Systems/CompetitorManager.cs
D2BFA31FA5894C48EBA65AB7467B7714B4EC24B317342FB9EF97050BD5BBA70E  Systems/DistanceModel.cs
```

The deletion of `SimTools/analyze-market-clearing-format-memory.mjs` is
intentional. Do not restore it. The current working tree also contains unrelated
`.uid` files and handoff documents; preserve them.

## M5 execution

Use the retained control:

```text
d6-transition-envelope-decade-control-1001
```

Use this new, never-before-used M5 prefix:

```text
d6-bounded-spillover-75-decade-enabled-1001
```

Run exactly:

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
$control = 'd6-transition-envelope-decade-control-1001'

& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=d6-bounded-spillover-75-decade-enabled-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --gate-control-run=$control
```

Do not run a probe, disabled replay, M3 repeat, or another M4 first. Their
accepted evidence is retained, and another simulation would not answer the
specific late-decade question as directly as M5.

## M5 acceptance

Adjudicate directly from `weeks.csv`, `market-revenue.csv`,
`decade-annual-rollup.csv`, `release-capacity.csv`, `fork-ratios.csv`, and
`album-projects.csv`. Use settlement, regional allocation, clearing, spillover,
and responsive-memory streams as required reconciliation and causal diagnostics,
but never let a diagnostic ledger replace an established economic value.

Require:

- normal `CHART_AUDIT_COMPLETE`, process exit zero, and 522 completed ticks;
- complete annual rows for 1960-1969;
- a header-only catastrophic stream;
- annual successful releases in `[0.85,1.15]`;
- annual scheduled Albums in `[0.80,1.20]`;
- annual Single units in `[0.85,1.15]`;
- annual Album units in `[0.80,1.20]`;
- annual total units, gross, label net, and market net in `[0.85,1.15]`;
- decade Single and Album units in `[0.85,1.15]`;
- decade total units, gross, label net, and market net in `[0.90,1.10]`;
- 1966 Album units at least `0.80x` control;
- 1969 scheduled-Album share inside inclusive `[0.78,0.85]`;
- 1969 scheduled-Album count at least `0.80x` control;
- no annual Single, Album, or total-unit ratio above its inherited upper band;
- the inherited paired all-decade closed Top-40 median movement no greater than
  `+/-2` weeks;
- zero weekly, annual, settlement, booking, audit, spillover, allocation,
  inventory, chronology, ownership, lifecycle, finance, and non-finite
  violations;
- only configured one-hop spillover, no forwarding, 15% recipient import caps,
  75% donor export caps, and no national pooling;
- the `1.34` common-market capacity multiplier unchanged; and
- responsive-memory confidence at most `0.65`, one effective observation per
  scoped release, valid revision chronology, and the 104-week horizon.

## Required trend report

Passing control-ratio gates is necessary but is not the whole health finding.
For every year, print one table containing:

- candidate absolute Single, Album, and total units;
- candidate year-over-year changes;
- control absolute values and year-over-year changes;
- candidate/control ratios;
- successful releases, scheduled Albums, Album drops, decisions, and
  Album-decision share;
- serviceable intent, base capacity, local clearing, spillover, final clearing,
  unused capacity, physical backorders, and residual displaced demand;
- cleared/serviceable and cleared/base-capacity percentages;
- Single and Album units per successful release and per corresponding format
  decision;
- memory observation ages, confidence, revisions, and format reversals; and
- label cash/status/roster evidence if it mediates a material change.

Treat 1965 as a denominator-transition hypothesis, not a predetermined failure.
Pay particular attention to:

1. whether the candidate's absolute market contracts for two or more consecutive
   years;
2. whether clearing/serviceable intent, unused capacity, residual displacement,
   or backorders worsen persistently rather than fluctuate;
3. whether Album decision share continues the 1965 transition;
4. whether Album units per Album decision recover, stabilize, or deteriorate;
5. whether Singles decline as the era shifts instead of remaining artificially
   pinned near the regional capacity ceiling; and
6. whether a ratio recovery is caused only by the control's 1966 contraction.

End with exactly one evidence-supported classification:

```text
HEALTHIER_BOUNDED_TRANSITION
CONTROL_RELATIVE_TROUGH_ONLY
CAPACITY_SATURATION
FORMAT_TRANSITION_LAG
MIXED_OR_UNRESOLVED
```

`HEALTHIER_BOUNDED_TRANSITION` requires both all hard gates and non-deteriorating
absolute/operational evidence. Do not call the candidate healthier merely
because the control contracts, and do not call it unhealthy merely because it
does not reproduce the control's 1965 peak.

## Terminal instruction

This handoff authorizes one frozen-source seed-1001 M5 run only. Do not tune
between years, alter the control or gates, change any source before launch, run
seeds 1002/1003, or select a holdout.

If M5 passes, append the full M4/M5 record and the trend classification to
`ArtistPopulationLifecycleAudit.md`.

If M5 fails, preserve the artifact, append the exact failed gate and trend
evidence to the audit, and stop. A failure is diagnostic evidence, not authority
for another behavioral correction.
