# Art direction: next session

Branch `codex/playtest-ui-ux-fixes`. Source: the art-direction critic's notes
(`Label-Man-playtest/Playtest/critics/art-direction-critic.md`, final report plus addendum A). This file records where that
list stands (updated after session 6), the **Label brand kit** (built), and the other critic pieces still open.

## 1. Where the critic's top ten stands

| # | Critic item | State |
|---|---|---|
| 1 | Bundle fonts, one Theme | Done (`UI/PaperTheme.cs`, `UI/Fonts`). |
| 2 | Re-skin stock controls | Done in the Theme. `PaperModal`'s CanvasLayer did not inherit it (stock grey buttons); fixed by giving its root `PaperTheme.Build()`. |
| 3 | Contrast offenders | Done earlier (tabs, chips, selected sub-tab, NEXT UP). |
| 4 | Calendar and hotspots | Done: transparent calendar hotspot, date lettered on the three painted cards (month, weekday over day, year; weekends in stamp red), hover tag + amber rim on every prop. The HUD (date, clock, cash, NEXT UP, latest news) is back in the paper widget under OPEN THE OFFICE: pencilled on the ledger pad it read as part of the painting, not a prompt. |
| 5 | Paper material pass | Done: `PaperStyleBox` (grain, edge burn, lamp falloff, contact shadow) on the folder, cards, paper, chart sheet, modal, banner, dossiers; `FolderTabStyle` trapezoid tabs that open into the card. |
| 6 | Time-of-day lamp lighting | Partial: the hour tint exists (`UIManager.OfficeTint`) and now also tints the painted-text layer. No animated smoke, dust or blind-slat bands. |
| 7 | Morning Paper v2 | Done: nameplate, ears (edition, price), two ruled columns, lead hierarchy, fold-corner dismiss, settle-in. |
| 8 | Index cards, carbon contract | Done: A&R index card (typewriter name, stamped verdict, inline buttons); contract is a carbon duplicate with letterhead, DUPLICATE stamp, signature line. The compare dialog is now a clipboard on felt (session 5, section 3). |
| 9 | Chart language | Done: chart rows (clipped columns, circled live rank numerals, palette movement colours, highlighter on your own record) and, in session 5, the Polar radar, identity box, fit lamps and `ReadBar` as grease pencil on pinned graph paper (teal and lilac retired). Open: reusing the chart rows as a trade sheet for genre/regional charts; `ChartTitle` is still static scene text ("BILLBOARD HOT 100 — SINGLES"). |
| 10 | Label brand kit | Built (section 2): data, crest control, founding picker, integration points 1-4, the nameplate, wall record, stack disc, jacket disc, paper ad and SIGNED stamp. Complete as of session 5: gold record, Save/Load crest, window icon. The right-hand stack's top disc is deliberately left out. |

Item 6 of the user's list (tooltips): "?" badges now use `TipLabel` (`UI/PaperTip.cs`): wrapped at 340px, text centred. Godot
places a tooltip beside the pointer, so it is a centred block, not centred on the badge. Other long tooltips still stretch;
make any that matter a `TipButton`/`TipLabel`.

## 2. Label brand kit

**Status (session 3): built.** `Data/LabelBrand.cs` (shape, palette, lettering, monogram; `For(label)` derives from an FNV
hash of the id, so every AI label and any old save has a brand), `UI/LabelCrest.cs` (procedural crest, the 45 centre label,
`Letterhead`), `UI/LabelBrandPicker.cs` (the founding step) and five fonts in `UI/Fonts` (Bevan, Abril Fatface, Alfa Slab
One, Yellowtail, Limelight, with licences). Landed: office header, label dossier (letterhead + accent rule), artist dossier
"signed to" strip, chart label column (crest chip), contract letterhead, Morning Paper nameplate (Abril).

**Session 4 landed:** the player's 45 centre label on the framed wall record and on the top disc of the left stack
(`UI/PaintedRecordLabels.cs`, in `paintedLayer` so the hour tints it; hidden until a label is founded), the centre label in
the corner of `RecordJacketWidget`, a "NEW ON <label>" ad at the foot of the Morning Paper whenever a `RELEASED:` story runs
(`UIManager.ReleaseAd`), and a SIGNED stamp in the label's ink on a player act's CONTRACT tab (`ArtistDetailPanel`).

