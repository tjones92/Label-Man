# Scouting Experience Expansion Directive — The First Name on the Label

**Status: design directive for future implementation. This task authorizes this document only.**

Prepared October 9, 2026. Builds on *Local Scene Ecosystem Directive — Cities With a Memory* and the accompanying scouting sketch. Those documents are design inputs, not instructions to execute changes in this task. No game code, defaults, economy, or saves are changed by this directive.

## 1. The experience

You have enough money to hear one more bill, book one modest session, or improve an offer. The band you liked is playing across town tonight. Their booker says they always fill this room; the musician who introduced you says the singer wants out. A rival has asked for a meeting. You can learn more, but you cannot do everything before Friday.

The player should feel present in the rooms where a record business begins: listening from the back, recognizing the people on the bill, catching a conversation after the set, asking for an introduction, and trying to persuade an act that this tiny label can deliver something worthwhile. Desperation comes from scarce resources, incomplete evidence, competing commitments, and ambitions larger than the label's means.

The first signature should feel earned through judgment and a credible proposal. It must not require discovering a hidden superstar, completing a relationship checklist, or enduring a prescribed number of failures. A dependable local seller, an unusual songwriter, a regional favorite, and an ambitious rough act can each be a sensible foundation for a different label.

Success means:

1. Players remember acts, rooms, intermediaries, and particular nights rather than numerical rankings.
2. Choosing where to go changes the evidence, people, and opportunities available.
3. Repeated listening answers specific questions without revealing a true talent score or development ceiling.
4. Acts have intelligible reasons to negotiate, decline, wait, accept another offer, or remain independent.
5. Competitors operate in the same world and face the same access constraints; pressure has a cause.
6. A small label has credible advantages as well as limitations. Knowing the people helps; showing up does not buy consent.
7. The experience remains playable when the scene is quiet, money is tight, or the first choice signs elsewhere.

## 2. Integration baseline and limits of this audit

This chat's checkout is `C:/Project/Label-Man`, branch `band-member-simulation`, reported HEAD prefix `01ee660`, with existing uncommitted band work. Scene work was inspected read-only in `C:/Users/grohl/.codex/worktrees/local-scene-phase-4/Label-Man` and `C:/Users/grohl/.codex/worktrees/local-scene-phase-5-feedback/Label-Man`. The worktree listing reported `55ba2cc` for both at inspection. File observations include working contents and are not a claim that every inspected line is committed, merged, enabled, or validated.

The scene implementation is moving. In particular, the older live-feedback handoff describes attention as opt-in and institutions/relocation as remaining work, while inspected working files contain institution/relocation entry points and default attention feedback under its dependencies. **Reconcile code, flags, handoffs, and accepted validation at implementation start. Do not use a handoff's completion language as a substitute for checking the chosen baseline.** No simulation tests were run for this document.

