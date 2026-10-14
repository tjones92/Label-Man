using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Phase 4c of SimTools/BandMemberSimulationDirective.md (§4.14 layers 2-4), behind <c>--enable-team-writing</c>.
/// <para>
/// <b>Team craft.</b> A member's crafts are derived, not stored: melody from creativity x musicalVersatility, lyric
/// from creativity x diction, hook from creativity x stagePresence. A team scores the stronger of each craft less a
/// friction term that grows with how alike the writers are, so complementary specialists make the strongest pairing
/// and two of the same kind are barely better than one. The song moves by the team's craft over its lead writer's
/// own, so a one-writer song is exactly what it was; rivalry adds a small bounded bonus.
/// </para>
/// <para>
/// <b>Professional teams.</b> Staff writers at the same publisher pair into standing teams by keyed draws preferring
/// complementary crafts; a keyed share of each team member's catalogue becomes team songs (split credit, team craft),
/// one for one, so the catalogue keeps its size. The catalogue's own stream is untouched.
/// </para>
/// <para>
/// <b>Cut-ins.</b> An AI label with a Shark manager that owns the act's publishing adds its name to some originals:
/// a share of the writers' credit, and CreditAndMoney and Outsider strain on the writers' edges -- the first strain a
/// label, not a bandmate, causes.
/// </para>
/// </summary>
public static class TeamWritingService {
	/// <summary>How far a song moves per unit of team craft over the lead's own. The stronger of two crafts beats the
	/// lead's own by ~.09 on average, so at .5 the bms5-on-1001 window raised the mean hook of originals and of professional
	/// songs by .015 each (release hook +1.3%); .2 keeps complementary teams the strongest and the mean rise near .3%.</summary>
	public const float TeamCraftWeight = 0.20f;
	/// <summary>Per extra writer, at full similarity: two writers with the same strengths mostly get in each other's way.</summary>
	public const float TeamFriction = 0.035f;
	public const float RivalryCraftBonus = 0.06f, RivalryCraftCap = 0.03f;
	public const float ProTeamShare = 0.50f, ProTeamSongShare = 0.60f;
	public const float AiCutInChance = 0.35f, AiCutInCreditShare = 0.25f, CutInStrain = 0.06f;

	public readonly struct Crafts {
		public readonly float Melody, Lyric, Hook;
		public Crafts(float melody, float lyric, float hook) { Melody = melody; Lyric = lyric; Hook = hook; }
	}

	public static Crafts Of(Musician m) {
		float diction = m.axesVersion > 0 ? m.diction : 0.5f;
		return new Crafts(m.creativity * m.musicalVersatility, m.creativity * diction, m.creativity * m.stagePresence);
	}

	public static Crafts Of(ProfessionalSongwriter w) => new(w.melodyCraft, w.lyricCraft, w.hookCraft);

	/// <summary>The team's craft over its lead's own, per craft, after friction and rivalry. Zero for a lone writer.</summary>
	public static Crafts Delta(IReadOnlyList<Crafts> team, int leadIndex, float rivalry) {
		if (team == null || team.Count < 2) return default;
		Crafts lead = team[leadIndex];
		float melody = team.Max(c => c.Melody), lyric = team.Max(c => c.Lyric), hook = team.Max(c => c.Hook);
		float diff = 0f; int pairs = 0;
		for (int i = 0; i < team.Count; i++) for (int j = i + 1; j < team.Count; j++) {
			diff += (Math.Abs(team[i].Melody - team[j].Melody) + Math.Abs(team[i].Lyric - team[j].Lyric) + Math.Abs(team[i].Hook - team[j].Hook)) / 3f;
			pairs++;
		}
		float similarity = Mathf.Clamp(1f - 3f * diff / Math.Max(1, pairs), 0f, 1f);
		float friction = TeamFriction * (team.Count - 1) * (0.4f + 0.6f * similarity);
		float bonus = Mathf.Min(RivalryCraftCap, RivalryCraftBonus * rivalry);
		return new Crafts(melody - lead.Melody - friction + bonus, lyric - lead.Lyric - friction + bonus, hook - lead.Hook - friction + bonus);
	}

	/// <summary>
	/// The craft shift for an artist original about to be written: the team that WILL be credited (a side-effect-free
	/// peek at the same keyed draw), scored against its lead. Zero with the phase off or a lone writer.
	/// </summary>
	public static Crafts ForArtistOriginal(SimulatedArtist artist, string songKey, int year) {
		if (!BandLife.TeamWritingEnabled || !BandLife.CowritingEnabled || artist == null) return default;
		var team = CowritingService.PickTeam(artist, songKey, year, record: false);
		if (team.Count < 2) return default;
		var crafts = team.Select(t => Of(t.Member)).ToList();
		int lead = 0;
		for (int i = 1; i < team.Count; i++) if (team[i].Share > team[lead].Share) lead = i;
		float rivalry = BandLifeService.FindEdge(artist, team[0].Member, team[1].Member)?.rivalry ?? 0f;
		return Delta(crafts, lead, rivalry);
	}

