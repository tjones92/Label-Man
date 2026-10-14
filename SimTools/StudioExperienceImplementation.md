# Studio expansion — implementation progress

Branch: `codex/studio-experience-expansion`, based on `scouting-experience-expansion` at `f00635a`. Existing local changes in the project file, artist manager and export presets are preserved and are not part of this slice.

The October 11 studio directive is implementation guidance. Its statement that the original task produced a directive only does not supersede the current request to implement it.

## Delivered

- Esc opens a separate game menu over the current office view. Resume restores that view and input state. Save/load is removed from office chrome; founding still offers Continue and the folded-label recovery button opens the game menu.
- The game menu includes confirmed exit to the desktop, including before founding a label or creating a save. Nested paper confirmations retain Esc priority.
- New studio dates begin with no selected take. Printing validates all selections before doing work. Invalid indices do not select a performance.
- Take cards show frozen engineer, performer and producer impressions, with bounded uncertainty. No raw take bars, hidden-quality recommendation, selected-side quality summary or take-derived radar remains on the console.
- Explicit producer selection uses those same listening impressions, with stable first-take tie-breaking, and preserves selections the player has already made. This is selection delegation, not full date delegation.
- Save v14 preserves unselected dates and their notes. Legacy paid sessions keep their existing performances, cost and selected index; notes are added without rerolling takes. Earlier executables reject v14 rather than silently selecting an unselected take.
- `--profile-startup` reports elapsed and per-stage timings without consuming randomness or changing world initialization.

## Startup investigation

Seed 1001, real autoloads and normal defaults, headless Godot 4.7/.NET Debug build. Two initial runs reached the interface at 66.647 and 31.965 seconds. This is bootstrap-to-interface timing, not editor launch, engine process creation or time to the first displayed frame. Host load, JIT and filesystem warming are not controlled; the second run is not evidence of an optimization.

| Stage | Run 1 seconds | Run 2 seconds |
|---|---:|---:|
| Autoload setup plus initial artist pool | 13.747 | 3.316 |
| Rosters plus 4,000 unsigned reserve artists | 21.736 | 12.411 |
| Competitor / composition initialization | 10.190 | 5.807 |
| Regions and radio | 0.244 | 0.059 |
| Chart prewarm | 0.531 | 0.274 |
| 52 weeks of room history | 14.623 | 7.822 |
| Live room calendar | 2.470 | 1.552 |
| Player/UI initialization | 3.106 | 0.724 |

The final timing markers additionally separate naming-data/autoload initialization, initial artist creation, roster assignment and unsigned reserve creation. The slow work runs synchronously before the player interface is ready. Population construction and historical room scheduling deserve the next focused performance pass; chart prewarming is a small part of the delay. Do not shorten room history or cut the unsigned population merely to improve this timing. A loading surface would make progress visible but would not reduce this work.

A refined seed-1002 run reached interface readiness at 33.165 seconds: naming data 0.281s, remaining autoload setup 0.455s, initial artist pool 2.338s, roster assignment 7.986s, unsigned reserve 4.618s, competitor/catalog initialization 6.154s, room history 8.425s and live room calendar 1.709s. These measurements narrow the follow-up investigation to roster assignment, population creation and room scheduling rather than naming-data loading or chart prewarming.

Raw runs and checks are in ignored `SimLogs/studio-*.log`. A sandboxed Godot invocation crashed before engine initialization; the same executable ran successfully with approved execution outside the sandbox.

## Validation

`dotnet build --no-restore -v quiet`

```powershell
& $godot --headless --path . SimTools/StudioFoundationRunner.tscn -- --seed=1001 --profile-startup
& $godot --headless --path . SimTools/StudioFoundationRunner.tscn -- --seed=1002 --profile-startup
& $godot --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --scouting-session-promise-check
```

The foundation runner loads the real UI and tests menu restoration, nested confirmation priority, unselected print refusal, observation stability and gameplay-RNG isolation, JSON and compressed save/load continuation, legacy migration, evidence-based selection, two-side master creation and exactly-once printing/time/cash. Headless checks do not replace a human visual/input playthrough.

Final results: build succeeded with zero warnings/errors; foundation checks passed 23 assertions on each of seeds 1001 and 1002, including the actual studio scene's disabled print control and absence of raw take bars; the scouting funded-session regression passed 65 assertions. `git diff --check` passed for this slice.

## Original slice boundary

This is a selection/evidence foundation, not the complete early date milestone. Booking still uses tiers and batch-generated takes, spends the booked time upfront and keeps the established realization/economic paths. Observation generation has a private keyed stream; performance generation and final material realization still use their existing random owners. The listening notes are coarse impressions of the current Hook/Production model, not a detailed diagnosis of actual vocal/ensemble/capture components.

Next: map all realization consumers; introduce stable named local room/capability snapshots; persist an incremental action clock, protected source takes, arrangement revisions and cross-song workload; then preparation/direction, finishing and full date delegation. Preserve existing funded-session promise semantics, session-player/payment owners and calibrated composition application. Broader economic or autonomous behavior changes need explicit off/observe/enabled states and separate validation. Human playthroughs through pressing remain required for the complete milestone.

## Incremental working date (second slice)

New player dates use stable fictional local room identities and a saved mono-layout/rate/craft snapshot. Local quality limits the available room selection; sparse cities receive a modest recording service rather than three universal choices. Rates and the existing room-cost recoupment owner are retained. The quote separates room and booked musician cost, includes stock/playback/house finishing, and checks that setup, a pass per song and finishing fit before accepting payment.

Only 20 minutes of setup pass at booking. Rehearsal takes 15 minutes, a pass 12, direction and a break 10 each, and finishing 10 per side. Work is incremental and protects finishing time. Other desk jobs and calendar skips are blocked during the paid date. The current own lineup is checked against overlapping shows/appointments and reserved for the quoted window. An actual paid start keeps the existing funded-session promise; abandoning does not refund incurred costs.

Passes now have distinct vocal, ensemble, feel and capture components. Rehearsal improves coordination within bounds; cross-song workload causes fatigue, and breaks partially recover it. Directions trade one aspect against another, respect basic lineup applicability and can be declined under creative control. Earlier performances and kept choices survive every subsequent action. Engineer, performer, band and producer notes are frozen attributed impressions, with no raw quality cards or truth-based tie-breakers.

The player can delegate remaining work under a capture, tighten or explore brief. Delegation uses the same actions and visible listening impressions, preserves manual choices, limits retries, and reserves time for the flip and finishing. It leaves final review and printing to the player. Masters retain room, selected performance, direction, observations, participants and work history.

Save v15 persists empty/unselected sides, preparation, room snapshot, action number, workload, private take identities and master history. Legacy paid batch dates remain batch dates with their original takes and payment. New take generation and observation use private keyed draws; saving/reloading and consuming unrelated gameplay RNG cannot reroll the next identical action. Final material realization retains its existing RNG owner and is not covered by that private-take guarantee.

