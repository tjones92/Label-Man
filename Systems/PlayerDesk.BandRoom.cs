using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

// ============================================================================================
// THE BAND ROOM -- characters talk (SimTools/BandMemberSimulationDirective.md Part B, Phase 3).
//
// Members of the player's acts come into the office with grievances the simulation can back with a number,
// with requests, with news. What the player does about it writes the sim: every verb moves an edge, a
// stage, a credit, a life state, a lineup, the label's cash, or a member's trust in the label. The grammar
// is the Rolodex call's (BandRoomScene.cs); the writes go through BandLifeService and CowritingService, the
// same code the AI's default policy uses.
//
// Player-only. Nothing here runs for an AI act, and nothing here draws from the population or global stream
// in a way an AI-only headless run can reach: there is no player in that run.
// ============================================================================================
public partial class PlayerDesk {
	// ---- tunables -----------------------------------------------------------------------------------
	public const int MaxVisitsSurfacedPerWeek = 2;
	public const int VisitExpiryWeeks = 4;
	public const int BandRoomHours = 1;
	public const int MediateHours = 2;
	public const int WritingSessionHours = 3;
	public const int RequestCooldownWeeks = 26;
	public const float DefaultTrust = 0.5f;
	public const float ResidencyWeeklyPay = 22f;
	public const float TourWeeklyNet = 30f;
	public const float LabelCutInShare = 0.25f;
	public const int LabelCutInSongs = 3;
	/// <summary>Below this much money of their own, a married member with children asks for an advance (§4.15).</summary>
	public const float BandRoomBrokeWealth = 1500f;

	/// <summary>One button in a Band Room scene.</summary>
	public sealed class BandOption {
		public string label;
		public string subLabel;
		public ExecutiveVoice voice;
		public BandVerb verb;
		public CallCounter counter = CallCounter.None;
		public string targetPersonId;
		public bool isBluff;
		public bool enabled = true;
		public string disabledReason;
		public int hours;
		public float cash;
	}

	/// <summary>The live scene for an opened visit. Not persisted: reopening rebuilds it from the visit.</summary>
	public sealed class BandRoomLive {
		public BandVisit visit;
		public BandRoomContext ctx;
		public CallStage stage = CallStage.Open;
		public readonly List<CallLine> transcript = new();
		public BandVerb pendingVerb;
		public string pendingTarget;
		public float chanceModifier;
		public void Say(RolodexSceneBeat beat, string text, string speaker = null, ExecutiveVoice voice = ExecutiveVoice.None, bool isPlayer = false) {
			if (string.IsNullOrEmpty(text)) return;
			transcript.Add(new CallLine { beat = beat, text = text, speaker = speaker, voice = voice, isPlayer = isPlayer });
		}
	}

	/// <summary>An act's booked year on the road: the Hamburg lever. Sets the act's road load for the annual
	/// pass, pays a little each week, and wears on them.</summary>
	public sealed class RoadBooking {
		public string ArtistId;
		public int Year;
		public int ResidencyWeeks;
		public int TourWeeks;
		public int WeeksRemaining;
		public int TimeOffUntilWeek = -1;
		public string DryingOutPersonId;
		public int DryOutUntilWeek = -1;
	}

	/// <summary>A session player hired for the act's next date in the studio.</summary>
	public sealed class SessionPlayerBooking {
		public string ArtistId;
		public string ReplacesPersonId;
		public string Name;
		public float Skill;
		public float SightReading;
		public float Fee;
	}

	/// <summary>A writing session in progress: two writers in a room, a song at the end of it.</summary>
	public sealed class WritingSessionPending {
		public string ArtistId;
		public List<string> TeamPersonIds = new();
		public string StaffWriterName;
		public GameDate ReadyDate;
		public float Hook;
		public float Originality;
	}

	private readonly List<BandVisit> bandVisits = new();
	private readonly Dictionary<string, float> memberTrust = new(StringComparer.Ordinal);
	private readonly Dictionary<string, RoadBooking> roadBookings = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string> promisedCredit = new(StringComparer.Ordinal);
	private readonly Dictionary<string, int> labelCutInSongsLeft = new(StringComparer.Ordinal);
	private readonly HashSet<string> knewAndHid = new(StringComparer.Ordinal);
	private readonly Dictionary<string, int> requestCooldownWeek = new(StringComparer.Ordinal);
	private readonly Dictionary<string, SessionPlayerBooking> sessionPlayers = new(StringComparer.Ordinal);
	private readonly List<WritingSessionPending> writingSessions = new();
	private int visitCounter;
	private int visitsSurfacedWeek = -1;

	public BandRoomLive ActiveVisit { get; private set; }

	private int ChartWeekNow => ChartManager.Instance?.GetCurrentChartWeek() ?? 0;
	private int YearNow => TimeManager.Instance?.CurrentDate.year ?? 1960;

	/// <summary>Wired from _Ready. The signal only ever carries the player's own acts.</summary>
	private void InitBandRoom() {
		BandLifeService.OnPlayerSignal += OnBandLifeSignal;
		BandLifeService.PlayerRoadLoad = PlayerRoadLoadFor;
		CowritingService.PlayerTeamOverride = TeamOverrideFor;
	}

	private void TeardownBandRoom() {
		BandLifeService.OnPlayerSignal -= OnBandLifeSignal;
		if (BandLifeService.PlayerRoadLoad == PlayerRoadLoadFor) BandLifeService.PlayerRoadLoad = null;
		CowritingService.PlayerTeamOverride = null;
	}

	public float TrustOf(string personId) => personId != null && memberTrust.TryGetValue(personId, out float t) ? t : DefaultTrust;
	private void AddTrust(Musician m, float delta) {
		if (m == null) return;
		memberTrust[m.personId] = Mathf.Clamp(TrustOf(m.personId) + delta, 0f, 1f);
	}

	// ==================================================================================================
	// THE QUEUE
	// ==================================================================================================

	/// <summary>Visits waiting, most urgent first, capped so the Band Room never becomes a chore.</summary>
	public IReadOnlyList<BandVisit> PendingVisits() {
		var open = bandVisits.Where(v => !v.resolved && ArtistManager.Instance?.GetArtist(v.artistId) != null)
			.OrderBy(v => Priority(v.kind)).ThenBy(v => v.createdWeek).ThenBy(v => v.visitId, StringComparer.Ordinal).ToList();
		return open;
	}

	public int PendingVisitCount => PendingVisits().Count;

	private static int Priority(BandVisitKind kind) => kind switch {
		BandVisitKind.Death => 0, BandVisitKind.Departure => 1, BandVisitKind.Ultimatum => 2, BandVisitKind.LifeEvent => 3,
		BandVisitKind.AffairDiscovered => 4, BandVisitKind.Grievance => 5, BandVisitKind.FirstHit => 6,
		BandVisitKind.Request => 7, BandVisitKind.AffairSecret => 8, _ => 9
	};

	private BandVisit Queue(SimulatedArtist a, Musician m, BandVisitKind kind, StrainCause cause, Musician other = null,
		string headline = null, Action<BandVisit> fill = null) {
		if (a == null || m == null) return null;
		// One open visit per person per kind: a second grievance from the same member replaces the first.
		bandVisits.RemoveAll(v => !v.resolved && v.personId == m.personId && v.kind == kind);
		var visit = new BandVisit {
			visitId = $"visit_{++visitCounter}", artistId = a.artistId, personId = m.personId, otherPersonId = other?.personId,
			kind = kind, cause = cause, createdWeek = ChartWeekNow, year = YearNow,
			expiresWeek = ChartWeekNow + (kind is BandVisitKind.Death or BandVisitKind.Departure or BandVisitKind.LifeEvent ? 52 : VisitExpiryWeeks),
			headline = headline
		};
		fill?.Invoke(visit);
		bandVisits.Add(visit);
		Note($"BAND ROOM — {a.stageName}: {headline ?? DescribeVisit(visit, m, other)}");
		Changed?.Invoke();
		return visit;
	}

	private static string DescribeVisit(BandVisit v, Musician m, Musician other) => v.kind switch {
		BandVisitKind.Grievance => $"{m.firstName} wants a word ({CauseWord(v.cause)}).",
		BandVisitKind.Ultimatum => $"{m.firstName} has an ultimatum ({CauseWord(v.cause)}).",
		BandVisitKind.Request => v.request switch {
			BandRequest.SongOnRecord => $"{m.firstName} wants a song on the next record.",
			BandRequest.SoloSingle => $"{m.firstName} wants to cut one under {(m.isMale ? "his" : "her")} own name.",
			BandRequest.Advance => $"{m.firstName} needs money.",
			BandRequest.OffTheRoad => $"{m.firstName} wants off the road.",
			_ => $"{m.firstName} has a request."
		},
		BandVisitKind.FirstHit => $"{m.firstName} wants to talk about the credits now there's a hit.",
		BandVisitKind.LifeEvent => v.lifeEvent switch {
			"drafted" => $"{m.firstName} has been drafted.",
			"marriage" => $"{m.firstName} got married.",
			"studio-only" => $"{m.firstName} is coming off the road.",
			_ => $"News about {m.firstName}."
		},
		BandVisitKind.Death => $"{m.FullName} has died.",
		BandVisitKind.AffairSecret => $"The Street has heard something about {m.firstName}.",
		BandVisitKind.AffairDiscovered => $"{m.firstName} found out about {other?.firstName ?? "it"}.",
		BandVisitKind.Departure => $"{m.firstName} has left.",
		BandVisitKind.SubstanceRead => $"Something isn't right with {m.firstName}.",
		_ => $"{m.firstName} came by."
	};

	public static string CauseWord(StrainCause cause) => cause switch {
		StrainCause.CreditAndMoney => "credit and money", StrainCause.Spotlight => "the spotlight",
		StrainCause.Direction => "the direction", StrainCause.Reliability => "someone not pulling their weight",
		StrainCause.Romance => "a falling-out", StrainCause.Burnout => "the road", StrainCause.Outsider => "an outsider", _ => "the band"
	};

