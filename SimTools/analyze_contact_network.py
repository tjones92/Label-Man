"""Contact network + session employment against a stored reference (SimTools/ContactNetworkDirective.md).

usage: py SimTools/analyze_contact_network.py TREATMENT_TAG REF_PREFIX [SEED ...]
  e.g. py SimTools/analyze_contact_network.py net-v1 live-inc-v1-live 1001 2002
"""
import pathlib
import re
import sys

import pandas as pd

LOGS = pathlib.Path(__file__).resolve().parent.parent / "SimLogs"


def find(prefix, suffix):
    hits = list(LOGS.glob(f"{prefix}-{suffix}")) + list(LOGS.glob(f"ref-*/{prefix}-{suffix}"))
    if not hits:
        raise FileNotFoundError(f"{prefix}-{suffix}")
    return hits[0]


def main():
    tag, ref_prefix, seeds = sys.argv[1], sys.argv[2], sys.argv[3:] or ["1001", "2002"]
    for seed in seeds:
        trt, ref = f"{tag}-live-{seed}", f"{ref_prefix}-{seed}"
        print(f"\n=== seed {seed}: {trt} vs {ref}")
        console = (LOGS / tag / f"{trt}-console.log").read_text(encoding="utf-8", errors="replace")
        census = re.findall(r"CONTACT_NETWORK_CENSUS .*", console)
        print(census[-1] if census else "no census line")

        work = pd.read_csv(find(trt, "session-work.csv"))
        for year, w in work.groupby("year"):
            top = w.sort_values("sessions", ascending=False)
            share10 = top.head(10).sessions.sum() / max(w.sessions.sum(), 1)
            print(f"{year}: {len(w)} session players, {w.sessions.sum()} player-sessions, pay ${w.pay.sum():,.0f}; "
                  f"sessions/player median {w.sessions.median():.0f}, p90 {w.sessions.quantile(.9):.0f}, max {w.sessions.max()}; "
                  f"top-10 share {share10:.1%}; pooled players {int((w.inAct == 0).sum())}")
            by_city = w.groupby("basePlaceId").agg(players=("personId", "nunique"), sessions=("sessions", "sum"))
            print("   busiest cities:", by_city.sort_values("sessions", ascending=False).head(6).to_dict("index"))

        regs = pd.read_csv(find(trt, "session-regulars.csv"))
        per_label = regs.groupby("labelId").sessions.agg(["count", "sum"])
        core = regs[regs.sessions >= 10].groupby("labelId").size()
        print(f"labels using crews {len(per_label)}; players per label median {per_label['count'].median():.0f}; "
              f"labels with a core (>=10 dates) crew {len(core)}, core size median {core.median() if len(core) else 0:.0f}")

        edges = pd.read_csv(find(trt, "contact-edges.csv"))
        print("edges by kinds:", edges.kinds.value_counts().to_dict(), "fallout", int(edges.fallout.sum()))

        r, t = pd.read_csv(find(ref, "lineup-annual.csv")), pd.read_csv(find(trt, "lineup-annual.csv"))
        cols = ["poolSize", "poolEntries", "poolExpired", "replacements", "replacementsFromPool", "recombinations",
                "dissolutions", "kindDayJob", "kindSessionWork", "strainDepartures"]
        print("lineup-annual 1960, ref -> trt:", {c: f"{int(r[c].iloc[0])}->{int(t[c].iloc[0])}" for c in cols if c in r})

        r, t = pd.read_csv(find(ref, "decade-annual-rollup.csv")), pd.read_csv(find(trt, "decade-annual-rollup.csv"))
        print("units, 2 years:", {c: f"{(t[c].sum() - r[c].sum()) / r[c].sum() * 100:+.2f}%" for c in ("singleUnits", "albumUnits")})

        m = pd.read_csv(find(trt, "lineup-members.csv"))
        m60 = m[m.year == 1960]
        mr = pd.read_csv(find(ref, "lineup-members.csv"))
        mr60 = mr[mr.year == 1960]
        print(f"member wealth 1960 median ref {mr60.wealth.median():.0f} -> trt {m60.wealth.median():.0f}; "
              f"p99 {mr60.wealth.quantile(.99):.0f} -> {m60.wealth.quantile(.99):.0f}")


if __name__ == "__main__":
    main()
