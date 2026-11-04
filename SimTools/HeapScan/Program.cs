using Microsoft.Diagnostics.Runtime;

// Heap census for a `dotnet-dump collect --type Heap` dump of a headless run (attach to the
// Godot_v4.7-stable_mono_win64 child, not the _console wrapper). Read-only over the dump.
//   dotnet run -c Release --project SimTools/HeapScan -- <dump> [--no-strings]
// Reports duplicate strings by value, then strings, collections, arrays and EMPTY arrays by the (type.field) that
// first references them. Arrays, strings and collections are deduplicated by address, so a shared instance counts once.
// Includes unreachable objects; `dotnet-dump analyze <dump> -c "dumpheap -stat -live"` gives the live total.
string path = args[0];
bool strings = !args.Contains("--no-strings");
using DataTarget target = DataTarget.LoadDump(path);
ClrRuntime runtime = target.ClrVersions[0].CreateRuntime();
ClrHeap heap = runtime.Heap;

if (strings) {
	var byValue = new Dictionary<string, (long Count, long Bytes)>(StringComparer.Ordinal);
	long stringCount = 0, stringBytes = 0;
	foreach (ClrObject obj in heap.EnumerateObjects()) {
		if (!obj.IsValid || obj.Type == null || !obj.Type.IsString) continue;
		string value = obj.AsString(4096) ?? "";
		long size = (long)obj.Size;
		stringCount++; stringBytes += size;
		var v = byValue.GetValueOrDefault(value);
		byValue[value] = (v.Count + 1, v.Bytes + size);
	}
	long wasted = byValue.Values.Sum(v => v.Bytes - v.Bytes / v.Count);
	Console.WriteLine($"strings {stringCount:N0} {stringBytes / 1e6:F1} MB; distinct {byValue.Count:N0}; duplicate bytes {wasted / 1e6:F1} MB");
	Console.WriteLine("-- top duplicated values by wasted bytes");
	foreach (var kv in byValue.OrderByDescending(kv => kv.Value.Bytes - kv.Value.Bytes / kv.Value.Count).Take(25))
		Console.WriteLine($"{(kv.Value.Bytes - kv.Value.Bytes / kv.Value.Count) / 1e6,8:F2} MB x{kv.Value.Count,9:N0}  \"{(kv.Key.Length > 70 ? kv.Key[..70] + "..." : kv.Key)}\"");
	byValue = null;
	GC.Collect();
}

var fieldStrings = new Dictionary<string, (long Count, long Bytes)>();
var fieldEmptyArrays = new Dictionary<string, (long Count, long Bytes)>();
var fieldArrays = new Dictionary<string, (long Count, long Bytes)>();
var fieldCollections = new Dictionary<string, (long Count, long Bytes)>();
var seenString = new HashSet<ulong>();
var seenArray = new HashSet<ulong>();
var seenCollection = new HashSet<ulong>();
void Add(Dictionary<string, (long, long)> d, string key, long bytes) {
	var v = d.GetValueOrDefault(key);
	d[key] = (v.Item1 + 1, v.Item2 + bytes);
}
foreach (ClrObject obj in heap.EnumerateObjects()) {
	if (!obj.IsValid || obj.Type == null || obj.Type.IsString || obj.Type.IsFree || obj.Type.IsArray) continue;
	ClrType type = obj.Type;
	foreach (ClrInstanceField field in type.Fields) {
		if (!field.IsObjectReference) continue;
		ClrObject child;
		try { child = field.ReadObject(obj.Address, false); } catch { continue; }
		if (child.IsNull || !child.IsValid || child.Type == null) continue;
		string key = type.Name + "." + field.Name;
		if (child.Type.Name != null && child.Type.Name.StartsWith("System.Collections.Generic.") && seenCollection.Add(child.Address)) {
			// The collection object plus the arrays it holds directly (entries, buckets, items).
			long bytes = (long)child.Size;
			foreach (ClrInstanceField inner in child.Type.Fields) {
				if (!inner.IsObjectReference) continue;
				ClrObject arr; try { arr = inner.ReadObject(child.Address, false); } catch { continue; }
				if (!arr.IsNull && arr.IsValid && arr.Type?.IsArray == true && seenArray.Add(arr.Address)) bytes += (long)arr.Size;
			}
			Add(fieldCollections, key + " [" + child.Type.Name + "]", bytes);
		}
		if (child.Type.IsString) {
			if (seenString.Add(child.Address)) Add(fieldStrings, key, (long)child.Size);
		} else if (child.Type.IsArray) {
			if (!seenArray.Add(child.Address)) continue;
			Add(fieldArrays, key + " [" + child.Type.Name + "]", (long)child.Size);
			ClrArray array = child.AsArray();
			if (array.Length == 0) Add(fieldEmptyArrays, key + " [" + child.Type.Name + "]", (long)child.Size);
			else if (child.Type.ComponentType?.IsString == true)
				for (int i = 0; i < array.Length; i++) {
					ClrObject element = array.GetObjectValue(i);
					if (!element.IsNull && element.IsValid && seenString.Add(element.Address)) Add(fieldStrings, key + "[]", (long)element.Size);
				}
		}
	}
}
void Print(string title, Dictionary<string, (long Count, long Bytes)> d, int n) {
	Console.WriteLine($"-- {title} (total {d.Values.Sum(v => v.Bytes) / 1e6:F1} MB)");
	foreach (var kv in d.OrderByDescending(kv => kv.Value.Bytes).Take(n))
		Console.WriteLine($"{kv.Value.Bytes / 1e6,8:F1} MB x{kv.Value.Count,10:N0}  {kv.Key}");
}
Print("strings by first owning field", fieldStrings, 35);
Print("collections (object + internal arrays) by owning field", fieldCollections, 30);
Print("arrays by owning field", fieldArrays, 30);
Print("EMPTY arrays by owning field", fieldEmptyArrays, 25);
