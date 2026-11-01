# Celebrity fanbase Phase 1 notes: spotlight writes personal recognition

Status: built 2026-11-01, off unless named. Design: `CelebrityFanbaseDesign.md` §5, §10 Phase 1. This phase uses
directive amendment §0.3 (member spotlight may write canonical personal recognition), recorded in
`MediaAtmosphereDirective.md` §12.4.

## What is built

| Piece | Where | What it does |
|---|---|---|
| Switches | `MediaMemberFame.cs` | `--observe-member-fame` (shadow): deposits go to a media ledger only. `--media-member-fame` (causal): writes `Musician.personalRecognition`. Causal requires `--causal-media-atmosphere` and artist recognition. |
| TV deposit | `MediaAtmosphereService.MemberFame.cs`, `MemberFameAiring`, called from the weekly tally | Each airing of a group (two or more active members): registered viewers ÷ population, split by camera share. |
| Photo deposit | `MemberFamePhoto`, called from `PhotographMember` (AI and player sessions) | Registered readers ÷ population, to the member photographed. An exclusive session is the cover (3×). |
| Ledger | `MediaSaveData.MemberFame`, by personId | Shadow: the would-be stock beside the real one. Causal: the media-earned part of the real stock. Decays on recognition's clock. |
| Camera promises | `MediaSaveData.CameraPromises`, `PromiseCamera`, `MediaFans.CameraShares(…, promised)` | Causal only. *SpotlightThem* (billing) adds camera weight 1.0 while the act keeps that billing. *SoloSingleYes* adds it for a year. Neither mints fame in the office (audit finding 10). With the switch off, both verbs keep their old +0.01 / +0.008. |
| Telemetry | `MEDIA_MEMBER_FAME` (year end), `MEDIA_MEMBER_SPINOUT` (per spinout of a member with media-earned fame) | Deposits, applied gain by source, members carried over the 0.05 solo bar, solo-viable members with and without media, spinouts and spotlight-cause band events. |

### Arithmetic, as built

- **Registration** (§3.1): fresh reach × r0 0.25 × performance (0.6 Breakdown … 1.6 Electrifying) × novelty (1 → 0.4
  with national dates already seen) × attention × audience fit. Fit is weighted by the airing's young and adult viewers.
  The constants are the ones `MediaFollowing` uses. Attention is read from the followings, so it is 1 unless
  `--observe-following` also runs.
- **Member gain** = registered ÷ population × camera share. It goes in with the act's diminishing returns
  (stock + raw × (1 − stock)). It is extra to the act's own recognition and its 0.90 member share.
- **Photographs**: circulation × r0 × the outlet's fit for the genre, × 3 for a cover, ÷ population. A million-reader
  feature is about 0.001 of the country, so magazines are a small national deposit beside television.
- **Solo acts are excluded.** Their name is already the person's.
- A promised member's camera weight goes up by 1.0, about a lead singer's pull again. In the fixture a quiet drummer
  goes from 23% to 34% of the frame.

**Fixtures** (`MediaAtmosphereChecks`, 198 checks pass at `--seed=1001`, 10 of them new):
- a first ordinary Sullivan spot registers 0.014 of the country, split among the faces;
- an electrifying night registers more, and a twentieth date or a crowded week registers less;
- a cover is three times a feature;
- diminishing returns;
- a promise raises the member's camera share, and shares still sum to one;
- four shadow weeks credit 335 group members, all of them real members of the act named;
- shadow mode makes no promise;
- the ledger survives a save round trip;
- causal deposits reach 228 members' real stock, none below opening plus earned;
- billing promises the camera without minting fame, and the promise lapses when the billing changes.

## Screens

**Shadow inertness**, 104 weeks, ref flags (`--aggregate-only`) + `--observe-member-fame`, against `ref-f50b9a4-<seed>-104`:
- seed 1001: 106/106 CSVs byte-identical. The census files need `--contact-network-audit` (`mf1obsc`) and
  `--log-band-life-members` (`mf1obsm`). The second includes every member's `personalRecognition` by year.
- seed 2002 (`mf1obs`): 98/98 written CSVs identical; the 8 census files were not requested.

1960 shadow sizes (`MEDIA_MEMBER_FAME`):

