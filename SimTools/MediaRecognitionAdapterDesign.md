# Media → recognition adapter (design)

Status: design, 2026-10-31. The author approved adding a media writer to the act's fame
(`SimulatedArtist.publicRecognition`). As written, the directive keeps recognition out of Phase 4
(`MediaAtmosphereDirective.md` §12.4 and the Phase 4 line in §16), and §12.4 asks that any "future recognition
writer" get its own adapter inside an accepted combined envelope. This document is that adapter. It is built and
validated separately from the §12.3 singles demand adapter, then they are run together. Nothing here is implemented.

## Why

Television made names. A singer booked repeatedly on primetime variety stayed familiar to the public long after their
last hit; Perry Como, Dean Martin and Andy Williams all had their own series. Today the sim's fame is earned only on
the chart (milestone latches, plus run-completion sustain), so a TV booking can help an act's fame only by first selling
records. The 1963-66 check shows the result: no act whose last hit was four or more years back ever gets a standing
date. Recognition decays with a half-life of about 5.3 years, and nothing off the chart refreshes it.

## What recognition already does (the loops this writer feeds)

Act recognition is live by default (`chart_manager.tscn` sets `artistRecognitionEnabled = true`). Its readers:

| Reader | Effect | Cap |
|---|---|---|
| `SimulatedArtist.GetNewReleaseAwarenessBonus` | launch awareness | + `LaunchRecognition × 0.115` |
| `CompetitorManager` single and album launch stock | launch stock | × (1 + `LaunchRecognition × 0.30`) |
| `CompetitorManager` radio | launch radio heat | + `LaunchRecognition × 0.075` |
| `RegionalAudience` hometown radio | hometown heat shrinks as the act becomes known | `× (1 − EffectiveRecognition)` |
| Media standing pool, press, player office | who gets booked by name; press interest | floor 0.05, program floor 0.20 |
| `BandLifeService`, `MemberWealthService`, Rolodex | member "fame" for band life, wealth, contact cards | `publicRecognition × 2.5` |
| `MusicianRecognitionService.ShareArtistRecognitionGain` | every act gain is shared down to the current lineup | — |

So a media deposit moves sales through the three launch caps (the sales loop). It also moves TV bookings through the
standing pool (the bookings loop), along with band life and member wealth. Those are the effects to balance.

## The deposit

Once a week, after `TallyAiredThrough` has tallied the airings, each act with a fresh national or local airing gets one
deposit through the existing `ArtistRecognitionService.AddPublicRecognition`. That call keeps the diminishing returns
`(1 − r)` and shares the gain with the members, exactly as a chart milestone does.

```text
seen'     = CombineReach(seen, freshReach, population)      // people who have seen the act recently
gain_raw  = K × (seen' − seen) / population × perf
gain      = min(gain_raw, AnnualCap − depositedThisYear)    // per act, calendar year
```

- **`freshReach`** is the reach already computed in `TallyAiredThrough`. A first visit to a program counts at full
  unique reach, and a return to the same program at 25% of it (`ReturnVisitNewShare`). Repeats and syndicated tapes
  contribute only what that rule already gives them; they are never a second full event (§4.5, §12.4 root events).
- **`seen`** is a new per-act stock, `MediaActTally.RecognitionReach`. It decays weekly at
  `ArtistRecognitionService.PublicRecognitionDecay`, so it fades at the same rate as the fame it feeds. A regular who
  is on every week stops earning, because almost nobody new sees them. An act that comes back after years earns
  again, because the audience has forgotten. Saturation is built in, so no per-program rule is needed.
- **`perf`** comes from `MediaPerformanceResult.Outcome`: Breakdown 0.5, Weak 0.7, FlatAdequate 1.0, Engaging 1.15,
  Electrifying 1.35. A cut segment is already excluded from the tally. Recognition measures how familiar a name is,
  not how well it is regarded, so a disaster still makes a face known. That is why no outcome goes negative. Esteem
  stays with press tone and fan attachment.
- **What was sung doesn't matter.** A standard sung on Sullivan builds the act's name as much as its own record does.
  The song choice matters to the demand adapter (§12.3 reads `Song == Record`), not here.
- **Local shows use the same formula.** Their reach is a local market's, so they deposit little on their own. A
  hometown dance-show regular gets known locally, through fan attachment, not nationally.

### Starting values (to be checked by the shadow pass, not tuned to pass)

`population` is the same denominator `TallyAiredThrough` already uses: the sum of region populations, 158M in the model
(Phase 2 notes). The model's Sullivan spots reach 20-27M unique viewers, about 0.14 of that; a talk-show spot reaches
roughly 5-8M.

