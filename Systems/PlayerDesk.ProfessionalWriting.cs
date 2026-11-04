using System;
using System.Linq;

public partial class PlayerDesk {
	public bool CommissionProfessionalSong(SimulatedArtist artist, string writerId, out string message, string coWriterPersonId = null) {
		message = "That act isn't on your roster.";
		if (artist == null || artist.labelId != Label?.labelId || !ProfessionalWritingMarket.Enabled) return false;
		if (!RequireHome(out message)) return false;
		if (IsCommissioning(artist.artistId) || ProfessionalWritingMarket.State.Orders.Any(o => o.ArtistId == artist.artistId && o.Status == WritingOrderStatus.Pending)) {
			message = "A writer is already working on this act's assignment."; return false;
		}
		writerId ??= ProfessionalWritingMarket.Offers(Label, artist).FirstOrDefault(o => o.Available)?.WriterId;
		if (writerId == null) { message = "No writing capacity is available right now."; return false; }
		if (!Require(CommissionHours, out message)) return false;
		if (!ProfessionalWritingMarket.TryCommission(Label, artist, writerId, TimeManager.Instance.CurrentDate, out _, out message, coWriterPersonId)) return false;
		Spend(CommissionHours); Note(message); Changed?.Invoke(); return true;
	}

	public bool ContractProfessionalWriters(string writerId, WritingContractKind kind, out string message) {
		if (!RequireHome(out message) || !Require(1, out message)) return false;
		if (!ProfessionalWritingMarket.TryContract(Label, writerId, kind, out message)) return false;
		Spend(1); Note(message); Changed?.Invoke(); return true;
	}

	public bool LicenseProfessionalSong(SimulatedArtist artist, string songId, out string message) {
		message = "That act isn't on your roster.";
		if (artist == null || artist.labelId != Label?.labelId || !ProfessionalWritingMarket.Enabled) return false;
		if (!RequireHome(out message) || !Require(1, out message)) return false;
		if (RepertoireFor(artist.artistId).Any(s => s.SongId == songId) || rehearsals.Any(r => r.ArtistId == artist.artistId && r.SongId == songId)) {
			message = "This act already has that song."; return false;
		}
		var song = CompositionCatalogService.GetSong(songId);
		if (song?.originKind != SongOriginKind.ProfessionalOffice || !ProfessionalWritingMarket.TryLicense(song, Label, artist, out message)) return false;
		var date = TimeManager.Instance.CurrentDate;
		rehearsals.Add(new CoverRehearsal { ArtistId = artist.artistId, SongId = songId, Title = song.title, Genre = song.primaryGenre,
			SourceTag = "licensed", IsCommission = true, Started = date, ReadyDate = date.AddDays(2),
			ReadHook = ProfessionalWritingMarket.DemoRead(song, Label, "hook"), ReadQuality = ProfessionalWritingMarket.DemoRead(song, Label) });
		Spend(1); Note(message); Changed?.Invoke(); return true;
	}

	private void ProcessProfessionalWritingDeliveries(GameDate date) {
		if (!ProfessionalWritingMarket.Enabled || ProfessionalWritingMarket.State == null || Label == null) return;
		ProfessionalWritingMarket.Deliver(date);
		foreach (var order in ProfessionalWritingMarket.State.Orders.Where(o => o.LabelId == Label.labelId && !o.PlayerNotified && o.Status != WritingOrderStatus.Pending)) {
			order.PlayerNotified = true;
			if (order.Status == WritingOrderStatus.Cancelled) { Note("A writing assignment ended because the act or supplier is no longer available."); continue; }
			var song = CompositionCatalogService.GetSong(order.SongId);
			rehearsals.Add(new CoverRehearsal { ArtistId = order.ArtistId, SongId = song.songId, Title = song.title, Genre = song.primaryGenre,
				SourceTag = "commissioned", IsCommission = true, Started = order.DueDate, ReadyDate = date,
				ReadHook = ProfessionalWritingMarket.DemoRead(song, Label, "hook"), ReadQuality = ProfessionalWritingMarket.DemoRead(song, Label) });
		}
	}
}
