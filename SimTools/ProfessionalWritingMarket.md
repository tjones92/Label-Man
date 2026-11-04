# Finite professional writing market

`--professional-writing-market` enables the next development slice. It implies
writer-conditioned generation and professional standing teams, without enabling
unrelated band-life features. The ordinary-game baseline remains unchanged.
Market worlds save as v37 and require the same switch on resume; enabling the
market on an old save, or silently disabling it on a market save, is rejected
before world mutation. Use a fresh world for this policy.

## Supply and discernment

The existing writers are the workforce. A standing pair shares one capacity slot;
booking either partner books the pair. Solo suppliers also have one slot. Only
active writers can take assignments, and they must remain active through delivery.
There is at most one pending assignment per act and supplier across all labels.

An idle supplier drafts one candidate every four to six weeks, with an opening
phase offset; speculative drafting stops while eight demos are available. A keyed, imperfect
self-assessment shelves weak candidates; those attempts still consume the writing
opportunity. A commissioned assignment takes two or three weeks, based on the
supplier's reliability. It displaces speculative writing and is paid once when
ordered. The composition does not exist until delivery. An unavailable act or
retired supplier cancels delivery; the work fee is not refunded.

Commissioned material is reserved to the assigned act. Existing demos are licensed
separately, with a 12-week first-recording reservation. Competing labels cannot
license the same first recording. Unused holds expire and return to the market;
repeated reads do not extend them. Recording removes a song from the professional
first-recording supply. Once released, ordinary cover routes remain available.
The studio refuses to print an unavailable professional song instead of creating
an anonymous substitute master.

Writer screening and A&R/player demo reads are imperfect. The market read is keyed
by label, song and axis, so rereading does not average away uncertainty. Existing
AI shortlist and programme reads continue to use their own imperfect judgments.
Commissioned songs can disappoint and be left unrecorded. Revision orders and an
explicit player shelving interface remain future work.

## Access and fees

| Arrangement | Rights and cost |
| --- | --- |
| Freelance | Per-song licensing or a paid new assignment; competes for free capacity. |
| Preferred | 12-week term; retainer every four weeks; two-week first refusal on newly drafted demos; 20% assignment discount. |
| Exclusive | 12-week term; larger retainer every four weeks; new capacity and speculative output reserved for that label; assignments still have a delivery time and a 20% service fee. |

Initial retainers are paid at signing; subsequent instalments are charged at weeks
four and eight, never after the term ends. Inactive clients, retiring writers or
insufficient uncommitted cash end the arrangement and release unlicensed priority
material. Existing booked assignments retain their promised delivery rights.
There is no automatic renewal. Labels can retain at most two suppliers, including
one exclusive; at most a quarter of supplier slots can be exclusive worldwide.
These limits are provisional market-design parameters, not historical estimates.

Existing songs, copyright and publishing shares are outside these contracts.
The previous probabilistic professional publishing capture is bypassed under the
market. Explicit copyright acquisition is future work. The market also replaces
the old tier-ranked publisher pitch slices, rock-office hit-list access gate and
anonymous rock-office replenishment; their switches do not add extra capacity or
secret quality priority on top of market contracts.

Fees use public supplier reputation, assignments, pending workload and contractual
access. Initial reputation is seeded from experience and a keyed reputation draw;
Top 40 releases add observed reputation once per record. Fees never read a song's
true hook, melody or craft. The current scenario starts from $100 for new work or
$55 for an existing-song license, multiplied by reputation and demand. Preferred
and exclusive retainers are .75 and 2.5 times the freelance assignment quote every
four weeks. These are gameplay parameters requiring calibration, not quoted
historical prices. Fees are ordinary operating expenses through label accounts;
commissions, licenses and retainers have separate cumulative supplier ledgers.

## Partnerships and player/AI paths

Partnerships are saved by label, act and supplier. Assignments, recordings and
observed hits are distinct. After a hit, an AI label can return to the supplier
for that act, subject to cash, an eight-week assignment interval, unused deliveries
and shared capacity. Established repeat relationships may buy a retainer. These
decisions use actual past outcomes, not hidden future-song quality.

