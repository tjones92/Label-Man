import fs from "node:fs";
import path from "node:path";
import readline from "node:readline";

const logDirectory = path.resolve("SimLogs");

function splitCsv(line) {
  const values = [];
  let value = "";
  let quoted = false;
  for (let i = 0; i < line.length; i++) {
    const character = line[i];
    if (character === '"') {
      if (quoted && line[i + 1] === '"') {
        value += '"';
        i++;
      } else quoted = !quoted;
    } else if (character === "," && !quoted) {
      values.push(value);
      value = "";
    } else value += character;
  }
  values.push(value);
  return values;
}

async function rows(file, visit) {
  const input = fs.createReadStream(file);
  const lines = readline.createInterface({ input, crlfDelay: Infinity });
  let headers;
  for await (const line of lines) {
    if (!headers) {
      headers = splitCsv(line);
      continue;
    }
    if (!line) continue;
    const values = splitCsv(line);
    const row = Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""]));
    visit(row);
  }
}

const number = value => value === "" || value == null ? null : Number(value);
const mean = values => values.length ? values.reduce((sum, value) => sum + value, 0) / values.length : null;
function median(values) {
  if (!values.length) return null;
  const sorted = [...values].sort((a, b) => a - b);
  const middle = (sorted.length - 1) / 2;
  return (sorted[Math.floor(middle)] + sorted[Math.ceil(middle)]) / 2;
}
function percentile(values, fraction) {
  if (!values.length) return null;
  const sorted = [...values].sort((a, b) => a - b);
  const index = (sorted.length - 1) * fraction;
  const lower = Math.floor(index);
  const upper = Math.ceil(index);
  return sorted[lower] + (sorted[upper] - sorted[lower]) * (index - lower);
}
function correlation(pairs) {
  if (pairs.length < 2) return null;
  const meanX = mean(pairs.map(pair => pair[0]));
  const meanY = mean(pairs.map(pair => pair[1]));
  let covariance = 0;
  let varianceX = 0;
  let varianceY = 0;
  for (const [x, y] of pairs) {
    const dx = x - meanX;
    const dy = y - meanY;
    covariance += dx * dy;
    varianceX += dx * dx;
    varianceY += dy * dy;
  }
  return varianceX > 0 && varianceY > 0 ? covariance / Math.sqrt(varianceX * varianceY) : null;
}
function yearObject(map, year, create) {
  if (!map.has(year)) map.set(year, create());
  return map.get(year);
}

function summarize(values) {
  const present = values.filter(value => value != null && Number.isFinite(value));
  return { n: present.length, mean: mean(present), median: median(present), min: present.length ? Math.min(...present) : null,
    max: present.length ? Math.max(...present) : null };
}
function weightedMean(pairs) {
  const present = pairs.filter(pair => pair.value != null && Number.isFinite(pair.value) &&
    pair.weight != null && Number.isFinite(pair.weight) && pair.weight > 0);
  const weight = present.reduce((sum, pair) => sum + pair.weight, 0);
  return weight ? present.reduce((sum, pair) => sum + pair.value * pair.weight, 0) / weight : null;
}

function pivotStage(genre) {
  if (["Jazz", "EasyListening", "Folk", "TraditionalPop", "BossaNova"].includes(genre)) return "Adult";
  if (["Country", "Blues"].includes(genre)) return "CountryBlues";
  if (["RockAndRoll", "RnB", "Soul"].includes(genre)) return "RockRnBSoul";
  return null;
}

