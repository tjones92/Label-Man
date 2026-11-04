# LP catalogue and demand diagnosis

25 October 2026. Follow-up to [LpCapacityScreen.md](LpCapacityScreen.md).
The user requested an investigation of the unit shortfall after the joint LP
programme/purchase arm reached roughly the published annual issue count.

## Question and protocol

Separate inadequate opening catalogue, ordinary LP demand decay, and weak
new-release conversion. The starting point is the `lpc-v1-both` bundle: judged
v7, LP programme scale 2, LP purchase share .48. It does not include the earned
roster, per-act single cadence, forecast-realization or capital-risk probes.
Do not change calibration defaults or tune to an LP issue-count gate.

`--lp-demand-audit` writes one national row per LP per settlement week, from
the frozen settlement before retirement. It includes every LP, charted or not,
its opening/launch identity, release year, current age, format, current label
tier, quality, raw demand, serviceable intent and final cleared sales. Regional
raw stages come from the already-computed sales inputs. Rows include the final
sale of retiring titles; a retired runtime record is not retained by the audit.
The existing settlement, sales, census and cash streams validate completeness.

The same row includes frozen-state raw-demand shadows:

- Ordinary-album weekly decay .93 (the legacy rule).
- Ordinary-album weekly decay .985 (a sensitivity scenario).
- No ordinary-album age decay.
- No buyer exhaustion, holding the actual age decay and other factors fixed.
- Full awareness, holding all other factors fixed.
- Existing demand before single/LP cannibalization.

These are **nonadditive sensitivities**, not extra purchases, lifetime forecasts,
or predicted equilibria. They retain the actual population of active records,
awareness, buyer pools and accumulated sales. They cannot recreate omitted old
titles or records which already retired. Buyer pools are summed across title/
region observations for weighting, not an estimate of unique national buyers.

The live comparison is observe versus `--lp-catalog-weekly-decay=.985`. Only
ordinary LP age decay changes, starting after week eight. Soundtrack fallback
and external-media demand curves are unchanged. Purchase capacities, single
programme settings and all other flags are identical; realized sales and the
subsequent company/artist/random paths can diverge. No new RNG draws or save
fields are introduced. A resumed world must use the same scenario flags.

The .985 value is an exploratory contrast, not a historical estimate. This
screen does not fabricate an opening catalogue count from annual review totals.
Opening titles were seeded only 1-20 weeks before 1960; there is no multi-year
opening catalogue age distribution. Its historically appropriate size and
composition remain a separate empirical question.

## Reproduction and checks

Build with `dotnet build --no-restore`, then run:

```powershell
./SimTools/RunLpDemandProbe.ps1 -RunTag lpd-v2
python SimTools/analyze_lp_demand_probe.py --tag lpd-v2
```

The runner preserves full flags, seed, duration, assembly hash and exit results.
Native checks exercise the actual album decay function, soundtrack isolation,
default identity and shadow arithmetic. A probe-off replay and an enabled
13-week full-world save/load round-trip precede the matched 106-week arms.
Two isolated headless audit worlds may run concurrently; save checks run alone.

The analyzer reconciles every LP settlement week to market totals and label
sales, validates census identity and unchanged authored purchase capacity, and
replays the observed arm against the archived joint run. Calendar-year issue
counts come from launch-census years; units come from settlement years. First-
26-week yields include every eligible launch, treating weeks after retirement
as zero, and exclude launches without enough follow-up. They are not lifetime
profits or lifetime yields. Age, cohort, format, genre and current-tier reports
include off-chart records and retiring outcomes.

## Results: lpd-v2, seed 1001, 106 weeks

One seed, so the live treatment includes the subsequent reshuffle. The analyzer's
capacity check now asserts only the authored `baseCapacity` and
`albumChannelCapacity`. `purchaseCapacity` also reads live state: it differs in
2.8% of region-weeks, by at most 0.39%.

| 1961 (settlement year) | observe (.93) | slow (.985) |
|---|---|---|
| LP units cleared | 74.1M | 130.8M |
| Raw / serviceable demand | 82.4M / 74.1M | 200.3M / 181.7M |
| Album channel capacity | 139.3M | 139.3M |
| Cleared, LPs aged 0-8 weeks | 26.3M | 19.9M |
| Cleared, LPs aged 27-104 weeks | 14.2M | 67.7M |
| Units on charted LPs | 17.8M | 15.5M |
| Single units | 137.2M | 134.7M |

1960 LP units: 42.0M to 59.7M. The frozen-state slow shadow (202.8M raw) matches
the live slow raw demand (200.3M) closely at annual scale.

- **Slow decay fills the shared channel with catalogue.** The baseline is
  demand-bound: it clears all serviceable intent. The slow arm clears 72% of it,
  because 1961 sales hit the album channel capacity in most weeks. New LPs lose
  24% of their launch-window sales to their own back catalogue, and units on
  charted LPs fall.
- **The album chart clogs.** Of LPs whose first chart spell can be followed 26
  weeks, 5.6% stay on throughout under .93 and 31% under .985. Distinct entrants
  fall from 266 to 168. The mean first spell rises from 11.3 to 13.6 weeks.
- **It costs performance.** The slow arm ran 12% longer (1,772 s against
  1,581 s). It enters 1962 with 8,054 live records against 6,464.
- **Awareness is the largest in-scope shadow.** On the observed 1961 state, full
  awareness raises raw demand 3.8x (311.8M), against 1.3x without exhaustion
  (106.9M). Shadows are nonadditive.

Conclusion: do not lengthen ordinary-LP decay in the shared retail market.
Billboard's 6 August 1966 International Buyers Guide splits 1961 LP units into
stores 68M, racks 60M, and clubs and mail order 45M (173M). The model has one per-region retail capacity: record stores plus
department-store shelf. Rack jobbers appear only as extra department-store
shelf for proven records in regions their label cannot ship to; they add no
buyers. There is no club or mail-order channel. The in-scope comparison for
74.1M is therefore stores plus racks: 128M in 1961.

## Historical counting qualification

Billboard's 10 February 1962, p. 10 article, ["BMW Reviewers Dig 116 Singles Per
Week in '61"](https://retrocdn.net/images/b/ba/Billboard_US_1962-02-10.pdf#page=10),
identifies 2,927 LPs and 6,036 singles as numbers **reviewed**. It says most LPs
appeared in both mono and stereo, estimates receipt of 85-90% of U.S. records
across formats, and also mentions many foreign records. Contemporary paired
mono/stereo reviews support one review entry for the two versions, not a
two-SKU count. Reissues were eligible for review (for example [Mingus Revisited,
12 June 1965](https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1965/Billboard%201965-06-12.pdf)).
There is still no exhaustive rule here for repeat pressings, repackagings,
later stereo editions or import deduplication, nor a foreign subtotal.

Consequently 3,112 / 2,927 are approximate published review-volume references,
not hard targets for newly produced LP titles. Neither a uniform coverage
uplift nor a mono/stereo halving is supported. Retail unit targets and annual
review counts have different scopes. The same qualification affects the
national singles issue reference.
