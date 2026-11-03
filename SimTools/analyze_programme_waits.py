"""Selection waits in programme-choice audits; these are planning opportunities, not issue delays.

Rows must remain in emission order: rolling horizons can revisit earlier intended weeks.
An exclusion ends the eligibility episode (censored); selection ends it successfully.
No unobserved weeks are imputed as eligible. Actual issue delays belong to release-slate.csv.
"""
import argparse
import csv
import json


def episodes(rows):
    pending = {}
    result = []
    for row in rows:
        if not row.get("artistId"):
            continue
        key = row["labelId"], row["artistId"], row["format"]
        eligible = row["eligible"] == "1"
        episode = pending.get(key)
        if not eligible:
            if episode:
                result.append({**pending.pop(key), "selected": False, "end": "ineligible"})
            continue
        if episode is None:
            episode = pending[key] = dict(label=key[0], artist=key[1], format=key[2],
                firstWeek=int(row["week"]), firstIntendedWeek=int(row["intendedWeek"]),
                firstGap=int(row["gap"]), hot=row.get("hot") == "1", opportunities=0,
                reviewWeeks=0, intendedWeeks=0)
        episode["opportunities"] += 1
        episode["reviewWeeks"] = max(episode["reviewWeeks"], int(row["week"]) - episode["firstWeek"])
        episode["intendedWeeks"] = max(episode["intendedWeeks"], int(row["intendedWeek"]) - episode["firstIntendedWeek"])
        if row["selected"] == "1":
            result.append({**pending.pop(key), "selected": True, "end": "selected"})
    result.extend({**e, "selected": False, "end": "audit-end"} for e in pending.values())
    return result


def summarize(items):
    output = {}
    for fmt in ("Single", "Album"):
        for hot_only in (False, True):
            cohort = [e for e in items if e["format"] == fmt and (not hot_only or e["hot"])]
            selected = [e for e in cohort if e["selected"]]
            output[fmt + ("_hot" if hot_only else "_all")] = dict(
                episodes=len(cohort), selected=len(selected), censored=len(cohort)-len(selected),
                selectedMeanOpportunities=sum(e["opportunities"] for e in selected)/len(selected) if selected else None,
                selectedMeanReviewWeeks=sum(e["reviewWeeks"] for e in selected)/len(selected) if selected else None,
                selectedMeanIntendedWeeks=sum(e["intendedWeeks"] for e in selected)/len(selected) if selected else None,
                censoredMaxOpportunities=max((e["opportunities"] for e in cohort if not e["selected"]), default=0))
    return output


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("choices")
    args = parser.parse_args()
    with open(args.choices, newline="", encoding="utf-8-sig") as stream:
        print(json.dumps(summarize(episodes(csv.DictReader(stream))), indent=2))
