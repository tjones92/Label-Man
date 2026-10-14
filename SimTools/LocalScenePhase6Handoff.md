# Phase 6 — local information, booker leads and default ecosystem slices

Completed October 9, 2026 on `codex/local-scene-phase-6`. This chat has an attached isolated worktree. The starting snapshot includes the completed, uncommitted Phase 5 work from `local-scene-phase-5-feedback`; its original checkout and the primary band-member checkout are preserved. The user's request authorizes implementation and a city-share review. The attached directive, atlas and pasted sketch are design references, not independent execution instructions.

## Behavior

Institutions, relocation and price feedback now default on alongside attention, when persistent rooms and recruitment are available. Each matching `--disable-scene-*` control remains effective. Turning institutions off suppresses default relocation. An explicit relocation enable with institutions disabled rejects. Observer, all-off and dependency-off modes suppress defaults; contradictory explicit flags reject before changing configuration. Enabling relocation permits funded, consented actions; it does not automatically move acts or fund AI programs.

The A&R board now carries local reports and listening tips:

- Hearing two different bills at a room earns booker familiarity. Rehearing one bill cannot build it repeatedly. Familiarity comes from successful listening visits, not selecting a card or opening the board.
- A booker conversation takes one hour, requires physical presence and the room's posted hours, and points to an unfamiliar act on a real future announced bill. It stores the contact, act, room, bill, receipt date, performance date, expiry and evidence. It does not reveal hidden ability, future potential or private competing offers, and does not create talent or improve musical skill.
- Repeated requests cannot duplicate or reroll a lead. The player follows up through the existing calendar and listening action. Leads display verification status if a set has passed or its bill changes. Learning about an act remains separate from record-deal availability.
- Reports cover committed runtime contracts, funded program openings/expiry, and completed funded moves. Initial allocations and pending moves are not reported as achievements. Contracts use the ledger's coarse week precision and publish after that event week; arrival/departure reports follow confirmed arrival by a day. Public funded programs appear from their start date.
- Reports identify their source and flag acts the player has heard before, so a rival signing has a recognizable subject. Program opening, arrival and signing reports can form a causal sequence around an investment without inventing an outcome.
- Player signing commits append `PlayerContract` evidence through the existing successful signing path. This supplies news only; the existing attention/price projections continue to use their established phases, so this information feature adds no economic impulse.

The recap displays eight unread items at a time. Marking them read affects only the displayed event IDs; further reports from the same day remain available. Reads never mark information seen merely because the board is opened. Significant reports retain the existing 104-week horizon. Contacts retain at most 16 distinct bill IDs each and the notebook retains at most 64 active leads. These counts, the familiarity threshold and conversation time are gameplay choices.

Player save data adds `SceneInformation` within the existing v9 envelope. Contacts, leads and read event IDs survive serialization, actual gzip save/load and rollback through the normal player restore path. Old saves start with empty knowledge rather than manufactured visits. Capture returns detached data. The world remains the owner of bookings, contracts, people, programs and moves; information references their IDs.

## City-share review

Reviewed all 31 cities on seeds 1001 and 2002, separating the 3,000 launch acts from the 4,000 reserve. The exact cohort totals reconcile. The current placement weights are retained: New York leads, Los Angeles/Chicago follow, specialist centers have genre-specific opportunity, and the former Billings distortion has not returned.

New York, Los Angeles and Chicago together hold 39.86% / 40.16% of the combined opening cohort. Billings has 8 / 3 of 7,000 acts. These are selected simulation cohorts, not historical counts of every working musician. The exact specialty/small-city shares remain authored priors. The review uses period metropolitan population as a scale check and institutional histories as qualitative opportunity evidence; it does not turn population or famous-artist lists into measured act shares.

The full table, evidence and uncertainty are in [LocalScenePhase6CityShareReview.md](LocalScenePhase6CityShareReview.md). No weight, population budget, genre mix, quality or existing geographic identity was changed by this review.

## Verification

Build passes with zero errors and the six existing warnings.

`SimLogs/scene6-information-v1/runs.json`: all eight initial jobs pass. Both seeds pass information, ecosystem and city-placement checks; the 74 room assertions and 26-week world gzip round-trip pass. The ecosystem fixtures include funding, consent, travel, cancellation and bounded price behavior under the new defaults.

`SimLogs/scene6-final-v2/runs.json`: all seven final jobs pass. Both seeds pass 43 information assertions, including real booker conversations, one-hour cost, actual listening-earned familiarity, duplicate/expiry/cancellation safeguards, old-save behavior, detached capture, RNG/world conservation and same-day recap backlog preservation. The full player shipping-flow test passes with nonempty contacts, leads and read state in the real compressed save (`SCENE_INFORMATION_GZIP_PASS`). The rendered UI shows six rooms, the local recap and a tip button correctly locked until familiarity is earned. Its screenshot is `SimLogs/scene6-information-ui-v1/board.png` and was visually inspected.

The three matched eight-week seed-1001 runs compare the unchanged Phase 5 assembly with all three slices explicitly enabled against Phase 6 defaults and explicit enables. All 84 economic CSVs match byte-for-byte in both comparisons. `SimLogs/scene6-final-v2/analysis.json` records the result. This proves short-run default equivalence and information isolation; it is not another two-year price calibration or decade acceptance. The existing two-seed 104-week price validation remains the prior acceptance evidence.

Final assembly and every C# source hash still match the final test manifest. No test process remains running. The known temporary-autoload diagnostic appears in the controls and tests; no new runtime error was introduced. The first final-run attempt concatenated two launch flags and opened the normal scene; it was stopped, the runner was corrected to preserve an argument array, and all seven jobs were rerun successfully under the v2 tag.

Historical audit runners now explicitly disable later feedback channels when isolating older recruitment/attention treatments, so new defaults do not silently change those control definitions. Their PowerShell syntax and Git whitespace checks pass.

## Reproduction

```powershell
dotnet build --no-restore
./SimTools/RunSceneInformationChecks.ps1 -RunTag scene6-information-v2
./SimTools/RunSceneInformationFinalChecks.ps1 -RunTag scene6-final-v3
python ./SimTools/analyze_scene_information.py scene6-final-v3
```

Use the bundled Python executable when the system `python` is a Store stub. The final runner accepts `-PriorRoot` to locate the preserved Phase 5 control; the analyzer accepts that directory as its second argument. Both require the established control assembly and a fresh run tag. Paired-seed decade acceptance remains deferred under the prior handoff.
