# Early-Game Label Man Directive V2 — release preparation, working capital, and the trunk

Date: 2026-10-14.
Status: **revised directive only — no implementation authorized by this document alone.**
Supersedes `SimTools/EarlyGameLabelManDirective.md` as the proposed design. Keep that file as the
original for comparison. Its branch/base metadata is provenance, not a requirement to switch branches.

This revision incorporates the design/code review of the original directive. The aim remains:

1. Give the founder useful choices between cutting a master or acetate and receiving the pressing.
2. Prevent avoidable opening cash traps and make remaining risks legible.
3. Connect trunk work to actual accounts, rooms, audiences, and local music scenes.

The player should feel like a record-business founder preparing a release, not someone completing a
daily checklist for guaranteed rewards. A sensible opening should have a viable route to its first
release. Unsuccessful records, exhausted borrowing, and unsustainable spending can still end a label.

Inputs: the original directive; the cited Opus playtest and action backlog; a spot-check of the current
player, studio, scene-room, and royalty code; and the historical sources in §12. The opening simulation
matrix has **not** been run for this revision. Numerical claims below are code-derived inputs or
explicit hypotheses, not measured outcomes.

---

## §0 Charter and invariants

1. **Player-originated changes only.** AI-only runs never instantiate the player and must stay
   byte-identical. Prove this with the established 52-week, seed-1001 probe using
   `--enable-genre-market-v2 --enable-artist-population-lifecycle`, comparing every CSV in SimLogs.
   Player bookings, placements, and leases may intentionally affect counterparties in player-present
   games; document and test those effects rather than calling them inert.
2. **Every new activity is label work.** Promotion, booking, producing demos, placing songs, and
   negotiating backing belong inside the record trade. Unrelated employment remains outside scope.
   This is a game-fantasy choice, not a claim that historical founders never held day jobs.
3. **No automatic rescue money.** Loans carry obligations; deals transfer rights or future income;
   services require customers, resources, and time. Opportunity cost is sufficient for legitimate
   service work: a demo need not surrender a master just because it earns cash now. Access to an ask
   is not a guarantee that someone accepts it.
4. **Use existing evidence before adding another meter.** Read regional airplay/awareness, station
   playlists, room appearances and history, scene placement, and account relationships. If a local
   following cannot be derived adequately, add bounded player-side state with an explicit purpose,
   provenance, decay, and rules preventing double-counting existing awareness.
5. **Preparation changes later opportunities.** A kept DJ appointment, an advance dealer order,
   and a completed live engagement should matter when the record becomes deliverable. Their value
   should come from the opportunity they create, not an automatic reward for spending an hour.
6. **There need not be profitable work every day.** Quiet days and waiting are legitimate. Provide
   a small set of meaningful plans and convenient skipping, not a quota of state-changing clicks.
7. **Historical practice and authored economics are different claims.** Cite evidence for the kind
   of transaction. Mark invented rates, quantities, repayment schedules, audience turnover, and
   acceptance probabilities as authored estimates. Founder anecdotes do not prove universal access.
8. **Warnings inform; they do not insure success.** Show committed costs, likely cash dates, and
   completion costs before spending. Keep commercial uncertainty and the possibility of failure.

---

## §1 Audit: costs, liquidity, and failure are separate questions

### 1.1 Opening arithmetic to anchor the probe

| Line item | Code seam | New York | Detroit | Billings |
|---|---|---|---|---|
| Ex-Musician founding capital | `FoundingArchetypeData` | $800 | $800 | $800 |
| Other founding capital | `FoundingArchetypeData` | Pawn $1,400; Insider $900; read all profiles from code | same profiles | same profiles |
| Monthly overhead | `CityProfiles.MonthlyOverhead` | $175 | $80 | $53 |
| Signing reserve | `AILabel.CanAffordToSign` | 2× overhead ($350) | $160 | $106 |
| Credit floor | `PlayerDesk.CreditFloor` | −$525 | −$240 | −$159 |
| Budget room, 3h reference | `SessionCost` | about $60 | about $48 | about $36 |
| First pressing, 500 | `PressingCost` | $128 | $128 | $128 |
| Acetate | `CutAcetate` | $20 | $20 | $20 |
| Plant lead time | current plant stages | 14–30 days | 14–30 days | 14–30 days |

The pressing formula is $38 lacquer setup + $20 shipping + $0.14/copy. A repress omits the setup
and permits a smaller minimum; use its actual quantity and eligibility rather than the first-run
price. Named-room quotes, engineers, players, tape, travel, and finishing can change session costs.
The budget-room reference above is not a complete quote for the current studio experience.

New York overhead rose from the older $75 baseline to $175. Optional studio expenditures create more
ways to consume working capital. That motivates measurement; it does not establish a cash-lock rate.
The original's $250–320 cheapest-record estimate depends on signing terms and the actual studio
route, and must be confirmed rather than used as a fixture.

### 1.2 Revised diagnostic taxonomy

**L1. Pressing liquidity bottleneck.** Cash cannot fund the cheapest currently legal pressing or
repress; there is no sellable stock, pressing in flight, collectible account balance, or wholesale
receivable. Report the record, legal quantity, cost, and unmet prerequisite explicitly. If no single
is assembled, diagnose an unfunded completion path separately instead of comparing against an empty
set of pressing prices.

This condition is **not proof of a terminal lock**. Inspect independently:
- existing `SellMaster` / `LeaseMaster` eligibility on previously released titles;
- `RequestPlantCredit` eligibility from surviving open demand;
- distribution-deal eligibility and whether it actually creates timely cash;
- road bookings already producing income and additional bookings that remain legal;
- after implementation, backer capacity, accepted service work, and reachable master buyers.