| | Seed 1001 | Seed 2002 |
|---|---:|---:|
| TV deposits / applied | 2,306 / 2.03 | 2,219 / 2.00 |
| Photo features + covers / applied | 32 / 0.11 | 30 / 0.09 |
| Members credited | 344 | 331 |
| Carried over the 0.05 solo bar by media | 16 | 12 |
| Solo-viable members, without / with media | 12 / 10 | 15 / 15 |
| Top member's media gain in the year | 0.025 | 0.027 |

- Photographs are about 5% of the total; television carries member fame.
- **Crossing the bar is not becoming solo-viable.** The gate also needs a member who wants to go and a 50% share of the
  group's recognition. Shadow adds to every member at once, so shares barely move, and the count can even fall.
- No spinouts in 1960, in either arm, as expected this early.

**Causal screen**, 1963-66 resume of `media-p3-1001-1963` (seed 1001, `--aggregate-only`). Control `mf1-c`:
`--causal-media-atmosphere --observe-member-fame`. Its concentration is byte-identical to `tvbook-m`, so causal mode
plus the shadow stays inert on the resume too. Treatment `mf1-t`: `--causal-media-atmosphere --media-member-fame`.

Band life. Years are the band-life pass, so the "1963" pass runs at the 1963/64 boundary, after a year of deposits:

| | Control spinouts | Treatment spinouts | Control spotlight departures | Treatment spotlight departures | Ultimatums c / t |
|---|---:|---:|---:|---:|---:|
| 1963 | 3 | 1 | 10 | 10 | 9 / 9 |
| 1964 | 8 | 4 | 10 | 8 | 8 / 10 |
| 1965 | 6 | 4 | 12 | 9 | 13 / 8 |
| 1966 | (next pass) | | 18 | 11 | 18 / 21 |
| Total spinouts 1963-65 passes | 17 | 9 | | | |

