# Scoped startup caches

Launch roster allocation now caches geographic eligibility per label while continuing to filter unsigned supply and player holds on every round. Room scheduling caches bill keys and performer lineups within one scheduling call. The caches expire before subsequent world changes; neither population nor historical coverage is reduced.

Use `--disable-startup-caches` for the original path and `--profile-startup` for timings. `StartupSnapshotRunner.tscn` compares the serialized world, initial appointment order and next global RNG draw. Matched cached/control runs on seeds 1001 and 1002 produced identical values for all three.

| Stage | Seed 1001 control / cached | Seed 1002 control / cached |
|---|---:|---:|
| Roster allocation | 22.170 / 5.534 s | 8.534 / 3.652 s |
| Historical room scheduling | 9.118 / 11.784 s | 8.457 / 9.363 s |
| Bootstrap through live room calendar | 54.532 / 40.376 s | 32.964 / 29.880 s |

These are headless Godot 4.7 Mono Debug measurements on this host, not process-to-first-frame measurements. Host load and warming were not controlled; one cached run overlapped another validation startup. Roster allocation consistently improved. No measured room-history speedup is claimed. Population generation, history and all economic defaults remain intact.

Validation logs and snapshot JSON are in ignored `SimLogs/startup-control-{seed}.*` and `SimLogs/startup-cached-{seed}.*`. Reproduce with:

```powershell
& $godot --headless --path . SimTools/StartupSnapshotRunner.tscn -- --seed=1001 --profile-startup --disable-startup-caches --snapshot-tag=startup-control-1001
& $godot --headless --path . SimTools/StartupSnapshotRunner.tscn -- --seed=1001 --profile-startup --snapshot-tag=startup-cached-1001
```
