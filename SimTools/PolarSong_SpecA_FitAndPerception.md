Your instinct is right but the migration is deeper than the UI. Stars aren't in the widget — they're in `GetCraftScore()`. Swapping in a radar while that function still exists just gives you a shape whose _area_ is the star rating, and players will compute area within an hour.

## 1. Where the star system actually lives

Four places, and they all do the same thing:

csharp

```
// SongComposition.GetCraftScore() — a fixed weighted sum. This IS the star rating.
compositionQuality * .30f + melodicStrength * .25f + lyricQuality * .18f
	+ commercialHook * .17f + adaptability * .10f
```

csharp

```
// SampleBest / SampleBestAcross — AI always takes the highest craft score.
float score = s.GetCraftScore() + s.GetFamiliarityForYear(year) * 0.2f;
```

csharp

```
// InterpretationFit — "fit" that doesn't look at the song's character at all.
artist.CalculateBaseQuality() * 0.55f + GenreFit(song, genre) * 0.30f + song.adaptability * 0.15f
```

That third one is the worst offender. **A better artist is currently a better fit for every song in the catalogue.** Genre match is a 3-value step function and `adaptability` is a scalar "works with anyone." There is no way for a song to be _wrong_ for a good act, which means the two cases you named are literally unrepresentable.

And on the player side:

csharp

```
public float ReadHook;      // RepertoireItem
public float ReadQuality;
public float Hook;          // MaterialChoice
```

Two floats. The pad is a star rating with extra words.

## 2. The actual fix: songs carry demands, not qualities

The move is to stop asking _how good is this song_ and start asking _what does this song require, and who is it_. A song stops having a scalar quality and gets two vectors:

- **Demand** — what executing it requires. One-sided: shortfall hurts badly, surplus is mostly wasted.
- **Identity** — what kind of thing it is. Two-sided: distance hurts in _either_ direction.

The act gets the matching pair: **Capability** and **Identity**. Fit is a comparison of vectors, not a sum of attributes. There is no global ranking because the scoring function changes with every act you hold the song against.

That one change makes both of your test cases fall out:

- **How Do You Do It** — zero capability shortfall. The Beatles could play it in their sleep. It fails _entirely_ on identity distance, and the failure mode is refusal, not a bad record.
- **Respect** — massive capability surplus on the axis the song cares about, plus Aretha having enough interpretive reach to _drag the identity vector_ from pleading to demanding.

Keep the existing composition floats (`compositionQuality`, `commercialHook`, etc.) — they're fine as the raw material ceiling and the publishing/settlement layer already reads them. Just stop letting them be the decision.

## 3. The axes

Six capability spokes for the radar, four identity axes for the compass.

csharp

```
/// <summary>What a song requires of whoever cuts it, and what kind of thing it is.
/// Demands are one-sided (shortfall hurts, surplus is wasted). Identity is two-sided
/// (distance hurts in both directions). Derived from the song's taxonomy at mint time;
/// never hand-authored per song.</summary>
[Serializable]
public sealed class SongProfile {
	// --- Demand: the radar's six spokes, 0..1 ---
	public float vocalPower;      // belting, sustained range, projection
	public float vocalNuance;     // phrasing, intimacy, timing behind the beat
	public float musicianship;    // solos, changes, meter shifts, technique
	public float ensemble;        // tightness, arrangement density, section coordination
	public float lyricDelivery;   // word density, diction, narrative sustain
	public float studioCraft;     // production tags, overdub depth, construction

	// --- Identity: 0..1, two-sided ---
	public float toughness;       // 0 sweet/light .. 1 hard/aggressive
	public float sophistication;  // 0 earthy/raw .. 1 polished/literate
	public float sincerity;       // 0 arch/novelty/camp .. 1 earnest
	public float maturity;        // 0 teen .. 1 adult

	// --- Scalars outside the vectors ---
	public float dominantDemand;  // max of the six, cached: how exposed the record is
	public float plasticity;      // how far identity can be moved (archetype-driven)
}
```

The act mirrors it exactly, so comparison is trivial:

csharp

