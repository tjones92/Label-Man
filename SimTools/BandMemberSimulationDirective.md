# Band Member Simulation Directive — People, Not Averages

Branch: `band-member-simulation` (cut from `codex/polar-song-data-model`, because Part A reads
`ActProfile`, `PolarMaterialFit` and the ear-read layer that branch added). Source: the author's
design sketch `bandexpansion.md`, read against the tree on 2026-10-06. This file is the
code-verified plan. It is the "prerequisite lineup-dynamics directive" that
`CelebrityRecognitionDirective.md` §8.4 deferred to.

---

## 0. Objective

The author's brief: the characters have no life. They don't talk to the player, don't get into
disputes, don't break up, don't go behind each other's backs. The decade was built on its
characters — Lennon/McCartney, Jagger, Joplin, Hendrix — and the game's people are averages.

Done means that, in an ordinary game:

1. A member of the player's act walks into the office with a grievance the simulation can back with
   a number, and what the player does about it changes what happens next.
2. Bands in the wider world split, re-form, lose singers to solo careers and drummers to the draft,
   and the player reads about it.
3. People outlive their bands. The guitarist from a dead folk trio turns up in a folk-rock band two
   years later, a little better and a lot better known.
4. Some acts get better with work, and the player can sign one that isn't good yet, on purpose.
5. Songs have writing teams. A partnership's songs are different from either writer's alone, the
   credit on the label is something people fight over, and the player can put two writers in a room.
6. The decade's hazards reach the roster: a drummer is drafted in 1966, and — rarely, and more
   often as the decade turns — a member dies.

Two constraints govern everything below:

- **The AI economy is calibrated** (V3.1 baseline; the evolution bundle at 303.8). Anything that
  changes AI outcomes ships behind a flag, observe-only first, then a two-seed decade A/B
  ([[single-seed-decade-ab-noise-floor]]). Player-only work keeps the AI economy byte-identical in
  the headless harness and owes no decade run — the `ContractNegotiationDirective.md` Part 1
  precedent.
- **The Rolodex rule binds every line a character speaks** (`RolodexScene.cs:16-17`): a line must
  reveal a real sim fact, express a real motive, or execute a real sim action. No mood text the
  simulation cannot explain.

---

## 1. What the sketch gets right — keep all of it

- **The diagnosis.** Every band-level fact is a mean over members: `SimulatedArtist.RecalculateStats`
  (`SimulatedArtist.cs:150-178`) and `ArtistEvolutionService.DeriveDisposition`
  (`ArtistEvolutionService.cs:77-105`). A mean cannot represent a disagreement, so there is nothing
  to narrate and nothing to resolve.
- **One mechanism unlocks three dead systems.** Verified:
  - `WouldConsiderSoloCareer` (`Musician.cs:66`) has zero callers. So does `RemoveMember`
    (`SimulatedArtist.cs:376`).
  - `personalRecognition` is written (`MusicianRecognitionService.cs:43`) and read only by a UI
    caption (`ArtistDetailPanel.cs:101-115`).
  - The writer ledger is telemetry (`CompositionCatalogService.cs:609-621`).
  - All three wait on [[lineup-churn-never-fires]].
- **Sparse relation edges**, allocated on first strain. A quiet band costs nothing.
- **`creativeRivalry` separate from `strain`.** Productive tension that raises output while
  degrading the relationship is the whole Lennon–McCartney point.
- **Cohesion as a stock** that relaxes toward a personality-derived resting value.
- **Ceiling separate from current ability**, weakly correlated, with long-tailed headroom. Scouting
  reads evidence correlated with the ceiling, never the ceiling itself.
- **Growth driven by work, not time**, and **decline must exist**.
- **Identity owned by people**, aggregated by creative voice. Session players are capability with
  zero voice.
- **Method.** Observe-only first, a churn budget, the cause-mix check, the 1969 spread check, and
  `ArtistEvolution.RngNamespace` (`ArtistEvolution.cs:58-61`) as the tie-break namespace.
- **Two of the three named bugs are real as stated:**
  - `RefreshDispositionIfLineupChanged` compares only `dispositionMemberCount`
    (`ArtistEvolutionService.cs:108-114`), so a 1-for-1 swap re-derives nothing.
  - `GetMainWriter()` is `FirstOrDefault` (`SimulatedArtist.cs:385`). The real problem is worse than
    the sketch says (§2.3).

---

## 2. What the sketch gets wrong or leaves out

Every item is verified against the tree. They are ordered by how much they would cost if built as
sketched.

### 2.1 Nobody talks — the author's actual request is unaddressed

The sketch is entirely simulation-side. Its only player surfaces are the scouting pad and "poach a
member". Nothing tells the player the drummer is fed up, nothing lets the player act on it, and
nothing tells the player that a rival's band split. A departure the player discovers from a roster
count is a statistic, not a story. → Part B (§5) is half of this directive.

### 2.2 Romance is an enum value with no writer

`StrainCause.Romance` is declared and nothing ever produces it. The author named it specifically.
→ §4.9.

### 2.3 Credit inequality is binary by construction, so it would win every comparison on shape

Every artist original is credited `share = 1f` to `GetMainWriter()`
(`CompositionCatalogService.cs:369-384` and `:513-521`), the first active member flagged
`isPrimaryWriter`. Generation can flag two writers in a band (`ArtistManager.GenerateBand`: the first
member at 70%, the lead guitarist at ~35%), but only the first in list order is ever credited. Credit
share inside a band is always 100/0/0/0, chosen by list order. The sketch's 48/44/6/2 Lennon /
McCartney / Harrison / Starr split cannot occur.

Fed into `AccrueStrain`, that puts every non-writer pair in every band with a writer at the maximum
credit gap from the first project. That is exactly the "saturating input wins every `max()` on shape
rather than magnitude" failure the sketch warns about in its own §9. → Co-writing has to exist
before credit can be a grievance (§4.4, Phase 1).

The ledger has a second problem: it is keyed by `personId` alone (`CompositionCatalogService.cs:38`,
`:611`). Once people move between acts, it cannot separate credits earned in one band from credits
earned in the next. → Key it `personId|artistId`, with a person roll-up.

### 2.4 The solo gate measures elapsed time, not fame

`EvaluateDeparture` makes a member solo-viable when `personalRecognition > artist.publicRecognition * .45f`.
The member side is written by `ShareArtistRecognitionGain`: 45% of each realized act gain, split by
visibility weight (`MusicianRecognitionService.cs:19-26`).

Take a four-piece whose singer is also the band leader (stage presence .6) and whose three sidemen sit
at the generation mean (.42). The visibility weights come out at .96 for the singer and .35 for each
sideman, so the singer banks **~22%** of every gain the act banks.

Then the two stocks diverge. `DecayRegistryForWeek` decays `publicRecognition` (half-life ~5.3 years,
`ArtistRecognitionService.cs:39`, `:113-121`) and never touches `personalRecognition`. The ratio starts
near .22 and rises only as the act's stock decays and is topped back up. It crosses .45 about 5.6
years after a fading act's peak, or after a long run of sustained success. **The gate opens with
time, not because the public learned one name.** A forgotten 1961 one-hit act's singer passes it in
1967.

Two smaller facts:

- The member stock is zero for everyone unless `ArtistRecognition.Observing` is set.
- A solo act's only member sits exactly at the threshold.

→ Fix (§4.6):

- Decay member recognition at the act's rate.
- Define **spotlight share**: one member's recognition over the sum across active members. This asks
  whether the public learned one name.
- Require an **absolute bar** on the member's own recognition. This asks whether that name is enough
  to launch a record.

Solo viability needs both.

### 2.5 Most acts never accrue anything

Strain accrues "once per project, from ObserveProject". The median artist releases **two projects in
the decade** ([[careers-are-two-records-long]]). The sketch's worked example is the top half-percent
of the population ([[career-ladder-first-rung-is-unreachable]]).

For everyone else, the event that actually happens is the one the sketch only measures: quiet
disintegration. That already exists. `ApplyTerminalExit` (`ArtistManager.cs:1153-1166`) flips every
member `isActive = false` at once (`:1162`), and the people vanish.

→ Accrue on time spent together under load at the year boundary, as well as per project. Dissolution
releases the people (§4.1) instead of deleting them.

### 2.6 The growth formula depends on how often it is called

`ApplyWork` lerps skill toward the ceiling by `progress * rate * plasticity * .08` **per call**, where
`progress` comes from cumulative `practiceHours`. A hundred one-hour calls move skill much further
than one hundred-hour call. The age decline `vocalPower -= .004 * (age - 34)` is also per call.

So outcomes would depend on how work is chunked into events. A tour logged per leg would grow faster
than the same tour logged once, and any change to event granularity would move results. → Closed form
(§4.10):

- Accumulate additive **effective hours**. The sum is the same however the work is chunked.
- Compute skill as a pure function of formation skill, ceiling, effective hours and age.
- Recompute decline from age each year instead of subtracting it per call.

### 2.7 Growth and a cohesion stock move the whole calibrated economy

`CalculateBaseQuality()` is read in over twenty places, including:

- the unsigned-talent ranking (`ArtistManager.cs:1410`);
- the advance talent multipliers (`AILabel.cs:335`, `PlayerDesk.cs:1163`);
- the scouting truth (`ScoutingPerception.cs:34`);
- album cohesion ceilings (`CompetitorManager.cs:3255`).

Every skill or cohesion change lands on all of them. If the population's mean skill climbs through
the decade, 1969 is a different economy. The sketch's "wider, not higher" is a measurement with no
mechanism behind it.

A cohesion stock has the same problem in the other direction. Most records flop, so "a flop lowers
it" drags the population mean down unless the shocks are sized against the base rates.

→ Growth runs **shadow first**: grown skill is computed beside the live field and the drift is
reported before anything writes (Phase 5). The cohesion stock's first consumers are departure
pressure and the session variance range, which leave mean base quality alone (§4.5).

### 2.8 Phase 1 as written is not economy-identical

1. Drawing five new axes and two ceilings from the population stream reshuffles every draw after it
   ([[one-conversion-reshuffles-the-rng-stream]]). New per-person values must come from a keyed hash,
   the way `ConfigureJazzInstrumentalist` does today
   (`RepertoireTaxonomy.Unit($"{SimulationSeedBootstrap.RequestedSeed}|…|{artistId}")`).
2. `TechnicalSkill => Max(instrumentalSkill, isLead ? (vocalPower + vocalControl) * .5f : 0f)` does
   not reproduce the stored `technicalSkill` unless the new axes are generated *around* it.

→ `technicalSkill` stays the stored, authoritative value in Phase 1. The new axes are anchored to it,
nothing existing reads them, and byte identity is proved by probe hash
([[probe-run-byte-comparison-proves-inertness]]).

The first reader to switch is `PolarActProfileDeriver`. Its header already concedes that "all vocal
nuance/diction is inferred" (`PolarActProfileDeriver.cs:4`), so the split pays a real debt. But that
switch moves material fit, so it is its own measured step (Phase 5b).

### 2.9 The record-quality "fix" is a stream reshuffle, not a fix

`CalculateRecordQuality`'s two `GD.RandRange` calls (`SimulatedArtist.cs:186-192`) are not "the last
global-stream draw in the artist path":

- `CompetitorManager.cs:3215-3219` makes five more right after them (hook, production, originality,
  danceability, controversy), and album generation makes more.
- They don't override fit either. `SongMaterialApplicationService.cs:25-27` multiplies hook and
  production by the fit execution factor *after* the luck.

Deleting the draws shifts every later draw in the run, which amounts to a recalibration.

→ **Keep the draws and change their ranges.** The variance range is already
`(1 - groupCohesion) * .2f`. Feed the room's state into it: the morale stock, the worst strain
present, and session humiliation. The draw count is unchanged, so the stream stays aligned. A band
that can't stand each other becomes erratic in the studio — mostly worse, occasionally better — and
the mean is unchanged by construction.

