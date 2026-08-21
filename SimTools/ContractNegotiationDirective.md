# Contract Negotiation Directive

Branch: `rolodex-player-character` (rebalance landed) / a `contract-negotiation` branch for the
mini-game. Source: `managingsketch.odt` (five manager tiers + clause taxonomy), read against the
tree. This file is the code-verified plan.

## Part 1 — Rebalance (IMPLEMENTED)

Every change in Part 1 is player-side. The AI economy is byte-identical throughout, so **no decade
run is owed for any of it**. The one AI-affecting fix that surfaced during the work is scoped to the
player and recorded below as an open item.

### Overhead: $75/mo, home operation

`AILabel.GetMonthlyOverhead()` returned `Small` tier's `150 + 30/artist` for the player. It now
short-circuits on `isPlayerOwned` to a flat `PlayerHomeOfficeOverhead = 75f`. Player-only, so the
AI economy's overhead schedule is byte-identical — this matters, because overhead is read by
`UpdateLabelStatus`, the distribution-advance gates, `SelectAffordableCandidate`, and the runway
term in the operating-target eligibility test.

The per-artist line is gone rather than scaled: the player's marginal cost of an act is already
charged where it lands (studio time, pressing, gas on the road). Knock-on: `CanAffordToSign` holds
`overhead * 2`, so the player's signing reserve drops $300 → $150.

### Advance: the room sets the price band

Was: `Label.CalculateAdvanceOffer(artist)` — `Small` tier base **$300** × talent(0.5–2.0) ×
standing, then a flat `IndustryActMarkup = 1.6` for the trade. A bar band asked $150–600.

Now `PlayerDesk.VenueAdvanceAsk(artist, venue)`: a per-venue base, the same talent/standing curve as
the AI's offer (so the two read consistently), the manager multiplier on top, rounded to a figure a
period contract would carry ($5 steps under $100, $25 over).

| Room | Base | Typical unmanaged ask | With a Shark (2.5×) |
|---|---|---|---|
| Honky tonks | $20 | $10–40 | up to $100 |
| Clubs & roadhouses | $25 | $15–50 | up to $125 |
| Theatres & supper clubs | $260 | $130–520 | up to $1,300 |
| Industry meets | $600 | $300–1,200 | up to $3,000 |

`IndustryActMarkup` is deleted — the trade's premium is now in its base, which is the honest place
for it. `ApproachToSign` overrides the term sheet's advance with the same `VenueAdvanceAsk`, so the
number on the scouting pad and the number that pre-fills the contract form are **the same figure**.
That identity is a prerequisite for Part 2: the ask has to be a stable anchor to negotiate against.

### Royalty: the room sets that band too

`CalculateRoyaltyRate` gives `Small` tier a **10%** base. The sketch's no-manager small-act deal is
1–3% and the era's paper agrees (Stevie Wonder's first Motown deal ~2%, the Jackson 5's later ~2.7%).

`PlayerDesk.VenueRoyaltyBaseline(artist, venue)` now bands it the same way as the advance, plus the
existing `careerState` leverage bonus and the manager's `RoyaltyDemandMult`, rounded to quarter
points:

| Room | Baseline |
|---|---|
| Honky tonks | 1.5% |
| Clubs & roadhouses | 2.0% |
| Theatres & supper clubs | 3.0% |
| Industry meets | 4.5% |

This is the rate with a **reasonable chance of acceptance** — not a floor and not a promise. The
player may write it lower, down to `PlayerRoyaltyFloor = 0.5%`; the further under the act's number,
the likelier the pushback. The old `0.02` clamp in `OfferContract` and the `min 2` on the UI spinner
both moved down to accommodate that, and the spinner steps in quarter points.

**Correcting an earlier claim in this file:** a previous draft said dropping the royalty would make
the early game unwinnable and would need a decade run. Both were wrong. Royalty is a straight
deduction from label net on both player paths — `net = gross - royalty` in `BookTrunkSale`, and
`recordRevenue = grossAfterCogs - skimAmount - royaltyPaid` in `CalculateLabelRevenue` — so a lower
rate is strictly *more* money for the player (10% → 2% is about +13% margin per unit on a 45).
And a decade run is only owed if the shared constant moves: `CalculateRoyaltyRate` is called for
every AI signing (`RosterManager` :419, :1170) and `LabelTier.Small` is **79.5%** of the AI
population (`AILabelFactory` :311). The player-only band above touches none of it.

