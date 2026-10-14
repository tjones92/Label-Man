using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;

public partial class ChartAuditRunner {
	private StreamWriter sessionSupplyWriter;
	private static readonly string[] SessionSupplyCities = {
		"new_york", "los_angeles", "chicago", "nashville", "philadelphia", "memphis", "detroit", "new_orleans", "atlanta" };

	/// <summary>--session-supply-audit: each week, for the recording centres, how many acts are based in town and present,
	/// and how many session players and backing singers the studio could actually book on each day of that week. Read-only:
	/// the same queries the player's studio makes, which draw nothing.</summary>
	private void CaptureSessionSupplyWeek(int week) {
		if (!OS.GetCmdlineUserArgs().Contains("--session-supply-audit")) return;
		if (sessionSupplyWriter == null) {
			string dir = ProjectSettings.GlobalizePath("res://SimLogs");
			sessionSupplyWriter = new StreamWriter(Path.Combine(dir, $"{runName}-session-supply.csv"));
			sessionSupplyWriter.WriteLine("week,city,basedActs,presentActs,dayOfWeek,players,freeDaytime,drums,bass,keys,guitar,singers");
		}
		int today = LocalSceneRoomService.Day(TimeManager.Instance.CurrentDate);
		foreach (string city in SessionSupplyCities) {
			var based = LocalSceneIdentityService.ArtistsBasedAt(city).Select(id => ArtistManager.Instance.GetArtist(id))
				.Where(a => a?.lifecycleStatus == ArtistLifecycleStatus.Active).ToList();
			int present = based.Count(a => LocalScenePersistenceService.Present(a, city, today / 7));
			for (int d = 0; d < 7; d++) {
				var players = SessionEmploymentService.ListeningCandidates(city, today + d);
				int Count(params MusicianRole[] roles) => players.Count(m => roles.Any(r => MemberRoleService.Plays(m, r)) || m.primaryRole == MusicianRole.MultiInstrumentalist);
				int free = players.Count(m => !LocalSceneRoomService.HasPersonBooking(m.personId, today + d, 9 * 60, 17 * 60, null));
				int singers = SessionEmploymentService.SingerCandidates(city, today + d).Count;
				sessionSupplyWriter.WriteLine($"{week},{city},{based.Count},{present},{d},{players.Count},{free},{Count(MusicianRole.Drums)},{Count(MusicianRole.Bass)}," +
					$"{Count(MusicianRole.Piano, MusicianRole.Organ)},{Count(MusicianRole.LeadGuitar, MusicianRole.RhythmGuitar)},{singers}");
			}
		}
		if (week == 1 || week % 13 == 0) CaptureCityCensus(week, today);
		if (week % 13 == 0) sessionSupplyWriter.Flush();
	}

	private StreamWriter cityCensusWriter;
	/// <summary>Quarterly, every playable city: active based acts by primary genre, and the session players and singers
	/// bookable on that day. Long form (week,city,kind,key,count). Read-only.</summary>
	private void CaptureCityCensus(int week, int today) {
		if (cityCensusWriter == null) {
			cityCensusWriter = new StreamWriter(Path.Combine(ProjectSettings.GlobalizePath("res://SimLogs"), $"{runName}-city-census.csv"));
			cityCensusWriter.WriteLine("week,year,city,kind,key,count");
		}
		int year = TimeManager.Instance.CurrentDate.year;
		foreach (var place in ScenePlaceRegistry.All.Where(p => p.CountryCode == "US" && p.Id == p.PlayableCityId).OrderBy(p => p.Id, StringComparer.Ordinal)) {
			var based = LocalSceneIdentityService.ArtistsBasedAt(place.Id).Select(id => ArtistManager.Instance.GetArtist(id))
				.Where(a => a?.lifecycleStatus == ArtistLifecycleStatus.Active).ToList();
			foreach (var g in based.GroupBy(a => a.primaryGenre).OrderBy(g => g.Key.ToString(), StringComparer.Ordinal))
				cityCensusWriter.WriteLine($"{week},{year},{place.Id},genre,{g.Key},{g.Count()}");
			cityCensusWriter.WriteLine($"{week},{year},{place.Id},signed,all,{based.Count(a => !string.IsNullOrEmpty(a.labelId))}");
			cityCensusWriter.WriteLine($"{week},{year},{place.Id},session,players,{SessionEmploymentService.ListeningCandidates(place.Id, today).Count}");
			cityCensusWriter.WriteLine($"{week},{year},{place.Id},session,singers,{SessionEmploymentService.SingerCandidates(place.Id, today).Count}");
		}
		cityCensusWriter.Flush();
	}

	private void CloseSessionSupply() { sessionSupplyWriter?.Flush(); sessionSupplyWriter?.Dispose(); sessionSupplyWriter = null; cityCensusWriter?.Dispose(); cityCensusWriter = null; }
}
