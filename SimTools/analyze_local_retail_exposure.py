"""Regional-breakout outcomes for the local-retail-exposure screen (SimTools/LocalRetailExposureInvestigation.md).

Complements analyze_local_consumption.py (economy, leads). Per run, from the settled regional ledger:
  overlap      mean pairwise top-11 overlap of the seven regional sales lists (weeks 10+)
  localOnly    first entries of a record into a region's top 10 that are not on the national Top 100 that week / ever
  stagger      live singles that reach national Top 40: weeks between the first and last region whose top 10 they enter
  path         weeks from a record's first regional top-10 week to its national Top-40 week; regions in a top 10 then
  lateMass     share of connected live singles' settled units sold in regions exposed after launch
  debut        indie Top-10 records at national debut: best region's share of units, per-capita max/mean
Equal-weight seed means and seed-paired deltas against the control arm.
"""
import argparse, concurrent.futures, json, pathlib
import numpy as np
import pandas as pd

ROOT = pathlib.Path(__file__).resolve().parents[1]
BIG5 = [f'label_000{i}' for i in range(1, 6)]
NOT_EXPOSED = 2147483647


def overlap(top):
    ov = []
    for _, g in top.groupby('week'):
        sets = [set(x.recordId) for _, x in g.groupby('region')]
        ov += [len(sets[i] & sets[j]) / 11 for i in range(len(sets)) for j in range(i + 1, len(sets))]
    return 100 * np.mean(ov)