### Recoupment: fixed player-side, AI-side bug left standing (OPEN ITEM)

The advance is real cash out at signing (`RecordExpense` → `label.cashReserves -= amount`). At
settlement the label was charged the **full** royalty, and the recouped slice was then subtracted
artist-side only:

```csharp
float recordRevenue = grossAfterCogs - skimAmount - artistPayment;   // full royalty gone
...
float recouped = Mathf.Min(artist.unrecoupedAdvance, artistPayment);
artist.unrecoupedAdvance -= recouped;
artist.totalRoyaltyEarnings += artistPayment - recouped;             // artist doesn't get it either
```

The recouped money leaves the label and reaches nobody — it is destroyed, and every advance is paid
twice. The player's trunk path (`PlayerDesk.BookTrunkSale`) was worse still: it never recouped at all.

**Fixed for the player only.** `CalculateLabelRevenue` now nets the label against
`royaltyExpense = label.isPlayerOwned ? royaltyToArtist : artistPayment`, and `BookTrunkSale` recoups
the same way. Artist-side bookkeeping (`unrecoupedAdvance`, `totalRoyaltyEarnings`) is unchanged for
everyone, so `HasRecoupedCurrentContract` and every lifecycle read of it are untouched. The AI branch
reproduces the original expression exactly — **byte-identical, no decade run owed for this change.**

**OPEN ITEM — the AI-side bug is real and deliberately unfixed.** Not because it is defensible, but
because the AI economy is calibrated *against* it: label survival, roster growth, and tier
distribution were all tuned with advances being double-paid, and handing 600 labels their advances
back is exactly the "fixing a sampler tuned against buggy behavior inflates incumbents" trap. When it
is fixed, it needs a decade run, and two things want watching:

- **Seeded-roster windfall.** `RosterManager.InitialSignArtist` stamps `unrecoupedAdvance` on the
  frozen 1960 roster *without* ever expensing it, so those balances would flow back as income the
  label never paid. If that distorts the early years, zero the seeded balances rather than
  special-casing recoupment.
- **Tier skew.** Majors carry the biggest advances and would recover the most. Check Major share
  against `[[major-share-late-decade-consolidation-goal]]` — it could move for the wrong reason.

## Part 2 — The negotiation mini-game (IMPLEMENTED)

Built on `rolodex-player-character`: `ContractTalk.cs` (the scene data: posture, axis, counter, and
stage enums, plus the `ContractTalk` object held live on `Prospect.Talk`) and
`PlayerDesk.ContractNegotiation.cs` (posture, the reservation package, the table/objection/counter
loop, and the shared `FinalizeSigning` both this and the Pushover `OfferContract` path call into).
`ContractTermSheet.NegotiationDifficulty` is no longer a dead field. Three places where this file's
pseudocode needed a concrete number and didn't have one:

- **Axis terms for Term/Publishing/CreativeControl.** Only Royalty's shortfall curve was specified.
  The other four axes use the same shape by construction (1.0 at the ask, `Advance` linear above/
  below it, the three "give something back" axes worth +25% for a concession and −35% for a
  take-away) so `Value(ask)` is always exactly 1.0 and the reservation stays a clean fraction of it.
- **Trade axes** is concrete, not a menu: it gives back publishing AND creative control and cuts the
  tabled advance 25%, then returns to the table for the player to fine-tune before re-tabling — the
  directive named the shape ("give publishing, take the advance back down") but not the ratio.
- **Promise is a package-value credit, not a loyalty system.** It bumps `contractSinglesObligation`
  by 2 and adds a flat credit (0.05–0.16, scaled by `commercialPragmatism`) toward clearing
  reservation. "Writes an obligation that damages loyalty when broken" is NOT built — there is no
  delivery-deadline tracking to break yet, and adding one is a second mechanism (watching
  `contractReleases` against a promised date) that this pass didn't scope. The obligation itself is
  real and already enforced by the existing delivery-maturity code in `RosterManager`; only the
  *broken-promise consequence* is the deferred half.

