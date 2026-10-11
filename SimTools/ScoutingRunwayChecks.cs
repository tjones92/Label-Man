using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;

/// <summary>Phase 6 opening runway (SimTools/ScoutingExperienceExpansionDirective.md §11, scenario 12). The opening
/// check proved every founding city has an affordable set to hear. This asks the next question: can a new label in
/// each town reach an act that would discuss a deal at a price it can pay, and how much grind does it take?
///
/// Part A projects the posted calendar from every founding city with every starting-cash profile: audit only, no
/// time passes. Part B plays real openings in contrasting towns from one restored world and counts the actions,
/// minutes, days and money to a first offer. Neither part spawns talent, grants a hold or reads a ceiling.</summary>
public static class ScoutingRunwayChecks {
    private static readonly string[] Contrasts = { "billings", "albuquerque", "nashville", "new_york" };
    private const int HorizonDays = 84;
    private static bool DirectAsks => !OS.GetCmdlineUserArgs().Contains("--runway-no-direct-asks");

    private static bool OffMarket(SimulatedArtist a) => a != null && string.IsNullOrEmpty(a.labelId) &&
        a.lifecycleStatus == ArtistLifecycleStatus.Active && a.prospectMarketStatus == ProspectMarketStatus.Latent;

    public static void Run() {
        int checks = 0;
        void Check(bool ok, string why) { if (!ok) throw new InvalidOperationException("SCOUTING_RUNWAY_FAILED: " + why); checks++; }
        string Json(object value) => JsonSerializer.Serialize(value, SaveGameService.TestJsonOptions);
        var desk = PlayerDesk.Instance; var clock = TimeManager.Instance; var am = ArtistManager.Instance;
        Check(desk.FoundLabel("Runway Checks", "nashville", out string message), "found label: " + message);
        string seed = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--seed="))?.Substring(7);
        var poorest = FoundingArchetypeData.All.OrderBy(p => p.Value.Capital).First();

        // ---- Part A: every town, every purse ----------------------------------------------------------------
        var opening = desk.CaptureState();
        string world0 = Json(WorldStateService.Capture());
        var rows = new List<object>(); var thin = new List<string>();
        foreach (var city in DistanceModel.GetCities().OrderBy(c => c.cityId, StringComparer.Ordinal)) {
            opening.CurrentCityId = city.cityId;
            Check(desk.RestoreState(opening, out message), "viewpoint: " + city.cityId + " " + message);
            var rooms = SceneRoomCatalog.ForPlace(city.cityId);
            Check(rooms.Select(r => r.ContactName).Distinct().Count() == rooms.Count, "each room in " + city.cityId + " has its own booker");
            foreach (var profile in FoundingArchetypeData.All) {
                float cash = profile.Value.Capital; desk.Label.cashReserves = cash;
                var routes = desk.ScoutingItinerary(true).Where(o => o.Affordable).ToArray();
                var deals = routes.Select(o => (o, q: desk.SceneDealQuote(o.ArtistId, o.RoomId)))
                    .Where(x => (x.q.Available || DirectAsks && OffMarket(am.GetArtist(x.o.ArtistId))) && x.o.Admission + x.o.TravelCost + x.q.Ask <= cash).ToArray();
                var local = deals.Where(x => x.o.TravelMinutes == 0).ToArray();
                int heardActs = routes.Select(o => o.ArtistId).Distinct().Count();
                int dealActs = deals.Select(x => x.o.ArtistId).Distinct().Count();
                var bookers = rooms.Where(r => r.OpenHour < clock.HardStopHour && r.CloseHour > clock.WorkdayStartHour).Select(r => r.ContactName).Distinct().Count();
                var first = deals.OrderBy(x => x.o.TravelMinutes > 0).ThenBy(x => x.o.DaysUntil).FirstOrDefault();
                var acts = routes.Select(o => o.ArtistId).Distinct().Select(id => am.GetArtist(id)).Where(a => a != null).ToArray();
                int week = ChartManager.Instance.GetCurrentChartWeek();
                rows.Add(new { city = city.cityId, archetype = profile.Key.ToString(), cash, affordableRoutes = routes.Length, heardActs,
                    signed = acts.Count(a => !string.IsNullOrEmpty(a.labelId)), latent = acts.Count(a => string.IsNullOrEmpty(a.labelId) && a.prospectMarketStatus == ProspectMarketStatus.Latent),
                    eligible = acts.Count(a => am.IsEligibleForPopulationSigning(a, week)),
                    quotedAvailable = routes.Select(o => o.ArtistId).Distinct().Count(id => desk.SceneDealQuote(id, routes.First(o => o.ArtistId == id).RoomId).Available),
                    medianAsk = acts.Length == 0 ? 0 : routes.Select(o => desk.SceneDealQuote(o.ArtistId, o.RoomId).Ask).OrderBy(v => v).ElementAt(routes.Length / 2),
                    dealActs, localDealActs = local.Select(x => x.o.ArtistId).Distinct().Count(),
                    dealShare = heardActs == 0 ? 0 : (float)dealActs / heardActs, bookers,
                    firstDealDay = first.o?.DaysUntil, firstDealRoom = first.o?.RoomName, firstDealCity = first.o?.CityId,
                    firstDealAsk = first.o == null ? (float?)null : first.q.Ask, firstDealAdmission = first.o?.Admission, firstDealGas = first.o?.TravelCost });
                Check(bookers > 0, "a booker to ask in " + city.cityId);
                if (dealActs == 0) thin.Add($"{city.cityId}/{profile.Key}");
            }
        }
        Check(Json(WorldStateService.Capture()) == world0, "the audit changes no world state");
        string dir = ProjectSettings.GlobalizePath("res://SimLogs"); Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, $"scouting-runway-audit-{seed}.json"), Json(rows));
        // A town with no reachable act taking offers on this calendar is a finding to report, not a failure to hide:
        // the directive forbids spawning talent to fill it. The check is that the poorest purse still has a route.
        {
            int week = ChartManager.Instance.GetCurrentChartWeek(), day = LocalSceneRoomService.Day(clock.CurrentDate);
            var seeking = am.GetAllArtists().Where(a => string.IsNullOrEmpty(a.labelId) && am.IsEligibleForPopulationSigning(a, week)).ToArray();
            var billed = SceneRoomCatalog.All.Select(r => r.PlaceId).Distinct().SelectMany(p => LocalSceneRoomService.AnnouncedCalendar(p, clock.CurrentDate))
                .SelectMany(b => b.Appearances).Select(a => a.ArtistId).ToHashSet();
            GD.Print($"RUNWAY_POPULATION seeking={seeking.Length} withBase={seeking.Count(a => a.geography?.basePlaceId != null)} " +
                $"presentAnywhere={seeking.Count(a => SceneRoomCatalog.All.Any(r => LocalSceneRoomService.Eligible(a, r, day)))} onBills={seeking.Count(a => billed.Contains(a.artistId))} " +
                $"cohorts={string.Join(",", seeking.GroupBy(a => a.cohort).Select(g => g.Key + ":" + g.Count()))}");
        }
        if (!OS.GetCmdlineUserArgs().Contains("--runway-report-only"))
            Check(thin.Count == 0, "no reachable act to propose to: " + string.Join(", ", thin));

        // ---- Part B: real openings in contrasting towns -------------------------------------------------------
        opening.CurrentCityId = "nashville";
        Check(desk.RestoreState(opening, out message), "restore home viewpoint");
        var preWorld = LocalSceneRoomService.Copy(WorldStateService.Capture()); var prePlayer = LocalSceneRoomService.Copy(desk.CaptureState());
        var date = clock.CurrentDate; int startHour = clock.CurrentHour, startMinute = clock.CurrentMinute;
        var plays = new List<object>();
        foreach (string town in Contrasts) {
            WorldStateService.Apply(LocalSceneRoomService.Copy(preWorld), date, SimulationSeedBootstrap.RequestedSeed);
            var player = LocalSceneRoomService.Copy(prePlayer); player.CurrentCityId = town;
            Check(desk.RestoreState(player, out message), "restore " + town + ": " + message);
            clock.RestoreClock(date, startHour, startMinute);
            float cash = poorest.Value.Capital; desk.Label.cashReserves = cash;
            int actions = 0, listens = 0, minutes = 0, refusals = 0, asks = 0; string outcome = "no offer reached", reason = null, act = null;
            float admissionPaid = 0, ask = 0; int startDay = LocalSceneRoomService.Day(clock.CurrentDate);
            int? firstOpenDay = null, offerDay = null; var opened = new HashSet<string>(StringComparer.Ordinal);
            var tried = new Dictionary<string, int>(StringComparer.Ordinal);
            int Elapsed() => LocalSceneRoomService.Day(clock.CurrentDate) - startDay;
            int Minute() => Elapsed() * 24 * 60 + clock.CurrentHour * 60 + clock.CurrentMinute;
            // Any act in the notebook that would talk today gets an offer at its own asking terms.
            void TryOffers() {
                int week = ChartManager.Instance.GetCurrentChartWeek();
                foreach (var dossier in desk.ScoutingDossiers) {
                    var a = am.GetArtist(dossier.ArtistId);
                    // Off the market: ask whether they'd hear an offer (once a season; the desk keeps the answer).
                    if (DirectAsks && desk.CanAskDirectApproach(a) && !desk.DirectApproaches.Any(x => x.ArtistId == a.artistId &&
                            (x.Status == ScoutingApproachStatus.Considering || x.Status == ScoutingApproachStatus.Declined && x.UntilDay > LocalSceneRoomService.Day(clock.CurrentDate)))
                        && desk.OpenScoutingDossier(a.artistId, out message)) {
                        var asked = desk.Slate.FirstOrDefault(p => p.Artist.artistId == a.artistId);
                        if (asked != null) { actions++; asks++; desk.ApproachToSign(asked, out message); }
                        continue;
                    }
                    if (a == null || !string.IsNullOrEmpty(a.labelId) || !am.IsEligibleForPopulationSigning(a, week)) continue;
                    if (opened.Add(a.artistId)) firstOpenDay ??= Elapsed();
                    if (tried.TryGetValue(a.artistId, out int last) && Elapsed() - last < 7) continue;
                    tried[a.artistId] = Elapsed();
                    if (!desk.OpenScoutingDossier(a.artistId, out message)) continue;
                    var heard = desk.Slate.FirstOrDefault(p => p.Artist.artistId == a.artistId);
                    if (heard == null || heard.AskingAdvance > desk.Label.cashReserves) continue;
                    actions++;
                    if (!desk.ApproachToSign(heard, out message)) { refusals++; reason = message; continue; }
                    act = heard.Artist.stageName; ask = heard.Baseline.Advance; offerDay ??= Elapsed();
                    if (heard.Talk != null) { outcome = "talks opened (" + heard.Posture + ")"; reason = message; return; }
                    actions++; var t = heard.Baseline;
                    if (desk.OfferContract(heard, t.Advance, t.RoyaltyRate, t.TermYears, t.SinglesObligation, t.LabelOwnsPublishing, t.ArtistCreativeControl, out message)) {
                        outcome = "signed"; reason = message; return;
                    }
                    refusals++; reason = message;
                }
            }
            while (Elapsed() < HorizonDays && outcome == "no offer reached" && !clock.IsGameOver) {
                TryOffers();
                if (outcome != "no offer reached") break;
                var heardIds = desk.ScoutingDossiers.Select(d => d.ArtistId).ToHashSet();
                var today = desk.ScoutingItinerary().Where(o => o.Affordable && o.DaysUntil == 0).ToArray();
                var plan = today.FirstOrDefault(o => !heardIds.Contains(o.ArtistId)) ?? today.FirstOrDefault();
                if (plan != null) {
                    int before = Minute(); float purse = desk.Label.cashReserves; bool ready = true;
                    if (!plan.Playing) { actions++; ready = desk.WaitForSceneAppearance(plan.BillId, plan.EventId, out message); }
                    if (ready) { actions++; ready = desk.ListenSceneAppearance(plan.BillId, plan.EventId, out message); }
                    if (ready) {
                        listens++; admissionPaid += purse - desk.Label.cashReserves; minutes += Minute() - before;
                        // Keep every unsigned act heard on the attention list: that is what the player's pursuit means.
                        foreach (var heard in desk.Slate.Where(p => string.IsNullOrEmpty(p.Artist.labelId)))
                            desk.SetScoutingPriority(heard.Artist.artistId, ScoutingPriority.Watch);
                        TryOffers();
                    }
                }
                if (outcome == "no offer reached") clock.EndDay();
            }
            int days = Elapsed();
            var dossiers = desk.ScoutingDossiers;
            plays.Add(new { town, directAsks = DirectAsks, cash, outcome, act, ask, reason, firstOpenDay, offerDay, openedActs = opened.Count, listens, actions,
                listeningMinutes = minutes, days, admissionPaid, refusals, dossiers = dossiers.Count,
                heardUnsignedAtEnd = dossiers.Count(d => string.IsNullOrEmpty(am.GetArtist(d.ArtistId)?.labelId)),
                rivalNotes = dossiers.Sum(d => d.Observations.Count(o => o.Kind == ScoutingEvidenceKind.RivalInterest)),
                approachAsks = asks, considering = desk.DirectApproaches.Count(x => x.Status != ScoutingApproachStatus.Declined), declined = desk.DirectApproaches.Count(x => x.Status == ScoutingApproachStatus.Declined),
                ready = desk.DirectApproaches.Count(x => x.Status == ScoutingApproachStatus.Ready),
                lostToRivals = dossiers.Count(d => opened.Contains(d.ArtistId) && !string.IsNullOrEmpty(am.GetArtist(d.ArtistId)?.labelId) && am.GetArtist(d.ArtistId).labelId != desk.Label.labelId),
                roster = desk.Label.roster.Count, cashLeft = desk.Label.cashReserves });
            // Scenario 12: a poor opening still leaves an understandable next path.
            Check(outcome != "no offer reached" || desk.ScoutingDossiers.Count > 0 && desk.ScoutingItinerary(true).Count > 0,
                town + ": no offer and no next path");
            if (outcome == "signed") Check(desk.Label.roster.Count == 1 && desk.FoundingAct?.ArtistName == act, town + ": first signature recorded");
        }
        File.WriteAllText(Path.Combine(dir, $"scouting-runway-play-{seed}.json"), Json(plays));

        // ---- Part C: the ask itself -----------------------------------------------------------------------------
        WorldStateService.Apply(LocalSceneRoomService.Copy(preWorld), date, SimulationSeedBootstrap.RequestedSeed);
        Check(desk.RestoreState(LocalSceneRoomService.Copy(prePlayer), out message), "restore for ask checks");
        clock.RestoreClock(date, startHour, startMinute); desk.Label.cashReserves = 800;
        var cold = desk.ScoutingItinerary().First(o => OffMarket(am.GetArtist(o.ArtistId)));
        var coldProspect = new PlayerDesk.Prospect { Artist = am.GetArtist(cold.ArtistId), CityId = desk.CurrentCityId, SceneRoomId = cold.RoomId };
        Check(!desk.CanAskDirectApproach(coldProspect.Artist) && !desk.ApproachToSign(coldProspect, out message) && desk.DirectApproaches.Count == 0,
            "an unheard act cannot be asked: " + message);
        int waits = 0;
        PlayerDesk.Prospect Heard() {
            var plans = desk.ScoutingItinerary().Where(o => o.Affordable && OffMarket(am.GetArtist(o.ArtistId))).ToArray();
            foreach (var plan in plans) {
                if (!plan.Playing && !desk.WaitForSceneAppearance(plan.BillId, plan.EventId, out message)) continue;
                waits++;
                if (desk.ListenSceneAppearance(plan.BillId, plan.EventId, out message))
                    return desk.Slate.FirstOrDefault(p => p.Artist.artistId == plan.ArtistId && OffMarket(p.Artist));
            }
            return null;
        }
        var target = Heard(); Check(target != null && desk.CanAskDirectApproach(target.Artist), "a heard off-market act can be asked");
        float purseBefore = desk.Label.cashReserves; int minute = clock.CurrentHour * 60 + clock.CurrentMinute;
        string snapshot = Json(WorldStateService.Capture());
        Check(!desk.ApproachToSign(target, out message) && desk.DirectApproaches.Count == 1, "the ask records an answer and opens no terms: " + message);
        var answer = desk.DirectApproaches[0];
        Check(desk.Label.cashReserves == purseBefore && clock.CurrentHour * 60 + clock.CurrentMinute == minute && Json(WorldStateService.Capture()) == snapshot,
            "asking costs nothing and changes no world state");
        Check(answer.Status is ScoutingApproachStatus.Considering or ScoutingApproachStatus.Declined, "the act answers considering or declined");
        desk.ApproachToSign(target, out string again);
        Check(desk.DirectApproaches.Count == 1 && desk.DirectApproaches[0].Status == answer.Status && again.Contains(LocalSceneRoomService.Date(answer.UntilDay).ToHeadlineString()),
            "a repeat ask does not reroll and states the date");
        var saved = LocalSceneRoomService.Copy(desk.CaptureState());
        Check(desk.RestoreState(LocalSceneRoomService.Copy(saved), out message) && Json(desk.DirectApproaches) == Json(new[] { answer }), "the answer survives save/load");
        var malformed = LocalSceneRoomService.Copy(saved); malformed.ScoutingKnowledge.Approaches.Add(new ScoutingDirectApproach { ArtistId = null });
        Check(desk.RestoreState(malformed, out message) && desk.DirectApproaches.Count == 1, "a malformed approach is dropped on load");
        Check(desk.ScoutingSummary(target.Artist.artistId).Any(l => l.StartsWith("Approach (")), "the notebook states the approach");
        // Roster cap: a full roster hands the owner nobody.
        if (answer.Status == ScoutingApproachStatus.Considering) {
            int max = desk.Label.maxRosterSize; desk.Label.maxRosterSize = desk.Label.roster.Count;
            var provider = am.DirectApproachProvider();
            Check(provider == null || provider.Count == 0, "no open roster slot, no activation demand");
            desk.Label.maxRosterSize = max;
            Check(am.DirectApproachProvider().Contains(target.Artist.artistId), "an open slot hands the act to the owner");
            // Another label signs them before the answer: the approach lapses.
            var rival = ChartManager.Instance.GetAllLabels().First(l => !l.isPlayerOwned && l.IsActive);
            target.Artist.labelId = rival.labelId;
            clock.EndDay();
            Check(desk.DirectApproaches[0].Status == ScoutingApproachStatus.Lapsed, "signed elsewhere lapses the approach");
        }
        GD.Print($"SCOUTING_RUNWAY_CHECK_PASS checks={checks} cities={DistanceModel.GetCities().Count} thin={thin.Count} " +
            string.Join(" ", plays.Select(p => Json(p))));
    }
}
