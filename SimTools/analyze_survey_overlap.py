"""Top-11 overlap between WLS / KRLA / KDWB surveys of the same week (cached pages only).

Usage: python SimTools/analyze_survey_overlap.py   (needs research_regional_chart_examples.py --download once)
Records are matched on the first four significant title words: same first word, or two shared words.
KRLA's 1964 table pages arrive as "lastWeek | TITLE | artist | label", so numeric columns are dropped
before the title is read (keying on them made every such row unmatchable and understated overlap).
A tie row's "TIE" column is dropped the same way (its first title is kept). A few transcription variants are
normalized (Marie/Maria Elena, Nifty/Nitty Gritty, Outer/Out Of Limits, Honululu/Honolulu Lulu).
Station surveys rank requests/sales/playlist, not regional sales, and these are city lists, not regions.
"""
import itertools
from pathlib import Path
import re
import statistics
import sys
from datetime import date

sys.path.insert(0, str(Path(__file__).resolve().parent))
import research_regional_chart_examples as r

r.TITLES = {"*": (r".", r".")}  # keep every ranked row, not just the eight selected titles
STOP = {"the", "a", "i", "my", "you", "to", "of", "in", "on", "be", "and", "it", "s", "me", "is"}
NUMBERS = {"24": "twenty four", "500": "five hundred"}
VARIANTS = {"marie": "maria", "nifty": "nitty", "outer": "out", "honululu": "honolulu"}


def words(context):
    parts = [p for p in context.split("|") if not re.fullmatch(r"\s*(\d{1,2}|--|debut|new|tie)?\s*", p, re.I)]
    title = re.split(r"\.{2,}|\||-{2,}| - |\(", "|".join(parts))[0]
    title = title.lower().replace("'", "").replace("’", "")
    for digits, spelled in NUMBERS.items():
        title = title.replace(digits, spelled)
    return [VARIANTS.get(w, w) for w in re.findall(r"[a-z]+", title) if w not in STOP][:4]


def same(a, b):
    return bool(a and b) and (a[0] == b[0] or len(set(a) & set(b)) >= 2)


def overlap(ta, tb):
    used, hits, unmatched = set(), 0, []
    for a in ta:
        j = next((j for j, b in enumerate(tb) if j not in used and same(a, b)), None)
        if j is None:
            unmatched.append(" ".join(a))
        else:
            used.add(j)
            hits += 1
    return hits / 11, unmatched


charts = {}
for station, indexes in r.INDEXES.items():
    for index in indexes:
        source, _ = r.get(index, False)
        for day, url in r.chart_links(source, index):
            page, prov = r.get(url, False)
            rows = r.observations(station, day, page, prov["url"])
            if not rows:
                continue
            d = date.fromisoformat(rows[0]["date"])
            top = {}
            for row in rows:
                if row["rank"] <= 11 and row["rank"] not in top:
                    top[row["rank"]] = words(row["context"])
            if len(top) >= 10:
                charts[(station, d)] = [top[k] for k in sorted(top)]

results = []
for a, b in itertools.combinations(r.INDEXES, 2):
    for (sa, da), ta in charts.items():
        if sa != a:
            continue
        near = [(abs((db - da).days), db) for (sb, db) in charts if sb == b and abs((db - da).days) <= 3]
        if near:
            db = min(near)[1]
            ov, miss = overlap(ta, charts[(b, db)])
            results.append((a, b, da, ov, miss))
by_pair = {}
for a, b, d, ov, _ in results:
    by_pair.setdefault((a, b), []).append(ov)
print("charts parsed:", len(charts))
for pair, vals in by_pair.items():
    print(pair, "n", len(vals), "median", round(statistics.median(vals), 3), "mean", round(statistics.mean(vals), 3))
allv = [v for vals in by_pair.values() for v in vals]
print("ALL n", len(allv), "median", round(statistics.median(allv), 3), "mean", round(statistics.mean(allv), 3))
for a, b, d, ov, miss in results[:6]:
    print(a, b, d, round(ov, 2), "only-in-A:", miss)
