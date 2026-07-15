import fs from "node:fs";
import path from "node:path";

/*
 * Offline attribution report for the bounded Directive 6 economic-yield
 * investigation.  It reads immutable ChartAuditRunner CSV families only.
 *
 * Usage:
 *   node SimTools/analyze-economic-yield-attribution.mjs <control-run> <enabled-run> [--output <report.md>]
 *
 * A run is a prefix below SimLogs (for example
 * systemic-label-capacity-control-104-1001) or an absolute prefix.  Revenue
 * streams are aggregate-only, so group gross/label-net/market-net values are
 * allocated by that group's realized units within the same year/format.  The
 * report calls this out rather than presenting it as record-level finance.
 */

const logDirectory = path.resolve("SimLogs");
const formatNumber = new Intl.NumberFormat("en-US", { maximumFractionDigits: 2 });
const f = value => Number.isFinite(value) ? formatNumber.format(value) : "—";
const ratio = (a, b) => b ? a / b : null;
const n = value => { const parsed = Number(value); return Number.isFinite(parsed) ? parsed : 0; };
const key = (...parts) => parts.join("|");

function splitCsv(line) {
  const values = []; let value = ""; let quoted = false;
  for (let i = 0; i < line.length; i++) {
    const c = line[i];
    if (c === '"') {
      if (quoted && line[i + 1] === '"') { value += c; i++; } else quoted = !quoted;
    } else if (c === "," && !quoted) { values.push(value); value = ""; } else value += c;
  }
  values.push(value); return values;
}

function csv(file, required = true) {
  if (!fs.existsSync(file)) {
    if (required) throw new Error(`Missing telemetry: ${file}`);
    return [];
  }
  const content = fs.readFileSync(file, "utf8").trim();
  if (!content) return [];
  const lines = content.split(/\r?\n/);
  const headers = splitCsv(lines.shift());
  return lines.filter(Boolean).map(line => {
    const values = splitCsv(line);
    return Object.fromEntries(headers.map((header, index) => [header, values[index] ?? ""]));
  });
}

function prefix(run) { return path.isAbsolute(run) ? run : path.join(logDirectory, run); }
function get(map, id, create) { if (!map.has(id)) map.set(id, create()); return map.get(id); }
function sourceForArtist(id, enabled, cohort) {
  if (!enabled) return "Original3000";
  // RuntimeFormation is emitted by artist-project-identity.csv at release time.
  // This is authoritative for the no-reserve diagnostic, where runtime IDs begin
  // immediately after artist_03000 rather than after the default reserve.
  if (cohort === "RuntimeFormation") return "RuntimeFormation";
  const numericId = Number(String(id).replace(/^artist_/, ""));
  if (!Number.isFinite(numericId)) return "Unknown";
  if (numericId <= 3000) return "Original3000";
  if (numericId <= 7000) return "EnabledInitialReserve";
  return "RuntimeFormation";
}
function quartile(quality) { return quality < .25 ? "Q1" : quality < .5 ? "Q2" : quality < .75 ? "Q3" : "Q4"; }
function markdownTable(headers, rows) {
  const escaped = value => String(value ?? "").replaceAll("|", "\\|");
  return [
    `| ${headers.map(escaped).join(" | ")} |`,
    `| ${headers.map(() => "---").join(" | ")} |`,
    ...rows.map(row => `| ${row.map(escaped).join(" | ")} |`)
  ].join("\n");
}

function finalRecordRows(records) {
  const latest = new Map(); const first = new Map();
  for (const row of records) {
    const existing = latest.get(row.recordId);
    if (!existing || n(row.week) > n(existing.week)) latest.set(row.recordId, row);
    const initial = first.get(row.recordId);
    if (!initial || n(row.week) < n(initial.week)) first.set(row.recordId, row);
  }
  return [...latest.values()].map(row => ({ ...row, releaseYear: n(first.get(row.recordId).year), releaseWeek: n(first.get(row.recordId).week) }));
}

