# Directive: Artist Evolution — Career Arcs, Eras, and the Cohesive Album

## Objective

Give artists a career that *moves*. Today a Label-Man act is stamped with a genre at
formation and dies wearing it — across a full decade run, **0 of 22,513 artists ever
changed identity.** Nobody goes electric. Nobody comes back from the psychedelic
wilderness with a country record. Nobody grows up.

We want a discography a player can read like a biography: a band that starts as a
rough R&B club act, gets swept into the beat boom, makes one ambitious studio record
because they heard someone else's ambitious studio record, and — when it doesn't sell —
retreats to the blues they started with. Every turn needs four things: a **musical
path** (adjacency, not teleportation), a **motive** (pressure, ambition, failure,
influence), a **record** of that motive the UI can quote back, and a **climate** that
makes the move possible in 1966 and absurd in 1961.

The proximate simulation payoff is Folk Rock. The mechanism payoff is that every genre
that historically grew by *conversion* rather than *birth* gets the channel it is
currently missing.

**Constraint of record, from the author:** this is a large feature expansion that must
not move the economy or the genre balance much. V3.1 plus the radio branch's mix8 pass
is a calibrated instrument. This directive is written to be *redistributive at the
margin and neutral in aggregate*, and every phase carries a gate that fails loudly if
it isn't.

---

## 1. Verified starting point

Repository and measured facts. Treat as given; do not rediscover.

### 1.1 Identity is frozen, and this is structural, not incidental

- `CompetitorManager.TryReleaseRecord` (`Systems/CompetitorManager.cs:1573`) snapshots
  `artist.primaryGenre`, asks `GenreSupplyService` for a **project** genre, and — when
  `ArtistPopulationLifecycle.IsLive`, i.e. every canonical decade run — routes the
  decision through a throwaway shadow artist (`CreateProjectDecisionArtist`,
  `:1717`). The real artist's `primaryGenre` is **never written**. The legacy branch
  (`:1581-1584`) mutates and restores it within the same call.
- The record gets the project genre (`ApplyProjectGenre`, `:1872`); the artist keeps
  the formation genre forever.
- Measured on `mix8-decade`: `groupby(artistId).artistIdentity.nunique() > 1` is true
  for **0 of 22,513** artists.

### 1.2 The transition channel already exists — it just doesn't stick

`mix8-decade-supply-selections.csv`, 59,231 selections:

| mode | share |
|---|---|
| Retained | 71.4% |
| WeightedTransition | 27.2% |
| AnnualFloor | 1.4% |

Non-retained share rises 23.5% (1960) → 32.5% (1967). **A quarter to a third of all
projects are already off-identity.** The sim is constantly having artists cut records
outside their lane and then forgetting it happened by Monday.

### 1.3 The Folk Rock case, measured

Who supplies Folk Rock projects, 1965–69:

| artist identity | projects |
|---|---|
| **FolkRock** | **538** |
| Country | 51 |
| RockAndRoll | 33 |
| Folk | 33 |
| ContemporaryFolk | 28 |

Folk Rock is populated almost entirely by acts **born** Folk Rock from 1965 onward. It
is historically backwards: Dylan, the Byrds, the Turtles, Sonny & Cher, the Mamas and
the Papas were folk acts who plugged in. In our sim, the folk acts are right there and
they don't move:

| year | releasing artists | Folk | ContemporaryFolk | FolkRock |
|---|---|---|---|---|
| 1964 | 4,038 | 210 | 105 | 0 |
| 1965 | 4,453 | 199 | 141 | 26 |
| 1966 | 4,702 | 191 | 124 | 86 |
| 1967 | 4,944 | 109 | 141 | 131 |

340 folk-family acts in 1965 against 26 folk-rock acts. And `Folk` carries
`DeathYear = 1966`, so from 1967 those acts can take **no new supply in their own
identity** — they don't convert, they just thin out (191 → 109). The historical acts
didn't retire. They bought a Rickenbacker.

Where do Folk acts' off-identity projects actually go? Across the decade:
`Country 113`, `Folk 74`, `ContemporaryFolk 48`, **`FolkRock 33`**, `Soul 23`,
`TraditionalPop 23`. Country outdraws Folk Rock 3.4:1 as a destination for folk
artists — a direct artifact of `FormationAffinity(Country) = 2.2` leaking from
formation into project selection (both route through `GetSupplyWeight`). Noted as an
interaction risk in §7.

### 1.4 The selection machinery we must not fight

- `GenreSupplyService.ChooseGenreWithSelection` is the single authority on project
  genre. Weight = `demand × artistFit × labelFit × lifecycle × concentrationBrake ×
  globalConcentrationBrake × britishBridge × FormationAffinity`.
- `GetIdentityFit` (`:302`) is the artist-side term: **4.0** primary, **2.25**
  secondary, **1.45** same-family, **0.55** otherwise. This is the knob that decides how
  hard an act is pinned to its lane.
- `GetProjectIdentityRetention` (`:255`) is computed **from the identity genre's own
  baseline curve**. This matters enormously: migrating a Folk act to FolkRock in 1966
  moves retention from ≈0.55 (Folk: baseline .27 against a .44 peak, negative slope,
  death year reached) to ≈0.82 (FolkRock: baseline .78, at its own peak, capped at
  .88). **Identity migration is a positive feedback loop.** One ratified conversion is
  durable. This is why the caps in §7 are not optional.
- The supply roll is a **deterministic FNV hash** of
  `labelId|artistId|year|week|sequence` (`GetDeterministicSupplyRoll`, `:1779`), not a
  draw from the global stream. Genre selection consumes no RNG by design.
- `GenreMarketMomentumService.GetAdjacency` is a pure lookup over an explicit edge
  table plus a `.12` same-family floor. It is live and already used by
  `IsPsychedelicTransitionCompatible` and by `ChooseRuntimeSecondaryGenre`. The rest of
  that service (shocks, donors, zeitgeist deltas) remains unwired — **not this
  directive's problem, do not wire it here.**

### 1.5 The cohesive album already exists in the model

- `Album.thematicCohesion` is real, generated in `CompetitorManager.GenerateAlbum`
  (`:2978`) from `AlbumModel.GetMaximumAchievableCohesion(year, artistTalent,
  labelProduction, luckyRoll)`.
- That ceiling is a **purely exogenous era ramp**, `SmoothStep(0.12 → 0.96)` across
  `CohesionRiseStartYear = 1964` → `CohesionRiseEndYear = 1968`, plus a deliberately
  vanishing `pioneer` path from 1965 (top talent × top room × best ~6% of rolls) that
  exists solely so Rubber Soul and Pet Sounds are reachable before the ramp arrives.
- `AlbumFormat.Concept` is minted when `statementViable && year >= 1965 && roll < .24`.
- Cohesion is **load-bearing on money**:
  `thematicCohesion → CalculatePooledAppeal → album.pooledAppeal → record.hookStrength /
  productionQuality / danceability → units`. Also `CalculatePooledAppeal` lerps from
  peak-weighted to whole-weighted by `GetAlbumEraWeight`. Raising cohesion globally
  raises album quality globally, which is an economy change. §5.4 is gated accordingly.

### 1.6 Fields the sketch assumed that do not work

- **`SimulatedArtist.criticalAcclaim` is dead.** Declared at `Data/SimulatedArtist.cs:87`
  and referenced **nowhere else in the codebase** — never written, never read. It is
  always `0f`. Any pressure formula that multiplies by it is multiplying by zero.
  Either give it a real writer (§5.3) or don't use it.
- **`careerEvents` is an unbounded `List<string>`** on every artist in a 22.5k registry,
  appended by ~10 call sites and read by exactly one UI panel. It is a narration buffer,
  not a data structure. Era records go in a typed list; only *milestones* get a string.
- `ArtistPublicProfile` already carries `reputationTags`, and `ReputationTag` already
  defines `Experimental`, `Innovator`, `GenreBending`, `Authentic`, `Traditional`,
  `Difficult`, `Derivative`, `Trendsetter`. Phase 4 spends this enum; it does not extend it.
- `Musician` has exactly the traits the sketch wants: `creativity`, `musicalVersatility`,
  `ego`, `ambition`, `loyalty`, `temperament`, `reliability`, `studioEfficiency`, plus
  `isPrimaryWriter` / `isBandLeader` / `GetDramaRisk()` / `WouldConsiderSoloCareer(...)`.
  Personality generation is free; it's already sitting in the band.

