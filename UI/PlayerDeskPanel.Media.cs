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
		bool waiting = queries.Count + offers.Count + inquiries.Count > 0;
		if (singles.Count == 0 && !waiting && clippings.Count == 0) return;

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

	/// <summary>The contact's own recent memory of the label, so a call reads as a person with a history (§14).</summary>
	private void ContactLine(string key) {
		MediaContactMemory c = MediaAtmosphereService.ContactFor(key);
		if (c == null || c.Lines.Count == 0) return;
		Body($"    {c.Name} ({c.Role}, {c.OutletName}) remembers: {c.Lines[0].Substring(Math.Min(c.Lines[0].Length, 12)).Trim()}");
	}
}
