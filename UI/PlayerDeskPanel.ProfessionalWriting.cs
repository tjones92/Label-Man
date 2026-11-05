using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
	private void ProfessionalWritingSection(PlayerDesk desk, SimulatedArtist artist) {
		Heading("PROFESSIONAL WRITERS");
		Body("Commission new work or license an available demo. Quotes reflect reputation and demand; the song may still disappoint. " +
			"A retainer lasts 12 weeks, billed every four weeks, and covers new services. Existing songs and publishing rights stay with their owners.");
		if (desk.IsCommissioning(artist.artistId)) Body("A writing assignment or licensed song is being prepared for this act.");
		foreach (var offer in ProfessionalWritingMarket.Offers(desk.Label, artist).Take(4)) {
			var contract = ProfessionalWritingMarket.ContractFor(offer.WriterId, desk.Label.labelId);
			Body(offer.Name + $" · reputation {offer.Reputation:P0} · {offer.PreviousAssignments} previous assignments, {offer.PreviousHits} Top 40 releases with this act" +
				(contract == null ? "" : $" · {contract.Kind}, {contract.EndWeek - ProfessionalWritingMarket.Week} weeks remaining"));
			var row = new HFlowContainer(); row.AddThemeConstantOverride("h_separation", 8);
			var commission = Btn($"COMMISSION ${offer.CommissionFee:N0} · {offer.DeliveryWeeks} weeks");
			commission.Disabled = !offer.Available || desk.IsCommissioning(artist.artistId);
			GreyWhy(commission, desk.IsCommissioning(artist.artistId) ? "A song is already being written for them." : "This writer is not taking work right now.");
			commission.Pressed += () => Act(() => { bool ok = desk.CommissionProfessionalSong(artist, offer.WriterId, out string message); Say(message, ok); return ok; });
			row.AddChild(commission);
			if (contract == null) {
				var preferred = Btn($"FIRST REFUSAL ${offer.PreferredRetainer:N0}/4wk");
				preferred.Pressed += () => Act(() => { bool ok = desk.ContractProfessionalWriters(offer.WriterId, WritingContractKind.Preferred, out string message); Say(message, ok); return ok; });
				row.AddChild(preferred);
				var exclusive = Btn($"EXCLUSIVE ${offer.ExclusiveRetainer:N0}/4wk");
				exclusive.Pressed += () => Act(() => { bool ok = desk.ContractProfessionalWriters(offer.WriterId, WritingContractKind.Exclusive, out string message); Say(message, ok); return ok; });
				row.AddChild(exclusive);
			}
			content.AddChild(row);
		}
		var demos = ProfessionalWritingMarket.Visible(CompositionCatalogService.GetProfessionalForGenre(artist.primaryGenre), desk.Label, artist)
			.Where(s => !desk.RepertoireFor(artist.artistId).Any(r => r.SongId == s.songId))
			.OrderByDescending(s => ProfessionalWritingMarket.DemoRead(s, desk.Label)).Take(4).ToArray();
		if (demos.Length == 0) Body("No available demos in this genre. New work requires a commission and delivery time.");
		foreach (var song in demos) {
			float fee = ProfessionalWritingMarket.LicenseFee(song, desk.Label, artist);
			var license = Btn($"LICENSE \"{song.title}\" · ${fee:N0} · {ProfessionalWritingMarket.SupplierName(ProfessionalWritingMarket.SupplierFor(song))}");
			license.TooltipText = "Your demo read: " + (ProfessionalWritingMarket.DemoRead(song, desk.Label) >= .7f ? "promising" : "uncertain") +
				". First recording reserved for 12 weeks; two days to prepare. Publishing rights are excluded.";
			license.Pressed += () => Act(() => { bool ok = desk.LicenseProfessionalSong(artist, song.songId, out string message); Say(message, ok); return ok; });
			content.AddChild(license);
		}
	}
}
