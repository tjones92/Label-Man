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
