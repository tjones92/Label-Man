using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Godot;

/// <summary>Studio rules v2 (slices 10-13): members' second roles, the creative palette (key, tempo, ends, balance,
/// incidents, ideas and who resists them), new arrangements with hired horns and arrangers, tape stock, lockouts and
/// evening dates, a hired producer, delegation limits and stops, standing house crews and other labels' renown, dated
/// equipment with capture layouts, vocal and part overdubs, splices and the vocal-forward finish, period lengths and the
/// radio's length preference. Every check runs on the real desk.
/// Run: SimTools/StudioCraftRunner.tscn -- --seed=N [--studio-ui-capture]</summary>
public partial class StudioCraftRunner : Node {
    public override async void _Ready() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException(why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        T Clone<T>(T value) => JsonSerializer.Deserialize<T>(Json(value), SaveGameService.TestJsonOptions);
        IEnumerable<Node> Walk(Node node) { yield return node; foreach (var c in node.GetChildren()) foreach (var n in Walk(c)) yield return n; }
        async System.Threading.Tasks.Task Screenshot(string name) {
            if (DisplayServer.GetName() == "headless" || !OS.GetCmdlineUserArgs().Contains("--studio-ui-capture")) return;
            for (int i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
            GetViewport().GetTexture().GetImage().SavePng(ProjectSettings.GlobalizePath("res://SimLogs/studio-craft-" + name + ".png"));
        }
        object Private(string method, params object[] args) =>
            typeof(PlayerDesk).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Invoke(PlayerDesk.Instance, args);
        bool Rejected(Func<bool> load) { try { return !load(); } catch (InvalidOperationException) { return true; } catch (TargetInvocationException) { return true; } }
        try {
            Check(StudioExperience.Enabled, "Incremental rollout enabled");
            var desk = PlayerDesk.Instance; var clock = TimeManager.Instance;
            var startDate = clock.CurrentDate;

            // ================= Second roles =================
            var everyone = ArtistManager.Instance.GetAllArtists().SelectMany(a => a.members.Select(m => (Act: a, M: m))).ToList();
            Check(everyone.Count > 1000 && everyone.All(x => x.M.secondaryRoleVersion == MemberRoleService.CurrentVersion),
                $"Every generated member has a second role assigned ({everyone.Count(x => x.M.secondaryRoleVersion == 0)} unassigned of {everyone.Count})");
            Check(everyone.All(x => MemberRoleService.Derive(x.M, x.Act.primaryGenre, x.Act.formedYear) == MemberRoleService.Derive(x.M, x.Act.primaryGenre, x.Act.formedYear)),
                "Second roles are a pure keyed function");
            Check(everyone.All(x => x.M.secondaryRole != x.M.primaryRole || x.M.secondaryRole == MusicianRole.None) &&
                everyone.All(x => !(x.M.isLeadVocalist || x.M.primaryRole is MusicianRole.LeadVocals or MusicianRole.BackingVocals) || x.M.secondaryRole != MusicianRole.BackingVocals),
                "Nobody doubles their own part, and singers' second role is never harmony");
            var bandsAll = ArtistManager.Instance.GetAllArtists().Where(a => a.type == ArtistType.Band && !a.instrumentalPerformance && a.members.Any(m => m.isActive && m.primaryRole == MusicianRole.Drums)).ToList();
            int harmonyBands = bandsAll.Count(a => a.members.Any(m => m.isActive && !m.isLeadVocalist && m.secondaryRole == MusicianRole.BackingVocals));
            float Share(Genre g) { var b = bandsAll.Where(a => a.primaryGenre == g).ToList(); return b.Count == 0 ? -1 : b.Count(a => a.members.Any(m => !m.isLeadVocalist && m.secondaryRole == MusicianRole.BackingVocals)) / (float)b.Count; }
            var folkSingers = everyone.Where(x => x.Act.primaryGenre == Genre.Folk && x.M.isLeadVocalist).ToList();
            float folkGuitar = folkSingers.Count == 0 ? 0 : folkSingers.Count(x => x.M.secondaryRole == MusicianRole.RhythmGuitar) / (float)folkSingers.Count;
            int needBacking = bandsAll.Count(PlayerDesk.StudioNeedsBacking);
            GD.Print($"STUDIO_CRAFT roles bands={bandsAll.Count} withHarmonySinger={harmonyBands} needBacking={needBacking} surf={Share(Genre.SurfRock):F2} rock={Share(Genre.RockAndRoll):F2} " +
                $"country={Share(Genre.Country):F2} soul={Share(Genre.Soul):F2} jazz={Share(Genre.Jazz):F2} folkSingersOnGuitar={folkGuitar:F2}");
            Check(harmonyBands > bandsAll.Count / 4 && harmonyBands < bandsAll.Count, "A real share of bands has a member who sings harmony on stage");
            Check(folkGuitar > .3f, "Folk singers mostly play guitar too");
            var restoredMember = Clone(everyone.First(x => x.M.secondaryRole != MusicianRole.None).M);
            Check(restoredMember.secondaryRole == everyone.First(x => x.M.secondaryRole != MusicianRole.None).M.secondaryRole && restoredMember.secondaryRoleVersion == 1, "A second role survives a save");
            var legacy = Clone(everyone[0].M); legacy.secondaryRole = MusicianRole.LeadVocals; legacy.secondaryRoleVersion = 0;
            Check(MemberRoleService.Secondary(legacy) == MemberRoleService.Derive(legacy, null, 1960) && legacy.secondaryRoleVersion == 1, "A member from an older save gets the role on first read, not LeadVocals");
            var swap = Clone(everyone.First(x => x.M.primaryRole == MusicianRole.Piano && x.M.secondaryRole == MusicianRole.Organ).M);
            swap.primaryRole = MusicianRole.Organ; MemberRoleService.OnPrimaryChanged(swap, MusicianRole.Piano, null, 1961);
            Check(swap.secondaryRole == MusicianRole.Piano, "A pianist who moves to the organ doubles on piano");

            // ================= The label and the acts =================
            Check(desk.FoundLabel("Craft Studio", "nashville", out string message), "Found: " + message);
            desk.Label.cashReserves = 80000;
            void Morning(GameDate date) => clock.RestoreClock(date, 9, 0);
            SimulatedArtist Live(SimulatedArtist a) => ArtistManager.Instance.GetArtist(a.artistId);
            var homeLabels = new Dictionary<string, string>();
            void Sign(SimulatedArtist a, bool control = false) {
                a = Live(a); if (a.labelId == desk.Label.labelId) { a.artistCreativeControl = control; return; }
                homeLabels[a.artistId] = a.labelId;
                ChartManager.Instance.GetLabelById(a.labelId)?.roster.Remove(a);
                a.labelId = desk.Label.labelId; a.artistCreativeControl = control;
                desk.Label.roster.Add(a);
            }
            bool Free(SimulatedArtist a, GameDate d) => LocalSceneRoomService.StudioParticipantsFree(desk.Label.labelId, a.members.Select(m => m.personId), d, 0, 1440);
            var pool = ArtistManager.Instance.GetAllArtists().Where(a => a.labelId != null && a.lifecycleStatus == ArtistLifecycleStatus.Active && !a.instrumentalPerformance &&
                a.members.Count(m => m.isActive && m.lifeState == MemberLifeState.Active) >= 3).OrderBy(a => a.artistId, StringComparer.Ordinal).ToList();
            var band = pool.First(a => a.members.Any(m => m.primaryRole == MusicianRole.Drums) && a.members.Any(m => m.primaryRole is MusicianRole.LeadGuitar or MusicianRole.RhythmGuitar) && Free(a, startDate));
            var harmonyAct = pool.FirstOrDefault(a => a != band && a.members.Any(m => m.isActive && !m.isLeadVocalist && MemberRoleService.SingsHarmony(m)) && Free(a, startDate));
            Check(harmonyAct != null, "An act with a harmony singer exists");
            Sign(band);
            var one = new List<PlayerDesk.MaterialChoice> { new() { Kind = PlayerDesk.MaterialKind.Original, Title = "Craft Side", Detail = "their own" } };
            var two = new List<PlayerDesk.MaterialChoice> { one[0], new() { Kind = PlayerDesk.MaterialKind.Original, Title = "Craft Flip", Detail = "their own" } };

            // ================= Session players in Philadelphia (the slice-9 note) =================
            int phillyDay = LocalSceneRoomService.Day(clock.CurrentDate);
            int phillyMin = Enumerable.Range(0, 28).Min(d => SessionEmploymentService.ListeningCandidates("philadelphia", phillyDay + d).Count);
            GD.Print($"STUDIO_CRAFT philadelphia session players, fewest on any day of the first four weeks: {phillyMin}");
            Check(phillyMin > 0, "Philadelphia has session players every day of the opening month");

            // ================= Dated equipment =================
            var roomIds = desk.StudioRoomIds().ToList();
            var offers = roomIds.Select(id => desk.StudioOffer(id, band.artistId)).Where(o => o != null).ToList();
            Check(offers.All(o => o.Room.Tracks >= 1 && o.Room.Tracks <= 8), "Every room has a dated machine");
            var budget = StudioRoomCatalog.Local("nashville", "deepsouth", PlayerDesk.StudioTier.Budget);
            Check(StudioEquipment.TracksFor(budget, PlayerDesk.StudioTier.Budget, 1960) == 1 && StudioEquipment.TracksFor(budget, PlayerDesk.StudioTier.Budget, 1969) >= 3,
                "A cheap local service is mono in 1960 and has a multitrack by the end of the decade");
            var three = offers.FirstOrDefault(o => o.Local && o.Room.Tracks >= 3 && o.Locked == null);
            Check(three != null, "A local room in Nashville has a three-track machine in 1960: " + string.Join(", ", offers.Select(o => $"{o.Room.Name}={o.Room.Tracks}")));
            var mono = offers.FirstOrDefault(o => o.Local && o.Room.Tracks == 1 && o.Locked == null);

            // ================= Booking options =================
            Morning(startDate);
            float cash0 = desk.Label.cashReserves;
            if (mono != null) {
                Check(!desk.StartSession(Live(band), one, mono.Tier, 3, out message, overrideRefusal: true, roomId: mono.Room.Id, options: new StudioBookingOptions { Layout = StudioLayout.VocalApart }) &&
                    desk.Session == null && desk.Label.cashReserves == cash0, "A mono room refuses a separated-vocal layout: " + message);
            }
            Check(!desk.StartSession(Live(band), one, three.Tier, 3, out message, overrideRefusal: true, roomId: three.Room.Id, options: new StudioBookingOptions { Evening = true }) && desk.Session == null,
                "An evening rate can't be booked in the morning: " + message);
            float rate = PlayerDesk.StudioRoomCost(three.Room, 4);
            Check(desk.StartSession(Live(band), two, three.Tier, 4, out message, overrideRefusal: true, roomId: three.Room.Id,
                options: new StudioBookingOptions { Layout = StudioLayout.ThreeTrack, Days = 2 }), "Book a two-day three-track lockout: " + message);
            var session = desk.Session;
            Check(session.RulesVersion == PlayerDesk.StudioRulesCurrent && session.Work?.Layout == StudioLayout.ThreeTrack && session.Work.LockoutDays == 2 &&
                Math.Abs(cash0 - desk.Label.cashReserves - Mathf.Round(rate * PlayerDesk.StudioLockoutFactor(2))) < 1.01f, $"Rules v2 date, layout and block rate charged once ({cash0 - desk.Label.cashReserves} vs {rate * 1.8f})");
            Check(session.Work.StockPasses >= PlayerDesk.StudioStockIncluded(2, 8, 12) - 2, "The quote includes tape stock");

            // ================= Shape: key, tempo, ends =================
            int t0 = clock.CurrentHour * 60 + clock.CurrentMinute; int actions0 = session.ActionNumber; ulong rng0 = GameRandom.Randi();
            GameRandom.Seed(4242); uint expect = GameRandom.Randi(); GameRandom.Seed(4242);
            string proposal = desk.StudioShapeProposal(0, StudioShapeChange.LowerKey);
            Check(proposal.Contains("minutes") && clock.CurrentHour * 60 + clock.CurrentMinute == t0 && session.ActionNumber == actions0 && GameRandom.Randi() == expect,
                "A shape proposal is free: no time, no action, no gameplay randomness");
            Check(desk.CutStudioTake(0, out message), "First pass, original shape: " + message);
            var firstTake = session.Cuts[0].Takes[^1];
            Check(firstTake.Work != null && firstTake.Work.KeyShift == 0 && firstTake.Work.Layout == StudioLayout.ThreeTrack && firstTake.Work.DurationSeconds == session.Cuts[0].Structure.DurationSeconds,
                "A take records the shape and layout it was cut in");
            int power0 = (int)(session.Cuts[0].Structure.PowerDemand * 1000), length0 = session.Cuts[0].Structure.DurationSeconds;
            session.Cuts[0].Rehearsals = 4;
            Check(desk.ShapeStudio(0, StudioShapeChange.LowerKey, out message), "Lower the key: " + message);
            Check((int)(session.Cuts[0].Structure.PowerDemand * 1000) < power0 && session.Cuts[0].Rehearsals == 2 && session.Cuts[0].Work.BaseStructure != null && firstTake.Work.KeyShift == 0,
                "Lowering the key eases the vocal demand, unsettles half the drilling, and leaves the earlier take as cut");
            Check(desk.ShapeStudio(0, StudioShapeChange.TrimEnds, out message) && session.Cuts[0].Structure.DurationSeconds == length0 - PlayerDesk.StudioTrimSeconds,
                $"Trimming the ends shortens the side ({session.Cuts[0].Structure.DurationSeconds} from {length0})");
            Check(!desk.ShapeStudio(0, StudioShapeChange.TrimEnds, out _), "The same change twice is refused");
            Check(desk.CutStudioTake(0, out message), "Pass in the new shape: " + message);
            var shapedTake = session.Cuts[0].Takes[^1];
            Check(shapedTake.Work.KeyShift == -1 && shapedTake.Work.Trimmed && shapedTake.Work.DurationSeconds == length0 - PlayerDesk.StudioTrimSeconds, "The new pass carries the new shape");
            Check(PlayerDesk.StudioStructureOf(session.Cuts[0], firstTake).DurationSeconds == length0, "The first take's structure is still the full-length one");

            // ================= Identical draws: the balance and the layout move a pass by their effect =================
            var probe = desk.CaptureState();
            var probeClock = (clock.CurrentDate, clock.CurrentHour, clock.CurrentMinute);
            bool Rewind(PlayerSaveData state, out string why) { clock.RestoreClock(probeClock.CurrentDate, probeClock.CurrentHour, probeClock.CurrentMinute); return desk.RestoreState(Clone(state), out why); }
            Check(desk.CutStudioTake(1, out message), "Flip: natural balance: " + message);
            var natural = desk.Session.Cuts[1].Takes[^1];
            Check(Rewind(probe, out message), "Back to before the flip's pass: " + message);
            desk.Session.Cuts[1].Work = new StudioCutWork { Balance = StudioBalance.BandForward };
            Check(desk.CutStudioTake(1, out message), "Flip: band forward: " + message);
            var forward = desk.Session.Cuts[1].Takes[^1];
            Check(natural.Incomplete || forward.Incomplete || natural.Work.Incident != StudioIncident.None || forward.Work.Incident != StudioIncident.None ||
                Math.Abs(forward.Capture - Math.Clamp(natural.Capture - .02f, 0, 1)) < .0005f && forward.Feel >= natural.Feel,
                $"Under identical draws, opening the band up costs capture and adds feel ({natural.Capture:F3}->{forward.Capture:F3}, {natural.Feel:F3}->{forward.Feel:F3})");
            Check(Rewind(probe, out message), "Back again: " + message);
            session = desk.Session;

            // ================= Incidents, faults and ideas =================
            int incidents = 0, buzz = 0, strings = 0;
            var seenKinds = new HashSet<StudioIncident>();
            for (int i = 0; i < 6 && desk.StudioMinutesLeft(session) > PlayerDesk.StudioPassMinutes(session.Cuts[1]) + PlayerDesk.FinishMinutes(session) + 5; i++) {
                if (PlayerDesk.StudioStockLeft(session) <= 0) break;
                if (!desk.CutStudioTake(1, out message)) break;
                var t = session.Cuts[1].Takes[^1];
                if (t.Work.Incident != StudioIncident.None) { incidents++; seenKinds.Add(t.Work.Incident); }
                if (t.Work.Incident == StudioIncident.AmpBuzz) buzz++;
                if (t.Work.Incident == StudioIncident.BrokenString) { strings++; Check(t.Incomplete && t.FalseStart && t.FaultPersonId == null, "A broken string stops the pass without blaming a part"); }
            }
            GD.Print($"STUDIO_CRAFT incidents on the flip: {incidents} ({string.Join(",", seenKinds)})");
            if (buzz > 0) {
                Check(desk.StudioHasFault(PlayerDesk.StudioBuzzFault), "A buzz stays in the room until it is found");
                Check(desk.FixStudioFault(out message) && !desk.StudioHasFault(PlayerDesk.StudioBuzzFault), "Fixing the buzz clears it: " + message);
            }
            Check(!desk.FixStudioFault(out _), "Nothing to fix is refused");
            // Incidents over many keyed passes: the chance and kinds are bounded.
            var census = new Dictionary<StudioIncident, int>();
            int rolled = 0;
            for (int i = 0; i < 400; i++) {
                var s = desk.Session; var cut = s.Cuts[0];
                var take = new PlayerDesk.SessionTake { TakeId = $"probe|{i}", Number = 900 + i, Vocal = .5f, Ensemble = .5f, Feel = .5f, Capture = .5f, Minutes = 12, Work = new StudioTakeWork() };
                Private("StudioIncidentRoll", s, cut, Live(band), take, .3f, $"incident-census|{i}");
                census[take.Work.Incident] = census.GetValueOrDefault(take.Work.Incident) + 1; rolled++;
                s.Work.Faults.Clear();
            }
            float rate1 = 1 - census.GetValueOrDefault(StudioIncident.None) / (float)rolled;
            GD.Print($"STUDIO_CRAFT incident census of {rolled}: {string.Join(", ", census.OrderBy(k => k.Key).Select(k => $"{k.Key}={k.Value}"))}");
            Check(rate1 > .05f && rate1 < .25f && census.Keys.Count(k => k != StudioIncident.None) >= 3, $"Incidents are occasional ({rate1:P0}) and varied");
            Check(Rewind(probe, out message), "Restore after the census: " + message);
            session = desk.Session;

            // Ideas: stable, free and declinable; resistance costs goodwill.
            var ideasA = desk.StudioIdeas(0).Select(i => i.Id).ToList();
            var ideasB = desk.StudioIdeas(0).Select(i => i.Id).ToList();
            Check(ideasA.SequenceEqual(ideasB) && ideasA.Count <= 2, "Ideas are the same on every read, at most two");
            session.Cuts[0].Takes.Add(new PlayerDesk.SessionTake { TakeId = "fixture-pickup", Number = 50, Vocal = .5f, Ensemble = .5f, Feel = .5f, Capture = .5f, Minutes = 12,
                ProducerNote = "x", EngineerNote = "x", PerformerNote = "x", BandNote = "x",
                Work = new StudioTakeWork { Incident = StudioIncident.CompellingPickup, IncidentPersonId = Live(band).members.First(m => m.isActive).personId, IncidentNote = "A pickup." } });
            var pickupIdea = desk.StudioIdeas(0).FirstOrDefault(i => i.Pickup);
            Check(pickupIdea != null, "A pickup somebody played becomes an idea from that person");
            Check(desk.PassOnStudioIdea(0, pickupIdea.Id, out message) && desk.StudioIdeas(0).All(i => i.Id != pickupIdea.Id), "Passing on an idea removes it for this side");
            session.DeclinedCallIns.Remove($"{PlayerDesk.StudioIdeaDeclinePrefix}0:{pickupIdea.Id}");
            Check(desk.TakeStudioIdea(0, pickupIdea.Id, out message) && session.Cuts[0].Work.KeepPickup, "Taking the pickup up puts it in the arrangement: " + message);
            session.Cuts[0].Takes.RemoveAt(session.Cuts[0].Takes.Count - 1);
            var (favors, resists) = ((List<string>, List<string>))typeof(PlayerDesk).GetMethod("StudioStance", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { Live(band), StudioArrangement.SimplerRhythm });
            Check(resists.Contains(Live(band).members.First(m => m.isActive && m.primaryRole == MusicianRole.Drums).firstName), "The drummer resists a simpler rhythm figure");
            float tension0 = session.Tension;
            Check(desk.ArrangeStudio(0, StudioArrangement.SimplerRhythm, out message) && session.Tension > tension0 && session.ActionLog.Any(l => l.Contains("not happily")),
                "Going ahead over an objection raises room tension and is logged: " + message);

            // ================= New arrangements, horns and arrangers =================
            Check(Rewind(probe, out message), "Fresh time for the arrangements: " + message);
            session = desk.Session;
            bool hasPiano = Live(band).members.Any(m => m.isActive && (MemberRoleService.Plays(m, MusicianRole.Piano) || MemberRoleService.Plays(m, MusicianRole.Organ)));
            string pianoProposal = desk.StudioArrangementProposal(0, StudioArrangement.PianoLed);
            Check(hasPiano ? !pianoProposal.Contains("Not possible yet") : pianoProposal.Contains("Nobody on this date plays piano"), "Piano-led needs somebody who plays piano (a second role counts)");
            bool hasHorn = Live(band).members.Any(m => m.isActive && (MemberRoleService.Plays(m, MusicianRole.Saxophone) || MemberRoleService.Plays(m, MusicianRole.Trumpet)));
            if (!hasHorn) Check(!desk.ArrangeStudio(0, StudioArrangement.HornLine, out message) && message.Contains("horn"), "A horn line needs a horn: " + message);
            var hornOffer = desk.StudioHornOffer();
            if (hornOffer != null) {
                float before = desk.Label.cashReserves;
                Check(desk.HireStudioCrew(StudioHireKind.Horn, out message) && session.Hires.Any(h => h.Kind == StudioHireKind.Horn && h.Valid && !h.Replaces) &&
                    Math.Abs(before - desk.Label.cashReserves - hornOffer.Fee) < .01f, "Hire a horn from town, paid once: " + message);
                if (PlayerDesk.StudioPartWriter(session, Live(band)) == null) {
                    Check(!desk.ArrangeStudio(0, StudioArrangement.HornLine, out message) && message.Contains("write"), "Without a writer the horn line waits for an arranger: " + message);
                    if (desk.StudioArrangerOffer() != null) Check(desk.HireStudioCrew(StudioHireKind.Arranger, out message), "Hire an arranger: " + message);
                }
                if (PlayerDesk.StudioPartWriter(session, Live(band)) != null) {
                    Check(desk.ArrangeStudio(0, StudioArrangement.HornLine, out message), "Prepare the horn line: " + message);
                    Check(desk.CutStudioTake(0, out message), "A pass with the horns: " + message);
                    var hornTake = session.Cuts[0].Takes[^1];
                    var credits = PlayerDesk.StudioSessionCredits(session, hornTake);
                    var plainCredits = PlayerDesk.StudioSessionCredits(session, firstTake);
                    Check(credits.Any(c => c.Contains(hornOffer.Name)) && !plainCredits.Any(c => c.Contains(hornOffer.Name)), "The horn player is credited only on horn takes");
                }
            } else GD.Print("STUDIO_CRAFT no horn player free in town today");
            Check(desk.StudioArrangementOptions(new PlayerDesk.PendingSession { RulesVersion = 0 }).Count() == 3, "Dates before rules v2 keep the original three arrangements");

            // ================= Tape stock =================
            var stockState = desk.CaptureState();
            session.Work.PassesUsed = session.Work.StockPasses + session.Work.StockBought;
            Check(!desk.CutStudioTake(1, out message) && message.Contains("tape"), "With the stock used up a pass is refused: " + message);
            int reusable = Enumerable.Range(0, session.Cuts[1].Takes.Count).FirstOrDefault(i => PlayerDesk.StudioTakeReusable(session.Cuts[1], i), -1);
            if (reusable >= 0) {
                var gone = session.Cuts[1].Takes[reusable];
                Check(desk.ReuseStudioTape(1, reusable, out message) && gone.Work.Erased && PlayerDesk.StudioStockLeft(session) == 1, "Recording over a take frees one pass: " + message);
                desk.KeepTake(1, reusable);
                Check(session.Cuts[1].KeptTake != reusable && StudioTakeEvidence.Choose(session.Cuts[1].Takes) != reusable, "A recorded-over take can't be kept or chosen");
                Check(!desk.ReuseStudioTape(1, reusable, out _), "A take can't be recorded over twice");
            }
            float cashStock = desk.Label.cashReserves, cost0 = session.Cost, debt0 = Live(band).unrecoupedAdvance;
            Check(desk.BuyStudioStock(out message) && Math.Abs(cashStock - desk.Label.cashReserves - PlayerDesk.StudioReelPrice) < .01f && session.Cost == cost0 + PlayerDesk.StudioReelPrice &&
                Math.Abs(Live(band).unrecoupedAdvance - debt0 - PlayerDesk.StudioReelPrice) < .01f, "A reel is studio cost: paid once, on the date, recouped like the room: " + message);
            Check(desk.RestoreState(Clone(stockState), out message), "Back to before the stock test: " + message);
            session = desk.Session;

            // ================= Overdubs and splices =================
            Check(desk.CutStudioTake(1, out message), "A flip pass on three tracks: " + message);
            int source = Enumerable.Range(0, session.Cuts[1].Takes.Count).Last(i => !session.Cuts[1].Takes[i].Incomplete && session.Cuts[1].Takes[i].Work?.Erased != true);
            var src = session.Cuts[1].Takes[source];
            string srcJson = Json(src);
            Check(desk.StudioVocalOverdubBlocked(1, source) == null, "A three-track take allows a new vocal: " + desk.StudioVocalOverdubBlocked(1, source));
            Check(desk.OverdubStudioVocal(1, source, out message), "Sing it again over the backing: " + message);
            var revocal = session.Cuts[1].Takes[^1];
            Check(revocal.Work.Derivation == StudioDerivation.VocalOverdub && revocal.Work.SourceTakeId == src.TakeId && revocal.Ensemble == src.Ensemble &&
                Json(src) == srcJson, "The new vocal keeps the backing's ensemble, and the source take is untouched");
            var parts = desk.StudioOverdubOptions(1, source).ToList();
            Check(parts.Count == 3 && parts.Any(p => p.Part == StudioOverdubPart.Handclaps && p.Blocked == null), "A spare track can take handclaps");
            Check(desk.OverdubStudioPart(1, source, StudioOverdubPart.Handclaps, out message) && session.Cuts[1].Takes[^1].Work.Parts.SequenceEqual(new[] { StudioOverdubPart.Handclaps }) &&
                session.Cuts[1].Takes[^1].Feel >= src.Feel, "Handclaps overdubbed onto the spare track lift the feel: " + message);
            int clapped = session.Cuts[1].Takes.Count - 1;
            Check(desk.StudioOverdubOptions(1, clapped).All(p => p.Blocked != null), "A spare track used is a spare track gone");
            desk.KeepTake(1, source);
            Check(PlayerDesk.StudioFinishes(session.Room, src).Contains(StudioFinish.VocalForward),
                "A separated vocal can be brought up at the finish");
            Check(desk.FinishStudioSide(1, StudioFinish.VocalForward, out message), "Bring the vocal up: " + message);
            desk.KeepTake(1, source);
            var monoTake = new PlayerDesk.SessionTake { Work = new StudioTakeWork { Layout = StudioLayout.Together } };
            Check(!PlayerDesk.StudioFinishes(session.Room, monoTake).Contains(StudioFinish.VocalForward), "A take with everyone together can't have its vocal brought up");
            // A splice: two passes of the same shape on the A side.
            Check(desk.CutStudioTake(0, out message) && desk.CutStudioTake(0, out message), "Two A-side passes in the same shape: " + message);
            var aTakes = session.Cuts[0].Takes;
            int sa = aTakes.Count - 2, sb = aTakes.Count - 1;
            string block = desk.StudioSpliceBlocked(0, sa, sb);
            if (block == null && desk.StudioSplicePoint(session.Cuts[0], sa, sb) >= 1) {
                Check(desk.SpliceStudio(0, sa, sb, out message), "Splice: " + message);
                var spliced = aTakes[^1];
                Check(spliced.Work.Derivation == StudioDerivation.Splice && spliced.Work.SourceTakeId == aTakes[sa].TakeId && spliced.Work.SecondSourceTakeId == aTakes[sb].TakeId &&
                    !spliced.Incomplete, "The splice is a new take built from both sources");
                Check(desk.StudioVocalOverdubBlocked(0, aTakes.Count - 1) != null, "A spliced tape takes no overdub");
            } else GD.Print("STUDIO_CRAFT splice not available: " + block);
            Check(desk.StudioSpliceBlocked(0, 0, aTakes.Count - 1) != null || aTakes[0].Work.KeyShift == aTakes[^1].Work.KeyShift, "Takes in different keys can't be spliced");

            // ================= Save and load =================
            var saved = desk.CaptureState();
            var round = Clone(saved);
            Check(desk.RestoreState(round, out message), "v21 date save/load: " + message);
            var back = desk.Session;
            Check(back.Work.Layout == StudioLayout.ThreeTrack && back.Work.LockoutDays == 2 && back.Cuts[0].Work.KeyShift == -1 && back.Cuts[0].Work.Trimmed &&
                back.Cuts[1].Takes.Any(t => t.Work.Derivation == StudioDerivation.VocalOverdub) && Json(PendingSessionSaveData.From(back)) == Json(PendingSessionSaveData.From(session)),
                "Every piece of v21 work survives a save");
            var broken = Clone(saved); broken.Session.Flow.RulesVersion = PlayerDesk.StudioRulesCurrent + 1;
            Check(Rejected(() => desk.RestoreState(broken, out _)), "A future rules version is refused");
            broken = Clone(saved); broken.Session.Flow.RulesVersion = 0;
            Check(Rejected(() => desk.RestoreState(broken, out _)), "v21 work on a pre-v2 date is refused");
            broken = Clone(saved); broken.Session.Cuts[1].Takes[^1].Work.SourceTakeId = "nowhere";
            Check(Rejected(() => desk.RestoreState(broken, out _)), "A derived take pointing at a missing source is refused");
            broken = Clone(saved); var keptSaved = broken.Session.Cuts[1].Takes[broken.Session.Cuts[1].KeptTake]; keptSaved.Work.Erased = true;
            Check(Rejected(() => desk.RestoreState(broken, out _)), "A kept take that was recorded over is refused");
            broken = Clone(saved); broken.Session.Flow.Work.PassesUsed = 999;
            Check(Rejected(() => desk.RestoreState(broken, out _)), "More passes than stock is refused");
            Check(desk.RestoreState(Clone(saved), out message), "Reload the good save: " + message);
            session = desk.Session;

            // ================= The lockout's second day =================
            Check(desk.StudioSleepsOn, "A lockout with a day to come lets the calendar move one day");
            float workload = session.WorkloadMinutes;
            var tomorrow = startDate.NextDay();
            Morning(tomorrow);
            Private("ProcessStudioLockoutDawn", tomorrow);
            Check(desk.StudioDayIndex(session) == 1 && desk.StudioMinutesLeft(session) == session.Hours * 60 && session.WorkloadMinutes < workload * .5f && !desk.StudioSleepsOn,
                $"Day two: the room is the label's from nine, the band slept off most of the wear ({workload:F0}->{session.WorkloadMinutes:F0})");
            Check(desk.CutStudioTake(0, out message) && session.Work.DayElapsed[1] > 0, "Work on day two counts against day two: " + message);
            Morning(tomorrow.NextDay());
            Check(desk.StudioMinutesLeft(session) == 0 && !desk.StudioSleepsOn, "A lockout ends with its days");
            Morning(tomorrow);
            // Print: the kept A-side's master keeps its own shape and the date's setup.
            desk.KeepTake(0, Enumerable.Range(0, session.Cuts[0].Takes.Count).Last(i => !session.Cuts[0].Takes[i].Incomplete && session.Cuts[0].Takes[i].Work?.KeyShift == -1));
            int keptLength = session.Cuts[0].Takes[session.Cuts[0].KeptTake].Work.DurationSeconds;
            int mastersBefore = desk.Masters.Count;
            Check(desk.PrintSession(out message), "Print the lockout: " + message);
            var aMaster = desk.Masters[mastersBefore];
            Check(aMaster.StudioHistory.DurationSeconds == keptLength && aMaster.StudioHistory.Setup.Contains("a step lower") && aMaster.StudioHistory.Setup.Contains("short intro and clean ending"),
                "The master keeps its own key, length and setup: " + string.Join("; ", aMaster.StudioHistory.Setup));
            Check(PolarSongMetadataService.Get(aMaster.Record.masterId)?.taxonomy?.durationSeconds == keptLength, "The master's taxonomy carries the performed length");

            // ================= An evening date and a hired producer =================
            var evening = desk.StudioRoomIds().Select(id => desk.StudioOffer(id, band.artistId)).FirstOrDefault(o => o != null && o.Local && o.Room.EveningWork && o.Locked == null);
            if (evening != null) {
                var day3 = tomorrow.NextDay().NextDay();
                clock.RestoreClock(day3, PlayerDesk.StudioEveningStartHour, 0);
                float before = desk.Label.cashReserves;
                Check(desk.StartSession(Live(band), one, evening.Tier, 3, out message, overrideRefusal: true, roomId: evening.Room.Id, options: new StudioBookingOptions { Evening = true }),
                    "Book an evening date: " + message);
                Check(Math.Abs(before - desk.Label.cashReserves - Mathf.Round(PlayerDesk.StudioRoomCost(desk.Session.Room, 3) * PlayerDesk.StudioEveningRateFactor)) < 1.01f &&
                    desk.Session.WorkloadMinutes >= PlayerDesk.StudioEveningWorkload, "Evening rate charged; the band comes in tired");
                desk.ScrapSession();
            }
            var day5 = tomorrow.AddDays(4);
            Morning(day5);
            var producer = desk.StudioProducerOffer(three.Room.CityId, Live(band), day5, 9 * 60, 12 * 60);
            if (producer != null) {
                float before = desk.Label.cashReserves, craft = desk.Label.productionQuality;
                Check(desk.StartSession(Live(band), one, three.Tier, 3, out message, overrideRefusal: true, roomId: three.Room.Id, options: new StudioBookingOptions { ProducerId = producer.PersonId }),
                    "Book with a hired producer: " + message);
                Check(desk.Session.Hires.Any(h => h.Kind == StudioHireKind.Producer && h.PersonId == producer.PersonId) && desk.Session.ProducerCraft >= Math.Max(craft, producer.Craft) - .0001f,
                    "The producer is on the date and directs at their craft");
                // Delegation with no money: buys nothing, hires nobody, and can stop at a promising take.
                desk.Session.Work.PassesUsed = desk.Session.Work.StockPasses;
                int hires = desk.Session.Hires.Count;
                Check(!desk.RunStudioDate(new StudioDelegation { SpendLimit = 0 }, out message) && desk.Session.Hires.Count == hires && desk.Session.Work.StockBought == 0,
                    "With a zero limit and no tape the producer stops rather than buy: " + message);
                desk.Session.Work.PassesUsed = 0;
                bool stopped = !desk.RunStudioDate(new StudioDelegation { StopAtPromising = true, StopAtIdea = true, StopAtThreat = true }, out message);
                GD.Print("STUDIO_CRAFT delegation with stops: " + message);
                Check(message.Contains(producer.Name) || !stopped, "The hired producer's name is on the hand-back");
                desk.ScrapSession();
            }

            // ================= Rooms: standing crews and other labels' hits =================
            var specialistId = StudioRoomCatalog.SpecialistId("nashville");
            var special = StudioRoomCatalog.ById(specialistId, out _);
            var crew = (StudioHouseCrew)Private("HouseCrewFor", special, LocalSceneRoomService.Day(day5), day5.year);
            Check(crew != null && crew.Seats.Count >= 2 && crew.Name.Length > 0, "The specialist room has a named standing crew: " + crew?.Name);
            var crewAgain = (StudioHouseCrew)Private("HouseCrewFor", special, LocalSceneRoomService.Day(day5) + 30, day5.year);
            Check(crewAgain.Seats.Select(s => s.PersonId).SequenceEqual(crew.Seats.Select(s => s.PersonId)), "The same regulars a month later");
            Private("UpdateStudioMarketHits", day5);
            int hitsNow = desk.StudioMarketHits.Count;
            Private("UpdateStudioMarketHits", day5);
            Check(desk.StudioMarketHits.Count == hitsNow, "Other labels' hits are noted once each");
            GD.Print($"STUDIO_CRAFT other labels' charting records attributed to rooms: {hitsNow} ({string.Join(", ", desk.StudioMarketHits.GroupBy(h => h.RoomId).Select(g => $"{g.Key}={g.Count()}").Take(6))})");
            if (hitsNow > 0) {
                var busiest = desk.StudioMarketHits.GroupBy(h => h.RoomId).OrderByDescending(g => g.Count()).First().Key;
                Check(desk.StudioRenown(busiest, day5) < desk.StudioRenown(busiest, day5.AddDays(140)), "Other labels' hits build a room's name after the lag");
            }
            var roomsState = Clone(desk.CaptureState());
            Check(desk.RestoreState(roomsState, out message) && desk.StudioHouseCrews.Count == roomsState.StudioHouseCrews.Count && desk.StudioMarketHits.Count == hitsNow,
                "Crews and attributed hits survive a save: " + message);

            // ================= Period lengths and radio =================
            var shape1960 = StudioSongStructures.For(one[0], Live(band), 1960);
            var shape1968 = StudioSongStructures.For(one[0], Live(band), 1968);
            Check(shape1968.DurationSeconds > shape1960.DurationSeconds && StudioSongStructures.PeriodLength(1963) == 1, "The same song runs longer when cut in 1968");
            Check(StationNetwork.LengthFit(0, 1960, StationFormat.Top40) == 1f && StationNetwork.LengthFit(150, 1961, StationFormat.Top40) > 1f &&
                StationNetwork.LengthFit(240, 1961, StationFormat.Top40) < 1f && StationNetwork.LengthFit(240, 1968, StationFormat.Top40) > StationNetwork.LengthFit(240, 1961, StationFormat.Top40),
                "Top 40 prefers short sides, an unknown length is neutral, and the tolerance widens later in the decade");

            // ================= Own harmony on a pass =================
            Sign(harmonyAct);
            Morning(day5.NextDay());
            var hOffer = desk.StudioRoomIds().Select(id => desk.StudioOffer(id, harmonyAct.artistId)).First(o => o != null && o.Local && o.Locked == null);
            Check(desk.StartSession(Live(harmonyAct), one, hOffer.Tier, 3, out message, overrideRefusal: true, roomId: hOffer.Room.Id), "Book the harmony act: " + message);
            Check(PlayerDesk.StudioOwnHarmony(desk.Session, Live(harmonyAct)).Count > 0 && PlayerDesk.StudioHarmonyVoiceOn(desk.Session, Live(harmonyAct)) >= 0, "The act's own harmony singers are on the date");
            var own = desk.CaptureState();
            Check(desk.CutStudioTake(0, out message), "Pass with their own harmony: " + message);
            var withVoices = desk.Session.Cuts[0].Takes[^1];
            Check(desk.RestoreState(Clone(own), out message), "Back: " + message);
            desk.Session.RulesVersion = 1;   // the same pass under the old rules: no own harmony (and no rules-v2 effects at all)
            desk.Session.Work = null; desk.Session.Cuts[0].Work = null;
            Check(desk.CutStudioTake(0, out message), "The same pass under the old rules: " + message);
            var without = desk.Session.Cuts[0].Takes[^1];
            float call = PlayerDesk.StudioBackingCall(desk.Session.Cuts[0].Structure, Live(harmonyAct).primaryGenre);
            Check(withVoices.Incomplete || without.Incomplete || withVoices.Work?.Incident != StudioIncident.None || Math.Sign(withVoices.Vocal - without.Vocal) == Math.Sign(call - .4f) || Math.Abs(withVoices.Vocal - without.Vocal) < .0005f,
                $"Under identical draws the act's own voices lift a song that calls for them and clutter one that doesn't (call {call:F2}: {without.Vocal:F3}->{withVoices.Vocal:F3})");
            desk.ScrapSession();

            // ================= The window =================
            Morning(day5.AddDays(2));
            Check(desk.StartSession(Live(band), two, three.Tier, 4, out message, overrideRefusal: true, roomId: three.Room.Id, options: new StudioBookingOptions { Layout = StudioLayout.ThreeTrack }), "Book for the window: " + message);
            string flipTitle = desk.Session.Cuts[1].Choice.Title;
            Check(desk.CutStudioSideFirst(1, out message) && desk.Session.Cuts[0].Choice.Title == flipTitle, "Before tape rolls the flip can go first: " + message);
            Check(desk.CutStudioTake(0, out message), "A pass for the window: " + message);
            Check(!desk.CutStudioSideFirst(1, out _), "Once tape has rolled the order is fixed");
            var main = GD.Load<PackedScene>("res://MainMenu.tscn").Instantiate<UIManager>(); AddChild(main);
            for (int i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            main.OpenStudio(band.artistId);
            for (int i = 0; i < 3; i++) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            var panel = main.GetNode<PlayerDeskPanel>("StudioWindow");
            var buttons = Walk(panel).OfType<Button>().Select(b => b.Text).ToList();
            Check(buttons.Any(t => t.StartsWith("KEY, TEMPO")) && buttons.Any(t => t.StartsWith("ROOM BALANCE")) && buttons.Any(t => t == "TAPE…"),
                "The console offers shape, balance and the take's tape operations: " + string.Join(" | ", buttons.Take(40)));
            Check(Walk(panel).OfType<Node>().Any(n => n is Control c && c.Visible && (n as Label)?.Text?.Contains("passes") == true),
                "The session sheet shows the tape left");
            await Screenshot("console");
            desk.ScrapSession();
            main.QueueFree();

            GD.Print($"STUDIO_CRAFT_CHECK_PASS checks={checks}"); GetTree().Quit();
        } catch (Exception ex) { GD.PrintErr("STUDIO_CRAFT_CHECK_FAIL " + (ex is TargetInvocationException t ? t.InnerException : ex)); GetTree().Quit(1); }
    }
}
