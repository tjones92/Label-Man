"""Causal screens for the media writers: --media-recognition and --media-demand (MediaRecognitionAdapterDesign.md, directive §12.3).

Usage:  py SimTools/analyze_media_recognition_screen.py <control-run> <treatment-run> [<control-run> <treatment-run> ...]

Reads each pair of 1963-66 resumes from SimLogs and prints treatment against control:
  concentration  top 1/5/10% of acts' share of chart slot-weeks over the whole run (lifecycles weeksOnChart; per-week
                 rows are not kept for these resumes), kill rule: top 10% up more than 3 points
  Big-5          major-family chart share and CR4 per year (concentration.csv)
  genre          slot-week share per genre over the run, flagged beyond +-1 point
  units          single and album units per year (decade-annual-rollup.csv), flagged beyond +-1%
  media          standing dates by years since last hit, Sullivan's bill, MEDIA_RECOGNITION, closures (console)
Also reads MEDIA_DEMAND (the §12.3 adapter). Diagnostic only. Single-seed reshuffle noise is about the size of these limits, so this catches only a catastrophe.
"""
import glob
import os
import re
import sys

import pandas as pd

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOGS = os.path.join(ROOT, "SimLogs")
YEARS = [1963, 1964, 1965, 1966]


def where(run, file):
    """A run's file in SimLogs, SimLogs/<run>/ (references) or SimLogs/tvbook/runs/<run>/ (gates)."""
    for d in (LOGS, os.path.join(LOGS, run), os.path.join(LOGS, "tvbook", "runs", run), os.path.join(LOGS, "tvbook")):
        path = os.path.join(d, f"{run}-{file}")
        if os.path.exists(path):
            return path
    # RunCorporateEconomyAudit.ps1 keeps a tag's console logs in SimLogs/<tag>/.
    hits = glob.glob(os.path.join(LOGS, "*", f"{run}-{file}"))
    return hits[0] if hits else os.path.join(LOGS, f"{run}-{file}")


def read(run, name, **kw):
    return pd.read_csv(where(run, f"{name}.csv"), low_memory=False, **kw)


def console(run):
    for path in (where(run, "console.log"),):
        if os.path.exists(path):
            with open(path, encoding="utf-8", errors="replace") as f:
                return f.read().splitlines()
    return []


def slot_weeks(run):
    """Chart weeks per record over the run, with the record's act and genre."""
    life = read(run, "lifecycles", usecols=["recordId", "weeksOnChart"])
    life = life[life.weeksOnChart > 0].drop_duplicates("recordId", keep="last")
    # song-material and records are truncated for these resumes; the release-lane log names every single's act and genre.
    # Calibration runs (the decade game profile) write no lane log; they write record-acts beside the lifecycles instead.
    who = read(run, "single-release-lanes", usecols=["recordId", "artistId", "genre"]).drop_duplicates("recordId")
    if len(who) == 0 and os.path.exists(where(run, "record-acts.csv")):
        who = read(run, "record-acts", usecols=["recordId", "artistId", "genre"]).drop_duplicates("recordId")
    out = life.merge(who, on="recordId", how="left")
    missing = out.artistId.isna()
    if missing.any() and os.path.exists(where(run, "live-records-snapshot.csv")):
        snap = read(run, "live-records-snapshot", usecols=["recordId", "artistId"]).drop_duplicates("recordId")
        out.loc[missing, "artistId"] = out.loc[missing, "recordId"].map(snap.set_index("recordId").artistId)
    return out


def concentration(sw):
    acts = sw.dropna(subset=["artistId"]).groupby("artistId").weeksOnChart.sum().sort_values(ascending=False)
    total = acts.sum()
    res = {"acts": len(acts), "slotWeeks": int(total), "unattributed": int(sw[sw.artistId.isna()].weeksOnChart.sum())}
    for p in (0.01, 0.05, 0.10):
        n = max(1, int(round(p * len(acts))))
        res[f"top{int(p * 100)}%"] = acts.iloc[:n].sum() / total
    return res


