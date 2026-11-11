# Stature marketing runtime review, 2026-11-06

The trial runner forced `DOTNET_gcServer=0` and `LABELMAN_WORKSTATION_GC=1`, bypassing the shared audit GC setup. It now dot-sources `AuditGcEnvironment.ps1`, which defaults to server GC with four heaps and respects an explicit workstation opt-out. It restores the caller's GC environment on exit and records the selected environment in each new request manifest.

New decade batches enable `--profile-performance` in both causal arms by default. The trial runner exposes `-ProfilePerformance` for causal-only runs; neutrality screens reject it because timing CSVs are nondeterministic. No assembly or simulation logic was changed.

## Live profile

A 40-second `dotnet-sampled-thread-time` trace was attached to the active seed-1001 control (PID 17148), without restarting it. Raw trace, Speedscope export and summary are under `SimLogs/writing-calibration/stature-marketing-decade-v2-1001-control-cpu*`.

Selected inclusive sampled-time shares:

| Method | Percent |
|---|---:|
| CompetitorManager.OnWeekEndedCore | 41.16 |
| ProfessionalWritingMarket.Advance | 20.68 |
| CompetitorManager.ProcessReleaseProgramme | 17.45 |
| LabelLifecycleManager.ProcessMonth | 13.32 |
| CompositionCatalogService.WriteOfficeSong | 10.09 |
| ProfessionalWritingMarket.RunReturningClients | 9.59 |
| ProfessionalWritingMarket.OwnedDelivery | 8.72 |
| StatureObservation.WriteWeek | 7.33 |

These inclusive measurements overlap and cannot be summed. The sample covers one brief late-decade window in one seed; it is not a whole-decade attribution or a controlled GC comparison. It points toward writing-market and release-programme scans as larger optimization targets than stature alone. No speedup is claimed from this sample.

The existing workers already loaded the old PowerShell scripts. Editing those files cannot change their current GC mode or the code they will execute for their treatment arms. They were left running; the correction and built-in profiling apply to newly started workers. An attached CPU profile of the existing control is available now.

## Validation

Both edited PowerShell scripts parse successfully. The shared GC helper selects server mode with four heaps in a fresh environment. All five `test_summarize_stature_marketing_trial.py` tests pass. New profiled runs have not yet been launched, and the GC speedup has not been remeasured for this configuration.
