# Media Atmosphere — Phase 0 recon and control baseline

**October 15, 2026, branch `media-atmosphere`, code at `2ce304b` (directive committed as `a358946`).**
Audit of every seam `MediaAtmosphereDirective.md` §1 names, plus the §16 Phase 0 items (trade bonuses,
content tags, station refusals, rights, diary/money). No code was changed. Program-registry research
(exact show dates, performance methods) is deferred to Phase 1, where the catalog is built.

## 1. Seam-by-seam findings

| Seam | What the code actually does | Consequence for this directive |
|---|---|---|
| `MediaInfrastructure` (`Data/MediaInfrastructure.cs`) | `tvMarketRank`, `hasLocalMusicShow`, `bandstandReach` are declared and exported, but **no code or resource ever assigns or reads them**. Every region holds the defaults (0 / false / 0). Live readers: `radioReach` (ChartManager.cs:1688, ChartSimulator.cs:1248), `payolaSusceptibility` (PayolaLedger, StationNetwork, PlayerDesk), `jukeboxCount`/`concertVenueCount` (PlayerStopFactory). | The TV fields are not "geographic priors": they are empty. Phase 1 must author per-region TV data itself (in the new catalog, not by back-filling these fields), or the local-show footprint has nothing to stand on. |
| Name collision | `ExternalMediaService` / `ExternalMediaProfile` own film/stage soundtracks (`Systems/ExternalMediaService.cs`). No `MediaAtmosphere` type exists. | Use `MediaAtmosphere` as directed. Also avoid `LiveRepertoire.ExternalMedia(...)`, which means soundtracks. |
| Trade review desk (`Systems/TradePress.cs`, `PlayerDesk.TradePress.cs`) | Player-only. One submission per record, resolved 1-2 weeks later. Outcome is weighted by hook/production/reputation and drawn with **global `GD.RandRange`/`GD.Randf`** (lines 53, 73). A pick lasts 4 weeks (`TradePickWindowWeeks`). | Fine for AI identity (no AI path), but any desk refactor must keep the same draws in the same order, or player-run RNG shifts. Named desks are presentation only (§8.1). |
| Trade bonuses (same file, lines 157-196, 199-216) | Distributor-facing only, no consumer awareness. They **add up with no shared ceiling**: mailing landing = pick (≤.25) + breakout (.15) + ad (≤.22), up to +0.62. Inbound-call multiplier = 1 + pick (≤1.5) + breakout (1.8) + ad (≤1.6), up to 5.9×. House accept takes the catalogue max (≤.30). Ad connect bonus ≤.14. Readers: PlayerDesk.cs:3948/3967, PlayerDesk.cs:4465, PlayerDesk.Mailing.cs:42, PlayerDesk.Rolodex.cs:147/320/334. | The §8.1 "shared B2B ceiling per title/window" **does not exist today**. Rule: three named desks must route through these same four functions so they can't multiply the bonuses. Any new ceiling would change existing player behaviour, so it needs its own decision. |
| Trade paper (`PlayerDesk.TradePaper.cs`) | Observational; the header asserts no economic writes and no `GD.Rand*`. It reads the current chart, movers, rival breakouts and a capped rival-signing queue. | Good base for the morning media digest. Keep the same contract. |
| Radio (`Systems/Radio/*`, `RecordServicing`, `StationAdvocacy`, `PlayerDesk.RecordHop`) | Stations, DJs, advocacy, servicing, payola, record hops and `RadioEra` (pure date functions: regulatory heat, Boss radio, FM) all exist. **None of these exist:** station refusals/bans, listener requests or dedications, interviews or station visits, premieres/exclusivity, dayparts (the time-of-day hits are Rolodex call scheduling). | Every §11 radio row is new work except hop/advocacy reuse. The "station refusal" adapter has no existing switch to gate. It would be the first supply-side refusal in the radio stack. |
| Rolodex / contacts | `RolodexEntry` is DJ-shaped (`djId`, `stationId`, payola flags). `RolodexDirectory` already projects other roles (`Manager, Publisher, Plant, Distributor, Dealer, Booker, Engineer`) from their own systems. `ContactNetwork*` is musician-to-musician only (`FormerBandmate, Session, HouseBand`). | Media contacts should be a new `RolodexRole` with their own entry type, projected the way Booker/Dealer are. Don't reuse `RolodexEntry` (it would inherit payola) or `ContactNetwork` (wrong semantics). |
| `AudienceSegment` / `SegmentCapacityModel` | Present (not re-audited in depth). The directive's claim that there is no segment-level clearing API holds: clearing is regional (below). | Segments may only weight requests, as §12.3 says. |
| Recognition (`ArtistRecognition`, `ArtistRecognitionService`) | Off by default (`ChartManager.artistRecognitionEnabled = false`). Flags: `--enable-` / `--disable-` / `--observe-artist-recognition`. When **enabled**, it is causal: it adds a radio lift and a stock multiplier inside `CompetitorManager.ApplyReleasePromotion`. It has its own RNG namespace and draws nothing. | Media must not write `publicRecognition` / `culturalStanding` (§2, §12.4). Note: if recognition is ever switched on by default, media exposure must still not feed it. |
| `criticalAcclaim` | **No longer dead.** `ArtistCriticalAcclaimService.OnChartRunComplete` writes it (line 82) from craft + commercial scores. | The older memory note is stale. Critics stay separate from this pipeline (§8.4). |
| `Musician` | Has the performance inputs the directive wants: `stagePresence`, `vocalPower`/`vocalControl`, `isLeadVocalist`, `reliability`, `fatigue`, `substanceLoad`, `personalRecognition`, `liveReputation`, `creativeReputation`, identity axes. | Read these per performing member (§6). Don't add a charisma stat. |
| Public incidents | `BandLifeService` emits person events (`OnEvent`, `EmitPerson(... "bust" ...)`, `substance-onset`, etc.) keyed off `BandLife.Unit`. | Busts are a real, observable controversy seed (§7.4). `substanceLoad` itself must stay private (fixture 8). |
| Content tags | **Completely empty.** `SongComposition.contentTagIds` has no writer (only two SimTools probes read it). `PolarSongTags.content` is never set: `ClassifyLegacyTags` fills production/instrumentation/scene/release only, and the authored Polar tables contain no `content` lists. The only content-like truth is `SongContentContext {Unknown, Sacred, Secular, Mixed}`. | Standards queries (§7) have **no lyric data to read**. A content-authoring pass (vocabulary, aliases, evidence, coverage) has to come before censorship. Until then, every song's content is Unknown, which per §7.1 means no query. |
| Managers (`Data/ManagerProfile.cs`) | Static lookup table, never ticks: `None, LocalHustler, Shark, Svengali, Visionary`, with advance/royalty/difficulty/aura/control/publishing modifiers. | Matches the directive's discipline. Map these five onto the §13 policy tendencies. Don't add a sixth agent-like manager. |
| Creative control | `SimulatedArtist.artistCreativeControl` (bool). The player's studio gate honours it. | Read it as the consent gate for edits and image demands (§7.3). |
| Market seam (`ChartManager.CalculateLiveRegionalSalesWithMarketClearing`, line 1140) | Per record × region, `serviceable` is clamped to `min(unitsInStores, storeCapacityThisWeek)` (1171-1173), written to `data.serviceableIntentThisWeek`, and appended to `intentsByRegion`. Stage A (1180+) sums serviceable singles into `CalculateFormatClearingBudget`, then `AllocateProportionalLocal`. Stage B is bounded spillover. The demand loop's order is fixed to preserve the sales-jitter RNG sequence (1145). | **The §12.3 insertion point exists exactly as described: between line 1177 and 1179.** If the per-region singles total is conserved, the format budget is unchanged. The adapter must rewrite both `intent.Serviceable` and `data.serviceableIntentThisWeek`, and draw no RNG. This path runs only when `genreMarketLive`. The legacy branch (980+) has no intents, so the adapter must be inert there. |
| Weekly order (`ChartManager.OnWeekEnded`, line 655) | `SimulateWeek` → `FreezeCompletedWeekSettlement` → `CompetitorManager.BookCompletedWeekSettlement` → `OnWeekSettlement` (audit must acknowledge). `TimeManager.OnWeekEnded` also has separate subscribers in CompetitorManager, PlayerDesk and RosterManager, which run in subscription order. | The media freeze must be an **explicit call inside `ChartManager.OnWeekEnded` before `SimulateWeek`**, never another `OnWeekEnded` subscriber (§15.1). Post-settlement media updates go after `OnWeekSettlement`. |
| AI promotion envelope (`CompetitorManager.cs` ~1925-1951, `ApplyReleasePromotion` 3706) | `totalCost = productionCost + marketingBudget`, debited once from `cashReserves`, tallied in `WeeklyMarketingSpend`. `ApplyReleasePromotion` turns `marketingBudget` into `awareness`, and campaign/payola/recognition into `radioHeat` and initial stock. Album and promo-single paths call it too (2174, 2179). | `AiMediaAllocation` is a read-only split of `marketingBudget` per release (§12.2). No new debit anywhere in this function. |
| Performer money (`MemberWealthService`, `LocalSceneRoomService.Bookings`) | Wealth accrues **annually** (`OnActYear`, `OnPooledYear`). Gig pay after 5B belongs to performers, and the label takes only a booking commission. | There is no per-event payment API. Player TV fees (§5.4) need a small per-event accrual that folds into the act-year pass, or they must ride the gig-pay route. Decide in Phase 4. AI fees stay estimates. |
| Player diary | There is no unified calendar. Scouting meetings, scene-room bookings, studio sessions and station stops each keep their own records and conflict checks (e.g. `OfferResourcePlan.AppointmentRisk`, `ScoutingItineraryRisks`). | TV dates (rehearsal, taping, travel) need to register in each existing conflict check, or a shared calendar query has to come first. That is a Phase 2 prerequisite. |
| Deterministic draws | Pattern to copy: `BandLife.Unit(key)` = `RepertoireTaxonomy.Unit(WorldSeed + "|band-life|" + key)`. It is keyed, seed-aware and touches no stream. Also `ArtistRecognition.RngNamespace`. | `MediaAtmosphere.Unit(key)` should follow this with a `"|media|v1|"` namespace (§15.1). |

