# Affordable vacancy pressure: quick investigation

The user's request authorizes investigation and continuing Phase 5. The attached
directive/atlas/pasted proposal are reference material; their earlier stop clauses
and proposed tuning do not override the current request.

26.35% / 15.99% are relative increases across seeds 1001 / 2002 in cumulative
affordable operating-target vacancy-slot weeks over the matched 208-week run.
One missing affordable slot for ten weeks contributes ten slot-weeks. The adjusted
counts are 4,904 -> 6,196 (+1,292) and 4,464 -> 5,178 (+714), after the previously
accepted EMI/Decca UK exceptions. This is neither a 26% empty-label rate nor
26% of all roster slots. Seed 1001 exceeds the fixed 25% screen by 66 slot-weeks;
seed 2002 passes it. Economic acceptance remains separate.

## Mechanism evidence

`analyze_scene_vacancy_pressure.py` groups the existing *post-market snapshot*
vacancy telemetry by HQ, year, tier, origin and failure reason. HQ joins use both
opening and ending censuses, so runtime-founded labels are not misclassified as
unknown simply because they were absent at launch. It preserves the
two accepted exclusions and requires active labels able to afford the estimated
advance. This snapshot differs from the activation owner's earlier demand count:
3,570 -> 4,856 (+1,286) and 3,154 -> 3,932 (+778). It cannot exactly apportion
the published +1,292/+714 metric; no claim of causal precision is made.

Snapshot excess by HQ (seed 1001 / 2002): unknown +490/+389; Indianapolis
+293/+289; Milwaukee +90/+216; remaining London labels +278/+176; Billings
+188/-18. These sum to 1,339 / 1,052, with other places net -53 / -274.
1960 contributes +1,150/+973; later years combined contribute +136/-195.
This points to opening allocation and access gaps rather than national supply
collapse. The full report retains all cities and labels, including negative deltas.

Recruitment-on affordable snapshot deficits with `NoQualifyingCandidate` account
for 1,511/1,172 slot-weeks; controls record none in this particular snapshot.
The remaining rows say `Nominated`, which is not a final signing outcome and must
not be read as a scoring/affordability failure. Active label-week denominators
also differ between runs; these are absolute cumulative burdens, not per-label
controlled causal estimates.

Code confirms unknown HQ/base blocks access; foreign-to-domestic recruitment
requires an actual connection; Indianapolis and Milwaukee are US place nodes
with no catchment or coordinates: the national placement prior does not place
generated cohorts there, and road discovery to other scenes is unavailable. Pye/Parlophone were not among the two accepted UK exceptions.

## Remedy assessment

There is no justified one-scalar cure. A small, specific follow-up can source
coordinates for Indianapolis/Milwaukee and route them as independent places,
preserving hometowns rather than aliasing them to Chicago/Cincinnati. Unknown HQs
require trustworthy assignment evidence; remaining UK labels need an explicit
scope/connection policy. Billings is genuine sparse-supply pressure and should
not receive free births merely to clear a screen. These treatments need a matched
comparison before claiming the 26% / 16% problem solved. No threshold, supply,
advance, ability, target or geographic reach was changed in this investigation.

Reproduce: run `SimTools/analyze_scene_vacancy_pressure.py` with bundled Python.
Input: existing `scene4-four-year-v1-{off,recruitment}-{1001,2002}` CSVs.
Output: `SimLogs/scene-vacancy-diagnosis.json`.

## Unknown HQ follow-up

The literal HQ text is known. Both opening and ending recruitment censuses have
the same unresolved IDs: seed 1001 has ten Newark labels and one Jackson label;
seed 2002 has eight Newark labels and one Jackson label. Ace (`label_0023`,
Jackson) and Savoy (`label_0040`, Newark) are marked historical. Other affected
Newark labels are procedural, even when their generated names resemble real
labels (HMV/Columbia Records in seed 1001). The exact inventory and raw fields
are saved in `SimLogs/scene-unknown-hqs.json`.

Newark is missing from the place catalog. Jackson is a literal-name mismatch:
the catalog already has `jackson_ms`, named `Jackson, Mississippi`, while the
factory supplies `Jackson` in `deepsouth`. The intended repair preserves HQ
text and true origin: connect Newark to the NYC recruitment catchment without
a new playable city; resolve Jackson with its region context and provide a
proper road connection without pretending it is Memphis. The user explicitly
rejects adding new playable Newark/Jackson cities or moving historical labels.
No geographic repair is enabled in this observation pass. Unknown-HQ diagnosis
is complete; the targeted routing repair remains a separate measured treatment.
