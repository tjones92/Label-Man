# Naming fidelity (branch `naming-fidelity`, off `media-atmosphere`)

User-facing pass on procedurally generated act and label names. Goal: names that read as
1960s-plausible and are **true of the act they name**.

## What was wrong (measured)

From `SimLogs/branch-end-decade-market-1001-musician-recognition.csv` and `-label-directory.csv`
(seed 1001 decade, media-atmosphere):

| Failure | Count |
|---|---|
| Band names containing a first name that names **nobody in the band** | 1,304 of 1,343 (97%) |
| Bands carrying a Roman/number disambiguator ("... II", "(12)") | 340 (11% of bands) |
| Duplicate act names | 291 |
| Labels with a Roman suffix ("Capitol Records III") | 46 |
| Procedural labels wearing a real company name (RCA, Columbia, Parlophone, Starday, HMV...) | 40+ |
| "The the ..." (lexicon entry "the Ozarks" in a "The %noun% Players" frame) | 8 |

Root causes:

1. **Names were drawn blind.** `ArtistManager.GenerateArtist` builds the lineup first, then asked the
   namer for a name without passing it. Every `%firstName%` slot drew a random lexicon name; every
   "Trio", "Brothers", "and His Orchestra" was unchecked. Solo acts skipped the namer entirely
   (`stageName = members[0].FullName`), so the nickname/honorific strategies never ran in game.
2. **Label grammar hard-coded real companies** (`(Columbia|Capitol|RCA|Decca|Mercury) Records`,
   `(Stax|Volt|Chess|Duke|Ace)`, `(Starday|Monument|...)`): five-way alternations exhaust after a
   handful of labels, so the uniqueness loop fell to Roman numerals. Runtime births
   (`LabelGenerator`) drew from a separate prefix list that was also mostly real labels.
