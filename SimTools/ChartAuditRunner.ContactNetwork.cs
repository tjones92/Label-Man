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
		using (var writer = new StreamWriter(Path.Combine(dir, $"{runName}-house-bands.csv"))) {
			writer.WriteLine("roomId,personId,shows,lastDay");
			foreach (var h in LocalSceneRoomService.HouseStanding.OrderBy(h => h.RoomId, StringComparer.Ordinal).ThenBy(h => h.PersonId, StringComparer.Ordinal))
				writer.WriteLine($"{h.RoomId},{h.PersonId},{h.Shows},{h.LastDay}");
		}
		using (var writer = new StreamWriter(Path.Combine(dir, $"{runName}-session-regulars.csv"))) {
			writer.WriteLine("labelId,personId,sessions,lastYear");
			foreach (var r in SessionEmploymentService.Regulars.OrderBy(r => r.LabelId, StringComparer.Ordinal).ThenBy(r => r.PersonId, StringComparer.Ordinal))
				writer.WriteLine($"{r.LabelId},{r.PersonId},{r.Sessions},{r.LastYear}");
		}
		using (var writer = new StreamWriter(Path.Combine(dir, $"{runName}-carried-covers.csv"))) {
			writer.WriteLine("recordId,artistId,songId,carrierId,viaArtistId,source,year");
			foreach (var c in RepertoireInfluenceService.Choices)
				writer.WriteLine($"{c.RecordId},{c.ArtistId},{c.SongId},{c.CarrierId},{c.ViaArtistId},{c.Source},{c.Year}");
		}
		using (var writer = new StreamWriter(Path.Combine(dir, $"{runName}-work-moves.csv"))) {
			writer.WriteLine("year,personId,from,to,contactId,miles,cost,destinationWorkers,homeWorkers");
			foreach (var m in WorkMigrationService.Log)
				writer.WriteLine(FormattableString.Invariant($"{m.Year},{m.PersonId},{m.From},{m.To},{m.ContactId},{m.Miles:F0},{m.Cost:F2},{m.DestinationWorkers},{m.HomeWorkers}"));
		}
		using (var writer = new StreamWriter(Path.Combine(dir, $"{runName}-scene-programs.csv"))) {
			writer.WriteLine("programId,roomId,sponsorId,sponsorTier,player,startDay,throughDay,cost");
			foreach (var p in SceneEcosystemService.Programs) {
				var l = ChartManager.Instance?.GetLabelById(p.SponsorId);
				writer.WriteLine(FormattableString.Invariant($"{p.Id},{p.RoomId},{p.SponsorId},{l?.tier},{(l?.isPlayerOwned == true ? 1 : 0)},{p.StartDay},{p.ThroughDay},{p.Cost}"));
			}
			foreach (var m in SceneEcosystemService.Moves)
				writer.WriteLine($"#move,{m.ArtistId},{m.SponsorId},{m.FromPlaceId},{m.ToPlaceId},{m.Status},{m.ArrivalDay},{m.Cost}");
		}
		int carrying = 0, carried = 0;
		foreach (var a in ArtistManager.Instance.GetAllArtists())
			foreach (var m in a.members ?? new()) if (m.isActive && m.carriedSongs?.Count > 0) { carrying++; carried += m.carriedSongs.Count; }
		GD.Print($"REPERTOIRE_INFLUENCE_CENSUS learned={RepertoireInfluenceService.SongsLearned} considered={RepertoireInfluenceService.CarriedConsidered} " +
			$"chosen={RepertoireInfluenceService.CarriedChosen} arrangements={RepertoireInfluenceService.ArrangementsCarried} " +
			$"activeMembersCarrying={carrying} songsCarried={carried}");
		GD.Print($"WORK_MIGRATION_CENSUS considered={WorkMigrationService.Considered} declined={WorkMigrationService.Declined} " +
			$"unaffordable={WorkMigrationService.Unaffordable} moved={WorkMigrationService.Moved}");
		GD.Print($"AI_SCENE_INVESTMENT_CENSUS programs={SceneEcosystemService.AiPrograms} allPrograms={SceneEcosystemService.Programs.Count} " +
			$"moveAttempts={SceneEcosystemService.AiMoveAttempts} moves={SceneEcosystemService.AiMoves} " +
			$"arrived={SceneEcosystemService.Moves.Count(m => m.Status == SceneMoveStatus.Arrived)} cancelled={SceneEcosystemService.Moves.Count(m => m.Status == SceneMoveStatus.Cancelled)}");
		var edges = ContactNetworkService.Edges;
		GD.Print($"CONTACT_NETWORK_CENSUS edges={edges.Count} bandmate={edges.Count(e => e.Kinds.HasFlag(ContactKind.FormerBandmate))} " +
			$"session={edges.Count(e => e.Kinds.HasFlag(ContactKind.Session))} fallout={edges.Count(e => e.Fallout)} " +
			$"contactHires={ContactNetworkService.ContactHires} crewed={SessionEmploymentService.RecordsCrewed} uncrewed={SessionEmploymentService.RecordsUncrewed} " +
			$"sessionPlayers={SessionEmploymentService.Accounts.Select(a => a.PersonId).Distinct().Count()} " +
			$"house={edges.Count(e => e.Kinds.HasFlag(ContactKind.HouseBand))} houseBackers={LocalSceneRoomService.HouseStanding.Select(h => h.PersonId).Distinct().Count()} " +
			$"houseShows={LocalSceneRoomService.HouseStanding.Sum(h => h.Shows)} houseRegulars20={LocalSceneRoomService.HouseStanding.Count(h => h.Shows >= 20)} " +
			FormattableString.Invariant($"meanCrewScore={(SessionEmploymentService.RecordsCrewed > 0 ? SessionEmploymentService.CrewScoreSum / SessionEmploymentService.RecordsCrewed : 0):F4}"));
	}
}