Previous recordings gradually improve assignment understanding. Familiarity only
reduces genre mismatch; an already matching assignment gets no craft or hook bonus.
Neither a hit nor signing a contract increases writer abilities.

The player's repertoire screen shows supplier-specific assignment quotes,
contracts, remembered partnerships and separately priced available demos. Daily
delivery enters the existing repertoire/studio flow. Band Room member-plus-staff
sessions use a real supplier slot, delivery and fee, with the member retaining a
co-writing credit and contributing craft. AI legacy material selection,
shortlists, programme searches and album cuts share the license gate and expense
path. A programme search of an existing demo is a license, not secretly new work.

## Validation and audit

Build with `dotnet build "Label Man.csproj" --no-restore`. Native checks:

```
Godot_console.exe --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --professional-writing-market --professional-writing-market-check
```

For a pending commission and active retainer through the real world/gzip save
path, use `--professional-writing-market-roundtrip --weeks=0` instead of the
check switch. `--weeks=13` without either fixture switch exercises autonomous
production, partnerships and releases before a world round-trip and resumed week.
The runner checks capacity, catalog references, finances and contract exclusivity.

`ChartAuditRunner.tscn` with `--professional-writing-market-audit` and the market
switch adds `*-writing-suppliers.csv`, `*-writing-orders.csv` and
`*-writing-partnerships.csv`. Rows are week snapshots: deduplicate orders by ID
when summing fees, and difference cumulative supplier fee ledgers. Existing
record/act/owner-group audits supply release and concentration outcomes.

Structural checks and short runs establish mechanics, not economic balance or
improved repeat careers. Before changing the working bundle, compare several
seeds and fully followed hit cohorts against stage one, including silent acts,
repeat-career middle, leading-act and owner-group concentration, source mix,
supply shortages, unused deliveries, supplier revenue and label survival.

### Initial verification (2026-11-02)

The build passed without warnings or errors. Native market checks passed with
seeds 1001 and 2002. A 13-week seed-2002 world passed byte-identical save/load and
gzip IO; a forced active preferred retainer and pending order also survived the
whole-world round-trip and resumed week. The structural checks cover first-refusal
expiry, unfunded retainers, services-only exclusivity, unbookable shared capacity,
no early/duplicate delivery, hidden-quality-independent prices, expiring licenses,
observed-hit deduplication, member co-writing and assignment familiarity.

A separate seed-2002, 13-week screen enabled AI shortlists, perceived selection,
slate allocation/capacity and anticipatory material search. It completed with 94
supplier slots, 508 paid orders (417 delivered, 91 pending), and 21 partnerships
with multiple recordings. The order fee totals reconciled to supplier commission
ledgers ($123,840); license fees totaled $29,290. No supplier had more than one
pending assignment. Only three speculative candidates were shelved, and no AI
retainers had yet been signed. Forced checks cover contract mechanics; this short
screen does not establish autonomous contract frequency or career improvements.
The 91 pending slots also show substantial capacity saturation requiring balance
work. Logs, CSVs and the reconciliation summary are under
`SimLogs/professional-writing-market-*` and remain uncommitted scratch evidence.

### Economic and career calibration

The first calibration adds AI-only reorder, delivery-use and operating-cash
guards plus a keyed weekly rotation of repertoire access across acts. Player
booking rules and supplier prices remain unchanged. Eight two-year screens show
lower booking pressure and commission spending, but leading-act concentration
remains unresolved; the market remains experimental. See
[ProfessionalWritingMarketCalibration.md](ProfessionalWritingMarketCalibration.md)
for the matched two-seed experiment, complete career-cohort definitions and checks.

The subsequent controlled workflow test adds independent
`--writing-market-planned-assignments` and `--writing-market-delivery-offers`
switches. See [ProfessionalWritingWorkflowExperiment.md](ProfessionalWritingWorkflowExperiment.md)
for policy boundaries, persistence requirements and the four-arm test.
The corrected two-seed test improves intended-client use of paid commissions but
increases leading-act concentration under the combined policy. Both workflow
switches remain opt-in; plain-market defaults are unchanged.