def analyze(name):
    pre = str(ROOT / 'SimLogs' / name) + '-'
    s = pd.read_csv(pre + 'single-demand-stages.csv', usecols=['week', 'recordId', 'region', 'age', 'clearedUnits', 'potentialAudience'])
    r = pd.read_csv(pre + 'records.csv', usecols=['week', 'recordId', 'labelId', 'currentPosition', 'unitsThisWeek'])
    live = set(pd.read_csv(pre + 'single-launch-quality.csv').singleId)
    pop = s.groupby('region').potentialAudience.max()
    s['rank'] = s.groupby(['week', 'region']).clearedUnits.rank(ascending=False, method='first')
    s.loc[s.clearedUnits.le(0), 'rank'] = np.inf
    out = {}
    w = s[s.week.ge(10)]
    out['overlapTop11Pct'] = overlap(w[w['rank'].le(11)])
    nat = r[r.currentPosition.between(1, 100)]
    pos = nat.set_index(['week', 'recordId']).currentPosition
    ever = nat.groupby('recordId').currentPosition.min()
    top10 = s[s['rank'].le(10) & s.week.ge(2)]
    first = top10.sort_values('week').groupby(['recordId', 'region']).head(1).copy()
    first['natPos'] = [pos.get(k, 0) for k in zip(first.week, first.recordId)]
    first['ever'] = first.recordId.map(ever).fillna(0)
    out['localTop10Entries'] = len(first)
    out['localTop10OffHot100SameWeekPct'] = 100 * (~first.natPos.between(1, 100)).mean()
    out['localTop10NeverHot100Pct'] = 100 * (~first.ever.between(1, 100)).mean()
    small = first.region.map(pop / pop.sum()).lt(.1)
    out['smallRegionLocalTop10OffHot100Pct'] = 100 * (~first[small].natPos.between(1, 100)).mean()
    out['distinctRegionalTop10Records'] = top10.recordId.nunique()
    # live singles reaching national Top 40: regional stagger and path
    t40 = nat[nat.currentPosition.le(40)].groupby('recordId').week.min()
    t10 = nat[nat.currentPosition.le(10)].groupby('recordId').week.min()
    lab = r.drop_duplicates('recordId').set_index('recordId').labelId
    fl = first[first.recordId.isin(live)]
    g = fl.groupby('recordId').week
    span = (g.max() - g.min()).rename('span')
    firstLocal = g.min().rename('firstLocal')
    nreg = g.size().rename('regions')
    hits = pd.concat([span, firstLocal, nreg], axis=1).join(t40.rename('t40'), how='inner')
    hits['indie'] = ~hits.index.map(lab).isin(BIG5)
    hits['lead'] = hits.t40 - hits.firstLocal
    regionsAtT40 = []
    for rid, row in hits.iterrows():
        regionsAtT40.append(int((fl[(fl.recordId == rid)].week <= row.t40).sum()))
    hits['regionsAtT40'] = regionsAtT40
    for label, h in [('all', hits), ('indie', hits[hits.indie]), ('big5', hits[~hits.indie])]:
        out[f't40_{label}_n'] = len(h)
        out[f't40_{label}_regionSpanGe6Pct'] = 100 * h.span.ge(6).mean() if len(h) else np.nan
        out[f't40_{label}_regionSpanMedian'] = h.span.median() if len(h) else np.nan
        out[f't40_{label}_localLeadGe2Pct'] = 100 * h.lead.ge(2).mean() if len(h) else np.nan
        out[f't40_{label}_regionsAtT40Median'] = h.regionsAtT40.median() if len(h) else np.nan
        out[f't40_{label}_oneRegionAtT40Pct'] = 100 * h.regionsAtT40.le(1).mean() if len(h) else np.nan
    # late-exposed regional mass for live singles (exposure from the observer, ages <= 14)
    lc = pathlib.Path(pre + 'local-consumption.csv')
    if lc.exists():
        o = pd.read_csv(lc, usecols=['recordId', 'region', 'age', 'fitExposureAge'])
        exp = o.sort_values('age').groupby(['recordId', 'region']).fitExposureAge.last()
        tot = s[s.recordId.isin(live)].groupby(['recordId', 'region']).clearedUnits.sum()
        j = pd.concat([tot.rename('units'), exp.rename('exp')], axis=1, join='inner')
        out['launchHeldRegionPct'] = 100 * j.exp.ne(0).mean()
        out['lateExposedRegionPct'] = 100 * j.exp.between(1, NOT_EXPOSED - 1).mean()
        out['lateExposedUnitsPct'] = 100 * j.loc[j.exp.between(1, NOT_EXPOSED - 1), 'units'].sum() / j.units.sum()
        out['neverExposedUnitsPct'] = 100 * j.loc[j.exp.eq(NOT_EXPOSED), 'units'].sum() / j.units.sum()
    # indie top-10 hits at national debut: concentration
    peak = nat.groupby('recordId').currentPosition.min()
    debut = nat.groupby('recordId').week.min()
    rows = []
    for rid in [x for x in peak[peak.le(10)].index if x in live and lab.get(x) not in BIG5]:
        x = s[(s.recordId == rid) & (s.week == debut[rid])]
        if x.clearedUnits.sum() <= 0: continue
        sh = x.set_index('region').clearedUnits / x.clearedUnits.sum()
        pc = x.set_index('region').clearedUnits / pop
        rows.append((sh.max(), pc.max() / max(pc.mean(), 1e-9), x.age.iloc[0]))
    if rows:
        d = np.array(rows)
        out['indieTop10DebutTopRegionShare'] = np.median(d[:, 0])
        out['indieTop10DebutPerCapitaMaxOverMean'] = np.median(d[:, 1])
        out['indieTop10DebutAge'] = np.mean(d[:, 2])
        out['indieTop10N'] = len(rows)
    # national concentration of live-single units and million sellers within the window
    tot = r[r.recordId.isin(live)].groupby('recordId').unitsThisWeek.sum()
    out['liveSingles'] = len(tot)
    out['liveSingleUnitsM'] = tot.sum() / 1e6
    out['liveOver250k'] = int(tot.ge(250_000).sum())
    out['liveOver1M'] = int(tot.ge(1_000_000).sum())
    out['liveUnder2kPct'] = 100 * tot.lt(2000).mean()
    out['liveTop1PctUnitShare'] = 100 * tot.sort_values(ascending=False).head(max(1, len(tot) // 100)).sum() / tot.sum()
    return out


def main():
    p = argparse.ArgumentParser()
    p.add_argument('tag')
    p.add_argument('--control', default='ctl')
    p.add_argument('--jobs', type=int, default=3)
    a = p.parse_args()
    folder = ROOT / 'SimLogs' / a.tag
    plan = json.loads((folder / 'plan.json').read_text())
    runs = []
    for run in plan:
        receipt = folder / (run['name'] + '-receipt.json')
        if not receipt.exists(): continue
        rc = json.loads(receipt.read_text())
        if not rc['complete'] or rc['exitCode'] or rc['assembly'] != rc['assemblyAfter']: continue
        runs.append(run)
    with concurrent.futures.ProcessPoolExecutor(max_workers=a.jobs) as pool:
        results = list(pool.map(analyze, [run['name'] for run in runs]))
    rows = [dict(name=run['name'], arm=run['arm'], seed=run['seed']) | res for run, res in zip(runs, results)]
    d = pd.DataFrame(rows)
    d.to_csv(folder / 'regional-metrics.csv', index=False)
    num = d.select_dtypes('number').columns.drop('seed')
    means = d.groupby('arm', sort=False)[num].mean()
    means.to_csv(folder / 'regional-means.csv')
    deltas = []
    ctl = d[d.arm.eq(a.control)].set_index('seed')[num]
    for arm, g in d.groupby('arm', sort=False):
        if arm == a.control: continue
        diff = g.set_index('seed')[num].subtract(ctl).dropna(how='all')
        for m in num:
            v = diff[m].dropna()
            deltas.append(dict(arm=arm, metric=m, n=len(v), delta=v.mean(), se=v.std() / len(v) ** .5 if len(v) > 1 else np.nan))
    pd.DataFrame(deltas).to_csv(folder / 'regional-paired.csv', index=False)
    print(means.T.round(2).to_string())


if __name__ == '__main__':
    main()