```
[Serializable]
public sealed class ActProfile {
	public float vocalPower, vocalNuance, musicianship, ensemble, lyricDelivery, studioCraft;
	public float toughness, sophistication, sincerity, maturity;
	public float interpretiveReach;   // how far this act can bend a song toward itself
	public float identityRigidity;    // how much a mismatch costs them (ambition-driven)
}
```

`vocalPower` and `vocalNuance` must be separate — that split is the whole crooner/shouter distinction from the `VocalApproach` axis, and without it Sinatra and Wilson Pickett are the same vector with different magnitudes.

## 4. Derive the profile, never author it

Ten floats per song × ~4,000 catalogue songs is not authorable. Derive from the taxonomy you already have — Archetype, LyricMode, VocalApproach, Meter, Production, WordDensity, tags — plus the existing composition floats as a modifier.

csharp

```
public static class SongProfileDeriver {
	// Base demand/identity per archetype. The table is ~30 rows, authored once.
	private static SongProfile Base(SongArchetype a) => a switch {
		SongArchetype.DeepSoulPleader => new SongProfile {
			vocalPower = .85f, vocalNuance = .80f, musicianship = .35f, ensemble = .55f,
			lyricDelivery = .30f, studioCraft = .30f,
			toughness = .55f, sophistication = .35f, sincerity = .95f, maturity = .70f,
			plasticity = .35f },
		SongArchetype.BrightPopNumber => new SongProfile {
			vocalPower = .40f, vocalNuance = .35f, musicianship = .30f, ensemble = .60f,
			lyricDelivery = .25f, studioCraft = .45f,
			toughness = .25f, sophistication = .40f, sincerity = .55f, maturity = .25f,
			plasticity = .70f },
		SongArchetype.LushStandard => new SongProfile {
			vocalPower = .55f, vocalNuance = .90f, musicianship = .25f, ensemble = .70f,
			lyricDelivery = .55f, studioCraft = .50f,
			toughness = .15f, sophistication = .90f, sincerity = .75f, maturity = .90f,
			plasticity = .55f },
		SongArchetype.StomperRocker => new SongProfile {
			vocalPower = .65f, vocalNuance = .20f, musicianship = .40f, ensemble = .70f,
			lyricDelivery = .20f, studioCraft = .20f,
			toughness = .80f, sophistication = .20f, sincerity = .60f, maturity = .35f,
			plasticity = .60f },
		SongArchetype.VerseDrivenSong => new SongProfile {
			vocalPower = .30f, vocalNuance = .55f, musicianship = .25f, ensemble = .30f,
			lyricDelivery = .95f, studioCraft = .20f,
			toughness = .40f, sophistication = .75f, sincerity = .85f, maturity = .70f,
			plasticity = .25f },   // you cannot make Desolation Row into something else
		SongArchetype.SuiteMultiPart => new SongProfile {
			vocalPower = .50f, vocalNuance = .55f, musicianship = .85f, ensemble = .90f,
			lyricDelivery = .55f, studioCraft = .95f,
			toughness = .45f, sophistication = .90f, sincerity = .70f, maturity = .75f,
			plasticity = .15f },
		// ...~24 more
		_ => Neutral()
	};

	public static SongProfile Derive(SongComposition s) {
		SongProfile p = Base(s.archetype);
		ApplyLyricMode(p, s.lyricMode);        // Invective +toughness, Nonsense -sincerity, etc.
		ApplyVocalApproach(p, s.vocalApproach); // Testifying +vocalPower/+nuance, Deadpan -both
		p.lyricDelivery = Mathf.Lerp(p.lyricDelivery, s.wordDensity, 0.45f);
		ApplyProductionTags(p, s.productionTagIds); // tags push studioCraft + ensemble
		// Craft raises what the song can reward, not what it demands: a great
		// bright pop number is still easy to sing.
		p.dominantDemand = Max6(p);
		return p;
	}
}
```

Three things to note. **Craft does not raise demand** — a brilliant simple song stays simple to execute, which is the whole point of the Brill Building. **Plasticity is archetype-level** — a bright pop number can become almost anything, a verse-driven song cannot. And the derivation runs once at mint and caches onto `SongComposition`, so `SampleBest` stays cheap.

For the act, derive from members, keeping it a pure read:

csharp

