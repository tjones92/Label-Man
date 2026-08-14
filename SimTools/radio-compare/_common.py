import csv, re, os

SIMLOGS = r"C:\Project\Label-Man\SimLogs"
TARGETS = r"C:\Project\Label-Man\SimTools\AdjustedHistoricalGenreShareTargets.csv"

def norm(name):
    n = re.sub(r"\(.*?\)", "", name)          # drop parentheticals
    n = re.sub(r"[^A-Za-z0-9]", "", n).lower()  # strip non-alnum
    aliases = {"rb": "rnb"}                    # R&B -> RnB
    return aliases.get(n, n)

def load_shape(run):
    """run -> {(year, normgenre): {col: float/str}}  plus keeps display genre name."""
    path = os.path.join(SIMLOGS, f"{run}-genre-decade-shape.csv")
    rows = {}
    with open(path, newline="") as f:
        for r in csv.DictReader(f):
            y = int(r["year"])
            g = r["genre"].strip().strip('"')
            rows[(y, norm(g))] = {**r, "_genre": g}
    return rows

def load_targets():
    """(year, normgenre) -> target share pct (float)."""
    t = {}
    with open(TARGETS, newline="") as f:
        for r in csv.DictReader(f):
            g = norm(r["genre"])
            for y in range(1960, 1970):
                col = str(y)
                if col in r and r[col] != "":
                    t[(y, g)] = float(r[col])
    return t
