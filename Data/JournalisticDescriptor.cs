using System.Collections.Generic;
using System.Linq;
using Godot;

public static class JournalisticDescriptor {
	
	// === RECORD DESCRIPTIONS ===
	
	public static string DescribeRecord(RecordRuntimeData record) {
		var descriptions = new List<string>();
		var r = record.baseRecord;
		
		if (record.currentPosition == 1) {
			descriptions.Add("The #1 record in America.");
		} else if (record.currentPosition <= 10) {
			descriptions.Add($"A top 10 smash, currently at #{record.currentPosition}.");
		} else if (record.currentPosition <= 40) {
			descriptions.Add($"A solid hit at #{record.currentPosition}.");
		} else if (record.currentPosition > 0) {
			descriptions.Add($"Bubbling under at #{record.currentPosition}.");
		}
		
		if (record.isBullet) {
			descriptions.Add("The fastest riser on the chart this week.");
		} else if (record.isAnchor) {
			descriptions.Add("Slipping fast down the charts.");
		}
		
		if (record.weeksOnChart >= 20) {
			descriptions.Add($"A perennial favorite, now in its {record.weeksOnChart}th week.");
		} else if (record.weeksOnChart >= 10) {
			descriptions.Add("Showing real staying power.");
		}
		
		if (record.peakPosition < record.currentPosition && record.peakPosition <= 10) {
			descriptions.Add($"Previously peaked at #{record.peakPosition}.");
		}
		
		if (r.hookStrength > 0.8f) {
			descriptions.Add(GetRandomElement(new[] {
				"An undeniable earworm.",
				"Try getting this one out of your head.",
				"The hook is absolutely killer.",
				"Catchier than a summer cold."
			}));
		} else if (r.hookStrength < 0.3f) {
			descriptions.Add(GetRandomElement(new[] {
				"More of a slow burn.",
				"Not exactly a sing-along.",
				"Rewards patient listening.",
				"The melody takes some getting used to."
			}));
		}
		
		if (r.productionQuality > 0.8f) {
			descriptions.Add(GetRandomElement(new[] {
				"Immaculately produced.",
				"Studio craftsmanship at its finest.",
				"Every note in its perfect place.",
				"Polished to a mirror shine."
			}));
		} else if (r.productionQuality < 0.3f) {
			descriptions.Add(GetRandomElement(new[] {
				"Rough around the edges.",
				"Has a certain raw charm.",
				"The lo-fi sound is... intentional?",
				"Sounds like it was recorded in a garage. Possibly was."
			}));
		}
		
		if (r.originality > 0.8f) {
			descriptions.Add(GetRandomElement(new[] {
				"Unlike anything else on the radio.",
				"Genuinely pushing boundaries.",
				"You've never heard anything quite like it.",
				"The sound of tomorrow, today."
			}));
		} else if (r.originality < 0.3f) {
			descriptions.Add(GetRandomElement(new[] {
				"Familiar territory, competently executed.",
				"If you liked their last one, you'll like this.",
				"Critics might call it 'safe.'",
				"Not exactly reinventing the wheel."
			}));
		}
		
		if (r.controversy > 0.6f) {
			descriptions.Add(GetRandomElement(new[] {
				"Parents hate it. Kids can't get enough.",
				"Banned in several Southern states.",
				"The lyrics have raised more than a few eyebrows.",
				"Your pastor definitely has opinions about this one."
			}));
		}
		
		if (descriptions.Count > 3) {
			descriptions = PickRandom(descriptions, 3);
		}
		
		return string.Join(" ", descriptions);
	}

	// === SALES COMMENTARY ===

