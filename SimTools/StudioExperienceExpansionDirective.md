# Studio Experience Expansion Directive — One More Take

**Status: design sketch for future implementation. This task produces a directive only.**

Prepared October 11, 2026. Based on the supplied studio critique and the current working checkout at HEAD `51adfc7`. Prioritize the player experience in **1960–1964**; support later expansion without requiring a late-decade production simulator to make the opening enjoyable. No gameplay code, economic defaults, or saves are changed here.

## 1. The experience

You promised this band a record. The room is paid for until six. The singer finally means the last verse, but the drummer rushes the ending. The engineer thinks you can get a cleaner one. The band thinks that was it. You still need a flip side.

The studio should turn a signed act and a chosen song into a particular recorded performance that the player remembers helping make. Its central decision is **what to commit while there is still a chance to try something else**.

In the opening years, richness comes from people, arrangements, room sound, limited time, and imperfect judgment. A modest mono date must support consequential choices from the first session. Technology later increases what can be changed independently; it must not invalidate the early experience or make expensive rooms universally superior.

The first playable milestone is a complete, affordable A-side/B-side session: book, prepare, hear an attempt, give a direction, choose a take, finish, and return to the release desk. Successful judgment can mean keeping a rough performance, making a careful correction, or recognizing that the B-side is now the stronger record.

## 2. Current seams and corrections to the supplied sketch

This is a targeted source inspection, not a full implementation audit or playtest. Reconcile the selected implementation baseline, active flags, and parallel scouting work before coding.

| Inspected seam | What exists now | Expansion requirement |
|---|---|---|
| `Systems/PlayerDesk.cs`: `StartSession`, `RollTake`, `BestTakeIndex` | Hours are divided into 1–4 takes per song; takes use Hook/Production; a take is automatically selected using visible star totals and raw quality as a tie-breaker. | Replace the batch roll with decisions that consume session time. Manual selection must not receive a hidden-quality recommendation. |
| `UI/PlayerDeskPanel.cs`: `TakesView` | Selection and summaries read the underlying take values. | Replace truth ratings throughout the screen, including summaries and defaults, with attributed observations. |
| `StudioQualityIn` / `StudioQualityT` | City quality already exists, with a region fallback; the current booking uses the label's home city. | Bind capabilities and rates to the booked room. Do not describe the older region-only bug as still wholly present. |
| `PrintMaster`, `PolarSongBehavior`, `SongMaterialApplicationService` | Material resolution and enabled polar realization already affect completed masters. `RollTake` still lacks the proposed detailed material/member interaction. | Move relevant execution context into the session without applying Capability or composition blending twice. Preserve the calibrated completed-recording contract. |
| `ConsumeSessionPlayer`, `RoomTension`, `SessionEmploymentService`, `SceneHouseBandService` | Session hiring, tension, musician employment and live house-band infrastructure exist. | Reuse people, availability, payment and contact owners. A live house band is not automatically a studio's permanent crew. |
| `SaveGameService` pending-session serialization | Saves include takes and clamp the kept index to an existing take. | Introduce explicit unselected state and migrate legacy sessions without losing their already-paid takes. |
| `SimTools/ScoutingSessionPromiseHandoff.md` | Describes a funded-session commitment in a separate working snapshot. | Reconcile that integration. A future reservation must not silently fulfill a promise that previously required an actual paid session. |

Further corrections are binding design choices:

- Track count describes recording layout, not a universal quality ceiling or a complete equipment specification.
- Creative discovery can happen during rehearsal or the first pass. Boredom is one cause, not the entry ticket.
- A released record never exposes its objective artistic truth. Market response is evidence affected by promotion, distribution, audience and timing.
- More skilled producers can make experimentation worthwhile on average. The requirement is a meaningful opportunity cost and credible failure, not a mathematically non-positive expected return.
- Do not require a 20× cost spread, a prescribed take-index histogram, or a fixed drift frequency to declare the design successful. Those are hypotheses to examine, not outcomes to manufacture.
- Retain previous takes by default within the quoted stock allowance. Deliberate tape reuse is a visible economy choice, not a surprise deletion trap.

