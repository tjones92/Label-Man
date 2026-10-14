using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public enum RivalCourtshipStage { Inquiry, Meeting, Offer, Faded, Ended }
/// <summary>Why the rival came: the town's own competition (the crowding roll), its daily market choosing an act the player
/// is pursuing, or a player's act whose contract has run out.</summary>
public enum RivalCourtshipOrigin { Crowding, DailyMarket, Renewal }
/// <summary>Who told the player: the act's own side, the booker of the room the rival heard them in, a musician on the
/// player's roster who knows somebody in the act, or talk around a scene the player has been working.</summary>
public enum RivalSightingChannel { ActSide, RoomBooker, RosterContact, SceneTalk }

/// <summary>One thing the player learned about a courtship: when, at which stage, from whom, and what was said.</summary>
public sealed class RivalSighting {
    public int Day { get; set; }
    public RivalCourtshipStage Stage { get; set; }
    public RivalSightingChannel Channel { get; set; }
    public string SourceId { get; set; }
    public string SourceName { get; set; }
    public string Text { get; set; }
    /// <summary>Handed to the player (a note and a notebook entry). Word that travels overnight waits for its day.</summary>
    public bool Delivered { get; set; }
}

/// <summary>
/// A rival label's pursuit of one act, from first inquiry through a meeting to an offer (RivalOffer), or until it fades.
/// The pursuit is world truth the player does not see; <see cref="Known"/> is what the player learned of it, each item
/// from a source that could know. IDs only.
/// </summary>
public sealed class RivalCourtship {
    public string Id { get; set; }
    public string PlayerLabelId { get; set; }
    public string ArtistId { get; set; }
    public string ArtistName { get; set; }
    public string RivalLabelId { get; set; }
    public string RivalLabelName { get; set; }
    public RivalCourtshipOrigin Origin { get; set; }
    public RivalCourtshipStage Stage { get; set; }
    public int InquiryDay { get; set; }
    public int? MeetingDay { get; set; }
    /// <summary>The dawn the courtship moves on (inquiry to meeting, meeting to offer). Meaningless once offered or over.</summary>
    public int NextDay { get; set; }
    /// <summary>The room the rival heard the act in, if it heard them in one; null when it asked around.</summary>
    public string RoomId { get; set; }
    public string OfferId { get; set; }
    public int? EndedDay { get; set; }
    public string EndReason { get; set; }
    public List<RivalSighting> Known { get; set; } = new();
}

// ============================================================================================
// RIVAL COURTSHIP -- Phases 3-5 of the scouting expansion directive (§9 "rivals: real pressure,
// imperfectly known"; §14 Phase 5 "bounded visibility effects").
//
// A rival's interest used to arrive as an offer out of nowhere. It now has the shape the trade
// had: somebody from the label comes to hear the act (inquiry), sits down with them (meeting),
// and puts paper in front of them (offer), or loses interest. Each step is world truth decided by
// stable hashes and the recruitment owner's own access/roster/cash checks; nothing draws the
// global RNG and nothing runs without a player label.
//
// The player learns of a step only through somebody who could know it:
//   * the act's own side, once the player has sat down or talked terms with them;
//   * the booker of the room the rival heard them in -- told the next time the player is there;
//   * a musician on the player's roster who knows somebody in the act (the contact network);
//   * talk around a scene the player has been working in the last fortnight (meetings and offers,
//     not a man quietly listening at the back).
// Otherwise the step happens unseen, and the player may learn of it only from the outcome.
//
// Phase 5's visibility channel lives here too. What the player does around an act is seen by other
// people: a night at the back of a room by a few, a booker asked questions by the booker's other
// callers, a studio session by everybody who works there, terms on the table by every label the act
// shows them to. The signals are read off the player's own acquired evidence (one per event, with a
// delay and an expiry by kind), so nothing new is stored and a refresh cannot farm them. They raise
// the chance a rival with access comes to look, and weight the AI's own slate toward the act -- the
// larger of that and the scene's existing attention, never both.
// ============================================================================================
public partial class PlayerDesk {
    /// <summary>Share of inquiries a rival drops after hearing the act: interest can end.</summary>
    public const float RivalInterestFadeShare = 0.25f;
    /// <summary>How much a fully visible pursuit multiplies the daily chance that a rival comes to look (1 + gain).</summary>
    public const float VisibilityInquiryGain = 2f;
    /// <summary>The most a fully visible pursuit weights the act in an AI label's slate (1 + boost); the same ceiling as
    /// the scene's attention feedback, and combined with it by max, so the two never compound.</summary>
    public const float VisibilityDiscoveryBoost = 0.25f;
    /// <summary>Daily chance a rival approaches a player's act whose contract has run out, at full heat.</summary>
    public const float RenewalPoachDailyBase = 0.015f;
    /// <summary>How long scene talk reaches a player who has been around an act's rooms, and how long a booker remembers.</summary>
    public const int SceneTalkDays = 14, BookerMemoryDays = 21;

