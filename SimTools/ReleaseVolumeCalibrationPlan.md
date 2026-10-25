# Release volume and LP units: plan

24 October 2026. Follows "Studio share across seeds" and "Release volume against
Billboard" in [LabelProjectJudgement.md](LabelProjectJudgement.md). Benchmarks:
Billboard, 6 August 1966 International Buyers Guide (releases 1955-65; units and
dollars by outlet 1961-65), and the per-label catalog spans in
[LabelReleaseProgrammeImplementation.md](LabelReleaseProgrammeImplementation.md)
("Singles issued per label, 1960").

## Where the singles are missing

Seed 1001, balance control, 1960: 3,565 singles against Billboard's 5,797 (0.61).
The 43 historical labels issue 722 of them. The 537 generated labels issue 2,843,
about 5.3 each. The named head firms run at 10-25% of their catalog anchors:

| Label | Model 1960 | Anchor 1960 | Model rate source |
|---|---:|---:|---|
| Columbia | 84 | ~360 | `MajorSinglesPerMonth` 10 → plans 120, issues 84 |
| RCA Victor (47- series) | 84 | ~160 | same |
| King (+ Federal) | 17 | ~168 | MidTier: `releasesPerMonth` × work budget 1 |
| Dot | 16 | ~130 | same (studio parent) |
| Imperial | 9 | ~80 | Independent: `releasesPerMonth` × .67 |
| Liberty | 3 | ~70 | same |
| Vee-Jay | 10 | ~45 | same |
| Atlantic (+ Atco) | 7 | ~40 (+25-30) | MidTier |
| Fire / Fury | 11 | 38 | Independent |
| Chess (main) | 3 | ~25 | MidTier |
| Monument | 2 | ~20 | Independent |
| Cameo (main) | 2 | ~15 | MidTier |
| Mercury | 78 | ~195 | `MajorSinglesPerMonth` |
| Decca | 80 | ~185 | same |
| Capitol | 74 | ~178 | same |
| MGM | 20 | ~110 | MidTier (studio parent) |
| ABC-Paramount | 17 | ~100 | same |
| United Artists | 17 | ~80 | same |
| Warner Bros. | 29 | ~65 | same |
| Colpix | 3 | ~25 | Independent (studio parent) |

Anchors for the last eight are added in
[LabelReleaseProgrammeImplementation.md](LabelReleaseProgrammeImplementation.md)
(25 October, ranges ±10-20%). The twenty anchored labels issued about 2,090
singles in 1960 against the model's ~570. Raising them alone adds about 1,500,
which takes the national total to about 5,100 (0.88 of Billboard). The rest has
to come from unanchored historical labels or the generated tail.

Planned and issued volume agree for non-Majors
(King plans 21, issues 17), so their volume is set by the rate, not by issue
gates. Majors lose about 30% of their plan at issue in the control; the judged
arm's issue-gate fix recovers most of that.

Working hypothesis: the national shortfall (about 2,200 a year) sits in the head
firms. The generated tail's 5.3 singles per label is not contradicted by
anything sourced. Raising the head while leaving the tail alone reaches roughly
0.88 of Billboard from the anchors alone.

## Step 1: release volume

**Base.** The balance configuration plus `--label-project-judgement` (v7). It
carries the issue-gate fix, so Major plans reach issue. Three seeds (1001, 2002,
3003), 106 weeks, one assembly per comparison.

1. **Fill the anchors.** Get 1960 (and 1961 where cheap) catalog spans for
   Capitol, Decca, Mercury, MGM, ABC-Paramount, United Artists, Warner Bros. and
   Colpix, by the same method as the existing table. Match each sim label to an
   imprint or a corporate family, and record which one (Columbia main series
   without Epic; Mercury with or without Smash and Philips). Unanchored historical
   labels keep their current rate.
2. **Per-firm programme rate, opt-in** (`--programme-firm-rates`, requires slate
   and capacity). A data table keyed by historical label name gives singles per
   year. It replaces `MonthlyCapacity`'s single rate for that label, Major or
   not: anchor ÷ 52 per week, times the existing status and seasonal factors.
   LP rates are untouched in this step. Generated labels keep the envelope. No
   new random draws: slots still round through `Slots`.
3. **Check that acts can carry the rate.** Columbia at 360 a year needs about 50
   active acts at a 6-8 week spacing; the model's Big Five issue from about 18
   acts each in year one. Before any roster change, measure where the new slots
   are lost (artist spacing, eligibility, material, cash) from the slate and
   refusal ledgers. Prefer existing channels that history used: deeper
   head-firm rosters for the Majors and King, and leased finished masters (AI
   master deals) for Dot, which leased hits from independent producers. A
   roster-depth change is a separate, named switch.
4. **Hold single units fixed.** Demand and channel capacity are not touched.
   The market should absorb more records as a lower sell-through rate (`r`),
   not as more units.

**Gates (three-seed means, against the base):**

- National singles issued 1960 / 1961 at 0.85-1.0 of 5,797 / 6,036.
- Anchored head firms within ±25% of their anchor.
- Single units within ±3% of the base; `basePurchaseCapacity` identical.
- Studio Top 10 share moves toward 19% (report, don't gate: one-run spread is
  6 points).

**Report, expected to move:** share of singles losing money (history 80-90%,
model ~8%); Top 10 units; Big Five Top 10 share (34% now, history 29%); distinct
Hot 100 and Top 10 acts (already 36% and 19% high: more volume may worsen this,
and that is a finding, not a reason to cut volume); opening field MidTier and
Small closures (closures must be earned); runtime (more records per week).

## Step 2: the LP unit target

**Retarget first.** The current LP unit-share curve (30% in 1960 rising to 55%
in 1969) is about 20 points under Billboard at retail and has the wrong shape.
Stores plus racks, the channels the game models (it has no record clubs):

