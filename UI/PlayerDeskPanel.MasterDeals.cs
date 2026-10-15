using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class PlayerDeskPanel {
	/// <summary>Early-game Phase 5 (§5): the master deals on the books -- open offers with every contract's rights and
	/// money spelled out before signing, signed deals with their statements, and who you could shop each title to.</summary>
	private void BuildMasterDeals(PlayerDesk desk) {
		var offers = desk.MasterOffers;
		var deals = desk.MasterDeals.Where(d => d.Live || d.Ledger.Count > 0).ToList();
		var shoppable = desk.ShoppableMasterIds().Take(4).ToList();
		if (offers.Count == 0 && deals.Count == 0 && shoppable.Count == 0) return;
		Heading("SHOP THE MASTER", "Lease or sell a master to a real label, which pays out of its own cash and puts the record out itself. " +
			"A lease comes home after its term; a sale doesn't. Either way the title leaves your channels the day you sign, and they take " +
			$"your pressed copies at ${PlayerDesk.MasterStockPerCopy:F2} each. Publishing is never part of it.");

		foreach (var offer in offers) {
			Body($"{offer.Answer} On \"{offer.Title}\". Open until {LocalSceneRoomService.Date(offer.ExpiresDay).ToShortString()}." +
				(offer.Route == MasterRoute.Inbound ? " They called you." : ""));
			var (copies, stock) = desk.MasterStockValue(offer.RecordId);
			if (copies > 0 || stock > 0f) Body($"They take {copies:N0} pressed copies (and any run at the plant) for ${stock:N2} on signing.");
			var row = new HFlowContainer(); row.AddThemeConstantOverride("h_separation", 8);
			for (int i = 0; i < offer.Terms.Count; i++) {
				int pick = i;
				var t = offer.Terms[i];
				string label = t.Kind switch {
					MasterDealKind.Lease => $"LEASE: ${t.Advance:N0} + {t.CentsPerUnit * 100f:F1}c/COPY, {t.TermWeeks}WK",
					MasterDealKind.Sale => $"SELL: ${t.Advance:N0}, FOR GOOD",
					_ => $"SELL WITH THE ACT: ${t.Advance:N0} + {t.CentsPerUnit * 100f:F1}c/COPY",
				};
				var accept = Btn(label);
				string block = desk.MasterSignBlock(offer, pick);
				accept.Disabled = block != null;
				accept.TooltipText = (block != null ? block + "\n" : "") + t.Why +
					(t.Recoupable ? " The advance comes back out of the royalty before any more is paid; statements every quarter." : "");
				string id = offer.Id;
				accept.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.AcceptMasterOffer(id, pick, out string m); Say(m, ok); return ok; });
				row.AddChild(accept);
			}
			string offerId = offer.Id;
			var decline = Btn("PASS");
			decline.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.DeclineMasterOffer(offerId, out string m); Say(m, ok); return ok; });
			row.AddChild(decline);
			content.AddChild(row);
		}

		if (deals.Count > 0) {
			var today = LocalSceneRoomService.Day(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
			content.AddChild(new LedgerTable().Set(null, new[] { "title", "buyer", "deal", "status", "copies", "earned", "recouped", "paid you" },
				deals.Select(d => LedgerTable.Entry($"\"{d.Title}\"", d.BuyerName, d.Terms.Kind switch {
					MasterDealKind.Lease => $"lease {d.Terms.RoyaltyRate:P0}", MasterDealKind.Sale => "sold", _ => $"sold + act, {d.Terms.RoyaltyRate:P0}" },
					d.Status switch {
						MasterDealStatus.AwaitingRelease => $"out {LocalSceneRoomService.Date(d.ReleaseDay).ToShortString()}",
						MasterDealStatus.Out => $"back {LocalSceneRoomService.Date(d.ExpiresDay ?? today).ToShortString()}",
						MasterDealStatus.Permanent => "theirs",
						_ => d.EndNote ?? d.Status.ToString().ToLowerInvariant(),
					}, $"{d.Units:N0}", LedgerTable.Money(d.Earned), LedgerTable.Money(d.Recouped), LedgerTable.Money(d.Paid)))));
			var next = deals.Where(d => d.Status is MasterDealStatus.Out or MasterDealStatus.Permanent && d.Terms.Kind != MasterDealKind.Sale)
				.OrderBy(d => d.NextStatementDay).FirstOrDefault();
			if (next != null) Body($"Next statement: {next.BuyerName} on \"{next.Title}\", {LocalSceneRoomService.Date(next.NextStatementDay).ToShortString()}." +
				(next.Terms.Kind == MasterDealKind.Lease ? $" You owe {next.ArtistName} its royalty on their copies, settled the same day." : ""));
		}

		foreach (string recordId in shoppable) {
			var buyers = desk.MasterBuyersFor(recordId, 5);
			if (buyers.Count == 0) { Body($"\"{TitleOf(desk, recordId)}\": no label in its sound is buying."); continue; }
			Body($"\"{TitleOf(desk, recordId)}\": who might take it.");
			foreach (var b in buyers) {
				var row = new HFlowContainer(); row.AddThemeConstantOverride("h_separation", 8);
				string passed = desk.MasterPasses.LastOrDefault(p => p.BuyerLabelId == b.Label.labelId && p.RecordId == recordId
					&& p.Evidence == desk.MasterEvidence(recordId))?.Reason;
				var post = desk.MasterPostings.LastOrDefault(p => p.BuyerLabelId == b.Label.labelId && p.RecordId == recordId && !p.NoReply);
				string state = b.Block ?? passed ?? (post != null ? $"tape posted; word by {LocalSceneRoomService.Date(post.AnswerDay).ToShortString()}" : null);
				var call = desk.MasterCalls.LastOrDefault(c => c.BuyerLabelId == b.Label.labelId && c.RecordId == recordId && c.AppointmentDay.HasValue);
				var text = new Label { Text = $"{b.Label.labelName} ({b.Label.tier}, {b.CityName}{(b.OwnSound ? "" : ", near their sound")})" +
					(state != null ? $" · {state}" : "") + (call != null ? $" · appointment {LocalSceneRoomService.Date(call.AppointmentDay.Value).ToShortString()}" : "") };
				text.AddThemeColorOverride("font_color", Ink);
				row.AddChild(text);
				string buyerId = b.Label.labelId;
				var phone = Btn($"CALL  (${desk.MasterCallToll(b.Label):N2}, {PlayerDesk.MasterCallHours}h)");
				phone.TooltipText = "From the office, in their hours. They'll say whether they'd hear it, not what they think of it.";
				phone.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.CallMasterBuyer(buyerId, recordId, out string m); Say(m, ok); return ok; });
				var tape = Btn($"POST A TAPE  (${PlayerDesk.MasterTapeDub + PlayerDesk.MasterPostage:N2})");
				tape.TooltipText = "An answer in two to four weeks, if anyone plays it. Far likelier when a call asked for it.";
				tape.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.PostMasterTape(buyerId, recordId, out string m); Say(m, ok); return ok; });
				row.AddChild(phone); row.AddChild(tape);
				if (desk.CurrentCityId == b.CityId) {
					var visit = Btn($"PLAY IT FOR THEM  ({PlayerDesk.MasterVisitHours}h)");
					visit.TooltipText = "In their office, in their hours, with an acetate or a pressed copy. Without an appointment you may not be seen.";
					visit.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.VisitMasterBuyer(buyerId, recordId, out string m); Say(m, ok); return ok; });
					row.AddChild(visit);
				}
				content.AddChild(row);
			}
		}
	}

	private static string TitleOf(PlayerDesk desk, string recordId) =>
		desk.Masters.FirstOrDefault(m => m.Record?.recordId == recordId)?.SongTitle
		?? desk.ReleasedRecords.FirstOrDefault(r => r.baseRecord?.recordId == recordId)?.baseRecord.title ?? recordId;
}