    private readonly List<RivalCourtship> rivalCourtships = new();
    public IReadOnlyList<RivalCourtship> RivalCourtships => LocalSceneRoomService.Copy(rivalCourtships).AsReadOnly();

    private static bool CourtshipLive(RivalCourtship c) => c.Stage is RivalCourtshipStage.Inquiry or RivalCourtshipStage.Meeting;
    private RivalCourtship ActiveCourtship(string artistId) => artistId == null ? null :
        rivalCourtships.FirstOrDefault(c => c.ArtistId == artistId && CourtshipLive(c));

    // ========================================================================================
    // VISIBILITY (Phase 5): what the player's own pursuit lets other people see
    // ========================================================================================

    /// <summary>How visible one piece of the player's acquired evidence is to the trade: reach (0-1), the days before word
    /// travels, and the days it stays live. Null for evidence nobody else saw happen (a recap read at the desk, an old
    /// impression, a rival's interest reported to the player).</summary>
    internal static (float Reach, int Delay, int Expiry)? VisibilityOf(ScoutingEvidenceKind kind) => kind switch {
        ScoutingEvidenceKind.Performance => (0.04f, 2, 14),        // a face at the back of the room
        ScoutingEvidenceKind.ListenerOpinion => (0.03f, 2, 14),    // somebody else asking in the room
        ScoutingEvidenceKind.BookerClaim => (0.08f, 1, 21),        // bookers talk to every label that calls
        ScoutingEvidenceKind.ActBusinessClaim => (0.06f, 1, 21),
        ScoutingEvidenceKind.MeetingStatement => (0.12f, 1, 21),   // the act tells people who came to see them
        ScoutingEvidenceKind.RehearsalSample => (0.10f, 1, 21),
        ScoutingEvidenceKind.DemoSample => (0.30f, 2, 30),         // a studio booking: engineers, players, the whole trade
        ScoutingEvidenceKind.BusinessStatement => (0.20f, 2, 30),  // paper on the table gets shown around
        _ => null,
    };

