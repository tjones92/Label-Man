# Celebrity, regional markets, and economy audit

**October 15, 2026 — report only.** Reviewed branch `media-atmosphere`, HEAD `a282d24c220614b06cfc24f26ebdc2049054e797`, including the pre-existing working-tree changes to the media files. No game implementation was changed.

The question guiding this review is whether a player entering on January 1, 1960 encounters an established musical world, can follow the rise of rival acts, and can build an act's fame through believable economic and audience behavior.

Claude's handoff was treated as a set of leads to verify, not as instructions to implement its proposed work. Source comments and older directives were also checked against executable paths; several describe phases that have since changed.

## Principal conclusions

**The regional problem is real, but “zero selling regions” is a misleading name for the symptom.** The simulation maintains separate regional inventories and calculates local sales before adding them to the national total. However, common national signals dominate those local calculations. The regions consequently produce almost the same best-seller lists. A fan-system cutoff then turns a modest ranking decline into an apparent nationwide disappearance. In 185 such transitions in the saved mid-decade run, the affected records continued to sell in all seven regions.

**The January 1960 world has a star roster without a corresponding stock of established celebrity.** Thirty acts receive impressive career counters, momentum, and reputation. Their public recognition, cultural standing, personal recognition, audience attachment, and actual back catalogs are not initialized to match. The general artist generator cannot produce a long-established prewar career: initial acts are formed in 1955–60. This is a substantial obstacle to the Sinatra/Como side of the intended world.

**Later fame is possible, but the world has several competing definitions of it.** Recognition, career rank, artistic reputation, teen attachment, media exposure, and member spotlight have different writers and readers. Some separation is desirable. The missing connections mean that television can make someone the face of a phenomenon without changing their actual personal fame, while an office conversation can increase that fame immediately.

**There is substantial outcome steering in the broader economy.** Historical chart-share benchmarks actively damp radio candidacy; label tier directly multiplies album demand; selected labels are prohibited from building independent distribution; and format capacity is fitted to an authored LP-share curve. These are distinct from ordinary historical setting inputs. There are also two concrete accounting defects: player returns reserves can release money that was never withheld, and AI advance recoupment destroys money.

The appropriate repair is a connected model of local demand, durable audience memory, and publicly observable careers. Lowering fan thresholds alone cannot supply that model.

## Evidence and limits

I built the current project successfully with `dotnet build "Label Man.csproj" -c Debug --no-restore`: zero warnings and zero errors. I ran two new 13-week headless audits, seeds 1001 and 2002, with the genre-market and artist-population lifecycle enabled. Both exited successfully, produced 13 completed weekly rows, and printed `CHART_AUDIT_COMPLETE`.

I also independently analyzed the existing `media-p3-hot36` regional-sales CSVs, the January 4, 1963 world, and an older January 3, 1969 band-life world. The latter is evidence that the architecture has produced later stars and solo careers; it is **not** a fresh decade validation of the current working tree. The older world's metadata includes `--observe-band-life`, so it must not be used as a present-day forecast of lineup outcomes.

| Measurement | Fresh seed 1001 | Fresh seed 2002 | Existing seed 1001 mid-decade run |
|---|---:|---:|---:|
| Completed weeks analyzed | 13 | 13 | 209, January 1963–January 1967 |
| Mean pairwise regional sales correlation | 0.935 | 0.955 | 0.984 |
| Mean overlap between two regional top-eleven lists | 85.1% | 86.5% | 90.4% |
| Act transitions: all seven qualifying regions → none | 2 | 7 | 185 |
| Regional clearing observations | 91 | 91 | 1,463 |
| Inventory/allocation violations | 0 / 0 | 0 / 0 | 0 / 0 |
| Nonzero reconciliation/settlement deltas | 0 / 0 | 0 / 0 | 0 / 0 |

Correlation here is **cross-sectional Pearson correlation between regional unit-sales vectors**, averaged over regional pairs and weeks. It is not a measurement of each record's time-series correlation. Regional top-list overlap is the more direct evidence that local rankings lack independence. No historical regional-overlap benchmark was fitted during this audit.

The top-eleven depth is intentional: the current fan formula's “near the top” test admits eleven records, despite descriptions calling it a top ten. National sales ranks in the evidence files are calculated from copies sold; they are not the game's published sales-plus-airplay chart ranks.

Generated evidence and reproduction code:

- [Analysis script](C:/Project/Label-Man/SimTools/analyze_celebrity_audit.py)
- [Fresh seed 1001 regional evidence](C:/Project/Label-Man/SimTools/CelebrityAudit-1001-regional-evidence.json) and [seed 2002](C:/Project/Label-Man/SimTools/CelebrityAudit-2002-regional-evidence.json)
- [Mid-decade regional evidence](C:/Project/Label-Man/SimTools/CelebrityAudit-regional-evidence.json)
- [First-week world evidence](C:/Project/Label-Man/SimTools/CelebrityAudit-first-week-world-evidence.json), [1963 world evidence](C:/Project/Label-Man/SimTools/CelebrityAudit-1963-world-evidence.json), and [older 1969 world evidence](C:/Project/Label-Man/SimTools/CelebrityAudit-1969-world-evidence.json)