## 3. The first three sessions

These are onboarding opportunities, not scripted results or mandatory stages.

**First date: can we get a record?** A local room, the act's own lineup or necessary backing, and two prepared songs. Explain only the booked time, protected takes, and the difference between another attempt and moving on. One intelligible flaw gives the player something to address. A usable first take can be enough; no compulsory failure teaches the system.

**Return date: did we learn anything?** The engineer remembers the singer's microphone distance. The player knows the drummer tightens up after a rehearsal pass. Offer a specific response: rehearse the ending, lower the key, change the order, or book the same engineer. Familiarity reduces setup uncertainty or improves communication within bounds; it is not a permanent quality multiplier for repeat bookings.

**First deliberate stretch: what kind of record are we making?** A backing vocalist, a different room, a piano-led arrangement, a dedicated rehearsal, or a vocal overdub where the actual setup supports it. One new capability is enough. The player should understand what the purchase permits and what it costs before committing.

A player may delegate all three dates. Detailed intervention should create authorship and informed tradeoffs, not a mandatory score bonus over competent delegation.

## 4. Booking a room and planning the date

Replace the tier ladder on the player surface with persistent named rooms. Start with a small, credible local selection wherever the label can begin. Sparse towns may require a modest reachable room or travel; do not spawn a prestigious specialist to satisfy a UI quota.

A room card answers:

- Where is it, when is it available, and can this act and its people get there?
- What does it sound like, according to what evidence?
- Who operates it, and who is actually available for this booking?
- What recording layouts, instruments and effects can this date support?
- What is included in the price, and what would an extension cost?

Use named capabilities and descriptive evidence: dry and intimate, lively but difficult to separate, reliable piano, a good vocal setup, unfamiliar engineer, or an owner who insists on their regular players. Renown is reputation and demand, not another quality stat.

Useful opening choices include a cheap straightforward room, a more controlled room with a dependable engineer, and a distinctive specialist reachable through money, travel or contacts. These are design roles, not three tiers every city must contain. Cheap rooms can make exceptional records; an unsuitable expensive room can waste money.

The booking plan names the songs, provisional A/B priorities, order, lineup, recording layout, and spending limit. Carry in repertoire familiarity, scouting observations, member circumstances and any contractual control. Warn about a genuinely tight plan using estimated time and a reason, not a hidden optimal schedule.

Offer three simple briefs: **capture the live act**, **tighten it for a single**, and **leave room to explore**. They guide recommendations and delegation. None applies a free quality modifier or forbids a later change of mind.

A first-date quote includes setup, reasonable tape stock, engineering, musicians where needed, and finishing time. Show room charges separately from personnel, travel and optional work. The player must be able to reach the pressing decision after a sensible opening session; calibrate against total founding cash and release costs, not studio costs in isolation.

## 5. Inside the room: small actions with audible consequences

The main surface has four elements: the current song and intention, an understandable clock, a short take log, and a few relevant next actions. Do not simulate every microphone or make the player advance individual minutes.

Actions consume actual time: setup, rehearsal, a take including reset, playback, a break, an arrangement change, an overdub, and finishing. Duration depends on song length, personnel familiarity and the work attempted. A false start still costs time, but usually less than a full pass. Moving the vocalist takes minutes; rewriting the middle section takes longer.

The clock advances the ordinary world once. It must not charge the booking's hours upfront and then spend the same player hours again through actions. Room rental, musician minimums and actual elapsed player time can differ, and the UI should explain the difference.

Early actions worth supporting:

| Action | Why the player might choose it | Tradeoff |
|---|---|---|
| Rehearse the trouble spot | The bridge repeatedly falls apart. | Uses time and some energy without capturing a master. |
| Give one direction | Hold back the drums, lean into the lyric, shorten the ending. | Helps a particular problem; another quality may suffer. |
| Change key or tempo | The chorus strains the singer, or the groove feels wrong. | Changes demands and character; requires adjustment. |
| Change room balance | The guitar masks the vocal on a shared track. | Resets setup and may weaken a pleasing blend. |
| Simplify or substitute a part | The arrangement exceeds today's players. | Improves feasibility while changing the arrangement and possibly provoking disagreement. |
| Take a break | Concentration is slipping. | Uses booked time; offers partial recovery rather than resetting exhaustion. |
| Keep it and move on | This performance serves the song. | Accepts known imperfections and preserves time for another side. |