function aggregateMarket(rows) {
  const result = new Map();
  for (const row of rows) {
    if (row.period !== "annual" || row.labelTier !== "All" || !["Single", "Album"].includes(row.releaseFormat)) continue;
    result.set(key(row.year, row.releaseFormat), { units: n(row.totalMarketUnits), gross: n(row.gross), labelNet: n(row.labelNet), marketNet: n(row.marketNet) });
  }
  return result;
}

function load(run, enabled) {
  const base = prefix(run);
  const records = finalRecordRows(csv(`${base}-records.csv`));
  const decisions = csv(`${base}-a3-economic-decisions.csv`);
  const decisionByRecord = new Map(decisions.map(row => [row.recordId, row]));
  const identities = csv(`${base}-artist-project-identity.csv`, false);
  const identityByRecord = new Map(identities.map(row => [row.recordId, row]));
  const market = aggregateMarket(csv(`${base}-market-revenue.csv`));
  const labels = csv(`${base}-label-finance.csv`, false);
  const albums = csv(`${base}-album-composition.csv`, false);
  const capacity = csv(`${base}-release-capacity.csv`, false);
  const weeklyMarket = csv(`${base}-market-revenue.csv`, false);
  const all = records.map(record => {
    const decision = decisionByRecord.get(record.recordId);
    const identity = identityByRecord.get(record.recordId);
    const releaseFormat = decision?.chosenFormat || (record.recordId ? "Single" : "Unknown");
    const format = releaseFormat === "Album" ? "Album" : "Single";
    const quality = decision ? n(decision.qualityEstimate) : n(record.quality);
    return {
      ...record, format, source: sourceForArtist(record.artistId, enabled, identity?.cohort), quality,
      qualityQuartile: decision?.qualityQuartile || quartile(quality),
      launchCareerState: record.launchCareerState || "Unavailable",
      labelTier: record.labelTier || "Unavailable",
      releaseCohort: record.releaseYear === n(record.year) ? "CurrentYear" : "Carryover",
      awareness: n(record.initialLaunchAwareness), stock: n(record.initialLaunchStock), perceivedQualityMultiplier: n(record.perceivedQualityMultiplier),
      units: n(record.totalUnitsSold)
    };
  });
  return { run, enabled, records: all, decisions, market, labels, albums, capacity, weeklyMarket };
}

function releaseGroups(data, year) {
  const groups = new Map();
  for (const row of data.records.filter(row => row.releaseYear === Number(year))) {
    const group = get(groups, key(year, row.format, row.source, row.releaseCohort, row.launchCareerState, row.labelTier, row.qualityQuartile), () => ({
      year, format: row.format, artistSource: row.source, releaseCohort: row.releaseCohort, launchCareerState: row.launchCareerState,
      labelTier: row.labelTier, qualityQuartile: row.qualityQuartile, recordCount: 0, units: 0, quality: 0, awareness: 0, stock: 0, multiplier: 0
    }));
    group.recordCount++; group.units += row.units; group.quality += row.quality; group.awareness += row.awareness; group.stock += row.stock; group.multiplier += row.perceivedQualityMultiplier;
  }
  const marketByFormat = data.market;
  return [...groups.values()].map(group => {
    const totals = marketByFormat.get(key(year, group.format)) || { units: 0, gross: 0, labelNet: 0, marketNet: 0 };
    return {
      ...group, unitsPerRecord: ratio(group.units, group.recordCount), meanQuality: ratio(group.quality, group.recordCount),
      meanInitialAwareness: ratio(group.awareness, group.recordCount), meanInitialStock: ratio(group.stock, group.recordCount),
      meanPerceivedQualityMultiplier: ratio(group.multiplier, group.recordCount), gross: group.units * ratio(totals.gross, totals.units),
      labelNet: group.units * ratio(totals.labelNet, totals.units), marketNet: group.units * ratio(totals.marketNet, totals.units)
    };
  }).sort((a, b) => b.units - a.units);
}

function annual(data) {
  const values = new Map();
  for (const [marketKey, row] of data.market) {
    const [year, format] = marketKey.split("|");
    const aggregate = get(values, year, () => ({ year: Number(year), units: 0, gross: 0, labelNet: 0, marketNet: 0, singles: 0, albums: 0 }));
    aggregate.units += row.units; aggregate.gross += row.gross; aggregate.labelNet += row.labelNet; aggregate.marketNet += row.marketNet;
    if (format === "Single") aggregate.singles = row.units; else aggregate.albums = row.units;
  }
  return values;
}