Also fixed: label -> roster -> artist drew the label dossier's open ROSTER tab through the artist dossier. `FolderTabButton`
lifts the open tab with `ZIndex = 1`, and a z lift beats tree order, so a buried dossier's tab showed through the one on
top. `UIManager.StackDossiers` now gives the dossier being opened its own z band (4) above the buried one (2). Any new
stacked panel that holds folder tabs needs the same.

**Session 5 landed:** the gold record (`UI/PaintedGoldRecord.cs`), the Save/Load row crest and the window icon.
Deliberately not done: the centre label on the right-hand stack (its top disc is hidden in the painting; the author said skip).

Departures from the sketch below: crests are drawn live (a Control retains its draw commands, so no baked textures were
needed); lettering is five preview buttons, not a drop-down; the brand lives on `AILabel.brand` (null for AI labels, so
nothing new in the world save) and maps through `LabelSaveData.Brand*`. Fonts: the Abril file is the Google Fonts served
build, because the copy in the google/fonts repo segfaults Godot 4.7's font importer; Yellowtail is Apache 2.0, the other
four are OFL. Verified by driver: founding picker, office header, chart, contract, both dossiers, paper, a 12-label gallery,
a save/load round trip (including an old-save fallback) and an even spread of brands over all 601 labels.

### Original sketch (historical; every item in it has now landed except the right-hand stack)

Goal: the one saturated colour on any page belongs to the player's label. Crest + colour pair + lettering, chosen at
founding, carried through the desk, dossier, contract, chart and paper.

### 2.1 Data

* `Data/LabelBrand.cs`: `CrestShape` (Roundel, Shield, Banner, Burst, Star, Bar), a palette index into a fixed table of
  period pairs (Sun yellow on black, Chess blue on cream, Atlantic red on cream, olive on cream, King orange on black),
  `LetteringStyle` (Slab, Didone, Heavy slab, Script, Deco), and a monogram (up to three letters, defaults to initials).
* Store as plain fields on `AILabel` (Identity group, `Data/AILabel.cs:13`) and map them in the save DTO
  (`Systems/SaveGameService.cs:498`, mapped at `:545` and `:569`). Old saves: a missing brand falls back to
  `LabelBrand.For(labelId)`, so no migration.
* `LabelBrand.For(labelId)` derives a brand for **every** label (AI too) from a stable hash (FNV over the id; never
  `string.GetHashCode`, never the sim RNG). The chart then shows crests for all 600 labels and the player's is one of
  them. It must not touch any RNG stream: prove it with the probe-run byte comparison, and the save round-trip runner.

### 2.2 Rendering

* `UI/LabelCrest.cs` (a `Control`): draws the shape procedurally (`DrawColoredPolygon`, `DrawArc`), the monogram in the
  lettering face, and a second mode, the **45 centre label** (disc, spindle hole, label name on an arc, "45 RPM").
* Chart rows need 30 crests a page: bake once per (brand, size) to an `ImageTexture` and cache it, as `PaperTextures`
  does. The control just draws the cached texture.
* Fonts: five OFL faces for the lettering menu (Alfa Slab One, Abril Fatface, Bevan, Yellowtail, Limelight) under
  `UI/Fonts`, with their licence texts, exposed as `PaperTheme.Lettering(style)`. Abril Fatface doubles as the paper's
  nameplate (today: Gelasio Bold plus a 1px outline). **Needs the font files fetched; ask first.**

### 2.3 Founding step

`PageFounding` (`UI/PlayerDeskPanel.cs:625`, button at `:712`): add a "YOUR LABEL'S PAPER" row between the name field
and OPEN THE DOORS. Six crest toggles, five palette swatches, a lettering drop-down, SURPRISE ME, and a live preview of the
45 centre label plus a letterhead strip that updates as the name is typed. Default = `LabelBrand.For(name)`, so skipping
the step still yields a unique brand. `PlayerDesk.FoundLabel(...)` (`Systems/PlayerDesk.cs:1068`) takes the brand through
the same overload pattern the archetype already uses.

### 2.4 Where it lands, in value order

1. Office header: crest chip and the name in the lettering face (`titleLabel`, `PlayerDeskPanel.cs` ~`:149`).
2. Dossier "Label:" strip (`ArtistDetailPanel.cs:31`) and label dossier title (`LabelDetailPanel.cs:23`) as a letterhead.
3. Chart label column: replace `ChartEntryUI.GetLabelAbbrev` text with a small crest chip plus the monogram.
4. Contract letterhead (the house line in `TermsForm`); the SIGNED stamp in the palette's ink.
5. Desk painting: centre label on the framed record on the wall (about (1366, 159) in 1920 space) and on the top record
   of each stack, in `paintedLayer` so the light tint applies; a gold record at a sales milestone.