No need for a branching dialogue tree at every take. The chosen action, the resulting performance, and a brief specific reaction should carry the story.

## 6. Takes, fatigue and fair uncertainty

Internally distinguish vocal delivery, ensemble coordination, feel and technical capture. Derive final record properties through one shared realization path. Do not simply average these into another visible overall score.

Performance depends on the chosen material's demands, actual participating members and roles, familiarity, direction, room and engineer, cumulative workload, and bounded variation. A hired drummer can improve timing without replacing the singer's identity. A supporting musician can still change the arrangement's character. Songwriting credit remains with the actual writing contributors; arrangement novelty is recorded separately.

Learning and fatigue compete. Practice can improve precision; repeated effort can flatten delivery or strain the voice. Apply workload across songs and actions, not just within one take counter. Reordering tracks matters. Do not impose a universal formula in which professionals always peak on take eight or every singer has the same cliff. Skill, demands, prior work and recovery should determine useful ranges.

Reads should be concrete and attributable:

> **Take 3 — complete, protected**  
> Engineer: “Clean enough. The guitar is crowding the last chorus.”  
> Singer: “That's the first time the last verse felt right.”  
> Your note: The ending still seems hurried.

Playback can reveal a masked flaw or strengthen a comparison; repeated identical playback cannot reroll the read until it becomes favorable. Expertise changes what people notice and how well they explain it. Their preferences can differ without everyone being arbitrarily unreliable.

Manual mode begins with no selected take. “Keep this” protects and selects an available performance. Printing requires a valid selection for each included side, or an explicit request to let the producer choose. Incomplete takes are visibly incomplete; usable fragments only matter where a supported editing operation can use them.

An extra attempt never destroys a protected take. If stock runs short, quote more stock or identify the unprotected tape being overwritten. Confirm actual destruction at the point of reuse. Keeping an earlier take after an unsuccessful experiment is legitimate; spent time, wages and fatigue supply the cost.

Incidents should usually be minor and contextual: a late entrance, an amplifier buzz, a broken string, a compelling pickup, or an unexpectedly good harmony. Do not turn early recording into a random catastrophe deck. Players need ways to respond to recurring problems.

## 7. Early creative development

The 1960–1964 palette should work even with everyone recorded together:

- Half-time or a quicker feel; a different key; a shorter intro or ending.
- Backing responses, a unison hook, handclaps or a changed instrumental lead.
- A simpler rhythm figure, a piano substitution, or an available horn part.
- Different room balance, slap or chamber treatment where supported, and deliberate use of an incidental sound.

Ideas arise from actual participants, preparation, failed attempts, spare time, equipment or a producer's suggestion. Let the player deliberately request an alternative as well. Repetition may increase restlessness; do not reward grinding takes as the only way to discover ideas.

Each proposal states what changes, who favors or resists it, estimated time and money, and which existing take remains safe. Distinguish trying an arrangement from committing the new version. Respect contractual creative control and refusal; paying for a date does not override it.

> **“Try it without the busy guitar part.”**  
> The pianist has found a slower answering figure. The singer wants one pass; the guitarist thinks it drains the song.  
> About twenty minutes to work it out and try it. Fifty minutes remain, and the flip side is uncut. Take 3 is safe.  
> **Try it / Return to the brief / Keep take 3 and cut the flip**

This can yield a stronger ballad, an unconvincing compromise, or useful knowledge for another date. Alter tempo, arrangement, demands and resulting musical properties coherently. Never award originality merely because the player clicked “experiment.” A transformation applies to this performance/arrangement, not retroactively to every recording of its source composition.

## 8. People, finishing and the ride home

The default early crew is the player directing, the booked engineer, and the actual performing lineup. Separate producer and engineer responsibilities internally; one person can hold both. A modest date must not require hiring three specialist roles.

