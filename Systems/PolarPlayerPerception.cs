using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Godot;

/// <summary>Only this boundary sees truth. Everything returned for display is estimated evidence.
/// Recording proposals are pure; reading/previewing never registers material or allocates IDs.</summary>
public static class PolarPlayerPerception {
	private static Dictionary<string, PolarObservation> observations = new(StringComparer.Ordinal);
	private static readonly Lazy<PolarPerceptionTable> config = new(() => {
		using var file = Godot.FileAccess.Open("res://Data/PolarPerceptionTable.json", Godot.FileAccess.ModeFlags.Read);
		return JsonSerializer.Deserialize<PolarPerceptionTable>(file.GetAsText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
	});
	public static PolarPerceptionTable Config => config.Value;
	public static void Reset() => observations.Clear();
	public static List<PolarObservation> Capture() => observations.Values.OrderBy(o => o.key, StringComparer.Ordinal).ToList();
	public static void Restore(IEnumerable<PolarObservation> saved) => observations = (saved ?? Array.Empty<PolarObservation>())
		.Where(o => o?.key != null && o.axes?.Length == SongProfile.AxisCount && o.perceptionVersion == Config.Version)
		.Take(Config.MaximumObservations).GroupBy(o => o.key).ToDictionary(g => g.Key, g => g.Last(), StringComparer.Ordinal);
	public static float Standing(SimulatedArtist artist) => Config.Standing.GetValueOrDefault(artist.careerState.ToString(), .3f);

	public static SongComposition Subject(PlayerDesk.MaterialChoice choice, SimulatedArtist artist) {
		var song = CompositionCatalogService.GetSong(choice.SongId);
		if (song != null) return song;
		// Explicit provisional demo for player originals without a committed composition; never registered.
		return new SongComposition { songId = "player-demo:" + artist.artistId + ":" + (choice.WrittenSong?.SongId ?? choice.Title),
			title = choice.Title, primaryGenre = artist.primaryGenre, secondaryGenre = artist.secondaryGenre,
			demoTaxonomy = new SongTaxonomy { primaryGenre = artist.primaryGenre, secondaryGenre = artist.secondaryGenre } };
	}
	public static PolarArrangementProposal Proposal(PlayerDesk.MaterialChoice choice, SimulatedArtist artist, string plannedId, PolarSessionContext session = null) {
		var song = Subject(choice, artist);
		var reference = choice.Kind == PlayerDesk.MaterialKind.LiveCover ? PolarSongBehavior.Reference(song, TimeManager.Instance.CurrentDate.year, choice.ReferenceMasterId) : null;
		return PolarCoverResolver.Propose(song, reference, PolarActProfileDeriver.Derive(artist, PlayerDesk.Instance.Label, session, PolarSongTable.Current),
			artist.primaryGenre, TimeManager.Instance.CurrentDate.year, ChartManager.Instance.GetCurrentChartWeek() + 1,
			plannedId, PolarSongBehavior.CaptureTaste().GetValueOrDefault(artist.primaryGenre), PolarSongTable.Current);
	}

	public static PolarComparisonRead Compare(PlayerDesk.MaterialChoice choice, SimulatedArtist artist, PolarEvidenceGate gate,
		string eventId, string plannedId, PolarSessionContext session = null, string printedMasterId = null) {
		var label = PlayerDesk.Instance.Label;
		PolarArrangementProposal proposal;
		if (printedMasterId == null) proposal = Proposal(choice, artist, plannedId, session);
		else {
			var master = PolarSongMetadataService.Get(printedMasterId) ?? throw new ArgumentException("Unknown printed master");
			var song = CompositionCatalogService.GetSong(master.songId);
			var parent = PolarSongMetadataService.Get(master.parentRecordingId);
			proposal = new PolarArrangementProposal { songId = master.songId, parentRecordingId = master.parentRecordingId,
				referenceProfile = SongProfileDeriver.Derive(song, parent?.taxonomy ?? song.demoTaxonomy, PolarSongTable.Current),
				realizedProfile = SongProfileDeriver.Derive(song, master.taxonomy, PolarSongTable.Current) };
		}
		var actor = PolarActProfileDeriver.Derive(artist, label, session, PolarSongTable.Current);
		var act = Observe(actor.axes, actor.interpretiveReach, actor.identityRigidity, artist.evolution?.artisticAmbition ?? .5f,
			0, label, "act:" + artist.artistId, "performer", eventId, gate);
		string subject = proposal.parentRecordingId ?? "demo:" + proposal.songId;
		var reference = Observe(proposal.referenceProfile.axes, 0, 0, 0, proposal.referenceProfile.plasticity, label, subject, "heard-material", eventId, gate);
		var proposed = Observe(proposal.realizedProfile.axes, 0, 0, 0, proposal.realizedProfile.plasticity, label,
			printedMasterId == null ? "arrangement:" + artist.artistId + ":" + subject + ":" + plannedId : "master:" + printedMasterId,
			printedMasterId == null ? "prospective-arrangement" : "printed-performance", eventId, gate);
		var taste = PolarSongBehavior.CaptureTaste().GetValueOrDefault(artist.primaryGenre);
		bool hasMarket = taste != null && taste.observations > 0 && taste.asOfWeek <= ChartManager.Instance.GetCurrentChartWeek();
		PolarBand[] market = null;
		if (hasMarket) {
			var marketAxes = new float[SongProfile.AxisCount];
			Array.Copy(taste.identity, 0, marketAxes, SongProfile.DemandCount, SongProfile.IdentityCount);
			var heardMarket = Observe(marketAxes, 0, 0, 0, 0, label, "market:" + artist.primaryGenre, "market", "completed-week:" + taste.asOfWeek, PolarEvidenceGate.FirstListen);
			market = heardMarket.axes.Skip(SongProfile.DemandCount).ToArray();
		}
		var before = FitBands(reference.axes, act.axes, act.rigidity, market);
		var after = FitBands(proposed.axes, act.axes, act.rigidity, market);
		var stretch = IdentityDifference(reference.axes, proposed.axes, squared: false);
		float standing = Standing(artist);
		float softening = PlayerDesk.Instance.RepertoireFor(artist.artistId).Count == 0 && !PlayerDesk.Instance.SongsFor(artist.artistId).Any()
			? PolarSongTable.Current.N("emptySongbookSoftening") : 1;
		float threshold = (PolarSongTable.Current.N("refusalBase") + PolarSongTable.Current.N("refusalAmbition") * act.ambition.hi * standing) * softening;
		bool resistance = after[1].lo < threshold && stretch.hi >= after[0].lo * PolarSongTable.Current.N("refusalStretchCapability");
		int weakest = Enumerable.Range(0, SongProfile.DemandCount).OrderByDescending(i => proposed.axes[i].Center - act.axes[i].Center).First();
		float gap = proposed.axes[weakest].Center - act.axes[weakest].Center;
		string[] demands = { "vocal power", "vocal nuance", "instrumental skill", "ensemble precision", "lyric delivery", "studio execution" };
		string explanation = gap > 0 ? $"The arrangement may stretch their {demands[weakest]}." : "The heard arrangement seems within their playing range.";
		int identityGap = Enumerable.Range(SongProfile.DemandCount, SongProfile.IdentityCount).OrderByDescending(i => Math.Abs(proposed.axes[i].Center - act.axes[i].Center)).First();
		string[] mismatch = { "Its edge may sit outside the act's style.", "Its polish may sit outside the act's style.", "Its emotional tone may feel unfamiliar to them.", "Its outlook may sit outside the act's style." };
		string resistanceText = resistance ? "They may resist this material. " + mismatch[identityGap - SongProfile.DemandCount] : "Resistance is possible; no strong sign in this read.";
		float pull = proposed.axes[(int)SongAxis.Toughness].Center - reference.axes[(int)SongAxis.Toughness].Center;
		return new PolarComparisonRead { songTitle = choice.Title, actName = artist.stageName, reference = reference, proposed = proposed, act = act,
			referenceFit = before, proposedFit = after, hasMarketEvidence = hasMarket, mayResist = resistance, isRecorded = printedMasterId != null,
			subjectLabel = proposal.parentRecordingId == null ? "Demo / inferred reference" : "Heard reference performance",
			evidenceLabel = gate.ToString() == "FirstListen" ? "First listen · broad read" : gate + " · observed read",
			explanation = explanation, resistance = printedMasterId == null ? resistanceText : "Playback read · use it when choosing their next material.",
			arrangement = printedMasterId != null ? "Playback: the printed version." : Math.Abs(pull) < Config.ArrangementDescriptionThreshold ? "Preview: a reading close to the heard version." : pull > 0 ? "Preview: a harder-edged reading." : "Preview: a gentler reading." };
	}

	public static PolarObservation Observe(float[] axes, float reach, float rigidity, float ambition, float plasticity,
		AILabel observer, string subject, string kind, string eventId, PolarEvidenceGate gate) {
		string version = Hash(string.Join("|", axes.Concat(new[] { reach, rigidity, ambition, plasticity }).Select(v => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture))) + "|" + PolarSongTable.Current.ContentFingerprint);
		string key = $"{observer.labelId}|{subject}|{kind}|{eventId}|{version}|{Config.Version}";
		observations.TryGetValue(key, out var prior);
		// Keep earned evidence even when a later view asks for a weaker gate; improved staff may refine it.
		if (prior != null && Config.GateScale[prior.gate.ToString()] < Config.GateScale[gate.ToString()]) gate = prior.gate;
		float scale = Config.GateScale[gate.ToString()];
		float demandError = Mathf.Lerp(Config.DemandErrorWeak, Config.DemandErrorStrong, Math.Clamp(observer.productionQuality, 0, 1)) * scale;
		float identityError = Mathf.Lerp(Config.IdentityErrorWeak, Config.IdentityErrorStrong, Math.Clamp(observer.scoutingAbility, 0, 1)) * scale;
		if (kind == "market") identityError = Mathf.Lerp(Config.MarketErrorWeak, Config.MarketErrorStrong, Math.Clamp(observer.marketingPower, 0, 1));
		if (prior != null && prior.demandError <= demandError && prior.identityError <= identityError) return prior;
		if (prior != null) { demandError = Math.Min(demandError, prior.demandError); identityError = Math.Min(identityError, prior.identityError); }
		var observation = new PolarObservation { key = key, observerId = observer.labelId, subjectId = subject, kind = kind, eventId = eventId,
			subjectVersion = version, perceptionVersion = Config.Version, gate = gate, demandError = demandError, identityError = identityError,
			axes = new PolarBand[SongProfile.AxisCount] };
		for (int i = 0; i < axes.Length; i++) observation.axes[i] = Read(axes[i], i < SongProfile.DemandCount ? demandError : identityError, key, "axis" + i);
		observation.reach = Read(reach, identityError, key, "reach");
		observation.rigidity = Read(rigidity, identityError, key, "rigidity");
		observation.ambition = Read(ambition, identityError, key, "ambition");
		observation.plasticity = Read(plasticity, identityError, key, "plasticity");
		if (!observations.ContainsKey(key) && observations.Count >= Config.MaximumObservations) observations.Remove(observations.Keys.First());
		observations[key] = observation;
		return observation;
	}
	private static PolarBand Read(float truth, float error, string key, string salt) {
		float center = Math.Clamp(truth + (Unit(key + "|" + salt) - .5f) * error, 0, 1);
		return new PolarBand(center - error * Config.BandWidth, center + error * Config.BandWidth);
	}
	private static float Unit(string value) { ulong hash = 14695981039346656037UL; foreach (char c in value) { hash ^= c; hash = unchecked(hash * 1099511628211UL); } return (hash >> 40) / 16777216f; }
	private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

