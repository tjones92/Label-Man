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

## What this did NOT fix — the template stage (addressed in the next section)

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

# Title templates: shape, albums, thin genres (2026-11-04, third pass)

The grammar layer could not fix shape because the problem was space: single-slot frames ("I %verb% You")
are a few dozen titles, so world uniqueness spends them and the draws fall to "Adjective Noun". This pass
rewrites the template stage instead.

## What was built

* **Shared sets + `include`** (`ConstraintTemplateLoader`): `{ "include": "songTitle._loveClauses", "scale": 0.15 }`
  splices scaled copies of a shared set (named `kind._name`, never routed to directly) into a genre set.
  `ConstraintTemplate.Scaled` clones; ids are shared, so the satisfiability cache is too.
* **`songTitle._loveClauses`** (34 templates) over new parts of speech (`clHead`, `clVerb`, `clTail`, `clLead`
  in `lexicon.ontology.json`, kept off `verb`/`noun` so phrasal verbs never reach the grammar or old filters):
  head x verb x object x tail, so the space is thousands ("I Won't Give Up on You (Even Though I Want To)",
  "Will You Love Me When I'm Old", "Why Can't I Get Over You", "Sherry (Don't Leave Me Now)"). Heads and verbs come in
  classes that read together (wanting / longing / refusing / leaving / getting over; you-subject; imperative;
  don't; questions). Tails carry perspective tags (`clMeOk`, `clMeOnly`, `clImpOk`), so "...If You Let Me" never
  follows a "Me" object and "Not in a Million Years" never follows "Don't". About 17% of clause draws are
  parenthetical, as on the real chart. It replaces the single-slot pronoun frames in soul, trad pop, R&R, country,
  doo-wop, teen pop, blues, vocal jazz and British, at 0.1-0.25 scale.
* **`songTitle._extras`**: one-word titles (adjective / noun / adverb pools), name or word plus subtitle
  ("Tina (One More Kiss)", "Tonight (I Need You)"), "Love Is ...". Most real parentheticals are of this
  kind, not clause tails: on the 1960-64 chart, clause titles are 15.6% of titles but only 2.6 points of the 8.8% parentheticals.
* **The early-60s title-name pool had 6 names** (Johnny, Susie, Donna, Diana, Sherry, Bobby), which is why "Donna x5" and
  "Bobby x4" led the repeat list; it now has ~80. The country names lost their 1970s hit titles (Jolene, Delta Dawn, Fancy).
* **Thin genres rewritten from the period's own shapes**, each on its own pools:
  folk ("The Ballad of Nancy Riley", "The Emigrant's Daughter", "On the Banks of the Sweetwater", "Farewell to
  Galway", "Talkin' Picket Line Blues", "If I Had a Banjo"); gospel ("Precious Lord, Don't Pass Me By",
  "Some Glad Morning I'll Wear a Crown", "A Table in My Father's House", "Martha, Don't You Worry", "The Glory
  Chariot"); surf ("Huntington Pier Stomp", "Shoot the Curl"-type, "Surfer Ronnie", "Roadster Rumble", one-word
  instrumentals); psych ("Tangerine Kite", "Plastic Piper", "Juniper Lane", "See Rosemary Grow", "Voyage to the
  Edge of the Mind", "Ten Thousand Light Years Away", "Free Your Mind (Into the Light)"); latin, added because
  BossaNova outweighs psych and surf in the decade mix ("Carnival in Bahia", "Acapulco Cha-Cha", glossed titles
  "Corazón Loco (Crazy Heart)"). The old noun-connector-noun frames ("Odyssey Under the Ranch") are gone from these sets.
* **Albums**: shared `_lpOther` ("<single> and Other Golden Hits", "The Great Ballads of <act>"), `_lpDance`
  ("Mashed Potato Party", "Do the Watusi with <act>"), `_lpMood` ("Holiday in Lisbon", "Songs for Swingin' Lovers",
  "Midnight Moods"), included per genre; new album sets for folk, gospel, surf, psych and latin (BossaNova /
  LatinPop / TexMex had fallen to the generic set). Self-titled template weights halved. The genre-blind
  `albumFormat` grammar path drops from 12% to 7% of albums (TitleSelection on) and loses its objectless frames
  ("X Celebrates", "X Goes") and "X Rocks" for any genre.
* **Fixes found on the way**: the `address` love filter only ever matched "you / the one I love / my baby /
  everybody" ("Kiss Me, Everybody", "My You Won't Come Home"); address slots now use a `vocative` tag, and
  `possessed` keeps "My My Love" out. Parentheticals are title-cased as their own title. No "!" on a question
  (the roll is still made). "Elmer 'n' Ray Sing" (the `'n'` plural test could never match: no `\b` before an
  apostrophe). "and Other" is never "'n' Other". "Get Up Up, Bobby"; "My Heart Is Give Outing".
* `reserved.json` +24 titles the new frames can reach ("Love Me Tender", "Do You Love Me", "Love Is Strange",
  "Itchycoo Park", "See Emily Play", "I'll Fly Away", "Peace in the Valley", "Blame It on the Bossa Nova"...);
  bare first names stay unreserved, as before.
* Census: `--routing` (set per genre), `--dump` (all titles to `SimLogs/title-census-{songs,albums}-<seed>.tsv`),
  and an album split by build (bare single / bare act / act or single in a frame / free). The masked count folds
  every self-titled LP into one shape, so on its own it understates the shelf. The lint accepts words a set's
  own pool writes with "the" ("the Tennessee") and dance names ("Do the Cha-Cha").

## Result (TitleCensus, seeds 1001 / 2002 / 3003)

| | Hot 100 1960-64 | before (1001) | now |
|---|---|---|---|
| parenthetical | 8.8% | 0.4% | 7.4 / 7.9 / 8.1% |
| question opener | 4.2% | 0.5% | 3.7 / 3.6 / 3.4% |
| contains "love" | 9.1% | 3.9% | 7.3 / 7.3 / 7.9% |
| you/your | 13.3% | 4.2% | 14.7 / 15.0 / 14.8% |
| I-opener | 6.7% | 2.1% | 6.4 / 6.1 / 6.5% |
| one word | 9.6% | 2.7% | 7.1 / 7.0 / 6.9% |
| five+ words | 26.7% | 11.0% | 26.7 / 26.5 / 26.1% |
| two capitalised words | 17.2% | 32.4% | 19.1 / 18.4 / 19.1% |
| "blues" | 0.6% | 1.7% | 1.4 / 1.5 / 1.7% |
| distinct song titles | | 96.9% | 100.0% (0-0.1% share a title) |
| album shapes, masked | | 34.0% | 49.9 / 47.9 / 46.9% |
| album named after single / self-titled | | 20.2 / 17.1% | 17.7-20.4 / 11.9-12.5% |
| lint per 1,000 | | 0.9 | 1.2 / 0.7 / 0.9 |
| real hit titles reproduced | | 0.7% | 1.0-1.1% (one-word generics: Forever, Maybe, Tonight) |

`NamingCoreTests` 184/186 (7 new: include splicing and scaling, parenthetical casing, no "!" on questions,
`'n'` plurals; the same 2 pre-existing Blues nickname failures). `NamingCensus` acts unchanged (2.4% non-member,
0-4 Roman suffixes, 0 false claims). The game project builds. `--legacy` lint is now 76.7, not 110.2, because the
legacy switch turns off code, not data, and the data changed.

## Still open

* One-word titles stop at ~7% (9.6%): the pools (~300 words with names) run out under world uniqueness, as
  the pronoun frames used to. "Blues" is ~1.5% because RnB routes to the blues set.
* ~~FolkRock shares the revival folk set; HardRock, reggae, sunshine, bubblegum, garage are 2-6 templates~~ — done, next section.
* Older single-pool oddities remain where nothing replaced them ("Lotuses on the Work", "Vibraphone in Speedoo").
* Calibration: another re-baseline. Titles feed track traits, so every A/B against earlier runs is a seed change.

# Late-decade genre sets: FolkRock, HardRock, sunshine, bubblegum, garage, Caribbean (2026-11-04, fourth pass)

The last thin sets, rewritten the same way as folk/psych/surf: period shapes over their own parts of speech
(129 new pools in `lexicon.ontology.json`, prefixes `fr` `hr` `sn` `bg` `gr` `rg`), so nothing leaks into other sets.

| set | routes | before | now | shapes |
|---|---|---|---|---|
| `songTitle.folkRock` (new) | FolkRock | shared revival folk | 15 + clauses/extras | "Eve of Destruction", "Universal Soldier", "California Dreamin'", "Like a Rolling Stone", "It Ain't Me, Babe", "Summer in the City", "Elusive Butterfly", "Standin' in the Rain", the imperative, the plain line |
| `albumTitle.folkRock` (new) | FolkRock | revival folk | 11 | "Wednesday Morning, 3 A.M.", "Parsley, Sage..." lists, "Songs from Big Sur", one-word LPs |
| `songTitle.hardRock` | HardRock, ProtoMetal | 2 | 14 + | "Born to Be Wild", "Black Magic Woman", "Bad Moon Rising", "Crosstown Traffic", "Dazed and Confused", "Good Times, Bad Times", "Kick Out the Jams", "Spirit in the Sky", "I'm a Man", one-word riff titles |
| `albumTitle.hardRock` (new) | HardRock, ProtoMetal | generic `albumTitle` | 9 | "Wheels of Fire", "Cheap Thrills", "Are You Experienced", "Heavy", "Live at the Fillmore" |
| `songTitle.sunshine` | SunshinePop, BaroquePop, PopRock | 2 | 12 + | "Windy", "Walk Away Renée", "Pretty Ballerina", "Kites Are Fun", "Pleasant Valley Sunday", "Incense and Peppermints", "Groovin'", "Sunshine Girl", "A Beautiful Morning", the invitation |
| `albumTitle.sunshine` | same | 2 | 10 + mood/other | "The Magic Garden", "Pisces, Aquarius, Capricorn...", "Along Comes ...", one-word LPs |
| `songTitle.bubblegum` | Bubblegum | 3 | 17 + | "Sugar, Sugar", "Yummy Yummy Yummy", "Goody Goody Gumdrops", "Simon Says", "1, 2, 3, Red Light", "Gimme Gimme Good Lovin'", "Sugar and Spice", "Bubble Gum Music", "Down at Lulu's" |
| `songTitle.garage` | GarageRock | 6 | 12 + | "96 Tears", "Psychotic Reaction", "Liar, Liar", the taunt, "Pushin' Too Hard", "Hey Little Girl", "Mr. Pharmacist", "The Witch" |
| `songTitle.reggae` | Ska, Rocksteady, Reggae | 2 | 17 + | "Train to Skaville", "Shanty Town", "Tougher Than Tough", "Rudy, a Message to You", "Oh Carolina", "Rivers of Babylon", "Return of Django" (1966+), "Rock Steady ..." (1966+), "Johnny Too Bad", "Dancing Mood" |
| `albumTitle.reggae` (new) | same | generic | 9 | "Ska Authentic", "Reggae ..." (1968+), "Battle of the ...", "Tougher Than Tough" |

Garage and bubblegum albums grew from 2-4 to 7-8 templates. The old generic adjective-noun frames stay at low weight;
the noun-connector-noun frames ("Sheriff Beside the Wanderer", "Queen in the Cowgirl") are gone from these sets.
`reserved.json` +170 real titles the new frames can reach (both "-in'" and "-ing" spellings, since the match is letters-only);
`features.json` tags "war" abstract ("Rumors of War", and no more "Under the War").

`TitleCensus --genres A,B,...` measures only the named genres, equally weighted: these sets are ~2% of the decade mix,
too small to see there.

## Result

Focused census (`--genres FolkRock,HardRock,ProtoMetal,SunshinePop,BaroquePop,Bubblegum,GarageRock,Ska,Rocksteady,Reggae`,
2,400 songs / 1,200 albums, seed 1001), committed data vs now:

| | Hot 100 1960-64 | before | now |
|---|---|---|---|
| two capitalised words | 17.2% | 45.3% | 17.2% |
| parenthetical | 8.8% | 0.1% | 6.1% |
| five+ words | 26.7% | 5.8% | 21.8% |
| you/your | 13.3% | 2.5% | 15.6% |
| lint per 1,000 | | 0.6 | 0.0 |
| album shapes / free albums distinct | | 49.6% / 92.9% | 44.9% / 77.4% |

The album "distinct" figures fall because the new sets use a few fixed-phrase pools ("Funhouse", "Weathervane").
The old numbers came from free adjective+noun draws ("Cerulean Eclipse") that were unique but read as random.
Decade mix, seeds 1001/2002/3003: shape metrics within ±0.5 pt of the third pass, songs still 100% distinct,
no template fails >20% of fills, lint 0.7 / 0.7 / 1.7 (3003's hits are in older shared frames, reshuffled). `NamingCoreTests`
184/186 (the same 2 pre-existing failures). Game project builds.

## Still open

* ~~"!" endings ~15% in these genres~~ — fixed, next section.
* British (6 templates incl. includes), protoPunk (4) and funk (5) song sets are still thin; band-name sets for folk (2),
  reggae/bubblegum/sunshine (2) were out of scope.
* Calibration: another re-baseline for these genres' track traits.

# Exclamation marks (2026-11-04)

`PostProcessor.Punctuation` added "!" with chance `(punctuationIntensity - 0.6) x 1.2`: 30-42% of titles in the
loudest voices (HardRock 0.85, GarageRock 0.9, Bubblegum 0.95) and none at all below 0.6. Real rates are low:
0.4% of 1960-64 Hot 100 titles end in "!", 1% of the 1965-69 sample (2 of 99: "Help!", "Stop! In the Name of
Love"), and the loud genres' hits mostly did without ("96 Tears", "Yummy Yummy Yummy", "Sugar, Sugar").

Now `(intensity - 0.4) x 0.06` with TitleSelection on (legacy keeps the old rule): rock 'n' roll / teen pop 1.2%,
soul 0.6%, bubblegum 3.3%, crooners and country 0. The roll still depends only on the genre, never on the wording,
but genres between 0.4 and 0.6 now draw it, so their title streams reshuffle (part of the same re-baseline).

| ends "!" | Hot 100 | before | now |
|---|---|---|---|
| decade mix 1001 / 2002 / 3003 | 0.4% | ~0.9-1.2% (all from loud genres) | 0.4 / 0.2 / 0.5% |
| late-decade genres (focused census) | | 14.5% | 1.8% |

The census now reports "ends !" on its own row. "?" is unchanged and stays low (0.1-0.3% vs 0.63%): it only
lands on question titles in voices above 0.4.

# Audit pass: "?", band names, one-word and "blues" titles, labels (2026-11-04)

## "?" on questions

On the 1960-64 chart 14% of question titles carry "?" in every genre ("Are You Lonesome To-night?", "What Kind of
Fool Am I?"). The legacy rule allowed it only above 0.4 intensity, at 0-16%. Now `0.08 + 0.12 x intensity` (10% at
0.2, 19% at 0.9), still only on a question. Decade mix "?" endings go from ~0.1-0.3% to ~0.6-0.7% (chart 0.63%).

## Late-decade band names

The NamingCensus samples showed the thin band sets (2-6 generic adjective+noun templates) were the weakest names
left: "Fight Haze", "Lipstick Wind" (hard rock), "The Luminescent Illusion" (psych), "The Angelic Satellites" (sunshine),
"The Sad Trio" / "The Rain Three" (the folk trio grammar's `{adjective:universal} Trio` / `{noun:universal} Three`).
Rewritten from period shapes over 54 new `bn*` pools:

| set | shapes |
|---|---|
| `bandName.folk` / `folkTrio` / `folkDuo` (template sets now override the grammar) | "The Kingston Trio", "The Highwaymen", "The Rooftop Singers", "The New Christy Minstrels", "Peter, Paul and Mary", "Ian & Sylvia", kin billing |
| `bandName.folkRock` (new) | "The Byrds" (misspelt bird), "Buffalo Springfield", "The Leaves", "Fairport Convention", "We Five" |
| `bandName.hardRock` | "Iron Butterfly" / "Blue Cheer" heavy-light pair, "Cream" / "Mountain" / "Steppenwolf", "The Amboy Dukes", "MC5", "Grand Funk Railroad", "Sir Lord Baltimore", "The Jeff Beck Group" |
| `bandName.psych` | "Strawberry Alarm Clock", "Country Joe and the Fish", "Love", "The Mothers of Invention" |
| `bandName.sunshine` | "The Yellow Balloon", "The Association", "Spanky and Our Gang", "The 5th Dimension", "Sagittarius" |
| `bandName.bubblegum` | "The 1910 Fruitgum Company", "The Ohio Express", "The Lemon Pipers", "The Music Explosion" |
| `bandName.reggae` | "The Clarendonians" / "The Heptones", "The Paragons", "The Righteous Flames", "Desmond Dekker and the Aces" |
| `bandName.garage` | "The Remains", "The Shadows of Knight", "Sam the Sham and the Pharaohs", "Count Five", "The Music Machine" |

Person slots bind to members as before; counts come from `%countWord%` / `%ensemble%`; leader titles are gender-neutral
(Country Joe, Captain, Professor). Folk-rock trios no longer route to the folk trio set. `reserved.json` +113 acts (the
real names each shape could reach, including backing bands: Aces, Pharaohs, Holding Company).
NamingCensus seeds 1001 / 2002: false head-count / kin claims 0 / 0, duplicates 0, non-member names 2.4 / 2.7%
(stage diminutives, unchanged). Samples: "The Topanga Convention", "Frozen Lullaby", "The Toledo Wardens", "Mesa",
"Cinnamon Barometer", "Electric Duane and the Glee Club", "The Tulsa Express", "The 1898 Jellybean Company".

## Titles: one-word, "blues", R&B

* **Instrumental one-word pool** (`instOne`, 70 place and mood words, none a period hit: "Marrakesh", "Portofino",
  "Moonmist", "Barcarolle") in easy listening, jazz and crooner pop, which had 1.6-6% one-word titles.
  Decade one-word 6.3-7.0% -> 7.3-8.0% (chart 9.6%).
* **R&B** routed to the blues set (every 20th R&B title had "Blues"). New `songTitle.rnB` = soul x1 + blues x0.55
  ("Shop Around", "Hit the Road Jack" read as both). Jazz "Blues" frames and folk's talking blues halved.
  Decade "blues" 1.3-1.6% -> 1.1-1.2% (chart 0.6%; jazz and blues records are off-chart, so the game's rate is plausible).
* **Nested includes**: the loader now resolves `include` depth-first, so a set can include a set that has includes
  (`songTitle.rnB` -> soul -> `_loveClauses`) whatever the file order; cycles terminate. No existing set nested, so
  earlier output is unchanged. Reserved +26 songs (W.C. Handy blues, instrumental standards: "Exodus", "Moonglow").

## Labels

"Voice of Sanctified Records" (an adjective after "Voice of") now draws a noun list; the major-label
"Dominion-Meadowlark Records" hyphen join becomes "{corporate} Artists Records" ("United Artists" shape).

## Tests

`NamingCoreTests` 189/189: three new include tests (nesting, scale product, cycle), and the two long-failing
Blues nickname tests fixed. They hard-coded 0.9, but BluesRoot was deliberately lowered to 0.45, so they now test
the inheritance instead of the number. TitleCensus decade 1001/2002/3003: lint 1.0 / 0.5 / 0.5, no template fails >20%.

## Still open

* One-word titles 7.3-8.0% vs 9.6%.
* British (6), protoPunk (4), funk (5) song sets; gospel / girl-group / teen-pop / surf / latin band sets (3 each)
  are serviceable but small; `bandName.garageEarly` (pre-1966 garage, grammar, 2 rules).
* Calibration: names and titles are simulation inputs (programme identity, track traits), so this pass re-baselines too.