**L1b. No currently executable recovery path.** None of those routes can yield usable cash before
closure, given actual fees, travel, due dates, commitments, and prerequisites. A merely available
negotiation is a speculative route, not assured recovery. Record why each route is unavailable or
uncertain. Call a state terminal only after checking the actual rules and horizon.

The original statement that a New York L1 state means certain death in about four months is withdrawn.
`RunRoadBookings` pays $22/week **per active residency booking**, not per label. Two booked acts can
produce roughly $190/month before other expenses, exceeding $175 overhead. Whether a particular
label can maintain those engagements is a separate question for the probe. This current income is
also scheduled for a fidelity correction (§9 F5), so it must not become a permanent balancing crutch.

**L2. Plant-wait interval.** The first pressing takes 14–30 days. There is no revenue from selling
that physical stock before arrival. Existing catalog sales or live-booking income may still occur.
Measure useful preparation and avoidable repetition; do not equate every quiet day with a defect.

**L3. Stockout and replenishment gap.** A sellout followed by a repress wait interrupts delivery.
Measure stockout days, lost orders, call expiries, and radio consequences. Demand can persist while
cash is unavailable; a stockout alone is not evidence that the record or label has failed.

**L4. Monthly solvency reckoning.** `OnMonthChanged` checks after overhead has been deducted. Three
consecutive negative checks, or crossing the credit floor at the reckoning, closes the label. Explain
this timing consistently; existing UI language sometimes calls it month-end. Any alternative reset
rule is a separate balance change (§9 F7).

### 1.3 Existing work to preserve

Do not redo adaptive NEXT UP, pressing/studio affordability warnings, While You Wait, low-stock
warnings, the red-ink chip, arrival announcements, arrival-based release defaults, $0 campaign
handling, interruptible skips, or existing negotiation floors. Extend these seams.

`PlayerDesk.OfferPlanning.cs` already exposes signing reserve, retained appointment fees, and a budget
session estimate. Extend it to the release completion path rather than claiming signing has no
financial guidance today. Its current estimate explicitly excludes pressing and distribution.

### 1.4 Existing acetate routes

In-person `DeliverAcetateToStation` services the record and can generate dealer preorders on a strong
hook. `RolodexApproach.PlayTheAcetate` gives a DJ a listening opportunity without servicing the title.
Keep their distinction. Courtesy conversations should work before release and, where applicable,
without an acetate; they should not become disguised successful record pitches.

---

## §2 Historical grounding and its limits

| Practice | Historical anchor | Design implication | Limits |
|---|---|---|---|
| Family backing | Gordy's $800 Ber-Berry Co-op loan | A personal pitch and real repayment obligation | Does not establish a second $800 loan for every founder |
| Pre-release master leasing | Gordy's Miracles recordings leased to End | An independent producer can shop recorded work | Acceptance, advance, and terms are uncertain |
| Regional success followed by pickup | Tamla's “Come to Me,” then United Artists | Regional evidence can attract a larger offer | The deal also signed Marv Johnson to UA; artist retention is not universal |
| Partner backing and collateral | Axton mortgaged her home to support Satellite/Stax | Capital can involve personal assets and shared ownership | A $1,500–3,000 game offer is an authored range |
| Retail alongside a label | Satellite Record Shop; Carter/Bracken and other shop-owner founders | Cash flow, a home counter, and market listening | Requires inventory, staffing, and access to supply |
| Custom recording | Memphis Recording Service; Rayber demo work | Paid production can bring cash and expose talent | Owning a service studio differs from renting another owner's room |
| Booking alongside recording | Robey's Buffalo Booking Agency | A founder can earn for booking engagements | Does not prove a universal 10% commission or ownership of gig wages |
| Song placement | Gordy's Jackie Wilson songwriting | Publishing can yield delayed income from other acts' recordings | The game's advances and settlement timing need separate assumptions |
| Dealer and DJ promotion | Chess road work and small-label regional promotion | Direct contact and hand delivery are central activities | Does not establish that every room permits a retail table |

The strongest directly checked accounts for this revision are the Stax Museum history and Classic
Motown's accounts of the Miracles lease and Marv Johnson transaction (§12). Other original references
remain research leads; their exact amounts and terms have not all been independently verified.

The Stax Museum describes the shop paying rent and helping Axton hear what sold. It also describes
Jim Stewart keeping his bank job into 1965. Excluding unrelated jobs is therefore a deliberate scope
choice. Do not use “all founders lived only on label work” as the historical justification.

Keep ordinary unsecured bank-startup loans, loan sharks, gambling, and furniture liquidation outside
this feature. The supplied research does not justify a routine bank rescue for an unknown label;
that is narrower than claiming no founder ever obtained bank or collateral-backed finance.

Thin evidence remains: exact distributor payment terms, independent wholesale prices, plant-credit
terms, record-hop economics, routine venue retail permissions, and audience turnover. Keep the
existing dealer ladder unless separate research and calibration justify changing it.

---

## §3 Design shape: a few release plans, with different consequences

| Plan | Cash timing | Release-day value | Main tradeoff |
|---|---|---|---|
| Arrange DJ listening and delivery | no immediate cash | a relevant contact and kept appointment | time, access, and a possible rejection |
| Seek dealer commitments | COD after stock arrives and is delivered | reachable orders to fulfill | time, limited quantity, cancellation risk |
| Book the act before an audience | possible commission after the engagement | live response and a local audience | availability, fatigue, booking obligations |
| Seek backing | after agreement | funded completion or a larger buffer | repayment or ownership/income concessions |
| Shop the master | after acceptance and settlement | another label may work the record | master rights and possibly artist/follow-up rights |
| Produce paid demos | deposit and completion settlement | cash and scouting knowledge | clients, production costs, and calendar time |
| Place a song | advance, if agreed; later royalties | catalog income | a competing recording and delayed settlement |