Production direction affects arrangement, confidence, coordination and use of time. Engineering affects capture, fault diagnosis and feasible technical operations. An arranger becomes useful for work that genuinely needs arranged parts, not an automatic toll on every appearance of a horn. Use existing person/contact infrastructure before inventing isolated staff rosters.

Hiring someone adds their availability, fees, preferences and authority. Delegating performance direction does not authorize unlimited spending or the override of an artist's rights. Repeated collaboration can develop trust and practical familiarity. Replacement may upset a member when the circumstances warrant it; hiring assistance is not universally humiliating.

Finishing is a short, purposeful step. With a committed mono performance, offer only processing the source actually permits. With separately captured vocals, a vocal-forward balance may be possible. Do not offer “turn down the drums” when the drums are inseparable from the band.

Playback is included at a basic level. Further work costs time. A simple house finish is always available for a valid take; do not impose a surprise finishing bill after the player exhausts the quote.

The completion card gives the named master, kept performance, actual credits and arrangement, audible compromises, time and itemized cost, contractually recoupable amount, and any changed relationship or unfinished side. Keep a compact session history attached to the master. Let the player reconsider A/B priority at the release desk.

Charge expenses and recoupment once through their established owners. Do not expand every studio expense into recoupable artist debt irrespective of contract terms. Failed sessions retain their actual costs; allocating costs across masters must not erase abandoned work or charge it again when a master is reused.

## 9. Technology and the rest of the decade

Store capabilities on dated room/equipment records. Separate recorded channels, routing and isolation, monitoring/overdub support, additional machines, effects, editing skill, instruments and staff access. Track count alone does not guarantee that two performances can be combined: layout, bleed, timing, source retention and editing method matter.

| Available setup | Additional player decision |
|---|---|
| Whole performance committed together | Balance and arrangement have to work when played. Choose the complete performance. |
| Separately captured vocal and backing, with suitable overdub facilities | Preserve the backing and retry the vocal, paying time and vocal workload. |
| More independent tracks | Reserve room for additions, decide what shares a track, and choose which elements to commit. |
| Supported reduction/bounce workflow | Free tracks by committing a mix; accept cumulative degradation and lost separation. Retained source reels remain identifiable. |
| Larger later setups | Build in sections and edit more selectively, at increased organizational and financial cost. |

These are capabilities, not global year unlocks. Recording formats and mono/stereo release mixes are separate decisions. Later adoption belongs to researched room schedules and market conditions, not a universal annual switch.

Defer elaborate bouncing, tape construction, large staff markets, studio ownership and rental income until the early loop works. Reserve IDs and capability history for them. Renown can eventually affect bookings and rates with lag and capacity limits; one hit should not instantly price the player out of their familiar room.