async function analyze(run) {
  const prefix = path.join(logDirectory, run);
  const yearByWeek = new Map();
  const weeks = new Map();
  await rows(`${prefix}-weeks.csv`, row => {
    const year = number(row.year);
    const week = number(row.week);
    yearByWeek.set(week, year);
    const annual = yearObject(weeks, year, () => ({ entries: 0, units: 0 }));
    annual.entries += number(row.newEntriesTop100) ?? 0;
    annual.units += number(row.totalMarketUnits) ?? 0;
  });

  const releases = new Map();
  await rows(`${prefix}-release-capacity.csv`, row => {
    const year = number(row.year);
    releases.set(year, (releases.get(year) ?? 0) + (number(row.successfulReleases) ?? 0));
  });

  const records = new Map();
  await rows(`${prefix}-records.csv`, row => {
    const position = number(row.currentPosition);
    if (!(position > 0)) return;
    const year = number(row.year);
    const byId = yearObject(records, year, () => new Map());
    const id = row.recordId;
    const existing = byId.get(id);
    if (!existing) byId.set(id, { quality: number(row.quality), peak: position });
    else existing.peak = Math.min(existing.peak, position);
  });

  const closed = new Map();
  await rows(`${prefix}-lifecycles.csv`, row => {
    const peak = number(row.peakPosition);
    if (!(peak > 0 && peak <= 40)) return;
    const year = yearByWeek.get(number(row.week));
    yearObject(closed, year, () => []).push(number(row.weeksOnChart));
  });

  const rollups = new Map();
  await rows(`${prefix}-decade-annual-rollup.csv`, row => rollups.set(number(row.year), row));

  const strategyByRecord = new Map();
  const confidence = new Map();
  const standaloneAnnual = new Map();
  await rows(`${prefix}-release-strategy.csv`, row => {
    strategyByRecord.set(row.recordId, row);
    const year = number(row.year);
    const tier = row.tier;
    const key = `${year}|${tier}`;
    if (!confidence.has(key)) confidence.set(key, { year, tier, single: [], album: [] });
    const bucket = confidence.get(key);
    bucket.single.push(number(row.confidenceSingle));
    bucket.album.push(number(row.confidenceAlbum));
    if (row.strategy === "AlbumStandalone") {
      const annual = yearObject(standaloneAnnual, year, () => ({ count: 0, highStature: 0, demand: [] }));
      annual.count++;
      if (["Established", "Star", "Superstar"].includes(row.careerState)) annual.highStature++;
      annual.demand.push(number(row.albumDemandFactor));
    }
  });

  const prior = new Map();
  const affinityFactors = new Map();
  const hitCohorts = new Map();
  const pivotBuckets = new Map();
  await rows(`${prefix}-a3-economic-decisions.csv`, row => {
    const year = number(row.year);
    const genreGroup = row.genreGroup;
    const stage = pivotStage(row.genre);
    if (stage) {
      const pivot = yearObject(pivotBuckets, `${stage}|${year}`, () => ({ stage, year, decisions: 0, albums: 0 }));
      pivot.decisions++;
      if (row.chosenFormat === "Album") pivot.albums++;
    }
    const priorKey = `${year}|${genreGroup}`;
    if (row.chosenFormat === "Album") {
      if (!prior.has(priorKey)) prior.set(priorKey, { year, genreGroup, affinity: [], hit: [] });
      prior.get(priorKey).affinity.push(number(row.affinityUnits));
      prior.get(priorKey).hit.push(number(row.weightedHitUnits));
    }
    if (genreGroup !== "Youth" || ![1960, 1961].includes(year)) return;
    if (row.chosenFormat === "Album") {
      if (!affinityFactors.has(year)) affinityFactors.set(year, {
        qualityEstimate: [], statureMultiplier: [], reachFactor: [], albumDemandFactor: [], affinityUnits: [],
        transitionDecisions: 0
      });
      const factors = affinityFactors.get(year);
      for (const field of ["qualityEstimate", "statureMultiplier", "reachFactor", "albumDemandFactor", "affinityUnits"])
        factors[field].push(number(row[field]));
      if (row.careerStateTransitionOccurredThisYear === "true") factors.transitionDecisions++;
    }
    if (row.chosenFormat === "Album") {
      const cohort = row.hitInventoryCohort;
      const cohortKey = `${year}|${cohort}`;
      if (!hitCohorts.has(cohortKey)) hitCohorts.set(cohortKey, { year, cohort, hitScore: [] });
      hitCohorts.get(cohortKey).hitScore.push(number(row.hitScore));
    }
  });

  const compilationIds = new Set();
  const albumReleases = [];
  const adultAlbumFormats = new Map();
  const adultCompilationIds = new Set();
  const compositionAnnual = new Map();
  await rows(`${prefix}-album-composition.csv`, row => {
    if (row.albumFormat === "Compilation") compilationIds.add(row.recordId);
    const release = { id: row.recordId, week: number(row.week), year: number(row.year), genre: row.genre,
      format: row.albumFormat };
    albumReleases.push(release);
    const composition = yearObject(compositionAnnual, release.year,
      () => ({ albums: 0, concepts: 0, cohesion: [], youthAlbums: 0, youthComps: 0 }));
    composition.albums++;
    if (release.format === "Concept") composition.concepts++;
    composition.cohesion.push(number(row.thematicCohesion));
    if (["RockAndRoll", "TeenPop", "RnB", "DooWop", "GirlGroup"].includes(release.genre)) {
      composition.youthAlbums++;
      if (release.format === "Compilation") composition.youthComps++;
    }
    const adult = ["Jazz", "EasyListening", "Folk", "TraditionalPop", "BossaNova", "Country"].includes(row.genre);
    if (!adult) return;
    if (row.albumFormat === "Compilation") adultCompilationIds.add(row.recordId);
    const key = `${release.year}|${release.format}`;
    adultAlbumFormats.set(key, (adultAlbumFormats.get(key) ?? 0) + 1);
  });
  const sourceHitAges = new Map();
  const adultSourceHitAges = new Map();
  await rows(`${prefix}-album-track-links.csv`, row => {
    if (!compilationIds.has(row.albumRecordId)) return;
    const year = number(row.year);
    if (!sourceHitAges.has(year)) sourceHitAges.set(year, []);
    sourceHitAges.get(year).push({ value: number(row.sourceHitAgeWeeks), weight: number(row.freshnessApplied) });
    if (adultCompilationIds.has(row.albumRecordId)) {
      if (!adultSourceHitAges.has(year)) adultSourceHitAges.set(year, []);
      adultSourceHitAges.get(year).push({ value: number(row.sourceHitAgeWeeks), weight: number(row.freshnessApplied) });
    }
  });

  const albumChartAnnual = new Map();
  await rows(`${prefix}-album-chart.csv`, row => {
    const year = number(row.year);
    const chart = yearObject(albumChartAnnual, year, () => ({ rows: 0, nonAdultRows: 0 }));
    chart.rows++;
    if (!["Jazz", "EasyListening", "Folk", "TraditionalPop", "BossaNova", "Country"].includes(row.genre))
      chart.nonAdultRows++;
  });

  const projects = { total: 0, cancelled: 0, transferred: 0 };
  await rows(`${prefix}-album-projects.csv`, row => {
    projects.total++;
    if (row.terminalState === "Cancelled") projects.cancelled++;
    if (row.wasTransferred === "true") projects.transferred++;
  });

  const annual = {};
  for (const year of [...weeks.keys()].sort()) {
    const charting = [...(records.get(year)?.values() ?? [])];
    const rollup = rollups.get(year) ?? {};
    annual[year] = {
      units: number(rollup.singleUnits) + (number(rollup.albumUnits) ?? 0),
      singleUnits: number(rollup.singleUnits),
      albumUnits: number(rollup.albumUnits),
      pearson: charting.length ? correlation(charting.map(record => [record.quality, 101 - record.peak])) : number(rollup.singlePearson),
      pearsonN: charting.length || (number(rollup.singlePearsonN) ?? 0),
      closedTop40Median: (closed.get(year)?.length ?? 0) ? median(closed.get(year)) : number(rollup.closedTop40Median),
      closedTop40N: (closed.get(year)?.length ?? 0) || (number(rollup.closedTop40N) ?? 0),
      competitionRatio: weeks.get(year).entries ? (releases.get(year) ?? 0) / weeks.get(year).entries : null,
      releases: releases.get(year) ?? 0,
      entries: weeks.get(year).entries
    };
    for (const [key, value] of Object.entries(rollup)) {
      if (!(key in annual[year]) && key !== "seed" && key !== "year") annual[year][key] = number(value);
    }
  }
  const phase0 = {
    priorDecomposition: [...prior.values()].map(bucket => {
      const affinity = summarize(bucket.affinity);
      const weightedHit = summarize(bucket.hit);
      const total = (affinity.mean ?? 0) + (weightedHit.mean ?? 0);
      return { year: bucket.year, genreGroup: bucket.genreGroup, decisions: affinity.n,
        meanAffinityUnits: affinity.mean, meanWeightedHitUnits: weightedHit.mean,
        weightedHitContributionShare: total ? (weightedHit.mean ?? 0) / total : null };
    }).sort((a, b) => a.year - b.year || a.genreGroup.localeCompare(b.genreGroup)),
    memoryBlend: [...confidence.values()].flatMap(bucket => [
      { year: bucket.year, tier: bucket.tier, format: "Single", ...summarize(bucket.single),
        memoryLedShare: bucket.single.filter(value => value > 0.5).length / bucket.single.length },
      { year: bucket.year, tier: bucket.tier, format: "Album", ...summarize(bucket.album),
        memoryLedShare: bucket.album.filter(value => value > 0.5).length / bucket.album.length }
    ]).sort((a, b) => a.year - b.year || a.tier.localeCompare(b.tier) || a.format.localeCompare(b.format)),
    catalogGrossByAge: [...rollups.entries()].map(([year, row]) => ({ year,
      over26WeeksShare: number(row.albumGrossOver26WeeksShare),
      over52WeeksShare: number(row.albumGrossOver52WeeksShare) })).sort((a, b) => a.year - b.year),
    compSourceHitAge: [...sourceHitAges.entries()].map(([year, pairs]) => ({ year,
      ...summarize(pairs.map(pair => pair.value)), freshnessWeightedMean: weightedMean(pairs) }))
      .sort((a, b) => a.year - b.year)
  };
  const factorNames = ["qualityEstimate", "statureMultiplier", "reachFactor", "albumDemandFactor"];
  const factorMeans = Object.fromEntries([...affinityFactors.entries()].map(([year, bucket]) => [year,
    Object.fromEntries([...factorNames, "affinityUnits"].map(field => [field, summarize(bucket[field]).mean]))]));
  const baseline = factorMeans[1960];
  const comparison = factorMeans[1961];
  const scalarCandidates = [];
  for (const bucket of affinityFactors.values()) {
    for (let i = 0; i < bucket.affinityUnits.length; i++) {
      const product = factorNames.reduce((value, field) => value * bucket[field][i], 1);
      if (product > 0 && bucket.affinityUnits[i] != null) scalarCandidates.push(bucket.affinityUnits[i] / product);
    }
  }
  const priorUnitScalarAlbum = median(scalarCandidates);
  const baseProduct = baseline ? factorNames.reduce((value, field) => value * baseline[field], priorUnitScalarAlbum) : null;
  const oneAtATime = baseline && comparison ? factorNames.map(field => ({ factor: field,
    contribution: priorUnitScalarAlbum * (comparison[field] - baseline[field]) *
      factorNames.filter(other => other !== field).reduce((value, other) => value * baseline[other], 1)
  })) : [];
  const actualAffinityGrowth = baseline && comparison ? comparison.affinityUnits - baseline.affinityUnits : null;
  const phase0b = {
    affinityAttribution: {
      factorMeans,
      priorUnitScalarAlbum,
      baselineProductOfMeans: baseProduct,
      comparisonProductOfMeans: comparison ? factorNames.reduce((value, field) => value * comparison[field], priorUnitScalarAlbum) : null,
      actualMeanAffinityGrowth: actualAffinityGrowth,
      oneAtATime: oneAtATime.map(item => ({ ...item,
        shareOfActualGrowth: actualAffinityGrowth ? item.contribution / actualAffinityGrowth : null })),
      interactionAndCovarianceResidual: actualAffinityGrowth == null ? null : actualAffinityGrowth -
        oneAtATime.reduce((sum, item) => sum + item.contribution, 0),
      transitionDecisionShare: Object.fromEntries([...affinityFactors.entries()].map(([year, bucket]) =>
        [year, bucket.affinityUnits.length ? bucket.transitionDecisions / bucket.affinityUnits.length : null]))
    },
    hitInventoryCohorts: [...hitCohorts.values()].map(bucket => ({ year: bucket.year, cohort: bucket.cohort,
      decisions: bucket.hitScore.length, meanHitScore: mean(bucket.hitScore.filter(Number.isFinite)) }))
      .sort((a, b) => a.year - b.year || a.cohort.localeCompare(b.cohort)),
    catalogEligibility: [...weeks.entries()].sort(([a], [b]) => a - b).map(([year]) => {
      const endWeek = Math.max(...[...yearByWeek.entries()].filter(([, value]) => value === year).map(([week]) => week));
      const cumulative = albumReleases.filter(release => release.week <= endWeek);
      const ages = cumulative.map(release => endWeek - release.week);
      const eligibleAges = ages.filter(age => age > 52);
      return { year, annualAlbumReleases: albumReleases.filter(release => release.year === year).length,
        cumulativeAlbums: cumulative.length, eligibleOver52Weeks: eligibleAges.length,
        eligibleCountShare: cumulative.length ? eligibleAges.length / cumulative.length : null,
        ageMedian: median(ages), ageP90: percentile(ages, 0.9), eligibleAgeMedian: median(eligibleAges),
        over52GrossShare: number(rollups.get(year)?.albumGrossOver52WeeksShare) };
    }),
    adultFormatCliff: [...weeks.keys()].sort().map(year => {
      const compilations = adultAlbumFormats.get(`${year}|Compilation`) ?? 0;
      const standards = adultAlbumFormats.get(`${year}|Standard`) ?? 0;
      const agePairs = adultSourceHitAges.get(year) ?? [];
      return { year, compilations, standards, compilationShare: compilations + standards ? compilations / (compilations + standards) : null,
        sourceHitAge: { ...summarize(agePairs.map(pair => pair.value)), freshnessWeightedMean: weightedMean(agePairs) } };
    })
  };
  const pivotYears = {};
  for (const stage of ["Adult", "CountryBlues", "RockRnBSoul"]) {
    const buckets = [...pivotBuckets.values()].filter(bucket => bucket.stage === stage).sort((a, b) => a.year - b.year);
    pivotYears[stage] = buckets.find(bucket => bucket.albums / bucket.decisions >= 0.5)?.year ?? null;
  }
  const phase3 = {
    pivotYears,
    pivotAnnual: [...pivotBuckets.values()].map(bucket => ({ ...bucket, albumShare: bucket.albums / bucket.decisions }))
      .sort((a, b) => a.year - b.year || a.stage.localeCompare(b.stage)),
    compositionAnnual: [...compositionAnnual.entries()].map(([year, bucket]) => ({ year,
      albums: bucket.albums, concepts: bucket.concepts, meanCohesion: mean(bucket.cohesion),
      youthAlbums: bucket.youthAlbums, youthComps: bucket.youthComps,
      youthCompShare: bucket.youthAlbums ? bucket.youthComps / bucket.youthAlbums : null }))
      .sort((a, b) => a.year - b.year),
    albumChartAnnual: [...albumChartAnnual.entries()].map(([year, bucket]) => ({ year, ...bucket,
      nonAdultShare: bucket.rows ? bucket.nonAdultRows / bucket.rows : null })).sort((a, b) => a.year - b.year),
    standaloneAnnual: [...standaloneAnnual.entries()].map(([year, bucket]) => ({ year, count: bucket.count,
      highStatureShare: bucket.count ? bucket.highStature / bucket.count : null, meanAlbumDemandFactor: mean(bucket.demand) }))
      .sort((a, b) => a.year - b.year),
    projects
  };
  return { run, annual, phase0, phase0b, phase3 };
}

const compact = process.argv.includes("--compact");
const phase0bOnly = process.argv.includes("--phase0b");
const runs = process.argv.slice(2).filter(argument => !["--compact", "--phase0b"].includes(argument));
if (!runs.length) throw new Error("Pass one or more SimLogs run prefixes.");
const results = [];
for (const run of runs) results.push(await analyze(run));
if (phase0bOnly) {
  process.stdout.write(`${JSON.stringify(results.map(result => ({ run: result.run, phase0b: result.phase0b })), null, 2)}\n`);
} else if (compact) {
  const compactResults = results.flatMap(result => Object.entries(result.annual).map(([year, annual]) => ({
    run: result.run,
    year: Number(year),
    units: annual.units,
    pearson: annual.pearson,
    pearsonN: annual.pearsonN,
    closedTop40Median: annual.closedTop40Median,
    closedTop40N: annual.closedTop40N,
    competitionRatio: annual.competitionRatio
  })));
  process.stdout.write(`${JSON.stringify(compactResults, null, 2)}\n`);
} else process.stdout.write(`${JSON.stringify(results, null, 2)}\n`);
