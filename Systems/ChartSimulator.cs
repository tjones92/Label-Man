// Scripts/Systems/ChartSimulator.cs
using System.Collections.Generic;
using System.Linq;
using Godot;

public static class ChartSimulator {
	
	// =======================================================================
	// CONFIGURATION - Tuned for 1960 Reality
	// =======================================================================
	
	private const float BASE_POTENTIAL_AUDIENCE = 12000000f;
	private const float BASE_AWARENESS_GROWTH = 0.012f; 
	private const float RADIO_AWARENESS_MULT = 0.18f;
	private const float WORD_OF_MOUTH_MULT = 0.14f;     
	private const float ARTIST_HEAT_AWARENESS_BONUS = 0.18f;
	private const float AWARENESS_DECAY_RATE = 0.95f;
	
	private const float RADIO_QUALITY_WEIGHT = 0.7f;
	private const float RADIO_MOMENTUM_WEIGHT = 0.25f;
	private const float RADIO_LABEL_WEIGHT = 0.4f;
	// AIRPLAY WAS MIS-PHASED AGAINST SALES AT BOTH ENDS OF A RECORD'S LIFE, and it was the release
	// ramp that did it: the ramp put a six-week build on sales and left airplay on its old, faster
	// onset. Measured on d7-survey-decade-522-1001, airplay's share of a top-ten record's chart points
	// is U-shaped -- 77.3% in week one, bottoming at 37.1% at the week-nine sales peak, then climbing
	// back to 54.3% by week twenty:
	//
	//   week            1     4     8     12    17    20
	//   sales % peak  8.7%  33.5% 87.7% 70.9% 25.5%  8.2%
	//   airplay % pts 77.3% 47.8% 37.1% 45.2% 52.4% 54.3%
	//
	// Both ends are defects. In week one a record sells 8.7% of what it eventually will while already
	// carrying full campaign rotation, which is a large part of why debuts land near #73 instead of
	// #90. By week seventeen sales are a quarter of peak while airplay is over half the points, so
	// published points are roughly double what sales justify -- which is what holds records near the
	// top after they have commercially died, and the fat 3+ week number-one tail.
	//
	// The build is applied to the REGIONAL rotation rather than to radioHeat, deliberately. radioHeat
	// multiplies conversion directly in CalculateRegionalSales, so recomposing it moves the demand
	// model rather than the chart (section 11.7). Keeping the fix on the regional pass leaves units
	// alone -- radio's measured sales channel is only 1.07x across its observed range anyway.
	// Measured like-for-like at 52 weeks, the build moves airplay's TIMING and not its weight: its
	// share of a top-ten record's points falls 71.6% -> 56.2% in week one and 25.8% -> 15.2% at week
	// six, then converges back to baseline by week ten (26.2% -> 25.3%). radioHeat is untouched
	// (week eight 0.733 -> 0.759), which is the point -- the demand side must not move.
	//
	// The floor was tried at 0.50 as well. It barely reaches week one (56.2% either way, because the
	// launch seed is swamped by the lerp toward target before the chart is computed) and it costs the
	// named target: mean debut 81.3 -> 80.2 and debuts above #60 11.1% -> 13.3%. It does restore a
	// little mid-life level (week six 12.5% -> 15.2%), so if that level matters later the structural
	// fix is a faster regional lerp rather than a higher floor -- data.radioPlay = Lerp(radioPlay *
	// 0.92, target, 0.15) settles at only 0.688 of target and closes a gap by 0.782 a week, so it
	// takes about six weeks to catch up from any early suppression.
	private const int RADIO_BUILD_FULL_WEEK = 6;
	private const float RADIO_BUILD_FLOOR = 0.28f;
	// Rotation decays from the record's OWN sales peak, not from a fixed week-eight clock.
	//
	// A decline-keyed replacement for this term was built and REJECTED on decade evidence -- see
	// handoff section 12.4r before rebuilding it. `Lerp(0.15, 1, unitsThisWeek / peakWeeklyUnits)`
	// looked right (neutral through the climb, biting only past the peak) but was far GENTLER than
	// what it replaced: at week twenty it returned 0.366 where 0.88^12 gives 0.216. Airplay's share
	// of a top-ten record's points at week twenty went 52.9% -> 64.0%, sales at week twenty went 8.4%
	// -> 25.4% of peak, chart life 7.49 -> 8.31 and charting records 6,350 -> 5,719. It is also
	// self-reinforcing: more airplay -> higher rank -> more exposure -> more sales -> a higher support
	// ratio -> less burnout. A linear lerp from a floor CANNOT be both neutral during the climb and
	// severe in the tail: reaching 0.216 at 0.254 support requires a negative floor.
	//
	// What was wrong with the clock was its PHASE, not its existence. The release ramp moved the
	// sales peak to week nine, so `weeksSinceRelease > 8` began fatiguing a hit the week before it
	// peaked, while a marginal record that peaked at week four kept undamped rotation for five weeks
	// after it was commercially finished. Keying it to weeksSincePeakUnits fixes both ends at once
	// and costs nothing during the climb, where the term is now exactly 1.
	//
	// It is deliberately NOT deleted in favour of the station drop alone. Measured on
	// d7-buildonly-52-1001, a top-ten record's median radioHeat runs 0.738 at its week-nine peak and
	// 0.282 by week twenty; targetHeat with the fatigue term removed floors near 0.9 for those weeks,
	// because qualityFactor is an ageless constant (quality^1.8 * 0.7 is 0.51 of the target for a
	// quality-0.837 record). Rotation therefore rises 0.73 -> ~0.90 across weeks ten to fourteen,
	// which AIRPLAY_CONVEXITY turns into roughly 3.4x the airplay points exactly where the U-shaped
	// share is already too high. The station drop is linear in surviving panel reach and cannot
	// counter a 3.4x multiplicative rise; superseding the clock means re-keying it and letting the
	// drop end rotation outright, not running two decays or none.
	private const float RADIO_FATIGUE_DECAY = 0.88f;
	// THE STATION DROP. Stations dropped a record as a decision, not as an exponential: the playlist
	// held thirty or forty current slots, new records needed them, and a record that stopped moving
	// in the local sales reports was cut. That is why this is a weekly hazard evaluated per record
	// per region rather than another curve -- it ends rotation abruptly, at a different week in every
	// market, and the spread between those weeks is the tail variance the model is missing (the
	// coefficient of variation of peak-to-40%-of-peak sat flat at 0.24 through the whole ramp arc).
	//
	// The decision is read off the record's own peak, which is what a programme director had: a
	// one-stop's weekly reorder against last week's. unitsThisWeek/peakWeeklyUnits is 1 all the way
	// up the climb by construction, so the hazard cannot fire on a record that is still growing.
	private const int STATION_DROP_GRACE_WEEKS = 2;
	// Cut pressure opens once a record has slipped a fifth off its peak and is at its maximum once it
	// is down to a quarter. Read against the record's OWN peak, so a regional record on a small label
	// faces the same decision curve as a national smash and the mechanic is scale-free.
	//
	// The signal is national and the roll is per market, which is the deliberate split: a programme
	// director read his own market's one-stop reorders, but he also read the trades, and the model has
	// only seven regions, so a per-region support ratio would be a noisy read of a small number and
	// could latch a market out on one bad week. Replayed against the real trajectories of
	// d7-buildonly-52-1001, these values cut half the panel about six weeks after a top-ten record's
	// sales peak -- around the point it is falling through the twenties -- and 95% of it by week
	// twenty. A record still setting weekly highs faces a hazard of exactly zero at every setting.
	private const float STATION_DROP_SUPPORT_CEILING = 0.80f;
	private const float STATION_DROP_SUPPORT_FLOOR = 0.25f;
	private const float STATION_DROP_MAX_WEEKLY_CHANCE = 0.40f;
	// Burn: a record that has been in heavy rotation for months gets cut even while it sells, because
	// the audience is tired of it and the slot is worth more to something new. Deliberately a weak
	// backstop rather than a driver -- early-60s playlists turned over on sales, and callout-measured
	// burn is a later idea -- but it guarantees every record eventually leaves rotation, which a
	// support-only hazard does not.
	private const int STATION_DROP_BURN_ONSET_WEEKS = 8;
	private const int STATION_DROP_BURN_FULL_WEEKS = 8;
	// A record has to be on the air to be taken off it. Below this the region never latches, so a
	// record that breaks regionally months after release is still droppable when it eventually fades.
	private const float STATION_DROP_MIN_ROTATION = 0.01f;
	// What is left the week the drop lands. A playlist cut is abrupt, so this replaces the regional
	// lerp rather than feeding it; AIRPLAY_CONVEXITY of 5 makes 0.30 worth 0.24% of the region's
	// former airplay points, which is off the air in every sense the chart can see, while leaving a
	// residue that decays rather than a discontinuity at exactly zero.
	private const float STATION_DROP_RESIDUAL = 0.30f;
	// Being top 10 grants radio heat, and once airplay carries chart points that is positive
	// feedback: heat grants points, points hold the position, the position grants heat. The bonus
	// therefore has to be paid for in sales, so that when the sales which justified the rotation die
	// the rotation dies with them and the record falls. The floor sits between the top-10 tenth
	// percentile (20,918 units) and the 11-40 median (13,419): a record keeps its full bonus while it
	// is still selling like a genuine Top 40 record and tapers out over the three or four weeks after
	// its sales peak.
	private const float RADIO_POSITION_BONUS_SALES_FLOOR = 15000f;
	// Radio was half the story of a 60s hit -- sales spiked and decayed while stations kept a record
	// in rotation, and that rotation is what held a single at number one for weeks instead of one.
	// The airplay term existed but was inert: region.population is authored in millions and every
	// other absolute consumer multiplies it out (MarketRegion 104/124/131/169/225,
	// SingleOpportunityLedger 25, ChartManager 992), so summing it raw against unitsThisWeek left
	// airplay at 0.18% of a number-one record's points and made this a pure weekly-sales chart.
	// Measured consequence: 380 number ones a decade against a historical 203, 77% of them holding a
	// single week against 27%, and six 3+ week number ones against 84.
	//
	// Correcting the units alone overshoots by ~1000x, so the coefficient is re-derived instead:
	// this scales one million radio-reached listeners into the units-equivalent chart contribution
	// they are worth. Measured at 1960 (era weight 0.60), airplay is ~36% of a number one's points
	// against 0.18% before.
	private const float AIRPLAY_POINTS_PER_MILLION_REACHED = 2720f;
	// radioPlay is a saturating 0-1 rotation level, so it barely orders the chart: measured across a
	// 52-week run, radioHeat separates a number one from the bottom of the chart by only 1.48x where
	// sales separate them by 8.12x. Added linearly at a weight that makes airplay 31% of a number
	// one's points, it becomes 74% of a 41-100 record's -- a near-constant, and a near-constant
	// compresses every lead it is added to, since (S1+C)/(S2+C) is always nearer 1 than S1/S2. Adding
	// it linearly did exactly that: Top-40 life improved 9 -> 12 weeks but distinct number ones went
	// 38 -> 47 in the same 52 weeks, the opposite of the goal.
	//
	// So rotation is raised to a power before it is paid out, which is closer to the truth anyway --
	// a record in heavy rotation on the major stations gets six to ten times the spins of a
	// light-rotation record, not 1.5x. The reference play keeps the coefficient interpretable: a
	// record at reference earns what it would have earned linearly, weaker records earn
	// disproportionately less, a genuine smash disproportionately more.
	//
	// The exponent is 5 rather than 3 because it applies to the record's own rotation only, with
	// genre access divided out and paid back linearly (see CalculateChartPoints). Cubing the whole
	// product instead bought a much better chart -- 29 number ones a year against 39, 45% of them
	// one-week against 74% -- but it did so by cubing genre acceptance as well, and that is not a
	// plateau mechanism, it is a genre amplifier: over a decade run it drove Soul to a +26.4 chart
	// divergence and RnB to -4.7 as the era ramp compounded each genre's acceptance trend.
	//
	// Honest limit of this calibration: the record-level signal is still too flat to carry a plateau
	// on its own, because radioHeat is ~0.6 of generic quality-and-push shared by every charting
	// record plus a position bonus worth only 0.25. Raising the exponent spreads that thin signal but
	// amplifies its noise with it -- k=5 yields more 3+ week number ones than baseline (11% vs 8%)
	// AND more one-week ones (79% vs 74%), leaving total turnover flat at 38 against 39. What this
	// setting does bank is longevity: Top-40 median life 9 -> 11 weeks (in band), longest number-one
	// run 3 -> 5, entries 17.4/wk (in band), with the least genre distortion of any airplay variant
	// (top-three concentration 58.2% against a 52.7% baseline and 66.6% for the cubed product).
	// Reducing number-one turnover needs UpdateRadioHeat recomposed so heat is mostly earned rather
	// than mostly generic; that touches awareness and therefore demand, so it is its own change.
	private const float AIRPLAY_CONVEXITY = 5.0f;
	private const float AIRPLAY_REFERENCE_PLAY = 0.30f;
	// Top 40 radio consolidated across the decade and its chart influence grew with it, so a flat
	// weight is the wrong shape: 1960 is closer to a sales chart than 1968 is.
	private const int AIRPLAY_ERA_START_YEAR = 1960;
	private const int AIRPLAY_ERA_FULL_YEAR = 1968;
	private const float AIRPLAY_ERA_WEIGHT_EARLY = 0.60f;
	private const float AIRPLAY_ERA_WEIGHT_LATE = 1.00f;
	