### 2.10 Fields with no reader, and one sign error

- **`sightReading`** has no consumer, because no session-musician market exists. Add it together
  with its reader (session hire, §5.4) or not at all ([[criticalacclaim-is-a-dead-field]]).
- **`creativeRivalry`** is declared but never accrued in the sketch. §4.4 gives it a writer and two
  readers.
- **`MoneyDispute`** has no input. `Musician` has no income field, and money's only causal effect
  anywhere is the recoupment shield. With co-writing, money and credit read the same ledger, so they
  merge into one cause.
- **The reliability grievance is sign-inverted.** `dragGrudge` multiplies by
  `Max(a.temperament, b.temperament)`. In this codebase, high `temperament` means even-tempered: it
  adds to cohesion (`SimulatedArtist.cs:177`), and `(1 - temperament)` is the drama term
  (`Musician.cs:64`). As written, the calmest member resents the unreliable one most. Use
  `(1 - temperament)`.

### 2.11 Every lineup is treated as a democracy

The pairwise model fits a four-piece rock band. It fits much of the rest of the decade badly:

- **Leader and sidemen.** The bandleader acts that Easy Listening and the new Jazz combos generate
  (`ArtistManager.ConfigureEasyListeningBandleader`, `ConfigureJazzInstrumentalist`). Sidemen are
  employees; their grievance is pay, and replacing one is routine.
- **Name-owned vocal groups.** The Drifters' and the Platters' managers owned the name and changed
  singers at will. A Spector record could be credited to the Crystals and sung by someone else. A
  departure there is a hiring decision, not a crisis. `ManagerArchetype.Svengali` is the natural
  owner.
- **Duos.** One edge, and a split ends the act.
- **Solo acts.** No internal edges at all. Their pressures come from outside: a partner, the label,
  the road.

→ A derived `LineupConstitution` (§4.3). It changes what a departure *means* without adding a stored
field.

### 2.12 People evaporate when bands end

A dissolved act's members are deactivated forever (`ArtistManager.cs:1162`). The 1960s ran on people
moving between acts:

- the Byrds and the Mamas & the Papas came out of folk groups;
- Cream, Crosby, Stills & Nash and Blind Faith were assembled from other bands;
- Hendrix played behind other people's acts first.

With no person pool there are no supergroups, no replacement market, no free agent to poach and no
solo career to spin out.

This is also a supply channel. Post-1964 formation is vacancy-bound with a drained fresh pool
([[post-1964-formation-is-vacancy-bound]]), and folk-rock is supply-bound
([[folk-rock-conversion-is-supply-bound]]). Ex-folk musicians forming folk-rock acts is how folk-rock
actually got its supply. → §4.1, §4.7. The binding rule (§2.19): recombinations *fill* formation
vacancies; they never add acts on top of them.

### 2.13 Much of the decade happened to bands from outside

The sketch only generates strain from inside the band. A large share of real lineup changes came
from outside:

- the draft;
- exhaustion (Brian Wilson left the road at the end of 1964 and stayed on as a studio-only member);
- busts (Jagger and Richards, 1967);
- marriages;
- deaths, on the road (Patsy Cline 1963, Otis Redding 1967) and from substance use (Brian Jones 1969).

→ §4.8: life events as keyed annual hazards.

### 2.14 Strain is a sum, so every long-lived band eventually splits

`Accrue(..., Mathf.Max(0f, incoming - solvent), year)` only adds, and no decay is specified anywhere.
Against fixed thresholds (.5 / .8), the longer a band survives, the more certain its split, at any
rate. → Strain relaxes each year (§4.4), so the thresholds compare a level against a level.

### 2.15 Departures are dice, not stories

`EvaluateDeparture` returns true and the member is gone. The player gets no warning, no window and no
decision. → Strain moves through visible stages — **Brewing → Ultimatum → Departure** (§4.6). Each
stage produces a signal from the member, and Part B lets the player act inside the window. AI acts go
through the same stages and are resolved by a default policy.

### 2.16 Personality never changes

`ego`, `ambition`, `loyalty`, `temperament` and `reliability` are set at generation and never move.
The shy bassist who becomes famous and then difficult cannot happen. → Bounded drift, computed at the
year boundary from state (§4.11):

- fame raises ego;
- tenure and fair treatment raise loyalty;
- road load wears down temperament;
- substance wears down reliability.

### 2.17 The cause-mix target is a guess presented as a benchmark

"Reliability 30%, DirectionDispute 25%, …" and "most charting bands had at least one change" have no
source, and this project sizes against measured references. → Before Phase 4 ratifies, build a
hand-coded **reference set** in `Data/`:

- rows: 1960s acts with a Hot 100 top-40 entry;
- coded for lineup change, split, solo spin-out and cause class;
- a source per row.

Until it exists, the sketch's mix is a prior, and any report that uses it says so.

### 2.18 Keyed draws need the world seed

The sketch keys its tie-break on `artistId|personId|year` with no seed. Ids come from counters
(`mus_000123`), so the same id gets the same draw in every world. That correlates seeds 1001 and 2002
on exactly these mechanics and weakens the two-seed check.

→ Every keyed draw in this directive includes `SimulationSeedBootstrap.RequestedSeed`, as
`ConfigureJazzInstrumentalist` already does. Cosmetic prose picks such as
`ArtistEraSummaryComposer.Pick` can stay seedless.

### 2.19 Solo spin-outs add acts on top of a calibrated population

A new solo act is a new supplier competing for chart share. `Directive6-Codex.md:295` deferred lineup
replacement for this reason — it would activate "an uncalibrated second population system".

→ Spin-outs and recombinations go through the formation servo's accounting
(`MaterializeRuntimeFormation`, `ArtistManager.cs:793`). A solo career fills a vacancy the servo
would otherwise have filled with a fresh act. **They change who forms, not how many.** Phase 0
verifies the hook.

### 2.20 One person in two lineups would load as two people

Members are serialized inside `artist.members`. Suppose the same `Musician` object sat in an old act's
list and a new act's list:

- `isActive`, `isLeadVocalist`, `joinedYear` and `reasonLeft` are **membership** facts stored on the
  person, so the old act would read the person as active again;
- a save/load would split one person into two objects that then diverge.

→ §4.1: a person lives in exactly one place — the current act's `members` or the person pool. Past
stints are frozen `AlumniRecord`s.

---

## 3. Not building (scope fences)

- **Managers are not agents.** They stay a lookup table (`ManagerProfile.cs:17-19`). A manager
  touches lineups only through a constitution (name ownership) and through modifiers.
- **No dense relation matrix, no weekly sweep** over the person registry. Work happens only at the
  year boundary and at existing event seams.
- **No genre-drift rewrite.** `ArtistEvolutionService` is calibrated. Identity spread may become a
  bounded new input to existing pressure (Phase 7); it does not replace ratification.
- **No controversy reader here.** Busts and scandals finally give `Record.controversy` the *writer*
  that `CelebrityRecognitionDirective.md` §8.3 asked for ([[controversy-is-a-cosmetic-record-field]]).
  The awareness-up / radio-down reader is that directive's mechanism.
- **No explicit content.** Romance is written in a gossip-column register — seen with, ran off with,
  married, divorced — and always happens off-screen. (Author-confirmed, 2026-10-06.)
- **Death is written as the period press wrote it.** "The plane went down outside Madison." "Found
  at his home." No detail beyond that, no comedy, no choice that monetizes grief without the scene
  saying so plainly.
- **The draft stays shallow.** It follows the historical induction curve, scaled to how much it
  actually touched acts. There are no deferment strategies, reserve enlistment or lottery numbers in
  this directive.

---

## 4. Part A — the simulation

### 4.1 The person is the unit; the act is a coalition

- **`Musician` stays the type**, so every existing reader compiles. A person lives in
  `artist.members` while they are in that act.
- **`PersonPool`** (new, `Systems/PersonPool.cs`) holds people between acts, keyed by `personId`. It
  is sparse. On departure or dissolution a person enters it only if they have a career to continue:
  `personalRecognition > ε`, any writer credit, skill above the population median, or age under 30.
  Everyone else leaves music with a `reasonLeft`, as now.
- **`AlumniRecord`** (list on `SimulatedArtist`):
  `personId, name, role, joinedYear, leftYear, reason, departureKind`. This is the old act's memory
  of the stint. The `Musician` object itself moves on.
- **On joining a new act**, the membership fields reset: `primaryRole`, `isLeadVocalist`,
  `isPrimaryWriter`, `isBandLeader`, `joinedYear`, `isFoundingMember`. Person fields carry over:
  traits, skills, recognition, life state, practice.
- **Writer ledger** keyed `personId|artistId`, with a person roll-up, so credits are per stint.
- **Saves** gain the pool and the alumni lists. Bump the version in `SaveGameService`, and prove the
  change with `SaveLoadRoundTripRunner` and `--inspect-slot` ([[headless-save-inspection-probe]]).

### 4.2 Member data — every field names its reader

| Field (on `Musician`) | Written by | Read by | Phase |
|---|---|---|---|
| `instrumentalSkill`, `vocalPower`, `vocalControl`, `diction` | keyed generation anchored on `technicalSkill`; growth (§4.10) | `PolarActProfileDeriver` (Musicianship, VocalPower, VocalNuance, LyricDelivery); `RecalculateStats` once growth is live | 1b (inert) → 5 |
| `ceilingInstrumental`, `ceilingVocal`, `developmentRate` | keyed generation (§4.13) | growth; scouting tells | 1b → 5 |
| `formationSkill` (per axis — the growth origin) | set once at generation / Phase 1b migration | growth closed form | 1b |
| `effectiveHours` | work events (§4.10) | growth; scouting improvement delta | 5 |
| `roadYears` | annual work proxy | decline, Burnout, romance and road hazards | 2 |
| `substanceLoad` | life events (§4.8) | reliability drift, Reliability strain, hazards | 2 |
| `partner` (sparse ref, §4.9) | romance events | Romance strain, road reluctance, Band Room prose | 2 |
| `lifeState` (`Active`, `Drafted(untilYear)`, `StudioOnly`, `Retired`, `Deceased`) | life events | departure, lineup, scenes | 2 |
| `sightReading` | keyed generation | session-hire price and outcome (§5.4) | 3 |

The identity axes (`toughness`, `sophistication`, `sincerity`, `maturity`) move to people in
Phase 7, not before.

### 4.3 Lineup constitution (derived, never stored)

| Constitution | Derived from | Edges | What a departure is |
|---|---|---|---|
| `Band` | `Band` / `Trio`, not an instrumental-leader act | all pairs among voiced members | the full model |
| `LeaderAndSidemen` | `instrumentalPerformance` with a band leader | leader↔sideman only; grievance is pay | routine replacement; strain rarely passes Brewing |
| `NameOwned` | `VocalGroup` with a `Svengali` manager | member↔owner, not member↔member | the owner replaces; the member leaves with only their own recognition |
| `Duo` | `Duo` | one edge | a split ends the act; both people go to the pool |
| `Solo` | solo types | none internal | outside pressures only (§4.8, §4.9) |

### 4.4 Relations — strain, rivalry, and how each accrues

```csharp
[Serializable]
public sealed class MemberRelation {
	public string personA, personB;   // ordinal order, A < B
	public float strain;              // 0 fine .. 1 one of them is leaving
	public float rivalry;             // productive creative tension -- NOT strain
	public float[] causeWeight;       // per StrainCause, relaxed with strain; prose and telemetry read the mix
	public bool secret;               // a Romance cause the wronged party hasn't discovered (§4.9)
	public int lastEventYear;
}
public enum StrainCause { CreditAndMoney, Spotlight, Direction, Reliability, Romance, Burnout, Outsider }
```

Edges live on the act (`artist.relations`, null until the first one). An edge is dropped when both
strain and rivalry fall below ε.

**Accrual** happens at the year boundary (the `formationYear != date.year` rollover in the lifecycle
tick, `ArtistManager.cs:784`) for every working group act. A smaller per-project term is added in
`ObserveProject`, weighted so prolific acts don't accrue faster merely by releasing more.

