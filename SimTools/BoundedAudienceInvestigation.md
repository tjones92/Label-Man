# Bounded local audience experiment

Started 2026-10-30, following `ResponseAuditionInvestigation.md`. Research switches
only; fresh uninterrupted worlds; no production default changes.

## Contract

The four-arm radio experiment crosses recommendation ranking (keyed random or
source-response) with the existing consumer model or a bounded anonymous audience.
Both recommendation policies keep the prior trial-capacity and acceptance rules.
Two additional arms add enthusiast sharing to the bounded model, changing no
other coefficient. No same-record matching is inferred from generated IDs in
diverging worlds.

| Arm | Recommendation | Anonymous consumers |
|---|---|---|
| `audition_random` | Random | Existing model |
| `audition_response` | Response | Existing model |
| `audience_random` | Random | Bounded, radio-driven |
| `audience_response` | Response | Bounded, radio-driven |
| `sharing_random` | Random | Bounded, radio plus enthusiasts |
| `sharing_response` | Response | Bounded, radio plus enthusiasts |

For each live AI single/region, fix capacity `M` at first demand evaluation to
the existing potential buying audience minus the release's returning-fan share.
`C` is interested nonowners, `B` settled anonymous owners, and `E` active anonymous
enthusiasts. Enthusiasts overlap owners and nonowners; they are not another buying
pool. Initial `C` is `M` times the existing aware share; initial `B=E=0`. Launch
awareness is thus a seed of interest, an explicit screening simplification.

At the start of each demand pass, freeze preceding listener-panel rotations and
enthusiast totals before any title's state changes. For each region:

```
radioContacts = 2 * titlePanelRotation / max(1, sum(all single panel rotations))
peerContacts = 0                              [radio-only arms]
             = 2 * E / max(buyingPopulation, sum(active title E)) [sharing arms]
externalContacts = 0.02 * existingAwareShare
response = quality^2
newInterest = (M-B-C) * (1-exp(-externalContacts-response*(radioContacts+peerContacts)))
Edecayed = 0.75 * E
newEnthusiasts = (M-Edecayed) * (1-exp(-0.35*response*(radioContacts+peerContacts)))
anonymousIntent = (C+newInterest) * (1-exp(-existingConversionRate))
```

Then clear anonymous plus returning-fan intent through existing stock, store
access and the common purse. After the existing fan settlement partitions the
integer sales ledger:

```
anonymousPurchases = clearedPurchases - settledReturningFanPurchases
B' = B + anonymousPurchases
C' = 0.9 * (C + newInterest - anonymousPurchases)
E' = Edecayed + newEnthusiasts
```

The existing fan release snapshot is excluded once from the anonymous population;
its demand and purchase depletion remain in `ArtistFanbase`. Cohort allocations
are fractional expected people, while total settled purchases remain integers.
Anonymous owners plus fan owners must equal cumulative regional title sales.
The final serviceable order is capped at `floor(anonymousIntent+fanIntent)`, so
post-demand sales jitter cannot buy more than the finite intended audience.
Unfilled intent does not create owners. Interest loss returns nonowners to the
available pool; owners never return. Capacity does not refresh weekly.

Radio and peer contacts each sum to at most two per regional potential buyer per
week across titles. The panel denominator includes opening catalogue competition,
even though the new state applies only to live releases. These are normalized
contact opportunities, not measured unique listeners. The external-awareness
route retains the old promotion/chart awareness inputs and is not a new shared
promotion-attention model.

## What is held fixed, and what this does not establish

All coefficients above are explicit mechanism-screen assumptions. Quality is
persistent, and the **entire existing conversion rate** remains in purchase
willingness: national/regional commercial fit, chart purchase exposure, lifecycle,
soft saturation and other modifiers. This first experiment therefore adds a hard
ownership bound while retaining old soft exhaustion and age decay. It does not
claim to have replaced those mechanisms or national chart feedback.

The initial aware-share seed and ongoing external-awareness contacts preserve a
route for campaigns; they do not mean awareness and interest are empirically the
same. Enjoyment uses quality, while buying additionally faces the full commercial
fit multiplier. Nonbuyers can become enthusiasts from listening, and enthusiasm
decays without reinforcement. Regional cohorts are not city/segment networks.

`legacyRaw` in the new trace is a same-state existing-model demand calculation,
not an untreated counterfactual world. The old `local-consumption.csv` observer
runs before this replacement and before the fan partition; it cannot reconstruct
final audience demand. Use `bounded-audience.csv` and the clearing ledger.

The diagnostic that divides purchase rate by commercial fit holds current
interest and all other factors fixed. It measures arithmetic sensitivity only;
it does not forecast a world with national fit removed. Connection-stratified
medians compare selected populations, not causal treatment groups.

## Verification

The pure state transition has checks for no-source/no-seed demand, stockouts,
complete ownership saturation, nonbuying enthusiasts, unreinforced decay,
oversettlement rejection, and 20,000 randomized multiweek conservation steps.
Live traces additionally check the fan/anonymous split against cumulative title
units, fixed capacity, integer order bounds, state continuity, contact budgets
and finite telemetry. Existing inventory, cash/settlement and trial checks remain
separate. Feature-off controls must reproduce prior exports.

```powershell
dotnet build 'Label Man.csproj' --no-restore
dotnet run --project SimTools/PlaylistMeetingTests
$studyPython = 'C:/Users/grohl/AppData/Local/Programs/Python/Python314/python.exe'
& $studyPython SimTools/run_local_consumption_screen.py --tag NEW --weeks 12 --seeds 1001 --arms audition_random,audition_response,audience_random,audience_response --observe
& $studyPython SimTools/analyze_bounded_audience.py NEW
& $studyPython SimTools/analyze_response_auditions.py NEW
& $studyPython SimTools/verify_local_consumption.py NEW
& $studyPython SimTools/analyze_local_consumption.py NEW --horizon 8 --start 3 --control audition_random
```

## Results

### Radio-only short screen

`ba12-v1` completed four 12-week worlds, seed 1001. Both feature-off arms
exactly reproduce the prior recommendation experiment's parsed record, playlist
and label-finance exports. All 61,446 audience-state observations and 123,144
pre-replacement observer rows pass validation, with zero market integrity errors.

| Consumer / ranking | Single purchases, M | Raw single demand, M | Mature trial graduation | Two ordinary weeks |
|---|---:|---:|---:|---:|
| Existing / random | 22.96 | 38.84 | 30/107 (28.04%) | 19/74 (25.68%) |
| Existing / response | 22.96 | 38.86 | 43/108 (39.81%) | 26/75 (34.67%) |
| Bounded radio / random | 20.22 | 26.79 | 18/106 (16.98%) | 7/69 (10.14%) |
| Bounded radio / response | 20.22 | 26.79 | 32/106 (30.19%) | 11/70 (15.71%) |

All-world totals include opening catalogue, which retains the old consumer model.
The bounded state covers live releases only. Short-window lead cohorts have only
one indie Top-10 record per arm and cannot resolve national-breakout differences.
No closures occur in this short window. These findings do not justify a default
change or coefficient adjustment to recover the old volume.

The national fit constraint remains material. In the bounded-radio/random world,
anonymous intent among below-threshold observations totals 0.784M. Replacing fit
by one in the same-state arithmetic expression gives 3.759M; this is not a
counterfactual world and removes the complete commercial-fit multiplier, not just
its national component. Median conversion rates are about 0.0004 below the
threshold and 0.0020 above it, with selected-group caveats. Response ranking gives
similar diagnostics. Enthusiast sharing is screened separately with unchanged
radio, interest-loss and purchase-rate coefficients.