	// THE CHART IS A SURVEY, NOT A CENSUS. Before 1973 Billboard polled roughly 110 outlets by hand --
	// 63 radio stations, 25 one-stops and 22 retailers -- and graded each return "very good" (20),
	// "good" (15) or "fair" (5), for a theoretical maximum of 1,645 sales points and 2,040 airplay
	// points. Every chart this model has ever produced ranked on an exact continuous read of the whole
	// live population instead, and that is a mechanism missing rather than a constant mistuned.
	//
	// It matters because sampling error is NOT demand noise: it reorders the chart without moving a
	// single unit, which is exactly what three separate misses need. Measured on
	// d7-hesb-decade-522-1001: mean weeks at number one 3.80 against a historical 2.57, only 5,150
	// distinct records charting against ~6,964, and a mean chart life of 9.23 weeks against 7.48. The
	// three are not independent -- records x mean life is pinned at 52,100 slot-weeks by the hundred
	// slots themselves -- and survey noise moves all of them together, because records near the cutoff
	// begin to flicker in and out instead of sitting stably.
	//
	// The diagnosis it replaces: the first guess was that rank exposure was sustaining leaders at the
	// top, and the fix proposed was a sales gate. Both were wrong. Chart life by peak band shows the
	// number-one band overshooting LEAST (17.67 -> 20.46, 1.16x) and the 41-70 band overshooting most
	// (4.21 -> 8.55, 2.03x), so the excess sits at the bottom of the chart, not the top. A sales gate
	// keyed on "selling like a record of that rank" is also circular: sales are what set the rank, so
	// by construction every record clears it.
	//
	// A record's sampling error scales with how many of the panel's outlets carry it at all, so the
	// error is small for a smash and large for a record scraping the hundred -- the same J-curve
	// Hesbacher describes, arriving from the measurement side.
	private const int SURVEY_PANEL_SIZE = 110;
	private const float SURVEY_FULL_REPORT_UNITS = 30000f;
	private const float SURVEY_MIN_PANEL_SHARE = 0.06f;
	private const float SURVEY_NOISE_SCALE = 1.0f;
	// Capped well below what 1/sqrt(n) alone would give the smallest reporting records. At 0.45 a
	// record carried by a handful of outlets could be published at twice its true score, which vaulted
	// marginal records onto the chart far too high: debuts above #60 went 14.7% -> 20.4% against a
	// historical 2.6%. The panel was rough about small records, not delusional about them.
	private const float SURVEY_MAX_SIGMA = 0.30f;

