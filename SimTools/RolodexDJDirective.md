# Rolodex & Player-Character Directive (DJ slice)

Branch work on the core relationship feature: a **Disco-Elysium-style player character**
chosen at startup, and a **Rolodex** of collectible contact cards. This first slice ships
**DJ contacts only** and one verb — *get your disc spun* — starting the player with **zero
contacts**, so the opening move is to **discover** the first DJ.

Two external sketches informed this (`rolodexsketch.odt` = GPT, `gemrolodex.odt` = Gemini).
Both are good on *feel* and both assume systems that don't exist and miss systems that do.
This directive is the codebase-grounded version.

---

## The goal (design intent)

The Rolodex is the human interface to the sim's hidden bottlenecks. You are not talking to a
fully-simulated person; you are talking to *your read of them*, filtered through your
character's instincts, against a real market fact. Every line of dialogue must **reveal a real
sim fact, express a real motive, or execute a real sim action** — never invent flavor the sim
can't back (this is the GPT sketch's core rule, and it is correct).

The strategic fantasy at the start: weak distribution, no known artists, little cash, **no
national leverage, and an empty Rolodex.** You survive on intuition, regional knowledge, and a
single cultivated late-night DJ who will take one spin on your record because you read the room
better than he did. A contact is valuable because they have a **specific, non-duplicated
capability** (the sketch's "rare holographic card" — but economically grounded): a Tastemaker
on a station whose format admits your genre is a cheap door into a market you otherwise can't
touch.

**Scope of THIS slice** (everything else is a named later phase):
1. Player-character selection at founding (traits = the four Executive Instincts).
2. A Rolodex UI: rectangular cards, info + portrait, **mouse-wheel to spin through them**.
3. **DJ contacts only.** Start with **none**; discover the first one through play.
4. One player verb with real teeth: influence a reporter station's willingness to spin your
   record, through the candidacy hooks that already exist.

---

## The reframe — what already exists (do NOT rebuild)

The sketches propose a large new `RolodexContact` model, a `StationAdvocacy` object,
per-contact stat blocks, and a `RolodexCallContext`. **Most of this is already in the tree,
under different names, and already wired into the live market.** Build the Rolodex as a
*view + interaction layer* over these, not a parallel economy. (Same discipline the Publishing
branch used: extend the real settlement, never fork it.)

### 1. The DJ is already a live, causal object — `Deejay`
[Systems/Radio/Deejay.cs](Systems/Radio/Deejay.cs). One `Deejay` per reporter station
(`RadioStation.leadDjId`). It already carries exactly the stats the sketches want to invent:

- `DJArchetype` ∈ { `Personality`, `Tastemaker`, `Hustler`, `CompanyMan`, `Regional` }
- `influence`, `taste` (discovers quality ahead of sales), `greed` (payola receptiveness),
  `ego` (wants courting, not just cash)
- `genreAffinity` (per-genre taste skew), `labelRapport`, `suspicion` (regulatory heat)

Archetype→stat tables and the per-format archetype draw already exist
([StationNetwork.cs:408-441](Systems/Radio/StationNetwork.cs)). This maps the GPT sketch's
`ContactArchetype` onto **real objects that already change what gets played.** Do not
re-roll a second personality model on top of it.

### 2. "Get the disc spun" already has TWO dormant hooks in the candidacy meeting
The weekly reporter playlist decision
([StationNetwork.Playlist.cs](Systems/Radio/StationNetwork.Playlist.cs)) scores every eligible
single per station:

```
candidacy = formatMatch × qualityTaste × salesSupport × relationship × payola
            × freshness × heatPull × vitality × chartGuard   (line ~436)
```

Two of those terms are **player-only and currently inert**:

- **`relationship = 1 + rt.Rapport(labelId)*0.5 + rt.Loyalty(artistId)*0.3`**
  ([StationNetwork.Playlist.cs:425](Systems/Radio/StationNetwork.Playlist.cs)).
  `rt.Rapport` reads `StationRuntime.labelRapport`
  ([StationRuntime.cs:23,43](Systems/Radio/StationRuntime.cs)), described in the source as
  *"the cultivation surface."* **Nothing writes it today** except a payola bust, which
  *removes* it. The code comment literally says *"Phase 4 cultivation writes these; 0 until
  then."* **This is the Rolodex's primary lever.** A personal pitch / favor / relationship
  action writes `rt.labelRapport[playerLabelId]`, and the record's chance of being spun goes
  up **through the real meeting** — format, sales support, genre fit and burn all still apply.
  You open the door; the market decides whether the record stays in the room. (Exactly the GPT
  sketch's `StationAdvocacy` intent — but the plumbing is already here.)

- **`payola = 1 + clamp(ActivePayolaLookup(recordId, stationId), 0, 1.5)`**
  ([StationNetwork.Playlist.cs:430](Systems/Radio/StationNetwork.Playlist.cs)), fed by
  `StationNetwork.ActivePayolaLookup` ([StationNetwork.cs:41](Systems/Radio/StationNetwork.cs))
  → the **already-complete** `PayolaLedger`
  ([Systems/Radio/PayolaLedger.cs](Systems/Radio/PayolaLedger.cs)). It has `PlaceCash`,
  `PlaceCutIn`, `PlaceIndiePromoter`, decay/expiry, full scandal adjudication with teeth
  (`SackDeejay`, region poisoning, destroying cultivated rapport). It reads `dj.greed`,
  `dj.ego`, `station.payolaSusceptibility`. **It has no UI.** The Rolodex is its front end.

### 3. A legacy contact scaffold exists — mine it for intent, do NOT build on it
[Data/Contact.cs](Data/Contact.cs), [Data/ContactRuntimeData.cs](Data/ContactRuntimeData.cs),
[Data/InteractionRecord.cs](Data/InteractionRecord.cs), and the Contact-specific enums in
[Data/ContactEnums.cs](Data/ContactEnums.cs) (`ContactType`, `ContactCategory`,
`RelationshipTier`, `AvailabilityStatus`) are **dead code from an earlier version of the game**
(per the author) — grep-confirmed referenced by nothing but each other. **Do not build the
Rolodex on this scaffold; build fresh** (around the live `Deejay`, per The Model). Read it only
for design intent worth carrying forward:

- the *idea* of a `portrait` on a card, a `DisplayName`/nickname, a discovery flag, relationship
  **tiers** (Burned→InYourPocket) with a color/label, and favor-owed direction.

Treat these four files as **cleanup**: fold whatever ideas survive into the new model, then
delete them. **Surgical caveat** — `ContactEnums.cs` *also* defines `LabelTier`,
`LabelArchetype`, `LabelStatus`, `LabelPopulationOrigin`, `LabelOperatingTargetReason` and
`ArtistType`, which **are live across the tree** (e.g. `FoundLabel`). Remove only the four
Contact-specific enums from that file; leave the rest.

### 4. The player loop it hangs off — `PlayerDesk` / `PlayerDeskPanel`
[Systems/PlayerDesk.cs](Systems/PlayerDesk.cs) (~2100 lines): founds a real `AILabel`, runs an
hour-driven scout→sign→write→cut→release→trunk-distribute loop, raises `Changed`, keeps a
`Log`/`Note()`, handles save/load and game-over. `FoundLabel(name, cityId)`
([PlayerDesk.cs:424](Systems/PlayerDesk.cs)) hardcodes `founderName="You"` and a fixed stat
block (`scoutingAbility=0.5`, etc.) — **there is no character system yet.**
[UI/PlayerDeskPanel.cs](UI/PlayerDeskPanel.cs) is entirely **code-built** (helpers `Heading`,
`Body`, `Btn`, `Option`; a tab strip A&R/Roster/Catalog/Distribution/Finances/Office; page
routing in `Refresh()`; a `ScrollContainer`). Founding is `PageFounding()`
([PlayerDeskPanel.cs:262](UI/PlayerDeskPanel.cs)). The trait picker and the Rolodex tab slot
into this pattern — no scene edits.

---

## Code-verified findings (checked against the tree)

- **Panel weight is small, and that is fine.** The reporter panel is ~13% of airplay
  (memory: `hot100-panel-is-whole-dial`, `radio-station-network-branch`), and each region has
  only **6–11 reporter stations** (`ReporterCountForTier`,
  [StationNetwork.cs:20](Systems/Radio/StationNetwork.cs)). So cultivating **one** DJ moves the
  needle a little, once. This is the correct starting-out magnitude ("one spin on a late-night
  AM broadcast") and must be stated honestly in the UI — never sell a rapport point as a
  guaranteed hit. Scaling comes from collecting *many* cards across regions, not from one being
  strong.
- **The player's label already charts on the same rails** as every AI label
  ([PlayerDesk.cs:6-16](Systems/PlayerDesk.cs)), so its singles are already in the candidacy
  sweep. No new "make my record eligible" work is needed — only the two hook-writes.
- **Determinism is load-bearing and must be preserved.** Candidacy uses the network's own RNG,
  and both hooks are player-only, so headless AI audits are byte-identical with them at zero
  (payola: [PayolaLedger.cs:50-56,131](Systems/Radio/PayolaLedger.cs); rapport: unwritten). The
  Rolodex must keep this: **every write it makes is gated to the player label / a player
  action**, and it must never perturb an AI label's decisions or read the global `GD` RNG
  inside the weekly sim. (Calibration-run guard: `--calibration` and probe byte-comparison,
  memory `probe-run-byte-comparison-proves-inertness`.)
- **DJs have placeholder names** (`djName = $"DJ {callsign}"`,
  [StationNetwork.cs:411](Systems/Radio/StationNetwork.cs)); a real `NameGenerator` engine
  exists (`Systems/Naming/`). Real DJ names are part of making a card worth collecting.
- **No portrait art exists** anywhere in the tree. Portraits must be a small authored set +
  a fallback (see Phase 2), not blocked on bespoke art.
- **The four "Executive Instincts" are not in the tree at all** — new player state.

---

## The model

### A. The player character = four Executive Instincts (new state on `PlayerDesk`)

Adopt the sketches' voices verbatim; they are good and they map to real sim reads:

| Instinct | Reads (real sim facts) | Drives (actions) |
|---|---|---|
| **THE EAR** | record hook/production/originality vs the DJ's *stated* objection; `dj.taste`, `genreAffinity`, genre fit | Personal Pitch success |
| **THE STREET** | regional genre acceptance & momentum; whether the DJ is bluffing about local taste | Rival-Pressure / reading the room |
| **THE SUIT** | station reach, expected units, cost, the label's own business stats | Commercial (ad-buy) pitch; also the label's cash-side competence |
| **THE FIXER** | `dj.greed`, `dj.suspicion`, vulnerability, minimum effective bribe, scandal risk | Payola; (blackmail — later) |

```csharp
public sealed class ExecutiveInstinctProfile {   // serializable; lives on PlayerDesk, saved/loaded
    public int theEar, theFixer, theSuit, theStreet;   // small ints, e.g. 1..6, points-buy at founding
}
```

**Founding archetypes** (the Disco-Elysium "who were you before this?" pick). Each sets an
instinct spread **and** the founding label stats that `FoundLabel` currently hardcodes, and —
per the user — **all start with an empty Rolodex** in this slice (starting-contact differences
are a later phase). Ship 3–4:

- **The Pawn-Shop Owner** — *knows money, not music.* High SUIT/FIXER, low EAR/STREET. More
  founding capital / better cash discipline, worse `scoutingAbility` and read accuracy.
- **The Ex-Musician** — *good ear, bad business.* High EAR/STREET, low SUIT. Tight reads on
  records and rooms, thin capital and a shorter credit line.
- **The Promo Man / Hustler** — *works the phones.* High FIXER/STREET. Cheapest payola reads,
  best at turning up contacts, but burns hot (more scandal exposure).
- *(optional)* **The Trade Insider** — balanced SUIT/EAR; the "normal" start.

Instincts gate **which reads and options appear** (low FIXER = you don't even see the "there's
cash in the sleeve" line) and **modify success chances** — the Disco-Elysium move of *skills
surfacing options*. They do not reveal exact hidden numbers; a successful read returns an
**interpreted tier** (Hint / ClearRead / DeepRead), never "greed = 0.71" (GPT §8). Exact values
stay in a debug overlay only.

### B. The DJ contact = a fresh, thin wrapper that BINDS to the live `Deejay`

Do **not** copy the DJ's stats into the Rolodex, and do **not** revive the legacy scaffold.
Build one small new player-side type that binds to the live objects:

```csharp
public sealed class RolodexEntry {   // new; lives on PlayerDesk, saved/loaded
    public string djId;              // → StationNetwork.GetDeejay(djId)  (the causal object)
    public string stationId;         // → reporter station whose labelRapport is the real lever
    public DiscoveryState state;     // Unknown → HeardOf → Introduced → Known → Trusted
    public string portraitKey;       // archetype(×look) key into the portrait set (Phase 2)
    public bool youOweThem, theyOweThem;   // favor direction (Phase 5)
    public List<string> log = new();       // interaction history for the card
}
```

- **Causal truth** lives on the live objects, never duplicated here: `Deejay`
  (taste/greed/ego/archetype/genreAffinity) and `StationRuntime.labelRapport[playerLabelId]`
  (the candidacy relationship term).
- **The card's "relationship" reading is derived, not stored** — read `rt.Rapport(playerLabelId)`
  at render time and classify it into a tier/label/color for the card. One source of truth (the
  meeting), never two. (Carry forward the legacy tier *idea* — a fresh
  `ClassifyRapport(float) → tier` helper — without the legacy type.)
- **Real name + identity are synthesized at discovery**: a real DJ name via `NameGenerator`
  (today it's `"DJ {callsign}"`), the station's city/format/archetype, and an archetype-derived
  `portraitKey`. No `Contact` Resource is needed for procedural DJs; authored `.tres` contacts
  are a later concern for hand-made historical figures.

### C. The action model — verbs, not flavor (GPT §11), all routed through the real meeting

First slice, DJ-only:

| Verb | Instinct | Real effect (the write) |
|---|---|---|
| **Personal Pitch** | EAR | on success, `rt.labelRapport[playerLabelId] +=` small; scaled by `dj.taste`, `genreAffinity`, record quality, current rapport |
| **Ad-Buy / Commercial Pitch** | SUIT | costs cash; adds rapport (and, later, a regional-awareness nudge); weaker personal-trust gain |
| **Payola** | FIXER | `PayolaLedger.PlaceCash(recordId, playerLabelId, stationId, budget, …)` — the payola term; full scandal risk already implemented |
| *(later)* **Ask a Favor** | relationship | requires `theyOweYou`; guaranteed moderate rapport, spends the favor |

Each resolution is **logged and auditable** (GPT §12): chance, roll, cost, rapport delta,
scandal risk, resulting station effect → surfaced in the card's `history` and the desk `Log`.
No hidden "narrative rolls." A pitch is bounded and expiring in effect because rapport decays
and the record still has to sell (`salesSupport` in the same product) — payola *"can't save a
dog"* is already true in the ledger.

### D. Discovery — the opening loop (start from zero)

The player founds a label with an **empty Rolodex**. To get anyone spun they must first find a
DJ. Two routes; ship the **active** one first so a zero-contact player has agency:

1. **Active — "Work the phones / make the rounds"** (new desk action, costs hours like
   scouting). In your current region, surfaces one reporter station's DJ as a card at knowledge
   state *Heard-Of* → *Introduced*. Higher STREET/FIXER turns up better-placed DJs and reveals
   more on first contact (perception-gated, mirroring `ScoutingPerception`'s fog, memory
   `scouting-mechanic-directive`).
2. **Passive — a station finds YOU** (Phase note): when a reporter station in your region first
   commits one of your records to its playlist (it already can — `DecideStationPlaylist`), fire
   a one-off "so-and-so has been spinning your side" discovery. Free, and it teaches the
   relationship's direction (they came to you because the record was working).

Knowledge states (the sketch's ladder, as `RolodexEntry.state`): **Unknown → Heard-Of →
Introduced → Known → Trusted.** (Unknown DJs simply have no entry yet.) Higher states unlock more
reads and more verbs. The Rolodex is a campaign map you fill in, not a menu handed to you.

### E. The card UI (the "rolodex" itself)

A new **ROLODEX tab** in `PlayerDeskPanel`, built in code like every other page:

- **Rectangular cards**, one focused at a time: portrait, display name ("Wolfman" Larry Bell),
  role/station/format/city, the derived relationship label + color (from `ClassifyRapport`), a
  one-line archetype read, and the known genre affinity.
- **Mouse wheel spins through them** — advance/retreat one card per wheel notch, the focused
  card enlarged, neighbors peeking (a Rolodex flip, not a scroll list). Implement by handling
  the wheel in the tab and re-rendering with a selected index; the existing `ScrollContainer`
  wheel-scroll is suppressed on this page.
- Selecting a card opens the **call view**: the assembled scene (opening line by
  archetype+relationship, the instinct-gated passive reads, the verb buttons, the resolution).
  Dialogue is **fragments keyed by tags** (archetype / relationship / a boolean sim condition),
  string-interpolated (GPT §9-10, Gemini §3-4) — no branching trees. A tiny fragment library
  ships in this slice; it grows per contact type later.
- **Portraits:** a small authored set keyed by archetype (× a coarse look variant), with a
  silhouette-with-monogram fallback so an un-arted DJ still gets a legible card. Full portrait
  art is deferred, not blocking.

---

## Phased plan (ordered low → high determinism/calibration risk)

Each phase is independently shippable and leaves the headless audit byte-identical.

### Phase 1 — Player character at founding (no radio yet)
`ExecutiveInstinctProfile` on `PlayerDesk`; a founding archetype picker in `PageFounding`
(before "OPEN THE DOORS"); archetypes set instincts + the founding stat block + capital;
persist through save/load ([SaveGameService.cs](Systems/SaveGameService.cs)). No Rolodex, no
radio effect yet — just proves the character exists, shows on the OFFICE page, and round-trips.
**Verify:** save/load round-trip runner ([SimTools/SaveLoadRoundTripRunner.cs](SimTools/SaveLoadRoundTripRunner.cs))
green; headless decade byte-identical (character is player-only).

### Phase 2 — Rolodex UI + discovery, read-only (no interventions)
New `RolodexEntry` model + ROLODEX tab; wheel-spun cards; the active "work the phones" discovery
action; bind cards to live `Deejay`s; synthesize real DJ names via `NameGenerator`; portrait set
+ fallback; call view shows opening line + instinct-gated **passive reads only** (interpreted
tiers, GPT §8). No verbs that change the sim yet. **Cleanup in this phase:** delete the legacy
`Data/Contact.cs`, `Data/ContactRuntimeData.cs`, `Data/InteractionRecord.cs` and the four
Contact-specific enums in `Data/ContactEnums.cs` (leaving the live label/artist enums). **Verify:**
project still builds after the deletion; discovery surfaces only in-region reporter DJs; reads
match the live `Deejay` values at the right tiers; headless still byte-identical (pure reads).

### Phase 3 — Personal Pitch + Ad-Buy: write `labelRapport`
The two non-payola verbs. On success, write `rt.labelRapport[playerLabelId]`; mirror into the
card; log the resolution. This is the first phase that **moves the chart** — through the real
candidacy meeting, bounded by panel weight. **Verify:** a cultivated DJ's station raises the
player record's `candidacy`/spin tier for a genre its format admits, and *does not* for a genre
it doesn't (`formatMatch` still gates); rapport decays; AI labels unaffected; headless
byte-identical (writes are player-label-only).

### Phase 4 — Payola via the existing ledger
Wire the FIXER **Payola** verb to `PayolaLedger.PlaceCash` (and later `PlaceCutIn`). Surface
`ActivePayolaLookup` is already set by ChartManager. Expose scandal outcomes
(`pendingScandals`) in the desk log; make the teeth legible (a bust sacks the DJ and *burns the
card* — `isBurnedBridge`, and the ledger already removes the cultivated rapport). **Verify:**
payola boosts candidacy on the target station only; scandal fires at the era-appropriate rate
and destroys the relationship; headless byte-identical (ledger is inert with no player actions).

### Phase 5 — Relationship depth: favors, memory, "burned" nuance
`theyOweYou`/`youOweThem`, contact **record-memory** ("you said it would move; it sold forty
copies" — grounded in the actual settlement, GPT §22-23), and channel-specific burn
(burned-for-payola ≠ burned-professionally, GPT §21). Trusted state unlocks introductions →
the first **networked discovery** (a DJ refers another), turning the Rolodex into a graph.

### Later (named, out of this slice)
Generic contact types (program director / station manager as *separate* cards on the same
station, GPT §18; then publishers, distributors, critics, promoters — GPT §24-25); the
`thePen`/`theRoom`/`theWire` instincts; starting-contact differences per founding archetype;
authored historical contacts as `Contact` `.tres`.

---

## Determinism & calibration flags (the ones that will bite)

- **Player-only, always.** Every rapport/payola write is gated to `playerLabelId` and to an
  explicit player action. Never write AI-label rapport, never call these from the weekly sim,
  never read the global `GD` RNG inside candidacy. Prove inertness with a probe byte-comparison
  (memory `probe-run-byte-comparison-proves-inertness`) after Phases 3 and 4.
- **Rapport is uncapped in the read but bounded in effect** — `relationship` multiplies at
  weight 0.5 and payola is clamped to +1.5. Do not let a cultivation action write unbounded
  rapport; a runaway would let one DJ manufacture hits and break the "you open the door"
  contract. Cap per-action gain and let decay do the rest.
- **Panel weight sets the ceiling, not the code.** One DJ ≈ one small nudge on ~13% of airplay.
  If Phase 3 feels weak in playtest, the fix is *more cards / regional coverage*, or the
  reporter-panel weight, **not** a bigger per-DJ multiplier. Re-tuning the multiplier to feel
  good in isolation would distort the AI economy the panel is calibrated against.
- **Save/load surface grows each phase** — new player state (`ExecutiveInstinctProfile`,
  rolodex entries, discovery states, favor flags). Extend `SaveGameService` and re-run the
  round-trip runner every phase; a dropped field is a silent regression.

---

## Open questions to settle before Phase 1

1. **Points-buy vs fixed spreads** for the founding archetypes — a hand-tuned spread per
   archetype (simplest, most legible) or a small points-buy the archetype seeds?
2. **How much does an archetype change founding capital / credit line?** The Ex-Musician being
   poorer is thematic but interacts with the existing 3-month-red loss condition
   ([PlayerDesk.cs:271-284](Systems/PlayerDesk.cs)) — needs a playtest bound.
3. **Discovery cost & yield** — hours per "work the phones", and how many of a region's 6–11
   reporter DJs are findable early (all, or gated by STREET so the map fills slowly)?
4. **Portrait scope for v1** — how many authored archetype portraits before the silhouette
   fallback carries the rest?

---

# Phase 6 — Scene architecture, station advocacy, earned access, sub-hour clock

Playtest note that drove this: the call read as *"I pressed PERSONAL PITCH and he said yes."*
Four fixes, all of them things the earlier phases deferred or got wrong.

## 1. Calls are scenes, not buttons (GPT sketch §9-13)

New files: [Systems/RolodexScene.cs](Systems/RolodexScene.cs) (beats, context, conditions,
fragment library), [Systems/RolodexCall.cs](Systems/RolodexCall.cs) (live scene state, shifts),
[Systems/PlayerDesk.Rolodex.cs](Systems/PlayerDesk.Rolodex.cs) (discovery, context build,
connection), [Systems/PlayerDesk.RolodexVerbs.cs](Systems/PlayerDesk.RolodexVerbs.cs)
(approaches, pushback, counters, resolution, weekly settlement).

A call runs the sketch's beat sequence: **Opening → PassiveRead → SituationRead → PlayerPitch →
Pushback → ActiveCheckPrompt → Success/Failure → RelationshipAftermath → Exit.** The scene lives
on `PlayerDesk.ActiveCall` (not the panel) so a UI refresh cannot drop it mid-sentence.

`RolodexCallContext` is built **once** per call from live sim values and is the only thing
fragments and conditions may read — `RolodexConditions.Meets` is ~35 named predicates over it.
A selected line is therefore auditable: the condition that chose it has a name.

**The counter is the point.** He raises ONE objection, drawn from the most severe thing actually
true about this record at this station (`PickObjection`). You then answer *that* objection with
an option gated on **two** things: an instinct score high enough to think of the answer, and the
underlying fact being true. Where the instinct is there but the fact is not, the option is still
offered — **labelled a bluff** — and he rolls to catch it on taste, archetype (`Regional` knows
his own market) and rapport. Getting caught costs rapport and auto-fails the approach.
A grounded counter is worth roughly double `PressIt`; `SuitLateNight` (ask for a smaller yes) is
worth the most, because it is a smaller ask.

## 2. What a won pitch actually buys — `StationAdvocacy`

The earlier phases wrote only `StationRuntime.labelRapport`, which is **label-wide**: pitching a
specific record bought a general warmth that helped everything you released equally. The sketch's
`StationAdvocacy` (§6) was rejected in the original directive; it was needed after all, because it
is the only thing that answers *"did he put it in rotation?"*

[Systems/Radio/StationAdvocacy.cs](Systems/Radio/StationAdvocacy.cs): a bounded, expiring,
**record-specific** commitment, consumed by the candidacy meeting as a term in the product
alongside payola:

```
candidacy = formatMatch × qualityTaste × salesSupport × relationship × advocacy × payola
            × freshness × heatPull × vitality × chartGuard
```

Wired exactly like payola — `StationNetwork.ActiveAdvocacyLookup`, `ADVOCACY_CAP = 0.9` (sized
below the payola cap: talk opens a door less wide than cash). A won call means **he carries the
record into the next meeting**; format, sales support, freshness and the chart guard all still
get their vote. Sizes: pitch `0.14 + influence×0.12` for 3 weeks; favour `0.34` for 4;
ad-buy `0.08/0.15/0.24` for 2/3/4; rival pressure `0.11` for 2.

Both channels now write: rapport (slow, label-wide, permanent-ish) **and** advocacy (this record,
this station, expiring). The card shows the live advocacy under CARRYING FOR YOU with weeks left.

## 3. Access is earned, not granted

`WorkThePhones` no longer always yields a contact. Outcomes: **nothing at all** (a miss narration),
**a name only** (`HeardOf`), or **you got the man** (`Introduced`). Odds come from STREET, the
**hour of day**, diminishing returns within a day (`callAttemptsToday`), and the size of the book
you already have. Getting *through* is a second, harder gate than getting a name, and an
influential jock is screened.

Placing a call can also fail — `ConnectFailure`: `OffShift`, `OnAir`, `Gatekeeper`, `NoAnswer`,
`LineBusy`. Each costs `DialMinutes` whether or not anybody picks up.

**Dayparts** (`RolodexShifts`) make the clock matter and are the main reason a call fails. A DJ's
shift is a deterministic function of `(archetype, djId)` — no new persisted state — with a
reachable window inside the 9am-9pm desk day. A graveyard jock can only be reached by working
overtime. `RolodexEntry.shiftKnown` gates the card's advice, so the first cold call buys you
something even when it fails.

## 4. The clock carries a minute hand

`TimeManager` gained `currentMinute`, `SpendMinutes`, `CanAffordMinutes`,
`MinutesRemainingWithOvertime`; `GetTimeString()` prints real minutes. The old
`PlayerDesk.phoneMinutesAccum` swallowed sub-hour actions into an invisible bucket, so four
consecutive calls left the clock reading 9:00. It is retained as a dead save field only.
Persisted via `WorldSaveData.Minute`.

## Verification

- **AI economy byte-identical.** 104wk / seed 1001 / `--calibration --enable-genre-market-v2
  --enable-artist-population-lifecycle`, patched tree vs a clean `HEAD` worktree: **75 of 75
  CSVs identical**. Advocacy is player-only, so the lookup returns 0 and the new term is exactly
  `1.0f` — an exact-in-IEEE754 no-op that preserves multiplication order.
- **Save/load round-trip PASS** at 26wk/seed 1001. `StationAdvocacySaveData` added to
  `PlayerSaveData.Advocacy` (advocacy is not derivable from anything else, so a load without it
  would silently cancel every outstanding promise); `RolodexEntrySaveData.ShiftKnown` added.

---

# Phase 6b — playtest fixes (Alice Records save)

Playtest report: *"he said he'd give it one spin on the graveyard, then it said playlist meeting,
then 4 weeks. Called back and it said I'd already sold him — but nothing happened and my song got
no boost. He's a Tastemaker in 1960, before Boss Radio — shouldn't he just toss it up?"*

Diagnosed against the actual save (`AliceRecords`, week 7, Stevie Watts / WSUV / FullService /
autonomy 0.81 / Tastemaker). Every complaint was a real defect.

### 1. Rapport did not survive save/load — **root cause of "nothing happened"**
`RebuildRadioForLoad()` rebuilds the panel from the station seed, discarding every
`StationRuntime`. That is fine for AI playlists (they re-derive in a week) but it silently
destroyed **every relationship the player had cultivated by hand** — the save's card log read
"Rapport +0.07" while the live station read `0.000`. The player's own records' rotation slots went
the same way. Both are now snapshotted in `PlayerSaveData.StationState`
(`StationPlayerStateSaveData`), captured for the player's `labelId` and the player's records only,
and restored after `WorldStateService.Apply`. Verified: rapport `0.420` and a `Light` rotation slot
both survive a round trip.

### 2. A 26% candidacy bump could not lift an unknown record into a 36-slot sheet
Probed directly: with the advocacy applied, `player_2` was **never picked up by any reporter
station over 16 weeks**. The multiplier was competing against the whole national field, so a won
pitch was mechanically inert for exactly the records the Rolodex exists to help.

Fix is structural, not a bigger multiplier (see the "panel weight sets the ceiling" rule above):
**`DiscretionaryPicks(autonomy)`** — a jock may seat 1 (autonomy ≥ 0.45) or 2 (≥ 0.75) records into
his own Light slots, drawn only from records he **personally committed to** (`PersonalPitch` or
`FavorCalledIn`; a paid ad-buy and a spite-driven rival play are deliberately excluded — money buys
a hearing in the ranking, not the man's own picks). Boss Radio at autonomy 0.10 gets **zero**. The
pick still must clear `formatMatch`, never displaces an incumbent, and lasts only as long as the
advocacy. Verified: the record now holds `WSUV:Light` across the advocacy window and then ages out
naturally.

### 3. "Playlist meeting" was wrong for a 1960 personality jock
`ResolvePitch` now splits on `djAutonomy` — the sim's own measure of how much this jock's opinion
counts at his own station, and the exact quantity the Boss Radio conversion drives to 0.10:

- **`>= DirectSpinAutonomy` (0.45)** — he puts it on **himself, tonight**
  (`StationNetwork.PlayerSpinNow` → live playlist at Light, drop latch cleared, record becomes an
  incumbent). Two weeks of advocacy behind it. No meeting, no memo.
- **below** — it goes to the meeting, three weeks of advocacy, and the copy says so plainly.

### 4. Off-by-one in advocacy weeks
`expiresWeek = week + durationWeeks` with an inclusive `>=` test gave `durationWeeks + 1`: a
"3 weeks" promise displayed as "4 week(s) left". Now `week + durationWeeks - 1`.

### 5. Voice tags claimed instincts the player did not have
"Offer to buy time around it" was tagged `[THE SUIT]` with **no gate**, so an Ex-Musician
(SUIT 1) saw a SUIT option. Buying airtime is money and is on the table for everybody; what a SUIT
adds is knowing what it is worth. Both the ad-buy and the pitch are now untagged below the
instinct threshold and carry a plain sub-label; at ≥ 3 they gain the voice tag *and* a real read.

### 6. No feedback loop — the reason it all read as inert
`ProcessAdvocacyOutcomes()` (weekly) diffs each advocacy's `lastSeenTier` against the live
playlist and reports **added / moved up / cut back / dropped**, plus a closing "he argued for it
and lost" when an advocacy expires having never been played. Expired rows are kept as *watches*
until the record leaves the air, so the outcome still gets reported after the argument stops
counting. The card's `WHERE IT STANDS` block now leads with what the station is actually doing
(`ON THE AIR — light rotation`), not with what the player bought; re-pitching a carried record
reports its real status instead of a bare greyed-out button.

### Verification
- **AI economy still byte-identical**: 75/75 CSVs, 104wk, seed 1001, canonical flags, vs a clean
  `HEAD` worktree. `AdvocacyReservationLookup` is null in headless, so the picks block allocates
  nothing and the ranking is untouched.
- **Save/load round-trip PASS** at 26wk, plus a direct check that rapport and rotation slots
  survive.