| | 1961 | 1962 | 1963 | 1964 | 1965 |
|---|---:|---:|---:|---:|---:|
| LP units, stores + racks (M) | 128 | 146 | 145 | 154 | 182 |
| Single units, stores + racks (M) | 132 | 161 | 162 | 172 | 192 |
| LP unit share | 49.2% | 47.6% | 47.2% | 47.2% | 48.7% |
| LPs released | 2,927 | 3,655 | 3,686 | 3,553 | 3,546 |
| LP retail units per release (k) | 44 | 40 | 39 | 43 | 51 |

The share is flat at 47-49% through 1965. 1966-69 needs the revised 1969-70
directory split by outlet before it gets a target.

**Then the mechanism.** Model 1961: 50M LP units from 1,760 releases (29k each),
against 128M from 2,927 (44k each). The miss splits into releases (0.60) and units
per release (0.66). Album unit share follows the album channel-share curve
(share ≈ s / (1 + s) once demand saturates the channel). Reaching about 48%
needs a channel share near 0.92-0.96 from 1960 (`AlbumChannelShareEraC0/C1/C2`
in `ChartManager`), and enough album demand to fill it. LP release volume comes
from the head-firm LP rates (`MajorAlbumsPerMonth`, the .33 envelope) once
Step 1's per-firm table exists.

**Economic consequence.** With singles held, LP units of about 130M would add
about two-thirds to market units, and far more to revenue. Billboard has LPs at
75% of 1961 retail dollars. Labels will be much richer, so expect closures to
fall and cash to pile up. Plan a separate economy re-screen (cost per LP,
royalties, closures earned) before adopting anything. The same goes for the
genre and album-chart benchmarks: album-chart genre mix and soundtrack longevity
move with LP volume.

## Order and stopping points

1. Anchor research (no code).
2. `--programme-firm-rates` with native checks: the table parse, rate
   replacement, flag off byte-identical to the base.
3. Three-seed screen, then the act-capacity diagnosis. Stop and report before
   any roster or master-deal change.
4. LP retarget in the analysis code and docs (no sim change).
5. LP channel and release screens, then the economy re-screen.

The studio question returns after step 3: if studio share does not follow volume
once studio labels run at their anchors, it is a conversion question again.

## First probe: one act cadence for every label (`pac-v1`)

`--programme-act-cadence=W` (requires slate and capacity; native checks in
`--slate-work-budget-check`, 60 pass) gives every AI label one planned single per
release-eligible signed act every W weeks, in place of its tier rate. LPs keep
their envelope. No firm or tier number enters, and the catalog counts above are
used only to judge the result. Seed 1001, 53 weeks, balance flags plus
`--label-project-judgement`. The flag-off arm reproduces v7's 1960 launches
exactly, by tier and by label.

| 1960 | Off | W 13 (4/yr) | W 17 (3/yr) | History |
|---|---:|---:|---:|---:|
| Singles issued | 3,607 | 7,765 | 6,412 | 5,797 |
| Major / MidTier | 603 / 486 | 750 / 870 | 597 / 822 | |
| Independent / Small / Boutique | 1,213 / 780 / 525 | 2,869 / 1,519 / 1,757 | 2,268 / 1,294 / 1,431 | |
| Twenty anchored labels | 701 | 1,119 | 902 | 2,089 |
| Columbia / King / Dot | 107 / 15 / 19 | 125 / 63 / 51 | 96 / 47 / 42 | 360 / 168 / 130 |
| Single units (M) | 145.0 | 147.0 | 146.2 | |
| Sell-through r | .336 | .250 | .275 | |
| Top 10 units (M) | 38.6 | 29.2 | 30.7 | |
| Big Five / studio Top 10 share | 36.5 / 8.1% | 34.4 / 9.6% | 29.2 / 7.9% | 28.8 / 19.0% |
| Distinct Hot 100 / Top 10 acts | 470 / 91 | 520 / 95 | 510 / 104 | |

Single units hold (the purse is fixed; r absorbs the volume). The distribution
fails. Small labels triple their output while the anchored head reaches only
about half its history: at W 13 the national total is 1.34 times Billboard's,
with the twenty anchored labels at 0.54 of theirs. A uniform cadence cannot put
the volume where history had it, for two reasons:

1. **Head rosters are too shallow.** Columbia issues about 4.3 singles per act
   but from about 29 acts; 360 a year needs about 90. The roster hard capacity
   is still set by tier (Major 50, MidTier 25, `GetRosterCapacityForTier`).
2. **Nothing holds back a small label's per-act volume.** History's small labels
   issued fewer singles per act than the head; in the model, cash does not bind
   them (wbg-v1, and only ~8% of singles lose money against 80-90%).

Kept opt-in and inert when off. Not adopted.

## Second probe: roster capacity earned from earnings (`erc-v1`)