	private const float BASE_PURCHASE_RATE = 0.07f;
	private const float QUALITY_EXPONENT = 4.0f;
	// Neither of these is the sales curve's problem, and both have been measured saying so.
	// Across the sales peak of the 99 top-10 records of d7-airplay5-52-1001, the geometric-mean
	// week-over-week ratio was 0.9821 for saturation and 0.9924 for age decay, against an observed
	// 0.6970. Median saturation AT the sales peak is 0.0030 -- a hit has reached three tenths of one
	// percent of its potential audience, so there is no exhaustion to model. Do not tune these to
	// flatten the curve; the launch term below was carrying the entire fall.
	private const float SATURATION_POWER = 0.45f;
	private const float DEMAND_AGE_DECAY_RATE = 0.91f;
	// A 1960s single was not born at its peak. It shipped to a fraction of the market, earned
	// rotation week by week, and reached full availability over roughly six weeks. That is why the
	// average Hot 100 debut position was #86.8, why 75.7% of debuts landed in the bottom twenty, and
	// why no record debuted inside the top ten until "Hey Jude" in 1968 -- which entered at #10,
	// reached #3, then held #1 for nine weeks from its third week on the chart.
	//
	// What stood here was the exact inverse: a launch multiplier of 2.0 + push*2.5 (3.25x at a
	// typical push) in week one, decaying to 1.0 by week four. Measured, that one term supplied a
	// 0.6995 week-over-week ratio across the sales peak against an observed 0.6970 -- the whole 30%
	// fall. Its consequences were a sales peak and a chart peak both at week 2, 87.6% of charting
	// records debuting at their peak position, a #1 record whose median debut position was #1, and a
	// debut distribution that was uniform across all ten deciles against a history concentrated
	// 44.2% in 91-100.
	//
	// The plateau this arc has been trying to build was already underneath it. With the launch term
	// divided out, latent demand for a number one runs 47/100/89/77/72/66/63/62 over its first eight
	// weeks. Multiplying that by a ramp rather than a spike turns it into a five-week shelf at
	// 88-100% of peak, which is what holds a record at number one for more than a single week.
	//
	// The floor is where a record starts, not where a weak record stays: push widens the opening
	// shipment but cannot skip the ramp, because national distribution and radio rotation took weeks
	// to build for anyone. At a typical push of 0.5 the floor is 0.28, which puts week-one sales at
	// 20% of an eventual number one's peak and 29% of a top-ten record's -- inside the historical
	// 20-35% and 25-40% bands.
	// A single ramp length applied to every record was the first version of this, and it failed in a
	// way worth recording: it moved every record onto the SAME schedule, so the whole chart climbed
	// in lockstep, nobody crossed anybody, and the leader simply outlasted its challengers. Top-ten
	// entrants halved (103 -> 59 across 52 weeks) while top-ten dwell doubled, and mean weeks at
	// number one went to 3.5 against a historical 2.57 -- and that was measured at 1960, the year
	// with the weakest airplay era weight, so the decade would have been worse. The number-one
	// margin over the runner-up was 1.074, i.e. the leader was not winning by much; it was winning
	// unopposed.
	//
	// Ramp length therefore varies by campaign. A national push shipped to every market at once and
	// bought rotation immediately; a small label's record crept outward region by region on local
	// airplay and jukebox play, which is the slow regional-to-national breakout this model already
	// simulates elsewhere. Records now peak at different weeks, cross each other, and displace each
	// other.
	// KNOWN OPEN ISSUE, with three hypotheses already falsified against it. This ramp overshoots
	// number-one tenure: 3.71 mean weeks at 1960 against a historical 2.57, and 50% of number ones
	// holding 3+ weeks against 41%. That is measured at 1960, the weakest airplay-era-weight year,
	// so the decade will read worse.
	//
	// It is NOT that challengers are scarce -- that was the first guess and the telemetry refutes it.
	// Records within 10% of the leader's points went 1 -> 2 and within 25% went 3 -> 5, so the
	// contender pool GREW. It is not the ramp length: 5 weeks against 6 moved nothing and the peak
	// stayed at week 8 either way, because the top-ten feedback loop rather than the ramp is what
	// sets the peak. It is not AIRPLAY_CONVEXITY: 5 -> 3 left tenure at 3.47.
	//
	// The cause is volatility, not level. The median week-over-week change in the number-one to
	// number-two points gap fell from 0.2497 to 0.0496 while the gap itself only moved 1.149 ->
	// 1.074. Under the old spiky curves the lead was smaller than its own weekly noise, so the
	// ordering flipped almost every week -- 77% one-week number ones, far too MUCH churn. Smooth
	// plateaus cut that noise five- to sixfold and ordering became persistent. The historical
	// distribution is bimodal (27% at one week AND 41% at three or more), which needs a genuine
	// appeal separation at the top plus enough weekly noise to displace marginal number ones.
	// Whatever supplies that noise belongs in the airplay pass, where station adds and drops were
	// genuinely lumpy, and not in demand.
	//
	// Per-record ramp dispersion by campaign was tried as a fix and rejected: it de-synchronised the
	// pack but barely moved tenure (3.71 -> 3.25) while taking top-ten debuts from 2 to 6 across 52
	// weeks, roughly sixty a decade against the one the Hot 100 saw before 1970.
	private const float RELEASE_RAMP_FLOOR_BASE = 0.20f;
	private const float RELEASE_RAMP_FLOOR_PUSH = 0.16f;
	private const int RELEASE_RAMP_FULL_WEEK = 6;
	// The ramp is LINEAR. A convex ramp has now been tried and rejected TWICE, on opposite sides of
	// the Hesbacher change, and the second test is the informative one. The first rejection (flat
	// chart) moved mean debut 74.9 -> 74.0; the retest on the steep chart, where the same points
	// shortfall should have cost far more positions, moved it 77.2 -> 76.7 and cost chart life
	// (6.94 -> 6.01). Debut position is not a function of this ramp. Do not try it a third time.
	//
	// What debut IS a function of: the week-over-week growth rate at the moment of entry, against the
	// density of published points around the cutoff. A record clears #100 and then passes every rank
	// whose points its next week exceeds. Measured on d7-survey-decade-522-1001 the published curve
	// runs #75 at 10.4% of a number one and #100 at 8.2%, a ratio of 1.27, where Hesbacher wants
	// 295/178 = 1.66. A record growing 30-40% in its entry week therefore vaults from #100 to about
	// #73 -- which is exactly the 41-70 band's observed median debut of #73.
	//
	// So the debut distribution is downstream of how steep the BOTTOM of the published curve is, and
	// the lever for that is CHART_EXPOSURE_EXPONENT, which is currently entangled with Soul's chart
	// divergence (section 12.4i). Sequence the Soul authoring fix first.
	// Reshaping the curve without rescaling it costs 28.9% of Single units at first order, measured
	// by reweighting every record-week of d7-airplay5-52-1001 by the new ramp over the old launch
	// term. Decade Single units are an accepted result, so the ramp is renormalised to hold them.
	// This is deliberately its own constant rather than a change to BASE_PURCHASE_RATE: it is a
	// consequence of the shape change, and the first-order estimate ignores the awareness, momentum
	// and chart-position feedbacks that will amplify the cut. RE-DERIVE IT from the realised units
	// of the run that follows this change rather than trusting 1.41.
	private const float RELEASE_RAMP_UNIT_RENORMALIZATION = 1.41f;
	private const float LegacyMajorDemandScale = 0.60f;
	private const float LegacyMidTierDemandScale = 0.85f;
	
	private const float TOP_5_VISIBILITY_MULT = 4.5f;
	private const float TOP_10_VISIBILITY_MULT = 3.0f;
	private const float TOP_20_VISIBILITY_MULT = 2.0f;
	private const float TOP_40_VISIBILITY_MULT = 1.4f;
	private const float TOP_100_VISIBILITY_MULT = 1.0f;
	
	private const float WEEKLY_SALES_PER_RECORD_STORE = 250f;
	private const float WEEKLY_SALES_PER_DEPT_STORE = 500f;

	// Rack jobbers ran the record departments of department stores, discount chains and
	// supermarkets (handoff section 33.1 stage 2). They stocked narrow, high-turn inventory
	// -- the proven hits -- so the rack is an amplifier of a record that is already selling,
	// never a way to break an unproven one. Their share of retail grew across the decade at
	// the expense of the mom-and-pop record store.
	//
	// The authored departmentStoreCount is a 1960 baseline and stays intact: gating it on
	// proof instead cut every unproven record's shelf by ~79% and every 1960 record's by 60%,
	// which crowded the chart onto incumbents and dropped cumulative breadth below the
	// reference run. What a proven record earns is extra rack space on top of the authored
	// baseline, and the decade's shift toward rack retail scales that bonus rather than the
	// baseline (section 12: do not rewrite an accepted calibration to add a mechanism).
	private const float RACK_ERA_FLOOR = 0.30f;
	private const int RACK_ERA_START_YEAR = 1960;
	private const int RACK_ERA_FULL_YEAR = 1969;
	private const float RACK_MAX_SHELF_BONUS = 0.80f;
	/// <summary>
	/// A jobber restocking its own racks with a record that turns over is a real but partial
	/// substitute for the label being able to ship to that market itself. Lifting an uncovered
	/// record all the way to parity overstated it physically and, because the lift only reaches
	/// records that are already proven, amplified the biggest sellers on a hundred-slot chart
	/// and cost cumulative breadth.
	/// </summary>
	internal const float RackServiceShareOfDistributed = 0.50f;
	private const float RACK_REGIONAL_PROOF_FLOOR = 0.30f;
	private const float RACK_REGIONAL_PROOF_FULL = 0.55f;
	
	private const float HIT_MOMENTUM_BONUS = 0.3f;
	
	private const float BASE_INERTIA = 0.80f;       
	private const float INERTIA_QUALITY_OVERRIDE = 0.15f;
	private const float MIN_SALES_FOR_FULL_INERTIA = 8000f;
	
	private const float MOMENTUM_SMOOTHING = 0.22f;     
	private const float MOMENTUM_QUALITY_FLOOR = -0.12f;
	private const float MOMENTUM_CLAMP = 0.35f;
	
	// =======================================================================
	// MAIN UPDATE
	// =======================================================================
	
	public static void UpdateRecord(RecordRuntimeData record, AILabel label, float genreAcceptance, float artistHeat) {
		record.artistHeat = artistHeat;
		float quality = record.GetQuality();
		
		UpdateLabelPush(record, label);
		UpdateRadioHeat(record, label, quality, genreAcceptance);
		UpdateAwareness(record, quality);
		UpdateWordOfMouth(record, quality);
	}
	
	public static void FinalizeWeeklySales(RecordRuntimeData record, int totalSales) {
		record.unitsPreviousWeek = record.unitsThisWeek;
		record.unitsThisWeek = totalSales;
		record.totalUnitsSold += totalSales;
		if (totalSales > record.peakWeeklyUnits) {
			record.peakWeeklyUnits = totalSales;
			record.weeksSincePeakUnits = 0;
		} else record.weeksSincePeakUnits++;
		UpdateMomentum(record);
	}
	
	// =======================================================================
	// REGIONAL SALES CALCULATION
	// =======================================================================
	
