using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

/// <summary>Enabled walkthrough through the real desk and autoloads. This is scripted player-path
/// evidence, not a human usability test or a historical population census.</summary>
public partial class PolarPlayerPlaytestRunner : Node {
	private readonly List<object> evidence = new();
	private PlayerDeskPanel panel;
	private string run;
	private string artistId;
	private bool screenshots;
	private bool keepOpen;
	private PlayerDesk Desk => PlayerDesk.Instance;
	private SimulatedArtist Act => ArtistManager.Instance.GetArtist(artistId);
	private string Output(string suffix) => ProjectSettings.GlobalizePath($"res://SimLogs/{run}-{suffix}");

	public override async void _Ready() {
		try {
			run = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--run="))?[6..] ?? "polar-player-1001";
			if (run.Any(c => !char.IsLetterOrDigit(c) && c != '-')) throw new InvalidOperationException("Invalid run name");
			screenshots = OS.GetCmdlineUserArgs().Contains("--screenshots");
			keepOpen = OS.GetCmdlineUserArgs().Contains("--keep-open");
			Check(PolarSongBehavior.UsePolarFitSelection, "enabled before player/world initialization");
			AddChild(GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate());
			panel = Descendants(this).OfType<PlayerDeskPanel>().Single();
			UIManager.Instance.OnClick_Desk();
			string openSave = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--open-save="))?[12..];
			if (openSave != null) {
				Action(SaveGameService.Load(openSave, out var loadMessage), "open saved playtest", loadMessage);
				Check(PolarSongBehavior.UsePolarFitSelection, "opened save restores enabled mode");
				await Press("CATALOG");
				GD.Print($"POLAR_PLAYER_PLAYTEST_OPEN save={openSave} enabled=true");
				return;
			}
			Check(!File.Exists(Output("summary.json")) && !SaveGameService.HasSave(run), "fresh artifact/save family");
			if (OS.GetCmdlineUserArgs().Contains("--refusal-ui-fixture")) { await RefusalUiFixture(); return; }
			CheckSessionSaveContract();
			await Screen("01-founding");

			var city = DistanceModel.GetCities().FirstOrDefault(c => c.name.Contains("Memphis", StringComparison.OrdinalIgnoreCase))
				?? DistanceModel.GetCities().First();
			Action(Desk.FoundLabel("Polar Playtest Records", city.cityId, FoundingArchetype.ExMusician, out var message), "found", message);
			Action(Desk.PassTime(Desk.HoursUntil(17), out message), "wait for clubs", message);
			Action(Desk.ScoutVenue(PlayerDesk.ScoutingVenue.ClubsAndRoadhouses, out message), "scout clubs", message);
			Check(Desk.Slate.Count > 0, "scouting yields real prospects");
			foreach (var prospect in Desk.Slate) {
				Check(prospect.LiveSet.Count >= 3 && prospect.LiveSet.Count <= 7, "live set has supported length");
				Check(prospect.LiveSet.Where(s => s.SongId != null).Select(s => s.SongId).Distinct().Count() == prospect.LiveSet.Count(s => s.SongId != null), "distinct live covers");
				evidence.Add(new { stage = "live-set", artist = prospect.Artist.stageName, genre = prospect.Artist.primaryGenre.ToString(), songs = prospect.LiveSet.Select(s => new { s.Title, s.SongId, s.SourceTag, s.ReferenceMasterId }).ToArray() });
			}
			await Screen("02-scouting");
			await Press("COMPARE HEARD MATERIAL");
			Check(Descendants(panel).OfType<PolarComparisonWidget>().Any(), "scouting comparison opens");
			await Screen("02b-heard-comparison");
			Descendants(panel).OfType<AcceptDialog>().Single(w => w.Visible).EmitSignal(AcceptDialog.SignalName.Confirmed);
			await Frame();
			var chosen = Desk.Slate.OrderBy(p => CompositionCatalogService.GetProfessionalForGenre(p.Artist.primaryGenre).Count > 0 ? 0 : 1)
				.ThenBy(p => GenreCatalog.Get(p.Artist.primaryGenre).Family == GenreFamily.Rock ? 0 : 1).ThenBy(p => p.AskingAdvance).First();
			artistId = chosen.Artist.artistId;
			Action(Desk.FollowUp(chosen, out message), "follow up", message);
			Check(chosen.HeardCount == chosen.LiveSet.Count, "follow-up reveals complete set");
			Action(Desk.ApproachToSign(chosen, out message), "open contract", message);
			TimeManager.Instance.EndDay();
			var terms = chosen.Talk?.ask ?? chosen.Baseline;
			bool signed = chosen.Talk != null
				? Desk.TableOffer(chosen.Talk, terms.Advance, terms.RoyaltyRate, terms.TermYears, terms.SinglesObligation, terms.LabelOwnsPublishing, terms.ArtistCreativeControl, out message)
				: Desk.OfferContract(chosen, terms.Advance, terms.RoyaltyRate, terms.TermYears, terms.SinglesObligation, terms.LabelOwnsPublishing, terms.ArtistCreativeControl, out message);
			Action(signed && Act.labelId == Desk.Label.labelId, "sign at asked terms", message);
			await Press("ROSTER");
			await Press("MANAGE");
			await Screen("03-repertoire");
			await Press("TEACH A COVER");
			await Screen("04-cover-catalog");
			var catalog = Desk.CoverCatalogFor(Act);
			Check(catalog.Count > 0 && catalog.Select(c => c.SongId).Distinct().Count() == catalog.Count, "catalog is available and distinct");
			Check(catalog.Select(c => c.SongId).SequenceEqual(Desk.CoverCatalogFor(Act).Select(c => c.SongId)), "catalog reopen order stable");
			var cover = catalog.First();
			await Press("COMPARE / PREVIEW");
			Check(Descendants(panel).OfType<PolarComparisonWidget>().Any(), "catalog compare opens perceived graph");
			await Screen("04b-cover-comparison");
			string referenceBefore = cover.ReferenceMasterId?.StartsWith("demo:") == false
				? JsonSerializer.Serialize(PolarSongMetadataService.Get(cover.ReferenceMasterId), SaveGameService.TestJsonOptions) : null;
			var teachButton = Descendants(panel).OfType<Button>().First(b => b.Text.StartsWith("TEACH (~") && !b.IsQueuedForDeletion());
			teachButton.EmitSignal(Button.SignalName.Pressed);
			await Frame();
			Check(Desk.RehearsalsFor(artistId).Any(r => r.SongId == cover.SongId), "real catalog teach button queues selected composition");
			RoundTrip("rehearsal");
			Check(Desk.RehearsalsFor(artistId).Single(r => r.SongId == cover.SongId).ReferenceMasterId == cover.ReferenceMasterId, "heard reference survives rehearsal save/load");
			var due = Desk.RehearsalsFor(artistId).Single(r => r.SongId == cover.SongId).ReadyDate;
			AdvanceTo(due);
			Check(Desk.RepertoireFor(artistId).Any(s => s.SongId == cover.SongId && s.ReferenceMasterId == cover.ReferenceMasterId), "rehearsed song arrives with heard reference");
			TimeManager.Instance.EndDay();
			await Press("COMMISSION A SONG", prefix: true);
			Check(Desk.IsCommissioning(artistId), "real commission button queues delivery");
			var commissionId = Desk.RehearsalsFor(artistId).Single(r => r.IsCommission).SongId;
			AdvanceTo(Desk.RehearsalsFor(artistId).Single(r => r.IsCommission).ReadyDate);
			Check(Desk.MaterialOptionsFor(Act).Any(c => c.SongId == commissionId), "commission is a named recordable composition");
			await Press("HIDE THE CATALOG");
			var options = Desk.MaterialOptionsFor(Act);
			var coverChoice = options.Single(c => c.SongId == cover.SongId);
			var commissionChoice = options.Single(c => c.SongId == commissionId);
			foreach (var check in Descendants(panel).OfType<CheckBox>().Where(c => !c.IsQueuedForDeletion()).ToList())
				check.ButtonPressed = check.Text == coverChoice.Describe() || check.Text == commissionChoice.Describe();
			var comparisonPick = Descendants(panel).OfType<OptionButton>().First(p => Enumerable.Range(0, p.ItemCount).Any(i => p.GetItemText(i) == coverChoice.Describe()));
			int coverIndex = Enumerable.Range(0, comparisonPick.ItemCount).First(i => comparisonPick.GetItemText(i) == coverChoice.Describe());
			comparisonPick.Select(coverIndex); comparisonPick.EmitSignal(OptionButton.SignalName.ItemSelected, coverIndex);
			await Screen("04c-studio-preview");
			await Press("BOOK THE ROOM");
			Check(Desk.Session?.Cuts.Count == 2, "real studio button books both named songs");
			Check(Desk.Session.Cuts.Select(c => c.Choice.SongId).Contains(cover.SongId), "session retains selected cover identity");
			await Press("Take 2", prefix: true);
			Check(Desk.Session.Cuts[0].KeptTake == 1, "real take button changes kept take");
			await Screen("05-takes");
			RoundTrip("pending-session");
			Check(Desk.Session?.Cuts.Count == 2 && Desk.Session.Cuts[0].KeptTake == 1, "paid session and kept takes survive load");
			Check(Desk.Session.Cuts.Single(c => c.Choice.SongId == cover.SongId).Choice.ReferenceMasterId == cover.ReferenceMasterId, "heard reference survives session save/load");
			await Press("PRINT MASTERS");
			Check(Desk.Session == null && Desk.Masters.Count == 2, "print creates two shelf masters");
			var a = Desk.Masters.Single(m => m.Record.songId == cover.SongId);
			var b = Desk.Masters.Single(m => m.Record.songId == commissionId);
			foreach (var master in Desk.Masters) {
				var metadata = PolarSongMetadataService.Get(master.Record.masterId);
				Check(metadata?.realizedFit != null && metadata.songId == master.Record.songId, "printed master has independent taxonomy and realized components");
				evidence.Add(new { stage = "master", master.SongTitle, master.Record.songId, master.Record.masterId, metadata.parentRecordingId, taxonomy = metadata.taxonomy.archetype.ToString(), metadata.realizedFit, master.Record.hookStrength, master.Record.productionQuality });
			}
			Check(PolarSongMetadataService.Get(a.Record.masterId).parentRecordingId == (cover.ReferenceMasterId.StartsWith("demo:") ? null : cover.ReferenceMasterId), "printed cover retains selected lineage");
			Check(referenceBefore == null || referenceBefore == JsonSerializer.Serialize(PolarSongMetadataService.Get(cover.ReferenceMasterId), SaveGameService.TestJsonOptions), "prior master unchanged by cover");
			var compositionAfter = CompositionCatalogService.GetSong(cover.SongId);
			// A first committed performance may legitimately freeze the provisional plasticity.
			Check(a.Record.songwriterNames.SequenceEqual(compositionAfter.credits.Select(w => w.writerName)), "cover retains composition writers");
			await Screen("06-printed-masters");
			await Press("CATALOG"); await Press("COMPARE PLAYBACK");
			Check(Descendants(panel).OfType<PolarComparisonWidget>().Any(g => !g.IsQueuedForDeletion()), "printed master playback comparison opens");
			await Screen("06b-master-playback");
			Descendants(panel).OfType<AcceptDialog>().Single(w => w.Visible).EmitSignal(AcceptDialog.SignalName.Confirmed);
			await Frame(); await Press("DISTRIBUTION");
			Action(Desk.AssembleSingle(a, b, out message), "assemble two-sided single", message);
			Action(Desk.OrderPressing(a.Record.recordId, 500, 120, out message), "order minimum pressing with promos", message);
			var arrival = Desk.PressingOrderFor(a.Record.recordId).Arrives;
			var now = TimeManager.Instance.CurrentDate;
			int days = Math.Max(1, (int)(new DateTime(arrival.year, arrival.month, arrival.day) - new DateTime(now.year, now.month, now.day)).TotalDays);
			Action(Desk.SetReleaseDate(Desk.Planned.Single(), days, 0, out message), "date release after plant arrival", message);
			await Screen("07-pressing");
			AdvanceTo(arrival);
			var released = Desk.ReleasedRecords.Single(r => r.baseRecord.recordId == a.Record.recordId).baseRecord;
			Check(released.songId == cover.SongId && released.bSideSongId == commissionId, "release retains two composition identities");
			Check(released.masterId == a.Record.masterId && released.bSideMasterId == b.Record.masterId, "release retains both durable masters");
			Check(PolarSongMetadataService.Get(released.bSideMasterId)?.realizedFit != null, "B-side fit survives removal from shelf");
			Check(Desk.StockFor(released.recordId).Remaining == 380 && Desk.StockFor(released.recordId).PromoRemaining == 120, "plant delivers exact sellable/promo split");
			Action(Desk.MailPromoCopies(released.recordId, Desk.Label.homeRegion, 2, out message), "mail real promotional copies", message);
			Check(Desk.StockFor(released.recordId).PromoRemaining == 118, "promotion consumes promo stock");
			RoundTrip("released-single");
			Check(Desk.ReleasedRecords.Single(r => r.baseRecord.recordId == released.recordId).baseRecord.bSideMasterId == b.Record.masterId, "two-sided release survives real save/load");
			Check(PolarSongMetadataService.Get(b.Record.masterId)?.realizedFit != null, "saved B-side master fit restored");
			CheckAlbums();
			await Press("CATALOG");
			await Screen("08-released-catalog");
			WriteSummary("pass");
			GD.Print($"POLAR_PLAYER_PLAYTEST_PASS run={run} seed={SimulationSeedBootstrap.RequestedSeed} save={run} cash={Desk.Label.cashReserves:F2} date={TimeManager.Instance.CurrentDate.ToShortString()} screenshots={screenshots}");
			if (!keepOpen) GetTree().Quit(0);
		} catch (Exception e) {
			evidence.Add(new { stage = "error", message = e.ToString() });
			if (run != null) WriteSummary("fail");
			GD.PushError("POLAR_PLAYER_PLAYTEST_FAIL " + e);
			GetTree().Quit(3);
		}
	}
	private async Task RefusalUiFixture() {
		Action(Desk.FoundLabel("Polar Refusal Fixture", DistanceModel.GetCities().First().cityId, FoundingArchetype.ExMusician, out var message), "found fixture", message);
		var (artist, cover) = PolarPlayerPerceptionChecks.RefusalFixture(Desk); artistId = artist.artistId;
		await Press("ROSTER"); await Press("MANAGE");
		var tier = Descendants(panel).OfType<OptionButton>().Single(p => p.ItemCount > 0 && p.GetItemText(0).Contains("/hr"));
		tier.Select(0); tier.EmitSignal(OptionButton.SignalName.ItemSelected, 0);
		void SelectCover() {
			foreach (var box in Descendants(panel).OfType<CheckBox>().Where(b => !b.IsQueuedForDeletion()).ToList()) box.ButtonPressed = box.Text == cover.Describe();
		}
		float cash = Desk.Label.cashReserves; int hour = TimeManager.Instance.CurrentHour;
		void Unpaid() => Check(Desk.Session == null && Desk.Label.cashReserves == cash && TimeManager.Instance.CurrentHour == hour, "dialog response before booking stays unpaid");
		SelectCover();
		Check(Descendants(panel).OfType<OptionButton>().Any(p => p.GetItemText(p.Selected) == cover.Describe()), "checked song becomes the visible preview");
		await Press("BOOK THE ROOM"); Unpaid();
		await Screen("09-refusal-dialog");
		await Press("SET THIS ASIDE"); Unpaid();
		Check(!Descendants(panel).OfType<CheckBox>().Any(b => b.ButtonPressed), "shelve deselects material");
		SelectCover(); await Press("BOOK THE ROOM"); await Press("USE THEIR OWN MATERIAL"); Unpaid();
		Check(Descendants(panel).OfType<CheckBox>().Any(b => b.ButtonPressed && b.Text.Contains("Our Own Tune")) &&
			!Descendants(panel).OfType<CheckBox>().Any(b => b.ButtonPressed && b.Text == cover.Describe()), "own-material response selects their song");
		SelectCover(); await Press("BOOK THE ROOM"); await Press("INSIST — BOOK THE ROOM");
		Check(Desk.Session != null && Desk.Label.cashReserves == cash - Desk.Session.Cost, "insist books once");
		await Screen("05-refusal-override-playback");
		WriteSummary("pass"); GD.Print($"POLAR_PLAYER_PLAYTEST_PASS run={run} refusalDialog=ok own=ok shelve=ok insist=ok unpaid=ok"); GetTree().Quit(0);
	}

