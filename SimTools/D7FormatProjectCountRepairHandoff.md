# D7 format project-count repair

## Source diagnosis

`d7-format-structural-repair-decade-522-1001` crossed the catastrophic hard gate
at the first completed year:

```
CompletedYearCatastrophicDivergence,scheduledAlbumProjects,1476,1083,1960,53,"1/6/1961",ratio=1.362881 band=[0.70,1.30]
```

The repair had two pieces. The first — a count-neutral probabilistic format
chooser — was already landed in `67ab461`. It was re-verified here rather than
assumed: inverting the recorded logistic on
`d7-format-count-neutral-replay-52-1001-format-decision-explanation.csv` gives

- binary economic argmax: 1363 Albums
- probabilistic chooser:  1368 Albums

so the chooser contributes **+5 of 1368** and is genuinely count-neutral while
retaining crossover behaviour. It was not the residual problem.

That meant the remaining excess over the pre-commit baseline
(`d7-genre-calibration-pass2-522-1001`, 1220 Albums in 1960) was entirely
economic. Comparing *binary* win rates across the two runs removes the chooser
from the comparison and localizes the uplift:

| genre | pre (binary) | post (binary) | ΔAlbums | album format tilt |
|---|---|---|---|---|
| TraditionalPop | 186/466 = 0.40 | 255/433 = 0.59 | **+69** | 1.114 → 1.115 (unchanged) |
| Gospel | 12/103 = 0.12 | 63/94 = 0.67 | **+51** | 0.642 → 1.238 (orientation) |
| Country | 21/318 = 0.07 | 59/320 = 0.18 | **+38** | 0.717 → 0.718 (unchanged) |
| Blues | 59/126 = 0.47 | 93/127 = 0.73 | **+34** | 1.000 → 1.000 (exactly neutral) |
| — total — | 1220 | 1363 | **+143** | |

Blues carries a format tilt of *exactly* 1.000 and still gained 34 projects, and
Traditional Pop and Country gained 107 between them with tilts unchanged to three
decimals. Orientation was therefore never the primary driver.

The actual seam is `CompetitorManager.CalculateAlbumPriorMarketReconciliation`,
which `df6d9dc` changed from

```csharp
return Mathf.Clamp(routedRelative / Mathf.Max(.000001f, acceptedRelative), .25f, 4f);
```

to a bare `routedRelative`. The Album prior's unit scalars were calibrated
against the legacy relative-market comparison, so removing that divisor handed
every large legacy-domain genre — Traditional Pop, Country, Blues, Gospel — an
unearned Album uplift of up to ~30% at the format fork. The change's own
rationale was sound (the divisor was undefined for canonical genres whose legacy
comparator pool is zero), but it over-applied: it dropped the divisor for every
genre rather than only for the genres that lacked a denominator.

## Implemented correction

1. **`CalculateAlbumPriorMarketReconciliation`** — the accepted-relative divisor
   is restored for any genre that has a legacy comparator pool; genres with
   `acceptedSelected <= 0f` keep the bare routed factor. This satisfies both the
   original calibration and the `df6d9dc` fix without erasing canonical-genre
   Album affinity.
2. **`LiveAlbumDecisionEligibilityScale` 1.07f → 1f** — the live-only eligibility
   thumb existed to keep near-miss Albums alive under a binary fork. The
   probabilistic chooser now carries that behaviour explicitly and
   count-neutrally, so the scale had become a double count. The parameter stays
   plumbed through `ResolveAlbumDecision` so the seam remains explicit, bounded
   and independently probeable.
3. **Gospel `SingleOrientation` .40f → .70f** — restoration of the pre-`df6d9dc`
   value, per the standing instruction that the restored prior remains.

Traditional Pop and Country orientations were briefly moved during this work and
**reverted**. They were threshold-driven, not historical: early-60s traditional
pop *is* the adult LP market (Sinatra, Cole, Williams), so `.45` album-leaning is
correct, and 60s country was singles/jukebox-led at `.65`. Genre orientations are
historical data and are not a calibration knob.

## Result — 52 weeks, seed 1001, gate armed

Run `d7-format-orientation-audit-52-1001`. No catastrophic rows; D5 probe suite
passes.

| gate metric | enabled | control | ratio |
|---|---|---|---|
| scheduledAlbumProjects | 1105 | 1083 | **1.0203** |
| successfulReleases | 4548 | 4298 | 1.0582 |
| totalUnits | 148,362,256 | 144,715,423 | 1.0252 |
| grossRevenue | 139,123,518 | 132,865,355 | 1.0471 |
| labelNet | 78,337,125 | 72,065,107 | 1.0870 |
| marketNet | 78,338,755 | 72,154,886 | 1.0857 |