Everything else matches the pseudocode below: posture, the reservation/room formula, the five-round-
to-two-round patience curve, and the fogged objection read (a stable hash on `(labelId, artistId,
round)`, exactly `ScoutingPerception`'s discipline — the axis named is always the true worst one;
what a bad scout loses is the precision of the number, shown as "a little/a fair bit/a long way"
under a 0.6 `scoutingAbility` cutoff instead of an exact percentage).

### The reframe

Do **not** build a new negotiation grammar. One already exists and works: the Rolodex DJ call
(`PlayerDesk.RolodexVerbs.cs`) is *approach → he raises ONE objection, the most severe thing
actually true → you answer it with a counter that is either grounded in a fact or a bluff he can
call → roll → resolve, with the odds shown as real numbers from the context.* That is a
push-and-pull negotiation mini-game the player has already learned. Contract negotiation should be
the **same loop with different nouns**, so the second one costs the player no new vocabulary.

### The one dead lever

`ContractTermSheet.NegotiationDifficulty` is declared, stored at every signing, and **never read** —
its own doc comment says "stored now, unused, ready for that later minigame." `ManagerProfile` feeds
it (None 0.0, LocalHustler 0.2, Svengali 0.6, Visionary 0.7, Shark 0.9). This is the whole plumbing
job: the field exists, the values exist, nothing consumes them. `PlayerDesk.OfferContract` currently
signs **anything** the player types — there is no acceptance test at all.

### Posture: most signings must stay one click

The player signs a lot of bar bands. A mini-game on every one of them is a tax, not a feature. So
the difficulty gates the *interaction*, not just the numbers:

```
NegotiationPosture Of(artist, venue, label):
  difficulty = ManagerProfile.Of(artist.manager).NegotiationDifficulty
  drama      = mean(active members -> Musician.GetDramaRisk())   // ego/ambition/disloyalty/temperament, already computed
  heat       = artist.reputation, artist.momentum                // is anyone else circling
  Pushover   if difficulty < 0.25 && drama < 0.55 && heat low    // accept-or-walk, exactly today's form
  Firm       if difficulty < 0.60 || drama < 0.75                // one objection, one counter
  Hardball   otherwise                                           // the full loop
```

Pushover is the common case and keeps the current single-click form. Firm and Hardball open the
scene. `GetDramaRisk()` is the "high-ego/greed" axis the sketch asks for and it needs **no new
fields** — same for `groupCohesion` (a fractious band negotiates worse) and the
`ArtistEvolutionProfile` disposition terms (`artisticAmbition`, `commercialPragmatism`), which are
the natural weights for *which axis they care about*.

### The hidden number: reservation, not a haggling AI

Each act carries a **stated ask** (the term sheet the player sees) and a hidden **reservation
package** — what they will actually take. The gap is the room. Room is wide for a nobody and narrow
for a represented act:

```
room = Lerp(0.65, 0.10, saturate(0.6*difficulty + 0.4*drama))
reservation = askValue * (1 - room)
```

An unmanaged, easy-going roadhouse quartet will take ~35% of their ask — they'd sign for dinner,
which is the point. A Visionary-managed act will take ~90%, and the missing 10% is almost never
money.

Package value is a weighted sum over the five axes **already on `ContractTermSheet`** — Advance,
RoyaltyRate, TermYears, LabelOwnsPublishing, ArtistCreativeControl — each normalized against the
ask, weights from disposition. No new contract fields. Accept when `Value(offer) >= reservation`.

### Where the royalty pushback plugs in

The band above is defined as "reasonable chance of acceptance", which is a statement about the
acceptance curve, so the curve has to be written to match it. Royalty is one axis of the package
value, and its own term is:

```
shortfall = saturate((baseline - offered) / baseline)     // 0 at the ask, 1 at zero points
royaltyTerm = 1 - shortfall^1.5                           // shallow near the ask, steep as you cut
```