---

## 2. The design inversion (read this before writing code)

The draft sketch has evolution **pick a new genre**:

```csharp
var chosen = WeightedPick(candidates);
ApplyStyleEvolution(artist, chosen.Genre, ...);   // ← writes primaryGenre directly
```

**Reject this.** It stands up a second, uncalibrated genre-selection authority next to
`GenreSupplyService` and lets it write identity from outside. Identity feeds
`GetIdentityFit` (4x) and `GetProjectIdentityRetention`, both of which feed supply
weight, which is the thing the entire D7 → mix8 calibration arc was spent tuning. A
parallel picker is a direct, unbounded hit on genre makeup. It is exactly the failure
the author's constraint forbids.

**Build the inverse.** Identity is not a decision — it is a **lagging ratification of
what the artist has actually been releasing.**

> An act that has cut three folk-rock sides *is* a folk-rock act. Nobody in 1966 held a
> meeting about it.

This buys everything at once:

1. **Zero new supply authority.** `GenreSupplyService` remains the only thing that
   decides what genre a record is. Evolution never picks a genre; it reads the ones
   already picked.
2. **First-order neutrality *at the point of ratification*.** Ratifying an existing
   selection creates and destroys no releases: the act of writing identity adds nothing to
   and removes nothing from the release pipeline. The effect on genre makeup is
   **second-order**, arriving only through the identity terms (`identityFit`, `retention`)
   on *subsequent* projects.

   **Measured correction (do not restore the stronger claim).** This is *not* the same as
   "aggregate release count and label economics are untouched by construction", which is
   what this clause used to say and which is false. `AILabel.CalculateReleasePriority`
   reads `artist.primaryGenre` for genre heat, so a converted act's odds of being *picked*
   to release move with its new genre — and the pick itself draws from the global stream.
   Measured on a 255-conversion decade: total releases 202,258 → 200,824, **−0.7%**. That
   is a downstream cascade rather than a penalty on converting acts (ratification writes no
   cooldown, no career state, no contract field), but it is real, and Gate 1's economy
   clause is stated as a **tolerance** below rather than as an identity.
3. **Motive is a bias, not an override.** Pressure and personality adjust the artist-side
   term of the existing weight — softening the 4.0 primary anchor, lifting adjacent
   candidates — inside hard bounds, with a neutral setting that reproduces today's
   numbers exactly.
4. **The story writes itself from telemetry we already emit.** We know the project
   genre, the retained/transition mode, the chart outcome, and the artist's traits at
   every release. The era record is assembled from facts, not invented.

So the loop is:

```
GenreSupplyService picks project genre  (unchanged, calibrated, hash-driven)
        ↓
release resolves; outcome lands on the artist  (unchanged)
        ↓
ArtistEvolution observes: recent project genres, hits, flops, influences
        ↓
pressure crosses resistance  →  RATIFY: identity migrates to the genre they've been playing
        ↓                                close the old era, open a new one, record the motive
        ↓
identity feeds back into GetIdentityFit / retention on the NEXT project  (the drift)
```

Evolution is a **witness with a rubber stamp**, not a second composer.

---

## 3. What we are not building

**No `AlbumArtMovementService`, no `AlbumArtMilestone` object, no separate "art album"
type.** The author's call, and it's the right one: an album's importance is not a class,
it's an outcome. We already mint albums with `thematicCohesion`, `packaging`,
`pooledAppeal`, an `AlbumFormat.Concept` flag, and a full chart run. A landmark record is
simply *an album that scored high on cohesion and then succeeded in public*. The
"movement" is the **knock-on**: the exogenous cohesion ramp in `AlbumModel` becomes
partly earned by records that actually happened, and peers who were paying attention
remember it.

Concretely: we are not adding a milestone registry. We are adding **one bounded global
scalar** (`albumLegitimacy`) that the existing `GetMaximumAchievableCohesion` era term can
lean on, plus per-artist influence memory. The Rubber Soul → Pet Sounds → Sgt. Pepper
chain emerges from records raising a ceiling that lets the next record go further, or it
doesn't emerge at all and the exogenous ramp carries the decade exactly as it does today.

Also explicitly **out of scope** for this directive (Phase 5 speculation only, do not
start): solo-career splits, producer signature sounds, regional scene objects, labelmate
influence, wiring the dormant `GenreMarketMomentumService` shock/donor market.

---

## 4. Data model

New file `Data/ArtistEvolution.cs`. One new field on `SimulatedArtist`:

```csharp
public ArtistEvolutionProfile evolution;   // null until Phase 0 initializes it
```

```csharp
[Serializable]
public sealed class ArtistEvolutionProfile {
    // --- disposition, derived once from the lineup (§5.1). Stable unless membership changes.
    public float artisticAmbition;      // wants to make important records
    public float experimentalAppetite;  // tolerance for the unfamiliar
    public float commercialPragmatism;  // will chase a hit under pressure
    public float rootsAttachment;       // resistance to abandoning the original sound
    public float conceptualThinking;    // album-as-statement inclination
    public float peerSensitivity;       // reacts to other people's records
    public float volatility;            // swings hard after success or failure

    // --- mood, moves with outcomes
    public float confidence;
    public float frustration;

    // --- arc state
    public ArtistArcPhase phase;
    public Genre artisticCenter;        // == artist.primaryGenre; the ratified identity
    public Genre priorArtisticCenter;
    public int lastIdentityChangeYear;  // -1 until first migration
    public int projectsSinceIdentityChange;

    // --- the drift window: the last N project genres, oldest first. Fixed capacity.
    public Genre[] recentProjectGenres;
    public int recentProjectCount;

    public List<ArtistEraRecord> eras;          // typed, one per era, NOT careerEvents
    public List<ArtistInfluenceMemory> influences; // capacity-bounded, see §6.2
}

public enum ArtistArcPhase {
    Formative, HitSeeking, Breakthrough, Consolidation,
    Experimental, Conceptual, RootsReturn, CommercialPivot, Declining, Legacy
}

public enum ArtistEvolutionTrigger {
    None, CommercialFailure, CommercialBreakthrough, CriticalBreakthrough,
    PeerInfluence, GenreClimateShift, CohesiveAlbumMovement,
    InternalTension, LabelPressure, PersonalAmbition, BackToRoots
}

[Serializable]
public sealed class ArtistEraRecord {
    public int eraIndex;
    public int startYear, endYear;      // endYear 0 == current era
    public Genre primaryGenre, secondaryGenre;
    public ArtistArcPhase phase;
    public ArtistEvolutionTrigger trigger;
    public string summary;              // one authored line, generated at close/open
}

[Serializable]
public struct ArtistInfluenceMemory {
    public string sourceArtistId;
    public Genre sourceGenre;
    public ArtistInfluenceType type;
    public int year;
    public float strength;
}

public enum ArtistInfluenceType { HitSingle, CohesiveAlbum, GenreBreakthrough }
```

Plus one static: `ArtistEvolution` (config + master switch), and one service:
`ArtistEvolutionService` (all logic, stateless where possible).

`ReleaseCreativeIntent` from the sketch is **kept**, but as a *derived label* computed at
release time from pressure + outcome history and stored on the record's telemetry row —
not as an input that changes what gets made. It is flavor with a paper trail. Promoting
it to a causal input is a Phase 5 question.

---

## 5. Phases

Each phase is independently shippable, independently gated, and independently
revertible. **Do not start a phase before its predecessor's gate passes on a decade run.**

### Phase 0 — Inert scaffolding (no behavior change)

Build the whole data layer, wire nothing.

- Add `ArtistEvolutionProfile`, enums, era record.
- `ArtistEvolution.Enabled` master flag, default **off**, plus a
  `--enable-artist-evolution` runner flag parsed in the autoload alongside
  `--enable-genre-market-v2` / `--enable-artist-population-lifecycle`.
- `ArtistEvolutionService.Initialize(artist, year)` called from `ArtistManager` after
  `RecalculateStats()` on **both** creation paths (`MaterializeRuntimeFormation` and the
  initial-population path). Derives disposition from the lineup (§5.1), opens era 0.
- Telemetry writer: `<run>-artist-evolution.csv` with columns
  `week,year,artistId,eraIndex,fromGenre,toGenre,trigger,phase,commercialPressure,
  artisticPressure,peerPressure,labelPressure,internalPressure,resistance,ratified`.
  Emit **observation rows even when disabled** — we want the counterfactual: how often
  *would* an artist have converted, and to what, before we let it happen.