| Cause | Incoming term for pair (A, B) | Note |
|---|---|---|
| CreditAndMoney | `max(gapAB·b.ambition·b.ego, gapBA·a.ambition·a.ego)` | gap = this act's credit-and-royalty share difference (`personId\|artistId` ledger); a contented sideman generates nothing |
| Spotlight | `\|spotA − spotB\| · max(a.ego, b.ego)` | spotlight share (§2.4), never raw recognition |
| Direction | until Phase 7: distance between the two members' contributions to `experimentalAppetite` and `commercialPragmatism`; after: identity distance | |
| Reliability | `max(1−a.reliability, 1−b.reliability) · max(1−a.temperament, 1−b.temperament)` | sign fixed (§2.10) |
| Romance | event-driven only (§4.9) | |
| Burnout | `roadLoad · (1 − min(temperament))` | shared, not a grudge, so it lands on the weakest edge |
| Outsider | written by events and player verbs | partner at sessions, session player replaced them on the record, label took a side |

**A level, not a sum:**

```
strain ← strain · StrainRetention + max(0, incoming − solvent)
solvent = success + restingCohesion · .3
```

- `StrainRetention` is roughly .6 per year and is calibrated in Phase 2.
- `success` is this year's chart evidence: charted, top-40, regional breakout.
- `success` is deliberately **not** the morale stock. Feeding morale into the solvent would close the
  departure cascade loop through the very term meant to damp it.

**Rivalry** accrues only between two credited writers on the same project, scaled by
`min(creativity) · min(ambition)`, and relaxes like strain. It has two readers:

- **Writing:** a bounded bump to the songwriting term on projects where both write. This shifts the
  mean, so it is measured in Phase 4.
- **Strain:** a slow feed into Direction and CreditAndMoney. The band's best records and its eventual
  split come from the same cause.

### 4.5 Morale — cohesion becomes a stock, in stages

`restingCohesion` is today's formula, unchanged, and keeps feeding `CalculateBaseQuality`. `morale`,
in `[−.3, +.2]`, is the deviation from it. It moves on events:

| Event | Effect on morale |
|---|---|
| a hit | + |
| a flop | − (sized so the population's expected annual drift is about zero at observed hit/flop base rates) |
| a long road year | − |
| a new lineup | + (the honeymoon) |
| a forced song, or session humiliation | − |
| a successful Polar stretch | + |

Morale's consumers come online in order:

1. departure-stage speed (Phases 2/4);
2. the session variance range (§2.9) — stream-safe and mean-neutral;
3. Band Room tone (Phase 3);
4. `CalculateBaseQuality`, only after a measured A/B (optional, Phase 5+).

### 4.6 Departure — stages, kinds, the breaker

Evaluated at the year boundary for each member, on that member's worst edge:

| Stage | Entry | What happens |
|---|---|---|
| Brewing | worst strain ≥ .45 | the member starts complaining: a Band Room visit for player acts, trade-press gossip for AI acts (Phase 4) |
| Ultimatum | worst strain ≥ .65, or Brewing two years running | a stated demand from the dominant cause: "half the B-sides", "him or me", "off the road" |
| Departure | the Ultimatum is still unresolved at the next evaluation, or strain ≥ .85 | the member leaves; the kind is chosen below |

**Kinds:**

| Kind | When |
|---|---|
| `SoloCareer` | `WouldConsiderSoloCareer` (now with real inputs) **and** spotlight share ≥ .5 **and** `personalRecognition` above a launch bar, sized so a solo debut isn't anonymous |
| `Acrimony` | the default for a strain-driven exit |
| `Fired` | the act or label chooses; dominant cause Reliability or Outsider |
| `Service` | drafted (§4.8.1); a two-year absence, not a departure from the person's side |
| `Death` | §4.8.2 |
| `LifeEvent` | the other §4.8 events |
| `StudioOnly` | Burnout, for a writer or a member with a high studio role |
| `Dissolution` | a duo split, or the last voiced member leaving |

**Guards:**

- `MemberChangeCooldownYears = 1` per act.
- A new-lineup honeymoon (a morale bump).
- An annual churn **breaker**. It is a circuit breaker, not the rate. Set it at roughly twice the
  would-be rate observed in Phase 2. When it trips, the lowest-strain departures are deferred a year,
  ordered by strain and then by a keyed tie-break.
- The rate itself comes from the mechanism and is checked against the reference set (§2.17).

### 4.7 After a departure

- **Replacement** comes from the person pool, matched on role, skill and region. If the pool has no
  fit, a new person is generated from a keyed draw, never from population RNG.
  - AI acts follow a deterministic rule: charting acts replace. A non-charting act that loses a voiced
    member dissolves, with the probability rising with strain.
  - Player acts choose (§5).
- **Identity and disposition re-derive** for the remaining lineup. The swap bug is fixed by hashing
  the active person ids instead of counting them.
- **Name ownership.** A `NameOwned` act's owner keeps the name. A `Band` keeps it unless the departing
  member held the majority of voice weight; in that case the act dissolves and the rest go to the
  pool.
- **Solo spin-out.** A new `SimulatedArtist` is built around the person and inherits their
  recognition, credits and partner. It is counted against the formation servo (§2.19).
- **Leaving-member clause.** Period contracts commonly gave the label an option on a departing
  member. `ContractTermSheet` gains `leavingMemberOption`.
  - The player exercises it for their own acts (§5.4).
  - AI labels follow a rule: exercise it when the departing member's recognition clears the bar and
    the label can afford it.
- **Recombination.** When the servo fills a vacancy and the pool holds at least two compatible people
  (region, genre adjacency to the vacancy's genre, age), it may build the new act from them. That gives
  people continuity, and it gives emergent genres supply from adjacent scenes. The share of vacancies
  filled this way is a Phase 4 parameter, measured.

### 4.8 Life events — keyed annual hazards

All are evaluated at the year boundary and keyed on `seed|personId|year|event`. None touches the
population stream.

| Event | Who / hazard | Effect |
|---|---|---|
| Draft | §4.8.1 | `Drafted(+2y)` |
| Exhaustion | `roadYears × (1 − temperament) × age` | `StudioOnly` (stays for records, is replaced live) or retirement |
| Substance | ramps up from 1965: `(1 − reliability) × fame × scene` (rock, psych and jazz higher) | `substanceLoad` up → reliability drifts down → Reliability strain; a bust is a trade-press scandal; it feeds the death hazard (§4.8.2) |
| Marriage / children | ramped by age | road reluctance (Burnout); partner presence (Outsider); a father is draft-exempt |
| Death | §4.8.2 | `Deceased` |

#### 4.8.1 The draft — the real curve, scaled to its real reach

The draft follows its historical shape and is deliberately shallow:

```
hazard(person, year) = DraftActExposure × inductions(year) / eligiblePool(year)
```

- **`Data/DraftInductions.csv`** holds the annual US induction totals and an estimate of the
  eligible pool (men 19–26), one sourced row per year from 1958 to 1970. The shape it has to carry
  is roughly flat and low through 1964, then escalating sharply from the 1965 build-up and staying
  high through 1969. Elvis's 1958–60 service is the decade's opening precedent. The December 1969
  lottery only affects 1970 inductions, so it is out of scope.
- **`DraftActExposure`** is one scalar. It absorbs everything this model deliberately leaves out:
  failed physicals, reserve and Guard enlistment, deferments, and the fact that working musicians
  weren't a random sample of 19-year-olds. Tune it so the share of reference-set acts (§2.17) that
  lost a member to service matches history. The reference set gains a `Service` cause column for
  this.
- **Who is eligible:** male, age 19–26, in an act with a US home region, not a father. Phase 0
  checks whether the region model contains any non-US acts at all.
- **Effect:** two years as `Drafted`. The act replaces the member (AI rule, or the player's choice)
  or goes dormant if he was its voice. On return he rejoins if the act still exists and still wants
  him, otherwise he enters the pool. Practice hours don't accrue while he's away; recognition decays
  as normal.
- **Player view:** the letter arrives as a visit (§5.2). The verbs are the ordinary lineup verbs —
  replace him, carry on short-handed, or put the act on ice.

#### 4.8.2 Death — rare early, rising toward the decade's end

Labels in the 1960s lost roster artists, and the game should too. Death is **on**, rare, and shaped
by period causes rather than one flat rate. Its frequency over the decade isn't authored; it emerges
from how its inputs build up:

| Channel | Hazard | Shape across the decade |
|---|---|---|
| Travel | `roadYears` × success (successful acts flew small charters) × a small era travel rate | present all decade and the main early cause (Patsy Cline 1963, Jim Reeves 1964, Otis Redding 1967) |
| Illness | age-based, small | negligible for a young population; a few older acts (Nat King Cole 1965) |
| Misadventure | a tiny constant | Sam Cooke 1964, Bobby Fuller 1966 |
| Substance | `substanceLoad` above a threshold × years at that level | close to zero before 1966; rises through 1967–69 (Frankie Lymon 1968, Brian Jones 1969) |

- **The 27 Club is an outcome, not a rule.** No term keys on age 27. Substance deaths cluster in the
  mid-twenties because that's where several things peak together: years of use, fame (which raises
  the substance hazard), and the late-decade era ramp. If the model's substance deaths don't cluster
  there, that's a calibration failure to report — never a reason to hardcode the age.
- **Calibration** uses the reference set's deaths among charting performers by year (sourced). The
  model is matched on *charting* people, because deaths in the obscure 90% of the population have no
  historical record to compare against. Early-decade rates must stay rare: Phase 2 counts deaths by
  year and channel before anything writes.
- **The act afterwards** applies the ordinary §4.7 rules:
  - a voiced majority dissolves the act;
  - a sideman is replaced (the Bar-Kays were rebuilt after Redding's crash);
  - the survivors take a large morale shock;
  - strain between survivors is partly forgiven, with a one-time cut to every edge.
- **Afterlife:**
  - the person's recognition takes a one-time spike;
  - `culturalStanding` comes through the existing standing seam;
  - masters the label holds become a posthumous-release opportunity ("Dock of the Bay" went to #1
    in 1968). AI labels release by rule; the player decides (§5.4).
- **The player can see it coming, and can act.**
  - Street surfaces a substance problem as it grows.
  - Time off and drying out lower `substanceLoad`.
  - Cutting the road schedule lowers travel exposure.
  - A death on the player's roster is never pure dice: the road and substance inputs it ran on are
    readable on the roster card well beforehand. Travel and misadventure deaths are the exception,
    and they stay rare.

### 4.9 Romance — sparse, can be secret, gender-neutral

- **`partner`** is a sparse reference `{name, personId?, sinceYear, state}`. The partner may be a
  `Musician` (in this act or another) or a name only. It is allocated only when an event needs one.
  The model is gender-neutral; prose takes pronouns from the partner record.
- **In-band couple.** In a mixed lineup, the chance rises with proximity × years together. While the
  couple lasts, morale is up. When it ends, it leaves large Romance strain on that edge, with some
  spillover onto the others (the Mamas & the Papas, 1966).
- **Affair.** A with B's partner. The hazard is `(1 − a.loyalty) · a.ego · roadLoad · fame`.
  - The edge is created `secret = true` and carries no strain while it stays hidden.
  - **Discovery** is its own annual hazard, raised by road time and press attention.
  - On discovery, Romance strain spikes and B's partnership ends.
- **Information asymmetry.** While the affair is secret, the player's Street instinct can learn of it
  first (§5.3). That is the "behind their back" the author asked for, and it hands the player a real
  dilemma.
- **Partner as outsider.** A partner who sits in on sessions or manages one member adds Outsider strain
  with the others. In Phase 7 the partner also pulls that member's identity.

### 4.10 Growth and decline — closed form

```
effectiveHours += hours × kindWeight × developmentRate × plasticity(ageThatYear)   // additive: chunking can't change the sum
skill(axis)     = formationSkill + (ceiling − formationSkill) × (1 − exp(−effectiveHours / H))
                  − decline(axis, age, roadYears, substanceLoad)                   // recomputed from state, never accumulated
```

- **`kindWeight`**, from the sketch's table: residency 1.0, road .65, session .45, rehearsal .35, idle
  ≈ 0.
- **Work proxy for AI acts.** No gig simulation exists, so annual hours come from the act's state:

  | State | Annual hours |
  |---|---|
  | Unsigned / Seeking | the room class of their genre family; club rooms play most nights |
  | Signed | sessions per project, plus road time scaled by chart evidence |
  | Latent / reservoir | idle |
  | Player acts | what the player actually booked: sessions, record hops (`RecordHopHours`), and the new residency/tour booking (§5.4) |

- **Decline:**
  - `vocalPower` falls after about 34, faster with `roadYears`;
  - instrumental plasticity falls after about 26;
  - `creativity` peaks around 30;
  - `reliability` falls with `substanceLoad`.
- **Ceiling nudge — the Harrison case.** A member credited alongside a much stronger writer for at
  least three years gains a small, capped amount of creative headroom. It is rare by construction.
- **Phase 1b start state:** `formationSkill = technicalSkill` and `effectiveHours = 0`, so growth
  starts at exactly zero.

### 4.11 Personality drift — bounded, annual, from state

| Trait | Drifts with |
|---|---|
| `ego` | `restingEgo + k₁·personalRecognition` |
| `loyalty` | tenure, and the player's treatment (§5.4) |
| `temperament` | wears down with `roadYears` |
| `reliability` | wears down with `substanceLoad` |

Each trait stays within ±.2 of its generated value: people change without becoming someone else.
`DeriveDisposition` re-runs when any trait moves past a step.

### 4.12 Identity on people (Phase 7)

Keep the sketch's §5 voice-weighted aggregation, with these changes:

- People are generated near their formation act's genre prior, with a small spread inside each band —
  bands form from people who want the same thing.
- Members drift through `CulturalMemoryService`'s existing exposure.
- `identitySpread` feeds the Direction cause and a bounded `interpretiveReach` term.
- Session players carry about zero voice weight.

It does **not** replace the evolution ratifier.

### 4.13 Scouting the rough

- **Generation.** Ceilings come from keyed draws with ≈ .35 correlation to current ability and a
  long-tailed headroom distribution. Most acts are already at their ceiling.
- **Tells, not truth.** The player sees:
  - age;
  - repertoire reach — the live set's Polar demand against the act's capability, reusing
    `PolarMaterialFit`;
  - originals in the set;
  - ambition, partly visible;
  - the **improvement delta**: revisiting a `WatchNote` (`PlayerDesk.cs:224-236` already stores
    `LastSeen`) three months or more later reads the change in Execution.
- **Structured errors.** The potential read is built from the tells, so it is wrong the way the tells
  are wrong. An over-reaching act with a low ceiling reads as promising; a finished pro with untapped
  vocal headroom reads as done. It is never noise added to the ceiling, which would just be a noisy
  reveal of the answer.
- **Display.** The sketch's Execution / Identity / Potential (wide|narrow) lines, written in the
  ear-read vocabulary the polar branch added.

### 4.14 Co-writing — songs have teams

**Today.** Every song has exactly one credited writer at `share = 1f`:

- An artist original goes to `GetMainWriter()` (§2.3).
- A professional song goes to one staff writer drawn from its publisher
  (`CompositionCatalogService.cs:310-315`).
- `ProfessionalSongwriter` already carries separate crafts — `melodyCraft`, `lyricCraft`,
  `hookCraft` — so a team built from complementary specialists is representable today. Nothing ever
  builds one.
- The decade's songbook was built by teams: Goffin–King, Bacharach–David, Holland–Dozier–Holland,
  Leiber–Stoller, Lennon–McCartney, Jagger–Richards.

Co-writing comes in four layers. Each layer is gated on its own, because the first is inert and the
rest are not.

**1. Credit teams (inert; Phase 1).** For each artist original, a keyed draw picks the writing team
from the act's voiced writers, weighted by `creativity × ambition`. Most songs have one writer, some
have two, and minor writers get an occasional contribution (the Harrison quota). Credits split across
the team. A **pact** between two writers — the Lennon–McCartney convention — credits both equally on
anything either writes. Pacts form when two members have co-written over a run of songs, or when the
player concedes one (§5.4). Song traits are untouched, so the economy stays byte-identical. This is
the layer that gives CreditAndMoney a real gap to measure (§2.3).

**2. Team craft (world; Phase 4c).** A team's song draws on its members' crafts.

- For members, the crafts are derived rather than stored:
  - melody from `creativity × musicalVersatility`;
  - lyric from `creativity × diction` (Phase 1b axis);
  - hook from `creativity × stagePresence`.
- A team scores the stronger of each craft, less a friction term. That makes complementary
  specialists the strongest pairing and two of the same kind barely better than one.
- Rivalry (§4.4) adds a bounded bonus.

This moves hooks, so it moves the economy: it is measured on its own decade A/B, with the population
mean hook held inside the noise floor. Without that check, "teams are better" quietly inflates every
record.

**3. Professional teams (world; Phase 4c).** At catalog initialization and at writer turnover, staff
writers at the same publisher pair into standing teams, preferring complementary crafts. Some staff
writers stay solo. A pro team's songs carry split credits and team craft. The pairing draw is keyed
on the world seed (the existing trait `rng` stays untouched). Team songs replace solo songs one for
one, so the catalog keeps its size.

**4. Cut-ins (world; Phase 4c, player verbs in Phase 3).** In the period, a manager, DJ or label
could add its name to a song for a share of the writer's money. Elvis's publishing arrangements and
Alan Freed's credit on "Maybellene" are the textbook cases.

- A cut-in takes a share of a member's credit. It writes CreditAndMoney and Outsider strain onto
  that member's edge with the label — the first strain that the label, not a bandmate, causes.
- AI labels cut in by a rule keyed to `ManagerArchetype.Shark` and `labelOwnsPublishing`.
- The player can demand one (§5.4) and take the money and the grudge.

**Readers.** Co-writing has three:

- CreditAndMoney strain, from the ledger keyed `personId|artistId`.
- `creativeReputation`, which now accrues to each credited writer by share. That gives
  `CelebrityRecognitionDirective.md` §8.3's "famous writer" channel its input.
- After a split, each former partner's songs come from their own crafts alone. The Lennon-without-
  McCartney record is measurably different without any special case.

### 4.15 Wealth — people get rich, unevenly (author-approved 2026-10-08)

The case this models: inside a successful band, two members (the writers) become measurably richer and better
known than the rest. That gap is friction, and it is what the press will eventually write about. Wealth has to
be **uneven inside an act**. An even split would be the band's money, which `totalRoyaltyEarnings` already holds.

**The stock (observe-only first).** `Musician.wealth`, updated at the year boundary from figures the economy
already computes. It moves no economy figure:

| Flow | Source | Split |
|---|---|---|
| Performer royalty | the year's increase in `artist.totalRoyaltyEarnings` (already net of recoupment, `CompetitorManager.cs:1212`) | evenly across the members present that year (the period convention); `LeaderAndSidemen` sidemen get a wage instead, and the leader keeps the rest |
| Writer income | the writer ledger's units × credit share × the writer's half of the mechanical (1¢) | per credit, so co-writing (§4.14) is the source of the gap |
| Spending | `wealth × spendRate(fame, ego)` | a sink, not a cap: fame and ego raise the lifestyle, so wealth is a level that rises and falls, never a counter that only grows. Most 1960s musicians went broke; the stock must allow that |

On the AI side, mechanicals live inside COGS (`MechanicalRoyaltyService` is player-only). The writer term here is
**derived for the person's account only** and is never charged to a label.

**Readers, each on its own, in this order:**

1. **Independence.** Wealth lowers the cost of leaving. It raises the chance that an exit is `SoloCareer` or
   `StudioOnly` rather than nothing, and it lowers the weight the act's success carries in the solvent for that
   member. A rich member can afford to walk, and a rich act can afford to stop touring (§4.16).
2. **The money grievance.** CreditAndMoney (§4.4) reads the wealth gap between the pair as well as the credit
   gap: "he's bought a house; I'm still in a flat." This closes §2.10's "`MoneyDispute` has no input".
3. **Lasting fame (small).** A non-decaying recognition term from conspicuous wealth: the estate, the cars, the
   fan-magazine spread. It keeps a rich star known between records. It is kept modest **on purpose**: wealth
   comes from the same records as recognition, so a large term would count success twice. This is the hook the
   press mechanics will read (celebrity interest scales with fame × wealth).
4. **Hazards and visits.** Wealth replaces the fame proxy in the substance hazard. It backs the Band Room's
   advance request and the "we don't need the road" ultimatum with a number.

**What it is not.** It is not the fix for rare solo spin-outs (§11 open item 3). Measured on the bms2 world run:
6,701 group members at decade end, 65 above the 0.05 launch bar, 30 of those also holding half the spotlight.
The best individual recognition was 0.13, while top acts reach 0.66. The binding term is the **member's slice
of act fame**: `ShareArtistRecognitionGain` passes 45% of each gain through a visibility split, so a frontman
banks ~22% of his act's fame. Raising that slice is a separate one-constant world change (Phase 4d). It comes
before wealth, so the spin-out rate it buys is measured on its own.

**Corrected by the Phase 4d A/B (§11, 2026-10-08):** the slice is *a* binding term, not *the* binding term.
Doubling it tripled the members above the launch bar and left spin-outs at 1-2 per window. The term that binds is
`WouldConsiderSoloCareer`'s intent (§11 "Phase 4d").

### 4.16 Road fatigue is a level, not a counter (author-approved 2026-10-08)

`roadYears` only accumulates (`BandLifeService.cs:315`), so exhaustion (`:401`) grows without bound. For club acts
that stay unsigned for years it reached ~380 retirements a year by 1968 (§11). The fix is the strain pattern
(§2.14), a level with a sink, **not a cap**:

```
fatigue ← fatigue + roadLoad                                   // a road year adds
fatigue ← fatigue − recovery · offRoadShare                    // recovery only while OFF the road
recovery = baseRecovery / (1 + k_wear · careerRoadYears)        // the more an act has toured, the slower it heals (burnout)
             · (1 + k_success · success)                        // success motivates: it slows burnout, it never prevents it
```

- **Recovery only off the road.** Rest is the only sink. An act on the road all year does not heal, however
  successful it is.
- **Burnout is the slowing rate.** `careerRoadYears` stays a lifetime counter (it feeds travel and romance
  hazards, where exposure really is cumulative). Fatigue is the level the exhaustion hazard reads. A veteran
  rests as long as a newcomer and comes back less recovered.
- **Success offsets but does not save.** `k_success` is bounded so that a top-40 act on a heavy schedule still
  accumulates. The Beatles in 1966 were the most successful act alive and still stopped.
- **Wealth is the way out** (§4.15 reader 1). Stopping touring costs road income and momentum. A rich act can
  pay that; a poor one keeps going until the hazard fires. The exit becomes `StudioOnly` (the Brian Wilson /
  1966 Beatles case) when the act can afford it, and a retirement or dissolution when it can't.
- **AI work proxy until touring exists.** `offRoadShare` comes from the same annual-hours proxy as §4.10. An
  unsigned club act's year counts as part road and part rest, by its room class. Today's model treats it as
  all road, and that is why the unsigned club population exhausts itself. Player acts read the booked calendar
  once tour/residency booking (§5.4) lands.
- Old saves load with `fatigue = roadYears` scaled by a one-time decay, so a loaded world doesn't retire its
  veterans in a single January.

---

## 5. Part B — the Band Room: characters talk

### 5.1 The grammar is already built

`RolodexScene` (beats, context, conditions, fragments), `RolodexCall`, and the instinct-gated counter
in `PlayerDesk.RolodexVerbs.cs` already provide:

- one grounded objection;
- an answer gated on both an instinct score **and** the fact being true;
- an instinct-without-fact answer, labelled as a bluff that can be called.

`ContractTalk` already reuses that grammar with different nouns, and the Band Room does the same.
**Do not invent a second dialogue system.**

| Rolodex beat | Band Room use |
|---|---|
| Opening | who came in, and how: calls the office, turns up at the session, the manager rings on their behalf |
| PassiveRead | the player's instincts read the room — Ear: who is actually carrying the record; Street: who is seeing whom, who is using; Suit: what the contract says; Fixer: who can be talked down |
| SituationRead | the grievance — one cause, stated with its fact: "Four of the six sides are mine. My name's on one." (ledger) |
| PlayerPitch / Pushback | the player's response (§5.4) and the member's counter |
| Success / Failure | the sim write |
| RelationshipAftermath | the edge, morale and loyalty changes, said out loud |
| Exit | what happens next if nothing changes: "I'm not doing the tour." |

### 5.2 Who comes in, and when

The player's roster has a **visit queue**. It is filled at the year boundary and at project seams,
and capped per week so it never becomes a chore. A visit is queued when:

- a member enters Brewing or Ultimatum;
- a member makes a request backed by a fact:
  - a song on the record (they have credits pending);
  - a solo single (spotlight share);
  - an advance (`unrecoupedAdvance`, a new partner, children);
  - off the road (`roadYears`, partner);
- a life event lands: drafted, arrested, getting married;
- a death. This one is a call, not a visit: the road manager or a bandmate rings the office. The scene
  has no counter beat, only the facts and the decisions that follow (§5.4);
- an affair is discovered;
- the first hit lands, and the credit conversation starts.

AI acts never visit. Once AI churn is live (Phase 4), their stages surface as trade-press gossip in
the desk ledger (`PlayerDesk.Note`, `PlayerDeskPanel.cs:2803`) under a new "Scene" filter. Not before
Phase 4: reporting a would-have split that never happened would be a lie.

### 5.3 Member voice

Fragments are selected by the speaker's personality, role, the cause and the constitution:

- high ego, low temperament → blunt;
- high loyalty, low ego → apologetic;
- low reliability → evasive.

Every fragment's condition reads only the call context, so every line can be audited back to its fact
(the Rolodex invariant).

**Street is the gossip channel.** In a passive read it can surface a secret affair, a substance
problem, or a member who is already talking to another label — before anyone tells the player.

### 5.4 What the player can do — every verb writes the sim

| Verb | Cost | Sim write | Risk |
|---|---|---|---|
| Hear them out | hours | reveals the edge and its cause; small strain relief | the cause is still there |
| Concede credit | future publishing share | co-write pact on the pair (equal split going forward); the CreditAndMoney gap closes | rivalry can fall with it; the other writer's Spotlight strain can rise |
| Give them a song | a slot on the next project | their credit on the next project | fit risk — the Polar read shows it |
| Pay them | label cash | the money part of CreditAndMoney falls | the others notice (Spotlight) |
| Mediate (Fixer) | hours, instinct-gated | strain on the edge falls | fails, or backfires on a called bluff |
| Side with one | — | the favoured member's loyalty to the label rises; the other's strain rises, with an Outsider (label) cause | you've chosen who leaves |
| Spotlight them | the act is renamed "X & the Ys" | their spotlight share and loyalty rise; the others' Spotlight strain rises | speeds up a solo exit — which you can then sign |
| Session player on the record | fee, priced by `sightReading` | that record's execution rises; humiliation adds Outsider strain and cuts morale; the band earns no practice hours | the Monkees' 1967 revolt |
| Time off | weeks | morale rises; Burnout falls | momentum decays while they're away |
| Book a residency / tour | weeks of the act's calendar | `effectiveHours` rises — the Hamburg lever; small income; `roadYears` rises | morale wear, road hazards, affairs |
| Writing session | hours, plus the staff writer's fee if one is used | pairs two members, or a member and a staff writer; delivers a song credited to the team after the commission delay (`CommissionDeliveryDays`); repeated good sessions make the pair eligible for a pact | two high-ego writers in one room raise rivalry and strain together |
| Cut in on the song | — | the label takes a credit share: money now | CreditAndMoney and Outsider strain against the label; the member remembers |
| Dry them out | weeks off the road | `substanceLoad` falls; death and bust hazards fall | momentum decays; the member may refuse (low reliability, high ego) |
| Posthumous release / tribute | the masters the label holds | release or compile; recognition spike; the survivors' morale moves with how it's handled | the scene says plainly what is being sold |
| Fire / replace | hours | a `Fired` departure; replacement from the pool | the fired member's allies take Outsider strain |
| Exercise the leaving-member option | contract terms | the departing member is signed as a new act | you're bound by the paper you signed |
| Lean on the contract (Suit) | — | works on high-loyalty, low-ego members; on high ego it turns Brewing into an Ultimatum | — |
| Affair: tell / keep quiet / keep them apart | — | tell: discovery now, and the wronged member's trust in the label rises; keep quiet: if it comes out later that you knew, their trust falls; keep apart: road scheduling | — |

Loyalty-to-label (the "trust" above) is a per-member float that exists only on the player's roster,
so the AI economy carries nothing new.

### 5.5 The roster card

Each member gets a card showing:

- name, age and role;
- an interpreted personality read — an InsightStrength sentence, never "ego .71";
- who they're not speaking to, and why;
- their partner, if the player knows;
- their current stage: content, grumbling, or has given you an ultimatum;
- for a growth act, the Ear's read on how far they've come since the last look.

---

## 6. Phases

There are three tracks:

| Track | Meaning |
|---|---|
| **F** | inert fixes and scaffolding; byte-identical, proved by probe hash |
| **P** | player-only; the AI economy stays byte-identical in the headless harness; no decade run owed |
| **W** | world; changes AI outcomes; observe first, then a two-seed decade A/B with the canonical flags ([[canonical-decade-run-flags]]) |

All telemetry goes to `SimLogs/<run>-lineup-*.csv` ([[audit-output-goes-to-simlogs]]).

| Phase | Track | Flag | Delivers | Gate |
|---|---|---|---|---|
| 0 | F | — | `PersonPool` and `AlumniRecord` (empty); ledger keyed `personId\|artistId`; `GetWriters()` with weights; disposition refresh on a hash of active ids; member-recognition decay (recognition-gated — prove it inert on an observing run); the servo hook for spin-outs identified and documented; save version bump | probe CSV hashes identical; save round-trip passes |
| 1 | F→W | `--enable-cowriting` | writing teams per original (keyed draw over voiced writers, weighted by creativity × ambition; pacts); credits split; song traits **unchanged** | economy CSVs byte-identical; credit telemetry shows a realistic share distribution, not 100/0 |
| 1b | F | `--enable-member-axes` | split axes, ceilings, `formationSkill`, `sightReading` — keyed and anchored; **no reader switched** | byte-identical |
| 2 | W (observe) | `--observe-band-life` | strain, rivalry, morale, life events (draft, death by channel), romance, stages, and would-be departures by kind and cause; **no writes** to anything that existed before | counts by success tier, cause, year and channel; breaker sized at ~2× the observed rate; `DraftActExposure` and death rates fitted; reference set and `DraftInductions.csv` built |
| 3 | P (dev checkpoint) | `--enable-lineup-churn=roster` | the one churn implementation, scoped to the player's roster, plus the Band Room, visits, verbs (writing session, cut-in, dry-out, posthumous release included), roster cards and residency booking | AI byte-identical in headless runs; playtest on seed 1002 ([[polar-playtest-seed-1001-commission-break]]) |
| 4 | W | `--enable-lineup-churn` (scope = world) | the same code with the scope widened: AI departures, draft and death, replacement, dissolution into the pool, solo spin-outs through the servo, recombination, the leaving-member-option rule, the trade-press "Scene" feed; the Polar deriver re-derives on lineup change | two-seed decade A/B against the §7 measures |
| 4d | W | `--member-fame-share=X` | member slice of act fame raised (§4.15 "What it is not"); the spin-out rate it buys | two-seed decade A/B; spin-outs per decade and their success, against the reference set's solo spin-outs. **Run 2026-10-08 (§12): calibration-neutral, buys no spin-outs alone; default raised to .90 with 4d2 (§13)** |
| 4d2 | W | `WouldConsiderSoloCareer` (no flag) | solo intent: success raises the urge, tenure no longer only raises the ties (§13) | one 1965-69 window from the saved 1965 world with the doubled slice. **Run 2026-10-08 (§13): 13 of 67 groups with 5+ top-40 hits lost a member to a solo career (19.4%, ref 17.6%); 12 of 26 spin-outs charted; calibration-neutral** |
| 4e | F→W | `--enable-member-wealth` | the wealth stock and its sink, observe-only (§4.15); then the readers one at a time: independence, money grievance, lasting fame, hazards | observe stage byte-identical (probe hash); readers sized offline from the observe ledger, smoke-tested on 104-week probes, then bundled into one two-seed decade A/B (see "Test tiers" below); the intra-act wealth gap distribution reported (writers vs non-writers in charting acts) **Built and on by default 2026-10-08 (§14).** |
| 4f | W | `--enable-road-fatigue` | road fatigue as a recovering level with burnout and the success offset (§4.16) | two-seed A/B; exhaustion retirements per year no longer climbing with the unsigned-club backlog; `StudioOnly` exits concentrated in rich, charting acts **Built and on by default 2026-10-08 (§14).** |
| 4c | W | `--enable-team-writing` | co-writing layers 2–4 (§4.14): team craft, professional teams, AI cut-ins | its own two-seed decade A/B; mean hook inside the noise floor **Built and on by default 2026-10-08 (§14).** |
| 5 | W | `--enable-musician-growth` | a shadow pass first (grown skills computed beside the live fields, drift reported), then live: growth, decline, personality drift | mean base quality of active acts within the control's noise floor every year; 1969 skill spread wider than 1961 **Built and on by default 2026-10-08 (§14).** |
| 5b | W | `--polar-member-axes` | `PolarActProfileDeriver` reads the split axes | Polar fit A/B in the polar branch's own harness **Built and on by default 2026-10-08 (§14).** |
| 6 | P | (with 5) | scouting the rough: ceiling tells, the revisit delta, the potential read | playtest **Built and on by default 2026-10-08 (§14).** |
| 7 | W | `--enable-member-identity` | identity on people; spread feeds Direction and reach; partner pull | decade A/B; the evolution ledger's mix unchanged within noise **Built and on by default 2026-10-08 (§14).** |

**Test tiers (author-agreed 2026-10-08).** A decade run per change is too slow. Each world change climbs only as
far as it needs to:

1. **Offline sizing.** Replay constants in Python over an observe run's telemetry (the `fit_band_life_strain.py`
   precedent). No run.
2. **104-week probe.** A probe hash proves an observe stage is inert. For a live change it is a smoke test
   (population, money, exit counts) that catches the horribly amiss, never a verdict: late-building stocks
   (wealth, fatigue) barely show by 1962, and short-run deltas have inverted
   ([[short-run-deltas-can-invert]]).
3. **Single-seed decade**, beside its control, only when a direction is needed.
4. **Two-seed decade A/B** for the bundle of changes that passed tiers 1-3, before merge (the evolution-bundle
   precedent).

**Late-decade windows (built 2026-10-08, `SimTools/ChartAuditRunner.Resume.cs`).**
`--save-world-at-year=1965 --save-world=<name>` writes the AI world to `SimLogs/worlds/<name>.world.json.gz` the
first time the clock reaches that year, and the run carries on. `--resume-world=<name>` loads it before week one
(`--weeks` then counts from there; `--seed` must match the save). The flags that wrote the world are stored with
it, and any difference prints `RESUME_FLAG_MISMATCH`. A treatment arm adding its own flag is the intended use.
Verified: a 1961 world (41 MB) resumed twice gave 80/80 byte-identical CSVs. Against the unbroken run over the
same 17 weeks, formations (688) and drops matched exactly, because the private streams are restored, while
signings moved 474→469 from the reseeded global stream. A 1965-67 window should run ~15-20 min against ~50 for
a decade. Constraints:

- On load the global stream is **reseeded**, not restored (`WorldSaveData.cs:300`). A resumed run is therefore
  not a continuation of the unbroken decade. Both arms resume from the same save, and the old decade control
  is not their control. The shared 1960-64 history narrows the noise floor.
- The save must already hold what the treatment reads (e.g. a wealth stock accumulated from 1960, so the save is
  cut from an observe run). A change that acts from 1960 needs a save made with it on.
- Accumulated telemetry (year-end recaps, rollups) starts at the resume; score only the resumed years.
- Before trusting it: two resumes from one save produce byte-identical CSVs.

**Both player and AI breakups land in this branch** (author decision, 2026-10-06). There is **one**
churn implementation with a scope switch — not a player system plus an AI system. Phase 3 is a
development checkpoint, not a shipping state: running the code on the player's roster first means the
Band Room can be playtested and iterated without paying for a decade run on every change. Phase 4
widens the scope to the whole world in the same branch. **The branch merges only once Phase 4's
decade A/B passes**, so the shipped game never has a player who loses members while the AI doesn't.

If the Phase 4 A/B fails, Phase 3's player-only scope is the fallback that can still merge. That's a
decision for the author at that point, never a default.

**Ordering.** The author's first ask is that characters have lives, so Phases 0–3 come first and need
no calibration work. The world changes (4, 4c, 5, 7) each get their own decade A/B and are bundled
only after each is clean alone (the evolution-bundle precedent). Phases 4 and 4c must not share an A/B:
churn and team craft both move hooks and lineups, and a joint run can't attribute a miss to either one.

---

## 7. Measures and kill criteria

**Before trusting Phase 4:**

1. **Churn by success tier.** Share of acts losing a member in the decade, split charted vs never
   charted. **Kill** if churn is concentrated in successful acts while failing acts mostly split
   dramatically. Failure should mostly look like quiet dissolution — the sketch's own test.
2. **Cause mix**, measured against the reference set (§2.17), not the sketch's prior. **Kill** if any
   one cause exceeds 45%. That is the saturating-input signature, and CreditAndMoney is the first
   suspect if co-writing is mis-sized.
3. **Cascades.** Share of departures that come within a year of another departure from the same act.
   **Kill** at more than twice the reference set, or if the breaker binds in more than 2 of the 10
   years.
4. **Population conservation.** Active act count and formations per year stay inside the control's
   noise floor. Spin-outs and recombinations replace fresh formations; they never add to them.
5. **Calibration.** Genre-share error, chart-slot error, owner-Major share and album unit share stay
   inside the two-seed noise floor. Read the per-genre ledger, not just the aggregate
   ([[one-conversion-reshuffles-the-rng-stream]]).
5a. **Draft reach.** The share of acts losing a member to service, by year, follows the shape of the
    induction curve and matches the reference set's `Service` share within its sampling error.
    **Kill** on a flat profile: if 1962 looks like 1967, the series isn't being read.
5b. **Deaths.** Count deaths among charting people by year and channel against the reference set.
    **Kill** if early-decade deaths (1960–65) run above the reference, or if substance deaths appear
    before 1966 in more than a trace. **Report, don't tune by age:** the age distribution of
    substance deaths. It should peak in the mid-twenties without any age term asking it to.

**Before trusting Phase 4c:**

5c. **Mean hook and quality** of records by year stay inside the noise floor. Teams change *which*
    songs are strong, not how strong songs are on average.
5d. **Credit shape.** The share of originals with two or more credited writers rises through the
    decade, and pacts stay a minority of writing pairs. Cut-ins are concentrated in `Shark`-managed
    and label-published acts.

**Before trusting Phase 5:**

6. **Mean base quality** of active acts, by year, stays inside the noise floor. A drift kills the
   phase.
7. **The 1969 spread.** The spread of `instrumentalSkill` across active acts is wider in 1969 than
   in 1961, and its mean is not higher.
8. **Hamburg exists, and is rare.** Acts whose skill rose by .25 or more over the decade are a tail of
   a few percent, concentrated in club-genre families and young formations.

**Across all phases:**

9. **Determinism.** Two runs on the same seed are byte-identical. Any population-RNG draw in new code
   is a defect.
10. **Runtime.** A decade run takes within 3% of the control's time. Everything runs at the year
    boundary or at existing seams.

---

## 8. Determinism, cost, saves

- **RNG.** No new code draws `GD.Rand*` or the population stream. Keyed draws use
  `RepertoireTaxonomy.Unit($"{RequestedSeed}|band-life|{event}|{personId}|{year}")`, or an FNV hash
  over the same key in `ArtistEvolution.RngNamespace`. The only global-stream change in the whole
  directive is the *range* of `CalculateRecordQuality`'s existing draws (§2.9). The count stays the
  same.
- **Cost.**
  - About 22.5k acts × ~3.5 members ≈ 80k people, each gaining about ten floats plus a sparse
    partner.
  - Edges exist only for acts with something going on.
  - The annual pass covers group acts × at most 10 pairs each.
  - The pool holds only people with a career to continue.
- **Saves.** The pool, alumni records, edges, partners and new person fields are serialized. Old
  saves load with defaults: `formationSkill = technicalSkill`, no edges, an empty pool. Verify with
  `SaveLoadRoundTripRunner` and `--inspect-slot`.

---

## 9. Author decisions (resolved 2026-10-06)

1. **Death: on, within realistic bounds.** Rare early in the decade; the substance channel takes over
   toward the end. The 27 Club falls out of the inputs and is never hardcoded (§4.8.2). There is no
   off switch for the game. The `observe` and scope flags exist only so it can be measured.
2. **Romance: gossip-column register, off-screen.** The model is gender-neutral. In-band couples come
   in with Phase 3.
3. **The draft: it follows the historical induction curve,** scaled by one exposure factor to how much
   it actually touched acts. Deliberately shallow (§4.8.1). Phase 0 still checks whether the region
   model has non-US acts.
4. **Player and AI breakups both land in this branch** (§6). Phase 3 is a development checkpoint, and
   the merge waits on Phase 4's A/B.
5. **Co-writing is in scope** as four layers (§4.14). The credit layer is inert; team craft,
   professional teams and cut-ins get their own A/B (Phase 4c); the writing-session and cut-in verbs
   ship with the Band Room.

Still open: in-band couples in all-male or all-female lineups. The model allows any pairing. The
open question is how the period-register prose should handle a relationship most acts of the era
would have kept from the press. Recommendation: allow it, have it stay secret by default (§4.9's
discovery mechanic), and let the press beat read as rumor rather than announcement.

---

## 10. Worked example — the player's view

**1961.** The player signs four 19-year-olds from a Cleveland club. Execution reads poorly. Potential
reads *wide*: they're reaching for material they can't play yet, and there are two originals in the
set. A rival signs a finished club band and charts within a month.

**1962.** The player books them a summer residency instead of a session. On a revisit the Ear reads
them "tighter than in March". The bassist hasn't improved. In the Band Room the singer says, "Danny's
my buddy. He's also holding us back." The player hires a session bassist for the next single. It
charts, and Danny takes Outsider strain.

**1963.** The player books the singer and the guitarist into writing sessions together. One is all
melody, the other all lyric, and the team's songs are better than either writer's alone. After the
first hit the guitarist comes in: "Four of the six sides are mine. My name's on one." The player can
cut the label in on the publishing for the money, or concede a pact. The player concedes. Credit
strain falls, rivalry stays high, and the records get better.

**1965.** At a session, the player's Street instinct notices that the drummer's girlfriend has been
riding in the singer's car. Tell the drummer, sit on it, or keep the two apart on the next tour?

**1966.** The draft letter comes for the bassist, now 22. The player hires a replacement from the
pool — a session man from a dissolved Detroit act — rather than putting the band on ice in its best
year.
**1967.** The singer now holds two-thirds of the act's name recognition. The player can spotlight
him — rename the act — and exercise the leaving-member option when he walks. Or the player can try to
hold the band together and watch the Brewing stage harden into an ultimatum.

**1969.** Street has been flagging the guitarist for two years, and the roster card shows it. The
player can pull the tour and dry him out, at a cost in momentum, or keep the act on the road through
its biggest summer.

Every line above reads a number the simulation already holds, or will hold once its phase lands.

---

## 11. Status — Phase 4 A/B, 2026-10-08

**Runs.** Controls `bms2-obs-{1001,2002}` (observe scope is economy-identical, so it is the control; stopped mid-1969,
scored on 1960-67). World `bms2-world-{1001,2002}` on the same build plus the fitted constants. Compare with
`py SimTools/band_life_ab.py <control> <world> --max-year=1967`; lineup measures with `SimTools/analyze_band_life.py`.
Strain weights were fitted offline from the control's pair log with `SimTools/fit_band_life_strain.py`.

**Calibration (§7.5) — passes.** World minus same-seed control, against a seed-to-seed spread of 44.6 / 26:
genre-share sumAbsErr −4.0 / −7.5; year-end slot error −34 / +12; album unit share within ±0.8 pt; owner-Major
entry share ±4 pt, mixed sign.

**Lineup measures.** Failing acts dissolve quietly 91-92% (§7.1 ok). Largest cause Direction 25-26% (§7.2 ok).
Cascades 8-10%, breaker never binds (§7.3 ok). Charting acts losing a member to service 2.2-2.6% vs reference 2.8%,
peaking 1966-67 (§7.5a ok). Charting deaths 1960-65: 7 and 3 vs reference 7 (§7.5b ok). Charting groups that
changed lineup: 34-38% vs the reference's 63% (selection-biased toward famous long careers).

**Open items from the first A/B, and how they closed.**
1. §7.4 population conservation **missed**. Active acts ended 2.5-3.3% below control by 1967-68 (seed spread
   ~1%), because ~190 band-life dissolutions a year went unrefilled. **Author decision (2026-10-08): refill.**
   `EndAct` credits every world-scope dissolution to the formation servo (`BandLifeService.ReleaseFormationCredit`).
   The refills are spread over the weeks left in the year and sit outside the servo's calendar quota. The servo
   sat at its 2,200 floor every year in every run (supply already exceeds label demand), so nothing counteracts
   them. Exhaustion is left alone; §4.16 fixes it as a level, not a cap.
2. The substance channel produced no deaths; a habit draw was added (now measured below).
3. Solo spin-outs are rare (4-8 per decade). Cause measured: the member's slice of act fame, not the gate (§4.15
   "What it is not"; Phase 4d).

**Refill A/B (`bms3-refill-{1001,2002}` vs `bms2-obs-*`, 1960-67).** Note: the obs controls never wrote a 1968
genre-shape row, so `--max-year=1968` silently adds the world's 1968 error against nothing (it read as +32 / +34;
the earlier bms2 world run's "+43" at 1968 was the same artifact). Score on 1960-67.

| Measure | 1001 | 2002 |
|---|---|---|
| Active acts vs control, 1967 / 1968 | +0.7% / +0.8% | +0.7% / +1.3% |
| Formations 1968 | 2,406 (+9.4%) | 2,393 (+8.8%) |
| Genre-share sumAbsErr Δ | −12.6 | −8.8 |
| Year-end slot error Δ | −30 | +14 |
| Album unit share | within ±0.5 pt | within ±0.5 pt |
| Owner-Major entry share | ±5 pt, mixed sign | ±5 pt, mixed sign |
| Quiet-dissolution share of failing acts (§7.1) | 91% | 91% |
| Largest cause (§7.2) | 27.0% | 28.3% |
| Cascades / breaker binds (§7.3) | 9% / 0 yrs | 9% / 0 yrs |
| Charting deaths 1960-65 (§7.5b, ref 7) | 8 | 6 |
| Substance deaths through 1968 (all) | 2, ages 25 and 35 | 5, median 28 |

Population is conserved: the act count now runs slightly above control, and groups run ~0.8% below, because
refills follow the runtime type mix. The ~9% formation rise moved no calibration measure outside the seed spread
(44.6 / 26); both seeds improved share error, with Soul the largest mover. Substance deaths now exist and start
in 1967, below the Monte Carlo's ~15 per seed through 1969. The 27-Club report has n=7 so far: too few to read.

World-scope churn stays command-line only until the merge A/B. Scene defaults turn on co-writing, member axes and
roster-scope churn.

## 12. Status — Phase 4d (member fame slice), 2026-10-08

The first real use of the resume (§6 "Late-decade windows"). Two 1965 worlds were cut from 1960-start runs with the
refill flags (`--enable-cowriting --enable-member-axes --observe-band-life --enable-lineup-churn=world` plus the
canonical pair), saved as `SimLogs/worlds/bms4-w65-{1001,2002}`. Each seed then ran two 261-week windows to 1970:
`bms4d-ctl-*` (default slice .45) and `bms4d-fame90-*` (`--member-fame-share=0.90`). Measures:
`py SimTools/spinout_measures.py <run>` and `py SimTools/band_life_ab.py <ctl> <treatment> --max-year=1969`.

**What changed in code.**
- `MusicianRecognitionService.MemberFameShare` is the slice that becomes `personalRecognition`. It is separate from
  `MemberShare`, which still feeds `liveReputation` and `creativeReputation`. `creativeReputation` reaches every
  launch through `EffectiveRecognition`, so raising it would move the calibrated economy through a second channel.
  The fame slice reaches only the solo gate, a spin-out's launch stock and the substance hazard's fame term. At the
  default it is byte-identical (same expression, same floats).
- A resume under a different slice from the one the world was saved with rescales every member's fame once by
  new/old (`CHART_AUDIT_MEMBER_FAME_RESCALED`). Deposits are proportional to the slice, decay is multiplicative, and
  the stocks sit far below saturation (best member ~.25), so this reproduces a 1960-start treatment to first order.
  The window then measures the treatment itself, not a ramp toward it.
- **Bug fixed:** world churn keeps an act on ice when the draft takes its only or last member
  (`WouldDissolve` returns false for `Service`). A label could still pick that act for a release, and
  `GenerateRecordFromArtist` threw on `members.Max` (`bms4d-ctl-2002` died in Feb 1968). `TryReleaseRecord` now
  skips an act with no members. It never fires without churn, and the three windows that finished on the old build
  never reached the throw, so they are unchanged by it. The control was re-run on the fixed build.

**Result (1965-69 windows, treatment vs same-world control).**

| Measure | 1001 ctl / .90 | 2002 ctl / .90 |
|---|---|---|
| Members ≥ .05 launch bar at end | 186 / 561 | 171 / 528 |
| Best member fame | .244 / .468 | .254 / .457 |
| Solo spin-outs | 1 / 1 | 1 / 2 |
| Spin-out acts that charted | 0 / 0 | 0 / 0 |
| Substance onsets / deaths | 750 / 2 → 745 / 2 | 713 / 4 → 712 / 3 |
| Genre-share sumAbsErr Δ | −5.7 | +1.2 |
| Year-end slot error Δ | +8 | 0 |
| Album unit share | within ±0.2 pt | within ±0.3 pt |
| Owner-Major entry share | ±4.7 pt, mixed sign | ±3.4 pt, mixed sign |
| Active acts, 1968 | +0.2% | 0.0% |

Reference: 16 of the reference set's 91 top-40 groups (17.6%) lost a member to a solo career. The model's rate is
0.1-0.2% of charting groups, before and after. Every spin-out in every run (bms3 included, 15 in all) came through
CreditAndMoney, and **none of the solo acts has ever charted**.

**Why: the intent gate binds.** Replaying the gate over the `bms2-obs-1001` pair log (lead singers in charting acts
who clear the bar at a doubled slice and hold half the spotlight, n=336), `WouldConsiderSoloCareer` passes 2.1%.
The urge (`ambition·.4 + ego·.3 + stagePresence·.2`) averages .42, and the ties (`loyalty·.5 + yearsInGroup·.05 + .3`)
average .78. The years-in-group term alone is worth .18 at the mean tenure of 3.7 years, and it runs the wrong way:
history's spin-outs came after years of success (Ruffin, Medley, Sebastian, Durham), and here every year in the
group makes a member less likely to leave. Dropping it lifts intent to 13%. The `groupHits > 5` bonus (+.2) is
out of reach for nearly every act ([[careers-are-two-records-long]]).

**Decision for the author.** The slice stays at .45 by default: alone it buys nothing measurable and costs nothing
measurable. It is the *second* term, though: with the intent fixed, a doubled slice would put 100 acts past bar and
spotlight per decade against 26 at .45 (offline, `bms2-obs-1001`). The proposed next change is the intent itself,
bundled with the slice in one A/B from the same two 1965 worlds:
- tenure raises the urge after success instead of only raising the ties (the act's top-40 hits or recognition,
  not `> 5` hits);
- the success-blind constant .3 is re-sized offline against the reference set's 17.6%, read on top-40 groups.

Separately, spin-outs that never chart are a launch question (the solo act starts Unsigned with half the act's
reputation and no label): the leaving-member option fired 4 times in four windows.

## 13. Status — Phase 4d2 (solo intent), 2026-10-08

**Author decision (2026-10-08): rework the intent; one post-1965 window, no paired seed.** Paired seeds come back
with the next A/B in this directive or the one after it.

**What changed in code.**
- `Musician.WouldConsiderSoloCareer`: the urge is `ambition·.4 + ego·.3 + stagePresence·.2 + successPull`, where
  `successPull = .05 × min(yearsInGroup + 1, 6) × min(1, actTop40Hits / 2)`. The ties are `loyalty·.5 + .15`.
  Tenure is gone from the ties, and the `hits > 5` bonus is gone. A year in a group that has had hits now pushes
  toward leaving, up to +.30. A year in a group without hits pushes neither way. This is the only change to the
  sim. It also reaches the Band Room's solo-single request (`PlayerDesk.BandRoom`), which reads `IsSoloViable`.
- Sized offline with `py SimTools/fit_solo_intent.py` over the `bms2-obs-1001` pair log at a doubled slice. The
  91 model groups with 5+ top-40 hits match the reference set's size. 18.7% of them ever hold a member past the
  fame bar and the spotlight test. The old intent let 2.2% through; the new one lets 17.6% through.
- Telemetry: the `solo-spinout` event never set a cause, so it always read as the enum default, CreditAndMoney.
  It now carries the departure's cause. **This corrects §12's claim that every spin-out came through
  CreditAndMoney**: by the matching departure rows, the old runs' spin-outs were mostly Spotlight too.
- `SimTools/spinout_measures.py` read chart success from `first-chart-events`, which records a *label's* first
  chart entry, not a record's. It now takes each spin-out act's peak from `records.csv`. **This retracts §12's
  claim that no spin-out has ever charted**: the claim came from the wrong table. The old runs' spin-outs (2 and 3
  in the bms4d windows) did not chart under the corrected measure either.