	public static string GetSalesCommentary(RecordRuntimeData record) {
		int sales = record.unitsThisWeek;
		int prevSales = record.unitsPreviousWeek;
		
		if (prevSales == 0) {
			if (sales > 50000) {
				return GetRandomElement(new[] {
					"Explosive first week sales!",
					"Flying off the shelves from day one.",
					"Retailers scrambling to restock."
				});
			} else if (sales > 10000) {
				return GetRandomElement(new[] {
					"Solid opening week.",
					"A respectable debut.",
					"Off to a promising start."
				});
			} else {
				return GetRandomElement(new[] {
					"Quiet first week.",
					"Building slowly.",
					"Word hasn't spread yet."
				});
			}
		}
		
		float change = (float)sales / prevSales;
		
		if (change >= 2.0f) {
			return GetRandomElement(new[] {
				"Sales have doubled!",
				"Demand is through the roof!",
				"Can't press them fast enough!"
			});
		} else if (change >= 1.5f) {
			return GetRandomElement(new[] {
				"Sales surging.",
				"Major momentum building.",
				"Word of mouth is spreading."
			});
		} else if (change >= 1.1f) {
			return GetRandomElement(new[] {
				"Healthy sales growth.",
				"Continuing to build.",
				"Steady climb."
			});
		} else if (change >= 0.9f) {
			return GetRandomElement(new[] {
				"Holding steady.",
				"Consistent performance.",
				"Reliable sales."
			});
		} else if (change >= 0.7f) {
			return GetRandomElement(new[] {
				"Sales softening.",
				"Starting to cool off.",
				"Easing back a bit."
			});
		} else if (change >= 0.5f) {
			return GetRandomElement(new[] {
				"Sales declining.",
				"Losing steam.",
				"The rush is over."
			});
		} else {
			return GetRandomElement(new[] {
				"Sales collapsed.",
				"Dead in the water.",
				"Yesterday's news."
			});
		}
	}

	// === CONTEXT COMMENTARY ===

	public static string GetContextCommentary(RecordRuntimeData record, int year) {
		var genre = record.baseRecord.primaryGenre;
		float genreAcceptance = ChartManager.Instance?.GetEffectiveGenreAcceptance(genre) ?? 0.5f;
		
		if (genreAcceptance > 0.7f && record.currentPosition <= 20) {
			return GetRandomElement(new[] {
				$"Riding the {genre} wave.",
				$"Part of the {genre} explosion.",
				$"Right place, right time for {genre}."
			});
		}
		
		if (genreAcceptance < 0.4f && record.currentPosition <= 40) {
			return GetRandomElement(new[] {
				$"Succeeding despite {genre}'s struggles.",
				$"Bucking the trend for {genre}.",
				$"Keeping {genre} alive on the charts."
			});
		}
		
		if (record.baseRecord.primaryGenre != record.baseRecord.secondaryGenre) {
			return GetRandomElement(new[] {
				"Crossing genre boundaries.",
				"Appeals to multiple audiences.",
				"Hard to pigeonhole."
			});
		}
		
		return "";
	}

	// === QUICK ONE-LINER ===

	public static string GetQuickSummary(RecordRuntimeData record) {
		if (record.currentPosition == 1) {
			return "The #1 record in America.";
		} else if (record.currentPosition <= 10) {
			return $"Top 10 hit at #{record.currentPosition}.";
		} else if (record.currentPosition <= 40) {
			return $"Solid hit at #{record.currentPosition}.";
		} else if (record.currentPosition <= 100) {
			return $"On the chart at #{record.currentPosition}.";
		} else if (record.peakPosition > 0) {
			return $"Formerly peaked at #{record.peakPosition}.";
		} else {
			return "Not currently charting.";
		}
	}

	// === ARTIST DESCRIPTIONS ===
	
