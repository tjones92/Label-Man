# Response-earned destination auditions

Started 2026-10-30, following `LocalRadioConsumptionInvestigation.md`.
This is an opt-in fresh-world mechanism screen, not a production calibration.

## Experimental contract

Three arms retain the previous control profile, including indie tastemakers and
unchanged consumer coefficients, latent taste, inventory and common-market clearing:

- `ctl`: existing DJ picks.
- `audition_random`: one additional reserved Light seat (taken from the ordinary
  sheet), with eligible incoming reports ranked by a keyed random draw.
- `audition_response`: identical seat and duration, ranking reports by source response.

Both treatment arms keep the original two DJ-pick reservations. Their extra
research reservation displaces ordinary capacity; even when empty it remains
reserved. Thus treatment versus control includes a capacity-policy change.
Response versus random isolates selection policy within that common capacity,
but actual admission counts need not match in diverging live worlds.

For each title/region, before any station's meeting mutates its playlist, read
completed regional purchases and preceding regional rotation. A report requires
two consecutive observations with at least 20 purchases, positive listener-panel
rotation, and an ordinary station listing in that region. Protected trials cannot
alone produce a report. Reports travel with a one-meeting delay.

The screening response score is:

```
radio = max(preceding listener-panel rotation, sales-pass regional rotation)
score = purchases / [population in millions * (radio + 0.02)]
        * purchases / (purchases + 100)
```

This is a population/exposure-adjusted sales proxy with small-count shrinkage,
not a measured conversion probability or a causal attribution to radio. Taking
the maximum avoids adding overlapping panel and aggregate radio estimates but
does not solve overlap. Promotion, established awareness, fan purchases, stock
and the common purse still affect it. Backorders accompany decision telemetry.
The floor, shrinkage, minimum purchases and acceptance threshold are explicit
screening assumptions, not historically estimated values.

Each record can send reports to at most two format-compatible stations per week,
selected by keyed draws outside the source region. If multiple sources qualify,
the first eligible source in region-ID order supplies the report in both arms;
this avoids selecting the strongest source only in the response arm. Eligibility
still respects circulation, physical arrival, city reach and cooldown. Indie
tastemakers reject Major-label recommendations just as they do ordinary picks.
There is no national-fit eligibility test. National connection is logged only
for outcome stratification. No private fit score enters recommendation ranking.

A new audition lasts at most three weekly meetings in one reserved Light seat.
At least two sales passes following admission must each show positive local
panel rotation, at least 20 purchases and a response score of at least 100 before
ordinary graduation is permitted. Its ordinary score is format match times
`min(2, local score / 1000)`, with no national heat term. Acceptance permits
competition; it does not guarantee an ordinary seat. The same local rule applies
to subsequent retention/re-add competition for that station/title. An expired
research trial cannot restart. Ordinary picks cannot restart it either.

Recommendations and audition starts do not change `fitExposureAge`, restart a
lifecycle, remove the national connection gate, or add a consumer multiplier.
Existing chart/sales exposure paths still operate in every arm. Ordinary
placement can also happen without a research trial; telemetry distinguishes it.

## Interpretation limits

Destination evidence is regional, not station-specific. Another station in the
same region may contribute to acceptance. Stations make separate capacity and
format decisions, but do not observe statistically independent audiences.
The information network here samples format-compatible stations nationally;
it does not yet represent historical trade or promoter links.

The consumer path remains attenuated, and the national multiplier still shapes
actual demand. Failure to produce national breakouts would not, by itself,
reject information-led diffusion with a fuller listener model. A graduation is
an operational local-response outcome, not proof of incremental purchases caused
by that audition. Matched seeds compare worlds, not `gen_N` identities.
Destinations can already have another station playing the title. Results therefore
separately count admissions to regions with zero preceding panel rotation; this
means panel-silent at admission, not never previously exposed or wholly unaware.
All audition counts are station/title pairs, not unique records.

## Validation and reproduction

`PlaylistMeetingTests` covers bounded research capacity, expiry, cooldown,
no restart and the requirement that even a very high ordinary score cannot
graduate a trial without explicit destination acceptance. Existing playlist and
exposure tests remain applicable. Live `analyze_response_auditions.py` checks
report lag, cross-region routing, the two-report limit, one trial seat per
station, trial duration, admission history and graduation requirements.

The runner retains source and assembly snapshots, arguments and completion
receipts. The game project excludes retained `SimLogs` C# snapshots from compilation.
Research state is session-only; save/resume flags are rejected.

```powershell
dotnet build 'Label Man.csproj' --no-restore
dotnet run --project SimTools/PlaylistMeetingTests
$studyPython = 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe'
& $studyPython SimTools/run_local_consumption_screen.py --tag NEW --weeks 12 --seeds 1001 --arms ctl,audition_random,audition_response --observe --extra=--station-decisions
& $studyPython SimTools/analyze_response_auditions.py NEW
& $studyPython SimTools/verify_local_consumption.py NEW
& $studyPython SimTools/analyze_local_consumption.py NEW --horizon 8 --start 3
```

## Results

The 12-week screen `ra12-v1` completed three worlds on seed 1001. Of trials
starting by week 9 (three meetings of follow-up), random ranking graduated
30/107 (28.04%) and response ranking 43/108 (39.81%). With starts through week 8,
two consecutive ordinary listings occurred in 19/74 (25.68%) and 26/75 (34.67%).
There were 77 versus 65 mature rejections. Each arm graduated one trial below
the national connection threshold. These are operational outcomes, not causal
purchase effects or evidence of unexpected national hits.