	// ---- professional teams -------------------------------------------------------------------------------

	/// <summary>
	/// Pairs staff writers into standing teams and converts a keyed share of their catalogue to team songs. Runs once
	/// per world (catalogue initialization, or the first load of an older world). Keyed on the world seed.
	/// </summary>
	public static int BuildProfessionalTeams(IList<ProfessionalSongwriter> writers, IEnumerable<SongComposition> catalogue, ulong seed) {
		if (!BandLife.TeamWritingEnabled || writers == null) return 0;
		string world = seed.ToString(System.Globalization.CultureInfo.InvariantCulture);
		float Unit(string key) => RepertoireTaxonomy.Unit(world + "|pro-team|" + key);
		var byId = writers.ToDictionary(w => w.writerId, StringComparer.Ordinal);
		foreach (var group in writers.GroupBy(w => w.publisherId).OrderBy(g => g.Key, StringComparer.Ordinal)) {
			var open = group.Where(w => string.IsNullOrEmpty(w.teamPartnerId) && Unit(w.writerId + "|joins") < ProTeamShare)
				.OrderBy(w => w.writerId, StringComparer.Ordinal).ToList();
			while (open.Count >= 2) {
				ProfessionalSongwriter a = open[0];
				open.RemoveAt(0);
				// Prefer complementary crafts: the partner whose strengths differ most, ties broken by a keyed draw.
				ProfessionalSongwriter b = open.OrderByDescending(w => Complement(a, w) + 0.05f * Unit(a.writerId + "|" + w.writerId))
					.ThenBy(w => w.writerId, StringComparer.Ordinal).First();
				open.Remove(b);
				a.teamPartnerId = b.writerId;
				b.teamPartnerId = a.writerId;
			}
		}
		int converted = 0;
		foreach (SongComposition song in catalogue.Where(s => s.originKind == SongOriginKind.ProfessionalOffice).OrderBy(s => s.songId, StringComparer.Ordinal)) {
			if (song.credits.Count != 1) continue;
			SongwriterCredit credit = song.credits[0];
			if (credit.writerId == null || !byId.TryGetValue(credit.writerId, out var author) || string.IsNullOrEmpty(author.teamPartnerId)) continue;
			if (!byId.TryGetValue(author.teamPartnerId, out var partner) || Unit(song.songId + "|team-song") >= ProTeamSongShare) continue;
			Crafts d = Delta(new[] { Of(author), Of(partner) }, 0, 0f);
			song.compositionQuality = Mathf.Clamp(song.compositionQuality + TeamCraftWeight * d.Melody, 0f, 1f);
			song.melodicStrength = Mathf.Clamp(song.melodicStrength + TeamCraftWeight * d.Melody, 0f, 1f);
			song.lyricQuality = Mathf.Clamp(song.lyricQuality + TeamCraftWeight * d.Lyric, 0f, 1f);
			song.commercialHook = Mathf.Clamp(song.commercialHook + TeamCraftWeight * d.Hook, 0f, 1f);
			credit.share = 0.5f;
			song.credits.Add(new SongwriterCredit { writerType = WriterEntityType.ProfessionalSongwriter, writerId = partner.writerId,
				writerName = partner.name, share = 0.5f });
			converted++;
		}
		return converted;
	}

	private static float Complement(ProfessionalSongwriter a, ProfessionalSongwriter b) =>
		Math.Abs(a.melodyCraft - b.melodyCraft) + Math.Abs(a.lyricCraft - b.lyricCraft) + Math.Abs(a.hookCraft - b.hookCraft);

	// ---- AI cut-ins ---------------------------------------------------------------------------------------

	/// <summary>The share an AI label takes of this original's writer credit, 0 for none. Keyed per song.</summary>
	public static float AiCutInShare(SimulatedArtist artist, string songKey) {
		if (!BandLife.TeamWritingEnabled || artist == null || artist.isPlayerOwned || string.IsNullOrEmpty(artist.labelId)) return 0f;
		if (artist.manager != ManagerArchetype.Shark || !artist.labelOwnsPublishing) return 0f;
		return BandLife.Chance($"cut-in|{artist.artistId}|{songKey}", AiCutInChance) ? AiCutInCreditShare : 0f;
	}

	/// <summary>The writers remember: strain on each credited member's worst edge, or a new one with a bandmate.</summary>
	public static void ApplyCutInStrain(SimulatedArtist artist, IEnumerable<Musician> credited, int year) {
		foreach (Musician m in credited) {
			Musician other = artist.members.FirstOrDefault(x => x != m && x.isActive);
			if (other == null) continue;
			MemberRelation worst = BandLifeService.WorstEdgeOf(artist, m) ?? BandLifeService.Edge(artist, m, other);
			BandLifeService.AddStrain(worst, StrainCause.Outsider, CutInStrain, year);
			BandLifeService.AddStrain(worst, StrainCause.CreditAndMoney, CutInStrain, year);
		}
	}
}
