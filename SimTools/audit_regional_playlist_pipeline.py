"""Static audit of saved 1960 playlists; never launches the simulator.

Reconstructs commercial fit from the production Philox draw (profile: sigma 2.5,
hit share .15, upside .15, launch weight .25, floor .12, no push weight).
Replays observation/eligibility filters on recorded lists with national outcomes
held fixed. These are diagnostic counterfactuals, NOT sales forecasts. Early adds
are observable; exact pick provenance is not exported and is not inferred as fact.

Usage: python SimTools/audit_regional_playlist_pipeline.py
Outputs: SimLogs/regprobe/playlist-pipeline-audit/*.csv
"""
import argparse
import math
from pathlib import Path
from statistics import NormalDist

import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parents[1]
LOG = ROOT / 'SimLogs'
OUT = LOG / 'regprobe/playlist-pipeline-audit'
BIG5 = {f'label_000{i}' for i in range(1, 6)}
CITIES = ['Los Angeles', 'Minneapolis']
MASK = (1 << 64) - 1


def identity(s):
    h = 14695981039346656037
    for c in s:
        h = ((h ^ ord(c)) * 1099511628211) & MASK
    return h


def philox(seed, rid, region, decision):
    e, k = identity(rid), seed ^ identity(region)
    a, b, c, d = e & 0xffffffff, e >> 32, 0, decision
    k0, k1 = k & 0xffffffff, k >> 32
    for _ in range(10):
        left, right = 0xD2511F53 * a, 0xCD9E8D57 * c
        a, b, c, d = (right >> 32) ^ b ^ k0, right & 0xffffffff, (left >> 32) ^ d ^ k1, left & 0xffffffff
        k0, k1 = (k0 + 0x9E3779B9) & 0xffffffff, (k1 + 0xBB67AE85) & 0xffffffff
    return ((a << 21) | (b >> 11)) / 9007199254740992


def normal(seed, rid):
    u = philox(seed, rid, 'national', 0x43460000)
    v = philox(seed, rid, 'national', 0x43460001)
    return math.sqrt(-2 * math.log(max(u, 1e-300))) * math.cos(2 * math.pi * v)


def band_normal(seed, key):
    values = []
    for i in range(4):
        h = identity(f'{seed}|band-life|{key}#{i}')
        h ^= h >> 33
        h = (h * 0xff51afd7ed558ccd) & MASK
        h ^= h >> 33
        h = (h * 0xc4ceb9fe1a85ec53) & MASK
        h ^= h >> 33
        values.append(np.float32((h >> 40) / (1 << 24)))
    return float(np.float32(np.float32(sum(values, np.float32(0))) - 2) * np.float32(1.7320508))


def fit_log(seed, ids, awareness):
    # CDF approximation in production differs from NormalDist by <1.5e-7.
    cdf = NormalDist().cdf
    kink = NormalDist().inv_cdf(.85)
    norm = math.log(math.exp(-2.5*kink + .5*2.5**2)*cdf(kink-2.5)
                    + math.exp(-.15*kink + .5*.15**2)*(1-cdf(kink-.15)))
    z = np.array([normal(seed, rid) for rid in ids])
    k = kink - .25*(awareness.to_numpy()-.2)
    value = np.maximum(np.where(z < k, 2.5*(z-k), .15*(z-k))-norm, math.log(.12))
    return value, max(-2.5*kink-norm, math.log(.12)), z >= k


def read(seed, tag, suffix, cols=None):
    return pd.read_csv(LOG / f'hs52op{seed}-SMP-HGRPO-{tag}-{suffix}.csv', usecols=cols)


