# Professional writing, stage one

The following stage is implemented behind `--professional-writing-market`;
see [ProfessionalWritingMarket.md](ProfessionalWritingMarket.md) for finite supply,
partnerships, contracts and fees. This document describes the isolated first stage.

Use `--professional-writing` for writer-conditioned professional catalogue quality.
Add `--enable-team-writing` to use the existing standing pairs. Both are opt-in;
the ordinary-game baseline and existing experiments retain their previous behavior.

The existing 120 saved professional writers supply melody, lyric and hook craft.
A song's latent score combines standardized craft (weight .6), assignment genre
fit, and its existing private-stream song variation (weight .8). The squared
weights sum to one; the song term retains 64% of the original latent variance.
Genre fit favors the writer's primary and secondary genres; versatility reduces
the penalty outside them. Teams use the strongest craft on each axis less the
existing similarity-dependent friction. Credit remains split equally. There is
no extra team bonus on top of this conditioning.

At fresh-world initialization, scores **rank-allocate the existing values within
each genre**, separately for composition, melody, lyrics and hook. This preserves
the exact marginal quality distributions, including tails, and catalogue size.
It changes correlations between axes and writers; it does not preserve the
distribution of the weighted aggregate craft score or of selected releases.
That is a deliberate limitation to measure in subsequent world comparisons.

New office songs receive the conditional score directly around the existing
quality means and spreads. Their finite-sample distribution is approximate;
teams, clipping and the active writer mix can move it. Standing pairs continue
to receive joint credit on their keyed share of these new songs. Existing songs
are not repeatedly reconditioned on load. Song values, writer abilities, team
links, and catalogue random state already persist through the existing save.
Use the same switches when resuming; this development slice does not introduce
a saved policy version or migrate old songs into the new distribution.

Run `SimTools/SaveLoadRoundTripRunner.tscn` with:

```
--seed=1001 --professional-writing --enable-team-writing --professional-writing-check
```

The check covers exact marginal preservation, repeated writer craft, opposing
specialists, overlapping outcomes, genre mismatch, order independence, bounded
new-song output, conserved credit and ongoing team output. Use `--weeks=0` with
the feature switches and without the check switch for the world round-trip.

Next stages remain finite workload and delivery, imperfect demo assessment,
repeat assignments, and contracts/fees. The $150 player action currently grants
access to an existing catalogue song; this change does not turn that action into
a new commissioned delivery. No exclusivity or privileged label access is added.
Before promoting this to the working bundle, compare fully followed repeat-career
cohorts and leading-act/owner-group concentration across multiple seeds, alongside
quality, genre, team and material-source distributions.
