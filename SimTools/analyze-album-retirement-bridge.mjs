#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";
import readline from "node:readline";

const input = process.argv[2];
if (!input) {
  console.error("usage: node SimTools/analyze-album-retirement-bridge.mjs <run-prefix-or-csv>");
  process.exit(2);
}
const file = input.endsWith(".csv")
  ? input
  : path.join("SimLogs", `${input}-album-realization-bridge.csv`);
if (!fs.existsSync(file)) throw new Error(`Album realization bridge not found: ${file}`);

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

const policies = [
  { name: "current", floor: 10, never: 26, charted: 52 },
  { name: "charted45", floor: 10, never: 26, charted: 45 },
  { name: "charted39", floor: 10, never: 26, charted: 39 },
  { name: "charted26", floor: 10, never: 26, charted: 26 },
  { name: "floor15", floor: 15, never: 26, charted: 52 },
  { name: "floor20", floor: 20, never: 26, charted: 52 },
  { name: "floor30", floor: 30, never: 26, charted: 52 },
];

function isRetirable(recordWeek, policy) {
  if (recordWeek.position !== 0 || recordWeek.units >= policy.floor) return false;
  const everCharted = recordWeek.weeksSinceLastCharted < recordWeek.ageWeeks;
  if (!everCharted) return recordWeek.ageWeeks >= policy.never;
  return recordWeek.weeksSinceLastCharted >= policy.charted &&
    recordWeek.weeksSinceSalesAboveFloor >= policy.charted;
}

function getYear(map, year) {
  if (!map.has(year)) {
    map.set(year, {
      observedActual: new Set(),
      byPolicy: new Map(policies.map((policy) => [policy.name, new Set()])),
    });
  }
  return map.get(year);
}

const lines = readline.createInterface({
  input: fs.createReadStream(file),
  crlfDelay: Infinity,
});
let columns;
let current;
const years = new Map();

function finishRecordWeek() {
  if (!current) return;
  const year = getYear(years, current.year);
  if (current.retired) year.observedActual.add(current.recordId);
  for (const policy of policies) {
    if (isRetirable(current, policy)) year.byPolicy.get(policy.name).add(current.recordId);
  }
}

for await (const line of lines) {
  if (!columns) {
    columns = new Map(parseCsvLine(line).map((name, index) => [name, index]));
    continue;
  }
  if (!line) continue;
  const row = parseCsvLine(line);
  const value = (name) => row[columns.get(name)];
  const key = `${value("settlementId")}:${value("recordId")}`;
  if (!current || current.key !== key) {
    finishRecordWeek();
    current = {
      key,
      year: Number(value("year")),
      recordId: value("recordId"),
      ageWeeks: Number(value("ageWeeks")),
      units: 0,
      position: Number(value("currentPosition")),
      weeksSinceLastCharted: Number(value("weeksSinceLastCharted")),
      weeksSinceSalesAboveFloor: Number(value("weeksSinceSalesAboveFloor")),
      retired: value("retiredAfterSettlement") === "true",
    };
  }
  current.units += Number(value("finalCleared"));
}
finishRecordWeek();

console.log(path.basename(file));
for (const [year, summary] of [...years].sort(([left], [right]) => left - right)) {
  const values = policies.map((policy) =>
    `${policy.name}=${summary.byPolicy.get(policy.name).size}`
  );
  console.log(`${year} actual=${summary.observedActual.size} ${values.join(" ")}`);
}