- `--save-world-at-year` takes a list (`1966,1967,1968,1969`). Each year is saved as `<save-world or run>-<year>`.

**Run.** `bms4e-intent90-1001` resumed `bms4-w65-1001` with the canonical pair, the refill flags, and
`--member-fame-share=0.90`. It ran 261 weeks and is scored against `bms4d-ctl-1001` (.45, old intent) and
`bms4d-fame90-1001` (.90, old intent). The window holds five band-life passes, 1964-68: a resume runs the 1964
pass first (§6), and the 1969 pass falls in 1970, outside the window. Worlds were saved along the way as
`SimLogs/worlds/bms4e-intent90-1001-{1966,1967,1968,1969}`. They carry this treatment's departures, so they are
starting points for work that builds on the new intent. They are not clean controls for it; `bms4-w65-*` still
are.

| Measure | ctl (.45, old) | fame90 (.90, old) | intent90 (.90, new) |
|---|---|---|---|
| Solo spin-outs, passes 1964-68 | 2 | 3 | 26 |
| Groups with 5+ top-40 hits that lost a member solo | 2 of 64 (3.1%) | — | 13 of 67 (19.4%) |
| Groups with 3+ hits / 1+ hit | 1.1% / 0.2% | — | 7.2% / 1.8% |
| Spin-out cause (from the departure row) | Spotlight 2 | Spotlight 2, Credit 1 | Spotlight 18, Direction 4, Credit 2, Burnout 2 |
| Spin-out acts that charted (top 40) | 0 of 2 | 0 of 3 | 12 of 26 (3); peaks 2, 20, 38, 43, 52, ... |
| Spin-outs signed (by first signing tier) | — | — | 24 signings: Indep 9, MidTier 7, Major 6, Boutique 2 |
| Leaving-member options exercised | 1 | 1 | 8 |

