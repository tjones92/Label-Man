"""Plot completed five-seed results; every input is a saved analysis artifact."""
import argparse
from pathlib import Path

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import pandas as pd

from analyze_genre_hook_calibration import norm


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--routes", required=True, type=Path)
    parser.add_argument("--market-summary", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    routes = pd.read_csv(args.routes)
    routes["tag"] = routes.run.str.rsplit("-", n=2).str[0]
    market = pd.read_csv(args.market_summary)
    target = pd.read_csv(Path(__file__).resolve().parent / "AdjustedHistoricalGenreShareTargets.csv")
    soul_target = target[target.genre.map(norm).eq("soul")].iloc[0]
    colors = {"awrw-on": "#426a98", "apr6-main": "#cc6b3e"}
    labels = {"awrw-on": "Reference bundle", "apr6-main": "Release-practice candidate"}
    fig, axes = plt.subplots(1, 3, figsize=(14, 4.5), layout="constrained")
    for tag, color in colors.items():
        frame = routes[routes.tag.eq(tag) & routes.genre.eq("TraditionalPop")]
        group = frame.groupby("year").advancePctOfSingles.agg(["mean", "std", "count"])
        axes[0].errorbar(group.index, group["mean"], yerr=group["std"] / group["count"].pow(.5),
                         color=color, marker="o", capsize=3, label=labels[tag])
        for axis, metric in [(axes[1], "Share:Soul"), (axes[2], "CR8")]:
            data = market[market.tag.eq(tag) & market.metric.eq(metric)].sort_values("year")
            axis.errorbar(data.year, data["mean"], yerr=data.se, color=color, marker="o", capsize=3, label=labels[tag])
    axes[1].plot(range(1960, 1965), [soul_target[str(y)] for y in range(1960, 1965)], "k:", label="Project historical benchmark")
    axes[2].plot(range(1960, 1965), [48, 38, 38, 38, 38], color="#777777", linestyle=":", label="Design floor")
    axes[0].set(title="Traditional pop: advance cuts", ylabel="% of all single launches", ylim=(0, 100))
    axes[1].set(title="Soul: combined market share", ylabel="% of single + LP units", ylim=(0, 15))
    axes[2].set(title="Top 10 label concentration", ylabel="Top-eight labels' slot-week share (%)")
    for axis in axes:
        axis.set_xticks(range(1960, 1965))
        axis.grid(axis="y", alpha=.20)
        axis.spines[["top", "right"]].set_visible(False)
        axis.legend(frameon=False, fontsize=8, loc="best")
    fig.suptitle("1960-64 simulation: changing the album/single release pipeline", fontsize=14)
    fig.supxlabel("Error bars: one seed-level standard error (five seeds). No historical advance-rate census is claimed.", fontsize=9)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    fig.savefig(args.output, dpi=160)
    print(args.output)


if __name__ == "__main__":
    main()
