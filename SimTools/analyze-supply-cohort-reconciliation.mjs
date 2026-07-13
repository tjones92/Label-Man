import fs from "node:fs";
import path from "node:path";
import readline from "node:readline";

const logDirectory = path.resolve("SimLogs");

function splitCsv(line) {
  const values = []; let value = ""; let quoted = false;
  for (let i = 0; i < line.length; i++) {
    const ch = line[i];
    if (ch === '"') {
      if (quoted && line[i + 1] === '"') { value += ch; i++; } else quoted = !quoted;
    } else if (ch === "," && !quoted) { values.push(value); value = ""; } else value += ch;
  }
  values.push(value); return values;
}

async function csvRows(file) {
  const input = fs.createReadStream(file);
  const lines = readline.createInterface({ input, crlfDelay: Infinity });
  let headers = null; const output = [];
  for await (const line of lines) {
    if (!headers) { headers = splitCsv(line); continue; }
    if (!line) continue;
    const values = splitCsv(line);
    output.push(Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""])));
  }
  return output;
}

function prefix(run) { return path.isAbsolute(run) ? run : path.join(logDirectory, run); }
function number(value) { const result = Number(value); return Number.isFinite(result) ? result : 0; }
function sum(values) { return values.reduce((total, value) => total + value, 0); }
function ratio(numerator, denominator) { return denominator ? numerator / denominator : null; }

function mapLegacy(genre, year) {
  switch (genre) {
    case "Psychedelic": return "PsychedelicRock";
    case "BritishInvasion": return "BritishBeat";
    case "Motown": return "Soul";
    case "GirlGroup": return "TeenPop";
    case "Skiffle": return "Folk";
    case "SkaRocksteady": return year <= 1965 ? "Ska" : year <= 1967 ? "Rocksteady" : "Reggae";
    default: return genre;
  }
}

// Mirrors GenreMigration.Canonicalize rather than the simpler MapLegacy helper:
// Girl Group's destination depends on its unmapped secondary identity.
function canonicalGenre(primary, secondary, year) {
  if (primary === "GirlGroup") {
    if (!secondary) throw new Error(`GirlGroup decision in ${year} has no raw secondary genre; full migration is impossible.`);
    return secondary === "Soul" || secondary === "RnB" ? "Soul" : "TeenPop";
  }
  return mapLegacy(primary, year);
}

function reachBucket(value) {
  if (value < .75) return "low(<0.75)";
  if (value < 1.25) return "mid(0.75-1.25)";
  return "high(>=1.25)";
}

function get(map, key, create) { if (!map.has(key)) map.set(key, create()); return map.get(key); }

async function load(run) {
  const base = prefix(run);
  const detailsPath = `${base}-format-decision-cohort-details.csv`;
  const decisionsPath = `${base}-fork-ratios.csv`;
  const explanationPath = `${base}-format-decision-explanation.csv`;
  if (!fs.existsSync(detailsPath) || !fs.existsSync(decisionsPath) || !fs.existsSync(explanationPath))
    throw new Error(`Expected cohort details, fork ratios, and decision explanations for ${run}.`);
  const [detailRows, decisionRows, explanationRows] = await Promise.all([csvRows(detailsPath), csvRows(decisionsPath), csvRows(explanationPath)]);
  for (const row of [...detailRows, ...decisionRows]) {
    if (!("rawSecondaryGenre" in row)) throw new Error(`${run} predates raw-secondary telemetry; refusing partial legacy canonicalization.`);
  }
  const decisions = new Map();
  const girlGroupSecondaries = new Map();
  const explanations = new Map(explanationRows.map(row => [row.recordId, row]));
  for (const row of decisionRows) {
    const year = number(row.year); const primary = row.genre; const secondary = row.rawSecondaryGenre;
    if (primary === "GirlGroup") {
      const bucket = get(girlGroupSecondaries, secondary, () => ({ rawSecondaryGenre: secondary, decisions: 0, albums: 0,
        canonicalGenre: canonicalGenre(primary, secondary, year) }));
      bucket.decisions++; bucket.albums += row.chosenFormat === "Album" ? 1 : 0;
    }
    const explanation = explanations.get(row.recordId);
    if (!explanation) throw new Error(`Missing decision explanation for ${run} record ${row.recordId}.`);
    decisions.set(row.recordId, {
      primary, secondary, canonical: canonicalGenre(primary, secondary, year), careerBand: row.careerBand,
      qualityQuartile: row.qualityQuartile, reach: number(row.reachFactor), format: row.chosenFormat,
      deterministicAlbum: number(row.priorAlbumNet) > number(row.priorSingleNet),
      memoryAlbum: number(explanation.albumMemoryBlend) > number(explanation.singleMemoryBlend)
    });
  }
  const cohorts = new Map();
  for (const row of detailRows) {
    const year = number(row.year); const canonical = canonicalGenre(row.rawPrimaryGenre, row.rawSecondaryGenre, year);
    const key = `${canonical}|${row.format}`;
    const bucket = get(cohorts, key, () => ({ canonicalGenre: canonical, format: row.format, projects: 0, units: 0 }));
    bucket.projects++; bucket.units += number(row.realizedUnits);
  }
  return { cohorts, decisions, girlGroupSecondaries: [...girlGroupSecondaries.values()].sort((a, b) => b.decisions - a.decisions || a.rawSecondaryGenre.localeCompare(b.rawSecondaryGenre)) };
}