	public static string DescribeArtist(ArtistPublicProfile artist) {
		var lines = new List<string>();
		
		if (artist.isBand) {
			lines.Add($"{artist.name} are a {artist.primaryGenre.ToString().ToLower()} act out of {artist.homeCity}.");
		} else {
			lines.Add($"{artist.name} is a {artist.primaryGenre.ToString().ToLower()} singer from {artist.homeCity}.");
		}
		
		if (artist.totalCharted == 0) {
			lines.Add("They haven't charted yet, but the industry is watching.");
		} else if (artist.totalCharted == 1) {
			lines.Add("They've had one chart entry so far.");
		} else if (artist.totalCharted < 5) {
			lines.Add($"They've placed {artist.totalCharted} records on the Hot 100.");
		} else {
			lines.Add($"A proven hitmaker with {artist.totalCharted} charting records.");
		}
		
		if (artist.highestPosition == 1) {
			if (artist.numberOneHits > 1) {
				lines.Add($"They've topped the charts {artist.numberOneHits} times.");
			} else {
				lines.Add("They've reached the summit with a #1 hit.");
			}
		} else if (artist.highestPosition <= 10) {
			lines.Add($"Their highest charting single reached #{artist.highestPosition}.");
		}
		
		return string.Join(" ", lines);
	}
	
	public static string GetGenreDescription(Genre genre, int year) {
		var zeitgeist = Zeitgeist.GetForYear(year);
		float acceptance = zeitgeist.genreAcceptance.ContainsKey(genre) 
			? zeitgeist.genreAcceptance[genre] 
			: 0.5f;
		
		string status;
		if (acceptance > 0.8f) {
			status = GetRandomElement(new[] {
				"dominating the charts",
				"absolutely everywhere right now",
				"the sound of the moment",
				"what everyone's buying"
			});
		} else if (acceptance > 0.6f) {
			status = GetRandomElement(new[] {
				"having a strong year",
				"doing well commercially",
				"popular with audiences",
				"a reliable seller"
			});
		} else if (acceptance > 0.4f) {
			status = GetRandomElement(new[] {
				"holding steady",
				"maintaining its audience",
				"neither hot nor cold",
				"plugging along"
			});
		} else if (acceptance > 0.2f) {
			status = GetRandomElement(new[] {
				"struggling to find an audience",
				"falling out of favor",
				"losing ground",
				"yesterday's news"
			});
		} else {
			status = GetRandomElement(new[] {
				"practically dead commercially",
				"only for the diehards now",
				"a tough sell these days",
				"deader than disco (before disco exists)"
			});
		}
		
		return $"{genre} is {status}.";
	}
	
	// === LABEL DESCRIPTIONS ===
	
	public static string DescribeLabel(AILabel label) {
		var lines = new List<string>();
		
		lines.Add($"{label.labelName}.");
		
		// Fix: Genre[] uses .Length, and convert with LINQ .Select() instead of .ConvertAll()
		if (label.preferredGenres != null) {
			if (label.preferredGenres.Length == 1) {
				lines.Add($"Known primarily for {label.preferredGenres[0].ToString().ToLower()}.");
			} else if (label.preferredGenres.Length <= 3) {
				var genres = string.Join(" and ", label.preferredGenres.Select(g => g.ToString().ToLower()));
				lines.Add($"Specializing in {genres}.");
			} else {
				lines.Add("A diverse roster spanning multiple genres.");
			}
		}
		
		if (label.budgetLevel > 0.8f) {
			lines.Add(GetRandomElement(new[] {
				"Deep pockets and industry clout.",
				"One of the major players.",
				"They can afford the big promotional pushes.",
				"The industry giant."
			}));
		} else if (label.budgetLevel > 0.5f) {
			lines.Add(GetRandomElement(new[] {
				"A solid mid-tier operation.",
				"Respectable resources.",
				"Not the biggest, but competitive.",
				"A serious contender."
			}));
		} else {
			lines.Add(GetRandomElement(new[] {
				"A scrappy independent.",
				"Running on passion more than cash.",
				"Small but hungry.",
				"The underdog."
			}));
		}
		
		if (label.riskTolerance > 0.7f) {
			lines.Add("Known for taking chances on unconventional acts.");
		} else if (label.riskTolerance < 0.3f) {
			lines.Add("They play it safe with proven formulas.");
		}
		
		return string.Join(" ", lines);
	}
	