6. Morning Paper ad box carrying the crest after a release; `RecordJacketWidget` centre label.
7. Save/Load list row, window icon (still the default Godot `icon.svg`).

### 2.5 Decisions that are the author's

* Do AI labels get crests (recommended: yes, same derivation, which is also what makes the chart feel populated)?
* Can the player re-brand after founding (a cost? a once-only freebie)?
* Real label names on the chart (Columbia, RCA Victor) against a fictional set: unresolved IP question from the critic.
* Accent contrast: Sun yellow and King orange only pass as fills; any text on cream must use the black/ink side of the pair.

### 2.6 Verification

Temporary driver recipe (see the UI verification memory): a gallery shot of the 45 label and the letterhead for twelve
AI labels plus the player's, the founding step, a chart page, the contract, and the desk with the wall record. Headless:
determinism of `LabelBrand.For`, save round trip, probe-run byte identity.

Estimate: one session if the fonts are approved (data, crest control, founding picker, integration points 1-4); the wall
record, stacks and paper ad the session after.

## 2b. Session 5: the charts re-inked, the clipboard, the ledger, and the brand kit's last pieces

**Pencil and paper.** `UI/GreasePencil.cs` is the shared drawing kit: graphite, blue pencil, green pencil and red grease pencil,
a wandering two-pass stroke (the wander is a hash of seed and vertex index, never an RNG, so a redraw is stable and no seeded
stream can move), dashed lines, graph paper and brass pins. Series colours are fixed across the Polar read: **graphite** = the
song as heard, **blue** = the arrangement or performance, **green dashed** = your read of the act, **red grease** only where the
song reaches past what the act can do. `ReadBar` uses the same pencils on a ruled strip with a highlighter wash for the doubt.

**`PolarComparisonWidget`** is one 890x430 plate of graph paper pinned at its corners (`ShrinkCenter`, so it never stretches).
The identity box is 224px. Fit is a row of ten indicator lamps: lit up to the best guess, a dim glow through the doubt, dark
beyond; the bezel is the series' pencil colour. Radar names sit outside the rim on their own side. The legend is stacked in the left
column because a one-line legend runs into the identity box's notes.

**Clipboard.** `PaperModal.OpenClipboard` (felt baize backdrop + vignette, `UI/ClipboardStyle.cs`: hardboard, hanging hole, paper
sheet, brass clip, all drawn by one stylebox so the card grows with its contents). It is used for COMPARE HEARD MATERIAL and COMPARE
PLAYBACK at width 1040 (the plate is 890 and the scroll bar needs room). `ComparisonCard(..., onSheet: true)` drops its own paper
inside a modal; inline (roster, catalogue, takes) the card is a `PaperStyleBox` sheet with the plate on it.

**Ledger.** `UI/LedgerTable.cs` draws the whole page itself (paper, double red margin rule, ruled rows, ruled figure columns,
Courier Prime figures). A cell that starts with "(" is a loss and draws in stamp red; `LedgerTable.Money` and `.Deduct` make them;
`Total` rows get a single rule over and a double rule under. It replaced `Table()` on THE BOOKS, LAST WEEK'S SETTLEMENT, WEEK BY
WEEK, RECORD BY RECORD and ARTIST ACCOUNTS. Master lease/sale buttons moved out of the record rows into a "a one-off deal on the master"
line under the table, because a table drawn as one control cannot hold buttons.

**Gold record.** `PaintedGoldRecord` hangs a framed gold disc with a brass plaque on the right-hand panelling once a record passes
500,000 copies (the bar `ChartDetailPanel.GetSalesTierDescription` already calls "Gold Record territory"; keep the two in step).
It shows the best seller and "N GOLD" when there are several, has a hover tooltip, and re-checks once per game day. A record crossing
the bar while you play raises a banner; `Forget()` on load/clock-restore stops a loaded save re-announcing. Its centre label is the
player's own 45 label.

**Save/Load crest.** `SaveMeta` and the header now carry the label's chosen brand, and `SaveInfo.Brand` feeds a crest on each row.
A sidecar written before this commit has no brand, so its row shows the name-derived crest until that slot is saved again (reading
every old 10-15 MB save body to draw a crest costs seconds each; deliberately not done). Verified by a save/list round trip through both the
sidecar and the body-header fallback.