function cohortDelta(enabled, control) {
  const keys = new Set([...enabled.keys(), ...control.keys()]);
  return [...keys].map(key => {
    const e = enabled.get(key) ?? { canonicalGenre: key.split("|")[0], format: key.split("|")[1], projects: 0, units: 0 };
    const c = control.get(key) ?? { canonicalGenre: e.canonicalGenre, format: e.format, projects: 0, units: 0 };
    const enabledUPP = ratio(e.units, e.projects) ?? 0; const controlUPP = ratio(c.units, c.projects) ?? 0;
    return {
      canonicalGenre: e.canonicalGenre, format: e.format,
      enabledProjects: e.projects, controlProjects: c.projects,
      enabledUnits: e.units, controlUnits: c.units, unitDelta: e.units - c.units,
      enabledUnitsPerProject: enabledUPP, controlUnitsPerProject: controlUPP,
      projectCountMixEffect: (e.projects - c.projects) * controlUPP,
      unitsPerProjectEffect: e.projects * (enabledUPP - controlUPP)
    };
  }).sort((a, b) => a.canonicalGenre.localeCompare(b.canonicalGenre) || a.format.localeCompare(b.format));
}

function summarizePopulation(decisions, selector) {
  const result = new Map();
  for (const decision of decisions.values()) {
    const key = selector(decision);
    const bucket = get(result, key, () => ({ decisions: 0, albums: 0, reachTotal: 0 }));
    bucket.decisions++; bucket.albums += decision.format === "Album" ? 1 : 0; bucket.reachTotal += decision.reach;
  }
  return result;
}

function populationDelta(enabled, control, selector) {
  const e = summarizePopulation(enabled, selector); const c = summarizePopulation(control, selector);
  const keys = new Set([...e.keys(), ...c.keys()]);
  return [...keys].map(key => {
    const a = e.get(key) ?? { decisions: 0, albums: 0, reachTotal: 0 };
    const b = c.get(key) ?? { decisions: 0, albums: 0, reachTotal: 0 };
    return { cohort: key, enabledDecisions: a.decisions, controlDecisions: b.decisions,
      decisionDelta: a.decisions - b.decisions, enabledAlbumShare: ratio(a.albums, a.decisions),
      controlAlbumShare: ratio(b.albums, b.decisions), enabledMeanReach: ratio(a.reachTotal, a.decisions),
      controlMeanReach: ratio(b.reachTotal, b.decisions) };
  }).sort((a, b) => a.cohort.localeCompare(b.cohort));
}

function decisionTotal(decisions) {
  const rows = [...decisions.values()]; const albums = rows.filter(row => row.format === "Album").length;
  return { decisions: rows.length, albums, albumShare: ratio(albums, rows.length) };
}

function aggregateEffects(rows, selector) {
  const groups = new Map();
  for (const row of rows) {
    const key = selector(row); const bucket = get(groups, key, () => ({ cohort: key, unitDelta: 0, projectCountMixEffect: 0, unitsPerProjectEffect: 0 }));
    bucket.unitDelta += row.unitDelta; bucket.projectCountMixEffect += row.projectCountMixEffect; bucket.unitsPerProjectEffect += row.unitsPerProjectEffect;
  }
  return [...groups.values()].sort((a, b) => Math.abs(b.unitDelta) - Math.abs(a.unitDelta) || a.cohort.localeCompare(b.cohort));
}

