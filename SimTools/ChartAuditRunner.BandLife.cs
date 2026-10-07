using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;

// Band-member simulation telemetry (SimTools/BandMemberSimulationDirective.md §6: "All telemetry goes to
// SimLogs/<run>-lineup-*.csv"). Every file here is diagnostic: none of it is an economy CSV, so the inertness
// gates compare everything EXCEPT these.
public partial class ChartAuditRunner {
	private StreamWriter lineupEventWriter;
	private StreamWriter lineupAnnualWriter;
	private StreamWriter lineupPairWriter;

	private void OpenBandLifeOutputs() {
		if (!BandLife.AnnualPassActive) return;
		string dir = ProjectSettings.GlobalizePath("res://SimLogs");
		lineupEventWriter = CreateWriter(Path.Combine(dir, $"{runName}-lineup-events.csv"));
		lineupEventWriter.WriteLine("year,artistId,stageName,personId,personName,otherPersonId,event,kind,cause,strain," +
			"everCharted,chartedThisYear,constitution,channel,applied,playerOwned,age,detail");
		lineupAnnualWriter = CreateWriter(Path.Combine(dir, $"{runName}-lineup-annual.csv"));
		lineupAnnualWriter.WriteLine("year,actsInScope,groupActs,chartedGroupActs,edges,secretEdges,meanStrain,maxStrain,meanMorale," +
			"brewing,ultimatums,aiConcessions,strainDepartures,strainDeparturesCharted,strainDeparturesNeverCharted,cooldownDeferred," +
			"breakerCap,breakerDeferred," + string.Join(",", Enum.GetNames(typeof(DepartureKind)).Select(n => "kind" + n)) + "," +
			string.Join(",", Enum.GetNames(typeof(StrainCause)).Select(n => "cause" + n)) + "," +
			string.Join(",", Enum.GetNames(typeof(DeathChannel)).Select(n => "death" + n)) + "," +
			"deathsCharting,draftEligible,drafted,draftedCharting,draftReturns,exhaustion,substanceOnsets,busts,marriages,children," +
			"couples,coupleBreakups,affairs,discoveries,quietDissolutionsCharted,quietDissolutionsNeverCharted,poolSize,poolEntries," +
			"poolExpired,replacements,replacementsFromPool,spinOuts,dissolutions,recombinations,leavingMemberOptions");
		BandLifeService.OnEvent += WriteLineupEvent;
		BandLifeService.OnAnnualSummary += WriteLineupAnnual;
		// Calibration only: every pair's raw strain terms each year (large; opt-in).
		if (Array.Exists(OS.GetCmdlineUserArgs(), a => a == "--log-band-life-pairs")) {
			lineupPairWriter = CreateWriter(Path.Combine(dir, $"{runName}-lineup-pairs.csv"));
			lineupPairWriter.WriteLine("year,artistId,everCharted,chartedNow,top40Now,constitution,row,personA,personB,creditRaw,spotlightRaw," +
				"directionRaw,projectFriction,reliabilityRaw,substanceRaw,outsiderRaw,rivalry,scale,workFactor,solvent,strainBefore,strainAfter," +
				"creditShareA,creditShareB,temperamentA,temperamentB,loyaltyA,loyaltyB,egoA,egoB,ambitionA,ambitionB,reliabilityA,reliabilityB," +
				"substanceA,substanceB,spotA,spotB,recognitionA,recognitionB,soloViableA,soloViableB,writerA,writerB,leadA,leadB,burnoutRaw,morale");
			BandLifeService.OnPairTerms += WriteLineupPair;
		}
	}