An advance dealer order is a reservation, not prepaid consumer demand. It improves cash **after
delivery** and does not shorten the plant's physical lead time. Commission and service income need
not trade away future recording rights; they must pay for actual work and resources.

The first implementation should concentrate on completion-cost guidance and the first three plans.
Financing adds options, not a guarantee that every business can be saved.

---

## §4 Working-capital guidance and backers

### 4.1 Release completion preflight — ship early

Before a material spend, show the cash left and the remaining minimum credible route to a first
deliverable release: unfinished studio work, mandatory extras, acetate if the chosen plan needs it,
pressing, and necessary travel/delivery. Show overhead dates falling before likely arrival. Separate
paid pipeline costs, binding obligations, cancellable appointments, and estimates; do not reserve
cash twice or treat a retained quote as a debt.

Extend existing signing guidance and warnings on studio work, hired extras, appointments, pressing,
and travel. If no assembled single exists yet, use a completion forecast rather than waiting until
the first pressing price is known.

For an assembled title, use the cheapest legal run and actual repress status. When appropriate, say:
“After this, you cannot afford to press any of the singles you have ready.” When the problem is
earlier in production, say which completion stage is unfunded. Existing stock, receivables, and
pressings in flight should qualify the warning. Keep the player free to accept the risk.

### 4.2 Cash-options card

Use the original WAY OUT concept as a navigation card, labelled **CASH OPTIONS** or equivalent.
Preserve the solvency count and its timing; do not replace the red-ink rule with a list of actions.

List current options with prerequisites, costs, expected cash date, and certainty:
- sell or deliver accessible stock;
- collect account balances or due receivables;
- artist buy-in where currently eligible;
- request remaining backer capacity;
- approach an interested or reachable master buyer;
- fulfill accepted demo work or inquire about prospective clients;
- earn a booking commission on eligible engagements.

Distinguish an accepted payable order from a speculative pitch. A route that needs unaffordable gas,
an unavailable room, or money arriving after closure is not a live immediate rescue. When the card
has no executable route, explain the blockage honestly. **Remove the invariant that it can never be
empty.** The card reveals the economy; it does not manufacture demand or alter the closure rules.

### 4.3 One early backer agreement

Use one initial backer agreement per label. Repeat financing and top-ups are outside V2; remove the
original's contradictory references to second asks. An unsuccessful approach must not allow repeated
rerolls without changed evidence or terms.

Suggested archetype sources:
- Ex-Musician: a family fund or relative with the means to help;
- Pawn-Shop Owner: a specified collateral-backed personal/business arrangement;
- Promo Man: a club owner or established trade contact;
- Trade Insider: a named private backer introduced by a trade contact.

The latter three are authored analogues, not directly demonstrated by the Gordy loan. Specify who
provides the capital; “knows a guy” is not sufficient provenance and must not silently introduce a
loan shark or an unsupported distributor advance.

Before quoting, distinguish this backer from the source of founding capital. If founding money
already represents that family's loan, reconcile it or identify a separate source; do not count the
same historical $800 twice.

The ask takes a meeting (reference: 2h at home), with approach, objection, and grounded answer. A
signed act, completed tape/acetate, realistic completion budget, and the founder's existing standing
are relevant. The backer does not inspect the true hidden hook. Terms may be altered rather than
reducing every objection to a new acceptance roll.

Reference principal: $300–800, capped by the backer's resources/collateral and founding-capital
reference. Office rent can inform the player's need; it does not create the backer's lending capacity.
Reference interest: around 6% annually. The documented family-loan rate does not establish the game's
repayment schedule. Author an explicit schedule, including whether interest uses outstanding
principal, when the first payment falls, and whether the twelve-month term starts at that payment.
The original suggestion of repayments beginning in month 3 remains a calibration candidate.

Book debt principal as financing, not operating revenue; principal repayment is a cash obligation,
not an operating expense. Interest has its own accounting. Apply due payments in a defined order
before the monthly solvency check and test that order.

A missed payment creates arrears and loss of standing, not a separate automatic game over. Any
windfall collection must use explicit terms, deduct once, and leave the outstanding debt correct.
For family backing, use a contextual paper line without inventing an unmodelled morale penalty.
Collateral consequences must be specified for collateral-backed sources; they are not universal.

Save agreement/source identity, principal, rate convention, term and due dates, repayment ledger,
arrears, standing, and any collateral. A recomputed loan balance must agree with the saved ledger.

### 4.4 Partner backing — later and optional

After the first release, a relative, shop owner, or club owner may offer capital. Reference ranges
of $1,500–3,000 and a 25–50% stake are authored estimates and need evidence/asset-based pricing.

Choose and name the contract correctly:
- **Equity:** ownership stake, its decision rights, distribution policy, and buyout valuation.
- **Profit participation:** a contractual share of a defined profit base, without implied ownership.

Do not call a perpetual payment from positive monthly net “equity” unless ownership is modelled.
Specify treatment of losses, retained earnings, financing proceeds, principal payments, prior
deficits, and distributions. A profitable month can still be short of cash: distributions must not
consume binding obligations or make the label insolvent. One partner agreement is sufficient for
this scope. This feature is not required to solve the first plant wait.

---

## §5 Shop the master: access, rights, and settlement

### 5.1 Existing late exits and an earlier producer route

Today `MasterDealEligible` requires a released title with regional radio and one-stop knowledge.
The current cash price uses `lifetimeLabelNet` with a floor. An unreleased master cannot simply use
that verb for the floor price because it fails the eligibility gate.