**Window icon.** `icon.svg` is a 45 (black disc, grooves, cream centre label with an oxblood star). Godot uses `config/icon` for the
window and taskbar. There is no export preset yet; when one exists, set `application/icon` to an `.ico` for the exe.

Verified by a temporary driver scene (deleted): desk with and without the gold record, all five Finances ledgers, the Save/Load list,
studio and ear clipboards on fabricated reads, the real inline card through ROSTER > MANAGE, ReadBars and the icon at five sizes.

## 2c. Session 6: tabs, decals, the grade, the label and record cards, the venue board

**Morning Paper bug.** `DismissMorningPaper` ended with `paperSheet.Position = Vector2.Zero`. On a centre-anchored Control
`Position` is absolute, so every paper after the first opened in the top-left corner. `UIManager.CenterPaperSheet()` re-applies
the anchors and offsets (at build, after the slide-out, and again before each show). Verified by driving three morning
cycles and reading the sheet's position each time.

**Tabs.** `FolderTabStyle` takes a `Stagger` (0..2: a closed tab sits 3px lower per step, picked by child index % 3) and draws a
typed cream sticker with a hairline edge, a drop of shadow and a hair of tilt. Tab text is Special Elite. Unread counts are no
longer "LEDGER • 3" in the text: `UI/TabStamp.cs` strikes a red double-ring stamp over the tab's shoulder
(`TabStamp.Apply(button, count)`). Both the office tabs (`StyleFolderTab`) and the dossier tabs (`FolderTabButton`) use it.

**Decals.** `PaperStyleBox.Decorated(clip, ring, seed)` adds a steel gem paper clip on the top edge and/or a coffee ring, drawn in the
stylebox (so no nodes) and seeded so a sheet always wears them the same way. On: the Morning Paper, the office page, both
dossiers, the record card, the modal. The clip sits in the sheet's margin, leaning outward.

**Grade and vignette.** `UI/ScreenGrade.cs` is a CanvasLayer (90, above the modals) with a multiply warm tint and a radial
dark-brown vignette, installed from `UIManager._Ready`. One place to change the look of the whole screen. Strength is
`VignetteStrength` (0.22 after a first 0.36 read as a stage spotlight on the light pages).

**Label dossier.** The roster is 45-sleeve cards (`SleeveCard`, as on the player's own roster) with the label's own disc in the
die-cut window; headings are a typed rust rubric over a hairline and body copy is the serif. The artist dossier got the same
headings.

**Record card (`ChartDetailPanel`).** Still the scene's nodes, restyled in `Restyle()`: paper sheet with clip and ring, the title in
the serif, the act typed, the label's 45 in the corner, the position circled like a chart-row rank, state struck as stamps (No. 1,
New entry, Bullet, Anchor, Gold record, Million seller), sales in a ruled typewriter box and tags as typed stickers. The card now
sizes to its contents and grows from the middle (the scene fixed it at 700px).

**Venue scene.** The A&R page's dropdown is four handbills (`UI/VenueBoard.cs`): a linocut of each room (`VenueIcon`: roadhouse,
marquee, swing doors under a star, microphone), the name in poster slab, who plays it, the ask, the hours and an OPEN / SHUT
stamp. The picked room is pinned and ringed. Same state as before (`selectedVenue`, `hasUserSelectedVenue`).

**The Ear linocut** was a bulb: a round head on a narrow stem. It is now an ear in profile (broad top, a tragus on the face
side, one fat helix gouge, an antihelix curl, a tapering lobe) with two bold sound arcs, checked at 128, 64, 46, 30 and 24px.

Verified by the temporary driver in `tmp/shot` (git-ignored): the paper cycle, tabs with stamps, label roster, record card,
the A&R board, the icons at five sizes. **Not done here:** the Band Room (it is untracked work on `band-member-simulation`; see
that branch; it was restyled there in 28f1cf4), the hearing plates beyond the Polar widget (already clipboard sheets).

## 2d. Session 7: playtest fixes, boxed fields

Found in a Copper Kettle save (Mar 4, 1960), all fixed and screenshot-verified through the driver:

* **Office header.** The date/hours/cash block is a `RichTextLabel` set in bold (Libre Franklin Bold). The cash figure is 21px and
  green when positive, stamp red when negative, ink at exactly zero (`PlayerDeskPanel.CashMarkup`; inks are darkened from the chart's
  rising/falling pair to hold against manila).