	private void WriteLineupPair(BandLifeService.PairTermRow r) {
		static string B(bool v) => v ? "1" : "0";
		lineupPairWriter?.WriteLine(string.Join(",", new[] {
			r.year.ToString(CultureInfo.InvariantCulture), r.artistId, B(r.everCharted), r.chartedNow.ToString(CultureInfo.InvariantCulture),
			r.top40Now.ToString(CultureInfo.InvariantCulture), r.constitution.ToString(), r.burnoutRow ? "act" : "pair",
			r.burnoutRow ? r.burnoutDefaultA : r.personA, r.burnoutRow ? r.burnoutDefaultB : r.personB,
			F(r.creditRaw), F(r.spotlightRaw), F(r.directionRaw), F(r.projectFriction), F(r.reliabilityRaw), F(r.substanceRaw), F(r.outsiderRaw),
			F(r.rivalry), F(r.scale), F(r.workFactor), F(r.solvent), F(r.strainBefore), F(r.strainAfter), F(r.creditShareA), F(r.creditShareB),
			F(r.temperamentA), F(r.temperamentB), F(r.loyaltyA), F(r.loyaltyB), F(r.egoA), F(r.egoB), F(r.ambitionA), F(r.ambitionB),
			F(r.reliabilityA), F(r.reliabilityB), F(r.substanceA), F(r.substanceB), F(r.spotA), F(r.spotB), F(r.recognitionA), F(r.recognitionB),
			B(r.soloViableA), B(r.soloViableB), B(r.writerA), B(r.writerB), B(r.leadA), B(r.leadB), F(r.burnoutRaw), F(r.morale)
		}));
	}

	private void WriteLineupEvent(BandLifeEvent e) {
		lineupEventWriter?.WriteLine(string.Join(",", new[] {
			e.year.ToString(CultureInfo.InvariantCulture), Csv(e.artistId), Csv(e.stageName), Csv(e.personId), Csv(e.personName),
			Csv(e.otherPersonId), Csv(e.eventType), e.kind.ToString(), e.cause.ToString(), F(e.strain),
			e.everCharted ? "true" : "false", e.chartedThisYear ? "true" : "false", e.constitution.ToString(), e.channel.ToString(),
			e.applied ? "true" : "false", e.playerOwned ? "true" : "false", e.age.ToString(CultureInfo.InvariantCulture), Csv(e.detail)
		}));
	}

	private void WriteLineupAnnual(BandLifeAnnualSummary s) {
		static string I(int v) => v.ToString(CultureInfo.InvariantCulture);
		var cells = new List<string> {
			I(s.year), I(s.actsInScope), I(s.groupActs), I(s.chartedGroupActs), I(s.edges), I(s.secretEdges), F(s.meanStrain),
			F(s.maxStrain), F(s.meanMorale), I(s.brewing), I(s.ultimatums), I(s.aiConcessions), I(s.strainDepartures),
			I(s.strainDeparturesCharted), I(s.strainDeparturesNeverCharted), I(s.cooldownDeferred), I(s.breakerCap), I(s.breakerDeferred)
		};
		cells.AddRange(s.departuresByKind.Select(I));
		cells.AddRange(s.departuresByCause.Select(I));
		cells.AddRange(s.deathsByChannel.Select(I));
		cells.AddRange(new[] {
			I(s.deathsCharting), I(s.draftEligible), I(s.drafted), I(s.draftedCharting), I(s.draftReturns), I(s.exhaustion),
			I(s.substanceOnsets), I(s.busts), I(s.marriages), I(s.children), I(s.couples), I(s.coupleBreakups), I(s.affairs),
			I(s.discoveries), I(s.quietDissolutionsCharted), I(s.quietDissolutionsNeverCharted), I(s.poolSize), I(s.poolEntries),
			I(s.poolExpired), I(s.replacements), I(s.replacementsFromPool), I(s.spinOuts), I(s.dissolutions), I(s.recombinations),
			I(s.leavingMemberOptions)
		});
		lineupAnnualWriter?.WriteLine(string.Join(",", cells));
		lineupAnnualWriter?.Flush();
	}

