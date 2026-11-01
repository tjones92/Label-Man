using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// PRESS & TELEVISION in the office (media directive §14): decisions waiting first (a standards question, an offered date,
/// a reporter on deadline), then what's booked, then the two approaches (a disc to a producer, a sheet to a desk), then the
/// clippings. Every number on an offer is an estimate before air, and the costs are labelled as estimates in observe mode.
/// </summary>
public partial class PlayerDeskPanel {
	private void MediaSection(PlayerDesk desk) {
		if (!MediaAtmosphere.Observing || desk?.Label == null) return;
		GameDate today = TimeManager.Instance?.CurrentDate ?? GameDate.StartDate;
		int day = BroadcastExposure.DayIndex(today);
		var singles = desk.PitchableSingles().ToList();
		var queries = MediaAtmosphereService.OpenPlayerQueries().ToList();
		var offers = MediaAtmosphereService.Pitches.Where(p => p.Kind == PitchKind.Television && p.Status == PitchStatus.Offered).ToList();
		var inquiries = MediaAtmosphereService.Inquiries.Where(i => i.Open).ToList();
		var clippings = MediaAtmosphereService.PlayerClippings(day).Take(6).ToList();
		var seriesOffers = MediaAtmosphereService.ResidencyOffers.Where(o => o.Open).ToList();
		bool waiting = queries.Count + offers.Count + inquiries.Count + seriesOffers.Count > 0;
		if (singles.Count == 0 && !waiting && clippings.Count == 0 && !MediaResidencies.Active) return;

		SectionHeading("media", "PRESS & TELEVISION", waiting,
			"Send a disc to a television producer or a press sheet to an editor. Producers offer real dates with real conditions; " +
			"editors decide what runs. Audiences are estimates until the show airs. Media runs in observe mode: costs are shown as " +
			"estimates and nothing is charged, and nothing here moves record sales.");

		foreach (StandardsQuery q in queries) {
			TvProgram program = MediaOutletCatalog.Get(q.ProgramId);
			Body($"STANDARDS — {q.Authority} on \"{q.Title}\" ({q.ArtistName}, {MediaOutletCatalog.DisplayName(program, today)}, {q.AirDate.ToShortString()}): " +
				$"they read {q.Evidence} as {q.Tag.Replace('-', ' ')}{(q.Anxious ? ", though it's a cautious reading" : "")}. They want you to {q.RequestedChange}. " +
				$"Answer by {q.Deadline.ToShortString()}." + (q.ProducerSympathetic ? " The producer is on your side; the network decides." : ""));
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);
			foreach (StandardsResponse response in MediaAtmosphereService.StandardsOptions(q)) {
				var b = Btn(MediaAtmosphereService.ResponseLabel(response).ToUpperInvariant());
				string id = q.Id;
				b.Pressed += () => Act(() => { bool ok = desk.AnswerStandardsQuery(id, response, out string m); Say(m, ok); return true; });
				row.AddChild(b);
			}
			content.AddChild(row);
		}