public partial class PlayerDeskPanel {
	/// <summary>Early-game Phase 5A (§7.2): songs whose publishing the label controls, plugged to real labels for their own
	/// acts to cut -- open offers, signed placements with their statements, and who might take each song.</summary>
	private void BuildPublishingRoyalties(PlayerDesk desk) {
		var books = desk.Publishing;
		if (books.Lines.Count == 0) return;
		Heading("PUBLISHING", "As publisher you keep a cent of the 2c mechanical on every copy of your songs, on any label, and pay the writer the " +
			"other. Radio plays of your songs, on anyone's record, are logged by the performing-rights society: about 4c a play, half to you and half " +
			"paid straight to the writers, a quarter's plays paid on the 1st of the quarter after next.");
		var owed = books.Lines.Where(l => l.Kind == PublishingLineKind.PerformanceLogged && !l.Settled)
			.GroupBy(l => l.PayOnDay).OrderBy(g => g.Key).ToList();
		var rows = books.Lines.Where(l => l.Kind == PublishingLineKind.PerformanceLogged).GroupBy(l => l.Title ?? l.SongId)
			.OrderByDescending(g => g.Sum(l => l.Performances)).Take(8)
			.Select(g => LedgerTable.Entry($"\"{g.Key}\"", $"{g.Sum(l => l.Performances):N0}", LedgerTable.Money(g.Sum(l => l.Amount)),
				LedgerTable.Money(g.Where(l => l.Settled).Sum(l => l.Amount)), LedgerTable.Money(g.Sum(l => l.WriterAmount)))).ToList();
		if (rows.Count > 0) content.AddChild(new LedgerTable().Set(null, new[] { "song", "radio plays", "yours", "paid you", "writers'" }, rows));
		foreach (var g in owed)
			Body($"Statement {LocalSceneRoomService.Date(g.Key).ToShortString()}: ${g.Sum(l => l.Amount):N2} to you, ${g.Sum(l => l.WriterAmount):N2} to the writers.");
		if (books.WriterMechanicalPaid > 0f) Body($"Writer's half of the mechanical paid to your acts so far: ${books.WriterMechanicalPaid:N2}.");
	}

