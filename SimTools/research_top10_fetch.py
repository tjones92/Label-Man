"""Fetch the Billboard Hot 100 weekly Top 10 for one year from billboard.com (HistoricalLabelTierBalanceResearch.md §12-13).

Usage: research_top10_fetch.py YEAR OUT_DIR [CACHE_DIR]

billboard.com serves the chart dated on or after the requested date and prints its own "Week of" date, so the
walk starts at the last chart of YEAR-1 (requested as Dec 25, the earliest possible last Monday or Saturday) and
steps one day past each chart it reads. That handles the January 1962 move from Monday to Saturday issue dates.
Writes top10_weekly_YEAR.csv (the YEAR-1 closing chart included, to mark carryover) and records_YEAR.csv (one row
per record with its YEAR slots, first YEAR chart, best YEAR position and inTop10PrevYear). Asserts 52 consecutive
weekly charts in YEAR, then prints the #1 sequence for checking against the known history.
"""
import csv
import datetime as dt
import html
import os
import re
import sys
import time
import urllib.request

import pandas as pd

YEAR, OUT = int(sys.argv[1]), sys.argv[2]
CACHE = sys.argv[3] if len(sys.argv) > 3 else None
MONTHS = {m: i for i, m in enumerate(["January", "February", "March", "April", "May", "June", "July", "August",
                                       "September", "October", "November", "December"], 1)}


def parse(page):
    m = re.search(r"Week of ([A-Z][a-z]+) (\d{1,2}), (\d{4})", page)
    week = dt.date(int(m.group(3)), MONTHS[m.group(1)], int(m.group(2)))
    rows = page.split('class="o-chart-results-list-row-container"')[1:]
    out = []
    for pos, r in enumerate(rows, 1):
        t = re.search(r'<h3 id="title-of-a-story"[^>]*>\s*(.*?)\s*</h3>', r, re.S)
        i = r.find("</h3>")
        a = re.search(r'<span class="[^"]*c-label[^"]*"[^>]*>(.*?)</span>', r[i:], re.S)
        artist = re.sub(r"<[^>]+>", "", a.group(1)) if a else ""
        out.append((pos, html.unescape(t.group(1)).strip(), html.unescape(re.sub(r"\s+", " ", artist)).strip()))
    return week, out


def fetch(d):
    path = CACHE and os.path.join(CACHE, f"hot100-req-{d.isoformat()}.html")
    if path and os.path.exists(path):
        return open(path, encoding="utf-8").read()
    url = f"https://www.billboard.com/charts/hot-100/{d.isoformat()}/"
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    page = urllib.request.urlopen(req, timeout=60).read().decode("utf-8", "replace")
    if path:
        open(path, "w", encoding="utf-8").write(page)
    time.sleep(1.0)
    return page


def main():
    os.makedirs(OUT, exist_ok=True)
    if CACHE:
        os.makedirs(CACHE, exist_ok=True)
    charts, req = [], dt.date(YEAR - 1, 12, 25)
    while True:
        week, rows = parse(fetch(req))
        if week.year > YEAR:
            break
        assert len(rows) == 100, (week, len(rows))
        assert not charts or week > charts[-1][0], (req, week)
        charts.append((week, rows[:10]))
        print(week, rows[0][1], "|", rows[0][2], flush=True)
        req = week + dt.timedelta(days=1)
    assert charts[0][0].year == YEAR - 1, charts[0][0]
    weeks = [w for w, _ in charts if w.year == YEAR]
    assert len(weeks) == 52, len(weeks)
    assert all((b - a).days == 7 for a, b in zip(weeks, weeks[1:])), weeks
    with open(f"{OUT}/top10_weekly_{YEAR}.csv", "w", newline="", encoding="utf-8") as fh:
        w = csv.writer(fh)
        w.writerow(["chartDate", "position", "title", "artist"])
        for week, rows in charts:
            for pos, title, artist in rows:
                w.writerow([week.isoformat(), pos, title, artist])

    wk = pd.read_csv(f"{OUT}/top10_weekly_{YEAR}.csv")
    prev = wk[wk.chartDate.str[:4] == str(YEAR - 1)]
    cur = wk[wk.chartDate.str[:4] == str(YEAR)]
    rec = cur.groupby(["title", "artist"], sort=False).agg(
        slots=("position", "size"), first=("chartDate", "min"), best=("position", "min")).reset_index()
    carried = set(zip(prev.title, prev.artist))
    rec["inTop10PrevYear"] = [(t, a) in carried for t, a in zip(rec.title, rec.artist)]
    rec = rec.sort_values(["first", "best"]).reset_index(drop=True)
    assert rec.slots.sum() == 520
    rec.to_csv(f"{OUT}/records_{YEAR}.csv", index=False)
    print(f"\n{YEAR}: {len(weeks)} charts {weeks[0]}..{weeks[-1]}, {len(rec)} records, "
          f"{int(rec.inTop10PrevYear.sum())} carried from {YEAR - 1}")
    ones = cur[cur.position == 1]
    print(f"#1 sequence ({ones.title.ne(ones.title.shift()).sum()} runs):")
    for (t, a), g in ones.groupby([ones.title.ne(ones.title.shift()).cumsum(), "artist"], sort=False):
        print(f"  {g.chartDate.iloc[0]}  {len(g)} wk  {g.title.iloc[0]} | {a}")


if __name__ == "__main__":
    main()
