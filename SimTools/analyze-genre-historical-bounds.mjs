import fs from "node:fs";
import path from "node:path";
import readline from "node:readline";

const runs = [
  { seed: 1001, prefix: "d6-economic-lifecycle-founder-emergence-decade-1001" },
  { seed: 1002, prefix: "d6-economic-lifecycle-founder-emergence-decade-1002" },
  { seed: 2007, prefix: "d6-economic-lifecycle-genre-label-holdout-2007" },
];
const years = Array.from({ length: 10 }, (_, index) => 1960 + index);
const logDirectory = process.argv[2] ?? "SimLogs";
const annualOutput = process.argv[3] ?? "SimTools/GenreHistoricalBoundsAnnual.csv";
const summaryOutput = process.argv[4] ?? "SimTools/GenreHistoricalBoundsSummary.md";

function parseCsvLine(line) {
  const fields = [];
  let value = "";
  let quoted = false;
  for (let index = 0; index < line.length; index++) {
    const character = line[index];
    if (character === '"') {
      if (quoted && line[index + 1] === '"') {
        value += '"';
        index++;
      } else {
        quoted = !quoted;
      }
    } else if (character === "," && !quoted) {
      fields.push(value);
      value = "";
    } else {
      value += character;
    }
  }
  fields.push(value);
  return fields;
}

async function readCsv(filePath, consume) {
  const input = fs.createReadStream(filePath, { encoding: "utf8" });
  const lines = readline.createInterface({ input, crlfDelay: Infinity });
  let indexes;
  for await (const line of lines) {
    if (!indexes) {
      indexes = Object.fromEntries(parseCsvLine(line).map((field, index) => [field, index]));
      continue;
    }
    if (line.length > 0) consume(parseCsvLine(line), indexes);
  }
}