Trajectory: 1.363 (fail) → 1.263 (count-neutral chooser alone) → **1.020**.

## Genre orientation audit

`SingleOrientation` is the second float in each `GenreCatalog.Add(...)` call.
Higher is more Single-oriented; it feeds only
`GenreAcceptanceService.GetFormatMultiplier`, so it moves the format split within
a genre and does **not** move genre market share. The D7P1–P3 share calibration
is unaffected by changes to it.

Three ahistoric values were corrected:

- **psychedelic-rock .35f → .40f.** Its peak baselines (1966/67/68 = .55/.95/.90)
  sit exactly on a singles-rich window — White Rabbit, Somebody to Love, Incense
  and Peppermints (#1), Purple Haze, See Emily Play, Time of the Season. The
  genre also created album rock (Sgt. Pepper, Surrealistic Pillow, Piper at the
  Gates of Dawn), so it stays clearly Album-leaning, but it should not read as
  more Album-pure than easy listening. `.45` was rejected: by 1969–71 psych is
  thoroughly LP territory.
- **acid-rock .40f → .28f.** Acid rock was ordered as *more* Single-leaning than
  psychedelic rock, which inverts the real relationship. Extended-jam acid rock
  (Grateful Dead, Blue Cheer) is the least 45-compatible rock genre of the era
  and belongs beside progressive rock.
- **surf-rock .80f → .70f.** `.80` placed surf level with Jamaican sound-system
  45 culture and R&B. The Ventures charted 38 albums, Beach Boys LPs were
  consistently top-10 from 1963, and *Surfers' Choice* was a genre landmark. Surf
  stays Single-led ("Wipe Out", "Pipeline", "Surfin' U.S.A."), just not
  absolutist.

All three emerge in 1961+/1966+, so the 1960 gate window is untouched: the
post-change run is **bit-identical** to the pre-change run at 1105 projects.
Their effect is only measurable in the decade run.

Reviewed and left alone as historically sound: classical/comedy `.15` (comedy was
an LP format — Cosby, Newhart, *The First Family*), progressive-rock `.25`,
jazz `.30`, singer-songwriter `.35`, traditional-pop `.45`, bossa-nova `.45`
(*Getz/Gilberto* won Album of the Year but "Girl from Ipanema" was a monster
single), country `.65`, gospel `.70`, british-beat `.75`, soul/rnb `.80`,
ska/rocksteady/reggae `.80` (pure sound-system singles culture), garage-rock
`.85` (Nuggets is literally a singles compilation), doo-wop/teen-pop/bubblegum
`.85`–`.90`.

Two remain flagged as judgment calls, deliberately **not** changed:

- **folk `.50`** — the revival was album-heavy (Kingston Trio, Baez, PP&M) with
  occasional large singles ("Tom Dooley"); `.45` would arguably be truer. Folk is
  load-bearing in 1960 (237 decisions, 147 Album wins), so it was left rather
  than moved on a weak argument while the gate is healthy.
- **childrens `.30`** — Disney/Golden story LPs dominated, but the little
  45/78 kid-record market was large. Slightly Album-heavy, within reason.

## Outstanding validation

The 52-week run only proves 1960. The decade run is the real test, for two
reasons:

1. The 1966+ orientations from `df6d9dc` (british-blues `.85→.40`,
   soul `.95→.80`) plus the three corrections above are untested against the
   gate, since none of those genres exist in 1960.
2. Cutting Album counts carries a floor risk in the other direction. The
   `Strict1965Acceptance` gate requires `albumUnits >= 0.80x` control at 1965,
   and several D6 runs failed exactly there.

Headless Godot executable:

```
C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe
```

Reproduce the 52-week gate result:

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=52 --run=d7-format-orientation-audit-52-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --genre-market-v2-probes --catastrophic-fail-fast --gate-control-run=d6-transition-envelope-decade-control-1001
```

Next step — the decade run with the 1965 acceptance floor armed:

```powershell
$godot = 'C:\Users\grohl\Downloads\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64\Godot_v4.7-stable_mono_win64_console.exe'
& $godot --headless --path . SimTools/ChartAuditRunner.tscn -- --weeks=522 --run=d7-format-project-count-decade-522-1001 --seed=1001 --enable-genre-market-v2 --enable-artist-population-lifecycle --profile-performance --catastrophic-fail-fast --strict-1965-acceptance-gate --gate-control-run=d6-transition-envelope-decade-control-1001
```

Note that `--strict-1965-acceptance-gate` requires `--catastrophic-fail-fast` and
a completed `--gate-control-run`; the two catastrophic modes
(`--catastrophic-fail-fast` and `--catastrophic-control-preflight`) are mutually
exclusive.
