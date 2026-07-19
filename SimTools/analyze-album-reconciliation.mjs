import fs from "node:fs";
import readline from "node:readline";

function parseCsv(line) {
	const fields = [];
	let field = "";
	let quoted = false;
	for (let i = 0; i < line.length; i++) {
		const character = line[i];
		if (character === '"') {
			if (quoted && line[i + 1] === '"') {
				field += '"';
				i++;
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

async function forEachCsvRow(path, visitor) {
	const input = readline.createInterface({
		input: fs.createReadStream(path),
		crlfDelay: Infinity
	});
	let columns;
	let indexes;
	for await (const line of input) {
		if (!columns) {
			columns = parseCsv(line);
			indexes = Object.fromEntries(columns.map((column, index) => [column, index]));
			continue;
		}
		if (line.length > 0) visitor(parseCsv(line), indexes);
	}
}

function number(row, indexes, name) {
	return Number(row[indexes[name]]);
}

function increment(map, key, create, update) {
	let value = map.get(key);
	if (!value) {
		value = create();
		map.set(key, value);
	}
	update(value);
}

function mean(sum, count) {
	return count === 0 ? 0 : sum / count;
}

const prefix = process.argv[2];
if (!prefix) {
	console.error("usage: node SimTools/analyze-album-reconciliation.mjs <SimLogs/run-prefix>");
	process.exitCode = 2;
} else {
	const demandPath = `${prefix}-album-demand-explanation.csv`;
	const settlementPath = `${prefix}-completed-week-settlement.csv`;
	const recordsPath = `${prefix}-records.csv`;
	const demand = new Map();
	const settlement = new Map();
	const albumRecordFirstWeek = new Map();
	const albumRecordIds = new Set();

	await forEachCsvRow(demandPath, (row, indexes) => {
		const year = row[indexes.year];
		const genre = row[indexes.genre];
		const key = `${year}|${genre}`;
		const tilt = number(row, indexes, "formatTilt");
		const normalization = number(row, indexes, "opportunityNormalization");
		increment(demand, key,
			() => ({ year: Number(year), genre, rows: 0, tilt: 0, normalization: 0, minTilt: Infinity, maxTilt: -Infinity }),
			value => {
				value.rows++;
				value.tilt += tilt;
				value.normalization += normalization;
				value.minTilt = Math.min(value.minTilt, tilt);
				value.maxTilt = Math.max(value.maxTilt, tilt);
			});
	});

	await forEachCsvRow(settlementPath, (row, indexes) => {
		if (row[indexes.format] !== "Album") return;
		const recordId = row[indexes.recordId];
		const week = number(row, indexes, "week");
		albumRecordIds.add(recordId);
		if (!albumRecordFirstWeek.has(recordId)) albumRecordFirstWeek.set(recordId, week);
		const year = row[indexes.year];
		const genre = row[indexes.genre];
		const key = `${year}|${genre}`;
		const units = number(row, indexes, "totalUnits");
		const gross = number(row, indexes, "gross");
		increment(settlement, key,
			() => ({ year: Number(year), genre, rows: 0, units: 0, gross: 0, records: new Set() }),
			value => {
				value.rows++;
				value.units += units;
				value.gross += gross;
				value.records.add(recordId);
			});
	});

	const initialAlbumAge = new Map();
	await forEachCsvRow(recordsPath, (row, indexes) => {
		const week = number(row, indexes, "week");
		if (week !== 1) return;
		const recordId = row[indexes.recordId];
		if (albumRecordIds.has(recordId)) initialAlbumAge.set(recordId, number(row, indexes, "weeksSinceRelease"));
	});

	const catalog = new Map();
	await forEachCsvRow(settlementPath, (row, indexes) => {
		if (row[indexes.format] !== "Album") return;
		const recordId = row[indexes.recordId];
		const week = number(row, indexes, "week");
		const age = initialAlbumAge.has(recordId)
			? initialAlbumAge.get(recordId) + week - 1
			: week - albumRecordFirstWeek.get(recordId);
		const cohort = age >= 104 ? "104+" : age >= 52 ? "52-103" : "0-51";
		const year = number(row, indexes, "year");
		increment(catalog, `${year}|${cohort}`,
			() => ({ year, cohort, units: 0, gross: 0, recordWeeks: 0, records: new Set() }),
			value => {
				value.units += number(row, indexes, "totalUnits");
				value.gross += number(row, indexes, "gross");
				value.recordWeeks++;
				value.records.add(recordId);
			});
	});

	const rows = [...new Set([...demand.keys(), ...settlement.keys()])]
		.map(key => {
			const d = demand.get(key);
			const s = settlement.get(key);
			return {
				year: d?.year ?? s.year,
				genre: d?.genre ?? s.genre,
				albumUnits: s?.units ?? 0,
				albumGross: s?.gross ?? 0,
				recordCount: s?.records.size ?? 0,
				demandSampleRows: d?.rows ?? 0,
				meanFormatTilt: mean(d?.tilt ?? 0, d?.rows ?? 0),
				minFormatTilt: d?.minTilt ?? 0,
				maxFormatTilt: d?.maxTilt ?? 0,
				meanBuyerPoolNormalization: mean(d?.normalization ?? 0, d?.rows ?? 0)
			};
		})
		.sort((left, right) => left.year - right.year || right.albumUnits - left.albumUnits || left.genre.localeCompare(right.genre));

	const annual = new Map();
	for (const row of rows) {
		increment(annual, row.year,
			() => ({ units: 0, gross: 0, sampledUnits: 0, tiltUnitWeight: 0, normalizationUnitWeight: 0 }),
			value => {
				value.units += row.albumUnits;
				value.gross += row.albumGross;
				if (row.demandSampleRows > 0) {
					value.sampledUnits += row.albumUnits;
					value.tiltUnitWeight += row.albumUnits * row.meanFormatTilt;
					value.normalizationUnitWeight += row.albumUnits * row.meanBuyerPoolNormalization;
				}
			});
	}

	console.log("NOTE: format-tilt and buyer-pool values are sampled Top-40/launch diagnostics, weighted by full-settlement genre units only across genres with sample coverage; they are not population-complete causal estimates.");
	console.log("NOTE: age cohorts infer release age from week-1 records and first settlement, so use the authoritative cohort analyzer for formal reconciliation.");
	console.log("year,albumUnits,albumGross,sampledGenreUnitCoverage,sampledGenreUnitWeightedMeanFormatTilt,sampledGenreUnitWeightedMeanBuyerPoolNormalization");
	for (const [year, value] of [...annual].sort(([left], [right]) => left - right)) {
		console.log([
			year,
			value.units,
			value.gross.toFixed(2),
			mean(value.sampledUnits, value.units).toFixed(6),
			mean(value.tiltUnitWeight, value.sampledUnits).toFixed(6),
			mean(value.normalizationUnitWeight, value.sampledUnits).toFixed(6)
		].join(","));
	}

	console.log("\nyear,ageCohort,albumUnits,albumGross,activeRecordWeeks,distinctRecords");
	for (const value of [...catalog.values()].sort((left, right) =>
		left.year - right.year || left.cohort.localeCompare(right.cohort))) {
		console.log([
			value.year,
			value.cohort,
			value.units,
			value.gross.toFixed(2),
			value.recordWeeks,
			value.records.size
		].join(","));
	}

	if (!process.argv.includes("--annual-only")) {
		console.log("\nyear,genre,albumUnits,albumGross,recordCount,demandSampleRows,meanFormatTilt,minFormatTilt,maxFormatTilt,meanBuyerPoolNormalization");
		for (const row of rows) {
			console.log([
				row.year,
				row.genre,
				row.albumUnits,
				row.albumGross.toFixed(2),
				row.recordCount,
				row.demandSampleRows,
				row.meanFormatTilt.toFixed(6),
				row.minFormatTilt.toFixed(6),
				row.maxFormatTilt.toFixed(6),
				row.meanBuyerPoolNormalization.toFixed(6)
			].join(","));
		}
	}
}
