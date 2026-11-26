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
| 5 | **The Talk** (audit §3.3) built on 2–4: opening line by disposition, wants as script lines | No | Audit §4 row 3 |
| 6 | **Signed as a clock** (§3.4): Street contract read, watch item, the call at expiry | No (reads expiry decisions) | An act let go after regard calls the player |
| 7 | **Leads** (§3.6) from scene sources | No | Leads name real draw/residency facts; low Street misreads |
| 8 | **World booking (a)**, then **(b)** only if (a) measures short | **Yes** | Own screen: bills composition + AI CSV A/B |
| 9 | Later: the holdout member, courtship objections (audit §3.6) | Player-only outcomes | Own screen |

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