function quarterly(data) {
  const result = new Map();
  for (const row of data.weeklyMarket) {
    if (row.period !== "weekly" || row.labelTier !== "All" || row.releaseFormat !== "All") continue;
    const block = Math.floor((n(row.week) - 1) / 13) + 1;
    const item = get(result, key(row.year, block), () => ({ year: n(row.year), block, units: 0, gross: 0 }));
    item.units += n(row.totalMarketUnits); item.gross += n(row.gross);
  }
  return result;
}

function labelCounts(data) {
  const finalByLabel = new Map();
  for (const row of data.labels) {
    const previous = finalByLabel.get(row.labelId);
    if (!previous || n(row.week) > n(previous.week)) finalByLabel.set(row.labelId, row);
  }
  const result = new Map();
  for (const row of finalByLabel.values()) {
    const group = get(result, key(row.year, row.status), () => ({ year: n(row.year), status: row.status, count: 0 })); group.count++;
  }
  return [...result.values()].sort((a, b) => a.year - b.year || a.status.localeCompare(b.status));
}

function albumMetrics(data) {
  const groups = new Map();
  for (const album of data.albums) {
    const group = get(groups, key(album.year, album.albumFormat), () => ({ year: n(album.year), format: album.albumFormat, records: 0, pooledAppeal: 0, reuse: 0, freshness: 0 }));
    group.records++; group.pooledAppeal += n(album.pooledAppeal); group.reuse += n(album.reusedSingleTracks);
    // Freshness is not stored directly in this CSV.  The report deliberately warns instead of deriving it from a different semantic field.
  }
  const annualRows = annual(data);
  return [...groups.values()].map(group => {
    const yearly = annualRows.get(String(group.year));
    return { ...group, units: yearly?.albums ?? 0, unitsPerReleasedAlbum: ratio(yearly?.albums ?? 0, group.records), meanPooledAppeal: ratio(group.pooledAppeal, group.records), meanReuseCount: ratio(group.reuse, group.records), meanFreshness: null };
  }).sort((a, b) => a.year - b.year || a.format.localeCompare(b.format));
}

function capacityByYear(data) {
  const result = new Map();
  for (const row of data.capacity) {
    const year = n(row.year);
    const item = get(result, year, () => ({ year, releases: 0 })); item.releases += n(row.successfulReleases);
  }
  return result;
}

function scheduledAlbumsByYear(data) {
  const result = new Map();
  for (const project of csv(`${prefix(data.run)}-album-projects.csv`, false)) {
    // pipeline week 1-52 is 1960 and 53-104 is 1961 in these bounded runs.
    // The project CSV does not carry a calendar year, so retain the exact
    // schedule boundary rather than trying to infer it from drop timing.
    const year = 1960 + Math.floor((n(project.scheduledWeek) - 1) / 52);
    result.set(year, (result.get(year) || 0) + 1);
  }
  return result;
}

function capacityRows(control, enabled) {
  const controlCapacity = capacityByYear(control), enabledCapacity = capacityByYear(enabled);
  const controlAlbums = scheduledAlbumsByYear(control), enabledAlbums = scheduledAlbumsByYear(enabled);
  const years = [...new Set([...controlCapacity.keys(), ...enabledCapacity.keys(), ...controlAlbums.keys(), ...enabledAlbums.keys()])].sort((a, b) => a - b);
  return years.map(year => [year, f(controlCapacity.get(year)?.releases), f(enabledCapacity.get(year)?.releases),
    f(ratio(enabledCapacity.get(year)?.releases, controlCapacity.get(year)?.releases)), f(controlAlbums.get(year)),
    f(enabledAlbums.get(year)), f(ratio(enabledAlbums.get(year), controlAlbums.get(year)))]);
}