Calibration, intent90 minus ctl, 1965-69: genre-share sumAbsErr −3.6 (Soul +6.5 is the largest per-genre
move); year-end slot error +4; album unit share within ±0.2 pt; owner-Major entry share +0.1 to +4.0 pt, all
the same sign. That last one is inside the ±4.7 seen for the slice alone, though one-sided this time. Active acts
1965-68 are within 0.2% of control; groups are 0.8% below by 1968, the spin-outs having moved members into solo
acts. All of these are inside the seed spread (§11: 44.6 / 26 on the first two). Substance deaths: 1 (control 2).

**Reading.** On success-matched groups the rate lands on the reference: 19.4% against 17.6%, with n=13. The
caveats are a single seed, a five-pass window against careers in the reference set, and a sizing that was fitted
on the same seed. The causes look like history: the spotlight carries most spin-outs, and the famous member
walks. About half the spin-outs chart, and one reached #2.

**Open for the author.**
1. **The slice default. Resolved (author, 2026-10-08): flipped to .90.** `DefaultMemberFameShare = 0.90`.
   Saved audit worlds now store their slice (`AuditWorldFile.MemberFameShare`). A world without it predates the
   flip, and resumes as `LegacyMemberFameShare` (.45) unless its flags say otherwise. Smoke-tested: a flagless resume
   of `bms4-w65-1001` printed `MEMBER_FAME_RESCALED saved=0.45 now=0.9`. The bms4e worlds carry the flag explicitly.