def load(seed, tag):
    r = read(seed, tag, 'records', ['week','year','recordId','labelId','labelTier','genre',
        'weeksSinceRelease','currentPosition','initialLaunchAwareness','radioHeat','awareness','unitsThisWeek'])
    r = r[r.year.eq(1960)].copy()
    r['launch'] = r.week-r.weeksSinceRelease+1
    # Restrict to singles by the demand export; records.csv also contains albums.
    singles = set(read(seed, tag, 'single-launch-quality', ['singleId']).singleId)
    first = r.sort_values('week').drop_duplicates('recordId').set_index('recordId')
    ch = r[r.currentPosition.between(1,100)]
    nat = ch.groupby('recordId').agg(debut=('week','min'), peak=('currentPosition','min'))
    d = first[['labelId','labelTier','genre','launch','initialLaunchAwareness']].join(nat)
    d['liveSingle'] = d.index.isin(singles) & d.launch.ge(1)
    d['big5'] = d.labelId.isin(BIG5)
    d['bucket'] = pd.cut(d.peak.fillna(999), [0,10,40,100,1000], labels=['top10','11-40','41-100','never'])
    d['fit'], cap, d['aboveKink'] = fit_log(seed, d.index, d.initialLaunchAwareness)
    d['fitAboveCap'] = d.fit-cap
    d['debutAge'] = d.debut-d.launch+1
    x = read(seed, tag, 'station-playlists')
    x = x[x.year.eq(1960) & x.format.isin(['Top40','FullService'])].copy()
    x = x.join(d[['launch','fit','initialLaunchAwareness']], on='recordId')
    x['age'] = x.week-x.launch+1
    noise = {(s,rid):band_normal(seed, f'dj-ear|{s}|{rid}')
             for s,rid in set(zip(x.stationId,x.recordId))}
    x['heardAboveCap'] = [f-cap+.5*noise[s,rid]
                          for f,s,rid in zip(x.fit,x.stationId,x.recordId)]
    x['tailPickSeat'] = (tag == 'djb24u') & x['rank'].gt(x['format'].map({'Top40':33,'FullService':34}))
    return d, x


def metric(d, x, scenario, seed, tag):
    city = x[x.city.isin(CITIES)]
    first = city.groupby('recordId').week.min()
    v = d.join(first.rename('firstCity'))
    v['lead'] = v.debut-v.firstCity
    v = v[v.debut.between(7,46)]
    groups = {'indie10': v.peak.le(10)&~v.big5, 'big5_10': v.peak.le(10)&v.big5,
              '11-40': v.peak.between(11,40), '41-100': v.peak.between(41,100), 'all': v.peak.le(100)}
    return [dict(tag=tag, seed=seed, scenario=scenario, group=name, n=int(m.sum()),
                 lead2=int(v.loc[m,'lead'].ge(2).sum()), onAny=int(v.loc[m,'firstCity'].notna().sum()))
            for name,m in groups.items()]