| Inspected seam | Current evidence | Directive consequence |
|---|---|---|
| `PlayerDesk.SceneRooms.cs`, `LocalSceneRooms.cs`, `LocalSceneRoomService.cs` | Named rooms, dated bills, appearance/person IDs, sets, engagements, admission, attendance, response, work and fees exist. `ScoutSceneRoom` hears a scheduled cast and advances time. | Extend an existing performance encounter. Do not build another venue roll, calendar, fee ledger, or live-work system. |
| `PlayerDesk.LocalScenes.cs`, `LocalScenePersistence.cs` | Persistent discoveries retain room/bill/contact references; legacy discoveries are adopted; record-deal eligibility is separate from presence. | Expand knowledge and preserve IDs. Seeing an act never implies availability to sign. |
| `ScoutSceneRoom` | Still constructs `Prospect` with scalar `ReadQuality`, confidence, `HeardCount`, set reads, and a descriptive note. | The scene foundation exists; the player's interpretation remains a principal replacement seam. |
| `LocalSceneRoomService` | Expected audience depends on room capacity, bill size and prior appearances; resolved attendance is bill-level. | Attendance is real scene state, but not yet an attributable fan count or demonstrated cross-town draw. |
| `SceneRecruitmentRecord`, `SceneAttentionFeedback` | Committed signings have geographic/access provenance; lagged genre/place attention weights existing accessible AI discovery candidates. | Reuse competition and attention ownership. A signing ledger is not a complete history of inquiries, rejections, or pending offers. |
| `UI/PlayerDeskPanel.SceneRooms.cs` | Named handbills, schedules, contacts and admission already have a player surface. | Evolve this board into the entry to a night out; preserve the useful calendar. |
| `PlayerDesk.ContractNegotiation.cs` | `NegotiationWeights` already uses manager, ambition and pragmatism across advance, royalty, term, deliverables, publishing and control. `PostureOf` recalculates posture. Easy acts have offer floors and counters. | Extend the existing negotiation owner; neither manager preferences nor minimum acceptance rules are missing wholesale. |
| `UI/PlayerDeskPanel.cs` in the scene checkout | Negotiation text still exposes package percentage, reservation requirement and advance floor. | Remove hidden decision thresholds from ordinary UI while retaining actual asks, counters and contractual terms. |
| `ScoutingPerception.cs` | Stable scalar perception is keyed by label, act and discovery window. The inspected room path supplies window zero. | Do not assume repeat room visits currently reroll the window. Replace the player read deliberately; preserve the AI baseline unless separately authorized. |

Paths in this table are repository-relative. At implementation, record exact source hashes and inspect all player scouting, notebook, follow-up, approach, direct-offer, renewal, save/restore and debug-display callers.

## 3. Binding corrections to the sketch

| Sketch proposal | Decision |
|---|---|
| Change the `Pushover` initializer to `Firm`; trivial fix | Audit `PostureOf`, approach and every acceptance path. Interaction length and willingness to accept are different. Easy negotiations may remain, with meaningful terms. |
| Add preference vectors because all acts want the same package | Extend existing weights with credible capabilities, relationships and outside options. Avoid a parallel contract evaluator. |
| Create region-level `LocalFollowing` and seed recognition on signing | Reuse scene geography and performance records. A bill's attendance is not an act's portable following. Signing itself creates no fans. |
| Every observation raises exposure; rival interest rises every week | Different actions have different visibility. Discovery must reach a rival through a feasible channel, within attention and budget limits. Interest can fade or end. |
| Rejected acts are mostly bad and cheap | Rejections have reasons: fit, budget, timing, rights, capacity, or an assessment. Neither quality nor price follows automatically. |
| Clubs hide ceilings; honky tonks have poor reliability | Rooms differ by programming, audiences, access, cost, acoustics and social networks. They do not assign innate potential or moral character. |
| A demo gives near-truth | A demo tests recording performance under particular material, personnel and production conditions. It cannot reveal destiny. |
| Three nights expose consistency directly through cohesion | Variation has several causes. Preserve lineup and context, avoid treating one rough night as proof of low cohesion, and never display the hidden causal field. |
| Persistent bias must make weak scouts confidently wrong | Structured blind spots are useful, but independent evidence and expertise must allow correction. A permanent uncorrectable per-act error is not a satisfying skill test. |
| Almost everyone refuses; first signing inside a month is failure | Reject calendar gates and refusal quotas. Measure viable decisions, affordability and understanding. A fast, well-judged signing is legitimate. |
| Potential diamonds replace a star bar | A ceiling rating is still a star bar. Show evidence, open questions and development hypotheses. |
| Early acts automatically surrender publishing until the decade teaches them | Use individual knowledge, advice, experience and negotiated rights. Do not impose a universal year-based ignorance curve or an unsourced historical claim. |

## 4. The playable loop: a place, a night, a next move

### Before leaving the office

The city board combines announced bills, appointments, existing leads, known costs and feasible travel. A lead states who supplied it, when, what they actually claimed, and any known deadline. “A good band is somewhere in town” is atmosphere; “the booker says their closing set on Thursday includes two new songs” is actionable.

Show the player's available hours and cash alongside the itinerary. A distant date includes travel and lodging where applicable. Waiting until a bill starts must be an explicit, convenient action that advances the ordinary world. Never require players to click through empty hours, discover essential scheduling rules by failed actions, or repeatedly arrive just after the last reachable set.

