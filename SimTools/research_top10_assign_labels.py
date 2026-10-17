"""Assign one operating label to each Top 10 record of one year: printed Billboard first, then the infobox.

Usage: research_top10_assign_labels.py YEAR DIR

Reads DIR/records_YEAR_print.csv (research_top10_print_labels.py, which carries the raw infobox label) and the
manual decisions for YEAR in SimTools/research_top10_label_overrides.csv (year, title, artist, label, labelSource:
labels found by hand in the issue text, wrong-version infoboxes, and records neither source settles, which are
marked UNVERIFIED). Label spellings are normalized to one name per operating label. Writes
DIR/records_YEAR_final.csv with label and per-row labelSource, and prints every row that needs a decision:
print and infobox disagree, only one source exists, or the infobox lists several labels.
"""
import difflib
import os
import re
import sys

import pandas as pd

YEAR, D = int(sys.argv[1]), sys.argv[2]
HERE = os.path.dirname(os.path.abspath(__file__))

ALIAS = {
    "rca": "RCA Victor", "victor": "RCA Victor", "rca victor": "RCA Victor",
    "col": "Columbia", "columbia": "Columbia",
    "abc": "ABC-Paramount", "abc paramount": "ABC-Paramount", "abc-paramount": "ABC-Paramount",
    "vee jay": "Vee-Jay", "veejay": "Vee-Jay", "vee-jay": "Vee-Jay",
    "warner": "Warner Bros.", "warner bros": "Warner Bros.", "warner bros.": "Warner Bros.",
    "warner brothers": "Warner Bros.", "warner bros.-seven arts": "Warner Bros.", "warner bros. seven arts": "Warner Bros.",
    "warner/7 arts": "Warner Bros.", "w.b.": "Warner Bros.", "w. b.": "Warner Bros.", "wb": "Warner Bros.",
    "u.a.": "United Artists", "ua": "United Artists", "united artists": "United Artists",
    "mgm": "MGM", "m-g-m": "MGM", "m.g.m.": "MGM",
    "canadian american": "Canadian-American", "canadian-american": "Canadian-American",
    "philles": "Philles", "phil": "Philles", "a&m": "A&M", "a & m": "A&M", "a.f.o": "A.F.O.", "a.f.o.": "A.F.O.", "afo": "A.F.O.",
    "big top": "Big Top", "bigtop": "Big Top", "le grand": "Legrand", "legrand": "Legrand", "del fi": "Del-Fi", "del-fi": "Del-Fi", "philly groove": "Philly Groove", "sound stage 7": "Sound Stage 7", "sound stage": "Sound Stage 7", "nimbus": "Nimbus 9", "phil-l.a. of soul": "Phil-L.A. of Soul", "abc/dunhill": "Dunhill", "abc dunhill": "Dunhill", "abc-dunhill": "Dunhill", "tamla motown": "Tamla", "red bird": "Red Bird", "red-bird": "Red Bird", "kama sutra": "Kama Sutra",
    "london": "London", "parrot": "Parrot", "imperial": "Imperial", "dunhill": "Dunhill",
    "dot": "Dot", "tamla": "Tamla", "gordy": "Gordy", "motown": "Motown", "soul": "Soul", "v.i.p.": "V.I.P.",
    "atco": "Atco", "atlantic": "Atlantic", "stax": "Stax", "volt": "Volt", "decca": "Decca", "coral": "Coral",
    "brunswick": "Brunswick", "uni": "Uni", "kapp": "Kapp", "epic": "Epic", "okeh": "Okeh", "date": "Date",
    "smash": "Smash", "philips": "Philips", "fontana": "Fontana", "mercury": "Mercury", "liberty": "Liberty",
    "capitol": "Capitol", "tower": "Tower", "apple": "Apple", "reprise": "Reprise", "chess": "Chess",
    "checker": "Checker", "argo": "Argo", "cadet": "Cadet", "scepter": "Scepter", "wand": "Wand",
    "laurie": "Laurie", "cameo": "Cameo", "parkway": "Parkway", "swan": "Swan", "jamie": "Jamie",
    "colpix": "Colpix", "colgems": "Colgems", "bell": "Bell", "amy": "Amy", "mala": "Mala", "buddah": "Buddah",
    "elektra": "Elektra", "roulette": "Roulette", "dimension": "Dimension", "monument": "Monument",
    "hickory": "Hickory", "king": "King", "duke": "Duke", "peacock": "Peacock", "musicor": "Musicor",
    "verve": "Verve", "cotillion": "Cotillion", "minit": "Minit", "dolton": "Dolton", "sue": "Sue",
    "white whale": "White Whale", "calendar": "Calendar", "20th century fox": "20th Century Fox",
    "20th century-fox": "20th Century Fox", "20th fox": "20th Century Fox", "fox": "20th Century Fox",
}