	/// <summary>Turns band-life signals about the player's acts into visits.</summary>
	private void OnBandLifeSignal(BandLifeEvent e) {
		if (Label == null || e == null) return;
		SimulatedArtist a = ArtistManager.Instance?.GetArtist(e.artistId);
		if (a == null || a.labelId != Label.labelId) return;
		Musician m = FindPerson(a, e.personId);
		Musician other = FindPerson(a, e.otherPersonId);
		switch (e.eventType) {
			case "stage-brewing": Queue(a, m, BandVisitKind.Grievance, e.cause, other); break;
			case "stage-ultimatum": Queue(a, m, BandVisitKind.Ultimatum, e.cause, other); break;
			case "studio-only": Queue(a, m, BandVisitKind.LifeEvent, StrainCause.Burnout, fill: v => v.lifeEvent = "studio-only"); break;
			case "marriage": Queue(a, m, BandVisitKind.LifeEvent, StrainCause.Outsider, fill: v => v.lifeEvent = "marriage"); break;
			case "affair-secret":
				// Information asymmetry (§4.9): only an executive with a Street ear hears it first.
				if (InstinctProfile.TheStreet >= 3) Queue(a, m, BandVisitKind.AffairSecret, StrainCause.Romance, other);
				break;
			case "affair-discovered": {
				Queue(a, m, BandVisitKind.AffairDiscovered, StrainCause.Romance, other);
				// Sat on it and it came out anyway: the wronged member knows you knew.
				string key = AffairKey(e.personId, e.otherPersonId);
				if (knewAndHid.Remove(key)) { AddTrust(m, -0.25f); Note($"{m?.firstName} found out you knew."); }
				break;
			}
			case "departure":
			case "departure-dissolves": {
				Musician gone = m ?? PersonPoolOrRegistry(e.personId);
				if (e.kind == DepartureKind.Death)
					Queue(a, gone, BandVisitKind.Death, StrainCause.Burnout, fill: v => { v.channel = e.channel; v.departureKind = e.kind; });
				else if (e.kind == DepartureKind.Service)
					Queue(a, gone, BandVisitKind.LifeEvent, StrainCause.Outsider, fill: v => { v.lifeEvent = "drafted"; v.departureKind = e.kind; });
				else
					Queue(a, gone, BandVisitKind.Departure, e.cause, other, fill: v => v.departureKind = e.kind);
				break;
			}
			case "dissolution":
				Note($"{a.stageName} is finished. {e.detail}.");
				break;
		}
	}

	private static Musician FindPerson(SimulatedArtist a, string personId) =>
		personId == null ? null : a.members.FirstOrDefault(x => x.personId == personId) ?? PersonPoolOrRegistry(personId);

	private static Musician PersonPoolOrRegistry(string personId) =>
		personId == null ? null : PersonPool.Get(personId)?.person ?? ArtistManager.Instance?.GetMusician(personId);

	private static string AffairKey(string a, string b) => string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";

	/// <summary>Weekly: expire stale visits, raise fact-backed requests, run the road bookings.</summary>
	private void ProcessBandRoomWeek(GameDate date) {
		if (Label == null || !BandLife.RosterChurn) return;
		int week = ChartWeekNow;
		foreach (BandVisit v in bandVisits.Where(v => !v.resolved && v.expiresWeek < week)) v.resolved = true;
		bandVisits.RemoveAll(v => v.resolved && v.expiresWeek < week - 52);
		RaiseRequests(week);
		RunRoadBookings(date, week);
	}

	/// <summary>
	/// Requests a member makes when a fact backs them (§5.2): a writer with nothing on the record, a name the
	/// public learned, a new family and no money, years on the road. Rate-limited per person.
	/// </summary>
	private void RaiseRequests(int week) {
		foreach (SimulatedArtist a in Roster.ToList()) {
			if (a.members.Count == 0) continue;
			int originals = CompositionCatalogService.WriterStintLedger.Where(e => e.artistId == a.artistId).Sum(e => e.songs);
			foreach (Musician m in a.members.Where(x => x.isActive).ToList()) {
				if (requestCooldownWeek.TryGetValue(m.personId, out int last) && week - last < RequestCooldownWeeks) continue;
				if (bandVisits.Any(v => !v.resolved && v.personId == m.personId)) continue;
				var stint = CompositionCatalogService.GetStint(m.personId, a.artistId);
				BandRequest request = BandRequest.None;
				StrainCause cause = StrainCause.CreditAndMoney;
				if (a.top40Hits > 0 && !requestCooldownWeek.ContainsKey("firsthit|" + a.artistId) && m.creativity > 0.5f &&
					CompositionCatalogService.GetStintCreditShare(m.personId, a) < 0.35f && originals >= 2) {
					requestCooldownWeek["firsthit|" + a.artistId] = week;
					requestCooldownWeek[m.personId] = week;
					Queue(a, m, BandVisitKind.FirstHit, StrainCause.CreditAndMoney);
					continue;
				}
				if (m.creativity >= 0.6f && originals >= 3 && (stint?.songs ?? 0) == 0) request = BandRequest.SongOnRecord;
				else if (a.members.Count > 1 && BandLifeService.IsSoloViable(m, a, YearNow)) { request = BandRequest.SoloSingle; cause = StrainCause.Spotlight; }
				// Wealth reader 4 (§4.15): the advance and the "we don't need the road" asks are backed by the member's own
				// money when the stock exists -- a broke father asks for an advance; a rich, worn-out one asks to stop touring.
				else if (m.partner?.state == PartnerState.Married && m.hasChildren &&
					(BandLife.MemberWealthActive ? m.wealth < BandRoomBrokeWealth : a.totalRoyaltyEarnings < 200f)) request = BandRequest.Advance;
				else if (m.lifeState == MemberLifeState.Active && (BandLife.RoadFatigue ? m.fatigue >= 2f : m.roadYears >= 3f) &&
					(m.hasChildren || m.partner?.state == PartnerState.Married || MemberWealthService.Norm(m) >= MemberWealthService.StudioOnlyAffordNorm))
					{ request = BandRequest.OffTheRoad; cause = StrainCause.Burnout; }
				if (request == BandRequest.None) continue;
				// Not every fact becomes a visit the week it is true.
				if (!BandLife.Chance($"request|{m.personId}|{week}", 0.25f)) continue;
				requestCooldownWeek[m.personId] = week;
				Queue(a, m, BandVisitKind.Request, cause, fill: v => v.request = request);
			}
			// The Street notices someone using before anyone says it.
			if (InstinctProfile.TheStreet >= 2)
				foreach (Musician m in a.members.Where(x => x.isActive && x.substanceLoad > 0.35f)) {
					string key = "substance|" + m.personId;
					if (requestCooldownWeek.TryGetValue(key, out int seen) && week - seen < 52) continue;
					requestCooldownWeek[key] = week;
					Queue(a, m, BandVisitKind.SubstanceRead, StrainCause.Reliability);
				}
		}
	}

	// ==================================================================================================
	// OPENING A VISIT
	// ==================================================================================================

	public bool OpenVisit(string visitId, out string message) {
		message = "";
		BandVisit visit = bandVisits.FirstOrDefault(v => v.visitId == visitId && !v.resolved);
		if (visit == null) { message = "They've gone."; return false; }
		SimulatedArtist a = ArtistManager.Instance?.GetArtist(visit.artistId);
		if (a == null) { visit.resolved = true; message = "That act is gone."; return false; }
		BandRoomContext ctx = BuildBandContext(visit, a);
		if (ctx.member == null) { visit.resolved = true; message = "They're gone."; return false; }
		var live = new BandRoomLive { visit = visit, ctx = ctx };
		string speaker = visit.kind is BandVisitKind.Death or BandVisitKind.AffairSecret or BandVisitKind.SubstanceRead
			or BandVisitKind.Departure or BandVisitKind.LifeEvent ? null : ctx.member.FullName;
		live.Say(RolodexSceneBeat.Opening, BandRoomFragments.Pick(RolodexSceneBeat.Opening, ctx));
		foreach ((ExecutiveVoice voice, string line) in PassiveReads(ctx)) live.Say(RolodexSceneBeat.PassiveRead, line, voice: voice);
		live.Say(RolodexSceneBeat.SituationRead, BandRoomFragments.Pick(RolodexSceneBeat.SituationRead, ctx), speaker: LineSpeaker(visit, ctx));
		ActiveVisit = live;
		Changed?.Invoke();
		return true;
	}

	private static string LineSpeaker(BandVisit v, BandRoomContext c) =>
		v.kind is BandVisitKind.Grievance or BandVisitKind.Ultimatum or BandVisitKind.Request or BandVisitKind.FirstHit or BandVisitKind.AffairDiscovered
			? c.member.FullName : null;

	public void CloseVisit() { ActiveVisit = null; Changed?.Invoke(); }

	private BandRoomContext BuildBandContext(BandVisit v, SimulatedArtist a) {
		int year = YearNow;
		Musician m = FindPerson(a, v.personId);
		Musician o = FindPerson(a, v.otherPersonId);
		var c = new BandRoomContext {
			visit = v, artist = a, member = m, other = o, instincts = InstinctProfile, year = year,
			constitution = BandLifeService.ConstitutionOf(a), cause = v.cause,
			trust = m == null ? DefaultTrust : TrustOf(m.personId), morale = a.morale, age = m?.GetAge(year) ?? 0,
			top40Hits = a.top40Hits, charted = a.charted, unrecoupedAdvance = a.unrecoupedAdvance,
			contractExpiresYear = a.contractExpiresYear, leavingMemberOption = a.contractLeavingMemberOption,
			labelCash = Label?.cashReserves ?? 0f
		};
		if (m == null) return c;
		c.voice = VoiceOf(m);
		c.stage = m.departureStage;
		c.edge = o != null ? BandLifeService.FindEdge(a, m, o) : BandLifeService.WorstEdgeOf(a, m);
		if (c.other == null && c.edge != null) c.other = FindPerson(a, c.edge.Other(m.personId));
		c.strain = c.edge?.strain ?? 0f;
		var stints = CompositionCatalogService.WriterStintLedger.Where(e => e.artistId == a.artistId).ToList();
		c.actOriginals = (int)Math.Round(stints.Sum(e => e.creditMass));
		c.memberSongs = stints.FirstOrDefault(e => e.personId == m.personId)?.songs ?? 0;
		c.otherSongs = c.other == null ? 0 : stints.FirstOrDefault(e => e.personId == c.other.personId)?.songs ?? 0;
		c.memberCreditShare = CompositionCatalogService.GetStintCreditShare(m.personId, a);
		c.otherCreditShare = c.other == null ? 0f : CompositionCatalogService.GetStintCreditShare(c.other.personId, a);
		c.memberWrites = m.isPrimaryWriter || m.creativity >= SimulatedArtist.MinorWriterCreativityFloor;
		c.otherWrites = c.other != null && (c.other.isPrimaryWriter || c.other.creativity >= SimulatedArtist.MinorWriterCreativityFloor);
		c.pactBetween = c.other != null && CowritingService.FindPartnership(a, m.personId, c.other.personId)?.pact == true;
		float total = a.members.Where(x => x.isActive).Sum(x => x.personalRecognition);
		c.memberSpotlight = total > 0.005f ? m.personalRecognition / total : 0f;
		c.otherSpotlight = total > 0.005f && c.other != null ? c.other.personalRecognition / total : 0f;
		c.soloViable = BandLifeService.IsSoloViable(m, a, year);
		c.roadYears = m.roadYears;
		c.substanceLoad = m.substanceLoad;
		c.married = m.partner?.state == PartnerState.Married;
		c.hasChildren = m.hasChildren;
		c.partnerSitsIn = m.partner?.sitsInOnSessions == true && c.married;
		c.partnerName = m.partner?.name;
		c.secretAffair = a.relations?.FirstOrDefault(e => e.secret && e.Involves(m.personId));
		if (c.secretAffair != null) {
			Musician wronged = FindPerson(a, c.secretAffair.secretAgainstPersonId);
			c.affairPartnerName = wronged?.partner?.name;
			if (v.kind == BandVisitKind.AffairSecret) c.other = wronged;
		}
		return c;
	}

