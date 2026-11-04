# Opening careers — coherent January 1960 history (audit step 2)

**Status: built 2026-10-15** (results in [CelebrityAuditProgress.md](CelebrityAuditProgress.md)). Remedies [CelebrityEconomyAudit-2026-10-15.md](CelebrityEconomyAudit-2026-10-15.md)
§1 and §2. Progress and the decade watch list live in [CelebrityAuditProgress.md](CelebrityAuditProgress.md).

## What the opening world has today (seed 1001, first-week save)

- **No veterans exist.** Of 2,967 signed launch acts, no lead performer is over 42 and the median lead age is 24
  *in every genre* — Traditional Pop 24, Easy Listening 24, Jazz 25, Classical 24. No act formed before 1955.
  Musician ages are 18–29 (85%) or 29–42 at generation (`ArtistManager.cs:562`); `formedYear` is 0–5 years back.
- **The canopy is quality-ranked and genre-blind.** `SeedInitialStarCanopy` stamped 6 Superstars + 24 Stars with a
  median lead age of 21 (five are 18). It holds 4 Folk and 4 Soul stars beside 3 Traditional Pop in 1960.
- **Counters contradict themselves.** `StampStar` sets `charted = top40 + 6..12` independently of `totalReleases`,
  so all 30 have more chart entries than releases (Solomon Reynolds: 42 charted, 28 released).
- **Fame was never initialized.** All 30 have `culturalStanding = 0`; recognition peaks at 0.087; every member's
  `personalRecognition`, `liveReputation` and `creativeReputation` is 0.
- **Recognition is the live launch input.** `artistRecognitionEnabled = true` in `chart_manager.tscn`, so
  `GetNewReleaseAwarenessBonus` reads `EffectiveRecognition × 0.115` *instead of* the old career-state arm
  (Superstar +0.25). A seeded Superstar launches today with ~+0.01, i.e. like a newcomer with good momentum.
- **No past exists to look at.** `historicalRecords` is null; the opening catalog is 1–20 weeks old.

## Principle

Generate one compact **career history** per notable act, then **derive everything else from it** using the rules
the live game already applies. The counters come from the history, recognition and members' fame come from
replaying it through `ArtistRecognitionService`, and the artist file shows it. Nothing is stamped
independently, so the pieces cannot disagree, and no new launch multiplier is added (audit §1: do not reinstate
the 2.5× stock switch).

## Design

### 1. Archetypes (replace the quality-ranked canopy)

Selected from the existing Major/MidTier launch rosters (Independents too for regional names), by genre fit
first and quality second, with the current per-label spread caps. Counts are starting values for the whole
industry (~75 of ~2,970 signed acts).

| Archetype | n | Genres | Lead age 1960 | Career start | History shape | Rung |
|---|---:|---|---|---|---|---|
| Adult standard-bearer | 8 | TradPop, EasyL, Jazz vocal, Country, Classical | 35–50 | 1936–50 | Long singles run, 6–25 LPs, hits spread over 10–20 yrs; current single optional | Superstar / Star |
| Current hitmaker / teen idol | 10 | R&R, TeenPop, DooWop, RnB | 17–26 | 1955–58 | 6–20 singles, 0–3 LPs, hits concentrated 1957–59 | Superstar / Star |
| Established hitmaker | 12 | Any 1960-live genre | 25–40 | 1948–56 | Steady mid-chart career, a #1 or two | Star |
| Declining former star | 10 | TradPop, EasyL, Country, early RnB, Big-band-style EasyL | 38–55 | 1935–48 | Peak 1945–54, little since | Declining |
| Critics' favourite | 15 | Jazz, Folk, Blues, Classical, Gospel | 25–50 | 1945–57 | Few hits, steady LPs, acclaim | Established |
| Regional name | 20 | Country, Blues, RnB, Gospel, Latin | 25–45 | 1945–56 | Regional sellers, little national chart | Established |

Genres must be live in 1960 (`GenreCatalog` birth years): no Soul/Folk superstars unless the catalog says the genre exists.

### 2. Compact career history (new data)

`SimulatedArtist.publicHistory`: a list of `PublicReleaseEntry { year, title, format, peakPosition, weeksCharted,
labelName, regionId? }`. History acts get ~5–60 entries each, a few KB for the whole world. It is **display and
derivation only**: these records have no demand, inventory or chart presence. Titles come from the existing
song-title generator.

