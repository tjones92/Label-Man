# Monotonic-Penetration Candidate Resume Amendment

Status: **CURRENT MIXED STATE AUTHORIZED / OLD HASH STOP VOID / PROCEED WITH EDITS**

Date: 2026-07-18

This amendment is the controlling entry point for:

```text
SimTools/ArtistPopulationAlbumMonotonicPenetrationCorrectionHandoff.md
```

It supersedes every earlier instruction in that handoff, the telemetry replay
handoff, or the telemetry compatibility handoff that requires
`Systems/ChartManager.cs`, `SimTools/ChartAuditRunner.cs`, or
`SimTools/ArtistPopulationLifecycleProbeSuite.cs` to equal an older
pre-telemetry whole-file hash before edits.

## 1. The prior stop is void

Do **not** require:

```text
D11A...D52C  Systems/ChartManager.cs
814A...CB93  SimTools/ChartAuditRunner.cs
153B...6F8   SimTools/ArtistPopulationLifecycleProbeSuite.cs
```

Those are obsolete target hashes from before known valid post-M5 changes. They
are not a mandatory boundary. A mismatch against them is expected and is not
authority to stop.

The two prior reports that stopped solely because those three old hashes did
not match are superseded. They establish no implementation failure because no
candidate edit was attempted.

## 2. Authorized starting manifest

The following is the owner-authorized mixed starting state:

```text
B434D8507AF7DE80DCCA76FF8BD12F12D86B3F951EDC70A64AF8C1FB913A5916  Systems/ChartManager.cs
2E7302980ED0435D53B44644172ED6AEFF643C8A388679A8E6F5B3FE534A0F4C  SimTools/ChartAuditRunner.cs
25A3550436EE9DEFDEE554E3A841A15F0AA18A02C5A7FF31CFD4845D6CD7D2C9  SimTools/ArtistPopulationLifecycleProbeSuite.cs
4954724F386F2C08506F8A86EF2E7E7242CAAEABF0B0A056CC9C2DC55F77DB8A  Data/RegionalRecordData.cs
B7162551D3958CE04444F90AC6F1FC1B89145207AC3788EE72695CC3DB5E09F8  Systems/AlbumSimulator.cs
687DA937F02724D13C3F2958E109DE84CE3F213475BE12D134BD22E2AA7160DD  Systems/CompetitorManager.cs
E8AFF4842C817E82D0F750DEA4ECF40A57DB1014C24AA95574E7FC19BF370A3E  SimTools/GenreMarketV2ProbeSuite.cs
```

If these values match, **proceed with edits immediately**. Do not compare the
files with another older manifest first.

If one of these current starting hashes differs, inspect and classify the new
delta. Stop only for a genuinely new, unclassified change made after this
amendment.

## 3. Required semantic edit

In one focused implementation pass:

1. Remove the abandoned Album catalog diagnostic source path by the explicit
   symbol/code-path list in section 4 of
   `ArtistPopulationAlbumMonotonicPenetrationCorrectionHandoff.md`.
2. Preserve:
   - completed-year fail-fast identity and CSV column;
   - `FormatCompletedYearRatioState`;
   - `AdvanceMarketRevenueYear`;
   - format-memory `revisionOrdinal`;
   - immutable settlement label-tier/genre snapshots;
   - probe `64d` completed-year attribution;
   - probe `64e` birth-week protection; and
   - the disabled legacy-settlement compatibility amendment.
3. Remove telemetry-only causal formatting probe `65g`.
4. Restore the original Album sales arithmetic where telemetry had merely
   split expressions into diagnostic locals.
5. Implement the live-enabled monotonic effective Album penetration candidate
   exactly as specified in sections 2 and 3 of the correction handoff.
6. Add the one new production-backed D6 monotonic-penetration probe.

There is no intermediate whole-file hash gate between cleanup and candidate
implementation. Cleanup and the candidate may be completed in the same patch
so long as the focused diff proves the semantic boundary.

## 4. Post-edit gate

After editing:

- run the abandoned diagnostic symbol search from the correction handoff;
- inspect the focused diff;
- record a new SHA-256 manifest;
- run `git diff --check`;
- run the build and M1 fixed probes; and
- continue sequentially through M2-M5 only when each rung passes.

The remainder of
`ArtistPopulationAlbumMonotonicPenetrationCorrectionHandoff.md`—implementation
formula, prohibited surfaces, run prefixes, commands, acceptance gates, and
hard stops—remains authoritative.

## 5. Unambiguous instruction

If the authorized starting manifest matches, the next action is to edit the
source. Reporting the already-known old-hash mismatch again without attempting
the semantic cleanup and candidate implementation is noncompliant with this
amendment.
