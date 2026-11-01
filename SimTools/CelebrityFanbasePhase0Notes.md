# Celebrity fanbase Phase 0 notes: followings, regard and salience, observe-only

Status: built 2026-10-31, off unless `--observe-following` is passed. Design: `CelebrityFanbaseDesign.md` §2, §4.1, §7,
§10 Phase 0. Phase 0 changes no governing decision, so none of the four directive amendments (§0) is needed yet.

## What is built

| Piece | Where | What it does |
|---|---|---|
| Switch | `MediaFollowing.cs` (`--observe-following`) | Runs inside the media world (observe or causal). Writes media state and telemetry only. |
| Following stock | `MediaFollowingState`, `FollowingRegion`; `MediaSaveData.Following` | People, by act, market region and audience. Young followers carry their join-year cohort; adult followers are one number. |
| Weekly pass | `MediaAtmosphereService.Following.cs`, after `FansWeek` | Joiners, losses, regard, salience and the attention states, in that order. |
| Opening seed | `SeedOpeningFollowing` | A 1960 act walks in with the following its last five years of opening-career hits would have built. A world resumed from a save without followings starts them at zero. |
| Telemetry | `MEDIA_FOLLOWING`, `MEDIA_SALIENCE_YEAR` and `MEDIA_SALIENCE` lines; `<run>-following.csv` | Monthly act rows (following ≥ 1,000), one row per release by a followed act, one row per week for the salience leader. |
| Calibration read | `SimTools/analyze_following.py` | The §9 targets, per year. |

### Arithmetic, as built (all initial design values)

- **Joiners** each week, per act, region and audience:
  - buyers × `cBuy` 0.10 × heat, where heat is 0.3 + 0.7 × teen attachment (young) or adult regard (adult);
  - registered TV viewers × `cView` 0.02.
  - A record's buyers split young/adult by genre and format: singles 0.25 + 0.7 × teen fit, LPs 0.10 + 0.5 × teen fit.
  - Joiners are scaled by the room left: following × (1 − following / fit population).