### At the room

Present the room and the bill as the main scene. Establish where the player is, who is on now, who is expected later, what has actually been heard, and how much time remains. Short descriptions can make a coffeehouse, community hall, roadhouse and trade meeting distinct without creating a new minigame for each.

Give the player a few consequential choices at a time:

- Listen to this set or stay for a later appearance.
- Ask the booker a specific question, speak to another musician, or seek an introduction.
- Talk to the act when they are available, arrange a later meeting, or leave for another commitment.

Choices compete in time and attention. Do not automatically forbid listening while exchanging a brief greeting; do not let a substantial meeting count as careful observation of the entire simultaneous set. Charge admission once per actual visit according to venue policy, not once per card. Hearing a multi-act bill creates distinct observations without multiplying the visit cost.

The cast includes signed and uninterested performers. They can provide comparisons, introductions and scene continuity. Do not fill every bill with purchasable prospects or replace unavailable acts because the player needs one.

### After the night

Offer a concise notebook update: what changed, what remains uncertain, and the next reachable opportunity. Players can dismiss, watch, pursue or archive a lead. Archiving changes the attention list, not the act's life or remembered relationship. Automatic notes preserve important evidence; manual pinning is for priorities.

One useful encounter can justify an approach. Further visits should answer a question the player cares about. The loop must never require every activity for every candidate.

## 5. Evidence, interpretation and relationships

Keep three layers distinct:

1. **World event:** a performance, lineup change, appointment, introduction, offer or cancellation, owned by its authoritative simulation system.
2. **Acquired evidence:** what this label saw, heard or was told, with source, time and coverage.
3. **Interpretation:** the label's assessment, with uncertainty, assumptions and unresolved contradictions.

A dossier belongs to the label and act, outside transient `Prospect` cards. It survives slate replacement, notebook removal and signing. It references acts, people and sources by stable IDs rather than holding another artist object graph.

Minimum conceptual observation contract:

| Field group | Purpose |
|---|---|
| Observation ID, label ID, act ID | Stable ownership and deduplication. |
| Event/bill/appearance ID, source/contact ID, channel | Provenance; copied gossip is not independent corroboration. |
| Occurred time, acquired time, place, coverage | Separate an old report received today from a performance heard today. |
| Lineup/person IDs, repertoire references, relevant conditions | Explain changing assessments and stale evidence. |
| Kind, bounded payload, source claim versus direct observation | Preserve what was acquired without silently importing hidden truth. |
| Credibility, interpretation version, superseded/disputed links | Support revision without rewriting history. |

Familiarity, trust, access and business credibility are related but distinct. Familiarity means recognition; trust follows kept commitments and interactions; access concerns introductions and availability; credibility concerns what the label can deliver. Use existing relationship owners where available. A dossier can project these, but must not independently own conflicting relationship balances.

An introduction may open a conversation immediately. Four silent visits need not build trust. Respectful refusal can preserve a relationship; repeated unwanted approaches can damage it. Relationship effects must be bounded, contextual and resistant to repeating the cheapest action.

## 6. Activities answer different questions

All durations and charges below are categories to tune against the actual action clock, not replacements for current constants. Use scheduled availability and explicit cost previews.

| Activity | Evidence or opportunity | Limit and tradeoff |
|---|---|---|
| Hear a scheduled set | Repertoire, audible execution, stage interaction, audience response during that set | A sample in a particular room; attendance is not artist-only draw. |
| Stay for a later set | Different material, fatigue or recovery, crowd retention where modeled | Requires a genuine later appearance; cannot invent extra paid work or force every venue to have one. |
| Hear another date | Repeated performance evidence with preserved context | A second event, not reopening the same bill. Differences can reflect lineup, material or conditions. |
| Ask the booker | Booking history, observed punctuality, room economics, a referral | Knowledge is local; possible incentives and limited coverage should be evident. |
| Speak with musicians or the act | Stated intentions, working relationships, introduction or appointment | Statements are claims, not access to personality or health truth. Silence is not proof of conflict. |
| Follow an existing out-of-town booking | Another audience and setting | Travel, an actual booking, and feasible presence; no free destination gig generated on request. |
| Arrange an audition or rehearsal visit | Requested material, arrangement choices, response to preparation | Requires consent, people, room and appointment; narrow controlled sample. |
| Record a demo | Evidence about recording, preparation and collaboration | Reserve existing studio/personnel capacity and pay through existing accounting; no automatic release or near-perfect read. |
| Seek an independent opinion | Another assessment and possible explanation of disagreement | Different expertise helps; shared source material does not become a second independent performance. |
| Ask for a business meeting | Stated needs, actual package discussion, next steps | Negotiation can begin with unresolved musical questions. |

