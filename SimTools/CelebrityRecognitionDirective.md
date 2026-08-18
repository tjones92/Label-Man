# Celebrity / Artist Recognition Directive

Turns the adjacent finding in `ArtistEvolutionDirective.md` §7e ("the chart has stars and no
celebrities") into an implementation plan. Opens a new branch off the deepened-musician work.
This directive is scoped to the **act-level recognition stock** and its launch and write-back
plumbing. The person-level musician layer is addressed in §8, and most of it is **blocked** —
read that section before promising anyone a Beatles breakup.

The two attached sketches (`celebrityv1.doc`, `celebrityv2.doc`) are good on structure and wrong
on the seam. This directive keeps their model — a decaying recognition stock, diminishing-returns
gains at milestones, cultural standing separated from mass recognition — and re-grounds every
integration point against the code that actually runs.

---

## 0. The premise correction that changes the whole plan

§7e says "fame is an output, not an input… the artist contributes nothing… a superstar's twelfth
single arrives at radio exactly as anonymous as an unknown's debut." **That is not what the code
does, and building on it would double-count fame.**

The line §7e quotes —

```
data.awareness = (0.15f + rand(0.05,0.15)) * campaignImpact * regionStrength;   // ChartManager.cs:765
```

— lives in `ChartManager.PromoteRecordAI`, which the **canonical AI release path does not call.**
`ReleaseRecord(record, releasingLabel)` only runs `PromoteRecordAI` when a non-null label is
passed for a non-player record (`ChartManager.cs:692/718`). The bulk singles path passes **no
label**:

```
CompetitorManager.TryReleaseRecord            (CompetitorManager.cs:1572)
  → ChartManager.ReleaseRecord(record)        (1646, null label → PromoteRecordAI SKIPPED)
  → ApplyReleasePromotion(record, artist, …)  (1655, this authors the real launch)
```

`ApplyReleasePromotion` (`CompetitorManager.cs:3311`) already makes fame an input:

- **Awareness** reads `artist.GetNewReleaseAwarenessBonus()` (`3317`), which is
  `momentum·0.5 + reputation·0.3 + careerState∈{Superstar .25 … Rising .04, else 0}`
  (`SimulatedArtist.cs:323`).
- **Initial stock** is scaled by a discrete careerState switch (`3329`):
  **Superstar 2.5×, Star 2.0×, Established 1.5×, Rising 1.2×, else 1.0×.**
- **radioHeat** gets *no* fame term (`quality·0.3 + push + payola`, `3323`).

The album pipeline is the same shape: `ProcessDueAlbumProjects → ApplyPromotionSnapshot`
(`CompetitorManager.cs:3217`) reuses `snapshot.artistAwareness = GetNewReleaseAwarenessBonus()`
(`3203`) and an identical `careerStockScale` 2.5× switch (`3226`).

So the true state of the world is:

**Fame is already a launch input, but a *discrete, careerState-gated* one that is inert for ~94%
of the population and does not compound.** `careerState` reaches Rising+ for only ~6% of acts
(942 Rising, 292 Established, 92 Star+ of 3,120 that chart; 93.95% never leave NewSigning), so the
2.5× stock lever and the awareness bump touch the top of the chart and nothing else. There is no
memory: an act that *was* a Star but slid to `Declining` reverts to the 1.0× default the same week,
and a one-hit act that never crosses `Rising` carries nothing forward. `launchCareerState` on the
record (`ChartManager.cs:780`) genuinely *is* a dead wire — it is written to CSV and read by
nothing — but the **live careerState reads in the promotion paths are not dead**, and they are the
thing recognition must replace.

**The design consequence.** A continuous `publicRecognition` stock must **subsume** the discrete
`stockScale` switch and the careerState term inside `GetNewReleaseAwarenessBonus`, not stack on
top of them. If it stacks, a Superstar's launch is (2.5× stock)·(recognition stock bonus) and the
top of the chart detonates. The correct move is: recognition *is* the continuous replacement for
the careerState ladder as a launch input; the ladder stays only as the UI/policy label §7e already
calls it.

This also relocates the fix. The GPT sketches edit `PromoteRecordAI:765`. **Author recognition in
`ApplyReleasePromotion` and `ApplyPromotionSnapshot` first** (the paths that run), and touch
`PromoteRecordAI` and the player/`AlbumSimulator` paths only for parity so no release route is left
on the old discrete lever.

---

## 1. What already exists (do not rebuild it)

| Sketch proposes | Reality | Action |
|---|---|---|
| new `Musician` class | `Data/Musician.cs` exists — a subset of v1's fields, with `GetOverallTalent/GetDramaRisk/WouldConsiderSoloCareer` | **extend**, don't replace |
| `members` on the act | `SimulatedArtist.members` exists and feeds `RecalculateStats` | reuse |
| `currentVisibility` field | `SimulatedArtist.momentum` already is short-lived heat and already feeds launch awareness | **do not add** `currentVisibility`; reuse `momentum` |
| milestone credit hooks | `ArtistManager.OnRecordChartUpdated` (`:179`) already latches ChartEntry/Top40/Top10/#1 with per-record flags | add recognition calls beside the existing `Register*` calls |
| run-complete hook | `ArtistManager.OnRecordLeftChart` (`:235`) → `CompleteChartRun`; and `RosterManager.RecordChartRunComplete` → `RunCulturalReads` (`:1135`) | add `OnChartRunComplete` beside these |
| landmark writer | `AlbumLegitimacyService.TryPublishLandmark` (`:268`) computes `strength` and calls `CulturalMemoryService.Publish` | culturalStanding writer hangs here |
| influence ledger | `CulturalMemoryService` + `CulturalRecognitionService` exist; `RunCulturalReads` already routes both album landmarks and single breakthrough hits into it | culturalStanding writer hangs here |
| per-record launch audit | `RecordRuntimeData` already has `initialLaunchAwareness`, `initialLaunchStock`, `launchCareerState`, `perceivedQualityMultiplier`; and the credit flags | add recognition fields alongside |
| feature-flag scaffold | `ArtistEvolution` (`Systems/ArtistEvolution.cs`) is the model: `Configure`, `CaptureSwitches`, `RestoreSwitches`, `ConfigureForProbe`, `ResetForProbe`, an `Observing` superset flag, a dedicated `RngNamespace` | **copy this pattern exactly** |

Greenfield confirmed: no `publicRecognition` / `culturalStanding` / `personalRecognition`
identifier exists anywhere in the tree today.

---

## 2. The model

Two stocks on `SimulatedArtist`, both `[0,1]`, both **starting at ~0** (never seeded high — a
high-`stagePresence` newcomer has star *potential*, not public awareness):

```csharp
// SimulatedArtist.cs
public float publicRecognition;   // mass familiarity with the act's name; the launch input
public float culturalStanding;    // slow legacy: landmarks, influence conversions, repeated majors
public float recognitionAtLastRelease;   // telemetry snapshot (proves causation, not correlation)
public int   recognitionLastUpdatedWeek = -1;  // idempotent weekly decay guard
```

`momentum` stays as the third, short-lived "hot right now" term — do **not** add `currentVisibility`.

**Effective launch value** — cultural standing is not mass awareness, so it contributes a fraction:

```csharp
float effectiveRecognition = Mathf.Clamp01(publicRecognition + culturalStanding * 0.20f);
```

**Diminishing returns on every gain** (the anti-monopoly core; a #1 does not mint a permanent
franchise):

```csharp
recognition = Mathf.Clamp01(recognition + rawGain * (1f - recognition));
```

**Two decay rates, on the calendar, not per-release** (a legacy act fades from the mass market even
if it never releases again — this is the whole point of a stock that is not `careerState`):

```csharp
publicRecognition *= 0.9975f;   // ~half-life 277 weeks ≈ 5.3y  — placeholder, calibrated in Phase A
culturalStanding  *= 0.9992f;   // ~half-life 866 weeks — standing is nearly permanent
```

Constants are placeholders; the **structure** is the commitment: milestone gains, bounded
run-complete gain, diminishing returns, slow calendar decay, standing separated from recognition.

---

## 3. Launch integration — a bounded, quality-gated, auditable profile

Compute one profile per release, **before any release-facing RNG draw**, store it on the record,
and consume it at the three launch inputs. This mirrors the sketch's
`ArtistRecognitionLaunchProfile` — keep that struct — with the seam corrected.

```csharp
public static class ArtistRecognitionService {
    // Feature-flag / config-owned for A/B. Ceilings are intentionally modest: recognition
    // improves the odds people hear about and can find a release; it never overrides
    // campaign, distribution, genre fit, and record quality combined.
    public const float MaxAwarenessLift = 0.115f;
    public const float MaxStockBonus    = 0.30f;   // NOTE: replaces, not augments, the 2.5× switch
    public const float MaxRadioLift     = 0.075f;

    // A poor record gets less benefit from name recognition: fame gets it sampled,
    // the record still has to hold listeners.
    static float QualityGate(float q) => Mathf.Lerp(0.35f, 1f, Mathf.Clamp01(q));

    public static ArtistRecognitionLaunchProfile GetLaunchProfile(SimulatedArtist a, Record r) { … }
}
```

**§3 is where the double-count risk lives.** In `ApplyReleasePromotion` and
`ApplyPromotionSnapshot`, when `ArtistRecognition.Enabled`:

1. **Delete the discrete `stockScale`/`careerStockScale` careerState switch** and replace the
   multiplier with `1f + commercialRecognition * MaxStockBonus`. The 2.5× Superstar case becomes a
   ~1.3× cap that any act can climb to continuously, not a cliff five acts stand on.
2. **Replace the careerState term inside the awareness read.** Either give
   `GetNewReleaseAwarenessBonus` a recognition-aware branch under the flag, or compute launch
   awareness as `base + momentum·0.5 + reputation·0.3 + AwarenessLift` and drop the careerState
   arm. Keep `momentum`/`reputation` — they are the "hot now" and "quality track record" terms and
   are orthogonal to durable recognition.
3. **radioHeat** gains an additive `RadioLift`, weaker than awareness and divided by
   `radioDifficulty` like the rest of the radio launch, because programmers already have campaign,
   genre, and sales-support systems. Singles only; leave albums' radio path alone.

Region strength stays authoritative in all three: fame is applied as `lift * Mathf.Lerp(0.45f, 1f,
regionStrength)`, so a known act has some pull in a weak market but a local label cannot turn a
past hit into national awareness. **Recognition multiplies a label's available launch capacity; it
never invents distribution the label does not have.**

Do the same substitution in `PromoteRecordAI` (the `ChartManager.cs:765` line the sketches target)
and the player/`AlbumSimulator` release paths, so that with the flag on, **no release route is left
reading `careerState` as a launch input.** With the flag off, every path is byte-identical to today.

### 3.1 Record-level audit fields

```csharp
// RecordRuntimeData.cs — beside the existing launchCareerState/initialLaunch* block
public float launchArtistRecognition;
public float launchCulturalStanding;
public float launchEffectiveRecognition;
public float launchRecognitionAwarenessLift;
public float launchRecognitionStockMultiplier = 1f;
public float launchRecognitionRadioLift;
```

Keep `launchCareerState` — it becomes the *control* column the A/B reads recognition against.
Emit the new fields from the single-release lane writer (`ChartAuditRunner.cs:2147`) and the
records CSV, right next to `launchCareerState`.

---

## 4. Write-back — recognition earned from outcomes, at the existing seams

All of this is additive at seams that already fire exactly once per milestone per record. **Do not
add a weekly gain for merely occupying a chart slot** — that is the rich-get-richer failure mode in
its purest form, and the flags to prevent double-credit already exist.

**Milestones** — in `ArtistManager.OnRecordChartUpdated` (`:179`), beside each existing `Register*`
call, gated on the same `!record.artist*Credited` latch:

```
OnChartEntry  +0.007      OnTop40  +0.022      OnTop10  +0.045      OnNumberOne  +0.085
```

**Run completion** — in `ArtistManager.OnRecordLeftChart` (`:235`), beside `CompleteChartRun`,
guarded by the existing `artistChartRunCompleted` latch. Rewards sustained, broad success once,
from facts already on the record (`peakPosition`, `weeksOnChart`, `weeksInTopTen`,
`regionalBreakoutCount`) — so it cannot double-count the weekly milestones. This is where the
Temptations tier is built: three or four solid runs accumulate into durable recognition without any
single one being iconic.

**Cultural standing** — in `RunCulturalReads` (`RosterManager.cs:1135`), which already runs exactly
once per record whichever completion path arrives first and already routes both album landmarks and
single breakthrough hits. Write standing (not recognition, and never a chart point) from:
- landmark strength at `AlbumLegitimacyService.TryPublishLandmark` (`:290`, the `strength` it
  already computes) → `AddCulturalStanding(0.12·strength)` + a smaller `AddPublicRecognition(0.025·strength)`;
- influence-ledger conversions at the `CulturalMemoryService` sink → `AddCulturalStanding(0.008·strength)` on the source act.

**Calendar decay** — one idempotent weekly pass over the registry, guarded by
`recognitionLastUpdatedWeek == week`, in the live weekly path. Give it **its own flag**, not
`ArtistPopulationLifecycle` — recognition is an A/B-sensitive economy change and needs a clean
control (see §6).

---

## 5. Feature flag — copy `ArtistEvolution` exactly, including observe-only

`§7e`'s own caution is the governing constraint: *"fame-as-input is rich-get-richer, and chart
slot-weeks are a fixed 52,100 ([[chart-slot-weeks-identity]]). A recognition term will concentrate
the chart and it will come out of breadth, so it wants its own A/B against the genre-slot metrics —
not a bundled ride-along on someone else's phase."*

`ArtistEvolution` already solved this exact problem with **observation separated from
ratification** — a mode where the counterfactual is measured and emitted but nothing is written,
"the measurement that sizes the channel before it is allowed to carry anything." Recognition gets
the same:

```csharp
public static class ArtistRecognition {
    public static bool Enabled   { get; }   // stock is written AND consumed at launch
    public static bool Observing { get; }    // stock is written and logged; launch consumes NOTHING
    // Configure(--enable-artist-recognition / --observe-artist-recognition / --disable-…)
    // requires ArtistPopulationLifecycle + GenreMarketV2 live, like evolution.
    // CaptureSwitches / RestoreSwitches / ConfigureForProbe / ResetForProbe; own RngNamespace.
}
```

**Observe-only runs the entire write-back and decay of §4 and emits recognition trajectories, but
launches exactly as the control does.** That is how you confirm recognition *trajectories* look
plausible (a Superstar climbing to ~0.8, a one-hit act peaking then decaying, a cult act with high
standing and low recognition) **before** any of it touches a launch and moves a slot.

---

## 6. The A/B — this is the phase, not a ride-along

Recognition **must not ship bundled** with anything. Its A/B is against breadth and genre-slot
metrics, not just "did the top act reach 20 #1s." Canonical decade run
([[canonical-decade-run-flags]]): `--enable-genre-market-v2 --enable-artist-population-lifecycle`,
plus the recognition flag, one paired control **per seed** (treatment−control in the *same* world;
never against a recorded scalar — the control has drifted before, `ArtistEvolutionDirective.md`
§7d.5), on ≥2 seeds because the per-genre noise floor is 4–6 pts ([[single-seed-decade-ab-noise-floor]]).

Report, per year, control vs treatment:

- unique artists charting; unique reaching Top 40 / Top 10 / #1
- **chart-slot-weeks by `launchCareerState`** and **by launch-recognition decile** (the fixed
  52,100 total is the conservation law — watch where it moves)
- concentration: top 1% / 5% / 10% of acts' share of slot-weeks
- **count of 3–5-hit Established, 6–10-hit Star, 10+-hit Superstar acts** (the §7e target: deepen
  the middle and top)
- genre slot shares and label-tier slot shares vs control (the breadth the concentration comes out of)
- regional-breakout→national-chart conversion rate

**Success is not "the Beatles got 20 #1s." It is "recognition deepened careers while preserving the
number of viable acts and the genre/label pathways."** If breadth craters, the §11-of-the-sketch
ordering is right: pull **stock and radio** effects down before awareness — awareness is the most
historically defensible celebrity benefit; inventory confidence and programmer confidence are where
feedback turns explosive.

**Kill criteria** (any one fails the phase):
- top-10% slot-week share rises more than ~3–4 pts over control
- unique-artists-charting per year falls outside control noise
- any unbenchmarked genre absorbs slots the way LatinPop did ([[unbenchmarked-genres-are-the-guard-sink]])
- genre share-error (the standing mix8 metric) regresses beyond the two-control noise band

---

## 7. Rollout order

The sketch's A→E staging is right; the seam and the blocked ending are what change.

- **Phase A — stock only, observe-only.** Add fields, write-back (§4), decay, telemetry. Launch
  consumes nothing (`ArtistRecognition.Observing`). Confirm trajectories are plausible. No economy
  effect, so no A/B needed yet — this is the channel-sizing pass.
- **Phase B — awareness-only launch, A/B.** Flip to `Enabled` but wire **only** `AwarenessLift`;
  stock multiplier fixed at 1.0, radio lift 0. **Simultaneously delete the careerState awareness
  arm** so this is a clean swap, not an addition. The cleanest causal test.
- **Phase C — replace the stock switch.** Turn on `StockMultiplier` **and in the same commit delete
  the discrete 2.5× `careerStockScale`/`stockScale` switches** in `ApplyReleasePromotion` /
  `ApplyPromotionSnapshot` / `ApplyReleasePromotion`(4211) / `PromoteRecordAI`. Verify recognition
  improves *availability* rather than producing stock waste (units stocked in markets the record
  never sells through).
- **Phase D — small radio confidence.** `RadioLift`, singles only, smaller than awareness. Audit
  whether radio now over-amplifies recognized acts ([[airplay-convexity-amplifies-label-push]] —
  airplay sits inside a high exponent, so a small lift here is not small at the chart).
- **Phase E — musician attribution. BLOCKED for the transfer half. See §8.**

---

## 8. The musician layer — what is buildable and what is not

The sketches' richest material — recognition transferring to a departing singer, a Beatles that
splits into solo careers, a Hendrix whose death guts the act — **has no runtime surface to attach
to.** Verified:

- `RemoveMember` and mid-career lineup replacement are **explicitly deferred** (`Directive6-Codex.md:295`):
  *"RemoveMember and lineup replacement remain deferred rather than being activated as an
  uncalibrated second population system."*
- *"Member replacement, reunions, reactivation/comeback, solo spin-offs… remain out of scope"*
  (`ArtistPopulationLifecycleAudit.md:238`).
- Disbandment (`ArtistManager.cs:1080`) flips **every** member `isActive=false` at once — no
  departure event, no surviving act, no solo formation.
- `Musician.WouldConsiderSoloCareer()` has **zero runtime callers.**

So the departure-transfer mechanic (sketch §10–11, the whole justification for a rich musician
object) **cannot fire** — there are no departures. Do not implement it against an event that never
happens; it would be untested code shaped like a feature.

### 8.1 The v1 `Musician` trait set — triage by live consumer, not by evocativeness

`celebrityv1` proposes a large trait expansion (`emotionalDirectness`, `instrumentalVoice`,
`musicalVision`, `artisticDirection`, `sceneReceptivity`, `mediaInstinct`, `volatility`,
`controversy`, `creativeReputation`, `liveReputation`, `irreplaceability`, `actAssociation`,
`leadership`, `collaboration`, plus `GetCreativeContribution/GetPublicStarPotential/GetRecognitionPortability`).
Every one reads well. But this project's recurring failure is the **declared-and-never-consumed
field** — `criticalAcclaim` ([[criticalacclaim-is-a-dead-field]]), `launchCareerState`, and (as §0
shows) `salience`. The only question that decides whether a trait earns its place is: **what reads
it, and does that reader fire at runtime?** By that test the v1 set sorts three ways.

| Trait(s) | Reader that fires today | Verdict |
|---|---|---|
| role flags, `stagePresence`, `ego`/`ambition`/`loyalty`/`temperament` | `GetDramaRisk` → `ArtistEvolutionService.cs:91` (drama feeds evolution pressure); `ego`/`loyalty`/`temperament` → `groupCohesion` → `CalculateBaseQuality` | **already live** — several already exist; keep |
| `mediaInstinct`, `instrumentalVoice`, `musicalVision`, `emotionalDirectness`, `sceneReceptivity` | attribution weighting (`GetRecognitionShareWeight`, §8.2) once that exists | **add with Phase E-lite** — a live reader arrives in this directive |
| `personalRecognition`, `actAssociation`, `liveReputation` | nothing until presentation (§9) or the blocked transfer | **write-only accumulators** — cheap, make the biography real, but be honest they are inert until §9 |
| `irreplaceability`, `actAssociation`-for-portability | only `GetRecognitionPortability` → the blocked departure transfer | **do not add yet** — dead until lineup dynamics exist |
| `controversy` | see §8.3 | **needs a mechanism, not a field** |
| `creativeReputation` | see §8.3 | **give it a real reader or leave it out** |

### 8.2 Attribution — the one live person-level mechanic (Phase E-lite)

**Attribute** a fraction (~0.45) of each *realized, bounded* act-recognition gain to active members,
weighted by role/visibility (`MusicianRecognitionService.ShareArtistRecognitionGain`, sketch §9).
This is a pure read-down from an event that *does* fire (§4's milestone/run-complete gains), so it is
safe, and it is what writes `liveReputation` (leads/high-`stagePresence`) and `creativeReputation`
(writers/high-`musicalVision`). Gate the person-level fields' allocation behind the recognition flag
exactly as `ArtistEvolutionProfile` is gated (`SimulatedArtist.evolution` is null unless observing),
so an off run allocates nothing across the ~22.5k-artist registry.

### 8.3 `controversy` and `creativeReputation` — promote them from fields to readers

These two are worth calling out because they are the ones most tempting to add as inert fields and
the ones that pay off if given a consumer instead.

**`controversy` is not missing — it exists and is cosmetic.** `Record.controversy` (`Data/Record.cs:33`)
is assigned at generation as `RandRange(0, 0.2)` (`CompetitorManager.cs:2982`, floored for Gospel)
and read only by two UI tag builders (`ReputationTag.Controversial`) and `JournalisticDescriptor`
prose. It moves no unit, no awareness, no radio — the exact dead-wire shape above. It is also the one
fame dynamic the recognition stock **structurally cannot represent**: recognition is monotonic-good
(every gain helps the launch), whereas controversy is *awareness up, gatekeeper support down* — bad
publicity is still publicity, but radio won't touch you. That opposite-sign channel is a genuine,
distinctive feature. It is therefore **its own candidate mechanism**, not a `Musician` field: a
**writer** (what generates controversy — a person-level `volatility` trait crossing an event
threshold, or a policy hook) plus two **readers** at the §3 launch (`+AwarenessLift`, `−radioLift`).
Named here, deliberately out of this directive's phased scope, so it is not smuggled in as another
inert float. It is a good idea; give it a mechanism or leave it alone.

**`creativeReputation`** should not ship as a presentation-only accumulator. Give it a live reader:
a famous writer/auteur (`isPrimaryWriter` or high `musicalVision`) lifts the act's `culturalStanding`
and buys a modest launch benefit *even when they are not the public face* — the George Martin / Brian
Wilson case, where the record launches on the maker's reputation, not the frontperson's. That is a
second, standing-side channel into §3's `effectiveRecognition`, and it makes "the act has a famous
writer" a causal fact rather than a caption. Reasonable to fold into Phase C once the stock switch is
gone.

### 8.4 What stays blocked

Transfer-on-departure, solo-launch carryover, replacement-member recognition loss, reunion. These
are a **prerequisite directive** — a lineup-dynamics system — not a sub-phase of this one. Building it
is a separate uncalibrated population system with its own A/B, and both prior directives deferred it
deliberately. Record this so it is not rediscovered: `[[lineup-churn-never-fires]]`.

---

## 9. Presentation is genuinely last (§7e.3)

The chart is record-facing; the act has a discography service, an era history, and a `stageName`
that naming v2 fills, none of which the chart surfaces. Presentation depends on nothing in this
directive except the stock existing, and it moves no simulation quantity. Do it after the A/B
settles. It is the payoff — "seeing the Beatles on the chart" — but it is the last mile, not the
mechanism.

---

## 10. The one-paragraph version

Fame is *already* a launch input, but a discrete `careerState`-gated one (2.5× stock for the 92
Star+ acts) that is inert for everyone else and never compounds — authored in
`CompetitorManager.ApplyReleasePromotion`/`ApplyPromotionSnapshot`, **not** the
`ChartManager.cs:765` line the sketches edit. Replace that discrete lever with a continuous,
decaying `publicRecognition` stock (plus a slow `culturalStanding`), written at the milestone and
run-complete seams that already fire once per record, consumed at launch as bounded, quality-gated,
region-weighted lifts to awareness/stock/radio. Gate it behind an `ArtistRecognition` flag that
copies `ArtistEvolution`'s observe-then-ratify pattern, size the channel in observe-only first, and
A/B it **alone** against slot-week concentration and genre/label breadth because the chart is a
fixed 52,100 slot-weeks and this term concentrates them. The musician-celebrity transfer story is
blocked — no lineup churn fires at runtime — so ship act-level recognition and member *attribution*,
and leave departures to a future lineup-dynamics directive.