	public static int CalculateRegionalSales(
		RecordRuntimeData record, 
		MarketRegion region, 
		RegionalRecordData regionalData,
		float quality,
		float genreAcceptance,
		int year,
		int month,
		bool liveTick,
		int internalChartPosition,
		AILabel label,
		float singleOpportunityNormalization = 1f)
	{
		// === 1. POTENTIAL BUYERS ===
		float populationMillions = region.population;
		float buyingPercentage = region.GetBuyingPopulationPercentage();
		float potentialBuyers = populationMillions * 1000000f * buyingPercentage;
		
		// === 2. AWARENESS FILTER ===
		float effectiveAwareness = (record.awareness * 0.4f) + (regionalData.awareness * 0.6f);
		bool stagedLiveDemand = GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true;
		
		if (record.currentPosition > 0 && record.currentPosition <= 10) {
			effectiveAwareness = Mathf.Max(effectiveAwareness, 0.7f);
		} else if (record.currentPosition > 0 && record.currentPosition <= 40) {
			effectiveAwareness = Mathf.Max(effectiveAwareness, 0.4f);
		}
		regionalData.salesRecordAwarenessThisWeek = record.awareness;
		regionalData.salesRegionalAwarenessThisWeek = regionalData.awareness;
		regionalData.salesEffectiveAwarenessThisWeek = effectiveAwareness;
		regionalData.salesRadioHeatThisWeek = record.radioHeat;
		regionalData.salesRegionalRadioPlayThisWeek = regionalData.radioPlay;
		
		float baselineAwareness = Mathf.Clamp(effectiveAwareness, 0f, 1f);
		
		// === 3. MARKET EXHAUSTION ===
		float potentialAudience = GetRegionalPotentialAudience(record, region, quality);
		
		float regionalSold = regionalData.unitsSoldTotal;
		float penetration = regionalSold / Mathf.Max(1f, potentialAudience);
		
		float exhaustionFactor = 1f / (1f + Mathf.Pow(penetration * 3f, SATURATION_POWER));
		exhaustionFactor = Mathf.Max(exhaustionFactor, 0.08f);
		
		// === 4. DEMAND CURVE ===
		float demandCurve = Mathf.Pow(quality, QUALITY_EXPONENT);
		float conversionRate = BASE_PURCHASE_RATE * demandCurve * exhaustionFactor;
		// The high-volume label families dominate every measured sales window.
		// Keep indie-family conversion intact instead of applying another blanket
		// purchase-rate reduction that erases their narrow charting margin.
		if (stagedLiveDemand) conversionRate *= GetLiveLabelDemandScale(label, record.baseRecord?.recordId);
		else if (label?.tier == LabelTier.Major) conversionRate *= LegacyMajorDemandScale;
		else if (label?.tier == LabelTier.MidTier) conversionRate *= LegacyMidTierDemandScale;
		
		// === 5. CHART VISIBILITY BONUS ===
		float chartVisibility = GetChartVisibilityMultiplier(internalChartPosition);
		if (internalChartPosition <= 0) {
			// Proven local discovery softens, but never erases, the uncharted moat.
			// Even the strongest regional signal remains below #100's 1.0 exposure.
			float regionalDiscovery = Mathf.Clamp((regionalData.breakoutScore - 0.24f) / 0.40f, 0f, 1f);
			regionalDiscovery = Mathf.Max(regionalDiscovery, regionalData.neighboringMarketTestStrength * 0.60f);
			chartVisibility = 0.40f + regionalDiscovery * 0.55f;
		}
		regionalData.breakoutVisibilityMultiplier = chartVisibility;
		float chartSignal = Mathf.Max(.01f, chartVisibility);
		if (!stagedLiveDemand) conversionRate *= chartVisibility;
		// The J-curve. chartVisibility above is a five-step ladder that enters the staged model only
		// through the geometric-mean discovery term, where a 4.5x spread is cube-rooted to 1.65x and
		// cannot express Hesbacher at all. This carries the rank curve directly instead: it is
		// purchase exposure -- rack space, jukebox slots, the listening booth -- rather than
		// discovery, so it belongs on conversion and not inside the awareness odds.
		conversionRate *= GetChartExposureWeight(internalChartPosition);
		
		// === 6. RELEASE RAMP ===
		conversionRate *= GetReleaseRampWeight(record.weeksSinceRelease, record.currentLabelPush) *
			RELEASE_RAMP_UNIT_RENORMALIZATION;
		
		// === 7. MOMENTUM BONUS ===
		float momentumBonus = 1f + Mathf.Clamp(record.momentum, -0.2f, 0.5f);
		if (!stagedLiveDemand) conversionRate *= momentumBonus;

		// Records eventually leave the active demand cycle even when chart
		// visibility keeps their effective awareness artificially high.
		if (record.weeksSinceRelease > 8) {
			int weeksOverThreshold = record.weeksSinceRelease - 8;
			conversionRate *= Mathf.Pow(DEMAND_AGE_DECAY_RATE, weeksOverThreshold);
		}
		
		// === 8. OTHER MODIFIERS ===
		bool useGenreMarketV2DemandTransfer = stagedLiveDemand;
		if (useGenreMarketV2DemandTransfer) {
			conversionRate *= GenreAcceptanceService.GetEnabledSingleDemandMultiplier(genreAcceptance);
			if (singleOpportunityNormalization != 1f) conversionRate *= singleOpportunityNormalization;
		} else conversionRate *= 0.6f + genreAcceptance * 0.5f;
		conversionRate *= GenreAcceptanceService.GetLiveFormatMultiplier(record.baseRecord.primaryGenre,
			record.baseRecord.secondaryGenre, ReleaseFormat.Single, year,
			region.GetAlbumOpportunityWeight(record.baseRecord.primaryGenre, year, useGenreMarketV2DemandTransfer),
			useGenreMarketV2DemandTransfer);
		if (useGenreMarketV2DemandTransfer) conversionRate *= GenreAcceptanceService.GetLiveSpecialistSingleOpportunityNormalizer(
			record.baseRecord.primaryGenre, record.baseRecord.secondaryGenre, year, live: true);
		if (!stagedLiveDemand) conversionRate *= 0.75f + record.radioHeat * 0.5f;
		conversionRate *= 0.75f + Mathf.Max(0, regionalData.sentiment) * 0.25f;
		conversionRate *= record.GetAwardMultiplier();
		conversionRate *= 1f - (region.distribution.difficulty * 0.3f);
		conversionRate *= MarketSeasonality.GetSingleSalesMultiplier(year, month, liveTick);
		
		// The enabled staged model requires a bounded baseline. The disabled branch
		// retains its historical un-clamped awareness value and rounding contract.
		float awareBuyers = potentialBuyers * (stagedLiveDemand ? baselineAwareness : effectiveAwareness);
		if (stagedLiveDemand) {
			SingleDemandStages stages = CalculateSingleDemandStages(potentialBuyers, baselineAwareness, chartSignal,
				Mathf.Max(.01f, momentumBonus), Mathf.Max(.01f, .75f + record.radioHeat * .5f), demandCurve,
				genreAcceptance, GenreAcceptanceService.GetLiveFormatMultiplier(record.baseRecord.primaryGenre,
					record.baseRecord.secondaryGenre, ReleaseFormat.Single, year,
					region.GetEnabledAlbumOpportunityWeight(record.baseRecord.primaryGenre, year), true),
				conversionRate / Mathf.Max(.000001f, BASE_PURCHASE_RATE * demandCurve));
			awareBuyers = stages.AwareBuyers;
			conversionRate = stages.IntrinsicConversionRate;
			regionalData.demandPotentialAudience = stages.PotentialAudience;
			regionalData.demandBaselineAwareness = stages.BaselineAwareness;
			regionalData.demandEarnedDiscoveryExposure = stages.EarnedDiscoveryExposure;
			regionalData.demandAwareBuyers = stages.AwareBuyers;
			regionalData.demandIntrinsicQualityFactor = stages.IntrinsicQualityFactor;
			regionalData.demandAcceptanceFactor = stages.AcceptanceFactor;
			regionalData.demandFormatFactor = stages.FormatFactor;
			regionalData.demandIntrinsicConversionRate = stages.IntrinsicConversionRate;
			regionalData.demandChartSignal = chartSignal;
			regionalData.demandMomentumSignal = Mathf.Max(.01f, momentumBonus);
			regionalData.demandRadioSignal = Mathf.Max(.01f, .75f + record.radioHeat * .5f);
		}
		float rawSales = awareBuyers * conversionRate;
		// Backorders represent recent unmet intent, not a permanent bank of future
		// purchases. Most stale intent expires before this week's demand is added.
		regionalData.unitsBackordered = Mathf.RoundToInt(regionalData.unitsBackordered * 0.35f);
		regionalData.rawDemandThisWeek = rawSales;
		bool captureBreakoutDiagnostic = !record.baseRecord.isPlayerOwned &&
			record.weeksSinceRelease >= 1 &&
			record.weeksSinceRelease <= 14;
		if (captureBreakoutDiagnostic) {
			regionalData.breakoutDiagnosticAge = record.weeksSinceRelease;
			regionalData.breakoutWeekStartStock = regionalData.unitsInStores;
			regionalData.breakoutRawSales = rawSales;
			regionalData.breakoutAwareBuyers = awareBuyers;
			regionalData.breakoutConversionRate = conversionRate;
		}
		
		// === 9. SUPPLY CONSTRAINTS ===
		float storeCapacity = region.distribution.recordStoreCount * WEEKLY_SALES_PER_RECORD_STORE;
		// Department-store shelf: the authored baseline, plus rack shelf a proven record earns
		// in a market its label cannot ship to itself. Applying that bonus to every proven
		// record instead simply amplified the biggest sellers, and on a hundred-slot chart an
		// amplifier is zero-sum -- it pushed marginal independents off and took cumulative
		// breadth back to the reference run. Where the label already has a network the rack is
		// part of the authored baseline; where it has none, the jobber buying a record that
		// turns over is the only way onto that market's shelves.
		bool labelShipsHere = label.HasDistributionInRegionForRecord(region.regionId, record.baseRecord?.recordId);
		float rackShelf = labelShipsHere ? 1f : GetRackJobberShelfMultiplier(record.currentPosition,
			regionalData?.peakBreakoutScore ?? 0f, TimeManager.Instance?.CurrentDate.year ?? RACK_ERA_START_YEAR);
		float deptCapacity = region.distribution.departmentStoreCount * WEEKLY_SALES_PER_DEPT_STORE * rackShelf;
		float totalCapacity = (storeCapacity + deptCapacity) * region.distribution.inventoryDepth;

		if (record.currentPosition > 0 && record.currentPosition <= 20) {
			totalCapacity *= 1.5f;
		}

		// A former indie-distribution penalty stood here. It tested
		// "!hasIndieDistribution && !hasOneStopDistributors", but every authored region has
		// one-stops, so the branch was unreachable in every run this model has ever done --
		// and it keyed off labelId being non-null rather than off the label being an
		// independent, so it would have charged majors identically had it fired. Access for a
		// label without its own network in a region is now carried by the coverage model and
		// by the rack channel above.

		if (regionalData.unitsInStores < rawSales) {
			regionalData.unitsBackordered += Mathf.RoundToInt(rawSales - regionalData.unitsInStores);
			rawSales = regionalData.unitsInStores;
		}
		if (captureBreakoutDiagnostic) {
			regionalData.breakoutBackordersBeforeRestock = regionalData.unitsBackordered;
		}
		
		rawSales = Mathf.Min(rawSales, totalCapacity);
		rawSales *= (float)GD.RandRange(0.96, 1.04);
		if (!(GenreMarketV2.Enabled && ChartManager.Instance?.IsGenreMarketV2Live == true)) {
			// Frozen disabled/prewarm behavior, including the historical post-jitter
			// rounding semantics, is intentionally retained unchanged.
			return Mathf.Max(0, Mathf.RoundToInt(rawSales));
		}
		regionalData.storeCapacityThisWeek = Mathf.Max(0, Mathf.FloorToInt(totalCapacity));
		// Jitter is deliberately drawn in the legacy order.  The live caller may
		// subsequently ration this serviceable intent against the common market.
		regionalData.serviceableIntentThisWeek = Mathf.Clamp(Mathf.RoundToInt(rawSales), 0,
			Mathf.Min(regionalData.unitsInStores, regionalData.storeCapacityThisWeek));
		return regionalData.serviceableIntentThisWeek;
	}
		