Player rollout is `--studio-experience=off|observe|enabled`, default enabled. Off and observe retain the previous batch behavior; observe currently has no additional telemetry. No autonomous production or shared economic defaults were changed.

### Realization owners

`PlayerDesk.PrintMaster` still prepares and applies selected material once. `SongMaterialApplicationService` remains the sole capability/composition blending owner. The new performance calculation does not apply a second capability penalty. Existing competitor singles/promoted masters call `Apply`; album tracks and seeded catalog tracks use `ApplyIdentityToAlbumTrack`. `SongMaterialSelectionService` prepares selection candidates through the existing polar behavior. Those shared consumers are unchanged.

### Validation and remaining scope

`StudioDateRunner.tscn` passed 39 assertions on seeds 1001 and 1002: actual UI controls/no raw bars, setup/payment ownership, availability/reservations, empty-side compressed saves, rehearsal/fatigue comparisons, direction/no-op costs, protected takes, private next-action continuation after reload, bounded delegation, finishing, master histories, idempotent printing and abandonment. `StudioFoundationRunner.tscn` with `--studio-experience=off` passed 23 assertions. The headless foundation fixture reports Godot resource leaks on exit; its assertions pass, but this is not a clean visual playthrough.

The funded scouting-session regression passed 65 assertions with incremental dates enabled. After closing the calendar-skip bypass and correcting the overhead warning's musician fee, the build succeeded with zero warnings/errors and the studio runner passed all 39 assertions again on seed 1001. `git diff --check` passed.

Startup cache comparisons and timing limitations are recorded in [StartupCacheResults.md](StartupCacheResults.md).

This is a playable incremental-date foundation, not the full directive. Persistent engineer relationships, detailed arrangements/material-member interactions, actual hired-musician identity/credits, backing crew discovery, room travel and independent recording layouts remain. The existing session-player booking/payment path is reused, including its anonymous-player fallback. Room identities are fictional and derived from the retained tier calibration, rather than a researched facility catalog. Human sessions through pressing/release, economic calibration and the broader first-three-dates onboarding remain to be evaluated.

## Protected arrangement alternatives (third slice)

The console offers a half-time feel or simpler rhythm figure, including on the first pass. A proposal explains the change, the act's response, preparation/pass time, remaining time and protected earlier takes. Reading or cancelling it does no work and consumes no gameplay randomness. Preparing an alternative takes 20 minutes within the paid quote; returning to the live arrangement takes 10. Both require enough remaining time for a pass and finishing. Preparation resets rehearsal familiarity for the changed part and adds workload. It never selects or replaces a performance.

The alternatives change vocal/ensemble/feel execution with bounded private variation and producer craft; neither grants originality. Half-time reduces movement on the completed recording after the shared material application. The source composition and writing credits remain unchanged. Creative control can refuse an alternative under saved room tension, without charging time or money. All existing passes and manual selections remain safe. Delegation continues using the prepared part and attributed take evidence; it does not invent arrangements or override refusals.

Save v16 records the prepared and captured arrangement revisions. Previous incremental dates default to their original live arrangement without changing paid work, takes, evidence or selections; legacy batch migration remains unchanged. Master histories and catalog cards identify the selected take's arrangement even after later experiments.

Validation: `dotnet build --no-restore -v quiet` passed with zero warnings/errors. `StudioDateRunner.tscn` passed 58 assertions on each of seeds 1001 and 1002, including free proposal/RNG isolation, old-field migration, invalid saved alternatives, creative-control refusal, preparation/pass time protection, protected earlier takes, actual performance differences under identical draws, deterministic arrangement continuation, finished-record movement and selected master histories. `StudioFoundationRunner.tscn --studio-experience=off` passed 23 assertions (the previously documented headless UI fixture exit leaks remain); the funded scouting-session regression passed 65. `git diff --check` passed. Logs are in ignored `SimLogs/studio-arrangements-*.log`. Godot again crashed before initialization inside the sandbox and completed these runs with approved execution outside it.

This adds two deliberate ensemble alternatives, not a full arrangement/personnel system. Persistent engineers, specific member preferences, key changes, instruments/backing identities, travel and independent capture layouts remain. Human opening-date playthroughs through pressing and release and economic calibration are still required for acceptance of the complete early-date milestone.

## Source-bound mono finishing (fourth slice)

New room snapshots name optional tone-filter and compression capabilities independently of the mono layout. The current fictional local services supply a tone filter; the controlled and spacious rooms also supply gentle whole-tape compression. The booking quote lists these operations and their eight-minute time cost before payment. These are provisional authored facilities, not researched historical equipment schedules.

After keeping a source, the player can soften the whole tape's top end or use supported whole-tape compression. The proposal explains reduced edges/attack or closer peaks/dynamics, names the protected source and states that vocal and drums cannot be changed independently. Processing uses eight minutes of already-booked time, adds no fee or performer workload, and protects final playback/mastering time. Each treatment carries a small capture/dynamics tradeoff; the included house finish remains the default and adds no optional work.

Processed versions are retained alongside their untouched sources. Choosing a different take returns to its included house finish; returning to a previously processed source permits free selection of its saved version. Repeated inspection or selection cannot reroll evidence or consume gameplay RNG. Processing changes the final recorded properties once, after the existing shared material realization, without altering the source composition or performance. The master and catalog card retain the actual selected treatment, source identity and engineer's note, with the date's full work log.

Save v17 persists retained processing, selected treatment, frozen results/notes and equipment capabilities. Older paid dates keep their original house finish and capability snapshot; loading does not invent optional equipment or retroactively process takes. Invalid source references, unsupported treatments, duplicate versions and nonfinite/unbounded saved processing are rejected.

Validation: `dotnet build --no-restore -v quiet` passed with zero warnings/errors. `StudioDateRunner.tscn` passed 76 assertions on each of seeds 1001 and 1002, including proposal/RNG isolation, supported equipment, optional work and final-time reservation, protected sources, cached processing, selection changes, old-save defaults, invalid source rejection, the real console's supported controls/no truth bars, processing effects after shared realization, selected master histories, compression save/load and abandonment. The legacy foundation passed 23 assertions with `--studio-experience=off` (its previously documented UI fixture exit leaks remain); the funded scouting-session regression passed 65. `git diff --check` passed. Runs used approved Godot execution outside the sandbox; logs are in ignored `SimLogs/studio-finishing-*.log`.

The remaining scope still includes persistent engineers, actual hired/backing musician identities and credits, travel, deeper material/member interactions and independent capture layouts. Human playthroughs through pressing/release and broader economic evaluation remain required.

## Dedicated studio window (fifth slice)

Roster sleeves and act management now offer Take Into Studio (Return To Studio for an open date). Booking and session work live in a separate paper window. Its three stages guide the player through planning, recording/keeping performances and finishing. One song is shown at a time, with a next-song action after keeping a performance. Once all songs have kept takes, the default view focuses on finishing; reviewing earlier takes and doing more work remain available.

