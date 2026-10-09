"""§16.4 measure 6 -- Jazz substance and exits, control vs treatment.

  py SimTools/jazz_life_check.py <ctl-run> <trt-run> [--min-year 1960] [--max-year 1969]

Jazz = Jazz or BossaNova, by the act's formation genre (artist-population-events), falling back to the genre of
its first record. Onsets are `substance-onset` rows in lineup-events; deaths are `departure` rows of kind Death on the
Substance channel. Reports per-year counts, the +/-10% test over the window, and Jazz's own non-substance exits by
kind (the "move earlier" half). Coverage prints the share of events whose act has a known genre.
"""
import argparse, os, sys
import pandas as pd

LOGS = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "SimLogs")
JAZZ = {"Jazz", "BossaNova"}


def load(run):
    """`early+late` stitches a 1960-start run (years <= 1964) to its resumed window (years >= 1965)."""
    if "+" in run:
        early, late = run.split("+")
        a, b = load(early), load(late)
        return pd.concat([a[a.year <= 1964], b[b.year >= 1965]], ignore_index=True)
    p = lambda s: os.path.join(LOGS, f"{run}-{s}.csv")
    ev = pd.read_csv(p("lineup-events"), low_memory=False)
    pop = pd.read_csv(p("artist-population-events"), usecols=["artistId", "formationPrimaryGenre"], low_memory=False)
    pop = pop.dropna().drop_duplicates("artistId").set_index("artistId")["formationPrimaryGenre"]
    rec = pd.read_csv(p("records"), usecols=["artistId", "genre"], low_memory=False)
    rec = rec.drop_duplicates("artistId").set_index("artistId")["genre"]
    genre = pop.combine_first(rec)
    ev["genre"] = ev["artistId"].map(genre)
    ev["jazz"] = ev["genre"].isin(JAZZ)
    return ev


def per_year(ev, mask, lo, hi):
    s = ev[mask & ev.year.between(lo, hi)]
    return s.groupby("year").size().reindex(range(lo, hi + 1), fill_value=0)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("ctl"); ap.add_argument("trt")
    ap.add_argument("--min-year", type=int, default=1960)
    ap.add_argument("--max-year", type=int, default=1969)
    a = ap.parse_args()
    lo, hi = a.min_year, a.max_year
    runs = {"ctl": load(a.ctl), "trt": load(a.trt)}

    def rows(ev):
        on = ev.event == "substance-onset"
        dead = (ev.event == "departure") & (ev.kind == "Death") & (ev.channel == "Substance")
        return {"onsets (all)": per_year(ev, on, lo, hi), "onsets (Jazz)": per_year(ev, on & ev.jazz, lo, hi),
                "sub deaths (all)": per_year(ev, dead, lo, hi), "sub deaths (Jazz)": per_year(ev, dead & ev.jazz, lo, hi)}

    out = {k: rows(v) for k, v in runs.items()}
    print(f"years {lo}-{hi}; genre known for ctl {runs['ctl'].genre.notna().mean():.1%} / trt {runs['trt'].genre.notna().mean():.1%} of events")
    for key in ("onsets (all)", "onsets (Jazz)", "sub deaths (all)", "sub deaths (Jazz)"):
        df = pd.DataFrame({"ctl": out["ctl"][key], "trt": out["trt"][key]})
        df["d"] = df.trt - df.ctl
        tc, tt = df.ctl.sum(), df.trt.sum()
        print(f"\n{key}\n{df.T.to_string()}\n  total ctl {tc}  trt {tt}  ({(tt - tc) / tc:+.1%})" if tc else f"\n{key}: ctl 0")
    for half, (h0, h1) in {"early": (lo, min(hi, 1964)), "late": (max(lo, 1965), hi)}.items():
        if h0 > h1: continue
        c = out["ctl"]["onsets (Jazz)"].loc[h0:h1].sum(); t = out["trt"]["onsets (Jazz)"].loc[h0:h1].sum()
        print(f"\nJazz onsets {half} {h0}-{h1}: ctl {c}  trt {t}  ({(t - c) / c:+.1%})" if c else f"\nJazz onsets {half}: ctl 0 trt {t}")

    print("\nJazz departures by kind and year (treatment | control)")
    for name, ev in (("trt", runs["trt"]), ("ctl", runs["ctl"])):
        d = ev[(ev.event == "departure") & ev.jazz & ev.year.between(lo, hi)]
        print(f"-- {name}\n{pd.crosstab(d.kind, d.year).to_string()}")


if __name__ == "__main__":
    sys.exit(main())
