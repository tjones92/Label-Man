"""Download and parse 1960 station surveys for the regional-overlap benchmark (RegionalSyncDesign.md).

Usage: python SimTools/research_station_surveys.py [--download] [--year 1960]

Sources (public fan archives, cached raw under SimLogs/historical-station-surveys/):
  oldiesloon.com  KFWB, KRLA (Los Angeles), WLS (Chicago), KDWB, WDGY (Minneapolis)
  musicradio77.com WABC (New York)
No Southern or Western-interior station: ARSA holds them, but browsing its stations needs a member login.
Writes SimLogs/historical-station-surveys/surveys_YEAR.csv: station, city, date, rank, text (the raw ranked line).
Station surveys rank requests/sales/playlist in one city; they are not regional sales.
"""
import argparse
import csv
import hashlib
import html
import re
import time
import urllib.parse
import urllib.request
from datetime import datetime
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / "SimLogs" / "historical-station-surveys"
HOSTS = {"www.oldiesloon.com", "oldiesloon.com", "musicradio77.com", "www.musicradio77.com"}
STATIONS = {
    "KFWB": ("Los Angeles", "https://www.oldiesloon.com/ca/kfwbind{yy}.htm"),
    "KRLA": ("Los Angeles", "https://www.oldiesloon.com/ca/krlaind{yy}.htm"),
    "WLS": ("Chicago", "https://www.oldiesloon.com/il/wlsind{yy}.htm"),
    "KDWB": ("Minneapolis", "https://www.oldiesloon.com/mn/kdind{yy}.htm"),
    "WDGY": ("Minneapolis", "https://www.oldiesloon.com/mn/wgind{yy}.htm"),
    "WABC": ("New York", "https://www.musicradio77.com/Surveys/surveys{yyyy}.html"),
}
MONTHS = "January|February|March|April|May|June|July|August|September|October|November|December"


def get(url, download):
    parts = urllib.parse.urlsplit(url)
    if parts.hostname not in HOSTS:
        raise ValueError("Source outside the declared archives: " + url)
    path = CACHE / "raw" / (parts.hostname.replace("www.", "") + "-" + hashlib.sha256(url.encode()).hexdigest()[:10]
                            + "-" + Path(parts.path).name)
    if not path.exists():
        if not download:
            raise FileNotFoundError("Not cached; run --download: " + url)
        time.sleep(0.6)
        request = urllib.request.Request(url, headers={"User-Agent": "Label-Man historical research/1.0"})
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(urllib.request.urlopen(request, timeout=30).read())
    return path.read_bytes().decode("cp1252", errors="replace")


def plain_lines(source):
    source = re.sub(r"(?is)<(script|style)\b.*?</\1>", "", source)
    pre = "<pre" in source.lower()
    if not pre:
        source = re.sub(r"[\r\n]+", " ", source)
    source = re.sub(r"(?i)<br\s*/?>|</(?:p|tr|div|pre|li|h\d)\s*>", "\n", source)
    source = re.sub(r"(?i)</td\s*>", " | ", source)
    source = html.unescape(re.sub(r"<[^>]+>", "", source)).replace("\xa0", " ")
    return [" ".join(re.sub(r"[^\x20-\x7e]", " ", line).split()) for line in source.splitlines()]


def survey_links(station, index_url, source, year):
    links = []
    for href, label in re.findall(r'(?is)<a\b[^>]*href=["\']([^"\']+)["\'][^>]*>(.*?)</a>', source):
        label = " ".join(html.unescape(re.sub(r"<[^>]+>", " ", label)).split())
        m = re.search(rf"({MONTHS})\.?\s+(\d{{1,2}}),?\s+({year})", label, re.I)
        if not m:
            continue
        try:
            day = datetime.strptime(f"{m.group(1)} {m.group(2)} {m.group(3)}", "%B %d %Y").date()
        except ValueError:
            continue
        links.append((day, urllib.parse.urljoin(index_url, html.unescape(href.strip()))))
    return sorted(set(links))


RANKED = re.compile(r"^(?:\(?\d{1,2}\)?\s+|--\s+|HB\s+|new\s+)?(\d{1,2})[.)]?\s+(?:[-|]\s*)?(.*[A-Za-z].*)$", re.I)


def ranked_rows(lines):
    """Rank and text for each numbered line; keeps the first occurrence of each rank 1-40."""
    rows = {}
    for line in lines:
        m = RANKED.match(line)
        if m and 1 <= int(m.group(1)) <= 40 and int(m.group(1)) not in rows:
            rows[int(m.group(1))] = m.group(2)
    return rows


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--download", action="store_true")
    ap.add_argument("--year", type=int, default=1960)
    ap.add_argument("--dump", help="print the parsed lines of one station's first survey")
    a = ap.parse_args()
    out = []
    for station, (city, pattern) in STATIONS.items():
        index_url = pattern.format(yy=str(a.year)[2:], yyyy=a.year)
        try:
            index = get(index_url, a.download)
        except Exception as e:
            print(station, "index failed:", e)
            continue
        links = survey_links(station, index_url, index, a.year)
        parsed = 0
        for day, url in links:
            try:
                page = get(url, a.download)
            except Exception as e:
                print(station, day, "failed:", e)
                continue
            rows = ranked_rows(plain_lines(page))
            if a.dump == station and parsed == 0:
                print("\n".join(f"{k:2d} {v}" for k, v in sorted(rows.items())))
            if len(rows) >= 15:
                parsed += 1
                out += [dict(station=station, city=city, date=day.isoformat(), rank=k, text=v[:200])
                        for k, v in sorted(rows.items())]
        print(f"{station}: {len(links)} survey links, {parsed} parsed")
    CACHE.mkdir(parents=True, exist_ok=True)
    with (CACHE / f"surveys_{a.year}.csv").open("w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, ["station", "city", "date", "rank", "text"])
        w.writeheader()
        w.writerows(out)


if __name__ == "__main__":
    main()
