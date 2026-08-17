"""Gate report for the bundled artist-evolution run against its paired supply control.

The bundle turns on Phases 1-4 at once, so attribution comes from WHICH gate fails:
economy metrics (LP share, concept albums, album chart life, units) are Phase 4's;
genre share and the conversion ledger are Phase 1/2's.
"""
import sys, os, collections
import pandas as pd

SIM = r"C:\Project\Label-Man\SimLogs"
run = sys.argv[1] if len(sys.argv) > 1 else "bundle-1001"
ctl = sys.argv[2] if len(sys.argv) > 2 else "ctl-supply-1001"

def path(r, name): return os.path.join(SIM, f"{r}-{name}.csv")

# ---- 1. LP unit share, the primary Phase 4 gate -------------------------------------------
# Calibrated band, from the album-channel work: 29.5 / 35.0 / 41.3 / 48.4 / 55.4 at
# 1960/62/64/66/68. Tolerance +/-1.5 points.
BAND = {1960: 29.5, 1962: 35.0, 1964: 41.3, 1966: 48.4, 1968: 55.4}
TOL = 1.5

def lp_share(r):
    d = pd.read_csv(path(r, "format-mix"))
    d = d[d.period == "annual"]
    out = {}
    for year, g in d.groupby("year"):
        total = g.units.sum()
        album = g[g.releaseFormat == "Album"].units.sum()
        if total: out[int(year)] = 100.0 * album / total
    return out

print(f"=== 1. LP unit share (Phase 4 money gate, band +/-{TOL}pts) ===")
lb, lr = lp_share(ctl), lp_share(run)
print(f"  {'year':<6}{'target':>8}{'control':>9}{'bundle':>9}{'delta':>8}  verdict")
for y in sorted(BAND):
    t, b, r_ = BAND[y], lb.get(y, float('nan')), lr.get(y, float('nan'))
    ok = abs(r_ - t) <= TOL
    print(f"  {y:<6}{t:8.1f}{b:9.1f}{r_:9.1f}{r_-b:+8.1f}  {'PASS' if ok else 'FAIL'}")

# ---- 2. Concept albums: 'a handful across 1965-66, a wave by 1968' -------------------------
def concepts(r):
    d = pd.read_csv(path(r, "album-composition"), low_memory=False)
    if "albumFormat" not in d.columns or d.empty: return {}, 0
    c = d[d.albumFormat == "Concept"]
    return dict(c.groupby("year").size()), len(d)

print("\n=== 2. Concept albums per year (if legitimacy makes 40 in 1966, k is wrong) ===")
cb, nb = concepts(ctl); cr, nr = concepts(run)
years = sorted(set(cb) | set(cr))
print(f"  control total albums={nb}  bundle total albums={nr}")
print("  " + "  ".join(f"{y}:{cb.get(y,0)}->{cr.get(y,0)}" for y in years) if years else "  (none)")

# ---- 3. Album chart life and genre composition --------------------------------------------
def album_chart(r):
    d = pd.read_csv(path(r, "album-chart"), low_memory=False)
    life = d.groupby("recordId").weeksOnChart.max()
    return life.mean(), dict(d.genre.value_counts().head(6))

print("\n=== 3. Album chart life + genre composition ===")
mb, gb = album_chart(ctl); mr, gr = album_chart(run)
print(f"  mean album chart life: control {mb:.1f}wk -> bundle {mr:.1f}wk  ({mr-mb:+.1f})")
print(f"  control top genres: {gb}")
print(f"  bundle  top genres: {gr}")

# ---- 4. Economy: units and release count --------------------------------------------------
print("\n=== 4. Economy (release-count tolerance is +/-1.5% per the revised section 2) ===")
for label, r in (("control", ctl), ("bundle", run)):
    d = pd.read_csv(path(r, "format-mix"))
    d = d[d.period == "annual"]
    globals()[f"_u_{label}"] = d.units.sum(); globals()[f"_r_{label}"] = d.releases.sum()
du, dr = _u_bundle - _u_control, _r_bundle - _r_control
print(f"  total units:    {_u_control:,} -> {_u_bundle:,}  ({100*du/_u_control:+.2f}%)")
print(f"  total releases: {_r_control:,} -> {_r_bundle:,}  ({100*dr/_r_control:+.2f}%)")

# ---- 5. Conversion ledger: Phase 1/2 mechanism --------------------------------------------
print("\n=== 5. Conversion ledger (Phase 1/2) ===")
ev = path(run, "artist-evolution")
if os.path.exists(ev):
    e = pd.read_csv(ev)
    rat = e[e.ratified == True]
    print(f"  observations={len(e)}  ratified={len(rat)}  refused={len(e)-len(rat)}")
    print("  refusals:", dict(e[e.ratified == False].block.value_counts().head(6)))
    print("  per year:", dict(rat.groupby("year").size()))
    print("  top migrations:", dict(collections.Counter(
        f"{a}->{b}" for a, b in zip(rat.fromGenre, rat.toGenre)).most_common(8)))
    print("  triggers:", dict(rat.trigger.value_counts()))
else:
    print("  (no evolution telemetry)")