Long explanations live in help overlays, including room costs, date workflow, direction and optional processing. Material comparison and advanced recording options are optional disclosures. Booking selections survive comparison and help refreshes. Quotes show costs, room character, available finishing and the reason booking is unavailable. Printing opens Catalog; closing the studio returns to the prior office view without ending a paid date.

The game menu hides both office and studio windows, including raised tabs and unread stamps, then restores the previous window on Resume. Loading closes the old windows instead of restoring a stale date. Reopening the active studio preserves its return destination. Dynamically created windows explicitly inherit the paper theme. The funded scouting-session regression passed 65 assertions, and git diff --check passed.

Validation: the build passed with zero warnings/errors; StudioDateRunner passed 90 assertions on seeds 1001 and 1002, including the actual roster launch, separate window, menu restoration, help/Esc priority, song switching, comparison selection persistence and observation/RNG stability. The legacy foundation passed 23 assertions with its previously documented exit cleanup warnings. Off-screen rendered screenshots were inspected for planning, recording, finishing and both menu entry paths; finishing controls fit and save/load contains no floating office tabs. Screenshots and logs are in ignored SimLogs/studio-window-*.png and .log. This is UI validation, not a complete human playthrough through release.

## Song structure, located reads and song length (sixth slice)

Each side on a new incremental date carries a saved song structure: archetype, form, length, what it demands of the performers (power, lyric, ensemble, feel) and its ordered parts. A catalog cover or commission uses its composition's archetype and saved form, with the shared `SongProfileDeriver` supplying the demands. An act's own original has no composition until printing (the existing `ArtistWritten` selection still chooses it then, with its existing RNG owner), so it takes a genre default: one of the genre's authored song shapes, picked by a stable private key per act and title. Derivation is pure, keyed and draws no gameplay randomness. Forms map to parts: AABA (verses, middle eight, last verse; a break and second middle eight above 3:10), verse-chorus (a bridge from 2:30), twelve-bar (a solo; a third verse from 2:50), strophic and through-composed. Instrumentals relabel verses as themes or strains.

Lengths are provisional authored early-sixties ranges by archetype: pop sides 2:10-2:40, ballads 2:45-3:15, big builds to ~3:35, workouts and extended pieces 3:20-5:30, classical longer. Each song has a fixed keyed variation; an authored `durationSeconds` on the composition wins. A pass costs twice the song (the take and a listen back) plus six minutes, 9-25 minutes, replacing the flat 12; a whole-song rehearsal costs at least 15. The booking check, plan text, delegation reserves and arrangement preparation use the per-song pass time. A printed master stores its performed length on its registered master taxonomy (`durationSeconds`, previously never written) and in the master's studio history, with the form. The composition itself is not changed.

Each pass is located. Its own vocal, ensemble and feel values, weighted by what each part demands and what the song demands, point at the part where it was weakest. Final vocal parts strain more with fatigue; later parts slip a little with workload; directions soften their own target (lean into the lyric on verses, tighten the ending, hold back the drums in choruses). A private keyed read, widened by lower producer craft, bounds how reliably the producer locates it. The producer's read is frozen with the take ("The middle eight came apart.", "The words get swallowed in the first verse.", or a located compliment on a sound pass), and the engineer names what crowds the vocal in the loudest part when capture is poor. These reads differ between passes, fixing the repeated-phrase problem; the coarse impressions used by evidence-based selection are unchanged.

Working one part takes 10 minutes and tires the band like any run-through. It lifts the aspects that part demands (at most +.05 per aspect) and makes that part less likely to be where a pass fails. Whole-song rehearsal keeps its general coordination effect. Preparing a new arrangement resets both. The console offers a direct "Rehearse the [part]" button for the part the latest pass was heard to fail in, and a rehearsal menu listing the whole song and every part, with how often each was flagged. Talkback shows where a direction lands. Delegation under the tighten brief works the flagged part before its single retry when time allows.

Save v18 persists structures, part rehearsals and located reads, and rejects reads that point outside the saved structure. v17 and earlier dates derive the same structure on load (it is pure) with zero part rehearsals; their paid takes keep the flat pass time and gain no located reads after the fact. Everything is player-only inside the incremental `--studio-experience=enabled` flow; off/observe batch dates are unchanged and no autonomous or shared economic path is touched.

Validation: build passed with zero warnings/errors. `StudioDateRunner.tscn` passed 121 assertions on seeds 1001 and 1002, including pure/RNG-free derivation, distinct shapes per original, a catalog twelve-bar keeping its form and solo, archetype length ordering (ballad over pop by more than 15 seconds, pop within 2:10-2:40), pass time from length, located reads varying across passes, part rehearsal time/workload/RNG isolation and bounded lift under identical draws, a drilled part no longer being located, pre-structure save migration, rejection of out-of-structure reads, master length/form history, and the real console's song map, located note and flagged-part button. Legacy foundation (`--studio-experience=off`) passed 23; funded scouting-session regression passed 65. A rendered capture of the console after a pass was inspected (`SimLogs/studio-window-take-notes.png`). Logs are in ignored `SimLogs/studio-structure-*.log`.

Not done: false starts and passes that break down at their weak part, radio preferring sides under three minutes, period-dependent lengths, and arrangement/direction changes to length. Originals' studio structure is a genre default and can differ from the composition chosen at print time.

## Engineers, room acoustics and return visits (seventh slice; directive slice 2, partial)

New incremental bookings snapshot a v3 room. Each fictional local room has a stable house engineer: a keyed name (the Rolodex name generator), an ear and hands within that room role's authored range, and one working habit with a real effect: a close microphone (helps a quiet lyric), riding the fader (halves loud-part spill), a quick setup (five minutes less) or careful playback (a closer engineer read). On roughly one day in six the house engineer is cutting elsewhere and a relief engineer, with a weaker ear and hands, works the date. Everything is derived from stable keys; viewing a room card draws no gameplay randomness.

Rooms now differ in kind as well as rate. The close service flatters an intimate vocal but loud parts spill; the controlled workshop keeps loud parts separate and adds little; the lively theatre lifts a full band and a big voice but can lose a hushed lyric. The effect on a pass depends on the song's structure demands, is bounded to a few hundredths per aspect (vocal and feel ±.04, capture −.05 to +.04), and applies only to v3 rooms. A cheap close room can therefore suit a ballad better than the theatre, while the theatre suits a stomper. The booking card gives the engineer's attributed opinion of each ticked song in the selected room. That opinion is an expectation read through the engineer's ear (close calls can be misjudged) and says nothing about any take.

The label remembers each finished or abandoned incremental date: room, engineer, act, passes, kept sides, whether it printed, and which parts were heard to fail. Booking the same engineer for the same act again shortens setup from 20 to 12 minutes (less with a quick-setup habit) and gives a closer engineer read. Familiarity belongs to the engineer, not to the room or every booking, and is not a quality multiplier. The room card says what the engineer remembers ("Norm engineered their date on Jan 1, 1960 (4 passes) ... and that the middle eight gave them trouble"), so the player can plan rehearsal. Engineers the label has worked with are filed under a new ENGINEERS tab in the Rolodex, with dates, acts, passes and abandoned dates. Text names the engineer rather than guessing pronouns.