	private void CheckAlbums() {
		int projects = 0, promos = 0, cuts = 0;
		foreach (var project in CompetitorManager.Instance.GetAlbumProjects()) {
			var album = project.albumRecord?.album;
			if (album == null) continue;
			var tracks = (album.nonSingleTracks ?? Array.Empty<AlbumTrack>()).Concat(album.trackRefs ?? Array.Empty<AlbumTrack>()).ToArray();
			foreach (var cut in tracks.Where(t => PolarSongMetadataService.Get(t.masterId)?.realizedFit != null)) {
				Check(PolarSongMetadataService.Get(cut.masterId).songId == cut.songId, "runtime album cut links correct composition/master");
				cuts++;
			}
			var promo = project.promoSingleRecord;
			if (promo != null && PolarSongMetadataService.Get(promo.masterId)?.realizedFit != null) {
				var cut = tracks.Single(t => t.sourceRecordId == promo.recordId);
				Check(cut.masterId == promo.masterId && cut.songId == promo.songId, "runtime album promo reuses composition/master");
				Check(cut.hookStrength == promo.hookStrength && cut.productionQuality == promo.productionQuality, "runtime promo does not apply execution loss twice");
				promos++;
			}
			projects++;
		}
		Check(cuts > 0 && promos > 0, "real world provides album cuts and promoted singles");
		evidence.Add(new { stage = "autonomous-album-runtime", projects, cuts, promos, playerAlbumUI = false });
	}

