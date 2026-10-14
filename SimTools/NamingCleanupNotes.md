# Naming cleanup notes (for the naming branch)

Collected from the first-five-years playtest (`Label-Man-playtest/Playtest/PlaytestLog.md`) and the game-design critic.
None of this is fixed on `codex/playtest-ui-ux-fixes` on purpose: song titles are hashed into hook/production and decide
which album track ships ([[song-titles-feed-track-traits]]), and name generation runs inside the seeded population, so any
change moves RNG streams and the calibrated economy. Do it in the naming branch, with a re-baseline, and read the ledger
rather than the aggregates ([[one-conversion-reshuffles-the-rng-stream]]).

Architecture to work in: `SimTools/NameGenerationLayeredArchitecture.md`, `SimTools/NameGenerationOverhaulDirective.md`,
data in `Data/Naming/` (`templates.json`, `lexicon.ontology.json`).

## 1. Repeated words in song titles

Seen in the Morning Paper: "Down at the the Firehouse Club".

Likely cause (verified by grep, not yet by a probe): `Data/Naming/templates.json` has
`blues_at_the_place` with pattern `Down at the %noun#1%`, and the noun pool it draws from
(`lexicon.ontology.json`, around line 977) holds venue entries that already carry their own article:
`"the Firehouse Club"`, `"the Riverside Theater"`, `"the Delta Festival"`, `"the Blue Note Room"`,
`"the Kingston Lounge"`. The template's "the" plus the entry's "the" gives "the the".

Fix shape: pick one owner for the article. Either strip the leading "the " from those entries, or have the template
engine collapse a duplicated adjacent word at render time. The render-time collapse is the safer general net, because the
same seam can appear wherever a template ends in an article and a lexicon entry begins with one ("of the", "in the",
"on the"). Add a regression check that renders every template against every pool entry and fails on any repeated
adjacent token.

## 2. Artist name vs. member name clashes

Group names that name a person who is not in the group. Seen: "Paulie and the Promises" is a duo of Lee Dean and
Phil Martino; there is no Paulie. Same family: "Paulie and the Teens".

Rule to enforce: a "Name and the Group" act must take its front name from a real member (the lead or the principal
songwriter). Where the name is a stage name rather than a given name, the roster and dossier should still be able to
resolve it to a member (stage name -> person). Check the other possessive and fronted forms too ("X's Y", "X with the Y").

Related generator seams from the same playtest:

- "RCA Records III": a collision suffix on a rival label's name reads as a generator artifact, not a real firm.
  Prefer a different name over a numeral; keep numerals only where the era used them.
- Cluster samey names: three "X Strings" acts in a single top 30 (The Collins / Campbell / Barnes Strings). Wants a
  per-chart-week diversity cap on a shared suffix.
- "The Beetles" (Doo-Wop). Reads as a joke on a real act. Decide whether pastiche names are wanted, and if so mark
  them as deliberate.
- Real trade names (Decca, RCA, Capitol, Columbia, Atlantic, EMI, King) mixed with invented ones on the chart: an IP
  decision for the author, not a generator bug.