The session sheet shows the engineer, and the date log records who engineered and any familiarity. The setup check, plan and quote use the room's actual setup time. The room snapshot is now copied in and out of saved session state; previously the in-memory capture shared the live snapshot.

Save data adds `StudioVisits` (validated: ids, non-negative counts, one entry per session, a full part list). Saves without it load as first meetings. v3 room snapshots are validated (engineer present, ear/hands/acoustics within 0-1, defined habit, setup 1-60 minutes). v2 and older snapshots keep the original behaviour exactly: no acoustics, the original constant engineer read and a 20-minute setup. Player-only within `--studio-experience=enabled`; no autonomous or shared economic path is touched.

Validation: build passed with zero warnings/errors. The new `StudioReturnVisitRunner.tscn` passed 35 assertions on seeds 1001 and 1002: pure, RNG-free room/engineer derivation; one house engineer with a relief on 6-29 of 90 days; the close room flattering a ballad's vocal, the lively room lifting a stomper and loud songs spilling in the close room; bounded effects; unchanged pre-v3 behaviour; a pass differing by exactly the room effect under identical draws; v3 room save/load and rejection of an impossible engineer; remembered visits with flagged-part counts; the Rolodex card; a faster, closer return visit with the same engineer and none in another room; the familiar setup actually spent; an abandoned date remembered once; history save/load, damaged-history refusal and pre-history saves. `StudioDateRunner.tscn` passed 121 on both seeds (its setup assertions now read the booked room's setup rather than assuming 20 minutes); the legacy foundation (`--studio-experience=off`) passed 23; the funded scouting-session regression passed 65. Rendered booking captures were inspected. Logs are in ignored `SimLogs/studio-return-*.log` and `studio-slice2-*.log`.

Directive slice 2 remains partial. Not done: travelling to rooms in other cities, contact-based access to a distinctive specialist room, choosing or requesting a particular engineer, real session-musician identities and availability, and room renown or pricing that responds to the player's record. Engineers and rooms are fictional and their ranges provisional. Human playthroughs and economic calibration remain outstanding.

## Travel, introductions, named engineers and session players, breakdowns (eighth slice; completes directive slice 2's listed items)

**Booking sheet.** Each room is now its own card instead of one text block on a button. Every card shows the name, a sound tag (Dry & close, Big & lively, Controlled & even, or a specialist's feature), the hourly rate, and chips for "Knows this act" or "Introduction needed". A one-line subline gives the engineer on duty and what the room does to a record. Only the chosen card opens up, in labelled rows: travel, sound, access, engineer (with the room's other engineer), what the engineer remembers, and finishing. Rooms are grouped "In town" and "A drive away".

**Rooms in other towns.** The sheet offers every local room, the home town's specialist room if it has one, and up to three rooms in reachable towns (same or adjacent region, at most 6h away) whose studio quality beats home by more than .05, or that have a specialist. Booking one drives there through the existing `Travel` owner: its hours and gas, collections on arrival, and the player physically in that town. The date starts on arrival, and the drive plus the date must fit before the hard stop. Studio work requires being at the booked room's town (`RequireAtStudio`), and driving anywhere else is refused during a date. Afterwards the player drives home the ordinary way. Gas is not studio cost and is not recouped.

**Specialist rooms.** Towns at studio quality ≥ .85 (New York, Los Angeles, Nashville, Chicago, Memphis, Philadelphia) have one fictional specialist room. Its feature is picked by stable key: echo chamber, theatre floor, dead room or church hall. Each sets its own acoustics, two of them support a new **chamber echo** finish (hook +.015, production −.005), and the rate is 1.4× the town's top rate. The owner books regulars and people who come recommended. There are three ways in: having recorded there before, a Rolodex DJ (Known or better) at a station in that town, or a session player there who has a contact edge, without a fallout, with a member of one of the label's acts. All three are read-only.

**Engineers.** v4 rooms have a chief (the old house id, so remembered visits still match) and a second engineer (−.12 ear, −.06 hands). The chief is away about one day in six. On about half of the remaining days the chief is booked with regulars, so the room puts the second on. The player can ask for whichever engineer is in. Asking for the chief costs a quarter of the room rate per hour (at least $3), booked through the room-cost owner. Asking for an engineer who is away is refused. The Rolodex now labels chief, second and relief engineers.

**Named session musicians.** A Band Room booking now picks a real local player who plays the replaced member's part (from `SessionEmploymentService.ListeningCandidates`, regulars first). The old pool fallback is kept. A booking carries `PersonId`/`Role` and arrives on the date as a `StudioSessionHire`. Paying, the lift and the member's humiliation are shared with call-ins through `ApplySessionHire`. Player hires record session work, regular standing and contacts through the new `SessionEmploymentService.OnPlayerSession`. Hired players are reserved for the date. Each take records who played on it, and a master credits only the session players on its kept take, so a take cut before the call-in stays the band's own.

**Breakdowns.** A pass can fall apart at its located weak part. The chance is zero below severity .45, then scales with how loose the band is: tightness = .35 × the skill of the player that part rests on (the lead voice for power/lyric, otherwise the loosest player) + .65 × studio performance, with zero at .58 or above. Room tension and fatigue raise it, and it is capped at .75. A breakdown costs the time to the stop plus a 3-minute reset. It is logged, attributed ("Pete lost the changes in the middle eight and the band stopped.") and never keepable by the player, the producer or a save.

**Flubs.** Separately, any band, however tight, false-starts about 4% of passes (up to 8% tired): 2-3 minutes, early in the song, blaming nobody's part.

**Call-ins.** Once one non-singing member has caused two breakdowns on the date, the producer suggests a named local session player who plays the part and is free. The card shows the fee and a 30-minute wait (the band rests), and says the member will take it hard. Calling them in replaces that member for later passes (ensemble lift up to +.06; the lift also lowers later breakdown odds). Declining ends the suggestion for that member. Flubs never count toward it. Delegation works through breakdowns by rehearsing the failed part and retrying (at most 3 attempts per side), and never hires anyone.

**Save v19** adds take `Incomplete`/`FalseStart`/`FaultPersonId`/`SessionPersonIds`, session `Hires`/`DeclinedCallIns`/`BreakdownVersion`, and v4 room fields. It rejects a kept incomplete take, an incomplete take on a pre-breakdown date, unknown session players on a take, and invalid hires or premiums. Older dates load with `BreakdownVersion` 0 and never break down. Rollout: `--studio-breakdowns=on|off` (default on).

