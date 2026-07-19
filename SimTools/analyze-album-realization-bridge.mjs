#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";
import readline from "node:readline";

const inputs = process.argv.slice(2);
if (inputs.length === 0) {
  console.error("usage: node SimTools/analyze-album-realization-bridge.mjs <run-prefix-or-csv> [...]");
  process.exit(2);
}

function resolveInput(input) {
  const candidate = input.endsWith(".csv")
    ? input
    : path.join("SimLogs", `${input}-album-realization-bridge.csv`);
  if (!fs.existsSync(candidate)) {
    throw new Error(`Album realization bridge not found: ${candidate}`);
  }
  return candidate;
}

function parseCsvLine(line) {
  const fields = [];
  let field = "";
  let quoted = false;
  for (let i = 0; i < line.length; i += 1) {
    const char = line[i];
    if (char === "\"") {
      if (quoted && line[i + 1] === "\"") {
        field += "\"";
        i += 1;
      } else {
        quoted = !quoted;
      }
    } else if (char === "," && !quoted) {
      fields.push(field);
      field = "";
    } else {
      field += char;
    }
  }
  fields.push(field);
  return fields;
}

function ageBand(ageWeeks) {
  if (ageWeeks < 26) return "00-25";
  if (ageWeeks < 52) return "26-51";
  if (ageWeeks < 104) return "52-103";
  return "104+";
}

function emptyAggregate() {
  return {
    rows: 0,
    recordWeeks: new Set(),
    records: new Set(),
    retiredRecordWeeks: new Set(),
    buyerPool: 0,
    rawBefore: 0,
    rawAfter: 0,
    rounded: 0,
    serviceable: 0,
    cleared: 0,
    formatTiltWeighted: 0,
    exhaustionWeighted: 0,
    effectivePenetrationWeighted: 0,
    clearedAtDecay: new Map([[0.984, 0], [0.983, 0], [0.982, 0], [0.98, 0]]),
  };
}

function addRow(aggregate, row, column) {
  const number = (name) => Number(row[column.get(name)]);
  const rawBefore = number("rawDemandBeforeCannibalization");
  aggregate.rows += 1;
  aggregate.recordWeeks.add(`${row[column.get("settlementId")]}:${row[column.get("recordId")]}`);
  aggregate.records.add(row[column.get("recordId")]);
  if (row[column.get("retiredAfterSettlement")] === "true") {
    aggregate.retiredRecordWeeks.add(
      `${row[column.get("settlementId")]}:${row[column.get("recordId")]}`
    );
  }
  aggregate.buyerPool += number("buyerPool");
  aggregate.rawBefore += rawBefore;
  aggregate.rawAfter += number("rawDemandAfterCannibalization");
  aggregate.rounded += number("roundedRawIntent");
  aggregate.serviceable += number("serviceableIntent");
  aggregate.cleared += number("finalCleared");
  aggregate.formatTiltWeighted += number("formatTilt") * rawBefore;
  aggregate.exhaustionWeighted += number("exhaustion") * rawBefore;
  aggregate.effectivePenetrationWeighted += number("effectivePenetration") * rawBefore;
  const ageWeeks = number("ageWeeks");
  const catalogWeeks = Math.max(0, ageWeeks - 26);
  for (const [weeklyDecay, estimate] of aggregate.clearedAtDecay) {
    const relativeDecay = Math.pow(weeklyDecay / 0.985, catalogWeeks);
    aggregate.clearedAtDecay.set(
      weeklyDecay,
      estimate + number("finalCleared") * relativeDecay
    );
  }
}

async function analyze(file) {
  const stream = fs.createReadStream(file);
  const lines = readline.createInterface({ input: stream, crlfDelay: Infinity });
  let column;
  const aggregates = new Map();
  for await (const line of lines) {
    if (!column) {
      column = new Map(parseCsvLine(line).map((name, index) => [name, index]));
      continue;
    }
    if (!line) continue;
    const row = parseCsvLine(line);
    const year = row[column.get("year")];
    const band = ageBand(Number(row[column.get("ageWeeks")]));
    for (const key of [`${year}:ALL`, `${year}:${band}`]) {
      if (!aggregates.has(key)) aggregates.set(key, emptyAggregate());
      addRow(aggregates.get(key), row, column);
    }
  }
  return aggregates;
}

function fixed(value, digits = 3) {
  return Number.isFinite(value) ? value.toFixed(digits) : "n/a";
}

function percent(numerator, denominator) {
  return denominator === 0 ? "n/a" : `${fixed((100 * numerator) / denominator, 2)}%`;
}

function printAggregate(year, band, aggregate) {
  const cannibalizationLoss = aggregate.rawBefore - aggregate.rawAfter;
  const roundingDelta = aggregate.rawAfter - aggregate.rounded;
  const serviceLoss = aggregate.rounded - aggregate.serviceable;
  const clearingLoss = aggregate.serviceable - aggregate.cleared;
  const fields = [
      year,
      band.padEnd(6),
      `records=${aggregate.records.size}`,
      `recordWeeks=${aggregate.recordWeeks.size}`,
      `retired=${aggregate.retiredRecordWeeks.size}`,
      `raw=${fixed(aggregate.rawBefore, 0)}`,
      `cleared=${fixed(aggregate.cleared, 0)}`,
      `realization=${percent(aggregate.cleared, aggregate.rawBefore)}`,
      `cannibLoss=${fixed(cannibalizationLoss, 0)}`,
      `roundDelta=${fixed(roundingDelta, 0)}`,
      `serviceLoss=${fixed(serviceLoss, 0)}`,
      `clearingLoss=${fixed(clearingLoss, 0)}`,
      `tilt(raw-w)=${fixed(aggregate.formatTiltWeighted / aggregate.rawBefore, 4)}`,
      `exhaust(raw-w)=${fixed(aggregate.exhaustionWeighted / aggregate.rawBefore, 4)}`,
      `penetration(raw-w)=${fixed(aggregate.effectivePenetrationWeighted / aggregate.rawBefore, 4)}`,
    ];
  if (band === "ALL") {
    fields.push(
      `staticDecayUnits(.984/.983/.982/.980)=${[...aggregate.clearedAtDecay.values()]
        .map((value) => fixed(value, 0))
        .join("/")}`
    );
  }
  console.log(fields.join(" "));
}

for (const input of inputs) {
  const file = resolveInput(input);
  const aggregates = await analyze(file);
  console.log(`\n${path.basename(file)}`);
  const years = [...new Set([...aggregates.keys()].map((key) => key.split(":")[0]))].sort();
  for (const year of years) {
    for (const band of ["ALL", "00-25", "26-51", "52-103", "104+"]) {
      const aggregate = aggregates.get(`${year}:${band}`);
      if (aggregate) printAggregate(year, band, aggregate);
    }
  }
}
