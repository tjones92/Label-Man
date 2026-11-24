"""Where Big-5 newcomer singles' stronger hooks come from (LpGenreAllocationFollowup.md §58). Usage from the repo root:
py SimTools/analyze_newcomer_hook_source.py RUN [RUN ...]      (RUN = e.g. zfg-2002-sc; reads SimLogs/RUN-*.csv[.gz])
Newcomer singles (NewSigning / Unsigned at launch) launched to week 92, Big-5 vs field. A record's hook is
lerp(performance, composition hook, 0.9) with a 15% ceiling and an execution loss (PolarSongBehavior.RealizeHook), so the
gap is split into composition hook and realization (record - composition); the composition gap is split by song source
(shift-share: source mix at field hooks vs hook within source at Big-5 mix), and within professional songs by route and
by writer (supplier) rank. Pooled rows weight every run equally."""
import os
import sys
import pandas as pd

B5 = {f"label_000{i}" for i in range(1, 6)}
NEW = {"NewSigning", "Unsigned"}


def csv(path):
    return path if os.path.exists(path) or not os.path.exists(path + ".gz") else path + ".gz"


def load(run):
    p = f"SimLogs/{run}-"
    a = pd.read_csv(csv(p + "single-launch-attribution.csv"), usecols=["week", "singleId", "labelId", "career"])
    w = pd.read_csv(csv(p + "writing-launches.csv"), usecols=["recordId", "source", "route", "writerId", "compositionHook",
                                                              "compositionQuality", "recordHook", "recognition", "priorTop40"])
    r = pd.read_csv(csv(p + "records.csv"), usecols=["recordId", "currentPosition"])
    peak = r[r.currentPosition.between(1, 100)].groupby("recordId").currentPosition.min()
    d = a.merge(w, left_on="singleId", right_on="recordId")
    d = d[d.career.isin(NEW) & (d.week <= 92)].copy()
    d["b5"] = d.labelId.isin(B5)
    d["src"] = d.source.replace({"CoverRecentHit": "CoverHit", "CoverStandard": "CoverStd", "TraditionalPublicDomain": "Trad",
                                 "ExternalProfessional": "Pro", "ArtistWritten": "Own"})
    d.loc[d.src.eq("Pro") & d.route.eq("Commission"), "src"] = "ProCommission"
    d["peak"] = d.singleId.map(peak)
    d["run"] = run
    return d


def shift_share(b, f, col, key):
    sb, sf = b[key].value_counts(normalize=True), f[key].value_counts(normalize=True)
    hb, hf = b.groupby(key)[col].mean(), f.groupby(key)[col].mean()
    keys = sorted(set(sb.index) | set(sf.index))
    sb, sf = sb.reindex(keys, fill_value=0), sf.reindex(keys, fill_value=0)
    hf = hf.reindex(keys).fillna(f[col].mean())
    hb = hb.reindex(keys).fillna(hf)
    mix = ((sb - sf) * hf).sum()
    within = (sb * (hb - hf)).sum()
    return mix, within, pd.DataFrame({"B5 share": sb, "field share": sf, "B5 hook": hb, "field hook": hf,
                                      "within contrib": sb * (hb - hf), "mix contrib": (sb - sf) * hf})


def report(d, title):
    b, f = d[d.b5], d[~d.b5]
    print(f"\n==== {title}: Big-5 newcomers {len(b)}, field newcomers {len(f)}")
    for c in ["recordHook", "compositionHook", "compositionQuality"]:
        print(f"  {c:20s} B5 {b[c].mean():.3f}  field {f[c].mean():.3f}  gap {b[c].mean() - f[c].mean():+.3f}")
    rb, rf = (b.recordHook - b.compositionHook).mean(), (f.recordHook - f.compositionHook).mean()
    print(f"  realization (record - composition) B5 {rb:+.3f} field {rf:+.3f} gap {rb - rf:+.3f}")
    mix, within, t = shift_share(b, f, "compositionHook", "src")
    print(f"  composition-hook gap by source: mix {mix:+.3f}  within {within:+.3f}")
    print(t.round(3).to_string())
    pb, pf = b[b.src.str.startswith("Pro")], f[f.src.str.startswith("Pro")]
    print(f"  professional songs: B5 {len(pb)} hook {pb.compositionHook.mean():.3f}; field {len(pf)} hook {pf.compositionHook.mean():.3f}")
    # Writer rank: suppliers ranked by mean composition hook over all their newcomer launches in the run(s).
    allpro = d[d.src.str.startswith("Pro") & d.writerId.notna()]
    wr = allpro.groupby(["run", "writerId"]).compositionHook.mean()
    q = wr.groupby(level=0).transform(lambda s: pd.qcut(s.rank(method="first"), 4, labels=["W1 low", "W2", "W3", "W4 top"]))
    allpro = allpro.join(q.rename("wq"), on=["run", "writerId"])
    print("  professional launches by writer quartile (share of group):")
    print(pd.crosstab(allpro.wq, allpro.b5, normalize="columns").round(3).rename(columns={False: "field", True: "B5"}).to_string())
    mix, within, _ = shift_share(allpro[allpro.b5], allpro[~allpro.b5], "compositionHook", "wq")
    print(f"  pro-song hook gap by writer quartile: writer mix {mix:+.3f}  within writer quartile {within:+.3f}")
    nb = allpro[allpro.b5].groupby("run").writerId.nunique().mean()
    nf = allpro[~allpro.b5].groupby("run").writerId.nunique().mean()
    print(f"  distinct writers per run: B5 {nb:.0f}  field {nf:.0f}; mean writer recognition B5 {allpro[allpro.b5].recognition.mean():.3f}"
          f" field {allpro[~allpro.b5].recognition.mean():.3f}; writer prior Top 40 B5 {allpro[allpro.b5].priorTop40.mean():.2f}"
          f" field {allpro[~allpro.b5].priorTop40.mean():.2f}")
    print("  Top 10 per launch by source (B5 / field, %):")
    for s, g in d.groupby("src"):
        gb, gf = g[g.b5], g[~g.b5]
        print(f"    {s:14s} B5 {len(gb):4d} {100 * (gb.peak <= 10).mean():5.1f}   field {len(gf):5d} {100 * (gf.peak <= 10).mean():5.1f}")


runs = sys.argv[1:]
ds = [load(r) for r in runs]
for d in ds:
    report(d, d.run.iloc[0])
if len(ds) > 1:
    report(pd.concat(ds, ignore_index=True), "pooled")