	public static MemberVoice VoiceOf(Musician m) {
		if (m.ego > 0.55f && m.temperament < 0.45f) return MemberVoice.Blunt;
		if (m.reliability < 0.45f) return MemberVoice.Evasive;
		if (m.loyalty > 0.6f && m.ego < 0.35f) return MemberVoice.Apologetic;
		return MemberVoice.Measured;
	}

	/// <summary>
	/// The instincts read the room (§5.1): interpreted intelligence, never a raw stat, and only facts that are
	/// true. Ear: who is actually carrying the record. Street: who is seeing whom, who is using, who is already
	/// talking to another label. Suit: what the contract says. Fixer: who can be talked down.
	/// </summary>
	private List<(ExecutiveVoice, string)> PassiveReads(BandRoomContext c) {
		var reads = new List<(ExecutiveVoice, string)>();
		ExecutiveInstinctProfile i = c.instincts;
		Musician m = c.member;
		if (c.visit.kind is BandVisitKind.Death) return reads;
		if (i.TheEar >= 3 && c.actOriginals > 0) {
			Musician carrier = c.artist.members.Where(x => x.isActive).OrderByDescending(x => x.creativity)
				.ThenBy(x => x.personId, StringComparer.Ordinal).FirstOrDefault();
			if (carrier != null)
				reads.Add((ExecutiveVoice.Ear, carrier == m
					? $"{m.firstName} is the one writing the hooks. You can hear it on every side."
					: $"Whatever the credits say, {carrier.firstName} is carrying the writing."));
		}
		if (i.TheStreet >= 3 && c.secretAffair != null && c.visit.kind != BandVisitKind.AffairSecret)
			reads.Add((ExecutiveVoice.Street, $"{m.firstName} has been riding in somebody else's car. Nobody in the band knows yet."));
		if (i.TheStreet >= 2 && c.substanceLoad > 0.35f && c.visit.kind != BandVisitKind.SubstanceRead)
			reads.Add((ExecutiveVoice.Street, c.substanceLoad > 0.55f ? $"{m.firstName} is using, and it's getting worse." : $"{m.firstName} is using."));
		if (i.TheStreet >= 4 && c.soloViable)
			reads.Add((ExecutiveVoice.Street, $"Somebody's been buying {m.firstName} lunch. Another label, by the look of it."));
		if (i.TheSuit >= 3) {
			int left = c.contractExpiresYear - c.year;
			reads.Add((ExecutiveVoice.Suit, left > 0
				? $"The paper has {left} year{(left == 1 ? "" : "s")} to run{(c.leavingMemberOption ? ", and you hold an option on anyone who leaves." : ".")}"
				: "Their contract is up. Nothing holds them but goodwill."));
		}
		if (i.TheFixer >= 3 && c.visit.kind is BandVisitKind.Grievance or BandVisitKind.Ultimatum)
			reads.Add((ExecutiveVoice.Fixer, m.ego > 0.55f
				? $"{m.firstName} won't be talked down. {(m.isMale ? "He" : "She")} wants to be seen to win."
				: m.loyalty > 0.6f ? $"{m.firstName} wants to stay. Give {(m.isMale ? "him" : "her")} a way to."
				: $"{m.firstName} can be talked round, if somebody gives a little."));
		return reads;
	}

	// ==================================================================================================
	// VERBS
	// ==================================================================================================

	/// <summary>The verbs on offer for this visit, gated on the facts that make them possible.</summary>
	public List<BandOption> VisitOptions(BandRoomLive live) {
		var opts = new List<BandOption>();
		BandRoomContext c = live.ctx;
		BandVisit v = live.visit;
		SimulatedArtist a = c.artist;
		Musician m = c.member;
		Musician o = c.other;
		void Offer(BandVerb verb, string label, string sub, ExecutiveVoice voice = ExecutiveVoice.None, int hours = 0,
			float cash = 0f, string target = null, bool enabled = true, string why = null) =>
			opts.Add(new BandOption { verb = verb, label = label, subLabel = sub, voice = voice, hours = hours, cash = cash,
				targetPersonId = target, enabled = enabled, disabledReason = why });
		bool memberPresent = a.members.Contains(m);

		switch (v.kind) {
			case BandVisitKind.Grievance:
			case BandVisitKind.Ultimatum:
			case BandVisitKind.FirstHit:
			case BandVisitKind.AffairDiscovered:
				Offer(BandVerb.HearThemOut, $"Hear {m.firstName} out  ({BandRoomHours}h)", "Lets the steam off. The cause is still there.", hours: BandRoomHours);
				StrainCause cause = v.kind == BandVisitKind.FirstHit ? StrainCause.CreditAndMoney : v.cause;
				if (cause == StrainCause.CreditAndMoney) {
					if (o != null && c.memberWrites && c.otherWrites && !c.pactBetween)
						Offer(BandVerb.ConcedePact, $"Concede a pact: {m.firstName} and {o.firstName} share credit on everything",
							"The credit gap closes for good. The rivalry may cool with it, and the records with the rivalry.", target: o.personId);
					if (c.memberWrites)
						Offer(BandVerb.GiveThemASong, $"Promise {m.firstName} a song on the next record", "Their name goes on the next original you cut. It has to fit.");
					float pay = PayAmount(a);
					Offer(BandVerb.PayThem, $"Pay {(m.isMale ? "him" : "her")}  (${pay:N0})", "The money half of it goes away. The others will notice.",
						cash: pay, enabled: Label.cashReserves >= pay, why: $"You don't have ${pay:N0}.");
				}
				if (cause == StrainCause.Spotlight && a.members.Count > 1 && !a.stageName.Contains(" & the "))
					Offer(BandVerb.SpotlightThem, $"Put {m.firstName}'s name on the act: \"{m.firstName} {m.lastName} & {AndThe(a)}\"",
						"Their name and loyalty rise. Everyone else's grievance about the spotlight rises faster. It speeds a solo exit -- which you could sign.");
				if (cause == StrainCause.Burnout) {
					Offer(BandVerb.TimeOff, "Give the act time off", "Morale up, the road's grudge eased. Momentum fades while they're away.");
					if (m.isPrimaryWriter || m.studioEfficiency > 0.6f)
						Offer(BandVerb.OffTheRoadYes, $"Take {m.firstName} off the road -- records only", "Stays in the band for the sessions. Somebody else makes the dates.");
				}
				if (cause == StrainCause.Reliability && o != null) {
					if (o.substanceLoad > 0.35f && InstinctProfile.TheStreet >= 2)
						Offer(BandVerb.DryOut, $"Dry {o.firstName} out", "Weeks off the road. The habit eases if they let it.", target: o.personId);
					Offer(BandVerb.Fire, $"Fire {o.firstName}", $"{o.firstName}'s friends in the band won't forget it.", target: o.personId);
				}
				if (cause == StrainCause.Outsider && m.partner?.sitsInOnSessions == true)
					Offer(BandVerb.AskPartnerOut, $"Ask {m.partner.name} to stay out of the sessions", $"The others breathe. {m.firstName} won't thank you.");
				if (cause == StrainCause.Romance && o != null && a.members.Contains(o))
					Offer(BandVerb.KeepApart, $"Keep {m.firstName} and {o.firstName} apart on the road", "Separate cars, separate floors. It buys time, not peace.", target: o.personId);
				if (o != null && a.members.Contains(o)) {
					Offer(BandVerb.SideWith, $"Side with {m.firstName}", $"{m.firstName}'s trust in you rises. {o.firstName} will hold it against the label.", target: o.personId);
					Offer(BandVerb.SideAgainst, $"Side with {o.firstName}", $"{o.firstName}'s trust rises. {m.firstName} hears whose side you're on.", target: o.personId);
				}
				Offer(BandVerb.Mediate, $"Get them in a room together  ({MediateHours}h)", "It can work. It can also go badly in front of you.",
					ExecutiveVoice.Fixer, hours: MediateHours, enabled: o != null && a.members.Contains(o), why: "There's nobody to sit across from.");
				if (InstinctProfile.TheSuit >= 2)
					Offer(BandVerb.LeanOnContract, "Remind them what they signed", "Works on the loyal and the modest. On a big ego it turns grumbling into an ultimatum.", ExecutiveVoice.Suit);
				if (v.kind == BandVisitKind.Ultimatum && memberPresent)
					Offer(BandVerb.LetThemGo, $"Let {m.firstName} go", "Accept it now, on your terms, rather than at the end of the year on theirs.");
				break;

			case BandVisitKind.Request:
				switch (v.request) {
					case BandRequest.SongOnRecord:
						Offer(BandVerb.GiveThemASong, $"Yes -- {m.firstName}'s song goes on the next record", "Their name on the next original. It has to fit the act.");
						break;
					case BandRequest.SoloSingle:
						Offer(BandVerb.SoloSingleYes, "Yes -- let them cut one", $"{m.firstName}'s name gets out on its own. The others won't love it.");
						break;
					case BandRequest.Advance: {
						float amount = 100f;
						Offer(BandVerb.AdvanceYes, $"Advance {(m.isMale ? "him" : "her")} ${amount:N0} against royalties", "Recoupable. It'll come out of the act's earnings.",
							cash: amount, enabled: Label.cashReserves >= amount, why: $"You don't have ${amount:N0}.");
						break;
					}
					case BandRequest.OffTheRoad:
						Offer(BandVerb.OffTheRoadYes, $"Yes -- {m.firstName} stays home and makes the records", "Somebody else makes the dates.");
						break;
				}
				Offer(BandVerb.Decline, "Not now", "They'll remember you said it.");
				break;

			case BandVisitKind.LifeEvent:
				if (v.lifeEvent == "drafted") {
					// A drafted voice stays on the roll and the act waits; anyone else leaves a chair to fill.
					if (!memberPresent) OfferReplacements(opts, a, m, live);
					Offer(BandVerb.CarryOn, "Carry on short-handed", "The others cover. He's back in two years, if he still wants to be.");
					Offer(BandVerb.PutOnIce, "Put the act on ice", "No dates, no road. The act waits for him.");
				} else if (v.lifeEvent == "marriage") {
					Offer(BandVerb.Congratulate, "Send flowers and a bottle", $"{m.firstName} remembers that kind of thing.", cash: 15f, enabled: Label.cashReserves >= 15f);
					if (m.partner?.sitsInOnSessions == true)
						Offer(BandVerb.AskPartnerOut, $"Ask {m.partner.name} to keep out of the sessions", "The others will thank you. The newlyweds won't.");
				} else {
					Offer(BandVerb.CarryOn, "Fine -- make the records", "Somebody else will have to make the dates.");
					OfferReplacements(opts, a, m, live);
				}
				break;

			case BandVisitKind.Death: {
				var shelf = masters.Where(x => x.ArtistId == a.artistId && !x.Released && !x.Scheduled).ToList();
				if (shelf.Count > 0) {
					Offer(BandVerb.ReleaseMasters, $"Put out what's in the can ({shelf.Count} master{(shelf.Count == 1 ? "" : "s")})",
						"People will buy it, and some of them will say why. The band will hear them say it.");
					Offer(BandVerb.HoldMasters, "Hold the masters for now", "The survivors will remember who was decent.");
				}
				if (a.lifecycleStatus == ArtistLifecycleStatus.Active && a.members.Count > 0) {
					OfferReplacements(opts, a, m, live);
					Offer(BandVerb.CarryOn, "Let them grieve, and carry on as they are", "Nobody's replaced yet.");
				} else Offer(BandVerb.CarryOn, "Hang up the phone", "There is nothing to decide today.");
				break;
			}

			case BandVisitKind.AffairSecret:
				if (o != null) {
					Offer(BandVerb.TellAboutAffair, $"Tell {o.firstName}", $"It comes out now, on your word. {o.firstName} will trust you for it.", ExecutiveVoice.Street, target: o.personId);
					Offer(BandVerb.KeepQuiet, "Keep it to yourself", "If it comes out later that you knew, it costs you.", ExecutiveVoice.Street, target: o.personId);
					Offer(BandVerb.KeepApart, "Keep them apart on the road", "Separate cars, separate dates. It may burn out on its own.", ExecutiveVoice.Street, target: o.personId);
				}
				break;

			case BandVisitKind.Departure: {
				SimulatedArtist solo = v.departureKind == DepartureKind.SoloCareer ? FindSpinOut(m) : null;
				if (solo != null && a.contractLeavingMemberOption && string.IsNullOrEmpty(solo.labelId))
					Offer(BandVerb.ExerciseOption, $"Exercise the leaving-member option: sign {m.FullName} as a solo act",
						"It's in the paper they signed. You're bound by it too.", ExecutiveVoice.Suit);
				if (a.lifecycleStatus == ArtistLifecycleStatus.Active && a.members.Count > 0) {
					OfferReplacements(opts, a, m, live);
					Offer(BandVerb.CarryOn, "Carry on without a replacement", "One fewer in the van.");
				} else Offer(BandVerb.CarryOn, "Let it go", "There's no act left to keep.");
				break;
			}

			case BandVisitKind.SubstanceRead:
				Offer(BandVerb.DryOut, $"Take {m.firstName} off the road and dry {(m.isMale ? "him" : "her")} out", "Weeks off. Momentum goes. They may refuse.", target: m.personId);
				Offer(BandVerb.LookAway, "Look the other way", "The record's selling. For now.");
				break;
		}
		foreach (BandOption opt in opts)
			if (opt.enabled && opt.hours > 0 && TimeManager.Instance?.HoursRemaining < opt.hours) {
				opt.enabled = false;
				opt.disabledReason = "Not enough hours left today.";
			}
		return opts;
	}