Validation: build passed with zero warnings/errors. The new `StudioAccessRunner.tscn` passed 179 / 183 assertions on seeds 1001 / 1002. Measured over 56-58 real bands × 3 passes, breakdowns ran 22.2% / 14.0% for the loosest third, 0.0% / 3.5% for the tightest and 9.5% / 9.8% overall. Flubs ran 4.8% / 1.7%. The call-in fired on both seeds. `StudioDateRunner` passed 121 on both seeds with `--studio-breakdowns=off` (it assumes every pass completes). `StudioReturnVisitRunner` passed 35 on both (updated to chief/second staffing). The legacy foundation (`--studio-experience=off`) passed 23 and the funded scouting promise 65. Rendered booking captures were inspected: `SimLogs/studio-access-booking-*.png`.

Not done: specialist owners insisting on their own house players, room renown and pricing responding to the label's records, travel lodging or overnight dates, a call-in for a singer needing backing vocalists, and human playthroughs/economic calibration. Rooms, specialists, engineers and every rate here are fictional and provisional.

## House players, room renown, overnight trips and backing singers (ninth slice)

Historical grounding (sources checked October 13, 2026; design inputs, not a facility census):

- **House players.** The period's distinctive rooms ran with their own rhythm sections, and outside acts mostly recorded with them: Dave Bartholomew's band backed nearly every date at Cosimo Matassa's J&M (New Orleans, about $15 an hour); Booker T. & the M.G.'s backed Stax's acts and the Atlantic singers Jerry Wexler sent to Memphis; FAME's successive rhythm sections were why singers drove to Muscle Shoals. Norbert Putnam recalled that most acts didn't travel with a band and arrived with charts. Sources describe the house band as a draw more than a written rule, so the owner insists only where he has reason to.
- **Renown and rates.** Arthur Alexander's "You Better Move On" (FAME, 1961; the first gold record in Muscle Shoals history) paid for Rick Hall's larger Avalon Avenue studio in 1962 and drew outside acts. No source gave 1960s rate cards for Sun or Gold Star, or documented a rate rise after a hit, so the rate response is a modest authored assumption. The directive's rule applies: renown has a lag and capacity limits, and one hit must not price the label out of its own room.
- **Overnight trips.** Buddy Holly and the Crickets drove from Lubbock to Norman Petty's Clovis studio. Petty charged per song rather than per hour, recorded late at night beside a machine shop, and the band slept in the guest room by day. Singers came to Muscle Shoals and Memphis from far away.
- **Backing singers.** The Jordanaires and the Anita Kerr Singers sang behind a large share of Nashville's late-50s/early-60s sides, often several sessions a day. 1960 AFM/AFTRA scale figures could not be confirmed, so fees reuse the Band Room's provisional scale.

**House players.** At a specialist room the owner's terms come with the booking card (`StudioHouseBand`, pure and keyed):

- A singer without a rhythm section (`NeedsCrew`) gets the house drums and bass, plus piano if nobody plays a chord instrument. They fill parts; nobody is replaced.
- A band the owner doesn't know, below the tight-band bar (.58), has the house drummer and bass player sit in for its own.
- A tight band brings its own players. So does a band that has printed a date there before and plays at .50 or better.
- House players are the town's best-reading session players, with a stable per-room key, so the same regulars recur.
- An owner insists only on parts somebody in town plays on sessions; a town with no session community has no house section.
- A house player who is booked elsewhere locks the room for the day. An act with creative control won't record without its own players, so where the owner insists the room is locked.
- Fees use the Band Room scale, paid at booking and not recouped. A replaced member takes it half as hard as a label's own replacement (`ApplySessionHire(..., upset)`).
- House players are reserved for the date and appear on every take. A member who sat out is never blamed for a breakdown. Masters credit "the room's house player".

**Renown and rates.** A remembered date now records the record ids it printed, and a weekly pass notes each one that charts (best peak and the week it was first seen). This lives on the visit, so renown outlives the chart run. A room's renown is a pure function of those hits and the date:

- Weight: 1 for a top-ten hit, .5 for top forty, .15 for a minor chart record.
- Timing: no pull for 4 weeks, ramping to full at 16 weeks, then a 104-week half-life. Renown = 1 − exp(−sum/1.5).
- Rate: the posted rate rises up to 40% with renown, and the label that made the room's name pays half of the rise.
- Demand: the act that cut the hit always gets the chief and never finds the room full. Other acts find the chief booked more often (.5 → .5 + .3·renown) and the room booked solid on up to 30%·renown of days.
- Renown is never a quality input. The card shows what the room is known for and the rate change.

**Overnight trips.** Rooms more than a day's drive away (6 to 12 hours; the 12h cap is a 9 a.m. start to the hard stop) are listed under "Drive up the night before", staffed for tomorrow, and can't be booked the same day. "Drive up with the band" (`DriveUpForStudio`):

- Drives through the ordinary road owner, then holds the act on a trip for the arrival day and the next.
- Bills the band's motel each dawn (two to a room at the player's $9 rate) alongside the player's own.
- On arrival the sheet lists that town's rooms as local, and `StartSession` accepts them away from home.
- Leaving town, or letting the next day pass, sends the band home. Starting the date logs the trip and the nights.
- Same-day rooms can be driven up to the day before as well, to start with a full day. Gas and motels are the label's costs, not studio cost, and are not recouped. Trip holds keep the band out of scouting appointments.

**Backing singers.** An act needs backing when no member besides the lead sings, or has the voice to hold a harmony (backing voice ≥ .45; a hired singer needs .55 and reading .40).

- `SessionEmploymentService.SingerCandidates` supplies the town's singers. The best two or three free ones form "The [leader] Singers", each paid 10 + 30 × reading.
- A song's call for voices comes from its archetype (gospel shouts, sing-alongs, bright pop and big ballads high; blues, message songs and spoken word low), adjusted by genre (doo-wop, girl groups, soul, Nashville country and teen pop up; folk, blues and jazz down).
- A pass's vocal changes by .09 × (call − .4) × (.5 + voice), clamped to −.02 to +.05, so a group lifts a song that calls for it and clutters one that doesn't. Feel gains 40% of any lift.
- They can be booked on the sheet, which says song by song whether voices help. Or, after a complete pass on a song that calls for them, the producer suggests calling the group in (30-minute wait; declinable for the date).
- Credits name the group once. Delegation never hires anyone.

Save v20 adds:

- Hire `Kind`/`GroupName`, validated: a backing voice replaces nobody and carries no playing lift; a replacement must replace someone.
- Room `Renown`/`BaseRate`/`ActCutAHit`, validated within bounds.
- Visit `RecordIds`/`Hits`, validated: peaks 1-100 and only printed records.
- `PlayerSaveData.StudioTrip`.

Older saves load with no hires of the new kinds, zero renown and no trip. Everything is player-only inside `--studio-experience=enabled`, and no AI or shared economic path changes. With no hits and no trip, room listings, staffing and rates are unchanged from slice 8.

**Validation:**