def audit(seed, tag):
    d, x = load(seed, tag)
    rows, counts = [], []
    rows += metric(d,x,'observed',seed,tag)
    city = x[x.city.isin(CITIES)]
    # Filter list observations, retaining all subsequent sales/playlist outcomes.
    # No reseating, demand feedback or changes to chart outcomes are claimed.
    variants = {'rank<=30': x['rank'].le(30), 'rank<=20': x['rank'].le(20),
                'exclude_pick_seats': ~x.tailPickSeat,
                'no_age<=2': ~x.age.le(2), 'no_light_age<=2': ~(x.age.le(2)&x.tier.eq('Light'))}
    for a in [.1,.2,.3,.4,.5]:
        variants[f'early_awareness>={a}'] = ~x.age.le(2) | x.initialLaunchAwareness.ge(a)
    for name,mask in variants.items():
        rows += metric(d,x[mask],name,seed,tag)
    # Two successive weeks at the same station: first-date vs confirmation-date definitions.
    keys = set(zip(x.stationId,x.recordId,x.week))
    sustained = np.array([(s,r,w+1) in keys for s,r,w in zip(x.stationId,x.recordId,x.week)])
    rows += metric(d,x[sustained],'two_week_start',seed,tag)
    confirmed = x[sustained].copy(); confirmed['week'] += 1
    rows += metric(d,confirmed,'two_week_confirm',seed,tag)
    for cityname,g in city.groupby('city'):
        first = g.groupby('recordId').week.min()
        actual = first[first.between(5,44)]
        old = g[g.week.between(5,44)].groupby('recordId').week.min()
        for method,f in [('window_first_old',old),('true_first',actual)]:
            ever = d.reindex(f.index).peak.notna()
            counts.append(dict(seed=seed,tag=tag,city=cityname,method=method,n=len(f),never=int((~ever).sum())))
    # Per-record adds: only live singles; bounded launch window prevents late-year censoring.
    fc = city.sort_values(['week','rank']).drop_duplicates('recordId').set_index('recordId')
    d = d.join(fc[['week','age','tier','rank','heardAboveCap']].add_prefix('first_'))
    d['lead'] = d.debut-d.first_week
    d['early'] = d.first_age.le(2)
    d['eligibleLeadAtAge1'] = d.debutAge.ge(3)
    d['seed'], d['tag'] = seed, tag
    d['cityWeeks'] = city.groupby('recordId').week.nunique().reindex(d.index).fillna(0)
    # Exact station-level early adds, including whether the record survives age 2 -> 3.
    sf = x.sort_values('week').drop_duplicates(['stationId','recordId']).copy()
    sf = sf[sf.age.between(1,2) & sf.recordId.isin(d[d.liveSingle & d.launch.between(5,44)].index)].copy()
    sf['nextWeek'] = [(s,r,w+1) in keys for s,r,w in zip(sf.stationId,sf.recordId,sf.week)]
    sf['age3'] = [(s,r,int(l+2)) in keys for s,r,l in zip(sf.stationId,sf.recordId,sf.launch)]
    sf = sf.join(d[['bucket','big5','labelTier','aboveKink']],on='recordId')
    sf['seed'], sf['tag'] = seed,tag
    # Empty reserved seats: use observed max no-pick capacity by format (1960).
    occupancy = x.groupby(['stationId','week','format']).size().rename('size').reset_index()
    occupancy['capacity'] = occupancy['format'].map({'Top40':35,'FullService':36})
    occupancy['seed'],occupancy['tag'] = seed,tag
    return rows,counts,d.reset_index(),sf,occupancy


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--seeds',default='1001,1002,1003,1004')
    p.add_argument('--tags',default='ufxal025,djb24u')
    a = p.parse_args()
    OUT.mkdir(parents=True,exist_ok=True)
    metrics,precision,records,adds,occupancy = [],[],[],[],[]
    for tag in a.tags.split(','):
        for seed in map(int,a.seeds.split(',')):
            print(f'Reading {tag} {seed}',flush=True)
            m,c,d,s,o = audit(seed,tag)
            metrics.extend(m); precision.extend(c); records.append(d); adds.append(s); occupancy.append(o)
    m = pd.DataFrame(metrics); m.to_csv(OUT/'replay-per-seed.csv',index=False)
    pool = m.groupby(['tag','scenario','group'],sort=False)[['n','lead2','onAny']].sum().reset_index()
    pool['leadPct'] = 100*pool.lead2/pool.n
    pool.to_csv(OUT/'replay-pooled.csv',index=False)
    c = pd.DataFrame(precision); c.to_csv(OUT/'precision-per-seed.csv',index=False)
    d = pd.concat(records,ignore_index=True); d.to_csv(OUT/'records.csv',index=False)
    s = pd.concat(adds,ignore_index=True); s.to_csv(OUT/'early-station-adds.csv',index=False)
    pd.concat(occupancy,ignore_index=True).to_csv(OUT/'occupancy.csv',index=False)
    print(pool.pivot(index=['tag','scenario'],columns='group',values='leadPct').round(1).to_string())
    eligible = d[d.liveSingle & d.launch.between(5,44)]
    summary = eligible.groupby(['tag','bucket'],observed=True).agg(n=('recordId','size'),
        fit=('fitAboveCap','median'), awareness=('initialLaunchAwareness','median'),
        aboveKink=('aboveKink','mean'),early=('early','mean'),debutAge=('debutAge','median'),
        canLead=('eligibleLeadAtAge1','mean'))
    summary.to_csv(OUT/'cohorts.csv'); print(summary.round(3).to_string())
    print(f'Artifacts: {OUT}')


if __name__ == '__main__':
    main()