	private void OfferReplacements(List<BandOption> opts, SimulatedArtist a, Musician departed, BandRoomLive live) {
		if (a.lifecycleStatus != ArtistLifecycleStatus.Active) return;
		AlumniRecord stint = a.alumni?.LastOrDefault(r => r.personId == departed.personId);
		MusicianRole role = stint?.role ?? departed.primaryRole;
		bool lead = stint?.wasLeadVocalist ?? departed.isLeadVocalist;
		foreach (PooledPerson p in PoolCandidatesFor(a, role, lead, 3))
			opts.Add(new BandOption {
				verb = BandVerb.HireFromPool, targetPersonId = p.person.personId,
				label = $"Hire {p.person.FullName} ({RoleWord(p.person.primaryRole)}, ex-{p.lastStageName}, {p.person.GetAge(YearNow)})",
				subLabel = $"{SkillWord(p.person.technicalSkill)}. Free since {p.sinceYear}.{(p.person.personalRecognition > 0.02f ? " People know the name." : "")}"
			});
		opts.Add(new BandOption { verb = BandVerb.HireNewcomer, label = "Find somebody new through the musicians' union",
			subLabel = "An unknown. You won't know what you've got until the first date." });
	}

	public IEnumerable<PooledPerson> PoolCandidatesFor(SimulatedArtist a, MusicianRole role, bool lead, int count) {
		var list = new List<PooledPerson>();
		var seen = new HashSet<string>();
		for (int i = 0; i < count; i++) {
			PooledPerson p = PersonPool.Ordered().Where(x => !seen.Contains(x.person.personId))
				.Where(x => x.person.lifeState == MemberLifeState.Active && x.lastArtistId != a.artistId && SceneSourceService.CanJoinFromPool(x.person, a))
				.OrderByDescending(x => (lead ? (MemberAxesService.IsSinger(x.person) ? 3f : 0f) : (x.person.primaryRole == role ? 3f : 1f)) +
					(x.homeRegion == a.homeRegion ? 1f : 0f) + ContactNetworkService.HiringScore(x.person, a) + x.person.technicalSkill)
				.ThenBy(x => x.person.personId, StringComparer.Ordinal).FirstOrDefault();
			if (p == null) break;
			seen.Add(p.person.personId);
			list.Add(p);
		}
		return list;
	}

	private static SimulatedArtist FindSpinOut(Musician m) =>
		m == null ? null : ArtistManager.Instance?.GetAllArtists().FirstOrDefault(x => x.lifecycleStatus == ArtistLifecycleStatus.Active &&
			x.members.Count == 1 && x.members[0].personId == m.personId && x.members[0].isActive);

	private static float PayAmount(SimulatedArtist a) => a.top40Hits > 0 ? 150f : 60f;
	private static string AndThe(SimulatedArtist a) => a.stageName.StartsWith("The ", StringComparison.Ordinal) ? "the " + a.stageName[4..] : "the " + a.stageName;

	public static string RoleWord(MusicianRole r) => r switch {
		MusicianRole.LeadVocals => "singer", MusicianRole.BackingVocals => "harmony singer", MusicianRole.LeadGuitar => "lead guitar",
		MusicianRole.RhythmGuitar => "rhythm guitar", MusicianRole.Bass => "bass", MusicianRole.Drums => "drums", MusicianRole.Piano => "piano",
		MusicianRole.Organ => "organ", MusicianRole.Saxophone => "sax", MusicianRole.Trumpet => "trumpet", MusicianRole.Harmonica => "harp",
		MusicianRole.Violin => "fiddle", MusicianRole.Percussion => "percussion", _ => "plays anything"
	};

	public static string SkillWord(float skill) => skill > 0.7f ? "A real player" : skill > 0.5f ? "Solid" : skill > 0.35f ? "Can keep up" : "Rough";

	/// <summary>Choose a verb. Talking verbs go to a pushback; everything else resolves now.</summary>
	public void ChooseBandVerb(BandRoomLive live, BandOption option, out string message) {
		message = "";
		if (live == null || live.stage != CallStage.Open || option == null || !option.enabled) return;
		if (option.hours > 0) {
			if (!Require(option.hours, out message)) return;
			Spend(option.hours);
		}
		live.Say(RolodexSceneBeat.PlayerPitch, option.label, isPlayer: true);
		live.pendingVerb = option.verb;
		live.pendingTarget = option.targetPersonId;
		if (option.verb is BandVerb.Mediate or BandVerb.LeanOnContract) {
			live.Say(RolodexSceneBeat.Pushback, PushbackLine(live), speaker: live.ctx.member.FullName);
			live.stage = CallStage.Pushback;
			Changed?.Invoke();
			return;
		}
		ResolveBandVerb(live, option.verb, option.targetPersonId, success: true, out message);
	}

	private static string PushbackLine(BandRoomLive live) {
		BandRoomContext c = live.ctx;
		if (live.pendingVerb == BandVerb.LeanOnContract)
			return c.member.ego > 0.55f ? "\"You want to read me my contract? Go on, then. Read it to me.\""
				: c.member.loyalty > 0.55f ? "\"I know what I signed. I'm not trying to get out of anything.\""
				: "\"Paper's paper. It doesn't stand in the room with {other}.\"".Replace("{other}", c.OtherName);
		return c.voice switch {
			MemberVoice.Blunt => $"\"{c.OtherName} isn't going to change, and I'm not sitting in a room to be told I should.\"",
			MemberVoice.Apologetic => $"\"I don't want a scene. I just -- all right. If you think it'll help.\"",
			MemberVoice.Evasive => "\"Sure. Whenever. Maybe next week, things are a bit -- you know.\"",
			_ => $"\"I'll sit down with {c.OtherName}. I'm not promising anything comes of it.\""
		};
	}

	/// <summary>The counters to a pushback: instinct-gated, and grounded only when the fact is true -- otherwise
	/// offered as a labelled bluff the member can call.</summary>
	public List<BandOption> BandCounterOptions(BandRoomLive live) {
		var opts = new List<BandOption>();
		BandRoomContext c = live.ctx;
		ExecutiveInstinctProfile i = c.instincts;
		void Offer(CallCounter counter, ExecutiveVoice voice, string label, string sub, bool bluff = false) =>
			opts.Add(new BandOption { counter = counter, voice = voice, label = label, subLabel = sub, isBluff = bluff });
		if (i.TheFixer >= 3) {
			bool talkable = c.other != null && c.other.temperament > 0.5f && c.other.ego < 0.55f;
			Offer(CallCounter.FixerSweeten, ExecutiveVoice.Fixer, $"\"Leave {c.OtherName} to me.\"",
				talkable ? $"True: {c.OtherName} can be talked down." : $"{c.OtherName} won't be handled, and they both know it.", bluff: !talkable);
		}
		if (i.TheEar >= 3) {
			bool good = c.morale > 0f || c.top40Hits > 0;
			Offer(CallCounter.EarChorus, ExecutiveVoice.Ear, "\"These are the best records you've made. Don't walk away from that.\"",
				good ? "True: the records are working." : "They aren't. They know it better than you.", bluff: !good);
		}
		if (i.TheStreet >= 3) {
			bool knows = c.secretAffair != null || c.substanceLoad > 0.35f || (c.other?.substanceLoad ?? 0f) > 0.35f;
			Offer(CallCounter.StreetScene, ExecutiveVoice.Street, "\"I know what's really going on in this band.\"",
				knows ? "True, and they'll realise you mean it." : "There's nothing behind it.", bluff: !knows);
		}
		if (i.TheSuit >= 3) {
			bool paper = c.contractExpiresYear > c.year;
			Offer(CallCounter.SuitNumbers, ExecutiveVoice.Suit, "\"There's a contract, and it runs both ways.\"",
				paper ? $"True: it runs to {c.contractExpiresYear}." : "It's run out. They can check.", bluff: !paper);
		}
		opts.Add(new BandOption { counter = CallCounter.PressIt, label = "Press the point", subLabel = "Say it again, plainer." });
		opts.Add(new BandOption { counter = CallCounter.BackOff, label = "Let it go", subLabel = "Nothing gained, nothing burned." });
		return opts;
	}