	/// <summary>Pure enabled Single demand stages; discovery is owned only here.</summary>
	internal static SingleDemandStages CalculateSingleDemandStages(float potentialAudience, float baselineAwareness,
		float chartSignal, float momentumSignal, float radioSignal, float intrinsicQualityFactor,
		float acceptanceFactor, float formatFactor, float otherConversionFactor) {
		float boundedBase = Mathf.Clamp(baselineAwareness, 0f, 1f);
		// Move the historical discovery multipliers into awareness odds exactly once.
		// Chart, momentum, and radio are correlated views of the same discovery event,
		// so use their geometric mean instead of compounding all three as independent
		// multipliers. One stays neutral, equally weak/strong signals retain their
		// level, and a second or third signal cannot multiply the audience again.
		float discoveryProduct = Mathf.Max(.000001f, chartSignal) *
			Mathf.Max(.000001f, momentumSignal) * Mathf.Max(.000001f, radioSignal);
		float discoveryMultiplier = Mathf.Pow(discoveryProduct, 1f / 3f);
		float awareFraction = boundedBase <= 0f ? 0f : boundedBase >= 1f ? 1f :
			boundedBase * discoveryMultiplier / (1f - boundedBase + boundedBase * discoveryMultiplier);
		float exposure = awareFraction > boundedBase
			? (awareFraction - boundedBase) / Mathf.Max(.000001f, 1f - boundedBase)
			: 0f;
		float conversion = BASE_PURCHASE_RATE * Mathf.Max(0f, intrinsicQualityFactor) * Mathf.Max(0f, otherConversionFactor);
		return new SingleDemandStages(Mathf.Max(0f, potentialAudience), boundedBase, exposure,
			Mathf.Max(0f, potentialAudience) * Mathf.Clamp(awareFraction, 0f, 1f), intrinsicQualityFactor,
			acceptanceFactor, formatFactor, conversion);
	}

	/// <summary>
	/// Realized Single demand follows the distribution capabilities a label has now,
	/// rather than the tier it had when it was generated. The former fixed switch
	/// continued to charge Independent labels 0.55 after they built national reach
	/// while granting Boutique labels 1.20 regardless of reach. That made promotion,
	/// self-built expansion, and completed distribution deals largely cosmetic at
	/// the chart-access seam. This continuous scale retains the calibrated national
	/// label ceiling while allowing earned reach to change outcomes.
	/// </summary>
	internal static float GetLiveLabelDemandScale(AILabel label) =>
		label == null ? 1f : CalculateLiveLabelDemandScale(label.distributionStrength, label.effectiveNationalReach);

	/// <summary>
	/// Demand scale for one release. A distribution deal carries the record that
	/// earned it and the label's subsequent output, so a record outside the contract
	/// sells on the label's own reach rather than the distributor's borrowed network.
	/// </summary>
	internal static float GetLiveLabelDemandScale(AILabel label, string recordId) =>
		label == null ? 1f : CalculateLiveLabelDemandScale(
			label.DistributionStrengthForRecord(recordId), label.EffectiveNationalReachForRecord(recordId));

	internal static float CalculateLiveLabelDemandScale(float distributionStrength, float nationalReach) =>
		Mathf.Clamp(0.45f + Mathf.Clamp(distributionStrength, 0f, 1f) * 0.55f +
			Mathf.Clamp(nationalReach, 0f, 1f) * 0.35f, 0.55f, 1.20f);

	private static float GetGenreMarketReach(Genre genre) {
		return genre switch {
			Genre.TraditionalPop => 0.95f,
			Genre.RockAndRoll => 0.85f,
			Genre.Soul => 0.70f,
			Genre.RnB => 0.65f,
			Genre.TeenPop => 0.75f,
			Genre.DooWop => 0.60f,
			Genre.Country => 0.50f,
			Genre.Gospel => 0.35f,
			Genre.Jazz => 0.40f,
			Genre.Folk => 0.45f,
			Genre.BritishInvasion => 0.80f,
			Genre.Psychedelic => 0.50f,
			Genre.SurfRock => 0.55f,
			_ => 0.60f
		};
	}

	/// <summary>
	/// The share of its eventual market a record can reach this week. Distribution breadth and radio
	/// rotation both start near zero and build, so this rises from a push-widened floor to 1.0 and
	/// stays there. Everything after the ramp completes is owned by age decay, saturation and the
	/// awareness stock -- this term never causes a decline. A heavier campaign opens wider but runs
	/// the same six weeks, because national distribution and radio rotation took weeks to build for
	/// anyone; see the constants above for why varying the length by campaign was tried and rejected.
	/// </summary>
	internal static float GetReleaseRampWeight(int weeksSinceRelease, float labelPush) {
		if (weeksSinceRelease >= RELEASE_RAMP_FULL_WEEK) return 1f;
		float floor = RELEASE_RAMP_FLOOR_BASE + Mathf.Clamp(labelPush, 0f, 1f) * RELEASE_RAMP_FLOOR_PUSH;
		float progress = Mathf.Clamp((weeksSinceRelease - 1f) / (RELEASE_RAMP_FULL_WEEK - 1f), 0f, 1f);
		return Mathf.Lerp(floor, 1f, progress);
	}

	// Hesbacher's Billboard weighting, adapted to the 1960s Hot 100:
	//
	//     y(x) = 4139 - 4357 * x / (x + 10)
	//
	// the "appropriate proportion of designated popularity" a rank commands. It reproduces the
	// authored tier table exactly -- 3,743 at #1, 1,960 at #10, 1,027 at #25, 508 at #50, 295 at #75,
	// 178 at #100 -- and its J-curve of inequality is the shape this chart was missing. Pre-1973
	// Billboard polled ~110 outlets by hand (63 stations, 25 one-stops, 22 retailers), so rank was
	// always a survey-weighted composite rather than a units count, and this curve is what that
	// composite produced.
	//
	// Measured against it, the model's curve was far too flat below #20: 17.4% of the number one's
	// points at #90 against Hesbacher's 5.8%, and #100 at 16.2% against 4.8%. The cause was not a
	// bloated tail -- #100 sells 5,377 a week against a historical ~7,200, so the bottom of the chart
	// is about right -- but a missing top: #1 sold 28,838 against a historical ~150,000. The chart was
	// flat because the hits were absent.
	//
	// NOTE ON LEVEL vs SHAPE. Summing the authored per-rank sales across all 100 ranks at #1 = 150,000
	// needs 3.23M units a week on the chart against a total Single market of 2.85M/week, i.e. the top
	// hundred would be 113% of everything sold. The shape is therefore implemented and the level is
	// left to fall out of the calibrated total, which puts #1 nearer 60-80k. Raising the level is a
	// separate decision about total market size -- 148M Singles a year is itself roughly 22% under
	// real 1960 US volume.
	private const float HESBACHER_INTERCEPT = 4139f;
	private const float HESBACHER_SCALE = 4357f;
	private const float HESBACHER_HALF_RANK = 10f;
	// Rank already earns exposure elsewhere -- through GetChartVisibilityMultiplier, the top-10 and
	// top-40 awareness floors, the top-20 shelf-capacity bonus and the rack-jobber channel -- so
	// paying the raw 21x Hesbacher spread again would compound to roughly 113x between #1 and #100
	// against the 20.8x the tier table wants. The exponent is the fitted share of the curve this term
	// carries: 21^0.44 = 3.86, which is the extra spread needed on top of the 5.4x the demand model
	// already produces. Position feeds back on itself, so the realised spread will exceed this
	// first-order figure -- re-derive it from the probe rather than trusting 0.44.
	private const float CHART_EXPOSURE_EXPONENT = 0.44f;

	private static readonly float ChartExposureMean = ComputeChartExposureMean();

	private static float ComputeChartExposureMean() {
		float total = 0f;
		for (int rank = 1; rank <= 100; rank++) total += RawChartExposure(rank);
		return total / 100f;
	}

