# Package H: state before changing derivation

October 4, 2026. This inspection precedes the new fixed-world measurements.

| Composition source | Existing origin of variation | Composition variation at authoring |
| --- | --- | --- |
| Act-authored originals | Genre fallback/archetype template, demo/recording taxonomy, lyric/vocal/production modifiers and content fields | No persistent `repertoireVariation` is assigned by `AttachArtistOriginal`; same inputs can collide |
| Seeded catalogs/songbooks | `RepertoireTaxonomy.Assign` stores ID-keyed independent uniform offsets on all ten axes: demand ±0.025, identity ±0.020; selected authored forms, density and vocal presence add differences | Yes, for assigned songbook/recent-hit/native supply |
| Scouted/imported/professional material | Scouting reuses the existing composition; professional originals have no songbook assignment, and imported material retains supplied taxonomy/content | No universal authoring offset; inherited assigned material retains its existing offsets |
| Covers | `PolarCoverResolver` searches archetypes and re-resolves pace/mood/genre with the composition's stored offsets and content modifiers; recording caches store completed shapes | No new composition; variation belongs to the covered composition |

The old jitter is shared in magnitude across archetypes, but is independently keyed for each axis and each assigned composition. It is not restricted to the first two axes. Its standard deviation is about 0.0144 on demand axes and 0.0115 on identity axes before constraints/modifiers. The reported roughly 0.11/0.05 vocal spreads cannot arise from this jitter alone. Vocal/instrumental masking and differing recording modifiers contribute; the new measures separate distinct compositions from repeated recordings.

H v1 adds a separate deterministic world-seed/composition-ID offset on every creation path, demand ±0.012 and identity ±0.008. H on uses this smaller offset instead of stacking it on the old songbook jitter; H off retains the legacy jitter and original shapes. Every composition stores the offset in either mode. Old saves remain H off and receive deterministic backfill for future opt-in resolutions. Their stored recording caches are preserved. A new world's version is saved; CLI `--composition-shape-v1=off` supplies the A+B control. Covers retain the offset while choosing a different archetype, pace, mood and genre. Instrumental taxonomy and intrinsically instrumental templates enforce zero VocalPower, VocalNuance and LyricDelivery in H on.