The measurements establish behavior and prevalence within these samples. Source tracing establishes the mechanisms below. I did not run a new decade, play a player-controlled campaign, or claim to have calibrated a replacement model.

## Findings

### 1. The seeded star roster does not initialize the fame system that now matters

**High priority — confirmed initialization mismatch.**

`SeedInitialStarCanopy` selects high-quality acts on Major/MidTier rosters and `StampStar` sets career state, hit counts, momentum, reputation, and release totals. It never sets `publicRecognition`, `culturalStanding`, or members' recognition/reputations. Media initialization books from the opening chart; it does not import an established fan history.

The first-week save from the fresh seed 1001 run contains all 30 stamped acts:

- All 30 have zero cultural standing.
- Eight still have exactly zero public recognition after the first live week.
- Twenty-five have recognition at or below 0.01, where the artist UI hides STANDING.
- The highest recognition among the 30 is only 0.086706.
- Across the entire 7,042-act registry, no act exceeds 0.40 or the UI's 0.60 household-name threshold. The maximum is 0.331262.

The small nonzero values are not a coherent pre-1960 celebrity history. Prewarm chart credit and live activity can award recognition around startup, but the canopy's historical hit counters are not replayed through the recognition service.

**Important qualification:** these stars are not commercially identical to anonymous newcomers. Their seeded momentum and reputation still feed release awareness, and career state still affects marketing budgets and album planning. The failure is that those advantages and the world's public memory disagree about who is famous.

**Suggestion:** initialize a single coherent historical career state, then derive the opening counters, recognition, audience relationships, person-level reputations, visible history, and booking eligibility from it. Do not solve this by reinstating the old automatic 2.5× stock bonus on top of recognition.

Sources: [RosterManager.cs:232](C:/Project/Label-Man/Systems/RosterManager.cs:232), [StampStar:275](C:/Project/Label-Man/Systems/RosterManager.cs:275), [awareness inputs:400](C:/Project/Label-Man/Data/SimulatedArtist.cs:400), [media initialization:133](C:/Project/Label-Man/Systems/Media/MediaAtmosphereService.cs:133), [standing presentation:120](C:/Project/Label-Man/UI/ArtistDetailPanel.cs:120).

### 2. The opening biographies cannot represent an established adult-entertainment world

**High priority — confirmed generation and history problem.**

The initial generator assigns `formedYear = year - RandInt(0, 5)`. Ordinary musician generation chooses ages 18–29 for 85% of draws and 29–42 otherwise. A later quality-ranked stamp can make any selected act a Superstar without a corresponding career-length requirement.

In the fresh first-week sample, four of the 30 seeded stars were formed in 1960 and eight in 1959 or 1960. Marie Price is an 18-year-old, formed in 1960, already stamped Superstar. This is not inherently wrong for a breakout teenager; it is wrong as the general mechanism supplying a whole established canopy, including the older adult market.

All 30 also have more lifetime chart entries than total releases. For example, Solomon Reynolds has 42 chart entries against 28 releases. That contradiction originates in the stamp itself: its synthetic charted count can exceed its synthetic release count. There is no separately named historical accounting convention that explains the difference.

The initial record population selects random roster acts and backdates releases by 1–20 weeks. `historicalRecords` is null and has no scene assignment. Consequently, stamping decades of success does not create the named old singles, albums, awards, or career events a player could investigate.

For a historical reference point, Sinatra's official catalog describes more than 300 Capitol recordings from 1953–62, including the established concept-album body of work. A plausible January 1960 analogue needs a substantial existing career and catalog, not merely a current hit multiplier. [Frank Sinatra Enterprises/UMe](https://www.sinatra.com/frank-sinatra-platinum-out-today/).

**Suggestion:** author distributions for veteran careers, established adult stars, current teen idols, regional specialists, respected non-hitmakers, and declining former stars. Generate plausible age, career start, actual catalog summaries, recent activity, and regional/audience recognition together. A compact historical initialization can do this without simulating twenty full years.

Sources: [ArtistManager.cs:329](C:/Project/Label-Man/Systems/ArtistManager.cs:329), [musician ages:562](C:/Project/Label-Man/Systems/ArtistManager.cs:562), [initial catalog:653](C:/Project/Label-Man/Systems/CompetitorManager.cs:653), [historical-record hook:249](C:/Project/Label-Man/Systems/CompetitorManager.cs:249).

### 3. The seven-region disappearance is a ranking cutoff layered over highly similar markets

**High priority — independently reproduced.**

For each region, media ranks singles by actual copies sold and retains the top 20. The score is `1 - zeroBasedRank / 20`. A “selling region” requires score ≥0.5, so positions 1–11 qualify; position 12 does not. Falling out of all seven lists therefore reports zero even if substantial sales continue everywhere.

Examples from the independently analyzed data:

| Sample | Record/act | National copies-sold rank change | Qualifying regions | Copies next week | Regions with actual positive sales next week |
|---|---|---:|---:|---:|---:|
| Fresh 1960 seed 1001, week 5 | “The Captivating Bad-luck Spell,” The Latin Twangers (`gen_7`) | 6 → 16 | 7 → 0 | 18,558 | 7 |
| Mid-decade sample, week 2 | `gen_21383`, `artist_02066` | 10 → 19 | 7 → 0 | 19,120 | 7 |
| Mid-decade sample, week 5 | `gen_21979`, `artist_03963` | 9 → 13 | 7 → 0 | 23,258 | 7 |

