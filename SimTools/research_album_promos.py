"""Download a declared primary-source research corpus and index its PDF text.

Research only; no simulation input or dependency. Corpus stays in ignored SimLogs.
"""
import concurrent.futures
import hashlib
import json
import subprocess
import urllib.request
from pathlib import Path

from pypdf import PdfReader
import pypdf._cmap

# One historical embedded font maps 103,229 entries. Keep a finite, explicit limit.
pypdf._cmap.MAPPING_DICTIONARY_SIZE_LIMIT = 500_000

ROOT = Path(__file__).resolve().parents[1] / "SimLogs/album-promo-research"
DATES = ["1960-01-04", "1960-07-04", "1961-01-23", "1961-07-17",
         "1962-01-13", "1962-04-21", "1962-05-05", "1963-08-24",
         "1963-11-09", "1964-01-18", "1964-04-18", "1960-08-01", "1962-09-22"]
PDFTOTEXT = Path("C:/Users/grohl/.cache/codex-runtimes/codex-primary-runtime/dependencies/native/poppler/Library/bin/pdftotext.exe")
FALLBACKS = {
    "1961-01-23": "https://retrocdn.net/images/a/ad/Billboard_US_1961-01-23.pdf",
    "1961-07-17": "https://www.worldradiohistory.com/Archive-All-Music/Billboard/60s/1961/Billboard%201961-07-17.pdf",
}


def fetch(date):
    url = FALLBACKS.get(date, f"https://www.worldradiohistory.com/Archive-Billboard/60s/{date[:4]}/Billboard%20{date}.pdf")
    path = ROOT / f"billboard-{date}.pdf"
    if not path.exists():
        request = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
        with urllib.request.urlopen(request, timeout=55) as response:
            data = response.read()
        if not data.startswith(b"%PDF"):
            raise ValueError(f"Not a PDF: {url}")
        path.write_bytes(data)
    # Poppler handles the historical embedded-font maps more robustly than pypdf.
    if PDFTOTEXT.exists():
        result = subprocess.run([str(PDFTOTEXT), "-layout", "-enc", "UTF-8", str(path), "-"],
                                capture_output=True, check=True)
        pages = result.stdout.decode("utf-8").split("\f")
        if not pages[-1].strip():
            pages.pop()
    else:
        pdf = PdfReader(path)
        pages = [page.extract_text() for page in pdf.pages]
    (ROOT / f"billboard-{date}.txt").write_text(
        "\n".join(f"\n--- PDF PAGE {i + 1} ---\n{text}" for i, text in enumerate(pages)), encoding="utf-8")
    print(f"Indexed {date}: {len(pages)} pages", flush=True)
    return dict(date=date, url=url, pages=len(pages), sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def main():
    ROOT.mkdir(parents=True, exist_ok=True)
    results = []
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as executor:
        jobs = {executor.submit(fetch, date): date for date in DATES}
        for job in concurrent.futures.as_completed(jobs):
            try:
                results.append(job.result())
            except Exception as error:
                results.append(dict(date=jobs[job], error=str(error)))
                print(f"Failed {jobs[job]}: {error}", flush=True)
    (ROOT / "manifest.json").write_text(json.dumps(sorted(results, key=lambda r: r["date"]), indent=2), encoding="utf-8")


if __name__ == "__main__":
    main()