All 92,505 consumption-probe rows and market integrity counters passed. Each
arm's 924 captured meetings obeyed tier capacities (2,772 total); both treatment
captures replayed exactly through the live resolver. With the feature off,
8,073 record rows, 31,642 playlist rows and 7,152 finance rows exactly match
the prior `lrc12-v2-ctl-1001` control as parsed CSV strings.

The unchanged build completed nine 26-week worlds, seeds 1001–1003, in
`ra26-v1`. All receipts report zero exit codes and unchanged assembly hashes.
There are 12 new worlds in total; the short screen and its same-seed extension
are not independent replications.

For graduation, include admissions through week 23, allowing the three-week
trial to finish. For persistence, include admissions through week 22, allowing
an additional ordinary meeting. Both use all such admissions, not a cohort
selected on later chart success. Persistence means at least two consecutive
ordinary listings within the observed window, not permanent retention.

| Seed | Random: mature graduations / starts | Response: mature graduations / starts | Random: two ordinary weeks | Response: two ordinary weeks |
|---|---:|---:|---:|---:|
| 1001 | 134/459 (29.19%) | 247/493 (50.10%) | 115/435 (26.44%) | 217/459 (47.28%) |
| 1002 | 136/462 (29.44%) | 227/486 (46.71%) | 116/435 (26.67%) | 203/463 (43.84%) |
| 1003 | 139/461 (30.15%) | 244/488 (50.00%) | 121/441 (27.44%) | 215/459 (46.84%) |

The equal-weight mean graduation rate rises from **29.59% to 48.94%**, a paired
gain of 19.34 percentage points (SE 1.08). Two-week ordinary persistence rises
from **26.85% to 45.99%**, a gain of 19.14 points (SE 1.07). Three adaptively
selected screening seeds and arbitrary thresholds do not support precision
claims beyond this experiment.

The gain includes previously panel-silent destination regions: across seeds,
random ranking graduates 199 of 921 mature admissions there, versus response
ranking's 310 of 870. It is not solely filling additional stations in regions
already carrying the title. Again, panel silence does not establish zero other
exposure or incremental purchases caused by the new station.

There are genuine failures. Of the mature admissions, random ranking rejects
973 and response ranking rejects 749. Among these, 55 versus 83 satisfy the
local acceptance rule but fail to obtain an ordinary seat; the others lack
acceptance. Late admissions remain open and are excluded from these rates.

However, the proposed upside for poor national priors is **not demonstrated**.
Random ranking gives 287 mature auditions to records below the connection
threshold and graduates 15; response ranking gives only 123 and graduates 14.
The response rule concentrates opportunity on titles already favoured by the
current demand process. Removing a nomination veto is not equivalent to removing
the national demand multiplier. These counts are auditions, not unique breakout
records, and no unexpected national-breakout claim follows from them.

### Economic and national outcomes

Equal-weight seed means below. Demand, purchases and closures cover the whole
26-week world; chart and sales-size outcomes follow weeks 5–15 releases for
12 weeks. Brackets use the fixed 2+1 station panel. Exported concentration uses
all record histories, not single-only market clearing.

| Arm | Single purchases, M | Raw single demand, M | Closures | Indie Top-10 lead ≥2, full [fixed] | Million-copy cohort releases | Ten-largest-sellers unit share |
|---|---:|---:|---:|---:|---:|---:|
| Control | 57.066 | 165.470 | 10.00 | 15.03% [6.41] | 1.33 | 33.14% |
| Random recommendations | 57.031 | 155.893 | 9.67 | 11.76% [7.59] | 1.00 | 30.52% |
| Response recommendations | 57.031 | 160.306 | 9.33 | 16.12% [7.02] | 1.00 | 30.54% |

Response versus random raises the full-panel lead rate in each seed, but the
fixed-panel mean falls slightly. Relative to control, the full-panel response
rate improves in seed 1001 and declines in 1002 and 1003. Response and random
have virtually identical total purchases within each seed despite different
raw demand and many different playlist decisions. Their closure counts are
8/15/5 versus 8/15/6; control is 7/16/7. This screen does not establish an
economic gain or improved national-breakout production. The trial capacity
change also redistributes playlist space, so control comparisons cannot be
attributed solely to information quality.

All **1,342,558** half-year consumption-probe rows pass bounds/identity checks,
and all exported inventory/allocation/reconciliation/settlement counters are
zero. Live audition validation passes for all nine worlds, including report
lags, routing, capacity, trial duration, two local response observations before
acceptance, and a terminal outcome for every mature trial. Results are retained
in `audition-metrics.csv`, `audition-means.csv`, `audition-paired.csv`,
`audition-validation.json`, and the ordinary economic/lead analysis tables.

### Interpretation and next step

Keep this feature experimental. Local response carries useful information for
choosing auditions that survive the specified destination rule; both rejection
and continued rotation occur without an explicit national-fit permission check.
The effect persists beyond protected trial time and includes panel-silent regions.

It remains primarily a better selection mechanism operating on the old consumer
model. It does not establish that auditions create substantial new local demand,
or that a weak national draw can become a national success through accumulated
local adoption. The next discriminating extension is the bounded listener-state
model described in the original investigation, crossed with these two selection
policies. Preserve a small exploration route when testing that extension: this
screen shows that ranking solely by observed sales response can sharply reduce
auditions for records with poor national priors. No production default was changed.
