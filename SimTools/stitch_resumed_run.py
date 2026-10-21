"""Join a checkpoint continuation onto the run it continues (ChartAuditRunner --checkpoint-world / --continue-checkpoint).

    python -I SimTools/stitch_resumed_run.py SimLogs/<run> SimLogs/<run>_s1 --checkpoint-week 312 --checkpoint-year 1965

The checkpoint was written at the end of audit week W, the last week of year Y. Every CSV of the base run is
rewritten in place:
  * a table with a `year` column keeps the base rows with year <= Y and takes the continuation's rows after Y;
  * otherwise, a table with a `week` (else `chartWeek`) column keeps base rows with week <= W and takes the
    continuation's later rows (a continuation numbers its weeks from W + 1, and chart week equals audit week);
  * any other table (end-of-run snapshots, cumulative totals) is taken whole from the continuation, and is listed
    in the report, because its cumulative counters restart at the resume.
A continuation that was killed outright (not stopped) is stitched with --until-week/--until-year, its last whole
checkpoint: only its rows up to there are kept, and its missing end-of-run snapshots leave the base's in place.
Lines are copied byte for byte (only the key field is parsed), one at a time, so a stitched table can be compared
with an unbroken run's. Each table goes to a temporary file and then replaces the base table.
"""
import argparse
import csv
import glob
import json
import os

csv.field_size_limit(1 << 30)
KEYS = ("year", "week", "chartWeek")


def tables(prefix):
    base = os.path.basename(prefix) + "-"
    return {os.path.basename(p)[len(base):]: p for p in glob.glob(prefix + "-*.csv")}


def fields(line):
    return next(csv.reader([line]))


def first_line(path):
    if not path or not os.path.exists(path):
        return None
    with open(path, encoding="utf-8", newline="") as handle:
        return handle.readline() or None


def stitch_table(base_path, cont_path, out_path, week, year, until_week=None, until_year=None):
    cont_head, base_head = first_line(cont_path), first_line(base_path)
    head = cont_head or base_head
    if head is None:
        return "empty"
    if cont_head and base_head and fields(cont_head) != fields(base_head):
        raise ValueError(f"header differs: {cont_path}")
    header = fields(head)
    mode = next((k for k in KEYS if k in header), "continuation")
    key = header.index(mode) if mode != "continuation" else None
    if mode == "continuation" and until_week is not None:
        return "kept"  # a killed segment wrote no end-of-run snapshot; keep the base's
    limit = year if mode == "year" else week
    until = until_year if mode == "year" else until_week

    def keep(line, base):
        if mode == "continuation":
            return True
        value = int(float(fields(line)[key]))
        return value <= limit if base else limit < value and (until is None or value <= until)
    with open(out_path, "w", encoding="utf-8", newline="") as out:
        out.write(head)
        for path, base in ((base_path, True), (cont_path, False)):
            if (base and (mode == "continuation" or not base_head)) or not path or not os.path.exists(path):
                continue
            with open(path, encoding="utf-8", newline="") as handle:
                handle.readline()
                for line in handle:
                    if line.strip() and keep(line, base):
                        out.write(line)
    return mode


def main():
    p = argparse.ArgumentParser()
    p.add_argument("base")
    p.add_argument("continuation")
    p.add_argument("--checkpoint-week", type=int, required=True)
    p.add_argument("--checkpoint-year", type=int, required=True)
    p.add_argument("--until-week", type=int, help="a killed continuation: keep its rows only through this later checkpoint")
    p.add_argument("--until-year", type=int)
    a = p.parse_args()
    if (a.until_week is None) != (a.until_year is None):
        p.error("--until-week and --until-year go together")
    base, cont = tables(a.base), tables(a.continuation)
    report = {"base": a.base, "continuation": a.continuation, "checkpointWeek": a.checkpoint_week,
              "checkpointYear": a.checkpoint_year, "untilWeek": a.until_week, "untilYear": a.until_year, "baseOnly": sorted(set(base) - set(cont))}
    report.update({k: [] for k in KEYS + ("continuation",)})
    for name, cont_path in sorted(cont.items()):
        target = f"{a.base}-{name}"
        tmp = target + ".stitch-tmp"
        mode = stitch_table(base.get(name), cont_path, tmp, a.checkpoint_week, a.checkpoint_year, a.until_week, a.until_year)
        if mode == "kept":
            continue
        if mode == "empty":
            if os.path.exists(tmp):
                os.remove(tmp)
            continue
        os.replace(tmp, target)
        report[mode].append(name)
    with open(f"{a.base}-stitch-report.json", "a", encoding="utf-8") as r:
        r.write(json.dumps(report) + "\n")
    print(json.dumps({k: (len(v) if isinstance(v, list) else v) for k, v in report.items()}))
    if report["continuation"]:
        print("taken whole from the continuation (cumulative counters restart at the resume):", ", ".join(report["continuation"]))


if __name__ == "__main__":
    main()