The exponent is what makes the baseline feel like a soft number rather than a wall: shaving a
quarter point off 2% costs almost nothing, halving it costs real acceptance probability, and
offering half a point against a 4.5% trade act fails against everything but a desperate nobody.
Note this reads **below** baseline only — offering *over* the ask buys goodwill on other axes, it
does not buy more than acceptance on this one.

**Inert until Part 2 lands.** `OfferContract` still has no acceptance test, so today the player can
write half a point and the act signs it. The band and the floor are in; the consequence is not.
That is the first thing Part 2 turns on.

### The loop

1. **Approach** (0h, as today) — generates the ask; posture decides what opens.
2. **Table an offer** (2h, `ActionCosts.QuickMeeting`) — the five-axis form we already render. If
   it clears reservation, signed. If not, they **counter with one objection**: the axis with the
   largest deficit, in plain words. *"He'll do it for forty, but he wants his name on the label."*
   That naming is the pull — it is information, and it is what makes this a game rather than a
   slider hunt.
3. **Counters** (mirroring `BuildCounters` in the DJ call) — a small menu gated two ways, instinct
   *and* fact:
   - **Sweeten the named axis** — the direct answer. Always available.
   - **Trade axes** — give publishing, take the advance back down. Available when their weights
     make it a real trade.
   - **Promise** — a release date, an A-side, a specific push. Costs nothing now, writes an
     obligation that damages loyalty when broken. This is how a broke player closes, and it is the
     most period-true verb on the list. Hooks the existing `contractSinglesObligation`.
   - **Hold firm** — re-table unchanged. Drops their reservation slightly if patience is high;
     ends the scene if it is not. The bluff, and it can be called.
4. **Patience** — `2 + round(3 * (1 - difficulty))`: five rounds with an easy act, two with a hard
   one. Exhausted → they walk, with a cooldown before you can re-approach.

Hours: 6h currently buys the whole signing. Under this, a Pushover signing costs the same ~6h and a
Hardball one genuinely eats a day. That is the cost curve doing the work the sketch wants.

### Fog applies here too

`ScoutingPerception` already fogs the quality read by `scoutingAbility`. The negotiation read —
"how far off am I, and which axis do they actually care about?" — must be fogged by the same band,
by the same pure stable hash, never a `GD.Rand*` draw. A bad scout gets a vague objection and
overpays; a good one hears the number. This is the payoff for investing in scouting and it costs
nothing new to build.

### Determinism

Everything above is player-turn-local. It must not touch `RosterManager.ProcessDailyTalentMarket`,
`AILabel.GenerateTermSheet`'s RNG call order, or the population stream — the AI economy signs
through `SignArtist(artist, year, sheet)` and must keep doing so unchanged. Verify with the
byte-comparison probe: a decade run with the mini-game compiled in and no player acting should hash
identical CSVs.

## Part 3 — Deliverables, renewal, and mid-contract managers (IMPLEMENTED)

Three gaps that surfaced once Part 2 was played through, all on `rolodex-player-character`.

### Deliverables is now a term, not a hidden default

`ContractTermSheet.SinglesObligation` was carried through the whole negotiation form invisibly —
the player set Advance, Royalty, Term, Publishing, and Creative Control, and singles owed came
along for the ride from `AILabel.CalculateContractSinglesObligation` untouched and unseen.

It's now the sixth field on `TermsForm` ("Deliverables (singles)"), and the sixth axis on
`ContractTalk` (`ContractAxis.Deliverables`) for a Firm/Hardball negotiation — same shape as Term
(a lighter quota is a concession, a heavier one is a bigger ask), weighted toward `artisticAmbition`
(an ambitious act resents being milked for product more than a pragmatic one does). The player's
opening ask is a new player-only re-price, `PlayerDeliverablesAsk` — 2-3 singles a year for a new
act, tapering to zero once `CalculateContractSinglesObligation`'s own career-state gate would retire
the quota (Established and up) — same spirit as `VenueAdvanceAsk`/`VenueRoyaltyBaseline`: the AI's
own obligation formula is untouched, this only touches what the player is shown and can set.