**Gate 0 (hard):** decade run with the flag off is **byte-identical** to `mix8-decade`
on `genre-decade-shape.csv`, `year-end-hot100.csv`, and `format-mix.csv`. Initialization
must consume no draw from the global `GD.Rand` stream (see §6.1).

### Phase 1 — Eras and identity ratification ← *the one that fixes Folk Rock*

The consolidation rule. After a release resolves, `ArtistEvolutionService.OnProjectReleased`
pushes the project genre into `recentProjectGenres` (fixed ring, N = 4). Then:

```
RATIFY when ALL of:
  • the window is full (N projects on record)
  • a single genre X ≠ identity holds a strict majority of the window
  • GetAdjacency(identity, X) >= AdjacencyFloor        (musical path, no teleports)
  • X is available for new supply this year             (climate)
  • year - lastIdentityChangeYear >= IdentityChangeCooldownYears
  • the artist is not in a terminal career state
  • the annual global conversion budget is not exhausted (§7)
```

On ratification: close the current era (`endYear = year`), set `primaryGenre = X`,
demote the old primary to `secondaryGenre`, open a new era with the trigger derived from
the outcome history, append **one** string to `careerEvents`, emit telemetry.

`formationPrimaryGenre` / `formationSecondaryGenre` are **never** touched — the
`native vs transitioned` telemetry in `ChartAuditRunner` (`:2407`) keys off them and must
keep meaning "against where they started."

Note what this alone does for folk: a Folk act that cuts three folk-rock sides in 1966
becomes a Folk Rock act, and its retention jumps ≈0.55 → ≈0.82 because retention reads
the *new* identity's rising baseline. It stays converted. The 199 folk acts of 1965 stop
being a dead-end pool.

**Gate 1 (hard, decade run, seed 1001, canonical flags):**
- Total genre-share `sumAbsErr` ≤ **320** (mix8 = 309.1; ≤ +11 absorbs single-seed noise
  for reachable genres — but see §8 on the ~50-pt floor for unreachable ones).