	private static float RawChartExposure(int position) {
		float x = Mathf.Clamp(position, 1, 100);
		float weight = HESBACHER_INTERCEPT - HESBACHER_SCALE * x / (x + HESBACHER_HALF_RANK);
		float floorWeight = HESBACHER_INTERCEPT - HESBACHER_SCALE * 100f / (100f + HESBACHER_HALF_RANK);
		return Mathf.Pow(weight / floorWeight, CHART_EXPOSURE_EXPONENT);
	}

	/// <summary>
	/// Exposure a chart rank buys, normalised to average 1 across the hundred slots so this reshapes
	/// the chart without moving total units. An uncharted record is treated as the bottom of the
	/// chart rather than worse than it, because the charted/uncharted gap is already owned by
	/// GetChartVisibilityMultiplier and must not be charged twice.
	/// </summary>
	internal static float GetChartExposureWeight(int position) =>
		RawChartExposure(position <= 0 ? 100 : position) / ChartExposureMean;

	private static float GetChartVisibilityMultiplier(int position) {
		if (position <= 0) return 0.4f;
		if (position <= 5) return TOP_5_VISIBILITY_MULT;
		if (position <= 10) return TOP_10_VISIBILITY_MULT;
		if (position <= 20) return TOP_20_VISIBILITY_MULT;
		if (position <= 40) return TOP_40_VISIBILITY_MULT;
		return TOP_100_VISIBILITY_MULT;
	}

	// Returns the furthest position an established record may fall this week.
	// Low-quality novelty records receive less protection; quality itself never adds
	// protection beyond BASE_INERTIA. Weak sales and sustained decline remove it.
	public static int GetInertiaPositionCap(RecordRuntimeData record, int previousPosition, int rawPosition) {
		if (previousPosition <= 0 || rawPosition <= previousPosition) return rawPosition;
		if (record.unitsThisWeek <= 0 || record.weeksNegative >= 3 || record.momentum <= -0.20f) return rawPosition;

		float salesGate = Mathf.Clamp(record.unitsThisWeek / MIN_SALES_FOR_FULL_INERTIA, 0f, 1f);
		float quality = record.GetQuality();
		float qualityAdjustment = (1f - quality) * INERTIA_QUALITY_OVERRIDE;
		float inertia = Mathf.Max(0f, BASE_INERTIA - qualityAdjustment) * salesGate;

		if (previousPosition <= 40 && record.momentum > 0f) {
			inertia = Mathf.Min(BASE_INERTIA, inertia + record.momentum * HIT_MOMENTUM_BONUS * salesGate);
		}

		int rawDrop = rawPosition - previousPosition;
		int allowedDrop = Mathf.Max(1, Mathf.CeilToInt(rawDrop * (1f - inertia)));
		return previousPosition + allowedDrop;
	}
		
	// =======================================================================
	// RADIO HEAT
	// =======================================================================
		
	private static void UpdateRadioHeat(RecordRuntimeData record, AILabel label, float quality, float genreAcceptance) {
		float qualityFactor = Mathf.Pow(quality, 1.8f) * RADIO_QUALITY_WEIGHT; 
		float pushFactor = record.currentLabelPush * RADIO_LABEL_WEIGHT;
		float momentumFactor = Mathf.Max(0, record.momentum) * RADIO_MOMENTUM_WEIGHT;
		
		float targetHeat = (qualityFactor + pushFactor + momentumFactor) * genreAcceptance;
		targetHeat += record.artistHeat * 0.12f;
		
		// Earned by sales, not by the position itself -- see RADIO_POSITION_BONUS_SALES_FLOOR.
		float positionBonusGate = Mathf.Clamp(record.unitsThisWeek / RADIO_POSITION_BONUS_SALES_FLOOR, 0f, 1f);
		if (record.currentPosition > 0 && record.currentPosition <= 10) {
			targetHeat += 0.25f * positionBonusGate;
		} else if (record.currentPosition > 0 && record.currentPosition <= 40) {
			targetHeat += 0.1f * positionBonusGate;
		}
		
		if (record.weeksSincePeakUnits > 0) {
			targetHeat *= Mathf.Pow(RADIO_FATIGUE_DECAY, record.weeksSincePeakUnits);
		}

		float lerpRate = (targetHeat > record.radioHeat) ? 0.28f :
						(record.weeksSinceRelease > 12) ? 0.22f : 0.10f;
		
		record.radioHeat = Mathf.Lerp(record.radioHeat, targetHeat, lerpRate);
		record.radioHeat = Mathf.Clamp(record.radioHeat, 0f, 1f);
	}
	
	/// <summary>
	/// How much of its eventual rotation a record has earned this week. Stations added a record over
	/// several weeks rather than all at once, so this mirrors the release ramp: sales and airplay now
	/// build on the same clock instead of airplay arriving at full strength while sales are still
	/// throttled to a quarter of peak.
	/// </summary>
	internal static float GetRadioBuildWeight(int weeksSinceRelease) {
		if (weeksSinceRelease >= RADIO_BUILD_FULL_WEEK) return 1f;
		float progress = Mathf.Clamp((weeksSinceRelease - 1f) / (RADIO_BUILD_FULL_WEEK - 1f), 0f, 1f);
		return Mathf.Lerp(RADIO_BUILD_FLOOR, 1f, progress);
	}

	/// <summary>
	/// How far a record is still selling against its own best week. 1 all the way up the climb,
	/// because peakWeeklyUnits is a running maximum, so anything keyed to this is neutral until the
	/// record turns over. A record with no sales history yet reads as fully supported.
	/// </summary>
	internal static float GetSalesSupportRatio(RecordRuntimeData record) =>
		record == null || record.peakWeeklyUnits <= 0
			? 1f
			: Mathf.Clamp(record.unitsThisWeek / (float)record.peakWeeklyUnits, 0f, 1f);

	/// <summary>
	/// This week's chance that one region's stations cut a record from current rotation. Two
	/// independent reasons to drop it, combined as competing hazards: the local sales reports have
	/// gone soft, or the record has simply been on too long. Returns 0 during the grace period so a
	/// record cannot be dropped on the one-week wobble that follows its peak.
	/// </summary>
	internal static float GetStationDropChance(float salesSupportRatio, int weeksSincePeakUnits) {
		if (weeksSincePeakUnits < STATION_DROP_GRACE_WEEKS) return 0f;
		float fade = Mathf.Clamp(
			(STATION_DROP_SUPPORT_CEILING - salesSupportRatio) /
			(STATION_DROP_SUPPORT_CEILING - STATION_DROP_SUPPORT_FLOOR), 0f, 1f);
		float burn = Mathf.Clamp(
			(weeksSincePeakUnits - STATION_DROP_BURN_ONSET_WEEKS) / (float)STATION_DROP_BURN_FULL_WEEKS,
			0f, 1f);
		return STATION_DROP_MAX_WEEKLY_CHANCE * (1f - (1f - fade) * (1f - burn));
	}

	/// <summary>
	/// Whether a region's rotation is even a candidate for the drop. A record has to be on the air to
	/// be taken off it, which also keeps the RNG stream off the thousands of live records carrying no
	/// rotation at all.
	/// </summary>
	internal static bool IsStationDropCandidate(RegionalRecordData data) =>
		data != null && !data.stationsDropped && data.radioPlay > STATION_DROP_MIN_ROTATION;

	/// <summary>
	/// Rotation left in a region the week its stations cut the record. Not a lerp: the point of the
	/// mechanic is that a playlist drop is a decision taken between two weekly meetings.
	/// </summary>
	internal static float GetDroppedRotation(float radioPlay) => radioPlay * STATION_DROP_RESIDUAL;

	public static float GetRadioDifficulty(MarketRegion region) {
		// Godot Mathf lacks Log10, so we use natural Log divided by Log(10)
		float log10 = Mathf.Log(region.media.totalRadioStations + 1) / Mathf.Log(10);
		float log16 = Mathf.Log(16) / Mathf.Log(10);
		
		float difficulty = log10 / log16;
		return Mathf.Clamp(difficulty, 0.3f, 2.5f);
	}
	
	// =======================================================================
	// LABEL PUSH
	// =======================================================================
	
	public static float GetCampaignImpact(AILabel label) {
		if (label == null) return 0.02f;
		// Budget sustains and broadens campaigns; marketing controls spend efficiency.
		// Distribution is deliberately absent: it fulfills demand rather than creating it.
		float spendCapacity = 0.45f + (label.budgetLevel * 0.55f);
		return Mathf.Clamp(label.marketingPower * spendCapacity, 0f, 1f);
	}

	/// <summary>
	/// How much of a market's rack shelf a record can claim. A national top-40 hit is fully
	/// racked; one charting below that is partially racked; one proven only in this region is
	/// racked by the jobber servicing it, which is how a regional hit reached mainstream
	/// retail with no major-label deal at all. An unproven record gets no rack space.
	/// </summary>
	internal static float GetRackJobberAccess(int chartPosition, float regionalBreakoutPeak) {
		float national = chartPosition >= 1 && chartPosition <= 40 ? 1f
			: chartPosition >= 1 && chartPosition <= 100 ? 0.55f
			: 0f;
		float regional = Mathf.Clamp(
			(regionalBreakoutPeak - RACK_REGIONAL_PROOF_FLOOR) / (RACK_REGIONAL_PROOF_FULL - RACK_REGIONAL_PROOF_FLOOR),
			0f, 1f) * 0.70f;
		return Mathf.Clamp(Mathf.Max(national, regional), 0f, 1f);
	}

