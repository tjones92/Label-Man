import fs from "node:fs";
import path from "node:path";
import readline from "node:readline";

/*
 * Compare label survival and release participation for one completed calendar
 * year using immutable ChartAuditRunner CSV artifacts.
 *
 * Usage:
 *   node SimTools/analyze-label-survival-participation.mjs \
 *     <control-run> <candidate-run> [--year 1964] [--json]
 */

const args = process.argv.slice(2);
const controlRun = args[0];
const candidateRun = args[1];
if (!controlRun || !candidateRun) {
  throw new Error("Expected <control-run> <candidate-run> [--year 1964] [--json]");
}

const option = name => {
  const index = args.indexOf(name);
  return index >= 0 ? args[index + 1] : undefined;
};
const targetYear = Number(option("--year") ?? 1964);
const jsonOnly = args.includes("--json");
const logDirectory = path.resolve("SimLogs");
const closedStatuses = new Set(["Bankrupt", "Defunct", "Acquired"]);
const tiers = ["Major", "MidTier", "Independent", "Small", "Boutique"];

function prefix(run) {
  return path.isAbsolute(run) ? run : path.join(logDirectory, run);
}

function splitCsv(line) {
  const values = [];
  let value = "";
  let quoted = false;
  for (let index = 0; index < line.length; index++) {
    const character = line[index];
    if (character === '"') {
      if (quoted && line[index + 1] === '"') {
        value += character;
        index++;
      } else {
        quoted = !quoted;
      }
    } else if (character === "," && !quoted) {
      values.push(value);
      value = "";
    } else {
      value += character;
    }
  }
  values.push(value);
  return values;
}