```
public static ActProfile DeriveAct(SimulatedArtist a) {
	Musician lead = a.GetLeadVocalist();
	var p = new ActProfile {
		vocalPower   = lead?.vocalPower ?? Avg(a, m => m.vocalPower),
		vocalNuance  = lead?.vocalControl ?? Avg(a, m => m.vocalControl),
		musicianship = Best(a, m => m.musicianship),
		ensemble     = a.chemistry * .6f + Avg(a, m => m.musicianship) * .4f,
		lyricDelivery= lead?.diction ?? .4f,
		studioCraft  = 0f,   // filled at session time from producer + studio, NOT the act
		interpretiveReach = Mathf.Clamp(
			a.CalculateBaseQuality() * .35f
			+ (a.evolution?.artisticAmbition ?? .3f) * .35f
			+ a.chemistry * .30f, 0f, 1f),
		identityRigidity = Mathf.Clamp(
			.25f + (a.evolution?.artisticAmbition ?? .3f) * .55f
			+ (a.evolution?.rootsAttachment ?? .3f) * .20f, 0f, 1f)
	};
	a.DeriveIdentityAxes(p);   // from genre history, member traits, scene
	return p;
}
```

`studioCraft` belonging to the session rather than the act is deliberate — it's what makes the producer hire matter, and it's why a garage band plus George Martin can attempt a suite.

## 5. Three fits, never summed

csharp

```
public readonly struct MaterialFit {
	public readonly float Capability;   // can they execute it
	public readonly float Identity;     // is it them
	public readonly float Moment;       // does the market want it now
	public readonly float Deficit;      // worst single shortfall, for UI highlighting
	public readonly SongAxis WorstAxis;
	public readonly float Stretch;      // how far identity had to be dragged
}
```

Capability — shortfall is superlinear, surplus on the dominant axis converts to a small bonus:

csharp

```
private static float CapabilityFit(SongProfile s, ActProfile a, out float worstDeficit, out SongAxis worst) {
	Span<float> demand = stackalloc float[6] { s.vocalPower, s.vocalNuance, s.musicianship, s.ensemble, s.lyricDelivery, s.studioCraft };
	Span<float> have   = stackalloc float[6] { a.vocalPower, a.vocalNuance, a.musicianship, a.ensemble, a.lyricDelivery, a.studioCraft };

	float penalty = 0f; worstDeficit = 0f; worst = SongAxis.VocalPower;
	for (int i = 0; i < 6; i++) {
		// Weight each axis by how much the song leans on it. A song that doesn't
		// ask for musicianship doesn't care that you have none.
		float w = demand[i];
		float d = Mathf.Max(0f, demand[i] - have[i]);
		if (d > worstDeficit) { worstDeficit = d; worst = (SongAxis)i; }
		penalty += w * d * d * 2.2f;        // quadratic: small gaps forgiven, big gaps fatal
	}
	// Headroom on the axis the song is built around reads as effortlessness.
	float headroom = Mathf.Max(0f, Max6Have(have, demand) - s.dominantDemand);
	return Mathf.Clamp(Mathf.Exp(-penalty) + headroom * 0.12f, 0f, 1f);
}
```

Identity — distance _after_ the act has pulled the song toward itself:

csharp

```
private static float IdentityFit(SongProfile s, ActProfile a, out float stretch, out SongProfile heard) {
	float reach = a.interpretiveReach * s.plasticity;
	heard = s.Clone();
	stretch = 0f;
	stretch += Pull(ref heard.toughness,      a.toughness,      reach);
	stretch += Pull(ref heard.sophistication, a.sophistication, reach);
	stretch += Pull(ref heard.sincerity,      a.sincerity,      reach);
	stretch += Pull(ref heard.maturity,       a.maturity,       reach);

	float d2 = Sq(heard.toughness - a.toughness) * 1.25f      // toughness mismatch is the loudest
			 + Sq(heard.sophistication - a.sophistication)
			 + Sq(heard.sincerity - a.sincerity) * 1.15f
			 + Sq(heard.maturity - a.maturity) * 0.85f;
	float dist = Mathf.Sqrt(d2 / 4.25f);
	// A rigid act is punished harder for the same distance than a jobbing act.
	return Mathf.Clamp(1f - dist * (0.70f + 0.60f * a.identityRigidity), 0f, 1f);
}

private static float Pull(ref float songAxis, float actAxis, float reach) {
	float delta = Mathf.Clamp(actAxis - songAxis, -reach, reach);
	songAxis += delta;
	return Mathf.Abs(delta);
}
```