All 185 mid-decade transitions identified by the script have mapped artist IDs, and all affected records continue selling in all seven regions afterward. Thus the handoff's diagnosis points toward a real problem, but “an act stopped selling nationally” would be the wrong conclusion.

The cutoff still matters mechanically: it removes Sales evidence and qualifying breadth from the fan episode calculation. A continuous sales decline becomes a categorical collapse in the world's response.

**Suggestion:** rename the diagnostic immediately to describe its actual meaning. For the eventual model, base regional audience response on local penetration, repeat demand, momentum, and sustained audience engagement, with soft thresholds or hysteresis where a categorical state is needed. A regional rank can contribute evidence without acting as a nationwide on/off switch.

Sources: [regional fan ranking:57](C:/Project/Label-Man/Systems/Media/MediaAtmosphereService.Fans.cs:57), [Sales family and breadth:164](C:/Project/Label-Man/Systems/Media/MediaAtmosphereService.Fans.cs:164).

### 4. Local sales are downstream of national popularity through several simultaneous channels

**High priority — principal structural explanation for synchronization.**

The national chart is correctly built after local sales. The causality then loops back strongly into every market the following week:

1. National top-ten status imposes an effective-awareness floor of 0.70 in every region; top 40 imposes 0.40.
2. Previous national/internal rank enters the visibility/discovery calculation and a separate rank-exposure multiplier on conversion.
3. National rank also supports national awareness, word of mouth, radio heat, and label push.
4. Regional radio is predominantly pulled toward the same national `radioHeat`. The reporter-station contribution is blended at 0.13, leaving 87% of the blend on the regional tail path that follows national heat.
5. Radio fatigue and regional drop probabilities read the national sales peak/support clock. Drop rolls are regional, but the deterioration signal is shared.
6. The release ramp and post-eight-week demand-age decay are record-wide, so a market discovering a record late inherits an already-aged national lifecycle.

The outcome is not literally one national sales number divided seven ways. Local genre acceptance, inventory, exhaustion, sentiment, stations, and breakout propagation do exist. They operate under a large common national envelope, consistent with the measured 85–90% overlap in regional leading records.

**Suggestion:** make local audience awareness, local radio adoption, local sales support, and time since local introduction the main states. National chart visibility should be a bounded information channel into those states. Aggregate the resulting local sales and spins into the national survey. Allow a record to be rising in the West while it is fading in the East.

Preserve shared national events and legitimate national campaigns. Independence does not require unrelated regional randomness, nor does it require deleting national influence.

Sources: [regional demand:449](C:/Project/Label-Man/Systems/ChartSimulator.cs:449), [rank exposure:499](C:/Project/Label-Man/Systems/ChartSimulator.cs:499), [radio heat:842](C:/Project/Label-Man/Systems/ChartSimulator.cs:842), [label push and awareness:1059](C:/Project/Label-Man/Systems/ChartSimulator.cs:1059), [regional radio update:1663](C:/Project/Label-Man/Systems/ChartManager.cs:1663), [national aggregation:1227](C:/Project/Label-Man/Systems/ChartSimulator.cs:1227).

### 5. A supposed discovery gain can reduce existing awareness and radio play

**High priority — confirmed arithmetic; apparent implementation defect.**

`ApplyBreakoutDiscovery` uses assignments such as:

```csharp
source.awareness = Mathf.Min(0.58f, source.awareness + localAwarenessGain);
neighbor.awareness = Mathf.Min(0.34f, neighbor.awareness + neighborAwarenessGain);
```

Those cap the entire existing stock, not the incremental contribution. A source with awareness 0.70 is reduced to 0.58 when awarded a discovery gain. Similar assignments cap source radio at 0.45 and neighboring radio at 0.24. The final national assignment caps awareness at 0.60 for every record that reaches the method with a valid label, even when no regional discovery gain was earned.

This can undo stronger awareness or rotation earned through another channel. It can also make the order in which sources visit a neighboring market consequential.

**Suggestion:** limit each channel's incremental contribution or its remaining headroom; a positive discovery operation should not erase an existing stock above that channel's ceiling. Verify this with above-cap starting states and multiple source-region orders before recalibrating downstream constants.

Source: [ChartManager.cs:1982](C:/Project/Label-Man/Systems/ChartManager.cs:1982), particularly lines 2003, 2009, 2032, 2034, and 2042.

### 6. Radio stations actively enforce historical genre chart-share benchmarks

**High priority for simulation design — deliberate outcome steering.**

`GenreChartGuard` compares a genre's current national top-20 share with a historical year-end slot-share benchmark. When it exceeds both the relative deadband and the absolute five-point excess, station candidacy is reduced toward the target. The multiplier can fall to 0.35. This is live from 1960, and the current exemption set is empty.

For example, if a genre holds 40% of the current top 20 against a 20% target, the code supplies a 0.65 multiplier, all other conditions equal. A record's chance of station support is reduced because other records in its genre have collectively done too well against the historical target.

That is more than historical listener tastes or station formats. It is a corrective force against an alternate history the player or rivals might actually earn. Its direct scope is reporter candidacy, not a blanket 65% reduction of all sales or all airplay; the broader effect needs an isolated experiment.

