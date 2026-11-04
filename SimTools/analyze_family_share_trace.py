"""Split a run pair's Major-family chart share change into sales vs label-tier changes.

Usage:  py SimTools/analyze_family_share_trace.py <control-run> <treatment-run> [<control-run> <treatment-run> ...]

Rebuilds majorFamilyChartShare (Major + MidTier share of charted single units, by year-end tier) from
label-sales-weekly.csv and label-finance.csv, then re-scores the treatment's sales on the control's tiers, lists the
tier switches that carry units into or out of the family, and splits the family into Major and MidTier.
"""
import csv, sys, collections
FAM = {"Major", "MidTier"}
def load(run):
    tier = {}  # (year, label) -> tier at last week of year
    lastweek = {}
    for r in csv.DictReader(open(f"SimLogs/{run}-label-finance.csv", newline="")):
        y, w = int(r["year"]), int(r["week"])
        if w >= lastweek.get((y, r["labelId"]), -1):
            lastweek[(y, r["labelId"])] = w; tier[(y, r["labelId"])] = r["labelTier"]
    sales = collections.defaultdict(lambda: collections.Counter())  # year -> label -> charted single units
    for r in csv.DictReader(open(f"SimLogs/{run}-label-sales-weekly.csv", newline="")):
        if r["format"] == "Single": sales[int(r["year"])][r["labelId"]] += int(r["chartedUnits"])
    conc = {int(r["year"]): float(r["majorFamilyChartShare"]) for r in csv.DictReader(open(f"SimLogs/{run}-concentration.csv", newline=""))}
    return tier, sales, conc
def share(sales, tiers, y):
    tot = sum(sales[y].values()); fam = sum(u for l, u in sales[y].items() if tiers.get((y, l)) in FAM)
    return fam / tot if tot else 0, tot
for c, t in zip(sys.argv[1::2], sys.argv[2::2]):
    (tc, sc, cc), (tt, st, ct) = load(c), load(t)
    print(f"== {t} vs {c}")
    for y in (1963, 1964, 1965, 1966):
        a, _ = share(sc, tc, y); b, _ = share(st, tt, y); bx, _ = share(st, tc, y)
        moved = sum(1 for (yy, l), v in tt.items() if yy == y and tc.get((y, l)) and tc[(y, l)] != v and (v in FAM) != (tc[(y, l)] in FAM))
        print(f" {y} concentration {cc.get(y,0):.3f}->{ct.get(y,0):.3f} | rebuilt own tiers {a:.3f}->{b:.3f} ({(b-a)*100:+.1f}) | t sales on control tiers {bx:.3f} ({(bx-a)*100:+.1f}) | labels switching family {moved}")
print("\n--- switches carrying chart units (treatment units, by direction) ---")
for c, t in zip(sys.argv[1::2], sys.argv[2::2]):
    (tc, sc, cc), (tt, st, ct) = load(c), load(t)
    print(f"== {t} vs {c}")
    for y in (1963, 1964, 1965, 1966):
        tot = sum(st[y].values())
        def ctier(l): return tc.get((y, l)) or tt.get((y, l))
        bx = sum(u for l, u in st[y].items() if ctier(l) in FAM) / tot
        a = sum(u for l, u in sc[y].items() if tc.get((y, l)) in FAM) / sum(sc[y].values())
        up = collections.Counter(); down = collections.Counter(); newl = 0
        for l, u in st[y].items():
            ctl, ttl = tc.get((y, l)), tt.get((y, l))
            if ctl is None: newl += u; continue
            if (ttl in FAM) and not (ctl in FAM): up[(ctl, ttl)] += u
            if (ctl in FAM) and not (ttl in FAM): down[(ctl, ttl)] += u
        # also units the control's family lost because its labels left the family in control but not treatment
        cdown = collections.Counter()
        for l, u in sc[y].items():
            ctl, ttl = tc.get((y, l)), tt.get((y, l))
            if ttl and (ttl in FAM) and not (ctl in FAM): cdown[(ctl, ttl)] += u
        f = lambda cnt: " ".join(f"{k[0]}->{k[1]} {v/tot*100:.1f}pt" for k, v in cnt.most_common(4))
        print(f" {y} t-sales on control tiers (new labels own tier) {bx:.3f} vs control {a:.3f} ({(bx-a)*100:+.1f}) | new-label units {newl/tot*100:.1f}%")
        print(f"      into family in t only: {f(up)}")
        print(f"      out of family in t only: {f(down)}")
print("\n--- Major-only and MidTier-only chart unit share (own tiers) ---")
for c, t in zip(sys.argv[1::2], sys.argv[2::2]):
    (tc, sc, cc), (tt, st, ct) = load(c), load(t)
    print(f"== {t} vs {c}")
    for y in (1963,1964,1965,1966):
        def sh(s, tr, k): return sum(u for l,u in s[y].items() if tr.get((y,l)) == k) / sum(s[y].values())
        print(f" {y} Major {sh(sc,tc,'Major'):.3f}->{sh(st,tt,'Major'):.3f} ({(sh(st,tt,'Major')-sh(sc,tc,'Major'))*100:+.1f})  MidTier {sh(sc,tc,'MidTier'):.3f}->{sh(st,tt,'MidTier'):.3f} ({(sh(st,tt,'MidTier')-sh(sc,tc,'MidTier'))*100:+.1f})  MidTier firms {sum(1 for (yy,l),v in tc.items() if yy==y and v=='MidTier')}->{sum(1 for (yy,l),v in tt.items() if yy==y and v=='MidTier')}")