| Parameter | Value | Meaning |
|---|---|---|
| `K` | 0.15 | A first, ordinary Sullivan spot (≈ 0.14 of the population) deposits ≈ 0.021, about one top-40 milestone (0.022). A talk-show spot deposits about a chart entry (0.007). |
| `AnnualCap` | 0.06 | At most about a top-10 plus a top-40's worth of familiarity a year from TV, however much the act appears. |
| Decay of `seen` | `PublicRecognitionDecay` (≈ 5.3-year half-life) | Same clock as the fame it feeds. |

The diminishing returns keep a known name from gaining much. An act at 0.40 recognition keeps only 60% of any raw
gain. That is the historical shape: TV makes a minor act known and keeps a known act known, but it cannot make a star
out of airtime alone.

## Guards on the two loops

1. **The bookings loop** (TV → recognition → standing bookings → more TV). It is bounded by:
   - the season limit, four national name bookings per 13 weeks;
   - `seen` saturation, since a regular earns about nothing;
   - the annual cap.

   Telemetry counts acts that cross the 0.05 and 0.20 standing floors because of TV, as opposed to the chart.
2. **The sales loop** (TV → recognition → launch awareness, stock and radio → chart → milestones → recognition). It is
   bounded by the three existing launch caps and by the annual cap. The chart milestones it earns are the existing
   earned path (§12.4), not a second media deposit. The share of each act's recognition that came from TV is logged,
   so the loop can be read directly.
3. **Concentration.** Chart-lane bookings already follow chart position, so TV fame lands on acts that are already
   winning. Rival media planning is the same Phase 3 policy for every label, so a better-funded label's bookings
   don't buy extra fame beyond the cap. The acceptance screen measures top-1/5/10% act slot-week concentration, using
   the recognition Phase B screen's method and its kill rule (top 10% up by more than 3 points).
4. **The combined envelope with the demand adapter.** The two adapters share root events (one appearance), but they
   write different things: this one recognition, §12.3 regional singles intent. §12.5 limits apply to their combined
   effect. Each is validated alone first, then together.
5. **Press is excluded for now.** Press items (Life, Look, fan magazines) are a natural second writer under the same
   annual cap and the same root ids, so a TV appearance and its trade write-up are one event. They are left out of the
   first build so the TV effect can be read cleanly.
6. **Who is covered.** Every act, player and AI alike: AI bookings already happen in observe mode. Member spotlight
   stays media-only (§12.4). The member share of the act gain still flows through the existing
   `ShareArtistRecognitionGain`.

## Modes and switches

- **Off by default:** no field is written and nothing is computed.
- **`--observe-media-recognition`** (shadow): `seen` is written (it is media-owned), and the would-be gain goes to
  `MediaActTally.ShadowRecognitionGain` and the telemetry. `publicRecognition` is never touched, so the economy must
  stay byte-identical to the control. This pass sizes `K` and the cap before anything is causal.
- **`--media-recognition`** (causal): deposits go through `AddPublicRecognition`. It requires `ArtistRecognition.Enabled`.
  Kept independent of the §12.3 adapter switch.

Save data: `RecognitionReach`, `ShadowRecognition`, `RecognitionDepositedYear` and `RecognitionYear` on
`MediaActTally`, saved as JSON fields. Old saves load them as zero, so no fame is invented for past airings (§15.2). No
`ConfigVersion` bump: the version keys every media draw, and bumping it would reshuffle all bookings for a change that
books nothing differently.

## Telemetry (`MEDIA_RECOGNITION`, yearly)

- Deposits, their sum and the cap-clipped amount; split national/local and chart/standing lane.
- The TV share of all recognition gained that year, against chart milestones and run completion.
- The top 10 acts by TV deposit, with chart gain beside them.
- Acts crossing 0.05 and 0.20 because of TV.
- Mean recognition of acts whose last hit was 4+ years back, split by TV-active or not. This is the "TV keeps names
  alive" read.

## Evidence ladder

1. **Fixtures** in `MediaAtmosphereChecks`:
   - one Sullivan spot ≈ one top-40 milestone, and a talk spot ≈ a chart entry;
   - a weekly regular stops earning;
   - an act back after years earns again;
   - a disaster deposits less, never negative;
   - the cap clips;
   - repeats and syndicated tapes add only their fresh share;
   - save/reload deposits once;
   - shadow mode never writes `publicRecognition`;
   - reads draw no RNG.
2. **Shadow, observe-only:** the 1963 resume plus 104-week seeds 1001 and 2002. The economy must be byte-identical to
   `ref-f50b9a4`. Read the deposit distribution and the TV share. Adjust `K` and the cap only against the stated
   shape, never to pass an economy gate.