## 2. Pre-existing problems found during the baseline (not caused by this branch)

1. **`--scene-room-audit` is red at world start** on both seeds: `remoteDays=94` (s1001) / `72` (s2002). The census
   (`ChartAuditRunner.SceneRooms.cs:14,36`) still forbids one person playing two rooms in a day. The later
   working-week rule ("double rooms": a full-time act can play a second room in its town) allows exactly that.
   The invariant is stale, not the scheduler. The control refs were taken without this flag.
2. **`--scouting-commitment-planning-ui-check` (seed 1001) fails** at "Player must retain the choice to hear a
   conflicting set", on `bdac2df` as well. Unrelated to media. The contract-ledger check
   (`--scouting-offer-planning-ui-check`) passes.

## 3. Control baseline (frozen)

Per the reuse-control-runs convention: one 104-week reference set per seed at this base, for hash-based inertness
checks in the observe-only phases. Decade controls were deliberately **not** run. They wait until a causal phase
needs paired runs.

| Ref | Seed | Weeks | Flags |
|---|---|---|---|
| `SimLogs/ref-a358946-1001-104/` | 1001 | 104 | `--enable-genre-market-v2 --enable-artist-population-lifecycle --aggregate-only --log-band-life-members --contact-network-audit` |
| `SimLogs/ref-a358946-2002-104/` | 2002 | 104 | same |

