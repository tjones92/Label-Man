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
		var demo = new SongComposition { songId = "player-demo:" + artist.artistId + ":" + (choice.WrittenSong?.SongId ?? choice.Title),
			title = choice.Title, primaryGenre = artist.primaryGenre, secondaryGenre = artist.secondaryGenre,
			demoTaxonomy = new SongTaxonomy { primaryGenre = artist.primaryGenre, secondaryGenre = artist.secondaryGenre } };
		CompositionShapeVariation.Ensure(demo); // Local provisional composition, no registry or RNG writes.
		return demo;
	}
	/// <summary>What a song IS, in the player's words: its archetype, mood and lyrical turn, read off the written song.
	/// Empty for material with no composition behind it yet (a fresh original is still a demo). Flavour, not a score:
	/// it tells two songs apart without ranking them.</summary>
	public static string DescribeCharacter(PlayerDesk.MaterialChoice choice) => DescribeCharacter(CompositionCatalogService.GetSong(choice?.SongId)?.demoTaxonomy);
	/// <summary>A released record's character: the master as cut where one was printed, otherwise the written song.</summary>
	public static string DescribeCharacter(Record record) => record == null ? "" : DescribeCharacter(
		PolarSongMetadataService.Get(record.PlugMasterId)?.taxonomy ?? CompositionCatalogService.GetSong(record.songId)?.demoTaxonomy);
	public static string DescribeCharacter(SongTaxonomy taxonomy) {
		if (taxonomy == null) return "";
		string Words(string name) => string.Concat(name.Select((c, i) => i > 0 && char.IsUpper(c) ? " " + char.ToLowerInvariant(c) : i == 0 ? char.ToLowerInvariant(c).ToString() : c.ToString()));
		var parts = new List<string>();
		if (taxonomy.archetype != SongArchetype.Unknown) parts.Add(Words(taxonomy.archetype.ToString()));
		if (!string.IsNullOrWhiteSpace(taxonomy.mood)) parts.Add(taxonomy.mood.Trim().ToLowerInvariant() + " mood");
		SongLyricMode lyric = taxonomy.lyricModes?.FirstOrDefault(mode => mode != SongLyricMode.Unknown) ?? SongLyricMode.Unknown;
		if (lyric != SongLyricMode.Unknown) parts.Add("lyrics: " + Words(lyric.ToString()));
		string text = string.Join("  ·  ", parts);
		return text.Length == 0 ? "" : char.ToUpperInvariant(text[0]) + text[1..];
	}

	public static PolarArrangementProposal Proposal(PlayerDesk.MaterialChoice choice, SimulatedArtist artist, string plannedId, PolarSessionContext session = null) {
		var song = Subject(choice, artist);
		var reference = choice.Kind == PlayerDesk.MaterialKind.LiveCover ? PolarSongBehavior.Reference(song, TimeManager.Instance.CurrentDate.year, choice.ReferenceMasterId) : null;
		return PolarCoverResolver.Propose(song, reference, PolarActProfileDeriver.Derive(artist, PlayerDesk.Instance.Label, session, PolarSongTable.Current),
			artist.primaryGenre, TimeManager.Instance.CurrentDate.year, ChartManager.Instance.GetCurrentChartWeek() + 1,
			plannedId, PolarSongBehavior.CaptureTaste().GetValueOrDefault(artist.primaryGenre), PolarSongTable.Current);
	}

	/// <summary>Kind suffix for reads made by the scout's ear rather than by working with the act in a room.</summary>
	public const string EarKind = ":ear";
	private static readonly string[] Demands = { "vocal power", "vocal nuance", "instrumental skill", "ensemble precision", "lyric delivery", "studio execution" };

	/// <summary>Studio hearings project the act onto the song (identity, fit, pushback). Every other hearing is the
	/// player's ear on the song itself: its shape, its hook and, when the act was watched playing it, how tight they were.</summary>
	public static PolarComparisonRead Compare(PlayerDesk.MaterialChoice choice, SimulatedArtist artist, PolarEvidenceGate gate,
		string eventId, string plannedId, PolarSessionContext session = null, string printedMasterId = null, PolarHearing hearing = null) {
		hearing ??= PolarHearing.Studio;
		bool ear = hearing.IsEar;
		var label = PlayerDesk.Instance.Label;
		PolarArrangementProposal proposal;
		SongComposition song;
		if (printedMasterId == null) { song = Subject(choice, artist); proposal = Proposal(choice, artist, plannedId, session); }
		else {
			var master = PolarSongMetadataService.Get(printedMasterId) ?? throw new ArgumentException("Unknown printed master");
			song = CompositionCatalogService.GetSong(master.songId);
			var parent = PolarSongMetadataService.Get(master.parentRecordingId);
			proposal = new PolarArrangementProposal { songId = master.songId, parentRecordingId = master.parentRecordingId,
				referenceProfile = SongProfileDeriver.Derive(song, parent?.taxonomy ?? song.demoTaxonomy, PolarSongTable.Current),
				realizedProfile = master.cachedProfile?.Copy() ?? SongProfileDeriver.Derive(song, master.taxonomy, PolarSongTable.Current) };
		}
		var actor = PolarActProfileDeriver.Derive(artist, label, session, PolarSongTable.Current);
		// Your own act is known from working with them; a stranger on a stage is only what the scout's ear caught.
		var act = Observe(actor.axes, actor.interpretiveReach, actor.identityRigidity, artist.evolution?.artisticAmbition ?? .5f,
			0, label, "act:" + artist.artistId, "performer" + (hearing.WatchedAct ? EarKind : ""), eventId, gate);
		string subject = proposal.parentRecordingId ?? "demo:" + proposal.songId;
		string earKind = ear ? EarKind : "";
		var reference = Observe(proposal.referenceProfile.axes, 0, 0, 0, proposal.referenceProfile.plasticity, label, subject, "heard-material" + earKind, eventId, gate);
		var proposed = Observe(proposal.realizedProfile.axes, 0, 0, 0, proposal.realizedProfile.plasticity, label,
			printedMasterId == null ? "arrangement:" + artist.artistId + ":" + subject + ":" + plannedId : "master:" + printedMasterId,
			(printedMasterId == null ? "prospective-arrangement" : "printed-performance") + earKind, eventId, gate);
		if (ear) return Hear(choice, artist, gate, eventId, hearing, song, proposal, act, reference, proposed, printedMasterId);
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
		float standing = Standing(artist);
		float softening = PlayerDesk.Instance.RepertoireFor(artist.artistId).Count == 0 && !PlayerDesk.Instance.SongsFor(artist.artistId).Any()
			? PolarSongTable.Current.N("emptySongbookSoftening") : 1;
		// Pushback is only voiced when the staff are sure: they have worked the song with the act (rehearsal or
		// playback, not a rough demo read), and their best guess -- the centres of what they observed, run
		// through the same refusal rule the act uses -- says the act will balk.
		bool emptySongbook = softening != 1;
		float[] Centers(PolarObservation o) => o.axes.Select(b => b.Center).ToArray();
		var guess = PolarMaterialFit.Evaluate(new SongProfile { axes = Centers(reference) }, new SongProfile { axes = Centers(proposed) },
			new ActProfile { axes = Centers(act), identityRigidity = act.rigidity.Center }, null, 0, PolarSongTable.Current);
		bool resistance = gate is PolarEvidenceGate.Rehearsal or PolarEvidenceGate.Playback &&
			PolarMaterialFit.WouldRefuse(guess, act.ambition.Center, standing, emptySongbook, PolarSongTable.Current);
		string explanation = StretchText(proposed.axes, act.axes, "The arrangement", "their", "The heard arrangement seems within their playing range.");
		int identityGap = Enumerable.Range(SongProfile.DemandCount, SongProfile.IdentityCount).OrderByDescending(i => Math.Abs(proposed.axes[i].Center - act.axes[i].Center)).First();
		string[] mismatch = { "Its edge sits outside the act's style.", "Its polish sits outside the act's style.", "Its emotional tone is unfamiliar to them.", "Its outlook sits outside the act's style." };
		float pull = proposed.axes[(int)SongAxis.Toughness].Center - reference.axes[(int)SongAxis.Toughness].Center;
		return new PolarComparisonRead { songTitle = choice.Title, actName = artist.stageName, reference = reference, proposed = proposed, act = act,
			referenceFit = before, proposedFit = after, hasMarketEvidence = hasMarket, mayResist = resistance, isRecorded = printedMasterId != null,
			source = PolarHearingSource.Studio, showsAct = true, showsFit = true,
			subjectLabel = choice.Kind == PlayerDesk.MaterialKind.Original && proposal.parentRecordingId == null ? "Their own demo"
				: proposal.parentRecordingId == null ? "Reference: the sheet music" : "Reference: " + RecordName(song, proposal.parentRecordingId),
			evidenceLabel = GateLabel(gate), explanation = explanation,
			resistance = resistance ? "Expect pushback on this one. " + mismatch[identityGap - SongProfile.DemandCount] : "",
			arrangement = printedMasterId != null ? "Playback: the printed version." : Math.Abs(pull) < Config.ArrangementDescriptionThreshold ? "Preview: a reading close to the heard version." : pull > 0 ? "Preview: a harder-edged reading." : "Preview: a gentler reading." };
	}

	private static PolarComparisonRead Hear(PlayerDesk.MaterialChoice choice, SimulatedArtist artist, PolarEvidenceGate gate, string eventId,
		PolarHearing hearing, SongComposition song, PolarArrangementProposal proposal, PolarObservation act, PolarObservation reference,
		PolarObservation proposed, string printedMasterId) {
		var label = PlayerDesk.Instance.Label;
		var heard = hearing.source == PolarHearingSource.Record ? reference : proposed;
		var read = new PolarComparisonRead { songTitle = choice.Title, actName = artist.stageName, reference = reference, proposed = proposed, act = act,
			heard = heard, source = hearing.source, showsAct = hearing.WatchedAct, showsFit = hearing.fitWithAct, isRecorded = printedMasterId != null,
			fromSheetMusic = hearing.source == PolarHearingSource.Record && proposal.parentRecordingId == null,
			evidenceLabel = GateLabel(gate), resistance = "", arrangement = "", explanation = "" };
		read.subjectLabel = hearing.source switch {
			PolarHearingSource.Venue => "Heard live · " + hearing.place + (hearing.when is GameDate when ? ", " + when.ToHeadlineString() : ""),
			PolarHearingSource.Playback => "Your pressing",
			_ => read.fromSheetMusic ? "Known from the sheet music · no record heard" : "Heard on record · " + RecordName(song, proposal.parentRecordingId)
		};

		// The hook is the scout's ear on the tune itself. A venue read is the one the set list already showed.
		float scouting = Math.Clamp(label.scoutingAbility, 0, 1);
		if (hearing.heardHook is float heardHook) {
			float spread = PlayerDesk.ScoutingReadNoise(hearing.heardHookConfidence);
			read.hook = new PolarBand(heardHook - spread, heardHook + spread);
			read.hookText = Capitalize(DescribeHook(heardHook, hearing.heardHookConfidence));
		} else {
			float truth = song?.commercialHook ?? 0;
			if (printedMasterId != null) truth = PlayerDesk.Instance.Masters.FirstOrDefault(m => m.Record.masterId == printedMasterId)?.Record.hookStrength ?? truth;
			float error = Mathf.Lerp(Config.HookErrorWeak, Config.HookErrorStrong, scouting) * Config.GateScale[gate.ToString()];
			read.hook = Read(truth, error, $"{label.labelId}|{heard.subjectId}|hook|{eventId}", "hook", $"{label.labelId}|hook|{hearing.source}");
			read.hookText = Capitalize(DescribeHook(read.hook.Center, BandConfidence(read.hook)));
		}

		if (hearing.WatchedAct) {
			var ensemble = act.axes[(int)SongAxis.Ensemble];
			string tight = ensemble.Center >= .7f ? "locked in" : ensemble.Center >= .5f ? "tight enough" : ensemble.Center >= .3f ? "a little loose" : "ragged";
			read.tightnessText = Capitalize(BandConfidence(ensemble) >= .7f ? tight : "probably " + tight);
			// Studio execution is not something a stage shows.
			int weakest = Enumerable.Range(0, (int)SongAxis.StudioCraft).OrderByDescending(i => heard.axes[i].Center - act.axes[i].Center).First();
			read.explanation = heard.axes[weakest].lo > act.axes[weakest].hi ? $"They were audibly reaching on the {Demands[weakest]}."
				: heard.axes[weakest].Center > act.axes[weakest].Center ? $"The {Demands[weakest]} may have stretched them." : "They had it well in hand.";
		}
		if (hearing.fitWithAct) {
			// Choosing material for your own act: a short read against what you know of them. The full
			// arrangement, market moment and any pushback are studio reads.
			read.referenceFit = FitBands(reference.axes, act.axes, act.rigidity, null);
			read.explanation = StretchText(reference.axes, act.axes, "It", "their", $"It looks within {artist.stageName}'s range.");
		}
		return read;
	}

	/// <summary>
	/// The song's biggest asks of the act, graded. Naming only the single weakest axis made every song of one
	/// archetype read "may stretch their vocal power" word for word; the size of the stretch, and the second
	/// demand when it is a real one, is what tells two songs apart. Sizes are the read centres, so they carry
	/// the same uncertainty the bands show.
	/// </summary>
	private static string StretchText(PolarBand[] song, PolarBand[] act, string subject, string possessive, string within) {
		var gaps = Enumerable.Range(0, SongProfile.DemandCount)
			.Select(i => (Axis: i, Gap: song[i].Center - act[i].Center))
			.OrderByDescending(entry => entry.Gap).ToList();
		var first = gaps[0];
		if (first.Gap <= 0.02f) return within;
		string Size(float gap) => gap < .06f ? "slightly" : gap < .14f ? "noticeably" : gap < .24f ? "a good deal" : "far beyond what they have";
		string text = $"{subject} may stretch {possessive} {Demands[first.Axis]} {Size(first.Gap)}";
		var second = gaps[1];
		if (second.Gap >= .05f) text += $", and {possessive} {Demands[second.Axis]} {Size(second.Gap)}";
		text += ".";
		return text;
	}

	public static string DescribeHook(float value, float confidence) {
		string core = HookWord(value);
		return confidence >= 0.7f ? core : confidence >= 0.45f ? $"likely {core}" : $"might be {core}";
	}
	/// <summary>The unhedged bucket for a hook read; two reads in different buckets are a different verdict.</summary>
	public static string HookWord(float value) =>
		value >= 0.75f ? "a standout hook" : value >= 0.55f ? "a strong tune" : value >= 0.35f ? "a fair number" : "a weak number";
	/// <summary>Reads a band's width back onto the scouting-confidence scale the set list uses.</summary>
	private static float BandConfidence(PolarBand band) => Math.Clamp(1 - (band.hi - band.lo) * 1.5f, 0, 1);
	private static string Capitalize(string text) => string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text[1..];
	private static string GateLabel(PolarEvidenceGate gate) => gate switch {
		PolarEvidenceGate.FirstListen => "first listen · broad read",
		PolarEvidenceGate.FollowUp => "second look · closer read",
		PolarEvidenceGate.Rehearsal => "rehearsal · working read",
		PolarEvidenceGate.Playback => "playback · close read",
		_ => "rough read"
	};
	private static string RecordName(SongComposition song, string masterId) {
		var memory = song?.recordings?.FirstOrDefault(r => r.recordId == masterId);
		var master = PolarSongMetadataService.Get(masterId);
		string name = memory?.artistName ?? (master?.artistId == null ? null : ArtistManager.Instance?.GetArtist(master.artistId)?.stageName);
		int year = memory?.year ?? master?.recordingYear ?? 0;
		return name == null ? (year > 0 ? $"a {year} record" : "a record") : year > 0 ? $"{name}'s {year} record" : $"{name}'s record";
	}

	public static PolarObservation Observe(float[] axes, float reach, float rigidity, float ambition, float plasticity,
		AILabel observer, string subject, string kind, string eventId, PolarEvidenceGate gate) {
		string version = Hash(string.Join("|", axes.Concat(new[] { reach, rigidity, ambition, plasticity }).Select(v => v.ToString("R", System.Globalization.CultureInfo.InvariantCulture))) + "|" + PolarSongTable.Current.ContentFingerprint);
		string key = $"{observer.labelId}|{subject}|{kind}|{eventId}|{version}|{Config.Version}|{NoiseModel}";
		observations.TryGetValue(key, out var prior);
		// Keep earned evidence even when a later view asks for a weaker gate; improved staff may refine it.
		if (prior != null && Config.GateScale[prior.gate.ToString()] < Config.GateScale[gate.ToString()]) gate = prior.gate;
		float scale = Config.GateScale[gate.ToString()];
		// Away from the studio, reading what a performance demands is the scout's ear, not the producer's.
		float demandSkill = kind.EndsWith(EarKind, StringComparison.Ordinal) ? observer.scoutingAbility : observer.productionQuality;
		float demandError = Mathf.Lerp(Config.DemandErrorWeak, Config.DemandErrorStrong, Math.Clamp(demandSkill, 0, 1)) * scale;
		float identityError = Mathf.Lerp(Config.IdentityErrorWeak, Config.IdentityErrorStrong, Math.Clamp(observer.scoutingAbility, 0, 1)) * scale;
		if (kind == "market") identityError = Mathf.Lerp(Config.MarketErrorWeak, Config.MarketErrorStrong, Math.Clamp(observer.marketingPower, 0, 1));
		if (prior != null && prior.demandError <= demandError && prior.identityError <= identityError) return prior;
		if (prior != null) { demandError = Math.Min(demandError, prior.demandError); identityError = Math.Min(identityError, prior.identityError); }
		var observation = new PolarObservation { key = key, observerId = observer.labelId, subjectId = subject, kind = kind, eventId = eventId,
			subjectVersion = version, perceptionVersion = Config.Version, gate = gate, demandError = demandError, identityError = identityError,
			axes = new PolarBand[SongProfile.AxisCount] };
		for (int i = 0; i < axes.Length; i++) observation.axes[i] = Read(axes[i], i < SongProfile.DemandCount ? demandError : identityError, key, "axis" + i,
			$"{observer.labelId}|{kind}|axis{i}");
		observation.reach = Read(reach, identityError, key, "reach");
		observation.rigidity = Read(rigidity, identityError, key, "rigidity");
		observation.ambition = Read(ambition, identityError, key, "ambition");
		observation.plasticity = Read(plasticity, identityError, key, "plasticity");
		if (!observations.ContainsKey(key) && observations.Count >= Config.MaximumObservations) observations.Remove(observations.Keys.First());
		observations[key] = observation;
		return observation;
	}
	/// <summary>Part of every observation key, so reads made under an older noise model are never reused.</summary>
	private const string NoiseModel = "shared-bias-v1";
	/// <summary>How much of a read's miss is the observer's own lean (the same for every song of that kind on
	/// that axis) rather than luck on this one hearing. A producer who over-hears vocal power does it to every
	/// song, so two songs heard the same way still compare truthfully even while each read is uncertain. With
	/// fully independent noise, the miss on each song was as big as the real difference between songs of one
	/// archetype and the ordering between them was scrambled.</summary>
	private const float SharedBiasShare = .75f;
	private static PolarBand Read(float truth, float error, string key, string salt, string biasKey = null) {
		float miss = biasKey == null ? Unit(key + "|" + salt) - .5f
			: SharedBiasShare * (Unit(biasKey + "|lean") - .5f) + (1 - SharedBiasShare) * (Unit(key + "|" + salt) - .5f);
		float center = Math.Clamp(truth + miss * error, 0, 1);
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