**Suggestion:** keep historical recap shares as diagnostic targets. Express history causally through station formats, programmer preferences, audience composition, distribution, and supply. If deliberate scenario-level convergence is wanted, identify it as a separate design choice and measure how much it suppresses exceptional acts.

Source: [StationNetwork.Playlist.cs:269](C:/Project/Label-Man/Systems/Radio/StationNetwork.Playlist.cs:269), [guard:448](C:/Project/Label-Man/Systems/Radio/StationNetwork.Playlist.cs:448), [candidacy multiplication:500](C:/Project/Label-Man/Systems/Radio/StationNetwork.Playlist.cs:500).

### 7. Album customers respond directly to the label's tier

**High priority — confirmed hidden demand multiplier.**

Live album conversion is multiplied by this table:

| Label tier | Album demand multiplier |
|---|---:|
| Major | 1.36 |
| MidTier | 0.95 |
| Independent | 0.57 |
| Boutique | 0.63 |
| Small | 0.50 |

A Major therefore gets 2.72 times the album conversion of a Small label with the same other inputs. Reach, launch stock, promotion, and supply constraints already have their own effects. A tier promotion can thus improve demand without a corresponding change in what listeners know or stores can deliver.

The single-demand path has already moved toward a continuous scale based on distribution strength and national reach. Albums retain the categorical table.

**Suggestion:** identify the physical or audience mechanism this factor is meant to represent and model it once. Use actual availability, promotion, retail fulfillment, or a bounded consumer-facing label reputation where appropriate. Audit the remaining overlap with reach and campaign effects.

Sources: [AlbumSimulator.cs:89](C:/Project/Label-Man/Systems/AlbumSimulator.cs:89), [tier table:203](C:/Project/Label-Man/Systems/AlbumSimulator.cs:203), [single comparison:657](C:/Project/Label-Man/Systems/ChartSimulator.cs:657).

### 8. Celebrity and media are disconnected, and prime-time booking ignores established fame

**High priority — implemented boundary, major gap against the desired experience.**

The useful distinctions currently include:

| State | Main meaning | Important live use |
|---|---|---|
| `careerState` | Discrete commercial-career rung | Budgets, release policy, album prior |
| `publicRecognition` | Durable familiarity with the act | Release awareness, stock, radio lift |
| `culturalStanding` | Landmark/influence standing | Small contribution to effective launch recognition |
| `personalRecognition` | A musician's own name | Solo viability, spin-out inheritance, band life |
| `criticalAcclaim` | Craft-based reputation | Cultural/narrative layers; distinct from press circulation |
| Media attachment/spotlight | Teen response and camera attention | Fan episodes, media interest, hype limits |

The problem is missing exchange between these states. A television appearance and press coverage can generate media attachment and member spotlight but no durable public or personal recognition. Media is explicitly Observe-only; causal mode is rejected. Player media costs are estimates and are not charged, so this is not currently an undisclosed cash sink. It is, however, a career-development loop that stops inside media.

National ordinary bookings come from the current singles chart and a program's chart ceiling. Sullivan has ceiling 25 and no discovery slot. A famous adult performer with a strong album career but no current top-25 single cannot enter that ordinary candidate pool. Other discovery-enabled shows admit regional breakouts, which is a useful existing seam.

