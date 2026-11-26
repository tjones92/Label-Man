# Who wants a deal: making scouting a courtship, not a coin (2026-11-25)

Follows `SigningExperienceAudit.md` §2.8. The author chose to do both of its open options (player-side odds and more
unsigned acts on bills) and widened the question. Playtest complaint, in the author's words: the signing game is
"a few buttons and coin flips". It doesn't make you think *I have to go see this band*, and it doesn't let you build a
relationship with them. The three answers a player keeps getting feel arbitrary and confuse new players:

- "they're signed"
- "they're not looking for a deal" (but they'll be open to one in exactly 7 days)
- "we want to keep playing locally this season"

Many real acts of the period played constantly and were desperate for a record. The game has them backwards.

## 1. Diagnosis: three pieces of sim plumbing are dressed up as the act's feelings

Every one of the three answers is a fact about the **population economy**, shown to the player as if it were a fact
about **the band**.

### 1.1 "Not looking for a deal" is the labour market's reservoir flag
`ProspectMarketStatus.Latent` (`ArtistManager.cs`) is a **pacing hold**. Each week,
`ActivateProspectsForHiringOpportunities` moves just enough Latent acts to `Seeking` to cover the AI labels' affordable
roster vacancies (`CalculateProspectActivationCount = min(latent, vacancies − seeking)`), ordered by spell count and a
hash. It also takes in unsigned veterans after 78 weeks (`EnterProspectReservoir`). Nothing about the act decides it.
The flag means "the AI market isn't currently drawing from this act". The UI reads it out as "not looking for a
record deal" (`PlayerDesk.LocalScenes.cs:62`).

So the Seeking pool is sized to be emptied every week. That's why only **0.4%** of bill slots are "on the market". It
isn't that bands don't want records; the market is built to have no surplus.

### 1.2 The ask is a keyed coin with a fixed clock
`TryDirectApproach` (`PlayerDesk.DirectApproach.cs`):
- `StableUnit(seed|label|act|season) < 0.65`, re-keyed every 91-day season. Nothing the player has done, and nothing
  about the act, enters the draw.
- A "yes" always waits `DirectApproachAnswerDays = 8`. That's because the act has to pass through the **weekly**
  activation pass to become Seeking before terms are legal. That's the "open in exactly 7 days" the author felt.
- A "no" always reopens on the next season boundary, as a date.
- Hearing them three times, paying for a demo or getting them a booking changes nothing.

The phase-6 note states the principle: *"The player never activates anyone."* That rule kept the AI economy clean. Its
cost is that the act's answer can't respond to anything the player does.

### 1.3 "Signed" is honest, but it's overrepresented, and it's a wall
Rooms book from `LocalScenePersistenceService.MembersOf(scene)`: **every** living act whose base is in that town,
signed or not. Bills therefore mirror the registry, and the registry holds the industry's rosters. The result is that
**46% of distinct acts** you hear are under contract (55.6% of slots, because signed acts work more).

Today a signed act is a dead end. Yet the sim already knows things a scout would care about:
- `contractExpiresWeek`, when the deal runs out
- `ContractExpiryDecision`, whether the label is likely to keep them
- whether the label is small and struggling, and whether the act has charted

### 1.4 The act already has wants. The ask ignores them.
Real state that exists today and could drive a willingness to sign:

| State | Where | What it would mean to a band |
|---|---|---|
| Member `ambition`, `loyalty`, `ego` | `BandLifeService` person sheet | Hungry vs comfortable; who holds out |
| Member `wealth`, `income`, day jobs (`FullTime`) | `MemberWealthService`, `LocalSceneRoomService.WorkingWeek` | A day job makes a record a way out; a steady residency makes it a risk |
| Ages, years together | members | Young and impatient, or settled |
| Originals share | the heard set, `ScoutingRough.Tells` | Writers want their songs on wax |
| Prior contracts, last drop reason | `contractSequence`, `lastDropReason` | Burned before: careful, wants guarantees |
| What the act values in a label | `RosterManager.CalculateArtistChoiceUtility`: genre fit, locality, buzz, royalty, advance, reach, roster room | Used **only** when AI labels compete. The player's ask never reads it |
| Rival interest | `PlayerDesk.RivalCourtship.cs` | Being courted changes how you talk |
| The perceived side | `ScoutingRough.AmbitionRead` ("hungry"/"comfortable") | The player can already *see* a tell. It has no effect |

The game already models a band that should be desperate, and then flips a coin over whether they are.

## 2. Principles for the redesign

1. **Market status stays plumbing; disposition is the act's own.** Leave Latent/Seeking to run the AI labour market.
   Give the player-facing answer a separate, derived **disposition**, computed from the act's real state.
2. **Every answer names a reason the player could have found out.** "Carl's got the Tuesday-to-Saturday at the Flame
   and two kids" is a no you can respect, and a clue about what would change it. "Playing locally this season" isn't.
3. **No calendar dates for feelings.** Waits exist only when something real takes time: the band talks it over at
   rehearsal, a manager has to be consulted, a rival's offer is pending. The answer arrives at that event.
4. **What the player does moves the answer.** Showing up, the demo you paid for, the booking you got them, a hit on
   your label in their genre: these are the relationship. Use the existing verbs only; no new currency to grind.
5. **Most unsigned acts of the period are hungry.** The challenge moves from "will they?" to "should I, and before who
   else?": judgement under fog, roster room, cash, and rivals. That's the game's central skill (audit §2.7).
6. **Deterministic, no dice in the read.** Same rule as the voices: the outcome follows from state. If a tie-break
   draw is needed, it's small and keyed, never the main term.

## 3. The design

### 3.1 Disposition: what an unsigned act wants (player-only, pure function of state)
Each unsigned act gets one disposition, recomputed when it's read (no stored roll):

| Disposition | Typical reason (from real state) | What happens when you ask |
|---|---|---|
| **Hungry** | High ambition, day jobs, young, writes its own songs | "Where do we sign?" Terms can open in the same conversation |
| **Interested, careful** | Burned before (`contractSequence > 0`, dropped), a manager, a loyal member wary of a stranger | Wants something first: hear your records, meet again, a demo paid for, a guarantee to release |
| **Being courted** | A rival has inquired or offered (real courtship state) | Will compare. Terms open now, but the act names what the other label offers |
| **Comfortable** | Low ambition, a well-paid residency, older, wealth ≥ the full-time line | A real no, with its reason. It reopens when the reason changes (the residency ends, a member quits, someone in their genre charts) |
| **Spoken for** | Signed, or a handshake hold with someone else | See §3.4: a timer and a watch item, not a wall |

Inputs (all exist; no new fields):
- **Drive** = mean member ambition, raised by money pressure (day jobs, low wealth) and youth, lowered by a steady
  well-paid engagement.
- **Wariness** = prior contracts ending in a drop, a Shark/Svengali manager, high loyalty to an existing local
  arrangement.
- **Fit to you** = the same `CalculateArtistChoiceUtility` terms the act uses against AI labels, applied to the
  player's label: genre fit, locality, buzz, roster room. A label with nothing out reads as "we've never heard of
  you", which is honest and fixable.
- **Regard** (§3.2).

Disposition = thresholds on those. **Calibration targets** for unsigned acts heard in the opening (author's call;
these are proposals): Hungry ≈ 55–65%, Careful ≈ 20–25%, Comfortable ≈ 10–15%, Courted is whatever the rival system
produces. The 65/35 coin goes away.

### 3.2 Regard: the relationship, built from things you already do
A small per-act ledger the player can read in The Talk (audit §3.3) as the act's attitude, not as a number:

| You did | Regard | Their line, for example |
|---|---|---|
| Heard them (each distinct night, capped) | + | "You were at the Roadhouse Friday, weren't you?" |
| Talked to them, asked for their number | + | |
| Paid for a demo / a rehearsal visit | ++ | "Nobody's ever paid for a tape for us." |
| Got them a booking (early-game §6.3 bookings) | ++ | |
| Your label charted in their genre | + (also raises fit) | |
| Promised a week and came back late, let a demo lapse, or let them go and returned | − | |
| Time apart | slow decay | |

Regard pushes **Careful → Hungry** and can turn **Comfortable** into a hearing. It never turns a Comfortable act into
Hungry on its own; the reason has to change. That is the "build a relationship to sign them" loop, made from existing
verbs.

### 3.3 Answers when they'd really come, said by a person
- **Hungry**: the answer is immediate, in The Talk. SIT DOWN ON TERMS appears in the same scene.
- **Careful**: they say what they want. "Let us hear something you've put out." "Come and see us again before we talk
  money." The want is a concrete existing verb. When it's met, the answer changes on the spot.
- **A band talks it over** only when the members disagree. Use the per-member traits: spread of ambition, a low-
  ambition member with a day job. The answer arrives **at their next rehearsal or set**, a real dated event the
  player can attend. Spokesperson + holdout gives a later beat: you can talk to the holdout (§5, later).
- **No "ask again after <date>"** anywhere. The notebook says what would change their mind.

**The activation problem.** Terms today require `Seeking`, which only the weekly pass grants. Proposed: when a
player-heard act **agrees**, the player desk may sign it directly from Latent. It's the player's own roster slot, so
no AI label loses an activation. The phase-6 rule "the player never activates anyone" keeps its purpose (the AI
market isn't steered), because the act goes straight to the player's roster and never enters the AI's pool.

Consequence to check: today rivals learn of the act **because** it enters Seeking ("Other labels can hear of it too").
Without the detour, rival pressure has to come from the rival courtship system's own sources (word that reaches a
scene, a rival's scout in the room). That's more honest, but it needs a measurement of how often a rival contests a
player-courted act before and after.

### 3.4 "Signed" becomes a story with a clock
- The Street (≥3) says what it knows: "They're with Fortune. Two sides, nothing charted. Deal's up in March."
  Facts: `labelId`, the label's tier and health, the act's chart record, `contractExpiresWeek`.
- **KEEP AN EYE ON THEM** files a reminder for the expiry. Near it, the Street reports what `ContractExpiryDecision`
  will likely do, as a read, not an oracle.
- When a signed act's deal ends and the label lets them go, an act you've built regard with **calls you first**.
  That's the payoff for a relationship you couldn't act on.
- The player can't poach mid-contract; it wasn't done, and it would move the AI economy.

### 3.5 World: more unsigned acts on bills (own screen; touches AI CSVs)
Bills mirror the town's registered population. Two possible levers, in increasing cost:

**(a) Signed acts with national careers leave the small rooms.** An act on a label with a charted record, or at
Established and above, plays its label's dates and bigger rooms, not a Tuesday at the roadhouse. Small and regional
signings still play locally; that's realistic, and it keeps "they've got a single out on a local label" as a scene
fact. This frees bill slots for unsigned residents with no new population. It changes who plays where, so audience
attribution and the live-draw recognition deposit move. That touches the AI, so it needs an A/B.
**First measure whether towns *have* enough unsigned residents to fill the freed slots** (the probe can count
residents by standing per town). If they don't, bills thin out and (b) becomes necessary.

**(b) A deeper unsigned layer.** The period had far more bar and dance acts than record deals; the registry is the
industry's size, not the scene's. Options: raise the enabled unsigned reserve per scene town, or let
`SceneRecruitmentService`/formation seed local-only acts weighted to scene towns. This moves the formation servo and
the labour market, so it's a decade-A/B change. Build only if (a) measures short.

**(a′) A signed act on a national push gives up its weeknight residency** (author, 2026-11-25: "a truly national act
is going to focus on tours or label engagements, not residencies"). This is the lever step 8 starts from, ahead of the
broader (a). §12 found that residencies are where the signed share lives (67% signed, against 49% of other slots;
Detroit 101 of 102), while (a) as written reaches only 17.6% of slots. The act keeps one-off dates and its label's
rooms; it stops being a room's standing Tuesday-to-Saturday resident, and `OfferEngagements` passes the slot to the
next resident by `BookingWeight`.
- **Measure first:** the reputation and live-performance gap between signed and unsigned residents (does the freed
  slot go to an unsigned act of similar strength, or does the room's history keep handing it to another signed act?),
  and how many residencies a signed act holds after its first release.
- **What "national push" means** still needs defining from real state, e.g. a national release on the label's
  distribution, a charted record, or Established and above. Not a regional single on a local label.
- **Touring is separate work.** Here the act just leaves the residency; where it goes instead isn't modelled. Don't
  build a tour schedule as part of step 8.
- Moves audience attribution and the live-draw recognition deposit, so it needs the AI CSV A/B like (a).

### 3.6 The pull: making you *want* to go see somebody
The audit's Set and Talk scenes give the moment. What's missing is the **reason to travel** before you've heard
anyone. From real state, through sources that could know (`SceneInformationService` / `SceneSources`):
- The Street: draw growing week on week, a residency extended, kids calling for an original by name, "they turned
  people away Saturday".
- A booker you've met: "You should catch the late set Friday. Trust me."
- The Fixer: "Somebody from Liberty was asking about them." (only when true)
- Rumours can be **wrong in the fog's direction**. A low Street hears the noise, not the band. You go, and they're
  loud and bad. That's the instinct system doing its job, and it makes the trip a decision.

The NEXT SETS list then answers "who's worth the hour?" with leads, not a sorted timetable. It keeps (audit §2.8a) the
"who's free" fact visible before the hour is spent, as a Street line rather than a filter.

## 4. Order of work

| # | Piece | Touches AI? | Check |
|---|---|---|---|
| 1 | **Measure** — **done**, see §7 | No (read only) | Probe output against the §3.1 targets |
| 2 | **Disposition + reasons** — **built**, see §8 | No (player-only) | New `DispositionChecks`: every disposition reachable, reasons name real state, same state → same answer; AI CSVs byte-identical |
| 3 | **Immediate answers + direct signing from Latent** — **built**, see §9 — for agreeing acts; band talks-it-over tied to the next real set | Player worlds only | Rival-contest rate before/after; first signing day in the opening (today day 1–8) |
| 4 | **Regard ledger** — **built**, see §11 — from existing verbs; Careful wants named as verbs | No | Every regard source moves the line it should |
| 5 | **The Talk** — **built**, see §12 — (audit §3.3) built on 2–4: opening line by disposition, wants as script lines | No | Audit §4 row 3 |
| 6 | **Signed as a clock** — **built**, see §13 — (§3.4): Street contract read, watch item, the call at expiry | No (reads expiry decisions) | An act let go after regard calls the player |
| 7 | **Leads** — **built**, see §14 — (§3.6) from scene sources | No | Leads name real draw/residency facts; low Street misreads |
| 8 | **World booking** — **(a′) adopted as default 2026-11-25**, see §15 — residency surrender on a national push (§3.5 a′) first, then broader (a), then **(b)** only if they measure short | **Yes** | Own screen: residency composition + signed/unsigned resident strength gap + AI CSV A/B |
| 9 | **The holdout and courtship objections** — **built**, see §16 — (audit §3.6) | Player-only outcomes | Own screen (§16) |

Steps 1–7 are player-side: AI-only CSVs should stay byte-identical, and one byte-comparison at the end of step 7
confirms it. Step 8 is the only economy change.

## 5. Decisions for the author

**Decided 2026-11-25:**
1. **The mix** is about 60% Hungry, 20–25% Careful, 10–15% Comfortable.
2. **Hungry acts can be signed directly**, but the author doesn't want that to happen every time. §6 gives the
   history and §7.3 measures how often the real frictions apply.
3. **Signed acts:** watch and wait; no poaching mid-contract.
4. **World booking:** (a) first, after measuring residents. §7.2 changes what (a) can do.

The original questions:
1. **Disposition targets** (§3.1): Hungry ≈ 55–65%, Careful ≈ 20–25%, Comfortable ≈ 10–15% of unsigned acts heard.
2. **Direct signing from Latent** (§3.3) relaxes the phase-6 rule "the player never activates anyone".
3. **Signed acts** (§3.4): watch-and-wait only, no mid-contract poaching.
4. **World booking** (§3.5): start with (a), measure residents first, and hold (b) unless towns run short.

## 6. Research: how quickly were acts signed early in the decade?

A short web search (2026-11-25), not a study. Popular-press and encyclopedia sources, so dates disagree in places.

**Acts chased labels, not the other way round.**
- *The Beatles* were turned down by several British labels before Decca auditioned them. Decca's A&R man heard them
  at the Cavern in December 1961, held a test session on 1 January 1962, and said no about a month later.
  Parlophone's George Martin heard the Decca tapes, auditioned the band, and had a contract by May 1962.
  ([Wikipedia: Decca audition](https://en.wikipedia.org/wiki/The_Beatles%27_Decca_audition),
  [OUP blog](https://blog.oup.com/2012/02/brian-epstein-beatles-contract/))
- *The Primettes* auditioned for Berry Gordy in 1960 and were told to finish high school. They kept turning up at
  Hitsville to sing backing vocals and handclaps, recorded for Lu Pine in between, and signed with Motown in
  January 1961 at fifteen. ([Wikipedia: The Supremes](https://en.wikipedia.org/wiki/The_Supremes))
- *Elvis* paid to cut an acetate at Sun in 1953. Phillips called him back months later.
  ([NPR](https://www.npr.org/transcripts/1147052846),
  [Ultimate Classic Rock](https://ultimateclassicrock.com/elvis-presley-first-recording/))

**When a label wanted an act, it moved fast.**
- Richard Barrett heard the Premiers audition and signed them on the spot as the Teenagers.
  ([history-of-rock.com](https://www.history-of-rock.com/doo.htm))
- Hy Weiss overheard the Fiestas singing next to his office and signed them to Old Town in 1959.
- Jerry Dennon heard the Kingsmen's "Louie Louie" on Portland radio, signed them, and released it on Jerden. It was
  then picked up for national release. ([NW Music Archives](https://nwmusicarchives.com/artist/kingsmen-the-portland-or/),
  [Wikipedia: Jerden Records](https://en.wikipedia.org/wiki/Jerden_Records))

**Small-label deals were bought and licensed upward.**
- Sun sold Elvis's contract to RCA in 1955. ([Sun Records](https://sunrecords.com/artists/sam-phillips/))
- The Beach Boys had a local hit on Candix before Capitol signed them in 1962.
  ([uDiscover](https://www.udiscovermusic.com/stories/beach-boys-hit-big-time/))
- Satellite's Rufus and Carla Thomas record sold 40,000 regionally and was taken to Atlantic.
  ([Wikipedia: Stax](https://en.wikipedia.org/wiki/Stax_Records))

**Turning down a deal to keep playing live:** the search found no documented early-60s case. That doesn't mean it
never happened, but it was the exception. *Comfortable* should be rare and always individual.

**What this means for "how often".** In the period, the act's willingness was almost never what slowed a signing.
The delays were:
1. **The label's own caution.** A test session or audition before committing, as with Decca and Parlophone. In game
   terms this is the player's choice to pay for a demo or rehearsal first, or to skip it and sign under the fog.
2. **Age.** A teenager couldn't sign alone: the Primettes were fifteen. In 1960 most states set the age of majority
   at 21, so a parent signed for a member under 21.
3. **A manager** who shops and negotiates (Epstein).
4. **The band itself**: members to persuade, a member's job or family.

So "sign directly, but not always" falls out of real frictions rather than a rate. A hungry act signs **tonight**
only when no member is under 21, no member is holding out, and there's no demanding manager. Otherwise the yes comes
at a dated, attendable step: meet the parents, sit down with the manager, come to their rehearsal. §7.3 measures how
often that is.

## 7. Step 1 results: the extended probe (2026-11-25)

`SimTools/SigningAvailabilityProbe` (seed 1001, Detroit, 56 days, all 31 towns, about a minute to run). It now also
writes `SimLogs/signing-dispositions.csv`, one row per unsigned act heard.

### 7.1 Who the signed acts on bills are
Signed acts hold 5,371 slots and 1,095 distinct acts.

| | |
|---|---|
| Career state | **NewSigning 89%** (12% of them charted), Rising 5%, Established 3%, Star/Superstar 2% |
| Label tier (acts) | Independent 41%, Boutique 22%, Small 19%, MidTier 11%, Major 7% |
| National career (charted, Established+, or on a Major) | 25% of signed acts, **17.6% of all bill slots** |
| Weeks left on the deal | p10 44, median 97, p90 253. **None end within 26 weeks** |

What a player meets on a bill is mostly *an unknown act with a fresh deal on a small local label*. That's realistic.
The problem is that it's a wall, not that it exists.

### 7.2 The resident census: unsigned supply is not the constraint
Averages per town-week:
- 226 acts present in town; 94 signed (15 with national careers) and 132 unsigned
- about 40 bill slots a week

Unsigned acts that played **no** bill that week range from 677 (New York) and 526 (Los Angeles) down to 3.3
(Albuquerque) and 1.6 (Billings). **No town** is short of unsigned acts to fill the slots a national act would give
up. Signed acts are 42% of residents but take 56% of slots, mostly through residencies (67% signed).

**What this changes in §3.5:**
- Lever (a), national acts leaving small rooms, is safe on supply, but it moves only **17.6% of slots**. At best the
  signed share falls from 56% to about 38%, and the rest are small-label new signings.
- (b), a deeper unsigned population, isn't needed for supply.
- The bigger realism win is the history in §6: a small-label deal can be **bought or licensed**, as Sun sold Elvis's
  contract to RCA. Today the signed-act clock (§3.4) is useless in year one, because no deal ends inside 26 weeks.
  Buying from a small label turns the 38% that would remain into an opportunity with a price. The AI side already
  sells masters (`AiMasterDeals.TryShopUnreleased`, `TryDistressSale`); a player-side contract buy is new and
  belongs with step 6.

### 7.3 Draft disposition (1,271 distinct unsigned acts heard)
Inputs, as read:
- mean ambition 0.56
- **day jobs 73%**
- standing engagement 6%
- own songs at least half the set 10% (covers dominate, which fits the period)
- median age 22 or under 29%; 32 or over 7%
- never signed 99.9%, so "burned before" doesn't exist in the opening

Managers: none 35%, **LocalHustler 31%**, Shark 14%, Svengali 13%, Visionary 8% (the quality-correlated roll in
`RollManagerArchetype`). The first draft made any manager mean Careful, which gave 70% Careful. A local hustler is
"eager to deal" (`ManagerProfile`), and historically managers were the ones shopping the act. So **a manager shapes
the terms and the pace, not the willingness.** In the draft, a hustler adds a little drive, and a demanding manager is
a friction (below).

Drive = ambition, adjusted:

| Adjustment | Change |
|---|---|
| Day job | +.08 |
| Median age ≤ 22 | +.06 |
| Median age ≥ 32 | −.08 |
| Own songs at least half the set | +.05 |
| Local hustler manager | +.05 |
| Paid standing engagement and no day job | −.10 |
| A member who doesn't need the money | −.08 |

Its deciles: .43 .51 .56 .60 .64 .69 .73 .79 .88.

**Fitted thresholds: Comfortable below 0.49, Hungry from 0.60.** This gives the agreed mix:

| Disposition | Share | Reasons as drafted |
|---|---|---|
| Hungry | **61%** | a day job to get out of 63%, young and impatient 22%, ambitious 14% |
| Careful | **22%** | "wants to know who you are" (generic: nobody has been burned yet) |
| Comfortable | **17%** | "not much ambition" 68%, steady paying engagement 19%, settled older line-up 13% |

The neighbouring thresholds: 0.46/0.56 gives 70/17/13, and 0.52/0.62 gives 56/22/22.

**Frictions** (slow a yes without being reluctance), across all unsigned heard:
- a member under 21: **44%** (every member under 21: 12%)
- a demanding manager: 35%
- a 3+ group with a member below 0.35 ambition: 32%

**How often a hungry act signs tonight:**
- No member under 21, no holdout, and no manager or a local hustler: **33% of hungry acts = 20% of all unsigned acts
  heard.**
- Of hungry acts, 46% have a member under 21 and 20% have a holdout.

So the real frictions give the author's "not always that often" with no rate to tune. About one act in five that you
hear signs the night you ask. Another two in five agree but send you to the parents, the manager or the next
rehearsal. The other two in five are Careful or Comfortable.

### 7.4 What step 2 has to fix in the draft
- **Careful needs real reasons.** With no burned acts in the opening, every Careful act says the same generic thing.
  Ground it in the act's view of *your* label, using the `CalculateArtistChoiceUtility` terms:
  - "We've never heard a record of yours" (no releases, low buzz)
  - "You don't do our kind of music" (genre fit)
  - "You're from Chicago?" (locality)

  These are also the things regard and your own record can fix.
- **Comfortable needs a concrete reason or it isn't Comfortable.** "Not much ambition" is 68% of them. Better reasons
  from state: a weekend act with good day jobs ("Carl's a foreman at Dodge Main"), an older settled line-up, a paid
  engagement.
- **The thresholds are one seed and one opening.** Check the mix at 1961 and 1963, and in a second seed, before
  locking them.
- The age-21 rule should be stated as period fact in the copy ("Her mother has to sign"), not explained as a game
  rule.

## 8. Step 2 built: the disposition replaces the coin (2026-11-25)

### What changed
- **`Systems/ActDisposition.cs`**, a pure read with no draws and nothing stored. It returns Hungry, Careful or
  Comfortable, a reason that names real state, and the frictions step 3 will use (members under 21, a holdout, the
  manager type, a "signs tonight" flag).
- **Drive** is member ambition, adjusted as follows:

  | Adjustment | Change |
  |---|---|
  | Day jobs | +.08 |
  | Median age ≤ 22 | +.06 |
  | Median age ≥ 32 | −.08 |
  | Own songs at least half the set | +.05 |
  | Local hustler manager | +.05 |
  | Paid standing engagement and no day job | −.10 |
  | A member who doesn't need the money | −.08 |
  | A release on your label | +.02 |
  | A top-40 hit on your label | +.05 |

- **Thresholds:** Comfortable below **0.48**, Hungry from **0.59**.
- **Out of town is a reason, not a pull.** As a −.03 pull it counted against every act a probe hears away from home
  and pushed Comfortable to 22%. Now it only explains why an act in the Careful band is careful.
- **The ask** (`PlayerDesk.DirectApproach`):
  - Hungry: thinks it over. The 8-day answer stays until step 3.
  - Careful: wants to be heard again (status `WantsAnotherLook`). Once you've heard them on a second night, they think
    it over. The answer gives their next posted date when there is one.
  - Comfortable: says no, with the reason and what could change it.
  - Asking again with nothing changed repeats the answer and files nothing.
  - A no lifts as soon as the act's state does. Older saves' season declines are read the same way.
  - **The 65% coin, the season keys and every "ask again after <date>" are gone.**
- **The answer is spoken.** Whoever talks for the act says it (the after-set speaker, else the leader or singer). It's
  stored as `ScoutingDirectApproach.Said` and quoted in the notebook ("Asked (Jan 1, 1960): they said no. Theodore
  Sutton: …").
- **Copy:**
  - "Not looking for a record deal" is now "no record deal yet · nobody's asked them".
  - The ask tooltips say they'll tell you where they stand and why.
  - The card's hint for a careful act gives their next date.
  - A lapsed answer says why (roster full, or they never called back).
  - T3 gets a Street line for "they want to see you come back", and the declined line no longer blames "the calendar".

### Measured (SigningAvailabilityProbe now calls ActDisposition itself)
- All towns, 1,271 unsigned acts heard: **Hungry 63.7%, Careful 20.8%, Comfortable 15.5%**. Detroit alone (home):
  76 / 11 / 13.
- Reasons:
  - Hungry: day job 64%, young 21%, ambitious 13%
  - Careful: out of town 97%, no records 3%
  - Comfortable: weekend act 65%, engagement 21%, settled 14%
- Signs tonight: 20.5% of unsigned acts heard, as in §7.3.
- `DispositionChecks`: 4,000 off-market scene acts against a new Detroit label give 59.5 / 25.6 / 14.9.

### Checks (seed 1001)
- **`SimTools/DispositionChecks.tscn`, new, 26 checks.** It covers:
  - every read is repeatable
  - every reason names state that is really there
  - all three dispositions occur
  - ambition moves the kind; a holdout is named; a hit on your label raises drive
  - through the desk: a comfortable no with its reason and no date; a repeat ask files nothing; the notebook quotes
    them; the no lifts when the reason does
  - a hungry act thinks it over, and asking changes no world state
  - a careful act wants another look, and after a real second night thinks it over
- **Existing suites pass:**
  - `--scouting-runway-check`: 212 checks, 31 cities. Its ask checks now expect the disposition's answer, and a
    repeat ask must give the same answer.
  - `TheSetChecks`: 92
  - `ScoutingUiChecks`: 72
  - `FirstWeekUiChecks`: 14
- **Runway openings still reach a first offer:** Billings, Albuquerque and New York on day 8, Nashville on day 15.
- **AI byte-comparison: not run.** Everything touched is player-desk code, a read-only engagement lookup
  (`LocalSceneRoomService.EngagementRoom`) and an enum value appended at the end. The plan's one byte-comparison
  runs at the end of step 7.

### Not done in step 2
- Careful's only want is a second night. The other wants (hear your records, a demo paid for) come with regard,
  step 4.
- The probe's "Careful" is mostly "out of town" because it hears all 31 towns. At home it's rare (11%), and its
  reason there is "no records".
- Frictions are computed but don't change the outcome yet. Step 3 makes hungry acts sign tonight or at a dated step.

## 9. Step 3 built: a yes signs tonight, or after its real steps (2026-11-25)

### What changed
- **The ask answers on the spot** (`PlayerDesk.DirectApproach`). A hungry act, or a careful one you came back for on
  a second night, says yes (new status `Agreed`). The 8-day "talking it over" and the hand-off to the weekly
  activation pass are gone for new asks. Older saves' Considering/Ready approaches still run the old way.
- **Nothing in the way: they sign tonight.** With no member under 21, no holdout and no demanding manager, terms open
  in the same conversation. `ApproachToSign` prefixes the yes to its terms message.
- **Each friction is a dated step** (`ScoutingApproachStep`, stored on the approach):

  | Friction | Step | When | How it's taken |
  |---|---|---|---|
  | A member under 21 | Meet the family | The next Sunday | MEET THE FAMILY on the card, in their town, 2h |
  | A shark, svengali or visionary manager | Sit down with him, by name | The next weekday | SIT DOWN WITH THE MANAGER, in their town, 1h |
  | A holdout (3+ members, one below 0.35 ambition) | They settle it at their next set | The next posted set; none posted means any set heard after the yes | Hear a set on or after the date, or attend a rehearsal you arranged after the yes |

  A step can't be taken before its day, or away from the act's town. The manager's demands still come through
  `GenerateTermSheet` at the table. Each step taken writes what was said, and the notebook quotes it.
- **A yes holds for 28 days** (`AgreedHoldDays`) past the agreement or its last dated step, whichever is later. It
  lapses if nobody acts on it, or at once if another label signs the act.
- **Direct signing from Latent.** An agreed act stays Latent, so the AI market never sees it on offer and the weekly
  pass gets no demand from it. `ArtistManager.ReleaseLatentForPlayerSigning` moves it into the free-agent pool
  **inside** `FinalizeSigning`, so the ownership seam reads a normal free-agent contract. That method is the only
  ArtistManager change, and it does nothing unless the player is signing. The terms gates (`ApproachToSign`,
  `CanCommitSceneSigning`, the card, the pad, The Set's Suit) use `CanTalkTerms`: the market's rule, or a yes with
  every step taken. A yes with a step left can't skip it, even if the weekly pass activates the act meanwhile.
- **Rivals:** an agreed act counts as pursued (`IsConsideringApproach`), so a rival's pick of it becomes a courtship.
- **Copy:**
  - The card's status reads "said yes · meet the family Sunday (Jan 31)", "said yes · sit down with Sol Kramer" or
    "said yes · sit down on terms".
  - The first-week line "Sleep on it" is now "Get their yes". The Street, the Suit or the Ear teaches whichever step
    is pending, and the Street's ask line no longer says no is the usual answer.

### Checks (seed 1001)
- **`DispositionChecks`: 51 checks** (26 before). New checks:
  - A hungry act of age with no manager opens terms on the spot and stays Latent, off the AI market.
  - It signs at its ask from Latent and leaves the latent pool.
  - A careful act heard twice says "You came back".
  - Part D hears a fresh band and gives it a minor, a shark and a holdout. It gets three steps: Sunday, a weekday,
    and the next set. The family can't be seen before Sunday, the holdout is not a verb, and each step is taken on its
    day and costs time. The holdout settles at the set heard, and then terms open.
  - An untaken yes lapses after 28 days.
- **`--scouting-runway-check`: 213 checks, thin=0.** The ask check expects Agreed for hungry acts, terms open only
  when they sign tonight, and one step per friction. A yes asks the activation pass for nothing, and being signed
  elsewhere lapses it.
- **`TheSetChecks` 92, `ScoutingUiChecks` 72, `FirstWeekUiChecks` 13.** First week was 14; the missing check is the
  morning paper after END THE DAY, which never comes now because the walkthrough's act says yes and signs the same
  night.

### Measured: the opening (runway Part B, poorest purse)

| Town | First offer: step 2 → step 3 | Asks | Yes (tonight) | Steps taken | Lost to rivals |
|---|---|---|---|---|---|
| Billings | day 8 → **7** | 3 | 1 (1) | 0 | 0 |
| Albuquerque | day 8 → **2** | 2 | 2 (1) | 1 | 0 |
| Nashville | day 15 → **8** (signed) | 3 | 2 (1) | 0 | 0 |
| New York | day 8 → **4** | 3 | 1 (1) | 0 | 0 |

- The first offer now comes when the first hungry act is heard, rather than a week after it was asked.
- **Rival-contest rate: not measured properly.** These openings last 2 to 8 days, too short for a rival to reach an
  agreed act. A real before/after needs a longer player-side run that counts courtships of agreed acts. That
  measurement is owed.
- **AI byte-comparison:** not run. It's planned for the end of step 7. The ArtistManager addition only runs from
  the player's signing call.

### Not done in step 3
- The holdout always comes round once you're in the room. A holdout who still says no, or quits the band, is step 9.
- A family visit or manager meeting can't be missed or rescheduled: it opens on its day and stays open until the yes
  lapses.
- The step verbs aren't in the conversation modal or The Set yet, only on the act card.

## 10. Who the manager is (2026-11-25): drafted, decided, built

### The problem
`ManagerArchetype` has five rows: None, LocalHustler, Shark, Svengali and Visionary. They're **deal postures**, and
the AI economy reads them: advance and royalty multipliers, the Shark's chart push, the Svengali's production lift,
and master deals. The roll is one `Randf()` at generation, so adding a row reshuffles the population stream
(ManagerSystem's own note). What the player meets, though, is always "a personal manager" with a generated name. In
1960 the man across the table was a father, the club owner, a deejay, a songwriter, an agency man, a jukebox operator
or a pro. That's where the variety belongs.

### The proposal
A **manager identity**, read from state with no draw and nothing stored (like `ActDisposition`). It sits on top of
the archetype the act already rolled and never changes the numbers, so the AI is untouched. Each identity lists the
archetypes it can sit on. Rules run in the priority order below, and an act takes the first that fits. Shares inside
a rule are keyed on the act's id (as `ConfigureEasyListeningBandleader` does), so a save always reads the same.

| # | Identity | Sits on | When it applies | Period reference |
|---|---|---|---|---|
| 1 | **Family**: father, mother, older brother or uncle. Shares a member's surname | Hustler, Svengali | A member under 21, or members sharing a surname; about half of such managed acts | Murry Wilson managed and produced the Beach Boys from 1961 |
| 2 | **The club owner** whose room they hold: the room's existing contact | Hustler, Shark | The act holds a standing engagement (`EngagementRoom`) | Bandleaders and club owners ran their house acts; Johnny Otis built and handled Little Esther and Etta James |
| 3 | **The record-hop deejay**: a local deejay, preferring one already in the Rolodex | Hustler, Shark | A town with a station and record hops, for an act with local draw | Payola hearings, 1959-60; Alan Freed's co-writer credit on "Maybellene" |
| 4 | **The songwriter-producer** building the act around his songs | Svengali, Visionary | A Svengali on an act that writes little, or a vocal group | Phil Spector with the Teddy Bears (1958) and the Crystals (1961); Leiber and Stoller with the Coasters |
| 5 | **The agency man** from GAC, William Morris or Shaw | Shark, Visionary | An act with a past contract or a chart record, or based in New York, Chicago or Los Angeles | Agencies booked the package tours and held established acts |
| 6 | **The jukebox-route operator**: money from the vending business | Shark | A keyed share of Sharks in the big jukebox cities (New York, Chicago, Detroit, Philadelphia, Newark) | Jukebox and vending money behind labels and acts, e.g. Morris Levy at Roulette (history only; no real names in the game) |
| 7 | **The personal manager**, a professional | Visionary, Svengali, Shark | The fallback for Visionary and Svengali, and for Sharks on better acts | Colonel Parker (Shark-like); Albert Grossman (Visionary: folk, Peter, Paul and Mary, 1961) |
| 8 | **The hustler** from the neighbourhood: a car salesman, a barber, a friend who knows a booker | Hustler | The fallback for every Hustler | Most bar bands' "manager" was this |

### What it changes (player-side only)
- **Who you meet and where.** The step-3 sit-down moves to his place, on a day that fits him:
  - the club owner's club on a night they play
  - the deejay's station after his shift
  - the agency's office
  - the operator's booth at a restaurant
- **Family merges the steps.** When the father is the manager, the family visit *is* the sit-down: one Sunday, one
  step.
- **His line at the sit-down and in the Rolodex.** The Rolodex title becomes, for example, "Sal's father", "owns the
  Michigan Roadhouse" or "WJLB record-hop deejay" instead of "Personal manager". The family name comes from the
  member; the club owner and deejay are people already in the world.
- **The Street can know him.** "That's Frank's band. Frank owns the Roadhouse, and he likes his Friday crowd." This
  feeds step 7 (leads).

### Options beyond the identity (each moves the opening mix, so each needs its own measurement)
- **Club owner as a Comfortable reason:** "Frank won't let us go." The engagement pull already exists; this would
  name who is pulling.
- **Deejay wants a cut** at the table: a co-writer credit on the A-side, through the existing publishing axis. It's
  period-true and gives the player a real choice.
- **Agency man out of town:** the sit-down is in the agency's city, so it costs a trip.

### What stays the same
- The roll, `ManagerProfile` numbers and the AI's term sheets. The AI CSVs stay byte-identical.
- The archetype's demands at the table. The identity only says who is making them.

### Open questions for the author
1. Are eight identities the right list? The candidates for cutting are the jukebox operator (tone) and the agency
   man (rare in the opening).
2. Should the family identity be visible before the ask? ("Her father does their business.")
3. Should any of the options above go into step 3.5, or wait for step 4 (regard)?

### Decided (author, 2026-11-25)
1. Keep all eight, but the agency man and the jukebox operator appear only for acts that would realistically have
   one:
   - **Agency man:** only acts with a record career behind them (a past contract, or Rising and up). Being based in a
     big city doesn't count.
   - **Jukebox operator:** only R&B, doo-wop, blues, soul and rock and roll acts in New York, Chicago, Detroit and
     Philadelphia.
2. The band says so when a relative manages them. The notebook card shows it once you've heard them: They say:
   "Sal's father handles our business."
3. The extras (club owner as a Comfortable reason, the deejay's cut, the out-of-town agency) wait for step 4.

### Built
- **`Systems/ManagerIdentity.cs`:** a pure read with no draw and nothing stored. Shares are keyed with `BandLife.Unit`.
  - Family 0.5 of eligible acts, deejay 0.15, vocal-group songwriter 0.4, jukebox 0.5.
  - The deejay share was first 0.30, which made deejays 15% of managed acts. That was too many.
  - Family names are a period first name plus the member's surname. Relation: father 55%, mother 20%, uncle 13%,
    older brother 12%.
  - The club booker is the engagement room's existing contact.
  - Agencies are fictional: Consolidated Artists, National Talent Bureau, Continental Booking, Allied Artists Agency.
- **Step 3 uses it.** The manager step names who he is and meets at his place:
  - the club booker at his room, on a night it's open
  - family at the kitchen table on Sunday
  - everyone else at his place on a weekday
  - When the family is the manager *and* a member is under 21, the Sunday visit is the sit-down: one step, not two.
  - The line spoken at the sit-down is the place (who he is) plus the archetype's manner (what he'll be at the table).
- **Names shown to the player** come from the identity, and the AI's term sheets are untouched. Places:
  - the Rolodex manager card, with "Sal's father", "books the Michigan Roadhouse" and so on as its title
  - the act's rights text and manager note
  - the roster sleeve
  - the player's baseline term sheet
  - rival offers and rival meeting news
- **Unchanged:** the roll, `managerName`, `ManagerProfile` and every AI read. The checks confirm that reading
  managers changes no world state.
- **A known wrinkle:** the club booker is read from today's engagement. If the residency ends, the act's manager
  reads differently from then on. A yes already given keeps the name it was given with.
- **Measured (seed 1001, 1960 opening, unsigned active acts):**

  | Manager | Acts |
  |---|---|
  | Unmanaged | 1,567 |
  | Hustler | 970 |
  | Personal manager | 946 |
  | Family | 448 |
  | Songwriter-producer | 229 |
  | Deejay | 206 |
  | Jukebox operator | 46 |
  | Club booker | 37 |
  | Agency | 0 |

  Agency is zero because almost no unsigned act in the opening has a record career. It appears with dropped acts later
  in the decade, which is the point of decision 1.
- **Checks:** `DispositionChecks` 61, adding Part E: every kind sits only on its allowed archetypes, family shares a
  member's surname, the club booker is the room's booker, agency and jukebox only where realistic, repeatable reads, no
  world state touched, and the family merge. Runway 213, The Set 92, Scouting UI 72 and First Week 13 all pass.

## 11. Step 4 built: regard, read from what you've done (2026-11-25)

### What changed
- **`Systems/PlayerDesk.Regard.cs`.** `ReadRegard` is a pure read over the label's own records of the act. There's no
  new stored ledger, so an older save reads the same way it would have been built.

  | You did | Points | Read from |
  |---|---|---|
  | Heard them, each different night (at most 3) | +1 | performance observations |
  | Sat down with them at an arranged meeting (once) | +1 | the meeting's "attended" statement |
  | A studio session together: a rehearsal visit or a demo | +2 first, +1 second | rehearsal and demo samples |
  | Got them a night from a booker, and they played it | +2 first, +1 second | the player's bookings |
  | Didn't turn up to produce their demo | −2 | demo jobs the label missed |
  | Said yes and you never acted on it before it ran out | −2 | the approach's new `LetLapse` flag |
  | Cancelled an arranged meeting on them | −1 | the meeting's "cancelled" statement |

  Good points fade by one every 45 days once 30 days have passed since you last did anything with them, never below
  zero: neglect isn't an insult. Bad turns fade after half a year. Levels, as the act shows them: let down (below 0),
  stranger, knows your face (1-2), friendly (3-4), trusts you (5+).
- **What it does at the ask** (`PlayerDesk.DirectApproach`):
  - **Careful:** yes once regard reaches 2 (3 for an act dropped before). Hearing them twice still works. So does one
    night and a sit-down, or one night and a session.
  - **Hungry, let down:** wants you back at a set before it says yes again. One night mends one bad turn.
  - **Comfortable:** at regard 5 it hears you out ("You've been good to us. We'll hear you out, but…"). It stays
    comfortable, and at the table it talks terms rather than signing what it's handed (a Pushover posture becomes Firm;
    `ScoutingDirectApproach.Hearing`). Regard never makes it hungry; that still needs the reason to change.
  - An act that knows you opens with what it remembers best: "You were at the River Lantern Friday, weren't you?",
    "You came out and heard us work", "You got us the night at…", or, let down, "We said yes to you once, and then we
    never heard from you."
- **Careful wants name verbs.** "Never heard a record of yours" now asks you to come again *or come out to a
  rehearsal*. "Dropped before" wants to see you a few times and see you put some work in. A comfortable no adds that
  trust might at least earn a hearing.
- **The notebook card** gets "Between you: they're friendly with you (heard 2 nights, a session together)."
- **Two plumbing fixes that the verbs needed:**
  - *Sessions with acts off the market.* A rehearsal, demo or sit-down needed `IsEligibleForPopulationSigning`, the AI
    market's Seeking flag, both to agree and to keep the appointment. So no off-market act could ever be heard working,
    which is §1.1's problem again. Now an off-market act the player has heard agrees for itself
    (`PlayerDesk.WouldMeetOffMarket`): hungry or careful acts yes; a comfortable act once regard is 2. An appointment it
    agreed to holds while it stays unsigned. Player label only: conversations are quoted only for the player's label.
  - *Getting an unsigned act a night.* PITCH THE BOOKER took roster acts only. It now also takes an unsigned act you've
    heard and hold a number for, as a favour: no commission (`TakeBookingCommission` skips an act not on the booking
    label). The booking form lists them as "(unsigned: a favour)". AI worlds hold no bookings.
- **Not a regard source:** "your label charted in their genre". A release and a hit already raise drive (§8); a genre
  match would need the label's chart record by genre, which isn't kept.
- **Build:** `Label Man.csproj` now drops every `tmp\**\*.cs`. A nested checkout under `tmp\` was compiling into the game.

### Checks (seed 1001)
- **`DispositionChecks`: 82 checks** (61 before).
  - The lapse check now goes on: the act remembers the lapsed yes (regard −1, let down), says so, and wants you back
    at a set. The notebook says what you did.
  - Part F checks the shortfall rules, then builds regard on one heard act with real verbs. One night is regard 1, and
    reading it changes no world state. A comfortable act that hardly knows you says no and won't sit for a session; a
    careful one off the market will. A real demo at your place brings regard to 3, which meets a careful request with
    no second night. A second demo and an attended sit-down bring it to 5: the comfortable act hears you out, stays
    comfortable, and once its holdout settles at a set it sits at the table Firm, not Pushover.
- **Existing suites pass:** `--scouting-runway-check` 213 (31 cities, thin=0), `--scouting-appointment-check` 61,
  `TheSetChecks` 92, `ScoutingUiChecks` 72, `FirstWeekUiChecks` 13, `ConnieChecks` 49.
- **AI byte-comparison:** not run; still planned for the end of step 7. What touches world code is player-gated: the
  conversation consent reads the player desk and conversations are quoted only for the player's label, and bookings
  exist only in player worlds.

### Not done in step 4
- **Rehearsals are hard to get in Detroit by March 1960.** The check meant to use a rehearsal visit, but every room
  near the acts it heard was booked through its open hours (`RehearsalRoomOccupied`), so it uses demos. That's a
  world fact about room calendars, not a regard rule, but it means the rehearsal want that careful acts now name is
  often hard to meet. Worth a look with The Talk (step 5).
- **The favour booking isn't driven by a check to a played set.** The pitch, the form and the no-commission rule are
  built; a check that waits for the booked night to be billed and played is owed.
- **The opener can name the holdout's own speaker.** A lukewarm band's spokesperson can also be its holdout ("June
  isn't sure yet", said by June). That's a step-3 wording issue; the holdout beat is step 9.
- **The "hearing" threshold (5) and the weights are authored.** Nothing measures how often a player reaches them in
  an opening.

## 12. Step 5 built: The Talk (2026-11-25)

### What changed
- **`Systems/PlayerDesk.TheTalk.cs`, `UI/PlayerDeskPanel.TheTalk.cs`.** TALK TO THEM (from the card or The Set) no
  longer opens the six-button modal. The conversation takes the A&R page, the way a call takes the Rolodex (audit §5
  decision 1). `BuildTalkScene` is a pure read; the evening's transcript is kept in memory and dropped when you end the
  conversation, so nothing is saved.
- **Who and how.** The setting says whether you caught them after the set, rang a number, or are only looking them
  up. Their spokesperson (`SpeakerFor`) opens with a line chosen by where things stand:
  - signed elsewhere: who with, and "ask what you like"
  - a yes: "We meant what we said", plus the step still to take
  - a no that holds, a request you haven't met, or a let-down act: they say so
  - shopping themselves: "We're looking for a deal"
  - nobody's asked yet: their manner follows the disposition (hungry: "We've been hoping somebody would come out";
    careful: "Which one?"; comfortable: "Thanks for staying for the set"), led by what they remember of you
  - a family manager is named (§10 decision 2)
- **The voices.** The Street reads what they want, gated like the Rolodex: a hint from 3, the reason from 4 ("Day jobs,
  the lot of them. A record's the way out."), the frictions from 5 (a minor, a holdout, a manager who'll want a
  sit-down). The Fixer's rival read is now shared with The Set (`FixerRivalRead`).
- **What you say**, each an existing verb with its cost or why not underneath:

  | Line | Verb |
  |---|---|
  | "Would you hear an offer from us?" | `ApproachToSign` (the ask) |
  | "Let's sit down on terms." | `ApproachToSign` (terms open; the contract takes the page) |
  | "I'll come to Sunday dinner…" / "I'll sit down with Sol Kramer." | `TakeApproachStep` (step 3's verbs, now in the conversation) |
  | "Who've you dealt with before?" (Suit) | `AskScoutingBusinessHistory` |
  | "Can I hear you rehearse, somewhere quieter?" (Ear) | rehearsal invitation |
  | "Put a song on tape for me, at my place." (Ear) | demo invitation |
  | "Can we sit down properly this week, just us?" | return meeting |
  | "How do I reach you?" (Street) | `AskActForNumber` |
  | "When are you on next?" (Street) | answered from the posted calendar, free |
  | "Give me a week." (Fixer) | `HoldWithHandshake`, only where the act has a legacy watch entry |

  The answer comes back into the transcript, not a toast. An act signed elsewhere is offered only its history and its
  number.
- **The old modal is gone** (`OpenScoutingConversation`, `ScoutingMeetingInvitation`). T3's glow now finds The Talk's
  lines first on the Ask, Answer and Terms steps.
- **Regard openers never quote tonight.** "You were at River Lantern Friday, weren't you?" was being said right after
  the only set heard. `RegardOpener` now takes a day and uses only what happened before it, in The Talk and in the ask.

### Checks (seed 1001)
- **`SimTools/TheTalkChecks.tscn`, new, 126 checks** (`--talk-city=`, Detroit by default). It hears the opening days'
  sets and builds the talk right after each one, at voices 1, 3 and 5. Every talk opens with a real member speaking.
  Building it twice gives the same scene and moves no world or player state. Every voice is silent at 1, and the
  Street's deep read builds on its hint. An act signed elsewhere says who with and gets no ask. For every free act
  heard, the ask, their history, a rehearsal, a demo, a return meeting, their number and their next date are all lines
  (audit §4 row 3), each with its cost or why not. Saying "when are you on next?" costs nothing and is answered in the
  transcript. The ask is the desk's own ask, and its answer lands in the transcript. When terms open, The Talk hands
  over the contract. Ending the talk drops the transcript. On the A&R page: they open, every line is there and greyed
  exactly when it can't be said, a pressed line's answer appears, and END THE CONVERSATION goes back to the notebook.
- **Updated:** `ScoutingUiChecks` 74 (72): its two conversation legs now read The Talk's lines on the page.
- **Pass unchanged:** `DispositionChecks` 82, `TheSetChecks` 92, `FirstWeekUiChecks` 13, `ConnieChecks` 49,
  `--scouting-runway-check` 213 (31 cities, thin=0), `--scouting-appointment-check` 61.
- **AI byte-comparison:** not run; still planned for the end of step 7. Presentation only, apart from the opener's day.

### Found while building it: who closes Detroit's nights
The first TheTalkChecks run heard only signed acts on Detroit's first day. From the probe's slot log
(`SimLogs/signing-availability.csv`, seed 1001, 56 days):
- Detroit's first day has two rooms. The Club runs two unsigned acts under a signed resident (a MidTier Star, 19
  charted). The Roadhouse's whole bill is signed, all three to Boutique labels. The check walked the Roadhouse first and
  ran out of evening. So the first night is 4 of 6 signed, not 6 of 6.
- **Residencies are where the signed share lives.** Nearly every closing set is a resident (102 of Detroit's 104; `NightOrder` puts a regular on
  last). In Detroit, 101 of 102 residency slots are signed acts, against 49% of the other slots. Across all towns,
  residencies are 67% signed and other slots 49%.
- **It's town by town.** Residency slots signed vs unsigned: Cleveland 106/0, Omaha 151/4, Boston 100/8, Detroit
  101/1, Los Angeles 93/7; but Portland 13/117, Salt Lake City 8/113, Seattle 30/86, San Antonio 50/86, Phoenix 47/71.
- **Why (from the code; not yet measured):** `OfferEngagements` gives a room's two standing engagements to its
  strongest working residents by `BookingWeight` (live performance + reputation, times the room's history with the act).
  Where AI labels have already signed a town's best working acts, the residents are signed acts. A label signing an act
  doesn't appear to take it out of the rooms, so it keeps its residency. The towns with many opening labels (Detroit, Cleveland,
  Boston, New York) show this most; the thin-label western towns least.
- **What it means for §3.5:** lever (a) (national acts leave the small rooms) only reaches 17.6% of slots. A narrower,
  more period-true lever is the residency itself: a signed act on a national push wouldn't hold a Tuesday-to-Saturday
  at a roadhouse. To measure next: the reputation and live-performance gap between signed and unsigned residents, and
  how many residencies a signed act holds after its first release. That's world booking, so it belongs to step 8 and
  needs an AI A/B. **Author agreed (2026-11-25); written up as §3.5 (a′), the first lever for step 8.**

## 13. Step 6 built: signed as a clock (2026-11-25)

`Systems/PlayerDesk.SignedClock.cs`. A signed act isn't a dead end; it's a deal with a date on it.

- **The Street reads their paper in The Talk.** At 3: the label, the sides out under this deal, and what charted
  ("They're with Midtown Records. Nothing out yet."). At 4, when it's up: "Deal's up in December 1961, or sooner if
  they deliver the sides they owe." At 5, what the label will likely do and why. The read follows
  `RosterManager.ShouldResignArtist`: a star, a rising act with momentum, an established act still selling, or a Top 40
  under this deal gets kept; a declining act, or a new signing with two sides and nothing to show, gets let go; anything
  else could go either way. It's a read, not an oracle: it can't see the label's money or its loyalty roll.
- **"I'll keep an eye on you."** A free line in The Talk for an act on another label, answered in the transcript. Up
  to six at once; "Good luck with it. I'll stop keeping tabs." undoes it. The card says you're watching, with the
  clock (and the Street's read from 3).
- **The reminder.** Eight weeks before a watched term ends, the ledger notes it once per deal, with the Street's read
  from 3. A renewal or a move to a bidder is noted too, and the watch carries on.
- **The call.** `RosterManager.ContractEndObserver` (new) tells the desk how each matured AI contract ended, right after
  the decision. When the label lets an act go, the act rings the office first if you were watching it and it knew your
  face then or now, or if it's friendly with you today. It never calls if it feels let down. Its number goes in your
  book. The talk opens "Midtown Records let us go. We wanted you to hear it from us first." Other labels can't sign it
  for a week (the same `SetPlayerHold` as a handshake), set inside the AI's pass so no market reaches it first. After a
  week, it's talking to other labels. A stranger the label lets go doesn't call.
- **Why watching freezes regard:** regard fades after 30 days apart, and a deal runs one to three years. Without the
  freeze, nobody would ever call. Saying you'd keep an eye on them is what they remember.
- **Save:** `PlayerSaveData.SignedWatches`, null in older saves. Restore puts back the holds of acts still waiting on
  your call.

**Touches the AI:** only the hold on an act that called, and only in a world with a player label. With no label the
observer returns at once. Steps 1–7 still owe their one AI byte-comparison at the end of step 7.

### Checks (seed 1001)
- **`SimTools/SignedClockChecks.tscn`, new, 39 checks.** It hears a signed act on Detroit's opening day (The Ridge
  Fiddlers, Midtown Records). Then: the Street says nothing at 1, the paper at 3, the clock at 4, and the likely call at
  5. Building the talk moves nothing, and there's still no ask or handshake. Watching is answered in the transcript and
  shows on the card. No reminder months out, then exactly one. A renewal keeps the watch. When the label lets them go,
  they call: a number, a hold, the opening line, the card. The call and hold survive a save. A stranger doesn't call.
  After a week the hold lifts.

## 14. Step 7 built: leads (2026-11-25)

`Systems/PlayerDesk.Leads.cs`. A reason to go and hear somebody before you've heard them, said by a source that could
know it. NEXT SETS shows each set's leads on its card, and puts the sets the town is talking about first (among the
soonest twelve free ones).

- **The Street passes on the noise**, all of it real state: a residency, and "the room keeps having them back" after 13
  nights there; a room that was packed this week with them on the bill; the people who come in for them, from the room's
  own draw record; an original from their last set in a packed room ("Kids are asking for "My Truck", one of theirs").
- **The fog (§3.6), in the noise's direction.** Below 3 the Street talks the noise up ("Everybody's talking about…")
  and can't tell a crowd from a band, so a loud, ordinary band sounds like the one to see and a good quiet band isn't
  mentioned. At 3 it passes the noise on plainly. From 4 it says when the noise is the room's ("It's the room, not the
  band. They're loud and not much else.") or that the band is the real thing. At 5 it also hears about good bands that
  play the rooms but nobody's talking about yet.
- **A booker you've met** recommends an act that has played their room and is their regular or draws there: "You should
  catch Lawrence Scott Saturday. They're our regulars. Trust me."
- **The Fixer, from 3,** passes on that an AI label's daily market looked at an act that's still free, for four weeks.
  The looks come from the AI market's own appointments and are kept in the player's scene knowledge
  (`SceneInformationKnowledge.RivalLooks`, saved; null in older saves). They're rare in the opening week, because a
  market that looks at an act usually signs it.

**Calibrated to what the world actually does** (seed 1001, opening week, 3,977 free acts in 31 scene towns, 1,998 of
them playing rooms):
- **No bill ever fills.** Attendance is capped by expected audience: median 57% of capacity, maximum 68%. "Packed" is
  65%, the top of that range.
- **An act draws at most a fifth of a room:** median 10%, p90 14%. A draw lead is 14%.
- **"The real thing" and the quiet good bands** are the top tenth by `CalculateBaseQuality` (0.69). "Not much else" is
  below 0.45, about the bottom fifth.
- **Result:** 122 free acts have noise (83 residents, 33 packed rooms, 31 draws). A Street of 2 hypes 23 loud ordinary
  bands, which a Street of 4 calls the room's. A Street of 5 hears 197 good quiet ones.
- **Detroit's free acts get almost no leads,** because its residents are signed (§12). The western towns carry most of
  them. That's the step 8 lever again.

**Not done from §3.6:** "draw growing week on week" and "a residency extended". The world keeps neither: draw is a
running total per room, and engagements are pruned as their quarter ends. Leads use the room's show count and the
current residency instead. The "who's free" fact stays a filter on NEXT SETS (signed acts are left off and counted)
rather than a Street line. Putting signed acts back would fill the three slots with acts you can't sign.

### Checks (seed 1001)
- **`SimTools/LeadsChecks.tscn`, new.** It runs the opening week, then reads the leads for every free act in every scene
  town at each Street level. Building them moves nothing, and they read the same twice. A residency is named exactly
  when the act holds one, a draw is the room's own figure, and a packed room really was. The Street talks the noise up
  below 3, never reads it, and never mentions a quiet good band. At 3 it's plain. At 4 it flags exactly the ordinary
  bands a low Street hyped. At 5 it hears the quiet good ones. A booker speaks only after you've met them, and only for
  acts that played the room. The Fixer speaks from 3, names the label, goes quiet after four weeks, and its look
  survives the save. NEXT SETS shows the leads (`--talk-city=portland`; Detroit has none on today's list).
  `--leads-dist` prints the distributions above.
- **Pass:** `SignedClockChecks` 39, `TheTalkChecks` 126, `DispositionChecks` 82, `TheSetChecks` 92,
  `FirstWeekUiChecks` 13. `ScoutingUiChecks` 72 (was 74). NEXT SETS now orders by leads, so the card it presses leaves
  the player in a different room. Its short-listen fixture now leaves early by the walk across town. Its conditional
  TALK TO THEM leg is skipped, because the act it hears now is already looking for a deal.

### AI byte-comparison for steps 1–7 (owed since step 2)
104 weeks, seed 1001, `--enable-genre-market-v2 --enable-artist-population-lifecycle --aggregate-only`. Before step 2
(`db9c144`, built in a worktree, DLL swapped in) against steps 2–7 (`sigbase-1001-104` vs `sig7-1001-104`): **all 100
CSVs byte-identical.** With no player label, the new observer, the rival-look recorder and the leads change nothing in
an AI world.

## 15. Step 8: residency surrender on a national push, measured and built behind a flag (2026-11-25)

### Measure first (§3.5 a′)
`SimTools/ResidencyCensusProbe.tscn`, new: an AI-only world (no player label). Each week it records every standing
engagement with the resident's deal and record, its booking weight, and the room's next pick by that same weight
(`LocalSceneRoomService.ResidencyCensusForProbe`, read-only). Seed 1001, 52 weeks, 15,111 resident-weeks:
- **Signed acts hold 59% of residency-weeks,** drifting from 71% in week 0 to 54% in week 51.
- **Unsigned residents play as well or better.** Live performance is 0.85 against 0.80 for signed residents. They're
  less known (reputation 0.11 against 0.30), so the booking weight is close (19.5 against 21.3). The supply to fill
  freed slots is there, which answers the "measure first" question: (b) isn't needed to start.
- **If a nationally established signed resident left,** the room's next pick would be unsigned 44% of the time and
  another signed act otherwise. The replacement is weaker on weight (18 against 24) but plays the same (live 0.80).
- **Released signed acts hold one residency in 74% of their weeks, two in 19% and three in 6%.**
- **Which definition to use.** "Has an old Top 40" covers 60% of signed residents, mostly veterans on new deals, so it
  doesn't mean a push now. Rising-or-better careers plus a Top 40 under the current deal cover 31% of signed residents
  and 18% of all residency slots.

### The rule (`--enable-national-residency-surrender`; **on by default since 2026-11-25**, `--disable-national-residency-surrender` is the control)
`LocalSceneRoomService.NationalPush`: a signed act whose career is Rising, Established, Star or Superstar, or that has
a Top 40 under its current deal. A declining act and old hits don't count. Such an act gives up its standing
engagements (they end the day before the scheduler next books its room), and `Regular` won't offer it new ones. It
still plays one-off nights by the night-order draw. Touring isn't modelled.

### Screen (seed 1001)
- **Flag off: byte-identical.** 104 weeks, all 100 CSVs match step 7's run (`sig8off` vs `sig7`).
- **The rooms (census, 52 weeks):**

  | | flag off | flag on |
  |---|---|---|
  | Signed share of residency-weeks | 59% | 50% |
  | Week 0 / 26 / 51 | 71% / 64% / 54% | 60% / 54% / 44% |
  | National-push residents (resident-weeks) | 2,763 | 27 |
  | Unsigned residents' live performance | 0.85 | 0.86 |

  By town: Los Angeles goes from 77% signed to 11%, Detroit 70% to 48%, Boston 90% to 69%, Omaha 85% to 75%, Seattle
  35% to 25%. Cleveland stays at 98–100%: its residents are new signings on small labels, which this rule rightly
  leaves alone.
- **The AI world (104 weeks, one seed):** 88 of 100 CSVs differ, as expected. Who plays where moves the live-draw
  recognition deposit and every keyed draw after it. Headline numbers move little and in mixed directions:
  - CR4 1960 0.296 → 0.284, 1961 0.261 → 0.263;
  - major-family chart share 1961 0.449 → 0.442;
  - firms charting 1961 227 → 234;
  - Top 40 acts 1961 212 → 218;
  - album-chart acts 1961 134 → 117;
  - label tier transitions 53 → 38.

  One paired seed can't separate any of this from a reshuffle (Top 10 shares need about 8 seeds). Album-chart acts and
  tier transitions are the ones to watch in a multi-seed screen.

### Three-seed screen (seeds 1001, 2002, 3003; 104 weeks; flag on minus flag off)
Mean delta, with its standard error and per-seed deltas. Effects with the same sign on all three seeds are in bold.

| | 1960 | 1961 |
|---|---|---|
| CR4 chart share (off ≈ 0.345 / 0.251) | **−0.029** (se 0.009; −0.012 −0.040 −0.036) | −0.014 (se 0.015; +0.003 −0.043 −0.002) |
| CR8 | **−0.033** (se 0.015) | −0.010 (se 0.012) |
| Major-family chart share (off ≈ 0.545 / 0.460) | −0.016 (se 0.024) | **−0.029** (se 0.011; −0.007 −0.040 −0.040) |
| Firms charting (off ≈ 235 / 239) | −1.7 | **+14.3** (se 3.8; +7 +20 +16) |
| Top 40 acts (off ≈ 193 / 211) | −1.3 | **+5.7** (se 0.3; +6 +6 +5) |
| Top 10 acts | **+5.0** (se 1.7) | −2.3 |
| Album-chart acts | +4.7 (se 6.9) | −4.3 (se 9.8; −17 −11 +15) |
| Chart units (M) | +0.2 | +2.0 (se 2.0) |
| First-time signings | −2.7 | **+47** (se 24; +22 +96 +24) |
| Label tier transitions | 0 | −1.0 (se 7.8) |

**Reading.** The rule spreads the market a little. Concentration falls (CR4 1960 −3 pts on every seed; the major
family's 1961 share −3 pts). More firms chart (+14 in 1961) and more acts reach the Top 40 (+6). AI labels sign more
first-time acts (+47). The likely route: the live-draw recognition deposit national acts earned as residents now goes
to the local acts that take their place. Seed 1001's drop in album-chart acts and tier transitions didn't replicate.

**What it costs:** it moves calibrated targets in the direction away from the late-decade consolidation goal (major
share, CR4). Three seeds give the direction; the size of a Top 10 or CR4 shift needs about 8 seeds (memory: 3-seed SE
understates the reshuffle).

### Against history: Top 10 slots by operating label (the review basis)
The CR4 above is a chart-unit share. The adopted review bands are on weekly Top 10 slots by operating label
(`analyze_label_tier_historical_benchmarks.py`, from the Billboard reconstruction: 1960 CR4 34.6 / CR8 52.7, band
30–40 / 48–58; 1961 27.9 / 43.5, pooled band 22–32 / 38–48). The same three seed pairs, rerun with full telemetry
(`sig8full{off,on}-<seed>-104`; flag-off concentration matches the aggregate runs byte for byte):

| Mean of 3 seeds | 1960 CR4 | 1960 CR8 | 1961 CR4 | 1961 CR8 | Major share of Top 10 slots 1960 / 1961 |
|---|---|---|---|---|---|
| History (reconstruction) | 34.6 | 52.7 | 27.9 | 43.5 | |
| Review band | 30–40 | 48–58 | 22–32 | 38–48 | |
| Flag off | **42.8** (38.5, 43.7, 46.2) | 58.1 | **33.5** | 47.1 | 47.9 / 37.2 |
| Flag on | 38.0 (36.7, 38.1, 39.2) | 52.0 | 31.3 | 45.1 | 45.0 / 35.3 |
| On − off | −4.7 (se 1.6; −1.7 −5.6 −6.9) | −6.2 (se 1.1) | −2.1 (se 4.0) | −2.1 (se 2.6) | −3.0 / −1.9 |

**Today's default sits above the band in both years:** 1960 CR4 42.8 against a ceiling of 40, and 1961 33.5 against
32. (Memory had 1960 inside the band at 37.85 on five seeds in October; the world has drifted since.) The flag brings
both years inside, and 1960 CR8 to within a point of history (52.0 against 52.7). The 1960 move is the same sign on
every seed. 1961 is within noise, but its mean is also inside the band. What looked like a cost on the chart-unit
basis is a correction on the basis history is measured on.

**Status: adopted as the default (author, 2026-11-25).** It moves Top 10 concentration toward the Billboard
reconstruction in 1960 and 1961, and the rooms toward period booking. The cost is about 3 points of chart-unit
concentration, which the late-decade consolidation calibration has to absorb.

- `LocalScenes.NationalResidencySurrender` is now on whenever room rotation is.
- `--disable-national-residency-surrender` restores the old rooms as a control.
- `--enable-national-residency-surrender` still works for older scripts. It's rejected alongside the disable flag or
  without room rotation.
- The decade check is owed at audit end. Late-decade consolidation comes from absorption after 1966, not from rooms.
- Every AI baseline recorded before this date ran with the old rooms. Compare new runs against a
  `--disable-national-residency-surrender` control, or re-run the control.

## 16. Step 9 built: the holdout, and courtship objections (2026-11-25)

### What changed
`Systems/PlayerDesk.Objections.cs`, new. In two places where someone isn't sure, they now raise **one grounded
objection**, and you answer it in The Talk. No draws: the same state gives the same objection and the same answer
(§2 principle 6).

- **The holdout speaks for themselves.** In step 3, the holdout came round as soon as you were at their set. Now they
  come over at the first set or rehearsal you attend after the yes, and say the one thing in their way. It's read from
  that person, in this order:

  | Objection | When | What they say | The true answer (voice) | True when |
  |---|---|---|---|---|
  | Songs | they're the band's writer | "They're my songs. I'm not signing them over to somebody I've just met." | "I came for your songs. We'll cut yours, not ours." (Ear) | half or more of what you've heard them play is their own |
  | Road | children, or married | "I've got a family at home. I'm not living on a bus for a maybe." | "Nobody goes on the road till a record sells. You'd record here, in Detroit." (Suit) | your label is in their town |
  | Money | they don't need it (wealth at the studio-only level) | "I don't need the money, and I don't need the trouble." | "You're the best player up there. That belongs on a record." (Ear) | they're the band's strongest player |
  | School | under 21 | "My folks want me finishing school, or learning a trade. Not playing records." | "Sessions are Saturdays. School comes first, and I told your folks so at their table." (Suit) | you've been to the family's Sunday dinner |
  | Job | 25 or older, and the act works day jobs | "I've got a good job. I'm not quitting it for a maybe." | "Keep the job. We record on weekends until one sells." (Suit) | always |
  | Fun | ambition below 0.2 | "I'm in this for the fun. A record's somebody else's idea." | none: take your time, or go on without them | never |
  | Cheated | none of those | "Labels cheat you. Everybody knows somebody who signed and never saw a dime." | "Ask [your act]. We've paid them royalties, and the paper shows every deduction." (Suit) | an act on your roster has recouped and been paid royalties under your contract |

  **Reworked the same day.** The first cut checked the act's day jobs ahead of everything but a catch-all, and 80% of
  holdouts said Job. A day job was a real reason to leave a band, but at the point the band went on the road, not at
  the signing: a 1960 deal meant a few sessions, and touring came after a hit. So Job now applies only to a holdout of
  25 or older with a working life to keep. The young, unambitious member who is usually the holdout now says what such
  a member would: their folks want school, they're only in it for the fun, or labels cheat. Trust ("I don't know you")
  remains only for objections already raised in older saves.

- **Three ways to answer**, each a line in The Talk:
  - **The true answer.** When its fact isn't true, the line is greyed and says what would make it true ("You haven't
    heard enough of their own songs to say so"). Once you say it, the holdout is in.
  - **"Take your time. I'll be at the next one."** The holdout settles it at the next set or rehearsal you attend. This
    is step 3's old beat, one night later, and now your choice.
  - **"Then the rest of you can sign without you."** (Fixer.) Offered only in a band of four or more, and not when the
    holdout leads the band, sings, or writes. The member leaves (`BandLifeService.RemoveFromAct`, alumni reason
    "wouldn't sign") and the rest sign. When the objection was a family or a day job, that's the departure kind.
- **A comfortable act that hears you out** (regard 5, step 4) now puts its own reason to you as a question before it
  talks terms. This is a new step kind, `Objection`, placed first on the yes:

  | Reason | They ask | True answer | True when |
  |---|---|---|---|
  | Engagement | "What do we get for walking out on the Flame?" | "Keep the room. A record on the jukebox fills it." (Street) | your label has a record out |
  | No need for money | "Sal doesn't need the money. So what's in it for us?" | "Your own songs, on a record with your name on it." (Ear) | you've heard their own songs |
  | Settled / weekend act | "We've got jobs and families. Who's going on the road?" | the Road answer (Suit) | your label is in their town |
  | Local living | "A record won't pay the rent. Why give up the rooms?" | "A record gets you the better rooms. Ours get played." (Street) | your label has a record out |

  "I'll come back when I've more to show you" keeps the yes open, so you can return once the fact is true (when your
  first record is out, say).
- **A yes holds 28 days from the last conversation.** `AgreementHoldsUntil` now also counts the day an objection was
  put to you and the day you told a holdout to take their time. A band whose next date is a month off doesn't lapse
  while you wait for it.
- **The spokesperson is never the holdout.** This fixes the wording issue noted in step 4 ("June isn't sure yet", said
  by June). `SpeakerFor` passes over the member holding out, who now speaks only for themselves.
- **The voices and the card:**
  - The Street's deep read (5) names what the holdout will say before you ask: "Sonny is the one who isn't sure.
    They've a job worth keeping."
  - The card's primary button stays TALK TO THEM while an objection waits.
  - The notebook reads "answer Sonny" or "they want an answer".
- **Older saves:** a holdout step with no person attached finds its member by name. If that member has left the band,
  the step is done ("Sonny isn't with them any more"). Steps already settled stay settled.

### Checks (seed 1001)
- **`DispositionChecks`: 92 checks in Detroit, 93 in Nashville** (82 before).
  - **Part D:** at the set, the holdout raises the objection read from who they are. The Talk shows them saying it and
    offers answers rather than terms. The true answer is open exactly when its fact holds.
    - In Nashville, John says labels cheat. A new label has no true answer, so the check tells him to take his time; he
      settles it at the band's next set, and terms open.
    - In Detroit, Sonny says the same. The band's next set comes after the yes's 28-day hold, so the yes lapses, which
      is the right outcome.
  - **Part G, new:**
    - The objections come in order, with one member's real fields moved and restored:
      - a writer objects for their songs;
      - a parent objects to the road;
      - an 18-year-old's folks want school;
      - at 30, the day job is the objection when the act works day jobs;
      - at 22 with ambition 0.1, they're in it for the fun, and that has no true answer;
      - at 22 with ambition 0.3, labels cheat.

      The same state gives the same objection each time.
    - A band of four or more with a sideman holding out has nothing to answer until the set.
    - At the set the sideman raises their objection (Detroit: Solomon, Job). Signing without them removes them with
      the alumni reason, and the rest are ready to sign.
  - **Part F:** the hearing wants an answer first (Road: "Who's going on the road?"). The answer whose fact is true
    meets it; for a home-town label, that's the Suit's line.
  - **The world snapshot is now a streamed hash** (`WorldHash`). Comparing equal hashes checks the same thing. The
    serialized string ran out of memory on a box short of commit memory.
- **Legs not reached on this seed:** Detroit's hearing leg (the act gave no phone number, so there was no second
  session). It was reached on the first-cut runs, where it answered Road with "You'd record here, in Detroit/Nashville".
  Nashville's Part G found no band of four, out of 129 candidates, with a next set posted inside the hold.
- After the rework: `TheTalkChecks` 126, `--scouting-runway-check` 212.
- **Pass:** `TheTalkChecks` 126, `FirstWeekUiChecks` 18, `ConnieChecks` 49, `SignedClockChecks` 39, `LeadsChecks`,
  `ScoutingUiChecks` 72, `--scouting-appointment-check` 61.
- **`TheSetChecks` 91 and `--scouting-runway-check` 212 (thin=0)**, one check fewer each than before. With
  `--disable-national-residency-surrender` they give 92 and 213 again, so the missing check comes from the rooms
  changing under the new default, not from step 9. Each is a conditional leg that the new rooms don't reach.

### Own screen: what the one who isn't sure says (SigningAvailabilityProbe, seed 1001, 56 days, Detroit label)
Every unsigned act heard, read with the game's own functions (`HoldoutObjection`, `HearingObjection`), for a new
Detroit label with no record out. "First night" assumes one night heard.

- **Holdouts:** 347 acts, 27% of the unsigned acts heard (hungry or careful acts that have one).
  - The true answer is open on the first night for 41% (82% in the first cut).
  - The band could sign without them for 63%.
  - For 26%, neither is open, and "take your time" is the only way.

  | Objection | Share (first cut) | True answer open the first night | Could leave them behind |
  |---|---|---|---|
  | Job | 39% (80%) | 100% | 78% |
  | Songs | 20% (20%) | 12% (you need to have heard half their own songs) | 0% (a writer can't be left behind) |
  | School | 17% | 0% (the family's Sunday comes first) | 76% |
  | Fun | 13% | never | 89% |
  | Cheated | 12% | 0% (a new label has paid nobody) | 74% |
  | Road, Money | 0% | | |

- **Comfortable acts hearing you out:** Road 77% (open to a new label 5%, only in your own town), Engagement 23% (0%,
  you'd need a record out).

**Reading.** No single objection dominates any more, and the holdout is a real decision for about three in five. The
true answer is open the first night for 41%: mostly older members with a job to keep, plus writers whose songs you've
heard. For most of the rest, the choice is a second night or losing a member. School becomes answerable at the
family's Sunday dinner, and "labels cheat" once you have a paid act to point to, so both answers open as the game
goes on. Road and Money still never come up for an opening holdout, because the low-ambition member is young,
unmarried and not well off. For a comfortable act, the objection is something a new label can't answer yet, which is
the point: the hearing waits for your first record or a home-town act.

### Not done in step 9
- **Bluffs.** The Rolodex lets you give an answer whose fact isn't true and risk being called. Here an untrue answer is
  greyed instead, because a bluff's outcome would need a draw or a rule for who sees through it. It's worth its own
  design if the author wants it.
- A careful act raises no objection, because its request (come back, put some work in) is already the beat.
- The member who leaves doesn't go anywhere in particular. As with any departure, the band-life service owns what
  happens to them next.