- Build passed with zero warnings/errors.
- The new `StudioRoomsRunner.tscn` passed 88 / 86 assertions on seeds 1001 / 1002; a few branches depend on who is free that day. It covers: pure, RNG-free listings; overnight rooms (Atlanta → Philadelphia's specialist, 12h); the same-day refusal; the drive, gas and holds; trip save/load and damage refusal; dawn billing; a morning date with no second drive; lapse and drive-off; owner insistence, the tight-band and visited-band excuses, the creative-control lock, house fees once, the half-weight upset, reservations, house players on every take, no blame for sat-out members, credits and invalid-hire refusal; backing-call and effect ordering and bounds; pure group finding; a band with its own harmony refused; fees once; an identical-draw lift within the effect; the mid-date suggestion, decline and call-in with the wait; delegation hiring nobody; hit weight and pull; the weekly chart pass; lag; the regular's rate; chief and calendar demand; renown save/load, damage and session-room refusal; and the booking sheet's grouping, trip button and motel quote.
- Measured renown on one top-ten hit at 20 weeks: .48. The rate went $33 → $36 for the label ($39 market). The hit act got the chief on 80 of 80 days the chief was in. Another act got the chief on 23 of 90 days (37 for an unknown room) and found the room booked solid on 17 of 90.
- Population: 657-694 of ~940 bands and ~990 of ~1,600 singer acts need backing at the .45 bar.
- Regressions: `StudioAccessRunner` 179/183 (updated: overnight rooms are excluded from its same-day list, and house fees count in its cash check); `StudioDateRunner --studio-breakdowns=off` 121/121; `StudioReturnVisitRunner` 35/35; legacy foundation (`--studio-experience=off`) 23; funded scouting promise 65.
- Rendered captures inspected: `SimLogs/studio-rooms-booking-*.png`.

Not done:

- Specialist rooms' house players are the town's best readers, not authored named crews. A town's session community can be thin (Philadelphia had none on some days).
- Rates respond only to the label's own records. AI labels don't book these rooms, so other labels' hits don't move them.
- Overnight trips are one stay before a date; multi-day lockouts and late-night (Petty-style) dates are not modelled.
- Backing singers are not offered on dates run by the producer.
- Human playthroughs and economic calibration remain outstanding.

(Slices 10-13 below close the first four of these.)

## Studio rules v2: second roles, the creative palette, people, rooms and selective recording (slices 10-13)

Every behaviour below applies only to dates booked under **studio rules v2** (`PendingSession.RulesVersion`, set at booking to `PlayerDesk.StudioRulesCurrent` = 2). A date paid for under earlier rules keeps them: no own harmony, no reshaping, no new arrangements, no stock, no incidents. Save v21 adds per-date (`StudioDateWork`), per-side (`StudioCutWork`) and per-take (`StudioTakeWork`) state; all three are null on older dates.

### Members' second roles

`Musician.secondaryRole` was declared and read by studio code but never written, so it read as `LeadVocals` for everyone. `MemberRoleService` now assigns it once per person (`secondaryRoleVersion`), keyed on the world seed and person id, using the voice and instrument axes where they exist and their expected value otherwise (so it never generates axes early). `MusicianRole.None` is appended to the enum (saved roles keep their numbers). Generation, pooled-person creation, joins and the formation slot swap all assign it; a person whose main part changes keeps the old part as their double. Older saves assign it on first read.

- **Harmony** (`BackingVocals` as a second role) goes to non-singing members by genre (surf .65, beat .60, folk .55, country and rock-and-roll .45, doo-wop .40, soul/R&B .25, blues .15, jazz .08), less for drummers (×.6) and horn players (×.3), scaled by the member's voice. These are provisional authored rates from broad period practice (Beach Boys, Beatles and folk trios built on members' harmonies; Don Rich in the Buckaroos), not a census.
- **Doubles:** singers in folk (.70 guitar), country (.55) and blues (.40 guitar, .20 harp) play; pianists double on organ, organists on piano, drummers on percussion, fiddlers on guitar, and so on.
- **Readers:** a bandmate who sings harmony with a voice of at least .35 now counts as a harmony voice (`MemberRoleService.SingsHarmony`). On rules-v2 dates the act's own harmony singers (best three, discounted when only one or two sing) act like a backing group on a pass: they lift a song that calls for voices and clutter one that doesn't. Member cards show the double ("bass, sings harmony").
- **Measured, seed 1001:** 812 of 2,499 drummer bands have a harmony singer by role; 56% of folk lead singers play guitar.
- **AI economy unaffected:** only studio code reads the role. A 104-week canonical run, hashed against a fresh reference at 7a95367 (`SimLogs/ref-7a95367-1001-104`), matched on all 92 economy CSVs.

### The creative palette (directive §5, §7)

- **Key, tempo, ends.** Lower or raise the key (15 min), push or ease the tempo (10), shorten the intro and ending by 12 seconds (10). A change reshapes the side's structure (its length and vocal demand; the parts stay so located reads keep their meaning) and halves the drilling of a key or tempo change. Each take records the shape it was cut in, and its master is printed in that shape: length, danceability (±5% for tempo), hook (+.01 trimmed). Raising the key showcases a strong voice and strains a weak one; lowering it holds up late in the day and darkens the record.
- **Room balance.** Bring the vocal forward (fixes what crowded the last pass; costs some blend) or open the band up (more feel, harder to hear clearly). 8 minutes, applies to later passes.
- **New arrangements.** Piano carries the figure (needs someone who plays piano, a second role counts), a horn line (needs a horn and a written part), handclaps, backing answers (needs voices). The originals' effects are unchanged. Arrangements and parts change the record's hook and danceability after the shared material blend, never the composition.
- **Who favours and who resists.** Every proposal names the members for and against it (whose part grows or shrinks; strong egos dislike gimmicks). Going ahead over objections raises room tension (logged by name). An act with creative control refuses when its leader objects and the room is tense.
- **Incidents.** About 12% of complete passes (more when tired or tense): an amplifier buzz that stays on every pass until fixed (10 min), a broken string that stops the pass (never blamed on a part), a late entrance, a pickup into the first verse nobody planned, or a harmony somebody sang unasked. Census of 400 keyed rolls on one act: buzz 16, string 17, late 11, pickup 5 (that act had no voice of .40 for an unasked harmony).
- **Ideas from the room.** At most two at a time, read-only and stable: the player who played the pickup, a pianist with a figure, the harmony singer, the horn player, the producer after a part fell apart twice or the top kept straining, the engineer after a crowded pass, a restless member after four passes the same way, a hired producer. Try it (same time and cost as asking directly) or pass (free; a proud member goes quiet).

### Stock, help and delegation (directive §6, §8, §10)

- **Tape stock.** The quote includes about four passes a side plus two, or enough for passes over 60% of the booked time, whichever is more. Out of stock, a pass is refused with two choices: buy a reel ($8, four passes; studio cost, recouped like the room) or record over a take you don't need, confirmed at the point of reuse. A take recorded over can never be kept or chosen; kept takes and the sources of finishes or derived takes can't be reused.
- **Horn players and arrangers** come from the town's real people (`SessionEmploymentService.HornCandidates`: sax or trumpet, as their part or a double, reading ≥ .55). A horn line needs somebody to write it: a member who reads and has ideas, or a hired arranger. Horn players and arrangers are credited only on takes with the horns.
- **A producer** can be brought in at booking: the town's best free reader with ideas, $40 + $60 × craft, directing at their craft, with a leaning from who they are (an ideas person likes to explore). Credited on the masters.
- **Delegation** takes a hard spending limit (zero hires nobody and buys nothing) and stop points: a take that sounds like the record, an idea someone has, a side running out of time. Within the limit the producer buys stock and calls backing singers in when a song sounds bare. The hand-back names the producer and counts the passes and money.