def normalize(raw):
    """One operating-label name from a printed or infobox label string ('' when empty)."""
    if not isinstance(raw, str) or not raw.strip():
        return ""
    parts = [x for x in re.split(r"\s*(?:/|;|\*|\n|,|·|•|\s[–—�-]\s|�)\s*", raw.strip(" *")) if x.strip()]
    us = [x for x in parts if re.search(r"\((?:US|U\.S\.|USA|United States)\)", x)]
    abroad = r"\((?:UK|U\.K\.|Canada|Europe|Germany|France|Japan|Australia|Netherlands|international)[^)]*\)"
    rest = [x for x in parts if not re.search(abroad, x, re.I)]
    s = (us or rest or parts or [raw])[0]
    s = re.sub(r"\([^)]*\)", "", s)
    s = re.sub(r"\b(Records?|Record Co\.?|Recording(?:s| Co\.?)?|Inc\.?)\b", "", s, flags=re.I)
    for _ in range(3):  # catalog numbers, possibly several tokens ('Parrot 45PAR 9695')
        s = re.sub(r"(?:\s+#?\s*(?:[A-Z]{1,4}|No)?\.?[- ]?\d[\w-]*|\s+\d*[A-Z]{2,5}\b|\s+#)\s*$", "", s.strip())
    s = re.sub(r"\s+", " ", s).strip(" .-,#:!")
    return ALIAS.get(s.lower(), s)


def read_print(r, known, wk):
    """The printed label after OCR repair, and a note when the scan text needed reading ('' if none legible).

    Candidates are the most frequent printed label and its alternatives. A known label name is taken as printed;
    an unknown string that is a prefix of, or close to, the infobox label (OCR 'Mer', 'Dee', 'United Anises')
    confirms the infobox; anything else is illegible."""
    cands = [] if r.printCount == 0 else [r.printLabel] + re.findall(
        r"([^;()]+)\(\d+\)", r.printAlternatives if isinstance(r.printAlternatives, str) else "")
    for raw in (c.strip() for c in cands):
        n = normalize(raw)
        if n.lower() in known:
            return known[n.lower()], "" if raw == cands[0] else f"alternative '{raw}'"
        if wk and len(n) >= 3 and (wk.lower().startswith(n.lower()[:3])
                                   or difflib.SequenceMatcher(None, n.lower(), wk.lower()).ratio() >= 0.6):
            return wk, f"OCR '{raw}'"
    return "", f"illegible '{cands[0]}'" if cands else ""


def main():
    p = pd.read_csv(f"{D}/records_{YEAR}_print.csv")
    carry = pd.read_csv(f"{D}/records_{YEAR}.csv")[["title", "artist", "inTop10PrevYear"]]
    p = p.merge(carry, on=["title", "artist"], how="left")
    ov = pd.read_csv(f"{HERE}/research_top10_label_overrides.csv")
    ov = {(r.title, r.artist): (r.label, r.labelSource) for r in ov[ov.year == YEAR].itertuples()}
    aff = pd.read_csv(f"{HERE}/research_top10_label_affiliations.csv")
    known = {k.lower(): k for k in set(ALIAS.values()) | set(aff.label) | {normalize(w) for w in p.wikiLabel} - {""}}
    labels, sources = [], []
    for r in p.itertuples():
        wk = normalize(r.wikiLabel)
        if len(wk) > 24 or re.search(r"=|review|unterberger|access|''", wk, re.I):
            wk = ""  # a citation or the next infobox field, not a label
        pr, how = read_print(r, known, wk)
        multi = isinstance(r.wikiLabel, str) and bool(re.search(r"\(US|/|;|\*", r.wikiLabel))
        if (r.title, r.artist) in ov:
            lab, src = ov[(r.title, r.artist)]
        elif pr:
            lab = pr
            src = "print" + (f" ({how})" if how else "") if (not wk or wk == pr) else f"print (infobox: {wk})"
            if getattr(r, "printMode", "") == "bare":
                src += " [no catalog no.]"
        elif wk and how:
            lab, src = wk, f"wikipedia infobox (print {how})"
        elif wk:
            lab, src = wk, "wikipedia infobox (US)" if multi else "wikipedia infobox"
        else:
            lab, src = "", "UNRESOLVED"
        labels.append(lab)
        sources.append(src)
        decide = (r.title, r.artist) not in ov and (src.startswith("print (infobox") or "illegible" in src
                                                     or (multi and not src.startswith("print")) or not lab)
        if decide:
            print(f"DECIDE {r.slots:>2} {r.title[:30]:<31} {r.artist[:22]:<23} -> {lab or '?':<14} print={pr or '-':<14} "
                  f"wiki={str(r.wikiLabel)[:44]:<45} alt={r.printAlternatives if isinstance(r.printAlternatives, str) else ''}")
    p["label"], p["labelSource"] = labels, sources
    p.to_csv(f"{D}/records_{YEAR}_final.csv", index=False)
    print(f"\n{YEAR}: {len(p)} records;", p.labelSource.str.split(" ").str[0].value_counts().to_dict())
    print("labels:", ", ".join(f"{k} {v}" for k, v in p.groupby("label").slots.sum().sort_values(ascending=False).items()))


if __name__ == "__main__":
    main()
