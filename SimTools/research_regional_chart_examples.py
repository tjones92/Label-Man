"""Collect a small, explicitly selected historical radio-survey timing sample.

Source: Oldiesloon transcriptions of WLS, KRLA and KDWB printed surveys.
This is a mechanism check, not a representative estimate of regional sales peaks.
Raw pages and extraction contexts are retained in ignored SimLogs/ for review.
Network access occurs only with --download; default operation uses cached pages.
"""
import argparse
from concurrent.futures import ThreadPoolExecutor
import csv
from datetime import datetime, date
import hashlib
import html
import json
from pathlib import Path
import re
import urllib.parse
import urllib.request
from lxml import html as lhtml


ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / "SimLogs" / "historical-chart-research"
START, END = date(1963, 10, 1), date(1964, 2, 29)
INDEXES = {
    "WLS": ["https://www.oldiesloon.com/il/wlsind63.htm", "https://www.oldiesloon.com/il/wlsind64.htm"],
    "KRLA": ["https://www.oldiesloon.com/ca/krlaind63.htm", "https://www.oldiesloon.com/ca/krlaind64.htm"],
    "KDWB": ["https://www.oldiesloon.com/mn/kdind63.htm", "https://www.oldiesloon.com/mn/kdind64.htm"],
}
# Selected before extraction, covering a regional breakout, national hits and a
# nationwide launch. Identification includes the performer to prevent cover merges.
TITLES = {
    "Surfin' Bird | Trashmen": (r"surfin['’]?\s*bird", r"trashmen"),
    "Louie Louie | Kingsmen": (r"louie[, ]+louie", r"kingsmen"),
    "Dominique | Singing Nun": (r"dominique", r"singing\s*nun|soeur"),
    "Sugar Shack | Jimmy Gilmer": (r"sugar\s*shack", r"gilmer|fireballs"),
    "Wonderful Summer | Robin Ward": (r"wonderful\s*summer", r"robin\s*ward"),
    "I Want to Hold Your Hand | Beatles": (r"i\s*want\s*to\s*hold\s*your\s*hand", r"beatles"),
    "There! I've Said It Again | Bobby Vinton": (r"there[!, ]*i['’]?ve\s*said\s*it\s*again", r"vinton"),
    "Drip Drop | Dion": (r"drip[ ,\-]*drop", r"dion"),
}


def plain(source):
    source = re.sub(r"(?is)<(script|style)\b.*?</\1>", "", source)
    source = re.sub(r"(?i)<br\s*/?>|</(?:p|tr|div|pre)\s*>", "\n", source)
    source = re.sub(r"<[^>]+>", "", source)
    return html.unescape(source).replace("\xa0", " ")


def get(url, download):
    parts = urllib.parse.urlsplit(url)
    if parts.hostname not in {"www.oldiesloon.com", "oldiesloon.com"}:
        raise ValueError("Source outside the declared public archive: " + url)
    url = urllib.parse.urlunsplit(("https", "www.oldiesloon.com", parts.path, "", ""))
    path = CACHE / (hashlib.sha256(url.encode()).hexdigest()[:12] + "-" + Path(parts.path).name)
    if not path.exists():
        if not download:
            raise FileNotFoundError("Not cached; run --download: " + url)
        request = urllib.request.Request(url, headers={"User-Agent": "Label-Man historical research/1.0"})
        content = urllib.request.urlopen(request, timeout=25).read()
        path.write_bytes(content)
    content = path.read_bytes()
    return content.decode("cp1252", errors="replace"), {
        "url": url, "cache": str(path.relative_to(ROOT)),
        "sha256": hashlib.sha256(content).hexdigest(),
    }


def chart_links(source, base):
    links = []
    for href, label in re.findall(r'(?is)<a\b[^>]*href=["\']([^"\']+)["\'][^>]*>(.*?)</a>', source):
        label = " ".join(plain(label).split())
        match = re.search(r"([A-Za-z]+)\s+(\d{1,2}),?\s+(196[34])", label)
        if not match:
            continue
        try:
            day = datetime.strptime(" ".join(match.groups()), "%B %d %Y").date()
        except ValueError:
            continue
        if START <= day <= END:
            links.append((day, urllib.parse.urljoin(base, html.unescape(href))))
    return links