Inertness gate for Phases 1-3: rerun a treatment with the same flags plus the media switch (disabled, then observe), and
hash every economy CSV against these refs. Any mismatch is a finding: locate the draw before going further. The
hash manifest is `SimLogs/ref-a358946-<seed>-104/SHA256SUMS.txt`. Both runs printed `CHART_AUDIT_COMPLETE` with exit 0,
92 CSVs each, in 583 s (s1001) and 559 s (s2002) running two-wide. SimLogs is not committed; the refs live locally.

**Seeds.** The directive says "1001 and 1002". The repo's accepted pair is 1001 and **2002**, and every ref set and
decade A/B uses it. These refs follow the repo. **Preregistered holdouts for causal acceptance (§16.2): 3003 and 4004**,
recorded here before any media run exists. They are not to be moved after results are seen.

## 4. Recommended Phase 1 cut

The smallest observe-only slice that touches every hard seam once:
1. `MediaAtmosphere` switch (Disabled / Observe) + keyed `Unit` namespace. Prove that disabled and observe both hash-match the refs.
2. `MediaOutletCatalog` with effective-dated institutions (16, Tiger Beat 9/1965, Crawdaddy! 1966, Rolling Stone 11/1967,
   Voice from 1955, Music Vendor→Record World 1964, Bandstand, Shindig!, Hullabaloo, Sullivan, Hollywood Palace, Smothers)
   + per-region local-TV authoring (replacing the empty `MediaInfrastructure` TV fields).
3. `BroadcastScheduleVersion` + 15-minute viewing curves + exposure accounting (peak / average / viewer-minutes / unique).
4. Read-only paper items fed from AI airings ("booked" vs "aired" distinct).

Hold censorship until content tags are authored (§1, Content tags).