function albumDecisionDecomposition(enabled, control) {
  const byGenre = decisions => {
    const result = new Map();
    for (const decision of decisions.values()) {
      const bucket = get(result, decision.canonical, () => ({ canonicalGenre: decision.canonical, decisions: 0, albums: 0 }));
      bucket.decisions++; bucket.albums += decision.format === "Album" ? 1 : 0;
    }
    return result;
  };
  const e = byGenre(enabled); const c = byGenre(control); const keys = new Set([...e.keys(), ...c.keys()]);
  const rows = [...keys].map(key => {
    const enabledBucket = e.get(key) ?? { canonicalGenre: key, decisions: 0, albums: 0 };
    const controlBucket = c.get(key) ?? { canonicalGenre: key, decisions: 0, albums: 0 };
    const enabledShare = ratio(enabledBucket.albums, enabledBucket.decisions) ?? 0;
    const controlShare = ratio(controlBucket.albums, controlBucket.decisions) ?? 0;
    return {
      canonicalGenre: key, enabledDecisions: enabledBucket.decisions, controlDecisions: controlBucket.decisions,
      enabledAlbums: enabledBucket.albums, controlAlbums: controlBucket.albums, albumDecisionDelta: enabledBucket.albums - controlBucket.albums,
      enabledAlbumShare: enabledShare, controlAlbumShare: controlShare,
      canonicalGenreCompositionEffect: (enabledBucket.decisions - controlBucket.decisions) * controlShare,
      withinGenreAlbumChoiceEffect: enabledBucket.decisions * (enabledShare - controlShare)
    };
  }).sort((a, b) => Math.abs(b.albumDecisionDelta) - Math.abs(a.albumDecisionDelta) || a.canonicalGenre.localeCompare(b.canonicalGenre));
  return {
    albumDecisionDelta: sum(rows.map(row => row.albumDecisionDelta)),
    canonicalGenreCompositionEffect: sum(rows.map(row => row.canonicalGenreCompositionEffect)),
    withinGenreAlbumChoiceEffect: sum(rows.map(row => row.withinGenreAlbumChoiceEffect)),
    byCanonicalGenre: rows
  };
}

function fineCohortKey(decision) {
  return `${decision.primary}|${decision.canonical}|${decision.careerBand}|${decision.qualityQuartile}|${reachBucket(decision.reach)}`;
}

function fineCohorts(decisions) {
  const cohorts = new Map();
  for (const decision of decisions.values()) {
    const key = fineCohortKey(decision);
    const bucket = get(cohorts, key, () => ({ cohort: key, primaryGenre: decision.primary, canonicalGenre: decision.canonical,
      careerBand: decision.careerBand, qualityQuartile: decision.qualityQuartile, reachBucket: reachBucket(decision.reach), decisions: 0, albums: 0 }));
    bucket.decisions++; bucket.albums += decision.format === "Album" ? 1 : 0;
  }
  return cohorts;
}

function stageSummary(decisions) {
  const rows = [...decisions];
  const finalAlbum = row => row.format === "Album";
  return {
    decisions: rows.length,
    deterministicPriorAlbumShare: ratio(rows.filter(row => row.deterministicAlbum).length, rows.length),
    memoryBlendedAlbumShare: ratio(rows.filter(row => row.memoryAlbum).length, rows.length),
    finalAlbumShare: ratio(rows.filter(finalAlbum).length, rows.length),
    priorToMemoryFlips: rows.filter(row => row.deterministicAlbum !== row.memoryAlbum).length,
    memoryToFinalFlips: rows.filter(row => row.memoryAlbum !== finalAlbum(row)).length,
    priorToFinalFlips: rows.filter(row => row.deterministicAlbum !== finalAlbum(row)).length
  };
}

function commonSupportAnalysis(enabled, control) {
  const enabledCohorts = fineCohorts(enabled); const controlCohorts = fineCohorts(control);
  const keys = new Set([...enabledCohorts.keys(), ...controlCohorts.keys()]);
  const supportedKeys = new Set([...keys].filter(key => enabledCohorts.has(key) && controlCohorts.has(key)));
  const supportedRows = [...supportedKeys].map(key => {
    const e = enabledCohorts.get(key); const c = controlCohorts.get(key);
    const enabledShare = ratio(e.albums, e.decisions); const controlShare = ratio(c.albums, c.decisions);
    return {
      ...e, enabledDecisions: e.decisions, controlDecisions: c.decisions, enabledAlbums: e.albums, controlAlbums: c.albums,
      albumDecisionDelta: e.albums - c.albums, enabledAlbumShare: enabledShare, controlAlbumShare: controlShare,
      compositionEffect: (e.decisions - c.decisions) * controlShare,
      withinCohortAlbumChoiceEffect: e.decisions * (enabledShare - controlShare)
    };
  });
  const unsupportedRows = [...keys].filter(key => !supportedKeys.has(key)).map(key => {
    const e = enabledCohorts.get(key) ?? { cohort: key, decisions: 0, albums: 0 };
    const c = controlCohorts.get(key) ?? { cohort: key, decisions: 0, albums: 0 };
    return { cohort: key, enabledDecisions: e.decisions, controlDecisions: c.decisions, enabledAlbums: e.albums, controlAlbums: c.albums,
      albumDecisionDelta: e.albums - c.albums };
  });
  const enabledSupported = [...enabled.values()].filter(row => supportedKeys.has(fineCohortKey(row)));
  const controlSupported = [...control.values()].filter(row => supportedKeys.has(fineCohortKey(row)));
  const groups = [
    ["all supported", () => true],
    ["Gospel", row => row.canonical === "Gospel"],
    ["SurfRock", row => row.canonical === "SurfRock"],
    ["native Soul", row => row.primary === "Soul" && row.canonical === "Soul"]
  ];
  return {
    supportedFineCohorts: supportedKeys.size,
    supportedAlbumDecisionDelta: sum(supportedRows.map(row => row.albumDecisionDelta)),
    compositionEffect: sum(supportedRows.map(row => row.compositionEffect)),
    withinCohortAlbumChoiceEffect: sum(supportedRows.map(row => row.withinCohortAlbumChoiceEffect)),
    largestSupportedFineCohorts: supportedRows.sort((a, b) => Math.abs(b.albumDecisionDelta) - Math.abs(a.albumDecisionDelta)).slice(0, 20),
    unsupported: {
      cohorts: unsupportedRows.length,
      albumDecisionDelta: sum(unsupportedRows.map(row => row.albumDecisionDelta)),
      largestCohorts: unsupportedRows.sort((a, b) => Math.abs(b.albumDecisionDelta) - Math.abs(a.albumDecisionDelta)).slice(0, 20)
    },
    stages: groups.map(([cohort, predicate]) => ({ cohort,
      enabled: stageSummary(enabledSupported.filter(predicate)), control: stageSummary(controlSupported.filter(predicate))
    }))
  };
}