def media(run):
    lines = console(run)
    out = {}
    for y in YEARS:
        row = {}
        for line in lines:
            if line.startswith(f"MEDIA_AI year={y} "):
                for k in ("0-1y", "2-3y", "4y+", "never"):
                    m = re.search(rf"standing-lasthit-{re.escape(k)}:(\d+)", line)
                    row[f"stand{k}"] = int(m.group(1)) if m else 0
            elif line.startswith(f"MEDIA_PROGRAMS year={y} "):
                m = re.search(r'"The Ed Sullivan Show" (\d+)/(\d+)/(\d+)', line)
                if m: row["sullivan"] = "/".join(m.groups())
            elif line.startswith(f"MEDIA_RECOGNITION year={y} "):
                for k in ("applied", "chartEarned", "tvShare", "actsAtCap", "over05", "over20", "hitmakersTvActive"):
                    m = re.search(rf" {re.escape(k)}=([^ ]+)", line)
                    if m: row[k] = m.group(1)
                m = re.search(r"recog\[noTv/withTv\]=([^ ]+)", line)
                if m: row["tvHitmakers noTv/withTv"] = m.group(1)
                m = re.search(r" hitmakersQuiet=(\d+) recog=([^ ]+)", line)
                if m: row["quietHitmakers"] = f"{m.group(1)}@{m.group(2)}"
            elif line.startswith(f"MEDIA_DEMAND year={y} "):
                for k in ("intent", "moved", "unmatched", "requestTitleWeeks", "frenzy", "noBaseline", "regionCapBinding", "physicalBinding"):
                    m = re.search(rf" {re.escape(k)}=([^ ]+)", line)
                    if m: row["demand." + k] = m.group(1)
                m = re.search(r"wanted\[up/down\]=([^ ]+)", line)
                if m: row["demand.wanted"] = m.group(1)
                m = re.search(r" moved=\d+ \(([^)]+)\)", line)
                if m: row["demand.movedShare"] = m.group(1)
            elif line.startswith(f"MEDIA_STANDING year={y} "):
                m = re.search(r"dates=(\d+) acts=(\d+) recog\[p25/50/75\]=([^ ]+)", line)
                if m: row["standing dates/acts/recog"] = f"{m.group(1)}/{m.group(2)}/{m.group(3)}"
        out[y] = row
    out["closures"] = sum(1 for line in lines if "has closed. Reason:" in line)
    return out


def pair(ctl, trt):
    global YEARS
    # The years both runs report: 1963-66 for the resumes, 1960-61 for the 104-week gates.
    YEARS = sorted(set(read(ctl, "concentration").year) & set(read(trt, "concentration").year))
    print(f"\n=== {trt} vs {ctl} ===")
    sc, st = slot_weeks(ctl), slot_weeks(trt)
    cc, ct = concentration(sc), concentration(st)
    print("[concentration] whole run, acts' share of slot-weeks")
    for k in cc:
        a, b = cc[k], ct[k]
        if isinstance(a, float):
            flag = "  KILL" if k == "top10%" and (b - a) > 0.03 else ""
            print(f"   {k:14s} {a:.4f} -> {b:.4f}  ({(b - a) * 100:+.2f} pt){flag}")
        else:
            print(f"   {k:14s} {a} -> {b}")

    conc = {r: read(r, "concentration").set_index("year") for r in (ctl, trt)}
    print("[Big-5] majorFamilyChartShare / c4ChartShare / top40Major / insolvencies-closures below")
    for y in YEARS:
        a, b = conc[ctl].loc[y], conc[trt].loc[y]
        print(f"   {y}  major {a.majorFamilyChartShare:.3f} -> {b.majorFamilyChartShare:.3f} ({(b.majorFamilyChartShare - a.majorFamilyChartShare) * 100:+.1f} pt)"
              f"   CR4 {a.c4ChartShare:.3f} -> {b.c4ChartShare:.3f}   top40 major {a.top40Major} -> {b.top40Major}   entries {a.chartEntries} -> {b.chartEntries}")

    print("[genre] slot-week share over the run (|delta| >= 1 pt flagged)")
    ga = sc.groupby("genre").weeksOnChart.sum(); ga /= ga.sum()
    gb = st.groupby("genre").weeksOnChart.sum(); gb /= gb.sum()
    g = pd.concat([ga, gb], axis=1, keys=["ctl", "trt"]).fillna(0)
    g["d"] = (g.trt - g.ctl) * 100
    for name, r in g.sort_values("ctl", ascending=False).iterrows():
        if r.ctl >= 0.01 or abs(r.d) >= 1:
            print(f"   {name:24s} {r.ctl * 100:5.1f} -> {r.trt * 100:5.1f}  ({r.d:+.1f}){'  !' if abs(r.d) >= 1 else ''}")

    roll = {r: read(r, "decade-annual-rollup").set_index("year") for r in (ctl, trt)}
    print("[units] (|delta| >= 1% flagged)")
    for y in YEARS:
        a, b = roll[ctl].loc[y], roll[trt].loc[y]
        s = (b.singleUnits / a.singleUnits - 1) * 100
        al = (b.albumUnits / a.albumUnits - 1) * 100
        print(f"   {y}  singles {a.singleUnits / 1e6:7.2f}M -> {b.singleUnits / 1e6:7.2f}M ({s:+.2f}%){'  !' if abs(s) >= 1 else ''}"
              f"   albums {a.albumUnits / 1e6:7.2f}M -> {b.albumUnits / 1e6:7.2f}M ({al:+.2f}%){'  !' if abs(al) >= 1 else ''}")

    ma, mb = media(ctl), media(trt)
    print(f"[closures] {ma['closures']} -> {mb['closures']}")
    print("[media]")
    for y in YEARS:
        print(f"   {y} ctl {ma[y]}")
        print(f"   {y} trt {mb[y]}")


def main():
    args = sys.argv[1:]
    if len(args) < 2 or len(args) % 2:
        sys.exit(__doc__)
    for i in range(0, len(args), 2):
        pair(args[i], args[i + 1])


if __name__ == "__main__":
    main()