	public void PlayBandCounter(BandRoomLive live, BandOption counter, out string message) {
		message = "";
		if (live == null || live.stage != CallStage.Pushback || counter == null) return;
		BandRoomContext c = live.ctx;
		live.Say(RolodexSceneBeat.ActiveCheckPrompt, counter.label, isPlayer: true);
		if (counter.counter == CallCounter.BackOff) {
			live.Say(RolodexSceneBeat.Exit, BandRoomFragments.Pick(RolodexSceneBeat.Exit, c) ?? "They leave it there.");
			FinishVisit(live);
			message = "You let it drop.";
			return;
		}
		string k = $"bandroom|{live.visit.visitId}|{live.pendingVerb}|counter";
		if (counter.isBluff) {
			float catchChance = Mathf.Clamp(0.30f + 0.30f * (1f - c.member.loyalty) + 0.20f * c.member.ego - 0.05f * c.instincts.TheFixer, 0.1f, 0.85f);
			if (BandLife.Unit(k + "|catch") < catchChance) {
				live.Say(RolodexSceneBeat.Pushback, "\"No. That isn't true, and you know it isn't.\"", speaker: c.member.FullName);
				ResolveBandVerb(live, live.pendingVerb, live.pendingTarget, success: false, out message, caughtBluff: true);
				return;
			}
			live.chanceModifier += 0.08f;
		} else live.chanceModifier += counter.counter == CallCounter.PressIt ? 0.04f : 0.18f;
		ResolveBandVerb(live, live.pendingVerb, live.pendingTarget, success: RollTalk(live), out message);
	}

	private bool RollTalk(BandRoomLive live) {
		BandRoomContext c = live.ctx;
		float chance = live.pendingVerb == BandVerb.LeanOnContract
			? (c.member.loyalty > 0.55f && c.member.ego < 0.45f ? 0.85f : c.member.ego > 0.55f ? 0.10f : 0.45f)
			: 0.30f + 0.08f * c.instincts.TheFixer - 0.25f * c.member.ego + 0.20f * c.trust;
		chance = Mathf.Clamp(chance + live.chanceModifier, 0.05f, 0.92f);
		return BandLife.Unit($"bandroom|{live.visit.visitId}|{live.pendingVerb}|roll") < chance;
	}

