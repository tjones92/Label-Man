param(
    [Parameter(Mandatory)][string]$Checkpoint,
    [int]$Week = 13,
    [int]$Limit = 40,
    [switch]$FailOnDifferences
)
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
public static class StatureSavedWorldDiff {
    static JsonDocument Read(string path) {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        return JsonDocument.Parse(gzip);
    }
    public static void Run(string path, int week, int limit, bool fail) {
        string name = $"world-week-{week:D3}.json.gz";
        using var left = Read(Path.Combine(path, name));
        using var right = Read(Path.Combine(path, "resumed-" + name));
        int count = 0, sets = 0;
        Compare(left.RootElement, right.RootElement, "world", ref count, ref sets, limit);
        Console.WriteLine($"Different leaves={count}; equal reordered sets={sets}");
        if (fail && count != 0) throw new InvalidOperationException($"World parity failed: {count} differing leaves at week {week}.");
    }
    static void Compare(JsonElement a, JsonElement b, string path, ref int count, ref int sets, int limit) {
        if (a.ValueKind == b.ValueKind && a.ValueKind == JsonValueKind.Object) {
            var remaining = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var p in b.EnumerateObject()) remaining.Add(p.Name, p.Value);
            foreach (var p in a.EnumerateObject()) {
                if (remaining.Remove(p.Name, out var other)) Compare(p.Value, other, path + "." + p.Name, ref count, ref sets, limit);
                else Report(path + "." + p.Name, "present", "missing", ref count, limit);
            }
            foreach (var p in remaining) Report(path + "." + p.Key, "missing", "present", ref count, limit);
        } else if (a.ValueKind == b.ValueKind && a.ValueKind == JsonValueKind.Array) {
            if (path.StartsWith("world.Media.TopSellerIn.", StringComparison.Ordinal) ||
                (path.StartsWith("world.Competitor.IndependentDistributors[", StringComparison.Ordinal) &&
                 path.EndsWith(".clientLabelIds", StringComparison.Ordinal))) {
                var x = new HashSet<string>(); var y = new HashSet<string>();
                foreach (var e in a.EnumerateArray()) x.Add(e.GetString());
                foreach (var e in b.EnumerateArray()) y.Add(e.GetString());
                if (x.Count == a.GetArrayLength() && y.Count == b.GetArrayLength() && x.SetEquals(y)) {
                    if (a.GetRawText() != b.GetRawText()) { sets++; Console.WriteLine($"Equal reordered set: {path}"); }
                    return;
                }
            }
            int n = Math.Min(a.GetArrayLength(), b.GetArrayLength());
            if (a.GetArrayLength() != b.GetArrayLength()) Report(path + ".Length", a.GetArrayLength().ToString(), b.GetArrayLength().ToString(), ref count, limit);
            var leftItems = a.EnumerateArray(); var rightItems = b.EnumerateArray();
            for (int i = 0; i < n; i++) {
                leftItems.MoveNext(); rightItems.MoveNext();
                Compare(leftItems.Current, rightItems.Current, path + $"[{i}]", ref count, ref sets, limit);
            }
        } else if (a.GetRawText() != b.GetRawText()) Report(path, a.GetRawText(), b.GetRawText(), ref count, limit);
    }
    static void Report(string path, string a, string b, ref int count, int limit) {
        if (++count <= limit) Console.WriteLine($"{path}: {a} vs {b}");
    }
}
'@
[StatureSavedWorldDiff]::Run((Resolve-Path -LiteralPath $Checkpoint).Path, $Week, $Limit, $FailOnDifferences.IsPresent)
