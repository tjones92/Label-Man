# Local scene Phase 1 implementation and acceptance handoff

Status: implementation ready; runtime acceptance pending a user-coordinated game test window. No Phase 1 Godot process has been launched. Compilation passes with the five pre-existing warnings.

## Revision boundary

Branch: `codex/local-scene-phase-1`, isolated checkout `C:/Users/grohl/.codex/worktrees/local-scene-phase-0/Label-Man`.

Integrated comparator revision: `f5862fe1d0819977fa0bc0f6bf1276f084b09230`. This merges committed band work through `d1cc91b` into the completed Phase 0 branch. It includes the solo-intent revision, fame-share default 0.90, world-scope churn default, saved band slice, and yearly snapshots. Concurrent uncommitted growth/wealth work in `C:/Project/Label-Man` is not imported. The old Phase 0 controls used fame-share 0.45 and an earlier band revision; they establish replay tooling, not Phase 1 economic acceptance.

The attached directive/atlas and earlier draft are design inputs. The user's phase-by-phase implementation request supersedes their historical design-only stop instruction. The revised directive's Phase 1 identity-first sequence governs; the purge/factory changes belong to Phase 2.

## Delivered contract

- `ScenePlaceRegistry`: 31 playable US identities, 18 separate US satellite identities, six GB identities. Country, commercial region, playable catchment, and literal identity remain distinct. Names are textual synonyms only; Oakland does not become San Francisco and Milwaukee does not become Chicago.
- Domestic playable coordinates reuse the existing approximate game map. Satellite/foreign coordinates are unknown; unavailable or international road routes return false and NaN. No fabricated historical venue dates or researched population shares are introduced.
- Acts, people and labels hold optional `GeographicIdentity`: origin, working base, separate evidence, assignment provenance/version and dated base moves. New simulated acts receive procedural formation places; a legacy act receives an inferred base while its unknown hometown remains unknown. Personal birthplace remains unknown unless supplied explicitly.
- Seeded assignment uses its own keyed hash namespace and sorted place registry, never the population/global RNG. All playable cities in the existing commercial region remain eligible. Weights 1.0/1.5 are provisional qualitative placement priors, not historical genre shares, city economic weights or consumer-demand inputs.
- The typed optional fifth argument to `GenerateArtist` admits an explicit formation place without changing the existing fourth region argument. Existing solo formations, member generation, joins and recombination populate/record identity after their existing decisions. Signing never relocates an act. Existing member joins can record year-precision working-base changes; geographic feasibility belongs to later phases.
- Scene indexes contain authoritative act IDs. There is no second population owner. Removal clears the index; save restore rebuilds it after authoritative world/player objects exist.
- Public profiles separate hometown from working base; inferred bases are marked. Existing commercial `homeRegion`, HQ distribution projections, contracts, quotas, genres and member attributes retain their old readers.
- Save version 5 persists identity inline and world seed/content/assignment metadata. Legacy migration is idempotent. Unknown origins are not filled from label HQ or commercial hub. Foreign literal HQ identity retains its country despite the existing US sales projection. Newer content/assignment versions are rejected before manager restore. Real loads defer scene migration until player restoration succeeds. Existing world loading is not made transactional by this patch.

## Flag matrix

| Flags | Behavior |
|---|---|
| none / `--disable-local-scenes` | No new assignment; stored identities survive unchanged. |
| `--observe-local-scenes` | Assign/restore identity and record existing moves; no scene gameplay readers. |
| observe plus disable | Reject. |
| `--enable-local-scenes`, `--enable-scene-recruitment`, `--enable-scene-dynamics` | Reject as unimplemented later phases. |
| `--local-scene-identity-audit` | Emit separate start/end identity CSVs; does not enable observation. |
| `--local-scene-identity-check` | Opt-in fixed runtime probes in SaveLoadRoundTripRunner; never ordinary startup. |

Flag validation occurs in the first seed bootstrap autoload, before population generation. Stored metadata is preserved even when observation is disabled; all-off cleanliness means no new metadata assignment or economic change, not dropping saved identities.

## Checks completed

`dotnet build 'Label Man.csproj' -c Debug`: zero errors, five existing warnings. `git diff --check` passes. Static review confirms no new RNG draws, birth/removal owner, recruitment fallback, economic reader or schedule factory. The primary concurrent checkout was not edited.

Runtime probes are compiled but **not executed**. They cover registry/adapters, catchments, unknown/international distances, keyed replay, legacy idempotence/unknown origins, signing stability, foreign HQ versus sales proxy, unmapped places, simulated formation provenance, dated move preservation, serialization, conflicting/future flags/content and disabled metadata. They also compare the live world before/after fixtures. These checks require Godot Resources and therefore count as in-game tests.

## Pre-registered runtime acceptance

Run only after the user confirms the band simulation has released the test window. Check for active Godot/game processes before every job; run serially and use new artifact tags. Retain process exit, completion marker, revision, assembly and executable hashes, exact flags and run durations.

Use these exact common comparator flags at BOTH the integrated control revision and Phase 1:

```powershell
$common = @('--enable-genre-market-v2', '--enable-artist-population-lifecycle',
  '--enable-artist-evolution', '--enable-artist-recognition', '--enable-managers',
  '--seed-star-canopy', '--enable-cowriting', '--enable-member-axes',
  '--observe-band-life', '--enable-lineup-churn=world', '--member-fame-share=0.90')
```

1. First run fixed probes at Phase 1, then the existing world/gzip round-trip runner at 0 and 26 weeks in off and observed modes. Example, from this worktree (after Godot resources are available):

```powershell
& $godot --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=0 --local-scene-identity-check @common
& $godot --headless --path . SimTools/SaveLoadRoundTripRunner.tscn -- --seed=1001 --weeks=26 --observe-local-scenes @common
```

Require `SCENE_IDENTITY_CHECK_PASS`, `SAVELOAD_ROUNDTRIP_PASS` and exit 0. Also exercise actual player-label save restoration, including off-after-observed metadata preservation; a world-only round trip does not prove the player layer.

2. Create a separately built frozen comparator checkout at the integrated revision without changing the concurrent primary checkout. Run matched ChartAuditRunner controls, Phase 1 off, and Phase 1 observe for seeds 1001 and 2002 over 52 weeks. Use `--aggregate-only` for each. Add `--local-scene-identity-audit` only on Phase 1. Repeat seed 1001 to establish the integrated noise floor. Do not reuse the Phase 0 helper unmodified: it pins the obsolete fame share.

3. Hash every corresponding existing CSV after removing only the run prefix. Require byte identity across repeat/control/off/observe; exclude only the new `scene-identity-start/end.csv` files. Investigate any difference before tuning. Check exact week counts, zero integrity violations, authoritative population/ownership/person reconciliation, label survival/vacancies/signings, chart genre mix, singles/albums and finances. Compare world snapshots after removing only new `SceneIdentity` and inline `geography` fields; compare canonical JSON rather than gzip bytes.

4. Load an integrated legacy world under observation twice: origin unknown, base/evidence/assignment unchanged on the second load, no new acts/people/contracts or other world changes. Save the migrated world; run two identical 8-week continuations and compare all existing outputs plus identity diagnostics. Verify saved band-slice flags remain effective.

5. Shared act/person hooks also require the band branch's established two-seed decade economic comparison after fixed and 52-week checks pass. Do not substitute obsolete scalar guardrails. Phase 1 is not accepted, and Phase 2 must not begin, until the coordinated runtime results establish unchanged economic controls and save/replay behavior.