function controlSoulOrigins(decisions) {
  const origins = new Map();
  for (const decision of decisions.values()) {
    if (decision.canonical !== "Soul") continue;
    let origin = "other canonical Soul";
    if (decision.primary === "Soul") origin = "native Soul";
    else if (decision.primary === "Motown") origin = "Motown → Soul";
    else if (decision.primary === "GirlGroup") origin = "Girl Group → Soul";
    const bucket = get(origins, origin, () => ({ origin, decisions: 0, albums: 0 }));
    bucket.decisions++; bucket.albums += decision.format === "Album" ? 1 : 0;
  }
  const total = sum([...origins.values()].map(bucket => bucket.decisions));
  return [...origins.values()].map(bucket => ({ ...bucket, decisionShare: ratio(bucket.decisions, total) }))
    .sort((a, b) => b.decisions - a.decisions || a.origin.localeCompare(b.origin));
}

const [enabledRun, controlRun] = process.argv.slice(2);
if (!enabledRun || !controlRun) throw new Error("Usage: node SimTools/analyze-supply-cohort-reconciliation.mjs <enabled-run> <control-run>");
const [enabled, control] = await Promise.all([load(enabledRun), load(controlRun)]);
const selector = decision => `${decision.canonical}|${decision.careerBand}|${decision.qualityQuartile}|${reachBucket(decision.reach)}`;
const result = {
  enabledRun, controlRun,
  unitDeltaDecomposition: cohortDelta(enabled.cohorts, control.cohorts),
  decisionPopulation: {
    byCanonicalGenre: populationDelta(enabled.decisions, control.decisions, decision => decision.canonical),
    byCareerBand: populationDelta(enabled.decisions, control.decisions, decision => decision.careerBand),
    byQualityQuartile: populationDelta(enabled.decisions, control.decisions, decision => decision.qualityQuartile),
    byReach: populationDelta(enabled.decisions, control.decisions, decision => reachBucket(decision.reach)),
    byCanonicalGenreCareerBandQualityQuartileReach: populationDelta(enabled.decisions, control.decisions, selector)
  }
};
const summary = {
  enabledRun, controlRun,
  decisionTotals: { enabled: decisionTotal(enabled.decisions), control: decisionTotal(control.decisions) },
  decisionPopulation: {
    byCanonicalGenre: result.decisionPopulation.byCanonicalGenre,
    byCareerBand: result.decisionPopulation.byCareerBand,
    byQualityQuartile: result.decisionPopulation.byQualityQuartile,
    byReach: result.decisionPopulation.byReach
  },
  unitDeltaDecomposition: {
    allFormats: aggregateEffects(result.unitDeltaDecomposition, () => "all"),
    byFormat: aggregateEffects(result.unitDeltaDecomposition, row => row.format),
    largestCanonicalGenreFormatDeltas: [...result.unitDeltaDecomposition]
      .sort((a, b) => Math.abs(b.unitDelta) - Math.abs(a.unitDelta)).slice(0, 20)
  },
  unadjustedAlbumDecisionDecomposition: albumDecisionDecomposition(enabled.decisions, control.decisions),
  commonSupportAlbumDecisionDecomposition: commonSupportAnalysis(enabled.decisions, control.decisions),
  controlCanonicalSoulByRawOrigin: controlSoulOrigins(control.decisions),
  controlGirlGroupRawSecondaryDistribution: control.girlGroupSecondaries
};
process.stdout.write(`${JSON.stringify(process.argv.includes("--summary") ? summary : result, null, 2)}\n`);