function number(value) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function csv(value) {
  const text = value == null ? "" : String(value);
  return /[",\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text;
}

function metricKey(seed, year, genre) {
  return `${seed}|${year}|${genre}`;
}

function peakYear(rows, field) {
  let best = null;
  for (const row of rows) {
    if (best == null || row[field] > best[field] || (row[field] === best[field] && row.year < best.year)) best = row;
  }
  return best?.year ?? "";
}

function firstPositiveYear(rows, field) {
  return rows.find((row) => row[field] > 0)?.year ?? "";
}

function range(values, digits = 1) {
  const finite = values.filter(Number.isFinite);
  if (finite.length === 0) return "";
  const minimum = Math.min(...finite);
  const maximum = Math.max(...finite);
  const format = (value) => value.toFixed(digits);
  return minimum === maximum ? format(minimum) : `${format(minimum)}-${format(maximum)}`;
}

function millions(value) {
  return `${(value / 1_000_000).toFixed(2)}m`;
}

function correlation(pairs) {
  const leftMean = pairs.reduce((sum, pair) => sum + pair[0], 0) / pairs.length;
  const rightMean = pairs.reduce((sum, pair) => sum + pair[1], 0) / pairs.length;
  const covariance = pairs.reduce((sum, pair) => sum + (pair[0] - leftMean) * (pair[1] - rightMean), 0);
  const leftVariance = pairs.reduce((sum, pair) => sum + (pair[0] - leftMean) ** 2, 0);
  const rightVariance = pairs.reduce((sum, pair) => sum + (pair[1] - rightMean) ** 2, 0);
  return covariance / Math.sqrt(leftVariance * rightVariance);
}

const catalog = [];
const catalogBySlug = new Map();
const catalogByGenre = new Map();
const catalogFile = path.join(logDirectory, `${runs[0].prefix}-genre-catalog.csv`);
await readCsv(catalogFile, (fields, index) => {
  const profile = {
    id: fields[index.id],
    genre: fields[index.genre],
    family: fields[index.family],
    emergence: number(fields[index.emergenceYear]),
    death: fields[index.deathYear] === "" ? null : number(fields[index.deathYear]),
    audienceLean: number(fields[index.audienceLean]),
    singleOrientation: number(fields[index.singleOrientation]),
    baselines: Object.fromEntries(years.map((year) => {
      const exact = index[`baseline${year}`];
      if (exact != null) return [year, number(fields[exact])];
      const anchors = [1960, 1962, 1964, 1966, 1967, 1968, 1969];
      const upperPosition = anchors.findIndex((anchor) => anchor >= year);
      if (upperPosition <= 0) return [year, number(fields[index[`baseline${anchors[0]}`]])];
      const lower = anchors[upperPosition - 1];
      const upper = anchors[upperPosition];
      const lowerValue = number(fields[index[`baseline${lower}`]]);
      const upperValue = number(fields[index[`baseline${upper}`]]);
      return [year, lowerValue + (upperValue - lowerValue) * ((year - lower) / (upper - lower))];
    })),
  };
  catalog.push(profile);
  catalogBySlug.set(profile.id, profile);
  catalogByGenre.set(profile.genre, profile);
});

const metrics = new Map();
const annualTotals = new Map();
const regionalTotals = new Map();
const regionalGenreUnits = new Map();
for (const run of runs) {
  for (const year of years) {
    for (const profile of catalog) {
      metrics.set(metricKey(run.seed, year, profile.genre), {
        seed: run.seed,
        year,
        genre: profile.genre,
        supply: 0,
        retainedSupply: 0,
        transitionSupply: 0,
        floorSupply: 0,
        decisions: 0,
        singleDecisions: 0,
        albumDecisions: 0,
        orphanDecisions: 0,
        standaloneDecisions: 0,
        promoDecisions: 0,
        fulfilledUnits: 0,
        backorders: 0,
        settlementSingleUnits: 0,
        settlementAlbumUnits: 0,
        acceptanceSum: 0,
        eligibleSum: 0,
        chartedSum: 0,
        radioSum: 0,
        marketRows: 0,
      });
    }
  }
}

function getMetric(seed, year, genre) {
  return metrics.get(metricKey(seed, year, genre));
}

for (const run of runs) {
  const file = (suffix) => path.join(logDirectory, `${run.prefix}-${suffix}.csv`);

  await readCsv(file("supply-selections"), (fields, index) => {
    const year = number(fields[index.year]);
    const metric = getMetric(run.seed, year, fields[index.chosenProjectGenre]);
    if (!metric) return;
    metric.supply++;
    const mode = fields[index.selectionMode];
    if (mode === "Retained") metric.retainedSupply++;
    else if (mode === "WeightedTransition") metric.transitionSupply++;
    else if (mode === "AnnualFloor") metric.floorSupply++;
  });

  await readCsv(file("release-strategy"), (fields, index) => {
    const year = number(fields[index.year]);
    const metric = getMetric(run.seed, year, fields[index.genre]);
    if (!metric) return;
    metric.decisions++;
    const format = fields[index.chosenFormat];
    if (format === "Single") metric.singleDecisions++;
    else if (format === "Album") metric.albumDecisions++;
    const strategy = fields[index.strategy];
    if (strategy === "OrphanSingle") metric.orphanDecisions++;
    else if (strategy === "AlbumStandalone") metric.standaloneDecisions++;
    else if (strategy === "AlbumWithPromo") metric.promoDecisions++;
  });

  await readCsv(file("genre-market-weekly"), (fields, index) => {
    if (fields[index.segment] !== "AllSegments") return;
    const year = number(fields[index.year]);
    const profile = catalogBySlug.get(fields[index.genre]);
    const metric = profile ? getMetric(run.seed, year, profile.genre) : null;
    if (!metric) return;
    metric.acceptanceSum += number(fields[index.effectiveAcceptance]);
    metric.eligibleSum += number(fields[index.eligibleRecords]);
    metric.chartedSum += number(fields[index.chartedRecords]);
    metric.radioSum += number(fields[index.radioPlay]);
    metric.marketRows++;
  });

  await readCsv(file("geography-metrics"), (fields, index) => {
    const year = number(fields[index.year]);
    const genre = fields[index.genre];
    const metric = getMetric(run.seed, year, genre);
    if (!metric) return;
    const units = number(fields[index.totalUnits]);
    metric.fulfilledUnits += units;
    metric.backorders += number(fields[index.backorders]);
    const totalKey = `${run.seed}|${year}`;
    annualTotals.set(totalKey, (annualTotals.get(totalKey) ?? 0) + units);
    const region = fields[index.regionId];
    const regionalKey = `${run.seed}|${region}`;
    regionalTotals.set(regionalKey, (regionalTotals.get(regionalKey) ?? 0) + units);
    const regionalGenreKey = `${run.seed}|${genre}|${region}`;
    regionalGenreUnits.set(regionalGenreKey, (regionalGenreUnits.get(regionalGenreKey) ?? 0) + units);
  });

  await readCsv(file("completed-week-settlement"), (fields, index) => {
    const year = number(fields[index.year]);
    const metric = getMetric(run.seed, year, fields[index.genre]);
    if (!metric) return;
    const units = number(fields[index.totalUnits]);
    if (fields[index.format] === "Single") metric.settlementSingleUnits += units;
    else if (fields[index.format] === "Album") metric.settlementAlbumUnits += units;
  });
}

const annualRows = [...metrics.values()].sort((left, right) =>
  left.seed - right.seed || left.year - right.year || left.genre.localeCompare(right.genre));
for (const row of annualRows) {
  const denominator = annualTotals.get(`${row.seed}|${row.year}`) ?? 0;
  row.marketSharePct = denominator > 0 ? 100 * row.fulfilledUnits / denominator : 0;
  row.backorderRatePct = row.fulfilledUnits + row.backorders > 0
    ? 100 * row.backorders / (row.fulfilledUnits + row.backorders) : 0;
  row.decisionSingleSharePct = row.decisions > 0 ? 100 * row.singleDecisions / row.decisions : 0;
  const settlementUnits = row.settlementSingleUnits + row.settlementAlbumUnits;
  row.settlementSingleSharePct = settlementUnits > 0 ? 100 * row.settlementSingleUnits / settlementUnits : 0;
  row.meanAcceptance = row.marketRows > 0 ? row.acceptanceSum / row.marketRows : 0;
  row.meanEligible = row.marketRows > 0 ? row.eligibleSum / row.marketRows : 0;
  row.meanCharted = row.marketRows > 0 ? row.chartedSum / row.marketRows : 0;
  row.meanRadio = row.marketRows > 0 ? row.radioSum / row.marketRows : 0;
  row.unitsPerSupply = row.supply > 0 ? row.fulfilledUnits / row.supply : 0;
}

const annualHeaders = [
  "seed", "year", "genre", "family", "emergenceYear", "deathYear", "singleOrientation", "authoredBaseline",
  "supply", "retainedSupply", "transitionSupply", "floorSupply",
  "decisions", "singleDecisions", "albumDecisions", "decisionSingleSharePct",
  "orphanDecisions", "standaloneDecisions", "promoDecisions",
  "fulfilledUnits", "marketSharePct", "backorders", "backorderRatePct", "unitsPerSupply",
  "settlementSingleUnits", "settlementAlbumUnits", "settlementSingleSharePct",
  "meanAcceptance", "meanEligible", "meanCharted", "meanRadio",
];
const annualLines = [annualHeaders.join(",")];
for (const row of annualRows) {
  const profile = catalogByGenre.get(row.genre);
  const values = {
    ...row,
    family: profile.family,
    emergenceYear: profile.emergence,
    deathYear: profile.death ?? "",
    singleOrientation: profile.singleOrientation,
    authoredBaseline: profile.baselines[row.year],
  };
  annualLines.push(annualHeaders.map((header) => csv(values[header])).join(","));
}
fs.writeFileSync(annualOutput, `${annualLines.join("\n")}\n`);

function rowsFor(seed, genre) {
  return years.map((year) => getMetric(seed, year, genre));
}

function baselinePeak(profile) {
  let bestYear = years[0];
  for (const year of years) if (profile.baselines[year] > profile.baselines[bestYear]) bestYear = year;
  return bestYear;
}

function activeRows(seed, profile) {
  return rowsFor(seed, profile.genre).filter((row) => row.year >= Math.max(1960, Math.floor(profile.emergence)));
}

function formatCollapses(profile) {
  const collapses = [];
  for (const run of runs) {
    for (const row of activeRows(run.seed, profile)) {
      if (row.decisions < 10) continue;
      if (row.singleDecisions === 0) collapses.push(`${run.seed}:${row.year} S=0/${row.decisions}`);
      if (row.albumDecisions === 0) collapses.push(`${run.seed}:${row.year} A=0/${row.decisions}`);
    }
  }
  return collapses;
}

const largestShare = annualRows.reduce((best, row) => row.marketSharePct > best.marketSharePct ? row : best, annualRows[0]);
const genreFormatPairs = catalog.map((profile) => {
  const rows = runs.flatMap((run) => activeRows(run.seed, profile));
  const decisions = rows.reduce((sum, row) => sum + row.decisions, 0);
  const singles = rows.reduce((sum, row) => sum + row.singleDecisions, 0);
  return [profile.singleOrientation, decisions > 0 ? 100 * singles / decisions : 0];
});
const markdown = [];
markdown.push("# Cross-seed genre historical-bounds summary", "");
markdown.push("Generated from the immutable 522-week seeds 1001, 1002, and 2007. `geography-metrics.csv` owns fulfilled commercial units; `completed-week-settlement.csv` supplies the independent Single/Album unit split; `AllSegments` rows avoid audience-segment double counting.", "");
markdown.push(`Largest annual canonical share: **${largestShare.genre} ${largestShare.seed}/${largestShare.year}, ${largestShare.marketSharePct.toFixed(2)}%**, below the 35% cap.`, "");
markdown.push(`Across the 42 pooled active-genre portfolios, authored Single orientation correlates only **${correlation(genreFormatPairs).toFixed(3)}** with actual Single decision share. This is a diagnostic of realized format allocation, not a replacement for the fixed-input conservation probe.`, "");
markdown.push("The table is diagnostic. Authored-peak distance is not automatically a binding historical failure; it identifies genres requiring human review alongside the explicit Directive 5 gates.", "");
markdown.push("| Genre | Authored | Commercial peak (1001/1002/2007) | Acceptance peak | First units | Active decision Single % | Active settlement Single-unit % | Peak share % range | Zero-lane years (>=10 decisions) |");
markdown.push("| --- | --- | --- | --- | --- | ---: | ---: | ---: | --- |");

for (const profile of catalog) {
  const commercialPeaks = runs.map((run) => peakYear(rowsFor(run.seed, profile.genre), "fulfilledUnits"));
  const acceptancePeaks = runs.map((run) => peakYear(rowsFor(run.seed, profile.genre), "meanAcceptance"));
  const firstYears = runs.map((run) => firstPositiveYear(rowsFor(run.seed, profile.genre), "fulfilledUnits"));
  const decisionShares = runs.map((run) => {
    const rows = activeRows(run.seed, profile);
    const decisions = rows.reduce((sum, row) => sum + row.decisions, 0);
    const singles = rows.reduce((sum, row) => sum + row.singleDecisions, 0);
    return decisions > 0 ? 100 * singles / decisions : Number.NaN;
  });
  const settlementShares = runs.map((run) => {
    const rows = activeRows(run.seed, profile);
    const singles = rows.reduce((sum, row) => sum + row.settlementSingleUnits, 0);
    const albums = rows.reduce((sum, row) => sum + row.settlementAlbumUnits, 0);
    return singles + albums > 0 ? 100 * singles / (singles + albums) : Number.NaN;
  });
  const peakShares = runs.map((run) => Math.max(...rowsFor(run.seed, profile.genre).map((row) => row.marketSharePct)));
  const authored = `${profile.emergence}/${profile.death ?? "-"}; base peak ${baselinePeak(profile)}; orient ${profile.singleOrientation.toFixed(2)}`;
  markdown.push(`| ${profile.genre} | ${authored} | ${commercialPeaks.join("/")} | ${acceptancePeaks.join("/")} | ${firstYears.join("/")} | ${range(decisionShares)} | ${range(settlementShares)} | ${range(peakShares, 2)} | ${formatCollapses(profile).join("; ")} |`);
}

markdown.push("", "## 1969 orientation-group decision mix", "");
markdown.push("These are decision-weighted group means. The observed ordering is reversed: the nominally Single-leaning group makes the fewest Single decisions in every seed.", "");
markdown.push("| Seed | Album-lean (orientation <= .40) | Middle (.45-.65) | Single-lean (>= .70) |");
markdown.push("| ---: | ---: | ---: | ---: |");
for (const run of runs) {
  const groups = {
    album: { singles: 0, decisions: 0 },
    middle: { singles: 0, decisions: 0 },
    single: { singles: 0, decisions: 0 },
  };
  for (const profile of catalog) {
    const row = getMetric(run.seed, 1969, profile.genre);
    const group = profile.singleOrientation <= .40 ? groups.album :
      profile.singleOrientation >= .70 ? groups.single : groups.middle;
    group.singles += row.singleDecisions;
    group.decisions += row.decisions;
  }
  const share = (group) => group.decisions > 0 ? `${(100 * group.singles / group.decisions).toFixed(1)}%` : "";
  markdown.push(`| ${run.seed} | ${share(groups.album)} | ${share(groups.middle)} | ${share(groups.single)} |`);
}

markdown.push("", "## Explicit historical-gate evidence", "");
function unit(seed, year, genre) {
  return getMetric(seed, year, genre).fulfilledUnits;
}
for (const run of runs) {
  const dooPeak = peakYear(rowsFor(run.seed, "DooWop"), "fulfilledUnits");
  const surfPeak = peakYear(rowsFor(run.seed, "SurfRock"), "fulfilledUnits");
  const psychPeak = peakYear(rowsFor(run.seed, "PsychedelicRock"), "fulfilledUnits");
  const funk1967 = unit(run.seed, 1967, "Funk");
  const funk1969 = unit(run.seed, 1969, "Funk");
  const late1969 = ["HardRock", "BluesRock", "ProtoMetal", "ProgressiveRock"]
    .reduce((sum, genre) => sum + unit(run.seed, 1969, genre), 0);
  markdown.push(`- Seed ${run.seed}: Doo-Wop peak ${dooPeak}; Surf Rock peak ${surfPeak}; British Beat/Pop first ${firstPositiveYear(rowsFor(run.seed, "BritishBeat"), "fulfilledUnits")}/${firstPositiveYear(rowsFor(run.seed, "BritishPop"), "fulfilledUnits")}; Psychedelic Rock peak **${psychPeak}**; Funk ${millions(funk1967)} -> ${millions(funk1969)} (1967->69); late Hard/Blues/Proto-Metal/Progressive 1969 ${millions(late1969)}.`);
}

markdown.push("", "## Specialist regional shares (full decade)", "");
markdown.push("| Seed | Genre | Highest region | Highest share | Required region/share | Result |");
markdown.push("| ---: | --- | --- | ---: | --- | --- |");
for (const run of runs) {
  for (const [genre, requiredRegion] of [["TexMex", "southwest"], ["Boogaloo", "eastcoast"]]) {
    const shares = [...new Set([...regionalTotals.keys()].filter((key) => key.startsWith(`${run.seed}|`)).map((key) => key.split("|")[1]))]
      .map((region) => {
        const units = regionalGenreUnits.get(`${run.seed}|${genre}|${region}`) ?? 0;
        const total = regionalTotals.get(`${run.seed}|${region}`) ?? 0;
        return { region, share: total > 0 ? 100 * units / total : 0 };
      }).sort((left, right) => right.share - left.share);
    const required = shares.find((entry) => entry.region === requiredRegion);
    markdown.push(`| ${run.seed} | ${genre} | ${shares[0].region} | ${shares[0].share.toFixed(4)}% | ${requiredRegion} / ${required.share.toFixed(4)}% | ${shares[0].region === requiredRegion ? "PASS" : "FAIL"} |`);
  }
  const countryPreferred = ["deepsouth", "greatplains", "southwest"];
  const countryShares = countryPreferred.map((region) => {
    const units = regionalGenreUnits.get(`${run.seed}|Country|${region}`) ?? 0;
    const total = regionalTotals.get(`${run.seed}|${region}`) ?? 0;
    return { region, share: total > 0 ? 100 * units / total : 0 };
  });
  const nationalCountry = years.reduce((sum, year) => sum + unit(run.seed, year, "Country"), 0) /
    years.reduce((sum, year) => sum + (annualTotals.get(`${run.seed}|${year}`) ?? 0), 0) * 100;
  markdown.push(`| ${run.seed} | Country | ${countryShares.map((entry) => `${entry.region} ${entry.share.toFixed(3)}%`).join("; ")} |  | national / ${nationalCountry.toFixed(3)}% | ${countryShares.every((entry) => entry.share > nationalCountry) ? "PASS" : "FAIL"} |`);
}

fs.writeFileSync(summaryOutput, `${markdown.join("\n")}\n`);
console.log(`Wrote ${annualRows.length} annual rows to ${annualOutput}`);
console.log(`Wrote ${catalog.length} genre summaries to ${summaryOutput}`);