	// === HELPERS ===
	
	private static string GetRandomElement(string[] options) {
		// Fix: GD.RandRange instead of Random.Range
		return options[(int)GD.RandRange(0, options.Length)];
	}
	
	private static List<string> PickRandom(List<string> source, int count) {
		var result = new List<string>();
		var indices = new List<int>();
		
		for (int i = 0; i < source.Count; i++) {
			indices.Add(i);
		}
		
		// Fix: Mathf.Min works fine with using Godot at the top
		for (int i = 0; i < Mathf.Min(count, source.Count); i++) {
			int pick = (int)GD.RandRange(0, indices.Count);
			result.Add(source[indices[pick]]);
			indices.RemoveAt(pick);
		}
		
		return result;
	}
	
	// === CHART MOVEMENT COMMENTARY ===
	
	public static string GetChartMovementComment(RecordRuntimeData record) {
		if (record.lastWeekPosition == 0) {
			if (record.currentPosition <= 10) {
				return GetRandomElement(new[] {
					"Exploding onto the chart!",
					"A stunning debut!",
					"Where did THIS come from?",
					"The hottest new entry."
				});
			}
			return GetRandomElement(new[] {
				"New this week.",
				"Just entering the chart.",
				"A fresh face.",
				"Making its debut."
			});
		}
		
		int movement = record.lastWeekPosition - record.currentPosition;
		
		if (movement >= 20) {
			return GetRandomElement(new[] {
				"Absolutely rocketing up the chart!",
				"Unstoppable momentum!",
				"On a tear!",
				"Nothing's slowing this one down!"
			});
		} else if (movement >= 10) {
			return GetRandomElement(new[] {
				"Flying up the chart.",
				"Major upward movement.",
				"Gaining serious steam.",
				"The momentum is real."
			});
		} else if (movement >= 3) {
			return GetRandomElement(new[] {
				"Climbing nicely.",
				"Solid gains this week.",
				"Moving on up.",
				"Healthy growth."
			});
		} else if (movement > 0) {
			return GetRandomElement(new[] {
				"Inching upward.",
				"Slight improvement.",
				"Modest gains.",
				"A small step up."
			});
		} else if (movement == 0) {
			return GetRandomElement(new[] {
				"Holding steady.",
				"No change this week.",
				"Staying put.",
				"Maintaining position."
			});
		} else if (movement >= -3) {
			return GetRandomElement(new[] {
				"Slight dip.",
				"Minor slide.",
				"Losing a little ground.",
				"Small pullback."
			});
		} else if (movement >= -10) {
			return GetRandomElement(new[] {
				"Slipping.",
				"On the way down.",
				"Losing altitude.",
				"Fading."
			});
		} else {
			return GetRandomElement(new[] {
				"In freefall!",
				"Plummeting!",
				"The bottom is falling out!",
				"Last week's news."
			});
		}
	}
	
	public static string GetRegionalPerformanceHint(RecordRuntimeData record, List<MarketRegion> regions) {
		string strongestRegion = null;
		string weakestRegion = null;
		float highestAwareness = 0f;
		float lowestAwareness = 1f;
		
		foreach (var region in regions) {
			if (record.regionalData.TryGetValue(region.regionId, out var data)) {
				if (data.awareness > highestAwareness) {
					highestAwareness = data.awareness;
					strongestRegion = region.regionName;
				}
				if (data.awareness < lowestAwareness && data.awareness > 0) {
					lowestAwareness = data.awareness;
					weakestRegion = region.regionName;
				}
			}
		}
		
		if (strongestRegion != null && highestAwareness > 0.6f) {
			return $"Particularly strong in {strongestRegion}.";
		}
		
		if (weakestRegion != null && lowestAwareness < 0.2f) {
			return $"Still hasn't caught on in {weakestRegion}.";
		}
		
		return "Steady performance across all regions.";
	}
}
