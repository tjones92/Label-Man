"""Compare every non-timing CSV and optionally the complete saved worlds of two runs."""
import argparse
import csv
import gzip
import hashlib
import json
from pathlib import Path


def digest(path, compressed=False):
    hasher = hashlib.sha256()
    with (gzip.open(path, "rb") if compressed else path.open("rb")) as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            hasher.update(chunk)
    return hasher.hexdigest()


def compare(before, after, before_world=None, after_world=None):
    def outputs(prefix):
        return {path.name[len(prefix.name) + 1:]: path
                for path in prefix.parent.glob(prefix.name + "-*.csv")
                if not path.name.endswith("-performance-profile.csv")}
    left, right = outputs(before), outputs(after)
    if not left or not right:
        raise ValueError("Both runs must have CSV outputs")
    mismatches = []
    for name in sorted(left.keys() | right.keys()):
        if name not in left or name not in right:
            mismatches.append({"file": name, "reason": "missing file"})
        elif digest(left[name]) != digest(right[name]):
            mismatches.append({"file": name, "reason": "different bytes"})
    result = {"before": str(before), "after": str(after), "comparedCsvs": len(left.keys() & right.keys()),
              "mismatches": mismatches}
    if bool(before_world) != bool(after_world):
        raise ValueError("Supply both world paths")
    if before_world:
        # Equal compressed bytes already prove equal decompressed contents. This
        # avoids expanding several gigabytes when the deterministic writer agrees.
        left_gzip, right_gzip = digest(before_world), digest(after_world)
        identical_gzip = left_gzip == right_gzip
        result["worldHashRepresentation"] = "gzip" if identical_gzip else "decompressed"
        result["beforeWorldSha256"] = left_gzip if identical_gzip else digest(before_world, compressed=True)
        result["afterWorldSha256"] = right_gzip if identical_gzip else digest(after_world, compressed=True)
        result["worldIdentical"] = result["beforeWorldSha256"] == result["afterWorldSha256"]
    def profile(prefix):
        with prefix.with_name(prefix.name + "-performance-profile.csv").open(newline="", encoding="utf-8-sig") as source:
            return list(csv.DictReader(source))
    result["beforeProfile"], result["afterProfile"] = profile(before), profile(after)
    result["passed"] = not mismatches and result.get("worldIdentical", True)
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("before", type=Path)
    parser.add_argument("after", type=Path)
    parser.add_argument("--before-world", type=Path)
    parser.add_argument("--after-world", type=Path)
    parser.add_argument("--output", type=Path)
    args = parser.parse_args()
    result = compare(args.before, args.after, args.before_world, args.after_world)
    rendered = json.dumps(result, indent=2)
    if args.output:
        args.output.write_text(rendered + "\n", encoding="utf-8")
    print(rendered)
    raise SystemExit(0 if result["passed"] else 1)