	/// <summary>
	/// Weight of the rack channel by year. Rack jobbing and discount retail expanded through
	/// the 1960s while mom-and-pop record stores contracted, so the same department-store
	/// shelf is worth progressively more across the decade.
	/// </summary>
	internal static float GetRackJobberEraWeight(int year) => Mathf.Lerp(RACK_ERA_FLOOR, 1f,
		Mathf.Clamp(
			(year - RACK_ERA_START_YEAR) / (float)(RACK_ERA_FULL_YEAR - RACK_ERA_START_YEAR), 0f, 1f));

	/// <summary>
	/// Department-store shelf a record commands, as a multiple of the authored 1960 baseline.
	/// Never below 1: the rack channel adds shelf for a record that has proven it turns over,
	/// and cannot take shelf away from one that has not.
	/// </summary>
	internal static float GetRackJobberShelfMultiplier(int chartPosition, float regionalBreakoutPeak, int year) =>
		1f + (GetRackJobberAccess(chartPosition, regionalBreakoutPeak) *
			GetRackJobberEraWeight(year) * RACK_MAX_SHELF_BONUS);

	public static float GetRegionalLaunchFactor(AILabel label, string regionId, string recordId = null) {
		if (label == null) return 1f;
		bool strong = label.strongRegions?.Contains(regionId) ?? false;
		bool covered = label.HasDistributionInRegionForRecord(regionId, recordId);
		float reach = label.EffectiveNationalReachForRecord(recordId);
		if (strong) return 1.35f;
		if (covered) return 0.55f + (reach * 0.45f);
		return 0.12f + (reach * 0.18f);
	}

	public static int CalculateInitialRegionalStock(AILabel label, string regionId, float careerScale, float perceivedQualityMultiplier, string recordId = null) {
		if (label == null) return 0;
		bool strong = label.strongRegions?.Contains(regionId) ?? false;
		bool covered = label.HasDistributionInRegionForRecord(regionId, recordId);
		bool isHome = !string.IsNullOrEmpty(label.homeRegion) && label.homeRegion == regionId;
		float reachForRecord = label.DistributionStrengthForRecord(recordId);
		float access = covered ? 1f : 0.18f;
		float localDepth = isHome || strong
			? 0.25f + (reachForRecord * 0.75f)
			: 0.10f + (reachForRecord * 0.75f);
		float strongDepth = strong ? 1.45f : 1f;
		float noise = (float)GD.RandRange(0.85, 1.15);
		// DISTANCE-4B: neutral in 4a; 4b turns regional reach into real stock friction.
		float reachFactor = DistanceModel.GetEffectiveReach(label, DistanceModel.GetHubCityIdForRegion(regionId));
		int raw = Mathf.RoundToInt(10000f * access * localDepth * strongDepth * careerScale * perceivedQualityMultiplier * noise * reachFactor);
		int floor = isHome || strong ? 100 : 0;
		return Mathf.Max(floor, raw);
	}

	/// <summary>
	/// Redistributes already-drawn per-region stock. Callers use this after their
	/// established launch loop so disabled execution retains its exact RNG order.
	/// </summary>
	public static IReadOnlyDictionary<string, int> RedistributeInitialRegionalStockAllocation(Genre primaryGenre, int year,
		bool live, IEnumerable<MarketRegion> regions, IReadOnlyDictionary<string, int> baselineStock) {
		MarketRegion[] regionArray = regions?.Where(region => region != null).ToArray() ?? System.Array.Empty<MarketRegion>();
		int[] baseline = regionArray.Select(region => baselineStock?.GetValueOrDefault(region.regionId) ?? 0).ToArray();
		int[] allocated = AllocateSpecialistInitialStock(primaryGenre, year, live,
			regionArray.Select(region => region.regionId).ToArray(), baseline);
		var result = new Dictionary<string, int>(regionArray.Length, System.StringComparer.Ordinal);
		for (int i = 0; i < regionArray.Length; i++) result[regionArray[i].regionId] = allocated[i];
		return result;
	}

	internal static int[] AllocateSpecialistInitialStockForProbe(Genre primaryGenre, int year, bool live,
		IReadOnlyList<string> regionIds, IReadOnlyList<int> baselineStock) =>
		AllocateSpecialistInitialStock(primaryGenre, year, live, regionIds, baselineStock);

	private static int[] AllocateSpecialistInitialStock(Genre primaryGenre, int year, bool live,
		IReadOnlyList<string> regionIds, IReadOnlyList<int> baselineStock) {
		int count = System.Math.Min(regionIds?.Count ?? 0, baselineStock?.Count ?? 0);
		var unchanged = Enumerable.Range(0, count).Select(index => System.Math.Max(0, baselineStock[index])).ToArray();
		Genre canonical = GenreCatalog.MapLegacy(primaryGenre, year);
		if (!live || !GenreAcceptanceService.IsSpecialistFulfillmentGenre(canonical) || count == 0) return unchanged;

		int nationalBudget = unchanged.Sum();
		if (nationalBudget <= 0) return unchanged;
		var weighted = new float[count];
		float weightedTotal = 0f;
		for (int i = 0; i < count; i++) {
			weighted[i] = unchanged[i] * GenreAcceptanceService.GetCenteredSpecialistTextureForProbe(canonical, year, regionIds[i]);
			weightedTotal += weighted[i];
		}
		if (weightedTotal <= 0f) return unchanged;

		var allocated = new int[count];
		var remainders = new float[count];
		int assigned = 0;
		for (int i = 0; i < count; i++) {
			float exact = nationalBudget * weighted[i] / weightedTotal;
			allocated[i] = Mathf.FloorToInt(exact);
			remainders[i] = exact - allocated[i];
			assigned += allocated[i];
		}
		foreach (int index in Enumerable.Range(0, count).OrderByDescending(index => remainders[index]).ThenBy(index => index)) {
			if (assigned >= nationalBudget) break;
			allocated[index]++;
			assigned++;
		}
		return allocated;
	}

	private static void UpdateLabelPush(RecordRuntimeData record, AILabel label) {
		if (label == null) {
			record.currentLabelPush = 0.02f;
			return;
		}
		
		float basePush = GetCampaignImpact(label);
		
		float weekFactor = record.weeksSinceRelease switch {
			0 or 1 => 1.0f,
			2 or 3 => 0.9f,
			4 or 5 => 0.6f,
			6 or 7 => 0.3f,
			_ => 0.1f
		};
		
		if (record.currentPosition > 0 && record.currentPosition <= 20) {
			weekFactor = Mathf.Max(weekFactor, 0.85f);
		} else if (record.momentum > 0.15f && record.weeksSinceRelease < 14) {
			weekFactor = Mathf.Max(weekFactor, 0.7f);
		}
		
		record.currentLabelPush = basePush * weekFactor;
		record.totalLabelInvestment += record.currentLabelPush;
	}
	
	// =======================================================================
	// AWARENESS
	// =======================================================================
	
	private static void UpdateAwareness(RecordRuntimeData record, float quality) {
		if (record.weeksSinceRelease <= 1 && record.awareness < 0.02f) {
			float initialAwareness = record.artistHeat * ARTIST_HEAT_AWARENESS_BONUS;
			initialAwareness += 0.04f;
			record.awareness = Mathf.Max(record.awareness, initialAwareness);
		}
		
		float radioGrowth = record.radioHeat * RADIO_AWARENESS_MULT;
		
		float womEffectiveness = Mathf.Max(0, (quality - 0.45f) * 2.2f); 
		float womGrowth = record.wordOfMouth * WORD_OF_MOUTH_MULT * womEffectiveness;
		
		float chartVisibility = 0f;
		if (record.currentPosition > 0) {
			if (record.currentPosition <= 5) chartVisibility = 0.12f;
			else if (record.currentPosition <= 10) chartVisibility = 0.08f;
			else if (record.currentPosition <= 20) chartVisibility = 0.05f;
			else if (record.currentPosition <= 40) chartVisibility = 0.025f;
			else {
				float normalizedRank = (101f - record.currentPosition) / 100f;
				chartVisibility = Mathf.Pow(normalizedRank, 3f) * 0.02f;
			}
		}
		
		float organicGrowth = BASE_AWARENESS_GROWTH * quality;
		float growthRoom = 1f - record.awareness;
		
		float totalGrowth = (radioGrowth + womGrowth + chartVisibility + organicGrowth) * growthRoom;
		record.awareness = Mathf.Clamp(record.awareness + totalGrowth, 0f, 1f);

		record.awareness = ApplyWeeklyAwarenessAgeDecay(record.awareness, record.weeksSinceRelease);
	}

	// Awareness is mutable stock, so the post-peak rate is applied once per
	// elapsed week. Raising the rate to the record's age and then applying that
	// increasingly large factor to last week's already-decayed stock produced a
	// triangular exponent: by age 18 the stock had received .95^55 instead of
	// .95^10. That erased the slow regional-to-national breakouts this system is
	// intended to model.
	internal static float ApplyWeeklyAwarenessAgeDecay(float awareness, int weeksSinceRelease) =>
		weeksSinceRelease > 8
			? Mathf.Max(0f, awareness) * AWARENESS_DECAY_RATE
			: Mathf.Max(0f, awareness);
	
	// =======================================================================
	// WORD OF MOUTH
	// =======================================================================
	
	private static void UpdateWordOfMouth(RecordRuntimeData record, float quality) {
		float qualityWOM = Mathf.Pow(quality, 2.2f) * 0.55f;
		
		float chartWOM = 0f;
		if (record.currentPosition > 0 && record.currentPosition <= 40) {
			chartWOM = (40f - record.currentPosition) / 40f * 0.35f;
		}
		
		float momentumFactor = record.momentum * 0.45f; 
		
		float targetWOM = Mathf.Max(0f, qualityWOM + chartWOM + momentumFactor);
		record.wordOfMouth = Mathf.Lerp(record.wordOfMouth, targetWOM, 0.22f);
	}
	