	public static PolarBand[] FitBands(PolarBand[] song, PolarBand[] act, PolarBand rigidity, PolarBand[] market) {
		var table = PolarSongTable.Current;
		float minimumPenalty = 0, maximumPenalty = 0, maximumHeadroom = 0;
		for (int i = 0; i < SongProfile.DemandCount; i++) {
			float low = Math.Max(0, song[i].lo - act[i].hi), high = Math.Max(0, song[i].hi - act[i].lo);
			minimumPenalty += song[i].lo * low * low * table.N("capabilityPenalty");
			maximumPenalty += song[i].hi * high * high * table.N("capabilityPenalty");
			maximumHeadroom = Math.Max(maximumHeadroom, act[i].hi - song[i].lo);
		}
		var capability = new PolarBand(MathF.Exp(-maximumPenalty), MathF.Exp(-minimumPenalty) + Math.Max(0, maximumHeadroom) * table.N("headroomBonus"));
		var distance = IdentityDifference(song, act, squared: true);
		var identity = new PolarBand(1 - distance.hi * (table.N("identityBasePenalty") + table.N("rigidityPenalty") * rigidity.hi),
			1 - distance.lo * (table.N("identityBasePenalty") + table.N("rigidityPenalty") * rigidity.lo));
		var moment = new PolarBand(0, 1);
		if (market != null) {
			float low = 0, high = 0;
			foreach (int i in new[] { 0, 1, 3 }) { var delta = Difference(song[SongProfile.DemandCount + i], market[i]); low += delta.lo * delta.lo; high += delta.hi * delta.hi; }
			moment = new PolarBand(1 - MathF.Sqrt(high / 3) * table.N("momentPenalty"), 1 - MathF.Sqrt(low / 3) * table.N("momentPenalty"));
		}
		return new[] { capability, identity, moment };
	}
	public static PolarBand IdentityDifference(PolarBand[] left, PolarBand[] right, bool squared) {
		float low = 0, high = 0, weight = 0;
		for (int i = 0; i < SongProfile.IdentityCount; i++) {
			var delta = Difference(left[SongProfile.DemandCount + i], right[SongProfile.DemandCount + i]);
			float w = PolarSongTable.Current.IdentityWeights[i]; weight += w;
			low += (squared ? delta.lo * delta.lo : delta.lo) * w; high += (squared ? delta.hi * delta.hi : delta.hi) * w;
		}
		return squared ? new PolarBand(MathF.Sqrt(low / weight), MathF.Sqrt(high / weight)) : new PolarBand(low / weight, high / weight);
	}
	private static PolarBand Difference(PolarBand a, PolarBand b) => new(Math.Max(0, Math.Max(a.lo - b.hi, b.lo - a.hi)), Math.Max(Math.Abs(a.lo - b.hi), Math.Abs(a.hi - b.lo)));
}