def observations(station, day, source, url):
    # Read the date printed on the survey; some index labels contain typos.
    text = re.sub(r"\s+", " ", plain(source)).strip()
    printed = re.search(r"(January|February|October|November|December)\s+(\d{1,2})"
                        r"(?:\s+thru\s+\d{1,2})?,?\s+(196[34])", text, re.I)
    if printed:
        day = datetime.strptime(" ".join(printed.groups()), "%B %d %Y").date()
    if station == "WLS" and day == date(1964, 2, 21):
        return []  # Silver Beatle Survey: special artist list, not a regular chart.
    entries = []
    tree = lhtml.fromstring(source)
    blocks = tree.xpath("//pre")
    if station == "KRLA" and not blocks:
        # These pages store entire columns in table cells. Match each title to
        # the performer and current rank at the same row within those columns.
        for tr in tree.xpath("//tr"):
            columns = []
            for cell in tr.xpath("./td"):
                fragment = lhtml.tostring(cell, encoding="unicode")
                fragment = re.sub(r"[\r\n]+", " ", fragment)
                lines = [" ".join(line.split()) for line in plain(fragment).splitlines() if line.strip()]
                if len(lines) >= 20:
                    columns.append(lines)
            if not columns or not all(re.fullmatch(r"\d{1,2}", x) for x in columns[0]):
                continue
            ranks = columns[0]
            for i, rank in enumerate(ranks):
                entries.append((int(rank), " | ".join(c[i] for c in columns[1:] if i < len(c))))
    else:
        # In ordinary HTML, physical source wrapping is not a row separator.
        # Within PRE it is; preserve it there, otherwise use explicit BRs.
        lines = ("\n".join(b.text_content() for b in blocks).splitlines() if blocks else
                 plain(re.sub(r"[\r\n]+", " ", source)).splitlines())
        for line in lines:
            # Some donated transcriptions contain replacement characters in
            # whitespace; none of the eight selected titles needs non-ASCII.
            line = " ".join(re.sub(r"[^\x20-\x7e\t]", " ", line).split())
            if station == "KDWB":
                match = re.match(r"^(\d{1,2})\.\s*\*?\s*(.+)", line)
            elif station == "KRLA":
                match = re.match(r"^(\d{1,2})\s+(?:\d{1,2}(?:-\d{1,2})?|--|debut)\s+(.+)", line, re.I)
            elif day == date(1964, 1, 3):
                match = re.match(r"^(\d{1,2})\.\s*(.+)", line)
            elif day.year == 1963:
                match = re.match(r"^(?:\d{1,2}|--)\s+(\d{1,2})[.]\s*(.+)", line)
            else:
                match = re.match(r"^(\d{1,2})\s+(?:\d{1,2}|--)\s+\d{1,2}\s+(.+)", line)
            if match:
                entries.append((int(match.group(1)), match.group(2)))
            elif blocks and entries and line and not line.lower().startswith(("back", "previous", "pick")):
                rank, context = entries[-1]
                entries[-1] = (rank, context + " " + line)
    result = []
    for rank, context in entries:
        if not 1 <= rank <= 50:
            continue
        for title, (song, artist) in TITLES.items():
            if re.search(song, context, re.I) and re.search(artist, context, re.I):
                result.append({"station": station, "date": day.isoformat(), "rank": rank,
                               "record": title, "source": url, "context": context[:350]})
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--download", action="store_true")
    args = parser.parse_args()
    CACHE.mkdir(parents=True, exist_ok=True)
    pages, manifest, errors = [], [], []
    for station, indexes in INDEXES.items():
        for index in indexes:
            source, provenance = get(index, args.download)
            manifest.append({"station": station, "kind": "index", **provenance})
            pages.extend((station, day, url) for day, url in chart_links(source, index))

    def read_chart(task):
        station, day, url = task
        try:
            source, provenance = get(url, args.download)
            rows = observations(station, day, source, provenance["url"])
            return rows, {"station": station, "date": day.isoformat(), "kind": "chart", **provenance}, None
        except Exception as error:
            return [], None, {"station": station, "date": day.isoformat(), "url": url, "error": str(error)}

    rows = []
    with ThreadPoolExecutor(max_workers=3) as pool:
        for found, provenance, error in pool.map(read_chart, sorted(set(pages))):
            rows.extend(found)
            if provenance:
                manifest.append(provenance)
            if error:
                errors.append(error)
    rows = list({(r["station"], r["date"], r["record"]): r for r in rows}.values())
    rows.sort(key=lambda r: (r["record"], r["station"], r["date"]))
    with (CACHE / "selected-survey-observations.csv").open("w", newline="", encoding="utf-8") as output:
        writer = csv.DictWriter(output, fieldnames=["station", "date", "rank", "record", "source", "context"])
        writer.writeheader()
        writer.writerows(rows)
    results = []
    for record in TITLES:
        stations = {}
        for station in INDEXES:
            selected = [r for r in rows if r["record"] == record and r["station"] == station]
            if not selected:
                stations[station] = {"observations": 0}
                continue
            best = min(r["rank"] for r in selected)
            peaks = [r for r in selected if r["rank"] == best]
            stations[station] = {"observations": len(selected), "bestObservedRank": best,
                "firstObserved": selected[0]["date"], "lastObserved": selected[-1]["date"],
                "peakDates": [r["date"] for r in peaks], "peakSources": [r["source"] for r in peaks]}
        results.append({"record": record, "stations": stations})
    output = {"purpose": "Selected examples; not a population estimate or a sales/spin census",
              "window": [START.isoformat(), END.isoformat()], "requestedCharts": len(set(pages)),
              "fetchedCharts": sum(p["kind"] == "chart" for p in manifest), "errors": errors,
              "results": results, "manifest": manifest}
    (CACHE / "research.json").write_text(json.dumps(output, indent=2), encoding="utf-8")
    print(json.dumps({k: v for k, v in output.items() if k != "manifest"}, indent=2))


if __name__ == "__main__":
    main()