This is also the store step 7 needs (audit §12): when a live record retires, its public line is appended here, so
rival discographies stop disappearing. Step 2 creates the store and seeds it; step 7 adds the retirement writer.

### 3. Derivation (all from the history)

- **Counters:** `totalReleases`, `charted`, `top40Hits`, `top10Hits`, `numberOnes`, `weeksAtNumberOne`,
  `consecutiveHits/Flops` are aggregates of the entries, so `charted ≤ totalReleases` by construction. The
  generator keeps redrawing a history until it satisfies the ladder thresholds of its rung, so the runtime ladder
  does not immediately demote or promote the act.
- **Public recognition and members' fame:** replay the history in date order through
  `ArtistRecognitionService.OnChartEntry/OnTop40/OnTop10/OnNumberOne`, applying `PublicRecognitionDecay` for the
  weeks between entries up to January 1960. Because that goes through `AddPublicRecognition`, members receive
  `personalRecognition`, `liveReputation` and `creativeReputation` via `MusicianRecognitionService` exactly as at
  runtime. A rough estimate: an 8-#1 superstar lands ~0.6–0.8 (the UI's household-name band), a declining 1949
  star ~0.25–0.45, a critics' favourite ~0.05–0.2.
- **Cultural standing / critical acclaim:** from archetype (critics' favourites and standard-bearers high), using
  the existing `AddCulturalStanding`, so its decay and caps apply.
- **Momentum / reputation:** from *recent* history (last two years) rather than from rung, so a declining star
  has a famous name and cold momentum.
- **Ages and dates:** shift members' `birthYear` and the act's `formedYear` / `signedYear` to match the archetype;
  then re-derive the age-keyed person values (`MemberAxesService`, identity, growth origin), which are keyed and
  draw-free, so they stay consistent with the new age.
- **Media:** seed teen-idol `MediaFanState` attachment from recent hits (the media layer is still observe-only).
- **Visible:** `ArtistDetailPanel` reads `publicHistory` as the pre-1960 discography and career events.

### 4. RNG and switch

- Generation uses a **keyed RNG** (world seed + artistId), never the shared or population stream. With the
  switch off, the world is byte-identical; with it on, every difference comes from the stamped values, not a
  reshuffled stream.
- Flag `OpeningCareers` (scene default on once it passes a short check; `--no-opening-careers` is the control).
  It supersedes `StarCanopy`; `--no-seed-star-canopy` keeps working as the older control.
- Save: `publicHistory` must round-trip (save version bump; `SaveLoadRoundTripRunner`).

## What it will move (short check now, decade at end of audit)

- **Top-tier launches get stronger.** Recognition at 0.6–0.8 means up to +0.09 awareness, ×1.24 stock and +0.06
  radio at launch for ~20 acts; today they get ~0. This partly restores what the canopy had before recognition
  replaced the career-state arm (+0.25). It goes on the watch list: Major share, #1 tenure, genre shares.
- **Older members.** Members aged 45–60 hit band-life's age-gated exits (`MinimumTerminalExitAge`, retirement,
  pool-joining limits), so some veterans will leave the business during the decade, which is historically right
  but new.
- **Album prior and budgets** read the rung, and declining stars sit lower than today's stamped Stars.

## Out of scope here

- Per-region recognition: recognition is one national stock. A regional name's `regionId` entries are recorded so
  step 3 (regional audience state) can seed local audiences; until then a regional name is modestly known nationally.
- Prime-time booking of famous acts without a current hit (step 4).
- Giving veterans live catalog records in the opening market: the history is display-only. Seeding an active LP
  for standard-bearers is possible but changes `PopulateInitialRecords` draws; propose it separately if wanted.

## Decisions for the author

1. **Re-stamp existing launch acts vs. generate a separate veteran cohort.** Recommended: re-stamp. Roster sizes,
   label allocation and the shared stream stay as they are; the cost is re-aging generated people and re-deriving
   their keyed age values. A separate cohort would add ~75 acts to rosters and is a larger calibration move.
2. **Derived recognition vs. authored targets.** Recommended: derived by replay. The same rules then govern before
   and after January 1960, at the cost of only indirect control over the final values (tuned through history shape).
3. **Archetype counts** in the table: starting values to adjust.
