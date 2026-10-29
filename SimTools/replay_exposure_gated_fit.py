"""RegionalSyncDesign.md §9s. Offline replay of exposure-gated fit realization, 1960, on one run's actual regional demand curves.

A live single connects (commercial fit) everywhere today. Here its fit is realized only in regions where it is exposed:
  - at launch: the label's home region, plus each other region with probability clip(p0 + p1 * nationalReach)
    (keyed per record x region, so arms share draws);
  - later: a neighbour region where it ranked in the regional top KN (lag DN weeks), any region's top KT
    ("regional breakout" trade report, lag DT), or the units-only national top KH (lag 1);
  - unexposed: demand x exp(min(0, CAP - fit)), fit = the record's log conversion relative to the median (flops
    unchanged); exposed late: the region runs its own clock from exposure (local age) unless --clock national.
Catalog records (launched before the run) are always exposed. Ranks: regional = weekly raw demand among all singles;
national = units-only top 100. Metrics follow hist_spread.py (westcoast vs greatplains stand in for LA vs Minneapolis).
Usage: replay.py SEED TAG [--p0 1 --p1 0 --kn 10 --dn 2 --kt 0 --dt 3 --kh 40 --cap 0 --clock local]"""
import argparse
import hashlib
import numpy as np
import pandas as pd

ap = argparse.ArgumentParser()
ap.add_argument("seed"); ap.add_argument("tag")
ap.add_argument("--p0", type=float, default=1.0); ap.add_argument("--p1", type=float, default=0.0)
ap.add_argument("--kn", type=int, default=10); ap.add_argument("--dn", type=int, default=2)
ap.add_argument("--kt", type=int, default=0); ap.add_argument("--dt", type=int, default=3)
ap.add_argument("--kh", type=int, default=40); ap.add_argument("--cap", type=float, default=0.0)
ap.add_argument("--clock", default="local")
ap.add_argument("--quiet", action="store_true")
ap.add_argument("--demote", type=float, default=0.0)  # share of connected live Big-5 singles that do not connect anywhere; ap.add_argument("--quiet", action="store_true")
a = ap.parse_args()

L = str(__import__("pathlib").Path(__file__).resolve().parents[1]).replace("\\", "/") + "/SimLogs/"
SP = L + "regprobe/"  # demand-array cache
BIG5 = {f"label_000{i}" for i in range(1, 6)}
REG = ["eastcoast", "greatlakes", "greatplains", "deepsouth", "southwest", "rockies", "westcoast"]
NB = {"eastcoast": ["greatlakes", "deepsouth"], "greatlakes": ["eastcoast", "deepsouth", "greatplains"],
      "greatplains": ["greatlakes", "rockies", "southwest"], "deepsouth": ["eastcoast", "greatlakes", "southwest"],
      "southwest": ["deepsouth", "rockies", "westcoast", "greatplains"], "rockies": ["greatplains", "southwest", "westcoast"],
      "westcoast": ["rockies", "southwest"]}
W, A = 52, 80
pre = f"hs52op{a.seed}-SMP-HGRPO" + (f"-{a.tag}" if a.tag else "")
cache = SP + f"replay_{a.seed}_{a.tag}.npz"
try:
    z = np.load(cache, allow_pickle=True)
    ids, D, launch, fit, big5, home, reach = (z[k] for k in ["ids", "D", "launch", "fit", "big5", "home", "reach"])
except FileNotFoundError:
    st = pd.read_csv(L + pre + "-single-demand-stages.csv", usecols=["week", "year", "recordId", "region", "age", "rawDemand",
                     "intrinsicConversionRate", "intrinsicQualityFactor"])
    st = st[st.year.eq(1960)]
    ids = np.array(sorted(st.recordId.unique()))
    ix = {r: i for i, r in enumerate(ids)}
    st["ri"], st["gi"] = st.recordId.map(ix), st.region.map({g: i for i, g in enumerate(REG)})
    launch = (st.week - st.age + 1).groupby(st.ri).min().reindex(range(len(ids))).to_numpy()
    D = np.zeros((len(ids), len(REG), A), dtype=np.float32)
    ok = st.age.between(1, A - 1)
    D[st.ri[ok], st.gi[ok], st.age[ok]] = st.rawDemand[ok]
    oc = np.log((st.intrinsicConversionRate / st.intrinsicQualityFactor.clip(lower=1e-9)).clip(lower=1e-12))
    e = st[st.age.between(1, 3)].assign(oc=oc)
    e["rel"] = e.oc - e.groupby(["region", "age", "week"]).oc.transform("median")
    fit = e.groupby("ri").rel.median().reindex(range(len(ids))).fillna(0).to_numpy()
    rec = pd.read_csv(L + pre + "-records.csv", usecols=["recordId", "labelId"]).drop_duplicates("recordId").set_index("recordId").labelId
    lab = pd.Series(ids).map(rec)
    big5 = lab.isin(BIG5).to_numpy()
    home = lab.map(pd.read_csv(L + pre + "-label-geography.csv").set_index("labelId").homeRegion).map(
        {g: i for i, g in enumerate(REG)}).fillna(-1).astype(int).to_numpy()
    lf = pd.read_csv(L + pre + "-label-finance.csv", usecols=["week", "labelId", "nationalReach"])
    lr = lf.groupby("labelId").nationalReach.median()
    reach = lab.map(lr).fillna(0).to_numpy()
    np.savez(cache, ids=ids, D=D, launch=launch, fit=fit, big5=big5, home=home, reach=reach)

n, G = len(ids), len(REG)
gi = {g: i for i, g in enumerate(REG)}
live = launch >= 1  # launched inside the run window: meets the market cold