“Work the room” can summarize lightweight contact discovery, but meaningful questions should be specific: “Do they arrive prepared?”, “Who brings the audience?”, “Are they looking for a record?”, “Who can introduce us?” Conversations reveal an existing fact or belief, express a modeled motive, or execute an action. Drinks are optional local color, not a universal access fee.

## 7. Hearing an act without revealing its future

Replace the primary scalar quality presentation with a small set of decision-relevant assessments: vocal performance where applicable, ensemble/execution, material and writing, stage communication, and recording readiness when observed. Use canonical musical identity and member capabilities; do not create duplicate underlying talent axes to suit the screen.

An assessment states its basis: “Held the harmony together on both dates,” “Only heard familiar covers,” “No recording evidence,” or “The new lineup has not played this arrangement together.” Instrumental acts must not be penalized for lacking vocal evidence.

Consistency requires multiple independent samples and a comparison of contexts. Two reports from the same night are not two nights. Repetition improves the estimate of present performance; it does not converge on hidden ceilings. Lineup changes mark affected judgments as stale while retaining useful history about continuing members.

Separate world performance variation from observer error. A shared performance snapshot should depend on its actual appearance, lineup, repertoire and conditions. All observers encounter that same event; observers can interpret it differently. Do not introduce a second night-quality roll in the dossier or reuse `CalculateBaseQuality()` as the entire live performance model.

Where current scenes lack the required performance detail, implement the smallest shared event snapshot through the performance owner. Its creation must not depend on whether the player attends. Do not change development, fatigue, injury or fee outcomes merely to add descriptive variance.

Observer skill affects discrimination, relevant inference and error. Retain source correlation and some stable blind spots so repeated clicks cannot average away all uncertainty. Allow targeted new evidence, another qualified listener and changed conditions to revise beliefs. “High confidence” describes the assessment's support, not a promise that it is true; interface wording must not suggest statistical guarantees the model does not provide.

Potential appears as a hypothesis tied to a feasible intervention: “The writer's unfinished material may suit a patient development deal.” No potential meter, inferred ceiling stars, or prose generated directly from a hidden ceiling. Keep hidden-truth diagnostics in developer tools only.

## 8. The audience: a signal with a denominator

Use existing `SceneBill.Attendance`, capacity, engagements, room standing and response first. Show “roughly three-quarters full” from observation; a booker's credible count can be more precise. Always make clear whether the number describes the whole bill, the audience during an appearance, or a sourced estimate of people specifically drawn by the act.

The current bill-level calculation cannot support “180 regular fans of this band.” Where attribution matters, extend the scene performance model with bounded evidence such as audience arriving for a set, staying through it, requesting material, or repeated response across bills. Do not manufacture that evidence from generic attendance text.

Assess portability through multiple rooms, towns, comparable bills and sources. Account for capacity, admission, other acts, venue reputation and residency. A crowded small room and a thin large hall do not directly rank acts. A strong home audience can be commercially valuable without traveling.

Keep local draw, short-lived attention, national recognition and sales distinct. If local performance should affect recognition, route a bounded, deduplicated contribution through its existing owner and validate separately. No automatic fanbase deposit when a contract is signed; no double credit for an already-counted show.

## 9. Rivals: real pressure, imperfectly known

