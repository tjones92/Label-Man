using System;
using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
	/// <summary>--contact-network-audit: the edge list, session work and label regulars at the end of the run.</summary>
	private void WriteContactNetworkCensus() {
		if (!OS.GetCmdlineUserArgs().Contains("--contact-network-audit")) return;
		string dir = ProjectSettings.GlobalizePath("res://SimLogs");
		using (var writer = new StreamWriter(Path.Combine(dir, $"{runName}-contact-edges.csv"))) {
			writer.WriteLine("a,b,kinds,firstYear,lastYear,jobs,fallout");
			foreach (var e in ContactNetworkService.Edges.OrderBy(e => e.A, StringComparer.Ordinal).ThenBy(e => e.B, StringComparer.Ordinal))
				writer.WriteLine($"{e.A},{e.B},{e.Kinds.ToString().Replace(", ", "+")},{e.FirstYear},{e.LastYear},{e.Jobs},{(e.Fallout ? 1 : 0)}");
		}
		using (var writer = new StreamWriter(Path.Combine(dir, $"{runName}-session-work.csv"))) {
			writer.WriteLine("personId,year,sessions,pay,basePlaceId,inAct");
			foreach (var a in SessionEmploymentService.Accounts.OrderBy(a => a.Year).ThenBy(a => a.PersonId, StringComparer.Ordinal)) {
				var m = ArtistManager.Instance?.GetMusician(a.PersonId);
				writer.WriteLine(FormattableString.Invariant($"{a.PersonId},{a.Year},{a.Sessions},{a.Pay},{m?.geography?.basePlaceId},{(m?.isActive == true ? 1 : 0)}"));
			}
		}
		using (var writer = new StreamWriter(Path.Combine(dir, $"{runName}-session-regulars.csv"))) {
			writer.WriteLine("labelId,personId,sessions,lastYear");
			foreach (var r in SessionEmploymentService.Regulars.OrderBy(r => r.LabelId, StringComparer.Ordinal).ThenBy(r => r.PersonId, StringComparer.Ordinal))
				writer.WriteLine($"{r.LabelId},{r.PersonId},{r.Sessions},{r.LastYear}");
		}
		var edges = ContactNetworkService.Edges;
		GD.Print($"CONTACT_NETWORK_CENSUS edges={edges.Count} bandmate={edges.Count(e => e.Kinds.HasFlag(ContactKind.FormerBandmate))} " +
			$"session={edges.Count(e => e.Kinds.HasFlag(ContactKind.Session))} fallout={edges.Count(e => e.Fallout)} " +
			$"contactHires={ContactNetworkService.ContactHires} crewed={SessionEmploymentService.RecordsCrewed} uncrewed={SessionEmploymentService.RecordsUncrewed} " +
			$"sessionPlayers={SessionEmploymentService.Accounts.Select(a => a.PersonId).Distinct().Count()}");
	}
}
