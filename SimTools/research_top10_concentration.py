"""Billboard Top 10 concentration for one year under three firm groupings (HistoricalLabelTierBalanceResearch.md §12-13).

Usage: research_top10_concentration.py YEAR DIR [AFFILIATIONS_CSV]

Input: DIR/records_YEAR_final.csv (research_top10_assign_labels.py), one row per YEAR Top 10 record with its
operating label, slots, inTop10PrevYear and labelSource. Groupings:
  label       the operating label as printed;
  owned       owned imprints folded into their corporate owner in YEAR;
  affiliated  owned imprints plus Peterson and Berger's "any indication of affiliation" (distribution links).
Links come from SimTools/research_top10_label_affiliations.csv (label, firm, kind, from, to, source; from/to are
YYYY or YYYY-MM). A link applies to a record whose first YEAR Top 10 week falls inside it, so mid-year changes
(Stax to Paramount in May 1968) split by record. kind "self" renames an operating label to its firm (London ->
London (British Decca)) under both groupings. Links resolve transitively (Reprise -> Warner Bros. -> Warner-Seven
Arts). Writes DIR/records_YEAR_grouped.csv and DIR/concentration_YEAR.csv.
"""
import os
import sys

import pandas as pd

YEAR, D = int(sys.argv[1]), sys.argv[2]
AFF = sys.argv[3] if len(sys.argv) > 3 else os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                                         "research_top10_label_affiliations.csv")
# Peterson and Berger (1975) Table 1: CR4, CR8, firms with hits.
PB = {1960: (28, 52, 39), 1961: (27, 48, 39), 1962: (25, 46, 41), 1963: (26, 55, 36), 1964: (34, 51, 37),
      1965: (37, 61, 35), 1966: (38, 61, 31), 1967: (40, 60, 35), 1968: (42, 61, 30), 1969: (42, 64, 31)}


def bound(v, end):
    v = str(v)
    if len(v) == 4:
        return f"{v}-12-31" if end else f"{v}-01-01"
    return f"{v}-31" if end else f"{v}-01"


AFF_ROWS = pd.read_csv(AFF, dtype={"from": str, "to": str})
AFF_ROWS["start"] = AFF_ROWS["from"].map(lambda v: bound(v, False))
AFF_ROWS["end"] = AFF_ROWS["to"].map(lambda v: bound(v, True))


def links(kinds, date):
    a = AFF_ROWS[AFF_ROWS.kind.isin(kinds) & (AFF_ROWS.start <= date) & (AFF_ROWS.end >= date)]
    dup = a.label[a.label.duplicated()]
    assert dup.empty, f"label linked twice on {date}: {list(dup)}"
    return dict(zip(a.label, a.firm))


def resolve(label, kinds, date):
    m, seen = links(kinds, date), [label]
    while label in m and m[label] != label:
        label = m[label]
        assert label not in seen, seen
        seen.append(label)
    return label


def stats(df, key, by_slots):
    s = (df.groupby(key).slots.sum() if by_slots else df.groupby(key).size()).sort_values(ascending=False)
    total = s.sum()
    c4, c8 = 100 * s.head(4).sum() / total, 100 * s.head(8).sum() / total
    return c4, c8, c8 - c4, len(s), int((df.groupby(key).size() == 1).sum())


def main():
    p = pd.read_csv(f"{D}/records_{YEAR}_final.csv").merge(
        pd.read_csv(f"{D}/records_{YEAR}.csv")[["title", "artist", "first"]], on=["title", "artist"])
    assert p.slots.sum() == 520, p.slots.sum()
    p["owned"] = [resolve(l, {"owned", "self"}, d) for l, d in zip(p.label, p["first"])]
    p["affiliated"] = [resolve(l, {"owned", "self", "distributed"}, d) for l, d in zip(p.label, p["first"])]
    p.to_csv(f"{D}/records_{YEAR}_grouped.csv", index=False)
    rows = []
    for name, df in [("all", p), ("exclCarryover", p[~p.inTop10PrevYear])]:
        for key in ("label", "owned", "affiliated"):
            for by_slots in (True, False):
                c4, c8, d, n, one = stats(df, key, by_slots)
                rows.append({"year": YEAR, "records": name, "grouping": key, "unit": "slots" if by_slots else "records",
                             "CR4": round(c4, 1), "CR8": round(c8, 1), "firms5to8": round(d, 1), "firms": n,
                             "oneHitFirms": one})
    out = pd.DataFrame(rows)
    out.to_csv(f"{D}/concentration_{YEAR}.csv", index=False)
    print(out.drop(columns="year").to_string(index=False))
    if YEAR in PB:
        print(f"Peterson & Berger {YEAR} Table 1: CR4 {PB[YEAR][0]}, CR8 {PB[YEAR][1]}, {PB[YEAR][2]} firms")
    for key in ("label", "affiliated"):
        s = p.groupby(key).agg(slots=("slots", "sum"), records=("title", "size")).sort_values("slots", ascending=False)
        s["pct"] = (100 * s.slots / 520).round(1)
        print(f"\nLeading firms by {key}:\n{s.head(12).to_string()}")
    print("\nlabel provenance:", p.labelSource.str.split(" ").str[0].value_counts().to_dict())


if __name__ == "__main__":
    main()