2. **The next paired A/B** should run intent + .90 on both 1965 worlds against `bms4d-ctl-*`, and seed 2002
   especially, since the sizing was fitted on seed 1001.

## 14. Status — Phases 4e, 4f, 4c, 5, 5b, 6, 7 (built, on by default), 2026-10-08

**Author decisions (2026-10-08).** World churn is on by default. The remaining phases ship in this branch without a
decade run; later-decade snapshot windows are the check that nothing breaks unduly. `main` had no content this
branch lacks (the five PR merges are bookkeeping over commits already in its history), so nothing was merged.

**Defaults and flags.** `chart_manager.tscn` turns on `worldLineupChurnEnabled` and every phase below. Each phase has
`--enable-X` / `--disable-X` (`member-wealth`, `road-fatigue`, `team-writing`, `musician-growth`, `polar-member-axes`,
`member-identity`). `--observe-member-wealth` and `--shadow-musician-growth` keep a stock without readers.
`--log-band-life-members` writes one row per person per act-year (`<run>-lineup-members.csv`).

**What was built.**
- **4e wealth** (`MemberWealthService`). A stock per person: the act's performer royalty for the year, split evenly
  (sidemen get a wage and the leader keeps the rest), plus writer mechanicals (share-weighted charted units x 1¢, from
  a new `shareUnits` on the person ledger), less spending of 18-70% a year by ego and fame. Old worlds seed 35% of
  lifetime income. There are four readers on `Norm = 1 − exp(−wealth/$8k)`:
  1. Independence: +.12·Norm to the solo urge; the act's success absorbs less of a rich member's strain; an exhausted
     member with Norm ≥ .45 goes studio-only instead of quitting.
  2. The money grievance: the wealth gap enters CreditAndMoney.
  3. Lasting fame: a floor of at most .02 on member fame.
  4. Substance exposure, plus the Band Room's advance and off-the-road asks.