	/// <summary>End-of-run: credit shape (Phase 1 gate) and member axes (Phase 1b report).</summary>
	private void WriteBandLifeEndOfRun() {
		BandLifeService.OnEvent -= WriteLineupEvent;
		BandLifeService.OnAnnualSummary -= WriteLineupAnnual;
		BandLifeService.OnPairTerms -= WriteLineupPair;
		lineupPairWriter?.Dispose(); lineupPairWriter = null;
		lineupEventWriter?.Dispose(); lineupEventWriter = null;
		lineupAnnualWriter?.Dispose(); lineupAnnualWriter = null;
		if (ArtistManager.Instance == null) return;
		string dir = ProjectSettings.GlobalizePath("res://SimLogs");
		WriteCreditShape(dir);
		if (BandLife.MemberAxesEnabled) WriteMemberAxes(dir);
	}

	/// <summary>
	/// Per act with credited originals: how the credit is split among its members, largest share first. The
	/// Phase 1 gate reads this for "a realistic share distribution, not 100/0". Plus the per-year co-writing rate.
	/// </summary>
	private void WriteCreditShape(string dir) {
		var stints = CompositionCatalogService.WriterStintLedger.GroupBy(e => e.artistId)
			.ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
		using (StreamWriter w = CreateWriter(Path.Combine(dir, $"{runName}-lineup-credits.csv"))) {
			w.WriteLine("artistId,stageName,type,activeMembers,writerFlags,creditedMembers,songs,coWrittenSongs,share1,share2,share3,shareRest,pacts,everCharted");
			foreach (SimulatedArtist a in ArtistManager.Instance.GetAllArtists().OrderBy(a => a.artistId, StringComparer.Ordinal)) {
				if (!stints.TryGetValue(a.artistId, out var entries)) continue;
				float total = entries.Sum(e => e.creditMass);
				if (total <= 0f) continue;
				var shares = entries.Select(e => e.creditMass / total).OrderByDescending(v => v).ToList();
				float Share(int i) => i < shares.Count ? shares[i] : 0f;
				int songs = (int)Math.Round(total);
				int co = entries.Sum(e => e.coWrittenSongs);
				w.WriteLine(string.Join(",", new[] {
					Csv(a.artistId), Csv(a.stageName), a.type.ToString(),
					a.members.Count(m => m.isActive).ToString(CultureInfo.InvariantCulture),
					a.members.Count(m => m.isActive && m.isPrimaryWriter).ToString(CultureInfo.InvariantCulture),
					entries.Count(e => e.creditMass > 0f).ToString(CultureInfo.InvariantCulture),
					songs.ToString(CultureInfo.InvariantCulture), co.ToString(CultureInfo.InvariantCulture),
					F(Share(0)), F(Share(1)), F(Share(2)), F(Math.Max(0f, 1f - Share(0) - Share(1) - Share(2))),
					(a.writingPartnerships?.Count(p => p.pact) ?? 0).ToString(CultureInfo.InvariantCulture),
					a.charted > 0 ? "true" : "false"
				}));
			}
		}
		using (StreamWriter w = CreateWriter(Path.Combine(dir, $"{runName}-lineup-credit-years.csv"))) {
			w.WriteLine("year,originals,multiWriterOriginals,multiWriterShare,pactsFormed");
			foreach (int year in CowritingService.OriginalsByYear.Keys.OrderBy(y => y)) {
				int n = CowritingService.OriginalsByYear[year];
				int multi = CowritingService.MultiWriterByYear.GetValueOrDefault(year);
				w.WriteLine(string.Join(",", year.ToString(CultureInfo.InvariantCulture), n.ToString(CultureInfo.InvariantCulture),
					multi.ToString(CultureInfo.InvariantCulture), F(n == 0 ? 0f : multi / (float)n),
					CowritingService.PactsFormedByYear.GetValueOrDefault(year).ToString(CultureInfo.InvariantCulture)));
			}
		}
	}