Surface actual market events through a limited event projection: inquiry/discovery where meaningful, meeting, offer, withdrawal, rejection and committed signing. Instrument missing events at their owner rather than deriving a whole courtship from the final signing record. A candidate being considered in an internal scoring loop is not automatically a public visit or formal bid.

The player's knowledge is separate. A contact might report that someone requested an introduction; a manager might disclose an offer; a signing may become public later. Name a rival only when the source could know its identity. Preserve occurrence time, receipt time and uncertainty. “No known rival” must never mean “safe indefinitely.”

Make consequential pressure legible when reasonably discoverable: an actual appointment, an expiring offer, or the act saying they intend to decide Friday. Do not invent a deadline because the player has spent three visits. The simulation can surprise the player, but losses should have explainable histories and lead to playable next choices.

Player activity can create discoverable signals. An invitation-only conversation has different reach from an advertised showcase or a widely discussed studio booking. Model who could learn, channel, delay, expiry and event identity. If signals affect AI discovery, respect geographic access, existing slate sizes and budgets; apply bounded delayed effects through the recruitment owner. Never grant every rival immediate interest or a hidden quality revelation.

Avoid double amplification between these act-specific signals and existing scene signing attention. Watching an act is not a signing impulse. Ship read-only rival information before enabling new behavioral feedback.

Passed-over leads record reason and age, when known. “Their label had no roster space” offers different information from “they disliked the material.” Managers can report beliefs or negotiate strategically, but fabricated pressure requires a modeled claim, not a nonexistent offer inserted into the market.

## 10. Negotiation: what can this label actually do?

Preserve concrete asks, offers, counters, terms and player costs. Hide internal reservation percentages, exact utility gaps, acceptance floors and patience counters unless a party explicitly states a real condition or deadline. An act saying “We need at least this advance to buy the equipment” is usable negotiation information; a UI announcing the hidden acceptance calculation is not.

Use objections tied to meaningful terms and motives. The player should know what to change or why to walk away. Better negotiating judgment improves the interpretation of responses without producing “will certainly sign” from a secret threshold. Do not replace arithmetic certainty with uniformly opaque dialogue.

Extend the existing contract evaluator, including straightforward offers and renewals, to account for:

- **Actual outside options:** competing offers when available, independent work, current commitments and the option to wait.
- **Credible capabilities:** distribution the label has, funded recording capacity, achievable promotion, fit and attention it can realistically give.
- **The relationship:** relevant introductions, kept commitments, demonstrated understanding and prior dealings.
- **Individual priorities:** current manager/evolution preferences, expressed ambitions, practical needs, and advice actually received.

An unknown label can win with a modest deliverable plan, a favorable term, creative latitude, a trusted collaborator, local access or genuine attention. Large-label standing helps but does not universally dominate money or fit. Money still matters to people who need it. Familiarity never overrides binding ownership, consent, signing eligibility, cash or roster capacity.

Separate wishes from commitments. A funded demo date can be a concrete offer. “We will make you famous” cannot buy utility as if it were guaranteed promotion. Any promise that affects acceptance needs an owner, cost/capacity check, due date, fulfillment condition, remedy/consequence and save representation. If the game cannot enforce a proposed promise, do not offer it as a scored term in the first release.

Business knowledge should come from people, experience and advice. Managers are not interchangeable, and members need not agree; use existing representation and band-life authority rather than adding an unrelated voting simulation. Rights terms must use the actual publishing and contract systems. Deceptive or exploitative bargains, if supported by existing mechanics, have consequences from those mechanics rather than a scripted year-triggered punishment.

Approaching cold remains possible. Introductions and previous contact can improve access or trust, but there is no mandatory familiarity threshold. A clear refusal should offer its known reason and an appropriate future path where one exists. Do not let reopening a card reset patience, cooldown, asks or pending offers.

Final signing rechecks authoritative artist identity, ownership, activation/cooldown, available money, roster space, rights and offer validity. Resolve simultaneous claims through the existing contract/market owner. Preserve existing intentional short holds and their expiry; a notebook entry or conversation grants no additional reservation.

## 11. The founding act and the opening game

