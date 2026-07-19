#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";
import readline from "node:readline";

const run = process.argv[2];
if (!run) {
  console.error("usage: node SimTools/analyze-album-cohort-clearing.mjs <run-prefix>");
  process.exit(2);
}

const bridgeFile = path.join("SimLogs", `${run}-album-realization-bridge.csv`);
const clearingFile = path.join("SimLogs", `${run}-market-clearing-weekly.csv`);
for (const file of [bridgeFile, clearingFile]) {
  if (!fs.existsSync(file)) throw new Error(`Required input not found: ${file}`);
}

function parseCsvLine(line) {
  const fields = [];
  let field = "";
  let quoted = false;
  for (let index = 0; index < line.length; index += 1) {
    const character = line[index];
    if (character === "\"") {
      if (quoted && line[index + 1] === "\"") {
        field += "\"";
        index += 1;
      } else {
        quoted = !quoted;
      }
    } else if (character === "," && !quoted) {
      fields.push(field);
      field = "";
    } else {
      field += character;
    }
  }
  fields.push(field);
  return fields;
}

const thresholds = [26, 52, 104];
const pressureValues = [1, 2, 3, 4, 6, 8, 10, 12];
const transitionRanges = [
  [0.05, 0.06],
  [0.052, 0.062],
  [0.052, 0.065],
  [0.055, 0.065],
  [0.055, 0.07],
];
const serviceableByMarket = new Map();

{
  const input = fs.createReadStream(bridgeFile);
  const lines = readline.createInterface({ input, crlfDelay: Infinity });
  let column;
  for await (const line of lines) {
    if (!column) {
      column = new Map(parseCsvLine(line).map((name, index) => [name, index]));
      continue;
    }
    const row = parseCsvLine(line);
    const key = `${row[column.get("week")]}:${row[column.get("year")]}:${row[column.get("regionId")]}`;
    let market = serviceableByMarket.get(key);
    if (!market) {
      market = {
        album: 0,
        byThreshold: new Map(thresholds.map((threshold) => [threshold, { young: 0, catalog: 0 }])),
      };
      serviceableByMarket.set(key, market);
    }
    const age = Number(row[column.get("ageWeeks")]);
    const serviceable = Number(row[column.get("serviceableIntent")]);
    market.album += serviceable;
    for (const threshold of thresholds) {
      market.byThreshold.get(threshold)[age < threshold ? "young" : "catalog"] += serviceable;
    }
  }
}

function albumBudget(single, album, capacity, effectiveAlbum) {
  const effectiveTotal = single + effectiveAlbum;
  if (effectiveTotal <= capacity) return Math.min(album, Math.round(effectiveAlbum));
  return Math.min(album, Math.round(capacity * effectiveAlbum / effectiveTotal));
}

function smoothStep(from, to, value) {
  const progress = Math.max(0, Math.min(1, (value - from) / Math.max(Number.EPSILON, to - from)));
  return progress * progress * (3 - 2 * progress);
}

const annual = new Map();
{
  const input = fs.createReadStream(clearingFile);
  const lines = readline.createInterface({ input, crlfDelay: Infinity });
  let column;
  for await (const line of lines) {
    if (!column) {
      column = new Map(parseCsvLine(line).map((name, index) => [name, index]));
      continue;
    }
    const row = parseCsvLine(line);
    const week = row[column.get("week")];
    const year = Number(row[column.get("year")]);
    const region = row[column.get("regionId")];
    const key = `${week}:${year}:${region}`;
    const single = Number(row[column.get("serviceableSingleIntent")]);
    const album = Number(row[column.get("serviceableAlbumIntent")]);
    const capacity = Number(row[column.get("purchaseCapacity")]);
    const cohorts = serviceableByMarket.get(key) ?? {
      album: 0,
      byThreshold: new Map(thresholds.map((threshold) => [threshold, { young: 0, catalog: 0 }])),
    };
    if (album !== cohorts.album) {
      throw new Error(`Album serviceable mismatch for ${key}: clearing=${album}, bridge=${cohorts.album}`);
    }
    let result = annual.get(year);
    if (!result) {
      result = {
        actual: 0,
        album: 0,
        replay: new Map(),
        transitionReplay: new Map(),
      };
      annual.set(year, result);
    }
    result.actual += Number(row[column.get("clearedAlbumUnits")]);
    result.album += album;
    for (const threshold of thresholds) {
      const { young, catalog } = cohorts.byThreshold.get(threshold);
      for (const pressure of pressureValues) {
        const effectiveCatalog = catalog * capacity / Math.max(1, capacity + pressure * catalog);
        const effectiveAlbum = young + effectiveCatalog;
        const replay = albumBudget(single, album, capacity, effectiveAlbum);
        const replayKey = `${threshold}:${pressure}`;
        result.replay.set(replayKey, (result.replay.get(replayKey) ?? 0) + replay);
      }
    }
    const { young, catalog } = cohorts.byThreshold.get(104);
    const allOverlap = album * capacity / Math.max(1, capacity + 2 * album);
    const allOverlapFactor = album > 0 ? allOverlap / album : 0;
    const distinctCatalog = catalog * capacity / Math.max(1, capacity + 2 * catalog);
    const share = album / Math.max(1, single + album);
    for (const [start, full] of transitionRanges) {
      const maturity = smoothStep(start, full, share);
      const effectiveYoung = young * allOverlapFactor * (1 - maturity) + young * maturity;
      const effectiveCatalog = catalog * allOverlapFactor * (1 - maturity) + distinctCatalog * maturity;
      const replay = albumBudget(single, album, capacity, effectiveYoung + effectiveCatalog);
      const replayKey = `${start}:${full}`;
      result.transitionReplay.set(replayKey, (result.transitionReplay.get(replayKey) ?? 0) + replay);
    }
  }
}

for (const [year, result] of [...annual.entries()].sort(([left], [right]) => left - right)) {
  console.log(`\n${year} actual=${result.actual} serviceable=${result.album}`);
  console.log("threshold pressure replay delta ratioToActual");
  for (const threshold of thresholds) {
    for (const pressure of pressureValues) {
      const replay = result.replay.get(`${threshold}:${pressure}`);
      console.log([
        String(threshold).padStart(9),
        String(pressure).padStart(8),
        String(replay).padStart(10),
        String(replay - result.actual).padStart(10),
        (replay / Math.max(1, result.actual)).toFixed(4).padStart(13),
      ].join(" "));
    }
  }
  console.log("transitionStart transitionFull replay delta ratioToActual");
  for (const [start, full] of transitionRanges) {
    const replay = result.transitionReplay.get(`${start}:${full}`);
    console.log([
      start.toFixed(3).padStart(15),
      full.toFixed(3).padStart(14),
      String(replay).padStart(10),
      String(replay - result.actual).padStart(10),
      (replay / Math.max(1, result.actual)).toFixed(4).padStart(13),
    ].join(" "));
  }
}