	/// <summary>Phase 1b report: the distribution of the new axes, how close ceilings sit to ability, and how
	/// strongly a ceiling correlates with today's skill (directive §4.13 asks for roughly .35).</summary>
	private void WriteMemberAxes(string dir) {
		var people = ArtistManager.Instance.GetAllArtists().Where(a => a?.members != null)
			.SelectMany(a => a.members).Where(m => m != null && m.axesVersion > 0).ToList();
		using StreamWriter w = CreateWriter(Path.Combine(dir, $"{runName}-lineup-axes.csv"));
		w.WriteLine("group,count,metric,mean,sd,p10,p50,p90");
		void Row(string group, IEnumerable<float> values, string metric) {
			var v = values.OrderBy(x => x).ToList();
			if (v.Count == 0) return;
			float mean = v.Average();
			float sd = (float)Math.Sqrt(v.Sum(x => (x - mean) * (x - mean)) / v.Count);
			float Q(float q) => v[Math.Clamp((int)(q * (v.Count - 1)), 0, v.Count - 1)];
			w.WriteLine(string.Join(",", group, v.Count.ToString(CultureInfo.InvariantCulture), metric, F(mean), F(sd), F(Q(.1f)), F(Q(.5f)), F(Q(.9f))));
		}
		foreach ((string group, List<Musician> set) in new[] {
			("all", people), ("singers", people.Where(MemberAxesService.IsSinger).ToList()),
			("instrumentalists", people.Where(m => !MemberAxesService.IsSinger(m)).ToList()) }) {
			Row(group, set.Select(m => m.technicalSkill), "technicalSkill");
			Row(group, set.Select(m => m.instrumentalSkill), "instrumentalSkill");
			Row(group, set.Select(m => m.vocalPower), "vocalPower");
			Row(group, set.Select(m => m.vocalControl), "vocalControl");
			Row(group, set.Select(m => m.diction), "diction");
			Row(group, set.Select(m => m.ceilingInstrumental - m.instrumentalSkill), "headroomInstrumental");
			Row(group, set.Select(m => m.ceilingVocal - Math.Max(m.vocalPower, m.vocalControl)), "headroomVocal");
			Row(group, set.Select(m => m.developmentRate), "developmentRate");
			Row(group, set.Select(m => m.sightReading), "sightReading");
			Row(group, set.Select(m => m.ceilingInstrumental - m.instrumentalSkill < 0.03f ? 1f : 0f), "atCeilingInstrumental");
		}
		// Correlation of ceiling with current ability on each person's PRIMARY axis.
		float Primary(Musician m) => MemberAxesService.IsSinger(m) ? Math.Max(m.vocalPower, m.vocalControl) : m.instrumentalSkill;
		float Ceiling(Musician m) => MemberAxesService.IsSinger(m) ? m.ceilingVocal : m.ceilingInstrumental;
		float Corr(List<Musician> set, Func<Musician, float> x, Func<Musician, float> y) {
			if (set.Count < 3) return 0f;
			double mx = set.Average(m => x(m)), my = set.Average(m => y(m));
			double sxy = set.Sum(m => (x(m) - mx) * (y(m) - my)), sxx = set.Sum(m => (x(m) - mx) * (x(m) - mx)), syy = set.Sum(m => (y(m) - my) * (y(m) - my));
			return sxx <= 0 || syy <= 0 ? 0f : (float)(sxy / Math.Sqrt(sxx * syy));
		}
		w.WriteLine($"all,{people.Count},corrCeilingVsCurrent,{F(Corr(people, Primary, Ceiling))},,,,");
		w.WriteLine($"all,{people.Count},corrHeadroomVsCurrent,{F(Corr(people, Primary, m => Ceiling(m) - Primary(m)))},,,,");
		w.WriteLine($"all,{people.Count},corrTechnicalVsPrimaryAxis,{F(Corr(people, m => m.technicalSkill, Primary))},,,,");
	}
}
