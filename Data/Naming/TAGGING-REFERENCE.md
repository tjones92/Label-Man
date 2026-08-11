# Word Tagging Reference

How to tag words so the Layer-2 constraint templates can find them and keep titles coherent. A word
group in a lexicon JSON file looks like:

```json
{ "pos": "noun", "tags": ["celestial", "dreamy", "poetic"], "words": ["star", "moon", "comet"] }
```

Every word in `words` inherits every tag in `tags`. At load, each tag is sorted onto one of the five
axes below (the ontology figures out which axis from the tag name — you don't label the axis). Put a
word in its own group whenever it needs a different tag mix.

**Minimum useful tagging = 1 DOMAIN + 1 MOOD.** Register/Era/Locale are optional.

---

## 1. DOMAIN — what the word is *about* (pick 1+, hierarchical)

A filter for a **parent** matches all its children, so `filter: "nature"` catches `celestial`,
`nautical`, etc. Tag the most specific leaf that fits.

| Parent | Leaves (use these as tags) |
|---|---|
| **nature** | `celestial` `nautical` `weather` `terrain` `flora` `fauna` |
| **human** | `body` `romance` `kin` `emotion` `identity` |
| **place** | `urban` `rural` `regional` `domestic` `mythic` |
| **motion** | `travel` `dance` `vehicle` |
| **time** | `diurnal` `seasonal` `temporal` |
| **spirit** | `faith` `cosmic` `mystical` |
| **material** | `gem` `luxury` `grit` `candy` `mechanical` |
| **social** | `party` `conflict` `vice` `protest` |
| **abstract** | `virtue` `fate` `nonsense` |

## 2. MOOD — what it *feels* like (pick 0–2; drives mood-coherence)

Grouped into four clusters. Words pair well within a cluster and along the bridges; mixing across
clusters (e.g. `serene` + `aggressive`) is rejected in multi-word titles.

| Cluster | Moods |
|---|---|
| **TENDER** | `romantic` `wistful` `melancholy` `serene` `nostalgic` `dreamy` |
| **BRIGHT** | `joyful` `playful` `cheeky` `absurd` |
| **HARD** | `defiant` `aggressive` `gritty` `restless` `ominous` |
| **ELEVATED** | `elegant` `grand` `spiritual` `earnest` |

Good cross-cluster bridges (idiomatic): `gritty↔earnest` (folk/blues), `grand↔ominous` (prog/metal),
`spiritual↔earnest` (gospel/soul), `restless↔dreamy` (psych), `wistful↔nostalgic` (trad pop).

## 3. REGISTER — diction level (pick 0–1)

`slang` · `plain` · `poetic` · `ornate` · `archaic` · `formal`  (low → high formality)

## 4. ERA — decade idiom (pick 0–1; omit = timeless)

`timeless` (default, omit) · `early60s` (≤1963) · `mid60s` (1962–66) · `late60s` (≥1966) ·
`emerging:1968` (hard floor — never appears before that year)

## 5. LOCALE — culture/orthography (pick 0–1; omit = neutral)

`us` · `uk` · `portuguese` · `spanish` · `jamaican`  (gates the word to matching-orthography genres)

---

## Notes & gotchas

- **Nouns are singular lemmas.** The engine pluralizes (`star`→`stars`) via `%noun:pl%`. Don't tag
  already-plural words (`kisses`) into pools that get pluralized — you'll get "kisseses".
- **Verbs are base lemmas.** The engine conjugates: `run`→`running`/`ran`. Tag verbs by MOOD.
- **Anything not on an axis** (e.g. a style tag like `psych`, or a demographic like `male`) is kept
  as a plain tag and still works as an exact-match filter — it just isn't hierarchical or mood-aware.
- **Where to put words:** curated pools → `lexicon.ontology.json`; bulk re-tagging of existing base
  words → `lexicon.ontology.base.json`; names → `lexicon.people.json`. Tuner edits land in
  `lexicon.user.json`. All are merged at load.
- **In NameLab:** select a word to see its tags in the tags box, edit them, and hit **Retag** to apply
  (replaces all of that word's entries with one newly-tagged one). **Delete** removes every variant.
