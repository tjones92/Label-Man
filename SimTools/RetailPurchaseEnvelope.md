# Shared retail purchasing envelope

27 October 2026. Implements the user's requested format-composition change.
No decade simulation was started. The retained seed supplies a frozen
calibration substrate, not an independent test of multi-year feedback.

## Behavior

Ordinary gameplay uses `lp-retail-v2`, adding
`--retail-purchase-envelope=1` to the previously adopted release/audience/LP
bundle. Explicit zero restores the previous retail policy; historical headless
commands remain unchanged unless they request the new flag or default profile.

Each region receives one prospective LP purchasing purse. Provisional store
buyers and autonomous rack intentions compete for this purse with capped,
demand-weighted allocation. Unused allowance can serve the other cohort if it
has real demand. Existing quality, genre, awareness, exhaustion, assortment and
physical inventory calculations remain load-bearing; no units are fabricated
to meet an annual observation.

The store cohort is reduced before stock depletion, finance, retirement and
settlement. Rack preferences then route disjoint subsets of those actual
buyers to front-line and budget products. Migration spends rack shelf capacity
but does not spend a second autonomous purchasing allowance. Integer rounding
is separate for migration and autonomous cohorts. Failed shelf fulfillment
leaves a migrated preference in stores. A bargain product still requires a
positive margin. Both rack products retain one shared shelf.

The live Single purse is reduced by 5%. Idle LP capacity no longer supports
Single exports: portable Single capacity is calculated from unused Single
purchasing only, with a 3% Single-capacity import limit. Retained Single local
budgets are exhausted everywhere, so this removes the LP-funded hump in the
frozen state. Demand-starved Single markets can still export genuine spare
Single capacity. Single and LP purchasing do not reclaim unspent club/off-chart
holds or the separate additional LP floor.

## Calibration and scope

The common LP purse is a ratio of the prospective regional live Single purse,
not a copy of national annual sales. The 1969-1970 guide's internally
consistent 1966/1967/1968 retail LP:Single ratios are 133/129, 143/130 and
147/126. Before that measured window, the first ratio is held as a conservative
scenario; the guide's revised early estimates are not spliced into the older
reference series. The authored 1969 endpoint remains 55% LP units; it is not a
historical 1969 observation. The 5% Single correction brings the retained
52-week local series from 134.709M to 127.974M, near the later guide's
129/130/126M retail figures.

Relative autonomous rack weight is 1.3 through 1966, 1.45 in 1967 and 1.6 from
1968 onward. This transition was calibrated on the retained substrate after
comparing constant weights 1, 1.3, 1.6 and 2. It distinguishes migration to
racks from growth in the total LP market. It is not a fresh-seed holdout.

The published model comparison continues to count live-record Singles and
store/front-line/budget LP units. Clubs are separate. Synthetic Major
off-chart Singles are also separately booked; their retained ledger contains
no retail/jukebox split. Therefore matching this reported format share does
not establish an all-industry or complete household-purchasing fit. No
synthetic unit was relabeled, dropped from finance or assigned an invented
outlet scope.

## Frozen production-allocator replay

`RetailPurchaseEnvelopeReplay` reads existing ledgers and calls the actual C#
purse/cohort allocators. It never advances the simulation. Store title cohorts
are proportionally rescaled because the compact decade omitted detailed
per-title store data. Original rack assortment, quality, exhaustion, stock and
future behavior remain fixed. Budget switch intentions are interval-inferred
from fulfilled transfers; low/mid/high reconstructions are reported.

Outputs belong in `SimLogs/retail-envelope-static/`:
`native-frozen-replay.csv`, `policy-annual-comparison.csv`,
`policy-review.json` and the comparison plot. Weight zero in the native CSV
means the production year-dependent schedule, not zero rack preference.

| Complete settlement year | New stores M | New racks M | New Singles M | New LP share | Guide stores/racks M |
|---|---:|---:|---:|---:|---:|
| 1966 | 54.871 | 76.658 | 127.974 | 50.685% | 56 / 77 |
| 1967 | 54.336 | 86.265 | 127.974 | 52.351% | 56 / 87 |
| 1968 | 55.094 | 94.045 | 127.974 | 53.819% | 54 / 93 |
| 1969 | 58.111 | 98.164 | 127.974 | 54.978% | No observed guide target |

The 1966-1968 mean absolute channel percentage error falls from 63.975% to
1.571% in this calibration replay. LP retail growth from 1966 to 1969 is
18.815%, against the retained world's 70.170%. This is not a forecast from a
new world. The budget-switch reconstruction changes annual retail totals by
at most 8,676 units across the interval cases, which does not measure missing
behavioral feedback.

The older 1961-1965 references remain visible and fit worsens: mean absolute
channel error rises from 6.770% to 13.726%. In 1965, the replay gives
54.465M stores and 79.318M racks against the earlier edition's 76M/106M.
These source vintages do not support forcing one smoothly growing trajectory
through every value. No earlier reference was overwritten or retroactively
declared passed. Early fit, inventory accumulation, label cash, release supply
and title survival remain requirements for eventual native validation.

## Save compatibility and verification

Active saves write v36 with `RetailPurchasePolicy = 1`. Policy mismatch is
rejected before mutation. Disabled-policy saves omit the field and retain
their prior save version; use `--retail-purchase-envelope=0` to resume the
previous adopted LP profile. Increment the policy version when changing its
semantics for already published saves.

`--retail-purchase-envelope-check` exercises cohort conservation, scarce
shelves, empty demand, pure transfers, regional-scale arithmetic and save
policy rejection. `--retail-envelope-replay=PREFIX` additionally reads frozen
CSV inputs; it is not a run horizon. `analyze_retail_purchase_envelope.py`
evaluates the native CSV output without launching Godot.

Verification logs are under `SimLogs/retail-envelope-checks-20261027/`.
The implementation builds with zero warnings/errors. Native allocation checks
pass 4,016 assertions. A two-week seed-1001 default-profile check passes
byte-identical save/load, gzip file IO and one resumed release-programme week.
The two-week compact audit reconciles all 14 regional purchasing rows against
store/rack sales and front-line/budget financial units, with zero inventory or
allocation violations. Legacy rack, channel and LP-factor checks pass 50, 38
and 20 assertions; default-profile checks pass 13. The verification manifest
is `SimLogs/retail-envelope-checks-20261027/verification.json`.
No decade run or long feedback simulation is authorized by this document.