Add an earlier approach for completed recorded work, using a real AI label with genre fit and
capacity. Offices and buyers come from actual counterparties, not a compulsory four-city list.
Use the existing rivalry/courtship seams where appropriate. Interest can read quality through the
counterparty's taste and uncertainty; never expose the hidden quality score in the answer.

Access routes can include a known intermediary, posted tape/acetate, a preliminary call, or a visit.
Charge postage, duplication, travel, accommodation if applicable, and appointment time. A visit is
not guaranteed to secure an audition. Existing driving adjacency and daylight restrictions must be
respected; do not offer an impossible cross-country rescue trip to a cash-poor founder.

Prevent repeated quality-testing or acceptance rerolls on unchanged material. A pass can stand until
new material, credible regional evidence, changed personnel, or materially revised terms.

### 5.2 Contracts, not just a cash button

Reference pre-release lease advance: $150–600, explicitly provisional. A sale can pay more because
it transfers more rights; it need not always be offered. Define each offered contract's:
- master/title and exclusive or nonexclusive rights;
- territory, term, reversion, and any release obligation/deadline;
- advance, payment date, royalty base and rate, recoupment, settlement period, and returns treatment;
- existing stock, pressings in flight, player sell-off permissions, and re-servicing;
- artist, management/production, follow-up, option, and first-refusal rights, if any;
- publishing rights, kept separate unless specifically included.

“2–4% of units” is not a complete royalty formula. If retaining that authored range, specify a
percentage of a monetary base such as defined net receipts; alternatively quote cents per payable
unit. An advance against royalties is recouped before additional royalty cash is paid. A separate
nonrecoupable fee must be named as such.

**Artist retention is a deal variable.** A master-only lease may leave the act on the player's roster.
Another buyer may require signing the artist or obtaining follow-up rights. Do not cite Gordy's UA
deal as evidence that the act always remains: Classic Motown describes Johnson signing to UA and
Gordy receiving a $3,000 advance against royalties. Retaining production or publishing can still
benefit the founder even when the recording relationship changes.

Show rights surrendered and estimated cash timing before acceptance. Subsequent statements show
reported payable units/receipts, recoupment, and money actually received. Do not turn the historically
small Miracles check into a rule that every leased successful record pays almost nothing, or grant
perfect knowledge of the buyer's internal profit.

### 5.3 Regional pickup

A regional breakout can trigger an inbound approach through `InboundCall`. Price it from regional
evidence and the rights requested, not only lifetime label net. A stronger offer is likely, not
guaranteed. Merge earlier shopping and later pickup with the existing master-deal state machine.

### 5.4 Player-present integration

Use one explicit, tagged buyer-release path (`leasedFromPlayer` or equivalent), with a contract ID.
Specify who funds pressing/promotion, whether the buyer actually releases, when the player listing
becomes unavailable, and what happens at expiry or failure to meet a release obligation. Avoid
duplicate chart entries, double sales, and an automatic second release in competitor loops.

AI-only hashes remain identical. Player-present tests additionally verify buyer cash flows, artist
contracts, recoupment, chart ownership, stock rights, royalty settlement, and reversion.

---

## §6 Groundwork: meaningful release preparation

### 6.1 DJ courtesy conversations and appointments

Replace the no-record HANG UP branch with conversations that make sense for the contact's state:
- **Introduce the label:** establish familiarity and learn whether this is a relevant contact.
- **Ask what is moving:** hear a limited account of the DJ's own station and current preferences.
- **Arrange a listen or delivery:** negotiate an available date/hour, not guaranteed airplay.

Reference duration: 10–20 minutes using minute-aware time controls. A weekly contact limit can prevent
harassment, but it does not grant a weekly rapport reward. Introduction gains occur once and are
bounded. Later gains require substance: keeping an appointment, following up with appropriate
material, providing useful information, or established professional reliability. Repeating an
unchanged question can yield no new information or gain; excessive contact can be unwelcome.

Use actual playlist state to ground dialogue, but expose an imperfect, situated read. A DJ need not
recite exact top-two genre rankings or reliably name a disliked genre every week. Note source and
date, stale information sensibly, and feed the existing studio Moment evidence without revealing
hidden simulation values. STREET/EAR interpretations belong in contextual text/tooltips.

Appointments use real availability and existing callback seams. A kept delivery improves access and
reliability; the record still has to persuade the DJ. The player can miss the appointment, the DJ can
be unavailable, and a reschedule must have defined effects.

### 6.2 Dealer advance orders from an acetate

At Shop and Op accounts, offer a listening pitch using an acetate and the existing daily-approach
constraint. Reference duration: about 1h, with actual account/travel time respected.

Acceptance uses the recorded work, account taste and nerve, relationship, and existing access split.
`PitchEarPull` and `EarForRecord` currently look up released runtime records; implement a shared
appeal evaluation that can also read an assembled unreleased single, rather than receiving their
neutral fallback on every acetate pitch.

An accepted order reserves a limited quantity, **COD when delivered**. Use existing small shop/op
quantity bands as calibration starting points, not a stranger's guaranteed minimum. A rejection
persists until relevant evidence or terms change. Direct pitches and DJ-generated orders share one
order ledger; do not duplicate demand or exceed the same account's realistic capacity.

Specify promised delivery window, cancellation, delayed pressing, and expiry. Existing DJ preorders
already allow time beyond expected arrival; preserve that protection and define what happens if
arrival moves. Delays may lose an order or harm reliability, but should not erase every commitment
merely because the factory took the allowed lead time.

The release plan shows reserved copies, accessible buyers, delivery hours/costs, and expected COD.
Orders are not cash, retail sell-through, public buzz, or a guarantee of plant credit.

### 6.3 Pitch the act to a real booker

