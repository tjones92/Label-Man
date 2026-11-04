# Server GC for headless audit runs. Dot-source after a runner's param block; Start-Process children inherit it.
# A late-decade world holds a ~2 GB heap and allocates ~1.8 GB per simulated week. Workstation GC spent ~16% of a
# 1969 window in gen0 pauses; server GC with 4 heaps cut that window ~15% (LR ~9%) with byte-identical outputs.
# Godot's editor binary ignores the project's runtimeconfig, so this must come from the environment. Costs ~+190 MB
# committed late, ~+600 MB early. Set LABELMAN_WORKSTATION_GC=1 to opt out (e.g. when RAM is short).
# These are process environment variables: they stay set in the calling PowerShell session after the runner ends.
if ($env:LABELMAN_WORKSTATION_GC -ne '1') {
    $env:DOTNET_gcServer = '1'
    $env:DOTNET_GCHeapCount = '4'
}