Historical anchors support this approach: Abbey Road describes adopting the four-track J37 in 1964 and expanding its use for bouncing, and acquiring an eight-track machine in 1968. Those are examples of one institution's changes, not a worldwide adoption table. The Stax Museum's account of its reconstructed sloping theater floor supports treating room character as a specific property. Sources: [Abbey Road: J37](https://www.abbeyroad.com/news/studer-j37-gearthatmadeus-3195), [Abbey Road: White Album](https://www.abbeyroad.com/news/the-beatles-white-album-2474), [Stax Museum collection](https://staxmuseum.org/museum/collection/).

The supplied named-studio roster is a research shortlist. Verify opening dates, equipment, personnel movements, ownership, outsider access and techniques before seeding historical entries. Do not encode “Wall of Sound only at one address,” automatic per-instrument comping at eight tracks, universal union side caps/overtime, or a blanket non-union licensing ban from the sketch. Labor terms require period, jurisdiction and agreement-specific research; the existing employment scale explicitly identifies itself as provisional.

## 10. Delegation and pacing

**Run the date** uses the same action model, costs, uncertainty and staff judgment as manual play. It follows the brief and a hard spending limit. The player can choose to stop at a promising take, a substantial arrangement proposal, or a threatened side. Routine false starts and minor adjustments collapse into a readable log.

Delegation chooses from the producer's evidence and preferences, not hidden realized quality. It may reasonably pick a different performance from the player. Fast resolution must not create extra takes, evade fatigue, bypass refusals or supply free musicians.

Show only decisions that remain relevant. A B-side can be completed through a short delegated sequence. A player can intervene for a song they care about and then hand the date back. Measure interaction burden in playtests; do not require a modal for every rehearsal, payment or take.

## 11. Implementation contract and delivery order

Keep truth, observations and decisions separate. Persist booking/session IDs, room and capability snapshot, participating person IDs, arrangement revisions, elapsed work, fatigue, take/source IDs, observations, selected takes, costs and completion state. Exact class names are implementation choices.

Use stable private random streams keyed by session, side, action/take and purpose. Freeze an outcome when its action resolves. Inspecting, sorting, printing, delegating or reloading must not reroll it or perturb unrelated world randomness. A new action may create a new outcome; replaying the same saved action may not.

Before replacing realization, map all player, autonomous, album and promoted-master consumers. Define where material demands act and where final composition/recording values are combined. Do not bolt new fatigue and Capability penalties onto an already-penalized final record. Derive danceability and content-related properties from musical/content information with bounded residual variation only where justified.

| Slice | Deliverable | Completion evidence |
|---|---|---|
| 1. Complete early date | A modest named local room, incremental clock, preparation/direction, differentiated takes, observations, safe selection, simple finish and delegation; one affordable two-sided release path. | Human playthroughs with a prepared band, a rough act and a singer needing backing. Players can explain their choices without true ratings. |
| 2. Local differentiation | Persistent engineers, real personnel availability, contact-based access, alternate rooms, return visits and correct travel. | Same song/act can justify different rooms; scheduling and money reconcile; weak local scenes remain playable. |
| 3. Early creative latitude | Small arrangement proposals, targeted interventions, protected alternatives and consequences of control/disagreement. | Success, disappointment, rejection and sensible abstention all occur; proposals change actual recordings. |
| 4. Selective recording | Authored early multitrack availability, explicit capture layouts and supported vocal replacement/finishing. | Mono remains viable; a layout enables only operations its recorded sources support. |
| 5. Decade expansion | Broader technical workflows, evolving access and renown, then ownership. | Longer-run compatibility and economic validation after the early player loop is accepted. |

Use explicit off/observe/enabled rollout states where simulation behavior changes. Observation proposals must not mutate artists, consume gameplay RNG, reserve personnel or spend money. Avoid enabling population-wide economic changes just to expose a richer player interface; separately validate shared autonomous paths before changing their defaults.

Migrate old pending sessions as already-paid legacy sessions with their takes and selected index intact. Do not retroactively reroll them into the new model. New saves must preserve unselected and partly completed sessions. Closing/printing must be idempotent, and abandoning a date must release remaining reservations without refunding already-incurred costs.

## 12. What to validate

Focused checks should establish deterministic continuation across save/load; private-stream isolation; selection and source validity; no truth leaks through defaults or summaries; cross-song fatigue and partial recovery; personnel conflicts; control/refusal handling; affordable and bounded delegation; and exactly-once time, cash, wages, recoupment and master creation.

Compare cases that should differ: more rehearsal for an unfamiliar bridge, a fresh versus exhausted singer, the same room with a different capture layout, a usable protected take before a failed experiment, and an engineer correctly identifying a fixable problem. Check that a UI refresh changes none of them.

Playtest the opening through pressing and release preparation, not just the studio screen. Record why players chose to stop, retry, change direction or delegate; time to complete a single; unexpected costs; unfinished sides; and whether the saved master retains the story they remember.

Track take choices conditional on act, material, workload and brief; spending by room/personnel/work type; retained alternatives; experimentation costs and outcomes; and delegation frequency. Look for dominant strategies such as always booking the longest date, always choosing the last take, always experimenting, or always hiring replacements. Investigate the mechanism rather than forcing distributions to target percentages.

The opening succeeds when a player can say: **“We had a cleaner take, but this was the record.”** It also succeeds when they choose the cleaner take for a reason grounded in what they heard.