Use `SceneRoomProfile.ContactId`, booker introductions, existing room history, and actual room bills.
An accepted pitch creates a dated engagement or standing arrangement with defined nights and terms.
Respect the existing calendar, genre/access rules, member availability, shared players, travel,
rehearsals, studio commitments, and road fatigue. Do not displace every existing bill automatically.

Reference pitch cost: 1h during booker availability. Acceptance reads live ability, room fit,
relationship, and credible attendance prospects. Scheduling a show creates an opportunity; only
completed performances contribute audience response and performed-room history.

Prefer existing appearances/history and awareness for local audience effects. If a new city-following
value is needed, make it bounded and auditable by (artistId, cityId), with decay and diminishing
returns from repeat listeners. Attendance is exposure, not an equal number of new fans. Do not count
the same response again as independent buzz, following, and dealer demand lifts.

Payoff can include appropriate local sales appeal, permitted table sales on a relevant night (§9 F1),
and advocacy when a DJ actually attends or MCs. Use the existing record-hop seam for the last effect;
do not assume a DJ is present at every engagement.

Gig money belongs to the performers. A label earns a booking commission only for work it actually
booked under agreed terms, collected after the engagement settles. The 10% reference is authored,
not a universal documented Robey rate. Define the commission base and deduct it once from the act's
gross; do not create a second copy of the gig fee.

### 6.4 Acceptance for groundwork

Remove the ≥70%-of-hours-changing-state requirement. On scripted New York and Detroit openings,
seeds 1001/1002, one act and one single, demonstrate distinct feasible preparation plans:
- dealer-first: seek realistic advance orders and arrange delivery;
- radio-first: identify a suitable contact and arrange a listening/delivery opportunity;
- live-first: arrange a feasible engagement that can produce audience evidence.

A competent plan should produce useful opportunities in suitable fixtures; not every account or
poor record must accept. Measure accepted commitments, completed engagements, kept appointments,
cash timing after delivery, costs, repeated low-value actions, and missed obligations. Include
rejections and unsuitable material. Compare a prepared release with an otherwise comparable waiting
policy, using matched fixtures without assuming unchanged player-present world state.

Once available useful work is done, repeating introductions should not be optimal. The player should
be able to skip to a relevant appointment, factory update, or arrival without losing visibility of
commitments. Quiet time is not counted as a failure merely because no save field changed.

---

## §7 Services and song placement

### 7.1 Paid demos: a producer service with a real cost structure

Clients come from actual unsigned local acts with suitable resources, intent, and availability. Do
not invent clients to rescue a cash-poor label or assume every unsigned act wants a paid demo.

Specify the service model. The current player generally hires outside rooms; it does not acquire a
Memphis Recording Service merely by signing its first act. This phase uses **paid production in a
hired room**, unless a separate owned-studio feature already establishes facilities. Demand follows
the player's reputation, contacts, offered service, and clients' alternatives. The hired room's
renown affects access/cost and can describe the offer, but is not the player's business reputation.

Quote fee, deliverable, deposit, date, cancellation/refund terms, room booking, engineer/players,
media and duplication, and final settlement. Reference gross fees of $35–100 are provisional; check
them against actual quotes and all required costs before assuming a positive margin. Use named-room
pricing/access/calendar machinery where active, not only the legacy `StudioHourlyRate` estimate.

For a cash-poor founder, an accepted client deposit must cover the agreed third-party commitments
before booking. Do not require the label to advance an unaffordable session and then list it as an
immediate cash option. A deposit remains a delivery obligation, not spendable profit; cancellation
or failure must settle costs and refunds correctly. Optional custom pressing is separately funded
by the client and requires a supported legal quantity, not a fictitious cheap first run.

The client receives a specified demo/master/copy for the agreed use; recording work does not
automatically transfer artist or publishing rights to the label. The player hears the performance
and gains a justified scouting read. Signing remains a separate negotiation.

Demand, service price, supplier cost, and time determine earnings. Compare measured monthly margins
with 30–60% of New York overhead as an initial **balance diagnostic**, not a fee formula or required
income. Include zero-client months, low reputation, and expensive-room cases. If demos dominate
record production, adjust grounded demand/cost/access assumptions rather than applying a magic
income ceiling. They need not be available to every opening or rescue every lock.

### 7.2 Song placement

When the player controls relevant publishing, approach a real publisher/plugger or suitable label
through the master-shopping contact routes. An AI act can accept and record the composition, subject
to its rights, repertoire, production costs, and schedule.

A $25–100 placement advance is an authored candidate, not guaranteed money for submitting a song.
Define advance recoupment, publishing/writer shares, units eligible for mechanicals, payment periods,
and any performance-income routing. Keep song rights separate from ownership of a master recording.
If the competing cover gains traction first, use existing cover-threat consequences.

`MechanicalRoyaltyService` currently describes player-selling paths; do not assume an AI cover will
already credit a player publisher through it. Add a player-originated settlement seam that respects
existing AI composition costs, avoids a duplicate charge, and routes payer/recipient amounts
correctly. Inject placements explicitly without changing AI-only material-choice draws.

---

## §8 Storefront: defer, retain as an optional later business

The historical case is strong. The Satellite shop supplied cash flow and market knowledge. That does
not make a shop a small first-month escape: stock, supplier terms, staffing, and demand are a whole
additional business. **Defer beyond the core early-game work; author approval is required before
implementation.**

If separately approved:
- rent and fit out an actual counter with a funded inventory/supply route;
- sell other labels' records with real purchasing costs, stock risk, and replenishment;
- sell eligible own records at the appropriate retail price;
- derive demand from local buyers and competition, not office overhead;
- require player staffing time or an affordable clerk, with explicit hours and payroll;
- produce sourced, dated WHAT MOVED knowledge from actual sales, not a perfect market dashboard.

