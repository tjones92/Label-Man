"""Download Billboard issues for one year from worldradiohistory.com and keep their text layer (pdftotext).

Usage: research_top10_fetch_issues.py YEAR RECORDS_DIR ISSUE_DIR [EVERY_N_WEEKS]

Issue dates are taken from the billboard.com chart dates in RECORDS_DIR/top10_weekly_YEAR.csv: every Nth week
(default 6) plus the last week, so each Top 10 record is on the Hot 100 of at least one sampled issue. The archive
names files "Billboard YYYY-MM-DD.pdf" in most years and "BB-YYYY-MM-DD[-I].pdf" in others; a missing issue is
replaced by the next or previous week. PDFs are untrusted downloads: ISSUE_DIR should be a dedicated folder,
and each PDF is deleted once bb-DATE.txt is written. Needs pdftotext on PATH.

Some scans (all of 1961) have no text layer. When pdftotext returns under 20 KB, the embedded page JPEGs are
extracted and read with the Windows OCR engine (research_top10_winocr.ps1); bb-DATE.txt then holds the OCR text
and bb-DATE.txt.src notes "(Windows OCR)".
"""
import datetime as dt
import os
import re
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.request

import pandas as pd

YEAR, D, B = int(sys.argv[1]), sys.argv[2], sys.argv[3]
EVERY = int(sys.argv[4]) if len(sys.argv) > 4 else 6
BASE = "https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/{y}/"
NAMES = ["Billboard%20{d}.pdf", "BB-{d}.pdf", "BB-{d}-I.pdf", "Billboard-{d}.pdf", "Billboard%20{d}-I.pdf"]


def get(url):
    """The response body, None on 404; waits out HTTP 429 (the archive rate-limits bursts)."""
    for wait in (30, 60, 120, 240, 480):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
            return urllib.request.urlopen(req, timeout=300).read()
        except urllib.error.HTTPError as e:
            if e.code == 404:
                return None
            if e.code != 429:
                raise
            print(f"  429, waiting {wait}s", flush=True)
            time.sleep(wait)
    raise RuntimeError(f"still rate-limited: {url}")


def try_get(d, pdf):
    for n in NAMES:
        url = BASE.format(y=d.year) + n.format(d=d.isoformat())
        data = get(url)
        time.sleep(2.0)
        if data is not None:
            open(pdf, "wb").write(data)
            return url
    return None


def ocr(pdf, txt):
    """Windows OCR of a scanned PDF's embedded JPEG pages, concatenated in page order."""
    pages = txt + ".pages"
    os.makedirs(pages, exist_ok=True)
    b = open(pdf, "rb").read()
    n = 0
    for m in re.finditer(rb"<<(?:(?!>>).){0,600}?/DCTDecode(?:(?!>>).){0,600}?>>\s*stream\r?\n", b, re.S):
        lm = re.search(rb"/Length\s+(\d+)(\s+\d+\s+R)?", m.group(0))
        end = m.end() + int(lm.group(1)) if lm and not lm.group(2) else b.find(b"endstream", m.end())
        data = b[m.end():end].rstrip(b"\r\n")
        if data.startswith(b"\xff\xd8"):
            n += 1
            open(f"{pages}/p{n:03d}.jpg", "wb").write(data)
    ps1 = os.path.join(os.path.dirname(os.path.abspath(__file__)), "research_top10_winocr.ps1")
    subprocess.run(["powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", ps1, "-InDir", pages,
                    "-OutDir", pages], check=True, stdout=subprocess.DEVNULL)
    with open(txt, "w", encoding="utf-8") as out:
        for k in range(1, n + 1):
            out.write(f"\n\nPDF_PAGE {k}\n" + open(f"{pages}/p{k:03d}.txt", encoding="utf-8-sig", errors="replace").read())
    shutil.rmtree(pages)
    return n


def main():
    os.makedirs(B, exist_ok=True)
    weeks = sorted({w for w in pd.read_csv(f"{D}/top10_weekly_{YEAR}.csv").chartDate if w.startswith(str(YEAR))})
    picks = weeks[::EVERY] + ([weeks[-1]] if (len(weeks) - 1) % EVERY else [])
    log = []
    for w in picks:
        d0 = dt.date.fromisoformat(w)
        for shift in (0, 7, -7, 14, -14, 21):
            d = d0 + dt.timedelta(days=shift)
            txt = f"{B}/bb-{d.isoformat()}.txt"
            if os.path.exists(txt + ".src"):
                log.append((w, d, open(txt + ".src").read().strip()))
                break
            pdf = f"{B}/bb-{d.isoformat()}.pdf"
            url = try_get(d, pdf)
            if url:
                subprocess.run(["pdftotext", pdf, txt], check=True)
                if os.path.getsize(txt) < 20000:
                    url += f" (Windows OCR, {ocr(pdf, txt)} pages)"
                os.remove(pdf)
                open(txt + ".src", "w").write(url)
                log.append((w, d, url))
                break
        else:
            log.append((w, None, "not found"))
        print(*log[-1], flush=True)
    pd.DataFrame(log, columns=["wantedWeek", "issueDate", "source"]).to_csv(f"{D}/print_issues_{YEAR}.csv", index=False)


if __name__ == "__main__":
    main()