	private void BuildSongPlacements(PlayerDesk desk) {
		var offers = desk.PlacementOffers;
		var deals = desk.Placements.Where(d => d.Live || d.Ledger.Count > 1).ToList();
		var placeable = desk.PlaceableSongIds().Take(4).ToList();
		if (offers.Count == 0 && deals.Count == 0 && placeable.Count == 0) return;
		Heading("PLACE A SONG", "When the publishing is yours, plug the song to a label for one of its own acts to cut. They pay a small " +
			$"advance and hold it {PlayerDesk.PlacementHoldDays / 7} weeks; if they cut it, the publisher's cent of the 2c on their sales comes to you once the " +
			"advance is back, on a quarterly statement; the writer's cent goes to your act. Their record competes with yours. The song stays yours.");

		foreach (var offer in offers) {
			Body($"{offer.Answer} On \"{offer.Title}\". Open until {LocalSceneRoomService.Date(offer.ExpiresDay).ToShortString()}." +
				(offer.Cover ? " Theirs would be a cover of your record." : ""));
			var row = new HFlowContainer(); row.AddThemeConstantOverride("h_separation", 8);
			var accept = Btn($"PLACE IT: ${offer.Advance:N0} ADVANCE");
			string block = desk.PlacementSignBlock(offer);
			accept.Disabled = block != null;
			accept.TooltipText = (block != null ? block + "\n" : "") + offer.Why;
			string id = offer.Id;
			accept.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.AcceptPlacementOffer(id, out string m); Say(m, ok); return ok; });
			var decline = Btn("PASS");
			decline.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.DeclinePlacementOffer(id, out string m); Say(m, ok); return ok; });
			row.AddChild(accept); row.AddChild(decline);
			content.AddChild(row);
		}

		if (deals.Count > 0) {
			content.AddChild(new LedgerTable().Set(null, new[] { "song", "label", "act", "status", "copies", "earned", "recouped", "paid you" },
				deals.Select(d => LedgerTable.Entry($"\"{d.Title}\"", d.BuyerName, d.ArtistName ?? "—",
					d.Status switch {
						SongPlacementStatus.Held => $"held to {LocalSceneRoomService.Date(d.HoldUntilDay).ToShortString()}",
						SongPlacementStatus.Out => "on their record",
						_ => d.EndNote ?? d.Status.ToString().ToLowerInvariant(),
					}, $"{d.Units:N0}", LedgerTable.Money(d.Earned), LedgerTable.Money(d.Recouped), LedgerTable.Money(d.Paid)))));
			var next = deals.Where(d => d.Status == SongPlacementStatus.Out).OrderBy(d => d.NextStatementDay).FirstOrDefault();
			if (next != null) Body($"Next statement: {next.BuyerName} on \"{next.Title}\", {LocalSceneRoomService.Date(next.NextStatementDay).ToShortString()}.");
		}

		foreach (string songId in placeable) {
			string title = CompositionCatalogService.GetSong(songId)?.title ?? songId;
			var buyers = desk.PlacementBuyersFor(songId, 5);
			if (buyers.Count == 0) { Body($"\"{title}\": no label has an act who sings that kind of song."); continue; }
			Body($"\"{title}\": who might cut it.");
			foreach (var b in buyers) {
				var row = new HFlowContainer(); row.AddThemeConstantOverride("h_separation", 8);
				string passed = desk.PlacementPasses.LastOrDefault(p => p.BuyerLabelId == b.Label.labelId && p.SongId == songId
					&& p.Evidence == desk.PlacementEvidence(songId))?.Reason;
				var post = desk.PlacementPostings.LastOrDefault(p => p.BuyerLabelId == b.Label.labelId && p.SongId == songId && !p.NoReply);
				string state = b.Block ?? passed ?? (post != null ? $"tape posted; word by {LocalSceneRoomService.Date(post.AnswerDay).ToShortString()}" : null);
				var call = desk.PlacementCalls.LastOrDefault(c => c.BuyerLabelId == b.Label.labelId && c.SongId == songId && c.AppointmentDay.HasValue);
				var text = new Label { Text = $"{b.Label.labelName} ({b.Label.tier}, {b.CityName}{(b.OwnSound ? "" : ", acts near its sound")})" +
					(state != null ? $" · {state}" : "") + (call != null ? $" · appointment {LocalSceneRoomService.Date(call.AppointmentDay.Value).ToShortString()}" : "") };
				text.AddThemeColorOverride("font_color", Ink);
				row.AddChild(text);
				string buyerId = b.Label.labelId;
				var phone = Btn($"CALL  (${desk.MasterCallToll(b.Label):N2}, {PlayerDesk.MasterCallHours}h)");
				phone.TooltipText = "From the office, in their hours. They'll say whether anyone there sings that kind of song, not what they think of it.";
				phone.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.CallPlacementBuyer(buyerId, songId, out string m); Say(m, ok); return ok; });
				var tape = Btn($"POST A TAPE  (${PlayerDesk.MasterTapeDub + PlayerDesk.MasterPostage:N2})");
				tape.TooltipText = "An answer in two to four weeks, if anyone plays it. Far likelier when a call asked for it.";
				tape.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.PostPlacementTape(buyerId, songId, out string m); Say(m, ok); return ok; });
				row.AddChild(phone); row.AddChild(tape);
				if (desk.CurrentCityId == b.CityId) {
					var visit = Btn($"PLAY IT FOR THEM  ({PlayerDesk.MasterVisitHours}h)");
					visit.TooltipText = "In their office, in their hours, with an acetate or a pressed copy of your record of it.";
					visit.Pressed += () => Act(() => { bool ok = PlayerDesk.Instance.VisitPlacementBuyer(buyerId, songId, out string m); Say(m, ok); return ok; });
					row.AddChild(visit);
				}
				content.AddChild(row);
			}
		}
	}
}