The original +$40–90/month counter rent and $25–35/week clerk estimates remain research/calibration
candidates. A storefront is not required for the opening acceptance gate.

---

## §9 Trunk fidelity and interface

Preserve named accounts, buyer nerve/taste, existing segregation/access modelling, COD/consignment,
calls, runners, one-stops, returns, and the dealer ladder. Historical confidence in a practice does
not validate every current probability or price; calibrate rather than declaring the whole model
period-true.

### F1. Real rooms, dated bills, and permission to sell — high priority

Project potential venue accounts from `SceneRoomCatalog` instead of only synthetic church/hop tables.
Only performance rooms with a relevant retail opportunity qualify. `CommunityHall` may support a
church/community occasion, but not every hall hosts one every night.

An open room and a crowd do **not** automatically grant table access. Specify booker/promoter
permission, whether the act's engagement includes selling, staffing, table hours, and any fee or
room share. A paid permission request is not guaranteed profitable access.

Sales read the actual open night, performed bill, attendance, audience fit, whether the player's act
performed, and stock/title rights. Settle at a defined point relative to bill resolution; do not
award sales from a scheduled show that later cancels. Time spent staffing limits other player work.

Keep a defensible per-night ceiling. For old saves, map a synthetic stop to a compatible room in the
same city/community where possible; do not infer retail permission from identity alone. Otherwise
retire it, return its inventory without loss or duplication, and settle/preserve its outstanding
balances and calls explicitly. Never teleport old inventory to an unrelated city.

### F2. Repeat audiences saturate — high priority

Use an audience/title purchase pool with diminishing demand from repeat listeners. Attendance × an
interest share is a starting estimate, not a fresh set of buyers every visit. Turnover can depend on
dated bills, changing audiences, visitors, and room kind. Prevent farming the same underlying local
audience by cycling between nearby rooms or accounts. A bounded shared-overlap estimate is enough;
individual simulated customers are not required.

Busy nights can outperform quiet ones because attendance and audience turnover differ, not because
Friday automatically grants a multiplier. A fifth visit to the same audience should often sell less.

Measure resulting monthly net contribution after stock, commission/permission costs, and time.
The original 1–1.5× mid-size-city overhead is a **diagnostic reference**, not a guaranteed lifeline,
target payout, or formula tied to office rent. Table work must not become the dominant endless route.

### F3. Local taste and following — medium priority

Share one record-appeal evaluation across pitches, order sizing, and sell-through, including
unreleased acetate evaluation (§6.2). A bounded local taste contribution may read
`SceneCityPlacement`, but scene genre share is a proxy for buyer preference, not measured consumer
demand. Account-specific taste and audience composition still matter.

Use following derived from §6.3 with no duplicate awareness reward. The original maximum ±0.08
contributions are provisional sensitivity bounds: specify whether the cap applies separately or
jointly and test the combined lift. Use stable sparse-city fallbacks so a weak national denominator
does not create an extreme local bonus.

### F4. Distributor channel shift — defer

Once a regional line is placed, an account may prefer ordering through the named distributor.
Keep direct selling as an accessible opening activity. Any wholesale route must specify price,
receivable timing, stock ownership, returns, and exclusivity where relevant; a placed line does not
instantly force every dealer into the same arrangement. Research the breadth of this practice
before treating hub-city direct selling as categorically impossible.

### F5. Gig income belongs to performers — medium priority, coupled rollout

Replace the flat per-act $22/week residency and $30/week tour label inflows with engagement-based
performer pay. The label receives an agreed booking commission only on engagements it booked.
Keep `BookRoad` as a road-load control, but scheduling road hours is not itself proof of a paid gig.

Use realized scene fees and `MemberLiveIncome` consistently. That service already combines realized
income with estimated unrealized live hours: do not add the same engagement again, take commission
twice, or convert a gross gig fee directly into a member's saved wealth without existing routing.

Ship after cash guidance, groundwork, backing, and demo economics are validated. Re-run opening
policies with the old flat inflow removed. Investigate new locks and ensure sensible completion
policies remain viable; do not compensate with guaranteed demo clients or excessive commissions.

### F6. Work the route — useful convenience, later

Offer an account plan with chosen actions and an hour budget, using the same functions and costs.
Preview travel/visit time, expected stock, and important commitments. Stop for changed prerequisites,
meaningful offers, warnings, or decisions requiring the player. Automation must not choose contract
concessions, reroll pitches, or bypass daily limits. It need not wait for the venue rebuild.

### F7. Solvency wording first; recovery-rule change separately

Explain the existing rule at the count: “checked on the 1st, after monthly bills.” Align tooltips,
books, and closure messages with the actual event order.

Keep the existing reset rule in the core implementation. The original proposal to reset after ≥15
days in the black during the prior month is **deferred as an explicit balance experiment**. It can
grant renewed grace even while the latest overhead leaves cash negative, so it does soften closure.
If approved later, define the daily measurement, save state, exact reset/increment order, zero-cash
boundary, and interaction with the credit floor. Compare failures and exploit policies separately
instead of presenting it as only a wording correction.

---

## §10 Phase 0: establish the baseline and recovery classifications

Build `SimTools/OpeningCashLockRunner`: headless, player present, scripted policies. It reports only;
it must not patch the economy, invent clients, waive access, or force negotiations to succeed.

Initial matrix: four archetypes × five towns (NYC, LA, Detroit, Memphis, Billings) × seeds 1001/1002,
with these policy families:
- **Frugal completion:** one suitable club act, affordable room, one single, a legal first run,
  appropriate selling; completes the release before discretionary expansion.