def u(rid, g):
    h = hashlib.blake2b(f"{rid}|{g}".encode(), digest_size=8).digest()
    return int.from_bytes(h, "little") / 2 ** 64


U = np.array([[u(r, g) for g in REG] for r in ids])
p = np.clip(a.p0 + a.p1 * reach, 0, 1)
E = np.full((n, G), 10 ** 6)  # exposure week per record x region
E[~live, :] = -10 ** 6
for i in np.where(live)[0]:
    for g in range(G):
        if g == home[i] or U[i, g] < p[i]: E[i, g] = launch[i]
capf = np.exp(np.minimum(0, a.cap - fit)).astype(np.float32)
if a.demote > 0:
    V = np.array([u(r, "demote") for r in ids])
    dem = live & big5 & (fit > a.cap) & (V < a.demote)
    E[dem, :] = 10 ** 6  # never realized: sells at the flop level everywhere
nb = [[gi[x] for x in NB[g]] for g in REG]

reg_rank = np.zeros((W + 1, n, G), dtype=np.int32)
nat_rank = np.zeros((W + 1, n), dtype=np.int32)
units = np.zeros((W + 1, n, G), dtype=np.float32)
for t in range(1, W + 1):
    age = t - launch + 1
    act = (age >= 1) & (age < A)
    d = np.zeros((n, G), dtype=np.float32)
    for g in range(G):
        exposed = E[:, g] <= t
        late = exposed & (E[:, g] > launch) & live
        la = np.where(late & (a.clock == "local"), t - E[:, g] + 1, age)
        la = np.clip(la, 0, A - 1)
        ok = act & (la >= 1)
        v = np.where(exposed, D[np.arange(n), g, la], D[np.arange(n), g, np.clip(age, 0, A - 1)] * capf)
        d[:, g] = np.where(ok | (late & (la >= 1)), v, 0)
    units[t] = d
    for g in range(G):
        o = np.argsort(-d[:, g]); rk = np.empty(n, dtype=np.int32); rk[o] = np.arange(1, n + 1)
        reg_rank[t, :, g] = np.where(d[:, g] > 0, rk, 10 ** 6)
    tot = d.sum(1); o = np.argsort(-tot); rk = np.empty(n, dtype=np.int32); rk[o] = np.arange(1, n + 1)
    nat_rank[t] = np.where(tot > 0, rk, 10 ** 6)
    # spread for next weeks
    unexp = E > t
    if a.kh > 0:
        hit = nat_rank[t] <= a.kh
        E[hit[:, None] & unexp] = np.minimum(E[hit[:, None] & unexp], t + 1)
    if a.kt > 0:
        tr = (reg_rank[t] <= a.kt).any(1)
        m = tr[:, None] & (E > t + a.dt)
        E[m] = t + a.dt
    if a.kn > 0:
        for g in range(G):
            src = np.zeros(n, bool)
            for h in nb[g]: src |= (E[:, h] <= t) & (reg_rank[t, :, h] <= a.kn)
            m = src & (E[:, g] > t + a.dn)
            E[m, g] = t + a.dn

# metrics
wc, gp = gi["westcoast"], gi["greatplains"]
ch = nat_rank[1:] <= 100
debut = np.where(ch.any(0), ch.argmax(0) + 1, 0)
peak = np.where(ch.any(0), nat_rank[1:].min(0), 999)


def first(mask):
    return np.where(mask.any(0), mask.argmax(0) + 1, np.nan)


fa, fb = first(reg_rank[1:, :, wc] <= 40), first(reg_rank[1:, :, gp] <= 40)
fa10, fb10 = first(reg_rank[1:, :, wc] <= 10), first(reg_rank[1:, :, gp] <= 10)
df = pd.DataFrame(dict(debut=debut, peak=peak, a=fa, b=fb, a10=fa10, b10=fb10, big5=big5, live=live))
df = df[df.live & df.debut.between(7, 46)]
df["lead"] = df.debut - df[["a", "b"]].min(1)
df["gap"] = (df.a - df.b).abs()


def summ(g, name):
    both = g.a.notna() & g.b.notna()
    br = ((g.a10 < g.b) | (g.b10 < g.a))[both].sum()
    return dict(group=name, n=len(g), oneOnly=(g.a.notna() ^ g.b.notna()).mean() * 100, leadGE2=(g.lead >= 2).mean() * 100,
                gapGE3=(g.gap[both] >= 3).mean() * 100, gapGE6=(g.gap[both] >= 6).mean() * 100, cityBreak=br / max(1, both.sum()) * 100)


tot = units[1:].sum((0, 2))
top10 = nat_rank[1:] <= 10
res = pd.DataFrame([summ(df[df.peak <= 10], "top 10"), summ(df[(df.peak <= 10) & df.big5], "top 10, Big-5"),
                    summ(df[(df.peak <= 10) & ~df.big5], "top 10, indie"), summ(df[df.peak.between(41, 100)], "peak 41-100")])
head = dict(big5Units=tot[big5].sum() / tot.sum() * 100, big5Top10=top10[:, big5].sum() / top10.sum() * 100,
            units=tot.sum() / 1e6, indieLiveUnits=tot[~big5 & live].sum() / 1e6,
            lateExposures=((E > launch[:, None]) & (E < 10 ** 6) & live[:, None]).sum(),
            neverExposed=((E >= 10 ** 6) & live[:, None]).mean() * 100)
print(f"{pre} demote={a.demote} p0={a.p0} p1={a.p1} kn={a.kn} dn={a.dn} kt={a.kt} dt={a.dt} kh={a.kh} cap={a.cap} clock={a.clock}")
print("  " + "  ".join(f"{k} {v:.1f}" if isinstance(v, float) else f"{k} {v}" for k, v in head.items()))
if not a.quiet: print(res.round(1).to_string(index=False))
