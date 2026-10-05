using System;
using System.Linq;

/// <summary>Explicit fail-closed gate until a historically sourced expansion is approved.</summary>
public static class GospelSongbookExpansion {
 public static void ValidateRequest(string[] args) {
  var flags=args.Where(a=>a.StartsWith("--gospel-songbook-v2=",StringComparison.Ordinal)).ToArray();
  if(flags.Length>1 || flags.Any(a=>a is not "--gospel-songbook-v2=on" and not "--gospel-songbook-v2=off"))
   throw new ArgumentException("Invalid or duplicate gospel-songbook-v2 flag");
  if(flags.Contains("--gospel-songbook-v2=on"))
   throw new InvalidOperationException("Gospel songbook v2 is gated: Directive 3 research has not established an expanded reachable canon. See SimTools/PolarGospelResearchNotes.md.");
 }
}
