"""Descriptive bounded comparison; no invented economic acceptance tolerance."""
import argparse
import csv
import hashlib
import json
from collections import Counter
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("manifest", type=Path)
parser.add_argument("--baseline-root", type=Path, required=True)
args = parser.parse_args()
manifest = json.loads(args.manifest.read_text(encoding="utf-8-sig"))
root = args.manifest.resolve().parents[2]

def rows(folder, name, suffix):
    with (folder / "SimLogs" / f"{name}-{suffix}.csv").open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))

def hashes(folder, name):
    result = {}
    for path in (folder / "SimLogs").glob(name + "-*.csv"):
        suffix = path.name[len(name) + 1:]
        if suffix.startswith(("scene-identity-", "scene-participation-", "scene-room-", "scene-work-", "scene-recruitment-")):
            continue
        result[suffix] = hashlib.sha256(path.read_bytes()).hexdigest()
    return result

def summary(folder, name):
    population = [r for r in rows(folder, name, "artist-population-weekly") if r["labelTier"] == "All"]
    labor = rows(folder, name, "artist-labor-market-weekly")
    revenue = [r for r in rows(folder, name, "market-revenue") if r["labelTier"] == "All" and r["period"] == "weekly"]
    finance = rows(folder, name, "label-finance")
    last_week = max(int(r["week"]) for r in finance)
    last_labels = [r for r in finance if int(r["week"]) == last_week]
    integrity = {}
    for field in ("ownershipConflicts", "duplicateRosterEntries", "duplicatePoolEntries", "terminalRostered", "terminalReleaseEligible"):
        integrity[field] = max(int(r[field]) for r in population)
    for field in ("duplicateSeekingEntries", "latentUnsignedPoolEntries", "seekingMissingFromUnsignedPool", "prospectStatusContractConflicts"):
        integrity[field] = max(int(r[field]) for r in labor)
    return dict(startRostered=int(population[0]["rostered"]), endRostered=int(population[-1]["rostered"]),
        endRegistry=int(population[-1]["registryTotal"]), formations=int(population[-1]["formedYtd"]),
        firstSignings=sum(int(r["firstTimeSignings"]) for r in population),
        repeatSignings=sum(int(r["reSignings"]) for r in population),
        vacancyWeeks=sum(int(r["affordableHiringVacancies"]) for r in labor),
        endAffordableVacancies=int(labor[-1]["affordableHiringVacancies"]),
        labelStatuses=dict(Counter(r["status"] for r in last_labels)),
        singleUnits=sum(float(r["totalMarketUnits"]) for r in revenue if r["releaseFormat"] == "Single"),
        albumUnits=sum(float(r["totalMarketUnits"]) for r in revenue if r["releaseFormat"] == "Album"),
        marketNet=sum(float(r["marketNet"]) for r in revenue if r["releaseFormat"] == "All"), integrity=integrity)

comparisons = []
for job in manifest["results"]:
    if not job["passed"]:
        raise RuntimeError("Incomplete audit")
    name, seed = job["name"], job["seed"]
    base = f"scene3-matched-v1-rooms-{seed}"
    old, new = hashes(args.baseline_root, base), hashes(root, name)
    changed = [s for s in sorted(old.keys() & new.keys()) if old[s] != new[s]]
    report = dict(name=name, mode=job["mode"], baseline=base,
        identicalEconomicFiles=len(old.keys() & new.keys()) - len(changed), changedEconomicFiles=changed,
        missingFiles=sorted(old.keys() - new.keys()), unexpectedFiles=sorted(new.keys() - old.keys()),
        baselineSummary=summary(args.baseline_root, base), candidateSummary=summary(root, name))
    if job["mode"] == "recruitment":
        records = rows(root, name, "scene-recruitment-end")
        labels = rows(root, name, "scene-recruitment-labels-end")
        report["routes"] = dict(Counter(r["route"] for r in records))
        report["unexplainedSignings"] = sum(r["route"] not in {"Catchment", "RoadCircuit", "NationalAr", "LocalVisitor"} for r in records)
        report["activeEmptyLabels"] = sum(r["active"] == "True" and int(r["rosterSize"]) == 0 for r in labels)
        report["activeUnmappedLabels"] = sum(r["active"] == "True" and not r["hqPlaceId"] for r in labels)
        report["vacantWithNoAccess"] = sum(r["active"] == "True" and int(r["unfilledSlots"]) > 0 and int(r["accessiblePool"]) == 0 for r in labels)
    comparisons.append(report)
off = [r for r in comparisons if r["mode"] == "off"]
result = dict(comparisons=comparisons,
    completedRuns=len(comparisons), expectedRuns=len(manifest.get("jobs", [None] * 3)),
    offControlPassed=bool(off) and all(r["identicalEconomicFiles"] == 84 and not r["changedEconomicFiles"] and not r["missingFiles"] and not r["unexpectedFiles"] for r in off),
    integrityPassed=all(not any(r["candidateSummary"]["integrity"].values()) for r in comparisons),
    longHorizonEconomicAcceptance=False)
out = args.manifest.parent / "analysis.json"
out.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
print(json.dumps(result, indent=2))
raise SystemExit(0 if result["offControlPassed"] and result["integrityPassed"] and
    result["completedRuns"] == result["expectedRuns"] else 1)