### Rooms (directive §4, §9)

- **Standing house crews.** A specialist room's house band is now a named crew ("the Holt Rhythm Section", "Holt's Regulars"), formed once from the town's session players by the same reading-and-skill rule and saved. Its regulars play date after date; a regular who has left town, retired or died is replaced, and on a day a regular is booked elsewhere the owner calls a deputy, credited as such.
- **Other labels' hits.** AI labels record in their home town, so each charting top-40 AI record is attributed (keyed per record, weighted toward the better rooms and the specialist) to a room there. Those hits add to a room's renown at a quarter of the weight of the label's own, with the same lag and decay. The label's regular's rate still applies only to rooms made known by its own records. Seed 1001, start of 1960: 50 records attributed, New York's specialist the busiest.
- **Lockouts.** Book a room for up to three days at a block rate (10% off each day). The room stays set up; the calendar may move one day (and only one) while the date is open. Each morning the room is the label's from nine; the band sleeps off 70% of the previous day's wear and, away from home, sleeps in a motel at the label's expense.
- **Evening dates.** Half the rooms take evening work from 6 p.m. at 75% of the day rate. The band arrives with 25 minutes of the day already on them. Evening dates run to the hard stop. Norman Petty's Clovis nights are the period model, but a true after-midnight date needs a different clock.

### Selective recording (directive slice 4)