- No individual **benchmarked** genre's `sumAbsErr` degrades by more than **4.0**.
- ~~FolkRock: identity-holding releasing artists in 1966 ≥ **150** (from 86)~~ —
  **DROPPED AS A GATE (author's call, 2026-08-14).** Retained below as a watched metric and
  an open investigation. It was never reachable by the mechanism this phase owns, and
  holding Phase 1 to it meant Phase 1 could never pass for reasons that have nothing to do
  with Phase 1. Still required: **FolkRock share error does not *worsen*.**
- Economy inside tolerance vs mix8: total market units, LP unit share per year, and
  label-tier counts inside the seed-noise band, and **total release count within ±1.5%**
  (measured cascade at 255 conversions is −0.7%; see §2.2). "Untouched by construction" was
  the wrong bar — genre feeds release priority, so it was never going to hold exactly.
- Conversions/decade land inside the §7 budget with the budget **not** binding in most
  years (if the cap is the binding constraint everywhere, the rule is mistuned, not safe).

### Phase 2 — Pressure, motive, and drift

Now the artist gets a say in *whether* they drift, without getting a say in *where*
supply goes.

- `ArtistEvolutionPressureService.Evaluate(artist, year)` → the five pressures and a
  dominant trigger. Inputs that actually exist: `consecutiveFlops`,
  `contractConsecutiveFlops`, `momentum`, `careerState`, `GetDramaRisk()`,
  `groupCohesion`, recent influence memories. **Do not use `criticalAcclaim` until §5.3
  gives it a writer.**
- Pressure feeds two things and only two things:
  1. **The ratification threshold.** High pressure shortens the window / lowers the
     majority requirement; high `rootsAttachment` + high `reputation` + high
     `groupCohesion` raise resistance. A settled star with three hits doesn't wander.
  2. **A bounded artist-side reweighting** inside `GetIdentityFit`. The current constants
     (4.0 / 2.25 / 1.45 / 0.55) become the **neutral case**, reproduced exactly when
     evolution is disabled or the artist is unpressured. Under pressure, the primary
     anchor softens and *adjacent* candidates lift — strictly bounded, e.g. primary
     ∈ [2.6, 4.0], adjacent-family ∈ [1.45, 2.1]. Never below the current "other" floor,
     never above the current primary. This is the "restlessness" term.
- `BackToRoots`: when `rootsAttachment` is high and commercial pressure dominates, the
  lift goes to `formationPrimaryGenre` instead of forward-adjacent genres. The band that
  strips back to blues after two failed pop singles is the same mechanism running
  backwards, and it's half of what makes the arc feel authored.
- `ReleaseCreativeIntent` computed and logged per release.

**Gate 2:** same share/economy bands as Gate 1, plus: the distribution of era counts per
artist is sane (median 1–2 eras for a normal career, not 5), and no genre's project
supply swings more than **±8%** relative to Phase 1.

### Phase 3 — Critical acclaim gets a writer

`criticalAcclaim` is currently a zero. Give it a real one before anything depends on it:
a bounded per-release critical read derived from what we already compute —
`record.originality`, `album.thematicCohesion`, `pooledAppeal` vs. commercial outcome
(the acclaimed-but-didn't-sell case is the interesting one), `label.productionQuality`.
Decays slowly. It feeds artistic pressure, the `CriticsDarling` / `Underrated` reputation
tags, and Phase 4's cohesion loop.

Keep it **read-only to the economy** in this phase: acclaim must not touch units,
advances, or signing decisions yet. It is a narrative and pressure signal only.

**Gate 3:** economy metrics byte-comparable to Phase 2 (acclaim is inert on money by
construction — verify it, don't assume it).

### Phase 4 — The cohesive-album knock-on

The album-as-art movement, built as an emergent loop rather than an object.

- One bounded global: `AlbumLegitimacy` ∈ [0, 1], starts at 0, **hard-zero before 1964**.
- After an album completes its chart life, it contributes to legitimacy **only if it
  cleared a real bar in public**: high `thematicCohesion` **and** genuine reception
  (chart performance and/or the Phase-3 acclaim signal). Contribution scales with how
  early it happened — a 1965 statement record moves the needle far more than a 1968 one,
  because by 1968 everyone is already doing it.
- Legitimacy feeds back into **exactly two places**:
  1. `AlbumModel.GetMaximumAchievableCohesion` — the era term becomes
     `era_exogenous × (1 + k × legitimacy)`, **clamped to at most 1.25× the current
     exogenous curve and never below it.** The existing curve remains the floor and the
     shape; legitimacy can pull it forward in time, not rewrite it. The `pioneer` path
     stays exactly as authored — it is the seed that makes the loop startable, and it is
     tuned against a measured concept-album count.
  2. Per-artist **influence memory**: artists with high `peerSensitivity` and non-trivial
     adjacency to the landmark's genre remember it, which raises artistic pressure and
     conceptual inclination in Phase 2's evaluation. This is the Rubber Soul → Pet Sounds
     chain, and it is the *only* place peer influence enters.
- Influence propagation must **not** be a full 22.5k-artist sweep per landmark (§6.2).

**Gate 4 (the money gate — this is the phase that can break the economy):**
- LP unit share per year stays inside the calibrated band (29.5 / 35.0 / 41.3 / 48.4 /
  55.4 for 1960/62/64/66/68 — treat ±1.5 pts as the tolerance).
- Album chart genre composition and mean album chart life do not move outside seed noise.
- Concept-album count per year stays in the "handful across 1965–66, wave by 1968"
  shape the current constants were tuned to. If legitimacy produces 40 concept albums
  in 1966, `k` is wrong.
- Total market units within seed noise.
- **A run with `AlbumLegitimacy` pinned at 0 must reproduce Phase 3 exactly.**

### Phase 5 — Presentation (and only presentation)

- `ArtistDiscographyProfile` / `ArtistDiscographyEra` assembled on demand from
  `evolution.eras` + release history. **Assembled for display, never stored on the
  artist** — 22.5k artists must not each carry a UI model.
- `ArtistDetailPanel` groups the discography by era with the era summary line.
- Reputation tags derived from evolution state (`Experimental`, `Innovator`,
  `GenreBending`, `Authentic`, `Traditional`, `Difficult`) — spend the existing enum.

Target output, from real state:

> **The Ascenders**
> **1961–1963 · R&B club years** — Early sides leaned on rough vocals and a sax section.
> **1964–1965 · Soul crossover** — After a regional breakout, the group smoothed out.
> **1966–1967 · Studio experiment** — Following two cohesive albums by acts they'd
> shared bills with, their third LP reached for a unified sound. Critics heard it before
> radio did.
> **1968–1969 · Back to roots** — After two underperforming singles, they stripped back
> toward bluesier material.

**Speculative, not authorized:** solo splits, producer signature sounds, regional scenes,
labelmate influence, wiring the dormant momentum market.

---

## 6. Determinism and performance

### 6.1 RNG discipline

Non-negotiable, and the reason Phase 0's gate is byte-identity:

- Evolution gets its **own `RandomNumberGenerator`**, seeded from
  `SimulationSeedBootstrap.RequestedSeed` with a fixed namespace XOR — the exact pattern
  `ArtistManager.EnsurePopulationRng` uses (`Seed = seed ^ 0x617274697374706fUL`). Pick a
  distinct constant.
- **Never draw from `GD.Rand`/`GD.Randf` on an evolution path.** Prefer no randomness at
  all: the ratification rule as specified in Phase 1 is fully deterministic, and the
  existing project-genre selection is already a hash rather than a draw. Keep it that
  way. If a tie-break needs entropy, hash `artistId|year|eraIndex` the way
  `GetDeterministicSupplyRoll` does.
- Anything that changes how many draws the global stream sees will shift every downstream
  result exactly like a seed change, and no gate in this document will be interpretable.

### 6.2 Cost

Runtime is album-count-bound and this codebase has already been bitten by four
accidental quadratics (roster sweeps, population reconciliation, live-record scans).
Evolution runs against 22.5k artists. Therefore:

- **Event-driven only.** Hook `OnProjectReleased` / chart-run completion. **No weekly
  sweep over the artist registry.** There is no "check every artist every week" step in
  this design and there must not be one.
- `recentProjectGenres` is a **fixed-size ring buffer** (`Genre[4]` + count), not a
  `List` that grows for a decade.
- `influences` is **capacity-bounded** (keep the strongest ~8, drop anything older than
  ~3 years on insert). An unbounded influence list on 22.5k artists is a memory leak
  wearing a narrative costume.
- Phase 4 influence propagation must be **indexed, not swept**: maintain a genre →
  susceptible-artist index, or propagate lazily at the artist's next evaluation by
  reading a small global landmark ring. Do not iterate `GetAllArtists()` per landmark.
- `eras` is naturally small (1–4 per career). Fine as a `List`.
- One string per era into `careerEvents`, not one per pressure evaluation.

---

## 7. Guardrails and kill criteria

The safety story is §2's inversion. These are the belts and braces.

1. **Global annual conversion budget.** Cap ratifications per year as a fraction of the
   active releasing population (start at **≤ 3%/yr**, tunable). Migration is
   self-reinforcing through retention; an uncapped rule can cascade a genre pool in two
   years. The budget is the circuit breaker, not the design.
2. **Per-genre outflow cap.** No single identity genre may lose more than **~15%** of its
   identity population to conversion in one year. A scene thins; it doesn't evaporate.
3. **Adjacency floor.** Ratification requires an explicit or same-family edge above the
   floor. No Soul → ProtoMetal. If a historically real path has no edge, **add the edge**
   in `BuildEdges` — deliberately, in its own commit, with the run to show it — rather
   than lowering the floor.
4. **Cooldown.** One identity change per artist per N years (start N = 2). Careers, not
   weathervanes.
5. **The Country interaction.** `FormationAffinity(Country) = 2.2` currently makes Country
   the top off-identity destination for Folk acts (113 vs 33). Once identity ratifies,
   that leak becomes *permanent conversions into Country*, which would inflate a genre we
   just spent mix8 calibrating. **Measure Country's identity population and share in Gate
   1 specifically.** If Country over-runs, the fix is to make `FormationAffinity` apply to
   *formation only* and not to project transition — it is documented as compounding
   across both, and 71% of selections never reach it, so splitting the two is cheap and
   honest. Do not fix it by lowering 2.2 (that value is backed by a measured elasticity).
6. **Emergent-genre asymmetry, on purpose.** Conversion *into* a genre in its emergence
   window is the historically correct direction and is where the payoff lives. Conversion
   *out of* a genre at its peak is suspicious. Asymmetric caps are fine and expected.
7. **Kill criteria — revert the phase, don't tune around it:**
   - Total share `sumAbsErr` > 340 (mix8 + ~30).
   - Any benchmarked genre degrades > 6 pts `sumAbsErr`.
   - Total market units, LP share, or label-tier counts outside the seed-noise band.
   - Median eras per completed career > 3.
   - Decade wall-clock regresses > 10%.

---

## 7a. OPEN INVESTIGATION — the Folk Rock shortfall is not an evolution problem

Recorded because the proximate motivation for this whole directive turned out to sit
outside it. **This wants its own investigation and probably its own directive.**

**What is established.** FolkRock runs 0.9–2.5 points under its market-share target every
year from 1965. Its own share target implies roughly **111** identity-holding releasing
artists at 1966, not the 150 the gate asked for; the model has 86–90.

**Why ratification cannot fix it.** Evolution ratifies genres that `GenreSupplyService`
already selected. Across 1965–69 there are **61** folk-family → FolkRock projects, from
**56 distinct artists, ever**. The convertible pool is smaller than the shortfall.

**Two supply levers were tried and both failed, in the same direction:**

| lever | total sumAbsErr | FolkRock | what happened |
|---|---|---|---|
| adjacency-aware `GetIdentityFit` (one continuous scale) | 309.1 → 310.6 | 6.1 → **10.1** | Inverted. Cross-family destinations with an authored edge start from the .55 floor and gain far more than a same-family lineage starting at 1.45 — Country gained 2.5x against FolkRock's 1.3x, and folk acts went to Country **71 → 98**. Fixed by tiering (§ `GenreSupplyService`), but the fix is a *correction*, not a FolkRock solution. |
| adjacency-aware fit, **two-tier** (family as base, adjacency modulating inside it) | 309.1 → **315.1** | 6.1 → **11.0** | Worse than the version it fixed. It restored FolkRock's advantage over Country (1.40x → 2.24x) but *weakened* its advantage over **ContemporaryFolk** (1.23x → 1.13x) — and CF, not Country, is the genre actually absorbing the folk-family surplus. Fix one competitor, lose to the other. |
| `--split-formation-affinity` | 309.1 → 324.5 | 6.1 → **8.5** | Mechanically correct — Country vanishes from folk destinations entirely, confirming §7.5's leak was real — but Country's own share fell 5.5 and FolkRock still got worse. |

**Both runs REDUCED total FolkRock project supply** (893 → 752 and 801). Every lever pulled
at the identity-fit weight has moved other genres more than it moved FolkRock.

**The unexamined suspects**, in the order worth trying:
1. **Retention, not fit.** `GetProjectIdentityRetention` reads the identity genre's own
   baseline curve. FolkRock's baseline peaks at .78 in 1966, so a FolkRock act retains at
   ~.82 — meaning acts who arrive *stay*, but acts elsewhere are never offered the move.
   The 71% Retained share is the real ceiling on all transition, and no fit change touches it.
2. **`Folk` carries `DeathYear = 1966`.** From 1967 folk acts take no new supply in their
   own identity and simply thin out (191 → 109 → 32) rather than converting. That is a hard
   supply zero sitting exactly where the historical conversion should happen.
3. **ContemporaryFolk absorbs the family surplus** — over target 1.2–2.1 points *every year*
   while FolkRock is under. Trimming its baseline toward its own (correct, mid-decade-peaking)
   target is the most direct redistribution available, and has not been tried.
4. Whether the 1965 emergence year is simply too late for a genre whose target share is
   already 2.2% in its first year.

**Do not** re-attack this from `GetIdentityFit`. That has now been measured **three times**,
in two different formulations, and FolkRock got worse in all three. The weight has one
FolkRock lever and several competitors sharing it; every setting trades one competitor for
another. `--enable-adjacency-identity-fit` stays **OFF** — the code is kept, flagged and
probed, as the record of what was tried.

**CONFOUND, disclosed.** The adjacency EDGE FILL (31 → 74 edges) shipped before all four
of the runs above and has never been measured on its own. It is not inert: it changes
`ArtistManager.ChooseRuntimeSecondaryGenre`, which picks uniformly among adjacency-positive
candidates, so more edges change that array's length and therefore **every runtime artist's
secondary genre** — and secondary feeds `GetIdentityFit` at 2.25. Some unknown part of the
regressions above belongs to the fill rather than to the lever each run was testing. An
edge-fill-only control is required before anything else is layered on top, and it doubles
as the correct paired baseline for the evolution bundle.

## 7b. PHASE 6 — the monotone-biography repair

Phases 1–4 bundled at **303.8** (beating mix8's 309.2) with every kill criterion clear, and
produced a mechanism that worked and a biography that didn't: **92% of 1,616 conversions said
`CommercialFailure`**, and five of eleven triggers never fired at all. Diagnosed against
`bundle-1001-artist-evolution.csv` (8,371 observations). Three separate faults, not one.

### 7b.1 Two triggers had no writer anywhere in the codebase

`CriticalBreakthrough` and `CohesiveAlbumMovement` were declared in the enum and returned by
no code path. Zero rows even in the *pre-block* observation set, which is the tell — a bar
set too high still produces blocked candidates. `AbsorbLandmarks` wrote
`ArtistInfluenceType.CohesiveAlbum` into the memory record and **nothing ever read `.type`**;
every peer motive collapsed to `PeerInfluence` regardless of what kind of record caused it.
So the Rubber Soul → Pet Sounds chain the directive is built around had no route to its own
trigger even with perfect numbers.

### 7b.2 `Dominant()` compared six floats that were not on one scale

Measured means: commercial **.748**, internal .436, artistic .410, label .170, peer **.0021**
(max .0845). A raw `max()` over a three-term additive sum and a five-factor sub-unit product
is not a comparison of motives — it is a comparison of formula shapes.

- **Commercial pressure was a constant.** `.50*streak + .30*cold + state` gave a **~0.40
  floor before a single flop**, because momentum sits near zero for nearly everyone (careers
  are two records long; most never chart). Rebuilt so the streak is the spine: no streak, no
  commercial pressure, however cold and precarious the act.
- **Label pressure was a scaled copy of its own competitor.** Both its terms multiplied
  `failing = flops/4` — the identical variable commercial's streak term used, with a smaller
  coefficient and no additive floor. Winning required momentum > 0.33 in an act on a
  four-flop streak, which is self-contradictory. It now has its own motive: a label that has
  noticed somebody else's record working in a genre it believes in, whether or not the act in
  front of it is failing. (`SetLabelPressure` also had **zero callers** — the player lever is
  wired but nothing in the UI writes it. Still open.)
- **`GenreClimateShift` was collateral damage from Phase 2.** It lived in the fallback
  *below* `if (PressureEnabled && dominant != None && restlessness > 0)`, and since commercial
  never read zero the fallback was unreachable whenever motive was on. Turning Phase 2 on had
  switched a Phase-1 trigger off. It is now scored candidate-side and weighed against the
  winning pressure's normalised score.

Motive is now decided on **normalised salience** (which pressure is unusually high *for that
pressure*); **restlessness stays on raw magnitude**, so conversion volume is not quietly
inflated by a change that claims to only relabel.

### 7b.3 The earliness premium was anti-phased with the thing it scored

`GetEarliness` paid `(1969 − year)/5`. Albums clearing the 0.72 cohesion bar ran
**32, 33, 564, 1441, 1610** across 1965–69 — so the premium paid 0.8 in a year that produced
33 of them and **exactly 0.0** in the year that produced 1,610. Mean landmark strength by
year: .534, .496, .348, .256, .133, **.000**. The premium was spent entirely in the years the
model structurally could not produce the record it was meant to reward.

Re-phased onto **legitimacy** rather than the calendar: the first act to make one is early
whenever they do it, and the premium decays as the movement actually happens. Same quantity
that lifts the cohesion ceiling, so getting easier to make and less remarkable to have made
are one process. Floored at `MinimumEarliness` so a late landmark still counts for something.

### 7b.4 The defect that hid all of the above

**The entire Phase 3/4 chain only ever ran on records that never charted.**
`ArtistManager.OnRecordLeftChart` sets `record.artistChartRunCompleted = true` *before*
calling `RosterManager.RecordChartRunComplete`, whose first statement is a guard on that flag
— and the acclaim writer, the landmark rule and the cultural ledger all sat **below** the
guard. A record that charted took the early return; only records that never charted reached
the narrative reads. Fixed with a separate `culturalRunCompleted` flag and a
`RunCulturalReads` call on both completion paths.

This is why the pre-repair run showed 408 landmarks in offline analysis but peer pressure
peaking at .0845: almost none of those albums were ever seen by the service.

### 7b.5 The modular seam for journalism

Split into three layers so the magazine system can arrive as a *caller*, not an edit:

| Layer | File | Mutable? | Journalism touches it? |
|---|---|---|---|
| 1 — **merit** | `ArtisticMeritService` | no, intrinsic | **never** — a magazine discovers a work of art, it does not create one |
| 2 — **recognition** | `CulturalRecognitionService` | yes, channel-fed | **yes** — `Deposit(recordId, amount, RecognitionChannel.Press, year)` |
| 3 — **ledger** | `CulturalMemoryService` | append-only ring | reads merit × recognition; **no rule changes** |

The landmark bar is stated against *recognition*, not chart position. A record the press
carried and the public ignored clears it through the same door with nothing in
`AlbumLegitimacyService` edited — pinned by probe 32. Ledger capacity 16 → **256** (the old
ring was lapped every ~6 weeks at the 1968-69 landmark rate while the median act releases
twice a *decade*, so an act witnessed ~16 of the decade's 408 landmarks).

`peerSensitivity` is now applied **once**, at memory formation. It was applied at both ends,
squaring a sub-unit term.

### 7b.6 Measured result — 1960-62, seed 1001

| trigger | before (decade) | after (3yr) |
|---|---|---|
| CommercialFailure | **92.0%** | **40.6%** |
| PersonalAmbition | 4.3% | 35.5% |
| InternalTension | 2.9% | 13.5% |
| CriticalBreakthrough | **0** | 4.2% |
| GenreClimateShift | **0** | 2.7% |
| PeerInfluence | **0** | 1.9% |
| LabelPressure | **0** | 0.5% → retuned |
| CohesiveAlbumMovement | **0** | *untestable before 1964* |

Cultural ledger live: 246 events from 150 distinct source acts in three years.

**Calibration note, recorded because it will recur.** At equal event weight the hit channel
(~78 top-ten singles a year) drowned the landmark channel (a few dozen a *decade*) and
`PeerInfluence` took **68%** of conversions — a second monopoly in place of the commercial
one. Split via `HitInfluenceWeight = .55` vs `LandmarkInfluenceWeight = 1.0`: a hit makes
other acts want to do that; a landmark changes what they think a record can be.

### 7b.7 RESOLVED — a landmark album is a body of work, not a concept album

Two further defects surfaced chasing it, both of which had to be fixed before the real one
was visible.

**Landmarks fired on the retirement hook.** `OnAlbumChartRunComplete` is reached when a
record retires — for an album, ~94 weeks after release (a ~42-week chart life plus a 52-week
tolerance). Sgt. Pepper was a landmark within weeks, and the acts who answered it did so that
summer. Worse, it made the channel unobservable: across the full `bundle-1001` decade run,
**41,674 albums minted and not one completion hook ever fired**. The album-as-art loop had
never once executed in any run. Now offered the weekly *published* album chart (tens of
entries, self-guarding after publish), publishing once at recognition. Merit moved onto
`Album.artisticMerit` at pressing time, which is Layer 1's contract made literal.

**And then the real blocker.** Of every album clearing the 0.72 cohesion bar in 1960–66:

| year | over the bar | artist-made | soundtracks |
|---|---|---|---|
| 1960–63 | 84 | **0** | 84 |
| 1964 | 20 | **0** | 20 |
| 1965 | 29 | **0** | 29 |
| 1966 | 31 | **0** | 31 |

Max cohesion on an *artist-made* album: **pinned at exactly 0.080** — the clamp floor in
`GetMaximumAchievableCohesion` — for 1960 through 1965, reaching 0.484 in 1966. Soundtracks
take a separate path (`0.6 + criticalPrestige * 0.3`) that routinely lands 0.72–0.90, and
carry `artistId = string.Empty` by design, so they can never publish a cultural event.

Root cause: `Mathf.SmoothStep(0.12f, 0.96f, t)` is being read as a lerp from 0.12 to 0.96.
Godot's SmoothStep treats those as **edges**, not as an output range, so the era term is
`0, 0, 0.065, 0.428, 0.84, 1.0` across 1964–69 — zero until 1965, roughly two years later
than the surrounding comments assume. The `pioneer` escape hatch that was supposed to open a
"deliberately vanishing path from 1965" requires `excellence > 0.55`, where excellence is a
product of two `(x−0.70)/0.30` terms — both talent and label production at 0.92 still yields
only 0.53. It fires **zero times in seven years**.

**Consequence: `CohesiveAlbumMovement` cannot fire before 1967 by construction.** No run
ending before 1967 can test it, which is why the 3-year and 7-year runs both returned zero.

### 7b.8 The resolution — and the definition that was wrong

**A landmark album is not a concept album.** Neither Rubber Soul nor Pet Sounds was one. The
rule was stated against `thematicCohesion`, which is wrong twice: it is the concept-album axis
*and* it is the field the era ceiling clamps to 0.08. Restated against
`AlbumModel.GetAlbumIntegrity` — **mean track quality against the best track** — which is a
fact about the tracks and therefore reachable in any year.

**Singles are not disqualifying.** Rubber Soul and Pet Sounds both carried hits. A hit raises
the peak; what decides the record is whether the other ten sides stand up beside it. Integrity
reads UNDECAYED quality, since freshness is about commercial life remaining, not about how
good a song is.

**Soundtracks stay out**, now explicitly via `IsEligibleFormat` rather than incidentally via an
empty `artistId`. They sit with comedy, children's and classical as an odd entity — culturally
weighty sometimes, but not the album-as-art movement.

**`LegitimacyStartYear` 1964 → 1960.** Jazz established the album as a serious form before pop
reached for it, and that chain is now emergent rather than authored: early jazz landmarks
accumulate legitimacy → legitimacy lifts the cohesion ceiling → the lifted ceiling is what
lets mid-decade rock records become statements. Rarity keeps the early ones scarce.

**Target: 25–40 landmarks across the WHOLE DECADE** — a handful of jazz records before 1965,
then three or four a year. Plenty of albums are cohesive, ambitious and well reviewed; almost
none are landmarks, and that gap is what the bar exists to create.

**Calibration trap, recorded because it will recur.** A bar carried across from another
quantity is meaningless until checked against the new distribution. Track qualities within one
album cluster tightly (sd .045), so mean-vs-peak sits at **.823 for an ordinary record** — the
inherited 0.72 bar admitted **98%** of eligible albums, minted 347 landmarks in two years and
saturated legitimacy by 1965. Now .92 (~99.3rd pct) plus a separate **merit** gate at .78,
because integrity alone says a record is consistent, not that it is worth consistently
listening to; a uniformly mediocre album scores well on a ratio.

Estimation method validated: a static join over composition × chart predicted ~17 landmarks
for 1964–65 at the interim bars; the run produced **19**.

### 7b.9 The album shift is a rock phenomenon

Album integrity measured **flat across genres** (0.823–0.831), so landmarks came out as genre
noise. That is wrong about the period in a specific way: jazz cut LPs as bodies of work from
the start and had no revolution to undergo, while pop and rock began as a hit with filler
around it. `AlbumModel.GetTrackConsistency` gives each family an innate starting point
(jazz/classical .80, blues .55, folk .50, gospel .45, rest .20) and lets it learn at
`GetAlbumRevolutionSusceptibility × GetAlbumEraWeight` — so rock (.80) travels most of the
distance by 1969 and pop (.12) largely does not.

Spent as **variance, not level**: raising jazz track quality would worsen a known miss (the
model already over-weights jazz on the early album chart), whereas narrowing the spread raises
the body-of-work reading while slightly *lowering* peak-driven chart appeal. Draw count
unchanged, so the RNG stream is untouched.

### 7b.10 Eligibility — the two gates were reading one fact twice

`evo9` produced **exactly the predicted 12** landmarks for 1960–65, confirming the count
calibration. Six of the twelve were **children's records**, which exposed two defects.

**Odd-entity families.** Comedy, children's and classical are the same kind of object as the
soundtrack — they sell as albums, are occasionally culturally large, and are not participants
in the album-as-art movement. Novelty material is uniformly pitched *by construction*, and a
body-of-work reading is a consistency ratio, so it rated them highly for exactly the wrong
reason. `IsEligibleFamily` now excludes `NonMusic` and `Classical`.

**The merit gate was not independent of the integrity gate.** It was supposed to separate
"consistent" from "worth consistently listening to", but `bodyOfWork` feeds `GetCraft`'s
coherence term at 35%, so a consistent record scored well on merit *partly for being
consistent*. Added `LandmarkOriginalityBar = .70` against `record.originality` — the one axis
nothing else in the rule derives from, and therefore the only gate that actually separates a
landmark from a competently uniform record.

### 7b.11 MEASURED RESULT — `evo11`, seed 1001, 1960-65

The state the branch is paused at.

**Landmarks: 17 across 1960-65** (2, 0, 0, 6, 5, 4). Projecting 1966-69 at the cap gives a
decade total of **~33-41**, inside the 25-40 target. *Projection, not measurement — no run has
gone past 1965.*

**71% of landmarks are jazz-family** (BossaNova 8 + Jazz 4 of 17). `GetTrackConsistency`
reproduces the historical shape without being told to: jazz starts at .80 innate consistency
and has no revolution to undergo, so it dominates the early landmark set exactly as the period
did. Legitimacy reaches .4395 by end-1965 — building, not saturating.

**Trigger mix** (793 ratified conversions):

| trigger | share | was (bundle) |
|---|---|---|
| PersonalAmbition | 38.2% | 4.3% |
| CommercialFailure | **37.5%** | **92.0%** |
| InternalTension | 14.1% | 2.9% |
| CriticalBreakthrough | 3.2% | **0** |
| LabelPressure | 2.5% | **0** |
| GenreClimateShift | 2.5% | **0** |
| PeerInfluence | 1.5% | **0** |
| BackToRoots | 0.4% | 0.5% |
| **CohesiveAlbumMovement** | **0.1%** | **0** |

**The Rubber Soul → Pet Sounds chain executed for the first time.** One conversion, motivated
by another act's landmark album, with the source act named in the era line. 792 of 793
conversions name the specific act whose record moved them; 36 distinct source acts.

**Honest reading of that 0.1%.** 76 ratified conversions had a `CohesiveAlbum` as their
strongest live influence, so the channel carries — it loses the *motive contest*, because peer
pressure peaks at .421 against a commercial pressure that reaches 1.0. The chain is proven
end-to-end but is currently too rare to write a storyline. As recorded in 7c, the lever for
that is `HitInfluenceWeight` / `InfluenceMemoryYears` / `LedgerCapacity` — **not** the landmark
bar, which is calibrated to a historical target.

`CommercialBreakthrough` did not fire in this run (it reached 1-2 conversions in earlier ones);
it lives in the Phase-1 fallback, which is reached only when nothing is pressing.

---

## 7c. REMAINING WORK

Ordered by what blocks what. Everything below is measured-from or explicitly unverified;
nothing here is speculative scope.

### Blocking the decade A/B

1. ~~Verify the genre distribution of landmarks.~~ **DONE** (7b.11): 71% jazz-family, no
   odd-entity genre survives the family gate.
2. ~~`CohesiveAlbumMovement` has never fired.~~ **FIRES, and the 0.1% was the wrong reading.**
   That is the *trigger* share; the decade run shows **6.8% (s1001) and 9.7% (s2002) of
   conversions name an album as the influence that moved them** (§7d). The channel carries and
   the biography already says so — what loses the contest is the trigger *label*, because peer
   pressure peaks at .421 against commercial at 1.0. If that is worth closing, the lever is
   `HitInfluenceWeight` (currently .55), `InfluenceMemoryYears` (3) or `PeerSalience` (.30) —
   **not** the landmark bar, which is calibrated to a historical target and must not be moved
   to make a trigger fire more often.
3. ~~**Verify the decade tail.**~~ **DONE** (7d). 33 and 26 landmarks on two seeds, both inside
   25–40; legitimacy reaches .68/.66 without saturating; the canonisation cap **never binds
   late** — the era-rising bar governs instead, and its interaction with early landmark timing
   is the one open instability (s2002 puts 3 landmarks in 1966–69 against s1001's 16).

### The decade A/B itself

3. ~~**Run against the 303.8 Phase 1–4 bundle.**~~ **DONE** (7d), and superseded in method: the
   recorded scalar was unusable because the control had drifted +7.4 since the bundle, so the
   A/B is against a **contemporaneous paired control, one per seed**. Result: +12.7 on seed
   1001, −5.2 on seed 2002, mean **+3.7**. No regression. Album unit share and album-chart
   genre mix were in the blast radius and came back neutral.
4. **Salience constants are sized off a single 3-year window** (`CommercialSalience .45`,
   `Artistic .52`, `Critical .30`, `Peer .30`, `Label .36`, `Internal .58`). They are the most
   likely thing to want moving after a decade run, and the trigger mix is the metric to read
   them against — not `sumAbsErr`.
5. **Commercial pressure no longer has a floor**, which lowers `restlessness` broadly and
   therefore reduces conversion *volume*, not just its labelling. Expect fewer conversions than
   the bundle's 1,616 and check that against the guardrail budget before reading it as a
   regression.

### Deferred, with reasons

6. **The `Mathf.SmoothStep` defect** in `GetMaximumAchievableCohesion` (7b.7). Real, ~80%
   confidence, no longer blocks anything since the landmark rule left `thematicCohesion`. Wants
   its own commit and A/B because the fix is **not monotonic** — it raises 1960–66 cohesion and
   *lowers* 1967–69 by ~10%, and cohesion feeds `pooledAppeal → hookStrength →` album chart.
   Pair with `EarlyStatementExcellence = .55`, which is the unambiguous half: max label
   `productionQuality` measured .91, so the bar needs artist talent above **.936** and fires
   zero times in seven years against a comment stating the target is "a handful across 1965-66".
7. **`SetLabelPressure` has no callers.** The player's lever on artist direction exists in the
   pressure model and nothing in the UI writes it. AI label pressure now works (2.5% of
   conversions); the player half is still unwired.
8. **`CulturalRecognitionService.Deposit` has no callers** — by design. It is the journalism
   seam, and probe 32 pins that a press-carried record which never charted can mint a landmark
   through it with no rule in `AlbumLegitimacyService` changed.
9. **Soundtracks are excluded from the cultural ledger entirely.** Deliberate (author's call:
   an odd entity alongside comedy/children's/classical), but it means a blockbuster
   soundtrack currently influences nobody despite `blockbuster-soundtrack-longevity` giving it
   200–350 chart weeks.

## 7d. MEASURED RESULT — the decade A/B, two seeds

Four runs, `--lean-probe`, 522 weeks, canonical flags. Treatment adds
`--enable-artist-evolution --enable-evolution-pressure --enable-album-legitimacy
--enable-cultural-memory`; control is `--disable-artist-evolution` and **nothing else** —
`AlbumLegitimacyEnabled` and `CulturalMemoryEnabled` gate independently of `enabled`
(`AlbumLegitimacyService.cs:201`, `ChartManager.cs:2268`), so passing the phase flags to a
control contaminates it.

| | treatment | control | delta |
|---|---|---|---|
| seed 1001 | `evo12` **322.1** | `ctl12` **309.5** | **+12.7** |
| seed 2002 | `evo12b` **302.3** | `ctl12b` **307.6** | **−5.2** |
| mean | 312.2 | 308.6 | **+3.7** |

**The single-seed result was read as a regression and it was not one.** Seed 1001's +12.7 —
against the bundle's +1.7 — looked like Phase 6 had made evolution expensive. Seed 2002
inverts the sign: evolution *improves* the calibration by 5.2. The two-seed mean cost is
+3.7, next door to the bundle. There is no regression here to diagnose.

**Every marginal per-genre degradation flipped sign.**

| genre | s1001 | s2002 |
|---|---|---|
| Country | +4.76 | −2.50 |
| BritishBeat | +4.66 | −1.30 |
| TeenPop | +4.49 | −2.20 |
| Comedy | +4.20 | +0.02 |

The Country reading built on seed 1001 — deficit deepening on a *net-positive* identity flow,
therefore "the loss moved to the units side" — described one world and is withdrawn. §7's
guardrail 5 is not currently binding in either direction.

**What does replicate** (same sign both seeds): EasyListening improves −4.88/−2.99 (the
largest consistent effect either way), Folk −1.53/−1.68, Childrens −1.36/−1.80, RnB
−0.54/−3.71. **BritishPop is the only genre consistently worse**, +1.84/+3.42, and is already
filed as panel over-play in [[britishpop-panel-overplay-vitality]]. SurfRock, Blues and
FolkRock degrade mildly on both.

**The story metrics replicate, which is the half that matters.**

| | seed 1001 | seed 2002 |
|---|---|---|
| ratified conversions | 1,549 | 1,551 |
| landmarks (target 25–40) | 33 | 26 |
| distinct tastemakers | 69 | 82 |
| conversions naming an album | 6.8% | 9.7% |
| end-1969 legitimacy | 0.6813 | 0.6555 |

Trigger mix agrees within ~1.5 points on every trigger. Conversion volume came in at 1,549
against the bundle's 1,616 — the small drop §7c item 5 predicted from commercial pressure
losing its floor.

**`CohesiveAlbumMovement` at 0.1% understates the album channel by ~100×.** That figure is the
*trigger*. The `influenceType` column says **106 conversions (6.8%), and 150 (9.7%) on seed
2002, name an album as the record that moved them** — the biography already reads "moved by
X's album" while the motive is filed as PersonalAmbition. §7c item 2's lever is therefore
about making the *label* match a story the ledger is already telling, not about making the
channel carry. It carries.

**The one real instability is landmark timing, and it runs the historically wrong way:**

```
          60 61 62 63 64 65 | 66 67 68 69   late
s1001      2  0  0  6  5  4 |  5  3  6  2    16
s2002      5  3  3  4  5  3 |  1  0  1  1     3
```

Both decade totals are inside 25–40, so the canonisation budget is right. What is unstable is
the era-rising bar's interaction with *when* the early landmarks land: seed 2002 front-loads,
raises the bar, and then produces almost nothing across 1966–69 — precisely the years that
define the landmark album historically. **This is the open item worth working, ahead of any
genre number in this section.**

**Economy is neutral** (seed 1001 vs its control): total units +0.06% against a ±1.5%
tolerance, LP share within 0.8 at every year and identical from 1966 on, album-chart genre mix
shifting ≤1.2 points (Soul +1.0, EasyListening +0.9, TraditionalPop −1.2, Folk −1.0). Album
chart slot-weeks identical at 65,890. Phase 4 remains free.

**Guardrails:** budget and outflow caps never appear in the refusal ledger; cooldown refused
14; `NoMusicalPath` is still the bulk at 54%. 1,493 of 1,520 converting artists converted
exactly once, max 3 — the >3 median-eras kill is far away.

**Gate status:** mean 312.2 passes ≤320; seed 1001 alone breaches it by 2.1; the 340 kill is
untouched. No genre approaches the 6.0 per-genre kill on either seed.

### Method notes this run bought

- **The paired control is not optional, and it needs one per seed.** A delta is
  treatment-minus-control *in the same world*; comparing across seeds measures world variance.
- **The >4.0 per-genre gate is below the noise for several genres.** Two *controls* on
  identical code differ by BossaNova 6.09, Comedy 4.81, EasyListening 4.51, TraditionalPop
  4.48. Absolute per-genre numbers at that scale say nothing; only a paired delta that
  **replicates across seeds** does.
- **The control has drifted +7.4 since the bundle** — 309.5/307.6 against the recorded
  `ctl-supply-1001` 302.1 — from work outside this branch that had never been decade-run. Read
  a treatment against a *contemporaneous* control, never against a recorded scalar.
- **SimLogs was empty at the start of this session.** `mix8-decade`, `bundle-1001`,
  `ctl-supply-1001` and `evo11` are all gone, which is why the baselines had to be regenerated.
  Archive the CSVs of any run whose number gets quoted in a directive.
- **Wall clock is unmeasured.** All four runs shared the machine; the >10% timing kill needs a
  solo run.

## 7e. ADJACENT FINDING — the chart has stars and no celebrities

Not part of this directive's scope; recorded here because the evolution telemetry is what
exposed it and the cultural ledger is half the fix.

**The star careers already exist.** Joining `lifecycles.csv` to `artist-project-identity.csv`
on seed 1001:

```
artist_04047   19 top-40   10 #1s   32 weeks at #1   charted 1961-1968
artist_00675   15 top-40    9 #1s   24 weeks at #1   charted 1960-1968
artist_00721   15 top-40    5 #1s   15 weeks at #1   charted 1960-1968
artist_00282   14 top-40    1 #1    19 entries       charted 1960-1969, all ten years
```

3,120 artists chart; 92 reach Star or Superstar. `artist_04047` is also the **8th-ranked
tastemaker** in the influence ledger (69 conversions cite it), so chart stardom and cultural
influence already land on the same act — the hard half works. **The recorded claim that no
runtime artist ever reaches Star is stale and retracted.**

**What is missing is that fame is an output, not an input.** `ChartManager.cs:780` sets
`record.launchCareerState` from the artist. Every reader: `RecordRuntimeData` declares it and
`ChartAuditRunner` writes it to three CSVs. **Nothing in the sim consumes it** — the same
defect shape as `criticalAcclaim` before Phase 3.

The line above it is the actual launch:

```
data.awareness = (0.15f + rand(0.05,0.15)) * campaignImpact * regionStrength;
```

Label campaign, region strength, dice. The artist contributes nothing. A superstar's twelfth
single arrives at radio exactly as anonymous as an unknown's debut, and every star in that
table got there on repeated quality draws plus label promo — not on accumulated public
recognition. That's why the top is thinner than history: the Beatles had ~20 #1s in the decade,
our best act has 10, and nothing compounds.

What that means for "seeing the Beatles on the chart":

1. **Fame has to become an input.** A recognition stock on the artist — decaying, fed by chart
   results, landmarks, and the cultural ledger you already built — feeding launch awareness,
   initial stock, and radio interest. The plumbing is half-built: `stageName` exists, naming v2
   fills it, `CulturalMemoryService` and the era summaries already give an act a biography, and
   `launchCareerState` is a wire that runs to the chart and terminates.
2. **The middle of the ladder is thin.** 93.95% of acts never leave NewSigning; 942 reach
   Rising, 292 Established, 92 Star+. The Temptations tier — a working act with three or four
   hits that you recognize without it being iconic — is the band that barely exists.
3. **Presentation is genuinely last.** The chart is record-facing; the act behind it has a
   discography service, an era history, and a name, none of which the chart surfaces.

**Storytelling is bounded by career length**, and that bound is structural — ratification reads
a window of projects, so a two-record act cannot have an arc:

| projects released | artists | with a pivot |
|---|---|---|
| 1–2 | 4,833 | 0% |
| 3 | 2,174 | 1.3% |
| 4–5 | 8,752 | 4.9% |
| 6–9 | 5,434 | 15.9% |
| 10+ | 713 | 27.5% |

**One caution on (1):** fame-as-input is rich-get-richer, and chart slot-weeks are a fixed
52,100 ([[chart-slot-weeks-identity]]). A recognition term will concentrate the chart and it
will come out of breadth, so it wants its own A/B against the genre-slot metrics — not a
bundled ride-along on someone else's phase.

## 8. Validation protocol

Non-negotiable mechanics, learned the hard way:

- **Build `-c Debug` before every headless run.** Godot headless loads `bin/Debug`;
  `ExportDebug` is ignored and you will silently run stale code.
- **Canonical decade run** — anything less is not comparable:

```bash
"C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe" --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=<name> --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --enable-artist-evolution
```

- Omitting `--enable-genre-market-v2` or `--enable-artist-population-lifecycle` yields a
  ~2× smaller sim in ~7 minutes with non-comparable metrics. Full telemetry decade is ~1 hr.
- **Noise floor.** Single-seed decade A/B churns ~32.7 pts on genres that are unreachable
  anyway; big pop genres swing ±3–4 (max 13) between seeds from vacuum-filling alone.
  **Any claimed delta under ~50 pts on an unreachable genre, or under ~4 pts on a big pop
  genre, needs 2–3 seeds (1001 / 2002 / third) before it means anything.** Gate 1's
  ±11 total-error band is a *reachable-genre* band; read it with `target_check.py`
  per-genre, not off the total alone.
- Tools are committed at `SimTools/radio-compare/`:
  - `target_check.py <run> [--genres A,B,C]` — absolute per-genre share vs targets. Primary gate tool.
  - `genre_shift.py <runA> <runB>` — per-genre per-year delta vs a baseline run.
  - `returns_no1.py <run>` — chart-health metric; needs full telemetry (a `--calibration`
    run empties `weeks.csv`).
  - Baselines: `mix8-decade` (current head, seed 1001), `v31-1001` (V3.1 frozen).
- **New probes required**, in `SimTools/ArtistPopulationLifecycleProbeSuite.cs` style:
  ratification fires on a full majority window; does **not** fire without adjacency;
  does **not** fire inside cooldown; era close/open is atomic and idempotent;
  `formationPrimaryGenre` survives migration; disabled flag is fully inert;
  `AlbumLegitimacy = 0` reproduces the exogenous ceiling exactly. **Anchor fixtures to
  the constants, never to hard-coded "one under the bar" values** — those silently invert
  when the bar is re-derived.
- **Diagnose from decision telemetry, not annual aggregates.** Three mechanism claims on
  this project reasoned from annual rollups were flatly wrong and one query against
  `release-strategy.csv` settled it. The new `<run>-artist-evolution.csv` exists for
  exactly this: when a genre moves, ask *which conversions moved it*.
- Python only for SimLogs analysis (`Import-Csv` dies above ~50MB):
  `C:\Users\grohl\AppData\Local\Programs\Python\Python314\python.exe`, `PYTHONUTF8=1`.

---

## 9. Open questions for the author

1. **Player-owned artists.** Does evolution apply to the player's roster automatically, or
   is a style change something the player is *asked about* (or can veto/push)? This is a
   design decision with real gameplay weight and it changes the Phase 2 API shape. Default
   assumption if unanswered: AI acts evolve autonomously; player acts surface the pressure
   in the UI and evolve only on player assent. Label pressure as a *player action* is a
   natural Phase 5.
2. **Should `FormationAffinity` split formation from project transition?** §7.5 argues
   yes and it is cheap. It is a change to a calibrated file, so it wants an explicit
   blessing and its own A/B.
3. **Does critical acclaim eventually pay?** Phase 3 keeps it inert on money. Historically
   an acclaimed act got better deals and more studio rope. That's a real economic edge —
   worth having, but a separate directive with its own gates.
4. **Era summaries: templated or generated?** The naming v2 stack (voice/DSL/ontology/
   mood/blend/inflection, 148 tests green) could write these lines properly instead of
   `$"shifted from {from} toward {to}"`. Tempting, and it is the difference between a
   database row and a biography. Also scope creep. Author's call.

---

## Appendix — build order at a glance

| Phase | Ships | Risk | Gate |
|---|---|---|---|
| 0 | Data layer, flag, telemetry, inert | none | byte-identical to mix8 |
| 1 | Eras + identity ratification | **genre makeup** | sumAbsErr ≤ 320; FolkRock does not worsen (headcount gate dropped — see §7a) |
| 2 | Pressure, motive, bounded drift | genre makeup | ±8% project supply vs P1 |
| 3 | criticalAcclaim gets a writer | none (inert on money) | economy byte-comparable |
| 4 | Cohesive-album knock-on | **economy** | LP share band; concept-album shape |
| 5 | Discography-by-era UI, tags | none | visual |

Phase 1 is the whole thesis. If ratification alone puts folk acts into folk rock without
moving the calibrated balance, everything after it is flavor on a sound mechanism. If it
doesn't, stop and re-diagnose before building motive on top of a channel that doesn't carry.

**RESOLVED, and not the way this paragraph expected.** The re-diagnosis happened (§7a): the
folk-rock channel is 56 artists wide and ratification cannot reach it, but the *mechanism*
is sound — 255 conversions a decade, rising 3/yr to 52/yr, with historically right
migrations (DooWop→Soul, RnB→Soul, RockAndRoll→PsychedelicRock) and **no guardrail ever
binding**. So the thesis holds in the general case and fails only on the specific genre it
was motivated by, which is a supply problem filed as §7a. Phase 1 proceeds on the general
result; Folk Rock is tracked, not gated.