async function forEachCsv(file, visit) {
  if (!fs.existsSync(file)) throw new Error(`Missing telemetry: ${file}`);
  const input = fs.createReadStream(file, { encoding: "utf8" });
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

function number(value) {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}

function get(map, key, create) {
  if (!map.has(key)) map.set(key, create());
  return map.get(key);
}

function quantile(values, probability) {
  if (!values.length) return null;
  const ordered = [...values].sort((left, right) => left - right);
  const index = (ordered.length - 1) * probability;
  const lower = Math.floor(index);
  const upper = Math.ceil(index);
  if (lower === upper) return ordered[lower];
  return ordered[lower] + (ordered[upper] - ordered[lower]) * (index - lower);
}

async function loadLaunchDirectory(base) {
  const launch = new Map();
  await forEachCsv(`${base}-label-directory.csv`, row => {
    launch.set(row.labelId, { initialTier: row.initialTier, isHistorical: row.isHistorical === "true" });
  });
  return launch;
}

async function analyzeRun(run) {
  const base = prefix(run);
  const launch = await loadLaunchDirectory(base);
  const labels = new Map();
  const weekly = new Map();
  const closures = [];

  await forEachCsv(`${base}-label-finance.csv`, row => {
    const week = number(row.week);
    const year = number(row.year);
    const active = !closedStatuses.has(row.status);
    const label = get(labels, row.labelId, () => ({
      labelId: row.labelId,
      origin: launch.has(row.labelId) ? "LaunchPopulation" : "RuntimeFounded",
      initialTier: launch.get(row.labelId)?.initialTier ?? row.labelTier,
      birthWeek: week,
      birthYear: year,
      priorActive: undefined,
      closureWeek: null,
      closureYear: null,
      closureStatus: null,
      targetActiveWeeks: 0,
      targetFirstTier: null,
      targetLastTier: null,
      targetLastStatus: null
    }));

    if (label.priorActive === true && !active && label.closureWeek === null) {
      label.closureWeek = week;
      label.closureYear = year;
      label.closureStatus = row.status;
      closures.push(label);
    }
    label.priorActive = active;

    if (year !== targetYear) return;
    const weekState = get(weekly, week, () => ({
      week,
      active: 0,
      byTier: Object.fromEntries(tiers.map(tier => [tier, 0])),
      byOrigin: { LaunchPopulation: 0, RuntimeFounded: 0 }
    }));
    if (!active) return;
    weekState.active++;
    if (weekState.byTier[row.labelTier] !== undefined) weekState.byTier[row.labelTier]++;
    weekState.byOrigin[label.origin]++;
    label.targetActiveWeeks++;
    label.targetFirstTier ??= row.labelTier;
    label.targetLastTier = row.labelTier;
    label.targetLastStatus = row.status;
  });

  const decisionsByLabel = new Map();
  let decisions = 0;
  const decisionLabelsByTier = new Map(tiers.map(tier => [tier, new Set()]));
  const decisionsByTier = Object.fromEntries(tiers.map(tier => [tier, 0]));
  const decisionsByOrigin = { LaunchPopulation: 0, RuntimeFounded: 0 };
  const decisionLabelsByOrigin = {
    LaunchPopulation: new Set(),
    RuntimeFounded: new Set()
  };

  await forEachCsv(`${base}-release-strategy.csv`, row => {
    if (number(row.year) !== targetYear) return;
    const label = labels.get(row.labelId);
    const origin = label?.origin ?? (launch.has(row.labelId) ? "LaunchPopulation" : "RuntimeFounded");
    decisions++;
    decisionsByTier[row.tier] = (decisionsByTier[row.tier] ?? 0) + 1;
    get(decisionLabelsByTier, row.tier, () => new Set()).add(row.labelId);
    decisionsByOrigin[origin]++;
    decisionLabelsByOrigin[origin].add(row.labelId);
    const item = get(decisionsByLabel, row.labelId, () => ({
      labelId: row.labelId,
      origin,
      decisions: 0,
      tierCounts: new Map()
    }));
    item.decisions++;
    item.tierCounts.set(row.tier, (item.tierCounts.get(row.tier) ?? 0) + 1);
  });

  let releaseRolls = 0;
  let successfulReleases = 0;
  await forEachCsv(`${base}-release-capacity.csv`, row => {
    if (number(row.year) !== targetYear) return;
    releaseRolls += number(row.releaseRollsFired);
    successfulReleases += number(row.successfulReleases);
  });

  const weeks = [...weekly.values()].sort((left, right) => left.week - right.week);
  if (!weeks.length) throw new Error(`${run} has no label-finance rows for ${targetYear}`);
  const firstWeek = weeks[0].week;
  const endWeek = weeks.at(-1).week;
  const mean = values => values.reduce((sum, value) => sum + value, 0) / values.length;
  const activeMean = mean(weeks.map(row => row.active));
  const activeLabelIds = [...labels.values()].filter(label => label.targetActiveWeeks > 0);
  const participants = [...decisionsByLabel.values()];
  const participantDecisionCounts = participants.map(label => label.decisions);

  const cohort = origin => {
    const members = [...labels.values()].filter(label => label.origin === origin);
    const bornThroughTarget = members.filter(label => label.birthYear <= targetYear);
    const activeInTarget = bornThroughTarget.filter(label => label.targetActiveWeeks > 0);
    const activeAtEnd = activeInTarget.filter(label => label.targetActiveWeeks && label.closureWeek !== null
      ? label.closureWeek > endWeek
      : label.targetLastStatus && !closedStatuses.has(label.targetLastStatus));
    const targetClosures = members.filter(label => label.closureYear === targetYear);
    return {
      bornThroughTarget: bornThroughTarget.length,
      bornInTarget: members.filter(label => label.birthYear === targetYear).length,
      activeLabelWeeks: activeInTarget.reduce((sum, label) => sum + label.targetActiveWeeks, 0),
      meanActive: mean(weeks.map(row => row.byOrigin[origin])),
      activeAtStart: weeks[0].byOrigin[origin],
      activeAtEnd: weeks.at(-1).byOrigin[origin],
      closuresInTarget: targetClosures.length,
      closureStatus: Object.fromEntries([...new Set(targetClosures.map(label => label.closureStatus))]
        .sort().map(status => [status, targetClosures.filter(label => label.closureStatus === status).length])),
      participants: decisionLabelsByOrigin[origin].size,
      decisions: decisionsByOrigin[origin],
      decisionsPerMeanActive: decisionsByOrigin[origin] / mean(weeks.map(row => row.byOrigin[origin])),
      decisionsPerParticipant: decisionLabelsByOrigin[origin].size
        ? decisionsByOrigin[origin] / decisionLabelsByOrigin[origin].size
        : null,
      activeAtEndReconciled: activeAtEnd.length
    };
  };

  const yearlyFlow = {};
  for (let year = 1960; year <= targetYear; year++) {
    yearlyFlow[year] = {
      births: [...labels.values()].filter(label => label.birthYear === year && label.origin === "RuntimeFounded").length,
      launchClosures: closures.filter(label => label.closureYear === year && label.origin === "LaunchPopulation").length,
      runtimeClosures: closures.filter(label => label.closureYear === year && label.origin === "RuntimeFounded").length
    };
  }

  return {
    run,
    year: targetYear,
    firstWeek,
    endWeek,
    weeksObserved: weeks.length,
    active: {
      mean: activeMean,
      min: Math.min(...weeks.map(row => row.active)),
      max: Math.max(...weeks.map(row => row.active)),
      start: weeks[0].active,
      end: weeks.at(-1).active,
      uniqueDuringYear: activeLabelIds.length
    },
    tierMeanActive: Object.fromEntries(tiers.map(tier => [tier, mean(weeks.map(row => row.byTier[tier]))])),
    cohorts: {
      LaunchPopulation: cohort("LaunchPopulation"),
      RuntimeFounded: cohort("RuntimeFounded")
    },
    yearlyFlow,
    release: {
      decisions,
      participants: decisionsByLabel.size,
      participantShareOfUniqueActive: decisionsByLabel.size / activeLabelIds.length,
      decisionsPerMeanActive: decisions / activeMean,
      decisionsPerParticipant: decisions / decisionsByLabel.size,
      decisionsByTier,
      participantsByTier: Object.fromEntries([...decisionLabelsByTier].map(([tier, ids]) => [tier, ids.size])),
      decisionsByOrigin,
      participantsByOrigin: Object.fromEntries(Object.entries(decisionLabelsByOrigin).map(([origin, ids]) => [origin, ids.size])),
      distribution: {
        min: Math.min(...participantDecisionCounts),
        p25: quantile(participantDecisionCounts, 0.25),
        median: quantile(participantDecisionCounts, 0.5),
        p75: quantile(participantDecisionCounts, 0.75),
        max: Math.max(...participantDecisionCounts)
      },
      releaseRolls,
      successfulReleases,
      successRate: successfulReleases / releaseRolls
    }
  };
}

function ratio(candidate, control) {
  return control ? candidate / control : null;
}

function difference(candidate, control) {
  return candidate - control;
}

function comparison(control, candidate) {
  const activeGap = difference(candidate.active.mean, control.active.mean);
  const launchGap = difference(candidate.cohorts.LaunchPopulation.meanActive, control.cohorts.LaunchPopulation.meanActive);
  const runtimeGap = difference(candidate.cohorts.RuntimeFounded.meanActive, control.cohorts.RuntimeFounded.meanActive);
  const participantGap = difference(candidate.release.participants, control.release.participants);
  const launchParticipantGap = difference(
    candidate.release.participantsByOrigin.LaunchPopulation,
    control.release.participantsByOrigin.LaunchPopulation
  );
  const runtimeParticipantGap = difference(
    candidate.release.participantsByOrigin.RuntimeFounded,
    control.release.participantsByOrigin.RuntimeFounded
  );
  return {
    activeMeanRatio: ratio(candidate.active.mean, control.active.mean),
    activeMeanGap: activeGap,
    activeGapByOrigin: {
      LaunchPopulation: launchGap,
      RuntimeFounded: runtimeGap
    },
    activeGapShareByOrigin: {
      LaunchPopulation: launchGap / activeGap,
      RuntimeFounded: runtimeGap / activeGap
    },
    participantRatio: ratio(candidate.release.participants, control.release.participants),
    participantGap,
    participantGapByOrigin: {
      LaunchPopulation: launchParticipantGap,
      RuntimeFounded: runtimeParticipantGap
    },
    participantGapShareByOrigin: {
      LaunchPopulation: launchParticipantGap / participantGap,
      RuntimeFounded: runtimeParticipantGap / participantGap
    },
    decisionRatio: ratio(candidate.release.decisions, control.release.decisions),
    decisionsPerMeanActiveRatio: ratio(candidate.release.decisionsPerMeanActive, control.release.decisionsPerMeanActive),
    decisionsPerParticipantRatio: ratio(candidate.release.decisionsPerParticipant, control.release.decisionsPerParticipant),
    successRateDifference: candidate.release.successRate - control.release.successRate
  };
}

function percent(value) {
  return `${(value * 100).toFixed(2)}%`;
}

function fixed(value, digits = 2) {
  return Number.isFinite(value) ? value.toFixed(digits) : "n/a";
}

function table(headers, rows) {
  return [
    `| ${headers.join(" | ")} |`,
    `| ${headers.map(() => "---").join(" | ")} |`,
    ...rows.map(row => `| ${row.join(" | ")} |`)
  ].join("\n");
}

function markdown(control, candidate, compared) {
  const lines = [
    `# ${targetYear} label survival and release-participation review`,
    "",
    `Control: \`${control.run}\`  `,
    `Candidate: \`${candidate.run}\``,
    "",
    "## Headline",
    "",
    `The candidate averaged ${fixed(candidate.active.mean)} active labels versus ${fixed(control.active.mean)} control ` +
      `(${fixed(compared.activeMeanRatio, 4)}x, +${fixed(compared.activeMeanGap)}). ` +
      `${percent(compared.activeGapShareByOrigin.LaunchPopulation)} of that mean-active gap came from launch-population labels and ` +
      `${percent(compared.activeGapShareByOrigin.RuntimeFounded)} from runtime-founded labels.`,
    "",
    `It had ${candidate.release.participants} annual release participants versus ${control.release.participants} ` +
      `(${fixed(compared.participantRatio, 4)}x, +${compared.participantGap}). ` +
      `${percent(compared.participantGapShareByOrigin.LaunchPopulation)} of the participant gap came from launch-population labels and ` +
      `${percent(compared.participantGapShareByOrigin.RuntimeFounded)} from runtime-founded labels.`,
    "",
    "## Active population by origin",
    "",
    table(
      ["Run", "Origin", "Mean active", "Start", "End", `Born ${targetYear}`, `Closed ${targetYear}`, "Participants", "Decisions"],
      [control, candidate].flatMap(run => ["LaunchPopulation", "RuntimeFounded"].map(origin => {
        const row = run.cohorts[origin];
        return [
          run === control ? "Control" : "Candidate",
          origin,
          fixed(row.meanActive),
          row.activeAtStart,
          row.activeAtEnd,
          row.bornInTarget,
          row.closuresInTarget,
          row.participants,
          row.decisions
        ];
      }))
    ),
    "",
    "## Annual birth/closure flow",
    "",
    table(
      ["Year", "Control births", "Candidate births", "Control launch closures", "Candidate launch closures", "Control runtime closures", "Candidate runtime closures"],
      Object.keys(control.yearlyFlow).map(year => [
        year,
        control.yearlyFlow[year].births,
        candidate.yearlyFlow[year].births,
        control.yearlyFlow[year].launchClosures,
        candidate.yearlyFlow[year].launchClosures,
        control.yearlyFlow[year].runtimeClosures,
        candidate.yearlyFlow[year].runtimeClosures
      ])
    ),
    "",
    "## Release participation",
    "",
    table(
      ["Metric", "Control", "Candidate", "Candidate/control"],
      [
        ["Mean active labels", fixed(control.active.mean), fixed(candidate.active.mean), fixed(compared.activeMeanRatio, 4)],
        ["Distinct participating labels", control.release.participants, candidate.release.participants, fixed(compared.participantRatio, 4)],
        ["Release decisions", control.release.decisions, candidate.release.decisions, fixed(compared.decisionRatio, 4)],
        ["Decisions / mean active", fixed(control.release.decisionsPerMeanActive), fixed(candidate.release.decisionsPerMeanActive), fixed(compared.decisionsPerMeanActiveRatio, 4)],
        ["Decisions / participant", fixed(control.release.decisionsPerParticipant), fixed(candidate.release.decisionsPerParticipant), fixed(compared.decisionsPerParticipantRatio, 4)],
        ["Capacity success rate", percent(control.release.successRate), percent(candidate.release.successRate), `${(compared.successRateDifference * 100).toFixed(2)} pp`]
      ]
    ),
    "",
    "Participant release-decision distribution (min / p25 / median / p75 / max):",
    "",
    `- Control: ${[
      control.release.distribution.min,
      control.release.distribution.p25,
      control.release.distribution.median,
      control.release.distribution.p75,
      control.release.distribution.max
    ].map(value => fixed(value)).join(" / ")}`,
    "",
    `- Candidate: ${[
      candidate.release.distribution.min,
      candidate.release.distribution.p25,
      candidate.release.distribution.median,
      candidate.release.distribution.p75,
      candidate.release.distribution.max
    ].map(value => fixed(value)).join(" / ")}`,
    "",
    "## Tier detail",
    "",
    table(
      ["Tier", "Control mean active", "Candidate mean active", "Control participants", "Candidate participants", "Control decisions", "Candidate decisions"],
      tiers.map(tier => [
        tier,
        fixed(control.tierMeanActive[tier]),
        fixed(candidate.tierMeanActive[tier]),
        control.release.participantsByTier[tier],
        candidate.release.participantsByTier[tier],
        control.release.decisionsByTier[tier],
        candidate.release.decisionsByTier[tier]
      ])
    )
  ];
  return lines.join("\n");
}

const control = await analyzeRun(controlRun);
const candidate = await analyzeRun(candidateRun);
const compared = comparison(control, candidate);
const result = { control, candidate, comparison: compared };
console.log(jsonOnly ? JSON.stringify(result, null, 2) : markdown(control, candidate, compared));