- **4f fatigue.** `fatigue ← fatigue·(1 − recovery·(1 − load)) + load`, with `recovery = .8/(1 + .08·roadYears)·(1 +
  success)`. The exhaustion hazard reads it at rate .018 (×1.5, since fatigue sits below road years for anyone who
  rests). Old worlds seed fatigue at half their road years. `roadYears` stays the lifetime exposure.
- **4c team writing** (`TeamWritingService`). Member crafts: melody = creativity×versatility, lyric =
  creativity×diction, hook = creativity×stagePresence. A team scores the max of each craft, less friction that grows
  with similarity, plus a small bonus for rivalry. The shift over the lead writer's own craft applies at weight .2;
  a lone writer is unchanged. Staff writers pair into standing teams by keyed draws, preferring complementary crafts,
  and 60% of their catalogue becomes team songs, one for one. An older world forms its teams once, on load. AI cut-ins:
  a Shark-managed act whose label owns its publishing loses 25% of the credit on 35% of its originals, and its writers
  carry CreditAndMoney and Outsider strain. `PickTeam` gained a side-effect-free peek.
- **5 growth** (`MemberGrowthService`).
  - Hours come from the act's state: club rooms 900 a year at residency weight, road 900×load at .65, sessions 40 per
    release at .45, rehearsal 150 at .35. Plasticity falls from 1 at 20 to .3 at 35.
  - Each axis is `formation + (ceiling − formation)(1 − e^(−hours/3000))`, less decline counted from the age at the
    growth origin. A 45-year-old's voice is not aged again by switching growth on.
  - **Population centring:** each pass subtracts the population's mean shift, so the young grow against the field
    while the field's mean holds. Unchecked, the shadow's mean rose +.0055 then +.011 a year.
  - Live writes wait for the end of the pass.
  - Personality drifts within ±.2 of the generated values: ego with fame, loyalty with tenure, temperament with road
    years, reliability with substance.
- **5b.** `PolarActProfileDeriver` reads instrumental skill for Musicianship and the lead's vocal power, control and
  diction for the vocal axes.
- **6 scouting the rough** (`ScoutingRough`, player only). The tells are median age, the heard set's capability
  deficit (`PolarMaterialFit`), the originals share, a blurred ambition read, and a revisit 90+ days after the first
  note (the improvement delta in the label's execution read). The potential read (wide / some room / narrow) is built
  from the tells only. The card shows Execution / Identity / Potential lines; the notebook saves the first note.
- **7 identity on people** (`MemberIdentityService`).
  - People are drawn at the act's genre prior, ±.06 keyed spread inside the band, plus the trait adjustments the
    deriver used to apply act-wide.
  - Drift: 8% a year toward the prior of what the act now plays, and a married partner (more with children) pulls
    toward maturity and sincerity.
  - The act's Polar identity axes are the voice-weighted mean of its members. The project-history blend and the
    ratifier still apply.
  - The identity spread adds up to .06 reach.
  - Direction is half the old proxy and half `.211·(distance·(.5 + maxCreativity)/.1536)^1.58`. A linear term
    matched the proxy's mean but not its tail (q90/q99 .35/.48 against .46/.75) and cut Direction's share 22% → 13%;
    the power matches median, tail and mean (fitted on one-week pair-log probes of the 1968 world).

**Runs.** These are 1968-69 windows from `bms4e-intent90-1001-1968`, at slice .90, with world churn.
- `bms5-obs-1001` is the sizing run (wealth stock and growth shadow only), sized with
  `py SimTools/size_band_life_members.py`.
- `bms5-off-1001` is the control: every phase `--disable`d.
- `bms5-on-1001` is the first treatment, on defaults.
- `bms5-on2-1001` is the treatment after the two fixes.
- Measures: `py SimTools/phase_bundle_measures.py <ctl> <trt>` and `band_life_ab.py`.

**Inertness.** `bms5-off` against `bms5-obs`: every economy CSV is identical on every row both runs wrote. The obs run
stopped one week short of the 1969 year-end, so 13 annual files differ only by their missing 1969 rows. The world
save round-trips byte-identically with every new field (`--band-life-check`, which now includes Phase 4c-7
invariants), and `SaveLoadRoundTripRunner` passes over a band-life pass.

| Measure (1968-69 window) | control `bms5-off` | `bms5-on2` |
|---|---|---|
| Genre-share sumAbsErr / year-end slot error | 90.3 / 1003 | 85.3 (−5.0) / 997 (−6) |
| Album unit share 1968 / 1969 | 51.87 / 55.41 | 51.88 / 55.41 |
| Owner-Major entry share 1968 / 1969 | 38.2 / 42.0 | 38.3 / 39.4 |
| Active acts 1968 | 21,000 | 20,992 |
| Release hook (all / originals / professional) | .6267 / .4916 / .8151 | .6285 / .4936 / .8218 |
| Exhaustion exits 1967 / 1968 | 343 / 357 | 171 / 169 |
| Dissolutions 1967 / 1968 | 222 / 224 | 170 / 176 |
| Studio-only exits (share from charted acts) | 403 (30%) | 293 (36%) |
| Cause mix: Credit / Spotlight / Direction / Reliability / Burnout | 12 / 7 / 22 / 39 / 16% | 15 / 8 / 16 / 38 / 19% |
| Mean member technical skill 1968 | .4535 | .4534 |
| Charting groups' wealth, writers vs others (median / q90) | — | $596 / $4,596 vs $602 / $3,736 |
| Spin-outs (charted) | 14 (2) | 11 (4) |

The first treatment (`bms5-on`) raised the release hook 1.3% and cut Direction to 13%; `TeamCraftWeight` .5 → .2 and
the Direction power fixed both. Reliability is the largest cause in **both** arms (38-39% by 1967-68, with the
substance era); that predates this bundle. (Correction, §15: §7.2's kill line is 45%, not 30%, and on charted groups
the share is 24%.)

**Open.**
1. ~~Reliability at 38% of late-decade departures, in control as well as treatment.~~ Investigated in §15.
2. The scouting card was checked logically (`--band-life-check` prints real cards), not by a screenshot.
3. Wealth starts at a 35% seed in resumed worlds. A 1960-start run is where the writer/non-writer gap will show at
   full size (writer income is only 1.8% of income in a resumed 1968 window, because the share-weighted ledger starts
   at the resume).
4. The paired-seed check of the whole bundle is still owed, at the author's discretion.

## 15. Reliability at 38% late in the decade — investigation, 2026-10-08

No new runs. Everything here comes from data already on disk: the lineup logs of `bms3-refill-*`, `bms4d-ctl-*`,
`bms4e-intent90-1001` and `bms5-{off,on2}-1001`, the `bms5` member snapshots, the `bms2-obs-1001` pair log, and
`Data/LineupReferenceSet.csv`.

**The line.** §7.2 kills at 45% for any single cause. The 30% in §2.17 is the sketch's unsourced prior, not a limit.
§14 said Reliability was "over §7.2's line"; it was not. Still, it was rising: 1968 alone ran 41-44% (`bms4d-ctl-1001`
44%), and no run has scored a 1969 year-end pass yet.

**1. It is substance, through the firing channel.** In 1967-68 (`bms5-off` / `on2`):
- 71-74% of Reliability departures fire a member who is using (`substanceLoad` ≥ .05 that year).
- About 1% of group members are using, and each one is fired at **13-15% a year**. Non-users leave over
  Reliability at 0.05-0.07% a year, roughly 250 times less often.
- The term behind it is `SubstanceReliabilityWeight · max(load)`. It puts .95 × ~.30 on **every** edge the user
  is on, and unlike the trait term it carries no temperament factor. The other members all become complainers, so
  the user is the one who leaves (`Fired`).
- Drop the using-leaver exits and the pooled Reliability share falls to 14-15%. That is an upper bound, since some
  of that strain would leave under another cause.

**2. The 38% is mostly never-charted acts.** By period, for group strain departures:

| | 1960-64 | 1965-66 | 1967-69 |
|---|---|---|---|
| Charted groups, model (all runs) | 5-9% | 12-17% | 18-24% |
| Never-charted, model | 40-48% | 47-53% | 50-53% |
| Reference, Reliability first changes | 0 / 20 | 0 / 15 | 4 / 22 (18%); 4 / 14 strain-coded (29%) |

- In never-charted acts Reliability has led all decade, including 1960-64, when substance was near zero. Spotlight
  (× fame) and CreditAndMoney (× money) are close to 0 for unknown acts, so Reliability and Direction win by default.
- Never-charted acts are 55% of late departures, so they set the pooled number.
- The reference covers only top-40 groups. On those the model sits at 18-24% late, inside the reference.
- The reference's late changes are drug-heavy as well: 6 of the 14 strain-coded 1967-69 first changes are
  drug-linked. Four are coded Reliability (Jones, Ballard, Love, Bratton), and two were busts coded Outsider
  (Yanovsky, Palmer).
- Once using leavers are removed, charted Reliability is 5%. So nearly all of the charted late Reliability is
  substance, and that is the historical pattern.

**3. Why it overshot: the weight was fitted before the habit was resized.** `w_sub` .20 → .95 was fitted (b81ca92)
on the `bms2-obs` pair log. b9c63be then strengthened the habit draw:
- persistence went from about .42 to about .74 a year at reliability .6;
- the dose went up 25%.

The weight was not refitted afterwards. Onsets barely moved (pairs with a user: 1.85% → 1.94% in 1967), but the
load per user rose about 1.8× (.18 → .30 in 1967). An offline replay of the `bms2-obs-1001` pair log
(`fit_band_life_strain.py`, fitted constants, new `--sub-mult`) shows this:

| Substance stock / `w_sub` | Pooled 1967-68 | Charted | Never | Total departures 1960-68 |
|---|---|---|---|---|
| as fitted (×1.0) / .95 | 25% | 13% | 39% | 1,813 |
| today (×1.8) / .95 | **39%** | **25%** | **52%** | 2,025 |
| ×1.8 / .60 | 27% | 15% | 40% | 1,845 |
| ×1.8 / .45 | 22% | 11% | 34% | 1,779 |
| ×1.8 / .30 | 18% | 8% | 30% | 1,738 |

The ×1.8 / .95 row matches the live runs (39% pooled, 24% charted, 50-52% never), which validates the replay. At
the stock it was fitted on, the weight gave 25%.

**Recommendation: don't cut `w_sub` to chase the pooled number.**
- Refitting to the old effective level (about .53) would drop charted late Reliability to about 13%, below the
  reference's 18-29%.
- It would also remove about 9% of all strain departures, most of them late. That moves the refill and population
  measures (§7.4), so it would need a seed A/B.
- The charted mix is the only part with a benchmark, and it currently matches history.

What changes now (tooling only, so the economy is untouched):
- `analyze_band_life.py` §[2] prints the mix by period × charted/never-charted.
- The 45% kill now reads charted groups. The pooled max is report-only.

**Still open.**
1. Score a 1969 year-end pass. Pooled 1968 was 41-44%, and 1969 runs at `SubstanceEra` 1.0 with users still rising
   (1.0% → 1.2% of members), so the charted number may move too.
2. A design question for the author, about what the player sees. A player's early acts are never-charted, and half
   their strain exits are "Fired (unreliable)". If that reads as samey, the structural lever is not `w_sub`. Two
   options:
   - give the substance term the complainer's `(1 − temperament)` factor, as the trait term already has;
   - route some substance exits to a non-strain kind: walked out, or a bust coded Outsider, the way the reference
     codes Yanovsky and Palmer.

   Either one needs a run.