(Lineup-events count, both arms sharing the five 1962 spinouts that ran before the resume's first deposit.)

- **Member fame lowers spinouts.** That is the opposite of the design's expectation (§5: "solo spinouts get easier for
  camera-loved members").
- **The cause is arithmetic, not noise.** The camera spreads fame more evenly than the chart's member attribution:
  - chart (`MusicianRecognitionService.VisibilityWeight`): 0.20 + 0.40 lead + 0.15 leader + 0.35 × stage presence;
  - camera (`MediaFans.CameraShares`): 0.4 + stage presence + 0.7 lead ± youth + 0.9 × keyed draw.
  - In a typical four-piece the lead banks about 44% of chart-earned member fame but holds about 36% of the camera.
  - So TV fame lowers the front person's share of the group's recognition, which is what the solo gate (50%) and
    spotlight strain read.
- The shadow telemetry already showed this. In control, solo-viable members without / with media were 19/14 and 31/29
  in 1963-64. No spinout in either arm was made by media: every spinout member was over the bar without it.

Economy, for a catastrophe only (single seed, reshuffle-sized limits):

| | 1964 | 1965 | 1966 |
|---|---:|---:|---:|
| Single units, t vs c | −0.01% | −0.42% | −0.03% |
| Album units, t vs c | +0.02% | +0.98% | +0.04% |
| CR4 (pts) | −1.3 | −4.5 | −7.3 |
| Major-family chart share (pts) | −6.6 | −7.5 | −7.4 |

- Units are within the ±1% limits.
- **Major-family share falls about 7 points** from 1964. Earlier arms on the same world moved it:
  - recognition (`mrec-c`): +3.8 / −0.5 / +2.5;
  - permuted demand (`dmd-perm`): −3.2 / −1.3 / −0.2;
  - targeted demand (`dmd-c`): −2.4 / −6.8 / −10.5.
- That is larger than the reshuffle-only arm. One seed can't tell whether it comes from the changed lineups or from
  reshuffle. The new fame lands on 700-1,200 group members a year, so band life diverges from 1964 on.

### Sharper camera (author's decision, 2026-11-01)

TV member fame is now deposited by camera share squared, renormalized (`MediaMemberFame.FameShares`,
`CameraFameExponent` 2). In the fixture's typical four-piece, the lead holds 35% of the frame and takes 46% of the fame,
just above the chart's 44%. Photographs are unchanged: they already pick one member. Checks: 199 pass.

Re-run `mf1-t2` against the same control `mf1-c`:

| | Control | Flat camera `mf1-t` | Sharper camera `mf1-t2` |
|---|---:|---:|---:|
| Spinouts, 1963 / 1964 / 1965 passes | 3 / 8 / 6 (17) | 1 / 4 / 4 (9) | 3 / 6 / 5 (14) |
| Solo-viable without / with media, 1963 / 1966 | (shadow) 19/14, 35/35 | 19/14, 37/35 | 19/24, 38/46 |
| Major-family share, 1964 / 1965 / 1966 | 59.3 / 62.1 / 66.1% | −6.6 / −7.5 / −7.4 pts | −4.2 / −6.5 / −8.0 pts |
| CR4, 1964 / 1965 / 1966 | 23.0 / 24.3 / 29.4% | −1.3 / −4.5 / −7.3 pts | −0.8 / −2.5 / −6.7 pts |
| Single units vs control | — | −0.01 / −0.42 / −0.03% | −0.11 / −0.42 / −0.38% |
| Album units vs control | — | +0.02 / +0.98 / +0.04% | +0.27 / +0.97 / +0.75% |

- **Media now makes members solo-viable rather than fewer.** It adds 5 in 1963 and 8 in 1966.
- Spinouts are back near control. Kenny Adams and Hank Bell leave on time again. The rest is the reshuffled lineups.
- **The Major-share fall does not depend on the camera rule.** It is about −7 points either way, so it does not come from
  spinouts. The candidates are the other readers of personal recognition:
  - ego growth;
  - member wealth spending;
  - the substance hazard;
  - pool careers;
  - spotlight strain;
  - reshuffle. Band life diverges for 700-1,200 members a year.
- One seed can't separate these. The other saved world, `asc-2002-1963`, could carry a second pair.
- Units stay within ±1%. Albums in 1965 are at +0.97% in both arms.

### Second world: `asc-2002-1963` (seed 2002, sharper camera)

Control `mf1-c2002` (`--causal-media-atmosphere --observe-member-fame`), treatment `mf1-t2002` (`--media-member-fame`):

| | Control | Treatment | t − c |
|---|---:|---:|---:|
| Spinouts, 1963 / 1964 / 1965 passes | 4 / 6 / 10 (20) | 3 / 6 / 11 (20) | 0 |
| Major-family share, 1964 / 1965 / 1966 | 51.9 / 44.6 / 57.3% | 44.8 / 54.2 / 53.1% | −7.2 / +9.6 / −4.2 pts |
| CR4, 1964 / 1965 / 1966 | 20.1 / 22.6 / 20.3% | 22.0 / 23.1 / 22.4% | +1.8 / +0.5 / +2.1 pts |
| Single units | | | +1.1 / −0.2 / +1.7% |
| Album units | | | −3.0 / +0.6 / −3.7% |
| Solo-viable without / with media (treatment) | | 31/32, 38/45, 31/42 | |

- **Spinouts are unchanged** (20 and 20). Media adds 1-11 solo-viable members a year, as on seed 1001.
- **Major share swings with both signs.** The control alone moves 51.9 → 44.6 → 57.3% year to year. So the −7 points on
  seed 1001 is not a consistent effect of member fame. Read it as reshuffle until the end-of-branch decade A/B says otherwise.
- **Units:** singles +1.7% and albums −3.7% in 1966 are outside the ±1% limits on this world. On seed 1001 both stayed
  inside. Personal recognition has no direct route to LP decisions, and no reshuffle reference exists for this world, so
  this is also a decade A/B question.
- **Verdict:** the screen finds no catastrophe and no consistent economic direction across two worlds. Member fame does
  what the design asked: camera-loved members become known and solo-viable, without an excess of spinouts. Phase 1 can be
  committed off by default. The §12.5 verdict waits for the paired decade A/B at the branch end.

### Open (resolved above): what member fame should do to spinouts

As built, television makes the whole band known, and that dilutes the front person's lead. Options:
1. **Keep it.** TV made all four Beatles and all four Monkees names. Fewer one-face groups is a defensible reading.
2. **A sharper camera.** Deposit by camera share squared (renormalized), so the face the camera loves takes most of it.
   That is the Davy Jones and Mark Lindsay story the design wanted.
3. **Fame only where the camera beats the chart.** Deposit only the part of a member's camera share above their
   visibility share. A camera-loved non-lead gains; the lead doesn't lose.