- **Registration** (§3.1, used here only to feed joiners and salience): fresh reach by region × audience share of the
  segment × `r0` 0.25 × audience fit × performance (0.6 Breakdown … 1.6 Electrifying) × novelty (1 → 0.4 with national
  dates already seen) × attention (1 / (1 + others' excess over the sensation bar / θp)).
  - Fit is applied once, in registration, not again in the joiner formula.
- **Losses:**
  - drift 15% a year young, 10% adult;
  - silence +15% a year once 12 months pass with no release;
  - ageing: a young cohort ages out evenly between three and nine years after joining, half of it into the adult
    following;
  - disappointment: a release more than 0.05 below the act's last three releases of the same format loses
    5-20% of both audiences at once;
  - switching: young followings lose each other sensation's salience as a yearly rate.
- **Regard** (§4.1): 13-week half-life, weekly gain capped at 0.08, from adult purchase standing (top 20 adult buyers in
  the region, 1 − k/20), adult fresh viewers as a share of the region's adults × performance, and mainstream and
  local-daily items.
- **Salience** (§7): three families in Phase 0, sales 0.5, television 0.3, press 0.2. Live attendance joins with its
  inflow. Each family is the act's share of the nation's week (sales young and adult weighted by audience size;
  registered viewers; root-deduplicated readers), smoothed at 0.89 a week.
  - Breadth counts the families where the act holds more than 3% (three times one of a hundred chart slots), plus one
    when both its young and adult shares clear that bar.
  - Sensation: salience ≥ 1.5% and breadth ≥ 2; leaves below 0.9%.
  - Phenomenon: salience ≥ 8%, ≥ 3× the next act, breadth 4 (every measured family and both audiences), eight weeks
    running; leaves after eight weeks failing.
- **Would-be advance orders** at a release, telemetry only: young × 0.35 + adult × 0.10 for a single, young × 0.12 +
  adult × 0.30 for an LP.

## Evidence

**Fixtures** (`MediaAtmosphereChecks`, 183 checks pass at `--seed=1001`, 18 of them new):
- saturation at the fit population; one million units leave 30k followers cold and 100k at full heat;
- young drift of 15% a year applied weekly; a cohort keeps everyone to year 3, half at year 6, nobody past year 9;
- disappointment only on a clear drop, 5-20%;
- novelty and attention limits;
- the states: a teen idol is a sensation and stays one; dominance for eight weeks makes a phenomenon; a rival at half
  its salience keeps it a sensation; one bad week doesn't end it, eight do; the peak is remembered;
- a live four-week observe pass builds followings for 1,122 acts, all finite and non-negative, salience summing below
  1; a save round trip keeps every stock and cohort; with the switch off nothing is written or decayed.

**Inertness**, 104 weeks, ref flags + `--observe-following`, run in parallel, against `ref-f50b9a4-<seed>-104`:
106/106 economy CSVs byte-identical on both seeds 1001 and 2002 (`fb0obs-<seed>-104`). The run adds
`-following.csv` (about 32k rows for two years).

### First calibration read (`analyze_following.py`, 1960-61, both seeds)

| §9 measure | Target | Seed 1001 | Seed 2002 | Reading |
|---|---|---|---|---|
| Largest following | top teen act 1-3M | 350-357k | 514-559k | Low, but 1960-61 has no phenomenon to compare against |
| Fan club (3%) | 50-80k | 10.5-10.7k | 15-17k | About 5x low |
| Advance orders, acts with ≥ 50k followers (median) | 100-300k | singles 23-25k, LPs 34-35k | singles 24-25k, LPs 25-32k | 4-10x low |
| Advance orders, largest | — | 94k single, 83k LP | 114k single, 128k LP | |
| Kept a year after the last Top 40 | half-life 2-3y young, 5-7y adult | young 0.87 (5.1y), adult 0.93 (10y) | young 0.84 (4.0y), adult 0.91 (7.3y) | Decays too slowly; ageing has barely started (cohorts under 3 years) |
| Sensations | 10-25 a decade | 0 | 0 | No act reaches breadth 2 |
| Phenomena | 0-1 a decade | 0 | 0 | Leader 2.0-3.1%, lead over the runner-up at most 1.9x |

What the yearly ledger shows (1960):
- **Television outjoins buying.** TV joiners 10.2-12.9M against 6.5M from buyers. The design meant buyers to be the main
  route, with TV leading only for weekly series. Every local and standing-lane airing registers viewers today.
- **Disappointment is frequent.** It hits 24-27% of releases by followed acts, at 11% each, and makes a third of all
  losses (0.9-1.1M of 3.1M). Release quality varies a lot between an act's records, so "0.05 under the last three"
  trips often.
- **The salience leaders are adult LP acts** (TraditionalPop, EasyListening, Folk, Country). Sales weigh adults by
  audience size (58%) and LP units count in full. No act clears the 3% bar in two families, so attention is spread too
  thin for the breadth test as set.

Candidate calibration, not yet run:
- `cBuy` up and `cView` down, so buyers lead (fan club and advance orders both need about 4-5x more followers on hit
  acts, not on every act);
- young drift up toward the 25%-a-year club figure;
- disappointment on a wider gap or against a longer average;
- the breadth bar measured against the week's own field (3 × the mean share of acts with any share) in place of a
  fixed 3%.

Half-lives past three years, ageing, and the phenomenon distribution need an observe decade (about 2 hours, one
paired seed is enough since it is inert).

### Calibration 1 (2026-11-01)

Changes, all in `MediaFollowing.cs`:
- `cBuy` 0.10 → 0.30; heat factor 0.3 + 0.7h → 0.2 + 0.8h;
- `cView` 0.02 → 0.005;
- drift young 15% → 22%, adult 10% → 8%;
- silence split: young 15%, adult 7.5%;
- disappointment gap 0.05 → 0.10, after at least two releases of the format;
- breadth bars set each week at 3× the mean smoothed share of the acts on the national chart, not a fixed 3%;
- LP units count a third of a single in the sales family of attention;
- θs 1.5% → 2%.

Both screens stay 106/106 economy CSVs byte-identical on seeds 1001 and 2002 (`fb0c1`, `fb0c2`).

| §9 measure | Target | First read | Calibration 1 |
|---|---|---|---|
| Joiners from buying / TV, 1960 | buying leads | 6.5M / 10.2-12.9M | 14.4-14.7M / 2.6-3.2M |
| Largest following | top teen act 1-3M | 350-560k | 0.6-1.2M (one act past 1M) |
| Fan club (3%) | 50-80k | 10-17k | 17-35k |
| Advance orders on singles, top-20 followings (median) | 100-300k | — | 70-82k (largest 170-220k) |
| Releases that disappoint | — | 24-27% | 4.5-5.1% |
| Young kept a year after the last Top 40 | half-life 2-3y | 0.84-0.87 | 0.81-0.86 (3.3-4.5y) |
| Adult kept | half-life 5-7y | 0.91-0.93 | 1.06-1.13 (still growing) |
| Sensations entered in two years | 10-25 a decade | 0 | 6 / 17 (`fb0c1`: 17 / 32) |

- The breadth bars land at 0.6-1.4% (sales about 1%, TV 1.3-1.4%, press 0.6%).
- **The sensations are the wrong acts.** On both seeds they are mostly adult TradPop, EasyListening and Country
  orchestras and singers, with one Folk act. The leader never passes 3.2% and never leads the runner-up by 2×.
- LP weighting didn't change who leads, so the adult lead comes from adult singles and primetime variety audiences.
  Primetime variety audiences are mostly adult, which is right for 1960.
- The design's "national sensation" is a teen craze (the Monkees, the Dave Clark Five). The rule as written lets any
  act with two broad families qualify. Requiring the young audience share above its bar would make sensation the teen
  state, and leave adult acts to reach only a phenomenon, through crossover. This is a definition, not a constant, so
  it is left to the author.
- Adult followings still grow a year after the last hit, from catalog LP buying and ageing teens. Half-lives need the
  observe decade.

### Young-lead rule (2026-11-01, author's decision)

The author decided that a national sensation must lead in the young audience: the 1960s sensation is a youth craze.
`MediaFollowing.LeadsYoung` is now part of the entry test (the exit test is unchanged). Design §7.2 is amended.

- **First form, young share above the young bar (`fb0c3`): almost no effect.** The entrants were still the Cox Strings,
  the Watts Orchestra, the Graham Orchestra and the like. A quarter of every single's buyers are young whatever the
  genre, so a big adult act clears 3× the average young share on volume alone.
- **Second form, young share above the bar *and* at least the act's adult share (`fb0c4`).** Seed 1001: no sensations in
  two years. Seed 2002: three, Brenda Myers (RnB, young .0236 against adult .0229), The Young Teens (TeenPop) and The
  Wasps (DooWop).
- Both screens are 106/106 byte-identical to `ref-f50b9a4` on seeds 1001 and 2002. 184 checks pass, including a new
  fixture: an adult act with the same sales and TV as the teen idol is not a sensation.
- **The test is now blocked by θs and breadth, which are measured on total attention.** On seed 1001, Grant and the
  Daydreams (TeenPop) have the largest young following, 596k against 42k adult, and the old fan-episode pass makes them
  a sensation on 4/8/1960. Yet their salience peaks at 1.1% with breadth 1. Total salience weighs adults at 58%, so a
  teen act needs about twice an adult act's young dominance to reach 2%. The salience leaders in 1960-61 are still
  adult Country, Folk and TradPop acts at 2.0-2.3%.
- The author chose to measure the whole sensation test on young attention.

### Young-only sensation test (2026-11-01)

`MediaFollowing.SensationTest`:
- **young salience**, the smoothed share of young sales and television attention (`Audience[0]`), at or above θs
  young (`YoungSensationShare`);
- **young breadth ≥ 2**, counted over young sales, young television and press. Young sales and television are new
  smoothed shares (`YoungFamily`), with bars at 3× the charting acts' mean. Press stays all-audience, since readers are
  not split in Phase 0;
- **a young lean**: the act's young share is at least its adult share.

The sensation leaves when young salience falls below 0.6 θs young. The phenomenon is unchanged: total attention, both
audiences. The attention limit in registration still uses θs on total salience (2%).

| Screen | θs young | Seed 1001 | Seed 2002 |
|---|---|---|---|
| `fb0c5` | 2% | 27 entries in two years | 23 |
| `fb0c6` | **3%** | 2: The Dreams II, Dusty Moore (both TeenPop) | 3: The Bewitching Daydreams, The Young Teens (TeenPop), The Wasps (DooWop) |

- At 2% the field bunches at 2.0-2.6% young share. Young attention sits on fewer acts than total attention, so 2% of it
  is easy to reach.
- At 3%, two to three sensations in two years is a pace of 10-15 a decade, inside the 10-25 target. Each lasts two to
  four months, and every one is a teen act. The adult TradPop and Country salience leaders (2.0-2.5% of total
  attention) no longer enter.
- Grant and the Daydreams, the act with the biggest young following on seed 1001 (618k), peaks near 2% young share and
  is not a sensation. A big standing following isn't the same as this season's attention.
- 186 checks pass. New fixtures: a teen idol below θs in total attention is a sensation, and a big adult act that clears
  the young bars on volume but leans adult is not. Both screens are 106/106 byte-identical to `ref-f50b9a4` on seeds 1001
  and 2002.
- The count over the 1964-66 British wave needs the observe decade.