The first deal should leave a durable memory: date, act, label, terms and the relationship's starting context. Record it through career/contract history. If a dedicated founding-act reference is useful, it points to the existing ID and remains historical after a departure; it is not a separate ownership mechanism.

Consequences follow treatment. Keeping a difficult promise can build trust. An unexplained abandonment may affect people who learn of it. A fair mutual departure need not trigger a universal reputation penalty. Avoid permanent loyalty bonuses and automatic anger from every act on the notebook.

Validate an opening runway in each playable scene. Players need reachable, affordable ways to hear music, find an introduction and make a credible proposal. A quiet town can require using its connected circuit; it cannot require an unaffordable trip while hiding all local opportunities. Preserve viable low-cost halls, listening appointments or community routes where authored content supports them.

If the player loses a lead, retain the relationship and history where appropriate, suggest another known bill or contact, and explain the lost opportunity without conjuring replacement talent. The player may also decide that waiting, building capability or making a small record is wiser than signing immediately, where those existing systems permit it.

Track hours, cash runway and opportunities, not just calendar weeks. Do not set a mandatory 60–75% refusal rate, a minimum month to first signature, or a rule that two-thirds of acts must be obviously bad.

## 12. Player presentation and a representative encounter

Use three connected surfaces: the city board for plans, the current room for immediate decisions, and the notebook for comparison and follow-through. Contract discussion should retain the act, intermediary and context, rather than feeling like an unrelated spreadsheet.

The notebook prioritizes a few comparable questions: what has been heard, audience evidence, what the people say they want, concerns and contradictions, recording evidence, known availability, and next opportunity. Detailed provenance is expandable. Do not require reading an essay for every act, and do not encode uncertainty only by color.

Illustrative fictional entry, contingent on real supporting events:

> **THE NIGHT WINDOWS — heard twice at Mercer Hall**  
> Last heard Friday, closing set. The singer and guitarist recognize you.  
> **What you heard:** the harmonies held together on both visits. Friday's new original lost its shape in the middle.  
> **The room:** most seats were occupied for the bill. The booker says people ask for their closing set; you have not checked another room.  
> **What they want:** a chance to record their own material. Their representative asked who would handle distribution.  
> **Still unknown:** how they record; whether the new arrangement is ready.  
> **Another conversation:** your contact says a rival requested an introduction yesterday. No offer is confirmed.  
> **Next choices:** arrange a rehearsal visit; discuss a short deal; hear the announced Saturday bill.

An encounter might end with the player securing a rehearsal appointment but missing another act across town. At rehearsal the writing looks promising; the players still need preparation. The player offers a funded session and a modest contract rather than an impossible promotion promise. The act counters on the term. The player can accept a smaller upside, risk waiting, or walk away with the relationship intact.

This is an example of a supported chain, not a scripted required sequence. Templates must only state evidence the game has. Do not invent audience behavior, songs, rival visits, interpersonal conflict or an artist's private intention to make the prose vivid.

## 13. Ownership, time and persistence

| Owner | Responsibility |
|---|---|
| Artist/population and band-life | People, acts, capabilities, lineups, formation, development, career/market eligibility and terminal history. |
| Scene rooms/performance | Calendars, presence, appearances, shared performance snapshots, audience, work and fees. |
| Existing relationship/contact systems, extended as needed | Introductions, access, familiarity, trust, source knowledge and commitments. Define one owner per relationship type. |
| Player knowledge | Acquired evidence, dossiers, source claims, interpreted reads and known deadlines. |
| Recruitment/contract | Rival decisions, offers, outside-option records, holds, acceptance, commitments and atomic ownership. |
| Recognition/attention/economy | Any approved feedback, attribution, cash transactions and conserved effects. |

Reconcile this division with actual service boundaries before adding classes. The sketch's `Dossier`, `Observation`, `Exposure` and `ContractPreference` names are conceptual, not mandated schemas.

Actions must resolve against the shared clock. Validate a feasible action, snapshot the relevant event/appointment, apply costs and advance time through existing services, then commit observations and consequences in a defined order. Specify what happens if a lineup changes, the room cancels, or another label signs the act during the action. No stale slate may bypass those checks. Refund rules distinguish a blocked action from a journey or appointment already consumed.