`--earned-roster-capacity=φ` (`Systems/EarnedRosterCapacity.cs`; native checks
`--earned-roster-capacity-check`, 8 pass). At each quarterly review a
growth-eligible label's hard roster capacity becomes `φ × annualized pretax
earnings ÷ its own recent direct cost per act`, never below the acts held and
never above 250. When the existing earned-growth gates pass (profitable, runway,
charting), the operating target moves to that capacity instead of one slot.
Which labels may grow is unchanged (launch Small, Boutique and Independent
labels stay frozen). φ = .5; seed 1001, 106 weeks, judged base; one seed.

| 1961 (weeks 53-104) | Base | Capacity only | Capacity + W 13 | Capacity + W 17 | History |
|---|---:|---:|---:|---:|---:|
| Singles issued | 3,658 | 3,578 | 7,605 | 6,700 | 6,036 |
| Big Five | 611 | 622 | 1,167 | 1,149 | ~1,080 (1960) |
| Columbia | 118 | 124 | 254 | 229 | ~360 |
| Studio (MGM, ABC-P, UA, WB, Dot, Colpix) | 76 | 61 | 290 | 276 | ~510 |
| King | 14 | 16 | 69 | 59 | ~168 |
| Twenty anchored labels | 733 | 740 | 1,644 | 1,635 | 2,089 |
| Independent / Small / Boutique | 1,221 / 839 / 586 | 1,228 / 829 / 572 | 2,573 / 1,445 / 1,556 | 2,187 / 1,203 / 1,328 | |
| Single units (M) | 145.1 | 145.5 | 150.0 | 149.3 | |
| Top 10 units (M) | 33.6 | 35.4 | 26.0 | 25.8 | |
| Big Five / studio Top 10 share | 32.3 / 6.0% | 41.5 / 6.2% | 27.7 / 4.6% | 28.5 / 10.2% | 22.1 / 16.9% |
| Distinct Hot 100 acts | 516 | 523 | 574 | 592 | ~362 a year |
| Closed labels (cumulative, week 104) | 83 | 84 | 119 | 102 | |

Big Five releasing acts in 1961 rise from 33-43 per firm (base) to 80-93 with
capacity plus cadence: scouting signs about 95 acts per Major over two years. The
Big Five then match their 1960 anchors as a group (Columbia stays short, the
other four run over). Capacity alone does not raise output: the tier rate still
binds, and releasing acts rise only to 53-59.

The smaller head firms reach about half their anchors. Their targets rise later
or not at all. MGM's first growth came in week 61 under W 13. A target that
does rise is filled more slowly, because their scouting signs fewer acts
(MGM 27-30 over two years, against the Majors' 95).

Problems with this version:

1. **Early-year annualization.** The week-9 review annualizes January-February
   earnings alone, so capacities jump erratically (Decca to the 250 bound in one
   arm, 70 in the other). The rule needs a smoothed or trailing-twelve-month
   earnings measure before any multi-seed screen.
2. **The tail still floods under the cadence.** It is unaffected by capacity
   (those labels cannot grow), so the per-act cadence alone sets their volume.
3. **Costs rise as volume rises.** Distinct Hot 100 acts go up (already high),
   Top 10 units fall about 23%, and closures rise (83 → 102-119). Single units
   rise 3%, at the edge of the ±3% guard.
4. **LP units fall** (53 → 47M) as singles crowd the shared channel. That moves
   against the next step's LP target.

Neither switch is adopted.

## Trailing earnings, three seeds (`erc-v2-w17`)

The capacity rule now uses trailing-twelve-month pretax earnings, kept per
calendar year from the closed monthly statements (`LabelEarningsHistory`, saved
only when the switch is on). Until six months have closed, nothing is earned and
the ordinary one-slot growth applies. Native checks: 14 pass. φ = .5 with
`--programme-act-cadence=17`; seeds 1001, 2002, 3003; 106 weeks; judged base.
The bases are the existing flag-off judged runs (`label-project-v7-judged` 1001,
`studio-seeds-judged` 2002/3003). The flag-off arm reproduced v7 by tier and by
label.

Three-seed means:

| | 1960 base | 1960 treated | 1961 base | 1961 treated | History |
|---|---:|---:|---:|---:|---:|
| Singles issued | 3,573 | 6,571 | 3,636 | 6,442 | 5,797 / 6,036 |
| Big Five | 497 | 486 | 604 | 900 | ~1,080 |
| Studio (six labels) | 111 | 197 | 90 | 263 | ~510 |
| Twenty anchored labels | 684 | 872 | 751 | 1,319 | 2,089 |
| Independent + Small + Boutique | 2,634 | 5,271 | 2,716 | 4,826 | |
| Single units (M) | 144.7 | 146.4 | 146.1 | 149.9 | |
| LP units (M) | 31.0 | 29.9 | 51.8 | 46.8 | |
| Sell-through r | .352 | .268 | .282 | .231 | |
| Top 10 units (M) | 37.8 | 31.8 | 34.5 | 28.1 | |
| Big Five / studio Top 10 share | 36.2 / 11.7% | 34.5 / 12.0% | 32.8 / 11.0% | 32.8 / 8.6% | 35.6 / 21.2%, 22.1 / 16.9% |
| Distinct Hot 100 acts | 461 | 531 | 501 | 562 | ~362 a year |
| Closed labels, cumulative | 39 | 35 | 82 | 98 | |

By label, 1961 (treated; base in brackets; anchor after the slash): Capitol 180
(123) / 178, Decca 187 (122) / 185, Mercury 186 (116) / 195, RCA 165 (118) / 160,
Columbia 181 (124) / 360; WB 47 (17) / 65, ABC-Paramount 58 (17) / 100, UA 46
(17) / 80, MGM 45 (18) / 110, Dot 55 (16) / 130, King 27 (9) / 168.

What it shows:

1. **The early-year jumps are gone.** Earned capacity now first applies at the
   week-35 review (six closed months). Columbia then reaches 98-140, RCA 73-111,
   and the smaller firms 12-68.
2. **Four Majors match their anchors in 1961.** Columbia reaches only half:
   nothing in firm state separates it from RCA, so the model cannot reproduce
   that difference, and should not without a cause.
3. **The smaller head firms reach 30-70% of their anchors, and King 16%.** Their
   capacity grows later and less. More of their signings fail
   `EstimatedAdvanceUnaffordable`: in seed 1001, King signed 5 acts and was refused
   26 times; Dot signed 15 and was refused 35. Their earnings are real but their
   cash is not, so signing affordability is the next gate for MidTier head firms.
4. **The tail doubles.** It carries most of the extra volume (about 2,100 a
   year). The national total lands near Billboard's only because the tail
   overshoots. This is the next step.
5. **Guards.** Single units +2.6% (inside ±3%). LP units fall 10% in 1961.
   Top 10 units fall 19%. Distinct Hot 100 acts rise 12% (already about 40%
   above history). Cumulative closures rise 82 → 98. Studio Top 10 share does
   not improve (8.6% against 11.0%), although studio output nearly triples.

Not adopted. Head-firm volume is now mostly an earnings-and-cash question; the
tail's volume is an economics question (next).

## The tail: why small-label volume is not held back

Seed 1001, singles launched by week 78 with a retired outcome, base (judged v7)
and capacity + W 17:

| Tier | Base: share losing / median net / mean net | Treated: share losing / median net / mean net |
|---|---:|---:|
| Small | 50% / $14 / $3,171 | 53% / −$75 / $2,727 |
| Independent | 42% / $440 / $8,242 | 50% / $8 / $4,718 |
| Boutique | 38% / $656 / $9,126 | 48% / $55 / $3,734 |
| Major | 48% / $425 / $18,938 | 58% / −$1,658 / $11,024 |

On the current balance configuration about half of singles already lose money,
not the 8% of the 21 October corporate default. Under the cadence the median
tail single breaks even, but the mean stays positive because hits pay for the
rest. For a label that can carry the risk, an extra single is still worth
releasing on average, so cash never pushes back.

The gate that should screen the marginal single is the issue forecast
(`projectedOrphanSingleNet` > marketing budget). For tail labels its median is
5-10 times the realized median even in the base (Independent $5,103 against
$440). Its mean was close in the base ($8,805 against $8,095) but runs 1.6-2
times high under the cadence ($7,498 against $4,718), because it does not see the
fuller market. It also barely sorts singles: the bottom fifth of tail forecasts
(median $1.1k) realizes −$312, and the top fifth ($16k) realizes $503.

So the tail is volume-bound by the planned rate alone, as before. A
non-template fix has to make a small label's own economics push back:

- **Forecasts that see the market.** The single forecast learns from the label's
  recent realized-to-forecast ratio, so a fuller market lowers it. The prior's
  known 2-13x career miscalibration (Economy audit A2-A4) belongs here too.
- **Capital-constrained risk.** A label with little cash weighs the chance of a
  loss against its runway, not only the mean. Small firms cannot diversify
  across many releases the way a Major can.
- **Cash-flow timing.** Pressing paid up front, distributor payment 60-120 days
  later, returns. History's independents were bound by working capital. The
  model has receivables, but the receivable lag and returns are not calibrated.

The first changes no label's behavior unless its forecasts are wrong. The other
two need evidence on 1960s independent finance before being sized.

## Forecasts that see the market (`sfr-v1`)

`--single-forecast-realization=K` (`Systems/SingleForecastRealization.cs`;
native checks `--single-forecast-realization-check`, 20 pass). Diagnosis first:
under capacity + W 17 (seed 1001), tail singles realize 32-55% of their prior
in dollars, but the residual memory's confidence for those labels is only
.18-.25 (K 12, at most .65), so their forecast stays within about 6% of the
prior. The switch replaces the single lane's residual term with the label's own
realized-to-prior ratio in dollars: the same maturity- and recency-weighted
revisions (52-week half-life, 104-week window), so a hit counts at its value, not
a clipped residual. The ratio is shrunk toward 1 by K releases at the label's
mean prior and bounded at 0-3. Forecast = prior + (ratio − 1) × |prior|. The
estimated outcome is kept on each observation only when the switch is on (null
otherwise; not written to saves). K = 4 on the erc-v2-w17 bundle (capacity .5 +
W 17), 106 weeks, seeds 1001 and 2002; seed 3003 was not run. Base: the
existing erc-v2-w17 runs. Switch off: a 13-week seed-1001 replay matches the base
in all 63 week-keyed CSV streams through week 12.

**The ratio reaches the forecast.** Tail forecasts, as a share of prior (seed
1001, median): weeks 1-26 .93-.95 (base .97-.98), weeks 79+ .56-.60 (base
.77-.82).

**Tail volume does not move.** Two-seed means, 1961:

| | Base | Realization ratio |
|---|---:|---:|
| Singles issued | 6,373 | 6,404 |
| Independent / Small / Boutique | 2,240 / 1,277 / 1,237 | 2,235 / 1,255 / 1,267 |
| Big Five | 904 | 978 |
| Studio six / King | 237 / 16 | 218 / 16 |
| Single-net-below-marketing refusals (tail) | 861 (0) | 333 (0) |
| Single / LP units (M) | 148.9 / 48.2 | 149.2 / 47.7 |
| Top 10 units (M) | 27.6 | 27.2 |
| Distinct Hot 100 acts | 562 | 571 |
| Closed labels, cumulative | 104 | 105 |

Why:

1. **The issue gate is not a bar for the tail.** It refuses a single only when
   the forecast is at or below the marketing budget: $150-400 base times
   marketing power and the artist's career multiplier. Tail forecasts are
   $3-5k after the cut. No tail single was refused in either arm.
2. **A correct forecast still says release.** Tail singles realize a positive
   mean before marketing ($1.8-4.5k in the base), because hits pay for the rest.
   An unbiased forecast of a positive mean passes any break-even gate. A forecast
   cannot make the tail push back unless its expected single loses money.
3. **The head moves instead, for the wrong reason.** In year one the Majors'
   dollar ratio runs above 1 (forecast/prior summed 1.44 in seed 1001, weeks
   1-26), so refusals fall by about 60% and the Big Five issue 8% more in 1961.
   Retired Major singles' forecast/realized rises from 1.08 to 1.65. The likely
   cause is the provisional revisions: at ages 10-14 they annualize label net on
   the linear maturity ramp ((age + 1)/20), and a front-loaded hit is overstated
   most. Provisional estimates ran 11% above the final on average in the base.

Not adopted. Corrections before any rerun: build the ratio from finalized
outcomes only, or on the front-loaded curve (`--format-memory-maturity-curve`).
The tail result stands either way. Holding the tail back needs either a cost
side that makes its marginal single lose money on average, or risk weighed
against a small label's runway ("capital-constrained risk" above). The returns
research below gives no period basis for a cash-flow version.

## Independent working capital: what the sources size

25 October 2026. Research only. The payment lag was already sourced in
[WorkingCapitalResearch.md](WorkingCapitalResearch.md) (22 October). This adds
returns, read from the full text of the scans (worldradiohistory PDFs, text
layer extracted with pypdf; "p." is the PDF page). Copies and extracted text
are in the session scratchpad, not the repository.

### The terms, by link in the chain

**Label to distributor, singles: a 5% return privilege, then 10%.**

- Billboard, 9 Mar 1963, p. 10 ("'Granddaddy' Smith on This & That"): Leonard
  Smith, an Albany distributor and rack jobber, calls "the 5 per cent return
  privilege" a "holdover from the Glenn Miller days", and contrasts it with "the
  100 per cent exchange situation on LP's". So 5% on singles was the long-standing
  standard term.
- Billboard, 29 Jan 1966, p. 6 (Chess distributor convention, San Juan): a new
  Chess policy "grants 10 per cent return privileges on single purchases with 30
  cents credit per unit on all records returned over and above the
  distributor's allocation per quarter. The company's policy of 100 per cent
  exchange on LP's remains unchanged."

**Label to distributor, LPs: 100% exchange.** Both items above. Exchange means
credit against other product, not cash back.

**Distributor or salesman to dealer: promised 100%, not always honoured.**

- Billboard, 29 Feb 1960, p. 3 ("Hot Topics Set for ARD Meet Agenda"): the
  New York/New Jersey dealers' association lists the "100 per cent exchange
  privileges that manufacturers don't live up to". Distributors disown what a
  salesman promised ("he doesn't work for us anymore"), so dealers plan to make
  salesmen sign a standard form.
- Music Vendor, 23 Jan 1961, p. 3 (editorial): dealers swamped with merchandise
  "on memo", "shipped in on a 100% return guarantee and a 'pay us when you sell
  the goods' policy", tied to rising dealer mortality.

**Racks: heavy returns falling; slow payers.**

- Billboard, 3 Nov 1962, p. 7 ("Survey Shows Distribs Feel They Must Rack",
  ARMADA survey): a Boston distributor, "Rack returns are exorbitant and our
  profits from rack sales are very small." Distributors blame manufacturers and
  each other for extending credit to rackers "who don't pay their bills".
- Billboard, 9 Mar 1963, p. 10: distributor advice and smarter rack buying cut
  the return rate, which "used to run into astronomical figures. A number of
  manufacturers say that the rack return rate is now under 10 per cent." Rackers
  carry a reputation "as slow payers". Billboard's rack survey, same page: about
  50% of rackers pay suppliers in 30 days, just under 15% on 30-60 days, and a
  little over 30% on 30-60-90 day terms.

**Label to dealer, seasonal dating.** Billboard, 3 Nov 1962, p. 11 (Liberty
Christmas LP ad): "10% cash discount. 100% guaranteed sale: return-for-credit.
Payments: 1/2 January 10, 1/2 February 10, 1963." Goods shipped in November are
paid two to three months later, with the label carrying the unsold risk.

**Collections, distributor to label.** Billboard, 14 Mar 1964, p. 1 ("Collections
Are Worse Than Ever"), confirmed in full: some firms wait four to six months;
fieldmen are told to spend most of their time collecting; volume users control
80% of the business; a leading chain account pays in 100-120 days instead of
90. "The hottest of the hot labels are getting their money more promptly", but
"one or two hits do not make a hot label"; one firm "held back shipments on its
coming hit to make a number of its accounts pay up."

### What this sizes

1. **The payment lag is already right.** 12-18 weeks
   (`IndependentDistributorFactory.PaymentTermWeeksFor`) matches 90-120 days.
   Leverage, not the average term, decides who waits four to six months.
2. **Singles returns to the label were small.** A label sold singles to its
   distributors outright, less a 5% privilege (10% at Chess by 1966, plus 30¢
   credit on returns above allocation). The large single returns of the period
   were further down the chain: dealers' exchange promises, consignment "on
   memo", and racks. The distributor and the dealer carried them, not the label.
   So a returns reserve on a label's singles billing would hold back about 5% of
   a receivable. It is a small timing term, not a cash-flow constraint.
3. **LPs are where a label carried returns.** A 100% exchange on LPs gives
   credit against new product. It drags most on a label with little new product
   to exchange into.
4. **The model inverts who carries an unsold single.** AI labels are billed on
   units sold and pay the plant for every copy pressed (the pressing book), so
   the label carries 100% of an unsold single. Historically the distributor
   carried about 95% of what it ordered, and it ordered cautiously. This is a
   fact about the model's risk split, not yet a proposal. Moving the risk to
   distributors would make flops cheaper for labels and push against the
   loss-rate gap (history 80-90% of singles losing money). It needs a
   distributor ordering rule first.
5. **The working-capital squeeze that is sourced is pressing paid well before
   collection.** The game's four-week plant term against 12-18 week receipts
   already gives that. What it lacks is the leverage term: paid sooner with a
   sustained flow of hits (`--hot-label-collections`, specified in
   WorkingCapitalResearch.md), later or not at all once seen as failing (Ace).

**Still not found:** independent plant payment terms (C.O.D. or credit),
factoring or bank lines on record receivables, and a national singles return
rate. So the period gives no basis for a cash-flow constraint that would hold
back the tail's per-act volume. The 5% returns term is too small to do it. A tail
constraint would have to come from the forecast (this plan's first probe) or
from the leverage rule, not from returns.

## Capital-constrained single risk (`scr-v2`)

25 October 2026. An opt-in mechanism probe, not a historically sized risk
tolerance. `--single-capital-risk=F` requires the slate and capacity switches;
0 < F <= 1. `Systems/SingleCapitalRisk.cs` prices a label's own finalized
orphan-single outcomes against cash above its existing commitment reserve.
The reserve already includes runway, plant bills, tax/service holds and other
slate reservations; receivables are not cash. The item's own old reservation
is excluded, so it is not charged twice. No tier or firm preference enters.

For risk capital B = F times unreserved cash, the empirical certainty equivalent
is B times (exp(weighted mean log(1 + net / B)) - 1). The arithmetic mean minus
that certainty equivalent is the risk premium. Deduct the premium from the
current mean forecast after campaign cost. An observed outcome exhausting B
vetoes the bet. As capital grows the premium approaches zero. This uses the
chance and size of losses, including the hits that finance them, rather than
imposing a historical loss percentage as a quota.

Evidence is local, finalized only, with the existing 104-week window and
52-week half-life. Provisional estimates cannot price risk. Under the legacy
economics, outcomes omit marketing: today's reserved campaign is subtracted
from each historical outcome as an explicit proxy for its missing historical
campaign. All-in observations already include their campaign. The optional
`estimatedOutcomeNet` field is reused and saved only with either forecast or
capital-risk probe enabled. No new random draws.

The gate runs at `ReserveSlate` before recording, so a refusal can save the
session cost. A prepared master proceeds through the existing issue gates;
sunk recording cost is not risked a second time. Empty evidence and nonpositive
mean forecasts defer to the existing gates. This keeps a new pre-recording
mean-profit screen out of the treatment: every risk refusal has evidence and a
positive forecast. Refused work uses the existing postponement/window expiry
policy and can be admitted later as cash or evidence changes.

`RunSingleCapitalRiskProbe.ps1` records the assembly hash, native checks, enabled
13-week save/load round-trip, an off replay and the treated runs. Defaults:
F = 1, seed 1001, 106 weeks, the `erc-v2-w17` bundle without `sfr-v1`.
`analyze_single_capital_risk.py` checks matched flags, unchanged authored capacity,
finite risk arithmetic and positive-mean-only refusals; it reports annual
guards, mature retired outcomes, attempts versus unique refused slots, and
whether refused slots eventually issue. This first screen is preliminary.
The risk ledger precedes ordinary funding checks: a risk refusal can overlap
an existing cash shortfall. Neither its attempt count nor its distinct-slot
count alone measures releases prevented by the treatment.

Limitations before adoption: no period evidence sizes F or log utility; each
small label may have very few final outcomes; the distribution does not yet
condition on the proposed act, career, production budget or material; returns
are lifetime profit outcomes rather than dated cash-flow paths. A low-cash
single worst case is a conservative veto, not a calibrated bankruptcy
probability. The existing reserve protects cash needs, but this does not model
the joint losses of outstanding releases or assume independent bets.
The arithmetic mean used to compute the empirical premium can also differ
substantially from today's forecast. Transferring its absolute dollar premium
can then reject a positive forecast even when every historical outcome in
the sample was profitable. The analyzer counts those cases separately. This
is a limitation of the prototype, not evidence of a cash-runway loss risk:
a next version needs a candidate-conditioned distribution whose mean agrees
with the forecast and whose downside agrees with the candidate's costs.

**Completed first screen:** F = 1, seed 1001, 106 weeks. Annual windows are
weeks 1-52 and 53-104. Control: the archived `erc-v2-w17` seed-1001 run.
The 25 native checks and enabled 13-week full-world save/load round-trip pass.
With the switch off, all 73 week-keyed CSV streams have identical headers and
cell values through week 12. Authored base and album-channel capacities match
at every region/week through week 104; the Hot 100 is complete and market
settlement reconciles. The treated run completed without engine errors, in
988 seconds (control 883 seconds; an unreplicated runtime comparison).

| | 1960 control | 1960 risk | 1961 control | 1961 risk |
|---|---:|---:|---:|---:|
| Singles issued | 6,430 | 6,116 | 6,294 | 5,342 |
| Big Five | 519 | 519 | 907 | 950 |
| Studio six | 177 | 140 | 212 | 136 |
| MidTier | 818 | 700 | 674 | 309 |
| Independent | 2,258 | 2,151 | 2,116 | 1,767 |
| Small | 1,301 | 1,285 | 1,201 | 1,130 |
| Boutique | 1,428 | 1,357 | 1,253 | 1,050 |
| Single units (M, live settlement) | 146.29 | 146.35 | 147.21 | 147.12 |
| LP units (M) | 31.13 | 31.05 | 50.35 | 50.48 |
| Sell-through r | .272 | .278 | .219 | .262 |
| Top 10 units (M) | 31.09 | 32.85 | 27.29 | 29.95 |
| Big Five Top 10 share | 30.6% | 36.7% | 35.4% | 52.1% |
| Studio Top 10 share | 8.7% | 6.3% | 6.2% | 4.4% |
| Distinct Hot 100 / Top 10 acts | 511 / 106 | 522 / 98 | 570 / 91 | 551 / 92 |
| Closed labels, cumulative | 37 | 37 | 111 | 103 |

**The tail moves, but the head is damaged more.** In 1961 the three tail tiers
fall from 4,570 singles to 3,947 (-13.6%). Small alone falls only 5.9%.
MidTier falls 54.2%, studio output 35.8%. Big Five output rises 4.7% and its
Top 10 share gains 16.7 points. The desired head-versus-tail distribution
does not emerge. Single units change -0.07% and LP units +0.26%; fixed units
are absorbed by higher sell-through. Eight fewer labels close; that single-seed
survival change is not a calibrated benefit.

**Evidence and mechanism:** the last assessment per label has a median of 5
final outcomes for Small, 10 Independent, 13 Boutique, 26 MidTier and 185.5
Major (tiers at assessment; a promoted label can appear in more than one tier).
Among assessments with evidence, positive forecast and positive capital, the
median Small risk premium is just $3 against a $1,356 forecast. Independent
and MidTier premiums are $2,385 and $1,965 against forecasts of $1,606 and $553.
Repeated postponements greatly inflate attempt counts, and many refusals overlap
ordinary funding failure. For example, Small's 3,693 refused attempts cover
499 distinct slots; 1,101 attempts have no capital above the reserve, and 127
of those 499 slots eventually issue. These are gate encounters, not 499
incrementally prevented releases.

**The forecast basis does not support adoption.** There are 38 distinct
positive-capital refused slots with no observed losses: a direct counterexample
to interpreting every transferred premium as candidate downside risk. Also,
Major assessments have a median pre-recording forecast of -$2,072 and mostly
defer to the existing issue-time gate, which treats already-paid production as
sunk. Only two Major assessments have evidence, positive capital and a positive
pre-recording mean. Their near absence of risk refusals therefore does not
establish diversification earned from cash alone. Reintroducing a new
pre-recording mean hurdle would be a separate experiment, not a repair to this
risk-only comparison.

Mature retired singles (launched by week 78, final by week 104) remain near the
same loss rates: Small 53.4% -> 54.3%, Independent 49.9% -> 48.0%, Boutique
48.4% -> 46.6%. Their positive means persist ($3.0k / $4.9k / $4.1k treated,
before marketing on the legacy outcome basis). This does not close the
historical loss-rate gap.

**Not adopted; no working-bundle change.** Keep the switch as a diagnostic.
The next risk probe needs one forecast-consistent candidate distribution at
the funding decision, with a common cost basis for prepared and unprepared
masters, a loss bound from the candidate's own costs, and an explicit treatment
of sparse local evidence. Do not tune F to recover the missing output before
those pieces agree. This screen uses one seed, without a sensitivity or
multi-seed claim. Reproducible outputs: `SimLogs/scr-v2/review/{annual,risk,losses}.csv`
and `validation.json`; run/check manifests live in `SimLogs/scr-v2/` and
`SimLogs/scr-v2-checks/`.

## One candidate distribution for mean and risk (`scr-v3`)

25 October 2026. Replaces the transferred dollar premium under the same opt-in
switch. `--single-capital-risk-policy=observe|mean|log` selects observation only,
a mean gate, or log utility (default). All three read exactly the same candidate
forecast and distribution. The working bundle is unchanged.

For candidate expected contribution A and remaining commitments C, outcomes
are **A times X minus C**. Finalized records provide dimensionless shocks
rho = final contribution / immutable release-time expected contribution.
Normalize them by their recency-weighted mean: X = rho / mean(rho). Thus
E[X] = 1, the actual distribution mean is A - C, and every outcome is at least
-C. There is no historical dollar premium to transfer to a smaller project.
Quality and career enter the candidate prior and each historical denominator;
the shape uses the label's own matching launch career band. No tier preference,
pooled future observations or new random draws enter.

The funding read now fills the quality and career indices before calling the
prior, as `DecideRelease` does. The v2 funding read had left those indices at
their default Q0/NewSigning values. This was an implementation error in v2,
in addition to its incompatible historical premium, and explains part of its
Major forecast anomaly. An integration check changes a real funded act's
career and verifies that the contribution read changes.

The prior is a net forecast that already subtracts recording and embeds
initial pressing losses. For its pre-campaign forecast f, recording estimate
P, planned campaign M and initial plant commitment I, recover A = max(0,
f + P + I). Before recording, C = P + M + I; after recording, C = M + I.
This preserves the existing expected net less the actual reserved campaign
at the first funding decision, and removes the recording charge exactly once
at the prepared-master decision. The initial plant commitment belongs to both
the contribution reconstruction and the cost subtraction, once each. Both
policies use this same decomposition; the mean arm isolates any effect of a
new pre-recording profitability hurdle from the incremental risk decision.
It conditions the distribution on the existing net forecast; it does not refit
that forecast's response to a different pressing policy or correct its bias.

Risk is assessed only after the ordinary current funding check succeeds,
including any existing finance. Cash below the reserve is no longer counted
as a risk refusal. B is the same fraction of cash above the existing reserve
as before. The mean policy accepts A - C > 0. Log utility accepts only a
positive mean and positive certainty equivalent on that very distribution;
an outcome exhausting B vetoes the bet. All-profitable candidate support with
a positive mean cannot be risk-refused. Prepared masters are assessed on their
remaining costs, with recording already paid from the cash balance.

**Sparse evidence is explicit.** Require at least four effective finalized
observations in the same career band, with the 52-week half-life and 104-week
window retained. Effective sample size is (sum weights)^2 / sum squared weights.
Provisional, future, untagged old-save and unsupported negative-contribution
observations are excluded. When evidence is insufficient or every historical
contribution is zero, the status says so and log falls back to the mean gate.
Its deterministic fallback is not an estimated zero probability of loss.
This threshold is a scenario choice, not evidence that four releases identify
the hit tail. Report coverage before interpreting a small-label result.

Each live record saves the optional immutable expected contribution; finalized
memory saves that denominator and final contribution. These fields are absent
in fresh flag-off worlds. They round-trip through full-world save/load; an
older save can accumulate fresh evidence without reconstructing old releases
from current costs. The new outcome stream preserves denominators for audit.

Native checks cover mean conservation over different budgets, the candidate
loss floor, historical dollar-scale invariance, probability normalization,
Jensen's bound, all-profitable support, capital monotonicity, prepared/unprepared
funding, the actual career read, evidence eligibility and saved state. The
screen runner now defaults to 53 weeks and all three policies. Use
`analyze_single_capital_distribution.py` for v3; the v2 analyzer remains for its
archived result. Compare observe with the archived bundle, then log with mean.

Remaining limits: the mean forecast can still be wrong; conditional mathematical
consistency is not empirical calibration of loss probabilities. Recording and
plant commitments are estimates, future re-press expense is in contribution,
and the model does not price uncertain future cost overruns, dated collection
lags or simultaneous losses across outstanding projects. The contribution
shape assumes nonnegative operating contribution after variable expenses;
negative finals are exposed in the audit and excluded rather than silently
clipped. Log utility and F still lack a period-sized risk preference.
Final-only samples can also be retirement-selected while long-running hits
are still active. Their shape is not a validated lifetime hit distribution;
four effective observations are only an explicit minimum for this probe.

**Initial validation.** Build succeeds with zero warnings. All 180 native
checks and the enabled 13-week full-world save/load round-trip pass. The
53-week seed-1001 observe arm reproduces all 73 legacy week-keyed CSV streams
through week 52, including raw record rows. It is behaviorally inert over that
window. Its 17,952 funding assessments have maximum absolute mean error of
$0.000000000022, no loss-floor violations, no ordinary cash failures in the
risk ledger, and no risk refusals on all-profitable candidate support (444
assessments). Twelve raw forecasts imply negative contribution: they are
clamped to zero contribution, explicitly flagged, and remain nonpositive-mean
bets. All 3,536 captured finalized contribution outcomes have nonnegative
contribution and a positive saved denominator in this run.

On the unchanged observe history, log would refuse 63 distinct positive-mean
slots: Independent 36, MidTier 10, Major 7, Boutique 6, Small 4. These are
counterfactual gate encounters, not measured releases prevented. Only 3.7%
of Small pre-recording assessments and 3.8% of their prepared-master
assessments have an empirical shape; the rest explicitly use the sparse
fallback. Own-label data coverage limits the first-year result.

**Completed three-policy screen:** F = 1, seed 1001, 53 weeks; compare the
complete 1960 window (weeks 1-52). Mean and log have the same assembly, bundle,
forecast, cost basis, evidence rule and capital fraction. Observe is identical
to the archived `erc-v2-w17` world through week 52. The off replay also matches
all 73 legacy week-keyed streams through week 12. All policies preserve the
authored base and album-channel capacity at every region/week, complete the
Hot 100 window and reconcile market settlement.

| 1960 | Observe / archived | Mean gate | Log utility |
|---|---:|---:|---:|
| Singles issued | 6,430 | 6,388 | 6,371 |
| Big Five | 519 | 453 | 450 |
| Studio six | 177 | 192 | 184 |
| MidTier | 818 | 808 | 789 |
| Independent | 2,258 | 2,291 | 2,292 |
| Small | 1,301 | 1,331 | 1,341 |
| Boutique | 1,428 | 1,439 | 1,435 |
| Three tail tiers combined | 4,987 | 5,061 | 5,068 |
| Single units (M, live settlement) | 146.29 | 145.51 | 146.04 |
| LP units (M) | 31.13 | 31.95 | 31.23 |
| Top 10 units (M) | 31.09 | 30.81 | 31.25 |
| Big Five Top 10 share | 30.6% | 35.2% | 37.9% |
| Studio Top 10 share | 8.7% | 12.9% | 11.7% |
| Distinct Hot 100 / Top 10 acts | 511 / 106 | 526 / 102 | 492 / 91 |
| Closed labels, cumulative | 37 | 39 | 37 |

The incremental risk comparison is log against mean. Singles fall by 17
(-0.27%); tail volume rises by 7 (+0.14%), and Small rises by 10. MidTier
falls 2.4%, rather than v2's 54% second-year fall. Single units rise 0.36%
and LP units fall 2.24%. The old penalty's large volume effect is not
reproduced by this consistent candidate distribution. The mean-only gate has
its own effect (Big Five 519 -> 453) and must not be called risk aversion.
These one-seed annual swings also include the ensuing stochastic reshuffle;
they do not estimate a multi-seed policy effect.

In the live log arm there are 235 risk-only refused attempts on 83 distinct
slots: Independent 26, MidTier 33, Major 10, Boutique 7, Small 7. All occur
before recording; prepared masters' smaller remaining downside never adds
a risk-only refusal in this window. Many attempts retry the same intention.
Small's empirical coverage is only 5.0% before recording and 4.2% after;
the median last effective sample size per Small label remains below two.
There is no evidence here that current cash capitalization plus this local
history rule would restrain the tail materially. There is also too little
Small-label shape coverage to infer that a calibrated richer prior would
have no effect.

Across all three policies, 59,067 assessments pass the distribution checks;
maximum absolute mean error is below $0.000000000030. There are no loss-floor
violations, no ordinary cash failures in the risk ledger and no risk refusals
on all-profitable candidate support. All captured final contributions are
nonnegative and all saved denominators positive in these runs. Runtime was
356 / 472 / 557 seconds for observe / mean / log, unreplicated timings.

**Outcome:** mathematical and funding consistency is repaired, including the
v2 bucket error and sunk recording basis. Keep v3 opt-in. The remaining work
before adoption is empirical validation of the mean and conditional shape,
especially sparse histories and retirement selection; do not tune risk
tolerance to force a target release count. This screen makes no calibrated
loss-probability, historical-preference or multi-seed claim. Results and
validation are in `SimLogs/scr-v3/review/`; manifests are in
`SimLogs/scr-v3-{observe,mean,log}/` and `SimLogs/scr-v3-checks/`.

## LP capacity factorial

The user approved the next LP programme-only, purchase-only and joint screen.
Protocol and results: [LpCapacityScreen.md](LpCapacityScreen.md). It uses the
judged base without earned-roster-capacity, per-act cadence or risk probes, keeps authored
single purchase capacity fixed, and separates calendar-year issues from units.
The completed one-seed screen reaches 3,032 LP issues in 1961 against 2,927,
but only 74.05M LP units against 128M. The joint arm fills all serviceable LP
demand; raw demand is only 82.38M. LP catalogue/demand depth is the next unit
diagnosis. Realized single units fall 5.43% as legacy mixed spillover shifts to
LPs despite identical authored single capacity. Head cash and tail income rise
in aggregate, but individual head liquidity remains mixed and Small singles
still have a majority-losing retired sample. No defaults are adopted.