		foreach (MediaPitch offer in offers) {
			TvProgram program = MediaOutletCatalog.Get(offer.ProgramId);
			GameDate air = MediaAtmosphereService.FromDayIndex(offer.AirDay);
			BroadcastScheduleVersion version = program.VersionOn(air);
			string slot = MediaText.SlotIn(program, version, desk.Label.homeCityId);
			string order = program.Kind == TvProgramKind.PrimetimeVariety ? "an early spot on the bill" : offer.Discovery ? "the new-record spot" : "a guest spot";
			Body($"OFFER — {MediaAtmosphereService.ProducerName(program)}, {MediaOutletCatalog.DisplayName(program, today)}: {offer.ArtistName} with \"{offer.Title}\" on " +
				$"{air.DayName} {air.ToShortString()} ({slot}); {order}. They {MediaText.Verb(program.Method)}: {program.MethodNote} " +
				$"Expected audience {MediaText.Audience(offer.ReachLow)} to {MediaText.Audience(offer.ReachHigh)}, about {offer.TeenShare:P0} teenagers. " +
				$"Costs: {offer.CostNote}. Answer by {MediaAtmosphereService.FromDayIndex(offer.OfferExpiresDay).ToShortString()}.");
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);
			var prep = Option();
			prep.CustomMinimumSize = new Vector2(360, 34);
			var preps = new[] { MediaPreparation.Standard, MediaPreparation.Extra, MediaPreparation.Minimal };
			foreach (MediaPreparation p in preps) prep.AddItem($"{p}: {MediaAtmosphereService.PreparationNote(p)}");
			row.AddChild(prep);
			string id = offer.Id;
			var accept = Primary("CONFIRM THE DATE");
			accept.Pressed += () => Act(() => {
				bool ok = desk.AnswerTelevisionOffer(id, true, preps[Mathf.Clamp(prep.Selected, 0, preps.Length - 1)], out string m); Say(m, ok); return true;
			});
			row.AddChild(accept);
			var decline = Btn("TURN IT DOWN");
			decline.Pressed += () => Act(() => { bool ok = desk.AnswerTelevisionOffer(id, false, MediaPreparation.Standard, out string m); Say(m, ok); return true; });
			row.AddChild(decline);
			content.AddChild(row);
		}

		ResidencyOffers(desk, seriesOffers, today);

		foreach (MediaInquiry inquiry in inquiries) {
			PressOutlet outlet = MediaOutletCatalog.GetPress(inquiry.OutletId);
			ContactLine(inquiry.ContactKey);
			string id = inquiry.Id;
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);
			if (inquiry.Kind == InquiryKind.OrdersQuestion) {
				Body($"DEADLINE — {outlet?.Name} wants to know how many \"{inquiry.Title}\" has sold before the issue closes " +
					$"({MediaAtmosphereService.FromDayIndex(inquiry.DeadlineDay).ToShortString()}). Your books say about {inquiry.ActualUnits:N0}.");
				foreach ((string answer, string label) in new[] { ("facts", "GIVE THE REAL FIGURE"), ("inflate", "MAKE IT SOUND BIGGER"), ("decline", "NO COMMENT") }) {
					var b = Btn(label);
					b.Pressed += () => Act(() => { bool ok = desk.AnswerMediaInquiry(id, answer, null, out string m); Say(m, ok); return true; });
					row.AddChild(b);
				}
				content.AddChild(row);
			} else {
				Body($"PHOTOS — {outlet?.Name} wants pictures of {inquiry.ArtistName} for a feature. An exclusive session is theirs first for four weeks; " +
					"supplied photos are the ones you already have. How the act talks about their private lives is up to them and you.");
				var romance = Option();
				romance.CustomMinimumSize = new Vector2(300, 34);
				var policies = new[] { ("private", "Private lives stay private"), ("dreamy", "A dreamy, noncommittal interview"), ("plant", "Plant a romance rumor") };
				foreach (var (_, label) in policies) romance.AddItem(label);
				row.AddChild(romance);
				foreach ((string answer, string label) in new[] { ("exclusive", "EXCLUSIVE SESSION"), ("supplied", "SEND PHOTOS"), ("decline", "DECLINE") }) {
					var b = Btn(label);
					b.Pressed += () => Act(() => {
						string policy = policies[Mathf.Clamp(romance.Selected, 0, policies.Length - 1)].Item1;
						bool ok = desk.AnswerMediaInquiry(id, answer, policy, out string m); Say(m, ok); return true;
					});
					row.AddChild(b);
				}
				content.AddChild(row);
			}
		}

		// What's in hand: confirmed dates and approaches still waiting on an answer.
		foreach (MediaPitch p in MediaAtmosphereService.Pitches.Where(p => p.Status is PitchStatus.Confirmed or PitchStatus.Sent).OrderBy(p => p.AirDay)) {
			if (p.Kind == PitchKind.Television) {
				TvProgram program = MediaOutletCatalog.Get(p.ProgramId);
				Body(p.Status == PitchStatus.Confirmed
					? $"    Booked: {p.ArtistName} on {MediaOutletCatalog.DisplayName(program, today)}, {MediaAtmosphereService.FromDayIndex(p.AirDay).ToShortString()} ({p.Preparation} preparation)."
					: $"    With {MediaAtmosphereService.ProducerName(program)} at {program.Name}: \"{p.Title}\", answer in a few days.");
			} else {
				Body($"    With {MediaOutletCatalog.GetPress(p.OutletId)?.Name}: your sheet on \"{p.Title}\", decided at the next issue.");
			}
		}

		if (singles.Count > 0) {
			if (!desk.AtHome) Body("You send discs and press sheets from the office. Answers reach you anywhere.");
			else {
				Body("Send a disc and a press sheet to a producer (estimated: 1 promo copy, $0.15 postage, 1h; not charged in observe mode).");
				var programs = MediaAtmosphereService.PitchablePrograms(desk.Label.homeCityId, desk.CurrentCityId, today);
				var row = new HBoxContainer();
				row.AddThemeConstantOverride("separation", 8);
				var recordPick = Option();
				recordPick.CustomMinimumSize = new Vector2(280, 34);
				foreach (var r in singles) recordPick.AddItem($"\"{r.baseRecord.title}\" — {r.baseRecord.artistName}");
				row.AddChild(recordPick);
				var programPick = Option();
				programPick.CustomMinimumSize = new Vector2(420, 34);
				foreach (TvProgram p in programs) {
					var v = p.VersionOn(today) ?? p.Schedule.FirstOrDefault(x => today >= x.First && today <= x.Last) ?? p.Schedule.First();
					programPick.AddItem($"{MediaOutletCatalog.DisplayName(p, today)} ({MediaText.NetworkName(p)}) — {MediaText.SlotIn(p, v, desk.Label.homeCityId)}");
				}
				row.AddChild(programPick);
				var send = Btn("SEND THE DISC");
				send.Pressed += () => Act(() => {
					if (programs.Count == 0) return false;
					string recordId = singles[Mathf.Clamp(recordPick.Selected, 0, singles.Count - 1)].baseRecord.recordId;
					string programId = programs[Mathf.Clamp(programPick.Selected, 0, programs.Count - 1)].Id;
					bool ok = desk.PitchTelevision(recordId, programId, out string m); Say(m, ok); return true;
				});
				row.AddChild(send);
				content.AddChild(row);

				Body("Send a press sheet to an editor (estimated: 1 promo copy, a photo, postage, 1h; not charged in observe mode).");
				var desks = MediaAtmosphereService.PitchableDesks(desk.Label.homeCityId, today);
				var prow = new HBoxContainer();
				prow.AddThemeConstantOverride("separation", 8);
				var pressRecord = Option();
				pressRecord.CustomMinimumSize = new Vector2(280, 34);
				foreach (var r in singles) pressRecord.AddItem($"\"{r.baseRecord.title}\" — {r.baseRecord.artistName}");
				prow.AddChild(pressRecord);
				var deskPick = Option();
				deskPick.CustomMinimumSize = new Vector2(300, 34);
				foreach (PressOutlet o in desks) deskPick.AddItem($"{o.Name} ({MediaPress.RoleTitle(o, MediaPress.RoleFor(o, StoryAngle.LabelPitch)).ToLowerInvariant()})");
				prow.AddChild(deskPick);
				var angles = new[] { StoryAngle.LabelPitch, StoryAngle.Hometown, StoryAngle.Breakout, StoryAngle.CommunityNews };
				var anglePick = Option();
				anglePick.CustomMinimumSize = new Vector2(220, 34);
				foreach (StoryAngle a in angles) anglePick.AddItem(MediaAtmosphereService.AngleLabel(a));
				prow.AddChild(anglePick);
				var mail = Btn("SEND THE SHEET");
				mail.Pressed += () => Act(() => {
					if (desks.Count == 0) return false;
					string recordId = singles[Mathf.Clamp(pressRecord.Selected, 0, singles.Count - 1)].baseRecord.recordId;
					string outletId = desks[Mathf.Clamp(deskPick.Selected, 0, desks.Count - 1)].Id;
					bool ok = desk.PitchPress(recordId, outletId, angles[Mathf.Clamp(anglePick.Selected, 0, angles.Length - 1)], out string m); Say(m, ok); return true;
				});
				prow.AddChild(mail);
				content.AddChild(prow);
			}
		}

		ResidencyDesk(desk, today);

		var fans = desk.FanLines();
		if (fans.Count > 0) {
			Body("The fans (what the mail, the shops and the teen papers say; the act's standing in the business is a separate thing):");
			foreach (string line in fans) Body("    " + line);
		}

		if (clippings.Count > 0) {
			Body("Clippings:");
			foreach (MediaStory s in clippings) {
				PressOutlet outlet = MediaOutletCatalog.GetPress(s.OutletId);
				string body = MediaPress.Body(s);
				string wrong = s.Inaccurate && !s.Corrected ? $" (wrong: {s.InaccurateClaim})" : s.Corrected ? " (corrected)" : "";
				Body($"    {MediaAtmosphereService.FromDayIndex(s.IssueDay).ToShortString()}, {outlet?.Name}: {MediaPress.Sentence(MediaPress.Headline(s))}{(string.IsNullOrEmpty(body) ? "" : " " + body)}{wrong}");
				if (s.Inaccurate && !s.Corrected && s.Angle != StoryAngle.Correction) {
					string id = s.Id;
					var fix = Btn("ASK FOR A CORRECTION");
					fix.Pressed += () => Act(() => { bool ok = desk.RequestPressCorrection(id, out string m); Say(m, ok); return true; });
					content.AddChild(fix);
				}
			}
		}
	}

	/// <summary>
	/// A producer's or network's residency offer (CelebrityFanbaseDesign.md §3.3.2): the series, its dates and audience, and
	/// the four terms. Take the producer's terms, counter once, or say no.
	/// </summary>
	private void ResidencyOffers(PlayerDesk desk, List<MediaResidencyOffer> offers, GameDate today) {
		foreach (MediaResidencyOffer o in offers) {
			bool vehicle = o.Kind == ResidencyKind.Vehicle;
			float scale = MediaResidencies.ScalePerEpisode(o.Daily);
			string what = vehicle
				? $"{o.Network} wants a half-hour series built around {o.ArtistName}: \"{o.ShowName}\", {o.Night}s at 7:30 from " +
				  $"{MediaResidencies.VehiclePremiere(o.SeasonYear, o.Night).ToShortString()} to the end of next August, reruns from May."
				: $"{o.ShowName} wants {o.ArtistName} as a regular on every original episode to {MediaAtmosphereService.FromDayIndex(o.SeasonEndDay).ToShortString()}" +
				  (o.Daily ? ", five afternoons a week." : ".");
			Body($"SERIES OFFER — {what} Expected audience per episode {MediaText.Audience(o.ReachLow)} to {MediaText.Audience(o.ReachHigh)}, " +
				$"about {o.TeenShare:P0} teenagers. Their terms: scale (${scale:N0} a performer an episode, paid to the band), " +
				$"{(vehicle ? "the show's songs" : "the act's own records")}, and a full call ({MediaAtmosphereService.DiaryNote(o, false)}, weeks the act can't spend on the road). " +
				(o.Countered ? "They've refused one counter; only these terms stand. " : "You can counter once. ") +
				$"Answer by {MediaAtmosphereService.FromDayIndex(o.ExpiresDay).ToShortString()}.");
			var row = new HBoxContainer();
			row.AddThemeConstantOverride("separation", 8);
			OptionButton length = null;
			if (!vehicle) {
				length = Option();
				length.AddItem("The season");
				length.AddItem($"{MediaResidencies.TrialWeeks} weeks, then talk");
				row.AddChild(length);
			}
			var fee = Option();
			foreach (float f in MediaResidencies.FeeMultiples) fee.AddItem(f == 1f ? "Scale" : $"{f:0.#}× scale");
			row.AddChild(fee);
			var material = Option();
			material.AddItem(vehicle ? "The show's songs" : "Their own records");
			material.AddItem(vehicle ? "Their own records" : "The show's songs");
			row.AddChild(material);
			var diary = Option();
			diary.CustomMinimumSize = new Vector2(260, 34);
			diary.AddItem($"Full call ({MediaAtmosphereService.DiaryNote(o, false)})");
			diary.AddItem($"Tape in blocks ({MediaAtmosphereService.DiaryNote(o, true)})");
			row.AddChild(diary);
			string id = o.Id;
			ResidencyTerms Terms() => new() {
				Trial = length?.Selected == 1, FeeMultiple = MediaResidencies.FeeMultiples[Mathf.Clamp(fee.Selected, 0, MediaResidencies.FeeMultiples.Length - 1)],
				ShowSongs = vehicle ? material.Selected == 0 : material.Selected == 1, Blocks = diary.Selected == 1,
			};
			var take = Primary("TAKE THEIR TERMS");
			take.Pressed += () => Act(() => { bool ok = desk.AnswerResidencyOffer(id, true, ResidencyTerms.Producer(o.Kind), out string m); Say(m, ok); return true; });
			row.AddChild(take);
			if (!o.Countered) {
				var counter = Btn("PROPOSE THESE TERMS");
				counter.Pressed += () => Act(() => { bool ok = desk.AnswerResidencyOffer(id, true, Terms(), out string m); Say(m, ok); return true; });
				row.AddChild(counter);
			}
			var decline = Btn("TURN IT DOWN");
			decline.Pressed += () => Act(() => { bool ok = desk.AnswerResidencyOffer(id, false, default, out string m); Say(m, ok); return true; });
			row.AddChild(decline);
			content.AddChild(row);
		}
	}

	/// <summary>The label's series in hand, pitches waiting, and the approach: an act put up for a chair or a series of its own.</summary>
	private void ResidencyDesk(PlayerDesk desk, GameDate today) {
		if (!MediaResidencies.Active) return;
		int day = BroadcastExposure.DayIndex(today);
		foreach (MediaResidency r in MediaAtmosphereService.PlayerResidencies.Where(r => r.ToDay >= day).OrderBy(r => r.FromDay)) {
			string show = r.ShowName ?? MediaOutletCatalog.Get(r.ProgramId)?.Name;
			Body($"    Series: {r.ArtistName} on {show}, {MediaAtmosphereService.FromDayIndex(r.FromDay).ToShortString()} to " +
				$"{MediaAtmosphereService.FromDayIndex(r.ToDay).ToShortString()}; {r.Episodes} episodes so far; {(r.ShowSongs ? "the show's songs" : "their own records")}, " +
				$"{r.FeeMultiple:0.#}× scale, {r.DiaryShare:P0} of each week taping.");
		}
		foreach (MediaResidencyPitch p in MediaAtmosphereService.ResidencyPitches.Where(p => p.Status is ResidencyPitchStatus.Sent or ResidencyPitchStatus.Listed && day <= p.ExpiresDay)) {
			string to = p.ProgramId == MediaResidencies.VehiclePitch ? "the networks, for a series of their own" : MediaOutletCatalog.Get(p.ProgramId)?.Name;
			Body(p.Status == ResidencyPitchStatus.Sent ? $"    With {to}: {p.ArtistName}, answer in a few days."
				: $"    On the list at {to}: {p.ArtistName}, until {MediaAtmosphereService.FromDayIndex(p.ExpiresDay).ToShortString()}.");
		}
		var acts = desk.Roster.Where(a => a != null && a.isActive).ToList();
		if (acts.Count == 0 || !desk.AtHome) return;
		var programs = MediaAtmosphereService.ResidencyPrograms(today);
		var targets = programs.Select(p => (Id: p.Id, Label: $"A regular spot on {MediaOutletCatalog.DisplayName(p, today)}")).ToList();
		if (today.year >= MediaResidencies.FirstVehicleYear) targets.Add((MediaResidencies.VehiclePitch, "A series of their own"));
		if (targets.Count == 0) return;
		Body("Put an act up for a television series (estimated: postage and 1h). Producers cast on fit and evidence; a pitch counts at the next casting.");
		var row = new HBoxContainer();
		row.AddThemeConstantOverride("separation", 8);
		var actPick = Option();
		actPick.CustomMinimumSize = new Vector2(240, 34);
		foreach (SimulatedArtist a in acts) actPick.AddItem(a.stageName);
		row.AddChild(actPick);
		var targetPick = Option();
		targetPick.CustomMinimumSize = new Vector2(360, 34);
		foreach (var t in targets) targetPick.AddItem(t.Label);
		row.AddChild(targetPick);
		var send = Btn("PUT THEM UP");
		send.Pressed += () => Act(() => {
			string artistId = acts[Mathf.Clamp(actPick.Selected, 0, acts.Count - 1)].artistId;
			string target = targets[Mathf.Clamp(targetPick.Selected, 0, targets.Count - 1)].Id;
			bool ok = desk.PitchResidency(artistId, target, out string m); Say(m, ok); return true;
		});
		row.AddChild(send);
		content.AddChild(row);
	}

	/// <summary>The contact's own recent memory of the label, so a call reads as a person with a history (§14).</summary>
	private void ContactLine(string key) {
		MediaContactMemory c = MediaAtmosphereService.ContactFor(key);
		if (c == null || c.Lines.Count == 0) return;
		Body($"    {c.Name} ({c.Role}, {c.OutletName}) remembers: {c.Lines[0].Substring(Math.Min(c.Lines[0].Length, 12)).Trim()}");
	}
}