* **Label dossier, TRACK RECORD.** "Signed to Mercury Records" on Mercury's own page read as the label signing itself, because each
  line is the *act's* career entry. `LabelDetailPanel.NotableMoves` now leads with the act's name, strips the label's name out of
  the line, sorts by year and drops the seeding bookkeeping ("Established Star at launch (seeded canopy ...)"), which is how the
  starting world was built and not an event.
* **Genre and regional record cards.** The country list is a ranking of its own (units this week, `TradeCharts.BuildGenre`), but the
  card was reading the record's Hot 100 fields: a #1 country record said "peaked at #2" and a record off the Hot 100 (position 0) said
  "A top 10 smash, currently at #0" and OUT. `ChartDetailPanel.Show(record, row, listName)` now takes the clicked row; with a list it
  shows the place on that list, the movement against last week's *list* rank, stamps like "No. 1 Country", no bullet/anchor, and a
  second stats line for the Hot 100 ("Hot 100: #2 | Peak: #2" or "Has not reached the Hot 100"). A list keeps no peak or weeks of its
  own (no stored history, by design), so it does not print any. `JournalisticDescriptor.DescribeRecord` also guards `position > 0`.
* **Card wording is deterministic.** The record blurb drew from `GD.RandRange`, the global stream the sim also seeds, so opening a card
  moved it and the wording re-rolled on every open. The record describers now use a generator seeded from the record id, week and total
  units (FNV), so the same card reads the same and no sim stream moves. The label/artist describers still use the global stream.
* **Regional chips over the card.** `ChartUI` adds the MARKET chips and the empty-list note after the scene's `ChartDetailPanel`, so they
  drew on top of it. `BehindRecordCard` slots them in under the card.
* **Boxed typed fields** (`UI/TypedFields.cs`): a rust caption over a typewriter value in a ruled box, wrapping in an `HFlowContainer`.
  On the artist dossier it replaces the "Duo • Teen Pop • East Coast / New Signing | Formed 1960" lines and the CHART RECORD sentence.
  Not yet used elsewhere (the label dossier's header lines and the contract summary are the next candidates).

## 3. Other critic pieces still open

Ranked by how much of the painted-versus-flat gap each closes.

1. **Trade sheet** (remainder of top-ten #9): reuse the chart rows as one scene for genre charts and regional breakouts, with
   a masthead-free plate so the trade name and chart type are live text. `ChartTitle` in `MainMenu.tscn` is static text
   ("BILLBOARD HOT 100 — SINGLES"), and "Billboard" is a real trade name (the critic's unresolved IP question, section 2.5).
   The charts-as-pencil work, the clipboard compare sheet and the ledger are done (section 2b).
2. **Rolodex cards with A-Z tabs, roster as 45-sleeve cards, dossier portrait and boxed typed fields** (acts still have no
   faces: halftone silhouettes coded by genre).
3. **Distribution, Office and Catalog as pressing-plant work orders**: three-part NCR set, boxed QTY/SPEED/SIDE fields,
   a RUSH stamp. Catalog's stage board is already a board; Office and Distribution are still prose.
4. **Feedback toast as a Post-it or telegram slip** next to the control that failed, not the top-left toast.
5. **Morning Paper follow-ups**: quiet-day fillers (weather, a price ticker, an ad), rolling repeated sales lines up
   ("36 copies at 4 tables"), a halftone slot, clippings with stamps (SOLD OUT), slide-out on fold, an Extra edition.
   The author declined suppressing quiet days; fill them instead.
6. **Desk props and time**: animated smoke, dust in the lamp cone, blind-slat bands, ashtray that fills with overtime,
   pink "WHILE YOU WERE OUT" slips for queued calls, a Rolodex prop, the door as a route map, a filing cabinet for
   Save/Load, record-stack height tracking catalogue size.
7. **Iconography**: Ear, Street, Suit, Fixer as linocut icons; stars to vinyl discs (`StarBar` is still a glyph helper).
8. **Type upgrades that need font files** (the nameplate is done in Abril Fatface; the pad note is gone): Special Elite
    for index-card names (Courier Prime Bold today), Abril Fatface for the nameplate.
9. **Small surface work**: DONE in session 6 (staggered sticker tabs, unread stamps, clip and coffee-ring decals, warm grade
    and vignette; the founding page's title card and origin buttons were done earlier). Left: a grain layer in `ScreenGrade`.
10. **Critic's unreviewed screens**: label dossier, record card and the A&R venue scene are done (section 2c). Band Room is on
    `band-member-simulation`. Ask the critic to continue from shot 057 on a fresh playthrough.