- **Eager expansion:** two acts, a mid room, two singles, with the exact expenditure ordering logged.
- **Playtest replay:** a $350 advance and two sessions where the current rules permit them; report
  divergences from the old playtest rather than forcing an obsolete sequence.
- **Studio extras:** quoted player/engineer extras on a viable current studio route.

Anchor costs, quantities, and eligibility to constants and live quotes. Explicit policy choices such
as the historical replay's $350 advance can be scenario inputs, not hidden substitutes for code
prices. Policies must obey real scheduling, material, signing, travel, and room-access rules.

Also include targeted fixtures for two residency bookings, no live income, existing master-deal
eligibility during a stockout, surviving plant-credit demand, poor material, rejected asks,
unaffordable delivery/travel, delayed arrival, exhausted borrowing, and an approaching overhead date.

Report per run:
- first L1 day, minimum completion/pressing cost, and cash shortfall;
- recovery routes classified as executable/accepted, speculative, too late, or blocked;
- actual recoveries, first no-executable-route interval, and closure date/reason if reached;
- plant-wait preparation plans, useful commitments, repeated actions with no new value, and costs;
- stockout days, expired/lost orders, and deferred deliveries;
- cash and obligations at days 30/60/90, identifying financing separately from operating income;
- accepted orders, kept appointments, completed shows, and cash after first deliveries.

Initial hypotheses, to confirm or refute: eager NYC/LA openings are more exposed to an unfunded
completion path; sensible frugal openings can fund a first release across the selected towns. A
frugal opening can still fail later because buyers reject the record. Do not turn long-term survival
of all material into an opening guarantee.

The two seeds are smoke/exploratory coverage, not enough to support a reliable “lock rate halves”
claim. If estimating incidence, expand and declare the seed sample and scenario distribution, and
report paired before/after counts and uncertainty. Do not silently change policies to flatter a phase.

Acceptance:
- baseline numbers and recovered/nonrecovered cases are recorded before economy changes;
- suitable frugal fixtures fund and deliver a first release without forced success or rescue money;
- guidance reveals avoidable completion shortfalls before the relevant spend;
- groundwork creates distinct useful opportunities without rewarding repetitive courtesy farming;
- financing/services improve measured options where available, with actual cost and timing;
- no UI option falsely presents an unaffordable, rejected, or too-late route as assured recovery;
- genuinely exhausted businesses can have no executable cash option and can fail visibly.

Re-run after each phase. Keep AI-only determinism separate from player-present outcome comparisons.

---

## §11 Build order and gates

| Phase | Content | Depends on | Gate |
|---|---|---|---|
| 0 | Lock/completion probe and baseline (§10) | existing player systems | classifications and numbers recorded; no economy changes |
| 1A | Release completion preflight, cash-options navigation, solvency wording (§4.1–4.2, F7 wording) | Phase 0 | correct quotes, prerequisites, due dates, and event order |
| 1B | Courtesy listening appointments, dealer commitments, booker pitches (§6) | Phase 0 and current scene calendars | distinct preparation plans; no repetition reward or duplicate demand |
| 2 | One backer agreement (§4.3) | Phase 1A | founding-source reconciliation; debt ledger; rejection/default/timing cases |
| 3 | Real venue access, saturation, shared local appeal (F1–F3) | Phase 1B | permission, realized bills, audience turnover, net-income diagnostics, migration |
| 4 | Paid demo production (§7.1) | current studio machinery and Phase 1A | deposit funds costs; correct deliverables/refunds; real clients and calendar |
| 5 | Early master shopping and regional pickup (§5) | Phase 1A and contract/record state design | one deal state machine; rights, buyer releases, recoupment, reversion |
| 5A | Song placement (§7.2) | Phase 5 contact routes and publishing settlement seam | correct AI-to-player routing; no duplicate composition charges |
| 5B | Gig pay/booking commission correction (F5) | Phases 1B, 2, 4 | no duplicate income; viable suitable opening policies with flat inflow removed |
| Later | Route convenience (F6), partner agreement (§4.4) | respective stable systems | no bypassed decisions; explicit contract/accounting semantics |
| Deferred | Storefront (§8), wholesale channel shift (F4), alternative red-ink reset (F7) | separate author decision and research/calibration | dedicated design and acceptance gates |

Economy gates use measured transactions. Overhead comparisons are diagnostics; neither service
fees nor table sales scale automatically to the label's bills. No phase requires unlimited escape
routes or profitable use of every day.

Every implemented phase must:
- build `-c Debug` before headless runs;
- prove AI-only byte-identical results with the established 52-week CSV comparison;
- pass `SaveLoadRoundTripRunner --integration` and cover the phase's new state;
- extend `--inspect-slot` where saved commitments/ledgers need inspection;
- verify player-present shared-world effects intentionally introduced by the phase;
- preserve existing affordability, callback, stock, and skip behavior unless explicitly changed;
- document historical anchors separately from authored mechanics/economic parameters.

Targeted verification must cover cancelled/missed appointments, factory delays, blocked travel,
unreleased appeal, rejected negotiations, delivery-time COD, licensed stock, services with deposits,
debt due at month change, and actual performed gigs as applicable. Run the opening probe after each
phase and explain regressions; do not mask them with guaranteed acceptance.

---

## §12 Sources and verification status

### Directly checked for this revision