function marketFormatReconciliation(data) {
  const snapshot = new Map();
  for (const record of data.records) {
    const row = get(snapshot, key(record.year, record.format), () => ({ units: 0, records: 0 }));
    row.units += record.units; row.records++;
  }
  return [...data.market.entries()].map(([marketKey, market]) => {
    const [year, format] = marketKey.split("|"); const observed = snapshot.get(marketKey) || { units: 0, records: 0 };
    return [year, format, f(market.units), f(observed.units), f(observed.units - market.units), observed.records];
  }).sort((a, b) => Number(a[0]) - Number(b[0]) || a[1].localeCompare(b[1]));
}

function sourceSummary(data) {
  const groups = new Map();
  for (const record of data.records.filter(record => record.format === "Single" && record.releaseCohort === "CurrentYear")) {
    const group = get(groups, key(record.releaseYear, record.source), () => ({ year: record.releaseYear, source: record.source, records: 0, units: 0, quality: 0 }));
    group.records++; group.units += record.units; group.quality += record.quality;
  }
  return [...groups.values()].map(group => [group.year, group.source, group.records, f(group.units), f(ratio(group.units, group.records)), f(ratio(group.quality, group.records))])
    .sort((a, b) => Number(a[0]) - Number(b[0]) || String(a[1]).localeCompare(String(b[1])));
}

function pairedRows(control, enabled) {
  const c = annual(control), e = annual(enabled), years = [...new Set([...c.keys(), ...e.keys()])].sort();
  return years.map(year => {
    const left = c.get(year) || {}, right = e.get(year) || {};
    return [year, f(left.units), f(right.units), f(ratio(right.units, left.units)), f(left.gross), f(right.gross), f(ratio(right.gross, left.gross)), f(ratio(right.labelNet, left.labelNet)), f(ratio(right.marketNet, left.marketNet))];
  });
}

function paired13Week(control, enabled) {
  const c = quarterly(control), e = quarterly(enabled), keys = [...new Set([...c.keys(), ...e.keys()])].sort();
  return keys.map(id => {
    const left = c.get(id) || {}, right = e.get(id) || {}, block = left.block || right.block;
    return [`${left.year || right.year} W${(block - 1) * 13 + 1}-${block * 13}`, f(ratio(right.units, left.units)), f(ratio(right.gross, left.gross))];
  });
}

function summaryRows(data, year) {
  const groups = releaseGroups(data, year);
  return groups.map(row => [row.year, row.format, row.artistSource, row.releaseCohort, row.launchCareerState, row.labelTier, row.qualityQuartile, f(row.recordCount), f(row.units), f(row.unitsPerRecord), f(row.meanQuality), f(row.meanInitialAwareness), f(row.meanInitialStock), f(row.meanPerceivedQualityMultiplier), f(row.gross), f(row.labelNet), f(row.marketNet)]);
}