The official Sullivan history documents the Beatles booking agreement in November 1963 and the later American number-one breakthrough in January 1964. Booking judgment, reputation, management, overseas evidence, and anticipation can precede a domestic chart result. [The Ed Sullivan Show archive](https://www.edsullivan.com/artists/the-beatles/).

**Suggestion:** retain separate exposure, familiarity, attachment, and esteem, but connect actual audience encounters to bounded recognition deposits. Prime-time bookers need career/album/reputation candidates alongside current singles, with genre and audience fit. Teen attachment should remain a teen measure; adult stars need a visible relationship with their own audience rather than being forced through teen frenzy.

Sources: [MediaAtmosphere.cs:6](C:/Project/Label-Man/Systems/Media/MediaAtmosphere.cs:6), [Observe cost note:78](C:/Project/Label-Man/Systems/Media/MediaAtmosphereService.Player.cs:78), [national candidates:535](C:/Project/Label-Man/Systems/Media/MediaAtmosphereService.cs:535), [program catalog](C:/Project/Label-Man/Systems/Media/MediaOutletCatalog.cs), [recognition writers](C:/Project/Label-Man/Systems/ArtistRecognitionService.cs).

### 9. Fan episodes are too dependent on coincident weekly gates to describe a durable phenomenon

**High priority — confirmed mechanism; observed limitation, not a proof of mathematical impossibility.**

Sales-only attachment at maximum youth fit deposits 0.06 per week, after retention with an eight-week half-life. Its equilibrium is approximately 0.435. The old 0.45 hot threshold exceeded that sales-only equilibrium; the current default is 0.36. Television and coverage can lift attachment beyond the sales-only equilibrium, so 0.435 is not a universal ceiling.

A national sensation needs hot attachment in at least three regions, warmth in at least four, sufficient current qualifying sales breadth, national exposure, two evidence families, and two qualifying weeks in four. A phenomenon needs at least five hot regions and four consecutive highest-level weeks, including extraordinary record evidence. A fading national episode starts a fixed 56-day cooldown that blocks a new national episode even if another release breaks immediately.

The existing phase-3 notes report three sensations lasting roughly two to three weeks across 1963–66 and no phenomena at the new default. I independently verified the sales synchronization underlying that explanation, not a fresh four-year media replay. Synthetic qualifying weeks can reach phenomenon; the production trajectory has failed to align the gates.

There are further losses of information: sales evidence uses the act's best single in each region rather than cumulative response to its catalog; attachment is not initialized for seeded idols; and the `selling` boolean that changes TV gain is true for the whole act if any region meets its test.

**Suggestion:** separate an enduring fan relationship from short publicity surges. Let successive releases, returning listeners, local appearances, and catalog demand build on one another. Make peaks and cooling depend on audience behavior and competing attention. A sustained phenomenon should be rare because an exceptional career is rare, not because several weekly booleans must happen to overlap.

Sources: [attachment and evidence:144](C:/Project/Label-Man/Systems/Media/MediaAtmosphereService.Fans.cs:144), [episode arithmetic:64](C:/Project/Label-Man/Systems/Media/MediaFans.cs:64), [existing measurements](C:/Project/Label-Man/SimTools/MediaAtmospherePhase3Notes.md).

### 10. Private office decisions can mint public fame before anything reaches the public

**Medium priority — clear example of game-like reward leakage.**

The player's `SpotlightThem` conversation immediately adds 0.01 to a musician's `personalRecognition` while renaming the act. `SoloSingleYes` immediately adds 0.008 when agreeing to a solo side. Neither increment waits for a pressing, release, broadcast, attendance, or audience response.

These are meaningful quantities: the solo-launch bar is 0.05, and personal recognition feeds other band-life and person-level systems. The changes are not merely private trust or morale rewards.

**Suggestion:** let the conversation change billing, commitments, trust, or future camera allocation. Award public fame when the promised activity actually reaches an audience. This finding does not establish an unlimited repeat-click exploit; that would require a separate interaction/cooldown test.

Sources: [PlayerDesk.BandRoom.cs:806](C:/Project/Label-Man/Systems/PlayerDesk.BandRoom.cs:806), [solo promise:957](C:/Project/Label-Man/Systems/PlayerDesk.BandRoom.cs:957), [solo gates:96](C:/Project/Label-Man/Systems/BandLifeService.cs:96).

### 11. The career ladder still treats fame as a collection of chart trophies

**Medium priority — design mismatch that remains economically relevant.**

Established → Star requires three consecutive hits and at least one number one. Star → Superstar requires at least four number ones and four consecutive hits. Top-40/10 milestones, flop streaks, and chart-run completion control the ladder. These hooks receive album as well as single chart updates; this is not exclusively a singles ladder.

A performer can accumulate broad familiarity, years of successful albums, or a devoted audience and remain below Star without the specified number-one history. Conversely, the initial canopy bypasses the ladder with invented counters. Fame can therefore disagree with career labels in both directions.

`GetMarketingBudget` and the album prior still read the ladder. It affects investment and opportunity, not just UI wording. Those budget effects are **not automatically double-counted fame**: more real spending on a proven artist can be legitimate. The mismatch is using brittle trophy thresholds as a universal stature measure.

**Suggestion:** distinguish contract/career operating state from observed commercial stature and celebrity. Derive policy from relevant evidence—album business, audience draw, cumulative familiarity, recent demand, costs—rather than requiring every successful career to collect the same trophies.

Sources: [career transition rules:315](C:/Project/Label-Man/Data/SimulatedArtist.cs:315), [marketing budgets:808](C:/Project/Label-Man/Data/AILabel.cs:808), [album prior:2816](C:/Project/Label-Man/Systems/CompetitorManager.cs:2816).

### 12. Rival careers lose their visible discographies as records leave active simulation

**High priority for the requested rival-watching experience.**

The artist detail panel builds discography and awards from `ChartManager.GetAllRecords()`. AI record retirement removes records from that collection. Retired singles have an `AlbumTrack` archive for track resolution, but the artist panel does not consult it; retired albums do not enter that single-track archive. A career can retain its counters and eras while its actual old releases disappear from the artist's displayed file.

There are useful existing rival surfaces: charts, clickable artist files, rival signing news, regional breakout listings, persistent scouting dossiers, and follow-through after losing a prospect. This is not an absence of rival systems. They do not yet provide a durable public career file for watching the same rival act across the decade.

The regional-rival listing also uses **peak** breakout score, emits the first qualifying region, and the UI takes six rows without sorting for current momentum. It returns label, record title, and region, omitting the act name. Old qualifying records can therefore occupy a limited feed intended to reveal who is coming up now. The fan summary exposes rivals chiefly when national or in cooldown, missing much of the local-to-national rise.

**Suggestion:** preserve compact, immutable public release/award history separately from live demand records. Extend the existing dossier concept into an act watch list with links to current local sales, catalog history, lineup, label, press, and announced appearances. Sort emerging-rival news by fresh evidence and name the act. Retain uncertainty about private talent and finances.

Sources: [ArtistDetailPanel.cs:149](C:/Project/Label-Man/UI/ArtistDetailPanel.cs:149), [live-record lookup:243](C:/Project/Label-Man/UI/ArtistDetailPanel.cs:243), [retirement:2313](C:/Project/Label-Man/Systems/ChartManager.cs:2313), [rival breakout listing:137](C:/Project/Label-Man/Systems/PlayerDesk.TradePress.cs:137), [rival display:2855](C:/Project/Label-Man/UI/PlayerDeskPanel.cs:2855), [fan summaries:64](C:/Project/Label-Man/Systems/Media/PlayerDesk.Media.cs:64).

### 13. Player returns reserves can create additional money at maturity

**High priority — concrete accounting defect, independent of celebrity tuning.**

`DeferWholesaleBillings` creates a normal receivable for `billed × reportingHonesty`. For a player-owned label it also creates a returns reserve for `billed × returnAllowance`. The full collectable receivable remains intact. The banked weekly amount subtracts `deferred`, which includes the billing but does not separately withhold the additional reserve. At maturity the normal receivable pays, and a surviving distribution relationship releases the reserve into cash too.

A simple all-honest, fully reliable case illustrates the identity:

- Billing: $100.
- Normal receivable: $100.
- Additional returns reserve at 20%: $20.
- Initial cash receipt: $0.
- Later cash receipt with the relationship retained: $100 + $20.

The reserve was recorded as a second future asset without reducing the first. Its release creates the extra $20. Losing the relationship instead records a write-off against that extra asset. No other reader of the reserve balance supplies the missing initial withholding.

**Suggestion:** represent the held reserve as part of the original receivable, split into currently collectable and withheld portions. Verify that total eventual receipts plus actual losses reconcile to the original billing. The audited AI-only clearing CSVs cannot catch this player-only defect.

Sources: [cash settlement:865](C:/Project/Label-Man/Systems/CompetitorManager.cs:865), [receivable creation:931](C:/Project/Label-Man/Systems/CompetitorManager.cs:931), [reserve creation:940](C:/Project/Label-Man/Systems/CompetitorManager.cs:940), [reserve release:964](C:/Project/Label-Man/Systems/CompetitorManager.cs:964).

### 14. AI advance recoupment charges royalties that nobody receives

**High priority — confirmed and explicitly acknowledged in the code.**

The settlement calculates the recouped portion of an artist royalty and the cash royalty remaining for the artist. Player labels expense only the remaining amount. AI labels expense the full royalty, even though the recouped portion is not paid to the artist. The advance was already expensed at signing.

Thus, if $10 of a week's $10 royalty is recouped, the artist receives $0 but the AI label loses another $10. The source comment correctly identifies money destruction and explains that AI survival was calibrated with the defect in place.

**Suggestion:** repair the accounting identity before treating survival, acquisition pressure, and reinvestment rates as realistic. Re-estimate those rates afterward rather than preserving an accounting error as a balancing tool. This is distinct from demanding that every payment stay within the simulated music industry; it is a discrepancy in what this particular payment is said to do.

Source: [CompetitorManager.cs:1068](C:/Project/Label-Man/Systems/CompetitorManager.cs:1068).

### 15. Player and rival success are funded and settled under materially different conventions

**Medium priority — confirmed asymmetries; net balance impact unmeasured.**

Player singles receive `DealerPrice` ($0.63) in wholesale settlement while AI singles retain the configured default of $0.89. Player manufacturing is paid up front through its own production model and skipped in per-sale COGS. AI manufacturing is expensed per sold unit. Player singles have an explicit mechanical-royalty leg; AI composition cost is described as folded into calibrated COGS. Returns reserves and advance-recoupment treatment also differ as above.

Different channels can legitimately have different economics, and these comparisons alone do **not** prove that one side is easier overall. They do mean the player cannot infer a rival's commercial strength from identical copies sold, and AI-only balance runs do not validate the player's path to rival-level fame.

Wholesale deferral adds another approximation: it allocates a label's weekly net revenue across wholesale regions by unit counts. It does not defer the exact cash margin of the specific records sold in each region. A mixed singles/album catalog with different regional footprints can therefore have distorted payment timing.

**Suggestion:** use common record/channel transaction definitions for list price, dealer price, manufacturing funding, distributor commission, mechanicals, recoupment, receivables, and returns. Allow differences to follow contracts and channels. Add player-inclusive audits of the same “local hit → wider demand → finance larger supply” path that rivals use.

Sources: [revenue settlement:1021](C:/Project/Label-Man/Systems/CompetitorManager.cs:1021), [mechanical royalties:1097](C:/Project/Label-Man/Systems/CompetitorManager.cs:1097), [unit-weighted deferral:902](C:/Project/Label-Man/Systems/CompetitorManager.cs:902), [dealer price:5093](C:/Project/Label-Man/Systems/PlayerDesk.cs:5093).

### 16. Some labels are assigned dependency as a permanent fate

**Medium priority — deliberate historical scripting through capability restrictions.**

`distributionDependentHitmaker` causes an immediate return from both independent-distributor pursuit and self-built distribution growth. Runtime profile generation sets the flag. An economically successful label can therefore remain unable to pursue these independence routes because of its authored identity, rather than current contracts, financing, or managerial decisions.

The comments explicitly connect this to keeping successful labels dependent enough to become late-decade absorption targets. Consolidation itself still has gates and a random decision; the flag does not guarantee an acquisition. It does deliberately remove important escape routes.

**Suggestion:** model a preference to reinvest in music as a revisable business policy, with real opportunity costs. Let cash, available partners, contract restrictions, management, and strategic choices determine whether a label remains dependent.

Sources: [profile assignment:155](C:/Project/Label-Man/Systems/RuntimeLabelProfileFactory.cs:155), [distributor pursuit:482](C:/Project/Label-Man/Systems/CompetitorManager.cs:482), [self-built growth:3907](C:/Project/Label-Man/Systems/CompetitorManager.cs:3907), [consolidation eligibility:4543](C:/Project/Label-Man/Systems/CompetitorManager.cs:4543).

### 17. Format shares and record lifetimes have strong authored shapes

**Medium priority — model limitation to keep separate from accounting defects.**

The regional purchase budget is bounded, which is valuable: adding records does not create unlimited customers. Local proportional clearing and bounded one-hop spillover also prevent a simple unlimited national pool. The sampled ledgers reconcile cleanly.

However, the album channel is supplied by an era polynomial explicitly fitted to LP-share anchors. When demand saturates the channel, capacity substantially determines the format split. Singles have a shared six-week launch ramp and demand decay after week eight. Normal albums likewise have an authored decay clock; selected soundtrack/cast/blockbuster categories receive much longer curves.

These are useful approximations for a bounded simulation. They become problematic when the intended story is “a local audience discovers this record late,” “a catalog revival takes off,” or “this label changes the LP business.” Such events face clocks and channel limits keyed to the record's age or calendar, not just actual audience behavior.

Chubby Checker is a useful boundary case: ABKCO's history describes the 1960 hit and its separate return to number one in January 1962. A model of celebrity and catalog history should be able to express a renewed audience for the same record. This audit did not locate a general demand-revival route equivalent to that case in the normal single lifecycle. [ABKCO](https://www.abkco.com/artist/chubby-checker/).

**Suggestion:** keep era infrastructure and demographics as starting conditions. Let fatigue depend more on local exposure and penetration, and give genuinely renewed demand a route to reactivation. Check whether format limits represent consumers' willingness to spend, retail capacity, or a desired output share; those are different quantities.

Sources: [market/channel constants:132](C:/Project/Label-Man/Systems/ChartManager.cs:132), [regional clearing:1190](C:/Project/Label-Man/Systems/ChartManager.cs:1190), [format clearing:1408](C:/Project/Label-Man/Systems/ChartManager.cs:1408), [single aging:506](C:/Project/Label-Man/Systems/ChartSimulator.cs:506), [album curves](C:/Project/Label-Man/Systems/AlbumSimulator.cs).

## Working foundations and corrections to the handoff

Several important systems already work and should survive a redesign:

- **Regions are real accounting entities.** They have inventories, sales totals, demand exhaustion, genre acceptance, radio state, backorders, clearing limits, and adjacent-market propagation. The national total is built from local results. The necessary work is changing the strength and ownership of causal signals, not creating regional objects from scratch.
- **Recognition is persistent and bounded.** Public recognition decays on a roughly five-year half-life and cultural standing much more slowly. Milestone gains have diminishing returns and per-record credit latches. A chart decline does not directly delete celebrity.
- **The live launch route is correctly identified.** `CompetitorManager.ApplyReleasePromotion` is used by both ordinary rival releases and `ReleasePlayerRecord`. Recognition replaces the old career-state launch-stock switch when enabled; this review did not find that old multiplier stacked on the new one in those canonical paths. Album promotion has the same replacement. `ChartManager.PromoteRecordAI` is a separate initialization/alternate route and must not be mistaken for every AI release.
- **Solo recognition transfer is implemented.** `CreateSoloSpinOut` copies the departing musician's personal recognition into the new act's public recognition. `RemoveFromAct` physically removes the person, and `JoinAct` retains person-level facts. The old group keeps its act-level stock; retaining a group's name is reasonable and does not by itself prove duplicate credit.
- **Later stars have actually emerged.** The January 1963 save contains a runtime-formed Superstar. The older January 1969 world contains 14 runtime-formed Stars, three runtime-formed Superstars, and one runtime-formed act above the 0.60 recognition threshold. It also contains 24 solo-spinout acts with positive recognition. These are existence checks, not current-decade rate estimates or proof that every new/player career has fair access.
- **Recognition has a small non-chart route.** Local-room attributed draw can deposit up to 0.00012 recognition per set. It is not correct to say every canonical recognition deposit comes from the national chart. It is also far too small and geographically undifferentiated to substitute for an established national audience.

The still-open person-level seam is **joining an existing group**: effective act recognition reads the act's own stock, cultural standing, and the strongest member's creative reputation. It does not generally incorporate a famous incoming performer's personal recognition. The solo route carries a famous singer's name; the general new-group route is much weaker. Media member spotlight is another separate stock and does not supply that missing transfer.

The handoff's comment that lineup churn never fires is obsolete in the underlying recognition-service comments. Conversely, its observation that record-level press recognition has no journalism writer still holds: `CulturalRecognitionService.Deposit` has no current production caller. Its `Word` contribution does exist through `Consume`; the file's claim that only one channel writes recognition is an outdated description.

The generic record `controversy` field and media controversies also remain separate. Normal generated records receive only 0–0.2 generic controversy, while generic UI tags require over 0.5 or 0.6. Actual media standards disputes do not populate that field. It is a presentation/schema inconsistency, not evidence that controversy boosts sales.

Sources: [player release:4821](C:/Project/Label-Man/Systems/CompetitorManager.cs:4821), [canonical promotion:3706](C:/Project/Label-Man/Systems/CompetitorManager.cs:3706), [album snapshot promotion:3605](C:/Project/Label-Man/Systems/CompetitorManager.cs:3605), [solo transfer:1289](C:/Project/Label-Man/Systems/ArtistManager.cs:1289), [member removal/join:1336](C:/Project/Label-Man/Systems/BandLifeService.cs:1336), [live draw:310](C:/Project/Label-Man/Systems/LocalSceneRoomService.cs:310), [effective recognition:62](C:/Project/Label-Man/Systems/ArtistRecognitionService.cs:62), [cultural record recognition](C:/Project/Label-Man/Systems/CulturalRecognitionService.cs).

## Suggested improvement order

| Order | Work | Observable result to require |
|---|---|---|
| 1 | Repair the two cash identities and the discovery-cap behavior | A reserve cannot pay more than its billing; recoupment balances; a positive discovery deposit cannot reduce stock |
| 2 | Initialize coherent January 1960 careers and durable public history | Established adult stars, teen idols, regional names, critics' favorites, and fading veterans are visibly different on day one |
| 3 | Rebalance causality toward regional audience state | Local peaks occur at different times; a national ranking decline does not erase simultaneous local success; national sales still reconcile |
| 4 | Connect actual exposure to bounded fame and suitable booking | A famous crooner without a current hit remains bookable; a newcomer can be discovered before a national hit; public exposure can grow a person's name |
| 5 | Separate audience loyalty from frenzy | Successive records sustain a following; exceptional sustained careers can generate phenomena; renewed demand can overcome an old cooldown |
| 6 | Remove hidden outcome enforcement in controlled stages | Exceptional genres/labels can outperform history through actual mechanisms; album demand follows access and audience rather than a tier badge |
| 7 | Build rival monitoring on retained public evidence | A player can follow one rival from first regional notice through multiple releases, lineup changes, mature fame, and decline |

The ordering is about dependencies, not authorization to implement. Correcting accounting or removing historical guards changes the baseline on which many constants were tuned. Each change should be measured rather than offset by an immediate new hidden multiplier.

## Acceptance scenarios for the intended game

These would be more useful than accepting a decade merely because aggregate genre shares land near historical targets:

1. **The established adult star:** on January 1, 1960 a veteran has a credible age/career/catalog, broad adult recognition, recognizable musicians, and prime-time opportunities despite no current top-25 single.
2. **The local surprise:** a record starts in one market, peaks there first, then spreads through actual radio, distribution, press, and audience contact. A different region can reject it or adopt it months later.
3. **The career under watch:** the player marks an unsigned or rival-signed act and can follow its first local crowd, breakthrough record, next release, label relationship, and lineup changes without the old evidence disappearing.
4. **The player's emerging star:** the same public recognition rules apply to player and rival releases; audience growth improves opportunities, while pressing, distribution, and payment timing remain real constraints.
5. **The durable hitmaker:** several good records build familiarity and repeat demand without requiring a number one or a constant teen frenzy.
6. **The famous departing singer:** the old group's name retains appropriate value, the singer carries their own recognition into a solo act or new group, and media/public histories identify who actually left.
7. **The exceptional phenomenon:** a rare act can sustain extraordinary public response across releases and markets. Rarity emerges from audience response and competition; it does not depend on an unreachable conjunction of weekly gates.
8. **The revival:** a previously successful record can find a renewed audience without its original release-age clock making that response ineffective.
9. **The alternate decade:** a successful player or rival can change which acts, labels, and genres dominate. Historical setting remains recognizable without stations secretly correcting earned success back toward a recap table.

The present game has many of the required components. The largest gap is their causal agreement: the label's file, the regional audience, the media, the artist's own memory, and the player's view need to describe the same career.

## Reproduction notes

Raw new runs are under `C:/Project/Label-Man/SimLogs/celebrity-audit-20261015-1001-*` and `...-2002-*`. The separately named first-week world is `C:/Project/Label-Man/SimLogs/worlds/celebrity-audit-20261015-1001-first-week.world.json.gz`. Existing source datasets were read without alteration.

The Python script uses the standard library only. For example:

```powershell
& 'C:/Users/grohl/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe' `
  SimTools/analyze_celebrity_audit.py --prefix SimLogs/media-p3-hot36 `
  --output SimTools/CelebrityAudit-regional-evidence.json
```

For a saved world, use `--world <absolute-or-workspace-path>` instead of `--prefix`. Add `--artists-only` to avoid reading the active-record section. The script streams large arrays and CSVs rather than loading an entire world into memory. Its top-eleven and rank definitions are documented in the JSON outputs. Build and game-run results are distinct from the offline analysis: the former validate execution, while the latter measure the selected outputs.