Moment — distance from a per-year, per-demographic taste centroid, which should lag and drift:

csharp

```
public static float MomentFit(SongProfile heard, Genre genre, int year, int chartWeek) {
	MarketTaste t = MarketTasteService.Current(genre, year, chartWeek);
	float d = Mathf.Sqrt((Sq(heard.toughness - t.toughness)
		+ Sq(heard.sophistication - t.sophistication)
		+ Sq(heard.maturity - t.maturity)) / 3f);
	return Mathf.Clamp(1f - d * 1.3f, 0f, 1f);
}
```

**Never expose a weighted sum of the three.** They go into the chart model separately: Capability gates execution quality, Identity gates conviction and critic response, Moment gates commercial ceiling. A player who wants one number has to decide which one they're playing for, and that decision _is_ the game.

## 6. Reinterpretation — the Respect mechanic

`Stretch` is the gameplay. Pulling a familiar song a long way is the highest-variance move available:

csharp

```
public static float ReinterpretationOutcome(MaterialFit fit, SongComposition song, ActProfile act, float familiarity) {
	if (fit.Stretch < 0.15f) return 0f;                  // a straight reading, no bet placed
	float ambition = fit.Stretch * familiarity;          // far + famous = loud, either way
	float landed = fit.Capability * act.interpretiveReach;
	// Below 0.5 you've mangled a song everyone knows; above, you own it.
	return ambition * (landed - 0.5f) * 2f;
}
```

Feed a positive outcome into `definitiveVersionScore` on the new `SongRecordingMemory` and into `CoverFatigueShadow`, so a successful reinterpretation **retires the song from the cover pool** — exactly what Aretha did to _Respect_. Feed a negative one into a critic penalty and no familiarity lift.

This also finally gives `arrangementOriginality` a real source. Replace the noise function:

csharp

```
// Was: record.originality * 0.5f + creativity * 0.3f + hash noise
ArrangementOriginality = fit.Stretch;
```

## 7. Refusal — the How Do You Do It mechanic

csharp

```
/// <summary>An act with standing and ambition will not cut material that isn't them,
/// regardless of how strong the song is. Pure read; no RNG.</summary>
public static bool WillRefuse(MaterialFit fit, SimulatedArtist a, out string reason) {
	float standing = StandingOf(a.careerState);        // NewSigning .15 .. Superstar 1.0
	float threshold = 0.30f + 0.35f * a.evolution.artisticAmbition * standing;
	if (fit.Identity < threshold && fit.Stretch >= fit.Capability * 0.9f) {
		reason = IdentityComplaint(fit);               // "it's too soft for us"
		return true;
	}
	reason = null; return false;
}
```

Note the second clause: they only refuse when they _can't_ bend it to fit. An act with high reach takes the song and makes it theirs instead of refusing. That's the correct branch — the Beatles refused _How Do You Do It_ partly because its plasticity was low in their hands and partly because they had their own material. Make the refusal softer if their songbook is empty.

For the player this must be a dialogue, not a silent score penalty: _"The band listened to the demo twice and put it down. Harrison said it sounded like something for Billy Fury."_ Then: push it anyway (morale hit, conviction penalty, possible lineup churn), take their song instead, or shelve it. Forcing it should sometimes still produce a hit — that's the Gerry and the Pacemakers outcome, and a player should be able to shop a refused song to a different act on the roster.

Replace `ExternalPenalty` with this. It currently reads ambition in the abstract; the refusal check reads ambition _about a specific song_, which is both more accurate and more legible.

## 8. Perception — bands, not values

Everything above is truth. The player never sees it.

csharp

