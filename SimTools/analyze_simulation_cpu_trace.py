"""Summarize sampled CPU stacks from a dotnet-trace Speedscope export."""
import argparse
from collections import Counter
import json
from pathlib import Path


def summarize(path):
    data = json.loads(path.read_text(encoding="utf-8"))
    names = [frame["name"] for frame in data["shared"]["frames"]]
    inclusive, exclusive = Counter(), Counter()
    total = 0.0
    for profile in data["profiles"]:
        stack, previous = [], None
        for event in profile.get("events", []):
            now = event["at"]
            if previous is not None and stack and names[stack[-1]] == "CPU_TIME":
                duration = now - previous
                frames = [names[index] for index in stack[:-1]]
                total += duration
                if frames:
                    exclusive[frames[-1]] += duration
                for name in set(frames):
                    inclusive[name] += duration
            if event["type"] == "O":
                stack.append(event["frame"])
            else:
                assert stack.pop() == event["frame"], "Unbalanced stack"
            previous = now
    def top(counter):
        return [{"method": name, "percent": round(value * 100 / total, 2)}
                for name, value in counter.most_common(45)] if total else []
    return {"sampledCpuTime": total, "exclusive": top(exclusive), "inclusive": top(inclusive)}


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("trace", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    result = summarize(args.trace)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({"exclusive": result["exclusive"][:12], "inclusive": result["inclusive"][:20]}, indent=2))