	// =======================================================================
	// SATURATION
	// =======================================================================
	
	public static void UpdateSaturation(RecordRuntimeData record, MarketRegion[] regions) {
		float weightedPenetration = 0f;
		float totalPotentialAudience = 0f;
		float quality = record.GetQuality();

		foreach (var region in regions) {
			if (!record.regionalData.TryGetValue(region.regionId, out var regionalData)) continue;

			float potentialAudience = GetRegionalPotentialAudience(record, region, quality);
			float penetration = regionalData.unitsSoldTotal / Mathf.Max(1f, potentialAudience);
			weightedPenetration += penetration * potentialAudience;
			totalPotentialAudience += potentialAudience;
		}

		record.saturation = totalPotentialAudience > 0f
			? weightedPenetration / totalPotentialAudience
			: 0f;
	}

	private static float GetRegionalPotentialAudience(RecordRuntimeData record, MarketRegion region, float quality) {
		float qualityAppeal = 0.3f + (quality * 0.7f);
		float genreReach = GetGenreMarketReach(record.baseRecord.primaryGenre);
		return BASE_POTENTIAL_AUDIENCE * qualityAppeal * genreReach * (region.population / 50f);
	}
	
	// =======================================================================
	// MOMENTUM
	// =======================================================================
	
	private static void UpdateMomentum(RecordRuntimeData record) {
		float salesChange = 0f;
		
		if (record.unitsPreviousWeek > 100) {
			salesChange = (float)(record.unitsThisWeek - record.unitsPreviousWeek) / record.unitsPreviousWeek;
			salesChange = Mathf.Clamp(salesChange, -MOMENTUM_CLAMP, MOMENTUM_CLAMP); 
		} else if (record.unitsThisWeek > 500) {
			salesChange = 0.4f;
		} else if (record.unitsThisWeek > 100) {
			salesChange = 0.2f;
		}
		
		float quality = record.GetQuality();
		float momentumFloor = MOMENTUM_QUALITY_FLOOR * (1.4f - quality);
		float targetMomentum = Mathf.Max(salesChange, momentumFloor);
		
		record.momentum = Mathf.Lerp(record.momentum, targetMomentum, MOMENTUM_SMOOTHING);
		
		if (record.momentum > record.peakMomentum) {
			record.peakMomentum = record.momentum;
		}
		
		if (record.momentum > 0.02f) {
			record.weeksPositive++;
			record.weeksNegative = 0;
		} else if (record.momentum < -0.02f) {
			record.weeksNegative++;
			record.weeksPositive = 0;
		}
	}
	
	// =======================================================================
	// CHART POINTS
	// =======================================================================
	
	// Changed List<MarketRegion> to MarketRegion[] to match ChartManager
	// The year is resolved here rather than threaded through the five call sites so the audit
	// telemetry that recomputes chart points can never disagree with the ranking that used them.
	public static float CalculateChartPoints(RecordRuntimeData record, MarketRegion[] regions) =>
		CalculateChartPoints(record, regions, TimeManager.Instance?.CurrentDate.year ?? AIRPLAY_ERA_START_YEAR);

	public static float CalculateChartPoints(RecordRuntimeData record, MarketRegion[] regions, int year) {
		float salesPoints = record.unitsThisWeek;

		float airplayPoints = 0f;
		foreach (var region in regions) {
			if (!record.regionalData.ContainsKey(region.regionId)) continue;
			var data = record.regionalData[region.regionId];

			if (region.media != null) {
				// Convexity is there to separate a heavily rotated record from a lightly rotated one.
				// It must not also cube the genre's radio access. Acceptance already enters rotation
				// twice -- once through radioHeat, once through the regional radio opportunity -- so
				// raising the product to a power compounded a genre disadvantage to roughly the sixth
				// power. Measured at 1960 that moved the top three genres from 52.6% to 66.6% of the
				// chart and left Soul holding 0.9% of chart weeks against 5.8% of units. Access is
				// divided back out, the record's own rotation carries the exponent, and access is then
				// paid back linearly.
				float access = data.genreRadioOpportunityThisWeek > 0f ? data.genreRadioOpportunityThisWeek : 1f;
				float ownRotation = data.radioPlay / access;
				float rotation = AIRPLAY_REFERENCE_PLAY *
					Mathf.Pow(ownRotation / AIRPLAY_REFERENCE_PLAY, AIRPLAY_CONVEXITY) * access;
				airplayPoints += rotation * region.media.radioReach * region.population *
					AIRPLAY_POINTS_PER_MILLION_REACHED;
			}
		}

		// The published score, not the true one: surveySampleThisWeek is the week's panel draw, cached
		// on the record so this method stays a pure function of stored state and the audit telemetry
		// reproduces the ranking byte for byte.
		return (salesPoints + (airplayPoints * GetAirplayEraWeight(year))) * record.surveySampleThisWeek;
	}

	/// <summary>
	/// How many of the panel's outlets report a record at all. A national smash is stocked and played
	/// nearly everywhere the survey reaches; a record scraping the hundred turns up in a handful of
	/// returns, which is why its published position was so much rougher than a hit's.
	/// </summary>
	internal static float GetSurveyReportingOutlets(float unitsThisWeek) =>
		SURVEY_PANEL_SIZE * Mathf.Clamp(unitsThisWeek / SURVEY_FULL_REPORT_UNITS,
			SURVEY_MIN_PANEL_SHARE, 1f);

	/// <summary>
	/// Relative sampling error on this week's published score. Standard error of a mean falls as
	/// 1/sqrt(n), so the panel's coarse three-grade returns are far noisier for a record only a few
	/// outlets carry.
	/// </summary>
	internal static float GetSurveySigma(float unitsThisWeek) =>
		Mathf.Min(SURVEY_MAX_SIGMA,
			SURVEY_NOISE_SCALE / Mathf.Sqrt(Mathf.Max(1f, GetSurveyReportingOutlets(unitsThisWeek))));

	/// <summary>
	/// One week's survey draw for one record: a lognormal multiplier with a mean of exactly 1, so the
	/// panel is unbiased and only its precision varies. Drawn once per record per week by
	/// ChartManager and cached on the record -- never call this from CalculateChartPoints, which the
	/// audit telemetry re-invokes and which must reproduce the ranking exactly.
	/// </summary>
	public static float DrawSurveySample(float unitsThisWeek) {
		float sigma = GetSurveySigma(unitsThisWeek);
		if (sigma <= 0f) return 1f;
		// Box-Muller off the seeded global RNG, so the draw stays reproducible under --seed.
		float u1 = Mathf.Max(1e-7f, (float)GD.RandRange(0.0, 1.0));
		float u2 = (float)GD.RandRange(0.0, 1.0);
		float standardNormal = Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(Mathf.Tau * u2);
		// -sigma^2/2 keeps E[exp(X)] at 1, so noise reorders the chart without inflating it.
		return Mathf.Exp(standardNormal * sigma - sigma * sigma * 0.5f);
	}

	internal static float GetAirplayEraWeight(int year) {
		if (year <= AIRPLAY_ERA_START_YEAR) return AIRPLAY_ERA_WEIGHT_EARLY;
		if (year >= AIRPLAY_ERA_FULL_YEAR) return AIRPLAY_ERA_WEIGHT_LATE;
		float progress = (float)(year - AIRPLAY_ERA_START_YEAR) / (AIRPLAY_ERA_FULL_YEAR - AIRPLAY_ERA_START_YEAR);
		return Mathf.Lerp(AIRPLAY_ERA_WEIGHT_EARLY, AIRPLAY_ERA_WEIGHT_LATE, progress);
	}
	
	// =======================================================================
	// STUDIO QUALITY
	// =======================================================================
	
	public static float GetStudioQualityModifier(MarketRegion recordingRegion) {
		if (recordingRegion?.musicIndustry == null) {
			return 0.7f;
		}
		
		var infra = recordingRegion.musicIndustry;
		
		float modifier = 0.55f + (infra.studioQuality * 0.45f);
		float studioBonus = Mathf.Min(infra.recordingStudioCount * 0.015f, 0.15f);
		float signatureBonus = infra.hasSignatureSound ? 0.08f : 0f;
		float majorBonus = infra.hasMajorLabelPresence ? 0.05f : 0f;
		
		return Mathf.Clamp(modifier + studioBonus + signatureBonus + majorBonus, 0.5f, 1.15f);
	}
}

public readonly struct SingleDemandStages {
	public readonly float PotentialAudience, BaselineAwareness, EarnedDiscoveryExposure, AwareBuyers;
	public readonly float IntrinsicQualityFactor, AcceptanceFactor, FormatFactor, IntrinsicConversionRate;
	public SingleDemandStages(float potentialAudience, float baselineAwareness, float earnedDiscoveryExposure, float awareBuyers,
		float intrinsicQualityFactor, float acceptanceFactor, float formatFactor, float intrinsicConversionRate) {
		PotentialAudience = potentialAudience; BaselineAwareness = baselineAwareness; EarnedDiscoveryExposure = earnedDiscoveryExposure;
		AwareBuyers = awareBuyers; IntrinsicQualityFactor = intrinsicQualityFactor; AcceptanceFactor = acceptanceFactor;
		FormatFactor = formatFactor; IntrinsicConversionRate = intrinsicConversionRate;
	}
}
