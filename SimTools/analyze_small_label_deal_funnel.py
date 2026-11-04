"""Starting-Small P&D deal funnel for 1960: offer attempts, gate pass rates, distributor tiers.

Usage: python SimTools/analyze_small_label_deal_funnel.py SimLogs/<prefix>
"""
import sys
import pandas as pd

prefix = sys.argv[1]
att = pd.read_csv(prefix + "-distribution-offer-attempts.csv")
led = pd.read_csv(prefix + "-deal-ledger.csv")
lab = pd.read_csv(prefix + "-label-directory.csv")

small = set(lab.loc[lab.initialTier == "Small", "labelId"])
print("starting Small labels:", len(small))

a = att[att.clientId.isin(small) & (att.year == 1960)].copy()
print("1960 Small attempts:", len(a), "labels attempted:", a.clientId.nunique())
print("\noutcome counts:")
print(a.outcome.value_counts())

signed = led[(led.resolution == "Signed") & led.clientId.isin(small) & (led.year == 1960)]
print("\n1960 signed deals with starting-Small clients:", len(signed), "unique:", signed.clientId.nunique())
print("origin:", signed.origin.value_counts().to_dict())
dists = signed.merge(lab[["labelId", "initialTier"]], left_on="distributorId", right_on="labelId", how="left")
print("distributor tier:", dists.initialTier.value_counts().to_dict())
print("reachGranted median %.3f, advance median %.0f, skim median %.3f, masters %.1f%%" % (
    signed.reachGranted.median(), signed.advance.median(), signed.marginSkim.median(), 100 * signed.ownsMasters.mean()))
print("signed week quartiles:", signed.eventWeek.quantile([.1, .25, .5, .75, .9]).to_dict())

# funnel per label: first week with evidence, first week signed
first_ev = a[a.persistentRegionalEvidence].groupby("clientId").week.min()
print("\nSmall labels with persistent evidence (peak>=.20 any region) in 1960:", len(first_ev),
      "of", len(small), "= %.1f%%" % (100 * len(first_ev) / len(small)))
print("first evidence week quartiles:", first_ev.quantile([.1, .25, .5, .75, .9]).to_dict())

ev = a[a.persistentRegionalEvidence]
print("\nattempts WITH evidence:", len(ev))
print(" pullChancePassed rate: %.3f" % ev.pullChancePassed.mean())
print(" pushEvidence rate: %.3f" % ev.pushEvidence.mean())
rolled = ev[ev.pullChancePassed | ev.pushChancePassed]
print(" after chance roll:", len(rolled), "outcomes:", rolled.outcome.value_counts().to_dict())
offered = rolled[rolled.outcome.isin(["Signed", "Rejected"])]
print(" acceptance rate: %.3f" % (offered.outcome == "Signed").mean())

print("\nbestAnyRegionPeak among Small attempts (first attempt per label/month):")
print(a.bestAnyRegionPeak.describe(percentiles=[.1, .25, .5, .75, .9]))
print("bestStrongRegionPeak:")
print(a.bestStrongRegionPeak.describe(percentiles=[.1, .25, .5, .75, .9]))
# evidence from home/strong regions vs outside
print("evidence where strong-region peak also >= .20: %.3f" % (ev.bestStrongRegionPeak >= .20).mean())
print("legacy gate (quality>.70 & current strong sale) pass among evidence: %.3f" % ev.legacyQualityAndCurrentSalesEvidence.mean())
print("evidence quality median %.3f" % ev.bestPersistentEvidenceQuality.median())

for thr in [.20, .30, .40, .50, .60]:
    n = a[a.bestAnyRegionPeak >= thr].clientId.nunique()
    print("labels reaching any-region peak >= %.2f in 1960: %d (%.1f%%)" % (thr, n, 100 * n / len(small)))