### Renewal: RosterManager already said whose call this was

`RosterManager.OnMonthChanged` skips player-owned labels entirely — `if (label.isPlayerOwned)
continue;`, with the comment "Renewals and drops on the player's roster are the player's calls."
That was true and unbuilt: a player contract simply never expired. It matures
(`RosterManager.IsContractMatured` — term or delivery, exactly the same test the AI uses) and then
sits there, flagged "CONTRACT UP" on the Roster tab, until the player acts.

Renewal reuses the Part 2 machinery wholesale: `ContractTalk` now carries either a `Prospect` (new
signing) or a bare `SimulatedArtist` (renewal) behind a `ContractTalk.Artist` property, and the same
`TableOffer`/objection/counter loop drives both. The ask is `Label.GenerateTermSheet(artist, year)`
generated fresh off the act's **current** stats and **current** manager — which is the whole answer
to "renewal terms dictated by improved fame/wealth, or by manager": nothing new had to be built,
`CalculateAdvanceOffer`/`CalculateRoyaltyRate`/`PostureOf` already read `careerState`, `reputation`,
`momentum`, and `ManagerProfile.Of(artist.manager)` live. A Star who broke out under the player,
or picked up a Shark since signing, negotiates their renewal like one even if they signed as a
Pushover bar band.

Consequences are asymmetric on purpose. A **new signing** that falls through costs nothing but a
cooldown — there was never a deal. A **renewal** that falls through for good (patience exhausted)
means the act actually leaves (`ArtistManager.DropArtist(..., ArtistDropReason.ContractExpired)`) —
the old paper already ran out, so pushing a Hardball renewal too hard has real teeth. Stepping back
voluntarily, either case, costs nothing; the old terms hold and the player can try again.

This is ordinary lifecycle renewal, not the deferred "post-hit renegotiation trigger" below — it
only opens at natural contract maturity (term or delivery), never mid-term off the back of a hit.

### Managers were a one-time stamp; signed acts can earn one now

`ArtistManager.RollManagerArchetype` only ever ran at generation (its own doc comment: "Stamping a
manager at generation costs one Randf() draw per artist"). A None act stayed None forever, even
under a player label that took it from a bar band to a Star. `PlayerDesk.CheckForManagerInterest`
(monthly, player-roster only, gated behind `ManagerSystem.Enabled`) now rolls interest for an
unmanaged act actively on a career track (Rising and up), scaled by momentum + reputation, and hands
a genuine hit through the exact same quality-correlated table via a new public door,
`ArtistManager.RollManagerArchetypeFor`. The moment `artist.manager` changes, the existing passive
auras (`ChartVisibilityAura` in `ChartManager`'s heat calc, `ProductionBonus` in `ReleaseRecord`)
start applying — they already read `artist.manager` live, so nothing else needed wiring.

## Explicitly out of scope (mid-late game)

Per the directive to leave sharks and high-level managers for later, the sketch's **tiers 4 and 5**
are deferred: contract buyouts (the Parker/RCA $35k move), dual-role management and the player's own
management arm, 360-style packaging, label-vs-label bidding wars, post-hit renegotiation triggers,
key-man clauses, leaving-member clauses, minors and disaffirmance.

**One correction to that scoping.** `ManagerArchetype.Shark` is not a future thing — it already
exists and `ArtistManager.RollManagerArchetype` assigns it to ~28% of high-quality acts and ~15% of
mid ones, today, whenever `ManagerSystem.Enabled`. So "leave sharks out" cannot mean "no shark acts."
In Phase 1 a Shark-managed act is simply a **Hardball posture with hard numbers** — narrow room, two
rounds of patience, expensive. The tier-4 *behaviors* above are what wait.

Also deferred from the sketch because it is a mechanism, not a clause: royalty basis ("of 90% of
retail", wholesale vs net, deductions, free goods, breakage) and cross-collateralization. The model
has a single `royaltyRate` float against units. Adding a basis is a revenue-model change with a
decade run behind it, and it should not ride in on the negotiation UI.