	/// <summary>Every verb's sim write, spoken aloud in the aftermath beat (§5.1: RelationshipAftermath).</summary>
	private void ResolveBandVerb(BandRoomLive live, BandVerb verb, string target, bool success, out string message, bool caughtBluff = false) {
		BandRoomContext c = live.ctx;
		SimulatedArtist a = c.artist;
		Musician m = c.member;
		Musician t = target == null ? c.other : FindPerson(a, target) ?? c.other;
		MemberRelation edge = c.edge ?? (t != null && a.members.Contains(t) && a.members.Contains(m) ? BandLifeService.Edge(a, m, t) : null);
		int year = YearNow;
		var after = new List<string>();
		message = "";

		void Relieve(MemberRelation e, float amount, StrainCause? cause = null) {
			if (e == null) return;
			float before = e.strain;
			e.strain = Mathf.Max(0f, e.strain - amount);
			if (cause.HasValue) e.causeWeight[(int)cause.Value] = Mathf.Max(0f, e.causeWeight[(int)cause.Value] - amount);
			else for (int i = 0; i < e.causeWeight.Length; i++) e.causeWeight[i] *= before <= 0f ? 1f : e.strain / before;
		}
		void OthersSpotlight(Musician favoured, float amount) {
			foreach (Musician x in a.members.Where(x => x.isActive && x != favoured))
				BandLifeService.AddStrain(BandLifeService.Edge(a, favoured, x), StrainCause.Spotlight, amount, year);
		}

		switch (verb) {
			case BandVerb.HearThemOut:
				Relieve(edge, 0.05f);
				AddTrust(m, 0.03f);
				after.Add($"{m.firstName} feels heard. That's worth something, not much.");
				break;
			case BandVerb.ConcedePact:
				if (t != null) {
					CowritingService.ConcedePact(a, m, t, year);
					Relieve(edge, 0.25f, StrainCause.CreditAndMoney);
					if (edge != null) edge.rivalry *= 0.8f;
					AddTrust(m, 0.10f);
					after.Add($"From now on it's \"{m.lastName}-{t.lastName}\" on everything either of them writes.");
				}
				break;
			case BandVerb.GiveThemASong:
				promisedCredit[a.artistId] = m.personId;
				Relieve(edge, 0.15f, StrainCause.CreditAndMoney);
				AddTrust(m, 0.08f);
				after.Add($"{m.firstName}'s name goes on the next original you cut for {a.stageName}.");
				break;
			case BandVerb.PayThem: {
				float pay = PayAmount(a);
				Label.cashReserves -= pay;
				Label.monthlyExpenses += pay;
				Relieve(edge, 0.15f, StrainCause.CreditAndMoney);
				AddTrust(m, 0.05f);
				foreach (Musician x in a.members.Where(x => x.isActive && x != m)) BandLifeService.AddStrain(BandLifeService.Edge(a, m, x), StrainCause.Spotlight, 0.04f, year);
				after.Add($"${pay:N0} changes hands. By Friday the rest of the band know the figure.");
				break;
			}
			case BandVerb.SpotlightThem: {
				string old = a.stageName;
				a.stageName = $"{m.firstName} {m.lastName} & {AndThe(a)}";
				m.personalRecognition = Mathf.Clamp(m.personalRecognition + 0.01f, 0f, 1f);
				AddTrust(m, 0.12f);
				OthersSpotlight(m, 0.10f);
				a.careerEvents.Add($"{year}: Billed as {a.stageName} (was {old})");
				after.Add($"The next pressing says \"{a.stageName}\". The rest of the band read it on the label like everyone else.");
				break;
			}
			case BandVerb.SideWith:
			case BandVerb.SideAgainst: {
				Musician favoured = verb == BandVerb.SideWith ? m : t;
				Musician other = verb == BandVerb.SideWith ? t : m;
				AddTrust(favoured, 0.10f);
				AddTrust(other, -0.10f);
				if (edge != null) BandLifeService.AddStrain(edge, StrainCause.Outsider, 0.12f, year);
				after.Add($"{favoured?.firstName} knows the label is in {(favoured?.isMale == true ? "his" : "her")} corner. {other?.firstName} knows it too.");
				break;
			}
			case BandVerb.Mediate:
				if (success) { Relieve(edge, 0.30f); AddTrust(m, 0.05f); AddTrust(t, 0.05f); after.Add("It gets loud, then it gets quiet, then they shake on it."); }
				else {
					if (edge != null) BandLifeService.AddStrain(edge, edge.DominantCause, 0.05f, year);
					AddTrust(m, caughtBluff ? -0.08f : -0.03f);
					after.Add(caughtBluff ? "You were caught out in front of both of them. It's worse than before you started." : "It ends with a door. Nothing's settled.");
				}
				break;
			case BandVerb.LeanOnContract:
				if (success) { Relieve(edge, 0.20f); after.Add($"{m.firstName} goes quiet. {(m.isMale ? "He" : "She")}'ll honour the paper, for now."); }
				else {
					if (m.ego > 0.55f && m.departureStage == DepartureStage.Brewing) {
						m.departureStage = DepartureStage.Ultimatum; m.stageEnteredYear = year;
						after.Add($"That was the wrong thing to say to {m.firstName}. Grumbling just became an ultimatum.");
					} else after.Add("Paper doesn't settle it.");
					if (edge != null) BandLifeService.AddStrain(edge, edge.DominantCause, 0.05f, year);
					AddTrust(m, -0.06f);
				}
				break;
			case BandVerb.TimeOff: {
				RoadBooking b = BookingFor(a);
				b.TimeOffUntilWeek = ChartWeekNow + 6;
				b.WeeksRemaining = 0; b.ResidencyWeeks = 0; b.TourWeeks = 0;
				a.morale = Mathf.Clamp(a.morale + 0.08f, BandLifeService.MoraleMin, BandLifeService.MoraleMax);
				if (a.relations != null) foreach (MemberRelation e in a.relations) Relieve(e, Mathf.Min(e.causeWeight[(int)StrainCause.Burnout], 0.15f), StrainCause.Burnout);
				a.momentum *= 0.85f;
				after.Add($"Six weeks off. {a.stageName} go home. The phones go quiet, and so does the buzz.");
				break;
			}
			case BandVerb.OffTheRoadYes:
				m.lifeState = MemberLifeState.StudioOnly;
				Relieve(edge, 0.20f, StrainCause.Burnout);
				AddTrust(m, 0.10f);
				after.Add($"{m.firstName} makes the records from now on. Somebody else will have to make the dates.");
				break;
			case BandVerb.DryOut: {
				Musician who = t ?? m;
				bool refuses = BandLife.Unit($"bandroom|{live.visit.visitId}|dryout") < (1f - who.reliability) * who.ego * 1.5f;
				if (refuses) { AddTrust(who, -0.05f); after.Add($"{who.firstName} won't go. \"I'm fine,\" {(who.isMale ? "he" : "she")} says. {(who.isMale ? "He" : "She")} isn't."); }
				else {
					who.substanceLoad = Mathf.Max(0f, who.substanceLoad - 0.30f);
					RoadBooking b = BookingFor(a);
					b.DryingOutPersonId = who.personId; b.DryOutUntilWeek = ChartWeekNow + 8;
					a.momentum *= 0.85f;
					AddTrust(who, 0.05f);
					after.Add($"Eight weeks somewhere quiet. {who.firstName} comes back thinner and steadier. Nobody's calling it a cure.");
				}
				break;
			}
			case BandVerb.LookAway:
				knewAndHid.Add("substance|" + m.personId);
				after.Add("You say nothing. The record's selling.");
				break;
			case BandVerb.TellAboutAffair:
				if (c.secretAffair != null) {
					BandLifeService.DiscoverAffair(a, c.secretAffair, year);
					AddTrust(t, 0.15f);
					after.Add($"You tell {t?.firstName}. It's the worst conversation you've had in this office.");
				}
				break;
			case BandVerb.KeepQuiet:
				if (c.secretAffair != null) knewAndHid.Add(AffairKey(c.secretAffair.personA, c.secretAffair.personB));
				after.Add("You keep it to yourself. For now it's just yours to carry.");
				break;
			case BandVerb.KeepApart:
				if (c.secretAffair != null && BandLife.Chance($"bandroom|{live.visit.visitId}|apart", 0.5f)) {
					a.relations?.Remove(c.secretAffair);
					after.Add("Separate cars, separate floors. By the end of the tour, whatever it was is over.");
				} else {
					Relieve(edge, 0.08f);
					after.Add("Separate cars, separate floors. It buys a little time.");
				}
				break;
			case BandVerb.AskPartnerOut:
				if (m.partner != null) m.partner.sitsInOnSessions = false;
				if (a.relations != null) foreach (MemberRelation e in a.relations.Where(e => e.Involves(m.personId))) Relieve(e, 0.08f, StrainCause.Outsider);
				AddTrust(m, -0.05f);
				after.Add($"{m.partner?.name ?? "The partner"} stays home on session days now. The room's easier. {m.firstName} isn't.");
				break;
			case BandVerb.Congratulate:
				Label.cashReserves -= 15f;
				AddTrust(m, 0.06f);
				after.Add($"{m.firstName} sends a thank-you card. It's on the desk for a month.");
				break;
			case BandVerb.Fire: {
				Musician fired = t;
				if (fired != null && a.members.Contains(fired)) {
					var allies = a.members.Where(x => x.isActive && x != fired && x != m &&
						(BandLifeService.FindEdge(a, x, fired)?.strain ?? 0f) < 0.2f && x.loyalty > 0.55f).ToList();
					BandLifeService.PlayerDepart(a, fired, DepartureKind.Fired, StrainCause.Reliability, year);
					foreach (Musician ally in allies) { AddTrust(ally, -0.10f); BandLifeService.AddStrain(BandLifeService.Edge(a, ally, m), StrainCause.Outsider, 0.10f, year); }
					after.Add($"{fired.firstName} is out. {(allies.Count > 0 ? string.Join(" and ", allies.Select(x => x.firstName)) + " won't forget whose idea it was." : "Nobody argues.")}");
				}
				break;
			}
			case BandVerb.LetThemGo:
				BandLifeService.PlayerDepart(a, m, DepartureKind.Acrimony, c.cause, year);
				after.Add($"{m.firstName} goes with a handshake instead of a lawyer. That's the most you could get.");
				break;
			case BandVerb.HireFromPool:
			case BandVerb.HireNewcomer: {
				Musician departed = m;
				AlumniRecord stint = a.alumni?.LastOrDefault(r => r.personId == departed.personId);
				Musician hire = BandLifeService.PlayerHire(a, stint?.role ?? departed.primaryRole, stint?.wasLeadVocalist ?? departed.isLeadVocalist,
					stint?.wasWriter ?? false, verb == BandVerb.HireFromPool ? target : null, departed, year);
				if (hire != null) after.Add($"{hire.FullName} is in. {SkillWord(hire.technicalSkill)}. The honeymoon starts now.");
				break;
			}
			case BandVerb.CarryOn:
			case BandVerb.PutOnIce:
				if (verb == BandVerb.PutOnIce) {
					RoadBooking b = BookingFor(a);
					b.TimeOffUntilWeek = ChartWeekNow + 52; b.WeeksRemaining = 0;
				}
				after.Add(verb == BandVerb.PutOnIce ? $"{a.stageName} goes quiet until he's home." : "They carry on as they are.");
				break;
			case BandVerb.ExerciseOption: {
				SimulatedArtist solo = FindSpinOut(m);
				string why = null;
				if (solo != null && SignLeavingMember(solo, out why)) after.Add($"{m.FullName} is your act now, under {(m.isMale ? "his" : "her")} own name.");
				else after.Add(why ?? "It didn't come off.");
				break;
			}
			case BandVerb.ReleaseMasters:
				foreach (Musician x in a.members.Where(x => x.isActive)) AddTrust(x, -0.04f);
				a.morale = Mathf.Clamp(a.morale - 0.03f, BandLifeService.MoraleMin, BandLifeService.MoraleMax);
				after.Add("The masters are yours to schedule. Some of the papers will call it a tribute. Some won't.");
				break;
			case BandVerb.HoldMasters:
				foreach (Musician x in a.members.Where(x => x.isActive)) AddTrust(x, 0.05f);
				after.Add("You hold them. The band notice.");
				break;
			case BandVerb.SoloSingleYes:
				m.personalRecognition = Mathf.Clamp(m.personalRecognition + 0.008f, 0f, 1f);
				AddTrust(m, 0.12f);
				OthersSpotlight(m, 0.06f);
				after.Add($"{m.firstName} gets {(m.isMale ? "his" : "her")} side. The others get a reason to watch {(m.isMale ? "him" : "her")}.");
				break;
			case BandVerb.AdvanceYes:
				Label.cashReserves -= 100f;
				a.unrecoupedAdvance += 100f;
				AddTrust(m, 0.10f);
				after.Add($"$100 against royalties. {m.firstName} won't forget the week you said yes.");
				break;
			case BandVerb.Decline: {
				AddTrust(m, -0.07f);
				MemberRelation worst = BandLifeService.WorstEdgeOf(a, m);
				StrainCause cause = live.visit.request switch { BandRequest.OffTheRoad => StrainCause.Burnout, BandRequest.SoloSingle => StrainCause.Spotlight, _ => StrainCause.CreditAndMoney };
				if (worst != null) BandLifeService.AddStrain(worst, cause, 0.05f, year);
				after.Add("\"Not now\" is a phrase people remember.");
				break;
			}
		}

		foreach (string line in after) live.Say(RolodexSceneBeat.RelationshipAftermath, line);
		if (verb is not (BandVerb.HireFromPool or BandVerb.HireNewcomer or BandVerb.CarryOn or BandVerb.ReleaseMasters or BandVerb.HoldMasters
			or BandVerb.Congratulate or BandVerb.ExerciseOption or BandVerb.LetThemGo or BandVerb.Fire or BandVerb.PutOnIce))
			live.Say(RolodexSceneBeat.Exit, BandRoomFragments.Pick(RolodexSceneBeat.Exit, c, verb.ToString()));
		Note($"BAND ROOM — {a.stageName}: {after.FirstOrDefault() ?? verb.ToString()}");
		FinishVisit(live);
		message = after.FirstOrDefault() ?? "";
	}

	private void FinishVisit(BandRoomLive live) {
		live.stage = CallStage.Resolved;
		live.visit.resolved = true;
		Changed?.Invoke();
	}

	// ==================================================================================================
	// ACT-LEVEL ACTIONS (the roster card)
	// ==================================================================================================

	private RoadBooking BookingFor(SimulatedArtist a) {
		int year = YearNow;
		if (!roadBookings.TryGetValue(a.artistId, out RoadBooking b) || b.Year != year) {
			b = new RoadBooking { ArtistId = a.artistId, Year = year };
			roadBookings[a.artistId] = b;
		}
		return b;
	}

	public RoadBooking RoadBookingFor(string artistId) =>
		roadBookings.TryGetValue(artistId, out RoadBooking b) && b.Year == YearNow ? b : null;

	/// <summary>
	/// Book a residency (a club, most nights, the Hamburg lever: the most work for the hours) or a tour (more
	/// money, more miles, more hazards). Sets the act's road load for the annual pass: effective hours, road
	/// years, morale wear, the road hazards and the affairs (§5.4).
	/// </summary>
	public bool BookRoad(SimulatedArtist a, int residencyWeeks, int tourWeeks, out string message) {
		message = "";
		if (a == null || a.labelId != Label?.labelId) { message = "That act isn't on your roster."; return false; }
		if (SceneEcosystemService.Moves.Any(m => m.ArtistId == a.artistId && m.Status == SceneMoveStatus.Planned)) {
			message = "The act already has a relocation arranged."; return false;
		}
		if (!Require(1, out message)) return false;
		Spend(1);
		RoadBooking b = BookingFor(a);
		b.ResidencyWeeks = Mathf.Clamp(b.ResidencyWeeks + residencyWeeks, 0, 40);
		b.TourWeeks = Mathf.Clamp(b.TourWeeks + tourWeeks, 0, 40);
		b.WeeksRemaining = Mathf.Clamp(b.WeeksRemaining + residencyWeeks + tourWeeks, 0, 48);
		b.TimeOffUntilWeek = -1;
		Note($"Booked {a.stageName}: {(residencyWeeks > 0 ? $"{residencyWeeks} weeks' residency" : "")}{(residencyWeeks > 0 && tourWeeks > 0 ? " and " : "")}{(tourWeeks > 0 ? $"{tourWeeks} weeks on the road" : "")}.");
		message = $"{a.stageName} are booked.";
		Changed?.Invoke();
		return true;
	}

	/// <summary>The road load BandLifeService reads for a player act: what the player actually booked.</summary>
	private float PlayerRoadLoadFor(SimulatedArtist a) {
		if (a == null || Label == null || a.labelId != Label.labelId) return -1f;
		if (!roadBookings.TryGetValue(a.artistId, out RoadBooking b) || b.Year != BandLifeService.LastAnnualYear + 1) return 0.10f;
		float load = 0.10f + 0.015f * b.ResidencyWeeks + 0.022f * b.TourWeeks;
		if (b.TimeOffUntilWeek >= 0) load *= 0.6f;
		return Mathf.Clamp(load, 0f, 0.95f);
	}

	private void RunRoadBookings(GameDate date, int week) {
		foreach (RoadBooking b in roadBookings.Values) {
			if (b.WeeksRemaining <= 0 || b.TimeOffUntilWeek >= week) continue;
			SimulatedArtist a = ArtistManager.Instance?.GetArtist(b.ArtistId);
			if (a == null || a.labelId != Label.labelId) continue;
			b.WeeksRemaining--;
			bool residency = b.ResidencyWeeks >= b.TourWeeks;
			float pay = residency ? ResidencyWeeklyPay : TourWeeklyNet;
			Label.cashReserves += pay;
			Label.monthlyRevenue += pay;
			// Live work builds a little local name and momentum; growth itself is Phase 5's.
			a.momentum = Mathf.Clamp(a.momentum + (residency ? 0.002f : 0.004f), 0f, 1f);
		}
	}

	/// <summary>Hire a session player for the act's next date in the studio, in place of one member (§5.4). The
	/// record's execution rises; the replaced member is humiliated and the band earns no practice from it.</summary>
	public bool BookSessionPlayer(SimulatedArtist a, Musician replaced, out string message) {
		message = "";
		if (a == null || replaced == null || a.labelId != Label?.labelId) { message = "That act isn't on your roster."; return false; }
		string key = $"session|{a.artistId}|{replaced.personId}|{ChartWeekNow}";
		PooledPerson pooled = PersonPool.Ordered().Where(p => p.person.lifeState == MemberLifeState.Active && p.person.sightReading >= 0.55f)
			.OrderByDescending(p => p.person.sightReading + p.person.technicalSkill).ThenBy(p => p.person.personId, StringComparer.Ordinal).FirstOrDefault();
		float skill = pooled?.person.technicalSkill ?? Mathf.Clamp(0.62f + 0.12f * BandLife.Normal(key + "|skill"), 0.35f, 0.95f);
		float reading = pooled?.person.sightReading ?? Mathf.Clamp(0.70f + 0.10f * BandLife.Normal(key + "|read"), 0.4f, 1f);
		string name = pooled?.person.FullName ?? string.Join(" ", BandLifeNames.Person(true, key).First, BandLifeNames.Person(true, key).Last);
		float fee = Mathf.Round(15f + 60f * reading);
		if (Label.cashReserves < fee) { message = $"A session man who reads like that costs ${fee:N0}."; return false; }
		sessionPlayers[a.artistId] = new SessionPlayerBooking { ArtistId = a.artistId, ReplacesPersonId = replaced.personId, Name = name, Skill = skill, SightReading = reading, Fee = fee };
		message = $"{name} will sit in for {replaced.firstName} at the next session (${fee:N0}, paid at the date).";
		Note($"Booked session man {name} in place of {replaced.firstName} ({a.stageName}).");
		Changed?.Invoke();
		return true;
	}

	public SessionPlayerBooking SessionPlayerFor(string artistId) => artistId != null && sessionPlayers.TryGetValue(artistId, out var b) ? b : null;

	/// <summary>Consumed by StartSession: the production lift the hired player brings, and the humiliation.</summary>
	private float ConsumeSessionPlayer(SimulatedArtist a) {
		if (a == null || !sessionPlayers.Remove(a.artistId, out SessionPlayerBooking b)) return 0f;
		Musician replaced = a.members.FirstOrDefault(m => m.personId == b.ReplacesPersonId);
		Label.cashReserves -= b.Fee;
		Label.monthlyExpenses += b.Fee;
		float lift = Mathf.Clamp(0.10f * (b.Skill - (replaced?.technicalSkill ?? 0.45f)) + 0.04f * b.SightReading, 0f, 0.10f);
		if (replaced != null) {
			int year = YearNow;
			foreach (Musician x in a.members.Where(x => x.isActive && x != replaced))
				BandLifeService.AddStrain(BandLifeService.Edge(a, replaced, x), StrainCause.Outsider, 0.12f, year);
			a.morale = Mathf.Clamp(a.morale - 0.05f, BandLifeService.MoraleMin, BandLifeService.MoraleMax);
			AddTrust(replaced, -0.10f);
			Note($"{b.Name} played {replaced.firstName}'s part. {replaced.firstName} sat in the control room and watched.");
		}
		return lift;
	}

	/// <summary>
	/// Put two writers in a room (§5.4): two members, or a member and a staff writer. A song credited to the team
	/// arrives after the commission delay. Complementary writers make the better song; two big egos make heat.
	/// Repeated sessions are what make a pair eligible for a pact (the co-credits accrue when the song is cut).
	/// </summary>
	public bool BookWritingSession(SimulatedArtist a, Musician first, Musician second, bool withStaffWriter, out string message) {
		message = "";
		if (a == null || first == null || a.labelId != Label?.labelId) { message = "That act isn't on your roster."; return false; }
		if (!RequireHome(out message)) return false;
		if (!withStaffWriter && (second == null || second == first)) { message = "Pick two different writers."; return false; }
		float fee = withStaffWriter ? CommissionFee : 0f;
		if (Label.cashReserves < fee) { message = $"A staff writer's fee is ${fee:N0}."; return false; }
		if (!Require(WritingSessionHours, out message)) return false;
		Spend(WritingSessionHours);
		Label.cashReserves -= fee;
		Label.monthlyExpenses += fee;
		int year = YearNow;
		string key = $"writing|{a.artistId}|{first.personId}|{second?.personId ?? "staff"}|{ChartWeekNow}";
		float Melody(Musician m) => m.creativity * m.musicalVersatility;
		float Lyric(Musician m) => m.creativity * (m.axesVersion > 0 ? m.diction : 0.5f);
		float HookCraft(Musician m) => m.creativity * m.stagePresence;
		float staff = 0.55f;
		float melody = Mathf.Max(Melody(first), second != null ? Melody(second) : staff);
		float lyric = Mathf.Max(Lyric(first), second != null ? Lyric(second) : staff);
		float hook = Mathf.Max(HookCraft(first), second != null ? HookCraft(second) : staff);
		// Two of the same kind barely beat one; complementary specialists are the strongest pairing.
		float friction = second != null && first.ego > 0.55f && second.ego > 0.55f ? 0.05f : 0f;
		float craft = (melody + lyric + hook) / 3f;
		var pending = new WritingSessionPending {
			ArtistId = a.artistId, StaffWriterName = withStaffWriter ? "a staff writer" : null,
			ReadyDate = (TimeManager.Instance?.CurrentDate ?? GameDate.StartDate).AddDays(CommissionDeliveryDays),
			Hook = Mathf.Clamp(0.30f + 0.80f * craft - friction + 0.08f * BandLife.Normal(key + "|hook"), 0f, 1f),
			Originality = Mathf.Clamp(0.30f + 0.60f * Mathf.Max(first.creativity, second?.creativity ?? 0.5f) + 0.08f * BandLife.Normal(key + "|orig"), 0f, 1f),
		};
		pending.TeamPersonIds.Add(first.personId);
		if (second != null) pending.TeamPersonIds.Add(second.personId);
		writingSessions.Add(pending);
		if (second != null) {
			MemberRelation e = BandLifeService.Edge(a, first, second);
			if (first.ego > 0.55f && second.ego > 0.55f) {
				e.rivalry = Mathf.Clamp(e.rivalry + 0.10f, 0f, 1f);
				BandLifeService.AddStrain(e, StrainCause.Direction, 0.05f, year);
			} else e.rivalry = Mathf.Clamp(e.rivalry + 0.04f, 0f, 1f);
		}
		message = $"{first.firstName}{(second != null ? " and " + second.firstName : " and a staff writer")} are writing. Something in about {CommissionDeliveryDays} days.";
		Note($"Writing session for {a.stageName}: {message}");
		Changed?.Invoke();
		return true;
	}

	/// <summary>Daily: a finished writing session lands in the act's songbook, credited to its team.</summary>
	private void ProcessWritingSessions(GameDate date) {
		for (int i = writingSessions.Count - 1; i >= 0; i--) {
			WritingSessionPending w = writingSessions[i];
			if (date < w.ReadyDate) continue;
			writingSessions.RemoveAt(i);
			SimulatedArtist a = ArtistManager.Instance?.GetArtist(w.ArtistId);
			if (a == null) continue;
			var song = new Song {
				SongId = $"song_{++counter}", ArtistId = a.artistId, Genre = a.primaryGenre, Written = date,
				Hook = w.Hook, Originality = w.Originality, Danceability = Mathf.Clamp(0.45f + 0.2f * BandLife.Normal($"dance|{counter}"), 0.2f, 0.95f),
				TeamPersonIds = w.TeamPersonIds.ToList(), WithStaffWriter = w.StaffWriterName != null
			};
			song.Title = NameGenerator.Instance?.GenerateSongTitle(song.Genre, date.year, a.artistId) ?? $"Untitled {counter}";
			songs.Add(song);
			Note($"{a.stageName} have a new one from the writing session: \"{song.Title}\".");
		}
	}

	/// <summary>The credits for a player original about to be cut: a writing session's team, a promised member,
	/// and any label cut-in. Read by CowritingService when it credits the song.</summary>
	private CowritingService.TeamOverride TeamOverrideFor(SimulatedArtist a, string songKey) {
		if (a == null || Label == null || a.labelId != Label.labelId) return null;
		var o = new CowritingService.TeamOverride();
		if (pendingTeamForCut != null && pendingTeamForCut.Count > 0) o.PersonIds.AddRange(pendingTeamForCut);
		else if (promisedCredit.TryGetValue(a.artistId, out string promised)) { o.PersonIds.Add(promised); o.PromiseConsumed = true; }
		if (labelCutInSongsLeft.TryGetValue(a.artistId, out int left) && left > 0) { o.LabelCutInShare = LabelCutInShare; o.LabelName = Label.labelName; o.LabelId = Label.labelId; }
		if (o.PersonIds.Count == 0 && o.LabelCutInShare <= 0f) return null;
		o.OnApplied = () => {
			if (o.PromiseConsumed) promisedCredit.Remove(a.artistId);
			if (o.LabelCutInShare > 0f) {
				labelCutInSongsLeft[a.artistId] = left - 1;
				foreach (Musician m in a.members.Where(m => m.isActive && o.CreditedPersonIds.Contains(m.personId))) {
					AddTrust(m, -0.08f);
					MemberRelation worst = BandLifeService.WorstEdgeOf(a, m);
					if (worst != null) BandLifeService.AddStrain(worst, StrainCause.Outsider, 0.06f, YearNow);
					BandLifeService.AddStrain(worst ?? BandLifeService.Edge(a, m, a.members.First(x => x != m && x.isActive)), StrainCause.CreditAndMoney, 0.06f, YearNow);
				}
			}
		};
		return o;
	}

	/// <summary>Set around a single PrintMaster so a writing-session song carries its own team.</summary>
	private List<string> pendingTeamForCut;

	/// <summary>
	/// Cut the label in on the act's next few originals (§4.14 layer 4): the label takes a share of the writer
	/// credit -- money now, through the mechanical royalty -- and the writers remember. The first strain the
	/// label, not a bandmate, causes.
	/// </summary>
	public bool CutInOnSongs(SimulatedArtist a, out string message) {
		message = "";
		if (a == null || a.labelId != Label?.labelId) { message = "That act isn't on your roster."; return false; }
		labelCutInSongsLeft[a.artistId] = LabelCutInSongs;
		message = $"{Label.labelName} takes {LabelCutInShare:P0} of the writer credit on {a.stageName}'s next {LabelCutInSongs} originals.";
		Note(message);
		Changed?.Invoke();
		return true;
	}

	public int CutInSongsLeft(string artistId) => artistId != null && labelCutInSongsLeft.TryGetValue(artistId, out int n) ? n : 0;
	public string PromisedCreditFor(string artistId) => artistId != null && promisedCredit.TryGetValue(artistId, out string p) ? p : null;

	/// <summary>Sign a departing member's solo act under the leaving-member clause: the paper already says so,
	/// so it's the baseline terms with no negotiation and the label's usual advance.</summary>
	private bool SignLeavingMember(SimulatedArtist solo, out string message) {
		message = null;
		if (solo == null || !string.IsNullOrEmpty(solo.labelId)) { message = "Somebody else has already signed them."; return false; }
		ContractTermSheet sheet = Label.GenerateTermSheet(solo, YearNow);
		if (Label.cashReserves < sheet.Advance) { message = $"The option means paying the advance -- ${sheet.Advance:N0} you don't have."; return false; }
		Label.SignArtist(solo, YearNow, sheet);
		Label.cashReserves -= sheet.Advance;
		Label.monthlyExpenses += sheet.Advance;
		ArtistManager.Instance?.SignArtist(solo, Label.labelId, YearNow);
		Note($"Exercised the leaving-member option: {solo.stageName} signed (${sheet.Advance:N0}).");
		return true;
	}

	// ==================================================================================================
	// MEMBER CARDS (§5.5)
	// ==================================================================================================

	public sealed class MemberCard {
		public Musician Member;
		public string Line;          // name, age, role
		public string Personality;   // an interpreted read, never a number
		public string NotSpeakingTo;
		public string Partner;
		public string Stage;
		public string Life;
		public float Trust;
	}

	public List<MemberCard> MemberCards(SimulatedArtist a) {
		var cards = new List<MemberCard>();
		if (a == null) return cards;
		int year = YearNow;
		ExecutiveInstinctProfile i = InstinctProfile;
		foreach (Musician m in a.members.Where(x => x.isActive)) {
			var card = new MemberCard { Member = m, Trust = TrustOf(m.personId) };
			card.Line = $"{m.FullName}, {m.GetAge(year)} -- {RoleWord(m.primaryRole)}{(m.isLeadVocalist && m.primaryRole != MusicianRole.LeadVocals ? " and lead voice" : "")}" +
				$"{(m.isPrimaryWriter ? ", writes" : "")}{(m.isBandLeader ? ", leads the band" : "")}";
			int reads = Math.Max(i.TheEar, Math.Max(i.TheStreet, i.TheFixer));
			card.Personality = reads >= 4 ? DeepRead(m) : reads >= 2 ? ClearRead(m) : "You don't know them well enough yet to say.";
			MemberRelation worst = BandLifeService.WorstEdgeOf(a, m);
			if (worst != null && worst.strain >= 0.30f) {
				Musician o = a.members.FirstOrDefault(x => x.personId == worst.Other(m.personId));
				if (o != null) card.NotSpeakingTo = $"{(worst.strain >= 0.6f ? "Not speaking to" : "At odds with")} {o.firstName} -- {CauseWord(worst.DominantCause)}.";
			}
			if (m.partner != null && m.partner.state is PartnerState.Married or PartnerState.Seeing)
				card.Partner = m.partner.personId != null ? $"Seeing {m.partner.name}." : $"{(m.partner.state == PartnerState.Married ? "Married to" : "Seeing")} {m.partner.name}{(m.hasChildren ? ", with kids" : "")}.";
			if (i.TheStreet >= 3 && a.relations?.Any(e => e.secret && e.Involves(m.personId)) == true) card.Partner = (card.Partner ?? "") + " The Street says there's more to it.";
			card.Stage = m.departureStage switch {
				DepartureStage.Ultimatum => "Has given you an ultimatum.",
				DepartureStage.Brewing => "Grumbling.",
				_ => "Content."
			};
			var life = new List<string>();
			if (m.lifeState == MemberLifeState.StudioOnly) life.Add("records only, off the road");
			if (m.lifeState == MemberLifeState.Drafted) life.Add($"in the service until {m.lifeStateUntilYear}");
			if (m.lifeState == MemberLifeState.Jailed) life.Add($"in jail until {m.lifeStateUntilYear}");
			if (m.lifeState == MemberLifeState.Injured) life.Add($"laid up until {m.lifeStateUntilYear}");
			if (m.roadYears >= 3f) life.Add($"{Mathf.RoundToInt(m.roadYears)} hard years on the road");
			if (i.TheStreet >= 2 && m.substanceLoad > 0.35f) life.Add(m.substanceLoad > 0.55f ? "using, and it shows" : "using");
			card.Life = life.Count == 0 ? null : string.Join("; ", life) + ".";
			cards.Add(card);
		}
		return cards;
	}

	private static string ClearRead(Musician m) =>
		m.ego > 0.6f ? "Wants it to be about them." : m.loyalty > 0.65f ? "Loyal. Would take a bullet for the band." :
		m.temperament < 0.35f ? "Short fuse." : m.reliability < 0.45f ? "Hard to pin down." : "Easy to work with.";

	private static string DeepRead(Musician m) {
		var parts = new List<string>();
		if (m.ego > 0.6f) parts.Add("wants the spotlight");
		else if (m.ego < 0.3f) parts.Add("happy at the back");
		if (m.ambition > 0.65f) parts.Add("hungry");
		if (m.loyalty > 0.65f) parts.Add("loyal to the band");
		else if (m.loyalty < 0.35f) parts.Add("loyal to themselves");
		if (m.temperament < 0.35f) parts.Add("short fuse");
		if (m.reliability < 0.45f) parts.Add("late, and later");
		if (m.creativity > 0.65f) parts.Add("full of songs");
		return parts.Count == 0 ? "Steady. Nothing you'd notice in a room." : char.ToUpperInvariant(parts[0][0]) + string.Join(", ", parts)[1..] + ".";
	}

	// ==================================================================================================
	// SAVE / LOAD
	// ==================================================================================================

	private BandRoomSaveData CaptureBandRoom() => new() {
		Visits = bandVisits.Where(v => !v.resolved).ToList(),
		Trust = new Dictionary<string, float>(memberTrust),
		Bookings = roadBookings.Values.ToList(),
		PromisedCredit = new Dictionary<string, string>(promisedCredit),
		CutInSongsLeft = new Dictionary<string, int>(labelCutInSongsLeft),
		KnewAndHid = knewAndHid.ToList(),
		RequestCooldown = new Dictionary<string, int>(requestCooldownWeek),
		SessionPlayers = sessionPlayers.Values.ToList(),
		WritingSessions = writingSessions.Select(w => new WritingSessionSaveData {
			ArtistId = w.ArtistId, TeamPersonIds = w.TeamPersonIds.ToList(), StaffWriterName = w.StaffWriterName,
			ReadyYear = w.ReadyDate.year, ReadyMonth = w.ReadyDate.month, ReadyDay = w.ReadyDate.day, Hook = w.Hook, Originality = w.Originality
		}).ToList(),
		VisitCounter = visitCounter,
	};

	private void RestoreBandRoom(BandRoomSaveData s) {
		bandVisits.Clear(); memberTrust.Clear(); roadBookings.Clear(); promisedCredit.Clear(); labelCutInSongsLeft.Clear();
		knewAndHid.Clear(); requestCooldownWeek.Clear(); sessionPlayers.Clear(); writingSessions.Clear();
		ActiveVisit = null;
		// A v3 save's acts start counting this year's evidence from now, not from 1960.
		BandLifeService.BaselineSnapshots(Roster);
		if (s == null) return;
		bandVisits.AddRange(s.Visits ?? new List<BandVisit>());
		foreach (var kv in s.Trust ?? new Dictionary<string, float>()) memberTrust[kv.Key] = kv.Value;
		foreach (RoadBooking b in s.Bookings ?? new List<RoadBooking>()) if (b?.ArtistId != null) roadBookings[b.ArtistId] = b;
		foreach (var kv in s.PromisedCredit ?? new Dictionary<string, string>()) promisedCredit[kv.Key] = kv.Value;
		foreach (var kv in s.CutInSongsLeft ?? new Dictionary<string, int>()) labelCutInSongsLeft[kv.Key] = kv.Value;
		foreach (string k in s.KnewAndHid ?? new List<string>()) knewAndHid.Add(k);
		foreach (var kv in s.RequestCooldown ?? new Dictionary<string, int>()) requestCooldownWeek[kv.Key] = kv.Value;
		foreach (SessionPlayerBooking b in s.SessionPlayers ?? new List<SessionPlayerBooking>()) if (b?.ArtistId != null) sessionPlayers[b.ArtistId] = b;
		foreach (WritingSessionSaveData w in s.WritingSessions ?? new List<WritingSessionSaveData>())
			writingSessions.Add(new WritingSessionPending { ArtistId = w.ArtistId, TeamPersonIds = w.TeamPersonIds ?? new List<string>(),
				StaffWriterName = w.StaffWriterName, ReadyDate = new GameDate(w.ReadyYear, w.ReadyMonth, w.ReadyDay), Hook = w.Hook, Originality = w.Originality });
		visitCounter = s.VisitCounter;
	}
}

/// <summary>The Band Room's player-layer save (save v4).</summary>
public sealed class BandRoomSaveData {
	public List<BandVisit> Visits { get; set; } = new();
	public Dictionary<string, float> Trust { get; set; } = new();
	public List<PlayerDesk.RoadBooking> Bookings { get; set; } = new();
	public Dictionary<string, string> PromisedCredit { get; set; } = new();
	public Dictionary<string, int> CutInSongsLeft { get; set; } = new();
	public List<string> KnewAndHid { get; set; } = new();
	public Dictionary<string, int> RequestCooldown { get; set; } = new();
	public List<PlayerDesk.SessionPlayerBooking> SessionPlayers { get; set; } = new();
	public List<WritingSessionSaveData> WritingSessions { get; set; } = new();
	public int VisitCounter { get; set; }
}

public sealed class WritingSessionSaveData {
	public string ArtistId { get; set; }
	public List<string> TeamPersonIds { get; set; } = new();
	public string StaffWriterName { get; set; }
	public int ReadyYear { get; set; }
	public int ReadyMonth { get; set; }
	public int ReadyDay { get; set; }
	public float Hook { get; set; }
	public float Originality { get; set; }
}