Stable keys include save/world seed, namespace/version, event or appearance identity, observer/source, evidence kind and genuine occurrence ordinal as appropriate. `(artist, date, activity, label)` alone is insufficient: two appearances or two sources on one day are different, while revisiting the same report is not new evidence. UI refresh consumes no world RNG. Save/reload reproduces evidence, costs, appointments and offer states.

Persist acquired snapshots needed by dossiers before the bounded scene calendar discards routine bills. Keep compact references and significant evidence, not every unvisited show forever. Archived acts and departed members retain sufficient identity for historical notes. Rebuild indexes after world and player artists have been restored.

Migrate old notebook reads as legacy impressions with honest, possibly unknown provenance; never fabricate visits or independent evidence. Adopt existing scene discovery and artist IDs. Choose the next save version at implementation. Define downgrade/feature-disable behavior without deleting dossiers, breaking contracts or recreating population.

## 14. Delivery sequence

| Phase | Deliverable | Exit condition |
|---|---|---|
| 0 — Reconcile the scene baseline | Exact commit/working-state manifest, flags, owners, current acceptance and save contracts; small end-to-end design fixture | No assumption that current geography, rooms, recruitment or contract preferences need rebuilding. |
| 1 — A playable night and honest notebook | Room itinerary, shared-event observations, persistent dossier, concise next steps; remove hidden negotiation thresholds | One room-to-notebook-to-approach loop works, including signed/uninterested acts and save/load. No live rival-feedback changes. |
| 2 — Purposeful follow-up | Meetings, source questions, repeat listening, auditions/demo appointments where existing owners support them; per-axis interpretation | Independent evidence changes decisions; refresh and repeated cheap actions do not improve reads or trust. |
| 3 — Visible competition | Market event instrumentation and sourced rival information, loss/withdrawal follow-through | Every rival claim has evidence or an explicit modeled belief. Read-only projections preserve AI behavior. |
| 4 — Credible offers | Extend existing acceptance with supported capabilities, relationships and outside options; enforce only supported promises | Small-label routes remain viable; direct, negotiated and renewal paths share required rules and atomic commit checks. |
| 5 — Optional behavioral feedback | Bounded visibility effects and improved audience attribution/recognition where separately justified | Controlled shocks, access/budget conservation and matched economic checks pass before enabling defaults. |
| 6 — Breadth and polish | All-city opening validation, differentiated rooms/contacts, founding history, accessibility and pacing | The experience works outside famous hubs without generic stereotypes or repeated compulsory dialogue. |

Phases are dependency boundaries, not claims about implementation cost. Instrumenting competition and persistent evidence can require meaningful owner/save work. Do not label them trivial because a daily market already exists.

Use repository flag conventions after the baseline audit; the sketch's proposed flags are not existing API contracts. Specify dependencies and observer/off controls. Changing player observations alone must not change an unplayed AI simulation; enabling a new behavioral channel requires separate validation.

## 15. Verification and acceptance

### Non-negotiable invariants

- Observing, refreshing, pinning and archiving never create/delete acts, reroll bills, reserve talent or change musical truth.
- One performance has one shared outcome; people, live work, fees and attendance are not double-counted.
- Knowledge never silently becomes omniscience: source, timing, coverage and uncertainty survive presentation and reload.
- Repeated claims from the same event do not masquerade as independent evidence.
- No true ceiling, scalar quality, reservation value or hidden patience leaks through ordinary UI, tooltips or narrative templates.
- New commitments spend real resources once and survive refresh/save; expired or fulfilled commitments cannot be farmed.
- Rival action respects actual access, budgets and artist availability; no fake offer is inserted to force urgency.
- Eligibility and ownership are rechecked at signing; only the existing owner can activate a latent prospect or grant a hold.
- Legacy saves retain people, contracts, geography and notebook history without fabricated encounters.

### Required scenarios