const args = process.argv.slice(2); const outputAt = args.indexOf("--output");
const output = outputAt >= 0 ? args.splice(outputAt, 2)[1] : null;
if (args.length !== 2) throw new Error("Usage: node SimTools/analyze-economic-yield-attribution.mjs <control-run> <enabled-run> [--output report.md]");
const control = load(args[0], false), enabled = load(args[1], true);
const years = [...new Set([...annual(control).keys(), ...annual(enabled).keys()])].sort(Number);
const report = [
  "# Economic-yield attribution report",
  "",
  `Control: \`${control.run}\`  
Enabled: \`${enabled.run}\``,
  "",
  "## Method and limits",
  "",
  "Artist source first uses the release-time `artist-project-identity.csv` cohort (`RuntimeFormation` is authoritative), then splits the initial cohort at the explicit construction boundary: `artist_00001`–`artist_03000` = Original3000 and `artist_03001`–`artist_07000` = EnabledInitialReserve. The no-reserve diagnostic therefore correctly classifies `artist_03001+` as runtime when its emitted cohort says so. Runtime is never inferred from `formedYear`. A record is assigned to the earliest observed snapshot; its units are the final observed `totalUnitsSold` snapshot. `gross`, `labelNet`, and `marketNet` below are allocated from annual all-label format aggregates by realized units, because the archived streams do not carry record-level values for those three fields.",
  "",
  "Warnings: records.csv cannot identify records that retired before the final snapshot; no immutable per-record freshness field exists in album-composition.csv; control artifacts do not carry enabled artist cohorts. The analyzer reports those limitations instead of inventing values.",
  "",
  "## Annual economic comparison",
  "",
  markdownTable(["Year", "Control units", "Enabled units", "Unit ratio", "Control gross", "Enabled gross", "Gross ratio", "Label-net ratio", "Market-net ratio"], pairedRows(control, enabled)),
  "",
  "## Capacity comparison", "",
  markdownTable(["Year", "Control releases", "Enabled releases", "Release ratio", "Control scheduled Albums", "Enabled scheduled Albums", "Album ratio"], capacityRows(control, enabled)),
  "",
  "## Thirteen-week comparison",
  "",
  markdownTable(["Block", "Unit ratio", "Gross ratio"], paired13Week(control, enabled)),
  "",
  "## Release attribution (all available dimensions)",
  "",
  ...years.flatMap(year => [
    `### ${year} control`, "", markdownTable(["Year", "Format", "Artist source", "Release cohort", "Launch state", "Label tier", "Quality", "Records", "Units", "Units/record", "Mean quality", "Mean awareness", "Mean stock", "Mean perceived-quality", "Allocated gross", "Allocated label net", "Allocated market net"], summaryRows(control, year)), "",
    `### ${year} enabled`, "", markdownTable(["Year", "Format", "Artist source", "Release cohort", "Launch state", "Label tier", "Quality", "Records", "Units", "Units/record", "Mean quality", "Mean awareness", "Mean stock", "Mean perceived-quality", "Allocated gross", "Allocated label net", "Allocated market net"], summaryRows(enabled, year)), ""
  ]),
  "## Label status at final observed boundary", "",
  "### Control", "", markdownTable(["Year", "Status", "Count"], labelCounts(control).map(row => [row.year, row.status, row.count])), "",
  "### Enabled", "", markdownTable(["Year", "Status", "Count"], labelCounts(enabled).map(row => [row.year, row.status, row.count])), "",
  "## Album composition", "",
  "### Control", "", markdownTable(["Year", "Format", "Released albums", "Units", "Units/released album", "Mean pooled appeal", "Mean reuse count", "Mean freshness"], albumMetrics(control).map(row => [row.year, row.format, row.records, f(row.units), f(row.unitsPerReleasedAlbum), f(row.meanPooledAppeal), f(row.meanReuseCount), f(row.meanFreshness)])), "",
  "### Enabled", "", markdownTable(["Year", "Format", "Released albums", "Units", "Units/released album", "Mean pooled appeal", "Mean reuse count", "Mean freshness"], albumMetrics(enabled).map(row => [row.year, row.format, row.records, f(row.units), f(row.unitsPerReleasedAlbum), f(row.meanPooledAppeal), f(row.meanReuseCount), f(row.meanFreshness)])), "",
  "## Current-year Single source summary", "",
  "### Control", "", markdownTable(["Year", "Artist source", "Records", "Units", "Units/record", "Mean quality"], sourceSummary(control)), "",
  "### Enabled", "", markdownTable(["Year", "Artist source", "Records", "Units", "Units/record", "Mean quality"], sourceSummary(enabled)), "",
  "## Reconciliation", "",
  `The annual economic table is taken directly from annual all-label market-revenue rows. The table below exposes the separately observed final record snapshot subset; it is intentionally not forced to equal market-revenue because records.csv omits records retired before capture.`, "",
  "### Control: market-revenue vs final records snapshot", "", markdownTable(["Year", "Format", "Market-revenue units", "Final-snapshot units", "Snapshot minus market", "Snapshot records"], marketFormatReconciliation(control)), "",
  "### Enabled: market-revenue vs final records snapshot", "", markdownTable(["Year", "Format", "Market-revenue units", "Final-snapshot units", "Snapshot minus market", "Snapshot records"], marketFormatReconciliation(enabled)),
  ""
].join("\n");
if (output) fs.writeFileSync(output, report); else process.stdout.write(report);
