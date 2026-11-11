"""Per-cohort single outcomes in the three 261-week follower channel arms (existing runs). Read-only."""
import sys, json, collections, statistics
from veteran_demand_common import *
import analyze_veteran_launch_week as lw

def summarize(run, year=1964):
    weeks, first, rho, books, traj, entries, launches = lw.load(run)
    res = {}
    for c in ('1960', '1961-62', '1963-64'):
        ls = [l for l in launches if l['year'] == year and cohort_label(first.get(l['artist'])) == c]
        acts = {l['artist'] for l in ls}
        t = [traj.get(l['rec'], {}) for l in ls]
        top10w = sum(sum(1 for x in tr.values() if x['pos'] <= 10) for tr in t)
        t10 = [tr for tr in t if any(x['pos'] <= 10 for x in tr.values())]
        peakunits = [max(x['units'] for x in tr.values()) for tr in t10]
        res[c] = dict(launches=len(ls), acts=len(acts), quality=statistics.fmean(l['q'] for l in ls), awareness=statistics.fmean(l['aw'] for l in ls),
            momentum=statistics.fmean(l['mom'] for l in ls), recognition=statistics.fmean(l['recog'] for l in ls),
            hot100=sum(bool(tr) for tr in t), top40=sum(any(x['pos'] <= 40 for x in tr.values()) for tr in t), top10=len(t10),
            top10_weeks=top10w, weeks_per_top10=top10w / max(1, len(t10)),
            hi_q=sum(l['q'] >= .78 for l in ls), hi_q_top10=sum(l['q'] >= .78 and any(x['pos'] <= 10 for x in tr.values()) for l, tr in zip(ls, t)),
            aware_hi=sum(l['aw'] >= .6 for l in ls))
    return res

if __name__ == '__main__':
    out = {}
    for arm in ('full', 'no-stock', 'no-intent'):
        out[arm] = summarize(f'veteran-follower-channels-261w-v2-2002-{arm}')
    for c in ('1960', '1961-62', '1963-64'):
        print(f'\n1964 single launches, cohort {c}')
        for arm, r in out.items():
            d = r[c]
            print(f"  {arm:9s} launches {d['launches']:4d} acts {d['acts']:3d} q {d['quality']:.3f} aware {d['awareness']:.2f} mom {d['momentum']:.2f} recog {d['recognition']:.2f}"
                  f" | Hot100 {d['hot100']:3d} ({d['hot100']/d['launches']:.0%}) Top40 {d['top40']:3d} ({d['top40']/d['launches']:.0%}) Top10 {d['top10']:3d} ({d['top10']/d['launches']:.1%})"
                  f" | Top10 wks {d['top10_weeks']:3d} = {d['weeks_per_top10']:.1f}/record | q>=.78: {d['hi_q']} -> Top10 {d['hi_q_top10']} | aware>=.6: {d['aware_hi']}")
    if len(sys.argv) > 1: json.dump(out, open(sys.argv[1], 'x'), indent=1)