1. Hear the same bill twice through refresh and reload: unchanged event and no duplicate progress/cost.
2. Hear two appearances on one date: distinct evidence where actually observed, correct overlapping-time handling.
3. See one act on three nights with different material or lineup: interpretable differences, no hidden cohesion diagnosis.
4. A packed multi-act bill: the player cannot infer that all attendees follow the opener.
5. A quiet room with strong musical evidence: the act can still be a rational signing for the right plan.
6. A signed resident or a latent prospect: useful encounter, correct availability, no activation bypass.
7. A cold introduction and a familiar act: both can negotiate; neither relationship guarantees acceptance.
8. An actual rival offer arrives during an audition or trip: time advances once, signing is exclusive, the resulting update has provenance.
9. A stale rival rumor and a passed-over lead: known facts, beliefs and outcomes remain distinct.
10. A promised session is completed, cancelled or missed: costs, remedies and relationship consequences use the same commitment record.
11. Departure, breakup or relocation after observation: historical notes survive and affected assessments become stale.
12. A poor opening, lost lead, or small-city start: at least one understandable next path remains, without free money or spawned talent.
13. Founding act departs amicably versus after a broken promise: distinct, causally supported consequences.
14. Disable/observer modes and legacy migration: documented compatibility and replay behavior.

### Experience and balance measurements

Pre-register comparisons against the chosen scene baseline. Treat initial numbers as hypotheses, not required outcomes.

| Measure | Question |
|---|---|
| Visits and distinct evidence before an approach | Is another visit a purposeful choice? Two to four can be a useful median hypothesis, with valid one-visit deals and longer pursuits. |
| Player hours and money to a viable offer/first signing | Is difficulty coming from meaningful tradeoffs or empty scheduling and compulsory clicks? Split by city, starting resources and strategy. |
| Refusal and loss reasons | Can players explain the outcome, and did they have an actionable alternative? Do not tune toward a universal failure quota. |
| Candidate funnel | Can players set priorities quickly without a hidden score or a requirement to declare most acts worthless? |
| Scout skill | Does better judgment improve decisions under matched evidence and budget? Examine costly misreads and suitable signings, not merely confidence or signed ceiling averages. |
| Contract diversity | Do different motives produce meaningfully different viable offers and reasons to choose a small label? |
| Rival visibility | Are claims timely, sourced and geographically plausible? How often does observed urgency correspond to a real decision? |
| City and venue distinction | Do itineraries, intermediaries, bills and practical opportunities differ without venue-based talent or reliability stereotypes? |
| Grind and comprehension | How many repeated clicks, unread notes, missed appointments and unexplained refusals occur per meaningful decision? |
| Economy and runtime | Do costs, vacancy burden, releases, label survival, save size and simulation time remain acceptable? |

Start with deterministic fixtures and save/ownership probes, then human playthroughs of contrasting openings. Use matched shorter runs to diagnose shared-system changes. Recruitment, recognition, development or contract-economic changes must satisfy the established paired-seed long-run acceptance for the integration baseline; do not invent new thresholds after seeing treatment results. Pure presentation/document work does not warrant running decade simulations.

The final acceptance is experiential as well as numerical: the player can name the room, explain why they believed in the act, describe what they offered, and understand what they risked by waiting.

## 16. Content and scope boundaries

Use the existing historical atlas and scene profiles for period, place and institutional constraints. Any new historical claim, named venue chronology or community-specific practice needs source review before shipping. This directive's examples are fictional design examples, not newly verified historical accounts.

Preserve the earlier directive's separation of origin, base, presence and market region; its one population owner; its external treatment of access barriers; and its prohibition on attaching ability or reliability to identity. Room atmosphere can be specific without asserting that every musician in a community behaves the same way.

Out of scope for the first implementation: a new population factory, replacement geography, mandatory full foreign touring, a second contract market, exhaustive dialogue trees, a separate interpersonal simulation, universal ceiling prediction, and a recalibration of every AI label's perception. Design extension points where necessary, but deliver the coherent local loop first.

**The expansion should make the existing scene legible through participation: hear something worth pursuing, learn enough to take a position, and persuade real people that this label can give them a future worth choosing.**
