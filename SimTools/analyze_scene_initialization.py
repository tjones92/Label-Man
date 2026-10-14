"""Compare opening allocation and tier outcomes against the prior Phase 4 candidate."""
import argparse
import csv
import json
from collections import Counter
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("manifest", type=Path)
parser.add_argument("--previous-tag", default="scene4-audit-v1")
args = parser.parse_args()
manifest = json.loads(args.manifest.read_text(encoding="utf-8-sig"))
root = args.manifest.resolve().parents[2] / "SimLogs"

def read(name, suffix):
    with (root / f"{name}-{suffix}.csv").open(encoding="utf-8-sig", newline="") as f:
        return list(csv.DictReader(f))

results = []
for run in manifest["results"]:
    if not run["passed"]:
        raise RuntimeError("Incomplete run")
    if run["mode"] != "recruitment":
        continue
    name, seed = run["name"], run["seed"]
    old = read(f"{args.previous_tag}-recruitment-{seed}", "scene-recruitment-labels-start")
    new = read(name, "scene-initialization-labels")
    appointments = read(name, "scene-initialization-appointments")
    tiers = {}
    for tier in sorted({r["tier"] for r in new}):
        before = [r for r in old if r["tier"] == tier]
        after = [r for r in new if r["tier"] == tier]
        count = sum(int(r["filled"]) for r in after)
        tiers[tier] = dict(labels=len(after),
            previousFilled=sum(int(r["rosterSize"]) for r in before), filled=count,
            previousEmpty=sum(int(r["rosterSize"]) == 0 for r in before),
            empty=sum(int(r["filled"]) == 0 for r in after),
            desired=sum(int(r["target"]) for r in after),
            meanTrueQuality=sum(float(r["meanTrueQuality"]) * int(r["filled"]) for r in after) / max(1, count),
            strongActs=sum(int(r["strongActs"]) for r in after))
    label_tiers = {r["labelId"]: r["tier"] for r in new}
    rank = {"Major": 0, "MidTier": 1, "Independent": 2, "Boutique": 3, "Small": 4}
    rounds = {}
    for a in appointments:
        rounds.setdefault(int(a["round"]), []).append(a)
    invariants = dict(
        duplicateActs=len(appointments) - len({a["artistId"] for a in appointments}),
        duplicateLabelTurns=sum(n - 1 for n in Counter((a["round"], a["labelId"]) for a in appointments).values()),
        tierOrderViolations=sum(
            [rank[label_tiers[a["labelId"]]] for a in rows] != sorted(rank[label_tiers[a["labelId"]]] for a in rows)
            for rows in rounds.values()),
        unexplainedUnderfill=sum(int(r["filled"]) < int(r["target"]) and int(r["accessibleAny"]) > 0 for r in new))

    start_places = {r["id"]: r["basePlaceId"] for r in read(name, "scene-identity-start") if r["entity"] == "act"}
    end_places = {r["id"]: r["basePlaceId"] for r in read(name, "scene-identity-end") if r["entity"] == "act"}
    # These seeded audits generate the frozen 3,000 launch acts before reserve births.
    # Require their actual sequential IDs rather than treating arbitrary saves as this cohort.
    launch_ids = {f"artist_{i:05d}" for i in range(1, 3001)}
    assert launch_ids <= start_places.keys()
    previous_places = {r["id"]: r["basePlaceId"] for r in read(f"{args.previous_tag}-recruitment-{seed}", "scene-identity-start") if r["entity"] == "act"}
    assert all(start_places[a] == previous_places[a] for a in launch_ids), "Initialization changed launch placement"
    signed_ids = {a["artistId"] for a in appointments}
    signed_start = {r["artistId"] for r in read(name, "scene-participation-start") if r["relationship"] == "Resident" and r["labelId"]}
    signed_end = {r["artistId"] for r in read(name, "scene-participation-end") if r["relationship"] == "Resident" and r["labelId"]}
    city_rows = []
    for city in sorted(set(start_places.values()) | set(end_places.values())):
        launch = sum(start_places[a] == city for a in launch_ids)
        city_rows.append(dict(city=city, launchActs=launch, launchSharePercent=100 * launch / 3000,
            initialContracts=sum(start_places[a] == city for a in signed_ids),
            unsignedLaunchActs=sum(start_places[a] == city for a in launch_ids - signed_ids),
            literalHqLabels=sum(r["hqPlaceId"] == city for r in new),
            registryStart=sum(p == city for p in start_places.values()),
            registryEnd=sum(p == city for p in end_places.values()),
            registryEndSharePercent=100 * sum(p == city for p in end_places.values()) / len(end_places),
            signedResidentsStart=sum(start_places.get(a) == city for a in signed_start),
            signedResidentsEnd=sum(end_places.get(a) == city for a in signed_end)))
    with (args.manifest.parent / f"city-allocation-{seed}.csv").open("w", newline="", encoding="utf-8") as f:
        writer = csv.DictWriter(f, fieldnames=list(city_rows[0]))
        writer.writeheader()
        writer.writerows(city_rows)

    results.append(dict(seed=seed,
        cityAllocation=city_rows, launchPlacementUnchanged=True,
        previousContracts=sum(int(r["rosterSize"]) for r in old),
        contracts=sum(int(r["filled"]) for r in new),
        previousEmpty=sum(int(r["rosterSize"]) == 0 for r in old),
        empty=sum(int(r["filled"]) == 0 for r in new),
        desired=sum(int(r["target"]) for r in new),
        unsignedLaunchActs=3000 - sum(int(r["filled"]) for r in new),
        rounds=len(rounds), tiers=tiers, invariants=invariants,
        meanAbsolutePerceptionError=sum(abs(float(a["trueQuality"]) - float(a["perceivedQuality"])) for a in appointments) / max(1, len(appointments)),
        maximumSlate=max(int(a["slateSize"]) for a in appointments),
        emptyHqs=dict(Counter(r["hqPlaceId"] for r in new if int(r["filled"]) == 0))))
output = dict(comparisons=results, initializationIntegrityPassed=all(not any(r["invariants"].values()) for r in results))
(args.manifest.parent / "initialization-analysis.json").write_text(json.dumps(output, indent=2) + "\n", encoding="utf-8")
print(json.dumps(output, indent=2))
raise SystemExit(0 if output["initializationIntegrityPassed"] and len(results) == 2 else 1)