```
/// <summary>The player's read on a song. Each axis is a band, not a value.
/// Width shrinks with the relevant staff skill. Deterministic per (observer, song,
/// observation) so reopening the panel never re-rolls.</summary>
public sealed class ReadProfile {
	public float[] Lo = new float[10];
	public float[] Hi = new float[10];
	public bool CapabilityLegible;   // producer ear: are the demand spokes readable at all
	public bool IdentityLegible;     // A&R ear
	public bool MomentLegible;       // market research
}

public static ReadProfile Read(SongProfile truth, PlayerStaff staff, string observationKey) {
	// Scout skill reads identity well and demand poorly: you can hear what a song IS
	// in a club. You cannot hear what it will NEED in a studio.
	float idErr  = Mathf.Lerp(0.30f, 0.04f, staff.ArAndRear);
	float capErr = Mathf.Lerp(0.42f, 0.07f, staff.ProducerEar);
	var r = new ReadProfile {
		CapabilityLegible = staff.ProducerEar > 0.25f,
		IdentityLegible   = staff.ArAndRear > 0.15f,
		MomentLegible     = staff.MarketResearch > 0.35f
	};
	for (int i = 0; i < 10; i++) {
		float err = i < 6 ? capErr : idErr;
		// Bias, not just width: a weak ear is wrong, not merely vague.
		float bias = (StableUnit(observationKey, $"bias{i}") - 0.5f) * err;
		float c = Mathf.Clamp(truth[i] + bias, 0f, 1f);
		r.Lo[i] = Mathf.Max(0f, c - err * 0.8f);
		r.Hi[i] = Mathf.Min(1f, c + err * 0.8f);
	}
	return r;
}
```

Two rules that matter more than the math:

**Bias, not just width.** If the band is always centred on truth, players average across axes and recover the truth. A weak scout must be _wrong_, consistently and in a direction they can't detect.

**Different staff read different axes.** Scout reads identity. Producer reads demand. A&R reads moment. That turns three hires into three distinct kinds of blindness, and it's why a player can have a great ear for what a band _is_ while repeatedly putting them on material they can't execute.

The read tightens at known gates: seeing the act twice (`FollowedUp`), hearing a demo, a rehearsal, the first take in the room. By playback you have near-truth — and the money's spent.

## 9. Worked examples

**How Do You Do It → the Beatles, late 1962.**

||Song|Act||
|---|---|---|---|
|vocalPower|.40|.62|ok|
|vocalNuance|.35|.55|ok|
|musicianship|.30|.55|ok|
|ensemble|.60|.78|ok|
|lyricDelivery|.25|.45|ok|
|studioCraft|.45|.70 (Martin)|ok|
|**Capability**|||**0.94**|
|toughness|.25|.70||
|sophistication|.40|.45||
|sincerity|.55|.75||
|maturity|.25|.45||

Reach .58 × plasticity .70 = .41, so they can drag toughness .25 → .66 and maturity .25 → .45. Residual distance is small _after_ stretch — but `Stretch` = 0.72, and ambition .78 × standing .15 gives threshold .34 against Identity .61. **They don't refuse on the numbers.** Which is right: they _did_ record it. It took Martin backing down, not the band walking out. Tune `standing` for a brand-new signing and the player gets the real outcome — the band grumbles, cuts a listless take, and you choose between a probable hit that isn't them and _Please Please Me_.

**Respect → Aretha, 1967.** Capability: song wants vocalPower .85, she's at .97 — headroom bonus. Identity: Otis's recording sits at sincerity .90 / toughness .60 / maturity .65; her act profile is toughness .82, maturity .78. Reach .88 × plasticity .55 = .48, so she pulls it most of the way. `Stretch` = 0.44, familiarity 0.58, landed = .93 × .88 = .82. Outcome = .44 × .58 × (.82 − .5) × 2 = **+0.16**, a strong positive — definitive version, song leaves the cover pool, Redding's publishing still pays out. Exactly the round-two story, now computed.

**Worth building a third:** a _Lush standard_ handed to a garage act. Capability fails on `vocalNuance` and `ensemble`; identity fails on sophistication and maturity; plasticity .55 isn't enough and reach is low. The UI should say **"Nobody in this band can sing this"** — a sentence no star rating can produce.

## 10. Integration checklist