3. **Causal screen** on the 1963 resume (seed 1001) against a paired control: concentration (kill rule above), Big-5
   share, genre ±1 point, singles and album units ±1%, standing dates to acts whose last hit was 4+ years back, and
   Sullivan fill. Single-seed reshuffle noise is about as large as these limits, so a screen only catches a
   catastrophe.
4. **End-of-branch decade A/B,** batched with the hit-history-signing A/B and the §12.3 adapter. Paired seeds 1001 and
   1002 plus two preregistered holdouts, game profile, all §12.5 limits per seed and year.

## Shadow pass (built 2026-10-31)

- **Built:**
  - `Systems/Media/MediaRecognition.cs`, with the `--observe-media-recognition` switch and the pure `Deposit` arithmetic;
  - four `MediaActTally` fields;
  - the weekly decay and per-airing deposit in `MediaAtmosphereService`;
  - the yearly `MEDIA_RECOGNITION` line;
  - `ArtistRecognitionService.RealizedGainTotal`, a print-only counter of chart-earned gains.

  `--media-recognition` (causal) is rejected until it is built.
- **Fixtures** (`MediaAtmosphereChecks.RecognitionShadow`, 146 checks pass at `--seed=1001`):
  - a first Sullivan spot is 0.0210 against a top-40's 0.022, and a talk spot 0.0057 against a chart entry's 0.007;
  - a regular earns 0.0021, and the same act five years later 0.0144;
  - a breakdown earns exactly half, an electrifying night 0.0284;
  - the cap clips;
  - the new fields survive a save round trip.
- **Inertness, with the shadow on:**
  - 104-week seeds 1001 and 2002: 106/106 economy CSVs byte-identical to `ref-f50b9a4`;
  - the 1963-66 resume (`tvbook-s`): population events and records byte-identical to `tvbook-m`.

1963-66 resume, seed 1001. The 1963 "chart-earned" figure also counts credits made at world load, so read 1964-66:

| Year | TV deposits | Applied (after diminishing returns) | Chart-earned | TV share | Acts at cap | Would cross 0.05 / 0.20 | TV-active hitmakers: recognition real → with TV |
|---|---:|---:|---:|---:|---:|---:|---|
| 1964 | 7,991 | 4.77 | 40.8 | 10.5% | 18 | 30 / 23 | 363 acts: 0.157 → 0.177 |
| 1965 | 11,266 | 5.91 | 39.7 | 12.9% | 25 | 37 / 32 | 345 acts: 0.177 → 0.204 |
| 1966 | 10,945 | 5.09 | 39.1 | 11.5% | 17 | 49 / 34 | 344 acts: 0.192 → 0.221 |

- **Size:** TV would add about an eighth of the fame the chart earns in a year. Hitmakers who are on TV would be about
  13-15% better known than they are now. That's a visible effect, not a takeover, so K = 0.15 and the 0.06 cap stand
  for the causal screen.
- **Where it lands:**
  - the chart lane takes 65-77% of deposits, so most TV fame goes to acts already charting (the concentration risk to
    watch);
  - the standing lane takes 20-30%;
  - local shows take about 3%;
  - primetime variety takes about half.
- **The cap binds for 17-25 acts a year,** the busiest chart-lane guests on daily teen TV and talk shows. They are
  the top-five lists every year.
- **The bookings loop's entry point:** 23-34 acts a year would cross the 0.20 program floor through TV. The shadow
  does not feed back, so in causal mode they would then get standing dates and deposit again. The causal screen has
  to read that loop.
- **Past hitmakers off TV are untouched.** About 1,000-1,200 of them sit at about 0.05 recognition, and the shadow
  never reaches them, because the standing pool needs a label and recognition first. Keeping old names known also
  needs `--hit-history-signing-recovery` to get them signed and onto bills. The causal screen should run with that
  switch both off and on.

## Open

- **Interaction with `--hit-history-signing-recovery`.** That switch keeps old names signed; this one keeps them
  known. Both are needed for the "old star on Sullivan" picture. Read the screen with the Recovery switch both off
  and on.
- **An act's own series** (the variety-host case) isn't in the catalog. A host would deposit every week under the
  `seen` saturation, which is the right shape: high and stable rather than rising. Add hosts only if the shadow pass
  shows TV-active veterans still fading.
- **Senior cooling** (`--senior-cooling`) cuts the launch read for acts a year past their last top 40. If it's adopted,
  TV fame helps bookings more than sales for veterans, which is historically right.