- **Dated equipment.** Each room has a machine dated by role and town (`StudioEquipment.TracksFor`): top rooms and specialists 2 tracks from 1957, 3 from 1959, 4 from 1963, 8 from 1966; mid rooms 1960/63/65/69; cheap services 1963/66/68/72. Strong recording towns adopt a year earlier, weak ones a year later, and each room's purchase falls within a keyed two-year window. Anchors: Ampex Sel-Sync and three-track as the working pop format into the mid-sixties; Atlantic's 1958 eight-track as an exception; four-track machines what a dealer sold by 1963 (Abbey Road's J37 in 1964); Motown's eight-track around 1964-65 ([History of multitrack recording](https://en.wikipedia.org/wiki/History_of_multitrack_recording), the directive's Abbey Road sources, a Motown draughtsman's recollection on [soul-source.co.uk](https://www.soul-source.co.uk/forums/topic/125127-studio-a-equipment)). Track count is a layout, never a quality ceiling.
- **Capture layouts** chosen at booking: everyone together (always), the vocal on its own track (2+ tracks), or band, vocal and a spare track (3+). A separated vocal is cleaner to balance (+capture with the engineer's hands) and costs the singer a little of the room's feel.
- **Vocal overdub.** Over a complete separated take: sing the vocal again over its backing (song × 2 + 4 minutes, one pass of stock). The new take keeps the backing's ensemble and capture (a generation down on two tracks), with a fresh vocal read and the singer's fatigue.
- **Part overdub.** Onto a three-track take's spare track: backing voices, handclaps or a horn line, with the same requirements as the arrangements. A spare track used is gone; four tracks leave room for two parts.
- **Splice.** Two passes of the same arrangement, key, tempo and layout can be cut together at the part where the first went wrong (or its middle). The first may be a breakdown that got somewhere: the "usable fragment" the directive asks editing to give meaning to. It takes 12 minutes and no tape; an engineer with unsteady hands (< .35) won't do it, and a large difference in feel leaves an audible join.
- **Vocal-forward finish.** Only on a kept take whose vocal is on its own track. A take recorded with everyone together still cannot have its vocal or drums changed independently.

### Song length and radio

- **Period lengths.** Derived song lengths stay at the early-sixties ranges through 1964 and grow about 4% a year after (+24% by 1970). Older dates derive with the 1960 lengths.
- **Radio.** Station candidacy reads a side's length: early-sixties Top 40 favours sides well under three minutes (×1.04) and discounts longer ones (to ×0.70). The tolerance widens 15 seconds a year from 1965, and non-Top-40 formats feel half the effect. Only the player's studio writes a master's performed length, so every AI record multiplies by exactly 1. Provisional authored curve.

### Philadelphia's session community

The slice-9 note that Philadelphia sometimes had no session players was not reproduced. A new `--session-supply-audit` census in `ChartAuditRunner` counts, every day, the session players, horns and singers the studio could book in nine recording centres. On seed 1001 over 104 weeks Philadelphia never had fewer than 44 bookable session players, and the new runner found at least 44 on every day of the opening month. Philadelphia did place fewer acts than Memphis or Nashville, despite its 1960 role, which the sources support:

- Dave Appell's Applejacks were Cameo-Parkway's house band, backing Chubby Checker, Bobby Rydell, the Dovells and Dee Dee Sharp.
- Chancellor, Swan and Jamie recorded in town, and *American Bandstand* went out from WFIL.
- Frank Virtue opened his Broad Street studio in late 1962.
- The Uptown Theater's house band launched Earl Young, Norman Harris and Ronnie Baker (later MFSB) from 1963.
- The city had two AFM locals (77 and 274) and a deep jazz scene.

Sources: [Dave Appell](https://en.wikipedia.org/wiki/Dave_Appell), [Please Kill Me on Cameo-Parkway](https://pleasekillme.com/philadelphia-cameo-parkway/), [Norman Harris](https://en.wikipedia.org/wiki/Norman_Harris_(musician)), [Philadelphia Music Alliance: Baker, Harris, Young](https://www.philadelphiamusicalliance.org/walk-of-fame/baker-harris-young-2/), [Virtue Recording Studios](https://fleamarketfunk.com/2017/05/04/virtue-recording-studios/), [Unions and jazz in Philadelphia](https://swarthmorephoenix.com/2024/03/07/unions-and-jazz-in-philadelphia-a-problematic-informative-and-instructive-history/).

`SceneCityPlacement` gives Philadelphia a 1.8 fit for teen pop, doo-wop and girl groups and 1.4 for jazz, behind `--enable-philadelphia-recording-center`. It is **opt-in** because a population change needs a decade A/B before it becomes the default.

104-week canonical A/B, seeds 1001 and 2002 (`SimLogs/philly-{off,on}-*-104`; the seed-1001 control is `roles-1001-104`):

- Philadelphia-based acts rose 474 → 523 and 431 → 488; mean bookable session players 51 → 62 and 52 → 55; minimums 44 → 53 and 41 → 43.
- Total market units moved by at most 0.24%.
- Genre shares (sum of absolute changes 6-14 points a year) and year-end slots (20-30 moved) went in opposite directions on the two seeds: the usual reshuffle when placement changes.
- Major-family chart share in 1961 rose on both seeds, by 7.2 and 6.5 points. That agreement is why the change stays opt-in until a decade A/B on two or three seeds.

### Validation

- Build: zero warnings and errors.
- New `StudioCraftRunner.tscn`: 114 / 112 checks on seeds 1001 / 1002, covering:
  - roles: assignment, purity, saves, old-save first read and doubles after a role change;
  - Philadelphia session supply on every day of the opening month;
  - equipment and booking: dated machines, layouts refused by a mono room, the morning evening-rate refusal, the lockout rate charged once;
  - the palette: free shape proposals that draw no randomness, shape changes and their per-take record, balance under identical draws, incidents (and a 400-roll census), buzz fixing, ideas (stable, declinable, taken up), resistance raising tension;
  - help: piano and horn requirements, horn and arranger hires, horn credits only on horn takes, the original three arrangements on old dates;
  - stock: running out, reusing a take with confirmation, reels paid once and recouped;
  - selective recording: vocal and part overdubs with sources untouched, the spare track used once, the vocal-forward finish only on separated takes, splices;
  - saves: v21 save/load plus five kinds of damaged-save refusal;
  - time: the lockout's second day and its end, printing in the kept take's own shape and length, evening dates, a hired producer, delegation with a zero limit and with stops, the running order;
  - rooms: standing crews and other labels' hits, including save/load;
  - lengths and radio: period lengths and the radio length fit;
  - own harmony under identical draws, and the real console's shape, balance and tape controls.
- Regressions:
  - `StudioDateRunner --studio-breakdowns=off`: 121 on both seeds.
  - `StudioAccessRunner`: 179 / 183. Its false-start assertion was narrowed to its intent (false starts never count toward a call-in), because broken strings now make false starts commoner alongside real breakdowns.
  - `StudioReturnVisitRunner`: 35.
  - `StudioRoomsRunner`: 88 / 86.
  - Legacy foundation (`--studio-experience=off`): 23.
  - Funded scouting promise: 65.
- The pre-existing `--scene-room-audit` startup failure at 7a95367 is unrelated and filed separately.

Not done:
- (Closed in slice 14) An engineer can't be booked away from their own room.
- (Closed in slice 14) A true after-midnight date needs a clock that runs past the hard stop.
- Bouncing and larger setups belong to directive slice 5.
- Human playthroughs and economic calibration remain outstanding.

## Guest engineers and late nights (slice 14)

Both are rules-independent booking options. With no guest asked for and no late night booked, every listing, staffing, rate and clock value is unchanged.

### Bringing an engineer in from another room

Staff engineers were the period norm, so this is narrow:

- **Who.** Only an engineer the label has worked a date with (a remembered visit) can be asked.
- **Where.** Only to another ordinary room in the engineer's own town. A specialist owner keeps his own engineers on the board.
- **When.** On a quiet day at the engineer's own room. A chief comes out on a day the regulars haven't booked him; a second comes out when the chief is minding the room. Neither comes on a day they're away cutting elsewhere, or when their room is booked solid. On seed 1001 the first engineer the label met was free on 21 of 60 days.
- **What they bring.** Their own ear, hands and habit, and their familiarity with the act (familiar setup, closer read). The host room keeps its own acoustics and rate.
- **Cost.** A fee by the hour on top of the room: 35% of the engineer's own room rate for a chief (at least $4), 20% for a second (at least $3). It is charged through the room-cost owner, like the chief's premium, and recouped the same way.
- **Setup.** A strange board takes 10 more minutes, until they have engineered a date for the label in that room.
- **Surfaces.** The booking card lists known engineers from across town with a "bring in" button, or says why each can't come. The quote names the fee. The date log says "brought in for the date". The Rolodex files the engineer under their own room.
- **Saves.** `StudioRoomSnapshot.EngineerHomeRoomId`, within save v21; null on older dates. A saved guest is rejected when the snapshot is pre-v4, the host is a specialist, the "home" is the host room, the engineer's id is not from that room, the request flag is missing, or the home room is in another town.

### Late-night dates

An evening booking can run on past midnight, to 2 a.m. at the latest. Norman Petty's Clovis nights are the period model.

- **The clock.** `TimeManager.LateStopProbe` lets the open late-night date extend the day's stop to 26:00. Hours 24-25 stay on the same calendar day until the day is ended, and display as 12-1 a.m. Only the desk's late-night date answers the probe, and other desk work is already blocked during a paid date.
- **Booking.** `StudioBookingOptions.LateNight` needs `Evening`, so it starts at 6 p.m. or later at the evening rate. It can be booked for up to 8 hours, provided the date ends by 2 a.m. A plain evening date still ends at the hard stop. The booking sheet has a "Run on past midnight" box under the evening rate. It warns when an evening date won't fit before the hard stop, and when a late night runs past 2 a.m.
- **The cost of the small hours.** Every minute of work after midnight adds a quarter again to the band's workload. The date log notes once that midnight came and went.
- **Afterwards.** When the date ends, the clock stops: nothing else runs at 1:30 a.m. A night that ran past midnight is slept off, and the next day starts 8 hours after the finish, never before nine. A 1:30 finish starts the next day at 9:30. An ordinary day still starts at nine.
- **Saves.** `StudioDateWork.LateNight`; a saved late night must also be an evening (`Night`) single-day booking. The clock's hour can be saved past 23.

### Validation

- Build: zero warnings and errors.
- New `StudioNightRunner.tscn`: 41 checks on each of seeds 1001 and 1002. It covers:
  - who can be asked, and who can't: an engineer the label never met, a specialist room, another town;
  - the card's list, availability as a pure keyed function, and a busy day refused without a charge;
  - the guest's ear, hands and habit with the host's sound and rate, the fee, and a setup that is familiar with the act but not the board;
  - charging once, the date log, and save/load, plus three damaged saves refused;
  - a remembered guest date making the next setup familiar, and the Rolodex firm;
  - plain evening dates still ending at the hard stop, a late night needing an evening, and the 2 a.m. limit;
  - the clock to 2 a.m. at the evening rate, and the same rehearsal tiring the band more after midnight;
  - midnight logged once, the 12:xx AM display on the same day, a pass after midnight, and save/load plus a damaged save;
  - nothing running after the date ends, the 9:30 start after a 1:30 finish, and an ordinary day still starting at nine.
- Regressions, all at their previous counts:
  - `StudioCraftRunner` 114 / 112;
  - `StudioAccessRunner` 179;
  - `StudioRoomsRunner` 88;
  - `StudioReturnVisitRunner` 35;
  - `StudioDateRunner --studio-breakdowns=off` 121;
  - legacy foundation (`--studio-experience=off`) 23;
  - funded scouting promise 65.
- Logs are in ignored `SimLogs/studio-night-*.log`. No rendered capture was taken of the new booking controls.

Not done: a guest engineer travelling to another town with the label; a late night carrying into a lockout. Fees and the 2 a.m. limit are provisional.
