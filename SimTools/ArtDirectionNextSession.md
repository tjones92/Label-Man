# Art direction: next session

Branch `codex/playtest-ui-ux-fixes`. Source: the art-direction critic's notes
(`Label-Man-playtest/Playtest/critics/art-direction-critic.md`, final report plus addendum A). This file records where that
list stands after the second art pass, then sketches the **Label brand kit** (deliberately not built yet) and lists the
other critic pieces still open.

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
| 8 | Index cards, carbon contract | Done: A&R index card (typewriter name, stamped verdict, inline buttons); contract is a carbon duplicate with letterhead, DUPLICATE stamp, signature line. Compare dialog is still the modal, not a clipboard sheet. |
| 9 | Chart language | Partial: chart rows (clipped columns, circled live rank numerals, palette movement colours, highlighter on your own record). Radar/bars/identity box not re-inked. |
| 10 | Label brand kit | Built (section 2): data, crest control, founding picker, integration points 1-4 and the nameplate. Wall record, stacks and paper ad remain. |

Item 6 of the user's list (tooltips): "?" badges now use `TipLabel` (`UI/PaperTip.cs`): wrapped at 340px, text centred. Godot
places a tooltip beside the pointer, so it is a centred block, not centred on the badge. Other long tooltips still stretch;
make any that matter a `TipButton`/`TipLabel`.

## 2. Label brand kit

**Status (session 3): built.** `Data/LabelBrand.cs` (shape, palette, lettering, monogram; `For(label)` derives from an FNV
hash of the id, so every AI label and any old save has a brand), `UI/LabelCrest.cs` (procedural crest, the 45 centre label,
`Letterhead`), `UI/LabelBrandPicker.cs` (the founding step) and five fonts in `UI/Fonts` (Bevan, Abril Fatface, Alfa Slab
One, Yellowtail, Limelight, with licences). Landed: office header, label dossier (letterhead + accent rule), artist dossier
"signed to" strip, chart label column (crest chip), contract letterhead, Morning Paper nameplate (Abril).

Not done: wall record and stack centre labels, gold record, paper ad, `RecordJacketWidget` centre label, Save/Load row,
window icon, a SIGNED stamp (only DUPLICATE exists today).

Departures from the sketch below: crests are drawn live (a Control retains its draw commands, so no baked textures were
needed); lettering is five preview buttons, not a drop-down; the brand lives on `AILabel.brand` (null for AI labels, so
nothing new in the world save) and maps through `LabelSaveData.Brand*`. Fonts: the Abril file is the Google Fonts served
build, because the copy in the google/fonts repo segfaults Godot 4.7's font importer; Yellowtail is Apache 2.0, the other
four are OFL. Verified by driver: founding picker, office header, chart, contract, both dossiers, paper, a 12-label gallery,
a save/load round trip (including an old-save fallback) and an even spread of brands over all 601 labels.

### Original sketch (kept for the open items)

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

## 3. Other critic pieces still open

Ranked by how much of the painted-versus-flat gap each closes.

1. **Charts re-inked** (top-ten #9, remainder): radar and bars as grease pencil on graph paper, retire teal and lilac,
   identity box at 220px or more, perceived-fit as lamps. `PolarComparisonWidget.cs`, `ReadBar.cs`. Also reuse the chart
   rows as one "trade sheet" scene for genre charts and regional breakouts, and a masthead-free plate so the trade name
   and chart type are live text. `ChartTitle` in `MainMenu.tscn` still reads "BILLBOARD HOT 100".
2. **Compare dialog as a clipboard sheet on felt** with brass pins; today it is a `PaperModal` around the radar.
3. **Finances as a ledger sheet**: tabular Courier, right-aligned figures, negatives in parentheses in stamp red, totals
   under a double rule. `Table()` already right-aligns; it needs ruled columns and the sheet.
4. **Rolodex cards with A-Z tabs, roster as 45-sleeve cards, dossier portrait and boxed typed fields** (acts still have no
   faces: halftone silhouettes coded by genre).
5. **Distribution, Office and Catalog as pressing-plant work orders**: three-part NCR set, boxed QTY/SPEED/SIDE fields,
   a RUSH stamp. Catalog's stage board is already a board; Office and Distribution are still prose.
6. **Feedback toast as a Post-it or telegram slip** next to the control that failed, not the top-left toast.
7. **Morning Paper follow-ups**: quiet-day fillers (weather, a price ticker, an ad), rolling repeated sales lines up
   ("36 copies at 4 tables"), a halftone slot, clippings with stamps (SOLD OUT), slide-out on fold, an Extra edition.
   The author declined suppressing quiet days; fill them instead.
8. **Desk props and time**: animated smoke, dust in the lamp cone, blind-slat bands, ashtray that fills with overtime,
   pink "WHILE YOU WERE OUT" slips for queued calls, a Rolodex prop, the door as a route map, a filing cabinet for
   Save/Load, record-stack height tracking catalogue size.
9. **Iconography**: Ear, Street, Suit, Fixer as linocut icons; stars to vinyl discs (`StarBar` is still a glyph helper).
10. **Type upgrades that need font files** (the nameplate is done in Abril Fatface; the pad note is gone): Special Elite
    for index-card names (Courier Prime Bold today), Abril Fatface for the nameplate.
11. **Small surface work**: staggered tabs with typed stickers and a red stamp circle for unread counts; the
    "Start a Label" page (origin buttons, star glyphs, no title card); paper-clip, coffee-ring and tilt decals; a shared
    warm grade, grain and vignette over painting and UI.
12. **Critic's unreviewed screens**: Band Room, venue and hearing scenes, label and record detail panels. Ask the critic to
    continue from shot 057 on a fresh playthrough.
