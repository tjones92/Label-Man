"""Which terms carry a starting-Small record across peakBreakoutScore >= .20 in 1960?"""
import sys
import pandas as pd

prefix = sys.argv[1]
lab = pd.read_csv(prefix + "-label-directory.csv")
small = set(lab.loc[lab.initialTier == "Small", "labelId"])

rec = pd.read_csv(prefix + "-records.csv", usecols=["week", "recordId", "labelId"])
rec = rec[rec.week <= 52].drop_duplicates("recordId")
smallRecs = set(rec.loc[rec.labelId.isin(small), "recordId"])
labelOf = rec.set_index("recordId").labelId

cols = ["week", "recordId", "regionId", "weeksSinceRelease", "rawSales", "unitsSoldThisWeek", "breakoutScore",
        "salesVelocity", "volumeInput", "velocityInput", "sustainedGrowthWeeks", "audienceInput", "mediaInput",
        "genreFitInput", "qualityInput", "unmetDemandInput", "distributionRegionCoverage"]
parts = []
for chunk in pd.read_csv(prefix + "-breakout-funnel.csv", usecols=cols, chunksize=2_000_000):
    chunk = chunk[(chunk.week <= 52) & chunk.recordId.isin(smallRecs)]
    parts.append(chunk)
f = pd.concat(parts)
print("Small record-region-weeks in 1960:", len(f), "records:", f.recordId.nunique())

f["sustainedInput"] = (f.sustainedGrowthWeeks / 3).clip(0, 1)
w = {"volumeInput": .30, "velocityInput": .15, "sustainedInput": .09, "audienceInput": .12,
     "mediaInput": .10, "genreFitInput": .08, "qualityInput": .08, "unmetDemandInput": .08}
for k, v in w.items():
    f["t_" + k] = f[k] * v
f["linear"] = sum(f["t_" + k] for k in w)
f["evidence"] = f.linear * (0.70 + f.volumeInput * 0.30)

# first crossing per record-region
f = f.sort_values(["recordId", "regionId", "week"])
cross = f[f.breakoutScore >= 0.20].groupby(["recordId", "regionId"]).head(1)
firstPerRecord = cross.sort_values("week").groupby("recordId").head(1)
print("Small records ever >= .20 in 1960: %d of %d (%.1f%%)" % (
    firstPerRecord.recordId.nunique(), f.recordId.nunique(), 100 * firstPerRecord.recordId.nunique() / f.recordId.nunique()))
print("\nAt the first crossing week (record's first region):")
print(" weeksSinceRelease median %.0f" % firstPerRecord.weeksSinceRelease.median())
print(" rawSales (regional weekly demand) quartiles:", firstPerRecord.rawSales.quantile([.1, .25, .5, .75, .9]).round(0).to_dict())
print(" units sold that week quartiles:", firstPerRecord.unitsSoldThisWeek.quantile([.1, .25, .5, .75, .9]).round(0).to_dict())
print(" covered region share: %.2f" % firstPerRecord.distributionRegionCoverage.mean())
print("\nMean weighted contribution at crossing (linear sum before the volume multiplier):")
for k in w:
    print("  %-17s %.3f" % (k, firstPerRecord["t_" + k].mean()))
print("  %-17s %.3f" % ("linear", firstPerRecord.linear.mean()))
print("  %-17s %.3f" % ("volume multiplier", (0.70 + firstPerRecord.volumeInput * 0.30).mean()))
print(" velocityInput at crossing: share at 1.0 = %.2f; salesVelocity median %.2f" % (
    (firstPerRecord.velocityInput >= 0.999).mean(), firstPerRecord.salesVelocity.median()))
nonvol = firstPerRecord.linear - firstPerRecord.t_volumeInput
print(" share of linear evidence NOT from volume: %.2f" % (nonvol / firstPerRecord.linear).mean())

# counterfactual: with velocity + sustained zeroed (a growth signal that needs volume), how many records cross?
for name, mask_cols in [("no velocity/sustained", ["t_velocityInput", "t_sustainedInput"]),
                         ("no velocity/sustained/genreFit/quality", ["t_velocityInput", "t_sustainedInput", "t_genreFitInput", "t_qualityInput"])]:
    lin = f.linear - sum(f[c] for c in mask_cols)
    ev = lin * (0.70 + f.volumeInput * 0.30)
    # breakoutScore is a lerp of evidence; peak of evidence is an upper bound for the score.
    n = f[ev >= 0.20].recordId.nunique()
    labels = labelOf.reindex(f[ev >= 0.20].recordId.unique()).nunique()
    print("counterfactual %s: records whose raw evidence ever >= .20: %d; labels %d of %d" % (name, n, labels, len(small)))

# volume needed: express rawSales at crossing as share of region's p50 Small single weekly demand
labs = labelOf.reindex(firstPerRecord.recordId).nunique()
print("\nlabels with a record crossing: %d" % labs)