    /// <summary>How visible the player's pursuit of this act is today (0-1): one signal per event (a night, a talk, a
    /// session), each live between its delay and expiry, combined as independent chances of being noticed. Pure.</summary>
    public float PursuitVisibility(string artistId, int day) {
        if (Label == null || artistId == null) return 0f;
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == artistId);
        if (dossier == null) return 0f;
        float unseen = 1f;
        foreach (var signal in dossier.Observations
                     .Select(o => (o, v: VisibilityOf(o.Kind)))
                     .Where(x => x.v.HasValue)
                     // One event, one signal: a talk is one discussion however many rounds it ran; a night is one bill.
                     .GroupBy(x => x.o.Kind == ScoutingEvidenceKind.BusinessStatement && !string.IsNullOrEmpty(x.o.BusinessDiscussionId)
                         ? "talk|" + x.o.BusinessDiscussionId
                         : x.o.Kind + "|" + (x.o.EventId ?? x.o.BillId ?? x.o.MeetingId ?? x.o.Id), StringComparer.Ordinal)
                     .Select(g => g.OrderBy(x => x.o.AcquiredDay).First())) {
            var (reach, delay, expiry) = signal.v.Value;
            int from = signal.o.AcquiredDay + delay;
            if (day >= from && day < from + expiry) unseen *= 1f - reach;
        }
        return Mathf.Clamp(1f - unseen, 0f, 1f);
    }

    /// <summary>The AI slate weight for an act the player's pursuit has made visible: 1 when nothing is live. Asked for every
    /// candidate in every slate, so the weights are built once per clock minute (the AI market clears in one pass).</summary>
    private double AiVisibilityWeight(SimulatedArtist artist) {
        if (Label == null || artist == null || !LocalScenes.RivalOffers || !string.IsNullOrEmpty(artist.labelId)) return 1d;
        int day = TodayIndex();
        long stamp = day * 1440L + (TimeManager.Instance?.CurrentHour ?? 0) * 60 + (TimeManager.Instance?.CurrentMinute ?? 0);
        if (stamp != visibilityStamp) {
            visibilityStamp = stamp;
            visibilityWeights.Clear();
            foreach (var d in scoutingKnowledge.Dossiers) {
                float seen = PursuitVisibility(d.ArtistId, day);
                if (seen > 0f) visibilityWeights[d.ArtistId] = 1d + VisibilityDiscoveryBoost * seen;
            }
        }
        return visibilityWeights.TryGetValue(artist.artistId, out double w) ? w : 1d;
    }
    private long visibilityStamp = long.MinValue;
    private readonly Dictionary<string, double> visibilityWeights = new(StringComparer.Ordinal);

    // ========================================================================================
    // WHO THE PLAYER IS PURSUING
    // ========================================================================================

    /// <summary>Acts the player is pursuing: in the notebook, on the pad, in a kept terms discussion, or marked watch or
    /// pursue in the dossiers. An act merely heard once and filed is not.</summary>
    internal bool IsPursuing(string artistId) =>
        artistId != null && (notebook.Any(n => n?.Artist?.artistId == artistId) || slate.Any(p => p?.Artist?.artistId == artistId) ||
            scoutingNegotiations.ContainsKey(artistId) || IsConsideringApproach(artistId) ||
            scoutingKnowledge.Dossiers.Any(d => d.ArtistId == artistId && d.Priority is ScoutingPriority.Watch or ScoutingPriority.Pursue));

    /// <summary>The AI's daily market chose an act the player is pursuing. Instead of signing on the spot, the label comes
    /// to meet them and makes an offer the act weighs against the player's. True when the nomination is taken over (the
    /// label signs nobody today). Player-only; a world without a player label never reaches here.</summary>
    private bool InterceptAiNomination(SimulatedArtist artist, AILabel label, GameDate date) {
        if (Label == null || artist == null || label == null || label.isPlayerOwned || !LocalScenes.RivalOffers ||
            !IsPursuing(artist.artistId)) return false;
        if (ActiveCourtship(artist.artistId) == null && PendingOffer(artist.artistId) == null)
            StartCourtship(artist, label, date, RivalCourtshipOrigin.DailyMarket);
        return true;
    }

    // ========================================================================================
    // THE COURTSHIP
    // ========================================================================================

    private int StableDays(string key, int min, int max) =>
        Math.Min(max, min + (int)(StableUnit($"{SimulationSeedBootstrap.RequestedSeed ?? 0UL}|{key}") * (max - min + 1)));

    /// <summary>A rival starts after an act: by listening (the crowding roll) or, when its daily market or a run-out
    /// contract brought it, by going straight to the act.</summary>
    private RivalCourtship StartCourtship(SimulatedArtist artist, AILabel rival, GameDate date, RivalCourtshipOrigin origin) {
        int today = LocalSceneRoomService.Day(date);
        var work = LocalSceneRoomService.ObserveActWork(artist.artistId, today);
        string room = work.EngagementRoomId ?? work.BookedRoomIds.OrderBy(id => id, StringComparer.Ordinal).FirstOrDefault();
        var c = new RivalCourtship {
            Id = $"{Label.labelId}|{artist.artistId}|{rival.labelId}|{today}|court", PlayerLabelId = Label.labelId,
            ArtistId = artist.artistId, ArtistName = artist.stageName, RivalLabelId = rival.labelId, RivalLabelName = rival.labelName,
            Origin = origin, InquiryDay = today, RoomId = origin == RivalCourtshipOrigin.Crowding ? room : null,
        };
        rivalCourtships.Add(c);
        if (origin == RivalCourtshipOrigin.Crowding) {
            c.Stage = RivalCourtshipStage.Inquiry;
            c.NextDay = today + StableDays(c.Id + "|meet", 2, 4);
            TellInquiry(c, artist, today);
        } else {
            BeginMeeting(c, artist, today);
        }
        return c;
    }

    private void BeginMeeting(RivalCourtship c, SimulatedArtist artist, int today) {
        c.Stage = RivalCourtshipStage.Meeting;
        c.MeetingDay = today;
        c.NextDay = today + StableDays(c.Id + "|offer", 1, 2);
        TellMeeting(c, artist, today);
    }

    private void EndCourtship(RivalCourtship c, RivalCourtshipStage stage, int today, string reason) {
        c.Stage = stage; c.EndedDay = today; c.EndReason = reason;
    }

    /// <summary>The rival who is courting this act can still take it: reach, room, cash and the act's availability.</summary>
    private AILabel CourtingRivalStillAble(RivalCourtship c, SimulatedArtist artist, GameDate date) {
        AILabel able = c.Origin == RivalCourtshipOrigin.Renewal
            ? RosterManager.Instance?.FindRivalForPlayerRenewal(artist, date, c.RivalLabelId)
            : RosterManager.Instance?.FindRivalToPlayer(artist, date, c.RivalLabelId);
        return able?.labelId == c.RivalLabelId ? able : null;
    }

    /// <summary>Dawn: courtships whose day has come move on, fade, or end; then any booker who has news gets to tell it.</summary>
    private void AdvanceRivalCourtships(GameDate date) {
        int today = LocalSceneRoomService.Day(date);
        foreach (RivalCourtship c in rivalCourtships.Where(CourtshipLive).OrderBy(c => c.Id, StringComparer.Ordinal).ToList()) {
            var artist = ArtistManager.Instance?.GetArtist(c.ArtistId);
            bool renewal = c.Origin == RivalCourtshipOrigin.Renewal;
            bool gone = artist == null || artist.lifecycleStatus != ArtistLifecycleStatus.Active ||
                (renewal ? artist.labelId != Label.labelId || !RosterManager.IsContractMatured(artist, date.year, ChartManager.Instance?.GetCurrentChartWeek() ?? 0)
                         : !string.IsNullOrEmpty(artist.labelId));
            if (gone) { EndCourtship(c, RivalCourtshipStage.Ended, today, renewal ? "renewed or left" : "signed or gone"); continue; }
            if (today < c.NextDay) continue;
            // A handshake holds them: the rival waits for it to run out rather than walking away.
            if (!renewal && RosterManager.Instance?.IsHeldForPlayer(c.ArtistId) == true) { c.NextDay = today + 1; continue; }
            AILabel rival = CourtingRivalStillAble(c, artist, date);
            if (rival == null) { EndCourtship(c, RivalCourtshipStage.Faded, today, "the label can no longer take them on"); continue; }
            if (c.Stage == RivalCourtshipStage.Inquiry) {
                if (StableUnit($"{SimulationSeedBootstrap.RequestedSeed ?? 0UL}|{c.Id}|fade") < RivalInterestFadeShare) {
                    EndCourtship(c, RivalCourtshipStage.Faded, today, "heard them and passed");
                    continue;
                }
                BeginMeeting(c, artist, today);
            } else {
                var offer = MakeRivalOffer(artist, rival, date, c);
                c.Stage = RivalCourtshipStage.Offer; c.OfferId = offer.Id;
            }
        }
        DeliverBookerNews(today);
        DeliverDueSightings(today);
    }

    // ========================================================================================
    // WHAT THE PLAYER HEARS, AND FROM WHOM
    // ========================================================================================

    /// <summary>A musician on the player's roster who knows somebody in the act, on good terms: the first such pair in a
    /// stable order. Null when nobody connects them.</summary>
    private (Musician Theirs, Musician Ours, SimulatedArtist OurAct)? RosterContactFor(SimulatedArtist artist) {
        if (Label?.roster == null || artist?.members == null || !ContactNetworkService.Enabled) return null;
        foreach (SimulatedArtist ours in Label.roster.Where(a => a != null && a != artist).OrderBy(a => a.artistId, StringComparer.Ordinal))
        foreach (Musician m in artist.members.Where(m => m?.personId != null && m.isActive).OrderBy(m => m.personId, StringComparer.Ordinal))
        foreach (Musician o in ours.members.Where(o => o?.personId != null && o.isActive).OrderBy(o => o.personId, StringComparer.Ordinal))
            if (ContactNetworkService.Edge(m.personId, o.personId) is { Fallout: false }) return (m, o, ours);
        return null;
    }

    private static string PlaceOf(ScoutingObservation o) =>
        LocalScenePersistenceService.SceneIdFor(o.PlaceId ?? SceneRoomCatalog.Get(o.RoomId)?.PlaceId);

    /// <summary>The player has been working the act's scene lately: any evidence acquired there in the last fortnight.</summary>
    private bool WithinSceneTalk(SimulatedArtist artist, int today) {
        string scene = SceneOf(artist);
        if (scene == null) return false;
        return scoutingKnowledge.Dossiers.SelectMany(d => d.Observations)
            .Any(o => o.AcquiredDay <= today && o.AcquiredDay > today - SceneTalkDays && VisibilityOf(o.Kind).HasValue && PlaceOf(o) == scene);
    }

    private void TellInquiry(RivalCourtship c, SimulatedArtist artist, int today) {
        // The booker's news waits for the player to be in the room (DeliverBookerNews). A musician on the roster hears
        // it from a friend in the act the next day.
        if (RosterContactFor(artist) is { } link)
            Hear(c, new RivalSighting { Day = today + 1, Stage = RivalCourtshipStage.Inquiry, Channel = RivalSightingChannel.RosterContact,
                SourceId = link.Ours.personId, SourceName = $"{link.Ours.FullName} of {link.OurAct.stageName}",
                Text = $"{link.Ours.FullName} of {link.OurAct.stageName} hears from {link.Theirs.FullName} that somebody from {c.RivalLabelName} has been asking about {c.ArtistName}." });
    }

    private void TellMeeting(RivalCourtship c, SimulatedArtist artist, int today) {
        if (c.Origin == RivalCourtshipOrigin.Renewal || ActWouldDisclose(c.ArtistId)) {
            string who = string.IsNullOrEmpty(artist.managerName) ? $"{c.ArtistName}'s side" : artist.managerName;
            Hear(c, new RivalSighting { Day = today, Stage = RivalCourtshipStage.Meeting, Channel = RivalSightingChannel.ActSide,
                SourceId = c.ArtistId, SourceName = who,
                Text = c.Origin == RivalCourtshipOrigin.Renewal
                    ? $"{who} tells you {c.RivalLabelName} has been in touch now that {c.ArtistName}'s contract with you has run out."
                    : $"{who} tells you {c.RivalLabelName} sat down with them." });
        } else if (RosterContactFor(artist) is { } link) {
            Hear(c, new RivalSighting { Day = today + 1, Stage = RivalCourtshipStage.Meeting, Channel = RivalSightingChannel.RosterContact,
                SourceId = link.Ours.personId, SourceName = $"{link.Ours.FullName} of {link.OurAct.stageName}",
                Text = $"{link.Ours.FullName} of {link.OurAct.stageName} says {c.RivalLabelName} has met with {c.ArtistName}." });
        } else if (WithinSceneTalk(artist, today)) {
            string town = TownName(SceneOf(artist));
            Hear(c, new RivalSighting { Day = today + 1, Stage = RivalCourtshipStage.Meeting, Channel = RivalSightingChannel.SceneTalk,
                SourceId = "scene-talk|" + SceneOf(artist), SourceName = $"talk around {town}",
                Text = $"Word around {town} is that {c.RivalLabelName} has been talking to {c.ArtistName}. Nobody has mentioned an offer." });
        }
    }

    /// <summary>The booker of the room a rival heard the act in tells the player the next time the player is in that room,
    /// while the booker still remembers it. Read off the player's own visit evidence, so it is the same after a reload.</summary>
    private void DeliverBookerNews(int today) {
        foreach (RivalCourtship c in rivalCourtships.Where(c => c.RoomId != null &&
                     c.Known.All(k => k.Channel != RivalSightingChannel.RoomBooker)).OrderBy(c => c.Id, StringComparer.Ordinal)) {
            var visit = scoutingKnowledge.Dossiers.SelectMany(d => d.Observations)
                .Where(o => o.RoomId == c.RoomId && o.AcquiredDay >= c.InquiryDay && o.AcquiredDay <= c.InquiryDay + BookerMemoryDays && o.AcquiredDay <= today)
                .OrderBy(o => o.AcquiredDay).ThenBy(o => o.AcquiredMinute ?? 0).FirstOrDefault();
            if (visit == null) continue;
            var room = SceneRoomCatalog.Get(c.RoomId);
            string roomName = room?.Name ?? "the room";
            Hear(c, new RivalSighting { Day = visit.AcquiredDay, Stage = RivalCourtshipStage.Inquiry, Channel = RivalSightingChannel.RoomBooker,
                SourceId = "booker|" + c.RoomId, SourceName = KnowsSceneBooker(c.RoomId) ? room?.ContactTitle : $"the booker at {roomName}",
                Text = $"{(room == null || !KnowsSceneBooker(c.RoomId) ? "The booker at " + roomName : room.ContactName + ", who books " + roomName + ",")} mentions {c.RivalLabelName} sent somebody to hear {c.ArtistName} on {LocalSceneRoomService.Date(c.InquiryDay).ToHeadlineString()}." });
        }
    }

    /// <summary>Records what the player will learn, and hands it over now if its day has come. Word that travels overnight
    /// arrives the next day, never before.</summary>
    private void Hear(RivalCourtship c, RivalSighting s) {
        if (c.Known.Any(k => k.Stage == s.Stage && k.Channel == s.Channel)) return;
        c.Known.Add(s);
        if (s.Day <= TodayIndex()) Deliver(c, s);
    }

    /// <summary>Dawn: word whose day has come reaches the player.</summary>
    private void DeliverDueSightings(int today) {
        foreach (RivalCourtship c in rivalCourtships.OrderBy(c => c.Id, StringComparer.Ordinal))
            foreach (RivalSighting s in c.Known.Where(k => !k.Delivered && k.Day <= today).OrderBy(k => k.Day).ToList()) Deliver(c, s);
    }

    /// <summary>The player learns it: in the morning notes, and in the notebook's dossier as sourced evidence.</summary>
    private void Deliver(RivalCourtship c, RivalSighting s) {
        s.Delivered = true;
        Note(s.Text);
        var dossier = scoutingKnowledge.Dossiers.FirstOrDefault(d => d.ArtistId == c.ArtistId);
        if (dossier == null) return;
        string eventId = $"{c.Id}|{s.Stage}|{s.Channel}";
        if (dossier.Observations.Any(o => o.EventId == eventId)) return;
        dossier.Observations.Add(new ScoutingObservation {
            Id = "rival-interest-v1|" + eventId, EventId = eventId, LabelId = Label.labelId, ArtistId = c.ArtistId, ArtistName = c.ArtistName,
            Kind = ScoutingEvidenceKind.RivalInterest, SourceId = s.SourceId, SourceName = s.SourceName, Channel = s.Channel.ToString(),
            OccurredDay = s.Stage == RivalCourtshipStage.Inquiry ? c.InquiryDay : c.MeetingDay ?? c.InquiryDay, RoomId = s.Channel == RivalSightingChannel.RoomBooker ? c.RoomId : null,
            AcquiredDay = s.Day, Credibility = s.Channel switch {
                RivalSightingChannel.ActSide => "the act's own account",
                RivalSightingChannel.RoomBooker => "the booker saw it in the room",
                RivalSightingChannel.RosterContact => "secondhand, through somebody in the act",
                _ => "scene talk; nobody named their source",
            },
            Claim = s.Text,
        });
    }

    /// <summary>The newest thing the player has heard about a rival's interest in this act, while the pursuit is under way
    /// (an offer's own line, when known, says more). Null when the player has heard nothing.</summary>
    public string RivalInterestLine(string artistId) {
        var c = rivalCourtships.Where(x => x.ArtistId == artistId && (CourtshipLive(x) || x.Stage == RivalCourtshipStage.Offer)).LastOrDefault();
        var known = c?.Known.Where(k => k.Delivered).OrderBy(k => k.Day).LastOrDefault();
        return known == null ? null : $"{known.Text} (heard {LocalSceneRoomService.Date(known.Day).ToHeadlineString()})";
    }

    /// <summary>What the player knows of rival interest that never became an offer, or came before one.</summary>
    private string RivalInterestHistory(string artistId) {
        var lines = rivalCourtships.Where(c => c.ArtistId == artistId).OrderBy(c => c.InquiryDay)
            .SelectMany(c => c.Known.Where(k => k.Delivered).OrderBy(k => k.Day)
                .Select(k => $"{k.Text} ({k.SourceName}, {LocalSceneRoomService.Date(k.Day).ToHeadlineString()})"))
            .ToList();
        return lines.Count == 0 ? null : "Interest you heard about: " + string.Join(" ", lines);
    }

    // ========================================================================================
    // RENEWAL: a player's act whose contract has run out
    // ========================================================================================

    /// <summary>A player's act whose paper has run out is free to listen. A label that can take it on may come calling --
    /// more likely the hotter the act -- and the act's side always tells the player: it is leverage.</summary>
    private void ProcessRenewalPoaching(GameDate date) {
        if (Label?.roster == null || !LocalScenes.RivalOffers || RosterManager.Instance == null) return;
        int week = ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
        ulong seed = SimulationSeedBootstrap.RequestedSeed ?? 0UL;
        foreach (SimulatedArtist artist in Label.roster.Where(a => a != null).OrderBy(a => a.artistId, StringComparer.Ordinal).ToList()) {
            if (!RosterManager.IsContractMatured(artist, date.year, week) || ActiveCourtship(artist.artistId) != null ||
                PendingOffer(artist.artistId) != null) continue;
            float heat = Mathf.Clamp(0.25f + artist.momentum + 0.5f * artist.reputation + (artist.contractChartedRecords > 0 ? 0.25f : 0f), 0f, 1f);
            if (StableUnit($"{seed}|{artist.artistId}|{date.year}-{date.month}-{date.day}|RenewalPoach") >= RenewalPoachDailyBase * heat) continue;
            AILabel rival = RosterManager.Instance.FindRivalForPlayerRenewal(artist, date);
            if (rival != null) StartCourtship(artist, rival, date, RivalCourtshipOrigin.Renewal);
        }
    }

    // ========================================================================================
    // SAVE
    // ========================================================================================

    private List<RivalCourtship> CaptureRivalCourtships() => rivalCourtships.Count == 0 ? null : LocalSceneRoomService.Copy(rivalCourtships);

    private static bool ValidateRivalCourtships(List<RivalCourtship> saved, string labelId, out string message) {
        message = "The saved rival courtships are invalid; no player state was restored.";
        if (saved == null) return true; // Older saves hold none; none are invented.
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var c in saved) {
            if (c == null || string.IsNullOrEmpty(c.Id) || !ids.Add(c.Id) || c.PlayerLabelId != labelId || string.IsNullOrEmpty(c.ArtistId) ||
                string.IsNullOrEmpty(c.RivalLabelId) || !Enum.IsDefined(c.Stage) || !Enum.IsDefined(c.Origin) || c.InquiryDay < 0 ||
                c.MeetingDay < c.InquiryDay || (c.Stage is RivalCourtshipStage.Faded or RivalCourtshipStage.Ended) != c.EndedDay.HasValue || c.EndedDay < c.InquiryDay ||
                (c.Stage == RivalCourtshipStage.Meeting && !c.MeetingDay.HasValue) || (c.Stage == RivalCourtshipStage.Offer && string.IsNullOrEmpty(c.OfferId)) ||
                c.Known == null || c.Known.Any(k => k == null || !Enum.IsDefined(k.Stage) || !Enum.IsDefined(k.Channel) || k.Day < c.InquiryDay || string.IsNullOrEmpty(k.Text)) ||
                saved.Count(x => x?.ArtistId == c.ArtistId && CourtshipLive(x)) > 1) return false;
        }
        return true;
    }

    private void RestoreRivalCourtships(List<RivalCourtship> saved) {
        visibilityStamp = long.MinValue; // the restored dossiers are a different player's evidence
        rivalCourtships.Clear();
        if (saved != null) rivalCourtships.AddRange(LocalSceneRoomService.Copy(saved));
    }

    // ========================================================================================
    // FIXTURE ACCESS -- never rendered
    // ========================================================================================

    internal RivalCourtship StartCourtshipForCheck(SimulatedArtist artist, AILabel rival, RivalCourtshipOrigin origin) =>
        LocalSceneRoomService.Copy(StartCourtship(artist, rival, TimeManager.Instance?.CurrentDate ?? GameDate.StartDate, origin));
    internal void AdvanceRivalCourtshipsForCheck() => AdvanceRivalCourtships(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
    internal void ProcessRenewalPoachingForCheck() => ProcessRenewalPoaching(TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
    internal bool InterceptAiNominationForCheck(SimulatedArtist artist, AILabel label) =>
        InterceptAiNomination(artist, label, TimeManager.Instance?.CurrentDate ?? GameDate.StartDate);
    internal double AiVisibilityWeightForCheck(SimulatedArtist artist) => AiVisibilityWeight(artist);
    internal void SetCourtshipRoomForCheck(string courtshipId, string roomId) {
        var c = rivalCourtships.FirstOrDefault(x => x.Id == courtshipId);
        if (c != null) c.RoomId = roomId;
    }
    internal static float StableUnitForCheck(string key) => StableUnit(key);
}
