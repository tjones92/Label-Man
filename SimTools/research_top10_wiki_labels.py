"""Attach a record label to each Top 10 record of one year from the Wikipedia infobox for that artist's single.

Usage: research_top10_wiki_labels.py YEAR DIR [CACHE_DIR]

Reads DIR/records_YEAR.csv (research_top10_fetch.py). Song pages come from the "List of Billboard Hot 100 top-ten
singles in YEAR" pages for YEAR-1 and YEAR (carryover hits are listed under the year they entered). Where a page
has several infoboxes, the one whose artist matches is used. A record the lists do not link, or whose linked
infobox has no label, is looked up directly as "Title (Artist song)", "Title (song)" and "Title" (part numbers
and second sides dropped), accepted only when a single/song infobox there names the artist.
Writes DIR/records_YEAR_labels_raw.csv.
"""
import hashlib
import os
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

import pandas as pd

YEAR, D = int(sys.argv[1]), sys.argv[2]
CACHE = sys.argv[3] if len(sys.argv) > 3 else None
UA = {"User-Agent": "LabelManResearch/1.0 (historical calibration; contact via repo owner)"}


def get(url):
    """Page text ('' for a missing page); waits out HTTP 429 (Wikipedia throttles bursts)."""
    for wait in (20, 60, 120, 300):
        try:
            return urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=60).read().decode("utf-8", "replace")
        except urllib.error.HTTPError as e:
            if e.code == 404:
                return ""
            if e.code != 429:
                raise
            time.sleep(wait)
    raise RuntimeError(f"still rate-limited: {url}")


def raw(title, depth=0):
    # the hash keeps titles that differ only in case apart on a case-insensitive filesystem
    path = CACHE and os.path.join(CACHE, re.sub(r"[^A-Za-z0-9._-]", "_", title)[:120] + "_"
                                  + hashlib.md5(title.encode()).hexdigest()[:8] + ".wiki")
    if path and os.path.exists(path):
        txt = open(path, encoding="utf-8").read()
    else:
        url = "https://en.wikipedia.org/w/index.php?" + urllib.parse.urlencode({"title": title, "action": "raw", "redirect": "yes"})
        txt = get(url)
        time.sleep(0.4)
        if path:
            os.makedirs(CACHE, exist_ok=True)
            open(path, "w", encoding="utf-8").write(txt)
    m = re.match(r"\s*#REDIRECT\s*\[\[([^\]|#]+)", txt, re.I)
    if m and depth < 5:
        return raw(m.group(1), depth + 1)
    return txt


def norm(s):
    s = s.lower().replace("&", "and")
    s = re.sub(r"\(.*?\)", "", s)
    return re.sub(r"[^a-z0-9]", "", s)


def clean(s):
    s = re.sub(r"<ref[^>]*/>|<ref[^>]*>.*?</ref>", " ", s, flags=re.S)
    s = re.sub(r"<small>.*?</small>|</?small>|<!--.*?-->", " ", s, flags=re.S)
    s = re.sub(r"\{\{\s*(?:hlist|ubl|unbulleted list|flatlist|plainlist|nowrap)\s*\|?", " ", s, flags=re.I)
    s = re.sub(r"<ref.*?(</ref>|/>)|\{\{[^{}]*\}\}|<br\s*/?>|\[\[(?:[^\]|]*\|)?([^\]]+)\]\]",
               lambda m: (m.group(2) or " / ") if m.group(0).startswith("[[") else " / ", s)
    s = re.sub(r"<ref[^>]*>.*", "", s, flags=re.S)  # unclosed ref
    s = re.sub(r"\}\}|\|", " / ", s)
    return re.sub(r"\s+", " ", s).strip(" /")


def infobox(txt, artist):
    """(infobox artist, label) of the single/song infobox naming the artist, else of the first one; None if none."""
    best = None
    for b in re.split(r"\{\{\s*Infobox (?:single|song)", txt, flags=re.I)[1:]:
        a = re.search(r"\|\s*artist\s*=\s*(.+)", b)
        lab = re.search(r"\|\s*label\s*=[ \t]*(.*?)\n\s*(?:\||\}\})", b, re.S)
        art = a.group(1) if a else ""
        if best is None or norm(artist)[:6] in norm(art):
            best = (art, lab.group(1) if lab else "")
            if norm(artist)[:6] in norm(art):
                break
    return best


def direct(title, artist):
    """(page, infobox) for a record the year lists do not link, or (None, None)."""
    t = re.sub(r"\s*-\s*Part\s+\w+$|\s*\(Part\s+\w+\)", "", title, flags=re.I)
    t = re.split(r"\s*/\s*", t)[0].strip()
    t = t[:1].upper() + t[1:]
    a = re.sub(r"^The\s+", "", re.split(r"\s+(?:&|And|With|Featuring)\s+", artist)[0])
    for page in (f"{t} ({a} song)", f"{t} (song)", t):
        box = infobox(raw(page), artist)
        if box and norm(artist)[:6] in norm(box[0]):
            return page, box
    return None, None


def main():
    # list rows: '| "[[Page|Shown]]" <markers>' then '| [attrs |] [[Artist...]]'
    row = re.compile(r'"\[\[([^\]|]+)(?:\|([^\]]+))?\]\]"[^\n]*\n\|\s*(?:(?:rowspan|align|style)[^|\n]*\|\s*)?(.+)')
    links = {}
    for year in (YEAR - 1, YEAR):
        txt = raw(f"List of Billboard Hot 100 top-ten singles in {year}")
        for m in row.finditer(txt):
            page, shown = m.group(1), m.group(2) or m.group(1)
            artist = re.sub(r"\[\[(?:[^\]|]*\|)?([^\]]+)\]\]", r"\1", m.group(3))
            links[(norm(shown), norm(artist)[:8])] = page
            links.setdefault((norm(shown), ""), page)

    recs = pd.read_csv(f"{D}/records_{YEAR}.csv")
    out = []
    for _, r in recs.iterrows():
        page = links.get((norm(r.title), norm(r.artist)[:8])) or links.get((norm(r.title), ""))
        best, note = None, ""
        if page:
            try:
                best = infobox(raw(page), r.artist)
            except Exception as e:  # noqa: BLE001
                note = f"fetch failed {e}"
        if not best or not clean(best[1]):
            found, box = direct(r.title, r.artist)
            if found:
                page, best, note = found, box, "direct page lookup"
            elif not page:
                note = "no song link in the year lists"
        infobox_artist, label = best or ("", "")
        out.append({**r.to_dict(), "wikiPage": page or "", "infoboxArtist": clean(infobox_artist),
                    "labelRaw": clean(label), "note": note})
        print(f"{r.slots:>2} {r.title[:34]:<35} {r.artist[:26]:<27} -> {clean(label)[:60]}", flush=True)
    pd.DataFrame(out).to_csv(f"{D}/records_{YEAR}_labels_raw.csv", index=False)


if __name__ == "__main__":
    main()
