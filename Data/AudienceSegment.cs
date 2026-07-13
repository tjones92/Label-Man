using System;
using System.Collections.Generic;
using Godot;

public enum AudienceSegment { MainstreamAM, Youth, AdultMOR, UrbanRnB, CountryWestern, CollegeFolk, UndergroundFM, JazzHiFiClassical, GospelChurch, RegionalLatin, FamilyChildrens }

public sealed class SegmentCapacityModel {
	public static readonly IReadOnlyList<AudienceSegment> All = Enum.GetValues<AudienceSegment>();
	public IReadOnlyDictionary<AudienceSegment, float> Shares { get; }
	private SegmentCapacityModel(Dictionary<AudienceSegment, float> shares) => Shares = shares;

	public static SegmentCapacityModel Create(MarketRegion region, int year) {
		float collegesPerMillion = region.population > 0f ? region.collegeCount / region.population : 0f;
		float fm = region.media?.hasFMUnderground == true && year >= 1967 ? Mathf.Clamp((year - 1966) * .25f, 0f, 1f) : 0f;
		float church = region.churchNetworkStrength;
		var raw = new Dictionary<AudienceSegment, float> {
			[AudienceSegment.MainstreamAM] = .38f + (region.media?.radioReach ?? 0f) * .10f,
			[AudienceSegment.Youth] = .10f + region.youthPercentage * .35f,
			[AudienceSegment.AdultMOR] = .14f + region.averageIncome * .04f,
			[AudienceSegment.UrbanRnB] = .03f + region.blackPopulation * (.12f + region.currentIntegration * .08f),
			[AudienceSegment.CountryWestern] = .05f + (1f - region.urbanization) * .16f + (region.media?.hasCountryStations == true ? .06f : 0f),
			[AudienceSegment.CollegeFolk] = .02f + Mathf.Clamp(collegesPerMillion / 25f, 0f, .12f),
			[AudienceSegment.UndergroundFM] = fm * (.02f + region.culturalProgressivism * .08f),
			[AudienceSegment.JazzHiFiClassical] = .04f + region.urbanization * .06f,
			[AudienceSegment.GospelChurch] = .02f + church * .12f,
			[AudienceSegment.RegionalLatin] = region.regionId == "southwest" ? .12f : region.regionId == "eastcoast" ? .06f : .015f,
			[AudienceSegment.FamilyChildrens] = .04f + (1f - region.youthPercentage) * .03f
		};
		float total = 0f; foreach (float value in raw.Values) total += value;
		foreach (AudienceSegment segment in All) raw[segment] /= total;
		return new SegmentCapacityModel(raw);
	}
}