|Call site|Change|
|---|---|
|`SongComposition.GetCraftScore()`|Keep for settlement/telemetry only. Add `[Obsolete]` on the selection path so it can't creep back.|
|`SampleBest`, `SampleBestAcross`|`score = Fit.Capability * .35f + Fit.Identity * .45f + Fit.Moment * .20f` — **AI only**, never shown. This is where the historical pattern emerges: Brill's best songs flow to the acts they suit, not to whoever's biggest.|
|`InterpretationFit`|Delete. Replace with `MaterialFit`.|
|`SelectedSongMaterial.ArtistIdentityFit`|Becomes `MaterialFit` (promote from float to struct).|
|`ArrangementOriginality(...)`|Replace hash noise with `fit.Stretch`.|
|`ExternalPenalty(artist)`|Replace with `WillRefuse(fit, artist, out reason)`.|
|`RepertoireItem.ReadHook/ReadQuality`|Replace with `ReadProfile`. Keep the floats as a deprecated fallback for one version so saves survive.|
|`MaterialChoice.Hook`|Replace with `ReadProfile` + `MaterialFit` computed against the selected act.|
|`BuildCoverForSong`|Already takes a specific song — thread `MaterialFit` through instead of `InterpretationFit`.|
|`GenreFit`|Demote to a tiebreaker. Genre is a market/scene fact; identity distance is the real fit now.|

Determinism holds throughout: `SongProfileDeriver` is pure, `MaterialFit` is pure, and perception draws only from `StableUnit` keyed on an observation id. No new RNG streams, no touch of the GD stream. The one thing to watch is that the observation key must include the _reason_ for the observation (`$"{staffId}|{songId}|{venueNight}"`), or re-entering a panel re-rolls the band and players will save-scum the read.

Serialization: `SongProfile` is ten floats on `SongComposition` — add to `CompositionSaveData` with the rest of the song, and derive-on-load for old saves where it's null.

## 11. UI

Primary widget, in the scouting pad and the studio material list: **one radar, two polygons.**

- **Filled polygon** = song demand (banded — draw Lo/Hi as a translucent ring, not a line)
- **Outline polygon** = the act's capability
- **Red where the song pokes outside the act** — that's your deficit, instantly readable with no numbers

Below it, three short bars with no digits: _Execution · Identity · Moment_, drawn as banded ranges. Then one line of generated prose, which is what players will actually read:

> **"Walk Away Renée" — chamber/baroque, 1966**  
> _They can play it. Nobody in this band can sing it._  
> Execution ▓▓▒░░░ · Identity ▓▓▓▓▓░ · Moment ▓▓▓▓░░

Identity gets its own small widget — a 2D plane (tough↔sweet × earthy↔sophisticated) with the act as a dot, the song as a dot, and a **ghost arrow showing where the act would pull it**. That arrow is the single most valuable thing on the screen: it's the player seeing reinterpretation potential before committing. Length of arrow = reach × plasticity. A low-reach act's arrow is a stub and the player immediately understands they must find material that already fits.

Make the whole thing comparative by default — a song panel is meaningless without an act beside it. Dragging a song onto a different act on the roster should redraw everything. That single interaction kills the star-system habit faster than any amount of math, because the player watches the shape change.

## 12. Does this actually resist being solved?

Worth checking honestly, because "radar chart" games often collapse back into "maximise area."

**What holds:**

- No global ranking — the scoring function changes per act, so there's no best song in the catalogue
- Three outputs that trade off, with no exposed sum
- Reads are biased, not just noisy, so averaging doesn't recover truth
- Moment drifts between selection and release, so even perfect reads decay

**What leaks:** a player with maxed staff on a settled roster will converge, and should. That's mastery, and it arrives around hour thirty. Two cheap defences keep it honest — **make moment genuinely unpredictable** (taste centroid moves on a random walk plus shocks like the British Invasion, so even a perfect read on song and act leaves the market unsolved), and **make scarcity bind** (exclusive holds, one commission in flight, rehearsal weeks, a rival label taking the song). Knowing the right song doesn't help if you can't get it this quarter.

The one thing I'd genuinely watch in playtest: **the deficit penalty is quadratic and weighted by demand, so it's easy to make everything feel unplayable.** Start the exponent lower than 2.2 and tune up. The failure mode you want is "this record is flat," not "this record is impossible" — the 60s are full of acts cutting material slightly beyond them and getting away with it.