3. **Thin people pools.** 96 generic surnames for ~4,800 musicians; the country and Italian lists
   were mostly real stars (Cash, Cline, Twitty, Darin, Avalon); the black first-name list contained
   stage nicknames (Muddy, Pinetop, Reverend, Sonny Boy, Howlin') that became legal names
   ("Reverend Hunter" on bass).
4. **Real acts in the band grammar**: The Who/Move/Cream, The Association/Turtles, 1910 Fruitgum
   Company, The Weavers/Highwaymen, Muddy/Howlin', Maytals/Wailers; group-noun pools also contain
   famous act names (Supremes, Drifters, Miracles).

## Design

* **`Systems/Naming/Core/Lineup.cs`** (Godot-free). `NamingContext.Lineup` carries the act's real
  members, front person first. With a lineup:
  * every person slot (`%firstName#k%`, `%lastName#k%`, grammar `{firstName}` / `{lastName}`) binds
    to member *k* instead of the lexicon;
  * a vocal-group head count (`The %number% ...`, `{writtenNumber}`) is the lineup size, and the new
    `%ensemble%` / `%countWord%` pseudo-slots give Duo/Trio/Quartet... / Two/Three/Four...;
  * `%kinGroup%` only offers Brothers/Sisters/Family to a family act of the right sex;
  * **cast claims** are inferred from each template's literal text (no per-template authoring):
    Duo/Trio/Quartet... fix the size, "The Five ..." fixes the size, Brothers/Boys/Sisters/Girls fix
    the sex, Brothers/Sisters/Family/Twins require kin, "and His/Her" fixes the leader's sex,
    "X and the Ys" (band) needs at least three, a persons-only billing ("X & Y") caps the size at the
    persons named. A template (or a grammar rule, or a `(a|b)` option) whose claim is false of the
    lineup is not eligible. An explicit `"cast"` object in templates.json can add to the inference.
  * a final safety net (`ActNaming`'s Accept) re-checks the finished name, so a claim carried by a
    drawn word ("Quintet" from an ensemble-noun pool) is caught too.
  * no lineup (NameLab tuner, legacy callers) = the old blind behaviour, unchanged.
* **`Systems/Naming/ActNaming.cs`** (Godot-free) holds the act/label/person naming *policy*
  (routing, demographics, family acts, uniqueness, reserved names). `NameGenerator` delegates to it
  and the census harness runs the same class, so the census measures the game's code.
* **Family acts**: `ActNaming.RollFamily` makes ~3-22% of multi-member acts (genre-dependent; country,
  gospel and folk highest, sibling duos ×1.6) kin — members take the front person's surname, copied
  back onto the `Musician`s — and only those acts can be billed "The Hart Brothers".
* **Two-person billing**: duos route 60% to the new `bandName.duo` set (Sam & Dave / Mickey & Sylvia /
  The Everly Brothers shapes).
* **Uniqueness**: exact act names are unique across all genres (they were per-genre); a taken name
  tries billing variants ("Dale and the Rockets", "Dale Hart and the Rockets"; solos get a middle
  initial) before Roman numerals. Loaded worlds reserve their existing act and label names.
* **Labels**: new `label.*` grammar (plus gospel/country/teen/folk/jazz voices) over
  `Data/Naming/lexicon.labels.json` (≈400 period words + founders'-blend syllables, "Kay-Dee
  Records"); `{founder}` / `{hq}` slots so a runtime-founded label can be "Pruitt Records" after its
  founder. Collisions are on the *core* word (Records/Sound/Recording Co. stripped), and
  `Data/Naming/reserved.json` blocks ~230 real labels and ~300 famous acts of the period; the
  historical label templates are reserved at world build.
* **`ActNamePolish`**: collapses doubled articles; act and label names only.

## Calibration note — this branch is a one-time re-baseline, not inert

* Album titles and label names moved off the song-title stream (`_rng`) onto their own streams
  (`_albumRng`, `_labelRng`). Album templates embed the act's name and the post-processor draws per
  word, so renaming acts used to change how many draws album titles took and therefore every track
  title hashed into hook/production/danceability after it. That coupling is gone.
* **But act, member and label names are still a simulation input**: the label release programme keys
  its rolls on a semantic identity built from names (`LabelReleaseProgramme.LabelIdentity`:
  `labelName|founderName|...`; `ProgrammeClock.Identity`: `stageName|homeRegion|member FullNames`).
  That is deliberate (ids are sequence counters, see `keyed-rng-ids-are-sequence-counters`), and
  it means any naming change reshuffles programme rolls the way a seed change does. Treat a decade
  A/B of this branch as a re-baseline (like V3.1), not as a measured effect.

## Verification

* `SimTools/NamingCensus` — `dotnet build -c Release` there, then run the exe (copy it out of the
  repo dir if AppControl blocks it) with `LABELMAN_NAMING_DIR=<repo>/Data/Naming`, args
  `acts labels seed [--samples]`. Seeds 1001 and 2002, 7,000 acts / 1,100 labels:

  | | before (decade logs) | after (census) |
  |---|---|---|
  | names naming a non-member | 97% | 2.7-2.9%, all legitimate stage forms (Jerry→Gerald, "Sister Mable Scott", "Little Dee Dee and the Dukes") — the detector counts diminutives/honorifics |
  | false head-count / Brothers-Sisters words | (unmeasured, common) | 0 / 0 |
  | doubled article | 8 | 0 |
  | Roman/number suffix (acts / labels) | 340 / 46 | 0 / 0 |
  | exact duplicate act names | 291 | 0 |
  | labels reusing a historical name | 40+ | 0 |

* `SimTools/NamingCoreTests` — 11 new lineup-binding assertions pass (159/161; the 2 failures,
  Blues/BritishBlues nickname inheritance, fail identically on the base commit).

## Not done

* Lineup churn: when a named front person leaves, the act keeps the name (historically normal;
  a rename event is a possible follow-up).
* `GenerateMembers` has no `ArtistType.Trio` case, so a Trio-typed act has no members and is named
  blind (pre-existing).

# Song and album titles: census + grammar layer (2026-11-04)

Second pass, on titles. Audit evidence (seed-2002 decade, 4,235 records): only 2,598 distinct titles,
"I Miss You" x22; title shapes far from the real chart; word-salad from slot filling that knew a word's
mood and topic but not how it can be used.

## What was built

* **`Systems/Naming/TitleNaming.cs`** (Godot-free): song/catalog/keyed/album title policy moved out of
  `NameGenerator` (which now delegates), so the census runs the game's own code path.
* **`SimTools/TitleCensus`**: `songs albums seed [--legacy] [--samples] [--templates] [--no-world]`.
  Genre mix of the seed-2002 decade run; reports reuse, title shape against the real 1960-64 Hot 100
  (`SimLogs/historical-hot100-1960-64`, found by walking up from the binary), twelve grammar lints with
  examples, real hit titles reproduced, album-title space per genre, and (`--templates`) every template
  that fails >20% of fills. `--legacy` switches off everything below and is the baseline. Output also goes
  to `SimLogs/title-census-{legacy|current}-{seed}.txt`.
* **Grammar layer, `Systems/Naming/Core/Selection.cs` + `Data/Naming/features.json`**: grammatical features
  per word as `g*` tags (person-object verbs, abstract / mass / proper / adverbial / exclamation / gerund /
  plural / path nouns; "the X" words and bare place names tagged automatically). `TemplateEngine.Fill`
  checks each song/album slot against the text around it (like the act-name cast claims, inferred from
  the template's literal text, no per-template authoring) and redraws a word that does not fit (8 tries,
  then the attempt fails; a final whole-title pass catches constraint rerolls):
  * an object pronoun after a verb needs a person-object verb ("You Sing Me" no, "You Hold Me" yes);
  * "the" never precedes a proper name, adverb, exclamation or gerund ("Down the Tijuana", "the Always");
  * a spatial "the" (down/under/beyond/beside/on...) rejects an abstract object ("Mercy Down the
    Redemption"), and "down/up/along the" needs a path ("Down the Highway", not "Down the Vacation");
  * an abstract noun is not placed beside/under/on something ("Honor Beside the Barn");
  * a bare preposition before a singular count noun is refused ("Songs of Shack", "Live at Alley");
  * a word followed by more capitalised noun phrase is a modifier and skips the article rules
    ("at the Newport Jazz Festival");
  * no plural for already-plural, mass, abstract, proper or "the X" words ("Blue Jeanses", "Sweetnesses").
* **Post-processing**: -ing is only dropped where it is a suffix ("Sprin'", "Thin'" gone); "?" only on a
  question; no "!"/"?" on titles built around a bound name (act, lead single). `TitlePolish`: doubled
  articles collapse; the verb after a plural act agrees ("The Calloways Sing"), with "Their"/"the" to
  match; an act's article is lower case mid-title ("Here's the Dean Brothers").
* **World-wide soft uniqueness**: `NameEngine.UniqueOptions.SoftBucket` — a song title already used by any
  act is avoided for the first 24 of 30 draws (exact uniqueness stays per act / per catalog genre).
* **Real titles**: 17 verbatim hit titles removed from the pools (It's My Party, Runaround Sue, Peggy Sue,
  Be My Baby, Puppy Love, 634-5789, the Motown clauses...), and `reserved.json` gains a 226-title `songs`
  list that `TitleNaming` rejects (letters-only match, with or without a parenthetical). Ordinary phrases
  many records shared ("I Love You", "Maybe", a bare first name) are deliberately not reserved.
* Authoring bug fixed in passing: `alb_rnr_party` had a word bound of [7,8] on a five-word pattern (never fired).

## Result (TitleCensus, 4,300 songs / 1,500 albums)

| | legacy (1001) | current 1001 / 2002 / 3003 |
|---|---|---|
| distinct song titles | 56.1% | 96.9% / 98.0% / 97.0% |
| songs sharing a title | 56.8% (worst x27) | 5.5% / 3.8% / 5.4% (worst x4-7) |
| grammar lint hits per 1,000 titles | 110.2 | 0.9 / 1.0 / 0.9 |
| real 1960-64 hit titles reproduced | 4.6% (Runaround Sue x10, It's My Party x7...) | 0.7% (generic phrases only) |
| album shapes (act + single masked) | 30.7% | 33-36% |

`NamingCoreTests`: 18 new title-selection assertions pass (177/179; the same 2 pre-existing failures).
`NamingCensus` (acts) is essentially unchanged: non-member names 2.7% -> 2.4%, Roman suffixes 2 -> 4 of 7,000
(act draws reshuffle because -ing dropping changed for every name).

## What this does NOT fix — the template stage

* **Title shape.** World uniqueness exposes that the pronoun frames ("I %verb% You", "You %verb% Me",
  "Don't %verb% Me") are a few dozen titles carrying ~13% of the song weight: once spent, draws fall to the
  big-space "Adjective Noun" templates. Two-capitalised-word titles go 24% -> 31% (Hot 100: 17%);
  parentheticals stay ~0.5% (8.8%), questions ~0.5% (4.2%), five+ words ~11% (26.7%). Proof it is space,
  not selection: `--no-world` restores the pronoun share to 12.7% along with x25 repeats.
* **Albums** are still ~35% distinct shapes; a fifth are named after the single and a fifth self-titled,
  and the genre sets have 2-4 templates. "Live at %noun%" can still draw "Downtown"/"Asphalt".
* **Thin genre sets** (folk, psych, surf, gospel, latin, hardRock: 2-3 song templates) and semantic oddities
  the rules do not see ("Odyssey Under the Ranch", "Strip Spring", "My Maybe").

## Calibration

A one-time re-baseline, like the act-name pass: every title changes, and titles feed
`GetDeterministicTrackTraits` (hook / production / danceability) and so which album tracks become singles.
Not verified in a Godot run; the census runs the same `TitleNaming` class the adapter calls, and the game
project builds.
