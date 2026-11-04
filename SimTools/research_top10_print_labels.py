"""Find each Top 10 record's printed label in Billboard issue text: Hot 100 lines like 'TITLE, Artist, Label 1234'.

Usage: research_top10_print_labels.py YEAR DIR ISSUE_DIR

Reads DIR/records_YEAR_labels_raw.csv and every bb-*.txt in ISSUE_DIR (research_top10_fetch_issues.py). A match
needs the title's first four words followed within 150 characters (the 1964-69 Hot 100 prints title and
"Artist, Label 1234" on separate lines) by an artist field that shares the artist's first or
last name, then a label name and a catalog number. From 1963 the Hot 100 also prints the producer in parentheses
after the artist; the artist field allows it. Writes DIR/records_YEAR_print.csv with the most frequent printed
label, its count, alternatives and the issues it was seen in.
"""
import collections
import glob
import os
import re
import sys

import pandas as pd

YEAR, D, B = int(sys.argv[1]), sys.argv[2], sys.argv[3]
STOP = {"the", "and", "his", "her", "with", "orchestra", "orch", "featuring", "little", "big", "jr", "sr", "mr"}


def key_words(title):
    t = re.sub(r"\(.*?\)|\"", "", title).upper().replace("--", " ")
    words = re.findall(r"[A-Z0-9']+", t)
    return words[:4]  # one-letter words kept, or 'WHEN I FALL IN LOVE' never matches


def main():
    texts = {os.path.basename(p)[3:13]: open(p, encoding="utf-8", errors="replace").read()
             for p in sorted(glob.glob(B + "/bb-*.txt"))}
    recs = pd.read_csv(f"{D}/records_{YEAR}_labels_raw.csv")
    rows = []
    for _, r in recs.iterrows():
        words = key_words(r.title)
        title = r"\b" + r"\W+".join(re.escape(w) for w in words) + r"\b"
        tail = r"(?:[,—–-]|\.{2,}|\n)\s*([^,\n]{2,80}?),\s*([A-Z][A-Za-z.\-'& ]{1,28}?)"
        # line: 'TITLE, Artist, Label 1234' on one line (1960-63 Hot 100, R&B chart, reviews). catalog: the same
        # across up to 150 characters (1964-69 Hot 100 prints title and artist on separate lines). bare: 'Title,
        # Artist, Label' ending the line or sentence (programming picks), mainly for OCR'd 1961. Each is tried
        # only when the previous one found nothing, since the looser patterns can reach a neighbouring record.
        pats = [("line", re.compile(title + r"[^\n]{0,60}?" + tail + r"\s+(\d{3,6})\b")),
                ("catalog", re.compile(title + r"[\s\S]{0,150}?" + tail + r"\s+(\d{3,6})\b")),
                ("bare", re.compile(title + r"[\s\S]{0,150}?" + tail + r"\s*(?=[.;:)\n]|$)"))]
        names = [w.lower() for w in re.findall(r"[A-Za-z]+", str(r.artist)) if len(w) > 2 and w.lower() not in STOP]
        probes = {n[:4] for n in names[:1] + names[-1:]}
        for mode, pat in pats:
            found, where = collections.Counter(), collections.defaultdict(set)
            for issue, t in texts.items():
                for m in pat.finditer(t):
                    artist, label = m.group(1).lower(), m.group(2).strip()
                    if label.rstrip(".") == "Col" and m.lastindex >= 3 and len(m.group(3)) <= 3:
                        label = "Colpix"  # 'Col. 609': Colpix numbers are 3-digit, Columbia's 5-digit (4-41568)
                    if probes and any(p in artist for p in probes):
                        found[label] += 1
                        where[label].add(issue)
            if found:
                break
        top = found.most_common(3)
        rows.append({"title": r.title, "artist": r.artist, "slots": r.slots, "wikiLabel": r.labelRaw,
                     "printLabel": top[0][0] if top else "", "printCount": top[0][1] if top else 0,
                     "printAlternatives": "; ".join(f"{l}({c})" for l, c in top[1:]),
                     "printIssues": " ".join(sorted(where[top[0][0]])) if top else "",
                     "printMode": mode if top else ""})
    out = pd.DataFrame(rows)
    out.to_csv(f"{D}/records_{YEAR}_print.csv", index=False)
    for _, r in out.iterrows():
        print(f"{r.slots:>2} {r.title[:30]:<31} {r.artist[:22]:<23} wiki={str(r.wikiLabel)[:26]:<27} "
              f"print={r.printLabel}({r.printCount}) {r.printAlternatives}")
    print(f"\n{len(texts)} issues; unmatched in print: {(out.printCount == 0).sum()} of {len(out)}")


if __name__ == "__main__":
    main()