	private void RoundTrip(string stage) {
		var before = Desk.CaptureState();
		string oldJson = JsonSerializer.Serialize(before, SaveGameService.TestJsonOptions);
		Action(SaveGameService.Save(run, out var message), "save " + stage, message);
		PolarSongBehavior.UsePolarFitSelection = false;
		Action(SaveGameService.Load(run, out message), "load " + stage, message);
		Check(PolarSongBehavior.UsePolarFitSelection, "saved enabled flag restored");
		var after = Desk.CaptureState();
		Check(after.Log.Count == before.Log.Count + 1 && after.Log.Skip(1).SequenceEqual(before.Log), "load adds only its expected log entry");
		after.Log = before.Log;
		after.UnreadLogCount = before.UnreadLogCount;
		string newJson = JsonSerializer.Serialize(after, SaveGameService.TestJsonOptions);
		if (oldJson != newJson) {
			File.WriteAllText(Output(stage + "-before.json"), oldJson);
			File.WriteAllText(Output(stage + "-after.json"), newJson);
		}
		Check(oldJson == newJson, stage + " player state exactly round-trips apart from load notification");
	}
	private void AdvanceTo(GameDate date) {
		int limit = 45;
		while (TimeManager.Instance.CurrentDate < date && limit-- > 0) TimeManager.Instance.EndDay();
		Check(TimeManager.Instance.CurrentDate >= date, "bounded daily advancement completes");
	}
	private async Task Press(string text, bool prefix = false) {
		await Frame();
		await DismissPaper();
		var button = Descendants(panel).OfType<Button>().FirstOrDefault(b => !b.IsQueuedForDeletion() && b.IsVisibleInTree() && !b.Disabled && (prefix ? b.Text.StartsWith(text) : b.Text == text));
		Check(button != null, "available UI button: " + text);
		button.EmitSignal(Button.SignalName.Pressed);
		await Frame();
	}
	private async Task Screen(string name) {
		await Frame();
		await DismissPaper();
		if (name.StartsWith("04b-") || name.StartsWith("04c-") || name.StartsWith("05-")) {
			var scroll = Descendants(panel).OfType<ScrollContainer>().First();
			var graph = Descendants(panel).OfType<PolarComparisonWidget>().First(g => !g.IsQueuedForDeletion());
			scroll.ScrollVertical += (int)(graph.GlobalPosition.Y - scroll.GlobalPosition.Y - 90);
		}
		await Frame();
		File.WriteAllText(Output(name + "-ui.txt"), string.Join("\n", Descendants(panel).OfType<Control>().Where(c => c.IsVisibleInTree() && !c.IsQueuedForDeletion()).Select(c => c switch { Godot.Label l => l.Text, Button b => b.Text, _ => null }).Where(t => t != null)));
		if (screenshots) {
			await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
			Check(GetViewport().GetTexture().GetImage().SavePng(Output(name + ".png")) == Error.Ok, "screen captured: " + name);
		}
	}
	private async Task Frame() => await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	private async Task DismissPaper() {
		await Frame();
		var fold = Descendants(this).OfType<Button>().FirstOrDefault(b => !b.IsQueuedForDeletion() && b.IsVisibleInTree() && b.Text == "FOLD THE PAPER  ×");
		if (fold != null) { fold.EmitSignal(Button.SignalName.Pressed); await Frame(); }
	}
	private static IEnumerable<Node> Descendants(Node node) {
		foreach (var child in node.GetChildren(includeInternal: true)) { yield return child; foreach (var descendant in Descendants(child)) yield return descendant; }
	}
	private void Action(bool success, string stage, string message) { evidence.Add(new { stage, success, message, cash = Desk.Label?.cashReserves, date = TimeManager.Instance.CurrentDate.ToShortString() }); Check(success, stage + ": " + message); }
	private void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); }
	private void CheckSessionSaveContract() {
		Check(JsonSerializer.Deserialize<PlayerSaveData>("{}", SaveGameService.TestJsonOptions).Session == null, "old player saves have an empty console");
		var original = new PlayerDesk.Song { SongId = "session-save-link", Title = "Own song", Hook = 0 };
		var session = new PlayerDesk.PendingSession { ArtistId = "session-save-act", Tier = PlayerDesk.StudioTier.Budget, Hours = 3, Cost = 30, Date = GameDate.StartDate };
		var cut = new PlayerDesk.SessionCut { Choice = new PlayerDesk.MaterialChoice { WrittenSong = original, Kind = PlayerDesk.MaterialKind.Original, Title = original.Title }, KeptTake = 1 };
		cut.Takes.Add(new PlayerDesk.SessionTake { Number = 1, Hook = 0, Production = 0 });
		cut.Takes.Add(new PlayerDesk.SessionTake { Number = 2, Hook = .4f, Production = .6f });
		session.Cuts.Add(cut);
		var saved = JsonSerializer.Deserialize<PendingSessionSaveData>(JsonSerializer.Serialize(PendingSessionSaveData.From(session), SaveGameService.TestJsonOptions), SaveGameService.TestJsonOptions);
		var songbookCopy = new PlayerDesk.Song { SongId = original.SongId, Title = original.Title, Hook = 0 };
		var restored = saved.ToSession(new[] { songbookCopy });
		Check(ReferenceEquals(restored.Cuts[0].Choice.WrittenSong, songbookCopy), "loaded original links actual songbook entry");
		Check(restored.Cuts[0].KeptTake == 1 && restored.Cuts[0].Takes[0].Hook == 0 && restored.Cost == 30 && restored.Date == GameDate.StartDate, "session DTO preserves kept index, explicit zero, paid cost and date");
		evidence.Add(new { stage = "session-save-contract", legacyAbsent = "empty", writtenSongLink = "same songbook object", explicitZero = "preserved" });
	}
	private void WriteSummary(string status) => File.WriteAllText(Output("summary.json"), JsonSerializer.Serialize(new { status, run, seed = SimulationSeedBootstrap.RequestedSeed, enabled = PolarSongBehavior.UsePolarFitSelection, artistId, saveSlot = run, evidence }, new JsonSerializerOptions { WriteIndented = true, IncludeFields = true }));
}