- [Stax Museum, 1957–1968](https://staxmuseum.com/1957-1968/): Axton's mortgages, Satellite shop cash
  flow/market listening, regional success and Atlantic agreement, and Stewart's continuing bank job.
  Supports the practice; does not establish the game's precise equity percentage or shop economics.
- [Classic Motown, Smokey Robinson's 'Job' of a Lifetime](https://classic.motown.com/story/smokey-robinsons-job-of-a-lifetime/):
  Miracles recordings leased to End and disappointing royalties motivating independent operation.
- [Classic Motown, Marv Johnson “Come to Me”](https://classic.motown.com/story/marv-johnson-come/):
  Ber-Berry loan, local release, Johnson's signing to United Artists, and $3,000 advance against
  royalties. Corrects the original's universal artist-retention assumption.
- [Adam White, 20 Motown Milestones](https://www.adampwhite.com/westgrandblog/2018/1/11/20-milestones-in-motown-history):
  cites loan paperwork reproduced in Gordy's autobiography, including 6% interest. Secondary account;
  consult the reproduced document before claiming a historically exact repayment schedule.
- [Country Music Hall of Fame and Museum, Sam Phillips exhibition](https://countrymusichalloffame.org/press/releases/flyin-saucers-rock-roll-the-cosmic-genius-of-sam-phillips-opens-friday-at-the-museum):
  documents the Presley acetate artifact and recording-contract material; this does not establish
  the proposed hired-room demo fee/margin model.

### Original research leads retained, not all independently verified

- [Berry Gordy](https://en.wikipedia.org/wiki/Berry_Gordy), [Motown](https://en.wikipedia.org/wiki/Motown),
  [Michigan Rock and Roll Legends, The Birth of Motown](https://michiganrockandrolllegends.com/blog/403-ch-10-the-birth-of-motown),
  and [American Songwriter, Gordy facts](https://americansongwriter.com/5-fascinating-berry-gordy-facts/).
- [“Bad Girl”](https://en.wikipedia.org/wiki/Bad_Girl_(The_Miracles_song)) and
  [Gwen Gordy Fuqua](https://en.wikipedia.org/wiki/Gwen_Gordy_Fuqua): Chess licensing and songwriting.
- [Soulful Detroit, Rayber Music Writing Company](https://soulfuldetroit.com/web01-soulfuldetroit/motown2.htm):
  original demo-service/$100 lead; unavailable through the review's browser tool, not verified here.
- [Encyclopedia.com, Sam Phillips](https://www.encyclopedia.com/people/literature-and-arts/music-popular-and-jazz-biographies/sam-phillips)
  and [Stereophile obituary](https://stereophile.com/content/sam-phillips-recording-pioneer-dead-80):
  custom-recording and leasing leads. Exact quoted prices need contextual verification.
- [BSN Pubs, Scepter/Wand story](https://bsnpubs.com/scepter/scepterstory.html) and
  [Florence Greenberg](https://en.wikipedia.org/wiki/Florence_Greenberg): Tiara/Decca/Scepter. Original
  research flags sale versus lease and the exact $4,000 account as matters to confirm.
- [Vivian Carter](https://en.wikipedia.org/wiki/Vivian_Carter),
  [Vee-Jay Records](https://en.wikipedia.org/wiki/Vee-Jay_Records), and
  [Stax Records](https://en.wikipedia.org/wiki/Stax_Records): founding and shop-owner leads. The
  repeated pawnbroker attribution for Vee-Jay's loan remains unverified.
- [WBEZ, Phil Chess obituary](https://www.wbez.org/arts/2016/10/20/phil-chess-co-founder-of-chess-records-dies-at-95):
  road promotion lead.
- [Don Robey](https://en.wikipedia.org/wiki/Don_Robey): Buffalo Booking Agency lead; not evidence of
  the exact commission proposed here.
- [The Rag Blog, Huey P. Meaux](https://www.theragblog.com/ivan-koop-kuper-huey-p-meaux-was-the-crazy-cajun-2/):
  regional-break/licensing lead.
- [Syd Nathan](https://en.wikipedia.org/wiki/Syd_Nathan),
  [Herman Lubinsky](https://en.wikipedia.org/wiki/Herman_Lubinsky),
  [BSN Pubs, Jewel/Paula](https://www.bsnpubs.com/gulfcoast/jewel-paulastory.html),
  [Johnny Vincent](https://en.wikipedia.org/wiki/Johnny_Vincent), and
  [Bobby Robinson](https://en.wikipedia.org/wiki/Bobby_Robinson_(music_producer)): shop-owner leads.
- [History of Rock, Independent Record Companies](https://history-of-rock.com/independent.htm):
  thin secondary distributor-terms lead, not a sufficient primary basis for a universal 90-day rule.

For implementation notes, identify which claim a source supports and which quantities remain
authored. Prefer period documents, contracts, interviews, archives, and specialist museum accounts
over a chain of unsourced summaries when setting specific contractual or economic rules.

---

## §13 Recommended scope decisions and deferred questions

Defaults for this revised proposal:
- **Storefront:** defer beyond the opening work; require separate approval before building.
- **Backer default:** arrears and lost standing; no additional automatic failure rule. Existing
  solvency rules still apply, and collateral-backed agreements need their own explicit consequences.
- **Live pay:** belongs to performers; introduce earned booking commissions only after coupled
  opening-policy verification.
- **Demos:** acceptable label work as paid production, with actual clients, facilities/access,
  deposits, costs, and deliverables. They are not a guaranteed bailout.
- **Ex-Musician outside session employment:** leave out of this scope. Participation in the label's
  own paid production service is a separate service-design choice, not automatic off-label wages.
- **Solvency reset:** keep current mechanics; clarify timing now. Test the fifteen-day alternative
  only as a separately approved balance change.
- **Core promise:** useful release preparation and visible completion costs, with viable suitable
  openings. Remove promises of universal rescue, guaranteed orders, and profitable activity every day.

Before implementing the optional partner or storefront, resolve their ownership, distribution,
inventory, staffing, and buyout rules in dedicated designs. Before final master-deal implementation,
choose the minimum contract variants that can faithfully represent retained artists versus artist
transfers without creating two independent deal systems.
